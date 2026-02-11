using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays XP progress bar with level indicator.
    /// Features smooth fill animation and level-up effects.
    /// </summary>
    public class XPBarDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_fillSpeed = 3f;
        [SerializeField] private float m_levelUpFlashDuration = 0.5f;
        [SerializeField] private int m_levelUpFlashCount = 3;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool m_useThemeColors = true;

        private Color BarColor => m_useThemeColors ? UITheme.Primary : m_customBarColor;
        private Color BarBackgroundColor => m_useThemeColors ? UITheme.BarBackground : m_customBarBackgroundColor;
        private Color LevelUpFlashColor => m_useThemeColors ? UITheme.Good : m_customLevelUpColor;
        private Color LevelTextColor => m_useThemeColors ? UITheme.TextPrimary : Color.white;

        [SerializeField] private Color m_customBarColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color m_customBarBackgroundColor = new Color(0.1f, 0.1f, 0.15f);
        [SerializeField] private Color m_customLevelUpColor = new Color(1f, 1f, 0.4f);

        [Header("Layout")]
        [SerializeField] private float m_barWidth = 200f;
        [SerializeField] private float m_barHeight = 12f;
#pragma warning disable CS0414 // Reserved for rounded corners feature
        [SerializeField] private float m_cornerRadius = 4f;
