using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class WildReleaseLevelTests
{
    private static BoardModel Make()
    {
        var board=new BoardModel(7,7,new[]{new MatchRule(MatchType.Snake,4)});
        board.Strategy=new StrategyLevelState(LevelBonus.WildRelease);
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=(x+y)%2==0?ItemType.Duck:ItemType.Bear;
        board.Strategy.Initialize(board,new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe});return board;
    }
    private static List<BoardModel.Cell> Beside()=>Enumerable.Range(0,4).Select(x=>new BoardModel.Cell(x,2)).ToList();
    private static void Charge(BoardModel board,int count)
    { for(int i=0;i<count;i++)board.Strategy.PrepareWave(board,new(){Beside()},null); }

    [Test]
    public void CageStartsClosedAndAlwaysReservesExactlyOneSpace()
    {
        var board=Make();var s=board.Strategy;
        Assert.That(s.CageMatches,Is.Zero);Assert.That(s.CageOpen,Is.False);
        Assert.That(board.Connected(s.ReleaseCage.X,s.ReleaseCage.Y),Is.Empty);
        Assert.That(Enumerable.Range(0,49).Count(n=>board.IsBlocked(new(n%7,n/7))),Is.EqualTo(1));
        Assert.That(s.CaptureCageAnimal(board,new(0,0),s.ReleaseCage),Is.Empty);
        Assert.That(s.CageCapturePending,Is.False);
    }

    [Test]
    public void EachAdjacentGroupChargesOnceIncludingCascadesAndStopsAtTen()
    {
        var board=Make();var s=board.Strategy;
        var diagonal=new List<BoardModel.Cell>{new(0,1),new(1,1),new(2,1),new(2,2)};
        s.PrepareWave(board,new(){diagonal},null);Assert.That(s.CageMatches,Is.Zero);
        var touching=Beside();s.PrepareWave(board,new(){touching},null);
        Assert.That(s.CageMatches,Is.EqualTo(1),"Four tiles count as one touching match.");
        var other=new List<BoardModel.Cell>{new(4,3),new(5,3),new(6,3),new(6,4)};
        s.PrepareWave(board,new(){touching,other},new HashSet<BoardModel.Cell>(touching));
        Assert.That(s.CageMatches,Is.EqualTo(3),"Distinct groups, including cascades, count separately.");
        Charge(board,6);Assert.That(s.CageOpen,Is.False);Assert.That(s.CageMatches,Is.EqualTo(9));
        Charge(board,1);Assert.That(s.CageOpen,Is.True);Assert.That(s.PowerTriggered,Is.True);
        Charge(board,4);Assert.That(s.CageMatches,Is.EqualTo(10));
    }

    [Test]
    public void FeedingCapturesEveryVisibleAnimalAsOneSnapshotAndMovesTheCageAfterClear()
    {
        var board=Make();var s=board.Strategy;var old=s.ReleaseCage;
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=ItemType.Bear;
        var ducks=new List<BoardModel.Cell>{new(0,0),new(6,0),new(1,5),new(5,6)};
        foreach(var c in ducks)board[c.X,c.Y]=ItemType.Duck;
        board[old.X,old.Y]=ItemType.Duck;Charge(board,10);
        Assert.That(s.CanFeedCage(board,ducks[0],old),Is.True);
        Assert.That(s.CanFeedCage(board,ducks[0],new(0,1)),Is.False);
        var captured=s.CaptureCageAnimal(board,ducks[0],old);
        Assert.That(captured,Is.EquivalentTo(ducks),"Disconnected animals join the same scored group; the empty cage does not.");
        Assert.That(s.CageCapturePending,Is.True);Assert.That(s.CageOpen,Is.False);
        Assert.That(s.CaptureCageAnimal(board,ducks[0],old),Is.Empty,"No second feed while resolving.");
        Assert.That(s.Reward(board,captured).Apply(100),Is.EqualTo(100),"Use full ordinary scoring, without a hidden multiplier.");
        s.PrepareWave(board,new(){captured},new HashSet<BoardModel.Cell>(captured));
        Assert.That(s.AfterClear(board,captured),Is.True);
        Assert.That(s.ReleaseCage,Is.Not.EqualTo(old));Assert.That(s.CageMatches,Is.Zero);
        Assert.That(captured.Contains(s.ReleaseCage),Is.False);
        Assert.That(s.IsProtected(old),Is.False);Assert.That(s.IsProtected(s.ReleaseCage),Is.True);
    }

    [Test]
    public void WholeBoardReleaseStillCreatesAnotherCageAndRefillDoesNotMoveIt()
    {
        var board=Make();var s=board.Strategy;var old=s.ReleaseCage;
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=ItemType.Duck;
        Charge(board,10);var captured=s.CaptureCageAnimal(board,new(0,0),old);
        Assert.That(captured.Count,Is.EqualTo(48));s.AfterClear(board,captured);
        Assert.That(s.ReleaseCage,Is.Not.EqualTo(old));var next=s.ReleaseCage;
        board.Refill(captured.Append(old),ReplacementStrategy.InPlace,()=>ItemType.Bear);
        Assert.That(s.ReleaseCage,Is.EqualTo(next));Assert.That(s.CageMatches,Is.Zero);
        Assert.That(board[next.X,next.Y],Is.EqualTo(ItemType.Bear));
    }

    [Test]
    public void CancelledFeedReopensTheSameChargedCage()
    {
        var board=Make();var s=board.Strategy;Charge(board,10);var cage=s.ReleaseCage;
        s.CaptureCageAnimal(board,new(0,0),cage);Assert.That(s.CageCapturePending,Is.True);
        s.CancelCageCapture();Assert.That(s.CageOpen,Is.True);Assert.That(s.ReleaseCage,Is.EqualTo(cage));
        Assert.That(s.CageMatches,Is.EqualTo(10));
    }

    [Test]
    public void CageRemovesCapturedTypeOnlyAfterClearAndStopsAtThreeUntilRoundReset()
    {
        var board=Make();var s=board.Strategy;Charge(board,10);
        var captured=s.CaptureCageAnimal(board,new(0,0),s.ReleaseCage);
        Assert.That(s.CapturedAnimals,Is.Empty);
        s.CancelCageCapture();Assert.That(s.CapturedAnimals,Is.Empty);Assert.That(s.PendingCageAnimal,Is.Null);
        captured=s.CaptureCageAnimal(board,new(0,0),s.ReleaseCage);s.AfterClear(board,captured);
        Assert.That(s.CapturedAnimals,Is.EquivalentTo(new[]{ItemType.Duck}));
        Assert.That(s.CagePoolExhausted,Is.True);Charge(board,10);Assert.That(s.CageOpen,Is.False);
        Assert.That(s.CageMatches,Is.Zero);Assert.That(s.CaptureCageAnimal(board,new(1,0),s.ReleaseCage),Is.Empty);
        s.SetAnimals(new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe});
        Assert.That(s.CagePoolExhausted,Is.True,"Refreshing a roster must not reintroduce captured animals.");
        s.Initialize(board,new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe});
        Assert.That(s.CapturedAnimals,Is.Empty);Assert.That(s.CagePoolExhausted,Is.False);
        Charge(board,10);Assert.That(s.CanFeedCage(board,new(0,0),s.ReleaseCage),Is.True);
    }

    [Test]
    public void SweepsCannotClearTheCageAndFreshRoundsResetItsProgress()
    {
        var board=Make();var s=board.Strategy;var group=Beside();
        var cleared=BlockClearPower.ExpandClears(board,new(){group},new[]{new BlockClearPower(BlockClearStyle.LargeMatch)},false);
        Assert.That(cleared.Contains(s.ReleaseCage),Is.False);
        SharedPowerRewards.ExpandClears(board,group,cleared,100);Assert.That(cleared.Contains(s.ReleaseCage),Is.False);
        Charge(board,9);s.FinishPlay();Assert.That(s.CageMatches,Is.EqualTo(9));
        s.Initialize(board,new[]{ItemType.Duck,ItemType.Bear});
        Assert.That(s.CageMatches,Is.Zero);Assert.That(s.ReleaseCage,Is.EqualTo(new BoardModel.Cell(3,3)));
    }

    [Test]
    public void BriefingShowsChargingOpeningDraggingBoardWideClearAndNewCage()
    {
        var lesson=new LevelLessonStoryboard(LevelLessonKind.WildRelease);
        var locked=lesson.Sample(.5f);Assert.That(locked.Tiles.Single(t=>t.Cage).Badge,Is.EqualTo("8/10"));
        var open=lesson.Sample(3);Assert.That(open.Tiles.Single(t=>t.Cage).CageOpen,Is.True);
        var dragged=lesson.Sample(3.8f);Assert.That(dragged.Cell(0,0).IconX,Is.GreaterThan(0));
        var released=lesson.Sample(5.5f);
        Assert.That(released.Tiles.Where(t=>t.Visible&&t.Animal==ItemType.Duck).All(t=>t.IconScale==0),Is.True);
        var reset=lesson.Sample(6.5f);Assert.That(reset.Cell(4,2).Cage,Is.True);
        Assert.That(reset.Cell(4,2).Badge,Is.EqualTo("0/10"));Assert.That(reset.Cell(2,2).Cage,Is.False);
    }

    [Test]
    public void SharedLevelHasPointsFoundationAndCompleteGenericDraftsWithoutPowerCopies()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        var level=(CampaignLevel)LevelLibrary.GetLevel(31);
        Assert.That(level.Bonus,Is.EqualTo(LevelBonus.WildRelease));Assert.That(level.Goal,Is.EqualTo(GoalType.Score));
        Assert.That(level.BasePointsPerTile,Is.EqualTo(4));Assert.That(level.Settings.Moves,Is.EqualTo(36));
        var active=new List<PowerUp>();var selected=new HashSet<string>();
        for(int round=1;round<=level.GetTotalRounds;round++) {
            var result=PowerOfferBuilder.Build(level.PowerOffers.ForRound(round),PowerOfferBuilder.Catalog(),selected,
                Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList(),new Random(31+round),level.Goal,level.Mode,level.GetMatchRules(round),active,round);
            Assert.That(result.IsComplete,Is.True,string.Join("; ",result.Diagnostics));Assert.That(result.Choices.Count,Is.EqualTo(3));
            Assert.That(result.Choices.All(p=>SharedGameContent.Power(p)["levelId"]==null),Is.True);
            active.Add(result.Choices[0]);selected.Add(result.Choices[0].Name);
        }
        Assert.That(LevelLessonStoryboard.ForLevel(level,level.GetMatchRules(1)).Kind,Is.EqualTo(LevelLessonKind.WildRelease));
    }
}

