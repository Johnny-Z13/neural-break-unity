# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**Neural Break** is a twin-stick survival arena shooter ported from TypeScript/Three.js to Unity 6000.x. It features 99 levels, 8 enemy types, procedural effects, and a rogue-like card upgrade system.

- **Engine**: Unity 6000.0.31f1+ (Unity 6 LTS)
- **Rendering**: Universal Render Pipeline (URP)
- **Input**: Unity Input System (New Input System) - **NOT legacy Input**
- **Language**: C# 10.0
- **Architecture**: Event-driven, config-driven, zero-allocation pooling

---

## Critical Rules

### Input System
**NEVER use legacy `UnityEngine.Input` API**. Project uses Unity Input System exclusively:

```csharp
// ❌ WRONG (causes InvalidOperationException)
if (Input.GetButtonDown("Fire1"))
if (Input.GetKeyDown(KeyCode.Space))

// ✅ CORRECT
if (Keyboard.current.spaceKey.wasPressedThisFrame)
if (Gamepad.current.buttonSouth.wasPressedThisFrame)

// ✅ BEST (use InputManager events)
InputManager.Instance.OnConfirmPressed += HandleConfirm;
```

### Unity 6000.x APIs
**Always use Unity 6000.x APIs**, not deprecated ones:

```csharp
// ❌ WRONG (deprecated in Unity 6)
FindObjectOfType<GameManager>()

// ✅ CORRECT
FindFirstObjectByType<GameManager>()  // For single instance
FindObjectsByType<Enemy>()            // For multiple instances
```

### File Size Limit
**All files must be ≤300 LOC**. If a file exceeds this, extract helpers/utilities to separate files.

### No Singletons (Except These 8)
Do not create new singleton patterns. Only these 8 singletons are allowed:
1. `GameManager`
2. `InputManager`
3. `UIManager`
4. `AudioManager`
5. `PermanentUpgradeManager`
6. `UpgradePoolManager`
7. `ConfigProvider` (static)
8. `EventBus` (static)

---

## Architecture

### Event-Driven Communication
All systems communicate via **EventBus** (pub/sub pattern). Never use direct references between systems.

**Pattern**:
```csharp
// Define event struct in EventBus.cs
public struct EnemyKilledEvent
{
    public EnemyType enemyType;
    public Vector2 position;
    public int scoreValue;
    public int xpValue;
}

// Publisher
EventBus.Publish(new EnemyKilledEvent { ... });

// Subscriber (in Awake/OnEnable)
EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);

// Unsubscribe (in OnDestroy/OnDisable)
EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
```

**Location**: All event structs are defined in `Assets/_Project/Scripts/Core/EventBus.cs`.

### Config-Driven Design
**All balance values live in ScriptableObjects**, not hard-coded in scripts.

**Master Config**: `Assets/_Project/Resources/Config/GameBalanceConfig.asset`

**Access Pattern**:
```csharp
// Use ConfigProvider static accessor
using NeuralBreak.Config;

// Get player config
int maxHealth = ConfigProvider.Player?.maxHealth ?? 100;

// Get enemy config
var enemyConfig = ConfigProvider.Balance?.GetEnemyConfig(EnemyType.DataMite);
int health = enemyConfig?.health ?? 1;
```

**When to Update Config**:
- Player stats (health, speed, dash)
- Enemy stats (health, speed, damage, spawn rates)
- Weapon stats (damage, fire rate, heat)
- Pickup effects and spawn rates
- Scoring/combo parameters

### Object Pooling (Zero-Allocation)
**Never instantiate/destroy at runtime**. Use `ObjectPool<T>` for:
- Projectiles
- Enemies
- Pickups
- VFX particles
- Damage numbers

