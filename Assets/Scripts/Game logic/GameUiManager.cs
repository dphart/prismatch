using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundText;

    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private RectTransform scoreAnchorTransform;
    [SerializeField] private RectTransform canvasTransform;

    private GameController gameController;
    private int previousScore = 0;

    private void Start()
    {
        gameController = Object.FindFirstObjectByType<GameController>();

        if (gameController == null)
        {
            Debug.LogError("GameController not found in scene.");
            return;
        }

        gameController.OnScoreChanged += UpdateScore;
        gameController.OnTargetChanged += UpdateTarget;
        gameController.OnTimeChanged += UpdateTimer;
        gameController.OnRoundChanged += UpdateRound;

        UpdateScore(gameController.CurrentScore);
        UpdateTarget(gameController.CurrentTarget);
        UpdateTimer(gameController.CurrentTime);
        UpdateRound(gameController.CurrentRound);
    }

    private void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";

        int delta = score - previousScore;
        if (delta > 0)
        {
            ShowFloatingScore(delta);
        }

        previousScore = score;
    }

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
        int level = GameState.Instance.CurrentLevel.GetLevelNumber;
        roundText.text = $"Level: {level}.{round}";
    }

    private void OnDestroy()
    {
        if (gameController == null) return;

        gameController.OnScoreChanged -= UpdateScore;
        gameController.OnTargetChanged -= UpdateTarget;
        gameController.OnTimeChanged -= UpdateTimer;
        gameController.OnRoundChanged -= UpdateRound;
    }

    public void ShowFloatingScore(int amount)
    {
        if (floatingTextPrefab == null || scoreAnchorTransform == null || canvasTransform == null)
        {
            Debug.LogWarning("FloatingText references missing!");
            return;
        }

        // Get anchored position from score label
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasTransform,
            scoreAnchorTransform.position,
            null,
            out anchoredPos
        );

        // Add spawn offset (right 50px, down 15px)
        Vector2 spawnOffset = new Vector2(75f, -25f);
        Vector2 spawnPosition = anchoredPos
            + Vector2.up * (scoreAnchorTransform.rect.height / 2f)
            + spawnOffset;

        var instance = Instantiate(floatingTextPrefab, canvasTransform);
        var rectTransform = instance.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = spawnPosition;

        // Determine direction (always up and out)
        Vector2 direction = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.2f)).normalized;

        // Determine color and size
        float percent = (float)amount / gameController.CurrentTarget;

        Color color = Color.white;
        float size = 36f;

        if (percent >= 0.25f)
        {
            color = new Color(0.6f, 0.4f, 1f); // purple
            size = 60f;
        }
        else if (percent >= 0.15f)
        {
            color = Color.green;
            size = 48f;
        }

        // Play the animation
        instance.Animate($"+{amount}", direction, color, size);
    }
}
