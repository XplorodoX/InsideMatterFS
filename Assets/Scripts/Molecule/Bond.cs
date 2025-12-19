using UnityEngine;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Repräsentiert eine chemische Bindung zwischen zwei Atomen.
    /// Enthält sowohl die logische Verbindung als auch die visuelle Darstellung.
    /// </summary>
    public class Bond
    {
        // Die beiden verbundenen Atome
        public Atom AtomA { get; private set; }
        public Atom AtomB { get; private set; }
        
        // Die beiden BondPoints, die verbunden wurden
        public BondPoint BondPointA { get; private set; }
        public BondPoint BondPointB { get; private set; }
        
        // Visuelle Darstellung der Bindung
        public GameObject Visual { get; set; }
        
        // Physikalische Verbindung
        public FixedJoint Joint { get; set; }

        // Bindungstyp (für spätere Erweiterungen: Einfach-, Doppel-, Dreifachbindung)
        public BondType Type { get; set; }
        
        // Bindungsstärke (für Physik/Gameplay)
        public float Strength { get; set; }
        
        public Bond(Atom atomA, Atom atomB, BondPoint bondPointA, BondPoint bondPointB)
        {
            AtomA = atomA;
            AtomB = atomB;
            BondPointA = bondPointA;
            BondPointB = bondPointB;
            Type = BondType.Single;
            Strength = 1.0f;
        }
        
        /// <summary>
        /// Prüft ob ein bestimmtes Atom an dieser Bindung beteiligt ist
        /// </summary>
        public bool ContainsAtom(Atom atom)
        {
            return AtomA == atom || AtomB == atom;
        }
        
        /// <summary>
        /// Gibt das andere Atom der Bindung zurück
        /// </summary>
        public Atom GetOtherAtom(Atom atom)
        {
            if (atom == AtomA) return AtomB;
            if (atom == AtomB) return AtomA;
            return null;
        }
        
        /// <summary>
        /// Aktualisiert die Position der visuellen Darstellung
        /// </summary>
        public void UpdateVisual()
        {
            if (Visual == null || BondPointA == null || BondPointB == null) return;
            
            Vector3 posA = BondPointA.transform.position;
            Vector3 posB = BondPointB.transform.position;
            
            // Position in der Mitte zwischen den beiden BondPoints
            Visual.transform.position = (posA + posB) / 2f;
            
            // Rotation zur Verbindung der Punkte
            Vector3 direction = posB - posA;
            if (direction.magnitude > 0.001f)
            {
                // Zylinder zeigt standardmäßig entlang Y-Achse
                Visual.transform.up = direction.normalized;
            }
            
            // Skalierung basierend auf Abstand
            float distance = direction.magnitude;
            
            // Standard Cylinder ist 2 Einheiten hoch (y = -1 bis 1) oder 1 Einheit? 
            // Unity Default Cylinder ist 2 Units hoch. Scale Y = distance / 2.
            Visual.transform.localScale = new Vector3(
                Visual.transform.localScale.x, // Behalte Dicke bei (wird im Manager oft gesetzt)
                distance / 2f,
                Visual.transform.localScale.z  // Behalte Dicke bei
            );
        }
    }
    
    /// <summary>
    /// Typ der chemischen Bindung
    /// </summary>
    public enum BondType
    {
        Single,   // Einfachbindung
        Double,   // Doppelbindung
        Triple,   // Dreifachbindung
        Aromatic  // Aromatische Bindung
    }
}
