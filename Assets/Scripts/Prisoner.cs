using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Prisoner : MonoBehaviour
{
    [Header("Animator")]
    public string idleTrigger = "PrisonerIdle"; // Animator'daki trigger adý
    public int layerIndex = 0;

    [Header("Opsiyonel")]
    public bool disableRootMotion = true; // yere çökmesin diye
    public float startDelay = 0f;         // istersen küçük gecikme

    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (disableRootMotion && anim) anim.applyRootMotion = false;
    }

    void OnEnable()
    {
        // Animator ilk frame init olana kadar bir frame beklemek daha stabil
        StartCoroutine(FireTriggerOnce());
    }

    System.Collections.IEnumerator FireTriggerOnce()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        else yield return null; // 1 frame bekle

        if (!anim) yield break;

        // Güvenlik: ayný isimde eski tetik kalmýþsa sýfýrla
        anim.ResetTrigger(idleTrigger);
        anim.SetTrigger(idleTrigger);
    }
}
