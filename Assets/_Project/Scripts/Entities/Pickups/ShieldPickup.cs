using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Shield pickup - adds a shield charge.
    /// Based on TypeScript Shield.ts.
    ///
    /// Effect: +1 shield (max 3, absorbs 1 hit each)
    /// Color: Cyan/Blue
    /// </summary>
    public class ShieldPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Shield;

        [Header("Shield Settings")]
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
                _visuals.SetLetter('S');
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.AddShield();
                Debug.Log("[Shield] Added shield to player");
            }
            else
            {
                Debug.LogWarning("[Shield] No PlayerHealth found on player!");
            }
        }
    }
}
