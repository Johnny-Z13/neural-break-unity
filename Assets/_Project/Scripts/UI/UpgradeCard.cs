using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Combat;
using NeuralBreak.Utils;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Individual upgrade card component.
    /// Displays upgrade info and handles selection.
    /// </summary>
    public class UpgradeCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _tierGlow;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _tierText;
        [SerializeField] private Button _button;

        [Header("Animation")]
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _animationSpeed = 10f;

        private UpgradeDefinition _upgrade;
        private System.Action _onSelected;
        private Vector3 _originalScale;
        private bool _isHovered;

        // Optional components for polish
        private UpgradeCardAnimator _animator;
        private Audio.UpgradeSystemAudio _audioManager;

        public Button Button => _button;

        private void Awake()
        {
            _originalScale = transform.localScale;

            // Get optional components
            _animator = GetComponent<UpgradeCardAnimator>();
            _audioManager = FindFirstObjectByType<Audio.UpgradeSystemAudio>();

            // Setup button if it exists (for prefab-based cards)
            // For programmatically built cards, this is done in Initialize()
            if (_button != null)
            {
                SetupButtonListener();
            }
        }

        private void Update()
        {
            // Smooth scale animation
            Vector3 targetScale = _isHovered ? _originalScale * _hoverScale : _originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * _animationSpeed);
        }

        /// <summary>
        /// Initialize card with upgrade data.
        /// </summary>
        public void Initialize(UpgradeDefinition upgrade, System.Action onSelected)
        {
            _upgrade = upgrade;
            _onSelected = onSelected;

            // Re-capture original scale (in case it was set before transform was configured)
            _originalScale = transform.localScale;
            if (_originalScale == Vector3.zero)
            {
                _originalScale = Vector3.one;
            }

            // Ensure UI components exist
            EnsureUIComponents();

            // Setup button click listener (may not have been set in Awake if built programmatically)
            SetupButtonListener();

            // Get animator reference if not set
            if (_animator == null)
            {
                _animator = GetComponent<UpgradeCardAnimator>();
            }

            // Update visuals
            UpdateVisuals();

            Debug.Log($"[UpgradeCard] Initialized: {upgrade.displayName}, Button={_button != null}, NameText={_nameText != null}, DescText={_descriptionText != null}");
        }

        /// <summary>
        /// Setup button click listener if not already set.
        /// </summary>
        private void SetupButtonListener()
        {
            if (_button == null) return;

            // Remove existing listeners and add fresh
            _button.onClick.RemoveListener(OnClick);
            _button.onClick.AddListener(OnClick);

            // Setup hover effects if not already done
            var existingTrigger = _button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (existingTrigger == null)
            {
                var trigger = _button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
                };
                pointerEnter.callback.AddListener((data) => OnPointerEnter());
                trigger.triggers.Add(pointerEnter);

                var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
                };
                pointerExit.callback.AddListener((data) => OnPointerExit());
                trigger.triggers.Add(pointerExit);

                var select = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.Select
                };
                select.callback.AddListener((data) => OnPointerEnter());
                trigger.triggers.Add(select);

                var deselect = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.Deselect
                };
                deselect.callback.AddListener((data) => OnPointerExit());
                trigger.triggers.Add(deselect);
            }
        }

        private void EnsureUIComponents()
        {
            // Create UI elements if they don't exist
            if (_background == null)
            {
                _background = GetComponent<Image>();
                if (_background == null)
                {
                    _background = gameObject.AddComponent<Image>();
                }
            }

            if (_button == null)
            {
                _button = GetComponent<Button>();
                if (_button == null)
                {
                    _button = gameObject.AddComponent<Button>();
                    _button.onClick.AddListener(OnClick);
                }
            }

            // Create text elements if they don't exist
            if (_nameText == null)
            {
                var nameObj = transform.Find("NameText");
                if (nameObj != null)
                {
                    _nameText = nameObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_descriptionText == null)
            {
                var descObj = transform.Find("DescriptionText");
                if (descObj != null)
                {
                    _descriptionText = descObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_tierText == null)
            {
                var tierObj = transform.Find("TierText");
                if (tierObj != null)
                {
                    _tierText = tierObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_icon == null)
            {
                var iconObj = transform.Find("Icon");
                if (iconObj != null)
                {
                    _icon = iconObj.GetComponent<Image>();
                }
            }

            if (_tierGlow == null)
            {
                var glowObj = transform.Find("TierGlow");
                if (glowObj != null)
                {
                    _tierGlow = glowObj.GetComponent<Image>();
                }
            }
        }

        private void UpdateVisuals()
        {
            if (_upgrade == null) return;

            // Set name
            if (_nameText != null)
            {
                _nameText.text = _upgrade.displayName.ToUpper();
                _nameText.color = GetTierColor(_upgrade.tier);
            }

            // Set description
            if (_descriptionText != null)
            {
                _descriptionText.text = _upgrade.description;
                _descriptionText.color = UITheme.TextSecondary;
            }

            // Set tier
            if (_tierText != null)
            {
                _tierText.text = $"[{_upgrade.tier.ToString().ToUpper()}]";
                _tierText.color = GetTierColor(_upgrade.tier);
            }

            // Set icon
            if (_icon != null && _upgrade.icon != null)
            {
                _icon.sprite = _upgrade.icon;
                _icon.color = _upgrade.iconColor;
            }

            // Set background color
            if (_background != null)
            {
                _background.color = UITheme.BackgroundMedium;
            }

            // Set tier glow
            if (_tierGlow != null)
            {
                _tierGlow.color = GetTierColor(_upgrade.tier).WithAlpha(0.2f);
            }
        }

        private Color GetTierColor(UpgradeTier tier)
        {
            return tier switch
            {
                UpgradeTier.Common => UITheme.TextSecondary,
                UpgradeTier.Rare => UITheme.Primary,
                UpgradeTier.Epic => UITheme.Accent,
                UpgradeTier.Legendary => UITheme.Warning,
                _ => Color.white
            };
        }

        private void OnClick()
        {
            LogHelper.Log($"[UpgradeCard] Selected: {_upgrade?.displayName}");

            // Play select effect before invoking callback
            PlaySelectEffect();

            _onSelected?.Invoke();
        }

        private void OnPointerEnter()
        {
            _isHovered = true;
            PlayHoverEffect();
        }

        private void OnPointerExit()
        {
            _isHovered = false;
        }

        public void PlayHoverEffect()
        {
            // Trigger animator
            if (_animator != null)
            {
                _animator.PlayHoverEffect();
            }

            // Play sound
            if (_audioManager != null)
            {
                _audioManager.PlayHoverSound();
            }
        }

        public void PlaySelectEffect()
        {
            // Trigger animator
            if (_animator != null)
            {
                _animator.PlaySelectEffect();
            }

            // Play sound
            if (_audioManager != null)
            {
                _audioManager.PlaySelectSound(UpgradeTier.Common); // Use tier from _upgrade if needed
            }
        }
    }
}
