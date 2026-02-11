using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor utility to create the Boot scene with all true singletons.
    /// Run via menu: Neural Break > Create Boot Scene
    /// </summary>
    public static class BootSceneCreator
    {
        private const string BOOT_SCENE_PATH = "Assets/Scenes/Boot.unity";
        private const string MAIN_SCENE_NAME = "main-neural-break";

        [MenuItem("Neural Break/Create Boot Scene")]
        public static void CreateBootScene()
        {
            // Confirm with user
            if (!EditorUtility.DisplayDialog(
                "Create Boot Scene",
                "This will create a new Boot scene with all global singletons.\n\n" +
                "The scene will be saved to:\n" + BOOT_SCENE_PATH + "\n\n" +
                "Continue?",
                "Create", "Cancel"))
            {
                return;
            }

            // Create new scene
            Scene bootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create BootManager GameObject
            GameObject bootManagerGO = new GameObject("BootManager");
            var bootManager = bootManagerGO.AddComponent<Core.BootManager>();

            // Create all singleton GameObjects
            GameObject inputManagerGO = CreateSingletonObject<Input.InputManager>("InputManager");
            GameObject saveSystemGO = CreateSingletonObject<Core.SaveSystem>("SaveSystem");
            GameObject accessibilityGO = CreateSingletonObject<Core.AccessibilityManager>("AccessibilityManager");
            GameObject audioManagerGO = CreateSingletonObject<Audio.AudioManager>("AudioManager");
            GameObject musicManagerGO = CreateSingletonObject<Audio.MusicManager>("MusicManager");
            GameObject highScoreGO = CreateSingletonObject<Core.HighScoreManager>("HighScoreManager");
            GameObject gameStateGO = CreateSingletonObject<Core.GameStateManager>("GameStateManager");

            // Set up the boot components array via SerializedObject
            SerializedObject so = new SerializedObject(bootManager);
            SerializedProperty bootComponents = so.FindProperty("m_bootComponents");

            // Set array size and populate in initialization order
            bootComponents.arraySize = 7;
            bootComponents.GetArrayElementAtIndex(0).objectReferenceValue = inputManagerGO.GetComponent<Input.InputManager>();
            bootComponents.GetArrayElementAtIndex(1).objectReferenceValue = saveSystemGO.GetComponent<Core.SaveSystem>();
            bootComponents.GetArrayElementAtIndex(2).objectReferenceValue = accessibilityGO.GetComponent<Core.AccessibilityManager>();
            bootComponents.GetArrayElementAtIndex(3).objectReferenceValue = audioManagerGO.GetComponent<Audio.AudioManager>();
            bootComponents.GetArrayElementAtIndex(4).objectReferenceValue = musicManagerGO.GetComponent<Audio.MusicManager>();
            bootComponents.GetArrayElementAtIndex(5).objectReferenceValue = highScoreGO.GetComponent<Core.HighScoreManager>();
            bootComponents.GetArrayElementAtIndex(6).objectReferenceValue = gameStateGO.GetComponent<Core.GameStateManager>();

            // Set main scene name
            SerializedProperty mainSceneName = so.FindProperty("m_mainSceneName");
            mainSceneName.stringValue = MAIN_SCENE_NAME;

            so.ApplyModifiedProperties();

            // Ensure directory exists
            string directory = Path.GetDirectoryName(BOOT_SCENE_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save the scene
            EditorSceneManager.SaveScene(bootScene, BOOT_SCENE_PATH);

            // Add to build settings
            AddSceneToBuildSettings(BOOT_SCENE_PATH, 0);

            Debug.Log($"[BootSceneCreator] Boot scene created at {BOOT_SCENE_PATH}");
            Debug.Log("[BootSceneCreator] Boot scene added to Build Settings at index 0");

            EditorUtility.DisplayDialog(
                "Boot Scene Created",
                "Boot scene has been created and added to Build Settings.\n\n" +
                "IMPORTANT: You need to configure the singletons:\n" +
                "1. Open the Boot scene\n" +
                "2. Configure InputManager with your InputActionAsset\n" +
                "3. Configure AudioManager with audio clips\n" +
                "4. Configure other singletons as needed\n\n" +
                "The game should now be started from the Boot scene.",
                "OK");
        }

        private static GameObject CreateSingletonObject<T>(string name) where T : MonoBehaviour
        {
            GameObject go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        private static void AddSceneToBuildSettings(string scenePath, int targetIndex)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Check if already in build settings
            int existingIndex = scenes.FindIndex(s => s.path == scenePath);
            if (existingIndex >= 0)
            {
                // Remove from current position
                scenes.RemoveAt(existingIndex);
            }

            // Insert at target index
            scenes.Insert(targetIndex, new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Neural Break/Open Boot Scene")]
        public static void OpenBootScene()
        {
            if (File.Exists(BOOT_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(BOOT_SCENE_PATH);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Boot Scene Not Found",
                    "Boot scene does not exist. Use 'Neural Break > Create Boot Scene' first.",
                    "OK");
            }
        }
    }
}
