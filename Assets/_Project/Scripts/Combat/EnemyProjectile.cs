using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Projectile fired by enemies toward the player.
    /// Pooled for performance. Can be "orphaned" if enemy dies (bullet keeps going).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 7f;
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _collisionRadius = 0.3f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Color _projectileColor = new Color(1f, 0.3f, 0.1f); // Orange-red

        // Components
        private Rigidbody2D _rb;

        // State
        private Vector2 _direction;
        private float _spawnTime;
        private bool _isActive;
        private bool _isOrphaned; // Enemy that fired this died
        private System.Action<EnemyProjectile> _returnToPool;

        // Public accessors
        public bool IsActive => _isActive;
        public int Damage => _damage;
        public bool IsOrphaned => _isOrphaned;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponentInChildren<TrailRenderer>();
            }
        }

        /// <summary>
        /// Initialize projectile when spawned
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, float speed, int damage,
            System.Action<EnemyProjectile> returnToPool, Color? color = null)
        {
            transform.position = position;
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _returnToPool = returnToPool;
            _spawnTime = Time.time;
            _isActive = true;
            _isOrphaned = false;

            // Set velocity
            _rb.linearVelocity = _direction * _speed;

            // Rotate to face direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Set color
            Color useColor = color ?? _projectileColor;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = useColor;
            }
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.startColor = useColor;
                _trailRenderer.endColor = new Color(useColor.r, useColor.g, useColor.b, 0f);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Mark as orphaned (enemy that fired this died)
        /// </summary>
        public void MarkOrphaned()
        {
            _isOrphaned = true;
        }

        private void Update()
        {
            if (!_isActive) return;

            // Check lifetime
            if (Time.time - _spawnTime > _lifetime)
            {
                ReturnToPool();
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            // Check player hit
            if (other.CompareTag("Player"))
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(_damage, transform.position);
                }

                ReturnToPool();
            }
            // Check wall/boundary collision
            else if (other.gameObject.CompareTag("Boundary"))
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (!_isActive) return;

            _isActive = false;
            _rb.linearVelocity = Vector2.zero;

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            gameObject.SetActive(false);
            _returnToPool?.Invoke(this);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
        public void OnReturnToPool()
        {
            _isActive = false;
            _isOrphaned = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _collisionRadius);
        }
    }
}
