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
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _baseRadius = 0.05f; // TypeScript BASE_RADIUS = 0.05

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        // Runtime state - FIXED AT SPAWN TIME (each bullet is independent)
        private Vector2 _direction;
        private float _speed; // Stored at spawn - never changes
        private int _damage;
        private int _powerLevel;
        private float _lifeTimer;
        private bool _isActive;
        private bool _isPiercing;
        private bool _isHoming;
        private int _pierceCount;
        private const int MAX_PIERCE = 5;

        // Pool callback
        private System.Action<Projectile> _returnToPool;

        // Public accessors
        public bool IsActive => _isActive;
        public bool IsPiercing => _isPiercing;
        public bool IsHoming => _isHoming;
        public float Radius => _baseRadius * (1f + _powerLevel * 0.06f); // Scale with power level

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
            var upgradeManager = WeaponUpgradeManager.Instance;
            if (upgradeManager == null) return;

            // Find nearest enemy
            Transform nearestEnemy = FindNearestEnemy(upgradeManager.HomingRange);
            if (nearestEnemy == null) return;

            // Calculate direction to enemy
            Vector2 toEnemy = ((Vector2)nearestEnemy.position - (Vector2)transform.position).normalized;

            // Smoothly turn toward enemy
            _direction = Vector2.Lerp(_direction, toEnemy, upgradeManager.HomingStrength * Time.deltaTime).normalized;
        }

        private Transform FindNearestEnemy(float range)
        {
            Transform nearest = null;
            float nearestDist = range;

            // Find all colliders in range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.transform;
                }
            }

            return nearest;
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

            // Store speed at spawn time (FIXED - each bullet has its own speed)
            _speed = ConfigProvider.Weapon.projectileSpeed;

            // Store damage at spawn time (FIXED)
            _damage = damage;
            _powerLevel = powerLevel;
            _returnToPool = returnToPool;

            // Store lifetime at spawn time (FIXED)
            _lifeTimer = ConfigProvider.Weapon.projectileLifetime;

            _isActive = true;
            _isPiercing = isPiercing;
            _isHoming = isHoming;
            _pierceCount = 0;

            // Set rotation ONCE at spawn (won't update unless homing)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Scale with power level - TypeScript formula: baseRadius * (1 + powerLevel * 0.06)
            // Visual scale is larger than collision for visibility
            float visualScale = (_baseRadius + powerLevel * 0.006f) * 10f; // 10x for visibility in Unity units
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
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(_damage, transform.position);

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
