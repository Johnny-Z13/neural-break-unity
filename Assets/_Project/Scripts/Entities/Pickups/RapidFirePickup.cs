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
        [SerializeField] private float m_duration = 6f;
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
                m_visuals.SetLetter('R');
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            var upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (upgradeManager != null)
            {
                upgradeManager.ActivateUpgrade(PickupType.RapidFire, m_duration);
                Debug.Log($"[RapidFire] Activated for {m_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[RapidFire] WeaponUpgradeManager not found!");
            }
        }
    }
}
