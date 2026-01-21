using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Homing pickup - projectiles track nearby enemies.
    /// Temporary weapon upgrade.
    ///
    /// Effect: Projectiles home in on enemies
    /// Duration: 8 seconds
    /// Color: Green
    /// </summary>
    public class HomingPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Homing;

        [Header("Homing Settings")]
        [SerializeField] private float _duration = 8f;
        [SerializeField] private Color _pickupColor = new Color(0.3f, 1f, 0.4f); // Green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            if (WeaponUpgradeManager.Instance != null)
            {
                WeaponUpgradeManager.Instance.ActivateUpgrade(PickupType.Homing, _duration);
                Debug.Log($"[Homing] Activated for {_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Homing] WeaponUpgradeManager not found!");
            }
        }
    }
}
