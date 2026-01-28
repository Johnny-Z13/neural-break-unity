using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Utils;
using NeuralBreak.Config;
using System.Collections;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Central game coordinator - manages game state, systems, and flow.
    /// Singleton pattern for easy access from other systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Trigger Recompile
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameStateType _currentState = GameStateType.StartScreen;
        [SerializeField] private GameMode _currentMode = GameMode.Arcade;
        [SerializeField] private bool _isPaused;
        [SerializeField] private bool _autoStartOnPlay = false; // DISABLED - let user choose mode via UI

        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private LevelManager _levelManager;

        // Note: MMFeedbacks removed - using native Unity feedback system
        // Feel package can be re-added later if desired

        // Game stats
        public GameStats Stats { get; private set; } = new GameStats();

        // Public accessors
        public GameStateType CurrentState => _currentState;
        public GameMode CurrentMode => _currentMode;
        public bool IsPaused => _isPaused;
        public bool IsPlaying => _currentState == GameStateType.Playing && !_isPaused;

        // Combo/Multiplier system
        private int _currentCombo;
        private float _currentMultiplier = 1f;
        private float _comboTimer;
        private const float COMBO_DECAY_TIME = 1.5f;
        private const float MULTIPLIER_DECAY_TIME = 2f;

        // Upgrade selection state
        private bool _upgradeSelected;

        private void Awake()
        {
            Debug.Log($"[GameManager] Awake START at {Time.realtimeSinceStartup:F3}s");

            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Subscribe to events
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);

            // Feedback setup disabled - Feel package not installed

            Debug.Log($"[GameManager] Awake DONE at {Time.realtimeSinceStartup:F3}s");
        }


        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<UpgradeSelectedEvent>(OnUpgradeSelected);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_autoStartOnPlay && _currentState == GameStateType.StartScreen)
            {
                StartGame(_currentMode);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            // Update survived time
            Stats.survivedTime += Time.deltaTime;

            // Combo decay
            UpdateComboDecay();
        }

        #region Game State Management

        public void SetState(GameStateType newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"[GameManager] Already in state {newState}!");
                return;
            }

            // Validate state transitions
            if (_currentState == GameStateType.GameOver && newState == GameStateType.Playing)
            {
                Debug.LogWarning("[GameManager] Cannot transition from GameOver to Playing directly. Use StartGame() instead.");
                return;
            }

            GameStateType previousState = _currentState;
            _currentState = newState;

            EventBus.Publish(new GameStateChangedEvent
            {
                previousState = previousState,
                newState = newState
            });

            LogHelper.Log($"[GameManager] State changed: {previousState} -> {newState}");
        }

        public void StartGame(GameMode mode)
        {
            LogHelper.Log($"[GameManager] ========================================");
            LogHelper.Log($"[GameManager] STARTING GAME IN {mode} MODE");
            LogHelper.Log($"[GameManager] ========================================");

            _currentMode = mode;
            Stats.Reset();
            _currentCombo = 0;
            _currentMultiplier = 1f;

            SetState(GameStateType.Playing);

            // Game start feedback (Feel package removed)

            // StartScreen will hide itself when it receives GameStartedEvent
            LogHelper.Log($"[GameManager] Publishing GameStartedEvent with mode: {mode}");
            EventBus.Publish(new GameStartedEvent { mode = mode });
            LogHelper.Log($"[GameManager] GameStartedEvent published successfully");
        }

        public void PauseGame()
        {
            if (_currentState != GameStateType.Playing)
            {
                Debug.LogWarning($"[GameManager] Cannot pause - not in Playing state (current: {_currentState})!");
                return;
            }

            _isPaused = true;
            Time.timeScale = 0f;

            SetState(GameStateType.Paused);
            EventBus.Publish(new GamePausedEvent { isPaused = true });
        }

        public void ResumeGame()
        {
            if (_currentState != GameStateType.Paused)
            {
                Debug.LogWarning($"[GameManager] Cannot resume - not in Paused state (current: {_currentState})!");
                return;
            }

            _isPaused = false;
            Time.timeScale = 1f;

            SetState(GameStateType.Playing);
            EventBus.Publish(new GamePausedEvent { isPaused = false });
        }

        public void GameOver()
        {
            SetState(GameStateType.GameOver);
            Time.timeScale = 1f;

            // Game over feedback (Feel package removed)

            EventBus.Publish(new GameOverEvent { finalStats = Stats });

            LogHelper.Log($"[GameManager] Game Over! Score: {Stats.score}, Level: {Stats.level}");
        }

        public void Victory()
        {
            Stats.gameCompleted = true;
            SetState(GameStateType.Victory);

            // Victory feedback (Feel package removed)

            EventBus.Publish(new VictoryEvent { finalStats = Stats });

            LogHelper.Log("[GameManager] VICTORY! All 99 levels completed!");
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            SetState(GameStateType.StartScreen);
        }

        #endregion

        #region Scoring & Combo

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.scoreValue < 0)
            {
                Debug.LogError($"[GameManager] Invalid score value from {evt.enemyType}: {evt.scoreValue}");
                return;
            }

            if (evt.xpValue < 0)
            {
                Debug.LogError($"[GameManager] Invalid XP value from {evt.enemyType}: {evt.xpValue}");
                return;
            }

            // Update kill counts
            Stats.enemiesKilled++;
            UpdateKillCount(evt.enemyType);

            // Update combo
            _currentCombo++;
            _comboTimer = COMBO_DECAY_TIME;

            if (_currentCombo > Stats.highestCombo)
            {
                Stats.highestCombo = _currentCombo;
            }

            // Update multiplier (increases with quick kills)
            if (_comboTimer > 0)
            {
                _currentMultiplier = Mathf.Min(_currentMultiplier + 0.1f, 10f);
                if (_currentMultiplier > Stats.highestMultiplier)
                {
                    Stats.highestMultiplier = _currentMultiplier;
                }
            }

            // Calculate score with multiplier
            int baseScore = evt.scoreValue;
            int finalScore = Mathf.RoundToInt(baseScore * _currentMultiplier);
            Stats.score += finalScore;

            // Add XP
            Stats.totalXP += evt.xpValue;

            // Publish events
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = _currentCombo,
                multiplier = _currentMultiplier
            });

            EventBus.Publish(new ScoreChangedEvent
            {
                newScore = Stats.score,
                delta = finalScore,
                worldPosition = evt.position
            });
        }

        private void UpdateKillCount(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.DataMite: Stats.dataMinersKilled++; break;
                case EnemyType.ScanDrone: Stats.scanDronesKilled++; break;
                case EnemyType.ChaosWorm: Stats.chaosWormsKilled++; break;
                case EnemyType.VoidSphere: Stats.voidSpheresKilled++; break;
                case EnemyType.CrystalShard: Stats.crystalSwarmsKilled++; break;
                case EnemyType.Fizzer: Stats.fizzersKilled++; break;
                case EnemyType.UFO: Stats.ufosKilled++; break;
                case EnemyType.Boss: Stats.bossesKilled++; break;
            }
        }

        private void UpdateComboDecay()
        {
            if (_currentCombo > 0)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0)
                {
                    _currentCombo = 0;
                    EventBus.Publish(new ComboChangedEvent
                    {
                        comboCount = 0,
                        multiplier = _currentMultiplier
                    });
                }
            }

            // Multiplier decays more slowly
            if (_currentMultiplier > 1f && _comboTimer <= 0)
            {
                _currentMultiplier = Mathf.Max(1f, _currentMultiplier - Time.deltaTime * 0.5f);
            }
        }

        public void ResetCombo()
        {
            _currentCombo = 0;
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = 0,
                multiplier = _currentMultiplier
            });
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            GameOver();
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            Stats.level = evt.levelNumber + 1;

            // Level complete feedback (Feel package removed)

            // Check for victory
            if (evt.levelNumber >= 99)
            {
                Victory();
            }
            else
            {
                // Start transition to next level
                StartCoroutine(LevelTransition());
            }
        }

        private IEnumerator LevelTransition()
        {
            LogHelper.Log("[GameManager] Level completed - transitioning...");

            // Clear remaining enemies
            if (_enemySpawner != null)
            {
                _enemySpawner.ClearAllEnemies();
            }
            else
            {
                Debug.LogWarning("[GameManager] EnemySpawner reference is null during level transition!");
            }

            // Check if we should show upgrade selection (every level in Rogue mode, every 5 levels in Arcade)
            bool showUpgradeSelection = ShouldShowUpgradeSelection();

            if (showUpgradeSelection)
            {
                // Pause game for upgrade selection
                SetState(GameStateType.RogueChoice);

                // Show upgrade selection screen via UI manager
                var uiManager = FindFirstObjectByType<UI.UIManager>();
                if (uiManager != null)
                {
                    // UIManager will show upgrade selection screen
                    // For now, just publish event to trigger it
                    EventBus.Publish(new UpgradeSelectionStartedEvent
                    {
                        options = new System.Collections.Generic.List<Combat.UpgradeDefinition>()
                    });
                }

                // Wait for player to select upgrade (with timeout protection)
                _upgradeSelected = false;
                float upgradeTimeout = 60f; // Max 60 seconds to select
                float upgradeWaitTime = 0f;

                while (!_upgradeSelected && upgradeWaitTime < upgradeTimeout)
                {
                    yield return null;
                    upgradeWaitTime += Time.unscaledDeltaTime;
                }

                if (!_upgradeSelected)
                {
                    Debug.LogWarning($"[GameManager] Upgrade selection timed out after {upgradeTimeout}s, continuing to next level...");
                }
            }
            else
            {
                // Brief pause before next level (no upgrade selection)
                yield return new WaitForSeconds(2f);
            }

            // Advance to next level
            if (_levelManager != null)
            {
                _levelManager.AdvanceLevel();
            }
            else if (LevelManager.Instance != null)
            {
                LevelManager.Instance.AdvanceLevel();
            }
            else
            {
                Debug.LogError("[GameManager] No LevelManager available for level transition!");
            }

            // Resume game
            SetState(GameStateType.Playing);

            // Resume spawning
            if (_enemySpawner != null)
            {
                _enemySpawner.StartSpawning();
            }
        }

        private bool ShouldShowUpgradeSelection()
        {
            // Always show in Rogue mode
            if (_currentMode == GameMode.Rogue)
            {
                return true;
            }

            // In Arcade mode, use config setting (default: every 1 level for testing)
            if (_currentMode == GameMode.Arcade)
            {
                int interval = ConfigProvider.Balance?.upgradeSystem?.showUpgradeEveryNLevels ?? 5;
                return Stats.level % interval == 0;
            }

            return false;
        }

        private void OnUpgradeSelected(UpgradeSelectedEvent evt)
        {
            if (evt.selected != null)
            {
                LogHelper.Log($"[GameManager] Upgrade selected: {evt.selected.displayName}");
            }
            else
            {
                LogHelper.LogWarning("[GameManager] No upgrade selected (no upgrades available)");
            }
            _upgradeSelected = true;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Start Arcade")]
        private void DebugStartArcade() => StartGame(GameMode.Arcade);

        [ContextMenu("Debug: Game Over")]
        private void DebugGameOver() => GameOver();

        [ContextMenu("Debug: Victory")]
        private void DebugVictory() => Victory();

        #endregion
    }
}
