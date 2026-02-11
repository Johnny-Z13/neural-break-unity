using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Types of environmental particle effects
    /// </summary>
    public enum EnvironmentParticleType
    {
        Dust,
        Sparks,
        DataBits,
        Energy,
        Snow,
        Embers,
        Glitch
    }

    /// <summary>
    /// Manages ambient environmental particle effects.
    /// Creates atmosphere with floating particles that react to game state.
    /// </summary>
    public class EnvironmentParticles : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private EnvironmentParticleType m_currentType = EnvironmentParticleType.DataBits;
        [SerializeField] private int m_particleCount = 100;
        [SerializeField] private float m_spawnRadius = 15f;
        [SerializeField] private float m_particleSpeed = 1f;
        [SerializeField] private float m_particleSize = 0.1f;
        [SerializeField] private bool m_reactToIntensity = true;

        [Header("Colors")]
        [SerializeField] private Color m_baseColor = new Color(0.3f, 0.8f, 1f, 0.5f);
        [SerializeField] private Color m_highlightColor = new Color(1f, 1f, 1f, 0.8f);

        // Particle system
        private List<Particle> m_particles = new List<Particle>();
        private SpriteRenderer[] m_particleRenderers;
        private Transform m_cameraTransform;
        private float m_currentIntensity = 0.5f;

        private class Particle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 velocity;
            public float phase;
            public float baseAlpha;
            public float size;
        }

        private void Awake()
        {
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                m_cameraTransform = Camera.main.transform;
            }

            CreateParticles();

            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<BossSpawnedEvent>(OnBossSpawned);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<BossSpawnedEvent>(OnBossSpawned);

        }

        private void Update()
        {
            UpdateParticles();
        }

        private void CreateParticles()
        {
            // Clean up existing
            foreach (var p in m_particles)
            {
                if (p.transform != null)
                {
                    Destroy(p.transform.gameObject);
                }
            }
            m_particles.Clear();

            // Create container
            var container = new GameObject("Particles");
            container.transform.SetParent(transform);

            // Create sprite for particles
            Sprite particleSprite = CreateParticleSprite(m_currentType);

            // Create particles
            for (int i = 0; i < m_particleCount; i++)
            {
                var particle = new Particle();

                var go = new GameObject($"Particle_{i}");
                go.transform.SetParent(container.transform);
                particle.transform = go.transform;

                particle.renderer = go.AddComponent<SpriteRenderer>();
                particle.renderer.sprite = particleSprite;
                particle.renderer.sortingOrder = -50;

                // Random initial state
                ResetParticle(particle);

                m_particles.Add(particle);
            }

            Debug.Log($"[EnvironmentParticles] Created {m_particleCount} {m_currentType} particles");
        }

        private void ResetParticle(Particle p)
        {
            Vector3 center = m_cameraTransform != null ? m_cameraTransform.position : Vector3.zero;
            center.z = 0;

            // Random position within radius
            Vector2 offset = Random.insideUnitCircle * m_spawnRadius;
            p.transform.position = center + new Vector3(offset.x, offset.y, Random.Range(1f, 5f));

            // Random velocity based on type
            p.velocity = GetVelocityForType(m_currentType);

            // Random visual properties
            p.phase = Random.value * Mathf.PI * 2f;
            p.baseAlpha = Random.Range(0.3f, 0.8f);
            p.size = m_particleSize * Random.Range(0.5f, 1.5f);

            // Apply size
            p.transform.localScale = Vector3.one * p.size;

            // Apply color with variation
            Color c = Color.Lerp(m_baseColor, m_highlightColor, Random.value * 0.3f);
            c.a = p.baseAlpha;
            p.renderer.color = c;
        }

        private Vector3 GetVelocityForType(EnvironmentParticleType type)
        {
            switch (type)
            {
                case EnvironmentParticleType.Dust:
                    return new Vector3(
                        Random.Range(-0.2f, 0.2f),
                        Random.Range(-0.1f, 0.1f),
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.Sparks:
                    return new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        Random.Range(0.5f, 1f),
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.DataBits:
                    return new Vector3(
                        Random.Range(-0.3f, 0.3f),
                        Random.Range(-0.5f, -0.1f),
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.Energy:
                    float angle = Random.value * Mathf.PI * 2f;
                    return new Vector3(
                        Mathf.Cos(angle) * 0.3f,
                        Mathf.Sin(angle) * 0.3f,
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.Snow:
                    return new Vector3(
                        Random.Range(-0.1f, 0.1f),
                        Random.Range(-0.3f, -0.5f),
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.Embers:
                    return new Vector3(
                        Random.Range(-0.2f, 0.2f),
                        Random.Range(0.3f, 0.8f),
                        0
                    ) * m_particleSpeed;

                case EnvironmentParticleType.Glitch:
                    return new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f),
                        0
                    ) * m_particleSpeed * 2f;

                default:
                    return Vector3.zero;
            }
        }

        private void UpdateParticles()
        {
            Vector3 center = m_cameraTransform != null ? m_cameraTransform.position : Vector3.zero;
            center.z = 0;

            float time = Time.time;
            float speedMult = m_reactToIntensity ? (0.5f + m_currentIntensity * 1f) : 1f;

            foreach (var p in m_particles)
            {
                if (p.transform == null) continue;

                // Move particle
                p.transform.position += p.velocity * Time.deltaTime * speedMult;

                // Add type-specific behavior
                switch (m_currentType)
                {
                    case EnvironmentParticleType.DataBits:
                        // Slight horizontal wave
                        float wave = Mathf.Sin(time * 2f + p.phase) * 0.01f;
                        p.transform.position += new Vector3(wave, 0, 0);
                        break;

                    case EnvironmentParticleType.Energy:
                        // Orbit behavior
                        float orbitAngle = time * 0.5f + p.phase;
                        p.velocity = new Vector3(
                            Mathf.Cos(orbitAngle) * 0.3f,
                            Mathf.Sin(orbitAngle) * 0.3f,
                            0
                        ) * m_particleSpeed;
                        break;

                    case EnvironmentParticleType.Snow:
                        // Gentle sway
                        float sway = Mathf.Sin(time + p.phase) * 0.02f;
                        p.transform.position += new Vector3(sway, 0, 0);
                        break;

                    case EnvironmentParticleType.Glitch:
                        // Random teleport occasionally
                        if (Random.value < 0.01f * m_currentIntensity)
                        {
                            Vector2 offset = Random.insideUnitCircle * 2f;
                            p.transform.position += new Vector3(offset.x, offset.y, 0);
                        }
                        break;
                }

                // Alpha pulse
                float pulse = Mathf.Sin(time * 3f + p.phase) * 0.2f + 0.8f;
                Color c = p.renderer.color;
                c.a = p.baseAlpha * pulse * (m_reactToIntensity ? (0.5f + m_currentIntensity * 0.5f) : 1f);
                p.renderer.color = c;

                // Check if out of range
                float dist = Vector3.Distance(p.transform.position, center);
                if (dist > m_spawnRadius * 1.5f)
                {
                    ResetParticle(p);
                }
            }
        }

        private Sprite CreateParticleSprite(EnvironmentParticleType type)
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            switch (type)
            {
                case EnvironmentParticleType.Dust:
                case EnvironmentParticleType.Snow:
                    // Soft circle
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                            float alpha = Mathf.Clamp01(1f - dist / (size / 2f));
                            alpha = alpha * alpha; // Soft falloff
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                    break;

                case EnvironmentParticleType.DataBits:
                    // Small square with glow
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = Mathf.Abs(x - center);
                            float dy = Mathf.Abs(y - center);
                            bool inside = dx < size * 0.25f && dy < size * 0.25f;
                            float glow = 1f - Mathf.Max(dx, dy) / (size / 2f);
                            glow = Mathf.Clamp01(glow);
                            pixels[y * size + x] = new Color(1f, 1f, 1f, inside ? 1f : glow * 0.3f);
                        }
                    }
                    break;

                case EnvironmentParticleType.Sparks:
                case EnvironmentParticleType.Embers:
                    // Bright center, quick falloff
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                            float alpha = Mathf.Clamp01(1f - dist / (size / 3f));
                            alpha = Mathf.Pow(alpha, 0.5f); // Hard center
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                    break;

                case EnvironmentParticleType.Energy:
                    // Ring shape
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                            float ring = Mathf.Abs(dist - size * 0.3f);
                            float alpha = Mathf.Clamp01(1f - ring / 3f);
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                    break;

                case EnvironmentParticleType.Glitch:
                    // Random pixels
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            bool on = Random.value > 0.6f;
                            pixels[y * size + x] = new Color(1f, 1f, 1f, on ? 1f : 0f);
                        }
                    }
                    break;

                default:
                    // Default circle
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                            float alpha = Mathf.Clamp01(1f - dist / (size / 2f));
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        #region Event Handlers

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            // Match particle type to arena theme
            var arena = FindFirstObjectByType<ArenaManager>();
            if (arena != null)
            {
                switch (arena.CurrentTheme)
                {
                    case ArenaTheme.Cyber:
                        SetType(EnvironmentParticleType.DataBits);
                        SetColors(new Color(0.3f, 0.8f, 1f, 0.5f), new Color(0.5f, 1f, 1f, 0.8f));
                        break;
                    case ArenaTheme.Void:
                        SetType(EnvironmentParticleType.Energy);
                        SetColors(new Color(0.5f, 0.2f, 0.8f, 0.5f), new Color(0.8f, 0.5f, 1f, 0.8f));
                        break;
                    case ArenaTheme.Inferno:
                        SetType(EnvironmentParticleType.Embers);
                        SetColors(new Color(1f, 0.5f, 0.2f, 0.5f), new Color(1f, 0.8f, 0.3f, 0.8f));
                        break;
                    case ArenaTheme.Matrix:
                        SetType(EnvironmentParticleType.DataBits);
                        SetColors(new Color(0.2f, 0.8f, 0.3f, 0.5f), new Color(0.5f, 1f, 0.5f, 0.8f));
                        break;
                    case ArenaTheme.Neon:
                        SetType(EnvironmentParticleType.Sparks);
                        SetColors(new Color(1f, 0.3f, 0.6f, 0.5f), new Color(1f, 0.6f, 0.8f, 0.8f));
                        break;
                    case ArenaTheme.Arctic:
                        SetType(EnvironmentParticleType.Snow);
                        SetColors(new Color(0.8f, 0.9f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.8f));
                        break;
                    case ArenaTheme.Sunset:
                        SetType(EnvironmentParticleType.Dust);
                        SetColors(new Color(1f, 0.6f, 0.3f, 0.5f), new Color(1f, 0.8f, 0.5f, 0.8f));
                        break;
                }
            }

            // Increase intensity with level
            m_currentIntensity = Mathf.Clamp01(evt.levelNumber / 50f);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            m_currentIntensity = 0.2f;
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            m_currentIntensity = 1f;

            // Switch to intense particle type
            if (m_currentType != EnvironmentParticleType.Glitch)
            {
                SetType(EnvironmentParticleType.Glitch);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set particle type
        /// </summary>
        public void SetType(EnvironmentParticleType type)
        {
            if (m_currentType == type) return;
            m_currentType = type;
            CreateParticles();
        }

        /// <summary>
        /// Set particle colors
        /// </summary>
        public void SetColors(Color baseColor, Color highlightColor)
        {
            m_baseColor = baseColor;
            m_highlightColor = highlightColor;

            foreach (var p in m_particles)
            {
                if (p.renderer != null)
                {
                    Color c = Color.Lerp(m_baseColor, m_highlightColor, Random.value * 0.3f);
                    c.a = p.baseAlpha;
                    p.renderer.color = c;
                }
            }
        }

        /// <summary>
        /// Set particle count
        /// </summary>
        public void SetCount(int count)
        {
            m_particleCount = Mathf.Clamp(count, 10, 500);
            CreateParticles();
        }

        /// <summary>
        /// Set intensity (affects speed and visibility)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            m_currentIntensity = Mathf.Clamp01(intensity);
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: DataBits")]
        private void DebugDataBits() => SetType(EnvironmentParticleType.DataBits);

        [ContextMenu("Debug: Energy")]
        private void DebugEnergy() => SetType(EnvironmentParticleType.Energy);

        [ContextMenu("Debug: Snow")]
        private void DebugSnow() => SetType(EnvironmentParticleType.Snow);

        [ContextMenu("Debug: Glitch")]
        private void DebugGlitch() => SetType(EnvironmentParticleType.Glitch);

        #endregion
    }
}
