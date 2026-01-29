using UnityEngine;
using UnityEngine.Rendering;
using NeuralBreak.Core;
using NeuralBreak.Config;

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
        [SerializeField] private int _starCount = 400;
        [SerializeField] private float _starFieldDepth = 100f;
        [SerializeField] private float _starFieldRadius = 50f;
        [SerializeField] private bool _autoSizeToArena = true;
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _minSpeed = 0.5f;
        [SerializeField] private float _maxSpeed = 10f;

        [Header("Star Appearance")]
        [SerializeField] private float _minStarSize = 0.02f;
        [SerializeField] private float _maxStarSize = 0.15f;
        [SerializeField] private float _twinkleSpeed = 2f;
        [SerializeField] private float _trailLength = 0.3f;
        [SerializeField] private bool _enableTrails = true;

        [Header("Star Colors")]
        [SerializeField] private Color _whiteStarColor = Color.white;
        [SerializeField] private Color _blueWhiteStarColor = new Color(0.88f, 0.91f, 1f);
        [SerializeField] private Color _yellowWhiteStarColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private Color _orangeStarColor = new Color(1f, 0.88f, 0.75f);
        [SerializeField] private Color _cyanStarColor = Color.cyan;
        [SerializeField] private Color _lightBlueStarColor = new Color(0.75f, 0.75f, 1f);
        [SerializeField] private Color _redStarColor = new Color(1f, 0.75f, 0.75f);

        [Header("Nebula")]
        [SerializeField] private bool _enableNebula = true;
        [SerializeField] private int _nebulaCount = 3;
        [SerializeField] private float _nebulaSize = 15f;
        [SerializeField] private float _nebulaIntensity = 0.1f;
        [SerializeField] private float _nebulaMoveSpeed = 0.1f;

        [Header("Grid")]
        [SerializeField] private bool _enableGrid = true;
        [SerializeField] private Color _gridColor = new Color(0f, 1f, 1f, 0.06f);
        [SerializeField] private float _gridHorizonY = 0f;

        [Header("Scanlines")]
        [SerializeField] private bool _enableScanlines = false;
        [SerializeField] private float _scanlineIntensity = 0.1f;

        [Header("References")]
        [SerializeField] private ParticleSystem _starParticles;

        // Star data
        private Star[] _stars;
        private ParticleSystem.Particle[] _particles;
        private float _time;

        // Sub-systems
        private StarfieldOptimizer _optimizer;
        private NebulaSystem _nebulaSystem;
        private StarGridRenderer _gridRenderer;
        private ScanlineEffect _scanlineEffect;
        private StarTrailRenderer _trailRenderer;

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
            _optimizer = new StarfieldOptimizer();
        }

        private void Start()
        {
            // Lock position to world origin
            transform.position = Vector3.zero;

            // Auto-size starfield to cover arena boundary
            if (_autoSizeToArena)
            {
                float arenaRadius = ConfigProvider.Player?.arenaRadius ?? 30f;
                // Make starfield radius larger than arena to ensure full coverage
                _starFieldRadius = arenaRadius * 1.5f;
            }

            CreateParticleSystem();
            InitializeStars();

            if (_enableNebula)
            {
                _nebulaSystem = new NebulaSystem(
                    transform,
                    _nebulaCount,
                    _nebulaSize,
                    _nebulaIntensity,
                    _nebulaMoveSpeed,
                    _starFieldDepth,
                    _optimizer
                );
            }

            if (_enableGrid)
            {
                _gridRenderer = new StarGridRenderer(
                    transform,
                    _gridColor,
                    _gridHorizonY,
                    _optimizer
                );
            }

            if (_enableScanlines)
            {
                _scanlineEffect = new ScanlineEffect(transform, _scanlineIntensity);
            }

            // Create trail renderer for motion trails
            if (_enableTrails)
            {
                _trailRenderer = gameObject.AddComponent<StarTrailRenderer>();
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {

            UnsubscribeFromEvents();

            // Clean up sub-systems
            _nebulaSystem?.Destroy();
            _gridRenderer?.Destroy();
            _scanlineEffect?.Destroy();
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
            if (_stars == null) return;

            _time += Time.deltaTime;

            // Update stars (allocation-free)
            for (int i = 0; i < _stars.Length; i++)
            {
                UpdateStar(ref _stars[i], Time.deltaTime);
            }

            UpdateParticles();

            // Update sub-systems
            _nebulaSystem?.UpdateNebulae(Time.deltaTime);
            _gridRenderer?.UpdateGrid(Time.deltaTime, _speed);
            _scanlineEffect?.UpdateScanlines(Time.deltaTime);
        }

        private void UpdateStar(ref Star star, float deltaTime)
        {
            star.previousPosition = star.position;
            star.z -= _speed * SPEED_MULTIPLIER * deltaTime;
            star.twinklePhase += star.twinkleSpeed * deltaTime;

            if (star.z <= DEPTH_THRESHOLD)
            {
                star = CreateStar(false);
                star.previousPosition = star.position;
            }
            else
            {
                // Update position (z changes, x/y stay same in field space)
                _optimizer.UpdateStarPosition(ref star.position, star.position.x, star.position.y, star.z);
            }
        }

        private void UpdateParticles()
        {
            // Clear trails for this frame
            if (_trailRenderer != null)
            {
                _trailRenderer.ClearTrails();
            }

            // Use world origin since starfield is locked to 0,0,0
            Vector3 origin = Vector3.zero;

            for (int i = 0; i < _stars.Length; i++)
            {
                ref Star star = ref _stars[i];

                // Calculate world position (allocation-free, locked to origin)
                Vector3 worldPos = _optimizer.CalculateWorldPosition(
                    star.position,
                    star.z,
                    _starFieldDepth,
                    origin
                );

                // Calculate previous world position for trails
                Vector3 prevWorldPos = _optimizer.CalculateWorldPosition(
                    star.previousPosition,
                    star.z + _speed * SPEED_MULTIPLIER * Time.deltaTime, // Approximate previous z
                    _starFieldDepth,
                    origin
                );

                // Depth-based size and brightness
                float depthFactor = (_starFieldDepth - star.z) / _starFieldDepth;
                float size = Mathf.Max(MIN_SIZE, star.baseSize * depthFactor * SIZE_MULTIPLIER);

                // Twinkling effect
                float twinkle = TWINKLE_BASE +
                    Mathf.Sin(star.twinklePhase * _twinkleSpeed) * TWINKLE_AMPLITUDE *
                    (1f - depthFactor * TWINKLE_DEPTH_DAMPING);
                float brightness = Mathf.Min(1f, depthFactor * twinkle);

                // Add motion trail for close/fast stars
                if (_enableTrails && _trailRenderer != null && _speed > 0.5f)
                {
                    _trailRenderer.AddTrail(worldPos, prevWorldPos, depthFactor, star.color, _speed);
                }

                // Set particle (allocation-free color)
                _particles[i].position = worldPos;
                _particles[i].startSize = size;
                _particles[i].startColor = _optimizer.CalculateParticleColor(star.color, brightness);
            }

            _starParticles.SetParticles(_particles, _stars.Length);
        }

        #region Initialization

        private void CreateParticleSystem()
        {
            if (_starParticles == null)
            {
                _starParticles = StarParticleFactory.CreateStarParticleSystem(transform, _starCount, _maxStarSize);
            }

            _particles = new ParticleSystem.Particle[_starCount];
        }

        private void InitializeStars()
        {
            _stars = new Star[_starCount];

            for (int i = 0; i < _starCount; i++)
            {
                _stars[i] = CreateStar(true);
            }

            PreSimulate(5f);
            UpdateParticles();
            _starParticles.Play();
        }

        private Star CreateStar(bool randomZ)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, _starFieldRadius);
            float z = randomZ ? Random.Range(10f, _starFieldDepth) : _starFieldDepth;

            Vector3 pos = _optimizer.CreateStarPosition(angle, distance, z);

            return new Star
            {
                position = pos,
                previousPosition = pos,
                z = z,
                twinklePhase = Random.Range(0f, Mathf.PI * 2f),
                twinkleSpeed = Random.Range(0.5f, 3f),
                baseSize = Random.Range(_minStarSize, _maxStarSize),
                color = GetRandomStarColor()
            };
        }

        private Color GetRandomStarColor()
        {
            float seed = Random.value;

            if (seed < 0.50f) return _whiteStarColor;
            if (seed < 0.70f) return _blueWhiteStarColor;
            if (seed < 0.82f) return _yellowWhiteStarColor;
            if (seed < 0.88f) return _orangeStarColor;
            if (seed < 0.92f) return _cyanStarColor;
            if (seed < 0.97f) return _lightBlueStarColor;
            return _redStarColor;
        }

        private void PreSimulate(float seconds)
        {
            float dt = 1f / 60f;
            int frames = Mathf.RoundToInt(seconds / dt);

            for (int frame = 0; frame < frames; frame++)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    UpdateStar(ref _stars[i], dt);
                }
                _time += dt;
            }
        }

        #endregion

        #region Public API

        public void SetSpeed(float speed) => _speed = Mathf.Clamp(speed, _minSpeed, _maxSpeed);
        public float GetSpeed() => _speed;

        public void Pause()
        {
            enabled = false;
            _starParticles?.Pause();
        }

        public void Resume()
        {
            enabled = true;
            _starParticles?.Play();
        }

        public void Reset()
        {
            if (_starParticles != null && _starParticles.isPlaying)
            {
                _starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            InitializeStars();
        }

        public void SetStarCount(int count)
        {
            _starCount = Mathf.Max(100, count);
            _starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _starParticles.main;
            main.maxParticles = _starCount;

            _particles = new ParticleSystem.Particle[_starCount];
            InitializeStars();
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Speed Up")]
        private void DebugSpeedUp() => SetSpeed(_speed + 1f);

        [ContextMenu("Debug: Speed Down")]
        private void DebugSpeedDown() => SetSpeed(_speed - 1f);

        [ContextMenu("Debug: Reset")]
        private void DebugReset() => Reset();

        #endregion
    }
}
