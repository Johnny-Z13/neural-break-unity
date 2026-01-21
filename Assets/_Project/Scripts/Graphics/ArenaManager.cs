using UnityEngine;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Arena theme definitions
    /// </summary>
    public enum ArenaTheme
    {
        Cyber,      // Blue/cyan digital theme
        Void,       // Purple/dark void theme
        Inferno,    // Red/orange fire theme
        Matrix,     // Green matrix code theme
        Neon,       // Pink/magenta neon theme
        Arctic,     // White/ice blue theme
        Sunset      // Orange/purple gradient
    }

    /// <summary>
    /// Manages arena backgrounds and visual themes.
    /// Changes based on level progression.
    /// </summary>
    public class ArenaManager : MonoBehaviour
    {
        public static ArenaManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private ArenaTheme _currentTheme = ArenaTheme.Cyber;
        [SerializeField] private float _transitionDuration = 2f;
        [SerializeField] private bool _autoChangeOnLevel = true;
        [SerializeField] private int _levelsPerTheme = 15;

        [Header("Background")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private Camera _mainCamera;

        [Header("Grid")]
        [SerializeField] private bool _showGrid = true;
        [SerializeField] private float _gridSize = 2f;
        [SerializeField] private float _gridAlpha = 0.1f;

        // Theme colors
        private static readonly (Color primary, Color secondary, Color accent, Color background)[] ThemeColors = new[]
        {
            // Cyber - Blue/cyan
            (new Color(0.2f, 0.6f, 1f), new Color(0f, 1f, 1f), new Color(1f, 1f, 1f), new Color(0.02f, 0.05f, 0.1f)),
            // Void - Purple/dark
            (new Color(0.5f, 0.1f, 0.8f), new Color(0.8f, 0.2f, 1f), new Color(1f, 0.5f, 1f), new Color(0.05f, 0.02f, 0.08f)),
            // Inferno - Red/orange
            (new Color(1f, 0.3f, 0.1f), new Color(1f, 0.6f, 0.2f), new Color(1f, 1f, 0.3f), new Color(0.1f, 0.03f, 0.02f)),
            // Matrix - Green
            (new Color(0.1f, 0.8f, 0.2f), new Color(0.2f, 1f, 0.4f), new Color(0.5f, 1f, 0.5f), new Color(0.02f, 0.06f, 0.03f)),
            // Neon - Pink/magenta
            (new Color(1f, 0.2f, 0.6f), new Color(1f, 0.4f, 0.8f), new Color(0.5f, 1f, 1f), new Color(0.08f, 0.02f, 0.06f)),
            // Arctic - Ice blue/white
            (new Color(0.7f, 0.9f, 1f), new Color(0.5f, 0.8f, 1f), new Color(1f, 1f, 1f), new Color(0.05f, 0.08f, 0.12f)),
            // Sunset - Orange/purple
            (new Color(1f, 0.5f, 0.2f), new Color(0.8f, 0.3f, 0.6f), new Color(1f, 0.8f, 0.4f), new Color(0.08f, 0.04f, 0.08f))
        };

        // State
        private GameObject _gridObject;
        private LineRenderer[] _gridLines;
        private Coroutine _transitionCoroutine;

        public ArenaTheme CurrentTheme => _currentTheme;
        public Color PrimaryColor => ThemeColors[(int)_currentTheme].primary;
        public Color SecondaryColor => ThemeColors[(int)_currentTheme].secondary;
        public Color AccentColor => ThemeColors[(int)_currentTheme].accent;
        public Color BackgroundColor => ThemeColors[(int)_currentTheme].background;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            CreateBackground();
            if (_showGrid) CreateGrid();
            ApplyTheme(_currentTheme, false);

            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CreateBackground()
        {
            if (_backgroundRenderer != null) return;

            var bgGO = new GameObject("ArenaBackground");
            bgGO.transform.SetParent(transform);
            bgGO.transform.position = new Vector3(0, 0, 10);

            _backgroundRenderer = bgGO.AddComponent<SpriteRenderer>();
            _backgroundRenderer.sprite = CreateBackgroundSprite();
            _backgroundRenderer.sortingOrder = -1000;

            // Scale to fill view
            float camHeight = _mainCamera != null ? _mainCamera.orthographicSize * 2f : 20f;
            float camWidth = camHeight * (_mainCamera != null ? _mainCamera.aspect : 1.77f);
            bgGO.transform.localScale = new Vector3(camWidth * 1.5f, camHeight * 1.5f, 1f);
        }

        private Sprite CreateBackgroundSprite()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];

            // Create gradient with some noise
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = (float)y / size;
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.1f;

                    Color c = Color.Lerp(Color.black, new Color(0.05f, 0.05f, 0.1f), t + noise);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void CreateGrid()
        {
            _gridObject = new GameObject("Grid");
            _gridObject.transform.SetParent(transform);
            _gridObject.transform.position = new Vector3(0, 0, 5);

            float camHeight = _mainCamera != null ? _mainCamera.orthographicSize * 2f : 20f;
            float camWidth = camHeight * (_mainCamera != null ? _mainCamera.aspect : 1.77f);

            int horizontalLines = Mathf.CeilToInt(camHeight / _gridSize) + 4;
            int verticalLines = Mathf.CeilToInt(camWidth / _gridSize) + 4;

            _gridLines = new LineRenderer[horizontalLines + verticalLines];
            int lineIndex = 0;

            // Horizontal lines
            for (int i = 0; i < horizontalLines; i++)
            {
                float y = (i - horizontalLines / 2) * _gridSize;
                _gridLines[lineIndex++] = CreateGridLine(
                    new Vector3(-camWidth, y, 5),
                    new Vector3(camWidth, y, 5)
                );
            }

            // Vertical lines
            for (int i = 0; i < verticalLines; i++)
            {
                float x = (i - verticalLines / 2) * _gridSize;
                _gridLines[lineIndex++] = CreateGridLine(
                    new Vector3(x, -camHeight, 5),
                    new Vector3(x, camHeight, 5)
                );
            }
        }

        private LineRenderer CreateGridLine(Vector3 start, Vector3 end)
        {
            var lineGO = new GameObject("GridLine");
            lineGO.transform.SetParent(_gridObject.transform);

            var lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 1f, 1f, _gridAlpha);
            lr.endColor = new Color(1f, 1f, 1f, _gridAlpha);
            lr.sortingOrder = -999;

            return lr;
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Reset to first theme
            SetTheme(ArenaTheme.Cyber, false);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (!_autoChangeOnLevel) return;

            // Change theme based on level
            int themeIndex = (evt.levelNumber - 1) / _levelsPerTheme;
            themeIndex = themeIndex % ThemeColors.Length;

            ArenaTheme newTheme = (ArenaTheme)themeIndex;
            if (newTheme != _currentTheme)
            {
                SetTheme(newTheme, true);
            }
        }

        /// <summary>
        /// Set arena theme
        /// </summary>
        public void SetTheme(ArenaTheme theme, bool animate = true)
        {
            if (animate && _transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            if (animate)
            {
                _transitionCoroutine = StartCoroutine(TransitionToTheme(theme));
            }
            else
            {
                ApplyTheme(theme, false);
            }
        }

        private void ApplyTheme(ArenaTheme theme, bool instant)
        {
            _currentTheme = theme;
            var colors = ThemeColors[(int)theme];

            // Apply background color
            if (_mainCamera != null)
            {
                _mainCamera.backgroundColor = colors.background;
            }

            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.color = colors.background * 2f;
            }

            // Apply grid color
            if (_gridLines != null)
            {
                Color gridColor = colors.primary;
                gridColor.a = _gridAlpha;

                foreach (var line in _gridLines)
                {
                    if (line != null)
                    {
                        line.startColor = gridColor;
                        line.endColor = gridColor;
                    }
                }
            }

            // Notify starfield if exists
            var starfield = FindFirstObjectByType<StarfieldController>();
            if (starfield != null)
            {
                // Could update starfield colors here
            }

            Debug.Log($"[ArenaManager] Applied theme: {theme}");
        }

        private IEnumerator TransitionToTheme(ArenaTheme newTheme)
        {
            var oldColors = ThemeColors[(int)_currentTheme];
            var newColors = ThemeColors[(int)newTheme];

            float elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _transitionDuration;
                t = Mathf.SmoothStep(0, 1, t);

                // Lerp background
                if (_mainCamera != null)
                {
                    _mainCamera.backgroundColor = Color.Lerp(oldColors.background, newColors.background, t);
                }

                if (_backgroundRenderer != null)
                {
                    _backgroundRenderer.color = Color.Lerp(oldColors.background * 2f, newColors.background * 2f, t);
                }

                // Lerp grid
                if (_gridLines != null)
                {
                    Color oldGrid = oldColors.primary;
                    oldGrid.a = _gridAlpha;
                    Color newGrid = newColors.primary;
                    newGrid.a = _gridAlpha;
                    Color currentGrid = Color.Lerp(oldGrid, newGrid, t);

                    foreach (var line in _gridLines)
                    {
                        if (line != null)
                        {
                            line.startColor = currentGrid;
                            line.endColor = currentGrid;
                        }
                    }
                }

                yield return null;
            }

            _currentTheme = newTheme;
            ApplyTheme(newTheme, true);
            _transitionCoroutine = null;
        }

        /// <summary>
        /// Get theme color for other systems to use
        /// </summary>
        public Color GetThemeColor(int index)
        {
            var colors = ThemeColors[(int)_currentTheme];
            return index switch
            {
                0 => colors.primary,
                1 => colors.secondary,
                2 => colors.accent,
                3 => colors.background,
                _ => colors.primary
            };
        }

        #region Debug

        [ContextMenu("Debug: Cyber Theme")]
        private void DebugCyber() => SetTheme(ArenaTheme.Cyber, true);

        [ContextMenu("Debug: Void Theme")]
        private void DebugVoid() => SetTheme(ArenaTheme.Void, true);

        [ContextMenu("Debug: Inferno Theme")]
        private void DebugInferno() => SetTheme(ArenaTheme.Inferno, true);

        [ContextMenu("Debug: Matrix Theme")]
        private void DebugMatrix() => SetTheme(ArenaTheme.Matrix, true);

        [ContextMenu("Debug: Neon Theme")]
        private void DebugNeon() => SetTheme(ArenaTheme.Neon, true);

        [ContextMenu("Debug: Next Theme")]
        private void DebugNextTheme()
        {
            int next = ((int)_currentTheme + 1) % ThemeColors.Length;
            SetTheme((ArenaTheme)next, true);
        }

        #endregion
    }
}
