using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace InsideMatter.UI
{
    /// <summary>
    /// Hauptmenü-Manager für das VR Molekül-Bauspiel.
    /// Zeigt das Startmenü und verwaltet den Spielfluss.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [Tooltip("Das Hauptmenü-Panel")]
        public GameObject mainMenuPanel;
        
        [Tooltip("Das Spiel-UI (wird bei Start aktiviert)")]
        public GameObject gameUI;
        
        [Header("Menu Buttons")]
        public VRSubmitButton startButton;
        public VRSubmitButton levelSelectButton;
        public VRSubmitButton quitButton;
        
        [Header("Menu Text")]
        public TextMeshPro titleText;
        public TextMeshPro subtitleText;
        
        [Header("Einstellungen")]
        [Tooltip("Erstes Level das gestartet wird")]
        public Puzzle.PuzzleLevel startLevel;
        
        [Header("Events")]
        public UnityEvent OnGameStarted;
        public UnityEvent OnMenuOpened;
        
        private bool isMenuActive = true;
        
        void Start()
        {
            ShowMenu();
            
            // Button-Events verbinden
            if (startButton != null)
            {
                startButton.OnButtonPressed.AddListener(StartGame);
            }
            
            if (quitButton != null)
            {
                quitButton.OnButtonPressed.AddListener(QuitGame);
            }
        }
        
        /// <summary>
        /// Zeigt das Hauptmenü
        /// </summary>
        public void ShowMenu()
        {
            isMenuActive = true;
            
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
            
            if (gameUI != null)
                gameUI.SetActive(false);
            
            OnMenuOpened?.Invoke();
            
            Debug.Log("[MainMenu] Menu angezeigt");
        }
        
        /// <summary>
        /// Startet das Spiel
        /// </summary>
        public void StartGame()
        {
            isMenuActive = false;
            
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            
            if (gameUI != null)
                gameUI.SetActive(true);
            
            // Puzzle-Manager starten
            var gameManager = FindFirstObjectByType<Puzzle.PuzzleGameManager>();
            if (gameManager != null && startLevel != null)
            {
                gameManager.StartLevel(startLevel);
            }
            else if (gameManager != null)
            {
                // Fallback: Nutze das im Manager zugewiesene Level
                if (gameManager.currentLevel != null)
                {
                    gameManager.StartLevel(gameManager.currentLevel);
                }
            }
            
            OnGameStarted?.Invoke();
            
            Debug.Log("[MainMenu] Spiel gestartet!");
        }
        
        /// <summary>
        /// Beendet das Spiel
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] Spiel beenden...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        /// <summary>
        /// Erstellt das Menü visuell (für Editor-Setup)
        /// </summary>
        public void CreateMenuVisual(Vector3 position)
        {
            transform.position = position;
            
            // Hauptpanel
            if (mainMenuPanel == null)
            {
                mainMenuPanel = new GameObject("MainMenuPanel");
                mainMenuPanel.transform.SetParent(transform);
                mainMenuPanel.transform.localPosition = Vector3.zero;
            }
            
            // Hintergrund-Tafel
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "MenuBackground";
            background.transform.SetParent(mainMenuPanel.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(1.5f, 1.2f, 0.05f);
            
            var bgMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bgMat.SetColor("_BaseColor", new Color(0.15f, 0.3f, 0.2f));
            background.GetComponent<MeshRenderer>().material = bgMat;
            Destroy(background.GetComponent<Collider>());
            
            // Titel
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mainMenuPanel.transform);
            titleObj.transform.localPosition = new Vector3(0, 0.4f, -0.03f);
            
            titleText = titleObj.AddComponent<TextMeshPro>();
            titleText.text = "🧪 MOLEKÜL-LABOR";
            titleText.fontSize = 2f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.95f, 0.8f);
            titleText.fontStyle = FontStyles.Bold;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(2f, 0.5f);
            
            // Untertitel
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(mainMenuPanel.transform);
            subtitleObj.transform.localPosition = new Vector3(0, 0.2f, -0.03f);
            
            subtitleText = subtitleObj.AddComponent<TextMeshPro>();
            subtitleText.text = "Lerne Moleküle zu bauen!";
            subtitleText.fontSize = 0.8f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.9f, 0.9f, 0.85f);
            
            RectTransform subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(2f, 0.3f);
            
            // Start-Button
            GameObject startBtnObj = new GameObject("StartButton");
            startBtnObj.transform.SetParent(mainMenuPanel.transform);
            startBtnObj.transform.localPosition = new Vector3(0, -0.1f, -0.1f);
            
            startButton = startBtnObj.AddComponent<VRSubmitButton>();
            startButton.buttonText = "▶ STARTEN";
            startButton.normalColor = new Color(0.2f, 0.7f, 0.3f);
            startButton.OnButtonPressed.AddListener(StartGame);
            
            // Beenden-Button (kleiner, unten)
            GameObject quitBtnObj = new GameObject("QuitButton");
            quitBtnObj.transform.SetParent(mainMenuPanel.transform);
            quitBtnObj.transform.localPosition = new Vector3(0, -0.4f, -0.1f);
            
            quitButton = quitBtnObj.AddComponent<VRSubmitButton>();
            quitButton.buttonText = "✕ BEENDEN";
            quitButton.buttonSize = 0.1f;
            quitButton.normalColor = new Color(0.6f, 0.2f, 0.2f);
            quitButton.OnButtonPressed.AddListener(QuitGame);
            
            Debug.Log("[MainMenu] Menü erstellt!");
        }
    }
}
