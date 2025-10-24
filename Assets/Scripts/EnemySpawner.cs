using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyPrefab;          // Düþman prefabýný sürükle (öneri: içinde NavMeshAgent olsun)

    [Header("When to spawn")]
    public bool spawnOnStart = true;        // Oyun baþýnda otomatik üret
    public int spawnCount = 5;              // Kaç tane üretilecek

    [Header("Where to spawn")]
    public float spawnRadius = 30f;         // Bu objenin (veya seçilen alanýn) etrafýnda
    public Transform[] spawnAreas;          // Opsiyonel: belirli merkezler (rasgele biri seçilir)

    [Header("Safety")]
    public float sampleMaxDistance = 4f;    // NavMesh.SamplePosition yarýçapý
    public float minDistanceBetweenEnemies = 2.0f;  // Birbirine çok yapýþmasýnlar
    public float minDistanceFromPlayer = 8.0f;      // Oyuncunun dibine doðmasýnlar
    public LayerMask groundMask = ~0;       // (opsiyonel) yere ray atýp saðlamasýný yapar

    // Dahili
    List<Transform> _spawned = new List<Transform>();
    Transform _player;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
    }

    void Start()
    {
        if (spawnOnStart)
            SpawnWave(spawnCount);
    }

    [ContextMenu("Spawn One")]
    public void SpawnOneContext() => SpawnOne();

    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i]) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }

    public void SpawnWave(int count)
    {
        int spawned = 0;
        int safety = 0;
        while (spawned < count && safety < count * 20)
        {
            safety++;
            if (SpawnOne())
                spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[EnemySpawner] Ýstenen {count} adetten {spawned} adet spawn edilebildi. NavMesh alanýný ya da ayarlarý kontrol et.");
    }

    bool SpawnOne()
    {
        if (!enemyPrefab)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab atanmamýþ.");
            return false;
        }

        // 1) Merkez seç (spawnAreas varsa onlardan biri, yoksa bu objenin konumu)
        Vector3 center = transform.position;
        if (spawnAreas != null && spawnAreas.Length > 0)
        {
            var t = spawnAreas[Random.Range(0, spawnAreas.Length)];
            if (t) center = t.position;
        }

        // 2) Rastgele bir hedef nokta öner
        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = center + new Vector3(r.x, 0f, r.y);

        // 3) NavMesh üzerinde en yakýn geçerli nokta
        if (!NavMesh.SamplePosition(candidate, out var hit, sampleMaxDistance, NavMesh.AllAreas))
            return false; // Bu deneme baþarýsýz, tekrar deneriz

        Vector3 spawnPos = hit.position;

        // 4) Oyuncuya çok yakýn doðmasýn
        if (_player && Vector3.Distance(spawnPos, _player.position) < minDistanceFromPlayer)
            return false;

        // 5) Baþka düþmanlarýn dibine doðmasýn
        for (int i = 0; i < _spawned.Count; i++)
        {
            var t = _spawned[i];
            if (!t) continue;
            if (Vector3.Distance(spawnPos, t.position) < minDistanceBetweenEnemies)
                return false;
        }

        // 6) (Opsiyonel) Yüzeye oturtmak için kýsa bir yere ray
        if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out var groundHit, 5f, groundMask, QueryTriggerInteraction.Ignore))
            spawnPos.y = groundHit.point.y;

        // 7) Üret
        var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        _spawned.Add(go.transform);

        // 8) Güvence: Agent varsa hemen NavMesh üzerindeyiz mi?
        var agent = go.GetComponent<NavMeshAgent>();
        if (agent)
        {
            // Agent zemine gömülmesin diye baseOffset’i sýfýrla
            agent.baseOffset = 0f;
            if (!agent.isOnNavMesh)
                Debug.LogWarning("[EnemySpawner] Spawnlanan ajan NavMesh üzerinde deðil görünüyor (spawn alaný/bake ayarýný kontrol et).");
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
            foreach (var t in spawnAreas)
            {
                if (!t) continue;
                Gizmos.DrawSphere(t.position, 0.4f);
            }
            // gösterim için ortalamayý alalým
            int c = 0; Vector3 sum = Vector3.zero;
            foreach (var t in spawnAreas) { if (t) { sum += t.position; c++; } }
            if (c > 0) center = sum / c;
        }
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
