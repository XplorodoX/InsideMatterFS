using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InsideMatter.Molecule;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Validiert ob ein gebautes Molekül einer Definition entspricht.
    /// Prüft Atom-Anzahl, Bindungen und Struktur.
    /// </summary>
    public class MoleculeValidator : MonoBehaviour
    {
        /// <summary>
        /// Validiert eine Gruppe von Atomen gegen eine Molekül-Definition
        /// </summary>
        public ValidationResult ValidateMolecule(List<Atom> atoms, MoleculeDefinition definition)
        {
            ValidationResult result = new ValidationResult();
            result.isValid = true;
            
            // 1. Prüfe ob die Atome zusammenhängend sind (ein Molekül)
            if (!AreAtomsConnected(atoms))
            {
                result.isValid = false;
                result.errors.Add("Die Atome sind nicht alle miteinander verbunden!");
                return result;
            }
            
            // 2. Zähle Atom-Typen
            Dictionary<string, int> atomCounts = CountAtomTypes(atoms);
            
            // 3. Validiere Atom-Anzahl
            if (!definition.ValidateAtomCount(atomCounts))
            {
                result.isValid = false;
                result.errors.Add($"Falsche Atom-Anzahl! Benötigt: {definition.GetAtomListString()}");
                
                // Details zu fehlenden/überzähligen Atomen
                foreach (var req in definition.requiredAtoms)
                {
                    int actual = atomCounts.ContainsKey(req.element) ? atomCounts[req.element] : 0;
                    if (actual != req.count)
                    {
                        result.errors.Add($"  • {req.element}: {actual} statt {req.count}");
                    }
                }
                return result;
            }
            
            // 4. Prüfe Bindungsanzahl (alle Atome sollten korrekt gebunden sein)
            foreach (var atom in atoms)
            {
                if (atom.CurrentBondCount > atom.maxBonds)
                {
                    result.isValid = false;
                    result.errors.Add($"Atom {atom.element} hat zu viele Bindungen!");
                }
            }
            
            // 5. Optional: Validiere genaue Bindungsstruktur
            if (definition.requiredBonds != null && definition.requiredBonds.Count > 0)
            {
                // Erweiterte Validierung (für komplexere Moleküle)
                // TODO: Implementierung für strukturelle Validierung
            }
            
            // Erfolg!
            result.score = definition.scoreReward;
            result.moleculeName = definition.moleculeName;
            
            return result;
        }
        
        /// <summary>
        /// Prüft ob alle Atome miteinander verbunden sind (ein zusammenhängendes Molekül)
        /// </summary>
        private bool AreAtomsConnected(List<Atom> atoms)
        {
            if (atoms == null || atoms.Count == 0) return false;
            if (atoms.Count == 1) return true;
            
            HashSet<Atom> visited = new HashSet<Atom>();
            Queue<Atom> queue = new Queue<Atom>();
            
            // Starte bei erstem Atom
            queue.Enqueue(atoms[0]);
            visited.Add(atoms[0]);
            
            // Breadth-First Search
            while (queue.Count > 0)
            {
                Atom current = queue.Dequeue();
                
                // Besuche alle verbundenen Atome
                foreach (var connectedAtom in current.ConnectedAtoms)
                {
                    if (!visited.Contains(connectedAtom) && atoms.Contains(connectedAtom))
                    {
                        visited.Add(connectedAtom);
                        queue.Enqueue(connectedAtom);
                    }
                }
            }
            
            // Alle Atome besucht?
            return visited.Count == atoms.Count;
        }
        
        /// <summary>
        /// Zählt die Anzahl jedes Atom-Typs
        /// </summary>
        private Dictionary<string, int> CountAtomTypes(List<Atom> atoms)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            
            foreach (var atom in atoms)
            {
                if (counts.ContainsKey(atom.element))
                    counts[atom.element]++;
                else
                    counts[atom.element] = 1;
            }
            
            return counts;
        }
        
        /// <summary>
        /// Findet zusammenhängende Moleküle in einer Liste von Atomen
        /// </summary>
        public List<List<Atom>> FindMolecules(List<Atom> allAtoms)
        {
            List<List<Atom>> molecules = new List<List<Atom>>();
            HashSet<Atom> processed = new HashSet<Atom>();
            
            foreach (var atom in allAtoms)
            {
                if (processed.Contains(atom)) continue;
                
                // Finde alle verbundenen Atome (ein Molekül)
                List<Atom> molecule = new List<Atom>();
                Queue<Atom> queue = new Queue<Atom>();
                
                queue.Enqueue(atom);
                processed.Add(atom);
                molecule.Add(atom);
                
                while (queue.Count > 0)
                {
                    Atom current = queue.Dequeue();
                    
                    foreach (var connected in current.ConnectedAtoms)
                    {
                        if (!processed.Contains(connected))
                        {
                            processed.Add(connected);
                            molecule.Add(connected);
                            queue.Enqueue(connected);
                        }
                    }
                }
                
                molecules.Add(molecule);
            }
            
            return molecules;
        }
    }
    
    /// <summary>
    /// Ergebnis einer Molekül-Validierung
    /// </summary>
    public class ValidationResult
    {
        public bool isValid = false;
        public List<string> errors = new List<string>();
        public int score = 0;
        public string moleculeName = "";
        
        public string GetErrorMessage()
        {
            if (errors.Count == 0) return "";
            return string.Join("\n", errors);
        }
    }
}
