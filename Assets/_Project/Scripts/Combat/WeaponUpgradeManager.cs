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
        [SerializeField] private float m_spreadShotDuration = 10f;
        [SerializeField] private float m_piercingDuration = 8f;
        [SerializeField] private float m_rapidFireDuration = 6f;
        [SerializeField] private float m_homingDuration = 8f;

        [Header("Upgrade Parameters")]
        [SerializeField] private int m_spreadShotCount = 3;
        [SerializeField] private float m_spreadAngle = 15f;
        [SerializeField] private float m_rapidFireMultiplier = 2f;
        [SerializeField] private float m_homingStrength = 5f;
        [SerializeField] private float m_homingRange = 10f;

        // Active upgrades with remaining time
        private Dictionary<PickupType, float> m_activeUpgrades = new Dictionary<PickupType, float>();

        // Public accessors
        public bool HasSpreadShot => m_activeUpgrades.ContainsKey(PickupType.SpreadShot);
        public bool HasPiercing => m_activeUpgrades.ContainsKey(PickupType.Piercing);
        public bool HasRapidFire => m_activeUpgrades.ContainsKey(PickupType.RapidFire);
        public bool HasHoming => m_activeUpgrades.ContainsKey(PickupType.Homing);

        public int SpreadShotCount => m_spreadShotCount;
        public float SpreadAngle => m_spreadAngle;
        public float RapidFireMultiplier => m_rapidFireMultiplier;
        public float HomingStrength => m_homingStrength;
        public float HomingRange => m_homingRange;

        public int ActiveUpgradeCount => m_activeUpgrades.Count;

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
            if (m_activeUpgrades.Count == 0) return;

            List<PickupType> expiredUpgrades = new List<PickupType>();

            // Update all timers
            List<PickupType> keys = new List<PickupType>(m_activeUpgrades.Keys);
            foreach (var key in keys)
            {
                m_activeUpgrades[key] -= Time.deltaTime;
                if (m_activeUpgrades[key] <= 0f)
                {
                    expiredUpgrades.Add(key);
                }
            }

            // Remove expired upgrades
            foreach (var upgrade in expiredUpgrades)
            {
                m_activeUpgrades.Remove(upgrade);

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
            if (m_activeUpgrades.ContainsKey(upgradeType))
            {
                m_activeUpgrades[upgradeType] = Mathf.Max(m_activeUpgrades[upgradeType], duration);
                Debug.Log($"[WeaponUpgradeManager] {upgradeType} extended to {m_activeUpgrades[upgradeType]:F1}s");
            }
            else
            {
                m_activeUpgrades[upgradeType] = duration;
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
            return m_activeUpgrades.TryGetValue(upgradeType, out float time) ? time : 0f;
        }

        /// <summary>
        /// Check if an upgrade is active
        /// </summary>
        public bool IsUpgradeActive(PickupType upgradeType)
        {
            return m_activeUpgrades.ContainsKey(upgradeType);
        }

        /// <summary>
        /// Get all active upgrades
        /// </summary>
        public Dictionary<PickupType, float> GetActiveUpgrades()
        {
            return new Dictionary<PickupType, float>(m_activeUpgrades);
        }

        private float GetDefaultDuration(PickupType type)
        {
            switch (type)
            {
                case PickupType.SpreadShot: return m_spreadShotDuration;
                case PickupType.Piercing: return m_piercingDuration;
                case PickupType.RapidFire: return m_rapidFireDuration;
                case PickupType.Homing: return m_homingDuration;
                default: return 10f;
            }
        }

        /// <summary>
        /// Clear all upgrades (on game over/restart)
        /// </summary>
        public void ClearAllUpgrades()
        {
            foreach (var upgrade in m_activeUpgrades.Keys)
            {
                EventBus.Publish(new WeaponUpgradeExpiredEvent
                {
                    upgradeType = upgrade
                });
            }
            m_activeUpgrades.Clear();
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
