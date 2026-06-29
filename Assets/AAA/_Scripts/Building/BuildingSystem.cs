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

        [Header("Rotation (Q / E)")]
        [SerializeField] private float rotationStep = 15f;

        [Header("Keys")]
        [SerializeField] private Key toggleKey = Key.B;
        [SerializeField] private Key rotateLeftKey = Key.Q;
        [SerializeField] private Key rotateRightKey = Key.E;

        public bool IsBuildModeActive { get; private set; }
        public int SelectedIndex { get; private set; }
        public IReadOnlyList<BuildableItem> Buildables => buildables;

        /// <summary>Fired when build mode is toggled. Argument is the new active state.</summary>
        public event Action<bool> OnBuildModeChanged;
        /// <summary>Fired when the selected building changes. Argument is the new selected index.</summary>
        public event Action<int> OnSelectionChanged;

        private GameObject _ghost;
        private Quaternion _baseRotation = Quaternion.identity;
        private float _distance;
        private float _yaw;
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
            HandleRotationInput();
            UpdateGhostTransform();
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

        private void HandleRotationInput()
        {
            if (Keyboard.current[rotateLeftKey].wasPressedThisFrame) _yaw -= rotationStep;
            if (Keyboard.current[rotateRightKey].wasPressedThisFrame) _yaw += rotationStep;
        }

        private void UpdateGhostTransform()
        {
            if (_ghost == null) return;
            Transform cam = GetCamera();
            if (cam == null) return;

            Vector3 pos = cam.position + cam.forward * _distance;
            // Apply the player's yaw (around world up) on top of the prefab's authored rotation.
            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f) * _baseRotation);
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

            // Fresh, fully solid copy (original colliders/materials intact). Ghost stays for more.
            Instantiate(item.prefab, _ghost.transform.position, _ghost.transform.rotation);
        }

        private void RebuildGhost()
        {
            DestroyGhost();
            if (SelectedIndex < 0 || SelectedIndex >= buildables.Count) return;

            BuildableItem item = buildables[SelectedIndex];
            if (item == null || item.prefab == null) return;

            _ghost = CreateGhost(item.prefab);
            UpdateGhostTransform();
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
