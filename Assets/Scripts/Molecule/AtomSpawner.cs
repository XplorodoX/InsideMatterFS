using UnityEngine;
using System.Collections.Generic;
using InsideMatter.Molecule;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Spawnt verschiedene Atome an einem festgelegten Punkt.
    /// Kann über den Inspector oder durch andere Scripte getriggert werden.
    /// </summary>
    public class AtomSpawner : MonoBehaviour
    {
        [Header("Atom Prefabs")]
        [Tooltip("Liste der Atome, die gespawnt werden können (C, H, O, N)")]
        public List<GameObject> atomPrefabs = new List<GameObject>();

        [Header("Spawn Settings")]
        [Tooltip("Spawn Point (defaults to this transform)")]
        public Transform spawnPoint;

        [Tooltip("Soll beim Start direkt ein Satz Atome gespawnt werden?")]
        public bool spawnOnStart = false;
        
        [Tooltip("Versatz der Atome beim Spawnen")]
        public float spawnRadius = 0.3f;

        [Header("VFX")]
        [Tooltip("Effekt beim Spawnen (optional)")]
        public ParticleSystem spawnEffect;

        private int lastSpawnIndex = 0;

        void Awake()
        {
            if (spawnPoint == null) spawnPoint = transform;
        }

        void Start()
        {
            if (spawnOnStart && atomPrefabs.Count > 0)
            {
                for (int i = 0; i < atomPrefabs.Count; i++)
                {
                    SpawnAtom(i);
                }
            }
        }

        /// <summary>
        /// Spawnt ein zufälliges Atom aus der Liste
        /// </summary>
        public void SpawnRandomAtom()
        {
            if (atomPrefabs.Count == 0) return;
            int randomIndex = Random.Range(0, atomPrefabs.Count);
            SpawnAtom(randomIndex);
        }

        /// <summary>
        /// Spawnt das nächste Atom in der Liste
        /// </summary>
        public void SpawnNextAtom()
        {
            if (atomPrefabs.Count == 0) return;
            SpawnAtom(lastSpawnIndex);
            lastSpawnIndex = (lastSpawnIndex + 1) % atomPrefabs.Count;
        }

        /// <summary>
        /// Spawnt ein spezifisches Atom nach Index
        /// </summary>
        public void SpawnAtom(int index)
        {
            if (index < 0 || index >= atomPrefabs.Count)
            {
                Debug.LogWarning($"[AtomSpawner] Index {index} out of range.");
                return;
            }

            GameObject prefab = atomPrefabs[index];
            if (prefab == null) return;

            // Ein bisschen Randomness beim Spawn-Punkt
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y) * 0.5f;
            
            GameObject spawnedAtom = Instantiate(prefab, spawnPoint.position + randomOffset, Quaternion.identity);
            spawnedAtom.name = $"{prefab.name}_{Time.time}";

            // Sicherstellen, dass ein Rigidbody da ist und Gravitation nutzt
            Rigidbody rb = spawnedAtom.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true; // Wichtig für Realismus
                rb.linearVelocity = Vector3.up * 1.5f;
            }

            // Sicherstellen, dass MoleculeManager existiert
            if (MoleculeManager.Instance == null)
            {
                Debug.LogWarning("[AtomSpawner] MoleculeManager fehlt in der Szene! Bindungen werden nicht funktionieren.");
            }

            // Effekt abspielen
            if (spawnEffect != null)
            {
                spawnEffect.transform.position = spawnedAtom.transform.position;
                spawnEffect.Play();
            }

            Debug.Log($"[AtomSpawner] Atom gespawnt: {prefab.name}");
        }

        #if UNITY_EDITOR
        public void FindPrefabsInProject()
        {
            atomPrefabs.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("Atom_ t:Prefab", new[] { "Assets/Prefabs/Atoms" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) atomPrefabs.Add(prefab);
            }
            Debug.Log($"[AtomSpawner] {atomPrefabs.Count} Atom-Prefabs gefunden.");
        }

        [ContextMenu("Spawn Carbon (C)")]
        private void SpawnC() => SpawnAtomByName("C");

        [ContextMenu("Spawn Hydrogen (H)")]
        private void SpawnH() => SpawnAtomByName("H");

        [ContextMenu("Spawn Oxygen (O)")]
        private void SpawnO() => SpawnAtomByName("O");

        private void SpawnAtomByName(string symbol)
        {
            for (int i = 0; i < atomPrefabs.Count; i++)
            {
                if (atomPrefabs[i].name.Contains(symbol))
                {
                    SpawnAtom(i);
                    return;
                }
            }
        }
        #endif
    }
}
