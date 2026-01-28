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
        [SerializeField] private Color _pickupColor = new Color(0f, 0.6f, 0.2f, 0.9f); // Forest Green #009933

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
                _visuals.SetLetter('P'); // P for Piercing
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            var upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (upgradeManager != null)
            {
                upgradeManager.ActivateUpgrade(PickupType.Piercing, _duration);
                Debug.Log($"[Piercing] Activated for {_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Piercing] WeaponUpgradeManager not found!");
            }
        }
    }
}
