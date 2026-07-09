using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// Kills any Health that touches this trigger collider while <see cref="active"/> is true.
    /// Reusable building block for hazards: the spike trap only turns it on while the spikes are
    /// out, the energy beam keeps it on the whole time.
    ///
    /// Uses both enter and stay so it still kills when the hazard becomes lethal on top of a
    /// stationary player (spikes popping up), or when a moving hazard sweeps onto the player
    /// (orbiting beam). Kills are idempotent, so repeated stay calls are harmless.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LethalTrigger : MonoBehaviour
    {
        [Tooltip("When false this trigger is harmless. Hazards toggle this (e.g. spikes only kill while extended).")]
        public bool active = true;

        private void Reset()
        {
            // Make sure the collider is a trigger when the component is first added in the editor.
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (active) Hazard.TryKill(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (active) Hazard.TryKill(other);
        }
    }
}
