using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Piercing pickup - projectiles pass through enemies.
    /// Temporary weapon upgrade.
    ///
    /// Effect: Projectiles pierce through up to 5 enemies
    /// Duration: 8 seconds
    /// Color: Red-Orange
    /// </summary>
    public class PiercingPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.Piercing;

        [Header("Piercing Settings")]
        [SerializeField] private float m_duration = 8f;
        [SerializeField] private Color m_pickupColor = new Color(0f, 0.6f, 0.2f, 0.9f); // Forest Green #009933

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
                m_visuals.SetLetter('P'); // P for Piercing
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            var upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (upgradeManager != null)
            {
                upgradeManager.ActivateUpgrade(PickupType.Piercing, m_duration);
                Debug.Log($"[Piercing] Activated for {m_duration} seconds!");
            }
            else
            {
                Debug.LogWarning("[Piercing] WeaponUpgradeManager not found!");
            }
        }
    }
}
