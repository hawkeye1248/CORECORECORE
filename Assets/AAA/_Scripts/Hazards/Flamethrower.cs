using MovementRework;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A flamethrower trap: projects a lethal box of fire straight out of its muzzle along local +Z.
    /// The player dies while inside the flame. The flame is stopped by solid geometry in front of it —
    /// in particular a block the player builds — so its box collider automatically shortens to end at
    /// the obstacle, keeping whoever hides behind it safe. The player themselves never blocks the
    /// flame, so it still reaches and kills them.
    ///
    /// Aim it by rotating the GameObject; the flame always fires along its forward (+Z) axis. The
    /// blocking cast is scale-aware, so scaling the object to size the flame is fine (uniform scale is
    /// the most predictable).
    ///
    /// Setup: put this on the flamethrower root. A trigger <see cref="BoxCollider"/> and a
    /// <see cref="LethalTrigger"/> are added automatically. Optionally assign a Flame Visual whose pivot
    /// sits at the muzzle and points +Z; it is scaled along Z to match the (possibly shortened) flame.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Flamethrower : MonoBehaviour
    {
        [Header("Flame shape (local space, fires along +Z)")]
        [Tooltip("Width (local X) of the flame box.")]
        [SerializeField] private float width = 1.5f;
        [Tooltip("Height (local Y) of the flame box.")]
        [SerializeField] private float height = 1.5f;
        [Tooltip("Maximum reach of the flame along +Z (in local units) when nothing is blocking it.")]
        [SerializeField] private float maxLength = 8f;
        [Tooltip("Local offset of the muzzle (where the flame starts). Leave at 0 to start at the pivot.")]
        [SerializeField] private Vector3 muzzleLocalOffset = Vector3.zero;

        [Header("Blocking")]
        [Tooltip("Layers that stop the flame: player-built blocks, walls, terrain, etc. The player is " +
                 "always ignored (so the flame can still reach them) and triggers are ignored too.")]
        [SerializeField] private LayerMask blockingLayers = ~0;

        [Header("Visual (optional)")]
        [Tooltip("Fire particle system emitted from the muzzle along +Z. Particles are stopped at the " +
                 "flame's end by the particle Collision module (auto-set-up below) instead of by scaling, " +
                 "and emission switches off when the flame is fully blocked. Leave empty to drive visuals elsewhere.")]
        [SerializeField] private ParticleSystem flameParticles;
        [Tooltip("On start, configure the particle system's Collision module so particles die when they " +
                 "hit whatever blocks the flame (same layers as Blocking Layers). Turn off to hand-tune it yourself.")]
        [SerializeField] private bool autoSetupParticleCollision = true;

        [Header("Impact effect (optional)")]
        [Tooltip("Spawned where the flame is blocked (e.g. a burst of flames/sparks on the block's " +
                 "surface). One instance is reused: it follows the hit point while blocked and is hidden " +
                 "when the flame reaches open air. Leave empty for none.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [Tooltip("Rotate the impact effect so its forward (+Z) points out of the hit surface (back toward " +
                 "the flame). Off = always face straight back along the flame.")]
        [SerializeField] private bool orientToSurface = true;

        private BoxCollider _box;
        private LethalTrigger _lethal;
        private float _currentLength;

        private GameObject _impactInstance;
        private bool _blocked;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal = Vector3.up;

        // Slight backset so a block placed flush against the muzzle is still caught by the cast.
        private const float CastBackset = 0.02f;

        // Shared cast buffer; flamethrowers resolve one at a time inside FixedUpdate, so reuse is safe.
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

            if (!TryGetComponent(out _lethal))
                _lethal = gameObject.AddComponent<LethalTrigger>();

            if (flameParticles != null && autoSetupParticleCollision)
                SetupParticleCollision();

            _currentLength = maxLength;
        }

        private void FixedUpdate()
        {
            _currentLength = ComputeFlameLength();
            ApplyFlame(_currentLength);
            UpdateImpactEffect();
        }

        private void OnDisable()
        {
            // Don't leave the impact effect showing if the trap gets disabled.
            if (_impactInstance != null) _impactInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_impactInstance != null) Destroy(_impactInstance);
        }

        /// <summary>
        /// Box-casts the flame's cross-section forward and returns how far it reaches before hitting
        /// something solid, expressed as a LOCAL length (matching <see cref="maxLength"/> and the
        /// collider's local size). Our own colliders and the player are skipped and triggers are
        /// ignored, so only real obstacles (built blocks, walls, terrain) cut the flame short.
        ///
        /// The cast runs in world space using the object's real scale, then the world hit distance is
        /// converted back to local units — otherwise any non-1 scale would desync the cast from the
        /// collider (the cast would reach the wrong distance and the collider would be sized wrong).
        /// </summary>
        private float ComputeFlameLength()
        {
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Abs(scale.x), sy = Mathf.Abs(scale.y), sz = Mathf.Abs(scale.z);

            Vector3 dir = transform.forward;                              // unit direction (scale-independent)
            Vector3 muzzle = transform.TransformPoint(muzzleLocalOffset); // world muzzle position
            Vector3 castOrigin = muzzle - dir * CastBackset;

            // World-space half-extents of the flame's cross-section, and world-space max reach.
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
                // Never let the player block the flame — it must still reach and kill them.
                if (Player.Instance != null && col.attachedRigidbody == Player.Instance.core) continue;

                float d = HitBuffer[i].distance - CastBackset; // distance measured from the muzzle
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
                    // Initial overlap (block flush at the muzzle): the cast gives no valid point/normal.
                    hitPoint = muzzle;
                    hitNormal = -dir;
                }
            }

            nearestWorld = Mathf.Clamp(nearestWorld, 0f, worldMax);

            _blocked = blocked;
            _hitNormal = hitNormal;
            // Prefer the real contact point; fall back to the flame-axis point at the block distance.
            _hitPoint = blocked && hitPoint != Vector3.zero ? hitPoint : muzzle + dir * nearestWorld;

            // World distance → local length so the collider (sized in local space) ends at the obstacle.
            return sz > 1e-5f ? nearestWorld / sz : nearestWorld;
        }

        /// <summary>
        /// Spawns (once) and positions a reusable impact effect at the point where the flame is blocked,
        /// orienting it out of the hit surface. Hidden whenever the flame reaches open air.
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

        private void ApplyFlame(float length)
        {
            float len = Mathf.Max(length, 0.001f); // never let the collider collapse to zero size

            // Grow the box out of the muzzle straight along +Z.
            _box.size = new Vector3(width, height, len);
            _box.center = muzzleLocalOffset + Vector3.forward * (len * 0.5f);

            // Lethal only while actually reaching past the muzzle (a flush block makes it safe).
            bool lethal = length > 0.01f;
            _lethal.active = lethal;

            if (flameParticles != null)
            {
                // Particles are trimmed at the block by their Collision module; here we just stop
                // emitting when the flame is fully blocked so nothing sputters against a flush block.
                ParticleSystem.EmissionModule emission = flameParticles.emission;
                if (emission.enabled != lethal) emission.enabled = lethal;
            }
        }

        /// <summary>
        /// Configures the particle Collision module so particles die on contact with whatever stops the
        /// flame. This is what makes the fire visually end at a built block — no scaling involved. Reuses
        /// <see cref="blockingLayers"/> so the particles collide with exactly the things that block the flame.
        /// </summary>
        private void SetupParticleCollision()
        {
            ParticleSystem.CollisionModule col = flameParticles.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.collidesWith = blockingLayers;                  // same things that block the flame
            col.quality = ParticleSystemCollisionQuality.High;  // accurate against arbitrary block colliders
            col.lifetimeLoss = 0f;                              // die on first contact
            col.dampen = 0.9f;
            col.bounce = 0.5f;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix; // draw in local space so the box tracks rotation + scale

            // Faint box: full max reach. Solid box: the current (possibly shortened) flame.
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.25f);
            DrawFlameBox(maxLength);

            float len = Application.isPlaying ? _currentLength : maxLength;
            Gizmos.color = new Color(1f, 0.3f, 0.05f, 0.9f);
            DrawFlameBox(len);

            Gizmos.matrix = prev;
        }

        private void DrawFlameBox(float length)
        {
            float len = Mathf.Max(length, 0.001f);
            Vector3 center = muzzleLocalOffset + Vector3.forward * (len * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(width, height, len));
        }
    }
}
