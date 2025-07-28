using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI roundsText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    private int currentLevelNumber; // ✅ Level numbers start at 1

    private void Start()
    {
        if (GameState.Instance?.CurrentLevel == null)
        {
            Debug.LogError("No current level set, defaulting to Level 1");
            currentLevelNumber = 1;
        }
        else
        {
            currentLevelNumber = GameState.Instance.CurrentLevel.GetLevelNumber;
        }

        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(() => NextLevel());
        prevButton.onClick.AddListener(() => PreviousLevel());


        ShowLevel(currentLevelNumber);
    }


    private void ShowLevel(int levelNumber)
    {
        var level = LevelLibrary.GetLevel(levelNumber);
        if (level == null) return;

        // ✅ Update UI text
        levelText.text = $"Level {level.GetLevelNumber}";
        descriptionText.text = $"Incomplete";
        roundsText.text = $"{level.GetLevelDescription()}";

        // ✅ Toggle buttons visibility
        prevButton.gameObject.SetActive(levelNumber > 1);
        nextButton.gameObject.SetActive(levelNumber < LevelLibrary.TotalLevels);
    }

    public void NextLevel()
    {
        if (currentLevelNumber < LevelLibrary.TotalLevels)
        {
            currentLevelNumber++;
            ShowLevel(currentLevelNumber);
        }
    }

    public void PreviousLevel()
    {
        if (currentLevelNumber > 1)
        {
            currentLevelNumber--;
            ShowLevel(currentLevelNumber);
        }
    }

    public void PlayLevel()
    {
        // ✅ Ensure GameState exists
        if (GameState.Instance == null)
        {
            Debug.LogError("❌ GameState is missing! Did you forget to add it to the scene?");
            return;
        }

        // ✅ Assign the selected level
        GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(currentLevelNumber);

        // ✅ Load the gameplay scene
        SceneManager.LoadScene("prismatch");
    }

}
