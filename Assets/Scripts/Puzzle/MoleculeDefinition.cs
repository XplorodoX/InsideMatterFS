using System;
using System.Collections.Generic;
using InsideMatter.Molecule;
using UnityEngine;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Definition eines Zielmoleküls für ein Puzzle/Level.
    /// Beschreibt welche Atome benötigt werden und wie sie verbunden sein müssen.
    /// </summary>
    [CreateAssetMenu(fileName = "Molecule", menuName = "InsideMatter/Molecule Definition")]
    public class MoleculeDefinition : ScriptableObject
    {
        [Header("Molekül-Info")]
        [Tooltip("Name des Moleküls (z.B. 'Wasser', 'Methan')")]
        public string moleculeName = "Wasser";
        
        [Tooltip("Chemische Formel (z.B. 'H₂O', 'CH₄')")]
        public string chemicalFormula = "H₂O";
        
        [Tooltip("Beschreibung für den Spieler")]
        [TextArea(3, 5)]
        public string description = "Wasser besteht aus zwei Wasserstoff-Atomen und einem Sauerstoff-Atom.";
        
        [Header("Benötigte Atome")]
        [Tooltip("Liste der benötigten Atom-Typen mit Anzahl")]
        public List<AtomRequirement> requiredAtoms = new List<AtomRequirement>();
        
        [Header("Bindungsstruktur")]
        [Tooltip("Erwartete Bindungen (optional für genaue Validierung)")]
        public List<BondRequirement> requiredBonds = new List<BondRequirement>();
        
        [Header("Schwierigkeit")]
        [Tooltip("Schwierigkeitsgrad des Puzzles")]
        [Range(1, 5)]
        public int difficulty = 1;
        
        [Tooltip("Zeitlimit in Sekunden (0 = kein Limit)")]
        public float timeLimit = 0f;
        
        [Header("Belohnung")]
        [Tooltip("Punkte bei erfolgreichem Abschluss")]
        public int scoreReward = 100;
        
        [Header("Visuals")]
        [Tooltip("Icon des Moleküls für UI")]
        public Sprite moleculeIcon;
        
        [Tooltip("3D-Modell Vorschau (optional)")]
        public GameObject moleculePrefab;
        
        /// <summary>
        /// Prüft ob die angegebenen Atome die Anforderungen erfüllen
        /// </summary>
        public bool ValidateAtomCount(Dictionary<string, int> atomCounts)
        {
            foreach (var req in requiredAtoms)
            {
                if (!atomCounts.ContainsKey(req.element) || atomCounts[req.element] != req.count)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Gibt eine lesbare Liste der benötigten Atome zurück
        /// </summary>
        public string GetAtomListString()
        {
            string result = "";
            foreach (var req in requiredAtoms)
            {
                result += $"{req.count}× {req.element}  ";
            }
            return result.TrimEnd();
        }
    }
    
    /// <summary>
    /// Anforderung für einen Atom-Typ
    /// </summary>
    [System.Serializable]
    public class AtomRequirement
    {
        [Tooltip("Element-Symbol (H, C, O, N, etc.)")]
        public string element = "H";
        
        [Tooltip("Benötigte Anzahl")]
        public int count = 1;
    }
    
    /// <summary>
    /// Anforderung für eine Bindung (optional für erweiterte Validierung)
    /// </summary>
    [System.Serializable]
    public class BondRequirement
    {
        [Tooltip("Erstes Atom (Index in der Reihenfolge)")]
        // Index des Atomtyps innerhalb der Liste
        public int atomIndexA = 0;
        
        [Tooltip("Zweites Atom (Index in der Reihenfolge)")]
        // Index des Atomtyps innerhalb der Liste
        public int atomIndexB = 1;
        
        [Tooltip("Bindungstyp")]
        public Molecule.BondType bondType = Molecule.BondType.Single;
    }
}
