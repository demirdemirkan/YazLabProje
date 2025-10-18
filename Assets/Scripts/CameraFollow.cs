using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // Player (parent)

    [Header("Follow")]
    public bool autoOffset = true;     // Editörde koyduðun konumu baz alsýn
    public Vector3 offset = new Vector3(0f, 2f, -3f);
    public float followLerp = 10f;     // 0=anýnda, 8-12=yumuþak

    [Header("Rotation")]
    public bool lockRotation = true;   // Kameranýn þimdiki açýsýný koru
    public bool lookAtTarget = false;  // Ýstersen hedefe baktýr (FPS deðil, TPS hissi)

    Quaternion initialRot;

    void Start()
    {
        if (!target)
        {
            Debug.LogWarning("[CameraFollow] Target atanmadý.");
            enabled = false;
            return;
        }

        if (autoOffset)
            offset = transform.position - target.position; // þimdiki konumu yakala

        if (lockRotation)
            initialRot = transform.rotation;               // þimdiki açýyý kilitle
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desired = target.position + offset;

        if (followLerp <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);

        if (lockRotation)
            transform.rotation = initialRot;
        else if (lookAtTarget)
            transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    // Editörden offset'i tek týkla yakalamak için:
    [ContextMenu("Capture Offset From Current Position")]
    void CaptureOffsetFromCurrent()
    {
        if (!target) return;
        offset = transform.position - target.position;
        Debug.Log($"[CameraFollow] Captured offset: {offset}");
    }
}
