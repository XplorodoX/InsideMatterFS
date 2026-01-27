using UnityEngine;
using UnityEditor;
using InsideMatter.Molecule;
using InsideMatter.UI;
using InsideMatter.Puzzle;
using InsideMatter.Effects;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace InsideMatter.Editor
{
    /// <summary>
    /// Utility to decorate the DemoScene with a professional laboratory look using only primitives.
    /// This avoids using SyntyStudios as requested.
    /// </summary>
    public static class SceneDecorator
    {
        [MenuItem("InsideMatter/Setup Complete School Lab")]
        public static void DecorateScene()
        {
            Debug.Log(">>> STARTE COMPLETE SCHOOL LAB SETUP <<<");
            
            // === PHASE 1: UMGEBUNG ===
            GameObject world = GameObject.Find("Environment");
            if (world == null) world = new GameObject("Environment");

            // 1. Boden & Wände (Klassenzimmer-Stil)
            CreateSchoolRoom(world.transform);

            // 2. Labor-Tische
            CreateLabBench(world.transform, new Vector3(0, 0, 2), "Front Bench (Arbeitstisch)");
            CreateLabBench(world.transform, new Vector3(-3, 0, 0), "Left Bench");
            CreateValidationTable(world.transform, new Vector3(3, 0, 0), "Validation Table (Abgabe)");

            // 3. Atom-Petrischalen auf dem Arbeitstisch
            SetupIntegratedSpawner(new Vector3(0, 0, 2));

            // 4. Tafel an der Wand
            CreateSchoolChalkboard(world.transform);

            // 5. Regal mit Dekorationen
            GameObject shelving = CreateShelving(world.transform, new Vector3(-4, 1.5f, 4f));
            PopulateShelves(shelving.transform);
            
            // === PHASE 2: GAME UI ===
            GameObject gameUI = GameObject.Find("GameUI");
            if (gameUI == null) gameUI = new GameObject("GameUI");
            gameUI.transform.SetParent(world.transform);
            
            // 6. Validierungszone auf dem Abgabe-Tisch
            CreateValidationZone(gameUI.transform);
            
            // 7. Prüfen-Button neben der Validierungszone
            CreateSubmitButton(gameUI.transform);
            
            // 8. Hauptmenü (vor dem Spieler)
            CreateMainMenu(gameUI.transform);
            
            // 9. Level-Abschluss-Dialog (versteckt)
            CreateLevelCompleteDialog(gameUI.transform);
            
            // 10. Feedback-Effekte
            CreateFeedbackEffects(gameUI.transform);

            // === PHASE 3: MANAGER ===
            SetupManagers();
            LinkGameManager();

            Debug.Log("[SceneDecorator] ✓ Komplettes Schul-Labor erstellt! Starte das Spiel mit 'Play'.");
        }
        
        /// <summary>
        /// Erstellt einen Klassenraum-artigen Raum mit Schul-Atmosphäre
        /// </summary>
        private static void CreateSchoolRoom(Transform parent)
        {
            // Alte Elemente entfernen
            var oldFloor = GameObject.Find("Lab_Floor");
            if (oldFloor != null) Object.DestroyImmediate(oldFloor);
            
            // Boden - Holzoptik
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Lab_Floor";
            floor.transform.SetParent(parent);
            floor.transform.localScale = new Vector3(2, 1, 2);
            ApplyMaterial(floor, new Color(0.55f, 0.35f, 0.2f), 0.3f); // Holz-Braun

            // Rückwand - Hellgrau wie Schule
            CreateWall(parent, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.2f), "Wall_Back", new Color(0.9f, 0.9f, 0.85f));
            
            // Linke Wand
            CreateWall(parent, new Vector3(-5, 2.5f, 0), new Vector3(0.2f, 5, 10), "Wall_Left", new Color(0.9f, 0.9f, 0.85f));
            
            // Rechte Wand
            CreateWall(parent, new Vector3(5, 2.5f, 0), new Vector3(0.2f, 5, 10), "Wall_Right", new Color(0.9f, 0.9f, 0.85f));
            
            // Decke
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(parent);
            ceiling.transform.position = new Vector3(0, 5, 0);
            ceiling.transform.rotation = Quaternion.Euler(180, 0, 0);
            ceiling.transform.localScale = new Vector3(2, 1, 2);
            ApplyMaterial(ceiling, new Color(0.95f, 0.95f, 0.95f), 0.1f);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>());
        }
        
        /// <summary>
        /// Erstellt die Schul-Tafel für Aufgaben-Anzeige
        /// </summary>
        private static void CreateSchoolChalkboard(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Schul-Tafel...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<ChalkboardUI>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            
            // Tafel-Container
            GameObject chalkboard = new GameObject("SchoolChalkboard");
            chalkboard.transform.SetParent(parent);
            chalkboard.transform.position = new Vector3(0, 2.2f, 4.85f);
            chalkboard.transform.rotation = Quaternion.Euler(0, 180, 0);
            
            // ChalkboardUI hinzufügen (erstellt sich selbst)
            chalkboard.AddComponent<ChalkboardUI>();
            
            Debug.Log("[SceneDecorator] ✓ Schul-Tafel erstellt!");
        }
        
        /// <summary>
        /// Erstellt den Abgabe-Tisch mit spezieller Markierung
        /// </summary>
        private static void CreateValidationTable(Transform parent, Vector3 pos, string name)
        {
            // Alte entfernen
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
            
            GameObject bench = new GameObject(name);
            bench.transform.SetParent(parent);
            bench.transform.position = pos;

            // Tischplatte - Grün markiert für "Abgabe"
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Surface_Validation";
            surface.transform.SetParent(bench.transform);
            surface.transform.localPosition = new Vector3(0, 0.9f, 0);
            surface.transform.localScale = new Vector3(2f, 0.1f, 1.2f);
            ApplyMaterial(surface, new Color(0.2f, 0.5f, 0.3f), 0.4f); // Grün für Abgabe
            
            // Beschriftung "ABGABE" auf dem Tisch
            GameObject labelObj = new GameObject("TableLabel");
            labelObj.transform.SetParent(bench.transform);
            labelObj.transform.localPosition = new Vector3(0, 0.96f, 0);
            labelObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            var label = labelObj.AddComponent<TMPro.TextMeshPro>();
            label.text = "📥 ABGABE";
            label.fontSize = 1.5f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = Color.white;
            
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(2f, 0.5f);

            // Tischbeine
            CreateLeg(bench.transform, new Vector3(-0.9f, 0.45f, 0.5f));
            CreateLeg(bench.transform, new Vector3(0.9f, 0.45f, 0.5f));
            CreateLeg(bench.transform, new Vector3(-0.9f, 0.45f, -0.5f));
            CreateLeg(bench.transform, new Vector3(0.9f, 0.45f, -0.5f));
        }
        
        [MenuItem("InsideMatter/Setup Game UI")]
        public static void SetupGameUI()
        {
            Debug.Log(">>> STARTE SETUP GAME UI <<<");
            
            GameObject uiRoot = GameObject.Find("GameUI");
            if (uiRoot == null) uiRoot = new GameObject("GameUI");
            
            // 1. Tafel an der Wand
            CreateChalkboard(uiRoot.transform);
            
            // 2. Validierungszone auf dem rechten Tisch
            CreateValidationZone(uiRoot.transform);
            
            // 3. Submit-Button neben der Validierungszone
            CreateSubmitButton(uiRoot.transform);
            
            // 4. Hauptmenü
            CreateMainMenu(uiRoot.transform);
            
            // 5. Level-Dialog (versteckt)
            CreateLevelCompleteDialog(uiRoot.transform);
            
            // 6. Feedback-Effekte
            CreateFeedbackEffects(uiRoot.transform);
            
            // 7. Game Manager verknüpfen
            LinkGameManager();
            
            Debug.Log("[SceneDecorator] Game UI erstellt! Siehe 'GameUI' GameObject.");
        }
        
        private static void CreateChalkboard(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Tafel...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<ChalkboardUI>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            
            // Neue erstellen
            GameObject chalkboard = new GameObject("Chalkboard");
            chalkboard.transform.SetParent(parent);
            chalkboard.transform.position = new Vector3(0, 2f, 4.8f); // An der Rückwand
            chalkboard.transform.rotation = Quaternion.Euler(0, 180, 0); // Zum Spieler zeigend
            
            var ui = chalkboard.AddComponent<ChalkboardUI>();
            // Die ChalkboardUI erstellt sich selbst in Awake
            
            Debug.Log("[SceneDecorator] Tafel erstellt!");
        }
        
        private static void CreateValidationZone(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Validierungszone...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<ValidationZone>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            
            // Auf dem rechten Tisch platzieren
            GameObject rightBench = GameObject.Find("Right Bench");
            Vector3 pos = new Vector3(3, 1f, 0);
            
            if (rightBench != null)
            {
                pos = rightBench.transform.position + new Vector3(0, 1f, 0);
            }
            
            GameObject zone = new GameObject("ValidationZone");
            zone.transform.SetParent(parent);
            zone.transform.position = pos;
            
            zone.AddComponent<ValidationZone>();
            
            Debug.Log("[SceneDecorator] Validierungszone erstellt!");
        }
        
        private static void CreateSubmitButton(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Submit-Button...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<VRSubmitButton>();
            if (old != null && old.gameObject.name == "SubmitButton")
            {
                Object.DestroyImmediate(old.gameObject);
            }
            
            // Neben der Validierungszone
            var zone = Object.FindFirstObjectByType<ValidationZone>();
            Vector3 pos = new Vector3(3.5f, 1f, -0.4f);
            
            if (zone != null)
            {
                pos = zone.transform.position + new Vector3(0.5f, 0f, -0.4f);
            }
            
            GameObject button = new GameObject("SubmitButton");
            button.transform.SetParent(parent);
            button.transform.position = pos;
            
            var submitBtn = button.AddComponent<VRSubmitButton>();
            submitBtn.buttonText = "PRÜFEN ✓";
            submitBtn.buttonSize = 0.12f;
            submitBtn.normalColor = new Color(0.2f, 0.7f, 0.3f);
            
            // Mit GameManager verbinden
            submitBtn.OnButtonPressed.AddListener(() => {
                var gm = Object.FindFirstObjectByType<PuzzleGameManager>();
                if (gm != null) gm.CheckCurrentMolecule();
            });
            
            Debug.Log("[SceneDecorator] Submit-Button erstellt!");
        }
        
        private static void CreateMainMenu(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Hauptmenü...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<MainMenuManager>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            
            GameObject menu = new GameObject("MainMenu");
            menu.transform.SetParent(parent);
            menu.transform.position = new Vector3(0, 1.5f, 3f);
            menu.transform.rotation = Quaternion.Euler(0, 180, 0);
            
            var menuManager = menu.AddComponent<MainMenuManager>();
            menuManager.CreateMenuVisual(menu.transform.position);
            
            Debug.Log("[SceneDecorator] Hauptmenü erstellt!");
        }
        
        private static void CreateLevelCompleteDialog(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Level-Dialog...");
            
            // Alte entfernen
            var old = Object.FindFirstObjectByType<LevelCompleteDialog>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            
            GameObject dialog = new GameObject("LevelCompleteDialog");
            dialog.transform.SetParent(parent);
            dialog.transform.position = new Vector3(0, 1.5f, 2f);
            dialog.transform.rotation = Quaternion.Euler(0, 180, 0);
            
            dialog.AddComponent<LevelCompleteDialog>();
            // Dialog startet versteckt (passiert in Awake)
            
            Debug.Log("[SceneDecorator] Level-Dialog erstellt!");
        }
        
        private static void CreateFeedbackEffects(Transform parent)
        {
            Debug.Log("[SceneDecorator] > Erstelle Feedback-Effekte...");
            
            // Alte entfernen
            if (FeedbackEffects.Instance != null)
            {
                Object.DestroyImmediate(FeedbackEffects.Instance.gameObject);
            }
            
            GameObject effects = new GameObject("FeedbackEffects");
            effects.transform.SetParent(parent);
            
            effects.AddComponent<FeedbackEffects>();
            
            Debug.Log("[SceneDecorator] Feedback-Effekte erstellt!");
        }
        
        private static void LinkGameManager()
        {
            Debug.Log("[SceneDecorator] > Verknüpfe Game Manager...");
            
            var gm = Object.FindFirstObjectByType<PuzzleGameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("PuzzleGameManager");
                gm = gmObj.AddComponent<PuzzleGameManager>();
            }
            
            // Komponenten verknüpfen
            gm.validationZone = Object.FindFirstObjectByType<ValidationZone>();
            // gm.chalkboardUI = Object.FindFirstObjectByType<ChalkboardUI>(); // Deprecated
            gm.levelCompleteDialog = Object.FindFirstObjectByType<LevelCompleteDialog>();
            gm.feedbackEffects = Object.FindFirstObjectByType<FeedbackEffects>();
            
            // Level erstellen falls nicht vorhanden
            CreateLevelAssets(gm);
            
            Debug.Log("[SceneDecorator] ✓ Game Manager verknüpft!");
        }
        
        /// <summary>
        /// Erstellt die Level-Assets (ScriptableObjects) für H2O und CH4
        /// </summary>
        private static void CreateLevelAssets(PuzzleGameManager gm)
        {
            string levelPath = "Assets/Resources/Levels";
            string moleculePath = "Assets/Resources/Molecules";
            
            // Ordner erstellen
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(levelPath))
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
            if (!AssetDatabase.IsValidFolder(moleculePath))
                AssetDatabase.CreateFolder("Assets/Resources", "Molecules");
            
            // === MOLEKÜL 1: WASSER (H₂O) ===
            // Struktur: H-O-H (gewinkelt, ca. 104.5°)
            // Atom-Indizes: 0=H, 1=H, 2=O
            MoleculeDefinition waterMolecule = AssetDatabase.LoadAssetAtPath<MoleculeDefinition>($"{moleculePath}/Molecule_Water.asset");
            if (waterMolecule == null)
            {
                waterMolecule = ScriptableObject.CreateInstance<MoleculeDefinition>();
                waterMolecule.moleculeName = "Wasser";
                waterMolecule.chemicalFormula = "H₂O";
                waterMolecule.description = "Wasser besteht aus zwei Wasserstoff-Atomen und einem Sauerstoff-Atom. Es ist die Grundlage allen Lebens!";
                waterMolecule.difficulty = 1;
                waterMolecule.scoreReward = 100;
                waterMolecule.requiredAtoms = new System.Collections.Generic.List<AtomRequirement>
                {
                    new AtomRequirement { element = "H", count = 2 },
                    new AtomRequirement { element = "O", count = 1 }
                };
                // Bindungsstruktur: H-O-H
                // Atom-Reihenfolge: [H(0), H(1), O(2)]
                waterMolecule.graphStructure = new System.Collections.Generic.Dictionary<string, Dictionary<string, BondType>>
                {
                    {"H_0", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"H_1", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"O_0", new Dictionary<string, BondType>{{"H_0", BondType.Single}, {"H_1", BondType.Single}}},
                };
                
                AssetDatabase.CreateAsset(waterMolecule, $"{moleculePath}/Molecule_Water.asset");
                Debug.Log("[SceneDecorator] ✓ Wasser-Molekül erstellt (mit Bindungsstruktur)!");
            }
            
            // === MOLEKÜL 2: METHAN (CH₄) ===
            // Struktur: Tetraeder - C in der Mitte, 4x H drum herum
            // Atom-Indizes: 0=C, 1=H, 2=H, 3=H, 4=H
            MoleculeDefinition methaneMolecule = AssetDatabase.LoadAssetAtPath<MoleculeDefinition>($"{moleculePath}/Molecule_Methane.asset");
            if (methaneMolecule == null)
            {
                methaneMolecule = ScriptableObject.CreateInstance<MoleculeDefinition>();
                methaneMolecule.moleculeName = "Methan";
                methaneMolecule.chemicalFormula = "CH₄";
                methaneMolecule.description = "Methan ist der einfachste Kohlenwasserstoff. Ein Kohlenstoff-Atom ist mit vier Wasserstoff-Atomen verbunden.";
                methaneMolecule.difficulty = 2;
                methaneMolecule.scoreReward = 150;
                methaneMolecule.requiredAtoms = new System.Collections.Generic.List<AtomRequirement>
                {
                    new AtomRequirement { element = "C", count = 1 },
                    new AtomRequirement { element = "H", count = 4 }
                };
                // Bindungsstruktur: Tetraeder C-H4
                // Atom-Reihenfolge: [C(0), H(1), H(2), H(3), H(4)]
                methaneMolecule.graphStructure = new Dictionary<string, Dictionary<string, BondType>>
                {
                    {"H_0", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"H_1", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"H_2", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"H_3", new Dictionary<string, BondType>{{"O_0", BondType.Single}}},
                    {"C_0", new Dictionary<string, BondType>{{"H_0", BondType.Single}, {"H_1", BondType.Single}, {"H_2", BondType.Single}, {"H_3", BondType.Single}}},
                };
                
                AssetDatabase.CreateAsset(methaneMolecule, $"{moleculePath}/Molecule_Methane.asset");
                Debug.Log("[SceneDecorator] ✓ Methan-Molekül erstellt (mit Bindungsstruktur)!");
            }
            
            // === MOLEKÜL 3: KOHLENDIOXID (CO₂) ===
            // Struktur: O=C=O (linear, zwei Doppelbindungen)
            // Atom-Indizes: 0=C, 1=O, 2=O
            MoleculeDefinition co2Molecule = AssetDatabase.LoadAssetAtPath<MoleculeDefinition>($"{moleculePath}/Molecule_CO2.asset");
            if (co2Molecule == null)
            {
                co2Molecule = ScriptableObject.CreateInstance<MoleculeDefinition>();
                co2Molecule.moleculeName = "Kohlendioxid";
                co2Molecule.chemicalFormula = "CO₂";
                co2Molecule.description = "Kohlendioxid besteht aus einem Kohlenstoff-Atom mit zwei Doppelbindungen zu je einem Sauerstoff-Atom. Es ist ein wichtiges Treibhausgas!";
                co2Molecule.difficulty = 3;
                co2Molecule.scoreReward = 200;
                co2Molecule.requiredAtoms = new System.Collections.Generic.List<AtomRequirement>
                {
                    new AtomRequirement { element = "C", count = 1 },
                    new AtomRequirement { element = "O", count = 2 }
                };
                // Bindungsstruktur: O=C=O (2x Doppelbindung!)
                // Atom-Reihenfolge: [C(0), O(1), O(2)]
                co2Molecule.graphStructure = new Dictionary<string, Dictionary<string, BondType>>
                {
                    {"O_0", new Dictionary<string, BondType>{{"C_0", BondType.Double}}},
                    {"O_1", new Dictionary<string, BondType>{{"C_0", BondType.Double}}},
                    {"C_0", new Dictionary<string, BondType>{{"O_0", BondType.Double}, {"O_1", BondType.Double}}},
                };
                
                AssetDatabase.CreateAsset(co2Molecule, $"{moleculePath}/Molecule_CO2.asset");
                Debug.Log("[SceneDecorator] ✓ CO₂-Molekül erstellt (mit Doppelbindungen)!");
            }
            
            // === MOLEKÜL 4: SAUERSTOFF (O₂) ===
            // Struktur: O=O (eine Doppelbindung)
            // Atom-Indizes: 0=O, 1=O
            MoleculeDefinition o2Molecule = AssetDatabase.LoadAssetAtPath<MoleculeDefinition>($"{moleculePath}/Molecule_O2.asset");
            if (o2Molecule == null)
            {
                o2Molecule = ScriptableObject.CreateInstance<MoleculeDefinition>();
                o2Molecule.moleculeName = "Sauerstoff";
                o2Molecule.chemicalFormula = "O₂";
                o2Molecule.description = "Sauerstoff-Molekül: Zwei Sauerstoff-Atome sind durch eine Doppelbindung verbunden. Wir atmen es zum Leben!";
                o2Molecule.difficulty = 2;
                o2Molecule.scoreReward = 120;
                o2Molecule.requiredAtoms = new System.Collections.Generic.List<AtomRequirement>
                {
                    new AtomRequirement { element = "O", count = 2 }
                };
                // Bindungsstruktur: O=O (1x Doppelbindung)
                o2Molecule.graphStructure = new Dictionary<string, Dictionary<string, BondType>>
                {
                    {"O_0", new Dictionary<string, BondType>{{"O_1", BondType.Double}}},
                    {"O_1", new Dictionary<string, BondType>{{"O_0", BondType.Double}}},
                };
                
                AssetDatabase.CreateAsset(o2Molecule, $"{moleculePath}/Molecule_O2.asset");
                Debug.Log("[SceneDecorator] ✓ O₂-Molekül erstellt (mit Doppelbindung)!");
            }
            
            // === MOLEKÜL 5: STICKSTOFF (N₂) ===
            // Struktur: N≡N (eine Dreifachbindung!)
            // Atom-Indizes: 0=N, 1=N
            MoleculeDefinition n2Molecule = AssetDatabase.LoadAssetAtPath<MoleculeDefinition>($"{moleculePath}/Molecule_N2.asset");
            if (n2Molecule == null)
            {
                n2Molecule = ScriptableObject.CreateInstance<MoleculeDefinition>();
                n2Molecule.moleculeName = "Stickstoff";
                n2Molecule.chemicalFormula = "N₂";
                n2Molecule.description = "Stickstoff-Molekül: Zwei Stickstoff-Atome sind durch eine Dreifachbindung verbunden - die stärkste chemische Bindung! Fast 80% der Luft besteht daraus.";
                n2Molecule.difficulty = 3;
                n2Molecule.scoreReward = 180;
                n2Molecule.requiredAtoms = new System.Collections.Generic.List<AtomRequirement>
                {
                    new AtomRequirement { element = "N", count = 2 }
                };
                // Bindungsstruktur: N≡N (1x Dreifachbindung!)
                n2Molecule.graphStructure = new Dictionary<string, Dictionary<string, BondType>>
                {
                    {"N_0", new Dictionary<string, BondType>{{"N_1", BondType.Double}}},
                    {"N_1", new Dictionary<string, BondType>{{"N_0", BondType.Double}}},
                };
                
                AssetDatabase.CreateAsset(n2Molecule, $"{moleculePath}/Molecule_N2.asset");
                Debug.Log("[SceneDecorator] ✓ N₂-Molekül erstellt (mit Dreifachbindung)!");
            }
            
            // === LEVEL 1: WASSER ===
            PuzzleLevel level1 = AssetDatabase.LoadAssetAtPath<PuzzleLevel>($"{levelPath}/Level_01_Water.asset");
            if (level1 == null)
            {
                level1 = ScriptableObject.CreateInstance<PuzzleLevel>();
                level1.levelName = "Level 1: Wasser";
                level1.levelDescription = "Baue dein erstes Molekül: Wasser (H₂O)!";
                level1.levelNumber = 1;
                level1.maxScore = 100;
                level1.sequentialOrder = true;
                level1.moleculesToBuild = new System.Collections.Generic.List<MoleculeDefinition> { waterMolecule };
                
                AssetDatabase.CreateAsset(level1, $"{levelPath}/Level_01_Water.asset");
                Debug.Log("[SceneDecorator] ✓ Level 1 (Wasser) erstellt!");
            }
            
            // === LEVEL 2: METHAN ===
            PuzzleLevel level2 = AssetDatabase.LoadAssetAtPath<PuzzleLevel>($"{levelPath}/Level_02_Methane.asset");
            if (level2 == null)
            {
                level2 = ScriptableObject.CreateInstance<PuzzleLevel>();
                level2.levelName = "Level 2: Methan";
                level2.levelDescription = "Baue ein Methan-Molekül (CH₄)!";
                level2.levelNumber = 2;
                level2.maxScore = 150;
                level2.sequentialOrder = true;
                level2.moleculesToBuild = new System.Collections.Generic.List<MoleculeDefinition> { methaneMolecule };
                level2.requiredLevel = level1;
                
                AssetDatabase.CreateAsset(level2, $"{levelPath}/Level_02_Methane.asset");
                Debug.Log("[SceneDecorator] ✓ Level 2 (Methan) erstellt!");
            }
            
            // Level dem GameManager zuweisen
            gm.currentLevel = level1;
            gm.allLevels = new System.Collections.Generic.List<PuzzleLevel> { level1, level2 };
            
            // MainMenu mit Level verbinden
            var menu = Object.FindFirstObjectByType<MainMenuManager>();
            if (menu != null)
            {
                menu.startLevel = level1;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[SceneDecorator] ✓ 2 Level-Assets erstellt/verknüpft!");
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
            CreateWall(parent, pos, scale, name, new Color(0.85f, 0.85f, 0.9f));
        }
        
        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, string name, Color color)
        {
            // Alte Wand entfernen falls vorhanden
            var oldWall = GameObject.Find(name);
            if (oldWall != null) Object.DestroyImmediate(oldWall);
            
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, color, 0.3f);
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
            InsideMatter.Molecule.AtomSpawner spawner = GameObject.FindFirstObjectByType<InsideMatter.Molecule.AtomSpawner>();
            if (spawner == null)
            {
                spawner = new GameObject("MainAtomSpawner").AddComponent<InsideMatter.Molecule.AtomSpawner>();
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
