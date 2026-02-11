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

        // Cached references
        private float m_originalTimeScale = 1f;
        private bool m_isTimeScaled;

        private void Awake()
        {
        }

        private void Start()
        {
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
            if (m_isTimeScaled)
            {
                Time.timeScale = m_originalTimeScale;
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
        /// Trigger hitstop (freeze frame) effect
        /// </summary>
        public void TriggerHitstop(float duration)
        {
            if (!m_enableHitstop || duration <= 0) return;

            StartCoroutine(HitstopCoroutine(duration));
        }

        /// <summary>
        /// Trigger slow motion effect
        /// </summary>
        public void TriggerSlowMotion()
        {
            if (!m_enableSlowMotion) return;

            StartCoroutine(SlowMotionCoroutine());
        }

        /// <summary>
        /// Trigger screen flash
        /// </summary>
        public void TriggerScreenFlash(Color color)
        {
            if (!m_enableScreenFlash) return;

            // Use ScreenFlash component
            if (FindFirstObjectByType<ScreenFlash>() != null)
            {
                FindFirstObjectByType<ScreenFlash>().Flash(color, m_flashDuration);
            }
        }

        #endregion

        #region Coroutines

        private System.Collections.IEnumerator HitstopCoroutine(float duration)
        {
            if (m_isTimeScaled) yield break;

            m_isTimeScaled = true;
            m_originalTimeScale = Time.timeScale;

            Time.timeScale = 0f;

            // Use unscaled time to wait
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = m_originalTimeScale;
            m_isTimeScaled = false;
        }

        private System.Collections.IEnumerator SlowMotionCoroutine()
        {
            if (m_isTimeScaled) yield break;

            m_isTimeScaled = true;
            m_originalTimeScale = Time.timeScale;

            Time.timeScale = m_slowMotionScale;

            // Lerp back to normal
            float elapsed = 0f;
            while (elapsed < m_slowMotionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_slowMotionDuration;
                Time.timeScale = Mathf.Lerp(m_slowMotionScale, m_originalTimeScale, t);
                yield return null;
            }

            Time.timeScale = m_originalTimeScale;
            m_isTimeScaled = false;
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