public class StrategyLevelTests
{
    [Test]
    public void GoldenRescueCountsCascadesRoundsEachMatchDownAndCapsAtEligibleAnimals()
    {
        var board=Make(LevelBonus.GoldenRescue);var s=board.Strategy;
        var five=Enumerable.Range(0,5).Select(x=>new BoardModel.Cell(x,0)).ToList();
        var three=Enumerable.Range(0,3).Select(x=>new BoardModel.Cell(x,6)).ToList();
        s.PrepareWave(board,new(){five,three},null);
        Assert.That(s.Caged.Count,Is.EqualTo(3),"Cascades count; each group rounds down independently.");
        s.Caged.Clear();
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)s.Golden.Add(new(x,y));
        s.Golden.Remove(new(6,6));
        s.PrepareWave(board,new(){five},null);
        Assert.That(s.Caged.SetEquals(new[]{new BoardModel.Cell(6,6)}),Is.True);
        Assert.That(s.Golden.Overlaps(s.Caged),Is.False);
    }

    [Test]
    public void GoldenRescueCagesHalfEachMatchAndBossCagesEveryTile()
    {
        foreach(bool boss in new[]{false,true}) {
            var board=Make(LevelBonus.GoldenRescue);board.Strategy.BossActive=boss;
            var six=Enumerable.Range(0,6).Select(x=>new BoardModel.Cell(x,0)).ToList();
            Player(board,six);
            Assert.That(board.Strategy.Caged.Count,Is.EqualTo(boss?6:3));
            Assert.That(board.Strategy.Caged.Any(six.Contains),Is.False);
            foreach(var cage in board.Strategy.Caged)Assert.That(board.Connected(cage.X,cage.Y),Is.Empty);
        }
    }

    [Test]
    public void GoldenRescueOnlyOpensOrthogonalOldCagesAndGoldCannotBeRecaged()
    {
        var board=Make(LevelBonus.GoldenRescue);var s=board.Strategy;
        var beside=new BoardModel.Cell(3,1);var diagonal=new BoardModel.Cell(4,1);
        s.Caged.Add(beside);s.Caged.Add(diagonal);
        Player(board,Row());
        Assert.That(s.Golden.Contains(beside),Is.True);Assert.That(s.Caged.Contains(beside),Is.False);
        Assert.That(s.Caged.Contains(diagonal),Is.True);
        Assert.That(s.Reward(board,new(){beside,new(6,6)}).Apply(2),Is.EqualTo(4));
        Assert.That(s.Golden.Overlaps(s.Caged),Is.False);
        var created=s.Caged.ToList();
        Assert.That(created.Except(new[]{diagonal}).Count(),Is.EqualTo(2),"New adjacent cages must survive this wave.");
    }

    [Test]
    public void GoldenRescueBuffFollowsSwapAndGravityButNotReplacementAnimals()
    {
        var board=Make(LevelBonus.GoldenRescue);var s=board.Strategy;
        var a=new BoardModel.Cell(0,0);var b=new BoardModel.Cell(6,5);
        s.Golden.Add(a);board.Swap(a,b);
        Assert.That(s.Golden.SetEquals(new[]{b}),Is.True);
        board.HasLegalMove();Assert.That(s.Golden.SetEquals(new[]{b}),Is.True,"Move probing must restore tile metadata.");
        board.Refill(new[]{new BoardModel.Cell(6,6)},ReplacementStrategy.South,()=>ItemType.Chicken);
        var fallen=new BoardModel.Cell(6,6);Assert.That(s.Golden.SetEquals(new[]{fallen}),Is.True);
        Assert.That(s.Reward(board,new(){fallen}).Apply(1),Is.EqualTo(3));
        s.AfterClear(board,new[]{fallen});board.Refill(new[]{fallen},ReplacementStrategy.InPlace,()=>ItemType.Duck);
        Assert.That(s.Golden,Is.Empty);Assert.That(s.Reward(board,new(){fallen}).Apply(1),Is.EqualTo(1));
        s.Golden.Add(a);s.Initialize(board,new[]{ItemType.Duck});Assert.That(s.Golden,Is.Empty);
    }

    private static BoardModel Make(LevelBonus bonus)
    {
        var board=new BoardModel(7,7,new[]{new MatchRule(MatchType.Snake,4)});
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=(x+y)%2==0?ItemType.Duck:ItemType.Bear;
        board.Strategy=new StrategyLevelState(bonus);
        board.Strategy.Initialize(board,new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken});
        return board;
    }
    private static List<BoardModel.Cell> Row(int y=0)=>Enumerable.Range(0,4).Select(x=>new BoardModel.Cell(x,y)).ToList();
    private static List<StrategyReward> Player(BoardModel board,params List<BoardModel.Cell>[] groups)=>
        board.Strategy.PrepareWave(board,groups.ToList(),new HashSet<BoardModel.Cell>(groups.SelectMany(g=>g)));

    [Test]
    public void MigrationFollowsTheTargetAndBossOnlyHalvesOtherAnimals()
    {
        var board=Make(LevelBonus.MigrationSeason);var s=board.Strategy;var row=Row();
        foreach(var c in row)board[c.X,c.Y]=ItemType.Duck;
        Assert.That(Player(board,row).Single().Apply(40),Is.EqualTo(80));
        s.BossActive=true;Assert.That(s.Reward(board,row).Apply(40),Is.EqualTo(80));
        foreach(var c in row)board[c.X,c.Y]=ItemType.Bear;
        Assert.That(Player(board,row).Single().Apply(40),Is.EqualTo(20));Assert.That(s.BossTriggered,Is.True);
        board[0,0]=board[1,0]=ItemType.Duck;
        Assert.That(s.Reward(board,row).Apply(40),Is.EqualTo(50),"Mixed-animal pact matches use each animal's actual factor.");
        s.FinishPlay();Assert.That(s.Marked,Is.EqualTo(ItemType.Bear));
        s.SetAnimals(new[]{ItemType.Duck,ItemType.Chicken});Assert.That(s.Marked,Is.EqualTo(ItemType.Duck));
    }

    [Test]
    public void VaultNeedsTwoHitsOnEverySideAndBoostsOnlyRemainingTiles()
    {
        var board=Make(LevelBonus.CrackTheVault);var s=board.Strategy;
        var sides=new[]{new BoardModel.Cell(3,2),new BoardModel.Cell(4,3),new BoardModel.Cell(3,4),new BoardModel.Cell(2,3)};
        Assert.That(board.IsBlocked(s.Vault),Is.True);Assert.That(board.Connected(3,3),Is.Empty);
        for(int hit=0;hit<8;hit++) {
            var group=new List<BoardModel.Cell>{sides[hit/2],new(0,0),new(1,0),new(2,0)};
            Player(board,group);bool opened=s.AfterClear(board,group);
            Assert.That(opened,Is.EqualTo(hit==7));
            if(hit<7)Assert.That(s.Boosted,Is.Empty);
            else {Assert.That(s.Boosted.Count,Is.EqualTo(44));Assert.That(group.Any(s.Boosted.Contains),Is.False);}
        }
        Assert.That(s.VaultHits,Is.EqualTo(new[]{0,0,0,0}),"The vault can be charged again.");
        var boosted=s.Boosted.Take(4).ToList();Assert.That(s.Reward(board,boosted).Apply(4),Is.EqualTo(8));
        s.AfterClear(board,boosted);Assert.That(boosted.Any(s.Boosted.Contains),Is.False);
    }

    [Test]
    public void VaultBossErasesBoostsOnlyOnPlayerMatchesAndMixedCreditsStayExact()
    {
        var board=Make(LevelBonus.CrackTheVault);var s=board.Strategy;var group=Enumerable.Range(0,7).Select(x=>new BoardModel.Cell(x,0)).ToList();
        s.Boosted.Add(group[0]);Assert.That(s.Reward(board,group).Apply(7),Is.EqualTo(8));
        s.BossActive=true;
        s.PrepareWave(board,new(){group},null);Assert.That(s.Boosted.Count,Is.EqualTo(1));Assert.That(s.BossTriggered,Is.False);
        Assert.That(Player(board,group).Single().Apply(7),Is.EqualTo(7));Assert.That(s.Boosted,Is.Empty);Assert.That(s.BossTriggered,Is.True);
        foreach(var c in group)s.Boosted.Add(c);
        Assert.That(Player(board,group).Single().Apply(7),Is.EqualTo(14));Assert.That(s.Boosted.Count,Is.EqualTo(7));
    }

    [Test]
    public void SteppingStonesRewardOldGlowsBeforeExtendingAndBossMissesRemoveOne()
    {
        var board=Make(LevelBonus.SteppingStones);var s=board.Strategy;
        var touching=new List<BoardModel.Cell>{new(2,3),new(3,3),new(2,2),new(3,2)};
        Assert.That(Player(board,touching).Single().Apply(20),Is.EqualTo(60));
        Assert.That(s.Glowing.Contains(new(2,2)),Is.True);
        s.BossActive=true;int before=s.Glowing.Count;var miss=Row();
        Assert.That(Player(board,miss).Single().Apply(20),Is.EqualTo(20));
        Assert.That(s.Glowing.Count,Is.EqualTo(before-1));Assert.That(miss.Any(s.Glowing.Contains),Is.False);
        before=s.Glowing.Count;s.PrepareWave(board,new(){miss},null);
        Assert.That(s.Glowing.Count,Is.EqualTo(before),"Cascades do not invoke the boss's player-match penalty.");
    }

    [Test]
    public void RescueCagesAreBlockedAdjacentMatchesFreeThemAndThreeAnimalsEarnOneMove()
    {
        var board=Make(LevelBonus.AnimalRescue);var s=board.Strategy;
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board[x,y]=ItemType.Bear;
        board[6,6]=ItemType.Duck;s.BossActive=true;
        Player(board,Row());Assert.That(s.Caged.Contains(new(6,6)),Is.True);Assert.That(board.Connected(6,6),Is.Empty);
        Player(board,new List<BoardModel.Cell>{new(5,6),new(5,5),new(4,5),new(4,6)});Assert.That(s.Caged,Is.Empty);
        s.BossActive=false;
        for(int rescued=0;rescued<3;rescued++) {
            var group=Row(3);foreach(var c in group)board[c.X,c.Y]=s.Marked;
            Assert.That(Player(board,group).Single().Apply(4),Is.EqualTo(8));
            Assert.That(s.TakeMoves(),Is.EqualTo(rescued==2?1:0));s.FinishPlay();
        }
        Assert.That(s.TakeMoves(),Is.Zero);
    }

    [Test]
    public void CrosswindsWarnsAtTwoRotatesAtThreeAndOnlyItsCascadeChainGetsFiveTimes()
    {
        var board=Make(LevelBonus.Crosswinds);var s=board.Strategy;var group=Row();
        Player(board,group);Assert.That(s.CrossWarning,Is.False);
        s.PrepareWave(board,new(){group},null);Assert.That(s.CrossMatches,Is.EqualTo(1));
        Player(board,group);Assert.That(s.CrossWarning,Is.True);Assert.That(s.CrossPending,Is.False);
        Player(board,group);Assert.That(s.CrossPending,Is.True);Assert.That(s.Percent(board,group),Is.EqualTo(100));
        var cross=s.RotateCross(board,new Random(4));Assert.That(cross.Count,Is.EqualTo(13));Assert.That(cross.Distinct().Count(),Is.EqualTo(13));
        Assert.That(cross.All(c=>c.X==3||c.Y==3),Is.True);Assert.That(s.Direction,Is.EqualTo(ReplacementStrategy.West));
        Assert.That(s.Reward(board,group).Apply(20),Is.EqualTo(100));
        s.FinishPlay();Assert.That(s.Percent(board,group),Is.EqualTo(100));Assert.That(s.CrossMatches,Is.Zero);
        s.BossActive=true;var random=new Random(9);var directions=new HashSet<ReplacementStrategy>();
        for(int turn=0;turn<20;turn++) {
            for(int n=0;n<3;n++)Player(board,group);
            var previous=s.Direction;s.RotateCross(board,random);Assert.That(s.Direction,Is.Not.EqualTo(previous));directions.Add(s.Direction);s.FinishPlay();
        }
        Assert.That(directions.Count,Is.GreaterThan(2));
        s.Initialize(board,new[]{ItemType.Duck});Assert.That(s.CrossWarning,Is.False);Assert.That(s.CrossCascades,Is.False);Assert.That(s.Direction,Is.EqualTo(ReplacementStrategy.South));
    }

    [Test]
    public void FiveNewCampaignLevelsLoadCloneOfferPowersAndHaveDistinctLessonsAndCompatibleBosses()
    {
        var document=JObject.Parse(File.ReadAllText("Assets/Resources/Content/prismatch.json"));SharedGameContent.Load(document.ToString());LevelLibrary.ReloadSharedContent();
        var names=new[]{"Migration Season","Crack the Vault","Stepping Stones","Animal Rescue","Crosswinds","Golden Rescue"};
        for(int i=0;i<names.Length;i++) {
            var level=(CampaignLevel)LevelLibrary.GetLevel(25+i);Assert.That(level.Name,Is.EqualTo(names[i]));
            Assert.That(level.GetWidth(1),Is.EqualTo(7));Assert.That(level.GetHeight(1),Is.EqualTo(7));
            Assert.That(level.GetMatchRules(1).Single().minimumLength,Is.EqualTo(4));
            Assert.That(((CampaignLevel)level.CloneNew()).Bonus,Is.EqualTo(level.Bonus));
            Assert.That(level.GetBossPowersMap().GetBossPowersForRound(1),Is.Empty);
            var boss=(ChallengeBoss)level.GetBossPowersMap().GetBossPowersForRound(6).Single();
            Assert.That(ChallengeBoss.CanAffect(boss.Style,level.Goal,level.Mode,level.Bonus),Is.True);
            Assert.That(ChallengeBoss.CanAffect(boss.Style,level.Goal,level.Mode,LevelBonus.None),Is.False);
            Assert.That(level.GetPowerUpsForRound(1).Count,Is.GreaterThanOrEqualTo(3),"Tag pools provide candidates; the picker selects three cards.");
            for(int seed=0;seed<8;seed++) {
                var selected=new HashSet<string>();var owned=new List<PowerUp>();
                var animals=new List<ItemType>{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe,ItemType.Hippo,ItemType.Sloth,ItemType.Walrus};
                for(int round=1;round<=6;round++) {
                    var choices=PowerUpSelector.BuildChoices(level.GetPowerUpsForRound(round),selected,animals,3,new Random(seed*10+round),level.Goal,level.Mode,level.GetMatchRules(round),owned,allowFallback:false,round:round);
                    Assert.That(choices.Count,Is.EqualTo(3),level.Name+" round "+round);
                    var chosen=choices[seed%3];selected.Add(chosen.Name);owned.Add(chosen);
                }
            }
            Assert.That(level.PowerOffers.ForRound(3),Is.Not.Null,"Later rewards use a live tag offer; eligibility is enforced by the builder.");
            Assert.That(LevelLessonStoryboard.ForLevel(level,level.GetMatchRules(1)).Kind.ToString(),Is.EqualTo(level.Bonus.ToString()));
        }
    }
}


