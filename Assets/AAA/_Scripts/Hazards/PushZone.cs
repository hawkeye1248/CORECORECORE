using MovementRework;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A wind zone that continuously pushes any Rigidbody inside it along its forward (+Z) axis, like the
    /// draft from a wind turbine or fan. The pushed body drifts up to a terminal speed rather than being
    /// flung once. The player is the main target, but it works on any physics body.
    ///
    /// The push region is a box that blows along local +Z. It is stopped by solid geometry in front of
    /// it — in particular a block the player builds — so its box collider automatically shortens to end
    /// at the obstacle, sheltering whoever stands behind it. The pushed body itself never blocks the
    /// wind, so the wind still reaches and pushes it.
    ///
    /// Aim it by rotating the GameObject; the wind always blows along its forward (+Z) axis. The
    /// blocking cast is scale-aware, so scaling the object to size the wind is fine (uniform scale is
    /// the most predictable).
    ///
    /// Setup: put this on the turbine root. A trigger <see cref="BoxCollider"/> is added/forced automatically.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PushZone : MonoBehaviour
    {
        [Header("Wind shape (local space, blows along +Z)")]
        [Tooltip("Width (local X) of the wind box.")]
        [SerializeField] private float width = 1.5f;
        [Tooltip("Height (local Y) of the wind box.")]
        [SerializeField] private float height = 1.5f;
        [Tooltip("Maximum reach of the wind along +Z (in local units) when nothing is blocking it.")]
        [SerializeField] private float maxLength = 8f;
        [Tooltip("Local offset of the source (where the wind starts). Leave at 0 to start at the pivot.")]
        [SerializeField] private Vector3 sourceLocalOffset = Vector3.zero;

        [Header("Push")]
        [Tooltip("How hard the wind pushes. With Acceleration force mode this is mass-independent, so the " +
                 "same value feels the same on any body. Higher = faster drift / stronger shove.")]
        [SerializeField] private float pushStrength = 40f;
        [Tooltip("How the force is applied each physics step. Acceleration ignores the body's mass " +
                 "(recommended); Force scales with it.")]
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [Header("Blocking")]
        [Tooltip("Layers that stop the wind: player-built blocks, walls, terrain, etc. The pushed player " +
                 "is always ignored (so the wind can still reach them) and triggers are ignored too.")]
        [SerializeField] private LayerMask blockingLayers = ~0;

        [Header("Visual (optional)")]
        [Tooltip("Wind particle system emitted from the source along +Z. Particles are stopped at the " +
                 "wind's end by the particle Collision module (auto-set-up below), and emission switches " +
                 "off when the wind is fully blocked. Leave empty to drive visuals elsewhere.")]
        [SerializeField] private ParticleSystem windParticles;
        [Tooltip("On start, configure the particle system's Collision module so particles die when they " +
                 "hit whatever blocks the wind (same layers as Blocking Layers). Turn off to hand-tune it yourself.")]
        [SerializeField] private bool autoSetupParticleCollision = true;

        [Header("Impact effect (optional)")]
        [Tooltip("Spawned where the wind is blocked (e.g. dust/leaves swirling on the block's surface). " +
                 "One instance is reused: it follows the hit point while blocked and is hidden when the " +
                 "wind reaches open air. Leave empty for none.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [Tooltip("Rotate the impact effect so its forward (+Z) points out of the hit surface (back toward " +
                 "the source). Off = always face straight back along the wind.")]
        [SerializeField] private bool orientToSurface = true;

        [Header("Gizmo")]
        [Tooltip("Length of the direction arrow drawn in the editor. Visual only; does not affect the push.")]
        [SerializeField] private float gizmoArrowLength = 3f;

        private BoxCollider _box;
        private float _currentLength;

        private GameObject _impactInstance;
        private bool _blocked;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal = Vector3.up;

        // Slight backset so a block placed flush against the source is still caught by the cast.
        private const float CastBackset = 0.02f;

        // Shared cast buffer; push zones resolve one at a time inside FixedUpdate, so reuse is safe.
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

        private void Reset()
        {
            // Force the collider to a trigger the moment the component is added in the editor.
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;

            if (windParticles != null && autoSetupParticleCollision)
                SetupParticleCollision();

            _currentLength = maxLength;
        }

        private void FixedUpdate()
        {
            _currentLength = ComputeWindLength();
            ApplyWind(_currentLength);
            UpdateImpactEffect();
        }

        private void OnDisable()
        {
            // Don't leave the impact effect showing if the zone gets disabled.
            if (_impactInstance != null) _impactInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_impactInstance != null) Destroy(_impactInstance);
        }

        // OnTriggerStay fires every FixedUpdate while a collider overlaps, which is exactly the cadence
        // we want for a steady, physics-correct push. The box has already been shortened this step, so a
        // body behind a blocking obstacle is outside the trigger and gets no push.
        private void OnTriggerStay(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            body.AddForce(transform.forward * pushStrength, forceMode);
        }

        /// <summary>
        /// Box-casts the wind's cross-section forward and returns how far it reaches before hitting
        /// something solid, expressed as a LOCAL length (matching <see cref="maxLength"/> and the
        /// collider's local size). Our own colliders and the player are skipped and triggers are
        /// ignored, so only real obstacles (built blocks, walls, terrain) cut the wind short.
        ///
        /// The cast runs in world space using the object's real scale, then the world hit distance is
        /// converted back to local units — otherwise any non-1 scale would desync the cast from the
        /// collider (the cast would reach the wrong distance and the collider would be sized wrong).
        /// </summary>
        private float ComputeWindLength()
        {
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Abs(scale.x), sy = Mathf.Abs(scale.y), sz = Mathf.Abs(scale.z);

            Vector3 dir = transform.forward;                              // unit direction (scale-independent)
            Vector3 source = transform.TransformPoint(sourceLocalOffset); // world source position
            Vector3 castOrigin = source - dir * CastBackset;

            // World-space half-extents of the wind's cross-section, and world-space max reach.
            Vector3 halfWorld = new Vector3(
                Mathf.Max(0.001f, width * 0.5f * sx),
                Mathf.Max(0.001f, height * 0.5f * sy),
                0.01f);
            float worldMax = maxLength * sz;

            int count = Physics.BoxCastNonAlloc(castOrigin, halfWorld, dir, HitBuffer, transform.rotation,
                worldMax + CastBackset, blockingLayers, QueryTriggerInteraction.Ignore);

            float nearestWorld = worldMax;
            bool blocked = false;
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = -dir;
            for (int i = 0; i < count; i++)
            {
                Collider col = HitBuffer[i].collider;
                if (col == null) continue;
                if (col.transform.IsChildOf(transform)) continue; // our own model/colliders
                // Never let the pushed player block the wind — it must still reach and push them.
                if (Player.Instance != null && col.attachedRigidbody == Player.Instance.core) continue;

                float d = HitBuffer[i].distance - CastBackset; // distance measured from the source
                if (d >= nearestWorld) continue;

                nearestWorld = d;
                blocked = true;
                if (HitBuffer[i].distance > 1e-4f)
                {
                    // Normal cast hit: use the real surface contact point and normal.
                    hitPoint = HitBuffer[i].point;
                    hitNormal = HitBuffer[i].normal.sqrMagnitude > 0.01f ? HitBuffer[i].normal : -dir;
                }
                else
                {
                    // Initial overlap (block flush at the source): the cast gives no valid point/normal.
                    hitPoint = source;
                    hitNormal = -dir;
                }
            }

            nearestWorld = Mathf.Clamp(nearestWorld, 0f, worldMax);

            _blocked = blocked;
            _hitNormal = hitNormal;
            // Prefer the real contact point; fall back to the wind-axis point at the block distance.
            _hitPoint = blocked && hitPoint != Vector3.zero ? hitPoint : source + dir * nearestWorld;

            // World distance → local length so the collider (sized in local space) ends at the obstacle.
            return sz > 1e-5f ? nearestWorld / sz : nearestWorld;
        }

        private void ApplyWind(float length)
        {
            float len = Mathf.Max(length, 0.001f); // never let the collider collapse to zero size

            // Grow the box out of the source straight along +Z.
            _box.size = new Vector3(width, height, len);
            _box.center = sourceLocalOffset + Vector3.forward * (len * 0.5f);

            if (windParticles != null)
            {
                // Particles are trimmed at the block by their Collision module; here we just stop
                // emitting when the wind is fully blocked so nothing sputters against a flush block.
                bool active = length > 0.01f;
                ParticleSystem.EmissionModule emission = windParticles.emission;
                if (emission.enabled != active) emission.enabled = active;
            }
        }

        /// <summary>
        /// Spawns (once) and positions a reusable impact effect at the point where the wind is blocked,
        /// orienting it out of the hit surface. Hidden whenever the wind reaches open air.
        /// </summary>
        private void UpdateImpactEffect()
        {
            if (impactEffectPrefab == null) return;

            if (_blocked)
            {
                if (_impactInstance == null)
                    _impactInstance = Instantiate(impactEffectPrefab); // one reusable instance, unparented (no inherited scale)

                Transform t = _impactInstance.transform;
                t.position = _hitPoint;
                t.rotation = orientToSurface && _hitNormal.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(_hitNormal)
                    : Quaternion.LookRotation(-transform.forward);

                if (!_impactInstance.activeSelf) _impactInstance.SetActive(true);
            }
            else if (_impactInstance != null && _impactInstance.activeSelf)
            {
                _impactInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Configures the particle Collision module so particles die on contact with whatever stops the
        /// wind. This is what makes the visual end at a built block — no scaling involved. Reuses
        /// <see cref="blockingLayers"/> so the particles collide with exactly the things that block the wind.
        /// </summary>
        private void SetupParticleCollision()
        {
            ParticleSystem.CollisionModule col = windParticles.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.collidesWith = blockingLayers;                  // same things that block the wind
            col.quality = ParticleSystemCollisionQuality.High;  // accurate against arbitrary block colliders
            col.lifetimeLoss = 0f;
            col.dampen = 0.9f;
            col.bounce = 1f;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix; // draw in local space so the box tracks rotation + scale

            // Faint box: full max reach. Solid box: the current (possibly shortened) wind field.
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            DrawWindBox(maxLength);

            float len = Application.isPlaying ? _currentLength : maxLength;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            DrawWindBox(len);

            // Push-direction arrow along +Z, clamped to the current reach so it never pokes past a block.
            DrawArrow(sourceLocalOffset, Vector3.forward, Mathf.Min(gizmoArrowLength, len));

            Gizmos.matrix = prev;
        }

        private void DrawWindBox(float length)
        {
            float len = Mathf.Max(length, 0.001f);
            Vector3 center = sourceLocalOffset + Vector3.forward * (len * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(width, height, len));
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