**Pattern**:
```csharp
// Create pool
private ObjectPool<Projectile> _projectilePool;

void Start()
{
    _projectilePool = new ObjectPool<Projectile>(
        prefab: _projectilePrefab,
        parent: _projectileContainer,
        initialSize: 100,
        onGet: (proj) => proj.gameObject.SetActive(true),
        onReturn: (proj) => proj.gameObject.SetActive(false)
    );
}

// Get from pool
var projectile = _projectilePool.Get();
projectile.Initialize(position, direction, damage);

// Return to pool
_projectilePool.Return(projectile);
```

**Location**: `Assets/_Project/Scripts/Core/ObjectPool.cs`

---

## Weapon & Upgrade System

### Two-Tier Upgrade System

1. **Temporary Pickups** (in-level, 6-10 second duration)
   - Managed by `WeaponUpgradeManager`
   - Examples: Rapid Fire, Piercing, Homing
   - Collected from enemy drops

2. **Permanent Upgrades** (between levels, rogue-like cards)
   - Managed by `PermanentUpgradeManager`
   - Selected from 3 cards at level completion
   - Stacks additively (fire rate, damage, etc.)
   - Defined in `UpgradeDefinition` ScriptableObjects

### Weapon Modifier Flow
```
WeaponSystem.GetFireRate()
    ↓ applies
WeaponUpgradeManager (temporary mods: rapid fire, spread)
    ↓ applies
PermanentUpgradeManager (permanent mods: card selections)
    ↓ result
Final fire rate = base × temporary × permanent
```

### Power Level Progression
Power pickups increase power level (0 → 20), which auto-upgrades fire patterns:
- Level 0-4: Single shot
- Level 5-9: Double shot
- Level 10-14: Triple shot
- Level 15-19: Quad shot
- Level 20: X5 shot (max)

**Config Location**: `GameBalanceConfig.weaponSystem.powerLevels`

---

## Key Systems

### GameManager (Core State Machine)
**Location**: `Assets/_Project/Scripts/Core/GameManager.cs`

**States**: `StartScreen`, `Playing`, `Paused`, `RogueChoice`, `GameOver`, `Victory`

**Responsibilities**:
- Game state transitions
- Scoring & combo system
- Level completion flow
- Upgrade selection triggering

**Level Transition Flow**:
```
LevelCompleted event
    ↓
GameManager.OnLevelCompleted()
    ↓
LevelTransition() coroutine
    ↓
Check: Should show upgrade selection?
    ├─ Yes: SetState(RogueChoice) → Show cards → Wait for selection → Advance level
    └─ No: Wait 2s → Advance level
```

### LevelManager (Objective Tracking)
**Location**: `Assets/_Project/Scripts/Core/LevelManager.cs`

**Responsibilities**:
- Level objectives (kill quotas, survival time)
- Enemy unlock schedule (DataMites at L1, Bosses at L5, etc.)
- Level progression (1 → 99)

### EnemySpawner (Spawning Logic)
**Location**: `Assets/_Project/Scripts/Entities/Enemies/EnemySpawner.cs`

**Spawning Strategy**:
- **Waves**: Spawn burst of enemies, wait for clear, repeat
- **Rate-Based**: Continuous spawning at configured intervals
- **Boss Waves**: Special single-boss spawns

**Spawn Position**: Off-screen positions with buffer distance from player

### InputManager (Twin-Stick Controls)
**Location**: `Assets/_Project/Scripts/Input/InputManager.cs`

**Input Modes**:
- **Keyboard + Mouse**: WASD movement, mouse aim, left-click fire
- **Gamepad**: Left stick movement, right stick aim, triggers fire

**Events**:
```csharp
InputManager.Instance.OnFirePressed += ...
InputManager.Instance.OnDashPressed += ...
InputManager.Instance.OnPausePressed += ...
```

---

## Project Structure

