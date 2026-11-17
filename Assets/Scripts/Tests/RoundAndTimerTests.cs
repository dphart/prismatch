using NUnit.Framework;

[TestFixture]
public class NewCampaignMechanicTests
{
    [Test]
    public void BorderCornersLightTwoSidesCompletionPaysOnceAndInteriorDoesNotAdvance()
    {
        var state=new CampaignMechanicState();
        Assert.That(state.BorderPercent(new[]{new BoardModel.Cell(3,3)},7,7),Is.EqualTo(100));
        Assert.That(state.LitEdges,Is.Zero);
        Assert.That(state.BorderPercent(new[]{new BoardModel.Cell(0,0)},7,7),Is.EqualTo(100));
        Assert.That(state.LitEdges,Is.EqualTo(9));
        Assert.That(state.BorderPercent(new[]{new BoardModel.Cell(0,0)},7,7),Is.EqualTo(100));
        Assert.That(state.BorderPercent(new[]{new BoardModel.Cell(6,6)},7,7),Is.EqualTo(400));
        Assert.That(state.LitEdges,Is.Zero);
        Assert.That(state.BorderPercent(new[]{new BoardModel.Cell(6,6)},7,7),Is.EqualTo(100));
        Assert.That(new CampaignMechanicState().LitEdges,Is.Zero);
    }

    [Test]
    public void TreasureRewardAppliesToNextPlayAndItsCascadesAndSurvivesFailure()
    {
        var state=new CampaignMechanicState();
        state.BeginPlay();
        for(int n=0;n<3;n++) state.CollectTreasure();
        Assert.That(state.PlayPercent(LevelBonus.TreasureTrail),Is.EqualTo(100));
        state.FinishPlay(true);
        Assert.That(state.TreasureReady,Is.True);
        state.BeginPlay(); state.FinishPlay(false);
        Assert.That(state.TreasureReady,Is.True);
        state.BeginPlay();
        Assert.That(state.PlayPercent(LevelBonus.TreasureTrail),Is.EqualTo(500));
        Assert.That(state.PlayPercent(LevelBonus.TreasureTrail),Is.EqualTo(500),"All cascade groups share the reward.");
        state.FinishPlay(true);
        Assert.That(state.TreasureReady,Is.False);
        Assert.That(state.TreasurePlay,Is.False);
        state.BeginPlay();
        Assert.That(state.PlayPercent(LevelBonus.TreasureTrail),Is.EqualTo(100));
    }

    [Test]
    public void TreasureCanRearmDuringBoostedPlayWithoutMultiplyingThatPlayAgain()
    {
        var state=new CampaignMechanicState();
        state.BeginPlay(); for(int n=0;n<3;n++)state.CollectTreasure(); state.FinishPlay(true);
        state.BeginPlay(); for(int n=0;n<3;n++)state.CollectTreasure();
        Assert.That(state.PlayPercent(LevelBonus.TreasureTrail),Is.EqualTo(500));
        state.FinishPlay(true);
        Assert.That(state.TreasureReady,Is.True);
        Assert.That(state.Treasures,Is.EqualTo(6));
    }

    [Test]
    public void TreasureCrabAccountsForWholePlayIncludingLateCascadeCollectionAndRounding()
    {
        var state=new CampaignMechanicState(); state.BeginPlay();
        Assert.That(state.CreditTreasureBoss(101),Is.EqualTo(50));
        Assert.That(state.CreditTreasureBoss(100),Is.EqualTo(50));
        state.CollectTreasure();
        Assert.That(state.CreditTreasureBoss(40),Is.EqualTo(141),"Release the 101 held points with this cascade's 40.");
        Assert.That(state.CreditTreasureBoss(20),Is.EqualTo(20));
        state.FinishPlay(true); state.BeginPlay();
        Assert.That(state.CreditTreasureBoss(1),Is.Zero);
        Assert.That(state.CreditTreasureBoss(1),Is.EqualTo(1),"Round the whole play down, not each group.");
        state.CollectTreasure();state.CollectTreasure();state.FinishPlay(true);state.BeginPlay();
        Assert.That(state.TreasurePlay,Is.True);
        Assert.That(state.CreditTreasureBoss(101),Is.EqualTo(101),"Armed plays are exempt without collecting.");
    }

    [Test]
    public void TreasureBossSaturatesLargePlayTotalsWithoutOverflow()
    {
        var state=new CampaignMechanicState();state.BeginPlay();
        Assert.That(state.CreditTreasureBoss(int.MaxValue),Is.EqualTo(int.MaxValue/2));
        Assert.That(state.CreditTreasureBoss(int.MaxValue),Is.Zero);
        state.CollectTreasure();
        Assert.That(state.CreditTreasureBoss(0),Is.EqualTo(int.MaxValue-int.MaxValue/2));
    }

    [Test]
    public void TideTurnsOnlyAfterFourSuccessfulPlaysAndBoostsNextWholePlay()
    {
        var state=new CampaignMechanicState();
        foreach(var direction in new[]{ReplacementStrategy.South,ReplacementStrategy.West,ReplacementStrategy.North,ReplacementStrategy.East}) {
            Assert.That(state.Gravity,Is.EqualTo(direction));
            state.BeginPlay(); state.FinishPlay(false);
            Assert.That(state.TideMoves,Is.Zero);
            for(int move=0;move<4;move++) {
                state.BeginPlay();
                Assert.That(state.Gravity,Is.EqualTo(direction),"Gravity cannot turn during cascades.");
                int expected=direction!=ReplacementStrategy.South&&move==0?300:100;
                Assert.That(state.PlayPercent(LevelBonus.ChangingTides),Is.EqualTo(expected));
                state.FinishPlay(true);
            }
            Assert.That(state.TideMoves,Is.Zero);
            Assert.That(state.TideReady,Is.True);
            Assert.That(state.TidePlay,Is.False);
        }
        Assert.That(state.Gravity,Is.EqualTo(ReplacementStrategy.South));
    }

