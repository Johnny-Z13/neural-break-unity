using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Smart Bomb pickup - adds a smart bomb to player's inventory.
    /// Screen-clearing super weapon that kills all enemies.
    ///
    /// Effect: +1 Smart Bomb (max 3)
    /// Color: Orange/Gold
    /// </summary>
    public class SmartBombPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.SmartBomb;

        [Header("Smart Bomb Settings")]
        [SerializeField] private Color _pickupColor = new Color(1f, 0.6f, 0.1f, 0.9f); // Orange/Gold

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
                _visuals.SetLetter('B'); // B for Bomb
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            SmartBombSystem bombSystem = player.GetComponent<SmartBombSystem>();
            if (bombSystem != null)
            {
                bombSystem.AddBomb();
                Debug.Log("[SmartBombPickup] Added smart bomb to player!");
            }
            else
            {
                Debug.LogWarning("[SmartBombPickup] No SmartBombSystem found on player!");
            }
        }
    }
}
