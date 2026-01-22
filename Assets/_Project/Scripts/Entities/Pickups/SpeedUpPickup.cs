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
        [SerializeField] private float _speedBonus = 0.05f; // 5% per pickup
        [SerializeField] private Color _pickupColor = new Color(0f, 0.67f, 0.27f, 0.9f); // Deep Emerald #00AA44

        [Header("Visual")]
        [SerializeField] private SpeedUpVisuals _visuals;
        private bool _visualsGenerated;

        // Track current speed level for events
        private static int _currentSpeedLevel = 0;
        private const int MAX_SPEED_LEVEL = 20;

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
                _visuals = GetComponentInChildren<SpeedUpVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<SpeedUpVisuals>();
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                if (_currentSpeedLevel < MAX_SPEED_LEVEL)
                {
                    _currentSpeedLevel++;
                    controller.AddSpeedBonus(_speedBonus);

                    EventBus.Publish(new SpeedUpChangedEvent
                    {
                        newLevel = _currentSpeedLevel
                    });

                    Debug.Log($"[SpeedUp] Speed level: {_currentSpeedLevel}/{MAX_SPEED_LEVEL} (+{_speedBonus * 100}%)");
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
            _currentSpeedLevel = 0;
        }

        public static int CurrentSpeedLevel => _currentSpeedLevel;
    }
}
