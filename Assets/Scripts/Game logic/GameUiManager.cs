using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField] private GameController gameController; // Assign via inspector

    private void Start()
    {
        if (gameController == null)
        {
            Debug.LogError("[GameUIManager] GameController reference is missing.");
            return;
        }

        gameController.OnScoreChanged += UpdateScore;
        gameController.OnTargetChanged += UpdateTarget;
        gameController.OnTimeChanged += UpdateTimer;

        // Optional: force UI sync in case events haven't fired yet
        UpdateScore(0); // You can expose gameController.Score if needed
        UpdateTarget(0);
        UpdateTimer(0f);
    }

    private void UpdateScore(int score) => scoreText.text = $"Score: {score}";
    private void UpdateTarget(int target) => targetText.text = $"Target: {target}";
    private void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"Timer: {minutes:00}:{seconds:00}";
    }

    private void OnDestroy()
    {
        if (gameController == null) return;
        gameController.OnScoreChanged -= UpdateScore;
        gameController.OnTargetChanged -= UpdateTarget;
        gameController.OnTimeChanged -= UpdateTimer;
    }
}
