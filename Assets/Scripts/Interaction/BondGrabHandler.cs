using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using InsideMatter.Molecule;
using System.Collections.Generic;

namespace InsideMatter.Interaction
{
    /// <summary>
    /// Ermöglicht das Greifen einer Bindungslinie in VR.
    /// Beim Greifen wird das gesamte Molekül starr mitbewegt.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class BondGrabHandler : MonoBehaviour
    {
        private XRGrabInteractable grabInteractable;
        private Bond bond;
        private bool isGrabbed = false;
        
        // Molekül-Tracking für starre Bewegung
        private Dictionary<Atom, Vector3> moleculeAtomOffsets = new Dictionary<Atom, Vector3>();
        private Dictionary<Atom, Quaternion> moleculeAtomRotations = new Dictionary<Atom, Quaternion>();
        private List<Atom> moleculeAtoms = new List<Atom>();
        private Vector3 grabStartPosition;
        private Quaternion grabStartRotation;
        
        [Header("Haptic Feedback")]
        [Range(0f, 1f)]
        public float grabHapticIntensity = 0.3f;
        public float hapticDuration = 0.1f;
        
        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            
            // XR Grab Interactable konfigurieren
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = false;
            grabInteractable.attachEaseInTime = 0.1f;
            
            // Events registrieren
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        
        /// <summary>
        /// Initialisiert den Handler mit der zugehörigen Bond-Referenz.
        /// Wird vom MoleculeManager aufgerufen.
        /// </summary>
        public void Initialize(Bond associatedBond)
        {
            bond = associatedBond;
        }
        
        private void Update()
        {
            if (isGrabbed)
            {
                UpdateMoleculePositions();
            }
        }
        
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            grabStartPosition = transform.position;
            grabStartRotation = transform.rotation;
            
            // Finde das Molekül über die Bond-Atome
            if (bond != null && bond.AtomA != null)
            {
                InitializeMoleculeTracking(bond.AtomA);
            }
            
            // Haptic Feedback
            SendHapticFeedback(args.interactorObject, grabHapticIntensity, hapticDuration);
            
            Debug.Log($"Bond grabbed: {bond?.AtomA?.element} <-> {bond?.AtomB?.element}");
        }
        
        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            moleculeAtomOffsets.Clear();
            moleculeAtomRotations.Clear();
            moleculeAtoms.Clear();
            
            Debug.Log("Bond released");
        }
        
        /// <summary>
        /// Initialisiert das Tracking aller verbundenen Atome für starre Molekülbewegung.
        /// </summary>
        private void InitializeMoleculeTracking(Atom startAtom)
        {
            moleculeAtomOffsets.Clear();
            moleculeAtomRotations.Clear();
            moleculeAtoms.Clear();
            
            if (startAtom == null) return;
            
            // Finde alle transitiv verbundenen Atome (BFS)
            moleculeAtoms = GetAllConnectedAtoms(startAtom);
            
            // Speichere relative Positionen und Rotationen zur Bond-Mitte
            foreach (var atom in moleculeAtoms)
            {
                Vector3 offset = atom.transform.position - transform.position;
                moleculeAtomOffsets[atom] = offset;
                
                Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * atom.transform.rotation;
                moleculeAtomRotations[atom] = relativeRotation;
            }
        }
        
        /// <summary>
        /// Aktualisiert die Positionen aller verbundenen Atome basierend auf der Bond-Bewegung.
        /// </summary>
        private void UpdateMoleculePositions()
        {
            if (moleculeAtomOffsets.Count == 0) return;
            
            // Berechne Rotations-Delta seit Grab-Start
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(grabStartRotation);
            
            foreach (var kvp in moleculeAtomOffsets)
            {
                var atom = kvp.Key;
                var originalOffset = kvp.Value;
                
                if (atom != null && atom.transform != null)
                {
                    // Rotiere den Offset-Vektor
                    Vector3 rotatedOffset = rotationDelta * originalOffset;
                    atom.transform.position = transform.position + rotatedOffset;
                    
                    // Setze auch die Rotation
                    if (moleculeAtomRotations.TryGetValue(atom, out Quaternion relativeRotation))
                    {
                        atom.transform.rotation = transform.rotation * relativeRotation;
                    }
                }
            }
            
            // Bond-Visuals aktualisieren (falls nötig)
            bond?.UpdateVisual();
        }
        
        /// <summary>
        /// Findet alle transitiv verbundenen Atome (BFS durch das Bindungsnetzwerk).
        /// </summary>
        private List<Atom> GetAllConnectedAtoms(Atom startAtom)
        {
            List<Atom> result = new List<Atom>();
            if (startAtom == null) return result;
            
            HashSet<Atom> visited = new HashSet<Atom>();
            Queue<Atom> queue = new Queue<Atom>();
            
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
        
        private void SendHapticFeedback(IXRInteractor interactor, float intensity, float duration)
        {
            if (interactor is XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(intensity, duration);
            }
        }
        
        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }
    }
}
