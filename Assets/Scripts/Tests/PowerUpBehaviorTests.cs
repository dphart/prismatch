using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

[TestFixture]
public class AnimalMasteryTests
{
    [Test]
    public void CascadesNeitherAdvanceMasteryNorReceiveItsEarnedBonus()
    {
        foreach(bool points in new[]{true,false}) {
            var power=new AnimalMasteryPower(ItemType.Duck,points);
            power.RecordMatch(true,false);power.RecordMatch(true,false);
            power.RecordMatch(true,false);
            Assert.That(power.EarnedBonus,Is.EqualTo(1));
            Assert.That(power.Progress,Is.EqualTo(1));
            Assert.That(power.RecordMatch(true,false,playerMatch:false),Is.Zero);
            Assert.That(power.RecordMatch(false,false,playerMatch:false),Is.Zero);
            Assert.That(power.EarnedBonus,Is.EqualTo(1));
            Assert.That(power.Progress,Is.EqualTo(1));
            Assert.That(power.RecordMatch(true,false),Is.EqualTo(1));
            Assert.That(power.EarnedBonus,Is.EqualTo(2));
        }
    }

    [Test]
    public void TwoGroupsRewardFutureMatchesOfOnlyTheTarget()
    {
        var power=new AnimalMasteryPower(ItemType.Duck,true);
        for(int i=0;i<2;i++)Assert.That(power.RecordMatch(true,false),Is.Zero);
        Assert.That(power.EarnedBonus,Is.EqualTo(1));
        Assert.That(power.Progress,Is.Zero);
        Assert.That(power.RecordMatch(false,false),Is.Zero);
        Assert.That(power.Progress,Is.Zero);
        Assert.That(power.RecordMatch(true,false),Is.EqualTo(1));
        power.OnRoundStarted();
        Assert.That(power.EarnedBonus,Is.EqualTo(1));
        Assert.That(power.Progress,Is.EqualTo(1));
        Assert.That(((AnimalMasteryPower)power.Clone()).EarnedBonus,Is.Zero);
        Assert.That(power.FlatBasePerTile,Is.Zero,"Animal bonuses must not appear as global base.");
    }
    [Test]
    public void RivalryGainsEveryMatchAndRemovesBonusWithoutDebt()
    {
        var power=new AnimalMasteryPower(ItemType.Duck,false,ItemType.Bear);
        for(int i=0;i<2;i++)power.RecordMatch(true,false);
        Assert.That(power.EarnedBonus,Is.EqualTo(2));
        power.RecordMatch(false,true);
        Assert.That(power.EarnedBonus,Is.EqualTo(1));
        Assert.That(power.Progress,Is.Zero);
        power.RecordMatch(false,true);power.RecordMatch(false,true);
        Assert.That(power.EarnedBonus,Is.Zero);
        Assert.That(power.SupportsItems(new List<ItemType>{ItemType.Duck}),Is.False);
        Assert.That(power.SupportsItems(new List<ItemType>{ItemType.Duck,ItemType.Bear}),Is.True);
        power.Apply(null);
        Assert.That(power.Progress,Is.Zero);
    }
}

[TestFixture]
public class SharedPowerRewardTests
{
    [Test]
    public void StackedOddsAddsFortyBaseOnlyForExactlyFourTiles()
    {
        var power = new StackedOddsPower();
        Assert.That(power.AdditiveBaseMod(new List<Tile> { null, null, null }), Is.Zero);
        Assert.That(power.AdditiveBaseMod(new List<Tile> { null, null, null, null }), Is.EqualTo(40));
        Assert.That(power.AdditiveBaseMod(new List<Tile> { null, null, null, null, null }), Is.Zero);
        Assert.That(power.CanUseWithMatchRules(new[] { new MatchRule(MatchType.Snake, 5) }), Is.False);
        Assert.That(power.CanUseWithMatchRules(new[] { new MatchRule(MatchType.Snake, 4) }), Is.True);
    }

    [Test]
    public void BlockClearingSlotsOfferSharedPowersWithoutMutatingTheSource()
    {
        var source=new IconTypeGivesMulti(ItemType.Duck,2);
        var offer=new PowerOfferDefinition("Clear",new[]{new PowerOfferSlot(new PowerTagQuery(PowerTags.Clearing),1)});
        var result=PowerOfferBuilder.Build(offer,new[]{source},new HashSet<string>(),new List<ItemType>{ItemType.Duck},
            new System.Random(1),GoalType.BlocksCleared,FreePlayMode.LimitedMoves,
            new List<MatchRule>{new(MatchType.Snake,4)},new List<PowerUp>(),2);
        Assert.That(result.IsComplete,Is.True);
        Assert.That(result.Choices.Single().UsesBlockReward,Is.True);
        Assert.That(source.RewardGoal,Is.EqualTo(GoalType.Score));
        Assert.That(source.OfferTags(FreePlayMode.LimitedMoves)&PowerTags.Clearing,Is.EqualTo(PowerTags.None));
        Assert.That(result.Choices.Single().Clone().UsesBlockReward,Is.True);
    }

    [Test]
    public void SharedCatalogSupportsBothGoalsAndBudgetsWithoutChangingPointsHooks()
    {
        var power=new FourExactlyPower();
        foreach(var goal in new[]{GoalType.Score,GoalType.BlocksCleared})
        foreach(var mode in new[]{FreePlayMode.Timed,FreePlayMode.LimitedMoves})
            Assert.That(power.Supports(goal,mode),Is.True);
        Assert.That(power.AdditiveBaseMod(new List<Tile>{null,null,null,null}),Is.Zero);
        Assert.That(power.AdditiveMultiplierMod(new List<Tile>{null,null,null,null}),Is.EqualTo(5));
        power.ForGoal(GoalType.BlocksCleared);
        Assert.That(power.Clone().RewardGoal,Is.EqualTo(GoalType.BlocksCleared));
        Assert.That(power.AdditiveBaseMod(new List<Tile>{null,null,null,null}),Is.Zero);
        Assert.That(power.AdditiveMultiplierMod(new List<Tile>{null,null,null,null}),Is.EqualTo(5));
        Assert.That(new RosterTradeoffPower(ItemType.Duck,true).SupportedGoals,Is.EqualTo(PowerGoals.Both));
    }

    [Test]
    public void BaseRewardsDistinguishWholeMatchAndPerTileSources()
    {
        var animal=new IconTypeGivesBase(ItemType.Duck,5);
        var whole=new StackedOddsPower();var result=new ScoreResult();
        SharedPowerRewards.Resolve(animal,4,true,20,0,1,1,result);
        SharedPowerRewards.Resolve(whole,4,true,2,0,1,1,result);
        Assert.That(result.BlockCredits[animal],Is.EqualTo(5));
        Assert.That(result.BlockCredits[whole],Is.EqualTo(2));
        Assert.That(result.ExtraClears,Is.Empty);
    }

    [Test]
    public void AnimalGrowthRewardIsScopedToItsAnimalAndUsesEarnedAmount()
    {
        var growth=new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck,2,10,0);
        var other=new ScoreResult();SharedPowerRewards.Resolve(growth,4,false,16,0,1,1,other);
        Assert.That(other.BlockCredits,Is.Empty);
        var own=new ScoreResult();SharedPowerRewards.Resolve(growth,4,true,16,0,1,1,own);
        Assert.That(own.BlockCredits[growth],Is.EqualTo(4),"Two upgrades add four per group, independent of group size.");
    }

    [Test]
    public void FractionalClearsAccumulateAndClonesStartFresh()
    {
        var power=new HabitatPower(HabitatStyle.Shore,2);
        var first=new ScoreResult();SharedPowerRewards.Resolve(power,4,true,0,.5f,1,1,first);
        Assert.That(first.ExtraClears,Is.Empty);
        var second=new ScoreResult();SharedPowerRewards.Resolve(power,4,true,0,.5f,1,1,second);
        Assert.That(second.ExtraClears[power],Is.EqualTo(1));
        var clone=power.Clone();var fresh=new ScoreResult();SharedPowerRewards.Resolve(clone,4,true,0,.5f,1,1,fresh);
        Assert.That(fresh.ExtraClears,Is.Empty);
    }

    [Test]
    public void QueuedClearsApplyOnceToTheFollowingMatch()
    {
        var power=new TwinShotPower();var first=new ScoreResult();
        SharedPowerRewards.Resolve(power,4,true,0,0,1,2,first);Assert.That(first.ExtraClears,Is.Empty);
        var second=new ScoreResult();SharedPowerRewards.Resolve(power,4,true,0,0,1,1,second);
        Assert.That(second.ExtraClears[power],Is.EqualTo(1));
        var third=new ScoreResult();SharedPowerRewards.Resolve(power,4,true,0,0,1,1,third);
        Assert.That(third.ExtraClears,Is.Empty);
    }

    [Test]
    public void ExtraClearsAreDistinctNeighborsAndDoNotRetrigger()
    {
        var board=new BoardModel(4,4,new List<MatchRule>{new(MatchType.Snake,4)});
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)board[x,y]=ItemType.Duck;
        var group=new List<BoardModel.Cell>{new(1,1),new(2,1),new(1,2),new(2,2)};
        var cleared=new List<BoardModel.Cell>(group);
        Assert.That(SharedPowerRewards.ExpandClears(board,group,cleared,2),Is.EqualTo(2));
        Assert.That(cleared.Distinct().Count(),Is.EqualTo(6));
        Assert.That(SharedPowerRewards.ExpandClears(board,group,cleared,100),Is.EqualTo(6));
        Assert.That(cleared.Count,Is.EqualTo(12),"Only the eight direct neighbors can be added.");
    }
}

[TestFixture]
public class EdgeBalancePowerTests
{
    private static BoardModel.Cell C(int x,int y)=>new(x,y);
    [Test]
    public void BankStartsAtZeroAndNeverAccumulatesNegativeDebt()
    {
        var power=new EdgeBalancePower();
        var inside=new[]{C(1,1),C(2,1),C(3,1),C(4,1)};
        for(int i=0;i<3;i++)Assert.That(power.ResolveMatch(inside,7,7),Is.Zero);
        Assert.That(power.StoredBase,Is.Zero);
        Assert.That(power.ResolveMatch(new[]{C(0,1),C(0,2),C(1,1)},7,7),Is.Zero);
        Assert.That(power.StoredBase,Is.EqualTo(1),"Interior matches at zero do not leave a debt.");
        Assert.That(power.ResolveMatch(inside,7,7),Is.EqualTo(1),"This match uses the previously stored bonus.");
        Assert.That(power.StoredBase,Is.Zero);
    }
    [Test]
    public void MixedMatchUsesNetChangeAndCountsCornersOnceOnRectangularBoards()
    {
        var power=new EdgeBalancePower();
        var group=new[]{C(0,0),C(5,3),C(5,1),C(2,2)};
        power.ResolveMatch(group,6,4);Assert.That(power.StoredBase,Is.EqualTo(2));
        var reversed=new EdgeBalancePower();reversed.ResolveMatch(group.Reverse(),6,4);
        Assert.That(reversed.StoredBase,Is.EqualTo(2));
        power.ResolveMatch(new[]{C(2,2),C(3,2),C(1,1),C(4,1)},6,4);
        Assert.That(power.StoredBase,Is.Zero);
        power.ResolveMatch(new[]{C(0,0),C(0,0)},6,4);Assert.That(power.StoredBase,Is.EqualTo(1));
    }
    [Test]
    public void BankPersistsAcrossRoundsButFreshAcquisitionAndCloneStartAtZero()
    {
        var power=new EdgeBalancePower();power.ResolveMatch(new[]{C(0,0),C(1,0)},7,7);
        power.OnRoundStarted();Assert.That(power.FlatBasePerTile,Is.EqualTo(2));
        Assert.That(power.Clone().FlatBasePerTile,Is.Zero);
        var activation=PowerActivation.FromScoring(power,2,0,0,1,1,0,0,1);
        Assert.That(activation.Effect,Does.Contain("+2 base"));
        power.ResolveMatch(new[]{C(1,1),C(2,1)},7,7);
        activation=PowerActivation.FromScoring(power,2,4,0,1,1,2,0,1);
        Assert.That(activation.Effect,Does.Contain("-2 base"));
        power.Apply(null);Assert.That(power.StoredBase,Is.Zero);
        Assert.That(power.SupportedGoals,Is.EqualTo(PowerGoals.Both));
        Assert.That(PowerUpLibrary.GetAll().Any(p=>p is EdgeBalancePower),Is.True);
    }
}

