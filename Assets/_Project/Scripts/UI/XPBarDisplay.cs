using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays XP progress bar with level indicator.
    /// Features smooth fill animation and level-up effects.
    /// </summary>
    public class XPBarDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fillSpeed = 3f;
        [SerializeField] private float _levelUpFlashDuration = 0.5f;
        [SerializeField] private int _levelUpFlashCount = 3;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool _useThemeColors = true;

        private Color BarColor => _useThemeColors ? UITheme.Primary : _customBarColor;
        private Color BarBackgroundColor => _useThemeColors ? UITheme.BarBackground : _customBarBackgroundColor;
        private Color LevelUpFlashColor => _useThemeColors ? UITheme.Good : _customLevelUpColor;
        private Color LevelTextColor => _useThemeColors ? UITheme.TextPrimary : Color.white;

        [SerializeField] private Color _customBarColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color _customBarBackgroundColor = new Color(0.1f, 0.1f, 0.15f);
        [SerializeField] private Color _customLevelUpColor = new Color(1f, 1f, 0.4f);

        [Header("Layout")]
        [SerializeField] private float _barWidth = 200f;
        [SerializeField] private float _barHeight = 12f;
#pragma warning disable CS0414 // Reserved for rounded corners feature
        [SerializeField] private float _cornerRadius = 4f;
