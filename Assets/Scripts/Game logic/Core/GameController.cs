using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GamePhase { PowerSelection, BossIntro, Playing, Resolving, LevelComplete, Paused, GameOver, Victory }

public class GameController : MonoBehaviour, TimeProvider, LevelInfoProvider, ItemInfoProvider
{
    [SerializeField] private PowerUpSelector powerUpSelector;
    [SerializeField] private GameObject bossBackground;
    [SerializeField] private BossBattleUi bossBattleUI;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI restartsRemainingText;
    public ActivePowerUpsUI activePowerUpsUI;
    public BossPowerDisplayUI bossPowerDisplayUI;
    public GameUIManager GameUIManager;
    public List<Item> limitedItemPool = new();

    private int score, targetScore;
    private TimerController _timer;
    private RoundManager _roundManager;
    private PauseController _pauseController;
    private PowerManager powerManager;
    private ScoreManager scoreManager;
    private GameAudio audioFeedback;
    private readonly RunSummary run = new();
    private int roundRestartsUsed;
    private const int maxRoundRestarts = 1;
    private GamePhase resumePhase = GamePhase.Playing;
    private bool manualPause, focusPause, powersOpen, bossesOpen, settingsOpen;
    private bool restartConfirmationArmed;
    private Coroutine restartConfirmationRoutine;
    private int lastWarningSecond = -1;
    private int transitionVersion;

    public GamePhase Phase { get; private set; } = GamePhase.PowerSelection;
    public bool CanInteract => Phase == GamePhase.Playing;
    public bool TargetReached => targetScore > 0 && score >= targetScore;
    public int CurrentRound => _roundManager?.CurrentRound ?? 0;
    public int CurrentScore => score;
    public int CurrentTarget => targetScore;
    public float CurrentTime => _timer?.Remaining ?? 0;
    public PowerManager PowerManager => powerManager;
    public int TimeRemaining => Mathf.CeilToInt(CurrentTime);
    public int TimeElapsed => Mathf.FloorToInt((_timer?.Duration ?? 0) - CurrentTime);
    public bool isBossLevel => powerManager?.ActiveBossPowers().Count > 0;
    public int currentRound => CurrentRound;
    public int maxRounds => GameState.Instance?.CurrentLevel?.GetTotalRounds ?? 0;
    public List<Item> IconList => limitedItemPool;
    public List<Item> GetActiveItems => limitedItemPool;
    public List<MatchRule> GetEffectiveMatchRules()
    {
        var rules = GameState.Instance?.CurrentLevel?.GetMatchRules(CurrentRound)
            ?.Select(rule => rule.Clone()).ToList() ?? new List<MatchRule> { new(MatchType.Snake, 3) };
        foreach (var power in powerManager?.ActivePowers() ?? Enumerable.Empty<PowerUp>())
            power.ModifyMatchRules(rules);
        return rules;
    }
    private GameUIFacade Ui => new(GameUIManager, gameOverPanel, restartsRemainingText);

    private void Awake()
    {
        if (_timer != null) return;
        powerManager = new PowerManager(this, this, this);
        scoreManager = new ScoreManager(GameState.Instance.PlayerConfigs, powerManager);
        _timer = new TimerController();
        _timer.OnTick += UpdateTimeRemaining;
        _timer.OnExpired += TriggerGameOver;
        _roundManager = new RoundManager(() => GameState.Instance.CurrentLevel);
        _pauseController = new PauseController(_timer);
        audioFeedback = gameObject.AddComponent<GameAudio>();
    }

