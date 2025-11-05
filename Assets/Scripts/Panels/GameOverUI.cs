using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class GameOverUI : MonoBehaviour
{
    [Header("UI Referanslarý")]
    [Tooltip("GameOver panel kökü (INACTIVE baþlamalý).")]
    public GameObject panel;

    [Tooltip("Panel üzerindeki CanvasGroup (fade için). Atamazsan otomatik bulunur/eklenir.")]
    public CanvasGroup canvasGroup;

    [Tooltip("Baþlýk (ÖLDÜN!) - opsiyonel.")]
    public TextMeshProUGUI title;

    [Tooltip("Alt baþlýk (Aslýnda her þey bir tuzakmýþ. Her þeyi seni öldürmek için kral planlamýþ. Halk tarafýndan çok sevildiðin için bir gün onun yerine geçebileceðini düþünmüþ. Prensesi kurtarmak ve bu sefer o krala haddini bildirmek için hazýr mýsýn?) - opsiyonel.")]
    public TextMeshProUGUI subtitle;

    [Tooltip("Vardýrsa hedefçik/crosshair - opsiyonel.")]
    public GameObject crosshair;

    [Header("Davranýþ")]
    [Tooltip("Panel açýlýnca oyunu dondurur.")]
    public bool freezeTime = true;

    [Tooltip("Açýlýþ/kapanýþ fade süresi (saniye).")]
    [Range(0.05f, 2f)] public float fadeDuration = 0.2f;

    [Header("Metinler (boþsa mevcut metne dokunulmaz)")]
    public string titleTextOnShow = "ÖLDÜN!";
    public string subtitleTextOnShow = "Aslýnda her þey bir tuzakmýþ. Her þeyi seni öldürmek için kral planlamýþ. Halk tarafýndan çok sevildiðin için bir gün onun yerine geçebileceðini düþünmüþ. Prensesi kurtarmak ve bu sefer o krala haddini bildirmek için hazýr mýsýn?";

    // Dahili
    bool _visible;
    bool _initialized;

    void Awake()
    {
        SafeInit();
    }

    // ---- Dýþ API ----

    /// <summary>Ölüm anýnda çaðýr.</summary>
    public void Show()
    {
        SafeInit();
        if (_visible) return;
        _visible = true;

        Debug.Log("[GameOverUI] Show()");

        if (crosshair) crosshair.SetActive(false);

        if (panel)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling(); // en üste
        }

        StopAllCoroutines();

        if (freezeTime) Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (title && !string.IsNullOrEmpty(titleTextOnShow))
            title.text = titleTextOnShow;

        if (subtitle && !string.IsNullOrEmpty(subtitleTextOnShow))
            subtitle.text = subtitleTextOnShow;

        if (canvasGroup != null)
            StartCoroutine(FadeCanvas(0f, 1f, true));
        else
            SetInteractable(true);
    }

    /// <summary>Ýstisnai durumlarda gizlemek için.</summary>
    public void Hide()
    {
        if (!_visible) return;
        _visible = false;

        Debug.Log("[GameOverUI] Hide()");

        StopAllCoroutines();

        if (canvasGroup != null)
            StartCoroutine(FadeCanvas(1f, 0f, false));
        else
        {
            SetInteractable(false);
            if (panel) panel.SetActive(false);
        }

        if (freezeTime) Time.timeScale = 1f;

        // Ýstersen tekrar FPS kilidi:
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    /// <summary>“Tekrar Dene” butonu.</summary>
    public void Restart()
    {
        if (freezeTime) Time.timeScale = 1f;
        StopAllCoroutines();

        Debug.Log("[GameOverUI] Restart() -> reload scene");

        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    /// <summary>“Oyundan Çýk” butonu.</summary>
    public void Quit()
    {
        if (freezeTime) Time.timeScale = 1f;

        Debug.Log("[GameOverUI] Quit()");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- Yardýmcýlar ----

    void SafeInit()
    {
        if (_initialized) return;

        // 1) Paneli otomatik bul (atanmamýþsa)
        if (panel == null)
        {
            var t = transform;
            // Önce doðrudan çocukta "GameOverPanel" ara:
            var child = t.Find("GameOverPanel");
            if (child != null) panel = child.gameObject;
            else
            {
                // Daha geniþ arama: sahnedeki tüm GameObjects arasýnda isme göre ilk eþleþme
                var all = GameObject.FindObjectsOfType<RectTransform>(true);
                foreach (var rt in all)
                {
                    if (rt.gameObject.name == "GameOverPanel")
                    {
                        panel = rt.gameObject;
                        break;
                    }
                }
            }
        }

        // 2) Panel hala yoksa hata ver ve çýk
        if (panel == null)
        {
            Debug.LogError("[GameOverUI] Panel atanmadý ve 'GameOverPanel' bulunamadý! Lütfen Inspector'da 'panel' alanýna GameOverPanel'i sürükleyin.");
            return;
        }

        // 3) Yanlýþlýkla Canvas'ýn kendisini panel yapmak: engelle
        if (panel == gameObject && GetComponent<Canvas>() != null)
        {
            Debug.LogError("[GameOverUI] 'panel' alanýna Canvas objesi atanmýþ! Lütfen sadece GameOverPanel'i atayýn. Canvas kapatýlmayacak.");
            // Canvas'ý kapatmayalým:
        }
        else
        {
            // 4) CanvasGroup yoksa ekle + baþlangýç state'i ayarla
            if (canvasGroup == null)
                canvasGroup = panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 5) Paneli kapalý baþlat (sahnede görünmesin)
            panel.SetActive(false);
        }

        _initialized = true;

        Debug.Log("[GameOverUI] Init OK | panel=" + panel.name + " | hasCanvasGroup=" + (canvasGroup != null));
    }

    System.Collections.IEnumerator FadeCanvas(float from, float to, bool enableAtEnd)
    {
        if (canvasGroup == null) yield break;

        SetInteractable(false);
        canvasGroup.alpha = from;

        float t = 0f;
        while (t < fadeDuration)
        {
            // timeScale=0 iken de akmasý için
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = to;

        if (enableAtEnd)
            SetInteractable(true);
        else
        {
            SetInteractable(false);
            if (panel) panel.SetActive(false);
        }
    }

    void SetInteractable(bool on)
    {
        if (canvasGroup == null) return;
        canvasGroup.interactable = on;
        canvasGroup.blocksRaycasts = on;
    }
}