[TestFixture]
public class BlockCollectionTests
{
    private static List<BlockMatchCell> Group(ItemType animal, params int[] xy)
    {
        var result = new List<BlockMatchCell>();
        for (int i = 0; i < xy.Length; i += 2) result.Add(new BlockMatchCell(xy[i], xy[i + 1], animal));
        return result;
    }
    [Test]
    public void EveryAnimalCanBeTheGoalAndAdditionalAnimalsDoNotReplaceIt()
    {
        foreach (ItemType animal in System.Enum.GetValues(typeof(ItemType)))
        {
            var other = animal == ItemType.Elephant ? ItemType.Duck : ItemType.Elephant;
            var objective = new BlockObjective(animal);
            var original = Group(animal, 0,0,1,0,2,0);
            var extra = Group(other, 0,0,1,0,2,0);
            Assert.That(objective.Credit(original, new PowerUp[0]), Is.EqualTo(3));
            Assert.That(objective.Credit(extra, new PowerUp[0]), Is.Zero);
            var powers = new PowerUp[] { new CollectionPower(CollectionStyle.Admission, other) };
            Assert.That(objective.Credit(extra, powers), Is.EqualTo(3));
            Assert.That(objective.Credit(original, powers), Is.EqualTo(3));
            Assert.That(objective.Credit(extra, new[] { new CollectionPower(CollectionStyle.AnimalCredit, other) }), Is.Zero,
                "A credit booster does not admit a different animal.");
        }
    }
    [Test]
    public void CreditBonusesStackButBonusClearsOnlyReceiveTheirOwnEligibleCredit()
    {
        var objective = new BlockObjective(ItemType.Duck);
        var ducks = Group(ItemType.Duck, 0,0,1,0,2,0);
        var power = new CollectionPower(CollectionStyle.AnimalCredit, ItemType.Duck);
        Assert.That(objective.Credit(ducks, new[] { power }), Is.EqualTo(6));
        Assert.That(objective.Credit(ducks.Concat(ducks), new[] { power }), Is.EqualTo(6), "Cells are deduplicated.");
        Assert.That(objective.Credit(ducks, new[] { power, power.Clone() }), Is.EqualTo(9));
        Assert.That(objective.Credit(ducks, new[] { power }, bonusClear: true), Is.EqualTo(3));
        Assert.That(objective.Credit(Group(ItemType.Bear, 0,0), new[] { power }, bonusClear: true), Is.Zero);
    }
    [Test]
    public void ShapesUseActualGeometryAndAdmissionDoesNotBypassThem()
    {
        var square = Group(ItemType.Elephant, 0,0,1,0,0,1,1,1);
        var line = Group(ItemType.Elephant, 0,0,1,0,2,0,3,0);
        var objective = new BlockObjective(ItemType.Duck, BlockShape.Square);
        var powers = new[] { new CollectionPower(CollectionStyle.Admission, ItemType.Elephant),
            new CollectionPower(CollectionStyle.ShapeCredit, shape: BlockShape.Square) };
        Assert.That(objective.Credit(square, powers), Is.EqualTo(8));
        Assert.That(objective.Credit(line, powers), Is.Zero);
        Assert.That(objective.Credit(square, powers, bonusClear: true), Is.Zero, "Bonus clears cannot satisfy match shapes.");
        var plus = Group(ItemType.Bear, 1,1,0,1,2,1,1,0,1,2);
        Assert.That(BlockObjective.HasShape(plus.Select(c => c.Cell), BlockShape.Plus), Is.True);
        Assert.That(BlockObjective.HasShape(square.Select(c => c.Cell), BlockShape.Plus), Is.False);
        Assert.That(BlockObjective.HasShape(line.Select(c => c.Cell), BlockShape.Horizontal), Is.True);
        Assert.That(BlockObjective.HasShape(line.Select(c => c.Cell), BlockShape.Vertical), Is.False);
        Assert.That(BlockObjective.HasShape(Group(ItemType.Bear, 1,0,1,1,1,2).Select(c => c.Cell), BlockShape.Vertical), Is.True);
        Assert.That(BlockObjective.HasShape(line.Select(c => c.Cell), BlockShape.Large), Is.False);
        Assert.That(BlockObjective.HasShape(Group(ItemType.Bear, 0,0,1,0,2,0,3,0,4,0,5,0).Select(c => c.Cell), BlockShape.Large), Is.True);
    }
    [Test]
    public void TransformationsAreBoundedSkipAllClearsAndNeverTriggerFromCascades()
    {
        foreach (ItemType animal in System.Enum.GetValues(typeof(ItemType)))
        {
            var board = new BoardModel(5, 5);
            var original = animal == ItemType.Bear ? ItemType.Duck : ItemType.Bear;
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++) board[x,y] = original;
            var group = new List<BoardModel.Cell> { new(1,1), new(2,1), new(3,1) };
            var clearing = group.Concat(new[] { new BoardModel.Cell(0,1) }).ToList();
            var powers = new[] { new CollectionPower(CollectionStyle.Transform, animal) };
            Assert.That(CollectionPower.TransformNeighbors(board, new[] { group }, clearing, powers, false), Is.Empty);
            var changed = CollectionPower.TransformNeighbors(board, new[] { group }, clearing, powers, true);
            Assert.That(changed.Count, Is.EqualTo(2));
            Assert.That(changed.Any(c => clearing.Contains(c)), Is.False);
            Assert.That(changed.All(c => board[c.X,c.Y] == animal), Is.True);
            Assert.That(clearing.All(c => board[c.X,c.Y] == original), Is.True);
        }
    }
    [Test]
    public void CollectionCatalogFiltersFocusAndKeepsAllAnimalAdmissionsAvailable()
    {
        var objective = new BlockObjective(ItemType.Duck);
        var catalog = BlockObjective.CatalogFor(objective).OfType<CollectionPower>().ToList();
        Assert.That(catalog.Count(p => p.Style == CollectionStyle.Admission), Is.EqualTo(System.Enum.GetValues(typeof(ItemType)).Length - 1));
        Assert.That(catalog.Where(p => p.Style != CollectionStyle.Admission).All(p => p.Animal == ItemType.Duck), Is.True);
        var admission = catalog.Single(p => p.Style == CollectionStyle.Admission && p.Animal == ItemType.Elephant);
        Assert.That(admission.SupportsItems(new List<ItemType> { ItemType.Duck }), Is.True, "Admission may introduce a new refill animal.");
        Assert.That(admission.CanOfferWith(new[] { admission }), Is.False);
        Assert.That(admission.Clone().Name, Is.EqualTo(admission.Name));
        Assert.That(BlockObjective.CatalogFor(objective, new[] { admission }).OfType<CollectionPower>().Any(p =>
            p.Style == CollectionStyle.AnimalCredit && p.Animal == ItemType.Elephant), Is.True, "The build can pivot toward an admitted animal.");
        Assert.That(new RosterTradeoffPower(ItemType.Elephant, false).CanOfferWith(new[] { admission }), Is.False);
    }
    [Test]
    public void ArchivedCollectionDesignsVaryByRoundAndSurviveCloning()
    {
        var animals = new HashSet<ItemType>();
        foreach (CampaignLevel level in CampaignExpansion.Create().Select(seed => CampaignIdentity.Create((CampaignLevel)seed)).Where(l => l.Goal == GoalType.BlocksCleared))
        {
            if (level.BlockObjectives == null) continue; // Clear Commitments deliberately counts every animal/shape.
            Assert.That(level.BlockObjectives, Is.Not.Null);
            var clone = (CampaignLevel)level.CloneNew();
            bool hasShape = false;
            for (int round = 1; round <= level.GetTotalRounds; round++)
            {
                var objective = level.BlockObjectives.ForRound(round);
                if (objective.Animal.HasValue) animals.Add(objective.Animal.Value);
                hasShape |= objective.Shape != BlockShape.Any;
                Assert.That(objective.Supports(level.GetMatchRules(round)), Is.True);
                Assert.That(clone.BlockObjectives.ForRound(round).Label, Is.EqualTo(objective.Label));
                Assert.That(level.PowerOffers.ForRound(round), Is.Not.Null);
            }
            Assert.That(hasShape, Is.True, level.Name);
            Assert.That(level.Settings.GrowthPercent, Is.EqualTo(level.Bonus == LevelBonus.None ? 25 : 35));
        }
        Assert.That(animals.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(animals, Does.Contain(ItemType.Elephant));
    }
    [Test]
    public void ObjectiveAuthoringCopiesInputAndRejectsImpossibleRules()
    {
        var source = new Dictionary<int, BlockObjective> { [1] = new(ItemType.Elephant) };
        var objectives = new BlockObjectives(source);
        source.Clear();
        Assert.That(objectives.ForRound(1).Animal, Is.EqualTo(ItemType.Elephant));
        Assert.That(objectives.ForRound(2), Is.SameAs(BlockObjective.Any));
        Assert.Throws<System.ArgumentException>(() => new CampaignLevel(90, "Wrong goal", "", new FreePlayConfig(), blockObjectives: objectives));
        Assert.Throws<System.ArgumentException>(() => new CampaignLevel(90, "Wrong shape", "",
            new FreePlayConfig(MatchType.Horizontal, goal: GoalType.BlocksCleared), blockObjectives:
            new BlockObjectives(new Dictionary<int, BlockObjective> { [1] = new(shape: BlockShape.Square) })));
        Assert.Throws<System.ArgumentException>(() => new CampaignLevel(90, "Wrong round", "",
            new FreePlayConfig(goal: GoalType.BlocksCleared, levels: 1), blockObjectives:
            new BlockObjectives(new Dictionary<int, BlockObjective> { [2] = new(ItemType.Duck) })));
    }
}

[TestFixture]
public class CampaignBossRosterTests
{
    private sealed class HabitatMatch : MatchInfo
    {
        public bool Player = true;
        public bool wasUserInitiatedMatch() => Player;
    }
    [Test]
    public void HabitatRewardsScaleAndGrowthSurvivesRoundsButNotClones()
    {
        var group = new List<Tile> { null, null, null, null, null, null };
        var context = new HabitatMatch();
        var bounty = new HabitatPower(HabitatStyle.Giants, 0) { MatchInfo = context };
        var amplifier = new HabitatPower(HabitatStyle.Giants, 1) { MatchInfo = context };
        var engine = new HabitatPower(HabitatStyle.Giants, 2) { MatchInfo = context };
        Assert.That(bounty.AdditiveBaseMod(group), Is.EqualTo(60));
        Assert.That(amplifier.AdditiveBaseMod(group), Is.Zero);
        Assert.That(amplifier.FractionalMultiplierMod(group), Is.EqualTo(5));
        Assert.That(engine.AdditiveBaseMod(group), Is.Zero);
        Assert.That(engine.FractionalMultiplierMod(group), Is.EqualTo(.5f));
        context.Player = false;
        Assert.That(engine.FractionalMultiplierMod(group), Is.EqualTo(.5f));
        engine.OnRoundStarted(); context.Player = true;
        Assert.That(engine.FractionalMultiplierMod(group), Is.EqualTo(1));
        var clone = (HabitatPower)engine.Clone(); clone.MatchInfo = context;
        Assert.That(clone.FractionalMultiplierMod(group), Is.EqualTo(.5f));
        engine.Apply(null);
        Assert.That(engine.FlatMultiplierBonus, Is.Zero);
        group.RemoveAt(0);
        Assert.That(bounty.AdditiveBaseMod(group), Is.Zero);
        Assert.That(amplifier.FractionalMultiplierMod(group), Is.Zero);
        var rapids = new HabitatPower(HabitatStyle.Rapids, 2) { MatchInfo = context };
        Assert.That(rapids.FractionalMultiplierMod(group), Is.Zero);
        context.Player = false;
        Assert.That(rapids.FractionalMultiplierMod(group), Is.EqualTo(.5f));
        Assert.That(rapids.MultiplierCondition, Does.Not.Contain("player matches only"));
    }
    private static List<BoardModel.Cell> Cells(params int[] xy)
    {
        var result = new List<BoardModel.Cell>();
        for (int i = 0; i < xy.Length; i += 2) result.Add(new BoardModel.Cell(xy[i], xy[i + 1]));
        return result;
    }
    private static int Reward(BossStyle style, List<BoardModel.Cell> cells, int move = 1,
        bool player = true, bool both = false) => ChallengeBoss.RewardPercent(style, cells, 7, 7, move, player, both);

