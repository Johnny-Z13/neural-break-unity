using UnityEngine;

namespace NeuralBreak.Config
{
    /// <summary>
    /// Provides global access to game configuration.
    /// Loads config from Resources folder for easy access.
    /// This replaces hard-coded values throughout the codebase.
    /// </summary>
    public static class ConfigProvider
    {
        private static GameBalanceConfig _balance;
        private static bool _initialized;

        /// <summary>
        /// The master game balance configuration.
        /// Lazy-loaded from Resources/Config/GameBalanceConfig.
        /// </summary>
        public static GameBalanceConfig Balance
        {
            get
            {
                if (!_initialized)
                {
                    Initialize();
                }
                return _balance;
            }
        }

        /// <summary>
        /// Shortcut accessors for common configs with null checks
        /// </summary>
        public static PlayerConfig Player
        {
            get
            {
                if (Balance == null)
                {
                    Debug.LogError("[ConfigProvider] Balance is null! Call Initialize() first.");
                    return null;
                }
                if (Balance.player == null)
                {
                    Debug.LogError("[ConfigProvider] PlayerConfig is null in Balance!");
                    return null;
                }
                return Balance.player;
            }
        }

        public static WeaponConfig Weapon
        {
            get
            {
                if (Balance == null)
                {
                    Debug.LogError("[ConfigProvider] Balance is null! Call Initialize() first.");
                    return null;
                }
                if (Balance.weapon == null)
                {
                    Debug.LogError("[ConfigProvider] WeaponConfig is null in Balance!");
                    return null;
                }
                return Balance.weapon;
            }
        }

        public static ComboConfig Combo
        {
            get
            {
                if (Balance == null)
                {
                    Debug.LogError("[ConfigProvider] Balance is null! Call Initialize() first.");
                    return null;
                }
                if (Balance.combo == null)
                {
                    Debug.LogError("[ConfigProvider] ComboConfig is null in Balance!");
                    return null;
                }
                return Balance.combo;
            }
        }

        public static SpawnConfig Spawning
        {
            get
            {
                if (Balance == null)
                {
                    Debug.LogError("[ConfigProvider] Balance is null! Call Initialize() first.");
                    return null;
                }
                if (Balance.spawning == null)
                {
                    Debug.LogError("[ConfigProvider] SpawnConfig is null in Balance!");
                    return null;
                }
                return Balance.spawning;
            }
        }

        /// <summary>
        /// Initialize the config provider.
        /// Called automatically on first access.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                _balance = Resources.Load<GameBalanceConfig>("Config/GameBalanceConfig");

                if (_balance == null)
                {
                    Debug.LogWarning("[ConfigProvider] GameBalanceConfig not found in Resources/Config/. Using defaults.");
                    _balance = ScriptableObject.CreateInstance<GameBalanceConfig>();

                    if (_balance == null)
                    {
                        Debug.LogError("[ConfigProvider] Failed to create default config instance!");
                        return;
                    }

                    SetDefaults(_balance);
                }

