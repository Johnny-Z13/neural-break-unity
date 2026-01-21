using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Game Over / Victory screen displaying final stats.
    /// </summary>
    public class GameOverScreen : ScreenBase
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private string _gameOverTitle = "GAME OVER";
        [SerializeField] private string _victoryTitle = "VICTORY!";

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private TextMeshProUGUI _timeSurvivedText;
        [SerializeField] private TextMeshProUGUI _enemiesKilledText;
        [SerializeField] private TextMeshProUGUI _levelReachedText;
        [SerializeField] private TextMeshProUGUI _highestComboText;
        [SerializeField] private TextMeshProUGUI _highestMultiplierText;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        // Cached state
        private bool _isVictory;

        protected override void Awake()
        {
            base.Awake();

            // Wire up button events
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            // Subscribe to events
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<VictoryEvent>(OnVictory);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<VictoryEvent>(OnVictory);
        }

        protected override void OnShow()
        {
            // Ensure first button is selected
            if (_firstSelected == null && _restartButton != null)
            {
                _firstSelected = _restartButton;
            }

            // Update title based on victory state
            if (_titleText != null)
            {
                _titleText.text = _isVictory ? _victoryTitle : _gameOverTitle;
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            _isVictory = false;
            UpdateStats(evt.finalStats);
        }

        private void OnVictory(VictoryEvent evt)
        {
            _isVictory = true;
            UpdateStats(evt.finalStats);
        }

        private void UpdateStats(GameStats stats)
        {
            if (stats == null) return;

            if (_finalScoreText != null)
            {
                _finalScoreText.text = $"FINAL SCORE: {stats.score:N0}";
            }

            if (_timeSurvivedText != null)
            {
                int minutes = Mathf.FloorToInt(stats.survivedTime / 60f);
                int seconds = Mathf.FloorToInt(stats.survivedTime % 60f);
                _timeSurvivedText.text = $"TIME: {minutes:00}:{seconds:00}";
            }

            if (_enemiesKilledText != null)
            {
                _enemiesKilledText.text = $"ENEMIES KILLED: {stats.enemiesKilled:N0}";
            }

            if (_levelReachedText != null)
            {
                _levelReachedText.text = $"LEVEL REACHED: {stats.level}";
            }

            if (_highestComboText != null)
            {
                _highestComboText.text = $"HIGHEST COMBO: {stats.highestCombo}x";
            }

            if (_highestMultiplierText != null)
            {
                _highestMultiplierText.text = $"BEST MULTIPLIER: {stats.highestMultiplier:F1}x";
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(GameManager.Instance.CurrentMode);
            }
        }

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
            }
        }

        #region Debug

        [ContextMenu("Debug: Show Game Over")]
        private void DebugGameOver()
        {
            _isVictory = false;
            var stats = new GameStats
            {
                score = 125000,
                survivedTime = 342f,
                enemiesKilled = 487,
                level = 15,
                highestCombo = 32,
                highestMultiplier = 4.5f
            };
            UpdateStats(stats);
            Show();
        }

        [ContextMenu("Debug: Show Victory")]
        private void DebugVictory()
        {
            _isVictory = true;
            var stats = new GameStats
            {
                score = 999999,
                survivedTime = 3600f,
                enemiesKilled = 9999,
                level = 99,
                highestCombo = 100,
                highestMultiplier = 10f,
                gameCompleted = true
            };
            UpdateStats(stats);
            Show();
        }

        #endregion
    }
}
