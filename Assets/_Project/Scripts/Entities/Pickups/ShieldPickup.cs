using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Shield pickup - adds a shield charge.
    /// Based on TypeScript Shield.ts.
    ///
    /// Effect: +1 shield (max 3, absorbs 1 hit each)
    /// Color: Cyan/Blue
    /// </summary>
    public class ShieldPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Shield;

        [Header("Shield Settings")]
        [SerializeField] private Color _pickupColor = new Color(0.2f, 1f, 0.7f); // Teal green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.AddShield();
                Debug.Log("[Shield] Added shield to player");
            }
            else
            {
                Debug.LogWarning("[Shield] No PlayerHealth found on player!");
            }
        }
    }
}
