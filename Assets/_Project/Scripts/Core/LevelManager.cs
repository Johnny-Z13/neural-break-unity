using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages the 99-level progression system with objective-based advancement.
    /// Each level has specific kill objectives that must be completed.
    /// Controls enemy spawn rates based on level configuration.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Settings")]
        [SerializeField] private int m_currentLevel = 1;

        // Config-driven max level (read from GameBalanceConfig.levels)
        private int MaxLevel => ConfigProvider.Balance?.levels?.totalLevels ?? 99;

        [Header("References")]
        [SerializeField] private EnemySpawner m_enemySpawner;

        [Header("Rogue Mode")]
        [SerializeField] private int m_rogueCurrentLayer = 1;

        // Current state
        private LevelConfig m_currentConfig;
        private LevelProgress m_currentProgress;
        private bool m_objectivesComplete;
        private float m_totalElapsedTime;
        private bool m_isTransitioning;

        // Public accessors
        public int CurrentLevel => m_currentLevel;
        public string CurrentLevelName => m_currentConfig?.name ?? $"Level {m_currentLevel}";
        public LevelProgress CurrentProgress => m_currentProgress;
        public bool ObjectivesComplete => m_objectivesComplete;
        public float TotalElapsedTime => m_totalElapsedTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LevelManager] Multiple instances detected");
            }
            Instance = this;
        }

        private void Start()
        {
            // EnemySpawner reference should be set in Inspector via GameSetup
            if (m_enemySpawner == null)
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
            m_totalElapsedTime += Time.deltaTime;
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
            m_currentLevel = 1;
            m_totalElapsedTime = 0;
            m_rogueCurrentLayer = 1;
            ResetProgress();
            LoadLevelConfig(m_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] ARCADE MODE - Level {m_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start at a specific level
        /// </summary>
        public void StartAtLevel(int level)
        {
            m_currentLevel = Mathf.Clamp(level, 1, MaxLevel);
            m_totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(m_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] Started at Level {m_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start test mode (endless with all enemies) - Level 999
        /// </summary>
        public void StartTestMode()
        {
            LogHelper.Log("[LevelManager] ========== STARTING TEST MODE ==========");
            m_currentLevel = 999;
            m_totalElapsedTime = 0;
            ResetProgress();
            LoadLevelConfig(999);  // This loads level 999 = TEST MODE

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] TEST MODE - Level {m_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Start rogue mode - Level 998
        /// </summary>
        public void StartRogueMode()
        {
            LogHelper.Log("[LevelManager] ========== STARTING ROGUE MODE ==========");
            m_currentLevel = 998;
            m_rogueCurrentLayer = 1;
            m_totalElapsedTime = 0;
            ResetProgress();
            LoadRogueLayerConfig(m_rogueCurrentLayer);  // This loads level 998 = ROGUE MODE

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] ROGUE MODE - Level {m_currentLevel} Layer {m_rogueCurrentLayer}: {CurrentLevelName}");
        }

        /// <summary>
        /// Advance to next level (called after transition completes)
        /// </summary>
        public void AdvanceLevel()
        {
            if (m_currentLevel >= MaxLevel)
            {
                // Game complete!
                EventBus.Publish(new GameCompletedEvent
                {
                    finalLevel = m_currentLevel,
                    totalTime = m_totalElapsedTime
                });
                return;
            }

            m_currentLevel++;
            ResetProgress();
            LoadLevelConfig(m_currentLevel);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] Advanced to Level {m_currentLevel}: {CurrentLevelName}");
        }

        /// <summary>
        /// Advance rogue layer
        /// </summary>
        public void AdvanceRogueLayer()
        {
            m_rogueCurrentLayer++;
            ResetProgress();
            LoadRogueLayerConfig(m_rogueCurrentLayer);

            EventBus.Publish(new LevelStartedEvent
            {
                levelNumber = m_currentLevel,
                levelName = CurrentLevelName
            });

            LogHelper.Log($"[LevelManager] Advanced to Rogue Layer {m_rogueCurrentLayer}: {CurrentLevelName}");
        }

        /// <summary>
        /// Get current level objectives
        /// </summary>
        public LevelObjectives GetObjectives()
        {
            return m_currentConfig?.objectives ?? new LevelObjectives();
        }

        /// <summary>
        /// Get level progress percentage (0-100)
        /// </summary>
        public float GetLevelProgressPercent()
        {
            if (m_currentConfig == null) return 0f;
            return m_currentProgress.GetProgressPercent(m_currentConfig.objectives);
        }

        /// <summary>
        /// Get total game progress percentage (0-100)
        /// </summary>
        public float GetTotalProgressPercent()
        {
            float completedLevels = m_currentLevel - 1;
            float currentLevelProgress = GetLevelProgressPercent() / 100f;
            return ((completedLevels + currentLevelProgress) / LevelGenerator.TOTAL_LEVELS) * 100f;
        }

        /// <summary>
        /// Check if game is complete
        /// </summary>
        public bool IsGameComplete()
        {
            return m_currentLevel >= MaxLevel && m_objectivesComplete;
        }

        #endregion

        #region Objective Tracking

        /// <summary>
        /// Register an enemy kill for objective tracking
        /// </summary>
        public void RegisterKill(EnemyType enemyType)
        {
            if (m_objectivesComplete || m_isTransitioning) return;

            switch (enemyType)
            {
                case EnemyType.DataMite:
                    m_currentProgress.dataMites++;
                    break;
                case EnemyType.ScanDrone:
                    m_currentProgress.scanDrones++;
                    break;
                case EnemyType.ChaosWorm:
                    m_currentProgress.chaosWorms++;
                    break;
                case EnemyType.VoidSphere:
                    m_currentProgress.voidSpheres++;
                    break;
                case EnemyType.CrystalShard:
                    m_currentProgress.crystalShards++;
                    break;
                case EnemyType.Fizzer:
                    m_currentProgress.fizzers++;
                    break;
                case EnemyType.UFO:
                    m_currentProgress.ufos++;
                    break;
                case EnemyType.Boss:
                    m_currentProgress.bosses++;
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
            if (m_objectivesComplete || m_currentConfig == null) return;

            if (m_currentProgress.MeetsObjectives(m_currentConfig.objectives))
            {
                m_objectivesComplete = true;
                m_isTransitioning = true;

                LogHelper.Log($"[LevelManager] Level {m_currentLevel} objectives complete!");

                // Publish level completed event
                EventBus.Publish(new LevelCompletedEvent
                {
                    levelNumber = m_currentLevel,
                    levelName = CurrentLevelName,
                    completionTime = m_totalElapsedTime
                });

                // Stop enemy spawning
                if (m_enemySpawner != null)
                {
                    m_enemySpawner.StopSpawning();
                }

                // Clear remaining enemies and trigger transition
                // The GameManager or transition system should handle this
                // and call AdvanceLevel() when ready
            }
        }

        private void ResetProgress()
        {
            m_currentProgress = LevelProgress.Empty;
            m_objectivesComplete = false;
            m_isTransitioning = false;
        }

        #endregion

        #region Level Configuration

        private void LoadLevelConfig(int level)
        {
            m_currentConfig = LevelGenerator.GetLevelConfig(level);
            LogHelper.Log($"[LevelManager] LoadLevelConfig({level}) -> {m_currentConfig.name}");
            LogHelper.Log($"[LevelManager] Spawn Rates: DataMite={m_currentConfig.spawnRates.dataMiteRate}, ScanDrone={m_currentConfig.spawnRates.scanDroneRate}, ChaosWorm={m_currentConfig.spawnRates.chaosWormRate}, Boss={m_currentConfig.spawnRates.bossRate}");
            ApplySpawnRates(m_currentConfig.spawnRates);
        }

        private void LoadRogueLayerConfig(int layer)
        {
            m_currentConfig = LevelGenerator.GetRogueLevelConfig(layer);
            ApplySpawnRates(m_currentConfig.spawnRates);
        }

        private void ApplySpawnRates(SpawnRates rates)
        {
            if (m_enemySpawner == null)
            {
                Debug.LogError("[LevelManager] No EnemySpawner reference set! Cannot apply spawn rates.");
                return;
            }

            LogHelper.Log($"[LevelManager] ApplySpawnRates called for {m_currentConfig.name}");
            LogHelper.Log($"[LevelManager] Setting DataMite={rates.dataMiteRate}, ScanDrone={rates.scanDroneRate}, ChaosWorm={rates.chaosWormRate}");

            m_enemySpawner.SetSpawnRates(
                rates.dataMiteRate,
                rates.scanDroneRate,
                rates.fizzerRate,
                rates.ufoRate,
                rates.chaosWormRate,
                rates.voidSphereRate,
                rates.crystalShardRate,
                rates.bossRate
            );

            LogHelper.Log($"[LevelManager] Applied spawn rates for {m_currentConfig.name}");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Show Current Status")]
        private void DebugShowStatus()
        {
            if (m_currentConfig == null)
            {
                LogHelper.Log("[LevelManager] No level loaded");
                return;
            }

            LogHelper.Log($"[LevelManager] Level {m_currentLevel}: {m_currentConfig.name}");
            LogHelper.Log($"  Progress: {GetLevelProgressPercent():F1}%");
            LogHelper.Log($"  DataMites: {m_currentProgress.dataMites}/{m_currentConfig.objectives.dataMites}");
            LogHelper.Log($"  ScanDrones: {m_currentProgress.scanDrones}/{m_currentConfig.objectives.scanDrones}");
            LogHelper.Log($"  ChaosWorms: {m_currentProgress.chaosWorms}/{m_currentConfig.objectives.chaosWorms}");
            LogHelper.Log($"  VoidSpheres: {m_currentProgress.voidSpheres}/{m_currentConfig.objectives.voidSpheres}");
            LogHelper.Log($"  CrystalShards: {m_currentProgress.crystalShards}/{m_currentConfig.objectives.crystalShards}");
            LogHelper.Log($"  Fizzers: {m_currentProgress.fizzers}/{m_currentConfig.objectives.fizzers}");
            LogHelper.Log($"  UFOs: {m_currentProgress.ufos}/{m_currentConfig.objectives.ufos}");
            LogHelper.Log($"  Bosses: {m_currentProgress.bosses}/{m_currentConfig.objectives.bosses}");
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
            if (m_currentConfig == null) return;

            m_currentProgress = new LevelProgress
            {
                dataMites = m_currentConfig.objectives.dataMites,
                scanDrones = m_currentConfig.objectives.scanDrones,
                chaosWorms = m_currentConfig.objectives.chaosWorms,
                voidSpheres = m_currentConfig.objectives.voidSpheres,
                crystalShards = m_currentConfig.objectives.crystalShards,
                fizzers = m_currentConfig.objectives.fizzers,
                ufos = m_currentConfig.objectives.ufos,
                bosses = m_currentConfig.objectives.bosses
            };

            CheckObjectivesComplete();
        }

        #endregion
    }
}
