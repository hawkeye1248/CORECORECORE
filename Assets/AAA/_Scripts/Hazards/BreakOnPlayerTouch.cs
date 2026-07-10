using System.Collections;
using Building;
using MovementRework;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A crumbling level object: the moment the player touches it, it counts down
    /// <see cref="breakDelay"/> seconds and then shatters through the same
    /// <see cref="BreakableBuilding"/> the energy beam uses — the intact model swaps for falling
    /// debris that fades out and disappears, and the collider goes with it, so a floor treated this
    /// way drops the player through.
    ///
    /// Setup: put this on the GameObject that carries the solid (non-trigger) collider the player
    /// touches, with a <see cref="BreakableBuilding"/> on that object or a parent (the broken pieces
    /// being its children). Nothing here is building-specific — it works on any level object.
    /// </summary>
    public class BreakOnPlayerTouch : MonoBehaviour
    {
        [Tooltip("Seconds between the player touching the object and it shattering. 0 = break on contact.")]
        [SerializeField] private float breakDelay = 0.5f;

        [Tooltip("Only break when the player is on top of it (ignore side/bottom bumps) — a floor that " +
                 "gives way underfoot. Off = any contact sets it off.")]
        [SerializeField] private bool topOnly = false;

        [Header("Warning vibration")]
        [Tooltip("How far the object rattles from its resting place while it counts down, in world " +
                 "units. Keep it small — the player standing on it feels the jitter too. 0 = no shake.")]
        [SerializeField] private float shakeAmplitude = 0.03f;

        [Tooltip("Rattles per second. Higher reads as a more urgent buzz.")]
        [SerializeField] private float shakeFrequency = 25f;

        [Tooltip("Start the shake subtle and build to full amplitude right before it gives way, so the " +
                 "player can read how much time is left. Off = a constant rattle.")]
        [SerializeField] private bool shakeRampsUp = true;

        private BreakableBuilding _breakable;
        private bool _triggered;

        private void Awake()
        {
            // GetComponentInParent so this can sit on a child collider of the breakable root.
            _breakable = GetComponentInParent<BreakableBuilding>();
            if (_breakable == null)
                Debug.LogWarning($"{nameof(BreakOnPlayerTouch)} on '{name}': found no {nameof(BreakableBuilding)} " +
                                 "on this object or a parent, so it can never break.", this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_triggered || _breakable == null) return;
            if (Player.Instance == null || collision.rigidbody != Player.Instance.core) return;

            if (topOnly)
            {
                // On top ⇒ the contact point sits below the player's centre. The same pivot- and
                // normal-independent test JumpPad/BoostPad use, so side bumps don't set it off.
                Vector3 contact = collision.GetContact(0).point;
                if (contact.y >= Player.Instance.core.position.y) return;
            }

            _triggered = true;
            StartCoroutine(BreakAfterDelay());
        }

        /// <summary>Rattle the object for <see cref="breakDelay"/> seconds as a tell, then shatter it.</summary>
        private IEnumerator BreakAfterDelay()
        {
            // Shake the breakable root, so the hidden debris children ride along with the model.
            Transform root = _breakable.transform;
            Vector3 restingPos = root.localPosition;
            bool shakes = shakeAmplitude > 0f;

            float elapsed = 0f;
            while (elapsed < breakDelay)
            {
                // A hazard beat us to it. Stop moving the root: the debris are live Rigidbodies
                // parented under it now, and dragging their parent would teleport them mid-fall.
                if (_breakable.IsBroken) yield break;

                if (shakes)
                {
                    float ramp = shakeRampsUp ? elapsed / breakDelay : 1f;
                    root.localPosition = restingPos + ShakeOffset(ramp);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Settle it back before it shatters, so the debris spawn exactly where the model stood.
            if (shakes) root.localPosition = restingPos;
            _breakable.Break();
        }

        /// <summary>
        /// A smooth, non-repeating jitter. Perlin noise (rather than a sine) keeps the rattle from
        /// looking like a clean oscillation, and sampling each axis at a different offset stops the
        /// three of them moving in lockstep.
        /// </summary>
        private Vector3 ShakeOffset(float ramp)
        {
            float t = Time.time * shakeFrequency;
            var offset = new Vector3(
                Mathf.PerlinNoise(t, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, t) - 0.5f,
                Mathf.PerlinNoise(t, t) - 0.5f);
            return offset * (2f * shakeAmplitude * ramp); // noise is [0,1] ⇒ centred offset is ±0.5
        }
    }
}
