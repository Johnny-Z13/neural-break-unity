using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using NeuralBreak.Utils;

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
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private Transform _enemyContainer;

        [Header("Enemy Prefabs")]
        [SerializeField] private DataMite _dataMitePrefab;
        [SerializeField] private ScanDrone _scanDronePrefab;
        [SerializeField] private Fizzer _fizzerPrefab;
        [SerializeField] private UFO _ufoPrefab;
        [SerializeField] private ChaosWorm _chaosWormPrefab;
        [SerializeField] private VoidSphere _voidSpherePrefab;
        [SerializeField] private CrystalShard _crystalShardPrefab;
        [SerializeField] private Boss _bossPrefab;

        [Header("Spawn Control")]
        [SerializeField] private bool _spawningEnabled = true;

        [Header("Spawn Warnings")]
        [SerializeField] private bool _enableWarnings = true;
        [SerializeField] private float _warningDuration = 0.6f;
        [SerializeField] private float _bossWarningDuration = 1.5f;

        [Header("Spawn Spacing")]
        [SerializeField] private float _minEnemySpacing = 2.0f;
        [SerializeField] private int _maxSpawnAttempts = 10;

        // Helper classes
        private EnemyPoolManager _poolManager;
        private EnemySpawnPositionCalculator _positionCalculator;
        private EnemySpawnRateManager _rateManager;

        // Config-driven properties
        private SpawnConfig SpawnConfig => ConfigProvider.Spawning;
        private int MaxActiveEnemies => SpawnConfig.maxActiveEnemies;

        // Active enemies tracking
        private List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        // Public accessors
        public int ActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<EnemyBase> ActiveEnemies => _activeEnemies;
        public Transform PlayerTarget => _playerTarget;
        public bool SpawningEnabled { get => _spawningEnabled; set => _spawningEnabled = value; }

        private void Awake()
        {
            if (_enemyContainer == null)
            {
                _enemyContainer = new GameObject("Enemies").transform;
                _enemyContainer.SetParent(transform);
            }

            // Initialize helper classes
            _poolManager = new EnemyPoolManager(
                _dataMitePrefab, _scanDronePrefab, _fizzerPrefab, _ufoPrefab,
                _chaosWormPrefab, _voidSpherePrefab, _crystalShardPrefab, _bossPrefab,
                _enemyContainer);

            _positionCalculator = new EnemySpawnPositionCalculator(
                _playerTarget, _minEnemySpacing, _maxSpawnAttempts);

            _rateManager = new EnemySpawnRateManager();
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
            if (!_spawningEnabled) return;

            UpdateSpawnTimers();
            CleanupDeadEnemies();
        }

        #region Spawning

        private void UpdateSpawnTimers()
        {
            // Check max enemies
            if (_activeEnemies.Count >= MaxActiveEnemies) return;

            // Get ready spawns from rate manager
            EnemySpawnRequest[] readySpawns = _rateManager.UpdateAndGetReadySpawns(Time.deltaTime);

            // Process each spawn request
            foreach (var request in readySpawns)
            {
                if (!_poolManager.HasPool(request.EnemyType))
                {
                    LogHelper.LogWarning($"[EnemySpawner] {request.EnemyType} pool is NULL!");
                    continue;
                }

                LogHelper.Log($"[EnemySpawner] Spawning {request.EnemyType}!");

                Vector2 spawnPos = request.UseEdgeSpawn
                    ? _positionCalculator.GetEdgeSpawnPosition()
                    : _positionCalculator.GetSpawnPosition(_activeEnemies);

                SpawnEnemyAtPosition(request.EnemyType, spawnPos);
            }
        }

        private void SpawnEnemyAtPosition(EnemyType enemyType, Vector2 spawnPos)
        {
            if (_enableWarnings)
            {
                float duration = enemyType == EnemyType.Boss ? _bossWarningDuration : _warningDuration;
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
            if (!_spawningEnabled || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
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
            EnemyBase enemy = _poolManager.GetEnemy<EnemyBase>(enemyType, spawnPos);
            if (enemy == null) return;

            // Ensure enemy tag is set (required for collision detection)
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                enemy.gameObject.tag = "Enemy";
            }

            enemy.Initialize(spawnPos, _playerTarget, ReturnEnemyToPool);
            _activeEnemies.Add(enemy);
        }

        private void ReturnEnemyToPool(EnemyBase enemy)
        {
            _activeEnemies.Remove(enemy);
            _poolManager.ReturnEnemy(enemy);
        }

        #endregion

        #region Public Spawn Methods

        /// <summary>
        /// Manually spawn a specific enemy type
        /// </summary>
        public EnemyBase SpawnEnemyOfType(EnemyType type, Vector2? position = null)
        {
            Vector2 spawnPos = position ?? _positionCalculator.GetSpawnPosition(_activeEnemies);

            if (!_poolManager.HasPool(type))
            {
                Debug.LogWarning($"[EnemySpawner] Cannot spawn {type} - no pool available");
                return null;
            }

            EnemyBase enemy = _poolManager.GetEnemy<EnemyBase>(type, spawnPos);
            if (enemy == null) return null;

            // Ensure enemy tag is set
            if (!enemy.gameObject.CompareTag("Enemy"))
            {
                enemy.gameObject.tag = "Enemy";
            }

            enemy.Initialize(spawnPos, _playerTarget, ReturnEnemyToPool);
            _activeEnemies.Add(enemy);
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
            _rateManager.SetEnemySpawnRate(type, rate);
        }

        #endregion

        #region Spawn Rate Control

        public void SetSpawnRates(float dataMite, float scanDrone, float fizzer, float ufo,
            float chaosWorm, float voidSphere, float crystalShard, float boss)
        {
            _rateManager.SetSpawnRates(dataMite, scanDrone, fizzer, ufo,
                chaosWorm, voidSphere, crystalShard, boss);
        }

        public void MultiplySpawnRates(float multiplier)
        {
            _rateManager.MultiplySpawnRates(multiplier);
        }

        /// <summary>
        /// Stop all enemy spawning
        /// </summary>
        public void StopSpawning()
        {
            _spawningEnabled = false;
            LogHelper.Log("[EnemySpawner] Spawning stopped");
        }

        /// <summary>
        /// Start enemy spawning
        /// </summary>
        public void StartSpawning()
        {
            _spawningEnabled = true;
            LogHelper.Log("[EnemySpawner] Spawning started");
        }

        /// <summary>
        /// Check if spawning is enabled
        /// </summary>
        public bool IsSpawning => _spawningEnabled;

        #endregion

        #region Enemy Management

        private void CleanupDeadEnemies()
        {
            // Use backward iteration instead of RemoveAll to avoid delegate allocation
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] == null || _activeEnemies[i].State == EnemyState.Dead)
                {
                    _activeEnemies.RemoveAt(i);
                }
            }
        }

        public void ClearAllEnemies()
        {
            // Use backward iteration instead of ToArray() allocation
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy != null)
                {
                    enemy.KillInstant();
                }
            }
            _activeEnemies.Clear();
            LogHelper.Log("[EnemySpawner] All enemies cleared");
        }

        public void KillAllEnemies()
        {
            // Use backward iteration instead of ToArray() allocation
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.Kill();
                }
            }
            LogHelper.Log("[EnemySpawner] All enemies killed");
        }

        public void ResetTimers()
        {
            _rateManager.ResetTimers();
        }

        /// <summary>
        /// Get count of active enemies of a specific type
        /// </summary>
        public int GetActiveCountOfType(EnemyType type)
        {
            int count = 0;
            foreach (var enemy in _activeEnemies)
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
            _spawningEnabled = true;
            LogHelper.Log("[EnemySpawner] Spawning enabled, waiting for LevelManager to configure rates");
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            KillAllEnemies();
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
            _positionCalculator?.DrawGizmos();
        }

        #endregion
    }
}