```
Assets/_Project/
├── Scripts/
│   ├── Audio/              # AudioManager, MusicManager, ProceduralSFX
│   ├── Combat/             # WeaponSystem, Projectile, Upgrades/
│   │   └── Upgrades/       # PermanentUpgradeManager, UpgradeDefinition
│   ├── Config/             # ConfigProvider, GameBalanceConfig
│   ├── Core/               # GameManager, LevelManager, EventBus, ObjectPool
│   ├── Entities/           # Player, Enemies/, Pickups/
│   │   ├── Enemies/        # EnemyBase, 8 enemy implementations, EnemySpawner
│   │   └── Pickups/        # PickupBase, PowerUpPickup, etc.
│   ├── Graphics/           # CameraController, VFX, Starfield, SpriteGenerator
│   ├── Input/              # InputManager, GamepadRumble
│   ├── UI/                 # UIManager, HUD, Menus, UpgradeSelectionScreen
│   └── Utils/              # LogHelper, Extensions
├── Prefabs/
│   ├── Enemies/            # Enemy prefabs (8 types)
│   ├── Pickups/            # Pickup prefabs
│   └── UI/                 # UI screen prefabs
├── Resources/
│   ├── Config/             # GameBalanceConfig.asset
│   └── Upgrades/           # UpgradeDefinition assets (FireRate/, Damage/, etc.)
└── Art/                    # Sprites, textures (procedurally generated at runtime)
```

### Namespace Organization
```csharp
NeuralBreak.Core        // GameManager, EventBus, LevelManager
NeuralBreak.Entities    // Player, Enemies, Pickups
NeuralBreak.Combat      // WeaponSystem, Projectiles, Upgrades
NeuralBreak.Input       // InputManager, GamepadRumble
NeuralBreak.Graphics    // CameraController, VFX, SpriteGenerator
NeuralBreak.UI          // UIManager, HUD, Screens
NeuralBreak.Audio       // AudioManager, MusicManager
NeuralBreak.Config      // ConfigProvider, GameBalanceConfig
NeuralBreak.Utils       // Helpers, Extensions
```

---

## Common Development Tasks

### Creating a New Enemy Type

1. **Create enemy script** (inherit from `EnemyBase`):
```csharp
using NeuralBreak.Entities;

public class MyEnemy : EnemyBase
{
    public override EnemyType EnemyType => EnemyType.MyEnemy; // Add to enum first

    protected override void UpdateAI()
    {
        // Movement logic
        Vector2 direction = GetDirectionToPlayer();
        transform.position += (Vector3)(direction * _speed * Time.deltaTime);
    }
}
```

2. **Add to EnemyType enum** (`Assets/_Project/Scripts/Core/EventBus.cs`):
```csharp
public enum EnemyType
{
    DataMite, ScanDrone, Fizzer, UFO,
    ChaosWorm, VoidSphere, CrystalShard, Boss,
    MyEnemy  // ADD HERE
}
```

3. **Add config** (`GameBalanceConfig.asset`):
```yaml
myEnemy:
  health: 50
  speed: 2.0
  contactDamage: 10
  xpValue: 20
  scoreValue: 500
  collisionRadius: 0.5
  spawnDuration: 0.25
  deathDuration: 0.5
  color: {r: 1, g: 0, b: 1, a: 1}
```

4. **Create prefab** (in `Assets/_Project/Prefabs/Enemies/`):
   - Add `MyEnemy` component
   - Add `CircleCollider2D` (trigger)
   - Add `SpriteRenderer`
   - Add `Rigidbody2D` (Kinematic)
   - Tag: "Enemy"

5. **Register in EnemyPoolManager**:
```csharp
[SerializeField] private MyEnemy _myEnemyPrefab;

private void CreatePools()
{
    // ... existing pools ...
    _pools[EnemyType.MyEnemy] = new ObjectPool<EnemyBase>(...);
}
```

### Creating a New Upgrade

1. **Create UpgradeDefinition asset**:
   - Right-click in `Resources/Upgrades/FireRate/` (or appropriate folder)
   - Select `Create > Neural Break > Upgrades > Definition`

