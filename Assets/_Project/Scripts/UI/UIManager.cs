using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Input;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Manages UI screen visibility based on game state.
    ///
    /// SCENE-SPECIFIC - Lives in the main scene, not a singleton.
    /// Uses GameStateManager (true singleton) for state queries.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private ScreenBase m_startScreen;
        [SerializeField] private ScreenBase m_pauseScreen;
        [SerializeField] private ScreenBase m_gameOverScreen;
        [SerializeField] private UpgradeSelectionScreen m_upgradeSelectionScreen;
        [SerializeField] private GameObject m_hudRoot;

        [Header("Settings")]
        [SerializeField] private bool m_showHUDDuringPause = true;

        // Current active screen
        private ScreenBase m_currentScreen;

        private void Awake()
        {
            // Subscribe to events
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

            // Unsubscribe from input
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPausePressed -= OnPausePressed;
            }
        }

        private void Start()
        {
            // Subscribe to pause input - InputManager is guaranteed to exist (Boot scene)
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPausePressed += OnPausePressed;
            }

            // Initialize UI state - GameStateManager is guaranteed to exist (Boot scene)
            // No more timeout loops needed!
            var currentState = GameStateManager.Instance?.CurrentState ?? GameStateType.StartScreen;
            Debug.Log($"[UIManager] Initializing with state: {currentState}");
            UpdateUIForState(currentState);
        }

        private void OnPausePressed()
        {
            // GameStateManager is guaranteed to exist (Boot scene)
            var state = GameStateManager.Instance.CurrentState;

            if (state == GameStateType.Playing)
            {
                GameStateManager.Instance.PauseGame();
            }
            else if (state == GameStateType.Paused)
            {
                GameStateManager.Instance.ResumeGame();
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdateUIForState(evt.newState);
        }

        private void UpdateUIForState(GameStateType state)
        {
            // Hide all screens first
            HideAllScreens();

            switch (state)
            {
                case GameStateType.StartScreen:
                    ShowScreen(m_startScreen);
                    SetHUDVisible(false);
                    break;

                case GameStateType.Playing:
                    SetHUDVisible(true);
                    break;

                case GameStateType.Paused:
                    ShowScreen(m_pauseScreen);
                    SetHUDVisible(m_showHUDDuringPause);
                    break;

                case GameStateType.RogueChoice:
                    ShowScreen(m_upgradeSelectionScreen);
                    SetHUDVisible(false);
                    break;

                case GameStateType.GameOver:
                    // StatisticsScreen handles its own visibility via GameOverEvent
                    SetHUDVisible(false);
                    break;

                case GameStateType.Victory:
                    // StatisticsScreen handles its own visibility via VictoryEvent
                    SetHUDVisible(false);
                    break;
            }
        }

        private void ShowScreen(ScreenBase screen)
        {
            if (screen == null) return;

            m_currentScreen = screen;
            screen.Show();
        }

        private void HideAllScreens()
        {
            m_startScreen?.Hide();
            m_pauseScreen?.Hide();
            m_gameOverScreen?.Hide();
            m_upgradeSelectionScreen?.Hide();
            m_currentScreen = null;
        }

        private void SetHUDVisible(bool visible)
        {
            if (m_hudRoot != null)
            {
                m_hudRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// Get the currently active screen
        /// </summary>
        public ScreenBase GetCurrentScreen() => m_currentScreen;

        /// <summary>
        /// Check if any menu screen is open
        /// </summary>
        public bool IsMenuOpen() => m_currentScreen != null && m_currentScreen.IsVisible;

        #region Debug

        [ContextMenu("Debug: Show Start Screen")]
        private void DebugShowStart() => UpdateUIForState(GameStateType.StartScreen);

        [ContextMenu("Debug: Show Pause Screen")]
        private void DebugShowPause() => UpdateUIForState(GameStateType.Paused);

        [ContextMenu("Debug: Show Game Over")]
        private void DebugShowGameOver() => UpdateUIForState(GameStateType.GameOver);

        #endregion
    }
}
