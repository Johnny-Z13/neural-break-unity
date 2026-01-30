using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeuralBreak.Combat;
using NeuralBreak.UI;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Complete setup for SmartBombSystem with all required components.
    /// Assigns particle systems and audio clips.
    /// Note: MMFeedbacks removed - feedback system disabled.
    /// </summary>
    public class SetupSmartBombComplete : MonoBehaviour
    {
        [MenuItem("Neural Break/Setup/Configure Smart Bomb System")]
        public static void ConfigureSmartBombSystem()
        {
            Debug.Log("[SetupSmartBombComplete] Starting Smart Bomb System configuration...");

            // Find or create GameSystems GameObject
            GameObject gameSystems = GameObject.Find("GameSystems");
            if (gameSystems == null)
            {
                gameSystems = new GameObject("GameSystems");
                Debug.Log("[SetupSmartBombComplete] Created GameSystems GameObject");
            }

            // Find Player GameObject with SmartBombSystem
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                Debug.LogError("[SetupSmartBombComplete] Player GameObject not found!");
                return;
            }

            SmartBombSystem smartBombSystem = playerGO.GetComponent<SmartBombSystem>();
            if (smartBombSystem == null)
            {
                Debug.LogError("[SetupSmartBombComplete] SmartBombSystem component not found on Player!");
                return;
            }

            // 1. Assign particle system for explosion VFX
            GameObject explosionParticles = GameObject.Find("Player/SmartBombExplosion");
            if (explosionParticles != null)
            {
                ParticleSystem particleSystem = explosionParticles.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    SerializedObject smartBombSO = new SerializedObject(smartBombSystem);
                    smartBombSO.FindProperty("_explosionParticles").objectReferenceValue = particleSystem;
                    smartBombSO.ApplyModifiedProperties();
                    Debug.Log("[SetupSmartBombComplete] Assigned particle system to SmartBombSystem");
                }
            }
            else
            {
                Debug.LogWarning("[SetupSmartBombComplete] SmartBombExplosion particle system not found!");
            }

            // 2. Feedback assignment removed - MMFeedbacks/Feel package not installed
            Debug.Log("[SetupSmartBombComplete] Skipping feedback setup - Feel package not installed");

            // 3. Assign audio clip for epic explosion sound
            AudioClip explosionAudio = FindExplosionAudio();
            if (explosionAudio != null)
            {
                SerializedObject smartBombSO = new SerializedObject(smartBombSystem);
                smartBombSO.FindProperty("_epicExplosionSound").objectReferenceValue = explosionAudio;
                smartBombSO.ApplyModifiedProperties();
                Debug.Log("[SetupSmartBombComplete] Assigned audio clip to SmartBombSystem");
            }
            else
            {
                Debug.LogWarning("[SetupSmartBombComplete] Could not find explosion audio clip");
            }

            // 4. BombDisplay is now created automatically by HUDBuilderArcade at runtime
            // No need for manual MainCanvas setup - just verify UIManager exists
            GameObject uiManager = GameObject.Find("UIManager");
            if (uiManager != null)
            {
                Debug.Log("[SetupSmartBombComplete] UIManager found - BombDisplay will be created by HUDBuilderArcade at runtime");
            }
            else
            {
                Debug.LogWarning("[SetupSmartBombComplete] UIManager not found - ensure it exists for HUD to display bombs");
            }

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[SetupSmartBombComplete] Smart Bomb System configuration complete!");
        }

        private static AudioClip FindExplosionAudio()
        {
            // Try to find an existing explosion-like audio clip in the project
            string[] guids = AssetDatabase.FindAssets("t:AudioClip explosion");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    Debug.Log($"[SetupSmartBombComplete] Found audio clip at {path}");
                    return clip;
                }
            }

            Debug.LogWarning("[SetupSmartBombComplete] No suitable audio clip found");
            return null;
        }
    }
}
