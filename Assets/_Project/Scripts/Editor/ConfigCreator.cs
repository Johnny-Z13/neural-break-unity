using UnityEngine;
using UnityEditor;
using NeuralBreak.Config;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor utility to create default configuration assets.
    /// </summary>
    public static class ConfigCreator
    {
        [MenuItem("Tools/Neural Break/Create Default Config")]
        public static void CreateDefaultConfig()
        {
            // Create the config asset
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();

            // Initialize with defaults via reflection call to ConfigProvider's logic
            InitializeDefaults(config);

            // Ensure directory exists
            string directory = "Assets/_Project/Resources/Config";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/_Project/Resources/Config");
                AssetDatabase.Refresh();
            }

            // Save the asset
            string path = $"{directory}/GameBalanceConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the created asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[ConfigCreator] Created GameBalanceConfig at {path}");
        }

        private static void InitializeDefaults(GameBalanceConfig config)
        {
            // Player defaults
            config.player = new PlayerConfig
            {
                baseSpeed = 7f,
                acceleration = 25f,
                deceleration = 20f,
                dashSpeed = 32f,
                dashDuration = 0.15f,
                dashCooldown = 2.5f,
                maxHealth = 130,
                startingShields = 0,
                maxShields = 3,
                spawnInvulnerabilityDuration = 2f,
                damageInvulnerabilityDuration = 0.5f,
                invulnerablePickupDuration = 7f,
                arenaRadius = 25f,
                boundaryPushStrength = 8f
            };

            // Weapon defaults
            config.weapon = new WeaponConfig
            {
                baseDamage = 12,
                baseFireRate = 0.12f,
                projectileSpeed = 22f,
                projectileLifetime = 2f,
                heatPerShot = 0.8f,
                heatCooldownRate = 15f,
                overheatThreshold = 100f,
                overheatCooldownDuration = 1.5f,
                maxPowerLevel = 10,
                damagePerLevel = 0.15f,
                fireRatePerLevel = 0.05f
            };

            // Enemy defaults
            config.dataMite = new EnemyTypeConfig
            {
                displayName = "Data Mite",
                color = new Color(1f, 0.5f, 0f),
                health = 1,
                speed = 1.5f,
                contactDamage = 5,
                scoreValue = 100,
                xpValue = 1,
                baseSpawnRate = 1.5f,
                poolSize = 100,
                explosionSize = ExplosionSize.Small
            };

            config.scanDrone = new EnemyTypeConfig
            {
                displayName = "Scan Drone",
                color = new Color(0.3f, 0.8f, 1f),
                health = 30,
                speed = 1.2f,
                contactDamage = 10,
                scoreValue = 250,
                xpValue = 3,
                baseSpawnRate = 8f,
                poolSize = 30,
                explosionSize = ExplosionSize.Medium,
                canShoot = true,
                fireRate = 2f,
                projectileDamage = 10
            };

            config.chaosWorm = new EnemyTypeConfig
            {
                displayName = "Chaos Worm",
                color = new Color(0.8f, 0.2f, 0.5f),
                health = 50,
                speed = 2f,
                contactDamage = 15,
                scoreValue = 500,
                xpValue = 5,
                baseSpawnRate = 15f,
                poolSize = 10,
                explosionSize = ExplosionSize.Large
            };

            config.voidSphere = new EnemyTypeConfig
            {
                displayName = "Void Sphere",
                color = new Color(0.6f, 0f, 1f),
                health = 100,
                speed = 0.8f,
                contactDamage = 25,
                scoreValue = 1000,
                xpValue = 10,
                baseSpawnRate = 20f,
                poolSize = 5,
                explosionSize = ExplosionSize.Large,
                canShoot = true,
                fireRate = 3f,
                projectileDamage = 15
            };

            config.crystalShard = new EnemyTypeConfig
            {
                displayName = "Crystal Shard",
                color = new Color(0.4f, 0.8f, 1f),
                health = 20,
                speed = 3f,
                contactDamage = 8,
                scoreValue = 750,
                xpValue = 8,
                baseSpawnRate = 25f,
                poolSize = 15,
                explosionSize = ExplosionSize.Medium
            };

            config.fizzer = new EnemyTypeConfig
            {
                displayName = "Fizzer",
                color = new Color(0f, 1f, 0.5f),
                health = 2,
                speed = 8f,
                contactDamage = 5,
                scoreValue = 200,
                xpValue = 2,
                baseSpawnRate = 12f,
                poolSize = 50,
                explosionSize = ExplosionSize.Small
            };

            config.ufo = new EnemyTypeConfig
            {
                displayName = "UFO",
                color = new Color(0.5f, 0.7f, 1f),
                health = 80,
                speed = 1.5f,
                contactDamage = 20,
                scoreValue = 1500,
                xpValue = 15,
                baseSpawnRate = 50f,
                poolSize = 20,
                explosionSize = ExplosionSize.Medium,
                canShoot = true,
                fireRate = 1.5f,
                projectileDamage = 12
            };

            config.boss = new EnemyTypeConfig
            {
                displayName = "Boss",
                color = Color.red,
                health = 500,
                speed = 0.5f,
                contactDamage = 50,
                scoreValue = 5000,
                xpValue = 40,
                baseSpawnRate = 120f,
                poolSize = 3,
                explosionSize = ExplosionSize.Boss,
                canShoot = true,
                fireRate = 1f,
                projectileDamage = 20
            };

            // Pickup defaults
            config.powerUp = new PickupConfig
            {
                displayName = "Power Up",
                color = new Color(1f, 0.8f, 0f),
                spawnsPerLevel = 3,
                minSpawnInterval = 25f,
                maxSpawnInterval = 40f,
                lifetime = 15f,
                magnetRadius = 5f,
                magnetStrength = 16f
            };

            config.speedUp = new PickupConfig
            {
                displayName = "Speed Up",
                color = new Color(1f, 0.3f, 0.8f),
                spawnsPerLevel = 2,
                minSpawnInterval = 30f,
                maxSpawnInterval = 45f,
                lifetime = 15f,
                magnetRadius = 5f,
                magnetStrength = 16f,
                effectDuration = 10f,
                effectAmount = 1.5f
            };

            config.medPack = new PickupConfig
            {
                displayName = "Med Pack",
                color = Color.green,
                spawnsPerLevel = 3,
                minSpawnInterval = 20f,
                maxSpawnInterval = 30f,
                lifetime = 15f,
                magnetRadius = 5f,
                magnetStrength = 16f,
                effectAmount = 30f
            };

            config.shield = new PickupConfig
            {
                displayName = "Shield",
                color = Color.cyan,
                spawnsPerLevel = 2,
                minSpawnInterval = 35f,
                maxSpawnInterval = 50f,
                lifetime = 15f,
                magnetRadius = 5f,
                magnetStrength = 16f,
                effectAmount = 1f
            };

            config.invulnerable = new PickupConfig
            {
                displayName = "Invulnerable",
                color = Color.yellow,
                spawnsPerLevel = 1,
                minSpawnInterval = 60f,
                maxSpawnInterval = 90f,
                lifetime = 12f,
                magnetRadius = 5f,
                magnetStrength = 16f,
                effectDuration = 7f
            };

            // Combo defaults
            config.combo = new ComboConfig
            {
                comboDecayTime = 1.5f,
                comboWindow = 2f,
                multiplierPerKill = 0.1f,
                maxMultiplier = 10f,
                multiplierDecayRate = 0.5f
            };

            // Spawn defaults
            config.spawning = new SpawnConfig
            {
                minSpawnDistance = 8f,
                maxSpawnDistance = 20f,
                maxActiveEnemies = 200,
                difficultyPerLevel = 0.03f,
                minSpawnRateMultiplier = 0.3f
            };
        }
    }
}
