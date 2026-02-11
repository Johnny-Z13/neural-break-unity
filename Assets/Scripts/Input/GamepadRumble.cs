using UnityEngine;
using UnityEngine.InputSystem;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Input
{
    /// <summary>
    /// Manages gamepad rumble/haptic feedback.
    /// Provides satisfying controller vibration for game events.
    ///
    /// ZERO-ALLOCATION: All rumble effects use timer-based Update instead of coroutines.
    /// </summary>
    public class GamepadRumble : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private bool m_enableRumble = true;
        [SerializeField] private float m_rumbleIntensityMultiplier = 1f;

        [Header("Hit Feedback")]
        [SerializeField] private float m_smallHitLow = 0.1f;
        [SerializeField] private float m_smallHitHigh = 0.2f;
        [SerializeField] private float m_smallHitDuration = 0.05f;

        [SerializeField] private float m_mediumHitLow = 0.3f;
        [SerializeField] private float m_mediumHitHigh = 0.4f;
        [SerializeField] private float m_mediumHitDuration = 0.1f;

        [SerializeField] private float m_largeHitLow = 0.6f;
        [SerializeField] private float m_largeHitHigh = 0.8f;
        [SerializeField] private float m_largeHitDuration = 0.15f;

        [Header("Player Damage")]
        [SerializeField] private float m_damageLow = 0.5f;
        [SerializeField] private float m_damageHigh = 0.7f;
        [SerializeField] private float m_damageDuration = 0.2f;

        [Header("Special Events")]
        [SerializeField] private float m_levelUpLow = 0.3f;
        [SerializeField] private float m_levelUpHigh = 0.5f;
        [SerializeField] private float m_levelUpDuration = 0.3f;

        [SerializeField] private float m_dashLow = 0.2f;
        [SerializeField] private float m_dashHigh = 0.1f;
        [SerializeField] private float m_dashDuration = 0.08f;

        // State - timer-based rumble (replaces coroutines - zero allocation)
        private Gamepad m_currentGamepad;
        private bool m_isRumbling;
        private float m_rumbleEndTime;

        // Pulse rumble state (replaces PulseRumble coroutine)
        private bool m_isPulsing;
        private float m_pulseLow;
        private float m_pulseHigh;
        private float m_pulseDuration;
        private int m_pulseRemaining;
        private float m_nextPulseTime;

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            StopRumble();
            UnsubscribeFromEvents();

        }

        private void Update()
        {
            // Track current gamepad
            m_currentGamepad = Gamepad.current;

            // Timer-based rumble stop (replaces RumbleCoroutine - zero allocation)
            if (m_isRumbling && Time.unscaledTime >= m_rumbleEndTime)
            {
                StopRumble();
                m_isRumbling = false;
            }

            // Timer-based pulse rumble (replaces PulseRumble coroutine - zero allocation)
            if (m_isPulsing && Time.unscaledTime >= m_nextPulseTime)
            {
                if (m_pulseRemaining > 0)
                {
                    Rumble(m_pulseLow, m_pulseHigh, m_pulseDuration);
                    m_pulseRemaining--;
                    m_nextPulseTime = Time.unscaledTime + m_pulseDuration * 1.5f;
                }
                else
                {
                    m_isPulsing = false;
                }
            }
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
                    Rumble(m_smallHitLow, m_smallHitHigh, m_smallHitDuration);
                    break;

                case EnemyType.ScanDrone:
                case EnemyType.CrystalShard:
                    Rumble(m_mediumHitLow, m_mediumHitHigh, m_mediumHitDuration);
                    break;

                case EnemyType.ChaosWorm:
                case EnemyType.UFO:
                    Rumble(m_mediumHitLow * 1.2f, m_mediumHitHigh * 1.2f, m_mediumHitDuration * 1.5f);
                    break;

                case EnemyType.VoidSphere:
                    Rumble(m_largeHitLow, m_largeHitHigh, m_largeHitDuration);
                    break;

                case EnemyType.Boss:
                    Rumble(m_largeHitLow * 1.5f, m_largeHitHigh * 1.5f, m_largeHitDuration * 2f);
                    break;
            }
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            // Very light rumble for hits
            Rumble(m_smallHitLow * 0.5f, m_smallHitHigh * 0.5f, m_smallHitDuration * 0.5f);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            Rumble(m_damageLow, m_damageHigh, m_damageDuration);
        }

        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            Rumble(m_dashLow, m_dashHigh, m_dashDuration);
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
        {
            // Pulsing rumble for level up (timer-based - zero allocation)
            PulseRumble(m_levelUpLow, m_levelUpHigh, m_levelUpDuration, 3);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            // Light positive feedback
            Rumble(m_smallHitLow, m_smallHitHigh * 0.5f, m_smallHitDuration);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            PulseRumble(m_mediumHitLow, m_mediumHitHigh, 0.15f, 4);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Long dramatic rumble
            Rumble(m_largeHitLow, m_largeHitHigh, 0.5f);
        }

        /// <summary>
        /// Trigger rumble with specified intensity and duration.
        /// Timer-based (zero allocation - no coroutines).
        /// </summary>
        public void Rumble(float lowFrequency, float highFrequency, float duration)
        {
            if (!m_enableRumble || m_currentGamepad == null) return;

            // Apply multiplier
            lowFrequency *= m_rumbleIntensityMultiplier;
            highFrequency *= m_rumbleIntensityMultiplier;

            // Clamp values
            lowFrequency = Mathf.Clamp01(lowFrequency);
            highFrequency = Mathf.Clamp01(highFrequency);

            m_currentGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
            m_isRumbling = true;
            m_rumbleEndTime = Time.unscaledTime + duration;
        }

        /// <summary>
        /// Trigger pulsing rumble effect.
        /// Timer-based (zero allocation - no coroutines).
        /// </summary>
        private void PulseRumble(float lowFreq, float highFreq, float pulseDuration, int pulseCount)
        {
            m_isPulsing = true;
            m_pulseLow = lowFreq;
            m_pulseHigh = highFreq;
            m_pulseDuration = pulseDuration;
            m_pulseRemaining = pulseCount;
            m_nextPulseTime = 0f; // Trigger first pulse immediately on next Update

            // Start first pulse immediately
            Rumble(lowFreq, highFreq, pulseDuration);
            m_pulseRemaining--;
            m_nextPulseTime = Time.unscaledTime + pulseDuration * 1.5f;
        }

        /// <summary>
        /// Stop all rumble
        /// </summary>
        public void StopRumble()
        {
            if (m_currentGamepad != null)
            {
                m_currentGamepad.SetMotorSpeeds(0, 0);
            }
        }

        /// <summary>
        /// Enable/disable rumble
        /// </summary>
        public void SetRumbleEnabled(bool enabled)
        {
            m_enableRumble = enabled;
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
            m_rumbleIntensityMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
        }

        #region Debug

        [ContextMenu("Debug: Small Rumble")]
        private void DebugSmallRumble() => Rumble(m_smallHitLow, m_smallHitHigh, m_smallHitDuration);

        [ContextMenu("Debug: Medium Rumble")]
        private void DebugMediumRumble() => Rumble(m_mediumHitLow, m_mediumHitHigh, m_mediumHitDuration);

        [ContextMenu("Debug: Large Rumble")]
        private void DebugLargeRumble() => Rumble(m_largeHitLow, m_largeHitHigh, m_largeHitDuration);

        [ContextMenu("Debug: Damage Rumble")]
        private void DebugDamageRumble() => Rumble(m_damageLow, m_damageHigh, m_damageDuration);

        #endregion
    }
}