    [Test]
    public void TreasureFollowsSwapsAndEveryGravityDirectionAndClearsOnlyOnce()
    {
        foreach(var gravity in new[]{ReplacementStrategy.South,ReplacementStrategy.North,ReplacementStrategy.West,ReplacementStrategy.East,ReplacementStrategy.InPlace}) {
            var board=new BoardModel(7,7);
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=ItemType.Duck;
            board.EnsureTreasure(new System.Random(6));
            var first=board.Treasure.Value;
            var other=new BoardModel.Cell(first.X==0?1:0,first.Y);
            board.Swap(first,other);
            Assert.That(board.Treasure.Value,Is.EqualTo(other));
            board.Swap(first,other);
            Assert.That(board.Treasure.Value,Is.EqualTo(first));
            var holes=new System.Collections.Generic.List<BoardModel.Cell>();
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)if(!first.Equals(new BoardModel.Cell(x,y))&&(x==0||x==6||y==0||y==6))holes.Add(new BoardModel.Cell(x,y));
            var moves=board.Refill(holes,gravity,()=>ItemType.Bear);
            var expected=first;
            foreach(var move in moves)if(!move.IsNew&&move.From.Equals(first))expected=move.To;
            Assert.That(board.Treasure.Value,Is.EqualTo(expected));
            Assert.That(board[expected.X,expected.Y],Is.EqualTo(ItemType.Duck));
            Assert.That(board.CollectTreasure(new[]{expected}),Is.True);
            Assert.That(board.CollectTreasure(new[]{expected}),Is.False);
            Assert.That(board.Campaign.Treasures,Is.EqualTo(1));
            board.Refill(new[]{expected},gravity,()=>ItemType.Bear);
            board.EnsureTreasure(new System.Random(7));
            Assert.That(board.Treasure.HasValue,Is.True);
            Assert.That(board[board.Treasure.Value.X,board.Treasure.Value.Y].HasValue,Is.True);
        }
    }

    [Test]
    public void RecoveryGeneratesFreshTreasureAndPreservesRoundProgress()
    {
        var board=new BoardModel(7,7);
        var types=new[]{ItemType.Duck,ItemType.Bear,ItemType.Hippo,ItemType.Sloth,ItemType.Walrus};
        board.Generate(types,new System.Random(18));board.EnsureTreasure(new System.Random(1));
        board.Campaign.BeginPlay();board.CollectTreasure(new[]{board.Treasure.Value});board.Campaign.FinishPlay(true);
        board.Generate(types,new System.Random(19));
        Assert.That(board.Treasure.HasValue,Is.False);
        board.EnsureTreasure(new System.Random(2));
        Assert.That(board.Campaign.Treasures,Is.EqualTo(1));
        Assert.That(board.Treasure.HasValue,Is.True);
        Assert.That(new BoardModel(7,7).Campaign.Treasures,Is.Zero);
    }

    [Test]
    public void NewBossesPenalizeMatchesMissingTheirConditions()
    {
        var interior=new[]{new BoardModel.Cell(2,2)};
        var corner=new[]{new BoardModel.Cell(0,0)};
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.FenceFox,interior,7,7,1,true,false),Is.EqualTo(75));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.FenceFox,corner,7,7,1,false,false),Is.EqualTo(100));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.TideTurtle,interior,7,7,1,true,false),Is.EqualTo(75));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.TideTurtle,interior,7,7,1,false,false),Is.EqualTo(75));
        Assert.That(ChallengeBoss.ScaleReward(int.MaxValue,500),Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void NewLevelsLoadCloneAndOfferCompatibleDistinctChoicesAcrossSeededBuilds()
    {
        SharedGameContent.Load(System.IO.File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        SharedGameContent.ValidateCoverage();LevelLibrary.ReloadSharedContent();
        Assert.That(LevelLibrary.TotalLevels,Is.GreaterThanOrEqualTo(23));
        for(int number=9;number<=11;number++) {
            var level=(CampaignLevel)LevelLibrary.GetLevel(number);
            var clone=(CampaignLevel)level.CloneNew();
            Assert.That(clone.Bonus,Is.EqualTo(level.Bonus));
            Assert.That(clone.FinalBossStyle,Is.EqualTo(level.FinalBossStyle));
            Assert.That(clone.GetTotalRounds,Is.EqualTo(6));
            Assert.That(clone.BasePointsPerTile,Is.EqualTo(4));
            Assert.That(clone.Mode,Is.EqualTo(number==10?FreePlayMode.LimitedMoves:FreePlayMode.Timed));
            for(int seed=0;seed<30;seed++) {
                var owned=new System.Collections.Generic.List<PowerUp>();
                var names=new System.Collections.Generic.HashSet<string>();
                var animals=new System.Collections.Generic.List<ItemType>{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Hippo,ItemType.Giraffe,ItemType.Sloth,ItemType.Walrus};
                for(int round=1;round<=6;round++) {
                    var pool=round==1?clone.GetPowerUpsForRound(1):PowerOfferBuilder.Catalog();
                    var choices=round==1 ? PowerUpSelector.BuildChoices(pool,names,animals,3,new System.Random(seed*10+round),GoalType.Score,clone.Mode,clone.GetMatchRules(round),owned,round:round) : new System.Collections.Generic.List<PowerUp>(PowerOfferBuilder.Build(clone.PowerOffers.ForRound(round),pool,names,animals,new System.Random(seed*10+round),GoalType.Score,clone.Mode,clone.GetMatchRules(round),owned,round).Choices);
                    Assert.That(choices.Count,Is.EqualTo(3));
                    var unique=new System.Collections.Generic.HashSet<string>();
                    foreach(var p in choices)Assert.That(unique.Add(p.Name),Is.True);
                    var selected=choices[seed%3];owned.Add(selected);names.Add(selected.Name);
                    if(selected is RosterTradeoffPower roster) { if(roster.AddsAnimal)animals.Add(roster.Animal);else animals.Remove(roster.Animal); }
                }
            }
        }
    }
}

[TestFixture]
public class RoundAndTimerTests
{
    [Test]
    public void TimeRewardsUpdateHudWithoutChangingDurationOrRevivingExpiredRounds()
    {
        var timer = new TimerController();
        float displayed = 0;
        timer.OnTick += value => displayed = value;
        timer.Reset(60); timer.Start(); timer.Tick(10); timer.AddTime(3);
        Assert.That(timer.Remaining, Is.EqualTo(53));
        Assert.That(displayed, Is.EqualTo(53));
        Assert.That(timer.Duration, Is.EqualTo(60));
        timer.AddTime(-1); timer.AddTime(float.NaN); timer.AddTime(float.PositiveInfinity);
        Assert.That(timer.Remaining, Is.EqualTo(53));
        timer.Tick(53); timer.AddTime(3);
        Assert.That(timer.Remaining, Is.Zero);
        Assert.That(timer.IsRunning, Is.False);
    }
    [Test]
    public void Timer_ClampsPenaltyAndExpiresOnlyOnce()
    {
        var timer = new TimerController(); int expired = 0;
        timer.OnExpired += () => expired++;
        timer.Reset(2); timer.Start(); timer.ApplyPenalty(10); timer.Tick(1);
        Assert.AreEqual(0, timer.Remaining); Assert.AreEqual(1, expired);
    }

    [Test]
    public void ScoreSaturation_DoesNotOverflow()
    {
        Assert.AreEqual(int.MaxValue, ScoreManager.SaturatingAdd(int.MaxValue, 100));
    }

    [Test]
    public void LevelTuning_GrowthIsMonotonicAndBounded()
    {
        var tuning = UnityEngine.ScriptableObject.CreateInstance<LevelTuning>();
        tuning.baseTarget = int.MaxValue; tuning.targetGrowth = 2;
        Assert.AreEqual(int.MaxValue, tuning.Target(10));
        UnityEngine.Object.DestroyImmediate(tuning);
    }

    [Test]
    public void LevelTwo_ExposesItsConfiguredBossSchedule()
    {
        Assert.That(new Level2().GetBossPowersMap().GetBossPowersForRound(2), Is.Not.Empty);
    }
}

[TestFixture]
public class CampaignCatalogTests
{
    [Test]
    public void FourthChallengeOffersBlockFoundationsAndCreditStaysSeparateFromPhysicalClears()
    {
        var level = (CampaignLevel)LevelLibrary.GetLevel(5);
        Assert.That(level.Goal, Is.EqualTo(GoalType.BlocksCleared));
        Assert.That(level.BlockObjectives, Is.Null);
        Assert.That(level.Settings.GrowthPercent, Is.EqualTo(25));
        var items = new System.Collections.Generic.List<ItemType> { ItemType.Bear, ItemType.Duck, ItemType.Chicken, ItemType.Sloth, ItemType.Walrus, ItemType.Giraffe, ItemType.Hippo };
        var result = PowerOfferBuilder.Build(level.PowerOffers.ForRound(1), PowerOfferBuilder.Catalog(), new System.Collections.Generic.HashSet<string>(),
            items,new System.Random(3),level.Goal,level.Mode,level.GetMatchRules(1),new PowerUp[0]);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Choices.Count, Is.EqualTo(3));
        foreach (var choice in result.Choices)
        {
            Assert.That(choice, Is.TypeOf<BlockMatchFoundationPower>());
            var power = (BlockMatchFoundationPower)choice;
            int expected = power.Minimum == 3 ? 1 : power.Minimum == 4 ? 2 : 4;
            Assert.That(power.CreditPerTile, Is.EqualTo(expected));
            Assert.That(power.FlatBasePerTile, Is.Zero);
            Assert.That(power.AdditiveBaseMod(null), Is.Zero);
            Assert.That(BlockMatchFoundationPower.ApplyCredit(5,new PowerUp[] {power}), Is.EqualTo(5 * expected));
            Assert.That(BlockMatchFoundationPower.ApplyCredit(0,new PowerUp[] {power}), Is.Zero);
            Assert.That(BlockMatchFoundationPower.ApplyCredit(int.MaxValue,new PowerUp[] {power}), Is.EqualTo(int.MaxValue));
            Assert.That(BlockMatchFoundationPower.ApplyCredit(5,new PowerUp[0]), Is.EqualTo(5));
            Assert.That(power.CanOfferWith(new PowerUp[] {power}), Is.False);
            Assert.That(power.Clone(), Is.TypeOf<BlockMatchFoundationPower>());
            var rules = level.GetMatchRules(2);
            power.ModifyMatchRules(rules);
            Assert.That(rules[0].minimumLength, Is.EqualTo(power.Minimum));
            var board = new BoardModel(7,7,rules);
            board.Generate(items,new System.Random(power.Minimum));
            Assert.That(board.HasLegalMove(), Is.True);
            var later = PowerOfferBuilder.Build(level.PowerOffers.ForRound(2),PowerOfferBuilder.Catalog(),new System.Collections.Generic.HashSet<string> {power.Name},
                items,new System.Random(5),level.Goal,level.Mode,rules,new PowerUp[] {power});
            Assert.That(later.IsComplete, Is.True);
            foreach (var next in later.Choices) Assert.That(next, Is.Not.InstanceOf<MatchFoundationPower>());
        }
    }

    [Test]
    public void ThirdChallengeOffersThreeOpeningFoundationsWithRunLongMinimums()
    {
        var level = (CampaignLevel)LevelLibrary.GetLevel(4);
        Assert.That(level.Goal, Is.EqualTo(GoalType.Score));
        Assert.That(level.BasePointsPerTile, Is.EqualTo(4));
        var items = new System.Collections.Generic.List<ItemType> { ItemType.Bear, ItemType.Duck, ItemType.Chicken, ItemType.Sloth, ItemType.Walrus, ItemType.Giraffe, ItemType.Hippo };
        var result = PowerOfferBuilder.Build(level.PowerOffers.ForRound(1), PowerOfferBuilder.Catalog(), new System.Collections.Generic.HashSet<string>(),
            items, new System.Random(8), level.Goal, level.Mode, level.GetMatchRules(1), new PowerUp[0]);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Choices.Count, Is.EqualTo(3));
        foreach (var choice in result.Choices)
        {
            Assert.That(choice, Is.TypeOf<MatchFoundationPower>());
            var power = (MatchFoundationPower)choice;
            Assert.That(power.FlatBasePerTile, Is.Zero);
            Assert.That(power.FlatMultiplierBonus, Is.Zero);
            Assert.That(power.CanOfferWith(new PowerUp[] { power }), Is.False);
            Assert.That(((MatchFoundationPower)power.Clone()).Minimum, Is.EqualTo(power.Minimum));
            for (int round = 1; round <= level.GetTotalRounds; round++)
            {
                var rules = level.GetMatchRules(round);
                power.ModifyMatchRules(rules);
                Assert.That(rules[0].minimumLength, Is.EqualTo(power.Minimum));
                Assert.That(rules[0].type, Is.EqualTo(MatchType.Snake));
            }
            var effective = level.GetMatchRules(1);
            power.ModifyMatchRules(effective);
            var board = new BoardModel(6,7,effective);
            board.Generate(items,new System.Random(power.Minimum));
            Assert.That(board.Matches().Count, Is.Zero);
            Assert.That(board.HasLegalMove(), Is.True);
            var later = PowerOfferBuilder.Build(level.PowerOffers.ForRound(2), PowerOfferBuilder.Catalog(), new System.Collections.Generic.HashSet<string> { power.Name },
                items, new System.Random(2), level.Goal, level.Mode, effective, new PowerUp[] { power });
            Assert.That(later.IsComplete, Is.True);
            foreach (var card in later.Choices) Assert.That(card, Is.Not.TypeOf<MatchFoundationPower>());
        }
    }

    [Test]
    public void LightningRequiresFiveMatchingTilesInAStraightLine()
    {
        foreach (bool horizontal in new[] { true, false })
        foreach (int length in new[] { 4, 5, 6 })
        {
            var board = new BoardModel(7, 7);
            var group = new System.Collections.Generic.List<BoardModel.Cell>();
            for (int n = 0; n < length; n++)
            {
                var cell = new BoardModel.Cell(horizontal ? n : 2, horizontal ? 2 : n);
                board[cell.X, cell.Y] = ItemType.Duck; group.Add(cell);
            }
            var lines = board.LightningLines(new[] { group });
            Assert.That(lines.Count, Is.EqualTo(length >= 5 ? 1 : 0));
            if (length >= 5) Assert.That(lines[0].Direction, Is.EqualTo(horizontal ? MatchType.Horizontal : MatchType.Vertical));
            board[group[2].X, group[2].Y] = ItemType.Bear;
            Assert.That(board.LightningLines(new[] { group }).Count, Is.Zero, "Adjacent different animals are not a line match.");
        }
        var bent = new BoardModel(7,7);
        var cells = new System.Collections.Generic.List<BoardModel.Cell> {
            new(0,0), new(1,0), new(2,0), new(2,1), new(3,1), new(4,1) };
        foreach (var cell in cells) bent[cell.X, cell.Y] = ItemType.Duck;
        Assert.That(bent.LightningLines(new[] { cells }).Count, Is.Zero);
    }

    [Test]
    public void LightningClearsIntersectingLinesAndChargesRemainingBoard()
    {
        var board = new BoardModel(7,7);
        for (int x = 0; x < 7; x++) for (int y = 0; y < 7; y++) board[x,y] = ItemType.Bear;
        var group = new System.Collections.Generic.HashSet<BoardModel.Cell>();
        for (int n = 0; n < 5; n++)
        {
            board[n,0] = board[0,n] = ItemType.Duck;
            group.Add(new(n,0)); group.Add(new(0,n));
        }
        var groups = new[] { new System.Collections.Generic.List<BoardModel.Cell>(group) };
        var lines = board.LightningLines(groups);
        Assert.That(lines.Count, Is.EqualTo(2));
        board.ChargeLightning(lines);
        Assert.That(board.IsCharged(6,0), Is.False);
        Assert.That(board.IsCharged(0,6), Is.False);
        Assert.That(board.IsCharged(1,1), Is.True);
        Assert.That(board[6,0], Is.EqualTo(ItemType.Bear), "Planning lightning preserves tiles until animated clearing.");
        var cleared = board.ExpandLightning(group, lines);
        Assert.That(cleared.Count, Is.EqualTo(13));
        Assert.That(cleared.Contains(new BoardModel.Cell(6,0)), Is.True);
        Assert.That(cleared.Contains(new BoardModel.Cell(0,6)), Is.True);
        Assert.That(cleared.Contains(new BoardModel.Cell(1,1)), Is.False);
        Assert.That(board[6,0], Is.EqualTo(ItemType.Bear), "Planning a strike must not mutate the board before animation/refill.");
        board[2,0] = ItemType.Walrus;
        Assert.That(board.LightningLines(groups).Count, Is.EqualTo(1));
        board.SetAnimalPairs(new[] { new AnimalPairPower(ItemType.Duck, ItemType.Walrus) });
        Assert.That(board.LightningLines(groups).Count, Is.EqualTo(2));
        Assert.That(board.LightningLines(new System.Collections.Generic.List<BoardModel.Cell>[0]).Count, Is.Zero);
        board.Refill(cleared,ReplacementStrategy.InPlace,()=>ItemType.Chicken);
        for (int x = 0; x < 7; x++) for (int y = 0; y < 7; y++)
            Assert.That(board.IsCharged(x,y), Is.EqualTo(x != 0 && y != 0),
                "Struck lines refill normally while all surviving tiles retain the board-wide charge.");
    }

    [Test]
    public void LightningChargeStaysOnSpacesThroughSwapsAndRefillUntilCleared()
    {
        var palette = new[] { ItemType.Duck,ItemType.Bear,ItemType.Chicken };
        foreach (ReplacementStrategy strategy in System.Enum.GetValues(typeof(ReplacementStrategy)))
        {
            var board = new BoardModel(4,4);
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) board[x,y] = palette[(x+y)%3];
            board.ChargeLightning(new[] { (MatchType.Horizontal,1) });
            board.ChargeLightning(new[] { (MatchType.Horizontal,1) }); // Recharging is idempotent.
            board.Swap(new BoardModel.Cell(0,1),new BoardModel.Cell(2,2));
            Assert.That(board.IsCharged(0,1), Is.False);
            Assert.That(board.IsCharged(2,2), Is.True);
            var before = new bool[4,4];
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) before[x,y] = board.IsCharged(x,y);
            board.HasLegalMove();
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) Assert.That(board.IsCharged(x,y), Is.EqualTo(before[x,y]));
            var clears = new[] { new BoardModel.Cell(2,2),new BoardModel.Cell(1,3),new BoardModel.Cell(3,0) };
            board.Refill(clears,strategy,()=>ItemType.Duck);
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++)
                Assert.That(board.IsCharged(x,y), Is.EqualTo(before[x,y] && !System.Array.Exists(clears,c => c.X==x && c.Y==y)), strategy.ToString());
            board.Generate(palette,new System.Random(9));
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) Assert.That(board.IsCharged(x,y), Is.False);
        }
    }

    [Test]
    public void ChargedTilesDoubleTheirShareWithoutStackingAndSaturateSafely()
    {
        Assert.That(ScoreManager.ApplyChargedTiles(40,5,0), Is.EqualTo(40));
        Assert.That(ScoreManager.ApplyChargedTiles(40,5,5), Is.EqualTo(80));
        Assert.That(ScoreManager.ApplyChargedTiles(40,5,2), Is.EqualTo(56));
        Assert.That(ScoreManager.ApplyChargedTiles(40,5,100), Is.EqualTo(80));
        Assert.That(ScoreManager.ApplyChargedTiles(0,5,5), Is.Zero);
        Assert.That(ScoreManager.ApplyChargedTiles(int.MaxValue,5,5), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ClearingAChargedBottomSpaceRemovesOnlyItsBonus()
    {
        var board = new BoardModel(5,5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) board[x,y] = ItemType.Duck;
        board.ChargeLightning(new[] { (MatchType.Horizontal,0) });
        board.Refill(new[] { new BoardModel.Cell(0,4) },ReplacementStrategy.South,()=>ItemType.Duck);
        Assert.That(board.IsCharged(0,4), Is.False, "The cleared space loses its bonus when another animal falls into it.");
        Assert.That(board.IsCharged(0,3), Is.True);
        Assert.That(board.IsCharged(0,2), Is.True);
        Assert.That(board.IsCharged(0,1), Is.True, "An uncleared space keeps its bonus when another animal falls into it.");
        Assert.That(board.IsCharged(0,0), Is.False, "A new tile has no charge, even with the same animal sprite.");
        Assert.That(board.IsCharged(1,4), Is.True, "An untouched neighboring tile must keep its charge.");
    }

    [Test]
    public void SecondChallengeKeepsStandardOpeningsAndClonesItsLightningRule()
    {
        var level = (CampaignLevel)LevelLibrary.GetLevel(3);
        Assert.That(level.LightningLines, Is.True);
        Assert.That(((CampaignLevel)level.CloneNew()).LightningLines, Is.True);
        Assert.That(level.BasePointsPerTile, Is.Zero);
        Assert.That(level.PowerOffers.ForRound(1), Is.Null);
        Assert.That(level.GetPowerUpsForRound(1).TrueForAll(power => power is TacticalStarter), Is.True);
        Assert.That(level.Settings.Matching, Is.EqualTo(MatchType.Snake));
        Assert.That(level.Settings.GrowthPercent, Is.EqualTo(70));
        foreach (CampaignLevel other in LevelLibrary.AllLevels)
            if (other.GetLevelNumber != 3) Assert.That(other.LightningLines, Is.False);
    }

    [Test]
    public void CampaignNeverRequiresDirectionalOnlyMatching()
    {
        foreach (var level in LevelLibrary.AllLevels)
        for (int round = 1; round <= level.GetTotalRounds; round++)
        {
            Assert.That(level.GetMatchRules(round).TrueForAll(rule => rule.type == MatchType.Snake), Is.True);
            if (level is CampaignLevel campaign && campaign.BlockObjectives != null)
                Assert.That(campaign.BlockObjectives.ForRound(round).Shape,
                    Is.Not.EqualTo(BlockShape.Horizontal).And.Not.EqualTo(BlockShape.Vertical));
        }
    }

    [Test]
    public void CampaignBoardsUsePreferredPortraitSizesThroughEveryRoundAndClone()
    {
        foreach (CampaignLevel level in LevelLibrary.AllLevels)
        {
            if (level.IsTutorial) continue;
            foreach (var copy in new LevelConfig[] { level, level.CloneNew() })
            for (int round = 1; round <= copy.GetTotalRounds; round++)
            {
                Assert.That(copy.GetWidth(round) == 6 || copy.GetWidth(round) == 7, Is.True);
                Assert.That(copy.GetHeight(round), Is.EqualTo(7));
            }
        }
    }

    [Test]
    public void ChainReactionUsesDownwardGravityAndOnlyCascadesEarnPoints()
    {
        var level = (CampaignLevel)LevelLibrary.GetLevel(6);
        Assert.That(level.Name, Is.EqualTo("Chain Reaction"));
        Assert.That(level.Replacement, Is.EqualTo(ReplacementStrategy.South));
        Assert.That(level.Goal, Is.EqualTo(GoalType.Score));
        Assert.That(((CampaignLevel)level.CloneNew()).Bonus, Is.EqualTo(LevelBonus.ChainReaction));
        var cells = new[] { new BoardModel.Cell(0,0),new BoardModel.Cell(1,0),new BoardModel.Cell(2,0),new BoardModel.Cell(3,0) };
        Assert.That(ChallengeBoss.ScaleReward(40,LevelBonuses.RewardPercent(level.Bonus,cells,true)), Is.Zero);
        Assert.That(ChallengeBoss.ScaleReward(40,LevelBonuses.RewardPercent(level.Bonus,cells,false)), Is.EqualTo(160));
        Assert.That(ChallengeBoss.ScaleReward(int.MaxValue,400), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void GardensPlantSquaresOnceAndStayOnSpacesThroughSwapsRefillsAndShuffles()
    {
        var board = new BoardModel(6,7);
        for (int x = 0; x < 6; x++) for (int y = 0; y < 7; y++) board[x,y] = ItemType.Duck;
        var square = new System.Collections.Generic.List<BoardModel.Cell> { new(1,1),new(2,1),new(1,2),new(2,2) };
        Assert.That(board.PlantSquares(new[] { square }), Is.EqualTo(4));
        Assert.That(board.PlantSquares(new[] { square }), Is.Zero);
        board.Swap(new(1,1),new(4,4));
        Assert.That(board.IsPlanted(1,1), Is.True);
        Assert.That(board.IsPlanted(4,4), Is.False);
        board.Refill(square,ReplacementStrategy.South,()=>ItemType.Bear);
        Assert.That(board.IsPlanted(2,2), Is.True);
        board.Generate(new[] { ItemType.Duck,ItemType.Bear,ItemType.Chicken },new System.Random(4));
        Assert.That(board.IsPlanted(2,2), Is.True);
        Assert.That(new BoardModel(6,7).IsPlanted(2,2), Is.False);
        var bent = new System.Collections.Generic.List<BoardModel.Cell> { new(0,0),new(1,0),new(2,0),new(2,1) };
        Assert.That(board.PlantSquares(new[] { bent }), Is.Zero);
    }

    [Test]
    public void SpotlightSwitchesOnlyAfterSuccessfulPlaysAndResetsWithNewRound()
    {
        var board = new BoardModel(7,7);
        Assert.That(board.SpotlightAnimal, Is.EqualTo(ItemType.Duck));
        Assert.That(board.NextSpotlightAnimal, Is.EqualTo(ItemType.Bear));
        board.FinishSpotlightPlay(false);
        Assert.That(board.SpotlightAnimal, Is.EqualTo(ItemType.Duck));
        board.FinishSpotlightPlay(true);
        Assert.That(board.SpotlightAnimal, Is.EqualTo(ItemType.Bear));
        board.FinishSpotlightPlay(true);
        Assert.That(board.SpotlightAnimal, Is.EqualTo(ItemType.Duck));
        Assert.That(new BoardModel(7,7).SecondSpotlight, Is.False);
    }

    [Test]
    public void NewLevelTileRewardsAndBossesUseConditionalFactorsAndPreserveClones()
    {
        Assert.That(ScoreManager.ApplyTileFactors(40,new[] { 2,2,1,1 }), Is.EqualTo(60));
        Assert.That(ScoreManager.ApplyTileFactors(40,new[] { 3,3,3,3 }), Is.EqualTo(120));
        Assert.That(ScoreManager.ApplyTileFactors(int.MaxValue,new[] { 3,3 }), Is.EqualTo(int.MaxValue));
        Assert.That(ScoreManager.ApplyTileFactors(0,new[] { 3,3 }), Is.Zero);
        var cells = new[] { new BoardModel.Cell(1,1) };
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.Overgrowth,cells,6,7,1,true,true,false), Is.EqualTo(75));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.Overgrowth,cells,6,7,1,true,true,true), Is.EqualTo(100));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.Blackout,cells,7,7,1,true,true,false,false), Is.EqualTo(75));
        Assert.That(ChallengeBoss.RewardPercent(BossStyle.Blackout,cells,7,7,1,true,true,false,true), Is.EqualTo(100));
        foreach (int number in new[] { 7,8 })
        {
            var level = (CampaignLevel)LevelLibrary.GetLevel(number);
            Assert.That(level.GetPowerUpsForRound(1).Count, Is.EqualTo(3));
            Assert.That(((CampaignLevel)level.CloneNew()).Bonus, Is.EqualTo(level.Bonus));
            Assert.That(level.Replacement, Is.EqualTo(ReplacementStrategy.South));
            Assert.That(level.BasePointsPerTile, Is.EqualTo(4));
        }
        var garden = (CampaignLevel)LevelLibrary.GetLevel(7);
        Assert.That(garden.GetPowerUpsForRound(1).Exists(power => power is BiomeOpeningPower), Is.True);
        var current = (CampaignLevel)LevelLibrary.GetLevel(8);
        Assert.That(current.GetPowerUpsForRound(1).Exists(power => power is SpotlightOpeningPower), Is.True);
        for (int round = 2; round <= current.GetTotalRounds; round++)
            foreach (var slot in current.PowerOffers.ForRound(round).Slots)
                Assert.That(slot.Query.Matches(new RosterTradeoffPower(ItemType.Duck,false).Tags), Is.False);
    }

    [Test]
    public void DirectionalPowersAreOptionalExclusiveAndPreserveMinimumsAndActiveBuilds()
    {
        var items = new System.Collections.Generic.List<ItemType> { ItemType.Duck, ItemType.Bear };
        foreach (DirectionalMatchPower power in new DirectionalMatchPower[] { new NoVerticalDoublePower(), new VerticalMatchPower() })
        {
            var rules = new System.Collections.Generic.List<MatchRule> { new(MatchType.Snake, 4) };
            Assert.That(PowerOfferBuilder.IsEligible(power, items, GoalType.Score, FreePlayMode.Timed, rules, new PowerUp[0]), Is.True);
            Assert.That(PowerOfferBuilder.IsEligible(power, items, GoalType.Score, FreePlayMode.Timed, rules,
                new PowerUp[] { new TacticalStarter(TacticalStyle.Square) }), Is.False);
            Assert.That(power.CanOfferWith(new PowerUp[] { new NoVerticalDoublePower() }), Is.False);
            Assert.That(power.CanOfferWith(new PowerUp[] { new VerticalMatchPower() }), Is.False);
            power.ModifyMatchRules(rules);
            Assert.That(rules.Count, Is.EqualTo(1));
            Assert.That(rules[0].type, Is.EqualTo(power.Direction));
            Assert.That(rules[0].minimumLength, Is.EqualTo(4));
            Assert.That(power.CurrentMatchMultiplier(null), Is.EqualTo(4));
            Assert.That(power.FlatMultiplierFactor, Is.EqualTo(4));
            Assert.That(power.Clone().Name, Is.EqualTo(power.Name));
            Assert.That(new TacticalStarter(TacticalStyle.Square).CanUseWithMatchRules(rules), Is.False);
            var board = new BoardModel(5, 5, rules);
            for (int i = 0; i < 4; i++)
                board[power.Direction == MatchType.Horizontal ? i : 0,
                    power.Direction == MatchType.Vertical ? i : 0] = ItemType.Duck;
            Assert.That(board.Matches().Count, Is.EqualTo(1));
            board = new BoardModel(5, 5, rules);
            for (int i = 0; i < 4; i++)
                board[power.Direction == MatchType.Vertical ? i : 0,
                    power.Direction == MatchType.Horizontal ? i : 0] = ItemType.Duck;
            Assert.That(board.Matches().Count, Is.Zero, "The opposite direction must really stop matching.");
        }
    }

    [Test]
    public void TileTotalsIncludeBonusClearsAndDiscardRetriedRounds()
    {
        var summary = new RunSummary();
        summary.RecordClearedTiles(4);
        summary.RecordClearedTiles(2);
        summary.FinishRound(1, 0, 6, 10, true, GoalType.BlocksCleared, 6, 6);
        summary.BeginRound();
        summary.RecordClearedTiles(5);
        summary.FinishRound(2, 0, 10, 10, false, GoalType.BlocksCleared, 3, 3);
        Assert.That(summary.TotalTilesCleared, Is.EqualTo(11));
        Assert.That(summary.rounds[1].tilesCleared, Is.EqualTo(5));
        summary.UndoLastFailedRound();
        Assert.That(summary.TotalTilesCleared, Is.EqualTo(6));
        summary.Reset();
        Assert.That(summary.TotalTilesCleared, Is.Zero);
    }

    [Test]
    public void EveryFinalBossHasReadableRulesAndFreshClones()
    {
        foreach (var level in LevelLibrary.AllLevels)
        {
            if (level.GetLevelNumber == 1) continue;
            var boss = level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds)[0];
            Assert.That(boss.Name, Is.Not.Empty);
            Assert.That(boss.PowerUpDescription, Is.Not.Empty);
            Assert.That(boss.Clone(), Is.Not.SameAs(boss));
            Assert.That(boss.Clone().Name, Is.EqualTo(boss.Name));
        }
    }

    [Test]
    public void GatekeeperPenalizesSmallGroupsButRewardsFive()
    {
        var boss = BossLibrary.ForLevel(1);
        var tiles = new System.Collections.Generic.List<Tile> { null, null, null, null };
        Assert.That(boss.ModifyAndReturnScore(tiles, 100), Is.EqualTo(75));
        Assert.That(boss.ModifyBlockCredit(tiles, 4), Is.EqualTo(3));
        tiles.Add(null);
        Assert.That(boss.ModifyAndReturnScore(tiles, 100), Is.EqualTo(100));
        Assert.That(boss.ModifyBlockCredit(tiles, 5), Is.EqualTo(5));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(3, 3)]
    [TestCase(4, 3)]
    [TestCase(5, 4)]
    [TestCase(100, 75)]
    [TestCase(int.MaxValue, 1610612736)]
    public void BossPenaltyRoundsCreditUpAndHandlesLargeScores(int value, int expected)
    {
        var boss = BossLibrary.ForLevel(1);
        var tiles = new System.Collections.Generic.List<Tile> { null, null, null };
        Assert.That(boss.ModifyAndReturnScore(tiles, value), Is.EqualTo(expected));
        Assert.That(boss.ModifyBlockCredit(tiles, value), Is.EqualTo(expected));
    }

    [Test]
    public void TutorialAndOpeningLevelsCloneAllSharedSettingsAndScaleTargets()
    {
        SharedGameContent.Load(System.IO.File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        Assert.That(LevelLibrary.TotalLevels, Is.GreaterThanOrEqualTo(23));
        for (int number = 1; number <= LevelLibrary.TotalLevels; number++)
        {
            var original = (CampaignLevel)LevelLibrary.GetLevel(number);
            var clone = (CampaignLevel)original.CloneNew();
            Assert.That(clone.GetLevelNumber, Is.EqualTo(number));
            Assert.That(clone.Name, Is.Not.Empty);
            Assert.That(clone.Settings, Is.Not.SameAs(original.Settings));
            Assert.That(clone.Goal, Is.EqualTo(original.Goal));
            Assert.That(clone.Mode, Is.EqualTo(original.Mode));
            Assert.That(clone.Moves, Is.EqualTo(original.Moves));
            Assert.That(clone.Replacement, Is.EqualTo(original.Replacement));
            Assert.That(clone.GetTotalRounds, Is.EqualTo(original.GetTotalRounds));
            if(number<=23)Assert.That(clone.GetTotalRounds, Is.EqualTo(number == 1 ? 1 : number == 2 ? 4 : number >= 9 ? 6 : 6 + (number - 2) % 4));
            for (int round = 1; round <= clone.GetTotalRounds; round++)
            {
                Assert.That(clone.GetTargetScore(round), Is.EqualTo(original.GetTargetScore(round)));
                Assert.That(clone.GetTargetScore(round), Is.GreaterThanOrEqualTo(clone.GetTargetScore(1)));
                var bosses = clone.GetBossPowersMap().GetBossPowersForRound(round);
                Assert.That(bosses.Count, Is.EqualTo(original.GetBossPowersMap().GetBossPowersForRound(round).Count));
            }
        }
    }
}

