using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Combat;
using NeuralBreak.Utils;

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
        private int _maxHealth;
        private int _currentHealth;
        private int _maxShields;
        private int _currentShields;

        // Upgrade bonuses
        private int _bonusShields;
        private int _bonusHealth;

        // Components
        private PlayerController _controller;

        // State
        private float _invulnerabilityTimer;
        private float _damageInvulnerabilityTimer;
        private bool _isDead;

        // Public accessors
        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public float HealthPercent => (float)_currentHealth / _maxHealth;
        public int CurrentShields => _currentShields;
        public int MaxShields => _maxShields;
        public bool IsDead => _isDead;
        public bool IsInvulnerable => _invulnerabilityTimer > 0f || _damageInvulnerabilityTimer > 0f;
        public float InvulnerabilityTimeRemaining => _invulnerabilityTimer;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();

            // Initialize from config
            _maxHealth = ConfigMaxHealth;
            _maxShields = ConfigMaxShields;
            _currentHealth = _maxHealth;
            _currentShields = Config.startingShields;

            // Subscribe to upgrade changes
            EventBus.Subscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);
        }

        private void OnModifiersChanged(WeaponModifiersChangedEvent evt)
        {
            var mods = evt.modifiers;

            // Apply shield bonus (difference from current bonus)
            int shieldDelta = mods.bonusShields - _bonusShields;
            if (shieldDelta != 0)
            {
                _bonusShields = mods.bonusShields;
                _maxShields = ConfigMaxShields + _bonusShields;

                // If bonus increased, add shields
                if (shieldDelta > 0)
                {
                    _currentShields = Mathf.Min(_currentShields + shieldDelta, _maxShields);
                }

                EventBus.Publish(new ShieldChangedEvent
                {
                    currentShields = _currentShields,
                    maxShields = _maxShields
                });

                LogHelper.Log($"[PlayerHealth] Shield bonus changed: +{_bonusShields}. Max shields: {_maxShields}");
            }

            // Apply health bonus
            int healthDelta = mods.bonusHealth - _bonusHealth;
            if (healthDelta != 0)
            {
                _bonusHealth = mods.bonusHealth;
                _maxHealth = ConfigMaxHealth + _bonusHealth;

                // If bonus increased, heal by that amount
                if (healthDelta > 0)
                {
                    _currentHealth = Mathf.Min(_currentHealth + healthDelta, _maxHealth);
                }

                EventBus.Publish(new PlayerHealedEvent
                {
                    amount = healthDelta > 0 ? healthDelta : 0,
                    currentHealth = _currentHealth,
                    maxHealth = _maxHealth
                });

                LogHelper.Log($"[PlayerHealth] Health bonus changed: +{_bonusHealth}. Max health: {_maxHealth}");
            }
        }

        private void Start()
        {
            // Grant brief spawn invulnerability
            if (SpawnInvulnerabilityDuration > 0)
            {
                _invulnerabilityTimer = SpawnInvulnerabilityDuration;
                LogHelper.Log($"[PlayerHealth] Spawn invulnerability active for {SpawnInvulnerabilityDuration}s");
            }

            // Publish initial health state so HUD can initialize
            EventBus.Publish(new PlayerHealedEvent
            {
                amount = 0,
                currentHealth = _currentHealth,
                maxHealth = _maxHealth
            });

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = _currentShields,
                maxShields = _maxShields
            });
        }

        private void Update()
        {
            // Update invulnerability timers
            if (_invulnerabilityTimer > 0f)
            {
                _invulnerabilityTimer -= Time.deltaTime;
            }

            if (_damageInvulnerabilityTimer > 0f)
            {
                _damageInvulnerabilityTimer -= Time.deltaTime;
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

            if (_isDead)
            {
                LogHelper.LogWarning("[PlayerHealth] Cannot take damage - player is already dead!");
                return;
            }

            // Check invulnerability states
            if (IsInvulnerable) return;
            if (_controller != null && _controller.IsInvulnerable()) return;

            // Check shields first
            if (_currentShields > 0)
            {
                _currentShields--;
                _damageInvulnerabilityTimer = DamageInvulnerabilityDuration;

                // Feedback (Feel removed)

                EventBus.Publish(new ShieldChangedEvent
                {
                    currentShields = _currentShields,
                    maxShields = _maxShields
                });

                // Reset combo when hit
                GameManager.Instance?.ResetCombo();

                LogHelper.Log($"[PlayerHealth] Shield absorbed hit! Shields: {_currentShields}");
                return;
            }

            // Apply damage (clamp to 0 minimum)
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _damageInvulnerabilityTimer = DamageInvulnerabilityDuration;

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
                currentHealth = _currentHealth,
                maxHealth = _maxHealth,
                damageSource = damageSource
            });

            LogHelper.Log($"[PlayerHealth] Took {damage} damage! Health: {_currentHealth}/{_maxHealth}");

            // Check death
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Instant kill (for debugging or special cases)
        /// </summary>
        public void Kill()
        {
            if (_isDead) return;
            _currentHealth = 0;
            Die();
        }

        private void Die()
        {
            _isDead = true;

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

            if (_isDead)
            {
                LogHelper.LogWarning("[PlayerHealth] Cannot heal - player is dead!");
                return;
            }

            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            int actualHeal = _currentHealth - previousHealth;

            if (actualHeal > 0)
            {
                // Feedback (Feel removed)

                EventBus.Publish(new PlayerHealedEvent
                {
                    amount = actualHeal,
                    currentHealth = _currentHealth,
                    maxHealth = _maxHealth
                });

                LogHelper.Log($"[PlayerHealth] Healed {actualHeal}! Health: {_currentHealth}/{_maxHealth}");
            }
        }

        /// <summary>
        /// Heal to full health
        /// </summary>
        public void HealFull()
        {
            Heal(_maxHealth);
        }

        #endregion

        #region Shields

        /// <summary>
        /// Add a shield
        /// </summary>
        public void AddShield()
        {
            if (_currentShields >= _maxShields) return;

            _currentShields++;

            // Feedback (Feel removed)

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = _currentShields,
                maxShields = _maxShields
            });

            LogHelper.Log($"[PlayerHealth] Shield gained! Shields: {_currentShields}/{_maxShields}");
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

            if (amount > _maxShields)
            {
                LogHelper.LogWarning($"[PlayerHealth] Shield amount {amount} exceeds max {_maxShields}. Clamping.");
                amount = _maxShields;
            }

            _currentShields = amount;

            EventBus.Publish(new ShieldChangedEvent
            {
                currentShields = _currentShields,
                maxShields = _maxShields
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

            _invulnerabilityTimer = duration > 0 ? duration : InvulnerabilityDuration;

            // Feedback (Feel removed)

            LogHelper.Log($"[PlayerHealth] Invulnerability activated for {_invulnerabilityTimer}s");
        }

        #endregion

        #region Reset

        /// <summary>
        /// Reset health for new game
        /// </summary>
        public void Reset()
        {
            // Reinitialize from config in case values changed
            _maxHealth = ConfigMaxHealth;
            _maxShields = ConfigMaxShields;
            _currentHealth = _maxHealth;
            _currentShields = Config.startingShields;
            _invulnerabilityTimer = 0f;
            _damageInvulnerabilityTimer = 0f;
            _isDead = false;
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

            _maxHealth += amount;
            _currentHealth += amount; // Also heal by that amount

            LogHelper.Log($"[PlayerHealth] Max health increased by {amount}. New max: {_maxHealth}");
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
