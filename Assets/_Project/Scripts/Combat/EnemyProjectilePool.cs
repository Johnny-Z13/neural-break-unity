using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Manages pooling for enemy projectiles.
    /// Singleton for easy access from enemy scripts.
    /// </summary>
    public class EnemyProjectilePool : MonoBehaviour
    {
        public static EnemyProjectilePool Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private EnemyProjectile m_projectilePrefab;
        [SerializeField] private int m_poolSize = 200;
        [SerializeField] private Transform m_container;

        private ObjectPool<EnemyProjectile> m_pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitializePool()
        {
            if (m_container == null)
            {
                m_container = new GameObject("EnemyProjectiles").transform;
                m_container.SetParent(transform);
            }

            if (m_projectilePrefab != null)
            {
                m_pool = new ObjectPool<EnemyProjectile>(
                    m_projectilePrefab,
                    m_container,
                    m_poolSize,
                    onReturn: proj => proj.OnReturnToPool()
                );
            }
            else
            {
                Debug.LogWarning("[EnemyProjectilePool] No projectile prefab assigned!");
            }
        }

        /// <summary>
        /// Fire a projectile from an enemy position
        /// </summary>
        public EnemyProjectile Fire(Vector2 position, Vector2 direction, float speed, int damage, Color? color = null)
        {
            if (m_pool == null)
            {
                Debug.LogError("[EnemyProjectilePool] Pool not initialized!");
                return null;
            }

            EnemyProjectile proj = m_pool.Get(position, Quaternion.identity);
            proj.Initialize(position, direction, speed, damage, Return, color);
            return proj;
        }

        /// <summary>
        /// Fire multiple projectiles in a spread pattern
        /// </summary>
        public void FireSpread(Vector2 position, Vector2 centerDirection, float speed, int damage,
            int count, float spreadAngle, Color? color = null)
        {
            if (count <= 1)
            {
                Fire(position, centerDirection, speed, damage, color);
                return;
            }

            float startAngle = -spreadAngle / 2f;
            float angleStep = spreadAngle / (count - 1);
            float baseAngle = Mathf.Atan2(centerDirection.y, centerDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + startAngle + (angleStep * i)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Fire(position, dir, speed, damage, color);
            }
        }

        /// <summary>
        /// Fire projectiles in a ring pattern (360 degrees)
        /// </summary>
        public void FireRing(Vector2 position, float speed, int damage, int count, Color? color = null)
        {
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = (angleStep * i) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Fire(position, dir, speed, damage, color);
            }
        }

        /// <summary>
        /// Fire a burst of projectiles with delay (coroutine-friendly)
        /// </summary>
        public System.Collections.IEnumerator FireBurst(Vector2 position, Vector2 direction,
            float speed, int damage, int burstCount, float burstDelay, Color? color = null)
        {
            for (int i = 0; i < burstCount; i++)
            {
                Fire(position, direction, speed, damage, color);

                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstDelay);
                }
            }
        }

        private void Return(EnemyProjectile proj)
        {
            m_pool?.Return(proj);
        }

        /// <summary>
        /// Clear all active projectiles (for level transitions)
        /// </summary>
        public void ClearAll()
        {
            // Deactivate all active projectiles
            if (m_container != null)
            {
                foreach (Transform child in m_container)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
