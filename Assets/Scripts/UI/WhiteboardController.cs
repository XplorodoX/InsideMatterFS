using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InsideMatter.Puzzle;

namespace InsideMatter.UI
{
    /// <summary>
    /// Manages the unified Whiteboard UI: Level Selection and Task Display.
    /// Projects a World Space Canvas onto the "Frontboard".
    /// </summary>
    public class WhiteboardController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform container;
        
        [Header("Pages")]
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject levelSelectPage;
        [SerializeField] private GameObject taskPage;

        [Header("Menu Elements")]
        [SerializeField] private Transform levelListContainer;
        [SerializeField] private GameObject levelButtonPrefab;

        [Header("Task Elements")]
        [SerializeField] private TextMeshProUGUI taskTitleText;
        [SerializeField] private TextMeshProUGUI taskDescriptionText;
        [SerializeField] private TextMeshProUGUI moleculeFormulaText;
        [SerializeField] private TextMeshProUGUI atomListText;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button backToHomeButton; // Back from Level Select to Home

        private PuzzleGameManager gameManager;

        public void Initialize(PuzzleGameManager gm)
        {
            gameManager = gm;
            ShowHome();
        }

        private void Start()
        {
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(ShowLevelSelection);
                
            if (backToHomeButton != null)
                backToHomeButton.onClick.AddListener(ShowHome);
                
            // Ensure correct start state if not initialized via code
            if (gameManager == null) 
                gameManager = FindFirstObjectByType<PuzzleGameManager>();
            
            ShowHome();
        }

        /// <summary>
        /// Shows the Start Screen (Home)
        /// </summary>
        public void ShowHome()
        {
            if (homePage != null) homePage.SetActive(true);
            if (levelSelectPage != null) levelSelectPage.SetActive(false);
            if (taskPage != null) taskPage.SetActive(false);
        }

        /// <summary>
        /// Shows the Level Selection Menu
        /// </summary>
        public void ShowLevelSelection()
        {
            if (homePage != null) homePage.SetActive(false);
            if (levelSelectPage != null) levelSelectPage.SetActive(true);
            if (taskPage != null) taskPage.SetActive(false);

            GenerateLevelButtons();
        }
        
        public void StartFreePlay()
        {
            Debug.Log("Free Play started!");
            // Implementation specific: Start a sandbox level or mode
            // For now, maybe just hide UI or show a "Sandbox" task?
        }

        /// <summary>
        /// Shows the current Task (Molecule to build)
        /// </summary>
        public void ShowTask(MoleculeDefinition molecule, int levelNumber)
        {
            if (homePage != null) homePage.SetActive(false);
            if (levelSelectPage != null) levelSelectPage.SetActive(false);
            if (taskPage != null) taskPage.SetActive(true);

            if (molecule != null)
            {
                taskTitleText.text = $"LEVEL {levelNumber}: {molecule.moleculeName.ToUpper()}";
                moleculeFormulaText.text = molecule.chemicalFormula;
                taskDescriptionText.text = molecule.description;

                string atoms = "BENÖTIGTE ATOME:\n";
                foreach (var req in molecule.requiredAtoms)
                {
                    atoms += $"• {req.count}x {req.element}\n";
                }
                atomListText.text = atoms;
            }
        }

        public void ShowSuccess(string message)
        {
             // Optional: visual feedback on the board
             taskDescriptionText.text = $"<color=green>BENÖTIGT: {message}</color>";
        }

        private void GenerateLevelButtons()
        {
            // Clear old buttons
            foreach (Transform child in levelListContainer)
            {
                Destroy(child.gameObject);
            }

            if (gameManager == null) return;

            foreach (var level in gameManager.allLevels)
            {
                if (level == null) continue;

                GameObject btnObj = Instantiate(levelButtonPrefab, levelListContainer);
                var btn = btnObj.GetComponent<Button>();
                var txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (txt != null) txt.text = level.levelName;

                btn.onClick.AddListener(() => {
                    gameManager.StartLevel(level);
                });
            }
        }
    }
}
