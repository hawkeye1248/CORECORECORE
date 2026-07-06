using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A trigger volume that continuously pushes any Rigidbody inside it along a chosen direction,
    /// like the draft from a wind turbine or a fan. The player (a Rigidbody, see
    /// <see cref="MovementRework.Player"/>) is the main target, but it works on any physics body.
    ///
    /// The push is a steady force applied every physics step while the body overlaps, so the body
    /// drifts up to a terminal speed rather than being flung once. Point it with <see cref="pushDirection"/>
    /// and read the on-screen arrow gizmo to see exactly where the wind blows.
    ///
    /// Setup: put this on a GameObject with a Collider (a trigger is added/forced automatically). Size
    /// the collider to the area the wind should fill. With <see cref="useLocalSpace"/> on, rotating this
    /// object aims the wind, so you can point the turbine wherever you like.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PushZone : MonoBehaviour
    {
        [Header("Push")]
        [Tooltip("Direction the wind blows. With Use Local Space on, this is relative to the object's " +
                 "rotation (so rotating the turbine re-aims it); off, it is a fixed world direction.")]
        [SerializeField] private Vector3 pushDirection = Vector3.forward;

        [Tooltip("Interpret Push Direction in this object's local space (rotates with it) instead of world space.")]
        [SerializeField] private bool useLocalSpace = true;

        [Tooltip("How hard the wind pushes. With Acceleration force mode this is mass-independent, so the " +
                 "same value feels the same on any body. Higher = faster drift / stronger shove.")]
        [SerializeField] private float pushStrength = 40f;

        [Tooltip("How the force is applied each physics step. Acceleration ignores the body's mass " +
                 "(recommended); Force scales with it.")]
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [Header("Gizmo")]
        [Tooltip("Length of the direction arrow drawn in the editor. Visual only; does not affect the push.")]
        [SerializeField] private float gizmoArrowLength = 3f;

        private void Reset()
        {
            // Make sure the collider is a trigger the moment the component is added in the editor.
            GetComponent<Collider>().isTrigger = true;
        }

        // OnTriggerStay fires every FixedUpdate while a collider overlaps, which is exactly the cadence
        // we want for a steady, physics-correct push.
        private void OnTriggerStay(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            body.AddForce(WorldPushDirection() * pushStrength, forceMode);
        }

        /// <summary>The normalized push direction in world space, honoring <see cref="useLocalSpace"/>.</summary>
        private Vector3 WorldPushDirection()
        {
            Vector3 dir = pushDirection.sqrMagnitude < 1e-6f ? Vector3.forward : pushDirection.normalized;
            // TransformDirection applies rotation only (ignores position and scale), so the aim stays true.
            return useLocalSpace ? transform.TransformDirection(dir) : dir;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f); // wind blue
            DrawArrow(transform.position, WorldPushDirection(), gizmoArrowLength);
        }

        private void OnDrawGizmosSelected()
        {
            // Outline the wind field so its reach is obvious while tuning.
            if (TryGetComponent<Collider>(out var col))
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
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
