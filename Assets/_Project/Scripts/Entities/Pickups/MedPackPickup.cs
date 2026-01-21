using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// MedPack pickup - heals the player.
    /// Based on TypeScript MedPack.ts.
    ///
    /// Effect: Heals 35 HP
    /// Color: Green
    /// Special: Only spawns when player health < 80%
    /// </summary>
    public class MedPackPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.MedPack;

        [Header("MedPack Settings")]
        [SerializeField] private int _healAmount = 35;
        [SerializeField] private Color _pickupColor = new Color(0.2f, 1f, 0.3f); // Bright green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(_healAmount);
                Debug.Log($"[MedPack] Healed player for {_healAmount}");
            }
            else
            {
                Debug.LogWarning("[MedPack] No PlayerHealth found on player!");
            }
        }
    }
}
