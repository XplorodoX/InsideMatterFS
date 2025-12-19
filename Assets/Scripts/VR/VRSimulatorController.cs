using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

namespace InsideMatter.VR
{
    /// <summary>
    /// Simulator für VR-Testing ohne Headset
    /// Benutzt Maus + Tastatur um VR Controller zu simulieren
    /// 
    /// Controls:
    /// - Rechtsklick gedrückt halten = Rechte Hand aktivieren
    /// - Linksklick gedrückt halten = Linke Hand aktivieren  
    /// - G während Hand aktiv = Grip (Greifen)
    /// - T während Hand aktiv = Trigger (UI Button)
    /// - WASD = Bewegung
    /// - Maus bewegen = Schauen
    /// - Q/E = Hand hoch/runter
    /// - R/F = Hand vor/zurück
    /// </summary>
    public class VRSimulatorController : MonoBehaviour
    {
        [Header("Simulator Settings")]
        [Tooltip("Enable keyboard/mouse VR simulation")]
        public bool enableSimulation = true;
        
        [Tooltip("Show hand visualizers")]
        public bool showHandVisuals = true;
        
        [Header("Hand Control")]
        public Transform leftHand;
        public Transform rightHand;
        public Transform cameraTransform;
        
        [Header("Hand Movement Settings")]
        public float handMoveSpeed = 2f;
        public float handDistance = 0.5f;
        public float handHeight = -0.3f;
        public float handSideOffset = 0.3f;
        
        [Header("XR Controller References")]
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor leftDirectInteractor;
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor rightDirectInteractor;
        
        private GameObject leftHandVisual;
        private GameObject rightHandVisual;
        private bool leftHandActive = false;
        private bool rightHandActive = false;
        private bool leftGripping = false;
        private bool rightGripping = false;
        
        private Vector3 leftHandOffset = Vector3.zero;
        private Vector3 rightHandOffset = Vector3.zero;

        private void Start()
        {
            if (!enableSimulation)
            {
                enabled = false;
                return;
            }
            
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;
            
            // Create hand visuals if needed
            if (showHandVisuals)
            {
                CreateHandVisuals();
            }
            
            // Find XR Interactors if not set
            if (leftHand != null && leftDirectInteractor == null)
                leftDirectInteractor = leftHand.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            
            if (rightHand != null && rightDirectInteractor == null)
                rightDirectInteractor = rightHand.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            
            UnityEngine.Debug.Log("VR Simulator активирован! Controls: Right-Click=Right Hand, Left-Click=Left Hand, G=Grip, Q/E=Up/Down, R/F=Forward/Back");
        }

        private void Update()
        {
            if (!enableSimulation || cameraTransform == null)
                return;
            
            HandleHandActivation();
            HandleHandMovement();
            HandleGripInput();
            UpdateHandPositions();
        }

        private void HandleHandActivation()
        {
            // Right mouse button = right hand
            if (Mouse.current.rightButton.isPressed)
            {
                if (!rightHandActive)
                {
                    rightHandActive = true;
                    if (rightHandVisual != null) rightHandVisual.SetActive(true);
                    UnityEngine.Debug.Log("Right Hand aktiviert");
                }
            }
            else
            {
                if (rightHandActive)
                {
                    rightHandActive = false;
                    if (rightHandVisual != null) rightHandVisual.SetActive(false);
                    rightGripping = false;
                }
            }
            
            // Left mouse button = left hand
            if (Mouse.current.leftButton.isPressed)
            {
                if (!leftHandActive)
                {
                    leftHandActive = true;
                    if (leftHandVisual != null) leftHandVisual.SetActive(true);
                    UnityEngine.Debug.Log("Left Hand aktiviert");
                }
            }
            else
            {
                if (leftHandActive)
                {
                    leftHandActive = false;
                    if (leftHandVisual != null) leftHandVisual.SetActive(false);
                    leftGripping = false;
                }
            }
        }

