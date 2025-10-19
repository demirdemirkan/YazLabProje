using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;

    Rigidbody rb;
    private bool isCharacterWalking;
    private bool isCharacterRunning;
    private bool isCharacterCrouched;
    public Animator animator;
    Vector3 inputDir;
    float currentSpeed;

    // ADD
    [Header("Visual")]
    public Transform model;              // cowboy child'ını buraya sürükle
    public float modelRotateLerp = 12f;  // dönüş yumuşatma hızı

    // NEW: crouch hızı
    public float crouchSpeed = 1.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // --- Input Topla ---
        inputDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) inputDir += transform.forward;
        if (Input.GetKey(KeyCode.S)) inputDir -= transform.forward;
        if (Input.GetKey(KeyCode.A)) inputDir -= transform.right;
        if (Input.GetKey(KeyCode.D)) inputDir += transform.right;

        // --- Crouch Toggle (C) ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isCharacterCrouched)
            {
                isCharacterCrouched = true;
                animator.ResetTrigger("Idle");
                animator.ResetTrigger("Walk");
                animator.ResetTrigger("Run");
                animator.SetTrigger("CrouchIdle");
                isCharacterWalking = false;
                isCharacterRunning = false;
            }
            else
            {
                isCharacterCrouched = false;
                animator.ResetTrigger("CrouchIdle");
                animator.ResetTrigger("CrouchWalk");
                animator.SetTrigger("Idle");
                isCharacterWalking = false;
                isCharacterRunning = false;
            }
        }

        // Crouch'ta hız kısıtı
        if (isCharacterCrouched)
            currentSpeed = Mathf.Min(currentSpeed, crouchSpeed);

        // --- Animasyon Kararı ---
        bool moving = inputDir.magnitude >= 0.1f;
        bool sprint = moving && Input.GetKey(KeyCode.LeftShift);

        if (isCharacterCrouched)
        {
            // Crouch modunda SADECE crouch animleri
            if (!moving)
            {
                TriggerCrouchIdleAnimation();
                isCharacterWalking = false;
                isCharacterRunning = false;
            }
            else
            {
                TriggerCrouchWalkAnimation();
                isCharacterWalking = true;   // yürüyorsun ama crouch-walk
                isCharacterRunning = false;
            }
        }
        else
        {
            // Normal mod (Idle/Walk/Run)
            if (!moving)
            {
                TriggerIdleAnimation();          // önce tetikle
                isCharacterWalking = false;      // sonra flagleri temizle
                isCharacterRunning = false;
                // DİKKAT: burada isCharacterCrouched = false YAZMIYORUZ!
            }
            else if (sprint)
            {
                TriggerRunAnimation();
            }
            else
            {
                isCharacterRunning = false;
                TriggerWalkAnimation();
            }
        }

        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // Y bileşenini koru (yerçekimi için)
        Vector3 v = rb.velocity;

        // Hedef yatay hız
        Vector3 targetXZ = inputDir * currentSpeed;

        // Anlık hızda XZ’yi hedefe ayarla, Y’yi dokunma
        v.x = targetXZ.x;
        v.z = targetXZ.z;
        rb.velocity = v;

        // ADD: Görsel modeli hareket yönüne döndür (parent dönmez)
        if (model != null && inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            model.rotation = Quaternion.Slerp(model.rotation, targetRot, modelRotateLerp * Time.fixedDeltaTime);
        }
        // input yoksa mevcut yönünü korur (idle'da sabit)
    }

    void TriggerWalkAnimation()
    {
        if (!isCharacterWalking)
        {
            animator.SetTrigger("Walk");
            isCharacterWalking = true;
        }
    }

    void TriggerIdleAnimation()
    {
        if (isCharacterWalking || isCharacterRunning)
        {
            animator.ResetTrigger("Walk"); // çakışmayı engeller
            animator.ResetTrigger("Run");
            animator.ResetTrigger("Squat");
            animator.SetTrigger("Idle");
            isCharacterWalking = false;
            isCharacterRunning = false;
        }
    }

    void TriggerRunAnimation()
    {
        if (!isCharacterRunning)
        {
            animator.SetTrigger("Run");
            isCharacterRunning = true;
            isCharacterWalking = false; // koşuya geçince yürüyüş flag’i kapansın
        }
    }

    void TriggerCrouchIdleAnimation()
    {
        animator.SetTrigger("CrouchIdle");
    }

    void TriggerCrouchWalkAnimation()
    {
        animator.SetTrigger("CrouchWalk");
    }
}
