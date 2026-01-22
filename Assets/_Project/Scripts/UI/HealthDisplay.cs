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
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _healthBackground;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private Gradient _healthGradient;

        [Header("Shield Icons (Uses UITheme)")]
        [SerializeField] private Transform _shieldContainer;
        [SerializeField] private Image[] _shieldIcons;
        [SerializeField] private bool _useThemeColors = true;

        private Color ShieldActiveColor => _useThemeColors ? UITheme.ShieldActive : _customShieldActiveColor;
        private Color ShieldInactiveColor => _useThemeColors ? UITheme.ShieldInactive : _customShieldInactiveColor;

        [SerializeField] private Color _customShieldActiveColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color _customShieldInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float _smoothSpeed = 10f;
        [SerializeField] private bool _animateChanges = true;

        // State
        private float _targetFillAmount;
        private float _currentFillAmount;
        private int _currentHealth;
        private int _maxHealth;

        private void Awake()
        {
            // Initialize gradient from UITheme if not set
            if (_healthGradient == null || _useThemeColors)
            {
                _healthGradient = UITheme.HealthGradient;
            }

            _currentFillAmount = 1f;
            _targetFillAmount = 1f;
        }

        private void Update()
        {
            if (!_animateChanges) return;

            // Smooth health bar animation
            if (Mathf.Abs(_currentFillAmount - _targetFillAmount) > 0.001f)
            {
                _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, Time.unscaledDeltaTime * _smoothSpeed);
                ApplyFillAmount(_currentFillAmount);
            }
        }

        /// <summary>
        /// Update health display
        /// </summary>
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;

            float percent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            _targetFillAmount = percent;

            if (!_animateChanges)
            {
                _currentFillAmount = percent;
                ApplyFillAmount(percent);
            }

            // Update text
            if (_healthText != null)
            {
                _healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }

        /// <summary>
        /// Update shield icons
        /// </summary>
        public void UpdateShields(int currentShields, int maxShields)
        {
            if (_shieldIcons == null) return;

            for (int i = 0; i < _shieldIcons.Length; i++)
            {
                if (_shieldIcons[i] == null) continue;

                bool isActive = i < currentShields;
                _shieldIcons[i].color = isActive ? ShieldActiveColor : ShieldInactiveColor;
                _shieldIcons[i].gameObject.SetActive(i < maxShields);
            }
        }

        /// <summary>
        /// Reset display to full health
        /// </summary>
        public void ResetDisplay()
        {
            _currentFillAmount = 1f;
            _targetFillAmount = 1f;
            ApplyFillAmount(1f);

            if (_healthText != null)
            {
                _healthText.text = "";
            }

            // Reset shields
            if (_shieldIcons != null)
            {
                foreach (var icon in _shieldIcons)
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
            if (_healthFill != null)
            {
                _healthFill.fillAmount = amount;

                // Apply color from gradient
                if (_healthGradient != null)
                {
                    _healthFill.color = _healthGradient.Evaluate(amount);
                }
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
