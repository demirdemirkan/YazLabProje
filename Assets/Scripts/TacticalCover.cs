using UnityEngine;

[DisallowMultipleComponent]
public class TacticalCover : MonoBehaviour
{
    [Header("Input (legacy Input)")]
    public KeyCode toggleCoverKey = KeyCode.V;
    public KeyCode leanLeftKey = KeyCode.Q;
    public KeyCode leanRightKey = KeyCode.E;
    public KeyCode aimKey = KeyCode.Mouse1;

    [Header("Cover Detection")]
    public LayerMask coverMask;           // Inspector'da sadece "Cover" seç
    public float findCoverDistance = 2.0f;
    public float findCoverRadius = 0.55f;
    public float snapDepth = 0.25f;   // duvara yaklaşım derinliği
    public float maxEnterAngle = 75f;     // stabil için 70-75 iyi

    [Header("Move Along Cover")]
    public float slideSpeed = 2.2f;
    public float friction = 12f;

    [Header("Peek / Lean (camera offset)")]
    public float leanAmount = 0.25f;
    public float leanHeadUp = 0.08f;
    public float leanRotDeg = 10f;
    public float leanLerp = 10f;

    [Header("Animator (optional)")]
    public Animator animator;
    public string inCoverBool = "InCover";
    public string leanFloat = "Lean";         // -1..+1
    public string movingFloat = "CoverSpeed";   // 0..1 (BlendTree Y)
    public string coverDirFloat = "CoverDir";     // -1..+1 (BlendTree X)

    [Header("Camera (optional)")]
    public Transform cameraRig; // yoksa Awake'de Main Camera bulunur
    public Vector3 cameraLocalOffsetCover = new Vector3(0.15f, 0.05f, 0f);
    public Vector3 cameraLocalOffsetDefault = Vector3.zero;

    [Header("Stabilite")]
    public float enterGrace = 0.25f; // cover'a girdikten sonra kısa süre çıkma
    float enterGraceTimer = 0f;

    [Header("Root Motion Control")]
    public bool disableRootMotionInCover = true;
    bool savedApplyRootMotion = false;

    [Header("Debug")]
    public bool debugLogs = true;

    // ---- runtime state
    bool inCover = false;
    Vector3 coverNormal;   // yatay normalize edilmiş
    Vector3 coverTangent;  // duvar boyunca yön
    Vector3 velocity;      // kayma hızı

    // refs
    Transform tr;
    Rigidbody rb;
    CapsuleCollider capCol;
    CharacterController cc;

    // arama yükseklikleri (karakter boyuna göre ayarlanır)
    float chestY = 1.0f;
    float waistY = 0.7f;
    float kneeY = 0.4f;

    void Awake()
    {
        tr = transform;
        rb = GetComponent<Rigidbody>();
        capCol = GetComponent<CapsuleCollider>();
        cc = GetComponent<CharacterController>();

        // Kamera otomatik: Rig yoksa Main Camera (parent varsa parent)
        if (cameraRig == null && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            cameraRig = cam.parent != null ? cam.parent : cam;
        }

        // Mask boşsa "Cover" layer'ını dener
        if (coverMask.value == 0)
        {
            int coverLayer = LayerMask.NameToLayer("Cover");
            if (coverLayer >= 0) coverMask = (1 << coverLayer);
        }

        // Yükseklikleri mevcut colliderlardan çıkar
        if (capCol)
        {
            float h = Mathf.Max(1.4f, capCol.height);
            chestY = Mathf.Clamp(h * 0.55f, 0.8f, 1.4f);
            waistY = Mathf.Clamp(h * 0.40f, 0.5f, 1.2f);
            kneeY = Mathf.Clamp(h * 0.25f, 0.3f, 0.9f);
        }
        if (cc)
        {
            float h = Mathf.Max(1.6f, cc.height);
            chestY = Mathf.Clamp(h * 0.55f, 0.9f, 1.5f);
            waistY = Mathf.Clamp(h * 0.40f, 0.6f, 1.3f);
            kneeY = Mathf.Clamp(h * 0.25f, 0.35f, 1.0f);
        }
    }

    void Update()
    {
        // toggle enter/exit
        if (Input.GetKeyDown(toggleCoverKey))
        {
            if (inCover) ExitCover();
            else TryEnterCover();
        }

        if (!inCover) return;

        // girişten hemen sonra kısa süre çıkma (grace)
        if (enterGraceTimer > 0f) enterGraceTimer -= Time.deltaTime;

        MaintainSnap();

        // Yan hareket girdisi (A/D + Q/E)
        float h = 0f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(leanLeftKey)) h -= 1f;  // Q = sola hareket de etsin
        if (Input.GetKey(leanRightKey)) h += 1f;  // E = sağa hareket de etsin
        h = Mathf.Clamp(h, -1f, 1f);

