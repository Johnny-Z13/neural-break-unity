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
                _visuals.SetLetter('H');
            }
        }

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
