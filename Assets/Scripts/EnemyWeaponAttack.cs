using UnityEngine;

[DisallowMultipleComponent]
public class EnemyWeaponAttack : MonoBehaviour
{
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
    }

    void Start()
    {
        EquipNow();
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
    }

    static Transform FindDeep(Transform root, string namePart)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(namePart)) return t;
        return null;
    }
}
