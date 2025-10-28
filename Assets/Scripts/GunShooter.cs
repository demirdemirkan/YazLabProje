using UnityEngine;

public class GunShooter : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // Main Camera'yý sürükle
    public Transform muzzle;           // opsiyonel, namlu ucu
    public WeaponEquip weaponEquip;    // Player'daki WeaponEquip (isAiming okunacak)

    [Header("Fire")]
    public bool requireAim = true;     // true = sadece niþandayken ateþ
    public bool semiAuto = true;       // true=tek tek, false=otomatik
    public float fireRate = 8f;        // otomatikte sn/mermi
    public float damage = 25f;
    public float range = 120f;
    public LayerMask hitMask = ~0;     // Enemy layer’ýný ekle, Player’ý çýkar

    [Header("FX (opsiyonel)")]
    public ParticleSystem muzzleFlash;
    public GameObject impactVfxPrefab;
    public AudioSource shotAudio;

    float cd;

    void Reset()
    {
        cam = Camera.main;
        if (!weaponEquip) weaponEquip = GetComponentInParent<WeaponEquip>();
    }

    void Update()
    {
        cd -= Time.deltaTime;

        if (requireAim && weaponEquip && !weaponEquip.isAiming) return;

        bool wantFire = semiAuto ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0);
        if (!wantFire || cd > 0f) return;

        cd = semiAuto ? 0.05f : (1f / Mathf.Max(1f, fireRate));
        FireOnce();
    }

    void FireOnce()
    {
        if (muzzleFlash) muzzleFlash.Play();
        if (shotAudio) shotAudio.Play();

        if (!cam) cam = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(dir * 6f, hit.point, ForceMode.Impulse);

            if (impactVfxPrefab)
            {
                var fx = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 3f);
            }
        }
    }
}
