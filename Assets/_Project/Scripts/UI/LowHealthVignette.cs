using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays a pulsing red vignette effect when player health is low.
    /// Creates dramatic tension and alerts player to danger.
    /// </summary>
    public class LowHealthVignette : MonoBehaviour
    {
        [Header("Vignette Settings")]
        [SerializeField] private float m_lowHealthThreshold = 0.3f;  // Show below 30% health
        [SerializeField] private float m_criticalHealthThreshold = 0.15f; // More intense below 15%
        [SerializeField] private float m_pulseSpeed = 2f;
        [SerializeField] private float m_criticalPulseSpeed = 4f;
        [SerializeField] private float m_minAlpha = 0.1f;
        [SerializeField] private float m_maxAlpha = 0.4f;
        [SerializeField] private float m_criticalMaxAlpha = 0.6f;
        [SerializeField] private float m_fadeSpeed = 3f;

        [Header("Colors")]
        [SerializeField] private Color m_lowHealthColor = new Color(0.8f, 0f, 0f, 1f);
        [SerializeField] private Color m_criticalHealthColor = new Color(1f, 0f, 0f, 1f);

        [Header("References")]
        [SerializeField] private RawImage m_vignetteImage;
        [SerializeField] private CanvasGroup m_canvasGroup;

        // State
        private float m_currentHealthPercent = 1f;
        private float m_targetAlpha;
        private float m_currentAlpha;
        private bool m_isLowHealth;
        private bool m_isCriticalHealth;
        private float m_pulseTime;
        private Texture2D m_vignetteTexture;

        private void Start()
        {
            CreateVignette();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (m_vignetteTexture != null)
            {
                Destroy(m_vignetteTexture);
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
            if (m_vignetteImage == null)
            {
                var imageGO = new GameObject("VignetteImage");
                imageGO.transform.SetParent(transform);

                var rectTransform = imageGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                m_vignetteImage = imageGO.AddComponent<RawImage>();
                m_vignetteImage.raycastTarget = false;
            }

            // Create canvas group for easy alpha control
            if (m_canvasGroup == null)
            {
                m_canvasGroup = m_vignetteImage.gameObject.GetComponent<CanvasGroup>();
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = m_vignetteImage.gameObject.AddComponent<CanvasGroup>();
                }
                m_canvasGroup.blocksRaycasts = false;
                m_canvasGroup.interactable = false;
            }

            // Generate vignette texture
            m_vignetteTexture = CreateVignetteTexture(256);
            m_vignetteImage.texture = m_vignetteTexture;
            m_vignetteImage.color = m_lowHealthColor;

            // Start hidden
            m_canvasGroup.alpha = 0f;
            m_currentAlpha = 0f;
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
            if (!m_isLowHealth)
            {
                // Fade out when not low health
                m_currentAlpha = Mathf.MoveTowards(m_currentAlpha, 0f, Time.unscaledDeltaTime * m_fadeSpeed);
            }
            else
            {
                // Pulse when low health
                float pulseSpeed = m_isCriticalHealth ? m_criticalPulseSpeed : m_pulseSpeed;
                m_pulseTime += Time.unscaledDeltaTime * pulseSpeed;

                float minA = m_minAlpha;
                float maxA = m_isCriticalHealth ? m_criticalMaxAlpha : m_maxAlpha;

                // Heartbeat-style pulse (quick up, slow down)
                float pulse = Mathf.Sin(m_pulseTime * Mathf.PI);
                pulse = Mathf.Abs(pulse);
                pulse = Mathf.Pow(pulse, 0.5f);

                m_targetAlpha = Mathf.Lerp(minA, maxA, pulse);

                // Smoothly approach target
                m_currentAlpha = Mathf.MoveTowards(m_currentAlpha, m_targetAlpha,
                    Time.unscaledDeltaTime * m_fadeSpeed * 2f);

                // Update color based on health level
                Color targetColor = m_isCriticalHealth ? m_criticalHealthColor : m_lowHealthColor;
                m_vignetteImage.color = Color.Lerp(m_vignetteImage.color, targetColor,
                    Time.unscaledDeltaTime * 5f);
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = m_currentAlpha;
            }
        }

        private void UpdateHealthState(int current, int max)
        {
            if (max <= 0) return;

            m_currentHealthPercent = (float)current / max;
            m_isLowHealth = m_currentHealthPercent <= m_lowHealthThreshold;
            m_isCriticalHealth = m_currentHealthPercent <= m_criticalHealthThreshold;

            // Reset pulse time when transitioning to critical for dramatic effect
            if (m_isCriticalHealth && m_currentHealthPercent > 0)
            {
                m_pulseTime = 0f;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            UpdateHealthState(evt.currentHealth, evt.maxHealth);

            // Flash on damage
            if (m_isLowHealth)
            {
                m_currentAlpha = Mathf.Max(m_currentAlpha, m_maxAlpha);
            }
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            UpdateHealthState(evt.currentHealth, evt.maxHealth);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            m_currentHealthPercent = 1f;
            m_isLowHealth = false;
            m_isCriticalHealth = false;
            m_currentAlpha = 0f;
            m_pulseTime = 0f;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Full red on death
            m_currentAlpha = m_criticalMaxAlpha;
            m_isLowHealth = false; // Stop pulsing
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled && m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
            }
        }
    }
}
