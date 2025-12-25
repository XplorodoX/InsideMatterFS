using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Unity.XR.CoreUtils;
using VR.Hands;

public class SetupHandControllerVisuals : EditorWindow
{
    [MenuItem("Tools/Setup Hand Controller Visuals")]
    public static void Setup()
    {
        // 1. Define Paths
        string leftControllerPath = "Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab";
        string rightControllerPath = "Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab";
        
        string leftHandModelPath = "Assets/Samples/XR Hands/1.3.0/HandVisualizer/Models/LeftHand.fbx";
        string rightHandModelPath = "Assets/Samples/XR Hands/1.3.0/HandVisualizer/Models/RightHand.fbx";
        
        string newPrefabsDir = "Assets/Prefabs/HandControllers";
        
        // 2. Validate Assets
        GameObject leftCtrlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(leftControllerPath);
        GameObject rightCtrlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rightControllerPath);
        GameObject leftHandModel = AssetDatabase.LoadAssetAtPath<GameObject>(leftHandModelPath);
        GameObject rightHandModel = AssetDatabase.LoadAssetAtPath<GameObject>(rightHandModelPath);

        if (leftCtrlPrefab == null || rightCtrlPrefab == null || leftHandModel == null || rightHandModel == null)
        {
            Debug.LogError("Could not find necessary assets. Please check paths in the script.");
            return;
        }

        // 3. Create Directory for new Prefabs
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(newPrefabsDir))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "HandControllers");
        }

        // 4. Create New Controller Prefabs
        GameObject newLeftCtrl = CreateHandControllerPrefab(leftCtrlPrefab, leftHandModel, newPrefabsDir + "/XR Hand Controller Left.prefab", true);
        GameObject newRightCtrl = CreateHandControllerPrefab(rightCtrlPrefab, rightHandModel, newPrefabsDir + "/XR Hand Controller Right.prefab", false);

        if (newLeftCtrl == null || newRightCtrl == null) return;

        // 5. Assign to XR Input Modality Manager in Scene
        AssignToSceneInputManager(newLeftCtrl, newRightCtrl);
    }

    private static GameObject CreateHandControllerPrefab(GameObject originalController, GameObject handModel, string savePath, bool isLeft)
    {
        // Instantiate original controller to modify it
        GameObject instance = PrefabUtility.InstantiatePrefab(originalController) as GameObject;
        
        // Find and replace the visual model
        // Usually named "Model Parent" or similar in XR Interaction Toolkit samples
        Transform modelParent = instance.transform.Find("Model Parent");
        if (modelParent == null)
        {
            // Fallback: look for where the mess meshes are
            // But standard XRI setup usually has offset objects. 
            // Let's create a specific container if we can't find one.
             modelParent = instance.transform;
        }

        // Destroy existing visual children (e.g. the controller model)
        // We need to be careful not to destroy functionality. 
        // In "XR Controller Left", the visuals are usually under an object.
        // Let's look for known visual names or just hide them? 
        // Replacing is better.
        
        // Strategy: Clear "Model Parent" children, instantiate Hand Model there.
        // NOTE: The starter assets usually have "XR Controller Left" > "Model Parent" > "UniversalController"
        if (instance.transform.Find("Model Parent") != null)
        {
            Transform mp = instance.transform.Find("Model Parent");
            for (int i = mp.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(mp.GetChild(i).gameObject);
            }
            
            GameObject handInstance = PrefabUtility.InstantiatePrefab(handModel, mp) as GameObject;
            handInstance.transform.localPosition = Vector3.zero;
            handInstance.transform.localRotation = Quaternion.identity;
            
            // Adjust rotation/position if needed. 
            // Often hands need -90 rotation or similar to align with controller grip.
            // For now, identity is a good start, user might need to tweak.
            if (isLeft)
            {
                 // Adjustment often needed for hands
                 handInstance.transform.localEulerAngles = new Vector3(0, -90, -90); // Heuristic guess for left hand
            }
            else
            {
                 handInstance.transform.localEulerAngles = new Vector3(0, 90, 90); // Heuristic guess for right hand
            }
        }
        else
        {
            Debug.LogWarning("Could not find 'Model Parent' in controller prefab. Appending hand model to root.");
            GameObject handInstance = PrefabUtility.InstantiatePrefab(handModel, instance.transform) as GameObject;
        }

        // --- NEW: Add Animation Script ---
        HandAnimationFromController anim = instance.AddComponent<HandAnimationFromController>();
        anim.AutoSetupBones();
        // ---------------------------------

        // Create new Prefab
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, savePath);
        DestroyImmediate(instance); // Remove from scene
        
        Debug.Log($"Created new Hand Controller Prefab: {savePath}");
        return newPrefab;
    }

    private static void AssignToSceneInputManager(GameObject leftPrefab, GameObject rightPrefab)
    {
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("No XROrigin found in scene.");
            return;
        }

        XRInputModalityManager manager = xrOrigin.GetComponent<XRInputModalityManager>();
        if (manager == null)
        {
            Debug.Log("Adding XRInputModalityManager to XR Origin...");
            manager = xrOrigin.gameObject.AddComponent<XRInputModalityManager>();
        }

        // Assign to Motion Controllers
        manager.leftController = leftPrefab;
        manager.rightController = rightPrefab;
        
        Debug.Log("Assigned new Hand Controllers to XR Input Modality Manager.");
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(xrOrigin.gameObject.scene);
    }
}
