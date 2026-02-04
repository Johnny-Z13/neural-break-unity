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
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private string m_gameOverTitle = "GAME OVER";
        [SerializeField] private string m_victoryTitle = "VICTORY!";

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI m_finalScoreText;
        [SerializeField] private TextMeshProUGUI m_timeSurvivedText;
        [SerializeField] private TextMeshProUGUI m_enemiesKilledText;
        [SerializeField] private TextMeshProUGUI m_levelReachedText;
        [SerializeField] private TextMeshProUGUI m_highestComboText;
        [SerializeField] private TextMeshProUGUI m_highestMultiplierText;

        [Header("Buttons")]
        [SerializeField] private Button m_restartButton;
        [SerializeField] private Button m_mainMenuButton;

        // Cached state
        private bool m_isVictory;

        protected override void Awake()
        {
            base.Awake();

            // Wire up button events
            if (m_restartButton != null)
            {
                m_restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (m_mainMenuButton != null)
            {
                m_mainMenuButton.onClick.AddListener(OnMainMenuClicked);
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
            if (m_firstSelected == null && m_restartButton != null)
            {
                m_firstSelected = m_restartButton;
            }

            // Update title based on victory state
            if (m_titleText != null)
            {
                m_titleText.text = m_isVictory ? m_victoryTitle : m_gameOverTitle;
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            m_isVictory = false;
            UpdateStats(evt.finalStats);
        }

        private void OnVictory(VictoryEvent evt)
        {
            m_isVictory = true;
            UpdateStats(evt.finalStats);
        }

        private void UpdateStats(GameStats stats)
        {
            if (stats == null) return;

            if (m_finalScoreText != null)
            {
                m_finalScoreText.text = $"FINAL SCORE: {stats.score:N0}";
            }

            if (m_timeSurvivedText != null)
            {
                int minutes = Mathf.FloorToInt(stats.survivedTime / 60f);
                int seconds = Mathf.FloorToInt(stats.survivedTime % 60f);
                m_timeSurvivedText.text = $"TIME: {minutes:00}:{seconds:00}";
            }

            if (m_enemiesKilledText != null)
            {
                m_enemiesKilledText.text = $"ENEMIES KILLED: {stats.enemiesKilled:N0}";
            }

            if (m_levelReachedText != null)
            {
                m_levelReachedText.text = $"LEVEL REACHED: {stats.level}";
            }

            if (m_highestComboText != null)
            {
                m_highestComboText.text = $"HIGHEST COMBO: {stats.highestCombo}x";
            }

            if (m_highestMultiplierText != null)
            {
                m_highestMultiplierText.text = $"BEST MULTIPLIER: {stats.highestMultiplier:F1}x";
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
            m_isVictory = false;
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
            m_isVictory = true;
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
