using DG.Tweening;
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
    [SerializeField] private GameObject bossBackground; // drag the BossBackground object here


    private int score;
    private int targetScore;
    private float timeRemaining;
    private bool timerRunning = false;
    private bool isPowerUpScreenActive = false;
    private bool isPaused = false;
    [SerializeField] private BossBattleUi bossBattleUI;   // 👈 Drag your Boss UI prefab/panel here in the Inspector


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
        StartLevel(false);
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

        // ✅ Hide Game Over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // ✅ Reset round (but do NOT increment round number)
        score = 0;
        handleScoreChange(score);
        powerManager.RemoveMostRecentPower();        // 🔥 Forces user to pick a new power again

        // ✅ Reset the board visuals
        Board.Instance.ResetBoard();

        // ✅ Replay the start-of-round flow (power picker, boss intro, etc.)
        StartLevel(true);
    }


    public void OnGameOverRestartGame()
    {
        Debug.Log("🆕 Restarting entire game.");
        roundRestartsUsed = 0;
        Board.Instance.ResetBoard();
        NewGameButton();
        gameOverPanel.SetActive(false);
    }

    public void ToggleActivePowerUpScreen()
    {
        TogglePause();
        activePowerUpsUI.TogglePanel();
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


    public void StartLevel(bool isRestartRound)
    {
        Debug.Log($"▶️ Starting Level {CurrentRound + 1}");

        // ✅ Always hide Game Over when starting a new level
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // ✅ Increment round (or reset if needed)
        if(!isRestartRound) CurrentRound++;

        // ✅ Clear and register boss powers for this round
        powerManager.ClearBossPowers();
        var bossPowers = GameState.Instance.CurrentLevel.GetBossEffectsByRound(CurrentRound);
        foreach (BossPowers bp in bossPowers)
        {
            powerManager.RegisterBossPower(bp);
        }

        // ✅ Reset scoring/timer for the round
        score = 0;
        targetScore = GameState.Instance.CurrentLevel.GetTargetScore(CurrentRound);
        timeRemaining = GameState.Instance.CurrentLevel.GetTotalTime;
        timerRunning = false;   // ❗ Don't start ticking until the player picks their power

        // ✅ Update UI
        GameUIManager.UpdateRound(CurrentRound);
        handleScoreChange(score);
        HandleTargetChanged(targetScore);
        UpdateTimeRemaining(timeRemaining);

        // ✅ Make sure board is generated fresh
        foreach (Transform child in transform.parent) // Or a dedicated parent container for scores
        {
            if (child.name.Contains("FloatingScore"))
            {
                Destroy(child.gameObject);
            }
        }
        Board.Instance.GenerateBoard();

        // ✅ Reset boss background visibility
        bossBackground.SetActive(false);

        // ✅ If there are boss powers, show the boss intro and background
        if (bossPowers.Count > 0)
        {
            Debug.Log($"🔥 Boss Round detected: {bossPowers.Count} powers");
            bossBackground.SetActive(true);
            bossBattleUI.ShowBossIntro(bossPowers);
        }
        else
        {
            // ✅ Otherwise, go straight to power picker
            powerUpSelector.ShowChoices();
        }
    }


    public void ShowPowerPickerAfterBossIntro()
    {
        Debug.Log("✅ Boss intro finished → showing Power Picker");
        powerUpSelector.ShowChoices();
    }






    private void OnNextRound(int completedRoundNumber, bool advanceRound)
    {
        DOTween.KillAll();
        if (advanceRound) CurrentRound = CurrentRound + 1;
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
        StartLevel(false);
    }

    public void OnPowerUpSelected()
    {
        Debug.Log("✅ Power selected, starting timer.");
        isPowerUpScreenActive = false;
        timerRunning = true;    // ⬅ Timer starts now
    }


    public void NewGameButton()
    {
        Debug.Log("🆕 Starting new game...");
        RemoveAllPowers();         // ✅ Clear old powers
        CurrentRound = 0;          // ✅ Reset rounds
        StartLevel(false);              // ✅ Fresh start
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

    public void PrepareLevel(bool advanceRound, bool isNewGame)
    {
        if (isNewGame) ResetGame();
        Debug.Log($"🟢 Preparing Level {CurrentRound + (advanceRound ? 1 : 0)}");

        // Advance round number before starting
        OnNextRound(CurrentRound, advanceRound);

        // Always reset background at level start
        bossBackground.SetActive(false);

        // Check if this round has boss powers
        var bossPowers = GameState.Instance.CurrentLevel.GetBossEffectsByRound(CurrentRound);

        if (bossPowers.Count > 0)
        {
            Debug.Log($"🔥 Boss Round detected with {bossPowers.Count} powers");
            bossBackground.SetActive(true);
            bossBattleUI.ShowBossIntro(bossPowers);  // Shows intro, then calls ShowPowerPickerAfterBossIntro()
        }
        else
        {
            // No boss → go straight to power picker
            powerUpSelector.ShowChoices();
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
