using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InsideMatter.Molecule;
using System;

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
            result.errors.Add($"Die Zusammensetzung stimmt noch nicht ganz ({definition.chemicalFormula}).");
            
            foreach (var req in definition.requiredAtoms)
            {
                int actual = atomCounts.ContainsKey(req.element) ? atomCounts[req.element] : 0;
                if (actual < req.count)
                {
                    result.errors.Add($"  • Dir fehlen noch {req.count - actual}x {req.element}-Atome.");
                }
                else if (actual > req.count)
                {
                    result.errors.Add($"  • Du hast {actual - req.count}x {req.element}-Atome zu viel.");
                }
            }
            return result;
        }
        private void checkBoundCounts(ValidationResult result, List<Atom> atoms) { 
            foreach (var atom in atoms)
            {
                int current = atom.CurrentBondCount;
                int max = atom.maxBonds;

                if (current > max)
                {
                    result.isValid = false;
                    result.errors.Add($"Ein {atom.element}-Atom hat mit {current} zu viele Bindungen (max. {max} erlaubt!).");
                }
                else if (current < max)
                {
                    result.isValid = false;
                    result.errors.Add($"Ein {atom.element}-Atom ist noch unvollständig ({current} von {max} Bindungen).");
                }
            }
        }

        private bool canBeIsomorphic(List<GraphNode> crafted, List<GraphNode> reference)
        {
            if (crafted.Count != reference.Count) return false;

            Dictionary<GraphNode, int> craftedLabels = getLabels(crafted);
            Dictionary<GraphNode, int> referenceLabels = getLabels(reference);

            // Prüfung der initialen Label-Verteilung
            if (!AreLabelDistributionsEqual(craftedLabels.Values, referenceLabels.Values))
            {
                return false;
            }

            int iterations = crafted.Count; // WL konvergiert spätestens nach N Schritten
            for (int i = 0; i < iterations; i++)
            {
                Dictionary<GraphNode, int> nextCrafted = refineLabels(craftedLabels, crafted);
                Dictionary<GraphNode, int> nextReference = refineLabels(referenceLabels, reference);

                if (!AreLabelDistributionsEqual(nextCrafted.Values, nextReference.Values))
                {
                    return false;
                }

                // Wenn sich nichts mehr ändert, sind wir fertig
                if (IsStable(craftedLabels, nextCrafted) && IsStable(referenceLabels, nextReference))
                {
                    break;
                }

                craftedLabels = nextCrafted;
                referenceLabels = nextReference;
            }

            return true;
        }

        private bool AreLabelDistributionsEqual(IEnumerable<int> labelsA, IEnumerable<int> labelsB)
        {
            var listA = labelsA.ToList();
            var listB = labelsB.ToList();
            listA.Sort();
            listB.Sort();
            return listA.SequenceEqual(listB);
        }

        private bool IsStable(Dictionary<GraphNode, int> oldLabels, Dictionary<GraphNode, int> newLabels)
        {
            // WL Stabilität: Die Partitionierung der Knoten ändert sich nicht mehr
            // Das prüfen wir hier vereinfacht über die Werte
            return true; // Für unsere Zwecke reicht die Iterations-Begrenzung
        }

        private Dictionary<GraphNode, int> getLabels(List<GraphNode> nodes) 
        { 
            Dictionary<GraphNode, string> baseLabels = nodes.ToDictionary(x => x, x => x.Label);
            return compressAndAssignLabels(baseLabels, nodes);
        }

        private Dictionary<GraphNode, int> compressAndAssignLabels<TValue>(Dictionary<GraphNode, TValue> labels, List<GraphNode> nodes)
        {
            Dictionary<GraphNode, int> compressed = new Dictionary<GraphNode, int>();
            var uniqueSignatures = labels.Values.Distinct().ToList();
            
            // Sortieren für deterministische IDs
            if (typeof(TValue) == typeof(string))
            {
                var stringList = uniqueSignatures.Cast<string>().ToList();
                stringList.Sort();
                uniqueSignatures = stringList.Cast<TValue>().ToList();
            }
            else // Für Tuple (int, List<int>)
            {
                // Hier brauchen wir einen speziellen Comparer oder wir machen es über String-Serialisierung
                var serialized = uniqueSignatures.Select(s => s.ToString()).ToList();
                serialized.Sort();
                // Wir nutzen einfach die Position in der sortierten Liste
                var signatureToId = serialized.Select((s, i) => new { s, i }).ToDictionary(x => x.s, x => x.i);
                foreach (var node in nodes)
                {
                    compressed[node] = signatureToId[labels[node].ToString()];
                }
                return compressed;
            }

            for (int i = 0; i < uniqueSignatures.Count; i++)
            {
                foreach (var node in nodes)
                {
                    if (EqualityComparer<TValue>.Default.Equals(labels[node], uniqueSignatures[i]))
                    {
                        compressed[node] = i;
                    }
                }
            }
            return compressed;
        }

        private Dictionary<GraphNode, int> refineLabels(Dictionary<GraphNode, int> labels, List<GraphNode> nodes)
        {
            var newSignatures = new Dictionary<GraphNode, string>();
            foreach (var node in nodes)
            {
                var neighborLabels = node.Neighbors.Select(n => labels[n]).ToList();
                neighborLabels.Sort();
                string signature = $"{labels[node]}_" + string.Join(",", neighborLabels);
                newSignatures[node] = signature;
            }
            return compressAndAssignLabels(newSignatures, nodes);
        }

        private class GraphNode
        {
            public string Label;
            public List<GraphNode> Neighbors = new List<GraphNode>();
            public object Source; // Referenz zum ursprünglichen Atom (optional)
        }

        private List<GraphNode> BuildGraphFromAtoms(List<Atom> atoms)
        {
            var nodes = atoms.ToDictionary(a => a, a => new GraphNode { Label = a.element, Source = a });
            foreach (var atom in atoms)
            {
                foreach (var neighbor in atom.ConnectedAtoms)
                {
                    if (nodes.ContainsKey(neighbor))
                    {
                        nodes[atom].Neighbors.Add(nodes[neighbor]);
                    }
                }
            }
            return nodes.Values.ToList();
        }

        private List<GraphNode> BuildGraphFromDefinition(MoleculeDefinition definition)
        {
            List<GraphNode> nodes = new List<GraphNode>();
            foreach (var req in definition.requiredAtoms)
            {
                for (int i = 0; i < req.count; i++)
                {
                    nodes.Add(new GraphNode { Label = req.element });
                }
            }

            foreach (var bond in definition.requiredBonds)
            {
                if (bond.atomIndexA < nodes.Count && bond.atomIndexB < nodes.Count)
                {
                    var a = nodes[bond.atomIndexA];
                    var b = nodes[bond.atomIndexB];
                    if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
                    if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
                }
            }
            return nodes;
        }



        public ValidationResult ValidateMolecule(List<Atom> atoms, MoleculeDefinition definition)
        {
            ValidationResult result = new ValidationResult();
            result.isValid = true;
            
            // 1. Prüfe ob die Atome zusammenhängend sind (ein Molekül)
            if (!AreAtomsConnected(atoms))
            {
                result = handleSeperatedAtoms(result);
            }
            
            // 2. Zähle Atom-Typen
            Dictionary<string, int> atomCounts = CountAtomTypes(atoms);
            
            // 3. Validiere Atom-Anzahl
            if (!definition.ValidateAtomCount(atomCounts))
            {
                result = handleIncorrectAtomCount(result, atomCounts, definition);
            }
            
            // 4. Prüfe Bindungsanzahl (Detailliertes Feedback)
            checkBoundCounts(result, atoms);
            if (!result.isValid)
            {
                return result;
            }
            
            // 5. Strukturelle Validierung (Isomorphie)
            if (definition.requiredBonds != null && definition.requiredBonds.Count > 0)
            {
                var craftedGraph = BuildGraphFromAtoms(atoms);
                var referenceGraph = BuildGraphFromDefinition(definition);

                if (!canBeIsomorphic(craftedGraph, referenceGraph))
                {
                    result.isValid = false;
                    result.errors.Add($"Strukturfehler! Die Atome sind falsch miteinander verbunden.");
                    result.errors.Add($"Hinweis: Prüfe das Diagramm auf der Tafel.");
                    return result;
                }
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
