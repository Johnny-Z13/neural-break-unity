using UnityEngine;
using UnityEditor;
using NeuralBreak.Core;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor utility to fix game mode auto-start issues
    /// </summary>
    public class GameModeAutoFix : EditorWindow
    {
        [MenuItem("Neural Break/Fix Game Mode Auto-Start")]
        public static void FixAutoStart()
        {
            Debug.Log("[GameModeAutoFix] Searching for GameManager and GameSetup...");

            var gameManager = FindFirstObjectByType<GameManager>();
            var gameSetup = FindFirstObjectByType<GameSetup>();

            if (gameManager != null)
            {
                SerializedObject so = new SerializedObject(gameManager);
                SerializedProperty autoStart = so.FindProperty("m_autoStartOnPlay");

                if (autoStart != null)
                {
                    Debug.Log($"[GameModeAutoFix] GameManager._autoStartOnPlay = {autoStart.boolValue}");
                    if (autoStart.boolValue)
                    {
                        autoStart.boolValue = false;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(gameManager);
                        Debug.Log("[GameModeAutoFix] ✓ Set GameManager._autoStartOnPlay = FALSE");
                    }
                    else
                    {
                        Debug.Log("[GameModeAutoFix] ✓ GameManager._autoStartOnPlay is already FALSE");
                    }
                }
            }
            else
            {
                Debug.LogError("[GameModeAutoFix] No GameManager found in scene!");
            }

            if (gameSetup != null)
            {
                SerializedObject so = new SerializedObject(gameSetup);
                SerializedProperty autoStart = so.FindProperty("m_autoStartGame");

                if (autoStart != null)
                {
                    Debug.Log($"[GameModeAutoFix] GameSetup._autoStartGame = {autoStart.boolValue}");
                    if (autoStart.boolValue)
                    {
                        autoStart.boolValue = false;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(gameSetup);
                        Debug.Log("[GameModeAutoFix] ✓ Set GameSetup._autoStartGame = FALSE");
                    }
                    else
                    {
                        Debug.Log("[GameModeAutoFix] ✓ GameSetup._autoStartGame is already FALSE");
                    }
                }
            }
            else
            {
                Debug.LogError("[GameModeAutoFix] No GameSetup found in scene!");
            }

            Debug.Log("[GameModeAutoFix] Done! Save the scene to persist changes.");
        }
    }
}
