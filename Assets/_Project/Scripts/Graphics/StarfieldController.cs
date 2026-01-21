using UnityEngine;
using UnityEngine.Rendering;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// 80s Arcade-style flying starfield background.
    /// Creates a "flying through space" effect with twinkling stars,
    /// motion trails, and nebula clouds.
    /// Based on TypeScript Starfield.ts.
    /// </summary>
    public class StarfieldController : MonoBehaviour
    {
        public static StarfieldController Instance { get; private set; }

        [Header("Star Configuration")]
        [SerializeField] private int _starCount = 400;
        [SerializeField] private float _starFieldDepth = 100f;
        [SerializeField] private float _starFieldRadius = 50f;
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
        [SerializeField] private Material _nebulaMaterial;
        [SerializeField] private Material _gridMaterial;

        // Star data
        private Star[] _stars;
        private ParticleSystem.Particle[] _particles;
        private float _time;

        // Nebula objects
        private GameObject[] _nebulaObjects;

        // Grid/scanline objects
        private LineRenderer _gridRenderer;
        private Material _scanlineMaterial;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CreateParticleSystem();
            InitializeStars();

            if (_enableNebula)
            {
                CreateNebulae();
            }

            if (_enableGrid)
            {
                CreateGrid();
            }

            if (_enableScanlines)
            {
                CreateScanlines();
            }

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            // Speed up during gameplay, slow down in menus
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

        private void OnGameStarted(GameStartedEvent evt)
        {
            SetSpeed(3f); // Faster during gameplay
        }

        private void OnGameOver(GameOverEvent evt)
        {
            SetSpeed(1f); // Slower in game over
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            SetSpeed(5f); // Speed burst on level complete
            Invoke(nameof(ResetSpeed), 2f);
        }

        private void ResetSpeed()
        {
            SetSpeed(2f);
        }

        private void CreateParticleSystem()
        {
            if (_starParticles == null)
            {
                // Create particle system for stars
                GameObject psGO = new GameObject("StarParticles");
                psGO.transform.SetParent(transform);
                psGO.transform.localPosition = Vector3.zero;

                _starParticles = psGO.AddComponent<ParticleSystem>();

                var main = _starParticles.main;
                main.loop = true;
                main.playOnAwake = true;
                main.maxParticles = _starCount;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = float.MaxValue;
                main.startSpeed = 0;
                main.startSize = _maxStarSize;
                main.startColor = Color.white;

                // Disable emission - we'll manually manage particles
                var emission = _starParticles.emission;
                emission.enabled = false;

                // Renderer settings
                var renderer = psGO.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;

                // Create additive particle material for glowing stars
                Material starMaterial = CreateStarMaterial();
                if (starMaterial != null)
                {
                    renderer.material = starMaterial;
                }
                else if (renderer.material == null)
                {
                    renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                }
            }

            _particles = new ParticleSystem.Particle[_starCount];
        }

        /// <summary>
        /// Create an additive material with a procedural circle texture for glowing stars
        /// </summary>
        private Material CreateStarMaterial()
        {
            // Use the built-in Sprites/Default shader which works reliably
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                // Fallback to legacy particle shader
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                Debug.LogError("[Starfield] Could not find any suitable shader!");
                return null;
            }

            Material mat = new Material(shader);

            // Create a procedural circle texture (soft glow)
            Texture2D starTexture = CreateCircleTexture(64);
            mat.mainTexture = starTexture;

            // For Sprites/Default, just set the color to white
            mat.color = Color.white;

            Debug.Log($"[Starfield] Created star material with shader: {shader.name}");

            return mat;
        }

        /// <summary>
        /// Create a soft circular gradient texture for star particles
        /// </summary>
        private Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = size / 2f;
            float maxDist = center;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft falloff from center
                    float alpha = 1f - Mathf.Clamp01(dist / maxDist);
                    // Apply power curve for softer glow
                    alpha = Mathf.Pow(alpha, 1.5f);

                    // Bright center, fading edges
                    float brightness = Mathf.Pow(alpha, 0.5f);

                    pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private void InitializeStars()
        {
            _stars = new Star[_starCount];

            for (int i = 0; i < _starCount; i++)
            {
                _stars[i] = CreateStar(true);
            }

            // Pre-simulate to avoid spawn-in effect
            PreSimulate(5f);

            UpdateParticles();
            _starParticles.Play();
        }

        private Star CreateStar(bool randomZ)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, _starFieldRadius);

            float z = randomZ
                ? Random.Range(10f, _starFieldDepth)
                : _starFieldDepth;

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                z
            );

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

            // Realistic star color distribution
            if (seed < 0.50f) return _whiteStarColor;           // Most common
            if (seed < 0.70f) return _blueWhiteStarColor;       // Hot stars
            if (seed < 0.82f) return _yellowWhiteStarColor;     // Sun-like
            if (seed < 0.88f) return _orangeStarColor;          // Cooler stars
            if (seed < 0.92f) return _cyanStarColor;            // Rare accent
            if (seed < 0.97f) return new Color(0.75f, 0.75f, 1f); // Light blue
            return _redStarColor;                                // Red giants
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

        private void Update()
        {
            _time += Time.deltaTime;

            for (int i = 0; i < _stars.Length; i++)
            {
                UpdateStar(ref _stars[i], Time.deltaTime);
            }

            UpdateParticles();
            UpdateNebulae();
            UpdateGrid();
        }

        private void UpdateStar(ref Star star, float deltaTime)
        {
            star.previousPosition = star.position;
            star.z -= _speed * 10f * deltaTime;
            star.twinklePhase += star.twinkleSpeed * deltaTime;

            if (star.z <= 1f)
            {
                // Reset star to back of field
                star = CreateStar(false);
                star.previousPosition = star.position;
            }
            else
            {
                // Update position based on z
                float scale = _starFieldDepth / star.z;
                star.position = new Vector3(
                    star.position.x,
                    star.position.y,
                    star.z
                );
            }
        }

        private void UpdateParticles()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;

            for (int i = 0; i < _stars.Length; i++)
            {
                ref Star star = ref _stars[i];

                // Calculate screen position based on z depth
                float scale = _starFieldDepth / Mathf.Max(star.z, 1f);

                // Convert to world position relative to camera
                Vector3 worldPos = camPos + new Vector3(
                    star.position.x * scale * 0.1f,
                    star.position.y * scale * 0.1f,
                    star.z * 0.5f
                );

                // Depth-based size and brightness
                float depthFactor = (_starFieldDepth - star.z) / _starFieldDepth;
                float size = Mathf.Max(0.01f, star.baseSize * depthFactor * 2.5f);

                // Twinkling effect using configured speed
                float twinkle = 0.7f + Mathf.Sin(star.twinklePhase * _twinkleSpeed) * 0.3f * (1f - depthFactor * 0.5f);
                float brightness = Mathf.Min(1f, depthFactor * twinkle);

                // Trail effect - stretch particles based on speed
                if (_enableTrails && _speed > 1f)
                {
                    float trailFactor = Mathf.Min(_trailLength * (_speed / _maxSpeed), 0.5f);
                    // Use rotation to create trail direction illusion
                    _particles[i].rotation = Mathf.Atan2(star.position.y, star.position.x) * Mathf.Rad2Deg;
                }

                // Set particle
                _particles[i].position = worldPos;
                _particles[i].startSize = size;
                _particles[i].startColor = new Color(
                    star.color.r * brightness,
                    star.color.g * brightness,
                    star.color.b * brightness,
                    brightness
                );
            }

            _starParticles.SetParticles(_particles, _stars.Length);
        }

        private void CreateNebulae()
        {
            _nebulaObjects = new GameObject[_nebulaCount];

            for (int i = 0; i < _nebulaCount; i++)
            {
                GameObject nebula = GameObject.CreatePrimitive(PrimitiveType.Quad);
                nebula.name = $"Nebula_{i}";
                nebula.transform.SetParent(transform);

                // Remove collider
                Destroy(nebula.GetComponent<Collider>());

                // Position far in background
                nebula.transform.localPosition = new Vector3(
                    Random.Range(-20f, 20f),
                    Random.Range(-10f, 10f),
                    _starFieldDepth * 0.8f
                );
                nebula.transform.localScale = Vector3.one * _nebulaSize;

                // Create nebula material
                var renderer = nebula.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Sprites/Default"));

                // Generate nebula color
                float hue = (i * 0.33f + Random.Range(-0.1f, 0.1f)) % 1f;
                Color nebulaColor = Color.HSVToRGB(hue, 1f, 0.5f);
                nebulaColor.a = _nebulaIntensity;
                mat.color = nebulaColor;

                renderer.material = mat;
                renderer.sortingOrder = -100;

                _nebulaObjects[i] = nebula;
            }
        }

        private void UpdateNebulae()
        {
            if (_nebulaObjects == null) return;

            for (int i = 0; i < _nebulaObjects.Length; i++)
            {
                if (_nebulaObjects[i] == null) continue;

                // Slowly drift nebulae
                Vector3 pos = _nebulaObjects[i].transform.localPosition;
                pos.x += Mathf.Sin(_time * _nebulaMoveSpeed + i * 2f) * Time.deltaTime * 0.5f;
                pos.y += Mathf.Cos(_time * _nebulaMoveSpeed * 0.8f + i * 2f) * Time.deltaTime * 0.3f;
                _nebulaObjects[i].transform.localPosition = pos;

                // Pulse size
                float pulse = 1f + Mathf.Sin(_time * 0.2f + i) * 0.1f;
                _nebulaObjects[i].transform.localScale = Vector3.one * _nebulaSize * pulse;

                // Rotate color hue slowly
                var renderer = _nebulaObjects[i].GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    float hue = ((i * 0.33f) + _time * 0.02f) % 1f;
                    Color nebulaColor = Color.HSVToRGB(hue, 1f, 0.5f);
                    nebulaColor.a = _nebulaIntensity;
                    renderer.material.color = nebulaColor;
                }
            }
        }

        private void CreateGrid()
        {
            // Create a perspective grid floor effect (80s style)
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(transform);
            gridObj.transform.localPosition = new Vector3(0, _gridHorizonY - 15f, 30f);
            gridObj.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            _gridRenderer = gridObj.AddComponent<LineRenderer>();
            _gridRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _gridRenderer.startColor = _gridColor;
            _gridRenderer.endColor = _gridColor;
            _gridRenderer.startWidth = 0.05f;
            _gridRenderer.endWidth = 0.05f;
            _gridRenderer.sortingOrder = -50;

            // Create grid lines
            int gridLines = 20;
            float gridSize = 40f;
            int totalPoints = (gridLines * 2 + 1) * 4;
            _gridRenderer.positionCount = totalPoints;

            int index = 0;
            float spacing = gridSize / gridLines;

            // Horizontal lines (perpendicular to camera)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                _gridRenderer.SetPosition(index++, new Vector3(-gridSize, 0, i * spacing));
                _gridRenderer.SetPosition(index++, new Vector3(gridSize, 0, i * spacing));
            }

            // Vertical lines (going into distance)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                _gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, -gridSize));
                _gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, gridSize));
            }
        }

        private void CreateScanlines()
        {
            // Create overlay quad with scanline effect
            GameObject scanlineObj = new GameObject("Scanlines");
            scanlineObj.transform.SetParent(transform);

            // Position in front of camera
            var cam = Camera.main;
            if (cam != null)
            {
                scanlineObj.transform.position = cam.transform.position + cam.transform.forward * 1f;
                scanlineObj.transform.rotation = cam.transform.rotation;
            }

            // Create a simple scanline texture
            Texture2D scanlineTex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            scanlineTex.filterMode = FilterMode.Point;
            scanlineTex.wrapMode = TextureWrapMode.Repeat;
            scanlineTex.SetPixel(0, 0, new Color(0, 0, 0, _scanlineIntensity));
            scanlineTex.SetPixel(0, 1, new Color(0, 0, 0, 0));
            scanlineTex.SetPixel(0, 2, new Color(0, 0, 0, _scanlineIntensity));
            scanlineTex.SetPixel(0, 3, new Color(0, 0, 0, 0));
            scanlineTex.Apply();

            _scanlineMaterial = new Material(Shader.Find("Sprites/Default"));
            _scanlineMaterial.mainTexture = scanlineTex;
            _scanlineMaterial.mainTextureScale = new Vector2(1, 100);

            // This would need a screen-space quad or post-process to work properly
            // For now, just mark as created - full implementation needs camera overlay
        }

        private void UpdateGrid()
        {
            if (!_enableGrid || _gridRenderer == null) return;

            // Scroll grid based on speed for motion effect
            Vector3 pos = _gridRenderer.transform.localPosition;
            float scrollAmount = _speed * Time.deltaTime * 2f;
            pos.z = 30f + ((_time * _speed) % 4f);
            _gridRenderer.transform.localPosition = pos;

            // Fade grid color based on distance from horizon
            Color c = _gridColor;
            c.a = _gridColor.a * (0.5f + Mathf.Sin(_time * 0.5f) * 0.2f);
            _gridRenderer.startColor = c;
            _gridRenderer.endColor = c;
        }

        #region Public API

        /// <summary>
        /// Set starfield speed (clamped to min/max)
        /// </summary>
        public void SetSpeed(float speed)
        {
            _speed = Mathf.Clamp(speed, _minSpeed, _maxSpeed);
        }

        /// <summary>
        /// Get current speed
        /// </summary>
        public float GetSpeed() => _speed;

        /// <summary>
        /// Pause the starfield
        /// </summary>
        public void Pause()
        {
            enabled = false;
            if (_starParticles != null)
            {
                _starParticles.Pause();
            }
        }

        /// <summary>
        /// Resume the starfield
        /// </summary>
        public void Resume()
        {
            enabled = true;
            if (_starParticles != null)
            {
                _starParticles.Play();
            }
        }

        /// <summary>
        /// Reset and reinitialize stars
        /// </summary>
        public void Reset()
        {
            // Stop particle system before reinitializing
            if (_starParticles != null && _starParticles.isPlaying)
            {
                _starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            InitializeStars();
        }

        /// <summary>
        /// Set star count (requires reinitialization)
        /// </summary>
        public void SetStarCount(int count)
        {
            _starCount = Mathf.Max(100, count);

            // Stop particle system before modifying it
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
