using UnityEngine;

[DisallowMultipleComponent]
public class GuardAimer : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;           // boþsa Player tag’inden bulur
    public Animator animator;          // boþsa çocuklarda arar

    [Header("Detection")]
    public float engageDistance = 20f; // bu mesafede niþan al
    public float loseDistance = 25f;   // bundan uzaklaþýnca býrak
    public LayerMask losMask = ~0;     // görüþ hattý kontrolü için (Duvar/Zemin seç, Player'ý çýkar)

    [Header("Facing")]
    public float turnSpeed = 5f;       // saniyede kaç “lerp” hýzýnda dönecek

    [Header("Animator")]
    public string aimBool = "Aim";     // Animator’daki bool parametresi

    bool aiming;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (!player || !animator) return;

        // mesafe
        float d = Vector3.Distance(transform.position, player.position);

        // görüþ hattý (opsiyonel ama iyi olur)
        bool hasLoS = true;
        Vector3 eye = animator.GetBoneTransform(HumanBodyBones.Head)?.position
                      ?? transform.position + Vector3.up * 1.6f;
        Vector3 dir = (player.position + Vector3.up * 1.2f) - eye;

        if (Physics.Raycast(eye, dir.normalized, out var hit, Mathf.Max(engageDistance, loseDistance), losMask,
                            QueryTriggerInteraction.Ignore))
        {
            // Player’a çarpmadýysa önünde engel var demektir
            hasLoS = hit.collider.CompareTag("Player");
        }

        // niþana gir / çýk
        if (!aiming && d <= engageDistance && hasLoS) SetAim(true);
        else if (aiming && (d >= loseDistance || !hasLoS)) SetAim(false);

        // niþandayken yumuþakça oyuncuya dön
        if (aiming)
        {
            Vector3 flatToPlayer = player.position - transform.position;
            flatToPlayer.y = 0f;
            if (flatToPlayer.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }

    void SetAim(bool v)
    {
        aiming = v;
        animator.SetBool(aimBool, v);
    }

    // sahnede menzili görsel olsun
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, engageDistance);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseDistance);
    }
}
