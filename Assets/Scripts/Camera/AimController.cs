using UnityEngine;
using System.Reflection;

public class AimController : MonoBehaviour
{
    [Header("References")]
    public Player player;                  // sahnedeki Player.cs
    public Animator playerAnimator;        // (opsiyonel)
    public MonoBehaviour cameraController; // TPSCameraSimple componentini sürükle
    public GameObject crosshairUI;         // Canvas > Crosshair Image

    [Header("Input")]
    public KeyCode aimToggleKey = KeyCode.Mouse1; // RMB ile toggle

    [Header("Camera Zoom (hafif)")]
    public float normalFov = 70f;
    public float aimFov = 62f;             // hafif dar
    public float fovLerp = 8f;

    // TPSCameraSimple.distance için hafif yaklaþma
    public float normalDistance = 2.2f;
    public float aimDistance = 2.0f;
    public float distanceLerp = 8f;

    Camera cam;
    FieldInfo distanceField;               // TPSCameraSimple.distance
    bool aiming;                           // dahili toggle durumu

    void Awake()
    {
        cam = Camera.main;
        if (cameraController != null)
            distanceField = cameraController.GetType()
                .GetField("distance", BindingFlags.Instance | BindingFlags.Public);
    }

    void Start()
    {
        // Baþlangýcý "normal" yap
        SetAim(false, immediate: true);
    }

    void Update()
    {
        // RMB'ye basýnca toggle
        if (Input.GetKeyDown(aimToggleKey))
        {
            SetAim(!aiming, immediate: false);
        }

        // Sürekli yumuþak geçiþ (FOV + distance)
        if (cam)
        {
            float targetFov = aiming ? aimFov : normalFov;
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView, targetFov,
                1f - Mathf.Exp(-fovLerp * Time.deltaTime)
            );
        }

        if (cameraController && distanceField != null)
        {
            float cur = (float)distanceField.GetValue(cameraController);
            float target = aiming ? aimDistance : normalDistance;
            cur = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-distanceLerp * Time.deltaTime));
            distanceField.SetValue(cameraController, cur);
        }
    }

    // Toggle'i merkezi olarak yönet
    void SetAim(bool enable, bool immediate)
    {
        aiming = enable;

        // Player'a bildir
        if (player) player.isAiming = aiming;

        // Animator bool (opsiyonel)
        if (playerAnimator) playerAnimator.SetBool("Aiming", aiming);

        // Crosshair sadece niþandayken açýk
        if (crosshairUI) crosshairUI.SetActive(aiming);

        if (immediate)
        {
            // Anýnda geçiþ istenirse deðerleri direkt hedefe ayarla
            if (cam) cam.fieldOfView = aiming ? aimFov : normalFov;

            if (cameraController && distanceField != null)
            {
                distanceField.SetValue(cameraController, aiming ? aimDistance : normalDistance);
            }
        }
    }
}
