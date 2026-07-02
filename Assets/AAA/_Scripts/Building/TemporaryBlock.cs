using System.Collections;
using UnityEngine;

namespace Building
{
    /// <summary>
    /// Quick-use platform. Once the player places it, it lerps its colour from
    /// <see cref="startColor"/> to <see cref="endColor"/> over <see cref="lifetime"/> seconds as a
    /// visible countdown, then destroys itself — so the player has to place it and jump off fast.
    /// The timer starts from <see cref="OnPlaced"/> (via <see cref="IPlacedBuilding"/>), never on the
    /// build-mode ghost, so the preview doesn't decay.
    /// </summary>
    public class TemporaryBlock : MonoBehaviour, IPlacedBuilding
    {
        [Header("Lifetime")]
        [Tooltip("Seconds the block stays before it disappears.")]
        [SerializeField] private float lifetime = 3f;

        [Header("Colour countdown")]
        [Tooltip("Colour right after placement.")]
        [SerializeField] private Color startColor = Color.white;
        [Tooltip("Colour just before it disappears.")]
        [SerializeField] private Color endColor = Color.red;
        [Tooltip("Also drive the material's emission colour (for blocks that glow).")]
        [SerializeField] private bool affectEmission = false;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in/legacy
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private bool _started;

        public void OnPlaced()
        {
            if (_started) return;
            _started = true;
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            StartCoroutine(DecayRoutine());
        }

        private IEnumerator DecayRoutine()
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                float k = lifetime > 0f ? elapsed / lifetime : 1f;
                ApplyColor(Color.Lerp(startColor, endColor, k));
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void ApplyColor(Color c)
        {
            if (_renderers == null) return;
            foreach (Renderer r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                // Set both so it works whether the material is URP (_BaseColor) or legacy (_Color);
                // an unknown property is simply ignored by the shader.
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                if (affectEmission) _mpb.SetColor(EmissionColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
