using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;

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

        private bool m_buttonsWired;

        protected override void Awake()
        {
            base.Awake();
            WireButtons();
        }

        private void Start()
        {
            // Wire buttons again in Start() in case they were set via reflection after Awake()
            WireButtons();
        }

        /// <summary>
        /// Wire button click listeners. Called by UIBuilder after setting fields via reflection.
        /// </summary>
        public void InitializeButtons()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (m_buttonsWired) return;

            if (m_playButton != null)
            {
                m_playButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
                m_buttonsWired = true;
            }

            if (m_arcadeButton != null)
            {
                m_arcadeButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
                m_buttonsWired = true;
            }

            if (m_rogueButton != null)
            {
                m_rogueButton.onClick.AddListener(() => StartGame(GameMode.Rogue));
                m_buttonsWired = true;
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

            // Hide this screen FIRST (before state change event)
            Hide();

            // Use GameStateManager (guaranteed to exist from Boot scene)
            // GameManager.StartGame delegates to GameStateManager anyway
            if (GameManager.Instance != null)
            {
                // Use scene-specific GameManager if available (handles stats reset)
                GameManager.Instance.StartGame(mode);
            }
            else
            {
                // Fall back to global GameStateManager
                GameStateManager.Instance.StartGame(mode);
            }

            Debug.Log($"[StartScreen] StartGame() called successfully");
        }
    }
}
