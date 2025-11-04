using UnityEngine;

[DisallowMultipleComponent]
public class EnemyWeaponAttack : MonoBehaviour
{
    // ----------------- SENÝN ALANLARIN -----------------
    [Header("Weapon")]
    public GameObject riflePrefab;
    public string gripName = "Grip";

    [Header("Fine Tune Offsets")]
    public Vector3 positionOffset;
    public Vector3 rotationOffsetEuler;

    [Header("Hand & Animator")]
    public bool useLeftHand = false;
    public Animator animator;
    [Tooltip("Animator'da rifle idle'a geçiren bool (kullanmayacaksan boþ býrak)")]
    public string rifleIdleBool = "";

    [Header("Tuning")]
    public bool liveApply = true;
    public bool log = false;

    Transform handBone;
    Transform handMount;
    GameObject rifleInstance;

    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    // ----------------- SHOOTING -----------------
    [Header("== Shooting ==")]
    public bool enableShooting = true;
    public Transform muzzle;
    public Transform eye;
    public Transform player;
    public float detectRange = 30f;
    public float fireRange = 28f;
    public float fireRate = 3.0f;
    public float damage = 12f;
    public float spreadDeg = 1.2f;
    public float hitForce = 6f;
    public float loseSightAfter = 1.5f;

    [Header("FX (optional)")]
    public AudioSource shotAudio;          // Sahnedeki instance! Boþ býrakabilirsin; otomatik bulunur
    public AudioClip shotClip;             // Ýstersen burada klip ver; fallback PlayOneShot kullanýr
    public ParticleSystem muzzleFlash;
    public GameObject impactVfxPrefab;

    [Header("Layers")]
    public LayerMask losMask = ~0;
    public LayerMask hitMask = ~0;

    [Header("Animator Params (opsiyonel)")]
    public string aimBool = "Aim";
    public string fireTrig = "Fire";

    float fireCd;
    float lostTimer;
    Health playerHealth;
    Transform playerHead;

    // Audio fallback
    AudioSource _fallbackAS;

