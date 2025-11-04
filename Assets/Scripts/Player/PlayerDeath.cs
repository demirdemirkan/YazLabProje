using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDeath : MonoBehaviour
{
    [Header("Refs")]
    public Health health;                 // Player'ýn Health komponenti
    public Animator animator;             // Player Animator
    public string dieTrigger = "Die";     // Animator'daki trigger adý
    public GameOverUI gameOverUI;         // <-- C adýmý: Canvas'taki GameOverUI referansý

    [Header("Ölünce kapatýlacak bileþenler")]
    public Behaviour[] disableOnDeath;    // Player.cs, CameraFollow/TPSOrbit, GunShooter, AimController vb.

    private bool dead;

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        if (dead || health == null) return;

        if (health.Current <= 0f)
        {
            dead = true;

            // Kontrolleri kapat
            foreach (var b in disableOnDeath)
                if (b) b.enabled = false;

            // Ölüm animasyonu tetikle
            if (animator && !string.IsNullOrEmpty(dieTrigger))
                animator.SetTrigger(dieTrigger);

            // Paneli animasyon bittiðinde açacaðýz (Animation Event çaðýracak)
        }
    }

    // C adýmý: Death klibinin SON frame’ine Animation Event ekle ve bunu seç
    public void OnPlayerDeathAnimationComplete()
    {
        if (gameOverUI) gameOverUI.Show();
    }
}
