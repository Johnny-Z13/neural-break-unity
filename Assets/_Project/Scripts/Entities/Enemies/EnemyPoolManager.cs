using UnityEngine;
using NeuralBreak.Config;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Manages object pools for all 8 enemy types.
    /// Handles pool initialization, cleanup, and Get/Return operations.
    /// </summary>
    public class EnemyPoolManager
    {
        // Enemy prefabs
        private readonly DataMite m_dataMitePrefab;
        private readonly ScanDrone m_scanDronePrefab;
        private readonly Fizzer m_fizzerPrefab;
        private readonly UFO m_ufoPrefab;
        private readonly ChaosWorm m_chaosWormPrefab;
        private readonly VoidSphere m_voidSpherePrefab;
        private readonly CrystalShard m_crystalShardPrefab;
        private readonly Boss m_bossPrefab;

        // Parent container
        private readonly Transform m_container;

        // Object pools
        private ObjectPool<DataMite> m_dataMitePool;
        private ObjectPool<ScanDrone> m_scanDronePool;
        private ObjectPool<Fizzer> m_fizzerPool;
        private ObjectPool<UFO> m_ufoPool;
        private ObjectPool<ChaosWorm> m_chaosWormPool;
        private ObjectPool<VoidSphere> m_voidSpherePool;
        private ObjectPool<CrystalShard> m_crystalShardPool;
        private ObjectPool<Boss> m_bossPool;

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
            m_dataMitePrefab = dataMitePrefab;
            m_scanDronePrefab = scanDronePrefab;
            m_fizzerPrefab = fizzerPrefab;
            m_ufoPrefab = ufoPrefab;
            m_chaosWormPrefab = chaosWormPrefab;
            m_voidSpherePrefab = voidSpherePrefab;
            m_crystalShardPrefab = crystalShardPrefab;
            m_bossPrefab = bossPrefab;
            m_container = container;

            InitializePools();
        }

        private void InitializePools()
        {
            // Initialize all pools with config-driven pool sizes
            if (m_dataMitePrefab != null)
            {
                m_dataMitePool = new ObjectPool<DataMite>(m_dataMitePrefab, m_container,
                    Balance.dataMite.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_scanDronePrefab != null)
            {
                m_scanDronePool = new ObjectPool<ScanDrone>(m_scanDronePrefab, m_container,
                    Balance.scanDrone.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_fizzerPrefab != null)
            {
                m_fizzerPool = new ObjectPool<Fizzer>(m_fizzerPrefab, m_container,
                    Balance.fizzer.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_ufoPrefab != null)
            {
                m_ufoPool = new ObjectPool<UFO>(m_ufoPrefab, m_container,
                    Balance.ufo.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_chaosWormPrefab != null)
            {
                m_chaosWormPool = new ObjectPool<ChaosWorm>(m_chaosWormPrefab, m_container,
                    Balance.chaosWorm.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_voidSpherePrefab != null)
            {
                m_voidSpherePool = new ObjectPool<VoidSphere>(m_voidSpherePrefab, m_container,
                    Balance.voidSphere.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_crystalShardPrefab != null)
            {
                m_crystalShardPool = new ObjectPool<CrystalShard>(m_crystalShardPrefab, m_container,
                    Balance.crystalShard.poolSize, onReturn: e => e.OnReturnToPool());
            }

            if (m_bossPrefab != null)
            {
                m_bossPool = new ObjectPool<Boss>(m_bossPrefab, m_container,
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
                case EnemyType.DataMite when m_dataMitePool != null:
                    return m_dataMitePool.Get(position, Quaternion.identity) as T;

                case EnemyType.ScanDrone when m_scanDronePool != null:
                    return m_scanDronePool.Get(position, Quaternion.identity) as T;

                case EnemyType.Fizzer when m_fizzerPool != null:
                    return m_fizzerPool.Get(position, Quaternion.identity) as T;

                case EnemyType.UFO when m_ufoPool != null:
                    return m_ufoPool.Get(position, Quaternion.identity) as T;

                case EnemyType.ChaosWorm when m_chaosWormPool != null:
                    return m_chaosWormPool.Get(position, Quaternion.identity) as T;

                case EnemyType.VoidSphere when m_voidSpherePool != null:
                    return m_voidSpherePool.Get(position, Quaternion.identity) as T;

                case EnemyType.CrystalShard when m_crystalShardPool != null:
                    return m_crystalShardPool.Get(position, Quaternion.identity) as T;

                case EnemyType.Boss when m_bossPool != null:
                    return m_bossPool.Get(position, Quaternion.identity) as T;

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
            if (typeof(T) == typeof(DataMite)) return m_dataMitePool as ObjectPool<T>;
            if (typeof(T) == typeof(ScanDrone)) return m_scanDronePool as ObjectPool<T>;
            if (typeof(T) == typeof(Fizzer)) return m_fizzerPool as ObjectPool<T>;
            if (typeof(T) == typeof(UFO)) return m_ufoPool as ObjectPool<T>;
            if (typeof(T) == typeof(ChaosWorm)) return m_chaosWormPool as ObjectPool<T>;
            if (typeof(T) == typeof(VoidSphere)) return m_voidSpherePool as ObjectPool<T>;
            if (typeof(T) == typeof(CrystalShard)) return m_crystalShardPool as ObjectPool<T>;
            if (typeof(T) == typeof(Boss)) return m_bossPool as ObjectPool<T>;
            return null;
        }

        /// <summary>
        /// Return an enemy to its pool
        /// </summary>
        public void ReturnEnemy(EnemyBase enemy)
        {
            switch (enemy)
            {
                case DataMite dm: m_dataMitePool?.Return(dm); break;
                case ScanDrone sd: m_scanDronePool?.Return(sd); break;
                case Fizzer f: m_fizzerPool?.Return(f); break;
                case UFO u: m_ufoPool?.Return(u); break;
                case ChaosWorm cw: m_chaosWormPool?.Return(cw); break;
                case VoidSphere vs: m_voidSpherePool?.Return(vs); break;
                case CrystalShard cs: m_crystalShardPool?.Return(cs); break;
                case Boss b: m_bossPool?.Return(b); break;
            }
        }

        /// <summary>
        /// Check if pool exists for enemy type
        /// </summary>
        public bool HasPool(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.DataMite: return m_dataMitePool != null;
                case EnemyType.ScanDrone: return m_scanDronePool != null;
                case EnemyType.Fizzer: return m_fizzerPool != null;
                case EnemyType.UFO: return m_ufoPool != null;
                case EnemyType.ChaosWorm: return m_chaosWormPool != null;
                case EnemyType.VoidSphere: return m_voidSpherePool != null;
                case EnemyType.CrystalShard: return m_crystalShardPool != null;
                case EnemyType.Boss: return m_bossPool != null;
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
