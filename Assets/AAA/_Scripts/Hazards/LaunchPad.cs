using MovementRework;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A fixed, level-placed jump pad: launches the player straight up when they land on it. This is
    /// the non-buildable twin of <see cref="Building.JumpPad"/> — same launch behavior, but it lives
    /// outside the building system so you can drop it directly into level geometry.
    ///
    /// It bounces the player every time they touch the top and preserves their horizontal momentum.
    ///
    /// Setup: put this on a GameObject with a solid (non-trigger) collider so the player can land on it.
    /// </summary>
    public class LaunchPad : MonoBehaviour
    {
        [Tooltip("Upward speed (units/second) applied to the player on contact. Horizontal momentum " +
                 "is preserved. Tune to the launch height you want.")]
        [SerializeField] private float launchVelocity = 25f;

        [Tooltip("Only bounce when the player lands on the top (ignore side/bottom bumps).")]
        [SerializeField] private bool topOnly = true;

        private void OnCollisionEnter(Collision collision)
        {
            if (Player.Instance == null || collision.rigidbody != Player.Instance.core) return;

            if (topOnly)
            {
                // Landed on top ⇒ the contact point sits below the player's centre. This is
                // pivot- and normal-sign-independent, unlike reading the contact normal.
                Vector3 contact = collision.GetContact(0).point;
                if (contact.y >= Player.Instance.core.position.y) return;
            }

            Player.Instance.JumpPadLaunch(launchVelocity);
        }

        private void OnDrawGizmos()
        {
            // Upward arrow so the launch is obvious at a glance; length scales with launch strength.
            Vector3 origin = transform.position;
            float length = Mathf.Max(0.5f, launchVelocity * 0.1f);
            Vector3 tip = origin + Vector3.up * length;

            Gizmos.color = new Color(0.4f, 1f, 0.5f); // launch green
            Gizmos.DrawLine(origin, tip);

            float headSize = length * 0.2f;
            Vector3 back = tip - Vector3.up * headSize;
            Gizmos.DrawLine(tip, back + Vector3.right * headSize);
            Gizmos.DrawLine(tip, back - Vector3.right * headSize);
            Gizmos.DrawLine(tip, back + Vector3.forward * headSize);
            Gizmos.DrawLine(tip, back - Vector3.forward * headSize);
        }
    }
}
