using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays boss health bar and "WARNING" announcement when a boss spawns.
    /// Shows at top of screen with dramatic entrance animation.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform _container;
        [SerializeField] private Image _healthBarFill;
        [SerializeField] private Image _healthBarBackground;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _warningText;

        [Header("Settings")]
        [SerializeField] private float _slideInDuration = 0.5f;
        [SerializeField] private float _slideOutDuration = 0.3f;
        [SerializeField] private float _warningDisplayTime = 2f;
        [SerializeField] private float _warningPulseSpeed = 8f;
        [SerializeField] private float _healthLerpSpeed = 5f;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool _useThemeColors = true;

        private Color HealthColor => _useThemeColors ? UITheme.Danger : _customHealthColor;
        private Color HealthBackgroundColor => _useThemeColors ? UITheme.DangerDark.WithAlpha(0.8f) : _customHealthBackgroundColor;
        private Color WarningColor => _useThemeColors ? UITheme.Danger : _customWarningColor;
        private Color Phase2Color => _useThemeColors ? UITheme.Warning : _customPhase2Color;
        private Color Phase3Color => _useThemeColors ? UITheme.Accent : _customPhase3Color;

        [SerializeField] private Color _customHealthColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color _customHealthBackgroundColor = new Color(0.3f, 0f, 0f, 0.8f);
        [SerializeField] private Color _customWarningColor = new Color(1f, 0f, 0f);
        [SerializeField] private Color _customPhase2Color = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color _customPhase3Color = new Color(1f, 0f, 0.5f);

        private float _targetHealthPercent = 1f;
        private float _displayedHealthPercent = 1f;
        private bool _isVisible;
        private Coroutine _animationCoroutine;
        private Vector2 _hiddenPosition;
        private Vector2 _visiblePosition;

        private void Start()
        {
            CreateUI();
            SubscribeToEvents();
            HideImmediate();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CreateUI()
        {
            // Create canvas if needed
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("BossHealthCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = UITheme.SortOrder.BossHealth;
                canvasGO.AddComponent<CanvasScaler>();
                transform.SetParent(canvasGO.transform);
            }

            // Create container
            if (_container == null)
            {
                var containerGO = new GameObject("BossHealthContainer");
                containerGO.transform.SetParent(transform);
                _container = containerGO.AddComponent<RectTransform>();
                _container.anchorMin = new Vector2(0.5f, 1f);
                _container.anchorMax = new Vector2(0.5f, 1f);
                _container.pivot = new Vector2(0.5f, 1f);
                _container.sizeDelta = new Vector2(600, 80);
                _container.anchoredPosition = Vector2.zero;
            }

            _visiblePosition = new Vector2(0, -20);
            _hiddenPosition = new Vector2(0, 100);

            // Create warning text
            if (_warningText == null)
            {
                var warningGO = new GameObject("WarningText");
                warningGO.transform.SetParent(_container);
                var warningRect = warningGO.AddComponent<RectTransform>();
                warningRect.anchorMin = new Vector2(0.5f, 0.5f);
                warningRect.anchorMax = new Vector2(0.5f, 0.5f);
                warningRect.sizeDelta = new Vector2(400, 50);
                warningRect.anchoredPosition = new Vector2(0, 20);

                _warningText = warningGO.AddComponent<TextMeshProUGUI>();
                _warningText.text = "WARNING";
                _warningText.fontSize = 36;
                _warningText.color = WarningColor;
                _warningText.alignment = TextAlignmentOptions.Center;
                _warningText.fontStyle = FontStyles.Bold;
            }

            // Create boss name text
            if (_bossNameText == null)
            {
                var nameGO = new GameObject("BossNameText");
                nameGO.transform.SetParent(_container);
                var nameRect = nameGO.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.5f, 0.5f);
                nameRect.anchorMax = new Vector2(0.5f, 0.5f);
                nameRect.sizeDelta = new Vector2(300, 30);
                nameRect.anchoredPosition = new Vector2(0, -5);

                _bossNameText = nameGO.AddComponent<TextMeshProUGUI>();
                _bossNameText.text = "SECTOR GUARDIAN";
                _bossNameText.fontSize = 18;
                _bossNameText.color = Color.white;
                _bossNameText.alignment = TextAlignmentOptions.Center;
            }

            // Create health bar background
            if (_healthBarBackground == null)
            {
                var bgGO = new GameObject("HealthBarBG");
                bgGO.transform.SetParent(_container);
                var bgRect = bgGO.AddComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0.5f, 0f);
                bgRect.anchorMax = new Vector2(0.5f, 0f);
                bgRect.pivot = new Vector2(0.5f, 0f);
                bgRect.sizeDelta = new Vector2(500, 20);
                bgRect.anchoredPosition = new Vector2(0, 10);

                _healthBarBackground = bgGO.AddComponent<Image>();
                _healthBarBackground.color = HealthBackgroundColor;
            }

            // Create health bar fill
            if (_healthBarFill == null)
            {
                var fillGO = new GameObject("HealthBarFill");
                fillGO.transform.SetParent(_healthBarBackground.transform);
                var fillRect = fillGO.AddComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(1f, 1f);
                fillRect.offsetMin = new Vector2(2, 2);
                fillRect.offsetMax = new Vector2(-2, -2);
                fillRect.pivot = new Vector2(0f, 0.5f);

                _healthBarFill = fillGO.AddComponent<Image>();
                _healthBarFill.color = HealthColor;
                _healthBarFill.type = Image.Type.Filled;
                _healthBarFill.fillMethod = Image.FillMethod.Horizontal;
                _healthBarFill.fillOrigin = 0;
                _healthBarFill.fillAmount = 1f;
            }
        }

        private void Update()
        {
            if (!_isVisible) return;

            // Smoothly lerp health bar
            _displayedHealthPercent = Mathf.Lerp(_displayedHealthPercent, _targetHealthPercent,
                Time.unscaledDeltaTime * _healthLerpSpeed);
            _healthBarFill.fillAmount = _displayedHealthPercent;

            // Update health bar color based on phase
            Color targetColor;
            if (_targetHealthPercent > 0.67f)
            {
                targetColor = HealthColor;
            }
            else if (_targetHealthPercent > 0.34f)
            {
                targetColor = Phase2Color;
            }
            else
            {
                targetColor = Phase3Color;
            }
            _healthBarFill.color = Color.Lerp(_healthBarFill.color, targetColor, Time.unscaledDeltaTime * 3f);
        }

        private void OnBossEncounter(BossEncounterEvent evt)
        {
            if (evt.isBossActive)
            {
                _targetHealthPercent = evt.healthPercent;
                if (!_isVisible)
                {
                    Show();
                }
            }
            else
            {
                Hide();
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            HideImmediate();
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            HideImmediate();
        }

        public void Show()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(ShowAnimation());
        }

        public void Hide()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(HideAnimation());
        }

        public void HideImmediate()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _isVisible = false;
            if (_container != null)
            {
                _container.anchoredPosition = _hiddenPosition;
            }
            if (_warningText != null)
            {
                _warningText.gameObject.SetActive(false);
            }
        }

        private IEnumerator ShowAnimation()
        {
            _isVisible = true;
            _displayedHealthPercent = 1f;
            _targetHealthPercent = 1f;
            _healthBarFill.fillAmount = 1f;

            // Show warning first
            _warningText.gameObject.SetActive(true);
            _bossNameText.gameObject.SetActive(false);
            _healthBarBackground.gameObject.SetActive(false);

            // Slide in
            float elapsed = 0f;
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _slideInDuration);
                _container.anchoredPosition = Vector2.Lerp(_hiddenPosition, _visiblePosition, t);

                // Pulse warning
                float pulse = Mathf.Sin(elapsed * _warningPulseSpeed) * 0.5f + 0.5f;
                Color c = WarningColor;
                c.a = 0.5f + pulse * 0.5f;
                _warningText.color = c;
                _warningText.transform.localScale = Vector3.one * (1f + pulse * 0.2f);

                yield return null;
            }

            // Hold warning
            float warningElapsed = 0f;
            while (warningElapsed < _warningDisplayTime)
            {
                warningElapsed += Time.unscaledDeltaTime;
                float pulse = Mathf.Sin(warningElapsed * _warningPulseSpeed) * 0.5f + 0.5f;
                Color c = WarningColor;
                c.a = 0.5f + pulse * 0.5f;
                _warningText.color = c;
                _warningText.transform.localScale = Vector3.one * (1f + pulse * 0.2f);
                yield return null;
            }

            // Transition to health bar
            _warningText.gameObject.SetActive(false);
            _bossNameText.gameObject.SetActive(true);
            _healthBarBackground.gameObject.SetActive(true);

            _animationCoroutine = null;
        }

        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            Vector2 startPos = _container.anchoredPosition;

            while (elapsed < _slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideOutDuration;
                _container.anchoredPosition = Vector2.Lerp(startPos, _hiddenPosition, t);
                yield return null;
            }

            _isVisible = false;
            _animationCoroutine = null;
        }
    }
}
