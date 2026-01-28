using UnityEngine;
using UnityEngine.InputSystem;
using NeuralBreak.Core;
using NeuralBreak.Utils;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Debug utility for testing WeaponSystem integration with EnhancedProjectile and BeamWeapon.
    /// Provides keyboard shortcuts and on-screen debug info.
    /// </summary>
    public class WeaponSystemDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponSystem _weaponSystem;
        [SerializeField] private PermanentUpgradeManager _permanentUpgrades;

        [Header("Debug UI")]
        [SerializeField] private bool _showDebugUI = true;
        [SerializeField] private int _fontSize = 16;

        private bool _showHelp = false;

        private void Awake()
        {
            if (_weaponSystem == null)
            {
                _weaponSystem = FindFirstObjectByType<WeaponSystem>();
            }

            if (_permanentUpgrades == null)
            {
                _permanentUpgrades = PermanentUpgradeManager.Instance;
            }
        }

        private void Update()
        {
            if (!_showDebugUI) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle help
            if (keyboard.hKey.wasPressedThisFrame)
            {
                _showHelp = !_showHelp;
            }

            // Quick test upgrades (numpad keys)
            if (keyboard.numpad1Key.wasPressedThisFrame)
            {
                TestHoming();
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame)
            {
                TestPiercing();
            }
            else if (keyboard.numpad3Key.wasPressedThisFrame)
            {
                TestExplosion();
            }
            else if (keyboard.numpad4Key.wasPressedThisFrame)
            {
                TestChainLightning();
            }
            else if (keyboard.numpad5Key.wasPressedThisFrame)
            {
                TestRicochet();
            }
            else if (keyboard.numpad6Key.wasPressedThisFrame)
            {
                TestBeam();
            }
            else if (keyboard.numpad7Key.wasPressedThisFrame)
            {
                TestCombo();
            }
            else if (keyboard.numpad0Key.wasPressedThisFrame)
            {
                ClearAllUpgrades();
            }
        }

        private void OnGUI()
        {
            if (!_showDebugUI) return;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
                normal = { textColor = Color.white }
            };

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize + 4,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            GUILayout.BeginArea(new Rect(10, 10, 400, 600));

            GUILayout.Label("=== WEAPON SYSTEM DEBUG ===", headerStyle);
            GUILayout.Space(10);

            // Current state
            if (_weaponSystem != null)
            {
                GUILayout.Label($"Power Level: {_weaponSystem.PowerLevel}/{_weaponSystem.MaxPowerLevel}", labelStyle);
                GUILayout.Label($"Pattern: {_weaponSystem.CurrentPattern}", labelStyle);
                GUILayout.Label($"Heat: {_weaponSystem.HeatPercent:P0}", labelStyle);
                GUILayout.Label($"Overheated: {_weaponSystem.IsOverheated}", labelStyle);
            }

            GUILayout.Space(10);

            // Active upgrades
            if (_permanentUpgrades != null)
            {
                var modifiers = _permanentUpgrades.GetCombinedModifiers();
                var activeUpgrades = _permanentUpgrades.GetActiveUpgrades();

                GUILayout.Label($"Active Upgrades: {activeUpgrades.Count}", headerStyle);

                GUILayout.Label($"Fire Rate: {modifiers.fireRateMultiplier:F2}x", labelStyle);
                GUILayout.Label($"Damage: {modifiers.damageMultiplier:F2}x", labelStyle);
                GUILayout.Label($"Projectile Speed: {modifiers.projectileSpeedMultiplier:F2}x", labelStyle);

                GUILayout.Space(5);

                GUILayout.Label($"Piercing: {modifiers.piercingCount}", labelStyle);
                GUILayout.Label($"Homing: {modifiers.enableHoming} (str: {modifiers.homingStrength:F1})", labelStyle);
                GUILayout.Label($"Explosion: {modifiers.enableExplosion} (rad: {modifiers.explosionRadius:F1})", labelStyle);
                GUILayout.Label($"Chain: {modifiers.enableChainLightning} (tgt: {modifiers.chainLightningTargets})", labelStyle);
                GUILayout.Label($"Ricochet: {modifiers.enableRicochet} (cnt: {modifiers.ricochetCount})", labelStyle);
                GUILayout.Label($"Beam: {modifiers.enableBeamWeapon} (dur: {modifiers.beamDuration:F2}s)", labelStyle);
            }

            GUILayout.Space(10);

            // Help text
            GUILayout.Label("Press [H] for controls", labelStyle);

            if (_showHelp)
            {
                GUILayout.Space(5);
                GUILayout.Label("=== CONTROLS ===", headerStyle);
                GUILayout.Label("Numpad 1: Add Homing", labelStyle);
                GUILayout.Label("Numpad 2: Add Piercing", labelStyle);
                GUILayout.Label("Numpad 3: Add Explosion", labelStyle);
                GUILayout.Label("Numpad 4: Add Chain Lightning", labelStyle);
                GUILayout.Label("Numpad 5: Add Ricochet", labelStyle);
                GUILayout.Label("Numpad 6: Add Beam Weapon", labelStyle);
                GUILayout.Label("Numpad 7: Add ALL (Combo)", labelStyle);
                GUILayout.Label("Numpad 0: Clear All", labelStyle);
            }

            GUILayout.EndArea();
        }

        #region Test Methods

        private void TestHoming()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing HOMING...");
            var upgrade = CreateTestUpgrade("test_homing", "Test Homing", "Homing projectiles");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableHoming = true;
            upgrade.modifiers.homingStrength = 5f;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestPiercing()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing PIERCING...");
            var upgrade = CreateTestUpgrade("test_piercing", "Test Piercing", "Pierce 3 enemies");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.piercingCount = 3;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestExplosion()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing EXPLOSION...");
            var upgrade = CreateTestUpgrade("test_explosion", "Test Explosion", "Explode on impact");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableExplosion = true;
            upgrade.modifiers.explosionRadius = 2f;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestChainLightning()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing CHAIN LIGHTNING...");
            var upgrade = CreateTestUpgrade("test_chain", "Test Chain Lightning", "Jump to 4 enemies");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableChainLightning = true;
            upgrade.modifiers.chainLightningTargets = 4;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestRicochet()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing RICOCHET...");
            var upgrade = CreateTestUpgrade("test_ricochet", "Test Ricochet", "Bounce 3 times");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableRicochet = true;
            upgrade.modifiers.ricochetCount = 3;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestBeam()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing BEAM WEAPON...");
            var upgrade = CreateTestUpgrade("test_beam", "Test Beam", "Continuous beam");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableBeamWeapon = true;
            upgrade.modifiers.beamDuration = 0.5f;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void TestCombo()
        {
            LogHelper.Log("[WeaponSystemDebug] Testing COMBO (All behaviors)...");
            var upgrade = CreateTestUpgrade("test_combo", "Test Combo", "ALL THE THINGS!");
            upgrade.modifiers = WeaponModifiers.Identity;
            upgrade.modifiers.enableHoming = true;
            upgrade.modifiers.homingStrength = 5f;
            upgrade.modifiers.piercingCount = 3;
            upgrade.modifiers.enableExplosion = true;
            upgrade.modifiers.explosionRadius = 2f;
            upgrade.modifiers.enableChainLightning = true;
            upgrade.modifiers.chainLightningTargets = 4;
            upgrade.modifiers.enableRicochet = true;
            upgrade.modifiers.ricochetCount = 3;
            upgrade.modifiers.fireRateMultiplier = 2f;
            upgrade.modifiers.damageMultiplier = 1.5f;
            _permanentUpgrades?.AddUpgrade(upgrade);
        }

        private void ClearAllUpgrades()
        {
            LogHelper.Log("[WeaponSystemDebug] Clearing all upgrades...");
            _permanentUpgrades?.ClearAllUpgrades();
        }

        private UpgradeDefinition CreateTestUpgrade(string id, string name, string desc)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            upgrade.upgradeId = id;
            upgrade.displayName = name;
            upgrade.description = desc;
            upgrade.category = UpgradeCategory.Special;
            upgrade.tier = UpgradeTier.Common;
            upgrade.isPermanent = true;
            upgrade.maxStacks = 1;
            return upgrade;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Debug: Test Homing")]
        private void DebugTestHoming() => TestHoming();

        [ContextMenu("Debug: Test Piercing")]
        private void DebugTestPiercing() => TestPiercing();

        [ContextMenu("Debug: Test Explosion")]
        private void DebugTestExplosion() => TestExplosion();

        [ContextMenu("Debug: Test Chain Lightning")]
        private void DebugTestChainLightning() => TestChainLightning();

        [ContextMenu("Debug: Test Ricochet")]
        private void DebugTestRicochet() => TestRicochet();

        [ContextMenu("Debug: Test Beam")]
        private void DebugTestBeam() => TestBeam();

        [ContextMenu("Debug: Test Combo")]
        private void DebugTestCombo() => TestCombo();

        [ContextMenu("Debug: Clear All")]
        private void DebugClearAll() => ClearAllUpgrades();

        #endregion
    }
}
