using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundText;


    private GameController gameController;

    private void Start()
    {
        gameController = Object.FindFirstObjectByType<GameController>(); // This must be refreshed if the scene was reloaded!

        if (gameController == null)
        {
            Debug.LogError("GameController not found in scene.");
            return;
        }

        // Subscribe to events
        gameController.OnScoreChanged += UpdateScore;
        gameController.OnTargetChanged += UpdateTarget;
        gameController.OnTimeChanged += UpdateTimer;
        gameController.OnRoundChanged += UpdateRound;

        // Set initial values
        UpdateScore(gameController.CurrentScore);
        UpdateTarget(gameController.CurrentTarget);
        UpdateTimer(gameController.CurrentTime);
        UpdateRound(gameController.CurrentRound);
    }

    private void UpdateScore(int score) => scoreText.text = $"Score: {score}";
    private void UpdateTarget(int target) => targetText.text = $"Target: {target}";
    private void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"Timer: {minutes:00}:{seconds:00}";
    }

    private void UpdateRound(int round)
    {
        Debug.Log($"Round being set to {round}");
        var level = GameState.Instance.CurrentLevel.GetLevelNumber;
        var currentRound = gameController.CurrentRound;
        roundText.text = $"Level: {level}.{currentRound}";
    }

    private void OnDestroy()
    {
        if (gameController == null) return;

        gameController.OnScoreChanged -= UpdateScore;
        gameController.OnTargetChanged -= UpdateTarget;
        gameController.OnTimeChanged -= UpdateTimer;
    }
}
