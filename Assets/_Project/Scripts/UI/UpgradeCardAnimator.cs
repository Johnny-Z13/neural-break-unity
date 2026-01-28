using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Combat;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Handles all animations for upgrade cards.
    /// Simple built-in Unity animations (no external dependencies).
    /// </summary>
    [RequireComponent(typeof(UpgradeCard))]
    public class UpgradeCardAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _entranceDelay = 0f;
        [SerializeField] private float _entranceDuration = 0.5f;
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _hoverSpeed = 8f;
        [SerializeField] private float _selectPunchScale = 1.15f;
        [SerializeField] private float _selectDuration = 0.3f;

        [Header("References")]
        [SerializeField] private Image _background;
        [SerializeField] private CanvasGroup _canvasGroup;

        private UpgradeCard _card;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private float _currentScale = 1f;
        private bool _isHovered = false;
        private bool _isAnimatingEntrance = false;
        private float _entranceTimer = 0f;
        private bool _isAnimatingSelect = false;
        private float _selectTimer = 0f;

        private void Awake()
        {
            _card = GetComponent<UpgradeCard>();
            EnsureCanvasGroup();

            _originalScale = transform.localScale;
            if (_originalScale == Vector3.zero)
            {
                _originalScale = Vector3.one;
            }
            _targetScale = _originalScale;
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            // Entrance animation
            if (_isAnimatingEntrance)
            {
                _entranceTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_entranceTimer / _entranceDuration);

                // Ease out back
                t = 1f - Mathf.Pow(1f - t, 3f);

                if (_canvasGroup != null) _canvasGroup.alpha = t;
                _currentScale = Mathf.Lerp(0.8f, 1f, t);

                if (_entranceTimer >= _entranceDuration)
                {
                    _isAnimatingEntrance = false;
                    if (_canvasGroup != null) _canvasGroup.alpha = 1f;
                    _currentScale = 1f;
                }
            }

            // Selection punch animation
            if (_isAnimatingSelect)
            {
                _selectTimer += Time.unscaledDeltaTime;
                float t = _selectTimer / _selectDuration;

                if (t <= 0.5f)
                {
                    // Punch out
                    float punchT = t / 0.5f;
                    _currentScale = Mathf.Lerp(1f, _selectPunchScale, punchT);
                }
                else
                {
                    // Punch back
                    float punchT = (t - 0.5f) / 0.5f;
                    _currentScale = Mathf.Lerp(_selectPunchScale, 1f, punchT);
                }

                if (t >= 1f)
                {
                    _isAnimatingSelect = false;
                    _currentScale = 1f;
                }
            }

            // Hover scale
            if (!_isAnimatingSelect && !_isAnimatingEntrance)
            {
                float targetScale = _isHovered ? _hoverScale : 1f;
                _currentScale = Mathf.Lerp(_currentScale, targetScale, _hoverSpeed * Time.unscaledDeltaTime);
            }

            // Apply scale
            transform.localScale = _originalScale * _currentScale;
        }

        /// <summary>
        /// Play entrance animation with delay.
        /// </summary>
        public void PlayEntranceAnimation(float delay = 0f)
        {
            _entranceDelay = delay;
            StartCoroutine(EntranceCoroutine());
        }

        private System.Collections.IEnumerator EntranceCoroutine()
        {
            // Ensure canvas group exists
            EnsureCanvasGroup();

            // Re-capture original scale in case it wasn't set properly in Awake
            if (_originalScale == Vector3.zero)
            {
                _originalScale = transform.localScale;
                if (_originalScale == Vector3.zero)
                {
                    _originalScale = Vector3.one;
                }
            }

            // Start invisible
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            transform.localScale = _originalScale * 0.8f;

            // Wait for delay
            if (_entranceDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_entranceDelay);
            }

            // Start entrance animation
            _isAnimatingEntrance = true;
            _entranceTimer = 0f;
        }

        /// <summary>
        /// Play hover effect.
        /// </summary>
        public void PlayHoverEffect()
        {
            _isHovered = true;
        }

        /// <summary>
        /// Stop hover effect.
        /// </summary>
        public void StopHoverEffect()
        {
            _isHovered = false;
        }

        /// <summary>
        /// Play selection effect.
        /// </summary>
        public void PlaySelectEffect()
        {
            _isAnimatingSelect = true;
            _selectTimer = 0f;

            // Flash background
            if (_background != null)
            {
                StartCoroutine(FlashBackground());
            }
        }

        private System.Collections.IEnumerator FlashBackground()
        {
            Color originalColor = _background.color;
            Color flashColor = new Color(1f, 1f, 1f, 1f);

            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / duration;
                _background.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }

            _background.color = originalColor;
        }
    }
}
