using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.Utils;

namespace NeuralBreak.Testing
{
    /// <summary>
    /// Simple debug script to test game functionality.
    /// </summary>
    public class DebugGameTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool _spawnTestEnemy = false; // Disabled - enemies spawn naturally
        [SerializeField] private float _spawnDelay = 3f; // Increased delay

        [Header("Test Mode - Spawn All Enemy Types")]
        [SerializeField] private bool _testModeEnabled = true;
        [SerializeField] private float _testModeSpawnInterval = 0.5f;
        [SerializeField] private bool _testModeSpawnAllOnStart = true;

        private bool _hasSpawned = false;
        private float _timer = 0f;
        private bool _testModeSpawned = false;
        private float _testModeTimer = 0f;
        private int _testModeEnemyIndex = 0;

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
            if (_testModeEnabled && !_testModeSpawned)
            {
                UpdateTestMode();
            }

            // Legacy single enemy spawn
            if (!_spawnTestEnemy || _hasSpawned) return;

            _timer += Time.deltaTime;

            if (_timer >= _spawnDelay)
            {
                UnityEngine.Debug.Log($"[DebugGameTest] Timer reached! Spawning test enemy. Time.deltaTime: {Time.deltaTime}");
                SpawnTestEnemy();
                _hasSpawned = true;
            }
        }

        private void UpdateTestMode()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner == null) return;

            // Spawn all at once on start
            if (_testModeSpawnAllOnStart && _testModeEnemyIndex == 0)
            {
                SpawnAllEnemyTypes(spawner);
                _testModeSpawned = true;
                return;
            }

            // Or spawn one by one with interval
            _testModeTimer += Time.deltaTime;
            if (_testModeTimer >= _testModeSpawnInterval)
            {
                _testModeTimer = 0f;
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

            if (_testModeEnemyIndex >= allTypes.Length)
            {
                _testModeSpawned = true;
                return;
            }

            float angle = (_testModeEnemyIndex / (float)allTypes.Length) * Mathf.PI * 2f;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;

            var type = allTypes[_testModeEnemyIndex];
            var enemy = spawner.SpawnEnemyOfType(type, pos);
            UnityEngine.Debug.Log($"[DebugGameTest] TEST MODE: Spawned {type}");

            _testModeEnemyIndex++;
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

        private void OnGUI()
        {
            // Show debug info on screen
            GUI.Label(new Rect(10, 10, 400, 20), $"GameTime: {Time.time:F2} | Timer: {_timer:F2}");
            GUI.Label(new Rect(10, 30, 400, 20), $"HasSpawned: {_hasSpawned} | deltaTime: {Time.deltaTime:F4}");

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
                bool hasPrefab = typeof(WeaponSystem).GetField("_projectilePrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(weapon) != null;
                GUI.Label(new Rect(10, 130, 400, 20), $"Weapon: Heat={weapon.Heat:F0} | HasPrefab: {hasPrefab}");
            }
        }
    }
}
