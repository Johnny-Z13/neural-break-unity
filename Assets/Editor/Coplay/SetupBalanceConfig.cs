using UnityEngine;
using UnityEditor;
using NeuralBreak.Config;

/// <summary>
/// Sets up the GameBalanceConfig ScriptableObject with values from the TypeScript balance.config.ts
/// </summary>
public class SetupBalanceConfig
{
    public static string Execute()
    {
        // Find or create the config asset
        string configPath = "Assets/_Project/Config/GameBalanceConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(configPath);
        
        if (config == null)
        {
            // Create directory if needed
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Config"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Config");
            }
            
            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(config, configPath);
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // PLAYER CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════
        config.player = new PlayerConfig
        {
            baseSpeed = 7.0f,
            acceleration = 25f,
            deceleration = 20f,
            dashSpeed = 32f,
            dashDuration = 0.45f,
            dashCooldown = 2.5f,
            dashInvulnerable = true,
            thrustSpeedMultiplier = 1.6f,
            thrustAccelerationTime = 0.15f,
            thrustDecelerationTime = 0.25f,
            maxHealth = 130,
            startingShields = 0,
            maxShields = 3,
            maxPowerUpLevel = 10,
            powerUpDamageMultiplier = 0.6f,
            maxSpeedLevel = 20,
            speedBoostPerLevel = 0.05f,
            spawnInvulnerabilityDuration = 2f,
            damageInvulnerabilityDuration = 0.5f,
            invulnerablePickupDuration = 7.0f,
            shieldAbsorbsOneHit = true,
            arenaRadius = 29f,
            boundaryPushStrength = 8f,
            collisionRadius = 0.5f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // LEGACY WEAPON CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════
        config.weapon = new WeaponConfig
        {
            baseDamage = 12,
            baseFireRate = 0.12f,
            projectileSpeed = 22f,
            projectileLifetime = 1.7f,
            heatEnabled = true,
            heatPerShot = 0.8f,
            heatCooldownRate = 85f,
            overheatThreshold = 100f,
            overheatCooldownDuration = 0.8f,
            maxPowerLevel = 10,
            damagePerLevel = 0.6f,
            fireRatePerLevel = 0.05f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // NEW WEAPON SYSTEM
        // ═══════════════════════════════════════════════════════════════════
        config.weaponSystem = new WeaponSystemConfig
        {
            baseDamage = 12,
            baseFireRate = 0.12f,
            baseProjectileSpeed = 22f,
            projectileLifetime = 1.7f,
            projectileSize = 0.15f,
            
            forwardWeapon = new ForwardWeaponConfig
            {
                pattern = ForwardFirePattern.Single,
                doubleSpreadAngle = 15f,
                tripleSpreadAngle = 30f,
                quadSpreadAngle = 45f,
                x5SpreadAngle = 60f,
                forwardOffset = 0.6f,
                lateralOffset = 0.2f
            },
            
            rearWeapon = new RearWeaponConfig
            {
                enabled = false,
                damageMultiplier = 0.5f,
                fireRateMultiplier = 1.0f,
                rearOffset = 0.4f,
                syncWithForward = true,
                independentFireRate = 0.3f
            },
            
            heatSystem = new HeatSystemConfig
            {
                enabled = true,
                heatPerShot = 0.8f,
                cooldownRate = 85f,
                maxHeat = 100f,
                overheatDuration = 0.8f,
                overheatCooldownMultiplier = 1.5f,
                multiShotHeatMultiplier = 0.3f,
                rearWeaponHeatMultiplier = 0.5f
            },
            
            powerLevels = new PowerLevelConfig
            {
                maxLevel = 10,
                autoUpgradePattern = true,
                damagePerLevel = 0.1f,
                fireRatePerLevel = 0.005f,
                projectileSpeedPerLevel = 0.5f,
                projectileSizePerLevel = 0.01f,
                doubleShotLevel = 2,
                tripleShotLevel = 4,
                quadShotLevel = 7,
                x5ShotLevel = 9
            },
            
            modifiers = new WeaponModifiersConfig
            {
                rapidFireMultiplier = 1.5f,
                rapidFireDuration = 10f,
                damageBoostMultiplier = 2f,
                damageBoostDuration = 8f,
                speedBoostMultiplier = 1.5f,
                sizeBoostMultiplier = 1.5f
            },
            
            specials = new SpecialWeaponsConfig
            {
                piercingEnabled = false,
                maxPierceCount = 5,
                pierceDamageReduction = 0.1f,
                homingEnabled = false,
                homingRange = 8f,
                homingStrength = 5f,
                explosiveEnabled = false,
                explosionRadius = 2f,
                explosionDamageMultiplier = 0.5f,
                ricochetEnabled = false,
                maxBounces = 3,
                chainLightningEnabled = false,
                chainRange = 4f,
                maxChainJumps = 3,
                chainDamageReduction = 0.3f,
                beamEnabled = false,
                beamDPS = 50f,
                beamRange = 15f
            }
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // ENEMY CONFIGURATIONS
        // ═══════════════════════════════════════════════════════════════════
        
        config.dataMite = new EnemyTypeConfig
        {
            displayName = "Data Mite",
            color = new Color(1f, 0.3f, 0.3f),
            health = 1,
            speed = 1.5f,
            contactDamage = 5,
            collisionRadius = 0.42f,
            scoreValue = 100,
            xpValue = 1,
            baseSpawnRate = 1.5f,
            poolSize = 100,
            canShoot = false,
            deathDamage = 0,
            deathRadius = 0f,
            spawnDuration = 0.25f,
            deathDuration = 0.3f,
            explosionSize = ExplosionSize.Small,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.scanDrone = new EnemyTypeConfig
        {
            displayName = "Scan Drone",
            color = new Color(0.3f, 0.7f, 1f),
            health = 30,
            speed = 1.2f,
            contactDamage = 15,
            collisionRadius = 1.2f,
            scoreValue = 600,
            xpValue = 6,
            baseSpawnRate = 8f,
            poolSize = 30,
            canShoot = true,
            fireRate = 2.0f,
            projectileSpeed = 7.0f,
            projectileDamage = 15,
            burstCount = 1,
            burstDelay = 0f,
            detectionRange = 15f,
            patrolRange = 10f,
            deathDamage = 0,
            deathRadius = 0f,
            spawnDuration = 0.3f,
            deathDuration = 0.5f,
            explosionSize = ExplosionSize.Small,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.fizzer = new EnemyTypeConfig
        {
            displayName = "Fizzer",
            color = new Color(0.2f, 0.8f, 1f),
            health = 2,
            speed = 8.0f,
            contactDamage = 6,
            collisionRadius = 0.35f,
            scoreValue = 1500,
            xpValue = 15,
            baseSpawnRate = 12f,
            poolSize = 20,
            canShoot = true,
            fireRate = 3.0f,
            projectileSpeed = 9.0f,
            projectileDamage = 6,
            burstCount = 2,
            burstDelay = 0.2f,
            detectionRange = 20f,
            patrolRange = 0f,
            deathDamage = 15,
            deathRadius = 2.0f,
            spawnDuration = 0.2f,
            deathDuration = 0.4f,
            explosionSize = ExplosionSize.Medium,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.ufo = new EnemyTypeConfig
        {
            displayName = "UFO",
            color = new Color(0.7f, 0.7f, 0.8f),
            health = 30,
            speed = 2.8f,
            contactDamage = 12,
            collisionRadius = 1.2f,
            scoreValue = 2500,
            xpValue = 25,
            baseSpawnRate = 15f,
            poolSize = 15,
            canShoot = true,
            fireRate = 2.0f,
            projectileSpeed = 8.0f,
            projectileDamage = 14,
            burstCount = 3,
            burstDelay = 0.15f,
            detectionRange = 20f,
            patrolRange = 0f,
            deathDamage = 25,
            deathRadius = 3.0f,
            spawnDuration = 0.4f,
            deathDuration = 0.6f,
            explosionSize = ExplosionSize.Medium,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.chaosWorm = new EnemyTypeConfig
        {
            displayName = "Chaos Worm",
            color = new Color(0.8f, 0.2f, 0.8f),
            health = 100,
            speed = 1.5f,
            contactDamage = 15,
            collisionRadius = 2.5f,
            scoreValue = 3500,
            xpValue = 35,
            baseSpawnRate = 30f,
            poolSize = 5,
            canShoot = false,
            fireRate = 0f,
            projectileSpeed = 8f,
            projectileDamage = 15,
            burstCount = 6,
            burstDelay = 0f,
            detectionRange = 30f,
            patrolRange = 0f,
            deathDamage = 0,
            deathRadius = 0f,
            spawnDuration = 0.5f,
            deathDuration = 2.0f,
            explosionSize = ExplosionSize.Large,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.voidSphere = new EnemyTypeConfig
        {
            displayName = "Void Sphere",
            color = new Color(0.3f, 0f, 0.5f),
            health = 650,
            speed = 0.5f,
            contactDamage = 40,
            collisionRadius = 3.2f,
            scoreValue = 5000,
            xpValue = 50,
            baseSpawnRate = 45f,
            poolSize = 3,
            canShoot = true,
            fireRate = 3.0f,
            projectileSpeed = 5.0f,
            projectileDamage = 20,
            burstCount = 4,
            burstDelay = 0.25f,
            detectionRange = 25f,
            patrolRange = 0f,
            deathDamage = 50,
            deathRadius = 8.0f,
            spawnDuration = 1.0f,
            deathDuration = 1.5f,
            explosionSize = ExplosionSize.Boss,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.crystalShard = new EnemyTypeConfig
        {
            displayName = "Crystal Swarm",
            color = new Color(0.5f, 0.8f, 1f),
            health = 250,
            speed = 1.8f,
            contactDamage = 25,
            collisionRadius = 4.5f,
            scoreValue = 4500,
            xpValue = 45,
            baseSpawnRate = 40f,
            poolSize = 5,
            canShoot = true,
            fireRate = 3.5f,
            projectileSpeed = 8.0f,
            projectileDamage = 10,
            burstCount = 2,
            burstDelay = 0.2f,
            detectionRange = 20f,
            patrolRange = 0f,
            deathDamage = 30,
            deathRadius = 5.0f,
            spawnDuration = 0.6f,
            deathDuration = 0.8f,
            explosionSize = ExplosionSize.Large,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        config.boss = new EnemyTypeConfig
        {
            displayName = "Boss",
            color = new Color(1f, 0.2f, 0.2f),
            health = 180,
            speed = 0.3f,
            contactDamage = 25,
            collisionRadius = 3.5f,
            scoreValue = 10000,
            xpValue = 100,
            baseSpawnRate = 70f,
            poolSize = 1,
            canShoot = true,
            fireRate = 1.5f,
            projectileSpeed = 6.5f,
            projectileDamage = 18,
            burstCount = 5,
            burstDelay = 0.1f,
            detectionRange = 40f,
            patrolRange = 0f,
            deathDamage = 75,
            deathRadius = 12.0f,
            spawnDuration = 2.0f,
            deathDuration = 3.0f,
            explosionSize = ExplosionSize.Boss,
            healthScalePerLevel = 1.025f,
            speedScalePerLevel = 1.012f,
            damageScalePerLevel = 1.02f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // PICKUP CONFIGURATIONS
        // ═══════════════════════════════════════════════════════════════════
        
        config.powerUp = new PickupConfig
        {
            displayName = "Power Up",
            color = new Color(1f, 0.8f, 0f),
            spawnsPerLevel = 2,
            minSpawnInterval = 30f,
            maxSpawnInterval = 45f,
            lifetime = 15f,
            magnetRadius = 5.0f,
            magnetStrength = 16.0f,
            maxMagnetSpeed = 18.0f,
            effectDuration = 0f,
            effectAmount = 1f,
            healthThreshold = 1.0f
        };
        
        config.speedUp = new PickupConfig
        {
            displayName = "Speed Up",
            color = new Color(0f, 1f, 0.5f),
            spawnsPerLevel = 2,
            minSpawnInterval = 25f,
            maxSpawnInterval = 35f,
            lifetime = 15f,
            magnetRadius = 5.0f,
            magnetStrength = 16.0f,
            maxMagnetSpeed = 18.0f,
            effectDuration = 0f,
            effectAmount = 0.05f,
            healthThreshold = 1.0f
        };
        
        config.medPack = new PickupConfig
        {
            displayName = "Med Pack",
            color = new Color(1f, 0.3f, 0.3f),
            spawnsPerLevel = 3,
            minSpawnInterval = 20f,
            maxSpawnInterval = 30f,
            lifetime = 15f,
            magnetRadius = 5.0f,
            magnetStrength = 16.0f,
            maxMagnetSpeed = 18.0f,
            effectDuration = 0f,
            effectAmount = 35f,
            healthThreshold = 0.8f
        };
        
        config.shield = new PickupConfig
        {
            displayName = "Shield",
            color = new Color(0.3f, 0.5f, 1f),
            spawnsPerLevel = 2,
            minSpawnInterval = 20f,
            maxSpawnInterval = 30f,
            lifetime = 15f,
            magnetRadius = 5.0f,
            magnetStrength = 16.0f,
            maxMagnetSpeed = 18.0f,
            effectDuration = 0f,
            effectAmount = 1f,
            healthThreshold = 1.0f
        };
        
        config.invulnerable = new PickupConfig
        {
            displayName = "Invulnerable",
            color = new Color(1f, 1f, 0f),
            spawnsPerLevel = 1,
            minSpawnInterval = 60f,
            maxSpawnInterval = 90f,
            lifetime = 15f,
            magnetRadius = 5.0f,
            magnetStrength = 16.0f,
            maxMagnetSpeed = 18.0f,
            effectDuration = 7.0f,
            effectAmount = 0f,
            healthThreshold = 1.0f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // COMBO SYSTEM
        // ═══════════════════════════════════════════════════════════════════
        config.combo = new ComboConfig
        {
            comboDecayTime = 3.0f,
            comboWindow = 1.5f,
            multiplierPerKill = 0.1f,
            maxMultiplier = 10f,
            multiplierDecayRate = 2.0f,
            baseKillPoints = 100,
            levelCompleteBonus = 1000,
            bossKillMultiplier = 2.0f,
            perfectLevelBonus = 500
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // SPAWNING
        // ═══════════════════════════════════════════════════════════════════
        config.spawning = new SpawnConfig
        {
            minSpawnDistance = 5f,
            maxSpawnDistance = 28f,
            pickupSpawnBoundary = 25f,
            maxActiveEnemies = 200,
            spawnRateScalePerLevel = 0.992f,
            minSpawnRateMultiplier = 0.3f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // LEVEL PROGRESSION
        // ═══════════════════════════════════════════════════════════════════
        config.levels = new LevelConfig
        {
            totalLevels = 99,
            levelDuration = 90f,
            bossAppearsAt = 70f,
            enemyHealthScale = 1.025f,
            enemySpeedScale = 1.012f,
            enemyDamageScale = 1.02f,
            spawnRateScale = 0.992f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // WORLD SETTINGS
        // ═══════════════════════════════════════════════════════════════════
        config.world = new WorldConfig
        {
            worldSize = 80f,
            boundaryRadius = 29f,
            boundaryDamage = 10f,
            spawnBoundary = 28f,
            pickupSpawnBoundary = 25f,
            minPlayerDistance = 5f
        };
        
        // ═══════════════════════════════════════════════════════════════════
        // FEEDBACK SETTINGS
        // ═══════════════════════════════════════════════════════════════════
        config.feedback = new FeedbackConfig
        {
            smallShake = new ShakeSettings { intensity = 0.2f, duration = 0.1f },
            mediumShake = new ShakeSettings { intensity = 0.5f, duration = 0.3f },
            largeShake = new ShakeSettings { intensity = 1.0f, duration = 0.5f },
            minZoom = 6f,
            maxZoom = 22f,
            zoomSpeed = 3.0f,
            baseZoom = 8f,
            particleDensity = 1.0f,
            trailEnabled = true,
            explosionScale = 1.0f
        };
        
        // Save the asset
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        return $"✅ GameBalanceConfig updated!\n" +
               $"   Path: {configPath}\n" +
               $"   Player: HP={config.player.maxHealth}, Speed={config.player.baseSpeed}\n" +
               $"   Weapon System: Pattern auto-upgrade={config.weaponSystem.powerLevels.autoUpgradePattern}\n" +
               $"   Enemies: 8 types configured";
    }
}
