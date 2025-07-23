using System;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private float levelDurationSeconds = 60f;
    [SerializeField] private int baseTargetScore = 50;
    [SerializeField] private PowerUpSelector powerUpSelector;

    private int score;
    private int targetScore;
    private float timeRemaining;
    private bool timerRunning = false;
    private bool isPowerUpScreenActive = false;

    private PowerManager powerManager;
    private ScoreManager scoreManager;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnTargetChanged;
    public event Action<float> OnTimeChanged;

    private void Awake()
    {
        powerManager = new PowerManager();
        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    private void Start()
    {
        StartLevel(baseTargetScore, true);
    }

    private void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        OnTimeChanged?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            IncreaseTarget();
        }

        if (score >= targetScore && !isPowerUpScreenActive)
        {
            CompleteLevel();
        }
    }

    public void StartLevel(int newTarget, bool isNewGame)
    {
        if (isNewGame) ResetGame();

        targetScore = newTarget;
        timeRemaining = levelDurationSeconds;
        timerRunning = true;
        isPowerUpScreenActive = false;

        score = 0;
        OnScoreChanged?.Invoke(score);
        OnTargetChanged?.Invoke(targetScore);
        OnTimeChanged?.Invoke(timeRemaining);

        powerManager.ClearPowers();
        powerManager.RegisterPower(new BasePowerUp());

        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    public void HandleTileMatch(List<Tile> tiles)
    {
        int scoreToAdd = scoreManager.CalculateScore(tiles);
        score += scoreToAdd;
        OnScoreChanged?.Invoke(score);
    }

    private void CompleteLevel()
    {
        timerRunning = false;
        isPowerUpScreenActive = true;
        powerUpSelector.ShowChoices();
    }

    private void IncreaseTarget()
    {
        int newTarget = targetScore + 200;
        Debug.Log($"🕒 Time's up! Increasing target to {newTarget}");
        StartLevel(newTarget, false);
    }

    public void OnPowerUpSelected()
    {
        isPowerUpScreenActive = false;
        StartLevel(targetScore * 2, false);
    }

    public void NewGameButton()
    {
        Debug.Log("🆕 Starting new game...");
        StartLevel(baseTargetScore, true);
    }

    public void ResetGame()
    {
        score = 0;
        OnScoreChanged?.Invoke(score);
        powerManager.ClearPowers();
        Debug.Log("♻️ Game state has been reset.");
    }

    public IReadOnlyList<PowerUp> GetActivePowers() => powerManager.ActivePowers;

    public void RegisterSelectedPower(PowerUp powerUp)
    {
        powerManager.RegisterPower(powerUp);
        Debug.Log($"🔌 Registered new power-up: {powerUp.Name}");
    }
}
