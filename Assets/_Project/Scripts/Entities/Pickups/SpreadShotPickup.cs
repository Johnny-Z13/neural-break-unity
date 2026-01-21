using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Spread Shot pickup - fires multiple projectiles in a spread pattern.
    /// Temporary weapon upgrade.
    ///
    /// Effect: Fire 3 projectiles at once in a spread
    /// Duration: 10 seconds
    /// Color: Orange
    /// </summary>
    public class SpreadShotPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.SpreadShot;

        [Header("Spread Shot Settings")]
        [SerializeField] private float _duration = 10f;
        [SerializeField] private Color _pickupColor = new Color(0.4f, 1f, 0.6f); // Light green

        protected override Color GetPickupColor() => _pickupColor;

        protected override void ApplyEffect(GameObject player)
        {
            if (WeaponUpgradeManager.Instance != null)
            {
                WeaponUpgradeManager.Instance.ActivateUpgrade(PickupType.SpreadShot, _duration);
                Debug.Log($"[SpreadShot] Activated for {_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[SpreadShot] WeaponUpgradeManager not found!");
            }
        }
    }
}
