using UnityEngine;

[DisallowMultipleComponent]
public class WeaponEquip : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sağ el için mount (RightHand_Mount). Boşsa Awake'te adından bulmayı dener.")]
    public Transform rightHandMount;

    [Tooltip("Varsayılan tek tuşla tak/çıkar için kullanılacak prefab (opsiyonel).")]
    public GameObject pistolPrefab;

    [Header("Options")]
    [Tooltip("Grip/Grip_R (veya WeaponAttachment.gripName) ile hizalama yap.")]
    public bool useGripAlignment = true;
    public bool debugLogs = true;

    [Header("Global Calibration Offsets (tüm silahlara eklenir)")]
    public Vector3 positionOffset = Vector3.zero;     // mm-cm arası düzeltme
    public Vector3 rotationOffsetEuler = Vector3.zero; // derecelerle düzeltme

    [Header("Rotation Lock")]
    [Tooltip("Her LateUpdate'te local poz/rot’u koru (animler bozarsa aç).")]
    public bool maintainLocalRotationEveryFrame = false;

    [Header("Input (Legacy Input)")]
    [Tooltip("WeaponEquip kendi içinde tuşla tak/çıkar yapsın (Switcher yoksa).")]
    public bool enableLocalToggle = false;
    public KeyCode toggleKey = KeyCode.None;
    public KeyCode altToggleKey = KeyCode.None;

    [Header("Aim State / Input (opsiyonel)")]
    public bool isAiming = false;
    public bool handleAimInput = true;
    public KeyCode aimToggleMouse = KeyCode.Mouse1;
    [Tooltip("Nişanda yaw’a ekstra düzeltme (örn. -90 ya da 90).")]
    public float aimYawFix = 0f;

    private GameObject currentPistol;   // eldeki instance
    private bool isEquipped;

    // ---------- LIFECYCLE ----------
    void Awake()
    {
        // Mount yoksa bul
        if (!rightHandMount)
        {
            rightHandMount = FindDeep(transform, "RightHand_Mount");
            if (!rightHandMount) rightHandMount = FindDeep(transform, "RightHand");
            if (debugLogs && rightHandMount) Debug.Log("[WeaponEquip] Mount bulundu: " + rightHandMount.name);
        }

        // Varsayılan silahsız başla
        currentPistol = null; isEquipped = false;
        if (rightHandMount)
        {
            for (int i = 0; i < rightHandMount.childCount; i++)
                rightHandMount.GetChild(i).gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Kendi toggle'ı (ops.)
        if (enableLocalToggle)
        {
            bool togglePressed =
                (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey)) ||
                (altToggleKey != KeyCode.None && Input.GetKeyDown(altToggleKey));

            if (togglePressed)
            {
                if (isEquipped) Unequip();
                else Equip();
            }
        }

        // Aim toggle (RMB)
        if (handleAimInput && Input.GetKeyDown(aimToggleMouse))
        {
            isAiming = !isAiming;
            if (debugLogs) Debug.Log("[WeaponEquip] isAiming = " + isAiming);
        }
    }

    void LateUpdate()
    {
        if (maintainLocalRotationEveryFrame && isEquipped && currentPistol != null)
        {
            var t = currentPistol.transform;
            t.localPosition = positionOffset;
            t.localRotation = Quaternion.Euler(rotationOffsetEuler) *
                              (isAiming ? Quaternion.Euler(0f, aimYawFix, 0f) : Quaternion.identity);
        }
    }

    // ---------- PUBLIC BUTTONS ----------
    [ContextMenu("Force Equip")] public void ForceEquip() => Equip();
    [ContextMenu("Force Unequip")] public void ForceUnequip() => Unequip();

    [ContextMenu("Debug State")]
    public void DebugState()
    {
        int cc = rightHandMount ? rightHandMount.childCount : -1;
        Debug.Log($"[WeaponEquip] isEquipped={isEquipped}, isAiming={isAiming}, current={(currentPistol ? currentPistol.name : "NULL")}, mountChildren={cc}");
        if (rightHandMount)
            for (int i = 0; i < rightHandMount.childCount; i++)
                Debug.Log($"  - child[{i}] {rightHandMount.GetChild(i).name} (active={rightHandMount.GetChild(i).gameObject.activeSelf})");
    }

    // ---------- CORE (tek prefab akışı) ----------
    private void Equip()
    {
        if (!rightHandMount) { if (debugLogs) Debug.LogWarning("[WeaponEquip] Mount yok."); return; }
        if (!pistolPrefab) { if (debugLogs) Debug.LogWarning("[WeaponEquip] 'pistolPrefab' atanmadı."); return; }

        // Eldeki yoksa var olan child’dan al ya da instantiate et
        if (currentPistol == null)
        {
            Transform pick = null;
            for (int i = 0; i < rightHandMount.childCount; i++)
            {
                var ch = rightHandMount.GetChild(i);
                if (ch.name.StartsWith(pistolPrefab.name)) { pick = ch; break; }
            }
            if (!pick && rightHandMount.childCount > 0)
                pick = rightHandMount.GetChild(rightHandMount.childCount - 1);

            if (pick) currentPistol = pick.gameObject;
        }
        if (currentPistol == null)
        {
            currentPistol = Instantiate(pistolPrefab);
            if (debugLogs) Debug.Log("[WeaponEquip] Instantiate: " + currentPistol.name);
        }

        AttachToMountAndCalibrate(currentPistol.transform);
        currentPistol.SetActive(true);
        isEquipped = true;
    }

    public void Unequip()
    {
        if (rightHandMount)
            for (int i = 0; i < rightHandMount.childCount; i++)
                rightHandMount.GetChild(i).gameObject.SetActive(false);

        if (currentPistol != null) currentPistol.SetActive(false);
        isEquipped = false;
    }

    /// <summary> Dışarıdan başka bir prefab takmak için. </summary>
    public void EquipPrefab(GameObject weaponPrefab)
    {
        if (!rightHandMount) { if (debugLogs) Debug.LogWarning("[WeaponEquip] EquipPrefab: Mount yok."); return; }
        if (!weaponPrefab) { if (debugLogs) Debug.LogWarning("[WeaponEquip] EquipPrefab: Prefab NULL."); return; }

        if (currentPistol != null) { Destroy(currentPistol); currentPistol = null; }

        currentPistol = Instantiate(weaponPrefab);
        if (debugLogs) Debug.Log("[WeaponEquip] Instantiate via EquipPrefab: " + currentPistol.name);

        AttachToMountAndCalibrate(currentPistol.transform);
        currentPistol.SetActive(true);
        isEquipped = true;
    }

    // ---------- INTERNAL ----------
    private void AttachToMountAndCalibrate(Transform weaponT)
    {
        weaponT.SetParent(rightHandMount, worldPositionStays: false);

        // 1) Grip seçimi: WeaponAttachment.gripName > "Grip" > "Grip_R"
        Transform grip = null;
        WeaponAttachment attach = weaponT.GetComponent<WeaponAttachment>();
        if (useGripAlignment)
        {
            if (attach != null && !string.IsNullOrEmpty(attach.gripName))
                grip = FindDeep(weaponT, attach.gripName);
            if (!grip) grip = weaponT.Find("Grip");
            if (!grip) grip = FindDeep(weaponT, "Grip_R");
        }

        // 2) Grip’e göre hizala
        bool aligned = false;
        if (useGripAlignment && grip)
        {
            weaponT.localRotation *= Quaternion.Inverse(grip.localRotation);
            weaponT.localPosition -= grip.localPosition;
            aligned = true;
        }
        else if (!aligned)
        {
            weaponT.localPosition = Vector3.zero;
            weaponT.localRotation = Quaternion.identity;
        }

        // 3) Global kalibrasyon
        weaponT.localPosition += positionOffset;
        weaponT.localRotation *= Quaternion.Euler(rotationOffsetEuler);

        // 4) Prefab’a özel ek offset (WeaponAttachment)
        if (attach != null)
        {
            weaponT.localPosition += attach.localPositionOffset;
            weaponT.localRotation *= Quaternion.Euler(attach.localRotationOffsetEuler);
        }

        // 5) Ölçek
        weaponT.localScale = Vector3.one;

        // 6) Aim’de ekstra yaw düzeltmesi isteniyorsa (maintainLocalRotationEveryFrame kapalıyken de çalışsın)
        if (isAiming && Mathf.Abs(aimYawFix) > 0.001f)
            weaponT.localRotation *= Quaternion.Euler(0f, aimYawFix, 0f);
    }

    // ---------- HELPERS ----------
    private static Transform FindDeep(Transform root, string namePart)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(namePart)) return t;
        return null;
    }
}
