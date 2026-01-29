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
        private float _baseRadius;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        // Physics
        private Rigidbody2D _rb;
        private CircleCollider2D _collider;

        // Runtime state - FIXED AT SPAWN TIME (each bullet is independent)
        private Vector2 _direction;
        private Vector2 _initialDirection; // For homing: original aim direction
        private float _speed; // Stored at spawn - never changes
        private int _damage;
        private int _powerLevel;
        private float _lifeTimer;
        private bool _isActive;
        private bool _isPiercing;
        private bool _isHoming;
        private int _pierceCount;
        private const int MAX_PIERCE = 5;

        // Homing target lock
        private Transform _lockedTarget;
        private float _reacquireTimer;
        private const float REACQUIRE_DELAY = 0.2f;
        private const float AIM_CONE_ANGLE = 45f;
        private const float AIM_PRIORITY_MULTIPLIER = 0.5f;

        // Pool callback
        private System.Action<Projectile> _returnToPool;

        // Public accessors
        public bool IsActive => _isActive;
        public bool IsPiercing => _isPiercing;
        public bool IsHoming => _isHoming;
        public float Radius => _baseRadius * (1f + _powerLevel * 0.06f); // Scale with power level

        private void Awake()
        {
            // Setup Rigidbody2D for trigger collision detection
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }
            _rb.gravityScale = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic; // We control movement manually
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Setup collider as trigger
            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CircleCollider2D>();
            }
            _collider.isTrigger = true;
            // Collider radius will be set in Initialize() from config

            // Get sprite renderer reference if not assigned
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            // Ensure sprite renderer has a sprite and is visible
            if (_spriteRenderer != null)
            {
                if (_spriteRenderer.sprite == null)
                {
                    // Create a default sprite if none exists
                    _spriteRenderer.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
                }
                _spriteRenderer.sortingOrder = 100; // High sorting order to be visible above everything
                _spriteRenderer.enabled = true;
            }

            // Get trail renderer reference if not assigned
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponent<TrailRenderer>();
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // Homing behavior (only thing that can change direction)
            if (_isHoming)
            {
                UpdateHoming();
                // Update rotation only for homing projectiles since direction changes
                float homingAngle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, homingAngle - 90f);
            }

            // Move using stored speed and direction (fixed at spawn)
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

            // Lifetime check
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Deactivate();
            }
        }

        private void UpdateHoming()
        {
            var upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (upgradeManager == null) return;

            float range = upgradeManager.HomingRange;
            float strength = upgradeManager.HomingStrength;

            // Check if we need to reacquire target
            if (_lockedTarget == null || !IsTargetValid(_lockedTarget, range))
            {
                _reacquireTimer -= Time.deltaTime;
                if (_reacquireTimer <= 0f)
                {
                    _lockedTarget = FindBestTarget(range);
                    _reacquireTimer = REACQUIRE_DELAY;
                }
            }

            // If we have a valid target, home toward it
            if (_lockedTarget != null && IsTargetValid(_lockedTarget, range))
            {
                Vector2 toTarget = ((Vector2)_lockedTarget.position - (Vector2)transform.position).normalized;
                _direction = Vector2.Lerp(_direction, toTarget, strength * Time.deltaTime).normalized;
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
                float dot = Vector2.Dot(_initialDirection, toEnemyDir);
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
            _direction = direction.normalized;
            _initialDirection = _direction; // Store for homing target acquisition

            // Reset homing state
            _lockedTarget = null;
            _reacquireTimer = 0f;

            // Store speed at spawn time (FIXED - each bullet has its own speed)
            _speed = ConfigProvider.WeaponSystem.baseProjectileSpeed;

            // Store damage at spawn time (FIXED)
            _damage = damage;
            _powerLevel = powerLevel;
            _returnToPool = returnToPool;

            // Store lifetime at spawn time (FIXED)
            _lifeTimer = ConfigProvider.WeaponSystem.projectileLifetime;

            _isActive = true;
            _isPiercing = isPiercing;
            _isHoming = isHoming;
            _pierceCount = 0;

            // Get projectile size from config
            _baseRadius = ConfigProvider.WeaponSystem?.projectileSize ?? 0.15f;

            // Apply projectile size per level scaling from config
            float sizePerLevel = ConfigProvider.WeaponSystem?.powerLevels?.projectileSizePerLevel ?? 0.01f;
            float scaledRadius = _baseRadius * (1f + powerLevel * sizePerLevel);

            // Update collider radius
            if (_collider != null)
            {
                _collider.radius = scaledRadius;
            }

            // Acquire initial target for homing projectiles
            if (_isHoming)
            {
                var upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
                float range = upgradeManager != null ? upgradeManager.HomingRange : 10f;
                _lockedTarget = FindBestTarget(range);
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
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();

                // Change trail color for special projectiles
                if (_isPiercing || _isHoming)
                {
                    Color trailColor = _isPiercing ? new Color(1f, 0.5f, 0f) : new Color(0.5f, 1f, 0.5f);
                    _trailRenderer.startColor = trailColor;
                    _trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                }
            }
        }

        private void UpdateVisualForUpgrades()
        {
            if (_spriteRenderer == null) return;

            if (_isPiercing && _isHoming)
            {
                _spriteRenderer.color = new Color(1f, 0.8f, 0.2f); // Gold
            }
            else if (_isPiercing)
            {
                _spriteRenderer.color = new Color(1f, 0.5f, 0f); // Orange
            }
            else if (_isHoming)
            {
                _spriteRenderer.color = new Color(0.5f, 1f, 0.5f); // Green
            }
            else
            {
                _spriteRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// Called when projectile hits something
        /// </summary>
        public int GetDamage()
        {
            return _damage;
        }

        /// <summary>
        /// Set projectile direction (for behaviors like homing).
        /// </summary>
        public void SetDirection(Vector2 direction)
        {
            _direction = direction.normalized;
        }

        /// <summary>
        /// Get current direction.
        /// </summary>
        public Vector2 GetDirection()
        {
            return _direction;
        }

        /// <summary>
        /// Set damage (for behaviors like ricochet that reduce damage).
        /// </summary>
        public void SetDamage(int damage)
        {
            _damage = damage;
        }

        /// <summary>
        /// Deactivate and return to pool
        /// </summary>
        public void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;
            _returnToPool?.Invoke(this);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
        public void OnReturnToPool()
        {
            _isActive = false;
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            // Check if hit enemy
            if (other.CompareTag("Enemy"))
            {
                bool hitSomething = false;

                // First try EnemyBase (standard enemies)
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(_damage, transform.position);
                    hitSomething = true;
                }
                else
                {
                    // Try WormSegment (ChaosWorm body segments)
                    var wormSegment = other.GetComponent<WormSegment>();
                    if (wormSegment != null)
                    {
                        wormSegment.TakeDamage(_damage, transform.position);
                        hitSomething = true;
                    }
                }

                if (hitSomething)
                {
                    // Piercing: continue through enemies up to max pierce count
                    if (_isPiercing)
                    {
                        _pierceCount++;
                        if (_pierceCount >= MAX_PIERCE)
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
            Gizmos.DrawWireSphere(transform.position, _baseRadius);
        }
    }
}
