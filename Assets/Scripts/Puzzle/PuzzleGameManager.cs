using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using InsideMatter.Molecule;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Haupt-Manager für das Puzzle-Spiel.
    /// Verwaltet Levels, Aufgaben, Validierung und Spielfortschritt.
    /// </summary>
    public class PuzzleGameManager : MonoBehaviour
    {
        public static PuzzleGameManager Instance { get; private set; }
        
        [Header("Aktuelles Level")]
        [Tooltip("Das zu spielende Level")]
        public PuzzleLevel currentLevel;
        
        [Tooltip("Aktuelle Aufgabe (Index in moleculesToBuild)")]
        private int currentTaskIndex = 0;
        
        [Header("Komponenten")]
        [Tooltip("Validator für Molekül-Prüfung")]
        public MoleculeValidator validator;
        
        [Tooltip("Spawner für Atome")]
        public AtomSpawner atomSpawner;
        
        [Header("Status")]
        [Tooltip("Aktueller Score")]
        private int currentScore = 0;
        
        [Tooltip("Verbleibende Zeit")]
        private float remainingTime = 0f;
        
        [Tooltip("Ist das Level aktiv?")]
        private bool isLevelActive = false;
        
        [Tooltip("Abgeschlossene Aufgaben")]
        private List<int> completedTasks = new List<int>();
        
        [Header("Events")]
        public UnityEvent<MoleculeDefinition> OnTaskStarted;
        public UnityEvent<ValidationResult> OnTaskCompleted;
        public UnityEvent<ValidationResult> OnTaskFailed;
        public UnityEvent OnLevelCompleted;
        public UnityEvent OnLevelFailed;
        public UnityEvent<float> OnTimeUpdate;
        public UnityEvent<int> OnScoreUpdate;
        
        /// <summary>
        /// Aktuelle Aufgabe
        /// </summary>
        public MoleculeDefinition CurrentTask
        {
            get
            {
                if (currentLevel == null || currentTaskIndex >= currentLevel.moleculesToBuild.Count)
                    return null;
                return currentLevel.moleculesToBuild[currentTaskIndex];
            }
        }
        
        public int CurrentScore => currentScore;
        public float RemainingTime => remainingTime;
        public bool IsLevelActive => isLevelActive;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Mehrere PuzzleGameManager! Zerstöre Duplikat.");
                Destroy(gameObject);
                return;
            }
            
            // Validator erstellen falls nicht vorhanden
            if (validator == null)
            {
                validator = gameObject.AddComponent<MoleculeValidator>();
            }
        }
        
        void Update()
        {
            if (!isLevelActive) return;
            
            // Timer
            if (currentLevel.totalTimeLimit > 0)
            {
                remainingTime -= Time.deltaTime;
                OnTimeUpdate?.Invoke(remainingTime);
                
                if (remainingTime <= 0)
                {
                    FailLevel("Zeit abgelaufen!");
                }
            }
        }
        
        /// <summary>
        /// Startet ein neues Level
        /// </summary>
        public void StartLevel(PuzzleLevel level)
        {
            if (level == null)
            {
                UnityEngine.Debug.LogError("Kein Level zugewiesen!");
                return;
            }
            
            currentLevel = level;
            currentTaskIndex = 0;
            currentScore = 0;
            completedTasks.Clear();
            isLevelActive = true;
            remainingTime = level.totalTimeLimit;
            
            UnityEngine.Debug.Log($"Level gestartet: {level.levelName}");
            
            // Spawn Atome
            if (atomSpawner != null)
            {
                atomSpawner.SpawnAtomsForLevel(level);
            }
            
            // Erste Aufgabe starten
            StartNextTask();
        }
        
        /// <summary>
        /// Startet die nächste Aufgabe
        /// </summary>
        private void StartNextTask()
        {
            if (CurrentTask == null)
            {
                CompleteLevel();
                return;
            }
            
            UnityEngine.Debug.Log($"Neue Aufgabe: {CurrentTask.moleculeName}");
            OnTaskStarted?.Invoke(CurrentTask);
        }
        
        /// <summary>
        /// Prüft ob das aktuelle Molekül korrekt ist
        /// </summary>
        public void CheckCurrentMolecule()
        {
            if (!isLevelActive || CurrentTask == null)
            {
                UnityEngine.Debug.LogWarning("Kein aktives Level oder Aufgabe!");
                return;
            }
            
            // Finde alle Atome in der Szene
            Atom[] allAtoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            List<Atom> atomList = new List<Atom>(allAtoms);
            
            // Finde zusammenhängende Moleküle
            List<List<Atom>> molecules = validator.FindMolecules(atomList);
            
            if (molecules.Count == 0)
            {
                UnityEngine.Debug.Log("Keine Moleküle gefunden!");
                return;
            }
            
            // Prüfe jedes Molekül
            ValidationResult bestResult = null;
            List<Atom> bestMolecule = null;
            
            foreach (var molecule in molecules)
            {
                ValidationResult result = validator.ValidateMolecule(molecule, CurrentTask);
                
                if (result.isValid)
                {
                    bestResult = result;
                    bestMolecule = molecule;
                    break;
                }
                
                // Behalte bestes Ergebnis für Feedback
                if (bestResult == null || result.errors.Count < bestResult.errors.Count)
                {
                    bestResult = result;
                    bestMolecule = molecule;
                }
            }
            
            // Auswertung
            if (bestResult != null && bestResult.isValid)
            {
                CompleteTask(bestResult, bestMolecule);
            }
            else
            {
                FailTask(bestResult);
            }
        }
        
        /// <summary>
        /// Aufgabe erfolgreich abgeschlossen
        /// </summary>
        private void CompleteTask(ValidationResult result, List<Atom> molecule)
        {
            UnityEngine.Debug.Log($"✅ Aufgabe erfolgreich: {result.moleculeName} (+{result.score} Punkte)");
            
            currentScore += result.score;
            completedTasks.Add(currentTaskIndex);
            
            OnScoreUpdate?.Invoke(currentScore);
            OnTaskCompleted?.Invoke(result);
            
            // Molekül markieren oder entfernen
            MarkMoleculeAsComplete(molecule);
            
            // Nächste Aufgabe
            currentTaskIndex++;
            
            if (currentLevel.sequentialOrder)
            {
                // Automatisch nächste Aufgabe starten
                StartCoroutine(StartNextTaskDelayed(2f));
            }
            else
            {
                // Parallel: Prüfe ob alle fertig
                if (completedTasks.Count >= currentLevel.moleculesToBuild.Count)
                {
                    CompleteLevel();
                }
            }
        }
        
        /// <summary>
        /// Aufgabe fehlgeschlagen
        /// </summary>
        private void FailTask(ValidationResult result)
        {
            UnityEngine.Debug.Log($"❌ Aufgabe fehlgeschlagen: {result.GetErrorMessage()}");
            OnTaskFailed?.Invoke(result);
        }
        
        /// <summary>
        /// Level erfolgreich abgeschlossen
        /// </summary>
        private void CompleteLevel()
        {
            isLevelActive = false;
            UnityEngine.Debug.Log($"🎉 Level abgeschlossen! Score: {currentScore}/{currentLevel.maxScore}");
            OnLevelCompleted?.Invoke();
        }
        
        /// <summary>
        /// Level fehlgeschlagen
        /// </summary>
        private void FailLevel(string reason)
        {
            isLevelActive = false;
            UnityEngine.Debug.Log($"💥 Level fehlgeschlagen: {reason}");
            OnLevelFailed?.Invoke();
        }
        
        /// <summary>
        /// Markiert ein Molekül als abgeschlossen
        /// </summary>
        private void MarkMoleculeAsComplete(List<Atom> molecule)
        {
            // Visuelles Feedback
            foreach (var atom in molecule)
            {
                // Z.B. grünes Leuchten
                atom.SetSelected(true);
            }
            
            // Optional: Nach kurzer Zeit entfernen/ausblenden
            // StartCoroutine(RemoveMoleculeDelayed(molecule, 3f));
        }
        
        /// <summary>
        /// Startet nächste Aufgabe verzögert
        /// </summary>
        private IEnumerator StartNextTaskDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextTask();
        }
        
        /// <summary>
        /// Level neu starten
        /// </summary>
        public void RestartLevel()
        {
            if (currentLevel != null)
            {
                // Alle Atome entfernen
                Atom[] atoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
                foreach (var atom in atoms)
                {
                    Destroy(atom.gameObject);
                }
                
                // Level neu starten
                StartLevel(currentLevel);
            }
        }
        
        /// <summary>
        /// Gibt Hinweis zur aktuellen Aufgabe
        /// </summary>
        public string GetCurrentTaskHint()
        {
            if (CurrentTask == null) return "";
            
            return $"Baue: {CurrentTask.moleculeName} ({CurrentTask.chemicalFormula})\n" +
                   $"Benötigt: {CurrentTask.GetAtomListString()}\n" +
                   CurrentTask.description;
        }
    }
}
