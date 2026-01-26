using UnityEngine;

namespace NeuralBreak.Config
{
    /// <summary>
    /// Master configuration asset containing all game balance values.
    /// This is the Unity equivalent of balance.config.ts from the TypeScript version.
    /// Designers can tweak all gameplay values without touching code.
    /// 
    /// ⚖️ BALANCE PHILOSOPHY (from original):
    /// - Easy to learn, hard to master
    /// - Fast-paced arcade action
    /// - Risk/reward choices
    /// - Escalating difficulty through levels
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Neural Break/Config/Game Balance")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Configuration")]
        public PlayerConfig player;

        [Header("Weapon Configuration (Legacy)")]
        public WeaponConfig weapon;

        [Header("=== NEW WEAPON SYSTEM ===")]
        public WeaponSystemConfig weaponSystem;

        [Header("Enemy Configurations")]
        public EnemyTypeConfig dataMite;
        public EnemyTypeConfig scanDrone;
        public EnemyTypeConfig chaosWorm;
        public EnemyTypeConfig voidSphere;
        public EnemyTypeConfig crystalShard;
        public EnemyTypeConfig fizzer;
        public EnemyTypeConfig ufo;
        public EnemyTypeConfig boss;

        [Header("Pickup Configurations")]
        public PickupConfig powerUp;
        public PickupConfig speedUp;
        public PickupConfig medPack;
        public PickupConfig shield;
        public PickupConfig invulnerable;

        [Header("Combo System")]
        public ComboConfig combo;

        [Header("Spawning")]
        public SpawnConfig spawning;

        [Header("Level Progression")]
        public LevelConfig levels;

        [Header("World Settings")]
        public WorldConfig world;

        [Header("Feedback Settings")]
        public FeedbackConfig feedback;

        /// <summary>
        /// Get enemy config by type (data-driven lookup, no switch statement)
        /// </summary>
        public EnemyTypeConfig GetEnemyConfig(Core.EnemyType type)
        {
            return type switch
            {
                Core.EnemyType.DataMite => dataMite,
                Core.EnemyType.ScanDrone => scanDrone,
                Core.EnemyType.ChaosWorm => chaosWorm,
                Core.EnemyType.VoidSphere => voidSphere,
                Core.EnemyType.CrystalShard => crystalShard,
                Core.EnemyType.Fizzer => fizzer,
                Core.EnemyType.UFO => ufo,
                Core.EnemyType.Boss => boss,
                _ => dataMite
            };
        }

        /// <summary>
        /// Get pickup config by type
        /// </summary>
        public PickupConfig GetPickupConfig(Core.PickupType type)
        {
            return type switch
            {
                Core.PickupType.PowerUp => powerUp,
                Core.PickupType.SpeedUp => speedUp,
                Core.PickupType.MedPack => medPack,
                Core.PickupType.Shield => shield,
                Core.PickupType.Invulnerable => invulnerable,
                _ => powerUp
            };
        }

        /// <summary>
        /// Get scaled value for current level (matches TypeScript getScaledValue)
        /// </summary>
        public static float GetScaledValue(float baseValue, int level, float scalePerLevel)
        {
            return baseValue * Mathf.Pow(scalePerLevel, level - 1);
        }
    }

    /// <summary>
    /// Player configuration values - matches TypeScript BALANCE_CONFIG.PLAYER
    /// </summary>
    [System.Serializable]
    public class PlayerConfig
    {
        [Header("Core Stats")]
        [Tooltip("Base movement speed (TS: 7.0)")]
        public float baseSpeed = 7.0f;

        [Tooltip("Acceleration rate")]
        public float acceleration = 25f;

        [Tooltip("Deceleration rate when no input")]
        public float deceleration = 20f;

        [Header("Dash Ability")]
        [Tooltip("Speed during dash (TS: 32)")]
        public float dashSpeed = 32f;

        [Tooltip("How long dash lasts in seconds (TS: 0.45)")]
        public float dashDuration = 0.45f;

        [Tooltip("Time between dashes in seconds (TS: 2.5)")]
        public float dashCooldown = 2.5f;

        [Tooltip("Invincible during dash? (TS: true)")]
        public bool dashInvulnerable = true;

        [Header("Thrust")]
        [Tooltip("Speed multiplier when thrusting (1.0 = no boost)")]
        public float thrustSpeedMultiplier = 1.6f;

        [Tooltip("Time to reach full thrust speed")]
        public float thrustAccelerationTime = 0.15f;

        [Tooltip("Time to return to normal speed after releasing thrust")]
        public float thrustDecelerationTime = 0.25f;

        [Header("Health")]
        [Tooltip("Starting/Maximum health (TS: 130)")]
        public int maxHealth = 130;

        [Tooltip("Starting shield count")]
        public int startingShields = 0;

        [Tooltip("Maximum shields that can be held")]
        public int maxShields = 3;

        [Header("Power-Up System")]
        [Tooltip("Max weapon power level (TS: 10)")]
        public int maxPowerUpLevel = 10;

        [Tooltip("Damage increase per level (TS: 0.6 = 60%)")]
        public float powerUpDamageMultiplier = 0.6f;

        [Header("Speed System")]
        [Tooltip("Max speed boost level (TS: 20)")]
        public int maxSpeedLevel = 20;

        [Tooltip("Speed increase per level (TS: 0.05 = 5%)")]
        public float speedBoostPerLevel = 0.05f;

        [Header("Invulnerability")]
        [Tooltip("Invulnerability on spawn")]
        public float spawnInvulnerabilityDuration = 2f;

        [Tooltip("Invulnerability after taking damage")]
        public float damageInvulnerabilityDuration = 0.5f;

        [Tooltip("Invulnerability pickup duration (TS: 7.0)")]
        public float invulnerablePickupDuration = 7.0f;

        [Tooltip("Does shield block 1 hit or all damage? (TS: true = 1 hit)")]
        public bool shieldAbsorbsOneHit = true;

        [Header("Arena")]
        [Tooltip("Radius of the play area")]
        public float arenaRadius = 30f;

        [Tooltip("How strongly player is pushed back at boundary")]
        public float boundaryPushStrength = 8f;

        [Header("Collision")]
        [Tooltip("Player collision radius for physics/hit detection")]
        public float collisionRadius = 0.5f;

        [Header("Controls")]
        [Tooltip("Control scheme for player movement and aiming")]
        public ControlScheme controlScheme = ControlScheme.TwinStick;
    }

    /// <summary>
    /// Control scheme options for player movement/rotation behavior
    /// </summary>
    public enum ControlScheme
    {
        [Tooltip("Move with left stick, aim with right stick/mouse. Ship faces where you're aiming. Classic twin-stick shooter.")]
        TwinStick,

        [Tooltip("Ship always faces movement direction. Aim independently with mouse/right stick. No visual strafing.")]
        FaceMovement,

        [Tooltip("Classic Asteroids-style. Ship rotates with input, moves forward in facing direction.")]
        ClassicRotate,

        [Tooltip("Tank controls. Forward/back moves in facing direction, left/right rotates ship.")]
        TankControls
    }

    /// <summary>
    /// Weapon configuration values - matches TypeScript BALANCE_CONFIG.WEAPONS
    /// </summary>
    [System.Serializable]
    public class WeaponConfig
    {
        [Header("Base Stats (Level 0)")]
        [Tooltip("Base bullet damage (TS: 12)")]
        public int baseDamage = 12;

        [Tooltip("Time between shots in seconds (TS: 0.12)")]
        public float baseFireRate = 0.12f;

        [Tooltip("Bullet speed in units/second (TS: 22)")]
        public float projectileSpeed = 22f;

        [Tooltip("Max bullet distance/lifetime (TS: 38 range)")]
        public float projectileLifetime = 1.7f; // ~38 units at speed 22

        [Header("Heat System")]
        [Tooltip("Enable weapon overheating? (TS: true)")]
        public bool heatEnabled = true;

        [Tooltip("Heat added per shot (TS: 0.8)")]
        public float heatPerShot = 0.8f;

        [Tooltip("Heat removed per second (TS: 85)")]
        public float heatCooldownRate = 85f;

        [Tooltip("Max heat before overheat (TS: 100)")]
        public float overheatThreshold = 100f;

        [Tooltip("Forced cooldown time when overheated (TS: 0.8)")]
        public float overheatCooldownDuration = 0.8f;

        [Header("Power Levels")]
        [Tooltip("Maximum power-up level (TS: 10)")]
        public int maxPowerLevel = 10;

        [Tooltip("Damage multiplier per power level (TS: 0.6 = 60%)")]
        public float damagePerLevel = 0.6f;

        [Tooltip("Fire rate improvement per power level")]
        public float fireRatePerLevel = 0.05f;
    }

    /// <summary>
    /// Per-enemy-type configuration - matches TypeScript enemy configs
    /// </summary>
    [System.Serializable]
    public class EnemyTypeConfig
    {
        [Header("Identity")]
        public string displayName = "Enemy";
        public Color color = Color.white;

        [Header("Stats")]
        [Tooltip("Health points")]
        public int health = 1;

        [Tooltip("Movement speed in units/second")]
        public float speed = 1.5f;

        [Tooltip("Damage dealt to player on contact")]
        public int contactDamage = 5;

        [Tooltip("Collision radius for physics/hit detection")]
        public float collisionRadius = 0.42f;

        [Header("Scoring")]
        [Tooltip("Points awarded on kill")]
        public int scoreValue = 100;

        [Tooltip("XP awarded on kill")]
        public int xpValue = 1;

        [Header("Spawning")]
        [Tooltip("Base spawn interval in seconds")]
        public float baseSpawnRate = 2f;

        [Tooltip("Pool size for this enemy type")]
        public int poolSize = 50;

        [Header("Behavior")]
        [Tooltip("Can this enemy shoot projectiles")]
        public bool canShoot = false;

        [Tooltip("Time between shots/bursts if canShoot")]
        public float fireRate = 2f;

        [Tooltip("Projectile speed")]
        public float projectileSpeed = 7f;

        [Tooltip("Projectile damage if canShoot")]
        public int projectileDamage = 10;

        [Tooltip("Number of shots per burst")]
        public int burstCount = 1;

        [Tooltip("Delay between burst shots")]
        public float burstDelay = 0.2f;

        [Tooltip("Detection/aggro range")]
        public float detectionRange = 15f;

        [Tooltip("Patrol radius")]
        public float patrolRange = 10f;

        [Header("Death")]
        [Tooltip("Damage dealt on death (explosion)")]
        public int deathDamage = 0;

        [Tooltip("Radius of death damage")]
        public float deathRadius = 0f;

        [Header("Animation")]
        [Tooltip("Duration of spawn animation")]
        public float spawnDuration = 0.25f;

        [Tooltip("Duration of death animation")]
        public float deathDuration = 0.5f;

        [Header("VFX")]
        [Tooltip("Explosion size on death")]
        public ExplosionSize explosionSize = ExplosionSize.Small;

        [Header("Level Scaling")]
        [Tooltip("Health multiplier per level (TS: 1.025 = 2.5%)")]
        public float healthScalePerLevel = 1.025f;

        [Tooltip("Speed multiplier per level (TS: 1.012 = 1.2%)")]
        public float speedScalePerLevel = 1.012f;

        [Tooltip("Damage multiplier per level (TS: 1.02 = 2%)")]
        public float damageScalePerLevel = 1.02f;
    }

    /// <summary>
    /// Pickup configuration values - matches TypeScript BALANCE_CONFIG.PICKUPS
    /// </summary>
    [System.Serializable]
    public class PickupConfig
    {
        [Header("Identity")]
        public string displayName = "Pickup";
        public Color color = Color.white;

        [Header("Spawning")]
        [Tooltip("Spawns per level")]
        public int spawnsPerLevel = 2;

        [Tooltip("Minimum spawn interval in seconds")]
        public float minSpawnInterval = 20f;

        [Tooltip("Maximum spawn interval in seconds")]
        public float maxSpawnInterval = 40f;

        [Header("Behavior")]
        [Tooltip("Lifetime before despawning")]
        public float lifetime = 15f;

        [Tooltip("Radius at which pickup is attracted to player (TS: 5.0)")]
        public float magnetRadius = 5.0f;

        [Tooltip("Strength of magnetic pull (TS: 16.0)")]
        public float magnetStrength = 16.0f;

        [Tooltip("Max speed when being pulled (TS: 18.0)")]
        public float maxMagnetSpeed = 18.0f;

        [Header("Effect")]
        [Tooltip("Duration of effect (if applicable)")]
        public float effectDuration = 10f;

        [Tooltip("Amount of effect (health restored, speed multiplier, etc)")]
        public float effectAmount = 30f;

        [Header("Spawn Conditions")]
        [Tooltip("Health threshold for spawning (e.g., 0.8 = only spawn if player < 80% health)")]
        public float healthThreshold = 1.0f;
    }

    /// <summary>
    /// Combo/multiplier system configuration - matches TypeScript BALANCE_CONFIG.SCORING
    /// </summary>
    [System.Serializable]
    public class ComboConfig
    {
        [Header("Combo")]
        [Tooltip("Time to maintain combo without kills (TS: 3.0)")]
        public float comboDecayTime = 3.0f;

        [Tooltip("Time window for multiplier increase (TS: 1.5)")]
        public float comboWindow = 1.5f;

        [Header("Multiplier")]
        [Tooltip("Multiplier increase per kill")]
        public float multiplierPerKill = 0.1f;

        [Tooltip("Maximum multiplier (TS: 10)")]
        public float maxMultiplier = 10f;

        [Tooltip("Multiplier decay rate per second (TS: 2.0)")]
        public float multiplierDecayRate = 2.0f;

        [Header("Scoring")]
        [Tooltip("Base points per enemy kill (TS: 100)")]
        public int baseKillPoints = 100;

        [Tooltip("Bonus for completing a level (TS: 1000)")]
        public int levelCompleteBonus = 1000;

        [Tooltip("Boss kills worth X times points (TS: 2.0)")]
        public float bossKillMultiplier = 2.0f;

        [Tooltip("Bonus for no damage taken (TS: 500)")]
        public int perfectLevelBonus = 500;
    }

    /// <summary>
    /// General spawning configuration
    /// </summary>
    [System.Serializable]
    public class SpawnConfig
    {
        [Header("Spawn Area")]
        [Tooltip("Minimum distance from player to spawn")]
        public float minSpawnDistance = 5f;

        [Tooltip("Maximum distance from player to spawn (spawn boundary)")]
        public float maxSpawnDistance = 28f;

        [Tooltip("Pickup spawn boundary")]
        public float pickupSpawnBoundary = 25f;

        [Tooltip("Maximum active enemies")]
        public int maxActiveEnemies = 200;

        [Header("Difficulty Scaling")]
        [Tooltip("Difficulty increase per level (percentage) (TS: 0.03 = 3%)")]
        public float difficultyPerLevel = 0.03f;

        [Tooltip("Spawn rate scale per level (TS: 0.992 = 0.8% faster)")]
        public float spawnRateScalePerLevel = 0.992f;

        [Tooltip("Minimum spawn rate multiplier")]
        public float minSpawnRateMultiplier = 0.3f;
    }

    /// <summary>
    /// Level progression configuration - matches TypeScript BALANCE_CONFIG.LEVELS
    /// </summary>
    [System.Serializable]
    public class LevelConfig
    {
        [Header("Progression")]
        [Tooltip("Total number of levels (TS: 99)")]
        public int totalLevels = 99;

        [Tooltip("Seconds per level (TS: 90 = 1.5 minutes)")]
        public float levelDuration = 90f;

        [Tooltip("Boss spawns at this time in seconds (TS: 70)")]
        public float bossAppearsAt = 70f;

        [Header("Difficulty Scaling Per Level")]
        [Tooltip("Enemy health scale (TS: 1.025 = 2.5% per level, ~10x at level 99)")]
        public float enemyHealthScale = 1.025f;

        [Tooltip("Enemy speed scale (TS: 1.012 = 1.2% per level, ~3x at level 99)")]
        public float enemySpeedScale = 1.012f;

        [Tooltip("Enemy damage scale (TS: 1.02 = 2% per level, ~6x at level 99)")]
        public float enemyDamageScale = 1.02f;

        [Tooltip("Spawn rate scale (TS: 0.992 = 0.8% faster per level)")]
        public float spawnRateScale = 0.992f;
    }

    /// <summary>
    /// World settings - matches TypeScript BALANCE_CONFIG.WORLD
    /// </summary>
    [System.Serializable]
    public class WorldConfig
    {
        [Tooltip("World diameter (TS: 80)")]
        public float worldSize = 80f;

        [Tooltip("Playable area radius (TS: 29)")]
        public float boundaryRadius = 29f;

        [Tooltip("Damage per second outside boundary (TS: 10)")]
        public float boundaryDamage = 10f;

        [Tooltip("Enemy spawn radius (TS: 28)")]
        public float spawnBoundary = 28f;

        [Tooltip("Pickup spawn radius (TS: 25)")]
        public float pickupSpawnBoundary = 25f;

        [Tooltip("Min distance from player for spawns (TS: 5)")]
        public float minPlayerDistance = 5f;
    }

    /// <summary>
    /// Visual & Audio feedback settings - matches TypeScript BALANCE_CONFIG.FEEDBACK
    /// </summary>
    [System.Serializable]
    public class FeedbackConfig
    {
        [Header("Screen Shake")]
        public ShakeSettings smallShake = new ShakeSettings { intensity = 0.2f, duration = 0.1f };
        public ShakeSettings mediumShake = new ShakeSettings { intensity = 0.5f, duration = 0.3f };
        public ShakeSettings largeShake = new ShakeSettings { intensity = 1.0f, duration = 0.5f };

        [Header("Camera Zoom")]
        [Tooltip("Zoomed in - tight view (default: 6)")]
        public float minZoom = 6f;

        [Tooltip("Zoomed out for many enemies (default: 22)")]
        public float maxZoom = 22f;

        [Tooltip("How fast zoom changes (TS: 3.0)")]
        public float zoomSpeed = 3.0f;

        [Tooltip("Base/default camera size (default: 8)")]
        public float baseZoom = 8f;

        [Header("Particle Effects")]
        [Tooltip("Multiplier for particle count (TS: 1.0)")]
        public float particleDensity = 1.0f;

        [Tooltip("Enemy trails on/off (TS: true)")]
        public bool trailEnabled = true;

        [Tooltip("Explosion size multiplier (TS: 1.0)")]
        public float explosionScale = 1.0f;
    }

    [System.Serializable]
    public class ShakeSettings
    {
        public float intensity = 0.5f;
        public float duration = 0.3f;
    }

    /// <summary>
    /// Explosion size categories (used for VFX)
    /// </summary>
    public enum ExplosionSize
    {
        Small,
        Medium,
        Large,
        Boss
    }
}
