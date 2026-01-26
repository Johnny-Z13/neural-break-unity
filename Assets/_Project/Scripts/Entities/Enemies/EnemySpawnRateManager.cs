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
        private float _dataMiteRate;
        private float _scanDroneRate;
        private float _fizzerRate;
        private float _ufoRate;
        private float _chaosWormRate;
        private float _voidSphereRate;
        private float _crystalShardRate;
        private float _bossRate;

        // Spawn timers
        private float _dataMiteTimer;
        private float _scanDroneTimer;
        private float _fizzerTimer;
        private float _ufoTimer;
        private float _chaosWormTimer;
        private float _voidSphereTimer;
        private float _crystalShardTimer;
        private float _bossTimer;

        public EnemySpawnRateManager()
        {
            // Initialize spawn rates to disabled - LevelManager will set proper rates when game starts
            _dataMiteRate = float.PositiveInfinity;
            _scanDroneRate = float.PositiveInfinity;
            _fizzerRate = float.PositiveInfinity;
            _ufoRate = float.PositiveInfinity;
            _chaosWormRate = float.PositiveInfinity;
            _voidSphereRate = float.PositiveInfinity;
            _crystalShardRate = float.PositiveInfinity;
            _bossRate = float.PositiveInfinity;
        }

        /// <summary>
        /// Set all spawn rates at once
        /// </summary>
        public void SetSpawnRates(float dataMite, float scanDrone, float fizzer, float ufo,
            float chaosWorm, float voidSphere, float crystalShard, float boss)
        {
            _dataMiteRate = dataMite;
            _scanDroneRate = scanDrone;
            _fizzerRate = fizzer;
            _ufoRate = ufo;
            _chaosWormRate = chaosWorm;
            _voidSphereRate = voidSphere;
            _crystalShardRate = crystalShard;
            _bossRate = boss;

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
                case EnemyType.DataMite: _dataMiteRate = rate; break;
                case EnemyType.ScanDrone: _scanDroneRate = rate; break;
                case EnemyType.Fizzer: _fizzerRate = rate; break;
                case EnemyType.UFO: _ufoRate = rate; break;
                case EnemyType.ChaosWorm: _chaosWormRate = rate; break;
                case EnemyType.VoidSphere: _voidSphereRate = rate; break;
                case EnemyType.CrystalShard: _crystalShardRate = rate; break;
                case EnemyType.Boss: _bossRate = rate; break;
            }
        }

        /// <summary>
        /// Multiply all spawn rates by a factor (for difficulty scaling)
        /// </summary>
        public void MultiplySpawnRates(float multiplier)
        {
            _dataMiteRate *= multiplier;
            _scanDroneRate *= multiplier;
            _chaosWormRate *= multiplier;
            _voidSphereRate *= multiplier;
            _crystalShardRate *= multiplier;
            _ufoRate *= multiplier;
        }

        /// <summary>
        /// Reset all spawn timers to zero
        /// </summary>
        public void ResetTimers()
        {
            _dataMiteTimer = 0f;
            _scanDroneTimer = 0f;
            _fizzerTimer = 0f;
            _ufoTimer = 0f;
            _chaosWormTimer = 0f;
            _voidSphereTimer = 0f;
            _crystalShardTimer = 0f;
            _bossTimer = 0f;
        }

        /// <summary>
        /// Update timers and check which enemies should spawn.
        /// Returns list of enemy types ready to spawn.
        /// </summary>
        public EnemySpawnRequest[] UpdateAndGetReadySpawns(float deltaTime)
        {
            var readySpawns = new System.Collections.Generic.List<EnemySpawnRequest>();

            // DataMite
            _dataMiteTimer += deltaTime;
            if (_dataMiteTimer >= _dataMiteRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.DataMite));
                _dataMiteTimer = 0f;
            }

            // ScanDrone
            _scanDroneTimer += deltaTime;
            if (_scanDroneTimer >= _scanDroneRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.ScanDrone));
                _scanDroneTimer = 0f;
            }

            // Fizzer (conditional - only when enabled)
            if (_fizzerRate < float.PositiveInfinity)
            {
                _fizzerTimer += deltaTime;
                if (_fizzerTimer >= _fizzerRate)
                {
                    readySpawns.Add(new EnemySpawnRequest(EnemyType.Fizzer));
                    _fizzerTimer = 0f;
                }
            }

            // UFO
            _ufoTimer += deltaTime;
            if (_ufoTimer >= _ufoRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.UFO));
                _ufoTimer = 0f;
            }

            // ChaosWorm
            _chaosWormTimer += deltaTime;
            if (_chaosWormTimer >= _chaosWormRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.ChaosWorm));
                _chaosWormTimer = 0f;
            }

            // VoidSphere
            _voidSphereTimer += deltaTime;
            if (_voidSphereTimer >= _voidSphereRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.VoidSphere));
                _voidSphereTimer = 0f;
            }

            // CrystalShard
            _crystalShardTimer += deltaTime;
            if (_crystalShardTimer >= _crystalShardRate)
            {
                readySpawns.Add(new EnemySpawnRequest(EnemyType.CrystalShard));
                _crystalShardTimer = 0f;
            }

            // Boss (level-based)
            if (_bossRate < float.PositiveInfinity)
            {
                _bossTimer += deltaTime;
                if (_bossTimer >= _bossRate)
                {
                    readySpawns.Add(new EnemySpawnRequest(EnemyType.Boss, useEdgeSpawn: true));
                    _bossTimer = 0f;
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
