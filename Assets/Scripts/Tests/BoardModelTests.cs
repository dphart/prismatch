using NUnit.Framework;
using System;
using System.Collections.Generic;

[TestFixture]
public class BoardModelTests
{
    private static readonly List<ItemType> Types = new()
    { ItemType.Bear, ItemType.Duck, ItemType.Hippo, ItemType.Sloth };

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
