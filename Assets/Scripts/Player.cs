using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;

    Rigidbody rb;
    public Animator animator;
    Vector3 inputDir;
    float currentSpeed;

    [Header("Visual")]
    public Transform model;
    public float modelRotateLerp = 12f;

    [Header("Crouch")]
    public float crouchSpeed = 1.5f;
    bool isCharacterCrouched;

    [Header("Aim (AimController set eder)")]
    public bool isAiming;
    public string aimBoolParam = "Aiming";

    enum AnimState
    {
        Idle, Walk, Run, CrouchIdle, CrouchWalk,
        PistolIdle, PistolWalk, PistolCrouchIdle
    }
    AnimState currentState = AnimState.Idle;

    const string TR_IDLE = "Idle";
    const string TR_WALK = "Walk";
    const string TR_RUN = "Run";
    const string TR_CROUCH_IDLE = "CrouchIdle";
    const string TR_CROUCH_WALK = "CrouchWalk";
    const string TR_PISTOL_IDLE = "PistolIdle";
    const string TR_PISTOL_WALK = "PistolWalk";
    const string TR_PISTOL_CROUCHID = "PistolCrouchIdle";

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

        // --- Input (kamera referanslı) ---
        inputDir = Vector3.zero;
        Transform cam = Camera.main ? Camera.main.transform : null;
        Vector3 camF = transform.forward, camR = transform.right;
        if (cam != null)
        {
            camF = cam.forward; camF.y = 0f; camF.Normalize();
            camR = cam.right; camR.y = 0f; camR.Normalize();
        }
        if (Input.GetKey(KeyCode.W)) inputDir += camF;
        if (Input.GetKey(KeyCode.S)) inputDir -= camF;
        if (Input.GetKey(KeyCode.A)) inputDir -= camR;
        if (Input.GetKey(KeyCode.D)) inputDir += camR;

        // C toggle
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCharacterCrouched = !isCharacterCrouched;
            // Aim açıkken normal crouch trigger’ları atmayacağız; state machine halleder.
        }

        // Crouch hız limiti
        if (isCharacterCrouched)
            currentSpeed = Mathf.Min(currentSpeed, crouchSpeed);

        bool moving = inputDir.sqrMagnitude >= 0.01f;
        bool sprint = moving && Input.GetKey(KeyCode.LeftShift);

        if (animator) animator.SetBool(aimBoolParam, isAiming);

        // --- Hedef durumu hesapla ---
        AnimState next = currentState;

        if (isAiming)
        {
            if (isCharacterCrouched)
            {
                // İSTEDİĞİN DEĞİŞİKLİK: crouch + aim → hareket ediyorsa PistolWalk, duruyorsa PistolCrouchIdle
                next = moving ? AnimState.PistolWalk : AnimState.PistolCrouchIdle;
            }
            else
            {
                next = moving ? AnimState.PistolWalk : AnimState.PistolIdle;
            }
        }
        else
        {
            if (isCharacterCrouched)
            {
                next = moving ? AnimState.CrouchWalk : AnimState.CrouchIdle;
            }
            else
            {
                if (!moving) next = AnimState.Idle;
                else next = (sprint ? AnimState.Run : AnimState.Walk);
            }
        }

        // --- Yalnızca değiştiyse trigger at ---
        if (next != currentState)
        {
            FireTransition(next);
            currentState = next;
        }

        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // hız uygula
        Vector3 v = rb.velocity;
        Vector3 targetXZ = inputDir * currentSpeed;
        v.x = targetXZ.x; v.z = targetXZ.z;
        rb.velocity = v;

        // model dönüşü: normalde hareket yönüne; aim'de kameraya
        if (model != null)
        {
            Vector3 faceDir = inputDir;
            if (isAiming)
            {
                var camTr = Camera.main ? Camera.main.transform : null;
                if (camTr)
                {
                    faceDir = camTr.forward; faceDir.y = 0f;
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

    void FireTransition(AnimState next)
    {
        // Temizle (sadece değişimde)
        animator.ResetTrigger(TR_IDLE);
        animator.ResetTrigger(TR_WALK);
        animator.ResetTrigger(TR_RUN);
        animator.ResetTrigger(TR_CROUCH_IDLE);
        animator.ResetTrigger(TR_CROUCH_WALK);
        animator.ResetTrigger(TR_PISTOL_IDLE);
        animator.ResetTrigger(TR_PISTOL_WALK);
        animator.ResetTrigger(TR_PISTOL_CROUCHID);

        // Hedef trigger
        switch (next)
        {
            case AnimState.Idle: animator.SetTrigger(TR_IDLE); break;
            case AnimState.Walk: animator.SetTrigger(TR_WALK); break;
            case AnimState.Run: animator.SetTrigger(TR_RUN); break;
            case AnimState.CrouchIdle: animator.SetTrigger(TR_CROUCH_IDLE); break;
            case AnimState.CrouchWalk: animator.SetTrigger(TR_CROUCH_WALK); break;
            case AnimState.PistolIdle: animator.SetTrigger(TR_PISTOL_IDLE); break;
            case AnimState.PistolWalk: animator.SetTrigger(TR_PISTOL_WALK); break;
            case AnimState.PistolCrouchIdle: animator.SetTrigger(TR_PISTOL_CROUCHID); break;
        }
    }
}
