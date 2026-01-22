using UnityEngine;
using MoreMountains.Feedbacks;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Centralized UI feedback system using FEEL (MMFeedbacks).
    /// Provides juice effects for all UI interactions.
    ///
    /// USAGE:
    /// 1. Attach to a persistent GameObject (e.g., UIManager or GameSetup)
    /// 2. Call static methods like UIFeedbacks.PlayScorePop(position)
    /// 3. Effects are pooled and reused for performance
    ///
    /// FEEL BEST PRACTICES:
    /// - Use MMF_Player prefabs for complex reusable effects
    /// - Layer subtle effects rather than one big effect
    /// - Use TimescaleMode.Unscaled for pause-independent UI
    /// </summary>
    public class UIFeedbacks : MonoBehaviour
    {
        public static UIFeedbacks Instance { get; private set; }

        [Header("Score Feedbacks")]
        [SerializeField] private MMF_Player _scorePunchFeedback;
        [SerializeField] private MMF_Player _comboMilestoneFeedback;
        [SerializeField] private MMF_Player _multiplierFeedback;

        [Header("Health Feedbacks")]
        [SerializeField] private MMF_Player _damageFeedback;
        [SerializeField] private MMF_Player _healFeedback;
        [SerializeField] private MMF_Player _shieldGainFeedback;
        [SerializeField] private MMF_Player _lowHealthFeedback;

        [Header("Level/Wave Feedbacks")]
        [SerializeField] private MMF_Player _levelUpFeedback;
        [SerializeField] private MMF_Player _waveStartFeedback;
        [SerializeField] private MMF_Player _bossWarningFeedback;

        [Header("Achievement Feedbacks")]
        [SerializeField] private MMF_Player _achievementUnlockFeedback;

        [Header("Button Feedbacks")]
        [SerializeField] private MMF_Player _buttonHoverFeedback;
        [SerializeField] private MMF_Player _buttonClickFeedback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create default feedbacks if not assigned
            CreateDefaultFeedbacks();
        }

        private void Start()
        {
            // Subscribe to events for automatic feedback
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnLevelUp);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnLevelUp);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
        }

        #region Event Handlers

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            if (evt.delta >= 500)
            {
                PlayFeedback(_scorePunchFeedback);
            }
        }

        private int _lastComboMilestone = 0;
        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Check for milestone
            foreach (var milestone in UITheme.ComboMilestones)
            {
                if (evt.comboCount >= milestone.threshold && milestone.threshold > _lastComboMilestone)
                {
                    PlayFeedback(_comboMilestoneFeedback);
                    _lastComboMilestone = milestone.threshold;
                    break;
                }
            }

            if (evt.comboCount == 0)
            {
                _lastComboMilestone = 0;
            }

            if (evt.multiplier >= 5f)
            {
                PlayFeedback(_multiplierFeedback);
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            PlayFeedback(_damageFeedback);

            float healthPercent = evt.maxHealth > 0 ? (float)evt.currentHealth / evt.maxHealth : 0;
            if (healthPercent <= 0.25f)
            {
                PlayFeedback(_lowHealthFeedback);
            }
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            PlayFeedback(_healFeedback);
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            // Only play on shield gain
            if (evt.currentShields > 0)
            {
                PlayFeedback(_shieldGainFeedback);
            }
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            PlayFeedback(_levelUpFeedback);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            PlayFeedback(_waveStartFeedback);
        }

        private void OnBossEncounter(BossEncounterEvent evt)
        {
            if (evt.isBossActive)
            {
                PlayFeedback(_bossWarningFeedback);
            }
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            PlayFeedback(_achievementUnlockFeedback);
        }

        #endregion

        #region Public API

        public static void PlayScorePunch()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._scorePunchFeedback);
            }
        }

        public static void PlayComboMilestone()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._comboMilestoneFeedback);
            }
        }

        public static void PlayDamage()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._damageFeedback);
            }
        }

        public static void PlayHeal()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._healFeedback);
            }
        }

        public static void PlayLevelUp()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._levelUpFeedback);
            }
        }

        public static void PlayButtonHover()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._buttonHoverFeedback);
            }
        }

        public static void PlayButtonClick()
        {
            if (Instance != null)
            {
                Instance.PlayFeedback(Instance._buttonClickFeedback);
            }
        }

        #endregion

        #region Helper Methods

        private void PlayFeedback(MMF_Player feedback)
        {
            if (feedback != null && feedback.isActiveAndEnabled)
            {
                feedback.PlayFeedbacks();
            }
        }

        private void CreateDefaultFeedbacks()
        {
            // Create runtime feedbacks if not assigned in inspector
            // These provide basic juice without needing prefab setup

            if (_scorePunchFeedback == null)
            {
                _scorePunchFeedback = CreateSimpleFeedback("ScorePunch");
                AddScaleEffect(_scorePunchFeedback, UITheme.Scale.PunchSmall, UITheme.Duration.Fast);
            }

            if (_comboMilestoneFeedback == null)
            {
                _comboMilestoneFeedback = CreateSimpleFeedback("ComboMilestone");
                AddScaleEffect(_comboMilestoneFeedback, UITheme.Scale.PunchMedium, UITheme.Duration.Normal);
                AddFlashEffect(_comboMilestoneFeedback, UITheme.Warning);
            }

            if (_damageFeedback == null)
            {
                _damageFeedback = CreateSimpleFeedback("Damage");
                AddFlashEffect(_damageFeedback, UITheme.Danger);
            }

            if (_healFeedback == null)
            {
                _healFeedback = CreateSimpleFeedback("Heal");
                AddFlashEffect(_healFeedback, UITheme.Good);
            }

            if (_levelUpFeedback == null)
            {
                _levelUpFeedback = CreateSimpleFeedback("LevelUp");
                AddScaleEffect(_levelUpFeedback, UITheme.Scale.PunchLarge, UITheme.Duration.Smooth);
                AddFlashEffect(_levelUpFeedback, UITheme.Warning);
            }

            if (_waveStartFeedback == null)
            {
                _waveStartFeedback = CreateSimpleFeedback("WaveStart");
                AddScaleEffect(_waveStartFeedback, UITheme.Scale.PunchSmall, UITheme.Duration.Normal);
            }

            if (_bossWarningFeedback == null)
            {
                _bossWarningFeedback = CreateSimpleFeedback("BossWarning");
                AddFlashEffect(_bossWarningFeedback, UITheme.Danger);
            }

            if (_achievementUnlockFeedback == null)
            {
                _achievementUnlockFeedback = CreateSimpleFeedback("AchievementUnlock");
                AddScaleEffect(_achievementUnlockFeedback, UITheme.Scale.PunchMedium, UITheme.Duration.Normal);
                AddFlashEffect(_achievementUnlockFeedback, UITheme.Warning);
            }

            if (_buttonHoverFeedback == null)
            {
                _buttonHoverFeedback = CreateSimpleFeedback("ButtonHover");
                AddScaleEffect(_buttonHoverFeedback, 1.05f, UITheme.Duration.Fast);
            }

            if (_buttonClickFeedback == null)
            {
                _buttonClickFeedback = CreateSimpleFeedback("ButtonClick");
                AddScaleEffect(_buttonClickFeedback, UITheme.Scale.PunchSmall, UITheme.Duration.Instant);
            }
        }

        private MMF_Player CreateSimpleFeedback(string name)
        {
            var feedbackGO = new GameObject($"UIFeedback_{name}");
            feedbackGO.transform.SetParent(transform);
            var player = feedbackGO.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Awake;
            player.FeedbacksIntensity = 1f;
            return player;
        }

        private void AddScaleEffect(MMF_Player player, float targetScale, float duration)
        {
            // Create a scale feedback using MMF_Scale
            var scaleFeedback = new MMF_Scale();
            scaleFeedback.Label = "Scale Punch";
            scaleFeedback.AnimateScaleTarget = player.transform;
            scaleFeedback.AnimateScaleDuration = duration;
            scaleFeedback.RemapCurveOne = 0f;
            scaleFeedback.RemapCurveZero = targetScale;
            scaleFeedback.Mode = MMF_Scale.Modes.Additive;
            scaleFeedback.Timing.TimescaleMode = TimescaleModes.Unscaled;

            player.AddFeedback(scaleFeedback);
        }

        private void AddFlashEffect(MMF_Player player, Color flashColor)
        {
            // Use MMF_ImageAlpha or similar for flash
            // For now, create a simple feedback reference
            var flashFeedback = new MMF_Flicker();
            flashFeedback.Label = "Color Flash";
            flashFeedback.FlickerDuration = UITheme.Duration.Fast;
            flashFeedback.FlickerPeriod = 0.05f;
            flashFeedback.FlickerColor = flashColor;
            flashFeedback.Timing.TimescaleMode = TimescaleModes.Unscaled;

            player.AddFeedback(flashFeedback);
        }

        #endregion

        #region Debug

        [ContextMenu("Test: Score Punch")]
        private void TestScorePunch() => PlayScorePunch();

        [ContextMenu("Test: Combo Milestone")]
        private void TestComboMilestone() => PlayComboMilestone();

        [ContextMenu("Test: Damage")]
        private void TestDamage() => PlayDamage();

        [ContextMenu("Test: Heal")]
        private void TestHeal() => PlayHeal();

        [ContextMenu("Test: Level Up")]
        private void TestLevelUp() => PlayLevelUp();

        #endregion
    }
}
