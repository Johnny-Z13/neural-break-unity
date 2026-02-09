# CLAUDE.md - Neural Break

## Project Overview

**Neural Break** is a twin-stick survival arena shooter (Unity 6000.x). 99 levels, 8 enemy types, rogue-like card upgrades.

- **Engine**: Unity 6000.0.31f1+ (URP)
- **Input**: Unity Input System (NOT legacy `UnityEngine.Input`)
- **Architecture**: Event-driven, config-driven, zero-allocation pooling
- **Shared Code**: Z13.Core package for reusable systems

---

## Z13 Shared Package Philosophy

**Goal**: Build reusable packages across all Z13 Labs game projects.

### When Writing New Code, Always Ask:
1. **Is this game-specific or generic?** Generic code → Z13.Core
2. **Can this be split?** Generic base class in Z13.Core + game-specific subclass in project
3. **Does it reference game types?** If yes, keep in project. If no, consider Z13.Core

### Pattern: Generic Base + Game-Specific Implementation
```csharp
// Z13.Core - Generic base (no game knowledge)
public abstract class SaveSystemBase<T> where T : class, new() { }

// Game Project - Specific implementation
public class SaveSystem : Z13.Core.SaveSystemBase<SaveData> { }
```

### Z13.Core Package Location
`Assets/_Project/Packages/Z13.Core/`

### Current Z13.Core Contents
| Module | Purpose |
|--------|---------|
| `EventBus` | Type-safe pub/sub messaging |
| `ObjectPool<T>` | Zero-allocation object pooling |
| `LogHelper` | Editor-only logging (stripped in builds) |
| `SaveSystemBase<T>` | Generic save/load with JSON |
| `IBootable` | Interface for controlled singleton initialization |

### Adding to Z13.Core
1. Create in `Assets/_Project/Packages/Z13.Core/Runtime/`
2. Use namespace `Z13.Core`
3. **No game-specific types** - must be fully generic
4. Update `Z13.Core/README.md`

### Using Z13.Core in Game Code
Add `using Z13.Core;` at the top of files that need EventBus, ObjectPool, LogHelper, etc:
```csharp
using Z13.Core;

// Then use directly
EventBus.Publish(new MyEvent { value = 10 });
LogHelper.Log("Debug message");
```

---

## Development Philosophy

### No Backward Compatibility Code
When refactoring, **delete code that's no longer relevant**. Do not:
- Keep wrapper classes for backward compatibility
- Leave `// DEPRECATED` comments on old APIs
- Add `// TODO: Remove this` comments
- Keep unused parameters or methods "just in case"

```csharp
// ❌ WRONG - Don't keep compatibility wrappers
namespace NeuralBreak.Core
{
    // DEPRECATED: Use Z13.Core.EventBus directly
    public static class EventBus { ... }
}

// ✅ CORRECT - Just use the real thing
using Z13.Core;
EventBus.Publish(new MyEvent());
```

### Delete Over Comment
If code is no longer needed, **delete it entirely**. Don't comment it out or mark it deprecated:
```csharp
// ❌ WRONG
// Note: OnDestroy does NOT null Instance - true singletons live forever
private void OnDestroy() { }

// ❌ WRONG
// DEPRECATED: Access via serialized field instead of Instance
public static MyClass Instance { get; private set; }

// ✅ CORRECT - Just write clean code without explanatory comments for removed features
private void OnDestroy() { }
public static MyClass Instance { get; private set; }
```

### Prefer Direct Imports
Use `using` statements rather than fully qualified names:
```csharp
// ❌ WRONG
Z13.Core.EventBus.Publish(evt);
Z13.Core.LogHelper.Log("message");

// ✅ CORRECT
using Z13.Core;
EventBus.Publish(evt);
LogHelper.Log("message");
```

---

## Critical Rules (Non-Negotiable)

