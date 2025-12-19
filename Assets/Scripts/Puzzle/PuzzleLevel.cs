using System.Collections.Generic;
using UnityEngine;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Ein Puzzle-Level mit mehreren Molekül-Aufgaben.
    /// Kann sequentiell oder parallel abgearbeitet werden.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "InsideMatter/Puzzle Level")]
    public class PuzzleLevel : ScriptableObject
    {
        [Header("Level-Info")]
        [Tooltip("Name des Levels")]
        public string levelName = "Level 1: Einfache Moleküle";
        
        [Tooltip("Beschreibung des Levels")]
        [TextArea(2, 4)]
        public string levelDescription = "Lerne die Grundlagen der Molekülbildung.";
        
        [Tooltip("Level-Nummer")]
        public int levelNumber = 1;
        
        [Header("Aufgaben")]
        [Tooltip("Moleküle die in diesem Level gebaut werden müssen")]
        public List<MoleculeDefinition> moleculesToBuild = new List<MoleculeDefinition>();
        
        [Tooltip("Müssen die Moleküle in Reihenfolge gebaut werden?")]
        public bool sequentialOrder = true;
        
        [Header("Verfügbare Atome")]
        [Tooltip("Welche Atome stehen dem Spieler zur Verfügung?")]
        public List<AtomRequirement> availableAtoms = new List<AtomRequirement>();
        
        [Header("Level-Einstellungen")]
        [Tooltip("Zeitlimit für das gesamte Level (0 = kein Limit)")]
        public float totalTimeLimit = 0f;
        
        [Tooltip("Maximale Punkte für dieses Level")]
        public int maxScore = 300;
        
        [Header("Freischaltung")]
        [Tooltip("Welches Level muss abgeschlossen sein?")]
        public PuzzleLevel requiredLevel;
        
        [Tooltip("Mindestpunktzahl um das nächste Level freizuschalten")]
        public int requiredScore = 0;
        
        /// <summary>
        /// Gibt die Gesamtanzahl der benötigten Atome zurück
        /// </summary>
        public Dictionary<string, int> GetTotalRequiredAtoms()
        {
            Dictionary<string, int> total = new Dictionary<string, int>();
            
            foreach (var molecule in moleculesToBuild)
            {
                foreach (var atom in molecule.requiredAtoms)
                {
                    if (total.ContainsKey(atom.element))
                        total[atom.element] += atom.count;
                    else
                        total[atom.element] = atom.count;
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// Prüft ob der Spieler genug Atome zur Verfügung hat
        /// </summary>
        public bool HasSufficientAtoms()
        {
            var required = GetTotalRequiredAtoms();
            
            foreach (var req in required)
            {
                var available = availableAtoms.Find(a => a.element == req.Key);
                if (available == null || available.count < req.Value)
                    return false;
            }
            
            return true;
        }
    }
}
