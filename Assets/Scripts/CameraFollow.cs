using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // Player (parent)

    [Header("Follow")]
    public bool autoOffset = true;     // Edit�rde koydu�un konumu baz als�n
    public Vector3 offset = new Vector3(0f, 2f, -3f);
    public float followLerp = 10f;     // 0=an�nda, 8-12=yumu�ak

    [Header("Rotation")]
    public bool lockRotation = true;   // Kameran�n �imdiki a��s�n� koru
    public bool lookAtTarget = false;  // �stersen hedefe bakt�r (FPS de�il, TPS hissi)

    Quaternion initialRot;

    void Start()
    {
        if (!target)
        {
            Debug.LogWarning("[CameraFollow] Target atanmad�.");
            enabled = false;
            return;
        }

        if (autoOffset)
            offset = transform.position - target.position; // �imdiki konumu yakala

        if (lockRotation)
            initialRot = transform.rotation;               // �imdiki a��y� kilitle
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

    // Edit�rden offset'i tek t�kla yakalamak i�in:
    [ContextMenu("Capture Offset From Current Position")]
    void CaptureOffsetFromCurrent()
    {
        if (!target) return;
        offset = transform.position - target.position;
        Debug.Log($"[CameraFollow] Captured offset: {offset}");
    }
}