### Input System
```csharp
// ❌ WRONG - causes InvalidOperationException
Input.GetKeyDown(KeyCode.Space)

// ✅ CORRECT
Keyboard.current.spaceKey.wasPressedThisFrame
InputManager.Instance.OnConfirmPressed += HandleConfirm;
```

### Unity 6000.x APIs
```csharp
// ❌ WRONG (deprecated)
FindObjectOfType<GameManager>()

// ✅ CORRECT
FindFirstObjectByType<GameManager>()
```

### File Size Limit
All files must be **≤300 LOC**. Extract helpers to separate files if exceeded.

### Singleton Architecture (Boot Scene Pattern)

**TRUE SINGLETONS (Boot Scene, App-Lifetime):**
These live in the Boot scene, implement `IBootable`, and persist via `DontDestroyOnLoad`:
- `GameStateManager` - Global game flow, state, and mode
- `InputManager` - Global input handling
- `AudioManager` - Global audio playback
- `MusicManager` - Global music playback
- `SaveSystem` - Persistent save data
- `AccessibilityManager` - Global accessibility settings
- `HighScoreManager` - Persistent high scores
- `ConfigProvider` (static) - Config access
- `EventBus` (static) - Pub/sub messaging

```csharp
// ✅ TRUE SINGLETON - Always safe to access (Boot scene guarantees existence)
GameStateManager.Instance.StartGame(mode);
AudioManager.Instance.PlaySFX(clip);
InputManager.Instance.OnFirePressed += HandleFire;
```

**SCENE-SPECIFIC OBJECTS:**
These are scene objects. They have `Instance` for convenience but should be accessed via serialized fields when possible:
- `GameManager` - Scene-specific gameplay: score, combo, enemies
- `UIManager` - Scene-specific UI management
- `LevelManager` - Scene-specific level logic
- `PermanentUpgradeManager` - Game-session specific upgrades
- `UpgradePoolManager` - Game-session specific pool
- `EnemyProjectilePool` - Scene-specific projectile pool

```csharp
// Both work - Instance is available for convenience
GameManager.Instance.Stats

// Preferred when you have a reference - Use serialized field references
[SerializeField] private GameManager m_gameManager;
m_gameManager.Stats
```

**Boot Scene Setup:**
Run `Neural Break > Create Boot Scene` to generate the Boot scene with proper singleton initialization order.

### Zero-Allocation Gameplay
- Use object pools for spawned objects (projectiles, enemies, pickups, VFX)
- Cache references in Awake/Start
- No `new` in Update/FixedUpdate
- No FindObjectOfType in hot paths

### Naming Conventions
```csharp
// ❌ WRONG
private int _health;
private static int _instanceCount;
public int Health;

// ✅ CORRECT
private int m_health;              // m_ prefix for private members
private static int s_instanceCount; // s_ prefix for static members
public int health;                  // camelCase for public fields (data structs only)
```

**Rules:**
- `m_` prefix for all private instance members
- `s_` prefix for all static members (private or public)
- Public fields allowed **only** on data-only structs (DTOs, events, configs)
- Public fields use camelCase (look like properties)
- Classes should use properties for public data, not fields

```csharp
// Data-only struct - public fields OK
public struct EnemyKilledEvent
{
    public EnemyType enemyType;
    public Vector2 position;
    public int pointValue;
}

// Class - use properties or private fields
public class WeaponSystem : MonoBehaviour
{
    private float m_heat;
    private static int s_activeWeapons;

    public float Heat => m_heat;  // Property for external access
}
```

---

## Agent Operating Rules

### Before Coding
1. Restate task (1-2 lines), outline plan, note risks
2. List files to touch and why
3. Inspect real project—verify APIs/files exist
4. Implement as small, atomic diffs

### Zero-Hallucination Rule
Never invent files, APIs, or package versions. If unsure, ask.

### After Work
Show diffs, reasoning, risks, rollback path.

---

## Boot Scene Status

