using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace InsideMatter.VR
{
    /// <summary>
    /// Helper script to configure VR rig with XR Interaction Toolkit
    /// Automatically sets up XR Origin, controllers, and teleportation
    /// </summary>
    public class VRRigSetup : MonoBehaviour
    {
        [Header("Controller Settings")]
        [Tooltip("Enable ray interactors for UI and distant objects")]
        public bool enableRayInteractors = true;
        
        [Tooltip("Enable direct interactors for grabbing nearby objects")]
        public bool enableDirectInteractors = true;
        
        [Tooltip("Max distance for ray interaction")]
        public float maxRayDistance = 10f;
        
        [Header("Teleportation")]
        [Tooltip("Enable teleportation locomotion")]
        public bool enableTeleportation = true;
        
        [Tooltip("Ground layer for teleportation")]
        public LayerMask teleportationLayerMask = 1; // Default layer
        
        [Header("Visual Feedback")]
        [Tooltip("Controller model prefabs (optional)")]
        public GameObject leftControllerModel;
        public GameObject rightControllerModel;
        
        [Tooltip("Show ray visuals")]
        public bool showRayVisuals = true;
        public Material rayMaterial;
        public Color rayColor = new Color(0.2f, 0.6f, 1f, 0.5f);

        private void Start()
        {
            // Check if XR Origin exists
            if (FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>() == null)
            {
                UnityEngine.Debug.LogWarning("No XR Origin found! Use GameObject -> XR -> XR Origin (Action-based) from menu");
            }
        }

        /// <summary>
        /// Call this from editor or runtime to configure an existing XR rig
        /// </summary>
        public void ConfigureXRRig()
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                UnityEngine.Debug.LogError("No XR Origin found! Create one first using GameObject -> XR -> XR Origin");
                return;
            }

            ConfigureControllers(xrOrigin.transform);
            
            if (enableTeleportation)
            {
                ConfigureTeleportation(xrOrigin.gameObject);
            }
            
            UnityEngine.Debug.Log("VR Rig configured successfully!");
        }

        private void ConfigureControllers(Transform xrOrigin)
        {
            Transform leftController = xrOrigin.Find("Camera Offset/LeftHand Controller");
            Transform rightController = xrOrigin.Find("Camera Offset/RightHand Controller");

            if (leftController != null)
            {
                ConfigureController(leftController.gameObject, true);
            }
            
            if (rightController != null)
            {
                ConfigureController(rightController.gameObject, false);
            }
        }

        private void ConfigureController(GameObject controller, bool isLeft)
        {
            // Add XR Ray Interactor for UI and distant interaction
            if (enableRayInteractors)
            {
                var rayInteractor = controller.GetComponent<XRRayInteractor>();
                if (rayInteractor == null)
                {
                    rayInteractor = controller.AddComponent<XRRayInteractor>();
                }
                
                rayInteractor.maxRaycastDistance = maxRayDistance;
                rayInteractor.enableUIInteraction = true;
                
                if (showRayVisuals)
                {
                    var lineVisual = controller.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
                    if (lineVisual == null)
                    {
                        lineVisual = controller.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
                    }
                    lineVisual.lineBendRatio = 0.5f;
                    lineVisual.invalidColorGradient = CreateGradient(rayColor * 0.5f);
                    lineVisual.validColorGradient = CreateGradient(rayColor);
                }
            }

            // Add XR Direct Interactor for grabbing atoms
            if (enableDirectInteractors)
            {
                GameObject directInteractorObj = new GameObject("Direct Interactor");
                directInteractorObj.transform.SetParent(controller.transform, false);
                
                var directInteractor = directInteractorObj.AddComponent<XRDirectInteractor>();
                
                // Add sphere collider for grab detection
                var sphereCollider = directInteractorObj.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1f;
                
                UnityEngine.Debug.Log($"Configured {(isLeft ? "left" : "right")} controller with direct interactor");
            }
        }

        private void ConfigureTeleportation(GameObject xrOrigin)
        {
            // Add teleportation provider if not exists
            var teleportProvider = xrOrigin.GetComponent<TeleportationProvider>();
            if (teleportProvider == null)
            {
                teleportProvider = xrOrigin.AddComponent<TeleportationProvider>();
            }

            UnityEngine.Debug.Log("Teleportation configured - create TeleportationAreas in your scene!");
        }

        private Gradient CreateGradient(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
            return gradient;
        }
    }
}
