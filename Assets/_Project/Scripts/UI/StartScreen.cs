using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Utils;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Start/Title screen with game title and Play button.
    /// </summary>
    public class StartScreen : ScreenBase
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private string m_gameTitle = "NEURAL BREAK";

        [Header("Subtitle")]
        [SerializeField] private TextMeshProUGUI m_subtitleText;
        [SerializeField] private string m_subtitle = "SURVIVE THE DIGITAL SWARM";

        [Header("Buttons")]
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_arcadeButton;
        [SerializeField] private Button m_rogueButton;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI m_versionText;

        protected override void Awake()
        {
            base.Awake();

            // Wire up button events
            if (m_playButton != null)
            {
                m_playButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
            }

            if (m_arcadeButton != null)
            {
                m_arcadeButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
            }

            if (m_rogueButton != null)
            {
                m_rogueButton.onClick.AddListener(() => StartGame(GameMode.Rogue));
            }
        }

        protected override void OnShow()
        {
            // Set title text
            if (m_titleText != null)
            {
                m_titleText.text = m_gameTitle;
            }

            if (m_subtitleText != null)
            {
                m_subtitleText.text = m_subtitle;
            }

            // Set version text
            if (m_versionText != null)
            {
                m_versionText.text = $"v{Application.version}";
            }

            // Select first button
            if (m_firstSelected == null)
            {
                if (m_playButton != null)
                    m_firstSelected = m_playButton;
                else if (m_arcadeButton != null)
                    m_firstSelected = m_arcadeButton;
            }
        }

        private void StartGame(GameMode mode)
        {
            Debug.Log($"[StartScreen] ========================================");
            Debug.Log($"[StartScreen] PLAY BUTTON CLICKED!");
            Debug.Log($"[StartScreen] Launching {mode} MODE");
            Debug.Log($"[StartScreen] ========================================");

            if (GameManager.Instance == null)
            {
                Debug.LogError("[StartScreen] ERROR: GameManager.Instance is NULL! Cannot start game!");
                return;
            }

            Debug.Log($"[StartScreen] GameManager found, calling StartGame({mode})");

            // Hide this screen FIRST (before state change event)
            Debug.Log($"[StartScreen] Calling Hide() - m_screenRoot: {(m_screenRoot != null ? m_screenRoot.name : "NULL")}");
            Hide();
            Debug.Log($"[StartScreen] Hide() completed - m_screenRoot active: {(m_screenRoot != null ? m_screenRoot.activeSelf.ToString() : "NULL")}");

            // Start the game (this will also trigger UIManager to hide screens via event)
            GameManager.Instance.StartGame(mode);

            Debug.Log($"[StartScreen] StartGame() called successfully");
        }
    }
}
