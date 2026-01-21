using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Rapid Fire pickup - doubles fire rate temporarily.
    /// Temporary weapon upgrade.
    ///
    /// Effect: 2x fire rate
    /// Duration: 6 seconds
    /// Color: Blue
    /// </summary>
    public class RapidFirePickup : PickupBase
    {
        public override PickupType PickupType => PickupType.RapidFire;

        [Header("Rapid Fire Settings")]
        [SerializeField] private float _duration = 6f;
        [SerializeField] private Color _pickupColor = new Color(0.3f, 1f, 0.5f); // Medium green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            if (WeaponUpgradeManager.Instance != null)
            {
                WeaponUpgradeManager.Instance.ActivateUpgrade(PickupType.RapidFire, _duration);
                Debug.Log($"[RapidFire] Activated for {_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[RapidFire] WeaponUpgradeManager not found!");
            }
        }
    }
}
