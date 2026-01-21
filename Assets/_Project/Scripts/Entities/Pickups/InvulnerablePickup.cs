using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Invulnerable pickup - grants temporary god mode.
    /// Based on TypeScript Invulnerable.ts.
    ///
    /// Effect: 7 seconds of invulnerability
    /// Color: Gold/White (rainbow pulse)
    /// Rarity: Very rare (1 per level, long spawn interval)
    /// </summary>
    public class InvulnerablePickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Invulnerable;

        [Header("Invulnerable Settings")]
        [SerializeField] private float _invulnerabilityDuration = 7f;
        [SerializeField] private Color _pickupColor = new Color(0.7f, 1f, 0.7f); // Bright pale green
        [SerializeField] private bool _useRainbowEffect = true;
        [SerializeField] private float _rainbowSpeed = 3f;

        protected override Color GetPickupColor() => _pickupColor;

        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();

            // Rainbow color effect for this special pickup
            if (_useRainbowEffect && _spriteRenderer != null)
            {
                float hue = (Time.time * _rainbowSpeed) % 1f;
                Color rainbow = Color.HSVToRGB(hue, 0.8f, 1f);
                _spriteRenderer.color = rainbow;
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ActivateInvulnerability(_invulnerabilityDuration);
                Debug.Log($"[Invulnerable] Activated for {_invulnerabilityDuration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Invulnerable] No PlayerHealth found on player!");
            }
        }
    }
}
