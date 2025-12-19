using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using InsideMatter.VR;
using InsideMatter;

public class VRSceneBuilder : EditorWindow
{
    [MenuItem("InsideMatter/Build VR Scene")]
    public static void BuildVRScene()
    {
        // 1. Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. Try to create XR Origin via Menu Item
        // This is the most reliable way to get the correct prefab setup from the package
        bool xrOriginCreated = EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Action-based)");
        
        if (!xrOriginCreated)
        {
            Debug.LogWarning("Could not auto-create XR Origin. Please create it manually: GameObject -> XR -> XR Origin (Action-based)");
        }
        
        // 3. Find XR Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            // Add VRRigSetup
            var rigSetup = xrOrigin.gameObject.AddComponent<VRRigSetup>();
            rigSetup.ConfigureXRRig();
            
            // Ensure Input Action Manager exists (needed for Action-based)
            var inputManager = FindFirstObjectByType<UnityEngine.InputSystem.InputActionAsset>();
            if (FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>() == null)
            {
                GameObject inputMgrObj = new GameObject("InputActionManager");
                var mgr = inputMgrObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>();
                // Try to find the default actions
                var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/InputSystem_Actions.inputactions");
                if (actions == null)
                {
                    // Fallback search
                    string[] guids = AssetDatabase.FindAssets("t:InputActionAsset XRI Default Input Actions");
                    if (guids.Length > 0)
                    {
                        actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
                
                if (actions != null)
                {
                    mgr.actionAssets = new System.Collections.Generic.List<UnityEngine.InputSystem.InputActionAsset> { actions };
                }
            }
        }
        
        // 4. Setup Game/Molecule Manager
        GameObject managerObj = new GameObject("GameManager");
        var sceneSetup = managerObj.AddComponent<VRPuzzleSceneSetup>();
        sceneSetup.SetupScene();
        
        // 5. Add Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // 6. Add Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(10, 1, 10);
        
        Debug.Log("VR Scene Built Successfully! Don't forget to save the scene.");
    }
}
