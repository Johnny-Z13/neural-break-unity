using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeuralBreak.Combat;
using NeuralBreak.UI;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Complete setup for SmartBombSystem with all required components and feedbacks.
    /// Assigns particle systems, MMF_Player feedbacks, and audio clips.
    /// </summary>
    public class SetupSmartBombComplete : MonoBehaviour
    {
        [MenuItem("NeuralBreak/Setup/Configure Smart Bomb System")]
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

            // 2. Create and assign activation feedback (MMF_Player)
            MMF_Player activationFeedback = CreateOrGetFeedback(playerGO, "SmartBombActivationFeedback");
            if (activationFeedback != null)
            {
                SerializedObject smartBombSO = new SerializedObject(smartBombSystem);
                smartBombSO.FindProperty("_activationFeedback").objectReferenceValue = activationFeedback;
                smartBombSO.ApplyModifiedProperties();
                Debug.Log("[SetupSmartBombComplete] Created and assigned activation feedback");
            }

            // 3. Create and assign camera shake feedback (MMF_Player)
            MMF_Player cameraShakeFeedback = CreateOrGetFeedback(playerGO, "SmartBombCameraShakeFeedback");
            if (cameraShakeFeedback != null)
            {
                SerializedObject smartBombSO = new SerializedObject(smartBombSystem);
                smartBombSO.FindProperty("_cameraShakeFeedback").objectReferenceValue = cameraShakeFeedback;
                smartBombSO.ApplyModifiedProperties();
                Debug.Log("[SetupSmartBombComplete] Created and assigned camera shake feedback");
            }

            // 4. Assign audio clip for epic explosion sound
            AudioClip explosionAudio = FindOrCreateExplosionAudio();
            if (explosionAudio != null)
            {
                SerializedObject smartBombSO = new SerializedObject(smartBombSystem);
                smartBombSO.FindProperty("_epicExplosionSound").objectReferenceValue = explosionAudio;
                smartBombSO.ApplyModifiedProperties();
                Debug.Log("[SetupSmartBombComplete] Assigned audio clip to SmartBombSystem");
            }
            else
            {
                Debug.LogWarning("[SetupSmartBombComplete] Could not find or create explosion audio clip");
            }

            // 5. Verify SmartBombDisplay on MainCanvas
            GameObject mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas != null)
            {
                Transform smartBombDisplayTransform = mainCanvas.transform.Find("SmartBombDisplay");
                if (smartBombDisplayTransform != null)
                {
                    SmartBombDisplay smartBombDisplay = smartBombDisplayTransform.GetComponent<SmartBombDisplay>();
                    if (smartBombDisplay != null)
                    {
                        Debug.Log("[SetupSmartBombComplete] SmartBombDisplay component verified on MainCanvas");
                    }
                    else
                    {
                        Debug.LogWarning("[SetupSmartBombComplete] SmartBombDisplay component not found on MainCanvas/SmartBombDisplay");
                    }
                }
                else
                {
                    Debug.LogWarning("[SetupSmartBombComplete] SmartBombDisplay GameObject not found under MainCanvas");
                }
            }
            else
            {
                Debug.LogError("[SetupSmartBombComplete] MainCanvas not found!");
            }

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[SetupSmartBombComplete] Smart Bomb System configuration complete!");
        }

        private static MMF_Player CreateOrGetFeedback(GameObject parent, string feedbackName)
        {
            Transform existingFeedback = parent.transform.Find(feedbackName);
            if (existingFeedback != null && existingFeedback.GetComponent<MMF_Player>() != null)
            {
                MMF_Player existingPlayer = existingFeedback.GetComponent<MMF_Player>();
                if (existingPlayer != null)
                {
                    return existingPlayer;
                }
            }

            // Create new feedback GameObject
            GameObject feedbackGO = new GameObject(feedbackName);
            feedbackGO.transform.SetParent(parent.transform);
            feedbackGO.transform.localPosition = Vector3.zero;

            MMF_Player player = feedbackGO.AddComponent<MMF_Player>();
            player.InitializationMode = MMF_Player.InitializationModes.Script;

            return player;
        }

        private static AudioClip FindOrCreateExplosionAudio()
        {
            // Try to find an existing explosion-like audio clip
            string[] audioSearchPaths = new string[]
            {
                "Assets/Feel/FeelDemos/Barbarians/Sounds/FeelBarbarianThunder.wav",
                "Assets/Feel/FeelDemosHDRP/Falcon/Sounds/FeelFalconEngineStopSound.wav",
                "Assets/Feel/FeelDemos/Wheel/Sounds/FeelWheelMusic.wav"
            };

            foreach (string path in audioSearchPaths)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    Debug.Log($"[SetupSmartBombComplete] Found audio clip at {path}");
                    return clip;
                }
            }

            Debug.LogWarning("[SetupSmartBombComplete] No suitable audio clip found in Feel demos");
            return null;
        }
    }
}
