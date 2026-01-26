using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Manages temporary weapon upgrades (spread shot, piercing, rapid fire, homing).
    /// Tracks active upgrades and their durations.
    /// </summary>
    public class WeaponUpgradeManager : MonoBehaviour
    {

        [Header("Default Durations")]
        [SerializeField] private float _spreadShotDuration = 10f;
        [SerializeField] private float _piercingDuration = 8f;
        [SerializeField] private float _rapidFireDuration = 6f;
        [SerializeField] private float _homingDuration = 8f;

        [Header("Upgrade Parameters")]
        [SerializeField] private int _spreadShotCount = 3;
        [SerializeField] private float _spreadAngle = 15f;
        [SerializeField] private float _rapidFireMultiplier = 2f;
        [SerializeField] private float _homingStrength = 5f;
        [SerializeField] private float _homingRange = 10f;

        // Active upgrades with remaining time
        private Dictionary<PickupType, float> _activeUpgrades = new Dictionary<PickupType, float>();

        // Public accessors
        public bool HasSpreadShot => _activeUpgrades.ContainsKey(PickupType.SpreadShot);
        public bool HasPiercing => _activeUpgrades.ContainsKey(PickupType.Piercing);
        public bool HasRapidFire => _activeUpgrades.ContainsKey(PickupType.RapidFire);
        public bool HasHoming => _activeUpgrades.ContainsKey(PickupType.Homing);

        public int SpreadShotCount => _spreadShotCount;
        public float SpreadAngle => _spreadAngle;
        public float RapidFireMultiplier => _rapidFireMultiplier;
        public float HomingStrength => _homingStrength;
        public float HomingRange => _homingRange;

        public int ActiveUpgradeCount => _activeUpgrades.Count;

        private void Awake()
        {
            // No singleton pattern - just a regular component
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Start()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Update()
        {
            UpdateUpgradeDurations();
        }

        private void UpdateUpgradeDurations()
        {
            if (_activeUpgrades.Count == 0) return;

            List<PickupType> expiredUpgrades = new List<PickupType>();

            // Update all timers
            List<PickupType> keys = new List<PickupType>(_activeUpgrades.Keys);
            foreach (var key in keys)
            {
                _activeUpgrades[key] -= Time.deltaTime;
                if (_activeUpgrades[key] <= 0f)
                {
                    expiredUpgrades.Add(key);
                }
            }

            // Remove expired upgrades
            foreach (var upgrade in expiredUpgrades)
            {
                _activeUpgrades.Remove(upgrade);

                EventBus.Publish(new WeaponUpgradeExpiredEvent
                {
                    upgradeType = upgrade
                });

                Debug.Log($"[WeaponUpgradeManager] {upgrade} expired");
            }
        }

        /// <summary>
        /// Activate a weapon upgrade
        /// </summary>
        public void ActivateUpgrade(PickupType upgradeType, float duration = 0f)
        {
            // Get default duration if not specified
            if (duration <= 0f)
            {
                duration = GetDefaultDuration(upgradeType);
            }

            // If already active, extend duration
            if (_activeUpgrades.ContainsKey(upgradeType))
            {
                _activeUpgrades[upgradeType] = Mathf.Max(_activeUpgrades[upgradeType], duration);
                Debug.Log($"[WeaponUpgradeManager] {upgradeType} extended to {_activeUpgrades[upgradeType]:F1}s");
            }
            else
            {
                _activeUpgrades[upgradeType] = duration;
                Debug.Log($"[WeaponUpgradeManager] {upgradeType} activated for {duration}s");
            }

            EventBus.Publish(new WeaponUpgradeActivatedEvent
            {
                upgradeType = upgradeType,
                duration = duration
            });
        }

        /// <summary>
        /// Get remaining time for an upgrade
        /// </summary>
        public float GetRemainingTime(PickupType upgradeType)
        {
            return _activeUpgrades.TryGetValue(upgradeType, out float time) ? time : 0f;
        }

        /// <summary>
        /// Check if an upgrade is active
        /// </summary>
        public bool IsUpgradeActive(PickupType upgradeType)
        {
            return _activeUpgrades.ContainsKey(upgradeType);
        }

        /// <summary>
        /// Get all active upgrades
        /// </summary>
        public Dictionary<PickupType, float> GetActiveUpgrades()
        {
            return new Dictionary<PickupType, float>(_activeUpgrades);
        }

        private float GetDefaultDuration(PickupType type)
        {
            switch (type)
            {
                case PickupType.SpreadShot: return _spreadShotDuration;
                case PickupType.Piercing: return _piercingDuration;
                case PickupType.RapidFire: return _rapidFireDuration;
                case PickupType.Homing: return _homingDuration;
                default: return 10f;
            }
        }

        /// <summary>
        /// Clear all upgrades (on game over/restart)
        /// </summary>
        public void ClearAllUpgrades()
        {
            foreach (var upgrade in _activeUpgrades.Keys)
            {
                EventBus.Publish(new WeaponUpgradeExpiredEvent
                {
                    upgradeType = upgrade
                });
            }
            _activeUpgrades.Clear();
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            ClearAllUpgrades();
        }

        #region Debug

        [ContextMenu("Debug: Activate Spread Shot")]
        private void DebugSpreadShot() => ActivateUpgrade(PickupType.SpreadShot);

        [ContextMenu("Debug: Activate Piercing")]
        private void DebugPiercing() => ActivateUpgrade(PickupType.Piercing);

        [ContextMenu("Debug: Activate Rapid Fire")]
        private void DebugRapidFire() => ActivateUpgrade(PickupType.RapidFire);

        [ContextMenu("Debug: Activate Homing")]
        private void DebugHoming() => ActivateUpgrade(PickupType.Homing);

        [ContextMenu("Debug: Activate All")]
        private void DebugActivateAll()
        {
            ActivateUpgrade(PickupType.SpreadShot);
            ActivateUpgrade(PickupType.Piercing);
            ActivateUpgrade(PickupType.RapidFire);
            ActivateUpgrade(PickupType.Homing);
        }

        #endregion
    }
}
