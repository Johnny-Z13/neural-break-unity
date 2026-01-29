using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Utils;
using NeuralBreak.Config;

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

        // Config-driven max level (read from GameBalanceConfig.levels)
        private int MaxLevel => ConfigProvider.Balance?.levels?.totalLevels ?? 99;

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
            // EnemySpawner reference should be set in Inspector via GameSetup
            if (_enemySpawner == null)
            {
                Debug.LogError("[LevelManager] EnemySpawner reference not set! Assign in Inspector or via GameSetup.");
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
            LogHelper.Log($"[LevelManager] ========== OnGameStarted received! Mode = {evt.mode} ==========");

            // Delay by one frame to ensure EnemySpawner finishes its OnGameStarted first
            StartCoroutine(StartGameAfterFrame(evt.mode));
        }

        private System.Collections.IEnumerator StartGameAfterFrame(GameMode mode)
        {
            yield return null; // Wait one frame

            LogHelper.Log($"[LevelManager] Starting game after frame delay, mode = {mode}");

            // Route to appropriate mode based on event
            switch (mode)
            {
                case GameMode.Test:
                    LogHelper.Log("[LevelManager] Routing to StartTestMode()");
                    StartTestMode();
                    break;
                case GameMode.Rogue:
                    LogHelper.Log("[LevelManager] Routing to StartRogueMode()");
                    StartRogueMode();
                    break;
                case GameMode.Arcade:
                default:
                    LogHelper.Log("[LevelManager] Routing to StartNewGame() (Arcade)");
                    StartNewGame();
                    break;
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            RegisterKill(evt.enemyType);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start a new game at level 1 (ARCADE MODE)
        /// </summary>
        public void StartNewGame()
        {
            LogHelper.Log("[LevelManager] ========== STARTING ARCADE MODE ==========");
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

            LogHelper.Log($"[LevelManager] ARCADE MODE - Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start at a specific level
        /// </summary>
        public void StartAtLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, MaxLevel);
            _totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] Started at Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start test mode (endless with all enemies) - Level 999
        /// </summary>
        public void StartTestMode()
        {
            LogHelper.Log("[LevelManager] ========== STARTING TEST MODE ==========");
            _currentLevel = 999;
            _totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(999);  // This loads level 999 = TEST MODE

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] TEST MODE - Level {_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start rogue mode - Level 998
        /// </summary>
        public void StartRogueMode()
        {
            LogHelper.Log("[LevelManager] ========== STARTING ROGUE MODE ==========");
            _currentLevel = 998;
            _rogueCurrentLayer = 1;
            _totalElapsedTime = 0;
            ResetProgress();
            LoadRogueLayerConfig(_rogueCurrentLayer);  // This loads level 998 = ROGUE MODE

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = _currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] ROGUE MODE - Level {_currentLevel} Layer {_rogueCurrentLayer}: {CurrentLevelName}");
        }

        /// <summary>
        /// Advance to next level (called after transition completes)
        /// </summary>
        public void AdvanceLevel()
        {
            if (_currentLevel >= MaxLevel)
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

            LogHelper.Log($"[LevelManager] Advanced to Level {_currentLevel}: {CurrentLevelName}");
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

            LogHelper.Log($"[LevelManager] Advanced to Rogue Layer {_rogueCurrentLayer}: {CurrentLevelName}");
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
            return _currentLevel >= MaxLevel && _objectivesComplete;
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

                LogHelper.Log($"[LevelManager] Level {_currentLevel} objectives complete!");

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
            LogHelper.Log($"[LevelManager] LoadLevelConfig({level}) -> {_currentConfig.name}");
            LogHelper.Log($"[LevelManager] Spawn Rates: DataMite={_currentConfig.spawnRates.dataMiteRate}, ScanDrone={_currentConfig.spawnRates.scanDroneRate}, ChaosWorm={_currentConfig.spawnRates.chaosWormRate}, Boss={_currentConfig.spawnRates.bossRate}");
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
                Debug.LogError("[LevelManager] No EnemySpawner reference set! Cannot apply spawn rates.");
                return;
            }

            LogHelper.Log($"[LevelManager] ApplySpawnRates called for {_currentConfig.name}");
            LogHelper.Log($"[LevelManager] Setting DataMite={rates.dataMiteRate}, ScanDrone={rates.scanDroneRate}, ChaosWorm={rates.chaosWormRate}");

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

            LogHelper.Log($"[LevelManager] Applied spawn rates for {_currentConfig.name}");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Show Current Status")]
        private void DebugShowStatus()
        {
            if (_currentConfig == null)
            {
                LogHelper.Log("[LevelManager] No level loaded");
                return;
            }

            LogHelper.Log($"[LevelManager] Level {_currentLevel}: {_currentConfig.name}");
            LogHelper.Log($"  Progress: {GetLevelProgressPercent():F1}%");
            LogHelper.Log($"  DataMites: {_currentProgress.dataMites}/{_currentConfig.objectives.dataMites}");
            LogHelper.Log($"  ScanDrones: {_currentProgress.scanDrones}/{_currentConfig.objectives.scanDrones}");
            LogHelper.Log($"  ChaosWorms: {_currentProgress.chaosWorms}/{_currentConfig.objectives.chaosWorms}");
            LogHelper.Log($"  VoidSpheres: {_currentProgress.voidSpheres}/{_currentConfig.objectives.voidSpheres}");
            LogHelper.Log($"  CrystalShards: {_currentProgress.crystalShards}/{_currentConfig.objectives.crystalShards}");
            LogHelper.Log($"  Fizzers: {_currentProgress.fizzers}/{_currentConfig.objectives.fizzers}");
            LogHelper.Log($"  UFOs: {_currentProgress.ufos}/{_currentConfig.objectives.ufos}");
            LogHelper.Log($"  Bosses: {_currentProgress.bosses}/{_currentConfig.objectives.bosses}");
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
