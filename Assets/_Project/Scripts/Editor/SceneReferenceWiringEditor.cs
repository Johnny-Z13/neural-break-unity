using UnityEngine;
using UnityEditor;
using NeuralBreak.Core;

namespace NeuralBreak.Editor
{
    [CustomEditor(typeof(SceneReferenceWiring))]
    public class SceneReferenceWiringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SceneReferenceWiring wiring = (SceneReferenceWiring)target;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Click button below to automatically find all scene references", MessageType.Info);

            if (GUILayout.Button("Auto-Find All References", GUILayout.Height(30)))
            {
                Undo.RecordObject(wiring, "Auto-Find References");
                AutoFindReferences(wiring);
                EditorUtility.SetDirty(wiring);
                Debug.Log("[SceneReferenceWiringEditor] Auto-find complete!");
            }
        }

        private void AutoFindReferences(SceneReferenceWiring wiring)
        {
            // Use reflection to set private serialized fields
            var type = typeof(SceneReferenceWiring);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Required scene objects
            SetFieldIfNull(type, wiring, "_player", Object.FindFirstObjectByType<NeuralBreak.Entities.PlayerController>(), flags);
            SetFieldIfNull(type, wiring, "_cameraController", Object.FindFirstObjectByType<NeuralBreak.Graphics.CameraController>(), flags);
            SetFieldIfNull(type, wiring, "_enemySpawner", Object.FindFirstObjectByType<NeuralBreak.Entities.EnemySpawner>(), flags);
            SetFieldIfNull(type, wiring, "_enemyProjectilePool", Object.FindFirstObjectByType<NeuralBreak.Combat.EnemyProjectilePool>(), flags);
            SetFieldIfNull(type, wiring, "_gameManager", Object.FindFirstObjectByType<GameManager>(), flags);
            SetFieldIfNull(type, wiring, "_levelManager", Object.FindFirstObjectByType<LevelManager>(), flags);

            // Optional scene objects
            SetFieldIfNull(type, wiring, "_spawnWarningIndicator", Object.FindFirstObjectByType<NeuralBreak.Graphics.SpawnWarningIndicator>(), flags);
            SetFieldIfNull(type, wiring, "_lowHealthVignette", Object.FindFirstObjectByType<NeuralBreak.UI.LowHealthVignette>(), flags);
            SetFieldIfNull(type, wiring, "_highScoreManager", Object.FindFirstObjectByType<NeuralBreak.Core.HighScoreManager>(), flags);
            SetFieldIfNull(type, wiring, "_bossHealthBar", Object.FindFirstObjectByType<NeuralBreak.UI.BossHealthBar>(), flags);
            SetFieldIfNull(type, wiring, "_controlsOverlay", Object.FindFirstObjectByType<NeuralBreak.UI.ControlsOverlay>(), flags);
            SetFieldIfNull(type, wiring, "_weaponUpgradeManager", Object.FindFirstObjectByType<NeuralBreak.Combat.WeaponUpgradeManager>(), flags);
            SetFieldIfNull(type, wiring, "_activeUpgradesDisplay", Object.FindFirstObjectByType<NeuralBreak.UI.ActiveUpgradesDisplay>(), flags);
            SetFieldIfNull(type, wiring, "_playerLevelSystem", Object.FindFirstObjectByType<PlayerLevelSystem>(), flags);
            SetFieldIfNull(type, wiring, "_xpBarDisplay", Object.FindFirstObjectByType<NeuralBreak.UI.XPBarDisplay>(), flags);
            SetFieldIfNull(type, wiring, "_levelUpAnnouncement", Object.FindFirstObjectByType<NeuralBreak.UI.LevelUpAnnouncement>(), flags);
            SetFieldIfNull(type, wiring, "_damageNumberPopup", Object.FindFirstObjectByType<NeuralBreak.UI.DamageNumberPopup>(), flags);
            SetFieldIfNull(type, wiring, "_waveAnnouncement", Object.FindFirstObjectByType<NeuralBreak.UI.WaveAnnouncement>(), flags);
            SetFieldIfNull(type, wiring, "_statisticsScreen", Object.FindFirstObjectByType<NeuralBreak.UI.StatisticsScreen>(), flags);
            SetFieldIfNull(type, wiring, "_arenaManager", Object.FindFirstObjectByType<NeuralBreak.Graphics.ArenaManager>(), flags);
            SetFieldIfNull(type, wiring, "_gamepadRumble", Object.FindFirstObjectByType<NeuralBreak.Input.GamepadRumble>(), flags);
            SetFieldIfNull(type, wiring, "_minimap", Object.FindFirstObjectByType<NeuralBreak.UI.Minimap>(), flags);
            SetFieldIfNull(type, wiring, "_accessibilityManager", Object.FindFirstObjectByType<AccessibilityManager>(), flags);
            SetFieldIfNull(type, wiring, "_saveSystem", Object.FindFirstObjectByType<SaveSystem>(), flags);
            SetFieldIfNull(type, wiring, "_musicManager", Object.FindFirstObjectByType<NeuralBreak.Audio.MusicManager>(), flags);
            SetFieldIfNull(type, wiring, "_environmentParticles", Object.FindFirstObjectByType<NeuralBreak.Graphics.EnvironmentParticles>(), flags);
            SetFieldIfNull(type, wiring, "_shipCustomization", Object.FindFirstObjectByType<NeuralBreak.Entities.ShipCustomization>(), flags);
            SetFieldIfNull(type, wiring, "_enemyDeathVFX", Object.FindFirstObjectByType<NeuralBreak.Graphics.EnemyDeathVFX>(), flags);
            SetFieldIfNull(type, wiring, "_uiFeedbacks", Object.FindFirstObjectByType<NeuralBreak.UI.UIFeedbacks>(), flags);
            SetFieldIfNull(type, wiring, "_achievementSystem", Object.FindFirstObjectByType<AchievementSystem>(), flags);

            // Find prefabs - these need to be in the Project folder
            // User must manually drag from Project/Prefabs folder
            Debug.Log("[SceneReferenceWiringEditor] Scene objects found. Prefab references must be manually assigned from Project folder.");
        }

        private void SetFieldIfNull(System.Type type, object target, string fieldName, Object value, System.Reflection.BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                var currentValue = field.GetValue(target);
                if (currentValue == null || (currentValue is Object obj && obj == null))
                {
                    field.SetValue(target, value);
                    if (value != null)
                    {
                        Debug.Log($"[Auto-Find] Assigned {fieldName} = {value.name}");
                    }
                }
            }
        }
    }
}
