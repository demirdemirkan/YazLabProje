using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;          // GameOverPanel
    public bool freezeTime = true;

    public void Show()
    {
        if (panel) panel.SetActive(true);
        if (freezeTime) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
        if (freezeTime) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Restart()
    {
        if (freezeTime) Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        if (freezeTime) Time.timeScale = 1f;
        Application.Quit();
    }
}
