using MovementRework;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A speed-boost strip, like the boost pads in racing games: while the player runs across it,
    /// it drives them forward faster than they could normally run. Put this on a solid (non-trigger)
    /// floor collider the player runs over — the same kind of setup as <see cref="Building.JumpPad"/>,
    /// but pushing forward instead of up.
    ///
    /// The extra speed is above the player's normal cap, so it bleeds off on its own (via the player's
    /// soft speed cap) once they leave the pad — they shoot off the end and coast back down to normal.
    ///
    /// Aim it by rotating the object: the pad's forward (blue) axis is the launch direction, shown by
    /// the yellow gizmo arrow. Or switch <see cref="direction"/> to boost whichever way the player is
    /// already moving.
    /// </summary>
    public class BoostPad : MonoBehaviour
    {
        public enum BoostDirection
        {
            PadForward,   // Always fire along the pad's forward axis (rotate the pad to aim it).
            PlayerTravel, // Boost along the way the player is already moving (preserves steering).
        }

        [Header("Boost")]
        [Tooltip("Target speed (units/sec) the boost drives the player to. Set it above the player's " +
                 "normal max speed to actually feel like a boost. Only ever speeds the player up.")]
        [SerializeField] private float boostSpeed = 45f;

        [Tooltip("Pad Forward: always fire along this pad's forward (blue) axis — a fixed launch strip. " +
                 "Player Travel: boost whichever way the player is already running (keeps their steering).")]
        [SerializeField] private BoostDirection direction = BoostDirection.PadForward;

        [Tooltip("Re-apply the boost every physics step the player is on the pad, so they stay at boost " +
                 "speed across the whole strip. Off = a single kick the moment they step on.")]
        [SerializeField] private bool continuous = true;

        [Tooltip("Only boost when the player is on top of the pad (ignore side/bottom bumps).")]
        [SerializeField] private bool topOnly = true;

        [Header("Gizmo")]
        [Tooltip("Length of the direction arrow drawn in the editor. Visual only; does not affect the boost.")]
        [SerializeField] private float gizmoArrowLength = 3f;

        private void OnCollisionEnter(Collision collision)
        {
            // One-shot mode kicks once on contact; continuous mode leaves it to OnCollisionStay.
            if (!continuous) TryBoost(collision);
        }

        // OnCollisionStay fires every FixedUpdate while the player rests on the pad, which is the
        // cadence we want to keep re-boosting them along the strip.
        private void OnCollisionStay(Collision collision)
        {
            if (continuous) TryBoost(collision);
        }

        private void TryBoost(Collision collision)
        {
            if (Player.Instance == null || collision.rigidbody != Player.Instance.core) return;

            if (topOnly)
            {
                // On top ⇒ the contact point sits below the player's centre. Same pivot- and
                // normal-independent test JumpPad uses so side bumps don't trigger the boost.
                Vector3 contact = collision.GetContact(0).point;
                if (contact.y >= Player.Instance.core.position.y) return;
            }

            Player.Instance.SpeedBoost(boostSpeed, BoostWorldDirection());
        }

        /// <summary>The world-space direction the boost should drive the player, per <see cref="direction"/>.</summary>
        private Vector3 BoostWorldDirection()
        {
            if (direction == BoostDirection.PlayerTravel && Player.Instance != null)
            {
                Vector3 v = Player.Instance.core.linearVelocity;
                Vector3 horizontal = new Vector3(v.x, 0f, v.z);
                // Fall back to the pad's forward when the player is basically stationary — there's no
                // travel direction to amplify, so send them along the strip instead of nowhere.
                if (horizontal.sqrMagnitude > 0.04f) return horizontal.normalized;
            }
            return transform.forward;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f); // boost yellow
            DrawArrow(transform.position, transform.forward, gizmoArrowLength);
        }

        private void OnDrawGizmosSelected()
        {
            // Outline the pad so its footprint is obvious while placing it.
            if (TryGetComponent<Collider>(out var col))
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
                Bounds b = col.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }

        /// <summary>Draws a 3D arrow (line plus a four-barb head) from <paramref name="origin"/>.</summary>
        private static void DrawArrow(Vector3 origin, Vector3 direction, float length)
        {
            if (direction.sqrMagnitude < 1e-6f || length <= 0f) return;

            Vector3 dir = direction.normalized;
            Vector3 tip = origin + dir * length;
            Gizmos.DrawLine(origin, tip);

            // Two perpendicular axes to splay the arrowhead barbs around the shaft.
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Normalize(Vector3.Cross(dir, up));
            Vector3 forward = Vector3.Cross(right, dir);

            float headSize = length * 0.2f;
            Vector3 back = tip - dir * headSize;
            Gizmos.DrawLine(tip, back + right * headSize);
            Gizmos.DrawLine(tip, back - right * headSize);
            Gizmos.DrawLine(tip, back + forward * headSize);
            Gizmos.DrawLine(tip, back - forward * headSize);
        }
    }
}
