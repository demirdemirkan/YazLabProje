using UnityEngine;

public class TPSCameraOrbit : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;          // Player (parent)
    public Transform model;           // (opsiy.) g�rsel child

    [Header("Pivot & Distance")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 2.2f;
    public float minDistance = 1.5f;
    public float maxDistance = 5.0f;

    [Header("Shoulder")]
    public float sideOffset = 0.7f;   // +sa� omuz, -sol omuz
    public KeyCode shoulderSwapKey = KeyCode.V; // omuz de�i�tir

    [Header("Look (Mouse)")]
    public float sensX = 120f;        // deg/sec
    public float sensY = 100f;
    public float deadzone = 0.03f;    // k���k hareketleri yut
    public float minPitch = -55f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Smoothing")]
    public float rotateLerp = 12f;    // 0 = an�nda
    public float distanceLerp = 12f;
    public float sideLerp = 18f;      // omuz ge�i� h�z�

    [Header("Aim / Align")]
    public bool centerWhenAiming = true;     // RMB bas�l�yken omuz = 0
    public bool alignPlayerOnAim = true;     // RMB bas�l�yken oyuncuyu yaw�a hizala
    public KeyCode aimKey = KeyCode.Mouse1;

    [Header("Recenter While Moving")]
    public bool recenterOnMove = true;
    public float recenterDelay = 0.35f;      // input kesilince bekleme
    public float recenterSpeed = 120f;       // deg/sec (ne kadar h�zl� arkaya gelsin)

    [Header("Collision")]
    public LayerMask collisionMask = ~0;     // �evre layer��
    public float collisionRadius = 0.2f;     // kamera k�resi
    public float collisionBuffer = 0.1f;     // duvardan pay

    float yaw, pitch;                  // hedef a��
    float yawSm, pitchSm;              // smooth a��
    float distSm;                      // smooth mesafe
    float sideSm;                      // smooth omuz
    float noInputTimer;

    void Start()
    {
        if (!target) { Debug.LogWarning("[TPSCameraOrbit] target yok."); enabled = false; return; }

        // Edit�rdeki ba�lang�� pozundan a��lar� al
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
        // omuz de�i�tir
        if (Input.GetKeyDown(shoulderSwapKey)) sideOffset = -sideOffset;

        // mouse giri�leri
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // �l� b�lge
        if (Mathf.Abs(mx) < deadzone) mx = 0f;
        if (Mathf.Abs(my) < deadzone) my = 0f;

        yaw = Mathf.Repeat(yaw + mx * sensX * Time.deltaTime, 360f);
        float dy = my * sensY * Time.deltaTime * (invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch + dy, minPitch, maxPitch);

        // scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance - scroll * 2.0f, minDistance, maxDistance);

        // hareket var m�? (WASD)
        bool moving =
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (moving) noInputTimer = 0f; else noInputTimer += Time.deltaTime;

        // hareket ederken (aim de�ilken) kameray� yumu�ak�a arkaya al
        if (recenterOnMove && moving && !Input.GetKey(aimKey))
        {
            float targetYaw = target.eulerAngles.y; // Player��n y�n�
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, recenterSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // yumu�atma
        float rt = (rotateLerp <= 0f) ? 1f : (1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
        yawSm = Mathf.LerpAngle(yawSm, yaw, rt);
        pitchSm = Mathf.LerpAngle(pitchSm, pitch, rt);

        float dt = (distanceLerp <= 0f) ? 1f : (1f - Mathf.Exp(-distanceLerp * Time.deltaTime));
        distSm = Mathf.Lerp(distSm, distance, dt);

        float st = (sideLerp <= 0f) ? 1f : (1f - Mathf.Exp(-sideLerp * Time.deltaTime));
        float sideTarget = 0f; // her durumda merkez arkas�

        sideSm = Mathf.Lerp(sideSm, sideTarget, st);

        // rot & pivot
        Quaternion rot = Quaternion.Euler(pitchSm, yawSm, 0f);
        Vector3 pivot = target.position + pivotOffset;

        // istenen pozisyon
        Vector3 desired = pivot
                        - rot * Vector3.forward * distSm
                        + rot * Vector3.right * sideSm;

        // �arp��ma: pivot->desired aras� spherecast ile k�salt
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

        // aim�de karakteri kamera yaw�una hizala (iste�e ba�l�)
        if (alignPlayerOnAim && Input.GetKey(aimKey))
        {
            Quaternion targetYaw = Quaternion.Euler(0f, yawSm, 0f);
            target.rotation = targetYaw;
            if (model) model.rotation = Quaternion.Slerp(model.rotation, targetYaw, rt);
        }
    }

    static float NormalizePitch(float x) { if (x > 180f) x -= 360f; return x; }
}
