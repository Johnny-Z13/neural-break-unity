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
        [SerializeField] private float m_speed = 7f;
        [SerializeField] private float m_lifetime = 5f;
        [SerializeField] private int m_damage = 10;
        [SerializeField] private float m_collisionRadius = 0.3f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private Color m_projectileColor = new Color(1f, 0.3f, 0.1f); // Orange-red

        // Components
        private Rigidbody2D m_rb;

        // State
        private Vector2 m_direction;
        private float m_spawnTime;
        private bool m_isActive;
        private bool m_isOrphaned; // Enemy that fired this died
        private System.Action<EnemyProjectile> m_returnToPool;

        // Public accessors
        public bool IsActive => m_isActive;
        public int Damage => m_damage;
        public bool IsOrphaned => m_isOrphaned;

        private void Awake()
        {
            m_rb = GetComponent<Rigidbody2D>();
            m_rb.gravityScale = 0f;
            m_rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (m_trailRenderer == null)
            {
                m_trailRenderer = GetComponentInChildren<TrailRenderer>();
            }
        }

        /// <summary>
        /// Initialize projectile when spawned
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, float speed, int damage,
            System.Action<EnemyProjectile> returnToPool, Color? color = null)
        {
            transform.position = position;
            m_direction = direction.normalized;
            m_speed = speed;
            m_damage = damage;
            m_returnToPool = returnToPool;
            m_spawnTime = Time.time;
            m_isActive = true;
            m_isOrphaned = false;

            // Set velocity
            m_rb.linearVelocity = m_direction * m_speed;

            // Rotate to face direction
            float angle = Mathf.Atan2(m_direction.y, m_direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Set color
            Color useColor = color ?? m_projectileColor;
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.color = useColor;
            }
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
                m_trailRenderer.startColor = useColor;
                m_trailRenderer.endColor = new Color(useColor.r, useColor.g, useColor.b, 0f);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Mark as orphaned (enemy that fired this died)
        /// </summary>
        public void MarkOrphaned()
        {
            m_isOrphaned = true;
        }

        private void Update()
        {
            if (!m_isActive) return;

            // Check lifetime
            if (Time.time - m_spawnTime > m_lifetime)
            {
                ReturnToPool();
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!m_isActive) return;

            // Check player hit
            if (other.CompareTag("Player"))
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    // Only damage player if alive
                    playerHealth.TakeDamage(m_damage, transform.position);
                }

                ReturnToPool();
            }
            // Check wall/boundary collision - use layer or name check instead of tag
            // to avoid "Tag not defined" errors
            else if (other.gameObject.layer == LayerMask.NameToLayer("Boundary") ||
                     other.gameObject.name.Contains("Boundary") ||
                     other.gameObject.name.Contains("Arena"))
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (!m_isActive) return;

            m_isActive = false;
            m_rb.linearVelocity = Vector2.zero;

            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
            }

            gameObject.SetActive(false);
            m_returnToPool?.Invoke(this);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
        public void OnReturnToPool()
        {
            m_isActive = false;
            m_isOrphaned = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_collisionRadius);
        }
    }
}
