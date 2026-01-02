using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelMenuController : MonoBehaviour
{
    [Header("Setup")]
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Button nextButton;
    public Button prevButton;
    public TextMeshProUGUI pageText;

    [Header("Levels")]
    public int totalLevels = 32;
    public int levelsPerPage = 8;

    private int currentPage = 0;

    void OnEnable()
    {
        ShowPage();
    }

    void Awake()
    {
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
    }

    void ShowPage()
    {
        foreach (Transform child in levelButtonContainer)
            Destroy(child.gameObject);

        int start = currentPage * levelsPerPage;
        int end = Mathf.Min(start + levelsPerPage, totalLevels);

        for (int i = start; i < end; i++)
        {
            GameObject btn = Instantiate(levelButtonPrefab, levelButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = $"Level {i + 1}";

            int levelIndex = i;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                LoadLevel(levelIndex);
            });
        }

        int maxPage = Mathf.CeilToInt((float)totalLevels / levelsPerPage) - 1;
        pageText.text = $"{currentPage + 1} / {maxPage + 1}";

        prevButton.interactable = currentPage > 0;
        nextButton.interactable = currentPage < maxPage;
    }

    void NextPage()
    {
        currentPage++;
        ShowPage();
    }

    void PrevPage()
    {
        currentPage--;
        ShowPage();
    }

    void LoadLevel(int index)
    {
        Debug.Log("Lade Level " + (index + 1));
        // SceneManager.LoadScene(...)
    }
}
