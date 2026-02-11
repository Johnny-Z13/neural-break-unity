using UnityEngine;
using Z13.Core;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Global game state manager - persists across scenes.
    /// Handles high-level game flow (StartScreen → Playing → GameOver) and game mode tracking.
    /// This is a TRUE SINGLETON that lives in the Boot scene.
    ///
    /// Scene-specific gameplay logic (score, combo, enemies) belongs in GameManager (scene object).
    /// </summary>
    public class GameStateManager : MonoBehaviour, IBootable
    {
        public static GameStateManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameStateType m_currentState = GameStateType.StartScreen;
        [SerializeField] private GameMode m_currentMode = GameMode.Arcade;
        [SerializeField] private bool m_isPaused;

        // Public accessors
        public GameStateType CurrentState => m_currentState;
        public GameMode CurrentMode => m_currentMode;
        public bool IsPaused => m_isPaused;
        public bool IsPlaying => m_currentState == GameStateType.Playing && !m_isPaused;

        private bool m_initialized;

        private void Awake()
        {
            // Self-initialize if not already done by BootManager or EnsureGameStateManager
            // This allows the main scene to run directly during development
            if (!m_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Called by BootManager - NOT Awake. This ensures controlled initialization order.
        /// When running without Boot scene, Awake calls this instead.
        /// </summary>
        public void Initialize()
        {
            if (m_initialized) return;

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameStateManager] Duplicate instance - destroying self");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            m_currentState = GameStateType.StartScreen;
            m_isPaused = false;
            m_initialized = true;
            Debug.Log("[GameStateManager] Initialized");
        }

        #region State Management

        /// <summary>
        /// Set the game state. Publishes GameStateChangedEvent.
        /// </summary>
        public void SetState(GameStateType newState)
        {
            if (m_currentState == newState)
            {
                Debug.LogWarning($"[GameStateManager] Already in state {newState}");
                return;
            }

            // Validate state transitions
            if (m_currentState == GameStateType.GameOver && newState == GameStateType.Playing)
            {
                Debug.LogWarning("[GameStateManager] Cannot transition from GameOver to Playing directly. Use StartGame() instead.");
                return;
            }

            GameStateType previousState = m_currentState;
            m_currentState = newState;

            EventBus.Publish(new GameStateChangedEvent
            {
                previousState = previousState,
                newState = newState
            });

            Debug.Log($"[GameStateManager] State: {previousState} -> {newState}");
        }

        /// <summary>
        /// Set the game mode (Arcade, Rogue, Test).
        /// </summary>
        public void SetMode(GameMode mode)
        {
            m_currentMode = mode;
            Debug.Log($"[GameStateManager] Mode set to: {mode}");
        }

        /// <summary>
        /// Pause the game. Only works when in Playing state.
        /// </summary>
        public void PauseGame()
        {
            if (m_currentState != GameStateType.Playing)
            {
                Debug.LogWarning($"[GameStateManager] Cannot pause - not in Playing state (current: {m_currentState})");
                return;
            }

            m_isPaused = true;
            Time.timeScale = 0f;

            SetState(GameStateType.Paused);
            EventBus.Publish(new GamePausedEvent { isPaused = true });
        }

        /// <summary>
        /// Resume the game. Only works when in Paused state.
        /// </summary>
        public void ResumeGame()
        {
            if (m_currentState != GameStateType.Paused)
            {
                Debug.LogWarning($"[GameStateManager] Cannot resume - not in Paused state (current: {m_currentState})");
                return;
            }

            m_isPaused = false;
            Time.timeScale = 1f;

            SetState(GameStateType.Playing);
            EventBus.Publish(new GamePausedEvent { isPaused = false });
        }

        /// <summary>
        /// Return to menu. Resets time scale.
        /// </summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            m_isPaused = false;
            SetState(GameStateType.StartScreen);
        }

        /// <summary>
        /// Start a new game with the specified mode.
        /// </summary>
        public void StartGame(GameMode mode)
        {
            Debug.Log($"[GameStateManager] ========================================");
            Debug.Log($"[GameStateManager] STARTING GAME IN {mode} MODE");
            Debug.Log($"[GameStateManager] ========================================");

            m_currentMode = mode;
            m_isPaused = false;
            Time.timeScale = 1f;

            SetState(GameStateType.Playing);
            EventBus.Publish(new GameStartedEvent { mode = mode });
        }

        /// <summary>
        /// Trigger game over state.
        /// </summary>
        public void GameOver(GameStats finalStats)
        {
            SetState(GameStateType.GameOver);
            Time.timeScale = 1f;
            m_isPaused = false;

            EventBus.Publish(new GameOverEvent { finalStats = finalStats });
            Debug.Log($"[GameStateManager] Game Over! Score: {finalStats.score}, Level: {finalStats.level}");
        }

        /// <summary>
        /// Trigger victory state.
        /// </summary>
        public void Victory(GameStats finalStats)
        {
            finalStats.gameCompleted = true;
            SetState(GameStateType.Victory);

            EventBus.Publish(new VictoryEvent { finalStats = finalStats });
            Debug.Log("[GameStateManager] VICTORY! All 99 levels completed!");
        }

        /// <summary>
        /// Enter rogue choice state (upgrade selection between levels).
        /// </summary>
        public void EnterRogueChoice()
        {
            SetState(GameStateType.RogueChoice);
        }

        /// <summary>
        /// Exit rogue choice and resume playing.
        /// </summary>
        public void ExitRogueChoice()
        {
            if (m_currentState == GameStateType.RogueChoice)
            {
                SetState(GameStateType.Playing);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Start Arcade")]
        private void DebugStartArcade() => StartGame(GameMode.Arcade);

        [ContextMenu("Debug: Start Rogue")]
        private void DebugStartRogue() => StartGame(GameMode.Rogue);

        [ContextMenu("Debug: Start Test")]
        private void DebugStartTest() => StartGame(GameMode.Test);

        [ContextMenu("Debug: Pause")]
        private void DebugPause() => PauseGame();

        [ContextMenu("Debug: Resume")]
        private void DebugResume() => ResumeGame();

        [ContextMenu("Debug: Return To Menu")]
        private void DebugReturnToMenu() => ReturnToMenu();

        #endregion
    }
}
