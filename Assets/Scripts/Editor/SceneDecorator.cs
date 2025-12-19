using UnityEngine;
using UnityEditor;
using InsideMatter.Molecule;

namespace InsideMatter.Editor
{
    /// <summary>
    /// Utility to decorate the DemoScene with a professional laboratory look using only primitives.
    /// This avoids using SyntyStudios as requested.
    /// </summary>
    public static class SceneDecorator
    {
        [MenuItem("InsideMatter/Decorate Demo Scene")]
        public static void DecorateScene()
        {
            Debug.Log(">>> STARTE DECORATE SCENE <<<");
            GameObject world = GameObject.Find("Environment");
            if (world == null) world = new GameObject("Environment");

            // 1. Floor & Walls
            CreateRoom(world.transform);

            // 2. Laboratory Furniture
            CreateLabBench(world.transform, new Vector3(0, 0, 2), "Front Bench");
            CreateLabBench(world.transform, new Vector3(-3, 0, 0), "Left Bench");
            CreateLabBench(world.transform, new Vector3(3, 0, 0), "Right Bench");

            // 4. Integrated Spawner
            SetupIntegratedSpawner(new Vector3(0, 0, 2));

            // 5. Setup Managers (Molecule Logic)
            SetupManagers();

            // 3. Decorations
            GameObject shelving = CreateShelving(world.transform, new Vector3(0, 2, 4.5f));
            PopulateShelves(shelving.transform);

            Debug.Log("[SceneDecorator] Room built with primitives! Look for 'Environment' group.");
        }

