using UnityEngine;
using UnityEditor;
using InsideMatter.Molecule;

namespace InsideMatter.Editor
{
    /// <summary>
    /// Editor-Tool zum schnellen Erstellen von Atom-Prefabs
    /// Menü: Tools > InsideMatter > Atom Creator
    /// </summary>
    public class AtomCreatorWindow : EditorWindow
    {
        private string elementSymbol = "C";
        private int maxBonds = 4;
        private Color atomColor = Color.black;
        private float atomRadius = 0.5f;
        private BondGeometry bondGeometry = BondGeometry.Tetrahedral;
        private float bondPointDistance = 0.5f;
        
        private bool createInScene = true;
        private bool createPrefab = true;
        private bool addVRSupport = true; // NEW: Toggle für VR Components
        
        [MenuItem("Tools/InsideMatter/Atom Creator")]
        public static void ShowWindow()
        {
            GetWindow<AtomCreatorWindow>("Atom Creator");
        }
        
        void OnGUI()
        {
            GUILayout.Label("Atom Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Chemische Eigenschaften", EditorStyles.boldLabel);
            elementSymbol = EditorGUILayout.TextField("Element Symbol", elementSymbol);
            maxBonds = EditorGUILayout.IntField("Max Bonds", maxBonds);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuelle Eigenschaften", EditorStyles.boldLabel);
            atomColor = EditorGUILayout.ColorField("Atom Color", atomColor);
            atomRadius = EditorGUILayout.Slider("Atom Radius", atomRadius, 0.1f, 2f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BondPoint Konfiguration", EditorStyles.boldLabel);
            bondGeometry = (BondGeometry)EditorGUILayout.EnumPopup("Bond Geometry", bondGeometry);
            bondPointDistance = EditorGUILayout.Slider("Bond Point Distance", bondPointDistance, 0.1f, 2f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optionen", EditorStyles.boldLabel);
            createInScene = EditorGUILayout.Toggle("Create in Scene", createInScene);
            createPrefab = EditorGUILayout.Toggle("Create Prefab", createPrefab);
            addVRSupport = EditorGUILayout.Toggle("Add VR Grab Component", addVRSupport);
            
            if (addVRSupport)
            {
                EditorGUILayout.HelpBox("Adds VRAtomGrab + XRGrabInteractable for VR controllers", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Adds AtomDrag for mouse-based interaction", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Quick Presets
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hydrogen (H)")) ApplyHydrogenPreset();
            if (GUILayout.Button("Carbon (C)")) ApplyCarbonPreset();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Nitrogen (N)")) ApplyNitrogenPreset();
            if (GUILayout.Button("Oxygen (O)")) ApplyOxygenPreset();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // Create Button
            if (GUILayout.Button("Create Atom", GUILayout.Height(40)))
            {
                CreateAtom();
            }
        }
        
        void ApplyHydrogenPreset()
        {
            elementSymbol = "H";
            maxBonds = 1;
            atomColor = Color.white;
            atomRadius = 0.3f;
            bondGeometry = BondGeometry.Linear1;
            bondPointDistance = 0.3f;
        }
        
        void ApplyCarbonPreset()
        {
            elementSymbol = "C";
            maxBonds = 4;
            atomColor = Color.black;
            atomRadius = 0.5f;
            bondGeometry = BondGeometry.Tetrahedral;
            bondPointDistance = 0.5f;
        }
        
        void ApplyNitrogenPreset()
        {
            elementSymbol = "N";
            maxBonds = 3;
            atomColor = new Color(0.2f, 0.2f, 1f);
            atomRadius = 0.45f;
            bondGeometry = BondGeometry.TrigonalPlanar;
            bondPointDistance = 0.45f;
        }
        
        void ApplyOxygenPreset()
        {
            elementSymbol = "O";
            maxBonds = 2;
            atomColor = Color.red;
            atomRadius = 0.4f;
            bondGeometry = BondGeometry.Linear2;
            bondPointDistance = 0.4f;
        }
        
        void CreateAtom()
        {
            // Atom GameObject erstellen
            GameObject atomObj = new GameObject($"Atom_{elementSymbol}");
            
            // Sphere Mesh
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(atomObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one;
            
            // WICHTIG: Collider vom Visual entfernen!
            Collider visualCollider = sphere.GetComponent<Collider>();
            if (visualCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(visualCollider);
            }
            
            // Sphere Collider AUF DEM PARENT für Raycast
            SphereCollider atomCollider = atomObj.AddComponent<SphereCollider>();
            atomCollider.radius = atomRadius;
            atomCollider.isTrigger = false; // MUSS false sein!
            
            // Atom Component
            Atom atom = atomObj.AddComponent<Atom>();
            atom.element = elementSymbol;
            atom.maxBonds = maxBonds;
            atom.atomColor = atomColor;
            atom.atomRadius = atomRadius;
            
            // Rigidbody für Physics
            Rigidbody rb = atomObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.useGravity = addVRSupport; // VR: Gravity enabled
            rb.constraints = addVRSupport ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeRotation;
            
            // Add interaction component based on mode
            if (addVRSupport)
            {
                // VR Mode: Add VRAtomGrab + XRGrabInteractable
                var vrGrab = atomObj.AddComponent<InsideMatter.Interaction.VRAtomGrab>();
                
                // XRGrabInteractable will be added automatically by VRAtomGrab's RequireComponent
                UnityEngine.Debug.Log("Added VR grab components (VRAtomGrab + XRGrabInteractable)");
            }
            else
            {
                // Desktop Mode: Add mouse-based drag
                atomObj.AddComponent<InsideMatter.Interaction.AtomDrag>();
                UnityEngine.Debug.Log("Added mouse drag component (AtomDrag)");
            }
            
            // BondPoints generieren
            int bondPointCount = GetBondPointCount(bondGeometry);
            BondPointGenerator.GenerateBondPoints(atomObj.transform, bondPointCount, bondPointDistance);
            
            // BondPoints sammeln
            atom.bondPoints.Clear();
            BondPoint[] points = atomObj.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);
            
            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }
            
            // Visuals anwenden
            atom.ApplyVisuals();
            
            // In Scene platzieren
            if (createInScene)
            {
                atomObj.transform.position = Vector3.zero;
                Selection.activeGameObject = atomObj;
                Undo.RegisterCreatedObjectUndo(atomObj, "Create Atom");
            }
            
            // Als Prefab speichern
            if (createPrefab)
            {
                string path = $"Assets/Prefabs/Atoms/Atom_{elementSymbol}.prefab";
                
                // Verzeichnis erstellen falls nicht vorhanden
                string directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                PrefabUtility.SaveAsPrefabAsset(atomObj, path);
                UnityEngine.Debug.Log($"Atom Prefab erstellt: {path}");
                
                if (!createInScene)
                {
                    UnityEngine.Object.DestroyImmediate(atomObj);
                }
            }
            
            UnityEngine.Debug.Log($"Atom '{elementSymbol}' erfolgreich erstellt!");
        }
        
        int GetBondPointCount(BondGeometry geometry)
        {
            switch (geometry)
            {
                case BondGeometry.Linear1: return 1;
                case BondGeometry.Linear2: return 2;
                case BondGeometry.TrigonalPlanar: return 3;
                case BondGeometry.Tetrahedral: return 4;
                case BondGeometry.TrigonalBipyramidal: return 5;
                case BondGeometry.Octahedral: return 6;
                default: return maxBonds;
            }
        }
    }
    
    /// <summary>
    /// Menu Items für schnelles Erstellen von Standard-Atomen
    /// </summary>
    public static class AtomCreatorMenuItems
    {
        [MenuItem("GameObject/InsideMatter/Atoms/Hydrogen (H)", false, 10)]
        static void CreateHydrogen()
        {
            CreateAtomQuick("H", 1, Color.white, 0.3f, BondGeometry.Linear1, 0.3f);
        }
        
        [MenuItem("GameObject/InsideMatter/Atoms/Carbon (C)", false, 10)]
        static void CreateCarbon()
        {
            CreateAtomQuick("C", 4, Color.black, 0.5f, BondGeometry.Tetrahedral, 0.5f);
        }
        
        [MenuItem("GameObject/InsideMatter/Atoms/Nitrogen (N)", false, 10)]
        static void CreateNitrogen()
        {
            CreateAtomQuick("N", 3, new Color(0.2f, 0.2f, 1f), 0.45f, BondGeometry.TrigonalPlanar, 0.45f);
        }
        
        [MenuItem("GameObject/InsideMatter/Atoms/Oxygen (O)", false, 10)]
        static void CreateOxygen()
        {
            CreateAtomQuick("O", 2, Color.red, 0.4f, BondGeometry.Linear2, 0.4f);
        }
        
        static void CreateAtomQuick(string symbol, int maxBonds, Color color, float radius, 
                                    BondGeometry geometry, float bondDist)
        {
            GameObject atomObj = new GameObject($"Atom_{symbol}");
            
            // Visual
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(atomObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            
            // Collider vom Visual entfernen
            Collider visualCollider = sphere.GetComponent<Collider>();
            if (visualCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(visualCollider);
            }
            
            // Collider AUF DEM PARENT
            SphereCollider atomCollider = atomObj.AddComponent<SphereCollider>();
            atomCollider.radius = radius;
            atomCollider.isTrigger = false;
            
            // Atom Component
            Atom atom = atomObj.AddComponent<Atom>();
            atom.element = symbol;
            atom.maxBonds = maxBonds;
            atom.atomColor = color;
            atom.atomRadius = radius;
            
            // Rigidbody
            Rigidbody rb = atomObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // BondPoints
            int count = 0;
            switch (geometry)
            {
                case BondGeometry.Linear1: count = 1; break;
                case BondGeometry.Linear2: count = 2; break;
                case BondGeometry.TrigonalPlanar: count = 3; break;
                case BondGeometry.Tetrahedral: count = 4; break;
                case BondGeometry.TrigonalBipyramidal: count = 5; break;
                case BondGeometry.Octahedral: count = 6; break;
            }
            
            BondPointGenerator.GenerateBondPoints(atomObj.transform, count, bondDist);
            
            atom.bondPoints.Clear();
            BondPoint[] points = atomObj.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);
            
            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }
            
            atom.ApplyVisuals();
            
            // Position bei Kamera
            if (SceneView.lastActiveSceneView != null)
            {
                atomObj.transform.position = SceneView.lastActiveSceneView.camera.transform.position + 
                                            SceneView.lastActiveSceneView.camera.transform.forward * 5f;
            }
            
            Selection.activeGameObject = atomObj;
            Undo.RegisterCreatedObjectUndo(atomObj, $"Create {symbol} Atom");
        }
    }
}
