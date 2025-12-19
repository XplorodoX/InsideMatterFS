using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace InsideMatter.UI
{
    /// <summary>
    /// Dialog der nach Level-Abschluss erscheint.
    /// Bietet Optionen: Nächstes Level, Nochmal spielen, Hauptmenü
    /// </summary>
    public class LevelCompleteDialog : MonoBehaviour
    {
        [Header("Dialog Einstellungen")]
        public float dialogWidth = 1.2f;
        public float dialogHeight = 0.9f;
        
        [Header("Farben")]
        public Color successBackgroundColor = new Color(0.1f, 0.35f, 0.15f);
        public Color failureBackgroundColor = new Color(0.35f, 0.1f, 0.1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.9f);
        
        [Header("UI Elemente")]
        public TextMeshPro titleText;
        public TextMeshPro messageText;
        public TextMeshPro scoreText;
        
        [Header("Buttons")]
        public VRSubmitButton nextLevelButton;
        public VRSubmitButton retryButton;
        public VRSubmitButton menuButton;
        
        [Header("Events")]
        public UnityEvent OnNextLevelPressed;
        public UnityEvent OnRetryPressed;
        public UnityEvent OnMenuPressed;
        
        private GameObject dialogPanel;
        private MeshRenderer backgroundRenderer;
        private Material backgroundMaterial;
        
        void Awake()
        {
            CreateDialog();
            Hide(); // Standardmäßig versteckt
        }
        
        /// <summary>
        /// Erstellt den Dialog visuell
        /// </summary>
        private void CreateDialog()
        {
            dialogPanel = new GameObject("DialogPanel");
            dialogPanel.transform.SetParent(transform);
            dialogPanel.transform.localPosition = Vector3.zero;
            
            // Hintergrund
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(dialogPanel.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(dialogWidth, dialogHeight, 0.03f);
            
            backgroundRenderer = background.GetComponent<MeshRenderer>();
            backgroundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            backgroundMaterial.SetColor("_BaseColor", successBackgroundColor);
            backgroundRenderer.material = backgroundMaterial;
            Destroy(background.GetComponent<Collider>());
            
            // Rahmen
            CreateFrame();
            
            // Titel
            titleText = CreateText("TitleText", 
                new Vector3(0, dialogHeight * 0.35f, -0.02f),
                "🎉 GESCHAFFT!", 1.8f);
            titleText.fontStyle = FontStyles.Bold;
            
            // Nachricht
            messageText = CreateText("MessageText",
                new Vector3(0, dialogHeight * 0.1f, -0.02f),
                "Du hast das Molekül richtig gebaut!", 0.7f);
            
            // Punkte
            scoreText = CreateText("ScoreText",
                new Vector3(0, -dialogHeight * 0.05f, -0.02f),
                "⭐ 100 Punkte", 0.9f);
            scoreText.color = new Color(1f, 0.9f, 0.3f);
            
            // Buttons erstellen
            CreateButtons();
        }
        
        private void CreateFrame()
        {
            float frameWidth = 0.04f;
            Color frameColor = new Color(0.5f, 0.35f, 0.15f);
            
            // Oben
            CreateFramePart("Top", 
                new Vector3(0, dialogHeight / 2f, 0),
                new Vector3(dialogWidth + frameWidth, frameWidth, 0.04f),
                frameColor);
            
            // Unten
            CreateFramePart("Bottom",
                new Vector3(0, -dialogHeight / 2f, 0),
                new Vector3(dialogWidth + frameWidth, frameWidth, 0.04f),
                frameColor);
            
            // Links
            CreateFramePart("Left",
                new Vector3(-dialogWidth / 2f, 0, 0),
                new Vector3(frameWidth, dialogHeight, 0.04f),
                frameColor);
            
            // Rechts
            CreateFramePart("Right",
                new Vector3(dialogWidth / 2f, 0, 0),
                new Vector3(frameWidth, dialogHeight, 0.04f),
                frameColor);
        }
        
        private void CreateFramePart(string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = $"Frame{name}";
            part.transform.SetParent(dialogPanel.transform);
            part.transform.localPosition = pos;
            part.transform.localScale = scale;
            
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            part.GetComponent<MeshRenderer>().material = mat;
            Destroy(part.GetComponent<Collider>());
        }
        
        private TextMeshPro CreateText(string name, Vector3 pos, string text, float size)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(dialogPanel.transform);
            textObj.transform.localPosition = pos;
            
            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            
            var rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(dialogWidth * 0.9f, dialogHeight * 0.3f);
            
            return tmp;
        }
        
        private void CreateButtons()
        {
            float buttonY = -dialogHeight * 0.3f;
            float buttonSpacing = 0.35f;
            
            // Nächstes Level
            GameObject nextObj = new GameObject("NextLevelButton");
            nextObj.transform.SetParent(dialogPanel.transform);
            nextObj.transform.localPosition = new Vector3(-buttonSpacing, buttonY, -0.05f);
            
            nextLevelButton = nextObj.AddComponent<VRSubmitButton>();
            nextLevelButton.buttonText = "WEITER ▶";
            nextLevelButton.buttonSize = 0.12f;
            nextLevelButton.normalColor = new Color(0.2f, 0.7f, 0.3f);
            nextLevelButton.OnButtonPressed.AddListener(() => {
                OnNextLevelPressed?.Invoke();
                Hide();
            });
            
            // Nochmal
            GameObject retryObj = new GameObject("RetryButton");
            retryObj.transform.SetParent(dialogPanel.transform);
            retryObj.transform.localPosition = new Vector3(0, buttonY, -0.05f);
            
            retryButton = retryObj.AddComponent<VRSubmitButton>();
            retryButton.buttonText = "↺ NOCHMAL";
            retryButton.buttonSize = 0.1f;
            retryButton.normalColor = new Color(0.5f, 0.5f, 0.6f);
            retryButton.OnButtonPressed.AddListener(() => {
                OnRetryPressed?.Invoke();
                Hide();
            });
            
            // Hauptmenü
            GameObject menuObj = new GameObject("MenuButton");
            menuObj.transform.SetParent(dialogPanel.transform);
            menuObj.transform.localPosition = new Vector3(buttonSpacing, buttonY, -0.05f);
            
            menuButton = menuObj.AddComponent<VRSubmitButton>();
            menuButton.buttonText = "🏠 MENÜ";
            menuButton.buttonSize = 0.1f;
            menuButton.normalColor = new Color(0.6f, 0.4f, 0.2f);
            menuButton.OnButtonPressed.AddListener(() => {
                OnMenuPressed?.Invoke();
                Hide();
            });
        }
        
        /// <summary>
        /// Zeigt den Erfolgs-Dialog
        /// </summary>
        public void ShowSuccess(string moleculeName, int score, bool hasNextLevel = true)
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(true);
            
            backgroundMaterial?.SetColor("_BaseColor", successBackgroundColor);
            
            titleText.text = "🎉 GESCHAFFT!";
            titleText.color = new Color(0.3f, 1f, 0.4f);
            
            messageText.text = $"Du hast {moleculeName} richtig gebaut!";
            scoreText.text = $"⭐ {score} Punkte";
            
            // Nächstes Level Button nur zeigen wenn es ein nächstes gibt
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(hasNextLevel);
            }
            
            Debug.Log($"[LevelCompleteDialog] Erfolg: {moleculeName}, {score} Punkte");
        }
        
        /// <summary>
        /// Zeigt den Fehler-Dialog
        /// </summary>
        public void ShowFailure(string errorMessage)
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(true);
            
            backgroundMaterial?.SetColor("_BaseColor", failureBackgroundColor);
            
            titleText.text = "✗ LEIDER FALSCH";
            titleText.color = new Color(1f, 0.4f, 0.3f);
            
            messageText.text = errorMessage;
            scoreText.text = "Versuche es nochmal!";
            scoreText.color = textColor;
            
            // Bei Fehler: Kein "Nächstes Level" Button
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Versteckt den Dialog
        /// </summary>
        public void Hide()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
        }
        
        /// <summary>
        /// Zeigt den Dialog
        /// </summary>
        public void Show()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(true);
        }
    }
}
