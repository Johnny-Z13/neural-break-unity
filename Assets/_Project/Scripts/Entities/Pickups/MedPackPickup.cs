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
        [SerializeField] private int m_healAmount = 35;
        [SerializeField] private Color m_pickupColor = new Color(0f, 1f, 0f, 0.9f); // Bright green #00FF00

        [Header("Visual")]
        [SerializeField] private MedPackVisuals m_visuals;
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
                m_visuals = GetComponentInChildren<MedPackVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<MedPackVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(m_healAmount);
                Debug.Log($"[MedPack] Healed player for {m_healAmount}");
            }
            else
            {
                Debug.LogWarning("[MedPack] No PlayerHealth found on player!");
            }
        }
    }
}
