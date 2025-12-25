using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRHandsSetup : EditorWindow
{
    [MenuItem("Tools/Setup XR Hands")]
    public static void SetupHands()
    {
        // Find XR Origin
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found in scene!");
            return;
        }

        // Find Input Modality Manager
        XRInputModalityManager inputManager = xrOrigin.GetComponent<XRInputModalityManager>();
        if (inputManager == null)
        {
            Debug.Log("XR Input Modality Manager not found on XR Origin. Adding it...");
            inputManager = xrOrigin.gameObject.AddComponent<XRInputModalityManager>();
        }

        // Parent to Camera Offset
        Transform cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;

        // Load Prefabs
        string leftHandPath = "Assets/Samples/XR Hands/1.3.0/HandVisualizer/Prefabs/Left Hand Tracking.prefab";
        string rightHandPath = "Assets/Samples/XR Hands/1.3.0/HandVisualizer/Prefabs/Right Hand Tracking.prefab";

        GameObject leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(leftHandPath);
        GameObject rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rightHandPath);

        if (leftHandPrefab == null || rightHandPrefab == null)
        {
            Debug.LogError("Could not find Hand Tracking prefabs at expected paths. Please ensure XR Hands samples are imported.");
            return;
        }

        // Instantiate and Assign Left Hand
        if (inputManager.leftHand == null)
        {
            GameObject leftHand = PrefabUtility.InstantiatePrefab(leftHandPrefab, cameraOffset) as GameObject;
            leftHand.name = "Left Hand Tracking";
            inputManager.leftHand = leftHand;
            Debug.Log("Instantiated and assigned Left Hand.");
        }
        else
        {
            Debug.Log("Left Hand already assigned.");
        }

        // Instantiate and Assign Right Hand
        if (inputManager.rightHand == null)
        {
            GameObject rightHand = PrefabUtility.InstantiatePrefab(rightHandPrefab, cameraOffset) as GameObject;
            rightHand.name = "Right Hand Tracking";
            inputManager.rightHand = rightHand;
            Debug.Log("Instantiated and assigned Right Hand.");
        }
        else
        {
            Debug.Log("Right Hand already assigned.");
        }

        // Save Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(xrOrigin.gameObject.scene);
    }
}
