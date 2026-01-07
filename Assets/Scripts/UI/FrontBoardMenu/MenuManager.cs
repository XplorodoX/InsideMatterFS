using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelMenuPanel;

    void Start()
    {
        ShowMainMenu();
    }

    // ====== Oeffentliche Funktionen fuer Buttons ======

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        levelMenuPanel.SetActive(false);
    }

    public void ShowLevelMenu()
    {
        mainMenuPanel.SetActive(false);
        levelMenuPanel.SetActive(true);
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
