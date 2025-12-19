using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using InsideMatter.Molecule;
using System.Collections.Generic;
using System.Linq;

namespace InsideMatter.Interaction
{
    /// <summary>
    /// VR-enabled atom grabbing using XR Interaction Toolkit
    /// Replaces mouse-based AtomDrag for VR controllers.
    /// 
    /// Features:
    /// - Preview-Linie beim Annähern an andere Atome
    /// - Bindung festigen beim Loslassen
    /// - Bindung lösen durch Wegziehen
    /// - BondPoint-Wechsel bei Rotation
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class VRAtomGrab : MonoBehaviour
    {
        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private Molecule.Atom atom;
        
        [Header("VR Interaction Settings")]
        [Tooltip("Velocity multiplier when throwing atoms")]
        public float throwVelocityScale = 1.5f;
        
        [Tooltip("Angular velocity multiplier when throwing atoms")]
        public float throwAngularVelocityScale = 1.0f;
        
        [Header("Visual Feedback")]
        [Tooltip("Highlight color when hovering")]
        public Color hoverColor = new Color(1f, 1f, 0.5f, 1f);
        
        [Tooltip("Highlight color when selected")]
        public Color selectColor = new Color(0.5f, 1f, 0.5f, 1f);
        
        private MaterialPropertyBlock propertyBlock;
        private MeshRenderer meshRenderer;
        private Color originalColor;
        private bool isGrabbed = false;
        
        [Header("Haptic Feedback")]
        [Tooltip("Haptic intensity on grab (0-1)")]
        [Range(0f, 1f)]
        public float grabHapticIntensity = 0.3f;
        
        [Tooltip("Haptic intensity on bond snap (0-1)")]
        [Range(0f, 1f)]
        public float snapHapticIntensity = 0.6f;
        
        [Tooltip("Haptic intensity when bond is confirmed")]
        [Range(0f, 1f)]
        public float bondConfirmHapticIntensity = 0.8f;
        
        [Tooltip("Duration of haptic feedback in seconds")]
        public float hapticDuration = 0.1f;

        [Header("Bond Preview Settings")]
        [Tooltip("Maximaler Abstand für Bond-Vorschau (Reduziert für präziseres Snapping)")]
        public float previewDistance = 0.4f;

        [Tooltip("Minimale Rotation in Grad um BondPoint zu wechseln")]
        public float rotationThreshold = 20f;

        [Tooltip("Kraft zum Lösen einer Bindung (Distanz die gezogen werden muss)")]
        public float bondBreakDistance = 0.5f;

        // Bond Preview Tracking
        private Molecule.Atom nearbyAtom;
        private BondPoint currentPreviewBondPointSelf;
        private BondPoint currentPreviewBondPointOther;
        private bool hasActivePreview = false;

        // Rotation Tracking für BondPoint-Wechsel
        private Quaternion lastRotation;
        private float accumulatedRotation = 0f;

        // Bond Breaking Tracking
        private Vector3 grabStartPosition;
        private List<Bond> bondsAtGrabStart = new List<Bond>();

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            atom = GetComponent<Molecule.Atom>();
            propertyBlock = new MaterialPropertyBlock();
            
            // Find the Visual child mesh renderer
            Transform visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                meshRenderer = visualChild.GetComponent<MeshRenderer>();
            }
            
            // Fallback: search in children
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (meshRenderer != null && atom != null)
            {
                originalColor = atom.atomColor;
            }
            
            // Configure XR Grab Interactable
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = true;
            grabInteractable.throwSmoothingDuration = 0.25f;
            grabInteractable.attachEaseInTime = 0.15f;
            
            // Set velocity scaling
            grabInteractable.throwVelocityScale = throwVelocityScale;
            grabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
            
