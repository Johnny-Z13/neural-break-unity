using UnityEngine;
using System.Collections.Generic;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Types of achievements
    /// </summary>
    public enum AchievementType
    {
        // Kill achievements
        FirstBlood,
        Slayer100,
        Slayer500,
        Slayer1000,
        BossKiller,
        BossSlayer5,

        // Combo achievements
        Combo10,
        Combo25,
        Combo50,
        ComboMaster,

        // Survival achievements
        Survive1Min,
        Survive5Min,
        Survive10Min,
        Invincible,

        // Level achievements
        Level10,
        Level25,
        Level50,
        Level99,

        // Score achievements
        Score10K,
        Score50K,
        Score100K,
        Millionaire,

        // Special achievements
        NoDamage,
        SpeedRunner,
        Collector,
        UpgradeHoarder
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [System.Serializable]
    public class Achievement
    {
        public AchievementType type;
        public string name;
        public string description;
        public bool isUnlocked;
        public System.DateTime unlockedAt;
    }

    /// <summary>
    /// Event fired when achievement is unlocked
    /// </summary>
    public struct AchievementUnlockedEvent
    {
        public AchievementType type;
        public string name;
        public string description;
    }

    /// <summary>
    /// Manages achievement tracking and unlocking.
    /// Saves achievement progress to PlayerPrefs.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private bool _saveToPrefs = true;
        [SerializeField] private string _prefsKey = "NeuralBreak_Achievements";

        // Achievement definitions
        private Dictionary<AchievementType, Achievement> _achievements = new Dictionary<AchievementType, Achievement>();

        // Tracking stats for current session
        private int _sessionKills;
        private int _sessionBossKills;
        private int _sessionHighestCombo;
        private float _sessionSurvivalTime;
        private int _sessionDamageTaken;
        private int _sessionPickupsCollected;
        private int _sessionUpgradesUsed;

        public IReadOnlyDictionary<AchievementType, Achievement> Achievements => _achievements;

        private void Awake()
        {

            InitializeAchievements();
            LoadProgress();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

        }

        private void InitializeAchievements()
        {
            // Kill achievements
            AddAchievement(AchievementType.FirstBlood, "First Blood", "Kill your first enemy");
            AddAchievement(AchievementType.Slayer100, "Slayer", "Kill 100 enemies");
            AddAchievement(AchievementType.Slayer500, "Mass Destruction", "Kill 500 enemies");
            AddAchievement(AchievementType.Slayer1000, "Genocide", "Kill 1000 enemies");
            AddAchievement(AchievementType.BossKiller, "Boss Killer", "Defeat a boss");
            AddAchievement(AchievementType.BossSlayer5, "Boss Hunter", "Defeat 5 bosses");

            // Combo achievements
            AddAchievement(AchievementType.Combo10, "Combo Starter", "Reach a 10x combo");
            AddAchievement(AchievementType.Combo25, "Combo Pro", "Reach a 25x combo");
            AddAchievement(AchievementType.Combo50, "Combo Master", "Reach a 50x combo");
            AddAchievement(AchievementType.ComboMaster, "Unstoppable", "Reach a 100x combo");

            // Survival achievements
            AddAchievement(AchievementType.Survive1Min, "Survivor", "Survive for 1 minute");
            AddAchievement(AchievementType.Survive5Min, "Endurance", "Survive for 5 minutes");
            AddAchievement(AchievementType.Survive10Min, "Marathon", "Survive for 10 minutes");
            AddAchievement(AchievementType.Invincible, "Invincible", "Complete a level without taking damage");

            // Level achievements
            AddAchievement(AchievementType.Level10, "Progressing", "Reach Level 10");
            AddAchievement(AchievementType.Level25, "Halfway There", "Reach Level 25");
            AddAchievement(AchievementType.Level50, "Veteran", "Reach Level 50");
            AddAchievement(AchievementType.Level99, "Champion", "Complete all 99 levels");

            // Score achievements
            AddAchievement(AchievementType.Score10K, "Score Chaser", "Score 10,000 points");
            AddAchievement(AchievementType.Score50K, "High Scorer", "Score 50,000 points");
            AddAchievement(AchievementType.Score100K, "Score Master", "Score 100,000 points");
            AddAchievement(AchievementType.Millionaire, "Millionaire", "Score 1,000,000 points");

            // Special achievements
            AddAchievement(AchievementType.NoDamage, "Untouchable", "Complete 3 levels without taking damage");
            AddAchievement(AchievementType.SpeedRunner, "Speed Runner", "Complete Level 10 in under 5 minutes");
            AddAchievement(AchievementType.Collector, "Collector", "Collect 50 pickups in one game");
            AddAchievement(AchievementType.UpgradeHoarder, "Upgrade Hoarder", "Have 3 weapon upgrades active at once");
        }

        private void AddAchievement(AchievementType type, string name, string description)
        {
            _achievements[type] = new Achievement
            {
                type = type,
                name = name,
                description = description,
                isUnlocked = false
            };
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<WeaponUpgradeActivatedEvent>(OnUpgradeActivated);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<WeaponUpgradeActivatedEvent>(OnUpgradeActivated);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                _sessionSurvivalTime += Time.deltaTime;

                // Check survival achievements
                if (_sessionSurvivalTime >= 60f) TryUnlock(AchievementType.Survive1Min);
                if (_sessionSurvivalTime >= 300f) TryUnlock(AchievementType.Survive5Min);
                if (_sessionSurvivalTime >= 600f) TryUnlock(AchievementType.Survive10Min);
            }
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            ResetSessionStats();
        }

        private void ResetSessionStats()
        {
            _sessionKills = 0;
            _sessionBossKills = 0;
            _sessionHighestCombo = 0;
            _sessionSurvivalTime = 0f;
            _sessionDamageTaken = 0;
            _sessionPickupsCollected = 0;
            _sessionUpgradesUsed = 0;
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            _sessionKills++;

            // First Blood
            if (_sessionKills == 1)
            {
                TryUnlock(AchievementType.FirstBlood);
            }

            // Kill milestones
            if (_sessionKills >= 100) TryUnlock(AchievementType.Slayer100);
            if (_sessionKills >= 500) TryUnlock(AchievementType.Slayer500);
            if (_sessionKills >= 1000) TryUnlock(AchievementType.Slayer1000);

            // Boss kills
            if (evt.enemyType == EnemyType.Boss)
            {
                _sessionBossKills++;
                TryUnlock(AchievementType.BossKiller);
                if (_sessionBossKills >= 5) TryUnlock(AchievementType.BossSlayer5);
            }
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            if (evt.comboCount > _sessionHighestCombo)
            {
                _sessionHighestCombo = evt.comboCount;
            }

            if (evt.comboCount >= 10) TryUnlock(AchievementType.Combo10);
            if (evt.comboCount >= 25) TryUnlock(AchievementType.Combo25);
            if (evt.comboCount >= 50) TryUnlock(AchievementType.Combo50);
            if (evt.comboCount >= 100) TryUnlock(AchievementType.ComboMaster);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            _sessionDamageTaken += evt.damage;
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            // Level achievements
            if (evt.levelNumber >= 10) TryUnlock(AchievementType.Level10);
            if (evt.levelNumber >= 25) TryUnlock(AchievementType.Level25);
            if (evt.levelNumber >= 50) TryUnlock(AchievementType.Level50);
            if (evt.levelNumber >= 99) TryUnlock(AchievementType.Level99);

            // Speed runner (level 10 in under 5 min)
            if (evt.levelNumber == 10 && _sessionSurvivalTime < 300f)
            {
                TryUnlock(AchievementType.SpeedRunner);
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            if (evt.newScore >= 10000) TryUnlock(AchievementType.Score10K);
            if (evt.newScore >= 50000) TryUnlock(AchievementType.Score50K);
            if (evt.newScore >= 100000) TryUnlock(AchievementType.Score100K);
            if (evt.newScore >= 1000000) TryUnlock(AchievementType.Millionaire);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            _sessionPickupsCollected++;

            if (_sessionPickupsCollected >= 50)
            {
                TryUnlock(AchievementType.Collector);
            }
        }

        private void OnUpgradeActivated(WeaponUpgradeActivatedEvent evt)
        {
            _sessionUpgradesUsed++;

            // Check if 3 upgrades active at once
            var upgradeManager = FindObjectOfType<Combat.WeaponUpgradeManager>();
            if (upgradeManager != null)
            {
                int activeCount = 0;
                if (upgradeManager.HasSpreadShot) activeCount++;
                if (upgradeManager.HasPiercing) activeCount++;
                if (upgradeManager.HasRapidFire) activeCount++;
                if (upgradeManager.HasHoming) activeCount++;

                if (activeCount >= 3)
                {
                    TryUnlock(AchievementType.UpgradeHoarder);
                }
            }
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
        {
            // Player level achievements could go here
        }

        /// <summary>
        /// Try to unlock an achievement
        /// </summary>
        public bool TryUnlock(AchievementType type)
        {
            if (!_achievements.TryGetValue(type, out var achievement)) return false;
            if (achievement.isUnlocked) return false;

            achievement.isUnlocked = true;
            achievement.unlockedAt = System.DateTime.Now;

            Debug.Log($"[Achievement] Unlocked: {achievement.name}");

            // Publish event
            EventBus.Publish(new AchievementUnlockedEvent
            {
                type = type,
                name = achievement.name,
                description = achievement.description
            });

            // Save progress
            if (_saveToPrefs)
            {
                SaveProgress();
            }

            return true;
        }

        /// <summary>
        /// Check if achievement is unlocked
        /// </summary>
        public bool IsUnlocked(AchievementType type)
        {
            return _achievements.TryGetValue(type, out var achievement) && achievement.isUnlocked;
        }

        /// <summary>
        /// Get achievement info
        /// </summary>
        public Achievement GetAchievement(AchievementType type)
        {
            return _achievements.TryGetValue(type, out var achievement) ? achievement : null;
        }

        /// <summary>
        /// Get total unlocked count
        /// </summary>
        public int GetUnlockedCount()
        {
            int count = 0;
            foreach (var a in _achievements.Values)
            {
                if (a.isUnlocked) count++;
            }
            return count;
        }

        /// <summary>
        /// Get total achievement count
        /// </summary>
        public int GetTotalCount() => _achievements.Count;

        /// <summary>
        /// Get list of unlocked achievement IDs for save system
        /// </summary>
        public List<string> GetUnlockedAchievementIds()
        {
            var ids = new List<string>();
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.isUnlocked)
                {
                    ids.Add(kvp.Key.ToString());
                }
            }
            return ids;
        }

        #region Save/Load

        private void SaveProgress()
        {
            var data = new AchievementSaveData();
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.isUnlocked)
                {
                    data.unlockedAchievements.Add(kvp.Key.ToString());
                }
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(_prefsKey, json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (!PlayerPrefs.HasKey(_prefsKey)) return;

            string json = PlayerPrefs.GetString(_prefsKey);
            var data = JsonUtility.FromJson<AchievementSaveData>(json);

            if (data?.unlockedAchievements != null)
            {
                foreach (string typeStr in data.unlockedAchievements)
                {
                    if (System.Enum.TryParse<AchievementType>(typeStr, out var type))
                    {
                        if (_achievements.TryGetValue(type, out var achievement))
                        {
                            achievement.isUnlocked = true;
                        }
                    }
                }
            }

            Debug.Log($"[Achievement] Loaded {GetUnlockedCount()}/{GetTotalCount()} achievements");
        }

        [System.Serializable]
        private class AchievementSaveData
        {
            public List<string> unlockedAchievements = new List<string>();
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Reset All Achievements")]
        private void DebugResetAll()
        {
            foreach (var a in _achievements.Values)
            {
                a.isUnlocked = false;
            }
            PlayerPrefs.DeleteKey(_prefsKey);
            Debug.Log("[Achievement] All achievements reset");
        }

        [ContextMenu("Debug: Unlock Random")]
        private void DebugUnlockRandom()
        {
            var types = new List<AchievementType>(_achievements.Keys);
            var randomType = types[Random.Range(0, types.Count)];
            TryUnlock(randomType);
        }

        #endregion
    }
}
