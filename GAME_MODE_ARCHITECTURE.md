# Game Mode Architecture - Clear Separation

## Mode Isolation System

Each game mode is **completely isolated** with its own configuration and level number:

### 1. ARCADE MODE (Levels 1-99)
- **Level Numbers**: 1 to 99
- **Entry Point**: `LevelManager.StartNewGame()` → sets level to 1
- **Config**: `LevelGenerator.GenerateDynamicLevelConfig(level)` for levels 1-99
- **Purpose**: Progressive campaign with 99 levels
- **Enemy Progression**: Unlocks new enemies as you progress
  - Level 1: DataMites + ScanDrones only
  - Level 2+: ChaosWorms
  - Level 3+: VoidSpheres + CrystalShards
  - Level 4+: UFOs
  - Level 5+: Bosses
  - Level 6+: Fizzers

### 2. ROGUE MODE (Level 998)
- **Level Number**: 998 (special marker)
- **Entry Point**: `LevelManager.StartRogueMode()` → sets level to 998
- **Config**: `LevelGenerator.GetRogueLevelConfig(layer)` for level 998
- **Purpose**: Procedurally generated layers with themed enemy compositions
- **Progression**: Advances through layers, not levels

### 3. TEST MODE (Level 999)
- **Level Number**: 999 (special marker)
- **Entry Point**: `LevelManager.StartTestMode()` → sets level to 999
- **Config**: `LevelGenerator.GetTestLevelConfig()` for level 999
- **Purpose**: Testing/debugging - spawn all enemy types with infinite objectives
- **Spawn Rates**: Slower than normal to prevent overcrowding
  - DataMite: 3.0s
  - ScanDrone: 12.0s
  - ChaosWorm: 20.0s
  - VoidSphere: 25.0s
  - CrystalShard: 18.0s
  - Fizzer: 22.0s
  - UFO: 28.0s
  - Boss: 45.0s

## Flow Chart

```
User Clicks Button
    ↓
StartScreen.StartGame(GameMode)
    ↓
GameManager.StartGame(GameMode)
    ↓
Publishes GameStartedEvent { mode }
    ↓
LevelManager.OnGameStarted(evt)
    ↓
Switch on evt.mode:
    ├─ GameMode.Arcade  → StartNewGame() → Level 1
    ├─ GameMode.Rogue   → StartRogueMode() → Level 998
    └─ GameMode.Test    → StartTestMode() → Level 999
    ↓
LoadLevelConfig(levelNumber)
    ↓
LevelGenerator.GetLevelConfig(levelNumber)
    ↓
Switch on levelNumber:
    ├─ 1-99  → GenerateDynamicLevelConfig() [ARCADE]
    ├─ 998   → GetRogueLevelConfig() [ROGUE]
    └─ 999   → GetTestLevelConfig() [TEST]
    ↓
Returns LevelConfig with spawn rates
    ↓
LevelManager.ApplySpawnRates()
    ↓
EnemySpawner.SetSpawnRates()
    ↓
Enemies spawn at configured rates
```

## Key Rules

1. **Never mix modes** - Each mode has its own level number space
2. **Level number determines config** - LevelGenerator routes based on level number
3. **Spawn rates are mode-specific** - TEST is slower to prevent overcrowding
4. **Clear logging** - Every mode transition is logged with "=========="
5. **No auto-start** - Game waits at StartScreen for user to select mode

## Debugging

Check console for these logs to trace mode flow:

```
[StartScreen] ========== BUTTON CLICKED! Starting game in Arcade mode ==========
[GameManager] ========== STARTING GAME IN Arcade MODE ==========
[LevelManager] ========== OnGameStarted received! Mode = Arcade ==========
[LevelManager] ========== STARTING ARCADE MODE ==========
[LevelGenerator] Returning ARCADE MODE config (level 1)
[LevelManager] LoadLevelConfig(1) -> NEURAL INITIALIZATION - LVL 1
[EnemySpawner] SetSpawnRates called: DataMite=3, ScanDrone=12, ChaosWorm=∞, ...
```

If you see wrong enemies spawning, check:
1. Which mode LevelManager started
2. What level number was set
3. What config LevelGenerator returned
4. What spawn rates EnemySpawner received
