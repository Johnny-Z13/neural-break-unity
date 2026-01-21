using UnityEngine;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Generates level configurations for all 99 levels dynamically.
    /// Matches the TypeScript LevelManager implementation with:
    /// - Dynamic difficulty scaling
    /// - Surprise levels every 5 levels
    /// - All enemy types unlocked by level 5
    /// - Continuous difficulty ramp
    /// </summary>
    public static class LevelGenerator
    {
        public const int TOTAL_LEVELS = 99;

        // Level names that cycle through progression
        private static readonly string[] LEVEL_NAMES =
        {
            "NEURAL INITIALIZATION", "SYSTEM BREACH", "VOID CORRUPTION", "ALIEN INCURSION",
            "DATA STORM", "NEURAL OVERLOAD", "DIGITAL CHAOS", "ALIEN ARMADA",
            "QUANTUM FLUX", "CYBER ASSAULT", "MATRIX COLLAPSE", "BINARY STORM",
            "PROTOCOL BREACH", "FIREWALL FAILURE", "MEMORY LEAK", "STACK OVERFLOW",
            "BUFFER OVERRUN", "KERNEL PANIC", "SYSTEM CRASH", "TOTAL MELTDOWN"
        };

        /// <summary>
        /// Get configuration for a specific level
        /// </summary>
        public static LevelConfig GetLevelConfig(int level)
        {
            // Test mode
            if (level == 999)
                return GetTestLevelConfig();

            // Rogue mode (future implementation)
            if (level == 998)
                return GetRogueLevelConfig(1);

            int clampedLevel = Mathf.Clamp(level, 1, TOTAL_LEVELS);

            // Surprise levels every 5th level
            if (clampedLevel % 5 == 0 && clampedLevel > 0)
                return GetSurpriseLevelConfig(clampedLevel);

            // Normal dynamic level
            return GenerateDynamicLevelConfig(clampedLevel);
        }

        /// <summary>
        /// Generate surprise level config (every 5th level)
        /// These are themed levels with special enemy compositions
        /// </summary>
        private static LevelConfig GetSurpriseLevelConfig(int level)
        {
            // Difficulty scaling
            float difficultyScale = 1f + (level - 1) * 0.03f;
            float spawnScale = Mathf.Max(0.3f, 1f - (level - 1) * 0.008f);

            // Cycle through surprise types
            int surpriseType = (level / 5) % 10;

            var config = new LevelConfig { level = level };

            switch (surpriseType)
            {
                case 1: // Level 5: WORM INVASION
                    config.name = "WORM INVASION!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(10 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(8 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 2.0f * spawnScale,
                        scanDroneRate = float.PositiveInfinity,
                        chaosWormRate = 8.0f * spawnScale,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 2: // Level 10: FIZZER FRENZY
                    config.name = "FIZZER FRENZY!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(15 * difficultyScale),
                        fizzers = Mathf.FloorToInt(20 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.5f * spawnScale,
                        scanDroneRate = float.PositiveInfinity,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = 2.5f * spawnScale,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 3: // Level 15: UFO ARMADA
                    config.name = "UFO ARMADA!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(20 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(5 * difficultyScale),
                        ufos = Mathf.FloorToInt(12 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.2f * spawnScale,
                        scanDroneRate = 8.0f * spawnScale,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = 6.0f * spawnScale,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 4: // Level 20: CRYSTAL CAVERN
                    config.name = "CRYSTAL CAVERN!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(15 * difficultyScale),
                        crystalShards = Mathf.FloorToInt(10 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.5f * spawnScale,
                        scanDroneRate = float.PositiveInfinity,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = 7.0f * spawnScale,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 5: // Level 25: BOSS RUSH
                    config.name = "BOSS RUSH!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(30 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(10 * difficultyScale),
                        chaosWorms = 1,
                        voidSpheres = 1,
                        crystalShards = 1,
                        ufos = 1,
                        bosses = Mathf.FloorToInt(3 + level / 25f)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.0f * spawnScale,
                        scanDroneRate = 6.0f * spawnScale,
                        chaosWormRate = 60.0f,
                        voidSphereRate = 60.0f,
                        crystalShardRate = 60.0f,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = 60.0f,
                        bossRate = 20.0f * spawnScale
                    };
                    break;

                case 6: // Level 30: VOID NIGHTMARE
                    config.name = "VOID NIGHTMARE!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(25 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(8 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(6 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.2f * spawnScale,
                        scanDroneRate = 7.0f * spawnScale,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = 12.0f * spawnScale,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 7: // Level 35: DRONE SWARM
                    config.name = "DRONE SWARM!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(20 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(40 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.5f * spawnScale,
                        scanDroneRate = 2.0f * spawnScale,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 8: // Level 40: MITE APOCALYPSE
                    config.name = "MITE APOCALYPSE!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(150 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 0.3f * spawnScale,
                        scanDroneRate = float.PositiveInfinity,
                        chaosWormRate = float.PositiveInfinity,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = float.PositiveInfinity,
                        fizzerRate = float.PositiveInfinity,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 9: // Level 45: TOTAL CHAOS
                    config.name = "TOTAL CHAOS!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(40 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(15 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(5 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(3 * difficultyScale),
                        crystalShards = Mathf.FloorToInt(4 * difficultyScale),
                        fizzers = Mathf.FloorToInt(10 * difficultyScale),
                        ufos = Mathf.FloorToInt(5 * difficultyScale),
                        bosses = 2
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 0.6f * spawnScale,
                        scanDroneRate = 3.0f * spawnScale,
                        chaosWormRate = 15.0f * spawnScale,
                        voidSphereRate = 25.0f * spawnScale,
                        crystalShardRate = 20.0f * spawnScale,
                        fizzerRate = 5.0f * spawnScale,
                        ufoRate = 15.0f * spawnScale,
                        bossRate = 45.0f
                    };
                    break;

                case 0: // Level 50, 100: NEURAL MELTDOWN
                default:
                    config.name = "NEURAL MELTDOWN!";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(80 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(25 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(8 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(4 * difficultyScale),
                        crystalShards = Mathf.FloorToInt(6 * difficultyScale),
                        fizzers = Mathf.FloorToInt(15 * difficultyScale),
                        ufos = Mathf.FloorToInt(8 * difficultyScale),
                        bosses = Mathf.FloorToInt(2 + level / 20f)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 0.4f * spawnScale,
                        scanDroneRate = 2.5f * spawnScale,
                        chaosWormRate = 10.0f * spawnScale,
                        voidSphereRate = 20.0f * spawnScale,
                        crystalShardRate = 15.0f * spawnScale,
                        fizzerRate = 4.0f * spawnScale,
                        ufoRate = 12.0f * spawnScale,
                        bossRate = 35.0f
                    };
                    break;
            }

            return config;
        }

        /// <summary>
        /// Generate dynamic level config with ramping difficulty
        /// All enemy types unlocked by level 5 for faster progression
        /// </summary>
        private static LevelConfig GenerateDynamicLevelConfig(int level)
        {
            // Difficulty scales 3% per level
            float difficultyScale = 1f + (level - 1) * 0.03f;

            // Spawn rates get faster (minimum 0.3x)
            float spawnScale = Mathf.Max(0.3f, 1f - (level - 1) * 0.008f);

            // Level name cycles through array
            int nameIndex = (level - 1) % LEVEL_NAMES.Length;
            string levelName = $"{LEVEL_NAMES[nameIndex]} - LVL {level}";

            // Enemy type availability (compressed progression - all by level 5)
            bool hasWorms = level >= 2;
            bool hasVoidSpheres = level >= 3;
            bool hasCrystals = level >= 3;
            bool hasUFOs = level >= 4;
            bool hasBosses = level >= 5;
            bool hasFizzers = level >= 6;

            // Calculate objectives - ensure minimum of 1 when enemy type is enabled
            var objectives = new LevelObjectives
            {
                dataMites = Mathf.FloorToInt((20 + level * 2) * difficultyScale),
                scanDrones = Mathf.FloorToInt((5 + level * 0.8f) * difficultyScale),
                chaosWorms = hasWorms ? Mathf.Max(1, Mathf.FloorToInt((1 + level * 0.2f) * difficultyScale)) : 0,
                voidSpheres = hasVoidSpheres ? Mathf.Max(1, Mathf.FloorToInt((1 + level * 0.1f) * difficultyScale)) : 0,
                crystalShards = hasCrystals ? Mathf.Max(1, Mathf.FloorToInt((1 + level * 0.12f) * difficultyScale)) : 0,
                fizzers = hasFizzers ? Mathf.Max(1, Mathf.FloorToInt((2 + level * 0.15f) * difficultyScale)) : 0,
                ufos = hasUFOs ? Mathf.Max(1, Mathf.FloorToInt((1 + level * 0.12f) * difficultyScale)) : 0,
                bosses = hasBosses ? Mathf.Max(1, Mathf.FloorToInt(level * 0.06f)) : 0
            };

            // Calculate spawn rates - faster spawns as level increases
            var spawnRates = new SpawnRates
            {
                dataMiteRate = Mathf.Max(0.3f, 1.5f - level * 0.012f) * spawnScale,
                scanDroneRate = Mathf.Max(1.5f, 8f - level * 0.07f) * spawnScale,
                chaosWormRate = hasWorms ? Mathf.Max(8f, 40f - level * 0.35f) * spawnScale : float.PositiveInfinity,
                voidSphereRate = hasVoidSpheres ? Mathf.Max(12f, 60f - level * 0.5f) * spawnScale : float.PositiveInfinity,
                crystalShardRate = hasCrystals ? Mathf.Max(10f, 50f - level * 0.45f) * spawnScale : float.PositiveInfinity,
                fizzerRate = hasFizzers ? Mathf.Max(5f, 25f - level * 0.2f) * spawnScale : float.PositiveInfinity,
                ufoRate = hasUFOs ? Mathf.Max(10f, 45f - level * 0.4f) * spawnScale : float.PositiveInfinity,
                bossRate = hasBosses ? Mathf.Max(25f, 90f - level * 0.8f) : float.PositiveInfinity
            };

            return new LevelConfig
            {
                level = level,
                name = levelName,
                objectives = objectives,
                spawnRates = spawnRates
            };
        }

        /// <summary>
        /// Test level config - endless with all enemy types
        /// </summary>
        public static LevelConfig GetTestLevelConfig()
        {
            return new LevelConfig
            {
                level = 999,
                name = "TEST MODE - ALL ENEMIES",
                objectives = new LevelObjectives
                {
                    dataMites = 99999,
                    scanDrones = 99999,
                    chaosWorms = 99999,
                    voidSpheres = 99999,
                    crystalShards = 99999,
                    fizzers = 99999,
                    ufos = 99999,
                    bosses = 99999
                },
                spawnRates = new SpawnRates
                {
                    dataMiteRate = 2.0f,
                    scanDroneRate = 8.0f,
                    chaosWormRate = 12.0f,
                    voidSphereRate = 15.0f,
                    crystalShardRate = 10.0f,
                    fizzerRate = 14.0f,
                    ufoRate = 18.0f,
                    bossRate = 25.0f
                }
            };
        }

        /// <summary>
        /// Rogue mode level config (layered progression)
        /// </summary>
        public static LevelConfig GetRogueLevelConfig(int layerNumber)
        {
            int themeIndex = ((layerNumber - 1) % 6) + 1;
            float difficultyScale = 1f + (layerNumber - 1) * 0.15f;

            var config = new LevelConfig { level = 998 };

            switch (themeIndex)
            {
                case 1: // SWARM LAYER
                    config.name = $"SWARM ASSAULT - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(40 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(15 * difficultyScale),
                        chaosWorms = 1,
                        crystalShards = 1,
                        fizzers = 2
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.0f / difficultyScale,
                        scanDroneRate = 5.0f / difficultyScale,
                        chaosWormRate = 45.0f,
                        voidSphereRate = float.PositiveInfinity,
                        crystalShardRate = 40.0f,
                        fizzerRate = 15.0f / difficultyScale,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 2: // CHAOS LAYER
                    config.name = $"CHAOS STORM - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(20 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(8 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(3 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(2 * difficultyScale),
                        crystalShards = 1,
                        fizzers = 1
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.5f / difficultyScale,
                        scanDroneRate = 7.0f,
                        chaosWormRate = 25.0f / difficultyScale,
                        voidSphereRate = 35.0f / difficultyScale,
                        crystalShardRate = 50.0f,
                        fizzerRate = 20.0f,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 3: // CRYSTAL LAYER
                    config.name = $"CRYSTAL FIELD - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(25 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(10 * difficultyScale),
                        chaosWorms = 1,
                        voidSpheres = 1,
                        crystalShards = Mathf.FloorToInt(3 * difficultyScale),
                        fizzers = 2
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.3f / difficultyScale,
                        scanDroneRate = 6.5f,
                        chaosWormRate = 50.0f,
                        voidSphereRate = 45.0f,
                        crystalShardRate = 20.0f / difficultyScale,
                        fizzerRate = 16.0f / difficultyScale,
                        ufoRate = float.PositiveInfinity,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 4: // MIXED LAYER
                    config.name = $"NEURAL MAZE - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(30 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(12 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(2 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(2 * difficultyScale),
                        crystalShards = Mathf.FloorToInt(2 * difficultyScale),
                        fizzers = 3,
                        ufos = 1
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.2f / difficultyScale,
                        scanDroneRate = 6.0f / difficultyScale,
                        chaosWormRate = 35.0f,
                        voidSphereRate = 40.0f,
                        crystalShardRate = 38.0f,
                        fizzerRate = 14.0f / difficultyScale,
                        ufoRate = 55.0f,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 5: // ELITE LAYER
                    config.name = $"ELITE GAUNTLET - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(15 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(10 * difficultyScale),
                        chaosWorms = Mathf.FloorToInt(2 * difficultyScale),
                        voidSpheres = Mathf.FloorToInt(3 * difficultyScale),
                        crystalShards = Mathf.FloorToInt(2 * difficultyScale),
                        fizzers = 2,
                        ufos = Mathf.FloorToInt(2 * difficultyScale)
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 2.0f / difficultyScale,
                        scanDroneRate = 7.0f,
                        chaosWormRate = 30.0f,
                        voidSphereRate = 28.0f / difficultyScale,
                        crystalShardRate = 35.0f,
                        fizzerRate = 18.0f,
                        ufoRate = 40.0f / difficultyScale,
                        bossRate = float.PositiveInfinity
                    };
                    break;

                case 6: // BOSS LAYER
                default:
                    config.name = $"SECTOR GUARDIAN - LAYER {layerNumber}";
                    config.objectives = new LevelObjectives
                    {
                        dataMites = Mathf.FloorToInt(20 * difficultyScale),
                        scanDrones = Mathf.FloorToInt(8 * difficultyScale),
                        chaosWorms = 1,
                        voidSpheres = 1,
                        crystalShards = 1,
                        fizzers = 1,
                        ufos = 1,
                        bosses = 1
                    };
                    config.spawnRates = new SpawnRates
                    {
                        dataMiteRate = 1.5f / difficultyScale,
                        scanDroneRate = 7.0f,
                        chaosWormRate = 45.0f,
                        voidSphereRate = 50.0f,
                        crystalShardRate = 45.0f,
                        fizzerRate = 20.0f,
                        ufoRate = 55.0f,
                        bossRate = 30.0f
                    };
                    break;
            }

            return config;
        }
    }

    /// <summary>
    /// Runtime level configuration (not a ScriptableObject)
    /// Used for dynamically generated levels
    /// </summary>
    public class LevelConfig
    {
        public int level;
        public string name;
        public LevelObjectives objectives;
        public SpawnRates spawnRates;
    }
}
