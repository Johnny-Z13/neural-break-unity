using UnityEngine;
using UnityEditor;
using NeuralBreak.Combat;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor utility to create upgrade definition assets programmatically.
    /// </summary>
    public static class UpgradeCreator
    {
        [MenuItem("Neural Break/Create Upgrades/Create Starter Pack")]
        public static void CreateStarterPack()
        {
            CreateFireRateUpgrades();
            CreateDamageUpgrades();
            CreateProjectileUpgrades();
            CreateSpecialUpgrades();
            CreateUtilityUpgrades();
            CreateHybridUpgrades();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UpgradeCreator] Created starter pack of upgrades");
        }

        private static void CreateFireRateUpgrades()
        {
            var rapidFire1 = WeaponModifiers.Identity;
            rapidFire1.fireRateMultiplier = 1.25f;
            CreateUpgrade(
                "rapid_fire_1",
                "Rapid Fire I",
                "+25% fire rate",
                "Upgrades/FireRate/RapidFire1",
                UpgradeCategory.FireRate,
                UpgradeTier.Common,
                rapidFire1,
                maxStacks: 3
            );

            var rapidFire2 = WeaponModifiers.Identity;
            rapidFire2.fireRateMultiplier = 1.5f;
            CreateUpgrade(
                "rapid_fire_2",
                "Rapid Fire II",
                "+50% fire rate",
                "Upgrades/FireRate/RapidFire2",
                UpgradeCategory.FireRate,
                UpgradeTier.Rare,
                rapidFire2,
                maxStacks: 2,
                prerequisites: new[] { "rapid_fire_1" }
            );

            var rapidFire3 = WeaponModifiers.Identity;
            rapidFire3.fireRateMultiplier = 2.0f;
            CreateUpgrade(
                "rapid_fire_3",
                "Rapid Fire III",
                "+100% fire rate (double speed!)",
                "Upgrades/FireRate/RapidFire3",
                UpgradeCategory.FireRate,
                UpgradeTier.Epic,
                rapidFire3,
                maxStacks: 1,
                prerequisites: new[] { "rapid_fire_2" }
            );
        }

        private static void CreateDamageUpgrades()
        {
            var heavyRounds = WeaponModifiers.Identity;
            heavyRounds.damageMultiplier = 1.3f;
            CreateUpgrade(
                "heavy_rounds",
                "Heavy Rounds",
                "+30% damage",
                "Upgrades/Damage/HeavyRounds",
                UpgradeCategory.Damage,
                UpgradeTier.Common,
                heavyRounds,
                maxStacks: 3
            );

            var armorPiercing = WeaponModifiers.Identity;
            armorPiercing.damageMultiplier = 1.5f;
            CreateUpgrade(
                "armor_piercing",
                "Armor Piercing",
                "+50% damage",
                "Upgrades/Damage/ArmorPiercing",
                UpgradeCategory.Damage,
                UpgradeTier.Rare,
                armorPiercing,
                maxStacks: 2
            );

            var highExplosive = WeaponModifiers.Identity;
            highExplosive.damageMultiplier = 2.0f;
            CreateUpgrade(
                "high_explosive",
                "High Explosive",
                "+100% damage (double damage!)",
                "Upgrades/Damage/HighExplosive",
                UpgradeCategory.Damage,
                UpgradeTier.Epic,
                highExplosive,
                maxStacks: 1
            );

            var criticalHits = WeaponModifiers.Identity;
            criticalHits.criticalChance = 0.2f;
            criticalHits.criticalMultiplier = 2.0f;
            CreateUpgrade(
                "critical_hits",
                "Critical Hits",
                "20% chance for 2x damage",
                "Upgrades/Damage/CriticalHits",
                UpgradeCategory.Damage,
                UpgradeTier.Rare,
                criticalHits,
                maxStacks: 2
            );
        }

        private static void CreateProjectileUpgrades()
        {
            var doubleShot = WeaponModifiers.Identity;
            doubleShot.additionalProjectiles = 1;
            CreateUpgrade(
                "double_shot",
                "Double Shot",
                "Fire 2 projectiles at once",
                "Upgrades/Special/DoubleShot",
                UpgradeCategory.ProjectileType,
                UpgradeTier.Common,
                doubleShot,
                maxStacks: 1
            );

            var tripleShot = WeaponModifiers.Identity;
            tripleShot.additionalProjectiles = 2;
            CreateUpgrade(
                "triple_shot",
                "Triple Shot",
                "Fire 3 projectiles at once",
                "Upgrades/Special/TripleShot",
                UpgradeCategory.ProjectileType,
                UpgradeTier.Rare,
                tripleShot,
                maxStacks: 1,
                prerequisites: new[] { "double_shot" }
            );

            var wideSpread = WeaponModifiers.Identity;
            wideSpread.spreadAngleAdd = 30f;
            CreateUpgrade(
                "wide_spread",
                "Wide Spread",
                "+30° spread angle for better coverage",
                "Upgrades/Special/WideSpread",
                UpgradeCategory.ProjectileType,
                UpgradeTier.Common,
                wideSpread,
                maxStacks: 2
            );
        }

        private static void CreateSpecialUpgrades()
        {
            var homingMissiles = WeaponModifiers.Identity;
            homingMissiles.enableHoming = true;
            homingMissiles.homingStrength = 5.0f;
            CreateUpgrade(
                "homing_missiles",
                "Homing Missiles",
                "Projectiles track nearby enemies",
                "Upgrades/Special/HomingMissiles",
                UpgradeCategory.Special,
                UpgradeTier.Rare,
                homingMissiles,
                maxStacks: 1
            );

            var piercingShot = WeaponModifiers.Identity;
            piercingShot.piercingCount = 3;
            CreateUpgrade(
                "piercing_shot",
                "Piercing Shot",
                "Pierce through 3 enemies",
                "Upgrades/Special/PiercingShot",
                UpgradeCategory.Special,
                UpgradeTier.Rare,
                piercingShot,
                maxStacks: 2
            );

            var explosiveRounds = WeaponModifiers.Identity;
            explosiveRounds.enableExplosion = true;
            explosiveRounds.explosionRadius = 2.0f;
            CreateUpgrade(
                "explosive_rounds",
                "Explosive Rounds",
                "Explode on hit (2.0 radius AOE)",
                "Upgrades/Special/ExplosiveRounds",
                UpgradeCategory.Special,
                UpgradeTier.Epic,
                explosiveRounds,
                maxStacks: 1
            );

            var rearGuns = WeaponModifiers.Identity;
            rearGuns.enableRearFire = true;
            CreateUpgrade(
                "rear_guns",
                "Rear Guns",
                "Fire backward for 360° coverage",
                "Upgrades/Special/RearGuns",
                UpgradeCategory.Special,
                UpgradeTier.Common,
                rearGuns,
                maxStacks: 1
            );
        }

        private static void CreateUtilityUpgrades()
        {
            var largeProjectiles = WeaponModifiers.Identity;
            largeProjectiles.projectileSizeMultiplier = 1.5f;
            CreateUpgrade(
                "large_projectiles",
                "Large Projectiles",
                "+50% projectile size (easier to hit)",
                "Upgrades/Utility/LargeProjectiles",
                UpgradeCategory.Utility,
                UpgradeTier.Common,
                largeProjectiles,
                maxStacks: 2
            );

            var fastBullets = WeaponModifiers.Identity;
            fastBullets.projectileSpeedMultiplier = 1.5f;
            CreateUpgrade(
                "fast_bullets",
                "Fast Bullets",
                "+50% projectile speed",
                "Upgrades/Utility/FastBullets",
                UpgradeCategory.Utility,
                UpgradeTier.Common,
                fastBullets,
                maxStacks: 2
            );
        }

        private static void CreateHybridUpgrades()
        {
            // Glass Cannon - High risk, high reward
            var glassCannon = WeaponModifiers.Identity;
            glassCannon.fireRateMultiplier = 2.5f;
            glassCannon.damageMultiplier = 0.8f;
            CreateUpgrade(
                "glass_cannon",
                "Glass Cannon",
                "+150% fire rate, -20% damage",
                "Upgrades/Special/GlassCannon",
                UpgradeCategory.FireRate,
                UpgradeTier.Legendary,
                glassCannon,
                maxStacks: 1
            );

            // Sniper Mode - Focused power
            var sniperMode = WeaponModifiers.Identity;
            sniperMode.damageMultiplier = 1.8f;
            sniperMode.fireRateMultiplier = 0.7f;
            sniperMode.spreadAngleAdd = -15f;
            CreateUpgrade(
                "sniper_mode",
                "Sniper Mode",
                "+80% damage, -30% fire rate, tighter spread",
                "Upgrades/Special/SniperMode",
                UpgradeCategory.Damage,
                UpgradeTier.Rare,
                sniperMode,
                maxStacks: 1
            );

            // Shotgun Blast
            var shotgunBlast = WeaponModifiers.Identity;
            shotgunBlast.additionalProjectiles = 4;
            shotgunBlast.spreadAngleAdd = 45f;
            shotgunBlast.damageMultiplier = 0.7f;
            CreateUpgrade(
                "shotgun_blast",
                "Shotgun Blast",
                "Fire 5 projectiles in wide spread, -30% damage per shot",
                "Upgrades/Special/ShotgunBlast",
                UpgradeCategory.ProjectileType,
                UpgradeTier.Epic,
                shotgunBlast,
                maxStacks: 1
            );

            // Chain Lightning
            var chainLightning = WeaponModifiers.Identity;
            chainLightning.enableChainLightning = true;
            chainLightning.chainLightningTargets = 4;
            CreateUpgrade(
                "chain_lightning",
                "Chain Lightning",
                "Projectiles jump to 4 nearby enemies",
                "Upgrades/Special/ChainLightning",
                UpgradeCategory.Special,
                UpgradeTier.Legendary,
                chainLightning,
                maxStacks: 1
            );

            // Ricochet
            var ricochet = WeaponModifiers.Identity;
            ricochet.enableRicochet = true;
            ricochet.ricochetCount = 3;
            CreateUpgrade(
                "ricochet",
                "Ricochet",
                "Projectiles bounce 3 times",
                "Upgrades/Special/Ricochet",
                UpgradeCategory.Special,
                UpgradeTier.Epic,
                ricochet,
                maxStacks: 1
            );

            // Beam Weapon
            var beamWeapon = WeaponModifiers.Identity;
            beamWeapon.enableBeamWeapon = true;
            beamWeapon.beamDuration = 0.5f;
            beamWeapon.damageMultiplier = 1.3f;
            CreateUpgrade(
                "beam_weapon",
                "Beam Weapon",
                "Continuous damage beam (+30% damage)",
                "Upgrades/Special/BeamWeapon",
                UpgradeCategory.Special,
                UpgradeTier.Legendary,
                beamWeapon,
                maxStacks: 1
            );
        }

        private static void CreateUpgrade(
            string id,
            string displayName,
            string description,
            string path,
            UpgradeCategory category,
            UpgradeTier tier,
            WeaponModifiers modifiers,
            int maxStacks = 1,
            string[] prerequisites = null,
            string[] incompatible = null,
            int minLevel = 1,
            float spawnWeight = 1f
        )
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.upgradeId = id;
            upgrade.displayName = displayName;
            upgrade.description = description;
            upgrade.category = category;
            upgrade.tier = tier;
            upgrade.isPermanent = true;
            upgrade.modifiers = modifiers;
            upgrade.maxStacks = maxStacks;
            upgrade.prerequisiteIds = prerequisites ?? new string[0];
            upgrade.incompatibleIds = incompatible ?? new string[0];
            upgrade.minPlayerLevel = minLevel;
            upgrade.spawnWeight = spawnWeight;

            // Set tier color
            upgrade.iconColor = GetTierColor(tier);

            string fullPath = $"Assets/_Project/Resources/{path}.asset";
            string directory = System.IO.Path.GetDirectoryName(fullPath);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(upgrade, fullPath);
            Debug.Log($"[UpgradeCreator] Created upgrade: {displayName} at {fullPath}");
        }

        private static Color GetTierColor(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Common => new Color(0.8f, 0.8f, 0.8f), // Gray
                UpgradeTier.Rare => new Color(0.3f, 0.5f, 1f),     // Blue
                UpgradeTier.Epic => new Color(0.7f, 0.3f, 1f),     // Purple
                UpgradeTier.Legendary => new Color(1f, 0.6f, 0f),  // Orange
                _ => Color.white
            };
        }

        [MenuItem("Neural Break/Create Upgrades/Clear All Upgrades")]
        public static void ClearAllUpgrades()
        {
            string path = "Assets/_Project/Resources/Upgrades";
            if (System.IO.Directory.Exists(path))
            {
                System.IO.Directory.Delete(path, true);
                AssetDatabase.Refresh();
                Debug.Log("[UpgradeCreator] Cleared all upgrade assets");
            }
        }
    }
}
