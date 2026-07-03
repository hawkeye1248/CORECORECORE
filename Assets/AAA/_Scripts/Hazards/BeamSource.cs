using UnityEngine;

namespace Hazards
{
    /// <summary>Predefined movement patterns a <see cref="BeamSource"/> can follow.</summary>
    public enum SourceMovement
    {
        None,
        Circular,
        BetweenTwoPoints
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

        [Header("Beam visuals")]
        [Tooltip("Optional material for the beam line. Leave empty for a built-in colored line.")]
        [SerializeField] private Material beamMaterial;
        [SerializeField] private Color beamColor = new Color(1f, 0.25f, 0.1f, 1f);
        [SerializeField] private float beamWidth = 0.3f;
        [Tooltip("Radius of the lethal capsule around the beam line.")]
        [SerializeField] private float beamLethalRadius = 0.3f;

        [Header("Movement")]
        [SerializeField] private SourceMovement movement = SourceMovement.None;

        [Header("Circular movement")]
        [Tooltip("Axis the source orbits around. Up = orbit in the horizontal plane.")]
        [SerializeField] private Vector3 circleAxis = Vector3.up;
        [Tooltip("Orbit radius. The source passes through its placed position, which sits on the circle.")]
        [SerializeField] private float circleRadius = 5f;
        [Tooltip("Orbit speed in degrees per second. Negative reverses direction.")]
        [SerializeField] private float circleSpeed = 60f;

        [Header("Between two points (offsets from placed position)")]
        [SerializeField] private Vector3 pointA = Vector3.zero;
        [SerializeField] private Vector3 pointB = new Vector3(0f, 5f, 0f);
        [Tooltip("Travel speed in units per second.")]
        [SerializeField] private float moveSpeed = 3f;

        private Vector3 _startPos;
        private Vector3 _orbitCenter;
        private Vector3 _orbitRight;
        private Vector3 _orbitForward;
        private float _angle;
        private EnergyBeam _beam;

        private void Awake()
        {
            _startPos = transform.position;
            SetupOrbitBasis();
            MakeLethal();
        }

        private void Start()
        {
            // Start (not Awake) so the partner has finished its own Awake first.
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
            }
        }

        private void UpdateCircular()
        {
            _angle += circleSpeed * Mathf.Deg2Rad * Time.deltaTime;
            Vector3 offset = (Mathf.Cos(_angle) * _orbitRight + Mathf.Sin(_angle) * _orbitForward) * circleRadius;
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
            Vector3 axis = circleAxis.sqrMagnitude < 1e-6f ? Vector3.up : circleAxis.normalized;

            // Any vector not parallel to the axis gives us a stable in-plane basis.
            Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            _orbitRight = Vector3.Normalize(Vector3.Cross(axis, reference));
            _orbitForward = Vector3.Cross(axis, _orbitRight);

            // Center the orbit so angle 0 lands exactly on the placed position (no start-of-play jump).
            _orbitCenter = _startPos - _orbitRight * circleRadius;
            _angle = 0f;
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
            _beam.Initialize(transform, partner.transform, beamMaterial, beamColor, beamWidth, beamLethalRadius);
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
            }
        }

        private void DrawCircleGizmo(Vector3 placedPos)
        {
            Vector3 axis = circleAxis.sqrMagnitude < 1e-6f ? Vector3.up : circleAxis.normalized;
            Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Normalize(Vector3.Cross(axis, reference));
            Vector3 forward = Vector3.Cross(axis, right);
            Vector3 center = placedPos - right * circleRadius;

            Gizmos.color = Color.yellow;
            const int segments = 32;
            Vector3 prev = center + right * circleRadius;
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + (Mathf.Cos(a) * right + Mathf.Sin(a) * forward) * circleRadius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
