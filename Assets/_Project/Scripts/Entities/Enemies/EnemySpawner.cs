using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages enemy spawning for all 8 enemy types.
    /// Uses object pooling for performance.
    /// All spawn rates and pool sizes driven by ConfigProvider - no magic numbers.
    /// Based on TypeScript EnemyManager.ts.
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

        // Config-driven properties
        private GameBalanceConfig Balance => ConfigProvider.Balance;
        private SpawnConfig SpawnConfig => ConfigProvider.Spawning;
        private float ArenaRadius => ConfigProvider.Player.arenaRadius;
        private float MinSpawnDistance => SpawnConfig.minSpawnDistance;
        private float MaxSpawnDistance => SpawnConfig.maxSpawnDistance;
        private int MaxActiveEnemies => SpawnConfig.maxActiveEnemies;

        // Runtime spawn rates (can be modified by difficulty scaling)
        private float _dataMiteRate;
        private float _scanDroneRate;
        private float _fizzerRate;
        private float _ufoRate;
        private float _chaosWormRate;
        private float _voidSphereRate;
        private float _crystalShardRate;
        private float _bossRate;

        // Object pools
        private ObjectPool<DataMite> _dataMitePool;
        private ObjectPool<ScanDrone> _scanDronePool;
        private ObjectPool<Fizzer> _fizzerPool;
        private ObjectPool<UFO> _ufoPool;
        private ObjectPool<ChaosWorm> _chaosWormPool;
        private ObjectPool<VoidSphere> _voidSpherePool;
        private ObjectPool<CrystalShard> _crystalShardPool;
        private ObjectPool<Boss> _bossPool;

        // Spawn timers
        private float _dataMiteTimer;
        private float _scanDroneTimer;
        private float _fizzerTimer;
        private float _ufoTimer;
        private float _chaosWormTimer;
        private float _voidSphereTimer;
        private float _crystalShardTimer;
        private float _bossTimer;

        // Active enemies tracking
        private List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        // Public accessors
        public int ActiveEnemyCount => _activeEnemies.Count;
        public IReadOnlyList<EnemyBase> ActiveEnemies => _activeEnemies;
        public Transform PlayerTarget => _playerTarget;
        public bool SpawningEnabled { get => _spawningEnabled; set => _spawningEnabled = value; }

        private void Awake()
        {
            InitializeSpawnRatesFromConfig();
            InitializePools();
        }

        /// <summary>
        /// Initialize spawn rates from config (can be modified at runtime)
        /// </summary>
        private void InitializeSpawnRatesFromConfig()
        {
            _dataMiteRate = Balance.dataMite.baseSpawnRate;
            _scanDroneRate = Balance.scanDrone.baseSpawnRate;
            _fizzerRate = float.PositiveInfinity; // Conditional - enabled by level config
            _ufoRate = Balance.ufo.baseSpawnRate;
            _chaosWormRate = Balance.chaosWorm.baseSpawnRate;
            _voidSphereRate = Balance.voidSphere.baseSpawnRate;
            _crystalShardRate = Balance.crystalShard.baseSpawnRate;
            _bossRate = float.PositiveInfinity; // Level-based
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

        // Debug: Spawn immediately on game start for testing (DISABLED)
        // private void OnEnable()
        // {
        //     Invoke(nameof(DebugInitialSpawn), 0.5f);
        // }
        //
        // private void DebugInitialSpawn()
        // {
        //     if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        //     {
        //         Debug.Log("[EnemySpawner] Debug: Force spawning initial enemies...");
        //         for (int i = 0; i < 5; i++)
        //         {
        //             SpawnEnemyOfType(EnemyType.DataMite);
        //         }
        //         Debug.Log($"[EnemySpawner] Debug: Spawned 5 DataMites. ActiveCount: {ActiveEnemyCount}");
        //     }
        // }

        #region Pool Setup

        private void InitializePools()
        {
            if (_enemyContainer == null)
            {
                _enemyContainer = new GameObject("Enemies").transform;
                _enemyContainer.SetParent(transform);
            }

            // Initialize all pools with config-driven pool sizes
            if (_dataMitePrefab != null)
            {
                _dataMitePool = new ObjectPool<DataMite>(_dataMitePrefab, _enemyContainer,
                    Balance.dataMite.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_scanDronePrefab != null)
            {
                _scanDronePool = new ObjectPool<ScanDrone>(_scanDronePrefab, _enemyContainer,
                    Balance.scanDrone.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_fizzerPrefab != null)
            {
                _fizzerPool = new ObjectPool<Fizzer>(_fizzerPrefab, _enemyContainer,
                    Balance.fizzer.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_ufoPrefab != null)
            {
                _ufoPool = new ObjectPool<UFO>(_ufoPrefab, _enemyContainer,
                    Balance.ufo.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_chaosWormPrefab != null)
            {
                _chaosWormPool = new ObjectPool<ChaosWorm>(_chaosWormPrefab, _enemyContainer,
                    Balance.chaosWorm.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_voidSpherePrefab != null)
            {
                _voidSpherePool = new ObjectPool<VoidSphere>(_voidSpherePrefab, _enemyContainer,
                    Balance.voidSphere.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_crystalShardPrefab != null)
            {
                _crystalShardPool = new ObjectPool<CrystalShard>(_crystalShardPrefab, _enemyContainer,
                    Balance.crystalShard.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_bossPrefab != null)
            {
                _bossPool = new ObjectPool<Boss>(_bossPrefab, _enemyContainer,
                    Balance.boss.poolSize, onReturn: e => e.OnReturnToPool());
            }
        }

        #endregion

        #region Spawning

        private void UpdateSpawnTimers()
        {
            // Check max enemies
            if (_activeEnemies.Count >= MaxActiveEnemies) return;

            // DataMite
            _dataMiteTimer += Time.deltaTime;
            if (_dataMiteTimer >= _dataMiteRate)
            {
                if (_dataMitePool != null)
                {
                    Debug.Log($"[EnemySpawner] Spawning DataMite! Timer: {_dataMiteTimer}, Pool: {_dataMitePool.CountInPool}");
                    SpawnEnemy(_dataMitePool, ReturnToPool<DataMite>);
                    _dataMiteTimer = 0f;
                }
                else
                {
                    Debug.LogWarning("[EnemySpawner] DataMite pool is NULL!");
                    _dataMiteTimer = 0f;
                }
            }

            // ScanDrone
            _scanDroneTimer += Time.deltaTime;
            if (_scanDroneTimer >= _scanDroneRate && _scanDronePool != null)
            {
                SpawnEnemy(_scanDronePool, ReturnToPool<ScanDrone>);
                _scanDroneTimer = 0f;
            }

            // Fizzer (conditional - only when enabled)
            if (_fizzerRate < float.PositiveInfinity)
            {
                _fizzerTimer += Time.deltaTime;
                if (_fizzerTimer >= _fizzerRate && _fizzerPool != null)
                {
                    SpawnEnemy(_fizzerPool, ReturnToPool<Fizzer>);
                    _fizzerTimer = 0f;
                }
            }

            // UFO
            _ufoTimer += Time.deltaTime;
            if (_ufoTimer >= _ufoRate && _ufoPool != null)
            {
                SpawnEnemy(_ufoPool, ReturnToPool<UFO>);
                _ufoTimer = 0f;
            }

            // ChaosWorm
            _chaosWormTimer += Time.deltaTime;
            if (_chaosWormTimer >= _chaosWormRate && _chaosWormPool != null)
            {
                SpawnEnemy(_chaosWormPool, ReturnToPool<ChaosWorm>);
                _chaosWormTimer = 0f;
            }

            // VoidSphere
            _voidSphereTimer += Time.deltaTime;
            if (_voidSphereTimer >= _voidSphereRate && _voidSpherePool != null)
            {
                SpawnEnemy(_voidSpherePool, ReturnToPool<VoidSphere>);
                _voidSphereTimer = 0f;
            }

            // CrystalShard
            _crystalShardTimer += Time.deltaTime;
            if (_crystalShardTimer >= _crystalShardRate && _crystalShardPool != null)
            {
                SpawnEnemy(_crystalShardPool, ReturnToPool<CrystalShard>);
                _crystalShardTimer = 0f;
            }

            // Boss (level-based)
            if (_bossRate < float.PositiveInfinity)
            {
                _bossTimer += Time.deltaTime;
                if (_bossTimer >= _bossRate && _bossPool != null)
                {
                    SpawnEnemy(_bossPool, ReturnToPool<Boss>, GetEdgeSpawnPosition());
                    _bossTimer = 0f;
                }
            }
        }

        private void SpawnEnemy<T>(ObjectPool<T> pool, System.Action<EnemyBase> returnCallback, Vector2? position = null) where T : EnemyBase
        {
            Vector2 spawnPos = position ?? GetSpawnPosition();

            if (_enableWarnings)
            {
                // Get enemy type from prefab
                EnemyType enemyType = GetEnemyTypeFromPool(pool);
                float duration = enemyType == EnemyType.Boss ? _bossWarningDuration : _warningDuration;

                // Start warning coroutine then spawn
                StartCoroutine(SpawnWithWarning(pool, returnCallback, spawnPos, enemyType, duration));
            }
            else
            {
                // Immediate spawn
                DoSpawn(pool, returnCallback, spawnPos);
            }
        }

        private IEnumerator SpawnWithWarning<T>(ObjectPool<T> pool, System.Action<EnemyBase> returnCallback,
            Vector2 spawnPos, EnemyType enemyType, float warningDuration) where T : EnemyBase
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
            DoSpawn(pool, returnCallback, spawnPos);

            // Publish spawned event
            EventBus.Publish(new EnemySpawnedEvent
            {
                enemyType = enemyType,
                position = spawnPos
            });
        }

        private void DoSpawn<T>(ObjectPool<T> pool, System.Action<EnemyBase> returnCallback, Vector2 spawnPos) where T : EnemyBase
        {
            T enemy = pool.Get(spawnPos, Quaternion.identity);
            enemy.Initialize(spawnPos, _playerTarget, returnCallback);
            _activeEnemies.Add(enemy);
        }

        private EnemyType GetEnemyTypeFromPool<T>(ObjectPool<T> pool) where T : EnemyBase
        {
            // Determine enemy type based on which pool was passed
            if (typeof(T) == typeof(DataMite)) return EnemyType.DataMite;
            if (typeof(T) == typeof(ScanDrone)) return EnemyType.ScanDrone;
            if (typeof(T) == typeof(Fizzer)) return EnemyType.Fizzer;
            if (typeof(T) == typeof(UFO)) return EnemyType.UFO;
            if (typeof(T) == typeof(ChaosWorm)) return EnemyType.ChaosWorm;
            if (typeof(T) == typeof(VoidSphere)) return EnemyType.VoidSphere;
            if (typeof(T) == typeof(CrystalShard)) return EnemyType.CrystalShard;
            if (typeof(T) == typeof(Boss)) return EnemyType.Boss;
            return EnemyType.DataMite;
        }

        private void ReturnToPool<T>(EnemyBase enemy) where T : EnemyBase
        {
            _activeEnemies.Remove(enemy);

            // Return to appropriate pool
            switch (enemy)
            {
                case DataMite dm: _dataMitePool?.Return(dm); break;
                case ScanDrone sd: _scanDronePool?.Return(sd); break;
                case Fizzer f: _fizzerPool?.Return(f); break;
                case UFO u: _ufoPool?.Return(u); break;
                case ChaosWorm cw: _chaosWormPool?.Return(cw); break;
                case VoidSphere vs: _voidSpherePool?.Return(vs); break;
                case CrystalShard cs: _crystalShardPool?.Return(cs); break;
                case Boss b: _bossPool?.Return(b); break;
            }
        }

        private Vector2 GetSpawnPosition()
        {
            if (_playerTarget == null)
            {
                return Random.insideUnitCircle * ArenaRadius;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(MinSpawnDistance, MaxSpawnDistance);

            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 spawnPos = (Vector2)_playerTarget.position + offset;

            // Clamp to arena bounds
            spawnPos.x = Mathf.Clamp(spawnPos.x, -ArenaRadius, ArenaRadius);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -ArenaRadius, ArenaRadius);

            return spawnPos;
        }

        private Vector2 GetEdgeSpawnPosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ArenaRadius * 0.95f;
        }

        #endregion

        #region Public Spawn Methods

        /// <summary>
        /// Manually spawn a specific enemy type
        /// </summary>
        public EnemyBase SpawnEnemyOfType(EnemyType type, Vector2? position = null)
        {
            Vector2 spawnPos = position ?? GetSpawnPosition();

            switch (type)
            {
                case EnemyType.DataMite when _dataMitePool != null:
                    var dm = _dataMitePool.Get(spawnPos, Quaternion.identity);
                    dm.Initialize(spawnPos, _playerTarget, ReturnToPool<DataMite>);
                    _activeEnemies.Add(dm);
                    return dm;

                case EnemyType.ScanDrone when _scanDronePool != null:
                    var sd = _scanDronePool.Get(spawnPos, Quaternion.identity);
                    sd.Initialize(spawnPos, _playerTarget, ReturnToPool<ScanDrone>);
                    _activeEnemies.Add(sd);
                    return sd;

                case EnemyType.Fizzer when _fizzerPool != null:
                    var f = _fizzerPool.Get(spawnPos, Quaternion.identity);
                    f.Initialize(spawnPos, _playerTarget, ReturnToPool<Fizzer>);
                    _activeEnemies.Add(f);
                    return f;

                case EnemyType.UFO when _ufoPool != null:
                    var u = _ufoPool.Get(spawnPos, Quaternion.identity);
                    u.Initialize(spawnPos, _playerTarget, ReturnToPool<UFO>);
                    _activeEnemies.Add(u);
                    return u;

                case EnemyType.ChaosWorm when _chaosWormPool != null:
                    var cw = _chaosWormPool.Get(spawnPos, Quaternion.identity);
                    cw.Initialize(spawnPos, _playerTarget, ReturnToPool<ChaosWorm>);
                    _activeEnemies.Add(cw);
                    return cw;

                case EnemyType.VoidSphere when _voidSpherePool != null:
                    var vs = _voidSpherePool.Get(spawnPos, Quaternion.identity);
                    vs.Initialize(spawnPos, _playerTarget, ReturnToPool<VoidSphere>);
                    _activeEnemies.Add(vs);
                    return vs;

                case EnemyType.CrystalShard when _crystalShardPool != null:
                    var cs = _crystalShardPool.Get(spawnPos, Quaternion.identity);
                    cs.Initialize(spawnPos, _playerTarget, ReturnToPool<CrystalShard>);
                    _activeEnemies.Add(cs);
                    return cs;

                case EnemyType.Boss when _bossPool != null:
                    var b = _bossPool.Get(spawnPos, Quaternion.identity);
                    b.Initialize(spawnPos, _playerTarget, ReturnToPool<Boss>);
                    _activeEnemies.Add(b);
                    return b;

                default:
                    Debug.LogWarning($"[EnemySpawner] Cannot spawn {type} - no pool available");
                    return null;
            }
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
            Debug.Log($"[EnemySpawner] Spawned wave of {count} {type}s");
        }

        /// <summary>
        /// Enable/disable spawning of a specific enemy type
        /// </summary>
        public void SetEnemySpawnRate(EnemyType type, float rate)
        {
            switch (type)
            {
                case EnemyType.DataMite: _dataMiteRate = rate; break;
                case EnemyType.ScanDrone: _scanDroneRate = rate; break;
                case EnemyType.Fizzer: _fizzerRate = rate; break;
                case EnemyType.UFO: _ufoRate = rate; break;
                case EnemyType.ChaosWorm: _chaosWormRate = rate; break;
                case EnemyType.VoidSphere: _voidSphereRate = rate; break;
                case EnemyType.CrystalShard: _crystalShardRate = rate; break;
                case EnemyType.Boss: _bossRate = rate; break;
            }
        }

        #endregion

        #region Spawn Rate Control

        public void SetSpawnRates(float dataMite, float scanDrone, float fizzer, float ufo,
            float chaosWorm, float voidSphere, float crystalShard, float boss)
        {
            _dataMiteRate = dataMite;
            _scanDroneRate = scanDrone;
            _fizzerRate = fizzer;
            _ufoRate = ufo;
            _chaosWormRate = chaosWorm;
            _voidSphereRate = voidSphere;
            _crystalShardRate = crystalShard;
            _bossRate = boss;
        }

        public void MultiplySpawnRates(float multiplier)
        {
            _dataMiteRate *= multiplier;
            _scanDroneRate *= multiplier;
            _chaosWormRate *= multiplier;
            _voidSphereRate *= multiplier;
            _crystalShardRate *= multiplier;
            _ufoRate *= multiplier;
        }

        /// <summary>
        /// Stop all enemy spawning
        /// </summary>
        public void StopSpawning()
        {
            _spawningEnabled = false;
            Debug.Log("[EnemySpawner] Spawning stopped");
        }

        /// <summary>
        /// Start enemy spawning
        /// </summary>
        public void StartSpawning()
        {
            _spawningEnabled = true;
            Debug.Log("[EnemySpawner] Spawning started");
        }

        /// <summary>
        /// Check if spawning is enabled
        /// </summary>
        public bool IsSpawning => _spawningEnabled;

        #endregion

        #region Enemy Management

        private void CleanupDeadEnemies()
        {
            _activeEnemies.RemoveAll(e => e == null || e.State == EnemyState.Dead);
        }

        public void ClearAllEnemies()
        {
            foreach (var enemy in _activeEnemies.ToArray())
            {
                if (enemy != null)
                {
                    enemy.KillInstant();
                }
            }
            _activeEnemies.Clear();
            Debug.Log("[EnemySpawner] All enemies cleared");
        }

        public void KillAllEnemies()
        {
            foreach (var enemy in _activeEnemies.ToArray())
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.Kill();
                }
            }
            Debug.Log("[EnemySpawner] All enemies killed");
        }

        public void ResetTimers()
        {
            _dataMiteTimer = 0f;
            _scanDroneTimer = 0f;
            _fizzerTimer = 0f;
            _ufoTimer = 0f;
            _chaosWormTimer = 0f;
            _voidSphereTimer = 0f;
            _crystalShardTimer = 0f;
            _bossTimer = 0f;
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
            ClearAllEnemies();
            ResetTimers();
            _spawningEnabled = true;
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
            // Use config values if available, fallback to defaults for editor preview
            float arenaRadius = ConfigProvider.Player?.arenaRadius ?? 25f;
            float minDist = ConfigProvider.Spawning?.minSpawnDistance ?? 8f;
            float maxDist = ConfigProvider.Spawning?.maxSpawnDistance ?? 20f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Vector3.zero, arenaRadius);

            if (_playerTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerTarget.position, minDist);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTarget.position, maxDist);
            }
        }

        #endregion
    }
}
