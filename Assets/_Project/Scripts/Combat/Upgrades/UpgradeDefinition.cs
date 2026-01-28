using UnityEngine;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Defines a single weapon upgrade with effects, requirements, and metadata.
    /// ScriptableObject pattern allows for data-driven upgrade design.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "Neural Break/Upgrades/Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string upgradeId;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public Color iconColor = Color.white;

        [Header("Categorization")]
        public UpgradeCategory category;
        public UpgradeTier tier;
        public bool isPermanent = true;

        [Header("Effects")]
        public WeaponModifiers modifiers;

        [Header("Requirements & Restrictions")]
        public string[] prerequisiteIds;
        public string[] incompatibleIds;
        public int minPlayerLevel = 1;

        [Header("Balance")]
        [Tooltip("Weight for random selection (higher = more likely)")]
        public float spawnWeight = 1f;
        [Tooltip("Maximum times this upgrade can be selected (1 = unique)")]
        public int maxStacks = 1;

        /// <summary>
        /// Check if this upgrade can be selected given current game state.
        /// </summary>
        public bool IsEligible(int playerLevel, System.Collections.Generic.List<UpgradeDefinition> activeUpgrades)
        {
            // Level gate
            if (playerLevel < minPlayerLevel) return false;

            // Check stack limit
            int currentStacks = 0;
            foreach (var upgrade in activeUpgrades)
            {
                if (upgrade.upgradeId == upgradeId)
                {
                    currentStacks++;
                }
            }
            if (currentStacks >= maxStacks) return false;

            // Check prerequisites
            if (prerequisiteIds != null && prerequisiteIds.Length > 0)
            {
                foreach (var prereqId in prerequisiteIds)
                {
                    bool hasPrereq = false;
                    foreach (var upgrade in activeUpgrades)
                    {
                        if (upgrade.upgradeId == prereqId)
                        {
                            hasPrereq = true;
                            break;
                        }
                    }
                    if (!hasPrereq) return false;
                }
            }

            // Check incompatibilities
            if (incompatibleIds != null && incompatibleIds.Length > 0)
            {
                foreach (var incompatId in incompatibleIds)
                {
                    foreach (var upgrade in activeUpgrades)
                    {
                        if (upgrade.upgradeId == incompatId)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Upgrade categories for organization.
    /// </summary>
    public enum UpgradeCategory
    {
        FireRate,
        Damage,
        Special,
        ProjectileType,
        Utility
    }

    /// <summary>
    /// Rarity tiers affecting spawn rates and visual presentation.
    /// </summary>
    public enum UpgradeTier
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Weapon modifiers that stack additively/multiplicatively.
    /// </summary>
    [System.Serializable]
    public struct WeaponModifiers
    {
        [Header("Multiplicative Modifiers (1.0 = no change)")]
        [Tooltip("Fire rate multiplier (1.5 = 50% faster)")]
        public float fireRateMultiplier;
        [Tooltip("Damage multiplier (2.0 = double damage)")]
        public float damageMultiplier;
        [Tooltip("Projectile speed multiplier")]
        public float projectileSpeedMultiplier;
        [Tooltip("Projectile size multiplier")]
        public float projectileSizeMultiplier;

        [Header("Additive Modifiers")]
        [Tooltip("Additional projectiles per shot")]
        public int additionalProjectiles;
        [Tooltip("Additional spread angle in degrees")]
        public float spreadAngleAdd;
        [Tooltip("Number of enemies to pierce through")]
        public int piercingCount;
        [Tooltip("Critical hit chance (0-1)")]
        public float criticalChance;
        [Tooltip("Critical hit damage multiplier")]
        public float criticalMultiplier;

        [Header("Special Behaviors (Flags)")]
        public bool enableHoming;
        public bool enableRearFire;
        public bool enableExplosion;
        public bool enableRicochet;
        public bool enableChainLightning;
        public bool enableBeamWeapon;

        [Header("Special Behavior Parameters")]
        public float homingStrength;
        public float explosionRadius;
        public int ricochetCount;
        public int chainLightningTargets;
        public float beamDuration;

        [Header("Player Bonuses")]
        [Tooltip("Additional shields (added to max shields)")]
        public int bonusShields;
        [Tooltip("Additional smart bombs (added to max bombs)")]
        public int bonusSmartBombs;
        [Tooltip("Additional max health")]
        public int bonusHealth;

        /// <summary>
        /// Identity modifier (no changes).
        /// </summary>
        public static WeaponModifiers Identity => new WeaponModifiers
        {
            fireRateMultiplier = 1f,
            damageMultiplier = 1f,
            projectileSpeedMultiplier = 1f,
            projectileSizeMultiplier = 1f,
            additionalProjectiles = 0,
            spreadAngleAdd = 0f,
            piercingCount = 0,
            criticalChance = 0f,
            criticalMultiplier = 1f,
            enableHoming = false,
            enableRearFire = false,
            enableExplosion = false,
            enableRicochet = false,
            enableChainLightning = false,
            enableBeamWeapon = false,
            homingStrength = 0f,
            explosionRadius = 0f,
            ricochetCount = 0,
            chainLightningTargets = 0,
            beamDuration = 0f,
            bonusShields = 0,
            bonusSmartBombs = 0,
            bonusHealth = 0
        };

        /// <summary>
        /// Combine two modifiers additively/multiplicatively.
        /// </summary>
        public static WeaponModifiers Combine(WeaponModifiers a, WeaponModifiers b)
        {
            return new WeaponModifiers
            {
                // Multiplicative: multiply together
                fireRateMultiplier = a.fireRateMultiplier * b.fireRateMultiplier,
                damageMultiplier = a.damageMultiplier * b.damageMultiplier,
                projectileSpeedMultiplier = a.projectileSpeedMultiplier * b.projectileSpeedMultiplier,
                projectileSizeMultiplier = a.projectileSizeMultiplier * b.projectileSizeMultiplier,

                // Additive: sum
                additionalProjectiles = a.additionalProjectiles + b.additionalProjectiles,
                spreadAngleAdd = a.spreadAngleAdd + b.spreadAngleAdd,
                piercingCount = a.piercingCount + b.piercingCount,
                criticalChance = a.criticalChance + b.criticalChance,
                criticalMultiplier = Mathf.Max(a.criticalMultiplier, b.criticalMultiplier),

                // Flags: OR together
                enableHoming = a.enableHoming || b.enableHoming,
                enableRearFire = a.enableRearFire || b.enableRearFire,
                enableExplosion = a.enableExplosion || b.enableExplosion,
                enableRicochet = a.enableRicochet || b.enableRicochet,
                enableChainLightning = a.enableChainLightning || b.enableChainLightning,
                enableBeamWeapon = a.enableBeamWeapon || b.enableBeamWeapon,

                // Parameters: take max
                homingStrength = Mathf.Max(a.homingStrength, b.homingStrength),
                explosionRadius = Mathf.Max(a.explosionRadius, b.explosionRadius),
                ricochetCount = Mathf.Max(a.ricochetCount, b.ricochetCount),
                chainLightningTargets = Mathf.Max(a.chainLightningTargets, b.chainLightningTargets),
                beamDuration = Mathf.Max(a.beamDuration, b.beamDuration),

                // Player bonuses: sum
                bonusShields = a.bonusShields + b.bonusShields,
                bonusSmartBombs = a.bonusSmartBombs + b.bonusSmartBombs,
                bonusHealth = a.bonusHealth + b.bonusHealth
            };
        }
    }
}