    [Test]
    public void EveryCampaignLevelPreservesFinalBossAndPersistentRulesInClones()
    {
        var names = new HashSet<string>();
        foreach (CampaignLevel level in LevelLibrary.AllLevels)
        {
            var bosses = level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds);
            Assert.That(bosses.Count, Is.EqualTo(level.RoundRule.HasValue ? 2 : 1), level.Name);
            names.Add(bosses[0].Name);
            Assert.That(level.CloneNew().GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds)[0].Name, Is.EqualTo(bosses[0].Name));
            for (int round = 1; round < level.GetTotalRounds; round++)
                Assert.That(level.GetBossPowersMap().GetBossPowersForRound(round).Count, Is.EqualTo(level.RoundRule.HasValue ? 1 : 0));
            if (bosses[0] is ChallengeBoss challenge)
            {
                Assert.That(challenge.Goal, Is.EqualTo(level.Goal));
                if (challenge.Style == BossStyle.BoxTroll) Assert.That(level.Settings.Matching, Is.EqualTo(MatchType.Snake));
            }
        }
        Assert.That(names.Count, Is.GreaterThanOrEqualTo(3));
        var custom = new CampaignLevel(99, "Custom", "", new FreePlayConfig(), finalBossStyle: BossStyle.Seesaw);
        Assert.That(((CampaignLevel)custom.CloneNew()).FinalBossStyle, Is.EqualTo(BossStyle.Seesaw));
    }

    [Test]
    public void BossRulesPenalizeMissingPatternsAndKeepQualifyingMatchesAtFullValue()
    {
        var top=Cells(2,0,3,0,4,0,5,0); var middle=Cells(2,2,3,2,4,2,5,2);
        Assert.That(Reward(BossStyle.Lighthouse,top,1),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Lighthouse,top,2),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Lighthouse,top,5),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Seesaw,top,2),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Seesaw,Cells(2,6,3,6),2),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Wallflower,middle),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Wallflower,Cells(0,0,1,0,1,1,1,2)),Is.EqualTo(75),"Two edge tiles are insufficient.");
        Assert.That(Reward(BossStyle.Wallflower,Cells(0,0,1,0,2,0,2,1)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Wallflower,Cells(0,0,0,0,1,0,1,1)),Is.EqualTo(75),"Duplicate cells cannot inflate the edge count.");
        Assert.That(Reward(BossStyle.CenterStage,middle),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.CenterStage,Cells(1,1,2,1,3,1,3,2,2,2)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.BoxTroll,Cells(1,1,2,1,1,2,2,2)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.BoxTroll,top),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Stretch,middle),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Stretch,Cells(1,1,2,1,3,1)),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Oddball,Cells(1,1,2,1,3,1,3,2,2,2)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.EvenSteven,Cells(0,0,1,0,2,0,3,0,4,0,5,0)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.CascadeSurfer,top,player:false),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.CascadeSurfer,top),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.OpeningAct,Cells(0,0,1,0,2,0,3,0,4,0,5,0),3),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.OpeningAct,top,3),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.CornerCollector,Cells(0,0,1,0,2,0,3,0,4,0,5,0,6,0)),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.CornerCollector,Cells(0,0)),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.TwinKeeper,top,both:true),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.TwinKeeper,top,player:false,both:true),Is.EqualTo(100));
        Assert.That(ChallengeBoss.ScaleReward(5,500),Is.EqualTo(25));
        Assert.That(ChallengeBoss.ScaleReward(int.MaxValue,500),Is.EqualTo(int.MaxValue));
        Assert.That(ChallengeBoss.ScaleReward(5,75),Is.EqualTo(4),"Quarter penalties round the remaining reward up.");
        Assert.That(ChallengeBoss.ScaleReward(0,75),Is.Zero);
        Assert.That(ChallengeBoss.ScaleReward(int.MaxValue,75),Is.EqualTo(int.MaxValue-int.MaxValue/4));
    }

    [Test]
    public void BossPenaltiesRequireLargeGroupsOrSpecificCascades()
    {
        var small=Cells(0,0,1,0,2,0,3,0); var large=Cells(0,0,1,0,2,0,3,0,4,0,5,0);
        Assert.That(Reward(BossStyle.TrophyHunter,small),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.TrophyHunter,large),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.CascadeTax,small,player:false),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.CascadeTax,small),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.CascadeTax,large,player:false),Is.EqualTo(75));
        var eight=Cells(0,0,1,0,2,0,3,0,4,0,5,0,6,0,6,1);
        Assert.That(Reward(BossStyle.Jackpot,eight,2),Is.EqualTo(75));
        Assert.That(Reward(BossStyle.Jackpot,eight,3),Is.EqualTo(100));
        Assert.That(Reward(BossStyle.Jackpot,large,3),Is.EqualTo(75));
    }

    [Test]
    public void BossPercentagesNeverBoostRewards()
    {
        foreach(BossStyle style in System.Enum.GetValues(typeof(BossStyle)))
        foreach(var cells in new[] { Cells(0,0),Cells(2,2,3,2,4,2),Cells(1,1,2,1,1,2,2,2),Cells(0,0,1,0,2,0,3,0,4,0,5,0) })
        for(int move=1;move<=12;move++)
        foreach(bool player in new[] { true,false })
        foreach(bool both in new[] { true,false })
            Assert.That(Reward(style,cells,move,player,both),Is.InRange(75,100),style.ToString());
    }

    [Test]
    public void LevelBonusesRewardTheirPatternAndRaiseGrowthAndSurviveClones()
    {
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Cascade, Cells(0,0,1,0,2,0,3,0), false), Is.EqualTo(125));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Cascade, Cells(0,0,1,0,2,0,3,0), true), Is.EqualTo(100));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.LargeGroup, Cells(0,0,1,0,2,0,3,0,4,0,5,0), true), Is.EqualTo(150));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Square, Cells(1,1,2,1,1,2,2,2), true), Is.EqualTo(125));
        Assert.That(LevelBonuses.RewardPercent(LevelBonus.Square, Cells(0,0,1,0,2,0,3,0), true), Is.EqualTo(100));
        foreach (CampaignLevel level in LevelLibrary.AllLevels)
        {
            Assert.That(((CampaignLevel)level.CloneNew()).Bonus, Is.EqualTo(level.Bonus));
            if (level.IsTutorial) continue;
            bool premium = level.PowerOffers.ForRound(2).Label.Contains("PREMIUM ENGINE");
            int baseline = level.GetLevelNumber == 2 ? 60 : level.Goal == GoalType.BlocksCleared ? 25 : premium ? 85 : 55 + level.GetLevelNumber % 3 * 5;
            int extra = level.LightningLines ? 15 : level.Bonus == LevelBonus.None ? 0 : level.Goal == GoalType.Score ? 15 : 10;
            Assert.That(level.Settings.GrowthPercent, Is.EqualTo(baseline + extra));
            if (level.Bonus != LevelBonus.None) Assert.That(level.Description, Does.Contain("LEVEL BONUS:"));
        }
    }

    [Test]
    public void HabitatClearsRespectGeometryAndDoNotConsumeImaginaryBlocks()
    {
        var board = new BoardModel(7, 7, new List<MatchRule> { new(MatchType.Snake, 3) });
        var square = Cells(2,2,3,2,2,3,3,3);
        Assert.That(new BlockClearPower(BlockClearStyle.Square).ExtraClears(board, square, false), Is.EqualTo(2));
        Assert.That(new BlockClearPower(BlockClearStyle.Edge).ExtraClears(board, square, false), Is.Zero);
        Assert.That(new BlockClearPower(BlockClearStyle.LongLine).ExtraClears(board, square, false), Is.Zero);
        var edge = Cells(0,0,1,0,2,0,3,0,4,0,5,0);
        Assert.That(new BlockClearPower(BlockClearStyle.Edge).ExtraClears(board, edge, false), Is.EqualTo(2));
        Assert.That(new BlockClearPower(BlockClearStyle.LongLine).ExtraClears(board, edge, false), Is.EqualTo(2));
        foreach (var cell in edge) board[cell.X, cell.Y] = ItemType.Bear;
        Assert.That(BlockClearPower.ExpandClears(board, new List<List<BoardModel.Cell>> { edge },
            new[] { new BlockClearPower(BlockClearStyle.LongLine) }, false).Count, Is.EqualTo(6));
    }
}

[TestFixture]
public class PowerOfferDefinitionTests
{
    [Test]
    public void StarterPowersAreOpeningOnlyAcrossScheduledAndFallbackOffersUnlessLevelOptsIn()
    {
        var rules = new List<MatchRule> { new(MatchType.Snake,4) };
        var starter = new TacticalStarter(TacticalStyle.Large);
        var active = new List<PowerUp> { starter };
        var offer = new PowerOfferDefinition("Starter exception",new[] { new PowerOfferSlot(new PowerTagQuery(all:PowerTags.Starter)) });
        Assert.That(PowerOfferBuilder.IsEligible(starter,Roster,GoalType.Score,FreePlayMode.Timed,rules,new PowerUp[0],1), Is.True);
        Assert.That(PowerOfferBuilder.IsEligible(starter,Roster,GoalType.Score,FreePlayMode.Timed,rules,new PowerUp[0],2), Is.False);
        Assert.That(PowerOfferBuilder.IsEligible(starter,Roster,GoalType.Score,FreePlayMode.Timed,rules,active,1), Is.False);
        var normal = PowerOfferBuilder.Build(offer,new[] {starter},new HashSet<string>(),Roster,new System.Random(1),
            GoalType.Score,FreePlayMode.Timed,rules,active,round:2);
        Assert.That(normal.Choices, Is.Empty);
        var special = PowerOfferBuilder.Build(offer,new[] {starter},new HashSet<string>(),Roster,new System.Random(1),
            GoalType.Score,FreePlayMode.Timed,rules,active,round:2,allowStarterRewards:true);
        Assert.That(special.Choices.Count, Is.EqualTo(1));
        var fallback = PowerUpSelector.BuildChoices(new PowerUp[0],new HashSet<string>(),Roster,3,new System.Random(1),
            GoalType.Score,FreePlayMode.Timed,rules,active,round:2);
        Assert.That(fallback.Count, Is.EqualTo(3));
        Assert.That(fallback.All(power => !power.OpeningOnly), Is.True);
        var authored = new CampaignLevel(99,"Special","",new FreePlayConfig(),allowStarterRewards:true);
        Assert.That(((CampaignLevel)authored.CloneNew()).AllowStarterRewards, Is.True);
        Assert.That(LevelLibrary.AllLevels.Cast<CampaignLevel>().All(level => !level.AllowStarterRewards), Is.True);
    }

    [Test]
    public void OrdinaryCatalogHasNoFreeBaseOrPassiveAnyMatchGrowth()
    {
        foreach (var power in PowerOfferBuilder.Catalog())
            if (!power.OpeningOnly)
            {
                Assert.That(power.FlatBasePerTile, Is.Zero, power.Name);
                Assert.That(power.FlatMultiplierBonus, Is.Zero, power.Name);
            }
        Assert.That(PowerUpLibrary.GetAll().Any(power => power is FlexibleMatchPower || power is BasePerTile ||
            power is SnowballPower || power is ComboCrazePower || power.OpeningOnly), Is.False);
        foreach (HabitatStyle style in System.Enum.GetValues(typeof(HabitatStyle)))
        for (int tier = 0; tier <= 2; tier++)
        {
            var power = new HabitatPower(style,tier);
            Assert.That(power.AdditiveBaseMod(new List<Tile>()), Is.Zero);
            Assert.That(power.FlatBasePerTile, Is.Zero);
        }
        Assert.That(PowerUpLibrary.BlockPowers().Any(power => power.OpeningOnly), Is.False);
    }

    [Test]
    public void OrdinaryFallbackCannotReintroduceReservedPowers()
    {
        var schedule = new LevelPowerOffers(new Dictionary<int, PowerOfferDefinition>(),
            new[] { new PowerTagQuery(all: PowerTags.Starter) });
        var choices = PowerUpSelector.BuildChoices(new List<PowerUp>(), new HashSet<string>(),
            Roster, 3, new System.Random(1), rules: new List<MatchRule> { new(MatchType.Snake, 3) },
            active: new List<PowerUp>(), filter: p => schedule.AllowsOrdinary(p, FreePlayMode.Timed));
        Assert.That(choices, Is.Empty);
    }
    private static readonly List<ItemType> Roster = new() { ItemType.Bear, ItemType.Sloth, ItemType.Chicken,
        ItemType.Duck, ItemType.Hippo, ItemType.Giraffe, ItemType.Walrus };
    private static PowerOfferResult Build(PowerOfferDefinition definition, List<PowerUp> pool = null,
        FreePlayMode mode = FreePlayMode.Timed, GoalType goal = GoalType.Score,
        List<PowerUp> active = null, MatchType matching = MatchType.Snake) =>
        PowerOfferBuilder.Build(definition, pool ?? PowerOfferBuilder.Catalog(), new HashSet<string>(),
            Roster, new System.Random(42), goal, mode, new List<MatchRule> { new(matching, 3) },
            active ?? new List<PowerUp>());

    [Test]
    public void QueriesSupportAllAnyAndNoneWithoutConfusingRosterAndAnimalPowers()
    {
        var query = new PowerTagQuery(all: PowerTags.Animal,
            any: PowerTags.AddAnimal | PowerTags.RemoveAnimal, none: PowerTags.AddAnimal);
        Assert.That(query.Matches(new RosterTradeoffPower(ItemType.Bear, false).Tags), Is.True);
        Assert.That(query.Matches(new RosterTradeoffPower(ItemType.Panda, true).Tags), Is.False);
        Assert.That(query.Matches(new AnimalPairPower(ItemType.Bear, ItemType.Sloth).Tags), Is.False);
        Assert.Throws<System.ArgumentException>(() => new PowerTagQuery(all: PowerTags.Roster, none: PowerTags.Roster));
        Assert.Throws<System.ArgumentException>(() => new PowerTagQuery(any: PowerTags.Budget, none: PowerTags.Budget));
    }

