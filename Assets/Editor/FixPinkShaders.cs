using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class FixPinkShaders : EditorWindow
{
    [MenuItem("Tools/Fix Pink Textures (Add Shaders)")]
    public static void FixShaders()
    {
        string[] shaders = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Terrain/Lit",
            "Hidden/Universal Render Pipeline/ScreenSpaceShadows",
            "Hidden/Universal Render Pipeline/ScreenSpaceJoints"
        };

        // Load as generic Object to avoid strict type dependency issues
        Object graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        
        if (graphicsSettingsObj == null)
        {
            Debug.LogError("Could not load ProjectSettings/GraphicsSettings.asset");
            return;
        }

        SerializedObject so = new SerializedObject(graphicsSettingsObj);
        SerializedProperty it = so.FindProperty("m_AlwaysIncludedShaders");

        if (it == null)
        {
             Debug.LogError("Could not find 'm_AlwaysIncludedShaders' property.");
             return;
        }

        int added = 0;

        foreach (var shaderName in shaders)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Shader not found in project: {shaderName}");
                continue;
            }

            // Check if already included
            bool found = false;
            for (int i = 0; i < it.arraySize; i++)
            {
                var elem = it.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == shader)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                it.InsertArrayElementAtIndex(it.arraySize);
                it.GetArrayElementAtIndex(it.arraySize - 1).objectReferenceValue = shader;
                added++;
                Debug.Log($"Included shader: {shaderName}");
            }
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>SUCCESS: {added} shaders added to Always Included list.</color>");
            EditorUtility.DisplayDialog("Fix Pink Textures", $"Added {added} shaders.\nPlease BUILD AND RUN again.", "OK");
        }
        else
        {
             EditorUtility.DisplayDialog("Fix Pink Textures", "All shaders are already included!", "OK");
        }
    }
}
