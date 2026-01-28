using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays dramatic level-up announcement when player levels up.
    /// Features scale animation, particle-like effects, and glow.
    /// </summary>
    public class LevelUpAnnouncement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _displayDuration = 1.5f;
        [SerializeField] private float _scaleInDuration = 0.2f;
        [SerializeField] private float _scaleOutDuration = 0.3f;
        [SerializeField] private float _maxScale = 1.3f;
        [SerializeField] private float _pulseFrequency = 8f;
        [SerializeField] private float _pulseAmplitude = 0.05f;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool _useThemeColors = true;

        private Color LevelUpColor => _useThemeColors ? UITheme.Good : _customLevelUpColor;
        private Color GlowColor => _useThemeColors ? UITheme.GoodGlow : _customGlowColor;

        [SerializeField] private Color _customLevelUpColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color _customGlowColor = new Color(1f, 0.8f, 0.2f, 0.5f);

        // UI Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _levelUpText;
        private TextMeshProUGUI _levelNumberText;
        private Image _glowImage;
        private RectTransform _container;

        // Animation
        private Coroutine _announcementCoroutine;

        private void Start()
        {
            CreateUI();

            if (FindFirstObjectByType<PlayerLevelSystem>() != null)
            {
                FindFirstObjectByType<PlayerLevelSystem>().OnLevelUp += OnLevelUp;
            }
        }

        private void OnDestroy()
        {
            if (FindFirstObjectByType<PlayerLevelSystem>() != null)
            {
                FindFirstObjectByType<PlayerLevelSystem>().OnLevelUp -= OnLevelUp;
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("LevelUpCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = UITheme.SortOrder.LevelUp;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // Container (positioned at bottom of top third - doesn't occlude player)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 0.5f);
            _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.pivot = new Vector2(0.5f, 0.5f);
            _container.anchoredPosition = new Vector2(0, 250); // Top third (was 50)
            _container.sizeDelta = new Vector2(400, 150);

            // Glow background
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(_container);
            var glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-100, -50);
            glowRect.offsetMax = new Vector2(100, 50);

            _glowImage = glowGO.AddComponent<Image>();
            _glowImage.color = GlowColor;
            _glowImage.sprite = CreateGlowSprite();

            // "LEVEL UP!" text
            var levelUpGO = new GameObject("LevelUpText");
            levelUpGO.transform.SetParent(_container);
            var levelUpRect = levelUpGO.AddComponent<RectTransform>();
            levelUpRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelUpRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelUpRect.pivot = new Vector2(0.5f, 0.5f);
            levelUpRect.anchoredPosition = new Vector2(0, 20);
            levelUpRect.sizeDelta = new Vector2(400, 60);

            _levelUpText = levelUpGO.AddComponent<TextMeshProUGUI>();
            _levelUpText.text = "POWER UP!";
            _levelUpText.fontSize = 48;
            _levelUpText.fontStyle = FontStyles.Bold;
            _levelUpText.color = LevelUpColor;
            _levelUpText.alignment = TextAlignmentOptions.Center;
            _levelUpText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            // Add outline
            var outline = levelUpGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            // Power level number text
            var levelNumGO = new GameObject("LevelNumberText");
            levelNumGO.transform.SetParent(_container);
            var levelNumRect = levelNumGO.AddComponent<RectTransform>();
            levelNumRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelNumRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelNumRect.pivot = new Vector2(0.5f, 0.5f);
            levelNumRect.anchoredPosition = new Vector2(0, -30);
            levelNumRect.sizeDelta = new Vector2(250, 50);

            _levelNumberText = levelNumGO.AddComponent<TextMeshProUGUI>();
            _levelNumberText.text = "POWER = 2";
            _levelNumberText.fontSize = 32;
            _levelNumberText.fontStyle = FontStyles.Bold;
            _levelNumberText.color = Color.white;
            _levelNumberText.alignment = TextAlignmentOptions.Center;
        }

        private Sprite CreateGlowSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                    alpha = alpha * alpha; // Quadratic falloff
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void OnLevelUp(int newLevel)
        {
            ShowAnnouncement(newLevel);
        }

        public void ShowAnnouncement(int level)
        {
            if (_announcementCoroutine != null)
            {
                StopCoroutine(_announcementCoroutine);
            }
            _announcementCoroutine = StartCoroutine(AnnouncementCoroutine(level));
        }

        private IEnumerator AnnouncementCoroutine(int level)
        {
            _levelNumberText.text = $"POWER = {level}";
            _container.localScale = Vector3.zero;
            _canvasGroup.alpha = 0;

            // Scale in
            float elapsed = 0f;
            while (elapsed < _scaleInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _scaleInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                _container.localScale = Vector3.one * _maxScale * easeT;
                _canvasGroup.alpha = easeT;

                yield return null;
            }

            _container.localScale = Vector3.one * _maxScale;
            _canvasGroup.alpha = 1f;

            // Hold with pulse
            elapsed = 0f;
            float holdDuration = _displayDuration - _scaleInDuration - _scaleOutDuration;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Pulse scale
                float pulse = 1f + Mathf.Sin(elapsed * _pulseFrequency) * _pulseAmplitude;
                _container.localScale = Vector3.one * _maxScale * pulse;

                // Glow pulse
                Color glowC = GlowColor;
                glowC.a = GlowColor.a * (0.7f + Mathf.Sin(elapsed * _pulseFrequency * 0.5f) * 0.3f);
                _glowImage.color = glowC;

                yield return null;
            }

            // Scale down and fade out
            elapsed = 0f;
            Vector3 startScale = _container.localScale;
            while (elapsed < _scaleOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _scaleOutDuration;
                float easeT = t * t * t; // Ease in cubic

                _container.localScale = Vector3.Lerp(startScale, Vector3.one * 0.5f, easeT);
                _canvasGroup.alpha = 1f - easeT;

                yield return null;
            }

            _canvasGroup.alpha = 0;
            _announcementCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Show Level 5")]
        private void DebugShowLevel5() => ShowAnnouncement(5);

        [ContextMenu("Debug: Show Level 10")]
        private void DebugShowLevel10() => ShowAnnouncement(10);

        [ContextMenu("Debug: Show Level 25")]
        private void DebugShowLevel25() => ShowAnnouncement(25);

        #endregion
    }
}
