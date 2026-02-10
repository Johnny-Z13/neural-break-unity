# CLAUDE.md - Neural Break

> **Unity C# Technical Reference** for Claude Code
> Senior Unity engineer-level instructions for AI-assisted development

---

## 🎮 Project Overview

**Neural Break** is a twin-stick survival arena shooter built in Unity 6000.x with URP.

- **Unity Version**: 6000.0.31f1+ (LTS recommended)
- **Rendering**: Universal Render Pipeline (URP) 17.0.3+
- **Platform Target**: PC (Windows/Mac/Linux), 1920x1080 base resolution
- **Architecture**: Event-driven, config-driven, zero-allocation hot paths
- **Genre**: Twin-stick shooter, rogue-like progression, 99 levels, 8 enemy types

---

## 🛠️ MCP Servers Available

Claude Code typically runs with these MCP (Model Context Protocol) servers:

- **Unity MCP Server** (if configured): Provides Unity API awareness, GameObject inspection, Scene hierarchy access
- **File System MCP** (default): Read/Write/Search local files
- **Git MCP** (default): Git operations and version control

**Note**: If Unity-specific MCP is not configured, Claude Code will still function but may need more explicit Unity API references.

---

## 📚 Unity C# Coding Conventions

### Naming Conventions (Strict)

```csharp
// ✅ CORRECT
private int m_health;                    // m_ prefix for private instance fields
private static int s_instanceCount;      // s_ prefix for static fields
public int MaxHealth { get; private set; }  // PascalCase for properties
private void UpdateHealth() { }          // PascalCase for methods
public const int MAX_LEVEL = 99;         // SCREAMING_SNAKE_CASE for constants

// ❌ WRONG
private int _health;                     // Underscore prefix (not used)
private int health;                      // No prefix (confusing)
public int maxHealth;                    // Public field (use property)
private void update_health() { }         // snake_case (not C#)
```

**Field/Property Rules**:
- Private instance fields: `m_fieldName`
- Private static fields: `s_fieldName`
- Public properties: `PropertyName` (PascalCase)
- Public fields: **Only allowed in data-only structs** (events, DTOs)
- Constants: `CONSTANT_NAME` (SCREAMING_SNAKE_CASE)

**Data-Only Structs** (public fields OK):
```csharp
public struct EnemyKilledEvent
{
    public EnemyType enemyType;  // camelCase for struct fields
    public Vector2 position;
    public int pointValue;
}
```

**Classes** (use properties):
```csharp
public class WeaponSystem : MonoBehaviour
{
    private float m_heat;
    private static int s_activeWeapons;

    public float Heat => m_heat;  // Property for external access
    public static int ActiveWeapons => s_activeWeapons;
}
```

---

## ⚡ Performance Rules (Non-Negotiable)

### Hot Path Performance (Update, FixedUpdate, LateUpdate)

```csharp
// ❌ WRONG - NO LINQ in hot paths
void Update()
{
    var enemies = FindObjectsOfType<Enemy>().Where(e => e.IsAlive).ToList();  // LINQ allocation
    float avgHealth = enemies.Average(e => e.Health);  // More LINQ
}

// ✅ CORRECT - Manual iteration, zero allocations
void Update()
{
    int aliveCount = 0;
    float totalHealth = 0f;

    for (int i = 0; i < m_cachedEnemies.Count; i++)
    {
        if (m_cachedEnemies[i].IsAlive)
        {
            aliveCount++;
            totalHealth += m_cachedEnemies[i].Health;
        }
    }

    float avgHealth = aliveCount > 0 ? totalHealth / aliveCount : 0f;
}
```