#pragma warning restore CS0414

        // UI Components
        private Canvas _canvas;
        private RectTransform _container;
        private Image _backgroundBar;
        private Image _fillBar;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _xpText;

        // Animation state
        private float _currentFill;
        private float _targetFill;
        private bool _isLevelingUp;
        private Coroutine _levelUpCoroutine;

        private void Start()
        {
            CreateUI();

            if (FindObjectOfType<PlayerLevelSystem>() != null)
            {
                FindObjectOfType<PlayerLevelSystem>().OnXPChanged += OnXPChanged;
                FindObjectOfType<PlayerLevelSystem>().OnLevelUp += OnLevelUp;

                // Initialize with current values
                UpdateDisplay(
                    FindObjectOfType<PlayerLevelSystem>().CurrentXP,
                    FindObjectOfType<PlayerLevelSystem>().XPForCurrentLevel,
                    FindObjectOfType<PlayerLevelSystem>().CurrentLevel
                );
            }

            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            if (FindObjectOfType<PlayerLevelSystem>() != null)
            {
                FindObjectOfType<PlayerLevelSystem>().OnXPChanged -= OnXPChanged;
                FindObjectOfType<PlayerLevelSystem>().OnLevelUp -= OnLevelUp;
            }

            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Update()
        {
            // Smooth fill animation
            if (!_isLevelingUp && Mathf.Abs(_currentFill - _targetFill) > 0.001f)
            {
                _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.unscaledDeltaTime * _fillSpeed);
                UpdateFillBar(_currentFill);
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("XPBarCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = UITheme.SortOrder.XPBar;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create container (top-center, below level text)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 1f);
            _container.anchorMax = new Vector2(0.5f, 1f);
            _container.pivot = new Vector2(0.5f, 1f);
            _container.anchoredPosition = new Vector2(0, -60);
            _container.sizeDelta = new Vector2(_barWidth + 80, _barHeight + 30);

            // Level text (left of bar)
            var levelGO = new GameObject("LevelText");
            levelGO.transform.SetParent(_container);
            var levelRect = levelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0.5f);
            levelRect.anchorMax = new Vector2(0, 0.5f);
            levelRect.pivot = new Vector2(1, 0.5f);
            levelRect.anchoredPosition = new Vector2(-5, 0);
            levelRect.sizeDelta = new Vector2(50, 30);

            _levelText = levelGO.AddComponent<TextMeshProUGUI>();
            _levelText.text = "LV 1";
            _levelText.fontSize = 16;
            _levelText.fontStyle = FontStyles.Bold;
            _levelText.color = LevelTextColor;
            _levelText.alignment = TextAlignmentOptions.MidlineRight;

            // Bar container
            var barContainerGO = new GameObject("BarContainer");
            barContainerGO.transform.SetParent(_container);
            var barContainerRect = barContainerGO.AddComponent<RectTransform>();
            barContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            barContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            barContainerRect.pivot = new Vector2(0.5f, 0.5f);
            barContainerRect.anchoredPosition = new Vector2(15, 0);
            barContainerRect.sizeDelta = new Vector2(_barWidth, _barHeight);

            // Background bar
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(barContainerGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _backgroundBar = bgGO.AddComponent<Image>();
            _backgroundBar.color = BarBackgroundColor;
            _backgroundBar.sprite = CreateRoundedRectSprite();
            _backgroundBar.type = Image.Type.Sliced;

            // Fill bar
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barContainerGO.transform);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);

            _fillBar = fillGO.AddComponent<Image>();
            _fillBar.color = BarColor;
            _fillBar.sprite = CreateRoundedRectSprite();
            _fillBar.type = Image.Type.Sliced;

            // XP text (right of bar)
            var xpGO = new GameObject("XPText");
            xpGO.transform.SetParent(_container);
            var xpRect = xpGO.AddComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(1, 0.5f);
            xpRect.anchorMax = new Vector2(1, 0.5f);
            xpRect.pivot = new Vector2(0, 0.5f);
            xpRect.anchoredPosition = new Vector2(5, 0);
            xpRect.sizeDelta = new Vector2(80, 20);

            _xpText = xpGO.AddComponent<TextMeshProUGUI>();
            _xpText.text = "0/10";
            _xpText.fontSize = 11;
            _xpText.color = new Color(0.7f, 0.7f, 0.7f);
            _xpText.alignment = TextAlignmentOptions.MidlineLeft;

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
            if (_levelUpCoroutine != null)
            {
                StopCoroutine(_levelUpCoroutine);
            }
            _levelUpCoroutine = StartCoroutine(LevelUpAnimation(newLevel));
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Reset display
            _currentFill = 0;
            _targetFill = 0;
            UpdateFillBar(0);
            _levelText.text = "LV 1";
            _xpText.text = "0/10";
            _levelText.color = LevelTextColor;
            _fillBar.color = BarColor;
        }

        private void UpdateDisplay(int currentXP, int xpForLevel, int level)
        {
            _targetFill = xpForLevel > 0 ? (float)currentXP / xpForLevel : 0;
            _levelText.text = $"LV {level}";
            _xpText.text = $"{currentXP}/{xpForLevel}";
        }

        private void UpdateFillBar(float fill)
        {
            if (_fillBar == null) return;

            RectTransform rect = _fillBar.rectTransform;
            float maxWidth = _barWidth - 4; // Account for padding
            rect.sizeDelta = new Vector2(maxWidth * Mathf.Clamp01(fill), rect.sizeDelta.y);
        }

        private IEnumerator LevelUpAnimation(int newLevel)
        {
            _isLevelingUp = true;

            // Flash the bar and level text
            for (int i = 0; i < _levelUpFlashCount; i++)
            {
                // Flash on
                _fillBar.color = LevelUpFlashColor;
                _levelText.color = LevelUpFlashColor;
                _backgroundBar.color = new Color(0.3f, 0.3f, 0.2f);

                yield return new WaitForSecondsRealtime(_levelUpFlashDuration / (_levelUpFlashCount * 2));

                // Flash off
                _fillBar.color = BarColor;
                _levelText.color = LevelTextColor;
                _backgroundBar.color = BarBackgroundColor;

                yield return new WaitForSecondsRealtime(_levelUpFlashDuration / (_levelUpFlashCount * 2));
            }

            // Reset bar to start of new level
            _currentFill = 0;
            UpdateFillBar(0);

            // Update target from current system state
            if (FindObjectOfType<PlayerLevelSystem>() != null)
            {
                _targetFill = FindObjectOfType<PlayerLevelSystem>().LevelProgress;
                _xpText.text = $"{FindObjectOfType<PlayerLevelSystem>().CurrentXP}/{FindObjectOfType<PlayerLevelSystem>().XPForCurrentLevel}";
            }

            _isLevelingUp = false;
            _levelUpCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Fill 50%")]
        private void DebugFill50()
        {
            _targetFill = 0.5f;
            _xpText.text = "5/10";
        }

        [ContextMenu("Debug: Fill 100%")]
        private void DebugFill100()
        {
            _targetFill = 1f;
            _xpText.text = "10/10";
        }

        [ContextMenu("Debug: Level Up")]
        private void DebugLevelUp()
        {
            if (_levelUpCoroutine != null)
            {
                StopCoroutine(_levelUpCoroutine);
            }
            _levelUpCoroutine = StartCoroutine(LevelUpAnimation(2));
        }

        #endregion
    }
}
