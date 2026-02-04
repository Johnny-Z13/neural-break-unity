using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Combat;
using NeuralBreak.Graphics;
using NeuralBreak.Utils;
using Z13.Core;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Handles runtime sprite generation for prefabs and scene objects.
    /// Generates simple geometric sprites (circles, squares) for objects that don't have sprites assigned.
    /// </summary>
    public class PrefabSpriteSetup : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController m_player;

        [Header("Prefab References")]
        [SerializeField] private Projectile m_projectilePrefab;
        [SerializeField] private EnemyProjectile m_enemyProjectilePrefab;
        [SerializeField] private DataMite m_dataMitePrefab;
        [SerializeField] private ScanDrone m_scanDronePrefab;
        [SerializeField] private Fizzer m_fizzerPrefab;
        [SerializeField] private UFO m_ufoPrefab;
        [SerializeField] private ChaosWorm m_chaosWormPrefab;
        [SerializeField] private VoidSphere m_voidSpherePrefab;
        [SerializeField] private CrystalShard m_crystalShardPrefab;
        [SerializeField] private Boss m_bossPrefab;

        // Cached sprites for runtime generation
        private static Sprite s_circleSprite;
        private static Sprite s_squareSprite;

        public static Sprite CircleSprite
        {
            get
            {
                if (s_circleSprite == null)
                    s_circleSprite = RuntimeSpriteGenerator.CreateCircleSprite(64);
                return s_circleSprite;
            }
        }

        public static Sprite SquareSprite
        {
            get
            {
                if (s_squareSprite == null)
                    s_squareSprite = RuntimeSpriteGenerator.CreateSquareSprite(64);
                return s_squareSprite;
            }
        }

        /// <summary>
        /// Sets up sprites for all prefabs and scene objects that need them.
        /// </summary>
        public void SetupAllSprites()
        {
            SetupPlayerSprite();
            SetupProjectileSprites();
            SetupEnemyPrefabSprites();

            Debug.Log("[PrefabSpriteSetup] All sprites configured");
        }

        private void SetupPlayerSprite()
        {
            if (m_player == null)
            {
                Debug.LogWarning("[PrefabSpriteSetup] Player reference is missing!");
                return;
            }

            var sr = m_player.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CircleSprite;
                Debug.Log("[PrefabSpriteSetup] Player sprite generated");
            }
        }

        private void SetupProjectileSprites()
        {
            // Setup player projectile prefab sprite
            if (m_projectilePrefab != null)
            {
                var sr = m_projectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }

            // Setup enemy projectile prefab sprite
            if (m_enemyProjectilePrefab != null)
            {
                var sr = m_enemyProjectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }
        }

        private void SetupEnemyPrefabSprites()
        {
            SetupEnemyPrefabSprite(m_dataMitePrefab);
            SetupEnemyPrefabSprite(m_scanDronePrefab);
            SetupEnemyPrefabSprite(m_fizzerPrefab);
            SetupEnemyPrefabSprite(m_ufoPrefab);
            SetupEnemyPrefabSprite(m_chaosWormPrefab);
            SetupEnemyPrefabSprite(m_voidSpherePrefab);
            SetupEnemyPrefabSprite(m_crystalShardPrefab);
            SetupEnemyPrefabSprite(m_bossPrefab);
        }

        private void SetupEnemyPrefabSprite(EnemyBase enemy)
        {
            if (enemy == null) return;

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CircleSprite;
            }
        }
    }
}
