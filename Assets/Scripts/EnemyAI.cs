using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public Animator animator;

    [Header("Ranges")]
    public float sightRange = 15f;   // görürse kovalar
    public float loseRange = 20f;   // bu mesafeyi aþarsa býrakýr
    public float attackRange = 1.8f;  // gerçek temas menzili
    [Range(0f, 180f)] public float attackAngle = 60f; // hedefe göre +/- açý penceresi

    [Header("Speeds")]
    public float patrolMoveSpeed = 3.0f;  // yürüyüþ
    public float chaseMoveSpeed = 5.5f;  // koþu
    public float angularSpeed = 720f;  // NavMeshAgent dönüþ hýzý (deg/s)
    public float faceTurnLerp = 12f;   // Attack modunda yumuþak dönüþ

    [Header("Patrol")]
    public float patrolRadius = 6f;
    public float patrolWait = 1.5f;
    public float patrolMinStep = 2f;

    [Header("Combat")]
    public float attackCooldown = 1.0f;
    public float attackDamage = 20f;

    [Header("Animator Params")]
    public string speedParam = "Speed";   // BlendTree paramý (0=idle, 0.5=walk, 1=run)
    public string attackParam = "Attack";  // Trigger

    private enum State { Patrol, Chase, Attack }
    private State state;

    private NavMeshAgent agent;
    private Vector3 home;
    private float waitTimer, cd;

    // anim “Speed” deðerini yumuþatarak besleyelim
    private float animSpeed, animSpeedTarget;
    public float animLerp = 8f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        // Agent temel ayarlarý
        agent.angularSpeed = angularSpeed;
        agent.acceleration = 12f;
        agent.autoBraking = true;
        agent.baseOffset = 0f;
        agent.updateRotation = true; // Patrol/Chase’te dönüþü ajan yapsýn
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange * 0.95f);

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
        cd -= Time.deltaTime;

        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        float angle = 999f;
        if (player)
        {
            Vector3 to = (player.position - transform.position); to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                angle = Vector3.Angle(transform.forward, to.normalized);
        }

        switch (state)
        {
            case State.Patrol:
                agent.speed = patrolMoveSpeed;
                animSpeedTarget = 0.5f; // Walk

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    waitTimer += Time.deltaTime;
                    if (waitTimer >= patrolWait) { waitTimer = 0f; SetPatrolDestination(); }
                }

                if (player && dist <= sightRange)
                    state = State.Chase;
                break;

            case State.Chase:
                if (!player) { GoPatrol(); break; }

                agent.updateRotation = true;
                agent.isStopped = false;
                agent.speed = chaseMoveSpeed;
                animSpeedTarget = 1.0f; // Run

                agent.SetDestination(player.position);

                // Yakýn + açý uygunsa Attack
                if (dist <= attackRange && angle <= attackAngle)
                {
                    state = State.Attack;
                    agent.ResetPath();
                    agent.isStopped = true;   // dur
                    agent.updateRotation = false;  // dönüþü biz yapacaðýz
                    animSpeedTarget = 0.0f;   // dur anim blend
                }
                else if (dist > loseRange)
                {
                    GoPatrol();
                }
                break;

            case State.Attack:
                if (!player) { GoPatrol(); break; }

                // hedefe dön (yumuþak)
                FaceTowards(player.position, faceTurnLerp);

                // pencere dýþýna çýktýysa tekrar kovala
                if (dist > attackRange + 0.3f || angle > attackAngle + 10f)
                {
                    state = State.Chase;
                    agent.isStopped = false;
                    agent.updateRotation = true;
                    break;
                }

                // saldýr tetikle
                if (cd <= 0f)
                {
                    cd = attackCooldown;
                    if (animator && !string.IsNullOrEmpty(attackParam))
                        animator.SetTrigger(attackParam);

                    // Anim Event “Hit()” eklemeden test edeceksen:
                    // Hit();
                }
                break;
        }

        // Animator Speed paramýný smooth besle
        animSpeed = Mathf.MoveTowards(animSpeed, animSpeedTarget, animLerp * Time.deltaTime);
        if (animator && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, animSpeed);
    }

    void GoPatrol()
    {
        state = State.Patrol;
        agent.isStopped = false;
        agent.updateRotation = true;
        animSpeedTarget = 0.5f;
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

    // Attack sýrasýnda dönüþü biz yapýyoruz (yumuþak Slerp)
    void FaceTowards(Vector3 worldPos, float slerp)
    {
        Vector3 dir = worldPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, slerp * Time.deltaTime);
    }

    // Animation Event'ten çaðýr (Attack klibinde doðru frame’e “Hit” event ekle)
    public void Hit()
    {
        if (!player) return;

        // Hit frame’inde tekrar doðrula (mesafe + açý)
        Vector3 to = player.position - transform.position; to.y = 0f;
        float dist = to.magnitude;
        float angle = 999f;
        if (to.sqrMagnitude > 0.0001f)
            angle = Vector3.Angle(transform.forward, to.normalized);

        if (dist > attackRange + 0.35f) return;
        if (angle > attackAngle + 15f) return;

        Debug.Log($"[Enemy] Hit! dist={dist:0.00}, angle={angle:0}");
        // Örnek damage:
        // var hp = player.GetComponentInParent<Health>();
        // hp?.TakeDamage(attackDamage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, patrolRadius);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);

#if UNITY_EDITOR
        // açý konisini kabaca göster
        Vector3 f = transform.forward;
        Quaternion qL = Quaternion.AngleAxis(-attackAngle, Vector3.up);
        Quaternion qR = Quaternion.AngleAxis(+attackAngle, Vector3.up);
        Debug.DrawRay(transform.position, qL * f * attackRange, Color.red);
        Debug.DrawRay(transform.position, qR * f * attackRange, Color.red);
#endif
    }
}
