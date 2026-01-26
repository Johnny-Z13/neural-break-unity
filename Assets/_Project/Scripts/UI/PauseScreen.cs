using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Audio;
using NeuralBreak.Graphics;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Pause menu with Resume, Restart, Quit buttons, and settings controls.
    /// Supports keyboard/mouse and gamepad navigation.
    /// </summary>
    public class PauseScreen : ScreenBase
    {
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _quitButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI _masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI _sfxVolumeLabel;
        [SerializeField] private TextMeshProUGUI _musicVolumeLabel;

        [Header("Toggles")]
        [SerializeField] private Toggle _screenShakeToggle;
        [SerializeField] private Toggle _spawnWarningsToggle;

        // Cached references
        private CameraController _cameraController;
        private Graphics.SpawnWarningIndicator _warningIndicator;

        // PlayerPrefs keys
        private const string PREF_MASTER_VOL = "NeuralBreak_MasterVolume";
        private const string PREF_SFX_VOL = "NeuralBreak_SFXVolume";
        private const string PREF_MUSIC_VOL = "NeuralBreak_MusicVolume";
        private const string PREF_SCREEN_SHAKE = "NeuralBreak_ScreenShake";
        private const string PREF_SPAWN_WARNINGS = "NeuralBreak_SpawnWarnings";

        protected override void Awake()
        {
            // Allow B/Escape to resume game
            _allowCancelToClose = true;

            base.Awake();

            // Wire up button events
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_controlsButton != null)
            {
                _controlsButton.onClick.AddListener(OnControlsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }

            // Wire up slider events
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            // Wire up toggles
            if (_screenShakeToggle != null)
            {
                _screenShakeToggle.onValueChanged.AddListener(OnScreenShakeToggled);
            }
            if (_spawnWarningsToggle != null)
            {
                _spawnWarningsToggle.onValueChanged.AddListener(OnSpawnWarningsToggled);
            }

            LoadSettings();
        }

        protected override void OnShow()
        {
            // Ensure first button is selected for keyboard/gamepad
            if (_firstSelected == null && _resumeButton != null)
            {
                _firstSelected = _resumeButton;
            }

            // Refresh UI from current settings
            LoadSettings();
        }

        /// <summary>
        /// Handle B/Escape - resume game instead of just hiding
        /// </summary>
        protected override void OnCancelPressed()
        {
            OnResumeClicked();
        }

        private void LoadSettings()
        {
            float masterVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
            float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
            float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.4f);
            bool screenShake = PlayerPrefs.GetInt(PREF_SCREEN_SHAKE, 1) == 1;
            bool spawnWarnings = PlayerPrefs.GetInt(PREF_SPAWN_WARNINGS, 1) == 1;

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = masterVol;
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = sfxVol;
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = musicVol;
            }
            if (_screenShakeToggle != null)
            {
                _screenShakeToggle.isOn = screenShake;
            }
            if (_spawnWarningsToggle != null)
            {
                _spawnWarningsToggle.isOn = spawnWarnings;
            }

            UpdateVolumeLabels();
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void UpdateVolumeLabels()
        {
            if (_masterVolumeLabel != null && _masterVolumeSlider != null)
            {
                _masterVolumeLabel.text = $"{Mathf.RoundToInt(_masterVolumeSlider.value * 100)}%";
            }
            if (_sfxVolumeLabel != null && _sfxVolumeSlider != null)
            {
                _sfxVolumeLabel.text = $"{Mathf.RoundToInt(_sfxVolumeSlider.value * 100)}%";
            }
            if (_musicVolumeLabel != null && _musicVolumeSlider != null)
            {
                _musicVolumeLabel.text = $"{Mathf.RoundToInt(_musicVolumeSlider.value * 100)}%";
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOL, value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOL, value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, value);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnScreenShakeToggled(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_SCREEN_SHAKE, enabled ? 1 : 0);

            // Cache reference on first use
            if (_cameraController == null)
            {
                var camGO = GameObject.Find("MainCamera");
                if (camGO != null)
                {
                    _cameraController = camGO.GetComponent<CameraController>();
                }
            }

            if (_cameraController != null)
            {
                _cameraController.SetShakeEnabled(enabled);
            }
            SaveSettings();
        }

        private void OnSpawnWarningsToggled(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_SPAWN_WARNINGS, enabled ? 1 : 0);

            // Cache reference on first use
            if (_warningIndicator == null)
            {
                var warningGO = GameObject.Find("SpawnWarningIndicator");
                if (warningGO != null)
                {
                    _warningIndicator = warningGO.GetComponent<Graphics.SpawnWarningIndicator>();
                }
            }

            if (_warningIndicator != null)
            {
                _warningIndicator.SetWarningsEnabled(enabled);
            }
            SaveSettings();
        }

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                // Resume time first
                Time.timeScale = 1f;

                // Start new game
                GameManager.Instance.StartGame(GameManager.Instance.CurrentMode);
            }
        }

        private void OnControlsClicked()
        {
            if (FindObjectOfType<ControlsOverlay>() != null)
            {
                FindObjectOfType<ControlsOverlay>().Show();
            }
        }

        private void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
            }
        }
    }
}
