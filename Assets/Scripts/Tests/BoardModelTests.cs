using NUnit.Framework;
using System;
using System.Collections.Generic;

[TestFixture]
public class BoardModelTests
{
    private static readonly List<ItemType> Types = new()
    { ItemType.Bear, ItemType.Duck, ItemType.Hippo, ItemType.Sloth };

    [Test]
    public void LightningOpeningHasThreeGapOneAndAnAdjacentWinningSwapWithoutPrematches()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var board = new BoardModel(7,7);
            board.Generate(Types,new Random(seed),lightningOpening:true);
            Assert.That(board.Matches(),Is.Empty,"Seed " + seed);
            var animal = board[1,3];
            Assert.That(board[2,3],Is.EqualTo(animal));
            Assert.That(board[3,3],Is.EqualTo(animal));
            Assert.That(board[4,3],Is.Not.EqualTo(animal));
            Assert.That(board[5,3],Is.EqualTo(animal));
            Assert.That(board[4,4],Is.EqualTo(animal));
            board.Swap(new BoardModel.Cell(4,3),new BoardModel.Cell(4,4));
            Assert.That(board.LightningLines(board.Matches()),Does.Contain((MatchType.Horizontal,3)),"Seed " + seed);
        }
    }

    [Test]
    public void OrdinaryGenerationIsUnchangedWhenLightningOpeningIsDisabled()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var normal = new BoardModel(7,7);
            var laterRound = new BoardModel(7,7);
            normal.Generate(Types,new Random(seed));
            laterRound.Generate(Types,new Random(seed),lightningOpening:false);
            for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++)
                Assert.That(laterRound[x,y],Is.EqualTo(normal[x,y]));
        }
    }

    [TestCase(ReplacementStrategy.South)]
    [TestCase(ReplacementStrategy.North)]
    [TestCase(ReplacementStrategy.West)]
    [TestCase(ReplacementStrategy.East)]
    public void Gravity_PreservesSurvivorOrderAndSpawnsFromOppositeEdge(ReplacementStrategy strategy)
    {
        bool vertical = strategy == ReplacementStrategy.South || strategy == ReplacementStrategy.North;
        var board = new BoardModel(vertical ? 3 : 5, vertical ? 5 : 3);
        var line = new[] { ItemType.Bear, ItemType.Duck, ItemType.Hippo, ItemType.Sloth, ItemType.Giraffe };
        for (int y = 0; y < board.Height; y++)
        for (int x = 0; x < board.Width; x++) board[x, y] = line[vertical ? y : x];
        var holes = new[] { new BoardModel.Cell(1, 1),
            new BoardModel.Cell(vertical ? 1 : 3, vertical ? 3 : 1) };
        int spawned = 0;
        var moves = board.Refill(holes, strategy, () => { spawned++; return ItemType.Walrus; });
        Assert.AreEqual(2, spawned);
        bool towardEnd = strategy == ReplacementStrategy.South || strategy == ReplacementStrategy.East;
        var expected = towardEnd
            ? new[] { ItemType.Walrus, ItemType.Walrus, ItemType.Bear, ItemType.Hippo, ItemType.Giraffe }
            : new[] { ItemType.Bear, ItemType.Hippo, ItemType.Giraffe, ItemType.Walrus, ItemType.Walrus };
        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(expected[i], board[vertical ? 1 : i, vertical ? i : 1]);
            Assert.AreEqual(line[i], board[vertical ? 0 : i, vertical ? i : 0], "Uncleared lanes stay unchanged.");
        }
        foreach (var move in moves)
        {
            Assert.AreEqual(move.Type, board[move.To.X, move.To.Y]);
            if (!move.IsNew) continue;
            Assert.IsTrue(strategy switch {
                ReplacementStrategy.South => move.From.Y < 0,
                ReplacementStrategy.North => move.From.Y >= board.Height,
                ReplacementStrategy.West => move.From.X >= board.Width,
                _ => move.From.X < 0
            }, "New blocks must enter from the edge opposite gravity.");
        }
    }

    [Test]
    public void InPlaceRefill_ChangesOnlyUniqueClearedCells()
    {
        var board = new BoardModel(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) board[x, y] = ItemType.Bear;
        var cell = new BoardModel.Cell(1, 1);
        int calls = 0;
        var moves = board.Refill(new[] { cell, cell }, ReplacementStrategy.InPlace,
            () => { calls++; return ItemType.Duck; });
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(ItemType.Duck, board[1, 1]);
        Assert.AreEqual(ItemType.Bear, board[1, 2]);
        Assert.AreEqual(cell, moves[0].From);
        Assert.AreEqual(cell, moves[0].To);
    }

    [TestCase(ReplacementStrategy.InPlace)]
    [TestCase(ReplacementStrategy.South)]
    [TestCase(ReplacementStrategy.North)]
    [TestCase(ReplacementStrategy.West)]
    [TestCase(ReplacementStrategy.East)]
    public void Refill_HandlesEmptyAndFullBoardClears(ReplacementStrategy strategy)
    {
        var board = new BoardModel(3, 4);
        var cells = new List<BoardModel.Cell>();
        for (int y = 0; y < 4; y++) for (int x = 0; x < 3; x++)
        { board[x, y] = ItemType.Bear; cells.Add(new BoardModel.Cell(x, y)); }
        Assert.IsEmpty(board.Refill(Array.Empty<BoardModel.Cell>(), strategy, () => throw new Exception("No spawn expected")));
        var moves = board.Refill(cells, strategy, () => ItemType.Duck);
        Assert.AreEqual(12, moves.Count);
        foreach (var cell in cells) Assert.AreEqual(ItemType.Duck, board[cell.X, cell.Y]);
    }

    [TestCase(MatchType.Horizontal, 10, 3, 10)]
    [TestCase(MatchType.Vertical, 3, 10, 10)]
    [TestCase(MatchType.Snake, 3, 10, 10)]
    [TestCase(MatchType.Snake, 10, 3, 10)]
    [TestCase(MatchType.Horizontal, 3, 3, 2)]
    [TestCase(MatchType.Vertical, 3, 3, 2)]
    [TestCase(MatchType.Snake, 3, 3, 2)]
    public void CustomMinimum_GeneratesPlayableBoard(MatchType type, int width, int height, int minimum)
    {
        for (int seed = 0; seed < 10; seed++)
        {
            var board = new BoardModel(width, height, new[] { new MatchRule(type, minimum) });
            board.Generate(Types, new Random(seed));
            Assert.That(board.Matches(), Is.Empty, $"seed {seed}");
            Assert.That(board.HasLegalMove(), Is.True, $"seed {seed}");
        }
    }

    [Test]
    public void Generate_CreatesNoStartingMatches_AndHasLegalMove()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var board = new BoardModel(7, 7);
            board.Generate(Types, new Random(seed));
            Assert.That(board.Matches(), Is.Empty, $"seed {seed}");
            Assert.That(board.HasLegalMove(), Is.True, $"seed {seed}");
        }
    }

    [Test]
    public void Connected_FindsBentGroups()
    {
        var board = new BoardModel(3, 3);
        board[0, 0] = board[1, 0] = board[1, 1] = ItemType.Bear;
        Assert.AreEqual(3, board.Connected(0, 0).Count);
        Assert.AreEqual(1, board.Matches().Count);
    }

    [Test]
    public void Swap_CanCreateAndRevertMatch()
    {
        var board = new BoardModel(3, 2);
        board[0, 0] = board[1, 0] = ItemType.Bear; board[2, 0] = ItemType.Duck;
        board[0, 1] = ItemType.Duck; board[1, 1] = ItemType.Hippo; board[2, 1] = ItemType.Bear;
        var a = new BoardModel.Cell(2, 0); var b = new BoardModel.Cell(2, 1);
        board.Swap(a, b);
        Assert.AreEqual(3, board.Connected(0, 0).Count);
        board.Swap(a, b);
        Assert.That(board.Matches(), Is.Empty);
    }

    [Test]
    public void HorizontalAndVerticalRules_UseIndependentMinimumLengths()
    {
        var board = new BoardModel(4, 4, new[]
        {
            new MatchRule(MatchType.Horizontal, 4),
            new MatchRule(MatchType.Vertical, 3)
        });
        board[0, 0] = board[1, 0] = board[2, 0] = ItemType.Bear;
        board[3, 0] = ItemType.Duck;
        board[3, 1] = board[3, 2] = board[3, 3] = ItemType.Duck;

        var matches = board.Matches();
        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual(4, matches[0].Count);
        Assert.That(matches[0], Does.Contain(new BoardModel.Cell(3, 3)));
    }

    [Test]
    public void SnakeRule_AllowsBentGroupAtConfiguredLength()
    {
        var board = new BoardModel(3, 3, new[] { new MatchRule(MatchType.Snake, 4) });
        board[0, 0] = board[1, 0] = board[1, 1] = board[2, 1] = ItemType.Bear;
        Assert.AreEqual(1, board.Matches().Count);
        board[2, 1] = ItemType.Duck;
        Assert.That(board.Matches(), Is.Empty);
    }

    [Test]
    public void OverlappingLineRules_MergeCrossIntoOneMatch()
    {
        var board = new BoardModel(3, 3, new[]
        {
            new MatchRule(MatchType.Horizontal, 3),
            new MatchRule(MatchType.Vertical, 3)
        });
        board[0, 1] = board[1, 1] = board[2, 1] = ItemType.Hippo;
        board[1, 0] = board[1, 2] = ItemType.Hippo;

        var matches = board.Matches();
        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual(5, matches[0].Count);
    }
}

