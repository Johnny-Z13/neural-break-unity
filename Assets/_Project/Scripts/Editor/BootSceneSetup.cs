#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeuralBreak.Core;
using NeuralBreak.Input;
using NeuralBreak.Audio;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor utility to create and configure the Boot scene.
    /// Run from menu: Neural Break > Create Boot Scene
    /// </summary>
    public static class BootSceneSetup
    {
        private const string BOOT_SCENE_PATH = "Assets/Scenes/Boot.unity";
        private const string MAIN_SCENE_NAME = "main-neural-break";

        [MenuItem("Neural Break/Create Boot Scene")]
        public static void CreateBootScene()
        {
            // Confirm with user
            if (!EditorUtility.DisplayDialog("Create Boot Scene",
                "This will create a new Boot scene with all singleton managers.\n\n" +
                "The scene will be saved to:\n" + BOOT_SCENE_PATH + "\n\n" +
                "Continue?", "Create", "Cancel"))
            {
                return;
            }

            // Create new scene
            var bootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create BootManager GameObject
            var bootManagerGO = new GameObject("BootManager");
            var bootManager = bootManagerGO.AddComponent<BootManager>();

            // Create singleton GameObjects
            var inputManagerGO = CreateSingletonGO<InputManager>("InputManager");
            var saveSystemGO = CreateSingletonGO<SaveSystem>("SaveSystem");
            var accessibilityGO = CreateSingletonGO<AccessibilityManager>("AccessibilityManager");
            var audioManagerGO = CreateSingletonGO<AudioManager>("AudioManager");
            var musicManagerGO = CreateSingletonGO<MusicManager>("MusicManager");
            var highScoreGO = CreateSingletonGO<HighScoreManager>("HighScoreManager");
            var gameStateGO = CreateSingletonGO<GameStateManager>("GameStateManager");

            // Configure BootManager's boot components list via SerializedObject
            var so = new SerializedObject(bootManager);
            var bootComponentsProp = so.FindProperty("m_bootComponents");
            bootComponentsProp.arraySize = 7;

            // Order matters for initialization!
            bootComponentsProp.GetArrayElementAtIndex(0).objectReferenceValue = inputManagerGO.GetComponent<InputManager>();
            bootComponentsProp.GetArrayElementAtIndex(1).objectReferenceValue = saveSystemGO.GetComponent<SaveSystem>();
            bootComponentsProp.GetArrayElementAtIndex(2).objectReferenceValue = accessibilityGO.GetComponent<AccessibilityManager>();
            bootComponentsProp.GetArrayElementAtIndex(3).objectReferenceValue = audioManagerGO.GetComponent<AudioManager>();
            bootComponentsProp.GetArrayElementAtIndex(4).objectReferenceValue = musicManagerGO.GetComponent<MusicManager>();
            bootComponentsProp.GetArrayElementAtIndex(5).objectReferenceValue = highScoreGO.GetComponent<HighScoreManager>();
            bootComponentsProp.GetArrayElementAtIndex(6).objectReferenceValue = gameStateGO.GetComponent<GameStateManager>();

            var mainSceneProp = so.FindProperty("m_mainSceneName");
            mainSceneProp.stringValue = MAIN_SCENE_NAME;

            so.ApplyModifiedProperties();

            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            // Save the scene
            EditorSceneManager.SaveScene(bootScene, BOOT_SCENE_PATH);

            // Add to build settings if not already there
            AddSceneToBuildSettings(BOOT_SCENE_PATH, 0);

            Debug.Log($"[BootSceneSetup] Boot scene created at {BOOT_SCENE_PATH}");
            Debug.Log("[BootSceneSetup] Remember to set Boot scene as index 0 in Build Settings!");

            EditorUtility.DisplayDialog("Boot Scene Created",
                "Boot scene has been created successfully!\n\n" +
                "Next steps:\n" +
                "1. Open Build Settings (File > Build Settings)\n" +
                "2. Ensure Boot scene is at index 0\n" +
                "3. Ensure main-neural-break scene is at index 1\n" +
                "4. Remove singleton managers from the main scene",
                "OK");
        }

        private static GameObject CreateSingletonGO<T>(string name) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        private static void AddSceneToBuildSettings(string scenePath, int targetIndex)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Check if scene already exists
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    // Move to target index if not already there
                    if (i != targetIndex)
                    {
                        var scene = scenes[i];
                        scenes.RemoveAt(i);
                        scenes.Insert(targetIndex, scene);
                    }
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Add at target index
                var newScene = new EditorBuildSettingsScene(scenePath, true);
                scenes.Insert(targetIndex, newScene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Neural Break/Validate Boot Scene Setup")]
        public static void ValidateBootSceneSetup()
        {
            var scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Validation Failed",
                    "No scenes in Build Settings!\n\n" +
                    "Add Boot scene and main scene to Build Settings.",
                    "OK");
                return;
            }

            string firstScene = scenes[0].path;
            bool bootFirst = firstScene.Contains("Boot");

            if (!bootFirst)
            {
                EditorUtility.DisplayDialog("Validation Warning",
                    $"Boot scene should be at index 0!\n\n" +
                    $"Current first scene: {firstScene}\n\n" +
                    "Reorder scenes in Build Settings.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog("Validation Passed",
                "Boot scene setup looks correct!\n\n" +
                $"Scene 0: {scenes[0].path}\n" +
                (scenes.Length > 1 ? $"Scene 1: {scenes[1].path}" : ""),
                "OK");
        }
    }
}
#endif
