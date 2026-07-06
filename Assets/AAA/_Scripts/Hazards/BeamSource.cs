using UnityEngine;

namespace Hazards
{
    /// <summary>Predefined movement patterns a <see cref="BeamSource"/> can follow.</summary>
    public enum SourceMovement
    {
        None,
        Circular,
        BetweenTwoPoints,
        Arc
    }

    /// <summary>
    /// A floating "energy source" sphere. It is lethal to the player on contact, and when connected
    /// to a partner source it spawns a lethal energy beam between the two (see <see cref="EnergyBeam"/>).
    /// Each source can also patrol using a simple predefined movement pattern.
    ///
    /// Setup: put this on a sphere GameObject (its collider is forced to a trigger, one is added if
    /// missing). To make a beam, assign <see cref="partner"/> on at least one of the two sources —
    /// assigning it on both is fine, only one beam is drawn.
    /// </summary>
    public class BeamSource : MonoBehaviour
    {
        [Header("Beam connection")]
        [Tooltip("The other source to connect to. Assigning on one source is enough to spawn the beam.")]
        [SerializeField] private BeamSource partner;
        [Tooltip("Rotate the source so its forward (+Z) points along the beam, toward the partner.")]
        [SerializeField] private bool alignToBeam = true;

        [Header("Beam visuals")]
        [Tooltip("Optional material for the beam line. Leave empty for a built-in colored line.")]
        [SerializeField] private Material beamMaterial;
        [SerializeField] private Color beamColor = new Color(1f, 0.25f, 0.1f, 1f);
        [SerializeField] private float beamWidth = 0.3f;
        [Tooltip("Radius of the lethal capsule around the beam line.")]
        [SerializeField] private float beamLethalRadius = 0.3f;

        [Header("Beam chain (optional prefab tiling)")]
        [Tooltip("If set, the beam tiles these prefab(s) along its length (cycling the list) instead of the line. " +
                 "Leave empty to use the line renderer.")]
        [SerializeField] private GameObject[] beamSegmentPrefabs;
        [Tooltip("Target distance between consecutive instances along the beam, in world units. " +
                 "Set this to roughly the length of one chain link.")]
        [SerializeField] private float beamSegmentSpacing = 1f;
        [Tooltip("Extra roll (degrees) applied to every other instance around the beam axis. " +
                 "90 gives the classic alternating-chain look.")]
        [SerializeField] private float beamSegmentTwist = 0f;
        [Tooltip("Hide the line renderer while prefab tiling is active.")]
        [SerializeField] private bool hideLineWhenSegmented = true;

        [Header("Movement")]
        [SerializeField] private SourceMovement movement = SourceMovement.None;

        [Header("Circular movement")]
        [Tooltip("Axis the source orbits around. Up = orbit in the horizontal plane.")]
        [SerializeField] private Vector3 circleAxis = Vector3.up;
        [Tooltip("Orbit radius. Used only when Center Offset is zero (the center is auto-placed this far along the axis).")]
        [SerializeField] private float circleRadius = 5f;
        [Tooltip("World-space offset from the placed position to the orbit center. Leave at 0 to auto-place the center. " +
                 "When set, the source orbits this point and the radius becomes the distance to it.")]
        [SerializeField] private Vector3 circleCenterOffset = Vector3.zero;
        [Tooltip("Orbit speed in degrees per second. Negative reverses direction.")]
        [SerializeField] private float circleSpeed = 60f;

