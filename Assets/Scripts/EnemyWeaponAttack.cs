using UnityEngine;

[DisallowMultipleComponent]
public class EnemyWeaponAttack : MonoBehaviour
{
    // ----------------- (SENÝN MEVCUT ALANLARIN) -----------------
    [Header("Weapon")]
    public GameObject riflePrefab;          // Tüfek prefabýný sürükle
    public string gripName = "Grip";        // Prefab içinde hizalama boþu (varsa)

    [Header("Fine Tune Offsets")]
    public Vector3 positionOffset;          // cm düzeltme
    public Vector3 rotationOffsetEuler;     // derece düzeltme

    [Header("Hand & Animator")]
    public bool useLeftHand = false;        // TRUE -> sol ele tak
    public Animator animator;               // boþsa otomatik bulunur
    [Tooltip("Animator'da rifle idle'a geçiren bool (kullanmayacaksan boþ býrak)")]
    public string rifleIdleBool = "";

    [Header("Tuning")]
    public bool liveApply = true;           // Play'de offset deðiþikliði anýnda uygulansýn
    public bool log = false;

    Transform handBone;
    Transform handMount;
    GameObject rifleInstance;

    // hizalama sonrasý baz deðer; liveApply'da üstüne offset ekleyeceðiz
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    // ----------------- (YENÝ: ATEÞ MODÜLÜ) -----------------
    [Header("== Shooting ==")]
    public bool enableShooting = true;            // Ýstersen kapat
    public Transform muzzle;                      // Silah prefabýndaki "Muzzle" boþunu sürükle (instancetan)
    public Transform eye;                         // Ray baþlangýcý (baþ/kafa); yoksa transform
    public Transform player;                      // Player transform (Tag=Player ise Awake'te bulunur)
    public float detectRange = 30f;               // Görünce niþana geç
    public float fireRange = 28f;               // Bu mesafede mermi at
    public float fireRate = 3.0f;              // sn baþýna mermi
    public float damage = 12f;               // mermi baþýna hasar
    public float spreadDeg = 1.2f;              // saçýlma (derece)
    public float hitForce = 6f;                // RB itiþi
    public float loseSightAfter = 1.5f;           // görüþ kaybý tamponu

    [Header("FX (opsiyonel)")]
    public AudioSource shotAudio;                 // Silah sesi
    public ParticleSystem muzzleFlash;            // Muzzle flash
    public GameObject impactVfxPrefab;           // Çarpma efekti

    [Header("Layers")]
    public LayerMask losMask = ~0;               // görüþ hattýný engelleyenler (duvar/zemin)
    public LayerMask hitMask = ~0;               // vurulabilecekler (Player katmaný dahil)

    [Header("Animator Params (opsiyonel)")]
    public string aimBool = "Aiming";            // true/false
    public string fireTrig = "Fire";             // trigger

    float fireCd;
    float lostTimer;
    Health playerHealth;
    Transform playerHead;

