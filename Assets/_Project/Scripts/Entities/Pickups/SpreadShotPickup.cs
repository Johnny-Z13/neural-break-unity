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
        [SerializeField] private Color _pickupColor = new Color(0f, 0.87f, 0.33f, 0.9f); // Jade #00DD55

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
                _visuals.SetLetter('S'); // S for Spread
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            // Publish event for WeaponUpgradeManager to handle
            EventBus.Publish(new WeaponUpgradeActivatedEvent
            {
                upgradeType = PickupType.SpreadShot,
                duration = _duration
            });
            Debug.Log($"[SpreadShot] Activated for {_duration} seconds!");
        }
    }
}
