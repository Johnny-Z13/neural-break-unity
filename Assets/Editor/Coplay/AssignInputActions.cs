using UnityEngine;
using UnityEditor;
using NeuralBreak.Input;
using UnityEngine.InputSystem;

public class AssignInputActions
{
    public static string Execute()
    {
        // Refresh asset database first
        AssetDatabase.Refresh();
        
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
        
        // Try multiple paths
        string[] paths = new string[]
        {
            "Assets/_Project/Input/GameInput.inputactions",
            "Assets/InputSystem_Actions.inputactions"
        };
        
        InputActionAsset inputAsset = null;
        string usedPath = "";
        
        foreach (var path in paths)
        {
            inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (inputAsset != null)
            {
                usedPath = path;
                break;
            }
        }
        
        if (inputAsset == null)
        {
            // List what we can find
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            var foundPaths = "";
            foreach (var guid in guids)
            {
                foundPaths += AssetDatabase.GUIDToAssetPath(guid) + "\n";
            }
            return $"❌ No InputActionAsset found!\nSearched: {string.Join(", ", paths)}\nFound assets:\n{foundPaths}";
        }
        
        // Assign via SerializedObject
        var serializedObject = new SerializedObject(component);
        var prop = serializedObject.FindProperty("_inputActionsAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = inputAsset;
            serializedObject.ApplyModifiedProperties();
            
            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            
            return $"✅ Assigned {usedPath} to InputManager";
        }
        
        return "❌ Could not find _inputActionsAsset property";
    }
}
