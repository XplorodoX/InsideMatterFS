using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using InsideMatter.Molecule;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

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

        // VR Controller Input - auto-created, no Inspector setup needed
        private InputAction cycleBondTypeInputAction;

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
         // bondsAtGrabStart removed as we check interactively

        // Molecule Movement Tracking (Rigid body movement for entire molecule)
        private Dictionary<Molecule.Atom, Vector3> moleculeAtomOffsets = new Dictionary<Molecule.Atom, Vector3>();
        private Dictionary<Molecule.Atom, Quaternion> moleculeAtomRotations = new Dictionary<Molecule.Atom, Quaternion>();
        private List<Molecule.Atom> currentMoleculeAtoms = new List<Molecule.Atom>();
        private Quaternion moleculeGrabRotation; // Rotation des gegriffenen Atoms beim Grab-Start

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
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = false; // Disable throwing - atoms stay in place
            grabInteractable.attachEaseInTime = 0.15f;
            
            // Subscribe to events
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            grabInteractable.hoverEntered.AddListener(OnHoverEnter);
            grabInteractable.hoverExited.AddListener(OnHoverExit);
            
            // Configure rigidbody - kinematic so atoms stay in place
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            
            // AUTO-CREATE VR Controller Input: Bind to B button (right) and Y button (left)
            cycleBondTypeInputAction = new InputAction("CycleBondType", InputActionType.Button);
            
            // Generic XR bindings (Oculus Rift/Quest, SteamVR, etc.)
            cycleBondTypeInputAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            cycleBondTypeInputAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            
            // PICO XR specific bindings (PICO 3, PICO 4)
            cycleBondTypeInputAction.AddBinding("<PXR_Controller>{RightHand}/button/b");
            cycleBondTypeInputAction.AddBinding("<PXR_Controller>{LeftHand}/button/y");
            cycleBondTypeInputAction.AddBinding("<PXR_Controller>/secondaryButton"); // Fallback
        }

        private void OnEnable()
        {
            // Enable VR controller input action
            if (cycleBondTypeInputAction != null)
            {
                cycleBondTypeInputAction.Enable();
            }
        }

        private void OnDisable()
        {
            // Disable VR controller input action
            if (cycleBondTypeInputAction != null)
            {
                cycleBondTypeInputAction.Disable();
            }
        }

        private void Update()
        {
            if (isGrabbed)
            {
                UpdateMoleculePositions(); // Move entire molecule rigidly
                UpdateBondPreview();
                CheckRotationForBondPointSwitch();
                CheckBondBreaking();
                CheckBondTypeInput();
            }
        }
        

        /// <summary>
        /// Prüft Input für Bond-Typ Wechsel (Single/Double/Triple)
        /// </summary>
        private void CheckBondTypeInput()
        {
            if (BondPreview.Instance == null) return;
            if (!BondPreview.Instance.IsPreviewActive) return;

            // VR Controller: Secondary Button (B/Y) to cycle bond type
            if (cycleBondTypeInputAction != null && cycleBondTypeInputAction.WasPressedThisFrame())
            {
                BondPreview.Instance.CycleBondType();
                
                // Haptic feedback on bond type change
                if (grabInteractable.interactorsSelecting.Count > 0)
                {
                    SendHapticFeedback(grabInteractable.interactorsSelecting[0], grabHapticIntensity, hapticDuration);
                }
                return;
            }

            // Desktop fallback: Keyboard input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    BondPreview.Instance.SetBondType(BondType.Single);
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    BondPreview.Instance.SetBondType(BondType.Double);
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    BondPreview.Instance.SetBondType(BondType.Triple);
                }
                else if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    BondPreview.Instance.CycleBondType();
                }
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
        /// Erfordert, dass BEIDE Atome gegriffen sind (Zweihand-Interaktion).
        /// </summary>
        private void CheckBondBreaking()
        {
            if (MoleculeManager.Instance == null || atom == null) return;

            // Hole aktuelle Bindungen
            List<Bond> currentBonds = MoleculeManager.Instance.GetBondsForAtom(atom);

            // Rückwärts iterieren, falls wir etwas entfernen
            for (int i = currentBonds.Count - 1; i >= 0; i--)
            {
                var bond = currentBonds[i];
                if (bond == null) continue;

                // Das andere Atom finden
                Molecule.Atom otherAtom = (bond.AtomA == atom) ? bond.AtomB : bond.AtomA;
                
                if (otherAtom != null)
                {
                    // Prüfen, ob das ANDERE Atom auch gegriffen ist
                    var otherGrab = otherAtom.GetComponent<VRAtomGrab>();
                    if (otherGrab != null && otherGrab.IsGrabbed)
                    {
                        // Distanz berechnen
                        float currentDistance = Vector3.Distance(transform.position, otherAtom.transform.position);
                        
                        // Schwelle berechnen: Standard-Abstand + Zieh-Toleranz
                        float breakThreshold = MoleculeManager.Instance.minAtomDistance + bondBreakDistance;
                        
                        if (currentDistance > breakThreshold)
                        {
                            Vector3 bondPosition = (bond.BondPointA.transform.position + bond.BondPointB.transform.position) / 2f;
                            
                            // Bindung lösen
                            MoleculeManager.Instance.RemoveBond(bond);
                            
                            // Sound abspielen
                            if (Molecule.BondPreview.Instance != null)
                            {
                                Molecule.BondPreview.Instance.PlayBreakSound(bondPosition);
                            }

                            // Haptic Feedback für BEIDE Controller
                            TriggerHaptic(grabInteractable);
                            TriggerHaptic(otherGrab.GetComponent<XRGrabInteractable>());

                            UnityEngine.Debug.Log($"Bond broken via pull: {atom.element} <-> {otherAtom.element}");
                        }
                    }
                }
            }
        }

        private void TriggerHaptic(XRBaseInteractable interactable)
        {
             if (interactable != null && interactable.interactorsSelecting.Count > 0)
             {
                 SendHapticFeedback(interactable.interactorsSelecting[0], snapHapticIntensity, hapticDuration * 1.5f);
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
            
            // Make non-kinematic while grabbed for XR movement
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            
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
            
            // Atom als "wurde gegriffen" markieren - erlaubt jetzt Bonding
            if (atom != null) atom.WasEverGrabbed = true;
            
            // MOLECULE TRACKING: Speichere alle verbundenen Atome für starre Bewegung
            InitializeMoleculeTracking();
            
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
                    
                    // NEU: Molekül-Tracking neu initialisieren für aktualisierte Struktur
                    InitializeMoleculeTracking();
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
            
            // Make kinematic again - stay in place
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Tracking zurücksetzen
            nearbyAtom = null;
            
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

        #region Molecule Movement (Rigid Body)
        
        /// <summary>
        /// Initialisiert das Tracking aller verbundenen Atome für starre Molekülbewegung.
        /// Speichert relative Positionen und Rotationen aller Atome im Molekül.
        /// </summary>
        private void InitializeMoleculeTracking()
        {
            moleculeAtomOffsets.Clear();
            moleculeAtomRotations.Clear();
            currentMoleculeAtoms.Clear();
            
            if (atom == null) return;
            
            // Speichere aktuelle Rotation des gegriffenen Atoms
            moleculeGrabRotation = transform.rotation;
            
            // Finde alle transitiv verbundenen Atome (das gesamte Molekül)
            currentMoleculeAtoms = GetAllConnectedAtoms(atom);
            
            // Speichere relative Positionen UND Rotationen zu diesem Atom
            foreach (var moleculeAtom in currentMoleculeAtoms)
            {
                if (moleculeAtom != atom) // Nicht das gegriffene Atom selbst
                {
                    Vector3 offset = moleculeAtom.transform.position - transform.position;
                    moleculeAtomOffsets[moleculeAtom] = offset;
                    
                    // Speichere relative Rotation
                    Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * moleculeAtom.transform.rotation;
                    moleculeAtomRotations[moleculeAtom] = relativeRotation;
                    
                    // Mache das andere Atom kinematisch, damit es sich nicht durch Physik bewegt
                    var otherRb = moleculeAtom.GetComponent<Rigidbody>();
                    if (otherRb != null)
                    {
                        otherRb.isKinematic = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// Aktualisiert die Positionen UND ROTATIONEN aller verbundenen Atome basierend auf der Bewegung des gegriffenen Atoms.
        /// Ermöglicht starre Molekülbewegung inklusive Rotation.
        /// </summary>
        private void UpdateMoleculePositions()
        {
            if (atom == null || moleculeAtomOffsets.Count == 0) return;
            
            // Prüfe ob ein anderes Atom des Moleküls AUCH gegriffen wird (Zweihand-Aktion)
            bool anotherAtomIsGrabbed = false;
            foreach (var moleculeAtom in currentMoleculeAtoms)
            {
                if (moleculeAtom == atom) continue;
                
                var otherGrab = moleculeAtom.GetComponent<VRAtomGrab>();
                if (otherGrab != null && otherGrab.IsGrabbed)
                {
                    anotherAtomIsGrabbed = true;
                    break;
                }
            }
            
            // Wenn zwei Atome gegriffen werden: NICHT synchronisieren (ermöglicht Trennung)
            if (anotherAtomIsGrabbed) return;
            
            // Berechne Rotations-Delta seit Grab-Start
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(moleculeGrabRotation);
            
            // Bewege UND rotiere alle verbundenen Atome starr mit
            foreach (var kvp in moleculeAtomOffsets)
            {
                var moleculeAtom = kvp.Key;
                var originalOffset = kvp.Value;
                
                if (moleculeAtom != null && moleculeAtom.transform != null)
                {
                    // Rotiere den Offset-Vektor mit dem Rotations-Delta
                    Vector3 rotatedOffset = rotationDelta * originalOffset;
                    moleculeAtom.transform.position = transform.position + rotatedOffset;
                    
                    // Setze auch die Rotation des verbundenen Atoms
                    if (moleculeAtomRotations.TryGetValue(moleculeAtom, out Quaternion relativeRotation))
                    {
                        moleculeAtom.transform.rotation = transform.rotation * relativeRotation;
                    }
                }
            }
        }
        
        /// <summary>
        /// Findet alle transitiv verbundenen Atome (BFS durch das Bindungsnetzwerk).
        /// </summary>
        private List<Molecule.Atom> GetAllConnectedAtoms(Molecule.Atom startAtom)
        {
            List<Molecule.Atom> result = new List<Molecule.Atom>();
            if (startAtom == null) return result;
            
            HashSet<Molecule.Atom> visited = new HashSet<Molecule.Atom>();
            Queue<Molecule.Atom> queue = new Queue<Molecule.Atom>();
            
            queue.Enqueue(startAtom);
            visited.Add(startAtom);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);
                
                foreach (var neighbor in current.ConnectedAtoms)
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return result;
        }
        
        #endregion

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
