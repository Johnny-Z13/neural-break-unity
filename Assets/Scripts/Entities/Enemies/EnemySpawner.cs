using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages enemy spawning for all 8 enemy types.
    /// Uses object pooling for performance.
    /// All spawn rates and pool sizes driven by ConfigProvider - no magic numbers.
    /// Based on TypeScript EnemyManager.ts.
    ///
    /// Refactored to delegate to:
    /// - EnemyPoolManager: Handles object pools
    /// - EnemySpawnPositionCalculator: Calculates spawn positions
    /// - EnemySpawnRateManager: Manages spawn rates and timers
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform m_playerTarget;
        [SerializeField] private Transform m_enemyContainer;

        [Header("Enemy Prefabs")]
        [SerializeField] private DataMite m_dataMitePrefab;
        [SerializeField] private ScanDrone m_scanDronePrefab;
        [SerializeField] private Fizzer m_fizzerPrefab;
        [SerializeField] private UFO m_ufoPrefab;
        [SerializeField] private ChaosWorm m_chaosWormPrefab;
        [SerializeField] private VoidSphere m_voidSpherePrefab;
        [SerializeField] private CrystalShard m_crystalShardPrefab;
        [SerializeField] private Boss m_bossPrefab;

        [Header("Spawn Control")]
        [SerializeField] private bool m_spawningEnabled = true;

        [Header("Spawn Warnings")]
        [SerializeField] private bool m_enableWarnings = true;
        [SerializeField] private float m_warningDuration = 0.6f;
        [SerializeField] private float m_bossWarningDuration = 1.5f;

        [Header("Spawn Spacing")]
        [SerializeField] private float m_minEnemySpacing = 2.0f;
        [SerializeField] private int m_maxSpawnAttempts = 10;

        // Helper classes
        private EnemyPoolManager m_poolManager;
        private EnemySpawnPositionCalculator m_positionCalculator;
        private EnemySpawnRateManager m_rateManager;

        // Pre-allocated buffer for spawn requests (avoids per-frame allocation)
        private readonly EnemySpawnRequest[] m_spawnBuffer = new EnemySpawnRequest[8];

        // Pre-allocated buffers for KillAllEnemiesFireworks (avoids per-level-completion allocations)
        private static readonly List<EnemyBase> s_fireworkEnemiesBuffer = new List<EnemyBase>(50);
        private static readonly List<float> s_fireworkDeathTimes = new List<float>(50);
        private static readonly HashSet<int> s_fireworkKilledIndices = new HashSet<int>();

        // Config-driven properties
        private SpawnConfig SpawnConfig => ConfigProvider.Spawning;
        private int MaxActiveEnemies => SpawnConfig.maxActiveEnemies;

        // Active enemies tracking
        private List<EnemyBase> m_activeEnemies = new List<EnemyBase>();

        // Public accessors
        public int ActiveEnemyCount => m_activeEnemies.Count;
        public IReadOnlyList<EnemyBase> ActiveEnemies => m_activeEnemies;
        public Transform PlayerTarget => m_playerTarget;
        public bool SpawningEnabled { get => m_spawningEnabled; set => m_spawningEnabled = value; }

        private void Awake()
        {
            if (m_enemyContainer == null)
            {
                m_enemyContainer = new GameObject("Enemies").transform;
                m_enemyContainer.SetParent(transform);
            }

            // Initialize helper classes
            m_poolManager = new EnemyPoolManager(
                m_dataMitePrefab, m_scanDronePrefab, m_fizzerPrefab, m_ufoPrefab,
                m_chaosWormPrefab, m_voidSpherePrefab, m_crystalShardPrefab, m_bossPrefab,
                m_enemyContainer);

            m_positionCalculator = new EnemySpawnPositionCalculator(
                m_playerTarget, m_minEnemySpacing, m_maxSpawnAttempts);

            m_rateManager = new EnemySpawnRateManager();
        }

        private void Start()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            if (!m_spawningEnabled) return;

            UpdateSpawnTimers();
            CleanupDeadEnemies();
        }

        #region Spawning

        private void UpdateSpawnTimers()
        {
            // Check max enemies
            if (m_activeEnemies.Count >= MaxActiveEnemies) return;

            // Get ready spawns from rate manager (zero-alloc via pre-allocated buffer)
            int spawnCount = m_rateManager.UpdateAndGetReadySpawns(Time.deltaTime, m_spawnBuffer);

            // Process each spawn request
            for (int i = 0; i < spawnCount; i++)
            {
                var request = m_spawnBuffer[i];
                if (!m_poolManager.HasPool(request.EnemyType))
                {
                    LogHelper.LogWarning($"[EnemySpawner] {request.EnemyType} pool is NULL!");
                    continue;
                }

                LogHelper.Log($"[EnemySpawner] Spawning {request.EnemyType}!");

                Vector2 spawnPos = request.UseEdgeSpawn
                    ? m_positionCalculator.GetEdgeSpawnPosition()
                    : m_positionCalculator.GetSpawnPosition(m_activeEnemies);

                SpawnEnemyAtPosition(request.EnemyType, spawnPos);
            }
        }

        private void SpawnEnemyAtPosition(EnemyType enemyType, Vector2 spawnPos)
        {
            if (m_enableWarnings)
            {
                float duration = enemyType == EnemyType.Boss ? m_bossWarningDuration : m_warningDuration;
                StartCoroutine(SpawnWithWarning(enemyType, spawnPos, duration));
            }
            else
            {
                DoSpawn(enemyType, spawnPos);
            }
        }

        private IEnumerator SpawnWithWarning(EnemyType enemyType, Vector2 spawnPos, float warningDuration)
        {
            // Publish warning event
            EventBus.Publish(new EnemySpawnWarningEvent
            {
                enemyType = enemyType,
                spawnPosition = spawnPos,
                warningDuration = warningDuration
            });

            // Wait for warning duration
            yield return new WaitForSeconds(warningDuration);

            // Check if still spawning (game might have ended)
            if (!m_spawningEnabled || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                yield break;

            // Spawn the enemy
            DoSpawn(enemyType, spawnPos);

            // Publish spawned event
            EventBus.Publish(new EnemySpawnedEvent
            {
                enemyType = enemyType,
                position = spawnPos
            });
        }

        private void DoSpawn(EnemyType enemyType, Vector2 spawnPos)
        {
            EnemyBase enemy = m_poolManager.GetEnemy<EnemyBase>(enemyType, spawnPos);
            if (enemy == null) return;

            // Ensure enemy tag is set (required for collision detection)
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                enemy.gameObject.tag = "Enemy";
            }

            enemy.Initialize(spawnPos, m_playerTarget, ReturnEnemyToPool);
            m_activeEnemies.Add(enemy);
        }

        private void ReturnEnemyToPool(EnemyBase enemy)
        {
            m_activeEnemies.Remove(enemy);
            m_poolManager.ReturnEnemy(enemy);
        }

        #endregion

        #region Public Spawn Methods

        /// <summary>
        /// Manually spawn a specific enemy type
        /// </summary>
        public EnemyBase SpawnEnemyOfType(EnemyType type, Vector2? position = null)
        {
            Vector2 spawnPos = position ?? m_positionCalculator.GetSpawnPosition(m_activeEnemies);

            if (!m_poolManager.HasPool(type))
            {
                Debug.LogWarning($"[EnemySpawner] Cannot spawn {type} - no pool available");
                return null;
            }

            EnemyBase enemy = m_poolManager.GetEnemy<EnemyBase>(type, spawnPos);
            if (enemy == null) return null;

            // Ensure enemy tag is set
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                enemy.gameObject.tag = "Enemy";
            }

            enemy.Initialize(spawnPos, m_playerTarget, ReturnEnemyToPool);
            m_activeEnemies.Add(enemy);
            return enemy;
        }

        /// <summary>
        /// Spawn a wave of enemies (for surprise levels)
        /// </summary>
        public void SpawnWave(EnemyType type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemyOfType(type);
            }
            LogHelper.Log($"[EnemySpawner] Spawned wave of {count} {type}s");
        }

        /// <summary>
        /// Enable/disable spawning of a specific enemy type
        /// </summary>
        public void SetEnemySpawnRate(EnemyType type, float rate)
        {
            m_rateManager.SetEnemySpawnRate(type, rate);
        }

        #endregion

        #region Spawn Rate Control

        public void SetSpawnRates(float dataMite, float scanDrone, float fizzer, float ufo,
            float chaosWorm, float voidSphere, float crystalShard, float boss)
        {
            m_rateManager.SetSpawnRates(dataMite, scanDrone, fizzer, ufo,
                chaosWorm, voidSphere, crystalShard, boss);
        }

        public void MultiplySpawnRates(float multiplier)
        {
            m_rateManager.MultiplySpawnRates(multiplier);
        }

        /// <summary>
        /// Stop all enemy spawning
        /// </summary>
        public void StopSpawning()
        {
            m_spawningEnabled = false;
            LogHelper.Log("[EnemySpawner] Spawning stopped");
        }

        /// <summary>
        /// Start enemy spawning
        /// </summary>
        public void StartSpawning()
        {
            m_spawningEnabled = true;
            LogHelper.Log("[EnemySpawner] Spawning started");
        }

        /// <summary>
        /// Check if spawning is enabled
        /// </summary>
        public bool IsSpawning => m_spawningEnabled;

        #endregion

        #region Enemy Management

        private void CleanupDeadEnemies()
        {
            // Use backward iteration instead of RemoveAll to avoid delegate allocation
            for (int i = m_activeEnemies.Count - 1; i >= 0; i--)
            {
                if (m_activeEnemies[i] == null || m_activeEnemies[i].State == EnemyState.Dead)
                {
                    m_activeEnemies.RemoveAt(i);
                }
            }
        }

        public void ClearAllEnemies()
        {
            // Use backward iteration instead of ToArray() allocation
            for (int i = m_activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = m_activeEnemies[i];
                if (enemy != null)
                {
                    enemy.KillInstant();
                }
            }
            m_activeEnemies.Clear();
            LogHelper.Log("[EnemySpawner] All enemies cleared");
        }

        public void KillAllEnemies()
        {
            // Use backward iteration instead of ToArray() allocation
            for (int i = m_activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = m_activeEnemies[i];
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.Kill();
                }
            }
            LogHelper.Log("[EnemySpawner] All enemies killed");
        }

        /// <summary>
        /// Kill all enemies with staggered timing for firework effect.
        /// Returns a coroutine that can be yielded to wait for completion.
        /// </summary>
        public System.Collections.IEnumerator KillAllEnemiesFireworks(float totalDuration = 1.5f)
        {
            LogHelper.Log($"[EnemySpawner] Starting firework sequence with {m_activeEnemies.Count} enemies over {totalDuration}s");

            // Collect all alive enemies (zero-alloc: reuse static buffers)
            s_fireworkEnemiesBuffer.Clear();
            s_fireworkDeathTimes.Clear();
            s_fireworkKilledIndices.Clear();

            for (int i = 0; i < m_activeEnemies.Count; i++)
            {
                if (m_activeEnemies[i] != null && m_activeEnemies[i].IsAlive)
                {
                    s_fireworkEnemiesBuffer.Add(m_activeEnemies[i]);
                }
            }

            if (s_fireworkEnemiesBuffer.Count == 0)
            {
                LogHelper.Log("[EnemySpawner] No enemies to kill in firework sequence");
                yield break;
            }

            // Assign each enemy a RANDOM death time within the duration (parallel list, same index)
            for (int i = 0; i < s_fireworkEnemiesBuffer.Count; i++)
            {
                s_fireworkDeathTimes.Add(Random.Range(0f, totalDuration));
            }

            // Track elapsed time and kill enemies at their scheduled times
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                // Check if any enemies should die this frame (indexed loop, no enumerator)
                for (int i = 0; i < s_fireworkEnemiesBuffer.Count; i++)
                {
                    if (s_fireworkKilledIndices.Contains(i)) continue;

                    if (elapsed >= s_fireworkDeathTimes[i])
                    {
                        var enemy = s_fireworkEnemiesBuffer[i];
                        if (enemy != null && enemy.IsAlive)
                        {
                            enemy.Kill();
                        }
                        s_fireworkKilledIndices.Add(i);
                    }
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            // Kill any remaining enemies that might have been missed
            for (int i = 0; i < s_fireworkEnemiesBuffer.Count; i++)
            {
                if (s_fireworkKilledIndices.Contains(i)) continue;

                var enemy = s_fireworkEnemiesBuffer[i];
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.Kill();
                }
            }

            LogHelper.Log("[EnemySpawner] Firework sequence complete");
        }

        public void ResetTimers()
        {
            m_rateManager.ResetTimers();
        }

        /// <summary>
        /// Get count of active enemies of a specific type
        /// </summary>
        public int GetActiveCountOfType(EnemyType type)
        {
            int count = 0;
            foreach (var enemy in m_activeEnemies)
            {
                if (enemy != null && enemy.EnemyType == type && enemy.IsActive)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            LogHelper.Log($"[EnemySpawner] OnGameStarted - Clearing enemies and resetting timers");
            ClearAllEnemies();
            ResetTimers();
            // DON'T reset spawn rates here - LevelManager will set them via SetSpawnRates()
            m_spawningEnabled = true;
            LogHelper.Log("[EnemySpawner] Spawning enabled, waiting for LevelManager to configure rates");
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Stop spawning new enemies - GameManager handles staggered kills via KillAllEnemiesFireworks()
            m_spawningEnabled = false;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Spawn DataMite")]
        private void DebugSpawnDataMite() => SpawnEnemyOfType(EnemyType.DataMite);

        [ContextMenu("Debug: Spawn ScanDrone")]
        private void DebugSpawnScanDrone() => SpawnEnemyOfType(EnemyType.ScanDrone);

        [ContextMenu("Debug: Spawn Fizzer")]
        private void DebugSpawnFizzer() => SpawnEnemyOfType(EnemyType.Fizzer);

        [ContextMenu("Debug: Spawn UFO")]
        private void DebugSpawnUFO() => SpawnEnemyOfType(EnemyType.UFO);

        [ContextMenu("Debug: Spawn ChaosWorm")]
        private void DebugSpawnChaosWorm() => SpawnEnemyOfType(EnemyType.ChaosWorm);

        [ContextMenu("Debug: Spawn VoidSphere")]
        private void DebugSpawnVoidSphere() => SpawnEnemyOfType(EnemyType.VoidSphere);

        [ContextMenu("Debug: Spawn CrystalShard")]
        private void DebugSpawnCrystalShard() => SpawnEnemyOfType(EnemyType.CrystalShard);

        [ContextMenu("Debug: Spawn Boss")]
        private void DebugSpawnBoss() => SpawnEnemyOfType(EnemyType.Boss);

        private void OnDrawGizmosSelected()
        {
            m_positionCalculator?.DrawGizmos();
        }

        #endregion
    }
}
