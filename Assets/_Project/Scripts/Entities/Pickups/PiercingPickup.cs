using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Piercing pickup - projectiles pass through enemies.
    /// Temporary weapon upgrade.
    ///
    /// Effect: Projectiles pierce through up to 5 enemies
    /// Duration: 8 seconds
    /// Color: Red-Orange
    /// </summary>
    public class PiercingPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Piercing;

        [Header("Piercing Settings")]
        [SerializeField] private float _duration = 8f;
        [SerializeField] private Color _pickupColor = new Color(0.1f, 0.9f, 0.4f); // Dark green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            if (WeaponUpgradeManager.Instance != null)
            {
                WeaponUpgradeManager.Instance.ActivateUpgrade(PickupType.Piercing, _duration);
                Debug.Log($"[Piercing] Activated for {_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Piercing] WeaponUpgradeManager not found!");
            }
        }
    }
}
