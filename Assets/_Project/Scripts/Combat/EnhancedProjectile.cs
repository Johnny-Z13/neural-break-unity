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
        [SerializeField] private float _baseRadius = 0.15f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        // Physics
        private Rigidbody2D _rb;
        private CircleCollider2D _collider;

        // Runtime state
        private Vector2 _direction;
        private float _speed;
        private int _damage;
        private int _powerLevel;
        private float _lifeTimer;
        private bool _isActive;

        // Behaviors (modular composition)
        private List<IProjectileBehavior> _behaviors = new List<IProjectileBehavior>();

        // Pool callback
        private System.Action<EnhancedProjectile> _returnToPool;

        // Public accessors
        public bool IsActive => _isActive;
        public float Radius => _baseRadius * (1f + _powerLevel * 0.06f);
        public Vector2 Direction => _direction;

        private void Awake()
        {
            // Setup physics
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Setup collider
            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = _baseRadius;

            // Setup sprite
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                if (_spriteRenderer.sprite == null)
                {
                    _spriteRenderer.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
                }
                _spriteRenderer.sortingOrder = 100;
                _spriteRenderer.enabled = true;
            }

            // Setup trail
            if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            if (!_isActive) return;

            // Update behaviors
            foreach (var behavior in _behaviors)
            {
                behavior.Update(Time.deltaTime);
            }

            // Move projectile
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

            // Lifetime check
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
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
            WeaponModifiers modifiers)
        {
            transform.SetParent(null);
            transform.position = position;
            _direction = direction.normalized;
            _damage = damage;
            _powerLevel = powerLevel;
            _returnToPool = returnToPool;
            _isActive = true;

            // Apply speed modifier
            _speed = ConfigProvider.WeaponSystem.baseProjectileSpeed * modifiers.projectileSpeedMultiplier;

            // Apply lifetime
            _lifeTimer = ConfigProvider.WeaponSystem.projectileLifetime;

            // Clear previous behaviors
            foreach (var behavior in _behaviors)
            {
                behavior.OnDeactivate();
            }
            _behaviors.Clear();

            // Add behaviors based on modifiers
            if (modifiers.enableHoming)
            {
                _behaviors.Add(new HomingBehavior(modifiers.homingStrength));
            }

            if (modifiers.piercingCount > 0)
            {
                _behaviors.Add(new PiercingBehavior(modifiers.piercingCount));
            }

            if (modifiers.enableExplosion)
            {
                _behaviors.Add(new ExplosionBehavior(modifiers.explosionRadius));
            }

            if (modifiers.enableChainLightning)
            {
                _behaviors.Add(new ChainLightningBehavior(modifiers.chainLightningTargets));
            }

            if (modifiers.enableRicochet)
            {
                _behaviors.Add(new RicochetBehavior(modifiers.ricochetCount));
            }

            // Initialize all behaviors
            foreach (var behavior in _behaviors)
            {
                behavior.Initialize(this);
            }

            // Set rotation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Apply size modifier
            float visualScale = _baseRadius * (1f + powerLevel * 0.1f) * 3f * modifiers.projectileSizeMultiplier;
            transform.localScale = Vector3.one * visualScale;

            // Update visuals
            UpdateVisualForBehaviors();

            // Reset trail
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                UpdateTrailColor();
            }
        }

        private void UpdateVisualForBehaviors()
        {
            if (_spriteRenderer == null) return;

            // Choose color based on behaviors
            if (_behaviors.Count == 0)
            {
                _spriteRenderer.color = Color.white;
            }
            else
            {
                // Multi-behavior: gold
                if (_behaviors.Count > 2)
                {
                    _spriteRenderer.color = new Color(1f, 0.8f, 0.2f);
                }
                // Explosion: orange
                else if (HasBehavior<ExplosionBehavior>())
                {
                    _spriteRenderer.color = new Color(1f, 0.5f, 0f);
                }
                // Chain lightning: cyan
                else if (HasBehavior<ChainLightningBehavior>())
                {
                    _spriteRenderer.color = new Color(0.2f, 0.8f, 1f);
                }
                // Homing: green
                else if (HasBehavior<HomingBehavior>())
                {
                    _spriteRenderer.color = new Color(0.5f, 1f, 0.5f);
                }
                // Piercing: purple
                else if (HasBehavior<PiercingBehavior>())
                {
                    _spriteRenderer.color = new Color(0.8f, 0.4f, 1f);
                }
                // Ricochet: yellow
                else if (HasBehavior<RicochetBehavior>())
                {
                    _spriteRenderer.color = new Color(1f, 1f, 0.3f);
                }
            }
        }

        private void UpdateTrailColor()
        {
            if (_trailRenderer == null) return;

            Color trailColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            _trailRenderer.startColor = trailColor;
            _trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }

        private bool HasBehavior<T>() where T : IProjectileBehavior
        {
            foreach (var behavior in _behaviors)
            {
                if (behavior is T) return true;
            }
            return false;
        }

        public int GetDamage()
        {
            return _damage;
        }

        public void SetDamage(int damage)
        {
            _damage = damage;
        }

        public void SetDirection(Vector2 direction)
        {
            _direction = direction.normalized;
        }

        public Vector2 GetDirection()
        {
            return _direction;
        }

        public void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;

            // Notify behaviors
            foreach (var behavior in _behaviors)
            {
                behavior.OnDeactivate();
            }

            _returnToPool?.Invoke(this);
        }

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

            if (other.CompareTag("Enemy"))
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    // Apply base damage
                    enemy.TakeDamage(_damage, transform.position);

                    // Let behaviors handle hit
                    bool shouldDestroy = true;
                    foreach (var behavior in _behaviors)
                    {
                        bool behaviorSaysDestroy = behavior.OnHitEnemy(enemy);
                        // If ANY behavior says don't destroy, keep projectile alive
                        if (!behaviorSaysDestroy)
                        {
                            shouldDestroy = false;
                        }
                    }

                    // Destroy if no behaviors or all behaviors agree
                    if (shouldDestroy && _behaviors.Count == 0)
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
