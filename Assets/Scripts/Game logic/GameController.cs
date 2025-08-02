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
    public GameUIManager GameUIManager;



    private PowerManager powerManager;
    private ScoreManager scoreManager;

    [HideInInspector]
    public int CurrentRound = 0;

    public int CurrentScore => score;
    public int CurrentTarget => targetScore;
    public float CurrentTime => timeRemaining;

    public PowerManager PowerManager => powerManager;

    public int TimeRemaining => (int)timeRemaining;

    public int TimeElapsed => (int)(GameState.Instance.CurrentLevel.GetTotalTime - timeRemaining);

    public bool isBossLevel => false;

    public int currentRound => CurrentRound;

    public int maxRounds => 999;

    public List<Item> IconList => limitedItemPool;

    public List<Item> GetActiveItems => IconList;



    private void Awake()
    {
        Debug.Log("✅ GameController.Awake() fired");

        powerManager = new PowerManager(this, this, this);
        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
    }

    private void OnEnable()
    {
        Debug.Log("✅ GameController.OnEnable() fired");
    }


    private void Start()
    {
        Debug.Log("Game Controller On Start");
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
        Board.Instance.ResetBoard();
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

        // ✅ Subtract from the real timer
        timeRemaining -= Time.deltaTime;

        // ✅ Clamp so it doesn’t go negative
        if (timeRemaining < 0f)
            timeRemaining = 0f;

        // ✅ Push updated timer value to UI
        UpdateTimeRemaining(TimeRemaining);

        if (timeRemaining <= 0f)
        {
            timerRunning = false;
            TriggerGameOver();
        }

        if (score >= targetScore && !isPowerUpScreenActive)
        {
            CompleteLevel();
        }
    }


    private void UpdateTimeRemaining(float timeRemaining)
    {
        GameUIManager.UpdateTimer(timeRemaining);
    }


    public void StartLevel(bool advanceRound, bool isNewGame)
    {
        if (isNewGame) ResetGame();
        Debug.Log($"Starting Level with previous round being {CurrentRound}");

        OnNextRound(CurrentRound, advanceRound);
        timerRunning = true;
        isPowerUpScreenActive = false;

        handleScoreChange(score);
        HandleTargetChanged(targetScore);     // ✅ Add this
        UpdateTimeRemaining(0);     // ✅ Add this


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
        GameUIManager.UpdateRound(CurrentRound);
        score = 0;
        targetScore = GameState.Instance.CurrentLevel.GetTargetScore(CurrentRound);
        timeRemaining = GameState.Instance.CurrentLevel.GetTotalTime;
    }


    public void HandleTileMatch(List<Tile> tiles)
    {
        int scoreToAdd = scoreManager.CalculateScore(tiles);
        score += scoreToAdd;
        handleScoreChange(score);
    }

    private void handleScoreChange(int newScore)
    {
        GameUIManager.UpdateScore(newScore);
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
        handleScoreChange(score);
        HandleTargetChanged(targetScore);
        UpdateTimeRemaining(CurrentTime);
        handleRoundChanged(currentRound);
    }
    private void HandleTargetChanged(int _newTarget)
    {
        GameUIManager.UpdateTarget(_newTarget);
    }

    private void handleRoundChanged(int _round)
    {
        GameUIManager.UpdateRound(_round);
    }


    public void ResetGame()
    {
        score = 0;
        handleScoreChange(score);
        RemoveAllPowers();
        CurrentRound = 0;
        Debug.Log("♻️ Game state has been reset.");
    }

    public void RegisterSelectedPower(PowerUp powerUp)
    {
        powerManager.RegisterPower(powerUp);
        Debug.Log($"🔌 Registered new power-up: {powerUp.Name}");
    }

    public void RemoveAllPowers()
    {
        powerManager.ClearPowers();
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
