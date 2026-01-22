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
                _visuals.SetLetter('R');
            }
        }

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
