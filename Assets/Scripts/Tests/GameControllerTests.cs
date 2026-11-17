using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

[TestFixture]
public class GameControllerTests
{
    private List<Object> _createdAssets;
    private List<GameObject> _createdObjects;
    private GameController _controller;

    [SetUp]
    public void SetUp()
    {
        _createdAssets = new List<Object>();
        _createdObjects = new List<GameObject>();

        ResetGameStateSingleton();
        CreateGameState();
        _controller = CreateController();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var asset in _createdAssets)
        {
            Object.DestroyImmediate(asset);
        }
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        ResetGameStateSingleton();
        Time.timeScale = 1f;
    }

    [Test]
    public void LevelIntroContinuesToPowerSelectionBeforeStartingTimer()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig();
        var canvas = new GameObject("IntroTestCanvas", typeof(Canvas));
        _createdObjects.Add(canvas);
        _controller.GameUIManager.transform.SetParent(canvas.transform, false);
        var timer = (TimerController)GetPrivateField(_controller, "_timer");
        timer.Reset(60f);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.LevelIntro);
        _controller.GameUIManager.ShowLevelIntro(_controller);
        _controller.OnPowerUpSelected();
        Assert.AreEqual(GamePhase.LevelIntro, _controller.Phase);
        Assert.IsFalse(_controller.CanInteract);
        Assert.IsFalse(timer.IsRunning);
        _controller.BeginResolution();
        Assert.AreEqual(GamePhase.LevelIntro, _controller.Phase);
        _controller.ContinueFromLevelIntro();
        Assert.AreEqual(GamePhase.PowerSelection, _controller.Phase);
        Assert.IsFalse(_controller.CanInteract);
        Assert.IsFalse(timer.IsRunning);
        _controller.OnPowerUpSelected();
        Assert.IsTrue(_controller.CanInteract);
        Assert.IsTrue(timer.IsRunning);
    }

    [Test]
    public void LevelBriefingDescribesBlocksMovesAndGravity()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(goal: GoalType.BlocksCleared,
            mode: FreePlayMode.LimitedMoves, replacement: ReplacementStrategy.West);
        SetPrivateField(_controller, "targetScore", 50);
        SetPrivateField(_controller, "movesRemaining", 12);
        string briefing = GameUIManager.BuildLevelBriefing(_controller);
        StringAssert.Contains("Match 50 blocks within 12 moves", briefing);
        StringAssert.Contains("Gravity left; blocks enter from the right", briefing);
    }

    [Test]
    public void DoesCurrentLevelHaveBossPowers_ReturnsFalseWhenNoneConfigured()
    {
        GameState.Instance.CurrentLevel = new FakeLevelNoBossPowers();

        bool hasBossPowers = _controller.DoesCurrentLevelHaveBossPowers();

        Assert.IsFalse(hasBossPowers);
    }

    [Test]
    public void DoesCurrentLevelHaveBossPowers_ReturnsTrueWhenAnyRoundHasBossPower()
    {
        GameState.Instance.CurrentLevel = new FakeLevelWithBossPowers();

        bool hasBossPowers = _controller.DoesCurrentLevelHaveBossPowers();

        Assert.IsTrue(hasBossPowers);
    }

    [Test]
    public void GetTotalBossPowersInCurrentLevel_CountsUniqueAcrossRounds()
    {
        GameState.Instance.CurrentLevel = new FakeLevelWithBossPowers();

        int count = _controller.GetTotalBossPowersInCurrentLevel();

        Assert.AreEqual(2, count, "Should dedupe powers by name across rounds.");
    }

    [Test]
    public void ApplyTimePenalty_ClampsToZeroAndUpdatesTimerUI()
    {
        var ui = CreateUiManager();
        _controller.GameUIManager = ui;

        var timer = (TimerController)GetPrivateField(_controller, "_timer");
        timer.Reset(2f);

        _controller.ApplyTimePenalty(3f);

        Assert.AreEqual(0f, timer.Remaining);
        var timerText = (TextMeshProUGUI)GetPrivateField(ui, "timerText");
        StringAssert.Contains("0:00", timerText.text);
    }

    [Test]
    public void TogglePause_TogglesTimeScaleBetweenZeroAndOne()
    {
        Time.timeScale = 1f;
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Playing);

        _controller.TogglePause();
        Assert.AreEqual(0f, Time.timeScale);

        _controller.TogglePause();
        Assert.AreEqual(1f, Time.timeScale);
    }

    [TestCase(GoalType.Score, 100, 0, true)]
    [TestCase(GoalType.BlocksCleared, 1000, 99, false)]
    [TestCase(GoalType.BlocksCleared, 0, 100, true)]
    public void Goal_UsesConfiguredMetric(GoalType goal, int score, int blocks, bool reached)
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(goal: goal, target: 100);
        SetPrivateField(_controller, "targetScore", 100);
        SetPrivateField(_controller, "score", score);
        SetPrivateField(_controller, "blocksCleared", blocks);
        Assert.AreEqual(reached, _controller.TargetReached);
    }

    [Test]
    public void LimitedMoves_ConsumesOnePerSwapAndFailsAfterResolution()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(mode: FreePlayMode.LimitedMoves);
        SetPrivateField(_controller, "movesRemaining", 1);
        SetPrivateField(_controller, "targetScore", 400);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Playing);
        _controller.BeginResolution();
        _controller.BeginResolution();
        Assert.AreEqual(0, _controller.MovesRemaining);
        Assert.AreEqual(GamePhase.Resolving, _controller.Phase);
        _controller.EndResolution();
        Assert.AreEqual(GamePhase.GameOver, _controller.Phase);
    }

    [Test]
    public void LimitedMoves_IgnoresTimePenalty()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(mode: FreePlayMode.LimitedMoves);
        var timer = (TimerController)GetPrivateField(_controller, "_timer");
        timer.Reset(60);
        _controller.ApplyTimePenalty(5);
        Assert.AreEqual(60, timer.Remaining);
    }

    [Test]
    public void LastMove_ReachingGoalDoesNotFail()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(mode: FreePlayMode.LimitedMoves);
        SetPrivateField(_controller, "targetScore", 400);
        SetPrivateField(_controller, "score", 400);
        SetPrivateField(_controller, "movesRemaining", 0);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Resolving);
        _controller.TriggerGameOver();
        Assert.AreEqual(GamePhase.Resolving, _controller.Phase);
        Assert.IsTrue(_controller.TargetReached);
    }

    [Test]
    public void FreePlay_ClonePreservesSettingsAndBoundsInvalidValues()
    {
        var config = new FreePlayConfig(MatchType.Vertical, 1, 50, GoalType.BlocksCleared, 250,
            FreePlayMode.LimitedMoves, 90, 30);
        var clone = (FreePlayConfig)config.CloneNew();
        Assert.AreNotSame(config, clone);
        Assert.AreEqual(3, clone.Width);
        Assert.AreEqual(10, clone.Height);
        Assert.AreEqual(GoalType.BlocksCleared, clone.Goal);
        Assert.AreEqual(250, clone.Target);
        Assert.AreEqual(30, clone.Moves);
        Assert.AreEqual(90, clone.Seconds);
        Assert.AreEqual(FreePlayMode.LimitedMoves, clone.Mode);
        Assert.AreEqual(MatchType.Vertical, clone.GetMatchRules(1)[0].type);
    }

    [Test]
    public void BlockGoal_CountsZeroPointMatchesAndCascadesWithoutExtraMoves()
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(goal: GoalType.BlocksCleared,
            target: 6, mode: FreePlayMode.LimitedMoves);
        SetPrivateField(_controller, "targetScore", 6);
        SetPrivateField(_controller, "movesRemaining", 2);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Playing);
        var tiles = new List<Tile>();
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Matched tile");
            _createdObjects.Add(go);
            tiles.Add(go.AddComponent<Tile>());
        }
        _controller.BeginResolution();
        _controller.HandleTileMatch(tiles);
        _controller.HandleTileMatch(tiles); // Refilled tiles in a cascade are new clears.
        Assert.AreEqual(0, _controller.CurrentScore);
        Assert.AreEqual(6, _controller.BlocksCleared);
        Assert.AreEqual(1, _controller.MovesRemaining);
        Assert.IsTrue(_controller.TargetReached);
        _controller.ResetGame();
        Assert.AreEqual(0, _controller.BlocksCleared);
        Assert.AreEqual(0, _controller.CurrentScore);
    }

    [Test]
    public void CollectionGoalCreditsOnlyEligibleMatchesAndAllowsAnotherAnimal()
    {
        GameState.Instance.CurrentLevel = new CampaignLevel(99, "Collection test", "",
            new FreePlayConfig(goal: GoalType.BlocksCleared, target: 100, levels: 1), bossFinal: false,
            blockObjectives: new BlockObjectives(new Dictionary<int, BlockObjective> { [1] = new(ItemType.Duck) }));
        ((RoundManager)GetPrivateField(_controller, "_roundManager")).BeginRound(true);
        SetPrivateField(_controller, "targetScore", 100);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Resolving);
        List<Tile> Tiles(ItemType animal)
        {
            var result = new List<Tile>();
            var item = ScriptableObject.CreateInstance<Item>(); item.ItemType = animal; _createdAssets.Add(item);
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Collection tile"); _createdObjects.Add(go);
                var tile = go.AddComponent<Tile>(); tile.x = i; tile.y = 0; tile.item = item; result.Add(tile);
            }
            return result;
        }
        var ducks = Tiles(ItemType.Duck);
        var elephants = Tiles(ItemType.Elephant);
        _controller.HandleTileMatch(elephants);
        Assert.That(_controller.GoalProgress, Is.Zero);
        _controller.HandleTileMatch(ducks);
        Assert.That(_controller.GoalProgress, Is.EqualTo(3));
        // Inject powers to exercise credit independently of the picker and refill assets.
        _controller.PowerManager.ActivePowers().Add(new CollectionPower(CollectionStyle.Admission, ItemType.Elephant));
        _controller.PowerManager.ActivePowers().Add(new CollectionPower(CollectionStyle.AnimalCredit, ItemType.Elephant));
        _controller.HandleTileMatch(elephants);
        Assert.That(_controller.GoalProgress, Is.EqualTo(9));
        _controller.HandleBonusBlocksCleared(elephants);
        Assert.That(_controller.GoalProgress, Is.EqualTo(12), "Bonus clears receive no match credit multiplier.");
        _controller.HandleBonusBlocksCleared(Tiles(ItemType.Bear));
        Assert.That(_controller.GoalProgress, Is.EqualTo(12));
        Assert.That(_controller.BlockObjectiveLabel, Does.Contain("OTHER ANIMALS"));
    }

    [TestCase(true,12)]
    [TestCase(false,8)]
    public void RosterAdjustmentWaitsForMatchedAndBonusClearsBeforeRounding(bool welcome,int expected)
    {
        GameState.Instance.CurrentLevel=new FreePlayConfig(goal:GoalType.BlocksCleared,target:9,levels:1);
        ((RoundManager)GetPrivateField(_controller,"_roundManager")).BeginRound(true);
        SetPrivateField(_controller,"targetScore",9);
        SetPrivateField(_controller,"<Phase>k__BackingField",GamePhase.Resolving);
        _controller.PowerManager.ActivePowers().Add(new RosterTradeoffPower(ItemType.Moose,welcome));
        _controller.BeginBlockRewardBatch();
        _controller.HandleBonusBlocksCleared(5);
        _controller.HandleBonusBlocksCleared(5);
        Assert.That(_controller.GoalProgress,Is.Zero,"Do not publish unfinished credit or stop the timer early.");
        Assert.That(_controller.TargetReached,Is.False);
        _controller.CommitBlockRewardBatch();
        Assert.That(_controller.GoalProgress,Is.EqualTo(expected));
        Assert.That(_controller.TargetReached,Is.EqualTo(welcome));
        _controller.CommitBlockRewardBatch();
        Assert.That(_controller.GoalProgress,Is.EqualTo(expected),"Cannot apply the adjustment twice.");
    }

    [TestCase(1,0,0)]
    [TestCase(5,5,5)]
    public void PactPenaltyIncludesExtraClearsAndRoundsOnlyAfterTheWave(int matched,int extra,int expected)
    {
        GameState.Instance.CurrentLevel=new FreePlayConfig(goal:GoalType.BlocksCleared,target:100,levels:1);
        ((RoundManager)GetPrivateField(_controller,"_roundManager")).BeginRound(true);
        SetPrivateField(_controller,"targetScore",100);
        SetPrivateField(_controller,"<Phase>k__BackingField",GamePhase.Resolving);
        _controller.PowerManager.ActivePowers().Add(new AnimalPairPower(ItemType.Duck,ItemType.Walrus));
        var item=ScriptableObject.CreateInstance<Item>();item.ItemType=ItemType.Duck;_createdAssets.Add(item);
        var tiles=new List<Tile>();
        for(int i=0;i<matched;i++) {
            var go=new GameObject("Pact match tile");_createdObjects.Add(go);
            var tile=go.AddComponent<Tile>();tile.item=item;tile.x=i;tiles.Add(tile);
        }
        _controller.BeginBlockRewardBatch();
        _controller.HandleTileMatch(tiles);_controller.HandleBonusBlocksCleared(extra);
        Assert.That(_controller.GoalProgress,Is.Zero);
        _controller.CommitBlockRewardBatch();
        Assert.That(_controller.GoalProgress,Is.EqualTo(expected));
    }

    [TestCase(MatchType.Horizontal, 4, 8, 7, 4)]
    [TestCase(MatchType.Vertical, 8, 4, 7, 4)]
    [TestCase(MatchType.Snake, 7, 7, 5, 5)]
    [TestCase(MatchType.Snake, 7, 7, 1, 2)]
    public void FreePlay_MinimumMatchLengthIsBoundedAndCloned(MatchType type, int width, int height, int requested, int expected)
    {
        var config = new FreePlayConfig(type, width, height, minimumMatchLength: requested);
        var clone = (FreePlayConfig)config.CloneNew();
        Assert.AreEqual(expected, config.MinimumMatchLength);
        Assert.AreEqual(expected, clone.GetMatchRules(1)[0].minimumLength);
        StringAssert.Contains($"{expected}+", clone.GetLevelDescription());
    }

    [TestCase(ReplacementStrategy.InPlace)]
    [TestCase(ReplacementStrategy.South)]
    [TestCase(ReplacementStrategy.North)]
    [TestCase(ReplacementStrategy.West)]
    [TestCase(ReplacementStrategy.East)]
    public void FreePlay_ClonePreservesReplacementStrategy(ReplacementStrategy strategy)
    {
        var clone = (FreePlayConfig)new FreePlayConfig(replacement: strategy).CloneNew();
        Assert.AreEqual(strategy, clone.Replacement);
    }

    [TestCase(GoalType.Score)]
    [TestCase(GoalType.BlocksCleared)]
    public void FreePlay_TargetGrowthAdvancesAndRetryKeepsTheSameTarget(GoalType goal)
    {
        var config = new FreePlayConfig(goal: goal, target: 400, levels: 3, growthPercent: 25);
        var rounds = new RoundManager(() => config);
        Assert.AreEqual(400, rounds.BeginRound(true).TargetScore);
        Assert.AreEqual(500, rounds.BeginRound(true).TargetScore);
        Assert.AreEqual(500, rounds.BeginRound(false).TargetScore);
        Assert.AreEqual(625, rounds.BeginRound(true).TargetScore);
        var clone = (FreePlayConfig)config.CloneNew();
        Assert.AreEqual(3, clone.GetTotalRounds);
        Assert.AreEqual(25, clone.GrowthPercent);
        Assert.AreEqual(625, clone.GetTargetScore(3));
    }

    [TestCase(400, 10, 2, 440)]
    [TestCase(100, 10, 3, 121)]
    [TestCase(3, 25, 2, 4)]
    [TestCase(3, 25, 3, 5)]
    [TestCase(400, 0, 100, 400)]
    [TestCase(400, 1000, 1, 400)]
    [TestCase(1000000, 1000, 100, int.MaxValue)]
    public void FreePlay_TargetGrowthRoundsUpAndCapsOverflow(int target, int growth, int level, int expected)
    {
        Assert.AreEqual(expected, FreePlayConfig.TargetForLevel(target, growth, level));
    }

    // --- Helpers ---

    private void ResetGameStateSingleton()
    {
        foreach (var existing in Object.FindObjectsByType<GameState>())
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var prop = typeof(GameState).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var setter = prop?.GetSetMethod(true);
        setter?.Invoke(null, new object[] { null });
    }

    private void CreateGameState()
    {
        var go = new GameObject("GameState");
        _createdObjects.Add(go);
        var gs = go.AddComponent<GameState>();

        // Unity may not invoke Awake in edit mode test instantiation; set singleton explicitly.
        var prop = typeof(GameState).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var setter = prop?.GetSetMethod(true);
        setter?.Invoke(null, new object[] { gs });
    }

    private sealed class BudgetMove : MatchInfo, PlayerMoveInfo
    {
        public int MoveId { get; set; }
        public bool wasUserInitiatedMatch() => true;
        public bool AllPlayerGroupsExactlyFour => false;
        public bool BothSwapTilesMatched => false;
        public int BoardWidth => 7;
        public int BoardHeight => 7;
    }

    [TestCase(FreePlayMode.Timed, GoalType.Score)]
    [TestCase(FreePlayMode.Timed, GoalType.BlocksCleared)]
    [TestCase(FreePlayMode.LimitedMoves, GoalType.Score)]
    [TestCase(FreePlayMode.LimitedMoves, GoalType.BlocksCleared)]
    public void PatternRewardsUpdateActiveBudgetAndCanSaveTheFinalMove(FreePlayMode mode, GoalType goal)
    {
        GameState.Instance.CurrentLevel = new FreePlayConfig(mode: mode, goal: goal);
        _controller.GameUIManager = null;
        ((GameAudio)GetPrivateField(_controller, "audioFeedback")).SetEnabled(false);
        var timer = (TimerController)GetPrivateField(_controller, "_timer");
        timer.Reset(60); timer.Start();
        SetPrivateField(_controller, "targetScore", 100000);
        SetPrivateField(_controller, "movesRemaining", 1);
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Playing);
        var context = new BudgetMove();
        var power = new BudgetPower(BudgetStyle.Edge) { MatchInfo = context };
        _controller.PowerManager.ActivePowers().Add(power);
        var tileObject = new GameObject("BudgetTile");
        _createdObjects.Add(tileObject);
        var tile = tileObject.AddComponent<Tile>();
        var item = ScriptableObject.CreateInstance<Item>();
        _createdAssets.Add(item);
        tile.item = item;
        var tiles = new List<Tile> { tile };
        // Two earlier qualifying swaps prime the reward; the third uses the last move.
        power.EarnBudgetRewards(tiles); context.MoveId++;
        power.EarnBudgetRewards(tiles); context.MoveId++;
        _controller.BeginResolution();
        _controller.HandleTileMatch(tiles);
        Assert.That(_controller.CurrentTime, Is.EqualTo(mode == FreePlayMode.Timed ? 63 : 60));
        Assert.That(_controller.MovesRemaining, Is.EqualTo(1));
        Assert.That(power.RewardsEarned, Is.EqualTo(1));
        _controller.EndResolution();
        Assert.That(_controller.Phase, Is.EqualTo(GamePhase.Playing));
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.GameOver);
        context.MoveId++;
        _controller.HandleTileMatch(tiles);
        Assert.That(power.Progress, Is.Zero, "Finished rounds cannot advance reward progress.");
    }

    private GameController CreateController()
    {
        var go = new GameObject("GameController");
        _createdObjects.Add(go);
        var controller = go.AddComponent<GameController>();
        typeof(GameController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);
        controller.GameUIManager = CreateUiManager();
        controller.limitedItemPool = new List<Item>();
        return controller;
    }

    private GameUIManager CreateUiManager()
    {
        var uiGo = new GameObject("GameUIManager");
        _createdObjects.Add(uiGo);
        var ui = uiGo.AddComponent<GameUIManager>();

        // Provide TextMeshProUGUI for timer updates so methods don't throw.
        var timerGo = new GameObject("TimerText");
        _createdObjects.Add(timerGo);
        var timer = timerGo.AddComponent<TextMeshProUGUI>();
        SetPrivateField(ui, "timerText", timer);

        return ui;
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(target, value);
    }

    private object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field.GetValue(target);
    }

    // --- Test doubles ---

    private class FakeLevelNoBossPowers : LevelConfig
    {
        public int GetTotalTime => 60;
        public int GetHeight(int round) => 7;
        public int GetWidth(int round) => 7;
        public int GetTargetScore(int round) => 100;
        public int GetTotalRounds => 1;
        public int GetLevelNumber => 99;
        public string GetLevelDescription() => "No boss powers.";
        public bool BossPowersPersist => false;
        public BossPowerMap GetBossPowersMap() => new BossPowerMap();
        public List<PowerUp> GetPowerUpsForRound(int round) => new List<PowerUp>();
        public List<MatchRule> GetMatchRules(int round) => new() { new MatchRule(MatchType.Snake, 3) };
        public LevelConfig CloneNew() => new FakeLevelNoBossPowers();
    }

    private class FakeLevelWithBossPowers : LevelConfig
    {
        private readonly BossPowerMap _map;

        public FakeLevelWithBossPowers()
        {
            _map = new BossPowerMap();
            _map.AddPower(1, new List<BossPowers> { new FirstItemsGivesNoPoints() });
            _map.AddPower(2, new List<BossPowers> { new FirstItemsGivesNoPoints(), new HalfPointsForFirstHalfOfLevel() });
        }

        public int GetTotalTime => 60;
        public int GetHeight(int round) => 7;
        public int GetWidth(int round) => 7;
        public int GetTargetScore(int round) => 100;
        public int GetTotalRounds => 2;
        public int GetLevelNumber => 98;
        public string GetLevelDescription() => "Boss powers present.";
        public bool BossPowersPersist => false;
        public BossPowerMap GetBossPowersMap() => _map;
        public List<PowerUp> GetPowerUpsForRound(int round) => new List<PowerUp>();
        public List<MatchRule> GetMatchRules(int round) => new() { new MatchRule(MatchType.Snake, 3) };
        public LevelConfig CloneNew() => new FakeLevelWithBossPowers();
    }
}
