using UnityEngine;
using UnityEditor;

public class AddBoundaryTag
{
    public static string Execute()
    {
        // Get the TagManager asset
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if Boundary tag already exists
        bool tagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue == "Boundary")
            {
                tagExists = true;
                break;
            }
        }

        if (!tagExists)
        {
            // Add the tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = "Boundary";
            tagManager.ApplyModifiedProperties();
            return "Added 'Boundary' tag to TagManager";
        }
        else
        {
            return "'Boundary' tag already exists";
        }
    }
}
