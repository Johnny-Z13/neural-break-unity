using UnityEngine;
using UnityEngine.InputSystem;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.Utils;

namespace NeuralBreak.Tools
{
    /// <summary>
    /// Debug utilities for testing the upgrade system.
    /// </summary>
    public class UpgradeSystemDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PermanentUpgradeManager _upgradeManager;
        [SerializeField] private UpgradePoolManager _poolManager;

        [Header("Debug Options")]
        [SerializeField] private bool _showDebugUI = true;
        [SerializeField] private Key _triggerUpgradeScreenKey = Key.U;
        [SerializeField] private Key _addRandomUpgradeKey = Key.I;
        [SerializeField] private Key _clearUpgradesKey = Key.O;

        private void Start()
        {
            if (_upgradeManager == null)
            {
                _upgradeManager = PermanentUpgradeManager.Instance;
            }

            if (_poolManager == null)
            {
                _poolManager = UpgradePoolManager.Instance;
            }
        }

        private void Update()
        {
            if (!_showDebugUI) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Trigger upgrade selection screen
            if (keyboard[_triggerUpgradeScreenKey].wasPressedThisFrame)
            {
                TriggerUpgradeScreen();
            }

            // Add random upgrade
            if (keyboard[_addRandomUpgradeKey].wasPressedThisFrame)
            {
                AddRandomUpgrade();
            }

            // Clear all upgrades
            if (keyboard[_clearUpgradesKey].wasPressedThisFrame)
            {
                ClearAllUpgrades();
            }
        }

        private void OnGUI()
        {
            if (!_showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 200, 350, 180));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== UPGRADE SYSTEM DEBUG ===", GUI.skin.box);

            if (GUILayout.Button($"[{_triggerUpgradeScreenKey}] Show Upgrade Selection"))
            {
                TriggerUpgradeScreen();
            }

            if (GUILayout.Button($"[{_addRandomUpgradeKey}] Add Random Upgrade"))
            {
                AddRandomUpgrade();
            }

            if (GUILayout.Button($"[{_clearUpgradesKey}] Clear All Upgrades"))
            {
                ClearAllUpgrades();
            }

            // Show active upgrades count
            if (_upgradeManager != null)
            {
                var activeCount = _upgradeManager.GetActiveUpgrades().Count;
                GUILayout.Label($"Active Upgrades: {activeCount}");

                var modifiers = _upgradeManager.GetCombinedModifiers();
                GUILayout.Label($"Fire Rate: {modifiers.fireRateMultiplier:F2}x");
                GUILayout.Label($"Damage: {modifiers.damageMultiplier:F2}x");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        [ContextMenu("Trigger Upgrade Screen")]
        public void TriggerUpgradeScreen()
        {
            if (GameManager.Instance == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] GameManager not found");
                return;
            }

            // Force RogueChoice state
            GameManager.Instance.SetState(GameStateType.RogueChoice);

            // Publish event to show screen
            EventBus.Publish(new UpgradeSelectionStartedEvent
            {
                options = new System.Collections.Generic.List<UpgradeDefinition>()
            });

            LogHelper.Log("[UpgradeSystemDebug] Triggered upgrade selection screen");
        }

        [ContextMenu("Add Random Upgrade")]
        public void AddRandomUpgrade()
        {
            if (_poolManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] UpgradePoolManager not found");
                return;
            }

            if (_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            var selection = _poolManager.GenerateSelection();
            if (selection.Count > 0)
            {
                var randomUpgrade = selection[Random.Range(0, selection.Count)];
                _upgradeManager.AddUpgrade(randomUpgrade);
                LogHelper.Log($"[UpgradeSystemDebug] Added upgrade: {randomUpgrade.displayName}");
            }
            else
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] No upgrades available");
            }
        }

        [ContextMenu("Clear All Upgrades")]
        public void ClearAllUpgrades()
        {
            if (_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            _upgradeManager.ClearAllUpgrades();
            LogHelper.Log("[UpgradeSystemDebug] Cleared all upgrades");
        }

        [ContextMenu("Print Active Upgrades")]
        public void PrintActiveUpgrades()
        {
            if (_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            var upgrades = _upgradeManager.GetActiveUpgrades();
            LogHelper.Log($"[UpgradeSystemDebug] Active Upgrades ({upgrades.Count}):");
            foreach (var upgrade in upgrades)
            {
                LogHelper.Log($"  - {upgrade.displayName} ({upgrade.tier})");
            }

            var modifiers = _upgradeManager.GetCombinedModifiers();
            LogHelper.Log("[UpgradeSystemDebug] Combined Modifiers:");
            LogHelper.Log($"  FireRate: {modifiers.fireRateMultiplier:F2}x");
            LogHelper.Log($"  Damage: {modifiers.damageMultiplier:F2}x");
            LogHelper.Log($"  Additional Projectiles: +{modifiers.additionalProjectiles}");
            LogHelper.Log($"  Piercing: {modifiers.piercingCount}");
            LogHelper.Log($"  Homing: {modifiers.enableHoming}");
        }
    }
}
