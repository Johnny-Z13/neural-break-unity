# NEURAL BREAK - Unity Porting Master Plan

> **Source Project:** `D:\Projects\Neural-Break-Unity` (TypeScript/Three.js - DELETE AFTER PORT)
> **Target Project:** `D:\Projects\Unity\neural-break-unity` (Unity 6000.3.0f1)
> **Document Version:** 1.0
> **Created:** 2026-01-20

---

## Executive Summary

Neural Break is a cyberpunk survival arena shooter with 99 levels, 8 enemy types, and multiple game modes. The original TypeScript codebase (~30,000 lines) is being ported to Unity C#.

**Current Unity Progress: ~35-40% Complete**
- Core architecture: COMPLETE
- Player systems: COMPLETE
- Combat systems: COMPLETE
- Enemy system: 1 of 8 enemies implemented
- UI: NOT STARTED
- Audio: NOT STARTED
- VFX/Polish: NOT STARTED

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What's Already Built](#2-whats-already-built)
3. [What Needs to Be Built](#3-what-needs-to-be-built)
4. [Recommended Asset Packs](#4-recommended-asset-packs)
5. [Feel Integration Strategy](#5-feel-integration-strategy)
6. [Implementation Phases](#6-implementation-phases)
7. [Enemy Implementation Guide](#7-enemy-implementation-guide)
8. [Configuration & Balance System](#8-configuration--balance-system)
9. [Performance Optimization](#9-performance-optimization)
10. [File Reference Map](#10-file-reference-map)

---

## 1. Architecture Overview

### Unity Project Structure
```
Assets/_Project/
├── Scripts/
│   ├── Core/           # GameManager, EventBus, ObjectPool, GameState, LevelManager
│   ├── Entities/
│   │   ├── Player/     # PlayerController, PlayerHealth
│   │   └── Enemies/    # EnemyBase, DataMite, + 7 more needed
│   ├── Combat/         # WeaponSystem, Projectile
│   ├── Input/          # InputManager
│   ├── Graphics/       # CameraController
│   ├── UI/             # (EMPTY - needs HUD, menus, screens)
│   ├── Audio/          # (EMPTY - needs AudioManager)
│   └── Pickups/        # (EMPTY - needs pickup system)
├── Prefabs/
│   ├── Player/         # (needs player prefab)
│   ├── Enemies/        # (needs 8 enemy prefabs)
│   ├── Projectiles/    # (needs projectile prefab)
│   ├── Pickups/        # (needs 5 pickup prefabs)
│   ├── UI/             # (needs UI prefabs)
│   └── VFX/            # (needs VFX prefabs)
├── ScriptableObjects/
│   ├── EnemyData/      # Enemy configuration assets
│   ├── LevelData/      # Level progression data
│   └── WeaponData/     # Weapon balance data
├── Art/
│   ├── Sprites/        # 2D sprites
│   └── Textures/       # Textures and materials
├── Audio/
│   ├── Music/          # Background tracks
│   └── SFX/            # Sound effects
├── Materials/          # Shaders and materials
└── VFX/                # Particle systems
```

### Namespace Organization
```csharp
NeuralBreak.Core        // GameManager, EventBus, GameState, LevelManager
NeuralBreak.Entities    // Player, enemies, pickups
NeuralBreak.Combat      // Weapons, projectiles
NeuralBreak.Input       // Input handling
NeuralBreak.Graphics    // Camera, VFX
NeuralBreak.UI          // HUD, menus, screens
NeuralBreak.Audio       // Sound management
```

---

## 2. What's Already Built

### Core Systems (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `GameManager.cs` | COMPLETE | Singleton, state machine, combo/multiplier system, event publishing |
| `EventBus.cs` | COMPLETE | Type-safe pub/sub, 20+ event types defined |
| `ObjectPool.cs` | COMPLETE | Generic pooling, pre-warming, MonoBehaviour wrapper |
| `GameState.cs` | COMPLETE | Enums, GameStats tracking, score/XP values |
| `LevelManager.cs` | PARTIAL | Basic 1-99 tracking, needs difficulty scaling |

### Player Systems (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `PlayerController.cs` | COMPLETE | Movement (accel/decel), dash with invuln, boundary enforcement |
| `PlayerHealth.cs` | COMPLETE | Health, shields (max 3), invulnerability, damage feedback |

### Combat Systems (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `WeaponSystem.cs` | COMPLETE | Fire rate, heat system, power levels 0-10, pooling |
| `Projectile.cs` | COMPLETE | Movement, lifetime, collision, trail, scaling |

### Input System (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `InputManager.cs` | COMPLETE | Keyboard + gamepad, events, deadzone handling |

### Graphics (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `CameraController.cs` | COMPLETE | Follow, dynamic zoom, screen shake, event reactions |

### Enemy System (15% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `EnemyBase.cs` | COMPLETE | Abstract base, state machine, lifecycle, Feel hooks |
| `EnemySpawner.cs` | PARTIAL | DataMite spawning works, 7 others need implementation |
| `DataMite.cs` | COMPLETE | Pursuit AI, oscillation, visual states |

### Feel Integration (Ready)
- 19 MMF_Player references across 6 scripts
- All ready for feedback configuration
- Feel package installed at `Assets/Feel/`

---

## 3. What Needs to Be Built

### Priority 1: Remaining Enemies (7 types)

| Enemy | Health | Speed | Damage | XP | Special Ability |
|-------|--------|-------|--------|-----|-----------------|
| ScanDrone | 30 | 1.2 | 15 | 6 | Ranged fire (2s cooldown) |
| Fizzer | 2 | 8.0 | 6 | 15 | Burst fire, erratic movement |
| UFO | 30 | 2.8 | 12 | 25 | Hit-and-run, dive attacks |
| ChaosWorm | 100 | 1.5 | 15 | 35 | 12 segments, death bullet spray |
| VoidSphere | 650 | 0.5 | 40 | 50 | Burst fire (4 shots), massive |
| CrystalShard | 250 | 1.8 | 25 | 45 | 6 orbiting crystals |
| Boss | 180 | 0.3 | 25 | 100 | 3-phase AI, expanding ring attack |

### Priority 2: Pickup System (5 types)

| Pickup | Effect | Spawn Condition |
|--------|--------|-----------------|
| PowerUp | +1 weapon level (max 10), +60% damage | Regular spawn |
| SpeedUp | +5% speed (max 20 levels = 100%) | Regular spawn |
| MedPack | +35 HP | When player < 80% health |
| Shield | +1 shield (max 3) | Regular spawn |
| Invulnerable | 7s god mode | Rare spawn |

### Priority 3: UI System

| Screen | Elements |
|--------|----------|
| **HUD** | Score + multiplier, health bar, shield icons, power/speed bars, level/objectives, combo counter, heat bar |
| **StartScreen** | Title, Arcade/Rogue/Test buttons, Leaderboard, Options |
| **PauseScreen** | Resume, Restart, Menu, Options |
| **GameOverScreen** | Final score, stats, high score comparison, Restart/Menu |
| **VictoryScreen** | "YOU BEAT NEURAL BREAK!", final stats |
| **LeaderboardScreen** | Top 10 per mode, name entry |
| **OptionsScreen** | Volume sliders, graphics toggles |
| **RogueChoiceScreen** | 3 mutation choices between layers |

### Priority 4: Level System Enhancement

- **99 Level Configurations** with specific objectives
- **Surprise Levels** (every 5th level has special theme)
- **Difficulty Scaling** (per-level multipliers)
- **Rogue Mode** layer system with wormhole progression

### Priority 5: Audio System

| Sound Type | Sounds Needed |
|------------|---------------|
| **Player** | Shoot, dash, hit, heal, level-up, death |
| **Enemies** | Spawn, hit, death (per type), boss music |
| **UI** | Menu select, confirm, cancel, pause |
| **Ambience** | Background music, tension escalation |

### Priority 6: VFX & Polish

| Effect | Purpose |
|--------|---------|
| **Particles** | Explosions, trails, spawn bursts, death effects |
| **Screen Effects** | Shake (done), chromatic aberration, flash |
| **Starfield** | Animated background (Arcade: drift, Rogue: scroll) |
| **Arena Boundary** | Glowing energy barrier |

---

## 4. Recommended Asset Packs

### Essential Purchases

#### VFX & Shaders

1. **[All In 1 Sprite Shader](https://assetstore.unity.com/packages/vfx/shaders/all-in-1-sprite-shader-156513)** (~$35)
   - 42+ combinable effects (glow, distortion, outline, etc.)
   - Perfect for cyberpunk neon aesthetics
   - Mobile-optimized, URP compatible
   - 2-click setup, batching support

2. **[PRO Effects: Sci-fi Shooter FX](https://assetstore.unity.com/packages/vfx/particles/pro-effects-sci-fi-shooter-fx-176115)** (~$30)
   - Sci-fi themed projectiles, explosions, impacts
   - Ready-to-use particle prefabs
   - Optimized for performance

3. **[Unique Projectiles Vol. 1](https://assetstore.unity.com/packages/vfx/particles/unique-projectiles-vol-1-124214)** (~$15)
   - Bullets, lasers, energy projectiles
   - Hit and explosion effects included

#### Audio

4. **[Pro Sound Collection](https://assetstore.unity.com/packages/audio/sound-fx/pro-sound-collection-50235)** (~$50)
   - Sci-fi weapons, impacts, UI sounds
   - High quality, royalty-free

5. **[Sci-Fi Music Pack](https://assetstore.unity.com/)** (~$20-40)
   - Cyberpunk/electronic background tracks
   - Multiple intensity levels for adaptive music

#### UI

6. **[Sci-Fi UI Pack](https://assetstore.unity.com/)** (~$25)
   - Cyberpunk/tech themed UI elements
   - Health bars, icons, frames

### Already Installed
- **Feel (MMFeedbacks)** - Game juice framework
- **CodeMonkey Toolkit** - Utility scripts
- **URP** - Universal Render Pipeline

### Optional Enhancements

7. **[VFX Graph]** (Free via Package Manager)
   - GPU-accelerated particles
   - Millions of particles at 60fps
   - Great for starfield, swarms

8. **[DOTween Pro](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)** (~$15)
   - Smooth UI animations
   - Integrates with Feel

---

## 5. Feel Integration Strategy

### Current Feel Hooks (19 total)

```
GameManager:      _gameStartFeedback, _gameOverFeedback, _levelCompleteFeedback, _victoryFeedback
PlayerController: _dashFeedback, _dashReadyFeedback
PlayerHealth:     _damageFeedback, _healFeedback, _shieldHitFeedback, _shieldGainFeedback,
                  _invulnerabilityFeedback, _deathFeedback
WeaponSystem:     _fireFeedback, _overheatFeedback, _overheatClearedFeedback
EnemyBase:        _spawnFeedback, _hitFeedback, _deathFeedback
CameraController: _impactShakeFeedback
```

### Recommended Feel Feedbacks by Hook

#### Player Feedbacks
```
_dashFeedback:
  - MMF_CameraShake (0.1s, amplitude 0.3)
  - MMF_Scale (punch, overshoot)
  - MMF_ParticlesInstantiation (dash trail)
  - MMF_AudioSource (whoosh sound)

_damageFeedback:
  - MMF_CameraShake (0.2s, amplitude 0.5)
  - MMF_ImageFill (health bar flash red)
  - MMF_ChromaticAberration (0.3s spike)
  - MMF_Freeze (0.05s hitstop)
  - MMF_AudioSource (hit sound)

_deathFeedback:
  - MMF_CameraShake (0.5s, amplitude 1.0)
  - MMF_Explosion (ship fragments)
  - MMF_SlowMotion (0.5s at 0.2 speed)
  - MMF_AudioSource (explosion)
```

#### Enemy Feedbacks
```
_hitFeedback:
  - MMF_MaterialSetProperty (flash white)
  - MMF_Scale (squash 0.9)
  - MMF_ParticlesInstantiation (hit sparks)

_deathFeedback:
  - MMF_CameraShake (scaled by enemy type)
  - MMF_ParticlesInstantiation (explosion)
  - MMF_AudioSource (death sound)
  - MMF_FloatingText (XP value)
```

#### Combat Feedbacks
```
_fireFeedback:
  - MMF_CameraShake (0.02s, amplitude 0.05)
  - MMF_Scale (gun recoil)
  - MMF_Muzzleflash
  - MMF_AudioSource (pew pew)

_overheatFeedback:
  - MMF_CameraShake (0.3s)
  - MMF_Vignette (red warning)
  - MMF_AudioSource (overheat alarm)
```

### Feel Best Practices

1. **Use MMF_Player prefabs** - Create feedback prefabs for reuse
2. **Pool particles** - Enable caching in ParticlesInstantiation
3. **Layer effects** - Combine multiple subtle feedbacks > one big one
4. **Timing matters** - Use delays to stagger effects
5. **Test on target hardware** - Post-processing is expensive on mobile

---

## 6. Implementation Phases

### Phase 1: Core Content (Est. 2-3 weeks)
**Goal: Playable game with all 8 enemies**

- [ ] Create 7 remaining enemy scripts (inherit from EnemyBase)
- [ ] Create enemy prefabs with colliders/renderers
- [ ] Configure EnemySpawner pools for all types
- [ ] Create ScriptableObjects for enemy data
- [ ] Implement enemy-specific AI behaviors
- [ ] Test all enemy combat interactions

### Phase 2: Pickups & Progression (Est. 1-2 weeks)
**Goal: Full pickup system and level scaling**

- [ ] Create PickupBase abstract class
- [ ] Implement 5 pickup types
- [ ] Create pickup prefabs with magnet physics
- [ ] Implement PickupManager spawning
- [ ] Enhance LevelManager with difficulty scaling
- [ ] Create level configuration data
- [ ] Implement surprise level themes

### Phase 3: UI System (Est. 2-3 weeks)
**Goal: Complete UI/UX**

- [ ] Create UIManager singleton
- [ ] Implement HUD (health, score, objectives, etc.)
- [ ] Create StartScreen with mode selection
- [ ] Create PauseScreen overlay
- [ ] Create GameOverScreen with stats
- [ ] Create VictoryScreen
- [ ] Implement screen transitions
- [ ] Add keyboard/gamepad navigation

### Phase 4: Audio & VFX (Est. 1-2 weeks)
**Goal: Polish and juice**

- [ ] Create AudioManager
- [ ] Import/create sound effects
- [ ] Add background music system
- [ ] Configure all Feel feedbacks
- [ ] Create particle effects (explosions, trails)
- [ ] Add starfield background
- [ ] Add arena boundary visuals

### Phase 5: Polish & Testing (Est. 1-2 weeks)
**Goal: Production quality**

- [ ] Balance all enemy values
- [ ] Tune difficulty curve
- [ ] Optimize performance (profiling)
- [ ] Fix bugs from playtesting
- [ ] Add options menu
- [ ] Implement leaderboard (optional)

---

## 7. Enemy Implementation Guide

### Creating a New Enemy

1. **Create Script** (inherit from EnemyBase)
```csharp
namespace NeuralBreak.Entities
{
    public class ScanDrone : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.ScanDrone;

        [Header("ScanDrone Settings")]
        [SerializeField] private float _fireRate = 2f;
        [SerializeField] private float _detectionRange = 15f;

        protected override void UpdateAI()
        {
            // Implement patrol + attack behavior
        }
    }
}
```

2. **Create Prefab**
   - Add SpriteRenderer (or 3D mesh)
   - Add Collider2D (trigger)
   - Add script component
   - Configure serialized fields
   - Add Feel feedback references

3. **Add to Spawner**
   - Create pool in EnemySpawner
   - Add prefab reference
   - Configure spawn rate

### Enemy AI Patterns (from TypeScript)

| Enemy | Movement | Attack |
|-------|----------|--------|
| DataMite | Direct pursuit + oscillation | Collision only |
| ScanDrone | Patrol until player in range | Shoot every 2s |
| Fizzer | Erratic high-speed zigzag | Burst fire (2 shots) |
| UFO | Bezier curves, dive attacks | Shoot during dive |
| ChaosWorm | Serpentine undulation | Collision + death spray |
| VoidSphere | Slow advance | Burst fire (4 shots, 3s) |
| CrystalShard | Orbit + pursuit | Fire from rotating shards |
| Boss | Track player, phase changes | Normal > Fast > Ring attack |

---

## 8. Configuration & Balance System

### Recommended: ScriptableObjects

Create data assets for all balance values instead of hardcoding.

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "NeuralBreak/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public EnemyType type;
    public int baseHealth;
    public float baseSpeed;
    public int baseDamage;
    public int xpValue;
    public int scoreValue;
    public float collisionRadius;

    // Scaling per level
    public float healthScalePerLevel = 1.025f;  // 2.5% per level
    public float speedScalePerLevel = 1.012f;   // 1.2% per level
    public float damageScalePerLevel = 1.02f;   // 2.0% per level
}
```

### Balance Values from TypeScript

#### Player
```
BASE_HEALTH: 130
BASE_SPEED: 7.0
DASH_SPEED: 32.0
DASH_DURATION: 0.45s
DASH_COOLDOWN: 2.5s
MAX_SHIELDS: 3
INVULNERABLE_DURATION: 7.0s
```

#### Weapon
```
BASE_DAMAGE: 12
BASE_FIRE_RATE: 0.12s (8.3/sec)
BASE_PROJECTILE_SPEED: 22
BASE_RANGE: 38
HEAT_PER_SHOT: 0.8
HEAT_MAX: 100
HEAT_COOLDOWN_RATE: 85/sec
OVERHEAT_COOLDOWN: 0.8s
POWER_UP_DAMAGE_MULTIPLIER: 0.6 (60% per level)
```

#### Scoring
```
BASE_KILL_POINTS: 100
MAX_MULTIPLIER: 10x
COMBO_DECAY_TIME: 1.5s
LEVEL_COMPLETE_BONUS: 1000
PERFECT_LEVEL_BONUS: 500
```

#### World
```
ARENA_RADIUS: 25 (Arcade circular)
CORRIDOR_WIDTH: 50 (Rogue mode)
SPAWN_DISTANCE_MIN: 8
SPAWN_DISTANCE_MAX: 20
```

---

## 9. Performance Optimization

### Object Pooling (Already Implemented)
- Projectiles: 50 pool size
- DataMites: 100 pool size
- Other enemies: 20-50 based on spawn rate

### Recommended Pool Sizes
```
DataMite:     100  (swarm enemy)
ScanDrone:    30
Fizzer:       50   (fast spawn in frenzy levels)
UFO:          20
ChaosWorm:    10
VoidSphere:   5    (rare, massive)
CrystalShard: 15
Boss:         3
Projectiles:  100  (player + enemies)
Pickups:      30
Particles:    200
```

### Performance Targets
- **60 FPS** on mid-range hardware
- **30 FPS minimum** on low-end
- **200+ active enemies** without frame drops
- **<16ms frame time** average

### Optimization Techniques
1. **Pool everything** - No Instantiate/Destroy during gameplay
2. **Spatial partitioning** - Grid-based collision (4-unit cells)
3. **LOD for particles** - Reduce density at distance
4. **Shader optimization** - Use All In 1 Sprite Shader batching
5. **Profile regularly** - Use Unity Profiler, check GC allocations

---

## 10. File Reference Map

### TypeScript Source Files → Unity Scripts

| TypeScript File | Unity Equivalent | Status |
|-----------------|------------------|--------|
| `src/core/Game.ts` | `GameManager.cs` | COMPLETE |
| `src/core/GameState.ts` | `GameState.cs` | COMPLETE |
| `src/core/InputManager.ts` | `InputManager.cs` | COMPLETE |
| `src/core/EnemyManager.ts` | `EnemySpawner.cs` | PARTIAL |
| `src/core/LevelManager.ts` | `LevelManager.cs` | PARTIAL |
| `src/entities/Player.ts` | `PlayerController.cs` + `PlayerHealth.cs` | COMPLETE |
| `src/entities/Enemy.ts` | `EnemyBase.cs` | COMPLETE |
| `src/entities/DataMite.ts` | `DataMite.cs` | COMPLETE |
| `src/entities/ScanDrone.ts` | NEEDS CREATION | - |
| `src/entities/Fizzer.ts` | NEEDS CREATION | - |
| `src/entities/UFO.ts` | NEEDS CREATION | - |
| `src/entities/ChaosWorm.ts` | NEEDS CREATION | - |
| `src/entities/VoidSphere.ts` | NEEDS CREATION | - |
| `src/entities/CrystalShardSwarm.ts` | NEEDS CREATION | - |
| `src/entities/Boss.ts` | NEEDS CREATION | - |
| `src/weapons/WeaponSystem.ts` | `WeaponSystem.cs` | COMPLETE |
| `src/weapons/Projectile.ts` | `Projectile.cs` | COMPLETE |
| `src/graphics/SceneManager.ts` | `CameraController.cs` | COMPLETE |
| `src/ui/UIManager.ts` | NEEDS CREATION | - |
| `src/ui/StartScreen.ts` | NEEDS CREATION | - |
| `src/ui/GameOverScreen.ts` | NEEDS CREATION | - |
| `src/audio/AudioManager.ts` | NEEDS CREATION | - |
| `src/config/balance.config.ts` | ScriptableObjects | NEEDS CREATION |

---

## Quick Start Checklist

### Immediate Next Steps

1. [ ] **Purchase recommended assets** (All In 1 Sprite Shader, Sci-fi FX)
2. [ ] **Create ScanDrone enemy** (simplest ranged enemy)
3. [ ] **Create enemy prefabs** with placeholder sprites
4. [ ] **Configure Feel feedbacks** for existing hooks
5. [ ] **Create basic HUD** (score, health display)
6. [ ] **Test full combat loop** with 2+ enemy types

### Getting Started Command
```bash
# In Unity, start with:
1. Open SampleScene
2. Create Player prefab from scene objects
3. Create DataMite prefab
4. Configure EnemySpawner references
5. Press Play - verify spawning works
6. Iterate!
```

---

## Notes

- **TypeScript Reference**: Keep `D:\Projects\Neural-Break-Unity` until port is complete
- **Feel Docs**: https://feel-docs.moremountains.com/
- **EventBus Pattern**: All systems communicate via events, not direct references
- **Test Often**: Each phase should result in a playable build

---

*This document should be updated as implementation progresses.*