    [Test]
    public void TimeAndMoveRewardsAreContextualAndStillRespectGoalAndRules()
    {
        var offer = new PowerOfferDefinition("Earn time", new[] {
            new PowerOfferSlot(new PowerTagQuery(all: PowerTags.TimeReward), 3) });
        var timed = Build(offer);
        Assert.That(timed.IsComplete, Is.True);
        Assert.That(timed.Choices.Count, Is.EqualTo(3));
        var moves = Build(offer, mode: FreePlayMode.LimitedMoves);
        Assert.That(moves.Choices, Is.Empty);
        Assert.That(moves.Diagnostics.Count, Is.EqualTo(3));
        var line = Build(offer, matching: MatchType.Horizontal);
        Assert.That(line.Choices.Count, Is.EqualTo(2));
        Assert.That(line.IsComplete, Is.False);
        var roster = new PowerOfferDefinition("Animals", new[] {
            new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Roster), 3) });
        var blockRoster = Build(roster, goal: GoalType.BlocksCleared);
        Assert.That(blockRoster.Choices.Count, Is.EqualTo(3));
        Assert.That(blockRoster.Choices.All(p => p is CollectionPower collection && collection.Style == CollectionStyle.Admission), Is.True,
            "Block roster choices use collection admissions, never point-multiplier roster tradeoffs.");
        var budget = new BudgetPower(BudgetStyle.Edge);
        Assert.That((budget.OfferTags(FreePlayMode.LimitedMoves) & PowerTags.MoveReward) != 0, Is.True);
        Assert.That((budget.OfferTags(FreePlayMode.LimitedMoves) & PowerTags.TimeReward) == 0, Is.True);
    }

    [Test]
    public void OverlappingSlotsFindDistinctAssignmentRatherThanStealingNarrowCandidates()
    {
        var offer = new PowerOfferDefinition("Specific choices", new[] {
            new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Scoring | PowerTags.Budget)),
            new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring)),
            new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring | PowerTags.Animal)) });
        var pool = new List<PowerUp> {
            new IconTypeGivesBase(ItemType.Bear, 5), new FlexibleMatchPower(3), new BudgetPower(BudgetStyle.Edge) };
        var result = Build(offer, pool);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Choices[0], Is.TypeOf<BudgetPower>());
        Assert.That(result.Choices[1], Is.TypeOf<FlexibleMatchPower>());
        Assert.That(result.Choices[2], Is.TypeOf<IconTypeGivesBase>());
        Assert.That(result.Choices[0], Is.Not.SameAs(pool[2]));
    }

    [Test]
    public void ExhaustionIsExplicitAndNewOnlyChecksActivePowersToo()
    {
        var pool = new List<PowerUp> { new HighStakesPower() };
        var slots = new[] { new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring), 2) };
        var partial = Build(new PowerOfferDefinition("Two scorers", slots), pool);
        Assert.That(partial.Choices.Count, Is.EqualTo(1));
        Assert.That(partial.Diagnostics.Count, Is.EqualTo(1));
        var strict = Build(new PowerOfferDefinition("Two scorers", slots, OfferShortagePolicy.RequireComplete), pool);
        Assert.That(strict.IsComplete, Is.False);
        Assert.That(strict.Choices, Is.Empty);
        var freshOnly = new PowerOfferDefinition("New scorer", new[] {
            new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring), repeats: OfferRepeatPolicy.NewOnly) });
        Assert.That(Build(freshOnly, pool, active: pool).Choices, Is.Empty);
        var repeat = new PowerOfferDefinition("Scorer", new[] { new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring)) });
        Assert.That(Build(repeat, pool, active: pool).Choices.Count, Is.EqualTo(1));
    }

    [Test]
    public void CustomSchedulesUseArbitraryRoundsSurviveCloningAndReserveByQuery()
    {
        var slots = new List<PowerOfferSlot> {
            new(new PowerTagQuery(all: PowerTags.AddAnimal), 1),
            new(new PowerTagQuery(all: PowerTags.RemoveAnimal), 2) };
        var offer = new PowerOfferDefinition("One new animal or two farewells", slots);
        slots.Clear();
        var rounds = new Dictionary<int, PowerOfferDefinition> { [3] = offer, [6] = offer };
        var schedule = new LevelPowerOffers(rounds, new[] { new PowerTagQuery(all: PowerTags.Roster) });
        rounds.Clear();
        var level = new CampaignLevel(99, "Custom", "Custom draft", new FreePlayConfig(levels: 6),
            powerOffers: schedule);
        var clone = (CampaignLevel)level.CloneNew();
        Assert.That(clone.PowerOffers.ForRound(3).Count, Is.EqualTo(3));
        Assert.That(clone.PowerOffers.ForRound(6), Is.SameAs(offer));
        Assert.That(clone.PowerOffers.ForRound(2), Is.Null);
        Assert.That(schedule.AllowsOrdinary(new RosterTradeoffPower(ItemType.Panda, true), FreePlayMode.Timed), Is.False);
        Assert.That(schedule.AllowsOrdinary(new AnimalPairPower(ItemType.Bear, ItemType.Sloth), FreePlayMode.Timed), Is.True);
        var result = Build(offer);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(((RosterTradeoffPower)result.Choices[0]).AddsAnimal, Is.True);
        Assert.That(((RosterTradeoffPower)result.Choices[1]).AddsAnimal, Is.False);
        Assert.That(((RosterTradeoffPower)result.Choices[2]).AddsAnimal, Is.False);
        Assert.Throws<System.ArgumentException>(() => new FreePlayConfig(levels: 5, powerOffers: schedule));
        var freePlay = new FreePlayConfig(levels: 6, powerOffers: schedule);
        Assert.That(((FreePlayConfig)freePlay.CloneNew()).PowerOffers.ForRound(6), Is.SameAs(offer));
        var wrapped = new CampaignLevel(100, "Wrapped", "Settings schedule", freePlay);
        Assert.That(((CampaignLevel)wrapped.CloneNew()).PowerOffers.ForRound(3), Is.SameAs(offer));
        Assert.Throws<System.ArgumentException>(() => new CampaignLevel(99, "Bad", "Conflicting schedules",
            new FreePlayConfig(), offerTheme: CampaignOfferTheme.Roster, powerOffers: schedule));
    }

    [Test]
    public void CatalogTagsAreStableAcrossClonesAndOfferSizesAreValidated()
    {
        foreach (var power in PowerOfferBuilder.Catalog())
        {
            Assert.That(power.OfferTags(FreePlayMode.Timed), Is.EqualTo(power.Clone().OfferTags(FreePlayMode.Timed)), power.Name);
            Assert.That(power.OfferTags(FreePlayMode.LimitedMoves), Is.EqualTo(power.Clone().OfferTags(FreePlayMode.LimitedMoves)), power.Name);
        }
        Assert.Throws<System.ArgumentException>(() => new PowerOfferDefinition("Empty", new PowerOfferSlot[0]));
        Assert.Throws<System.ArgumentException>(() => new PowerOfferDefinition("Too many", new[] {
            new PowerOfferSlot(new PowerTagQuery(), 3), new PowerOfferSlot(new PowerTagQuery()) }));
    }
}

[TestFixture]
public class CampaignExpansionTests
{
    [Test]
    public void AnimalIntroductionUsesSharedAnimalFilterForEveryRound()
    {
        var document = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        SharedGameContent.Load(document.ToString());
        var record = document["levels"].Cast<Newtonsoft.Json.Linq.JObject>().Single(l => (int)l["number"] == 2);
        var level = SharedGameContent.ApplyLevel((CampaignLevel)LevelLibrary.GetLevel(2), record);

        Assert.That(level.BasePointsPerTile, Is.EqualTo(4));
        Assert.That(((CampaignLevel)level.CloneNew()).BasePointsPerTile, Is.EqualTo(4));
        Assert.That(level.FinalBossStyle, Is.EqualTo(BossStyle.MostMatched));
        Assert.That(level.GetTotalRounds, Is.EqualTo(4));
        for (int seed = 0; seed < 30; seed++)
        for (int path = 0; path < 3; path++)
        {
            var items = new List<ItemType> { ItemType.Bear, ItemType.Sloth, ItemType.Chicken, ItemType.Duck, ItemType.Hippo, ItemType.Giraffe, ItemType.Walrus };
            var active = new List<PowerUp>();
            var selected = new HashSet<string>();
            var random = new System.Random(seed);
            for (int round = 1; round <= 4; round++)
            {
                var result = PowerOfferBuilder.Build(level.PowerOffers.ForRound(round), PowerOfferBuilder.Catalog(),
                    selected, items, random, level.Goal, level.Mode, level.GetMatchRules(round), active);
                Assert.That(result.IsComplete, Is.True, string.Join("; ", result.Diagnostics));
                Assert.That(result.Choices.Count, Is.EqualTo(3));
                Assert.That(result.Choices.All(power => (power.Tags & PowerTags.Animal) != 0), Is.True);
                Assert.That(result.Choices.Select(power => power.Name).Distinct().Count(), Is.EqualTo(3));
                var pick = result.Choices[path];
                active.Add(pick); selected.Add(pick.Name);
                if (pick is RosterTradeoffPower edit)
                {
                    if (edit.AddsAnimal) items.Add(edit.Animal); else items.Remove(edit.Animal);
                }
            }
        }
    }

    [Test]
    public void PopularityBossTracksCurrentLeaderTiesAndRoundReset()
    {
        var history = new RoundAnimalHistory();
        var boss = new ChallengeBoss(BossStyle.MostMatched, GoalType.Score) { AnimalHistory = history };
        Assert.That(boss.BlockedAnimal, Is.Null);
        history.RecordAnimals(Enumerable.Repeat(ItemType.Duck, 4));
        Assert.That(boss.BlockedAnimal, Is.EqualTo(ItemType.Duck));
        Assert.That(boss.GetCurrentPowerDescription(), Does.Contain("No points: Duck"));
        history.RecordAnimals(Enumerable.Repeat(ItemType.Bear, 5));
        Assert.That(boss.BlockedAnimal, Is.EqualTo(ItemType.Bear));
        history.RecordAnimals(new[] { ItemType.Duck });
        var tied = new[] { ItemType.Bear, ItemType.Duck }.OrderBy(animal => (int)animal).First();
        Assert.That(boss.BlockedAnimal, Is.EqualTo(tied));
        history.Reset();
        Assert.That(boss.BlockedAnimal, Is.Null);
        Assert.That(boss.GetCurrentPowerDescription(), Is.EqualTo(boss.PowerUpDescription));
    }

    [Test]
    public void CampaignCardsHandleTutorialBossAndBossFreeLevels()
    {
        Assert.That(LevelSelectUI.CampaignFinalRoundLabel(new CampaignLevel(99, "Custom", "",
            new FreePlayConfig(), bossFinal: false)), Is.EqualTo("COMPLETE ALL ROUNDS TO WIN"));
        foreach (CampaignLevel level in LevelLibrary.AllLevels)
        {
            var label = LevelSelectUI.CampaignFinalRoundLabel(level);
            if (level.IsTutorial) Assert.That(label, Is.EqualTo("Make your first match of 4"));
            else if (level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds).Count == 0)
                Assert.That(label, Is.EqualTo("COMPLETE ALL ROUNDS TO WIN"));
            else Assert.That(label, Does.StartWith("FINAL ROUND: "));
        }
    }
    [Test]
    public void AuthoredCampaignContainsTutorialAndTenDesignedChallenges()
    {
        SharedGameContent.Load(System.IO.File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        var expected = new[] { "Tutorial", "Animal Friends", "Lightning Lanes", "Size Matters", "Clear Commitments", "Chain Reaction", "Garden Plots", "Alternating Current", "Border Patrol", "Treasure Trail", "Changing Tides",
            "Cascade Cleanup", "Big Group Grove", "Square Deal", "Westward Sweep", "Rising Rescue", "Last Move Market", "Sideways Storm", "Tidal Tactics",
            "Shuffle School", "Square Shuffle", "Big Remix", "Storm Shuffle" };
        Assert.That(LevelLibrary.TotalLevels, Is.GreaterThanOrEqualTo(expected.Length));
        for (int n = 1; n <= expected.Length; n++)
        {
            var level = (CampaignLevel)LevelLibrary.GetLevel(n);
            var clone = (CampaignLevel)level.CloneNew();
            Assert.That(level.GetLevelNumber, Is.EqualTo(n));
            Assert.That(level.Name, Is.EqualTo(expected[n-1]));
            Assert.That(clone.Name, Is.EqualTo(level.Name));
            Assert.That(clone.GetTotalRounds, Is.EqualTo(level.GetTotalRounds));
            for (int round = 2; round <= clone.GetTotalRounds; round++)
                Assert.That(clone.PowerOffers.ForRound(round).Label, Is.EqualTo(level.PowerOffers.ForRound(round).Label));
        }
        Assert.That(LevelLibrary.GetLevel(LevelLibrary.TotalLevels+1), Is.Null);
        Assert.That(LevelLibrary.GetLevel(0), Is.Null);
    }
    [Test]
    public void EveryExpansionOfferHasThreeCompatibleDistinctCardsAfterEarlierChoices()
    {
        for (int number = 2; number <= LevelLibrary.TotalLevels; number++)
        for (int seed = 0; seed < 30; seed++)
        {
            var level = (CampaignLevel)LevelLibrary.GetLevel(number);
            var random = new System.Random(seed);
            var items = new List<ItemType> { ItemType.Bear, ItemType.Sloth, ItemType.Chicken,
                ItemType.Duck, ItemType.Hippo, ItemType.Giraffe, ItemType.Walrus };
            if (level.BlockObjectives != null)
                foreach (var animal in level.BlockObjectives.RequiredAnimals)
                    if (!items.Contains(animal)) items.Add(animal);
            var active = new List<PowerUp>();
            var selected = new HashSet<string>();
            var rules = level.GetMatchRules(1);
            for (int round = 1; round <= level.GetTotalRounds; round++)
            {
                var theme = level.OfferThemeForRound(round);
                List<PowerUp> choices;
                if (level.PowerOffers.ForRound(round) is PowerOfferDefinition definition)
                {
                    var objective = level.BlockObjectives?.ForRound(round);
                    var result = PowerOfferBuilder.Build(definition, objective == null ? PowerOfferBuilder.Catalog() : BlockObjective.CatalogFor(objective, active), selected, items, random,
                        level.Goal, level.Mode, rules, active, round, level.AllowStarterRewards);
                    Assert.That(result.IsComplete, Is.True, $"Level {number}, round {round}, seed {seed}: {string.Join("; ", result.Diagnostics)}");
                    choices = new List<PowerUp>(result.Choices);
                    if (level.Settings.GrowthPercent >= 85)
                        Assert.That(choices.Exists(p => (p.Tags & PowerTags.Premium) != 0), Is.True);
                }
                else
                {
                    var pool = level.Goal == GoalType.BlocksCleared
                        ? (round == 1 ? PowerUpLibrary.BlockStarters() : PowerUpLibrary.BlockPowers())
                        : level.GetPowerUpsForRound(round);
                    pool.RemoveAll(p => !CampaignExpansion.AllowOrdinaryPower(level.OfferTheme, p));
                    choices = PowerUpSelector.BuildChoices(pool, selected, items, 3, random,
                        level.Goal, level.Mode, rules, active, round: round, allowStarterRewards: level.AllowStarterRewards);
                }
                Assert.That(choices.Count, Is.EqualTo(3), $"Level {number}, round {round}, seed {seed}");
                var offeredNames = new HashSet<string>();
                foreach (var power in choices)
                {
                    Assert.That(offeredNames.Add(power.Name), Is.True);
                    Assert.That(power.Supports(level.Goal, level.Mode), Is.True);
                    Assert.That(power.SupportsItems(items), Is.True);
                    Assert.That(power.CanUseWithMatchRules(rules), Is.True);
                    Assert.That(power.CanOfferWith(active), Is.True);
                    if (round > 1 && !level.AllowStarterRewards) Assert.That(power.OpeningOnly, Is.False, power.Name);
                    if (theme == CampaignOfferTheme.Roster) Assert.That(power, Is.TypeOf<RosterTradeoffPower>());
                }
                if (theme == CampaignOfferTheme.PointsOrBudget || theme == CampaignOfferTheme.ClearsOrBudget)
                    Assert.That(choices.FindAll(p => p is BudgetPower).Count, Is.EqualTo(1));
                if (theme == CampaignOfferTheme.Shuffle)
                    Assert.That(choices.FindAll(p => p is ShufflePower).Count, Is.EqualTo(1));
                var pick = choices[random.Next(choices.Count)];
                active.Add(pick);
                selected.Add(pick.Name);
                pick.ModifyMatchRules(rules);
                if (pick is CollectionPower collection && collection.Style == CollectionStyle.Admission && !items.Contains(collection.Animal)) items.Add(collection.Animal);
                if (pick is RosterTradeoffPower roster)
                {
                    if (roster.AddsAnimal) items.Add(roster.Animal);
                    else items.Remove(roster.Animal);
                }
            }
        }
    }
}


