using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.UI;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Visual effects for upgrade selection screen.
    /// Creates particle effects and screen flashes.
    /// </summary>
    public class UpgradeSelectionVFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParticleSystem _selectionBurstPrefab;
        [SerializeField] private Transform _vfxContainer;

        [Header("Settings")]
        [SerializeField] private bool _enableScreenFlash = true;
        [SerializeField] private bool _enableParticles = true;

        private ScreenFlash _screenFlash;

        private void Start()
        {
            // Find components (ParticleEffectFactory is static, no need to find)
            _screenFlash = FindFirstObjectByType<ScreenFlash>();

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
            if (!_enableParticles) return;

            // Get tier color
            Color tierColor = GetTierColor(upgrade.tier);

            // Create particle burst at card position
            if (_selectionBurstPrefab != null)
            {
                var burst = Instantiate(_selectionBurstPrefab, Vector3.zero, Quaternion.identity, _vfxContainer);
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
            if (!_enableScreenFlash) return;
            if (_screenFlash == null) return;

            Color flashColor = GetTierColor(tier);
            float intensity = tier switch
            {
                UpgradeTier.Common => 0.1f,
                UpgradeTier.Rare => 0.15f,
                UpgradeTier.Epic => 0.2f,
                UpgradeTier.Legendary => 0.3f,
                _ => 0.1f
            };

            _screenFlash.Flash(flashColor.WithAlpha(intensity), 0.3f);
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
            if (!_enableParticles) return;

            // ParticleEffectFactory doesn't have CreateBurst - skip for now
            // Could use CreatePickupEffect as alternative
            Debug.Log($"[UpgradeSelectionVFX] CreateCardSparkles called at {position}");
        }
    }
}
