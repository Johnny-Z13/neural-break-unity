using UnityEngine;
using UnityEditor;
using NeuralBreak.Entities;
using NeuralBreak.Graphics;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor helper to wire up scene references automatically.
    /// Run from Tools menu to configure all managers.
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Neural Break/Setup Scene References")]
        public static void SetupSceneReferences()
        {
            SetupPickupSpawner();
            SetupEnemySpawner();
            SetupFeedbackManager();

            Debug.Log("[SceneSetup] Scene references configured!");
        }

        [MenuItem("Neural Break/Setup Pickup Spawner")]
        public static void SetupPickupSpawner()
        {
            var spawner = Object.FindFirstObjectByType<PickupSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[SceneSetup] No PickupSpawner found in scene!");
                return;
            }

            // Find player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var so = new SerializedObject(spawner);

                // Set player target
                var playerProp = so.FindProperty("m_playerTarget");
                if (playerProp != null)
                {
                    playerProp.objectReferenceValue = player.transform;
                }

                // Load and set prefabs (only 4 floating pickup types)
                SetPrefabReference(so, "m_medPackPrefab", "Assets/_Project/Prefabs/Pickups/MedPackPickup.prefab");
                SetPrefabReference(so, "m_shieldPrefab", "Assets/_Project/Prefabs/Pickups/ShieldPickup.prefab");
                SetPrefabReference(so, "m_smartBombPrefab", "Assets/_Project/Prefabs/Pickups/SmartBombPickup.prefab");
                SetPrefabReference(so, "m_invulnerablePrefab", "Assets/_Project/Prefabs/Pickups/InvulnerablePickup.prefab");

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawner);

                Debug.Log("[SceneSetup] PickupSpawner configured!");
            }
            else
            {
                Debug.LogWarning("[SceneSetup] No Player found in scene!");
            }
        }

        [MenuItem("Neural Break/Setup Enemy Spawner")]
        public static void SetupEnemySpawner()
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[SceneSetup] No EnemySpawner found in scene!");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var so = new SerializedObject(spawner);

                // Set player target
                var playerProp = so.FindProperty("m_playerTarget");
                if (playerProp != null)
                {
                    playerProp.objectReferenceValue = player.transform;
                }

                // Load and set enemy prefabs
                SetPrefabReference(so, "m_dataMitePrefab", "Assets/_Project/Prefabs/Enemies/DataMite.prefab");
                SetPrefabReference(so, "m_scanDronePrefab", "Assets/_Project/Prefabs/Enemies/ScanDrone.prefab");
                SetPrefabReference(so, "m_fizzerPrefab", "Assets/_Project/Prefabs/Enemies/Fizzer.prefab");
                SetPrefabReference(so, "m_ufoPrefab", "Assets/_Project/Prefabs/Enemies/UFO.prefab");
                SetPrefabReference(so, "m_chaosWormPrefab", "Assets/_Project/Prefabs/Enemies/ChaosWorm.prefab");
                SetPrefabReference(so, "m_voidSpherePrefab", "Assets/_Project/Prefabs/Enemies/VoidSphere.prefab");
                SetPrefabReference(so, "m_crystalShardPrefab", "Assets/_Project/Prefabs/Enemies/CrystalShard.prefab");
                SetPrefabReference(so, "m_bossPrefab", "Assets/_Project/Prefabs/Enemies/Boss.prefab");

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawner);

                Debug.Log("[SceneSetup] EnemySpawner configured!");
            }
        }

        [MenuItem("Neural Break/Setup Feedback Manager")]
        public static void SetupFeedbackManager()
        {
            var feedbackManager = Object.FindFirstObjectByType<FeedbackManager>();
            if (feedbackManager == null)
            {
                Debug.LogWarning("[SceneSetup] No FeedbackManager found in scene!");
                return;
            }

            var cameraController = Object.FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                var so = new SerializedObject(feedbackManager);
                var cameraProp = so.FindProperty("m_cameraController");
                if (cameraProp != null)
                {
                    cameraProp.objectReferenceValue = cameraController;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(feedbackManager);
                    Debug.Log("[SceneSetup] FeedbackManager configured!");
                }
            }
        }

        private static void SetPrefabReference(SerializedObject so, string propertyName, string prefabPath)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    // Get the specific component type from the prefab
                    var component = prefab.GetComponent<MonoBehaviour>();
                    if (component != null)
                    {
                        prop.objectReferenceValue = component;
                    }
                    else
                    {
                        prop.objectReferenceValue = prefab;
                    }
                }
                else
                {
                    Debug.LogWarning($"[SceneSetup] Prefab not found: {prefabPath}");
                }
            }
        }

        [MenuItem("Neural Break/Create All Managers")]
        public static void CreateAllManagers()
        {
            CreateIfMissing<PickupSpawner>("PickupSpawner");
            CreateIfMissing<VFXManager>("VFXManager");
            CreateIfMissing<FeedbackManager>("FeedbackManager");

            Debug.Log("[SceneSetup] All managers created!");
        }

        private static void CreateIfMissing<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindFirstObjectByType<T>() == null)
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                Debug.Log($"[SceneSetup] Created {name}");
            }
        }
    }
}