[TestFixture]
public class PowerUpBehaviorTests
{
    [Test]
    public void AlternatingAnimalTilesKeepTheirScoreFactorWithoutVisualHints()
    {
        var go=new GameObject("Unmarked bonus tile");_createdObjects.Add(go);
        var tile=go.AddComponent<Tile>();tile.background=go.AddComponent<UnityEngine.UI.Image>();
        tile.SetLevelEffects(false,true);
        Assert.That(tile.IsSpotlight,Is.True);
        Assert.That(tile.LevelPointFactor,Is.EqualTo(3));
        Assert.That(tile.background.color,Is.EqualTo(Color.white));
        tile.SetSelected(true);Assert.That(tile.background.color,Is.EqualTo(Color.darkGray));
        tile.SetSelected(false);Assert.That(tile.background.color,Is.EqualTo(Color.white));
        tile.SetCharged(true);Assert.That(tile.background.color,Is.EqualTo(Tile.ChargedColor));
        Assert.That(tile.LevelPointFactor,Is.EqualTo(3));
        tile.SetLevelEffects(true,false);Assert.That(tile.LevelPointFactor,Is.EqualTo(2));
        Assert.That(tile.background.color,Is.EqualTo(new Color(.78f,.93f,.8f,1)));
    }
    [Test]
    public void AnimalFriendsBonusMultipliesPowerMultipliersAndUpdatesAfterScoring()
    {
        _scoreManager.LevelBasePointsPerTile=4;
        _scoreManager.LevelBonus=LevelBonus.LeastMatched;
        _scoreManager.LevelAnimalPool=new[]{ItemType.Duck,ItemType.Bear};
        AddPower(new AnimalDoublePower(ItemType.Duck));
        AddPower(new IconTypeGivesMulti(ItemType.Duck,2));
        var ducks=MakeTiles(ItemType.Duck,4);
        Assert.That(_scoreManager.CalculateScore(ducks),Is.EqualTo(192),"32 doubled base x3 additive multiplier x2 level bonus.");
        Assert.That(_scoreManager.LastResult.Multiplier,Is.EqualTo(6));
        Assert.That(_scoreManager.LastResult.BasePoints,Is.EqualTo(32));
        Assert.That(_scoreManager.CalculateScore(ducks),Is.EqualTo(96),"Duck is no longer least matched after the first match.");
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Bear,4)),Is.EqualTo(32));
        _scoreManager.Reset();
        Assert.That(_scoreManager.CalculateScore(ducks),Is.EqualTo(192),"Round reset restores the starting tie.");
        var boss=new ChallengeBoss(BossStyle.MostMatched,GoalType.Score);
        _powerManager.RegisterBossPower(boss);
        var pool=new PatternTestPool();pool.GetActiveItems.Add(ducks[0].item);pool.GetActiveItems.Add(MakeTiles(ItemType.Bear,1)[0].item);
        boss.ItemInfoProvider=pool;
        Assert.That(_scoreManager.CalculateScore(ducks),Is.Zero,"The level bonus cannot bypass the boss veto.");
    }
    [Test]
    public void LeastMatchedFactorCombinesWithFractionalMultiplierBeforeRounding()
    {
        _scoreManager.LevelBasePointsPerTile=1;
        _scoreManager.LevelBonus=LevelBonus.LeastMatched;
        _scoreManager.LevelAnimalPool=new[]{ItemType.Duck,ItemType.Bear};
        AddPower(new SharedRulePower(Newtonsoft.Json.Linq.JObject.Parse("{id:'custom-power-half',source:'custom',name:'Half extra',description:'Test',goals:'Points',trigger:'always',animal:'Any',length:4,sequence:'4,5,6',reward:'multi',value:0.5,unit:'match',permanent:false,threshold:1}")));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Duck,3)),Is.EqualTo(9),"3 base x1.5 multiplier x2 bonus, rounded once.");
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Duck,3)),Is.EqualTo(4),"The former least-matched animal gets no bonus.");
    }
    [Test]
    public void AnimalDoubleUsesLevelBaseAndPopularityVetoKeepsCountingBlockedMatches()
    {
        _scoreManager.LevelBasePointsPerTile = 4;
        AddPower(new AnimalDoublePower(ItemType.Duck));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Duck, 4)), Is.EqualTo(32));
        Assert.That(_scoreManager.LastResult.BasePoints, Is.EqualTo(32));
        Assert.That(_scoreManager.LastResult.Multiplier, Is.EqualTo(1));
        Assert.That(_scoreManager.LastResult.Activations.Exists(a => a.Power is AnimalDoublePower && a.Effect == "x2 base"), Is.True);
        var boss = new ChallengeBoss(BossStyle.MostMatched, GoalType.Score);
        _powerManager.RegisterBossPower(boss);
        var pool = new PatternTestPool();
        pool.GetActiveItems.Add(MakeTiles(ItemType.Duck, 1)[0].item);
        pool.GetActiveItems.Add(MakeTiles(ItemType.Bear, 1)[0].item);
        boss.ItemInfoProvider = pool;
        Assert.That(boss.BlockedAnimal, Is.EqualTo(ItemType.Duck));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Duck, 4)), Is.Zero);
        Assert.That(_powerManager.AnimalHistory.Count(ItemType.Duck), Is.EqualTo(8));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Bear, 4)), Is.EqualTo(16));
    }
    [Test]
    public void DuckEmbargoBlocksPointsWithoutBlockingOtherAnimalsOrBlockCredit()
    {
        var boss = new ChallengeBoss(BossStyle.DuckEmbargo, GoalType.Score);
        Assert.That(boss.IsScoreValid(MakeTiles(ItemType.Duck, 4)), Is.False);
        Assert.That(boss.IsScoreValid(MakeTiles(ItemType.Bear, 4)), Is.True);
        Assert.That(boss.ModifyBlockCredit(MakeTiles(ItemType.Duck, 4), 4), Is.EqualTo(4));
        Assert.That(boss.Clone().Name, Is.EqualTo(boss.Name));
    }

    private sealed class TacticalMove : MatchInfo, PlayerMoveInfo
    {
        public bool User = true;
        public int MoveId { get; set; }
        public bool BothSwapTilesMatched { get; set; }
        public bool AllPlayerGroupsExactlyFour { get; set; } = true;
        public int BoardWidth => 7;
        public int BoardHeight => 7;
        public bool wasUserInitiatedMatch() => User;
    }

    [Test]
    public void ShoreBountyPaysForEachBoundarySideAndDoublesCorners()
    {
        var tiles = MakeTiles(ItemType.Bear, 4);
        var power = new HabitatPower(HabitatStyle.Shore, 0) { MatchInfo = new TacticalMove() };
        foreach (var tile in tiles) { tile.x = 3; tile.y = 3; }
        Assert.That(power.AdditiveBaseMod(tiles), Is.Zero);
        tiles[0].x = 0;
        Assert.That(power.AdditiveBaseMod(tiles), Is.EqualTo(10));
        tiles[0].y = 0;
        Assert.That(power.AdditiveBaseMod(tiles), Is.EqualTo(20));
        tiles[1].x = 6; tiles[1].y = 6;
        Assert.That(power.AdditiveBaseMod(tiles), Is.EqualTo(40));
        ((TacticalMove)power.MatchInfo).User = false;
        Assert.That(power.AdditiveBaseMod(tiles), Is.EqualTo(40));
        var blocks = new ScoreResult();
        SharedPowerRewards.Resolve(power, 4, true, 40, 0, 1, 1, blocks);
        Assert.That(blocks.BlockCredits[power], Is.EqualTo(40));
    }

    [Test]
    public void TacticalSquaresAccumulateHalfMultipliersAndCascadesDoNotGrow()
    {
        var tiles = MakeTiles(ItemType.Bear, 4);
        for (int i = 0; i < 4; i++) { tiles[i].x = i % 2; tiles[i].y = i / 2; }
        var context = new TacticalMove();
        var power = new TacticalStarter(TacticalStyle.Square);
        AddPower(power); power.MatchInfo = context;
        Assert.That(_scoreManager.CalculateScore(tiles), Is.EqualTo(24));
        Assert.That(_scoreManager.LastResult.Multiplier, Is.EqualTo(1.5f));
        context.User = false;
        Assert.That(_scoreManager.CalculateScore(tiles), Is.EqualTo(24));
        _scoreManager.Reset(); context.User = true;
        Assert.That(_scoreManager.CalculateScore(tiles), Is.EqualTo(32), "Growth survives rounds.");
        var clone = (TacticalStarter)power.Clone(); clone.MatchInfo = context;
        Assert.That(clone.FractionalMultiplierMod(tiles), Is.EqualTo(.5f), "Fresh clones have no earned growth.");
    }

    [Test]
    public void TacticalShapeDetectionRequiresActualGeometry()
    {
        var tiles = MakeTiles(ItemType.Duck, 5);
        var positions = new[] { (1,1), (0,1), (2,1), (1,0), (1,2) };
        for (int i=0; i<5; i++) { tiles[i].x=positions[i].Item1; tiles[i].y=positions[i].Item2; }
        Assert.That(TacticalStarter.CountShapes(tiles, true), Is.EqualTo(1));
        Assert.That(TacticalStarter.CountShapes(tiles, false), Is.Zero);
        tiles[4].x=5;
        Assert.That(TacticalStarter.CountShapes(tiles, true), Is.Zero);
    }

    [Test]
    public void TacticalStreakCountsSwapsAndIgnoresCascades()
    {
        var tiles=MakeTiles(ItemType.Bear,4); var context=new TacticalMove();
        var power=new TacticalStarter(TacticalStyle.FourStreak) { MatchInfo=context };
        for(int i=1;i<=3;i++) { context.MoveId=i; Assert.That(power.FractionalMultiplierMod(tiles),Is.Zero); }
        Assert.That(power.FractionalMultiplierMod(tiles),Is.Zero,"Same swap cannot advance twice.");
        context.User=false; power.FractionalMultiplierMod(MakeTiles(ItemType.Bear,6));
        context.User=true; context.MoveId=4;
        Assert.That(power.FractionalMultiplierMod(tiles),Is.EqualTo(1));
        power.OnRoundStarted(); context.MoveId=5;
        Assert.That(power.FractionalMultiplierMod(tiles),Is.EqualTo(1));
        context.AllPlayerGroupsExactlyFour=false; context.MoveId=6; power.FractionalMultiplierMod(tiles);
        StringAssert.Contains("Streak: 0/4",power.GetCurrentPowerDescription());
    }

    [Test]
    public void TacticalOpeningResetsAndDoubleSwapNeedsBothDestinations()
    {
        var tiles=MakeTiles(ItemType.Bear,6); var context=new TacticalMove();
        var opening=new TacticalStarter(TacticalStyle.Opening) { MatchInfo=context };
        Assert.That(opening.FractionalMultiplierMod(tiles),Is.EqualTo(1));
        Assert.That(opening.FractionalMultiplierMod(tiles),Is.EqualTo(1));
        opening.OnRoundStarted(); Assert.That(opening.FractionalMultiplierMod(tiles),Is.EqualTo(3));
        var dual=new TacticalStarter(TacticalStyle.DoubleSwap) { MatchInfo=context };
        Assert.That(dual.FractionalMultiplierMod(tiles),Is.Zero);
        context.BothSwapTilesMatched=true; Assert.That(dual.FractionalMultiplierMod(tiles),Is.EqualTo(.5f));
        Assert.That(dual.FractionalMultiplierMod(tiles),Is.EqualTo(.5f), "Once per swap.");
        context.User=false; Assert.That(dual.FractionalMultiplierMod(tiles),Is.EqualTo(.5f));
    }

    [Test]
    public void TacticalPoolFiltersShapesAndImpossibleExactFour()
    {
        Assert.That(PowerUpLibrary.Starters().Count,Is.EqualTo(8));
        var line=new List<MatchRule> { new MatchRule(MatchType.Horizontal,5) };
        Assert.That(new TacticalStarter(TacticalStyle.Plus).CanUseWithMatchRules(line),Is.False);
        Assert.That(new TacticalStarter(TacticalStyle.Square).CanUseWithMatchRules(line),Is.False);
        Assert.That(new TacticalStarter(TacticalStyle.FourStreak).CanUseWithMatchRules(line),Is.False);
        Assert.That(new TacticalStarter(TacticalStyle.Large).CanUseWithMatchRules(line),Is.True);
        Assert.That(new TacticalStarter(TacticalStyle.Plus).Supports(GoalType.BlocksCleared,FreePlayMode.Timed),Is.True);
    }


    [Test]
    public void FlatStatsExcludeConditionalAndQueuedBonusesAndNeverAdvancePowers()
    {
        var starter = new TacticalStarter(TacticalStyle.Opening) { MatchInfo = new TacticalMove() };
        var snowball = new SnowballPower();
        var powers = new List<PowerUp> { starter, new BaseMultiPowerUp(2, 1),
            new IconTypeGivesMulti(ItemType.Bear, 5), new RosterTradeoffPower(ItemType.Panda, false), snowball };
        for (int i=0; i<20; i++)
        {
            var stats = FlatScoreStats.Read(powers, 3);
            Assert.That(stats.BasePerTile, Is.EqualTo(6));
            Assert.That(stats.Multiplier, Is.EqualTo(1)); // (1 + 1) * .5, not bear/first/queued.
            StringAssert.Contains("NEXT MATCH ONLY: x3", stats.MultiplierDetails);
            StringAssert.Contains("Opening Gambit", stats.MultiplierDetails);
        }
        Assert.That(snowball.FlatMultiplierBonus, Is.Zero);
        Assert.That(starter.FractionalMultiplierMod(MakeTiles(ItemType.Bear,6)), Is.EqualTo(1));
    }

    [Test]
    public void FlatStatsIncludeEarnedGrowthAndRecomputeWhenPowerRemoved()
    {
        var power = new TacticalStarter(TacticalStyle.Large) { MatchInfo = new TacticalMove() };
        var powers = new List<PowerUp> { power };
        Assert.That(FlatScoreStats.Read(powers).Multiplier,Is.EqualTo(1));
        power.FractionalMultiplierMod(MakeTiles(ItemType.Bear,6));
        Assert.That(FlatScoreStats.Read(powers).Multiplier,Is.EqualTo(1.5f));
        power.OnRoundStarted();
        Assert.That(FlatScoreStats.Read(powers).Multiplier,Is.EqualTo(1.5f));
        powers.Clear();
        Assert.That(FlatScoreStats.Read(powers).BasePerTile,Is.Zero);
        Assert.That(FlatScoreStats.Read(powers).Multiplier,Is.EqualTo(1));
    }


    [Test]
    public void EdgeAndVarietyEarnPermanentGrowthAfterThreeTriggers()
    {
        var context = new TacticalMove();
        var edge = new TacticalStarter(TacticalStyle.Edge) { MatchInfo = context };
        var bears = MakeTiles(ItemType.Bear,4);
        for (int i=0;i<2;i++) Assert.That(edge.FractionalMultiplierMod(bears),Is.Zero);
        context.User=false; Assert.That(edge.FractionalMultiplierMod(bears),Is.Zero);
        context.User=true; Assert.That(edge.FractionalMultiplierMod(bears),Is.EqualTo(.5f));
        edge.OnRoundStarted(); Assert.That(edge.FlatMultiplierBonus,Is.EqualTo(.5f));
        var variety = new TacticalStarter(TacticalStyle.Alternating) { MatchInfo=context };
        var ducks=MakeTiles(ItemType.Duck,4);
        variety.FractionalMultiplierMod(bears);
        variety.FractionalMultiplierMod(ducks);
        variety.FractionalMultiplierMod(bears);
        Assert.That(variety.FractionalMultiplierMod(ducks),Is.EqualTo(.5f));
        Assert.That(variety.FractionalMultiplierMod(ducks),Is.EqualTo(.5f));
        var opening=new TacticalStarter(TacticalStyle.Opening) { MatchInfo=context };
        Assert.That(opening.FractionalMultiplierMod(bears),Is.Zero);
        Assert.That(opening.FractionalMultiplierMod(MakeTiles(ItemType.Bear,6)),Is.Zero,"Only the first group qualifies.");
    }


    [Test]
    public void StatCategoriesDoNotInferTrackingHooksAsBonuses()
    {
        var multi = new IconTypeStreakGivesBasePointsOrMulti(ItemType.Bear, 0, 10, 1);
        Assert.That(multi.AffectsStat(false), Is.False);
        Assert.That(multi.ConditionForStat(false), Is.Empty);
        Assert.That(multi.ConditionForStat(true), Does.Contain("multi"));
        var both = new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 2, 10, 1);
        Assert.That(both.StatEffects, Is.EqualTo(PowerStatEffects.Both));
        Assert.That(both.ConditionForStat(false), Does.Not.Contain("multi"));
        Assert.That(both.ConditionForStat(true), Does.Not.Contain("base / tile"));
        var starter = new TacticalStarter(TacticalStyle.Square);
        Assert.That(starter.ConditionForStat(false), Is.Empty);
        Assert.That(starter.ConditionForStat(true), Does.Not.Contain("+4 base"));
        foreach (var power in PowerUpLibrary.GetAll())
            Assert.That(power.StatEffects, Is.Not.EqualTo(PowerStatEffects.None), power.Name);
        foreach (var power in PowerUpLibrary.BlockPowers())
            Assert.That(power.StatEffects, Is.EqualTo(PowerStatEffects.None), power.Name);
    }

    private PowerManager _powerManager;
    private ScoreManager _scoreManager;
    private List<Object> _createdAssets;
    private List<GameObject> _createdObjects;

    [SetUp]
    public void SetUp()
    {
        _createdAssets = new List<Object>();
        _createdObjects = new List<GameObject>();
        _powerManager = new PowerManager(new FakeTimeProvider(), new FakeLevelInfoProvider(), new FakeItemInfoProvider());
        _scoreManager = new ScoreManager(new PlayerConfigs(), _powerManager);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _createdObjects)
        {
            Object.DestroyImmediate(go);
        }
        foreach (var asset in _createdAssets)
        {
            Object.DestroyImmediate(asset);
        }
    }

    private sealed class PatternTestPool : ItemInfoProvider
    {
        public List<Item> GetActiveItems { get; } = new();
        public void AddOrRemoveItem(Item item, bool add) { }
    }

    [Test]
    public void MenagerieEngineCompletesSetsUsingTheCurrentRoster()
    {
        var bears = MakeTiles(ItemType.Bear, 3);
        var ducks = MakeTiles(ItemType.Duck, 3);
        var elephants = MakeTiles(ItemType.Elephant, 3);
        var pool = new PatternTestPool();
        pool.GetActiveItems.Add(bears[0].item);
        pool.GetActiveItems.Add(ducks[0].item);
        var engine = new HabitatPower(HabitatStyle.Menagerie, 2)
        {
            ItemInfoProvider = pool,
            MatchInfo = new FakeMatchInfo(true)
        };
        Assert.That(engine.FractionalMultiplierMod(bears), Is.Zero);
        Assert.That(engine.FractionalMultiplierMod(bears), Is.Zero);
        Assert.That(engine.FractionalMultiplierMod(ducks), Is.EqualTo(1));
        Assert.That(engine.FractionalMultiplierMod(bears), Is.EqualTo(1));
        pool.GetActiveItems.Add(elephants[0].item);
        engine.OnRoundStarted();
        Assert.That(engine.FractionalMultiplierMod(ducks), Is.EqualTo(1));
        engine.MatchInfo = new FakeMatchInfo(false);
        Assert.That(engine.FractionalMultiplierMod(elephants), Is.EqualTo(1));
        engine.MatchInfo = new FakeMatchInfo(true);
        Assert.That(engine.FractionalMultiplierMod(elephants), Is.EqualTo(2));
        Assert.That(engine.FractionalMultiplierMod(bears), Is.EqualTo(2));
        pool.GetActiveItems.Remove(elephants[0].item);
        Assert.That(engine.FractionalMultiplierMod(ducks), Is.EqualTo(3));
        Assert.That(engine.Clone().FlatMultiplierBonus, Is.Zero);
        engine.Apply(null);
        Assert.That(engine.FractionalMultiplierMod(ducks), Is.Zero);
    }

    [Test]
    public void PatternRewardsUsePriorCountsAndResetWithoutOrderDependentPenalties()
    {
        var bears = MakeTiles(ItemType.Bear, 4);
        var ducks = MakeTiles(ItemType.Duck, 4);
        var pool = new PatternTestPool();
        pool.GetActiveItems.Add(bears[0].item);
        pool.GetActiveItems.Add(ducks[0].item);
        var pattern = new PatternAnimalPower(true);
        AddPower(pattern); // Deliberately before base power to test negative aggregation.
        pattern.ItemInfoProvider = pool;
        AddPower(new BasePowerUp(5));
        Assert.That(_scoreManager.CalculateScore(bears), Is.EqualTo(60), "All tied: +10 and +5 base per tile.");
        Assert.That(_scoreManager.CalculateScore(bears), Is.EqualTo(0), "Most matched: -5 cancels +5 regardless of registration order.");
        Assert.That(_scoreManager.CalculateScore(ducks), Is.EqualTo(60), "Least matched still earns its bonus before counts change.");
        StringAssert.Contains("Duck", pattern.GetCurrentPowerDescription());
        _scoreManager.Reset();
        Assert.That(_powerManager.AnimalHistory.Count(ItemType.Bear), Is.EqualTo(0));
        Assert.That(_scoreManager.CalculateScore(bears), Is.EqualTo(60));
        Assert.That(pattern.Supports(GoalType.BlocksCleared, FreePlayMode.Timed), Is.False);
    }

    [Test]
    public void AnimalPactHalvesAffectedGroupAfterBonusesAndPreservesUnrelatedScore()
    {
        AddPower(new BasePowerUp(5));
        AddPower(new AnimalPairPower(ItemType.Duck, ItemType.Walrus));
        var mixed = MakeTiles(ItemType.Duck, 2);
        mixed.AddRange(MakeTiles(ItemType.Walrus, 2));
        Assert.That(_scoreManager.CalculateScore(mixed), Is.EqualTo(10));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Duck, 3)), Is.EqualTo(7));
        Assert.That(_scoreManager.CalculateScore(MakeTiles(ItemType.Bear, 4)), Is.EqualTo(20));
    }

    [Test]
    public void CalculateScore_BasePlusIconMultiplier_AppliesMultiplier()
    {
        AddPower(new BasePowerUp(1));
        AddPower(new IconTypeGivesMulti(ItemType.Bear, 2));

        var tiles = MakeTiles(ItemType.Bear, 3);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(9, score, "3 tiles @1 point with +2 multi should yield 9.");
    }

    [Test]
    public void IconBasePower_AddsPointsForMatchingAnimal()
    {
        AddPower(new IconTypeGivesBase(ItemType.Bear, 5));
        Assert.AreEqual(15, _scoreManager.CalculateScore(MakeTiles(ItemType.Bear, 3)));
        Assert.AreEqual(0, _scoreManager.CalculateScore(MakeTiles(ItemType.Duck, 3)));
    }

    [Test]
    public void ColorFocus_AppliesImmediateDoubleOnRepeatedAnimal()
    {
        AddPower(new BasePowerUp(1));
        var focus = new ColorFocusPower { MatchInfo = new FakeMatchInfo(true) };
        AddPower(focus);
        var tiles = MakeTiles(ItemType.Bear, 3);
        Assert.AreEqual(3, _scoreManager.CalculateScore(tiles));
        Assert.AreEqual(6, _scoreManager.CalculateScore(tiles));
    }

    [Test]
    public void PrecisionStrike_SetsNextMatchMultiplierAfterTwoThreeMatches()
    {
        var precision = new PrecisionStrikePower();
        precision.MatchInfo = new FakeMatchInfo(true);

        AddPower(new BasePowerUp(1));
        AddPower(precision);

        var tiles = MakeTiles(ItemType.Bear, 3);

        _scoreManager.CalculateScore(tiles); // streak count 1
        _scoreManager.CalculateScore(tiles); // streak count 2, queues 4x next match
        int score = _scoreManager.CalculateScore(tiles); // should apply 4x

        Assert.AreEqual(12, score, "Base 3 points with queued 4x next-match multiplier should be 12.");
    }

    [TestCase(40, 6)]
    [TestCase(30, 6)]
    [TestCase(29, 8)]
    public void BossPower_SlowDownBuddy_ReducesScoreOnlyDuringFirstHalf(int timeRemaining, int expectedScore)
    {
        var bossPower = new HalfPointsForFirstHalfOfLevel
        {
            TimeProvider = new FixedTimeProvider(timeRemaining)
        };

        AddPower(new BasePowerUp(1));
        AddBossPower(bossPower);

        var tiles = MakeTiles(ItemType.Bear, 8);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(expectedScore, score);
    }

    [Test]
    public void SidewaysThinking_QuadruplesScoreAndRestrictsConnectedMatches()
    {
        var power = new NoVerticalDoublePower();
        AddPower(new BasePowerUp(1));
        AddPower(power);
        var rules = new List<MatchRule>
        {
            new(MatchType.Snake, 3)
        };

        power.ModifyMatchRules(rules);

        Assert.AreEqual(0, rules.FindAll(rule => rule.type == MatchType.Vertical).Count);
        Assert.AreEqual(1, rules.FindAll(rule => rule.type == MatchType.Horizontal).Count);
        Assert.AreEqual(12, _scoreManager.CalculateScore(MakeTiles(ItemType.Bear, 3)));
    }

    [TestCase(BudgetStyle.Edge, 3)]
    [TestCase(BudgetStyle.Square, 2)]
    [TestCase(BudgetStyle.Large, 2)]
    public void BudgetPatternsKeepRewardingBeyondThreeIgnoreDuplicateGroupsAndResetEachRound(BudgetStyle style, int required)
    {
        var context = new TacticalMove();
        var power = new BudgetPower(style) { MatchInfo = context };
        var tiles = MakeTiles(ItemType.Bear, 6);
        for (int i = 0; i < tiles.Count; i++) { tiles[i].x = i % 3; tiles[i].y = i / 3; }
        context.User = false;
        Assert.That(power.EarnBudgetRewards(tiles), Is.Zero);
        Assert.That(power.Progress, Is.Zero);
        context.User = true;
        for (int reward = 0; reward < 10; reward++)
        for (int match = 0; match < required; match++)
        {
            context.MoveId++;
            Assert.That(power.EarnBudgetRewards(tiles), Is.EqualTo(match == required - 1 ? 1 : 0));
            Assert.That(power.EarnBudgetRewards(tiles), Is.Zero, "Multiple groups cannot pay twice in one swap.");
        }
        context.MoveId++;
        Assert.That(power.EarnBudgetRewards(tiles), Is.Zero);
        Assert.That(power.RewardsEarned, Is.EqualTo(10));
        Assert.That(power.Progress, Is.EqualTo(1));
        var clone = (BudgetPower)power.Clone();
        Assert.That(clone.RewardsEarned, Is.Zero);
        power.OnRoundStarted();
        Assert.That(power.Progress, Is.Zero);
        Assert.That(power.RewardsEarned, Is.Zero);
        Assert.That(power.EarnBudgetRewards(tiles), Is.Zero);
        Assert.That(power.Progress, Is.EqualTo(1));
    }

    [Test]
    public void BudgetPatternsRequireActualShapeAndCanQualifyAfterNonmatchingGroup()
    {
        var context = new TacticalMove();
        var tiles = MakeTiles(ItemType.Bear, 4);
        for (int i = 0; i < tiles.Count; i++) { tiles[i].x = i + 1; tiles[i].y = 2; }
        foreach (var style in new[] { BudgetStyle.Edge, BudgetStyle.Square, BudgetStyle.Large })
        {
            var power = new BudgetPower(style) { MatchInfo = context };
            Assert.That(power.EarnBudgetRewards(tiles), Is.Zero);
            Assert.That(power.Progress, Is.Zero);
        }
        var edge = new BudgetPower(BudgetStyle.Edge) { MatchInfo = context };
        edge.EarnBudgetRewards(tiles);
        tiles[0].x = 6; // Right edge of the 7-column board.
        edge.EarnBudgetRewards(tiles);
        Assert.That(edge.Progress, Is.EqualTo(1));
        var large = new BudgetPower(BudgetStyle.Large) { MatchInfo = context };
        large.EarnBudgetRewards(new List<Tile> { tiles[0], tiles[0], tiles[0], tiles[0], tiles[0], tiles[0] });
        Assert.That(large.Progress, Is.Zero, "Duplicate tiles cannot make a large group.");
    }

    [Test]
    public void BudgetOffersSupportBothGoalsAndModesButExcludeImpossibleSquaresAndDuplicates()
    {
        foreach (var goal in new[] { GoalType.Score, GoalType.BlocksCleared })
        foreach (var mode in new[] { FreePlayMode.Timed, FreePlayMode.LimitedMoves })
        {
            var source = goal == GoalType.Score ? PowerUpLibrary.GetAll() : PowerUpLibrary.BlockPowers();
            var offers = PowerUpSelector.BuildChoices(source, new HashSet<string>(), new List<ItemType>(), 100,
                new System.Random(1), goal, mode, new List<MatchRule> { new(MatchType.Snake, 4) });
            Assert.That(offers.FindAll(p => p is BudgetPower).Count, Is.EqualTo(3));
            offers = PowerUpSelector.BuildChoices(source, new HashSet<string>(), new List<ItemType>(), 100,
                new System.Random(1), goal, mode, new List<MatchRule> { new(MatchType.Horizontal, 3) },
                new List<PowerUp> { new BudgetPower(BudgetStyle.Edge) });
            Assert.That(offers.FindAll(p => p is BudgetPower).Count, Is.EqualTo(1));
            Assert.That(offers.Find(p => p is BudgetPower), Is.TypeOf<BudgetPower>());
            Assert.That(((BudgetPower)offers.Find(p => p is BudgetPower)).Style, Is.EqualTo(BudgetStyle.Large));
        }
    }

    [Test]
    public void ExactThreeOffersRequireRulesThatCanProduceThreeTiles()
    {
        var source = new List<PowerUp> { new PrecisionStrikePower(),
            new StarterPower(StarterStrategy.Precision) };
        foreach (var type in new[] { MatchType.Horizontal, MatchType.Vertical, MatchType.Snake })
        foreach (var minimum in new[] { 2, 3, 4, 5 })
        {
            var rules = new List<MatchRule> { new(type, minimum) };
            var offers = PowerUpSelector.BuildChoices(source, new HashSet<string>(), new List<ItemType>(),
                3, new System.Random(1), rules: rules);
            foreach (var power in source)
            {
                Assert.That(power.CanUseWithMatchRules(rules), Is.EqualTo(minimum <= 3));
                Assert.That(offers.Exists(p => p.Name == power.Name), Is.EqualTo(minimum <= 3));
            }
        }
        var mixed = new List<MatchRule> { new(MatchType.Snake, 4), new(MatchType.Horizontal, 3) };
        foreach (var power in source) Assert.That(power.CanUseWithMatchRules(mixed), Is.True);
        Assert.That(new StarterPower(StarterStrategy.Momentum).CanUseWithMatchRules(
            new List<MatchRule> { new(MatchType.Snake, 4) }), Is.True);
    }

    private List<Tile> MakeTiles(ItemType type, int count)
    {
        var list = new List<Tile>();
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Tile_{i}");
            _createdObjects.Add(go);
            var tile = go.AddComponent<Tile>();
            var item = ScriptableObject.CreateInstance<Item>();
            _createdAssets.Add(item);
            item.ItemType = type;
            tile.item = item;
            list.Add(tile);
        }
        return list;
    }

    private void AddPower(PowerUp power)
    {
        SetProviders(power);
        GetPrivateList<PowerUp>("_activePowers").Add(power);
    }

    private void AddBossPower(BossPowers power)
    {
        SetProviders(power);
        GetPrivateList<BossPowers>("_activeBossPowers").Add(power);
    }

    private void SetProviders(PowerUp power)
    {
        power.TimeProvider ??= new FakeTimeProvider();
        power.LevelInfoProvider ??= new FakeLevelInfoProvider();
        power.ItemInfoProvider ??= new FakeItemInfoProvider();
        power.MatchInfo ??= new FakeMatchInfo(true);
    }

    private List<T> GetPrivateList<T>(string fieldName)
    {
        var field = typeof(PowerManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (List<T>)field.GetValue(_powerManager);
    }

    private class FixedTimeProvider : TimeProvider
    {
        private readonly int _timeRemaining;
        public FixedTimeProvider(int timeRemaining) { _timeRemaining = timeRemaining; }
        public int TimeRemaining => _timeRemaining;
        public int TimeElapsed => 0;
    }

    private class FakeMatchInfo : MatchInfo
    {
        private readonly bool _wasUserInitiated;
        public FakeMatchInfo(bool wasUserInitiated) { _wasUserInitiated = wasUserInitiated; }
        public bool wasUserInitiatedMatch() => _wasUserInitiated;
    }
}

