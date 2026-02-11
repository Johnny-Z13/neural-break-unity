using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using NeuralBreak.Core;
using Z13.Core;

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

        [Header("Press Any Key Prompt")]
        [SerializeField] private TextMeshProUGUI m_pressAnyKeyText;
        [SerializeField] private string m_pressAnyKeyPrompt = "PRESS ANY KEY TO CONTINUE";
        [SerializeField] private float m_blinkSpeed = 2f;

        // Cached state
        private bool m_isVictory;
        private bool m_waitingForInput = false;
        private float m_blinkTimer;

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
            // Start in "press any key" mode
            m_waitingForInput = true;
            m_blinkTimer = 0f;

            // Hide buttons initially
            if (m_restartButton != null) m_restartButton.gameObject.SetActive(false);
            if (m_mainMenuButton != null) m_mainMenuButton.gameObject.SetActive(false);

            // Show press any key prompt
            if (m_pressAnyKeyText != null)
            {
                m_pressAnyKeyText.gameObject.SetActive(true);
                m_pressAnyKeyText.text = m_pressAnyKeyPrompt;
            }

            // Update title based on victory state
            if (m_titleText != null)
            {
                m_titleText.text = m_isVictory ? m_victoryTitle : m_gameOverTitle;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!m_isVisible || !m_waitingForInput) return;

            // Blink "press any key" text
            if (m_pressAnyKeyText != null)
            {
                m_blinkTimer += Time.deltaTime * m_blinkSpeed;
                float alpha = Mathf.PingPong(m_blinkTimer, 1f);
                var color = m_pressAnyKeyText.color;
                color.a = Mathf.Lerp(0.3f, 1f, alpha);
                m_pressAnyKeyText.color = color;
            }

            // Wait for ANY input
            if (AnyKeyPressed())
            {
                OnAnyKeyPressed();
            }
        }

        private bool AnyKeyPressed()
        {
            // Check keyboard
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            // Check gamepad buttons
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                    Gamepad.current.buttonEast.wasPressedThisFrame ||
                    Gamepad.current.buttonWest.wasPressedThisFrame ||
                    Gamepad.current.buttonNorth.wasPressedThisFrame ||
                    Gamepad.current.startButton.wasPressedThisFrame)
                {
                    return true;
                }
            }

            // Check mouse
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnAnyKeyPressed()
        {
            m_waitingForInput = false;

            // Hide press any key prompt
            if (m_pressAnyKeyText != null)
            {
                m_pressAnyKeyText.gameObject.SetActive(false);
            }

            // Show buttons
            if (m_restartButton != null) m_restartButton.gameObject.SetActive(true);
            if (m_mainMenuButton != null) m_mainMenuButton.gameObject.SetActive(true);

            // Select first button for keyboard/gamepad navigation
            if (m_restartButton != null)
            {
                m_firstSelected = m_restartButton;
                SelectFirstElement();
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
            // Use GameManager if available (handles stats reset), otherwise GameStateManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(GameStateManager.Instance.CurrentMode);
            }
            else
            {
                GameStateManager.Instance.StartGame(GameStateManager.Instance.CurrentMode);
            }
        }

        private void OnMainMenuClicked()
        {
            // Use GameStateManager (guaranteed to exist from Boot scene)
            GameStateManager.Instance.ReturnToMenu();
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