**Hot Path Rules**:
1. ❌ **NO LINQ** (`Where`, `Select`, `ToList`, `Any`, `FirstOrDefault`, etc.)
2. ❌ **NO `new` keyword** (no heap allocations)
3. ❌ **NO `FindObjectOfType` / `GetComponent`** (cache in Awake/Start)
4. ❌ **NO string concatenation** (use `StringBuilder` or cached strings)
5. ❌ **NO boxing** (avoid `object` casts on value types)
6. ❌ **NO foreach on Lists** (use indexed `for` loop to avoid enumerator allocation)
7. ✅ **YES object pools** for spawned objects (projectiles, enemies, VFX)
8. ✅ **YES cached component references** (cache in Awake/Start)
9. ✅ **YES indexed for loops** (`for (int i = 0; i < list.Count; i++)`)
10. ✅ **YES Unity Jobs** (if needed for heavy computation)

### Object Pooling (Required for Spawned Objects)

```csharp
// ✅ Use Z13.Core.ObjectPool<T> for all spawned objects
private ObjectPool<Projectile> m_projectilePool;

void Awake()
{
    m_projectilePool = new ObjectPool<Projectile>(
        m_projectilePrefab,
        m_container,
        poolSize: 200,
        onReturn: proj => proj.OnReturnToPool()
    );
}

void Fire()
{
    Projectile proj = m_projectilePool.Get(position, rotation);
    proj.Initialize(direction, speed, damage);
    // Later: m_projectilePool.Return(proj);
}
```

**Pooled Objects**:
- Projectiles (player + enemy)
- Enemies
- Pickups
- VFX particles
- UI popup text (damage numbers, combo text)

### Caching Component References

```csharp
// ❌ WRONG - GetComponent every frame (expensive!)
void Update()
{
    GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
}

// ✅ CORRECT - Cache in Awake
private Rigidbody2D m_rb;

void Awake()
{
    m_rb = GetComponent<Rigidbody2D>();
}

void Update()
{
    m_rb.linearVelocity = Vector2.zero;
}
```

### String Performance

```csharp
// ❌ WRONG - String concatenation creates garbage
void Update()
{
    m_label.text = "Score: " + score + " Combo: " + combo;
}

// ✅ CORRECT - Use cached format strings
private const string SCORE_FORMAT = "Score: {0} Combo: {1}";

void Update()
{
    m_label.text = string.Format(SCORE_FORMAT, score, combo);
}

// ✅ EVEN BETTER - Use TextMeshPro SetText (zero allocation)
void Update()
{
    m_label.SetText("Score: {0} Combo: {1}", score, combo);
}
```

---

## 🏗️ Unity Architecture Patterns

### 1. Event-Driven Communication (Preferred)

**DO**: Use EventBus for cross-system communication

```csharp
// Publish event
EventBus.Publish(new EnemyKilledEvent
{
    enemyType = EnemyType.Boss,
    position = transform.position,
    pointValue = 1000
});

// Subscribe (in OnEnable) / Unsubscribe (in OnDisable)
void OnEnable()
{
    EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
}

void OnDisable()
{
    EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
}

private void OnEnemyKilled(EnemyKilledEvent evt)
{
    // Handle event
}
```

**DON'T**: Use direct references between systems

```csharp
// ❌ WRONG - Tight coupling
public class Enemy : MonoBehaviour
{
    public GameManager gameManager;  // Bad!

    void Die()
    {
        gameManager.AddScore(100);  // Tightly coupled
    }
}

// ✅ CORRECT - Loose coupling via events
public class Enemy : MonoBehaviour
{
    void Die()
    {
        EventBus.Publish(new EnemyKilledEvent { pointValue = 100 });
    }
}
```

**Event Subscription Pattern**:
- Subscribe in `OnEnable()` (not Awake/Start)
- Unsubscribe in `OnDisable()` (not OnDestroy)
- This ensures proper cleanup when objects are disabled

### 2. ScriptableObjects for Configuration (Preferred)

**DO**: Use ScriptableObjects for balance values and configs

```csharp
// GameBalanceConfig.asset (ScriptableObject)
public class GameBalanceConfig : ScriptableObject
{
    public PlayerConfig player;
    public EnemyConfig[] enemies;
    public WeaponConfig weapons;
}

// Access via ConfigProvider (static singleton)
int maxHealth = ConfigProvider.Player.maxHealth;
float enemySpeed = ConfigProvider.Balance.GetEnemyConfig(EnemyType.Boss).speed;
```

