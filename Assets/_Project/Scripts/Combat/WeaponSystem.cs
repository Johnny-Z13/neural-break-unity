using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Input;
using NeuralBreak.Config;
using NeuralBreak.Utils;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Player weapon system - handles firing, heat, and weapon types.
    /// All values driven by ConfigProvider - no magic numbers.
    /// Based on TypeScript WeaponSystem.ts.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _projectileContainer;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _fireFeedback;
        [SerializeField] private MMF_Player _overheatFeedback;
        [SerializeField] private MMF_Player _overheatClearedFeedback;

        // Config-driven properties
        private WeaponConfig Config => ConfigProvider.Weapon;
        private int BaseDamage => Config.baseDamage;
        private float BaseFireRate => Config.baseFireRate;
        private float ProjectileSpeed => Config.projectileSpeed;
        private float MaxHeat => Config.overheatThreshold;
        private float HeatPerShot => Config.heatPerShot;
        private float HeatCoolRate => Config.heatCooldownRate;
        private float OverheatCooldown => Config.overheatCooldownDuration;
        private int ConfigMaxPowerLevel => Config.maxPowerLevel;
        private float DamagePerLevel => Config.damagePerLevel;
        private float FireRatePerLevel => Config.fireRatePerLevel;

        // Runtime state
        private int _powerLevel = 0;

        // Object pool
        private ObjectPool<Projectile> _projectilePool;

        // State
        private float _fireTimer;
        private float _currentHeat;
        private bool _isOverheated;
        private float _overheatTimer;
        private InputManager _input;

        // Public accessors
        public float Heat => _currentHeat;
        public float HeatMax => MaxHeat;
        public float HeatPercent => _currentHeat / MaxHeat;
        public bool IsOverheated => _isOverheated;
        public int PowerLevel => _powerLevel;
        public int MaxPowerLevel => ConfigMaxPowerLevel;

        private void Awake()
        {
            // Initialize projectile pool
            if (_projectilePrefab == null)
            {
                LogHelper.LogError("[WeaponSystem] Projectile prefab is not assigned!");
                return;
            }

            if (_projectileContainer == null)
            {
                LogHelper.LogError("[WeaponSystem] Projectile container is not assigned!");
                return;
            }

            try
            {
                _projectilePool = new ObjectPool<Projectile>(
                    _projectilePrefab,
                    _projectileContainer,
                    initialSize: 50,
                    onReturn: proj => proj.OnReturnToPool()
                );
            }
            catch (System.Exception ex)
            {
                LogHelper.LogError($"[WeaponSystem] Failed to create projectile pool: {ex.Message}");
            }
        }

        private void Start()
        {
            _input = InputManager.Instance;

            if (_player == null)
            {
                _player = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            UpdateHeat();
            UpdateFiring();
        }

        #region Firing

        private void UpdateFiring()
        {
            _fireTimer -= Time.deltaTime;

            // Check if can fire
            if (_input != null && _input.FireHeld && !_isOverheated && _fireTimer <= 0f)
            {
                Fire();
            }
        }

        private void Fire()
        {
            if (_player == null)
            {
                LogHelper.LogError("[WeaponSystem] Cannot fire - player reference is null!");
                return;
            }

            if (_projectilePool == null)
            {
                LogHelper.LogError("[WeaponSystem] Cannot fire - projectile pool is null!");
                return;
            }

            // Get fire direction (player facing direction)
            Vector2 direction = _player.FacingDirection;
            if (direction == Vector2.zero)
            {
                direction = Vector2.up;
            }

            // Calculate damage with power level
            int damage = CalculateDamage();

            // Spawn projectile at player's forward position (tip of the ship)
            // Use the player's transform forward to get consistent spawn point
            Vector2 spawnOffset = direction * 0.6f;
            Vector2 spawnPos = _player.Position + spawnOffset;

            // Check for spread shot upgrade
            var upgradeManager = WeaponUpgradeManager.Instance;
            if (upgradeManager != null && upgradeManager.HasSpreadShot)
            {
                FireSpread(spawnPos, direction, damage, upgradeManager.SpreadShotCount, upgradeManager.SpreadAngle);
            }
            else
            {
                // Normal single shot
                FireProjectile(spawnPos, direction, damage);
            }

            // Apply heat
            _currentHeat += HeatPerShot;
            if (_currentHeat >= MaxHeat)
            {
                TriggerOverheat();
            }

            // Reset fire timer (affected by rapid fire)
            _fireTimer = GetFireRate();

            // Feedback
            _fireFeedback?.PlayFeedbacks();

            // Event
            EventBus.Publish(new ProjectileFiredEvent
            {
                position = spawnPos,
                direction = direction,
                powerLevel = _powerLevel
            });
        }

        private void FireProjectile(Vector2 position, Vector2 direction, int damage)
        {
            Projectile proj = _projectilePool.Get(position, Quaternion.identity);

            // Check for piercing and homing upgrades
            var upgradeManager = WeaponUpgradeManager.Instance;
            bool isPiercing = upgradeManager != null && upgradeManager.HasPiercing;
            bool isHoming = upgradeManager != null && upgradeManager.HasHoming;

            proj.Initialize(position, direction, damage, _powerLevel, ReturnProjectile, isPiercing, isHoming);
        }

        private void FireSpread(Vector2 position, Vector2 direction, int damage, int count, float spreadAngle)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float angleStep = spreadAngle / (count - 1);
            float startAngle = baseAngle - spreadAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + (angleStep * i);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 spreadDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                FireProjectile(position, spreadDirection, damage);
            }
        }

        private void ReturnProjectile(Projectile proj)
        {
            _projectilePool.Return(proj);
        }

        private int CalculateDamage()
        {
            float multiplier = 1f + (_powerLevel * DamagePerLevel);
            return Mathf.RoundToInt(BaseDamage * multiplier);
        }

        private float GetFireRate()
        {
            // Faster fire rate at higher power levels
            float reduction = _powerLevel * FireRatePerLevel;
            float rate = Mathf.Max(0.05f, BaseFireRate - reduction);

            // Apply rapid fire multiplier
            var upgradeManager = WeaponUpgradeManager.Instance;
            if (upgradeManager != null && upgradeManager.HasRapidFire)
            {
                rate /= upgradeManager.RapidFireMultiplier;
            }

            return Mathf.Max(0.03f, rate);
        }

        #endregion

        #region Heat System

        private void UpdateHeat()
        {
            if (_isOverheated)
            {
                // Overheat cooldown
                _overheatTimer -= Time.deltaTime;
                if (_overheatTimer <= 0f)
                {
                    ClearOverheat();
                }

                // Faster cooling when overheated
                _currentHeat = Mathf.Max(0f, _currentHeat - HeatCoolRate * 1.5f * Time.deltaTime);
            }
            else if (!_input.FireHeld)
            {
                // Cool down when not firing
                _currentHeat = Mathf.Max(0f, _currentHeat - HeatCoolRate * Time.deltaTime);
            }

            // Publish heat change
            EventBus.Publish(new WeaponHeatChangedEvent
            {
                heat = _currentHeat,
                maxHeat = MaxHeat,
                isOverheated = _isOverheated
            });
        }

        private void TriggerOverheat()
        {
            _isOverheated = true;
            _overheatTimer = OverheatCooldown;

            _overheatFeedback?.PlayFeedbacks();

            EventBus.Publish(new WeaponOverheatedEvent
            {
                cooldownDuration = OverheatCooldown
            });

            LogHelper.Log("[WeaponSystem] OVERHEATED!");
        }

        private void ClearOverheat()
        {
            _isOverheated = false;

            _overheatClearedFeedback?.PlayFeedbacks();

            LogHelper.Log("[WeaponSystem] Overheat cleared");
        }

        #endregion

        #region Power Level

        /// <summary>
        /// Increase power level from pickup
        /// </summary>
        public void AddPowerLevel(int amount = 1)
        {
            if (amount <= 0)
            {
                LogHelper.LogWarning($"[WeaponSystem] Invalid power level amount: {amount}. Must be > 0.");
                return;
            }

            int previousLevel = _powerLevel;
            _powerLevel = Mathf.Min(_powerLevel + amount, ConfigMaxPowerLevel);

            if (_powerLevel != previousLevel)
            {
                EventBus.Publish(new PowerUpChangedEvent
                {
                    newLevel = _powerLevel
                });

                LogHelper.Log($"[WeaponSystem] Power level: {_powerLevel}");
            }
        }

        /// <summary>
        /// Set power level directly
        /// </summary>
        public void SetPowerLevel(int level)
        {
            if (level < 0)
            {
                LogHelper.LogWarning($"[WeaponSystem] Invalid power level: {level}. Clamping to 0.");
                level = 0;
            }

            if (level > ConfigMaxPowerLevel)
            {
                LogHelper.LogWarning($"[WeaponSystem] Power level {level} exceeds max {ConfigMaxPowerLevel}. Clamping.");
                level = ConfigMaxPowerLevel;
            }

            _powerLevel = level;

            EventBus.Publish(new PowerUpChangedEvent
            {
                newLevel = _powerLevel
            });
        }

        /// <summary>
        /// Reset power level for new game
        /// </summary>
        public void Reset()
        {
            _powerLevel = 0;
            _currentHeat = 0f;
            _isOverheated = false;
            _fireTimer = 0f;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Return all active projectiles to pool
        /// </summary>
        public void ClearAllProjectiles()
        {
            // The pool will handle this when enemies are cleared
            // Individual projectiles check if their target is valid
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Add Power")]
        private void DebugAddPower() => AddPowerLevel();

        [ContextMenu("Debug: Max Power")]
        private void DebugMaxPower() => SetPowerLevel(ConfigMaxPowerLevel);

        [ContextMenu("Debug: Trigger Overheat")]
        private void DebugOverheat()
        {
            _currentHeat = MaxHeat;
            TriggerOverheat();
        }

        #endregion
    }
}