        private void HandleHandMovement()
        {
            float vertical = 0f;
            float horizontal = 0f;
            
            // Q/E = Up/Down
            if (Keyboard.current.qKey.isPressed) vertical += 1f;
            if (Keyboard.current.eKey.isPressed) vertical -= 1f;
            
            // R/F = Forward/Back
            if (Keyboard.current.rKey.isPressed) horizontal += 1f;
            if (Keyboard.current.fKey.isPressed) horizontal -= 1f;
            
            float deltaTime = Time.deltaTime * handMoveSpeed;
            
            if (rightHandActive)
            {
                rightHandOffset.y += vertical * deltaTime;
                rightHandOffset.z += horizontal * deltaTime;
                rightHandOffset.y = Mathf.Clamp(rightHandOffset.y, -1f, 1f);
                rightHandOffset.z = Mathf.Clamp(rightHandOffset.z, -1f, 1f);
            }
            
            if (leftHandActive)
            {
                leftHandOffset.y += vertical * deltaTime;
                leftHandOffset.z += horizontal * deltaTime;
                leftHandOffset.y = Mathf.Clamp(leftHandOffset.y, -1f, 1f);
                leftHandOffset.z = Mathf.Clamp(leftHandOffset.z, -1f, 1f);
            }
        }

        private void HandleGripInput()
        {
            // G = Grip/Grab
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (rightHandActive)
                {
                    rightGripping = !rightGripping;
                    SimulateGrip(rightDirectInteractor, rightGripping);
                    UnityEngine.Debug.Log($"Right Hand Grip: {(rightGripping ? "ON" : "OFF")}");
                }
                
                if (leftHandActive)
                {
                    leftGripping = !leftGripping;
                    SimulateGrip(leftDirectInteractor, leftGripping);
                    UnityEngine.Debug.Log($"Left Hand Grip: {(leftGripping ? "ON" : "OFF")}");
                }
            }
        }

        private void UpdateHandPositions()
        {
            if (leftHand != null)
            {
                Vector3 basePos = cameraTransform.position + 
                                 cameraTransform.forward * handDistance + 
                                 cameraTransform.up * handHeight +
                                 cameraTransform.right * -handSideOffset;
                leftHand.position = basePos + cameraTransform.TransformDirection(leftHandOffset);
                leftHand.rotation = cameraTransform.rotation;
            }
            
            if (rightHand != null)
            {
                Vector3 basePos = cameraTransform.position + 
                                 cameraTransform.forward * handDistance + 
                                 cameraTransform.up * handHeight +
                                 cameraTransform.right * handSideOffset;
                rightHand.position = basePos + cameraTransform.TransformDirection(rightHandOffset);
                rightHand.rotation = cameraTransform.rotation;
            }
        }

        private void SimulateGrip(UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor interactor, bool grip)
        {
            if (interactor == null) return;
            
            if (grip)
            {
                // Try to select hovered interactable
                var validTargets = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();
                interactor.GetValidTargets(validTargets);
                
                if (validTargets.Count > 0)
                {
                    var target = validTargets[0];
                    // Simulate select
                    UnityEngine.Debug.Log($"Attempting to grab: {target}");
                }
            }
        }

        private void CreateHandVisuals()
        {
            // Left hand visual
            if (leftHand != null)
            {
                leftHandVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftHandVisual.name = "Left Hand Visual";
                leftHandVisual.transform.SetParent(leftHand, false);
                leftHandVisual.transform.localScale = Vector3.one * 0.1f;
                leftHandVisual.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                Destroy(leftHandVisual.GetComponent<Collider>());
                leftHandVisual.SetActive(false);
            }
            
            // Right hand visual
            if (rightHand != null)
            {
                rightHandVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightHandVisual.name = "Right Hand Visual";
                rightHandVisual.transform.SetParent(rightHand, false);
                rightHandVisual.transform.localScale = Vector3.one * 0.1f;
                rightHandVisual.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f, 0.5f);
                Destroy(rightHandVisual.GetComponent<Collider>());
                rightHandVisual.SetActive(false);
            }
        }

        private void OnGUI()
        {
            if (!enableSimulation) return;
            
            // Show controls
            GUI.Box(new Rect(10, 10, 300, 180), "VR Simulator Controls");
            
            int yPos = 35;
            GUI.Label(new Rect(20, yPos, 280, 20), "Right-Click: Activate Right Hand");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), "Left-Click: Activate Left Hand");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), "G: Grip/Release (while hand active)");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), "Q/E: Move Hand Up/Down");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), "R/F: Move Hand Forward/Back");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), "WASD: Move Camera");
            yPos += 25;
            
            // Status
            GUI.Label(new Rect(20, yPos, 280, 20), $"Left Hand: {(leftHandActive ? "ACTIVE" : "Inactive")} {(leftGripping ? "[GRIPPING]" : "")}");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Right Hand: {(rightHandActive ? "ACTIVE" : "Inactive")} {(rightGripping ? "[GRIPPING]" : "")}");
        }
    }
}
