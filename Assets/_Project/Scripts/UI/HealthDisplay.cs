using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays player health bar and shield icons.
    /// Compatible with MMProgressBar if available.
    /// </summary>
    public class HealthDisplay : MonoBehaviour
    {
        [Header("Health Bar")]
        [SerializeField] private Image m_healthFill;
        [SerializeField] private Image m_healthBackground;
        [SerializeField] private TextMeshProUGUI m_healthText;
        [SerializeField] private Gradient m_healthGradient;

        [Header("Shield Icons (Uses UITheme)")]
        [SerializeField] private Transform m_shieldContainer;
        [SerializeField] private Image[] m_shieldIcons;
        [SerializeField] private bool m_useThemeColors = true;

        private Color ShieldActiveColor => m_useThemeColors ? UITheme.ShieldActive : m_customShieldActiveColor;
        private Color ShieldInactiveColor => m_useThemeColors ? UITheme.ShieldInactive : m_customShieldInactiveColor;

        [SerializeField] private Color m_customShieldActiveColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color m_customShieldInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float m_smoothSpeed = 10f;
        [SerializeField] private bool m_animateChanges = true;

        // State
        private float m_targetFillAmount;
        private float m_currentFillAmount;
        private int m_currentHealth;
        private int m_maxHealth;

        private void Awake()
        {
            // Initialize gradient from UITheme if not set
            if (m_healthGradient == null || m_useThemeColors)
            {
                m_healthGradient = UITheme.HealthGradient;
            }

            m_currentFillAmount = 1f;
            m_targetFillAmount = 1f;
        }

        private void Start()
        {
            // Verify health fill is properly configured
            if (m_healthFill != null)
            {
                // Ensure correct Image settings for fill
                m_healthFill.type = Image.Type.Filled;
                m_healthFill.fillMethod = Image.FillMethod.Horizontal;
                m_healthFill.fillOrigin = 0;
                m_healthFill.fillAmount = 1f;
            }
        }

        private void Update()
        {
            if (!m_animateChanges) return;

            // Smooth health bar animation
            if (Mathf.Abs(m_currentFillAmount - m_targetFillAmount) > 0.001f)
            {
                m_currentFillAmount = Mathf.Lerp(m_currentFillAmount, m_targetFillAmount, Time.unscaledDeltaTime * m_smoothSpeed);
                ApplyFillAmount(m_currentFillAmount);
            }
        }

        /// <summary>
        /// Update health display
        /// </summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            m_currentHealth = currentHealth;
            m_maxHealth = maxHealth;

            float percent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            m_targetFillAmount = percent;
            m_currentFillAmount = percent;  // Sync immediately for responsive feedback

            ApplyFillAmount(percent);

            // Update text
            if (m_healthText != null)
            {
                m_healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }

        /// <summary>
        /// Update shield icons
        /// </summary>
        public void UpdateShields(int currentShields, int maxShields)
        {
            if (m_shieldIcons == null) return;

            for (int i = 0; i < m_shieldIcons.Length; i++)
            {
                if (m_shieldIcons[i] == null) continue;

                bool isActive = i < currentShields;
                m_shieldIcons[i].color = isActive ? ShieldActiveColor : ShieldInactiveColor;
                m_shieldIcons[i].gameObject.SetActive(i < maxShields);
            }
        }

        /// <summary>
        /// Reset display to full health
        /// </summary>
        public void ResetDisplay()
        {
            m_currentFillAmount = 1f;
            m_targetFillAmount = 1f;
            ApplyFillAmount(1f);

            if (m_healthText != null)
            {
                m_healthText.text = "";
            }

            // Reset shields
            if (m_shieldIcons != null)
            {
                foreach (var icon in m_shieldIcons)
                {
                    if (icon != null)
                    {
                        icon.color = ShieldInactiveColor;
                    }
                }
            }
        }

        private void ApplyFillAmount(float amount)
        {
            if (m_healthFill == null) return;

            m_healthFill.fillAmount = amount;

            // Apply color from gradient (green -> yellow -> red)
            if (m_healthGradient != null)
            {
                m_healthFill.color = m_healthGradient.Evaluate(amount);
            }
        }

        #region Debug

        [ContextMenu("Debug: 50% Health")]
        private void DebugHalfHealth() => UpdateHealth(65, 130);

        [ContextMenu("Debug: Low Health")]
        private void DebugLowHealth() => UpdateHealth(20, 130);

        [ContextMenu("Debug: Full Health")]
        private void DebugFullHealth() => UpdateHealth(130, 130);

        [ContextMenu("Debug: 2 Shields")]
        private void DebugShields() => UpdateShields(2, 3);

        #endregion
    }
}
