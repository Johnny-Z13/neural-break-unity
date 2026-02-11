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
        [SerializeField] private float m_invulnerabilityDuration = 7f;
        [SerializeField] private Color m_pickupColor = new Color(0f, 1f, 0f, 0.9f); // Bright green #00FF00

        [Header("Visual")]
        [SerializeField] private InvulnerableVisuals m_visuals;
        private bool m_visualsGenerated;

        protected override Color GetPickupColor() => m_pickupColor;

        public override void Initialize(Vector2 position, Transform playerTarget, System.Action<PickupBase> returnCallback)
        {
            base.Initialize(position, playerTarget, returnCallback);

            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            if (m_visuals == null)
            {
                m_visuals = GetComponentInChildren<InvulnerableVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<InvulnerableVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ActivateInvulnerability(m_invulnerabilityDuration);
                Debug.Log($"[Invulnerable] Activated for {m_invulnerabilityDuration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Invulnerable] No PlayerHealth found on player!");
            }
        }
    }
}