public class BlackoutLevelTests
{
    [Test] public void Level32LoadsWithBossDraftsAndBlackoutLesson()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        var level=(CampaignLevel)LevelLibrary.GetLevel(32);
        Assert.That(level.Bonus,Is.EqualTo(LevelBonus.Blackout));
        Assert.That(level.FinalBossStyle,Is.EqualTo(BossStyle.NightKeeper));
        var lesson=LevelLessonStoryboard.ForLevel(level,level.GetMatchRules(1));
        Assert.That(lesson.Kind,Is.EqualTo(LevelLessonKind.Blackout));
        var frame=lesson.Sample(2);
        Assert.That(frame.Tiles.Where(t=>t.Visible).All(t=>!t.IconVisible&&t.Tint==LessonTint.Hidden),Is.True);
        var active=new List<PowerUp>();var selected=new HashSet<string>();
        for(int round=1;round<=level.GetTotalRounds;round++) {
            var result=PowerOfferBuilder.Build(level.PowerOffers.ForRound(round),PowerOfferBuilder.Catalog(),selected,
                Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList(),new Random(32+round),level.Goal,level.Mode,level.GetMatchRules(round),active,round);
            Assert.That(result.IsComplete,Is.True,string.Join("; ",result.Diagnostics));
            active.Add(result.Choices[0]);selected.Add(result.Choices[0].Name);
        }
    }
    [Test] public void BlackoutProtectionStaysOnSquaresDuringGravity()
    {
        var b=Make(true);Darken(b);
        var moves=b.Refill(new[]{new BoardModel.Cell(2,4)},ReplacementStrategy.South,()=>ItemType.Bear);
        Assert.That(b.Strategy.ClearedDarkness.Contains(new(2,4)),Is.True);
        foreach(var move in moves.Where(m=>m.IsNew))Assert.That(b.Strategy.IsHidden(move.To),Is.False);
        Darken(b);Assert.That(b.Strategy.IsHidden(new(2,4)),Is.False);
    }
    private static BoardModel Make(bool boss=false)
    {
        var b=new BoardModel(5,5,new[]{new MatchRule(MatchType.Snake,4)});
        b.Strategy=new StrategyLevelState(LevelBonus.Blackout);
        for(int y=0;y<5;y++)for(int x=0;x<5;x++)b[x,y]=ItemType.Duck;
        b.Strategy.Initialize(b,new[]{ItemType.Duck,ItemType.Bear});b.Strategy.BossActive=boss;return b;
    }
    private static void Match(BoardModel b,bool player=true)
    {
        var group=new List<BoardModel.Cell>{new(0,1),new(1,1),new(2,1),new(3,1)};
        b.Strategy.PrepareWave(b,new(){group},player?new HashSet<BoardModel.Cell>(group):null);
    }
    private static void Darken(BoardModel b)
    {for(int i=0;i<5;i++){Match(b);b.Strategy.FinishPlay();}}
    [Test] public void FifthPlayerMatchHidesWholeBoardAfterPlayAndRepeats()
    {
        var b=Make();for(int i=0;i<4;i++){Match(b);b.Strategy.FinishPlay();}
        Assert.That(b.Strategy.Hidden,Is.Empty);Match(b);
        Assert.That(b.Strategy.Hidden,Is.Empty,"Blackout waits for the play to settle.");
        b.Strategy.FinishPlay();Assert.That(b.Strategy.Hidden.Count,Is.EqualTo(25));
        Assert.That(b.Strategy.BlackoutMatches,Is.Zero);
        b.Strategy.Hidden.Clear();Darken(b);Assert.That(b.Strategy.Hidden.Count,Is.EqualTo(25));
        b.Strategy.Initialize(b,new[]{ItemType.Duck});Assert.That(b.Strategy.Hidden,Is.Empty);
    }
    [Test] public void ReplacementsRevealOrthogonalNeighborsAndCascadesDoNotCount()
    {
        var b=Make();Darken(b);Match(b,false);
        b.Refill(new[]{new BoardModel.Cell(2,2)},ReplacementStrategy.InPlace,()=>ItemType.Bear);
        foreach(var c in new[]{new BoardModel.Cell(2,2),new(1,2),new(3,2),new(2,1),new(2,3)})
            Assert.That(b.Strategy.IsHidden(c),Is.False);
        Assert.That(b.Strategy.IsHidden(new(1,1)),Is.True,"Diagonal tiles stay hidden.");
        Assert.That(b.Strategy.BlackoutMatches,Is.Zero);
    }
    [Test] public void ClearedSquaresStaySafeAcrossBlackoutsAndResetEachRound()
    {
        var b=Make();var s=b.Strategy;
        b.Refill(new[]{new BoardModel.Cell(2,2)},ReplacementStrategy.InPlace,()=>ItemType.Bear);
        Assert.That(s.ClearedDarkness,Is.Empty,"Initially visible squares are not protected.");
        Darken(b);
        b.Refill(new[]{new BoardModel.Cell(2,2)},ReplacementStrategy.InPlace,()=>ItemType.Bear);
        Assert.That(s.ClearedDarkness.Count,Is.EqualTo(5));
        Darken(b);Assert.That(s.Hidden.Count,Is.EqualTo(20));
        Assert.That(s.ClearedDarkness.Any(s.IsHidden),Is.False);
        s.Initialize(b,new[]{ItemType.Duck});Assert.That(s.ClearedDarkness,Is.Empty);
    }
    [Test] public void BossRevealsOnlyReplacementSquaresAndProtectionSurvivesSwaps()
    {
        var b=Make(true);Darken(b);
        b.Refill(new[]{new BoardModel.Cell(2,2)},ReplacementStrategy.InPlace,()=>ItemType.Bear);
        Assert.That(b.Strategy.Hidden.Count,Is.EqualTo(24));
        b.Swap(new(2,2),new(2,1));
        Assert.That(b.Strategy.IsHidden(new(2,2)),Is.False);
        Assert.That(b.Strategy.IsHidden(new(2,1)),Is.True);
        Darken(b);Assert.That(b.Strategy.IsHidden(new(2,2)),Is.False);
        Assert.That(b.IsBlocked(new(2,2)),Is.False);
        Assert.That(ChallengeBoss.CanAffect(BossStyle.NightKeeper,GoalType.Score,FreePlayMode.LimitedMoves,LevelBonus.Blackout),Is.True);
        Assert.That(ChallengeBoss.CanAffect(BossStyle.NightKeeper,GoalType.Score,FreePlayMode.LimitedMoves,LevelBonus.None),Is.False);
    }
}

