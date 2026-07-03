using UnityEngine;

namespace Hazards
{
    /// <summary>
    /// The lethal connection drawn between two <see cref="BeamSource"/> spheres. You don't add this
    /// yourself — a BeamSource spawns and configures it at runtime. It draws a line between the two
    /// endpoints every frame and keeps a capsule trigger stretched along that line so the beam kills
    /// the player on contact even as the sources move.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class EnergyBeam : MonoBehaviour
    {
        private Transform _a;
        private Transform _b;
        private LineRenderer _line;
        private CapsuleCollider _collider;

        public void Initialize(Transform a, Transform b, Material material, Color color, float beamWidth, float lethalRadius)
        {
            _a = a;
            _b = b;

            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = _line.endWidth = beamWidth;
            _line.material = material != null ? material : CreateFallbackMaterial();
            _line.startColor = _line.endColor = color;

            // Capsule trigger we stretch along the beam each frame. Direction 1 == local Y axis.
            _collider = gameObject.AddComponent<CapsuleCollider>();
            _collider.isTrigger = true;
            _collider.direction = 1;
            _collider.radius = lethalRadius;

            gameObject.AddComponent<LethalTrigger>().active = true;

            UpdateBeam();
        }

        // LateUpdate so the beam snaps to the sources after their movement has run this frame.
        private void LateUpdate()
        {
            if (_a == null || _b == null) return;
            UpdateBeam();
        }

        private void UpdateBeam()
        {
            Vector3 pa = _a.position;
            Vector3 pb = _b.position;

            _line.SetPosition(0, pa);
            _line.SetPosition(1, pb);

            Vector3 dir = pb - pa;
            float length = dir.magnitude;

            transform.position = (pa + pb) * 0.5f;
            if (length > 0.0001f)
                transform.rotation = Quaternion.FromToRotation(Vector3.up, dir / length);

            // Capsule height spans the full length; clamp so the end caps never invert when sources
            // are closer together than the capsule's own diameter.
            _collider.height = Mathf.Max(length, _collider.radius * 2f);
            _collider.center = Vector3.zero;
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
