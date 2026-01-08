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
        public float bondThickness = 0.015f; // Dünne Linien
        
        [Header("Bond Colors")]
        [Tooltip("Farbe für Einfachbindungen")]
        public Color singleBondColor = Color.white;
        
        [Tooltip("Farbe für Doppelbindungen")]
        public Color doubleBondColor = Color.white; // Weiß wie Einfachbindung
        
        [Tooltip("Farbe für Dreifachbindungen")]
        public Color tripleBondColor = Color.white; // Weiß wie Einfachbindung
        
        [Header("Snap Einstellungen")]
        [Tooltip("Snap-Stärke (0 = aus, 1 = instant)")]
        [Range(0f, 1f)]
        public float snapStrength = 0.5f;
        
        [Tooltip("Mindestabstand zwischen Atomen nach dem Snapping (Sorgt für sichtbare Bindungs-Stangen)")]
        public float minAtomDistance = 0.35f;
        
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
            
            // NEU: Valenz-Check vor Bindungserstellung
            int bondCost = GetBondCost(bondType);
            int valenceA = CalculateCurrentValence(atomA);
            int valenceB = CalculateCurrentValence(atomB);
            
            if (valenceA + bondCost > atomA.maxBonds)
            {
                Debug.LogWarning($"Kann {bondType}-Bindung nicht erstellen: {atomA.element} überschreitet Valenz ({valenceA + bondCost} > {atomA.maxBonds})");
                return;
            }
            
            if (valenceB + bondCost > atomB.maxBonds)
            {
                Debug.LogWarning($"Kann {bondType}-Bindung nicht erstellen: {atomB.element} überschreitet Valenz ({valenceB + bondCost} > {atomB.maxBonds})");
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
            // WICHTIG: Verwende die Konstante aus Bond.cs für konsistente kürzere Bindungen!
            bond.SetFixedLength(Bond.DEFAULT_BOND_LENGTH);
            bonds.Add(bond);
            
            // 4. Logische Verbindung registrieren
            atomA.AddConnection(atomB);
            atomB.AddConnection(atomA);
            
            // 5. Visuelle Darstellung erstellen
            CreateBondVisual(bond);
            
            // 6. Physikalisches Festigen - ConfigurableJoint mit komplett gesperrten Achsen
            Rigidbody rbA = atomA.GetComponent<Rigidbody>();
            Rigidbody rbB = atomB.GetComponent<Rigidbody>();
            if (rbA != null && rbB != null)
            {
                // ConfigurableJoint für maximale Stabilität
                ConfigurableJoint joint = atomA.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = rbB;
                
                // ALLE Bewegung und Rotation sperren
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                
                // Unzerstörbar
                joint.breakForce = float.PositiveInfinity;
                joint.breakTorque = float.PositiveInfinity;
                
                bond.ConfigJoint = joint;
                
                // WICHTIG: Beide Atome kinematisch machen um Physik-Drift zu verhindern
                // Sie können immer noch per XR-Grab bewegt werden
                rbA.isKinematic = true;
                rbB.isKinematic = true;
            }
            
            if (debugMode)
            {
                Debug.Log($"Bindung erstellt: {atomA.element} <-> {atomB.element} (Kinamatisch, Locked Joint)");
            }
        }
        
        /// <summary>
        /// Friert die Rotation eines Atoms ein (nach Bindung) - DEPRECATED, jetzt kinematisch
        /// </summary>
        private void FreezeAtomRotation(Atom atom)
        {
            // Nicht mehr benötigt - Atome sind jetzt kinematisch
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

            // ConfigurableJoint zerstören
            if (bond.ConfigJoint != null)
            {
                Destroy(bond.ConfigJoint);
            }
            
            // Legacy: FixedJoint zerstören falls vorhanden
            #pragma warning disable CS0618
            if (bond.Joint != null)
            {
                Destroy(bond.Joint);
            }
            #pragma warning restore CS0618
            
            // Aus Liste entfernen
            bonds.Remove(bond);
            
            // Prüfen ob Atome noch andere Bindungen haben - wenn nicht, wieder beweglich machen
            if (bond.AtomA != null && GetBondsForAtom(bond.AtomA).Count == 0)
            {
                Rigidbody rbA = bond.AtomA.GetComponent<Rigidbody>();
                if (rbA != null) rbA.isKinematic = false;
            }
            if (bond.AtomB != null && GetBondsForAtom(bond.AtomB).Count == 0)
            {
                Rigidbody rbB = bond.AtomB.GetComponent<Rigidbody>();
                if (rbB != null) rbB.isKinematic = false;
            }
            
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
        /// NUR das Atom ohne bestehende Bindungen wird bewegt, um Bindungslängen zu erhalten.
        /// WICHTIG: Rotation wird NICHT mehr geändert - Nutzer behält Kontrolle über Ausrichtung!
        /// </summary>
        private void SnapAtoms(BondPoint bondPointA, BondPoint bondPointB, bool instant = false)
        {
            Atom atomA = bondPointA.ParentAtom;
            Atom atomB = bondPointB.ParentAtom;
            
            // Prüfen welches Atom bereits Bindungen hat
            bool atomAHasBonds = GetBondsForAtom(atomA).Count > 0;
            bool atomBHasBonds = GetBondsForAtom(atomB).Count > 0;
            
            // Fallunterscheidung:
            // 1. Beide haben Bindungen -> keine Positionsänderung (nur FixedJoint wird erstellt)
            // 2. Nur A hat Bindungen -> B wird bewegt
            // 3. Nur B hat Bindungen -> A wird bewegt
            // 4. Keins hat Bindungen -> B wird bewegt (Standard-Verhalten)
            
            if (atomAHasBonds && atomBHasBonds)
            {
                // Beide Atome sind bereits Teil von Molekülen - keine Positionsänderung
                // Die FixedJoints werden die Verbindung herstellen
                if (debugMode)
                {
                    Debug.Log($"SnapAtoms: Beide Atome haben Bindungen - keine Positionsänderung");
                }
                return;
            }
            
            float strength = instant ? 1.0f : snapStrength;
            
            // Bestimme welches Atom bewegt wird (das ohne Bindungen)
            Atom fixedAtom;      // Atom das stehen bleibt
            Atom movingAtom;     // Atom das bewegt wird
            BondPoint fixedBP;   // BondPoint des festen Atoms
            
            if (atomAHasBonds)
            {
                // A hat Bindungen -> A bleibt, B wird bewegt
                fixedAtom = atomA;
                movingAtom = atomB;
                fixedBP = bondPointA;
            }
            else
            {
                // A hat keine Bindungen (oder beide haben keine) -> A bleibt, B wird bewegt
                // (Standard-Verhalten beibehalten für Kompatibilität)
                fixedAtom = atomA;
                movingAtom = atomB;
                fixedBP = bondPointA;
            }
            
            // Falls B Bindungen hat aber A nicht -> A wird bewegt
            if (atomBHasBonds && !atomAHasBonds)
            {
                fixedAtom = atomB;
                movingAtom = atomA;
                fixedBP = bondPointB;
            }
            
            // NUR Position anpassen - KEINE Rotation mehr!
            // Das bewahrt die vom Nutzer festgelegte Ausrichtung
            Vector3 dirFixed = (fixedBP.transform.position - fixedAtom.transform.position).normalized;
            // Verwende die konstante Bond-Länge für konsistente kurze Bindungen
            Vector3 targetPos = fixedAtom.transform.position + dirFixed * Bond.DEFAULT_BOND_LENGTH;
            
            movingAtom.transform.position = Vector3.Lerp(
                movingAtom.transform.position,
                targetPos,
                strength
            );
            
            // ENTFERNT: Keine automatische Rotation mehr!
            // Früher wurde hier die Rotation angepasst, was zu ungewolltem Neuausrichten führte.
            
            if (debugMode)
            {
                Debug.Log($"SnapAtoms: {movingAtom.element} wurde zu {fixedAtom.element} bewegt (nur Position, keine Rotation)");
            }
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
            
            // NEU: Bond greifbar machen für VR
            // Rigidbody hinzufügen (kinematisch)
            Rigidbody bondRb = visual.GetComponent<Rigidbody>();
            if (bondRb == null)
            {
                bondRb = visual.AddComponent<Rigidbody>();
            }
            bondRb.isKinematic = true;
            bondRb.useGravity = false;
            
            // XRGrabInteractable hinzufügen
            var grabInteractable = visual.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = visual.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }
            grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = false;
            
            // BondGrabHandler hinzufügen und initialisieren
            var grabHandler = visual.GetComponent<Interaction.BondGrabHandler>();
            if (grabHandler == null)
            {
                grabHandler = visual.AddComponent<Interaction.BondGrabHandler>();
            }
            grabHandler.Initialize(bond);
            
            bond.Visual = visual;
            bond.UpdateVisual();
            
            if (debugMode)
            {
                Debug.Log($"Bond Visual erstellt zwischen {bond.AtomA.element} und {bond.AtomB.element} (VR-greifbar)");
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
        
        /// <summary>
        /// Gibt die Farbe für einen bestimmten Bindungstyp zurück
        /// </summary>
        public Color GetBondColor(BondType type)
        {
            switch (type)
            {
                case BondType.Double: return doubleBondColor;
                case BondType.Triple: return tripleBondColor;
                default: return singleBondColor;
            }
        }
    }
}