        private static void CreateRoom(Transform parent)
        {
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Lab_Floor";
            floor.transform.SetParent(parent);
            floor.transform.localScale = new Vector3(2, 1, 2);
            ApplyMaterial(floor, new Color(0.15f, 0.15f, 0.18f), 0.1f); // Dark industrial floor

            // Back Wall
            CreateWall(parent, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.2f), "Wall_Back");
            // Left Wall
            CreateWall(parent, new Vector3(-5, 2.5f, 0), new Vector3(0.2f, 5, 10), "Wall_Left");
            // Right Wall
            CreateWall(parent, new Vector3(5, 2.5f, 0), new Vector3(0.2f, 5, 10), "Wall_Right");
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, string name)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, new Color(0.85f, 0.85f, 0.9f), 0.5f); // Clean white-ish wall
        }

        private static void CreateLabBench(Transform parent, Vector3 pos, string name)
        {
            GameObject bench = new GameObject(name);
            bench.transform.SetParent(parent);
            bench.transform.position = pos;

            // Surface
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.transform.SetParent(bench.transform);
            surface.transform.localPosition = new Vector3(0, 0.9f, 0);
            surface.transform.localScale = new Vector3(2.5f, 0.1f, 1.2f);
            ApplyMaterial(surface, new Color(0.1f, 0.3f, 0.5f), 0.2f); // Blue-ish tech surface

            // Legs
            CreateLeg(bench.transform, new Vector3(-1.1f, 0.45f, 0.5f));
            CreateLeg(bench.transform, new Vector3(1.1f, 0.45f, 0.5f));
            CreateLeg(bench.transform, new Vector3(-1.1f, 0.45f, -0.5f));
            CreateLeg(bench.transform, new Vector3(1.1f, 0.45f, -0.5f));
        }

        private static void CreateLeg(Transform parent, Vector3 pos)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(parent);
            leg.transform.localPosition = pos;
            leg.transform.localScale = new Vector3(0.1f, 0.9f, 0.1f);
            ApplyMaterial(leg, Color.gray, 0.8f);
        }

        private static GameObject CreateShelving(Transform parent, Vector3 pos)
        {
            GameObject shelves = new GameObject("Shelving");
            shelves.transform.SetParent(parent);
            shelves.transform.position = pos;

            for (int i = 0; i < 3; i++)
            {
                GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.transform.SetParent(shelves.transform);
                shelf.transform.localPosition = new Vector3(0, i * 0.8f, 0);
                shelf.transform.localScale = new Vector3(4, 0.05f, 0.4f);
                ApplyMaterial(shelf, new Color(0.4f, 0.4f, 0.45f), 0.8f);
            }
            return shelves;
        }

        private static void PopulateShelves(Transform shelfParent)
        {
            Debug.Log("[SceneDecorator] Populating shelves with educational models...");
            
            // Clean old models
            var oldModels = GameObject.Find("ShelfModels");
            if (oldModels != null) Object.DestroyImmediate(oldModels);
            
            GameObject modelsRoot = new GameObject("ShelfModels");
            modelsRoot.transform.SetParent(shelfParent);
            modelsRoot.transform.localPosition = Vector3.zero;

            // Find Prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab Atom_", new[] { "Assets/Prefabs/Atoms" });
            
            // --- Shelf 0 (Bottom): Showcase Atoms ---
            float spacing = 0.8f;
            float startX = -1.2f;
            int atomIdx = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                GameObject showcase = Object.Instantiate(prefab, modelsRoot.transform);
                showcase.name = "Showcase_" + prefab.name;
                showcase.transform.localPosition = new Vector3(startX + atomIdx * spacing, 0.25f, 0);
                showcase.transform.localScale = Vector3.one * 0.8f;

                // Strip physics and scripts for shelf display
                // IMPORTANT: Remove logic scripts BEFORE colliders to avoid Unity errors
                foreach (var bp in showcase.GetComponentsInChildren<Molecule.BondPoint>()) Object.DestroyImmediate(bp);
                foreach (var a in showcase.GetComponentsInChildren<Molecule.Atom>()) Object.DestroyImmediate(a);
                
                foreach (var c in showcase.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                var rb = showcase.GetComponent<Rigidbody>(); if (rb != null) Object.DestroyImmediate(rb);
                var grab = showcase.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(); if (grab != null) Object.DestroyImmediate(grab);
                var vrGrab = showcase.GetComponent<InsideMatter.Interaction.VRAtomGrab>(); if (vrGrab != null) Object.DestroyImmediate(vrGrab);

                atomIdx++;
                if (atomIdx >= 4) break;
            }

            // --- Shelf 1 (Middle): Simple Molecules ---
            var hPrefab = FindAtomPrefab("H");
            var oPrefab = FindAtomPrefab("O");

            if (hPrefab != null && oPrefab != null)
            {
                // Create H2O Showcase
                GameObject h2oModel = CreateH2OModel(hPrefab, oPrefab);
                h2oModel.transform.SetParent(modelsRoot.transform);
                h2oModel.transform.localPosition = new Vector3(-0.8f, 1.05f, 0);
                h2oModel.transform.localScale = Vector3.one * 0.5f;

                // Create O2 Showcase
                GameObject o2Model = CreateO2Model(oPrefab);
                o2Model.transform.SetParent(modelsRoot.transform);
                o2Model.transform.localPosition = new Vector3(0.8f, 1.05f, 0);
                o2Model.transform.localScale = Vector3.one * 0.6f;
            }
        }

        private static GameObject FindAtomPrefab(string element)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Prefab Atom_{element}", new[] { "Assets/Prefabs/Atoms" });
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private static GameObject CreateH2OModel(GameObject hPre, GameObject oPre)
        {
            GameObject root = new GameObject("H2O_Model");
            
            GameObject o = Object.Instantiate(oPre, root.transform);
            o.transform.localPosition = Vector3.zero;
            StripPhysics(o);

            GameObject h1 = Object.Instantiate(hPre, root.transform);
            h1.transform.localPosition = new Vector3(0.4f, 0.3f, 0);
            StripPhysics(h1);

            GameObject h2 = Object.Instantiate(hPre, root.transform);
            h2.transform.localPosition = new Vector3(-0.4f, 0.3f, 0);
            StripPhysics(h2);

            return root;
        }

        private static GameObject CreateO2Model(GameObject oPre)
        {
            GameObject root = new GameObject("O2_Model");
            
            GameObject o1 = Object.Instantiate(oPre, root.transform);
            o1.transform.localPosition = new Vector3(0.3f, 0, 0);
            StripPhysics(o1);

            GameObject o2 = Object.Instantiate(oPre, root.transform);
            o2.transform.localPosition = new Vector3(-0.3f, 0, 0);
            StripPhysics(o2);

            return root;
        }

        private static void StripPhysics(GameObject obj)
        {
            // IMPORTANT: Remove logic scripts BEFORE colliders to avoid Unity errors
            foreach (var bp in obj.GetComponentsInChildren<Molecule.BondPoint>()) Object.DestroyImmediate(bp);
            foreach (var a in obj.GetComponentsInChildren<Molecule.Atom>()) Object.DestroyImmediate(a);

            foreach (var c in obj.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var rb = obj.GetComponent<Rigidbody>(); if (rb != null) Object.DestroyImmediate(rb);
            var grab = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(); if (grab != null) Object.DestroyImmediate(grab);
            var vrGrab = obj.GetComponent<InsideMatter.Interaction.VRAtomGrab>(); if (vrGrab != null) Object.DestroyImmediate(vrGrab);
        }

        private static void SetupIntegratedSpawner(Vector3 benchPos)
        {
            Debug.Log("[SceneDecorator] >> Setup Atom Trays...");

            // Cleanup old stuff
            var oldButtons = Object.FindObjectsByType<AtomSpawnButton>(FindObjectsSortMode.None);
            foreach (var b in oldButtons) Object.DestroyImmediate(b.gameObject);
            
            var oldReplenishers = Object.FindObjectsByType<AtomReplenisher>(FindObjectsSortMode.None);
            foreach (var r in oldReplenishers) Object.DestroyImmediate(r.gameObject);

            var oldStation = GameObject.Find("InteractiveSpawnButtons");
            if (oldStation != null) Object.DestroyImmediate(oldStation);

            // Access Spawner mainly for the list of prefabs
            AtomSpawner spawner = GameObject.FindFirstObjectByType<AtomSpawner>();
            if (spawner == null)
            {
                spawner = new GameObject("MainAtomSpawner").AddComponent<AtomSpawner>();
            }
            spawner.FindPrefabsInProject();

            // Create Station Holder
            GameObject station = new GameObject("InteractiveSpawnButtons");
            station.transform.position = benchPos + Vector3.up * 0.95f; // Table surface height
            
            // Parent to bench
            GameObject frontBench = GameObject.Find("Front Bench");
            if (frontBench != null) 
            {
                station.transform.SetParent(frontBench.transform);
                station.transform.localPosition = new Vector3(0, 0.95f, 0); 
            }

            // --- CREATE TRAYS ---
            int count = spawner.atomPrefabs.Count > 0 ? spawner.atomPrefabs.Count : 4;
            float spacing = 0.7f; // Mehr Platz, damit keine Atome kollidieren!
            float startX = -((count - 1) * spacing) / 2f;

            for (int i = 0; i < count; i++)
            {
                // Determine Prefab & Color
                GameObject atomPrefab = (spawner.atomPrefabs.Count > i) ? spawner.atomPrefabs[i] : null;
                Color trayColor = Color.gray;
                string atomName = "Dummy";

                if (atomPrefab != null)
                {
                    atomName = atomPrefab.name;
                    var atomScript = atomPrefab.GetComponent<Atom>();
                    if (atomScript != null) trayColor = atomScript.atomColor;
                }
                else
                {
                    // Fallback colors for dummies
                    if(i==1) trayColor = Color.black;
                    else if(i==2) trayColor = Color.red;
                    else if(i==3) trayColor = Color.blue;
                }

                // 1. Create Tray Visual ("Petri Dish Style")
                GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tray.name = $"PetriDish_{atomName}";
                tray.transform.SetParent(station.transform);
                tray.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                tray.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f); // Very flat disc

                // 2. Apply Material (Vibrant / Glossy look)
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(trayColor.r, trayColor.g, trayColor.b, 0.75f); // More visible
                mat.SetFloat("_Smoothness", 0.9f);
                mat.SetFloat("_Metallic", 0.1f);
                
                // Add Emission to make color "pop"
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", trayColor * 0.4f); // Slight glow
                
                // Set Transparent Mode (but less ghost-like)
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
                
                tray.GetComponent<Renderer>().sharedMaterial = mat;

                // 3. Add Logic
                var replenisher = tray.AddComponent<AtomReplenisher>();
                replenisher.atomPrefab = atomPrefab;
                
                // Add a label? (Optional, maybe later)
            }
            
            Debug.Log($"[SceneDecorator] Created {count} Atom Replenish Trays.");
        }

        private static void SetupManagers()
        {
            Debug.Log("[SceneDecorator] >> Checking Molecule Managers...");
            
            // 1. Molecule Manager
            var molManager = Object.FindFirstObjectByType<MoleculeManager>();
            if (molManager == null)
            {
                GameObject mm = new GameObject("MoleculeManager");
                molManager = mm.AddComponent<MoleculeManager>();
                Debug.Log("[SceneDecorator] Created MoleculeManager.");
            }

            // 2. Bond Preview
            var bondPreview = Object.FindFirstObjectByType<BondPreview>();
            if (bondPreview == null)
            {
                GameObject bp = new GameObject("BondPreview");
                bondPreview = bp.AddComponent<BondPreview>();
                Debug.Log("[SceneDecorator] Created BondPreview.");
            }
        }

        private static void ApplyMaterial(GameObject obj, Color color, float smoothness)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                mat.SetFloat("_Smoothness", smoothness);
                rend.material = mat;
            }
        }
    }
}
