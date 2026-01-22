using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// MedPack pickup - heals the player.
    /// Based on TypeScript MedPack.ts.
    ///
    /// Effect: Heals 35 HP
    /// Color: Green
    /// Special: Only spawns when player health < 80%
    /// </summary>
    public class MedPackPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.MedPack;

        [Header("MedPack Settings")]
        [SerializeField] private int _healAmount = 35;
        [SerializeField] private Color _pickupColor = new Color(0f, 1f, 0f, 0.9f); // Bright green #00FF00

        [Header("Visual")]
        [SerializeField] private MedPackVisuals _visuals;
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
                _visuals = GetComponentInChildren<MedPackVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<MedPackVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(_healAmount);
                Debug.Log($"[MedPack] Healed player for {_healAmount}");
            }
            else
            {
                Debug.LogWarning("[MedPack] No PlayerHealth found on player!");
            }
        }
    }
}
