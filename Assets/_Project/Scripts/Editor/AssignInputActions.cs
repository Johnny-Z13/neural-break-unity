using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace NeuralBreak.Editor
{
    public static class AssignInputActions
    {
        [MenuItem("Tools/Neural Break/Assign Input Actions Asset")]
        public static void AssignInputActionsAsset()
        {
            // Find the InputManager in the scene
            var inputManager = Object.FindFirstObjectByType<NeuralBreak.Input.InputManager>();
            if (inputManager == null)
            {
                Debug.LogError("InputManager not found in scene!");
                return;
            }

            // Load the GameInput asset
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/GameInput.inputactions");
            if (inputAsset == null)
            {
                Debug.LogError("GameInput.inputactions not found!");
                return;
            }

            // Use reflection to set the private field
            var type = typeof(NeuralBreak.Input.InputManager);
            var field = type.GetField("_inputActionsAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Undo.RecordObject(inputManager, "Assign Input Actions");
                field.SetValue(inputManager, inputAsset);
                EditorUtility.SetDirty(inputManager);
                Debug.Log("Successfully assigned GameInput.inputactions to InputManager!");
            }
            else
            {
                Debug.LogError("Could not find _inputActionsAsset field!");
            }
        }
    }
}
