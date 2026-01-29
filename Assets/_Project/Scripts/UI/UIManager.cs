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
        [SerializeField] private ScreenBase _startScreen;
        [SerializeField] private ScreenBase _pauseScreen;
        [SerializeField] private ScreenBase _gameOverScreen;
        [SerializeField] private UpgradeSelectionScreen _upgradeSelectionScreen;
        [SerializeField] private GameObject _hudRoot;

        [Header("Settings")]
        [SerializeField] private bool _showHUDDuringPause = true;

        // Current active screen
        private ScreenBase _currentScreen;

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
                ShowScreen(_startScreen);
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
                    ShowScreen(_startScreen);
                    SetHUDVisible(false);
                    break;

                case GameStateType.Playing:
                    SetHUDVisible(true);
                    break;

                case GameStateType.Paused:
                    ShowScreen(_pauseScreen);
                    SetHUDVisible(_showHUDDuringPause);
                    break;

                case GameStateType.RogueChoice:
                    ShowScreen(_upgradeSelectionScreen);
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

            _currentScreen = screen;
            screen.Show();
        }

        private void HideAllScreens()
        {
            _startScreen?.Hide();
            _pauseScreen?.Hide();
            _gameOverScreen?.Hide();
            _upgradeSelectionScreen?.Hide();
            _currentScreen = null;
        }

        private void SetHUDVisible(bool visible)
        {
            if (_hudRoot != null)
            {
                _hudRoot.SetActive(visible);
            }
        }

        /// <summary>
        /// Get the currently active screen
        /// </summary>
        public ScreenBase GetCurrentScreen() => _currentScreen;

        /// <summary>
        /// Check if any menu screen is open
        /// </summary>
        public bool IsMenuOpen() => _currentScreen != null && _currentScreen.IsVisible;

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
