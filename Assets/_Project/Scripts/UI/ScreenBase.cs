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
        [SerializeField] protected GameObject m_screenRoot;
        [SerializeField] protected Selectable m_firstSelected;
        [SerializeField] protected bool m_allowCancelToClose = false;

        [Header("Animation")]
        [SerializeField] protected float m_fadeTime = 0.15f;
        [SerializeField] protected bool m_useAnimation = false;

        // State
        protected bool m_isVisible;
        protected CanvasGroup m_canvasGroup;

        public bool IsVisible => m_isVisible;

        protected virtual void Awake()
        {
            // If m_screenRoot is not assigned, use this gameObject as fallback
            if (m_screenRoot == null)
            {
                m_screenRoot = gameObject;
            }

            // Get or add canvas group for fade effects
            if (m_screenRoot != null)
            {
                m_canvasGroup = m_screenRoot.GetComponent<CanvasGroup>();
                if (m_canvasGroup == null && m_useAnimation)
                {
                    m_canvasGroup = m_screenRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        protected virtual void Update()
        {
            // Handle cancel input (B button / Escape) to close screen
            if (m_isVisible && m_allowCancelToClose)
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
            m_isVisible = true;

            // Ensure m_screenRoot is set (in case Show is called before Awake)
            if (m_screenRoot == null) m_screenRoot = gameObject;

            m_screenRoot.SetActive(true);

            // Select first button for keyboard/gamepad navigation
            SelectFirstElement();

            OnShow();
        }

        /// <summary>
        /// Select the first UI element for keyboard/gamepad navigation
        /// </summary>
        protected void SelectFirstElement()
        {
            if (m_firstSelected != null && EventSystem.current != null)
            {
                // Clear selection first to ensure fresh selection
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(m_firstSelected.gameObject);
            }
        }

        /// <summary>
        /// Hide this screen
        /// </summary>
        public virtual void Hide()
        {
            m_isVisible = false;

            // Ensure m_screenRoot is set (in case Hide is called before Awake)
            if (m_screenRoot == null) m_screenRoot = gameObject;

            Debug.Log($"[ScreenBase.Hide] {GetType().Name} - Setting {m_screenRoot.name} inactive");
            m_screenRoot.SetActive(false);

            OnHide();
        }

        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (m_isVisible)
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
