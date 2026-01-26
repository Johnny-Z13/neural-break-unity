using UnityEngine;
using UnityEditor;
using NeuralBreak.Input;
using UnityEngine.InputSystem;

public class ReimportGameInput
{
    public static string Execute()
    {
        string path = "Assets/_Project/Input/GameInput.inputactions";
        
        // Check if file exists
        if (!System.IO.File.Exists(path))
        {
            return $"❌ File not found at {path}";
        }
        
        // Reimport the asset
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        
        // Try to load it now
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset == null)
        {
            return $"❌ Still cannot load as InputActionAsset after reimport. File exists: {System.IO.File.Exists(path)}";
        }
        
        // Find InputManager and assign
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
        
        var serializedObject = new SerializedObject(component);
        var prop = serializedObject.FindProperty("_inputActionsAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = asset;
            serializedObject.ApplyModifiedProperties();
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            
            return $"✅ Reimported and assigned GameInput.inputactions to InputManager\n   Actions: {asset.actionMaps.Count} action maps";
        }
        
        return "❌ Could not find _inputActionsAsset property";
    }
}
