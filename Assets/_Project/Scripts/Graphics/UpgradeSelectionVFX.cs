using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.UI;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Visual effects for upgrade selection screen.
    /// Creates particle effects and screen flashes.
    /// </summary>
    public class UpgradeSelectionVFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem m_selectionBurstPrefab;
        [SerializeField] private Transform m_vfxContainer;

        [Header("Settings")]
        [SerializeField] private bool m_enableScreenFlash = true;
        [SerializeField] private bool m_enableParticles = true;

        private ScreenFlash m_screenFlash;

        private void Start()
        {
            // Find components (ParticleEffectFactory is static, no need to find)
            m_screenFlash = FindFirstObjectByType<ScreenFlash>();

            // Subscribe to events
            EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Subscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Unsubscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
        }

        private void OnUpgradeSelected(UpgradeSelectedEvent evt)
        {
            PlaySelectionVFX(evt.selected);
        }

        private void OnPermanentUpgradeAdded(PermanentUpgradeAddedEvent evt)
        {
            FlashScreenForTier(evt.upgrade.tier);
        }

        /// <summary>
        /// Play visual effects when upgrade is selected.
        /// </summary>
        private void PlaySelectionVFX(UpgradeDefinition upgrade)
        {
            if (!m_enableParticles) return;

            // Get tier color
            Color tierColor = GetTierColor(upgrade.tier);

            // Create particle burst at card position
            if (m_selectionBurstPrefab != null)
            {
                var burst = Instantiate(m_selectionBurstPrefab, Vector3.zero, Quaternion.identity, m_vfxContainer);
                var main = burst.main;
                main.startColor = tierColor;
                burst.Play();
                Destroy(burst.gameObject, 3f);
            }
            else
            {
                // ParticleEffectFactory doesn't have CreateBurst - would need to create a custom particle system
                // For now, skip particle creation if no prefab is provided
                Debug.Log($"[UpgradeSelectionVFX] No burst prefab assigned, skipping particle effect");
            }
        }

        /// <summary>
        /// Flash screen based on upgrade tier.
        /// </summary>
        private void FlashScreenForTier(UpgradeTier tier)
        {
            if (!m_enableScreenFlash) return;
            if (m_screenFlash == null) return;

            Color flashColor = GetTierColor(tier);
            float intensity = tier switch
            {
                UpgradeTier.Common => 0.1f,
                UpgradeTier.Rare => 0.15f,
                UpgradeTier.Epic => 0.2f,
                UpgradeTier.Legendary => 0.3f,
                _ => 0.1f
            };

            m_screenFlash.Flash(flashColor.WithAlpha(intensity), 0.3f);
        }

        /// <summary>
        /// Get color for tier.
        /// </summary>
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

        /// <summary>
        /// Create sparkles around a card (called from UpgradeCard).
        /// </summary>
        public void CreateCardSparkles(Vector3 position, Color color)
        {
            if (!m_enableParticles) return;

            // ParticleEffectFactory doesn't have CreateBurst - skip for now
            // Could use CreatePickupEffect as alternative
            Debug.Log($"[UpgradeSelectionVFX] CreateCardSparkles called at {position}");
        }
    }
}
