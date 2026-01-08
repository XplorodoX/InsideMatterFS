using UnityEngine;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Repräsentiert eine chemische Bindung zwischen zwei Atomen.
    /// Enthält sowohl die logische Verbindung als auch die visuelle Darstellung.
    /// Bindungslängen sind FIXIERT und ändern sich nie nach der Erstellung.
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
        
        // FIXE Bindungslänge - wird beim Erstellen gesetzt und ändert sich NIE
        public float FixedBondLength { get; private set; }
        
        // Standard-Bindungslänge falls nicht anders angegeben
        public const float DEFAULT_BOND_LENGTH = 0.7f;
        
        public Bond(Atom atomA, Atom atomB, BondPoint bondPointA, BondPoint bondPointB)
        {
            AtomA = atomA;
            AtomB = atomB;
            BondPointA = bondPointA;
            BondPointB = bondPointB;
            Type = BondType.Single;
            Strength = 1.0f;
            
            // Fixe Bindungslänge beim Erstellen setzen (Standard-Wert)
            FixedBondLength = DEFAULT_BOND_LENGTH;
        }
        
        /// <summary>
        /// Setzt die fixe Bindungslänge (nur einmal beim Erstellen aufrufen!)
        /// </summary>
        public void SetFixedLength(float length)
        {
            FixedBondLength = length;
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
        /// Ändert den Typ der Bindung (Single/Double/Triple)
        /// </summary>
        public void SetType(BondType newType)
        {
            Type = newType;
            UpdateVisual();
        }

        /// <summary>
        /// Aktualisiert die Position der visuellen Darstellung
        /// Verwendet die FIXE Bindungslänge - ändert sich nie!
        /// </summary>
        public void UpdateVisual()
        {
            if (Visual == null || AtomA == null || AtomB == null) return;
            
            // Berechne Mittelpunkt und Richtung zwischen den Atomen
            Vector3 posA = AtomA.transform.position;
            Vector3 posB = AtomB.transform.position;
            Vector3 center = (posA + posB) / 2f;
            Vector3 direction = posB - posA;
            
            // Position & Rotation des Visuals
            Visual.transform.position = center;
            if (direction.magnitude > 0.001f)
            {
                Visual.transform.up = direction.normalized;
            }
            
            // Visual Component abrufen oder hinzufügen
            var visualComp = Visual.GetComponent<BondVisual>();
            if (visualComp == null)
            {
                visualComp = Visual.AddComponent<BondVisual>();
            }
            
            // Material holen
            Material mat = null;
            if (MoleculeManager.Instance != null) mat = MoleculeManager.Instance.bondMaterial;
            
            // Visuals mit FIXER Länge aktualisieren (nicht dynamisch!)
            float thickness = 0.08f;
            if (MoleculeManager.Instance != null) thickness = MoleculeManager.Instance.bondThickness;
            
            // WICHTIG: Verwende FixedBondLength statt der aktuellen Distanz!
            visualComp.UpdateVisuals(Type, FixedBondLength, thickness, mat);
            
            // Collider mit fixer Länge
            UpdateCollider(FixedBondLength, thickness);
        }
        
        private void UpdateCollider(float length, float thickness)
        {
            if (Visual == null) return;
            
            // CapsuleCollider sicherstellen
            var col = Visual.GetComponent<CapsuleCollider>();
            if (col == null) col = Visual.AddComponent<CapsuleCollider>();
            
            col.direction = 1; // Y-Axis
            col.height = length;
            
            // Radius anpassen je nach Typ (damit man auch Doppelbindungen gut trifft)
            float radius = thickness;
            if (Type == BondType.Double) radius = thickness * 2f;
            if (Type == BondType.Triple) radius = thickness * 2.5f;
            
            col.radius = Mathf.Max(radius, 0.15f); // Mindestgröße für einfache Interaktion
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
