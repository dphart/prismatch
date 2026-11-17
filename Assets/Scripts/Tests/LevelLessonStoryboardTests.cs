using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class LevelLessonStoryboardTests
{
    [Test]
    public void RemixInfographicsShowActualBonusesWithoutFoundationChoices()
    {
        var cascade = new LevelLessonStoryboard(LevelLessonKind.CascadeBonus, blockGoal:true).Sample(3.5f);
        Assert.That(cascade.Status, Does.Contain("25%"));
        Assert.That(cascade.Tiles.Any(t => t.Badge == "x4"), Is.False);
        foreach(var kind in new[] { LevelLessonKind.LargeBonus,LevelLessonKind.SquareBonus }) {
            var lesson = new LevelLessonStoryboard(kind,blockGoal:true);
            var groups = BoardAt(lesson.Sample(1.3f)).Matches();
            Assert.That(groups.Count, Is.GreaterThan(0));
            if(kind == LevelLessonKind.LargeBonus) Assert.That(groups.Any(g => g.Count >= 6), Is.True);
            else Assert.That(groups.Any(g => BlockObjective.HasShape(g,BlockShape.Square)), Is.True);
            Assert.That(lesson.Sample(1.5f).Status, Does.Contain(kind == LevelLessonKind.LargeBonus ? "50%" : "25%"));
            Assert.That(lesson.Sample(1.5f).RowLabels.All(string.IsNullOrEmpty), Is.True);
        }
    }

    private static BoardModel BoardAt(LevelLessonFrame frame, int minimum = 4)
    {
        var board = new BoardModel(frame.Columns, frame.Rows, new[] { new MatchRule(MatchType.Snake, minimum) });
        foreach (var tile in frame.Tiles.Where(t => t.Visible && t.IconVisible && t.IconScale > .01f)) {
            int x = (int)Math.Round((tile.X + tile.IconX) / frame.Step + (frame.Columns - 1) * .5f);
            int y = (int)Math.Round((frame.Rows - 1) * .5f - (tile.Y + tile.IconY) / frame.Step);
            if (x >= 0 && y >= 0 && x < frame.Columns && y < frame.Rows) board[x, y] = tile.Animal;
        }
        return board;
    }

    [Test]
    public void EveryLiveLevelHasItsOwnLessonAndFreePlayUsesItsActualRule()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        LevelLibrary.ReloadSharedContent();
        var expected = new[] { LevelLessonKind.Matching, LevelLessonKind.AnimalFriends, LevelLessonKind.Lightning,
            LevelLessonKind.MatchSize, LevelLessonKind.BlockSize, LevelLessonKind.ChainReaction,
            LevelLessonKind.GardenPlots, LevelLessonKind.AlternatingCurrent, LevelLessonKind.BorderPatrol,
            LevelLessonKind.TreasureTrail, LevelLessonKind.ChangingTides,
            LevelLessonKind.CascadeBonus, LevelLessonKind.LargeBonus, LevelLessonKind.SquareBonus,
            LevelLessonKind.Matching, LevelLessonKind.CascadeBonus, LevelLessonKind.LargeBonus,
            LevelLessonKind.Lightning, LevelLessonKind.ChangingTides,
            LevelLessonKind.Matching, LevelLessonKind.SquareBonus, LevelLessonKind.LargeBonus, LevelLessonKind.Lightning };
        Assert.That(LevelLibrary.TotalLevels, Is.GreaterThanOrEqualTo(expected.Length));
        for (int number = 1; number <= LevelLibrary.TotalLevels; number++) {
            var level = LevelLibrary.GetLevel(number);
            var lesson = LevelLessonStoryboard.ForLevel(level, level.GetMatchRules(1));
            if(number<=expected.Length)Assert.That(lesson.Kind, Is.EqualTo(expected[number - 1]), "Level " + number);
            Assert.That(lesson.Sample(.5f).Caption, Is.Not.Empty);
            Assert.That(lesson.Sample(.5f).Footer, Is.Not.Empty);
        }
        var free = new FreePlayConfig(MatchType.Vertical, minimumMatchLength: 5);
        var custom = LevelLessonStoryboard.ForLevel(free, free.GetMatchRules(1));
        Assert.That(custom.Kind, Is.EqualTo(LevelLessonKind.Matching));
        Assert.That(custom.Matching, Is.EqualTo(MatchType.Vertical));
        Assert.That(custom.Minimum, Is.EqualTo(5));
    }

    [Test]
    public void LightningSwapsIntoFiveBeforeStrikingThenClearsTheWholeRowAndChargesOthers()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.Lightning);
        var before = BoardAt(lesson.Sample(.3f));
        Assert.That(before.LightningLines(before.Matches()), Is.Empty);
        var matched = BoardAt(lesson.Sample(1.5f));
        var lines = matched.LightningLines(matched.Matches());
        Assert.That(lines.Count, Is.EqualTo(1));
        Assert.That(lines[0], Is.EqualTo((MatchType.Horizontal, 1)));
        Assert.That(lesson.Sample(1.5f).Lightning, Is.Zero);
        Assert.That(lesson.Sample(2.02f).Lightning, Is.GreaterThan(0));
        var charged = lesson.Sample(3.2f);
        for (int x = 0; x < 7; x++) {
            Assert.That(charged.Cell(x, 1).IconScale, Is.Zero, "Every animal in the struck row clears, including nonmatching animals.");
            foreach (int y in new[] { 0, 2 }) {
                Assert.That(charged.Cell(x, y).Tint, Is.EqualTo(LessonTint.Charged));
                Assert.That(charged.Cell(x, y).Badge, Is.EqualTo("x2"));
            }
        }
    }

    [Test]
    public void ChainReactionShowsARealSetupAndARealFallingMatch()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.ChainReaction);
        Assert.That(BoardAt(lesson.Sample(.2f)).Matches(), Is.Empty);
        var player = BoardAt(lesson.Sample(1.4f)).Matches();
        Assert.That(player.Count, Is.EqualTo(1));
        Assert.That(player[0].Count, Is.EqualTo(4));
        Assert.That(lesson.Sample(1.9f).Status, Does.Contain("0 POINTS"));
        var cascadeBoard = BoardAt(lesson.Sample(3.3f));
        var cascade = cascadeBoard.Matches();
        Assert.That(cascade.Count, Is.EqualTo(1));
        Assert.That(cascade[0].Count, Is.EqualTo(5));
        Assert.That(cascadeBoard[cascade[0][0].X, cascade[0][0].Y], Is.EqualTo(ItemType.Bear));
        Assert.That(lesson.Sample(3.3f).Status, Does.Contain("4x"));
    }

    [Test]
    public void GardenPlantsAfterTheFirstMatchAndOnlyTheLaterMatchGetsDoublePoints()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.GardenPlots);
        var first = lesson.Sample(1.4f);
        Assert.That(first.Tiles.Count(t => t.Visible && t.Tint == LessonTint.Planted), Is.Zero);
        Assert.That(first.Tiles.All(t => t.Badge != "x2"), Is.True);
        var planted = lesson.Sample(2.4f);
        Assert.That(planted.Tiles.Count(t => t.Visible && t.Tint == LessonTint.Planted), Is.EqualTo(4));
        var later = lesson.Sample(3.8f);
        Assert.That(later.Tiles.Count(t => t.Visible && t.Tint == LessonTint.Planted && t.Badge == "x2" && t.Animal == ItemType.Bear), Is.EqualTo(4));
        Assert.That(later.Tiles.Where(t => t.Tint == LessonTint.Planted).Select(t => (t.X, t.Y)),
            Is.EquivalentTo(planted.Tiles.Where(t => t.Tint == LessonTint.Planted).Select(t => (t.X, t.Y))));
    }

    [Test]
    public void AnimalLessonUsesPriorCountsAndSharesTheBonusWhenTheCountsTie()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.AnimalFriends);
        var first = lesson.Sample(.4f);
        Assert.That(first.Tiles.Where(t => t.Visible && t.Badge == "x2").All(t => t.Animal == ItemType.Bear), Is.True);
        Assert.That(first.Status, Does.Contain("Bear 4"));
        var tied = lesson.Sample(3.4f);
        Assert.That(tied.Status, Does.Contain("Bear 8"));
        Assert.That(tied.Tiles.Where(t => t.Visible && t.Badge == "x2").Select(t => t.Animal).Distinct(),
            Is.EquivalentTo(new[] { ItemType.Duck, ItemType.Bear }));
    }

    [Test]
    public void AlternatingBonusWaitsUntilThePlayFinishesAndOnlyHighlightsTheActiveAnimal()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.AlternatingCurrent);
        foreach (var example in new[] { (.4f, ItemType.Duck), (2.3f, ItemType.Duck), (2.9f, ItemType.Bear), (5.4f, ItemType.Duck) }) {
            var frame = lesson.Sample(example.Item1);
            Assert.That(frame.Tiles.Where(t => t.Visible && t.Badge == "x3").All(t => t.Animal == example.Item2), Is.True);
        }
        Assert.That(BoardAt(lesson.Sample(1.4f)).Matches().Single().Count, Is.EqualTo(4));
        Assert.That(BoardAt(lesson.Sample(4f)).Matches().Single().Count, Is.EqualTo(4));
    }

    [Test]
    public void SizeLessonsCompareTheActualThreeFoundationsAndGoalCredits()
    {
        foreach (bool blockGoal in new[] { false, true }) {
            var lesson = new LevelLessonStoryboard(blockGoal ? LevelLessonKind.BlockSize : LevelLessonKind.MatchSize);
            var values = blockGoal ? new[] { 1, 2, 4 } : new[] { 2, 4, 8 };
            for (int row = 0; row < 3; row++) {
                var frame = lesson.Sample(row * 2 + .7f);
                Assert.That(frame.Tiles.Count(t => t.Visible && t.Tint == LessonTint.Match), Is.EqualTo(row + 3));
                Assert.That(frame.RowLabels[row], Does.StartWith(values[row].ToString()));
                Assert.That(frame.Status, Does.Contain(((row + 3) * values[row]).ToString()));
                Assert.That(frame.Footer, Does.Contain("whole run"));
            }
        }
    }

    [Test]
    public void BorderShowsTwoCornersCompletingAllFourEdgesThenResetting()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.BorderPatrol);
        Assert.That(lesson.Sample(.2f).LitEdges, Is.Zero);
        Assert.That(lesson.Sample(1.5f).LitEdges, Is.EqualTo(9));
        Assert.That(lesson.Sample(4f).LitEdges, Is.EqualTo(15));
        Assert.That(lesson.Sample(4f).Status, Does.Contain("4x"));
        Assert.That(lesson.Sample(5.5f).LitEdges, Is.Zero);
        Assert.That(BoardAt(lesson.Sample(1.4f)).Matches().Single().Count, Is.EqualTo(4));
        Assert.That(BoardAt(lesson.Sample(4f)).Matches().Single().Count, Is.EqualTo(4));
    }

    [Test]
    public void TreasureShowsThreeCollectionsBeforeBoostingTheFollowingPlay()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.TreasureTrail);
        foreach (var time in new[] { 1.4f, 3.3f, 5.2f }) {
            var frame = lesson.Sample(time);
            Assert.That(frame.Tiles.Count(t => t.Visible && t.Treasure), Is.EqualTo(1));
            Assert.That(frame.Tiles.All(t => t.Badge != "x5"), Is.True);
        }
        Assert.That(lesson.Sample(1.4f).Status, Does.Contain("1 / 3"));
        Assert.That(lesson.Sample(3.3f).Status, Does.Contain("2 / 3"));
        Assert.That(lesson.Sample(5.2f).Status, Does.Contain("3 / 3"));
        var reward = lesson.Sample(6.75f);
        Assert.That(reward.Tiles.Count(t => t.Visible && t.Badge == "x5"), Is.EqualTo(4));
    }

    [Test]
    public void TideShowsFourCompletedMovesBeforeTurningAndBoostingTheNextPlay()
    {
        var lesson = new LevelLessonStoryboard(LevelLessonKind.ChangingTides);
        var fourth = lesson.Sample(4.6f);
        Assert.That(fourth.Status, Does.Contain("4 / 4"));
        Assert.That(fourth.Direction, Is.EqualTo(ReplacementStrategy.South));
        var turned = lesson.Sample(4.95f);
        Assert.That(turned.Direction, Is.EqualTo(ReplacementStrategy.West));
        Assert.That(turned.Tiles.All(t => t.Badge != "x3"), Is.True);
        Assert.That(lesson.Sample(6.3f).Tiles.Count(t => t.Visible && t.Badge == "x3"), Is.EqualTo(4));
        Assert.That(lesson.Sample(7.1f).Tiles.Any(t => t.Visible && t.IconX > 0), Is.True, "New animals enter from the right and move left.");
    }

    [Test]
    public void StoryboardsLoopDeterministicallyAndReuseTheirFrameWithoutLeakingState()
    {
        foreach (LevelLessonKind kind in Enum.GetValues(typeof(LevelLessonKind))) {
            var lesson = new LevelLessonStoryboard(kind);
            var frame = new LevelLessonFrame();
            lesson.WriteFrame(lesson.Duration * .6f, frame);
            lesson.WriteFrame(lesson.Duration + .4f, frame);
            var start = lesson.Sample(.4f);
            Assert.That(frame.Caption, Is.EqualTo(start.Caption), kind.ToString());
            Assert.That(frame.Status, Is.EqualTo(start.Status), kind.ToString());
            Assert.That(frame.LitEdges, Is.EqualTo(start.LitEdges));
            for (int i = 0; i < frame.Tiles.Length; i++) {
                Assert.That(frame.Tiles[i].Visible, Is.EqualTo(start.Tiles[i].Visible));
                Assert.That(frame.Tiles[i].IconX, Is.EqualTo(start.Tiles[i].IconX).Within(.001f));
                Assert.That(frame.Tiles[i].IconY, Is.EqualTo(start.Tiles[i].IconY).Within(.001f));
                Assert.That(frame.Tiles[i].Badge, Is.EqualTo(start.Tiles[i].Badge));
                Assert.That(float.IsNaN(frame.Tiles[i].IconScale), Is.False);
            }
        }
    }
}

