using System.Collections;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class GameplaySmokeTests
{
    [UnityTest]
    public IEnumerator WildReleaseDragScoresTheWholeAnimalGroupAndUsesOneMove()
    {
        const string swipeKey="Prismatch.SwipeToMove";
        bool hadSwipe=PlayerPrefs.HasKey(swipeKey);int previousSwipe=PlayerPrefs.GetInt(swipeKey,1);
        PlayerPrefs.SetInt(swipeKey,0);
        try {
        SceneManager.LoadScene("prismatch");yield return null;yield return null;
        var controller=Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel=LevelLibrary.GetLevel(31).CloneNew();controller.NewGameButton();
        controller.ContinueFromLevelIntro();controller.OnPowerUpSelected();yield return null;
        var board=Board.Instance;var state=board.Strategy;var old=state.ReleaseCage;
        var source=board.Tiles[0,0];var cage=board.Tiles[old.X,old.Y];
        int before=controller.MovesRemaining;
        board.FinishCageDrag(source,cage);Assert.That(controller.MovesRemaining,Is.EqualTo(before),"Closed cages reject input without a move charge.");
        controller.PowerManager.RegisterPower(new IconTypeGivesBase(ItemType.Duck,5));
        controller.PowerManager.RegisterPower(new IconTypeGivesMulti(ItemType.Duck,2));
        for(int y=0;y<7;y++)for(int x=0;x<7;x++)board.Model[x,y]=(x+y)%2==0?ItemType.Bear:ItemType.Chicken;
        var ducks=new[]{new BoardModel.Cell(0,0),new(6,0),new(1,2),new(5,4),new(0,6),new(6,6)};
        foreach(var cell in ducks)board.Model[cell.X,cell.Y]=ItemType.Duck;
        for(int i=0;i<10;i++)state.PrepareWave(board.Model,new(){new(){new(old.X-1,old.Y)}},null);
        typeof(Board).GetMethod("SyncVisuals",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,null);
        Assert.That(cage.icon.enabled,Is.False,"The cage is an empty slot, not a scored animal.");
        Assert.That(board.CanDragIntoCage(source),Is.True);
        var pointer=new PointerEventData(EventSystem.current) {
            position=RectTransformUtility.WorldToScreenPoint(null,cage.transform.position),
            pointerDrag=source.gameObject,
            pointerCurrentRaycast=new RaycastResult{gameObject=cage.gameObject}
        };
        source.OnInitializePotentialDrag(pointer);Assert.That(pointer.pointerDrag,Is.Not.Null,"Ready cages allow dragging even if ordinary swipes are off.");
        source.OnBeginDrag(pointer);source.OnEndDrag(pointer);
        Assert.That(board.isSwapping,Is.True);
        Assert.That(controller.IsPresentingMechanic,Is.True);
        int scoreBeforePresentation=controller.CurrentScore;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(controller.CurrentScore,Is.EqualTo(scoreBeforePresentation),"Scoring waits for the capture presentation.");
        Assert.That(controller.IsPresentingMechanic,Is.True);
        float deadline=Time.realtimeSinceStartup+8;
        while(board.isSwapping&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(board.isSwapping,Is.False);Assert.That(controller.IsPresentingMechanic,Is.False);Assert.That(controller.Phase,Is.EqualTo(GamePhase.Playing));
        Assert.That(controller.MovesRemaining,Is.EqualTo(before-1));
        Assert.That(controller.CurrentScore,Is.GreaterThanOrEqualTo(162),"Six Ducks: (4 level base + 5 animal base) x 6 x (1 + 2 multi).");
        Assert.That(controller.PowerManager.AnimalHistory.Count(ItemType.Duck),Is.GreaterThanOrEqualTo(6));
        Assert.That(state.ReleaseCage,Is.Not.EqualTo(old));Assert.That(state.CageCapturePending,Is.False);
        Assert.That(GameObject.Find("Dragged cage animal"),Is.Null);
        Assert.That(controller.limitedItemPool.Exists(item=>item.ItemType==ItemType.Duck),Is.False);
        Assert.That(state.CapturedAnimals.Contains(ItemType.Duck),Is.True);
        controller.GenerateItemPool(7);
        Assert.That(controller.limitedItemPool.Exists(item=>item.ItemType==ItemType.Duck),Is.False,"Pool rebuilds preserve round removals.");
        for(int i=0;i<100;i++)Assert.That(controller.PickSpawnAnimal(),Is.Not.EqualTo(ItemType.Duck));
        controller.PrepareLevel(false,false);
        Assert.That(controller.limitedItemPool.Exists(item=>item.ItemType==ItemType.Duck),Is.True,"Retry restores the round roster.");
        // Existing scene/font warnings are unrelated; Unity still fails this test on runtime errors.
        } finally { if(hadSwipe)PlayerPrefs.SetInt(swipeKey,previousSwipe);else PlayerPrefs.DeleteKey(swipeKey); }
    }

    [UnityTest]
    public IEnumerator InventoryAndPickerReuseTheSamePanelLayoutAndHeight()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(2).CloneNew();
        controller.NewGameButton();
        controller.ContinueFromLevelIntro();
        yield return null;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var picker = Object.FindAnyObjectByType<PowerUpSelector>();
        var pickerPanel = (GameObject)typeof(PowerUpSelector).GetField("panel", flags).GetValue(picker);
        Assert.That(pickerPanel.activeSelf, Is.True);
        var choicesBefore = pickerPanel.GetComponentsInChildren<ShortPowerCardUI>(true);
        var owned = new AnimalDoublePower(ItemType.Bear);
        controller.PowerManager.RegisterPower(owned);
        var inventory = Object.FindAnyObjectByType<ActivePowerUpsUI>();
        inventory.HidePanel();
        controller.ToggleActivePowerUpScreen();
        Canvas.ForceUpdateCanvases();
        var inventoryPanel = (GameObject)typeof(ActivePowerUpsUI).GetField("powerUpPanel", flags).GetValue(inventory);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection));
        Assert.That(inventory.IsOpen, Is.True);
        Assert.That(pickerPanel.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        Assert.That(pickerPanel.GetComponent<CanvasGroup>().alpha, Is.Zero);
        Assert.That(System.Array.Exists(inventoryPanel.GetComponentsInChildren<ShortPowerCardUI>(),
            card => card.PowerName == owned.Name), Is.True);
        Assert.That(pickerPanel.GetComponent<PowerPanelLayout>(), Is.Not.Null);
        Assert.That(inventoryPanel.GetComponent<PowerPanelLayout>(), Is.Not.Null);
        var pickerRect = (RectTransform)pickerPanel.transform;
        var inventoryRect = (RectTransform)inventoryPanel.transform;
        Assert.That(inventoryRect.rect.height, Is.EqualTo(pickerRect.rect.height).Within(.1f));
        Assert.That(inventoryRect.rect.width, Is.EqualTo(pickerRect.rect.width).Within(.1f));
        Assert.That(inventoryRect.anchoredPosition, Is.EqualTo(pickerRect.anchoredPosition));
        controller.ToggleActivePowerUpScreen();
        Assert.That(inventory.IsOpen, Is.False);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection));
        Assert.That(pickerPanel.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        Assert.That(pickerPanel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
        CollectionAssert.AreEqual(choicesBefore, pickerPanel.GetComponentsInChildren<ShortPowerCardUI>(true),
            "Inspecting owned powers must preserve the exact offered cards without rerolling.");
    }

    [UnityTest]
    public IEnumerator OnlyAcceptedManualShufflesSpendTicketsAndArmPayoffs()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(20).CloneNew();
        controller.NewGameButton();
        var tickets = new ShufflePower(ShuffleStyle.Opening);
        var slate = new ShufflePower(ShuffleStyle.Salvage);
        controller.PowerManager.RegisterPower(tickets);
        controller.PowerManager.RegisterPower(slate);
        controller.OnShuffleBoardClicked();
        Assert.That(tickets.FreeUses, Is.EqualTo(3), "Briefings must reject shuffle input.");
        Assert.That(slate.Armed, Is.False);
        controller.ContinueFromLevelIntro();
        controller.OnPowerUpSelected();
        Board.Instance.ResetBoard();
        Assert.That(slate.Armed, Is.False, "Board recovery is not a player shuffle.");
        controller.OnShuffleBoardClicked();
        Assert.That(tickets.FreeUses, Is.EqualTo(2));
        Assert.That(slate.Armed, Is.True);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator NoMovesShufflesSpendTicketsArmPayoffsAndApplyNormalCosts()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(20).CloneNew();
        controller.NewGameButton();
        controller.ContinueFromLevelIntro();
        controller.OnPowerUpSelected();
        var tickets = new ShufflePower(ShuffleStyle.Opening);
        var payoff = new ShufflePower(ShuffleStyle.Salvage);
        controller.PowerManager.RegisterPower(tickets);
        controller.PowerManager.RegisterPower(payoff);
        var board = Board.Instance;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            controller.BeginResolution();
            for (int y = 0; y < board.Model.Height; y++)
                for (int x = 0; x < board.Model.Width; x++) board.Model[x,y] = null;
            Assert.That(board.Model.HasLegalMove(), Is.False);
            float timeBefore = controller.CurrentTime;
            float cost = controller.ShuffleCost;
            controller.EndResolution();
            Assert.That(tickets.FreeUses, Is.EqualTo(Mathf.Max(0, 2 - attempt)));
            Assert.That(payoff.Armed, Is.True);
            Assert.That(board.Model.HasLegalMove(), Is.True);
            Assert.That(controller.CurrentTime, Is.EqualTo(timeBefore - cost).Within(.01f));
            Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator BossBriefingShowsBossEffectsWithoutRepeatingLevelLesson()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(9).CloneNew();
        controller.NewGameButton();
        yield return null;
        Assert.That(Object.FindObjectsByType<LevelLessonAnimation>().Length, Is.EqualTo(1));
        for (int round = 2; round <= controller.maxRounds; round++)
            controller.PrepareLevel(true, false);
        yield return null;
        Assert.That(controller.isBossLevel, Is.True);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.LevelIntro));
        Assert.That(Object.FindObjectsByType<LevelLessonAnimation>().Length, Is.Zero);
        var panel = GameObject.Find("LevelIntro");
        Assert.That(panel, Is.Not.Null);
        var labels = panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (var boss in controller.PowerManager.ActiveBossPowers()) {
            Assert.That(System.Array.Exists(labels, text => text.text == boss.Name), Is.True);
            Assert.That(System.Array.Exists(labels, text => text.text == boss.GetCurrentPowerDescription()), Is.True);
        }
        Canvas.ForceUpdateCanvases();
        foreach (var label in labels) {
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, label.name);
        }
        controller.ContinueFromLevelIntro();
        yield return null;
        Assert.That(panel == null || !panel.activeInHierarchy, Is.True);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator NewMechanicLevelsStartFromFreshScenes()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        foreach (int number in new[] { 9, 10, 11 }) {
            GameState.Instance.CurrentLevel = LevelLibrary.GetLevel(number).CloneNew();
            SceneManager.LoadScene("prismatch");
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<GameController>();
            Assert.That(Board.Instance.Campaign, Is.Not.Null);
            Assert.That(controller.Phase, Is.EqualTo(GamePhase.LevelIntro));
            controller.ContinueFromLevelIntro();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }

    [UnityTest]
    public IEnumerator EveryLevelLessonAutoplaysUnscaledAndCleansUpWhenContinuing()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var ui = Object.FindAnyObjectByType<GameUIManager>();
        try {
            Time.timeScale = 0;
            for (int number = 0; number <= LevelLibrary.TotalLevels; number++) {
                GameState.Instance.CurrentLevel = number == 0 ? new FreePlayConfig() : LevelLibrary.GetLevel(number).CloneNew();
                controller.NewGameButton();
                Time.timeScale = 0;
                yield return null;
                var lessons = Object.FindObjectsByType<LevelLessonAnimation>();
                if (controller.isBossLevel && controller.CurrentRound > 1) {
                    Assert.That(lessons.Length, Is.Zero, "Boss briefings focus on the boss effects.");
                    controller.ContinueFromLevelIntro();
                    yield return null;
                    continue;
                }
                Assert.That(lessons.Length, Is.EqualTo(1), "One automatic lesson per briefing.");
                var lesson = lessons[0];
                Assert.That(lesson.IsPlaying, Is.True, "Explicit Play is required with this project's DOTween settings.");
                yield return new WaitForSecondsRealtime(.18f);
                Assert.That(lesson.PlaybackTime, Is.GreaterThan(.05f), "Lessons must animate while the game clock is paused.");
                Assert.That(lesson.GetComponentInParent<UnityEngine.UI.ScrollRect>(), Is.Null,
                    "The level tutorial must fit a fixed viewport without scrolling.");
                Assert.That(lesson.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(2));
                lesson.NextStep();
                Assert.That(lesson.CurrentStep, Is.EqualTo(1));
                Assert.That(lesson.PlaybackTime, Is.EqualTo(lesson.Storyboard.StepStarts[1]).Within(.01f));
                lesson.OnEndDrag(new PointerEventData(EventSystem.current) {
                    pressPosition = new Vector2(0, 0), position = new Vector2(Screen.width * .5f, 0)
                });
                Assert.That(lesson.CurrentStep, Is.Zero, "Swiping right returns to the previous step.");
                lesson.PreviousStep();
                Assert.That(lesson.CurrentStep, Is.Zero, "The first step must stay selected at the boundary.");
                lesson.Seek(lesson.Storyboard.Duration * .55f);
                Canvas.ForceUpdateCanvases();
                foreach (var text in lesson.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) {
                    text.ForceMeshUpdate();
                    Assert.That(text.isTextOverflowing, Is.False, $"Level {number}: {text.name} must fit in the visual lesson.");
                }
                if (number == 3) {
                    lesson.Seek(1.5f); Canvas.ForceUpdateCanvases();
                    var boxes = lesson.transform.Find("Looping lesson stage/Example board/Fixed boxes");
                    var home = boxes.GetChild(0).localPosition;
                    lesson.Seek(2.05f);
                    Assert.That(lesson.transform.Find("Looping lesson stage/Example board/Lightning zigzag").gameObject.activeInHierarchy, Is.True);
                    lesson.Seek(3.2f);
                    Assert.That(boxes.GetChild(0).localPosition, Is.EqualTo(home), "Boxes stay fixed during lightning.");
                    var badge = lesson.transform.Find("Looping lesson stage/Example board/Moving animals/Example animal 0/Reward badge/Badge label").GetComponent<TMPro.TextMeshProUGUI>();
                    Assert.That(badge.text, Is.EqualTo("x2"));
                    Assert.That(badge.gameObject.activeInHierarchy, Is.True);
                    lesson.Seek(lesson.Storyboard.Duration + .3f);
                    Assert.That(badge.transform.parent.gameObject.activeSelf, Is.False, "The next loop starts uncharged.");
                }
                lesson.Play();
                Assert.That(lesson.IsPlaying, Is.True);
                controller.ContinueFromLevelIntro();
                Assert.That(lesson.IsPlaying, Is.False, "Hiding the briefing immediately kills its infinite tween.");
                yield return null;
                Assert.That(Object.FindObjectsByType<LevelLessonAnimation>(), Is.Empty);
                Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection));
            }
        }
        finally { Time.timeScale = 1; ui.HideLevelIntro(); }
    }

    [UnityTest]
    public IEnumerator NewCampaignLevelsShowProgressAndKeepGridBoxesFixedDuringRefill()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller=Object.FindAnyObjectByType<GameController>();
        var board=Board.Instance;
        var ui=Object.FindAnyObjectByType<GameUIManager>();
        foreach(int number in new[]{9,10,11}) {
            GameState.Instance.CurrentLevel=LevelLibrary.GetLevel(number).CloneNew();
            controller.NewGameButton();
            controller.ContinueFromLevelIntro();
            yield return null;
            Canvas.ForceUpdateCanvases();
            ui.ShowBoardStats(true);ui.UpdateAnimalBonus(controller,board);
            var label=(TMPro.TextMeshProUGUI)typeof(GameUIManager).GetField("strategyStatus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
            Assert.That(label.gameObject.activeInHierarchy,Is.True);
            Assert.That(label.text,Does.Contain(LevelBonuses.ShortDescription(((CampaignLevel)GameState.Instance.CurrentLevel).Bonus)));
            Assert.That(label.text,Does.Contain(number==9?"Edges lit":number==10?"Treasures":"Gravity"));
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing,Is.False);
            if(number==10) {
                var treasure=board.Model.Treasure.Value;
                Assert.That(board.Tiles[treasure.X,treasure.Y].icon.transform.Find("Golden treasure tile"),Is.Not.Null);
            }
            var homes=new System.Collections.Generic.Dictionary<Tile,Vector3>();
            foreach(var tile in board.Tiles)homes[tile]=tile.background.transform.position;
            var moves=board.Model.Refill(new[]{new BoardModel.Cell(3,3)},board.RefillStrategy,()=>controller.limitedItemPool[0].ItemType);
            int version=(int)typeof(Board).GetField("generation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
            var task=(System.Threading.Tasks.Task)typeof(Board).GetMethod("AnimateGravity",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,new object[]{moves,version});
            yield return new WaitForSecondsRealtime(.08f);
            Canvas.ForceUpdateCanvases();
            foreach(var tile in board.Tiles) {
                Assert.That(tile.background.enabled,Is.True);
                Assert.That(Vector3.Distance(tile.background.transform.position,homes[tile]),Is.LessThan(.01f));
            }
            float timeout=Time.realtimeSinceStartup+3f;
            while(!task.IsCompleted&&Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(task.IsCompletedSuccessfully,Is.True);
            controller.NewGameButton();
            yield return null;
            Assert.That(board.Campaign.LitEdges,Is.Zero);
            Assert.That(board.Campaign.Treasures,Is.Zero);
            Assert.That(board.Campaign.TideMoves,Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator RewardParticlesAreBoundedIgnoreInputAndCleanUp()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var ui = Object.FindAnyObjectByType<GameUIManager>();
        var tiles = new System.Collections.Generic.List<Tile> { Board.Instance.Tiles[0, 0] };
        for (int n = 0; n < 10; n++) ui.ShowRewardBurst(.3f, tiles);
        int count = 0;
        foreach (var image in Object.FindObjectsByType<UnityEngine.UI.Image>())
        {
            if (image.name != "ClearSpark") continue;
            count++;
            Assert.That(image.raycastTarget, Is.False);
        }
        Assert.That(count, Is.GreaterThan(0).And.LessThanOrEqualTo(48));
        yield return new WaitForSecondsRealtime(.5f);
        foreach (var image in Object.FindObjectsByType<UnityEngine.UI.Image>())
            Assert.That(image.name, Is.Not.EqualTo("ClearSpark"));
    }

    [UnityTest]
    public IEnumerator ProgressFillAnimatesWithAutomaticTweenPlaybackDisabled()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var ui = Object.FindAnyObjectByType<GameUIManager>();
        ui.ShowBoardStats(true);
        var fill = (UnityEngine.UI.Image)typeof(GameUIManager).GetField("scoreProgressFill",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
        // The project DOTween settings disable automatic playback.
        {
            foreach (var goal in new[] { GoalType.Score, GoalType.BlocksCleared })
            {
                ui.UpdateTarget(200);
                ui.UpdateGoalProgress(30, goal);
                Assert.That(fill.fillAmount, Is.EqualTo(0f).Within(.001f));
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(fill.fillAmount, Is.GreaterThan(0f));
                ui.UpdateGoalProgress(100, goal);
                yield return new WaitForSecondsRealtime(.45f);
                Assert.That(fill.fillAmount, Is.EqualTo(.5f).Within(.01f));
                ui.UpdateGoalProgress(250, goal);
                yield return new WaitForSecondsRealtime(.45f);
                Assert.That(fill.fillAmount, Is.EqualTo(1f).Within(.01f));
                ui.UpdateTarget(300);
                Assert.That(fill.fillAmount, Is.EqualTo(0f));
            }
        }
    }

    [UnityTest]
    public IEnumerator PowerCardClickStartsRoundAfterBriefing()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        GameState.Instance.CurrentLevel = new FreePlayConfig();
        controller.NewGameButton();
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.LevelIntro));
        controller.ContinueFromLevelIntro();
        yield return null;
        var card = Object.FindAnyObjectByType<ShortPowerCardUI>();
        Assert.That(card, Is.Not.Null);
        var button = (UnityEngine.UI.Button)typeof(ShortPowerCardUI)
            .GetField("button", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(card);
        button.onClick.Invoke();
        button.onClick.Invoke(); // Repeated taps must not grant two powers.
        float timeout = Time.realtimeSinceStartup + 2f;
        while (controller.Phase != GamePhase.Playing && Time.realtimeSinceStartup < timeout)
            yield return null;
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(controller.PowerManager.ActivePowers().Count, Is.EqualTo(1));
        Assert.That(controller.CanInteract, Is.True);
    }

    [UnityTest]
    public IEnumerator Gravity_AllDirectionsRenderAboveTilesAndRestoreIcons()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var board = Board.Instance;
        foreach (var direction in new[] { ReplacementStrategy.North, ReplacementStrategy.South,
            ReplacementStrategy.East, ReplacementStrategy.West })
        {
            GameState.Instance.CurrentLevel = new FreePlayConfig(replacement: direction);
            controller.NewGameButton();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var moves = board.Model.Refill(new[] { new BoardModel.Cell(3, 3) }, direction,
                () => controller.limitedItemPool[0].ItemType);
            int version = (int)typeof(Board).GetField("generation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
            var task = (System.Threading.Tasks.Task)typeof(Board).GetMethod("AnimateGravity",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(board, new object[] { moves, version });
            var overlay = board.transform.Find("GravityOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(board.transform.childCount - 1), "Falling blocks must draw above all tile backgrounds.");
            Assert.That(overlay.childCount, Is.EqualTo(moves.Count));
            Assert.That(overlay.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout, Is.True);
            foreach (var move in moves) Assert.That(board.Tiles[move.To.X, move.To.Y].icon.enabled, Is.False);
            float timeout = Time.realtimeSinceStartup + 2f;
            while (!task.IsCompleted && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(task.IsCompletedSuccessfully, Is.True);
            yield return null;
            Assert.That(board.transform.Find("GravityOverlay"), Is.Null);
            foreach (var tile in board.Tiles) Assert.That(tile.icon.enabled, Is.True);

            // A round reset during an animation must also restore the originals.
            moves = board.Model.Refill(new[] { new BoardModel.Cell(3, 3) }, direction,
                () => controller.limitedItemPool[0].ItemType);
            task = (System.Threading.Tasks.Task)typeof(Board).GetMethod("AnimateGravity",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(board, new object[] { moves, version });
            board.CancelResolution();
            yield return null;
            Assert.That(board.transform.Find("GravityOverlay"), Is.Null);
            foreach (var tile in board.Tiles) Assert.That(tile.icon.enabled, Is.True);
        }
    }

    [UnityTest]
    public IEnumerator HeaderShowsBudgetAboveProgressInsteadOfDuplicateScores()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var ui = Object.FindAnyObjectByType<GameUIManager>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ui.ShowBoardStats(true);
        foreach (string field in new[] { "scoreText", "targetText" })
        {
            var obsolete = (TMPro.TextMeshProUGUI)typeof(GameUIManager).GetField(field, flags).GetValue(ui);
            Assert.That(obsolete.transform.parent.gameObject.activeSelf, Is.False);
        }
        var timer = (TMPro.TextMeshProUGUI)typeof(GameUIManager).GetField("timerText", flags).GetValue(ui);
        var progress = (GameObject)typeof(GameUIManager).GetField("scoreProgressRoot", flags).GetValue(ui);
        foreach (bool timed in new[] { true, false })
        {
            if (timed) ui.UpdateTimer(35); else ui.UpdateMoves(12);
            Canvas.ForceUpdateCanvases();
            timer.ForceMeshUpdate();
            Assert.That(timer.text, Does.Contain(timed ? "0:35" : "12"));
            Assert.That(timer.isTextOverflowing, Is.False);
            Assert.That(progress.activeInHierarchy, Is.True);
            var budgetCorners = new Vector3[4];
            var progressCorners = new Vector3[4];
            ((RectTransform)timer.transform.parent).GetWorldCorners(budgetCorners);
            ((RectTransform)progress.transform).GetWorldCorners(progressCorners);
            Assert.That(budgetCorners[0].y, Is.GreaterThan(progressCorners[1].y),
                "The timer or move budget must fit above the retained progress bar.");
        }
    }

    [UnityTest]
    public IEnumerator FreePlayNumbers_CreateCaretAcceptTypingAndCommit()
    {
        if (GameState.Instance != null) GameState.Instance.CurrentLevel = new FreePlayConfig();
        SceneManager.LoadScene("Level Select");
        yield return null;
        yield return null;
        foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>())
            if (button.name == "Free Play") { button.onClick.Invoke(); break; }
        yield return null;
        var fields = Object.FindObjectsByType<TMPro.TMP_InputField>();
        Assert.That(fields.Length, Is.EqualTo(8));
        Assert.That(EventSystem.current, Is.Not.Null);
        foreach (var field in fields)
        {
            Assert.That(typeof(TMPro.TMP_InputField).GetField("m_CachedInputRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(field), Is.Not.Null,
                $"{field.name} must initialize its caret after its text reference is assigned.");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(field.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            yield return null;
            Assert.That(field.isFocused, Is.True, $"{field.name} should focus on click.");
            field.selectionStringAnchorPosition = 0;
            field.selectionStringFocusPosition = field.text.Length;
            field.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Alpha5, character = '5' });
            Assert.That(field.text, Is.EqualTo("5"), $"{field.name} should replace the selected number.");
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(int.TryParse(field.text, out _), Is.True);
            Assert.That(field.isFocused, Is.False);
        }
        var selector = Object.FindAnyObjectByType<LevelSelectUI>();
        var privateFields = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.That(typeof(LevelSelectUI).GetField("width", privateFields).GetValue(selector), Is.EqualTo(5));
        Assert.That(typeof(LevelSelectUI).GetField("points", privateFields).GetValue(selector), Is.EqualTo(5));
        Assert.That(typeof(LevelSelectUI).GetField("seconds", privateFields).GetValue(selector), Is.EqualTo(10),
            "Committing a value below the allowed time budget should clamp it.");
    }

    [UnityTest]
    public IEnumerator GameplayScene_StartsWithPlayableBoardAndPowerChoice()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = UnityEngine.Object.FindAnyObjectByType<GameController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(Board.Instance, Is.Not.Null);
        Assert.That(Board.Instance.Model, Is.Not.Null);
        Assert.That(Board.Instance.Model.Matches(), Is.Empty);
        Assert.That(Board.Instance.Model.HasLegalMove(), Is.True);
        if (controller.Phase == GamePhase.LevelIntro) controller.ContinueFromLevelIntro();
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection).Or.EqualTo(GamePhase.BossIntro));
        if (controller.Phase == GamePhase.PowerSelection)
        {
            var card = Object.FindAnyObjectByType<ShortPowerCardUI>();
            Assert.That(card, Is.Not.Null);
            var viewport = Object.FindAnyObjectByType<PowerUpSelector>()
                .GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).viewport;
            Assert.That(((RectTransform)card.transform).rect.width,
                Is.EqualTo(viewport.rect.width - 32f).Within(1f), "Power cards should fill their parent with 16-unit side margins.");
            Assert.That(((RectTransform)card.transform).rect.height, Is.GreaterThanOrEqualTo(180f));
        }
    }

    [UnityTest]
    public IEnumerator ClickingTwoTiles_ResolvesAMoveAndReturnsInputControl()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var board = Board.Instance;
        var powerCard = Object.FindAnyObjectByType<ShortPowerCardUI>();
        Assert.That(powerCard, Is.Not.Null, "Power picker should create selectable cards.");
        var powerButton = powerCard.GetComponentInChildren<UnityEngine.UI.Button>();
        Assert.That(powerButton, Is.Not.Null);
        powerButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True, "Power selection should enter Playing.");

        Tile first = null, second = null;
        for (int a = 0; a < board.Width * board.Height && first == null; a++)
        for (int b = a + 1; b < board.Width * board.Height; b++)
        {
            var ca = new BoardModel.Cell(a % board.Width, a / board.Width);
            var cb = new BoardModel.Cell(b % board.Width, b / board.Width);
            if (board.Model[ca.X, ca.Y] == board.Model[cb.X, cb.Y]) continue;
            board.Model.Swap(ca, cb);
            bool legal = board.Model.Connected(ca.X, ca.Y).Count >= 3 || board.Model.Connected(cb.X, cb.Y).Count >= 3;
            board.Model.Swap(ca, cb);
            if (!legal) continue;
            first = board.Tiles[ca.X, ca.Y]; second = board.Tiles[cb.X, cb.Y]; break;
        }
        Assert.That(first, Is.Not.Null, "Generated board promised a legal move.");
        Assert.That(first.button, Is.Not.Null);
        Assert.That(second.button, Is.Not.Null);
        var firstStart = first.icon.transform.position;
        var secondStart = second.icon.transform.position;
        first.button.onClick.Invoke();
        Assert.That(first.background.color, Is.EqualTo(Color.darkGray), "First click should select the tile.");
        second.button.onClick.Invoke();
        Assert.That(board.isSwapping, Is.True, "Second click should start resolution.");
        yield return new WaitForSeconds(.08f);
        Assert.That(first.icon.transform.position, Is.Not.EqualTo(firstStart), "First icon should visibly move.");
        Assert.That(second.icon.transform.position, Is.Not.EqualTo(secondStart), "Second icon should visibly move.");
        float timeout = Time.realtimeSinceStartup + 4f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(board.isSwapping, Is.False, "Resolution should release the board lock.");
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(controller.CurrentScore, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator ClickingTwoTiles_WhenTimeScaleIsZero_DoesNotLockTheBoard()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var board = Board.Instance;
        Object.FindAnyObjectByType<ShortPowerCardUI>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True);

        var first = board.Tiles[0, 0];
        var second = board.Tiles[1, 0];
        Time.timeScale = 0f;
        first.button.onClick.Invoke();
        second.button.onClick.Invoke();
        Assert.That(board.isSwapping, Is.True);

        float timeout = Time.realtimeSinceStartup + 2f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Time.timeScale = 1f;
        Assert.That(board.isSwapping, Is.False, "A frozen scaled clock must not strand board input.");
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [UnityTest]
    public IEnumerator CompletingRound_SlidesBoardLeftBeforeNextPowerChoice()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var boardRect = (RectTransform)Board.Instance.transform;
        float startX = boardRect.anchoredPosition.x;

        typeof(GameController).GetField("score", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, controller.CurrentTarget);
        typeof(GameController).GetField("<Phase>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, GamePhase.Resolving);
        controller.EndResolution();

        Assert.That(controller.Phase, Is.EqualTo(GamePhase.LevelComplete));
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(boardRect.anchoredPosition.x, Is.LessThan(startX), "Board should move off the left side.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while (controller.Phase == GamePhase.LevelComplete && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(controller.CurrentRound, Is.EqualTo(2));
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection).Or.EqualTo(GamePhase.BossIntro));
        Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(startX).Within(.1f));
    }

    [UnityTest]
    public IEnumerator DraggingFromOneTileToAnother_SubmitsTheSameMoveAsTwoClicks()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        Object.FindAnyObjectByType<ShortPowerCardUI>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True);

        var board = Board.Instance;
        var source = board.Tiles[0, 0];
        var destination = board.Tiles[1, 0];
        var pointer = new PointerEventData(EventSystem.current)
        {
            pointerCurrentRaycast = new RaycastResult { gameObject = destination.gameObject }
        };
        ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.beginDragHandler);
        Assert.That(source.background.color, Is.EqualTo(Color.darkGray));
        ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.endDragHandler);
        Assert.That(board.isSwapping, Is.True, "Releasing over another tile should begin a swap.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(board.isSwapping, Is.False);
    }
}
