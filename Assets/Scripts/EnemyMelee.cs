using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;
    public float hitRadius = 1.2f;
    public LayerMask targetMask; // Player layer’ýný iþaretle

    [Header("Origin")]
    public Transform hitOrigin;  // boþsa this.transform

    [Header("Once-per-swing")]
    public float swingCooldown = 0.3f; // ayný event birden çok kez gelirse filtre
    float lastSwingTime = -999f;

    // ANIMATION EVENT
    public void Hit()
    {
        if (Time.time - lastSwingTime < swingCooldown) return;
        lastSwingTime = Time.time;

        var origin = hitOrigin ? hitOrigin.position : transform.position;
        var cols = Physics.OverlapSphere(origin, hitRadius, targetMask, QueryTriggerInteraction.Ignore);
        foreach (var c in cols)
        {
            var hp = c.GetComponentInParent<Health>();
            if (hp != null && !hp.IsDead)
            {
                hp.TakeDamage(damage);
                break; // tek hedef yeter
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var origin = hitOrigin ? hitOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, hitRadius);
    }
}
