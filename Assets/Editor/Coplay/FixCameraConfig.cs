using UnityEngine;
using UnityEditor;
using NeuralBreak.Config;

public class FixCameraConfig
{
    public static string Execute()
    {
        // Load the Resources config (the one actually used at runtime)
        string path = "Assets/Resources/Config/GameBalanceConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(path);
        
        if (config == null)
        {
            return $"❌ Config not found at {path}";
        }
        
        // Set camera zoom values
        config.feedback.baseZoom = 8f;
        config.feedback.minZoom = 6f;
        config.feedback.maxZoom = 22f;
        config.feedback.zoomSpeed = 3f;
        
        // Mark as dirty and save
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        return $"✅ Camera settings updated in Resources config!\n" +
               $"   Base Zoom: {config.feedback.baseZoom}\n" +
               $"   Min Zoom: {config.feedback.minZoom}\n" +
               $"   Max Zoom: {config.feedback.maxZoom}\n" +
               $"   Zoom Speed: {config.feedback.zoomSpeed}";
    }
}
