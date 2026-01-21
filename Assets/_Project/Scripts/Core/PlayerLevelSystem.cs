using UnityEngine;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages player XP and level progression.
    /// Calculates XP requirements per level and handles level-up events.
    /// </summary>
    public class PlayerLevelSystem : MonoBehaviour
    {
        public static PlayerLevelSystem Instance { get; private set; }

        [Header("Level Settings")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _currentXP = 0;
        [SerializeField] private int _maxLevel = 50;

        [Header("XP Curve")]
        [SerializeField] private int _baseXPRequired = 10;
        [SerializeField] private float _xpMultiplierPerLevel = 1.15f;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _levelUpFeedback;

        // Events
        public System.Action<int, int, int> OnXPChanged; // currentXP, xpForLevel, level
        public System.Action<int> OnLevelUp; // newLevel

        // Public accessors
        public int CurrentLevel => _currentLevel;
        public int CurrentXP => _currentXP;
        public int MaxLevel => _maxLevel;
        public int XPForCurrentLevel => GetXPForLevel(_currentLevel);
        public int XPForNextLevel => GetXPForLevel(_currentLevel + 1);
        public float LevelProgress => (float)_currentXP / XPForCurrentLevel;
        public int TotalXP { get; private set; }

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
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            if (Instance == this)
            {
                Instance = null;
            }
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
            if (level <= 1) return _baseXPRequired;
            return Mathf.RoundToInt(_baseXPRequired * Mathf.Pow(_xpMultiplierPerLevel, level - 1));
        }

        /// <summary>
        /// Add XP and check for level ups
        /// </summary>
        public void AddXP(int amount)
        {
            if (_currentLevel >= _maxLevel) return;

            TotalXP += amount;
            _currentXP += amount;

            // Check for level up(s)
            while (_currentXP >= XPForCurrentLevel && _currentLevel < _maxLevel)
            {
                _currentXP -= XPForCurrentLevel;
                LevelUp();
            }

            // Clamp XP at max level
            if (_currentLevel >= _maxLevel)
            {
                _currentXP = 0;
            }

            // Notify listeners
            OnXPChanged?.Invoke(_currentXP, XPForCurrentLevel, _currentLevel);
        }

        private void LevelUp()
        {
            _currentLevel++;

            // Play feedback
            _levelUpFeedback?.PlayFeedbacks();

            // Notify listeners
            OnLevelUp?.Invoke(_currentLevel);

            // Publish event
            EventBus.Publish(new PlayerLevelUpEvent
            {
                newLevel = _currentLevel,
                totalXP = TotalXP
            });

            Debug.Log($"[PlayerLevelSystem] Level Up! Now level {_currentLevel}");
        }

        /// <summary>
        /// Reset for new game
        /// </summary>
        public void Reset()
        {
            _currentLevel = 1;
            _currentXP = 0;
            TotalXP = 0;

            OnXPChanged?.Invoke(_currentXP, XPForCurrentLevel, _currentLevel);
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
            Debug.Log($"[PlayerLevelSystem] Level {_currentLevel}, XP: {_currentXP}/{XPForCurrentLevel}");
            Debug.Log($"[PlayerLevelSystem] Next levels: L{_currentLevel + 1}={GetXPForLevel(_currentLevel + 1)}, L{_currentLevel + 2}={GetXPForLevel(_currentLevel + 2)}, L{_currentLevel + 3}={GetXPForLevel(_currentLevel + 3)}");
        }

        #endregion
    }
}
