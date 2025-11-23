using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[TestFixture]
public class PowerUpBehaviorTests
{
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

    [Test]
    public void BossPower_HalfPointsForFirstHalfOfLevel_HalvesScoreWhenTimeHigh()
    {
        var bossPower = new HalfPointsForFirstHalfOfLevel
        {
            TimeProvider = new FixedTimeProvider(40) // >= 30 triggers half points
        };

        AddPower(new BasePowerUp(1));
        AddBossPower(bossPower);

        var tiles = MakeTiles(ItemType.Bear, 3);

        int score = _scoreManager.CalculateScore(tiles);

        Assert.AreEqual(1, score, "3 base points should be halved to 1 (int division).");
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
