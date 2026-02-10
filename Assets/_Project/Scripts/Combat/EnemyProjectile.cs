using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Graphics;

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
        [SerializeField] private Vector3 m_visualScale = new Vector3(0.5f, 0.5f, 1f); // Make bullets more visible
        [SerializeField] private bool m_enableGlow = true;
        [SerializeField] private float m_glowIntensity = 2f;

        // Components
        private Rigidbody2D m_rb;

        // Cached boundary layer (avoids LayerMask.NameToLayer + .name string alloc per collision)
        private static int s_boundaryLayer = -1;
        private static bool s_boundaryLayerCached;

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

            // Cache boundary layer once (avoids LayerMask.NameToLayer string alloc per collision)
            if (!s_boundaryLayerCached)
            {
                s_boundaryLayer = LayerMask.NameToLayer("Boundary");
                s_boundaryLayerCached = true;
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
                // Apply glow intensity to make bullets more visible
                Color displayColor = m_enableGlow ? useColor * m_glowIntensity : useColor;
                displayColor.a = 1f; // Keep alpha at 1
                m_spriteRenderer.color = displayColor;
            }
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
                m_trailRenderer.startColor = useColor;
                m_trailRenderer.endColor = new Color(useColor.r, useColor.g, useColor.b, 0f);
            }

            // Set scale for visibility
            transform.localScale = m_visualScale;

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
                // TryGetComponent is zero-alloc (unlike GetComponent which may allocate on null)
                other.TryGetComponent<PlayerHealth>(out var playerHealth);
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    // Only damage player if alive
                    playerHealth.TakeDamage(m_damage, transform.position);
                }

                ReturnToPool();
            }
            // Check wall/boundary collision - use cached layer + component check (zero-alloc)
            // Replaces .name.Contains() which allocates a new string per call (~40-80 bytes)
            else if ((s_boundaryLayer >= 0 && other.gameObject.layer == s_boundaryLayer) ||
                     other.TryGetComponent<ArenaBoundary>(out _))
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
