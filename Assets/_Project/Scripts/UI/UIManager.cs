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
        [SerializeField] private GameObject _hudRoot;

        [Header("Settings")]
        [SerializeField] private bool _showHUDDuringPause = true;

        // Current active screen
        private ScreenBase _currentScreen;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Subscribe to events
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

            if (Instance == this)
            {
                Instance = null;
            }
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
            yield return null; // Wait one frame for UIBuilder to finish

            // Initialize based on current state
            if (GameManager.Instance != null)
            {
                Debug.Log($"[UIManager] Initializing with state: {GameManager.Instance.CurrentState}");
                UpdateUIForState(GameManager.Instance.CurrentState);
            }
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

                case GameStateType.GameOver:
                    ShowScreen(_gameOverScreen);
                    SetHUDVisible(false);
                    break;

                case GameStateType.Victory:
                    ShowScreen(_gameOverScreen); // Reuse game over screen for victory
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
