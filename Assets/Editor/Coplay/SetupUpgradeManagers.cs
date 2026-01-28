using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeuralBreak.Combat;

/// <summary>
/// Editor script to ensure UpgradePoolManager and PermanentUpgradeManager
/// are properly added to the scene.
/// </summary>
public class SetupUpgradeManagers
{
    [MenuItem("Neural Break/Setup/Add Upgrade Managers to Scene")]
    public static void AddUpgradeManagersToScene()
    {
        // Find or create Managers object
        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = GameObject.Find("GameSystems");
        }
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Debug.Log("[SetupUpgradeManagers] Created new Managers GameObject");
        }

        bool madeChanges = false;

        // Check for PermanentUpgradeManager
        var permanentManager = Object.FindFirstObjectByType<PermanentUpgradeManager>();
        if (permanentManager == null)
        {
            permanentManager = managers.AddComponent<PermanentUpgradeManager>();
            Debug.Log("[SetupUpgradeManagers] Added PermanentUpgradeManager to " + managers.name);
            madeChanges = true;
        }
        else
        {
            Debug.Log("[SetupUpgradeManagers] PermanentUpgradeManager already exists on: " + permanentManager.gameObject.name);
        }

        // Check for UpgradePoolManager
        var poolManager = Object.FindFirstObjectByType<UpgradePoolManager>();
        if (poolManager == null)
        {
            poolManager = managers.AddComponent<UpgradePoolManager>();
            Debug.Log("[SetupUpgradeManagers] Added UpgradePoolManager to " + managers.name);
            madeChanges = true;
        }
        else
        {
            Debug.Log("[SetupUpgradeManagers] UpgradePoolManager already exists on: " + poolManager.gameObject.name);
        }

        if (madeChanges)
        {
            // Mark scene dirty so changes are saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SetupUpgradeManagers] Scene marked as dirty - please save the scene!");
        }
        else
        {
            Debug.Log("[SetupUpgradeManagers] No changes needed - both managers already exist.");
        }
    }

    [MenuItem("Neural Break/Setup/Verify Upgrade System")]
    public static void VerifyUpgradeSystem()
    {
        Debug.Log("=== Upgrade System Verification ===");

        // Check managers
        var permanentManager = Object.FindFirstObjectByType<PermanentUpgradeManager>();
        var poolManager = Object.FindFirstObjectByType<UpgradePoolManager>();

        Debug.Log($"PermanentUpgradeManager: {(permanentManager != null ? "FOUND on " + permanentManager.gameObject.name : "MISSING")}");
        Debug.Log($"UpgradePoolManager: {(poolManager != null ? "FOUND on " + poolManager.gameObject.name : "MISSING")}");

        // Check upgrade assets
        var upgrades = Resources.LoadAll<UpgradeDefinition>("Upgrades");
        Debug.Log($"Upgrade Assets Found: {upgrades.Length}");

        int permanentCount = 0;
        foreach (var upgrade in upgrades)
        {
            if (upgrade.isPermanent)
            {
                permanentCount++;
                Debug.Log($"  - {upgrade.displayName} (Tier: {upgrade.tier}, Category: {upgrade.category})");
            }
        }
        Debug.Log($"Permanent Upgrades: {permanentCount}");

        if (permanentManager == null || poolManager == null)
        {
            Debug.LogError("MISSING MANAGERS! Run: Neural Break > Setup > Add Upgrade Managers to Scene");
        }
        else if (permanentCount == 0)
        {
            Debug.LogError("NO UPGRADES! Run: Neural Break > Create Upgrades > Create Starter Pack");
        }
        else
        {
            Debug.Log("=== Upgrade System OK ===");
        }
    }
}
