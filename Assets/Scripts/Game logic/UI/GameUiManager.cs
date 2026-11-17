using DG.Tweening;
﻿using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText, targetText, timerText, roundText;
    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private RectTransform scoreAnchorTransform, canvasTransform;
    private int previousScore, latestTarget, latestLevel = 1, latestRound = 1;
    private TextMeshProUGUI messageText, pauseText, endTitle, endSummary;
    private TextMeshProUGUI basePointsText, multiplierText;
    private TextMeshProUGUI powersButtonText, bossButtonText, soundButtonText;
    private TextMeshProUGUI swipeToMoveButtonText;
    private TextMeshProUGUI shuffleButtonText;
    private Image restartButtonBackground;
    private TextMeshProUGUI scoreProgressText;
    private Image scoreProgressFill;
    private Tween scoreProgressTween;
    private float displayedProgressTarget = -1f;
    private const float ProgressFillDuration = .35f;
    // Animal bonus/footer messages share y=314..426 below the raised grid.
    // Levels without an animal bonus retain the feedback slot at y=282..358.
    private const float FooterStatsY = 210f;
    private GameObject boardScoreStats;
    private GameObject scoreProgressRoot;
    private GameObject endPanel;
    private GameObject settingsPanel, resetConfirmation;
    private TextMeshProUGUI animationSpeedButtonText;
    private GameObject analyticsPanel;
    private Button retryButton;
    private Button endNewRunButton;
    private Image endAccent;
    private TextMeshProUGUI endContext;
    private readonly TextMeshProUGUI[] endStatLabels = new TextMeshProUGUI[6];
    private readonly TextMeshProUGUI[] endStats = new TextMeshProUGUI[6];
    private Coroutine messageRoutine;
    private Image messageBackground;
    private GameObject statDetailsPanel;
    private RectTransform budgetPanel;
    private readonly Vector3[] headerBoardCorners = new Vector3[4];
    private Image budgetBackground, budgetFill;
    private GameController budgetController;
    private bool budgetIsLow;
    private float budgetPulse;
    private HabitatStyle? currentHabitat;
    private RectTransform animalBonusPanel;
    private TextMeshProUGUI animalBonusLabel;
    private TextMeshProUGUI strategyStatus;
    private RectTransform strategyPanel;
    private readonly System.Collections.Generic.List<Image> animalBonusIcons = new();
    private readonly System.Collections.Generic.List<TextMeshProUGUI> animalBonusNames = new();
    private bool boardStatsVisible,animalBonusActive;
    private int animalBonusTargetCount;
    public void UpdateAnimalBonus(GameController controller,Board board)
    {
        // Characteristics now report actual activations in the shared icon row.
        animalBonusActive=false;
        if(animalBonusPanel!=null)animalBonusPanel.gameObject.SetActive(false);
        if(controller!=null&&board?.Model!=null&&canvasTransform!=null) {
            if(strategyPanel==null) {
                strategyPanel=new GameObject("Level power status panel",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
                strategyPanel.SetParent(canvasTransform,false);
                strategyPanel.anchorMin=strategyPanel.anchorMax=new Vector2(.5f,1);
                strategyPanel.pivot=new Vector2(.5f,1);
                var background=strategyPanel.GetComponent<Image>();
                PrismatchTheme.Round(background,PrismatchTheme.Navy);background.raycastTarget=false;
                PrismatchTheme.Shadow(background,new Color(0,0,0,.3f),new Vector2(0,-4));
                strategyStatus=CreateText("Strategy status",new Vector2(.5f,1),Vector2.zero,32,TextAlignmentOptions.Center,strategyPanel);
                strategyStatus.rectTransform.pivot=new Vector2(.5f,1);strategyStatus.rectTransform.anchoredPosition=new Vector2(0,-12);
                strategyStatus.color=PrismatchTheme.Cream;strategyStatus.fontStyle=FontStyles.Bold;
                strategyStatus.enableAutoSizing=false;strategyStatus.textWrappingMode=TextWrappingModes.Normal;strategyStatus.raycastTarget=false;
            }
            strategyStatus.text=BuildLevelPowerStatus(controller,board);
            LayoutStrategyPanel();
            strategyPanel.gameObject.SetActive(boardStatsVisible);
        }else if(strategyPanel!=null)strategyPanel.gameObject.SetActive(false);
        ReserveStrategySpace(controller!=null&&board?.Model!=null);
    }
    public static string BuildLevelPowerStatus(GameController controller, Board board)
    {
        var level = GameState.Instance?.CurrentLevel as CampaignLevel;
        var bonus = level?.Bonus ?? LevelBonus.None;
        var model = board.Model;
        var campaign = model.Campaign;
        string rule = LevelBonuses.ShortDescription(bonus);
        string status = board.Strategy?.Status ?? bonus switch
        {
            LevelBonus.BorderPatrol => $"Edges lit {CountLitEdges(campaign.LitEdges)}/4",
            LevelBonus.TreasureTrail => campaign.TreasureReady ? "Next play ready for 5x points" : $"Treasures {campaign.Treasures % 3}/3",
            LevelBonus.ChangingTides => campaign.TideReady ? "Next play ready for 3x points" : $"Moves {campaign.TideMoves}/4 · Gravity {campaign.Gravity}",
            LevelBonus.AlternatingCurrent => $"3x points for {model.SpotlightAnimal} · Next {model.NextSpotlightAnimal}",
            LevelBonus.LeastMatched => "2x points for " + string.Join(", ", controller.PowerManager.AnimalHistory.LeastMatched(ItemUtils.GetItemTypes(controller.IconList))),
            LevelBonus.GardenPlots => $"Green spaces {CountPlantedSpaces(model)}/{model.Width * model.Height}",
            LevelBonus.ChainReaction => "Ready for cascades",
            LevelBonus.Cascade => "Ready for cascades",
            LevelBonus.LargeGroup => "Ready for a match of 6+",
            LevelBonus.Square => "Ready for a 2x2 square",
            _ => $"Goal {controller.GoalProgress:N0}/{controller.CurrentTarget:N0}"
        };
        if (level?.LightningLines == true)
        {
            rule += "\nMatch 5 in a straight line to strike it. Charged spaces give 2x points.";
            status += " · Lightning ready for a straight match of 5+";
        }
        return rule + "\n<size=85%>" + status + "</size>";
    }
    private static int CountLitEdges(int edges)
    {
        int count = 0;
        for (int side = 0; side < 4; side++) count += (edges >> side) & 1;
        return count;
    }
    private static int CountPlantedSpaces(BoardModel model)
    {
        int count = 0;
        for (int y = 0; y < model.Height; y++)
            for (int x = 0; x < model.Width; x++)
                if (model.IsPlanted(x,y)) count++;
        return count;
    }
    private void LayoutStrategyPanel()
    {
        if(strategyPanel==null||canvasTransform==null)return;
        float width=Mathf.Min(720,canvasTransform.rect.width-32);
        float textHeight=Mathf.Max(44,strategyStatus.GetPreferredValues(strategyStatus.text,width-36,0).y);
        strategyPanel.sizeDelta=new Vector2(width,textHeight+28);
        strategyStatus.rectTransform.sizeDelta=new Vector2(width-36,textHeight+4);
    }
    private void ReserveStrategySpace(bool hasStrategy)
    {
        var area=canvasTransform?.Find("BoardArea") as RectTransform;
        if(area!=null)area.offsetMax=new Vector2(-16,hasStrategy?-(Mathf.Max(72,strategyPanel?.rect.height??72)+300):-320);
    }

    public void ApplyHabitatTheme(bool bossRound = false)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null) return;
        var boardArea=canvasTransform.Find("BoardArea") as RectTransform;
        if(boardArea!=null)boardArea.offsetMin=new Vector2(16,(GameState.Instance?.CurrentLevel as CampaignLevel)?.Bonus is LevelBonus.LeastMatched or LevelBonus.AlternatingCurrent or LevelBonus.BorderPatrol or LevelBonus.TreasureTrail or LevelBonus.ChangingTides?434:374);
        ReserveStrategySpace(true);
        currentHabitat = (GameState.Instance?.CurrentLevel as CampaignLevel)?.Habitat;
        PrismatchTheme.ApplyBackdrop(canvasTransform, currentHabitat, bossRound);
        Color accent = currentHabitat.HasValue ? HabitatArtwork.Colors(currentHabitat.Value).Accent : PrismatchTheme.Green;
        if (scoreProgressFill != null) scoreProgressFill.color = accent;
    }

    public void InitializeRunUI(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null || messageText != null) return;
        PrismatchTheme.ApplyBackdrop(canvasTransform);
        StyleHud();
        StyleBudgetDisplay(controller);
        StyleSettingsButton(controller);
        BuildScoreProgress();
        StyleBottomBar(controller);
        var notice = new GameObject("BoardFeedback", typeof(RectTransform), typeof(Image));
        notice.transform.SetParent(canvasTransform, false);
        var noticeRect = (RectTransform)notice.transform;
        noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(.5f, 0);
        noticeRect.anchoredPosition = new Vector2(0, 320);
        noticeRect.sizeDelta = new Vector2(620, 76);
        messageBackground = notice.GetComponent<Image>();
        PrismatchTheme.Round(messageBackground, PrismatchTheme.Navy);
        messageBackground.raycastTarget = false;
        messageText = CreateText("RuleMessage", new Vector2(.5f, .5f), Vector2.zero, 27, TextAlignmentOptions.Center, noticeRect);
        messageText.rectTransform.anchorMin = Vector2.zero;
        messageText.rectTransform.anchorMax = Vector2.one;
        messageText.rectTransform.offsetMin = new Vector2(12, 6);
        messageText.rectTransform.offsetMax = new Vector2(-12, -6);
        messageText.fontStyle = FontStyles.Bold;
        messageText.margin = Vector4.zero;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 20;
        messageText.fontSizeMax = 27;
        messageText.raycastTarget = false;
        PrismatchTheme.Shadow(messageBackground, new Color(0f, .04f, .12f, .35f), new Vector2(0, -3));
        notice.SetActive(false);
        BuildBoardStats();
        pauseText = CreateText("PauseMessage", new Vector2(.5f, .5f), new Vector2(500, 100), 52, TextAlignmentOptions.Center);
        pauseText.text = "PAUSED"; pauseText.gameObject.SetActive(false);
        BuildEndPanel(controller);
        BuildSettingsPanel(controller);
        ApplyHabitatTheme();
    }

    public void ApplyEditorPreview(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null) return;
        StyleHud();
        StyleBudgetDisplay(controller);
        StyleSettingsButton(controller);
        StyleBottomBar(controller);
        ApplyHabitatTheme();
    }

    private void StyleHud()
    {
        if (roundText != null) roundText.gameObject.SetActive(false);

        var headerArea = canvasTransform.Find("HeaderArea") as RectTransform;
        if (headerArea != null)
        {
            headerArea.anchoredPosition = new Vector2(0f, -72f);
            headerArea.sizeDelta = new Vector2(0f, 150f);
        }

        // The progress bar is the single goal display. Retain the serialized labels
        // for existing update bindings, but hide their obsolete score cards.
        foreach (var text in new[] { scoreText, targetText })
            if (text != null && text.transform.parent != null)
                text.transform.parent.gameObject.SetActive(false);

        foreach (var button in canvasTransform.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInParent<ShortPowerCardUI>() != null) continue;
            var rect = button.transform as RectTransform;
            if (rect == null || rect.rect.width > 130f || rect.rect.height > 130f) continue;
            if (button.GetComponent<PowerCardFeedback>() == null) button.gameObject.AddComponent<PowerCardFeedback>();
            if (button.targetGraphic != null)
                PrismatchTheme.Shadow(button.targetGraphic, new Color(0f, .04f, .12f, .38f), new Vector2(0f, -3f));
        }
    }

    private void StyleBudgetDisplay(GameController controller)
    {
        if (timerText == null || canvasTransform == null) return;
        budgetController = controller;
        // Reuse the serialized timer card so existing timer/move bindings keep working.
        budgetPanel = timerText.transform.parent as RectTransform;
        if (budgetPanel == null || budgetPanel == canvasTransform) return;
        if (budgetPanel.parent != canvasTransform) budgetPanel.SetParent(canvasTransform, false);
        budgetPanel.anchorMin = budgetPanel.anchorMax = new Vector2(.5f, 1f);
        budgetPanel.pivot = new Vector2(.5f, 1f);
        budgetPanel.anchoredPosition = new Vector2(0f, -48f);
        budgetPanel.sizeDelta = new Vector2(Mathf.Min(510f, canvasTransform.rect.width - 32f), 128f);
        if (budgetPanel.TryGetComponent(out LayoutElement element)) element.ignoreLayout = true;
        budgetBackground = budgetPanel.GetComponent<Image>();
        PrismatchTheme.Round(budgetBackground, Color.Lerp(PrismatchTheme.Navy, PrismatchTheme.Sky, .22f));
        if (budgetBackground != null)
        {
            budgetBackground.raycastTarget = false;
            PrismatchTheme.Shadow(budgetBackground, new Color(0f, .04f, .12f, .55f), new Vector2(0f, -6f));
        }
        var textRect = timerText.rectTransform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 16); textRect.offsetMax = new Vector2(-10, -6);
        timerText.enableAutoSizing = false;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.margin = Vector4.zero;
        timerText.raycastTarget = false;

        if (roundText == null)
            roundText = CreateText("RoundLabel", new Vector2(.5f, 1f), new Vector2(510f, 40f), 28f,
                TextAlignmentOptions.Center, budgetPanel);
        roundText.transform.SetParent(budgetPanel, false);
        roundText.gameObject.SetActive(true);
        var roundRect = roundText.rectTransform;
        roundRect.anchorMin = new Vector2(0f, 1f);
        roundRect.anchorMax = Vector2.one;
        roundRect.pivot = new Vector2(.5f, 0f);
        roundRect.anchoredPosition = new Vector2(0f, 8f);
        roundRect.sizeDelta = new Vector2(0f, 40f);
        roundText.fontSize = 28f;
        roundText.enableAutoSizing = false;
        roundText.fontStyle = FontStyles.Bold;
        roundText.alignment = TextAlignmentOptions.Center;
        roundText.color = PrismatchTheme.Mist;
        roundText.raycastTarget = false;
        UpdateRoundLabel();

        var rail = budgetPanel.Find("BudgetRail") as RectTransform;
        if (rail == null)
        {
            rail = new GameObject("BudgetRail", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rail.SetParent(budgetPanel, false);
            rail.GetComponent<Image>().color = PrismatchTheme.Midnight;
            rail.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(rail, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }
        rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(1, 0);
        rail.pivot = new Vector2(.5f, 0);
        rail.offsetMin = new Vector2(16, 9); rail.offsetMax = new Vector2(-16, 16);
        budgetFill = rail.Find("Fill").GetComponent<Image>();
        budgetFill.sprite = PrismatchTheme.RoundedSprite;
        budgetFill.type = Image.Type.Filled; budgetFill.fillMethod = Image.FillMethod.Horizontal;
        budgetFill.fillOrigin = 0; budgetFill.raycastTarget = false;

        // Reserve a footer below every grid shape, clear of the action buttons.
        var boardArea = canvasTransform.Find("BoardArea") as RectTransform;
        if (boardArea != null)
        {
            boardArea.anchorMin = Vector2.zero; boardArea.anchorMax = Vector2.one;
            boardArea.offsetMin = new Vector2(16, 374);
            boardArea.offsetMax = new Vector2(-16, -320);
        }
        if (!Application.isPlaying)
        {
            var config = GameState.Instance?.CurrentLevel as FreePlayConfig;
            if (config?.Mode == FreePlayMode.LimitedMoves) UpdateMoves(config.Moves);
            else UpdateTimer(config?.Seconds ?? 60);
        }
    }

    private void UpdateBudget(string label, string value, int remaining, int warningAt, int total)
    {
        if (timerText == null) return;
        budgetIsLow = remaining <= warningAt;
        string heading = remaining == 0 ? (label == "TIME LEFT" ? "TIME UP" : "OUT OF MOVES")
            : budgetIsLow ? (label == "TIME LEFT" ? "TIME RUNNING OUT" : "FINAL MOVES") : label;
        timerText.text = $"<size=22>{heading}</size>\n<size=64>{value}</size>";
        timerText.color = budgetIsLow ? PrismatchTheme.Coral : PrismatchTheme.Cream;
        if (budgetFill != null)
        {
            budgetFill.fillAmount = Mathf.Clamp01((float)remaining / Mathf.Max(1, total));
            budgetFill.color = budgetIsLow ? PrismatchTheme.Coral : PrismatchTheme.Sky;
        }
    }

    private void LateUpdate()
    {
        RefreshFlatStats();
        if (budgetPanel == null || timerText == null) return;
        PositionHeaderAboveBoard();
        bool playing = budgetController != null && !budgetController.TargetReached &&
            (budgetController.Phase == GamePhase.Playing || budgetController.Phase == GamePhase.Resolving);
        bool pulse = budgetIsLow && playing && budgetPanel.gameObject.activeInHierarchy;
        budgetPulse = pulse ? budgetPulse + Time.deltaTime : 0f;
        float emphasis = pulse ? (1f - Mathf.Cos(budgetPulse * Mathf.PI * 2f)) * .5f : 0f;
        timerText.rectTransform.localScale = Vector3.one * (1f + emphasis * .045f);
        if (budgetBackground != null)
            budgetBackground.color = Color.Lerp(PrismatchTheme.Navy,
                budgetIsLow ? PrismatchTheme.Coral : PrismatchTheme.Sky, .22f + emphasis * .16f);
    }

    private void PositionHeaderAboveBoard()
    {
        var board = Board.Instance;
        if (canvasTransform == null || board == null || !board.gameObject.activeInHierarchy ||
            board.Tiles == null || board.Tiles.Length == 0) return;
        var topTile = board.Tiles[0, 0];
        if (topTile == null || topTile.background == null) return;
        topTile.background.rectTransform.GetWorldCorners(headerBoardCorners);
        float boardTop = canvasTransform.InverseTransformPoint(headerBoardCorners[1]).y;
        float gap = canvasTransform.rect.yMax - boardTop;
        if (gap <= 0f) return;

        bool hasStrategy=strategyPanel!=null&&strategyPanel.gameObject.activeSelf;
        float timerTop;
        if(hasStrategy) {
            LayoutStrategyPanel();ReserveStrategySpace(true);
            float statusHeight=strategyPanel.rect.height;
            float scoreHeight=scoreProgressRoot!=null?((RectTransform)scoreProgressRoot.transform).rect.height:0;
            float stackHeight=budgetPanel.rect.height+scoreHeight+statusHeight+48;
            timerTop=Mathf.Max(24,gap-stackHeight);
            strategyPanel.anchoredPosition=new Vector2(0,-(timerTop+budgetPanel.rect.height+scoreHeight+32));
        } else timerTop = gap * .5f - budgetPanel.rect.height * .5f;
        budgetPanel.anchoredPosition = new Vector2(0f, -timerTop);
        if (scoreProgressRoot != null)
        {
            var progress = (RectTransform)scoreProgressRoot.transform;
            progress.anchoredPosition = new Vector2(0f, -(timerTop + budgetPanel.rect.height + 16f));
        }
    }

    private void BuildScoreProgress()
    {
        var root = new GameObject("ScoreProgress", typeof(RectTransform), typeof(Image));
        scoreProgressRoot = root;
        root.transform.SetParent(canvasTransform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -205f);
        rect.sizeDelta = new Vector2(570f, 60f);
        var background = root.GetComponent<Image>();
        PrismatchTheme.Round(background, new Color(PrismatchTheme.Midnight.r, PrismatchTheme.Midnight.g, PrismatchTheme.Midnight.b, .92f));
        PrismatchTheme.Shadow(background, new Color(0f, .04f, .12f, .4f), new Vector2(0f, -3f));

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(root.transform, false);
        var fillRect = (RectTransform)fillObject.transform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(5f, 5f);
        fillRect.offsetMax = new Vector2(-5f, -5f);
        scoreProgressFill = fillObject.GetComponent<Image>();
        scoreProgressFill.sprite = PrismatchTheme.RoundedSprite;
        scoreProgressFill.type = Image.Type.Filled;
        scoreProgressFill.fillMethod = Image.FillMethod.Horizontal;
        scoreProgressFill.fillOrigin = 0;
        scoreProgressFill.color = PrismatchTheme.Green;
        scoreProgressFill.raycastTarget = false;

        scoreProgressText = CreateText("ProgressLabel", new Vector2(.5f, .5f), Vector2.zero, 24f,
            TextAlignmentOptions.Center, root.transform);
        var labelRect = (RectTransform)scoreProgressText.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        scoreProgressText.fontStyle = FontStyles.Bold;
        scoreProgressText.color = PrismatchTheme.Cream;
        scoreProgressText.richText = true;
        UpdateScoreProgress(previousScore);
    }

    private GoalType goalType;
    private int goalProgress;
    private string blockObjectiveLabel = "BLOCKS MATCHED";
    public void UpdateBlockObjective(string label) => blockObjectiveLabel = label;
    public void UpdateGoalProgress(int progress, GoalType type)
    {
        goalType = type; goalProgress = progress;
        UpdateScoreProgress(progress);
        if (targetText != null)
            targetText.text = $"<size=60%><color=#AFC7E8>{(type == GoalType.Score ? "POINTS TARGET" : blockObjectiveLabel)}</color></size>\n<size=100%>{progress:N0} / {latestTarget:N0}</size>";
    }
    public void UpdateMoves(int moves)
    {
        moves = Mathf.Max(0, moves);
        int total = (GameState.Instance?.CurrentLevel as ILevelSettings)?.Moves ?? Mathf.Max(1, moves);
        UpdateBudget("MOVES LEFT", moves.ToString(), moves, 3, total);
    }
    private void UpdateScoreProgress(int score)
    {
        float progress = latestTarget > 0 ? Mathf.Clamp01((float)score / latestTarget) : 0f;
        if (scoreProgressFill != null && !Mathf.Approximately(displayedProgressTarget, progress))
        {
            displayedProgressTarget = progress;
            // Retarget from the currently visible fill if another cascade arrives.
            scoreProgressTween?.Kill();
            scoreProgressTween = null;
            if (!Application.isPlaying || !scoreProgressFill.gameObject.activeInHierarchy || progress <= 0f)
                scoreProgressFill.fillAmount = progress;
            else
                scoreProgressTween = scoreProgressFill.DOFillAmount(progress, ProgressFillDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true)
                    .SetLink(scoreProgressFill.gameObject, LinkBehaviour.KillOnDestroy).Play();
        }
        if (scoreProgressText != null)
        {
            int remaining = Mathf.Max(0, latestTarget - score);
            scoreProgressText.text = remaining > 0
                ? $"<b>{progress * 100f:0}%</b>  •  {remaining:N0} {(goalType == GoalType.Score ? "POINTS" : "BLOCKS")} TO TARGET"
                : "<b>TARGET REACHED!</b>";
        }
    }

    private void OnDestroy()
    {
        scoreProgressTween?.Kill();
        rewardPulse?.Kill();
        levelLesson?.Stop();
    }

    private void BuildBoardStats()
    {
        if (canvasTransform == null) return;
        var row = new GameObject("BoardScoreStats", typeof(RectTransform));
        boardScoreStats = row;
        row.transform.SetParent(canvasTransform, false);
        var rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, 0f);
        rowRect.pivot = new Vector2(.5f, .5f);
        rowRect.anchoredPosition = new Vector2(0f, FooterStatsY);
        rowRect.sizeDelta = new Vector2(640f, 128f);
        basePointsText = CreateStatChip("BasePoints", rowRect);
        multiplierText = CreateStatChip("Multiplier", rowRect);
        var chips = new[] { basePointsText, multiplierText };
        for (int i = 0; i < chips.Length; i++)
        {
            var chip = (RectTransform)chips[i].transform.parent;
            chip.anchorMin = chip.anchorMax = new Vector2(.5f, .5f);
            chip.anchoredPosition = new Vector2(i == 0 ? -234f : 234f, 0f);
            chip.sizeDelta = new Vector2(148f, 100f);
            bool multiplier = i == 1;
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = chip.GetComponent<Image>();
            button.onClick.AddListener(() => ShowStatDetails(multiplier));
        }
        UpdateScoreStats(0, 1);
    }

    private TextMeshProUGUI CreateStatChip(string name, Transform parent)
    {
        var chip = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        chip.transform.SetParent(parent, false);
        var image = chip.GetComponent<Image>();
        PrismatchTheme.Round(image, new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .94f));
        PrismatchTheme.Shadow(image, new Color(0f, .04f, .12f, .35f), new Vector2(0f, -3f));
        var element = chip.GetComponent<LayoutElement>();
        element.preferredWidth = 194f;
        element.preferredHeight = 68f;

        var value = CreateText("Value", new Vector2(.5f, .5f), Vector2.zero, 20f,
            TextAlignmentOptions.Center, chip.transform);
        var rect = (RectTransform)value.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 4f);
        rect.offsetMax = new Vector2(-6f, -4f);
        value.color = PrismatchTheme.Cream;
        value.fontStyle = FontStyles.Bold;
        value.richText = true;
        return value;
    }

    private void UpdateScoreStats(float basePoints, float multiplier)
    {
        if (basePointsText != null)
            basePointsText.text = $"<size=20><color=#AFC7E8>BASE / TILE</color></size>\n<size=27>{basePoints:0.##}</size>";
        if (multiplierText != null)
            multiplierText.text = $"<size=15><color=#AFC7E8>MULTIPLIER</color></size>\n<size=27>\u00D7{Mathf.Max(0, multiplier):0.##}</size>";
    }


    private void RefreshFlatStats()
    {
        if (budgetController?.PowerManager == null) return;
        float points = budgetController.LevelBasePointsPerTile, additive = 1, factor = 1;
        foreach (var power in budgetController.PowerManager.ActivePowers())
        {
            points += power.FlatBasePerTile;
            additive += power.FlatMultiplierBonus;
            factor *= power.FlatMultiplierFactor;
        }
        UpdateScoreStats(Mathf.Max(0, points), Mathf.Max(0, additive * factor));
    }

    public void HideStatDetails()
    {
        if (statDetailsPanel == null) return;
        statDetailsPanel.SetActive(false);
        Destroy(statDetailsPanel);
        statDetailsPanel = null;
    }

    private void ShowStatDetails(bool multiplier)
    {
        if (budgetController?.PowerManager == null) return;
        HideStatDetails();
        budgetController.SetStatsOpen(true);
        statDetailsPanel = new GameObject("StatDetails", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        statDetailsPanel.transform.SetParent(canvasTransform, false);
        var overlay = (RectTransform)statDetailsPanel.transform;
        StretchIntroRect(overlay, Vector2.zero, Vector2.one);
        var modalCanvas = statDetailsPanel.GetComponent<Canvas>();
        modalCanvas.overrideSorting = true; modalCanvas.sortingOrder = 80;
        statDetailsPanel.GetComponent<Image>().color = new Color(.02f, .04f, .09f, .9f);
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay, false);
        var rect = (RectTransform)card.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = new Vector2(canvasTransform.rect.width * .98f, canvasTransform.rect.height * .84f);
        PrismatchTheme.Round(card.GetComponent<Image>(), PrismatchTheme.Navy);
        var title = CreateText("Title", Vector2.zero, Vector2.zero, 34, TextAlignmentOptions.Center, rect);
        StretchIntroRect(title.rectTransform, new Vector2(.04f,.88f), new Vector2(.96f,.99f));
        title.text = multiplier ? "YOUR MULTIPLIER" : "BASE POINTS PER TILE";
        var view = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        view.transform.SetParent(rect, false);
        var viewRect = (RectTransform)view.transform;
        StretchIntroRect(viewRect, new Vector2(.05f,.16f), new Vector2(.95f,.86f));
        view.GetComponent<Image>().color = new Color(0,0,0,.01f);
        var content = new GameObject("PowerRows", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewRect, false);
        content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
        var stats = FlatScoreStats.Read(budgetController.PowerManager.ActivePowers(), levelBasePointsPerTile: budgetController.LevelBasePointsPerTile);
        float y = 0;
        AddStatRow(content, ref y, null, "ALWAYS ACTIVE", multiplier ? "Your flat multiplier" : "Points for every tile",
            multiplier ? $"x{stats.Multiplier:0.##}" : $"{stats.BasePerTile:0.##}", true);
        foreach (var power in budgetController.PowerManager.ActivePowers())
        {
            if (!power.AffectsStat(multiplier)) continue;
            float flat = multiplier ? power.FlatMultiplierBonus : power.FlatBasePerTile;
            float factor = multiplier ? power.FlatMultiplierFactor : 1;
            if (flat != 0 || factor != 1)
                AddStatRow(content, ref y, power, power.Name, "Always active",
                    factor != 1 ? $"x{factor:0.##}" : $"+{flat:0.##}");
        }
        if (multiplier && budgetController.QueuedMultiplier > 1)
            AddStatRow(content, ref y, null, "Ready to use", "Next match only", $"x{budgetController.QueuedMultiplier}");
        foreach (var power in budgetController.PowerManager.ActivePowers())
        {
            string condition = power.ConditionForStat(multiplier);
            if (string.IsNullOrEmpty(condition)) continue;
            if (power is TacticalStarter tactical)
            {
                if (!multiplier) continue; // Its base contribution is entirely flat.
                var hint = (condition: tactical.TriggerText + " - next reward", bonus: tactical.RewardText);
                AddStatRow(content, ref y, power, power.Name, hint.condition, hint.bonus);
                continue;
            }
            AddStatRow(content, ref y, power, power.Name, condition, "IF");
        }
        foreach (var boss in budgetController.PowerManager.ActiveBossPowers())
            if (boss.AffectsStat(multiplier)) AddStatRow(content, ref y, boss, boss.Name, boss.ConditionForStat(multiplier), "BOSS");
        content.sizeDelta = new Vector2(0, y);
        var scroll = view.GetComponent<ScrollRect>();
        scroll.viewport = viewRect; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
        var close = CreateButton("Back to game", new Vector2(.5f,.07f), rect, () => {
            HideStatDetails(); budgetController.SetStatsOpen(false);
        });
        StretchIntroRect((RectTransform)close.transform, new Vector2(.08f,.025f), new Vector2(.92f,.13f));
        PrismatchTheme.Round(close.GetComponent<Image>(), PrismatchTheme.RaisedNavy);
        StretchIntroRect(close.GetComponentInChildren<TextMeshProUGUI>().rectTransform, Vector2.zero, Vector2.one);
    }

    private void AddStatRow(RectTransform content, ref float y, PowerUp power, string name, string condition, string value, bool total = false)
    {
        var row = new GameObject("StatRow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        row.SetParent(content, false);
        row.anchorMin = new Vector2(0,1); row.anchorMax = Vector2.one; row.pivot = new Vector2(.5f,1);
        row.anchoredPosition = new Vector2(0,-y);
        PrismatchTheme.Round(row.GetComponent<Image>(), total ? PrismatchTheme.RaisedNavy : PrismatchTheme.Midnight);
        row.GetComponent<Image>().raycastTarget = false;
        if (power != null)
        {
            var icon = new GameObject("PowerIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(row, false);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0,.5f);
            icon.rectTransform.pivot = new Vector2(0,.5f);
            icon.rectTransform.anchoredPosition = new Vector2(12,0);
            icon.rectTransform.sizeDelta = new Vector2(70,70);
            PowerUpIconLibrary.Apply(icon, power);
            icon.raycastTarget = false;
        }
        var label = CreateText("Power", Vector2.zero, Vector2.zero, 24, TextAlignmentOptions.TopLeft, row);
        label.margin = Vector4.zero; label.raycastTarget = false;
        label.text = $"<b>{name}</b>\n<size=22><color=#AFC7E8>{condition}</color></size>";
        float left = power == null ? 18 : 96;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(left,12); label.rectTransform.offsetMax = new Vector2(-115,-12);
        float right = string.IsNullOrEmpty(value) ? 18 : 115;
        label.rectTransform.offsetMax = new Vector2(-right,-12);
        float width = Mathf.Min(850, canvasTransform.rect.width * .92f) * .9f - left - right;
        float height = Mathf.Max(112, label.GetPreferredValues(label.text, Mathf.Max(100,width), 0).y + 28);
        row.sizeDelta = new Vector2(0,height);
        var badge = CreateText("Bonus", new Vector2(1,.5f), new Vector2(104,64), total ? 38 : 32, TextAlignmentOptions.Center, row);
        badge.rectTransform.anchoredPosition = new Vector2(-56,0);
        badge.color = PrismatchTheme.Green; badge.fontStyle = FontStyles.Bold; badge.text = value;
        badge.raycastTarget = false;
        y += height + 12;
    }

    public void ShowBoardStats(bool shown)
    {
        boardStatsVisible=shown;
        if(strategyPanel!=null)strategyPanel.gameObject.SetActive(shown);
        if(!shown)ClearTriggeredIcons();
        if(!shown)ClearBoardMessage();
        if(animalBonusPanel!=null)animalBonusPanel.gameObject.SetActive(shown&&animalBonusActive);
        if (boardScoreStats != null) boardScoreStats.SetActive(shown);
        if (budgetPanel != null) budgetPanel.gameObject.SetActive(shown);
        if (scoreProgressRoot != null) scoreProgressRoot.SetActive(shown);
    }

    private void StyleBottomBar(GameController controller)
    {
        var bar = canvasTransform.Find("BottomBar") as RectTransform;
        if (bar == null) return;
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 0f);
        bar.pivot = new Vector2(.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 50f);
        bar.sizeDelta = new Vector2(620f, 104f);

        if (bar.TryGetComponent(out HorizontalLayoutGroup layout))
        {
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        var restart = bar.Find("RestartButton") ?? bar.Find("ShuffleBoardButton");
        if (restart != null)
        {
            shuffleButtonText = StyleBottomAction(restart, 206f, "Shuffle", PowerSymbolLibrary.ResourceFor("shuffle"), PrismatchTheme.Orange);
            restartButtonBackground = restart.GetComponent<Image>();
        }
        SetRestartConfirmation(false);

        var powers = bar.Find("Powerups");
        if (powers != null)
            powersButtonText = StyleBottomAction(powers, 190f, "Powers", "UI/Generated/nav-powers", PrismatchTheme.Sky);

        var boss = bar.Find("Boss Icon");
        if (boss != null)
            bossButtonText = StyleBottomAction(boss, 200f, "Boss Rules", "UI/Generated/nav-boss-rules", PrismatchTheme.Coral);

        RefreshBottomBar(controller);
    }

    private void StyleSettingsButton(GameController controller)
    {
        var action = canvasTransform.Find("HeaderArea/Settings") ?? canvasTransform.Find("HeaderArea/New game");
        if (action == null) return;
        action.name = "Settings";
        var rect = (RectTransform)action;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(8f, -28f);
        rect.sizeDelta = new Vector2(96f, 96f);

        var background = action.GetComponent<Image>();
        if (background != null)
        {
            PrismatchTheme.Round(background, PrismatchTheme.Navy);
            PrismatchTheme.Shadow(background, new Color(0f, .04f, .12f, .42f), new Vector2(0f, -4f));
        }
        if (action.GetComponent<PowerCardFeedback>() == null) action.gameObject.AddComponent<PowerCardFeedback>();
        var oldButton = action.GetComponent<Button>();
        if (oldButton != null) oldButton.enabled = false;
        var relay = action.GetComponent<SettingsButtonRelay>() ?? action.gameObject.AddComponent<SettingsButtonRelay>();
        relay.Configure(controller);

        var existingIcon = action.Find("SettingsIcon");
        var iconObject = existingIcon != null
            ? existingIcon.gameObject
            : new GameObject("SettingsIcon", typeof(RectTransform), typeof(Image));
        if (existingIcon == null) iconObject.transform.SetParent(action, false);
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = new Vector2(.14f, .14f);
        iconRect.anchorMax = new Vector2(.86f, .86f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        var icon = iconObject.GetComponent<Image>() ?? iconObject.AddComponent<Image>();
        icon.sprite = Resources.Load<Sprite>("UI/Generated/nav-settings");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private TextMeshProUGUI StyleBottomAction(Transform action, float width, string label,
        string iconResource, Color accent)
    {
        var rect = (RectTransform)action;
        rect.sizeDelta = new Vector2(width, 96f);
        var layoutElement = action.GetComponent<LayoutElement>() ?? action.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = 68f;

        var background = action.GetComponent<Image>() ?? action.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(background, new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .96f));
        PrismatchTheme.Shadow(background, new Color(0f, .04f, .12f, .42f), new Vector2(0f, -4f));
        if (action.TryGetComponent(out Button button)) button.targetGraphic = background;
        if (action.GetComponent<PowerCardFeedback>() == null) action.gameObject.AddComponent<PowerCardFeedback>();

        var icon = action.GetComponentsInChildren<Image>(true).FirstOrDefault(candidate => candidate.transform != action);
        if (icon != null)
        {
            if (!string.IsNullOrEmpty(iconResource)) icon.sprite = Resources.Load<Sprite>(iconResource);
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;
            var iconRect = (RectTransform)icon.transform;
            if (label == null)
            {
                iconRect.anchorMin = new Vector2(.14f, .14f);
                iconRect.anchorMax = new Vector2(.86f, .86f);
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            }
            else
            {
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
                iconRect.pivot = new Vector2(0f, .5f);
                iconRect.anchoredPosition = new Vector2(12f, 0f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
            }
        }

        if (label == null)
        {
            var staleLabel = action.Find("ActionLabel");
            if (staleLabel != null)
            {
                staleLabel.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(staleLabel.gameObject);
                else DestroyImmediate(staleLabel.gameObject);
            }
            return null;
        }
        var existingLabel = action.Find("ActionLabel")?.GetComponent<TextMeshProUGUI>();
        var text = existingLabel != null
            ? existingLabel
            : CreateText("ActionLabel", new Vector2(.5f, .5f), Vector2.zero, 28f,
                TextAlignmentOptions.MidlineLeft, action);
        text.gameObject.SetActive(true);
        text.fontSize = 28f;
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(70f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);
        text.color = PrismatchTheme.Cream;
        text.fontStyle = FontStyles.Bold;
        text.richText = true;
        text.raycastTarget = false;
        text.text = label;
        return text;
    }

    public void RefreshBottomBar(GameController controller)
    {
        if (controller == null) return;
        UpdateAnimalBonus(controller,Board.Instance);
        UpdateShuffleCost(controller);
        int powers = controller.PowerManager?.ActivePowers().Count ?? 0;
        int bosses = controller.GetTotalBossPowersInCurrentLevel();
        if (powersButtonText != null)
            powersButtonText.text = $"Powers  <color=#58A6F6>{powers}</color>";
        if (bossButtonText != null)
        {
            bossButtonText.text = $"Boss Rules  <color=#FF6B6B>{bosses}</color>";
            var popularity = controller.PowerManager?.ActiveBossPowers().OfType<ChallengeBoss>()
                .FirstOrDefault(boss => boss.Style == BossStyle.MostMatched);
            if (popularity != null)
                bossButtonText.text = popularity.BlockedAnimal.HasValue
                    ? $"{popularity.BlockedAnimal.Value}\n<color=#FF6B6B>0 points</color>" : "Boss target\nNo matches yet";
        }
    }

    public void SetRestartConfirmation(bool armed)
    {
        if (restartButtonBackground == null) return;
        PrismatchTheme.Round(restartButtonBackground, armed
            ? Color.Lerp(PrismatchTheme.Navy, PrismatchTheme.Coral, .55f)
            : new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .96f));
    }

    private void BuildSettingsPanel(GameController controller)
    {
        settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        settingsPanel.transform.SetParent(canvasTransform, false);
        var rect = (RectTransform)settingsPanel.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(560f, 852f);
        var image = settingsPanel.GetComponent<Image>();
        PrismatchTheme.Round(image, new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .99f));
        PrismatchTheme.Shadow(image, new Color(0f, .02f, .08f, .65f), new Vector2(0f, -8f));

        var title = CreateText("Title", new Vector2(.5f, .91f), new Vector2(450f, 70f), 38f,
            TextAlignmentOptions.Center, rect);
        title.text = "Settings";
        title.color = PrismatchTheme.Cream;
        title.fontStyle = FontStyles.Bold;

        soundButtonText = CreateSettingsButton("Sound", new Vector2(0f, 260f), rect, controller.ToggleSound);
        UpdateSoundSetting(true);
        swipeToMoveButtonText = CreateSettingsButton("SwipeToMove", new Vector2(0f, 160f), rect, controller.ToggleSwipeToMove);
        UpdateSwipeToMoveSetting(GameController.SwipeToMoveEnabled);
        animationSpeedButtonText = CreateSettingsButton("AnimationSpeed", new Vector2(0f, 60f), rect, controller.CycleAnimationSpeed);
        UpdateAnimationSpeedSetting();
        CreateSettingsButton("Analytics", new Vector2(0f, -40f), rect, () => ShowAnalytics(controller)).text = "Analytics / Power Testing";
        CreateSettingsButton("LevelSelect", new Vector2(0f, -140f), rect,
            () => SceneManager.LoadScene("Level Select"))
            .text = "Back to Level Select";
        CreateSettingsButton("ResetProgress",new Vector2(0f,-240f),rect,()=>ShowResetConfirmation(controller)).text="Reset progress, notes & scores";
        CreateSettingsButton("Resume", new Vector2(0f, -340f), rect, controller.ToggleSettings)
            .text = "Resume Game";
        settingsPanel.SetActive(false);
    }

    private void ShowResetConfirmation(GameController controller)
    {
        if(resetConfirmation!=null)return;
        resetConfirmation=new GameObject("Confirm reset",typeof(RectTransform),typeof(Image));
        var rect=(RectTransform)resetConfirmation.transform;rect.SetParent(settingsPanel.transform,false);
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        PrismatchTheme.Round(resetConfirmation.GetComponent<Image>(),PrismatchTheme.Navy);
        var title=CreateText("Reset title",new Vector2(.5f,.5f),new Vector2(480,70),36,TextAlignmentOptions.Center,rect);
        title.rectTransform.anchoredPosition=new Vector2(0,270);title.text="Reset saved data?";title.color=PrismatchTheme.Coral;
        var explanation=CreateText("Reset explanation",new Vector2(.5f,.5f),new Vector2(470,300),28,TextAlignmentOptions.Center,rect);
        explanation.rectTransform.anchoredPosition=new Vector2(0,55);
        explanation.text="Deletes progress and best scores, all level notes, and saved target/growth changes on this device.\n\nEnds this run and returns to level select. This cannot be undone.\n\nSound and control settings are kept.";
        explanation.color=PrismatchTheme.Cream;
        CreateSettingsButton("ConfirmReset",new Vector2(0,-180),rect,()=>{
            controller.ResetGame();ProgressStore.ResetLocalPlayerData();
            GameState.Instance.CurrentLevel=LevelLibrary.GetLevel(1).CloneNew();
            SceneManager.LoadScene("Level Select");
        }).text="Reset all saved progress";
        CreateSettingsButton("CancelReset",new Vector2(0,-290),rect,()=>{
            Destroy(resetConfirmation);resetConfirmation=null;
        }).text="Cancel";
    }

    private TextMeshProUGUI CreateSettingsButton(string name, Vector2 position, Transform parent,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(440f, 96f);
        var image = go.GetComponent<Image>();
        PrismatchTheme.Round(image, PrismatchTheme.RaisedNavy);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        go.AddComponent<PowerCardFeedback>();
        var label = CreateText("Label", new Vector2(.5f, .5f), new Vector2(420f, 88f), 28f,
            TextAlignmentOptions.Center, rect);
        label.color = PrismatchTheme.Cream;
        label.fontStyle = FontStyles.Bold;
        return label;
    }

    public void UpdateAnimationSpeedSetting()
    {
        if (animationSpeedButtonText != null)
            animationSpeedButtonText.text = $"Animation speed   {GameController.AnimationSpeed:0.#}?";
    }

    public void ShowSettings(bool shown, GameController controller)
    {
        if (settingsPanel == null) return;
        if(!shown&&resetConfirmation!=null){Destroy(resetConfirmation);resetConfirmation=null;}
        settingsPanel.SetActive(shown);
        if (shown) settingsPanel.transform.SetAsLastSibling();
    }

    public void ShowAnalytics(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null) return;
        if (analyticsPanel != null) { analyticsPanel.SetActive(false); Destroy(analyticsPanel); }
        controller.SetStatsOpen(true);
        analyticsPanel = new GameObject("AnalyticsPanel", typeof(RectTransform), typeof(Image));
        analyticsPanel.transform.SetParent(canvasTransform, false);
        var root = (RectTransform)analyticsPanel.transform;
        StretchIntroRect(root, Vector2.zero, Vector2.one);
        analyticsPanel.GetComponent<Image>().color = new Color(.02f,.04f,.09f,.98f);
        var card = new GameObject("AnalyticsCard", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        card.SetParent(root, false); StretchIntroRect(card, new Vector2(.04f,.05f), new Vector2(.96f,.95f));
        PrismatchTheme.Round(card.GetComponent<Image>(), PrismatchTheme.Navy);
        var title = CreateEndText("Title", card, new Vector2(.04f,.90f), new Vector2(.96f,.98f), 34, PrismatchTheme.Cream);
        title.text = "ANALYTICS / POWER TESTING";
        var status = CreateEndText("Status", card, new Vector2(.05f,.81f), new Vector2(.95f,.90f), 20, PrismatchTheme.Mist);
        status.text = "Local history / a play is one swap attempt.\nAverages include failed swaps and resulting cascades.";
        var cloudStatus = CreateEndText("CloudStatus", card, new Vector2(.05f,.705f), new Vector2(.95f,.81f), 18, PrismatchTheme.Mist);
        cloudStatus.text = UnityCloudAnalytics.Status + "\nOptional: send gameplay metrics and device/session identifiers to Unity.\nLocal exports work with cloud OFF.";
        var viewport = new GameObject("AnalyticsScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(card, false);
        var view = (RectTransform)viewport.transform; StretchIntroRect(view, new Vector2(.04f,.23f), new Vector2(.96f,.64f));
        viewport.GetComponent<Image>().color = new Color(0,0,0,.01f);
        var content = new GameObject("AnalyticsRows", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(view, false); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1);
        content.sizeDelta = Vector2.zero;
        float y = 0;
        var data = controller.AnalyticsSnapshot();
        if (data == null || data.rounds.Count == 0) AddStatRow(content, ref y, null, "No plays recorded yet", "Play a round to begin collecting data.", "");
        else
        {
            foreach (var round in data.rounds)
                AddStatRow(content, ref y, null, $"ROUND {round.round} / ATTEMPT {round.attempt} / {round.status.ToUpperInvariant()}",
                    $"{round.plays.Count} plays / {round.SuccessfulPlays} successful\n{round.PointsPerPlay:0.##} points/play / {round.CreditPerPlay:0.##} goal credit/play\n" +
                    RunAnalytics.BuildOrder(data, round), "");
            AddStatRow(content, ref y, null, "POWER PICK ORDER (INCLUDING RETRIES)",
                data.picks.Count == 0 ? "No powers selected yet." : string.Join("\n", data.picks.Select(p => $"#{p.order} / R{p.round}.{p.attempt} / {p.name}")), "");
        }
        content.sizeDelta = new Vector2(0,y);
        var scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = view; scroll.content = content;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
        var export = CreateButton("Export All Runs", new Vector2(.5f,.10f), card, () => status.text = controller.ExportAnalytics());
        StretchIntroRect((RectTransform)export.transform, new Vector2(.05f,.035f), new Vector2(.61f,.145f));
        var close = CreateButton("Close", new Vector2(.8f,.10f), card, () => { analyticsPanel.SetActive(false); controller.SetStatsOpen(false); });
        StretchIntroRect((RectTransform)close.transform, new Vector2(.65f,.035f), new Vector2(.95f,.145f));
        Button cloudToggle = null;
        cloudToggle = CreateButton(UnityCloudAnalytics.ConsentGranted ? "Cloud: Turn Off" : "Cloud: Allow Uploads", new Vector2(.3f,.19f), card, async () =>
        {
            cloudToggle.interactable = false;
            var result = await UnityCloudAnalytics.SetConsent(!UnityCloudAnalytics.ConsentGranted);
            if (cloudStatus != null) cloudStatus.text = result;
            if (cloudToggle != null) { cloudToggle.interactable = true; cloudToggle.GetComponentInChildren<TextMeshProUGUI>().text = UnityCloudAnalytics.ConsentGranted ? "Cloud: Turn Off" : "Cloud: Allow Uploads"; }
        });
        StretchIntroRect((RectTransform)cloudToggle.transform, new Vector2(.05f,.155f), new Vector2(.46f,.22f));
        bool deleteArmed = false;
        Button deleteCloud = null;
        deleteCloud = CreateButton("Delete Cloud Data", new Vector2(.7f,.19f), card, async () =>
        {
            if (!deleteArmed) { deleteArmed = true; deleteCloud.GetComponentInChildren<TextMeshProUGUI>().text = "Confirm Cloud Delete"; return; }
            deleteCloud.interactable = false;
            var result = await UnityCloudAnalytics.DeleteCloudData();
            if (cloudStatus != null) cloudStatus.text = result;
            if (deleteCloud != null) { deleteArmed = false; deleteCloud.interactable = true; deleteCloud.GetComponentInChildren<TextMeshProUGUI>().text = "Delete Cloud Data"; }
            if (cloudToggle != null) cloudToggle.GetComponentInChildren<TextMeshProUGUI>().text = "Cloud: Allow Uploads";
        });
        StretchIntroRect((RectTransform)deleteCloud.transform, new Vector2(.5f,.155f), new Vector2(.95f,.22f));
        var privacy = CreateButton("Unity Privacy Policy", new Vector2(.5f,.83f), card,
            () => Application.OpenURL("https://unity.com/legal/game-player-and-app-user-privacy-policy"));
        StretchIntroRect((RectTransform)privacy.transform, new Vector2(.18f,.65f), new Vector2(.82f,.70f));
        foreach (var button in new[] { export, close, cloudToggle, deleteCloud, privacy })
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(); StretchIntroRect(label.rectTransform, Vector2.zero, Vector2.one);
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 26;
        }
        analyticsPanel.transform.SetAsLastSibling();
    }

    public void UpdateShuffleCost(GameController controller)
    {
        if (shuffleButtonText != null && controller != null)
            shuffleButtonText.text = $"Shuffle\n<size=20>{controller.ShuffleCostLabel}</size>";
    }

    public void UpdateSoundSetting(bool enabled)
    {
        if (soundButtonText != null)
            soundButtonText.text = $"Sound Effects   <color=#{(enabled ? "42CC72" : "FF6B6B")}>{(enabled ? "ON" : "OFF")}</color>";
    }

    public void UpdateSwipeToMoveSetting(bool enabled)
    {
        if (swipeToMoveButtonText != null)
            swipeToMoveButtonText.text = $"Swipe to move   <color=#{(enabled ? "42CC72" : "FF6B6B")}>{(enabled ? "ON" : "OFF")}</color>";
    }

    private GameObject levelIntroPanel;
    private LevelLessonAnimation levelLesson;

    public void HideLevelIntro()
    {
        levelLesson?.Stop(); levelLesson = null;
        if (levelIntroPanel == null) return;
        levelIntroPanel.SetActive(false);
        if (Application.isPlaying) Destroy(levelIntroPanel);
        else DestroyImmediate(levelIntroPanel);
        levelIntroPanel = null;
    }

    public bool ShowLevelIntro(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null) return false;
        HideLevelIntro();
        ClearBoardMessage();
        levelIntroPanel = new GameObject("LevelIntro", typeof(RectTransform), typeof(Image));
        levelIntroPanel.transform.SetParent(canvasTransform, false);
        var overlay = (RectTransform)levelIntroPanel.transform;
        StretchIntroRect(overlay, Vector2.zero, Vector2.one);
        levelIntroPanel.GetComponent<Image>().color = new Color(.02f, .04f, .09f, .94f);
        var card = new GameObject("LevelBriefing", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay, false);
        var rect = (RectTransform)card.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = new Vector2(canvasTransform.rect.width * .98f, canvasTransform.rect.height * .84f);
        PrismatchTheme.Round(card.GetComponent<Image>(), PrismatchTheme.Navy);
        var level = GameState.Instance.CurrentLevel;
        bool bossBriefing = controller.isBossLevel && controller.CurrentRound > 1;
        var accent = bossBriefing ? PrismatchTheme.Coral : PrismatchTheme.Orange;
        PrismatchTheme.Shadow(card.GetComponent<Image>(), new Color(0,0,0,.35f), new Vector2(0,-8));
        var stripe = new GameObject("Briefing accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        stripe.transform.SetParent(rect, false);
        StretchIntroRect(stripe.rectTransform, new Vector2(.40f,.97f), new Vector2(.60f,.977f));
        PrismatchTheme.Round(stripe, accent); stripe.raycastTarget = false;
        var eyebrow = CreateEndText("Briefing context", rect, new Vector2(.06f,.91f), new Vector2(.94f,.955f), 20, accent);
        eyebrow.fontStyle = FontStyles.Bold;
        eyebrow.text = bossBriefing ? "FINAL BOSS" : level is FreePlayConfig ? "FREE PLAY" : "LEVEL " + level.GetLevelNumber + (level is CampaignLevel difficultyLevel ? " / DIFFICULTY " + difficultyLevel.Difficulty : "");
        var title = CreateText("LevelName", Vector2.zero, Vector2.zero, 40f, TextAlignmentOptions.Center, rect);
        StretchIntroRect(title.rectTransform, new Vector2(.06f, .825f), new Vector2(.94f, .91f));
        title.fontStyle = FontStyles.Bold;
        title.color = PrismatchTheme.Cream;
        title.enableAutoSizing = true; title.fontSizeMin = 24; title.fontSizeMax = 40;
        title.text = bossBriefing ? string.Join(" & ", controller.PowerManager.ActiveBossPowers().Select(b => b.Name)) : level is FreePlayConfig ? "Free Play"
            : level is CampaignLevel campaign ? campaign.Name : "LEVEL " + level.GetLevelNumber;
        var subtitle = CreateEndText("Briefing subtitle", rect, new Vector2(.06f,.77f), new Vector2(.94f,.82f), 22, PrismatchTheme.Mist);
        subtitle.text = bossBriefing ? "" : controller.CurrentTarget.ToString("N0") +
            (controller.CurrentGoal == GoalType.BlocksCleared ? " blocks" : " points") + "  /  " +
            (controller.IsTimed ? level.GetTotalTime + " seconds" : controller.MovesRemaining + " moves");

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, bossBriefing
            ? Mathf.Min(720f, canvasTransform.rect.height * .72f)
            : canvasTransform.rect.height * .94f);

        if (!bossBriefing)
        {
            BuildLevelLesson(rect, level, controller);
            overlay.SetAsLastSibling();
            return true;
        }

        var viewport = new GameObject("BriefingScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(rect, false);
        var viewRect = (RectTransform)viewport.transform;
        StretchIntroRect(viewRect, new Vector2(bossBriefing?.06f:.02f, .20f), new Vector2(bossBriefing?.94f:.98f, bossBriefing ? .79f : .76f));
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, .01f);
        var bodyRect = new GameObject("VisualBriefing", typeof(RectTransform)).GetComponent<RectTransform>();
        bodyRect.SetParent(viewRect, false);
        bodyRect.anchorMin = new Vector2(0,1); bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(.5f,1); bodyRect.sizeDelta = Vector2.zero;
        float y = 0;
        var activeBosses = controller.PowerManager.ActiveBossPowers().ToList();
        foreach (var boss in activeBosses)
            if (bossBriefing) AddBossBriefingCard(bodyRect, ref y, boss);
            else AddStatRow(bodyRect, ref y, boss, boss.Name, boss is ChallengeBoss challenge ? challenge.BriefRule : boss.GetCurrentPowerDescription(), "");
        if (y == 0)
            AddStatRow(bodyRect, ref y, null, "READY TO PLAY", "No extra level rules.", "");
        foreach (Transform child in bodyRect)
        {
            if (child.name != "StatRow") continue;
            PrismatchTheme.Round(child.GetComponent<Image>(), PrismatchTheme.Card);
            foreach (var text in child.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = PrismatchTheme.Navy;
                text.text = text.text.Replace("#AFC7E8", "#1D3B68");
            }
        }
        bodyRect.sizeDelta = new Vector2(0,y);
        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewRect; scroll.content = bodyRect; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        var button = CreateButton("Choose your power", new Vector2(.5f, .10f), rect, controller.ContinueFromLevelIntro);
        StyleIntroPowerButton(button);
        overlay.SetAsLastSibling();
        return true;
    }

    private void AddBossBriefingCard(RectTransform parent, ref float y, BossPowers boss)
    {
        var row = IntroRow(parent, ref y, 260);
        row.name = "Boss briefing card";
        PrismatchTheme.Round(row.GetComponent<Image>(), PrismatchTheme.Card);
        var icon = new GameObject("Boss portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        icon.transform.SetParent(row, false);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f,1);
        icon.rectTransform.anchoredPosition = new Vector2(0,-66);
        icon.rectTransform.sizeDelta = new Vector2(104,104);
        PowerUpIconLibrary.Apply(icon, boss); icon.raycastTarget = false;
        var name = CreateText("Boss name", Vector2.zero, Vector2.zero, 32, TextAlignmentOptions.Top, row);
        var description = CreateText("Boss effect", Vector2.zero, Vector2.zero, 26, TextAlignmentOptions.Top, row);
        name.text = boss is ChallengeBoss moveBoss && moveBoss.UsesMoveTax ? "2 MOVES" : "THE RULE"; name.fontStyle = FontStyles.Bold; name.color = PrismatchTheme.Navy;
        description.text = boss is ChallengeBoss challenge ? challenge.BriefRule : boss.GetCurrentPowerDescription(); description.color = PrismatchTheme.Navy;
        name.raycastTarget = description.raycastTarget = false;
        float width = Mathf.Min(850, canvasTransform.rect.width * .92f) * .88f - 48;
        float nameHeight = name.GetPreferredValues(name.text, Mathf.Max(100,width), 0).y;
        float descriptionHeight = description.GetPreferredValues(description.text, Mathf.Max(100,width), 0).y;
        float height = 148 + nameHeight + descriptionHeight + 32;
        foreach (var text in new[] { name, description }) {
            text.rectTransform.anchorMin = new Vector2(0,1); text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(.5f,1);
            text.rectTransform.sizeDelta = new Vector2(-48,text == name ? nameHeight : descriptionHeight);
            text.rectTransform.anchoredPosition = new Vector2(0,text == name ? -132 : -148-nameHeight);
        }
        row.sizeDelta = new Vector2(0,height);
        y += height - 260;
    }

    private void BuildLevelLesson(RectTransform parent, LevelConfig level, GameController controller)
    {
        var viewport = new GameObject("Lesson viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(parent, false);
        StretchIntroRect(viewport, new Vector2(.02f, .17f), new Vector2(.98f, .76f));
        viewport.GetComponent<Image>().color = Color.clear;
        levelLesson = viewport.gameObject.AddComponent<LevelLessonAnimation>();
        levelLesson.Initialize(LevelLessonStoryboard.ForLevel(level, controller.GetEffectiveMatchRules()));
        if (controller.CurrentGoal == GoalType.BlocksCleared && controller.CurrentBlockObjective.IsRestricted)
        {
            string objective = controller.CurrentBlockObjective.Description;
            var admitted = controller.PowerManager.ActivePowers().OfType<CollectionPower>()
                .Where(power => power.Style == CollectionStyle.Admission).Select(power => power.Animal).Distinct().ToList();
            if (controller.CurrentBlockObjective.Animal.HasValue && admitted.Count > 0)
                objective += "\nAlso count " + string.Join(", ", admitted) + ".";
            levelLesson.AddInstructionStep("This round", objective);
        }
        foreach (var boss in controller.PowerManager.ActiveBossPowers())
            levelLesson.AddInstructionStep(boss.Name, boss is ChallengeBoss challenge ? challenge.BriefRule : boss.GetCurrentPowerDescription());
        var button = CreateButton("Choose your power", new Vector2(.5f, .10f), parent, controller.ContinueFromLevelIntro);
        StyleIntroPowerButton(button);
    }

    private RectTransform IntroRow(RectTransform parent, ref float y, float height)
    {
        var row = new GameObject("BriefingCard", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        row.SetParent(parent,false); row.anchorMin = new Vector2(0,1); row.anchorMax = Vector2.one;
        row.pivot = new Vector2(.5f,1); row.anchoredPosition = new Vector2(0,-y); row.sizeDelta = new Vector2(0,height);
        PrismatchTheme.Round(row.GetComponent<Image>(),PrismatchTheme.RaisedNavy);
        row.GetComponent<Image>().raycastTarget = false; y += height+12;
        return row;
    }

    private void IntroMetric(RectTransform parent, ref float y, string kind, string value, string heading)
    {
        var row=IntroRow(parent,ref y,104);
        var icon=new GameObject("Icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
        icon.transform.SetParent(row,false);icon.raycastTarget=false;
        var ir=icon.rectTransform;ir.anchorMin=ir.anchorMax=new Vector2(0,.5f);ir.anchoredPosition=new Vector2(54,0);ir.sizeDelta=new Vector2(52,52);
        if(kind=="shuffle" || kind=="power")
        {icon.sprite=Resources.Load<Sprite>(kind=="shuffle" ? PowerSymbolLibrary.ResourceFor("shuffle") : "UI/Generated/nav-powers");icon.preserveAspect=true;}
        else if(kind=="points") {icon.color=PrismatchTheme.Sky;ir.sizeDelta=new Vector2(36,36);ir.localEulerAngles=new Vector3(0,0,45);}
        else if(kind=="seconds")
        {
            PrismatchTheme.Round(icon,PrismatchTheme.Cream);
            var hands=CreateText("Clock",new Vector2(.5f,.5f),new Vector2(50,50),40,TextAlignmentOptions.Center,ir);
            hands.text="L";hands.color=PrismatchTheme.Navy;
        }
        else if(kind=="blocks" || kind=="grid")
        {
            icon.color=Color.clear;
            for(int i=0;i<4;i++)
            {
                var square=new GameObject("Block",typeof(RectTransform),typeof(Image)).GetComponent<Image>();square.transform.SetParent(ir,false);
                square.rectTransform.sizeDelta=new Vector2(20,20);square.rectTransform.anchoredPosition=new Vector2(i%2*25-12,i/2*25-12);
                PrismatchTheme.Round(square,PrismatchTheme.Green);square.raycastTarget=false;
            }
        }
        else { icon.color=Color.clear; var arrows=CreateText("Swap",new Vector2(.5f,.5f),new Vector2(70,50),32,TextAlignmentOptions.Center,ir);arrows.text="<>"; }
        var text=CreateText("Metric",Vector2.zero,Vector2.zero,30,TextAlignmentOptions.MidlineLeft,row);
        StretchIntroRect(text.rectTransform,Vector2.zero,Vector2.one);
        text.rectTransform.offsetMin=new Vector2(104,8);text.rectTransform.offsetMax=new Vector2(-12,-8);
        text.text=$"<size=19><color=#AFC7E8>{heading}</color></size>\n<b>{value}</b>" +
            (kind=="points" || kind=="blocks" || kind=="seconds" || kind=="moves" ? " " + kind : "");
    }

    private void IntroMatch(RectTransform parent, ref float y, MatchRule rule)
    {
        var row=IntroRow(parent,ref y,132);
        var animal=Resources.LoadAll<Item>("Items/").FirstOrDefault(i=>i.ItemType==ItemType.Duck);
        for(int cy=0;cy<3;cy++)for(int cx=0;cx<3;cx++)
        {
            bool lit=rule.type==MatchType.Horizontal ? cy==1 : rule.type==MatchType.Vertical ? cx==1 :
                (cy==0&&cx<2)||(cy==1&&cx==1)||(cy==2&&cx>0);
            var cell=new GameObject("MatchCell",typeof(RectTransform),typeof(Image)).GetComponent<Image>();cell.transform.SetParent(row,false);
            cell.rectTransform.anchorMin=cell.rectTransform.anchorMax=new Vector2(0,.5f);
            cell.rectTransform.anchoredPosition=new Vector2(30+cx*32,32-cy*32);cell.rectTransform.sizeDelta=new Vector2(29,29);
            PrismatchTheme.Round(cell,lit?PrismatchTheme.Green:PrismatchTheme.Navy);cell.raycastTarget=false;
            if(lit&&animal!=null)
            {
                var art=new GameObject("Animal",typeof(RectTransform),typeof(Image)).GetComponent<Image>();art.transform.SetParent(cell.transform,false);
                art.rectTransform.sizeDelta=new Vector2(25,25);art.sprite=animal.sprite;art.preserveAspect=true;art.raycastTarget=false;
            }
        }
        var label=CreateText("MatchRule",Vector2.zero,Vector2.zero,28,TextAlignmentOptions.MidlineLeft,row);
        StretchIntroRect(label.rectTransform,Vector2.zero,Vector2.one);label.rectTransform.offsetMin=new Vector2(130,8);label.rectTransform.offsetMax=new Vector2(-8,-8);
        label.text=$"<b>{(rule.type==MatchType.Snake ? "Connected" : rule.type.ToString())} {rule.minimumLength}+</b>\n<size=22>Same animal</size>";
    }

    private static void StretchIntroRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static string BuildLevelBriefing(GameController controller)
    {
        var level = GameState.Instance.CurrentLevel;
        string goal = controller.CurrentGoal == GoalType.Score ? $"Score {controller.CurrentTarget:N0} points"
            : $"Match {controller.CurrentTarget:N0} blocks";
        string budget = controller.IsTimed ? $"before {controller.TimeRemaining / 60}:{controller.TimeRemaining % 60:00} runs out."
            : $"within {controller.MovesRemaining} moves.";
        var replacement = (level as ILevelSettings)?.Replacement ?? ReplacementStrategy.InPlace;
        string refill = replacement switch
        {
            ReplacementStrategy.South => "Gravity down; blocks enter from above.",
            ReplacementStrategy.North => "Gravity up; blocks enter from below.",
            ReplacementStrategy.East => "Gravity right; blocks enter from the left.",
            ReplacementStrategy.West => "Gravity left; blocks enter from the right.",
            _ => "Matched blocks are replaced in place."
        };
        var text = new System.Text.StringBuilder();
        text.AppendLine($"<b><color=#42CC72>HOW TO WIN</color></b>\n{goal} {budget}\n");
        text.AppendLine($"<b>BOARD & MATCHING</b>\n{level.GetWidth(controller.CurrentRound)} x {level.GetHeight(controller.CurrentRound)} board");
        text.AppendLine(string.Join(" / ", controller.GetEffectiveMatchRules()));
        text.AppendLine(refill);
        text.AppendLine($"Shuffle: {controller.ShuffleCostLabel}." + (controller.IsTimed ? "\n" : " Swaps cost 1 move. Cascades are free.\n"));
        text.AppendLine("<b>ACTIVE MODIFIERS</b>");
        var powers = controller.PowerManager.ActivePowers().ToList();
        var bosses = controller.PowerManager.ActiveBossPowers().ToList();
        if (powers.Count == 0 && bosses.Count == 0) text.AppendLine("No additional modifiers.");
        foreach (var power in powers) text.AppendLine($"<b>{power.Name}</b>\n{power.PowerUpDescription}\n");
        foreach (var boss in bosses) text.AppendLine($"<color=#FF9977><b>{boss.Name}</b></color>\n{boss.GetCurrentPowerDescription()}\n");
        return text.ToString();
    }

    private void BuildEndPanel(GameController controller)
    {
        endPanel = new GameObject("RunEndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(canvasTransform, false);
        var rect = (RectTransform)endPanel.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        endPanel.GetComponent<Image>().color = PrismatchTheme.Midnight;
        PrismatchTheme.ApplyBackdrop(rect);

        var card = new GameObject("ResultsCard", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        card.SetParent(rect, false);
        StretchIntroRect(card, new Vector2(.06f, .05f), new Vector2(.94f, .95f));
        PrismatchTheme.Round(card.GetComponent<Image>(), PrismatchTheme.Navy);
        PrismatchTheme.Shadow(card.GetComponent<Image>(), new Color(0, 0, 0, .25f), new Vector2(0, -8));
        var accent = new GameObject("ResultAccent", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        accent.SetParent(card, false);
        StretchIntroRect(accent, new Vector2(.40f, .965f), new Vector2(.60f, .972f));
        endAccent = accent.GetComponent<Image>();
        PrismatchTheme.Round(endAccent, PrismatchTheme.Orange);
        endAccent.raycastTarget = false;

        endContext = CreateEndText("Context", card, new Vector2(.06f, .90f), new Vector2(.94f, .95f), 21, PrismatchTheme.Mist);
        endTitle = CreateEndText("RunEndTitle", card, new Vector2(.04f, .80f), new Vector2(.96f, .90f), 46, PrismatchTheme.Cream);
        endTitle.fontStyle = FontStyles.Bold;
        endSummary = CreateEndText("RunEndSummary", card, new Vector2(.06f, .69f), new Vector2(.94f, .80f), 28, PrismatchTheme.Mist);

        string[] labels = { "SCORE", "ROUNDS", "MATCHES", "CASCADES", "SHUFFLES", "TILES CLEARED" };
        for (int i = 0; i < endStats.Length; i++)
        {
            float x = i % 2 == 0 ? .06f : .515f;
            float top = .66f - (i / 2) * .115f;
            var tile = new GameObject(labels[i], typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            tile.SetParent(card, false);
            StretchIntroRect(tile, new Vector2(x, top - .10f), new Vector2(x + .425f, top));
            PrismatchTheme.Round(tile.GetComponent<Image>(), PrismatchTheme.RaisedNavy);
            tile.GetComponent<Image>().raycastTarget = false;
            var label = CreateEndText("Label", tile, new Vector2(.27f, .57f), new Vector2(.96f, .91f), 18, PrismatchTheme.Mist);
            label.text = labels[i];
            endStatLabels[i] = label;
            AddResultIcon(tile, i);
            endStats[i] = CreateEndText("Value", tile, new Vector2(.27f, .07f), new Vector2(.96f, .58f), 32, PrismatchTheme.Cream);
            endStats[i].fontStyle = FontStyles.Bold;
        }

        var btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(VerticalLayoutGroup));
        btnRow.transform.SetParent(card, false);
        var rowRt = (RectTransform)btnRow.transform;
        StretchIntroRect(rowRt, new Vector2(.08f, .035f), new Vector2(.92f, .295f));
        var hlg = btnRow.GetComponent<VerticalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        retryButton = CreateButton("Retry Round", new Vector2(0.5f, 0.5f), rowRt, controller.OnRestartRound);
        endNewRunButton = CreateButton("New Run", new Vector2(0.5f, 0.5f), rowRt, controller.NewGameButton);
        var selectButton = CreateButton("Level Select", new Vector2(0.5f, 0.5f), rowRt, () => SceneManager.LoadScene("Level Select"));
        var analyticsButton = CreateButton("Analytics / Export", new Vector2(.5f,.5f), rowRt, () => ShowAnalytics(controller));
        foreach (var button in new[] { retryButton, endNewRunButton, selectButton, analyticsButton })
        {
            button.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            StyleEndButton(button, false);
        }
        endPanel.SetActive(false);
    }

    private void AddResultIcon(RectTransform parent, int kind)
    {
        var slot = new GameObject("IconSlot", typeof(RectTransform)).GetComponent<RectTransform>();
        slot.SetParent(parent, false);
        StretchIntroRect(slot, new Vector2(.04f, .18f), new Vector2(.23f, .82f));
        var root = new GameObject("StatIcon", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
        root.SetParent(slot, false);
        root.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        root.GetComponent<AspectRatioFitter>().aspectRatio = 1;
        // Keep icon proportions independent of the wide stat cards.
        string resource = kind == 4 ? PowerSymbolLibrary.ResourceFor("shuffle") : kind == 3 ? "UI/Generated/power-momentum" : null;
        if (resource != null)
        {
            var icon = root.gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(resource);
            icon.preserveAspect = true; icon.raycastTarget = false;
            return;
        }
        for (int i = 0; i < (kind == 1 ? 2 : 4); i++)
        {
            var part = new GameObject("Symbol", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            part.SetParent(root, false);
            Vector2 min = kind == 1 ? (i == 0 ? new Vector2(.20f, .08f) : new Vector2(.30f, .48f))
                : new Vector2(.08f + (i % 2) * .47f, .08f + (i / 2) * .47f);
            Vector2 max = kind == 1 ? (i == 0 ? new Vector2(.30f, .92f) : new Vector2(.88f, .90f)) : min + Vector2.one * .36f;
            StretchIntroRect(part, min, max);
            var image = part.GetComponent<Image>();
            PrismatchTheme.Round(image, kind == 1 ? PrismatchTheme.Orange : kind == 2 ? PrismatchTheme.Sky : PrismatchTheme.Green);
            image.raycastTarget = false;
        }
    }

    private TextMeshProUGUI CreateEndText(string name, Transform parent, Vector2 min, Vector2 max, float size, Color color)
    {
        var text = CreateText(name, Vector2.zero, Vector2.zero, size, TextAlignmentOptions.Center, parent);
        StretchIntroRect(text.rectTransform, min, max);
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = size;
        return text;
    }

    private void StyleIntroPowerButton(Button button)
    {
        // Keep the primary action's height independent of the briefing card's height.
        var rect=(RectTransform)button.transform;
        rect.anchorMin=new Vector2(.12f,.10f);rect.anchorMax=new Vector2(.88f,.10f);
        rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
        rect.sizeDelta=new Vector2(0,96);
        StyleEndButton(button,true);
        PrismatchTheme.Shadow(button.GetComponent<Image>(),new Color(0,0,0,.25f),new Vector2(0,-4));
        var label=button.GetComponentInChildren<TextMeshProUGUI>();
        label.textWrappingMode=TextWrappingModes.NoWrap;
    }

    private void StyleEndButton(Button button, bool primary)
    {
        PrismatchTheme.Round(button.GetComponent<Image>(), primary ? PrismatchTheme.Orange : PrismatchTheme.RaisedNavy);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.90f, .95f, 1f);
        colors.pressedColor = new Color(.75f, .80f, .90f);
        colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
        button.colors = colors;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        StretchIntroRect(label.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
        label.color = primary ? PrismatchTheme.Midnight : PrismatchTheme.Cream;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = 28;
    }

    private TextMeshProUGUI CreateText(string name, Vector2 anchor, Vector2 size, float fontSize,
        TextAlignmentOptions alignment, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent != null ? parent : canvasTransform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize; text.alignment = alignment; text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string label, Vector2 anchor, Transform parent, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(440, 96);
        go.GetComponent<Image>().color = new Color(.18f, .46f, .72f, 1);
        var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = CreateText("Label", new Vector2(.5f, .5f), new Vector2(420, 88), 30, TextAlignmentOptions.Center, rect);
        text.text = label; return button;
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"<size=60%><color=#AFC7E8>{(GameState.Instance?.CurrentLevel is FreePlayConfig config ? $"ROUND {latestRound} / {config.Levels}" : $"LEVEL {latestLevel}")}</color></size>\n<size=60%>SCORE</size>\n<size=100%>{score:N0}</size>";
        UpdateScoreProgress(goalType == GoalType.BlocksCleared ? goalProgress : score);
        int delta = score - previousScore;
        if (delta > 0) ShowFloatingScore(delta);
        previousScore = score;
    }
    public void UpdateTarget(int target)
    {
        latestTarget = target;
        scoreProgressTween?.Kill();
        scoreProgressTween = null;
        displayedProgressTarget = -1f;
        goalProgress = 0;
        UpdateScoreStats(0, 1);
        UpdateScoreProgress(0);
        if (targetText != null)
            targetText.text = $"<size=60%><color=#AFC7E8>TARGET</color></size>\n<size=100%>{target:N0}</size>";
    }
    public void UpdateTimer(float time)
    {
        int remaining = Mathf.CeilToInt(Mathf.Max(0, time));
        int total = GameState.Instance?.CurrentLevel?.GetTotalTime ?? Mathf.Max(1, remaining);
        UpdateBudget("TIME LEFT", $"{remaining / 60:0}:{remaining % 60:00}", remaining, 10, total);
    }
    public void UpdateRound(int round)
    {
        latestRound = Mathf.Max(1, round);
        latestLevel = GameState.Instance?.CurrentLevel?.GetLevelNumber ?? Mathf.Max(1, round);
        UpdateRoundLabel();
        UpdateScore(previousScore);
    }

    private void UpdateRoundLabel()
    {
        if (roundText != null)
            roundText.text = $"Round {latestRound} of {GameState.Instance?.CurrentLevel?.GetTotalRounds ?? 1}";
    }

    private Tween rewardPulse;
    private int liveRewardParticles;
    public void ShowRewardBurst(float impact, System.Collections.Generic.List<Tile> tiles)
    {
        if (canvasTransform == null || tiles == null || tiles.Count == 0) return;
        int tier = impact >= .25f ? 2 : impact >= .1f ? 1 : 0;
        Color color = tier == 2 ? PrismatchTheme.Orange : tier == 1 ? PrismatchTheme.Green : PrismatchTheme.Sky;
        if (scoreProgressFill != null)
        {
            rewardPulse?.Kill();
            scoreProgressFill.color = Color.Lerp(color, Color.white, .45f);
            rewardPulse = scoreProgressFill.DOColor(PrismatchTheme.Green, .3f)
                .SetUpdate(true).SetLink(scoreProgressFill.gameObject).Play();
        }
        // UI particles render above the board but never intercept touch or mouse input.
        int count = Mathf.Min(6 + tier * 6, 48 - liveRewardParticles);
        var canvas = canvasTransform.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        for (int i = 0; i < count; i++)
        {
            var tile = tiles[i % tiles.Count];
            if (tile == null) continue;
            var screen = RectTransformUtility.WorldToScreenPoint(camera, tile.transform.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, screen, camera, out var point)) continue;
            var go = new GameObject("ClearSpark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasTransform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = canvasTransform.pivot;
            rect.anchoredPosition = point;
            rect.sizeDelta = Vector2.one * (8 + tier * 4);
            rect.localRotation = Quaternion.Euler(0, 0, 45);
            var spark = go.GetComponent<Image>();
            PrismatchTheme.Round(spark, color);
            spark.raycastTarget = false;
            float angle = i * 2.39996f;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (42 + tier * 24);
            liveRewardParticles++;
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(go);
            sequence.Join(rect.DOAnchorPos(point + offset, .32f).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOScale(.15f, .32f));
            sequence.Join(spark.DOFade(0, .32f));
            sequence.OnKill(() => { liveRewardParticles--; if (go != null) Destroy(go); });
            sequence.Play();
        }
    }

    private sealed class TriggeredIcon
    {
        public RectTransform Rect;
        public CanvasGroup Fade;
        public float Age, Spread, Height;
    }
    private RectTransform triggeredPowerRow;
    private readonly System.Collections.Generic.List<TriggeredIcon> triggeredIcons = new();
    private readonly PowerFountainQueue powerFountain = new();
    private Coroutine triggeredIconRoutine;
    private int fountainLaunchIndex;
    private RectTransform scoreFeedback;
    private TextMeshProUGUI scoreFeedbackText;
    private CanvasGroup scoreFeedbackFade;
    private int scoreFeedbackMove, scoreFeedbackAmount;
    private float scoreFeedbackAge;
    private bool scoreFeedbackActive;
    private const float ScoreRiseDuration=.35f;
    private void ShowTriggeredIcons(PowerActivation[] activations)
    {
        powerFountain.Enqueue(Board.Instance?.MoveId ?? 0,activations);
        EnsurePowerFountain();
        if(triggeredIconRoutine==null && powerFountain.Count>0)
            triggeredIconRoutine=StartCoroutine(AnimatePowerFountain());
    }
    private void EnsurePowerFountain()
    {
        if(triggeredPowerRow==null) {
            // The fountain emerges from behind the footer. Its mask keeps every pixel
            // inside the empty space below the grid and above all footer controls.
            triggeredPowerRow=new GameObject("Power fountain",typeof(RectTransform),typeof(RectMask2D),typeof(CanvasGroup)).GetComponent<RectTransform>();
            triggeredPowerRow.SetParent(canvasTransform,false);
            triggeredPowerRow.anchorMin=triggeredPowerRow.anchorMax=new Vector2(.5f,.5f);
            var group=triggeredPowerRow.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        }
    }
    private bool PositionPowerFountain()
    {
        float boardBottom=float.PositiveInfinity,left=float.PositiveInfinity,right=float.NegativeInfinity;
        var corners=new Vector3[4];
        var tiles=Board.Instance?.Tiles;
        if(tiles!=null)foreach(var tile in tiles) {
            var rect=tile!=null?tile.background?.rectTransform:null;
            if(rect==null)continue;
            rect.GetWorldCorners(corners);
            var lower=canvasTransform.InverseTransformPoint(corners[0]);
            var upper=canvasTransform.InverseTransformPoint(corners[2]);
            boardBottom=Mathf.Min(boardBottom,lower.y);left=Mathf.Min(left,lower.x);right=Mathf.Max(right,upper.x);
        }
        if(float.IsPositiveInfinity(boardBottom))return false;
        float footerTop=canvasTransform.rect.yMin;
        foreach(var rect in new[]{boardScoreStats!=null?boardScoreStats.transform as RectTransform:null,
            canvasTransform.Find("BottomBar") as RectTransform,messageBackground!=null?messageBackground.rectTransform:null}) {
            if(rect==null||!rect.gameObject.activeInHierarchy)continue;
            rect.GetWorldCorners(corners);
            footerTop=Mathf.Max(footerTop,canvasTransform.InverseTransformPoint(corners[1]).y);
        }
        float bottom=footerTop+12,top=boardBottom-12;
        float height=Mathf.Max(0,top-bottom);
        triggeredPowerRow.sizeDelta=new Vector2(Mathf.Min(right-left,canvasTransform.rect.width-32),height);
        triggeredPowerRow.localPosition=new Vector3((left+right)*.5f,bottom+height*.5f,0);
        triggeredPowerRow.gameObject.SetActive(height>=28);
        return height>=28;
    }
    private void LaunchPowerIcon(PowerActivation activation,float delay)
    {
        var rect=new GameObject("Flying: "+activation.Power.Name,typeof(RectTransform),typeof(Image),typeof(CanvasGroup)).GetComponent<RectTransform>();
        rect.SetParent(triggeredPowerRow,false);
        float size=Mathf.Min(71.875f,triggeredPowerRow.rect.height*.75f);
        rect.sizeDelta=new Vector2(size,size);
        PowerUpIconLibrary.Apply(rect.GetComponent<Image>(),activation.Power);
        foreach(var graphic in rect.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        int slot=fountainLaunchIndex++%7;
        float spread=slot==0?0:((slot+1)/2)/3f*(slot%2==1?-1:1);
        var fade=rect.GetComponent<CanvasGroup>();fade.blocksRaycasts=false;fade.interactable=false;fade.alpha=0;
        triggeredIcons.Add(new TriggeredIcon{Rect=rect,Fade=fade,Age=-delay,Spread=spread,Height=.62f+.04f*(slot%3)});
    }
    private System.Collections.IEnumerator AnimatePowerFountain()
    {
        float untilLaunch=0;
        while(powerFountain.Count>0||triggeredIcons.Count>0||scoreFeedbackActive) {
            bool visible=PositionPowerFountain();
            if(scoreFeedbackActive)AnimateScoreFeedback(Time.unscaledDeltaTime,visible);
            float delta=Time.deltaTime*GameController.AnimationSpeed;
            if(visible && delta>0) {
                untilLaunch-=delta;
                if(powerFountain.Count>0 && untilLaunch<=0) {
                    int burstIndex=0;
                    while(powerFountain.Count>0)
                        LaunchPowerIcon(powerFountain.Dequeue(),Mathf.Min(.18f,burstIndex++*.035f));
                    untilLaunch=.07f;
                }
                for(int i=triggeredIcons.Count-1;i>=0;i--) {
                    var icon=triggeredIcons[i];icon.Age+=delta;
                    float t=Mathf.Clamp01(icon.Age/2.8f);
                    if(t>=1) { Destroy(icon.Rect.gameObject);triggeredIcons.RemoveAt(i);continue; }
                    float size=icon.Rect.rect.width;
                    float horizontal=Mathf.Max(0,(triggeredPowerRow.rect.width-size)*.5f-6)*.85f;
                    float x=icon.Spread*horizontal*(.8f+.2f*t);
                    float y=-triggeredPowerRow.rect.height*.5f-size*.5f+4*t*(1-t)*triggeredPowerRow.rect.height*icon.Height;
                    icon.Rect.anchoredPosition=new Vector2(x,y);
                    icon.Rect.localScale=Vector3.one*(.95f+.05f*Mathf.Sin(t*Mathf.PI));
                    icon.Rect.localRotation=Quaternion.Euler(0,0,-icon.Spread*4*Mathf.Sin(t*Mathf.PI));
                    icon.Fade.alpha=Mathf.Min(Mathf.Clamp01(t/.12f),Mathf.Clamp01((1-t)/.2f));
                }
            }
            yield return null;
        }
        triggeredIconRoutine=null;
    }
    private void ClearTriggeredIcons()
    {
        if(triggeredIconRoutine!=null)StopCoroutine(triggeredIconRoutine);
        triggeredIconRoutine=null;powerFountain.Clear();fountainLaunchIndex=0;
        foreach(var icon in triggeredIcons)if(icon.Rect!=null)Destroy(icon.Rect.gameObject);
        triggeredIcons.Clear();
        scoreFeedbackActive=false;scoreFeedbackAmount=0;
        if(scoreFeedback!=null)scoreFeedback.gameObject.SetActive(false);
    }

    private void AnimateScoreFeedback(float delta,bool visible)
    {
        scoreFeedbackAge=Mathf.Min(ScoreRiseDuration,scoreFeedbackAge+delta);
        if(!visible)return;
        float height=Mathf.Min(62,triggeredPowerRow.rect.height*.6f);
        scoreFeedback.sizeDelta=new Vector2(Mathf.Min(340,triggeredPowerRow.rect.width-12),height);
        float rise=Mathf.Clamp01(scoreFeedbackAge/ScoreRiseDuration);
        float eased=1-Mathf.Pow(1-rise,3);
        float start=-triggeredPowerRow.rect.height*.5f-height*.5f;
        float end=triggeredPowerRow.rect.height*.5f-height*.5f-4;
        scoreFeedback.anchoredPosition=new Vector2(0,Mathf.Lerp(start,end,eased));
        scoreFeedbackFade.alpha=Mathf.Clamp01(rise*3);
        scoreFeedback.SetAsLastSibling();
    }

    private readonly System.Collections.Generic.Queue<GameObject> powerFeedbackBatches = new();

    public void ShowPowerActivations(System.Collections.Generic.IEnumerable<PowerActivation> activations, System.Collections.Generic.List<Tile> tiles)
    {
        var entries=activations?.Where(a=>a?.Power!=null).GroupBy(a=>a.Power.Id).Select(g=>
            new PowerActivation(g.First().Power,string.Join(" / ",g.Select(a=>a.Effect).Distinct()),g.All(a=>a.Progress),g.Aggregate(PowerTriggerDisplay.None,(display,a)=>display|a.Display))).ToArray();
        var matched=tiles?.Where(t=>t!=null).Distinct().ToArray();
        if(entries==null||entries.Length==0)return;
        if(canvasTransform==null)canvasTransform=GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if(canvasTransform==null)return;
        var iconEntries=entries.Where(a=>(a.Display & PowerTriggerDisplay.Icon)!=0).ToArray();
        if(iconEntries.Length>0)ShowTriggeredIcons(iconEntries);
        var tileEntries=entries.Where(a=>(a.Display & PowerTriggerDisplay.MatchedTiles)!=0).ToArray();
        if(tileEntries.Length==0||matched==null||matched.Length==0)return;
        while(powerFeedbackBatches.Count>0&&powerFeedbackBatches.Peek()==null)powerFeedbackBatches.Dequeue();
        while(powerFeedbackBatches.Count>=3){var old=powerFeedbackBatches.Dequeue();if(old!=null)Destroy(old);}
        var root=new GameObject("Power activation feedback",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();
        root.SetParent(canvasTransform,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var fade=root.GetComponent<CanvasGroup>();fade.blocksRaycasts=false;fade.interactable=false;
        powerFeedbackBatches.Enqueue(root.gameObject);
        RectTransform Element(string name,Transform parent,Vector2 position,Vector2 size){
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        float Duration(float seconds) => seconds / GameController.AnimationSpeed;
        Color accent=tileEntries.All(a=>a.Progress)?PrismatchTheme.Sky:PrismatchTheme.Orange;

        // One shared clock and lifetime for the entire match highlight.
        var highlights = Element("Match highlights", root, Vector2.zero, Vector2.zero);
        var highlightFade = highlights.gameObject.AddComponent<CanvasGroup>();
        highlightFade.blocksRaycasts = false;
        var highlightAnimation = DOTween.Sequence().SetLink(highlights.gameObject);
        highlightAnimation.Join(highlightFade.DOFade(0, Duration(.38f)));
        foreach(var tile in matched.Take(20)){
            var tileRect=tile.background!=null?tile.background.rectTransform:tile.transform as RectTransform;
            if(tileRect==null)continue;
            var corners=new Vector3[4];tileRect.GetWorldCorners(corners);
            var min=root.InverseTransformPoint(corners[0]);var max=root.InverseTransformPoint(corners[2]);
            var ring=Element("Triggered tile glow",highlights,(min+max)*.5f,new Vector2(Mathf.Abs(max.x-min.x),Mathf.Abs(max.y-min.y))+new Vector2(8,8));
            var image=ring.gameObject.AddComponent<Image>();PrismatchTheme.Round(image,new Color(accent.r,accent.g,accent.b,.8f));image.fillCenter=false;image.pixelsPerUnitMultiplier=3;image.raycastTarget=false;
            highlightAnimation.Join(ring.DOScale(1.16f,Duration(.32f)));
        }
        highlightAnimation.OnKill(() => { if (highlights != null) Destroy(highlights.gameObject); }).Play();
        fade.DOFade(0,Duration(.28f)).SetDelay(Duration(.72f)).SetLink(root.gameObject)
            .OnKill(()=>{if(root!=null)Destroy(root.gameObject);}).Play();
        StartCoroutine(ExpirePowerFeedback(root.gameObject, Duration(1.1f)));
    }

    private System.Collections.IEnumerator ExpirePowerFeedback(GameObject feedback, float lifetime)
    {
        // A real-time deadline also cleans up if a tween is paused or interrupted.
        yield return new WaitForSecondsRealtime(lifetime);
        if (feedback != null) Destroy(feedback);
    }

    private void OnDisable()
    {
        ClearTriggeredIcons();
        while (powerFeedbackBatches.Count > 0)
        {
            var feedback = powerFeedbackBatches.Dequeue();
            if (feedback != null) Destroy(feedback);
        }
    }

    public void ShowScoreBreakdown(ScoreResult result, System.Collections.Generic.List<Tile> tiles)
    {
        if (result == null) return;
        RefreshFlatStats();
        ShowPowerActivations(result.Activations,tiles);
    }

    public void ShowMessage(string message, float duration = 4f)
    {
        if (messageText == null) { Debug.Log(message); return; }
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(Message(message, duration));
    }
    private void SetAnimalBonusContentsVisible(bool visible)
    {
        if(animalBonusLabel!=null)animalBonusLabel.gameObject.SetActive(visible);
        for(int i=0;i<animalBonusIcons.Count;i++) {
            bool active=visible&&i<animalBonusTargetCount;
            animalBonusIcons[i].gameObject.SetActive(active);animalBonusNames[i].gameObject.SetActive(active);
        }
    }
    private void LayoutBoardMessage()
    {
        if(messageBackground==null)return;
        var rect=messageBackground.rectTransform;
        bool overlay=animalBonusActive&&animalBonusPanel!=null;
        if(overlay) {
            rect.SetParent(animalBonusPanel,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }else {
            rect.SetParent(canvasTransform,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
            rect.sizeDelta=new Vector2(620,76);rect.anchoredPosition=new Vector2(0,320);
        }
        messageBackground.enabled=!overlay;
        var shadow=messageBackground.GetComponent<Shadow>();if(shadow!=null)shadow.enabled=!overlay;
        var outline=messageBackground.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
        rect.SetAsLastSibling();
    }
    private void ClearBoardMessage()
    {
        if(messageRoutine!=null){StopCoroutine(messageRoutine);messageRoutine=null;}

        if(messageText!=null)messageText.gameObject.SetActive(false);
        if(messageBackground!=null)messageBackground.gameObject.SetActive(false);
        SetAnimalBonusContentsVisible(true);
    }
    private IEnumerator Message(string message, float duration)
    {

        LayoutBoardMessage();
        SetAnimalBonusContentsVisible(!(animalBonusActive&&animalBonusPanel!=null));
        bool invalid = message.StartsWith("No match.");
        messageText.text = invalid ? "<size=24><color=#FF9F1C>NO MATCH</color></size>\n" + message.Substring(9).Trim() : message;
        messageText.fontSize = invalid ? 28 : 25;
        messageText.color = PrismatchTheme.Cream;
        messageText.gameObject.SetActive(true);
        if(messageBackground!=null)messageBackground.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(duration);
        messageText.gameObject.SetActive(false);
        if(messageBackground!=null)messageBackground.gameObject.SetActive(false);
        messageRoutine=null;
        SetAnimalBonusContentsVisible(true);
    }
    public void ShowPaused(bool shown) { if (pauseText != null) pauseText.gameObject.SetActive(shown); }
    public void HideRunEnd() { if (endPanel != null) endPanel.SetActive(false); }
    public void ShowRunEnd(bool won, RunSummary summary, GameController controller)
    {
        if (endPanel == null) return;
        endPanel.SetActive(true); endPanel.transform.SetAsLastSibling();
        endTitle.text = won ? "GOAL REACHED" : controller.IsTimed ? "TIME UP" : "OUT OF MOVES";
        endTitle.color = PrismatchTheme.Cream;
        endAccent.color = won ? PrismatchTheme.Orange : PrismatchTheme.Coral;
        endContext.text = GameState.Instance?.CurrentLevel is FreePlayConfig ? "FREE PLAY / RESULTS" : $"LEVEL {latestLevel} / RESULTS";
        var last = summary.rounds.LastOrDefault();
        bool blockGoal = controller.CurrentGoal == GoalType.BlocksCleared;
        float pace = last == null ? 0 : (blockGoal ? last.tilesCleared / Mathf.Max(1f, last.seconds) : last.scorePerSecond);
        endStatLabels[0].text = blockGoal ? "TILES CLEARED" : "SCORE";
        endStatLabels[5].text = blockGoal ? "LAST ROUND TILES" : "TILES CLEARED";
        endSummary.text = $"<color=#FFF7E8>{controller.GoalProgress:N0} / {controller.CurrentTarget:N0}</color> {(controller.CurrentGoal == GoalType.Score ? "points" : "blocks")}" +
            (last == null ? "" : $"\n<size=70%>Last round: {pace:F1} {(blockGoal ? "tiles" : "points")}/sec</size>");
        string[] values = { (blockGoal ? summary.TotalTilesCleared : summary.TotalScore).ToString("N0"), $"{summary.CompletedRounds} / {controller.maxRounds}",
            summary.Matches.ToString("N0"), summary.Cascades.ToString("N0"), summary.Shuffles.ToString("N0"), (blockGoal ? last?.tilesCleared ?? 0 : summary.TotalTilesCleared).ToString("N0") };
        for (int i = 0; i < endStats.Length; i++) endStats[i].text = values[i];
        retryButton.gameObject.SetActive(!won);
        retryButton.interactable = controller.CanRetry;
        StyleEndButton(retryButton, !won && controller.CanRetry);
        StyleEndButton(endNewRunButton, won || !controller.CanRetry);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)retryButton.transform.parent);
    }

    public void BeginPlayerMoveScore()
    {
        scoreFeedbackMove=Board.Instance?.MoveId??0;
        scoreFeedbackAmount=0;
        scoreFeedbackAge=0;
        scoreFeedbackActive=false;
        if(scoreFeedback!=null)scoreFeedback.gameObject.SetActive(false);
    }

    public void ShowFloatingScore(int amount)
    {
        if(amount<=0||canvasTransform==null||latestTarget<=0)return;
        EnsurePowerFountain();
        if(scoreFeedback==null) {
            scoreFeedback=new GameObject("Match score feedback",typeof(RectTransform),typeof(Image),typeof(CanvasGroup)).GetComponent<RectTransform>();
            scoreFeedback.SetParent(triggeredPowerRow,false);
            var background=scoreFeedback.GetComponent<Image>();PrismatchTheme.Round(background,PrismatchTheme.Navy);background.raycastTarget=false;
            scoreFeedbackFade=scoreFeedback.GetComponent<CanvasGroup>();scoreFeedbackFade.blocksRaycasts=false;scoreFeedbackFade.interactable=false;
            scoreFeedbackText=CreateText("Score gained",new Vector2(.5f,.5f),new Vector2(320,58),42,TextAlignmentOptions.Center,scoreFeedback);
            var textRect=scoreFeedbackText.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;
            textRect.offsetMin=new Vector2(8,2);textRect.offsetMax=new Vector2(-8,-2);
            scoreFeedbackText.enableAutoSizing=true;scoreFeedbackText.fontSizeMin=20;scoreFeedbackText.fontSizeMax=42;
            scoreFeedbackText.fontStyle=FontStyles.Bold;scoreFeedbackText.raycastTarget=false;
        }
        int move=Board.Instance?.MoveId??0;
        bool sameMove=scoreFeedbackMove==move;
        scoreFeedbackAmount=sameMove?ScoreManager.SaturatingAdd(scoreFeedbackAmount,amount):amount;
        scoreFeedbackAge=sameMove&&scoreFeedbackActive?scoreFeedbackAge:0;
        scoreFeedbackMove=move;
        scoreFeedbackActive=true;scoreFeedback.gameObject.SetActive(true);
        scoreFeedbackText.text=$"+{scoreFeedbackAmount:N0} <size=55%>POINTS</size>";
        float percent=(float)scoreFeedbackAmount/latestTarget;
        scoreFeedbackText.color=percent>=.25f?PrismatchTheme.Orange:percent>=.15f?PrismatchTheme.Green:PrismatchTheme.Cream;
        if(PositionPowerFountain())AnimateScoreFeedback(0,true);
        if(triggeredIconRoutine==null)triggeredIconRoutine=StartCoroutine(AnimatePowerFountain());
    }
}
