using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Graphics.VFX;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Coordinator for enemy death visual effects using procedural particle systems.
    /// Uses a factory pattern to delegate VFX generation to per-enemy-type generators.
    /// Each enemy type has a unique death particle system matching their visual style.
    /// </summary>
    public class EnemyDeathVFX : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private int _maxVFXPerFrame = 5;

        [Header("Particle Settings")]
        [SerializeField] private float _emissionIntensity = 3f;

        private int _vfxThisFrame;
        private Material _particleMaterial;
        private Dictionary<EnemyType, IEnemyVFXGenerator> _vfxGenerators;

        private void Awake()
        {
            CreateParticleMaterial();
            InitializeVFXGenerators();
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);

            if (_particleMaterial != null)
            {
                Destroy(_particleMaterial);
            }
        }

        private void LateUpdate()
        {
            _vfxThisFrame = 0;
        }

        /// <summary>
        /// Initializes the VFX generator registry with all enemy type generators.
        /// </summary>
        private void InitializeVFXGenerators()
        {
            _vfxGenerators = new Dictionary<EnemyType, IEnemyVFXGenerator>
            {
                { EnemyType.DataMite, new DataMiteVFX() },
                { EnemyType.ScanDrone, new ScanDroneVFX() },
                { EnemyType.Fizzer, new FizzerVFX() },
                { EnemyType.UFO, new UFOVFX() },
                { EnemyType.ChaosWorm, new ChaosWormVFX() },
                { EnemyType.VoidSphere, new VoidSphereVFX() },
                { EnemyType.CrystalShard, new CrystalShardVFX() },
                { EnemyType.Boss, new BossVFX() }
            };
        }

        /// <summary>
        /// Creates the shared particle material used by all VFX generators.
        /// </summary>
        private void CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                _particleMaterial = new Material(shader);
                _particleMaterial.SetColor("_BaseColor", Color.white);
                _particleMaterial.SetColor("_Color", Color.white);
                _particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                _particleMaterial.SetInt("_ZWrite", 0);
                _particleMaterial.renderQueue = 3000;
                _particleMaterial.EnableKeyword("_EMISSION");

                if (_particleMaterial.HasProperty("_Surface"))
                {
                    _particleMaterial.SetFloat("_Surface", 1);
                    _particleMaterial.SetFloat("_Blend", 1);
                }
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!_enabled) return;
            if (_vfxThisFrame >= _maxVFXPerFrame) return;

            SpawnDeathEffect(evt.position, evt.enemyType);
            _vfxThisFrame++;
        }

        /// <summary>
        /// Spawns a death effect for the specified enemy type at the given position.
        /// </summary>
        public void SpawnDeathEffect(Vector3 position, EnemyType enemyType)
        {
            if (!_enabled) return;
            if (_particleMaterial == null) return;

            // Get the appropriate VFX generator
            if (_vfxGenerators.TryGetValue(enemyType, out IEnemyVFXGenerator generator))
            {
                GameObject vfxGO = generator.GenerateDeathEffect(position, _particleMaterial, _emissionIntensity);
                float lifetime = generator.GetEffectLifetime();
                Destroy(vfxGO, lifetime);
            }
            else
            {
                Debug.LogWarning($"No VFX generator registered for enemy type: {enemyType}");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
    }
}
