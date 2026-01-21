using UnityEngine;

namespace NeuralBreak.Data
{
    /// <summary>
    /// ScriptableObject for enemy configuration data.
    /// Allows balancing without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "NeuralBreak/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public Core.EnemyType enemyType;
        public string displayName;

        [Header("Base Stats")]
        [Tooltip("Base health at level 1")]
        public int baseHealth = 1;

        [Tooltip("Movement speed in units/second")]
        public float baseSpeed = 1.5f;

        [Tooltip("Collision damage to player")]
        public int baseDamage = 5;

        [Tooltip("XP awarded on kill")]
        public int xpValue = 1;

        [Tooltip("Score points awarded")]
        public int scoreValue = 100;

        [Header("Collision")]
        [Tooltip("Radius for collision detection")]
        public float collisionRadius = 0.5f;

        [Header("Spawn Settings")]
        [Tooltip("Duration of spawn animation")]
        public float spawnDuration = 0.25f;

        [Tooltip("Invulnerable during spawn animation")]
        public bool invulnerableDuringSpawn = true;

        [Header("Death Settings")]
        [Tooltip("Duration of death animation")]
        public float deathDuration = 0.5f;

        [Tooltip("Damage dealt to nearby enemies on death")]
        public int deathDamage = 0;

        [Tooltip("Radius of death damage")]
        public float deathDamageRadius = 0f;

        [Header("Level Scaling (multiplied per level)")]
        [Tooltip("Health multiplier per level (1.025 = 2.5% increase)")]
        public float healthScalePerLevel = 1.025f;

        [Tooltip("Speed multiplier per level")]
        public float speedScalePerLevel = 1.012f;

        [Tooltip("Damage multiplier per level")]
        public float damageScalePerLevel = 1.02f;

        [Header("Ranged Attack (optional)")]
        [Tooltip("Can this enemy shoot projectiles?")]
        public bool canShoot = false;

        [Tooltip("Time between shots")]
        public float fireRate = 2f;

        [Tooltip("Projectile speed")]
        public float projectileSpeed = 7f;

        [Tooltip("Projectile damage")]
        public int projectileDamage = 10;

        [Tooltip("Number of projectiles per burst")]
        public int burstCount = 1;

        [Tooltip("Delay between burst shots")]
        public float burstDelay = 0.2f;

        [Tooltip("Time between bursts")]
        public float burstCooldown = 3f;

        [Header("Visual")]
        public Color primaryColor = Color.red;
        public Color secondaryColor = Color.yellow;

        /// <summary>
        /// Get health scaled for a specific level
        /// </summary>
        public int GetScaledHealth(int level)
        {
            return Mathf.RoundToInt(baseHealth * Mathf.Pow(healthScalePerLevel, level - 1));
        }

        /// <summary>
        /// Get speed scaled for a specific level
        /// </summary>
        public float GetScaledSpeed(int level)
        {
            return baseSpeed * Mathf.Pow(speedScalePerLevel, level - 1);
        }

        /// <summary>
        /// Get damage scaled for a specific level
        /// </summary>
        public int GetScaledDamage(int level)
        {
            return Mathf.RoundToInt(baseDamage * Mathf.Pow(damageScalePerLevel, level - 1));
        }
    }
}
