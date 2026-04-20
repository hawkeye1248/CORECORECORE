using UnityEngine;

public class AnimationEventStateBehaviour : StateMachineBehaviour
{
    public string eventName;
    [Range(0f, 1f)] public float triggerTime;

    private bool isFired = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        isFired = false;       
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = stateInfo.normalizedTime % 1f;

        if(!isFired && triggerTime <= currentTime)
        {
            isFired = true;
            NotifyReceiver(animator);
        }       
    }

    void NotifyReceiver(Animator animator)
    {
        AnimationEventReceiver receiver = animator.GetComponent<AnimationEventReceiver>();
        receiver.OnAnimationEventTriggered(eventName);
    }
}
