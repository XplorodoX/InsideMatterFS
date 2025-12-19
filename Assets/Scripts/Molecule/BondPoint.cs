using UnityEngine;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Andockpunkt an einem Atom, an dem andere Atome binden können.
    /// Hat einen Trigger-Collider, der erkennt, wenn andere BondPoints in der Nähe sind.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class BondPoint : MonoBehaviour
    {
        [Header("Status")]
        [Tooltip("Ist dieser BondPoint bereits belegt?")]
        [SerializeField]
        private bool occupied = false;
        
        [Header("Snap-Einstellungen")]
        [Tooltip("Maximaler Abstand für automatisches Snapping")]
        public float snapDistance = 0.3f;
        
        [Tooltip("Mindestdauer der Berührung vor dem Snappen (Sekunden)")]
        public float snapDelay = 0.1f;
        
        // Referenz zum Parent-Atom
        private Atom parentAtom;
        
        // Aktueller BondPoint in der Nähe
        private BondPoint nearbyBondPoint;
        private float nearbyTimer = 0f;
        
        // Collider für Trigger-Erkennung
        private SphereCollider triggerCollider;
        
        /// <summary>
        /// Ist dieser BondPoint belegt?
        /// </summary>
        public bool Occupied
        {
            get => occupied;
            set => occupied = value;
        }
        
        /// <summary>
        /// Das Parent-Atom dieses BondPoints
        /// </summary>
        public Atom ParentAtom
        {
            get => parentAtom;
            set => parentAtom = value;
        }
        
        void Awake()
        {
            // Trigger-Collider setup
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            
            triggerCollider.isTrigger = true;
            triggerCollider.radius = snapDistance;
        }
        
        void Update()
        {
            // Snap-Timer nur wenn NICHT per VR gegriffen wird
            // Bei VR übernimmt VRAtomGrab die Preview-Logik
            if (IsParentOrNearbyGrabbedByVR())
            {
                // VR-Modus: Preview wird von VRAtomGrab gesteuert
                nearbyTimer = 0f;
                return;
            }

            // Desktop-Modus: Automatisches Snapping wie bisher
            if (nearbyBondPoint != null && !occupied)
            {
                nearbyTimer += Time.deltaTime;
                
                if (nearbyTimer >= snapDelay)
                {
                    TryCreateBond(nearbyBondPoint);
                    nearbyTimer = 0f;
                }
            }
            else
            {
                nearbyTimer = 0f;
            }
        }

        /// <summary>
        /// Prüft ob das Parent-Atom ODER das nahegelegene Atom gerade von einem VR-Controller gegriffen wird
        /// </summary>
        private bool IsParentOrNearbyGrabbedByVR()
        {
            if (parentAtom != null)
            {
                var vrGrab = parentAtom.GetComponent<InsideMatter.Interaction.VRAtomGrab>();
                // Check if parent atom is grabbed
                if (vrGrab != null && vrGrab.IsGrabbed) return true;
            }

            if (nearbyBondPoint != null && nearbyBondPoint.parentAtom != null)
            {
                var otherVrGrab = nearbyBondPoint.parentAtom.GetComponent<InsideMatter.Interaction.VRAtomGrab>();
                // Check if nearby atom is grabbed
                if (otherVrGrab != null && otherVrGrab.IsGrabbed) return true;
            }
            
            return false;
        }
        
        private void OnTriggerEnter(Collider other)
    {
        // Only trigger when close to another BondPoint
        BondPoint otherBondPoint = other.GetComponent<BondPoint>();
        if (otherBondPoint != null && otherBondPoint != this && !occupied && !otherBondPoint.Occupied)
        {
            // Check if parent atoms have free bonds
            if (parentAtom != null && otherBondPoint.parentAtom != null &&
                parentAtom.HasFreeBond && otherBondPoint.parentAtom.HasFreeBond)
            {
                nearbyBondPoint = otherBondPoint;
                
                // Trigger haptic feedback if VR atom is grabbed
                NotifyVRGrabOfBond();
            }
        }
    }
    
    /// <summary>
    /// Notify VR grab component about bond creation for haptic feedback
    /// </summary>
    private void NotifyVRGrabOfBond()
    {
        var vrGrab = parentAtom?.GetComponent<InsideMatter.Interaction.VRAtomGrab>();
        if (vrGrab != null)
        {
            vrGrab.OnBondCreated();
        }
    }
        
        void OnTriggerExit(Collider other)
        {
            var otherBP = other.GetComponent<BondPoint>();
            if (otherBP == nearbyBondPoint)
            {
                nearbyBondPoint = null;
                nearbyTimer = 0f;
            }
        }
        
        /// <summary>
        /// Versucht eine Bindung mit einem anderen BondPoint zu erstellen
        /// </summary>
        private void TryCreateBond(BondPoint other)
        {
            if (MoleculeManager.Instance != null)
            {
                MoleculeManager.Instance.CreateBond(this, other);
            }
            else
            {
                Debug.LogWarning("MoleculeManager nicht gefunden! Bitte MoleculeManager zur Szene hinzufügen.");
            }
        }
        
        /// <summary>
        /// Debug-Visualisierung
        /// </summary>
        void OnDrawGizmos()
        {
            Gizmos.color = occupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }
    }
}