**Current State:** Boot scene architecture is **partially implemented** but incomplete:
- ✅ Code prepared: `BootManager.cs`, `IBootable` interface, true singleton pattern
- ❌ Boot scene does NOT exist in project (only `main-neural-break.unity`)
- ⚠️ Singletons use fallback initialization in Awake for development

**To Complete Boot Scene Setup:**
Run `Neural Break > Create Boot Scene` (if available) or manually create Boot scene with BootManager and all true singletons.

---

## File Size Guidelines

**300 LOC Target:** Aspirational guideline, not strictly enforced. Many core systems exceed this (WeaponSystem: 929 LOC, PlayerController: 849 LOC, etc.). Prioritize readability and logical cohesion over strict line limits.

---

## Architecture

### Event-Driven Communication
All systems communicate via **EventBus**. Never use direct references.

```csharp
// Define in EventBus.cs
public struct MyEvent { public int value; }

// Publish
EventBus.Publish(new MyEvent { value = 10 });

// Subscribe (OnEnable) / Unsubscribe (OnDisable)
EventBus.Subscribe<MyEvent>(OnMyEvent);
EventBus.Unsubscribe<MyEvent>(OnMyEvent);
```

**Location**: `Assets/_Project/Packages/Z13.Core/Runtime/EventBus.cs`
**Game Events**: `Assets/_Project/Scripts/Core/GameEvents.cs`

### Config-Driven Design
Balance values live in ScriptableObjects, not code.

```csharp
using NeuralBreak.Config;
int maxHealth = ConfigProvider.Player?.maxHealth ?? 100;
var enemy = ConfigProvider.Balance?.GetEnemyConfig(EnemyType.DataMite);
```

**Master Config**: `Assets/_Project/Resources/Config/GameBalanceConfig.asset`

### Object Pooling
```csharp
var projectile = m_projectilePool.Get();
projectile.Initialize(position, direction, damage);
// Later...
m_projectilePool.Return(projectile);
```

**Location**: `Assets/_Project/Packages/Z13.Core/Runtime/ObjectPool.cs`

---

## Key Systems (Quick Reference)

| System | Location | Purpose |
|--------|----------|---------|
| BootManager | `Core/BootManager.cs` | Boot scene singleton initialization |
| GameStateManager | `Core/GameStateManager.cs` | Global state machine (Boot scene singleton) |
| GameManager | `Core/GameManager.cs` | Scene-specific scoring, combo, level flow |
| LevelManager | `Core/LevelManager.cs` | Objectives, enemy unlock schedule |
| EnemySpawner | `Entities/Enemies/EnemySpawner.cs` | Wave/rate-based spawning |
| InputManager | `Input/InputManager.cs` | Twin-stick controls, events (Boot scene singleton) |
| WeaponSystem | `Combat/WeaponSystem.cs` | Firing, heat, modifiers |
| EventBus | `Z13.Core` + `Core/GameEvents.cs` | Pub/sub + game events |

### Game States
`StartScreen` → `Playing` → `Paused` / `RogueChoice` / `GameOver` / `Victory`

State is managed by `GameStateManager` (global singleton). Scene-specific gameplay (scoring, combo) is handled by `GameManager` (scene object).

### Weapon Modifier Flow
```
Base → WeaponUpgradeManager (temp) → PermanentUpgradeManager (perm) → Final
```

---

## Project Structure