            // Subscribe to events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            grabInteractable.hoverEntered.AddListener(OnHoverEnter);
            grabInteractable.hoverExited.AddListener(OnHoverExit);
        }

        private void Start()
        {
            // Configure rigidbody for VR interaction
            if (rb != null)
            {
                rb.useGravity = false; // Atoms float
                rb.mass = 0.1f;
                rb.linearDamping = 1.0f; // Increased damping for weightless feel
                rb.angularDamping = 1.0f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.constraints = RigidbodyConstraints.None;
            }
        }

        private void Update()
        {
            if (isGrabbed)
            {
                UpdateBondPreview();
                CheckRotationForBondPointSwitch();
                CheckBondBreaking();
            }
        }

        /// <summary>
        /// Sucht nach nahegelegenen Atomen und zeigt Bond-Vorschau an
        /// </summary>
        private void UpdateBondPreview()
        {
            if (atom == null || !atom.HasFreeBond) return;

            // Finde nahegelegene Atome
            Collider[] nearby = Physics.OverlapSphere(transform.position, previewDistance);
            Molecule.Atom closestAtom = null;
            float closestDistance = float.MaxValue;

            foreach (var col in nearby)
            {
                // FIX: Use GetComponentInParent because colliders might be on child objects (like BondPoints)
                var otherAtom = col.GetComponentInParent<Molecule.Atom>();
                if (otherAtom != null && otherAtom != atom && otherAtom.HasFreeBond)
                {
                    // NEU: Nur wenn nicht bereits Teil desselben Moleküls (verhindert "wildes" Springen in Ketten)
                    if (!IsIndirectlyConnected(atom, otherAtom))
                    {
                        float dist = Vector3.Distance(transform.position, otherAtom.transform.position);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            closestAtom = otherAtom;
                        }
                    }
                }
            }

            if (closestAtom != null)
            {
                // Bestes BondPoint-Paar finden
                var (bpSelf, bpOther) = Molecule.BondPreview.FindBestBondPointPair(atom, closestAtom);

                if (bpSelf != null && bpOther != null)
                {
                    nearbyAtom = closestAtom;
                    currentPreviewBondPointSelf = bpSelf;
                    currentPreviewBondPointOther = bpOther;

                    // Preview anzeigen
                    if (Molecule.BondPreview.Instance != null)
                    {
                        Molecule.BondPreview.Instance.ShowPreview(bpSelf, bpOther);
                        hasActivePreview = true;
                    }
                }
                else
                {
                    HidePreview();
                }
            }
            else
            {
                HidePreview();
            }
        }

        /// <summary>
        /// Prüft Rotation und wechselt BondPoint wenn genug gedreht wurde
        /// </summary>
        private void CheckRotationForBondPointSwitch()
        {
            if (!hasActivePreview || nearbyAtom == null) return;

            // Rotations-Differenz berechnen
            float angleDiff = Quaternion.Angle(lastRotation, transform.rotation);
            accumulatedRotation += angleDiff;
            lastRotation = transform.rotation;

            if (accumulatedRotation >= rotationThreshold)
            {
                // Zum nächsten freien BondPoint wechseln
                var nextBondPoint = Molecule.BondPreview.GetNextFreeBondPoint(atom, currentPreviewBondPointSelf);
                
                if (nextBondPoint != null && nextBondPoint != currentPreviewBondPointSelf)
                {
                    currentPreviewBondPointSelf = nextBondPoint;

                    // Neues bestes Paar finden (mit dem neuen eigenen BondPoint)
                    var closestOther = Molecule.BondPreview.FindClosestFreeBondPoint(nearbyAtom, currentPreviewBondPointSelf.transform.position);
                    if (closestOther != null)
                    {
                        currentPreviewBondPointOther = closestOther;

                        // Preview aktualisieren
                        if (Molecule.BondPreview.Instance != null)
                        {
                            Molecule.BondPreview.Instance.ShowPreview(currentPreviewBondPointSelf, currentPreviewBondPointOther);
                        }

                        // Leichtes Haptic Feedback beim Wechsel
                        if (grabInteractable.interactorsSelecting.Count > 0)
                        {
                            SendHapticFeedback(grabInteractable.interactorsSelecting[0], grabHapticIntensity * 0.5f, hapticDuration * 0.5f);
                        }

                        UnityEngine.Debug.Log($"Switched to BondPoint: {atom.bondPoints.IndexOf(currentPreviewBondPointSelf)}");
                    }
                }

                accumulatedRotation = 0f;
            }
        }

        /// <summary>
        /// Prüft ob eine bestehende Bindung gelöst werden soll (durch Wegziehen)
        /// </summary>
        private void CheckBondBreaking()
        {
            if (bondsAtGrabStart.Count == 0) return;

            float distanceMoved = Vector3.Distance(transform.position, grabStartPosition);

            if (distanceMoved > bondBreakDistance)
            {
                // Alle Bindungen dieses Atoms lösen
                if (MoleculeManager.Instance != null)
                {
                    foreach (var bond in bondsAtGrabStart)
                    {
                        if (bond != null)
                        {
                            Vector3 bondPosition = (bond.BondPointA.transform.position + bond.BondPointB.transform.position) / 2f;
                            MoleculeManager.Instance.RemoveBond(bond);
                            
                            // Sound abspielen
                            if (Molecule.BondPreview.Instance != null)
                            {
                                Molecule.BondPreview.Instance.PlayBreakSound(bondPosition);
                            }
                        }
                    }

                    // Haptic Feedback beim Lösen
                    if (grabInteractable.interactorsSelecting.Count > 0)
                    {
                        SendHapticFeedback(grabInteractable.interactorsSelecting[0], snapHapticIntensity, hapticDuration * 1.5f);
                    }

                    UnityEngine.Debug.Log($"Bonds broken for atom: {atom?.element ?? "Unknown"}");
                }

                bondsAtGrabStart.Clear();
                grabStartPosition = transform.position; // Reset für weitere Bewegungen
            }
        }

        /// <summary>
        /// Versteckt die aktuelle Preview
        /// </summary>
        private void HidePreview()
        {
            hasActivePreview = false;
            nearbyAtom = null;
            currentPreviewBondPointSelf = null;
            currentPreviewBondPointOther = null;

            if (Molecule.BondPreview.Instance != null)
            {
                Molecule.BondPreview.Instance.HidePreview();
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            
            // Apply selection highlight
            if (meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", selectColor);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
            
            // Haptic feedback
            SendHapticFeedback(args.interactorObject, grabHapticIntensity, hapticDuration);
            
            // Rotation-Tracking starten
            lastRotation = transform.rotation;
            accumulatedRotation = 0f;

            // Grab-Position speichern für Bond-Breaking
            grabStartPosition = transform.position;
            
            // Aktuelle Bonds speichern
            bondsAtGrabStart.Clear();
            if (atom != null && MoleculeManager.Instance != null)
            {
                bondsAtGrabStart.AddRange(MoleculeManager.Instance.GetBondsForAtom(atom));
            }
            
            UnityEngine.Debug.Log($"VR Grabbed atom: {atom?.element ?? "Unknown"}");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            
            // Restore original color
            if (meshRenderer != null && atom != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", originalColor);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
            
            // Trigger creation if we have an active preview
            bool bondCreated = false;
            if (hasActivePreview && Molecule.BondPreview.Instance != null)
            {
                UnityEngine.Debug.Log($"Attempting to confirm bond for {atom?.element} with active preview.");
                bondCreated = Molecule.BondPreview.Instance.ConfirmBond();
                if (bondCreated)
                {
                    // Starkes Haptic Feedback bei erfolgreicher Bindung
                    SendHapticFeedback(args.interactorObject, bondConfirmHapticIntensity, hapticDuration * 2f);
                    UnityEngine.Debug.Log($"Bond SUCCESS: {atom?.element ?? "Unknown"}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Bond FAILED: ConfirmBond returned false for {atom?.element}");
                }
            }
            else if (!hasActivePreview)
            {
                UnityEngine.Debug.Log($"No bond created because hasActivePreview was false for {atom?.element}");
            }
            else if (Molecule.BondPreview.Instance == null)
            {
                UnityEngine.Debug.LogError("BondPreview.Instance is null! Cannot create bond.");
            }
            
            // Preview verstecken
            HidePreview();
            
            // Tracking zurücksetzen
            nearbyAtom = null;
            bondsAtGrabStart.Clear();
            
            UnityEngine.Debug.Log($"VR Released atom: {atom?.element ?? "Unknown"}");
        }



        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            // Apply hover highlight only if not already grabbed
            if (!isGrabbed && meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", hoverColor);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
            
            // Light haptic feedback on hover
            SendHapticFeedback(args.interactorObject, grabHapticIntensity * 0.3f, hapticDuration * 0.5f);
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            // Restore original color only if not grabbed
            if (!isGrabbed && meshRenderer != null && atom != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", originalColor);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Called by BondPoint when a bond is created - triggers haptic feedback
        /// </summary>
        public void OnBondCreated()
        {
            if (isGrabbed && grabInteractable.interactorsSelecting.Count > 0)
            {
                var interactor = grabInteractable.interactorsSelecting[0];
                SendHapticFeedback(interactor, snapHapticIntensity, hapticDuration);
            }
        }

        /// <summary>
        /// Prüft ob zwei Atome bereits über das Bindungs-Netzwerk verbunden sind.
        /// </summary>
        private bool IsIndirectlyConnected(Molecule.Atom start, Molecule.Atom target)
        {
            if (start == null || target == null) return false;
            if (start == target) return true;

            HashSet<Molecule.Atom> visited = new HashSet<Molecule.Atom>();
            Queue<Molecule.Atom> queue = new Queue<Molecule.Atom>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target) return true;

                foreach (var neighbor in current.ConnectedAtoms)
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Send haptic feedback to VR controller
        /// </summary>
        private void SendHapticFeedback(IXRInteractor interactor, float intensity, float duration)
        {
            if (interactor is XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(intensity, duration);
            }
        }

        /// <summary>
        /// Get whether this atom is currently being grabbed
        /// </summary>
        public bool IsGrabbed => isGrabbed;

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
                grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
                grabInteractable.hoverExited.RemoveListener(OnHoverExit);
            }
        }
    }
}
