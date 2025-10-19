using UnityEngine;

public class TPSCameraSimple : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                // Player (parent)

    [Header("Pivot & Distance")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f); // baþ hizasý
    public float distance = 2.2f;           // Inspector deðeri, Start'ta sahneden yakalanýr
    public float minDistance = 1.4f;
    public float maxDistance = 5f;

    [Header("Mouse (Old Input)")]
    public float sensX = 120f;              // deg/sec
    public float sensY = 100f;
    public float minPitch = -50f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Smoothing")]
    public float rotateLerp = 0f;           // TESTTE 0: hiç kayma yok. Beðenince 10–16 yap
    public float distanceLerp = 0f;         // TESTTE 0: hiç kayma yok. Beðenince 10–16 yap

    float yaw, pitch;        // hedef açý
    float yawSm, pitchSm;    // smooth açý
    float distSm;

    void Start()
    {
        if (!target) { Debug.LogWarning("[TPSCameraSimple] Target yok."); enabled = false; return; }

        // --- SAHNEDEKÝ MEVCUT KONU/AÇIYI YAKALA (en kritik kýsým) ---
        Vector3 pivot = target.position + pivotOffset;
        Vector3 toCam = transform.position - pivot;
        if (toCam.sqrMagnitude < 0.0001f) toCam = new Vector3(0, 0, -distance);

        // Mevcut sahne dönüþünden yaw/pitch hesapla
        Quaternion look = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        Vector3 e = look.eulerAngles;
        yaw = e.y;
        pitch = (e.x > 180f) ? e.x - 360f : e.x;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // *** Inspector'daki distance yerine SAHNEDEN ölçülen mesafeyi baz al ***
        distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);

        // Smooth buffer'larýný da anýnda eþitle
        yawSm = yaw;
        pitchSm = pitch;
        distSm = distance;

        // Ýstersen kilitle, istemezsen kapatabilirsin
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * sensX * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * sensY * Time.deltaTime * (invertY ? 1f : -1f);

        yaw = Mathf.Repeat(yaw + mx, 360f);
        pitch = Mathf.Clamp(pitch + my, minPitch, maxPitch);

        // (Ýstersen zoom aç: distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel")*2f, minDistance, maxDistance);)
    }

    void LateUpdate()
    {
        if (!target) return;

        // exp-lerp hissi (0 ise anýnda)
        float rt = rotateLerp <= 0f ? 1f : (1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
        yawSm = Mathf.LerpAngle(yawSm, yaw, rt);
        pitchSm = Mathf.LerpAngle(pitchSm, pitch, rt);

        float dt = distanceLerp <= 0f ? 1f : (1f - Mathf.Exp(-distanceLerp * Time.deltaTime));
        distSm = Mathf.Lerp(distSm, distance, dt);

        Quaternion rot = Quaternion.Euler(pitchSm, yawSm, 0f);
        Vector3 pivot = target.position + pivotOffset;

        transform.position = pivot - rot * Vector3.forward * distSm;
        transform.rotation = rot;
    }

    // Editörde "tam þu anki açýyý/mesafeyi" yakalamak için sað týk menüsü
    [ContextMenu("Capture From Current Transform")]
    void CaptureFromCurrentTransform()
    {
        if (!target) return;
        Vector3 pivot = target.position + pivotOffset;
        Vector3 toCam = transform.position - pivot;

        distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);

        Quaternion look = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        Vector3 e = look.eulerAngles;
        yaw = e.y;
        pitch = (e.x > 180f) ? e.x - 360f : e.x;
        yawSm = yaw; pitchSm = pitch; distSm = distance;

        Debug.Log($"[TPSCameraSimple] Captured: dist={distance:F2}, yaw={yaw:F1}, pitch={pitch:F1}");
    }
}