        [Header("Between two points (offsets from placed position)")]
        [SerializeField] private Vector3 pointA = Vector3.zero;
        [SerializeField] private Vector3 pointB = new Vector3(0f, 5f, 0f);
        [Tooltip("Travel speed in units per second.")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Arc (semi-circular oscillation)")]
        [Tooltip("Axis the arc curves around. Up = the arc lies in the horizontal plane.")]
        [SerializeField] private Vector3 arcAxis = Vector3.up;
        [Tooltip("Radius of the arc. Used only when Center Offset is zero (the pivot is auto-placed this far along the axis).")]
        [SerializeField] private float arcRadius = 5f;
        [Tooltip("World-space offset from the placed position to the arc pivot. Leave at 0 to auto-place the pivot. " +
                 "When set, the source arcs around this point and the radius becomes the distance to it.")]
        [SerializeField] private Vector3 arcCenterOffset = Vector3.zero;
        [Tooltip("Angular span of the arc in degrees. 180 sweeps a full semi-circle.")]
        [SerializeField] private float arcAngle = 180f;
        [Tooltip("Sweep speed in degrees per second (the average pace when Ease In/Out is on).")]
        [SerializeField] private float arcSpeed = 60f;
        [Tooltip("Slow to a stop at each end and accelerate through the middle, like a pendulum. Off = constant speed.")]
        [SerializeField] private bool arcEaseInOut = true;

        private Vector3 _startPos;
        private Vector3 _orbitCenter;
        private Vector3 _orbitRight;
        private Vector3 _orbitForward;
        private float _orbitRadius;
        private Vector3 _arcCenter;
        private Vector3 _arcRight;
        private Vector3 _arcForward;
        private float _arcRadius;
        private float _angle;
        private EnergyBeam _beam;
        private Transform _beamOther;

        private void Awake()
        {
            _startPos = transform.position;
            SetupOrbitBasis();
            SetupArcBasis();
            MakeLethal();
        }

        private void Start()
        {
            // Start (not Awake) so the partner has finished its own Awake first.
            if (partner != null) _beamOther = partner.transform;
            if (ShouldRenderBeam())
                CreateBeam();
        }

        private void OnDestroy()
        {
            if (_beam != null) Destroy(_beam.gameObject);
        }

        private void Update()
        {
            switch (movement)
            {
                case SourceMovement.Circular:
                    UpdateCircular();
                    break;
                case SourceMovement.BetweenTwoPoints:
                    UpdateBetweenPoints();
                    break;
                case SourceMovement.Arc:
                    UpdateArc();
                    break;
            }
        }

        private void LateUpdate()
        {
            // Aim along the beam after movement has settled this frame, so the model
            // stays aligned even as this source and its partner drift apart.
            if (!alignToBeam || _beamOther == null) return;

            Vector3 toOther = _beamOther.position - transform.position;
            if (toOther.sqrMagnitude < 1e-6f) return;

            // Roll around the beam is arbitrary; pick a safe up so LookRotation never degenerates.
            Vector3 up = Mathf.Abs(Vector3.Dot(toOther.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            transform.rotation = Quaternion.LookRotation(toOther, up);
        }

        private void UpdateCircular()
        {
            _angle += circleSpeed * Mathf.Deg2Rad * Time.deltaTime;
            Vector3 offset = (Mathf.Cos(_angle) * _orbitRight + Mathf.Sin(_angle) * _orbitForward) * _orbitRadius;
            transform.position = _orbitCenter + offset;
        }

        private void UpdateBetweenPoints()
        {
            Vector3 a = _startPos + pointA;
            Vector3 b = _startPos + pointB;
            float legLength = Vector3.Distance(a, b);

            // Ping-pong at a constant world speed, independent of how far apart the points are.
            float t = legLength > 0.0001f ? Mathf.PingPong(Time.time * moveSpeed / legLength, 1f) : 0f;
            transform.position = Vector3.Lerp(a, b, t);
        }

        private void UpdateArc()
        {
            // Oscillate the swept angle in [0, span] and back; angle 0 is the placed position.
            float span = arcAngle * Mathf.Deg2Rad;
            if (span < 1e-4f) return;

            // Normalized triangle wave 0->1->0; one end-to-end leg takes arcAngle/arcSpeed seconds.
            float t = Mathf.PingPong(Time.time * arcSpeed / arcAngle, 1f);
            if (arcEaseInOut) t = Mathf.SmoothStep(0f, 1f, t); // zero speed at the ends, faster mid-sweep

            float a = t * span;
            Vector3 offset = (Mathf.Cos(a) * _arcRight + Mathf.Sin(a) * _arcForward) * _arcRadius;
            transform.position = _arcCenter + offset;
        }

        private void MakeLethal()
        {
            if (!TryGetComponent<Collider>(out var col))
                col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;

            if (!TryGetComponent<LethalTrigger>(out var lethal))
                lethal = gameObject.AddComponent<LethalTrigger>();
            lethal.active = true;
        }

        private void SetupOrbitBasis()
        {
            ResolveOrbit(_startPos, circleAxis, circleCenterOffset, circleRadius,
                out _orbitCenter, out _orbitRight, out _orbitForward, out _orbitRadius);
            _angle = 0f;
        }

        private void SetupArcBasis()
        {
            ResolveOrbit(_startPos, arcAxis, arcCenterOffset, arcRadius,
                out _arcCenter, out _arcRight, out _arcForward, out _arcRadius);
        }

        /// <summary>
        /// Resolves the geometry of a circular/arc path so angle 0 lands exactly on
        /// <paramref name="placed"/> (no start-of-play jump). When <paramref name="centerOffset"/> is
        /// non-zero the center is placed there and the radius follows from it; otherwise the center is
        /// pushed <paramref name="fallbackRadius"/> out perpendicular to the axis.
        /// </summary>
        private static void ResolveOrbit(Vector3 placed, Vector3 axisInput, Vector3 centerOffset, float fallbackRadius,
            out Vector3 center, out Vector3 right, out Vector3 forward, out float radius)
        {
            Vector3 axis = axisInput.sqrMagnitude < 1e-6f ? Vector3.up : axisInput.normalized;

            if (centerOffset.sqrMagnitude > 1e-6f)
            {
                // Keep the center in the path's plane so the placed position stays exactly on the circle.
                Vector3 inPlane = centerOffset - Vector3.Dot(centerOffset, axis) * axis;
                center = placed + inPlane;
                radius = inPlane.magnitude;
                // Angle 0 points from the center back to the placed position.
                right = radius > 1e-6f ? -inPlane / radius : PlaneReference(axis);
                forward = Vector3.Cross(axis, right);
                return;
            }

            right = PlaneReference(axis);
            forward = Vector3.Cross(axis, right);
            radius = fallbackRadius;
            center = placed - right * radius;
        }

        /// <summary>
        /// A stable in-plane unit vector perpendicular to <paramref name="axis"/>. Any vector not
        /// parallel to the axis works; we pick one and cross with the axis.
        /// </summary>
        private static Vector3 PlaneReference(Vector3 axis)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            return Vector3.Normalize(Vector3.Cross(axis, reference));
        }

        private bool ShouldRenderBeam()
        {
            if (partner == null) return false;
            // If both sources point at each other, only the lower-id one owns the (single) beam.
            if (partner.partner == this) return GetInstanceID() < partner.GetInstanceID();
            return true;
        }

        private void CreateBeam()
        {
            var beamGo = new GameObject($"EnergyBeam ({name} <-> {partner.name})");
            beamGo.transform.localScale = Vector3.one; // unparented: never inherits source scale
            _beam = beamGo.AddComponent<EnergyBeam>();
            _beam.Initialize(transform, partner.transform, new EnergyBeam.Settings
            {
                material = beamMaterial,
                color = beamColor,
                width = beamWidth,
                lethalRadius = beamLethalRadius,
                segmentPrefabs = beamSegmentPrefabs,
                segmentSpacing = beamSegmentSpacing,
                segmentTwist = beamSegmentTwist,
                hideLineWhenSegmented = hideLineWhenSegmented,
            });

            // Tell the far end about us so it can align too, even if it has no partner assigned.
            if (partner._beamOther == null) partner._beamOther = transform;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _startPos : transform.position;

            Gizmos.color = beamColor;
            if (partner != null)
                Gizmos.DrawLine(transform.position, partner.transform.position);

            switch (movement)
            {
                case SourceMovement.Circular:
                    DrawCircleGizmo(origin);
                    break;
                case SourceMovement.BetweenTwoPoints:
                    Gizmos.color = Color.yellow;
                    Vector3 a = origin + pointA;
                    Vector3 b = origin + pointB;
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawWireSphere(a, 0.2f);
                    Gizmos.DrawWireSphere(b, 0.2f);
                    break;
                case SourceMovement.Arc:
                    DrawArcGizmo(origin);
                    break;
            }
        }

        private void DrawCircleGizmo(Vector3 placedPos)
        {
            ResolveOrbit(placedPos, circleAxis, circleCenterOffset, circleRadius,
                out Vector3 center, out Vector3 right, out Vector3 forward, out float radius);

            Gizmos.color = Color.yellow;
            const int segments = 32;
            Vector3 prev = center + right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + (Mathf.Cos(a) * right + Mathf.Sin(a) * forward) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawWireSphere(center, 0.1f); // orbit center
        }

        private void DrawArcGizmo(Vector3 placedPos)
        {
            ResolveOrbit(placedPos, arcAxis, arcCenterOffset, arcRadius,
                out Vector3 center, out Vector3 right, out Vector3 forward, out float radius);
            float span = arcAngle * Mathf.Deg2Rad;

            Gizmos.color = Color.yellow;
            const int segments = 32;
            Vector3 prev = center + right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * span;
                Vector3 next = center + (Mathf.Cos(a) * right + Mathf.Sin(a) * forward) * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            // Mark the two endpoints (placed position and the far end of the sweep) and the pivot.
            Vector3 endA = center + right * radius;
            Vector3 endB = center + (Mathf.Cos(span) * right + Mathf.Sin(span) * forward) * radius;
            Gizmos.DrawWireSphere(endA, 0.2f);
            Gizmos.DrawWireSphere(endB, 0.2f);
            Gizmos.DrawWireSphere(center, 0.1f);
        }
    }
}
