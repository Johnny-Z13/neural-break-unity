using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.Input
{
    /// <summary>
    /// Manages gamepad rumble/haptic feedback.
    /// Provides satisfying controller vibration for game events.
    /// </summary>
    public class GamepadRumble : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private bool _enableRumble = true;
        [SerializeField] private float _rumbleIntensityMultiplier = 1f;

        [Header("Hit Feedback")]
        [SerializeField] private float _smallHitLow = 0.1f;
        [SerializeField] private float _smallHitHigh = 0.2f;
        [SerializeField] private float _smallHitDuration = 0.05f;

        [SerializeField] private float _mediumHitLow = 0.3f;
        [SerializeField] private float _mediumHitHigh = 0.4f;
        [SerializeField] private float _mediumHitDuration = 0.1f;

        [SerializeField] private float _largeHitLow = 0.6f;
        [SerializeField] private float _largeHitHigh = 0.8f;
        [SerializeField] private float _largeHitDuration = 0.15f;

        [Header("Player Damage")]
        [SerializeField] private float _damageLow = 0.5f;
        [SerializeField] private float _damageHigh = 0.7f;
        [SerializeField] private float _damageDuration = 0.2f;

        [Header("Special Events")]
        [SerializeField] private float _levelUpLow = 0.3f;
        [SerializeField] private float _levelUpHigh = 0.5f;
        [SerializeField] private float _levelUpDuration = 0.3f;

        [SerializeField] private float _dashLow = 0.2f;
        [SerializeField] private float _dashHigh = 0.1f;
        [SerializeField] private float _dashDuration = 0.08f;

        // State
        private Coroutine _rumbleCoroutine;
        private Gamepad _currentGamepad;

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
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            StopRumble();
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Track current gamepad
            _currentGamepad = Gamepad.current;
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerDashedEvent>(OnPlayerDashed);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Bigger rumble for bigger enemies
            switch (evt.enemyType)
            {
                case EnemyType.DataMite:
                case EnemyType.Fizzer:
                    Rumble(_smallHitLow, _smallHitHigh, _smallHitDuration);
                    break;

                case EnemyType.ScanDrone:
                case EnemyType.CrystalShard:
                    Rumble(_mediumHitLow, _mediumHitHigh, _mediumHitDuration);
                    break;

                case EnemyType.ChaosWorm:
                case EnemyType.UFO:
                    Rumble(_mediumHitLow * 1.2f, _mediumHitHigh * 1.2f, _mediumHitDuration * 1.5f);
                    break;

                case EnemyType.VoidSphere:
                    Rumble(_largeHitLow, _largeHitHigh, _largeHitDuration);
                    break;

                case EnemyType.Boss:
                    Rumble(_largeHitLow * 1.5f, _largeHitHigh * 1.5f, _largeHitDuration * 2f);
                    break;
            }
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            // Very light rumble for hits
            Rumble(_smallHitLow * 0.5f, _smallHitHigh * 0.5f, _smallHitDuration * 0.5f);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            Rumble(_damageLow, _damageHigh, _damageDuration);
        }

        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            Rumble(_dashLow, _dashHigh, _dashDuration);
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
        {
            // Pulsing rumble for level up
            StartCoroutine(PulseRumble(_levelUpLow, _levelUpHigh, _levelUpDuration, 3));
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            // Light positive feedback
            Rumble(_smallHitLow, _smallHitHigh * 0.5f, _smallHitDuration);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            StartCoroutine(PulseRumble(_mediumHitLow, _mediumHitHigh, 0.15f, 4));
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Long dramatic rumble
            Rumble(_largeHitLow, _largeHitHigh, 0.5f);
        }

        /// <summary>
        /// Trigger rumble with specified intensity and duration
        /// </summary>
        public void Rumble(float lowFrequency, float highFrequency, float duration)
        {
            if (!_enableRumble || _currentGamepad == null) return;

            // Apply multiplier
            lowFrequency *= _rumbleIntensityMultiplier;
            highFrequency *= _rumbleIntensityMultiplier;

            // Clamp values
            lowFrequency = Mathf.Clamp01(lowFrequency);
            highFrequency = Mathf.Clamp01(highFrequency);

            if (_rumbleCoroutine != null)
            {
                StopCoroutine(_rumbleCoroutine);
            }
            _rumbleCoroutine = StartCoroutine(RumbleCoroutine(lowFrequency, highFrequency, duration));
        }

        private IEnumerator RumbleCoroutine(float lowFreq, float highFreq, float duration)
        {
            if (_currentGamepad == null) yield break;

            _currentGamepad.SetMotorSpeeds(lowFreq, highFreq);

            yield return new WaitForSecondsRealtime(duration);

            StopRumble();
            _rumbleCoroutine = null;
        }

        private IEnumerator PulseRumble(float lowFreq, float highFreq, float pulseDuration, int pulseCount)
        {
            for (int i = 0; i < pulseCount; i++)
            {
                Rumble(lowFreq, highFreq, pulseDuration);
                yield return new WaitForSecondsRealtime(pulseDuration * 1.5f);
            }
        }

        /// <summary>
        /// Stop all rumble
        /// </summary>
        public void StopRumble()
        {
            if (_currentGamepad != null)
            {
                _currentGamepad.SetMotorSpeeds(0, 0);
            }
        }

        /// <summary>
        /// Enable/disable rumble
        /// </summary>
        public void SetRumbleEnabled(bool enabled)
        {
            _enableRumble = enabled;
            if (!enabled)
            {
                StopRumble();
            }
        }

        /// <summary>
        /// Set rumble intensity multiplier (0-2)
        /// </summary>
        public void SetIntensity(float multiplier)
        {
            _rumbleIntensityMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
        }

        #region Debug

        [ContextMenu("Debug: Small Rumble")]
        private void DebugSmallRumble() => Rumble(_smallHitLow, _smallHitHigh, _smallHitDuration);

        [ContextMenu("Debug: Medium Rumble")]
        private void DebugMediumRumble() => Rumble(_mediumHitLow, _mediumHitHigh, _mediumHitDuration);

        [ContextMenu("Debug: Large Rumble")]
        private void DebugLargeRumble() => Rumble(_largeHitLow, _largeHitHigh, _largeHitDuration);

        [ContextMenu("Debug: Damage Rumble")]
        private void DebugDamageRumble() => Rumble(_damageLow, _damageHigh, _damageDuration);

        #endregion
    }
}
