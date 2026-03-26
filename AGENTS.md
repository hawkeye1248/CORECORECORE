# AGENTS.md - CORECORECORE Unity FPS Project

## Project Overview

- **Unity**: 2022.1+ (URP 17.0.4)
- **Input System**: Unity Input System 1.13.1
- **Language**: C#

## Project Structure

```
Assets/
├── AAA/_Scripts/              # Main game scripts
│   ├── Managers/              # GameManager, state management
│   ├── Input/                  # CoreInputActions, GameInput
│   ├── AnimationEvents/        # Animation event handling
│   ├── UI/                     # UI components
│   └── *.cs                    # Weapons, enemies, player, health
├── MovementRework/             # New movement (namespace MovementRework)
├── VolFx/                      # Third-party VFX toolkit
└── Lightweight Advanced Controller/  # Third-party movement
```

## Build, Test, and Development

### Unity Editor Commands
- **Open project**: Unity Hub > Open > select folder
- **Build Player**: File > Build Settings > Build
- **Run Tests**: Window > General > Test Runner > Run All

### Running a Single Test
1. Open Test Runner (Window > General > Test Runner)
2. Find the specific test in PlayMode or EditMode
3. Right-click > Run Selected

### Unity Package Manager
- Packages managed in `Packages/manifest.json`
- Add via: Window > Package Manager > Add from Git URL

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes/Methods/Events | PascalCase | `GameManager`, `OnJumpPerformed` |
| Private fields | camelCase | `weaponHolder`, `currentState` |
| Public fields | PascalCase | `Instance`, `GameState` |
| Enum values | PascalCase | `GameState.GAMEPLAY` |
| Constants | PascalCase | `MaxSpeed`, `GroundLayers` |
| Namespaces | PascalCase | `MovementRework`, `VolFx` |
| Interfaces | PascalCase with 'I' | `IGameplayActions` |

### Import Order
1. Unity namespaces (`UnityEngine`, `UnityEngine.InputSystem`)
2. System namespaces (`System`, `System.Collections`)
3. Third-party (`VolFx`, `peterkcodes.AdvancedMovement`, `DG.Tweening`)
4. Project-specific namespaces

### File Organization
```csharp
// Using statements
using UnityEngine;
// ...

// Namespace (if applicable)
namespace MovementRework;

// Class with fields, then methods
public class Example : MonoBehaviour
{
    // SerializeField private fields with [Header]
    [Header("Settings")]
    [SerializeField] private float damage;
    
    // Public fields
    public static Example Instance { get; private set; }
    
    // Unity lifecycle: Awake, Start, OnEnable, OnDisable, Update, FixedUpdate
    private void Awake() { }
    private void Start() { }
}
```

### Braces and Spacing
```csharp
// K&R-style braces (1TBS)
if (condition)
{
    DoSomething();
} else
{
    DoSomethingElse();
}

// Space after keywords, no space before parentheses
if (condition == true)
while (index < count)
```

### Common Patterns

**Singleton pattern**:
```csharp
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
```

**Event-based communication**:
```csharp
public event EventHandler OnStateChanged;
public event EventHandler OnPlayerDeath;

// Invoke with null-conditional
OnPlayerDeath?.Invoke(this, EventArgs.Empty);
```

**SerializeField for Inspector**:
```csharp
[Header("Weapon Settings")]
[SerializeField] private float damage;
[SerializeField] private LayerMask weaponLayer;
```

**Early returns**:
```csharp
private void OnTriggerEnter(Collider other)
{
    if (other == null)
        return;
    // ...
}
```

### Type Usage
- Prefer `var` when type is obvious
- Generic collections (`List<T>`, `HashSet<T>`) over non-generic
- Use `readonly` for fields that shouldn't change
- LINQ acceptable for queries

### Coroutines
```csharp
public IEnumerator FireInterval()
{
    isFireInterval = true;
    yield return new WaitForSeconds(fireInterval);
    isFireInterval = false;
}

StartCoroutine(FireInterval());
```

### Physics (Newer Unity)
Use `rigidbody.linearVelocity` instead of `rigidbody.velocity` in Unity 2023+

### Third-Party Libraries
- **DG.Tweening**: Animation sequences (`DOTween.Sequence()`)
- **peterkcodes.AdvancedMovement**: Player movement controller
- **VolFx**: Screen effects (Editor scripts in `.../Editor/` folders)

## Assembly Definitions

This project uses `.asmdef` files:
- `VolFx.Runtime.asmdef` / `VolFx.Editor.asmdef`
- `Tools.Runtime.asmdef` / `Tools.Editor.asmdef`
- `ScreenFx.Runtime.asmdef` / `ScreenFx.Editor.asmdef`

**Important**: When adding scripts that reference other assemblies, create or modify the appropriate `.asmdef` file and add references to dependent assemblies.

## Performance Tips

- Cache component references in `Awake()` or field initialization
- Avoid `GetComponent` in `Update` loops
- Use object pooling for frequently spawned objects
- Consider URP render pipeline optimizations

## Testing

- Tests go in `Assets/Tests/` or `Assets/PerformanceTests/`
- Use Unity Test Framework (`com.unity.test-framework`)
- Tests can be PlayMode or EditMode
- Run via Test Runner window (no CLI for individual tests)

## Quick Reference

| Item | Location |
|------|----------|
| Project Settings | `ProjectSettings/` |
| Scenes | `Assets/*.unity` |
| Prefabs | `Assets/**/*.prefab` |
| Packages config | `Packages/manifest.json` |
| VS Code config | `.vscode/settings.json` |
