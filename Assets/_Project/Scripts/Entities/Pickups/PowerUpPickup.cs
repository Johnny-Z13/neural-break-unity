using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// PowerUp pickup - increases weapon power level.
    /// Based on TypeScript PowerUp.ts.
    ///
    /// Effect: +1 weapon power level (max 10)
    /// Color: Yellow/Orange
    /// </summary>
    public class PowerUpPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.PowerUp;

        [Header("PowerUp Settings")]
        [SerializeField] private int _powerIncrease = 1;
        [SerializeField] private Color _pickupColor = new Color(0.6f, 1f, 0.2f); // Yellow-green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            // Find weapon system on player
            WeaponSystem weapon = player.GetComponent<WeaponSystem>();
            if (weapon != null)
            {
                weapon.AddPowerLevel(_powerIncrease);
                Debug.Log($"[PowerUp] Weapon power increased by {_powerIncrease}");
            }
            else
            {
                // Try to find it in children
                weapon = player.GetComponentInChildren<WeaponSystem>();
                if (weapon != null)
                {
                    weapon.AddPowerLevel(_powerIncrease);
                    Debug.Log($"[PowerUp] Weapon power increased by {_powerIncrease}");
                }
                else
                {
                    Debug.LogWarning("[PowerUp] No WeaponSystem found on player!");
                }
            }
        }
    }
}
