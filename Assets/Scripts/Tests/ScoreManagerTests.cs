using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[TestFixture]
public class ScoreManagerTests
{
    // === SIMPLE, STATELESS TESTS ===
    [Test]
    public void CalculateScore_WithEmptyTileList_ReturnsZero()
    {
        ScoreManager scoreManager = new ScoreManager(null, null);
        Assert.AreEqual(0, scoreManager.CalculateScore(new List<Tile>()));
    }

    [Test]
    public void CalculateScore_WithNullTileList_ReturnsZero()
    {
        ScoreManager scoreManager = new ScoreManager(null, null);
        Assert.AreEqual(0, scoreManager.CalculateScore(null));
    }


    // === STATEFUL TESTS WITH SETUP ===
    private ScoreManager _scoreManager;
    private PowerManager _powerManager;
    private List<GameObject> _testGameObjects;

    [SetUp]
    public void SetupForStatefulTests()
    {
        _testGameObjects = new List<GameObject>();
        var playerConfigs = new PlayerConfigs(); // Assuming PlayerConfigs is a concrete class

        // Instantiate real PowerManager with our fake providers
        var timeProvider = new FakeTimeProvider();
        var levelInfoProvider = new FakeLevelInfoProvider();
        var itemInfoProvider = new FakeItemInfoProvider();
        _powerManager = new PowerManager(timeProvider, levelInfoProvider, itemInfoProvider);

        _scoreManager = new ScoreManager(playerConfigs, _powerManager);
    }

    [TearDown]
    public void TeardownForStatefulTests()
    {
        // Clean up any GameObjects created during the test
        foreach (var go in _testGameObjects)
        {
            Object.DestroyImmediate(go);
        }
    }

    private sealed class ActivationCountingPower : PowerUp
    {
        public int Evaluations;
        public ActivationCountingPower() { Name="Activation test"; }
        public override int AdditiveBaseMod(List<Tile> tiles) { Evaluations++;return 10; }
        public override void Apply(GameState state) { }
        public override string GetCurrentPowerDescription()=>"Conditional test";
        public override PowerUp Clone()=>new ActivationCountingPower();
    }
    [Test]
    public void ConditionalActivationUsesTheSingleScoringEvaluation()
    {
        var power=new ActivationCountingPower();_powerManager.ActivePowers().Add(power);
        int score=_scoreManager.CalculateScore(new List<Tile>{CreateTestTile()});
        Assert.That(score,Is.EqualTo(10));Assert.That(power.Evaluations,Is.EqualTo(1));
        Assert.That(_scoreManager.LastResult.Activations.Count,Is.EqualTo(1));
        Assert.That(_scoreManager.LastResult.Activations[0].Power,Is.SameAs(power));
        Assert.That(_scoreManager.LastResult.Activations[0].Effect,Does.Contain("+10 base"));
    }

    private Tile CreateTestTile()
    {
        var go = new GameObject("TestTile");
        _testGameObjects.Add(go);
        var tile = go.AddComponent<Tile>();
        var item = ScriptableObject.CreateInstance<Item>();
        tile.item = item;
        return tile;
    }

    [Test]
    public void SweepPointsDoNotAdvanceAnimalPowersOrEnlargeTheMatch()
    {
        var sweep = new BlockClearPower(BlockClearStyle.Neighbor);
        var bearMastery = new AnimalMasteryPower(ItemType.Bear, false);
        _powerManager.ActivePowers().AddRange(new PowerUp[] { sweep, bearMastery,
            new IconTypeGivesBase(ItemType.Bear, 20), new HighStakesPower() });
        var matched = new List<Tile>();
        for (int i = 0; i < 4; i++)
        {
            var tile = CreateTestTile();
            tile.item.ItemType = ItemType.Duck;
            matched.Add(tile);
        }
        int score = _scoreManager.CalculateScore(matched, new Dictionary<PowerUp, int> { { sweep, 8 } });
        Assert.That(score, Is.EqualTo(8), "Two extra clears supply their sweep points, without animal or 5+ bonuses.");
        Assert.That(bearMastery.Progress, Is.Zero);
        Assert.That(_powerManager.AnimalHistory.Count(ItemType.Bear), Is.Zero);
        Assert.That(_powerManager.AnimalHistory.Count(ItemType.Duck), Is.EqualTo(4));
    }

    [Test]
    public void SweepRewardsBelongToTheirMatchAndObeyChainReactionAndGoal()
    {
        var sweep=new BlockClearPower(BlockClearStyle.Neighbor);
        _powerManager.ActivePowers().Add(sweep);
        var tiles=new List<Tile>{CreateTestTile()};
        var rewards=new Dictionary<PowerUp,int>{{sweep,8}};
        Assert.That(_scoreManager.CalculateScore(tiles,rewards),Is.EqualTo(8));
        _scoreManager.LevelBonus=LevelBonus.ChainReaction;
        Assert.That(_scoreManager.CalculateScore(tiles,rewards),Is.Zero,"Initial matches cannot bypass the level's zero-point rule.");
        _scoreManager.RewardGoal=GoalType.BlocksCleared;
        Assert.That(_scoreManager.CalculateScore(tiles,rewards),Is.Zero,"Points reward must not leak into blocks.");
    }

    [Test]
    public void CalculateScore_WithTilesButNoPowers_ReturnsZero()
    {
        // Arrange
        _powerManager.ClearPowers();
        _powerManager.ClearBossPowers();
        var tiles = new List<Tile> { CreateTestTile() };

        // Act
        int score = _scoreManager.CalculateScore(tiles);

        // Assert
        Assert.AreEqual(0, score, "Score should be 0 when no powers are active, even with tiles.");
    }
}
