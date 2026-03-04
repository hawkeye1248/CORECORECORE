using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public List<AnimationEvent> animationEvents = new List<AnimationEvent>();

    public void OnAnimationEventTriggered(string eventName)
    {
        AnimationEvent matchingEvent = animationEvents.Find(se => se.name == eventName);

        matchingEvent?.OnAnimationEvent?.Invoke();
    }
}
