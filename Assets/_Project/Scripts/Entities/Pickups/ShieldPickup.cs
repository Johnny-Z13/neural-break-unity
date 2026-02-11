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
        [SerializeField] private Color m_pickupColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald #00AA44

        [Header("Visual")]
        [SerializeField] private PowerUpVisuals m_visuals;
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
                m_visuals = GetComponentInChildren<PowerUpVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<PowerUpVisuals>();
                m_visuals.SetLetter('S');
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
