using UnityEngine;
using UnityEngine.InputSystem;

namespace Building
{
    /// <summary>
    /// Everything about how the build system feels — preview look, sand, reach, rotation, keys,
    /// snapping — pulled off <see cref="BuildingSystem"/> so it lives in one asset instead of being
    /// re-tuned on the Building Manager in every scene. The catalog of buildables stays on the
    /// component, since that is genuinely per-scene: different levels hand the player different
    /// blocks and different stock counts.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingSettings", menuName = "Building/Building Settings")]
    public class BuildingSettings : ScriptableObject
    {
        [Header("Preview")]
        [Tooltip("Draw the ghost's meshes with the blueprint material. Turn off to hide them and " +
                 "leave only the sand — with both this and Sand Effect off, the ghost is invisible.")]
        [SerializeField] private bool blueprintEffect = true;
        [Tooltip("Translucent material applied to every renderer of the ghost preview.")]
        [SerializeField] private Material blueprintMaterial;

        [Header("Sand")]
        [Tooltip("Drift sand grains over the ghost. Turn off to show the blueprint material alone.")]
        [SerializeField] private bool sandEffect = true;
        [Tooltip("Additive grain material (BlueprintSand.mat). Without it the sand is skipped.")]
        [SerializeField] private Material sandMaterial;
        [SerializeField] private BlueprintSand.Settings sandSettings = new BlueprintSand.Settings();

        [Header("Distance (mouse scroll)")]
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private float distanceStep = 2f;
        [SerializeField] private float startDistance = 15f;

        [Header("Rotation (hold Q / E)")]
        [Tooltip("Degrees per second the blueprint yaws while Q or E is held down.")]
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Keys")]
        [SerializeField] private Key toggleKey = Key.B;
        [SerializeField] private Key rotateLeftKey = Key.Q;
        [SerializeField] private Key rotateRightKey = Key.E;

        [Header("Snapping")]
        [Tooltip("Master switch. When on, the ghost auto-snaps when the crosshair ray hits something.")]
        [SerializeField] private bool enableSnapping = true;
        [Tooltip("Layers the aim ray can snap to. Include Ground (blocks + terrain) and Wall; " +
                 "exclude Player/Weapon/Enemy/DeadEnemy so the ray never snaps to you or a held weapon.")]
        [SerializeField] private LayerMask snapMask = ~0;

        public bool BlueprintEffect => blueprintEffect;
        public Material BlueprintMaterial => blueprintMaterial;

        public bool SandEffect => sandEffect;
        public Material SandMaterial => sandMaterial;
        public BlueprintSand.Settings SandSettings => sandSettings;

        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float DistanceStep => distanceStep;
        public float StartDistance => startDistance;

        public float RotationSpeed => rotationSpeed;

        public Key ToggleKey => toggleKey;
        public Key RotateLeftKey => rotateLeftKey;
        public Key RotateRightKey => rotateRightKey;

        public bool EnableSnapping => enableSnapping;
        public LayerMask SnapMask => snapMask;
    }
}
