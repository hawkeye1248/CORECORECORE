using System.Collections.Generic;
using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// The lethal connection drawn between two <see cref="BeamSource"/> spheres. You don't add this
    /// yourself — a BeamSource spawns and configures it at runtime. It keeps a capsule trigger stretched
    /// along the line so the beam kills the player on contact even as the sources move.
    ///
    /// The visual is either a stretched <see cref="LineRenderer"/> (default) or, if
    /// <see cref="Settings.segmentPrefabs"/> is set, a row of prefab instances tiled along the beam's
    /// length (e.g. a chain) — repositioned every frame and pooled as the length changes.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class EnergyBeam : MonoBehaviour
    {
        /// <summary>Everything a <see cref="BeamSource"/> passes in to configure the beam.</summary>
        public struct Settings
        {
            public Material material;
            public Color color;
            public float width;
            public float lethalRadius;

            // Optional prefab tiling. When segmentPrefabs has entries, the beam tiles them along its
            // length (cycling the list) instead of relying on the line.
            public GameObject[] segmentPrefabs;
            public float segmentSpacing;         // target world distance between instances
            public float segmentTwist;           // extra roll (deg) on every other instance
            public bool hideLineWhenSegmented;

            public bool breakBuildings;          // shatter placeable buildings the beam sweeps through
        }

        private Transform _a;
        private Transform _b;
        private LineRenderer _line;
        private CapsuleCollider _collider;

        private GameObject[] _segmentPrefabs;
        private float _segmentSpacing;
        private float _segmentTwist;
        private bool _segmented;
        private readonly List<Transform> _segments = new List<Transform>();

        private bool _breakBuildings;
        private float _lethalRadius;

        public void Initialize(Transform a, Transform b, Settings settings)
        {
            _a = a;
            _b = b;

            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = _line.endWidth = settings.width;
            _line.material = settings.material != null ? settings.material : CreateFallbackMaterial();
            _line.startColor = _line.endColor = settings.color;

            // Capsule trigger we stretch along the beam each frame. Direction 1 == local Y axis.
            _collider = gameObject.AddComponent<CapsuleCollider>();
            _collider.isTrigger = true;
            _collider.direction = 1;
            _collider.radius = settings.lethalRadius;

            _lethalRadius = settings.lethalRadius;
            _breakBuildings = settings.breakBuildings;

            gameObject.AddComponent<LethalTrigger>().active = true;

            SetupSegments(settings);

            UpdateBeam();
        }

        private void SetupSegments(Settings settings)
        {
            _segmentPrefabs = FilterNonNull(settings.segmentPrefabs);
            _segmentSpacing = Mathf.Max(settings.segmentSpacing, 0.01f);
            _segmentTwist = settings.segmentTwist;
            _segmented = _segmentPrefabs.Length > 0;

            if (_segmented && settings.hideLineWhenSegmented)
                _line.enabled = false;
        }

        // LateUpdate so the beam snaps to the sources after their movement has run this frame.
        private void LateUpdate()
        {
            if (_a == null || _b == null) return;
            UpdateBeam();

            // Beam endpoints are the sources; the lethal capsule spans between them.
            if (_breakBuildings)
                Hazard.BreakBuildingsInCapsule(_a.position, _b.position, _lethalRadius);
        }

        private void UpdateBeam()
        {
            Vector3 pa = _a.position;
            Vector3 pb = _b.position;

            if (_line.enabled)
            {
                _line.SetPosition(0, pa);
                _line.SetPosition(1, pb);
            }

            Vector3 dir = pb - pa;
            float length = dir.magnitude;

            transform.position = (pa + pb) * 0.5f;
            if (length > 0.0001f)
                transform.rotation = Quaternion.FromToRotation(Vector3.up, dir / length);

            // Capsule height spans the full length; clamp so the end caps never invert when sources
            // are closer together than the capsule's own diameter.
            _collider.height = Mathf.Max(length, _collider.radius * 2f);
            _collider.center = Vector3.zero;

            if (_segmented)
                UpdateSegments(pa, pb, dir, length);
        }

        private void UpdateSegments(Vector3 pa, Vector3 pb, Vector3 dir, float length)
        {
            // One instance per spacing step, always at least one, spanning both endpoints exactly.
            int count = Mathf.Max(1, Mathf.RoundToInt(length / _segmentSpacing));
            EnsureSegmentCount(count);

            Vector3 forward = length > 1e-4f ? dir / length : Vector3.up;
            // Roll around the beam is arbitrary; pick a safe up so LookRotation never degenerates.
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion baseRot = Quaternion.LookRotation(forward, up);
            Quaternion twistRot = baseRot * Quaternion.AngleAxis(_segmentTwist, Vector3.forward);

            for (int i = 0; i < _segments.Count; i++)
            {
                Transform seg = _segments[i];
                bool active = i < count;
                if (seg.gameObject.activeSelf != active) seg.gameObject.SetActive(active);
                if (!active) continue;

                float t = count > 1 ? i / (float)(count - 1) : 0.5f;
                // Every other link gets the twist, for the classic alternating-chain look.
                bool twisted = _segmentTwist != 0f && (i & 1) == 1;
                seg.SetPositionAndRotation(Vector3.Lerp(pa, pb, t), twisted ? twistRot : baseRot);
            }
        }

        private void EnsureSegmentCount(int count)
        {
            while (_segments.Count < count)
            {
                // Cycle through the prefab list; index is stable because we only ever append.
                GameObject prefab = _segmentPrefabs[_segments.Count % _segmentPrefabs.Length];
                GameObject instance = Instantiate(prefab, transform);
                _segments.Add(instance.transform);
            }
        }

        private static GameObject[] FilterNonNull(GameObject[] source)
        {
            if (source == null) return System.Array.Empty<GameObject>();

            int kept = 0;
            foreach (GameObject go in source) if (go != null) kept++;

            var result = new GameObject[kept];
            int i = 0;
            foreach (GameObject go in source) if (go != null) result[i++] = go;
            return result;
        }

        private static Material CreateFallbackMaterial()
        {
            // Sprites/Default is unlit, respects vertex colors, and ships with URP, so the line shows
            // its color without any project setup. Fall back to URP/Unlit just in case.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return new Material(shader);
        }
    }
}
