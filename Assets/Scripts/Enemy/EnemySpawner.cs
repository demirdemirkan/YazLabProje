using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;

    [Header("When to spawn")]
    public bool spawnOnStart = true;
    public int spawnCount = 5;

    [Header("Where to spawn")]
    public float spawnRadius = 30f;
    public Transform[] spawnAreas;

    [Header("Safety")]
    public float sampleMaxDistance = 6f;          // NavMesh.SamplePosition yarýçapý
    public float minDistanceBetweenEnemies = 2f;  // Birbirine yapýþmasýn
    public float minDistanceFromPlayer = 8f;      // Oyuncunun dibine doðmasýn
    public LayerMask groundMask = ~0;             // Zemini kapsasýn

    // Dahili
    private readonly List<Transform> _spawned = new List<Transform>();
    private Transform _player;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
    }

    void Start()
    {
        if (spawnOnStart) SpawnWave(spawnCount);
    }

    [ContextMenu("Spawn One")]
    public void SpawnOneContext() => SpawnOne();

    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
            if (_spawned[i]) Destroy(_spawned[i].gameObject);
        _spawned.Clear();
    }

    public void SpawnWave(int count)
    {
        int spawned = 0, safety = 0;
        while (spawned < count && safety < count * 30)
        {
            safety++;
            if (SpawnOne()) spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[EnemySpawner] Ýstenen {count} adetten {spawned} adet spawn edildi. NavMesh/Layer ayarlarýný kontrol et.");
    }

    bool SpawnOne()
    {
        if (!enemyPrefab)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab atanmadý.");
            return false;
        }

        // 1) Merkez
        Vector3 center = transform.position;
        if (spawnAreas != null && spawnAreas.Length > 0)
        {
            var t = spawnAreas[Random.Range(0, spawnAreas.Length)];
            if (t) center = t.position;
        }

        // 2) Rastgele öneri
        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = center + new Vector3(r.x, 2f, r.y); // hafif yukarýdan

        // 3) NavMesh üzerinde en yakýn nokta
        if (!NavMesh.SamplePosition(candidate, out var meshHit, sampleMaxDistance, NavMesh.AllAreas))
            return false;

        Vector3 spawnPos = meshHit.position + Vector3.up * 0.5f; // biraz yukarý buffer

        // 4) Oyuncuya ve diðer düþmanlara mesafe
        if (_player && Vector3.Distance(spawnPos, _player.position) < minDistanceFromPlayer)
            return false;
        for (int i = 0; i < _spawned.Count; i++)
        {
            var t = _spawned[i];
            if (!t) continue;
            if (Vector3.Distance(spawnPos, t.position) < minDistanceBetweenEnemies)
                return false;
        }

        // 5) Raycast ile zemine “snap”
        if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out var gh, 6f, groundMask, QueryTriggerInteraction.Ignore))
            spawnPos = gh.point + Vector3.up * 0.02f;

        // 6) Üret
        var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        _spawned.Add(go.transform);

        // 7) Agent varsa güvenli warp
        var agent = go.GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.baseOffset = 0f;

            // Eðer doðduðu nokta NavMesh dýþýnda görünürse, en yakýna örnekle
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(spawnPos, out var fix, sampleMaxDistance, NavMesh.AllAreas))
                    spawnPos = fix.position + Vector3.up * 0.02f;
                else
                    Debug.LogWarning("[EnemySpawner] NavMesh bulunamadý, transform.position kullanýlacak.");
            }

            // Ajanýn iç dengesini de o konuma getir
            agent.Warp(spawnPos);
            agent.nextPosition = spawnPos;
        }
        else
        {
            go.transform.position = spawnPos;
        }

        return true;
    }

    // Görsel rehber
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Vector3 center = transform.position;
        if (spawnAreas != null && spawnAreas.Length > 0)
        {
            foreach (var t in spawnAreas) { if (!t) continue; Gizmos.DrawSphere(t.position, 0.4f); }
            int c = 0; Vector3 sum = Vector3.zero;
            foreach (var t in spawnAreas) { if (t) { sum += t.position; c++; } }
            if (c > 0) center = sum / c;
        }
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
