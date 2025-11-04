using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("İkinci sahnenin adı (Build Profiles'ta ekli olmalı). Örn: Demo 1")]
    public string gameSceneName = "Demo 1";

    [Header("UI Roots")]
    [Tooltip("Ana ekrandaki başlık (LAST SHERIFF) kök objesi")]
    public GameObject titleRoot;
    [Tooltip("Ana ekrandaki butonları tutan grup (Play/HowTo vb.)")]
    public GameObject buttonGroupRoot;
    [Tooltip("Nasıl Oynanır paneli (içinde sadece yazılar ve Kapat butonu)")]
    public GameObject howToPlayPanel;

    [Header("Optional (UX)")]
    [Tooltip("Panel açılınca seçilecek buton (ör. Kapat)")]
    public GameObject firstSelectedOnHowTo;
    [Tooltip("Paneleten çıkınca seçilecek buton (ör. Oyuna Başla)")]
    public GameObject firstSelectedOnMenu;

    void Awake()
    {
        Time.timeScale = 1f;

        // Başlangıçta panel kapalı, ana menü elemanları açık
        SetHowToState(false);

        // EventSystem yoksa ekranda navigation için sorun olur
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }

    void Update()
    {
        // Esc ile paneli kapat
        if (howToPlayPanel != null && howToPlayPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseHowToPlay();
        }
    }

    // === OnClick'ler ===
    public void PlayGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[MainMenu] gameSceneName boş. Build Profiles'taki sahne adını yaz.");
            return;
        }
        // İkinci sahneyi İSİMLE yüklemek en güvenlisi (index değişse de çalışır)
        SceneManager.LoadScene(gameSceneName);
        // Alternatif (mutlaka 1. index olduğundan eminsen):
        // SceneManager.LoadScene(1);
    }

    public void OpenHowToPlay()
    {
        SetHowToState(true);
        if (firstSelectedOnHowTo != null)
            EventSystem.current?.SetSelectedGameObject(firstSelectedOnHowTo);
    }

    public void CloseHowToPlay()
    {
        SetHowToState(false);
        if (firstSelectedOnMenu != null)
            EventSystem.current?.SetSelectedGameObject(firstSelectedOnMenu);
    }

    // === Yardımcı ===
    private void SetHowToState(bool open)
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(open);

        // Arka planı elleme – Canvas altındaki Background(Image) aynen kalsın.
        // Sadece title ve buton köklerini aç/kapa:
        if (titleRoot != null) titleRoot.SetActive(!open);
        if (buttonGroupRoot != null) buttonGroupRoot.SetActive(!open);

        // Güvenlik: panel açıkken arkaya tıklama gitmesin istiyorsan,
        // HowToPlayPanel üzerindeki Image/CanvasGroup'ta Raycast Target açık olmalı.
    }
}
