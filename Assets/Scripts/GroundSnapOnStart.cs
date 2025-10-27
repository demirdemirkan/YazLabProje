using UnityEngine;
using UnityEngine.AI;

public class GroundSnapOnStart : MonoBehaviour
{
    [Tooltip("Model/Animator'ýn olduðu child (boþ býrakýlýrsa otomatik bulunur)")]
    public Transform model;
    public LayerMask groundMask = ~0;
    public float snapRayHeight = 3f;   // üstten atýlacak ray yüksekliði
    public float snapMaxDistance = 8f; // yere kadar max mesafe
    public float smallLift = 0.02f;    // zeminin biraz üstünde dursun

    void Start()
    {
        // 1) Agent varsa önce ayný noktaya warp et (NavMesh'te hizala)
        var agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.baseOffset = 0f;
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
            }
            agent.nextPosition = transform.position;
        }

        // 2) Hips merkezinden zemine ray at (model child'ý yoksa hips bul)
        Vector3 origin = transform.position + Vector3.up * snapRayHeight;

        // Hips varsa oradan dene (daha doðru)
        if (!model)
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim)
            {
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                if (hips) origin = hips.position + Vector3.up * 0.2f;
                model = anim.transform;
            }
        }

        if (Physics.Raycast(origin, Vector3.down, out var gh, snapRayHeight + snapMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = gh.point.y + smallLift;
            if (agent && agent.isOnNavMesh) agent.Warp(p);
            transform.position = p;
        }
    }
}
