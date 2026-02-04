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

### Adding to Z13.Core
1. Create in `Assets/_Project/Packages/Z13.Core/Runtime/`
2. Use namespace `Z13.Core`
3. **No game-specific types** - must be fully generic
4. Update `Z13.Core/README.md`

### Backward Compatibility Pattern
When extracting to Z13.Core, leave a thin wrapper in the game project:
```csharp
// Game project wrapper for backward compatibility
namespace NeuralBreak.Core
{
    public static class EventBus
    {
        public static void Publish<T>(T evt) where T : struct
            => Z13.Core.EventBus.Publish(evt);
    }
}
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

### Singleton Allowlist (Only These 8)
`GameManager`, `InputManager`, `UIManager`, `AudioManager`, `PermanentUpgradeManager`, `UpgradePoolManager`, `ConfigProvider` (static), `EventBus` (static)

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

**Location**: `Assets/_Project/Scripts/Core/EventBus.cs`

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

**Location**: `Assets/_Project/Scripts/Core/ObjectPool.cs`

---

## Key Systems (Quick Reference)

| System | Location | Purpose |
|--------|----------|---------|
| GameManager | `Core/GameManager.cs` | State machine, scoring, level flow |
| LevelManager | `Core/LevelManager.cs` | Objectives, enemy unlock schedule |
| EnemySpawner | `Entities/Enemies/EnemySpawner.cs` | Wave/rate-based spawning |
| InputManager | `Input/InputManager.cs` | Twin-stick controls, events |
| WeaponSystem | `Combat/WeaponSystem.cs` | Firing, heat, modifiers |
| EventBus | `Z13.Core` + `Core/GameEvents.cs` | Pub/sub + game events |

### Game States
`StartScreen` → `Playing` → `Paused` / `RogueChoice` / `GameOver` / `Victory`

### Weapon Modifier Flow
```
Base → WeaponUpgradeManager (temp) → PermanentUpgradeManager (perm) → Final
```

---

## Project Structure

```
Assets/_Project/
├── Packages/
│   └── Z13.Core/           # Shared reusable package
│       └── Runtime/        # EventBus, ObjectPool, LogHelper, SaveSystemBase
├── Scripts/
│   ├── Audio/              # AudioManager, MusicManager
│   ├── Combat/             # WeaponSystem, Projectile, Upgrades/
│   ├── Config/             # ConfigProvider, GameBalanceConfig
│   ├── Core/               # GameManager, LevelManager, GameEvents
│   │   ├── EventBus.cs     # Wrapper → Z13.Core.EventBus
│   │   ├── ObjectPool.cs   # Wrapper → Z13.Core.ObjectPool
│   │   ├── GameEvents.cs   # Game-specific events & enums
│   │   └── SaveSystem.cs   # Extends Z13.Core.SaveSystemBase
│   ├── Entities/           # Player, Enemies/, Pickups/
│   ├── Graphics/           # CameraController, VFX, Starfield
│   ├── Input/              # InputManager, GamepadRumble
│   ├── UI/                 # UIManager, HUD, Screens
│   └── Utils/              # Extensions (LogHelper → Z13.Core)
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
