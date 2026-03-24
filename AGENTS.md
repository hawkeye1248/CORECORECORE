# AGENTS.md - CORECORECORE Unity Project

## Project Overview

This is a Unity 3D FPS game project using:
- **Unity Version**: 2022.1+ (URP 17.0.4)
- **Input System**: Unity Input System 1.13.1
- **Rendering**: Universal Render Pipeline (URP)
- **Language**: C#

## Project Structure

```
Assets/
├── AAA/                    # Main game scripts
│   └── _Scripts/
│       ├── Managers/       # Game management (GameManager, etc.)
│       ├── Input/          # Input handling (CoreInputActions, GameInput)
│       ├── AnimationEvents/
│       └── *.cs            # Weapons, enemies, player, UI
├── MovementRework/         # New movement system (namespace MovementRework)
├── VolFx/                  # VolFx VFX toolkit (third-party)
│   ├── VolFx/             # VFX core library
│   ├── ScreenFx/          # Screen effects module
│   └── Tools/             # Utility tools
├── Lightweight Advanced Controller/  # Third-party movement
└── Retro FPS Kit/          # Third-party weapons
```

## Build, Test, and Development Commands

### Unity Editor
- **Open project**: Open in Unity Hub, select this folder
- **Build Player**: File > Build Settings > Build
- **Run Tests**: Window > General > Test Runner > Run All

### VS Code / CLI (Limited)
Unity projects do not have traditional CLI build commands. Scripts compile via:
- Opening the project in Unity
- Using `dotnet build` on `.csproj` files (won't produce playable build)

### Running Tests
```
In Unity Editor: Window > General > Test Runner > Run All
```

### Unity Package Manager
- Packages are managed in `Packages/manifest.json`
- Add packages via: Window > Package Manager > Add from Git URL

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `PlayerWeaponController` |
| Methods | PascalCase | `OnJumpPerformed`, `CheckGround()` |
| Private fields | camelCase | `weaponHolder`, `currentState` |
| Public fields | PascalCase | `Instance`, `GameState` |
| Enum values | PascalCase | `GameState.GAMEPLAY`, `EnemyState.Idle` |
| Constants | PascalCase | `MaxSpeed`, `GroundLayers` |
| Namespaces | PascalCase | `MovementRework`, `VolFx` |
| Events | PascalCase | `OnStateChanged`, `OnJumpPerformed` |
| Interfaces | PascalCase with 'I' prefix | `IGameplayActions` |

### Unity-Specific Patterns

```csharp
// Singleton pattern (common in this codebase)
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}

// Event-based communication
public event EventHandler OnJumpPerformed;
public event EventHandler OnFirePerformed;

// SerializeField for private Unity-serialized fields
[SerializeField] private Transform weaponHolder;
[SerializeField] private LayerMask weaponLayer;

// Header attributes for inspector organization
[Header("Weapon Settings")]
[SerializeField] private float damage;

// Property for read-only access
public bool IsGamePlaying => currentState == GameState.GAMEPLAY;
```

### File Organization

1. **Using statements** at top (UnityEngine first, then System.*, then others)
2. **Namespace** declaration (if applicable)
3. **Class declaration** with XML documentation for public APIs
4. **Fields** grouped by visibility (public, [SerializeField], private)
5. **Fields** grouped by purpose with `[Header]` attributes
6. **Unity lifecycle methods**: Awake, Start, OnEnable, OnDisable, Update, FixedUpdate, LateUpdate
7. **Public methods** (API)
8. **Private methods** (implementation)
9. **Event handlers** (often prefixed with `on_` in this codebase)

### Braces and Spacing

```csharp
// This codebase uses K&R-style braces (1TBS variant)
if (condition)
{
    DoSomething();
} else
{
    DoSomethingElse();
}

// Space after keywords, not after parentheses
if (condition == true)
while (index < count)

// No space before method parentheses
void Update()
{
    MyMethod(arg1, arg2);
}
```

### Type Usage

- **Prefer `var`** for local variables when type is obvious
- **Use `this`** sparingly, only when needed for clarity
- **LINQ** is acceptable for queries
- **Generic collections** over non-generic (`List<T>` not ArrayList)
- **readonly** for fields that shouldn't change after initialization

### Error Handling

```csharp
// Prefer null-coalescing and null-conditional operators
OnStateChanged?.Invoke(this, EventArgs.Empty);

// Check for null before accessing
if (weapon != null)
{
    weapon.Shoot();
}

// Use early returns to reduce nesting
private void OnTriggerEnter(Collider other)
{
    if (other == null)
        return;
    // ...
}
```

### Async/Threading

- Unity is not thread-safe; most operations must run on main thread
- For VolFx editor scripts, async/await patterns are used with caution
- Use `async Task` with `await Task.Yield()` to yield to main thread

### Comments

```csharp
// TODO comments for future work (found in codebase)
private void Awake()
{
    //TODO değiştirilcek - TODO: change this
}

// Use descriptive names over comments when possible
// VolFx uses copyright headers
//  VolFx © NullTale - https://x.com/NullTale
```

## Assembly Definitions

This project uses Assembly Definitions (.asmdef) to organize code:

- `VolFx.Runtime.asmdef` - Runtime VFX code
- `VolFx.Editor.asmdef` - Editor-only code
- `Tools.Runtime.asmdef`, `Tools.Editor.asmdef` - Tools module
- `ScreenFx.Runtime.asmdef`, `ScreenFx.Editor.asmdef` - Screen effects

**Important**: When adding new scripts that need to reference other assemblies:
1. Create or modify the appropriate `.asmdef` file
2. Add references to dependent assemblies
3. Avoid circular dependencies between assemblies

## Working with VolFx

VolFx is a third-party VFX library. When modifying:
- Editor scripts go in `.../Editor/` folders (auto-excluded from build)
- Use `[InitializeOnLoad]` for editor-time initialization
- Use `[DidReloadScripts]` for post-compilation callbacks
- Prefer ScriptableObject-based architecture for effects

## Quick Reference

- **Project Settings**: `ProjectSettings/` (do not modify unless necessary)
- **Scene files**: `Assets/*.unity`
- **Prefabs**: `Assets/**/*.prefab`
- **Scripts**: `Assets/**/*/*.cs`
- **Shader includes**: `Assets/Shaders/`
- **Packages**: Managed in `Packages/manifest.json`

## Import Organization

```csharp
// Unity namespaces first
using UnityEngine;
using UnityEngine.InputSystem;

// Then System namespaces
using System;
using System.Collections.Generic;

// Then third-party
using VolFx;

// Then project-specific
using MovementRework;
```

## Performance Considerations

- Cache component references in `Awake()` or at field initialization
- Use object pooling for frequently spawned objects
- Avoid `GetComponent` calls in `Update` loops
- Use `DontDestroyOnLoad` sparingly
- Consider URP render pipeline optimizations

## Testing Guidelines

- Unity tests go in `Assets/Tests/` or `Assets/PerformanceTests/`
- Use Unity Test Framework (`com.unity.test-framework`)
- Tests can be PlayMode or EditMode
- For integration testing, test game flow; for unit testing, test isolated logic
