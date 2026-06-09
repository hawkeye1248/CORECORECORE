# CORECORECORE - Player System Documentation

## Project Info

- **Unity Version**: 6000.0.43f1 (URP 17.0.4)
- **Input System**: Unity Input System 1.13.1
- **Language**: C# (namespace: `MovementRework`)

---

## Player Prefab Structure

```
Player (Root)
├── CamParent
│   └── CamController
│       └── Camera
├── PlayerModel
│   └── ArmParent
│       └── (First-person arms model + Animator)
├── Core
└── Orientation
```

### Root: `Player`

| Component | Purpose |
|-----------|---------|
| **`MovementRework.Player`** | Central singleton controller. Handles all movement: ground acceleration, jumping (coyote time), wallrunning, mantling, crouching/sliding. Applies forces to the `Core` Rigidbody. Exposes status bools (`IsGrounded`, `IsWallrunning`, `IsCrouching`, etc.) and utility methods like `GetMovementSpeed()`, `GetCamera()`, `LungeForward()`. |
| **`PlayerWeaponManager`** | Manages weapons: equipping, firing (LMB), throwing (RMB), melee punching. The melee system does overlapping box casts in a cone to hit enemies. Includes hitstop (time-scale freeze) and knockback. Picks up weapons via raycast on interact. |

**Serialized references on Player:**
- `orientation` → Orientation transform (for movement direction)
- `core` → Core's Rigidbody (physics body)
- `cameraController` → CamController script
- `movementData` → ScriptableObject with movement tuning values
- `playerScript` (on WeaponManager) → self-reference

### `CamParent`

| Component | Purpose |
|-----------|---------|
| **`CamPositioner`** | Follows the player's position. Smoothly transitions between `standingVerticalOffset` (0.5) and `crouchingVerticalOffset` (1.2) when crouching. Triggers a "jolt" camera shake on hard landings using an `AnimationCurve`. |

**Config values:**
- `standingVerticalOffset`: 0.5
- `crouchingVerticalOffset`: 1.2
- `camMovementTime`: 0.5
- `joltAmplitute`: 1
- `joltLength`: 0.2

### `CamController`

| Component | Purpose |
|-----------|---------|
| **`CameraController`** | Handles mouse-look rotation with smoothing, vertical clamping (60 up/down), camera Z-axis tilt during wallruns/strafing, and dynamic FOV (80-90 based on speed). Exposes `facingDirection` and `upwardsDirection` used by Player for movement calculations. |

**Config values:**
- `upperVerticalLimit` / `lowerVerticalLimit`: 60
- `camSmoothingFactor`: 50
- `camSpeed`: 70
- `cameraTiltMultiplier`: 3
- `wallrunTiltMultiplier`: 20
- `minFov`: 80 / `maxFov`: 90

### `Camera` (child of CamController)

| Component | Purpose |
|-----------|---------|
| **Camera** | Standard Unity camera. FOV 80, near clip 0.01, far clip 1000. |
| **AudioListener** | Captures in-game audio for the player. |
| **UniversalAdditionalCameraData** | URP rendering config: shadows on, no post-processing, no antialiasing, volume layer on Default, depth clear enabled. |

### `PlayerModel`

| Component | Purpose |
|-----------|---------|
| **`PlayerModel`** | Controls first-person arms. Follows player position, rotates hands toward camera direction (`turningSpeed: 20`). Drives the Animator parameters based on state (mantling, wallrunning, jumping, sliding, running, airborne). Provides `PunchRightTrigger()` / `PunchLeftTrigger()` for melee animations. |
| **Animator** | Drives arm animations (idle, run, punch, mantle, etc.) using the player model avatar. |
| **`AnimationEventReceiver`** | Generic event dispatcher. When an animation clip fires an event by name (e.g. `"Punch"`), it invokes the matching `UnityEvent` - configured to call `PlayerWeaponManager.Punch()` and `Player.LungeForward()`. |

### `Core`

| Component | Purpose |
|-----------|---------|
| **Rigidbody** | The main physics body. Mass 1, interpolation on, gravity on, no constraints. All movement forces are applied here. |
| **SphereCollider** | Radius 0.5, with a physics material for sliding. Excludes layers 10 and 11 from collision. |

### `Orientation`

Empty transform at the root, used as a forward-direction reference for movement calculations (rotates with the camera yaw).

### `WeaponTransform`

Empty child transform of the first-person arm's weapon mount point - parent for instantiated weapon models.

---

## Input System

Input is handled by two **separate singletons** (not on the Player prefab):

### `GameInput` prefab

A standalone `DontDestroyOnLoad` singleton. Provides:
- `OnJumpPerformed`
- `OnFirePerformed` / `OnFireCanceled`
- `OnReloadPerformed`
- `OnSlidePerformed` / `OnSlideCanceled`
- `GetMovementVector()` → normalized `Vector2`

### `MovementInput` (in `MovementRework` namespace)

Another standalone `DontDestroyOnLoad` singleton. Provides:
- `OnJumpPerformed`
- `OnLMBPerformed` / `OnLMBCanceled`
- `OnRMBPerformed` / `OnRMBCanceled`
- `OnCrouchPerformed` / `OnCrouchCanceled`
- `OnInteractPerformed`
- `GetMovementVector()` → normalized `Vector2`
- `GetLookVector()` → raw `Vector2`

**Note:** These two singletons overlap on Jump, LMB, RMB, and Slide events. See the simplification plan below for how to consolidate them.

