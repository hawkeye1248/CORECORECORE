using Building;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// Shared kill logic for hazards (spike trap, energy beam, ...). Mirrors KillZone: kills the
    /// Health on the touching collider (or its parent) if it isn't already dead. Keeping it in one
    /// place means every hazard kills the player exactly the same way.
    ///
    /// It also carries the "shatter a placeable building" path so any hazard can break buildings the
    /// same way (see <see cref="BreakBuildingsInSphere"/> / <see cref="BreakBuildingsInCapsule"/>).
    /// </summary>
    public static class Hazard
    {
        // Reused across every overlap query below. Physics runs single-threaded, so sharing is safe.
        private static readonly Collider[] OverlapBuffer = new Collider[32];

        /// <summary>Kills the Health on <paramref name="other"/> (or a parent) if present and alive.</summary>
        public static void TryKill(Collider other)
        {
            // GetComponentInParent so a child collider (e.g. the model) still finds the Health.
            if (other.GetComponentInParent<SimpleHealth>() is SimpleHealth health && !health.IsDead())
            {
                health.KillCharacter();
            }
        }

        /// <summary>Shatters the <see cref="BreakableBuilding"/> on <paramref name="other"/> (or a parent), if any.</summary>
        public static void TryBreak(Collider other)
        {
            if (other == null) return;
            if (other.GetComponentInParent<BreakableBuilding>() is BreakableBuilding breakable)
                breakable.Break();
        }

        /// <summary>
        /// Breaks every building whose solid collider overlaps the given sphere. Triggers are ignored
        /// so a hazard never detects itself or another hazard's trigger volume — only real buildings.
        /// </summary>
        public static void BreakBuildingsInSphere(Vector3 center, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) TryBreak(OverlapBuffer[i]);
        }

        /// <summary>
        /// Breaks every building whose solid collider overlaps the given capsule. <paramref name="p0"/>
        /// and <paramref name="p1"/> are the centers of the capsule's end spheres (the beam endpoints).
        /// </summary>
        public static void BreakBuildingsInCapsule(Vector3 p0, Vector3 p1, float radius)
        {
            int count = Physics.OverlapCapsuleNonAlloc(p0, p1, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) TryBreak(OverlapBuffer[i]);
        }
    }
}
