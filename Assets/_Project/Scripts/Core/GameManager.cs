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
        [SerializeField] private GameStateType m_currentState = GameStateType.StartScreen;
        [SerializeField] private GameMode m_currentMode = GameMode.Arcade;
        [SerializeField] private bool m_isPaused;
        [SerializeField] private bool m_autoStartOnPlay = false; // DISABLED - let user choose mode via UI

        [Header("References")]
        [SerializeField] private PlayerController m_player;
        [SerializeField] private EnemySpawner m_enemySpawner;
        [SerializeField] private LevelManager m_levelManager;

        // Note: MMFeedbacks removed - using native Unity feedback system
        // Feel package can be re-added later if desired

        // Game stats
        public GameStats Stats { get; private set; } = new GameStats();

        // Public accessors
        public GameStateType CurrentState => m_currentState;
        public GameMode CurrentMode => m_currentMode;
        public bool IsPaused => m_isPaused;
        public bool IsPlaying => m_currentState == GameStateType.Playing && !m_isPaused;

        // Combo/Multiplier system
        private int m_currentCombo;
        private float m_currentMultiplier = 1f;
        private float m_comboTimer;

        // Config-driven combo settings (read from GameBalanceConfig)
        private float ComboDecayTime => ConfigProvider.Combo?.comboDecayTime ?? 3f;
        private float ComboWindow => ConfigProvider.Combo?.comboWindow ?? 1.5f;
        private float MultiplierPerKill => ConfigProvider.Combo?.multiplierPerKill ?? 0.1f;
        private float MaxMultiplier => ConfigProvider.Combo?.maxMultiplier ?? 10f;
        private float MultiplierDecayRate => ConfigProvider.Combo?.multiplierDecayRate ?? 2f;
        private float BossKillMultiplier => ConfigProvider.Combo?.bossKillMultiplier ?? 2f;

        // Upgrade selection state
        private bool m_upgradeSelected;

        // Level transition tracking
        private Coroutine m_levelTransitionCoroutine;
        private bool m_isPlayerDead;

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
            if (m_autoStartOnPlay && m_currentState == GameStateType.StartScreen)
            {
                StartGame(m_currentMode);
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
            if (m_currentState == newState)
            {
                Debug.LogWarning($"[GameManager] Already in state {newState}!");
                return;
            }

            // Validate state transitions
            if (m_currentState == GameStateType.GameOver && newState == GameStateType.Playing)
            {
                Debug.LogWarning("[GameManager] Cannot transition from GameOver to Playing directly. Use StartGame() instead.");
                return;
            }

            GameStateType previousState = m_currentState;
            m_currentState = newState;

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

            m_currentMode = mode;
            Stats.Reset();
            m_currentCombo = 0;
            m_currentMultiplier = 1f;
            m_isPlayerDead = false;
            m_levelTransitionCoroutine = null;

            SetState(GameStateType.Playing);

            // Game start feedback (Feel package removed)

            // StartScreen will hide itself when it receives GameStartedEvent
            LogHelper.Log($"[GameManager] Publishing GameStartedEvent with mode: {mode}");
            EventBus.Publish(new GameStartedEvent { mode = mode });
            LogHelper.Log($"[GameManager] GameStartedEvent published successfully");
        }

        public void PauseGame()
        {
            if (m_currentState != GameStateType.Playing)
            {
                Debug.LogWarning($"[GameManager] Cannot pause - not in Playing state (current: {m_currentState})!");
                return;
            }

            m_isPaused = true;
            Time.timeScale = 0f;

            SetState(GameStateType.Paused);
            EventBus.Publish(new GamePausedEvent { isPaused = true });
        }

        public void ResumeGame()
        {
            if (m_currentState != GameStateType.Paused)
            {
                Debug.LogWarning($"[GameManager] Cannot resume - not in Paused state (current: {m_currentState})!");
                return;
            }

            m_isPaused = false;
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
            m_isPaused = false;
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

            // Update combo (config-driven)
            m_currentCombo++;
            m_comboTimer = ComboDecayTime;

            if (m_currentCombo > Stats.highestCombo)
            {
                Stats.highestCombo = m_currentCombo;
            }

            // Update multiplier (increases with quick kills, config-driven)
            if (m_comboTimer > 0)
            {
                m_currentMultiplier = Mathf.Min(m_currentMultiplier + MultiplierPerKill, MaxMultiplier);
                if (m_currentMultiplier > Stats.highestMultiplier)
                {
                    Stats.highestMultiplier = m_currentMultiplier;
                }
            }

            // Calculate score with multiplier (apply boss multiplier if applicable)
            int baseScore = evt.scoreValue;
            float scoreMultiplier = m_currentMultiplier;
            if (evt.enemyType == EnemyType.Boss)
            {
                scoreMultiplier *= BossKillMultiplier;
            }
            int finalScore = Mathf.RoundToInt(baseScore * scoreMultiplier);
            Stats.score += finalScore;

            // Add XP
            Stats.totalXP += evt.xpValue;

            // Publish events
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = m_currentCombo,
                multiplier = m_currentMultiplier
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
            if (m_currentCombo > 0)
            {
                m_comboTimer -= Time.deltaTime;
                if (m_comboTimer <= 0)
                {
                    m_currentCombo = 0;
                    EventBus.Publish(new ComboChangedEvent
                    {
                        comboCount = 0,
                        multiplier = m_currentMultiplier
                    });
                }
            }

            // Multiplier decays more slowly (config-driven decay rate)
            if (m_currentMultiplier > 1f && m_comboTimer <= 0)
            {
                float decayRate = MultiplierDecayRate > 0 ? 1f / MultiplierDecayRate : 0.5f;
                m_currentMultiplier = Mathf.Max(1f, m_currentMultiplier - Time.deltaTime * decayRate);
            }
        }

        public void ResetCombo()
        {
            m_currentCombo = 0;
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = 0,
                multiplier = m_currentMultiplier
            });
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            m_isPlayerDead = true;

            // Cancel any ongoing level transition to prevent upgrade screen showing
            if (m_levelTransitionCoroutine != null)
            {
                StopCoroutine(m_levelTransitionCoroutine);
                m_levelTransitionCoroutine = null;
            }

            // Small delay to let death explosion play, then show game over
            StartCoroutine(DelayedGameOver(1.5f));
        }

        private IEnumerator DelayedGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameOver();
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Don't process level completion if player is dead
            if (m_isPlayerDead) return;

            Stats.level = evt.levelNumber + 1;

            // Level complete feedback (Feel package removed)

            // Check for victory
            if (evt.levelNumber >= 99)
            {
                Victory();
            }
            else
            {
                // Start transition to next level (track coroutine so we can cancel if player dies)
                m_levelTransitionCoroutine = StartCoroutine(LevelTransition());
            }
        }

        private IEnumerator LevelTransition()
        {
            LogHelper.Log("[GameManager] Level completed - transitioning...");

            // Abort if player died during transition
            if (m_isPlayerDead)
            {
                m_levelTransitionCoroutine = null;
                yield break;
            }

            // Clear remaining enemies
            if (m_enemySpawner != null)
            {
                m_enemySpawner.ClearAllEnemies();
            }
            else
            {
                Debug.LogWarning("[GameManager] EnemySpawner reference is null during level transition!");
            }

            // Check if we should show upgrade selection (every level in Rogue mode, every 5 levels in Arcade)
            bool showUpgradeSelection = ShouldShowUpgradeSelection();

            // Don't show upgrade selection if player is dead
            if (showUpgradeSelection && !m_isPlayerDead)
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
                m_upgradeSelected = false;
                float upgradeTimeout = 60f; // Max 60 seconds to select
                float upgradeWaitTime = 0f;

                while (!m_upgradeSelected && upgradeWaitTime < upgradeTimeout)
                {
                    yield return null;
                    upgradeWaitTime += Time.unscaledDeltaTime;
                }

                if (!m_upgradeSelected)
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
            if (m_levelManager != null)
            {
                m_levelManager.AdvanceLevel();
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
            if (m_enemySpawner != null)
            {
                m_enemySpawner.StartSpawning();
            }
        }

        private bool ShouldShowUpgradeSelection()
        {
            // Always show in Rogue mode
            if (m_currentMode == GameMode.Rogue)
            {
                return true;
            }

            // In Arcade mode, use config setting (default: every 1 level for testing)
            if (m_currentMode == GameMode.Arcade)
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
            m_upgradeSelected = true;
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
