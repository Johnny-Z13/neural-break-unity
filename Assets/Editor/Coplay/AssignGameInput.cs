using UnityEngine;
using UnityEditor;
using NeuralBreak.Input;
using UnityEngine.InputSystem;

public class AssignGameInput
{
    public static string Execute()
    {
        // Find InputManager in scene
        var inputManager = GameObject.Find("InputManager");
        if (inputManager == null)
        {
            return "❌ InputManager not found in scene!";
        }
        
        var component = inputManager.GetComponent<InputManager>();
        if (component == null)
        {
            return "❌ InputManager component not found!";
        }
        
        // Find all InputActionAssets
        var guids = AssetDatabase.FindAssets("t:InputActionAsset");
        InputActionAsset gameInput = null;
        
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("GameInput"))
            {
                gameInput = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (gameInput != null)
                {
                    // Assign via SerializedObject
                    var serializedObject = new SerializedObject(component);
                    var prop = serializedObject.FindProperty("_inputActionsAsset");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = gameInput;
                        serializedObject.ApplyModifiedProperties();
                        
                        // Mark scene dirty
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                        );
                        
                        return $"✅ Assigned GameInput from {path} to InputManager";
                    }
                }
            }
        }
        
        // List what we found
        var foundPaths = "";
        foreach (var guid in guids)
        {
            foundPaths += AssetDatabase.GUIDToAssetPath(guid) + "\n";
        }
        return $"❌ GameInput not found!\nFound assets:\n{foundPaths}";
    }
}