[TestFixture]
public class ShufflePowerTests
{
    [Test]
    public void PatternTicketsIgnoreCascadesAndCountOnlyOneQualifyingGroupPerMove()
    {
        var square = new[] { new BoardModel.Cell(0,0), new BoardModel.Cell(1,0), new BoardModel.Cell(0,1), new BoardModel.Cell(1,1) };
        var large = new[] { new BoardModel.Cell(0,0), new BoardModel.Cell(1,0), new BoardModel.Cell(2,0), new BoardModel.Cell(3,0), new BoardModel.Cell(4,0), new BoardModel.Cell(5,0) };
        foreach (var style in new[] { ShuffleStyle.SquareTicket, ShuffleStyle.LargeTicket }) {
            var power = new ShufflePower(style);
            var cells = style == ShuffleStyle.SquareTicket ? square : large;
            var move = new Move { MoveId = 1, Player = false };
            Assert.That(power.RecordMatch(move, cells), Is.False);
            Assert.That(power.Progress, Is.Zero);
            move.Player = true;
            Assert.That(power.RecordMatch(move, cells), Is.False);
            Assert.That(power.Progress, Is.EqualTo(1));
            Assert.That(power.RecordMatch(move, cells), Is.False);
            move.MoveId++;
            Assert.That(power.RecordMatch(move, new[] { new BoardModel.Cell(0,0) }), Is.False);
            Assert.That(power.RecordMatch(move, cells), Is.True);
            Assert.That(power.FreeUses, Is.EqualTo(1));
            var active = new System.Collections.Generic.List<PowerUp> { power };
            Assert.That(ShufflePower.Cost(active, false), Is.Zero);
            Assert.That(ShufflePower.SpendFreeUse(active), Is.True);
            Assert.That(power.FreeUses, Is.Zero);
            Assert.That(ShufflePower.Cost(active, false), Is.EqualTo(2));
            power.OnRoundStarted(); Assert.That(power.Progress, Is.Zero);
        }
        Assert.That(new ShufflePower(ShuffleStyle.SquareTicket).CanUseWithMatchRules(new[] { new MatchRule(MatchType.Horizontal,4) }), Is.False);
    }

