using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.Config;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Base class for all pickup types.
    /// Handles magnetism, collection, and pooling.
    /// Based on TypeScript PickupManager.ts pattern.
    /// </summary>
    public abstract class PickupBase : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] protected float m_flashStartTime = 5f;
        [SerializeField] protected float m_flashSpeed = 10f;

        // Config-driven properties (read from GameBalanceConfig based on pickup type)
        protected float Lifetime => GetPickupConfig()?.lifetime ?? 15f;
        protected float MagnetRadius => GetPickupConfig()?.magnetRadius ?? 5f;
        protected float MagnetStrength => GetPickupConfig()?.magnetStrength ?? 16f;
        protected float MaxMagnetSpeed => GetPickupConfig()?.maxMagnetSpeed ?? 18f;

        /// <summary>
        /// Get config for this pickup type from GameBalanceConfig
        /// </summary>
        protected virtual PickupConfig GetPickupConfig()
        {
            return ConfigProvider.Balance?.GetPickupConfig(PickupType);
        }

        [Header("Collection")]
        [SerializeField] protected float m_collectionRadius = 1f;

        [Header("Visual")]
        [SerializeField] protected SpriteRenderer m_spriteRenderer;
        [SerializeField] protected float m_bobSpeed = 2f;
        [SerializeField] protected float m_bobAmount = 0.15f;
        [SerializeField] protected float m_rotateSpeed = 90f;

        // Note: MMFeedbacks removed

        // State
        protected Transform m_playerTarget;
        protected System.Action<PickupBase> m_returnToPoolCallback;
        protected float m_lifeTimer;
        protected bool m_isCollected;
        protected Vector2 m_startPosition;
        protected float m_bobPhase;

        // Abstract members
        public abstract PickupType PickupType { get; }
        protected abstract void ApplyEffect(GameObject player);
        protected abstract Color GetPickupColor();

        // Public accessors
        public bool IsActive => gameObject.activeInHierarchy && !m_isCollected;
        public Vector2 Position => transform.position;

        /// <summary>
        /// Initialize pickup when spawned from pool
        /// </summary>
        public virtual void Initialize(Vector2 position, Transform playerTarget, System.Action<PickupBase> returnCallback)
        {
            if (playerTarget == null)
            {
                Debug.LogError("[PickupBase] Cannot initialize - playerTarget is null!");
                return;
            }

            if (returnCallback == null)
            {
                Debug.LogError("[PickupBase] Cannot initialize - returnCallback is null!");
                return;
            }

            float lifetime = Lifetime;
            if (lifetime <= 0)
            {
                Debug.LogWarning($"[PickupBase] Invalid lifetime: {lifetime}. Using default of 15s.");
                lifetime = 15f;
            }

            transform.position = position;
            m_startPosition = position;
            m_playerTarget = playerTarget;
            m_returnToPoolCallback = returnCallback;
            m_lifeTimer = lifetime;
            m_isCollected = false;
            m_bobPhase = Random.Range(0f, Mathf.PI * 2f);

            gameObject.SetActive(true);

            // Apply generated sprite and color
            ApplyGeneratedSprite();

            // Feedback (Feel removed)
        }

        /// <summary>
        /// Apply procedurally generated sprite based on pickup type
        /// </summary>
        protected virtual void ApplyGeneratedSprite()
        {
            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = GetComponent<SpriteRenderer>();
                if (m_spriteRenderer == null) return;
            }

            Color color = GetPickupColor();

            // All pickups use glow sprites for a nice effect
            var sprite = Graphics.SpriteGenerator.CreateGlow(64, color, $"Pickup_{PickupType}");
            m_spriteRenderer.sprite = sprite;
            m_spriteRenderer.color = Color.white; // Color is baked into sprite
        }

        protected virtual void Update()
        {
            if (m_isCollected) return;

            UpdateLifetime();
            UpdateMagnetism();
            UpdateVisuals();
            CheckCollection();
        }

        #region Lifetime

        protected virtual void UpdateLifetime()
        {
            m_lifeTimer -= Time.deltaTime;

            // Flash when about to expire
            if (m_lifeTimer <= m_flashStartTime && m_spriteRenderer != null)
            {
                float flash = Mathf.Sin(Time.time * m_flashSpeed);
                Color c = GetPickupColor();
                c.a = 0.5f + (flash + 1f) * 0.25f;
                m_spriteRenderer.color = c;
            }

            // Expire
            if (m_lifeTimer <= 0f)
            {
                ReturnToPool();
            }
        }

        #endregion

        #region Magnetism

        protected virtual void UpdateMagnetism()
        {
            if (m_playerTarget == null) return;

            float distance = Vector2.Distance(transform.position, m_playerTarget.position);
            float magnetRadius = MagnetRadius;

            if (distance <= magnetRadius && distance > 0.01f)
            {
                // Calculate pull strength (stronger when closer)
                float pullFactor = 1f - (distance / magnetRadius);
                pullFactor = pullFactor * pullFactor; // Quadratic falloff

                // Direction to player
                Vector2 direction = ((Vector2)m_playerTarget.position - (Vector2)transform.position).normalized;

                // Apply magnetism (config-driven)
                float pullSpeed = Mathf.Min(MagnetStrength * pullFactor, MaxMagnetSpeed);
                transform.position = (Vector2)transform.position + direction * pullSpeed * Time.deltaTime;
            }
        }

        #endregion

        #region Visuals

        protected virtual void UpdateVisuals()
        {
            // Bob up and down
            m_bobPhase += Time.deltaTime * m_bobSpeed;
            float bob = Mathf.Sin(m_bobPhase) * m_bobAmount;

            // Only bob, don't override magnetism position
            // Visual bob effect handled by child sprite or offset

            // Rotate
            if (m_rotateSpeed > 0)
            {
                transform.Rotate(0, 0, m_rotateSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Collection

        protected virtual void CheckCollection()
        {
            if (m_playerTarget == null) return;

            float distance = Vector2.Distance(transform.position, m_playerTarget.position);

            if (distance <= m_collectionRadius)
            {
                Collect();
            }
        }

        protected virtual void Collect()
        {
            CollectByPlayer(m_playerTarget?.gameObject);
        }

        /// <summary>
        /// Collect pickup with explicit player reference (used by trigger collision)
        /// </summary>
        protected virtual void CollectByPlayer(GameObject player)
        {
            if (m_isCollected)
            {
                Debug.LogWarning($"[PickupBase] Pickup {PickupType} already collected!");
                return;
            }

            if (player == null)
            {
                Debug.LogError("[PickupBase] Cannot collect - player is null!");
                return;
            }

            m_isCollected = true;

            try
            {
                // Apply the pickup effect
                ApplyEffect(player);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PickupBase] Error applying pickup effect for {PickupType}: {ex.Message}");
            }

            // Feedback (Feel removed)

            // Publish event
            EventBus.Publish(new PickupCollectedEvent
            {
                pickupType = PickupType,
                position = transform.position
            });

            Debug.Log($"[Pickup] Collected {PickupType}");

            ReturnToPool();
        }

        #endregion

        #region Pool Management

        public virtual void OnReturnToPool()
        {
            m_isCollected = true;
            gameObject.SetActive(false);
        }

        protected virtual void ReturnToPool()
        {
            m_returnToPoolCallback?.Invoke(this);
        }

        #endregion

        #region Collision (Alternative collection method)

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (m_isCollected) return;

            if (other == null)
            {
                Debug.LogWarning("[PickupBase] OnTriggerEnter2D - other collider is null!");
                return;
            }

            if (other.CompareTag("Player"))
            {
                // Pass the player directly instead of relying on cached _playerTarget
                CollectByPlayer(other.gameObject);
            }
        }

        #endregion

        #region Debug

        protected virtual void OnDrawGizmosSelected()
        {
            // Collection radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, m_collectionRadius);

            // Magnet radius (config-driven)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, MagnetRadius);
        }

        #endregion
    }
}
