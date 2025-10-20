using UnityEngine;

public class WeaponEquip : MonoBehaviour
{
    [Header("References")]
    public Transform rightHandMount;     // RightHand_Mount'u sürükle
    public GameObject pistolPrefab;      // Pistol prefabını sürükle (içinde "Grip" varsa şahane)

    [Header("Options")]
    public bool useGripAlignment = true;   // Pistol içinde "Grip" varsa otomatik hizala
    public bool maintainWorldPose = false; // Parent ederken dünya pozunu koru (genelde false)

    [Header("Input")]
    public KeyCode equipKey = KeyCode.Q;         // Q = TAK
    public KeyCode unequipKey = KeyCode.Alpha1;  // 1 = BIRAK

    GameObject currentPistol;

    void Update()
    {
        if (Input.GetKeyDown(equipKey))
            EquipFromPrefab();   // Q: tak

        if (Input.GetKeyDown(unequipKey))
            Unequip();           // 1: bırak
    }

    [ContextMenu("Equip From Prefab")]
    public void EquipFromPrefab()
    {
        if (!rightHandMount)
        {
            Debug.LogError("[WeaponEquip] rightHandMount atanmadı.");
            return;
        }
        if (!pistolPrefab)
        {
            Debug.LogError("[WeaponEquip] pistolPrefab atanmadı.");
            return;
        }

        // önce varsa çıkar
        Unequip();

        // oluştur ve parent et
        currentPistol = Instantiate(pistolPrefab);
        AttachToMount(currentPistol.transform);
        Debug.Log("[WeaponEquip] Pistol equipped from prefab.");
    }

    public void EquipExisting(Transform pistolInScene)
    {
        if (!pistolInScene) { Debug.LogError("[WeaponEquip] Geçersiz pistol referansı."); return; }
        Unequip();
        currentPistol = pistolInScene.gameObject;
        AttachToMount(pistolInScene);
        Debug.Log("[WeaponEquip] Existing pistol equipped.");
    }

    // --- Güçlendirilmiş Unequip ---
    [ContextMenu("Unequip")]
    public void Unequip()
    {
        // 1) currentPistol varsa onu yok et
        if (currentPistol != null)
        {
            Destroy(currentPistol);
            currentPistol = null;
            Debug.Log("[WeaponEquip] Pistol unequipped (currentPistol).");
            return;
        }

        // 2) currentPistol null ise: mount altında kalmış çocuk var mı?
        if (rightHandMount && rightHandMount.childCount > 0)
        {
            Transform target = null;

            // prefab adıyla başlayan child'ı bulmaya çalış (tercih)
            if (pistolPrefab)
            {
                for (int i = rightHandMount.childCount - 1; i >= 0; i--)
                {
                    var ch = rightHandMount.GetChild(i);
                    if (ch.name.StartsWith(pistolPrefab.name))
                    {
                        target = ch; break;
                    }
                }
            }

            // bulunamazsa son çocuğu sil (fallback)
            if (target == null)
                target = rightHandMount.GetChild(rightHandMount.childCount - 1);

            Destroy(target.gameObject);
            Debug.Log("[WeaponEquip] Pistol unequipped (fallback child).");
        }
        else
        {
            Debug.Log("[WeaponEquip] Unequip: silinecek bir şey yok.");
        }
    }

    void AttachToMount(Transform pistolT)
    {
        pistolT.SetParent(rightHandMount, worldPositionStays: maintainWorldPose);

        if (useGripAlignment)
        {
            var grip = pistolT.Find("Grip");
            if (grip)
            {
                // Grip ofsetini tersleyerek mount'a sıfırla
                pistolT.localRotation *= Quaternion.Inverse(grip.localRotation);
                pistolT.localPosition -= grip.localPosition;
            }
        }

        pistolT.localScale = Vector3.one;

        if (!useGripAlignment || !pistolT.Find("Grip"))
        {
            pistolT.localPosition = Vector3.zero;
            pistolT.localRotation = Quaternion.identity;
        }
    }

    // --- Teşhis yardımcıları ---
    [ContextMenu("Debug State")]
    public void DebugState()
    {
        int childCount = rightHandMount ? rightHandMount.childCount : -1;
        Debug.Log($"[WeaponEquip] currentPistol={(currentPistol ? currentPistol.name : "NULL")}, rightHandMount children={childCount}");
        if (rightHandMount)
        {
            for (int i = 0; i < rightHandMount.childCount; i++)
                Debug.Log($"  - child[{i}] = {rightHandMount.GetChild(i).name}");
        }
    }

    [ContextMenu("Force Unequip (All Children)")]
    public void ForceUnequipAllChildren()
    {
        if (!rightHandMount) { Debug.LogWarning("[WeaponEquip] rightHandMount yok."); return; }
        for (int i = rightHandMount.childCount - 1; i >= 0; i--)
            Destroy(rightHandMount.GetChild(i).gameObject);
        currentPistol = null;
        Debug.Log("[WeaponEquip] FORCE: mount altındaki TÜM çocuklar silindi.");
    }

    void OnDrawGizmosSelected()
    {
        if (!rightHandMount) return;
        Gizmos.matrix = rightHandMount.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.02f);
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.1f); // Z ileri
        Gizmos.DrawRay(Vector3.zero, Vector3.up * 0.1f);      // Y yukarı
        Gizmos.DrawRay(Vector3.zero, Vector3.right * 0.1f);   // X sağ
    }
}
