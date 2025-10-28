using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;

    Rigidbody rb;

    [Header("Colliders")]
    public CapsuleCollider capsule;                 // Root’taki kapsül (runtime’da otomatik üretilecek)
    public CapsuleCollider childCapsuleHint;        // Cowboy child’daki kapsülü buraya sürükleyebilirsin (boşsa otomatik bulur)
    public string capsuleChildNameHint = "cowboy";  // Aramada isim ipucu (opsiyonel)

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

    // >>> Aiming param güvenliği için ek alanlar <<<
    int aimBoolHash;
    bool hasAimBool;

    // --- Jump (sade & sağlam) ---
    [Header("Jump")]
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpVelocity = 6.0f;
    public float fallMultiplier = 1.5f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;      // testte Everything
    public float footProbeRadius = 0.18f;  // ayak altı küre yarıçapı
    public float footProbeLift = 0.06f;    // küre merkezini tabandan yukarı
    public float footProbeDepth = 0.08f;   // referans (OverlapSphere kullanıyoruz)

    [Header("Jump Tuning")]
    public float coyoteTime = 0.12f;

    [Header("Anti-Sink")]
    public float overlapPadding = 0.02f;   // depenetrasyon için küçük pay
    public int maxOverlapIters = 1;        // her karede en fazla 1 düzeltme (yeterli)
    public LayerMask antiSinkMask = ~0;    // genelde groundMask ile aynı bırak

    bool wantToJump;
    bool isGrounded, wasGrounded;
    float coyoteTimer;

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

    // >>> Animator'da parametre var mı kontrolü (helper) <<<
    static bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (!anim) return false;
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].type == type && ps[i].name == name)
                return true;
        return false;
    }

    // >>> Trigger güvenliği (varsa set/reset) <<<
    bool HasTrigger(string name) => animator && HasParam(animator, name, AnimatorControllerParameterType.Trigger);
    void SafeSetTrigger(string name) { if (HasTrigger(name)) animator.SetTrigger(name); }
    void SafeResetTrigger(string name) { if (HasTrigger(name)) animator.ResetTrigger(name); }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 1) Child kapsülü bul
        if (!childCapsuleHint)
        {
            var all = GetComponentsInChildren<CapsuleCollider>(true);
            foreach (var c in all)
            {
                if (c.attachedRigidbody == rb) { childCapsuleHint = c; break; } // çoğu zaman root’takini bulur
            }
            if (!childCapsuleHint)
            {
                foreach (var c in all)
                {
                    if (c && c.transform != transform)
                    {
                        if (string.IsNullOrEmpty(capsuleChildNameHint) ||
                            c.transform.name.ToLower().Contains(capsuleChildNameHint.ToLower()))
                        { childCapsuleHint = c; break; }
                    }
                }
            }
        }

        // 2) Root’ta kapsül yoksa, child kapsülden root’a oluştur
        if (!capsule)
        {
            if (childCapsuleHint)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
                CopyChildCapsuleToRoot(childCapsuleHint, capsule);
                // Child kapsülü devre dışı bırak (çifte çarpışmayı önle)
                childCapsuleHint.enabled = false;
            }
            else
            {
                // Son çare: makul bir kapsül
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.zero;
                capsule.radius = 0.3f;
                capsule.height = 1.8f;
            }
        }

        capsule.isTrigger = false;

        // Rigidbody ayarları
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // sink’e karşı daha iyi
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        // >>> Aiming bool param kontrolü ve alternatif isimlere fallback <<<
        if (animator)
        {
            hasAimBool = HasParam(animator, aimBoolParam, AnimatorControllerParameterType.Bool);

            if (!hasAimBool)
            {
                // Animator’da farklı isim kullanılmış olabilir; yaygın alternatifleri sırayla dene
                string[] candidates = { "Aiming", "IsAiming", "isAiming", "Aim" };
                foreach (var c in candidates)
                {
                    if (HasParam(animator, c, AnimatorControllerParameterType.Bool))
                    {
                        aimBoolParam = c;
                        hasAimBool = true;
                        break;
                    }
                }

                if (!hasAimBool)
                {
                    Debug.LogWarning($"[Player] Animator bool param bulunamadı: '{aimBoolParam}'. Animator'a bir Bool ekleyin (örn: 'Aiming').");
                }
            }

            if (hasAimBool)
                aimBoolHash = Animator.StringToHash(aimBoolParam);
        }
    }

    // Child kapsülün WORLD bounds’larına göre root’a doğru kapsül kurar
    void CopyChildCapsuleToRoot(CapsuleCollider child, CapsuleCollider rootCol)
    {
        Bounds wb = child.bounds; // world bounds
        // Root’un local’ine dön
        Vector3 localCenter = transform.InverseTransformPoint(wb.center);

        // Height/Radius’ı lossyScale’e göre normalize et
        Vector3 lossy = transform.lossyScale;
        float hLocal = wb.size.y / Mathf.Max(0.0001f, lossy.y);
        float rLocalX = (wb.extents.x) / Mathf.Max(0.0001f, lossy.x);
        float rLocalZ = (wb.extents.z) / Mathf.Max(0.0001f, lossy.z);
        float rLocal = Mathf.Max(rLocalX, rLocalZ);

        rootCol.center = localCenter;
        rootCol.height = Mathf.Max(hLocal, rLocal * 2f + 0.01f);
        rootCol.radius = rLocal;
        rootCol.direction = 1; // Y ekseni
    }

    void Update()
    {
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // Input (kamera referanslı)
        inputDir = Vector3.zero;
        Transform cam = Camera.main ? Camera.main.transform : null;
        Vector3 camF = transform.forward, camR = transform.right;
        if (cam)
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
            isCharacterCrouched = !isCharacterCrouched;

        // Jump input
        if (Input.GetKeyDown(jumpKey))
            if (!isCharacterCrouched) wantToJump = true;

        if (isCharacterCrouched)
            currentSpeed = Mathf.Min(currentSpeed, crouchSpeed);

        bool moving = inputDir.sqrMagnitude >= 0.01f;
        bool sprint = moving && Input.GetKey(KeyCode.LeftShift);

        // >>> Güvenli Aiming set (parametre varsa) <<<
        if (animator && hasAimBool) animator.SetBool(aimBoolHash, isAiming);

        // Anim hedefi
        AnimState next = currentState;
        if (isAiming)
        {
            if (isCharacterCrouched)
                next = moving ? AnimState.PistolWalk : AnimState.PistolCrouchIdle;
            else
                next = moving ? AnimState.PistolWalk : AnimState.PistolIdle;
        }
        else
        {
            if (isCharacterCrouched)
                next = moving ? AnimState.CrouchWalk : AnimState.CrouchIdle;
            else
                next = !moving ? AnimState.Idle : (sprint ? AnimState.Run : AnimState.Walk);
        }

        if (next != currentState)
        {
            FireTransition(next);
            currentState = next;
        }

        inputDir = inputDir.normalized;
    }

    void FixedUpdate()
    {
        // 0) Çok hafif depenetrasyon (YUKARI ağırlıklı)
        ResolveOverlapsUp();

        // 1) Ground check (OverlapSphere, root kapsülle)
        wasGrounded = isGrounded;
        isGrounded = CheckGroundedSphere();

        // 2) coyote
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.fixedDeltaTime;

        // 3) XZ hız
        Vector3 v = rb.velocity;
        Vector3 targetXZ = inputDir * currentSpeed;
        v.x = targetXZ.x; v.z = targetXZ.z;

        // 4) Jump
        if (wantToJump && (isGrounded || coyoteTimer > 0f))
        {
            v.y = jumpVelocity;
            wantToJump = false;
            isGrounded = false;
            coyoteTimer = 0f;

            // Güvenli trigger kullan
            SafeResetTrigger(TR_CROUCH_IDLE);
            SafeSetTrigger("Jump");
        }

        // 5) Düşüş hızlandırma
        if (v.y < 0f && fallMultiplier > 1f)
            v.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;

        rb.velocity = v;

        // 6) Model dönüşü
        if (model)
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

        // 7) iniş tetik (güvenli)
        if (!wasGrounded && isGrounded)
            SafeSetTrigger("Land");
    }

    void FireTransition(AnimState next)
    {
        // Reset'leri güvenli yap
        SafeResetTrigger(TR_IDLE);
        SafeResetTrigger(TR_WALK);
        SafeResetTrigger(TR_RUN);
        SafeResetTrigger(TR_CROUCH_IDLE);
        SafeResetTrigger(TR_CROUCH_WALK);
        SafeResetTrigger(TR_PISTOL_IDLE);
        SafeResetTrigger(TR_PISTOL_WALK);
        SafeResetTrigger(TR_PISTOL_CROUCHID);

        // Set'leri güvenli yap
        switch (next)
        {
            case AnimState.Idle: SafeSetTrigger(TR_IDLE); break;
            case AnimState.Walk: SafeSetTrigger(TR_WALK); break;
            case AnimState.Run: SafeSetTrigger(TR_RUN); break;
            case AnimState.CrouchIdle: SafeSetTrigger(TR_CROUCH_IDLE); break;
            case AnimState.CrouchWalk: SafeSetTrigger(TR_CROUCH_WALK); break;
            case AnimState.PistolIdle: SafeSetTrigger(TR_PISTOL_IDLE); break;
            case AnimState.PistolWalk: SafeSetTrigger(TR_PISTOL_WALK); break;
            case AnimState.PistolCrouchIdle: SafeSetTrigger(TR_PISTOL_CROUCHID); break;
        }
    }

    // ------- Ground check (OverlapSphere: ROOT kapsülle) -------
    bool CheckGroundedSphere()
    {
        if (!capsule) return false;

        var b = capsule.bounds;
        float bottomY = b.center.y - b.extents.y;
        Vector3 center = new Vector3(b.center.x, bottomY + footProbeLift, b.center.z);

        Collider[] hits = Physics.OverlapSphere(center, footProbeRadius, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;

            // Kendimizi sayma
            if (h.attachedRigidbody == rb) continue;
            if (h.transform.IsChildOf(transform)) continue;

            return true;
        }
        return false;
    }

    // ------- Mini depenetrasyon: YUKARI ağırlıklı düzelt -------
    void ResolveOverlapsUp()
    {
        if (!capsule) return;

        var b = capsule.bounds;
        Vector3 p1 = new Vector3(b.center.x, b.center.y + b.extents.y - overlapPadding, b.center.z);
        Vector3 p2 = new Vector3(b.center.x, b.center.y - b.extents.y + overlapPadding, b.center.z);
        float r = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) - overlapPadding);

        for (int iter = 0; iter < maxOverlapIters; iter++)
        {
            var hits = Physics.OverlapCapsule(p1, p2, r, antiSinkMask, QueryTriggerInteraction.Ignore);
            bool fixedAny = false;

            foreach (var col in hits)
            {
                if (!col) continue;
                if (col.attachedRigidbody == rb) continue;
                if (col.transform.IsChildOf(transform)) continue;

                if (Physics.ComputePenetration(capsule, transform.position, transform.rotation,
                                               col, col.transform.position, col.transform.rotation,
                                               out Vector3 dir, out float dist))
                {
                    // Sadece yukarı bileşen uygula (zemine yapıştır)
                    float upComp = Mathf.Max(0f, Vector3.Dot(dir, Vector3.up));
                    if (upComp > 0f && dist > 0f)
                    {
                        Vector3 correction = Vector3.up * (upComp * (dist + 0.001f));
                        rb.position += correction;

                        // Düşüş hızını sıfırla
                        if (rb.velocity.y < 0f)
                            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

                        fixedAny = true;
                    }
                }
            }
            if (!fixedAny) break;
            // bounds’ı tazele
            b = capsule.bounds;
            p1 = new Vector3(b.center.x, b.center.y + b.extents.y - overlapPadding, b.center.z);
            p2 = new Vector3(b.center.x, b.center.y - b.extents.y + overlapPadding, b.center.z);
            r = Mathf.Max(0.05f, Mathf.Min(b.extents.x, b.extents.z) - overlapPadding);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!capsule)
        {
            var tmp = GetComponent<CapsuleCollider>();
            if (tmp) capsule = tmp;
        }
        if (!capsule) return;

        var b = capsule.bounds;
        float bottomY = b.center.y - b.extents.y;
        Vector3 center = new Vector3(b.center.x, bottomY + footProbeLift, b.center.z);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, footProbeRadius);
    }
#endif
}
