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
        [SerializeField] private Image _heatFill;
        [SerializeField] private Image _heatBackground;
        [SerializeField] private Gradient _heatGradient;

        [Header("Power Level")]
        [SerializeField] private TextMeshProUGUI _powerLevelText;
        [SerializeField] private Image[] _powerLevelPips;
        [SerializeField] private Color _pipActiveColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _pipInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Overheat Warning")]
        [SerializeField] private TextMeshProUGUI _overheatText;
        [SerializeField] private GameObject _warningContainer;
        [SerializeField] private float _warningBlinkRate = 4f;

        [Header("Animation")]
        [SerializeField] private float _smoothSpeed = 15f;
        [SerializeField] private bool _animateChanges = true;

        // State
        private float _targetFillAmount;
        private float _currentFillAmount;
        private bool _isOverheated;
        private float _blinkTimer;

        private void Awake()
        {
            // Initialize default gradient if not set
            if (_heatGradient == null)
            {
                _heatGradient = new Gradient();
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
                _heatGradient.SetKeys(colorKeys, alphaKeys);
            }

            _currentFillAmount = 0f;
            _targetFillAmount = 0f;

            if (_warningContainer != null)
            {
                _warningContainer.SetActive(false);
            }
        }

        private void Update()
        {
            // Smooth heat bar animation
            if (_animateChanges && Mathf.Abs(_currentFillAmount - _targetFillAmount) > 0.001f)
            {
                _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, Time.unscaledDeltaTime * _smoothSpeed);
                ApplyFillAmount(_currentFillAmount);
            }

            // Blink warning when overheated
            if (_isOverheated && _warningContainer != null)
            {
                _blinkTimer += Time.unscaledDeltaTime * _warningBlinkRate;
                bool visible = Mathf.Sin(_blinkTimer * Mathf.PI * 2f) > 0f;
                _warningContainer.SetActive(visible);
            }
        }

        /// <summary>
        /// Update heat display
        /// </summary>
        public void UpdateHeat(float heat, float maxHeat, bool isOverheated)
        {
            float percent = maxHeat > 0 ? heat / maxHeat : 0f;
            _targetFillAmount = percent;
            _isOverheated = isOverheated;

            if (!_animateChanges)
            {
                _currentFillAmount = percent;
                ApplyFillAmount(percent);
            }

            // Show/hide overheat warning
            if (_overheatText != null)
            {
                _overheatText.gameObject.SetActive(isOverheated);
            }

            if (!isOverheated && _warningContainer != null)
            {
                _warningContainer.SetActive(false);
                _blinkTimer = 0f;
            }
        }

        /// <summary>
        /// Update power level display
        /// </summary>
        public void UpdatePowerLevel(int currentLevel, int maxLevel)
        {
            if (_powerLevelText != null)
            {
                _powerLevelText.text = $"PWR {currentLevel}";
            }

            if (_powerLevelPips != null)
            {
                for (int i = 0; i < _powerLevelPips.Length; i++)
                {
                    if (_powerLevelPips[i] != null)
                    {
                        bool isActive = i < currentLevel;
                        _powerLevelPips[i].color = isActive ? _pipActiveColor : _pipInactiveColor;
                        _powerLevelPips[i].gameObject.SetActive(i < maxLevel);
                    }
                }
            }
        }

        /// <summary>
        /// Reset display
        /// </summary>
        public void ResetDisplay()
        {
            _currentFillAmount = 0f;
            _targetFillAmount = 0f;
            _isOverheated = false;
            _blinkTimer = 0f;

            ApplyFillAmount(0f);

            if (_warningContainer != null)
            {
                _warningContainer.SetActive(false);
            }

            if (_overheatText != null)
            {
                _overheatText.gameObject.SetActive(false);
            }

            // Reset power level
            UpdatePowerLevel(0, 10);
        }

        private void ApplyFillAmount(float amount)
        {
            if (_heatFill != null)
            {
                _heatFill.fillAmount = amount;

                // Apply color from gradient
                if (_heatGradient != null)
                {
                    _heatFill.color = _heatGradient.Evaluate(amount);
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
