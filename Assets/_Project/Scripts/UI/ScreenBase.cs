using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
        [SerializeField] protected bool _allowCancelToClose = false;

        [Header("Animation")]
        [SerializeField] protected float _fadeTime = 0.15f;
        [SerializeField] protected bool _useAnimation = false;

        // State
        protected bool _isVisible;
        protected CanvasGroup _canvasGroup;

        public bool IsVisible => _isVisible;

        protected virtual void Awake()
        {
            // If _screenRoot is not assigned, use this gameObject as fallback
            if (_screenRoot == null)
            {
                _screenRoot = gameObject;
            }

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

        protected virtual void Update()
        {
            // Handle cancel input (B button / Escape) to close screen
            if (_isVisible && _allowCancelToClose)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    OnCancelPressed();
                }
                else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    OnCancelPressed();
                }
            }
        }

        /// <summary>
        /// Called when cancel is pressed (B/Escape) - override to handle
        /// </summary>
        protected virtual void OnCancelPressed()
        {
            Hide();
        }

        /// <summary>
        /// Show this screen
        /// </summary>
        public virtual void Show()
        {
            _isVisible = true;

            // Ensure _screenRoot is set (in case Show is called before Awake)
            if (_screenRoot == null) _screenRoot = gameObject;

            _screenRoot.SetActive(true);

            // Select first button for keyboard/gamepad navigation
            SelectFirstElement();

            OnShow();
        }

        /// <summary>
        /// Select the first UI element for keyboard/gamepad navigation
        /// </summary>
        protected void SelectFirstElement()
        {
            if (_firstSelected != null && EventSystem.current != null)
            {
                // Clear selection first to ensure fresh selection
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
            }
        }

        /// <summary>
        /// Hide this screen
        /// </summary>
        public virtual void Hide()
        {
            _isVisible = false;

            // Ensure _screenRoot is set (in case Hide is called before Awake)
            if (_screenRoot == null) _screenRoot = gameObject;

            _screenRoot.SetActive(false);

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
