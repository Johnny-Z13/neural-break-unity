using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Utils;
using System.Collections.Generic;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Manages permanently selected upgrades during a run.
    /// Tracks active upgrades and calculates combined modifiers.
    /// Singleton pattern for global access.
    /// </summary>
    public class PermanentUpgradeManager : MonoBehaviour
    {
        public static PermanentUpgradeManager Instance { get; private set; }

        // Active upgrades during this run
        private List<UpgradeDefinition> _activeUpgrades = new List<UpgradeDefinition>();
        private Dictionary<string, int> _upgradeStacks = new Dictionary<string, int>();

        // Cached combined modifiers (recalculated when upgrades change)
        private WeaponModifiers _combinedModifiers = WeaponModifiers.Identity;

        // Events
        public event System.Action<UpgradeDefinition> OnUpgradeAdded;
        public event System.Action<WeaponModifiers> OnModifiersChanged;

        #region Lifecycle

        private void Awake()
        {
            Debug.Log($"[PermanentUpgradeManager] Awake called. Current Instance: {(Instance != null ? Instance.gameObject.name : "null")}");

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[PermanentUpgradeManager] Duplicate instance detected, destroying self");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Debug.Log($"[PermanentUpgradeManager] Singleton Instance set to: {gameObject.name}");

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

            _activeUpgrades.Add(upgrade);

            // Update stack count
            if (_upgradeStacks.ContainsKey(upgrade.upgradeId))
            {
                _upgradeStacks[upgrade.upgradeId]++;
            }
            else
            {
                _upgradeStacks[upgrade.upgradeId] = 1;
            }

            RecalculateModifiers();

            OnUpgradeAdded?.Invoke(upgrade);

            LogHelper.Log($"[PermanentUpgradeManager] Added upgrade: {upgrade.displayName} (Stack: {_upgradeStacks[upgrade.upgradeId]})");

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
            for (int i = _activeUpgrades.Count - 1; i >= 0; i--)
            {
                if (_activeUpgrades[i].upgradeId == upgradeId)
                {
                    var upgrade = _activeUpgrades[i];
                    _activeUpgrades.RemoveAt(i);

                    // Update stack count
                    if (_upgradeStacks.ContainsKey(upgradeId))
                    {
                        _upgradeStacks[upgradeId]--;
                        if (_upgradeStacks[upgradeId] <= 0)
                        {
                            _upgradeStacks.Remove(upgradeId);
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
            return _upgradeStacks.ContainsKey(upgradeId);
        }

        /// <summary>
        /// Get the number of stacks for a specific upgrade.
        /// </summary>
        public int GetUpgradeStacks(string upgradeId)
        {
            return _upgradeStacks.ContainsKey(upgradeId) ? _upgradeStacks[upgradeId] : 0;
        }

        /// <summary>
        /// Get the combined modifiers from all active upgrades.
        /// </summary>
        public WeaponModifiers GetCombinedModifiers()
        {
            return _combinedModifiers;
        }

        /// <summary>
        /// Get all active upgrades (for UI display).
        /// </summary>
        public List<UpgradeDefinition> GetActiveUpgrades()
        {
            return new List<UpgradeDefinition>(_activeUpgrades);
        }

        /// <summary>
        /// Clear all upgrades (on game over or run end).
        /// </summary>
        public void ClearAllUpgrades()
        {
            _activeUpgrades.Clear();
            _upgradeStacks.Clear();
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
            _combinedModifiers = WeaponModifiers.Identity;

            foreach (var upgrade in _activeUpgrades)
            {
                _combinedModifiers = WeaponModifiers.Combine(_combinedModifiers, upgrade.modifiers);
            }

            OnModifiersChanged?.Invoke(_combinedModifiers);

            // Publish event
            EventBus.Publish(new WeaponModifiersChangedEvent
            {
                modifiers = _combinedModifiers
            });

            LogHelper.Log($"[PermanentUpgradeManager] Modifiers recalculated: FireRate={_combinedModifiers.fireRateMultiplier:F2}x, Damage={_combinedModifiers.damageMultiplier:F2}x");
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
            LogHelper.Log($"[PermanentUpgradeManager] Active Upgrades: {_activeUpgrades.Count}");
            foreach (var upgrade in _activeUpgrades)
            {
                LogHelper.Log($"  - {upgrade.displayName} (Stacks: {_upgradeStacks[upgrade.upgradeId]})");
            }
        }

        [ContextMenu("Debug: Log Combined Modifiers")]
        private void DebugLogModifiers()
        {
            var m = _combinedModifiers;
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
