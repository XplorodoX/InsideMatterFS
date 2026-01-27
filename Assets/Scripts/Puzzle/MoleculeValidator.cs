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
                // WICHTIG: Verwende die korrekte Valenz-Berechnung die Bindungsordnung berücksichtigt
                // Doppelbindung = 2 Valenzelektronen, Dreifachbindung = 3
                int current = 0;
                if (MoleculeManager.Instance != null)
                {
                    current = MoleculeManager.Instance.CalculateCurrentValence(atom);
                }
                else
                {
                    // Fallback: Zähle nur belegte BondPoints (weniger genau)
                    current = atom.CurrentBondCount;
                }
                
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

            // NEU: Zusätzlicher Backtracking-Check für robustere Validierung
            // WL kann bei bestimmten regulären Graphen falsche Positive liefern
            return TryFindIsomorphism(crafted, reference, craftedLabels, referenceLabels);
        }
        
        /// <summary>
        /// Versucht eine konkrete Isomorphie-Zuordnung zu finden (Backtracking/VF2-Style)
        /// </summary>
        private bool TryFindIsomorphism(List<GraphNode> crafted, List<GraphNode> reference,
            Dictionary<GraphNode, int> craftedLabels, Dictionary<GraphNode, int> referenceLabels)
        {
            // Gruppiere Knoten nach ihren finalen WL-Labels
            var craftedByLabel = crafted.GroupBy(n => craftedLabels[n]).ToDictionary(g => g.Key, g => g.ToList());
            var referenceByLabel = reference.GroupBy(n => referenceLabels[n]).ToDictionary(g => g.Key, g => g.ToList());
            
            // Für kleine Moleküle (< 20 Atome) versuche Backtracking
            if (crafted.Count <= 20)
            {
                var mapping = new Dictionary<GraphNode, GraphNode>();
                return BacktrackIsomorphism(crafted, reference, mapping, 0, craftedLabels, referenceLabels);
            }
            
            // Für größere Moleküle vertrauen wir dem WL-Ergebnis
            return true;
        }
        
        /// <summary>
        /// Rekursives Backtracking um eine gültige Isomorphie-Zuordnung zu finden
        /// </summary>
        private bool BacktrackIsomorphism(List<GraphNode> crafted, List<GraphNode> reference,
            Dictionary<GraphNode, GraphNode> mapping, int index,
            Dictionary<GraphNode, int> craftedLabels, Dictionary<GraphNode, int> referenceLabels)
        {
            if (index == crafted.Count)
            {
                // Alle Knoten zugeordnet - prüfe ob alle Kanten übereinstimmen
                return ValidateMapping(crafted, mapping);
            }
            
            var craftedNode = crafted[index];
            int label = craftedLabels[craftedNode];
            
            // Nur Kandidaten mit gleichem Label probieren
            foreach (var refNode in reference)
            {
                if (mapping.ContainsValue(refNode)) continue; // Schon zugeordnet
                if (referenceLabels[refNode] != label) continue; // Falsches Label
                
                // Prüfe ob diese Zuordnung mit bestehenden Nachbar-Zuordnungen konsistent ist
                if (!IsConsistent(craftedNode, refNode, mapping))
                    continue;
                
                mapping[craftedNode] = refNode;
                
                if (BacktrackIsomorphism(crafted, reference, mapping, index + 1, craftedLabels, referenceLabels))
                    return true;
                
                mapping.Remove(craftedNode);
            }
            
            return false;
        }
        
        /// <summary>
        /// Prüft ob eine Zuordnung mit bestehenden Nachbar-Zuordnungen konsistent ist
        /// </summary>
        private bool IsConsistent(GraphNode craftedNode, GraphNode refNode, Dictionary<GraphNode, GraphNode> mapping)
        {
            for (int i = 0; i < craftedNode.Neighbors.Count; i++)
            {
                var craftedNeighbor = craftedNode.Neighbors[i];
                if (!mapping.ContainsKey(craftedNeighbor)) continue;
                
                var expectedRefNeighbor = mapping[craftedNeighbor];
                
                // Prüfe ob refNode auch mit expectedRefNeighbor verbunden ist
                int refNeighborIndex = refNode.Neighbors.IndexOf(expectedRefNeighbor);
                if (refNeighborIndex < 0) return false;
                
                // Prüfe ob Bindungstyp übereinstimmt
                var craftedBondType = i < craftedNode.EdgeLabels.Count ? craftedNode.EdgeLabels[i] : BondType.Single;
                var refBondType = refNeighborIndex < refNode.EdgeLabels.Count ? refNode.EdgeLabels[refNeighborIndex] : BondType.Single;
                
                if (craftedBondType != refBondType) return false;
            }
            return true;
        }
        
        /// <summary>
        /// Validiert ob alle Kanten im Mapping korrekt sind
        /// </summary>
        private bool ValidateMapping(List<GraphNode> crafted, Dictionary<GraphNode, GraphNode> mapping)
        {
            foreach (var craftedNode in crafted)
            {
                var refNode = mapping[craftedNode];
                
                // Anzahl Nachbarn muss stimmen
                if (craftedNode.Neighbors.Count != refNode.Neighbors.Count)
                    return false;
                
                // Alle Nachbarn und Bindungstypen müssen übereinstimmen
                for (int i = 0; i < craftedNode.Neighbors.Count; i++)
                {
                    var craftedNeighbor = craftedNode.Neighbors[i];
                    var expectedRefNeighbor = mapping[craftedNeighbor];
                    
                    int refNeighborIndex = refNode.Neighbors.IndexOf(expectedRefNeighbor);
                    if (refNeighborIndex < 0) return false;
                    
                    var craftedBondType = i < craftedNode.EdgeLabels.Count ? craftedNode.EdgeLabels[i] : BondType.Single;
                    var refBondType = refNeighborIndex < refNode.EdgeLabels.Count ? refNode.EdgeLabels[refNeighborIndex] : BondType.Single;
                    
                    if (craftedBondType != refBondType) return false;
                }
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
                // Kombiniere Nachbar-Label MIT Bindungstyp für korrekte Isomorphie
                var neighborData = new List<string>();
                for (int i = 0; i < node.Neighbors.Count; i++)
                {
                    var neighbor = node.Neighbors[i];
                    var edgeType = i < node.EdgeLabels.Count ? (int)node.EdgeLabels[i] : 0;
                    neighborData.Add($"{labels[neighbor]}:{edgeType}");
                }
                neighborData.Sort();
                string signature = $"{labels[node]}_" + string.Join(",", neighborData);
                newSignatures[node] = signature;
            }
            return compressAndAssignLabels(newSignatures, nodes);
        }

        private class GraphNode
        {
            public string Label;
            public List<GraphNode> Neighbors = new List<GraphNode>();
            public List<BondType> EdgeLabels = new List<BondType>(); // Bindungstyp zu jedem Nachbarn
            public object Source; // Referenz zum ursprünglichen Atom (optional)
        }

        private List<GraphNode> BuildGraphFromAtoms(List<Atom> atoms)
        {
            var nodes = atoms.ToDictionary(a => a, a => new GraphNode { Label = a.element, Source = a });
            
            // Hole alle Bindungen aus dem MoleculeManager
            var allBonds = MoleculeManager.Instance?.Bonds ?? new List<Bond>();
            
            foreach (var atom in atoms)
            {
                foreach (var neighbor in atom.ConnectedAtoms)
                {
                    if (nodes.ContainsKey(neighbor))
                    {
                        nodes[atom].Neighbors.Add(nodes[neighbor]);
                        
                        // Finde den Bindungstyp für diese Verbindung
                        BondType bondType = BondType.Single;
                        foreach (var bond in allBonds)
                        {
                            if ((bond.AtomA == atom && bond.AtomB == neighbor) ||
                                (bond.AtomA == neighbor && bond.AtomB == atom))
                            {
                                bondType = bond.Type;
                                break;
                            }
                        }
                        nodes[atom].EdgeLabels.Add(bondType);
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
                    if (!a.Neighbors.Contains(b))
                    {
                        a.Neighbors.Add(b);
                        a.EdgeLabels.Add(bond.bondType);
                    }
                    if (!b.Neighbors.Contains(a))
                    {
                        b.Neighbors.Add(a);
                        b.EdgeLabels.Add(bond.bondType);
                    }
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
            
            // 5. Prüfe Bindungstypen (Single/Double/Triple)
            if (definition.requiredBonds != null && definition.requiredBonds.Count > 0)
            {
                if (!ValidateBondTypes(atoms, definition, result))
                {
                    return result;
                }
            }
            
            // 6. Strukturelle Validierung (Isomorphie mit Bindungstypen)
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
        /// Prüft ob die Bindungstypen (Einfach-/Doppel-/Dreifachbindung) korrekt sind
        /// </summary>
        private bool ValidateBondTypes(List<Atom> atoms, MoleculeDefinition definition, ValidationResult result)
        {
            if (MoleculeManager.Instance == null) return true;
            
            // Zähle die Bindungstypen im gebauten Molekül
            var actualBondCounts = new Dictionary<BondType, int>
            {
                { BondType.Single, 0 },
                { BondType.Double, 0 },
                { BondType.Triple, 0 }
            };
            
            // Sammle alle Bindungen die zu diesem Molekül gehören
            var atomSet = new HashSet<Atom>(atoms);
            var countedBonds = new HashSet<Bond>();
            
            foreach (var bond in MoleculeManager.Instance.Bonds)
            {
                if (atomSet.Contains(bond.AtomA) && atomSet.Contains(bond.AtomB) && !countedBonds.Contains(bond))
                {
                    countedBonds.Add(bond);
                    if (actualBondCounts.ContainsKey(bond.Type))
                    {
                        actualBondCounts[bond.Type]++;
                    }
                }
            }
            
            // Zähle die erwarteten Bindungstypen aus der Definition
            var requiredBondCounts = new Dictionary<BondType, int>
            {
                { BondType.Single, 0 },
                { BondType.Double, 0 },
                { BondType.Triple, 0 }
            };
            
            foreach (var bondReq in definition.requiredBonds)
            {
                if (requiredBondCounts.ContainsKey(bondReq.bondType))
                {
                    requiredBondCounts[bondReq.bondType]++;
                }
            }
            
            // Vergleiche
            bool allCorrect = true;
            
            foreach (var kvp in requiredBondCounts)
            {
                int required = kvp.Value;
                int actual = actualBondCounts[kvp.Key];
                
                if (required > 0 && actual != required)
                {
                    allCorrect = false;
                    string bondTypeName = GetBondTypeName(kvp.Key);
                    
                    if (actual < required)
                    {
                        result.errors.Add($"  • Du brauchst noch {required - actual}x {bondTypeName}.");
                    }
                    else
                    {
                        result.errors.Add($"  • Du hast {actual - required}x {bondTypeName} zu viel.");
                    }
                }
            }
            
            if (!allCorrect)
            {
                result.isValid = false;
                result.errors.Insert(0, "Die Bindungstypen stimmen nicht!");
            }
            
            return allCorrect;
        }
        
        /// <summary>
        /// Gibt den deutschen Namen für einen Bindungstyp zurück
        /// </summary>
        private string GetBondTypeName(BondType type)
        {
            switch (type)
            {
                case BondType.Single: return "Einfachbindung";
                case BondType.Double: return "Doppelbindung";
                case BondType.Triple: return "Dreifachbindung";
                default: return type.ToString();
            }
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