public class DarknessVariationTests
{
    private static readonly ItemType[] Pool={ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe};
    private static BoardModel Make(LevelBonus bonus)
    {
        var b=new BoardModel(5,4,new[]{new MatchRule(MatchType.Snake,4)});
        b.Strategy=new StrategyLevelState(bonus);
        for(int y=0;y<4;y++)for(int x=0;x<5;x++)b[x,y]=Pool[(x+y)%4];
        b.Strategy.Initialize(b,Pool);return b;
    }
    private static void Match(BoardModel b,bool player=true)
    {
        var g=new List<BoardModel.Cell>{new(0,1),new(1,1),new(2,1),new(3,1)};
        b.Strategy.PrepareWave(b,new(){g},player?new HashSet<BoardModel.Cell>(g):null);
    }
    [Test] public void SpotlightHidesExactlyTwoTypesAndChangesAfterFivePlayerMatches()
    {
        var b=Make(LevelBonus.AnimalSpotlight);var s=b.Strategy;
        Assert.That(s.SpotlightAnimals,Is.EqualTo(new[]{ItemType.Duck,ItemType.Bear}));
        for(int y=0;y<4;y++)for(int x=0;x<5;x++)Assert.That(s.IsHidden(new(x,y)),Is.EqualTo(s.SpotlightAnimals.Contains(b[x,y].Value)));
        for(int i=0;i<4;i++){Match(b);s.FinishPlay();}
        Match(b,false);s.FinishPlay();Assert.That(s.BlackoutMatches,Is.EqualTo(4));
        Match(b);Assert.That(s.SpotlightAnimals[0],Is.EqualTo(ItemType.Duck));s.FinishPlay();
        Assert.That(s.SpotlightAnimals,Is.EqualTo(new[]{ItemType.Chicken,ItemType.Giraffe}));
        Assert.That(s.BlackoutMatches,Is.Zero);
    }
    [Test] public void SpotlightChecksAnimalIdentityAfterSwapsReplacementsAndPoolChanges()
    {
        var b=Make(LevelBonus.AnimalSpotlight);var s=b.Strategy;
        b.Swap(new(0,0),new(2,0));Assert.That(s.IsHidden(new(0,0)),Is.False);Assert.That(s.IsHidden(new(2,0)),Is.True);
        b.Refill(new[]{new BoardModel.Cell(2,0)},ReplacementStrategy.InPlace,()=>ItemType.Giraffe);
        Assert.That(s.IsHidden(new(2,0)),Is.False,"Replacement animals outside the pair remain visible.");
        s.SetAnimals(new[]{ItemType.Bear,ItemType.Chicken,ItemType.Giraffe});
        Assert.That(s.SpotlightAnimals.Length,Is.EqualTo(2));Assert.That(s.SpotlightAnimals.Contains(ItemType.Duck),Is.False);
        s.Initialize(b,Pool);Assert.That(s.BlackoutMatches,Is.Zero);Assert.That(s.SpotlightAnimals[0],Is.EqualTo(ItemType.Duck));
    }
    [Test] public void RollingDarknessAlternatesRowsColumnsWrapsAndIgnoresCascades()
    {
        var b=Make(LevelBonus.RollingDarkness);var s=b.Strategy;
        Assert.That(s.Hidden,Is.Empty);Match(b,false);s.FinishPlay();Assert.That(s.DarknessLines,Is.Zero);
        Match(b);Assert.That(s.Hidden,Is.Empty);s.FinishPlay();
        Assert.That(s.Hidden.Count,Is.EqualTo(2));Assert.That(s.Hidden.All(c=>c.Y==0),Is.True);
        Match(b);s.FinishPlay();Assert.That(s.Hidden.Count,Is.EqualTo(4));
        Match(b);s.FinishPlay();Assert.That(s.Hidden.Count,Is.EqualTo(6));
        Assert.That(s.Status,Does.Contain("COLUMN 1"));
        for(int i=0;i<10;i++){Match(b);s.FinishPlay();}
        Assert.That(s.Hidden.Count,Is.EqualTo(20));
        s.Initialize(b,Pool);Assert.That(s.Hidden,Is.Empty);Assert.That(s.DarknessLines,Is.Zero);
    }
    [Test] public void RollingCountsUniquePlayerTilesCarriesOddTilesAndResetsRemainder()
    {
        var b=Make(LevelBonus.RollingDarkness);var s=b.Strategy;
        var group=Enumerable.Range(0,5).Select(x=>new BoardModel.Cell(x,1)).ToList();
        for(int i=0;i<2;i++) {
            s.PrepareWave(b,new(){group,group},new HashSet<BoardModel.Cell>(group));s.FinishPlay();
            Assert.That(s.Hidden.Count,Is.EqualTo(i==0?2:5));
        }
        s.Initialize(b,Pool);
        s.PrepareWave(b,new(){group},null);s.FinishPlay();Assert.That(s.Hidden,Is.Empty);
        s.PrepareWave(b,new(){group},new HashSet<BoardModel.Cell>(group));s.FinishPlay();
        Assert.That(s.Hidden.Count,Is.EqualTo(2));
        s.Initialize(b,Pool);Match(b);s.FinishPlay();Assert.That(s.Hidden.Count,Is.EqualTo(2));
    }
    [Test] public void RollingMatchesRevealNewTilesAndOrthogonalNeighbors()
    {
        var b=Make(LevelBonus.RollingDarkness);var s=b.Strategy;
        for(int i=0;i<10;i++){Match(b);s.FinishPlay();}
        b.Refill(new[]{new BoardModel.Cell(2,1)},ReplacementStrategy.InPlace,()=>ItemType.Duck);
        foreach(var c in new[]{new BoardModel.Cell(2,1),new(1,1),new(3,1),new(2,0),new(2,2)})Assert.That(s.IsHidden(c),Is.False);
        Assert.That(s.IsHidden(new(1,0)),Is.True);
    }
    [Test] public void BothNewLevelsLoadWithDistinctLessonsAndCompleteDrafts()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        foreach(int number in new[]{33,34}) {
            var level=(CampaignLevel)LevelLibrary.GetLevel(number);
            Assert.That(level.Bonus,Is.EqualTo(number==33?LevelBonus.AnimalSpotlight:LevelBonus.RollingDarkness));
            var lesson=LevelLessonStoryboard.ForLevel(level,level.GetMatchRules(1));
            Assert.That(lesson.Kind,Is.EqualTo(number==33?LevelLessonKind.AnimalSpotlight:LevelLessonKind.RollingDarkness));
            Assert.That(lesson.Sample(2).Tiles.Any(t=>t.Visible&&!t.IconVisible),Is.True);
            var active=new List<PowerUp>();var selected=new HashSet<string>();
            for(int round=1;round<=level.GetTotalRounds;round++) {
                var result=PowerOfferBuilder.Build(level.PowerOffers.ForRound(round),PowerOfferBuilder.Catalog(),selected,
                    Pool,new Random(number+round),level.Goal,level.Mode,level.GetMatchRules(round),active,round);
                Assert.That(result.IsComplete,Is.True,string.Join("; ",result.Diagnostics));
                active.Add(result.Choices[0]);selected.Add(result.Choices[0].Name);
            }
        }
    }
}

