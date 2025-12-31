using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;
    public float deathCleanupDelay = 3f;

    [Header("Animation")]
    public Animator animator;
    public string dieTrigger = "Die";

    public bool IsDead { get; private set; }
    public float Current => hp;          // UI için
    public float Max => maxHealth;       // UI için
    public event Action<float, float> OnHealthChanged; // (current, max)

    float hp;

    EnemyAI ai;
    NavMeshAgent agent;

    void Awake()
    {
        hp = maxHealth;
        if (!animator) animator = GetComponentInChildren<Animator>();
        ai = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();

        // ilk yayýn
        OnHealthChanged?.Invoke(hp, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        hp = Mathf.Max(0f, hp - amount);
        OnHealthChanged?.Invoke(hp, maxHealth);
        if (hp <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        hp = Mathf.Min(maxHealth, hp + amount);
        OnHealthChanged?.Invoke(hp, maxHealth);
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (ai) ai.enabled = false;
        if (agent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        if (animator && !string.IsNullOrEmpty(dieTrigger))
            animator.SetTrigger(dieTrigger);

        StartCoroutine(DisableHitboxesNextFrame());

        if (destroyOnDeath)
            StartCoroutine(FallbackCleanup());
    }

    IEnumerator DisableHitboxesNextFrame()
    {
        yield return null;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var rb in GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
    }

    IEnumerator FallbackCleanup()
    {
        yield return new WaitForSeconds(deathCleanupDelay);
        if (this) Destroy(gameObject);
    }

    // ANIMATION EVENT ile çaðýrýlabilir
    public void OnDeathAnimationComplete()
    {
        if (destroyOnDeath) Destroy(gameObject);
    }
}
