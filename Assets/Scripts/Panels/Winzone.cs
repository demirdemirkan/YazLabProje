using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class WinZone : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Bölgeye girdiðinde kazanmayý tetikleyecek oyuncu tag'i.")]
    public string playerTag = "Player";

    [Header("UI & FX")]
    [Tooltip("Kazanma paneli (Canvas altýndaki WinPanel). Baþlangýçta INACTIVE olmalý.")]
    public GameObject winPanel;

    [Tooltip("WinPanel üzerinde CanvasGroup (fade ve týklanabilirlik için). Opsiyonel ama önerilir.")]
    public CanvasGroup panelCanvasGroup;

    [Tooltip("Panel açýlýþ/kapanýþ süresi (s).")]
    [Range(0.05f, 2f)] public float fadeDuration = 0.25f;

    [Tooltip("Paneli açarken en üste getir (UI sýralamasýnda).")]
    public bool bringToFront = true;

    [Tooltip("Kazanma anýnda çalýnacak fanfar/ses (opsiyonel).")]
    public AudioSource fanfare;

    [Header("Oyun Kontrol")]
    [Tooltip("Kazanýlýnca oyunu dondur (Time.timeScale = 0).")]
    public bool freezeTime = true;

    [Tooltip("Kazanýlýnca imleci serbest býrak.")]
    public bool showCursor = true;

    [Tooltip("Bir kez tetiklensin (ikinci giriþte tetiklemesin).")]
    public bool oneShot = true;

    [Header("Kazanýldýðýnda devre dýþý býrakýlacak bileþenler")]
    [Tooltip("Player.cs, CameraFollow, GunShooter vb. davranýþlarý buraya sürükle.")]
    public Behaviour[] disableOnWin;

    bool won;

    void Awake()
    {
        // UI baþlangýç durumu
        if (winPanel != null)
        {
            if (panelCanvasGroup == null)
                panelCanvasGroup = winPanel.GetComponent<CanvasGroup>();

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            // Paneli kapalý baþlat
            winPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[WinZone] winPanel atanmadý. Inspector'da Canvas/WinPanel'i sürükleyin.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && won) return;
        if (!other.CompareTag(playerTag)) return;
        Win();
    }

    public void Win()
    {
        if (oneShot && won) return;
        won = true;

        // Panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (bringToFront) winPanel.transform.SetAsLastSibling();

            StopAllCoroutines();
            if (panelCanvasGroup != null)
                StartCoroutine(FadeCanvas(0f, 1f, true));
        }

        // Ses
        if (fanfare) fanfare.Play();

        // Davranýþlarý kapat
        if (disableOnWin != null)
        {
            foreach (var b in disableOnWin)
                if (b) b.enabled = false;
        }

        // Zaman & imleç
        if (freezeTime) Time.timeScale = 0f;
        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // --- UI butonlarý ---
    public void Restart()
    {
        if (freezeTime) Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadScene(string sceneName)
    {
        if (freezeTime) Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        if (freezeTime) Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fade helper (timeScale=0'da da akar)
    System.Collections.IEnumerator FadeCanvas(float from, float to, bool enableAtEnd)
    {
        if (panelCanvasGroup == null) yield break;

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.alpha = from;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        panelCanvasGroup.alpha = to;

        panelCanvasGroup.interactable = enableAtEnd;
        panelCanvasGroup.blocksRaycasts = enableAtEnd;
    }

    // Tetik hacmini sahnede görselleþtir
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider bc) Gizmos.DrawCube(bc.center, bc.size);
        else if (col is SphereCollider sc) Gizmos.DrawSphere(sc.center, sc.radius);
        else if (col is CapsuleCollider cc) Gizmos.DrawWireSphere(cc.center, (cc.radius + cc.height * 0.5f) * 0.5f);
    }
}