    [Test]
    public void ShufflePayoffsWaitForSuccessDoNotStackAndResetWithTheRound()
    {
        var context = new Move { MoveId = 1, Player = false };
        var spark = new ShufflePower(ShuffleStyle.Afterglow) { MatchInfo = context };
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(1));
        spark.NotifyShuffle(); spark.NotifyShuffle();
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(1));
        Assert.That(spark.Armed, Is.True);
        context.MoveId = 3; context.Player = true;
        spark.ObserveMatch(context);
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(2));
        context.Player = false;
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(2), "Cascades share the first play's boost.");
        context.MoveId++; context.Player = true;
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(1));
        spark.NotifyShuffle(); spark.OnRoundStarted();
        Assert.That(spark.CurrentMatchMultiplier(null), Is.EqualTo(1));
        var slate = new ShufflePower(ShuffleStyle.Salvage);
        slate.NotifyShuffle(); slate.NotifyShuffle(); context.Player = false;
        Assert.That(slate.ClaimBlockCredits(context), Is.Zero);
        context.Player = true;
        Assert.That(slate.ClaimBlockCredits(context), Is.EqualTo(8));
        Assert.That(slate.ClaimBlockCredits(context), Is.Zero, "Multiple groups cannot duplicate the reward.");
        context.Player = false;
        Assert.That(slate.ClaimBlockCredits(context), Is.Zero);
        slate.NotifyShuffle(); slate.OnRoundStarted(); context.Player = true;
        Assert.That(slate.ClaimBlockCredits(context), Is.Zero);
        Assert.That(new ShufflePower(ShuffleStyle.Afterglow).Supports(GoalType.BlocksCleared,FreePlayMode.Timed), Is.True);
        Assert.That(slate.Supports(GoalType.Score,FreePlayMode.Timed), Is.True);
    }

    private sealed class Move : MatchInfo, PlayerMoveInfo
    {
        public bool Player = true;
        public int MoveId { get; set; }
        public bool wasUserInitiatedMatch() => Player;
        public bool AllPlayerGroupsExactlyFour => false;
        public bool BothSwapTilesMatched => false;
        public int BoardWidth => 7;
        public int BoardHeight => 7;
    }

    [Test]
    public void FreeUsesAccumulateWithoutLimitAndIgnoreCascadesAndDuplicateGroups()
    {
        var power = new ShufflePower(ShuffleStyle.Earned);
        var move = new Move();
        for (int i = 1; i <= 25; i++)
        {
            move.MoveId = i;
            move.Player = false;
            Assert.That(power.RecordMatch(move), Is.False);
            move.Player = true;
            Assert.That(power.RecordMatch(move), Is.EqualTo(i % 5 == 0));
            Assert.That(power.RecordMatch(move), Is.False);
        }
        Assert.That(power.FreeUses, Is.EqualTo(5));
        Assert.That(power.Progress, Is.Zero);
        move.MoveId++;
        power.RecordMatch(move);
        Assert.That(power.Progress, Is.EqualTo(1));
        power.OnRoundStarted();
        Assert.That(power.FreeUses, Is.Zero);
        Assert.That(power.Progress, Is.Zero);
        Assert.That(power.RecordMatch(move), Is.False);
        Assert.That(power.Progress, Is.EqualTo(1), "A new round resets move deduplication too.");
    }

    [Test]
    public void FreeUsesTakePriorityAndOnlyOneIsSpentBeforeDiscountedCostsResume()
    {
        var earned = new ShufflePower(ShuffleStyle.Earned);
        var opening = new ShufflePower(ShuffleStyle.Opening);
        var discount = new ShufflePower(ShuffleStyle.Discount);
        var powers = new System.Collections.Generic.List<PowerUp>();
        Assert.That(ShufflePower.Cost(powers, true), Is.EqualTo(5));
        Assert.That(ShufflePower.Cost(powers, false), Is.EqualTo(2));
        powers.Add(discount);
        Assert.That(ShufflePower.Cost(powers, true), Is.EqualTo(2.5f));
        Assert.That(ShufflePower.Cost(powers, false), Is.EqualTo(1));
        for (int i = 0; i < 5; i++) earned.RecordMatch(new Move { MoveId = i });
        powers.Add(earned); powers.Add(opening);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(ShufflePower.Cost(powers, true), Is.Zero);
            Assert.That(ShufflePower.Cost(powers, false), Is.Zero);
            Assert.That(opening.FreeUses, Is.EqualTo(3 - i), "Cost previews never spend uses.");
            Assert.That(ShufflePower.SpendFreeUse(powers), Is.True);
            Assert.That(earned.FreeUses, Is.EqualTo(1));
        }
        Assert.That(ShufflePower.SpendFreeUse(powers), Is.True);
        Assert.That(earned.FreeUses, Is.Zero);
        Assert.That(ShufflePower.SpendFreeUse(powers), Is.False);
        Assert.That(ShufflePower.Cost(powers, true), Is.EqualTo(2.5f));
        Assert.That(ShufflePower.Cost(powers, false), Is.EqualTo(1));
        opening.OnRoundStarted();
        Assert.That(opening.FreeUses, Is.EqualTo(3));
        Assert.That(((ShufflePower)earned.Clone()).FreeUses, Is.Zero);
        Assert.That(((ShufflePower)opening.Clone()).FreeUses, Is.EqualTo(3));
    }

    [Test]
    public void ShufflePowersAreOfferedForBothGoalsAndModesWithoutDuplicateCopies()
    {
        foreach (var goal in new[] { GoalType.Score, GoalType.BlocksCleared })
        foreach (var mode in new[] { FreePlayMode.Timed, FreePlayMode.LimitedMoves })
        {
            var source = goal == GoalType.Score ? PowerUpLibrary.GetAll() : PowerUpLibrary.BlockPowers();
            var active = new System.Collections.Generic.List<PowerUp>();
            var offers = PowerUpSelector.BuildChoices(source, new System.Collections.Generic.HashSet<string>(),
                new System.Collections.Generic.List<ItemType>(), 100, new System.Random(1), goal, mode,
                new System.Collections.Generic.List<MatchRule> { new(MatchType.Horizontal, 3) }, active);
            Assert.That(offers.FindAll(p => p is ShufflePower).Count, Is.EqualTo(5));
            active.Add(new ShufflePower(ShuffleStyle.Discount));
            foreach (var power in PowerUpLibrary.ShufflePowers())
                Assert.That(power.CanOfferWith(active), Is.EqualTo(((ShufflePower)power).Style != ShuffleStyle.Discount));
        }
    }
}

