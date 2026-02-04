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
        [SerializeField] private Image m_background;
        [SerializeField] private Image m_icon;
        [SerializeField] private Image m_tierGlow;
        [SerializeField] private TextMeshProUGUI m_nameText;
        [SerializeField] private TextMeshProUGUI m_descriptionText;
        [SerializeField] private TextMeshProUGUI m_tierText;
        [SerializeField] private Button m_button;

        [Header("Animation")]
        [SerializeField] private float m_hoverScale = 1.05f;
        [SerializeField] private float m_animationSpeed = 10f;

        private UpgradeDefinition m_upgrade;
        private System.Action m_onSelected;
        private Vector3 m_originalScale;
        private bool m_isHovered;

        // Optional components for polish
        private UpgradeCardAnimator m_animator;
        private Audio.UpgradeSystemAudio m_audioManager;

        public Button Button => m_button;

        private void Awake()
        {
            m_originalScale = transform.localScale;

            // Get optional components
            m_animator = GetComponent<UpgradeCardAnimator>();
            m_audioManager = FindFirstObjectByType<Audio.UpgradeSystemAudio>();

            // Setup button if it exists (for prefab-based cards)
            // For programmatically built cards, this is done in Initialize()
            if (m_button != null)
            {
                SetupButtonListener();
            }
        }

        private void Update()
        {
            // Smooth scale animation
            Vector3 targetScale = m_isHovered ? m_originalScale * m_hoverScale : m_originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * m_animationSpeed);
        }

        /// <summary>
        /// Initialize card with upgrade data.
        /// </summary>
        public void Initialize(UpgradeDefinition upgrade, System.Action onSelected)
        {
            m_upgrade = upgrade;
            m_onSelected = onSelected;

            // Re-capture original scale (in case it was set before transform was configured)
            m_originalScale = transform.localScale;
            if (m_originalScale == Vector3.zero)
            {
                m_originalScale = Vector3.one;
            }

            // Ensure UI components exist
            EnsureUIComponents();

            // Setup button click listener (may not have been set in Awake if built programmatically)
            SetupButtonListener();

            // Get animator reference if not set
            if (m_animator == null)
            {
                m_animator = GetComponent<UpgradeCardAnimator>();
            }

            // Update visuals
            UpdateVisuals();

            Debug.Log($"[UpgradeCard] Initialized: {upgrade.displayName}, Button={m_button != null}, NameText={m_nameText != null}, DescText={m_descriptionText != null}");
        }

        /// <summary>
        /// Setup button click listener if not already set.
        /// </summary>
        private void SetupButtonListener()
        {
            if (m_button == null) return;

            // Remove existing listeners and add fresh
            m_button.onClick.RemoveListener(OnClick);
            m_button.onClick.AddListener(OnClick);

            // Setup hover effects if not already done
            var existingTrigger = m_button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (existingTrigger == null)
            {
                var trigger = m_button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

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
            if (m_background == null)
            {
                m_background = GetComponent<Image>();
                if (m_background == null)
                {
                    m_background = gameObject.AddComponent<Image>();
                }
            }

            if (m_button == null)
            {
                m_button = GetComponent<Button>();
                if (m_button == null)
                {
                    m_button = gameObject.AddComponent<Button>();
                    m_button.onClick.AddListener(OnClick);
                }
            }

            // Create text elements if they don't exist
            if (m_nameText == null)
            {
                var nameObj = transform.Find("NameText");
                if (nameObj != null)
                {
                    m_nameText = nameObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_descriptionText == null)
            {
                var descObj = transform.Find("DescriptionText");
                if (descObj != null)
                {
                    m_descriptionText = descObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_tierText == null)
            {
                var tierObj = transform.Find("TierText");
                if (tierObj != null)
                {
                    m_tierText = tierObj.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_icon == null)
            {
                var iconObj = transform.Find("Icon");
                if (iconObj != null)
                {
                    m_icon = iconObj.GetComponent<Image>();
                }
            }

            if (m_tierGlow == null)
            {
                var glowObj = transform.Find("TierGlow");
                if (glowObj != null)
                {
                    m_tierGlow = glowObj.GetComponent<Image>();
                }
            }
        }

        private void UpdateVisuals()
        {
            if (m_upgrade == null) return;

            // Set name
            if (m_nameText != null)
            {
                m_nameText.text = m_upgrade.displayName.ToUpper();
                m_nameText.color = GetTierColor(m_upgrade.tier);
            }

            // Set description
            if (m_descriptionText != null)
            {
                m_descriptionText.text = m_upgrade.description;
                m_descriptionText.color = UITheme.TextSecondary;
            }

            // Set tier
            if (m_tierText != null)
            {
                m_tierText.text = $"[{m_upgrade.tier.ToString().ToUpper()}]";
                m_tierText.color = GetTierColor(m_upgrade.tier);
            }

            // Set icon
            if (m_icon != null && m_upgrade.icon != null)
            {
                m_icon.sprite = m_upgrade.icon;
                m_icon.color = m_upgrade.iconColor;
            }

            // Set background color
            if (m_background != null)
            {
                m_background.color = UITheme.BackgroundMedium;
            }

            // Set tier glow
            if (m_tierGlow != null)
            {
                m_tierGlow.color = GetTierColor(m_upgrade.tier).WithAlpha(0.2f);
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
            LogHelper.Log($"[UpgradeCard] Selected: {m_upgrade?.displayName}");

            // Play select effect before invoking callback
            PlaySelectEffect();

            m_onSelected?.Invoke();
        }

        private void OnPointerEnter()
        {
            m_isHovered = true;
            PlayHoverEffect();
        }

        private void OnPointerExit()
        {
            m_isHovered = false;
        }

        /// <summary>
        /// Set highlight state (called by UpgradeSelectionScreen for custom navigation).
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            m_isHovered = highlighted;

            // Update background color for visual feedback
            if (m_background != null)
            {
                m_background.color = highlighted
                    ? UITheme.BackgroundLight
                    : UITheme.BackgroundMedium;
            }

            // Trigger animator hover effects
            if (m_animator != null)
            {
                if (highlighted)
                {
                    m_animator.PlayHoverEffect();
                }
                else
                {
                    m_animator.StopHoverEffect();
                }
            }
        }

        public void PlayHoverEffect()
        {
            // Trigger animator
            if (m_animator != null)
            {
                m_animator.PlayHoverEffect();
            }

            // Play sound
            if (m_audioManager != null)
            {
                m_audioManager.PlayHoverSound();
            }
        }

        public void PlaySelectEffect()
        {
            // Trigger animator
            if (m_animator != null)
            {
                m_animator.PlaySelectEffect();
            }

            // Play sound
            if (m_audioManager != null)
            {
                m_audioManager.PlaySelectSound(UpgradeTier.Common); // Use tier from m_upgrade if needed
            }
        }
    }
}
