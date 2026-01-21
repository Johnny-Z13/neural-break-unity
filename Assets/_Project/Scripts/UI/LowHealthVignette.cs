using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays a pulsing red vignette effect when player health is low.
    /// Creates dramatic tension and alerts player to danger.
    /// </summary>
    public class LowHealthVignette : MonoBehaviour
    {
        [Header("Vignette Settings")]
        [SerializeField] private float _lowHealthThreshold = 0.3f;  // Show below 30% health
        [SerializeField] private float _criticalHealthThreshold = 0.15f; // More intense below 15%
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _criticalPulseSpeed = 4f;
        [SerializeField] private float _minAlpha = 0.1f;
        [SerializeField] private float _maxAlpha = 0.4f;
        [SerializeField] private float _criticalMaxAlpha = 0.6f;
        [SerializeField] private float _fadeSpeed = 3f;

        [Header("Colors")]
        [SerializeField] private Color _lowHealthColor = new Color(0.8f, 0f, 0f, 1f);
        [SerializeField] private Color _criticalHealthColor = new Color(1f, 0f, 0f, 1f);

        [Header("References")]
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        // State
        private float _currentHealthPercent = 1f;
        private float _targetAlpha;
        private float _currentAlpha;
        private bool _isLowHealth;
        private bool _isCriticalHealth;
        private float _pulseTime;
        private Texture2D _vignetteTexture;

        private void Start()
        {
            CreateVignette();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void CreateVignette()
        {
            // Create canvas if needed
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("VignetteCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Above other UI
                canvasGO.AddComponent<CanvasScaler>();
                transform.SetParent(canvasGO.transform);
            }

            // Create vignette image
            if (_vignetteImage == null)
            {
                var imageGO = new GameObject("VignetteImage");
                imageGO.transform.SetParent(transform);

                var rectTransform = imageGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                _vignetteImage = imageGO.AddComponent<RawImage>();
                _vignetteImage.raycastTarget = false;
            }

            // Create canvas group for easy alpha control
            if (_canvasGroup == null)
            {
                _canvasGroup = _vignetteImage.gameObject.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _vignetteImage.gameObject.AddComponent<CanvasGroup>();
                }
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            // Generate vignette texture
            _vignetteTexture = CreateVignetteTexture(256);
            _vignetteImage.texture = _vignetteTexture;
            _vignetteImage.color = _lowHealthColor;

            // Start hidden
            _canvasGroup.alpha = 0f;
            _currentAlpha = 0f;
        }

        private Texture2D CreateVignetteTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = size / 2f;
            float maxDist = size / 2f;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;

                    // Elliptical distance for widescreen feel
                    float dist = Mathf.Sqrt(dx * dx * 0.7f + dy * dy);

                    // Vignette falloff - transparent in center, opaque at edges
                    float vignette = Mathf.Clamp01((dist - 0.3f) / 0.7f);
                    vignette = Mathf.Pow(vignette, 1.5f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, vignette);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private void Update()
        {
            if (!_isLowHealth)
            {
                // Fade out when not low health
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.unscaledDeltaTime * _fadeSpeed);
            }
            else
            {
                // Pulse when low health
                float pulseSpeed = _isCriticalHealth ? _criticalPulseSpeed : _pulseSpeed;
                _pulseTime += Time.unscaledDeltaTime * pulseSpeed;

                float minA = _minAlpha;
                float maxA = _isCriticalHealth ? _criticalMaxAlpha : _maxAlpha;

                // Heartbeat-style pulse (quick up, slow down)
                float pulse = Mathf.Sin(_pulseTime * Mathf.PI);
                pulse = Mathf.Abs(pulse);
                pulse = Mathf.Pow(pulse, 0.5f);

                _targetAlpha = Mathf.Lerp(minA, maxA, pulse);

                // Smoothly approach target
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha,
                    Time.unscaledDeltaTime * _fadeSpeed * 2f);

                // Update color based on health level
                Color targetColor = _isCriticalHealth ? _criticalHealthColor : _lowHealthColor;
                _vignetteImage.color = Color.Lerp(_vignetteImage.color, targetColor,
                    Time.unscaledDeltaTime * 5f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _currentAlpha;
            }
        }

        private void UpdateHealthState(int current, int max)
        {
            if (max <= 0) return;

            _currentHealthPercent = (float)current / max;
            _isLowHealth = _currentHealthPercent <= _lowHealthThreshold;
            _isCriticalHealth = _currentHealthPercent <= _criticalHealthThreshold;

            // Reset pulse time when transitioning to critical for dramatic effect
            if (_isCriticalHealth && _currentHealthPercent > 0)
            {
                _pulseTime = 0f;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            UpdateHealthState(evt.currentHealth, evt.maxHealth);

            // Flash on damage
            if (_isLowHealth)
            {
                _currentAlpha = Mathf.Max(_currentAlpha, _maxAlpha);
            }
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            UpdateHealthState(evt.currentHealth, evt.maxHealth);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            _currentHealthPercent = 1f;
            _isLowHealth = false;
            _isCriticalHealth = false;
            _currentAlpha = 0f;
            _pulseTime = 0f;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Full red on death
            _currentAlpha = _criticalMaxAlpha;
            _isLowHealth = false; // Stop pulsing
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled && _canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }
    }
}
