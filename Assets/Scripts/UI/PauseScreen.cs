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
        [SerializeField] private Button m_resumeButton;
        [SerializeField] private Button m_restartButton;
        [SerializeField] private Button m_controlsButton;
        [SerializeField] private Button m_quitButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider m_masterVolumeSlider;
        [SerializeField] private Slider m_sfxVolumeSlider;
        [SerializeField] private Slider m_musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI m_masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI m_sfxVolumeLabel;
        [SerializeField] private TextMeshProUGUI m_musicVolumeLabel;

        [Header("Toggles")]
        [SerializeField] private Toggle m_screenShakeToggle;
        [SerializeField] private Toggle m_spawnWarningsToggle;

        // Cached references
        private CameraController m_cameraController;
        private Graphics.SpawnWarningIndicator m_warningIndicator;

        // PlayerPrefs keys
        private const string PREF_MASTER_VOL = "NeuralBreak_MasterVolume";
        private const string PREF_SFX_VOL = "NeuralBreak_SFXVolume";
        private const string PREF_MUSIC_VOL = "NeuralBreak_MusicVolume";
        private const string PREF_SCREEN_SHAKE = "NeuralBreak_ScreenShake";
        private const string PREF_SPAWN_WARNINGS = "NeuralBreak_SpawnWarnings";

        protected override void Awake()
        {
            // Allow B/Escape to resume game
            m_allowCancelToClose = true;

            base.Awake();

            // Wire up button events
            if (m_resumeButton != null)
            {
                m_resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (m_restartButton != null)
            {
                m_restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (m_controlsButton != null)
            {
                m_controlsButton.onClick.AddListener(OnControlsClicked);
            }

            if (m_quitButton != null)
            {
                m_quitButton.onClick.AddListener(OnQuitClicked);
            }

            // Wire up slider events
            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            // Wire up toggles
            if (m_screenShakeToggle != null)
            {
                m_screenShakeToggle.onValueChanged.AddListener(OnScreenShakeToggled);
            }
            if (m_spawnWarningsToggle != null)
            {
                m_spawnWarningsToggle.onValueChanged.AddListener(OnSpawnWarningsToggled);
            }

            LoadSettings();
        }

        protected override void OnShow()
        {
            // Ensure first button is selected for keyboard/gamepad
            if (m_firstSelected == null && m_resumeButton != null)
            {
                m_firstSelected = m_resumeButton;
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

            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.value = masterVol;
            }
            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.value = sfxVol;
            }
            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.value = musicVol;
            }
            if (m_screenShakeToggle != null)
            {
                m_screenShakeToggle.isOn = screenShake;
            }
            if (m_spawnWarningsToggle != null)
            {
                m_spawnWarningsToggle.isOn = spawnWarnings;
            }

            UpdateVolumeLabels();
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void UpdateVolumeLabels()
        {
            if (m_masterVolumeLabel != null && m_masterVolumeSlider != null)
            {
                m_masterVolumeLabel.text = $"{Mathf.RoundToInt(m_masterVolumeSlider.value * 100)}%";
            }
            if (m_sfxVolumeLabel != null && m_sfxVolumeSlider != null)
            {
                m_sfxVolumeLabel.text = $"{Mathf.RoundToInt(m_sfxVolumeSlider.value * 100)}%";
            }
            if (m_musicVolumeLabel != null && m_musicVolumeSlider != null)
            {
                m_musicVolumeLabel.text = $"{Mathf.RoundToInt(m_musicVolumeSlider.value * 100)}%";
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
            if (m_cameraController == null)
            {
                var camGO = GameObject.Find("MainCamera");
                if (camGO != null)
                {
                    m_cameraController = camGO.GetComponent<CameraController>();
                }
            }

            if (m_cameraController != null)
            {
                m_cameraController.SetShakeEnabled(enabled);
            }
            SaveSettings();
        }

        private void OnSpawnWarningsToggled(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_SPAWN_WARNINGS, enabled ? 1 : 0);

            // Cache reference on first use
            if (m_warningIndicator == null)
            {
                var warningGO = GameObject.Find("SpawnWarningIndicator");
                if (warningGO != null)
                {
                    m_warningIndicator = warningGO.GetComponent<Graphics.SpawnWarningIndicator>();
                }
            }

            if (m_warningIndicator != null)
            {
                m_warningIndicator.SetWarningsEnabled(enabled);
            }
            SaveSettings();
        }

        private void OnResumeClicked()
        {
            // Use GameStateManager (guaranteed to exist from Boot scene)
            GameStateManager.Instance.ResumeGame();
        }

        private void OnRestartClicked()
        {
            // Resume time first
            Time.timeScale = 1f;

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

        private void OnControlsClicked()
        {
            var controlsOverlay = FindFirstObjectByType<ControlsOverlay>();
            if (controlsOverlay != null)
            {
                controlsOverlay.Show();
            }
        }

        private void OnQuitClicked()
        {
            // Use GameStateManager (guaranteed to exist from Boot scene)
            GameStateManager.Instance.ReturnToMenu();
        }
    }
}
