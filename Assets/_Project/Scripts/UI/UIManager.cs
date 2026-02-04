using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Input;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Manages UI screen visibility based on game state.
    /// Singleton pattern for easy access.
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
            // Singleton setup

            // Subscribe to events
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

        }

        private void Start()
        {
            // Subscribe to pause input
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnPausePressed += OnPausePressed;
            }

            // Delay initialization to let UIBuilder wire references
            StartCoroutine(DelayedInit());
        }

        private System.Collections.IEnumerator DelayedInit()
        {
            Debug.Log("[UIManager] DelayedInit starting...");

            // Wait for GameManager.Instance with timeout protection
            float timeout = 5f;
            float elapsed = 0f;

            while (GameManager.Instance == null && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError($"[UIManager] GameManager.Instance not found after {timeout}s timeout! UI may not initialize correctly.");
                // Try to show start screen anyway as fallback
                ShowScreen(m_startScreen);
                SetHUDVisible(false);
                yield break;
            }

            Debug.Log($"[UIManager] GameManager found after {elapsed:F2}s. Initializing with state: {GameManager.Instance.CurrentState}");
            UpdateUIForState(GameManager.Instance.CurrentState);
        }

        private void OnPausePressed()
        {
            if (GameManager.Instance == null) return;

            var state = GameManager.Instance.CurrentState;

            if (state == GameStateType.Playing)
            {
                GameManager.Instance.PauseGame();
            }
            else if (state == GameStateType.Paused)
            {
                GameManager.Instance.ResumeGame();
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
                    // Don't show _gameOverScreen to avoid duplicate "GAME OVER" text
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
