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
        [SerializeField] private Transform m_playerTarget;
        [SerializeField] private Transform m_pickupContainer;

        [Header("Spawn Area")]
        [SerializeField] private float m_arenaRadius = 28f;
        [SerializeField] private float m_minDistanceFromPlayer = 5f;
        [SerializeField] private int m_maxSpawnAttempts = 10;

        [Header("Pickup Prefabs")]
        [SerializeField] private PowerUpPickup m_powerUpPrefab;
        [SerializeField] private SpeedUpPickup m_speedUpPrefab;
        [SerializeField] private MedPackPickup m_medPackPrefab;
        [SerializeField] private ShieldPickup m_shieldPrefab;
        [SerializeField] private InvulnerablePickup m_invulnerablePrefab;

        [Header("Special Pickups")]
        [SerializeField] private SmartBombPickup m_smartBombPrefab;

        [Header("Weapon Upgrade Prefabs")]
        [SerializeField] private SpreadShotPickup m_spreadShotPrefab;
        [SerializeField] private PiercingPickup m_piercingPrefab;
        [SerializeField] private RapidFirePickup m_rapidFirePrefab;
        [SerializeField] private HomingPickup m_homingPrefab;

        [Header("Pool Sizes")]
        [SerializeField] private int m_powerUpPoolSize = 10;
        [SerializeField] private int m_speedUpPoolSize = 10;
        [SerializeField] private int m_medPackPoolSize = 15;
        [SerializeField] private int m_shieldPoolSize = 10;
        [SerializeField] private int m_invulnerablePoolSize = 5;
        [SerializeField] private int m_smartBombPoolSize = 5;
        [SerializeField] private int m_weaponUpgradePoolSize = 5;

        [Header("Smart Bomb Spawn Settings")]
        [SerializeField] private int m_smartBombSpawnsPerLevel = 1;
        [SerializeField] private Vector2 m_smartBombInterval = new Vector2(45f, 75f);

        [Header("Weapon Upgrade Spawn Settings")]
        [SerializeField] private int m_weaponUpgradeSpawnsPerLevel = 1;
        [SerializeField] private Vector2 m_weaponUpgradeInterval = new Vector2(25f, 40f);

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
        [SerializeField] private bool m_spawningEnabled = true;
        [SerializeField] private int m_maxActivePickups = 30;

        // Object pools
        private ObjectPool<PowerUpPickup> m_powerUpPool;
        private ObjectPool<SpeedUpPickup> m_speedUpPool;
        private ObjectPool<MedPackPickup> m_medPackPool;
        private ObjectPool<ShieldPickup> m_shieldPool;
        private ObjectPool<InvulnerablePickup> m_invulnerablePool;
        private ObjectPool<SmartBombPickup> m_smartBombPool;
        private ObjectPool<SpreadShotPickup> m_spreadShotPool;
        private ObjectPool<PiercingPickup> m_piercingPool;
        private ObjectPool<RapidFirePickup> m_rapidFirePool;
        private ObjectPool<HomingPickup> m_homingPool;

        // Spawn timers
        private float m_powerUpTimer;
        private float m_speedUpTimer;
        private float m_medPackTimer;
        private float m_shieldTimer;
        private float m_invulnerableTimer;
        private float m_smartBombTimer;
        private float m_weaponUpgradeTimer;

        // Next spawn times (randomized)
        private float m_nextPowerUpTime;
        private float m_nextSpeedUpTime;
        private float m_nextMedPackTime;
        private float m_nextShieldTime;
        private float m_nextInvulnerableTime;
        private float m_nextSmartBombTime;
        private float m_nextWeaponUpgradeTime;

        // Spawns this level
        private int m_powerUpSpawns;
        private int m_speedUpSpawns;
        private int m_medPackSpawns;
        private int m_shieldSpawns;
        private int m_invulnerableSpawns;
        private int m_smartBombSpawns;
        private int m_weaponUpgradeSpawns;

        // Active pickups
        private List<PickupBase> m_activePickups = new List<PickupBase>();

        // Player health reference for conditional spawning
        private PlayerHealth m_playerHealth;

        // Public accessors
        public int ActivePickupCount => m_activePickups.Count;
        public IReadOnlyList<PickupBase> ActivePickups => m_activePickups;
        public bool SpawningEnabled { get => m_spawningEnabled; set => m_spawningEnabled = value; }

        private void Awake()
        {
            InitializePools();
        }

        private void Start()
        {
            // Get player health reference
            if (m_playerTarget != null)
            {
                m_playerHealth = m_playerTarget.GetComponent<PlayerHealth>();
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
            if (!m_spawningEnabled) return;

            UpdateSpawnTimers();
            CleanupInactivePickups();
        }

        #region Pool Setup

        private void InitializePools()
        {
            if (m_pickupContainer == null)
            {
                m_pickupContainer = new GameObject("Pickups").transform;
                m_pickupContainer.SetParent(transform);
            }

            if (m_powerUpPrefab != null)
            {
                m_powerUpPool = new ObjectPool<PowerUpPickup>(
                    m_powerUpPrefab, m_pickupContainer, m_powerUpPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_speedUpPrefab != null)
            {
                m_speedUpPool = new ObjectPool<SpeedUpPickup>(
                    m_speedUpPrefab, m_pickupContainer, m_speedUpPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_medPackPrefab != null)
            {
                m_medPackPool = new ObjectPool<MedPackPickup>(
                    m_medPackPrefab, m_pickupContainer, m_medPackPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_shieldPrefab != null)
            {
                m_shieldPool = new ObjectPool<ShieldPickup>(
                    m_shieldPrefab, m_pickupContainer, m_shieldPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_invulnerablePrefab != null)
            {
                m_invulnerablePool = new ObjectPool<InvulnerablePickup>(
                    m_invulnerablePrefab, m_pickupContainer, m_invulnerablePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_smartBombPrefab != null)
            {
                m_smartBombPool = new ObjectPool<SmartBombPickup>(
                    m_smartBombPrefab, m_pickupContainer, m_smartBombPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            // Weapon upgrade pools
            if (m_spreadShotPrefab != null)
            {
                m_spreadShotPool = new ObjectPool<SpreadShotPickup>(
                    m_spreadShotPrefab, m_pickupContainer, m_weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_piercingPrefab != null)
            {
                m_piercingPool = new ObjectPool<PiercingPickup>(
                    m_piercingPrefab, m_pickupContainer, m_weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_rapidFirePrefab != null)
            {
                m_rapidFirePool = new ObjectPool<RapidFirePickup>(
                    m_rapidFirePrefab, m_pickupContainer, m_weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_homingPrefab != null)
            {
                m_homingPool = new ObjectPool<HomingPickup>(
                    m_homingPrefab, m_pickupContainer, m_weaponUpgradePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }
        }

        #endregion

        #region Spawning

        private void UpdateSpawnTimers()
        {
            if (m_activePickups.Count >= m_maxActivePickups) return;

            float dt = Time.deltaTime;

            // PowerUp (config-driven)
            m_powerUpTimer += dt;
            if (m_powerUpTimer >= m_nextPowerUpTime && m_powerUpSpawns < PowerUpSpawnsPerLevel)
            {
                TrySpawnPowerUp();
            }

            // SpeedUp (config-driven)
            m_speedUpTimer += dt;
            if (m_speedUpTimer >= m_nextSpeedUpTime && m_speedUpSpawns < SpeedUpSpawnsPerLevel)
            {
                TrySpawnSpeedUp();
            }

            // MedPack (conditional, config-driven)
            m_medPackTimer += dt;
            if (m_medPackTimer >= m_nextMedPackTime && m_medPackSpawns < MedPackSpawnsPerLevel)
            {
                TrySpawnMedPack();
            }

            // Shield (config-driven)
            m_shieldTimer += dt;
            if (m_shieldTimer >= m_nextShieldTime && m_shieldSpawns < ShieldSpawnsPerLevel)
            {
                TrySpawnShield();
            }

            // Invulnerable (config-driven)
            m_invulnerableTimer += dt;
            if (m_invulnerableTimer >= m_nextInvulnerableTime && m_invulnerableSpawns < InvulnerableSpawnsPerLevel)
            {
                TrySpawnInvulnerable();
            }

            // Smart Bomb (rare spawn)
            m_smartBombTimer += dt;
            if (m_smartBombTimer >= m_nextSmartBombTime && m_smartBombSpawns < m_smartBombSpawnsPerLevel)
            {
                TrySpawnSmartBomb();
            }

            // Weapon Upgrades (random type each spawn)
            m_weaponUpgradeTimer += dt;
            if (m_weaponUpgradeTimer >= m_nextWeaponUpgradeTime && m_weaponUpgradeSpawns < m_weaponUpgradeSpawnsPerLevel)
            {
                TrySpawnWeaponUpgrade();
            }
        }

        private void TrySpawnPowerUp()
        {
            if (m_powerUpPool == null) return;

            Vector2 pos = GetSpawnPosition();
            PowerUpPickup pickup = m_powerUpPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_powerUpSpawns++;
            m_powerUpTimer = 0f;
            m_nextPowerUpTime = Random.Range(PowerUpInterval.x, PowerUpInterval.y);

            Debug.Log($"[PickupSpawner] PowerUp spawned ({m_powerUpSpawns}/{PowerUpSpawnsPerLevel})");
        }

        private void TrySpawnSpeedUp()
        {
            if (m_speedUpPool == null) return;

            Vector2 pos = GetSpawnPosition();
            SpeedUpPickup pickup = m_speedUpPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_speedUpSpawns++;
            m_speedUpTimer = 0f;
            m_nextSpeedUpTime = Random.Range(SpeedUpInterval.x, SpeedUpInterval.y);

            Debug.Log($"[PickupSpawner] SpeedUp spawned ({m_speedUpSpawns}/{SpeedUpSpawnsPerLevel})");
        }

        private void TrySpawnMedPack()
        {
            if (m_medPackPool == null) return;

            // Conditional: Only spawn if player health is below threshold (config-driven)
            if (m_playerHealth != null && m_playerHealth.HealthPercent >= MedPackHealthThreshold)
            {
                // Reset timer but don't count as a spawn
                m_medPackTimer = 0f;
                m_nextMedPackTime = Random.Range(MedPackInterval.x, MedPackInterval.y);
                return;
            }

            Vector2 pos = GetSpawnPosition();
            MedPackPickup pickup = m_medPackPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_medPackSpawns++;
            m_medPackTimer = 0f;
            m_nextMedPackTime = Random.Range(MedPackInterval.x, MedPackInterval.y);

            Debug.Log($"[PickupSpawner] MedPack spawned ({m_medPackSpawns}/{MedPackSpawnsPerLevel})");
        }

        private void TrySpawnShield()
        {
            if (m_shieldPool == null) return;

            Vector2 pos = GetSpawnPosition();
            ShieldPickup pickup = m_shieldPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_shieldSpawns++;
            m_shieldTimer = 0f;
            m_nextShieldTime = Random.Range(ShieldInterval.x, ShieldInterval.y);

            Debug.Log($"[PickupSpawner] Shield spawned ({m_shieldSpawns}/{ShieldSpawnsPerLevel})");
        }

        private void TrySpawnInvulnerable()
        {
            if (m_invulnerablePool == null) return;

            Vector2 pos = GetSpawnPosition();
            InvulnerablePickup pickup = m_invulnerablePool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_invulnerableSpawns++;
            m_invulnerableTimer = 0f;
            m_nextInvulnerableTime = Random.Range(InvulnerableInterval.x, InvulnerableInterval.y);

            Debug.Log($"[PickupSpawner] Invulnerable spawned! ({m_invulnerableSpawns}/{InvulnerableSpawnsPerLevel})");
        }

        private void TrySpawnSmartBomb()
        {
            if (m_smartBombPool == null) return;

            Vector2 pos = GetSpawnPosition();
            SmartBombPickup pickup = m_smartBombPool.Get(pos, Quaternion.identity);
            pickup.Initialize(pos, m_playerTarget, ReturnToPool);
            m_activePickups.Add(pickup);

            m_smartBombSpawns++;
            m_smartBombTimer = 0f;
            m_nextSmartBombTime = Random.Range(m_smartBombInterval.x, m_smartBombInterval.y);

            Debug.Log($"[PickupSpawner] Smart Bomb spawned! ({m_smartBombSpawns}/{m_smartBombSpawnsPerLevel})");
        }

        private void TrySpawnWeaponUpgrade()
        {
            // Pick a random weapon upgrade type
            int upgradeType = Random.Range(0, 4);
            Vector2 pos = GetSpawnPosition();
            PickupBase pickup = null;

            switch (upgradeType)
            {
                case 0 when m_spreadShotPool != null:
                    pickup = m_spreadShotPool.Get(pos, Quaternion.identity);
                    break;
                case 1 when m_piercingPool != null:
                    pickup = m_piercingPool.Get(pos, Quaternion.identity);
                    break;
                case 2 when m_rapidFirePool != null:
                    pickup = m_rapidFirePool.Get(pos, Quaternion.identity);
                    break;
                case 3 when m_homingPool != null:
                    pickup = m_homingPool.Get(pos, Quaternion.identity);
                    break;
            }

            if (pickup != null)
            {
                pickup.Initialize(pos, m_playerTarget, ReturnToPool);
                m_activePickups.Add(pickup);

                m_weaponUpgradeSpawns++;
                m_weaponUpgradeTimer = 0f;
                m_nextWeaponUpgradeTime = Random.Range(m_weaponUpgradeInterval.x, m_weaponUpgradeInterval.y);

                Debug.Log($"[PickupSpawner] Weapon upgrade ({pickup.PickupType}) spawned! ({m_weaponUpgradeSpawns}/{m_weaponUpgradeSpawnsPerLevel})");
            }
        }

        private Vector2 GetSpawnPosition()
        {
            if (m_playerTarget == null)
            {
                // Random within arena
                float r = m_arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            }

            Vector2 playerPos = m_playerTarget.position;

            for (int i = 0; i < m_maxSpawnAttempts; i++)
            {
                // Random within arena (uniform distribution)
                float r = m_arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                Vector2 pos = new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));

                // Check distance from player
                if (Vector2.Distance(pos, playerPos) >= m_minDistanceFromPlayer)
                {
                    return pos;
                }
            }

            // Fallback: spawn at edge away from player
            Vector2 awayFromPlayer = -playerPos.normalized;
            return awayFromPlayer * m_arenaRadius * 0.8f;
        }

        #endregion

        #region Pool Management

        private void ReturnToPool(PickupBase pickup)
        {
            m_activePickups.Remove(pickup);

            switch (pickup)
            {
                case PowerUpPickup p: m_powerUpPool?.Return(p); break;
                case SpeedUpPickup s: m_speedUpPool?.Return(s); break;
                case MedPackPickup m: m_medPackPool?.Return(m); break;
                case ShieldPickup sh: m_shieldPool?.Return(sh); break;
                case InvulnerablePickup inv: m_invulnerablePool?.Return(inv); break;
                case SmartBombPickup sb: m_smartBombPool?.Return(sb); break;
                case SpreadShotPickup ss: m_spreadShotPool?.Return(ss); break;
                case PiercingPickup pc: m_piercingPool?.Return(pc); break;
                case RapidFirePickup rf: m_rapidFirePool?.Return(rf); break;
                case HomingPickup hm: m_homingPool?.Return(hm); break;
            }
        }

        private void CleanupInactivePickups()
        {
            // Use backward iteration instead of RemoveAll to avoid delegate allocation
            for (int i = m_activePickups.Count - 1; i >= 0; i--)
            {
                if (m_activePickups[i] == null || !m_activePickups[i].IsActive)
                {
                    m_activePickups.RemoveAt(i);
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
                case PickupType.PowerUp when m_powerUpPool != null:
                    pickup = m_powerUpPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SpeedUp when m_speedUpPool != null:
                    pickup = m_speedUpPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.MedPack when m_medPackPool != null:
                    pickup = m_medPackPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Shield when m_shieldPool != null:
                    pickup = m_shieldPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Invulnerable when m_invulnerablePool != null:
                    pickup = m_invulnerablePool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SmartBomb when m_smartBombPool != null:
                    pickup = m_smartBombPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SpreadShot when m_spreadShotPool != null:
                    pickup = m_spreadShotPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Piercing when m_piercingPool != null:
                    pickup = m_piercingPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.RapidFire when m_rapidFirePool != null:
                    pickup = m_rapidFirePool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Homing when m_homingPool != null:
                    pickup = m_homingPool.Get(pos, Quaternion.identity);
                    break;
            }

            if (pickup != null)
            {
                pickup.Initialize(pos, m_playerTarget, ReturnToPool);
                m_activePickups.Add(pickup);
            }

            return pickup;
        }

        /// <summary>
        /// Clear all active pickups
        /// </summary>
        public void ClearAllPickups()
        {
            // Iterate backward to avoid allocation from ToArray()
            for (int i = m_activePickups.Count - 1; i >= 0; i--)
            {
                var pickup = m_activePickups[i];
                if (pickup != null)
                {
                    pickup.OnReturnToPool();
                    ReturnToPool(pickup);
                }
            }
            m_activePickups.Clear();
        }

        /// <summary>
        /// Reset spawn counters for new level
        /// </summary>
        public void ResetForNewLevel()
        {
            m_powerUpSpawns = 0;
            m_speedUpSpawns = 0;
            m_medPackSpawns = 0;
            m_shieldSpawns = 0;
            m_invulnerableSpawns = 0;
            m_smartBombSpawns = 0;
            m_weaponUpgradeSpawns = 0;

            RandomizeNextSpawnTimes();
        }

        private void RandomizeNextSpawnTimes()
        {
            // Use config-driven intervals (read from GameBalanceConfig)
            m_nextPowerUpTime = Random.Range(PowerUpInterval.x * 0.5f, PowerUpInterval.y * 0.5f);
            m_nextSpeedUpTime = Random.Range(SpeedUpInterval.x * 0.5f, SpeedUpInterval.y * 0.5f);
            m_nextMedPackTime = Random.Range(MedPackInterval.x * 0.5f, MedPackInterval.y * 0.5f);
            m_nextShieldTime = Random.Range(ShieldInterval.x * 0.5f, ShieldInterval.y * 0.5f);
            m_nextInvulnerableTime = Random.Range(InvulnerableInterval.x * 0.5f, InvulnerableInterval.y * 0.5f);
            m_nextSmartBombTime = Random.Range(m_smartBombInterval.x * 0.5f, m_smartBombInterval.y * 0.5f);
            m_nextWeaponUpgradeTime = Random.Range(m_weaponUpgradeInterval.x * 0.5f, m_weaponUpgradeInterval.y * 0.5f);
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            ClearAllPickups();
            ResetForNewLevel();

            // Reset speed level
            SpeedUpPickup.ResetSpeedLevel();

            m_spawningEnabled = true;
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
            Gizmos.DrawWireSphere(Vector3.zero, m_arenaRadius);

            // Min distance from player
            if (m_playerTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_playerTarget.position, m_minDistanceFromPlayer);
            }

            // Active pickups
            Gizmos.color = Color.green;
            foreach (var pickup in m_activePickups)
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