                _initialized = true;
                Debug.Log("[ConfigProvider] Configuration loaded successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigProvider] Failed to initialize config: {ex.Message}");
                _initialized = false;
            }
        }

        /// <summary>
        /// Force reload configuration (useful for hot-reloading in editor)
        /// </summary>
        public static void Reload()
        {
            _initialized = false;
            _balance = null;
            Initialize();
        }

        /// <summary>
        /// Set default values matching the original TypeScript balance.config.ts
        /// </summary>
        private static void SetDefaults(GameBalanceConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[ConfigProvider] Cannot set defaults - config is null!");
                return;
            }
            // Player defaults - matching TypeScript BALANCE_CONFIG
            config.player = new PlayerConfig
            {
                baseSpeed = 7f,
                acceleration = 25f,
                deceleration = 20f,
                dashSpeed = 32f,
                dashDuration = 0.45f,  // FIXED: was 0.15, TS is 0.45
                dashCooldown = 2.5f,
                maxHealth = 100,       // User requested: 100 health
                startingShields = 0,
                maxShields = 3,
                spawnInvulnerabilityDuration = 2f,
                damageInvulnerabilityDuration = 0.5f,
                invulnerablePickupDuration = 7f,
                arenaRadius = 29f,     // FIXED: was 25, TS BOUNDARY_RADIUS is 29
                boundaryPushStrength = 8f,
                collisionRadius = 0.5f  // TypeScript player collision radius
            };

            // Weapon defaults - matching TypeScript BALANCE_CONFIG
            config.weapon = new WeaponConfig
            {
                baseDamage = 12,
                baseFireRate = 0.12f,
                projectileSpeed = 22f,
                projectileLifetime = 1.7f, // ~38 range / 22 speed = 1.7s
                heatPerShot = 0.8f,
                heatCooldownRate = 85f,    // FIXED: was 15, TS is 85
                overheatThreshold = 100f,
                overheatCooldownDuration = 0.8f, // FIXED: was 1.5, TS is 0.8
                maxPowerLevel = 10,
                damagePerLevel = 0.6f,     // FIXED: was 0.15, TS is 60% per level
                fireRatePerLevel = 0.05f
            };

            // Enemy defaults - FIXED to match TypeScript BALANCE_CONFIG exactly
            // Collision radii from TypeScript: DataMite 0.42, ScanDrone 1.2, Fizzer 0.35, UFO 1.2, ChaosWorm 2.5, VoidSphere 3.2, CrystalShard 4.5, Boss 3.5
            config.dataMite = CreateEnemyConfig("Data Mite", new Color(1f, 0.5f, 0f),
                health: 1, speed: 1.5f, damage: 5, score: 100, xp: 1,
                spawnRate: 1.5f, poolSize: 100, ExplosionSize.Small, collisionRadius: 0.42f);

            config.scanDrone = CreateEnemyConfig("Scan Drone", new Color(0.3f, 0.8f, 1f),
                health: 30, speed: 1.2f, damage: 15, score: 250, xp: 6, // FIXED: damage 15, xp 6
                spawnRate: 8f, poolSize: 30, ExplosionSize.Medium, collisionRadius: 1.2f, canShoot: true, fireRate: 2f, projectileDamage: 15);

            config.chaosWorm = CreateEnemyConfig("Chaos Worm", new Color(0.8f, 0.2f, 0.5f),
                health: 100, speed: 1.5f, damage: 15, score: 500, xp: 35, // FIXED: health 100, speed 1.5, xp 35
                spawnRate: 15f, poolSize: 10, ExplosionSize.Large, collisionRadius: 2.5f);

            config.voidSphere = CreateEnemyConfig("Void Sphere", new Color(0.6f, 0f, 1f),
                health: 650, speed: 0.5f, damage: 40, score: 1000, xp: 50, // FIXED: health 650, speed 0.5, damage 40, xp 50
                spawnRate: 20f, poolSize: 5, ExplosionSize.Large, collisionRadius: 3.2f, canShoot: true, fireRate: 3f, projectileDamage: 20);

            config.crystalShard = CreateEnemyConfig("Crystal Shard", new Color(0.4f, 0.8f, 1f),
                health: 250, speed: 1.8f, damage: 25, score: 750, xp: 45, // FIXED: health 250, speed 1.8, damage 25, xp 45
                spawnRate: 25f, poolSize: 15, ExplosionSize.Medium, collisionRadius: 4.5f);

            config.fizzer = CreateEnemyConfig("Fizzer", new Color(0f, 1f, 0.5f),
                health: 2, speed: 8f, damage: 6, score: 200, xp: 15, // FIXED: damage 6, xp 15
                spawnRate: 12f, poolSize: 50, ExplosionSize.Small, collisionRadius: 0.35f);

            config.ufo = CreateEnemyConfig("UFO", new Color(0.5f, 0.7f, 1f),
                health: 30, speed: 2.8f, damage: 12, score: 1500, xp: 25, // FIXED: health 30, speed 2.8, damage 12, xp 25
                spawnRate: 50f, poolSize: 20, ExplosionSize.Medium, collisionRadius: 1.2f, canShoot: true, fireRate: 2f, projectileDamage: 14);

            config.boss = CreateEnemyConfig("Boss", Color.red,
                health: 180, speed: 0.3f, damage: 25, score: 5000, xp: 100, // FIXED: health 180, speed 0.3, damage 25, xp 100
                spawnRate: 120f, poolSize: 3, ExplosionSize.Boss, collisionRadius: 3.5f, canShoot: true, fireRate: 1.5f, projectileDamage: 18);

            // Pickup defaults - FIXED to match TypeScript
            config.powerUp = CreatePickupConfig("Power Up", new Color(1f, 0.8f, 0f),
                spawnsPerLevel: 2, minInterval: 30f, maxInterval: 45f, lifetime: 15f); // FIXED: spawns 2, intervals 30-45

            config.speedUp = CreatePickupConfig("Speed Up", new Color(1f, 0.3f, 0.8f),
                spawnsPerLevel: 2, minInterval: 25f, maxInterval: 35f, lifetime: 15f, effectDuration: 10f, effectAmount: 1.5f); // FIXED: intervals 25-35

            config.medPack = CreatePickupConfig("Med Pack", Color.green,
                spawnsPerLevel: 3, minInterval: 20f, maxInterval: 30f, lifetime: 15f, effectAmount: 35f); // FIXED: heal 35

            config.shield = CreatePickupConfig("Shield", Color.cyan,
                spawnsPerLevel: 2, minInterval: 20f, maxInterval: 30f, lifetime: 15f, effectAmount: 1f); // FIXED: intervals 20-30

            config.invulnerable = CreatePickupConfig("Invulnerable", Color.yellow,
                spawnsPerLevel: 1, minInterval: 60f, maxInterval: 90f, lifetime: 12f, effectDuration: 7f);

            // Combo defaults - FIXED to match TypeScript
            config.combo = new ComboConfig
            {
                comboDecayTime = 3f,       // FIXED: was 1.5, TS COMBO_TIMER is 3
                comboWindow = 1.5f,         // FIXED: was 2, TS KILL_CHAIN_WINDOW is 1.5
                multiplierPerKill = 0.1f,
                maxMultiplier = 10f,
                multiplierDecayRate = 2f   // FIXED: was 0.5, TS MULTIPLIER_DECAY_TIME is 2
            };

            // Spawn defaults - FIXED
            config.spawning = new SpawnConfig
            {
                minSpawnDistance = 5f,     // FIXED: TS MIN_PLAYER_DISTANCE is 5
                maxSpawnDistance = 28f,    // FIXED: TS SPAWN_BOUNDARY is 28
                maxActiveEnemies = 200,
                difficultyPerLevel = 0.03f,
                minSpawnRateMultiplier = 0.3f
            };
        }

        private static EnemyTypeConfig CreateEnemyConfig(
            string name, Color color, int health, float speed, int damage,
            int score, int xp, float spawnRate, int poolSize, ExplosionSize explosion,
            float collisionRadius = 0.5f, bool canShoot = false, float fireRate = 2f, int projectileDamage = 10)
        {
            return new EnemyTypeConfig
            {
                displayName = name,
                color = color,
                health = health,
                speed = speed,
                contactDamage = damage,
                collisionRadius = collisionRadius,
                scoreValue = score,
                xpValue = xp,
                baseSpawnRate = spawnRate,
                poolSize = poolSize,
                explosionSize = explosion,
                canShoot = canShoot,
                fireRate = fireRate,
                projectileDamage = projectileDamage,
                spawnDuration = 0.25f,
                deathDuration = 0.5f
            };
        }

        private static PickupConfig CreatePickupConfig(
            string name, Color color, int spawnsPerLevel, float minInterval, float maxInterval,
            float lifetime, float effectDuration = 0f, float effectAmount = 0f)
        {
            return new PickupConfig
            {
                displayName = name,
                color = color,
                spawnsPerLevel = spawnsPerLevel,
                minSpawnInterval = minInterval,
                maxSpawnInterval = maxInterval,
                lifetime = lifetime,
                magnetRadius = 5f,
                magnetStrength = 16f,
                effectDuration = effectDuration,
                effectAmount = effectAmount
            };
        }
    }
}
