using UnityEngine;
using System;
using System.Collections.Generic;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Ship shape types
    /// </summary>
    public enum ShipShape
    {
        Triangle,       // Default arrow shape
        Diamond,        // Rotated square
        Arrow,          // Sharper arrow
        Circle,         // Round ship
        Hexagon,        // Six-sided
        Star,           // Star shape
        Custom          // Uses custom sprite
    }

    /// <summary>
    /// How to unlock a skin
    /// </summary>
    public enum UnlockRequirement
    {
        Default,        // Available from start
        Score,          // Reach score threshold
        Level,          // Reach level threshold
        Kills,          // Kill X enemies
        Bosses,         // Kill X bosses
        Combo,          // Achieve X combo
        Achievement,    // Unlock specific achievement
        Time            // Survive X seconds
    }

    /// <summary>
    /// Ship skin definition
    /// </summary>
    [Serializable]
    public class ShipSkin
    {
        public string id;
        public string name;
        public string description;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.cyan;
        public Color trailColor = Color.cyan;
        public Color projectileColor = Color.yellow;
        public ShipShape shape = ShipShape.Triangle;
        public bool hasGlow = true;
        public float glowIntensity = 1f;
        public UnlockRequirement unlockRequirement;
        public int unlockValue;
    }

    /// <summary>
    /// Manages ship skin database and definitions.
    /// </summary>
    public static class ShipCustomizationData
    {
        private static List<ShipSkin> _skins;

        /// <summary>
        /// Get all available skins
        /// </summary>
        public static IReadOnlyList<ShipSkin> GetAllSkins()
        {
            if (_skins == null)
            {
                InitializeSkins();
            }
            return _skins;
        }

        /// <summary>
        /// Find a skin by ID
        /// </summary>
        public static ShipSkin GetSkin(string skinId)
        {
            if (_skins == null)
            {
                InitializeSkins();
            }
            return _skins.Find(s => s.id == skinId);
        }

        private static void InitializeSkins()
        {
            _skins = new List<ShipSkin>();

            // Default skin
            _skins.Add(new ShipSkin
            {
                id = "default",
                name = "Cyber Wing",
                description = "Standard issue neural fighter",
                primaryColor = Color.white,
                secondaryColor = new Color(0.3f, 0.8f, 1f),
                trailColor = new Color(0.3f, 0.8f, 1f, 0.8f),
                projectileColor = new Color(1f, 1f, 0.5f),
                shape = ShipShape.Triangle,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Default
            });

            // Score unlocks
            _skins.Add(new ShipSkin
            {
                id = "golden",
                name = "Golden Ace",
                description = "For high scorers",
                primaryColor = new Color(1f, 0.85f, 0.3f),
                secondaryColor = new Color(1f, 0.6f, 0.1f),
                trailColor = new Color(1f, 0.8f, 0.2f, 0.8f),
                projectileColor = new Color(1f, 0.9f, 0.3f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1.5f,
                unlockRequirement = UnlockRequirement.Score,
                unlockValue = 100000
            });

            _skins.Add(new ShipSkin
            {
                id = "platinum",
                name = "Platinum Elite",
                description = "Score 500,000 points",
                primaryColor = new Color(0.9f, 0.9f, 0.95f),
                secondaryColor = new Color(0.7f, 0.8f, 1f),
                trailColor = new Color(0.8f, 0.9f, 1f, 0.8f),
                projectileColor = Color.white,
                shape = ShipShape.Diamond,
                hasGlow = true,
                glowIntensity = 2f,
                unlockRequirement = UnlockRequirement.Score,
                unlockValue = 500000
            });

            // Level unlocks
            _skins.Add(new ShipSkin
            {
                id = "void",
                name = "Void Walker",
                description = "Reach level 25",
                primaryColor = new Color(0.4f, 0.1f, 0.6f),
                secondaryColor = new Color(0.8f, 0.3f, 1f),
                trailColor = new Color(0.6f, 0.2f, 0.8f, 0.8f),
                projectileColor = new Color(0.8f, 0.4f, 1f),
                shape = ShipShape.Hexagon,
                hasGlow = true,
                glowIntensity = 1.2f,
                unlockRequirement = UnlockRequirement.Level,
                unlockValue = 25
            });

            _skins.Add(new ShipSkin
            {
                id = "matrix",
                name = "Code Runner",
                description = "Reach level 50",
                primaryColor = new Color(0.2f, 0.8f, 0.3f),
                secondaryColor = new Color(0.1f, 1f, 0.4f),
                trailColor = new Color(0.2f, 1f, 0.3f, 0.8f),
                projectileColor = new Color(0.3f, 1f, 0.4f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Level,
                unlockValue = 50
            });

            // Kill unlocks
            _skins.Add(new ShipSkin
            {
                id = "hunter",
                name = "Hunter",
                description = "Kill 1,000 enemies",
                primaryColor = new Color(1f, 0.3f, 0.2f),
                secondaryColor = new Color(1f, 0.5f, 0.1f),
                trailColor = new Color(1f, 0.4f, 0.2f, 0.8f),
                projectileColor = new Color(1f, 0.6f, 0.2f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1.3f,
                unlockRequirement = UnlockRequirement.Kills,
                unlockValue = 1000
            });

            _skins.Add(new ShipSkin
            {
                id = "slayer",
                name = "Slayer",
                description = "Kill 10,000 enemies",
                primaryColor = new Color(0.8f, 0.1f, 0.1f),
                secondaryColor = new Color(1f, 0.2f, 0.1f),
                trailColor = new Color(1f, 0.3f, 0.1f, 0.8f),
                projectileColor = new Color(1f, 0.4f, 0.2f),
                shape = ShipShape.Star,
                hasGlow = true,
                glowIntensity = 1.5f,
                unlockRequirement = UnlockRequirement.Kills,
                unlockValue = 10000
            });

            // Boss unlocks
            _skins.Add(new ShipSkin
            {
                id = "boss_hunter",
                name = "Boss Hunter",
                description = "Defeat 5 bosses",
                primaryColor = new Color(1f, 0.1f, 0.5f),
                secondaryColor = new Color(1f, 0.4f, 0.7f),
                trailColor = new Color(1f, 0.3f, 0.6f, 0.8f),
                projectileColor = new Color(1f, 0.5f, 0.8f),
                shape = ShipShape.Diamond,
                hasGlow = true,
                glowIntensity = 1.4f,
                unlockRequirement = UnlockRequirement.Bosses,
                unlockValue = 5
            });

            // Combo unlocks
            _skins.Add(new ShipSkin
            {
                id = "combo_king",
                name = "Combo King",
                description = "Achieve 100x combo",
                primaryColor = new Color(0.3f, 1f, 1f),
                secondaryColor = new Color(0.5f, 1f, 0.8f),
                trailColor = new Color(0.4f, 1f, 0.9f, 0.8f),
                projectileColor = new Color(0.5f, 1f, 1f),
                shape = ShipShape.Hexagon,
                hasGlow = true,
                glowIntensity = 1.6f,
                unlockRequirement = UnlockRequirement.Combo,
                unlockValue = 100
            });

            // Survival unlocks
            _skins.Add(new ShipSkin
            {
                id = "survivor",
                name = "Survivor",
                description = "Survive 10 minutes",
                primaryColor = new Color(0.6f, 0.8f, 1f),
                secondaryColor = new Color(0.4f, 0.6f, 0.9f),
                trailColor = new Color(0.5f, 0.7f, 1f, 0.8f),
                projectileColor = new Color(0.6f, 0.8f, 1f),
                shape = ShipShape.Circle,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Time,
                unlockValue = 600
            });

            // Special skins
            _skins.Add(new ShipSkin
            {
                id = "neon",
                name = "Neon Dream",
                description = "Unlock all achievements",
                primaryColor = new Color(1f, 0.2f, 0.8f),
                secondaryColor = new Color(0.2f, 1f, 0.8f),
                trailColor = new Color(1f, 0.4f, 0.8f, 0.8f),
                projectileColor = new Color(0.8f, 1f, 0.4f),
                shape = ShipShape.Star,
                hasGlow = true,
                glowIntensity = 2f,
                unlockRequirement = UnlockRequirement.Achievement,
                unlockValue = 0 // Special case - all achievements
            });

            Debug.Log($"[ShipCustomizationData] Initialized {_skins.Count} skins");
        }
    }
}
