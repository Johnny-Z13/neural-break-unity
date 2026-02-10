using UnityEngine;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Defines visual appearance for special weapon projectiles.
    /// Allows different colored bullets for rare upgrades (homing, multi-fire, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileVisual_", menuName = "Neural Break/Combat/Projectile Visual Profile")]
    public class ProjectileVisualProfile : ScriptableObject
    {
        [Header("Color")]
        [Tooltip("Main color tint applied to projectile sprite")]
        public Color projectileColor = Color.white;

        [Tooltip("Trail color (if trail renderer exists)")]
        public Color trailColor = Color.white;

        [Header("Scale")]
        [Tooltip("Size multiplier (1.0 = normal, 2.0 = giant bullets)")]
        [Range(0.5f, 4.0f)]
        public float sizeMultiplier = 1.0f;

        [Header("Visual Effects")]
        [Tooltip("Particle effect to spawn at projectile position (optional)")]
        public GameObject particleEffectPrefab;

        [Tooltip("Glow intensity for HDR bloom effect")]
        [Range(0f, 5f)]
        public float glowIntensity = 1.0f;

        /// <summary>
        /// Default visual profile (white, no effects).
        /// </summary>
        public static ProjectileVisualProfile Default
        {
            get
            {
                var profile = CreateInstance<ProjectileVisualProfile>();
                profile.projectileColor = Color.white;
                profile.trailColor = Color.white;
                profile.sizeMultiplier = 1.0f;
                profile.glowIntensity = 1.0f;
                return profile;
            }
        }
    }
}
