using UnityEngine;

public class DiagnoseEquip : MonoBehaviour
{
    [Header("Optional")]
    public GameObject pistolPrefab;      // Ýstersen gerçek pistol prefabýný sürükle
    public bool useGripAlignment = true; // Prefab içinde "Grip" varsa otomatik hizalar

    Transform rightHand;
    Transform rightHandMount;
    GameObject equipped;

    void Start()
    {
        // 1) Animator ve RightHand'ý bul
        var animator = GetComponentInChildren<Animator>(true);
        if (!animator)
        {
            Debug.LogError("[DiagnoseEquip] Animator bulunamadý! Player objesinde Animator yok.");
            return;
        }
        if (!animator.isHuman)
        {
            Debug.LogError("[DiagnoseEquip] Animator var ama Humanoid deðil. FBX Rig'i Humanoid yap.");
            return;
        }

        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (!rightHand)
        {
            Debug.LogError("[DiagnoseEquip] HumanBodyBones.RightHand bulunamadý.");
            return;
        }
        Debug.Log($"[DiagnoseEquip] RightHand bulundu: {PathOf(rightHand)}");

        // 2) Mount yoksa yarat
        rightHandMount = rightHand.Find("RightHand_Mount");
        if (!rightHandMount)
        {
            rightHandMount = new GameObject("RightHand_Mount").transform;
            rightHandMount.SetParent(rightHand, false);
            rightHandMount.localPosition = Vector3.zero;
            rightHandMount.localRotation = Quaternion.identity;
            rightHandMount.localScale = Vector3.one;
            Debug.Log("[DiagnoseEquip] RightHand_Mount eksikti, otomatik oluþturuldu.");
        }
        else
        {
            Debug.Log($"[DiagnoseEquip] RightHand_Mount var: {PathOf(rightHandMount)}");
        }

        // 3) Silah instance'ýný oluþtur (prefab varsa onu, yoksa test küpü)
        if (pistolPrefab)
        {
            equipped = Instantiate(pistolPrefab);
            Debug.Log("[DiagnoseEquip] Prefab'tan pistol instance oluþturuldu.");
        }
        else
        {
            equipped = GameObject.CreatePrimitive(PrimitiveType.Cube);
            equipped.name = "PistolPlaceholder";
            equipped.transform.localScale = new Vector3(0.03f, 0.12f, 0.2f);
            var col = equipped.GetComponent<Collider>(); if (col) col.enabled = false;
            Debug.LogWarning("[DiagnoseEquip] pistolPrefab atanmadý; placeholder küp spawn edildi.");
        }

        // 4) Parent et ve hizala
        AttachToMount(equipped.transform, rightHandMount);

        Debug.Log("[DiagnoseEquip] BÝTTÝ: Hierarchy'de RightHand_Mount altýnda bir child görmelisin. " +
                  "Koþ/ yürü—silah eli takip etmeli.");
    }

    void AttachToMount(Transform t, Transform mount)
    {
        // parent et (world pose korumadan)
        t.SetParent(mount, false);

        // Grip hizalamasý
        if (useGripAlignment)
        {
            var grip = t.Find("Grip");
            if (grip)
            {
                t.localRotation *= Quaternion.Inverse(grip.localRotation);
                t.localPosition -= grip.localPosition;
                Debug.Log("[DiagnoseEquip] Grip bulundu, otomatik hizalama uygulandý.");
            }
            else
            {
                Debug.LogWarning("[DiagnoseEquip] Prefab içinde 'Grip' bulunamadý. " +
                                 "Mount'a local (0,0,0) ile oturtuldu; ince ayarý Mount/Prefab'ta yap.");
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
            }
        }
        else
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }

        t.localScale = Vector3.one;
    }

    string PathOf(Transform tr)
    {
        string path = tr.name;
        var p = tr.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    // Mount'u sahnede görmek için
    void OnDrawGizmosSelected()
    {
        if (!rightHandMount) return;
        Gizmos.matrix = rightHandMount.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.02f);
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.1f);
        Gizmos.DrawRay(Vector3.zero, Vector3.up * 0.1f);
        Gizmos.DrawRay(Vector3.zero, Vector3.right * 0.1f);
    }
}
