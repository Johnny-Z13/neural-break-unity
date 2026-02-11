using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages spawn rates and timers for all 8 enemy types.
    /// Handles difficulty scaling and spawn interval management.
    /// </summary>
    public class EnemySpawnRateManager
    {
        // Spawn rates (time between spawns in seconds)
        private float m_dataMiteRate;
        private float m_scanDroneRate;
        private float m_fizzerRate;
        private float m_ufoRate;
        private float m_chaosWormRate;
        private float m_voidSphereRate;
        private float m_crystalShardRate;
        private float m_bossRate;

        // Spawn timers
        private float m_dataMiteTimer;
        private float m_scanDroneTimer;
        private float m_fizzerTimer;
        private float m_ufoTimer;
        private float m_chaosWormTimer;
        private float m_voidSphereTimer;
        private float m_crystalShardTimer;
        private float m_bossTimer;

        public EnemySpawnRateManager()
        {
            // Initialize spawn rates to disabled - LevelManager will set proper rates when game starts
            m_dataMiteRate = float.PositiveInfinity;
            m_scanDroneRate = float.PositiveInfinity;
            m_fizzerRate = float.PositiveInfinity;
            m_ufoRate = float.PositiveInfinity;
            m_chaosWormRate = float.PositiveInfinity;
            m_voidSphereRate = float.PositiveInfinity;
            m_crystalShardRate = float.PositiveInfinity;
            m_bossRate = float.PositiveInfinity;
        }

        /// <summary>
        /// Set all spawn rates at once
        /// </summary>
        public void SetSpawnRates(float dataMite, float scanDrone, float fizzer, float ufo,
            float chaosWorm, float voidSphere, float crystalShard, float boss)
        {
            m_dataMiteRate = dataMite;
            m_scanDroneRate = scanDrone;
            m_fizzerRate = fizzer;
            m_ufoRate = ufo;
            m_chaosWormRate = chaosWorm;
            m_voidSphereRate = voidSphere;
            m_crystalShardRate = crystalShard;
            m_bossRate = boss;

            Debug.Log($"[EnemySpawnRateManager] SetSpawnRates called:");
            Debug.Log($"  DataMite={dataMite}, ScanDrone={scanDrone}, Fizzer={fizzer}, UFO={ufo}");
            Debug.Log($"  ChaosWorm={chaosWorm}, VoidSphere={voidSphere}, CrystalShard={crystalShard}, Boss={boss}");
        }

        /// <summary>
        /// Set spawn rate for a specific enemy type
        /// </summary>
        public void SetEnemySpawnRate(EnemyType type, float rate)
        {
            switch (type)
            {
                case EnemyType.DataMite: m_dataMiteRate = rate; break;
                case EnemyType.ScanDrone: m_scanDroneRate = rate; break;
                case EnemyType.Fizzer: m_fizzerRate = rate; break;
                case EnemyType.UFO: m_ufoRate = rate; break;
                case EnemyType.ChaosWorm: m_chaosWormRate = rate; break;
                case EnemyType.VoidSphere: m_voidSphereRate = rate; break;
                case EnemyType.CrystalShard: m_crystalShardRate = rate; break;
                case EnemyType.Boss: m_bossRate = rate; break;
            }
        }

        /// <summary>
        /// Multiply all spawn rates by a factor (for difficulty scaling)
        /// </summary>
        public void MultiplySpawnRates(float multiplier)
        {
            m_dataMiteRate *= multiplier;
            m_scanDroneRate *= multiplier;
            m_chaosWormRate *= multiplier;
            m_voidSphereRate *= multiplier;
            m_crystalShardRate *= multiplier;
            m_ufoRate *= multiplier;
        }

        /// <summary>
        /// Reset all spawn timers to zero
        /// </summary>
        public void ResetTimers()
        {
            m_dataMiteTimer = 0f;
            m_scanDroneTimer = 0f;
            m_fizzerTimer = 0f;
            m_ufoTimer = 0f;
            m_chaosWormTimer = 0f;
            m_voidSphereTimer = 0f;
            m_crystalShardTimer = 0f;
            m_bossTimer = 0f;
        }

        /// <summary>
        /// Update timers and check which enemies should spawn.
        /// Returns list of enemy types ready to spawn.
        /// </summary>
        public EnemySpawnRequest[] UpdateAndGetReadySpawns(float deltaTime)
        {
            var readySpawns = new System.Collections.Generic.List<EnemySpawnRequest>();

            // DataMite
            m_dataMiteTimer += deltaTime;
            if (m_dataMiteTimer >= m_dataMiteRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.DataMite));
                m_dataMiteTimer = 0f;
            }

            // ScanDrone
            m_scanDroneTimer += deltaTime;
            if (m_scanDroneTimer >= m_scanDroneRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.ScanDrone));
                m_scanDroneTimer = 0f;
            }

            // Fizzer (conditional - only when enabled)
            if (m_fizzerRate < float.PositiveInfinity)
            {
                m_fizzerTimer += deltaTime;
                if (m_fizzerTimer >= m_fizzerRate)
                {
                    readySpawns.Add(new EnemySpawnRequest(EnemyType.Fizzer));
                    m_fizzerTimer = 0f;
                }
            }

            // UFO
            m_ufoTimer += deltaTime;
            if (m_ufoTimer >= m_ufoRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.UFO));
                m_ufoTimer = 0f;
            }

            // ChaosWorm
            m_chaosWormTimer += deltaTime;
            if (m_chaosWormTimer >= m_chaosWormRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.ChaosWorm));
                m_chaosWormTimer = 0f;
            }

            // VoidSphere
            m_voidSphereTimer += deltaTime;
            if (m_voidSphereTimer >= m_voidSphereRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.VoidSphere));
                m_voidSphereTimer = 0f;
            }

            // CrystalShard
            m_crystalShardTimer += deltaTime;
            if (m_crystalShardTimer >= m_crystalShardRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.CrystalShard));
                m_crystalShardTimer = 0f;
            }

            // Boss (level-based)
            if (m_bossRate < float.PositiveInfinity)
            {
                m_bossTimer += deltaTime;
                if (m_bossTimer >= m_bossRate)
                {
                    readySpawns.Add(new EnemySpawnRequest(EnemyType.Boss, useEdgeSpawn: true));
                    m_bossTimer = 0f;
                }
            }

            return readySpawns.ToArray();
        }
    }

    /// <summary>
    /// Represents a request to spawn an enemy
    /// </summary>
    public struct EnemySpawnRequest
    {
        public EnemyType EnemyType;
        public bool UseEdgeSpawn;

        public EnemySpawnRequest(EnemyType enemyType, bool useEdgeSpawn = false)
        {
            EnemyType = enemyType;
            UseEdgeSpawn = useEdgeSpawn;
        }
    }
}
