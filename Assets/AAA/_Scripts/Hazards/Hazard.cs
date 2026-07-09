using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// Shared kill logic for hazards (spike trap, energy beam, ...). Mirrors KillZone: kills the
    /// Health on the touching collider (or its parent) if it isn't already dead. Keeping it in one
    /// place means every hazard kills the player exactly the same way.
    /// </summary>
    public static class Hazard
    {
        /// <summary>Kills the Health on <paramref name="other"/> (or a parent) if present and alive.</summary>
        public static void TryKill(Collider other)
        {
            // GetComponentInParent so a child collider (e.g. the model) still finds the Health.
            if (other.GetComponentInParent<SimpleHealth>() is SimpleHealth health && !health.IsDead())
            {
                health.KillCharacter();
            }
        }
    }
}
