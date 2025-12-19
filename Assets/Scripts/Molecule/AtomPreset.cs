using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Vordefinierte Atom-Konfigurationen für häufige Elemente.
    /// Nutzt CPK-Farbschema und realistische Atomradien.
    /// </summary>
    [CreateAssetMenu(fileName = "AtomPreset", menuName = "InsideMatter/Atom Preset")]
    public class AtomPreset : ScriptableObject
    {
        [Header("Chemische Eigenschaften")]
        public string elementSymbol = "C";
        public string elementName = "Carbon";
        public int atomicNumber = 6;
        public int maxBonds = 4;
        
        [Header("Visuelle Eigenschaften")]
        public Color cpkColor = Color.black;
        public float atomRadius = 0.5f;
        
        [Header("BondPoint-Konfiguration")]
        [Tooltip("Geometrie der BondPoints")]
        public BondGeometry bondGeometry = BondGeometry.Tetrahedral;
        
        [Tooltip("Abstand der BondPoints vom Atom-Zentrum")]
        public float bondPointDistance = 0.5f;
        
        /// <summary>
        /// Wendet dieses Preset auf ein Atom an
        /// </summary>
        public void ApplyToAtom(Atom atom)
        {
            if (atom == null) return;
            
            atom.element = elementSymbol;
            atom.maxBonds = maxBonds;
            atom.atomColor = cpkColor;
            atom.atomRadius = atomRadius;
            
            // BondPoints generieren
            int bondPointCount = GetBondPointCount();
            BondPointGenerator.GenerateBondPoints(atom.transform, bondPointCount, bondPointDistance);
            
            // Visual aktualisieren
            atom.ApplyVisuals();
            
            // BondPoints neu sammeln
            atom.bondPoints.Clear();
            BondPoint[] points = atom.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);
            
            // Parent-Referenzen setzen
            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }
        }
        
        private int GetBondPointCount()
        {
            switch (bondGeometry)
            {
                case BondGeometry.Linear1: return 1;
                case BondGeometry.Linear2: return 2;
                case BondGeometry.TrigonalPlanar: return 3;
                case BondGeometry.Tetrahedral: return 4;
                case BondGeometry.TrigonalBipyramidal: return 5;
                case BondGeometry.Octahedral: return 6;
                default: return maxBonds;
            }
        }
        
        #region Static Presets
        
        // CPK-Farbschema (Corey-Pauling-Koltun)
        public static readonly Color CPK_Hydrogen = Color.white;
        public static readonly Color CPK_Carbon = Color.black;
        public static readonly Color CPK_Nitrogen = new Color(0.2f, 0.2f, 1f); // Blau
        public static readonly Color CPK_Oxygen = Color.red;
        public static readonly Color CPK_Sulfur = Color.yellow;
        public static readonly Color CPK_Phosphorus = new Color(1f, 0.5f, 0f); // Orange
        
        /// <summary>
        /// Erstellt ein Wasserstoff-Preset
        /// </summary>
        public static AtomPreset CreateHydrogenPreset()
        {
            var preset = CreateInstance<AtomPreset>();
            preset.elementSymbol = "H";
            preset.elementName = "Hydrogen";
            preset.atomicNumber = 1;
            preset.maxBonds = 1;
            preset.cpkColor = CPK_Hydrogen;
            preset.atomRadius = 0.3f;
            preset.bondGeometry = BondGeometry.Linear1;
            preset.bondPointDistance = 0.5f; // Local distance 0.5 is on the surface
            return preset;
        }
        
        /// <summary>
        /// Erstellt ein Kohlenstoff-Preset
        /// </summary>
        public static AtomPreset CreateCarbonPreset()
        {
            var preset = CreateInstance<AtomPreset>();
            preset.elementSymbol = "C";
            preset.elementName = "Carbon";
            preset.atomicNumber = 6;
            preset.maxBonds = 4;
            preset.cpkColor = CPK_Carbon;
            preset.atomRadius = 0.5f;
            preset.bondGeometry = BondGeometry.Tetrahedral;
            preset.bondPointDistance = 0.5f; // Local distance 0.5 is on the surface
            return preset;
        }
        
        /// <summary>
        /// Erstellt ein Stickstoff-Preset
        /// </summary>
        public static AtomPreset CreateNitrogenPreset()
        {
            var preset = CreateInstance<AtomPreset>();
            preset.elementSymbol = "N";
            preset.elementName = "Nitrogen";
            preset.atomicNumber = 7;
            preset.maxBonds = 3;
            preset.cpkColor = CPK_Nitrogen;
            preset.atomRadius = 0.45f;
            preset.bondGeometry = BondGeometry.TrigonalPlanar;
            preset.bondPointDistance = 0.5f; // Local distance 0.5 is on the surface
            return preset;
        }
        
        /// <summary>
        /// Erstellt ein Sauerstoff-Preset
        /// </summary>
        public static AtomPreset CreateOxygenPreset()
        {
            var preset = CreateInstance<AtomPreset>();
            preset.elementSymbol = "O";
            preset.elementName = "Oxygen";
            preset.atomicNumber = 8;
            preset.maxBonds = 2;
            preset.cpkColor = CPK_Oxygen;
            preset.atomRadius = 0.4f;
            preset.bondGeometry = BondGeometry.Linear2;
            preset.bondPointDistance = 0.5f; // Local distance 0.5 is on the surface
            return preset;
        }
        
        #endregion
    }
    
    public enum BondGeometry
    {
        Linear1,              // 1 BondPoint (H)
        Linear2,              // 2 BondPoints, 180° (O)
        TrigonalPlanar,       // 3 BondPoints, 120° (N, B)
        Tetrahedral,          // 4 BondPoints, 109.5° (C)
        TrigonalBipyramidal,  // 5 BondPoints (P)
        Octahedral            // 6 BondPoints (S)
    }
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// Editor-Tool zum schnellen Erstellen von Atom-Presets
    /// </summary>
    [CustomEditor(typeof(AtomPreset))]
    public class AtomPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            AtomPreset preset = (AtomPreset)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Apply to Selected Atom"))
            {
                if (Selection.activeGameObject != null)
                {
                    Atom atom = Selection.activeGameObject.GetComponent<Atom>();
                    if (atom != null)
                    {
                        preset.ApplyToAtom(atom);
                        EditorUtility.SetDirty(atom);
                        UnityEngine.Debug.Log($"Preset '{preset.elementName}' auf {atom.gameObject.name} angewendet");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Selektiertes GameObject hat keine Atom-Komponente!");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Kein GameObject selektiert!");
                }
            }
        }
    }
    
    #endif
}
