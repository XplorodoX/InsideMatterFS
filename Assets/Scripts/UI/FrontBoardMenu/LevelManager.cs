using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("UI Referenzen")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public GameObject showHintButton;
    public Image hintImage;

    [Header("Level Daten")]
    public LevelData[] levels;

    private int currentLevel;

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

    public void LoadLevel(int level)
    {
        currentLevel = level;

        int index = level - 1;

        // Sicherheit
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogError("Level existiert nicht: " + level);
            return;
        }

        // Titel setzen
        titleText.text = "Level " + level;

        // Beschreibung setzen
        descriptionText.text = levels[index].description;

        showHintButton.SetActive(true);

        // Hint vorbereiten
        hintImage.sprite = levels[index].hintSprite;
        hintImage.gameObject.SetActive(false);
    }

    public void ShowHint()
    {
        hintImage.gameObject.SetActive(true);
        showHintButton.SetActive(false);
    }
}
