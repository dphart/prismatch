using System.Collections;
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
    private int previousScore, latestTarget, latestLevel = 1;
    private TextMeshProUGUI messageText, pauseText, endTitle, endSummary;
    private TextMeshProUGUI basePointsText, multiplierText;
    private TextMeshProUGUI powersButtonText, bossButtonText, soundButtonText;
    private Image restartButtonBackground;
    private TextMeshProUGUI scoreProgressText;
    private TextMeshProUGUI matchRulesText;
    private Image scoreProgressFill;
    private GameObject boardScoreStats;
    private GameObject scoreProgressRoot;
    private GameObject endPanel;
    private GameObject settingsPanel;
    private Button retryButton;
    private Coroutine messageRoutine;

    public void InitializeRunUI(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null || messageText != null) return;
        PrismatchTheme.ApplyBackdrop(canvasTransform);
        StyleHud();
        StyleSettingsButton(controller);
        BuildScoreProgress();
        BuildMatchRulesLabel();
        StyleBottomBar(controller);
        messageText = CreateText("RuleMessage", new Vector2(.5f, .80f), new Vector2(640, 55), 21, TextAlignmentOptions.Center);
        BuildBoardStats();
        pauseText = CreateText("PauseMessage", new Vector2(.5f, .5f), new Vector2(500, 100), 52, TextAlignmentOptions.Center);
        pauseText.text = "PAUSED"; pauseText.gameObject.SetActive(false);
        BuildEndPanel(controller);
        BuildSettingsPanel(controller);
    }

    public void ApplyEditorPreview(GameController controller)
    {
        if (canvasTransform == null) canvasTransform = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (canvasTransform == null) return;
        PrismatchTheme.ApplyBackdrop(canvasTransform);
        StyleHud();
        StyleSettingsButton(controller);
        StyleBottomBar(controller);
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

        Transform hudRow = scoreText != null && scoreText.transform.parent != null
            ? scoreText.transform.parent.parent
            : null;
        if (hudRow != null)
        {
            var rowRect = hudRow as RectTransform;
            if (rowRect != null) rowRect.sizeDelta = new Vector2(510f, 108f);
            if (hudRow.TryGetComponent(out HorizontalLayoutGroup layout))
            {
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }
        }

        foreach (var text in new[] { scoreText, targetText, timerText, roundText })
        {
            if (text == null) continue;
            text.color = PrismatchTheme.Cream;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 32f;
            text.margin = new Vector4(5f, 5f, 5f, 5f);
            if (text.transform.parent != null)
            {
                var element = text.transform.parent.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredWidth = 155f;
                    element.preferredHeight = 100f;
                }
            }
            if (text.transform.parent != null && text.transform.parent.TryGetComponent(out Image panelImage))
            {
                PrismatchTheme.Round(panelImage, PrismatchTheme.Navy);
                PrismatchTheme.Shadow(panelImage, new Color(0f, .04f, .12f, .45f), new Vector2(0f, -4f));
            }
        }
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

    private void BuildScoreProgress()
    {
        var root = new GameObject("ScoreProgress", typeof(RectTransform), typeof(Image));
        scoreProgressRoot = root;
        root.transform.SetParent(canvasTransform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -205f);
        rect.sizeDelta = new Vector2(510f, 54f);
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

        scoreProgressText = CreateText("ProgressLabel", new Vector2(.5f, .5f), Vector2.zero, 19f,
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

    private void UpdateScoreProgress(int score)
    {
        float progress = latestTarget > 0 ? Mathf.Clamp01((float)score / latestTarget) : 0f;
        if (scoreProgressFill != null) scoreProgressFill.fillAmount = progress;
        if (scoreProgressText != null)
        {
            int remaining = Mathf.Max(0, latestTarget - score);
            scoreProgressText.text = remaining > 0
                ? $"<b>{progress * 100f:0}%</b>  •  {remaining:N0} POINTS TO TARGET"
                : "<b>TARGET REACHED!</b>";
        }
    }

    private void BuildMatchRulesLabel()
    {
        matchRulesText = CreateText("MatchRules", new Vector2(.5f, 1f), new Vector2(600f, 36f), 18f,
            TextAlignmentOptions.Center);
        var rect = (RectTransform)matchRulesText.transform;
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -265f);
        matchRulesText.color = PrismatchTheme.Mist;
        matchRulesText.fontStyle = FontStyles.Bold;
        matchRulesText.richText = true;
    }

    public void UpdateMatchRules(System.Collections.Generic.IReadOnlyList<MatchRule> rules)
    {
        if (matchRulesText == null) return;
        matchRulesText.text = rules == null || rules.Count == 0
            ? "NO MATCH TYPES AVAILABLE"
            : "MATCHES  •  " + string.Join("   •   ", rules.Select(rule =>
                $"<color=#FFF7E8>{rule.type.ToString().ToUpperInvariant()} {rule.minimumLength}+</color>"));
    }

    private void BuildBoardStats()
    {
        var boardArea = canvasTransform.Find("BoardArea") as RectTransform;
        if (boardArea == null) return;

        var row = new GameObject("BoardScoreStats", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        boardScoreStats = row;
        row.transform.SetParent(boardArea, false);
        var rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, 0f);
        rowRect.pivot = new Vector2(.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 82f);
        rowRect.sizeDelta = new Vector2(400f, 72f);

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        basePointsText = CreateStatChip("BasePoints", rowRect);
        multiplierText = CreateStatChip("Multiplier", rowRect);
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

    private void UpdateScoreStats(int basePoints, int multiplier)
    {
        if (basePointsText != null)
            basePointsText.text = $"<size=15><color=#AFC7E8>BASE POINTS</color></size>\n<size=27>{basePoints:N0}</size>";
        if (multiplierText != null)
            multiplierText.text = $"<size=15><color=#AFC7E8>MULTIPLIER</color></size>\n<size=27>\u00D7{Mathf.Max(1, multiplier):N0}</size>";
    }

    public void ShowBoardStats(bool shown)
    {
        if (boardScoreStats != null) boardScoreStats.SetActive(shown);
        if (scoreProgressRoot != null) scoreProgressRoot.SetActive(shown);
        if (matchRulesText != null) matchRulesText.gameObject.SetActive(shown);
    }

    private void StyleBottomBar(GameController controller)
    {
        var bar = canvasTransform.Find("BottomBar") as RectTransform;
        if (bar == null) return;
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 0f);
        bar.pivot = new Vector2(.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 76f);
        bar.sizeDelta = new Vector2(570f, 78f);

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
            StyleBottomAction(restart, 72f, null, "UI/Generated/nav-restart", PrismatchTheme.Orange);
            restartButtonBackground = restart.GetComponent<Image>();
        }
        SetRestartConfirmation(false);

        var powers = bar.Find("Powerups");
        if (powers != null)
            powersButtonText = StyleBottomAction(powers, 210f, "Powers", "UI/Generated/nav-powers", PrismatchTheme.Sky);

        var boss = bar.Find("Boss Icon");
        if (boss != null)
            bossButtonText = StyleBottomAction(boss, 210f, "Boss Rules", "UI/Generated/nav-boss-rules", PrismatchTheme.Coral);

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
        rect.anchoredPosition = new Vector2(18f, -28f);
        rect.sizeDelta = new Vector2(68f, 68f);

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
        rect.sizeDelta = new Vector2(width, 68f);
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
            : CreateText("ActionLabel", new Vector2(.5f, .5f), Vector2.zero, 22f,
                TextAlignmentOptions.MidlineLeft, action);
        text.gameObject.SetActive(true);
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(70f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);
        text.color = PrismatchTheme.Cream;
        text.fontStyle = FontStyles.Bold;
        text.richText = true;
        text.raycastTarget = false;
        text.text = $"{label}\n<size=15><color=#{ColorUtility.ToHtmlStringRGB(accent)}>VIEW DETAILS</color></size>";
        return text;
    }

    public void RefreshBottomBar(GameController controller)
    {
        if (controller == null) return;
        int powers = controller.PowerManager?.ActivePowers().Count ?? 0;
        int bosses = controller.GetTotalBossPowersInCurrentLevel();
        if (powersButtonText != null)
            powersButtonText.text = $"Powers  <color=#58A6F6>{powers}</color>\n<size=15><color=#AFC7E8>VIEW YOUR BUILD</color></size>";
        if (bossButtonText != null)
            bossButtonText.text = $"Boss Rules  <color=#FF6B6B>{bosses}</color>\n<size=15><color=#AFC7E8>{(bosses == 1 ? "VIEW LEVEL RULE" : "VIEW LEVEL RULES")}</color></size>";
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
        rect.sizeDelta = new Vector2(520f, 470f);
        var image = settingsPanel.GetComponent<Image>();
        PrismatchTheme.Round(image, new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .99f));
        PrismatchTheme.Shadow(image, new Color(0f, .02f, .08f, .65f), new Vector2(0f, -8f));

        var title = CreateText("Title", new Vector2(.5f, .82f), new Vector2(450f, 70f), 38f,
            TextAlignmentOptions.Center, rect);
        title.text = "Settings";
        title.color = PrismatchTheme.Cream;
        title.fontStyle = FontStyles.Bold;

        soundButtonText = CreateSettingsButton("Sound", new Vector2(0f, 45f), rect, controller.ToggleSound);
        UpdateSoundSetting(true);
        CreateSettingsButton("HowToPlay", new Vector2(0f, -45f), rect,
            () => ShowMessage("Swap two animals. Make one of the match types shown above the board."))
            .text = "How to Play";
        CreateSettingsButton("Resume", new Vector2(0f, -135f), rect, controller.ToggleSettings)
            .text = "Resume Game";
        settingsPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateSettingsButton(string name, Vector2 position, Transform parent,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(410f, 72f);
        var image = go.GetComponent<Image>();
        PrismatchTheme.Round(image, PrismatchTheme.RaisedNavy);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        go.AddComponent<PowerCardFeedback>();
        var label = CreateText("Label", new Vector2(.5f, .5f), new Vector2(390f, 64f), 23f,
            TextAlignmentOptions.Center, rect);
        label.color = PrismatchTheme.Cream;
        label.fontStyle = FontStyles.Bold;
        return label;
    }

    public void ShowSettings(bool shown, GameController controller)
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(shown);
        if (shown) settingsPanel.transform.SetAsLastSibling();
    }

    public void UpdateSoundSetting(bool enabled)
    {
        if (soundButtonText != null)
            soundButtonText.text = $"Sound Effects   <color=#{(enabled ? "42CC72" : "FF6B6B")}>{(enabled ? "ON" : "OFF")}</color>";
    }

    private void BuildEndPanel(GameController controller)
    {
        endPanel = new GameObject("RunEndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(canvasTransform, false);
        var rect = (RectTransform)endPanel.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        endPanel.GetComponent<Image>().color = new Color(.035f, .05f, .08f, .96f);
        endTitle = CreateText("RunEndTitle", new Vector2(.5f, .78f), new Vector2(640, 110), 52, TextAlignmentOptions.Center, rect);
        endSummary = CreateText("RunEndSummary", new Vector2(.5f, .53f), new Vector2(640, 260), 24, TextAlignmentOptions.Center, rect);

        var btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRow.transform.SetParent(rect, false);
        var rowRt = (RectTransform)btnRow.transform;
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.22f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(560, 70);
        var hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        retryButton = CreateButton("Retry Round", new Vector2(0.5f, 0.5f), rowRt, controller.OnRestartRound);
        CreateButton("New Run", new Vector2(0.5f, 0.5f), rowRt, controller.NewGameButton);
        CreateButton("Level Select", new Vector2(0.5f, 0.5f), rowRt, () => SceneManager.LoadScene("Level Select"));
        endPanel.SetActive(false);
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
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(165, 60);
        go.GetComponent<Image>().color = new Color(.18f, .46f, .72f, 1);
        var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = CreateText("Label", new Vector2(.5f, .5f), new Vector2(155, 55), 22, TextAlignmentOptions.Center, rect);
        text.text = label; return button;
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"<size=16><color=#AFC7E8>LEVEL {latestLevel}</color></size>\n<size=15>SCORE</size>\n<size=30>{score:N0}</size>";
        UpdateScoreProgress(score);
        int delta = score - previousScore;
        if (delta > 0) ShowFloatingScore(delta);
        previousScore = score;
    }
    public void UpdateTarget(int target)
    {
        latestTarget = target;
        UpdateScoreStats(0, 1);
        UpdateScoreProgress(previousScore);
        if (targetText != null)
            targetText.text = $"<size=16><color=#AFC7E8>TARGET</color></size>\n<size=30>{target:N0}</size>";
    }
    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        int total = Mathf.CeilToInt(Mathf.Max(0, time));
        timerText.text = $"<size=16><color=#AFC7E8>TIMER</color></size>\n<size=30>{total / 60:0}:{total % 60:00}</size>";
        timerText.color = total <= 10 ? new Color(1, .3f, .25f) : PrismatchTheme.Cream;
    }
    public void UpdateRound(int round)
    {
        latestLevel = GameState.Instance?.CurrentLevel?.GetLevelNumber ?? Mathf.Max(1, round);
        UpdateScore(previousScore);
    }

    public void ShowScoreBreakdown(ScoreResult result, System.Collections.Generic.List<Tile> tiles)
    {
        if (result == null) return;
        UpdateScoreStats(result.BasePoints, result.Multiplier);
    }

    public void ShowMessage(string message, float duration = 4f)
    {
        if (messageText == null) { Debug.Log(message); return; }
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(Message(message, duration));
    }
    private IEnumerator Message(string message, float duration)
    {
        messageText.text = message; messageText.gameObject.SetActive(true); messageText.transform.SetAsLastSibling();
        yield return new WaitForSecondsRealtime(duration);
        messageText.gameObject.SetActive(false);
    }
    public void ShowPaused(bool shown) { if (pauseText != null) pauseText.gameObject.SetActive(shown); }
    public void HideRunEnd() { if (endPanel != null) endPanel.SetActive(false); }
    public void ShowRunEnd(bool won, RunSummary summary, GameController controller)
    {
        if (endPanel == null) return;
        endPanel.SetActive(true); endPanel.transform.SetAsLastSibling();
        endTitle.text = won ? "LEVEL COMPLETE" : "TIME UP";
        endTitle.color = won ? new Color(.45f, 1, .55f) : new Color(1, .4f, .35f);
        var last = summary.rounds.LastOrDefault();
        endSummary.text = $"Rounds cleared: {summary.CompletedRounds}/{controller.maxRounds}\n" +
            $"Run score: {summary.TotalScore:N0}   Â·   Matches: {summary.Matches}   Â·   Cascades: {summary.Cascades}\n" +
            $"Shuffles: {summary.Shuffles}   Â·   Zero-point clears: {summary.ZeroPointMatches}" +
            (last == null ? "" : $"\nLast round pace: {last.scorePerSecond:F1} points/sec");
        retryButton.gameObject.SetActive(!won);
        retryButton.interactable = controller.CanRetry;
    }

    public void ShowFloatingScore(int amount)
    {
        if (floatingTextPrefab == null || scoreAnchorTransform == null || canvasTransform == null || latestTarget <= 0) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, scoreAnchorTransform.position, null, out var anchoredPos);
        var instance = Instantiate(floatingTextPrefab, canvasTransform);
        instance.GetComponent<RectTransform>().anchoredPosition = anchoredPos + new Vector2(75, -25);
        float percent = (float)amount / latestTarget;
        instance.Animate($"+{amount:N0}", new Vector2(Random.Range(-.5f, .5f), Random.Range(.8f, 1.2f)).normalized,
            percent >= .25f ? new Color(.6f, .4f, 1) : percent >= .15f ? Color.green : Color.white,
            percent >= .25f ? 60 : percent >= .15f ? 48 : 36);
    }
}
