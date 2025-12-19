using UnityEngine;
using InsideMatter.Molecule;

namespace InsideMatter.Interaction
{
    /// <summary>
    /// Ermöglicht das Ziehen und Bewegen von Atomen mit der Maus in 3D.
    /// Verwendet Raycasting für die Maus-Interaktion.
    /// </summary>
    public class AtomDrag : MonoBehaviour
    {
        [Header("Kamera")]
        [Tooltip("Die Kamera für Raycasting (automatisch Main Camera wenn leer)")]
        public Camera mainCamera;
        
        [Header("Drag-Einstellungen")]
        [Tooltip("Distanz von der Kamera, auf der Atome gezogen werden")]
        public float dragDistance = 10f;
        
        [Tooltip("Smoothing beim Ziehen (0 = kein Smoothing, 1 = sehr smooth)")]
        [Range(0f, 0.95f)]
        public float dragSmoothing = 0.3f;
        
        [Tooltip("Nutze eine Plane zum Ziehen statt fixer Distanz")]
        public bool usePlane = true;
        
        [Tooltip("Y-Position der Drag-Plane (wenn usePlane = true)")]
        public float planeHeight = 0f;
        
        [Header("Physics")]
        [Tooltip("Friere Rigidbody während des Ziehens ein")]
        public bool freezePhysicsWhileDragging = true;
        
        [Header("Input")]
        [Tooltip("Mausbutton für Drag (0 = Links, 1 = Rechts, 2 = Mittel)")]
        public int dragMouseButton = 0;
        
        [Header("Layers")]
        [Tooltip("Layer Mask für Atom-Erkennung")]
        public LayerMask atomLayerMask = ~0; // Alle Layer
        
        // Aktuell ausgewähltes Atom
        private Atom selectedAtom;
        private Rigidbody selectedRigidbody;
        
        // Original Physics-Settings
        private bool originalUseGravity;
        private RigidbodyConstraints originalConstraints;
        
        // Drag-Offset
        private Vector3 dragOffset;
        private Vector3 targetPosition;
        
        // Hover-Detection
        private Atom hoveredAtom;
        
        void Start()
        {
            // Kamera automatisch finden falls nicht zugewiesen
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("Keine Kamera gefunden! Bitte Main Camera taggen oder Kamera zuweisen.");
                }
            }
        }
        
        void Update()
        {
            HandleHover();
            HandleDragInput();
            UpdateDrag();
        }
        
        /// <summary>
        /// Hover-Effekt für Atome unter der Maus
        /// </summary>
        private void HandleHover()
        {
            if (selectedAtom != null) return; // Kein Hover während Drag
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, atomLayerMask))
            {
                Atom atom = hit.collider.GetComponent<Atom>();
                
                if (atom != hoveredAtom)
                {
                    // Altes Hover entfernen
                    if (hoveredAtom != null)
                    {
                        hoveredAtom.SetSelected(false);
                    }
                    
                    // Neues Hover setzen
                    hoveredAtom = atom;
                    if (hoveredAtom != null)
                    {
                        hoveredAtom.SetSelected(true);
                    }
                }
            }
            else
            {
                // Kein Atom unter Maus
                if (hoveredAtom != null)
                {
                    hoveredAtom.SetSelected(false);
                    hoveredAtom = null;
                }
            }
        }
        
        /// <summary>
        /// Verarbeitet Maus-Input für Drag-Operationen
        /// </summary>
        private void HandleDragInput()
        {
            // Start Dragging
            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, atomLayerMask))
                {
                    Atom atom = hit.collider.GetComponent<Atom>();
                    if (atom != null)
                    {
                        StartDragging(atom, hit.point);
                    }
                }
            }
            
            // Stop Dragging
            if (Input.GetMouseButtonUp(dragMouseButton))
            {
                if (selectedAtom != null)
                {
                    StopDragging();
                }
            }
        }
        
        /// <summary>
        /// Startet das Ziehen eines Atoms
        /// </summary>
        private void StartDragging(Atom atom, Vector3 hitPoint)
        {
            selectedAtom = atom;
            selectedAtom.SetSelected(true);
            
            // Offset berechnen
            dragOffset = selectedAtom.transform.position - hitPoint;
            targetPosition = selectedAtom.transform.position;
            
            // Rigidbody behandeln
            selectedRigidbody = selectedAtom.GetComponent<Rigidbody>();
            if (selectedRigidbody != null && freezePhysicsWhileDragging)
            {
                originalUseGravity = selectedRigidbody.useGravity;
                originalConstraints = selectedRigidbody.constraints;
                
                selectedRigidbody.useGravity = false;
                selectedRigidbody.linearVelocity = Vector3.zero;
                selectedRigidbody.angularVelocity = Vector3.zero;
                selectedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
        
        /// <summary>
        /// Beendet das Ziehen eines Atoms
        /// </summary>
        private void StopDragging()
        {
            if (selectedAtom != null)
            {
                selectedAtom.SetSelected(false);
                
                // Rigidbody zurücksetzen
                if (selectedRigidbody != null && freezePhysicsWhileDragging)
                {
                    selectedRigidbody.useGravity = originalUseGravity;
                    selectedRigidbody.constraints = originalConstraints;
                }
            }
            
            selectedAtom = null;
            selectedRigidbody = null;
        }
        
        /// <summary>
        /// Aktualisiert die Position des gezogenen Atoms
        /// </summary>
        private void UpdateDrag()
        {
            if (selectedAtom == null || mainCamera == null) return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 newTargetPos;
            
            if (usePlane)
            {
                // Ziehen auf einer Ebene
                Plane dragPlane = new Plane(Vector3.up, new Vector3(0, planeHeight, 0));
                
                if (dragPlane.Raycast(ray, out float distance))
                {
                    newTargetPos = ray.GetPoint(distance) + dragOffset;
                }
                else
                {
                    return; // Keine Intersection mit Plane
                }
            }
            else
            {
                // Ziehen in fester Distanz zur Kamera
                Vector3 mouseWorldPos = ray.GetPoint(dragDistance);
                newTargetPos = mouseWorldPos + dragOffset;
            }
            
            // Smoothing
            targetPosition = Vector3.Lerp(targetPosition, newTargetPos, 1f - dragSmoothing);
            
            // Position setzen
            if (selectedRigidbody != null)
            {
                selectedRigidbody.MovePosition(targetPosition);
            }
            else
            {
                selectedAtom.transform.position = targetPosition;
            }
        }
        
        /// <summary>
        /// Setzt die Drag-Plane-Höhe basierend auf der aktuellen Selektion
        /// </summary>
        public void SetPlaneHeightFromSelection()
        {
            if (selectedAtom != null)
            {
                planeHeight = selectedAtom.transform.position.y;
            }
        }
    }
}
