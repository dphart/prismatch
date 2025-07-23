using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        GameState.Instance.OnScoreChanged += UpdateScore;
        GameState.Instance.OnTargetChanged += UpdateTarget;
        GameState.Instance.OnTimeChanged += UpdateTimer;

        // Set initial values
        UpdateScore(GameState.Instance.Score);
        UpdateTarget(GameState.Instance.TargetScore);
        UpdateTimer(GameState.Instance.TimeRemaining);
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
        if (GameState.Instance == null) return;
        GameState.Instance.OnScoreChanged -= UpdateScore;
        GameState.Instance.OnTargetChanged -= UpdateTarget;
        GameState.Instance.OnTimeChanged -= UpdateTimer;
    }
}