[TestFixture]
public class PowerIconVocabularyTests
{
    [Test]
    public void EveryCatalogPowerHasAnExplicitIconMeaning()
    {
        foreach (var power in PowerOfferBuilder.Catalog())
        {
            var icon = PowerUpIconLibrary.Describe(power);
            Assert.That(icon.Subject, Is.Not.EqualTo("POWER"), power.Name);
            Assert.That(icon.Effect, Is.Not.EqualTo("?"), power.Name);
            Assert.That(icon.Animal.HasValue || !string.IsNullOrWhiteSpace(icon.Subject), Is.True, power.Name);
        }
    }

    [Test]
    public void AnimalDoubleAndCollectionUseTheActualAnimalAndDifferentActionBadges()
    {
        foreach (ItemType animal in System.Enum.GetValues(typeof(ItemType)))
        {
            var doubled = PowerUpIconLibrary.Describe(new AnimalDoublePower(animal));
            var bloom = PowerUpIconLibrary.Describe(new CollectionPower(CollectionStyle.Transform,animal));
            var collector = PowerUpIconLibrary.Describe(new CollectionPower(CollectionStyle.AnimalCredit,animal));
            var admission = PowerUpIconLibrary.Describe(new CollectionPower(CollectionStyle.Admission,animal));
            Assert.That(doubled.Animal, Is.EqualTo(animal));
            Assert.That(bloom.Animal, Is.EqualTo(animal));
            Assert.That(collector.Animal, Is.EqualTo(animal));
            Assert.That(admission.Animal, Is.EqualTo(animal));
            Assert.That(new[] { doubled.Effect, bloom.Effect, collector.Effect, admission.Effect }.Distinct().Count(), Is.EqualTo(4));
        }
    }

