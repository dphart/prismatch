using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour, TimeProvider, LevelInfoProvider
{
    [SerializeField] private float levelDurationSeconds = 60f;
    [SerializeField] private int baseTargetScore = 50;
    [SerializeField] private PowerUpSelector powerUpSelector;

    private int score;
    private int targetScore;
    private float timeRemaining;
    private bool timerRunning = false;
    private bool isPowerUpScreenActive = false;
    private bool isPaused = false;


    private PowerManager powerManager;
    private ScoreManager scoreManager;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnTargetChanged;
    public event Action<float> OnTimeChanged;
    public event Action<int> OnRoundChanged;
    [HideInInspector]
    public int CurrentRound = 0;

    public int CurrentScore => score;
    public int CurrentTarget => targetScore;
    public float CurrentTime => timeRemaining;

    public int TimeRemaining => (int)timeRemaining;

    public int TimeElapsed => (int)(levelDurationSeconds - timeRemaining);

    public bool isBossLevel => false;

    public int currentRound => CurrentRound;

    public int maxRounds => 999;

    public List<Item> IconList => limitedItemPool;


    private void Awake()
    {
        powerManager = new PowerManager(this);
        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    private void Start()
    {
        StartLevel(baseTargetScore, true);
    }

    public List<Item> limitedItemPool;

    public void GenerateItemPool(int maxItemTypes)
    {
        if (ItemDatabase.items == null || ItemDatabase.items.Length == 0)
        {
            Debug.LogError("[Board] ItemDatabase is null or empty!");
            limitedItemPool = new List<Item>();
            return;
        }

        limitedItemPool = ItemDatabase.items
            .OrderBy(i => UnityEngine.Random.value)
            .Take(Mathf.Min(maxItemTypes, ItemDatabase.items.Length))
            .ToList();

        Debug.Log($"[Board] Using {limitedItemPool.Count} item types. Requested {maxItemTypes}");
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

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
        Debug.Log($"Starting Level with previous round being {CurrentRound}");

        CurrentRound = CurrentRound +1;
        Debug.Log($"Current Round being set to {CurrentRound}");
        OnRoundChanged?.Invoke(CurrentRound);
        score = 0;
        targetScore = newTarget;
        timeRemaining = levelDurationSeconds;
        timerRunning = true;
        isPowerUpScreenActive = false;

        OnScoreChanged?.Invoke(score);
        OnTargetChanged?.Invoke(targetScore);     // ✅ Add this
        OnTimeChanged?.Invoke(timeRemaining);     // ✅ Add this


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

        // Reset core game logic
        StartLevel(baseTargetScore, true);

        // Reset board
        Board.Instance.GenerateBoard();

        // Optionally clear active powerups
        powerManager.ClearPowers();

        // Reset UI via event
        OnScoreChanged?.Invoke(score);
        OnTargetChanged?.Invoke(targetScore);
        OnTimeChanged?.Invoke(timeRemaining);
        OnRoundChanged?.Invoke(CurrentRound);
    }


    public void ResetGame()
    {
        score = 0;
        OnScoreChanged?.Invoke(score);
        powerManager.ClearPowers();
        CurrentRound = 0;
        Debug.Log("♻️ Game state has been reset.");
    }

    public void RegisterSelectedPower(PowerUp powerUp)
    {
        powerManager.RegisterPower(powerUp);
        Debug.Log($"🔌 Registered new power-up: {powerUp.Name}");
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? "⏸ Game Paused" : "▶️ Game Resumed");
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Debug.Log("⏸ Game Paused (Focus Lost)");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Debug.Log("▶️ Game Resumed (Focus Gained)");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && timerRunning && !isPowerUpScreenActive)
        {
            PauseGame();
        }
        else if (hasFocus && isPaused)
        {
            ResumeGame(); // Optional: auto-resume or keep paused
        }
    }

    public void AddOrRemoveItem(Item item, bool add)
    {
        if(item!= null)
        {
            if (add)
            {
                if (limitedItemPool.Contains(item)) return;
                limitedItemPool.Add(item);
            }
            else
            {
                if (limitedItemPool.Contains(item)) limitedItemPool.Remove(item);
            }
        }
    }
}
