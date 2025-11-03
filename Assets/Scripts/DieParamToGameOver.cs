using UnityEngine;

public class DieParamToGameOver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator animator;           // Player Animator
    [SerializeField] GameOverUI gameOverUI;       // Canvas üzerindeki GameOverUI

    [Header("Animator Param")]
    [SerializeField] string dieParam = "die";     // senin param adýn
    [SerializeField] bool dieIsBool = true;     // bool ise TRUE, trigger ise FALSE

    [Header("Death State")]
    [SerializeField] string deathStateName = "Death"; // Animator’daki ölüm state adý
    [Range(0f, 1.2f)] public float showAtNormalizedTime = 0.99f;

    [Header("Hýzlý Açýlýþ (bool ise)")]
    public bool openImmediatelyOnBoolTrue = true; // bool true olur olmaz aç

    int dieHash;
    bool prevBool;
    bool fired;
    int lastStateHash;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!gameOverUI) gameOverUI = FindObjectOfType<GameOverUI>(true);
        dieHash = Animator.StringToHash(dieParam);

        if (animator)
            lastStateHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

        Debug.Log($"[DieParam] Init | animator={(animator ? animator.name : "NULL")} | gameOverUI={(gameOverUI ? "OK" : "NULL")} | die={dieParam} (bool={dieIsBool}) | state={deathStateName}");
    }

    void Update()
    {
        if (!animator || !gameOverUI || fired) return;

        // 1) Bool ise: false->true’yu yakala ve istersen anýnda aç
        if (dieIsBool)
        {
            bool now = false;
            try { now = animator.GetBool(dieHash); } catch { /* param bool deðilse */ }

            if (!prevBool && now)
            {
                Debug.Log("[DieParam] die bool TRUE oldu");
                if (openImmediatelyOnBoolTrue)
                    OpenNow("bool TRUE (immediate)");
                else
                    StartCoroutine(WaitDeathAndTimeThenOpen());
            }
            prevBool = now;
        }

        // 2) Trigger veya genel: Death state’e geçiþ ve %99 eþiðini bekle
        var info = animator.GetCurrentAnimatorStateInfo(0);
        int curHash = info.shortNameHash;
        if (curHash != lastStateHash && info.IsName(deathStateName))
        {
            Debug.Log("[DieParam] Death state'e girildi");
            StartCoroutine(WaitDeathAndTimeThenOpen());
        }
        lastStateHash = curHash;
    }

    System.Collections.IEnumerator WaitDeathAndTimeThenOpen()
    {
        // Death state’e girene dek bekle
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(deathStateName))
            yield return null;

        // Animasyon sonuna yakýn bekle
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < showAtNormalizedTime)
            yield return null;

        OpenNow("Death state normalizedTime threshold");
    }

    void OpenNow(string reason)
    {
        if (fired) return;
        fired = true;
        Debug.Log("[DieParam] GameOverUI.Show() -> " + reason);
        gameOverUI.Show();
    }
}
