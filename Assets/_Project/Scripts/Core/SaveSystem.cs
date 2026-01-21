using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages saving and loading game state.
    /// Handles player progress, unlocks, and statistics.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool _autoSaveOnGameOver = true;
        [SerializeField] private float _autoSaveInterval = 60f;

        public SaveData CurrentSave { get; private set; }
        public bool HasSaveData => CurrentSave != null && CurrentSave.hasPlayed;

        private const string SAVE_FILE = "neural_break_save.json";
        private float _lastAutoSave;

        public event Action<SaveData> OnSaveLoaded;
        public event Action OnSaveCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGame();
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

        private void Update()
        {
            // Auto-save periodically during gameplay
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                if (Time.time - _lastAutoSave > _autoSaveInterval)
                {
                    SaveGame();
                    _lastAutoSave = Time.time;
                }
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_autoSaveOnGameOver)
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
            {
                CurrentSave.highScore = stats.score;
            }
            if (stats.level > CurrentSave.highestLevel)
            {
                CurrentSave.highestLevel = stats.level;
            }
            if (stats.highestCombo > CurrentSave.highestCombo)
            {
                CurrentSave.highestCombo = stats.highestCombo;
            }
            if (stats.highestMultiplier > CurrentSave.highestMultiplier)
            {
                CurrentSave.highestMultiplier = stats.highestMultiplier;
            }
            if (stats.survivedTime > CurrentSave.longestSurvivalTime)
            {
                CurrentSave.longestSurvivalTime = stats.survivedTime;
            }

            CurrentSave.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Save current game state to file
        /// </summary>
        public void SaveGame()
        {
            if (CurrentSave == null)
            {
                CurrentSave = new SaveData();
            }

            // Update achievement data
            if (AchievementSystem.Instance != null)
            {
                CurrentSave.unlockedAchievements = new List<string>(AchievementSystem.Instance.GetUnlockedAchievementIds());
            }

            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                string path = GetSavePath();
                File.WriteAllText(path, json);
                Debug.Log($"[SaveSystem] Game saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load game state from file
        /// </summary>
        public void LoadGame()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log("[SaveSystem] No save file found, creating new save");
                CurrentSave = new SaveData();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Game loaded from {path}");

                OnSaveLoaded?.Invoke(CurrentSave);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                CurrentSave = new SaveData();
            }
        }

        /// <summary>
        /// Delete save data
        /// </summary>
        public void ClearSave()
        {
            string path = GetSavePath();

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Also clear PlayerPrefs data
            PlayerPrefs.DeleteKey("Achievements");
            PlayerPrefs.DeleteKey("HighScores");
            PlayerPrefs.Save();

            CurrentSave = new SaveData();
            OnSaveCleared?.Invoke();

            Debug.Log("[SaveSystem] Save data cleared");
        }

        /// <summary>
        /// Get full path to save file
        /// </summary>
        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE);
        }

        /// <summary>
        /// Export save as string (for cloud backup)
        /// </summary>
        public string ExportSave()
        {
            if (CurrentSave == null) return "";
            return JsonUtility.ToJson(CurrentSave);
        }

        /// <summary>
        /// Import save from string (for cloud restore)
        /// </summary>
        public bool ImportSave(string json)
        {
            try
            {
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
                SaveGame();
                OnSaveLoaded?.Invoke(CurrentSave);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Import failed: {e.Message}");
                return false;
            }
        }

        #region Unlocks Management

        /// <summary>
        /// Unlock a ship skin
        /// </summary>
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

        /// <summary>
        /// Check if ship skin is unlocked
        /// </summary>
        public bool IsShipSkinUnlocked(string skinId)
        {
            if (CurrentSave == null) return false;
            return CurrentSave.unlockedShipSkins.Contains(skinId);
        }

        /// <summary>
        /// Set selected ship skin
        /// </summary>
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
    /// Serializable save data structure
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
