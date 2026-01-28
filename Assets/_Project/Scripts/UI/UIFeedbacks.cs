using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Centralized UI feedback system.
    /// Note: MMFeedbacks (Feel) has been removed.
    /// This class now serves as a stub that logs feedback events.
    /// Future implementation can add DOTween, LeanTween, or custom effects.
    /// </summary>
    public class UIFeedbacks : MonoBehaviour
    {
        // Note: MMFeedbacks removed - all MMF_Player fields have been removed

        private void Start()
        {
            // Subscribe to events for automatic feedback
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
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
                // Feedback (Feel removed) - Score punch
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
                    // Feedback (Feel removed) - Combo milestone
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
                // Feedback (Feel removed) - Multiplier
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Feedback (Feel removed) - Damage

            float healthPercent = evt.maxHealth > 0 ? (float)evt.currentHealth / evt.maxHealth : 0;
            if (healthPercent <= 0.25f)
            {
                // Feedback (Feel removed) - Low health
            }
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            // Feedback (Feel removed) - Heal
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            // Only play on shield gain
            if (evt.currentShields > 0)
            {
                // Feedback (Feel removed) - Shield gain
            }
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            // Feedback (Feel removed) - Level up
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            // Feedback (Feel removed) - Wave start
        }

        private void OnBossEncounter(BossEncounterEvent evt)
        {
            if (evt.isBossActive)
            {
                // Feedback (Feel removed) - Boss warning
            }
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            // Feedback (Feel removed) - Achievement unlock
        }

        #endregion

        #region Debug

        [ContextMenu("Test: Score Punch")]
        private void TestScorePunch()
        {
            Debug.Log("[UIFeedbacks] Score punch feedback (Feel removed)");
        }

        [ContextMenu("Test: Combo Milestone")]
        private void TestComboMilestone()
        {
            Debug.Log("[UIFeedbacks] Combo milestone feedback (Feel removed)");
        }

        [ContextMenu("Test: Damage")]
        private void TestDamage()
        {
            Debug.Log("[UIFeedbacks] Damage feedback (Feel removed)");
        }

        [ContextMenu("Test: Heal")]
        private void TestHeal()
        {
            Debug.Log("[UIFeedbacks] Heal feedback (Feel removed)");
        }

        [ContextMenu("Test: Level Up")]
        private void TestLevelUp()
        {
            Debug.Log("[UIFeedbacks] Level up feedback (Feel removed)");
        }

        #endregion
    }
}
