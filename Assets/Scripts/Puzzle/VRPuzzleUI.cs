using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// 3D VR UI for puzzle game - displays in world space for VR interaction
    /// </summary>
    public class VRPuzzleUI : MonoBehaviour
    {
        [Header("3D UI References")]
        [Tooltip("Main UI panel (should be world space canvas)")]
        public Transform uiPanel;
        
        [Tooltip("Distance from player/camera")]
        public float distanceFromCamera = 2.0f;
        
        [Tooltip("Follow camera rotation")]
        public bool followCameraRotation = true;
        
        [Tooltip("Smooth follow speed")]
        public float followSpeed = 5.0f;
        
        [Header("Task Display")]
        public TextMeshProUGUI taskTitleText;
        public TextMeshProUGUI taskDescriptionText;
        public TextMeshProUGUI progressText;
        
        [Header("Feedback Display")]
        public GameObject successPanel;
        public TextMeshProUGUI successText;
        public GameObject errorPanel;
        public TextMeshProUGUI errorText;
        
        [Header("Score & Timer")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;
        public Image timerBar;
        public Color timerNormalColor = Color.green;
        public Color timerWarningColor = Color.yellow;
        public Color timerCriticalColor = Color.red;
        
        [Header("VR Buttons")]
        public Button checkButton;
        public Button restartButton;
        public Button hintButton;
        
        [Header("Molecule Preview")]
        [Tooltip("Transform to spawn target molecule preview")]
        public Transform previewSpawnPoint;
        public GameObject currentPreview;
        public float previewRotationSpeed = 20f;
        
        private Camera mainCamera;
        private PuzzleGameManager gameManager;
        private float maxTimeForLevel;

        private void Start()
        {
            mainCamera = Camera.main;
            gameManager = FindFirstObjectByType<PuzzleGameManager>();
            
            if (gameManager != null)
            {
                // Subscribe to game events
                gameManager.OnTaskStarted.AddListener((mol) => OnTaskStarted(mol));
                gameManager.OnTaskCompleted.AddListener((result) => OnTaskCompleted(result));
                gameManager.OnTaskFailed.AddListener((result) => OnTaskFailed(result));
                gameManager.OnLevelCompleted.AddListener(() => OnLevelCompleted());
                gameManager.OnScoreUpdate.AddListener((score) => OnScoreUpdate(score));
                gameManager.OnTimeUpdate.AddListener((time) => OnTimeUpdate(time));
            }
            
            // Setup button listeners
            if (checkButton != null)
                checkButton.onClick.AddListener(OnCheckButtonPressed);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonPressed);
            if (hintButton != null)
                hintButton.onClick.AddListener(OnHintButtonPressed);
            
            // Hide feedback panels
            if (successPanel != null) successPanel.SetActive(false);
            if (errorPanel != null) errorPanel.SetActive(false);
            
            // Position UI in front of camera
            PositionUIInFrontOfCamera();
        }

        private void Update()
        {
            // Follow camera for comfortable VR viewing
            if (followCameraRotation && mainCamera != null && uiPanel != null)
            {
                Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
                uiPanel.position = Vector3.Lerp(uiPanel.position, targetPosition, Time.deltaTime * followSpeed);
                
                Quaternion targetRotation = Quaternion.LookRotation(uiPanel.position - mainCamera.transform.position);
                uiPanel.rotation = Quaternion.Slerp(uiPanel.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
            
            // Rotate preview molecule
            if (currentPreview != null)
            {
                currentPreview.transform.Rotate(Vector3.up, previewRotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void PositionUIInFrontOfCamera()
        {
            if (mainCamera != null && uiPanel != null)
            {
                uiPanel.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
                uiPanel.rotation = Quaternion.LookRotation(uiPanel.position - mainCamera.transform.position);
            }
        }

        private void OnTaskStarted(MoleculeDefinition molecule)
        {
            if (taskTitleText != null)
                taskTitleText.text = $"Build {molecule.moleculeName}";
            
            if (taskDescriptionText != null)
                taskDescriptionText.text = molecule.description;
            
            UpdateProgress();
            CreateMoleculePreview(molecule);
            
            // Hide feedback panels
            if (successPanel != null) successPanel.SetActive(false);
            if (errorPanel != null) errorPanel.SetActive(false);
        }

        private void OnTaskCompleted(ValidationResult result)
        {
            if (successPanel != null && successText != null)
            {
                successPanel.SetActive(true);
                successText.text = "Perfect! Molecule validated!";
                Invoke(nameof(HideSuccessPanel), 3f);
            }
            
            UpdateProgress();
        }

        private void OnTaskFailed(ValidationResult result)
        {
            if (errorPanel != null && errorText != null)
            {
                errorPanel.SetActive(true);
                string errors = string.Join(", ", result.errors);
                errorText.text = $"Not quite! {errors}";
                Invoke(nameof(HideErrorPanel), 4f);
            }
        }

        private void OnLevelCompleted()
        {
            if (taskTitleText != null)
                taskTitleText.text = "Level Complete!";
            
            if (taskDescriptionText != null)
                taskDescriptionText.text = $"Total Score: {gameManager?.CurrentScore ?? 0}";
            
            if (successPanel != null && successText != null)
            {
                successPanel.SetActive(true);
                successText.text = "Amazing! All molecules completed!";
            }
        }

        private void OnScoreUpdate(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        private void OnTimeUpdate(float timeRemaining)
        {
            if (gameManager?.currentLevel == null) return;
            float timeLimit = gameManager.currentLevel.totalTimeLimit;
            maxTimeForLevel = timeLimit;
            
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
                
                // Color coding based on time remaining
                if (timeRemaining < timeLimit * 0.25f)
                    timerText.color = timerCriticalColor;
                else if (timeRemaining < timeLimit * 0.5f)
                    timerText.color = timerWarningColor;
                else
                    timerText.color = timerNormalColor;
            }
            
            // Update timer bar
            if (timerBar != null)
            {
                float fillAmount = timeRemaining / timeLimit;
                timerBar.fillAmount = fillAmount;
                
                if (fillAmount < 0.25f)
                    timerBar.color = timerCriticalColor;
                else if (fillAmount < 0.5f)
                    timerBar.color = timerWarningColor;
                else
                    timerBar.color = timerNormalColor;
            }
        }

        private void UpdateProgress()
        {
            if (progressText != null && gameManager != null && gameManager.currentLevel != null)
            {
                int total = gameManager.currentLevel.moleculesToBuild.Count;
                int completed = 0;
                progressText.text = $"Progress: {completed}/{total}";
            }
        }

        private void CreateMoleculePreview(MoleculeDefinition molecule)
        {
            // Destroy old preview
            if (currentPreview != null)
            {
                Destroy(currentPreview);
            }
            
            if (previewSpawnPoint == null || molecule == null)
                return;
            
            // Create small preview version of target molecule
            // This is a simplified representation - you might want to create a proper molecular structure
            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            preview.transform.position = previewSpawnPoint.position;
            preview.transform.localScale = Vector3.one * 0.3f;
            preview.transform.SetParent(previewSpawnPoint, false);
            
            // Add text label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(preview.transform, false);
            labelObj.transform.localPosition = Vector3.up * 0.5f;
            
            TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
            label.text = molecule.moleculeName;
            label.fontSize = 3;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            
            currentPreview = preview;
        }

        private void OnCheckButtonPressed()
        {
            gameManager?.CheckCurrentMolecule();
        }

        private void OnRestartButtonPressed()
        {
            if (gameManager != null)
            {
                UnityEngine.Debug.Log("Restart requested");
            }
        }

        private void OnHintButtonPressed()
        {
            if (gameManager != null && gameManager.CurrentTask != null)
            {
                string hint = $"You need: ";
                foreach (var req in gameManager.CurrentTask.requiredAtoms)
                {
                    hint += $"{req.count}x {req.element}, ";
                }
                
                if (errorPanel != null && errorText != null)
                {
                    errorPanel.SetActive(true);
                    errorText.text = hint;
                    Invoke(nameof(HideErrorPanel), 5f);
                }
            }
        }

        private void HideSuccessPanel()
        {
            if (successPanel != null)
                successPanel.SetActive(false);
        }

        private void HideErrorPanel()
        {
            if (errorPanel != null)
                errorPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnTaskStarted.RemoveListener((mol) => OnTaskStarted(mol));
                gameManager.OnTaskCompleted.RemoveListener((result) => OnTaskCompleted(result));
                gameManager.OnTaskFailed.RemoveListener((result) => OnTaskFailed(result));
                gameManager.OnLevelCompleted.RemoveListener(() => OnLevelCompleted());
                gameManager.OnScoreUpdate.RemoveListener((score) => OnScoreUpdate(score));
                gameManager.OnTimeUpdate.RemoveListener((time) => OnTimeUpdate(time));
            }
        }
    }
}