[TestFixture]
public class MatchDefaultTests
{
    [Test]
    public void AllMatchingTypesDefaultToFourWhileExplicitOverridesSurviveCloning()
    {
        Assert.That(new FreePlayConfig().MinimumMatchLength, Is.EqualTo(4));
        Assert.That(new FreePlayConfig(MatchType.Horizontal).MinimumMatchLength, Is.EqualTo(4));
        Assert.That(new FreePlayConfig(MatchType.Vertical).MinimumMatchLength, Is.EqualTo(4));
        var custom = new FreePlayConfig(minimumMatchLength: 2);
        Assert.That(((FreePlayConfig)custom.CloneNew()).MinimumMatchLength, Is.EqualTo(2));
        foreach (var level in LevelLibrary.AllLevels)
        for (int round = 1; round <= level.GetTotalRounds; round++)
        {
            Assert.That(level.GetMatchRules(round).TrueForAll(rule => rule.minimumLength == 4), Is.True,
                $"Level {level.GetLevelNumber}, round {round}");
            Assert.That(((CampaignLevel)level).CloneNew().GetMatchRules(round).TrueForAll(rule => rule.minimumLength == 4), Is.True);
        }
    }
}

[TestFixture]
public class PickerMobileScaleTests
{
    [TestCase(1080, 2200, true, 1.8f)]
    [TestCase(390, 844, true, .65f)]
    [TestCase(2200, 1080, true, 1.8f)]
    [TestCase(1920, 1080, false, .8f)]
    [TestCase(1625, 864, false, .64f)]
    [TestCase(3840, 2160, false, 1.6f)]
    [TestCase(2560, 1440, false, 1.066667f)]
    [TestCase(360, 720, false, .533333f)]
    public void PickerFillsMobileWidthAndScalesDesktopWithResolution(float width, float height, bool mobile, float expected)
    {
        Assert.That(LevelSelectUI.PickerScale(width, height, mobile), Is.EqualTo(expected).Within(.001f));
    }
}

