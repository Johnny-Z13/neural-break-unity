using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Invulnerable pickup - grants temporary god mode.
    /// Based on TypeScript Invulnerable.ts.
    ///
    /// Effect: 7 seconds of invulnerability
    /// Color: Gold/White (rainbow pulse)
    /// Rarity: Very rare (1 per level, long spawn interval)
    /// </summary>
    public class InvulnerablePickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Invulnerable;

        [Header("Invulnerable Settings")]
        [SerializeField] private float _invulnerabilityDuration = 7f;
        [SerializeField] private Color _pickupColor = new Color(0f, 1f, 0f, 0.9f); // Bright green #00FF00

        [Header("Visual")]
        [SerializeField] private InvulnerableVisuals _visuals;
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
                _visuals = GetComponentInChildren<InvulnerableVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<InvulnerableVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ActivateInvulnerability(_invulnerabilityDuration);
                Debug.Log($"[Invulnerable] Activated for {_invulnerabilityDuration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Invulnerable] No PlayerHealth found on player!");
            }
        }
    }
}
