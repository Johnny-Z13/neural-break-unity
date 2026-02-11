using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Combat;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Player health, shields, and damage handling.
    /// Based on TypeScript Player.ts health system.
    /// All values driven by ConfigProvider - no magic numbers.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        // Note: MMFeedbacks removed

        // Config-driven properties
        private PlayerConfig Config => ConfigProvider.Player;
        private int ConfigMaxHealth => Config.maxHealth;
        private int ConfigMaxShields => Config.maxShields;
        private float InvulnerabilityDuration => Config.invulnerablePickupDuration;
        private float DamageInvulnerabilityDuration => Config.damageInvulnerabilityDuration;
        private float SpawnInvulnerabilityDuration => Config.spawnInvulnerabilityDuration;

        // Runtime state (can be modified during gameplay)
        private int m_maxHealth;
        private int m_currentHealth;
        private int m_maxShields;
        private int m_currentShields;

        // Upgrade bonuses
        private int m_bonusShields;
        private int m_bonusHealth;

        // Components
        private PlayerController m_controller;

        // State
        private float m_invulnerabilityTimer;
        private float m_damageInvulnerabilityTimer;
        private bool m_isDead;

        // Public accessors
        public int MaxHealth => m_maxHealth;
        public int CurrentHealth => m_currentHealth;
        public float HealthPercent => (float)m_currentHealth / m_maxHealth;
        public int CurrentShields => m_currentShields;
        public int MaxShields => m_maxShields;
        public bool IsDead => m_isDead;
        public bool IsInvulnerable => m_invulnerabilityTimer > 0f || m_damageInvulnerabilityTimer > 0f;
        public float InvulnerabilityTimeRemaining => m_invulnerabilityTimer;

        private void Awake()
        {
            m_controller = GetComponent<PlayerController>();

            // Initialize from config
            m_maxHealth = ConfigMaxHealth;
            m_maxShields = ConfigMaxShields;
            m_currentHealth = m_maxHealth;
            m_currentShields = Config.startingShields;

            // Subscribe to events
            EventBus.Subscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Full reset on game start/restart
            Reset();

            // Reset upgrade bonuses
            m_bonusShields = 0;
            m_bonusHealth = 0;

            // Re-show player (was hidden on death)
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            // Reset position to center
            if (m_controller != null)
            {
                m_controller.SetPosition(Vector2.zero);
            }

            // Grant spawn invulnerability
            if (SpawnInvulnerabilityDuration > 0)
            {
                m_invulnerabilityTimer = SpawnInvulnerabilityDuration;
            }

            // Publish initial health state so HUD updates
            EventBus.Publish(new PlayerHealedEvent
            {
                amount = 0,
                currentHealth = m_currentHealth,
                maxHealth = m_maxHealth
            });

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = m_currentShields,
                maxShields = m_maxShields
            });

            LogHelper.Log($"[PlayerHealth] Reset for new game. Health: {m_currentHealth}/{m_maxHealth}");
        }

        private void OnModifiersChanged(WeaponModifiersChangedEvent evt)
        {
            var mods = evt.modifiers;

            // Apply shield bonus (difference from current bonus)
            int shieldDelta = mods.bonusShields - m_bonusShields;
            if (shieldDelta != 0)
            {
                m_bonusShields = mods.bonusShields;
                m_maxShields = ConfigMaxShields + m_bonusShields;

                // If bonus increased, add shields
                if (shieldDelta > 0)
                {
                    m_currentShields = Mathf.Min(m_currentShields + shieldDelta, m_maxShields);
                }

                EventBus.Publish(new ShieldChangedEvent
                {
                    currentShields = m_currentShields,
                    maxShields = m_maxShields
                });

                LogHelper.Log($"[PlayerHealth] Shield bonus changed: +{m_bonusShields}. Max shields: {m_maxShields}");
            }

            // Apply health bonus
            int healthDelta = mods.bonusHealth - m_bonusHealth;
            if (healthDelta != 0)
            {
                m_bonusHealth = mods.bonusHealth;
                m_maxHealth = ConfigMaxHealth + m_bonusHealth;

                // If bonus increased, heal by that amount
                if (healthDelta > 0)
                {
                    m_currentHealth = Mathf.Min(m_currentHealth + healthDelta, m_maxHealth);
                }

                EventBus.Publish(new PlayerHealedEvent
                {
                    amount = healthDelta > 0 ? healthDelta : 0,
                    currentHealth = m_currentHealth,
                    maxHealth = m_maxHealth
                });

                LogHelper.Log($"[PlayerHealth] Health bonus changed: +{m_bonusHealth}. Max health: {m_maxHealth}");
            }
        }

        private void Start()
        {
            // Grant brief spawn invulnerability
            if (SpawnInvulnerabilityDuration > 0)
            {
                m_invulnerabilityTimer = SpawnInvulnerabilityDuration;
                LogHelper.Log($"[PlayerHealth] Spawn invulnerability active for {SpawnInvulnerabilityDuration}s");
            }

            // Publish initial health state so HUD can initialize
            EventBus.Publish(new PlayerHealedEvent
            {
                amount = 0,
                currentHealth = m_currentHealth,
                maxHealth = m_maxHealth
            });

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = m_currentShields,
                maxShields = m_maxShields
            });
        }

        private void Update()
        {
            // Update invulnerability timers
            if (m_invulnerabilityTimer > 0f)
            {
                m_invulnerabilityTimer -= Time.deltaTime;
            }

            if (m_damageInvulnerabilityTimer > 0f)
            {
                m_damageInvulnerabilityTimer -= Time.deltaTime;
            }
        }

        #region Damage

        /// <summary>
        /// Take damage from an enemy or projectile
        /// </summary>
        public void TakeDamage(int damage, Vector3 damageSource)
        {
            if (damage < 0)
            {
                LogHelper.LogError($"[PlayerHealth] Invalid damage value: {damage}. Must be >= 0.");
                return;
            }

            if (m_isDead)
            {
                LogHelper.LogWarning("[PlayerHealth] Cannot take damage - player is already dead!");
                return;
            }

            // Check invulnerability states
            if (IsInvulnerable) return;
            if (m_controller != null && m_controller.IsInvulnerable()) return;

            // Check shields first
            if (m_currentShields > 0)
            {
                m_currentShields--;
                m_damageInvulnerabilityTimer = DamageInvulnerabilityDuration;

                // Feedback (Feel removed)

                EventBus.Publish(new ShieldChangedEvent
                {
                    currentShields = m_currentShields,
                    maxShields = m_maxShields
                });

                // Reset combo when hit
                GameManager.Instance?.ResetCombo();

                LogHelper.Log($"[PlayerHealth] Shield absorbed hit! Shields: {m_currentShields}");
                return;
            }

            // Apply damage (clamp to 0 minimum)
            m_currentHealth = Mathf.Max(0, m_currentHealth - damage);
            m_damageInvulnerabilityTimer = DamageInvulnerabilityDuration;

            // Track damage in stats
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Stats.damageTaken += damage;
                GameManager.Instance.ResetCombo();
            }

            // Feedback (Feel removed)

            EventBus.Publish(new PlayerDamagedEvent
            {
                damage = damage,
                currentHealth = m_currentHealth,
                maxHealth = m_maxHealth,
                damageSource = damageSource
            });

            LogHelper.Log($"[PlayerHealth] Took {damage} damage! Health: {m_currentHealth}/{m_maxHealth}");

            // Check death
            if (m_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Instant kill (for debugging or special cases)
        /// </summary>
        public void Kill()
        {
            if (m_isDead) return;
            m_currentHealth = 0;
            Die();
        }

        private void Die()
        {
            m_isDead = true;

            // Feedback (Feel removed)

            EventBus.Publish(new PlayerDiedEvent
            {
                position = transform.position
            });

            LogHelper.Log("[PlayerHealth] Player died!");
        }

        #endregion

        #region Healing

        /// <summary>
        /// Heal the player
        /// </summary>
        public void Heal(int amount)
        {
            if (amount < 0)
            {
                LogHelper.LogError($"[PlayerHealth] Invalid heal amount: {amount}. Must be >= 0.");
                return;
            }

            if (m_isDead)
            {
                LogHelper.LogWarning("[PlayerHealth] Cannot heal - player is dead!");
                return;
            }

            int previousHealth = m_currentHealth;
            m_currentHealth = Mathf.Min(m_currentHealth + amount, m_maxHealth);
            int actualHeal = m_currentHealth - previousHealth;

            if (actualHeal > 0)
            {
                // Feedback (Feel removed)

                EventBus.Publish(new PlayerHealedEvent
                {
                    amount = actualHeal,
                    currentHealth = m_currentHealth,
                    maxHealth = m_maxHealth
                });

                LogHelper.Log($"[PlayerHealth] Healed {actualHeal}! Health: {m_currentHealth}/{m_maxHealth}");
            }
        }

        /// <summary>
        /// Heal to full health
        /// </summary>
        public void HealFull()
        {
            Heal(m_maxHealth);
        }

        #endregion

        #region Shields

        /// <summary>
        /// Add a shield
        /// </summary>
        public void AddShield()
        {
            if (m_currentShields >= m_maxShields) return;

            m_currentShields++;

            // Feedback (Feel removed)

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = m_currentShields,
                maxShields = m_maxShields
            });

            LogHelper.Log($"[PlayerHealth] Shield gained! Shields: {m_currentShields}/{m_maxShields}");
        }

        /// <summary>
        /// Set shields to specific amount
        /// </summary>
        public void SetShields(int amount)
        {
            if (amount < 0)
            {
                LogHelper.LogWarning($"[PlayerHealth] Invalid shield amount: {amount}. Clamping to 0.");
                amount = 0;
            }

            if (amount > m_maxShields)
            {
                LogHelper.LogWarning($"[PlayerHealth] Shield amount {amount} exceeds max {m_maxShields}. Clamping.");
                amount = m_maxShields;
            }

            m_currentShields = amount;

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = m_currentShields,
                maxShields = m_maxShields
            });
        }

        #endregion

        #region Invulnerability

        /// <summary>
        /// Activate invulnerability power-up
        /// </summary>
        public void ActivateInvulnerability(float duration = -1f)
        {
            if (duration == 0f)
            {
                LogHelper.LogWarning("[PlayerHealth] Invulnerability duration is 0 - no effect!");
                return;
            }

            m_invulnerabilityTimer = duration > 0 ? duration : InvulnerabilityDuration;

            // Feedback (Feel removed)

            LogHelper.Log($"[PlayerHealth] Invulnerability activated for {m_invulnerabilityTimer}s");
        }

        #endregion

        #region Reset

        /// <summary>
        /// Reset health for new game
        /// </summary>
        public void Reset()
        {
            // Reinitialize from config in case values changed
            m_maxHealth = ConfigMaxHealth;
            m_maxShields = ConfigMaxShields;
            m_currentHealth = m_maxHealth;
            m_currentShields = Config.startingShields;
            m_invulnerabilityTimer = 0f;
            m_damageInvulnerabilityTimer = 0f;
            m_isDead = false;
        }

        /// <summary>
        /// Increase max health
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0)
            {
                LogHelper.LogWarning($"[PlayerHealth] Invalid max health increase: {amount}. Must be > 0.");
                return;
            }

            m_maxHealth += amount;
            m_currentHealth += amount; // Also heal by that amount

            LogHelper.Log($"[PlayerHealth] Max health increased by {amount}. New max: {m_maxHealth}");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Take 20 Damage")]
        private void DebugTakeDamage() => TakeDamage(20, transform.position);

        [ContextMenu("Debug: Heal 50")]
        private void DebugHeal() => Heal(50);

        [ContextMenu("Debug: Add Shield")]
        private void DebugAddShield() => AddShield();

        [ContextMenu("Debug: Activate Invuln")]
        private void DebugInvuln() => ActivateInvulnerability();

        #endregion
    }
}
