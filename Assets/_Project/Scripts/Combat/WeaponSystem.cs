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
    /// Player weapon system - handles firing patterns, heat, and weapon upgrades.
    /// 
    /// WEAPON PATTERNS:
    /// - Forward: Single, Double, Triple, Quad, X5 (with configurable spread)
    /// - Rear: Optional backward fire (special pickup)
    /// 
    /// All values driven by WeaponSystemConfig in GameBalanceConfig.
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
        [SerializeField] private MMF_Player _rearFireFeedback;

        // Config references
        private WeaponSystemConfig Config => ConfigProvider.Balance?.weaponSystem;
        private WeaponConfig LegacyConfig => ConfigProvider.Weapon; // Fallback

        // Runtime state
        private int _powerLevel = 0;
        private float _fireTimer;
        private float _rearFireTimer;
        private float _currentHeat;
        private bool _isOverheated;
        private float _overheatTimer;

        // Active modifiers (from pickups)
        private bool _rapidFireActive;
        private float _rapidFireTimer;
        private bool _damageBoostActive;
        private float _damageBoostTimer;
        private bool _rearWeaponActive;

        // Object pool
        private ObjectPool<Projectile> _projectilePool;
        private InputManager _input;
        private WeaponUpgradeManager _upgradeManager;

        #region Public Accessors

        public float Heat => _currentHeat;
        public float HeatMax => GetHeatMax();
        public float HeatPercent => _currentHeat / GetHeatMax();
        public bool IsOverheated => _isOverheated;
        public int PowerLevel => _powerLevel;
        public int MaxPowerLevel => Config?.powerLevels?.maxLevel ?? LegacyConfig?.maxPowerLevel ?? 10;
        public ForwardFirePattern CurrentPattern => GetCurrentPattern();
        public bool HasRearWeapon => _rearWeaponActive || (Config?.rearWeapon?.enabled ?? false);

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeProjectilePool();
        }

        private void InitializeProjectilePool()
        {
            // Create projectile container if not assigned
            if (_projectileContainer == null)
            {
                var container = new GameObject("PlayerProjectiles");
                _projectileContainer = container.transform;
            }

            // Create runtime projectile prefab if not assigned
            if (_projectilePrefab == null)
            {
                _projectilePrefab = CreateRuntimeProjectilePrefab();
            }

            // Initialize projectile pool
            try
            {
                _projectilePool = new ObjectPool<Projectile>(
                    _projectilePrefab,
                    _projectileContainer,
                    initialSize: 100, // Increased for multi-shot patterns
                    onReturn: proj => proj.OnReturnToPool()
                );
            }
            catch (System.Exception ex)
            {
                LogHelper.LogError($"[WeaponSystem] Failed to create projectile pool: {ex.Message}");
            }
        }

        private Projectile CreateRuntimeProjectilePrefab()
        {
            var projectileGO = new GameObject("RuntimeProjectile");
            projectileGO.layer = LayerMask.NameToLayer("Default");

            var rb = projectileGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var collider = projectileGO.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;

            var sr = projectileGO.AddComponent<SpriteRenderer>();
            sr.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
            sr.sortingOrder = 100;
            sr.color = Color.white;

            var trail = projectileGO.AddComponent<TrailRenderer>();
            trail.time = 0.1f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.02f;
            trail.startColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            trail.endColor = new Color(0.2f, 0.9f, 1f, 0f);
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.numCornerVertices = 4;
            trail.numCapVertices = 4;

            var projectile = projectileGO.AddComponent<Projectile>();
            projectileGO.SetActive(false);

            return projectile;
        }

        private void Start()
        {
            _input = InputManager.Instance;

            if (_player == null)
            {
                _player = GetComponent<PlayerController>();
            }

            // Cache WeaponUpgradeManager reference (performance fix - avoid FindObjectOfType per frame)
            _upgradeManager = FindObjectOfType<WeaponUpgradeManager>();
            if (_upgradeManager == null)
            {
                LogHelper.LogWarning("[WeaponSystem] WeaponUpgradeManager not found - pickup upgrades will not work");
            }

            // Subscribe to pickup events
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            UpdateModifierTimers();
            UpdateHeat();
            UpdateFiring();
        }

        private void UpdateModifierTimers()
        {
            // Rapid fire timer
            if (_rapidFireActive)
            {
                _rapidFireTimer -= Time.deltaTime;
                if (_rapidFireTimer <= 0f)
                {
                    _rapidFireActive = false;
                    LogHelper.Log("[WeaponSystem] Rapid fire expired");
                }
            }

            // Damage boost timer
            if (_damageBoostActive)
            {
                _damageBoostTimer -= Time.deltaTime;
                if (_damageBoostTimer <= 0f)
                {
                    _damageBoostActive = false;
                    LogHelper.Log("[WeaponSystem] Damage boost expired");
                }
            }
        }

        #endregion

        #region Firing System

        private void UpdateFiring()
        {
            _fireTimer -= Time.deltaTime;
            _rearFireTimer -= Time.deltaTime;

            if (_input == null || !_input.FireHeld || _isOverheated) return;

            // Forward weapons
            if (_fireTimer <= 0f)
            {
                FireForward();
                _fireTimer = GetFireRate();
            }

            // Rear weapon (if enabled and synced or independent timer ready)
            if (HasRearWeapon)
            {
                var rearConfig = Config?.rearWeapon;
                if (rearConfig != null)
                {
                    if (rearConfig.syncWithForward)
                    {
                        // Already fired with forward
                    }
                    else if (_rearFireTimer <= 0f)
                    {
                        FireRear();
                        _rearFireTimer = rearConfig.independentFireRate;
                    }
                }
            }
        }

        private void FireForward()
        {
            if (_player == null || _projectilePool == null) return;

            Vector2 direction = _player.FacingDirection;
            if (direction == Vector2.zero) direction = Vector2.up;

            int damage = CalculateDamage();
            ForwardFirePattern pattern = GetCurrentPattern();
            
            float forwardOffset = Config?.forwardWeapon?.forwardOffset ?? 0.6f;
            Vector2 basePos = _player.Position + direction * forwardOffset;

            // Fire based on pattern
            switch (pattern)
            {
                case ForwardFirePattern.Single:
                    FireSingle(basePos, direction, damage);
                    break;
                case ForwardFirePattern.Double:
                    FireDouble(basePos, direction, damage);
                    break;
                case ForwardFirePattern.Triple:
                    FireTriple(basePos, direction, damage);
                    break;
                case ForwardFirePattern.Quad:
                    FireQuad(basePos, direction, damage);
                    break;
                case ForwardFirePattern.X5:
                    FireX5(basePos, direction, damage);
                    break;
            }

            // Fire rear weapon if synced
            if (HasRearWeapon && (Config?.rearWeapon?.syncWithForward ?? true))
            {
                FireRear();
            }

            // Apply heat
            ApplyHeat(pattern);

            // Feedback
            _fireFeedback?.PlayFeedbacks();

            // Event
            EventBus.Publish(new ProjectileFiredEvent
            {
                position = basePos,
                direction = direction,
                powerLevel = _powerLevel
            });
        }

        #region Fire Patterns

        private void FireSingle(Vector2 position, Vector2 direction, int damage)
        {
            FireProjectile(position, direction, damage);
        }

        private void FireDouble(Vector2 position, Vector2 direction, int damage)
        {
            float spreadAngle = Config?.forwardWeapon?.doubleSpreadAngle ?? 15f;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Two projectiles with slight spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireTriple(Vector2 position, Vector2 direction, int damage)
        {
            float spreadAngle = Config?.forwardWeapon?.tripleSpreadAngle ?? 30f;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Center projectile
            FireProjectile(position, direction, damage);

            // Side projectiles
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireQuad(Vector2 position, Vector2 direction, int damage)
        {
            float spreadAngle = Config?.forwardWeapon?.quadSpreadAngle ?? 45f;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = spreadAngle / 3f;

            // Four projectiles evenly spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle - step / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + step / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireX5(Vector2 position, Vector2 direction, int damage)
        {
            float spreadAngle = Config?.forwardWeapon?.x5SpreadAngle ?? 60f;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = spreadAngle / 4f;

            // Five projectiles evenly spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle - step, damage);
            FireProjectile(position, direction, damage); // Center
            FireProjectileAtAngle(position, baseAngle + step, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireRear()
        {
            if (_player == null || _projectilePool == null) return;

            var rearConfig = Config?.rearWeapon;
            if (rearConfig == null) return;

            Vector2 direction = _player.FacingDirection;
            if (direction == Vector2.zero) direction = Vector2.up;

            // Rear direction is opposite of forward
            Vector2 rearDirection = -direction;
            float rearOffset = rearConfig.rearOffset;
            Vector2 rearPos = _player.Position + rearDirection * rearOffset;

            // Calculate rear damage
            int baseDamage = CalculateDamage();
            int rearDamage = Mathf.RoundToInt(baseDamage * rearConfig.damageMultiplier);

            FireProjectile(rearPos, rearDirection, rearDamage);

            // Apply rear heat
            float rearHeatMult = Config?.heatSystem?.rearWeaponHeatMultiplier ?? 0.5f;
            _currentHeat += GetHeatPerShot() * rearHeatMult;

            _rearFireFeedback?.PlayFeedbacks();
        }

        #endregion

        #region Projectile Spawning

        private void FireProjectile(Vector2 position, Vector2 direction, int damage)
        {
            Projectile proj = _projectilePool.Get(position, Quaternion.identity);

            // Get special weapon states from config
            bool isPiercing = Config?.specials?.piercingEnabled ?? false;
            bool isHoming = Config?.specials?.homingEnabled ?? false;

            // Check WeaponUpgradeManager for pickup-based specials (cached reference)
            if (_upgradeManager != null)
            {
                isPiercing = isPiercing || _upgradeManager.HasPiercing;
                isHoming = isHoming || _upgradeManager.HasHoming;
            }

            proj.Initialize(position, direction, damage, _powerLevel, ReturnProjectile, isPiercing, isHoming);
        }

        private void FireProjectileAtAngle(Vector2 position, float angleDegrees, int damage)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            FireProjectile(position, direction, damage);
        }

        private void ReturnProjectile(Projectile proj)
        {
            _projectilePool.Return(proj);
        }

        #endregion

        #endregion

        #region Calculations

        private ForwardFirePattern GetCurrentPattern()
        {
            var powerConfig = Config?.powerLevels;
            
            // If auto-upgrade is enabled, use power level to determine pattern
            if (powerConfig != null && powerConfig.autoUpgradePattern)
            {
                return powerConfig.GetPatternForLevel(_powerLevel);
            }

            // Otherwise use manual pattern from ForwardWeaponConfig
            return Config?.forwardWeapon?.pattern ?? ForwardFirePattern.Single;
        }

        private int CalculateDamage()
        {
            int baseDamage = Config?.baseDamage ?? LegacyConfig?.baseDamage ?? 12;
            float damagePerLevel = Config?.powerLevels?.damagePerLevel ?? LegacyConfig?.damagePerLevel ?? 0.1f;

            float multiplier = 1f + (_powerLevel * damagePerLevel);

            // Apply damage boost modifier
            if (_damageBoostActive)
            {
                multiplier *= Config?.modifiers?.damageBoostMultiplier ?? 2f;
            }

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        private float GetFireRate()
        {
            float baseRate = Config?.baseFireRate ?? LegacyConfig?.baseFireRate ?? 0.12f;
            float ratePerLevel = Config?.powerLevels?.fireRatePerLevel ?? LegacyConfig?.fireRatePerLevel ?? 0.005f;

            float rate = Mathf.Max(0.05f, baseRate - (_powerLevel * ratePerLevel));

            // Apply rapid fire modifier from internal state
            if (_rapidFireActive)
            {
                rate /= Config?.modifiers?.rapidFireMultiplier ?? 1.5f;
            }

            // Check WeaponUpgradeManager for rapid fire pickup (cached reference)
            if (_upgradeManager != null && _upgradeManager.HasRapidFire)
            {
                rate /= _upgradeManager.RapidFireMultiplier;
            }

            return Mathf.Max(0.03f, rate);
        }

        private float GetHeatPerShot()
        {
            return Config?.heatSystem?.heatPerShot ?? LegacyConfig?.heatPerShot ?? 0.8f;
        }

        private float GetHeatMax()
        {
            return Config?.heatSystem?.maxHeat ?? LegacyConfig?.overheatThreshold ?? 100f;
        }

        private void ApplyHeat(ForwardFirePattern pattern)
        {
            float baseHeat = GetHeatPerShot();
            int projectileCount = GetProjectileCountForPattern(pattern);

            // Multi-shot adds extra heat
            float multiShotMult = Config?.heatSystem?.multiShotHeatMultiplier ?? 0.3f;
            float totalHeat = baseHeat + (baseHeat * multiShotMult * (projectileCount - 1));

            _currentHeat += totalHeat;

            if (_currentHeat >= GetHeatMax())
            {
                TriggerOverheat();
            }
        }

        private int GetProjectileCountForPattern(ForwardFirePattern pattern)
        {
            return pattern switch
            {
                ForwardFirePattern.Single => 1,
                ForwardFirePattern.Double => 2,
                ForwardFirePattern.Triple => 3,
                ForwardFirePattern.Quad => 4,
                ForwardFirePattern.X5 => 5,
                _ => 1
            };
        }

        #endregion

        #region Heat System

        private void UpdateHeat()
        {
            var heatConfig = Config?.heatSystem;
            if (heatConfig == null || !heatConfig.enabled)
            {
                _currentHeat = 0f;
                return;
            }

            if (_isOverheated)
            {
                _overheatTimer -= Time.deltaTime;
                if (_overheatTimer <= 0f)
                {
                    ClearOverheat();
                }

                float overheatCoolMult = heatConfig.overheatCooldownMultiplier;
                _currentHeat = Mathf.Max(0f, _currentHeat - heatConfig.cooldownRate * overheatCoolMult * Time.deltaTime);
            }
            else if (_input == null || !_input.FireHeld)
            {
                _currentHeat = Mathf.Max(0f, _currentHeat - heatConfig.cooldownRate * Time.deltaTime);
            }

            EventBus.Publish(new WeaponHeatChangedEvent
            {
                heat = _currentHeat,
                maxHeat = GetHeatMax(),
                isOverheated = _isOverheated
            });
        }

        private void TriggerOverheat()
        {
            _isOverheated = true;
            _overheatTimer = Config?.heatSystem?.overheatDuration ?? LegacyConfig?.overheatCooldownDuration ?? 0.8f;

            _overheatFeedback?.PlayFeedbacks();

            EventBus.Publish(new WeaponOverheatedEvent
            {
                cooldownDuration = _overheatTimer
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

        #region Power Level & Modifiers

        public void AddPowerLevel(int amount = 1)
        {
            if (amount <= 0) return;

            int previousLevel = _powerLevel;
            _powerLevel = Mathf.Min(_powerLevel + amount, MaxPowerLevel);

            if (_powerLevel != previousLevel)
            {
                EventBus.Publish(new PowerUpChangedEvent { newLevel = _powerLevel });
                LogHelper.Log($"[WeaponSystem] Power level: {_powerLevel} -> Pattern: {GetCurrentPattern()}");
            }
        }

        public void SetPowerLevel(int level)
        {
            _powerLevel = Mathf.Clamp(level, 0, MaxPowerLevel);
            EventBus.Publish(new PowerUpChangedEvent { newLevel = _powerLevel });
        }

        public void ActivateRapidFire(float duration = -1f)
        {
            _rapidFireActive = true;
            _rapidFireTimer = duration > 0 ? duration : (Config?.modifiers?.rapidFireDuration ?? 10f);
            LogHelper.Log($"[WeaponSystem] Rapid fire activated for {_rapidFireTimer}s");
        }

        public void ActivateDamageBoost(float duration = -1f)
        {
            _damageBoostActive = true;
            _damageBoostTimer = duration > 0 ? duration : (Config?.modifiers?.damageBoostDuration ?? 8f);
            LogHelper.Log($"[WeaponSystem] Damage boost activated for {_damageBoostTimer}s");
        }

        public void ActivateRearWeapon()
        {
            _rearWeaponActive = true;
            LogHelper.Log("[WeaponSystem] Rear weapon activated!");
        }

        public void DeactivateRearWeapon()
        {
            _rearWeaponActive = false;
            LogHelper.Log("[WeaponSystem] Rear weapon deactivated");
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            // Handle weapon-related pickups
            switch (evt.pickupType)
            {
                case PickupType.PowerUp:
                    AddPowerLevel();
                    break;
                // Add more pickup types as needed
            }
        }

        public void Reset()
        {
            _powerLevel = 0;
            _currentHeat = 0f;
            _isOverheated = false;
            _fireTimer = 0f;
            _rearFireTimer = 0f;
            _rapidFireActive = false;
            _damageBoostActive = false;
            _rearWeaponActive = false;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Add Power")]
        private void DebugAddPower() => AddPowerLevel();

        [ContextMenu("Debug: Max Power")]
        private void DebugMaxPower() => SetPowerLevel(MaxPowerLevel);

        [ContextMenu("Debug: Activate Rapid Fire")]
        private void DebugRapidFire() => ActivateRapidFire();

        [ContextMenu("Debug: Activate Damage Boost")]
        private void DebugDamageBoost() => ActivateDamageBoost();

        [ContextMenu("Debug: Toggle Rear Weapon")]
        private void DebugRearWeapon()
        {
            if (_rearWeaponActive) DeactivateRearWeapon();
            else ActivateRearWeapon();
        }

        [ContextMenu("Debug: Trigger Overheat")]
        private void DebugOverheat()
        {
            _currentHeat = GetHeatMax();
            TriggerOverheat();
        }

        #endregion
    }
}
