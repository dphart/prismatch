using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GamePhase { PowerSelection, BossIntro, Playing, Resolving, LevelComplete, Paused, GameOver, Victory, LevelIntro }

public class GameController : MonoBehaviour, TimeProvider, LevelInfoProvider, ItemInfoProvider
{
    private const string AnimationSpeedKey = "Prismatch.AnimationSpeedIndex";
    private static readonly float[] AnimationSpeeds = { .5f, 1f, 1.5f, 2f };
    public static float AnimationSpeed => AnimationSpeeds[Mathf.Clamp(PlayerPrefs.GetInt(AnimationSpeedKey, 1), 0, AnimationSpeeds.Length - 1)];
    public void CycleAnimationSpeed()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt(AnimationSpeedKey, 1), 0, AnimationSpeeds.Length - 1);
        PlayerPrefs.SetInt(AnimationSpeedKey, (index + 1) % AnimationSpeeds.Length);
        PlayerPrefs.Save();
        GameUIManager?.UpdateAnimationSpeedSetting();
    }

    private const string SwipeToMoveKey = "Prismatch.SwipeToMove";
    public static bool SwipeToMoveEnabled => PlayerPrefs.GetInt(SwipeToMoveKey, 1) != 0;

    [SerializeField] private PowerUpSelector powerUpSelector;
    [SerializeField] private GameObject bossBackground;
    [SerializeField] private BossBattleUi bossBattleUI;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI restartsRemainingText;
    public ActivePowerUpsUI activePowerUpsUI;
    public BossPowerDisplayUI bossPowerDisplayUI;
    public GameUIManager GameUIManager;
    public List<Item> limitedItemPool = new();
    private readonly List<Item> startingItemPool = new();
    private readonly HashSet<ItemType> cageRemovedThisRound = new();

    private int score, targetScore, blocksCleared, movesRemaining;
    private bool blockRewardBatch;
    private int pendingBlockCredit;
    private readonly HashSet<ItemType> blockRewardAnimals=new();
    private float elapsed;
    private TimerController _timer;
    private RoundManager _roundManager;
    private PauseController _pauseController;
    private PowerManager powerManager;
    private ScoreManager scoreManager;
    private GameAudio audioFeedback;
    private readonly RunSummary run = new();
    private readonly RunAnalytics analytics = new();
    public RunAnalytics Analytics => analytics;
    private int roundRestartsUsed;
    private const int maxRoundRestarts = 1;
    private GamePhase resumePhase = GamePhase.Playing;
    private bool manualPause, focusPause, powersOpen, bossesOpen, settingsOpen, statsOpen;
    private bool restartConfirmationArmed;
    private Coroutine restartConfirmationRoutine;
    private int feedbackCascadeDepth;
    private int lastWarningSecond = -1;
    private int transitionVersion;

    public GamePhase Phase { get; private set; } = GamePhase.PowerSelection;
    public bool CanInteract => Phase == GamePhase.Playing;
    public GoalType CurrentGoal => (GameState.Instance?.CurrentLevel as ILevelSettings)?.Goal ?? GoalType.Score;
    public bool IsTimed => (GameState.Instance?.CurrentLevel as ILevelSettings)?.Mode != FreePlayMode.LimitedMoves;
    public int BlocksCleared => blocksCleared;
    public int LevelBasePointsPerTile => (GameState.Instance?.CurrentLevel as CampaignLevel)?.BasePointsPerTile ?? 0;
    public int MovesRemaining => movesRemaining;
    public float ShuffleCost => ShufflePower.Cost(powerManager.ActivePowers(), IsTimed);
    public string ShuffleCostLabel => ShuffleCost == 0 ? "FREE" : IsTimed ? $"{ShuffleCost:0.#} sec" : $"{ShuffleCost:0} move{(ShuffleCost == 1 ? "" : "s")}";
    public int GoalProgress => CurrentGoal == GoalType.BlocksCleared ? blocksCleared : score;
    public BlockObjective CurrentBlockObjective => (GameState.Instance?.CurrentLevel as CampaignLevel)?.BlockObjectives?.ForRound(CurrentRound) ?? BlockObjective.Any;
    public string BlockObjectiveLabel
    {
        get
        {
            var objective = CurrentBlockObjective;
            var foundation = powerManager?.ActivePowers().OfType<BlockMatchFoundationPower>().FirstOrDefault();
            if (foundation != null && !objective.IsRestricted) return $"BLOCK CREDIT / {foundation.CreditPerTile} PER TILE";
            int additional = powerManager?.ActivePowers().OfType<CollectionPower>().Where(p => p.Style == CollectionStyle.Admission && p.Animal != objective.Animal).Select(p => p.Animal).Distinct().Count() ?? 0;
            return objective.Animal.HasValue && additional > 0 ? objective.Animal.Value + " + " + additional + " OTHER ANIMALS" +
                (objective.Shape == BlockShape.Any ? "" : " / " + BlockObjective.ShapeLabel(objective.Shape)) : objective.IsRestricted ? objective.Label : "BLOCKS MATCHED";
        }
    }
    public bool TargetReached => targetScore > 0 && GoalProgress >= targetScore;
    public int CurrentRound => _roundManager?.CurrentRound ?? 0;
    public int CurrentScore => score;
    public int CurrentTarget => targetScore;
    public float CurrentTime => _timer?.Remaining ?? 0;
    public PowerManager PowerManager => powerManager;
    public int QueuedMultiplier => scoreManager?.nextMatchMulti ?? 1;
    public void ExpandSharedPowerClears(BoardModel model,List<BoardModel.Cell> group,List<BoardModel.Cell> cleared)
    {
        if(CurrentGoal!=GoalType.BlocksCleared||scoreManager==null)return;
        foreach(var reward in scoreManager.LastResult.ExtraClears) {
            int count=SharedPowerRewards.ExpandClears(model,group,cleared,reward.Value);
            if(count>0)GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(reward.Key,$"+{count} clears")},
                group.Select(cell=>Board.Instance.Tiles[cell.X,cell.Y]).ToList());
        }
    }
    public int TimeRemaining => Mathf.CeilToInt(CurrentTime);
    public int TimeElapsed => Mathf.FloorToInt(elapsed);
    public bool isBossLevel => powerManager?.ActiveBossPowers().Count > 0 &&
        !(GameState.Instance?.CurrentLevel is CampaignLevel campaign && campaign.RoundRule.HasValue && CurrentRound < campaign.GetTotalRounds);
    public int currentRound => CurrentRound;
    public int maxRounds => GameState.Instance?.CurrentLevel?.GetTotalRounds ?? 0;
    public List<Item> IconList => limitedItemPool;
    public List<Item> GetActiveItems => limitedItemPool;
    public bool HasSpawnPowers => powerManager.ActivePowers().Any(p => p is AnimalSpawnPower);
    public int SpawnWeight(ItemType animal) => AnimalSpawnPower.Weight(animal,powerManager.ActivePowers());
    public ItemType PickSpawnAnimal()
    {
        var animal=AnimalSpawnPower.Select(limitedItemPool.Select(item=>item.ItemType).Distinct().ToList(),Random.value,SpawnWeight);
        if(Phase is GamePhase.Resolving or GamePhase.Playing)
            foreach(var magnet in powerManager.ActivePowers().OfType<AnimalSpawnPower>().Where(p=>p.Animal==animal))
                GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(magnet,"Favored refill")},null);
        return animal;
    }
    public List<MatchRule> GetEffectiveMatchRules()
    {
        var rules = GameState.Instance?.CurrentLevel?.GetMatchRules(CurrentRound)
            ?.Select(rule => rule.Clone()).ToList() ?? new List<MatchRule> { new(MatchType.Snake, 4) };
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
        if ((Phase == GamePhase.Playing || Phase == GamePhase.Resolving) && !TargetReached && !IsPresentingMechanic) elapsed += Time.deltaTime;
        if (IsTimed) _timer?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        ArchiveAnalytics();
        if (_timer != null) { _timer.OnTick -= UpdateTimeRemaining; _timer.OnExpired -= TriggerGameOver; }
        Time.timeScale = 1;
    }

    public bool IsPresentingMechanic => _timer?.IsPresentationHeld == true;
    public System.IDisposable HoldMechanicPresentation() => _timer.HoldPresentation();
    public void CancelMechanicPresentation() => _timer?.ClearPresentationHolds();

    public void BeginResolution()
    {
        if (!CanInteract) return;
        feedbackCascadeDepth = 0;
        if (!IsTimed) foreach (var boss in powerManager.ActiveBossPowers().OfType<ChallengeBoss>().Where(b => b.UsesMoveTax))
            boss.MoveTax.BeginPlay(Board.Instance?.Tiles?.Cast<Tile>().Where(t => t != null && t.item != null).Select(t => t.item.ItemType) ?? Enumerable.Empty<ItemType>());
        analytics.BeginPlay(score, GoalProgress, elapsed);
        Phase = GamePhase.Resolving;
        if (!IsTimed) { movesRemaining = Mathf.Max(0, movesRemaining - 1); GameUIManager?.UpdateMoves(movesRemaining); }
    }

    public void EndResolution()
    {
        if (Phase == GamePhase.Paused && resumePhase == GamePhase.Resolving) resumePhase = GamePhase.Playing;
        else if (Phase == GamePhase.Resolving) Phase = GamePhase.Playing;
        else return;
        if (!IsTimed) foreach (var boss in powerManager.ActiveBossPowers().OfType<ChallengeBoss>().Where(b => b.UsesMoveTax))
        {
            int extra = boss.MoveTax.FinishPlay(boss.Style);
            if (extra == 0) continue;
            movesRemaining = Mathf.Max(0, movesRemaining - extra);
            GameUIManager?.UpdateMoves(movesRemaining);
            GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(boss,"Extra move")},null);
        }
        GameUIManager?.UpdateAnimalBonus(this, Board.Instance);
        if (feedbackCascadeDepth > 0) audioFeedback?.PlayReward(0f, feedbackCascadeDepth, true);
        feedbackCascadeDepth = 0;
        analytics.EndPlay(score, GoalProgress, elapsed, movesRemaining);
        SaveAnalytics();
        if (Phase != GamePhase.Paused) CheckRoundOutcome();
    }

    private void CheckRoundOutcome()
    {
        if (TargetReached) _ = CompleteRound();
        else if (!IsTimed && movesRemaining <= 0) TriggerGameOver();
        else if (CanInteract && Board.Instance?.Model != null && !Board.Instance.isSwapping &&
            !Board.Instance.Model.HasLegalMove()) OnShuffleBoardClicked();
    }

    public void HandleTileMatch(List<Tile> tiles) => HandleTileMatchWithRewards(tiles,null);
    public void HandleTileMatchWithRewards(List<Tile> tiles,IReadOnlyDictionary<PowerUp,int> sweepRewards)
    {
        if (Phase != GamePhase.Resolving || tiles == null || tiles.Count == 0) return;
        if (!IsTimed) foreach (var boss in powerManager.ActiveBossPowers().OfType<ChallengeBoss>().Where(b => b.UsesMoveTax))
            boss.MoveTax.Observe(tiles.Where(t => t != null && t.item != null).Select(t => t.item.ItemType),
                tiles.Where(t => t != null).Select(t => new BoardModel.Cell(t.x,t.y)).ToList(),
                Board.Instance?.Width ?? 7, Board.Instance?.Height ?? 7, Board.Instance?.wasUserInitiatedMatch() == true);
        foreach (var shuffle in powerManager.ActivePowers().OfType<ShufflePower>()) shuffle.ObserveMatch(Board.Instance);
        var levelBonus = (GameState.Instance?.CurrentLevel as CampaignLevel)?.Bonus ?? LevelBonus.None;
        scoreManager.LevelBonus = levelBonus;
        scoreManager.LevelBasePointsPerTile = LevelBasePointsPerTile;
        scoreManager.LevelMatchInfo = Board.Instance;
        scoreManager.LevelAnimalPool = IconList.Where(item=>item!=null).Select(item=>item.ItemType).Distinct().ToArray();
        scoreManager.RewardGoal = CurrentGoal;
        if(CurrentGoal==GoalType.BlocksCleared) {
            if(!blockRewardBatch)blockRewardAnimals.Clear();
            foreach(var tile in tiles)if(tile!=null&&tile.item!=null)blockRewardAnimals.Add(tile.item.ItemType);
        }
        int added = scoreManager.CalculateScore(tiles,sweepRewards);
        if(Board.Instance?.CampaignBonus==LevelBonus.TreasureTrail && powerManager.ActiveBossPowers().OfType<ChallengeBoss>().Any(b=>b.Style==BossStyle.TreasureCrab)) {
            int full=added;
            added=Board.Instance.Campaign.CreditTreasureBoss(added);
            scoreManager.LastResult.Score=added;
            scoreManager.LastResult.Contributions.Add($"Treasure Crab: {full} -> {added} (treasure releases held points)");
        }
        score = ScoreManager.SaturatingAdd(score, added);
        int blockCredit = tiles.Where(t => t != null).Distinct().Count();
        run.RecordClearedTiles(blockCredit);
        if (CurrentGoal == GoalType.BlocksCleared)
        {
            if (CurrentBlockObjective.IsRestricted || powerManager.ActivePowers().Any(p => p is CollectionPower))
                blockCredit = CurrentBlockObjective.Credit(BlockObjective.Snapshot(tiles), powerManager.ActivePowers(),onActivated:(power,amount)=>scoreManager.LastResult.Activate(power,$"+{amount} blocks"));
            blockCredit = BlockMatchFoundationPower.ApplyCredit(blockCredit,powerManager.ActivePowers());
            // Per-match bonuses are added after foundations, and only for an eligible objective group.
            if(CurrentBlockObjective.Credit(BlockObjective.Snapshot(tiles),powerManager.ActivePowers().OfType<CollectionPower>().Where(p=>p.Style==CollectionStyle.Admission))>0)
                foreach(var reward in scoreManager.LastResult.BlockCredits) {
                    blockCredit=ScoreManager.SaturatingAdd(blockCredit,reward.Value);
                    scoreManager.LastResult.Activate(reward.Key,$"+{reward.Value} blocks");
                }
            foreach (var shuffle in powerManager.ActivePowers().OfType<ShufflePower>()) {
                int extra = shuffle.ClaimBlockCredits(Board.Instance);
                blockCredit = ScoreManager.SaturatingAdd(blockCredit, extra);
                if (extra > 0) { scoreManager.LastResult.Contributions.Add(shuffle.Name + ": +" + extra + " block credits"); scoreManager.LastResult.Activate(shuffle,$"+{extra} blocks"); }
            }
            foreach (var rule in powerManager.ActivePowers().OfType<SharedRulePower>()) {
                int before=rule.EarnedActivations, extra=rule.ExtraCredits(tiles);
                blockCredit=ScoreManager.SaturatingAdd(blockCredit,extra);
                if(rule.EarnedActivations>before)scoreManager.LastResult.Activate(rule,"UPGRADE!");
                else if(extra>0&&!rule.Permanent)scoreManager.LastResult.Activate(rule,$"+{extra} blocks");
            }
            var levelPower=powerManager.Characteristic(levelBonus);
            if(levelPower!=null)blockCredit=levelPower.ApplyReward(blockCredit,
                tiles.Where(tile=>tile!=null).Select(tile=>new BoardModel.Cell(tile.x,tile.y)).Distinct().ToList(),
                Board.Instance?.wasUserInitiatedMatch()!=false,scoreManager.LastResult);
            if(Board.Instance?.Strategy!=null)
                blockCredit=Board.Instance.StrategyMatchReward.Apply(blockCredit);
            foreach (var boss in powerManager.ActiveBossPowers()) {
                int before=blockCredit;blockCredit=boss.ModifyBlockCredit(tiles,blockCredit);
                if(before!=blockCredit)scoreManager.LastResult.Activate(boss,"Block effect");
            }
        }
        AddBlockCredit(blockCredit);
        bool playerCreated = Board.Instance?.wasUserInitiatedMatch() ?? false;
        run.RecordMatch(added, playerCreated);
        analytics.Group(playerCreated, tiles.Where(t => t != null).Distinct().Count(), scoreManager.LastResult.Contributions);
        GameUIManager?.RefreshBottomBar(this);
        Ui.UpdateScore(score);
        GameUIManager?.UpdateGoalProgress(GoalProgress, CurrentGoal);
        foreach(var shuffle in powerManager.ActivePowers().OfType<ShufflePower>()) {
            int progress=shuffle.Progress;
            bool earned=shuffle.RecordMatch(shuffle.MatchInfo,tiles.Where(tile=>tile!=null).Select(tile=>new BoardModel.Cell(tile.x,tile.y)).Distinct().ToList());
            if(earned) { scoreManager.LastResult.Activate(shuffle,"Free shuffle!");PlaySound(GameSound.Power); }
            else if(shuffle.Progress!=progress)scoreManager.LastResult.Activate(shuffle,"Ticket progress",true);
        }
        GameUIManager?.UpdateShuffleCost(this);
        foreach (var power in powerManager.ActivePowers())
        {
            int rewards = power.EarnBudgetRewards(tiles);
            if (rewards <= 0) continue;
            if (IsTimed) _timer.AddTime(rewards * BudgetPower.SecondsPerReward);
            else
            {
                movesRemaining = ScoreManager.SaturatingAdd(movesRemaining, rewards);
                GameUIManager?.UpdateMoves(movesRemaining);
            }
            scoreManager.LastResult.Activate(power,IsTimed?$"+{rewards*BudgetPower.SecondsPerReward}s":$"+{rewards} move");

            PlaySound(GameSound.Power);
        }
        GameUIManager?.ShowScoreBreakdown(scoreManager.LastResult,tiles);
        if (added == 0 && CurrentGoal == GoalType.Score) PlaySound(GameSound.Blocked);
        if (scoreManager.LastResult.NextMultiplier > 1) PlaySound(GameSound.Power);
        // Stop the clock at the target, but wait for board animations to finish.
        if (TargetReached) _timer.Stop();
    }

    public void ShowClearFeedback(int gained, int cascadeDepth, List<Tile> tiles)
    {
        if (gained <= 0 || Phase != GamePhase.Resolving) return;
        feedbackCascadeDepth = cascadeDepth;
        float impact = CurrentTarget > 0 ? (float)gained / CurrentTarget : 0;
        audioFeedback?.PlayReward(impact, cascadeDepth);
        GameUIManager?.ShowRewardBurst(impact, tiles);
    }

    public void AddStrategyMoves(int amount)
    {
        if(amount<=0||IsTimed)return;
        movesRemaining=ScoreManager.SaturatingAdd(movesRemaining,amount);
        GameUIManager?.UpdateMoves(movesRemaining);
    }
    public void RecordUnscoredClear(int count) { run.RecordClearedTiles(count);analytics.BonusClears(count); }

    public void BeginBlockRewardBatch()
    {
        blockRewardBatch=CurrentGoal==GoalType.BlocksCleared;pendingBlockCredit=0;blockRewardAnimals.Clear();
    }
    public void CommitBlockRewardBatch()
    {
        if(!blockRewardBatch)return;
        int credit=pendingBlockCredit;blockRewardBatch=false;pendingBlockCredit=0;
        AddBlockCredit(credit);blockRewardAnimals.Clear();
        GameUIManager?.UpdateGoalProgress(GoalProgress,CurrentGoal);
        if(TargetReached)_timer.Stop();
    }
    private void AddBlockCredit(int credit)
    {
        if(blockRewardBatch){pendingBlockCredit=ScoreManager.SaturatingAdd(pendingBlockCredit,credit);return;}
        if(CurrentGoal==GoalType.BlocksCleared) {
            credit=AnimalPairPower.ApplyBlockPenalty(credit,blockRewardAnimals,powerManager.ActivePowers());
            credit=RosterTradeoffPower.ApplyBlockRewards(credit,powerManager.ActivePowers());
        }
        blocksCleared=ScoreManager.SaturatingAdd(blocksCleared,credit);
    }

    public void HandleBonusBlocksCleared(int count)
    {
        if (Phase != GamePhase.Resolving || count <= 0) return;
        run.RecordClearedTiles(count);
        analytics.BonusClears(count);
        if (CurrentGoal == GoalType.BlocksCleared && CurrentBlockObjective.IsRestricted) return;
        AddBlockCredit(count);
        GameUIManager?.UpdateGoalProgress(GoalProgress, CurrentGoal);
        if (TargetReached) _timer.Stop();
    }

    public void HandleBonusBlocksCleared(List<Tile> tiles)
    {
        if (Phase != GamePhase.Resolving || tiles == null || tiles.Count == 0) return;
        run.RecordClearedTiles(tiles.Where(t => t != null).Distinct().Count());
        analytics.BonusClears(tiles.Where(t => t != null).Distinct().Count());
        int credit = CurrentBlockObjective.IsRestricted || powerManager.ActivePowers().Any(p => p is CollectionPower)
            ? CurrentBlockObjective.Credit(BlockObjective.Snapshot(tiles), powerManager.ActivePowers(), bonusClear: true, onActivated:(power,amount)=>GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(power,"Admitted credit")},null))
            : tiles.Where(t => t != null).Distinct().Count();
        if(CurrentGoal==GoalType.BlocksCleared&&Board.Instance?.Strategy!=null) {
            int before=credit;
            credit=Board.Instance.Strategy.Reward(Board.Instance.Model,tiles.Where(t=>t!=null).Select(t=>new BoardModel.Cell(t.x,t.y)).Distinct().ToList()).Apply(credit);
            if(credit!=before)GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(powerManager.Characteristic(Board.Instance.CampaignBonus),"Boosted clears")},null);
        }
        AddBlockCredit(credit);
        GameUIManager?.UpdateGoalProgress(GoalProgress, CurrentGoal);
        if (TargetReached) _timer.Stop();
    }

    private async System.Threading.Tasks.Task CompleteRound()
    {
        if (Phase == GamePhase.LevelComplete || Phase == GamePhase.Victory) return;
        int version = ++transitionVersion;
        _timer.Stop();
        run.FinishRound(CurrentRound, score, targetScore, elapsed, true, CurrentGoal, GoalProgress, blocksCleared, movesRemaining);
        analytics.FinishRound("won", score, GoalProgress, elapsed, movesRemaining);
        UnityCloudAnalytics.FinishRound(analytics.Data, analytics.Current);
        if (CurrentRound >= maxRounds) analytics.Close("won");
        SaveAnalytics();
        Phase = GamePhase.LevelComplete;
        GameUIManager?.ShowMessage(CurrentRound >= maxRounds ? "LEVEL COMPLETE!" : "ROUND COMPLETE!", 1f);
        if (Board.Instance != null) await Board.Instance.PlayLevelCompleteExit();
        float finishAt = Time.realtimeSinceStartup + .55f;
        while (Time.realtimeSinceStartup < finishAt)
        {
            await Awaitable.NextFrameAsync();
            if (this == null || version != transitionVersion) return;
        }
        if (this == null || version != transitionVersion) return;

        if (CurrentRound >= maxRounds)
        {
            Phase = GamePhase.Victory;
            Board.Instance?.CancelResolution();
            if (GameState.Instance.CurrentLevel is not FreePlayConfig)
                ProgressStore.MarkCompleted(GameState.Instance.CurrentLevel.GetLevelNumber, run.TotalScore,
                    (GameState.Instance.CurrentLevel as CampaignLevel)?.Difficulty ?? 1);
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
        run.FinishRound(CurrentRound, score, targetScore, elapsed, false, CurrentGoal, GoalProgress, blocksCleared, movesRemaining);
        analytics.FinishRound("failed", score, GoalProgress, elapsed, movesRemaining);
        UnityCloudAnalytics.FinishRound(analytics.Data, analytics.Current);
        analytics.Close("failed");
        SaveAnalytics();
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
        ArchiveAnalytics();
        transitionVersion++;
        GameUIManager?.HideLevelIntro();
        Board.Instance?.CancelResolution();
        ClearPauseReasons();
        _timer.Stop();
        score = blocksCleared = 0;blockRewardBatch=false;pendingBlockCredit=0;blockRewardAnimals.Clear();
        elapsed = 0;
        roundRestartsUsed = 0;
        powerManager.ConfigureLevelPowers(GameState.Instance.CurrentLevel as CampaignLevel);
        scoreManager.Reset();
        powerManager.ClearPowers();
        GameUIManager?.UpdateShuffleCost(this);
        powerManager.ClearBossPowers();
        powerUpSelector?.ResetOfferedList();
        _roundManager.ResetRounds();
        run.Reset();
        limitedItemPool.Clear();
        startingItemPool.Clear();cageRemovedThisRound.Clear();
        analytics.Start(GameState.Instance.CurrentLevel, Application.version, Application.platform.ToString());
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
        GameUIManager?.HideLevelIntro();
        Phase = GamePhase.PowerSelection;
        Ui.HideGameOver();
        GameUIManager?.HideRunEnd();
        GameUIManager?.ShowBoardStats(false);
        powerUpSelector?.Hide();
        bossBattleUI?.Hide();
        bool startingLevel = CurrentRound == 0;
        var context = _roundManager.BeginRound(advanceRound);
        cageRemovedThisRound.Clear();
        if (!context.BossPowersPersist) powerManager.ClearBossPowers();
        foreach (var boss in context.BossPowers)
        {
            if (!powerManager.ActiveBossPowers().Any(p => p.Name == boss.Name))
                powerManager.RegisterBossPower((BossPowers)boss.Clone());
        }
        score = blocksCleared = 0;blockRewardBatch=false;pendingBlockCredit=0;blockRewardAnimals.Clear();
        elapsed = 0;
        targetScore = context.TargetScore;
        powerManager.ConfigureLevelPowers(GameState.Instance.CurrentLevel as CampaignLevel);
        scoreManager.Reset();
        GameUIManager?.ApplyHabitatTheme(isBossLevel);
        GameUIManager?.RefreshBottomBar(this);
        GameUIManager?.UpdateShuffleCost(this);
        _timer.Reset(context.RoundTime);
        movesRemaining = (GameState.Instance.CurrentLevel as ILevelSettings)?.Moves ?? 0;
        GameUIManager?.UpdateBlockObjective(BlockObjectiveLabel);
        if (!IsTimed) GameUIManager?.UpdateMoves(movesRemaining);
        lastWarningSecond = -1;
        Ui.UpdateTarget(targetScore);
        Ui.UpdateScore(score);
        GameUIManager?.UpdateGoalProgress(GoalProgress, CurrentGoal);
        Ui.UpdateRound(CurrentRound);
        run.BeginRound();
        Board.Instance.GenerateBoard();
        if (analytics.Data == null) analytics.Start(GameState.Instance.CurrentLevel, Application.version, Application.platform.ToString());
        analytics.BeginRound(CurrentRound, targetScore, CurrentGoal == GoalType.BlocksCleared ? CurrentBlockObjective.Label : "Points",
            string.Join(" | ", GetEffectiveMatchRules()) + " | Level bonus: " +
                ((GameState.Instance.CurrentLevel as CampaignLevel)?.Bonus ?? LevelBonus.None) + " | Level base: " + LevelBasePointsPerTile +
                " | Lightning: " + ((GameState.Instance.CurrentLevel as CampaignLevel)?.LightningLines == true ? "clear-and-board-charge-2x" : "off"),
            string.Join(" | ", powerManager.ActiveBossPowers().Select(p => p.Name + ": " + p.GetCurrentPowerDescription())),
            limitedItemPool.Select(i => i.ItemType), powerManager.ActivePowers());
        SaveAnalytics();
        if (bossBackground != null) bossBackground.SetActive(isBossLevel);
        ShowMessage($"Match {string.Join(", ", GetEffectiveMatchRules())}. Shuffle: {ShuffleCostLabel}." + (IsTimed ? "" : powerManager.ActiveBossPowers().OfType<ChallengeBoss>().Any(b => b.UsesMoveTax)
            ? " Boss matches can cost 2 moves. Cascades are free." : " Swaps cost 1 move. Cascades are free."));
        if (startingLevel || isBossLevel || CurrentGoal == GoalType.BlocksCleared && CurrentBlockObjective.IsRestricted)
        {
            Phase = GamePhase.LevelIntro;
            if (GameUIManager == null || !GameUIManager.ShowLevelIntro(this)) ContinueFromLevelIntro();
        }
        else powerUpSelector?.ShowChoices();
    }

    public void ContinueFromLevelIntro()
    {
        if (Phase != GamePhase.LevelIntro) return;
        GameUIManager?.HideLevelIntro();
        Phase = GamePhase.PowerSelection;
        powerUpSelector?.ShowChoices();
    }

    public void ShowPowerPickerAfterBossIntro()
    {
        if (Phase != GamePhase.BossIntro) return;
        Phase = GamePhase.PowerSelection;
        powerUpSelector.ShowChoices();
    }

    public void RegisterSelectedPower(PowerUp power)
    {
        if (Phase == GamePhase.PowerSelection && power != null &&
            (!power.OpeningOnly || CurrentRound == 1 && powerManager.ActivePowers().Count == 0 ||
                (GameState.Instance?.CurrentLevel as CampaignLevel)?.AllowStarterRewards == true) &&
            power.Supports(CurrentGoal, IsTimed ? FreePlayMode.Timed : FreePlayMode.LimitedMoves) &&
            power.CanUseWithMatchRules(GetEffectiveMatchRules()) && power.CanOfferWith(powerManager.ActivePowers()) &&
            PowerOfferBuilder.PreservesActiveMatches(power, GetEffectiveMatchRules(), powerManager.ActivePowers()) &&
            power.SupportsItems(limitedItemPool.Select(item => item.ItemType).ToList()))
        {
            powerManager.RegisterPower(power);
            RebuildItemPool();
            analytics.Pick(power, powerManager.ActivePowers(), limitedItemPool.Select(i => i.ItemType));
            UnityCloudAnalytics.Pick(analytics.Data, analytics.Current, analytics.Data?.picks.LastOrDefault());
            SaveAnalytics();
            GameUIManager?.UpdateBlockObjective(BlockObjectiveLabel);
            GameUIManager?.UpdateGoalProgress(GoalProgress, CurrentGoal);
            Board.Instance?.ReplaceRemovedAnimals();
            Board.Instance?.SetMatchRules(GetEffectiveMatchRules());
            if (power is MatchFoundationPower) Board.Instance?.ResetBoard();
            GameUIManager?.RefreshBottomBar(this);
        }
    }

    public void OnPowerUpSelected()
    {
        if (Phase != GamePhase.PowerSelection) return;
        activePowerUpsUI?.HidePanel();
        powersOpen = false;
        Phase = GamePhase.Playing;
        Board.Instance?.SetVisible(true);
        GameUIManager?.ShowBoardStats(true);
        if (IsTimed) _timer.Start();
        PlaySound(GameSound.Power);
    }

    public void RemoveAllPowers()
    {
        powerManager.ClearPowers();
        GameUIManager?.RefreshBottomBar(this);
    }

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
        if ((!IsGameplayPhase() && Phase != GamePhase.PowerSelection) || activePowerUpsUI == null) return;
        activePowerUpsUI.TogglePanel();
        powersOpen = activePowerUpsUI.IsOpen;
        if (Phase == GamePhase.PowerSelection)
        {
            powerUpSelector?.SetInventoryOverlay(powersOpen);
            return;
        }
        ApplyPauseReasons();
    }
    public void ToggleBossPowerScreen()
    {
        if (!IsGameplayPhase() || bossPowerDisplayUI == null) return;
        bossPowerDisplayUI.TogglePanel();
        bossesOpen = bossPowerDisplayUI.IsOpen;
        ApplyPauseReasons();
    }

    public void SetStatsOpen(bool open)
    {
        statsOpen = open;
        if (IsGameplayPhase()) ApplyPauseReasons();
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

    public void ToggleSwipeToMove()
    {
        PlayerPrefs.SetInt(SwipeToMoveKey, SwipeToMoveEnabled ? 0 : 1);
        PlayerPrefs.Save();
        GameUIManager?.UpdateSwipeToMoveSetting(SwipeToMoveEnabled);
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
        bool paused = manualPause || focusPause || powersOpen || bossesOpen || settingsOpen || statsOpen;
        if (paused && Phase != GamePhase.Paused)
        {
            resumePhase = Phase; Phase = GamePhase.Paused; _pauseController.Pause();
        }
        else if (!paused && Phase == GamePhase.Paused)
        {
            Phase = resumePhase; _pauseController.Resume();
            if (Phase == GamePhase.Playing) CheckRoundOutcome();
        }
        GameUIManager?.ShowPaused(Phase == GamePhase.Paused && (manualPause || focusPause));
    }

    private void ClearPauseReasons()
    {
        manualPause = focusPause = powersOpen = bossesOpen = settingsOpen = statsOpen = false;
        GameUIManager?.HideStatDetails();
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
        float cost = ShuffleCost;
        if (!IsTimed && movesRemaining < cost)
        {
            ShowMessage($"Shuffle needs {ShuffleCostLabel}.");
            return;
        }
        Board.Instance.ResetBoard();
        var freeProvider=powerManager.ActivePowers().OfType<ShufflePower>().FirstOrDefault(p=>p.Style==ShuffleStyle.Opening&&p.FreeUses>0)
            ?? powerManager.ActivePowers().OfType<ShufflePower>().FirstOrDefault(p=>p.FreeUses>0);
        if (cost == 0 && ShufflePower.SpendFreeUse(powerManager.ActivePowers()))
            GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(freeProvider,"Free shuffle")},null);
        if(cost>0)foreach(var discount in powerManager.ActivePowers().OfType<ShufflePower>().Where(p=>p.Style==ShuffleStyle.Discount))
            GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(discount,"Discount")},null);
        foreach (var shuffle in powerManager.ActivePowers().OfType<ShufflePower>()) shuffle.NotifyShuffle();
        run.Shuffles++;
        if (analytics.Current != null && analytics.Current.status == "active") analytics.Current.shuffles++;
        if (IsTimed) ApplyTimePenalty(cost);
        else { movesRemaining = Mathf.Max(0, movesRemaining - (int)cost); GameUIManager?.UpdateMoves(movesRemaining); CheckRoundOutcome(); }
        GameUIManager?.UpdateShuffleCost(this);
        analytics.Checkpoint(score, GoalProgress, elapsed, movesRemaining);
        SaveAnalytics();
        if (Phase != GamePhase.GameOver) {
            string message = cost == 0 ? "Board shuffled for free!" : IsTimed
                ? $"Board shuffled: -{cost:0.#} seconds." : $"Board shuffled: -{cost:0} move{(cost == 1 ? "" : "s")}.";
            if (powerManager.ActivePowers().OfType<ShufflePower>().Any(p => p.Armed))
                message += CurrentGoal == GoalType.Score ? " Next play: double points!" : " Next matching move: +8 block credits!";
            ShowMessage(message);
        }
    }

    public void ApplyTimePenalty(float seconds)
    {
        if (IsTimed) _timer.ApplyPenalty(Mathf.Max(0, seconds));
    }

    private void SaveAnalytics()
    {
        if (Application.isPlaying) AnalyticsStore.Save(analytics.Data);
    }
    private void ArchiveAnalytics()
    {
        bool unfinished = analytics.Current?.status == "active";
        analytics.EndPlay(score, GoalProgress, elapsed, movesRemaining, interrupted: true);
        if (analytics.Data?.status == "active") analytics.Close("abandoned");
        if (unfinished) UnityCloudAnalytics.FinishRound(analytics.Data, analytics.Current);
        SaveAnalytics();
    }
    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        analytics.Checkpoint(score, GoalProgress, elapsed, movesRemaining);
        SaveAnalytics();
    }
    public string ExportAnalytics()
    {
        analytics.Checkpoint(score, GoalProgress, elapsed, movesRemaining);
        SaveAnalytics();
        try { return AnalyticsStore.Export(); }
        catch (System.Exception error) { Debug.LogWarning(error); return "Could not export analytics: " + error.Message; }
    }
    public AnalyticsRun AnalyticsSnapshot()
    {
        analytics.Checkpoint(score, GoalProgress, elapsed, movesRemaining);
        return analytics.Data;
    }
    public void PlaySound(GameSound sound) => audioFeedback?.Play(sound);
    public void ShowMessage(string message) => GameUIManager?.ShowMessage(message);
    public void ExplainBlockedBoardInput()
    {
        string message = Phase switch
        {
            GamePhase.LevelIntro => "Review the level and press Continue to choose your power.",
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
        if (item == null || cageRemovedThisRound.Contains(item.ItemType)) return;
        if (add && !limitedItemPool.Contains(item)) limitedItemPool.Add(item);
        else if (!add && limitedItemPool.Select(i => i.ItemType).Distinct().Count() > 3) limitedItemPool.Remove(item);
    }

    public void ApplyCageRemovals(IEnumerable<ItemType> animals)
    {
        cageRemovedThisRound.UnionWith(animals);
        limitedItemPool.RemoveAll(item=>cageRemovedThisRound.Contains(item.ItemType));
    }

    public void GenerateItemPool(int count)
    {
        if (ItemDatabase.items == null || ItemDatabase.items.Length == 0)
            throw new System.InvalidOperationException("No animal items found in Resources/Items.");
        // Keep types stable for the run so animal-specialist builds remain usable.
        if (startingItemPool.Count > 0) { RebuildItemPool(); return; }
        limitedItemPool = ItemDatabase.items.Where(i => i != null && i.sprite != null && (int)i.ItemType <= (int)ItemType.Walrus)
            .GroupBy(i => i.ItemType).Select(g => g.First()).OrderBy(_ => Random.value)
            .Take(Mathf.Clamp(count, 3, 7)).ToList();
        startingItemPool.AddRange(limitedItemPool);
        RebuildItemPool();
    }

    private void RebuildItemPool()
    {
        if (startingItemPool.Count == 0) return;
        limitedItemPool = new List<Item>(startingItemPool);
        foreach (var power in powerManager.ActivePowers().OfType<RosterTradeoffPower>())
        {
            var item = ItemDatabase.items.FirstOrDefault(item => item != null && item.sprite != null && item.ItemType == power.Animal);
            if (item != null) AddOrRemoveItem(item, power.AddsAnimal);
        }
        var objectives = (GameState.Instance?.CurrentLevel as CampaignLevel)?.BlockObjectives;
        var required = (objectives?.RequiredAnimals ?? System.Linq.Enumerable.Empty<ItemType>())
            .Concat(powerManager.ActivePowers().OfType<CollectionPower>().Where(p => p.Style == CollectionStyle.Admission).Select(p => p.Animal));
        foreach (var animal in required.Distinct())
        {
            var item = ItemDatabase.items.FirstOrDefault(i => i != null && i.sprite != null && i.ItemType == animal);
            if (item != null) AddOrRemoveItem(item, true);
            else throw new System.InvalidOperationException("Missing collection animal asset: " + animal);
        }
        limitedItemPool.RemoveAll(item=>cageRemovedThisRound.Contains(item.ItemType));
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
