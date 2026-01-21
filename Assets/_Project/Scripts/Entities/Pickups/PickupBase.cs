using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

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
        [SerializeField] protected float _lifetime = 30f;
        [SerializeField] protected float _flashStartTime = 5f;
        [SerializeField] protected float _flashSpeed = 10f;

        [Header("Magnetism")]
        [SerializeField] protected float _magnetRadius = 5f;
        [SerializeField] protected float _magnetStrength = 16f;
        [SerializeField] protected float _maxMagnetSpeed = 18f;

        [Header("Collection")]
        [SerializeField] protected float _collectionRadius = 1f;

        [Header("Visual")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected float _bobSpeed = 2f;
        [SerializeField] protected float _bobAmount = 0.15f;
        [SerializeField] protected float _rotateSpeed = 90f;

        [Header("Feel Feedbacks")]
        [SerializeField] protected MMF_Player _spawnFeedback;
        [SerializeField] protected MMF_Player _collectFeedback;

        // State
        protected Transform _playerTarget;
        protected System.Action<PickupBase> _returnToPoolCallback;
        protected float _lifeTimer;
        protected bool _isCollected;
        protected Vector2 _startPosition;
        protected float _bobPhase;

        // Abstract members
        public abstract PickupType PickupType { get; }
        protected abstract void ApplyEffect(GameObject player);
        protected abstract Color GetPickupColor();

        // Public accessors
        public bool IsActive => gameObject.activeInHierarchy && !_isCollected;
        public Vector2 Position => transform.position;

        /// <summary>
        /// Initialize pickup when spawned from pool
        /// </summary>
        public virtual void Initialize(Vector2 position, Transform playerTarget, System.Action<PickupBase> returnCallback)
        {
            transform.position = position;
            _startPosition = position;
            _playerTarget = playerTarget;
            _returnToPoolCallback = returnCallback;
            _lifeTimer = _lifetime;
            _isCollected = false;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);

            gameObject.SetActive(true);

            // Apply generated sprite and color
            ApplyGeneratedSprite();

            _spawnFeedback?.PlayFeedbacks();
        }

        /// <summary>
        /// Apply procedurally generated sprite based on pickup type
        /// </summary>
        protected virtual void ApplyGeneratedSprite()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null) return;
            }

            Color color = GetPickupColor();

            // All pickups use glow sprites for a nice effect
            var sprite = Graphics.SpriteGenerator.CreateGlow(64, color, $"Pickup_{PickupType}");
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = Color.white; // Color is baked into sprite
        }

        protected virtual void Update()
        {
            if (_isCollected) return;

            UpdateLifetime();
            UpdateMagnetism();
            UpdateVisuals();
            CheckCollection();
        }

        #region Lifetime

        protected virtual void UpdateLifetime()
        {
            _lifeTimer -= Time.deltaTime;

            // Flash when about to expire
            if (_lifeTimer <= _flashStartTime && _spriteRenderer != null)
            {
                float flash = Mathf.Sin(Time.time * _flashSpeed);
                Color c = GetPickupColor();
                c.a = 0.5f + (flash + 1f) * 0.25f;
                _spriteRenderer.color = c;
            }

            // Expire
            if (_lifeTimer <= 0f)
            {
                ReturnToPool();
            }
        }

        #endregion

        #region Magnetism

        protected virtual void UpdateMagnetism()
        {
            if (_playerTarget == null) return;

            float distance = Vector2.Distance(transform.position, _playerTarget.position);

            if (distance <= _magnetRadius && distance > 0.01f)
            {
                // Calculate pull strength (stronger when closer)
                float pullFactor = 1f - (distance / _magnetRadius);
                pullFactor = pullFactor * pullFactor; // Quadratic falloff

                // Direction to player
                Vector2 direction = ((Vector2)_playerTarget.position - (Vector2)transform.position).normalized;

                // Apply magnetism
                float pullSpeed = Mathf.Min(_magnetStrength * pullFactor, _maxMagnetSpeed);
                transform.position = (Vector2)transform.position + direction * pullSpeed * Time.deltaTime;
            }
        }

        #endregion

        #region Visuals

        protected virtual void UpdateVisuals()
        {
            // Bob up and down
            _bobPhase += Time.deltaTime * _bobSpeed;
            float bob = Mathf.Sin(_bobPhase) * _bobAmount;

            // Only bob, don't override magnetism position
            // Visual bob effect handled by child sprite or offset

            // Rotate
            if (_rotateSpeed > 0)
            {
                transform.Rotate(0, 0, _rotateSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Collection

        protected virtual void CheckCollection()
        {
            if (_playerTarget == null) return;

            float distance = Vector2.Distance(transform.position, _playerTarget.position);

            if (distance <= _collectionRadius)
            {
                Collect();
            }
        }

        protected virtual void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;

            // Apply the pickup effect
            ApplyEffect(_playerTarget.gameObject);

            // Play feedback
            _collectFeedback?.PlayFeedbacks();

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
            _isCollected = true;
            gameObject.SetActive(false);
        }

        protected virtual void ReturnToPool()
        {
            _returnToPoolCallback?.Invoke(this);
        }

        #endregion

        #region Collision (Alternative collection method)

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }

        #endregion

        #region Debug

        protected virtual void OnDrawGizmosSelected()
        {
            // Collection radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _collectionRadius);

            // Magnet radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _magnetRadius);
        }

        #endregion
    }
}
