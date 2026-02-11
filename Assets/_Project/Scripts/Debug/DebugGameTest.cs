using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Testing
{
    /// <summary>
    /// Simple debug script to test game functionality.
    /// </summary>
    public class DebugGameTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool m_spawnTestEnemy = false; // Disabled - enemies spawn naturally
        [SerializeField] private float m_spawnDelay = 3f; // Increased delay

        [Header("Test Mode - Spawn All Enemy Types")]
        [SerializeField] private bool m_testModeEnabled = false; // DISABLED - use GameMode.Test instead
        [SerializeField] private float m_testModeSpawnInterval = 0.5f;
        [SerializeField] private bool m_testModeSpawnAllOnStart = false; // DISABLED - use GameMode.Test instead

        private bool m_hasSpawned = false;
        private float m_timer = 0f;
        private bool m_testModeSpawned = false;
        private float m_testModeTimer = 0f;
        private int m_testModeEnemyIndex = 0;

        private void Awake()
        {
            UnityEngine.Debug.Log("[DebugGameTest] Awake called");
        }

        private void Start()
        {
            UnityEngine.Debug.Log("[DebugGameTest] Start called");
        }

        private void Update()
        {
            // Test mode: spawn all enemy types quickly
            if (m_testModeEnabled && !m_testModeSpawned)
            {
                UpdateTestMode();
            }

            // Legacy single enemy spawn
            if (!m_spawnTestEnemy || m_hasSpawned) return;

            m_timer += Time.deltaTime;

            if (m_timer >= m_spawnDelay)
            {
                UnityEngine.Debug.Log($"[DebugGameTest] Timer reached! Spawning test enemy. Time.deltaTime: {Time.deltaTime}");
                SpawnTestEnemy();
                m_hasSpawned = true;
            }
        }

        private void UpdateTestMode()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner == null) return;

            // Spawn all at once on start
            if (m_testModeSpawnAllOnStart && m_testModeEnemyIndex == 0)
            {
                SpawnAllEnemyTypes(spawner);
                m_testModeSpawned = true;
                return;
            }

            // Or spawn one by one with interval
            m_testModeTimer += Time.deltaTime;
            if (m_testModeTimer >= m_testModeSpawnInterval)
            {
                m_testModeTimer = 0f;
                SpawnNextEnemyType(spawner);
            }
        }

        private void SpawnAllEnemyTypes(EnemySpawner spawner)
        {
            UnityEngine.Debug.Log("[DebugGameTest] TEST MODE: Spawning all enemy types!");

            // Spawn one of each type in a circle around player
            EnemyType[] allTypes = new EnemyType[]
            {
                EnemyType.DataMite,
                EnemyType.ScanDrone,
                EnemyType.Fizzer,
                EnemyType.UFO,
                EnemyType.ChaosWorm,
                EnemyType.VoidSphere,
                EnemyType.CrystalShard
                // Boss excluded from auto-spawn for safety
            };

            float radius = 6f;
            for (int i = 0; i < allTypes.Length; i++)
            {
                float angle = (i / (float)allTypes.Length) * Mathf.PI * 2f;
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                var enemy = spawner.SpawnEnemyOfType(allTypes[i], pos);
                if (enemy != null)
                {
                    UnityEngine.Debug.Log($"[DebugGameTest] Spawned {allTypes[i]} at {pos}");
                }
            }

            UnityEngine.Debug.Log($"[DebugGameTest] TEST MODE: Spawned {allTypes.Length} enemy types");
        }

        private void SpawnNextEnemyType(EnemySpawner spawner)
        {
            EnemyType[] allTypes = new EnemyType[]
            {
                EnemyType.DataMite,
                EnemyType.ScanDrone,
                EnemyType.Fizzer,
                EnemyType.UFO,
                EnemyType.ChaosWorm,
                EnemyType.VoidSphere,
                EnemyType.CrystalShard
            };

            if (m_testModeEnemyIndex >= allTypes.Length)
            {
                m_testModeSpawned = true;
                return;
            }

            float angle = (m_testModeEnemyIndex / (float)allTypes.Length) * Mathf.PI * 2f;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;

            var type = allTypes[m_testModeEnemyIndex];
            var enemy = spawner.SpawnEnemyOfType(type, pos);
            UnityEngine.Debug.Log($"[DebugGameTest] TEST MODE: Spawned {type}");

            m_testModeEnemyIndex++;
        }

        private void SpawnTestEnemy()
        {
            // Find spawner
            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                UnityEngine.Debug.LogError("[DebugGameTest] No EnemySpawner found!");
                return;
            }

            // Try to spawn
            var enemy = spawner.SpawnEnemyOfType(EnemyType.DataMite, new Vector2(3, 3));
            if (enemy != null)
            {
                UnityEngine.Debug.Log($"[DebugGameTest] Successfully spawned enemy at {enemy.transform.position}");
                
                // Also give it a sprite
                var sr = enemy.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = GameSetup.CircleSprite;
                    UnityEngine.Debug.Log("[DebugGameTest] Applied sprite to enemy");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("[DebugGameTest] Failed to spawn enemy!");
            }
        }

        // DISABLED: This was overlapping with game UI
        // If you need debug info, use the Unity Profiler or Console logs instead
        /*
        private void OnGUI()
        {
            // Show debug info on screen
            GUI.Label(new Rect(10, 10, 400, 20), $"GameTime: {Time.time:F2} | Timer: {m_timer:F2}");
            GUI.Label(new Rect(10, 30, 400, 20), $"HasSpawned: {m_hasSpawned} | deltaTime: {Time.deltaTime:F4}");

            if (GameManager.Instance != null)
            {
                GUI.Label(new Rect(10, 50, 400, 20), $"IsPlaying: {GameManager.Instance.IsPlaying} | State: {GameManager.Instance.CurrentState}");
                GUI.Label(new Rect(10, 70, 400, 20), $"SurvivedTime: {GameManager.Instance.Stats.survivedTime:F2}");
            }

            // Show spawner info
            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                GUI.Label(new Rect(10, 90, 400, 20), $"ActiveEnemies: {spawner.ActiveEnemyCount}");
            }

            // Show player health
            var playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                GUI.Label(new Rect(10, 110, 400, 20), $"Health: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} | Invuln: {playerHealth.IsInvulnerable}");
            }

            // Show weapon info
            var weapon = FindFirstObjectByType<WeaponSystem>();
            if (weapon != null)
            {
                bool hasPrefab = typeof(WeaponSystem).GetField("m_projectilePrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(weapon) != null;
                GUI.Label(new Rect(10, 130, 400, 20), $"Weapon: Heat={weapon.Heat:F0} | HasPrefab: {hasPrefab}");
            }
        }
        */
    }
}
