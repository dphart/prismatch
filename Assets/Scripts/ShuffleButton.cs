using UnityEngine;
using UnityEngine.UI;

public class ShuffleButton : MonoBehaviour
{
    private const float TIME_PENALTY_SECONDS = 5f;
    
    private Button button;
    private GameController gameController;
    

    private void Awake()
    {
        gameController = Object.FindAnyObjectByType<GameController>();
        SetupButton();
    }

    private void SetupButton()
    {
        button = GetComponent<Button>();
        

        Image buttonImage = GetComponent<Image>();
        

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnShuffleButtonClicked);

    }

    public void OnShuffleButtonClicked()
    {
        if (gameController == null)
        {
            Debug.LogError("GameController not found! Cannot shuffle board.");
            return;
        }

        if (Board.Instance == null)
        {
            Debug.LogError("Board instance not found! Cannot shuffle board.");
            return;
        }

        Debug.Log("🔀 Shuffle button clicked - resetting board and applying time penalty");
        gameController.OnShuffleBoardClicked();

    }

}