public class PowerGoalEquivalenceTests
{
    [Test] public void PactsHalveAffectedFinalWaveCreditsRoundDownAndLeaveOtherAnimalsAlone()
    {
        var pact=new AnimalPairPower(ItemType.Duck,ItemType.Walrus);
        Assert.That(pact.SupportedGoals,Is.EqualTo(PowerGoals.Both));
        Assert.That(AnimalPairPower.ApplyBlockPenalty(1,new[]{ItemType.Duck},new[]{pact}),Is.Zero);
        Assert.That(AnimalPairPower.ApplyBlockPenalty(5,new[]{ItemType.Walrus},new[]{pact}),Is.EqualTo(2));
        Assert.That(AnimalPairPower.ApplyBlockPenalty(5+5,new[]{ItemType.Duck},new[]{pact}),Is.EqualTo(5));
        Assert.That(AnimalPairPower.ApplyBlockPenalty(10,new[]{ItemType.Bear},new[]{pact}),Is.EqualTo(10));
        Assert.That(AnimalPairPower.ApplyBlockPenalty(10,new[]{ItemType.Duck,ItemType.Walrus},new[]{pact,pact}),Is.EqualTo(5));
        var other=new AnimalPairPower(ItemType.Bear,ItemType.Sloth);
        Assert.That(AnimalPairPower.ApplyBlockPenalty(10,new[]{ItemType.Duck,ItemType.Bear},new[]{pact,other}),Is.EqualTo(2));
    }
    [Test] public void RosterQuarterUsesFinalTotalRoundsAdjustmentDownAndCombinesWithoutOrderDependence()
    {
        var welcome=new RosterTradeoffPower(ItemType.Moose,true);
        var farewell=new RosterTradeoffPower(ItemType.Duck,false);
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(5+5,new[]{welcome}),Is.EqualTo(12));
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(5+5,new[]{farewell}),Is.EqualTo(8));
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(3,new[]{farewell}),Is.EqualTo(3));
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(8,new[]{welcome}),Is.EqualTo(10),"Round once after all rewards, not per sub-group.");
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(10,new[]{welcome,farewell}),Is.EqualTo(10));
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(10,new[]{farewell,welcome}),Is.EqualTo(10));
        Assert.That(RosterTradeoffPower.ApplyBlockRewards(int.MaxValue,new[]{welcome}),Is.EqualTo(int.MaxValue));
        Assert.That(welcome.AdjustFinalScore(null,10),Is.EqualTo(12));
        Assert.That(farewell.AdjustFinalScore(null,10),Is.EqualTo(8));
    }
    private sealed class Move : MatchInfo,PlayerMoveInfo
    {
        public int MoveId {get;set;}=1;
        public bool Player=true;
        public bool wasUserInitiatedMatch()=>Player;
        public bool AllPlayerGroupsExactlyFour=>true;
        public bool BothSwapTilesMatched=>false;
        public int BoardWidth=>7;
        public int BoardHeight=>7;
    }
    private static void Load()=>SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
    [Test] public void CatalogGoalFlagsAgreeWithRuntimeAndEverySingleGoalPowerHasAReviewReason()
    {
        Load();SharedGameContent.ValidateCoverage();
        var doc=SharedGameContent.Document;var powers=PowerOfferBuilder.Catalog();
        int both=0,flagged=0;
        foreach(var definition in doc["powers"].Cast<JObject>()) {
            var p=SharedGameContent.Find((string)definition["id"],powers);
            Assert.That(p,Is.Not.Null,(string)definition["name"]);
            Assert.That(p.SupportedGoals.ToString(),Is.EqualTo((string)definition["goals"]),p.Name);
            if(p.SupportedGoals==PowerGoals.Both){both++;Assert.That(definition["goalReview"],Is.Null);}
            else{flagged++;Assert.That((string)definition["goalReview"]?["reason"],Is.Not.Null.And.Not.Empty,p.Name);}
            foreach(var goal in new[]{GoalType.Score,GoalType.BlocksCleared}) {
                var clone=p.Clone().ForGoal(goal);Assert.That(clone.RewardGoal,Is.EqualTo(goal));
                Assert.That(clone.PowerUpDescription,Is.Not.Null.And.Not.Empty,p.Name);
            }
        }
        Assert.That(both,Is.EqualTo(169));Assert.That(flagged,Is.EqualTo(52));
    }
    [Test] public void CollectionPointsCountOnlyQualifiedAnimalsOrShapesAndDoNotLeakIntoBlocks()
    {
        var cells=new List<BlockMatchCell>{new(0,0,ItemType.Duck),new(1,0,ItemType.Bear),new(0,1,ItemType.Duck),new(1,1,ItemType.Bear)};
        var collector=new CollectionPower(CollectionStyle.AnimalCredit,ItemType.Duck);
        Assert.That(collector.PointBaseReward(cells),Is.EqualTo(8));
        var dividend=new CollectionPower(CollectionStyle.ShapeCredit,shape:BlockShape.Square);
        Assert.That(dividend.PointBaseReward(cells),Is.EqualTo(16));
        cells.RemoveAt(3);Assert.That(dividend.PointBaseReward(cells),Is.Zero);
        collector.ForGoal(GoalType.BlocksCleared);Assert.That(collector.PointBaseReward(cells),Is.Zero);
        Assert.That(new CollectionPower(CollectionStyle.Admission).SupportedGoals,Is.EqualTo(PowerGoals.Blocks));
    }
    [Test] public void FoundationEquivalentsPreserveMinimumAndCreditWithoutDoubleRewarding()
    {
        foreach(var p in MatchFoundationPower.Choices().Cast<MatchFoundationPower>()) {
            var block=p.Clone().ForGoal(GoalType.BlocksCleared);
            Assert.That(block.UsesBlockReward,Is.False,"Foundations use the objective credit path once.");
            Assert.That(BlockMatchFoundationPower.ApplyCredit(4,new[]{block}),Is.EqualTo(4*Math.Max(1,p.BasePerTile/2)));
            var rules=new List<MatchRule>{new(MatchType.Snake,4)};block.ModifyMatchRules(rules);
            Assert.That(rules[0].minimumLength,Is.EqualTo(p.Minimum));
        }
        foreach(var p in BlockMatchFoundationPower.Choices().Cast<BlockMatchFoundationPower>()) {
            Assert.That(p.ForGoal(GoalType.Score).AdditiveBaseMod(new(){null,null,null,null}),Is.EqualTo(p.CreditPerTile*8));
            Assert.That(p.ForGoal(GoalType.BlocksCleared).AdditiveBaseMod(new(){null,null,null,null}),Is.Zero);
        }
    }
    [Test] public void PositiveRewardAdaptersCarryFractionalGrowthWithoutAdvancingItTwice()
    {
        var opening=new TacticalStarter(TacticalStyle.Square).ForGoal(GoalType.BlocksCleared);
        Assert.That(opening.UsesBlockReward,Is.True);
        var first=new ScoreResult();SharedPowerRewards.Resolve(opening,4,true,16,.5f,1,1,first);
        Assert.That(first.BlockCredits[opening],Is.EqualTo(4));Assert.That(first.ExtraClears,Is.Empty);
        var second=new ScoreResult();SharedPowerRewards.Resolve(opening,4,true,16,.5f,1,1,second);
        Assert.That(second.ExtraClears[opening],Is.EqualTo(1));
        var garden=new BiomeOpeningPower(false);var planted=new ScoreResult();
        SharedPowerRewards.Resolve(garden,4,true,12,0,1,1,planted);
        Assert.That(planted.BlockCredits[garden],Is.EqualTo(2),"Only two planted tiles earn credit.");
        Assert.That(SharedPowerRewards.CanAdapt(new PatternAnimalPower(true)),Is.False,"Do not erase the curse.");
    }
    [Test] public void ShufflePayoffsRewardOneSuccessfulPlayAndRoundResetDisarmsThem()
    {
        var move=new Move{Player=false};var spark=new ShufflePower(ShuffleStyle.Afterglow);spark.ForGoal(GoalType.BlocksCleared);spark.MatchInfo=move;spark.NotifyShuffle();
        Assert.That(spark.ClaimBlockCredits(move),Is.Zero);move.Player=true;
        Assert.That(spark.ClaimBlockCredits(move),Is.EqualTo(8));Assert.That(spark.ClaimBlockCredits(move),Is.Zero);
        Assert.That(spark.CurrentMatchMultiplier(null),Is.EqualTo(1));
        var slate=new ShufflePower(ShuffleStyle.Salvage);slate.ForGoal(GoalType.Score);slate.MatchInfo=move;slate.NotifyShuffle();
        Assert.That(slate.AdditiveBaseMod(null),Is.EqualTo(32));Assert.That(slate.AdditiveBaseMod(null),Is.Zero);
        move.Player=false;Assert.That(slate.AdditiveBaseMod(null),Is.Zero);
        slate.NotifyShuffle();slate.OnRoundStarted();move.Player=true;Assert.That(slate.AdditiveBaseMod(null),Is.Zero);
        spark.ForGoal(GoalType.Score);spark.NotifyShuffle();Assert.That(spark.CurrentMatchMultiplier(null),Is.EqualTo(2));
    }
    [Test] public void SweepPointRewardsUseActualClearsAndSaturateSafely()
    {
        var board=new BoardModel(3,3,new[]{new MatchRule(MatchType.Snake,3)});
        for(int y=0;y<3;y++)for(int x=0;x<3;x++)board[x,y]=ItemType.Duck;
        var group=new List<BoardModel.Cell>{new(0,0),new(1,0),new(2,0),new(0,1),new(1,1),new(2,1),new(0,2),new(1,2)};
        var sweep=new BlockClearPower(BlockClearStyle.LongLine);int actual=0;
        var cleared=BlockClearPower.ExpandClears(board,new(){group},new[]{sweep},false,(p,n,g)=>actual+=n);
        Assert.That(actual,Is.EqualTo(1));Assert.That(cleared.Count,Is.EqualTo(9));
        Assert.That(sweep.BonusBaseForClears(actual),Is.EqualTo(4));
        Assert.That(sweep.BonusBaseForClears(int.MaxValue),Is.EqualTo(int.MaxValue));
        sweep.ForGoal(GoalType.BlocksCleared);Assert.That(sweep.BonusBaseForClears(actual),Is.Zero);
    }
}

