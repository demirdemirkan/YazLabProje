using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(WeaponEquip))]
public class WeaponSwitcher : MonoBehaviour
{
    [Tooltip("1..n tuşlarına atanacak silah prefab'ları (index 0 -> tuş 1).")]
    public List<GameObject> weaponPrefabs = new List<GameObject>(); // set in inspector

    [Tooltip("Varsayılan dinlenen tuşlar (1-4). Eğer daha az prefab varsa fazlalık göz ardı edilir.)")]
    public List<KeyCode> weaponKeys = new List<KeyCode>
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4
    };

    [Tooltip("Aynı tuşa tekrar basınca silahı kapat (toggle).")]
    public bool toggleOffOnSameKey = true;

    private WeaponEquip _equip;
    private int _currentIndex = -1; // -1 = el boş

    void Awake()
    {
        _equip = GetComponent<WeaponEquip>();
        if (_equip == null) Debug.LogError("[WeaponSwitcher] WeaponEquip component bulunamadı! (aynı GameObject'te olmalı)");
    }

    void Update()
    {
        // limit: sadece kaç prefab varsa o kadar tuşu dinle
        int keyCount = Mathf.Min(weaponKeys.Count, weaponPrefabs.Count);

        for (int i = 0; i < keyCount; i++)
        {
            if (Input.GetKeyDown(weaponKeys[i]))
            {
                HandleSlotKey(i);
            }
        }
    }

    private void HandleSlotKey(int slotIndex)
    {
        // safety
        if (_equip == null)
        {
            Debug.LogWarning("[WeaponSwitcher] WeaponEquip referansı yok.");
            return;
        }

        if (slotIndex < 0 || slotIndex >= weaponPrefabs.Count)
        {
            Debug.LogWarning($"[WeaponSwitcher] Slot {slotIndex + 1} out of range.");
            return;
        }

        var prefab = weaponPrefabs[slotIndex];
        if (prefab == null)
        {
            Debug.LogWarning($"[WeaponSwitcher] Slot {slotIndex + 1} prefab atanmadı.");
            return;
        }

        // Eğer aynı slot zaten takılıysa -> toggle ile bırak
        if (toggleOffOnSameKey && _currentIndex == slotIndex)
        {
            _equip.ForceUnequip();
            _currentIndex = -1;
            Debug.Log($"[WeaponSwitcher] Slot {slotIndex + 1} tekrar basıldı -> UNEQUIP");
            return;
        }

        // Aksi halde o slotu tak
        _equip.EquipPrefab(prefab);
        _currentIndex = slotIndex;
        Debug.Log($"[WeaponSwitcher] Slot {slotIndex + 1} selected -> Equipped {prefab.name}");
    }

    // yardımcı: dışarıdan da slot seçmek istersen
    public void SelectSlot(int slotIndex)
    {
        HandleSlotKey(slotIndex);
    }

    public int GetCurrentIndex() => _currentIndex;
}
