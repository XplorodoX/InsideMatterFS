using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsideMatter
{
    /// <summary>
    /// A simple utility to fix common URP setup issues automatically.
    /// </summary>
    public static class URPScriptableFixer
    {
#if UNITY_EDITOR
        [MenuItem("Tools/InsideMatter/Fix Camera Data")]
        public static void FixCameraData()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int fixedCount = 0;

            foreach (var cam in cameras)
            {
                var additionalData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (additionalData == null)
                {
                    cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    fixedCount++;
                }
            }

            Debug.Log($"[URP Fixer] Added UniversalAdditionalCameraData to {fixedCount} cameras.");
        }

        [MenuItem("Tools/InsideMatter/Bulk Upgrade Materials in Selection")]
        public static void UpgradeSelectedMaterials()
        {
            // This is a wrapper for the built-in conversion if possible, 
            // but usually it's better to use the Render Pipeline Converter.
            // We just point the user back to the standard tool for materials.
            Debug.Log("[URP Fixer] Please use 'Window > Rendering > Render Pipeline Converter' for material upgrades.");
        }
#endif
    }
}
