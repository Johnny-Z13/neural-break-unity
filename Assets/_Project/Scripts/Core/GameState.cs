using UnityEngine;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Game state enum - matches TypeScript GameStateType
    /// </summary>
    public enum GameStateType
    {
        StartScreen,
        Playing,
        Paused,
        GameOver,
        RogueChoice,
        Victory
    }

    /// <summary>
    /// Game mode enum - matches TypeScript GameMode
    /// </summary>
    public enum GameMode
    {
        Arcade,
        Rogue,
        Test
    }

    /// <summary>
    /// Tracks all game statistics for scoring and end-game display
    /// </summary>
    [System.Serializable]
    public class GameStats
    {
        public int score;
        public float survivedTime;
        public int level = 1;
        public int enemiesKilled;

        // Kill counts by enemy type
        public int dataMinersKilled;
        public int scanDronesKilled;
        public int chaosWormsKilled;
        public int voidSpheresKilled;
        public int crystalSwarmsKilled;
        public int fizzersKilled;
        public int ufosKilled;
        public int bossesKilled;

        public int damageTaken;
        public int totalXP;
        public int highestCombo;
        public float highestMultiplier = 1f;
        public bool gameCompleted;

        public void Reset()
        {
            score = 0;
            survivedTime = 0f;
            level = 1;
            enemiesKilled = 0;
            dataMinersKilled = 0;
            scanDronesKilled = 0;
            chaosWormsKilled = 0;
            voidSpheresKilled = 0;
            crystalSwarmsKilled = 0;
            fizzersKilled = 0;
            ufosKilled = 0;
            bossesKilled = 0;
            damageTaken = 0;
            totalXP = 0;
            highestCombo = 0;
            highestMultiplier = 1f;
            gameCompleted = false;
        }
    }

    /// <summary>
    /// Score values for each enemy type - matches TypeScript kill point values
    /// </summary>
    public static class ScoreValues
    {
        public const int DataMite = 100;
        public const int ScanDrone = 250;
        public const int ChaosWorm = 500;
        public const int VoidSphere = 1000;
        public const int CrystalSwarm = 750;
        public const int Fizzer = 200;
        public const int UFO = 1500;
        public const int Boss = 5000;
    }

    /// <summary>
    /// XP values for each enemy type
    /// </summary>
    public static class XPValues
    {
        public const int DataMite = 1;
        public const int ScanDrone = 3;
        public const int ChaosWorm = 5;
        public const int VoidSphere = 10;
        public const int CrystalSwarm = 8;
        public const int Fizzer = 2;
        public const int UFO = 15;
        public const int Boss = 40;
    }
}
