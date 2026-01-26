using UnityEngine;
using UnityEditor;
using NeuralBreak.Config;

/// <summary>
/// Ensures the GameBalanceConfig is in the Resources folder for runtime loading
/// </summary>
public class SetupConfigResources
{
    public static string Execute()
    {
        // Ensure Resources/Config folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Config"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Config");
        }
        
        string sourcePath = "Assets/_Project/Config/GameBalanceConfig.asset";
        string destPath = "Assets/Resources/Config/GameBalanceConfig.asset";
        
        // Check if source exists
        var sourceConfig = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(sourcePath);
        if (sourceConfig == null)
        {
            return $"❌ Source config not found at {sourcePath}";
        }
        
        // Check if destination already exists
        var destConfig = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(destPath);
        if (destConfig != null)
        {
            // Delete existing to replace
            AssetDatabase.DeleteAsset(destPath);
        }
        
        // Copy the asset
        bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
        
        if (success)
        {
            AssetDatabase.Refresh();
            return $"✅ GameBalanceConfig copied to Resources folder!\n   Source: {sourcePath}\n   Destination: {destPath}";
        }
        else
        {
            return $"❌ Failed to copy config to {destPath}";
        }
    }
}
