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

    [Header("Visual")]
    public Transform model;              // cowboy child'ı
    public float modelRotateLerp = 12f;  // dönüş yumuşatma

    [Header("Crouch")]
    public float crouchSpeed = 1.5f;     // eğilirken hız

    [Header("Aim")]
    public bool isAiming;                 // AimController set eder (toggle RMB)
    public string aimBoolParam = "Aiming";// Animator bool param adı (opsiyonel)

    // Pistol/Aim state takibi (trigger spam önlemek için)
    private bool isPistolActive;
    private bool isPistolWalking;
    private bool isPistolCrouched;

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

        // --- Input (KAMERA REFERANSLI) ---
        inputDir = Vector3.zero;

        Transform cam = Camera.main ? Camera.main.transform : null;
        Vector3 camF = transform.forward; // fallback
        Vector3 camR = transform.right;
        if (cam != null)
        {
            camF = cam.forward; camF.y = 0f; camF.Normalize();
            camR = cam.right; camR.y = 0f; camR.Normalize();
        }

        if (Input.GetKey(KeyCode.W)) inputDir += camF;
        if (Input.GetKey(KeyCode.S)) inputDir -= camF;
        if (Input.GetKey(KeyCode.A)) inputDir -= camR;
        if (Input.GetKey(KeyCode.D)) inputDir += camR;

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

        // Crouch hız limiti
        if (isCharacterCrouched)
            currentSpeed = Mathf.Min(currentSpeed, crouchSpeed);

        // --- Animasyon Kararı ---
        bool moving = inputDir.magnitude >= 0.1f;
        bool sprint = moving && Input.GetKey(KeyCode.LeftShift);

        // NİŞAN ÖNCELİKLİ
        if (isAiming)
        {
            if (animator) animator.SetBool(aimBoolParam, true);

            if (isCharacterCrouched)
            {
                // Nişan + eğilmişken HER ZAMAN crouch idle (yürüse bile üst gövde idle)
                TriggerPistolCrouchIdleAnimation();
                isCharacterWalking = false;
                isCharacterRunning = false;
            }
            else
            {
                // Nişan + ayakta: duruyorsa idle, hareket varsa walk
                if (!moving)
                {
                    TriggerPistolIdleAnimation();
                    isCharacterWalking = false;
                    isCharacterRunning = false;
                }
                else
                {
                    TriggerPistolWalkAnimation();
                    isCharacterWalking = true;   // pistol-walk
                    isCharacterRunning = false;
                }
            }
        }
        else
        {
            // nişandan çıkarken tüm pistol state'lerini kapat
            if (isPistolActive)
            {
                ResetPistolTriggers();
                isPistolActive = false;
                isPistolWalking = false;
                isPistolCrouched = false;
            }

            if (animator) animator.SetBool(aimBoolParam, false);

            // Normal/Crouch akışı
            if (isCharacterCrouched)
            {
                if (!moving)
                {
                    TriggerCrouchIdleAnimation();
                    isCharacterWalking = false;
                    isCharacterRunning = false;
                }
                else
                {
                    TriggerCrouchWalkAnimation();
                    isCharacterWalking = true;
                    isCharacterRunning = false;
                }
            }
            else
            {
                if (!moving)
                {
                    TriggerIdleAnimation();
                    isCharacterWalking = false;
                    isCharacterRunning = false;
                }
                else if (sprint)
                {
                    TriggerRunAnimation();
                }
                else
                {
                    isCharacterRunning = false;
                    TriggerWalkAnimation();
                    isCharacterWalking = true;
                }
            }
        }

        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // Y bileşenini koru
        Vector3 v = rb.velocity;

        // Hedef yatay hız
        Vector3 targetXZ = inputDir * currentSpeed;

        // XZ hızını ayarla
        v.x = targetXZ.x;
        v.z = targetXZ.z;
        rb.velocity = v;

        // Model dönüşü: normalde hareket yönüne; aim'de kameraya
        if (model != null)
        {
            Vector3 faceDir = inputDir;

            if (isAiming)
            {
                var camTr = Camera.main ? Camera.main.transform : null;
                if (camTr)
                {
                    faceDir = camTr.forward;
                    faceDir.y = 0f;
                    if (faceDir.sqrMagnitude < 0.0001f) faceDir = transform.forward;
                    faceDir.Normalize();
                }
            }

            if (faceDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
                model.rotation = Quaternion.Slerp(model.rotation, targetRot, modelRotateLerp * Time.fixedDeltaTime);
            }
        }
    }

    // -------- NORMAL STATE TRIGGER'LARI --------
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
<<<<<<< Updated upstream
        // her durumda Idle'a dön (pistol varyantlarını da kapat)
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Run");
        animator.ResetTrigger("CrouchIdle");

        animator.ResetTrigger("PistolIdle");
        animator.ResetTrigger("PistolWalk");
        animator.ResetTrigger("PistolCrouchIdle");

        animator.SetTrigger("Idle");

        isCharacterWalking = false;
        isCharacterRunning = false;
        isPistolActive = false;
        isPistolWalking = false;
        isPistolCrouched = false;
=======
        if (isCharacterWalking || isCharacterRunning)
        {
            animator.ResetTrigger("Walk");
            animator.ResetTrigger("Run");
            animator.ResetTrigger("CrouchIdle");
            animator.SetTrigger("Idle");
            isCharacterWalking = false;
            isCharacterRunning = false;
        }
>>>>>>> Stashed changes
    }

    void TriggerRunAnimation()
    {
        if (!isCharacterRunning)
        {
            animator.SetTrigger("Run");
            isCharacterRunning = true;
            isCharacterWalking = false;
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

    // -------- PISTOL (AIM) TRIGGER'LARI --------
    void TriggerPistolIdleAnimation()
    {
        if (!isPistolActive || isPistolWalking || isPistolCrouched)
        {
            animator.ResetTrigger("PistolWalk");
            animator.ResetTrigger("PistolCrouchIdle");
            animator.SetTrigger("PistolIdle");

            isPistolActive = true;
            isPistolWalking = false;
            isPistolCrouched = false;
        }
    }

    void TriggerPistolWalkAnimation()
    {
        if (!isPistolActive || !isPistolWalking || isPistolCrouched)
        {
            animator.ResetTrigger("PistolIdle");
            animator.ResetTrigger("PistolCrouchIdle");
            animator.SetTrigger("PistolWalk");

            isPistolActive = true;
            isPistolWalking = true;
            isPistolCrouched = false;
        }
    }

    void TriggerPistolCrouchIdleAnimation()
    {
        if (!isPistolActive || isPistolWalking || !isPistolCrouched)
        {
            animator.ResetTrigger("PistolWalk");
            animator.ResetTrigger("PistolIdle");
            animator.SetTrigger("PistolCrouchIdle");

            isPistolActive = true;
            isPistolWalking = false;
            isPistolCrouched = true;
        }
    }

    void ResetPistolTriggers()
    {
        animator.ResetTrigger("PistolIdle");
        animator.ResetTrigger("PistolWalk");
        animator.ResetTrigger("PistolCrouchIdle");
    }
}
