using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;

    float hp;

    void Awake() => hp = maxHealth;

    public void TakeDamage(float amount)
    {
        hp -= amount;
        if (hp <= 0f) Die();
    }

    void Die()
    {
        // TODO: ölüm animasyonu / ragdoll / loot / skor
        if (destroyOnDeath) Destroy(gameObject);
    }
}
