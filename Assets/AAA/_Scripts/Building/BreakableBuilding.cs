using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Building
{
    /// <summary>
    /// The shatter system, shared by everything that can break. The intact model lives on this root
    /// (or on non-piece children); the broken debris are child GameObjects, each with its own
    /// Rigidbody + Collider, that start hidden.
    ///
    /// On <see cref="Break"/> the intact renderers/colliders switch off and the debris pieces switch
    /// on so they drop under gravity. They stay solid for <see cref="debrisLifetime"/> seconds, then
    /// fade out over <see cref="fadeDuration"/> seconds before the whole object is destroyed.
    ///
    /// Despite the name nothing here is building-specific — it works on any level object. Break() is
    /// called by destructive hazards (<c>Hazard.TryBreak</c>, i.e. the beam source / energy beam) and
    /// by <c>BreakOnPlayerTouch</c> for props that crumble underfoot.
    /// </summary>
    public class BreakableBuilding : MonoBehaviour
    {
        [Header("Debris")]
        [Tooltip("The broken pieces, hidden until the building breaks. Leave empty to auto-collect " +
                 "every direct child that has a Rigidbody (the falling chunks).")]
        [SerializeField] private List<GameObject> debrisPieces = new List<GameObject>();

        [Tooltip("Optional outward impulse given to each piece on break, for a bit of scatter. " +
                 "0 = the pieces simply fall under gravity.")]
        [SerializeField] private float scatterForce = 0f;

        [Header("Disappearing")]
        [Tooltip("Seconds the debris stays fully visible after the break, before it starts fading.")]
        [SerializeField] private float debrisLifetime = 1f;

        [Tooltip("Seconds the debris takes to fade to invisible once its lifetime is up. " +
                 "0 = it pops out of existence instantly.")]
        [SerializeField] private float fadeDuration = 1f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in/legacy
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int SurfaceTypeId = Shader.PropertyToID("_SurfaceType");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        /// <summary>One colour property of one debris material, plus the RGB it started with.</summary>
        private struct FadeTarget
        {
            public Material Material;
            public int PropertyId;
            public Color Rgb;
        }

        private readonly List<FadeTarget> _fadeTargets = new();
        private readonly List<Material> _ownedMaterials = new();
        private bool _broken;

        /// <summary>True once it has shattered. Lets a countdown (e.g. a warning shake) bail out when
        /// something else — a beam, say — breaks the object first.</summary>
        public bool IsBroken => _broken;

        private void Awake()
        {
            if (debrisPieces.Count == 0) CollectDebris();

            // Guarantee the intact look on spawn, even if a piece was left enabled in the prefab.
            foreach (GameObject piece in debrisPieces)
                if (piece != null) piece.SetActive(false);
        }

        private void OnDestroy()
        {
            // Reading Renderer.materials handed us private copies that Unity won't clean up for us.
            foreach (Material m in _ownedMaterials)
                if (m != null) Destroy(m);
        }

        /// <summary>Treat every direct child that has a Rigidbody as a debris piece.</summary>
        private void CollectDebris()
        {
            foreach (Transform child in transform)
                if (child.GetComponent<Rigidbody>() != null)
                    debrisPieces.Add(child.gameObject);
        }

        /// <summary>
        /// Swap the intact model for the falling debris and start the fade-out timer. Idempotent: a
        /// hazard that keeps overlapping the building only breaks it once.
        /// </summary>
        public void Break()
        {
            if (_broken) return;
            _broken = true;

            HideIntact();
            ReleaseDebris();
            StartCoroutine(FadeThenDestroy());
        }

        /// <summary>Turn off every renderer/collider that belongs to the intact model, not the debris.</summary>
        private void HideIntact()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                if (!IsDebris(r.transform)) r.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                if (!IsDebris(c.transform)) c.enabled = false;
        }

        /// <summary>Show the pieces so their Rigidbodies wake up and fall, optionally scattering them.</summary>
        private void ReleaseDebris()
        {
            foreach (GameObject piece in debrisPieces)
            {
                if (piece == null) continue;
                piece.SetActive(true);
                if (scatterForce > 0f && piece.TryGetComponent(out Rigidbody rb))
                    rb.AddExplosionForce(scatterForce, transform.position, 0f, 0.2f, ForceMode.Impulse);
            }
        }

        /// <summary>Let the rubble lie for a beat, dissolve it, then clean the whole building up.</summary>
        private IEnumerator FadeThenDestroy()
        {
            if (debrisLifetime > 0f) yield return new WaitForSeconds(debrisLifetime);

            if (fadeDuration > 0f)
            {
                PrepareFade();

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    SetDebrisAlpha(1f - elapsed / fadeDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                SetDebrisAlpha(0f);
            }

            Destroy(gameObject);
        }

        /// <summary>Make every debris material alpha-blendable and remember the colours to fade from.</summary>
        private void PrepareFade()
        {
            foreach (GameObject piece in debrisPieces)
            {
                if (piece == null) continue;
                foreach (Renderer r in piece.GetComponentsInChildren<Renderer>())
                {
                    // A chunk that is fading out shouldn't keep casting a fully solid shadow.
                    r.shadowCastingMode = ShadowCastingMode.Off;

                    // Renderer.materials returns per-renderer copies, so the shared asset (and every
                    // other building using it) is never touched.
                    foreach (Material m in r.materials)
                    {
                        _ownedMaterials.Add(m);
                        MakeTransparent(m);
                        AddFadeTarget(m, BaseColorId);
                        AddFadeTarget(m, ColorId);
                    }
                }
            }
        }

        private void AddFadeTarget(Material m, int propertyId)
        {
            if (!m.HasProperty(propertyId)) return;
            _fadeTargets.Add(new FadeTarget { Material = m, PropertyId = propertyId, Rgb = m.GetColor(propertyId) });
        }

        /// <summary>
        /// Flip a material from opaque to alpha-blended, so its alpha channel actually shows. Opaque
        /// materials tend to carry a meaningless alpha (this project's are saved with a = 0), which is
        /// why <see cref="SetDebrisAlpha"/> drives alpha to an absolute value instead of scaling
        /// whatever the material happened to start with.
        /// </summary>
        private static void MakeTransparent(Material m)
        {
            SetIfPresent(m, SurfaceId, 1f);      // 0 = opaque, 1 = transparent
            SetIfPresent(m, SurfaceTypeId, 1f);
            SetIfPresent(m, BlendId, 0f);        // 0 = alpha blend
            SetIfPresent(m, SrcBlendId, (float)BlendMode.SrcAlpha);
            SetIfPresent(m, DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(m, SrcBlendAlphaId, (float)BlendMode.One);
            SetIfPresent(m, DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(m, ZWriteId, 0f);

            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>Sets a float only when the shader declares it, so non-URP shaders stay quiet.</summary>
        private static void SetIfPresent(Material m, int propertyId, float value)
        {
            if (m.HasProperty(propertyId)) m.SetFloat(propertyId, value);
        }

        private void SetDebrisAlpha(float alpha)
        {
            foreach (FadeTarget target in _fadeTargets)
            {
                if (target.Material == null) continue;
                Color c = target.Rgb;
                c.a = alpha;
                target.Material.SetColor(target.PropertyId, c);
            }
        }

        /// <summary>True if <paramref name="t"/> is one of the debris pieces or lives under one.</summary>
        private bool IsDebris(Transform t)
        {
            foreach (GameObject piece in debrisPieces)
                if (piece != null && t.IsChildOf(piece.transform)) return true;
            return false;
        }
    }
}
