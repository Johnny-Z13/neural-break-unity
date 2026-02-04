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
        [SerializeField] private float m_duration = 10f;
        [SerializeField] private Color m_pickupColor = new Color(0f, 0.87f, 0.33f, 0.9f); // Jade #00DD55

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
                m_visuals.SetLetter('S'); // S for Spread
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            // Publish event for WeaponUpgradeManager to handle
            EventBus.Publish(new WeaponUpgradeActivatedEvent
            {
                upgradeType = PickupType.SpreadShot,
                duration = m_duration
            });
            Debug.Log($"[SpreadShot] Activated for {m_duration} seconds!");
        }
    }
}
