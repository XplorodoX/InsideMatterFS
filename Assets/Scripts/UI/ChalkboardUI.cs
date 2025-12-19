using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace InsideMatter.UI
{
    /// <summary>
    /// Tafel-UI im Schul-Stil für Aufgaben-Anzeige.
    /// Zeigt die aktuelle Aufgabe, Ziel-Molekül und benötigte Atome.
    /// </summary>
    public class ChalkboardUI : MonoBehaviour
    {
        [Header("Tafel Einstellungen")]
        [Tooltip("Breite der Tafel")]
        public float boardWidth = 2f;
        
        [Tooltip("Höhe der Tafel")]
        public float boardHeight = 1.5f;
        
        [Tooltip("Tiefe des Rahmens")]
        public float frameDepth = 0.08f;
        
        [Header("Farben")]
        public Color boardColor = new Color(0.1f, 0.25f, 0.15f); // Dunkelgrün (Tafel)
        public Color frameColor = new Color(0.4f, 0.25f, 0.1f); // Holz-Braun
        public Color chalkColor = new Color(0.95f, 0.95f, 0.9f); // Kreide-Weiß
        public Color highlightColor = new Color(1f, 0.9f, 0.3f); // Gelb für Hervorhebungen
        
        [Header("UI Elemente")]
        public TextMeshPro titleText;
        public TextMeshPro moleculeNameText;
        public TextMeshPro formulaText;
        public TextMeshPro descriptionText;
        public TextMeshPro atomListText;
        public TextMeshPro hintText;
        
        [Header("Molekül-Vorschau")]
        public Transform previewSpawnPoint;
        public float previewScale = 0.3f;
        public float previewRotationSpeed = 15f;
        
        private GameObject currentPreview;
        private MeshRenderer boardRenderer;
        
        void Awake()
        {
            CreateChalkboard();
        }
        
        void Update()
        {
            // Molekül-Vorschau rotieren
            if (currentPreview != null)
            {
                currentPreview.transform.Rotate(Vector3.up, previewRotationSpeed * Time.deltaTime, Space.World);
            }
        }
        
        /// <summary>
        /// Erstellt die Tafel-Visualisierung
        /// </summary>
        private void CreateChalkboard()
        {
            // Haupt-Tafel
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(transform);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(boardWidth, boardHeight, 0.02f);
            
            boardRenderer = board.GetComponent<MeshRenderer>();
            var boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            boardMat.SetColor("_BaseColor", boardColor);
            boardRenderer.material = boardMat;
            
            Destroy(board.GetComponent<Collider>()); // Kein Collider nötig
            
            // Rahmen erstellen
            CreateFrame();
            
            // Text-Elemente erstellen
            CreateTextElements();
            
            // Preview-Punkt erstellen
            if (previewSpawnPoint == null)
            {
                GameObject previewPoint = new GameObject("PreviewSpawnPoint");
                previewPoint.transform.SetParent(transform);
                previewPoint.transform.localPosition = new Vector3(boardWidth * 0.3f, 0, -0.1f);
                previewSpawnPoint = previewPoint.transform;
            }
        }
        
        /// <summary>
        /// Erstellt den Holzrahmen
        /// </summary>
        private void CreateFrame()
        {
            float frameWidth = 0.06f;
            
            // Oben
            CreateFramePart("FrameTop", 
                new Vector3(0, boardHeight / 2f + frameWidth / 2f, 0),
                new Vector3(boardWidth + frameWidth * 2f, frameWidth, frameDepth));
            
            // Unten
            CreateFramePart("FrameBottom", 
                new Vector3(0, -boardHeight / 2f - frameWidth / 2f, 0),
                new Vector3(boardWidth + frameWidth * 2f, frameWidth, frameDepth));
            
            // Links
            CreateFramePart("FrameLeft", 
                new Vector3(-boardWidth / 2f - frameWidth / 2f, 0, 0),
                new Vector3(frameWidth, boardHeight, frameDepth));
            
            // Rechts
            CreateFramePart("FrameRight", 
                new Vector3(boardWidth / 2f + frameWidth / 2f, 0, 0),
                new Vector3(frameWidth, boardHeight, frameDepth));
            
            // Kreide-Ablage unten
            CreateFramePart("ChalkTray", 
                new Vector3(0, -boardHeight / 2f - frameWidth - 0.02f, frameDepth / 2f),
                new Vector3(boardWidth * 0.6f, 0.03f, 0.08f));
        }
        
        private void CreateFramePart(string name, Vector3 position, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", frameColor);
            part.GetComponent<MeshRenderer>().material = mat;
            
            Destroy(part.GetComponent<Collider>());
        }
        
        /// <summary>
        /// Erstellt alle Text-Elemente
        /// </summary>
        private void CreateTextElements()
        {
            float zOffset = -0.02f; // Leicht vor der Tafel
            
            // Titel "AUFGABE"
            titleText = CreateTextElement("TitleText", 
                new Vector3(-boardWidth * 0.35f, boardHeight * 0.38f, zOffset),
                "📚 AUFGABE", 1.2f, TextAlignmentOptions.Left);
            titleText.fontStyle = FontStyles.Bold;
            
            // Molekül-Name (groß)
            moleculeNameText = CreateTextElement("MoleculeNameText",
                new Vector3(-boardWidth * 0.35f, boardHeight * 0.2f, zOffset),
                "Wasser", 2f, TextAlignmentOptions.Left);
            moleculeNameText.fontStyle = FontStyles.Bold;
            moleculeNameText.color = highlightColor;
            
            // Chemische Formel
            formulaText = CreateTextElement("FormulaText",
                new Vector3(-boardWidth * 0.35f, boardHeight * 0.02f, zOffset),
                "H₂O", 1.5f, TextAlignmentOptions.Left);
            
            // Beschreibung
            descriptionText = CreateTextElement("DescriptionText",
                new Vector3(-boardWidth * 0.35f, -boardHeight * 0.12f, zOffset),
                "Baue ein Wassermolekül!", 0.6f, TextAlignmentOptions.Left);
            
            // Atom-Liste
            atomListText = CreateTextElement("AtomListText",
                new Vector3(-boardWidth * 0.35f, -boardHeight * 0.28f, zOffset),
                "Benötigt:\n• 2× Wasserstoff (H)\n• 1× Sauerstoff (O)", 0.5f, TextAlignmentOptions.Left);
            
            // Hinweis (optional)
            hintText = CreateTextElement("HintText",
                new Vector3(0, -boardHeight * 0.42f, zOffset),
                "💡 Tipp: Verbinde die Atome mit den gelben Punkten!", 0.4f, TextAlignmentOptions.Center);
            hintText.color = new Color(0.7f, 0.9f, 1f);
        }
        
        private TextMeshPro CreateTextElement(string name, Vector3 position, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = position;
            textObj.transform.localRotation = Quaternion.identity;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = chalkColor;
            tmp.enableWordWrapping = true;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(boardWidth * 0.9f, boardHeight * 0.3f);
            
            return tmp;
        }
        
        /// <summary>
        /// Zeigt eine neue Aufgabe an
        /// </summary>
        public void ShowTask(Puzzle.MoleculeDefinition molecule, int taskNumber = 1)
        {
            if (molecule == null) return;
            
            titleText.text = $"📚 AUFGABE {taskNumber}";
            moleculeNameText.text = molecule.moleculeName;
            formulaText.text = molecule.chemicalFormula;
            descriptionText.text = molecule.description;
            
            // Atom-Liste formatieren
            string atomList = "Benötigt:";
            foreach (var req in molecule.requiredAtoms)
            {
                string elementName = GetElementName(req.element);
                atomList += $"\n• {req.count}× {elementName} ({req.element})";
            }
            atomListText.text = atomList;
            
            // Vorschau erstellen
            CreateMoleculePreview(molecule);
        }
        
        /// <summary>
        /// Erstellt eine einfache Molekül-Vorschau
        /// </summary>
        private void CreateMoleculePreview(Puzzle.MoleculeDefinition molecule)
        {
            // Alte Vorschau entfernen
            if (currentPreview != null)
            {
                Destroy(currentPreview);
            }
            
            if (previewSpawnPoint == null) return;
            
            // Einfache Visualisierung: Atome als Kugeln
            currentPreview = new GameObject($"Preview_{molecule.moleculeName}");
            currentPreview.transform.SetParent(previewSpawnPoint);
            currentPreview.transform.localPosition = Vector3.zero;
            currentPreview.transform.localScale = Vector3.one * previewScale;
            
            // Atome basierend auf Molekül erstellen
            if (molecule.moleculeName.Contains("Wasser") || molecule.chemicalFormula == "H₂O")
            {
                // Wasser: O in der Mitte, 2x H an den Seiten (Winkel ~104.5°)
                CreatePreviewAtom(currentPreview.transform, Vector3.zero, Color.red, 0.5f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(-0.4f, 0.3f, 0), Color.white, 0.35f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(0.4f, 0.3f, 0), Color.white, 0.35f);
            }
            else if (molecule.moleculeName.Contains("Methan") || molecule.chemicalFormula == "CH₄")
            {
                // Methan: C in der Mitte, 4x H tetraedrisch
                CreatePreviewAtom(currentPreview.transform, Vector3.zero, Color.gray, 0.5f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(0.4f, 0.4f, 0.4f), Color.white, 0.3f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(-0.4f, -0.4f, 0.4f), Color.white, 0.3f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(-0.4f, 0.4f, -0.4f), Color.white, 0.3f);
                CreatePreviewAtom(currentPreview.transform, new Vector3(0.4f, -0.4f, -0.4f), Color.white, 0.3f);
            }
            else
            {
                // Fallback: Einfache Kugel
                CreatePreviewAtom(currentPreview.transform, Vector3.zero, Color.cyan, 0.5f);
            }
        }
        
        private void CreatePreviewAtom(Transform parent, Vector3 position, Color color, float size)
        {
            GameObject atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atom.transform.SetParent(parent);
            atom.transform.localPosition = position;
            atom.transform.localScale = Vector3.one * size;
            
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            atom.GetComponent<MeshRenderer>().material = mat;
            
            Destroy(atom.GetComponent<Collider>());
        }
        
        private string GetElementName(string symbol)
        {
            switch (symbol.ToUpper())
            {
                case "H": return "Wasserstoff";
                case "C": return "Kohlenstoff";
                case "O": return "Sauerstoff";
                case "N": return "Stickstoff";
                default: return symbol;
            }
        }
        
        /// <summary>
        /// Zeigt Erfolgs-Nachricht auf der Tafel
        /// </summary>
        public void ShowSuccess(string message)
        {
            moleculeNameText.text = "✓ RICHTIG!";
            moleculeNameText.color = new Color(0.3f, 1f, 0.4f);
            descriptionText.text = message;
            atomListText.text = "";
            hintText.text = "";
        }
        
        /// <summary>
        /// Zeigt Fehler-Nachricht auf der Tafel
        /// </summary>
        public void ShowError(string message)
        {
            descriptionText.text = $"✗ {message}";
            descriptionText.color = new Color(1f, 0.4f, 0.3f);
        }
        
        /// <summary>
        /// Setzt die Tafel-Farben zurück
        /// </summary>
        public void ResetColors()
        {
            moleculeNameText.color = highlightColor;
            descriptionText.color = chalkColor;
        }
    }
}
