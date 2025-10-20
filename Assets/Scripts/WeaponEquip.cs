using UnityEngine;

[DisallowMultipleComponent]
public class WeaponEquip : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sağ el için mount (RightHand_Mount). Boşsa Awake'te adından bulmayı dener.")]
    public Transform rightHandMount;

    [Tooltip("Pistol prefab (Grip boşluğu varsa hizalama yapılır).")]
    public GameObject pistolPrefab;

    [Header("Options")]
    public bool useGripAlignment = true;
    public bool maintainWorldPose = false;
    public bool debugLogs = true;

    [Header("Calibration Offsets")]
    [Tooltip("Mount'a göre elle düzelteceğin pozisyon ofseti (local).")]
    public Vector3 positionOffset = Vector3.zero;
    [Tooltip("Mount'a göre elle düzelteceğin rotasyon (Euler, local).")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Rotation Lock")]
    [Tooltip("true ise silah, mount tarafından her frame döndürülse bile local dönüşünü zorla sabitler.")]
    public bool maintainLocalRotationEveryFrame = false;

    [Header("Input (Legacy Input)")]
    public KeyCode toggleKey = KeyCode.Q;         // Q: tak/çıkar
    public KeyCode altToggleKey = KeyCode.Alpha1; // 1: alternatif toggle

    // >>> YENİ: Aim durumu ve RMB toggle ayarı
    [Header("Aim State / Input")]
    public bool isAiming = false;                 // Mevcut aim durumu
    public bool handleAimInput = true;            // Script RMB'yi kendi okusun mu?
    public KeyCode aimToggleMouse = KeyCode.Mouse1; // RMB
    public float aimYawFix = -90f;                // Aim açıkken ekstra Y düzeltmesi (gerekirse +90 yap)

    private GameObject currentPistol;   // Her zaman referans TUTULUR
    private bool isEquipped;            // Görünürlük durumu

    // ---------- LIFECYCLE ----------
    void Awake()
    {
        // Mount yoksa, isimden bulmayı dener
        if (!rightHandMount)
        {
            rightHandMount = FindDeep(transform, "RightHand_Mount");
            if (!rightHandMount) rightHandMount = FindDeep(transform, "RightHand");
            if (debugLogs && rightHandMount) Debug.Log("[WeaponEquip] Mount otomatik bulundu: " + rightHandMount.name);
        }

        // Sahnedeki mevcut çocukları sahiplen
        if (rightHandMount && rightHandMount.childCount > 0)
        {
            Transform pick = null;
            if (pistolPrefab)
            {
                for (int i = 0; i < rightHandMount.childCount; i++)
                {
                    var ch = rightHandMount.GetChild(i);
                    if (ch.name.StartsWith(pistolPrefab.name))
                    {
                        pick = ch; break;
                    }
                }
            }
            if (!pick) pick = rightHandMount.GetChild(rightHandMount.childCount - 1);

            currentPistol = pick.gameObject;
            isEquipped = currentPistol.activeSelf;
            if (debugLogs) Debug.Log("[WeaponEquip] Sahnedeki silah sahiplenildi: " + currentPistol.name + " (equipped=" + isEquipped + ")");
            if (isEquipped) ApplyCalibrationTo(currentPistol.transform);
        }
    }

    void Update()
    {
        // Equip/Unequip toggle (Q / 1)
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey))
        {
            if (isEquipped) Unequip();
            else Equip();
        }

        // >>> YENİ: RMB ile aim toggle
        if (handleAimInput && Input.GetKeyDown(aimToggleMouse))
        {
            isAiming = !isAiming;
            if (debugLogs) Debug.Log("[WeaponEquip] isAiming = " + isAiming);
        }
    }

    void LateUpdate()
    {
        // Her karede poz/rotasyonu kilitlemek istersen
        if (maintainLocalRotationEveryFrame && isEquipped && currentPistol != null)
        {
            // Pozisyonu zorla (local)
            currentPistol.transform.localPosition = positionOffset;

            // Aim açıkken ekstra yaw düzeltmesi uygula
            if (isAiming)
            {
                currentPistol.transform.localRotation =
                    Quaternion.Euler(rotationOffsetEuler) * Quaternion.Euler(0f, aimYawFix, 0f);
            }
            else
            {
                currentPistol.transform.localRotation = Quaternion.Euler(rotationOffsetEuler);
            }
        }
    }

    // ---------- PUBLIC BUTTONS ----------
    [ContextMenu("Force Equip")]
    public void ForceEquip() => Equip();

    [ContextMenu("Force Unequip")]
    public void ForceUnequip() => Unequip();

    [ContextMenu("Debug State")]
    public void DebugState()
    {
        int cc = rightHandMount ? rightHandMount.childCount : -1;
        Debug.Log($"[WeaponEquip] isEquipped={isEquipped}, isAiming={isAiming}, current={(currentPistol ? currentPistol.name : "NULL")}, mountChildren={cc}");
        if (rightHandMount)
        {
            for (int i = 0; i < rightHandMount.childCount; i++)
                Debug.Log($"  - child[{i}] {rightHandMount.GetChild(i).name} (active={rightHandMount.GetChild(i).gameObject.activeSelf})");
        }
    }

    // ---------- CORE ----------
    private void Equip()
    {
        if (!rightHandMount || !EnsurePrefabAssigned()) return;

        if (currentPistol == null)
        {
            currentPistol = Instantiate(pistolPrefab);
            if (debugLogs) Debug.Log("[WeaponEquip] Instantiate: " + currentPistol.name);
        }

        AttachToMount(currentPistol.transform);
        currentPistol.SetActive(true);
        isEquipped = true;

        if (debugLogs) Debug.Log("[WeaponEquip] EQUIPPED");
    }

    private void Unequip()
    {
        if (currentPistol == null)
        {
            if (rightHandMount && rightHandMount.childCount > 0)
            {
                currentPistol = rightHandMount.GetChild(rightHandMount.childCount - 1).gameObject;
            }
            else
            {
                if (debugLogs) Debug.Log("[WeaponEquip] Unequip: silah yok.");
                isEquipped = false;
                return;
            }
        }

        currentPistol.SetActive(false);
        isEquipped = false;
        if (debugLogs) Debug.Log("[WeaponEquip] UNEQUIPPED (SetActive false)");
    }

    private void AttachToMount(Transform pistolT)
    {
        pistolT.SetParent(rightHandMount, worldPositionStays: maintainWorldPose);

        bool aligned = false;
        if (useGripAlignment)
        {
            var grip = pistolT.Find("Grip");
            if (grip)
            {
                pistolT.localRotation *= Quaternion.Inverse(grip.localRotation);
                pistolT.localPosition -= grip.localPosition;
                aligned = true;
            }
        }

        if (!aligned)
        {
            pistolT.localPosition = Vector3.zero;
            pistolT.localRotation = Quaternion.identity;
        }

        // Kalibrasyon
        ApplyCalibrationTo(pistolT);

        pistolT.localScale = Vector3.one;
    }

    // ---------- HELPERS ----------
    private void ApplyCalibrationTo(Transform pistolT)
    {
        pistolT.localPosition += positionOffset;
        pistolT.localRotation *= Quaternion.Euler(rotationOffsetEuler);
    }

    private bool EnsurePrefabAssigned()
    {
        if (!pistolPrefab)
        {
            Debug.LogWarning("[WeaponEquip] 'pistolPrefab' atanmadı (Inspector).");
            return false;
        }
        return true;
    }

    private static Transform FindDeep(Transform root, string namePart)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(namePart)) return t;
        return null;
    }
}
