using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveCurrentScene
{
    public static string Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        return $"Saved scene: {scene.path}";
    }
}
