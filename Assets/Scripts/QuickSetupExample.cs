using UnityEngine;
using InsideMatter.Molecule;
using InsideMatter.VR;

namespace InsideMatter
{
    /// <summary>
    /// VR-Setup für eine schnelle Demo-Szene mit Molekülen.
    /// Füge dieses Script zu einem GameObject hinzu und drücke Play.
    /// 
    /// STEUERUNG (VR):
    /// - Controller greifen = Atom aufnehmen
    /// - Loslassen = Bindung festigen (wenn Preview aktiv)
    /// - Atom drehen = BondPoint wechseln
    /// - Wegziehen = Bindung lösen
    /// 
    /// STEUERUNG (Desktop-Simulator):
    /// - Rechte Maustaste = Rechte Hand aktivieren
    /// - Linke Maustaste = Linke Hand aktivieren
    /// - G = Greifen/Loslassen
    /// - Q/E = Hand hoch/runter
    /// - R/F = Hand vor/zurück
    /// - WASD = Bewegen
    /// </summary>
    public class QuickSetupExample : MonoBehaviour
    {
        [Header("Demo Settings")]
        [Tooltip("Anzahl der zu spawenden Atome pro Element")]
        public int atomsPerElement = 2;
        
        [Tooltip("Spawn-Radius um dieses GameObject")]
        public float spawnRadius = 1.5f;
        
        [Tooltip("Spawn-Höhe (Y-Position)")]
        public float spawnHeight = 1.2f;
        
        [Header("Auto-Setup")]
        [Tooltip("Erstelle automatisch Manager beim Start")]
        public bool autoCreateManagers = true;

        [Tooltip("Aktiviere VR-Simulator für Desktop-Testing")]
        public bool enableDesktopSimulator = true;
        
        void Start()
        {
            if (autoCreateManagers)
            {
                SetupManagers();
                SetupVREnvironment();
                CreateGroundPlane();
            }
            
            SpawnDemoAtoms();
            
            Debug.Log("=== VR Molekül-Bausystem gestartet ===");
            Debug.Log("VR: Controller zum Greifen, Loslassen zum Verbinden");
            Debug.Log("Desktop: Rechtsklick=Hand, G=Greifen, Q/E/R/F=Hand bewegen");
        }

        /// <summary>
        /// Erstellt einen Boden, auf dem die Atome landen können
        /// </summary>
        void CreateGroundPlane()
        {
            // Prüfe ob schon ein Boden existiert
            if (GameObject.Find("Ground") != null) return;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            // Material
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(0.3f, 0.3f, 0.35f);
                renderer.material = material;
            }

            Debug.Log("✓ Boden erstellt");
        }
        
        /// <summary>
        /// Erstellt automatisch die benötigten Manager
        /// </summary>
        void SetupManagers()
        {
            // MoleculeManager prüfen/erstellen
            if (MoleculeManager.Instance == null)
            {
                GameObject managerObj = new GameObject("MoleculeManager");
                MoleculeManager manager = managerObj.AddComponent<MoleculeManager>();
                
                // Standard-Einstellungen
                manager.snapStrength = 0.7f;
                manager.bondThickness = 0.1f;
                manager.debugMode = true;
                
                Debug.Log("✓ MoleculeManager erstellt");
            }
            
            // BondPreview prüfen/erstellen (für VR-Preview-Linien)
            if (BondPreview.Instance == null)
            {
                GameObject previewObj = new GameObject("BondPreview");
                BondPreview preview = previewObj.AddComponent<BondPreview>();
                
                // Standard-Einstellungen
                preview.previewDistance = 0.5f;
                preview.previewAlpha = 0.4f;
                preview.previewThickness = 0.1f;
                
                Debug.Log("✓ BondPreview erstellt");
            }
        }

        /// <summary>
        /// Erstellt VR-Umgebung oder Desktop-Simulator
        /// </summary>
        void SetupVREnvironment()
        {
            // Prüfe ob XR Origin vorhanden (echtes VR)
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                Debug.Log("✓ XR Origin gefunden - VR Modus aktiv");
                return;
            }

