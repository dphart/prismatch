using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Snapshot-style tests to lock in current scoring behavior before refactors.
/// These avoid Board/MonoBehaviour dependencies by injecting powers directly.
/// </summary>
[TestFixture]
public class ScoreManagerBehaviorSnapshotTests
{
    private PowerManager _powerManager;
    private ScoreManager _scoreManager;
    private List<GameObject> _createdObjects;
    private List<Object> _createdAssets;

    [SetUp]
    public void SetUp()
    {
        _powerManager = new PowerManager(new FakeTimeProvider(), new FakeLevelInfoProvider(), new FakeItemInfoProvider());
        _scoreManager = new ScoreManager(new PlayerConfigs(), _powerManager);
        _createdObjects = new List<GameObject>();
        _createdAssets = new List<Object>();
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

    [Test]
    public void CalculateScore_NoPowers_WithTiles_ReturnsZero()
    {
        var tiles = MakeTiles(ItemType.Bear, 3);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(0, score, "Current behavior: without scoring powers, matches award 0.");
    }

    [Test]
    public void CalculateScore_WithBasePower_AddsPointsPerTile()
    {
        AddPower(new BasePowerUp(1));
        var tiles = MakeTiles(ItemType.Bear, 3);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(3, score, "BasePowerUp(1) should grant 1 point per tile.");
    }

    [Test]
    public void CalculateScore_WithBaseMultiPower_AdditiveMultiplierApplied()
    {
        AddPower(new BaseMultiPowerUp(1, 2)); // +1 per tile, +2 additive multiplier => (1+2)=3x
        var tiles = MakeTiles(ItemType.Bear, 3);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(9, score, "Additive multiplier increases final multiplier (3 tiles * 3x = 9).");
    }

    [Test]
    public void CalculateScore_WithNextMatchMulti_QueuesMultiplierForNextMatch()
    {
        AddPower(new BasePowerUp(1));
        AddPower(new NextMatchMultiStub(3));
        var tiles = MakeTiles(ItemType.Bear, 3);

        // First call sets nextMatchMulti for the following match, so current uses 1x
        int firstScore = _scoreManager.CalculateScore(tiles);
        // Second call uses the queued multiplier from the previous call
        int secondScore = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(3, firstScore, "Current behavior: next-match multiplier is not applied to the triggering match.");
        Assert.AreEqual(9, secondScore, "Queued next-match multiplier applies on the following match (3 tiles * 3x = 9).");
    }

    [Test]
    public void CalculateScore_WithBossPower_FirstMatchTypeInvalidated()
    {
        var boss = new FirstItemsGivesNoPoints
        {
            MatchInfo = new FakeMatchInfo(true),
            ItemInfoProvider = new FakeItemInfoProvider(),
            LevelInfoProvider = new FakeLevelInfoProvider(),
            TimeProvider = new FakeTimeProvider()
        };

        AddPower(new BasePowerUp(1));
        AddBossPower(boss);

        // First match of Bear should be invalid (0 points)
        var bearMatch = MakeTiles(ItemType.Bear, 3);
        int scoreBear = _scoreManager.CalculateScore(bearMatch);
        Assert.AreEqual(0, scoreBear, "First matched item type is invalidated by boss power.");

        // Different type should score normally
        var duckMatch = MakeTiles(ItemType.Duck, 3);
        int scoreDuck = _scoreManager.CalculateScore(duckMatch);
        Assert.AreEqual(3, scoreDuck, "Different item type should score once boss power has recorded the first type.");
    }

    // --- Helpers ---

    private List<Tile> MakeTiles(ItemType type, int count)
    {
        var list = new List<Tile>();
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Tile_{type}_{i}");
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

    private class NextMatchMultiStub : PowerUp
    {
        private readonly int _multiplier;
        public NextMatchMultiStub(int multiplier)
        {
            _multiplier = multiplier;
            Name = "NextMatchStub";
            PowerUpDescription = $"Stub next-match x{multiplier}";
        }

        public override string GetCurrentPowerDescription() => PowerUpDescription;
        public override void Apply(GameState state) { }
        public override PowerUp Clone() => new NextMatchMultiStub(_multiplier);
        public override int NextMatchMulti(List<Tile> tiles) => _multiplier;
    }

    private class FakeMatchInfo : MatchInfo
    {
        private readonly bool _wasUserInitiated;
        public FakeMatchInfo(bool wasUserInitiated) { _wasUserInitiated = wasUserInitiated; }
        public bool wasUserInitiatedMatch() => _wasUserInitiated;
    }
}
