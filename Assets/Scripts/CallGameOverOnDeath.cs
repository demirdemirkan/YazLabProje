using UnityEngine;

public class CallGameOverOnDeathEnd : StateMachineBehaviour
{
    [Tooltip("Death state bitmeye yakýn çaðrýlacak method")]
    public string methodName = "OnPlayerDeathAnimationComplete";
    [Range(0.5f, 1.2f)]
    public float normalizedTimeThreshold = 0.98f; // 0.98 ~ son frame

    bool called;

    // Her frame çalýþýr, state süresine göre ilerlemeyi izler
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!called && stateInfo.normalizedTime >= normalizedTimeThreshold)
        {
            called = true;

            // PlayerDeathMinimal’ý bul ve methodu çaðýr
            var comp = animator.GetComponentInParent<PlayerDeath>();
            if (comp != null)
            {
                comp.OnPlayerDeathAnimationComplete();
            }
        }
    }

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        called = false;
    }
}
