using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Graphics.VFX;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Coordinator for enemy death visual effects using procedural particle systems.
    /// Uses a factory pattern to delegate VFX generation to per-enemy-type generators.
    /// Each enemy type has a unique death particle system matching their visual style.
    ///
    /// POOLED: Pre-creates VFX GameObjects per enemy type at startup.
    /// On death: reposition + replay. On effect complete: return to pool.
    /// Zero Instantiate/Destroy during gameplay.
    /// </summary>
    public class EnemyDeathVFX : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool m_enabled = true;
        [SerializeField] private int m_maxVFXPerFrame = 5;
        [SerializeField] private int m_poolSizePerType = 10;

        [Header("Particle Settings")]
        [SerializeField] private float m_emissionIntensity = 3f;

        private int m_vfxThisFrame;
        private Material m_particleMaterial;
        private Dictionary<EnemyType, IEnemyVFXGenerator> m_vfxGenerators;

        // Object pools per enemy type
        private Dictionary<EnemyType, Queue<GameObject>> m_vfxPools;
        private Dictionary<EnemyType, float> m_vfxLifetimes;
        private Transform m_poolContainer;

        // Cached ParticleSystem arrays per VFX GameObject (avoids GetComponentsInChildren allocation)
        private Dictionary<GameObject, ParticleSystem[]> m_cachedParticleSystems = new Dictionary<GameObject, ParticleSystem[]>();

        // Timer-based pool returns (replaces StartCoroutine per death - zero allocation)
        private struct ActiveDeathVFX
        {
            public GameObject vfxGO;
            public Queue<GameObject> pool;
            public float returnTime;
        }
        private readonly List<ActiveDeathVFX> m_activeDeathVFX = new List<ActiveDeathVFX>(32);

        private void Awake()
        {
            CreateParticleMaterial();
            InitializeVFXGenerators();
            InitializePools();
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

            // Timer-based pool returns (replaces StartCoroutine per death - zero allocation)
            float time = Time.time;
            for (int i = m_activeDeathVFX.Count - 1; i >= 0; i--)
            {
                var active = m_activeDeathVFX[i];
                if (time >= active.returnTime)
                {
                    if (active.vfxGO != null)
                    {
                        StopAllParticles(active.vfxGO);
                        active.vfxGO.SetActive(false);
                        active.pool.Enqueue(active.vfxGO);
                    }
                    m_activeDeathVFX.RemoveAt(i);
                }
            }
        }

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

        private void InitializePools()
        {
            m_poolContainer = new GameObject("DeathVFX_Pool").transform;
            m_poolContainer.SetParent(transform);

            m_vfxPools = new Dictionary<EnemyType, Queue<GameObject>>();
            m_vfxLifetimes = new Dictionary<EnemyType, float>();

            if (m_particleMaterial == null) return;

            foreach (var kvp in m_vfxGenerators)
            {
                var enemyType = kvp.Key;
                var generator = kvp.Value;
                var pool = new Queue<GameObject>();

                m_vfxLifetimes[enemyType] = generator.GetEffectLifetime();

                for (int i = 0; i < m_poolSizePerType; i++)
                {
                    var vfxGO = generator.GenerateDeathEffect(Vector3.zero, m_particleMaterial, m_emissionIntensity);
                    vfxGO.name = $"DeathVFX_{enemyType}_{i}";
                    vfxGO.transform.SetParent(m_poolContainer);
                    // Cache particle systems to avoid GetComponentsInChildren allocation during gameplay
                    m_cachedParticleSystems[vfxGO] = vfxGO.GetComponentsInChildren<ParticleSystem>(true);
                    StopAllParticles(vfxGO);
                    vfxGO.SetActive(false);
                    pool.Enqueue(vfxGO);
                }

                m_vfxPools[enemyType] = pool;
            }
        }

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

                var softTexture = VFXHelpers.GetSoftParticleTexture();
                if (m_particleMaterial.HasProperty("_BaseMap"))
                    m_particleMaterial.SetTexture("_BaseMap", softTexture);
                if (m_particleMaterial.HasProperty("_MainTex"))
                    m_particleMaterial.SetTexture("_MainTex", softTexture);

                m_particleMaterial.SetColor("_BaseColor", Color.white);
                m_particleMaterial.SetColor("_Color", Color.white);
                m_particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m_particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
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

        public void SpawnDeathEffect(Vector3 position, EnemyType enemyType)
        {
            if (!m_enabled) return;
            if (!m_vfxPools.TryGetValue(enemyType, out var pool)) return;

            GameObject vfxGO;

            if (pool.Count > 0)
            {
                // Reuse from pool
                vfxGO = pool.Dequeue();
            }
            else
            {
                // Pool exhausted — create a new one (rare, pool should be sized correctly)
                if (m_vfxGenerators.TryGetValue(enemyType, out var generator) && m_particleMaterial != null)
                {
                    vfxGO = generator.GenerateDeathEffect(position, m_particleMaterial, m_emissionIntensity);
                    vfxGO.name = $"DeathVFX_{enemyType}_overflow";
                    vfxGO.transform.SetParent(m_poolContainer);
                    // Cache particle systems for overflow objects too
                    m_cachedParticleSystems[vfxGO] = vfxGO.GetComponentsInChildren<ParticleSystem>(true);
                }
                else
                {
                    return;
                }
            }

            // Reposition and replay
            vfxGO.transform.position = position;
            vfxGO.SetActive(true);
            PlayAllParticles(vfxGO);

            // Schedule return to pool (timer-based, zero allocation - no coroutine)
            float lifetime = m_vfxLifetimes.TryGetValue(enemyType, out float lt) ? lt : 1.5f;
            m_activeDeathVFX.Add(new ActiveDeathVFX
            {
                vfxGO = vfxGO,
                pool = pool,
                returnTime = Time.time + lifetime
            });
        }

        private void PlayAllParticles(GameObject go)
        {
            if (!m_cachedParticleSystems.TryGetValue(go, out var particles)) return;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear();
                particles[i].Play();
            }
        }

        private void StopAllParticles(GameObject go)
        {
            if (!m_cachedParticleSystems.TryGetValue(go, out var particles)) return;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void SetEnabled(bool enabled)
        {
            m_enabled = enabled;
        }
    }
}