public class LightningRodLevelTests
{
    private static BoardModel Make() {
        var b=new BoardModel(7,7,new[]{new MatchRule(MatchType.Snake,4)});
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)b[x,y]=ItemType.Duck;
        b.Strategy=new StrategyLevelState(LevelBonus.LightningRods);b.Strategy.Initialize(b,new[]{ItemType.Duck,ItemType.Bear});return b;
    }
    private static List<BoardModel.Cell> Sides(BoardModel.Cell r)=>new(){new(r.X-1,r.Y),new(r.X,r.Y-1),new(r.X+1,r.Y)};
    [Test] public void ThreeTouchingSidesFillOneRodAndDuplicateGroupsDoNotDoubleCount() {
        var b=Make();var s=b.Strategy;var sides=Sides(s.Rods[0]);
        s.PrepareWave(b,new(){sides,sides},new HashSet<BoardModel.Cell>(sides));
        Assert.That(s.RodCharges,Is.EqualTo(new[]{3,0,0}));Assert.That(s.RodChainReady,Is.False);
        Assert.That(s.DischargeRods(b),Is.False);
        foreach(var rod in s.Rods){Assert.That(b.IsBlocked(rod),Is.True);Assert.That(b.Connected(rod.X,rod.Y),Is.Empty);}
    }
    [Test] public void DiagonalsDoNotChargeAndCascadesCanCompleteTheChain() {
        var b=Make();var s=b.Strategy;
        s.PrepareWave(b,new(){new(){new(0,0),new(2,0),new(0,2),new(2,2)}},null);
        Assert.That(s.RodCharges,Is.EqualTo(new[]{0,0,0}));
        foreach(var rod in s.Rods)s.PrepareWave(b,new(){Sides(rod)},null);
        Assert.That(s.RodChainReady,Is.True);Assert.That(b.IsCharged(0,0),Is.False);
        Assert.That(s.DischargeRods(b),Is.True);Assert.That(s.RodCharges,Is.EqualTo(new[]{0,0,0}));
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)Assert.That(b.IsCharged(x,y),Is.EqualTo(!b.IsBlocked(new(x,y))));
        b.Refill(new[]{new BoardModel.Cell(0,0)},ReplacementStrategy.InPlace,()=>ItemType.Bear);
        Assert.That(b.IsCharged(0,0),Is.False);Assert.That(b.IsCharged(1,0),Is.True);
        Assert.That(s.DischargeRods(b),Is.False);
    }
    [Test] public void ChargeCapsAndFreshRoundResetsRodProgress() {
        var b=Make();var s=b.Strategy;var rods=s.Rods.ToArray();
        for(int i=0;i<4;i++)s.PrepareWave(b,new(){Sides(s.Rods[0])},null);
        Assert.That(s.RodCharges[0],Is.EqualTo(3));s.SetAnimals(new[]{ItemType.Bear});
        Assert.That(s.Rods,Is.EqualTo(rods));Assert.That(s.RodCharges[0],Is.EqualTo(3));
        s.Initialize(b,new[]{ItemType.Duck});Assert.That(s.RodCharges,Is.EqualTo(new[]{0,0,0}));
    }
    [Test] public void NewLevelHasNumberedRodLessonAndCompleteDrafts() {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        var level=(CampaignLevel)LevelLibrary.GetLevel(35);Assert.That(level.Bonus,Is.EqualTo(LevelBonus.LightningRods));
        var lesson=LevelLessonStoryboard.ForLevel(level,level.GetMatchRules(1));Assert.That(lesson.Kind,Is.EqualTo(LevelLessonKind.LightningRods));
        Assert.That(lesson.Sample(2).Tiles.Count(t=>t.Visible&&t.Rod),Is.EqualTo(3));Assert.That(lesson.Sample(3.5f).RodChain,Is.True);
        Assert.That(lesson.Sample(4.5f).Tiles.Where(t=>t.Visible&&!t.Rod).All(t=>t.Tint==LessonTint.Charged),Is.True);
        var active=new List<PowerUp>();var selected=new HashSet<string>();
        for(int round=1;round<=level.GetTotalRounds;round++) {
            var result=PowerOfferBuilder.Build(level.PowerOffers.ForRound(round),PowerOfferBuilder.Catalog(),selected,Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList(),new Random(35+round),level.Goal,level.Mode,level.GetMatchRules(round),active,round);
            Assert.That(result.IsComplete,Is.True,string.Join("; ",result.Diagnostics));active.Add(result.Choices[0]);selected.Add(result.Choices[0].Name);
        }
    }
}

