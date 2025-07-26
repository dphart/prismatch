using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour, TimeProvider, LevelInfoProvider, ItemInfoProvider
{
   
    [SerializeField] private PowerUpSelector powerUpSelector;

    private int score;
    private int targetScore;
    private float timeRemaining;
    private bool timerRunning = false;
    private bool isPowerUpScreenActive = false;
    private bool isPaused = false;

    public ActivePowerUpsUI activePowerUpsUI;



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

    public int TimeElapsed => (int)(GameState.Instance.CurrentLevel.GetTotalTime - timeRemaining);

    public bool isBossLevel => false;

    public int currentRound => CurrentRound;

    public int maxRounds => 999;

    public List<Item> IconList => limitedItemPool;

    public List<Item> GetActiveItems => IconList;



    private void Awake()
    {
        powerManager = new PowerManager(this, this, this);
        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    private void Start()
    {
        StartLevel(true, true);
    }

    public List<Item> limitedItemPool;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI restartsRemainingText;

    private int maxRoundRestarts = 1;
    private int roundRestartsUsed = 0;

    private void UpdateGameOverUI()
    {
        restartsRemainingText.text = $"Restarts Round (Remaining: {maxRoundRestarts - roundRestartsUsed}";
    }

    public void OnRestartRound()
    {
        if (roundRestartsUsed >= maxRoundRestarts)
        {
            Debug.Log("❌ No more round restarts available.");
            return;
        }

        roundRestartsUsed++;
        Debug.Log($"🔄 Restarting round (used {roundRestartsUsed}/{maxRoundRestarts})");

        // Reset round but don't reset full game
        StartLevel(false, false);
        Board.Instance.ResetBoard();
        gameOverPanel.SetActive(false);
    }

    public void OnGameOverRestartGame()
    {
        Debug.Log("🆕 Restarting entire game.");
        roundRestartsUsed = 0;
        NewGameButton();
        gameOverPanel.SetActive(false);
    }


    public void TriggerGameOver()
    {
        timerRunning = false;
        isPowerUpScreenActive = true;
        UpdateGameOverUI();
        gameOverPanel.SetActive(true);
    }


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
            TriggerGameOver();
        }

        if (score >= targetScore && !isPowerUpScreenActive)
        {
            CompleteLevel();
        }
    }


    public void StartLevel(bool advanceRound, bool isNewGame)
    {
        if (isNewGame) ResetGame();
        Debug.Log($"Starting Level with previous round being {CurrentRound}");

        OnNextRound(CurrentRound, advanceRound);
        timerRunning = true;
        isPowerUpScreenActive = false;

        OnScoreChanged?.Invoke(score);
        OnTargetChanged?.Invoke(targetScore);     // ✅ Add this
        OnTimeChanged?.Invoke(timeRemaining);     // ✅ Add this


        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    private void OnNextRound(int completedRoundNumber, bool advanceRound)
    {
        
        if(advanceRound) CurrentRound = CurrentRound + 1;
        powerManager.ClearBossPowers();
        foreach (BossPowers bp in GameState.Instance.CurrentLevel.GetBossEffectsByRound(CurrentRound))
        {
            powerManager.RegisterBossPower(bp);
        }
        Debug.Log($"Current Round being set to {CurrentRound}");
        OnRoundChanged?.Invoke(CurrentRound);
        score = 0;
        targetScore = GameState.Instance.CurrentLevel.GetTargetScore(CurrentRound);
        timeRemaining = GameState.Instance.CurrentLevel.GetTotalTime;
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

    public void OnPowerUpSelected()
    {
        isPowerUpScreenActive = false;
        StartLevel(true, false);
    }

    public void NewGameButton()
    {
        Debug.Log("🆕 Starting new game...");

        // Reset core game logic
        StartLevel(true, true);

        // Reset board
        Board.Instance.GenerateBoard();

        // Optionally clear active powerups
        RemoveAllPowers();

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
        RemoveAllPowers();
        CurrentRound = 0;
        Debug.Log("♻️ Game state has been reset.");
    }

    public void RegisterSelectedPower(PowerUp powerUp)
    {
        powerManager.RegisterPower(powerUp);
        activePowerUpsUI?.AddCard(powerUp);   // 🔥 Update the UI directly
        Debug.Log($"🔌 Registered new power-up: {powerUp.Name}");
    }

    public void RemoveAllPowers()
    {
        powerManager.ClearPowers();
        activePowerUpsUI?.ClearAllCards();
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