**DON'T**: Hardcode balance values in scripts

```csharp
// ❌ WRONG - Hardcoded values (requires recompile to change)
public class Player : MonoBehaviour
{
    private int m_maxHealth = 100;  // Bad!
    private float m_speed = 5f;     // Bad!
}

// ✅ CORRECT - Config-driven
public class Player : MonoBehaviour
{
    private int MaxHealth => ConfigProvider.Player.maxHealth;
    private float Speed => ConfigProvider.Player.speed;
}
```

### 3. Singleton Pattern (Boot Scene Pattern)

**TRUE SINGLETONS** (App-lifetime, persist via DontDestroyOnLoad):
- `GameStateManager` - Global game state machine
- `InputManager` - Input handling and events
- `AudioManager` - Audio playback
- `MusicManager` - Music management
- `ConfigProvider` (static) - Config access
- `EventBus` (static) - Pub/sub messaging

```csharp
// ✅ TRUE SINGLETON - Safe to access anywhere
GameStateManager.Instance.ChangeState(GameState.Playing);
AudioManager.Instance.PlaySFX(clipName);
InputManager.Instance.OnFirePressed += HandleFire;
```

**SCENE-SPECIFIC OBJECTS** (Use serialized fields when possible):
- `GameManager` - Scene-specific gameplay (score, combo)
- `UIManager` - Scene-specific UI
- `LevelManager` - Scene-specific level logic
- `VFXManager` - Scene-specific VFX spawning
- `FeedbackManager` - Scene-specific game feel

```csharp
// ✅ PREFERRED - Serialized field reference
[SerializeField] private GameManager m_gameManager;

void Start()
{
    m_gameManager.AddScore(100);
}

// ⚠️ OK but less preferred - Instance access
void Start()
{
    GameManager.Instance.AddScore(100);
}
```

### 4. State Machines (Enums + Switch)

**DO**: Use enum-based state machines with switch statements

```csharp
private enum EnemyState { Spawning, Alive, Dying, Dead }
private EnemyState m_state = EnemyState.Spawning;

void Update()
{
    switch (m_state)
    {
        case EnemyState.Spawning:
            UpdateSpawning();
            break;
        case EnemyState.Alive:
            UpdateAlive();
            break;
        case EnemyState.Dying:
            UpdateDying();
            break;
        case EnemyState.Dead:
            // Do nothing
            break;
    }
}
```

**DON'T**: Use boolean flags for complex state

```csharp
// ❌ WRONG - Boolean soup (hard to maintain)
private bool m_isSpawning;
private bool m_isAlive;
private bool m_isDying;
private bool m_isDead;

void Update()
{
    if (m_isSpawning) { }
    else if (m_isAlive) { }
    else if (m_isDying) { }
}
```

---

## 🎯 Unity-Specific APIs (6000.x)

### Modern Unity APIs (Required)

```csharp
// ✅ Unity 6000.x APIs
Rigidbody2D.linearVelocity         // NOT .velocity (deprecated)
FindFirstObjectByType<T>()         // NOT FindObjectOfType<T>() (deprecated)
FindObjectsByType<T>()             // NOT FindObjectsOfType<T>() (deprecated)
Keyboard.current.spaceKey          // New Input System (NOT Input.GetKey)

// ❌ Deprecated (causes warnings)
rigidbody2D.velocity               // Use .linearVelocity
FindObjectOfType<GameManager>()    // Use FindFirstObjectByType
Input.GetKeyDown(KeyCode.Space)    // Use new Input System
```

### New Input System (Required)

```csharp
// ✅ CORRECT - New Input System
using UnityEngine.InputSystem;

void OnEnable()
{
    InputManager.Instance.OnFirePressed += HandleFire;
}

void OnDisable()
{
    InputManager.Instance.OnFirePressed -= HandleFire;
}

// Or direct access to Keyboard
if (Keyboard.current.spaceKey.wasPressedThisFrame)
{
    Jump();
}
```

