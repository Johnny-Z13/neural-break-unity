using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Entities;
using NeuralBreak.Combat.ProjectileBehaviors;
using System.Collections.Generic;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Enhanced projectile with modular behavior composition.
    /// Supports homing, piercing, explosion, chain lightning, ricochet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class EnhancedProjectile : MonoBehaviour
    {
        [Header("Settings")]
        // Base radius now read from ConfigProvider.WeaponSystem.projectileSize
        private float m_baseRadius;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private TrailRenderer m_trailRenderer;

        // Visual profile for special weapon effects
        private ProjectileVisualProfile m_visualProfile;

        // Physics
        private Rigidbody2D m_rb;
        private CircleCollider2D m_collider;

        // Runtime state
        private Vector2 m_direction;
        private float m_speed;
        private int m_damage;
        private int m_powerLevel;
        private float m_lifeTimer;
        private bool m_isActive;

        // Behaviors (modular composition)
        private List<IProjectileBehavior> m_behaviors = new List<IProjectileBehavior>();

        // Cached behavior instances (reused across shots - zero allocation after first use)
        private HomingBehavior m_cachedHomingBehavior;
        private PiercingBehavior m_cachedPiercingBehavior;
        private ExplosionBehavior m_cachedExplosionBehavior;
        private ChainLightningBehavior m_cachedChainLightningBehavior;
        private RicochetBehavior m_cachedRicochetBehavior;

        // Pool callback
        private System.Action<EnhancedProjectile> m_returnToPool;

        // Public accessors
        public bool IsActive => m_isActive;
        public float Radius => m_baseRadius * (1f + m_powerLevel * 0.06f);
        public Vector2 Direction => m_direction;

        private void Awake()
        {
            // Setup physics
            m_rb = GetComponent<Rigidbody2D>();
            if (m_rb == null) m_rb = gameObject.AddComponent<Rigidbody2D>();
            m_rb.gravityScale = 0f;
            m_rb.bodyType = RigidbodyType2D.Kinematic;
            m_rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Setup collider
            m_collider = GetComponent<CircleCollider2D>();
            if (m_collider == null) m_collider = gameObject.AddComponent<CircleCollider2D>();
            m_collider.isTrigger = true;
            // Collider radius will be set in Initialize() from config

            // Setup sprite
            if (m_spriteRenderer == null) m_spriteRenderer = GetComponent<SpriteRenderer>();
            if (m_spriteRenderer != null)
            {
                if (m_spriteRenderer.sprite == null)
                {
                    m_spriteRenderer.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
                }
                m_spriteRenderer.sortingOrder = 100;
                m_spriteRenderer.enabled = true;
            }

            // Setup trail
            if (m_trailRenderer == null) m_trailRenderer = GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            if (!m_isActive) return;

            // Update behaviors (indexed for loop - zero allocation)
            for (int i = 0; i < m_behaviors.Count; i++)
            {
                m_behaviors[i].Update(Time.deltaTime);
            }

            // Move projectile
            transform.position += (Vector3)(m_direction * m_speed * Time.deltaTime);

            // Lifetime check
            m_lifeTimer -= Time.deltaTime;
            if (m_lifeTimer <= 0f)
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Initialize projectile with modifiers from upgrade system.
        /// </summary>
        public void Initialize(
            Vector2 position,
            Vector2 direction,
            int damage,
            int powerLevel,
            System.Action<EnhancedProjectile> returnToPool,
            WeaponModifiers modifiers,
            ProjectileVisualProfile visualProfile = null)
        {
            transform.SetParent(null);
            transform.position = position;
            m_direction = direction.normalized;
            m_damage = damage;
            m_powerLevel = powerLevel;
            m_returnToPool = returnToPool;
            m_isActive = true;

            // Store visual profile (use default if not provided)
            m_visualProfile = visualProfile ?? ProjectileVisualProfile.Default;

            // Apply speed modifier
            m_speed = ConfigProvider.WeaponSystem.baseProjectileSpeed * modifiers.projectileSpeedMultiplier;

            // Apply lifetime
            m_lifeTimer = ConfigProvider.WeaponSystem.projectileLifetime;

            // Clear previous behaviors (indexed for loop - zero allocation)
            for (int i = 0; i < m_behaviors.Count; i++)
            {
                m_behaviors[i].OnDeactivate();
            }
            m_behaviors.Clear();

            // Add behaviors based on modifiers (cached instances - zero allocation after first shot)
            if (modifiers.enableHoming)
            {
                if (m_cachedHomingBehavior == null)
                    m_cachedHomingBehavior = new HomingBehavior(modifiers.homingStrength);
                else
                    m_cachedHomingBehavior.Reset(modifiers.homingStrength);
                m_behaviors.Add(m_cachedHomingBehavior);
            }

            if (modifiers.piercingCount > 0)
            {
                if (m_cachedPiercingBehavior == null)
                    m_cachedPiercingBehavior = new PiercingBehavior(modifiers.piercingCount);
                else
                    m_cachedPiercingBehavior.Reset(modifiers.piercingCount);
                m_behaviors.Add(m_cachedPiercingBehavior);
            }

            if (modifiers.enableExplosion)
            {
                if (m_cachedExplosionBehavior == null)
                    m_cachedExplosionBehavior = new ExplosionBehavior(modifiers.explosionRadius);
                else
                    m_cachedExplosionBehavior.Reset(modifiers.explosionRadius);
                m_behaviors.Add(m_cachedExplosionBehavior);
            }

            if (modifiers.enableChainLightning)
            {
                if (m_cachedChainLightningBehavior == null)
                    m_cachedChainLightningBehavior = new ChainLightningBehavior(modifiers.chainLightningTargets);
                else
                    m_cachedChainLightningBehavior.Reset(modifiers.chainLightningTargets);
                m_behaviors.Add(m_cachedChainLightningBehavior);
            }

            if (modifiers.enableRicochet)
            {
                if (m_cachedRicochetBehavior == null)
                    m_cachedRicochetBehavior = new RicochetBehavior(modifiers.ricochetCount);
                else
                    m_cachedRicochetBehavior.Reset(modifiers.ricochetCount);
                m_behaviors.Add(m_cachedRicochetBehavior);
            }

            // Initialize all behaviors (indexed for loop - zero allocation)
            for (int i = 0; i < m_behaviors.Count; i++)
            {
                m_behaviors[i].Initialize(this);
            }

            // Set rotation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Get projectile size from config
            m_baseRadius = ConfigProvider.WeaponSystem?.projectileSize ?? 0.15f;

            // Apply projectile size per level scaling from config
            float sizePerLevel = ConfigProvider.WeaponSystem?.powerLevels?.projectileSizePerLevel ?? 0.01f;
            float scaledRadius = m_baseRadius * (1f + powerLevel * sizePerLevel) * modifiers.projectileSizeMultiplier;

            // Update collider radius
            if (m_collider != null)
            {
                m_collider.radius = scaledRadius;
            }

            // Visual scale - use config-based radius with visual profile size multiplier
            float visualScale = scaledRadius * 3f * m_visualProfile.sizeMultiplier;
            transform.localScale = Vector3.one * visualScale;

            // Apply visual profile (overrides default behavior colors)
            if (visualProfile != null)
            {
                ApplyVisualProfile();
            }
            else
            {
                // Fallback to behavior-based colors if no profile
                UpdateVisualForBehaviors();
            }

            // Reset trail
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
                if (visualProfile != null)
                {
                    Color trailColor = m_visualProfile.trailColor;
                    m_trailRenderer.startColor = trailColor;
                    m_trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                }
                else
                {
                    UpdateTrailColor();
                }
            }
        }

        /// <summary>
        /// Apply visual profile settings (color, trail, glow, particles).
        /// </summary>
        private void ApplyVisualProfile()
        {
            if (m_visualProfile == null) return;

            // Apply projectile color with HDR glow intensity
            if (m_spriteRenderer != null)
            {
                Color finalColor = m_visualProfile.projectileColor * m_visualProfile.glowIntensity;
                m_spriteRenderer.color = finalColor;
            }

            // Spawn particle effect if provided
            if (m_visualProfile.particleEffectPrefab != null)
            {
                GameObject particles = Instantiate(m_visualProfile.particleEffectPrefab, transform.position, Quaternion.identity);
                particles.transform.SetParent(transform); // Attach to projectile
            }
        }

        private void UpdateVisualForBehaviors()
        {
            if (m_spriteRenderer == null) return;

            // Choose color based on behaviors
            if (m_behaviors.Count == 0)
            {
                m_spriteRenderer.color = Color.white;
            }
            else
            {
                // Multi-behavior: gold
                if (m_behaviors.Count > 2)
                {
                    m_spriteRenderer.color = new Color(1f, 0.8f, 0.2f);
                }
                // Explosion: orange
                else if (HasBehavior<ExplosionBehavior>())
                {
                    m_spriteRenderer.color = new Color(1f, 0.5f, 0f);
                }
                // Chain lightning: cyan
                else if (HasBehavior<ChainLightningBehavior>())
                {
                    m_spriteRenderer.color = new Color(0.2f, 0.8f, 1f);
                }
                // Homing: green
                else if (HasBehavior<HomingBehavior>())
                {
                    m_spriteRenderer.color = new Color(0.5f, 1f, 0.5f);
                }
                // Piercing: purple
                else if (HasBehavior<PiercingBehavior>())
                {
                    m_spriteRenderer.color = new Color(0.8f, 0.4f, 1f);
                }
                // Ricochet: yellow
                else if (HasBehavior<RicochetBehavior>())
                {
                    m_spriteRenderer.color = new Color(1f, 1f, 0.3f);
                }
            }
        }

        private void UpdateTrailColor()
        {
            if (m_trailRenderer == null) return;

            Color trailColor = m_spriteRenderer != null ? m_spriteRenderer.color : Color.white;
            m_trailRenderer.startColor = trailColor;
            m_trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }

        private bool HasBehavior<T>() where T : IProjectileBehavior
        {
            for (int i = 0; i < m_behaviors.Count; i++)
            {
                if (m_behaviors[i] is T) return true;
            }
            return false;
        }

        public int GetDamage()
        {
            return m_damage;
        }

        public void SetDamage(int damage)
        {
            m_damage = damage;
        }

        public void SetDirection(Vector2 direction)
        {
            m_direction = direction.normalized;
        }

        public Vector2 GetDirection()
        {
            return m_direction;
        }

        public void Deactivate()
        {
            if (!m_isActive) return;

            m_isActive = false;

            // Notify behaviors (indexed for loop - zero allocation)
            for (int i = 0; i < m_behaviors.Count; i++)
            {
                m_behaviors[i].OnDeactivate();
            }

            m_returnToPool?.Invoke(this);
        }

        public void OnReturnToPool()
        {
            m_isActive = false;
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!m_isActive) return;

            if (other.CompareTag("Enemy"))
            {
                EnemyBase enemy = null;

                // First try EnemyBase (standard enemies)
                // TryGetComponent is zero-alloc (unlike GetComponent which may allocate on null)
                other.TryGetComponent<EnemyBase>(out enemy);

                // If not found, try WormSegment (ChaosWorm body segments)
                if (enemy == null)
                {
                    other.TryGetComponent<WormSegment>(out var wormSegment);
                    if (wormSegment != null)
                    {
                        // Forward damage to worm segment (which forwards to parent)
                        wormSegment.TakeDamage(m_damage, transform.position);

                        // For behaviors, we need to get the actual enemy
                        // WormSegment doesn't expose parent, so just handle destruction
                        bool hasPiercing = HasBehavior<PiercingBehavior>();
                        if (!hasPiercing)
                        {
                            Deactivate();
                        }
                        return;
                    }
                }

                if (enemy != null && enemy.IsAlive)
                {
                    // Apply base damage
                    enemy.TakeDamage(m_damage, transform.position);

                    // Let behaviors handle hit (indexed for loop - zero allocation)
                    bool shouldDestroy = true;
                    for (int i = 0; i < m_behaviors.Count; i++)
                    {
                        bool behaviorSaysDestroy = m_behaviors[i].OnHitEnemy(enemy);
                        // If ANY behavior says don't destroy, keep projectile alive
                        if (!behaviorSaysDestroy)
                        {
                            shouldDestroy = false;
                        }
                    }

                    // Destroy if no behaviors or all behaviors agree
                    if (shouldDestroy && m_behaviors.Count == 0)
                    {
                        Deactivate();
                    }
                    else if (shouldDestroy)
                    {
                        // Check if piercing behavior exists and says no
                        bool hasPiercing = HasBehavior<PiercingBehavior>();
                        if (!hasPiercing)
                        {
                            Deactivate();
                        }
                    }
                }
            }
        }
    }
}
