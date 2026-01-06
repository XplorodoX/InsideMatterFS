using System.Collections.Generic;
using UnityEngine;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Zentraler Manager für alle Molekül-Operationen.
    /// Verwaltet Bindungen zwischen Atomen und die visuelle Darstellung.
    /// </summary>
    public class MoleculeManager : MonoBehaviour
    {
        public static MoleculeManager Instance { get; private set; }
        
        [Header("Bond Visuals")]
        [Tooltip("Prefab für die visuelle Darstellung einer Bindung (Zylinder)")]
        public GameObject bondVisualPrefab;
        
        [Tooltip("Material für Bindungen")]
        public Material bondMaterial;
        
        [Tooltip("Durchmesser der Bindungs-Zylinder")]
        public float bondThickness = 0.08f;
        
        [Header("Snap Einstellungen")]
        [Tooltip("Snap-Stärke (0 = aus, 1 = instant)")]
        [Range(0f, 1f)]
        public float snapStrength = 0.5f;
        
        [Tooltip("Mindestabstand zwischen Atomen nach dem Snapping (Sorgt für sichtbare Bindungs-Stangen)")]
        public float minAtomDistance = 0.7f;
        
        [Header("Debug")]
        [Tooltip("Zeige Debug-Informationen in der Konsole")]
        public bool debugMode = false;
        
        // Liste aller aktiven Bindungen
        private List<Bond> bonds = new List<Bond>();
        
        // Container für Bond-Visuals
        private Transform bondContainer;
        
        /// <summary>
        /// Alle aktiven Bindungen
        /// </summary>
        public IReadOnlyList<Bond> Bonds => bonds.AsReadOnly();
        
        void Awake()
        {
            // Singleton Pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Mehrere MoleculeManager gefunden! Zerstöre Duplikat.");
                Destroy(gameObject);
                return;
            }
            
            // Container für Bonds erstellen
            bondContainer = new GameObject("Bonds").transform;
            bondContainer.SetParent(transform);
        }
        
        void Start()
        {
            // Fallback: Standard Bond-Visual erstellen falls keins zugewiesen
            if (bondVisualPrefab == null)
            {
                CreateDefaultBondPrefab();
            }
        }
        
        void Update()
        {
            // Bond-Visuals aktualisieren (falls Atome bewegt werden)
            foreach (var bond in bonds)
            {
                bond.UpdateVisual();
            }
        }
        
        /// <summary>
        /// Erstellt eine Bindung zwischen zwei BondPoints
        /// </summary>
        public void CreateBond(BondPoint bondPointA, BondPoint bondPointB, BondType bondType = BondType.Single)
        {
            if (bondPointA == null || bondPointB == null)
            {
                Debug.LogWarning("Kann Bindung nicht erstellen: BondPoint ist null");
                return;
            }
            
            Atom atomA = bondPointA.ParentAtom;
            Atom atomB = bondPointB.ParentAtom;
            
            if (atomA == null || atomB == null)
            {
                Debug.LogWarning("Kann Bindung nicht erstellen: Parent-Atom ist null");
                return;
            }
            
            // Prüfen ob Bindung bereits existiert
            if (BondExists(atomA, atomB))
            {
                if (debugMode) Debug.Log($"Bindung zwischen {atomA.element} und {atomB.element} existiert bereits");
                return;
            }
            
            // 1. Atome ausrichten und snappen (Instant beim Festigen)
            SnapAtoms(bondPointA, bondPointB, true);
            
            // 2. BondPoints als belegt markieren
            bondPointA.Occupied = true;
            bondPointB.Occupied = true;
            
            // 3. Bond-Objekt erstellen mit gewähltem Typ
            Bond bond = new Bond(atomA, atomB, bondPointA, bondPointB);
            bond.Type = bondType; // Bond-Typ setzen (Single/Double/Triple)
            bonds.Add(bond);
            
            // 4. Logische Verbindung registrieren
            atomA.AddConnection(atomB);
            atomB.AddConnection(atomA);
            
            // 5. Visuelle Darstellung erstellen
            CreateBondVisual(bond);
            
            // 6. Physikalisches Festigen (FIXED JOINT)
            Rigidbody rbA = atomA.GetComponent<Rigidbody>();
            Rigidbody rbB = atomB.GetComponent<Rigidbody>();
            if (rbA != null && rbB != null)
            {
                FixedJoint joint = atomA.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = rbB;
                joint.breakForce = float.PositiveInfinity;
                joint.breakTorque = float.PositiveInfinity;
                bond.Joint = joint;
            }
           
            if (debugMode)
            {
                Debug.Log($"Bindung erstellt: {atomA.element} <-> {atomB.element} (Physik verbunden)");
            }
        }
        
        /// <summary>
        /// Entfernt eine Bindung zwischen zwei Atomen
        /// </summary>
        public void RemoveBond(Bond bond)
        {
            if (bond == null) return;
            
            // BondPoints freigeben
            if (bond.BondPointA != null) bond.BondPointA.Occupied = false;
            if (bond.BondPointB != null) bond.BondPointB.Occupied = false;
            
            // Logische Verbindung entfernen
            if (bond.AtomA != null && bond.AtomB != null)
            {
                bond.AtomA.RemoveConnection(bond.AtomB);
                bond.AtomB.RemoveConnection(bond.AtomA);
            }
            
            // Visual zerstören
            if (bond.Visual != null)
            {
                Destroy(bond.Visual);
            }

            // Physik lösen
            if (bond.Joint != null)
            {
                Destroy(bond.Joint);
            }
            
            // Aus Liste entfernen
            bonds.Remove(bond);
            
            if (debugMode)
            {
                Debug.Log($"Bindung entfernt: {bond.AtomA?.element} <-> {bond.AtomB?.element}");
            }
        }
        
        /// <summary>
        /// Prüft ob eine Bindung zwischen zwei Atomen bereits existiert
        /// </summary>
        private bool BondExists(Atom atomA, Atom atomB)
        {
            foreach (var bond in bonds)
            {
                if ((bond.AtomA == atomA && bond.AtomB == atomB) ||
                    (bond.AtomA == atomB && bond.AtomB == atomA))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Richtet zwei Atome aneinander aus (Snapping)
        /// </summary>
        private void SnapAtoms(BondPoint bondPointA, BondPoint bondPointB, bool instant = false)
        {
            Atom atomA = bondPointA.ParentAtom;
            Atom atomB = bondPointB.ParentAtom;
            
            float strength = instant ? 1.0f : snapStrength;
            
            // Position von atomB so anpassen, dass es den minAtomDistance Abstand zu atomA hält
            Vector3 dirA = (bondPointA.transform.position - atomA.transform.position).normalized;
            Vector3 targetPos = atomA.transform.position + dirA * minAtomDistance;
            
            // Snap mit Lerp
            atomB.transform.position = Vector3.Lerp(
                atomB.transform.position,
                targetPos,
                strength
            );
            
            // Rotation anpassen: BondPoints sollen entgegengesetzt zeigen
            Vector3 dirB = (bondPointB.transform.position - atomB.transform.position).normalized;
            
            // Zielrichtung: bondPointB soll in Richtung -dirA zeigen
            Quaternion targetRot = Quaternion.FromToRotation(dirB, -dirA) * atomB.transform.rotation;
            
            atomB.transform.rotation = Quaternion.Lerp(
                atomB.transform.rotation,
                targetRot,
                strength
            );
        }
        
        /// <summary>
        /// Erstellt die visuelle Darstellung einer Bindung
        /// </summary>
        private void CreateBondVisual(Bond bond)
        {
            if (bondVisualPrefab == null)
            {
                Debug.LogWarning("Kein Bond Visual Prefab zugewiesen!");
                return;
            }
            
            GameObject visual = Instantiate(bondVisualPrefab, bondContainer);
            visual.name = $"Bond_{bond.AtomA.element}_{bond.AtomB.element}";
            
            // WICHTIG: Sicherstellen dass das Visual aktiv ist (das Default-Prefab ist deaktiviert)
            visual.SetActive(true);
            
            // FIX: Disable the parent prefab's renderer - only child cylinders from BondVisual should be visible
            Renderer parentRenderer = visual.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                parentRenderer.enabled = false;
            }
            
            // FIX: Reset parent scale to 1,1,1 so child cylinder scales are not affected
            visual.transform.localScale = Vector3.one;
            
            bond.Visual = visual;
            bond.UpdateVisual();
            
            if (debugMode)
            {
                Debug.Log($"Bond Visual erstellt zwischen {bond.AtomA.element} und {bond.AtomB.element}");
            }
        }
        
        /// <summary>
        /// Erstellt ein Standard-Bond-Prefab falls keins zugewiesen wurde
        /// </summary>
        private void CreateDefaultBondPrefab()
        {
            // Template-Cylinder erstellen
            bondVisualPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bondVisualPrefab.name = "DefaultBondVisual_Template";
            
            // Als Kind des Managers verstecken (nicht in der Szene sichtbar)
            bondVisualPrefab.transform.SetParent(transform);
            bondVisualPrefab.transform.localPosition = Vector3.zero;
            
            // Collider entfernen (brauchen wir nicht für Visuals)
            var collider = bondVisualPrefab.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            // Material erstellen falls nicht vorhanden
            if (bondMaterial == null)
            {
                // Versuche einen sicheren Shader zu finden
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }
                
                bondMaterial = new Material(shader);
                bondMaterial.SetColor("_BaseColor", Color.white);
                bondMaterial.SetColor("_Color", Color.white);
            }
            
            var renderer = bondVisualPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = bondMaterial;
            }
            
            // Template deaktivieren (wird als Vorlage für Instantiate verwendet)
            bondVisualPrefab.SetActive(false);
            
            Debug.Log("[MoleculeManager] Standard Bond Visual Prefab erstellt. Für bessere Ergebnisse, weise ein eigenes Prefab im Inspector zu.");
        }
        
        /// <summary>
        /// Gibt alle Bindungen eines bestimmten Atoms zurück
        /// </summary>
        public List<Bond> GetBondsForAtom(Atom atom)
        {
            List<Bond> result = new List<Bond>();
            foreach (var bond in bonds)
            {
                if (bond.ContainsAtom(atom))
                {
                    result.Add(bond);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Zerstört alle Bindungen eines Atoms
        /// </summary>
        public void RemoveAllBondsForAtom(Atom atom)
        {
            List<Bond> atomBonds = GetBondsForAtom(atom);
            foreach (var bond in atomBonds)
            {
                RemoveBond(bond);
            }
        }


        /// <summary>
        /// Berechnet die aktuell genutzte Valenz eines Atoms (Summe der Bindungsordnungen)
        /// </summary>
        public int CalculateCurrentValence(Atom atom)
        {
            if (atom == null) return 0;
            
            int valence = 0;
            List<Bond> atomBonds = GetBondsForAtom(atom);
            
            foreach (var bond in atomBonds)
            {
                switch (bond.Type)
                {
                    case BondType.Single: valence += 1; break;
                    case BondType.Double: valence += 2; break;
                    case BondType.Triple: valence += 3; break;
                }
            }
            
            return valence;
        }

        /// <summary>
        /// Prüft, ob eine Bindung auf einen neuen Typ aufgewertet werden kann (Valenz-Check)
        /// </summary>
        public bool CanUpgradeBond(Bond bond, BondType newType)
        {
            if (bond == null) return false;

            int costDiff = GetBondCost(newType) - GetBondCost(bond.Type);
            
            // Check Atom A
            int currentValenceA = CalculateCurrentValence(bond.AtomA);
            if (currentValenceA + costDiff > bond.AtomA.maxBonds) return false;

            // Check Atom B
            int currentValenceB = CalculateCurrentValence(bond.AtomB);
            if (currentValenceB + costDiff > bond.AtomB.maxBonds) return false;

            return true;
        }

        private int GetBondCost(BondType type)
        {
            switch (type)
            {
                case BondType.Single: return 1;
                case BondType.Double: return 2;
                case BondType.Triple: return 3;
                default: return 0;
            }
        }
    }
}
