using System;
using UnityEngine;
using UnityEditor;
using NeuralBreak.Entities;
using NeuralBreak.Combat;

public class WireWeaponSystem
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();
        results.AppendLine("=== Wiring WeaponSystem ===\n");

        try
        {
            // Find Player
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return "ERROR: Player not found in scene!";
            }

            // Get WeaponSystem component
            var weapon = player.GetComponent<WeaponSystem>();
            if (weapon == null)
            {
                return "ERROR: WeaponSystem not found on Player!";
            }

            var weaponObj = new SerializedObject(weapon);

            // Wire projectile prefab
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/_Project/Prefabs/Projectiles/Projectile.prefab");
            if (projectilePrefab != null)
            {
                weaponObj.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
                results.AppendLine($"✓ Projectile prefab wired");
            }
            else
            {
                results.AppendLine("✗ Projectile prefab: NOT FOUND");
            }

            // Wire player reference
            weaponObj.FindProperty("_player").objectReferenceValue = player;
            results.AppendLine($"✓ Player reference wired");

            // Apply changes
            weaponObj.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            results.AppendLine("\n=== WeaponSystem Wiring Complete! ===");

            return results.ToString();
        }
        catch (Exception e)
        {
            return $"ERROR: {e.Message}\n{e.StackTrace}";
        }
    }
}
