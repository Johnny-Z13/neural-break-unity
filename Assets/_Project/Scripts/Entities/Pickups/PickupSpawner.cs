using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages spawning for all pickup types.
    /// Uses object pooling for performance.
    /// Based on TypeScript PickupManager pattern.
    ///
    /// Spawn Rates (from original balance.config.ts):
    /// - PowerUp:      2/level, 30-45s interval (RARE!)
    /// - SpeedUp:      2/level, 25-35s interval
    /// - MedPack:      3/level, 20-30s interval (only if health < 80%)
    /// - Shield:       2/level, 20-30s interval
    /// - Invulnerable: 1/level, 60-90s interval (VERY RARE!)
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private Transform _pickupContainer;

        [Header("Spawn Area")]
        [SerializeField] private float _arenaRadius = 28f;
        [SerializeField] private float _minDistanceFromPlayer = 5f;
        [SerializeField] private int _maxSpawnAttempts = 10;

        [Header("Pickup Prefabs")]
        [SerializeField] private PowerUpPickup _powerUpPrefab;
        [SerializeField] private SpeedUpPickup _speedUpPrefab;
        [SerializeField] private MedPackPickup _medPackPrefab;
        [SerializeField] private ShieldPickup _shieldPrefab;
        [SerializeField] private InvulnerablePickup _invulnerablePrefab;

        [Header("Special Pickups")]
        [SerializeField] private SmartBombPickup _smartBombPrefab;

        [Header("Weapon Upgrade Prefabs")]
        [SerializeField] private SpreadShotPickup _spreadShotPrefab;
        [SerializeField] private PiercingPickup _piercingPrefab;
        [SerializeField] private RapidFirePickup _rapidFirePrefab;
        [SerializeField] private HomingPickup _homingPrefab;

        [Header("Pool Sizes")]
        [SerializeField] private int _powerUpPoolSize = 10;
        [SerializeField] private int _speedUpPoolSize = 10;
        [SerializeField] private int _medPackPoolSize = 15;
        [SerializeField] private int _shieldPoolSize = 10;
        [SerializeField] private int _invulnerablePoolSize = 5;
        [SerializeField] private int _smartBombPoolSize = 5;
        [SerializeField] private int _weaponUpgradePoolSize = 5;

        [Header("Smart Bomb Spawn Settings")]
        [SerializeField] private int _smartBombSpawnsPerLevel = 1;
        [SerializeField] private Vector2 _smartBombInterval = new Vector2(45f, 75f);

        [Header("Weapon Upgrade Spawn Settings")]
        [SerializeField] private int _weaponUpgradeSpawnsPerLevel = 1;
        [SerializeField] private Vector2 _weaponUpgradeInterval = new Vector2(25f, 40f);

        // Config-driven properties (read from GameBalanceConfig)
        private int PowerUpSpawnsPerLevel => ConfigProvider.Balance?.powerUp?.spawnsPerLevel ?? 2;
        private int SpeedUpSpawnsPerLevel => ConfigProvider.Balance?.speedUp?.spawnsPerLevel ?? 2;
        private int MedPackSpawnsPerLevel => ConfigProvider.Balance?.medPack?.spawnsPerLevel ?? 3;
        private int ShieldSpawnsPerLevel => ConfigProvider.Balance?.shield?.spawnsPerLevel ?? 2;
        private int InvulnerableSpawnsPerLevel => ConfigProvider.Balance?.invulnerable?.spawnsPerLevel ?? 1;

        private Vector2 PowerUpInterval => new Vector2(
            ConfigProvider.Balance?.powerUp?.minSpawnInterval ?? 30f,
            ConfigProvider.Balance?.powerUp?.maxSpawnInterval ?? 45f);
        private Vector2 SpeedUpInterval => new Vector2(
            ConfigProvider.Balance?.speedUp?.minSpawnInterval ?? 25f,
            ConfigProvider.Balance?.speedUp?.maxSpawnInterval ?? 35f);
        private Vector2 MedPackInterval => new Vector2(
            ConfigProvider.Balance?.medPack?.minSpawnInterval ?? 20f,
            ConfigProvider.Balance?.medPack?.maxSpawnInterval ?? 30f);
        private Vector2 ShieldInterval => new Vector2(
            ConfigProvider.Balance?.shield?.minSpawnInterval ?? 20f,
            ConfigProvider.Balance?.shield?.maxSpawnInterval ?? 30f);
        private Vector2 InvulnerableInterval => new Vector2(
            ConfigProvider.Balance?.invulnerable?.minSpawnInterval ?? 60f,
            ConfigProvider.Balance?.invulnerable?.maxSpawnInterval ?? 90f);

        private float MedPackHealthThreshold => ConfigProvider.Balance?.medPack?.healthThreshold ?? 0.8f;

        [Header("Spawn Control")]
        [SerializeField] private bool _spawningEnabled = true;
        [SerializeField] private int _maxActivePickups = 30;

        // Object pools
        private ObjectPool<PowerUpPickup> _powerUpPool;
        private ObjectPool<SpeedUpPickup> _speedUpPool;
        private ObjectPool<MedPackPickup> _medPackPool;
        private ObjectPool<ShieldPickup> _shieldPool;
        private ObjectPool<InvulnerablePickup> _invulnerablePool;
        private ObjectPool<SmartBombPickup> _smartBombPool;
        private ObjectPool<SpreadShotPickup> _spreadShotPool;
        private ObjectPool<PiercingPickup> _piercingPool;
        private ObjectPool<RapidFirePickup> _rapidFirePool;
        private ObjectPool<HomingPickup> _homingPool;

        // Spawn timers
        private float _powerUpTimer;
        private float _speedUpTimer;
        private float _medPackTimer;
        private float _shieldTimer;
        private float _invulnerableTimer;
        private float _smartBombTimer;
        private float _weaponUpgradeTimer;

        // Next spawn times (randomized)
        private float _nextPowerUpTime;
        private float _nextSpeedUpTime;
        private float _nextMedPackTime;
        private float _nextShieldTime;
        private float _nextInvulnerableTime;
        private float _nextSmartBombTime;
        private float _nextWeaponUpgradeTime;

        // Spawns this level
        private int _powerUpSpawns;
        private int _speedUpSpawns;
        private int _medPackSpawns;
        private int _shieldSpawns;
        private int _invulnerableSpawns;
        private int _smartBombSpawns;
        private int _weaponUpgradeSpawns;

        // Active pickups
        private List<PickupBase> _activePickups = new List<PickupBase>();

        // Player health reference for conditional spawning
        private PlayerHealth _playerHealth;

        // Public accessors
        public int ActivePickupCount => _activePickups.Count;
        public IReadOnlyList<PickupBase> ActivePickups => _activePickups;
        public bool SpawningEnabled { get => _spawningEnabled; set => _spawningEnabled = value; }

        private void Awake()
        {
            InitializePools();
        }

        private void Start()
        {
            // Get player health reference
            if (_playerTarget != null)
            {
                _playerHealth = _playerTarget.GetComponent<PlayerHealth>();
            }

            // Initialize random spawn times
            RandomizeNextSpawnTimes();

            // Subscribe to events
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            if (!_spawningEnabled) return;

            UpdateSpawnTimers();
            CleanupInactivePickups();
        }

        #region Pool Setup

        private void InitializePools()
        {
            if (_pickupContainer == null)
            {
                _pickupContainer = new GameObject("Pickups").transform;
                _pickupContainer.SetParent(transform);
            }

            if (_powerUpPrefab != null)
            {
                _powerUpPool = new ObjectPool<PowerUpPickup>(
                    _powerUpPrefab, _pickupContainer, _powerUpPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_speedUpPrefab != null)
            {
                _speedUpPool = new ObjectPool<SpeedUpPickup>(
                    _speedUpPrefab, _pickupContainer, _speedUpPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_medPackPrefab != null)
            {
                _medPackPool = new ObjectPool<MedPackPickup>(
                    _medPackPrefab, _pickupContainer, _medPackPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_shieldPrefab != null)
            {
                _shieldPool = new ObjectPool<ShieldPickup>(
                    _shieldPrefab, _pickupContainer, _shieldPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_invulnerablePrefab != null)
            {
                _invulnerablePool = new ObjectPool<InvulnerablePickup>(
                    _invulnerablePrefab, _pickupContainer, _invulnerablePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_smartBombPrefab != null)
            {
                _smartBombPool = new ObjectPool<SmartBombPickup>(
                    _smartBombPrefab, _pickupContainer, _smartBombPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            // Weapon upgrade pools
            if (_spreadShotPrefab != null)
            {
                _spreadShotPool = new ObjectPool<SpreadShotPickup>(
                    _spreadShotPrefab, _pickupContainer, _weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_piercingPrefab != null)
            {
                _piercingPool = new ObjectPool<PiercingPickup>(
                    _piercingPrefab, _pickupContainer, _weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_rapidFirePrefab != null)
            {
                _rapidFirePool = new ObjectPool<RapidFirePickup>(
                    _rapidFirePrefab, _pickupContainer, _weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (_homingPrefab != null)
            {
                _homingPool = new ObjectPool<HomingPickup>(
                    _homingPrefab, _pickupContainer, _weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }
        }

        #endregion

        #region Spawning

        private void UpdateSpawnTimers()
        {
            if (_activePickups.Count >= _maxActivePickups) return;

            float dt = Time.deltaTime;

            // PowerUp (config-driven)
            _powerUpTimer += dt;
            if (_powerUpTimer >= _nextPowerUpTime && _powerUpSpawns < PowerUpSpawnsPerLevel)
            {
                TrySpawnPowerUp();
            }

            // SpeedUp (config-driven)
            _speedUpTimer += dt;
            if (_speedUpTimer >= _nextSpeedUpTime && _speedUpSpawns < SpeedUpSpawnsPerLevel)
            {
                TrySpawnSpeedUp();
            }

            // MedPack (conditional, config-driven)
            _medPackTimer += dt;
            if (_medPackTimer >= _nextMedPackTime && _medPackSpawns < MedPackSpawnsPerLevel)
            {
                TrySpawnMedPack();
            }

            // Shield (config-driven)
            _shieldTimer += dt;
            if (_shieldTimer >= _nextShieldTime && _shieldSpawns < ShieldSpawnsPerLevel)
            {
                TrySpawnShield();
            }

            // Invulnerable (config-driven)
            _invulnerableTimer += dt;
            if (_invulnerableTimer >= _nextInvulnerableTime && _invulnerableSpawns < InvulnerableSpawnsPerLevel)
            {
                TrySpawnInvulnerable();
            }

            // Smart Bomb (rare spawn)
            _smartBombTimer += dt;
            if (_smartBombTimer >= _nextSmartBombTime && _smartBombSpawns < _smartBombSpawnsPerLevel)
            {
                TrySpawnSmartBomb();
            }

            // Weapon Upgrades (random type each spawn)
            _weaponUpgradeTimer += dt;
            if (_weaponUpgradeTimer >= _nextWeaponUpgradeTime && _weaponUpgradeSpawns < _weaponUpgradeSpawnsPerLevel)
            {
                TrySpawnWeaponUpgrade();
            }
        }

        private void TrySpawnPowerUp()
        {
            if (_powerUpPool == null) return;

            Vector2 pos = GetSpawnPosition();
            PowerUpPickup pickup = _powerUpPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _powerUpSpawns++;
            _powerUpTimer = 0f;
            _nextPowerUpTime = Random.Range(PowerUpInterval.x, PowerUpInterval.y);

            Debug.Log($"[PickupSpawner] PowerUp spawned ({_powerUpSpawns}/{PowerUpSpawnsPerLevel})");
        }

        private void TrySpawnSpeedUp()
        {
            if (_speedUpPool == null) return;

            Vector2 pos = GetSpawnPosition();
            SpeedUpPickup pickup = _speedUpPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _speedUpSpawns++;
            _speedUpTimer = 0f;
            _nextSpeedUpTime = Random.Range(SpeedUpInterval.x, SpeedUpInterval.y);

            Debug.Log($"[PickupSpawner] SpeedUp spawned ({_speedUpSpawns}/{SpeedUpSpawnsPerLevel})");
        }

        private void TrySpawnMedPack()
        {
            if (_medPackPool == null) return;

            // Conditional: Only spawn if player health is below threshold (config-driven)
            if (_playerHealth != null && _playerHealth.HealthPercent >= MedPackHealthThreshold)
            {
                // Reset timer but don't count as a spawn
                _medPackTimer = 0f;
                _nextMedPackTime = Random.Range(MedPackInterval.x, MedPackInterval.y);
                return;
            }

            Vector2 pos = GetSpawnPosition();
            MedPackPickup pickup = _medPackPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _medPackSpawns++;
            _medPackTimer = 0f;
            _nextMedPackTime = Random.Range(MedPackInterval.x, MedPackInterval.y);

            Debug.Log($"[PickupSpawner] MedPack spawned ({_medPackSpawns}/{MedPackSpawnsPerLevel})");
        }

        private void TrySpawnShield()
        {
            if (_shieldPool == null) return;

            Vector2 pos = GetSpawnPosition();
            ShieldPickup pickup = _shieldPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _shieldSpawns++;
            _shieldTimer = 0f;
            _nextShieldTime = Random.Range(ShieldInterval.x, ShieldInterval.y);

            Debug.Log($"[PickupSpawner] Shield spawned ({_shieldSpawns}/{ShieldSpawnsPerLevel})");
        }

        private void TrySpawnInvulnerable()
        {
            if (_invulnerablePool == null) return;

            Vector2 pos = GetSpawnPosition();
            InvulnerablePickup pickup = _invulnerablePool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _invulnerableSpawns++;
            _invulnerableTimer = 0f;
            _nextInvulnerableTime = Random.Range(InvulnerableInterval.x, InvulnerableInterval.y);

            Debug.Log($"[PickupSpawner] Invulnerable spawned! ({_invulnerableSpawns}/{InvulnerableSpawnsPerLevel})");
        }

        private void TrySpawnSmartBomb()
        {
            if (_smartBombPool == null) return;

            Vector2 pos = GetSpawnPosition();
            SmartBombPickup pickup = _smartBombPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, _playerTarget, ReturnToPool);
            _activePickups.Add(pickup);

            _smartBombSpawns++;
            _smartBombTimer = 0f;
            _nextSmartBombTime = Random.Range(_smartBombInterval.x, _smartBombInterval.y);

            Debug.Log($"[PickupSpawner] Smart Bomb spawned! ({_smartBombSpawns}/{_smartBombSpawnsPerLevel})");
        }

        private void TrySpawnWeaponUpgrade()
        {
            // Pick a random weapon upgrade type
            int upgradeType = Random.Range(0, 4);
            Vector2 pos = GetSpawnPosition();
            PickupBase pickup = null;

            switch (upgradeType)
            {
                case 0 when _spreadShotPool != null:
                    pickup = _spreadShotPool.Get(pos, Quaternion.identity);
                    break;
                case 1 when _piercingPool != null:
                    pickup = _piercingPool.Get(pos, Quaternion.identity);
                    break;
                case 2 when _rapidFirePool != null:
                    pickup = _rapidFirePool.Get(pos, Quaternion.identity);
                    break;
                case 3 when _homingPool != null:
                    pickup = _homingPool.Get(pos, Quaternion.identity);
                    break;
            }

            if (pickup != null)
            {
                pickup.Initialize(pos, _playerTarget, ReturnToPool);
                _activePickups.Add(pickup);

                _weaponUpgradeSpawns++;
                _weaponUpgradeTimer = 0f;
                _nextWeaponUpgradeTime = Random.Range(_weaponUpgradeInterval.x, _weaponUpgradeInterval.y);

                Debug.Log($"[PickupSpawner] Weapon upgrade ({pickup.PickupType}) spawned! ({_weaponUpgradeSpawns}/{_weaponUpgradeSpawnsPerLevel})");
            }
        }

        private Vector2 GetSpawnPosition()
        {
            if (_playerTarget == null)
            {
                // Random within arena
                float r = _arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            }

            Vector2 playerPos = _playerTarget.position;

            for (int i = 0; i < _maxSpawnAttempts; i++)
            {
                // Random within arena (uniform distribution)
                float r = _arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                Vector2 pos = new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));

                // Check distance from player
                if (Vector2.Distance(pos, playerPos) >= _minDistanceFromPlayer)
                {
                    return pos;
                }
            }

            // Fallback: spawn at edge away from player
            Vector2 awayFromPlayer = -playerPos.normalized;
            return awayFromPlayer * _arenaRadius * 0.8f;
        }

        #endregion

        #region Pool Management

        private void ReturnToPool(PickupBase pickup)
        {
            _activePickups.Remove(pickup);

            switch (pickup)
            {
                case PowerUpPickup p: _powerUpPool?.Return(p); break;
                case SpeedUpPickup s: _speedUpPool?.Return(s); break;
                case MedPackPickup m: _medPackPool?.Return(m); break;
                case ShieldPickup sh: _shieldPool?.Return(sh); break;
                case InvulnerablePickup inv: _invulnerablePool?.Return(inv); break;
                case SmartBombPickup sb: _smartBombPool?.Return(sb); break;
                case SpreadShotPickup ss: _spreadShotPool?.Return(ss); break;
                case PiercingPickup pc: _piercingPool?.Return(pc); break;
                case RapidFirePickup rf: _rapidFirePool?.Return(rf); break;
                case HomingPickup hm: _homingPool?.Return(hm); break;
            }
        }

        private void CleanupInactivePickups()
        {
            // Use backward iteration instead of RemoveAll to avoid delegate allocation
            for (int i = _activePickups.Count - 1; i >= 0; i--)
            {
                if (_activePickups[i] == null || !_activePickups[i].IsActive)
                {
                    _activePickups.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually spawn a pickup of a specific type
        /// </summary>
        public PickupBase SpawnPickup(PickupType type, Vector2? position = null)
        {
            Vector2 pos = position ?? GetSpawnPosition();

            PickupBase pickup = null;

            switch (type)
            {
                case PickupType.PowerUp when _powerUpPool != null:
                    pickup = _powerUpPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SpeedUp when _speedUpPool != null:
                    pickup = _speedUpPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.MedPack when _medPackPool != null:
                    pickup = _medPackPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Shield when _shieldPool != null:
                    pickup = _shieldPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Invulnerable when _invulnerablePool != null:
                    pickup = _invulnerablePool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SmartBomb when _smartBombPool != null:
                    pickup = _smartBombPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SpreadShot when _spreadShotPool != null:
                    pickup = _spreadShotPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Piercing when _piercingPool != null:
                    pickup = _piercingPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.RapidFire when _rapidFirePool != null:
                    pickup = _rapidFirePool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Homing when _homingPool != null:
                    pickup = _homingPool.Get(pos, Quaternion.identity);
                    break;
            }

            if (pickup != null)
            {
                pickup.Initialize(pos, _playerTarget, ReturnToPool);
                _activePickups.Add(pickup);
            }

            return pickup;
        }

        /// <summary>
        /// Clear all active pickups
        /// </summary>
        public void ClearAllPickups()
        {
            // Iterate backward to avoid allocation from ToArray()
            for (int i = _activePickups.Count - 1; i >= 0; i--)
            {
                var pickup = _activePickups[i];
                if (pickup != null)
                {
                    pickup.OnReturnToPool();
                    ReturnToPool(pickup);
                }
            }
            _activePickups.Clear();
        }

        /// <summary>
        /// Reset spawn counters for new level
        /// </summary>
        public void ResetForNewLevel()
        {
            _powerUpSpawns = 0;
            _speedUpSpawns = 0;
            _medPackSpawns = 0;
            _shieldSpawns = 0;
            _invulnerableSpawns = 0;
            _smartBombSpawns = 0;
            _weaponUpgradeSpawns = 0;

            RandomizeNextSpawnTimes();
        }

        private void RandomizeNextSpawnTimes()
        {
            // Use config-driven intervals (read from GameBalanceConfig)
            _nextPowerUpTime = Random.Range(PowerUpInterval.x * 0.5f, PowerUpInterval.y * 0.5f);
            _nextSpeedUpTime = Random.Range(SpeedUpInterval.x * 0.5f, SpeedUpInterval.y * 0.5f);
            _nextMedPackTime = Random.Range(MedPackInterval.x * 0.5f, MedPackInterval.y * 0.5f);
            _nextShieldTime = Random.Range(ShieldInterval.x * 0.5f, ShieldInterval.y * 0.5f);
            _nextInvulnerableTime = Random.Range(InvulnerableInterval.x * 0.5f, InvulnerableInterval.y * 0.5f);
            _nextSmartBombTime = Random.Range(_smartBombInterval.x * 0.5f, _smartBombInterval.y * 0.5f);
            _nextWeaponUpgradeTime = Random.Range(_weaponUpgradeInterval.x * 0.5f, _weaponUpgradeInterval.y * 0.5f);
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            ClearAllPickups();
            ResetForNewLevel();

            // Reset speed level
            SpeedUpPickup.ResetSpeedLevel();

            _spawningEnabled = true;
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            ResetForNewLevel();
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Keep pickups between levels
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Spawn PowerUp")]
        private void DebugSpawnPowerUp() => SpawnPickup(PickupType.PowerUp);

        [ContextMenu("Debug: Spawn SpeedUp")]
        private void DebugSpawnSpeedUp() => SpawnPickup(PickupType.SpeedUp);

        [ContextMenu("Debug: Spawn MedPack")]
        private void DebugSpawnMedPack() => SpawnPickup(PickupType.MedPack);

        [ContextMenu("Debug: Spawn Shield")]
        private void DebugSpawnShield() => SpawnPickup(PickupType.Shield);

        [ContextMenu("Debug: Spawn Invulnerable")]
        private void DebugSpawnInvulnerable() => SpawnPickup(PickupType.Invulnerable);

        [ContextMenu("Debug: Spawn Smart Bomb")]
        private void DebugSpawnSmartBomb() => SpawnPickup(PickupType.SmartBomb);

        [ContextMenu("Debug: Spawn All Types")]
        private void DebugSpawnAll()
        {
            SpawnPickup(PickupType.PowerUp);
            SpawnPickup(PickupType.SpeedUp);
            SpawnPickup(PickupType.MedPack);
            SpawnPickup(PickupType.Shield);
            SpawnPickup(PickupType.Invulnerable);
        }

        private void OnDrawGizmosSelected()
        {
            // Arena radius
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Vector3.zero, _arenaRadius);

            // Min distance from player
            if (_playerTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTarget.position, _minDistanceFromPlayer);
            }

            // Active pickups
            Gizmos.color = Color.green;
            foreach (var pickup in _activePickups)
            {
                if (pickup != null)
                {
                    Gizmos.DrawWireSphere(pickup.Position, 0.5f);
                }
            }
        }

        #endregion
    }
}
