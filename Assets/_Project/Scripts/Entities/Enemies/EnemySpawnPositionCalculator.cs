using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Config;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Calculates spawn positions for enemies around the player.
    /// Ensures minimum/maximum distance from player and checks for overlapping enemies.
    /// </summary>
    public class EnemySpawnPositionCalculator
    {
        // Configuration
        private readonly Transform _playerTarget;
        private readonly float _minEnemySpacing;
        private readonly int _maxSpawnAttempts;

        // Config access
        private float ArenaRadius => ConfigProvider.Player.arenaRadius;
        private float MinSpawnDistance => ConfigProvider.Spawning.minSpawnDistance;
        private float MaxSpawnDistance => ConfigProvider.Spawning.maxSpawnDistance;

        public EnemySpawnPositionCalculator(
            Transform playerTarget,
            float minEnemySpacing = 2.0f,
            int maxSpawnAttempts = 10)
        {
            _playerTarget = playerTarget;
            _minEnemySpacing = minEnemySpacing;
            _maxSpawnAttempts = maxSpawnAttempts;
        }

        /// <summary>
        /// Get a spawn position that doesn't overlap with existing enemies
        /// </summary>
        public Vector2 GetSpawnPosition(IReadOnlyList<EnemyBase> activeEnemies)
        {
            // Try multiple times to find a non-overlapping position
            for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
            {
                Vector2 candidatePos = GetRandomSpawnPosition();

                if (!IsPositionOccupied(candidatePos, activeEnemies))
                {
                    return candidatePos;
                }
            }

            // Fallback: return a random position even if overlapping
            return GetRandomSpawnPosition();
        }

        /// <summary>
        /// Get a random spawn position around the player, respecting min/max distance
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            if (_playerTarget == null)
            {
                return Random.insideUnitCircle * ArenaRadius;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(MinSpawnDistance, MaxSpawnDistance);

            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 spawnPos = (Vector2)_playerTarget.position + offset;

            // Clamp to arena bounds
            spawnPos.x = Mathf.Clamp(spawnPos.x, -ArenaRadius, ArenaRadius);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -ArenaRadius, ArenaRadius);

            return spawnPos;
        }

        /// <summary>
        /// Get a spawn position at the edge of the arena (used for bosses)
        /// </summary>
        public Vector2 GetEdgeSpawnPosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ArenaRadius * 0.95f;
        }

        /// <summary>
        /// Check if a position is too close to any active enemy
        /// </summary>
        private bool IsPositionOccupied(Vector2 position, IReadOnlyList<EnemyBase> activeEnemies)
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsActive) continue;

                float distSq = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                if (distSq < _minEnemySpacing * _minEnemySpacing)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Draw debug gizmos for spawn ranges
        /// </summary>
        public void DrawGizmos()
        {
            // Use config values if available, fallback to defaults for editor preview
            float arenaRadius = ConfigProvider.Player?.arenaRadius ?? 25f;
            float minDist = ConfigProvider.Spawning?.minSpawnDistance ?? 8f;
            float maxDist = ConfigProvider.Spawning?.maxSpawnDistance ?? 20f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Vector3.zero, arenaRadius);

            if (_playerTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerTarget.position, minDist);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTarget.position, maxDist);
            }
        }
    }
}
