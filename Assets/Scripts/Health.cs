using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;
    public float deathCleanupDelay = 3f;  // event koymazsan yedek süre

    [Header("Animation")]
    public Animator animator;             // boþsa otomatik bulunur
    public string dieTrigger = "Die";     // Animator Trigger adý

    public bool IsDead { get; private set; }
    float hp;

    EnemyAI ai;
    NavMeshAgent agent;

    void Awake()
    {
        hp = maxHealth;
        if (!animator) animator = GetComponentInChildren<Animator>();
        ai = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        hp -= amount;
        if (hp <= 0f) Die();
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // AI ve hareketi durdur
        if (ai) ai.enabled = false;
        if (agent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false; // zemine yapýþýk kalsýn
        }

        // Ölüm animasyonu tetikle
        if (animator)
        {
            // güvenlik: saldýrý/koþu paramlarýný sýfýrla
            if (animator.HasParameterOfType("Attack", AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger("Attack");
            if (animator.HasParameterOfType("Speed", AnimatorControllerParameterType.Float))
                animator.SetFloat("Speed", 0f);

            if (!string.IsNullOrEmpty(dieTrigger))
                animator.SetTrigger(dieTrigger);
        }

        // Çarpýþmayý yumuþat (bir sonraki frame'de)
        StartCoroutine(DisableHitboxesNextFrame());

        if (destroyOnDeath)
        {
            // Anim Event gelmezse emniyet olarak gecikme ile sil
            StartCoroutine(FallbackCleanup());
        }
    }

    IEnumerator DisableHitboxesNextFrame()
    {
        yield return null; // bir frame bekle (anim pozisyonu otursun)
        // Ýstersen sadece "Hitbox" layer’lý collider’larý kapatabilirsin
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    IEnumerator FallbackCleanup()
    {
        yield return new WaitForSeconds(deathCleanupDelay);
        if (this) Destroy(gameObject);
    }

    // ANIMATION EVENT: Ölüm klibinin son frame’ine bu ismi ver
    public void OnDeathAnimationComplete()
    {
        if (destroyOnDeath) Destroy(gameObject);
    }
}

// Küçük yardýmcý: Parametre var mý kontrolü
public static class AnimatorExt
{
    public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in self.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
}
