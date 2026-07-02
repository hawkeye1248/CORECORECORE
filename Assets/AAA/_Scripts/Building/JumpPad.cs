using MovementRework;
using UnityEngine;

namespace Building
{
    /// <summary>
    /// Buildable jump pad: launches the player upward when they land on it. Reusable — it bounces
    /// the player every time they touch the top. Needs a solid (non-trigger) collider so the player
    /// can land on it; put this component on the same GameObject as that collider.
    /// </summary>
    public class JumpPad : MonoBehaviour
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
    }
}
