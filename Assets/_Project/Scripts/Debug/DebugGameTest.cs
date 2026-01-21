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

        private bool _hasSpawned = false;
        private float _timer = 0f;

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
            if (!_spawnTestEnemy || _hasSpawned) return;

            _timer += Time.deltaTime;

            if (_timer >= _spawnDelay)
            {
                UnityEngine.Debug.Log($"[DebugGameTest] Timer reached! Spawning test enemy. Time.deltaTime: {Time.deltaTime}");
                SpawnTestEnemy();
                _hasSpawned = true;
            }
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
