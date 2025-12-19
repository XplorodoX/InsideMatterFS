using UnityEngine;
using InsideMatter.Molecule;
using InsideMatter.Puzzle;
using InsideMatter.VR;

namespace InsideMatter
{
    /// <summary>
    /// Auto-Setup für VR Puzzle Scene
    /// Erstellt automatisch alle benötigten GameObjects und Components
    /// </summary>
    public class VRPuzzleSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [Tooltip("Beim Start automatisch Setup durchführen")]
        public bool autoSetupOnStart = true;
        
        [Header("Spawn Settings")]
        public int hydrogenCount = 4;
        public int carbonCount = 2;
        public int oxygenCount = 2;
        public int nitrogenCount = 1;
        
        public float spawnRadius = 1.5f;
        public float spawnHeight = 1.2f;
        
        [Header("Prefabs (optional)")]
        public GameObject hydrogenPrefab;
        public GameObject carbonPrefab;
        public GameObject oxygenPrefab;
        public GameObject nitrogenPrefab;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupScene();
            }
        }

        [ContextMenu("Setup Complete VR Puzzle Scene")]
        public void SetupScene()
        {
            UnityEngine.Debug.Log("=== VR Puzzle Scene Setup Started ===");
            
            // 1. MoleculeManager
            SetupMoleculeManager();
            
            // 2. Spawn Atome
            SpawnAtoms();
            
            // 3. Setup VR Simulator (wenn keine XR Origin vorhanden)
            SetupVRSimulator();
            
            UnityEngine.Debug.Log("=== VR Puzzle Scene Setup Complete! ===");
            UnityEngine.Debug.Log("Controls: WASD=Move, Mouse=Look, Right-Click=Right Hand, Left-Click=Left Hand, G=Grip");
        }

        private void SetupMoleculeManager()
        {
            var existing = FindFirstObjectByType<MoleculeManager>();
            if (existing == null)
            {
                GameObject managerObj = new GameObject("MoleculeManager");
                managerObj.AddComponent<MoleculeManager>();
                UnityEngine.Debug.Log("✓ MoleculeManager created");
            }
            else
            {
                UnityEngine.Debug.Log("✓ MoleculeManager already exists");
            }

            // BondPreview für VR-Preview-Linien
            var existingPreview = FindFirstObjectByType<BondPreview>();
            if (existingPreview == null)
            {
                GameObject previewObj = new GameObject("BondPreview");
                var preview = previewObj.AddComponent<BondPreview>();
                preview.previewDistance = 0.5f;
                preview.previewAlpha = 0.4f;
                preview.previewThickness = 0.1f;
                UnityEngine.Debug.Log("✓ BondPreview created");
            }
            else
            {
                UnityEngine.Debug.Log("✓ BondPreview already exists");
            }
        }

        private void SpawnAtoms()
        {
            // Clear old atoms
            var oldAtoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            if (oldAtoms.Length > 0)
            {
                UnityEngine.Debug.Log($"Removing {oldAtoms.Length} old atoms...");
                foreach (var atom in oldAtoms)
                {
                    if (Application.isPlaying)
                        Destroy(atom.gameObject);
                    else
                        DestroyImmediate(atom.gameObject);
                }
            }

            Camera mainCam = Camera.main;
            Vector3 spawnCenter = mainCam != null ? mainCam.transform.position + mainCam.transform.forward * 2f : Vector3.zero;
            spawnCenter.y = spawnHeight;

            int totalAtoms = hydrogenCount + carbonCount + oxygenCount + nitrogenCount;
            int index = 0;

            // Spawn Hydrogen
            for (int i = 0; i < hydrogenCount; i++)
            {
                Vector3 pos = CalculateCirclePosition(spawnCenter, spawnRadius, index, totalAtoms);
                CreateAtom("H", 1, Color.white, 0.3f, pos);
                index++;
            }

            // Spawn Carbon
            for (int i = 0; i < carbonCount; i++)
            {
                Vector3 pos = CalculateCirclePosition(spawnCenter, spawnRadius, index, totalAtoms);
                CreateAtom("C", 4, Color.black, 0.5f, pos);
                index++;
            }

            // Spawn Oxygen
            for (int i = 0; i < oxygenCount; i++)
            {
                Vector3 pos = CalculateCirclePosition(spawnCenter, spawnRadius, index, totalAtoms);
                CreateAtom("O", 2, Color.red, 0.4f, pos);
                index++;
            }

            // Spawn Nitrogen
            for (int i = 0; i < nitrogenCount; i++)
            {
                Vector3 pos = CalculateCirclePosition(spawnCenter, spawnRadius, index, totalAtoms);
                CreateAtom("N", 3, new Color(0.2f, 0.2f, 1f), 0.45f, pos);
                index++;
            }

            UnityEngine.Debug.Log($"✓ Spawned {totalAtoms} atoms in circle formation");
        }

        private Vector3 CalculateCirclePosition(Vector3 center, float radius, int index, int total)
        {
            float angle = (360f / total) * index * Mathf.Deg2Rad;
            float x = center.x + radius * Mathf.Cos(angle);
            float z = center.z + radius * Mathf.Sin(angle);
            return new Vector3(x, center.y, z);
        }

        private void CreateAtom(string element, int maxBonds, Color color, float radius, Vector3 position)
        {
            // Create atom GameObject
            GameObject atomObj = new GameObject($"Atom_{element}");
            atomObj.transform.position = position;

            // Visual sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(atomObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * radius * 2f;

            // Remove collider from visual
            var visualCollider = sphere.GetComponent<Collider>();
            if (visualCollider != null)
                Destroy(visualCollider);

            // Add collider to parent
            SphereCollider atomCollider = atomObj.AddComponent<SphereCollider>();
            atomCollider.radius = radius;
            atomCollider.isTrigger = false;

            // Atom component
            Atom atom = atomObj.AddComponent<Atom>();
            atom.element = element;
            atom.maxBonds = maxBonds;
            atom.atomColor = color;
            atom.atomRadius = radius;

            // Rigidbody for physics
            Rigidbody rb = atomObj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Generate bond points
            BondGeometry geometry = GetGeometry(maxBonds);
            BondPointGenerator.GenerateBondPoints(atomObj.transform, maxBonds, radius);

            // Collect bond points
            atom.bondPoints.Clear();
            BondPoint[] points = atomObj.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);

            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }

            // Add VR Grab component
            var vrGrab = atomObj.AddComponent<InsideMatter.Interaction.VRAtomGrab>();

            // Apply visuals
            atom.ApplyVisuals();
        }

        private BondGeometry GetGeometry(int maxBonds)
        {
            switch (maxBonds)
            {
                case 1: return BondGeometry.Linear1;
                case 2: return BondGeometry.Linear2;
                case 3: return BondGeometry.TrigonalPlanar;
                case 4: return BondGeometry.Tetrahedral;
                case 5: return BondGeometry.TrigonalBipyramidal;
                case 6: return BondGeometry.Octahedral;
                default: return BondGeometry.Tetrahedral;
            }
        }

        private void SetupVRSimulator()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                UnityEngine.Debug.LogWarning("No Main Camera found!");
                return;
            }

            // Check if VR Origin exists
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                UnityEngine.Debug.Log("✓ XR Origin found - VR mode");
                return;
            }

            // Desktop mode - add camera controller if not present
            var cameraController = mainCam.GetComponent<DesktopCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCam.gameObject.AddComponent<DesktopCameraController>();
                UnityEngine.Debug.Log("✓ Desktop Camera Controller added");
            }

            // Create hand visualizers
            GameObject leftHand = new GameObject("LeftHand_Simulator");
            leftHand.transform.position = mainCam.transform.position;
            
            GameObject rightHand = new GameObject("RightHand_Simulator");
            rightHand.transform.position = mainCam.transform.position;

            // Add direct interactors
            var leftInteractorObj = new GameObject("Direct Interactor");
            leftInteractorObj.transform.SetParent(leftHand.transform);
            var leftInteractor = leftInteractorObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            var leftCollider = leftInteractorObj.AddComponent<SphereCollider>();
            leftCollider.isTrigger = true;
            leftCollider.radius = 0.15f;

            var rightInteractorObj = new GameObject("Direct Interactor");
            rightInteractorObj.transform.SetParent(rightHand.transform);
            var rightInteractor = rightInteractorObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            var rightCollider = rightInteractorObj.AddComponent<SphereCollider>();
            rightCollider.isTrigger = true;
            rightCollider.radius = 0.15f;

            // Add VR Simulator Controller
            GameObject simulatorObj = new GameObject("VR_Simulator");
            var simulator = simulatorObj.AddComponent<VRSimulatorController>();
            simulator.enableSimulation = true;
            simulator.showHandVisuals = true;
            simulator.leftHand = leftHand.transform;
            simulator.rightHand = rightHand.transform;
            simulator.cameraTransform = mainCam.transform;
            simulator.leftDirectInteractor = leftInteractor;
            simulator.rightDirectInteractor = rightInteractor;

            UnityEngine.Debug.Log("✓ VR Simulator created (Desktop mode)");
            UnityEngine.Debug.Log("   Controls: Right-Click=Right Hand, Left-Click=Left Hand, G=Grip");
        }
    }
}
