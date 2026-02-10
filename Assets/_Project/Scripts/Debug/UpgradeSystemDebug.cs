using UnityEngine;
using UnityEngine.InputSystem;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using Z13.Core;

namespace NeuralBreak.Tools
{
    /// <summary>
    /// Debug utilities for testing the upgrade system.
    /// </summary>
    public class UpgradeSystemDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PermanentUpgradeManager m_upgradeManager;
        [SerializeField] private UpgradePoolManager m_poolManager;

        [Header("Debug Options")]
        [SerializeField] private bool m_showDebugUI = true;
        [SerializeField] private Key m_triggerUpgradeScreenKey = Key.U;
        [SerializeField] private Key m_addRandomUpgradeKey = Key.I;
        [SerializeField] private Key m_clearUpgradesKey = Key.O;

        private void Start()
        {
            if (m_upgradeManager == null)
            {
                m_upgradeManager = PermanentUpgradeManager.Instance;
            }

            if (m_poolManager == null)
            {
                m_poolManager = UpgradePoolManager.Instance;
            }
        }

        private void Update()
        {
            if (!m_showDebugUI) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Trigger upgrade selection screen
            if (keyboard[m_triggerUpgradeScreenKey].wasPressedThisFrame)
            {
                TriggerUpgradeScreen();
            }

            // Add random upgrade
            if (keyboard[m_addRandomUpgradeKey].wasPressedThisFrame)
            {
                AddRandomUpgrade();
            }

            // Clear all upgrades
            if (keyboard[m_clearUpgradesKey].wasPressedThisFrame)
            {
                ClearAllUpgrades();
            }
        }

        private void OnGUI()
        {
            if (!m_showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 200, 350, 180));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== UPGRADE SYSTEM DEBUG ===", GUI.skin.box);

            if (GUILayout.Button($"[{m_triggerUpgradeScreenKey}] Show Upgrade Selection"))
            {
                TriggerUpgradeScreen();
            }

            if (GUILayout.Button($"[{m_addRandomUpgradeKey}] Add Random Upgrade"))
            {
                AddRandomUpgrade();
            }

            if (GUILayout.Button($"[{m_clearUpgradesKey}] Clear All Upgrades"))
            {
                ClearAllUpgrades();
            }

            // Show active upgrades count
            if (m_upgradeManager != null)
            {
                var activeCount = m_upgradeManager.ActiveUpgradeCount;
                GUILayout.Label($"Active Upgrades: {activeCount}");

                var modifiers = m_upgradeManager.GetCombinedModifiers();
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
            if (m_poolManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] UpgradePoolManager not found");
                return;
            }

            if (m_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            var selection = m_poolManager.GenerateSelection();
            if (selection.Count > 0)
            {
                var randomUpgrade = selection[Random.Range(0, selection.Count)];
                m_upgradeManager.AddUpgrade(randomUpgrade);
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
            if (m_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            m_upgradeManager.ClearAllUpgrades();
            LogHelper.Log("[UpgradeSystemDebug] Cleared all upgrades");
        }

        [ContextMenu("Print Active Upgrades")]
        public void PrintActiveUpgrades()
        {
            if (m_upgradeManager == null)
            {
                LogHelper.LogWarning("[UpgradeSystemDebug] PermanentUpgradeManager not found");
                return;
            }

            var upgrades = m_upgradeManager.GetActiveUpgrades();
            LogHelper.Log($"[UpgradeSystemDebug] Active Upgrades ({upgrades.Count}):");
            foreach (var upgrade in upgrades)
            {
                LogHelper.Log($"  - {upgrade.displayName} ({upgrade.tier})");
            }

            var modifiers = m_upgradeManager.GetCombinedModifiers();
            LogHelper.Log("[UpgradeSystemDebug] Combined Modifiers:");
            LogHelper.Log($"  FireRate: {modifiers.fireRateMultiplier:F2}x");
            LogHelper.Log($"  Damage: {modifiers.damageMultiplier:F2}x");
            LogHelper.Log($"  Additional Projectiles: +{modifiers.additionalProjectiles}");
            LogHelper.Log($"  Piercing: {modifiers.piercingCount}");
            LogHelper.Log($"  Homing: {modifiers.enableHoming}");
        }
    }
}
