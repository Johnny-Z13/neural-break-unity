using System;
using UnityEngine;
using UnityEditor;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Combat;
using NeuralBreak.Graphics;
using NeuralBreak.Audio;
using NeuralBreak.Input;
using NeuralBreak.UI;

public class WireAllSceneReferences
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();
        results.AppendLine("=== Wiring All Scene References ===\n");

        try
        {
            // Find GameSetup
            var gameSetup = UnityEngine.Object.FindFirstObjectByType<GameSetup>();
            if (gameSetup == null)
            {
                return "ERROR: GameSetup not found in scene!";
            }

            // Get SceneReferenceWiring component
            var wiring = gameSetup.GetComponent<SceneReferenceWiring>();
            if (wiring == null)
            {
                return "ERROR: SceneReferenceWiring not found on GameSetup!";
            }

            // Get PrefabSpriteSetup component
            var spriteSetup = gameSetup.GetComponent<PrefabSpriteSetup>();
            if (spriteSetup == null)
            {
                return "ERROR: PrefabSpriteSetup not found on GameSetup!";
            }

            var wiringObj = new SerializedObject(wiring);
            var spriteObj = new SerializedObject(spriteSetup);

            // === Wire Scene References ===
            results.AppendLine("--- Scene References ---");

            // Player
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                wiringObj.FindProperty("_player").objectReferenceValue = player;
                spriteObj.FindProperty("_player").objectReferenceValue = player;
                results.AppendLine($"✓ Player: {player.name}");
            }
            else
            {
                results.AppendLine("✗ Player: NOT FOUND");
            }

            // Camera
            var camera = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (camera != null)
            {
                wiringObj.FindProperty("_cameraController").objectReferenceValue = camera;
                results.AppendLine($"✓ CameraController: {camera.name}");
            }
            else
            {
                results.AppendLine("✗ CameraController: NOT FOUND");
            }

            // EnemySpawner
            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                wiringObj.FindProperty("_enemySpawner").objectReferenceValue = spawner;
                results.AppendLine($"✓ EnemySpawner: {spawner.name}");
            }
            else
            {
                results.AppendLine("✗ EnemySpawner: NOT FOUND");
            }

            // EnemyProjectilePool
            var projectilePool = UnityEngine.Object.FindFirstObjectByType<EnemyProjectilePool>();
            if (projectilePool != null)
            {
                wiringObj.FindProperty("_enemyProjectilePool").objectReferenceValue = projectilePool;
                results.AppendLine($"✓ EnemyProjectilePool: {projectilePool.name}");
            }
            else
            {
                results.AppendLine("✗ EnemyProjectilePool: NOT FOUND");
            }

            // GameManager
            var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                wiringObj.FindProperty("_gameManager").objectReferenceValue = gameManager;
                results.AppendLine($"✓ GameManager: {gameManager.name}");
            }
            else
            {
                results.AppendLine("✗ GameManager: NOT FOUND");
            }

            // LevelManager
            var levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                wiringObj.FindProperty("_levelManager").objectReferenceValue = levelManager;
                results.AppendLine($"✓ LevelManager: {levelManager.name}");
            }
            else
            {
                results.AppendLine("✗ LevelManager: NOT FOUND");
            }

            // === Wire Prefab References ===
            results.AppendLine("\n--- Prefab References ---");

            // Projectile prefab
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/_Project/Prefabs/Projectiles/Projectile.prefab");
            if (projectilePrefab != null)
            {
                wiringObj.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
                spriteObj.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
                results.AppendLine($"✓ Projectile prefab");
            }
            else
            {
                results.AppendLine("✗ Projectile prefab: NOT FOUND");
            }

            // EnemyProjectile prefab
            var enemyProjectilePrefab = AssetDatabase.LoadAssetAtPath<EnemyProjectile>("Assets/_Project/Prefabs/Projectiles/EnemyProjectile.prefab");
            if (enemyProjectilePrefab != null)
            {
                wiringObj.FindProperty("_enemyProjectilePrefab").objectReferenceValue = enemyProjectilePrefab;
                spriteObj.FindProperty("_enemyProjectilePrefab").objectReferenceValue = enemyProjectilePrefab;
                results.AppendLine($"✓ EnemyProjectile prefab");

                // Also wire directly to EnemyProjectilePool
                if (projectilePool != null)
                {
                    var poolObj = new SerializedObject(projectilePool);
                    poolObj.FindProperty("_projectilePrefab").objectReferenceValue = enemyProjectilePrefab;
                    poolObj.ApplyModifiedProperties();
                    results.AppendLine($"✓ EnemyProjectilePool._projectilePrefab wired directly");
                }
            }
            else
            {
                results.AppendLine("✗ EnemyProjectile prefab: NOT FOUND");
            }

            // Enemy prefabs
            WirePrefab<DataMite>(wiringObj, spriteObj, "_dataMitePrefab", "Assets/_Project/Prefabs/Enemies/DataMite.prefab", results);
            WirePrefab<ScanDrone>(wiringObj, spriteObj, "_scanDronePrefab", "Assets/_Project/Prefabs/Enemies/ScanDrone.prefab", results);
            WirePrefab<Fizzer>(wiringObj, spriteObj, "_fizzerPrefab", "Assets/_Project/Prefabs/Enemies/Fizzer.prefab", results);
            WirePrefab<UFO>(wiringObj, spriteObj, "_ufoPrefab", "Assets/_Project/Prefabs/Enemies/UFO.prefab", results);
            WirePrefab<ChaosWorm>(wiringObj, spriteObj, "_chaosWormPrefab", "Assets/_Project/Prefabs/Enemies/ChaosWorm.prefab", results);
            WirePrefab<VoidSphere>(wiringObj, spriteObj, "_voidSpherePrefab", "Assets/_Project/Prefabs/Enemies/VoidSphere.prefab", results);
            WirePrefab<CrystalShard>(wiringObj, spriteObj, "_crystalShardPrefab", "Assets/_Project/Prefabs/Enemies/CrystalShard.prefab", results);
            WirePrefab<Boss>(wiringObj, spriteObj, "_bossPrefab", "Assets/_Project/Prefabs/Enemies/Boss.prefab", results);

            // === Wire Optional Systems ===
            results.AppendLine("\n--- Optional Systems ---");

            WireOptionalSystem<HighScoreManager>(wiringObj, "_highScoreManager", results);
            WireOptionalSystem<MusicManager>(wiringObj, "_musicManager", results);
            WireOptionalSystem<EnvironmentParticles>(wiringObj, "_environmentParticles", results);
            WireOptionalSystem<ShipCustomization>(wiringObj, "_shipCustomization", results);
            WireOptionalSystem<EnemyDeathVFX>(wiringObj, "_enemyDeathVFX", results);
            WireOptionalSystem<UIFeedbacks>(wiringObj, "_uiFeedbacks", results);
            WireOptionalSystem<AchievementSystem>(wiringObj, "_achievementSystem", results);
            WireOptionalSystem<AccessibilityManager>(wiringObj, "_accessibilityManager", results);
            WireOptionalSystem<SaveSystem>(wiringObj, "_saveSystem", results);
            WireOptionalSystem<ArenaManager>(wiringObj, "_arenaManager", results);
            WireOptionalSystem<GamepadRumble>(wiringObj, "_gamepadRumble", results);
            WireOptionalSystem<Minimap>(wiringObj, "_minimap", results);
            WireOptionalSystem<WeaponUpgradeManager>(wiringObj, "_weaponUpgradeManager", results);
            WireOptionalSystem<PlayerLevelSystem>(wiringObj, "_playerLevelSystem", results);

            // Apply all changes
            wiringObj.ApplyModifiedProperties();
            spriteObj.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            results.AppendLine("\n=== Wiring Complete! ===");
            results.AppendLine("Scene marked dirty - remember to save!");

            return results.ToString();
        }
        catch (Exception e)
        {
            return $"ERROR: {e.Message}\n{e.StackTrace}";
        }
    }

    private static void WirePrefab<T>(SerializedObject wiringObj, SerializedObject spriteObj, string propertyName, string path, System.Text.StringBuilder results) where T : UnityEngine.Object
    {
        var prefab = AssetDatabase.LoadAssetAtPath<T>(path);
        if (prefab != null)
        {
            var wiringProp = wiringObj.FindProperty(propertyName);
            if (wiringProp != null)
            {
                wiringProp.objectReferenceValue = prefab;
            }

            var spriteProp = spriteObj.FindProperty(propertyName);
            if (spriteProp != null)
            {
                spriteProp.objectReferenceValue = prefab;
            }

            results.AppendLine($"✓ {typeof(T).Name} prefab");
        }
        else
        {
            results.AppendLine($"✗ {typeof(T).Name} prefab: NOT FOUND at {path}");
        }
    }

    private static void WireOptionalSystem<T>(SerializedObject wiringObj, string propertyName, System.Text.StringBuilder results) where T : UnityEngine.Object
    {
        var system = UnityEngine.Object.FindFirstObjectByType<T>();
        var prop = wiringObj.FindProperty(propertyName);
        if (prop != null && system != null)
        {
            prop.objectReferenceValue = system;
            results.AppendLine($"✓ {typeof(T).Name}");
        }
        else if (system == null)
        {
            results.AppendLine($"○ {typeof(T).Name}: Will be auto-created at runtime");
        }
    }
}
