using UnityEngine;

namespace MovementRework {
    /// <summary>
    /// Kills anything with a Health component that enters this trigger.
    /// Put this on a GameObject with a Collider marked "Is Trigger" (e.g. a large box
    /// under/around the map). Reusable for the player and enemies alike.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        private void Reset()
        {
            // Make sure the collider is a trigger when the component is first added.
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // GetComponentInParent so a child collider (e.g. the model) still finds the Health.
            if (other.GetComponentInParent<Health>() is Health health && !health.IsDead())
            {
                health.KillCharacter();
            }
        }
    }
}
