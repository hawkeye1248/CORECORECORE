# AGENTS.md - CORECORECORE Unity FPS Project

## Project Overview

- **Unity**: 6000.0.43f1 (URP 17.0.4)
- **Input System**: Unity Input System 1.13.1
- **Language**: C#
- **Genre**: First-person shooter with custom movement system

## Project Structure

```
Assets/
├── AAA/
│   ├── _Scripts/                  # Core game scripts
│   ├── MovementRework/            # Custom movement (namespace: MovementRework)
│   ├── Animations/                # Animation controllers & clips
│   ├── Materials/                 # Materials
│   ├── Models/                    # 3D models
│   ├── Prefabs/                   # Prefabs
│   ├── Scenes/                    # Game scenes
│   ├── Scriptable/                # ScriptableObjects
│   └── Settings/                  # URP/render settings
├── VolFx/                         # Third-party VFX toolkit
├── Lightweight Advanced Controller/  # Third-party movement (peterkcodes)
├── Retro FPS Kit/                 # Retro FPS kit asset
├── Shaders/                       # Toony Colors Pro, Quibli, ProPixelizer
└── TextMesh Pro/                  # TextMesh Pro
```

## Build, Test, and Development

### Unity Editor Commands
- **Open project**: Unity Hub > Open > select this folder
- **Build Player**: File > Build Settings > Build
- **Run Tests**: Window > General > Test Runner > Run All

### Running a Single Test
1. Open Test Runner: **Window > General > Test Runner**
2. Select **PlayMode** or **EditMode** tab
3. Find the specific test in the tree
4. Right-click the test > **Run Selected**
5. Results appear in the Test Runner window and Console

**Note**: No CLI test runner is configured. All tests must be run through the Unity Editor Test Runner window.

### Unity Package Manager
- Packages managed in `Packages/manifest.json`
- Add via: Window > Package Manager > + > Add from Git URL

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes/Methods/Events | PascalCase | `GameManager`, `OnJumpPerformed` |
| Private fields | camelCase | `weaponHolder`, `currentState` |
| Public fields/properties | PascalCase | `Instance`, `GameState` |
| Private auto-properties | camelCase setter | `public static Player Instance { get; private set; }` |
| Enum values | PascalCase | `GameState.GAMEPLAY` |
| Constants | PascalCase | `MaxSpeed`, `GroundLayers` |
| Readonly fields | camelCase with `readonly` | `private readonly int castCount = 10;` |
| Namespaces | PascalCase | `MovementRework`, `VolFx` |
| Interfaces | PascalCase with 'I' | `IGameplayActions` |

### Import Order
1. System namespaces (`System`, `System.Collections`)
2. Unity namespaces (`UnityEngine`, `UnityEngine.InputSystem`)
3. Third-party (`DG.Tweening`, `peterkcodes.AdvancedMovement`, `VolFx`)
4. Project-specific namespaces (`MovementRework`)

### File Organization
```csharp
// Using statements (ordered as above)
using System.Collections;
using UnityEngine;
using MovementRework;

// Class structure example
public class Example : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage;

    public static Example Instance { get; private set; }

    private void Awake() { }
    private void Start() { }
}
```

### Braces and Spacing
- K&R-style / 1TBS braces
- `else` on same line as closing brace: `} else {`
- 4-space indentation
- Space after keywords: `if (condition)`, `foreach (var col in ...)`

### Common Patterns

**Singleton**: `public static GameManager Instance { get; private set; }` in `Awake()`.
**Event-based communication**: `public event EventHandler OnPlayerDeath; OnPlayerDeath?.Invoke(this, EventArgs.Empty);`
**SerializeField for Inspector**: Use `[SerializeField] private` (never `public`).
**Early returns**: `if (condition) return;`
**Coroutines**: Use `StartCoroutine(MyCoroutine());` for timed operations.

### Type Usage
- Prefer `var` when type is obvious.
- Generic collections (`List<T>`, `HashSet<T>`) over non-generic.
- Use `readonly` for fields that should not change after initialization.
- LINQ acceptable for queries.

### Error Handling
- Use null checks (`if (obj == null) return;` or `obj?.Method()`) to prevent `NullReferenceException`.
- Employ `try-catch` blocks for operations that might throw specific exceptions (e.g., file I/O, network requests).

### Physics API
Use `rigidbody.linearVelocity` (not `rigidbody.velocity`) — Unity 6+ API.

### Third-Party Libraries
- **DG.Tweening**: `DOTween.Sequence()`, `transform.DOMove()`, `transform.DOLocalMove()`
- **peterkcodes.AdvancedMovement**: Player movement controller
- **VolFx**: Screen effects

## Performance Tips

- Cache component references in `Awake()` or via field initialization.
- Avoid `GetComponent` in `Update` loops.
- Use object pooling for frequently spawned objects.

## Testing

- Framework: Unity Test Framework (`com.unity.test-framework` v1.4.6)
- Test location: `Assets/Tests/` (PlayMode and EditMode)
- Existing integration test scripts: `Assets/AAA/_Scripts/Enemy/NewEnemyTest.cs`, `Assets/AAA/_Scripts/MeleeTest.cs`

## Quick Reference

| Item | Location |
|------|----------|
| Project Settings | `ProjectSettings/` |
| Scenes | `Assets/AAA/Scenes/` |
| Prefabs | `Assets/AAA/Prefabs/` |
| Packages config | `Packages/manifest.json` |
| VS Code config | `.vscode/settings.json` |
| Solution file | `CORECORECORE.sln` |

## Agent-Specific Rules
- No Cursor rules (`.cursor/rules/` or `.cursorrules`) found.
- No Copilot rules (`.github/copilot-instructions.md`) found.
