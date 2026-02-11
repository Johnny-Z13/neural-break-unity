using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Manages permanently selected upgrades during a run.
    /// Tracks active upgrades and calculates combined modifiers.
    /// </summary>
    public class PermanentUpgradeManager : MonoBehaviour
    {
        public static PermanentUpgradeManager Instance { get; private set; }

        // Active upgrades during this run
        private List<UpgradeDefinition> m_activeUpgrades = new List<UpgradeDefinition>();
        private Dictionary<string, int> m_upgradeStacks = new Dictionary<string, int>();

        // Cached combined modifiers (recalculated when upgrades change)
        private WeaponModifiers m_combinedModifiers = WeaponModifiers.Identity;

        // Public accessors (zero allocation)
        public int ActiveUpgradeCount => m_activeUpgrades.Count;

        // Events
        public event System.Action<UpgradeDefinition> OnUpgradeAdded;
        public event System.Action<WeaponModifiers> OnModifiersChanged;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PermanentUpgradeManager] Multiple instances detected");
            }
            Instance = this;

            // Subscribe to game events
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Add a permanent upgrade (from card selection).
        /// </summary>
        public void AddUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                LogHelper.LogError("[PermanentUpgradeManager] Cannot add null upgrade");
                return;
            }

            m_activeUpgrades.Add(upgrade);

            // Update stack count
            if (m_upgradeStacks.ContainsKey(upgrade.upgradeId))
            {
                m_upgradeStacks[upgrade.upgradeId]++;
            }
            else
            {
                m_upgradeStacks[upgrade.upgradeId] = 1;
            }

            RecalculateModifiers();

            OnUpgradeAdded?.Invoke(upgrade);

            LogHelper.Log($"[PermanentUpgradeManager] Added upgrade: {upgrade.displayName} (Stack: {m_upgradeStacks[upgrade.upgradeId]})");

            // Publish event
            EventBus.Publish(new PermanentUpgradeAddedEvent
            {
                upgrade = upgrade
            });
        }

        /// <summary>
        /// Remove an upgrade by ID (used for special cases like curses).
        /// </summary>
        public void RemoveUpgrade(string upgradeId)
        {
            for (int i = m_activeUpgrades.Count - 1; i >= 0; i--)
            {
                if (m_activeUpgrades[i].upgradeId == upgradeId)
                {
                    var upgrade = m_activeUpgrades[i];
                    m_activeUpgrades.RemoveAt(i);

                    // Update stack count
                    if (m_upgradeStacks.ContainsKey(upgradeId))
                    {
                        m_upgradeStacks[upgradeId]--;
                        if (m_upgradeStacks[upgradeId] <= 0)
                        {
                            m_upgradeStacks.Remove(upgradeId);
                        }
                    }

                    RecalculateModifiers();

                    LogHelper.Log($"[PermanentUpgradeManager] Removed upgrade: {upgrade.displayName}");

                    EventBus.Publish(new PermanentUpgradeRemovedEvent
                    {
                        upgradeId = upgradeId
                    });

                    break;
                }
            }
        }

        /// <summary>
        /// Check if a specific upgrade is active.
        /// </summary>
        public bool HasUpgrade(string upgradeId)
        {
            return m_upgradeStacks.ContainsKey(upgradeId);
        }

        /// <summary>
        /// Get the number of stacks for a specific upgrade.
        /// </summary>
        public int GetUpgradeStacks(string upgradeId)
        {
            return m_upgradeStacks.ContainsKey(upgradeId) ? m_upgradeStacks[upgradeId] : 0;
        }

        /// <summary>
        /// Get the combined modifiers from all active upgrades.
        /// </summary>
        public WeaponModifiers GetCombinedModifiers()
        {
            return m_combinedModifiers;
        }

        /// <summary>
        /// Get all active upgrades (zero-allocation read-only view).
        /// WARNING: Do NOT modify the returned list. It is a direct reference to internal state.
        /// </summary>
        public IReadOnlyList<UpgradeDefinition> GetActiveUpgrades()
        {
            return m_activeUpgrades;
        }

        /// <summary>
        /// Clear all upgrades (on game over or run end).
        /// </summary>
        public void ClearAllUpgrades()
        {
            m_activeUpgrades.Clear();
            m_upgradeStacks.Clear();
            RecalculateModifiers();

            LogHelper.Log("[PermanentUpgradeManager] All upgrades cleared");
        }

        #endregion

        #region Modifier Calculation

        /// <summary>
        /// Recalculate combined modifiers from all active upgrades.
        /// </summary>
        private void RecalculateModifiers()
        {
            m_combinedModifiers = WeaponModifiers.Identity;

            foreach (var upgrade in m_activeUpgrades)
            {
                m_combinedModifiers = WeaponModifiers.Combine(m_combinedModifiers, upgrade.modifiers);
            }

            OnModifiersChanged?.Invoke(m_combinedModifiers);

            // Publish event
            EventBus.Publish(new WeaponModifiersChangedEvent
            {
                modifiers = m_combinedModifiers
            });

            LogHelper.Log($"[PermanentUpgradeManager] Modifiers recalculated: FireRate={m_combinedModifiers.fireRateMultiplier:F2}x, Damage={m_combinedModifiers.damageMultiplier:F2}x");
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Clear upgrades on new game start
            ClearAllUpgrades();
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Keep upgrades for stats display, but could clear here if desired
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Log Active Upgrades")]
        private void DebugLogActiveUpgrades()
        {
            LogHelper.Log($"[PermanentUpgradeManager] Active Upgrades: {m_activeUpgrades.Count}");
            foreach (var upgrade in m_activeUpgrades)
            {
                LogHelper.Log($"  - {upgrade.displayName} (Stacks: {m_upgradeStacks[upgrade.upgradeId]})");
            }
        }

        [ContextMenu("Debug: Log Combined Modifiers")]
        private void DebugLogModifiers()
        {
            var m = m_combinedModifiers;
            LogHelper.Log($"[PermanentUpgradeManager] Combined Modifiers:");
            LogHelper.Log($"  FireRate: {m.fireRateMultiplier:F2}x");
            LogHelper.Log($"  Damage: {m.damageMultiplier:F2}x");
            LogHelper.Log($"  Additional Projectiles: +{m.additionalProjectiles}");
            LogHelper.Log($"  Piercing: {m.piercingCount}");
            LogHelper.Log($"  Homing: {m.enableHoming}");
            LogHelper.Log($"  Rear Fire: {m.enableRearFire}");
        }

        [ContextMenu("Debug: Clear All Upgrades")]
        private void DebugClearAll()
        {
            ClearAllUpgrades();
        }

        #endregion
    }
}
