using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Enemy lifecycle states - matches TypeScript EnemyState
    /// </summary>
    public enum EnemyState
    {
        Spawning,   // Spawn animation, invulnerable
        Alive,      // Normal gameplay
        Dying,      // Death animation
        Dead        // Ready to return to pool
    }

    /// <summary>
    /// Abstract base class for all enemies.
    /// Based on TypeScript Enemy.ts.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] protected int _maxHealth = 1;
        [SerializeField] protected float _speed = 1.5f;
        [SerializeField] protected int _damage = 5;
        [SerializeField] protected int _xpValue = 1;
        [SerializeField] protected int _scoreValue = 100;
        [SerializeField] protected float _collisionRadius = 0.5f;

        [Header("Spawn Settings")]
        [SerializeField] protected float _spawnDuration = 0.25f;
        [SerializeField] protected bool _invulnerableDuringSpawn = true;

        [Header("Death Settings")]
        [SerializeField] protected float _deathDuration = 0.5f;

        // Note: MMFeedbacks removed

        // State
        protected EnemyState _state = EnemyState.Dead;
        protected int _currentHealth;
        protected float _stateTimer;

        // Target reference
        protected Transform _playerTarget;

        // Pool callback
        protected System.Action<EnemyBase> _returnToPool;

        // Public accessors
        public EnemyState State => _state;
        public bool IsAlive => _state == EnemyState.Alive;
        public bool IsActive => _state == EnemyState.Spawning || _state == EnemyState.Alive;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public float HealthPercent => (float)_currentHealth / _maxHealth;
        public float CollisionRadius => _collisionRadius;
        public int Damage => _damage;
        public abstract EnemyType EnemyType { get; }

        /// <summary>
        /// Get the config for this enemy type (for subclasses to access shooting params)
        /// </summary>
        protected EnemyTypeConfig EnemyConfig => ConfigProvider.Balance?.GetEnemyConfig(EnemyType);

        #region Lifecycle

        /// <summary>
        /// Initialize enemy when spawned from pool
        /// </summary>
        public virtual void Initialize(Vector2 position, Transform playerTarget,
            System.Action<EnemyBase> returnToPool)
        {
            if (playerTarget == null)
            {
                Debug.LogError("[EnemyBase] Cannot initialize - playerTarget is null!");
                return;
            }

            if (returnToPool == null)
            {
                Debug.LogError("[EnemyBase] Cannot initialize - returnToPool callback is null!");
                return;
            }

            transform.position = position;
            _playerTarget = playerTarget;
            _returnToPool = returnToPool;

            // Apply config-driven values
            ApplyConfigValues();

            // Auto-create feedbacks if not assigned (runtime juiciness)
            EnsureFeedbacks();

            _currentHealth = _maxHealth;
            _stateTimer = _spawnDuration;

            SetState(EnemyState.Spawning);
            // Feedback (Feel removed)

            // Initialize elite modifier if present
            var eliteModifier = GetComponent<EliteModifier>();
            if (eliteModifier != null)
            {
                eliteModifier.InitializeElite();
            }

            OnInitialize();
        }

        /// <summary>
        /// Create feedbacks at runtime if not assigned in inspector
        /// </summary>
        protected virtual void EnsureFeedbacks()
        {
            // Note: MMFeedbacks removed - FeedbackSetup no longer available
        }

        /// <summary>
        /// Apply config-driven values to this enemy
        /// </summary>
        protected virtual void ApplyConfigValues()
        {
            var config = ConfigProvider.Balance?.GetEnemyConfig(EnemyType);
            if (config == null) return;

            // Apply stats from config
            _maxHealth = config.health;
            _speed = config.speed;
            _damage = config.contactDamage;
            _xpValue = config.xpValue;
            _scoreValue = config.scoreValue;
            _collisionRadius = config.collisionRadius;
            _spawnDuration = config.spawnDuration;
            _deathDuration = config.deathDuration;

            // Apply collision radius to CircleCollider2D if present
            var circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                circleCollider.radius = _collisionRadius;
                circleCollider.isTrigger = true; // Must be trigger for OnTriggerEnter2D
            }

            // Scale visual to match collision radius (visual is typically 2x the collision)
            float visualScale = _collisionRadius * 2f;
            transform.localScale = Vector3.one * visualScale;

            // Apply generated sprite based on enemy type
            ApplyGeneratedSprite(config.color);
        }

        /// <summary>
        /// Apply a procedurally generated sprite based on enemy type
        /// </summary>
        protected virtual void ApplyGeneratedSprite(Color color)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // Generate different shapes for different enemy types
            Sprite sprite = EnemyType switch
            {
                EnemyType.DataMite => Graphics.SpriteGenerator.CreateCircle(64, color, "DataMite"),
                EnemyType.ScanDrone => Graphics.SpriteGenerator.CreateHexagon(64, color, "ScanDrone"),
                EnemyType.Fizzer => Graphics.SpriteGenerator.CreateDiamond(64, color, "Fizzer"),
                EnemyType.UFO => Graphics.SpriteGenerator.CreateCircle(64, color, "UFO"),
                EnemyType.ChaosWorm => Graphics.SpriteGenerator.CreateStar(64, 6, color, "ChaosWorm"),
                EnemyType.VoidSphere => Graphics.SpriteGenerator.CreateGlow(64, color, "VoidSphere"),
                EnemyType.CrystalShard => Graphics.SpriteGenerator.CreateDiamond(64, color, "CrystalShard"),
                EnemyType.Boss => Graphics.SpriteGenerator.CreateStar(64, 8, color, "Boss"),
                _ => Graphics.SpriteGenerator.CreateCircle(64, color, "Default")
            };

            sr.sprite = sprite;
            sr.color = Color.white; // Sprite already has color baked in
        }

        /// <summary>
        /// Override for enemy-specific initialization
        /// </summary>
        protected virtual void OnInitialize() { }

        protected virtual void Update()
        {
            switch (_state)
            {
                case EnemyState.Spawning:
                    UpdateSpawning();
                    break;
                case EnemyState.Alive:
                    UpdateAlive();
                    break;
                case EnemyState.Dying:
                    UpdateDying();
                    break;
            }
        }

        protected virtual void UpdateSpawning()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                SetState(EnemyState.Alive);
            }
        }

        protected virtual void UpdateAlive()
        {
            if (_playerTarget == null) return;

            // Override in subclasses for AI behavior
            UpdateAI();

            // Apply separation from other enemies to prevent overlap
            ApplyEnemySeparation();
        }

        /// <summary>
        /// Push away from nearby enemies to prevent overlapping
        /// </summary>
        protected virtual void ApplyEnemySeparation()
        {
            float separationRadius = _collisionRadius * 2.5f;
            float separationStrength = 3f;
            Vector2 separationForce = Vector2.zero;

            var colliders = Physics2D.OverlapCircleAll(transform.position, separationRadius);
            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;

                var otherEnemy = col.GetComponent<EnemyBase>();
                if (otherEnemy == null || !otherEnemy.IsActive) continue;

                Vector2 toThis = (Vector2)transform.position - (Vector2)otherEnemy.transform.position;
                float distance = toThis.magnitude;
                float minDist = _collisionRadius + otherEnemy.CollisionRadius;

                if (distance < minDist && distance > 0.01f)
                {
                    // Push apart based on overlap amount
                    float overlap = minDist - distance;
                    separationForce += toThis.normalized * overlap * separationStrength;
                }
            }

            if (separationForce.sqrMagnitude > 0.01f)
            {
                transform.position += (Vector3)(separationForce * Time.deltaTime);
            }
        }

        protected virtual void UpdateDying()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                SetState(EnemyState.Dead);
                _returnToPool?.Invoke(this);
            }
        }

        /// <summary>
        /// Override for enemy-specific AI behavior
        /// </summary>
        protected abstract void UpdateAI();

        #endregion

        #region Damage & Death

        /// <summary>
        /// Take damage from a projectile or other source
        /// </summary>
        public virtual void TakeDamage(int damage, Vector2 damageSource)
        {
            if (damage < 0)
            {
                Debug.LogError($"[EnemyBase] Invalid damage value: {damage}. Must be >= 0.");
                return;
            }

            // Can't damage during spawn or already dying
            if (_state == EnemyState.Spawning && _invulnerableDuringSpawn)
            {
                Debug.LogWarning($"[EnemyBase] Cannot damage {EnemyType} - invulnerable during spawn!");
                return;
            }

            if (_state != EnemyState.Alive)
            {
                Debug.LogWarning($"[EnemyBase] Cannot damage {EnemyType} - not alive (state: {_state})!");
                return;
            }

            // Check elite modifier for damage blocking (shields, etc.)
            var eliteModifier = GetComponent<EliteModifier>();
            if (eliteModifier != null && eliteModifier.OnTakeDamage(damage, damageSource))
            {
                // Damage was blocked by elite ability
                // Feedback (Feel removed)
                return;
            }

            _currentHealth -= damage;

            // Feedback (Feel removed)

            // Trigger hit flash effect
            var hitFlash = GetComponent<Graphics.HitFlashEffect>();
            if (hitFlash != null)
            {
                hitFlash.Flash();
            }

            EventBus.Publish(new EnemyDamagedEvent
            {
                enemyType = EnemyType,
                damage = damage,
                currentHealth = _currentHealth,
                position = transform.position
            });

            if (_currentHealth <= 0)
            {
                Kill();
            }
        }

        /// <summary>
        /// Kill the enemy
        /// </summary>
        public virtual void Kill()
        {
            if (_state == EnemyState.Dying || _state == EnemyState.Dead) return;

            _stateTimer = _deathDuration;
            SetState(EnemyState.Dying);

            // Feedback (Feel removed)

            // Notify elite modifier of death (for splitter, etc.)
            var eliteModifier = GetComponent<EliteModifier>();
            if (eliteModifier != null)
            {
                eliteModifier.OnDeath();
            }

            // Publish kill event for scoring (elite enemies give bonus XP/score)
            int finalScore = _scoreValue;
            int finalXP = _xpValue;
            if (eliteModifier != null && eliteModifier.IsElite)
            {
                finalScore = Mathf.RoundToInt(finalScore * 2f);
                finalXP = Mathf.RoundToInt(finalXP * 2f);
            }

            EventBus.Publish(new EnemyKilledEvent
            {
                enemyType = EnemyType,
                position = transform.position,
                scoreValue = finalScore,
                xpValue = finalXP
            });
        }

        /// <summary>
        /// Instant kill without animation (for level clear)
        /// </summary>
        public virtual void KillInstant()
        {
            SetState(EnemyState.Dead);
            _returnToPool?.Invoke(this);
        }

        #endregion

        #region State Management

        protected void SetState(EnemyState newState)
        {
            if (_state == newState) return;

            // Validate state transitions
            if (_state == EnemyState.Dead && newState != EnemyState.Spawning)
            {
                Debug.LogWarning($"[EnemyBase] Invalid state transition: {_state} -> {newState}. Dead enemies can only transition to Spawning!");
                return;
            }

            _state = newState;
            OnStateChanged(newState);
        }

        protected virtual void OnStateChanged(EnemyState newState)
        {
            // Override for state-specific visuals
        }

        #endregion

        #region Collision

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAlive) return;

            if (other == null)
            {
                Debug.LogWarning("[EnemyBase] OnTriggerEnter2D - other collider is null!");
                return;
            }

            // Check player collision
            if (other.CompareTag("Player"))
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    // Player takes damage from enemy contact (only if alive)
                    playerHealth.TakeDamage(_damage, transform.position);
                }
                else if (playerHealth == null)
                {
                    Debug.LogWarning($"[EnemyBase] Player object '{other.name}' has no PlayerHealth component!");
                }

                // Enemy dies on contact with player (TypeScript behavior)
                // This gives the satisfying "ramming" feel where both take damage
                Kill();
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (!IsActive) return;

            // Enemy-to-enemy soft collision (separation)
            if (other.CompareTag("Enemy"))
            {
                var otherEnemy = other.GetComponent<EnemyBase>();
                if (otherEnemy != null && otherEnemy.IsActive)
                {
                    ApplyEnemySeparation(otherEnemy);
                }
            }
        }

        /// <summary>
        /// Push enemies apart when overlapping (soft collision)
        /// </summary>
        protected virtual void ApplyEnemySeparation(EnemyBase other)
        {
            Vector2 toOther = (Vector2)(other.transform.position - transform.position);
            float distance = toOther.magnitude;
            float minDistance = _collisionRadius + other.CollisionRadius;

            // Only push if overlapping
            if (distance < minDistance && distance > 0.001f)
            {
                // Push strength based on overlap amount
                float overlap = minDistance - distance;
                float separationStrength = 2f; // Adjust for feel
                Vector2 pushDir = -toOther.normalized;

                // Move this enemy away (other enemy will handle its own push)
                transform.position += (Vector3)(pushDir * overlap * separationStrength * 0.5f * Time.deltaTime);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get direction to player
        /// </summary>
        protected Vector2 GetDirectionToPlayer()
        {
            if (_playerTarget == null) return Vector2.zero;
            return ((Vector2)_playerTarget.position - (Vector2)transform.position).normalized;
        }

        /// <summary>
        /// Get distance to player
        /// </summary>
        protected float GetDistanceToPlayer()
        {
            if (_playerTarget == null) return float.MaxValue;
            return Vector2.Distance(transform.position, _playerTarget.position);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
        public virtual void OnReturnToPool()
        {
            _state = EnemyState.Dead;
            _playerTarget = null;

            // Reset elite modifier
            var eliteModifier = GetComponent<EliteModifier>();
            if (eliteModifier != null)
            {
                eliteModifier.Reset();
            }
        }

        #endregion

        #region Debug

        protected virtual void OnDrawGizmosSelected()
        {
            // Collision radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _collisionRadius);

            // Direction to player
            if (_playerTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _playerTarget.position);
            }
        }

        #endregion
    }
}