        SlideAlong(h);

        // Lean hesapla (Q/E aim ile beraber çalışır)
        float lean = 0f;
        if (Input.GetKey(leanLeftKey)) lean -= 1f;
        if (Input.GetKey(leanRightKey)) lean += 1f;
        if (Mathf.Approximately(lean, 0f) && Input.GetKey(aimKey)) lean = 0.6f;
        ApplyLean(lean);

        // Animator paramları
        if (animator)
        {
            animator.SetBool(inCoverBool, true);
            // BlendTree Y: hız
            float curMove = animator.GetFloat(movingFloat);
            animator.SetFloat(movingFloat, Mathf.Lerp(curMove, Mathf.Abs(h), Time.deltaTime * 10f));
            // BlendTree X: yön (-1 sol, +1 sağ)
            float curDir = animator.GetFloat(coverDirFloat);
            animator.SetFloat(coverDirFloat, Mathf.Lerp(curDir, h, Time.deltaTime * 10f));
            // Lean layer
            animator.SetFloat(leanFloat, Mathf.Lerp(animator.GetFloat(leanFloat), lean, Time.deltaTime * 10f));
        }

        // --- duvar depenetrasyon (Update tarafında da koru)
        ResolveWallPenetration();
    }

    // ===== COVER'A GİRİŞ =====
    void TryEnterCover()
    {
        // 3 yükseklik: göğüs, bel, diz
        Vector3[] origins = new[]
        {
            tr.position + Vector3.up * chestY,
            tr.position + Vector3.up * waistY,
            tr.position + Vector3.up * kneeY
        };

        // 3 yön: ileri, hafif sağ, hafif sol
        Vector3 f = tr.forward;
        Vector3[] dirs = new[]
        {
            f,
            Vector3.Slerp(f, tr.right, 0.35f).normalized,
            Vector3.Slerp(f, -tr.right, 0.35f).normalized
        };

        // --- 1) SphereCast çoklu tarama
        RaycastHit bestHit = default;
        bool found = false;
        float bestDist = float.MaxValue;

        foreach (var o in origins)
            foreach (var d in dirs)
            {
                if (Physics.SphereCast(o, findCoverRadius, d, out RaycastHit hit, findCoverDistance, coverMask, QueryTriggerInteraction.Ignore))
                {
                    float ang = Vector3.Angle(-hit.normal, tr.forward);
                    if (ang > maxEnterAngle) continue;

                    if (hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestHit = hit;
                        found = true;
                    }
                }
            }

        if (found) { EnterWithHit(bestHit, "[Cover] SphereCast"); return; }

        // --- 2) OverlapSphere yedek plan (yakındaki en yakın collider)
        foreach (var o in origins)
        {
            Collider[] cols = Physics.OverlapSphere(o, Mathf.Max(findCoverDistance, 1.5f), coverMask, QueryTriggerInteraction.Ignore);
            if (cols == null || cols.Length == 0) continue;

            Collider best = null;
            float bestSqr = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;

            foreach (var c in cols)
            {
                Vector3 p = c.ClosestPoint(o);
                float d2 = (p - o).sqrMagnitude;
                if (d2 < bestSqr) { bestSqr = d2; best = c; bestPoint = p; }
            }

            if (best != null)
            {
                Vector3 back = (o - bestPoint).normalized;
                Vector3 rayStart = bestPoint + back * 0.25f;
                if (Physics.Raycast(rayStart, -back, out RaycastHit hit2, 0.6f, coverMask, QueryTriggerInteraction.Ignore))
                {
                    EnterWithHit(hit2, "[Cover] Overlap+ClosestPoint");
                    return;
                }
                else
                {
                    // normal çıkarılamadıysa da güvenli snap uygula
                    Vector3 horizN = HorizontalizeNormal((o - bestPoint).normalized);
                    coverNormal = horizN;
                    coverTangent = Vector3.Cross(Vector3.up, coverNormal).normalized;

                    RaycastHit fakeHit = new RaycastHit { point = bestPoint, normal = coverNormal };
                    Vector3 snapPos = GetSafeSnapTarget(fakeHit);
                    SnapTo(snapPos);
                    FaceTangent();

                    inCover = true;
                    if (animator)
                    {
                        if (disableRootMotionInCover)
                        {
                            savedApplyRootMotion = animator.applyRootMotion;
                            animator.applyRootMotion = false; // root motion kapat
                        }
                        animator.SetTrigger("CoverEnter");
                        animator.SetBool(inCoverBool, true);
                    }
                    if (cameraRig) cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, cameraLocalOffsetCover, 1f);

                    // grace
                    enterGraceTimer = enterGrace;

                    if (debugLogs) Debug.Log($"[Cover] Fallback (no normal ray). Collider={best.name}");
                    return;
                }
            }
        }

        if (debugLogs) Debug.Log("[Cover] Yakında cover bulunamadı. (Layer/Collider/mesafe/açı kontrol et.)");
    }

    // güvenli giriş
    void EnterWithHit(RaycastHit hit, string reason)
    {
        coverNormal = HorizontalizeNormal(hit.normal);
        coverTangent = Vector3.Cross(Vector3.up, coverNormal).normalized;

        Vector3 snapPos = GetSafeSnapTarget(hit);
        SnapTo(snapPos);
        FaceTangent();

        inCover = true;
        if (animator)
        {
            if (disableRootMotionInCover)
            {
                savedApplyRootMotion = animator.applyRootMotion;
                animator.applyRootMotion = false; // root motion kapat
            }
            animator.SetTrigger("CoverEnter");   // giriş animasyonu
            animator.SetBool(inCoverBool, true); // cover mod
        }
        if (cameraRig) cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, cameraLocalOffsetCover, 1f);

        // grace başlat
        enterGraceTimer = enterGrace;

        if (debugLogs)
            Debug.Log($"{reason}: collider={hit.collider.name}, dist={hit.distance:F2}, angle={Vector3.Angle(-hit.normal, tr.forward):F1}");
    }

    // ===== COVER'DA TUTUNMA =====
    void MaintainSnap()
    {
        // yakın yüzeyi korumak için kısa geri spherecast
        Vector3[] origins = new[]
        {
            tr.position + Vector3.up * chestY,
            tr.position + Vector3.up * waistY,
            tr.position + Vector3.up * kneeY
        };

        bool ok = false;
        RaycastHit lastHit = default;

        // 1) Geriye (duvara) doğru tarama
        foreach (var o in origins)
        {
            if (Physics.SphereCast(o, findCoverRadius * 0.75f, -coverNormal, out RaycastHit hit, 1.0f, coverMask, QueryTriggerInteraction.Ignore))
            {
                lastHit = hit;
                ok = true;
                break;
            }
        }

        // 2) İlk tarama başarısızsa ve grace sürüyorsa, coverNormal yönüne kısa kurtarma taraması
        if (!ok && enterGraceTimer > 0f)
        {
            foreach (var o in origins)
            {
                if (Physics.SphereCast(o, findCoverRadius * 0.75f, coverNormal, out RaycastHit hit2, 1.0f, coverMask, QueryTriggerInteraction.Ignore))
                {
                    lastHit = hit2;
                    ok = true;
                    break;
                }
            }

            // Hâlâ yoksa, grace bitene kadar çıkma (mikro jitter’ı tolere et)
            if (!ok) return;
        }

        if (!ok)
        {
            if (debugLogs) Debug.Log("[Cover] Duvar kaybedildi, cover'dan çıkılıyor.");
            ExitCover();
            return;
        }

        Vector3 want = GetSafeSnapTarget(lastHit);
        Vector3 p = Vector3.Lerp(tr.position, want, Time.deltaTime * Mathf.Max(8f, friction));
        SnapTo(p);

        // --- duvar depenetrasyon (snap sonrası da uygula)
        ResolveWallPenetration();
    }

    // ===== DUVAR BOYUNCA KAY =====
    void SlideAlong(float input)
    {
        if (Mathf.Approximately(input, 0f))
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * friction);
        else
            velocity = Vector3.Lerp(velocity, coverTangent * (input * slideSpeed), Time.deltaTime * friction);

        Vector3 newPos = tr.position + velocity * Time.deltaTime;
        if (rb) rb.MovePosition(newPos); else tr.position = newPos;
    }

    // ===== LEAN / PEEK =====
    void ApplyLean(float lean)
    {
        if (cameraRig == null) return;

        Vector3 wantedPos = cameraLocalOffsetCover + new Vector3(lean * leanAmount, Mathf.Abs(lean) * leanHeadUp, 0f);
        Quaternion wantedRot = Quaternion.Euler(0f, 0f, -lean * leanRotDeg);

        cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, wantedPos, Time.deltaTime * leanLerp);
        cameraRig.localRotation = Quaternion.Slerp(cameraRig.localRotation, wantedRot, Time.deltaTime * leanLerp);
    }

    // ===== ÇIKIŞ =====
    void ExitCover()
    {
        inCover = false;
        velocity = Vector3.zero;

        if (animator)
        {
            animator.SetBool(inCoverBool, false); // InCover=false → Idle
            if (disableRootMotionInCover)
                animator.applyRootMotion = savedApplyRootMotion; // root motion'u eski haline getir
        }
        if (cameraRig) cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, cameraLocalOffsetDefault, 1f);

        if (debugLogs) Debug.Log("[Cover] Cover'dan çıkıldı.");
    }

    // ===== YARDIMCI =====
    Vector3 HorizontalizeNormal(Vector3 n)
    {
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) return Vector3.forward;
        return n.normalized;
    }

    Vector3 GetSafeSnapTarget(RaycastHit hit)
    {
        // yatay normal ile karakter yüksekliğini koruyarak snap
        Vector3 n = HorizontalizeNormal(hit.normal);
        Vector3 planePoint = new Vector3(hit.point.x, tr.position.y, hit.point.z);
        Vector3 target = planePoint - n * snapDepth;

        // zemine oturt (opsiyonel): aşağı kısa ray
        if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out RaycastHit ghit, 4f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(ghit.point.y - tr.position.y) < 0.5f)
                target.y = ghit.point.y;
        }
        return target;
    }

    void FaceTangent()
    {
        Quaternion look = Quaternion.LookRotation(coverTangent, Vector3.up);
        tr.rotation = Quaternion.Slerp(tr.rotation, look, 1f);
    }

    void SnapTo(Vector3 worldPos)
    {
        Vector3 p = Vector3.Lerp(tr.position, worldPos, 0.35f); // ilk girişte bile yumuşak
        if (rb) rb.MovePosition(p); else tr.position = p;
    }

    // ---- DUVAR DEPENETRASYON KORUMASI ----
    void ResolveWallPenetration()
    {
        // Yalnızca collider varsa çalışır
        if (!capCol && !cc) return;

        // Kapsül geometrisini hesapla (capsulecollider varsa onunla, yoksa CC ile)
        Vector3 center;
        float radius;
        float height;
        int direction; // 0=x,1=y,2=z  (bizde genelde 1)

        if (capCol)
        {
            center = tr.TransformPoint(capCol.center);
            radius = Mathf.Abs(capCol.radius) * Mathf.Max(tr.lossyScale.x, tr.lossyScale.z);
            height = Mathf.Max(capCol.height * tr.lossyScale.y, radius * 2f + 0.01f);
            direction = capCol.direction;
        }
        else // CharacterController
        {
            center = tr.TransformPoint(cc.center);
            radius = Mathf.Abs(cc.radius) * Mathf.Max(tr.lossyScale.x, tr.lossyScale.z);
            height = Mathf.Max(cc.height * tr.lossyScale.y, radius * 2f + 0.01f);
            direction = 1;
        }

        Vector3 up = (direction == 0) ? tr.right : (direction == 2) ? tr.forward : tr.up;
        Vector3 p1 = center + up * (height * 0.5f - radius);
        Vector3 p2 = center - up * (height * 0.5f - radius);

        var hits = Physics.OverlapCapsule(p1, p2, radius, coverMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (!h || h.attachedRigidbody == rb) continue;
            if (h.transform.IsChildOf(tr)) continue;

            if (Physics.ComputePenetration(
                    capCol ? (Collider)capCol : (Collider)cc, tr.position, tr.rotation,
                    h, h.transform.position, h.transform.rotation,
                    out Vector3 dir, out float dist))
            {
                // Sadece duvardan DIŞARI bileşeni uygula
                // (dir: bizim kollaydırımızı dışarı iten yön)
                float push = Vector3.Dot(dir, coverNormal);
                Vector3 correction;
                if (push > 0f)
                    correction = coverNormal * (dist + 0.005f);
                else
                    correction = dir.normalized * (dist + 0.005f); // emniyet

                if (rb) rb.position += correction;
                else tr.position += correction;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform t = Application.isPlaying ? (tr ?? transform) : transform;
        float cy = chestY > 0 ? chestY : 1.0f;
        Gizmos.color = Color.cyan;
        Vector3 o = t.position + Vector3.up * cy;
        Gizmos.DrawWireSphere(o + t.forward * Mathf.Min(findCoverDistance, 0.1f), findCoverRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(o, t.forward * findCoverDistance);
    }

    // dışarıdan ihtiyaç olursa
    public bool IsInCover() => inCover;
}