2. **Configure the asset**:
```yaml
Upgrade ID: rapid_fire_2
Display Name: Rapid Fire II
Description: +50% fire rate
Tier: Rare
Category: FireRate
Is Permanent: true
Max Stacks: 1

Modifiers:
  fireRateMultiplier: 1.5  # 50% faster
  damageMultiplier: 1.0    # No change
  additionalProjectiles: 0
  enableHoming: false
  # ... other fields default to 0/false
```

3. **Icon**: Assign a sprite icon (32x32 recommended)

4. **Test**: Upgrades load automatically from `Resources/Upgrades/**/*.asset`

### Adding a New Event

**Location**: `Assets/_Project/Scripts/Core/EventBus.cs`

```csharp
// Add to appropriate region (e.g., #region Combat Events)
public struct MyCustomEvent
{
    public int value;
    public Vector2 position;
    public string message;
}
```

**Usage**:
```csharp
// Publish
EventBus.Publish(new MyCustomEvent { value = 10, position = transform.position });

// Subscribe
EventBus.Subscribe<MyCustomEvent>(OnMyCustomEvent);

// Handler
private void OnMyCustomEvent(MyCustomEvent evt)
{
    Debug.Log($"Event received: {evt.message}");
}
```

---

## Testing & Debugging

### Unity Editor Menu Commands

**Neural Break Menu** (custom editor menu):
```
Neural Break > Create Upgrades > Create Starter Pack  // Generate 20+ upgrade assets
Neural Break > Clear Save Data                         // Reset high scores
Neural Break > Test Mode                               // Enable debug mode
```

### Debug Context Menu Commands

**GameManager**:
```
Right-click component > Debug: Start Arcade
Right-click component > Debug: Game Over
Right-click component > Debug: Victory
```

**WeaponSystem**:
```
Right-click component > Debug: Add Power Level
Right-click component > Debug: Max Power Level
Right-click component > Debug: Toggle Rapid Fire
```

### Console Logging

**Use LogHelper** for consistent logging:
```csharp
using NeuralBreak.Utils;

LogHelper.Log("[MySystem] Initialized successfully");
LogHelper.LogWarning("[MySystem] Config missing, using defaults");
LogHelper.LogError("[MySystem] Critical failure!");
```

**Event Tracing**: Enable verbose logging in `EventBus.cs` (line 74) to trace all events.

---

## Setup Requirements

### Scene Setup Checklist

Main scene (`main-neural-break.unity`) requires these GameObjects:

1. **Managers** (all singletons)
   - GameManager
   - InputManager
   - UIManager
   - AudioManager
   - PermanentUpgradeManager ⚠️ **MUST ADD MANUALLY**
   - UpgradePoolManager

2. **Player**
   - PlayerController
   - WeaponSystem
   - PlayerHealth
   - PlayerMovement

3. **Systems**
   - LevelManager
   - EnemySpawner
   - CameraController
   - VFXManager

4. **UI Canvas**
   - HUD (health bar, score, level, combo)
   - StartScreen
   - GameOverScreen
   - PauseScreen
   - UpgradeSelectionScreen

### Resource Requirements

**Config Asset**: `Assets/_Project/Resources/Config/GameBalanceConfig.asset`
- Must exist at this exact path (loaded via Resources.Load)

**Upgrade Assets**: `Assets/_Project/Resources/Upgrades/**/*.asset`
- Minimum 10-15 upgrade definitions required
- Use menu command: "Neural Break > Create Upgrades > Create Starter Pack"

---

## Performance Guidelines

### Zero-Allocation Gameplay
**During gameplay (Playing state), ZERO allocations are allowed**:
- ✅ Use object pools for everything spawned/destroyed
- ✅ Cache component references in Awake/Start
- ✅ Use struct events (not classes)
- ❌ No `new` keyword in Update/FixedUpdate
- ❌ No string concatenation in hot paths
- ❌ No FindObjectOfType in Update loops

### Update Loop Optimization
```csharp
// ❌ BAD: Repeated lookups
void Update()
{
    var player = FindFirstObjectByType<PlayerController>();  // SLOW
    if (player != null) { ... }
}

// ✅ GOOD: Cached reference
private PlayerController _player;
void Start() { _player = FindFirstObjectByType<PlayerController>(); }
void Update()
{
    if (_player != null) { ... }
}
```

