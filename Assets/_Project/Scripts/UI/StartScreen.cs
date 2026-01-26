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
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private string _gameTitle = "NEURAL BREAK";

        [Header("Subtitle")]
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private string _subtitle = "SURVIVE THE DIGITAL SWARM";

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _arcadeButton;
        [SerializeField] private Button _rogueButton;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI _versionText;

        protected override void Awake()
        {
            base.Awake();

            // Wire up button events
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
            }

            if (_arcadeButton != null)
            {
                _arcadeButton.onClick.AddListener(() => StartGame(GameMode.Arcade));
            }

            if (_rogueButton != null)
            {
                _rogueButton.onClick.AddListener(() => StartGame(GameMode.Rogue));
            }
        }

        protected override void OnShow()
        {
            // Set title text
            if (_titleText != null)
            {
                _titleText.text = _gameTitle;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = _subtitle;
            }

            // Set version text
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }

            // Select first button
            if (_firstSelected == null)
            {
                if (_playButton != null)
                    _firstSelected = _playButton;
                else if (_arcadeButton != null)
                    _firstSelected = _arcadeButton;
            }
        }

        private void StartGame(GameMode mode)
        {
            LogHelper.Log($"[StartScreen] ========================================");
            LogHelper.Log($"[StartScreen] PLAY BUTTON CLICKED!");
            LogHelper.Log($"[StartScreen] Launching ARCADE MODE (GameMode.{mode})");
            LogHelper.Log($"[StartScreen] ========================================");

            if (GameManager.Instance == null)
            {
                LogHelper.LogError("[StartScreen] ERROR: GameManager.Instance is NULL! Cannot start game!");
                return;
            }

            LogHelper.Log($"[StartScreen] GameManager found, calling StartGame({mode})");

            // Start the game
            GameManager.Instance.StartGame(mode);

            LogHelper.Log($"[StartScreen] StartGame() called successfully");

            // Hide this screen
            Hide();
        }
    }
}
