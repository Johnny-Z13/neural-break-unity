using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays controls tutorial overlay.
    /// Shows automatically on first play, can be dismissed with any key.
    /// Also accessible from pause menu.
    /// </summary>
    public class ControlsOverlay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private bool _showOnFirstPlay = true;
        [SerializeField] private float _autoHideDelay = 0f; // 0 = don't auto-hide

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color _headerColor = new Color(0f, 1f, 1f);
        [SerializeField] private Color _keyColor = new Color(1f, 1f, 0f);
        [SerializeField] private Color _descriptionColor = Color.white;
        [SerializeField] private Color _hintColor = new Color(0.7f, 0.7f, 0.7f);

        private const string PREF_FIRST_PLAY = "NeuralBreak_FirstPlay";

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _container;
        private bool _isVisible;
        private bool _isFading;

        private void Awake()
        {
            CreateUI();
            _canvasGroup.alpha = 0f;
            _container.gameObject.SetActive(false);
        }

        private void Start()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            if (_showOnFirstPlay && IsFirstPlay())
            {
                Show();
                MarkFirstPlayComplete();
            }
        }

        private bool IsFirstPlay()
        {
            return PlayerPrefs.GetInt(PREF_FIRST_PLAY, 1) == 1;
        }

        private void MarkFirstPlayComplete()
        {
            PlayerPrefs.SetInt(PREF_FIRST_PLAY, 0);
            PlayerPrefs.Save();
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("ControlsCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200; // Above everything

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = true;

            // Create background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = _backgroundColor;

            // Create container
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 0.5f);
            _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.sizeDelta = new Vector2(600, 500);
            _container.anchoredPosition = Vector2.zero;

            // Add vertical layout
            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(20, 20, 20, 20);

            // Add content size fitter
            var sizeFitter = containerGO.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            CreateText(_container, "CONTROLS", 42, _headerColor, FontStyles.Bold, 60);

            // Spacer
            CreateSpacer(_container, 20);

            // Movement controls
            CreateControlRow(_container, "WASD / Arrow Keys", "Move");
            CreateControlRow(_container, "Shift / Right Click", "Dash (invincible)");

            // Spacer
            CreateSpacer(_container, 10);

            // Combat controls
            CreateControlRow(_container, "Mouse / Left Stick", "Aim");
            CreateControlRow(_container, "Left Click / RT", "Fire");

            // Spacer
            CreateSpacer(_container, 10);

            // System controls
            CreateControlRow(_container, "Escape / Start", "Pause");

            // Spacer
            CreateSpacer(_container, 30);

            // Tips header
            CreateText(_container, "TIPS", 28, _headerColor, FontStyles.Bold, 40);

            // Tips
            CreateText(_container, "Dashing through enemies damages them", 20, _descriptionColor, FontStyles.Normal, 30);
            CreateText(_container, "Build combos for higher score multipliers", 20, _descriptionColor, FontStyles.Normal, 30);
            CreateText(_container, "Watch for spawn warning indicators", 20, _descriptionColor, FontStyles.Normal, 30);

            // Spacer
            CreateSpacer(_container, 40);

            // Dismiss hint
            CreateText(_container, "Press any key to continue...", 18, _hintColor, FontStyles.Italic, 30);
        }

        private void CreateText(RectTransform parent, string text, int fontSize, Color color, FontStyles style, float height)
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(parent);

            var rect = textGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            var layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void CreateControlRow(RectTransform parent, string key, string description)
        {
            var rowGO = new GameObject("ControlRow");
            rowGO.transform.SetParent(parent);

            var rect = rowGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 35);

            var layoutElement = rowGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 35;
            layoutElement.flexibleWidth = 1;

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            // Key text
            var keyGO = new GameObject("Key");
            keyGO.transform.SetParent(rowGO.transform);
            var keyRect = keyGO.AddComponent<RectTransform>();

            var keyLayout = keyGO.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 250;

            var keyTmp = keyGO.AddComponent<TextMeshProUGUI>();
            keyTmp.text = key;
            keyTmp.fontSize = 22;
            keyTmp.color = _keyColor;
            keyTmp.fontStyle = FontStyles.Bold;
            keyTmp.alignment = TextAlignmentOptions.Right;

            // Description text
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(rowGO.transform);
            var descRect = descGO.AddComponent<RectTransform>();

            var descLayout = descGO.AddComponent<LayoutElement>();
            descLayout.preferredWidth = 250;

            var descTmp = descGO.AddComponent<TextMeshProUGUI>();
            descTmp.text = description;
            descTmp.fontSize = 22;
            descTmp.color = _descriptionColor;
            descTmp.alignment = TextAlignmentOptions.Left;
        }

        private void CreateSpacer(RectTransform parent, float height)
        {
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(parent);

            var rect = spacerGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            var layoutElement = spacerGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
        }

        private void Update()
        {
            if (!_isVisible || _isFading) return;

            // Dismiss on any key or mouse click
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            bool anyKeyPressed = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            bool mouseClicked = mouse != null && mouse.leftButton.wasPressedThisFrame;

            if (anyKeyPressed || mouseClicked)
            {
                Hide();
            }
        }

        public void Show()
        {
            if (_isVisible) return;

            _isVisible = true;
            _container.gameObject.SetActive(true);
            StartCoroutine(FadeIn());

            // Pause game while showing controls
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                Time.timeScale = 0f;
            }
        }

        public void Hide()
        {
            if (!_isVisible) return;

            StartCoroutine(FadeOut());
        }

        private System.Collections.IEnumerator FadeIn()
        {
            _isFading = true;
            float elapsed = 0f;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = elapsed / _fadeInDuration;
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _isFading = false;

            // Auto-hide after delay if configured
            if (_autoHideDelay > 0)
            {
                yield return new WaitForSecondsRealtime(_autoHideDelay);
                Hide();
            }
        }

        private System.Collections.IEnumerator FadeOut()
        {
            _isFading = true;
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _isVisible = false;
            _isFading = false;
            _container.gameObject.SetActive(false);

            // Resume game
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Reset first play flag (for testing)
        /// </summary>
        [ContextMenu("Reset First Play")]
        public void ResetFirstPlay()
        {
            PlayerPrefs.SetInt(PREF_FIRST_PLAY, 1);
            PlayerPrefs.Save();
            Debug.Log("[ControlsOverlay] First play flag reset");
        }
    }
}
