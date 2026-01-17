using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelMenuPanel;
    public GameObject levelView;

    void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Mehrere LevelManager gefunden! Zerstöre Duplikat.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ShowMainMenu();
    }

    // ====== Oeffentliche Funktionen fuer Buttons ======

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        levelMenuPanel.SetActive(false);
        levelView.SetActive(false);
    }

    public void ShowLevelMenu()
    {
        mainMenuPanel.SetActive(false);
        levelView.SetActive(false);
        levelMenuPanel.SetActive(true);
    }

    public void ShowLevelView()
    {
        mainMenuPanel.SetActive(false);
        levelMenuPanel.SetActive(false);
        levelView.SetActive(true);
    }

    public void ExitGame()
    {
        Debug.Log("Spiel wird beendet...");
        
#if UNITY_EDITOR
        // Im Editor: Stoppt den Play Mode
        EditorApplication.isPlaying = false;
#else
        // Im Build (VR/Standalone): Beendet die Anwendung
        Application.Quit();
#endif
    }
}
