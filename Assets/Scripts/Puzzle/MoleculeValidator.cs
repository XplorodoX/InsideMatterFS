using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InsideMatter.Molecule;
using UnityEditor.Rendering;
using System;
using JetBrains.Annotations;
using Unity.Collections;
using System.Linq.Expressions;
using TMPro;
using Mono.Cecil.Cil;

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
        /// 
        private ValidationResult handleSeperatedAtoms(ValidationResult result)
        {
            result.isValid = false;
            result.errors.Add("Die Atome sind nicht alle miteinander verbunden!");
            return result;
        }
        private ValidationResult handleIncorrectAtomCount(ValidationResult result, 
            Dictionary<string, int> atomCounts, MoleculeDefinition definition) {
            result.isValid = false;
            result.errors.Add($"Falsche Atom-Anzahl! Benötigt: {definition.GetAtomListString()}");
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
        private void checkBoundCounts(ValidationResult result, List<Atom> atoms) { 
            foreach (var atom in atoms)
            {
                if (atom.CurrentBondCount > atom.maxBonds)
                {
                    result.isValid = false;
                    result.errors.Add($"Atom {atom.element} hat zu viele Bindungen!");
                }
            }
        }

        private bool canBeIsomorphic(List<Atom> crafted, List<Atom> reference)
        {
            Dictionary<Atom, int> craftedLabels = getLabels(crafted);
            Dictionary<Atom, int> referenceLabels = getLabels(reference);
            if (!craftedLabels.Values.Equals(referenceLabels.Values)){
                return false;
            }
            while (true)
            {
                Dictionary<Atom, int> refinedCratedLabels = refineLabels(craftedLabels, crafted);
                Dictionary<Atom, int> refinedReferenceLabels = refineLabels(referenceLabels, reference);
                if (!refinedCratedLabels.Values.Equals(refinedReferenceLabels.Values))
                {
                    return false;
                }
                if (craftedLabels.Equals(refinedCratedLabels))
                {
                    break;
                }
            }
            return true;
        }

        private Dictionary<Atom, int> getLabels(List<Atom> atoms) 
        { 
            Dictionary<Atom, string> atom_labels = atoms.ToDictionary(x => x, x => x.element);
            return compressAndAssignLabels(atom_labels, atoms);
        }

        private Dictionary<Atom, int> compressAndAssignLabels<Value>(Dictionary<Atom, Value> labels, List<Atom> atoms)
        {
            Dictionary<Atom, int> compressedLabels = new Dictionary<Atom, int>();
            List<Value> unique_signatures = uniqueList<Atom, Value>(labels);
            var signature_to_id = unique_signatures.Select((signature, index) => new {signature, index}).ToDictionary(x => x.signature, x => x.index);
            foreach (var atom in atoms)
            {
                compressedLabels[atom] = signature_to_id[labels[atom]];
            }   
            return compressedLabels;
        }

        private List<Value> uniqueList<Key, Value>(Dictionary<Key, Value> dict)
        {
            var result = new HashSet<Value>(dict.Values).ToList();
            result.Sort();
            return result;    
        }

        private Dictionary<Atom, int> refineLabels(Dictionary<Atom, int> labels, List<Atom> atoms)
        {
            var newLabels = computeNewLabels(labels, atoms);
            return compressAndAssignLabels(newLabels, atoms);
        }
        
        private Dictionary<Atom, (int, List<int>)> computeNewLabels(Dictionary<Atom, int> oldLabels, List<Atom> atoms)
        {
            var newLabels = new Dictionary<Atom, (int, List<int>)>();
            foreach (var atom in atoms)
            {
                List<int> successorLabels = new List<int>();
                foreach (var successor in atom.ConnectedAtoms)
                {
                    successorLabels.Append(oldLabels[successor]);   
                }
                var newLabel = (oldLabels[atom], successorLabels);
                newLabels.Add(atom, newLabel);
            }
            return newLabels;
        }



        public ValidationResult ValidateMolecule(List<Atom> atoms, MoleculeDefinition definition)
        {
            ValidationResult result = new ValidationResult();
            result.isValid = true;
            
            // 1. Prüfe ob die Atome zusammenhängend sind (ein Molekül)
            if (!AreAtomsConnected(atoms))
            {
                handleSeperatedAtoms(result);
            }
            
            // 2. Zähle Atom-Typen
            Dictionary<string, int> atomCounts = CountAtomTypes(atoms);
            
            // 3. Validiere Atom-Anzahl
            if (!definition.ValidateAtomCount(atomCounts))
            {
                handleIncorrectAtomCount(result, atomCounts, definition);
            }
            
            // 4. Prüfe Bindungsanzahl (alle Atome sollten korrekt gebunden sein)
            checkBoundCounts(result, atoms);
            if (!result.isValid)
            {
                return result;
            }
            
            // 5. Optional: Validiere genaue Bindungsstruktur
            if (definition.requiredBonds != null && definition.requiredBonds.Count > 0)
            {
                if(!canBeIsomorphic(atoms, atoms))
                {
                    result.isValid = false;
                    result.errors.Add($"Moleküle haben eine unterschiedliche Struktur!");
                    return result;
                }
                // Erweiterte Validierung (für komplexere Moleküle)
                // TODO: Implementierung für strukturelle Validierung
                // Color-Refinement
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