    // ----------------- LIFECYCLE -----------------
    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator || !animator.isHuman)
        {
            Debug.LogError("[EnemyWeaponAttack] Humanoid Animator bulunamadý.");
            enabled = false; return;
        }

        handBone = animator.GetBoneTransform(useLeftHand ? HumanBodyBones.LeftHand
                                                         : HumanBodyBones.RightHand);
        if (!handBone)
        {
            Debug.LogError("[EnemyWeaponAttack] El kemiði bulunamadý.");
            enabled = false; return;
        }

        string mountName = useLeftHand ? "LeftHand_Mount" : "RightHand_Mount";
        handMount = handBone.Find(mountName);
        if (!handMount)
        {
            handMount = new GameObject(mountName).transform;
            handMount.SetParent(handBone, false);
            handMount.localPosition = Vector3.zero;
            handMount.localRotation = Quaternion.identity;
            handMount.localScale = Vector3.one;
            if (log) Debug.Log("[EnemyWeaponAttack] " + mountName + " oluþturuldu.");
        }

        if (!player)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
        if (player)
        {
            playerHealth = player.GetComponentInParent<Health>();
            playerHead = player.Find("Head") ? player.Find("Head") : player;
        }

        if (!eye)
        {
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            eye = headBone ? headBone : transform;
        }

        // Fallback AudioSource (her ihtimale)
        _fallbackAS = GetComponent<AudioSource>();
        if (!_fallbackAS) _fallbackAS = gameObject.AddComponent<AudioSource>();
        _fallbackAS.playOnAwake = false;
        _fallbackAS.spatialBlend = 1f; // 3D
    }

    void Start()
    {
        EquipNow();

        // Muzzle & Audio auto-bind
        if (!muzzle && rifleInstance)
            muzzle = FindDeep(rifleInstance.transform, "Muzzle") ?? rifleInstance.transform;

        AutoBindAudioFromWeapon();
    }

    void Update()
    {
        if (!enableShooting || !player) return;

        fireCd -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inDetect = dist <= detectRange;
        bool canSee = inDetect && HasLineOfSight();

        bool aimState = canSee || (lostTimer > 0f);
        if (animator && !string.IsNullOrEmpty(aimBool))
            animator.SetBool(aimBool, aimState);

        if (aimState)
        {
            Vector3 look = player.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(look);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 6f);
            }
        }

        if (canSee) lostTimer = loseSightAfter;
        else lostTimer = Mathf.Max(0f, lostTimer - Time.deltaTime);

        if (canSee && dist <= fireRange && fireCd <= 0f)
        {
            fireCd = 1f / Mathf.Max(0.1f, fireRate);
            FireOnce();
        }
    }

    void LateUpdate()
    {
        if (liveApply && rifleInstance != null)
        {
            var t = rifleInstance.transform;
            t.localPosition = baseLocalPos + positionOffset;
            t.localRotation = baseLocalRot * Quaternion.Euler(rotationOffsetEuler);
        }
    }

    // ----------------- EQUIP -----------------
    [ContextMenu("Reapply Now")]
    public void EquipNow()
    {
        if (!riflePrefab) { Debug.LogWarning("[EnemyWeaponAttack] riflePrefab boþ."); return; }

        for (int i = handMount.childCount - 1; i >= 0; i--)
            Destroy(handMount.GetChild(i).gameObject);

        rifleInstance = Instantiate(riflePrefab);
        var t = rifleInstance.transform;
        t.SetParent(handMount, false);

        Transform grip = FindDeep(t, gripName);
        if (grip)
        {
            t.localRotation *= Quaternion.Inverse(grip.localRotation);
            t.localPosition -= grip.localPosition;
            if (log) Debug.Log("[EnemyWeaponAttack] Grip ile hizalandý.");
        }
        else
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            if (log) Debug.Log("[EnemyWeaponAttack] Grip bulunamadý, sýfýrlandý.");
        }

        baseLocalPos = t.localPosition;
        baseLocalRot = t.localRotation;

        t.localPosition = baseLocalPos + positionOffset;
        t.localRotation = baseLocalRot * Quaternion.Euler(rotationOffsetEuler);
        t.localScale = Vector3.one;

        foreach (var c in rifleInstance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in rifleInstance.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;

        if (!string.IsNullOrEmpty(rifleIdleBool) && animator)
            animator.SetBool(rifleIdleBool, true);

        if (!muzzle) muzzle = FindDeep(t, "Muzzle");

        // Equip sonrasý ses kaynaklarýný yeniden baðla
        AutoBindAudioFromWeapon();
    }

    // ----------------- SHOOTING HELPERS -----------------
    bool HasLineOfSight()
    {
        Vector3 origin = eye ? eye.position : transform.position + Vector3.up * 1.6f;
        Vector3 target = playerHead ? playerHead.position : player.position + Vector3.up * 1.5f;
        Vector3 dir = (target - origin).normalized;
        float d = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, d + 0.1f, losMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.GetComponentInParent<Health>() || hit.collider.transform.root != player.root)
                return false;
        }
        return true;
    }

    void FireOnce()
    {
        if (!enableShooting) return;

        if (animator && !string.IsNullOrEmpty(fireTrig)) animator.SetTrigger(fireTrig);
        if (muzzleFlash) muzzleFlash.Play(true);

        // --- SES: güvenli çalma ---
        bool played = false;
        if (shotAudio && shotAudio.enabled && shotAudio.gameObject.activeInHierarchy)
        {
            shotAudio.Play();
            played = true;
        }
        if (!played && _fallbackAS)
        {
            var clip = (shotAudio && shotAudio.clip) ? shotAudio.clip : shotClip;
            if (clip) _fallbackAS.PlayOneShot(clip);
        }

        Vector3 origin = muzzle ? muzzle.position
                       : (eye ? eye.position : transform.position + Vector3.up * 1.5f);

        Vector3 toPlayer = ((playerHead ? playerHead.position : player.position) - origin).normalized;
        Vector3 dir = ApplySpread(toPlayer, spreadDeg);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 200f, hitMask, QueryTriggerInteraction.Ignore))
        {
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(damage);

            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(dir * hitForce, hit.point, ForceMode.Impulse);

            if (impactVfxPrefab)
            {
                var fx = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 2.5f);
            }
        }
    }

    static Vector3 ApplySpread(Vector3 dir, float degrees)
    {
        if (degrees <= 0.001f) return dir;
        Quaternion q = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up)
                     * Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.right);
        return (q * dir).normalized;
    }

    // ----------------- UTIL -----------------
    static Transform FindDeep(Transform root, string namePart)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(namePart)) return t;
        return null;
    }

    void AutoBindAudioFromWeapon()
    {
        // Sahneden doðru AudioSource’u bul ve aç
        if (!shotAudio && rifleInstance)
            shotAudio = rifleInstance.GetComponentInChildren<AudioSource>(true);

        if (shotAudio && !shotAudio.enabled) shotAudio.enabled = true;
        if (_fallbackAS) { _fallbackAS.spatialBlend = 1f; _fallbackAS.playOnAwake = false; }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, .7f, 1f, .25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, .4f, 0f, .25f);
        Gizmos.DrawWireSphere(transform.position, fireRange);
    }
}
