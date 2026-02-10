using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Centralized feedback manager for game juice.
    /// Creates and triggers Feel feedbacks for various game events.
    /// Provides that satisfying arcade feel!
    ///
    /// ZERO-ALLOCATION: All time-scale effects use timer-based Update instead of coroutines.
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {

        [Header("Camera Shake (Fallback - reads from config)")]
        [SerializeField] private float m_bossShakeIntensity = 5f;
        [SerializeField] private float m_bossShakeDuration = 0.6f;

        // Config-driven shake settings (read from GameBalanceConfig.feedback)
        private float SmallShakeIntensity => ConfigProvider.Balance?.feedback?.smallShake?.intensity ?? 0.2f;
        private float SmallShakeDuration => ConfigProvider.Balance?.feedback?.smallShake?.duration ?? 0.1f;
        private float MediumShakeIntensity => ConfigProvider.Balance?.feedback?.mediumShake?.intensity ?? 0.5f;
        private float MediumShakeDuration => ConfigProvider.Balance?.feedback?.mediumShake?.duration ?? 0.3f;
        private float LargeShakeIntensity => ConfigProvider.Balance?.feedback?.largeShake?.intensity ?? 1.0f;
        private float LargeShakeDuration => ConfigProvider.Balance?.feedback?.largeShake?.duration ?? 0.5f;

        [Header("Hitstop (Freeze Frame)")]
        [SerializeField] private bool m_enableHitstop = true;
        [SerializeField] private float m_smallHitstopDuration = 0.02f;
        [SerializeField] private float m_mediumHitstopDuration = 0.05f;
        [SerializeField] private float m_largeHitstopDuration = 0.1f;
        [SerializeField] private float m_bossHitstopDuration = 0.2f;

        [Header("Screen Flash")]
        [SerializeField] private bool m_enableScreenFlash = true;

        // Cached reference (avoid FindFirstObjectByType in hot paths)
        private ScreenFlash m_screenFlash;
        [SerializeField] private Color m_damageFlashColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color m_healFlashColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color m_powerUpFlashColor = new Color(1f, 0.8f, 0f, 0.2f);
        [SerializeField] private float m_flashDuration = 0.1f;

        [Header("Time Scale")]
        [SerializeField] private bool m_enableSlowMotion = true;
        [SerializeField] private float m_slowMotionScale = 0.3f;
        [SerializeField] private float m_slowMotionDuration = 0.15f;

        [Header("Combo Feedback")]
        [SerializeField] private float m_comboShakeMultiplier = 0.1f; // Shake increases with combo
        [SerializeField] private int m_comboMilestone = 10; // Every X kills = bigger feedback

        [Header("References")]
        [SerializeField] private CameraController m_cameraController;

        // Timer-based time-scale state (replaces coroutines - zero allocation)
        private enum TimeScaleState { None, Hitstop, SlowMotion }
        private TimeScaleState m_timeScaleState = TimeScaleState.None;
        private float m_originalTimeScale = 1f;
        private float m_hitstopEndTime;
        private float m_slowMotionElapsed;

        private void Awake()
        {
        }

        private void Start()
        {
            // Cache ScreenFlash reference (avoid FindFirstObjectByType in hot paths)
            m_screenFlash = FindFirstObjectByType<ScreenFlash>();

            // Cache camera controller via GameObject.Find
            if (m_cameraController == null)
            {
                var camGO = GameObject.Find("MainCamera");
                if (camGO != null)
                {
                    m_cameraController = camGO.GetComponent<CameraController>();
                }

                if (m_cameraController == null)
                {
                    Debug.LogWarning("[FeedbackManager] CameraController not found! Camera shake effects will not work.");
                }
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // Restore time scale
            if (m_timeScaleState != TimeScaleState.None)
            {
                Time.timeScale = m_originalTimeScale;
            }
        }

        private void Update()
        {
            switch (m_timeScaleState)
            {
                case TimeScaleState.Hitstop:
                    // Use unscaled time since timeScale is 0
                    if (Time.unscaledTime >= m_hitstopEndTime)
                    {
                        Time.timeScale = m_originalTimeScale;
                        m_timeScaleState = TimeScaleState.None;
                    }
                    break;

                case TimeScaleState.SlowMotion:
                    m_slowMotionElapsed += Time.unscaledDeltaTime;
                    if (m_slowMotionElapsed >= m_slowMotionDuration)
                    {
                        Time.timeScale = m_originalTimeScale;
                        m_timeScaleState = TimeScaleState.None;
                    }
                    else
                    {
                        float t = m_slowMotionElapsed / m_slowMotionDuration;
                        Time.timeScale = Mathf.Lerp(m_slowMotionScale, m_originalTimeScale, t);
                    }
                    break;
            }
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<PlayerDashedEvent>(OnPlayerDashed);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        #endregion

        #region Event Handlers

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Shake based on enemy type (config-driven)
            switch (evt.enemyType)
            {
                case EnemyType.DataMite:
                case EnemyType.Fizzer:
                    // Tiny shake for small enemies
                    TriggerCameraShake(SmallShakeIntensity * 0.5f, SmallShakeDuration * 0.5f);
                    break;

                case EnemyType.ScanDrone:
                case EnemyType.UFO:
                    TriggerCameraShake(SmallShakeIntensity, SmallShakeDuration);
                    TriggerHitstop(m_smallHitstopDuration);
                    break;

                case EnemyType.ChaosWorm:
                case EnemyType.CrystalShard:
                    TriggerCameraShake(MediumShakeIntensity, MediumShakeDuration);
                    TriggerHitstop(m_mediumHitstopDuration);
                    break;

                case EnemyType.VoidSphere:
                    TriggerCameraShake(LargeShakeIntensity, LargeShakeDuration);
                    TriggerHitstop(m_largeHitstopDuration);
                    TriggerSlowMotion();
                    break;

                case EnemyType.Boss:
                    TriggerCameraShake(m_bossShakeIntensity, m_bossShakeDuration);
                    TriggerHitstop(m_bossHitstopDuration);
                    TriggerSlowMotion();
                    break;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            TriggerCameraShake(MediumShakeIntensity, MediumShakeDuration);
            TriggerScreenFlash(m_damageFlashColor);
            TriggerHitstop(m_mediumHitstopDuration);
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            TriggerScreenFlash(m_healFlashColor);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            switch (evt.pickupType)
            {
                case PickupType.PowerUp:
                    TriggerScreenFlash(m_powerUpFlashColor);
                    TriggerCameraShake(SmallShakeIntensity, SmallShakeDuration);
                    break;

                case PickupType.Invulnerable:
                    TriggerScreenFlash(new Color(1f, 1f, 0f, 0.3f));
                    TriggerCameraShake(MediumShakeIntensity, MediumShakeDuration);
                    break;

                default:
                    // Small feedback for other pickups
                    break;
            }
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Bigger feedback at combo milestones (config-driven)
            if (evt.comboCount > 0 && evt.comboCount % m_comboMilestone == 0)
            {
                float intensity = Mathf.Min(MediumShakeIntensity + (evt.comboCount * m_comboShakeMultiplier), LargeShakeIntensity);
                TriggerCameraShake(intensity, MediumShakeDuration);

                // Flash based on multiplier
                Color flashColor = Color.Lerp(Color.white, Color.yellow, evt.multiplier / 10f);
                flashColor.a = 0.2f;
                TriggerScreenFlash(flashColor);
            }
        }

        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            // Small zoom/shake on dash (config-driven)
            TriggerCameraShake(SmallShakeIntensity * 0.3f, SmallShakeDuration);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            TriggerCameraShake(MediumShakeIntensity, MediumShakeDuration);
            TriggerScreenFlash(new Color(1f, 1f, 1f, 0.3f));
        }

        private void OnGameOver(GameOverEvent evt)
        {
            TriggerCameraShake(LargeShakeIntensity, LargeShakeDuration);
            TriggerSlowMotion();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Trigger camera shake manually
        /// </summary>
        public void TriggerCameraShake(float intensity, float duration)
        {
            if (m_cameraController != null)
            {
                m_cameraController.Shake(intensity, duration);
            }
        }

        /// <summary>
        /// Trigger hitstop (freeze frame) effect.
        /// Timer-based (zero allocation - no coroutines).
        /// </summary>
        public void TriggerHitstop(float duration)
        {
            if (!m_enableHitstop || duration <= 0) return;
            if (m_timeScaleState != TimeScaleState.None) return;

            m_timeScaleState = TimeScaleState.Hitstop;
            m_originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            m_hitstopEndTime = Time.unscaledTime + duration;
        }

        /// <summary>
        /// Trigger slow motion effect.
        /// Timer-based (zero allocation - no coroutines).
        /// </summary>
        public void TriggerSlowMotion()
        {
            if (!m_enableSlowMotion) return;
            if (m_timeScaleState != TimeScaleState.None) return;

            m_timeScaleState = TimeScaleState.SlowMotion;
            m_originalTimeScale = Time.timeScale;
            Time.timeScale = m_slowMotionScale;
            m_slowMotionElapsed = 0f;
        }

        /// <summary>
        /// Trigger screen flash
        /// </summary>
        public void TriggerScreenFlash(Color color)
        {
            if (!m_enableScreenFlash) return;

            if (m_screenFlash != null)
            {
                m_screenFlash.Flash(color, m_flashDuration);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Small Shake")]
        private void DebugSmallShake() => TriggerCameraShake(SmallShakeIntensity, SmallShakeDuration);

        [ContextMenu("Debug: Large Shake")]
        private void DebugLargeShake() => TriggerCameraShake(LargeShakeIntensity, LargeShakeDuration);

        [ContextMenu("Debug: Hitstop")]
        private void DebugHitstop() => TriggerHitstop(m_mediumHitstopDuration);

        [ContextMenu("Debug: Slow Motion")]
        private void DebugSlowMotion() => TriggerSlowMotion();

        #endregion
    }
}
