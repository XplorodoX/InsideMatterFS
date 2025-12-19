using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// UI für das Puzzle-Spiel.
    /// Zeigt Aufgaben, Fortschritt, Timer und Feedback.
    /// </summary>
    public class PuzzleUI : MonoBehaviour
    {
        [Header("Aufgaben-UI")]
        [Tooltip("Text für die aktuelle Aufgabe")]
        public TextMeshProUGUI taskTitle;
        
        [Tooltip("Text für die Aufgabenbeschreibung")]
        public TextMeshProUGUI taskDescription;
        
        [Tooltip("Text für benötigte Atome")]
        public TextMeshProUGUI requiredAtomsText;
        
        [Header("Status-UI")]
        [Tooltip("Score-Anzeige")]
        public TextMeshProUGUI scoreText;
        
        [Tooltip("Timer-Anzeige")]
        public TextMeshProUGUI timerText;
        
        [Tooltip("Fortschritts-Anzeige")]
        public TextMeshProUGUI progressText;
        
        [Header("Buttons")]
        [Tooltip("Button zum Überprüfen")]
        public Button checkButton;
        
        [Tooltip("Button zum Neustarten")]
        public Button restartButton;
        
        [Tooltip("Button für Hinweis")]
        public Button hintButton;
        
        [Header("Feedback")]
        [Tooltip("Panel für Erfolgs-Nachricht")]
        public GameObject successPanel;
        
        [Tooltip("Text für Erfolgs-Nachricht")]
        public TextMeshProUGUI successText;
        
        [Tooltip("Panel für Fehler-Nachricht")]
        public GameObject errorPanel;
        
        [Tooltip("Text für Fehler-Nachricht")]
        public TextMeshProUGUI errorText;
        
        [Header("Level Complete")]
        [Tooltip("Panel für Level-Abschluss")]
        public GameObject levelCompletePanel;
        
        [Tooltip("Final Score Text")]
        public TextMeshProUGUI finalScoreText;
        
        private PuzzleGameManager gameManager;
        
        void Start()
        {
            gameManager = PuzzleGameManager.Instance;
            
            if (gameManager == null)
            {
                UnityEngine.Debug.LogError("PuzzleGameManager nicht gefunden!");
                return;
            }
            
            // Events subscriben
            gameManager.OnTaskStarted.AddListener(OnTaskStarted);
            gameManager.OnTaskCompleted.AddListener(OnTaskCompleted);
            gameManager.OnTaskFailed.AddListener(OnTaskFailed);
            gameManager.OnLevelCompleted.AddListener(OnLevelCompleted);
            gameManager.OnScoreUpdate.AddListener(UpdateScore);
            gameManager.OnTimeUpdate.AddListener(UpdateTimer);
            
            // Buttons
            if (checkButton != null)
                checkButton.onClick.AddListener(OnCheckButtonClicked);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            
            if (hintButton != null)
                hintButton.onClick.AddListener(OnHintButtonClicked);
            
            // Panels ausblenden
            HideFeedbackPanels();
        }
        
        void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnTaskStarted.RemoveListener(OnTaskStarted);
                gameManager.OnTaskCompleted.RemoveListener(OnTaskCompleted);
                gameManager.OnTaskFailed.RemoveListener(OnTaskFailed);
                gameManager.OnLevelCompleted.RemoveListener(OnLevelCompleted);
                gameManager.OnScoreUpdate.RemoveListener(UpdateScore);
                gameManager.OnTimeUpdate.RemoveListener(UpdateTimer);
            }
        }
        
        /// <summary>
        /// Neue Aufgabe gestartet
        /// </summary>
        private void OnTaskStarted(MoleculeDefinition task)
        {
            if (taskTitle != null)
                taskTitle.text = $"Baue: {task.moleculeName}";
            
            if (taskDescription != null)
                taskDescription.text = task.description;
            
            if (requiredAtomsText != null)
                requiredAtomsText.text = $"Benötigt: {task.GetAtomListString()}";
            
            HideFeedbackPanels();
            UpdateProgress();
        }
        
        /// <summary>
        /// Aufgabe erfolgreich
        /// </summary>
        private void OnTaskCompleted(ValidationResult result)
        {
            if (successPanel != null)
            {
                successPanel.SetActive(true);
                
                if (successText != null)
                    successText.text = $"✅ Richtig!\n{result.moleculeName}\n+{result.score} Punkte";
            }
            
            UpdateProgress();
            
            // Automatisch ausblenden
            Invoke(nameof(HideFeedbackPanels), 2f);
        }
        
        /// <summary>
        /// Aufgabe fehlgeschlagen
        /// </summary>
        private void OnTaskFailed(ValidationResult result)
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
                
                if (errorText != null)
                    errorText.text = $"❌ Nicht ganz richtig!\n\n{result.GetErrorMessage()}";
            }
            
            // Automatisch ausblenden
            Invoke(nameof(HideFeedbackPanels), 4f);
        }
        
        /// <summary>
        /// Level abgeschlossen
        /// </summary>
        private void OnLevelCompleted()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
                
                if (finalScoreText != null)
                    finalScoreText.text = $"Level abgeschlossen!\n\nScore: {gameManager.CurrentScore}";
            }
        }
        
        /// <summary>
        /// Score aktualisieren
        /// </summary>
        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }
        
        /// <summary>
        /// Timer aktualisieren
        /// </summary>
        private void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                if (time > 0)
                {
                    int minutes = Mathf.FloorToInt(time / 60f);
                    int seconds = Mathf.FloorToInt(time % 60f);
                    timerText.text = $"Zeit: {minutes:00}:{seconds:00}";
                    
                    // Warnung bei wenig Zeit
                    if (time < 30f)
                        timerText.color = Color.red;
                    else
                        timerText.color = Color.white;
                }
                else
                {
                    timerText.text = "";
                }
            }
        }
        
        /// <summary>
        /// Fortschritt aktualisieren
        /// </summary>
        private void UpdateProgress()
        {
            if (progressText != null && gameManager != null && gameManager.currentLevel != null)
            {
                int current = Mathf.Min(gameManager.currentLevel.moleculesToBuild.Count, 
                                       System.Array.IndexOf(gameManager.currentLevel.moleculesToBuild.ToArray(), 
                                                           gameManager.CurrentTask) + 1);
                int total = gameManager.currentLevel.moleculesToBuild.Count;
                progressText.text = $"Aufgabe {current}/{total}";
            }
        }
        
        /// <summary>
        /// Check-Button geklickt
        /// </summary>
        private void OnCheckButtonClicked()
        {
            if (gameManager != null)
            {
                gameManager.CheckCurrentMolecule();
            }
        }
        
        /// <summary>
        /// Restart-Button geklickt
        /// </summary>
        private void OnRestartButtonClicked()
        {
            if (gameManager != null)
            {
                gameManager.RestartLevel();
            }
            
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);
        }
        
        /// <summary>
        /// Hint-Button geklickt
        /// </summary>
        private void OnHintButtonClicked()
        {
            if (gameManager != null)
            {
                string hint = gameManager.GetCurrentTaskHint();
                
                if (errorPanel != null && errorText != null)
                {
                    errorPanel.SetActive(true);
                    errorText.text = $"💡 Hinweis:\n\n{hint}";
                }
                
                Invoke(nameof(HideFeedbackPanels), 5f);
            }
        }
        
        /// <summary>
        /// Blendet Feedback-Panels aus
        /// </summary>
        private void HideFeedbackPanels()
        {
            if (successPanel != null)
                successPanel.SetActive(false);
            
            if (errorPanel != null)
                errorPanel.SetActive(false);
        }
    }
}