    [Test]
    public void HabitatTiersAndShapeRewardsKeepTheirMeaningWithoutAdvancingGrowth()
    {
        foreach (HabitatStyle style in System.Enum.GetValues(typeof(HabitatStyle)))
        {
            var engine = new HabitatPower(style,2);
            var icon = PowerUpIconLibrary.Describe(engine);
            Assert.That(icon.Subject, Is.EqualTo(style.ToString().ToUpperInvariant()));
            Assert.That(icon.Growth, Is.True);
            Assert.That(engine.FlatMultiplierBonus, Is.Zero);
            Assert.That(PowerUpIconLibrary.Describe(new HabitatPower(style,0)).Growth, Is.False);
        }
        foreach (BlockShape shape in System.Enum.GetValues(typeof(BlockShape)))
            if (shape != BlockShape.Any)
                Assert.That(PowerUpIconLibrary.Describe(new CollectionPower(CollectionStyle.ShapeCredit,shape:shape)).Subject,
                    Is.EqualTo(shape.ToString().ToUpperInvariant()));
        Assert.That(PowerUpIconLibrary.Describe(new FourExactlyPower()).Effect, Is.EqualTo("+\u00D7"));
    }

    [Test]
    public void AnimalBonusesKeepSubjectAndDistinguishEffect()
    {
        var points = PowerUpIconLibrary.Describe(new IconTypeGivesBase(ItemType.Bear, 5));
        var multi = PowerUpIconLibrary.Describe(new IconTypeGivesMulti(ItemType.Bear, 2));
        Assert.AreEqual(ItemType.Bear, points.Animal);
        Assert.AreEqual(points.Animal, multi.Animal);
        Assert.AreEqual("+", points.Effect);
        Assert.AreEqual("\u00D7", multi.Effect);
        Assert.IsFalse(points.Growth);
    }