            // Desktop-Simulator erstellen
            if (enableDesktopSimulator)
            {
                SetupDesktopVRSimulator();
            }
        }

        /// <summary>
        /// Erstellt VR-Simulator für Desktop-Testing
        /// </summary>
        void SetupDesktopVRSimulator()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("Keine Main Camera gefunden!");
                return;
            }

            // Desktop Camera Controller
            var cameraController = mainCam.GetComponent<DesktopCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCam.gameObject.AddComponent<DesktopCameraController>();
            }

            // Hand GameObjects erstellen
            GameObject leftHand = new GameObject("LeftHand_Simulator");
            leftHand.transform.position = mainCam.transform.position + mainCam.transform.forward * 0.5f - mainCam.transform.right * 0.3f;
            
            GameObject rightHand = new GameObject("RightHand_Simulator");
            rightHand.transform.position = mainCam.transform.position + mainCam.transform.forward * 0.5f + mainCam.transform.right * 0.3f;

            // Direct Interactors für Hände
            var leftInteractorObj = new GameObject("Direct Interactor");
            leftInteractorObj.transform.SetParent(leftHand.transform);
            leftInteractorObj.transform.localPosition = Vector3.zero;
            var leftInteractor = leftInteractorObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            var leftCollider = leftInteractorObj.AddComponent<SphereCollider>();
            leftCollider.isTrigger = true;
            leftCollider.radius = 0.15f;

            var rightInteractorObj = new GameObject("Direct Interactor");
            rightInteractorObj.transform.SetParent(rightHand.transform);
            rightInteractorObj.transform.localPosition = Vector3.zero;
            var rightInteractor = rightInteractorObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            var rightCollider = rightInteractorObj.AddComponent<SphereCollider>();
            rightCollider.isTrigger = true;
            rightCollider.radius = 0.15f;

            // Hand-Visuals (kleine Kugeln)
            CreateHandVisual(leftHand.transform, Color.blue);
            CreateHandVisual(rightHand.transform, Color.red);

            // VR Simulator Controller
            GameObject simulatorObj = new GameObject("VR_Simulator");
            var simulator = simulatorObj.AddComponent<VRSimulatorController>();
            simulator.enableSimulation = true;
            simulator.showHandVisuals = true;
            simulator.leftHand = leftHand.transform;
            simulator.rightHand = rightHand.transform;
            simulator.cameraTransform = mainCam.transform;
            simulator.leftDirectInteractor = leftInteractor;
            simulator.rightDirectInteractor = rightInteractor;

            Debug.Log("✓ Desktop VR-Simulator erstellt");
        }

        void CreateHandVisual(Transform parent, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "HandVisual";
            visual.transform.SetParent(parent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.1f;
            
            // Collider entfernen
            Destroy(visual.GetComponent<Collider>());
            
            // Farbe setzen
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = color;
            }
        }
        
        /// <summary>
        /// Spawnt Demo-Atome für Tests (VR-ready)
        /// </summary>
        void SpawnDemoAtoms()
        {
            int totalAtoms = atomsPerElement * 4;
            int index = 0;

            // Wasserstoff (H)
            for (int i = 0; i < atomsPerElement; i++)
            {
                CreateVRAtom("H", 1, Color.white, 0.3f, BondGeometry.Linear1, 0.3f, index++, totalAtoms);
            }
            
            // Kohlenstoff (C)
            for (int i = 0; i < atomsPerElement; i++)
            {
                CreateVRAtom("C", 4, Color.gray, 0.5f, BondGeometry.Tetrahedral, 0.5f, index++, totalAtoms);
            }
            
            // Sauerstoff (O)
            for (int i = 0; i < atomsPerElement; i++)
            {
                CreateVRAtom("O", 2, Color.red, 0.4f, BondGeometry.Linear2, 0.4f, index++, totalAtoms);
            }
            
            // Stickstoff (N)
            for (int i = 0; i < atomsPerElement; i++)
            {
                CreateVRAtom("N", 3, new Color(0.2f, 0.2f, 1f), 0.45f, BondGeometry.TrigonalPlanar, 0.45f, index++, totalAtoms);
            }
            
            Debug.Log($"✓ {totalAtoms} VR-Atome erstellt (H, C, O, N)");
        }
        
        /// <summary>
        /// Erstellt ein VR-greifbares Atom
        /// </summary>
        GameObject CreateVRAtom(string symbol, int maxBonds, Color color, float radius, 
                               BondGeometry geometry, float bondDist, int index, int total)
        {
            // Position im Kreis berechnen
            float angle = (360f / total) * index * Mathf.Deg2Rad;
            Vector3 position = transform.position + new Vector3(
                Mathf.Cos(angle) * spawnRadius,
                spawnHeight,
                Mathf.Sin(angle) * spawnRadius
            );
            
            // Atom GameObject
            GameObject atomObj = new GameObject($"Atom_{symbol}_{index}");
            atomObj.transform.position = position;
            
            // Visual (Sphere)
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Visual";
            sphere.transform.SetParent(atomObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * radius * 2f;
            
            // Collider vom Visual entfernen
            Collider visualCollider = sphere.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }
            
            // Atom Component
            Atom atom = atomObj.AddComponent<Atom>();
            atom.element = symbol;
            atom.maxBonds = maxBonds;
            atom.atomColor = color;
            atom.atomRadius = radius;
            
            // Sphere Collider für VR Grab
            SphereCollider atomCollider = atomObj.AddComponent<SphereCollider>();
            atomCollider.radius = radius;
            atomCollider.isTrigger = false;
            
            // Rigidbody für VR Physics
            Rigidbody rb = atomObj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.useGravity = true; // Fällt auf Boden wenn nicht verbunden
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.None; // Freie Rotation in VR
            
            // BondPoints generieren
            int bondPointCount = GetBondPointCount(geometry);
            BondPointGenerator.GenerateBondPoints(atomObj.transform, bondPointCount, bondDist);
            
            // BondPoints registrieren
            atom.bondPoints.Clear();
            BondPoint[] points = atomObj.GetComponentsInChildren<BondPoint>();
            atom.bondPoints.AddRange(points);
            
            foreach (var bp in atom.bondPoints)
            {
                bp.ParentAtom = atom;
            }
            
            // *** VR GRAB COMPONENT *** - Das macht das Atom greifbar!
            var vrGrab = atomObj.AddComponent<InsideMatter.Interaction.VRAtomGrab>();
            vrGrab.throwVelocityScale = 1.5f;
            vrGrab.previewDistance = 0.6f;
            vrGrab.bondBreakDistance = 0.4f;
            
            // Visuals anwenden
            atom.ApplyVisuals();
            
            return atomObj;
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
                default: return 1;
            }
        }
        
        /// <summary>
        /// Gizmo zum Visualisieren des Spawn-Bereichs
        /// </summary>
        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, spawnRadius);
            
            // Boden anzeigen
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnRadius * 3, 0.1f, spawnRadius * 3));
        }
    }
}
