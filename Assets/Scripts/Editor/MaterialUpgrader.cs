using UnityEngine;
using UnityEditor;

namespace InsideMatter.Editor
{
    public class MaterialUpgrader : EditorWindow
    {
        [MenuItem("Tools/InsideMatter/Upgrade All Materials to URP Lit")]
        public static void UpgradeMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int convertedCount = 0;
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpShader == null)
            {
                Debug.LogError("[MaterialUpgrader] Could not find 'Universal Render Pipeline/Lit' shader! Make sure URP is installed.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && (mat.shader.name == "Standard" || mat.shader.name == "Hidden/InternalErrorShader"))
                {
                    Undo.RecordObject(mat, "Upgrade Shader to URP Lit");
                    
                    // Try to preserve color
                    Color oldColor = Color.white;
                    if (mat.HasProperty("_Color")) oldColor = mat.GetColor("_Color");

                    mat.shader = urpShader;
                    mat.SetColor("_BaseColor", oldColor);
                    
                    // Basic transparency check
                    if (mat.IsKeywordEnabled("_ALPHATEST_ON") || mat.name.Contains("Transparent"))
                    {
                        mat.SetFloat("_Surface", 1);
                        mat.SetOverrideTag("RenderType", "Transparent");
                        mat.renderQueue = 3000;
                    }

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[MaterialUpgrader] ✓ Successfully upgraded {convertedCount} materials to URP Lit shader.");
            EditorUtility.DisplayDialog("Material Upgrade", $"Upgraded {convertedCount} materials to URP Lit shader.", "OK");
        }
    }
}
