using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class SharedContentTests
{
    [Test]
    public void GlobalBaseGrowthKeepsAllScopeAndShowsItsNumberedBadge()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var power=new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo,2,10,0);
        var spec=PowerCardFlow.Describe(power);
        Assert.That(spec.BaseBadge,Is.EqualTo("+2B"));
        Assert.That(spec.AllAnimals,Is.True);
        Assert.That(PowerCardFlow.Describe(power.Clone().ForGoal(GoalType.BlocksCleared)).BaseBadge,Is.Null);
        Assert.That(PowerCardFlow.Describe(new AnimalDoublePower(ItemType.Hippo)).BaseBadge,Is.Null,"Multiplying base is not an additive base bonus.");
    }

    [Test]
    public void AnimalBaseBonusesUseTunedBlueBBadges()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var power=new IconTypeGivesBase(ItemType.Giraffe,5);
        Assert.That(power.Name,Is.EqualTo("Giraffe Bonus (Points)"));
        var points=PowerCardFlow.Describe(power);
        Assert.That(points.RewardShape,Is.EqualTo("base-badge"));
        Assert.That(points.Reward,Is.EqualTo("+5B PER TILE"));
        Assert.That(points.RewardAnimal,Is.EqualTo(ItemType.Giraffe));
        Assert.That(points.Single,Is.True);
        Assert.That(PowerCardFlow.Describe(power.Clone().ForGoal(GoalType.BlocksCleared)).RewardShape,Is.EqualTo("credits"));
    }

    [Test]
    public void MagnetsUseOneRefillAndAnimalGraphicInBothGoals()
    {
        foreach(var goal in new[]{GoalType.Score,GoalType.BlocksCleared}) {
            var spec=PowerCardFlow.Describe(new AnimalSpawnPower(ItemType.Bear).ForGoal(goal));
            Assert.That(spec.Single,Is.True);
            Assert.That(spec.Shape,Is.EqualTo("refill"));
            Assert.That(spec.RewardAnimal,Is.EqualTo(ItemType.Bear));
            Assert.That(spec.Reward,Is.EqualTo("2x BEAR CHANCE"));
            Assert.That(spec.Condition,Is.EqualTo("REFILLS + SHUFFLES"));
        }
    }

    [Test]
    public void NextMatchPowersShowCountAndMultiplierArt()
    {
        var jackpot=PowerCardFlow.Describe(new JackpotFivePower());
        Assert.That(jackpot.Shape,Is.EqualTo("count-5plus"));
        Assert.That(jackpot.RewardShape,Is.EqualTo("factor-5"));
        Assert.That(jackpot.Reward,Is.EqualTo("NEXT MATCH"));
        Assert.That(PowerCardFlow.Describe(new TwinShotPower()).RewardShape,Is.EqualTo("factor-2"));
        Assert.That(PowerCardFlow.Describe(new FourFuryPower()).RewardShape,Is.EqualTo("factor-3"));
        Assert.That(PowerCardFlow.Describe(new PrecisionStrikePower()).RewardShape,Is.EqualTo("factor-4"));
    }

    [Test]
    public void PactDiagramShowsBothAnimalsAndHalfRewardInsteadOfBonus()
    {
        var pact=new AnimalPairPower(ItemType.Bear,ItemType.Sloth);
        var points=PowerCardFlow.Describe(pact);
        Assert.That(points.ConditionAnimal,Is.EqualTo(ItemType.Bear));
        Assert.That(points.PartnerAnimal,Is.EqualTo(ItemType.Sloth));
        Assert.That(points.Condition,Is.EqualTo("MATCH TOGETHER"));
        Assert.That(points.Shape,Is.EqualTo("pair"));
        Assert.That(points.Reward,Is.EqualTo("HALF POINTS"));
        Assert.That(PowerCardFlow.Describe(pact.Clone().ForGoal(GoalType.BlocksCleared)).Reward,Is.EqualTo("HALF FINAL BLOCKS"));
    }

    [Test]
    public void AlsoCountPowersAreRetiredFromGameplay()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        foreach(ItemType animal in System.Enum.GetValues(typeof(ItemType))) {
            var power=new CollectionPower(CollectionStyle.Admission,animal);
            Assert.That(SharedGameContent.Enabled(power),Is.False);
            Assert.That(SharedGameContent.Enabled(power.Clone()),Is.False);
        }
        var retired=SharedGameContent.Document["powers"].Where(p=>((string)p["name"]).StartsWith("Also Count ")).ToList();
        Assert.That(retired.Count,Is.EqualTo(10));
        Assert.That(retired.All(p=>(bool?)p["enabled"]==false),Is.True);
        Assert.That(SharedGameContent.Enabled(new CollectionPower(CollectionStyle.Transform,ItemType.Bear)),Is.True);
    }

    [Test]
    public void PowerFlowDiagramsShowAnimalCountsRewardsAndActualScope()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var growth=new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo,1,10,0);
        var points=PowerCardFlow.Describe(growth);
        Assert.That(points.Condition,Is.EqualTo("10 TILES"));
        Assert.That(points.ConditionAnimal,Is.EqualTo(ItemType.Hippo));
        Assert.That(points.Reward,Is.EqualTo("+2 BASE / TILE"),"Use authored tuning, not constructor defaults.");
        Assert.That(points.AllAnimals,Is.True);
        Assert.That(points.Growth,Is.True);
        Assert.That(growth.MatchedTiles,Is.Zero,"Building a preview must not advance power progress.");
        var blocks=PowerCardFlow.Describe(growth.Clone().ForGoal(GoalType.BlocksCleared));
        Assert.That(blocks.AllAnimals,Is.False);
        Assert.That(blocks.RewardAnimal,Is.EqualTo(ItemType.Hippo));
        Assert.That(blocks.Reward,Is.EqualTo("+2 BLOCKS"));
        var mastery=PowerCardFlow.Describe(new AnimalMasteryPower(ItemType.Hippo,true));
        Assert.That(mastery.Single,Is.True);
        Assert.That(mastery.Condition,Does.StartWith("EVERY 2 PLAYER MATCHES"));
        Assert.That(mastery.RewardAnimal,Is.EqualTo(ItemType.Hippo));
        var bloom=PowerCardFlow.Describe(new CollectionPower(CollectionStyle.Transform,ItemType.Hippo));
        Assert.That(bloom.Condition,Is.EqualTo("PLAYER MATCH"));
        Assert.That(bloom.Reward,Is.EqualTo("TURN 2 TILES"));
        Assert.That(bloom.RewardAnimal,Is.EqualTo(ItemType.Hippo));
    }

    [Test]
    public void GrowthDescriptionsStayConciseAndKeepTriggerRewardAndScope()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var growth=new IconTypeStreakGivesBasePointsOrMulti(ItemType.Sloth,2,10,0);
        Assert.That(growth.PowerUpDescription,Is.EqualTo("Every 10 Sloth tiles: +2 base points per tile for all matches this run."));
        growth.ForGoal(GoalType.BlocksCleared);
        Assert.That(growth.PowerUpDescription,Does.Contain("per Sloth match"));
        Assert.That(growth.PowerUpDescription,Does.Contain("Applies immediately"));
        Assert.That(growth.PowerUpDescription.Length,Is.LessThan(125));
        foreach(var definition in SharedGameContent.Document["powers"].Where(p=>(string)p["type"]=="IconTypeStreakGivesBasePointsOrMulti")) {
            Assert.That(SharedGameContent.RenderForGoal((JObject)definition,GoalType.Score).Length,Is.LessThan(125));
            Assert.That(SharedGameContent.RenderForGoal((JObject)definition,GoalType.BlocksCleared).Length,Is.LessThan(125));
        }
    }

    [Test]
    public void PowerDescriptionsDoNotAdvertiseRemovedExtraClearCap()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        foreach(var power in SharedGameContent.Document["powers"])
            Assert.That(power.ToString(),Does.Not.Contain("Max 2 extra clears per match"),(string)power["name"]);
    }

    [Test]
    public void PowerTiersUseAuthoredContentAndSurviveClonesAndGoalChanges()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var powers=new PowerUp[]{new IconTypeGivesBase(ItemType.Duck,1),
            new AnimalMasteryPower(ItemType.Duck,true),new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck,1,10,0)};
        var tiers=new[]{PowerTier.Basic,PowerTier.Rare,PowerTier.Legendary};
        for(int i=0;i<powers.Length;i++) {
            Assert.That(powers[i].Rarity,Is.EqualTo(tiers[i]));
            Assert.That(powers[i].Clone().Rarity,Is.EqualTo(tiers[i]));
            Assert.That(powers[i].Clone().ForGoal(GoalType.BlocksCleared).Rarity,Is.EqualTo(tiers[i]));
        }
        Assert.That(SharedGameContent.Document["powers"].All(p=>new[]{"Basic","Rare","Legendary"}.Contains((string)p["tier"])),Is.True);
    }

    [TestCase(BloomCondition.Edge)]
    [TestCase(BloomCondition.Square)]
    [TestCase(BloomCondition.Large)]
    public void ConditionalBloomsRequireTheirPatternAndConvertFourNeighbors(BloomCondition condition)
    {
        var board = new BoardModel(7,7,new[] { new MatchRule(MatchType.Snake,4) });
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++) board[x,y] = ItemType.Duck;
        var power = new CollectionPower(CollectionStyle.Transform, ItemType.Bear, bloom: condition);
        var ordinary = new List<BoardModel.Cell> { new(1,2), new(2,2), new(3,2), new(4,2) };
        Assert.That(CollectionPower.TransformNeighbors(board, new[] { ordinary }, ordinary, new[] { power }, true), Is.Empty);
        var group = condition switch
        {
            BloomCondition.Edge => new List<BoardModel.Cell> { new(0,2), new(1,2), new(2,2), new(3,2) },
            BloomCondition.Square => new List<BoardModel.Cell> { new(2,2), new(3,2), new(2,3), new(3,3) },
            _ => new List<BoardModel.Cell> { new(1,2), new(2,2), new(3,2), new(4,2), new(5,2), new(5,3) }
        };
        Assert.That(CollectionPower.TransformNeighbors(board, new[] { group }, group, new[] { power }, false), Is.Empty);
        var changed = CollectionPower.TransformNeighbors(board, new[] { group }, group, new[] { power }, true);
        Assert.That(changed.Count, Is.EqualTo(4));
        Assert.That(changed.All(cell => !group.Contains(cell) && board[cell.X,cell.Y] == ItemType.Bear), Is.True);
        Assert.That(((CollectionPower)power.Clone()).Bloom, Is.EqualTo(condition));
        Assert.That(power.SupportsItems(new List<ItemType> { ItemType.Duck }), Is.False);
        Assert.That(power.SupportsItems(new List<ItemType> { ItemType.Bear, ItemType.Duck }), Is.True);
        Assert.That(power.CanUseWithMatchRules(new[] { new MatchRule(MatchType.Horizontal,4) }), Is.EqualTo(condition != BloomCondition.Square));
    }

    [Test]
    public void SharedGoalVariantsKeepNativeIdentityTuningAndGoalSpecificWording()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        var adapted=PowerOfferBuilder.Catalog().Where(SharedPowerRewards.CanAdapt).ToList();
        Assert.That(adapted.Count,Is.EqualTo(52));
        foreach(var power in adapted) {
            var points=power.Clone().ForGoal(GoalType.Score);
            var blocks=power.Clone().ForGoal(GoalType.BlocksCleared);
            Assert.That(blocks.Name,Is.EqualTo(points.Name));
            Assert.That(blocks.NativeContentKey,Is.EqualTo(points.NativeContentKey));
            Assert.That(blocks.PowerUpDescription,Does.Not.Contain("base points").And.Not.Contain("multiplier"));
            Assert.That(blocks.PowerUpDescription,Does.Not.Contain("{"));
            Assert.That(SharedGameContent.Power(blocks)["goals"].ToString(),Is.EqualTo(blocks.SupportedGoals.ToString()));
            Assert.That(blocks.Clone().PowerUpDescription,Is.EqualTo(blocks.PowerUpDescription));
        }
        var four=new FourExactlyPower();four.ConfigureSharedContent();
        int original=four.AdditiveMultiplierMod(new List<Tile>{null,null,null,null});
        var result=new ScoreResult();SharedPowerRewards.Resolve(four,4,true,0,original,1,1,result);
        Assert.That(result.ExtraClears[four],Is.EqualTo(original));
        Assert.That(four.PowerUpDescription,Does.Contain("multiplier"));
    }

    [Test]
    public void AlwaysOnBonusesStayQuietWhileWalrusAndEarnedUpgradesStillTrigger()
    {
        var elephant=new RosterTradeoffPower(ItemType.Elephant,true);
        var moose=new RosterTradeoffPower(ItemType.Moose,true);
        int boosted=moose.AdjustFinalScore(null,elephant.AdjustFinalScore(null,100));
        Assert.That(boosted,Is.EqualTo(169),"Passive scoring remains intact.");
        var queue=new PowerFountainQueue();
        foreach(var passive in new PowerUp[]{elephant,moose,new BasePowerUp(4),new BaseMultiPowerUp(4,2),
            new MatchFoundationPower(4,4),new BlockMatchFoundationPower(4,2),new FlexibleMatchPower(3),new BasePerTile(),new BlockClearPower(BlockClearStyle.Neighbor)}) {
            Assert.That(passive.HasConditionalTrigger,Is.False,passive.Name);
            var eventData=new PowerActivation(passive,"Applied");
            Assert.That(eventData.Display,Is.EqualTo(PowerTriggerDisplay.None));
            queue.Enqueue(1,new[]{eventData});
            Assert.That(PowerActivation.FromScoring(passive,4,16,2,1,1,0,0,1),Is.Null);
        }
        Assert.That(queue.Count,Is.Zero);
        var walrus=new AnimalDoublePower(ItemType.Walrus);
        Assert.That(PowerActivation.FromScoring(walrus,4,0,0,1,1,0,0,1),Is.Null);
        var qualifying=PowerActivation.FromScoring(walrus,4,0,0,2,1,0,0,1);
        Assert.That(qualifying,Is.Not.Null);queue.Enqueue(1,new[]{qualifying});Assert.That(queue.Count,Is.EqualTo(1));
        var always=new SharedRulePower(new JObject{["id"]="custom-power-passive-feedback",["name"]="Passive",["trigger"]="always",["animal"]="Any",["permanent"]=false,["displayWhenTriggered"]="iconAndTiles"});
        Assert.That(always.HasConditionalTrigger,Is.False);
        Assert.That(new PowerActivation(always,"Applied").Display,Is.EqualTo(PowerTriggerDisplay.None));
        var conditional=new SharedRulePower(new JObject{["id"]="custom-power-conditional-feedback",["name"]="Conditional",["trigger"]="animal",["animal"]="Walrus"});
        Assert.That(conditional.HasConditionalTrigger,Is.True);
        var growth=new FeedbackHookSpy();
        Assert.That(PowerActivation.FromScoring(growth,4,8,0,1,1,1,0,1).Effect,Is.EqualTo("UPGRADE!"));
    }

    [Test]
    public void PowerFountainRetainsAllPowersFromOneMoveAndAllowsTheNextMoveToRetrigger()
    {
        var queue=new PowerFountainQueue();
        var powers=Enumerable.Range(0,24).Select(_=>new FeedbackHookSpy()).ToArray();
        queue.Enqueue(12,powers.Take(12).Select(p=>new PowerActivation(p,"Score")));
        queue.Enqueue(12,powers.Skip(12).Select(p=>new PowerActivation(p,"Clear")));
        queue.Enqueue(12,powers.Select(p=>new PowerActivation(p,"Another effect")));
        Assert.That(queue.Count,Is.EqualTo(24),"No power cap or replacement, one icon per distinct power per move.");
        Assert.That(queue.Dequeue().Power,Is.SameAs(powers[0]));
        queue.Enqueue(13,new[]{new PowerActivation(powers[0],"New move")});
        Assert.That(queue.Count,Is.EqualTo(24),"A later move may retrigger while earlier icons are queued.");
        for(int n=1;n<24;n++)Assert.That(queue.Dequeue().Power,Is.SameAs(powers[n]));
        Assert.That(queue.Dequeue().Power,Is.SameAs(powers[0]));
        queue.Enqueue(13,new[]{new PowerActivation(powers[0],"Duplicate")});
        Assert.That(queue.Count,Is.Zero);
        var silent=new FeedbackHookSpy{Presentation=PowerTriggerDisplay.None};
        var tilesOnly=new FeedbackHookSpy{Presentation=PowerTriggerDisplay.MatchedTiles};
        queue.Enqueue(13,new[]{new PowerActivation(silent,"No visual"),new PowerActivation(tilesOnly,"Tiles only")});
        Assert.That(queue.Count,Is.Zero,"The standardized display setting still controls icon emission.");
        queue.Clear();queue.Enqueue(13,new[]{new PowerActivation(powers[0],"Restarted round")});
        Assert.That(queue.Count,Is.EqualTo(1));
        queue.Clear();Assert.That(queue.Count,Is.Zero);
        Assert.That(powers.All(p=>p.Calls==0),Is.True,"Presentation never reruns scoring.");
    }

    [Test]
    public void TriggerDisplaysArePresentationOnlyAndKeepLegacyDefaults()
    {
        Assert.That(PowerTriggerFeedback.Parse(null),Is.EqualTo(PowerTriggerDisplay.IconAndMatchedTiles));
        foreach(var pair in new[]{("none",PowerTriggerDisplay.None),("icon",PowerTriggerDisplay.Icon),
            ("tiles",PowerTriggerDisplay.MatchedTiles),("iconAndTiles",PowerTriggerDisplay.IconAndMatchedTiles)}) {
            var power=new FeedbackHookSpy{Presentation=PowerTriggerFeedback.Parse(pair.Item1)};
            var activation=PowerActivation.FromScoring(power,4,10,0,1,1,2,0,1);
            Assert.That(activation,Is.Not.Null,"A display choice cannot suppress a gameplay activation.");
            Assert.That(activation.Display,Is.EqualTo(pair.Item2));
            Assert.That(activation.Effect,Does.Contain("+2 base"));
            Assert.That(power.Calls,Is.Zero);
            power.Presentation=PowerTriggerDisplay.None;
            Assert.That(activation.Display,Is.EqualTo(pair.Item2),"Resolved display metadata is immutable.");
            var rule=new SharedRulePower(new JObject{["id"]="custom-power-display-test",["name"]="Display test",["displayWhenTriggered"]=pair.Item1});
            Assert.That(rule.DisplayWhenTriggered,Is.EqualTo(pair.Item2));
            Assert.That(rule.Clone().DisplayWhenTriggered,Is.EqualTo(pair.Item2));
        }
    }

    [Test]
    public void LevelCharacteristicsAreRuntimePowersAndEmitOnlyAppliedRewards()
    {
        var manager=new PowerManager(null,null,null);
        var level=new CampaignLevel(2,"Garden","",new FreePlayConfig(),bonus:LevelBonus.GardenPlots,lightningLines:true);
        manager.ConfigureLevelPowers(level);
        Assert.That(manager.LevelPowers.Count,Is.EqualTo(2));
        var garden=manager.Characteristic(LevelBonus.GardenPlots);
        Assert.That(garden,Is.InstanceOf<PowerUp>());
        Assert.That(manager.Characteristic(LevelBonus.None,true).Name,Is.EqualTo("Lightning"));
        Assert.That(((LevelCharacteristicPower)garden.Clone()).Bonus,Is.EqualTo(garden.Bonus));
        var board=new BoardModel(5,5,new[]{new MatchRule(MatchType.Snake,4)});
        var square=new List<BoardModel.Cell>{new(1,1),new(2,1),new(1,2),new(2,2)};
        Assert.That(garden.Plant(board,new[]{square}),Is.EqualTo(4));
        Assert.That(garden.Plant(board,new[]{square}),Is.Zero,"Existing garden spaces do not retrigger planting.");
        var power=new LevelCharacteristicPower(LevelBonus.Square);var result=new ScoreResult();
        Assert.That(power.ApplyReward(4,square,true,result),Is.EqualTo(5));
        Assert.That(result.Activations.Single().Power,Is.SameAs(power));
        result=new ScoreResult();
        Assert.That(power.ApplyReward(0,square,true,result),Is.Zero);
        Assert.That(result.Activations,Is.Empty,"A condition with no applied reward does not flash.");
        var bent=new List<BoardModel.Cell>{new(0,0),new(1,0),new(2,0),new(2,1)};
        Assert.That(power.ApplyReward(4,bent,true,result),Is.EqualTo(4));
        Assert.That(result.Activations,Is.Empty);
        var border=new LevelCharacteristicPower(LevelBonus.BorderPatrol);
        Assert.That(border.BorderReward(board,new(){new(0,0),new(4,4)}),Is.EqualTo(400));
        Assert.That(board.Campaign.LitEdges,Is.Zero,"Feedback never evaluates a mechanic twice.");
        var admission=new CollectionPower(CollectionStyle.Admission,ItemType.Bear);
        int admitted=0;
        Assert.That(new BlockObjective(ItemType.Duck).Credit(square.Select(c=>new BlockMatchCell(c.X,c.Y,ItemType.Bear)),new[]{admission},
            onActivated:(p,n)=>{Assert.That(p,Is.SameAs(admission));admitted+=n;}),Is.EqualTo(4));
        Assert.That(admitted,Is.EqualTo(4));
        manager.ConfigureLevelPowers(null);Assert.That(manager.LevelPowers,Is.Empty);
    }

    [Test]
    public void AnimalGrowthCardsExplainTriggerAndRewardScope()
    {
        var mastery=new AnimalMasteryPower(ItemType.Duck,true);
        var growth=new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck,1,10,0);
        Assert.That(PowerUpIconLibrary.GrowthScope(mastery),Is.EqualTo("DUCK ONLY"));
        Assert.That(PowerUpIconLibrary.GrowthScope(growth),Is.EqualTo("ALL MATCHES"));
        Assert.That(PowerUpIconLibrary.GrowthExplanation(growth),Does.Contain("every animal"));
        growth.ForGoal(GoalType.BlocksCleared);
        Assert.That(PowerUpIconLibrary.GrowthScope(growth),Is.EqualTo("DUCK ONLY"));
        Assert.That(PowerUpIconLibrary.GrowthExplanation(growth),Does.Contain("Duck matches only"));
        Assert.That(PowerUpIconLibrary.GrowthScope(new SnowballPower()),Is.Empty);
    }

    [Test]
    public void CampaignSelectionAdvancesOnlyOnFirstCompletion()
    {
        Assert.That(ProgressStore.SelectionAfterWin(4,4,false,50),Is.EqualTo(5));
        Assert.That(ProgressStore.SelectionAfterWin(4,4,true,50),Is.EqualTo(4));
        Assert.That(ProgressStore.SelectionAfterWin(12,4,true,50),Is.EqualTo(12));
        Assert.That(ProgressStore.SelectionAfterWin(1,1,false,50),Is.EqualTo(2));
        Assert.That(ProgressStore.SelectionAfterWin(50,50,false,50),Is.EqualTo(50));
    }

    [Test]
    public void DifficultiesCompoundTargetsAndPreserveAuthoredRulesAndClones()
    {
        Assert.That(CampaignLevel.DifficultyTarget(100,1),Is.EqualTo(100));
        Assert.That(CampaignLevel.DifficultyTarget(100,2),Is.EqualTo(110));
        Assert.That(CampaignLevel.DifficultyTarget(100,3),Is.EqualTo(121));
        Assert.That(CampaignLevel.DifficultyTarget(34,2),Is.EqualTo(38));
        Assert.That(CampaignLevel.DifficultyTarget(1,int.MaxValue),Is.EqualTo(int.MaxValue));
        Assert.That(ProgressStore.CompletedAfterWin(0,1),Is.EqualTo(1));
        Assert.That(ProgressStore.CompletedAfterWin(1,2),Is.EqualTo(2));
        Assert.That(ProgressStore.CompletedAfterWin(1,3),Is.EqualTo(1));
        Assert.That(ProgressStore.CompletedAfterWin(5,2),Is.EqualTo(5));
        Assert.That(ProgressStore.CompletedAfterWin(0,0),Is.EqualTo(0));
        Assert.That(ProgressStore.NextDifficulty(0),Is.EqualTo(1));
        Assert.That(ProgressStore.NextDifficulty(5),Is.EqualTo(6));
        Assert.That(ProgressStore.NextDifficulty(int.MaxValue),Is.EqualTo(int.MaxValue));
        var original = new CampaignLevel(2,"Test","Rules",new FreePlayConfig(target:100,levels:3,growthPercent:50),bossTargetMultiplier:2);
        var harder = original.WithDifficulty(3);
        Assert.That(original.Difficulty,Is.EqualTo(1));
        Assert.That(harder.GetTargetScore(1),Is.EqualTo(121));
        Assert.That(harder.GetTargetScore(3),Is.EqualTo(545));
        var clone = (CampaignLevel)harder.CloneNew();
        Assert.That(clone.Difficulty,Is.EqualTo(3));
        Assert.That(clone.GetTargetScore(3),Is.EqualTo(harder.GetTargetScore(3)));
        Assert.That(harder.WithScaling(200,50).GetTargetScore(1),Is.EqualTo(242));
        Assert.That(harder.Settings.Target,Is.EqualTo(100));
        Assert.That(harder.GetTotalTime,Is.EqualTo(original.GetTotalTime));
        Assert.That(harder.Moves,Is.EqualTo(original.Moves));
        Assert.That(harder.GetTotalRounds,Is.EqualTo(original.GetTotalRounds));
    }

    private sealed class FeedbackHookSpy : PowerUp
    {
        public int Calls;
        public PowerTriggerDisplay Presentation = PowerTriggerDisplay.IconAndMatchedTiles;
        public override PowerTriggerDisplay DisplayWhenTriggered => Presentation;
        public override int AdditiveBaseMod(List<Tile> tiles) { Calls++; return 10; }
        public override float FlatBasePerTile => 2;
        public override void Apply(GameState state) { }
        public override string GetCurrentPowerDescription() => "Spy";
        public override PowerUp Clone() => new FeedbackHookSpy();
    }

    [Test]
    public void PowerFeedbackReportsResolvedBonusesWithoutReevaluatingHooks()
    {
        var animal=new AnimalDoublePower(ItemType.Bear);
        var triggered=PowerActivation.FromScoring(animal,4,0,0,2,1,0,0,1);
        Assert.That(triggered.Power,Is.SameAs(animal));Assert.That(triggered.Effect,Is.EqualTo("x2"));
        Assert.That(PowerActivation.FromScoring(animal,4,0,0,1,1,0,0,1),Is.Null,"A nonqualifying match has no activation.");
        var spy=new FeedbackHookSpy();
        Assert.That(PowerActivation.FromScoring(spy,4,8,0,1,1,2,0,1),Is.Null,"Always-on baseline points do not trigger visual feedback.");
        Assert.That(PowerActivation.FromScoring(spy,4,18,0,1,1,2,0,1).Effect,Does.Contain("+10 base"));
        Assert.That(spy.Calls,Is.Zero,"Rendering data must never call scoring hooks.");
        Assert.That(PowerActivation.FromScoring(new BasePowerUp(4),4,16,0,1,1,4,0,1),Is.Null);
        var result=new ScoreResult();result.Activate(animal,"x2");Assert.That(result.Activations.Count,Is.EqualTo(1));
        var next=new ScoreResult();Assert.That(next.Activations,Is.Empty,"Each group has its own trigger list.");
    }

    [Test]
    public void ClearAndCollectionFeedbackOnlyReportsActualMatchingAnimalEffects()
    {
        var board=new BoardModel(5,5,new[] { new MatchRule(MatchType.Snake,4) });
        for(int x=0;x<5;x++)for(int y=0;y<5;y++)board[x,y]=ItemType.Duck;
        var group=new List<BoardModel.Cell>{new(1,1),new(2,1),new(1,2),new(2,2)};
        foreach(var cell in group)board[cell.X,cell.Y]=ItemType.Bear;
        var bear=new BlockClearPower(BlockClearStyle.Animal,ItemType.Bear);
        var duck=new BlockClearPower(BlockClearStyle.Animal,ItemType.Duck);
        var observed=new List<PowerUp>();int extra=0;
        var cleared=BlockClearPower.ExpandClears(board,new(){group},new PowerUp[]{bear,duck},false,
            (power,amount,cells)=>{observed.Add(power);extra+=amount;});
        Assert.That(cleared.Count,Is.EqualTo(6));Assert.That(extra,Is.EqualTo(2));Assert.That(observed,Is.EqualTo(new[]{bear}));
        observed.Clear();
        var full=Enumerable.Range(0,5).SelectMany(x=>Enumerable.Range(0,5).Select(y=>new BoardModel.Cell(x,y))).ToList();
        BlockClearPower.ExpandClears(board,new(){full},new[]{bear},false,(power,amount,cells)=>observed.Add(power));
        Assert.That(observed,Is.Empty,"A qualifying power cannot claim an extra clear if none was possible.");
        var collector=new CollectionPower(CollectionStyle.AnimalCredit,ItemType.Bear);
        var cells=group.Select(c=>new BlockMatchCell(c.X,c.Y,ItemType.Bear)).ToList();int credited=0;
        int credit=BlockObjective.Any.Credit(cells,new[]{collector},onActivated:(power,amount)=>credited+=amount);
        Assert.That(credit,Is.EqualTo(8));Assert.That(credited,Is.EqualTo(4));
        credited=0;new BlockObjective(ItemType.Duck).Credit(cells,new[]{collector},onActivated:(power,amount)=>credited+=amount);
        Assert.That(credited,Is.Zero,"Ineligible goal animals do not produce collection feedback.");
    }

    [Test]
    public void TransformationFeedbackReportsCommittedChangesAndIgnoresCascades()
    {
        var board=new BoardModel(5,5,new[]{new MatchRule(MatchType.Snake,4)});
        for(int x=0;x<5;x++)for(int y=0;y<5;y++)board[x,y]=ItemType.Duck;
        var group=new List<BoardModel.Cell>{new(1,1),new(2,1),new(1,2),new(2,2)};
        foreach(var cell in group)board[cell.X,cell.Y]=ItemType.Bear;
        var bloom=new CollectionPower(CollectionStyle.Transform,ItemType.Sloth);int notified=0;
        var changed=CollectionPower.TransformNeighbors(board,new[]{group},group,new[]{bloom},true,
            (power,amount,cells)=>{Assert.That(power,Is.SameAs(bloom));notified+=amount;});
        Assert.That(changed.Count,Is.EqualTo(2));Assert.That(notified,Is.EqualTo(2));
        Assert.That(changed.All(c=>board[c.X,c.Y]==ItemType.Sloth),Is.True);
        notified=0;
        CollectionPower.TransformNeighbors(board,new[]{group},group,new[]{bloom},false,(power,amount,cells)=>notified++);
        Assert.That(notified,Is.Zero);
        for(int x=0;x<5;x++)for(int y=0;y<5;y++)board[x,y]=ItemType.Sloth;
        CollectionPower.TransformNeighbors(board,new[]{group},group,new[]{bloom},true,(power,amount,cells)=>notified++);
        Assert.That(notified,Is.Zero,"No visual trigger for unchanged neighbors.");
    }

    [Test]
    public void MoveBossTaxesUseBoardSnapshotAndChargeOnlyOncePerSwap()
    {
        var state = new BossMoveTax();
        var inner = new[] { new BoardModel.Cell(1,1), new BoardModel.Cell(2,1), new BoardModel.Cell(1,2), new BoardModel.Cell(2,2) };
        var board = new[] { ItemType.Bear,ItemType.Bear,ItemType.Duck,ItemType.Duck,ItemType.Sloth };
        Assert.That(BossMoveTax.MostCommon(board), Is.EquivalentTo(new[] { ItemType.Bear,ItemType.Duck }));
        state.BeginPlay(board);
        state.Observe(new[] { ItemType.Duck },inner,7,7,true);
        state.Observe(new[] { ItemType.Bear },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.CrowdTax), Is.EqualTo(1), "Tied common animals qualify, but multiple groups cost only one extra move.");
        Assert.That(state.FinishPlay(BossStyle.CrowdTax), Is.Zero, "Duplicate completion cannot charge twice.");
        state.BeginPlay(board);
        state.Observe(new[] { ItemType.Sloth },inner,7,7,true);
        state.Observe(new[] { ItemType.Bear },inner,7,7,false);
        Assert.That(state.FinishPlay(BossStyle.CrowdTax), Is.Zero, "Cascades never trigger the tax.");
        state.BeginPlay(board);
        Assert.That(state.FinishPlay(BossStyle.CrowdTax), Is.Zero, "Failed swaps keep their ordinary one-move cost.");
        Assert.That(BossMoveTax.MostCommon(System.Array.Empty<ItemType>()), Is.Empty);
    }

    [Test]
    public void MoveBossShapeRulesEvaluateWholePlayAndRepeatHistoryResets()
    {
        var state = new BossMoveTax();
        var inner = Enumerable.Range(1,4).Select(x => new BoardModel.Cell(x,2)).ToArray();
        var large = Enumerable.Range(0,6).Select(x => new BoardModel.Cell(x,2)).ToArray();
        var board = new[] { ItemType.Bear,ItemType.Duck };
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.SmallChange), Is.EqualTo(1));
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true); state.Observe(new[] { ItemType.Duck },large,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.SmallChange), Is.Zero, "One large initial group qualifies the entire swap.");
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true); state.Observe(new[] { ItemType.Duck },large,7,7,false);
        Assert.That(state.FinishPlay(BossStyle.InnerToll), Is.EqualTo(1), "A cascade reaching the edge does not exempt an interior swap.");
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true); state.Observe(new[] { ItemType.Duck },large,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.InnerToll), Is.Zero);
        state.Reset();
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.RepeatToll), Is.Zero);
        state.BeginPlay(board); Assert.That(state.FinishPlay(BossStyle.RepeatToll), Is.Zero);
        state.BeginPlay(board); state.Observe(new[] { ItemType.Bear },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.RepeatToll), Is.EqualTo(1), "Failed swaps do not erase repeat history.");
        state.BeginPlay(board); state.Observe(new[] { ItemType.Duck },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.RepeatToll), Is.Zero);
        state.Reset(); state.BeginPlay(board); state.Observe(new[] { ItemType.Duck },inner,7,7,true);
        Assert.That(state.FinishPlay(BossStyle.RepeatToll), Is.Zero, "Retries and rounds start with fresh history.");
    }

    [Test]
    public void MoveBossesHaveConcisePortraitsAndOnlyAppearInLimitedMoves()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json")); LevelLibrary.ReloadSharedContent();
        foreach (var style in new[] { BossStyle.CrowdTax,BossStyle.SmallChange,BossStyle.InnerToll,BossStyle.RepeatToll }) {
            var boss = new ChallengeBoss(style,GoalType.BlocksCleared);
            Assert.That(boss.Supports(GoalType.BlocksCleared,FreePlayMode.LimitedMoves), Is.True);
            Assert.That(boss.Supports(GoalType.BlocksCleared,FreePlayMode.Timed), Is.False);
            Assert.That(ChallengeBoss.RewardPercent(style,new[] { new BoardModel.Cell(1,1) },7,7,1,true,false), Is.EqualTo(100));
            Assert.That(((ChallengeBoss)boss.Clone()).Style, Is.EqualTo(style));
            Assert.That(boss.BriefRule.Length, Is.LessThan(100));
            Assert.Throws<System.ArgumentException>(() => new CampaignLevel(99,"Invalid","",new FreePlayConfig(),finalBossStyle:style));
        }
        foreach (var level in LevelLibrary.AllLevels.Cast<CampaignLevel>())
            foreach (var boss in level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds).OfType<ChallengeBoss>())
                if (boss.UsesMoveTax) Assert.That(level.Mode, Is.EqualTo(FreePlayMode.LimitedMoves));
        var portraits = System.Enum.GetValues(typeof(BossStyle)).Cast<BossStyle>().Select(PowerUpIconLibrary.BossPortrait).ToArray();
        Assert.That(portraits.All(p => p.Animal.HasValue && p.Subject != "BOSS"), Is.True);
        Assert.That(portraits.Select(p => (p.Animal,p.Effect)).Distinct().Count(), Is.EqualTo(portraits.Length));
    }

    [Test]
    public void SharedBossWordingAndRewardsAgreeForPointsAndBlocks()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        foreach(var goal in new[] { GoalType.Score,GoalType.BlocksCleared }) {
            var boss=new ChallengeBoss(BossStyle.Wallflower,goal);
            Assert.That(boss.PowerUpDescription,Does.Contain("at least 3"));
            Assert.That(boss.PowerUpDescription,Does.Contain(goal==GoalType.Score?"fewer points":"fewer block credit"));
            Assert.That(boss.BriefRule,Does.Contain("25%"));
            Assert.That(((ChallengeBoss)boss.Clone()).BriefRule,Is.EqualTo(boss.BriefRule));
        }
    }

    [Test]
    public void PenaltyBossesDoNotRetainRewardEraTargetMultipliers()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json")); LevelLibrary.ReloadSharedContent();
        var levels=LevelLibrary.AllLevels.Cast<CampaignLevel>().ToList();
        Assert.That(levels.All(l=>l.BossTargetMultiplier==1),Is.True);
        foreach(var level in levels) {
            for(int round=1;round<level.GetTotalRounds;round++) Assert.That(level.GetTargetScore(round),Is.EqualTo(level.Settings.GetTargetScore(round)));
            Assert.That(level.GetTargetScore(level.GetTotalRounds),Is.EqualTo(level.Settings.GetTargetScore(level.GetTotalRounds)));
            var clone=(CampaignLevel)level.CloneNew();
            Assert.That(clone.GetTargetScore(clone.GetTotalRounds),Is.EqualTo(level.GetTargetScore(level.GetTotalRounds)));
            var edited=level.WithScaling(100,0);
            Assert.That(edited.GetTargetScore(1),Is.EqualTo(100));
            Assert.That(edited.GetTargetScore(edited.GetTotalRounds),Is.EqualTo(100));
        }
        var noBoss=new CampaignLevel(99,"No boss","",new FreePlayConfig(levels:3),bossFinal:false,bossTargetMultiplier:2);
        Assert.That(noBoss.GetTargetScore(3),Is.EqualTo(noBoss.Settings.GetTargetScore(3)));
        var capped=new CampaignLevel(99,"Cap","",new FreePlayConfig(target:1000000,levels:30,growthPercent:1000),bossTargetMultiplier:10);
        Assert.That(capped.GetTargetScore(30),Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CalendarPaginationSupportsLargeCampaignsAndPreservesSelectedPage()
    {
        Assert.That(CampaignCalendar.PageCount(0),Is.EqualTo(1));
        Assert.That(CampaignCalendar.PageCount(20),Is.EqualTo(1));
        Assert.That(CampaignCalendar.PageCount(24),Is.EqualTo(2));
        Assert.That(CampaignCalendar.PageCount(500),Is.EqualTo(25));
        Assert.That(CampaignCalendar.PageForLevel(19,24),Is.Zero);
        Assert.That(CampaignCalendar.PageForLevel(20,24),Is.EqualTo(1));
        Assert.That(CampaignCalendar.PageForLevel(499,500),Is.EqualTo(24));
        Assert.That(CampaignCalendar.PageForLevel(999,24),Is.EqualTo(1));
        Assert.That(CampaignCalendar.PageForLevel(-1,24),Is.Zero);
        var covered=new HashSet<int>();
        for(int page=0;page<CampaignCalendar.PageCount(503);page++)
            for(int index=page*CampaignCalendar.PageSize;index<System.Math.Min(503,(page+1)*CampaignCalendar.PageSize);index++)
                Assert.That(covered.Add(index),Is.True);
        Assert.That(covered.Count,Is.EqualTo(503));
    }

    [Test]
    public void FoundationBalanceAndScalingPreserveLevelIdentity()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        var foundations = MatchFoundationPower.Choices();
        foreach (MatchFoundationPower power in foundations) power.ConfigureSharedContent();
        Assert.That(foundations.Cast<MatchFoundationPower>().Select(p => p.BasePerTile), Is.EqualTo(new[] { 2, 4, 8 }));
        foreach (var original in LevelLibrary.AllLevels.Cast<CampaignLevel>().Where(l => !l.IsTutorial))
        {
            var edited = original.WithScaling(100, 50);
            Assert.That(edited.GetTargetScore(1), Is.EqualTo(100));
            Assert.That(edited.GetTargetScore(2), Is.EqualTo(150));
            Assert.That(edited.GetTargetScore(3), Is.EqualTo(225));
            Assert.That(edited.Bonus, Is.EqualTo(original.Bonus));
            Assert.That(edited.PowerOffers, Is.SameAs(original.PowerOffers));
            Assert.That(edited.FinalBossStyle, Is.EqualTo(original.FinalBossStyle));
            Assert.That(edited.Replacement, Is.EqualTo(original.Replacement));
            Assert.That(((CampaignLevel)edited.CloneNew()).Settings.GrowthPercent, Is.EqualTo(50));
            if (original.GetLevelNumber > 5 && original.Goal == GoalType.BlocksCleared)
                {
                    var opening = original.GetPowerUpsForRound(1);
                    Assert.That(opening.Count, Is.EqualTo(3));
                    Assert.That(opening.Any(p => p is MatchFoundationPower), Is.False);
                    Assert.That(original.Settings.MinimumMatchLength, Is.EqualTo(4));
                }
        }
    }

    [Test]
    public void NativeLevelCopiesKeepIndependentTuningThroughCloningAndOffers()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            var d=JObject.Parse(baseline);
            var basis=d["powers"].Cast<JObject>().Single(p=>(string)p["type"]=="FourExactlyPower");
            var copy=(JObject)basis.DeepClone();copy.Remove("nativeKey");
            copy["id"]="custom-power-level-copy-test";copy["basePowerId"]=(string)basis["id"];
            copy["source"]="variant";copy["levelId"]="level-7";copy["name"]="Garden Four Copy";copy["parameters"]["basePointsMulti"]=9;
            ((JArray)d["powers"]).Add(copy);
            d["levels"][6]["powerOverrides"]=new JObject { [(string)basis["id"]]=(string)copy["id"] };
            SharedGameContent.Load(d.ToString());
            var catalog=PowerOfferBuilder.Catalog();
            var garden=SharedGameContent.ForLevel(catalog,7);
            var copied=garden.Single(p=>p.Name=="Garden Four Copy");
            var four=new List<Tile>{null,null,null,null};
            Assert.That(copied.AdditiveMultiplierMod(four),Is.EqualTo(9));
            Assert.That(copied.Clone().AdditiveMultiplierMod(four),Is.EqualTo(9));
            Assert.That(copied.Clone().SharedDefinitionId,Is.EqualTo((string)copy["id"]));
            Assert.That(SharedGameContent.ForLevel(catalog,8).Any(p=>p.Name=="Garden Four Copy"),Is.False);
            var group=JObject.Parse("{mode:'powers',powerIds:[]}");((JArray)group["powerIds"]).Add((string)basis["id"]);
            Assert.That(SharedGameContent.MatchesOfferGroup(group,copied),Is.True);
            basis["parameters"]["basePointsMulti"]=5;
            SharedGameContent.Load(d.ToString());
            var global=new FourExactlyPower();global.ConfigureSharedContent();
            Assert.That(global.AdditiveMultiplierMod(four),Is.EqualTo(5));
            Assert.That(SharedGameContent.Find((string)copy["id"],PowerOfferBuilder.Catalog()).AdditiveMultiplierMod(four),Is.EqualTo(9));
            d["levels"][7]["powerOverrides"]=new JObject { [(string)basis["id"]]=(string)copy["id"] };
            Assert.Throws<System.ArgumentException>(()=>SharedGameContent.Load(d.ToString()));
        } finally {SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }

    [Test]
    public void FoundationCopiesUseTheirOwnMatchSizeAndCreditInOpeningPools()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            var d=JObject.Parse(baseline);
            var basis=d["powers"].Cast<JObject>().Single(p=>(string)p["name"]=="Steady Clears");
            var copy=(JObject)basis.DeepClone();copy.Remove("nativeKey");
            copy["id"]="custom-power-block-copy-test";copy["basePowerId"]=(string)basis["id"];
            copy["source"]="variant";copy["levelId"]="level-5";copy["name"]="Custom Steady Clears";
            copy["parameters"]["pointsPerTile"]=3;copy["parameters"]["minimumMatch"]=5;
            ((JArray)d["powers"]).Add(copy);
            d["levels"][4]["powerOverrides"]=new JObject { [(string)basis["id"]]=(string)copy["id"] };
            SharedGameContent.Load(d.ToString());LevelLibrary.ReloadSharedContent();
            var level=(CampaignLevel)LevelLibrary.GetLevel(5).CloneNew();
            var power=level.GetPowerUpsForRound(1).OfType<BlockMatchFoundationPower>().Single(p=>p.Name=="Custom Steady Clears");
            Assert.That(power.Minimum,Is.EqualTo(5));Assert.That(power.CreditPerTile,Is.EqualTo(3));
            Assert.That(((BlockMatchFoundationPower)power.Clone()).CreditPerTile,Is.EqualTo(3));
            Assert.That(BlockMatchFoundationPower.ApplyCredit(5,new[]{power}),Is.EqualTo(15));
            Assert.That(power.PowerUpDescription,Does.Contain("3 blocks"));
            var rules=level.GetMatchRules(1);power.ModifyMatchRules(rules);Assert.That(rules[0].minimumLength,Is.EqualTo(5));
        } finally {SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }

    [Test]
    public void AnimalMagnetsChangeRelativeOddsAndCombineWithoutDuplicateStacking()
    {
        var animals = new[] { ItemType.Duck, ItemType.Bear, ItemType.Chicken, ItemType.Hippo, ItemType.Sloth, ItemType.Giraffe, ItemType.Walrus };
        var active = new List<PowerUp> { new AnimalSpawnPower(ItemType.Duck) };
        var random = new System.Random(847);
        int ducks = 0, bears = 0;
        for (int n=0;n<40000;n++) {
            var picked = AnimalSpawnPower.Select(animals,random.NextDouble(),animal=>AnimalSpawnPower.Weight(animal,active));
            if (picked==ItemType.Duck) ducks++;
            if (picked==ItemType.Bear) bears++;
        }
        Assert.That(ducks/40000d, Is.InRange(.24,.26), "One magnet: 2 out of 8 total chances.");
        Assert.That(bears/40000d, Is.InRange(.115,.135));
        active.Add(new AnimalSpawnPower(ItemType.Bear));
        Assert.That(animals.Sum(animal=>AnimalSpawnPower.Weight(animal,active)), Is.EqualTo(9));
        Assert.That(AnimalSpawnPower.Weight(ItemType.Duck,active), Is.EqualTo(2));
        Assert.That(AnimalSpawnPower.Weight(ItemType.Bear,active), Is.EqualTo(2));
        Assert.That(new AnimalSpawnPower(ItemType.Duck).CanOfferWith(active), Is.False);
        Assert.That(new AnimalSpawnPower(ItemType.Chicken).CanOfferWith(active), Is.True);
        Assert.That(AnimalSpawnPower.Weight(ItemType.Duck,new PowerUp[0]), Is.EqualTo(1));
    }

    [Test]
    public void MagnetsProtectTheirAnimalAndRemainAvailableForBothGoals()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        foreach (ItemType animal in System.Enum.GetValues(typeof(ItemType))) {
            var magnet = new AnimalSpawnPower(animal);
            Assert.That(magnet.SupportsItems(new[] { animal }), Is.True);
            Assert.That(magnet.SupportsItems(new ItemType[0]), Is.False);
            var removal = new RosterTradeoffPower(animal,false);
            Assert.That(removal.CanOfferWith(new PowerUp[] { magnet }), Is.False);
            Assert.That(magnet.CanOfferWith(new PowerUp[] { removal }), Is.False);
            Assert.That(magnet.Clone().NativeContentKey, Is.EqualTo(magnet.NativeContentKey));
            Assert.That(PowerUpIconLibrary.Describe(magnet).Animal, Is.EqualTo(animal));
            foreach (var goal in new[] { GoalType.Score,GoalType.BlocksCleared })
            foreach (var mode in new[] { FreePlayMode.Timed,FreePlayMode.LimitedMoves }) {
                Assert.That(magnet.Supports(goal,mode), Is.True);
                var pool = goal==GoalType.Score ? PowerUpLibrary.GetAll() : PowerUpLibrary.BlockPowers();
                Assert.That(pool.OfType<AnimalSpawnPower>().Any(p=>p.Animal==animal), Is.True);
            }
        }
    }

    [Test]
    public void WeightedGenerationKeepsBoardsPlayableAndFavorsTheChosenAnimal()
    {
        var animals = new[] { ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Hippo,ItemType.Sloth,ItemType.Giraffe,ItemType.Walrus };
        int normalDucks=0,magnetDucks=0;
        for (int seed=0;seed<60;seed++) {
            var normal = new BoardModel(7,7);
            var weighted = new BoardModel(7,7);
            normal.Generate(animals,new System.Random(seed));
            weighted.Generate(animals,new System.Random(seed),animal=>animal==ItemType.Duck?2:1);
            Assert.That(weighted.Matches(), Is.Empty);
            Assert.That(weighted.HasLegalMove(), Is.True);
            for(int y=0;y<7;y++)for(int x=0;x<7;x++) {
                if(normal[x,y]==ItemType.Duck)normalDucks++;
                if(weighted[x,y]==ItemType.Duck)magnetDucks++;
                Assert.That(animals.Contains(weighted[x,y].Value), Is.True);
            }
        }
        Assert.That(magnetDucks, Is.GreaterThan(normalDucks*1.15));
        foreach (ReplacementStrategy gravity in System.Enum.GetValues(typeof(ReplacementStrategy))) {
            var board = new BoardModel(7,7);
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=ItemType.Bear;
            var moves=board.Refill(new[] { new BoardModel.Cell(3,3) },gravity,
                ()=>AnimalSpawnPower.Select(animals,.2,animal=>animal==ItemType.Duck?2:1));
            Assert.That(moves.Where(m=>m.IsNew).All(m=>m.Type==ItemType.Duck), Is.True);
        }
    }

    [Test]
    public void CascadeDampingMultipliesRatherThanErasesMagnetWeights()
    {
        var animals = new[] { ItemType.Duck,ItemType.Bear,ItemType.Chicken };
        var board = new BoardModel(4,1,new[] { new MatchRule(MatchType.Horizontal,4) });
        for(int x=0;x<4;x++)board[x,0]=ItemType.Bear;
        var moves=board.Refill(new[] { new BoardModel.Cell(3,0) },ReplacementStrategy.InPlace,()=>ItemType.Duck);
        var random=new System.Random(231);
        int ducks=0;
        for(int n=0;n<6000;n++) {
            board.ApplyCascadeDamping(moves,animals,1,random,animal=>animal==ItemType.Duck?2:1);
            if(board[3,0]==ItemType.Duck)ducks++;
        }
        Assert.That(ducks/6000d, Is.InRange(.54,.60), "Duck weight 2, match-making Bear weight 0.5, Chicken weight 1.");
    }

    [Test]
    public void ShuffleLevelsGuaranteeToolsAndPayoffsAndSupportWholeRunBuilds()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        foreach (int number in Enumerable.Range(20,4)) {
            var level = (CampaignLevel)LevelLibrary.GetLevel(number).CloneNew();
            Assert.That(level.Goal, Is.EqualTo(number == 23 ? GoalType.Score : GoalType.BlocksCleared));
            Assert.That(level.GetBossPowersMap().GetBossPowersForRound(6).Count, Is.EqualTo(1));
            for (int seed = 0; seed < 40; seed++) {
                var owned = new List<PowerUp>(); var names = new HashSet<string>();
                var animals = new List<ItemType> { ItemType.Duck, ItemType.Bear, ItemType.Chicken, ItemType.Hippo, ItemType.Giraffe, ItemType.Sloth, ItemType.Walrus };
                var random = new System.Random(seed);
                for (int round = 1; round <= 6; round++) {
                    var rules = level.GetMatchRules(round);
                    foreach (var power in owned) power.ModifyMatchRules(rules);
                    var definition = level.PowerOffers.ForRound(round);
                    var choices = definition == null ? PowerUpSelector.BuildChoices(level.GetPowerUpsForRound(round),names,animals,3,random,level.Goal,level.Mode,rules,owned,round:round)
                        : PowerOfferBuilder.Build(definition,PowerOfferBuilder.Catalog(),names,animals,random,level.Goal,level.Mode,rules,owned,round).Choices.ToList();
                    string context = $"Level {number}, round {round}, seed {seed}";
                    int expectedCount = 3;
                    Assert.That(choices.Count, Is.EqualTo(expectedCount), context);
                    Assert.That(choices.Select(p=>p.Name).Distinct().Count(), Is.EqualTo(expectedCount), context);
                    Assert.That(choices.All(p=>p.Supports(level.Goal,level.Mode) && p.CanUseWithMatchRules(rules) && p.SupportsItems(animals)), Is.True, context);
                    if (round == 2 || round == 3) Assert.That(choices.All(p=>p is ShufflePower), Is.True, context);
                    if (round == 3) Assert.That(choices.OfType<ShufflePower>().Any(p=>p.Style==(number==23?ShuffleStyle.Afterglow:ShuffleStyle.Salvage)), Is.True, context);
                    var picked = choices[(seed+round)%choices.Count]; owned.Add(picked); names.Add(picked.Name);
                    if (picked is RosterTradeoffPower roster) { if (roster.AddsAnimal) animals.Add(roster.Animal); else animals.Remove(roster.Animal); }
                }
            }
        }
    }

    [Test]
    public void RemixLevelsUseBlockGoalsAndKeepFullOffersAcrossWholeRuns()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        var expansion = Enumerable.Range(12, 8).Select(n => (CampaignLevel)LevelLibrary.GetLevel(n)).ToList();
        Assert.That(expansion.Count(l => l.Goal == GoalType.BlocksCleared), Is.EqualTo(6));
        Assert.That(expansion.Where(l => l.Goal == GoalType.BlocksCleared).Select(l => l.Mode).Distinct().Count(), Is.EqualTo(2));
        foreach (var original in expansion) {
            var level = (CampaignLevel)original.CloneNew();
            Assert.That(level.GetTotalRounds, Is.EqualTo(6));
            Assert.That(level.GetBossPowersMap().GetBossPowersForRound(6).Count, Is.EqualTo(1));
            Assert.That(level.Bonus, Is.EqualTo(original.Bonus));
            Assert.That(level.Replacement, Is.EqualTo(original.Replacement));
            for (int seed = 0; seed < 30; seed++) {
                var random = new System.Random(seed);
                var owned = new List<PowerUp>();
                var names = new HashSet<string>();
                var animals = new List<ItemType> { ItemType.Duck, ItemType.Bear, ItemType.Chicken, ItemType.Hippo, ItemType.Giraffe, ItemType.Sloth, ItemType.Walrus };
                for (int round = 1; round <= 6; round++) {
                    var rules = level.GetMatchRules(round);
                    foreach (var power in owned) power.ModifyMatchRules(rules);
                    var definition = level.PowerOffers.ForRound(round);
                    var choices = definition == null
                        ? PowerUpSelector.BuildChoices(level.GetPowerUpsForRound(round), names, animals, 3, random, level.Goal, level.Mode, rules, owned, round: round)
                        : PowerOfferBuilder.Build(definition, PowerOfferBuilder.Catalog(), names, animals, random, level.Goal, level.Mode, rules, owned, round).Choices.ToList();
                    string context = $"Level {level.GetLevelNumber}, round {round}, seed {seed}";
                    int expectedCount = 3;
                    Assert.That(choices.Count, Is.EqualTo(expectedCount), context);
                    Assert.That(choices.Select(p => p.Name).Distinct().Count(), Is.EqualTo(expectedCount), context);
                    Assert.That(choices.All(p => p.Supports(level.Goal, level.Mode) && p.CanUseWithMatchRules(rules) && p.SupportsItems(animals)), Is.True, context);
                    if (round == 1 && level.Goal == GoalType.BlocksCleared)
                        Assert.That(choices.All(p => p is not MatchFoundationPower), Is.True, context);
                    if (round > 1) Assert.That(choices.All(p => !p.OpeningOnly), Is.True, context);
                    var picked = choices[(seed + round) % choices.Count];
                    owned.Add(picked); names.Add(picked.Name);
                    if (picked is RosterTradeoffPower roster) {
                        if (roster.AddsAnimal) animals.Add(roster.Animal); else animals.Remove(roster.Animal);
                    }
                }
            }
        }
        var group = Enumerable.Range(0, 6).Select(x => new BoardModel.Cell(x, 0)).ToList();
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.LargeGroup, group, true), Is.EqualTo(150));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Cascade, group, false), Is.EqualTo(125));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Cascade, group, true), Is.EqualTo(100));
        var square = new[] { new BoardModel.Cell(0,0), new BoardModel.Cell(1,0), new BoardModel.Cell(0,1), new BoardModel.Cell(1,1) };
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Square, square, true), Is.EqualTo(125));
    }

    [Test]
    public void LeastMatchedBonusUsesPriorRoundCountsActiveAnimalsAndSharesTies()
    {
        var history=new RoundAnimalHistory();
        var pool=new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken};
        Assert.That(history.LeastMatched(pool),Is.EquivalentTo(pool));
        Assert.That(LevelBonuses.AnimalMultiplier(LevelBonus.LeastMatched,history,pool,new[]{ItemType.Duck}),Is.EqualTo(2));
        history.RecordAnimals(Enumerable.Repeat(ItemType.Duck,4));
        Assert.That(history.LeastMatched(pool),Is.EquivalentTo(new[]{ItemType.Bear,ItemType.Chicken}));
        Assert.That(LevelBonuses.AnimalMultiplier(LevelBonus.LeastMatched,history,pool,new[]{ItemType.Duck}),Is.EqualTo(1));
        Assert.That(LevelBonuses.AnimalMultiplier(LevelBonus.LeastMatched,history,pool,new[]{ItemType.Bear,ItemType.Chicken}),Is.EqualTo(2),"Tied animals grant one x2 factor, not x4.");
        history.RecordAnimals(Enumerable.Repeat(ItemType.Bear,4));
        Assert.That(history.LeastMatched(new[]{ItemType.Duck,ItemType.Bear}),Is.EquivalentTo(new[]{ItemType.Duck,ItemType.Bear}),"Removed animals do not keep the bonus.");
        Assert.That(history.LeastMatched(new[]{ItemType.Duck,ItemType.Bear,ItemType.Moose}),Is.EqualTo(new[]{ItemType.Moose}),"Added animals start at zero.");
        Assert.That(LevelBonuses.AnimalMultiplier(LevelBonus.None,history,pool,new[]{ItemType.Chicken}),Is.EqualTo(1));
        Assert.That(history.LeastMatched(new ItemType[0]),Is.Empty);
        history.Reset();Assert.That(history.LeastMatched(pool),Is.EquivalentTo(pool));
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        var level=(CampaignLevel)LevelLibrary.GetLevel(2);
        Assert.That(level.Bonus,Is.EqualTo(LevelBonus.LeastMatched));
        Assert.That(((CampaignLevel)level.CloneNew()).Bonus,Is.EqualTo(LevelBonus.LeastMatched));
        Assert.That(level.FinalBossStyle,Is.EqualTo(BossStyle.MostMatched));
    }
    [Test]
    public void RoundOverridesMixSpecificPowersAndFiltersAndSurviveCloning()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            var d=JObject.Parse(baseline);
            var four=d["powers"].Cast<JObject>().Single(p=>(string)p["nativeKey"]=="FourExactlyPower:Four is Good");
            var offer=JObject.Parse("{round:2,label:'Grow your build',slots:[{mode:'powers',count:1,repeats:'NewOnly',powerIds:[]},{mode:'filters',count:2,match:'all',filters:{growth:'include',opening:'exclude'},repeats:'PreferNew'}]}");
            ((JArray)offer["slots"][0]["powerIds"]).Add((string)four["id"]);
            d["levels"][6]["roundOffers"]=new JArray(offer);
            SharedGameContent.Load(d.ToString());LevelLibrary.ReloadSharedContent();
            var level=(CampaignLevel)LevelLibrary.GetLevel(7);
            var definition=((CampaignLevel)level.CloneNew()).PowerOffers.ForRound(2);
            var items=System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList();
            var result=PowerOfferBuilder.Build(definition,PowerOfferBuilder.Catalog(),new HashSet<string>(),items,new System.Random(8),GoalType.Score,FreePlayMode.Timed,level.GetMatchRules(2),new PowerUp[0],2);
            Assert.That(result.IsComplete,Is.True);
            Assert.That(result.Choices.Count,Is.EqualTo(3));
            Assert.That(result.Choices.Select(p=>p.Name).Distinct().Count(),Is.EqualTo(3));
            Assert.That(result.Choices.Count(p=>p is FourExactlyPower),Is.EqualTo(1));
            Assert.That(result.Choices.Where(p=>p is not FourExactlyPower).All(p=>SharedGameContent.MatchesOfferGroup((JObject)offer["slots"][1],p)),Is.True);
            Assert.That(result.Choices.All(p=>!p.OpeningOnly),Is.True);
            Assert.That(level.PowerOffers.ForRound(3).Label,Is.Not.EqualTo("Grow your build"));
            Assert.That(level.GetPowerUpsForRound(1).Count,Is.EqualTo(3));
            var shortOffer=PowerOfferBuilder.Build(definition,PowerOfferBuilder.Catalog(),new HashSet<string>{"Four is Good"},items,new System.Random(8),GoalType.Score,FreePlayMode.Timed,level.GetMatchRules(2),new PowerUp[0],2);
            Assert.That(shortOffer.IsComplete,Is.False);
            Assert.That(shortOffer.Choices.Count,Is.EqualTo(2));
        }finally{SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }
    [Test]
    public void RoundFilterModesAndExclusionsUseSharedFinderQualities()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            SharedGameContent.Load(baseline);var four=new FourExactlyPower();
            var group=JObject.Parse("{mode:'filters',count:1,match:'all',filters:{exact:'include',growth:'include'}}");
            Assert.That(SharedGameContent.MatchesOfferGroup(group,four),Is.False);
            group["match"]="any";
            Assert.That(SharedGameContent.MatchesOfferGroup(group,four),Is.True);
            group["filters"]["base"]="exclude";
            Assert.That(SharedGameContent.MatchesOfferGroup(group,four),Is.False);
            group["filters"]=JObject.Parse("{'tag:Scoring':'include'}");
            Assert.That(SharedGameContent.MatchesOfferGroup(group,four),Is.True);
            var d=JObject.Parse(baseline);
            var starter=d["powers"].Cast<JObject>().First(p=>(bool?)p["openingOnly"]==true);
            var invalid=JObject.Parse("{round:2,label:'Invalid',slots:[{mode:'powers',count:1,powerIds:[]}]}");
            ((JArray)invalid["slots"][0]["powerIds"]).Add((string)starter["id"]);
            d["levels"][6]["roundOffers"]=new JArray(invalid);
            Assert.Throws<System.ArgumentException>(()=>SharedGameContent.Load(d.ToString()));
            Assert.That(JToken.DeepEquals(SharedGameContent.Document["levels"][6]["roundOffers"],JObject.Parse(baseline)["levels"][6]["roundOffers"]),Is.True);
        }finally{SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }
    [Test]
    public void SharedCatalogCoversNativeHandlersAndRebuildsEveryLevel()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        SharedGameContent.ValidateCoverage();
        LevelLibrary.ReloadSharedContent();
        foreach(var row in SharedGameContent.Document["levels"].Cast<JObject>()) {
            var level=(CampaignLevel)LevelLibrary.GetLevel((int)row["number"]);
            Assert.That(level.Name,Is.EqualTo((string)row["name"]));
            Assert.That(level.GetWidth(1),Is.EqualTo((int)row["width"]));
            int authoredStarters=((JArray)row["starters"]).Count;
            if(authoredStarters>0)Assert.That(level.GetPowerUpsForRound(1).Count,Is.EqualTo(authoredStarters));
            else Assert.That(level.GetPowerUpsForRound(1),Is.Not.Empty,"Empty authored pools use the default catalog.");
            Assert.That(level.GetBossPowersMap(),Is.Not.Null);
        }
    }
    [Test]
    public void SharedTuningChangesNativeScoreWordingAndCloneTogether()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            var d=JObject.Parse(baseline);
            var row=d["powers"].Cast<JObject>().Single(p=>(string)p["nativeKey"]=="FourExactlyPower:Four is Good");
            row["parameters"]["basePointsMulti"]=7;row["name"]="Seven for Four";
            SharedGameContent.Load(d.ToString());var p=new FourExactlyPower();p.ConfigureSharedContent();
            var tiles=new List<Tile>{null,null,null,null};
            Assert.That(p.AdditiveMultiplierMod(tiles),Is.EqualTo(7));
            Assert.That(p.PowerUpDescription,Is.EqualTo("Exactly 4 tiles: +7 multiplier."));
            Assert.That(p.Clone().Name,Is.EqualTo("Seven for Four"));
            var seed=new BaseMultiPowerUp(1,2);seed.ConfigureSharedContent();
            Assert.That(seed.Clone().NativeContentKey,Is.EqualTo(seed.NativeContentKey));
        }finally{SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }
    [Test]
    public void StructuredGrowthAppliesOnlyAfterEarningAndSupportsSharedPools()
    {
        string baseline=File.ReadAllText("Assets/Resources/Content/prismatch.json");
        try {
            var d=JObject.Parse(baseline);
            var p=JObject.Parse("{id:'custom-power-test',source:'custom',name:'Earned Four',description:'Test',goals:'Points',trigger:'exact',animal:'Any',length:4,sequence:'4,5,6',reward:'base',value:3,unit:'match',permanent:true,threshold:2,consecutive:true}");
            ((JArray)d["powers"]).Add(p);((JArray)d["levels"][2]["rewards"]).Add((string)p["id"]);
            SharedGameContent.Load(d.ToString());var rule=new SharedRulePower(p);var four=new List<Tile>{null,null,null,null};
            Assert.That(rule.AdditiveBaseMod(four),Is.Zero);
            Assert.That(rule.AdditiveBaseMod(four),Is.Zero);
            Assert.That(rule.AdditiveBaseMod(new List<Tile>{null,null,null}),Is.EqualTo(3));
            Assert.That(SharedGameContent.Pool(3,2).Single(),Is.TypeOf<SharedRulePower>());
        }finally{SharedGameContent.Load(baseline);LevelLibrary.ReloadSharedContent();}
    }
}
