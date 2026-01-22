# NEURAL BREAK - Unity Porting Master Plan

> **Source Project:** `D:\Projects\Neural-Break-Unity` (TypeScript/Three.js - DELETE AFTER PORT)
> **Target Project:** `D:\Projects\Unity\neural-break-unity` (Unity 6000.3.0f1)
> **Document Version:** 2.0
> **Last Updated:** 2026-01-22

---

## Executive Summary

Neural Break is a cyberpunk survival arena shooter with 99 levels, 8 enemy types, and multiple game modes. The original TypeScript codebase (~30,000 lines) has been ported to Unity C#.

**Current Unity Progress: ~85% Complete**
- Core architecture: COMPLETE
- Player systems: COMPLETE
- Combat systems: COMPLETE
- Input systems: COMPLETE
- Enemy system: 8/8 enemies implemented
- Pickup system: COMPLETE (9 pickup types)
- Graphics/VFX: COMPLETE
- Audio: COMPLETE (AudioManager, MusicManager, ProceduralSFX)
- UI: PARTIAL (HUD complete, menus in progress)

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
├── Scripts/                    # 105 C# scripts (~32,000 lines)
│   ├── Audio/                  # AudioManager, MusicManager, ProceduralSFX
│   ├── Combat/                 # WeaponSystem, Projectile, WeaponUpgradeManager
│   ├── Config/                 # ConfigProvider, GameBalanceConfig
│   ├── Core/                   # GameManager, EventBus, ObjectPool, LevelManager
│   ├── Data/                   # EnemyData
│   ├── Debug/                  # DebugGameTest (development only)
│   ├── Editor/                 # ConfigCreator, LevelValidator, SceneSetupHelper
│   ├── Entities/
│   │   ├── Enemies/            # EnemyBase + 8 enemy types + visuals
│   │   ├── Pickups/            # PickupBase + 9 pickup types + visuals
│   │   └── Player/             # PlayerController, PlayerHealth
│   ├── Graphics/               # Camera, VFX, particle systems, starfield
│   ├── Input/                  # InputManager, GamepadRumble
│   ├── UI/                     # HUD, menus, screens (23 scripts)
│   └── Utils/                  # RuntimeSpriteGenerator
├── Prefabs/                    # Game object prefabs
├── ScriptableObjects/          # Configuration data
├── Art/                        # Sprites and textures
├── Audio/                      # Music and SFX
├── Materials/                  # Shaders and materials
├── Input/                      # Input Action assets
├── Resources/                  # Runtime-loadable assets
└── VFX/                        # Particle systems
```

### Namespace Organization
```csharp
NeuralBreak.Core        // GameManager, EventBus, GameState, LevelManager
NeuralBreak.Entities    // Player, enemies, pickups
NeuralBreak.Combat      // Weapons, projectiles, upgrades
NeuralBreak.Input       // Input handling, gamepad rumble
NeuralBreak.Graphics    // Camera, VFX, particles
NeuralBreak.UI          // HUD, menus, screens
NeuralBreak.Audio       // Sound management
NeuralBreak.Config      // Configuration system
NeuralBreak.Utils       // Utility classes
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
| `LevelManager.cs` | COMPLETE | 99-level progression, difficulty scaling |
| `LevelGenerator.cs` | COMPLETE | Procedural level configuration |
| `GameSetup.cs` | COMPLETE | Runtime scene wiring and auto-setup |
| `ConfigProvider.cs` | COMPLETE | Centralized configuration access |

### Player Systems (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `PlayerController.cs` | COMPLETE | Movement (accel/decel), dash with invuln, boundary enforcement, thrust |
| `PlayerHealth.cs` | COMPLETE | Health, shields (max 3), invulnerability, damage feedback |
| `ShipCustomization.cs` | COMPLETE | Ship visual customization |

### Combat Systems (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `WeaponSystem.cs` | COMPLETE | Fire rate, heat system, power levels 0-10, pooling |
| `Projectile.cs` | COMPLETE | Movement, lifetime, collision, piercing, homing |
| `EnemyProjectile.cs` | COMPLETE | Enemy projectile system |
| `EnemyProjectilePool.cs` | COMPLETE | Enemy projectile pooling |
| `WeaponUpgradeManager.cs` | COMPLETE | Piercing, homing, multishot upgrades |

### Input System (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `InputManager.cs` | COMPLETE | Twin-stick, keyboard + gamepad, events, deadzone |
| `GamepadRumble.cs` | COMPLETE | Haptic feedback |

