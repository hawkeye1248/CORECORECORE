using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// A trap that periodically extends a set of spikes out of a box and retracts them on a loop.
    /// The spikes are lethal only while they are extended past <see cref="lethalThreshold"/>, so the
    /// box itself is safe to stand next to between cycles.
    ///
    /// Setup: put this on the trap root. Make the spikes a child object that carries the lethal
    /// collider (a trigger is added automatically if it has none) and assign it to <see cref="spikes"/>.
    /// The spikes' authored local position is treated as the fully-retracted (hidden) pose.
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        private enum Phase { Hidden, Extending, Extended, Retracting }

        [Header("Spikes")]
        [Tooltip("Child object that slides out and in. It carries the lethal trigger collider.")]
        [SerializeField] private Transform spikes;

        [Tooltip("Local direction (in the spikes' parent space) the spikes travel when extending.")]
        [SerializeField] private Vector3 extendAxis = Vector3.up;

        [Tooltip("How far the spikes travel from hidden to fully extended.")]
        [SerializeField] private float extendDistance = 1.5f;

        [Header("Cycle timing (seconds)")]
        [Tooltip("Time fully retracted and safe before the next strike.")]
        [SerializeField] private float hiddenDuration = 1.5f;

        [Tooltip("Time to pop out. Keep small for a snappy strike.")]
        [SerializeField] private float extendTime = 0.08f;

        [Tooltip("Time the spikes stay fully out.")]
        [SerializeField] private float extendedDuration = 1f;

        [Tooltip("Time to pull back in.")]
        [SerializeField] private float retractTime = 0.5f;

        [Tooltip("Delay before the first cycle. Give nearby traps different values to desync them.")]
        [SerializeField] private float startDelay = 0f;

        [Header("Lethality")]
        [Tooltip("Spikes become lethal once extended past this fraction of full travel.")]
        [Range(0f, 1f)]
        [SerializeField] private float lethalThreshold = 0.1f;

        private LethalTrigger _lethal;
        private Vector3 _retractedLocalPos;
        private Phase _phase = Phase.Hidden;
        private float _timer;
        private float _extension; // 0 = hidden, 1 = fully out

        private void Awake()
        {
            if (spikes == null)
            {
                Debug.LogError($"{nameof(SpikeTrap)} on '{name}': no spikes Transform assigned.", this);
                enabled = false;
                return;
            }

            _retractedLocalPos = spikes.localPosition;
            _lethal = EnsureLethalTrigger(spikes.gameObject);

            // A negative timer counts down the start delay before the first Hidden phase elapses.
            _timer = -Mathf.Max(0f, startDelay);
        }

        private void Update()
        {
            Advance();
            ApplyExtension();
        }

        private void Advance()
        {
            _timer += Time.deltaTime;
            if (_timer < 0f) return; // still inside the start delay

            switch (_phase)
            {
                case Phase.Hidden:
                    _extension = 0f;
                    if (_timer >= hiddenDuration) Next(Phase.Extending);
                    break;

                case Phase.Extending:
                    _extension = extendTime <= 0f ? 1f : Mathf.Clamp01(_timer / extendTime);
                    if (_timer >= extendTime) Next(Phase.Extended);
                    break;

                case Phase.Extended:
                    _extension = 1f;
                    if (_timer >= extendedDuration) Next(Phase.Retracting);
                    break;

                case Phase.Retracting:
                    _extension = retractTime <= 0f ? 0f : 1f - Mathf.Clamp01(_timer / retractTime);
                    if (_timer >= retractTime) Next(Phase.Hidden);
                    break;
            }
        }

        private void Next(Phase phase)
        {
            _phase = phase;
            _timer = 0f;
        }

        private void ApplyExtension()
        {
            Vector3 axis = extendAxis.sqrMagnitude < 1e-6f ? Vector3.up : extendAxis.normalized;
            spikes.localPosition = _retractedLocalPos + axis * (extendDistance * _extension);
            _lethal.active = _extension > lethalThreshold;
        }

        private LethalTrigger EnsureLethalTrigger(GameObject go)
        {
            if (!go.TryGetComponent<Collider>(out var col))
            {
                col = go.AddComponent<BoxCollider>();
                Debug.LogWarning($"{nameof(SpikeTrap)} on '{name}': spikes had no Collider; added a " +
                                 "BoxCollider. Size it to cover the spikes.", this);
            }
            col.isTrigger = true;

            if (!go.TryGetComponent<LethalTrigger>(out var lethal))
                lethal = go.AddComponent<LethalTrigger>();
            lethal.active = false; // starts hidden/safe

            return lethal;
        }

        private void OnDrawGizmosSelected()
        {
            if (spikes == null) return;

            // Show the extended pose so you can aim the strike in the editor.
            Vector3 axis = extendAxis.sqrMagnitude < 1e-6f ? Vector3.up : extendAxis.normalized;
            Transform parent = spikes.parent;
            Vector3 worldAxis = parent != null ? parent.TransformDirection(axis) : axis;

            Vector3 from = spikes.position;
            Vector3 to = from + worldAxis.normalized * extendDistance;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(to, 0.15f);
        }
    }
}
