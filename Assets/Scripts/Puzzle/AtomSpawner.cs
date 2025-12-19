using System.Collections.Generic;
using UnityEngine;
using InsideMatter.Molecule;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Spawnt Atome für ein Puzzle-Level.
    /// Verwaltet die verfügbaren Atome und ihre Positionen.
    /// </summary>
    public class AtomSpawner : MonoBehaviour
    {
        [Header("Spawn-Einstellungen")]
        [Tooltip("Spawn-Position (Zentrum)")]
        public Transform spawnCenter;
        
        [Tooltip("Spawn-Bereich")]
        public Vector3 spawnAreaSize = new Vector3(5f, 0f, 3f);
        
        [Tooltip("Anordnung der Atome")]
        public SpawnPattern spawnPattern = SpawnPattern.Grid;
        
        [Tooltip("Abstand zwischen Atomen")]
        public float atomSpacing = 1.5f;
        
        [Header("Atom-Prefabs")]
        [Tooltip("Hydrogen Prefab")]
        public GameObject hydrogenPrefab;
        
        [Tooltip("Carbon Prefab")]
        public GameObject carbonPrefab;
        
        [Tooltip("Oxygen Prefab")]
        public GameObject oxygenPrefab;
        
        [Tooltip("Nitrogen Prefab")]
        public GameObject nitrogenPrefab;
        
        [Header("Container")]
        [Tooltip("Parent für gespawnte Atome")]
        private Transform atomContainer;
        
        private List<GameObject> spawnedAtoms = new List<GameObject>();
        
        void Awake()
        {
            if (spawnCenter == null)
            {
                spawnCenter = transform;
            }
            
            // Container erstellen
            GameObject container = new GameObject("SpawnedAtoms");
            atomContainer = container.transform;
            atomContainer.SetParent(transform);
        }
        
        /// <summary>
        /// Spawnt Atome für ein Level
        /// </summary>
        public void SpawnAtomsForLevel(PuzzleLevel level)
        {
            ClearSpawnedAtoms();
            
            if (level == null || level.availableAtoms == null)
            {
                UnityEngine.Debug.LogWarning("Keine Atome zum Spawnen definiert!");
                return;
            }
            
            // Sammle alle zu spawnenden Atome
            List<string> atomsToSpawn = new List<string>();
            foreach (var atomReq in level.availableAtoms)
            {
                for (int i = 0; i < atomReq.count; i++)
                {
                    atomsToSpawn.Add(atomReq.element);
                }
            }
            
            // Spawn-Positionen berechnen
            List<Vector3> positions = CalculateSpawnPositions(atomsToSpawn.Count);
            
            // Atome spawnen
            for (int i = 0; i < atomsToSpawn.Count; i++)
            {
                GameObject atom = SpawnAtom(atomsToSpawn[i], positions[i]);
                if (atom != null)
                {
                    spawnedAtoms.Add(atom);
                }
            }
            
            UnityEngine.Debug.Log($"Spawned {spawnedAtoms.Count} atoms for level");
        }
        
        /// <summary>
        /// Spawnt ein einzelnes Atom
        /// </summary>
        private GameObject SpawnAtom(string element, Vector3 position)
        {
            GameObject prefab = GetPrefabForElement(element);
            
            if (prefab == null)
            {
                // Fallback: Erstelle Atom prozedural
                return CreateAtomProcedural(element, position);
            }
            
            GameObject atom = Instantiate(prefab, position, Quaternion.identity, atomContainer);
            atom.name = $"Atom_{element}_{spawnedAtoms.Count}";
            
            return atom;
        }
        
        /// <summary>
        /// Gibt das Prefab für ein Element zurück
        /// </summary>
        private GameObject GetPrefabForElement(string element)
        {
            switch (element.ToUpper())
            {
                case "H": return hydrogenPrefab;
                case "C": return carbonPrefab;
                case "O": return oxygenPrefab;
                case "N": return nitrogenPrefab;
                default: return null;
            }
        }
        
        /// <summary>
        /// Erstellt ein Atom prozedural falls kein Prefab vorhanden
        /// </summary>
        private GameObject CreateAtomProcedural(string element, Vector3 position)
        {
            GameObject atomObj = new GameObject($"Atom_{element}");
            atomObj.transform.position = position;
            atomObj.transform.SetParent(atomContainer);
            
            // Visual
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(atomObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            
            // Collider vom Visual entfernen
            Collider visualCollider = sphere.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }
            
            // Atom Component
            Atom atom = atomObj.AddComponent<Atom>();
            SetupAtomByElement(atom, element);
            
            // Collider auf Parent
            SphereCollider atomCollider = atomObj.AddComponent<SphereCollider>();
            atomCollider.radius = atom.atomRadius;
            atomCollider.isTrigger = false;
            
            // Rigidbody
            Rigidbody rb = atomObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // BondPoints
            int bondCount = atom.maxBonds;
            BondGeometry geometry = GetGeometryForElement(element);
            BondPointGenerator.GenerateBondPoints(atomObj.transform, bondCount, atom.atomRadius);
            
            atom.bondPoints.Clear();
            BondPoint[] points = atomObj.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);
            
            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }
            
            atom.ApplyVisuals();
            
            return atomObj;
        }
        
        /// <summary>
        /// Konfiguriert ein Atom basierend auf dem Element
        /// </summary>
        private void SetupAtomByElement(Atom atom, string element)
        {
            switch (element.ToUpper())
            {
                case "H":
                    atom.element = "H";
                    atom.maxBonds = 1;
                    atom.atomColor = Color.white;
                    atom.atomRadius = 0.3f;
                    break;
                case "C":
                    atom.element = "C";
                    atom.maxBonds = 4;
                    atom.atomColor = Color.gray;
                    atom.atomRadius = 0.5f;
                    break;
                case "O":
                    atom.element = "O";
                    atom.maxBonds = 2;
                    atom.atomColor = Color.red;
                    atom.atomRadius = 0.4f;
                    break;
                case "N":
                    atom.element = "N";
                    atom.maxBonds = 3;
                    atom.atomColor = new Color(0.2f, 0.2f, 1f);
                    atom.atomRadius = 0.45f;
                    break;
                default:
                    atom.element = element;
                    atom.maxBonds = 1;
                    atom.atomColor = Color.magenta;
                    atom.atomRadius = 0.4f;
                    break;
            }
        }
        
        /// <summary>
        /// Gibt die Geometrie für ein Element zurück
        /// </summary>
        private BondGeometry GetGeometryForElement(string element)
        {
            switch (element.ToUpper())
            {
                case "H": return BondGeometry.Linear1;
                case "C": return BondGeometry.Tetrahedral;
                case "O": return BondGeometry.Linear2;
                case "N": return BondGeometry.TrigonalPlanar;
                default: return BondGeometry.Linear1;
            }
        }
        
        /// <summary>
        /// Berechnet Spawn-Positionen basierend auf dem Pattern
        /// </summary>
        private List<Vector3> CalculateSpawnPositions(int count)
        {
            List<Vector3> positions = new List<Vector3>();
            Vector3 center = spawnCenter.position;
            
            switch (spawnPattern)
            {
                case SpawnPattern.Grid:
                    positions = CalculateGridPositions(count, center);
                    break;
                case SpawnPattern.Circle:
                    positions = CalculateCirclePositions(count, center);
                    break;
                case SpawnPattern.Random:
                    positions = CalculateRandomPositions(count, center);
                    break;
                case SpawnPattern.Line:
                    positions = CalculateLinePositions(count, center);
                    break;
            }
            
            return positions;
        }
        
        private List<Vector3> CalculateGridPositions(int count, Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);
            
            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                
                float x = (col - cols / 2f) * atomSpacing;
                float z = (row - rows / 2f) * atomSpacing;
                
                positions.Add(center + new Vector3(x, 0, z));
            }
            
            return positions;
        }
        
        private List<Vector3> CalculateCirclePositions(int count, Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            float radius = atomSpacing * count / (2f * Mathf.PI);
            
            for (int i = 0; i < count; i++)
            {
                float angle = i * 360f / count * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                positions.Add(center + new Vector3(x, 0, z));
            }
            
            return positions;
        }
        
        private List<Vector3> CalculateRandomPositions(int count, Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                float z = Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);
                
                positions.Add(center + new Vector3(x, 0, z));
            }
            
            return positions;
        }
        
        private List<Vector3> CalculateLinePositions(int count, Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            float totalWidth = (count - 1) * atomSpacing;
            float startX = center.x - totalWidth / 2f;
            
            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(startX + i * atomSpacing, center.y, center.z));
            }
            
            return positions;
        }
        
        /// <summary>
        /// Entfernt alle gespawnten Atome
        /// </summary>
        public void ClearSpawnedAtoms()
        {
            foreach (var atom in spawnedAtoms)
            {
                if (atom != null)
                {
                    Destroy(atom);
                }
            }
            spawnedAtoms.Clear();
        }
        
        /// <summary>
        /// Gizmo zum Visualisieren des Spawn-Bereichs
        /// </summary>
        void OnDrawGizmos()
        {
            if (spawnCenter == null) return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnCenter.position, spawnAreaSize);
        }
    }
    
    public enum SpawnPattern
    {
        Grid,
        Circle,
        Random,
        Line
    }
}