[TestFixture]
public class CascadeDampingTests
{
    [Test]
    public void WeightsHalveAfterEachCascade()
    {
        Assert.That(BoardModel.CascadeMatchWeight(0), Is.EqualTo(1));
        Assert.That(BoardModel.CascadeMatchWeight(1), Is.EqualTo(.5));
        Assert.That(BoardModel.CascadeMatchWeight(2), Is.EqualTo(.25));
        Assert.That(BoardModel.CascadeMatchWeight(3), Is.EqualTo(.125));
    }

    [Test]
    public void DampingReducesMatchMakingSpawnsWithoutChangingExistingTiles()
    {
        int early = 0, late = 0;
        var palette = new[] { ItemType.Bear, ItemType.Duck, ItemType.Sloth };
        for (int seed = 0; seed < 2000; seed++)
        foreach (int depth in new[] { 1, 5 })
        {
            var board = new BoardModel(3, 1, new[] { new MatchRule(MatchType.Horizontal, 3) });
            board[0, 0] = board[1, 0] = board[2, 0] = ItemType.Bear;
            var cell = new BoardModel.Cell(2, 0);
            var refill = new System.Collections.Generic.List<BoardModel.RefillMove>
                { new(cell, cell, ItemType.Bear, true) };
            board.ApplyCascadeDamping(refill, palette, depth, new System.Random(seed));
            Assert.That(board[0, 0], Is.EqualTo(ItemType.Bear));
            Assert.That(board[1, 0], Is.EqualTo(ItemType.Bear));
            Assert.That(refill[0].Type, Is.EqualTo(board[2, 0]));
            if (board.Matches().Count > 0) { if (depth == 1) early++; else late++; }
        }
        Assert.That(early, Is.GreaterThan(0));
        Assert.That(late, Is.LessThan(early / 2));
    }
}

