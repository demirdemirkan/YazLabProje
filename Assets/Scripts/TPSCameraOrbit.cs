using UnityEngine;

public class TPSCameraOrbit : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;          // Player (parent)
    public Transform model;           // (opsiy.) görsel child

    [Header("Pivot & Distance")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 2.2f;
    public float minDistance = 1.5f;
    public float maxDistance = 5.0f;

    [Header("Shoulder")]
    public float sideOffset = 0.7f;   // +sað omuz, -sol omuz
    public KeyCode shoulderSwapKey = KeyCode.V; // omuz deðiþtir

    [Header("Look (Mouse)")]
    public float sensX = 120f;        // deg/sec
    public float sensY = 100f;
    public float deadzone = 0.03f;    // küçük hareketleri yut
    public float minPitch = -55f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Smoothing")]
    public float rotateLerp = 12f;    // 0 = anýnda
    public float distanceLerp = 12f;
    public float sideLerp = 18f;      // omuz geçiþ hýzý

    [Header("Aim / Align")]
    public bool centerWhenAiming = true;     // RMB basýlýyken omuz = 0
    public bool alignPlayerOnAim = true;     // RMB basýlýyken oyuncuyu yaw’a hizala
    public KeyCode aimKey = KeyCode.Mouse1;

    [Header("Recenter While Moving")]
    public bool recenterOnMove = true;
    public float recenterDelay = 0.35f;      // input kesilince bekleme
    public float recenterSpeed = 120f;       // deg/sec (ne kadar hýzlý arkaya gelsin)

    [Header("Collision")]
    public LayerMask collisionMask = ~0;     // çevre layer’ý
    public float collisionRadius = 0.2f;     // kamera küresi
    public float collisionBuffer = 0.1f;     // duvardan pay

    float yaw, pitch;                  // hedef açý
    float yawSm, pitchSm;              // smooth açý
    float distSm;                      // smooth mesafe
    float sideSm;                      // smooth omuz
    float noInputTimer;

    void Start()
    {
        if (!target) { Debug.LogWarning("[TPSCameraOrbit] target yok."); enabled = false; return; }

        // Editördeki baþlangýç pozundan açýlarý al
        Vector3 pivot = target.position + pivotOffset;
        Vector3 toCam = transform.position - pivot;
        if (toCam.sqrMagnitude < 0.001f) toCam = new Vector3(0, 0, -distance);

        Quaternion startRot = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        Vector3 e = startRot.eulerAngles;
        yaw = e.y;
        pitch = NormalizePitch(e.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        yawSm = yaw; pitchSm = pitch;
        distSm = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);
        sideSm = sideOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // omuz deðiþtir
        if (Input.GetKeyDown(shoulderSwapKey)) sideOffset = -sideOffset;

        // mouse giriþleri
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // ölü bölge
        if (Mathf.Abs(mx) < deadzone) mx = 0f;
        if (Mathf.Abs(my) < deadzone) my = 0f;

        yaw = Mathf.Repeat(yaw + mx * sensX * Time.deltaTime, 360f);
        float dy = my * sensY * Time.deltaTime * (invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch + dy, minPitch, maxPitch);

        // scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance - scroll * 2.0f, minDistance, maxDistance);

        // hareket var mý? (WASD)
        bool moving =
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (moving) noInputTimer = 0f; else noInputTimer += Time.deltaTime;

        // hareket ederken (aim deðilken) kamerayý yumuþakça arkaya al
        if (recenterOnMove && moving && !Input.GetKey(aimKey))
        {
            float targetYaw = target.eulerAngles.y; // Player’ýn yönü
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, recenterSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // yumuþatma
        float rt = (rotateLerp <= 0f) ? 1f : (1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
        yawSm = Mathf.LerpAngle(yawSm, yaw, rt);
        pitchSm = Mathf.LerpAngle(pitchSm, pitch, rt);

        float dt = (distanceLerp <= 0f) ? 1f : (1f - Mathf.Exp(-distanceLerp * Time.deltaTime));
        distSm = Mathf.Lerp(distSm, distance, dt);

        float st = (sideLerp <= 0f) ? 1f : (1f - Mathf.Exp(-sideLerp * Time.deltaTime));
        float sideTarget = 0f; // her durumda merkez arkasý

        sideSm = Mathf.Lerp(sideSm, sideTarget, st);

        // rot & pivot
        Quaternion rot = Quaternion.Euler(pitchSm, yawSm, 0f);
        Vector3 pivot = target.position + pivotOffset;

        // istenen pozisyon
        Vector3 desired = pivot
                        - rot * Vector3.forward * distSm
                        + rot * Vector3.right * sideSm;

        // çarpýþma: pivot->desired arasý spherecast ile kýsalt
        Vector3 finalPos = desired;
        Vector3 dir = (desired - pivot);
        float len = dir.magnitude;
        if (len > 0.001f)
        {
            dir /= len;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, len, collisionMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = hit.point - dir * collisionBuffer;
            }
        }

        transform.position = finalPos;
        transform.rotation = rot;

        // aim’de karakteri kamera yaw’una hizala (isteðe baðlý)
        if (alignPlayerOnAim && Input.GetKey(aimKey))
        {
            Quaternion targetYaw = Quaternion.Euler(0f, yawSm, 0f);
            target.rotation = targetYaw;
            if (model) model.rotation = Quaternion.Slerp(model.rotation, targetYaw, rt);
        }
    }

    static float NormalizePitch(float x) { if (x > 180f) x -= 360f; return x; }
}