```
Assets/
├── Scenes/
│   ├── Boot.unity              # Singleton initialization scene (index 0)
│   └── main-neural-break.unity # Main gameplay scene (index 1)
└── _Project/
    ├── Packages/
    │   └── Z13.Core/           # Shared reusable package
    │       └── Runtime/        # EventBus, ObjectPool, LogHelper, SaveSystemBase, IBootable
    ├── Scripts/
    │   ├── Audio/              # AudioManager, MusicManager (Boot scene singletons)
    │   ├── Combat/             # WeaponSystem, Projectile, Upgrades/
    │   ├── Config/             # ConfigProvider, GameBalanceConfig
    │   ├── Core/               # BootManager, GameStateManager, GameManager, LevelManager
    │   │   ├── BootManager.cs      # Initializes singletons, loads main scene
    │   │   ├── GameStateManager.cs # Global state (Boot scene singleton)
    │   │   ├── GameManager.cs      # Scene-specific gameplay
    │   │   ├── GameEvents.cs       # Game-specific events & enums
    │   │   └── SaveSystem.cs       # Extends Z13.Core.SaveSystemBase
    │   ├── Entities/           # Player, Enemies/, Pickups/
    │   ├── Graphics/           # CameraController, VFX, Starfield
    │   ├── Input/              # InputManager, GamepadRumble (Boot scene singleton)
    │   └── UI/                 # UIManager, HUD, Screens (scene-specific)
    ├── Prefabs/                # Enemies/, Pickups/, UI/
    └── Resources/              # Config/, Upgrades/
```

### Namespaces
- `Z13.Core` - Shared package (EventBus, ObjectPool, LogHelper, SaveSystemBase)
- `NeuralBreak.*` - Game-specific (Core, Entities, Combat, Input, Graphics, UI, Audio, Config, Utils)

---

## Common Tasks

### Creating New Enemy
1. Add to `EnemyType` enum in `EventBus.cs`
2. Create class inheriting `EnemyBase`, implement `UpdateAI()`
3. Add config in `GameBalanceConfig.asset`
4. Create prefab (CircleCollider2D trigger, SpriteRenderer, Rigidbody2D kinematic)
5. Register pool in `EnemyPoolManager`

### Creating New Upgrade
1. Create `UpgradeDefinition` asset: `Create > Neural Break > Upgrades > Definition`
2. Configure: ID, name, description, tier, modifiers
3. Place in `Resources/Upgrades/[Category]/`

### Adding New Event
Add struct to `GameEvents.cs` (NOT EventBus.cs):
```csharp
public struct MyEvent { public int value; public Vector2 pos; }
```

---

## Known Issues

| Issue | Fix |
|-------|-----|
| "PermanentUpgradeManager not found" | Add component to Managers GameObject |
| Blank upgrade cards | Run: Neural Break > Create Upgrades > Create Starter Pack |
| InvalidOperationException (Input) | Replace `Input.*` with `Keyboard.current.*` |

---

## Dependencies

| Package | Purpose |
|---------|---------|
| Z13.Core (local) | EventBus, ObjectPool, SaveSystem, LogHelper |
| URP 17.0.3+ | Rendering |
| Input System 1.11.2+ | Modern input |
| TextMeshPro 3.0.8+ | UI text |
| Feel (MMFeedbacks) | Game juice |

---

## Git Workflow

```
type(scope): summary

feat(combat): add beam weapon
fix(enemies): collision detection
config(balance): adjust boss health
```

---

## Quick Reference

### Files You'll Edit Most
1. `GameBalanceConfig.asset` - Balance values
2. `GameManager.cs` - State, scoring
3. `WeaponSystem.cs` - Firing patterns
4. `EnemyBase.cs` - Enemy AI
5. `GameEvents.cs` - Event definitions (game-specific events)

### Common Events
```csharp
EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
```

### Debug Menu
- `Neural Break > Create Boot Scene` - Generate Boot scene with singletons
- `Neural Break > Validate Boot Scene Setup` - Check Boot scene configuration
- `Neural Break > Create Upgrades > Create Starter Pack`
- `Neural Break > Clear Save Data`
- Right-click GameManager > Debug commands

---

## Extended Reference

See `CLAUDE_REFERENCE.md` for:
- Detailed code examples and patterns
- Complete scene setup checklist
- Performance guidelines
- MMFeedbacks integration
- Full object pool sizing guide

---

**Developer**: Johnny @ Z13 Labs | johnny@z13labs.com
**Original TS/Three.js**: `D:\Projects\Neural-Break-Unity`
