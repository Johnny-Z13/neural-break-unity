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
        [SerializeField] private bool m_enabled = true;
        [SerializeField] private int m_maxVFXPerFrame = 5;

        [Header("Particle Settings")]
        [SerializeField] private float m_emissionIntensity = 3f;

        private int m_vfxThisFrame;
        private Material m_particleMaterial;
        private Dictionary<EnemyType, IEnemyVFXGenerator> m_vfxGenerators;

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

            if (m_particleMaterial != null)
            {
                Destroy(m_particleMaterial);
            }
        }

        private void LateUpdate()
        {
            m_vfxThisFrame = 0;
        }

        /// <summary>
        /// Initializes the VFX generator registry with all enemy type generators.
        /// </summary>
        private void InitializeVFXGenerators()
        {
            m_vfxGenerators = new Dictionary<EnemyType, IEnemyVFXGenerator>
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
                m_particleMaterial = new Material(shader);

                // Assign soft particle texture to avoid quad rendering
                var softTexture = VFXHelpers.GetSoftParticleTexture();
                if (m_particleMaterial.HasProperty("_BaseMap"))
                    m_particleMaterial.SetTexture("_BaseMap", softTexture);
                if (m_particleMaterial.HasProperty("_MainTex"))
                    m_particleMaterial.SetTexture("_MainTex", softTexture);

                m_particleMaterial.SetColor("_BaseColor", Color.white);
                m_particleMaterial.SetColor("_Color", Color.white);
                m_particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m_particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m_particleMaterial.SetInt("_ZWrite", 0);
                m_particleMaterial.renderQueue = 3000;
                m_particleMaterial.EnableKeyword("_EMISSION");

                if (m_particleMaterial.HasProperty("_Surface"))
                {
                    m_particleMaterial.SetFloat("_Surface", 1);
                    m_particleMaterial.SetFloat("_Blend", 1);
                }
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!m_enabled) return;
            if (m_vfxThisFrame >= m_maxVFXPerFrame) return;

            SpawnDeathEffect(evt.position, evt.enemyType);
            m_vfxThisFrame++;
        }

        /// <summary>
        /// Spawns a death effect for the specified enemy type at the given position.
        /// </summary>
        public void SpawnDeathEffect(Vector3 position, EnemyType enemyType)
        {
            if (!m_enabled) return;
            if (m_particleMaterial == null) return;

            // Get the appropriate VFX generator
            if (m_vfxGenerators.TryGetValue(enemyType, out IEnemyVFXGenerator generator))
            {
                GameObject vfxGO = generator.GenerateDeathEffect(position, m_particleMaterial, m_emissionIntensity);
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
            m_enabled = enabled;
        }
    }
}