### Object Pool Sizing
**Guidelines**:
- Projectiles: 100-200 (high fire rate)
- Enemies: 50-100 (max on-screen)
- Pickups: 20-30 (infrequent spawns)
- VFX: 30-50 (burst effects)

---

## MMFeedbacks Integration

**Feel** asset provides game juice (screen shake, hit stop, etc.).

**Pattern**:
```csharp
using MoreMountains.Feedbacks;

[SerializeField] private MMF_Player _hitFeedback;
[SerializeField] private MMF_Player _deathFeedback;

// Trigger feedback
_hitFeedback?.PlayFeedbacks();

// Create at runtime (for pooled objects)
var feedbackSetup = FindFirstObjectByType<FeedbackSetup>();
_hitFeedback = feedbackSetup.CreateHitFeedback(transform);
```

**Location**: `Assets/_Project/Scripts/Utils/FeedbackSetup.cs`

---

## Git Workflow

### Commit Message Format
```
type(scope): summary

Examples:
- feat(combat): add beam weapon projectile type
- fix(enemies): chaos worm collision detection
- refactor(ui): extract upgrade card to component
- config(balance): reduce boss health to 150
- docs(readme): update control scheme
```

### Recent Major Changes
```
116cbe3 - major REFACTORING (singleton reduction, modular code)
5a62043 - major refactoring and cleanup
60b6bdd - first port attempt (TypeScript → Unity)
79e6a5a - First commit of Unity port
```

---

## Known Issues & Gotchas

### PermanentUpgradeManager Missing
**Symptom**: Warning on game start: "[WeaponSystem] PermanentUpgradeManager not found"

**Fix**: Add `PermanentUpgradeManager` component to "Managers" GameObject in scene.

### No Upgrade Cards at Level End
**Symptom**: Blank upgrade selection screen, game gets stuck

**Fix**: Run Unity menu command: "Neural Break > Create Upgrades > Create Starter Pack"

### Input System Exceptions
**Symptom**: `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`

**Cause**: Using legacy `Input` API instead of `UnityEngine.InputSystem`

**Fix**: Replace all `Input.GetButtonDown()` with `Keyboard.current.*.wasPressedThisFrame`

### Camera Dynamic Zoom Issues
**Symptom**: Camera zooms too aggressively or not enough

**Fix**: Adjust `GameBalanceConfig.camera.minZoom` and `maxZoom` (default: 8-20)

---

## External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Universal Render Pipeline | 17.0.3+ | Rendering |
| Input System | 1.11.2+ | Modern input handling |
| TextMeshPro | 3.0.8+ | UI text rendering |
| Feel (MMFeedbacks) | Asset Store | Game juice, screen shake |
| CodeMonkey Toolkit | Asset Store | Utility helpers |

---

## Contact & Support

**Developer**: Johnny @ Z13 Labs
**Email**: johnny@z13labs.com
**Website**: [z13labs.com](https://z13labs.com)

**Original Project**: `D:\Projects\Neural-Break-Unity` (TypeScript/Three.js version)

---

## Quick Reference

### Most Common Files to Edit
1. `GameBalanceConfig.asset` - All balance values
2. `GameManager.cs` - Game state, scoring, level flow
3. `WeaponSystem.cs` - Firing patterns, heat, modifiers
4. `EnemyBase.cs` - Enemy behavior base class
5. `EventBus.cs` - Event definitions (add new events here)

### Most Common Commands
```bash
# Open project in Unity
unity -projectPath D:\Projects\Unity\neural-break-unity

# Run tests (Unity Test Framework)
# Window > General > Test Runner

# Build (File > Build Settings)
# Target: Windows Standalone (64-bit)
```

### Most Common Event Subscriptions
```csharp
EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
EventBus.Subscribe<GameOverEvent>(OnGameOver);
EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
```