```csharp
// ❌ WRONG - Legacy Input (causes InvalidOperationException)
if (Input.GetKeyDown(KeyCode.Space))  // DO NOT USE!
{
    Jump();
}
```

### URP-Specific Rendering

```csharp
// URP Pipeline
using UnityEngine.Rendering.Universal;

// Access URP-specific features
var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

// Post-processing (URP Volume system)
using UnityEngine.Rendering;

Volume m_volume = GetComponent<Volume>();
if (m_volume.profile.TryGet<Bloom>(out var bloom))
{
    bloom.intensity.value = 2f;
}
```

---

## 📦 Z13.Core Shared Package

**Location**: `Assets/_Project/Packages/Z13.Core/Runtime/`

**Reusable systems** across all Z13 Labs projects:

| Module | Purpose | Usage |
|--------|---------|-------|
| `EventBus` | Type-safe pub/sub messaging | `EventBus.Publish(evt)` |
| `ObjectPool<T>` | Zero-allocation object pooling | `pool.Get() / pool.Return()` |
| `LogHelper` | Editor-only logging (stripped in builds) | `LogHelper.Log("msg")` |
| `SaveSystemBase<T>` | Generic save/load with JSON | Extend for game-specific save |
| `IBootable` | Singleton initialization interface | Implement for boot scene pattern |

**Rule**: Z13.Core code must be **fully generic** (no game-specific types).

```csharp
// ✅ CORRECT - Generic reusable code
namespace Z13.Core
{
    public class ObjectPool<T> where T : Component { }
}

// ❌ WRONG - Game-specific type in Z13.Core
namespace Z13.Core
{
    public class EnemyPool  // References NeuralBreak.Entities.Enemy (bad!)
    {
        public Enemy Get() { }
    }
}
```

**Pattern**: Generic base in Z13.Core + Game-specific implementation

```csharp
// Z13.Core/SaveSystemBase.cs (generic)
public abstract class SaveSystemBase<T> where T : class, new()
{
    public abstract void Save(T data);
    public abstract T Load();
}

// NeuralBreak/SaveSystem.cs (game-specific)
public class SaveSystem : SaveSystemBase<SaveData>
{
    public override void Save(SaveData data) { /* ... */ }
    public override T Load() { /* ... */ }
}
```

---

## 🚀 Common Unity Tasks

### Creating New Enemy

1. **Add enum**: `GameEvents.cs` → `public enum EnemyType { DataMite, ScanDrone, NewEnemy }`
2. **Create class**: Inherit `EnemyBase`, implement `UpdateAI()`
3. **Add config**: `GameBalanceConfig.asset` → Add `EnemyConfig` entry
4. **Create prefab**: GameObject with:
   - `CircleCollider2D` (trigger)
   - `SpriteRenderer`
   - `Rigidbody2D` (kinematic, no gravity)
   - Enemy script component
5. **Register pool**: `EnemyPoolManager.cs` → Add pool initialization

### Creating New ScriptableObject Config

```csharp
// 1. Create ScriptableObject class
[CreateAssetMenu(menuName = "Neural Break/Config/My Config")]
public class MyConfig : ScriptableObject
{
    public int value;
    public float speed;
}

// 2. Create asset: Project window → Right-click → Create → Neural Break → Config → My Config
// 3. Access via ConfigProvider or direct reference
```

### Creating New Event

```csharp
// Add to GameEvents.cs (NOT Z13.Core/EventBus.cs)
public struct MyEvent
{
    public int value;
    public Vector2 position;
    public EnemyType enemyType;
}

// Publish
EventBus.Publish(new MyEvent { value = 10, position = transform.position });

// Subscribe
EventBus.Subscribe<MyEvent>(OnMyEvent);
```

---

## 🐛 Common Unity Pitfalls

### 1. Transform Position Comparison (Use `transform.position`, not `this.transform`)

```csharp
// ✅ CORRECT
Vector2 pos = transform.position;

// ⚠️ REDUNDANT (works but unnecessary)
Vector2 pos = this.transform.position;
```

