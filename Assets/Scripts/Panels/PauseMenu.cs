using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuPanel;   // Baþlangýçta INACTIVE
    public GameObject crosshair;        // Opsiyonel

    // --- Tek instance korumasý ---
    static PauseMenu _instance;

    // --- Debounce / tekrar tetiklemeyi önleme ---
    bool isPaused = false;
    int lastToggleFrame = -9999;        // Ayný framede ikinci kez çalýþmayý engeller
    bool inTransition = false;          // Aç/Kapa sýrasýnda kilit

    void Awake()
    {
        // Tek instance
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[PauseMenu] Sahnedeki ikinci PauseMenu devre dýþý býrakýldý.");
            enabled = false;
            return;
        }
        _instance = this;

        // Temiz baþlangýç
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pauseMenuPanel != null)
        {
            // Paneli tamamen kapalý tut
            pauseMenuPanel.SetActive(false);

            // Panel üzerinde varsa CanvasGroup'u bizim dýþýmýzda kimse yönetmesin diye sýfýrla/kullanma
            var cg = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        // Crosshair'in baþlangýç durumuna dokunmuyoruz
    }

    void Update()
    {
        if (inTransition) return;               // Aç/Kapa iþlemi ortasýndaysa bekle
        if (Time.frameCount == lastToggleFrame) return; // Ayný framede tekrar çalýþmasýn

#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        bool pressed = Input.GetKeyDown(KeyCode.Tab);
#endif
        if (pressed)
        {
            lastToggleFrame = Time.frameCount;
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (inTransition) return;
        inTransition = true;
        SetPaused(!isPaused);
        inTransition = false;
    }

    // Buton: Devam Et
    public void ResumeGame()
    {
        if (inTransition) return;
        inTransition = true;
        SetPaused(false);
        inTransition = false;
    }

    // Buton: Oyundan Çýk
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetPaused(bool pause)
    {
        isPaused = pause;

        // Paneli önce görünür yap / sýralamada en üste al
        if (pauseMenuPanel != null)
        {
            if (pause)
            {
                pauseMenuPanel.SetActive(true);
                pauseMenuPanel.transform.SetAsLastSibling(); // öne getir
            }
            else
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        // Crosshair: sadece pause durumuna göre
        if (crosshair != null)
            crosshair.SetActive(!pause);

        // Zaman ve imleç
        if (pause)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            // FPS'e dönüyorsan:
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible   = false;
        }
    }

    void OnApplicationQuit()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}