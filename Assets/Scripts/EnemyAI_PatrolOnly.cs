using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI_PatrolOnly : MonoBehaviour
{
    public float patrolRadius = 6f;   // küçük alan
    public float patrolWait = 1.5f;   // hedefte bekleme
    public float patrolMinStep = 2f;  // dibine nokta seçmesin

    NavMeshAgent agent;
    Vector3 home;
    float waitTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
    }

    void Start()
    {
        SetPatrolDestination();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWait)
            {
                waitTimer = 0f;
                SetPatrolDestination();
            }
        }
    }

    void SetPatrolDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 cand = home + new Vector3(r.x, 0f, r.y);
            if (Vector3.Distance(cand, transform.position) < patrolMinStep) continue;

            if (NavMesh.SamplePosition(cand, out var hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
        agent.SetDestination(home);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, patrolRadius);
    }
}