    // ----------------- LIFECYCLE -----------------
    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator || !animator.isHuman)
        {
            Debug.LogError("[EnemyWeaponAttach] Humanoid Animator bulunamadý.");
            enabled = false; return;
        }

        handBone = animator.GetBoneTransform(useLeftHand ? HumanBodyBones.LeftHand
                                                         : HumanBodyBones.RightHand);
        if (!handBone)
        {
            Debug.LogError("[EnemyWeaponAttach] El kemiði bulunamadý.");
            enabled = false; return;
        }

        // Mount yoksa oluþtur
        string mountName = useLeftHand ? "LeftHand_Mount" : "RightHand_Mount";
        handMount = handBone.Find(mountName);
        if (!handMount)
        {
            handMount = new GameObject(mountName).transform;
            handMount.SetParent(handBone, false);
            handMount.localPosition = Vector3.zero;
            handMount.localRotation = Quaternion.identity;
            handMount.localScale = Vector3.one;
            if (log) Debug.Log("[EnemyWeaponAttach] " + mountName + " oluþturuldu.");
        }

        // Player & göz referanslarý
        if (!player)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
        if (player)
        {
            playerHealth = player.GetComponentInParent<Health>();
            // oyuncunun baþýný bul (varsayýlan yoksa kendisi)
            playerHead = player.Find("Head") ? player.Find("Head") : player;
        }

        if (!eye)
        {
            // kafaya yakýn bir referans olarak gövde/baþ kemiði denenebilir
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            eye = headBone ? headBone : transform;
        }
    }

    void Start()
    {
        EquipNow(); // SENÝN orijinal akýþýn

        // silah instancelandýktan sonra muzzle'ý otomatik bulmayý dene
        if (!muzzle && rifleInstance)
        {
            muzzle = FindDeep(rifleInstance.transform, "Muzzle");
            if (!muzzle) muzzle = rifleInstance.transform; // fallback
        }
    }

    void Update()
    {
        if (!enableShooting || !player) return;

        fireCd -= Time.deltaTime;

        // mesafe + görüþ
        float dist = Vector3.Distance(transform.position, player.position);
        bool inDetect = dist <= detectRange;
        bool canSee = inDetect && HasLineOfSight();

        // aim state (Animator opsiyonel)
        bool aimState = canSee || (lostTimer > 0f);
        if (animator && !string.IsNullOrEmpty(aimBool))
            animator.SetBool(aimBool, aimState);

        // oyuncuya yumuþak bak (yatay)
        if (aimState)
        {
            Vector3 look = player.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(look);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 6f);
            }
        }

        // görüþ kaybý tamponu
        if (canSee) lostTimer = loseSightAfter;
        else lostTimer = Mathf.Max(0f, lostTimer - Time.deltaTime);

        // ateþ
        if (canSee && dist <= fireRange && fireCd <= 0f)
        {
            fireCd = 1f / Mathf.Max(0.1f, fireRate);
            FireOnce();
        }
    }

    void LateUpdate()
    {
        // Play sýrasýnda Inspector'dan offset deðiþtirince anlýk uygula
        if (liveApply && rifleInstance != null)
        {
            var t = rifleInstance.transform;
            t.localPosition = baseLocalPos + positionOffset;
            t.localRotation = baseLocalRot * Quaternion.Euler(rotationOffsetEuler);
        }
    }

    // ----------------- SENÝN ORÝJÝNAL METODUN + ufak ekler -----------------
    [ContextMenu("Reapply Now")]
    public void EquipNow()
    {
        if (!riflePrefab) { Debug.LogWarning("[EnemyWeaponAttach] riflePrefab boþ."); return; }

        // önce var olanlarý temizle
        for (int i = handMount.childCount - 1; i >= 0; i--)
            Destroy(handMount.GetChild(i).gameObject);

        rifleInstance = Instantiate(riflePrefab);
        var t = rifleInstance.transform;
        t.SetParent(handMount, false);

        // GRIP hizalamasý
        Transform grip = FindDeep(t, gripName);
        if (grip)
        {
            t.localRotation *= Quaternion.Inverse(grip.localRotation);
            t.localPosition -= grip.localPosition;
            if (log) Debug.Log("[EnemyWeaponAttach] Grip ile hizalandý.");
        }
        else
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            if (log) Debug.Log("[EnemyWeaponAttach] Grip bulunamadý, sýfýrlandý.");
        }

        // baz pozu kaydet (offset bu bazýn üstüne eklenecek)
        baseLocalPos = t.localPosition;
        baseLocalRot = t.localRotation;

        // ilk uygulama
        t.localPosition = baseLocalPos + positionOffset;
        t.localRotation = baseLocalRot * Quaternion.Euler(rotationOffsetEuler);
        t.localScale = Vector3.one;

        // physics kapat (elde sabit dursun)
        foreach (var c in rifleInstance.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in rifleInstance.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;

        // rifle idle bool opsiyonel
        if (!string.IsNullOrEmpty(rifleIdleBool) && animator)
            animator.SetBool(rifleIdleBool, true);

        // Muzzle otomatik bul (ilk kez takýnca)
        if (!muzzle) muzzle = FindDeep(t, "Muzzle");
    }

    // ----------------- SHOOTING ÝÇ METODLAR -----------------
    bool HasLineOfSight()
    {
        Vector3 origin = eye ? eye.position : transform.position + Vector3.up * 1.6f;
        Vector3 target = playerHead ? playerHead.position : player.position + Vector3.up * 1.5f;
        Vector3 dir = (target - origin).normalized;
        float d = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, d + 0.1f, losMask, QueryTriggerInteraction.Ignore))
        {
            // Ýlk çarpan Player deðilse engel var say
            if (!hit.collider.GetComponentInParent<Health>() || hit.collider.transform.root != player.root)
                return false;
        }
        return true;
    }

    void FireOnce()
    {
        if (!enableShooting) return;

        // anim/sfx/vfx
        if (animator && !string.IsNullOrEmpty(fireTrig)) animator.SetTrigger(fireTrig);
        if (muzzleFlash) muzzleFlash.Play(true);
        if (shotAudio) shotAudio.Play();

        Vector3 origin = muzzle ? muzzle.position
                       : (eye ? eye.position : transform.position + Vector3.up * 1.5f);

        Vector3 toPlayer = ((playerHead ? playerHead.position : player.position) - origin).normalized;
        Vector3 dir = ApplySpread(toPlayer, spreadDeg);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 200f, hitMask, QueryTriggerInteraction.Ignore))
        {
            // hasar
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(damage);

            // fizik
            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(dir * hitForce, hit.point, ForceMode.Impulse);

            // vfx
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

    // ----------------- YARDIMCILAR -----------------
    static Transform FindDeep(Transform root, string namePart)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(namePart)) return t;
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, .7f, 1f, .25f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, .4f, 0f, .25f);
        Gizmos.DrawWireSphere(transform.position, fireRange);
    }
}
