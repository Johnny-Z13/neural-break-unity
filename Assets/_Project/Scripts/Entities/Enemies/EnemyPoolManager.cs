using UnityEngine;
using NeuralBreak.Config;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages object pools for all 8 enemy types.
    /// Handles pool initialization, cleanup, and Get/Return operations.
    /// </summary>
    public class EnemyPoolManager
    {
        // Enemy prefabs
        private readonly DataMite _dataMitePrefab;
        private readonly ScanDrone _scanDronePrefab;
        private readonly Fizzer _fizzerPrefab;
        private readonly UFO _ufoPrefab;
        private readonly ChaosWorm _chaosWormPrefab;
        private readonly VoidSphere _voidSpherePrefab;
        private readonly CrystalShard _crystalShardPrefab;
        private readonly Boss _bossPrefab;

        // Parent container
        private readonly Transform _container;

        // Object pools
        private ObjectPool<DataMite> _dataMitePool;
        private ObjectPool<ScanDrone> _scanDronePool;
        private ObjectPool<Fizzer> _fizzerPool;
        private ObjectPool<UFO> _ufoPool;
        private ObjectPool<ChaosWorm> _chaosWormPool;
        private ObjectPool<VoidSphere> _voidSpherePool;
        private ObjectPool<CrystalShard> _crystalShardPool;
        private ObjectPool<Boss> _bossPool;

        // Config access
        private GameBalanceConfig Balance => ConfigProvider.Balance;

        public EnemyPoolManager(
            DataMite dataMitePrefab,
            ScanDrone scanDronePrefab,
            Fizzer fizzerPrefab,
            UFO ufoPrefab,
            ChaosWorm chaosWormPrefab,
            VoidSphere voidSpherePrefab,
            CrystalShard crystalShardPrefab,
            Boss bossPrefab,
            Transform container)
        {
            _dataMitePrefab = dataMitePrefab;
            _scanDronePrefab = scanDronePrefab;
            _fizzerPrefab = fizzerPrefab;
            _ufoPrefab = ufoPrefab;
            _chaosWormPrefab = chaosWormPrefab;
            _voidSpherePrefab = voidSpherePrefab;
            _crystalShardPrefab = crystalShardPrefab;
            _bossPrefab = bossPrefab;
            _container = container;

            InitializePools();
        }

        private void InitializePools()
        {
            // Initialize all pools with config-driven pool sizes
            if (_dataMitePrefab != null)
            {
                _dataMitePool = new ObjectPool<DataMite>(_dataMitePrefab, _container,
                    Balance.dataMite.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_scanDronePrefab != null)
            {
                _scanDronePool = new ObjectPool<ScanDrone>(_scanDronePrefab, _container,
                    Balance.scanDrone.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_fizzerPrefab != null)
            {
                _fizzerPool = new ObjectPool<Fizzer>(_fizzerPrefab, _container,
                    Balance.fizzer.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_ufoPrefab != null)
            {
                _ufoPool = new ObjectPool<UFO>(_ufoPrefab, _container,
                    Balance.ufo.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_chaosWormPrefab != null)
            {
                _chaosWormPool = new ObjectPool<ChaosWorm>(_chaosWormPrefab, _container,
                    Balance.chaosWorm.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_voidSpherePrefab != null)
            {
                _voidSpherePool = new ObjectPool<VoidSphere>(_voidSpherePrefab, _container,
                    Balance.voidSphere.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_crystalShardPrefab != null)
            {
                _crystalShardPool = new ObjectPool<CrystalShard>(_crystalShardPrefab, _container,
                    Balance.crystalShard.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (_bossPrefab != null)
            {
                _bossPool = new ObjectPool<Boss>(_bossPrefab, _container,
                    Balance.boss.poolSize, onReturn: e => e.OnReturnToPool());
            }
        }

        /// <summary>
        /// Get an enemy from the appropriate pool by type
        /// </summary>
        public T GetEnemy<T>(EnemyType type, Vector2 position) where T : EnemyBase
        {
            switch (type)
            {
                case EnemyType.DataMite when _dataMitePool != null:
                    return _dataMitePool.Get(position, Quaternion.identity) as T;

                case EnemyType.ScanDrone when _scanDronePool != null:
                    return _scanDronePool.Get(position, Quaternion.identity) as T;

                case EnemyType.Fizzer when _fizzerPool != null:
                    return _fizzerPool.Get(position, Quaternion.identity) as T;

                case EnemyType.UFO when _ufoPool != null:
                    return _ufoPool.Get(position, Quaternion.identity) as T;

                case EnemyType.ChaosWorm when _chaosWormPool != null:
                    return _chaosWormPool.Get(position, Quaternion.identity) as T;

                case EnemyType.VoidSphere when _voidSpherePool != null:
                    return _voidSpherePool.Get(position, Quaternion.identity) as T;

                case EnemyType.CrystalShard when _crystalShardPool != null:
                    return _crystalShardPool.Get(position, Quaternion.identity) as T;

                case EnemyType.Boss when _bossPool != null:
                    return _bossPool.Get(position, Quaternion.identity) as T;

                default:
                    Debug.LogWarning($"[EnemyPoolManager] Cannot get {type} - no pool available");
                    return null;
            }
        }

        /// <summary>
        /// Get pool for specific enemy type (used for generic spawning)
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : EnemyBase
        {
            if (typeof(T) == typeof(DataMite)) return _dataMitePool as ObjectPool<T>;
            if (typeof(T) == typeof(ScanDrone)) return _scanDronePool as ObjectPool<T>;
            if (typeof(T) == typeof(Fizzer)) return _fizzerPool as ObjectPool<T>;
            if (typeof(T) == typeof(UFO)) return _ufoPool as ObjectPool<T>;
            if (typeof(T) == typeof(ChaosWorm)) return _chaosWormPool as ObjectPool<T>;
            if (typeof(T) == typeof(VoidSphere)) return _voidSpherePool as ObjectPool<T>;
            if (typeof(T) == typeof(CrystalShard)) return _crystalShardPool as ObjectPool<T>;
            if (typeof(T) == typeof(Boss)) return _bossPool as ObjectPool<T>;
            return null;
        }

        /// <summary>
        /// Return an enemy to its pool
        /// </summary>
        public void ReturnEnemy(EnemyBase enemy)
        {
            switch (enemy)
            {
                case DataMite dm: _dataMitePool?.Return(dm); break;
                case ScanDrone sd: _scanDronePool?.Return(sd); break;
                case Fizzer f: _fizzerPool?.Return(f); break;
                case UFO u: _ufoPool?.Return(u); break;
                case ChaosWorm cw: _chaosWormPool?.Return(cw); break;
                case VoidSphere vs: _voidSpherePool?.Return(vs); break;
                case CrystalShard cs: _crystalShardPool?.Return(cs); break;
                case Boss b: _bossPool?.Return(b); break;
            }
        }

        /// <summary>
        /// Check if pool exists for enemy type
        /// </summary>
        public bool HasPool(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.DataMite: return _dataMitePool != null;
                case EnemyType.ScanDrone: return _scanDronePool != null;
                case EnemyType.Fizzer: return _fizzerPool != null;
                case EnemyType.UFO: return _ufoPool != null;
                case EnemyType.ChaosWorm: return _chaosWormPool != null;
                case EnemyType.VoidSphere: return _voidSpherePool != null;
                case EnemyType.CrystalShard: return _crystalShardPool != null;
                case EnemyType.Boss: return _bossPool != null;
                default: return false;
            }
        }

        /// <summary>
        /// Get enemy type from generic pool type
        /// </summary>
        public EnemyType GetEnemyTypeFromPool<T>() where T : EnemyBase
        {
            if (typeof(T) == typeof(DataMite)) return EnemyType.DataMite;
            if (typeof(T) == typeof(ScanDrone)) return EnemyType.ScanDrone;
            if (typeof(T) == typeof(Fizzer)) return EnemyType.Fizzer;
            if (typeof(T) == typeof(UFO)) return EnemyType.UFO;
            if (typeof(T) == typeof(ChaosWorm)) return EnemyType.ChaosWorm;
            if (typeof(T) == typeof(VoidSphere)) return EnemyType.VoidSphere;
            if (typeof(T) == typeof(CrystalShard)) return EnemyType.CrystalShard;
            if (typeof(T) == typeof(Boss)) return EnemyType.Boss;
            return EnemyType.DataMite;
        }
    }
}
