using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using NeuralBreak.Input;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages accessibility options for the game.
    /// Includes screen shake, flash effects, color options, etc.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        // Screen shake
        [Header("Screen Effects")]
        public bool ScreenShakeEnabled { get; private set; } = true;
        public float ScreenShakeIntensity { get; private set; } = 1f;

        // Flash effects
        public bool FlashEffectsEnabled { get; private set; } = true;
        public float FlashIntensity { get; private set; } = 1f;

        // Controller rumble
        public bool RumbleEnabled { get; private set; } = true;
        public float RumbleIntensity { get; private set; } = 1f;

        // Visual
        public bool HighContrastMode { get; private set; } = false;
        public bool ColorblindMode { get; private set; } = false;
        public int ColorblindType { get; private set; } = 0; // 0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia
        public float UIScale { get; private set; } = 1f;

        // Audio
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.7f;
        public float SFXVolume { get; private set; } = 1f;

        // Gameplay
        public bool AutoAim { get; private set; } = false;
        public float AutoAimStrength { get; private set; } = 0.3f;
        public bool SlowMotionOption { get; private set; } = false;
        public float GameSpeed { get; private set; } = 1f;

        // Events
        public event Action OnSettingsChanged;

        private const string PREFS_KEY = "AccessibilitySettings";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Load settings from PlayerPrefs
        /// </summary>
        public void LoadSettings()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<AccessibilityData>(json);
                    ApplyData(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AccessibilityManager] Failed to load settings: {e.Message}");
                    ResetToDefaults();
                }
            }

            ApplySettings();
        }

        /// <summary>
        /// Save settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            var data = new AccessibilityData
            {
                screenShakeEnabled = ScreenShakeEnabled,
                screenShakeIntensity = ScreenShakeIntensity,
                flashEffectsEnabled = FlashEffectsEnabled,
                flashIntensity = FlashIntensity,
                rumbleEnabled = RumbleEnabled,
                rumbleIntensity = RumbleIntensity,
                highContrastMode = HighContrastMode,
                colorblindMode = ColorblindMode,
                colorblindType = ColorblindType,
                uiScale = UIScale,
                masterVolume = MasterVolume,
                musicVolume = MusicVolume,
                sfxVolume = SFXVolume,
                autoAim = AutoAim,
                autoAimStrength = AutoAimStrength,
                slowMotionOption = SlowMotionOption,
                gameSpeed = GameSpeed
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log("[AccessibilityManager] Settings saved");
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            ScreenShakeEnabled = true;
            ScreenShakeIntensity = 1f;
            FlashEffectsEnabled = true;
            FlashIntensity = 1f;
            RumbleEnabled = true;
            RumbleIntensity = 1f;
            HighContrastMode = false;
            ColorblindMode = false;
            ColorblindType = 0;
            UIScale = 1f;
            MasterVolume = 1f;
            MusicVolume = 0.7f;
            SFXVolume = 1f;
            AutoAim = false;
            AutoAimStrength = 0.3f;
            SlowMotionOption = false;
            GameSpeed = 1f;

            ApplySettings();
            SaveSettings();
        }

        private void ApplyData(AccessibilityData data)
        {
            ScreenShakeEnabled = data.screenShakeEnabled;
            ScreenShakeIntensity = data.screenShakeIntensity;
            FlashEffectsEnabled = data.flashEffectsEnabled;
            FlashIntensity = data.flashIntensity;
            RumbleEnabled = data.rumbleEnabled;
            RumbleIntensity = data.rumbleIntensity;
            HighContrastMode = data.highContrastMode;
            ColorblindMode = data.colorblindMode;
            ColorblindType = data.colorblindType;
            UIScale = data.uiScale;
            MasterVolume = data.masterVolume;
            MusicVolume = data.musicVolume;
            SFXVolume = data.sfxVolume;
            AutoAim = data.autoAim;
            AutoAimStrength = data.autoAimStrength;
            SlowMotionOption = data.slowMotionOption;
            GameSpeed = data.gameSpeed;
        }

        private void ApplySettings()
        {
            // Apply rumble settings
            var rumble = FindFirstObjectByType<GamepadRumble>();
            if (rumble != null)
            {
                rumble.SetRumbleEnabled(RumbleEnabled);
                rumble.SetIntensity(RumbleIntensity);
            }

            // Apply audio settings
            AudioListener.volume = MasterVolume;

            // Apply game speed
            Time.timeScale = GameSpeed;

            OnSettingsChanged?.Invoke();
        }

        #region Setters

        public void SetScreenShakeEnabled(bool enabled)
        {
            ScreenShakeEnabled = enabled;
            ApplySettings();
        }

        public void SetScreenShakeIntensity(float intensity)
        {
            ScreenShakeIntensity = Mathf.Clamp(intensity, 0f, 2f);
            ApplySettings();
        }

        public void SetFlashEffectsEnabled(bool enabled)
        {
            FlashEffectsEnabled = enabled;
            ApplySettings();
        }

        public void SetFlashIntensity(float intensity)
        {
            FlashIntensity = Mathf.Clamp(intensity, 0f, 2f);
            ApplySettings();
        }

        public void SetRumbleEnabled(bool enabled)
        {
            RumbleEnabled = enabled;
            ApplySettings();
        }

        public void SetRumbleIntensity(float intensity)
        {
            RumbleIntensity = Mathf.Clamp(intensity, 0f, 2f);
            ApplySettings();
        }

        public void SetHighContrastMode(bool enabled)
        {
            HighContrastMode = enabled;
            ApplySettings();
        }

        public void SetColorblindMode(bool enabled, int type = 0)
        {
            ColorblindMode = enabled;
            ColorblindType = Mathf.Clamp(type, 0, 3);
            ApplySettings();
        }

        public void SetUIScale(float scale)
        {
            UIScale = Mathf.Clamp(scale, 0.5f, 2f);
            ApplySettings();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            ApplySettings();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            ApplySettings();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            ApplySettings();
        }

        public void SetAutoAim(bool enabled, float strength = 0.3f)
        {
            AutoAim = enabled;
            AutoAimStrength = Mathf.Clamp(strength, 0f, 1f);
            ApplySettings();
        }

        public void SetGameSpeed(float speed)
        {
            GameSpeed = Mathf.Clamp(speed, 0.25f, 2f);
            ApplySettings();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get screen shake multiplier (0 if disabled)
        /// </summary>
        public float GetScreenShakeMultiplier()
        {
            return ScreenShakeEnabled ? ScreenShakeIntensity : 0f;
        }

        /// <summary>
        /// Get flash intensity multiplier (0 if disabled)
        /// </summary>
        public float GetFlashMultiplier()
        {
            return FlashEffectsEnabled ? FlashIntensity : 0f;
        }

        /// <summary>
        /// Transform a color for colorblind mode
        /// </summary>
        public Color TransformColor(Color color)
        {
            if (!ColorblindMode) return color;

            // Simple colorblind simulation
            switch (ColorblindType)
            {
                case 1: // Deuteranopia (green-blind)
                    return new Color(
                        color.r * 0.625f + color.g * 0.375f,
                        color.r * 0.7f + color.g * 0.3f,
                        color.b,
                        color.a
                    );

                case 2: // Protanopia (red-blind)
                    return new Color(
                        color.r * 0.567f + color.g * 0.433f,
                        color.r * 0.558f + color.g * 0.442f,
                        color.b,
                        color.a
                    );

                case 3: // Tritanopia (blue-blind)
                    return new Color(
                        color.r,
                        color.g * 0.95f + color.b * 0.05f,
                        color.g * 0.433f + color.b * 0.567f,
                        color.a
                    );

                default:
                    return color;
            }
        }

        #endregion

        [Serializable]
        private class AccessibilityData
        {
            public bool screenShakeEnabled = true;
            public float screenShakeIntensity = 1f;
            public bool flashEffectsEnabled = true;
            public float flashIntensity = 1f;
            public bool rumbleEnabled = true;
            public float rumbleIntensity = 1f;
            public bool highContrastMode = false;
            public bool colorblindMode = false;
            public int colorblindType = 0;
            public float uiScale = 1f;
            public float masterVolume = 1f;
            public float musicVolume = 0.7f;
            public float sfxVolume = 1f;
            public bool autoAim = false;
            public float autoAimStrength = 0.3f;
            public bool slowMotionOption = false;
            public float gameSpeed = 1f;
        }
    }
}
