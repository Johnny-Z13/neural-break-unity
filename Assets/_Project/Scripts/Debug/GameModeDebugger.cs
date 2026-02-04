using UnityEngine;
using NeuralBreak.Core;
using System.IO;
using Z13.Core;

namespace NeuralBreak.Testing
{
    /// <summary>
    /// Dumps game mode debug info to a file for troubleshooting
    /// </summary>
    public class GameModeDebugger : MonoBehaviour
    {
        private string m_logPath;

        private void Awake()
        {
            m_logPath = Path.Combine(Application.dataPath, "..", "gamemode_debug.txt");
            File.WriteAllText(m_logPath, $"=== GAME MODE DEBUGGER STARTED ===\n");
            Log("GameModeDebugger Awake");

            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Log($"[EVENT] GameStartedEvent received! Mode = {evt.mode}");

            if (GameManager.Instance != null)
            {
                Log($"[GameManager] CurrentMode = {GameManager.Instance.CurrentMode}");
                Log($"[GameManager] IsPlaying = {GameManager.Instance.IsPlaying}");
            }

            var levelMgr = LevelManager.Instance;
            if (levelMgr != null)
            {
                Log($"[LevelManager] CurrentLevel = {levelMgr.CurrentLevel}");
                Log($"[LevelManager] CurrentLevelName = {levelMgr.CurrentLevelName}");
            }

            var spawner = FindFirstObjectByType<Entities.EnemySpawner>();
            if (spawner != null)
            {
                Log($"[EnemySpawner] SpawningEnabled = {spawner.SpawningEnabled}");
                Log($"[EnemySpawner] ActiveEnemyCount = {spawner.ActiveEnemyCount}");
            }
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            Log($"[EVENT] LevelStartedEvent received! Level = {evt.levelNumber}, Name = {evt.levelName}");
        }

        private void Log(string message)
        {
            string timestamped = $"[{Time.time:F2}s] {message}\n";
            File.AppendAllText(m_logPath, timestamped);
            Debug.Log($"[GameModeDebugger] {message}");
        }

        [ContextMenu("Check Current State")]
        private void CheckState()
        {
            Log("=== MANUAL STATE CHECK ===");
            OnGameStarted(new GameStartedEvent { mode = GameManager.Instance?.CurrentMode ?? GameMode.Arcade });
        }
    }
}
