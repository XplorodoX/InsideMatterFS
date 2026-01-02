using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject levelMenuPanel;

    void Start()
    {
        ShowMainMenu();
    }

    // ====== Öffentliche Funktionen für Buttons ======

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
        Debug.Log("Spiel wird beendet");
        Application.Quit();
    }
}
