using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Building
{
    /// <summary>
    /// Sand layer for the build-mode ghost: fine grains spawn across the block's bounds and drift
    /// slowly through a curl-noise vector field, fading in and out so the block reads as a
    /// shimmering cloud rather than a solid object. Meant to stand in for the lines drawn by the
    /// CORE/Blueprint Wireframe shader — spawn from <see cref="SpawnRegion.Edges"/> and the grains
    /// trace the same twelve edges the shader used to draw.
    ///
    /// The whole ParticleSystem is configured from code rather than authored as a prefab, because
    /// every buildable is the same cube mesh at a different scale: grain count, shape and drift all
    /// have to be re-derived from the bounds each time the ghost is rebuilt. Tune it from the Sand
    /// section of <see cref="BuildingSystem"/>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [DisallowMultipleComponent]
    public class BlueprintSand : MonoBehaviour
    {
        /// <summary>Which part of the block's bounding box grains are born in.</summary>
        public enum SpawnRegion
        {
            /// <summary>The twelve edges — reads like the wireframe this replaces.</summary>
            Edges,
            /// <summary>The six faces, so the block looks sand-blasted.</summary>
            Faces,
            /// <summary>The entire interior, so the block looks like a solid cloud of sand.</summary>
            Volume
        }

        [Serializable]
        public class Settings
        {
            [Header("Shape")]
            [Tooltip("Where grains are born inside the block's bounds.")]
            public SpawnRegion region = SpawnRegion.Edges;

            [Tooltip("Grain density. Per metre of edge for Edges, per square metre for Faces, per " +
                     "cubic metre for Volume — so a big block automatically gets more grains.")]
            public float grainsPerUnit = 60f;

            [Tooltip("Hard ceiling on live grains, so a huge block can't tank the frame rate.")]
            public int maxGrains = 3000;

            [Tooltip("Grain size in world units, picked at random per grain.")]
            public Vector2 grainSize = new Vector2(0.02f, 0.06f);

            [Tooltip("Seconds a grain lives, picked at random per grain.")]
            public Vector2 lifetime = new Vector2(1.5f, 3.5f);

            [Header("Motion")]
            [Tooltip("How fast a grain is flicked away from its spawn point, in units/sec.")]
            public float driftSpeed = 0.08f;

            [Tooltip("Steady wind pushing the whole cloud, in world units/sec. Zero for still air.")]
            public Vector3 wind = Vector3.zero;

            [Tooltip("Strength of the curl-noise vector field the grains ride, in units/sec. This is " +
                     "the swirl — turn it up for a sandstorm, down for a slow drift.")]
            public float fieldStrength = 0.25f;

            [Tooltip("Size of the swirls. Low = big lazy eddies; high = fine, busy turbulence.")]
            public float fieldFrequency = 0.4f;

            [Tooltip("How fast the vector field itself churns, so the flow never looks frozen.")]
            public float fieldScrollSpeed = 0.15f;

            [Tooltip("Downward pull. Slightly positive makes the grains settle like real sand.")]
            public float gravity = 0f;

            [Header("Colour")]
            [Tooltip("Grain colour. Push the HDR intensity above 1 so Bloom makes it glow — the " +
                     "grains are additive, so overlapping ones stack into bright cores.")]
            [ColorUsage(true, true)] public Color colorA = new Color(0.4f, 1.7f, 2.4f, 1f);

            [Tooltip("Second grain colour; each grain picks randomly between the two, which is what " +
                     "keeps the cloud from looking like one flat sheet of dots.")]
            [ColorUsage(true, true)] public Color colorB = new Color(1.6f, 2.2f, 2.6f, 1f);

            [Range(0f, 0.5f)]
            [Tooltip("Fraction of its life a grain spends fading in.")]
            public float fadeIn = 0.15f;

            [Range(0f, 0.5f)]
            [Tooltip("Fraction of its life a grain spends fading out.")]
            public float fadeOut = 0.35f;

            [Header("Space")]
            [Tooltip("On: grains are left behind in the world as you move the ghost, like a trail. " +
                     "Off: the cloud rides with the block, which keeps the shape readable while aiming.")]
            public bool simulateInWorldSpace = false;
        }

        /// <summary>
        /// Spawns the sand layer under <paramref name="ghostRoot"/>, sized to the bounds of every
        /// active renderer beneath it. Returns null when the ghost has nothing to measure.
        /// </summary>
        public static BlueprintSand Attach(GameObject ghostRoot, Material grainMaterial, Settings settings)
        {
            if (ghostRoot == null || grainMaterial == null || settings == null) return null;
            if (!TryGetLocalBounds(ghostRoot, out Bounds local)) return null;

            var go = new GameObject("[Sand]");
            go.transform.SetParent(ghostRoot.transform, false);

            BlueprintSand sand = go.AddComponent<BlueprintSand>(); // pulls in the ParticleSystem
            sand.Build(grainMaterial, settings, local, ghostRoot.transform.lossyScale);
            return sand;
        }

        private void Build(Material grainMaterial, Settings s, Bounds local, Vector3 rootScale)
        {
            // The buildables are a cube mesh stretched by their transform, so this node inherits a
            // non-uniform scale that would smear the grains into ellipses and skew their size. Cancel
            // the parent's scale here and express everything below in world units instead.
            transform.localPosition = local.center;
            transform.localScale = new Vector3(
                SafeInverse(rootScale.x), SafeInverse(rootScale.y), SafeInverse(rootScale.z));

            Vector3 size = Vector3.Scale(local.size, Abs(rootScale)); // the block's true world size

            var ps = GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            // Start mid-flow: without this the block would visibly fill up with sand each time you
            // switch buildable, instead of already shimmering the moment the ghost appears.
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(s.lifetime.x, s.lifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(s.grainSize.x, s.grainSize.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, s.driftSpeed);
            main.startColor = new ParticleSystem.MinMaxGradient(s.colorA, s.colorB);
            main.gravityModifier = s.gravity;
            main.maxParticles = Mathf.Max(1, s.maxGrains);
            main.simulationSpace = s.simulateInWorldSpace
                ? ParticleSystemSimulationSpace.World
                : ParticleSystemSimulationSpace.Local;
            // lossyScale is 1 after the cancellation above, so Hierarchy leaves world units intact.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ShapeFor(s.region);
            shape.scale = size;
            shape.position = Vector3.zero; // this node already sits at the bounds centre
            shape.rotation = Vector3.zero;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = GrainsPerSecond(s, size);

            // The vector field the grains ride. Unity's noise module is curl noise, so the flow is
            // divergence-free: grains swirl and fold through it instead of piling up in sinks.
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = s.fieldStrength > 0f;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.strength = s.fieldStrength;
            noise.frequency = s.fieldFrequency;
            noise.scrollSpeed = s.fieldScrollSpeed;
            noise.damping = false; // keep strength honest in world units when frequency changes
            noise.octaveCount = 2;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = s.wind != Vector3.zero;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = s.wind.x;
            velocity.y = s.wind.y;
            velocity.z = s.wind.z;

            // Fade each grain up and back down, so grains wink in and out rather than popping.
            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient(s.fadeIn, s.fadeOut));

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = grainMaterial;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.None; // additive blending is order-independent
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowRoll = false;

            ps.Play();
        }

        private static ParticleSystemShapeType ShapeFor(SpawnRegion region)
        {
            switch (region)
            {
                case SpawnRegion.Edges: return ParticleSystemShapeType.BoxEdge;
                case SpawnRegion.Faces: return ParticleSystemShapeType.BoxShell;
                default: return ParticleSystemShapeType.Box;
            }
        }

        /// <summary>
        /// Grains/sec for a block of this size. Scaled by the measure of whatever region they spawn
        /// in (edge length / surface area / volume) so density looks the same on a small platform and
        /// on a big wall, instead of the wall looking starved.
        /// </summary>
        private static float GrainsPerSecond(Settings s, Vector3 size)
        {
            float measure;
            switch (s.region)
            {
                case SpawnRegion.Edges:
                    measure = 4f * (size.x + size.y + size.z);
                    break;
                case SpawnRegion.Faces:
                    measure = 2f * (size.x * size.y + size.y * size.z + size.z * size.x);
                    break;
                default:
                    measure = size.x * size.y * size.z;
                    break;
            }

            // Grains die after `lifetime`, so live count settles at rate * averageLifetime. Cap the
            // rate to respect maxGrains rather than letting Unity silently clamp the emission.
            float averageLifetime = Mathf.Max(0.01f, (s.lifetime.x + s.lifetime.y) * 0.5f);
            float rate = Mathf.Max(0f, s.grainsPerUnit) * measure;
            return Mathf.Min(rate, s.maxGrains / averageLifetime);
        }

        private static Gradient BuildFadeGradient(float fadeIn, float fadeOut)
        {
            // Keep the two ramps from crossing over each other on a short-lived grain.
            float up = Mathf.Clamp(fadeIn, 0f, 0.49f);
            float down = Mathf.Clamp(1f - fadeOut, up + 0.01f, 1f);

            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, up),
                    new GradientAlphaKey(1f, down),
                    new GradientAlphaKey(0f, 1f)
                });
            return g;
        }

        /// <summary>
        /// Bounds of the ghost's visible renderers, in <paramref name="root"/>'s local space. Only
        /// active renderers count, so the debris hidden inside a breakable block doesn't blow the
        /// bounds out.
        /// </summary>
        private static bool TryGetLocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;

                Bounds lb = r.localBounds;
                Matrix4x4 toRoot = worldToRoot * r.transform.localToWorldMatrix;

                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? lb.min.x : lb.max.x,
                        (i & 2) == 0 ? lb.min.y : lb.max.y,
                        (i & 4) == 0 ? lb.min.z : lb.max.z);

                    Vector3 p = toRoot.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }

            return any;
        }

        private static float SafeInverse(float v) => Mathf.Abs(v) < 1e-5f ? 1f : 1f / v;

        private static Vector3 Abs(Vector3 v) =>
            new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
