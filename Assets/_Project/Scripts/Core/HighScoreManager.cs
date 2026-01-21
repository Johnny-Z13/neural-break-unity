using UnityEngine;
using System;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages high score persistence using PlayerPrefs.
    /// Tracks high score, best level reached, longest survival, and more.
    /// </summary>
    public class HighScoreManager : MonoBehaviour
    {
        public static HighScoreManager Instance { get; private set; }

        private const string PREF_HIGH_SCORE = "NeuralBreak_HighScore";
        private const string PREF_BEST_LEVEL = "NeuralBreak_BestLevel";
        private const string PREF_LONGEST_SURVIVAL = "NeuralBreak_LongestSurvival";
        private const string PREF_MOST_KILLS = "NeuralBreak_MostKills";
        private const string PREF_HIGHEST_COMBO = "NeuralBreak_HighestCombo";
        private const string PREF_HIGHEST_MULTIPLIER = "NeuralBreak_HighestMultiplier";
        private const string PREF_GAMES_PLAYED = "NeuralBreak_GamesPlayed";
        private const string PREF_TOTAL_PLAY_TIME = "NeuralBreak_TotalPlayTime";
        private const string PREF_GAME_COMPLETED = "NeuralBreak_GameCompleted";

        // Current session high scores
        public int HighScore { get; private set; }
        public int BestLevel { get; private set; }
        public float LongestSurvival { get; private set; }
        public int MostKills { get; private set; }
        public int HighestCombo { get; private set; }
        public float HighestMultiplier { get; private set; }
        public int GamesPlayed { get; private set; }
        public float TotalPlayTime { get; private set; }
        public bool GameCompleted { get; private set; }

        // Event for new high scores
        public event Action<HighScoreType, int> OnNewHighScore;
        public event Action<HighScoreType, float> OnNewHighScoreFloat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadHighScores();
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

        private void LoadHighScores()
        {
            HighScore = PlayerPrefs.GetInt(PREF_HIGH_SCORE, 0);
            BestLevel = PlayerPrefs.GetInt(PREF_BEST_LEVEL, 0);
            LongestSurvival = PlayerPrefs.GetFloat(PREF_LONGEST_SURVIVAL, 0f);
            MostKills = PlayerPrefs.GetInt(PREF_MOST_KILLS, 0);
            HighestCombo = PlayerPrefs.GetInt(PREF_HIGHEST_COMBO, 0);
            HighestMultiplier = PlayerPrefs.GetFloat(PREF_HIGHEST_MULTIPLIER, 1f);
            GamesPlayed = PlayerPrefs.GetInt(PREF_GAMES_PLAYED, 0);
            TotalPlayTime = PlayerPrefs.GetFloat(PREF_TOTAL_PLAY_TIME, 0f);
            GameCompleted = PlayerPrefs.GetInt(PREF_GAME_COMPLETED, 0) == 1;

            Debug.Log($"[HighScoreManager] Loaded: High Score={HighScore}, Best Level={BestLevel}");
        }

        public void SaveHighScores()
        {
            PlayerPrefs.SetInt(PREF_HIGH_SCORE, HighScore);
            PlayerPrefs.SetInt(PREF_BEST_LEVEL, BestLevel);
            PlayerPrefs.SetFloat(PREF_LONGEST_SURVIVAL, LongestSurvival);
            PlayerPrefs.SetInt(PREF_MOST_KILLS, MostKills);
            PlayerPrefs.SetInt(PREF_HIGHEST_COMBO, HighestCombo);
            PlayerPrefs.SetFloat(PREF_HIGHEST_MULTIPLIER, HighestMultiplier);
            PlayerPrefs.SetInt(PREF_GAMES_PLAYED, GamesPlayed);
            PlayerPrefs.SetFloat(PREF_TOTAL_PLAY_TIME, TotalPlayTime);
            PlayerPrefs.SetInt(PREF_GAME_COMPLETED, GameCompleted ? 1 : 0);
            PlayerPrefs.Save();

            Debug.Log("[HighScoreManager] Saved high scores");
        }

        public bool ProcessGameStats(GameStats stats)
        {
            bool anyNewHighScore = false;
            GamesPlayed++;
            TotalPlayTime += stats.survivedTime;

            // Check high score
            if (stats.score > HighScore)
            {
                HighScore = stats.score;
                OnNewHighScore?.Invoke(HighScoreType.Score, stats.score);
                anyNewHighScore = true;
                Debug.Log($"[HighScoreManager] NEW HIGH SCORE: {stats.score}");
            }

            // Check best level
            if (stats.level > BestLevel)
            {
                BestLevel = stats.level;
                OnNewHighScore?.Invoke(HighScoreType.Level, stats.level);
                anyNewHighScore = true;
            }

            // Check longest survival
            if (stats.survivedTime > LongestSurvival)
            {
                LongestSurvival = stats.survivedTime;
                OnNewHighScoreFloat?.Invoke(HighScoreType.Survival, stats.survivedTime);
                anyNewHighScore = true;
            }

            // Check most kills
            if (stats.enemiesKilled > MostKills)
            {
                MostKills = stats.enemiesKilled;
                OnNewHighScore?.Invoke(HighScoreType.Kills, stats.enemiesKilled);
                anyNewHighScore = true;
            }

            // Check highest combo
            if (stats.highestCombo > HighestCombo)
            {
                HighestCombo = stats.highestCombo;
                OnNewHighScore?.Invoke(HighScoreType.Combo, stats.highestCombo);
                anyNewHighScore = true;
            }

            // Check highest multiplier
            if (stats.highestMultiplier > HighestMultiplier)
            {
                HighestMultiplier = stats.highestMultiplier;
                OnNewHighScoreFloat?.Invoke(HighScoreType.Multiplier, stats.highestMultiplier);
                anyNewHighScore = true;
            }

            // Check game completed
            if (stats.gameCompleted && !GameCompleted)
            {
                GameCompleted = true;
                Debug.Log("[HighScoreManager] Game completed for the first time!");
            }

            SaveHighScores();
            return anyNewHighScore;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            ProcessGameStats(evt.finalStats);
        }

        private void OnVictory(VictoryEvent evt)
        {
            ProcessGameStats(evt.finalStats);
        }

        public void ResetAllHighScores()
        {
            HighScore = 0;
            BestLevel = 0;
            LongestSurvival = 0f;
            MostKills = 0;
            HighestCombo = 0;
            HighestMultiplier = 1f;
            GamesPlayed = 0;
            TotalPlayTime = 0f;
            GameCompleted = false;

            SaveHighScores();
            Debug.Log("[HighScoreManager] All high scores reset");
        }

        public string GetFormattedSurvivalTime()
        {
            return FormatTime(LongestSurvival);
        }

        public string GetFormattedTotalPlayTime()
        {
            return FormatTime(TotalPlayTime);
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:D2}:{secs:D2}";
        }

        #region Debug

        [ContextMenu("Debug: Reset All High Scores")]
        private void DebugResetAll() => ResetAllHighScores();

        [ContextMenu("Debug: Print High Scores")]
        private void DebugPrint()
        {
            Debug.Log($"High Score: {HighScore}");
            Debug.Log($"Best Level: {BestLevel}");
            Debug.Log($"Longest Survival: {GetFormattedSurvivalTime()}");
            Debug.Log($"Most Kills: {MostKills}");
            Debug.Log($"Highest Combo: {HighestCombo}");
            Debug.Log($"Highest Multiplier: {HighestMultiplier:F1}x");
            Debug.Log($"Games Played: {GamesPlayed}");
            Debug.Log($"Total Play Time: {GetFormattedTotalPlayTime()}");
            Debug.Log($"Game Completed: {GameCompleted}");
        }

        #endregion
    }

    public enum HighScoreType
    {
        Score,
        Level,
        Survival,
        Kills,
        Combo,
        Multiplier
    }
}