### Graphics (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `CameraController.cs` | COMPLETE | Follow, dynamic zoom, screen shake, event reactions |
| `StarfieldController.cs` | COMPLETE | Procedural animated background |
| `ArenaManager.cs` | COMPLETE | Arena boundary visuals |
| `EnvironmentParticles.cs` | COMPLETE | Ambient particle effects |
| `EnemyDeathVFX.cs` | COMPLETE | Enemy-specific death particles |
| `HitFlashEffect.cs` | COMPLETE | Damage flash effect |
| `ParticleEffectFactory.cs` | COMPLETE | Particle system creation |
| `SpriteGenerator.cs` | COMPLETE | Procedural sprite generation |
| `VFXManager.cs` | COMPLETE | VFX management |

### Enemy System (100% Complete - 8/8 enemies)

| Script | Status | Features |
|--------|--------|----------|
| `EnemyBase.cs` | COMPLETE | Abstract base, state machine, lifecycle, Feel hooks |
| `EnemySpawner.cs` | COMPLETE | All enemy type spawning, pooling, waves |
| `DataMite.cs` | COMPLETE | Pursuit AI, oscillation |
| `ScanDrone.cs` | COMPLETE | Ranged fire, patrol behavior |
| `Fizzer.cs` | COMPLETE | Erratic movement, burst fire |
| `UFO.cs` | COMPLETE | Hit-and-run, dive attacks |
| `ChaosWorm.cs` | COMPLETE | Multi-segment, death bullet spray |
| `VoidSphere.cs` | COMPLETE | Massive, burst fire |
| `CrystalShard.cs` | COMPLETE | Orbiting crystals |
| `Boss.cs` | COMPLETE | 3-phase AI, special attacks |
| `EliteModifier.cs` | COMPLETE | Elite enemy variants |

### Pickup System (100% Complete - 9 types)

| Script | Status | Features |
|--------|--------|----------|
| `PickupBase.cs` | COMPLETE | Abstract base, magnet physics |
| `PickupSpawner.cs` | COMPLETE | Spawn management, pools |
| `PowerUp.cs` | COMPLETE | Weapon level increase |
| `SpeedUp.cs` | COMPLETE | Movement speed boost |
| `MedPack.cs` | COMPLETE | Health restore |
| `Shield.cs` | COMPLETE | Shield pickup |
| `Invulnerable.cs` | COMPLETE | Temporary god mode |
| `XPOrb.cs` | COMPLETE | Experience points |
| `MultishotPickup.cs` | COMPLETE | Multishot upgrade |
| `PiercingPickup.cs` | COMPLETE | Piercing shots |
| `HomingPickup.cs` | COMPLETE | Homing missiles |

### Audio System (100% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `AudioManager.cs` | COMPLETE | Sound management, pooling |
| `MusicManager.cs` | COMPLETE | Background music, adaptive |
| `ProceduralSFX.cs` | COMPLETE | Runtime sound generation |

### UI System (80% Complete)

| Script | Status | Features |
|--------|--------|----------|
| `HUDManager.cs` | COMPLETE | Health, score, objectives |
| `ScoreDisplay.cs` | COMPLETE | Score with multiplier |
| `HealthDisplay.cs` | COMPLETE | Health/shield bars |
| `WaveAnnouncement.cs` | COMPLETE | Wave start notifications |
| `LevelUpAnnouncement.cs` | COMPLETE | Level up effects |
| `DamageNumberPopup.cs` | COMPLETE | Floating damage numbers |
| `BossHealthBar.cs` | COMPLETE | Boss HP display |
| `XPBarDisplay.cs` | COMPLETE | Experience bar |
| `ActiveUpgradesDisplay.cs` | COMPLETE | Active upgrade icons |
| `ControlsOverlay.cs` | COMPLETE | Control hints |
| `LowHealthVignette.cs` | COMPLETE | Low health warning |
| `NotificationManager.cs` | COMPLETE | In-game notifications |
| `AchievementPopup.cs` | COMPLETE | Achievement notifications |
| `StatisticsScreen.cs` | COMPLETE | Stats display |
| `Minimap.cs` | COMPLETE | Minimap display |
| `UIBuilder.cs` | COMPLETE | UI framework |
| **StartScreen** | NEEDED | Mode selection |
| **PauseScreen** | NEEDED | Pause menu |
| **GameOverScreen** | NEEDED | End game stats |
| **OptionsScreen** | NEEDED | Settings |