### 2. Coroutine Lifecycle

```csharp
// ✅ CORRECT - Store coroutine reference to stop later
private Coroutine m_flashCoroutine;

void StartFlash()
{
    if (m_flashCoroutine != null)
    {
        StopCoroutine(m_flashCoroutine);
    }
    m_flashCoroutine = StartCoroutine(FlashCoroutine());
}

// ❌ WRONG - Can't stop unnamed coroutine
void StartFlash()
{
    StartCoroutine(FlashCoroutine());  // Can't stop this!
}
```

### 3. OnEnable/OnDisable Event Subscription

```csharp
// ✅ CORRECT - Subscribe in OnEnable, Unsubscribe in OnDisable
void OnEnable()
{
    EventBus.Subscribe<MyEvent>(OnMyEvent);
}

void OnDisable()
{
    EventBus.Unsubscribe<MyEvent>(OnMyEvent);
}

// ❌ WRONG - Subscribe in Start (won't work if object disabled then re-enabled)
void Start()
{
    EventBus.Subscribe<MyEvent>(OnMyEvent);
}
```

### 4. Vector2 vs Vector3

```csharp
// ✅ CORRECT - Use Vector2 for 2D games
Vector2 position = transform.position;  // Implicit cast
Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

// ⚠️ AVOID - Unnecessary Vector3 for 2D
Vector3 position = transform.position;  // Wastes Z component
```

### 5. Collider2D Trigger vs Collision

```csharp
// Trigger Collider (isTrigger = true)
void OnTriggerEnter2D(Collider2D other)
{
    // No physics response, just detection
}

// Physical Collider (isTrigger = false)
void OnCollisionEnter2D(Collision2D collision)
{
    // Physics response (bounce, stop, etc.)
}
```

---

## 📂 Project Structure

```
Assets/
├── Scenes/
│   └── main-neural-break.unity        # Main gameplay scene
└── _Project/
    ├── Packages/
    │   └── Z13.Core/                  # Shared package (generic reusable code)
    │       └── Runtime/
    │           ├── EventBus.cs        # Type-safe pub/sub
    │           ├── ObjectPool.cs      # Zero-alloc pooling
    │           ├── LogHelper.cs       # Editor-only logging
    │           ├── SaveSystemBase.cs  # Generic save/load
    │           └── IBootable.cs       # Singleton interface
    ├── Scripts/
    │   ├── Audio/                     # AudioManager, MusicManager
    │   ├── Combat/                    # WeaponSystem, Projectile, Upgrades/
    │   ├── Config/                    # ConfigProvider, GameBalanceConfig
    │   ├── Core/                      # GameStateManager, GameManager, LevelManager
    │   │   ├── GameEvents.cs          # ⭐ Game-specific events & enums
    │   │   ├── GameStateManager.cs    # Global state machine (singleton)
    │   │   └── GameManager.cs         # Scene-specific gameplay
    │   ├── Entities/                  # Player/, Enemies/, Pickups/
    │   │   ├── Player/
    │   │   │   ├── PlayerController.cs
    │   │   │   ├── PlayerHealth.cs
    │   │   │   └── PlayerVisuals.cs
    │   │   └── Enemies/
    │   │       ├── EnemyBase.cs       # Base class for all enemies
    │   │       ├── DataMite.cs
    │   │       ├── ScanDrone.cs
    │   │       └── ...
    │   ├── Graphics/                  # VFXManager, FeedbackManager, CameraController
    │   ├── Input/                     # InputManager (singleton)
    │   └── UI/                        # UIManager, HUD, Menus
    ├── Prefabs/
    │   ├── Enemies/                   # Enemy prefabs
    │   ├── Pickups/                   # Pickup prefabs
    │   └── UI/                        # UI prefabs
    └── Resources/
        ├── Config/
        │   └── GameBalanceConfig.asset  # ⭐ Master balance config
        └── Upgrades/                    # Upgrade definitions
```

