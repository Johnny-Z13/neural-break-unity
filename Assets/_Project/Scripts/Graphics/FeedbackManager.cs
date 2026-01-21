using UnityEngine;
using NeuralBreak.Core;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Centralized feedback manager for game juice.
    /// Creates and triggers Feel feedbacks for various game events.
    /// Provides that satisfying arcade feel!
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        public static FeedbackManager Instance { get; private set; }

        [Header("Camera Shake")]
        [SerializeField] private float _smallShakeIntensity = 0.5f;
        [SerializeField] private float _smallShakeDuration = 0.1f;
        [SerializeField] private float _mediumShakeIntensity = 1.5f;
        [SerializeField] private float _mediumShakeDuration = 0.2f;
        [SerializeField] private float _largeShakeIntensity = 3f;
        [SerializeField] private float _largeShakeDuration = 0.4f;
        [SerializeField] private float _bossShakeIntensity = 5f;
        [SerializeField] private float _bossShakeDuration = 0.6f;

        [Header("Hitstop (Freeze Frame)")]
        [SerializeField] private bool _enableHitstop = true;
        [SerializeField] private float _smallHitstopDuration = 0.02f;
        [SerializeField] private float _mediumHitstopDuration = 0.05f;
        [SerializeField] private float _largeHitstopDuration = 0.1f;
        [SerializeField] private float _bossHitstopDuration = 0.2f;

        [Header("Screen Flash")]
        [SerializeField] private bool _enableScreenFlash = true;
        [SerializeField] private Color _damageFlashColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color _healFlashColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color _powerUpFlashColor = new Color(1f, 0.8f, 0f, 0.2f);
        [SerializeField] private float _flashDuration = 0.1f;

        [Header("Time Scale")]
        [SerializeField] private bool _enableSlowMotion = true;
        [SerializeField] private float _slowMotionScale = 0.3f;
        [SerializeField] private float _slowMotionDuration = 0.15f;

        [Header("Combo Feedback")]
        [SerializeField] private float _comboShakeMultiplier = 0.1f; // Shake increases with combo
        [SerializeField] private int _comboMilestone = 10; // Every X kills = bigger feedback

        [Header("References")]
        [SerializeField] private CameraController _cameraController;

        // Cached references
        private float _originalTimeScale = 1f;
        private bool _isTimeScaled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_cameraController == null)
            {
                _cameraController = FindFirstObjectByType<CameraController>();
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();

            // Restore time scale
            if (_isTimeScaled)
            {
                Time.timeScale = _originalTimeScale;
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
            // Shake based on enemy type
            switch (evt.enemyType)
            {
                case EnemyType.DataMite:
                case EnemyType.Fizzer:
                    // Tiny shake for small enemies
                    TriggerCameraShake(_smallShakeIntensity * 0.5f, _smallShakeDuration * 0.5f);
                    break;

                case EnemyType.ScanDrone:
                case EnemyType.UFO:
                    TriggerCameraShake(_smallShakeIntensity, _smallShakeDuration);
                    TriggerHitstop(_smallHitstopDuration);
                    break;

                case EnemyType.ChaosWorm:
                case EnemyType.CrystalShard:
                    TriggerCameraShake(_mediumShakeIntensity, _mediumShakeDuration);
                    TriggerHitstop(_mediumHitstopDuration);
                    break;

                case EnemyType.VoidSphere:
                    TriggerCameraShake(_largeShakeIntensity, _largeShakeDuration);
                    TriggerHitstop(_largeHitstopDuration);
                    TriggerSlowMotion();
                    break;

                case EnemyType.Boss:
                    TriggerCameraShake(_bossShakeIntensity, _bossShakeDuration);
                    TriggerHitstop(_bossHitstopDuration);
                    TriggerSlowMotion();
                    break;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            TriggerCameraShake(_mediumShakeIntensity, _mediumShakeDuration);
            TriggerScreenFlash(_damageFlashColor);
            TriggerHitstop(_mediumHitstopDuration);
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            TriggerScreenFlash(_healFlashColor);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            switch (evt.pickupType)
            {
                case PickupType.PowerUp:
                    TriggerScreenFlash(_powerUpFlashColor);
                    TriggerCameraShake(_smallShakeIntensity, _smallShakeDuration);
                    break;

                case PickupType.Invulnerable:
                    TriggerScreenFlash(new Color(1f, 1f, 0f, 0.3f));
                    TriggerCameraShake(_mediumShakeIntensity, _mediumShakeDuration);
                    break;

                default:
                    // Small feedback for other pickups
                    break;
            }
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Bigger feedback at combo milestones
            if (evt.comboCount > 0 && evt.comboCount % _comboMilestone == 0)
            {
                float intensity = Mathf.Min(_mediumShakeIntensity + (evt.comboCount * _comboShakeMultiplier), _largeShakeIntensity);
                TriggerCameraShake(intensity, _mediumShakeDuration);

                // Flash based on multiplier
                Color flashColor = Color.Lerp(Color.white, Color.yellow, evt.multiplier / 10f);
                flashColor.a = 0.2f;
                TriggerScreenFlash(flashColor);
            }
        }

        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            // Small zoom/shake on dash
            TriggerCameraShake(_smallShakeIntensity * 0.3f, _smallShakeDuration);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            TriggerCameraShake(_mediumShakeIntensity, _mediumShakeDuration);
            TriggerScreenFlash(new Color(1f, 1f, 1f, 0.3f));
        }

        private void OnGameOver(GameOverEvent evt)
        {
            TriggerCameraShake(_largeShakeIntensity, _largeShakeDuration);
            TriggerSlowMotion();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Trigger camera shake manually
        /// </summary>
        public void TriggerCameraShake(float intensity, float duration)
        {
            if (_cameraController != null)
            {
                _cameraController.Shake(intensity, duration);
            }
        }

        /// <summary>
        /// Trigger hitstop (freeze frame) effect
        /// </summary>
        public void TriggerHitstop(float duration)
        {
            if (!_enableHitstop || duration <= 0) return;

            StartCoroutine(HitstopCoroutine(duration));
        }

        /// <summary>
        /// Trigger slow motion effect
        /// </summary>
        public void TriggerSlowMotion()
        {
            if (!_enableSlowMotion) return;

            StartCoroutine(SlowMotionCoroutine());
        }

        /// <summary>
        /// Trigger screen flash
        /// </summary>
        public void TriggerScreenFlash(Color color)
        {
            if (!_enableScreenFlash) return;

            // Use ScreenFlash component
            if (ScreenFlash.Instance != null)
            {
                ScreenFlash.Instance.Flash(color, _flashDuration);
            }
        }

        #endregion

        #region Coroutines

        private System.Collections.IEnumerator HitstopCoroutine(float duration)
        {
            if (_isTimeScaled) yield break;

            _isTimeScaled = true;
            _originalTimeScale = Time.timeScale;

            Time.timeScale = 0f;

            // Use unscaled time to wait
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = _originalTimeScale;
            _isTimeScaled = false;
        }

        private System.Collections.IEnumerator SlowMotionCoroutine()
        {
            if (_isTimeScaled) yield break;

            _isTimeScaled = true;
            _originalTimeScale = Time.timeScale;

            Time.timeScale = _slowMotionScale;

            // Lerp back to normal
            float elapsed = 0f;
            while (elapsed < _slowMotionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slowMotionDuration;
                Time.timeScale = Mathf.Lerp(_slowMotionScale, _originalTimeScale, t);
                yield return null;
            }

            Time.timeScale = _originalTimeScale;
            _isTimeScaled = false;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Small Shake")]
        private void DebugSmallShake() => TriggerCameraShake(_smallShakeIntensity, _smallShakeDuration);

        [ContextMenu("Debug: Large Shake")]
        private void DebugLargeShake() => TriggerCameraShake(_largeShakeIntensity, _largeShakeDuration);

        [ContextMenu("Debug: Hitstop")]
        private void DebugHitstop() => TriggerHitstop(_mediumHitstopDuration);

        [ContextMenu("Debug: Slow Motion")]
        private void DebugSlowMotion() => TriggerSlowMotion();

        #endregion
    }
}
