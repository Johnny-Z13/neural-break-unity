using UnityEngine;
using System;
using System.Collections.Generic;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Neural Break save system. Inherits from Z13.Core.SaveSystemBase.
    /// Handles player progress, unlocks, and statistics.
    /// </summary>
    public class SaveSystem : Z13.Core.SaveSystemBase<SaveData>
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Neural Break Settings")]
        [SerializeField] private bool m_autoSaveOnGameOver = true;

        public bool HasPlayed => CurrentSave != null && CurrentSave.hasPlayed;

        protected override string SaveFileName => "neural_break_save.json";
        protected override bool ShouldAutoSave => GameManager.Instance != null && GameManager.Instance.IsPlaying;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            base.Awake();
        }

        private void Start()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<VictoryEvent>(OnVictory);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<VictoryEvent>(OnVictory);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (m_autoSaveOnGameOver)
            {
                UpdateStatsFromGame(evt.finalStats);
                SaveGame();
            }
        }

        private void OnVictory(VictoryEvent evt)
        {
            UpdateStatsFromGame(evt.finalStats);
            SaveGame();
        }

        private void UpdateStatsFromGame(GameStats stats)
        {
            if (CurrentSave == null)
            {
                CurrentSave = new SaveData();
            }

            CurrentSave.hasPlayed = true;
            CurrentSave.totalGamesPlayed++;
            CurrentSave.totalTimePlayed += stats.survivedTime;
            CurrentSave.totalEnemiesKilled += stats.enemiesKilled;
            CurrentSave.totalBossesKilled += stats.bossesKilled;
            CurrentSave.totalXPEarned += stats.totalXP;
            CurrentSave.totalDamageTaken += stats.damageTaken;

            // Update records
            if (stats.score > CurrentSave.highScore)
                CurrentSave.highScore = stats.score;
            if (stats.level > CurrentSave.highestLevel)
                CurrentSave.highestLevel = stats.level;
            if (stats.highestCombo > CurrentSave.highestCombo)
                CurrentSave.highestCombo = stats.highestCombo;
            if (stats.highestMultiplier > CurrentSave.highestMultiplier)
                CurrentSave.highestMultiplier = stats.highestMultiplier;
            if (stats.survivedTime > CurrentSave.longestSurvivalTime)
                CurrentSave.longestSurvivalTime = stats.survivedTime;

            CurrentSave.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected override void OnBeforeSave()
        {
            // Update achievement data
            var achievementSystem = FindAnyObjectByType<AchievementSystem>();
            if (achievementSystem != null)
            {
                CurrentSave.unlockedAchievements = new List<string>(achievementSystem.GetUnlockedAchievementIds());
            }
        }

        public override void ClearSave()
        {
            // Also clear PlayerPrefs data
            PlayerPrefs.DeleteKey("Achievements");
            PlayerPrefs.DeleteKey("HighScores");
            PlayerPrefs.Save();

            base.ClearSave();
        }

        #region Unlocks Management

        public void UnlockShipSkin(string skinId)
        {
            if (CurrentSave == null) CurrentSave = new SaveData();

            if (!CurrentSave.unlockedShipSkins.Contains(skinId))
            {
                CurrentSave.unlockedShipSkins.Add(skinId);
                SaveGame();
                Debug.Log($"[SaveSystem] Unlocked ship skin: {skinId}");
            }
        }

        public bool IsShipSkinUnlocked(string skinId)
        {
            if (CurrentSave == null) return false;
            return CurrentSave.unlockedShipSkins.Contains(skinId);
        }

        public void SetSelectedShipSkin(string skinId)
        {
            if (CurrentSave == null) CurrentSave = new SaveData();
            CurrentSave.selectedShipSkin = skinId;
            SaveGame();
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Print Save Info")]
        private void DebugPrintSave()
        {
            if (CurrentSave == null)
            {
                Debug.Log("No save data");
                return;
            }

            Debug.Log($"=== Save Data ===\n" +
                      $"Games Played: {CurrentSave.totalGamesPlayed}\n" +
                      $"High Score: {CurrentSave.highScore}\n" +
                      $"Highest Level: {CurrentSave.highestLevel}\n" +
                      $"Total Enemies: {CurrentSave.totalEnemiesKilled}\n" +
                      $"Total Time: {CurrentSave.totalTimePlayed:F0}s\n" +
                      $"Last Played: {CurrentSave.lastPlayedDate}");
        }

        [ContextMenu("Debug: Clear Save")]
        private void DebugClearSave()
        {
            ClearSave();
        }

        #endregion
    }

    /// <summary>
    /// Neural Break save data structure
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Meta
        public bool hasPlayed = false;
        public string lastPlayedDate = "";
        public int saveVersion = 1;

        // Statistics - Lifetime
        public int totalGamesPlayed = 0;
        public float totalTimePlayed = 0f;
        public int totalEnemiesKilled = 0;
        public int totalBossesKilled = 0;
        public int totalXPEarned = 0;
        public int totalDamageTaken = 0;

        // Records
        public int highScore = 0;
        public int highestLevel = 0;
        public int highestCombo = 0;
        public float highestMultiplier = 1f;
        public float longestSurvivalTime = 0f;

        // Unlocks
        public List<string> unlockedAchievements = new List<string>();
        public List<string> unlockedShipSkins = new List<string>();
        public string selectedShipSkin = "default";

        // Settings preserved
        public int selectedGameMode = 0;
    }
}
