using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Base class for menu screens. Provides show/hide functionality
    /// and optional first-selected button for keyboard/gamepad navigation.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        [Header("Screen Settings")]
        [SerializeField] protected GameObject _screenRoot;
        [SerializeField] protected Selectable _firstSelected;

        [Header("Animation")]
        [SerializeField] protected float _fadeTime = 0.15f;
        [SerializeField] protected bool _useAnimation = false;

        // State
        protected bool _isVisible;
        protected CanvasGroup _canvasGroup;

        public bool IsVisible => _isVisible;

        protected virtual void Awake()
        {
            // Get or add canvas group for fade effects
            if (_screenRoot != null)
            {
                _canvasGroup = _screenRoot.GetComponent<CanvasGroup>();
                if (_canvasGroup == null && _useAnimation)
                {
                    _canvasGroup = _screenRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// Show this screen
        /// </summary>
        public virtual void Show()
        {
            _isVisible = true;

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(true);
            }

            // Select first button for keyboard/gamepad
            if (_firstSelected != null)
            {
                EventSystem.current?.SetSelectedGameObject(_firstSelected.gameObject);
            }

            OnShow();
        }

        /// <summary>
        /// Hide this screen
        /// </summary>
        public virtual void Hide()
        {
            _isVisible = false;

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }

            OnHide();
        }

        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Called when the screen is shown - override for custom behavior
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called when the screen is hidden - override for custom behavior
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// Refresh screen data - call when underlying data changes
        /// </summary>
        public virtual void Refresh() { }
    }
}
