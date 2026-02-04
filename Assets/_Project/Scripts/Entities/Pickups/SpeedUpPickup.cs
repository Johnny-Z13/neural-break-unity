using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// SpeedUp pickup - increases player movement speed.
    /// Based on TypeScript SpeedUp.ts.
    ///
    /// Effect: +5% movement speed per level (max 20 levels = +100%)
    /// Color: Magenta/Pink
    /// </summary>
    public class SpeedUpPickup : PickupBase
    {
        public override PickupType PickupType => PickupType.SpeedUp;

        [Header("SpeedUp Settings")]
        [SerializeField] private float m_speedBonus = 0.05f; // 5% per pickup
        [SerializeField] private Color m_pickupColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald #00AA44

        [Header("Visual")]
        [SerializeField] private SpeedUpVisuals m_visuals;
        private bool m_visualsGenerated;

        // Track current speed level for events
        private static int s_currentSpeedLevel = 0;
        private const int MAX_SPEED_LEVEL = 20;

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
                m_visuals = GetComponentInChildren<SpeedUpVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<SpeedUpVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                if (s_currentSpeedLevel < MAX_SPEED_LEVEL)
                {
                    s_currentSpeedLevel++;
                    controller.AddSpeedBonus(m_speedBonus);

                    EventBus.Publish(new SpeedUpChangedEvent
                    {
                        newLevel = s_currentSpeedLevel
                    });

                    Debug.Log($"[SpeedUp] Speed level: {s_currentSpeedLevel}/{MAX_SPEED_LEVEL} (+{m_speedBonus * 100}%)");
                }
                else
                {
                    Debug.Log("[SpeedUp] Already at max speed level!");
                }
            }
            else
            {
                Debug.LogWarning("[SpeedUp] No PlayerController found on player!");
            }
        }

        /// <summary>
        /// Reset speed level for new game
        /// </summary>
        public static void ResetSpeedLevel()
        {
            s_currentSpeedLevel = 0;
        }

        public static int CurrentSpeedLevel => s_currentSpeedLevel;
    }
}