#pragma warning restore CS0414

        // UI Components
        private Canvas m_canvas;
        private RectTransform m_container;
        private Image m_backgroundBar;
        private Image m_fillBar;
        private TextMeshProUGUI m_levelText;
        private TextMeshProUGUI m_xpText;

        // Animation state
        private float m_currentFill;
        private float m_targetFill;
        private bool m_isLevelingUp;
        private Coroutine m_levelUpCoroutine;

        // Cached reference - avoids FindFirstObjectByType every frame!
        private PlayerLevelSystem m_levelSystem;

        private void Start()
        {
            CreateUI();
            CacheReferences();

            if (m_levelSystem != null)
            {
                m_levelSystem.OnXPChanged += OnXPChanged;
                m_levelSystem.OnLevelUp += OnLevelUp;

                // Initialize with current values
                UpdateDisplay(
                    m_levelSystem.CurrentXP,
                    m_levelSystem.XPForCurrentLevel,
                    m_levelSystem.CurrentLevel
                );
            }

            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CacheReferences()
        {
            if (m_levelSystem == null)
                m_levelSystem = FindFirstObjectByType<PlayerLevelSystem>();
        }

        private void OnDestroy()
        {
            if (m_levelSystem != null)
            {
                m_levelSystem.OnXPChanged -= OnXPChanged;
                m_levelSystem.OnLevelUp -= OnLevelUp;
            }

            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Update()
        {
            // Smooth fill animation
            if (!m_isLevelingUp && Mathf.Abs(m_currentFill - m_targetFill) > 0.001f)
            {
                m_currentFill = Mathf.Lerp(m_currentFill, m_targetFill, Time.unscaledDeltaTime * m_fillSpeed);
                UpdateFillBar(m_currentFill);
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("XPBarCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = UITheme.SortOrder.XPBar;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create container (top-center, below level text)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            m_container = containerGO.AddComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 1f);
            m_container.anchorMax = new Vector2(0.5f, 1f);
            m_container.pivot = new Vector2(0.5f, 1f);
            m_container.anchoredPosition = new Vector2(0, -60);
            m_container.sizeDelta = new Vector2(m_barWidth + 80, m_barHeight + 30);

            // Level text (left of bar)
            var levelGO = new GameObject("LevelText");
            levelGO.transform.SetParent(m_container);
            var levelRect = levelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0.5f);
            levelRect.anchorMax = new Vector2(0, 0.5f);
            levelRect.pivot = new Vector2(1, 0.5f);
            levelRect.anchoredPosition = new Vector2(-5, 0);
            levelRect.sizeDelta = new Vector2(50, 30);

            m_levelText = levelGO.AddComponent<TextMeshProUGUI>();
            m_levelText.text = "LV 1";
            m_levelText.fontSize = 16;
            m_levelText.fontStyle = FontStyles.Bold;
            m_levelText.color = LevelTextColor;
            m_levelText.alignment = TextAlignmentOptions.MidlineRight;

            // Bar container
            var barContainerGO = new GameObject("BarContainer");
            barContainerGO.transform.SetParent(m_container);
            var barContainerRect = barContainerGO.AddComponent<RectTransform>();
            barContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            barContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            barContainerRect.pivot = new Vector2(0.5f, 0.5f);
            barContainerRect.anchoredPosition = new Vector2(15, 0);
            barContainerRect.sizeDelta = new Vector2(m_barWidth, m_barHeight);

            // Background bar
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(barContainerGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            m_backgroundBar = bgGO.AddComponent<Image>();
            m_backgroundBar.color = BarBackgroundColor;
            m_backgroundBar.sprite = CreateRoundedRectSprite();
            m_backgroundBar.type = Image.Type.Sliced;

            // Fill bar
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barContainerGO.transform);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);

            m_fillBar = fillGO.AddComponent<Image>();
            m_fillBar.color = BarColor;
            m_fillBar.sprite = CreateRoundedRectSprite();
            m_fillBar.type = Image.Type.Sliced;

            // XP text (right of bar)
            var xpGO = new GameObject("XPText");
            xpGO.transform.SetParent(m_container);
            var xpRect = xpGO.AddComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(1, 0.5f);
            xpRect.anchorMax = new Vector2(1, 0.5f);
            xpRect.pivot = new Vector2(0, 0.5f);
            xpRect.anchoredPosition = new Vector2(5, 0);
            xpRect.sizeDelta = new Vector2(80, 20);

            m_xpText = xpGO.AddComponent<TextMeshProUGUI>();
            m_xpText.text = "0/10";
            m_xpText.fontSize = 11;
            m_xpText.color = new Color(0.7f, 0.7f, 0.7f);
            m_xpText.alignment = TextAlignmentOptions.MidlineLeft;

            // Initialize bar at zero
            UpdateFillBar(0);
        }

        private Sprite CreateRoundedRectSprite()
        {
            int size = 16;
            int border = 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            int cornerRadius = 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;

                    // Check corners
                    if (x < cornerRadius && y < cornerRadius)
                    {
                        int dx = cornerRadius - x;
                        int dy = cornerRadius - y;
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x >= size - cornerRadius && y < cornerRadius)
                    {
                        int dx = x - (size - cornerRadius - 1);
                        int dy = cornerRadius - y;
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x < cornerRadius && y >= size - cornerRadius)
                    {
                        int dx = cornerRadius - x;
                        int dy = y - (size - cornerRadius - 1);
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x >= size - cornerRadius && y >= size - cornerRadius)
                    {
                        int dx = x - (size - cornerRadius - 1);
                        int dy = y - (size - cornerRadius - 1);
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private void OnXPChanged(int currentXP, int xpForLevel, int level)
        {
            UpdateDisplay(currentXP, xpForLevel, level);
        }

        private void OnLevelUp(int newLevel)
        {
            if (m_levelUpCoroutine != null)
            {
                StopCoroutine(m_levelUpCoroutine);
            }
            m_levelUpCoroutine = StartCoroutine(LevelUpAnimation(newLevel));
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Reset display
            m_currentFill = 0;
            m_targetFill = 0;
            UpdateFillBar(0);
            m_levelText.text = "LV 1";
            m_xpText.text = "0/10";
            m_levelText.color = LevelTextColor;
            m_fillBar.color = BarColor;
        }

        private void UpdateDisplay(int currentXP, int xpForLevel, int level)
        {
            m_targetFill = xpForLevel > 0 ? (float)currentXP / xpForLevel : 0;
            m_levelText.text = $"LV {level}";
            m_xpText.text = $"{currentXP}/{xpForLevel}";
        }

        private void UpdateFillBar(float fill)
        {
            if (m_fillBar == null) return;

            RectTransform rect = m_fillBar.rectTransform;
            float maxWidth = m_barWidth - 4; // Account for padding
            rect.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(fill), rect.sizeDelta.y);
        }

        private IEnumerator LevelUpAnimation(int newLevel)
        {
            m_isLevelingUp = true;

            // Flash the bar and level text
            for (int i = 0; i < m_levelUpFlashCount; i++)
            {
                // Flash on
                m_fillBar.color = LevelUpFlashColor;
                m_levelText.color = LevelUpFlashColor;
                m_backgroundBar.color = new Color(0.3f, 0.3f, 0.2f);

                yield return new WaitForSecondsRealtime(m_levelUpFlashDuration / (m_levelUpFlashCount * 2));

                // Flash off
                m_fillBar.color = BarColor;
                m_levelText.color = LevelTextColor;
                m_backgroundBar.color = BarBackgroundColor;

                yield return new WaitForSecondsRealtime(m_levelUpFlashDuration / (m_levelUpFlashCount * 2));
            }

            // Reset bar to start of new level
            m_currentFill = 0;
            UpdateFillBar(0);

            // Update target from current system state (use cached reference)
            if (m_levelSystem != null)
            {
                m_targetFill = m_levelSystem.LevelProgress;
                m_xpText.text = $"{m_levelSystem.CurrentXP}/{m_levelSystem.XPForCurrentLevel}";
            }

            m_isLevelingUp = false;
            m_levelUpCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Fill 50%")]
        private void DebugFill50()
        {
            m_targetFill = 0.5f;
            m_xpText.text = "5/10";
        }

        [ContextMenu("Debug: Fill 100%")]
        private void DebugFill100()
        {
            m_targetFill = 1f;
            m_xpText.text = "10/10";
        }

        [ContextMenu("Debug: Level Up")]
        private void DebugLevelUp()
        {
            if (m_levelUpCoroutine != null)
            {
                StopCoroutine(m_levelUpCoroutine);
            }
            m_levelUpCoroutine = StartCoroutine(LevelUpAnimation(2));
        }

        #endregion
    }
}
