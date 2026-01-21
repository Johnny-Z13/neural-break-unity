using UnityEngine;

namespace NeuralBreak.Config
{
    /// <summary>
    /// Master configuration asset containing all game balance values.
    /// This is the Unity equivalent of balance.config.ts.
    /// Designers can tweak all gameplay values without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Neural Break/Config/Game Balance")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Configuration")]
        public PlayerConfig player;

        [Header("Weapon Configuration")]
        public WeaponConfig weapon;

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
    }

    /// <summary>
    /// Player configuration values
    /// </summary>
    [System.Serializable]
    public class PlayerConfig
    {
        [Header("Movement")]
        [Tooltip("Base movement speed in units/second")]
        public float baseSpeed = 7f;

        [Tooltip("Acceleration rate")]
        public float acceleration = 25f;

        [Tooltip("Deceleration rate when no input")]
        public float deceleration = 20f;

        [Header("Dash")]
        [Tooltip("Dash velocity in units/second")]
        public float dashSpeed = 32f;

        [Tooltip("Duration of dash in seconds")]
        public float dashDuration = 0.15f;

        [Tooltip("Cooldown between dashes in seconds")]
        public float dashCooldown = 2.5f;

        [Header("Thrust")]
        [Tooltip("Speed multiplier when thrusting (1.0 = no boost)")]
        public float thrustSpeedMultiplier = 1.6f;

        [Tooltip("Time to reach full thrust speed")]
        public float thrustAccelerationTime = 0.15f;

        [Tooltip("Time to return to normal speed after releasing thrust")]
        public float thrustDecelerationTime = 0.25f;

        [Header("Health")]
        [Tooltip("Maximum health points")]
        public int maxHealth = 130;

        [Tooltip("Starting shield count")]
        public int startingShields = 0;

        [Tooltip("Maximum shields that can be held")]
        public int maxShields = 3;

        [Header("Invulnerability")]
        [Tooltip("Invulnerability duration on spawn")]
        public float spawnInvulnerabilityDuration = 2f;

        [Tooltip("Invulnerability duration after taking damage")]
        public float damageInvulnerabilityDuration = 0.5f;

        [Tooltip("Invulnerability pickup duration")]
        public float invulnerablePickupDuration = 7f;

        [Header("Arena")]
        [Tooltip("Radius of the play area")]
        public float arenaRadius = 30f; // 20% larger than original 25f

        [Tooltip("How strongly player is pushed back at boundary")]
        public float boundaryPushStrength = 8f;

        [Header("Collision")]
        [Tooltip("Player collision radius for physics/hit detection")]
        public float collisionRadius = 0.5f;
    }

    /// <summary>
    /// Weapon configuration values
    /// </summary>
    [System.Serializable]
    public class WeaponConfig
    {
        [Header("Base Stats")]
        [Tooltip("Base damage per shot")]
        public int baseDamage = 12;

        [Tooltip("Time between shots in seconds")]
        public float baseFireRate = 0.12f;

        [Tooltip("Projectile speed in units/second")]
        public float projectileSpeed = 22f;

        [Tooltip("Projectile lifetime in seconds")]
        public float projectileLifetime = 2f;

        [Header("Heat System")]
        [Tooltip("Heat generated per shot (0-100 scale)")]
        public float heatPerShot = 0.8f;

        [Tooltip("Heat cooldown rate per second")]
        public float heatCooldownRate = 15f;

        [Tooltip("Heat threshold that triggers overheat")]
        public float overheatThreshold = 100f;

        [Tooltip("Cooldown duration when overheated")]
        public float overheatCooldownDuration = 1.5f;

        [Header("Power Levels")]
        [Tooltip("Maximum power-up level")]
        public int maxPowerLevel = 10;

        [Tooltip("Damage multiplier per power level")]
        public float damagePerLevel = 0.15f;

        [Tooltip("Fire rate improvement per power level")]
        public float fireRatePerLevel = 0.05f;
    }

    /// <summary>
    /// Per-enemy-type configuration
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
        public float collisionRadius = 0.5f;

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

        [Tooltip("Time between shots if canShoot")]
        public float fireRate = 2f;

        [Tooltip("Projectile damage if canShoot")]
        public int projectileDamage = 10;

        [Header("Animation")]
        [Tooltip("Duration of spawn animation")]
        public float spawnDuration = 0.25f;

        [Tooltip("Duration of death animation")]
        public float deathDuration = 0.5f;

        [Header("VFX")]
        [Tooltip("Explosion size on death")]
        public ExplosionSize explosionSize = ExplosionSize.Small;
    }

    /// <summary>
    /// Pickup configuration values
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

        [Tooltip("Minimum spawn interval")]
        public float minSpawnInterval = 20f;

        [Tooltip("Maximum spawn interval")]
        public float maxSpawnInterval = 40f;

        [Header("Behavior")]
        [Tooltip("Lifetime before despawning")]
        public float lifetime = 15f;

        [Tooltip("Radius at which pickup is attracted to player")]
        public float magnetRadius = 5f;

        [Tooltip("Strength of magnetic pull")]
        public float magnetStrength = 16f;

        [Header("Effect")]
        [Tooltip("Duration of effect (if applicable)")]
        public float effectDuration = 10f;

        [Tooltip("Amount of effect (health restored, speed multiplier, etc)")]
        public float effectAmount = 30f;
    }

    /// <summary>
    /// Combo/multiplier system configuration
    /// </summary>
    [System.Serializable]
    public class ComboConfig
    {
        [Header("Combo")]
        [Tooltip("Time before combo resets without kills")]
        public float comboDecayTime = 1.5f;

        [Tooltip("Time window to chain kills")]
        public float comboWindow = 2f;

        [Header("Multiplier")]
        [Tooltip("Multiplier increase per kill")]
        public float multiplierPerKill = 0.1f;

        [Tooltip("Maximum multiplier")]
        public float maxMultiplier = 10f;

        [Tooltip("Multiplier decay rate per second")]
        public float multiplierDecayRate = 0.5f;
    }

    /// <summary>
    /// General spawning configuration
    /// </summary>
    [System.Serializable]
    public class SpawnConfig
    {
        [Header("Spawn Area")]
        [Tooltip("Minimum distance from player to spawn")]
        public float minSpawnDistance = 10f; // Increased for larger arena

        [Tooltip("Maximum distance from player to spawn")]
        public float maxSpawnDistance = 24f; // Increased for larger arena

        [Tooltip("Maximum active enemies")]
        public int maxActiveEnemies = 200;

        [Header("Difficulty Scaling")]
        [Tooltip("Difficulty increase per level (percentage)")]
        public float difficultyPerLevel = 0.03f;

        [Tooltip("Minimum spawn rate multiplier")]
        public float minSpawnRateMultiplier = 0.3f;
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