### Feel Integration (Ready)
- 19+ MMF_Player references across scripts
- All hooks configured for feedback
- Feel package installed at `Assets/Feel/`

---

## 3. What Needs to Be Built

### Priority 1: UI Screens (4 screens needed)

| Screen | Elements |
|--------|----------|
| **StartScreen** | Title, Arcade/Rogue/Test buttons, Leaderboard, Options |
| **PauseScreen** | Resume, Restart, Menu, Options |
| **GameOverScreen** | Final score, stats, high score comparison, Restart/Menu |
| **OptionsScreen** | Volume sliders, graphics toggles |

### Priority 2: Polish & Testing

- Balance all enemy values
- Tune difficulty curve
- Optimize performance (profiling)
- Fix bugs from playtesting
- Implement leaderboard (optional)

---

## 4. Recommended Asset Packs

### Already Installed
- **Feel (MMFeedbacks)** - Game juice framework
- **CodeMonkey Toolkit** - Utility scripts
- **URP** - Universal Render Pipeline
- **Input System** - New Unity Input System

### Optional Enhancements

1. **[All In 1 Sprite Shader](https://assetstore.unity.com/packages/vfx/shaders/all-in-1-sprite-shader-156513)** (~$35)
   - 42+ combinable effects (glow, distortion, outline, etc.)
   - Perfect for cyberpunk neon aesthetics

2. **[DOTween Pro](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)** (~$15)
   - Smooth UI animations
   - Integrates with Feel

---

## 5. Feel Integration Strategy

### Current Feel Hooks (19+ total)

```
GameManager:      _gameStartFeedback, _gameOverFeedback, _levelCompleteFeedback, _victoryFeedback
PlayerController: _dashFeedback, _dashReadyFeedback
PlayerHealth:     _damageFeedback, _healFeedback, _shieldHitFeedback, _shieldGainFeedback,
                  _invulnerabilityFeedback, _deathFeedback
WeaponSystem:     _fireFeedback, _overheatFeedback, _overheatClearedFeedback
EnemyBase:        _spawnFeedback, _hitFeedback, _deathFeedback
CameraController: _impactShakeFeedback
```

### Feel Best Practices

1. **Use MMF_Player prefabs** - Create feedback prefabs for reuse
2. **Pool particles** - Enable caching in ParticlesInstantiation
3. **Layer effects** - Combine multiple subtle feedbacks > one big one
4. **Timing matters** - Use delays to stagger effects
5. **Test on target hardware** - Post-processing is expensive on mobile

---

## 6. Implementation Phases

### Phase 1: Core Content ✅ COMPLETE
- [x] Create 8 enemy scripts (inherit from EnemyBase)
- [x] Create enemy prefabs with colliders/renderers
- [x] Configure EnemySpawner pools for all types
- [x] Implement enemy-specific AI behaviors
- [x] Test all enemy combat interactions

### Phase 2: Pickups & Progression ✅ COMPLETE
- [x] Create PickupBase abstract class
- [x] Implement 9 pickup types
- [x] Create pickup prefabs with magnet physics
- [x] Implement PickupSpawner
- [x] Enhance LevelManager with difficulty scaling
- [x] Create level configuration data

### Phase 3: UI System (80% Complete)
- [x] Create HUD (health, score, objectives, etc.)
- [x] Create wave and level announcements
- [x] Create damage popups
- [ ] Create StartScreen with mode selection
- [ ] Create PauseScreen overlay
- [ ] Create GameOverScreen with stats
- [ ] Create OptionsScreen

### Phase 4: Audio & VFX ✅ COMPLETE
- [x] Create AudioManager
- [x] Create MusicManager
- [x] Create ProceduralSFX
- [x] Create particle effects (explosions, trails)
- [x] Add starfield background
- [x] Add arena boundary visuals
- [x] Add enemy-specific death VFX

### Phase 5: Polish & Testing (IN PROGRESS)
- [ ] Balance all enemy values
- [ ] Tune difficulty curve
- [ ] Optimize performance (profiling)
- [ ] Fix bugs from playtesting
- [ ] Add options menu
- [ ] Implement leaderboard (optional)

---

## 7. Enemy Implementation Guide

### Enemy Types Summary

| Enemy | Health | Speed | Damage | XP | Special Ability |
|-------|--------|-------|--------|-----|-----------------|
| DataMite | 1 | 1.5 | 5 | 1 | Pursuit AI |
| ScanDrone | 30 | 1.2 | 15 | 6 | Ranged fire (2s cooldown) |
| Fizzer | 2 | 8.0 | 6 | 15 | Burst fire, erratic movement |
| UFO | 30 | 2.8 | 12 | 25 | Hit-and-run, dive attacks |
| ChaosWorm | 100 | 1.5 | 15 | 35 | 12 segments, death bullet spray |
| VoidSphere | 650 | 0.5 | 40 | 50 | Burst fire (4 shots), massive |
| CrystalShard | 250 | 1.8 | 25 | 45 | 6 orbiting crystals |
| Boss | 180 | 0.3 | 25 | 100 | 3-phase AI, expanding ring attack |

### Enemy AI Patterns

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

### ConfigProvider System
All balance values are centralized in `ConfigProvider.cs` using ScriptableObjects.

### Balance Values

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

### Object Pooling (Implemented)
- Projectiles: 100 pool size
- DataMites: 100 pool size
- Other enemies: 20-50 based on spawn rate
- Pickups: 30

### Performance Targets
- **60 FPS** on mid-range hardware
- **30 FPS minimum** on low-end
- **200+ active enemies** without frame drops
- **<16ms frame time** average

### Optimization Techniques
1. **Pool everything** - No Instantiate/Destroy during gameplay
2. **Spatial partitioning** - Grid-based collision (4-unit cells)
3. **LOD for particles** - Reduce density at distance
4. **Shader optimization** - Use batching
5. **Profile regularly** - Use Unity Profiler, check GC allocations

---

## 10. File Reference Map

### TypeScript Source Files → Unity Scripts

| TypeScript File | Unity Equivalent | Status |
|-----------------|------------------|--------|
| `src/core/Game.ts` | `GameManager.cs` | COMPLETE |
| `src/core/GameState.ts` | `GameState.cs` | COMPLETE |
| `src/core/InputManager.ts` | `InputManager.cs` | COMPLETE |
| `src/core/EnemyManager.ts` | `EnemySpawner.cs` | COMPLETE |
| `src/core/LevelManager.ts` | `LevelManager.cs` | COMPLETE |
| `src/entities/Player.ts` | `PlayerController.cs` + `PlayerHealth.cs` | COMPLETE |
| `src/entities/Enemy.ts` | `EnemyBase.cs` | COMPLETE |
| `src/entities/DataMite.ts` | `DataMite.cs` | COMPLETE |
| `src/entities/ScanDrone.ts` | `ScanDrone.cs` | COMPLETE |
| `src/entities/Fizzer.ts` | `Fizzer.cs` | COMPLETE |
| `src/entities/UFO.ts` | `UFO.cs` | COMPLETE |
| `src/entities/ChaosWorm.ts` | `ChaosWorm.cs` | COMPLETE |
| `src/entities/VoidSphere.ts` | `VoidSphere.cs` | COMPLETE |
| `src/entities/CrystalShardSwarm.ts` | `CrystalShard.cs` | COMPLETE |
| `src/entities/Boss.ts` | `Boss.cs` | COMPLETE |
| `src/weapons/WeaponSystem.ts` | `WeaponSystem.cs` | COMPLETE |
| `src/weapons/Projectile.ts` | `Projectile.cs` | COMPLETE |
| `src/graphics/SceneManager.ts` | `CameraController.cs` | COMPLETE |
| `src/ui/UIManager.ts` | `HUDManager.cs` + `UIBuilder.cs` | PARTIAL |
| `src/audio/AudioManager.ts` | `AudioManager.cs` | COMPLETE |
| `src/config/balance.config.ts` | `ConfigProvider.cs` | COMPLETE |

---

## Quick Start Checklist

### Getting Started
```bash
# In Unity:
1. Open SampleScene
2. Press Play - game auto-starts in Arcade mode
3. Controls: WASD/Left Stick = Move, Mouse/Right Stick = Aim
4. Left Click/RT = Fire, Space/A = Dash, Shift = Thrust
```

---

## Codebase Metrics

| Metric | Value |
|--------|-------|
| Total Scripts | 105 |
| Total Lines of Code | ~32,000 |
| Enemy Types | 8/8 |
| Pickup Types | 9/9 |
| UI Scripts | 23 |
| Feel Integration Hooks | 19+ |
| Event Types | 20+ |

---

## Notes

- **TypeScript Reference**: Keep `D:\Projects\Neural-Break-Unity` until port is complete
- **Feel Docs**: https://feel-docs.moremountains.com/
- **EventBus Pattern**: All systems communicate via events, not direct references
- **Test Often**: Each phase should result in a playable build

---

*Last updated: 2026-01-22*
