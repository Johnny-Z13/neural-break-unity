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
            AutoWireReferences();
            SetupPlayerSprite();
            SetupProjectileSprites();
            SetupEnemyPrefabSprites();

            Debug.Log("[PrefabSpriteSetup] All sprites configured");
        }

        /// <summary>
        /// Auto-wire prefab and scene references if not assigned in Inspector.
        /// </summary>
        private void AutoWireReferences()
        {
            if (m_player == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) m_player = playerGO.GetComponent<PlayerController>();
            }

            #if UNITY_EDITOR
            if (m_projectilePrefab == null)
                m_projectilePrefab = LoadPrefab<Projectile>("Assets/_Project/Prefabs/Projectiles/Projectile.prefab");
            if (m_enemyProjectilePrefab == null)
                m_enemyProjectilePrefab = LoadPrefab<EnemyProjectile>("Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab");
            if (m_dataMitePrefab == null)
                m_dataMitePrefab = LoadPrefab<DataMite>("Assets/_Project/Prefabs/Enemies/DataMite.prefab");
            if (m_scanDronePrefab == null)
                m_scanDronePrefab = LoadPrefab<ScanDrone>("Assets/_Project/Prefabs/Enemies/ScanDrone.prefab");
            if (m_fizzerPrefab == null)
                m_fizzerPrefab = LoadPrefab<Fizzer>("Assets/_Project/Prefabs/Enemies/Fizzer.prefab");
            if (m_ufoPrefab == null)
                m_ufoPrefab = LoadPrefab<UFO>("Assets/_Project/Prefabs/Enemies/UFO.prefab");
            if (m_chaosWormPrefab == null)
                m_chaosWormPrefab = LoadPrefab<ChaosWorm>("Assets/_Project/Prefabs/Enemies/ChaosWorm.prefab");
            if (m_voidSpherePrefab == null)
                m_voidSpherePrefab = LoadPrefab<VoidSphere>("Assets/_Project/Prefabs/Enemies/VoidSphere.prefab");
            if (m_crystalShardPrefab == null)
                m_crystalShardPrefab = LoadPrefab<CrystalShard>("Assets/_Project/Prefabs/Enemies/CrystalShard.prefab");
            if (m_bossPrefab == null)
                m_bossPrefab = LoadPrefab<Boss>("Assets/_Project/Prefabs/Enemies/Boss.prefab");
            #endif
        }

        #if UNITY_EDITOR
        private T LoadPrefab<T>(string path) where T : Component
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab.GetComponent<T>();
            return null;
        }
        #endif

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
