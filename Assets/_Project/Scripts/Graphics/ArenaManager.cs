using UnityEngine;
using System.Collections;
using NeuralBreak.Core;
using Z13.Core;

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

        [Header("Settings")]
        [SerializeField] private ArenaTheme m_currentTheme = ArenaTheme.Cyber;
        [SerializeField] private float m_transitionDuration = 2f;
        [SerializeField] private bool m_autoChangeOnLevel = true;
        [SerializeField] private int m_levelsPerTheme = 15;

        [Header("Background")]
        [SerializeField] private SpriteRenderer m_backgroundRenderer;
        [SerializeField] private Camera m_mainCamera;

        [Header("Grid")]
        [SerializeField] private bool m_showGrid = true;
        [SerializeField] private float m_gridSize = 2f;
        [SerializeField] private float m_gridAlpha = 0.1f;

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
        private GameObject m_gridObject;
        private LineRenderer[] m_gridLines;
        private Coroutine m_transitionCoroutine;

        // Cached references
        private StarfieldController m_starfield;

        public ArenaTheme CurrentTheme => m_currentTheme;
        public Color PrimaryColor => ThemeColors[(int)m_currentTheme].primary;
        public Color SecondaryColor => ThemeColors[(int)m_currentTheme].secondary;
        public Color AccentColor => ThemeColors[(int)m_currentTheme].accent;
        public Color BackgroundColor => ThemeColors[(int)m_currentTheme].background;

        private void Awake()
        {
        }

        private void Start()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
            }

            CreateBackground();
            if (m_showGrid) CreateGrid();
            ApplyTheme(m_currentTheme, false);

            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

        }

        private void CreateBackground()
        {
            if (m_backgroundRenderer != null) return;

            var bgGO = new GameObject("ArenaBackground");
            bgGO.transform.SetParent(transform);
            bgGO.transform.position = new Vector3(0, 0, 10);

            m_backgroundRenderer = bgGO.AddComponent<SpriteRenderer>();
            m_backgroundRenderer.sprite = CreateBackgroundSprite();
            m_backgroundRenderer.sortingOrder = -1000;

            // Scale to fill view
            float camHeight = m_mainCamera != null ? m_mainCamera.orthographicSize * 2f : 20f;
            float camWidth = camHeight * (m_mainCamera != null ? m_mainCamera.aspect : 1.77f);
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
            m_gridObject = new GameObject("Grid");
            m_gridObject.transform.SetParent(transform);
            m_gridObject.transform.position = new Vector3(0, 0, 5);

            float camHeight = m_mainCamera != null ? m_mainCamera.orthographicSize * 2f : 20f;
            float camWidth = camHeight * (m_mainCamera != null ? m_mainCamera.aspect : 1.77f);

            int horizontalLines = Mathf.CeilToInt(camHeight / m_gridSize) + 4;
            int verticalLines = Mathf.CeilToInt(camWidth / m_gridSize) + 4;

            m_gridLines = new LineRenderer[horizontalLines + verticalLines];
            int lineIndex = 0;

            // Horizontal lines
            for (int i = 0; i < horizontalLines; i++)
            {
                float y = (i - horizontalLines / 2) * m_gridSize;
                m_gridLines[lineIndex++] = CreateGridLine(
                    new Vector3(-camWidth, y, 5),
                    new Vector3(camWidth, y, 5)
                );
            }

            // Vertical lines
            for (int i = 0; i < verticalLines; i++)
            {
                float x = (i - verticalLines / 2) * m_gridSize;
                m_gridLines[lineIndex++] = CreateGridLine(
                    new Vector3(x, -camHeight, 5),
                    new Vector3(x, camHeight, 5)
                );
            }
        }

        private LineRenderer CreateGridLine(Vector3 start, Vector3 end)
        {
            var lineGO = new GameObject("GridLine");
            lineGO.transform.SetParent(m_gridObject.transform);

            var lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 1f, 1f, m_gridAlpha);
            lr.endColor = new Color(1f, 1f, 1f, m_gridAlpha);
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
            if (!m_autoChangeOnLevel) return;

            // Change theme based on level
            int themeIndex = (evt.levelNumber - 1) / m_levelsPerTheme;
            themeIndex = themeIndex % ThemeColors.Length;

            ArenaTheme newTheme = (ArenaTheme)themeIndex;
            if (newTheme != m_currentTheme)
            {
                SetTheme(newTheme, true);
            }
        }

        /// <summary>
        /// Set arena theme
        /// </summary>
        public void SetTheme(ArenaTheme theme, bool animate = true)
        {
            if (animate && m_transitionCoroutine != null)
            {
                StopCoroutine(m_transitionCoroutine);
            }

            if (animate)
            {
                m_transitionCoroutine = StartCoroutine(TransitionToTheme(theme));
            }
            else
            {
                ApplyTheme(theme, false);
            }
        }

        private void ApplyTheme(ArenaTheme theme, bool instant)
        {
            m_currentTheme = theme;
            var colors = ThemeColors[(int)theme];

            // Apply background color
            if (m_mainCamera != null)
            {
                m_mainCamera.backgroundColor = colors.background;
            }

            if (m_backgroundRenderer != null)
            {
                m_backgroundRenderer.color = colors.background * 2f;
            }

            // Apply grid color
            if (m_gridLines != null)
            {
                Color gridColor = colors.primary;
                gridColor.a = m_gridAlpha;

                foreach (var line in m_gridLines)
                {
                    if (line != null)
                    {
                        line.startColor = gridColor;
                        line.endColor = gridColor;
                    }
                }
            }

            // Notify starfield if exists
            if (m_starfield == null)
            {
                var starfieldGO = GameObject.Find("Starfield");
                if (starfieldGO != null)
                {
                    m_starfield = starfieldGO.GetComponent<StarfieldController>();
                }
            }

            if (m_starfield != null)
            {
                // Could update starfield colors here
            }

            Debug.Log($"[ArenaManager] Applied theme: {theme}");
        }

        private IEnumerator TransitionToTheme(ArenaTheme newTheme)
        {
            var oldColors = ThemeColors[(int)m_currentTheme];
            var newColors = ThemeColors[(int)newTheme];

            float elapsed = 0f;
            while (elapsed < m_transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_transitionDuration;
                t = Mathf.SmoothStep(0, 1, t);

                // Lerp background
                if (m_mainCamera != null)
                {
                    m_mainCamera.backgroundColor = Color.Lerp(oldColors.background, newColors.background, t);
                }

                if (m_backgroundRenderer != null)
                {
                    m_backgroundRenderer.color = Color.Lerp(oldColors.background * 2f, newColors.background * 2f, t);
                }

                // Lerp grid
                if (m_gridLines != null)
                {
                    Color oldGrid = oldColors.primary;
                    oldGrid.a = m_gridAlpha;
                    Color newGrid = newColors.primary;
                    newGrid.a = m_gridAlpha;
                    Color currentGrid = Color.Lerp(oldGrid, newGrid, t);

                    foreach (var line in m_gridLines)
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

            m_currentTheme = newTheme;
            ApplyTheme(newTheme, true);
            m_transitionCoroutine = null;
        }

        /// <summary>
        /// Get theme color for other systems to use
        /// </summary>
        public Color GetThemeColor(int index)
        {
            var colors = ThemeColors[(int)m_currentTheme];
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
            int next = ((int)m_currentTheme + 1) % ThemeColors.Length;
            SetTheme((ArenaTheme)next, true);
        }

        #endregion
    }
}
