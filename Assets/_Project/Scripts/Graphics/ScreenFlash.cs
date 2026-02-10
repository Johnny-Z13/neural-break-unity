using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// UI overlay for screen flash effects (damage, pickup, etc).
    /// Now uses EventBus instead of singleton pattern.
    ///
    /// ZERO-ALLOCATION: Flash effect uses timer-based Update instead of coroutines.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private float m_defaultDuration = 0.1f;
        [SerializeField] private AnimationCurve m_flashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // References
        private Image m_flashImage;
        private Canvas m_canvas;

        // Timer-based flash state (replaces coroutine - zero allocation)
        private bool m_isFlashing;
        private float m_flashElapsed;
        private float m_flashDuration;
        private Color m_flashStartColor;
        private Color m_flashEndColor;

        private void Awake()
        {
            SetupFlashUI();
        }

        private void Start()
        {
            // Subscribe to flash request events
            EventBus.Subscribe<ScreenFlashRequestEvent>(OnScreenFlashRequest);
            EventBus.Subscribe<DamageFlashRequestEvent>(OnDamageFlashRequest);
            EventBus.Subscribe<HealFlashRequestEvent>(OnHealFlashRequest);
            EventBus.Subscribe<PickupFlashRequestEvent>(OnPickupFlashRequest);

            // Auto-respond to gameplay events
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ScreenFlashRequestEvent>(OnScreenFlashRequest);
            EventBus.Unsubscribe<DamageFlashRequestEvent>(OnDamageFlashRequest);
            EventBus.Unsubscribe<HealFlashRequestEvent>(OnHealFlashRequest);
            EventBus.Unsubscribe<PickupFlashRequestEvent>(OnPickupFlashRequest);

            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void Update()
        {
            if (!m_isFlashing) return;

            m_flashElapsed += Time.unscaledDeltaTime;

            if (m_flashElapsed >= m_flashDuration)
            {
                // Flash complete
                m_flashImage.color = m_flashEndColor;
                m_flashImage.gameObject.SetActive(false);
                m_isFlashing = false;
            }
            else
            {
                float t = m_flashElapsed / m_flashDuration;
                float curveValue = m_flashCurve.Evaluate(t);
                m_flashImage.color = Color.Lerp(m_flashStartColor, m_flashEndColor, 1f - curveValue);
            }
        }

        private void SetupFlashUI()
        {
            // Create canvas if we don't have one
            m_canvas = GetComponentInParent<Canvas>();
            if (m_canvas == null)
            {
                // Create a dedicated canvas for screen flash
                var canvasGO = new GameObject("ScreenFlashCanvas");
                canvasGO.transform.SetParent(transform.parent, false);
                m_canvas = canvasGO.AddComponent<Canvas>();
                m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                m_canvas.sortingOrder = 100; // On top of everything
                canvasGO.AddComponent<CanvasScaler>();
                transform.SetParent(canvasGO.transform, false);
            }

            // Create the flash image
            var flashGO = new GameObject("FlashImage");
            flashGO.transform.SetParent(transform, false);

            m_flashImage = flashGO.AddComponent<Image>();
            m_flashImage.color = new Color(1f, 1f, 1f, 0f);
            m_flashImage.raycastTarget = false;

            // Stretch to fill
            var rect = m_flashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Start invisible
            m_flashImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Flash the screen with a color.
        /// Timer-based (zero allocation - no coroutines).
        /// </summary>
        public void Flash(Color color, float duration = -1f)
        {
            if (m_flashImage == null) return;

            if (duration < 0) duration = m_defaultDuration;

            // Start flash (timer-based - replaces StartCoroutine)
            m_flashImage.gameObject.SetActive(true);
            m_isFlashing = true;
            m_flashElapsed = 0f;
            m_flashDuration = duration;
            m_flashStartColor = color;
            m_flashEndColor = new Color(color.r, color.g, color.b, 0f);
        }

        /// <summary>
        /// Quick damage flash (red)
        /// </summary>
        public void DamageFlash(float intensity = 0.3f)
        {
            Flash(new Color(1f, 0f, 0f, intensity), 0.15f);
        }

        /// <summary>
        /// Quick heal flash (green)
        /// </summary>
        public void HealFlash(float intensity = 0.2f)
        {
            Flash(new Color(0f, 1f, 0f, intensity), 0.2f);
        }

        /// <summary>
        /// Quick pickup flash (yellow/gold)
        /// </summary>
        public void PickupFlash(float intensity = 0.2f)
        {
            Flash(new Color(1f, 0.8f, 0f, intensity), 0.15f);
        }

        #region Event Handlers

        private void OnScreenFlashRequest(ScreenFlashRequestEvent evt)
        {
            Flash(evt.color, evt.duration);
        }

        private void OnDamageFlashRequest(DamageFlashRequestEvent evt)
        {
            DamageFlash(evt.intensity);
        }

        private void OnHealFlashRequest(HealFlashRequestEvent evt)
        {
            HealFlash(evt.intensity);
        }

        private void OnPickupFlashRequest(PickupFlashRequestEvent evt)
        {
            PickupFlash(evt.intensity);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            DamageFlash();
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            HealFlash();
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            PickupFlash();
        }

        #endregion
    }
}