[TestFixture]
public class MechanicPresentationTimerTests
{
    [Test] public void PresentationHoldsCountdownAndExpirationUntilDisposed()
    {
        var timer=new TimerController();timer.Reset(1);timer.Start();int expired=0;timer.OnExpired+=()=>expired++;
        using(timer.HoldPresentation()){timer.Tick(5);Assert.That(timer.Remaining,Is.EqualTo(1));Assert.That(expired,Is.Zero);}
        timer.Tick(1);Assert.That(expired,Is.EqualTo(1));Assert.That(timer.IsPresentationHeld,Is.False);
    }
    [Test] public void NestedPresentationDoesNotResumeManualPauseOrAnotherHold()
    {
        var timer=new TimerController();timer.Reset(10);timer.Start();
        var first=timer.HoldPresentation();var second=timer.HoldPresentation();
        first.Dispose();first.Dispose();timer.Tick(1);Assert.That(timer.Remaining,Is.EqualTo(10));
        timer.Pause();second.Dispose();timer.Tick(1);Assert.That(timer.Remaining,Is.EqualTo(10));
        timer.Resume();timer.Tick(1);Assert.That(timer.Remaining,Is.EqualTo(9));
    }
    [Test] public void CancellationAndNewRoundDiscardOldHoldsWithoutReleasingNewOnes()
    {
        var timer=new TimerController();timer.Reset(10);timer.Start();var old=timer.HoldPresentation();
        timer.ClearPresentationHolds();Assert.That(timer.IsPresentationHeld,Is.False);
        timer.Reset(20);timer.Start();var current=timer.HoldPresentation();old.Dispose();
        timer.Tick(5);Assert.That(timer.Remaining,Is.EqualTo(20));
        current.Dispose();timer.Tick(1);Assert.That(timer.Remaining,Is.EqualTo(19));
    }
}
