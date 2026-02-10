using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Audio;
using NeuralBreak.Graphics;
using Z13.Core;

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
        [SerializeField] protected int m_maxHealth = 1;
        [SerializeField] protected float m_speed = 1.5f;
        [SerializeField] protected int m_damage = 5;
        [SerializeField] protected int m_xpValue = 1;
        [SerializeField] protected int m_scoreValue = 100;
        [SerializeField] protected float m_collisionRadius = 0.5f;

        [Header("Spawn Settings")]
        [SerializeField] protected float m_spawnDuration = 0.25f;
        [SerializeField] protected bool m_invulnerableDuringSpawn = true;
        [SerializeField] protected AnimationCurve m_spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] protected bool m_enableSpawnParticles = true;
        [SerializeField] protected bool m_enableSpawnSound = true;

        [Header("Death Settings")]
        [SerializeField] protected float m_deathDuration = 0.5f;
        [SerializeField] protected AnimationCurve m_deathScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] protected bool m_enableDeathParticles = true;
        [SerializeField] protected bool m_enableDeathSound = true;

        // State
        protected EnemyState m_state = EnemyState.Dead;
        protected int m_currentHealth;
        protected float m_stateTimer;
        protected Vector3 m_targetScale; // Target scale after spawn animation

        // Target reference
        protected Transform m_playerTarget;

        // Pool callback
        protected System.Action<EnemyBase> m_returnToPool;

        // Cached component references (avoid GetComponent in hot paths)
        private EliteModifier m_eliteModifier;
        private Graphics.HitFlashEffect m_hitFlash;
        private CircleCollider2D m_circleCollider;
        protected SpriteRenderer m_spriteRenderer; // Protected so subclasses can access
        private SpriteRenderer[] m_childRenderers;

        // Static cached reference for VFXManager (avoids FindFirstObjectByType per spawn/death)
        private static VFXManager s_cachedVFXManager;

        protected virtual void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            m_eliteModifier = GetComponent<EliteModifier>();
            m_hitFlash = GetComponent<Graphics.HitFlashEffect>();
            m_circleCollider = GetComponent<CircleCollider2D>();
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            m_childRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        // Public accessors
        public EnemyState State => m_state;
        public bool IsAlive => m_state == EnemyState.Alive;
        public bool IsActive => m_state == EnemyState.Spawning || m_state == EnemyState.Alive;
        public int CurrentHealth => m_currentHealth;
        public int MaxHealth => m_maxHealth;
        public float HealthPercent => (float)m_currentHealth / m_maxHealth;
        public float CollisionRadius => m_collisionRadius;
        public int Damage => m_damage;
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
            m_playerTarget = playerTarget;
            m_returnToPool = returnToPool;

            // Apply config-driven values
            ApplyConfigValues();

            // Auto-create feedbacks if not assigned (runtime juiciness)
            EnsureFeedbacks();

            m_currentHealth = m_maxHealth;
            m_stateTimer = m_spawnDuration;

            // Start spawn animation from scale 0
            transform.localScale = Vector3.zero;

            SetState(EnemyState.Spawning);

            // Play spawn effects
            PlaySpawnEffects();

            // Initialize elite modifier if present
            if (m_eliteModifier != null)
            {
                m_eliteModifier.InitializeElite();
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
            m_maxHealth = config.health;
            m_speed = config.speed;
            m_damage = config.contactDamage;
            m_xpValue = config.xpValue;
            m_scoreValue = config.scoreValue;
            m_collisionRadius = config.collisionRadius;
            m_spawnDuration = config.spawnDuration;
            m_deathDuration = config.deathDuration;

            // Apply collision radius to CircleCollider2D if present
            if (m_circleCollider != null)
            {
                m_circleCollider.radius = m_collisionRadius;
                m_circleCollider.isTrigger = true;
            }

            // Scale visual to match collision radius (visual is typically 2x the collision)
            float visualScale = m_collisionRadius * 2f;
            m_targetScale = Vector3.one * visualScale;

            // Apply generated sprite based on enemy type
            ApplyGeneratedSprite(config.color);
        }

        /// <summary>
        /// Apply a procedurally generated sprite based on enemy type
        /// </summary>
        protected virtual void ApplyGeneratedSprite(Color color)
        {
            if (m_spriteRenderer == null) return;
            var sr = m_spriteRenderer;

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
            switch (m_state)
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
            m_stateTimer -= Time.deltaTime;

            // Animate scale up from 0 to target
            float progress = 1f - (m_stateTimer / m_spawnDuration);
            progress = Mathf.Clamp01(progress);

            // Apply spawn scale curve
            float curveValue = m_spawnScaleCurve.Evaluate(progress);
            transform.localScale = m_targetScale * curveValue;

            if (m_stateTimer <= 0f)
            {
                // Ensure final scale is exact
                transform.localScale = m_targetScale;
                SetState(EnemyState.Alive);
            }
        }

        protected virtual void UpdateAlive()
        {
            if (m_playerTarget == null) return;

            // Override in subclasses for AI behavior
            UpdateAI();

            // Enemy separation is handled by OnTriggerStay2D (zero-alloc, uses Unity broadphase)
        }

        protected virtual void UpdateDying()
        {
            m_stateTimer -= Time.deltaTime;

            // Animate scale down from target to 0 (or custom death animation)
            float progress = 1f - (m_stateTimer / m_deathDuration);
            progress = Mathf.Clamp01(progress);

            // Apply death scale curve
            float curveValue = m_deathScaleCurve.Evaluate(progress);
            transform.localScale = m_targetScale * curveValue;

            if (m_stateTimer <= 0f)
            {
                SetState(EnemyState.Dead);
                m_returnToPool?.Invoke(this);
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
            if (m_state == EnemyState.Spawning && m_invulnerableDuringSpawn)
            {
                Debug.LogWarning($"[EnemyBase] Cannot damage {EnemyType} - invulnerable during spawn!");
                return;
            }

            if (m_state != EnemyState.Alive)
            {
                Debug.LogWarning($"[EnemyBase] Cannot damage {EnemyType} - not alive (state: {m_state})!");
                return;
            }

            // Check elite modifier for damage blocking (shields, etc.)
            if (m_eliteModifier != null && m_eliteModifier.OnTakeDamage(damage, damageSource))
            {
                return;
            }

            m_currentHealth -= damage;

            // Trigger hit flash effect
            if (m_hitFlash != null)
            {
                m_hitFlash.Flash();
            }

            EventBus.Publish(new EnemyDamagedEvent
            {
                enemyType = EnemyType,
                damage = damage,
                currentHealth = m_currentHealth,
                position = transform.position
            });

            if (m_currentHealth <= 0)
            {
                Kill();
            }
        }

        /// <summary>
        /// Kill the enemy
        /// </summary>
        public virtual void Kill()
        {
            if (m_state == EnemyState.Dying || m_state == EnemyState.Dead) return;

            m_stateTimer = m_deathDuration;
            SetState(EnemyState.Dying);

            // Notify elite modifier of death (for splitter, etc.)
            if (m_eliteModifier != null)
            {
                m_eliteModifier.OnDeath();
            }

            // Publish kill event for scoring (elite enemies give bonus XP/score)
            int finalScore = m_scoreValue;
            int finalXP = m_xpValue;
            if (m_eliteModifier != null && m_eliteModifier.IsElite)
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
            m_returnToPool?.Invoke(this);
        }

        #endregion

        #region State Management

        protected void SetState(EnemyState newState)
        {
            if (m_state == newState) return;

            // Validate state transitions
            if (m_state == EnemyState.Dead && newState != EnemyState.Spawning)
            {
                Debug.LogWarning($"[EnemyBase] Invalid state transition: {m_state} -> {newState}. Dead enemies can only transition to Spawning!");
                return;
            }

            m_state = newState;
            OnStateChanged(newState);
        }

        protected virtual void OnStateChanged(EnemyState newState)
        {
            switch (newState)
            {
                case EnemyState.Spawning:
                    ShowVisuals();
                    break;

                case EnemyState.Alive:
                    ShowVisuals();
                    // Play signature "alive" sound effect
                    PlayAliveSound();
                    break;

                case EnemyState.Dying:
                    // Keep visuals visible during death animation (scale down effect)
                    // Death particles will be spawned separately
                    PlayDeathEffects();
                    break;

                case EnemyState.Dead:
                    HideVisuals();
                    break;
            }
        }

        /// <summary>
        /// Hide enemy visuals (called when entering Dying state)
        /// </summary>
        protected virtual void HideVisuals()
        {
            if (m_spriteRenderer != null) m_spriteRenderer.enabled = false;

            if (m_childRenderers != null)
            {
                for (int i = 0; i < m_childRenderers.Length; i++)
                {
                    if (m_childRenderers[i] != null) m_childRenderers[i].enabled = false;
                }
            }
        }

        /// <summary>
        /// Show enemy visuals (called when spawning/alive)
        /// </summary>
        protected virtual void ShowVisuals()
        {
            if (m_spriteRenderer != null) m_spriteRenderer.enabled = true;

            if (m_childRenderers != null)
            {
                for (int i = 0; i < m_childRenderers.Length; i++)
                {
                    if (m_childRenderers[i] != null) m_childRenderers[i].enabled = true;
                }
            }
        }

        #endregion

        #region Visual & Audio Effects

        /// <summary>
        /// Play spawn effects (particles and sound).
        /// Override in subclasses for custom spawn effects.
        /// </summary>
        protected virtual void PlaySpawnEffects()
        {
            // Spawn particles
            if (m_enableSpawnParticles)
            {
                if (s_cachedVFXManager == null)
                    s_cachedVFXManager = FindFirstObjectByType<VFXManager>();
                if (s_cachedVFXManager != null)
                {
                    // Use small explosion effect for spawn (reuse existing system)
                    s_cachedVFXManager.PlayExplosion(transform.position, Graphics.ExplosionSize.Small, GetEnemyColor());
                }
            }

            // Spawn sound
            if (m_enableSpawnSound)
            {
                PlaySpawnSound();
            }
        }

        /// <summary>
        /// Play spawn sound effect.
        /// Override in subclasses for unique spawn sounds.
        /// </summary>
        protected virtual void PlaySpawnSound()
        {
            if (AudioManager.Instance == null) return;

            // Generate procedural spawn sound based on enemy type
            // Higher pitch for small enemies, lower for large
            float pitch = EnemyType switch
            {
                EnemyType.DataMite => 1.3f,      // Small, high pitch
                EnemyType.Fizzer => 1.4f,        // Tiny, very high pitch
                EnemyType.ScanDrone => 1.1f,     // Medium pitch
                EnemyType.UFO => 0.9f,           // Lower pitch
                EnemyType.ChaosWorm => 0.7f,     // Deep pitch
                EnemyType.VoidSphere => 0.8f,    // Deep pitch
                EnemyType.CrystalShard => 1.2f,  // Crystal sound
                EnemyType.Boss => 0.6f,          // Very deep
                _ => 1.0f
            };

            // Play spawn whoosh sound (reuse pickup sound with pitch variation)
            AudioManager.Instance.PlaySFX(null, volumeMultiplier: 0.4f, pitchMultiplier: pitch);
        }

        /// <summary>
        /// Play signature "alive" sound effect when entering Alive state.
        /// Override in subclasses for unique signature sounds.
        /// </summary>
        protected virtual void PlayAliveSound()
        {
            if (AudioManager.Instance == null) return;

            // Each enemy type gets a unique signature sound
            // Override in subclasses for custom sounds (e.g., ChaosWorm roar)
            float pitch = EnemyType switch
            {
                EnemyType.DataMite => 1.2f,
                EnemyType.Fizzer => 1.5f,        // High-pitched buzz
                EnemyType.ScanDrone => 1.0f,     // Beep sound
                EnemyType.UFO => 0.8f,           // Low hum
                EnemyType.ChaosWorm => 0.6f,     // Deep roar
                EnemyType.VoidSphere => 0.7f,    // Ominous hum
                EnemyType.CrystalShard => 1.3f,  // Crystal chime
                EnemyType.Boss => 0.5f,          // Very deep roar
                _ => 1.0f
            };

            // Play a short beep/chirp sound (reuse hit sound with pitch variation)
            AudioManager.Instance.PlaySFX(null, volumeMultiplier: 0.3f, pitchMultiplier: pitch);
        }

        /// <summary>
        /// Play death effects (particles, sound, animation).
        /// Override in subclasses for custom death effects (e.g., ChaosWorm elaborate death).
        /// </summary>
        protected virtual void PlayDeathEffects()
        {
            // Death particles
            if (m_enableDeathParticles)
            {
                if (s_cachedVFXManager == null)
                    s_cachedVFXManager = FindFirstObjectByType<VFXManager>();
                if (s_cachedVFXManager != null)
                {
                    // Choose explosion size based on enemy type
                    Graphics.ExplosionSize size = EnemyType switch
                    {
                        EnemyType.DataMite => Graphics.ExplosionSize.Small,
                        EnemyType.Fizzer => Graphics.ExplosionSize.Small,
                        EnemyType.ScanDrone => Graphics.ExplosionSize.Medium,
                        EnemyType.UFO => Graphics.ExplosionSize.Medium,
                        EnemyType.ChaosWorm => Graphics.ExplosionSize.Large,
                        EnemyType.VoidSphere => Graphics.ExplosionSize.Large,
                        EnemyType.CrystalShard => Graphics.ExplosionSize.Medium,
                        EnemyType.Boss => Graphics.ExplosionSize.Boss,
                        _ => Graphics.ExplosionSize.Small
                    };

                    s_cachedVFXManager.PlayExplosion(transform.position, size, GetEnemyColor());
                }
            }

            // Death sound
            if (m_enableDeathSound)
            {
                PlayDeathSound();
            }
        }

        /// <summary>
        /// Play death sound effect.
        /// Override in subclasses for unique death sounds.
        /// </summary>
        protected virtual void PlayDeathSound()
        {
            if (AudioManager.Instance == null) return;

            // Use enemy-type-specific death sound if available
            // AudioManager.CreateEnemyDeath(enemyTypeIndex) generates varied death sounds
            int enemyTypeIndex = (int)EnemyType;
            // For now, use generic explosion sound with pitch variation
            float pitch = EnemyType switch
            {
                EnemyType.DataMite => 1.2f,
                EnemyType.Fizzer => 1.4f,
                EnemyType.ScanDrone => 1.0f,
                EnemyType.UFO => 0.9f,
                EnemyType.ChaosWorm => 0.6f,
                EnemyType.VoidSphere => 0.7f,
                EnemyType.CrystalShard => 1.1f,
                EnemyType.Boss => 0.5f,
                _ => 1.0f
            };

            // Play explosion sound (reuse existing explosion sound with pitch variation)
            AudioManager.Instance.PlaySFX(null, volumeMultiplier: 0.6f, pitchMultiplier: pitch);
        }

        /// <summary>
        /// Get the enemy's primary color for particle effects.
        /// </summary>
        protected virtual Color GetEnemyColor()
        {
            var config = ConfigProvider.Balance?.GetEnemyConfig(EnemyType);
            return config != null ? config.color : Color.white;
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
                // TryGetComponent is zero-alloc (unlike GetComponent which may allocate on null)
                other.TryGetComponent<PlayerHealth>(out var playerHealth);
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    // Player takes damage from enemy contact (only if alive)
                    playerHealth.TakeDamage(m_damage, transform.position);
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
            // TryGetComponent is zero-alloc in Unity 6000.x (unlike GetComponent which may allocate on null)
            if (other.CompareTag("Enemy") && other.TryGetComponent<EnemyBase>(out var otherEnemy) && otherEnemy.IsActive)
            {
                ApplyEnemySeparation(otherEnemy);
            }
        }

        /// <summary>
        /// Push enemies apart when overlapping (soft collision)
        /// </summary>
        protected virtual void ApplyEnemySeparation(EnemyBase other)
        {
            Vector2 toOther = (Vector2)(other.transform.position - transform.position);
            float distance = toOther.magnitude;
            float minDistance = m_collisionRadius + other.CollisionRadius;

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
            if (m_playerTarget == null) return Vector2.zero;
            return ((Vector2)m_playerTarget.position - (Vector2)transform.position).normalized;
        }

        /// <summary>
        /// Get distance to player
        /// </summary>
        protected float GetDistanceToPlayer()
        {
            if (m_playerTarget == null) return float.MaxValue;
            return Vector2.Distance(transform.position, m_playerTarget.position);
        }

        /// <summary>
        /// Called when returned to pool
        /// </summary>
        public virtual void OnReturnToPool()
        {
            m_state = EnemyState.Dead;
            m_playerTarget = null;

            // Re-enable visuals for next spawn
            ShowVisuals();

            // Reset elite modifier
            if (m_eliteModifier != null)
            {
                m_eliteModifier.Reset();
            }
        }

        #endregion

        #region Debug

        protected virtual void OnDrawGizmosSelected()
        {
            // Collision radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_collisionRadius);

            // Direction to player
            if (m_playerTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, m_playerTarget.position);
            }
        }

        #endregion
    }
}
