using UnityEngine;

[DisallowMultipleComponent]
public class WeaponAttachment : MonoBehaviour
{
    [Header("Grip override")]
    [Tooltip("Eðer prefab içinde özel bir grip objesi varsa buraya ismini yaz (ör: Grip_R). Boþsa default Grip aranýr.")]
    public string gripName = "Grip_R";

    [Header("Local offsets applied AFTER grip alignment")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localRotationOffsetEuler = Vector3.zero;

    [Header("Optional: left-hand target prefab inside this weapon (for 2-handed)")]
    public Transform leftHandTargetPrefab;
}