public class TagBasedCampaignOfferTests
{
    [Test] public void CampaignOffersStayCompleteAcrossRoundsRostersAndAccumulatedChoices()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));LevelLibrary.ReloadSharedContent();
        var doc=SharedGameContent.Document;
        foreach(var definition in doc["levels"].Cast<JObject>()) {
            int number=(int)definition["number"];var level=(CampaignLevel)LevelLibrary.GetLevel(number);
            for(int seed=0;seed<12;seed++) {
                var rng=new Random(number*100+seed);
                var animals=Enum.GetValues(typeof(ItemType)).Cast<ItemType>().Where(a=>(int)a<7).OrderBy(a=>rng.Next()).Take(seed%2==0?7:5).ToList();
                // Authored animal-specific levels require their declared roster.
                if(number==8){if(!animals.Contains(ItemType.Duck))animals.Add(ItemType.Duck);if(!animals.Contains(ItemType.Bear))animals.Add(ItemType.Bear);}
                var selected=new HashSet<string>();var owned=new List<PowerUp>();
                for(int round=1;round<=level.GetTotalRounds;round++) {
                    var offer=level.PowerOffers?.ForRound(round);
                    List<PowerUp> choices;
                    if(offer!=null){var result=PowerOfferBuilder.Build(offer,PowerOfferBuilder.Catalog(),selected,animals,rng,level.Goal,level.Mode,level.GetMatchRules(round),owned,round);choices=result.Choices.ToList();}
                    else choices=PowerUpSelector.BuildChoices(level.GetPowerUpsForRound(round),selected,animals,3,rng,level.Goal,level.Mode,level.GetMatchRules(round),owned,allowFallback:false,round:round);
                    Assert.That(choices.Count,Is.EqualTo(3),"Level "+number+" round "+round+" seed "+seed);
                    Assert.That(choices.Select(p=>p.Name).Distinct().Count(),Is.EqualTo(3));
                    Assert.That(choices.All(p=>p.Supports(level.Goal,level.Mode)&&(round==1||!p.OpeningOnly)),Is.True);
                    var pick=choices[seed%3];owned.Add(pick);selected.Add(pick.Name);
                }
            }
        }
    }
}
