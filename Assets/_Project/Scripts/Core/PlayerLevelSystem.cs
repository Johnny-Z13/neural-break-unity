using UnityEngine;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages player XP and level progression.
    /// Calculates XP requirements per level and handles level-up events.
    /// </summary>
    public class PlayerLevelSystem : MonoBehaviour
    {

        [Header("Level Settings")]
        [SerializeField] private int m_currentLevel = 1;
        [SerializeField] private int m_currentXP = 0;
        [SerializeField] private int m_maxLevel = 50;

        [Header("XP Curve")]
        [SerializeField] private int m_baseXPRequired = 10;
        [SerializeField] private float m_xpMultiplierPerLevel = 1.15f;

        // Note: MMFeedbacks removed

        // Events
        public System.Action<int, int, int> OnXPChanged; // currentXP, xpForLevel, level
        public System.Action<int> OnLevelUp; // newLevel

        // Public accessors
        public int CurrentLevel => m_currentLevel;
        public int CurrentXP => m_currentXP;
        public int MaxLevel => m_maxLevel;
        public int XPForCurrentLevel => GetXPForLevel(m_currentLevel);
        public int XPForNextLevel => GetXPForLevel(m_currentLevel + 1);
        public float LevelProgress => (float)m_currentXP / XPForCurrentLevel;
        public int TotalXP { get; private set; }

        private void Start()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Reset();
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            AddXP(evt.xpValue);
        }

        /// <summary>
        /// Calculate XP required to complete a level
        /// </summary>
        public int GetXPForLevel(int level)
        {
            if (level <= 1) return m_baseXPRequired;
            return Mathf.RoundToInt(m_baseXPRequired * Mathf.Pow(m_xpMultiplierPerLevel, level - 1));
        }

        /// <summary>
        /// Add XP and check for level ups
        /// </summary>
        public void AddXP(int amount)
        {
            if (m_currentLevel >= m_maxLevel) return;

            TotalXP += amount;
            m_currentXP += amount;

            // Check for level up(s)
            while (m_currentXP >= XPForCurrentLevel && m_currentLevel < m_maxLevel)
            {
                m_currentXP -= XPForCurrentLevel;
                LevelUp();
            }

            // Clamp XP at max level
            if (m_currentLevel >= m_maxLevel)
            {
                m_currentXP = 0;
            }

            // Notify listeners
            OnXPChanged?.Invoke(m_currentXP, XPForCurrentLevel, m_currentLevel);
        }

        private void LevelUp()
        {
            m_currentLevel++;

            // Feedback (Feel removed)

            // Notify listeners
            OnLevelUp?.Invoke(m_currentLevel);

            // Publish event
            EventBus.Publish(new PlayerLevelUpEvent
            {
                newLevel = m_currentLevel,
                totalXP = TotalXP
            });

            Debug.Log($"[PlayerLevelSystem] Level Up! Now level {m_currentLevel}");
        }

        /// <summary>
        /// Reset for new game
        /// </summary>
        public void Reset()
        {
            m_currentLevel = 1;
            m_currentXP = 0;
            TotalXP = 0;

            OnXPChanged?.Invoke(m_currentXP, XPForCurrentLevel, m_currentLevel);
        }

        #region Debug

        [ContextMenu("Debug: Add 10 XP")]
        private void DebugAdd10XP() => AddXP(10);

        [ContextMenu("Debug: Add 50 XP")]
        private void DebugAdd50XP() => AddXP(50);

        [ContextMenu("Debug: Add 100 XP")]
        private void DebugAdd100XP() => AddXP(100);

        [ContextMenu("Debug: Reset")]
        private void DebugReset() => Reset();

        [ContextMenu("Debug: Show Level Info")]
        private void DebugShowInfo()
        {
            Debug.Log($"[PlayerLevelSystem] Level {m_currentLevel}, XP: {m_currentXP}/{XPForCurrentLevel}");
            Debug.Log($"[PlayerLevelSystem] Next levels: L{m_currentLevel + 1}={GetXPForLevel(m_currentLevel + 1)}, L{m_currentLevel + 2}={GetXPForLevel(m_currentLevel + 2)}, L{m_currentLevel + 3}={GetXPForLevel(m_currentLevel + 3)}");
        }

        #endregion
    }
}
