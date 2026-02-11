using UnityEngine;
using UnityEngine.Rendering;
using NeuralBreak.Core;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// 80s Arcade-style flying starfield background coordinator.
    /// Creates a "flying through space" effect with twinkling stars,
    /// motion trails, and nebula clouds.
    /// Based on TypeScript Starfield.ts.
    /// Refactored to use sub-systems for cleaner separation of concerns.
    /// Locked to world origin (0,0,0) - does not follow camera.
    /// </summary>
    public class StarfieldController : MonoBehaviour
    {
        [Header("Star Configuration")]
        [SerializeField] private int m_starCount = 400;
        [SerializeField] private float m_starFieldDepth = 100f;
        [SerializeField] private float m_starFieldRadius = 50f;
        [SerializeField] private bool m_autoSizeToArena = true;
        [SerializeField] private float m_speed = 2f;
        [SerializeField] private float m_minSpeed = 0.5f;
        [SerializeField] private float m_maxSpeed = 10f;

        [Header("Star Appearance")]
        [SerializeField] private float m_minStarSize = 0.02f;
        [SerializeField] private float m_maxStarSize = 0.15f;
        [SerializeField] private float m_twinkleSpeed = 2f;
        [SerializeField] private bool m_enableTrails = true;

        [Header("Star Colors")]
        [SerializeField] private Color m_whiteStarColor = Color.white;
        [SerializeField] private Color m_blueWhiteStarColor = new Color(0.88f, 0.91f, 1f);
        [SerializeField] private Color m_yellowWhiteStarColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private Color m_orangeStarColor = new Color(1f, 0.88f, 0.75f);
        [SerializeField] private Color m_cyanStarColor = Color.cyan;
        [SerializeField] private Color m_lightBlueStarColor = new Color(0.75f, 0.75f, 1f);
        [SerializeField] private Color m_redStarColor = new Color(1f, 0.75f, 0.75f);

        [Header("Nebula")]
        [SerializeField] private bool m_enableNebula = true;
        [SerializeField] private int m_nebulaCount = 3;
        [SerializeField] private float m_nebulaSize = 15f;
        [SerializeField] private float m_nebulaIntensity = 0.1f;
        [SerializeField] private float m_nebulaMoveSpeed = 0.1f;

        [Header("Grid")]
        [SerializeField] private bool m_enableGrid = true;
        [SerializeField] private Color m_gridColor = new Color(0f, 1f, 1f, 0.06f);
        [SerializeField] private float m_gridHorizonY = 0f;

        [Header("Scanlines")]
        [SerializeField] private bool m_enableScanlines = false;
        [SerializeField] private float m_scanlineIntensity = 0.1f;

        [Header("References")]
        [SerializeField] private ParticleSystem m_starParticles;

        // Star data
        private Star[] m_stars;
        private ParticleSystem.Particle[] m_particles;
        private float m_time;

        // Sub-systems
        private StarfieldOptimizer m_optimizer;
        private NebulaSystem m_nebulaSystem;
        private StarGridRenderer m_gridRenderer;
        private ScanlineEffect m_scanlineEffect;
        private StarTrailRenderer m_trailRenderer;

        // Constants for star simulation
        private const float SPEED_MULTIPLIER = 10f;
        private const float DEPTH_THRESHOLD = 1f;
        private const float SCALE_MULTIPLIER = 0.1f;
        private const float Z_OFFSET_MULTIPLIER = 0.5f;
        private const float SIZE_MULTIPLIER = 2.5f;
        private const float MIN_SIZE = 0.01f;
        private const float TWINKLE_BASE = 0.7f;
        private const float TWINKLE_AMPLITUDE = 0.3f;
        private const float TWINKLE_DEPTH_DAMPING = 0.5f;

        private struct Star
        {
            public Vector3 position;
            public Vector3 previousPosition;
            public float z;
            public float twinklePhase;
            public float twinkleSpeed;
            public float baseSize;
            public Color color;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // Initialize optimizer first
            m_optimizer = new StarfieldOptimizer();
        }

        private void Start()
        {
            // Lock position to world origin
            transform.position = Vector3.zero;

            // Auto-size starfield to cover arena boundary
            if (m_autoSizeToArena)
            {
                float arenaRadius = ConfigProvider.Player?.arenaRadius ?? 30f;
                // Make starfield radius larger than arena to ensure full coverage
                m_starFieldRadius = arenaRadius * 1.5f;
            }

            CreateParticleSystem();
            InitializeStars();

            if (m_enableNebula)
            {
                m_nebulaSystem = new NebulaSystem(
                    transform,
                    m_nebulaCount,
                    m_nebulaSize,
                    m_nebulaIntensity,
                    m_nebulaMoveSpeed,
                    m_starFieldDepth,
                    m_optimizer
                );
            }

            if (m_enableGrid)
            {
                m_gridRenderer = new StarGridRenderer(
                    transform,
                    m_gridColor,
                    m_gridHorizonY,
                    m_optimizer
                );
            }

            if (m_enableScanlines)
            {
                m_scanlineEffect = new ScanlineEffect(transform, m_scanlineIntensity);
            }

            // Create trail renderer for motion trails
            if (m_enableTrails)
            {
                m_trailRenderer = gameObject.AddComponent<StarTrailRenderer>();
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {

            UnsubscribeFromEvents();

            // Clean up sub-systems
            m_nebulaSystem?.Destroy();
            m_gridRenderer?.Destroy();
            m_scanlineEffect?.Destroy();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        private void OnGameStarted(GameStartedEvent evt) => SetSpeed(3f);
        private void OnGameOver(GameOverEvent evt) => SetSpeed(1f);
        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            SetSpeed(5f);
            Invoke(nameof(ResetSpeed), 2f);
        }
        private void ResetSpeed() => SetSpeed(2f);

        private void Update()
        {
            if (m_stars == null) return;

            m_time += Time.deltaTime;

            // Update stars (allocation-free)
            for (int i = 0; i < m_stars.Length; i++)
            {
                UpdateStar(ref m_stars[i], Time.deltaTime);
            }

            UpdateParticles();

            // Update sub-systems
            m_nebulaSystem?.UpdateNebulae(Time.deltaTime);
            m_gridRenderer?.UpdateGrid(Time.deltaTime, m_speed);
            m_scanlineEffect?.UpdateScanlines(Time.deltaTime);
        }

        private void UpdateStar(ref Star star, float deltaTime)
        {
            star.previousPosition = star.position;
            star.z -= m_speed * SPEED_MULTIPLIER * deltaTime;
            star.twinklePhase += star.twinkleSpeed * deltaTime;

            if (star.z <= DEPTH_THRESHOLD)
            {
                star = CreateStar(false);
                star.previousPosition = star.position;
            }
            else
            {
                // Update position (z changes, x/y stay same in field space)
                m_optimizer.UpdateStarPosition(ref star.position, star.position.x, star.position.y, star.z);
            }
        }

        private void UpdateParticles()
        {
            // Clear trails for this frame
            if (m_trailRenderer != null)
            {
                m_trailRenderer.ClearTrails();
            }

            // Use world origin since starfield is locked to 0,0,0
            Vector3 origin = Vector3.zero;

            for (int i = 0; i < m_stars.Length; i++)
            {
                ref Star star = ref m_stars[i];

                // Calculate world position (allocation-free, locked to origin)
                Vector3 worldPos = m_optimizer.CalculateWorldPosition(
                    star.position,
                    star.z,
                    m_starFieldDepth,
                    origin
                );

                // Calculate previous world position for trails
                Vector3 prevWorldPos = m_optimizer.CalculateWorldPosition(
                    star.previousPosition,
                    star.z + m_speed * SPEED_MULTIPLIER * Time.deltaTime, // Approximate previous z
                    m_starFieldDepth,
                    origin
                );

                // Depth-based size and brightness
                float depthFactor = (m_starFieldDepth - star.z) / m_starFieldDepth;
                float size = Mathf.Max(MIN_SIZE, star.baseSize * depthFactor * SIZE_MULTIPLIER);

                // Twinkling effect
                float twinkle = TWINKLE_BASE +
                    Mathf.Sin(star.twinklePhase * m_twinkleSpeed) * TWINKLE_AMPLITUDE *
                    (1f - depthFactor * TWINKLE_DEPTH_DAMPING);
                float brightness = Mathf.Min(1f, depthFactor * twinkle);

                // Add motion trail for close/fast stars
                if (m_enableTrails && m_trailRenderer != null && m_speed > 0.5f)
                {
                    m_trailRenderer.AddTrail(worldPos, prevWorldPos, depthFactor, star.color, m_speed);
                }

                // Set particle (allocation-free color)
                m_particles[i].position = worldPos;
                m_particles[i].startSize = size;
                m_particles[i].startColor = m_optimizer.CalculateParticleColor(star.color, brightness);
            }

            m_starParticles.SetParticles(m_particles, m_stars.Length);
        }

        #region Initialization

        private void CreateParticleSystem()
        {
            if (m_starParticles == null)
            {
                m_starParticles = StarParticleFactory.CreateStarParticleSystem(transform, m_starCount, m_maxStarSize);
            }

            m_particles = new ParticleSystem.Particle[m_starCount];
        }

        private void InitializeStars()
        {
            m_stars = new Star[m_starCount];

            for (int i = 0; i < m_starCount; i++)
            {
                m_stars[i] = CreateStar(true);
            }

            PreSimulate(5f);
            UpdateParticles();
            m_starParticles.Play();
        }

        private Star CreateStar(bool randomZ)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, m_starFieldRadius);
            float z = randomZ ? Random.Range(10f, m_starFieldDepth) : m_starFieldDepth;

            Vector3 pos = m_optimizer.CreateStarPosition(angle, distance, z);

            return new Star
            {
                position = pos,
                previousPosition = pos,
                z = z,
                twinklePhase = Random.Range(0f, Mathf.PI * 2f),
                twinkleSpeed = Random.Range(0.5f, 3f),
                baseSize = Random.Range(m_minStarSize, m_maxStarSize),
                color = GetRandomStarColor()
            };
        }

        private Color GetRandomStarColor()
        {
            float seed = Random.value;

            if (seed < 0.50f) return m_whiteStarColor;
            if (seed < 0.70f) return m_blueWhiteStarColor;
            if (seed < 0.82f) return m_yellowWhiteStarColor;
            if (seed < 0.88f) return m_orangeStarColor;
            if (seed < 0.92f) return m_cyanStarColor;
            if (seed < 0.97f) return m_lightBlueStarColor;
            return m_redStarColor;
        }

        private void PreSimulate(float seconds)
        {
            float dt = 1f / 60f;
            int frames = Mathf.RoundToInt(seconds / dt);

            for (int frame = 0; frame < frames; frame++)
            {
                for (int i = 0; i < m_stars.Length; i++)
                {
                    UpdateStar(ref m_stars[i], dt);
                }
                m_time += dt;
            }
        }

        #endregion

        #region Public API

        public void SetSpeed(float speed) => m_speed = Mathf.Clamp(speed, m_minSpeed, m_maxSpeed);
        public float GetSpeed() => m_speed;

        public void Pause()
        {
            enabled = false;
            m_starParticles?.Pause();
        }

        public void Resume()
        {
            enabled = true;
            m_starParticles?.Play();
        }

        public void Reset()
        {
            if (m_starParticles != null && m_starParticles.isPlaying)
            {
                m_starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            InitializeStars();
        }

        public void SetStarCount(int count)
        {
            m_starCount = Mathf.Max(100, count);
            m_starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = m_starParticles.main;
            main.maxParticles = m_starCount;

            m_particles = new ParticleSystem.Particle[m_starCount];
            InitializeStars();
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Speed Up")]
        private void DebugSpeedUp() => SetSpeed(m_speed + 1f);

        [ContextMenu("Debug: Speed Down")]
        private void DebugSpeedDown() => SetSpeed(m_speed - 1f);

        [ContextMenu("Debug: Reset")]
        private void DebugReset() => Reset();

        #endregion
    }
}
