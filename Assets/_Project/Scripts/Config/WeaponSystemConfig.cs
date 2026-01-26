using UnityEngine;

namespace NeuralBreak.Config
{
    /// <summary>
    /// Comprehensive weapon system configuration.
    /// Supports multiple fire patterns, modifiers, and future weapon behaviors.
    /// 
    /// WEAPON ARCHITECTURE:
    /// - Forward Weapons: Single, Double, Triple, Quad, X5 (with spread)
    /// - Rear Weapons: Optional backward fire
    /// - Modifiers: Rate, damage, speed multipliers from power-ups
    /// - Specials: Future weapon behaviors (homing, piercing, etc.)
    /// </summary>
    [System.Serializable]
    public class WeaponSystemConfig
    {
        [Header("=== BASE WEAPON STATS ===")]
        [Tooltip("Base bullet damage before modifiers")]
        public int baseDamage = 12;

        [Tooltip("Base time between shots in seconds")]
        public float baseFireRate = 0.12f;

        [Tooltip("Base bullet speed in units/second")]
        public float baseProjectileSpeed = 22f;

        [Tooltip("Bullet lifetime in seconds")]
        public float projectileLifetime = 1.7f;

        [Tooltip("Visual size of projectiles")]
        public float projectileSize = 0.15f;

        [Header("=== FORWARD WEAPON PATTERNS ===")]
        public ForwardWeaponConfig forwardWeapon;

        [Header("=== REAR WEAPON ===")]
        public RearWeaponConfig rearWeapon;

        [Header("=== HEAT SYSTEM ===")]
        public HeatSystemConfig heatSystem;

        [Header("=== POWER LEVEL SCALING ===")]
        public PowerLevelConfig powerLevels;

        [Header("=== WEAPON MODIFIERS (from pickups) ===")]
        public WeaponModifiersConfig modifiers;

        [Header("=== SPECIAL WEAPONS (future-proof) ===")]
        public SpecialWeaponsConfig specials;
    }

    /// <summary>
    /// Forward weapon configuration - supports Single to X5 fire patterns
    /// </summary>
    [System.Serializable]
    public class ForwardWeaponConfig
    {
        [Header("Fire Pattern")]
        [Tooltip("Current forward fire pattern")]
        public ForwardFirePattern pattern = ForwardFirePattern.Single;

        [Header("Pattern Settings")]
        [Tooltip("Spread angle for Double fire (degrees)")]
        [Range(5f, 45f)]
        public float doubleSpreadAngle = 15f;

        [Tooltip("Spread angle for Triple fire (degrees)")]
        [Range(10f, 60f)]
        public float tripleSpreadAngle = 30f;

        [Tooltip("Spread angle for Quad fire (degrees)")]
        [Range(15f, 90f)]
        public float quadSpreadAngle = 45f;

        [Tooltip("Spread angle for X5 fire (degrees)")]
        [Range(20f, 120f)]
        public float x5SpreadAngle = 60f;

        [Header("Spawn Offsets")]
        [Tooltip("How far forward from player center to spawn projectiles")]
        public float forwardOffset = 0.6f;

        [Tooltip("Lateral offset for side-by-side patterns (Double, Quad)")]
        public float lateralOffset = 0.2f;

        /// <summary>
        /// Get the number of projectiles for current pattern
        /// </summary>
        public int GetProjectileCount()
        {
            return pattern switch
            {
                ForwardFirePattern.Single => 1,
                ForwardFirePattern.Double => 2,
                ForwardFirePattern.Triple => 3,
                ForwardFirePattern.Quad => 4,
                ForwardFirePattern.X5 => 5,
                _ => 1
            };
        }