    [Test]
    public void GrowthShowsBothBonusesWithoutEvaluatingScoring()
    {
        var power = new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 2, 10, 1);
        var icon = PowerUpIconLibrary.Describe(power);
        Assert.AreEqual(ItemType.Duck, icon.Animal);
        Assert.AreEqual("+\u00D7", icon.Effect);
        Assert.IsTrue(icon.Growth);
    }

    [Test]
    public void UnknownPowerUsesNeutralPlaceholder()
    {
        var icon = PowerUpIconLibrary.Describe(null);
        Assert.IsNull(icon.Animal);
        Assert.AreEqual("POWER", icon.Subject);
        Assert.AreEqual("?", icon.Effect);
    }
}

[TestFixture]
public class BlockPowerTests
{
    [Test]
    public void OffersAndFallbacksRespectGoalAndAnimalPool()
    {
        var items = new List<ItemType> { ItemType.Duck };
        var source = PowerUpLibrary.GetAll();
        source.AddRange(PowerUpLibrary.BlockPowers());
        foreach (var goal in new[] { GoalType.Score, GoalType.BlocksCleared })
        foreach (var mode in new[] { FreePlayMode.Timed, FreePlayMode.LimitedMoves })
        {
            var offers = PowerUpSelector.BuildChoices(source, new HashSet<string>(), items, 100,
                new System.Random(1), goal, mode);
            foreach (var power in offers)
            {
                Assert.That(power.Supports(goal, mode), Is.True);
                if (power is BlockClearPower block && block.Style == BlockClearStyle.Animal)
                    Assert.That(block.Animal, Is.EqualTo(ItemType.Duck));
            }
            var fallback = PowerUpSelector.BuildChoices(new List<PowerUp>(), new HashSet<string>(), items, 3,
                new System.Random(1), goal, mode);
            Assert.That(fallback.Count, Is.EqualTo(3));
            foreach (var power in fallback) Assert.That(power.Supports(goal, mode), Is.True);
        }
    }

    [Test]
    public void SharedExtraClearsKeepFullImmediateAndQueuedRewards()
    {
        var power = new IconTypeGivesMulti(ItemType.Duck, 5);
        var result = new ScoreResult();
        SharedPowerRewards.Resolve(power, 4, true, 0, 5.5f, 1, 5, result);
        Assert.That(result.ExtraClears[power], Is.EqualTo(5));
        var next = new ScoreResult();
        SharedPowerRewards.Resolve(power, 4, true, 0, 0, 1, 1, next);
        Assert.That(next.ExtraClears[power], Is.EqualTo(4));
        var empty = new ScoreResult();
        SharedPowerRewards.Resolve(power, 4, true, 0, 0, 1, 1, empty);
        Assert.That(empty.ExtraClears, Is.Empty, "Queued rewards are consumed once; fractional progress remains.");
    }

    [Test]
    public void BonusClearsStayAdjacentUniqueAndRespectTrigger()
    {
        var board = new BoardModel(3, 3);
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) board[x, y] = ItemType.Bear;
        var group = new List<BoardModel.Cell> { new(0, 0), new(1, 0), new(2, 0) };
        var groups = new List<List<BoardModel.Cell>> { group };
        var powers = new List<PowerUp> { new BlockClearPower(BlockClearStyle.Neighbor),
            new BlockClearPower(BlockClearStyle.Animal, ItemType.Bear), new BlockClearPower(BlockClearStyle.Cascade) };
        var cells = BlockClearPower.ExpandClears(board, groups, powers, true);
        Assert.That(cells.Count, Is.EqualTo(6), "Only the three adjacent cells are eligible; bonus cells cannot spread the effect.");
        Assert.That(new HashSet<BoardModel.Cell>(cells).Count, Is.EqualTo(cells.Count));
        foreach (var cell in cells) Assert.That(cell.Y, Is.LessThanOrEqualTo(1));
        Assert.That(BlockClearPower.ExpandClears(board, groups,
            new[] { new BlockClearPower(BlockClearStyle.Cascade) }, false).Count, Is.EqualTo(3));
        Assert.That(BlockClearPower.ExpandClears(board, groups,
            new[] { new BlockClearPower(BlockClearStyle.LargeMatch) }, false).Count, Is.EqualTo(3));
        Assert.That(BlockClearPower.ExpandClears(board, groups,
            new[] { new BlockClearPower(BlockClearStyle.Animal, ItemType.Duck) }, false).Count, Is.EqualTo(3));
    }

    private sealed class TimedOnlyPower : BasePowerUp
    {
        public TimedOnlyPower() : base(1) { }
        public override PowerModes SupportedModes => PowerModes.Timed;
        public override PowerUp Clone() => new TimedOnlyPower();
    }

    [Test]
    public void ModeSpecificPowersAreExcludedFromOtherModes()
    {
        var offers = PowerUpSelector.BuildChoices(new[] { new TimedOnlyPower() }, new HashSet<string>(),
            new List<ItemType>(), 3, new System.Random(1), GoalType.Score, FreePlayMode.LimitedMoves);
        Assert.That(offers.Exists(power => power is TimedOnlyPower), Is.False);
    }
}

[TestFixture]
public class RoundOfferTagTests
{
    [Test]
    public void AnimalTradeoffOfferRequiresBothTagsAndNeverFallsBackToStarters()
    {
        var items = new List<ItemType>((ItemType[])System.Enum.GetValues(typeof(ItemType)));
        var tags = PowerTags.Animal | PowerTags.Tradeoff;
        var choices = PowerUpSelector.BuildChoices(PowerUpLibrary.GetAll(), new HashSet<string>(), items,
            3, new System.Random(4), requiredTags: tags);
        Assert.That(choices.Count, Is.EqualTo(3));
        foreach (var power in choices) Assert.That((power.Tags & tags), Is.EqualTo(tags));
        var empty = PowerUpSelector.BuildChoices(PowerUpLibrary.Starters(), new HashSet<string>(), items,
            3, new System.Random(4), requiredTags: tags);
        Assert.That(empty, Is.Empty);
        var level = (CampaignLevel)new CampaignLevel(99, "Legacy tags", "", new FreePlayConfig(),
            roundOffers: new Dictionary<int, PowerTags> { [3] = tags }).CloneNew();
        Assert.That(level.RequiredPowerTags(3), Is.EqualTo(tags));
        Assert.That(level.RequiredPowerTags(2), Is.EqualTo(PowerTags.None));
        Assert.That(level.PowerOffers.ForRound(3).Slots[0].Query.All, Is.EqualTo(tags));
        Assert.That(level.PowerOffers.ForRound(2), Is.Null);
    }
}

[TestFixture]
public class RosterTradeoffTests
{
    [Test]
    public void ScoreAdjustmentsAndEligibilityFollowTheTradeoff()
    {
        var add = new RosterTradeoffPower(ItemType.Moose, true);
        var remove = new RosterTradeoffPower(ItemType.Duck, false);
        var pool = new List<ItemType> { ItemType.Bear, ItemType.Duck, ItemType.Walrus, ItemType.Sloth };
        Assert.That(add.SupportsItems(pool), Is.True);
        Assert.That(remove.SupportsItems(pool), Is.True);
        Assert.That(add.AdjustFinalScore(null, 100), Is.EqualTo(125));
        Assert.That(remove.AdjustFinalScore(null, 101), Is.EqualTo(76));
        Assert.That(remove.AdjustFinalScore(null, int.MaxValue), Is.EqualTo(int.MaxValue-int.MaxValue/4));
        Assert.That(add.AdjustFinalScore(null, int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.That(remove.CanOfferWith(new PowerUp[] { new IconTypeGivesMulti(ItemType.Duck, 2) }), Is.False);
        Assert.That(remove.CanOfferWith(new PowerUp[] { new AnimalPairPower(ItemType.Duck, ItemType.Walrus) }), Is.False);
        Assert.That(add.CanOfferWith(new PowerUp[] { add.Clone() }), Is.False);
        Assert.That(remove.SupportsItems(new List<ItemType> { ItemType.Duck, ItemType.Bear, ItemType.Walrus }), Is.False);
        Assert.That(add.Supports(GoalType.BlocksCleared, FreePlayMode.Timed), Is.True);
    }
}

[TestFixture]
public class MatchFoundationPowerTests
{
    private sealed class MockMoveContext : MatchInfo, PlayerMoveInfo
    {
        public bool User = true;
        public int MoveId { get; set; } = 1;
        public bool BothSwapTilesMatched => false;
        public bool AllPlayerGroupsExactlyFour => false;
        public int BoardWidth => 6;
        public int BoardHeight => 7;
        public bool wasUserInitiatedMatch() => User;
    }

    private static List<Tile> MakeTiles(int count)
    {
        var list = new List<Tile>();
        for (int i = 0; i < count; i++)
        {
            var t = new GameObject().AddComponent<Tile>();
            t.x = i; t.y = 0;
            list.Add(t);
        }
        return list;
    }

    [Test]
    public void StreakFoundationAwardsPermanentMultiplierAndIgnoresCascades()
    {
        var power = new MatchFoundationPower(MatchFoundationPower.FoundationStyle.StreakFive);
        var context = new MockMoveContext();
        power.MatchInfo = context;

        Assert.That(power.Minimum, Is.EqualTo(3));
        Assert.That(power.EarnedMultiplier, Is.Zero);

        var fiveTiles = MakeTiles(5);
        var threeTiles = MakeTiles(3);

        // 3 matches of 5 in a row
        for (int i = 0; i < 3; i++)
        {
            power.AdditiveMultiplierMod(fiveTiles);
            Assert.That(power.Streak, Is.EqualTo(i + 1));
            Assert.That(power.EarnedMultiplier, Is.Zero);
        }

        // A cascade occurs: should NOT increment and should NOT reset streak!
        context.User = false;
        power.AdditiveMultiplierMod(threeTiles);
        Assert.That(power.Streak, Is.EqualTo(3));
        Assert.That(power.EarnedMultiplier, Is.Zero);

        // 4th match of 5 by player
        context.User = true;
        power.AdditiveMultiplierMod(fiveTiles);
        Assert.That(power.Streak, Is.Zero);
        Assert.That(power.EarnedMultiplier, Is.EqualTo(1));
        Assert.That(power.FlatMultiplierBonus, Is.EqualTo(1));

        // A player match of less than 5 resets streak
        power.AdditiveMultiplierMod(fiveTiles);
        Assert.That(power.Streak, Is.EqualTo(1));
        power.AdditiveMultiplierMod(threeTiles);
        Assert.That(power.Streak, Is.Zero);
        Assert.That(power.EarnedMultiplier, Is.EqualTo(1)); // earned permanent multi remains
    }

    [Test]
    public void SpeedFoundationAwardsBasePointsAndIgnoresCascades()
    {
        var power = new MatchFoundationPower(MatchFoundationPower.FoundationStyle.SpeedFive);
        var context = new MockMoveContext();
        power.MatchInfo = context;

        Assert.That(power.Minimum, Is.EqualTo(3));
        Assert.That(power.EarnedBasePoints, Is.Zero);

        var fourTiles = MakeTiles(4);

        // 4 rapid matches
        for (int i = 0; i < 4; i++)
            power.AdditiveBaseMod(fourTiles);
        Assert.That(power.EarnedBasePoints, Is.Zero);

        // Cascade match should not count
        context.User = false;
        power.AdditiveBaseMod(fourTiles);
        Assert.That(power.EarnedBasePoints, Is.Zero);

        // 5th player match triggers +2 base points per tile
        context.User = true;
        int added = power.AdditiveBaseMod(fourTiles);
        Assert.That(power.EarnedBasePoints, Is.EqualTo(2));
        Assert.That(power.FlatBasePerTile, Is.EqualTo(2));
        Assert.That(added, Is.EqualTo(4 * 2));
    }

    [Test]
    public void GiantFoundationRequiresFiveAndAwardsBasePointsOnSixPlus()
    {
        var power = new MatchFoundationPower(MatchFoundationPower.FoundationStyle.GiantSix);
        var context = new MockMoveContext();
        power.MatchInfo = context;

        Assert.That(power.Minimum, Is.EqualTo(5));
        Assert.That(power.EarnedBasePoints, Is.Zero);

        var fiveTiles = MakeTiles(5);
        var sixTiles = MakeTiles(6);

        // Match of 5 does not count toward 6+ requirement
        power.AdditiveBaseMod(fiveTiles);
        Assert.That(power.GiantMatchCount, Is.Zero);

        // 1st match of 6
        power.AdditiveBaseMod(sixTiles);
        Assert.That(power.GiantMatchCount, Is.EqualTo(1));
        Assert.That(power.EarnedBasePoints, Is.Zero);

        // Cascade does not count
        context.User = false;
        power.AdditiveBaseMod(sixTiles);
        Assert.That(power.GiantMatchCount, Is.EqualTo(1));

        // 2nd match of 6 by player triggers +1 base point per tile
        context.User = true;
        int added = power.AdditiveBaseMod(sixTiles);
        Assert.That(power.GiantMatchCount, Is.Zero);
        Assert.That(power.EarnedBasePoints, Is.EqualTo(1));
        Assert.That(power.FlatBasePerTile, Is.EqualTo(1));
        Assert.That(added, Is.EqualTo(6 * 1));
    }
}
