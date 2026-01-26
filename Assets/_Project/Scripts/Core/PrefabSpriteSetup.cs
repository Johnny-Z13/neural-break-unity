using UnityEngine;
using NeuralBreak.Entities;
using NeuralBreak.Combat;
using NeuralBreak.Graphics;
using NeuralBreak.Utils;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Handles runtime sprite generation for prefabs and scene objects.
    /// Generates simple geometric sprites (circles, squares) for objects that don't have sprites assigned.
    /// </summary>
    public class PrefabSpriteSetup : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController _player;

        [Header("Prefab References")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private EnemyProjectile _enemyProjectilePrefab;
        [SerializeField] private DataMite _dataMitePrefab;
        [SerializeField] private ScanDrone _scanDronePrefab;
        [SerializeField] private Fizzer _fizzerPrefab;
        [SerializeField] private UFO _ufoPrefab;
        [SerializeField] private ChaosWorm _chaosWormPrefab;
        [SerializeField] private VoidSphere _voidSpherePrefab;
        [SerializeField] private CrystalShard _crystalShardPrefab;
        [SerializeField] private Boss _bossPrefab;

        // Cached sprites for runtime generation
        private static Sprite _circleSprite;
        private static Sprite _squareSprite;

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                    _circleSprite = RuntimeSpriteGenerator.CreateCircleSprite(64);
                return _circleSprite;
            }
        }

        public static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite == null)
                    _squareSprite = RuntimeSpriteGenerator.CreateSquareSprite(64);
                return _squareSprite;
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
            if (_player == null)
            {
                Debug.LogWarning("[PrefabSpriteSetup] Player reference is missing!");
                return;
            }

            var sr = _player.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CircleSprite;
                Debug.Log("[PrefabSpriteSetup] Player sprite generated");
            }
        }

        private void SetupProjectileSprites()
        {
            // Setup player projectile prefab sprite
            if (_projectilePrefab != null)
            {
                var sr = _projectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }

            // Setup enemy projectile prefab sprite
            if (_enemyProjectilePrefab != null)
            {
                var sr = _enemyProjectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }
        }

        private void SetupEnemyPrefabSprites()
        {
            SetupEnemyPrefabSprite(_dataMitePrefab);
            SetupEnemyPrefabSprite(_scanDronePrefab);
            SetupEnemyPrefabSprite(_fizzerPrefab);
            SetupEnemyPrefabSprite(_ufoPrefab);
            SetupEnemyPrefabSprite(_chaosWormPrefab);
            SetupEnemyPrefabSprite(_voidSpherePrefab);
            SetupEnemyPrefabSprite(_crystalShardPrefab);
            SetupEnemyPrefabSprite(_bossPrefab);
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
