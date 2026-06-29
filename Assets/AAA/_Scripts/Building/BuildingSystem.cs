using System;
using System.Collections.Generic;
using MovementRework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Building
{
    /// <summary>
    /// Survival-game-style placement system. Toggle build mode, see a translucent ghost of the
    /// selected building floating at crosshair center, set its distance with the mouse scroll wheel,
    /// rotate it, and click to place a solid copy. Placement does not require ground contact —
    /// buildings can be dropped in mid-air at the chosen distance.
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem Instance { get; private set; }

        [Header("Catalog (editable in inspector and code)")]
        [Tooltip("Buildings the player can place. Pick with the number keys; shown in the hotbar.")]
        [SerializeField] private List<BuildableItem> buildables = new List<BuildableItem>();

        [Header("Preview")]
        [Tooltip("Translucent material applied to every renderer of the ghost preview.")]
        [SerializeField] private Material blueprintMaterial;

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

        public bool IsBuildModeActive { get; private set; }
        public int SelectedIndex { get; private set; }
        public IReadOnlyList<BuildableItem> Buildables => buildables;

        /// <summary>Fired when build mode is toggled. Argument is the new active state.</summary>
        public event Action<bool> OnBuildModeChanged;
        /// <summary>Fired when the selected building changes. Argument is the new selected index.</summary>
        public event Action<int> OnSelectionChanged;
        /// <summary>Fired when a building's remaining count changes. Argument is the catalog index.</summary>
        public event Action<int> OnCountChanged;

        private GameObject _ghost;
        private Quaternion _baseRotation = Quaternion.identity;
        private float _distance;
        private float _yaw;
        private int _snapYawSteps;
        private Transform _cam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);

            // Seed each building's runtime stock from its inspector limit.
            foreach (BuildableItem item in buildables)
                if (item != null) item.Remaining = item.maxCount;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Don't leave a ghost (or a stuck build state) behind if we get disabled mid-build.
            DestroyGhost();
            if (IsBuildModeActive)
            {
                IsBuildModeActive = false;
                OnBuildModeChanged?.Invoke(false);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                ToggleBuildMode();

            if (!IsBuildModeActive) return;

            HandleSelectionInput();
            HandleDistanceInput();
            UpdateGhost();
            HandlePlacementInput();
        }

        public void ToggleBuildMode()
        {
            bool willActivate = !IsBuildModeActive;

            if (willActivate)
            {
                if (buildables.Count == 0)
                {
                    Debug.LogWarning("[BuildingSystem] Cannot enter build mode: no buildables configured.");
                    return;
                }
                IsBuildModeActive = true;
                SelectedIndex = Mathf.Clamp(SelectedIndex, 0, buildables.Count - 1);
                RebuildGhost();
            }
            else
            {
                IsBuildModeActive = false;
                DestroyGhost();
            }

            OnBuildModeChanged?.Invoke(IsBuildModeActive);
        }

        private void HandleSelectionInput()
        {
            // Number row keys 1..9 map to the first nine catalog entries.
            int count = Mathf.Min(buildables.Count, 9);
            for (int i = 0; i < count; i++)
            {
                Key digit = Key.Digit1 + i;
                if (Keyboard.current[digit].wasPressedThisFrame)
                {
                    Select(i);
                    break;
                }
            }
        }

        /// <summary>Select a building by catalog index. Rebuilds the ghost and notifies listeners.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= buildables.Count) return;
            if (index == SelectedIndex && _ghost != null) return;

            SelectedIndex = index;
            if (IsBuildModeActive) RebuildGhost();
            OnSelectionChanged?.Invoke(SelectedIndex);
        }

        private void HandleDistanceInput()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _distance = Mathf.Clamp(_distance + Mathf.Sign(scroll) * distanceStep, minDistance, maxDistance);
        }

        /// <summary>
        /// Positions/rotates the ghost each frame. One raycast decides between three cases:
        ///   A. ray hits a user-built block  → full snap (flush position + rotation aligned to it).
        ///   B. ray hits anything else        → rotation aligns to it, but position stays free.
        ///   C. ray hits nothing              → free position + free continuous yaw (default).
        /// </summary>
        private void UpdateGhost()
        {
            if (_ghost == null) return;
            Transform cam = GetCamera();
            if (cam == null) return;

            RaycastHit h = default;
            bool hit = enableSnapping &&
                       Physics.Raycast(cam.position, cam.forward, out h, maxDistance, snapMask,
                                       QueryTriggerInteraction.Ignore);
            PlaceableBlock block = hit ? h.collider.GetComponentInParent<PlaceableBlock>() : null;

            HandleRotationInput(snapping: hit);

            // Rotation (set first so the ghost's world bounds reflect it before the position math).
            // Snapping only ever rotates the ghost around Y (90° steps) on top of the prefab's own
            // upright base — it never inherits the target's pitch/roll, so it can't get tilted.
            Quaternion rot = hit
                ? Quaternion.Euler(0f, _snapYawSteps * 90f, 0f) * _baseRotation
                : Quaternion.Euler(0f, _yaw, 0f) * _baseRotation;
            _ghost.transform.rotation = rot;

            // Position.
            bool haveBounds = TryGetWorldBounds(_ghost, out Bounds gB);
            Vector3 pivotToCenter = haveBounds ? gB.center - _ghost.transform.position : Vector3.zero;
            Vector3 pos;
            if (block != null && haveBounds)
            {
                // Case A — full face snap against a player-built block.
                Bounds tB = h.collider.bounds;               // the exact piece the ray hit
                Vector3 n = DominantAxis(h.normal);          // nearest world axis of the hit face
                Vector3 center = tB.center + n * (ExtentAlong(tB, n) + ExtentAlong(gB, n));

                // Center the block on the neighbour's face: keep our value on the normal axis,
                // adopt the target's value on the two tangential axes (equal blocks tile perfectly).
                if (Mathf.Abs(n.x) < 0.5f) center.x = tB.center.x;
                if (Mathf.Abs(n.y) < 0.5f) center.y = tB.center.y;
                if (Mathf.Abs(n.z) < 0.5f) center.z = tB.center.z;

                pos = center + (_ghost.transform.position - gB.center); // world-center → pivot
            }
            else if (hit && haveBounds)
            {
                // Case B — non-player-built surface (terrain / pre-placed building). Slide the ghost
                // up to that surface and no further so it never clips in, keeping it on the player's
                // side. Scrolling closer than the surface still works (we take the nearer of the two).
                Vector3 dir = cam.forward;
                float ghostHalf = Mathf.Abs(dir.x) * gB.extents.x
                                + Mathf.Abs(dir.y) * gB.extents.y
                                + Mathf.Abs(dir.z) * gB.extents.z;
                // Pivot distance at which the ghost's front face just touches the hit surface.
                float maxD = h.distance - (Vector3.Dot(pivotToCenter, dir) + ghostHalf);
                float d = Mathf.Clamp(Mathf.Min(_distance, maxD), 0.1f, maxDistance);
                pos = cam.position + dir * d;
            }
            else
            {
                // Case C — open air: free placement at the scrolled distance.
                pos = cam.position + cam.forward * _distance;
            }
            _ghost.transform.position = pos;
        }

        private void HandleRotationInput(bool snapping)
        {
            if (snapping)
            {
                // Snapped (cases A/B): discrete 90° steps on key press.
                if (Keyboard.current[rotateRightKey].wasPressedThisFrame) _snapYawSteps++;
                if (Keyboard.current[rotateLeftKey].wasPressedThisFrame) _snapYawSteps--;
            }
            else
            {
                // Free (case C): continuous yaw while held.
                float dir = 0f;
                if (Keyboard.current[rotateLeftKey].isPressed) dir -= 1f;
                if (Keyboard.current[rotateRightKey].isPressed) dir += 1f;
                if (dir != 0f) _yaw += dir * rotationSpeed * Time.deltaTime;
            }
        }

        /// <summary>World-axis-aligned bounding box of all renderers under <paramref name="go"/>.</summary>
        private static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { bounds = default; return false; }
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);
            return true;
        }

        /// <summary>Signed unit vector of the world axis closest to <paramref name="v"/>.</summary>
        private static Vector3 DominantAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (ay >= az) return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        /// <summary>Half-size of <paramref name="b"/> along the cardinal <paramref name="axis"/>.</summary>
        private static float ExtentAlong(Bounds b, Vector3 axis)
        {
            return Mathf.Abs(b.extents.x * axis.x)
                 + Mathf.Abs(b.extents.y * axis.y)
                 + Mathf.Abs(b.extents.z * axis.z);
        }

        private void HandlePlacementInput()
        {
            if (Mouse.current == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
                Place();
        }

        private void Place()
        {
            if (_ghost == null) return;
            BuildableItem item = buildables[SelectedIndex];
            if (item == null || item.prefab == null) return;
            if (!item.HasStock) return; // ran out of this building

            // Fresh, fully solid copy (original colliders/materials intact). Ghost stays for more.
            GameObject placed = Instantiate(item.prefab, _ghost.transform.position, _ghost.transform.rotation);

            // Mark it so future placements can full-snap (position + rotation) against it.
            if (placed.GetComponent<PlaceableBlock>() == null)
                placed.AddComponent<PlaceableBlock>();

            // Spend one from the limited stock and let the hotbar update its count.
            if (!item.Unlimited)
            {
                item.Remaining = Mathf.Max(0, item.Remaining - 1);
                OnCountChanged?.Invoke(SelectedIndex);
            }
        }

        private void RebuildGhost()
        {
            DestroyGhost();
            if (SelectedIndex < 0 || SelectedIndex >= buildables.Count) return;

            BuildableItem item = buildables[SelectedIndex];
            if (item == null || item.prefab == null) return;

            _ghost = CreateGhost(item.prefab);
            UpdateGhost();
        }

        private GameObject CreateGhost(GameObject prefab)
        {
            GameObject g = Instantiate(prefab);
            g.name = $"[Ghost] {prefab.name}";

            // Remember the prefab's authored orientation so the player's yaw is applied on top of
            // it instead of replacing it (otherwise a "vertical" prefab would flatten to identity).
            _baseRotation = g.transform.rotation;

            foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            foreach (Rigidbody rb in g.GetComponentsInChildren<Rigidbody>(true))
                rb.isKinematic = true;

            if (blueprintMaterial != null)
            {
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                {
                    // Assign via sharedMaterials so we reference the blueprint asset (no leaked
                    // material instances) and never touch the source prefab's materials.
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = blueprintMaterial;
                    r.sharedMaterials = mats;
                }
            }

            return g;
        }

        private void DestroyGhost()
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
        }

        private Transform GetCamera()
        {
            if (_cam != null) return _cam;
            if (Player.Instance != null) _cam = Player.Instance.GetCamera();
            return _cam;
        }
    }
}
