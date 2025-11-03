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
    public LayerMask coverMask;           // "Cover" layer
    public float findCoverDistance = 2.0f;
    public float findCoverRadius = 0.55f;
    public float snapDepth = 0.30f;
    public float maxEnterAngle = 75f;
    public float maxSurfaceSlopeY = 0.35f; // üst yüzeyleri ele

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
    public string leanFloat = "Lean";
    public string movingFloat = "CoverSpeed";
    public string coverDirFloat = "CoverDir";

    [Header("Camera (optional)")]
    public Transform cameraRig;
    public Vector3 cameraLocalOffsetCover = new Vector3(0.15f, 0.05f, 0f);
    public Vector3 cameraLocalOffsetDefault = Vector3.zero;

    [Header("Collider (child)")]
    public CapsuleCollider childCapsule;   // cowboy child’daki kapsül

    [Header("Locks & Stability")]
    public bool lockYInCover = true;       // Y’yi kitler (çökme biter)
    public bool horizontalPushOnly = true; // depenetrasyon düzeltmesi XZ’de
    public float enterGrace = 0.25f;
    public bool disableRootMotionInCover = true;

    [Header("Debug")]
    public bool debugLogs = true;

    // runtime
    bool inCover;
    float coverBaseFootY;                  // cover boyunca sabitlenecek taban Y
    float enterGraceTimer;
    bool savedApplyRootMotion;

    Vector3 coverNormal, coverTangent, velocity;

    Transform tr;
    Rigidbody rb;

    // tarama yükseklikleri
    float chestY = 1.0f, waistY = 0.7f, kneeY = 0.4f;

    void Awake()
    {
        tr = transform;
        rb = GetComponent<Rigidbody>();

        if (cameraRig == null && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            cameraRig = cam.parent != null ? cam.parent : cam;
        }

        if (coverMask.value == 0)
        {
            int l = LayerMask.NameToLayer("Cover");
            if (l >= 0) coverMask = 1 << l;
        }

        if (!childCapsule)
        {
            var all = GetComponentsInChildren<CapsuleCollider>(true);
            foreach (var c in all) { if (c) { childCapsule = c; break; } }
        }

        float approxH = 1.8f;
        if (childCapsule)
            approxH = Mathf.Max(1.4f, childCapsule.height * Mathf.Abs(childCapsule.transform.lossyScale.y));
        chestY = Mathf.Clamp(approxH * 0.55f, 0.8f, 1.5f);
        waistY = Mathf.Clamp(approxH * 0.40f, 0.5f, 1.3f);
        kneeY = Mathf.Clamp(approxH * 0.25f, 0.3f, 1.0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleCoverKey))
        {
            if (inCover) ExitCover();
            else TryEnterCover();
        }
        if (!inCover) return;

        if (enterGraceTimer > 0f) enterGraceTimer -= Time.deltaTime;

        MaintainSnap();

        float h = 0f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(leanLeftKey)) h -= 1f;
        if (Input.GetKey(leanRightKey)) h += 1f;
        h = Mathf.Clamp(h, -1f, 1f);

        SlideAlong(h);

        float lean = 0f;
        if (Input.GetKey(leanLeftKey)) lean -= 1f;
        if (Input.GetKey(leanRightKey)) lean += 1f;
        if (Mathf.Approximately(lean, 0f) && Input.GetKey(aimKey)) lean = 0.6f;
        ApplyLean(lean);

        if (animator)
        {
            animator.SetBool(inCoverBool, true);
            animator.SetFloat(movingFloat, Mathf.Lerp(animator.GetFloat(movingFloat), Mathf.Abs(h), Time.deltaTime * 10f));
            animator.SetFloat(coverDirFloat, Mathf.Lerp(animator.GetFloat(coverDirFloat), h, Time.deltaTime * 10f));
            animator.SetFloat(leanFloat, Mathf.Lerp(animator.GetFloat(leanFloat), lean, Time.deltaTime * 10f));
        }

        ResolveWallPenetrationWithChildCapsule();
    }

    // ENTER
    void TryEnterCover()
    {
        Vector3 baseFoot = GetCapsuleFootWorld();
        Vector3[] origins =
        {
            baseFoot + Vector3.up * chestY,
            baseFoot + Vector3.up * waistY,
            baseFoot + Vector3.up * kneeY
        };

        Vector3 f = tr.forward;
        Vector3[] dirs =
        {
            f,
            Vector3.Slerp(f, tr.right, 0.35f).normalized,
            Vector3.Slerp(f, -tr.right, 0.35f).normalized
        };

        RaycastHit bestHit = default;
        bool found = false; float bestDist = float.MaxValue;

        foreach (var o in origins)
            foreach (var d in dirs)
            {
                if (Physics.SphereCast(o, findCoverRadius, d, out RaycastHit hit, findCoverDistance, coverMask, QueryTriggerInteraction.Ignore))
                {
                    if (Mathf.Abs(hit.normal.y) > maxSurfaceSlopeY) continue;
                    float ang = Vector3.Angle(-hit.normal, tr.forward);
                    if (ang > maxEnterAngle) continue;

                    if (hit.distance < bestDist) { bestDist = hit.distance; bestHit = hit; found = true; }
                }
            }

        if (!found)
        {
            if (debugLogs) Debug.Log("[Cover] Yakında cover yok.");
            return;
        }

        coverNormal = Horizontalize(bestHit.normal);
        coverTangent = Vector3.Cross(Vector3.up, coverNormal).normalized;

        // Y kilitleme: giriş anındaki child kapsül tabanı
        coverBaseFootY = GetCapsuleFootWorld().y;

        Vector3 snapPos = GetSnapXZ(bestHit); // yalnızca XZ
        SnapTo(snapPos);

        FaceTangent();

        inCover = true;
        enterGraceTimer = enterGrace;

        if (animator)
        {
            if (disableRootMotionInCover) { savedApplyRootMotion = animator.applyRootMotion; animator.applyRootMotion = false; }
            animator.SetTrigger("CoverEnter");
            animator.SetBool(inCoverBool, true);
        }
        if (cameraRig) cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, cameraLocalOffsetCover, 1f);

        if (debugLogs) Debug.Log($"[Cover] Enter: {bestHit.collider.name}");
    }

    // STAY
    void MaintainSnap()
    {
        Vector3 baseFoot = GetCapsuleFootWorld();
        Vector3[] origins =
        {
            baseFoot + Vector3.up * chestY,
            baseFoot + Vector3.up * waistY,
            baseFoot + Vector3.up * kneeY
        };

        bool ok = false; RaycastHit lastHit = default;

        foreach (var o in origins)
        {
            if (Physics.SphereCast(o, findCoverRadius * 0.75f, -coverNormal, out RaycastHit hit, 1.0f, coverMask, QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(hit.normal.y) <= maxSurfaceSlopeY) { lastHit = hit; ok = true; break; }
            }
        }
        if (!ok && enterGraceTimer > 0f)
        {
            foreach (var o in origins)
            {
                if (Physics.SphereCast(o, findCoverRadius * 0.75f, coverNormal, out RaycastHit hit2, 1.0f, coverMask, QueryTriggerInteraction.Ignore))
                {
                    if (Mathf.Abs(hit2.normal.y) <= maxSurfaceSlopeY) { lastHit = hit2; ok = true; break; }
                }
            }
            if (!ok) return;
        }

        if (!ok) { ExitCover(); return; }

        Vector3 want = GetSnapXZ(lastHit); // XZ hedefi
        SnapTo(want);

        ResolveWallPenetrationWithChildCapsule();
    }

    // MOVE ALONG
    void SlideAlong(float input)
    {
        if (Mathf.Approximately(input, 0f))
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * friction);
        else
            velocity = Vector3.Lerp(velocity, coverTangent * (input * slideSpeed), Time.deltaTime * friction);

        Vector3 dp = velocity * Time.deltaTime;
        Vector3 newPos = tr.position + ProjectXZ(dp);
        MoveTo(newPos);
    }

    // LEAN
    void ApplyLean(float lean)
    {
        if (!cameraRig) return;
        Vector3 wantedPos = cameraLocalOffsetCover + new Vector3(lean * leanAmount, Mathf.Abs(lean) * leanHeadUp, 0f);
        Quaternion wantedRot = Quaternion.Euler(0f, 0f, -lean * leanRotDeg);
        cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, wantedPos, Time.deltaTime * leanLerp);
        cameraRig.localRotation = Quaternion.Slerp(cameraRig.localRotation, wantedRot, Time.deltaTime * leanLerp);
    }

    // EXIT
    void ExitCover()
    {
        inCover = false;
        velocity = Vector3.zero;

        if (animator)
        {
            animator.SetBool(inCoverBool, false);
            if (disableRootMotionInCover) animator.applyRootMotion = savedApplyRootMotion;
        }
        if (cameraRig) cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, cameraLocalOffsetDefault, 1f);

        if (debugLogs) Debug.Log("[Cover] Exit.");
    }

    // HELPERS ---------------------------------------------------

    Vector3 Horizontalize(Vector3 n) { n.y = 0f; return n.sqrMagnitude < 1e-6f ? Vector3.forward : n.normalized; }

    // yalnızca XZ’de snap hedefi (Y sabit)
    Vector3 GetSnapXZ(RaycastHit hit)
    {
        Vector3 n = Horizontalize(hit.normal);
        Vector3 planePoint = new Vector3(hit.point.x, lockYInCover ? coverBaseFootY : tr.position.y, hit.point.z);
        Vector3 target = planePoint - n * snapDepth;
        target.y = lockYInCover ? coverBaseFootY : tr.position.y;
        return target;
    }

    // child kapsül tabanı (world)
    Vector3 GetCapsuleFootWorld()
    {
        if (!childCapsule) return tr.position;

        Vector3 cWorld = childCapsule.transform.TransformPoint(childCapsule.center);
        Vector3 s = childCapsule.transform.lossyScale;

        float radius = childCapsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        float height = Mathf.Max(childCapsule.height * Mathf.Abs(s.y), radius * 2f + 0.01f);

        int dir = childCapsule.direction; // 0=x,1=y,2=z
        Vector3 up = (dir == 0) ? childCapsule.transform.right :
                     (dir == 2) ? childCapsule.transform.forward :
                                  childCapsule.transform.up;

        float half = Mathf.Max(height * 0.5f - radius, 0f);
        return cWorld - up * half;
    }

    // duvardan depenetrasyon — sadece XZ’ye uygula (Y’ye dokunma)
    void ResolveWallPenetrationWithChildCapsule()
    {
        if (!childCapsule) return;

        Vector3 cWorld = childCapsule.transform.TransformPoint(childCapsule.center);
        Vector3 s = childCapsule.transform.lossyScale;
        float radius = childCapsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        float height = Mathf.Max(childCapsule.height * Mathf.Abs(s.y), radius * 2f + 0.01f);

        int dir = childCapsule.direction;
        Vector3 up = (dir == 0) ? childCapsule.transform.right :
                     (dir == 2) ? childCapsule.transform.forward :
                                  childCapsule.transform.up;

        Vector3 p1 = cWorld + up * (height * 0.5f - radius);
        Vector3 p2 = cWorld - up * (height * 0.5f - radius);

        var hits = Physics.OverlapCapsule(p1, p2, radius, coverMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.attachedRigidbody == rb) continue;
            if (h.transform.IsChildOf(tr)) continue;

            if (Physics.ComputePenetration(
                childCapsule, childCapsule.transform.position, childCapsule.transform.rotation,
                h, h.transform.position, h.transform.rotation,
                out Vector3 dirOut, out float dist))
            {
                Vector3 corr = dirOut.normalized * (dist + 0.003f);
                if (horizontalPushOnly) corr = ProjectXZ(corr);           // Y’yi sıfırla
                if (lockYInCover) corr.y = 0f;                       // güvence
                MoveBy(corr);
            }
        }
    }

    Vector3 ProjectXZ(Vector3 v) => new Vector3(v.x, 0f, v.z);

    void FaceTangent()
    {
        Quaternion look = Quaternion.LookRotation(coverTangent, Vector3.up);
        tr.rotation = look;
    }

    void SnapTo(Vector3 worldPos)
    {
        Vector3 p = Vector3.Lerp(tr.position, worldPos, 0.35f);
        if (lockYInCover) p.y = coverBaseFootY;
        MoveTo(p);
    }

    void MoveTo(Vector3 p)
    {
        if (rb) rb.MovePosition(p);
        else tr.position = p;
    }

    void MoveBy(Vector3 dp)
    {
        if (rb) rb.MovePosition(rb.position + dp);
        else tr.position += dp;
    }

    void OnDrawGizmosSelected()
    {
        Transform t = Application.isPlaying ? (tr ?? transform) : transform;
        float cy = chestY > 0 ? chestY : 1.0f;

        Vector3 baseFoot = Application.isPlaying ? GetCapsuleFootWorld() :
            (GetComponentInChildren<CapsuleCollider>() ?
                GetComponentInChildren<CapsuleCollider>().transform.position : t.position);

        Gizmos.color = Color.cyan;
        Vector3 o = baseFoot + Vector3.up * cy;
        Gizmos.DrawWireSphere(o + t.forward * Mathf.Min(findCoverDistance, 0.1f), findCoverRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(o, t.forward * findCoverDistance);
    }

    public bool IsInCover() => inCover;
}
