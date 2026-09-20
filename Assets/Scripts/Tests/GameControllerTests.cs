using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

[TestFixture]
public class GameControllerTests
{
    private List<Object> _createdAssets;
    private List<GameObject> _createdObjects;
    private GameController _controller;

    [SetUp]
    public void SetUp()
    {
        _createdAssets = new List<Object>();
        _createdObjects = new List<GameObject>();

        ResetGameStateSingleton();
        CreateGameState();
        _controller = CreateController();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var asset in _createdAssets)
        {
            Object.DestroyImmediate(asset);
        }
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        ResetGameStateSingleton();
        Time.timeScale = 1f;
    }

    [Test]
    public void DoesCurrentLevelHaveBossPowers_ReturnsFalseWhenNoneConfigured()
    {
        GameState.Instance.CurrentLevel = new FakeLevelNoBossPowers();

        bool hasBossPowers = _controller.DoesCurrentLevelHaveBossPowers();

        Assert.IsFalse(hasBossPowers);
    }

    [Test]
    public void DoesCurrentLevelHaveBossPowers_ReturnsTrueWhenAnyRoundHasBossPower()
    {
        GameState.Instance.CurrentLevel = new FakeLevelWithBossPowers();

        bool hasBossPowers = _controller.DoesCurrentLevelHaveBossPowers();

        Assert.IsTrue(hasBossPowers);
    }

    [Test]
    public void GetTotalBossPowersInCurrentLevel_CountsUniqueAcrossRounds()
    {
        GameState.Instance.CurrentLevel = new FakeLevelWithBossPowers();

        int count = _controller.GetTotalBossPowersInCurrentLevel();

        Assert.AreEqual(2, count, "Should dedupe powers by name across rounds.");
    }

    [Test]
    public void ApplyTimePenalty_ClampsToZeroAndUpdatesTimerUI()
    {
        var ui = CreateUiManager();
        _controller.GameUIManager = ui;

        var timer = (TimerController)GetPrivateField(_controller, "_timer");
        timer.Reset(2f);

        _controller.ApplyTimePenalty(3f);

        Assert.AreEqual(0f, timer.Remaining);
        var timerText = (TextMeshProUGUI)GetPrivateField(ui, "timerText");
        StringAssert.Contains("0:00", timerText.text);
    }

    [Test]
    public void TogglePause_TogglesTimeScaleBetweenZeroAndOne()
    {
        Time.timeScale = 1f;
        SetPrivateField(_controller, "<Phase>k__BackingField", GamePhase.Playing);

        _controller.TogglePause();
        Assert.AreEqual(0f, Time.timeScale);

        _controller.TogglePause();
        Assert.AreEqual(1f, Time.timeScale);
    }

    // --- Helpers ---

    private void ResetGameStateSingleton()
    {
        foreach (var existing in Object.FindObjectsByType<GameState>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var prop = typeof(GameState).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var setter = prop?.GetSetMethod(true);
        setter?.Invoke(null, new object[] { null });
    }

    private void CreateGameState()
    {
        var go = new GameObject("GameState");
        _createdObjects.Add(go);
        var gs = go.AddComponent<GameState>();

        // Unity may not invoke Awake in edit mode test instantiation; set singleton explicitly.
        var prop = typeof(GameState).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var setter = prop?.GetSetMethod(true);
        setter?.Invoke(null, new object[] { gs });
    }

    private GameController CreateController()
    {
        var go = new GameObject("GameController");
        _createdObjects.Add(go);
        var controller = go.AddComponent<GameController>();
        typeof(GameController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(controller, null);
        controller.GameUIManager = CreateUiManager();
        controller.limitedItemPool = new List<Item>();
        return controller;
    }

    private GameUIManager CreateUiManager()
    {
        var uiGo = new GameObject("GameUIManager");
        _createdObjects.Add(uiGo);
        var ui = uiGo.AddComponent<GameUIManager>();

        // Provide TextMeshProUGUI for timer updates so methods don't throw.
        var timerGo = new GameObject("TimerText");
        _createdObjects.Add(timerGo);
        var timer = timerGo.AddComponent<TextMeshProUGUI>();
        SetPrivateField(ui, "timerText", timer);

        return ui;
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(target, value);
    }

    private object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field.GetValue(target);
    }

    // --- Test doubles ---

    private class FakeLevelNoBossPowers : LevelConfig
    {
        public int GetTotalTime => 60;
        public int GetHeight(int round) => 7;
        public int GetWidth(int round) => 7;
        public int GetTargetScore(int round) => 100;
        public int GetTotalRounds => 1;
        public int GetLevelNumber => 99;
        public string GetLevelDescription() => "No boss powers.";
        public bool BossPowersPersist => false;
        public BossPowerMap GetBossPowersMap() => new BossPowerMap();
        public List<PowerUp> GetPowerUpsForRound(int round) => new List<PowerUp>();
        public List<MatchRule> GetMatchRules(int round) => new() { new MatchRule(MatchType.Snake, 3) };
        public LevelConfig CloneNew() => new FakeLevelNoBossPowers();
    }

    private class FakeLevelWithBossPowers : LevelConfig
    {
        private readonly BossPowerMap _map;

        public FakeLevelWithBossPowers()
        {
            _map = new BossPowerMap();
            _map.AddPower(1, new List<BossPowers> { new FirstItemsGivesNoPoints() });
            _map.AddPower(2, new List<BossPowers> { new FirstItemsGivesNoPoints(), new HalfPointsForFirstHalfOfLevel() });
        }

        public int GetTotalTime => 60;
        public int GetHeight(int round) => 7;
        public int GetWidth(int round) => 7;
        public int GetTargetScore(int round) => 100;
        public int GetTotalRounds => 2;
        public int GetLevelNumber => 98;
        public string GetLevelDescription() => "Boss powers present.";
        public bool BossPowersPersist => false;
        public BossPowerMap GetBossPowersMap() => _map;
        public List<PowerUp> GetPowerUpsForRound(int round) => new List<PowerUp>();
        public List<MatchRule> GetMatchRules(int round) => new() { new MatchRule(MatchType.Snake, 3) };
        public LevelConfig CloneNew() => new FakeLevelWithBossPowers();
    }
}