**Key Files**:
- `GameEvents.cs` - All game-specific events and enums
- `GameBalanceConfig.asset` - Master balance config (ScriptableObject)
- `EnemyBase.cs` - Base class for all enemies (inherit from this)
- `ConfigProvider.cs` - Static access to configs

---

## 🎨 Namespaces

```csharp
// Z13.Core - Shared reusable package
namespace Z13.Core
{
    public class EventBus { }
    public class ObjectPool<T> { }
}

// NeuralBreak.* - Game-specific code
namespace NeuralBreak.Core { }        // GameManager, LevelManager, etc.
namespace NeuralBreak.Entities { }    // Player, Enemies, Pickups
namespace NeuralBreak.Combat { }      // WeaponSystem, Projectile, Upgrades
namespace NeuralBreak.Graphics { }    // VFXManager, CameraController
namespace NeuralBreak.Input { }       // InputManager
namespace NeuralBreak.UI { }          // UIManager, HUD
namespace NeuralBreak.Audio { }       // AudioManager, MusicManager
namespace NeuralBreak.Config { }      // ConfigProvider, GameBalanceConfig
```

---

## 🔧 Development Workflow

### Before Coding
1. **Restate task** (1-2 lines) and outline plan
2. **List files** to modify and why
3. **Verify APIs/files exist** in project (no hallucination)
4. **Implement as small atomic diffs** (easier to review)

### After Coding
1. **Show diffs** and reasoning
2. **Note potential risks** or breaking changes
3. **Provide rollback path** if needed

### Git Commit Format
```
type(scope): summary

feat(combat): add beam weapon upgrade
fix(enemies): collision detection on VoidSphere
config(balance): reduce Level 3 spawn rates by 20%
refactor(core): extract LevelGenerator from LevelManager
```

---

## 📖 Quick Reference

### Most-Edited Files
1. `GameBalanceConfig.asset` - Balance values
2. `LevelGenerator.cs` - Level configurations
3. `EnemyBase.cs` - Enemy AI patterns
4. `WeaponSystem.cs` - Weapon behavior
5. `GameEvents.cs` - Event/enum definitions

### Common Events
```csharp
EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
```

### Debug Menu Commands
- **Neural Break > Create Upgrades > Create Starter Pack** - Generate upgrade assets
- **Neural Break > Clear Save Data** - Reset player progress
- **Right-click GameManager** - Debug commands (add score, spawn enemies, etc.)

---

## 🚨 Critical Rules Summary

1. ⚡ **NO LINQ in hot paths** (Update, FixedUpdate, LateUpdate)
2. ⚡ **NO allocations in hot paths** (no `new`, no string concat, no boxing)
3. ⚡ **Cache all component references** in Awake/Start
4. 🎯 **Use EventBus for cross-system communication** (not direct references)
5. 🎯 **Use ScriptableObjects for balance values** (not hardcoded constants)
6. 🎯 **Use object pools for spawned objects** (projectiles, enemies, VFX)
7. 🔧 **Use new Input System APIs** (not legacy `Input.*`)
8. 🔧 **Use Unity 6000.x APIs** (`FindFirstObjectByType`, `linearVelocity`, etc.)
9. 📝 **Follow naming conventions** (`m_` prefix, `s_` prefix, PascalCase properties)
10. 📦 **Keep Z13.Core generic** (no game-specific types)

---

## 📚 Extended Documentation

See `Documents/` folder for detailed guides:
- `CLAUDE_REFERENCE.md` - Detailed patterns and examples
- `UPGRADE_SYSTEM_SPEC.md` - Upgrade system architecture
- `PLAYER_VISUAL_FEEDBACK_FIX.md` - Player visual feedback system
- `SHOWCASE_LEVELS_4_10_FIX.md` - Level design showcase

---

## 👨‍💻 Developer

**Johnny @ Z13 Labs** | johnny@z13labs.com

**Original TypeScript/Three.js Prototype**: `D:\Projects\Neural-Break-Unity`
- Reference for game design decisions, balance values, original gameplay feel
- Built with TypeScript, Three.js, Web Audio API

---

**Last Updated**: 2026-02-10
