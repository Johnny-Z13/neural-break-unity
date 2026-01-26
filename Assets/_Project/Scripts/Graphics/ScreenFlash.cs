using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// UI overlay for screen flash effects (damage, pickup, etc).
    /// Now uses EventBus instead of singleton pattern.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private float _defaultDuration = 0.1f;
        [SerializeField] private AnimationCurve _flashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // References
        private Image _flashImage;
        private Canvas _canvas;
        private Coroutine _flashCoroutine;

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

        private void SetupFlashUI()
        {
            // Create canvas if we don't have one
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                // Create a dedicated canvas for screen flash
                var canvasGO = new GameObject("ScreenFlashCanvas");
                canvasGO.transform.SetParent(transform.parent, false);
                _canvas = canvasGO.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 100; // On top of everything
                canvasGO.AddComponent<CanvasScaler>();
                transform.SetParent(canvasGO.transform, false);
            }

            // Create the flash image
            var flashGO = new GameObject("FlashImage");
            flashGO.transform.SetParent(transform, false);

            _flashImage = flashGO.AddComponent<Image>();
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;

            // Stretch to fill
            var rect = _flashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Start invisible
            _flashImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Flash the screen with a color
        /// </summary>
        public void Flash(Color color, float duration = -1f)
        {
            if (_flashImage == null) return;

            if (duration < 0) duration = _defaultDuration;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
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

        private IEnumerator FlashCoroutine(Color color, float duration)
        {
            _flashImage.gameObject.SetActive(true);

            float elapsed = 0f;
            Color startColor = color;
            Color endColor = new Color(color.r, color.g, color.b, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float curveValue = _flashCurve.Evaluate(t);

                _flashImage.color = Color.Lerp(startColor, endColor, 1f - curveValue);

                yield return null;
            }

            _flashImage.color = endColor;
            _flashImage.gameObject.SetActive(false);
            _flashCoroutine = null;
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
