# 🎮 NEURAL BREAK

<div align="center">

![Neural Break Logo](https://img.shields.io/badge/NEURAL-BREAK-00ffff?style=for-the-badge&labelColor=0a0a1a)

**A Cyberpunk Survival Arena Shooter**

[![Unity](https://img.shields.io/badge/Unity-6000.0.31f1-000000?style=flat-square&logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-10.0-239120?style=flat-square&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-Proprietary-red?style=flat-square)](LICENSE)

*Survive 99 levels of relentless cyberpunk chaos*

</div>

---

## 📖 Table of Contents

- [About](#-about)
- [Features](#-features)
- [Getting Started](#-getting-started)
- [Controls](#-controls)
- [Game Modes](#-game-modes)
- [Enemies](#-enemies)
- [Pickups & Power-Ups](#-pickups--power-ups)
- [Scoring System](#-scoring-system)
- [Project Structure](#-project-structure)
- [Configuration](#-configuration)
- [Development History](#-development-history)
- [Credits](#-credits)
- [License](#-license)

---

## 🎯 About

**Neural Break** is a fast-paced twin-stick survival arena shooter set in a neon-drenched cyberpunk world. Battle through 99 increasingly difficult levels, facing 8 unique enemy types, collecting power-ups, and chasing high scores.

Originally developed as a TypeScript/Three.js web game, Neural Break has been fully ported to Unity for enhanced performance, cross-platform support, and expanded features.

### Key Highlights

- 🕹️ **Twin-Stick Shooter** - Classic arcade gameplay with modern polish
- 🌌 **Cyberpunk Aesthetic** - Neon visuals, procedural starfields, and glowing effects
- 📈 **99 Levels** - Progressive difficulty with new enemy unlocks
- 🎮 **Multiple Game Modes** - Arcade, Rogue, and Test modes
- 🏆 **Combo System** - Chain kills for massive score multipliers
- ⚡ **Juicy Feedback** - Screen shake, hit stop, and satisfying VFX

---

## ✨ Features

### Core Gameplay
- **Smooth Movement** - Acceleration-based physics with dash ability
- **Heat-Based Weapon** - Manage your weapon heat to avoid overheating
- **Shield System** - Collect shields for extra protection
- **Dynamic Camera** - Auto-zoom based on enemy count and action intensity

### Visual Effects
- **Procedural Starfield** - Animated parallax background
- **Enemy Death VFX** - Unique explosions for each enemy type
- **Screen Effects** - Shake, flash, vignette, and post-processing
- **Damage Numbers** - Floating combat feedback

### Audio
- **Procedural SFX** - Runtime-generated sound effects
- **Adaptive Music** - Dynamic soundtrack that responds to gameplay
- **Spatial Audio** - Positional sound for immersion

### Technical
- **Object Pooling** - Zero garbage collection during gameplay
- **Event-Driven Architecture** - Decoupled systems via EventBus
- **Config-Driven Balance** - All values editable via ScriptableObjects
- **Feel Integration** - MMFeedbacks for game juice

---

## 🚀 Getting Started

### Prerequisites

- **Unity 6000.0.31f1** or later (Unity 6 LTS)
- **Git** for version control

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/z13labs/neural-break-unity.git
   cd neural-break-unity
   ```

2. **Open in Unity Hub**
   - Add the project folder to Unity Hub
   - Open with Unity 6000.0.31f1 or compatible version

3. **Open the main scene**
   - Navigate to `Assets/Scenes/main-neural-break.unity`
   - Press **Play** to start the game

### Quick Start

1. Press **Play** in Unity Editor
2. Select **ARCADE** mode from the start screen
3. Use **WASD** to move, **Mouse** to aim
4. Hold **Left Click** to fire
5. Survive as long as you can!

---

## 🎮 Controls

### Keyboard + Mouse

| Action | Key |
|--------|-----|
| Move | WASD |
| Aim | Mouse |
| Fire | Left Mouse Button (hold) |
| Dash | Space |
| Thrust | Left Shift (hold) |
| Pause | Escape |

### Gamepad (Xbox/PlayStation)

| Action | Button |
|--------|--------|
| Move | Left Stick |
| Aim/Fire | Right Stick (auto-fires when moved) |
| Dash | A / X Button |
| Smart Bomb | B / Circle Button |
| Thrust | Left Trigger |
| Pause | Start / Options |

**Note**: Twin-stick gamepad controls use **right stick movement** for both aiming and firing (auto-fire when stick magnitude > deadzone).

---

## 🎲 Game Modes

### 🕹️ Arcade Mode (Levels 1-99)
The main campaign experience. Progress through 99 levels of increasing difficulty with new enemy types unlocking as you advance.

**Enemy Unlock Schedule:**
- Level 1: DataMites, ScanDrones
- Level 2+: ChaosWorms
- Level 3+: VoidSpheres, CrystalShards
- Level 4+: UFOs
- Level 5+: Bosses
- Level 6+: Fizzers

### 🎰 Rogue Mode
Procedurally generated layers with themed enemy compositions. Each layer presents unique challenges and rewards.

### 🧪 Test Mode
Development and debugging mode with all enemy types spawning at reduced rates. Perfect for testing and practice.

---

## 👾 Enemies

Neural Break features **8 unique enemy types**, each with distinct behaviors and challenges:

| Enemy | Health | Speed | Contact Damage | Score | XP | Special Ability |
|-------|--------|-------|----------------|-------|-----|-----------------|
| **DataMite** | 1 | 1.5 | 5 | 100 | 1 | Pursuit AI with oscillation |
| **ScanDrone** | 30 | 1.2 | 15 | 250 | 6 | Ranged fire (2s cooldown) |
| **Fizzer** | 2 | 8.0 | 6 | 200 | 15 | Erratic zigzag, burst fire |
| **UFO** | 30 | 2.8 | 12 | 1500 | 25 | Hit-and-run dive attacks |
| **ChaosWorm** | 100 | 1.5 | 15 | 500 | 35 | Multi-segment, death bullet spray |
| **VoidSphere** | 650 | 0.5 | 40 | 1000 | 50 | Massive tank, 4-shot burst |
| **CrystalShard** | 250 | 1.8 | 25 | 750 | 45 | 6 orbiting crystals |
| **Boss** | 180 | 0.3 | 25 | 5000 | 100 | 3-phase AI, ring attacks |

### Enemy Behaviors

- **DataMite** - Swarms toward player with slight oscillation
- **ScanDrone** - Patrols until player in range, then fires
- **Fizzer** - High-speed erratic movement with burst fire
- **UFO** - Bezier curve movement, dive bomb attacks
- **ChaosWorm** - Serpentine movement, explodes into bullets on death
- **VoidSphere** - Slow but devastating, gravity pull effect
- **CrystalShard** - Orbiting crystals fire at player
- **Boss** - Three phases: Normal → Fast Fire → Ring Attack

---

## 💎 Pickups & Upgrades

### Floating Arena Pickups (spawn during gameplay)

Only 4 pickup types float in the arena. Spawn rates are config-driven via GameBalanceConfig for easy balance tweaking.

| Pickup | Effect | Notes |
|--------|--------|-------|
| **Med Pack** | Heal +20 HP | Only spawns when health < 80% |
| **Shield** | +1 Shield (absorbs one hit) | Max 3 shields |
| **Smart Bomb** | +1 Smart Bomb charge | Max 3 bombs |
| **Invulnerable** | Temporary god mode (7s) | VERY RARE |

### End-of-Level Upgrade Selection

After completing each level, players choose from 3 upgrade cards (weighted by rarity). These are permanent for the run:

- Weapon upgrades (damage, fire rate, projectile size, multi-shot)
- Special abilities (piercing, homing, chain lightning, ricochet, explosions)
- Utility (extra shields, extra smart bombs)

---

## 🏆 Scoring System

### Base Scoring
- **Base Kill Points**: 100 per enemy (modified by enemy type)
- **Level Complete Bonus**: 1,000 points
- **Perfect Level Bonus**: 500 points (no damage taken)
- **Boss Kill Multiplier**: 2.0x

### Combo System
- **Combo Timer**: 3 seconds to maintain combo
- **Kill Chain Window**: 1.5 seconds between kills
- **Max Multiplier**: 10x
- **Multiplier Decay**: 2 seconds

### Score Formula
```
Final Score = (Base Points × Enemy Multiplier) × Combo Multiplier
```

---

## 📁 Project Structure

```
Assets/
├── Scenes/
│   └── main-neural-break.unity  # Main game scene
├── _Project/
│   ├── Packages/
│   │   └── Z13.Core/      # Shared reusable package
│   │       └── Runtime/   # EventBus, ObjectPool, LogHelper, SaveSystemBase
│   ├── Scripts/           # 170+ C# scripts
│   │   ├── Audio/         # AudioManager, MusicManager, ProceduralSFX
│   │   ├── Combat/        # WeaponSystem, Projectile, Upgrades, SmartBomb
│   │   ├── Config/        # ConfigProvider, GameBalanceConfig
│   │   ├── Core/          # GameSetup, GameStateManager, GameManager
│   │   ├── Entities/      # Player, Enemies, Pickups
│   │   ├── Graphics/      # VFXManager, FeedbackManager, ArenaManager
│   │   ├── Input/         # InputManager, GamepadRumble
│   │   ├── UI/            # UIManager, HUD, Screens, Builders
│   │   └── Utils/         # Helpers, Extensions
│   ├── Prefabs/           # Game object prefabs
│   ├── Resources/         # Runtime-loadable assets
│   │   ├── Config/        # GameBalanceConfig.asset
│   │   └── Upgrades/      # Upgrade definitions
│   └── Art/               # Sprites and textures
├── Feel/                  # MMFeedbacks asset (game juice)
└── Documents/             # Technical documentation & implementation notes
```

### Namespace Organization

```csharp
Z13.Core                // EventBus, ObjectPool, LogHelper (shared package)
NeuralBreak.Core        // GameSetup, GameStateManager, GameManager
NeuralBreak.Entities    // Player, Enemies, Pickups
NeuralBreak.Combat      // Weapons, Projectiles, Upgrades, SmartBomb
NeuralBreak.Input       // InputManager, GamepadRumble
NeuralBreak.Graphics    // VFXManager, FeedbackManager, ArenaManager
NeuralBreak.UI          // UIManager, HUD, Screens, Builders
NeuralBreak.Audio       // AudioManager, MusicManager, ProceduralSFX
NeuralBreak.Config      // ConfigProvider, GameBalanceConfig
NeuralBreak.Utils       // Utilities, Extensions
```

---

## 🔧 Systems Inventory (Source of Truth)

### Active Systems

| Category | Systems | Key Files |
|----------|---------|-----------|
| **Z13.Core** | EventBus, ObjectPool\<T\>, LogHelper, SaveSystemBase\<T\>, IBootable | `Packages/Z13.Core/Runtime/` |
| **Core** | GameStateManager, GameManager, GameSetup, LevelManager, LevelGenerator, PlayerLevelSystem, AchievementSystem, HighScoreManager, SaveSystem, PostProcessManager | `Scripts/Core/` |
| **Input** | InputManager (twin-stick + KB/M auto-fire), GamepadRumble | `Scripts/Input/` |
| **Audio** | AudioManager, MusicManager, ProceduralSFX, UpgradeSystemAudio | `Scripts/Audio/` |
| **Combat** | WeaponSystem (stat-based 0-100), SmartBombSystem, BeamWeapon, WeaponUpgradeManager, PermanentUpgradeManager, UpgradePoolManager, 5 Projectile Behaviors, ProjectileVisualProfile | `Scripts/Combat/` |
| **Entities** | PlayerController, PlayerHealth, 8 Enemy Types, EnemySpawner (4-file system), 4 Floating Pickups, PickupSpawner | `Scripts/Entities/` |
| **Graphics** | VFXManager, EnemyDeathVFX (pooled), FeedbackManager, CameraController, ArenaManager (7 themes), Starfield (5 files) | `Scripts/Graphics/` |
| **UI** | UIManager, Builder pattern (4 screens), HUD (8 displays), DamageNumberPopup, UpgradeSelectionScreen, GameOverScreen (press-any-key) | `Scripts/UI/` |
| **Config** | ConfigProvider, GameBalanceConfig (master ScriptableObject) | `Scripts/Config/` |

### Scaffolded / Not Yet Active

| System | Status |
|--------|--------|
| Boot Scene (BootManager) | Code ready, scene NOT created. Singletons self-init via Awake fallback. |
| ShipCustomization | 3 files, partially implemented |
| AccessibilityManager | Implements IBootable, needs completion |
| Rogue Mode | GameMode.Rogue supported, card pool incomplete |

---

## ⚙️ Configuration

All game balance values are stored in a ScriptableObject for easy tweaking:

**Location**: `Assets/Resources/Config/GameBalanceConfig.asset`

### Editable Values Include:
- Player stats (health, speed, dash)
- Weapon stats (damage, fire rate, heat)
- Enemy stats (health, speed, damage, spawn rates)
- Pickup effects and spawn rates
- Combo/scoring parameters
- Camera zoom settings
- Level progression

Simply select the asset in Unity and modify values in the Inspector. Changes take effect on next play.

---

## 📜 Development History

### Timeline

| Date | Milestone |
|------|-----------|
| **2025** | Original TypeScript/Three.js development |
| **Jan 2026** | Unity port initiated |
| **Jan 22, 2026** | Core systems complete (85%) |
| **Jan 23, 2026** | Major refactoring - God classes split, singletons reduced |
| **Jan 25-26, 2026** | TypeScript balance config ported, bug fixes |
| **Feb 9, 2026** | Twin-stick auto-fire control fix, smart bomb system |
| **Feb 10, 2026** | Documentation overhaul, Z13.Core package formalized |

### TypeScript to Unity Port

The original Neural Break was built with:
- **TypeScript** for game logic
- **Three.js** for 3D rendering (orthographic 2D style)
- **Web Audio API** for sound

The Unity port maintains feature parity while adding:
- Native performance improvements
- Cross-platform build support
- Enhanced visual effects via URP
- MMFeedbacks integration for game juice
- Improved input system with gamepad support

### Architecture Improvements

During the port, significant architectural improvements were made:
- **Z13.Core Package**: Extracted reusable systems (EventBus, ObjectPool, LogHelper) for use across all Z13 Labs projects
- **Event-Driven Design**: All systems communicate via type-safe EventBus (no direct coupling)
- **Object Pooling**: Zero-allocation gameplay via ObjectPool<T>
- **Config-Driven Balance**: All values in GameBalanceConfig ScriptableObject
- **Singleton Architecture**: Clear distinction between app-lifetime (Boot scene) and scene-specific managers
- **Modular Code**: Target 300 LOC per file (guideline, not strict)

---

## 👏 Credits

### Development

**Created by**: Johnny @ Z13 Labs  
**Contact**: johnny@z13labs.com  
**Website**: [z13labs.com](https://z13labs.com)

### Asset Packs Used

| Asset | Purpose | License |
|-------|---------|---------|
| **[Feel (MMFeedbacks)](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370)** | Game juice, screen shake, feedback effects | Asset Store License |
| **[Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)** | Modern input handling | Unity Package |
| **[Universal Render Pipeline](https://unity.com/srp/universal-render-pipeline)** | Rendering | Unity Package |
| **[TextMeshPro](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html)** | UI text rendering | Unity Package |

### Special Thanks

- The Three.js community for the original web version inspiration
- More Mountains for the incredible Feel/MMFeedbacks asset
- Unity Technologies for Unity 6

---

## 📄 License

**Proprietary** - All rights reserved.

This project and its source code are the property of Z13 Labs. Unauthorized copying, modification, distribution, or use of this software is strictly prohibited.

For licensing inquiries, contact: johnny@z13labs.com

---

## 📚 Developer Documentation

For contributors and AI agents working on this codebase:

- **CLAUDE.md** - Core development rules, architecture patterns, critical guidelines
- **CLAUDE_REFERENCE.md** - Extended examples, detailed patterns, checklists
- **Z13.Core/README.md** - Shared package usage and API reference
- **Documents/** - Implementation notes, refactoring summaries (archived in Documents/Archive/)

## 🐛 Known Issues

- Boot scene architecture designed but not yet implemented (single-scene initialization via GameSetup)
- Camera dynamic zoom may need tuning for different screen sizes

## 🔮 Future Plans

- [ ] Boot scene implementation (two-scene architecture)
- [ ] Rogue mode card upgrade system completion
- [ ] Online leaderboards
- [ ] Additional enemy types
- [ ] Boss rush mode
- [ ] Steam/Console release

---

<div align="center">

**Made with ❤️ and ☕ by Z13 Labs**

*"In the neon-lit depths of cyberspace, only the fastest survive."*

</div>