        /// <summary>
        /// Get spread angle for current pattern
        /// </summary>
        public float GetSpreadAngle()
        {
            return pattern switch
            {
                ForwardFirePattern.Single => 0f,
                ForwardFirePattern.Double => doubleSpreadAngle,
                ForwardFirePattern.Triple => tripleSpreadAngle,
                ForwardFirePattern.Quad => quadSpreadAngle,
                ForwardFirePattern.X5 => x5SpreadAngle,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Forward fire patterns
    /// </summary>
    public enum ForwardFirePattern
    {
        Single,     // 1 projectile, straight ahead
        Double,     // 2 projectiles, slight spread
        Triple,     // 3 projectiles, center + spread
        Quad,       // 4 projectiles, wider spread
        X5          // 5 projectiles, maximum spread
    }

    /// <summary>
    /// Rear weapon configuration - optional backward fire
    /// </summary>
    [System.Serializable]
    public class RearWeaponConfig
    {
        [Header("Rear Fire")]
        [Tooltip("Enable rear-facing weapon")]
        public bool enabled = false;

        [Tooltip("Damage multiplier for rear weapon (1.0 = same as forward)")]
        [Range(0.25f, 2f)]
        public float damageMultiplier = 0.5f;

        [Tooltip("Fire rate multiplier for rear weapon (1.0 = same as forward)")]
        [Range(0.25f, 2f)]
        public float fireRateMultiplier = 1.0f;

        [Tooltip("How far back from player center to spawn rear projectiles")]
        public float rearOffset = 0.4f;

        [Tooltip("Fires on same trigger as forward, or separate timing")]
        public bool syncWithForward = true;

        [Tooltip("If not synced, independent fire rate")]
        public float independentFireRate = 0.3f;
    }

    /// <summary>
    /// Heat system configuration
    /// </summary>
    [System.Serializable]
    public class HeatSystemConfig
    {
        [Header("Heat Mechanics")]
        [Tooltip("Enable weapon overheating")]
        public bool enabled = true;

        [Tooltip("Heat added per shot")]
        public float heatPerShot = 0.8f;

        [Tooltip("Heat removed per second when not firing")]
        public float cooldownRate = 85f;

        [Tooltip("Maximum heat before overheat")]
        public float maxHeat = 100f;

        [Tooltip("Forced cooldown duration when overheated")]
        public float overheatDuration = 0.8f;

        [Tooltip("Cooling rate multiplier when overheated")]
        public float overheatCooldownMultiplier = 1.5f;

        [Header("Heat Modifiers")]
        [Tooltip("Heat multiplier for multi-shot patterns (per extra projectile)")]
        public float multiShotHeatMultiplier = 0.3f;

        [Tooltip("Heat multiplier for rear weapon")]
        public float rearWeaponHeatMultiplier = 0.5f;
    }

    /// <summary>
    /// Power level scaling configuration
    /// </summary>
    [System.Serializable]
    public class PowerLevelConfig
    {
        [Header("Level Limits")]
        [Tooltip("Maximum power level achievable")]
        public int maxLevel = 10;

        [Header("Pattern Mode")]
        [Tooltip("If true, pattern upgrades automatically based on power level. If false, use manual pattern from ForwardWeaponConfig.")]
        public bool autoUpgradePattern = true;

        [Header("Per-Level Bonuses")]
        [Tooltip("Damage increase per level (0.1 = 10% per level)")]
        public float damagePerLevel = 0.1f;

        [Tooltip("Fire rate improvement per level (0.005 = 0.5% faster per level)")]
        public float fireRatePerLevel = 0.005f;

        [Tooltip("Projectile speed increase per level")]
        public float projectileSpeedPerLevel = 0.5f;

        [Tooltip("Projectile size increase per level")]
        public float projectileSizePerLevel = 0.01f;

        [Header("Pattern Upgrades")]
        [Tooltip("Power level required for Double fire (0 = start with it)")]
        public int doubleShotLevel = 0;

        [Tooltip("Power level required for Triple fire")]
        public int tripleShotLevel = 3;

        [Tooltip("Power level required for Quad fire")]
        public int quadShotLevel = 6;

        [Tooltip("Power level required for X5 fire")]
        public int x5ShotLevel = 9;

        /// <summary>
        /// Get the fire pattern for a given power level
        /// </summary>
        public ForwardFirePattern GetPatternForLevel(int level)
        {
            if (level >= x5ShotLevel) return ForwardFirePattern.X5;
            if (level >= quadShotLevel) return ForwardFirePattern.Quad;
            if (level >= tripleShotLevel) return ForwardFirePattern.Triple;
            if (level >= doubleShotLevel) return ForwardFirePattern.Double;
            return ForwardFirePattern.Single;
        }
    }

    /// <summary>
    /// Weapon modifiers from pickups and power-ups
    /// </summary>
    [System.Serializable]
    public class WeaponModifiersConfig
    {
        [Header("Rapid Fire Modifier")]
        [Tooltip("Fire rate multiplier when rapid fire is active")]
        public float rapidFireMultiplier = 1.5f;

        [Tooltip("Duration of rapid fire power-up")]
        public float rapidFireDuration = 10f;

        [Header("Damage Boost Modifier")]
        [Tooltip("Damage multiplier when damage boost is active")]
        public float damageBoostMultiplier = 2f;

        [Tooltip("Duration of damage boost power-up")]
        public float damageBoostDuration = 8f;

        [Header("Projectile Modifiers")]
        [Tooltip("Projectile speed multiplier from power-ups")]
        public float speedBoostMultiplier = 1.5f;

        [Tooltip("Projectile size multiplier from power-ups")]
        public float sizeBoostMultiplier = 1.5f;
    }

    /// <summary>
    /// Special weapon behaviors - future-proof for new weapon types
    /// </summary>
    [System.Serializable]
    public class SpecialWeaponsConfig
    {
        [Header("Piercing")]
        [Tooltip("Projectiles pass through enemies")]
        public bool piercingEnabled = false;

        [Tooltip("Max enemies a piercing shot can hit")]
        public int maxPierceCount = 5;

        [Tooltip("Damage reduction per pierce (0.1 = 10% less each hit)")]
        public float pierceDamageReduction = 0.1f;

        [Header("Homing")]
        [Tooltip("Projectiles seek enemies")]
        public bool homingEnabled = false;

        [Tooltip("Homing detection range")]
        public float homingRange = 8f;

        [Tooltip("Homing turn strength (higher = tighter turns)")]
        public float homingStrength = 5f;

        [Header("Explosive")]
        [Tooltip("Projectiles explode on impact")]
        public bool explosiveEnabled = false;

        [Tooltip("Explosion radius")]
        public float explosionRadius = 2f;

        [Tooltip("Explosion damage (multiplier of projectile damage)")]
        public float explosionDamageMultiplier = 0.5f;

        [Header("Ricochet")]
        [Tooltip("Projectiles bounce off arena walls")]
        public bool ricochetEnabled = false;

        [Tooltip("Max bounces before projectile expires")]
        public int maxBounces = 3;

        [Header("Chain Lightning")]
        [Tooltip("Damage chains to nearby enemies")]
        public bool chainLightningEnabled = false;

        [Tooltip("Chain jump range")]
        public float chainRange = 4f;

        [Tooltip("Max chain jumps")]
        public int maxChainJumps = 3;

        [Tooltip("Damage reduction per chain (0.3 = 30% less each jump)")]
        public float chainDamageReduction = 0.3f;

        [Header("Beam Weapon (Future)")]
        [Tooltip("Continuous beam instead of projectiles")]
        public bool beamEnabled = false;

        [Tooltip("Beam damage per second")]
        public float beamDPS = 50f;

        [Tooltip("Beam max range")]
        public float beamRange = 15f;
    }
}