public class LessonReadingStepsTests
{
    [NUnit.Framework.Test] public void EveryLessonHasOrderedNumberedStepsAndThreeSecondsToReadEach()
    {
        foreach(LevelLessonKind kind in System.Enum.GetValues(typeof(LevelLessonKind))) {
            var story=new LevelLessonStoryboard(kind);var starts=story.StepStarts;
            NUnit.Framework.Assert.That(starts[0],NUnit.Framework.Is.Zero);
            float reading=0;
            for(int i=0;i<starts.Length;i++) {
                float end=i+1<starts.Length?starts[i+1]:story.Duration;
                NUnit.Framework.Assert.That(end,NUnit.Framework.Is.GreaterThan(starts[i]),kind.ToString());
                float duration=System.Math.Max(3f,end-starts[i]);
                var time=story.StoryTimeForReading(reading+duration*.5f);
                NUnit.Framework.Assert.That(story.StepAt(time),NUnit.Framework.Is.EqualTo(i),kind.ToString());
                reading+=duration;
            }
            NUnit.Framework.Assert.That(story.ReadingDuration,NUnit.Framework.Is.EqualTo(reading).Within(.001f));
            NUnit.Framework.Assert.That(story.StepAt(story.StoryTimeForReading(reading)),NUnit.Framework.Is.Zero);
        }
    }
}