---

## MovementRework Scripts

All located in `Assets/AAA/_Scripts/MovementRework/`:

| Script | Purpose |
|--------|---------|
| `Player.cs` | Main player controller - movement, jump, wallrun, mantle, slide. Singleton. |
| `PlayerWeaponManager.cs` | Weapon equip/throw, melee punch, hit detection. |
| `CameraController.cs` | FPS camera rotation, FOV, tilt, headbob. |
| `CamPositioner.cs` | Camera offset for standing/crouching, landing jolt effect. |
| `PlayerModel.cs` | Animator state machine for first-person arms. |
| `MovementInput.cs` | Input singleton (Unity Input System, event bus). |
| `PlayerMovementData.cs` | ScriptableObject with all movement tuning parameters. |
| `GameEvents.cs` | Static Action events (OnEnemyDeathWithWeapon/WithoutWeapon). |

### External Dependencies on Player

| Consumer | Depends on |
|----------|-----------|
| `NewEnemyTest.cs` | `Player.Instance.core.transform` (navigation target) |
| `WeaponScript.cs` | `Player.Instance.cameraController.transform` (camera direction) |
| `PlayerWeaponManager.cs` | `Player` (direct reference, chains into camera, model, core) |

---

## How to Use

1. **Drag into scene** - The Player prefab is self-contained; just instantiate it.
2. **Input** - Requires `GameInput` or `MovementInput` prefab in the scene (handles WASD, mouse look, jump, crouch, fire, throw, interact).
3. **Movement tuning** - Adjust the `movementData` ScriptableObject assigned to `Player.movementData` for speed, acceleration, jump force, etc.
4. **Camera feel** - Tweak `CameraController` fields in Inspector (smoothing, FOV range, tilt multipliers) and `CamPositioner` for standing/crouching heights and jolt intensity.
5. **Weapons** - Call `PlayerWeaponManager` methods or use the built-in fire/throw/interact input bindings. Weapons are picked up by looking at them and pressing interact.
6. **Melee** - Triggered automatically via animation events through `AnimationEventReceiver` -> `PlayerWeaponManager.Punch()`.

---

## Simplification Plan

### Problems with Current Structure

1. **Two redundant input singletons** - `GameInput` and `MovementInput` both wrap `CoreInputActions`, both are `DontDestroyOnLoad`, and they overlap on Jump, LMB, RMB, and Slide events. This is confusing and wasteful.
2. **Overly nested camera hierarchy** - Three GameObjects (`CamParent` > `CamController` > `Camera`) where one would suffice. `CamPositioner` and `CameraController` are both camera logic and can live on the same object.
3. **PlayerModel disconnected from Camera** - The arms follow the camera via a serialized reference chain. If the arms were parented under the camera, they'd rotate automatically.

### Proposed Simplified Structure

```
Player (Root)
├── Camera              <- Camera + AudioListener + URP Data + CameraController
│   ├── Orientation     <- empty transform (for direction math)
│   └── Arms            <- PlayerModel + Animator + AnimationEventReceiver
└── Core                <- Rigidbody + SphereCollider
    └── WeaponTransform <- mount for weapon models
```

**6 GameObjects -> 4 GameObjects.**

### Changes Required

#### Step 1: Merge Input Singletons
- Delete `GameInput.cs` and its prefab.
- Keep `MovementInput` (it has all needed events: Jump, LMB, RMB, Crouch, Interact).
- Update `PlayerWeaponManager.cs` to subscribe to `MovementInput.Instance` events instead of `GameInput.Instance`.

#### Step 2: Flatten Camera Hierarchy
- Delete `CamParent` GameObject. Merge `CamPositioner`'s logic into `CameraController`:
  - Add `standingVerticalOffset`, `crouchingVerticalOffset`, `camMovementTime`, `joltAmplitute`, `joltLength`, `joltCurve` fields.
  - Move `SimplePosition()`, `MoveCamToCrouching()`/`MoveCamToStanding()`, and `Jolt()` into `CameraController`.
  - In `Player.cs`, replace all `camParent.*` calls with `cameraController.*`.
- Delete `CamController` GameObject. Move `CameraController.cs` onto the `Camera` GameObject directly.
- Reassign `cameraController` field on Player.

#### Step 3: Reparent Orientation and Arms Under Camera
- Move `Orientation` to be a child of `Camera` (inherits camera yaw automatically).
- Move `PlayerModel` (arms) to be a child of `Camera`:
  - Delete the `ArmParent` child.
  - Remove manual `SimplePosition()` rotation logic from `PlayerModel`.
- In `Player.Update()`:
  - Remove `playerModel.SimplePosition(core.position)`.
  - Remove `camParent.SimplePosition(core.position)`.
  - Set Camera world position to `core.position + offset` instead.

#### Step 4: Move WeaponTransform Under Core
- Move `WeaponTransform` to child of `Core` at local offset `(0, 0.0052, 0)`.
- Update `PlayerWeaponManager.weaponHolder` reference.

#### Step 5: Update External References
- `NewEnemyTest.cs` uses `Player.Instance.core.transform` - no change needed.
- `WeaponScript.cs` uses `Player.Instance.cameraController.transform` - no change needed.

### Result

- Fewer `GetComponentInChildren` calls
- No more `SimplePosition()` per-frame rotation hacks
- Single input singleton
- Arms inherit camera rotation naturally via hierarchy
- Cleaner Inspector (fewer GameObjects to scroll through)
