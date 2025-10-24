using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI_Chase : MonoBehaviour
{
    public Transform player;               // Boþ kalýrsa tag=Player’dan bulacaðýz
    public float sightRange = 15f;         // görürse kovala
    public float loseRange = 20f;         // bu mesafeyi aþarsa býrak
    public float patrolRadius = 6f;
    public float patrolWait = 1.5f;
    public float patrolMinStep = 2f;

    NavMeshAgent agent;
    Vector3 home;
    float waitTimer;

    enum State { Patrol, Chase }
    State state;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        GoPatrol();
    }

    void Update()
    {
        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        switch (state)
        {
            case State.Patrol:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= patrolWait) { waitTimer = 0f; SetPatrolDestination(); }
                }
                if (player && dist <= sightRange) state = State.Chase;
                break;

            case State.Chase:
                if (!player) { GoPatrol(); break; }
                agent.SetDestination(player.position);
                if (dist > loseRange) GoPatrol();
                break;
        }
    }

    void GoPatrol()
    {
        state = State.Patrol;
        SetPatrolDestination();
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
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, patrolRadius);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
