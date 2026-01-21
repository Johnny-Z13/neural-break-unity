using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages the 99-level progression system with objective-based advancement.
    /// Each level has specific kill objectives that must be completed.
    /// Controls enemy spawn rates based on level configuration.
    /// Based on TypeScript LevelManager.ts.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Settings")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _maxLevel = 99;

        [Header("References")]
        [SerializeField] private EnemySpawner _enemySpawner;

        [Header("Rogue Mode")]
        [SerializeField] private int _rogueCurrentLayer = 1;

        // Current state
        private LevelConfig _currentConfig;
        private LevelProgress _currentProgress;
        private bool _objectivesComplete;
        private float _totalElapsedTime;
        private bool _isTransitioning;

        // Public accessors
        public int CurrentLevel => _currentLevel;
        public int MaxLevel => _maxLevel;
        public string CurrentLevelName => _currentConfig?.name ?? $"Level {_currentLevel}";
        public LevelProgress CurrentProgress => _currentProgress;
        public bool ObjectivesComplete => _objectivesComplete;
        public float TotalElapsedTime => _totalElapsedTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_enemySpawner == null)
            {
                _enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            _totalElapsedTime += Time.deltaTime;
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            StartNewGame();
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            RegisterKill(evt.enemyType);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start a new game at level 1
        /// </summary>
        public void StartNewGame()
        {
            _currentLevel = 1;
            _totalElapsedTime = 0;
            _rogueCurrentLayer = 1;
            ResetProgress();
            LoadLevelConfig(_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log($"[LevelManager] Started new game at Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start at a specific level
        /// </summary>
        public void StartAtLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, _maxLevel);
            _totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log($"[LevelManager] Started at Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start test mode (endless with all enemies)
        /// </summary>
        public void StartTestMode()
        {
            _currentLevel = 999;
            _totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(999);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log("[LevelManager] Started TEST MODE - All enemies enabled");
        }

        /// <summary>
        /// Start rogue mode
        /// </summary>
        public void StartRogueMode()
        {
            _currentLevel = 998;
            _rogueCurrentLayer = 1;
            _totalElapsedTime = 0;
            ResetProgress();
            LoadRogueLayerConfig(_rogueCurrentLayer);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log("[LevelManager] Started ROGUE MODE - Layer 1");
        }

        /// <summary>
        /// Advance to next level (called after transition completes)
        /// </summary>
        public void AdvanceLevel()
        {
            if (_currentLevel >= _maxLevel)
            {
                // Game complete!
                EventBus.Publish(new GameCompletedEvent
                {
                    finalLevel = _currentLevel,
                    totalTime = _totalElapsedTime
                });
                return;
            }

            _currentLevel++;
            ResetProgress();
            LoadLevelConfig(_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log($"[LevelManager] Advanced to Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Advance rogue layer
        /// </summary>
        public void AdvanceRogueLayer()
        {
            _rogueCurrentLayer++;
            ResetProgress();
            LoadRogueLayerConfig(_rogueCurrentLayer);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            Debug.Log($"[LevelManager] Advanced to Rogue Layer {_rogueCurrentLayer}: {CurrentLevelName}");
        }

        /// <summary>
        /// Get current level objectives
        /// </summary>
        public LevelObjectives GetObjectives()
        {
            return _currentConfig?.objectives ?? new LevelObjectives();
        }

        /// <summary>
        /// Get level progress percentage (0-100)
        /// </summary>
        public float GetLevelProgressPercent()
        {
            if (_currentConfig == null) return 0f;
            return _currentProgress.GetProgressPercent(_currentConfig.objectives);
        }

        /// <summary>
        /// Get total game progress percentage (0-100)
        /// </summary>
        public float GetTotalProgressPercent()
        {
            float completedLevels = _currentLevel - 1;
            float currentLevelProgress = GetLevelProgressPercent() / 100f;
            return ((completedLevels + currentLevelProgress) / LevelGenerator.TOTAL_LEVELS) * 100f;
        }

        /// <summary>
        /// Check if game is complete
        /// </summary>
        public bool IsGameComplete()
        {
            return _currentLevel >= _maxLevel && _objectivesComplete;
        }

        #endregion

        #region Objective Tracking

        /// <summary>
        /// Register an enemy kill for objective tracking
        /// </summary>
        public void RegisterKill(EnemyType enemyType)
        {
            if (_objectivesComplete || _isTransitioning) return;

            switch (enemyType)
            {
                case EnemyType.DataMite:
                    _currentProgress.dataMites++;
                    break;
                case EnemyType.ScanDrone:
                    _currentProgress.scanDrones++;
                    break;
                case EnemyType.ChaosWorm:
                    _currentProgress.chaosWorms++;
                    break;
                case EnemyType.VoidSphere:
                    _currentProgress.voidSpheres++;
                    break;
                case EnemyType.CrystalShard:
                    _currentProgress.crystalShards++;
                    break;
                case EnemyType.Fizzer:
                    _currentProgress.fizzers++;
                    break;
                case EnemyType.UFO:
                    _currentProgress.ufos++;
                    break;
                case EnemyType.Boss:
                    _currentProgress.bosses++;
                    break;
            }

            // Check if objectives complete
            CheckObjectivesComplete();
        }

        /// <summary>
        /// Check if all objectives are met
        /// </summary>
        private void CheckObjectivesComplete()
        {
            if (_objectivesComplete || _currentConfig == null) return;

            if (_currentProgress.MeetsObjectives(_currentConfig.objectives))
            {
                _objectivesComplete = true;
                _isTransitioning = true;

                Debug.Log($"[LevelManager] Level {_currentLevel} objectives complete!");

                // Publish level completed event
                EventBus.Publish(new LevelCompletedEvent
                {
                    levelNumber = _currentLevel,
                    levelName = CurrentLevelName,
                    completionTime = _totalElapsedTime
                });

                // Stop enemy spawning
                if (_enemySpawner != null)
                {
                    _enemySpawner.StopSpawning();
                }

                // Clear remaining enemies and trigger transition
                // The GameManager or transition system should handle this
                // and call AdvanceLevel() when ready
            }
        }

        private void ResetProgress()
        {
            _currentProgress = LevelProgress.Empty;
            _objectivesComplete = false;
            _isTransitioning = false;
        }

        #endregion

        #region Level Configuration

        private void LoadLevelConfig(int level)
        {
            _currentConfig = LevelGenerator.GetLevelConfig(level);
            ApplySpawnRates(_currentConfig.spawnRates);
        }

        private void LoadRogueLayerConfig(int layer)
        {
            _currentConfig = LevelGenerator.GetRogueLevelConfig(layer);
            ApplySpawnRates(_currentConfig.spawnRates);
        }

        private void ApplySpawnRates(SpawnRates rates)
        {
            if (_enemySpawner == null)
            {
                Debug.LogWarning("[LevelManager] No EnemySpawner assigned!");
                return;
            }

            _enemySpawner.SetSpawnRates(
                rates.dataMiteRate,
                rates.scanDroneRate,
                rates.fizzerRate,
                rates.ufoRate,
                rates.chaosWormRate,
                rates.voidSphereRate,
                rates.crystalShardRate,
                rates.bossRate
            );

            Debug.Log($"[LevelManager] Applied spawn rates for {_currentConfig.name}");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Show Current Status")]
        private void DebugShowStatus()
        {
            if (_currentConfig == null)
            {
                Debug.Log("[LevelManager] No level loaded");
                return;
            }

            Debug.Log($"[LevelManager] Level {_currentLevel}: {_currentConfig.name}");
            Debug.Log($"  Progress: {GetLevelProgressPercent():F1}%");
            Debug.Log($"  DataMites: {_currentProgress.dataMites}/{_currentConfig.objectives.dataMites}");
            Debug.Log($"  ScanDrones: {_currentProgress.scanDrones}/{_currentConfig.objectives.scanDrones}");
            Debug.Log($"  ChaosWorms: {_currentProgress.chaosWorms}/{_currentConfig.objectives.chaosWorms}");
            Debug.Log($"  VoidSpheres: {_currentProgress.voidSpheres}/{_currentConfig.objectives.voidSpheres}");
            Debug.Log($"  CrystalShards: {_currentProgress.crystalShards}/{_currentConfig.objectives.crystalShards}");
            Debug.Log($"  Fizzers: {_currentProgress.fizzers}/{_currentConfig.objectives.fizzers}");
            Debug.Log($"  UFOs: {_currentProgress.ufos}/{_currentConfig.objectives.ufos}");
            Debug.Log($"  Bosses: {_currentProgress.bosses}/{_currentConfig.objectives.bosses}");
        }

        [ContextMenu("Debug: Start Level 1")]
        private void DebugLevel1() => StartAtLevel(1);

        [ContextMenu("Debug: Start Level 5")]
        private void DebugLevel5() => StartAtLevel(5);

        [ContextMenu("Debug: Start Level 10")]
        private void DebugLevel10() => StartAtLevel(10);

        [ContextMenu("Debug: Start Level 25")]
        private void DebugLevel25() => StartAtLevel(25);

        [ContextMenu("Debug: Start Level 50")]
        private void DebugLevel50() => StartAtLevel(50);

        [ContextMenu("Debug: Start Test Mode")]
        private void DebugTestMode() => StartTestMode();

        [ContextMenu("Debug: Start Rogue Mode")]
        private void DebugRogueMode() => StartRogueMode();

        [ContextMenu("Debug: Next Level")]
        private void DebugNextLevel() => AdvanceLevel();

        [ContextMenu("Debug: Complete Current Objectives")]
        private void DebugCompleteObjectives()
        {
            if (_currentConfig == null) return;

            _currentProgress = new LevelProgress
            {
                dataMites = _currentConfig.objectives.dataMites,
                scanDrones = _currentConfig.objectives.scanDrones,
                chaosWorms = _currentConfig.objectives.chaosWorms,
                voidSpheres = _currentConfig.objectives.voidSpheres,
                crystalShards = _currentConfig.objectives.crystalShards,
                fizzers = _currentConfig.objectives.fizzers,
                ufos = _currentConfig.objectives.ufos,
                bosses = _currentConfig.objectives.bosses
            };

            CheckObjectivesComplete();
        }

        #endregion
    }
}
