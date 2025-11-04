using UnityEngine;

[DisallowMultipleComponent]
public class GuardShooter : MonoBehaviour
{
    [Header("Targets & Refs")]
    public Transform player;                // Boþsa tag "Player" dan bulur
    public Animator animator;               // SWAT Animator
    public Transform head;                  // opsiyonel (ray origin), yoksa otomatik
    public Transform muzzle;                // namlu ucu (Muzzle boþu)

    [Header("Engage")]
    public float engageDistance = 22f;      // buna girince devreye gir
    public float loseDistance = 28f;      // bundan çýkýnca býrak
    public float rotateLerp = 8f;       // oyuncuya dönme hýzý (Y ekseni)

    [Header("Fire")]
    public float fireInterval = 0.45f;    // mermi aralýðý
    public float damage = 15f;
    public float range = 120f;

    [Tooltip("Görüþ hattýný engelleyen katmanlar (Environment vb.). Oyuncu katmanýný seçme).")]
    public LayerMask losMask = ~0;          // LoS: “Player” hariç engeller
    [Tooltip("Merminin vurabileceði katmanlar (Player/Enemy/Props vs).")]
    public LayerMask hitMask = ~0;

    [Header("FX (opsiyonel)")]
    public ParticleSystem muzzleFlash;
    public AudioSource shotAudio;
    public GameObject impactVfx;

    [Header("Animator Params")]
    public string aimBool = "Aim";        // niþan halinde true
    public string shootTrig = "Shoot";      // her atýþta tetik

    float fireCd;
    bool engaged;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!head && animator) head = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    void Update()
    {
        if (!player) return;

        float d = Vector3.Distance(transform.position, player.position);

        if (!engaged && d <= engageDistance && HasLOS())
            SetEngaged(true);
        else if (engaged && d > loseDistance)
            SetEngaged(false);

        if (!engaged) return;

        // Oyuncuya doðru sadece yatayda dön
        Vector3 flat = player.position - transform.position; flat.y = 0f;
        if (flat.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateLerp * Time.deltaTime);
        }

        // Ateþ
        fireCd -= Time.deltaTime;
        if (fireCd <= 0f && HasLOS())
        {
            FireOnce();
            fireCd = fireInterval;
        }
    }

    bool HasLOS()
    {
        // Baþ veya namlu hizasýndan oyuncunun göðsüne doðru
        Vector3 origin = muzzle ? muzzle.position
                                : head ? head.position
                                       : transform.position + Vector3.up * 1.6f;

        Vector3 target = player.position + Vector3.up * 0.9f;
        Vector3 dir = (target - origin);
        float dist = dir.magnitude;
        dir /= dist;

        // Önce engel var mý? (oyuncudan önce bir þey çarparsa engellenmiþ say)
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, losMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != player && !hit.transform.IsChildOf(player))
                return false;
        }
        return true;
    }

    void FireOnce()
    {
        if (animator && !string.IsNullOrEmpty(shootTrig)) animator.SetTrigger(shootTrig);
        if (muzzleFlash) muzzleFlash.Play();
        if (shotAudio) shotAudio.Play();

        Vector3 origin = muzzle ? muzzle.position : (head ? head.position : transform.position + Vector3.up * 1.6f);
        Vector3 target = player.position + Vector3.up * 0.9f;
        Vector3 dir = (target - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // hasar
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            // etki
            if (impactVfx)
            {
                var fx = Instantiate(impactVfx, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 3f);
            }
        }
    }

    void SetEngaged(bool on)
    {
        engaged = on;
        if (animator && !string.IsNullOrEmpty(aimBool)) animator.SetBool(aimBool, on);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, engageDistance);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseDistance);
    }
}
