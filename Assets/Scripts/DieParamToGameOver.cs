using UnityEngine;

public class DieParamToGameOver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator animator;           // Player Animator
    [SerializeField] GameOverUI gameOverUI;       // Canvas üzerindeki GameOverUI

    [Header("Animator")]
    [SerializeField] string dieParam = "Die";     // TRIGGER adı (bool değil!)
    [SerializeField] string deathStateName = "Die"; // Animator'daki ölüm state adı
    [Range(0f, 1.2f)] public float showAtNormalizedTime = 0.99f;

    bool fired;
    int lastStateHash;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!gameOverUI) gameOverUI = FindObjectOfType<GameOverUI>(true);
        if (animator) lastStateHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        Debug.Log($"[DieParam→GO] init | anim={(animator ? animator.name : "NULL")} | ui={(gameOverUI ? "OK" : "NULL")} | trigger={dieParam} | state={deathStateName}");
    }

    void Update()
    {
        if (!animator || !gameOverUI || fired) return;

        // Sadece state geçişini izliyoruz (trigger okunamaz)
        var info = animator.GetCurrentAnimatorStateInfo(0);
        int curHash = info.shortNameHash;

        // Yeni state Death ise, bitime kadar bekle
        if (curHash != lastStateHash && info.IsName(deathStateName))
        {
            Debug.Log("[DieParam→GO] Death state'e girildi, zaman eşiği bekleniyor…");
            StartCoroutine(WaitNormTimeThenOpen());
        }
        lastStateHash = curHash;
    }

    System.Collections.IEnumerator WaitNormTimeThenOpen()
    {
        // Death state'te kal, normalizedTime eşiğine kadar bekle
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(deathStateName))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < showAtNormalizedTime)
            yield return null;

        OpenNow("Death state threshold");
    }

    void OpenNow(string reason)
    {
        if (fired) return;
        fired = true;
        Debug.Log("[DieParam→GO] GameOverUI.Show() -> " + reason);
        gameOverUI.Show();
    }

    // İsteğe bağlı: Dışarıdan tetiklemek için yardımcı
    public void FireDieTrigger() => animator?.SetTrigger(dieParam);
}
