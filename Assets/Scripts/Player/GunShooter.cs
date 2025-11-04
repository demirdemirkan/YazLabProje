using UnityEngine;

public class GunShooter : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                      // Main Camera'yý sürükle (boþsa otomatik bulunur)
    public Transform muzzle;                // opsiyonel, namlu ucu (event ile oto bulunur)
    public WeaponEquip weaponEquip;         // Player'daki WeaponEquip (isAiming okunacak)

    [Header("Fire")]
    public bool requireAim = true;          // true = sadece niþandayken ateþ
    public bool semiAuto = true;            // true=tek tek, false=otomatik
    public float fireRate = 8f;             // otomatikte sn/mermi
    public float damage = 25f;
    public float range = 120f;
    public LayerMask hitMask = ~0;          // Enemy layer’ýný ekle, Player’ý çýkar

    [Header("FX (opsiyonel)")]
    public ParticleSystem muzzleFlash;      // event ile oto bulunur
    public GameObject impactVfxPrefab;

    [Tooltip("Ses kaynaðý; event ile eldeki silahtan oto bulunur")]
    public AudioSource shotAudio;
    [Tooltip("Varsa PlayOneShot ile çalar; boþsa AudioSource üzerindeki clip çalýnýr")]
    public AudioClip shotClip;
    public Vector2 pitchRandom = new Vector2(0.95f, 1.05f);

    [Header("Auto-bind")]
    [Tooltip("WeaponEquip silah deðiþince otomatik baðlansýn")]
    public bool autoBindFromWeaponEquip = true;
    [Tooltip("Silah prefabýnda namlu boþ objesinin adý")]
    public string muzzleName = "Muzzle";

    float cd;

    void Reset()
    {
        cam = Camera.main;
        if (!weaponEquip) weaponEquip = GetComponentInParent<WeaponEquip>();
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!weaponEquip) weaponEquip = GetComponentInParent<WeaponEquip>();

        // WeaponEquip event'ine abone ol (eldeki silah deðiþince oto baðla)
        if (autoBindFromWeaponEquip && weaponEquip != null)
        {
            weaponEquip.OnWeaponChanged += HandleWeaponChanged;
            // sahne baþýnda elde silah varsa ilk durumu baðla
            HandleWeaponChanged(weaponEquip.CurrentWeaponRoot);
        }
    }

    void OnDestroy()
    {
        if (weaponEquip != null)
            weaponEquip.OnWeaponChanged -= HandleWeaponChanged;
    }

    void Update()
    {
        cd -= Time.deltaTime;

        // Yalnýzca niþandayken ateþ (WeaponEquip yoksa niþan sayýlmýyor)
        if (requireAim && (weaponEquip == null || weaponEquip.isAiming == true))
            return;

        bool wantFire = semiAuto ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0);
        if (!wantFire || cd > 0f) return;

        cd = semiAuto ? 0.05f : (1f / Mathf.Max(1f, fireRate));
        FireOnce();
    }

    void FireOnce()
    {
        // VFX & SFX
        if (muzzleFlash) muzzleFlash.Play();

        if (shotAudio)
        {
            shotAudio.pitch = Random.Range(pitchRandom.x, pitchRandom.y);
            if (shotClip) shotAudio.PlayOneShot(shotClip);
            else shotAudio.Play(); // AudioSource üzerindeki clip
        }

        if (!cam) cam = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Hasar
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp) hp.TakeDamage(damage);

            // Küçük fizik etkisi
            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(dir * 6f, hit.point, ForceMode.Impulse);

            // Impact VFX
            if (impactVfxPrefab)
            {
                var fx = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 3f);
            }
        }
    }

    // === OTO BAÐLAMA (WeaponEquip olayýndan) ===
    void HandleWeaponChanged(Transform weaponRoot)
    {
        if (weaponRoot == null)
        {
            muzzle = null;
            muzzleFlash = null;
            shotAudio = null;
            return;
        }

        // Muzzle adýyla ara; bulunamazsa kökü kullan
        muzzle = FindDeepChild(weaponRoot, muzzleName);
        if (!muzzle) muzzle = weaponRoot;

        // Silah altýndan flash & audio kaynaklarýný bul
        muzzleFlash = weaponRoot.GetComponentInChildren<ParticleSystem>(true);
        shotAudio = weaponRoot.GetComponentInChildren<AudioSource>(true);

        // (Ýstersen debug)
        // Debug.Log($"[GunShooter] Bound -> weapon={weaponRoot.name}, muzzle={muzzle?.name}, audio={shotAudio?.name}, flash={muzzleFlash?.name}");
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        if (!parent) return null;
        if (parent.name == name) return parent;
        foreach (Transform c in parent)
        {
            var t = FindDeepChild(c, name);
            if (t) return t;
        }
        return null;
    }
}
