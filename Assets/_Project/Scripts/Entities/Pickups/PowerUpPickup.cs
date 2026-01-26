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
        [SerializeField] private Color _pickupColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald #00AA44

        [Header("Visual")]
        [SerializeField] private PowerUpVisuals _visuals;
        private bool _visualsGenerated;

        protected override Color GetPickupColor() => _pickupColor;

        public override void Initialize(Vector2 position, Transform playerTarget, System.Action<PickupBase> returnCallback)
        {
            base.Initialize(position, playerTarget, returnCallback);

            if (!_visualsGenerated)
            {
                EnsureVisuals();
                _visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            if (_visuals == null)
            {
                _visuals = GetComponentInChildren<PowerUpVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<PowerUpVisuals>();
                _visuals.SetLetter('P');
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            // Find weapon system on player
            WeaponSystem weapon = player.GetComponent<WeaponSystem>();
            if (weapon == null)
            {
                weapon = player.GetComponentInChildren<WeaponSystem>();
            }

            if (weapon != null)
            {
                // Increase power level
                weapon.AddPowerLevel(_powerIncrease);
                Debug.Log($"[PowerUp] Weapon power increased by {_powerIncrease}");

                // Also activate spread shot upgrade
                WeaponUpgradeManager upgradeManager = FindAnyObjectByType<WeaponUpgradeManager>();
                if (upgradeManager != null)
                {
                    upgradeManager.ActivateUpgrade(PickupType.SpreadShot);
                    Debug.Log($"[PowerUp] Spread shot activated! (3-gun)");
                }
            }
            else
            {
                Debug.LogWarning("[PowerUp] No WeaponSystem found on player!");
            }
        }
    }
}
