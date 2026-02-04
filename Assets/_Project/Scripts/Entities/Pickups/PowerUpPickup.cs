using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// PowerUp pickup - increases weapon power level.
    /// Based on TypeScript PowerUp.ts.
    ///
    /// Effect: +1 weapon power level (max 10)
    /// Color: Yellow/Orange
    /// </summary>
    public class PowerUpPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.PowerUp;

        [Header("PowerUp Settings")]
        [SerializeField] private int m_powerIncrease = 1;
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
                m_visuals.SetLetter('P');
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            // Find weapon system on player
            WeaponSystem weapon = player.GetComponent<WeaponSystem>();
            if (weapon == null)
            {
                weapon = player.GetComponentInChildren<WeaponSystem>();
            }

            if (weapon != null)
            {
                // Increase power level permanently
                weapon.AddPowerLevel(m_powerIncrease);

                // Log pattern progression
                int currentLevel = weapon.PowerLevel;
                var pattern = weapon.CurrentPattern;
                Debug.Log($"[PowerUp] Power level: {currentLevel} | Pattern: {pattern}");
            }
            else
            {
                Debug.LogWarning("[PowerUp] No WeaponSystem found on player!");
            }
        }
    }
}
