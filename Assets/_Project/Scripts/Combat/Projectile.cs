using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Player projectile - pooled, moves in a direction, damages enemies on contact.
    /// All values driven by ConfigProvider - no magic numbers.
    /// Based on TypeScript Projectile.ts.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        // Base radius now read from ConfigProvider.WeaponSystem.projectileSize
        private float m_baseRadius;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private TrailRenderer m_trailRenderer;

        // Physics
        private Rigidbody2D m_rb;
        private CircleCollider2D m_collider;

        // Runtime state - FIXED AT SPAWN TIME (each bullet is independent)
        private Vector2 m_direction;
        private Vector2 m_initialDirection; // For homing: original aim direction
        private float m_speed; // Stored at spawn - never changes
        private int m_damage;
        private int m_powerLevel;
        private float m_lifeTimer;
        private bool m_isActive;
        private bool m_isPiercing;
        private bool m_isHoming;
        private int m_pierceCount;
        private const int MAX_PIERCE = 5;

        // Homing target lock
        private Transform m_lockedTarget;
        private float m_reacquireTimer;
        private const float REACQUIRE_DELAY = 0.2f;
        private const float AIM_CONE_ANGLE = 45f;
        private const float AIM_PRIORITY_MULTIPLIER = 0.5f;

        // Cached reference - avoids FindFirstObjectByType every frame!
        private static WeaponUpgradeManager s_cachedUpgradeManager;

        // Pool callback
        private System.Action<Projectile> m_returnToPool;

        // Public accessors
        public bool IsActive => m_isActive;
        public bool IsPiercing => m_isPiercing;
        public bool IsHoming => m_isHoming;
        public float Radius => m_baseRadius * (1f + m_powerLevel * 0.06f); // Scale with power level

        private void Awake()
        {
            // Setup Rigidbody2D for trigger collision detection
            m_rb = GetComponent<Rigidbody2D>();
            if (m_rb == null)
            {
                m_rb = gameObject.AddComponent<Rigidbody2D>();
            }
            m_rb.gravityScale = 0f;
            m_rb.bodyType = RigidbodyType2D.Kinematic; // We control movement manually
            m_rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Setup collider as trigger
            m_collider = GetComponent<CircleCollider2D>();
            if (m_collider == null)
            {
                m_collider = gameObject.AddComponent<CircleCollider2D>();
            }
            m_collider.isTrigger = true;
            // Collider radius will be set in Initialize() from config

            // Get sprite renderer reference if not assigned
            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Ensure sprite renderer has a sprite and is visible
            if (m_spriteRenderer != null)
            {
                if (m_spriteRenderer.sprite == null)
                {
                    // Create a default sprite if none exists
                    m_spriteRenderer.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
                }
                m_spriteRenderer.sortingOrder = 100; // High sorting order to be visible above everything
                m_spriteRenderer.enabled = true;
            }

            // Get trail renderer reference if not assigned
            if (m_trailRenderer == null)
            {
                m_trailRenderer = GetComponent<TrailRenderer>();
            }
        }

        private void Update()
        {
            if (!m_isActive) return;

            // Homing behavior (only thing that can change direction)
            if (m_isHoming)
            {
                UpdateHoming();
                // Update rotation only for homing projectiles since direction changes
                float homingAngle = Mathf.Atan2(m_direction.y, m_direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, homingAngle - 90f);
            }

            // Move using stored speed and direction (fixed at spawn)
            transform.position += (Vector3)(m_direction * m_speed * Time.deltaTime);

            // Lifetime check
            m_lifeTimer -= Time.deltaTime;
            if (m_lifeTimer <= 0f)
            {
                Deactivate();
            }
        }

        private void UpdateHoming()
        {
            // Use cached reference instead of FindFirstObjectByType every frame
            if (s_cachedUpgradeManager == null)
                s_cachedUpgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (s_cachedUpgradeManager == null) return;

            float range = s_cachedUpgradeManager.HomingRange;
            float strength = s_cachedUpgradeManager.HomingStrength;

            // Check if we need to reacquire target
            if (m_lockedTarget == null || !IsTargetValid(m_lockedTarget, range))
            {
                m_reacquireTimer -= Time.deltaTime;
                if (m_reacquireTimer <= 0f)
                {
                    m_lockedTarget = FindBestTarget(range);
                    m_reacquireTimer = REACQUIRE_DELAY;
                }
            }

            // If we have a valid target, home toward it
            if (m_lockedTarget != null && IsTargetValid(m_lockedTarget, range))
            {
                Vector2 toTarget = ((Vector2)m_lockedTarget.position - (Vector2)transform.position).normalized;
                m_direction = Vector2.Lerp(m_direction, toTarget, strength * Time.deltaTime).normalized;
            }
            // else: maintain current direction (fly straight)
        }

        private bool IsTargetValid(Transform target, float range)
        {
            if (target == null) return false;

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > range * 1.5f) return false;

            var enemy = target.GetComponent<EnemyBase>();
            if (enemy == null || !enemy.IsAlive) return false;

            return true;
        }

        /// <summary>
        /// Find the best target, prioritizing enemies in the aim direction.
        /// </summary>
        private Transform FindBestTarget(float range)
        {
            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, range);

            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;

                Vector2 toEnemy = (Vector2)col.transform.position - (Vector2)transform.position;
                float dist = toEnemy.magnitude;

                if (dist < 0.1f) continue;

                // Calculate angle between initial aim direction and enemy direction
                Vector2 toEnemyDir = toEnemy.normalized;
                float dot = Vector2.Dot(m_initialDirection, toEnemyDir);
                float angleDeg = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

                // Score = distance, with bonus for enemies in aim cone
                float score = dist;
                if (angleDeg <= AIM_CONE_ANGLE)
                {
                    score *= AIM_PRIORITY_MULTIPLIER; // Prioritize enemies in aim cone
                }
                else if (angleDeg > 90f)
                {
                    score *= 2f; // Penalize enemies behind
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = col.transform;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Initialize projectile when spawned from pool.
        /// All values are FIXED at spawn time - each projectile is fully independent.
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, int damage, int powerLevel,
            System.Action<Projectile> returnToPool, bool isPiercing = false, bool isHoming = false)
        {
            // Unparent to ensure projectile moves independently in world space
            transform.SetParent(null);

            // Set position and direction (FIXED - won't change unless homing)
            transform.position = position;
            m_direction = direction.normalized;
            m_initialDirection = m_direction; // Store for homing target acquisition

            // Reset homing state
            m_lockedTarget = null;
            m_reacquireTimer = 0f;

            // Store speed at spawn time (FIXED - each bullet has its own speed)
            m_speed = ConfigProvider.WeaponSystem.baseProjectileSpeed;

            // Store damage at spawn time (FIXED)
            m_damage = damage;
            m_powerLevel = powerLevel;
            m_returnToPool = returnToPool;

            // Store lifetime at spawn time (FIXED)
            m_lifeTimer = ConfigProvider.WeaponSystem.projectileLifetime;

            m_isActive = true;
            m_isPiercing = isPiercing;
            m_isHoming = isHoming;
            m_pierceCount = 0;

            // Get projectile size from config
            m_baseRadius = ConfigProvider.WeaponSystem?.projectileSize ?? 0.15f;

            // Apply projectile size per level scaling from config
            float sizePerLevel = ConfigProvider.WeaponSystem?.powerLevels?.projectileSizePerLevel ?? 0.01f;
            float scaledRadius = m_baseRadius * (1f + powerLevel * sizePerLevel);

            // Update collider radius
            if (m_collider != null)
            {
                m_collider.radius = scaledRadius;
            }

            // Acquire initial target for homing projectiles
            if (m_isHoming)
            {
                // Use cached reference
                if (s_cachedUpgradeManager == null)
                    s_cachedUpgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
                float range = s_cachedUpgradeManager != null ? s_cachedUpgradeManager.HomingRange : 10f;
                m_lockedTarget = FindBestTarget(range);
            }

            // Set rotation ONCE at spawn (won't update unless homing)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Visual scale - use config-based radius with visual multiplier
            float visualScale = scaledRadius * 3f;
            transform.localScale = Vector3.one * visualScale;

            // Visual indication for special projectiles
            UpdateVisualForUpgrades();

            // Reset trail
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();

                // Change trail color for special projectiles
                if (m_isPiercing || m_isHoming)
                {
                    Color trailColor = m_isPiercing ? new Color(1f, 0.5f, 0f) : new Color(0.5f, 1f, 0.5f);
                    m_trailRenderer.startColor = trailColor;
                    m_trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                }
            }
        }

        private void UpdateVisualForUpgrades()
        {
            if (m_spriteRenderer == null) return;

            if (m_isPiercing && m_isHoming)
            {
                m_spriteRenderer.color = new Color(1f, 0.8f, 0.2f); // Gold
            }
            else if (m_isPiercing)
            {
                m_spriteRenderer.color = new Color(1f, 0.5f, 0f); // Orange
            }
            else if (m_isHoming)
            {
                m_spriteRenderer.color = new Color(0.5f, 1f, 0.5f); // Green
            }
            else
            {
                m_spriteRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// Called when projectile hits something
        /// </summary>
        public int GetDamage()
        {
            return m_damage;
        }

        /// <summary>
        /// Set projectile direction (for behaviors like homing).
        /// </summary>
        public void SetDirection(Vector2 direction)
        {
            m_direction = direction.normalized;
        }

        /// <summary>
        /// Get current direction.
        /// </summary>
        public Vector2 GetDirection()
        {
            return m_direction;
        }

        /// <summary>
        /// Set damage (for behaviors like ricochet that reduce damage).
        /// </summary>
        public void SetDamage(int damage)
        {
            m_damage = damage;
        }

        /// <summary>
        /// Deactivate and return to pool
        /// </summary>
        public void Deactivate()
        {
            if (!m_isActive) return;

            m_isActive = false;
            m_returnToPool?.Invoke(this);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
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

            // Check if hit enemy
            if (other.CompareTag("Enemy"))
            {
                bool hitSomething = false;

                // First try EnemyBase (standard enemies)
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(m_damage, transform.position);
                    hitSomething = true;
                }
                else
                {
                    // Try WormSegment (ChaosWorm body segments)
                    var wormSegment = other.GetComponent<WormSegment>();
                    if (wormSegment != null)
                    {
                        wormSegment.TakeDamage(m_damage, transform.position);
                        hitSomething = true;
                    }
                }

                if (hitSomething)
                {
                    // Piercing: continue through enemies up to max pierce count
                    if (m_isPiercing)
                    {
                        m_pierceCount++;
                        if (m_pierceCount >= MAX_PIERCE)
                        {
                            Deactivate();
                        }
                        // Otherwise, keep going!
                    }
                    else
                    {
                        Deactivate();
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_baseRadius);
        }
    }
}