[TestFixture]
public class AnimalPairMatchingTests
{
    [TestCase(MatchType.Snake)]
    [TestCase(MatchType.Horizontal)]
    [TestCase(MatchType.Vertical)]
    public void PairMatchesAcrossRulesAndRespectsMinimum(MatchType type)
    {
        bool vertical = type == MatchType.Vertical;
        var board = new BoardModel(vertical ? 1 : 4, vertical ? 4 : 1,
            new[] { new MatchRule(type, 4) });
        for (int n = 0; n < 4; n++) board[vertical ? 0 : n, vertical ? n : 0] =
            n % 2 == 0 ? ItemType.Duck : ItemType.Walrus;
        Assert.That(board.Matches(), Is.Empty);
        board.SetAnimalPairs(new[] { new AnimalPairPower(ItemType.Duck, ItemType.Walrus) });
        Assert.That(board.Matches().Count, Is.EqualTo(1));
        Assert.That(board.Matches()[0].Count, Is.EqualTo(4));
        board[0, 0] = ItemType.Bear;
        Assert.That(board.Matches(), Is.Empty);
        board.SetAnimalPairs(new AnimalPairPower[0]);
        Assert.That(board.Matches(), Is.Empty);
    }

    [Test]
    public void PactOffersRequireBothAnimalsAndCannotOverlap()
    {
        var pair = new AnimalPairPower(ItemType.Duck, ItemType.Walrus);
        Assert.That(pair.Supports(GoalType.BlocksCleared, FreePlayMode.Timed), Is.False);
        Assert.That(pair.SupportsItems(new[] { ItemType.Duck }), Is.False);
        Assert.That(pair.SupportsItems(new[] { ItemType.Duck, ItemType.Walrus }), Is.True);
        Assert.That(pair.CanOfferWith(new PowerUp[] { pair.Clone() }), Is.False);
        Assert.That(pair.CanOfferWith(new PowerUp[] { new AnimalPairPower(ItemType.Duck, ItemType.Bear) }), Is.False);
        Assert.That(pair.CanOfferWith(new PowerUp[] { new AnimalPairPower(ItemType.Bear, ItemType.Sloth) }), Is.True);
    }
}
