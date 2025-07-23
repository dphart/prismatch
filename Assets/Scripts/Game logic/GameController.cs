using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private float levelDurationSeconds = 60f;
    [SerializeField] private int baseTargetScore = 50;

    [SerializeField] private PowerUpSelector powerUpSelector;
    private bool isPowerUpScreenActive = false;

    private float timer;

    private void Start()
    {
        StartLevel(baseTargetScore, true);
    }

    private void Update()
    {
        
        if (GameState.Instance == null || !GameState.Instance.TimerIsRunning) return;

        timer += Time.deltaTime;

        // Check if the timer for this level has expired
        if (timer >= levelDurationSeconds)
        {
            timer = 0;
            IncreaseTarget();
        }

        // Check if player has beaten the level
        if (GameState.Instance.Score >= GameState.Instance.TargetScore && !isPowerUpScreenActive)
        {
            Debug.Log("Level Complete!");
            timer = 0;
            GameState.Instance.TimerIsRunning = false;
            isPowerUpScreenActive = true;
            powerUpSelector.ShowChoices(); // <- show the power-up picker        
        }
    }

    private void StartLevel(int targetScore, bool newGame)
    {
        if (newGame)
        {
            GameState.Instance.ResetGame();
            GameState.Instance.RegisterPowerUp(new BasePowerUp());

        }
        GameState.Instance.SetTargetScore(targetScore);
        GameState.Instance.StartTimer(levelDurationSeconds);
    }

    private void IncreaseTarget()
    {
        int newTarget = GameState.Instance.TargetScore + 200; // You can scale this however you'd like
        Debug.Log($"Time’s up! Increasing target to {newTarget}");
        StartLevel(newTarget, false);
    }

    private void DoubleTarget()
    {
        int doubledTarget = GameState.Instance.TargetScore * 3;
        Debug.Log($"Target met! New target is {doubledTarget}");
        StartLevel(doubledTarget, false);
    }

    public void OnPowerUpSelected()
    {
        isPowerUpScreenActive = false;
        DoubleTarget(); // or StartLevel(), depending on your flow
    }

}
