using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Building
{
    /// <summary>
    /// Materialises a block when it is placed: it builds itself up out of nothing, bottom-up, behind
    /// a glowing burn edge, while sand grains swirl around it — the placed-block counterpart of the
    /// sand ghost the player was just aiming with.
    ///
    /// Drop it on any buildable prefab. <see cref="BuildingSystem.Place"/> calls
    /// <see cref="OnPlaced"/> on every <see cref="IPlacedBuilding"/> of the fresh copy, so this runs
    /// on the real block and never on the preview, and it coexists with other placement-aware
    /// components such as <see cref="TemporaryBlock"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockDissolve : MonoBehaviour, IPlacedBuilding
    {
        [Header("Timing")]
        [Tooltip("Seconds the block takes to build itself up. Zero places it solid, with no effect.")]
        [SerializeField] private float duration = 0.8f;

        [Header("Dissolve")]
        [Tooltip("CORE/Block Dissolve. Assign it — a shader only ever found via Shader.Find gets " +
                 "stripped from builds unless it is also in Always Included Shaders.")]
        [SerializeField] private Shader dissolveShader;

        [Tooltip("Colour of the burn edge that runs ahead of the materialising surface. HDR, so " +
                 "Bloom makes it glow like the sand does.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color edgeColor = new Color(2.5f, 1.2f, 0.3f, 1f);

        [Range(0.001f, 0.5f)]
        [Tooltip("How thick the glowing burn edge is.")]
        [SerializeField] private float edgeWidth = 0.08f;

        [Tooltip("Size of the speckle the block dissolves through. High = fine grain, low = big chunks.")]
        [SerializeField] private float noiseScale = 6f;

        [Range(0f, 1f)]
        [Tooltip("0 = the block reassembles as random speckle; 1 = a clean bottom-up sweep. In " +
                 "between gives a ragged front rising up the block, which is usually what you want.")]
        [SerializeField] private float sweep = 0.6f;

        [Header("Sand")]
        [Tooltip("Swirl sand grains around the block while it materialises.")]
        [SerializeField] private bool sandEffect = true;
        [Tooltip("Additive grain material (BlueprintSand.mat). Without it the sand is skipped.")]
        [SerializeField] private Material sandMaterial;
        [SerializeField] private BlueprintSand.Settings sandSettings = new BlueprintSand.Settings
        {
            region = BlueprintSand.SpawnRegion.Volume, // the block coalesces out of a cloud
            grainsPerUnit = 25f,
            lifetime = new Vector2(0.4f, 0.9f),
            fieldStrength = 0.6f,
            driftSpeed = 0.3f
        };

        /// <summary>
        /// The material copies we own. Unity won't collect these for us, and the block can die
        /// mid-effect (a TemporaryBlock running out, a hazard smashing it), which would strand them —
        /// so they're tracked on the component and cleaned up in OnDestroy as well.
        /// </summary>
        private readonly List<Material> _clones = new List<Material>();

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int SweepId = Shader.PropertyToID("_Sweep");
        private static readonly int SweepMinYId = Shader.PropertyToID("_SweepMinY");
        private static readonly int SweepMaxYId = Shader.PropertyToID("_SweepMaxY");

        public void OnPlaced()
        {
            if (duration > 0f && dissolveShader != null) StartCoroutine(MaterialiseRoutine());
        }

        private IEnumerator MaterialiseRoutine()
        {
            var renderers = new List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                if (!(r is ParticleSystemRenderer))
                    renderers.Add(r);

            if (renderers.Count == 0) yield break;

            // The sweep is measured against the whole block, not each renderer, so a multi-part
            // block rises as one piece instead of every part filling up on its own.
            Bounds whole = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) whole.Encapsulate(renderers[i].bounds);

            Material[][] originals = new Material[renderers.Count][];

            for (int i = 0; i < renderers.Count; i++)
            {
                originals[i] = renderers[i].sharedMaterials;
                var swapped = new Material[originals[i].Length];

                for (int m = 0; m < swapped.Length; m++)
                {
                    // Clone the real material and change only its shader: every property the block's
                    // material already carries (_BaseMap, _BaseColor, _Metallic, ...) survives by
                    // name, so the block looks like itself throughout and doesn't pop when the
                    // original comes back at the end.
                    Material source = originals[i][m];
                    var clone = new Material(source) { shader = dissolveShader };

                    CopyShadingKeywords(source, clone);
                    clone.SetColor(EmissionColorId, SourceEmission(source));
                    clone.SetColor(EdgeColorId, edgeColor);
                    clone.SetFloat(EdgeWidthId, edgeWidth);
                    clone.SetFloat(NoiseScaleId, noiseScale);
                    clone.SetFloat(SweepId, sweep);
                    clone.SetFloat(SweepMinYId, whole.min.y);
                    clone.SetFloat(SweepMaxYId, whole.max.y);
                    clone.SetFloat(DissolveAmountId, 1f); // start fully gone

                    swapped[m] = clone;
                    _clones.Add(clone);
                }

                renderers[i].sharedMaterials = swapped;
            }

            BlueprintSand sand = sandEffect
                ? BlueprintSand.Attach(gameObject, sandMaterial, sandSettings)
                : null;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 1 -> 0: the block climbs out of nothing rather than eroding away.
                float amount = 1f - elapsed / duration;
                foreach (Material clone in _clones) clone.SetFloat(DissolveAmountId, amount);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hand the block back its real materials and drop ours.
            for (int i = 0; i < renderers.Count; i++)
                if (renderers[i] != null)
                    renderers[i].sharedMaterials = originals[i];

            ReleaseClones();
            if (sand != null) sand.Release(); // lets the last grains drift out instead of blinking off
        }

        /// <summary>
        /// The emission the source material actually renders with. URP Lit only applies
        /// <c>_EmissionColor</c> when the <c>_EMISSION</c> keyword is on, and materials routinely sit
        /// there with the swatch left at white and the keyword off — the blocks' prototype materials
        /// all do. Our shader has no such gate, so without this the whole block glows solid white
        /// while it materialises and then pops back to normal when its real material returns.
        /// </summary>
        private static Color SourceEmission(Material source)
        {
            if (!source.IsKeywordEnabled("_EMISSION")) return Color.black;
            return source.HasProperty(EmissionColorId) ? source.GetColor(EmissionColorId) : Color.black;
        }

        /// <summary>
        /// Carry over the shading toggles the block's material was authored with, so the dissolve
        /// shades it the same way its real material does and there's nothing to see at the handover.
        /// </summary>
        private static void CopyShadingKeywords(Material source, Material clone)
        {
            foreach (string keyword in ShadingKeywords)
            {
                if (source.IsKeywordEnabled(keyword)) clone.EnableKeyword(keyword);
                else clone.DisableKeyword(keyword);
            }
        }

        private static readonly string[] ShadingKeywords =
        {
            "_SPECULARHIGHLIGHTS_OFF",
            "_ENVIRONMENTREFLECTIONS_OFF"
        };

        private void ReleaseClones()
        {
            foreach (Material clone in _clones)
                if (clone != null)
                    Destroy(clone);

            _clones.Clear();
        }

        private void OnDestroy() => ReleaseClones();
    }
}
