using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages spawning for floating pickups during gameplay.
    /// Only 4 pickup types spawn as floating arena pickups:
    /// - MedPack:      Heal +20 HP (conditional: only if health below threshold)
    /// - Shield:       +1 shield charge
    /// - SmartBomb:    +1 smart bomb
    /// - Invulnerable: Temporary god mode (VERY RARE)
    ///
    /// All other upgrades (weapon, damage, fire rate, etc.) come from the
    /// end-of-level upgrade selection screen.
    ///
    /// Spawn rates are config-driven via GameBalanceConfig for easy balance tweaking.
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
        [SerializeField] private MedPackPickup m_medPackPrefab;
        [SerializeField] private ShieldPickup m_shieldPrefab;
        [SerializeField] private SmartBombPickup m_smartBombPrefab;
        [SerializeField] private InvulnerablePickup m_invulnerablePrefab;

        [Header("Pool Sizes")]
        [SerializeField] private int m_medPackPoolSize = 15;
        [SerializeField] private int m_shieldPoolSize = 10;
        [SerializeField] private int m_smartBombPoolSize = 5;
        [SerializeField] private int m_invulnerablePoolSize = 5;

        [Header("Smart Bomb Spawn Settings")]
        [SerializeField] private int m_smartBombSpawnsPerLevel = 1;
        [SerializeField] private Vector2 m_smartBombInterval = new Vector2(45f, 75f);

        // Config-driven properties (read from GameBalanceConfig)
        private int MedPackSpawnsPerLevel => ConfigProvider.Balance?.medPack?.spawnsPerLevel ?? 3;
        private int ShieldSpawnsPerLevel => ConfigProvider.Balance?.shield?.spawnsPerLevel ?? 2;
        private int InvulnerableSpawnsPerLevel => ConfigProvider.Balance?.invulnerable?.spawnsPerLevel ?? 1;

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
        [SerializeField] private int m_maxActivePickups = 20;

        // Object pools
        private ObjectPool<MedPackPickup> m_medPackPool;
        private ObjectPool<ShieldPickup> m_shieldPool;
        private ObjectPool<SmartBombPickup> m_smartBombPool;
        private ObjectPool<InvulnerablePickup> m_invulnerablePool;

        // Spawn timers
        private float m_medPackTimer;
        private float m_shieldTimer;
        private float m_smartBombTimer;
        private float m_invulnerableTimer;

        // Next spawn times (randomized)
        private float m_nextMedPackTime;
        private float m_nextShieldTime;
        private float m_nextSmartBombTime;
        private float m_nextInvulnerableTime;

        // Spawns this level
        private int m_medPackSpawns;
        private int m_shieldSpawns;
        private int m_smartBombSpawns;
        private int m_invulnerableSpawns;

        // Active pickups
        private List<PickupBase> m_activePickups = new List<PickupBase>();

        // Player health reference for conditional spawning
        private PlayerHealth m_playerHealth;

        // Public accessors
        public int ActivePickupCount => m_activePickups.Count;
        public IReadOnlyList<PickupBase> ActivePickups => m_activePickups;
        public bool SpawningEnabled { get => m_spawningEnabled; set => m_spawningEnabled = value; }

        private void Start()
        {
            // Auto-wire runs in Start to ensure SceneReferenceWiring (Awake) has already set prefabs
            AutoWireReferences();
            InitializePools();

            if (m_playerTarget != null)
            {
                m_playerHealth = m_playerTarget.GetComponent<PlayerHealth>();
            }

            RandomizeNextSpawnTimes();

            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
        }

        /// <summary>
        /// Auto-wire player target and prefab references if not already set
        /// (by SceneReferenceWiring or Inspector).
        /// </summary>
        private void AutoWireReferences()
        {
            if (m_playerTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    m_playerTarget = player.transform;
                }
            }

            // Editor fallback: load prefabs directly if SceneReferenceWiring didn't wire them
            #if UNITY_EDITOR
            if (m_medPackPrefab == null)
                m_medPackPrefab = LoadPrefab<MedPackPickup>("Assets/_Project/Prefabs/Pickups/MedPackPickup.prefab");
            if (m_shieldPrefab == null)
                m_shieldPrefab = LoadPrefab<ShieldPickup>("Assets/_Project/Prefabs/Pickups/ShieldPickup.prefab");
            if (m_smartBombPrefab == null)
                m_smartBombPrefab = LoadPrefab<SmartBombPickup>("Assets/_Project/Prefabs/Pickups/SmartBombPickup.prefab");
            if (m_invulnerablePrefab == null)
                m_invulnerablePrefab = LoadPrefab<InvulnerablePickup>("Assets/_Project/Prefabs/Pickups/InvulnerablePickup.prefab");
            #endif
        }

        #if UNITY_EDITOR
        private T LoadPrefab<T>(string path) where T : Component
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab.GetComponent<T>();
            }
            Debug.LogWarning($"[PickupSpawner] Prefab not found at: {path}");
            return null;
        }
        #endif

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

            if (m_smartBombPrefab != null)
            {
                m_smartBombPool = new ObjectPool<SmartBombPickup>(
                    m_smartBombPrefab, m_pickupContainer, m_smartBombPoolSize,
                    onReturn: p => p.OnReturnToPool());
            }

            if (m_invulnerablePrefab != null)
            {
                m_invulnerablePool = new ObjectPool<InvulnerablePickup>(
                    m_invulnerablePrefab, m_pickupContainer, m_invulnerablePoolSize,
                    onReturn: p => p.OnReturnToPool());
            }
        }

        #endregion

        #region Spawning

        private void UpdateSpawnTimers()
        {
            if (m_activePickups.Count >= m_maxActivePickups) return;

            float dt = Time.deltaTime;

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

            // Smart Bomb (rare spawn)
            m_smartBombTimer += dt;
            if (m_smartBombTimer >= m_nextSmartBombTime && m_smartBombSpawns < m_smartBombSpawnsPerLevel)
            {
                TrySpawnSmartBomb();
            }

            // Invulnerable (config-driven, VERY RARE)
            m_invulnerableTimer += dt;
            if (m_invulnerableTimer >= m_nextInvulnerableTime && m_invulnerableSpawns < InvulnerableSpawnsPerLevel)
            {
                TrySpawnInvulnerable();
            }
        }

        private void TrySpawnMedPack()
        {
            if (m_medPackPool == null) return;

            // Conditional: Only spawn if player health is below threshold (config-driven)
            if (m_playerHealth != null && m_playerHealth.HealthPercent >= MedPackHealthThreshold)
            {
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
        }

        private Vector2 GetSpawnPosition()
        {
            if (m_playerTarget == null)
            {
                float r = m_arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
            }

            Vector2 playerPos = m_playerTarget.position;

            for (int i = 0; i < m_maxSpawnAttempts; i++)
            {
                float r = m_arenaRadius * Mathf.Sqrt(Random.value);
                float theta = Random.Range(0f, Mathf.PI * 2f);
                Vector2 pos = new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));

                if (Vector2.Distance(pos, playerPos) >= m_minDistanceFromPlayer)
                {
                    return pos;
                }
            }

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
                case MedPackPickup m: m_medPackPool?.Return(m); break;
                case ShieldPickup sh: m_shieldPool?.Return(sh); break;
                case SmartBombPickup sb: m_smartBombPool?.Return(sb); break;
                case InvulnerablePickup inv: m_invulnerablePool?.Return(inv); break;
            }
        }

        private void CleanupInactivePickups()
        {
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

        public PickupBase SpawnPickup(PickupType type, Vector2? position = null)
        {
            Vector2 pos = position ?? GetSpawnPosition();
            PickupBase pickup = null;

            switch (type)
            {
                case PickupType.MedPack when m_medPackPool != null:
                    pickup = m_medPackPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Shield when m_shieldPool != null:
                    pickup = m_shieldPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.SmartBomb when m_smartBombPool != null:
                    pickup = m_smartBombPool.Get(pos, Quaternion.identity);
                    break;
                case PickupType.Invulnerable when m_invulnerablePool != null:
                    pickup = m_invulnerablePool.Get(pos, Quaternion.identity);
                    break;
            }

            if (pickup != null)
            {
                pickup.Initialize(pos, m_playerTarget, ReturnToPool);
                m_activePickups.Add(pickup);
            }

            return pickup;
        }

        public void ClearAllPickups()
        {
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

        public void ResetForNewLevel()
        {
            m_medPackSpawns = 0;
            m_shieldSpawns = 0;
            m_smartBombSpawns = 0;
            m_invulnerableSpawns = 0;

            RandomizeNextSpawnTimes();
        }

        private void RandomizeNextSpawnTimes()
        {
            m_nextMedPackTime = Random.Range(MedPackInterval.x * 0.5f, MedPackInterval.y * 0.5f);
            m_nextShieldTime = Random.Range(ShieldInterval.x * 0.5f, ShieldInterval.y * 0.5f);
            m_nextSmartBombTime = Random.Range(m_smartBombInterval.x * 0.5f, m_smartBombInterval.y * 0.5f);
            m_nextInvulnerableTime = Random.Range(InvulnerableInterval.x * 0.5f, InvulnerableInterval.y * 0.5f);
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            ClearAllPickups();
            ResetForNewLevel();
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

        [ContextMenu("Debug: Spawn MedPack")]
        private void DebugSpawnMedPack() => SpawnPickup(PickupType.MedPack);

        [ContextMenu("Debug: Spawn Shield")]
        private void DebugSpawnShield() => SpawnPickup(PickupType.Shield);

        [ContextMenu("Debug: Spawn Smart Bomb")]
        private void DebugSpawnSmartBomb() => SpawnPickup(PickupType.SmartBomb);

        [ContextMenu("Debug: Spawn Invulnerable")]
        private void DebugSpawnInvulnerable() => SpawnPickup(PickupType.Invulnerable);

        [ContextMenu("Debug: Spawn All Types")]
        private void DebugSpawnAll()
        {
            SpawnPickup(PickupType.MedPack);
            SpawnPickup(PickupType.Shield);
            SpawnPickup(PickupType.SmartBomb);
            SpawnPickup(PickupType.Invulnerable);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Vector3.zero, m_arenaRadius);

            if (m_playerTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_playerTarget.position, m_minDistanceFromPlayer);
            }

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
