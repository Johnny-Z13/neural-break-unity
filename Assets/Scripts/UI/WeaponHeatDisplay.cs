using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays weapon heat meter with overheat warning.
    /// </summary>
    public class WeaponHeatDisplay : MonoBehaviour
    {
        [Header("Heat Bar")]
        [SerializeField] private Image m_heatFill;
        [SerializeField] private Image m_heatBackground;
        [SerializeField] private Gradient m_heatGradient;

        [Header("Power Level")]
        [SerializeField] private TextMeshProUGUI m_powerLevelText;
        [SerializeField] private Image[] m_powerLevelPips;
        [SerializeField] private Color m_pipActiveColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color m_pipInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Overheat Warning")]
        [SerializeField] private TextMeshProUGUI m_overheatText;
        [SerializeField] private GameObject m_warningContainer;
        [SerializeField] private float m_warningBlinkRate = 4f;

        [Header("Animation")]
        [SerializeField] private float m_smoothSpeed = 15f;
        [SerializeField] private bool m_animateChanges = true;

        // State
        private float m_targetFillAmount;
        private float m_currentFillAmount;
        private bool m_isOverheated;
        private float m_blinkTimer;

        private void Awake()
        {
            // Initialize default gradient if not set
            if (m_heatGradient == null)
            {
                m_heatGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(Color.yellow, 0.6f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.85f),
                    new GradientColorKey(Color.red, 1f)
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(1f, 1f)
                };
                m_heatGradient.SetKeys(colorKeys, alphaKeys);
            }

            m_currentFillAmount = 0f;
            m_targetFillAmount = 0f;

            if (m_warningContainer != null)
            {
                m_warningContainer.SetActive(false);
            }
        }

        private void Update()
        {
            // Smooth heat bar animation
            if (m_animateChanges && Mathf.Abs(m_currentFillAmount - m_targetFillAmount) > 0.001f)
            {
                m_currentFillAmount = Mathf.Lerp(m_currentFillAmount, m_targetFillAmount, Time.unscaledDeltaTime * m_smoothSpeed);
                ApplyFillAmount(m_currentFillAmount);
            }

            // Blink warning when overheated
            if (m_isOverheated && m_warningContainer != null)
            {
                m_blinkTimer += Time.unscaledDeltaTime * m_warningBlinkRate;
                bool visible = Mathf.Sin(m_blinkTimer * Mathf.PI * 2f) > 0f;
                m_warningContainer.SetActive(visible);
            }
        }

        /// <summary>
        /// Update heat display
        /// </summary>
        public void UpdateHeat(float heat, float maxHeat, bool isOverheated)
        {
            float percent = maxHeat > 0 ? heat / maxHeat : 0f;
            m_targetFillAmount = percent;
            m_isOverheated = isOverheated;

            if (!m_animateChanges)
            {
                m_currentFillAmount = percent;
                ApplyFillAmount(percent);
            }

            // Show/hide overheat warning
            if (m_overheatText != null)
            {
                m_overheatText.gameObject.SetActive(isOverheated);
            }

            if (!isOverheated && m_warningContainer != null)
            {
                m_warningContainer.SetActive(false);
                m_blinkTimer = 0f;
            }
        }

        /// <summary>
        /// Update power level display
        /// </summary>
        public void UpdatePowerLevel(int currentLevel, int maxLevel)
        {
            if (m_powerLevelText != null)
            {
                m_powerLevelText.text = $"PWR {currentLevel}";
            }

            if (m_powerLevelPips != null)
            {
                for (int i = 0; i < m_powerLevelPips.Length; i++)
                {
                    if (m_powerLevelPips[i] != null)
                    {
                        bool isActive = i < currentLevel;
                        m_powerLevelPips[i].color = isActive ? m_pipActiveColor : m_pipInactiveColor;
                        m_powerLevelPips[i].gameObject.SetActive(i < maxLevel);
                    }
                }
            }
        }

        /// <summary>
        /// Reset display
        /// </summary>
        public void ResetDisplay()
        {
            m_currentFillAmount = 0f;
            m_targetFillAmount = 0f;
            m_isOverheated = false;
            m_blinkTimer = 0f;

            ApplyFillAmount(0f);

            if (m_warningContainer != null)
            {
                m_warningContainer.SetActive(false);
            }

            if (m_overheatText != null)
            {
                m_overheatText.gameObject.SetActive(false);
            }

            // Reset power level
            UpdatePowerLevel(0, 10);
        }

        private void ApplyFillAmount(float amount)
        {
            if (m_heatFill != null)
            {
                m_heatFill.fillAmount = amount;

                // Apply color from gradient
                if (m_heatGradient != null)
                {
                    m_heatFill.color = m_heatGradient.Evaluate(amount);
                }
            }
        }

        #region Debug

        [ContextMenu("Debug: 50% Heat")]
        private void DebugHalfHeat() => UpdateHeat(50f, 100f, false);

        [ContextMenu("Debug: Overheat")]
        private void DebugOverheat() => UpdateHeat(100f, 100f, true);

        [ContextMenu("Debug: Cool")]
        private void DebugCool() => UpdateHeat(0f, 100f, false);

        #endregion
    }
}
