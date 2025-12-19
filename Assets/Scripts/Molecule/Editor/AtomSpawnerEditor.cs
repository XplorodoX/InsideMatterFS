using UnityEngine;
using UnityEditor;

namespace InsideMatter.Molecule
{
    [CustomEditor(typeof(AtomSpawner))]
    public class AtomSpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AtomSpawner spawner = (AtomSpawner)target;

            if (GUILayout.Button("Create Spawn Station (Pedestal)", GUILayout.Height(30)))
            {
                CreateSpawnStation(spawner, true);
            }

            if (GUILayout.Button("Setup on Nearest Table", GUILayout.Height(30)))
            {
                SetupOnTable(spawner);
            }

            GUILayout.Space(10);
            GUILayout.Label("Spawn Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Spawn Next Atom (Cycle)", GUILayout.Height(30)))
            {
                spawner.SpawnNextAtom();
            }

            if (GUILayout.Button("Spawn Random Atom", GUILayout.Height(30)))
            {
                spawner.SpawnRandomAtom();
            }

            GUILayout.Space(5);
            
            if (spawner.atomPrefabs != null && spawner.atomPrefabs.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < spawner.atomPrefabs.Count; i++)
                {
                    if (spawner.atomPrefabs[i] != null)
                    {
                        if (GUILayout.Button(spawner.atomPrefabs[i].name))
                        {
                            spawner.SpawnAtom(i);
                        }
                    }
                    
                    // Zeilenumbruch nach 2 Buttons
                    if ((i + 1) % 2 == 0 && i < spawner.atomPrefabs.Count - 1)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void CreateSpawnStation(AtomSpawner spawner, bool createPedestal)
        {
            GameObject station = new GameObject("AtomSpawnStation");
            station.transform.position = spawner.transform.position;

            if (createPedestal)
            {
                // Pedestal
                GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedestal.name = "Pedestal";
                pedestal.transform.SetParent(station.transform);
                pedestal.transform.localPosition = new Vector3(0, -0.4f, 0);
                pedestal.transform.localScale = new Vector3(1.5f, 0.8f, 0.5f);
            }

            // Spawner auf Position halten
            spawner.transform.SetParent(station.transform);
            spawner.transform.localPosition = Vector3.zero;
            spawner.spawnPoint = spawner.transform;
            spawner.spawnRadius = 0.2f;

            // Buttons für jedes Prefab
            if (spawner.atomPrefabs.Count > 0)
            {
                float spacing = 0.3f;
                float startX = -(spawner.atomPrefabs.Count - 1) * (spacing * 0.5f);
                for (int i = 0; i < spawner.atomPrefabs.Count; i++)
                {
                    GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    btn.name = $"Button_{spawner.atomPrefabs[i].name}";
                    btn.transform.SetParent(station.transform);
                    btn.transform.localPosition = new Vector3(startX + i * spacing, 0.05f, 0.2f);
                    btn.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
                    btn.transform.localRotation = Quaternion.Euler(0, 0, 0);

                    // Component hinzufügen
                    var spawnBtn = btn.AddComponent<AtomSpawnButton>();
                    spawnBtn.spawner = spawner;
                    spawnBtn.atomIndex = i;

                    // Farbe setzen
                    var rend = btn.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        var atom = spawner.atomPrefabs[i].GetComponent<Atom>();
                        if (atom != null) rend.material.color = atom.atomColor;
                    }
                }
            }

            Selection.activeGameObject = station;
            Debug.Log("[AtomSpawner] Spawn Station erstellt!");
        }

        private void SetupOnTable(AtomSpawner spawner)
        {
            // Finde Benches
            GameObject[] benches = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject nearestBench = null;
            float minDist = float.MaxValue;

            foreach (var obj in benches)
            {
                if (obj.name.Contains("Bench") || obj.name.Contains("Table"))
                {
                    float d = Vector3.Distance(spawner.transform.position, obj.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearestBench = obj;
                    }
                }
            }

            if (nearestBench != null)
            {
                // Setze Spawner auf den Tisch
                spawner.transform.position = nearestBench.transform.position + Vector3.up * 1.0f;
                CreateSpawnStation(spawner, false);
                Debug.Log($"[AtomSpawner] Auf Tisch '{nearestBench.name}' platziert.");
            }
            else
            {
                Debug.LogWarning("[AtomSpawner] Kein Tisch oder 'Bench' in der Nähe gefunden. Benutze erst 'Decorate Demo Scene'.");
            }
        }
    }
}
