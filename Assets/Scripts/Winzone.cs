using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class WinZone : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";   // Oyuncu objenin tag'i

    [Header("UI & FX")]
    public GameObject winPanel;           // "Kýzý kurtardýnýz" paneli
    public AudioSource fanfare;           // Opsiyonel kazanma sesi

    [Header("Oyun Kontrol")]
    public bool freezeTime = true;        // Kazanýnca Time.timeScale = 0
    public bool showCursor = true;        // Ýmleci serbest býrak
    public Behaviour[] disableOnWin;      // Player.cs, CameraFollow, GunShooter vs.

    bool won;

    void OnTriggerEnter(Collider other)
    {
        if (won) return;
        if (!other.CompareTag(playerTag)) return;
        Win();
    }

    public void Win()
    {
        won = true;

        if (winPanel) winPanel.SetActive(true);
        if (fanfare) fanfare.Play();

        foreach (var b in disableOnWin)
            if (b) b.enabled = false;

        if (freezeTime) Time.timeScale = 0f;

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // --- UI butonlarý için yardýmcýlar ---
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
        Application.Quit();
    }

    // Sahne içinde tetik alanýný görmen için
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider bc) Gizmos.DrawCube(bc.center, bc.size);
        else if (col is SphereCollider sc) Gizmos.DrawSphere(sc.center, sc.radius);
    }
}
