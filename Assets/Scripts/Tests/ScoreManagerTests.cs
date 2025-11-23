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