    private void Start()
    {
        GameUIManager?.InitializeRunUI(this);
        NewGameButton();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
        _timer?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_timer != null) { _timer.OnTick -= UpdateTimeRemaining; _timer.OnExpired -= TriggerGameOver; }
        Time.timeScale = 1;
    }

    public void BeginResolution()
    {
        if (CanInteract) Phase = GamePhase.Resolving;
    }

    public void EndResolution()
    {
        if (Phase == GamePhase.Paused && resumePhase == GamePhase.Resolving) resumePhase = GamePhase.Playing;
        else if (Phase == GamePhase.Resolving) Phase = GamePhase.Playing;
        else return;
        if (TargetReached && Phase != GamePhase.Paused) _ = CompleteRound();
    }

    public void HandleTileMatch(List<Tile> tiles)
    {
        if (Phase != GamePhase.Resolving) return;
        int added = scoreManager.CalculateScore(tiles);
        score = ScoreManager.SaturatingAdd(score, added);
        run.RecordMatch(added, Board.Instance?.wasUserInitiatedMatch() ?? false);
        Ui.UpdateScore(score);
        GameUIManager?.ShowScoreBreakdown(scoreManager.LastResult, tiles);
        PlaySound(added == 0 ? GameSound.Blocked : GameSound.Clear);
        if (scoreManager.LastResult.NextMultiplier > 1) PlaySound(GameSound.Power);
        // Stop the clock at the target, but wait for board animations to finish.
        if (TargetReached) _timer.Stop();
    }

    private async System.Threading.Tasks.Task CompleteRound()
    {
        if (Phase == GamePhase.LevelComplete || Phase == GamePhase.Victory) return;
        int version = ++transitionVersion;
        _timer.Stop();
        run.FinishRound(CurrentRound, score, targetScore, _timer.Duration - CurrentTime, true);
        Phase = GamePhase.LevelComplete;
        GameUIManager?.ShowMessage("LEVEL COMPLETE!", 1f);
        if (Board.Instance != null) await Board.Instance.PlayLevelCompleteExit();
        await System.Threading.Tasks.Task.Delay(550);
        if (this == null || version != transitionVersion) return;

        if (CurrentRound >= maxRounds)
        {
            Phase = GamePhase.Victory;
            Board.Instance?.CancelResolution();
            ProgressStore.MarkCompleted(GameState.Instance.CurrentLevel.GetLevelNumber, run.TotalScore);
            run.Save(true, GameState.Instance.CurrentLevel.GetLevelNumber);
            PlaySound(GameSound.Victory);
            GameUIManager?.ShowRunEnd(true, run, this);
        }
        else BeginRound(true);
    }

    public void TriggerGameOver()
    {
        if (Phase == GamePhase.GameOver || Phase == GamePhase.Victory) return;
        if (TargetReached) return;
        _timer.Stop();
        Board.Instance?.CancelResolution();
        ClearPauseReasons();
        Phase = GamePhase.GameOver;
        run.FinishRound(CurrentRound, score, targetScore, _timer.Duration - CurrentTime, false);
        run.Save(false, GameState.Instance.CurrentLevel.GetLevelNumber);
        Ui.ShowGameOver(maxRoundRestarts - roundRestartsUsed);
        GameUIManager?.ShowRunEnd(false, run, this);
        PlaySound(GameSound.Blocked);
    }

    public void OnRestartRound()
    {
        if (Phase != GamePhase.GameOver || roundRestartsUsed >= maxRoundRestarts) return;
        roundRestartsUsed++;
        run.UndoLastFailedRound();
        powerManager.RemoveMostRecentPower();
        StartLevel(true);
    }

    public bool CanRetry => Phase == GamePhase.GameOver && roundRestartsUsed < maxRoundRestarts;
    public void OnGameOverRestartGame() => NewGameButton();

    public void NewGameButton()
    {
        ResetGame();
        StartLevel(false);
    }

    public void ResetGame()
    {
        transitionVersion++;
        Board.Instance?.CancelResolution();
        ClearPauseReasons();
        _timer.Stop();
        score = 0;
        roundRestartsUsed = 0;
        scoreManager.Reset();
        powerManager.ClearPowers();
        powerManager.ClearBossPowers();
        powerUpSelector?.ResetOfferedList();
        _roundManager.ResetRounds();
        run.Reset();
        limitedItemPool.Clear();
        Phase = GamePhase.PowerSelection;
    }

    // Compatibility entry points for existing Inspector bindings share one implementation.
    public void StartLevel(bool isRestartRound) => BeginRound(!isRestartRound);
    public void PrepareLevel(bool advanceRound, bool isNewGame)
    {
        if (isNewGame) ResetGame();
        BeginRound(advanceRound);
    }

    private void BeginRound(bool advanceRound)
    {
        ClearPauseReasons();
        Phase = GamePhase.PowerSelection;
        Ui.HideGameOver();
        GameUIManager?.HideRunEnd();
        GameUIManager?.ShowBoardStats(false);
        powerUpSelector?.Hide();
        bossBattleUI?.Hide();
        var context = _roundManager.BeginRound(advanceRound);
        if (!context.BossPowersPersist) powerManager.ClearBossPowers();
        foreach (var boss in context.BossPowers)
        {
            if (!powerManager.ActiveBossPowers().Any(p => p.Name == boss.Name))
                powerManager.RegisterBossPower((BossPowers)boss.Clone());
        }
        score = 0;
        targetScore = context.TargetScore;
        scoreManager.Reset();
        _timer.Reset(context.RoundTime);
        lastWarningSecond = -1;
        Ui.UpdateTarget(targetScore);
        Ui.UpdateScore(score);
        Ui.UpdateRound(CurrentRound);
        GameUIManager?.UpdateMatchRules(GetEffectiveMatchRules());
        run.BeginRound();
        Board.Instance.GenerateBoard();
        if (bossBackground != null) bossBackground.SetActive(isBossLevel);
        ShowMessage("Swap any two animals. Connect 3+ of the same type; bent groups count. Shuffle costs 5 seconds.");
        if (context.BossPowers.Count > 0 && bossBattleUI != null)
        {
            Phase = GamePhase.BossIntro;
            bossBattleUI.ShowBossIntro(powerManager.ActiveBossPowers());
        }
        else powerUpSelector.ShowChoices();
    }

    public void ShowPowerPickerAfterBossIntro()
    {
        if (Phase != GamePhase.BossIntro) return;
        Phase = GamePhase.PowerSelection;
        powerUpSelector.ShowChoices();
    }

    public void RegisterSelectedPower(PowerUp power)
    {
        if (Phase == GamePhase.PowerSelection && power != null)
        {
            powerManager.RegisterPower(power);
            Board.Instance?.SetMatchRules(GetEffectiveMatchRules());
            GameUIManager?.UpdateMatchRules(GetEffectiveMatchRules());
            GameUIManager?.RefreshBottomBar(this);
        }
    }

    public void OnPowerUpSelected()
    {
        if (Phase != GamePhase.PowerSelection) return;
        Phase = GamePhase.Playing;
        Board.Instance?.SetVisible(true);
        GameUIManager?.ShowBoardStats(true);
        _timer.Start();
        PlaySound(GameSound.Power);
    }

    public void RemoveAllPowers() => powerManager.ClearPowers();

    private void UpdateTimeRemaining(float remaining)
    {
        Ui.UpdateTimer(remaining);
        int second = Mathf.CeilToInt(remaining);
        if (_timer.IsRunning && !_timer.IsPaused && second > 0 && second <= 10 && second != lastWarningSecond)
        {
            lastWarningSecond = second;
            PlaySound(GameSound.Warning);
        }
    }

    public void TogglePause()
    {
        if (!IsGameplayPhase()) return;
        manualPause = !manualPause;
        ApplyPauseReasons();
    }
    public void PauseGame()
    {
        if (!IsGameplayPhase()) return;
        manualPause = true; ApplyPauseReasons();
    }
    public void ResumeGame()
    {
        manualPause = false; ApplyPauseReasons();
    }
    private bool IsGameplayPhase() => Phase == GamePhase.Playing || Phase == GamePhase.Resolving || Phase == GamePhase.Paused;

    public void ToggleActivePowerUpScreen()
    {
        if (!IsGameplayPhase() || activePowerUpsUI == null) return;
        activePowerUpsUI.TogglePanel();
        powersOpen = activePowerUpsUI.IsOpen;
        ApplyPauseReasons();
    }
    public void ToggleBossPowerScreen()
    {
        if (!IsGameplayPhase() || bossPowerDisplayUI == null) return;
        bossPowerDisplayUI.TogglePanel();
        bossesOpen = bossPowerDisplayUI.IsOpen;
        ApplyPauseReasons();
    }

    public void ToggleSettings()
    {
        settingsOpen = !settingsOpen;
        GameUIManager?.ShowSettings(settingsOpen, this);
        if (IsGameplayPhase()) ApplyPauseReasons();
    }

    public void ToggleSound()
    {
        if (audioFeedback == null) return;
        audioFeedback.SetEnabled(!audioFeedback.Enabled);
        GameUIManager?.UpdateSoundSetting(audioFeedback.Enabled);
        if (audioFeedback.Enabled) PlaySound(GameSound.Power);
    }

    public void RequestRestartRun()
    {
        if (!restartConfirmationArmed)
        {
            restartConfirmationArmed = true;
            GameUIManager?.SetRestartConfirmation(true);
            ShowMessage("Tap Restart again to start this run over.");
            if (restartConfirmationRoutine != null) StopCoroutine(restartConfirmationRoutine);
            restartConfirmationRoutine = StartCoroutine(CancelRestartConfirmation());
            return;
        }
        restartConfirmationArmed = false;
        if (restartConfirmationRoutine != null) StopCoroutine(restartConfirmationRoutine);
        GameUIManager?.SetRestartConfirmation(false);
        NewGameButton();
    }

    private System.Collections.IEnumerator CancelRestartConfirmation()
    {
        yield return new WaitForSecondsRealtime(3f);
        restartConfirmationArmed = false;
        GameUIManager?.SetRestartConfirmation(false);
    }

    private void ApplyPauseReasons()
    {
        if (!IsGameplayPhase()) return;
        bool paused = manualPause || focusPause || powersOpen || bossesOpen || settingsOpen;
        if (paused && Phase != GamePhase.Paused)
        {
            resumePhase = Phase; Phase = GamePhase.Paused; _pauseController.Pause();
        }
        else if (!paused && Phase == GamePhase.Paused)
        {
            Phase = resumePhase; _pauseController.Resume();
            if (TargetReached && Phase == GamePhase.Playing) _ = CompleteRound();
        }
        GameUIManager?.ShowPaused(Phase == GamePhase.Paused && (manualPause || focusPause));
    }

    private void ClearPauseReasons()
    {
        manualPause = focusPause = powersOpen = bossesOpen = settingsOpen = false;
        _pauseController.Resume();
        activePowerUpsUI?.HidePanel();
        bossPowerDisplayUI?.HidePanel();
        GameUIManager?.ShowSettings(false, this);
        GameUIManager?.ShowPaused(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (_timer == null || !IsGameplayPhase()) return;
        focusPause = !hasFocus;
        ApplyPauseReasons();
    }

    public void OnShuffleBoardClicked()
    {
        if (!CanInteract || Board.Instance == null || Board.Instance.isSwapping) return;
        Board.Instance.ResetBoard();
        run.Shuffles++;
        ApplyTimePenalty(5);
        if (Phase != GamePhase.GameOver) ShowMessage("Board shuffled: −5 seconds.");
    }

    public void ApplyTimePenalty(float seconds) => _timer.ApplyPenalty(Mathf.Max(0, seconds));
    public void PlaySound(GameSound sound) => audioFeedback?.Play(sound);
    public void ShowMessage(string message) => GameUIManager?.ShowMessage(message);
    public void ExplainBlockedBoardInput()
    {
        string message = Phase switch
        {
            GamePhase.PowerSelection => "Choose a power before making a move.",
            GamePhase.BossIntro => "Review the boss rule and continue before making a move.",
            GamePhase.LevelComplete => "The next level is about to begin.",
            GamePhase.Paused => "Resume the game before making a move.",
            GamePhase.GameOver => "The round is over. Retry or start a new run.",
            GamePhase.Victory => "Level complete! Start a new run or choose another level.",
            _ => "The board is resolving the previous move."
        };
        ShowMessage(message);
    }

    public void AddOrRemoveItem(Item item, bool add)
    {
        if (item == null) return;
        if (add && !limitedItemPool.Contains(item)) limitedItemPool.Add(item);
        else if (!add && limitedItemPool.Select(i => i.ItemType).Distinct().Count() > 3) limitedItemPool.Remove(item);
    }

    public void GenerateItemPool(int count)
    {
        if (ItemDatabase.items == null || ItemDatabase.items.Length == 0)
            throw new System.InvalidOperationException("No animal items found in Resources/Items.");
        // Keep types stable for the run so animal-specialist builds remain usable.
        if (limitedItemPool.Count > 0) return;
        limitedItemPool = ItemDatabase.items.Where(i => i != null && i.sprite != null)
            .GroupBy(i => i.ItemType).Select(g => g.First()).OrderBy(_ => Random.value)
            .Take(Mathf.Clamp(count, 3, ItemDatabase.items.Length)).ToList();
    }

    public bool DoesCurrentLevelHaveBossPowers() => GetTotalBossPowersInCurrentLevel() > 0;
    public int GetTotalBossPowersInCurrentLevel()
    {
        var level = GameState.Instance?.CurrentLevel;
        if (level == null) return 0;
        return Enumerable.Range(1, level.GetTotalRounds)
            .SelectMany(r => level.GetBossPowersMap().GetBossPowersForRound(r)).Select(p => p.Name).Distinct().Count();
    }
}
