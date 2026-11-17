using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelSelectUI : MonoBehaviour
{
    // Retain the scene reference to locate its canvas and font.
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText, roundsText;
    [SerializeField] private Button nextButton, prevButton;
    [Header("Free Play Defaults / Editor Preview")]
    [SerializeField] private MatchType matching = MatchType.Snake;
    [SerializeField, Range(2, 10)] private int minimumMatchLength = 4;
    private bool customMatchLength;
    [SerializeField] private ReplacementStrategy replacement;
    [SerializeField] private GoalType goal;
    [SerializeField] private FreePlayMode mode;
    [SerializeField, Range(3, 10)] private int width = 7, height = 7;
    [SerializeField, Range(1, 100)] private int levels = 10;
    [SerializeField, Range(0, 1000)] private int growthPercent = 25;
    [SerializeField, Min(1)] private int points = 400, blocks = 100;
    [SerializeField, Range(10, 3600)] private int seconds = 60;
    [SerializeField, Range(1, 999)] private int moves = 20;
    [Header("Editor Preview")]
    [SerializeField, Range(0, 1), Tooltip("1 shows the top; 0 shows the bottom. Does not affect Play mode.")]
    private float previewScroll = 1f;
    private bool showFreePlay;
    private int campaignIndex = -1;
    private const string SelectedCampaignLevelKey = ProgressStore.SelectedCampaignLevelKey;
    private bool editingCampaign, editingNotes;
    private int campaignPage = -1;
    private float lastCampaignPageTime;
    private const string GeneratedRootName = "Free Play Safe Area";
    private RectTransform panel, safeArea;
    private UnityEngine.Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private TMP_FontAsset font;
    private TextMeshProUGUI summary, budgetLabel;
    private TMP_InputField targetInput, budgetInput, matchLengthInput;
    private TextMeshProUGUI matchLengthHint, replacementHint, growthHeading, growthHint;

    private void Start()
    {
        if (GameState.Instance == null) new GameObject("GameState").AddComponent<GameState>();
        if (GameState.Instance.CurrentLevel is FreePlayConfig saved)
        {
            customMatchLength = true;
            levels = saved.Levels; growthPercent = saved.GrowthPercent;
            replacement = saved.Replacement; matching = saved.Matching; minimumMatchLength = saved.MinimumMatchLength; goal = saved.Goal; mode = saved.Mode;
            width = saved.Width; height = saved.Height; seconds = saved.Seconds; moves = saved.Moves;
            if (goal == GoalType.Score) points = saved.Target; else blocks = saved.Target;
        }
        BuildUI(true);
    }

    public void ApplyEditorPreview(bool rebuild = false)
    {
        if (Application.isPlaying || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        BuildUI(rebuild);
    }

    private void BuildUI(bool rebuild)
    {
        var canvas = levelText != null ? levelText.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
            canvas = Array.Find(Resources.FindObjectsOfTypeAll<Canvas>(), candidate =>
                candidate.gameObject.scene == gameObject.scene && candidate.isRootCanvas);
        if (canvas == null) return;
        var existing = canvas.transform.Find(GeneratedRootName) as RectTransform;
        if (existing != null && !rebuild)
        {
            safeArea = existing;
            ApplySafeArea();
            return;
        }
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
        width = Mathf.Clamp(width, 3, 10); height = Mathf.Clamp(height, 3, 10);
        levels = Mathf.Clamp(levels, 1, 100); growthPercent = Mathf.Clamp(growthPercent, 0, 1000);
        points = Mathf.Clamp(points, 1, 1000000); blocks = Mathf.Clamp(blocks, 1, 1000000);
        seconds = Mathf.Clamp(seconds, 10, 3600); moves = Mathf.Clamp(moves, 1, 999);
        font = levelText != null ? levelText.font : TMP_Settings.defaultFontAsset;
        foreach (Transform child in canvas.transform) child.gameObject.SetActive(false);
        PrismatchTheme.ApplyBackdrop(canvas.transform as RectTransform);
        canvas.transform.Find("PrismatchBackdrop")?.gameObject.SetActive(true);
        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        // Scale mobile to its usable width and desktop to a 1080p reference layout.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        safeArea = Rect(GeneratedRootName, canvas.transform, Vector2.zero, Vector2.zero);
        ApplySafeArea();
        var viewport = Rect("Setup Scroll", safeArea, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12, showFreePlay ? 124 : 12); viewport.offsetMax = new Vector2(-12, -108);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36;
        panel = Rect("Free Play Content", viewport, Vector2.zero, new Vector2(560, 2252));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1f);
        scroll.content = panel;
        BuildPickerTabs();
        if (!showFreePlay)
        {
            scroll.vertical = false;
            BuildCampaignPicker();
            var paging = viewport.gameObject.AddComponent<EventTrigger>();
            var wheel = new EventTrigger.Entry { eventID = EventTriggerType.Scroll };
            wheel.callback.AddListener(data => {
                if (editingCampaign || editingNotes) return;
                float delta = ((PointerEventData)data).scrollDelta.y;
                if (Mathf.Abs(delta) > .01f) ChangeCampaignPage(delta < 0 ? 1 : -1);
            });
            paging.triggers.Add(wheel);
            paging.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag });
            paging.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.Drag });
            var swipe = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            swipe.callback.AddListener(data => {
                var pointer = (PointerEventData)data;
                if (editingCampaign || editingNotes) return;
                float delta = pointer.position.x - pointer.pressPosition.x;
                if (Mathf.Abs(delta) > 60) ChangeCampaignPage(delta < 0 ? 1 : -1);
            });
            paging.triggers.Add(swipe);
            Canvas.ForceUpdateCanvases();
            FitCampaignPage();
            scroll.verticalNormalizedPosition = 1f;
            return;
        }

        SectionCard("Matching card", -136, -608, PrismatchTheme.Sky);
        SectionCard("Grid card", -620, -840, PrismatchTheme.Sky);
        SectionCard("Refill card", -850, -1238, PrismatchTheme.Sky);
        SectionCard("Goal card", -1254, -1492, PrismatchTheme.Green);
        SectionCard("Growth card", -1504, -1860, PrismatchTheme.Orange);
        SectionCard("Mode card", -1874, -2100, PrismatchTheme.Coral);
        SectionCard("Summary card", -2116, -2246, PrismatchTheme.Green);
        var title = Label("Free play", -46, 46);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 1f;
        HeroAnimal(ItemType.Bear, -218);
        HeroAnimal(ItemType.Duck, 218);
        var subtitle = Label("Your board. Your rules.", -102, 25);
        subtitle.color = PrismatchTheme.Mist;
        Heading("01   MATCHING", -164, PrismatchTheme.Sky);
        ChoiceRow(new[] { "Horizontal", "Vertical", "Snake / connected" }, -230, (int)matching, i => {
            matching = (MatchType)i;
            if (!customMatchLength) minimumMatchLength = MatchRule.DefaultMinimum(matching);
        });
        matchLengthInput = Number("Match\nlength", 0, -516, minimumMatchLength, 2, 10,
            v => { minimumMatchLength = v; customMatchLength = true; });
        matchLengthHint = Label("", -580, 23);
        Heading("02   BOARD SIZE", -644, PrismatchTheme.Sky);
        Number("Columns", 0, -700, width, 3, 10, v => width = v);
        Number("Rows", 0, -792, height, 3, 10, v => height = v);
        Heading("03   BLOCK REFILL", -878, PrismatchTheme.Sky);
        ReplacementChoices(-932);
        replacementHint = Label("", -1194, 23);
        replacementHint.rectTransform.sizeDelta = new Vector2(530, 64);
        Heading("04   YOUR GOAL", -1290, PrismatchTheme.Green);
        ChoiceRow(new[] { "Points", "Blocks matched" }, -1352, (int)goal, i => {
            goal = (GoalType)i; targetInput.SetTextWithoutNotify((goal == GoalType.Score ? points : blocks).ToString());
        });
        targetInput = Number("Starting\ntarget", 0, -1440, goal == GoalType.Score ? points : blocks, 1, 1000000,
            v => { if (goal == GoalType.Score) points = v; else blocks = v; });
        growthHeading = Heading("05   POINT GROWTH", -1536, PrismatchTheme.Orange);
        Number("Rounds", 0, -1600, levels, 1, 100, v => levels = v);
        Number("Growth\nrate (%)", 0, -1692, growthPercent, 0, 1000, v => growthPercent = v);
        growthHint = Label("", -1790, 23);
        growthHint.rectTransform.sizeDelta = new Vector2(530, 112);
        Heading("06   GAME MODE", -1902, PrismatchTheme.Coral);
        ChoiceRow(new[] { "Timed", "Limited moves" }, -1960, (int)mode, i => {
            mode = (FreePlayMode)i;
            budgetLabel.text = mode == FreePlayMode.Timed ? "Seconds" : "Moves";
            budgetInput.SetTextWithoutNotify((mode == FreePlayMode.Timed ? seconds : moves).ToString());
        });
        budgetInput = Number("Budget", 0, -2048, mode == FreePlayMode.Timed ? seconds : moves, 1, 3600,
            v => { if (mode == FreePlayMode.Timed) seconds = Mathf.Clamp(v, 10, 3600); else moves = Mathf.Clamp(v, 1, 999);
                budgetInput.SetTextWithoutNotify((mode == FreePlayMode.Timed ? seconds : moves).ToString()); });
        budgetLabel = panel.Find("Budget label").GetComponent<TextMeshProUGUI>();
        budgetLabel.text = mode == FreePlayMode.Timed ? "Seconds" : "Moves";
        summary = Label("", -2166, 28);
        summary.rectTransform.sizeDelta = new Vector2(520, 136);
        var footer = Rect("Play dock", safeArea, Vector2.zero, Vector2.zero);
        footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0);
        footer.pivot = new Vector2(.5f, 0);
        footer.sizeDelta = new Vector2(0, 120);
        PrismatchTheme.Round(footer.gameObject.AddComponent<Image>(), PrismatchTheme.Midnight);
        var play = MakeButton("Let's play", Vector2.zero, new Vector2(520, 96), PlayLevel);
        play.transform.SetParent(safeArea, false);
        var playRect = (RectTransform)play.transform;
        playRect.anchorMin = playRect.anchorMax = new Vector2(.5f, 0f);
        playRect.anchoredPosition = new Vector2(0, 60);
        play.GetComponent<Image>().color = PrismatchTheme.Green;
        play.GetComponentInChildren<TextMeshProUGUI>().color = PrismatchTheme.Navy;
        foreach (var hint in new[] { matchLengthHint, replacementHint, growthHint })
            hint.color = PrismatchTheme.Mist;
        summary.color = PrismatchTheme.Cream;
        summary.fontSize = 24;
        Refresh();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = Application.isPlaying ? 1f : previewScroll;
    }

    private void BuildPickerTabs()
    {
        for (int i = 0; i < 2; i++)
        {
            bool free = i == 1;
            var button = MakeButton(free ? "Free Play" : "Levels", Vector2.zero, new Vector2(260, 76), () =>
            {
                editingCampaign = editingNotes = false;
                showFreePlay = free;
                BuildUI(true);
            });
            var rect = (RectTransform)button.transform;
            rect.SetParent(safeArea, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(free ? 136 : -136, -52);
            StyleChoice(button.GetComponent<Image>(), showFreePlay == free);
        }
    }

    private void BuildCampaignPicker()
    {
        int total = LevelLibrary.TotalLevels, unlocked = ProgressStore.UnlockedLevel();
        campaignIndex = Mathf.Clamp(campaignIndex < 0
            ? (Application.isPlaying ? PlayerPrefs.GetInt(SelectedCampaignLevelKey, 1) - 1 : 0)
            : campaignIndex, 0, unlocked - 1);
        campaignPage = Mathf.Clamp(campaignPage < 0 ? CampaignCalendar.PageForLevel(campaignIndex,total) : campaignPage,
            0, CampaignCalendar.PageCount(total)-1);
        panel.sizeDelta = new Vector2(560, editingNotes ? 820 : editingCampaign ? 790 : 960);
        if (editingNotes) { BuildLevelNotesEditor(); return; }
        if (editingCampaign) { BuildCampaignScalingEditor(); return; }
        var title = Label("Choose a level", -28, 36); title.fontStyle = FontStyles.Bold;
        int first = campaignPage * CampaignCalendar.PageSize;
        int last = Mathf.Min(first + CampaignCalendar.PageSize,total);
        var range = Label($"LEVELS {first+1} - {last}", -86, 23);
        range.color = PrismatchTheme.Mist; range.fontStyle = FontStyles.Bold;
        range.rectTransform.sizeDelta = new Vector2(340,42);
        var previous = MakeButton("<",new Vector2(-238,-86),new Vector2(64,50),()=>ChangeCampaignPage(-1));
        previous.interactable = campaignPage > 0;
        var next = MakeButton(">",new Vector2(238,-86),new Vector2(64,50),()=>ChangeCampaignPage(1));
        next.interactable = campaignPage+1 < CampaignCalendar.PageCount(total);
        Label($"{unlocked} OF {total} UNLOCKED",-134,17).color = PrismatchTheme.Mist;
        for (int index=first;index<last;index++)
        {
            int selectedIndex=index, offset=index-first;
            bool available=index<unlocked, completed=ProgressStore.IsCompleted(index+1), selected=index==campaignIndex;
            var cell=MakeButton((index+1).ToString(),new Vector2((offset%5-2)*110,-206-(offset/5)*100),new Vector2(98,88),
                ()=>SelectCampaignLevel(selectedIndex));
            cell.name="Level cell "+(index+1);
            cell.interactable=available;
            var image=cell.GetComponent<Image>();
            Color background=selected?PrismatchTheme.Orange:completed?Color.Lerp(PrismatchTheme.Navy,PrismatchTheme.Green,.28f):PrismatchTheme.RaisedNavy;
            PrismatchTheme.Round(image,background);
            if(available)PrismatchTheme.Shadow(image,new Color(0,0,0,.25f),new Vector2(0,-3));
            var colors=cell.colors;colors.disabledColor=new Color(.55f,.59f,.67f,1);colors.selectedColor=Color.white;cell.colors=colors;
            var number=cell.GetComponentInChildren<TextMeshProUGUI>();
            number.text=(index+1).ToString();number.fontSize=34;number.fontStyle=FontStyles.Bold;
            number.color=selected?PrismatchTheme.Navy:available?PrismatchTheme.Cream:PrismatchTheme.Mist;
            number.rectTransform.anchoredPosition=new Vector2(0,12);number.rectTransform.sizeDelta=new Vector2(90,44);
            var state=CardLabel(cell.transform,completed?"BEST "+ProgressStore.HighestCompletedDifficulty(index+1):available?(index==0?"LEARN":"READY"):"LOCKED",new Vector2(0,-25),new Vector2(90,22),15,
                selected?PrismatchTheme.Navy:completed?PrismatchTheme.Green:available?PrismatchTheme.Sky:PrismatchTheme.Mist);
            state.alignment=TextAlignmentOptions.Center;
            state.enableAutoSizing=true;state.fontSizeMin=9;state.fontSizeMax=15;
            if(completed)state.fontStyle=FontStyles.Bold;
        }
        var selectedLevel = CampaignWithSavedScaling((CampaignLevel)LevelLibrary.AllLevels[campaignIndex]);
        int selectedNumber = selectedLevel.GetLevelNumber;
        int unlockedDifficulty = ProgressStore.UnlockedDifficulty(selectedNumber);
        string difficultyKey = $"Prismatch.Level.{selectedNumber}.SelectedDifficulty";
        int difficulty = Mathf.Clamp(PlayerPrefs.GetInt(difficultyKey, 1), 1, unlockedDifficulty);
        selectedLevel = selectedLevel.WithDifficulty(difficulty);
        BuildSelectedCampaignCard(selectedLevel);
        Action<int> selectDifficulty = value => {
            PlayerPrefs.SetInt(difficultyKey,Mathf.Clamp(value,1,unlockedDifficulty));
            PlayerPrefs.Save();BuildUI(true);
        };
        var difficultyInput = Number("Difficulty",0,-860,difficulty,1,unlockedDifficulty,selectDifficulty);
        difficultyInput.characterLimit = 10;
        difficultyInput.GetComponent<RectTransform>().sizeDelta = new Vector2(190,80);
        var easier = MakeButton("-",new Vector2(-50,-860),new Vector2(60,72),()=>selectDifficulty(difficulty-1));
        easier.interactable=difficulty>1;
        var harder = MakeButton("+",new Vector2(220,-860),new Vector2(60,72),()=>selectDifficulty(ProgressStore.NextDifficulty(difficulty)));
        harder.interactable=difficulty<unlockedDifficulty;
        int bestDifficulty=ProgressStore.HighestCompletedDifficulty(selectedNumber);
        var best=Label(bestDifficulty>0?$"BEST DIFFICULTY BEATEN: {bestDifficulty}":"NO DIFFICULTIES BEATEN YET",-914,19);
        best.color=bestDifficulty>0?PrismatchTheme.Green:PrismatchTheme.Mist;best.fontStyle=FontStyles.Bold;
        Label($"Unlocked: 1-{unlockedDifficulty}",-941,17).color=PrismatchTheme.Mist;
    }

    private void SelectCampaignLevel(int index)
    {
        int selection=Mathf.Clamp(index,0,ProgressStore.UnlockedLevel()-1);
        int page=CampaignCalendar.PageForLevel(selection,LevelLibrary.TotalLevels);
        if(selection==campaignIndex && page==campaignPage)return;
        campaignIndex=selection;campaignPage=page;
        RememberCampaignSelection();BuildUI(true);
    }

    private void BuildSelectedCampaignCard(CampaignLevel level)
    {
        var card=Rect("Selected level",panel,new Vector2(0,-688),new Vector2(552,232));
        PrismatchTheme.Round(card.gameObject.AddComponent<Image>(),PrismatchTheme.Card);
        var portrait=Rect("Level portrait",card,new Vector2(-210,38),new Vector2(82,82)).gameObject.AddComponent<Image>();
        portrait.raycastTarget=false;
        var bosses=level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds);
        if(!level.IsTutorial && bosses.Count>0)PowerUpIconLibrary.Apply(portrait,bosses[bosses.Count-1]);
        else {
            var animals=ItemDatabase.items??Resources.LoadAll<Item>("Items/");
            var duck=Array.Find(animals,item=>item!=null&&item.ItemType==ItemType.Duck);
            portrait.sprite=duck?.sprite;portrait.preserveAspect=true;
        }
        bool completed=ProgressStore.IsDifficultyCompleted(level.GetLevelNumber,level.Difficulty);
        CardLabel(card,$"LEVEL {level.GetLevelNumber:00}"+(completed?"  /  BEATEN":""),new Vector2(48,94),new Vector2(350,22),17,PrismatchTheme.RaisedNavy);
        var name=CardLabel(card,level.Name,new Vector2(48,46),new Vector2(350,64),32,PrismatchTheme.Navy);
        name.fontStyle=FontStyles.Bold;name.enableAutoSizing=true;name.fontSizeMin=24;name.fontSizeMax=32;
        string goalLabel=level.Goal==GoalType.Score?"Points":"Blocks";
        string modeLabel=level.IsTutorial?"Untimed tutorial":level.Mode==FreePlayMode.Timed?"Timed":"Limited moves";
        CardLabel(card,goalLabel+" / "+modeLabel,new Vector2(48,1),new Vector2(350,24),19,PrismatchTheme.RaisedNavy);
        CardLabel(card,level.IsTutorial?"Learn at your own pace":"Difficulty "+level.Difficulty+" / Target "+level.GetTargetScore(1).ToString("N0"),
            new Vector2(48,-24),new Vector2(350,24),18,PrismatchTheme.RaisedNavy);
        var play=MakeButton(level.IsTutorial?"Start tutorial":completed?"Play again":"Play level",Vector2.zero,new Vector2(350,64),()=>{
            if(!Application.isPlaying||GameState.Instance==null||level.GetLevelNumber!=campaignIndex+1)return;
            if(level.Difficulty>ProgressStore.UnlockedDifficulty(level.GetLevelNumber))return;
            RememberCampaignSelection();
            if(level.IsTutorial){campaignTutorial=true;ShowTutorial(0);return;}
            GameState.Instance.CurrentLevel=level.CloneNew();SceneManager.LoadScene("prismatch");
        });
        var playRect=(RectTransform)play.transform;playRect.SetParent(card,false);
        playRect.anchorMin=playRect.anchorMax=new Vector2(.5f,.5f);playRect.anchoredPosition=new Vector2(52,-73);
        PrismatchTheme.Round(play.GetComponent<Image>(),PrismatchTheme.Orange);
        var playText=play.GetComponentInChildren<TextMeshProUGUI>();playText.color=PrismatchTheme.Navy;playText.fontStyle=FontStyles.Bold;
        var edit=MakeButton("Edit",Vector2.zero,new Vector2(80,64),()=>{editingCampaign=true;BuildUI(true);});
        var editRect=(RectTransform)edit.transform;editRect.SetParent(card,false);
        editRect.anchorMin=editRect.anchorMax=new Vector2(.5f,.5f);editRect.anchoredPosition=new Vector2(-212,-73);
        var notes=MakeButton("Notes",Vector2.zero,new Vector2(94,42),()=>{editingNotes=true;BuildUI(true);});
        var notesRect=(RectTransform)notes.transform;notesRect.SetParent(card,false);
        notesRect.anchorMin=notesRect.anchorMax=new Vector2(.5f,.5f);notesRect.anchoredPosition=new Vector2(-212,-23);
        notes.GetComponentInChildren<TextMeshProUGUI>().fontSize=22;
        edit.interactable=!level.IsTutorial;
        edit.GetComponentInChildren<TextMeshProUGUI>().fontSize=23;
    }

    private static string NotesKey(int number) => "Prismatch.LevelNotes."+number;

    private static string ExportLevelNotes()
    {
        var entries=new Newtonsoft.Json.Linq.JArray();
        foreach(CampaignLevel level in LevelLibrary.AllLevels) {
            string note=PlayerPrefs.GetString(NotesKey(level.GetLevelNumber),"");
            if(string.IsNullOrWhiteSpace(note))continue;
            entries.Add(new Newtonsoft.Json.Linq.JObject {
                ["number"]=level.GetLevelNumber,["name"]=level.Name,["notes"]=note
            });
        }
        string json=new Newtonsoft.Json.Linq.JObject {
            ["schema"]="prismatch-level-notes",["version"]=1,
            ["exportedAt"]=DateTime.UtcNow.ToString("o"),["levels"]=entries
        }.ToString();
#if UNITY_WEBGL && !UNITY_EDITOR
        PrismatchAnalyticsDownload("prismatch-level-notes.json",json);
        return "Downloaded all level notes. Share the JSON file for review.";
#else
        string path=System.IO.Path.Combine(Application.persistentDataPath,"prismatch-level-notes.json");
        System.IO.File.WriteAllText(path,json);GUIUtility.systemCopyBuffer=path;
        return "Exported all notes; file path copied.\nShare prismatch-level-notes.json for review.";
#endif
    }

    private void BuildLevelNotesEditor()
    {
        var level=(CampaignLevel)LevelLibrary.AllLevels[campaignIndex];
        int number=level.GetLevelNumber;
        Label("Level "+number+": "+level.Name,-55,30);
        Label("Playtest notes",-125,32).fontStyle=FontStyles.Bold;
        var hint=Label("What worked? What should change?\nInclude the difficulty or round if helpful.",-205,23);
        hint.rectTransform.sizeDelta=new Vector2(520,80);
        var rect=Rect("Level notes input",panel,new Vector2(0,-425),new Vector2(520,320));
        rect.gameObject.SetActive(false);
        var background=rect.gameObject.AddComponent<Image>();PrismatchTheme.Round(background,PrismatchTheme.RaisedNavy);
        var viewport=Rect("Text area",rect,Vector2.zero,Vector2.zero);
        viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;
        viewport.offsetMin=new Vector2(16,12);viewport.offsetMax=new Vector2(-16,-12);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text=Text(Rect("Notes text",viewport,Vector2.zero,Vector2.zero),"",25);
        text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
        text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
        text.richText=false;text.alignment=TextAlignmentOptions.TopLeft;
        var input=rect.gameObject.AddComponent<TMP_InputField>();
        input.textViewport=viewport;input.textComponent=text;input.targetGraphic=background;
        input.contentType=TMP_InputField.ContentType.Standard;input.lineType=TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit=4000;input.onFocusSelectAll=false;input.shouldHideSoftKeyboard=false;
        input.shouldHideMobileInput=false;input.customCaretColor=true;input.caretColor=PrismatchTheme.Cream;
        input.selectionColor=new Color(.35f,.65f,1f,.5f);
        input.SetTextWithoutNotify(PlayerPrefs.GetString(NotesKey(number),""));
        var status=Label("Notes save automatically on this device.",-620,21);
        status.rectTransform.sizeDelta=new Vector2(520,64);
        input.onValueChanged.AddListener(value=>{
            PlayerPrefs.SetString(NotesKey(number),value);PlayerPrefs.Save();
            status.text="Saved on this device.";
        });
        rect.gameObject.SetActive(true);
        MakeButton("Export all level notes",new Vector2(0,-690),new Vector2(488,58),()=>{
            try{status.text=ExportLevelNotes();}
            catch(Exception error){status.text="Notes are saved. Export failed; try again.";Debug.LogWarning(error.Message);}
        });
        MakeButton("Done",new Vector2(0,-765),new Vector2(488,58),()=>{editingNotes=false;BuildUI(true);});
    }

    private static string ScalingKey(int number) => "Prismatch.LevelScaling." + number + ".";

    private static CampaignLevel CampaignWithSavedScaling(CampaignLevel level)
    {
        string key = ScalingKey(level.GetLevelNumber);
        return level.WithScaling(PlayerPrefs.GetInt(key + "Target", level.Settings.Target),
            PlayerPrefs.GetInt(key + "Growth", level.Settings.GrowthPercent));
    }

    private void BuildCampaignScalingEditor()
    {
        var original = (CampaignLevel)LevelLibrary.AllLevels[campaignIndex];
        var level = CampaignWithSavedScaling(original);
        int target = level.Settings.Target, growth = level.Settings.GrowthPercent;
        Label("Edit: " + level.Name, -50, 32);
        Label("Scaling", -130, 30);
        Label("Changes are saved on this device.", -190, 23);
        var preview = Label("", -465, 24);
        preview.rectTransform.sizeDelta = new Vector2(530, 100);
        Action updatePreview = () => {
            preview.text = "Round targets\n" + string.Join(" / ", System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Range(1, Math.Min(3, level.GetTotalRounds)),
                round => level.WithScaling(target, growth).GetTargetScore(round).ToString("N0"))) +
                "\nFinal: " + level.WithScaling(target, growth).GetTargetScore(level.GetTotalRounds).ToString("N0") +
                (level.BossTargetMultiplier > 1 ? " (boss x" + level.BossTargetMultiplier + ")" : "");
        };
        Number("Starting\ntarget", 0, -280, target, 1, 1000000, value => { target = value; updatePreview(); });
        Number("Growth\nrate (%)", 0, -370, growth, 0, 1000, value => { growth = value; updatePreview(); });
        updatePreview();
        MakeButton("Save scaling", new Vector2(0, -550), new Vector2(488, 58), () => {
            string key = ScalingKey(original.GetLevelNumber);
            PlayerPrefs.SetInt(key + "Target", target); PlayerPrefs.SetInt(key + "Growth", growth); PlayerPrefs.Save();
            editingCampaign = false; BuildUI(true);
        });
        MakeButton("Save & export changes", new Vector2(0, -615), new Vector2(488, 58), () => {
            string key = ScalingKey(original.GetLevelNumber);
            PlayerPrefs.SetInt(key + "Target", target); PlayerPrefs.SetInt(key + "Growth", growth); PlayerPrefs.Save();
            preview.text = ExportScalingChanges();
        });
        MakeButton("Reset to level defaults", new Vector2(0, -670), new Vector2(488, 58), () => {
            string key = ScalingKey(original.GetLevelNumber);
            PlayerPrefs.DeleteKey(key + "Target"); PlayerPrefs.DeleteKey(key + "Growth"); PlayerPrefs.Save();
            BuildUI(true);
        });
        MakeButton("Cancel", new Vector2(0, -745), new Vector2(488, 58), () => { editingCampaign = false; BuildUI(true); });
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void PrismatchAnalyticsDownload(string filename, string text);
#endif
    private static string ExportScalingChanges()
    {
        var levels = new Newtonsoft.Json.Linq.JArray();
        foreach (CampaignLevel original in LevelLibrary.AllLevels)
        {
            string key = ScalingKey(original.GetLevelNumber);
            if (!PlayerPrefs.HasKey(key + "Target") && !PlayerPrefs.HasKey(key + "Growth")) continue;
            var level = CampaignWithSavedScaling(original);
            levels.Add(new Newtonsoft.Json.Linq.JObject {
                ["id"] = (string)SharedGameContent.Level(original.GetLevelNumber)["id"],
                ["target"] = level.Settings.Target, ["growth"] = level.Settings.GrowthPercent
            });
        }
        string json = new Newtonsoft.Json.Linq.JObject {
            ["schema"] = "prismatch-level-scaling", ["version"] = 1, ["levels"] = levels
        }.ToString();
#if UNITY_WEBGL && !UNITY_EDITOR
        PrismatchAnalyticsDownload("prismatch-level-scaling.json", json);
        return "Downloaded level changes.\nImport the JSON in the design editor\nto save them permanently to the game.";
#else
        string path = System.IO.Path.Combine(Application.persistentDataPath, "prismatch-level-scaling.json");
        System.IO.File.WriteAllText(path, json);
        GUIUtility.systemCopyBuffer = path;
        return "Exported changes; file path copied.\nImport JSON in the design editor\nto save permanently to the game.";
#endif
    }

    public static string CampaignFinalRoundLabel(CampaignLevel level)
    {
        if (level.IsTutorial) return "Make your first match of 4";
        var bosses = level.GetBossPowersMap().GetBossPowersForRound(level.GetTotalRounds);
        return bosses.Count > 0 ? $"FINAL ROUND: {bosses[bosses.Count - 1].Name}" : "COMPLETE ALL ROUNDS TO WIN";
    }

    private void CampaignPlayButton(Button card, string caption)
    {
        var action = Rect("Play action", card.transform, new Vector2(0, -197), new Vector2(488, 72));
        var image = action.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(image, PrismatchTheme.Orange);
        PrismatchTheme.Shadow(image, new Color(0, 0, 0, .16f), new Vector2(0, -4));
        var button = action.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => card.onClick.Invoke());
        var label = CardLabel(action, caption, Vector2.zero, new Vector2(464, 62), 30, PrismatchTheme.Navy);
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
    }

    private void RememberCampaignSelection()
    {
        if (!Application.isPlaying) return;
        PlayerPrefs.SetInt(SelectedCampaignLevelKey, campaignIndex + 1);
        PlayerPrefs.Save();
    }

    private void ChangeCampaignPage(int direction)
    {
        if(editingCampaign || editingNotes || Time.unscaledTime-lastCampaignPageTime<.25f)return;
        int next=Mathf.Clamp(campaignPage+direction,0,CampaignCalendar.PageCount(LevelLibrary.TotalLevels)-1);
        if(next==campaignPage)return;
        lastCampaignPageTime=Time.unscaledTime;campaignPage=next;BuildUI(true);
    }

    private void FitCampaignPage()
    {
        if (showFreePlay || panel == null || panel.parent is not RectTransform viewport) return;
        panel.localScale = Vector3.one * Mathf.Min(1f, Mathf.Max(1f, viewport.rect.height) / Mathf.Max(1f, panel.sizeDelta.y),
            Mathf.Max(1f, viewport.rect.width) / 560f);
    }


    private GameObject tutorial;
    private bool campaignTutorial;
    private const int TutorialPageCount = 6;
    private const int TutorialBossPage = TutorialPageCount - 1;
    private void DrawMatchDiagram(Transform parent, Vector2 position, MatchType type, float cell)
    {
        cell = Mathf.Min(cell,32);
        var root = Rect("Match diagram", parent, position, new Vector2(cell*4,cell*4));
        var animals = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        var duck = Array.Find(animals, x => x != null && x.ItemType == ItemType.Duck);
        for (int y=0;y<4;y++) for (int x=0;x<4;x++)
        {
            bool active = type == MatchType.Horizontal ? y==1 : type == MatchType.Vertical ? x==1 :
                (y==1 && (x==0 || x==1)) || (y==2 && (x==1 || x==2));
            var tile = Rect("Cell", root, new Vector2((x-1.5f)*cell,(1.5f-y)*cell), Vector2.one*(cell-3));
            var bg = tile.gameObject.AddComponent<Image>();
            PrismatchTheme.Round(bg, active ? PrismatchTheme.Green : new Color(.3f,.4f,.55f,.25f)); bg.raycastTarget=false;
            if (active && duck != null)
            {
                var art=Rect("Animal",tile,Vector2.zero,Vector2.one*(cell-8)).gameObject.AddComponent<Image>();
                art.sprite=duck.sprite; art.preserveAspect=true; art.raycastTarget=false;
            }
        }
    }

    private void VisualBadge(Transform parent, Vector2 position, string kind, string value)
    {
        var badge=Rect(kind,parent,position,new Vector2(290,62));
        var bg=badge.gameObject.AddComponent<Image>(); PrismatchTheme.Round(bg,PrismatchTheme.Navy); bg.raycastTarget=false;
        var symbol=Rect("Symbol",badge,new Vector2(-111,0),new Vector2(32,32));
        if(kind=="seconds")
        {
            var face=symbol.gameObject.AddComponent<Image>(); PrismatchTheme.Round(face,PrismatchTheme.Cream); face.raycastTarget=false;
            foreach(var data in new[] { (new Vector2(0,6),new Vector2(3,13)), (new Vector2(5,0),new Vector2(12,3)) })
            { var hand=Rect("Hand",symbol,data.Item1,data.Item2).gameObject.AddComponent<Image>(); hand.color=PrismatchTheme.Navy;hand.raycastTarget=false; }
        }
        else if(kind=="blocks")
        {
            for(int i=0;i<4;i++) {var block=Rect("Block",symbol,new Vector2((i%2)*17-8,(i/2)*17-8),new Vector2(13,13)).gameObject.AddComponent<Image>();PrismatchTheme.Round(block,PrismatchTheme.Green);block.raycastTarget=false;}
        }
        else if(kind=="points")
        {var gem=symbol.gameObject.AddComponent<Image>();gem.color=PrismatchTheme.Sky;gem.raycastTarget=false;symbol.localEulerAngles=new Vector3(0,0,45);symbol.sizeDelta=new Vector2(24,24);}
        else Text(symbol,"<>",26);
        var label=CardLabel(badge,"<b>"+value+"</b>  "+kind,new Vector2(22,0),new Vector2(220,56),25,PrismatchTheme.Cream);
    }

    private void ShowTutorial(int page)
    {
        if(tutorial!=null) { tutorial.SetActive(false); Destroy(tutorial); }
        tutorial=new GameObject("Visual Tutorial",typeof(RectTransform),typeof(Image));
        var overlay=(RectTransform)tutorial.transform; overlay.SetParent(safeArea,false);
        overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        tutorial.GetComponent<Image>().color=new Color(.02f,.05f,.1f,.98f);
        var card=Rect("Lesson",overlay,Vector2.zero,new Vector2(560,740));
        var lessonBackground=card.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(lessonBackground,PrismatchTheme.Navy);
        lessonBackground.raycastTarget=false;
        PrismatchTheme.Shadow(lessonBackground,new Color(0,.02f,.07f,.55f),new Vector2(0,-6));
        float availableHeight=Screen.safeArea.height / safeArea.GetComponentInParent<Canvas>().scaleFactor;
        card.localScale=Vector3.one*Mathf.Min(1,availableHeight/780f);
        string heading=page switch { 0 => "Make a match of 4", 1 => "Match the highlighted shape",
            2 => "How points add up", 3 => "New round. New power.", 4 => "Need a fresh board?", _ => "Boss duel: Coach Cub" };
        Text(Rect("Title",card,new Vector2(0,280),new Vector2(530,90)),heading,34).fontStyle=FontStyles.Bold;
        Button lessonNext = null;
        bool swapComplete = page != 0 && page != TutorialBossPage;
        if(page==0 || page==TutorialBossPage)
        {
            BuildTutorialSwap(card, () => {
                swapComplete = true;
                if (lessonNext != null) lessonNext.interactable = true;
            });
        }
        else if(page==1)
        {
            for(int i=0;i<3;i++)
            {
                var type=(MatchType)i;float y=140-i*155;
                DrawMatchDiagram(card,new Vector2(-145,y),type,40);
                string label=type==MatchType.Snake ? "Connected\nCorners can turn" : type==MatchType.Horizontal ? "Horizontal\nAcross" : "Vertical\nUp / down";
                Text(Rect("Rule",card,new Vector2(95,y),new Vector2(275,100)),label,28);
            }
            Text(Rect("Hint",card,new Vector2(0,-265),new Vector2(520,60)),"Same animal. Reach the level's match size.",23);
        }
        else if(page==2)
        {
            BuildTutorialScoring(card);
        }
        else if(page==3)
        {
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*175;
                var mini=Rect("Power card",card,new Vector2(x,50),new Vector2(145,185));
                var bg=mini.gameObject.AddComponent<Image>();PrismatchTheme.Round(bg,PrismatchTheme.RaisedNavy);bg.raycastTarget=false;
                var art=Rect("Power",mini,new Vector2(0,20),new Vector2(88,88)).gameObject.AddComponent<Image>();
                PowerUpIconLibrary.Apply(art,new TacticalStarter((TacticalStyle)i));
                Text(Rect("Round",mini,new Vector2(0,-60),new Vector2(140,42)),"Round "+(i+1),23);
            }
            Text(Rect("Gain",card,new Vector2(0,-120),new Vector2(510,90)),"Pick 1 power each round.\nBuild strength. Reach the goal.",29);
        }
        else if(page==4)
        {
            var animals=ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
            var pictures=new Image[9];
            for(int i=0;i<9;i++)
            {
                var tile=Rect("Demo tile",card,new Vector2((i%3-1)*82,145-(i/3)*82),new Vector2(74,74));
                PrismatchTheme.Round(tile.gameObject.AddComponent<Image>(),PrismatchTheme.RaisedNavy);
                var art=Rect("Animal",tile,Vector2.zero,new Vector2(62,62)).gameObject.AddComponent<Image>();
                art.sprite=animals[i%animals.Length].sprite;art.preserveAspect=true;art.raycastTarget=false;pictures[i]=art;
            }
            int offset=0;
            TutorialAction(card,"Shuffle",new Vector2(0,-95),new Vector2(290,76),"UI/Generated/nav-restart",()=> {
                offset++;
                for(int i=0;i<pictures.Length;i++) pictures[i].sprite=animals[((i+offset)%pictures.Length)%animals.Length].sprite;
            });
            Text(Rect("Cost",card,new Vector2(0,-190),new Vector2(510,90)),"5 seconds  /  2 moves\nTap Shuffle to try it",27);
        }
        Text(Rect("Page",card,new Vector2(0,340),new Vector2(200,30)),(page+1)+" / "+TutorialPageCount,19).color=PrismatchTheme.Mist;
        var next=MakeButton(page==TutorialBossPage ? "Boss beaten!" : "Next  >",Vector2.zero,new Vector2(300,68),()=> {
            if (!swapComplete) return;
            if(page<TutorialBossPage)ShowTutorial(page+1);else CloseTutorial(true);
        });
        lessonNext = next;
        next.interactable = swapComplete;
        next.transform.SetParent(card,false);((RectTransform)next.transform).anchorMin=((RectTransform)next.transform).anchorMax=new Vector2(.5f,.5f);((RectTransform)next.transform).anchoredPosition=new Vector2(90,-325);
        var back=MakeButton(page==0 ? "Skip" : "Back",Vector2.zero,new Vector2(140,68),()=> {if(page==0)CloseTutorial();else ShowTutorial(page-1);});
        back.transform.SetParent(card,false);((RectTransform)back.transform).anchorMin=((RectTransform)back.transform).anchorMax=new Vector2(.5f,.5f);((RectTransform)back.transform).anchoredPosition=new Vector2(-180,-325);
    }

    private void BuildTutorialScoring(RectTransform card)
    {
        var animals = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        var duck = Array.Find(animals, item => item != null && item.ItemType == ItemType.Duck);
        var tiles = new RectTransform[5];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = Rect("Scoring example tile", card, Vector2.zero, new Vector2(64,64));
            var background = tiles[i].gameObject.AddComponent<Image>();
            PrismatchTheme.Round(background, PrismatchTheme.Green);
            background.raycastTarget = false;
            if (duck != null)
            {
                var art = Rect("Animal", tiles[i], Vector2.zero, new Vector2(54,54)).gameObject.AddComponent<Image>();
                art.sprite = duck.sprite; art.preserveAspect = true; art.raycastTarget = false;
            }
        }
        Text(Rect("Scoring formula",card,new Vector2(0,100),new Vector2(510,48)),
            "BASE POINTS x MULTI = POINTS",25).fontStyle = FontStyles.Bold;
        var calculation = Text(Rect("Calculation",card,new Vector2(0,25),new Vector2(510,90)),"",28);
        var result = Text(Rect("Result",card,new Vector2(0,-48),new Vector2(510,48)),"",32);
        result.fontStyle = FontStyles.Bold;
        Text(Rect("Example powers",card,new Vector2(0,-131),new Vector2(510,100)),
            "Example: +4 base / tile.\nExactly 4 tiles: +1 multi this match.\nOtherwise, multi stays at 1.",23);
        bool exactlyFour = true;
        void RefreshExample()
        {
            int count = exactlyFour ? 4 : 5;
            int basePoints = count * 4;
            int multi = exactlyFour ? 2 : 1;
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].gameObject.SetActive(i < count);
                tiles[i].anchoredPosition = new Vector2((i - (count - 1) / 2f) * 76, 180);
            }
            calculation.text = $"{count} tiles x 4 base / tile = {basePoints} base\n{basePoints} base x {multi} multi = {basePoints * multi} points";
            result.text = exactlyFour ? "Condition met: 32 points!" : "No condition bonus: 20 points";
            result.color = exactlyFour ? PrismatchTheme.Green : PrismatchTheme.Cream;
        }
        TutorialAction(card,"Try 4 / 5 tiles",new Vector2(0,-231),new Vector2(310,60),null,()=> {
            exactlyFour = !exactlyFour;
            RefreshExample();
        });
        Text(Rect("Block goal note",card,new Vector2(0,-281),new Vector2(510,32)),
            "Block goals count eligible tiles instead of points.",20).color = PrismatchTheme.Mist;
        RefreshExample();
    }

    private void BuildTutorialSwap(RectTransform card, Action onComplete)
    {
        // No starting matches. Swap the bottom-right duck into the gap in row 2.
        ItemType[] layout = {
            ItemType.Bear, ItemType.Hippo, ItemType.Sloth, ItemType.Giraffe,
            ItemType.Duck, ItemType.Duck, ItemType.Bear, ItemType.Duck,
            ItemType.Sloth, ItemType.Giraffe, ItemType.Hippo, ItemType.Bear,
            ItemType.Hippo, ItemType.Bear, ItemType.Giraffe, ItemType.Duck
        };
        const int source = 15, target = 6;
        var animals = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        var cells = new RectTransform[16];
        var art = new RectTransform[16];
        var backgrounds = new Image[16];
        var hint = Text(Rect("Hint", card, new Vector2(0, -225), new Vector2(510, 95)),
            "Move the orange duck to the blue tile.\nDrag, or tap one then the other.", 24);
        int selected = -1;
        bool moving = false;

        void Select(int index)
        {
            if (moving) return;
            if (index != source && index != target)
            {
                hint.text = "Use the orange and blue tiles\nto complete the row of 4 ducks.";
                return;
            }
            if (selected < 0 || selected == index)
            {
                selected = index;
                hint.text = index == source ? "Now tap the blue tile." : "Now tap the orange duck.";
                return;
            }
            moving = true;
            var from = art[source].anchoredPosition;
            var to = art[target].anchoredPosition;
            art[source].SetAsLastSibling();
            art[target].SetAsLastSibling();
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(card.gameObject);
            sequence.Append(art[source].DOAnchorPos(to, .4f).SetEase(Ease.InOutQuad));
            sequence.Join(art[target].DOAnchorPos(from, .4f).SetEase(Ease.InOutQuad));
            sequence.AppendCallback(() => {
                backgrounds[source].color = PrismatchTheme.RaisedNavy;
                for (int i = 4; i < 8; i++) backgrounds[i].color = PrismatchTheme.Green;
                hint.text = "<b>4 in a row!</b>\nYou can swap tiles at any distance.";
                hint.color = PrismatchTheme.Cream;
                onComplete();
            });
            sequence.Play();
        }

        for (int i = 0; i < layout.Length; i++)
        {
            int index = i;
            var cell = Rect("Tutorial tile " + i, card, new Vector2((i % 4 - 1.5f) * 86, 105 - i / 4 * 86), new Vector2(78, 78));
            cells[i] = cell;
            backgrounds[i] = cell.gameObject.AddComponent<Image>();
            PrismatchTheme.Round(backgrounds[i], i == source ? PrismatchTheme.Orange : i == target ? PrismatchTheme.Sky : PrismatchTheme.RaisedNavy);
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = backgrounds[i];
            button.onClick.AddListener(() => Select(index));
            art[i] = Rect("Animal " + i, card, cell.anchoredPosition, new Vector2(65, 65));
            var image = art[i].gameObject.AddComponent<Image>();
            var animal = Array.Find(animals, item => item != null && item.ItemType == layout[index]);
            image.sprite = animal != null ? animal.sprite : null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            ItemUtils.StyleIcon(image);

            var events = cell.gameObject.AddComponent<EventTrigger>();
            var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            begin.callback.AddListener(_ => { if (!moving) { selected = -1; Select(index); } });
            events.triggers.Add(begin);
            // A drag handler is required for Unity to dispatch begin/end drag events.
            events.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.Drag });
            var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            end.callback.AddListener(data => {
                if (moving || (index != source && index != target)) return;
                var pointer = (PointerEventData)data;
                int destination = index == source ? target : source;
                if (RectTransformUtility.RectangleContainsScreenPoint(cells[destination], pointer.position, pointer.pressEventCamera))
                    Select(destination);
                else
                {
                    selected = -1;
                    hint.text = "Drag between the orange and blue tiles.";
                }
            });
            events.triggers.Add(end);
        }
    }

    private void TutorialAction(Transform parent, string label, Vector2 position, Vector2 size, string spritePath, UnityEngine.Events.UnityAction action)
    {
        var button=MakeButton(label,Vector2.zero,size,action);
        button.transform.SetParent(parent,false);
        var rect=(RectTransform)button.transform;
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;
        if(spritePath==null) return;
        var icon=Rect("Icon",rect,new Vector2(-size.x*.34f,0),new Vector2(48,48)).gameObject.AddComponent<Image>();
        icon.sprite=Resources.Load<Sprite>(spritePath);icon.preserveAspect=true;icon.raycastTarget=false;
        var text=button.GetComponentInChildren<TextMeshProUGUI>();
        text.rectTransform.anchoredPosition=new Vector2(30,0);text.rectTransform.sizeDelta=new Vector2(size.x-78,size.y-8);
    }

    private void CloseTutorial(bool completed = false)
    {
        PlayerPrefs.SetInt("VisualTutorialSeen",1);PlayerPrefs.Save();
        bool advance=campaignTutorial && completed && !ProgressStore.IsCompleted(1);
        if (campaignTutorial && completed) ProgressStore.MarkCompleted(1, 0);
        bool returnToCampaign = campaignTutorial;
        campaignTutorial = false;
        if(tutorial!=null) {tutorial.SetActive(false);Destroy(tutorial);tutorial=null;}
        if (returnToCampaign)
        {
            if(advance){campaignIndex=-1;campaignPage=-1;}
            else RememberCampaignSelection();
            BuildUI(true);
        }
    }

    private TextMeshProUGUI CardLabel(Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var label = Text(Rect("Label", parent, position, size), value, fontSize);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = color;
        return label;
    }

    private void Update()
    {
        if (safeArea != null && (lastSafeArea != Screen.safeArea ||
            lastScreenSize != new Vector2Int(Screen.width, Screen.height)))
        {
            ApplySafeArea();
            Canvas.ForceUpdateCanvases();
            FitCampaignPage();
        }
    }

    private void ApplySafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        var canvas = safeArea.GetComponentInParent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        // Browsers can expose a touchscreen on desktop. Touch capability alone
        // must not enlarge a landscape web player to the phone layout.
        bool mobileLayout = Application.isMobilePlatform || lastSafeArea.height > lastSafeArea.width;
        float scale = PickerScale(lastSafeArea.width, lastSafeArea.height, mobileLayout);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
        }
        // CanvasScaler applies on its next Update; set this too for editor previews
        // and resize events so layout and clipping use the same scale immediately.
        canvas.scaleFactor = scale;
        float inset = Mathf.Max(0f, (lastSafeArea.width / scale - 600f) * .5f);
        safeArea.offsetMin = new Vector2(inset, 0f);
        safeArea.offsetMax = new Vector2(-inset, 0f);
    }

    public static float PickerScale(float availableWidth) => PickerScale(availableWidth, 1080f, false);
    public static float PickerScale(float availableWidth, float availableHeight, bool mobile)
    {
        float widthScale = Mathf.Max(1f, availableWidth) / 600f;
        float height = Mathf.Max(1f, availableHeight);
        if (mobile) return Mathf.Min(widthScale, height / 600f);
        // Preserve the 1080p composition at 1440p/4K instead of capping pixel size.
        return Mathf.Min(widthScale, .8f * height / 1080f);
    }

    private void Refresh()
    {
        int maxLength = FreePlayConfig.MaxMatchLength(matching, width, height);
        minimumMatchLength = Mathf.Clamp(minimumMatchLength, 2, maxLength);
        matchLengthInput?.SetTextWithoutNotify(minimumMatchLength.ToString());
        if (matchLengthHint != null) matchLengthHint.text = $"Minimum: 2-{maxLength} matching animals.";
        if (replacementHint != null) replacementHint.text = replacement switch
        {
            ReplacementStrategy.South => "Fall down; new blocks enter from above.",
            ReplacementStrategy.North => "Fall up; new blocks enter from below.",
            ReplacementStrategy.West => "Fall left; new blocks enter from the right.",
            ReplacementStrategy.East => "Fall right; new blocks enter from the left.",
            _ => "Matched blocks are replaced where they were."
        };
        if (growthHeading != null) growthHeading.text = goal == GoalType.Score ? "05   POINT GROWTH" : "05   BLOCK GROWTH";
        int startingTarget = goal == GoalType.Score ? points : blocks;
        if (growthHint != null)
        {
            var targets = new string[Mathf.Min(levels, 3)];
            for (int i = 0; i < targets.Length; i++)
                targets[i] = FreePlayConfig.TargetForLevel(startingTarget, growthPercent, i + 1).ToString("N0");
            growthHint.text = $"+{growthPercent}% per round (compounded)\nTargets: {string.Join(", ", targets)}{(levels > 3 ? "..." : "")}\n" +
                $"Final target: {FreePlayConfig.TargetForLevel(startingTarget, growthPercent, levels):N0}";
        }
        if (summary == null) return;
        string target = goal == GoalType.Score ? points + " points" : blocks + " blocks";
        summary.text = $"{levels} round{(levels == 1 ? "" : "s")} / {matching} {minimumMatchLength}+\n" +
            (mode == FreePlayMode.Timed ? $"Start: {target} / {seconds}s per round\nShuffle: 5 seconds."
                : $"Start: {target} / {moves} moves per round\nSwap: 1 move. Shuffle: 2. Cascades: free.");
    }
    private void SectionCard(string name, float top, float bottom, Color accent)
    {
        var card = Rect(name, panel, new Vector2(0, (top + bottom) * .5f), new Vector2(560, top - bottom));
        var image = card.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(image, PrismatchTheme.Navy);
        image.raycastTarget = false;
        PrismatchTheme.Shadow(image, new Color(0, 0, 0, .2f), new Vector2(0, -4));
        var line = Rect("Accent", card, new Vector2(0, (top - bottom) * .5f - 2), new Vector2(110, 3));
        var stroke = line.gameObject.AddComponent<Image>();
        stroke.color = accent; stroke.raycastTarget = false;
    }

    private TextMeshProUGUI Heading(string value, float y, Color color)
    {
        var label = Label(value, y, 23);
        label.color = color;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 1.5f;
        return label;
    }

    private void HeroAnimal(ItemType animal, float x)
    {
        var items = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        var item = Array.Find(items, candidate => candidate != null && candidate.ItemType == animal);
        if (item == null || item.sprite == null) return;
        var tile = Rect(animal + " decoration", panel, new Vector2(x, -48), new Vector2(68, 68));
        tile.localEulerAngles = new Vector3(0, 0, x < 0 ?  -10 : 10);
        var background = tile.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(background, PrismatchTheme.RaisedNavy);
        background.raycastTarget = false;
        var art = Rect("Animal", tile, Vector2.zero, new Vector2(58, 58)).gameObject.AddComponent<Image>();
        art.sprite = item.sprite; art.preserveAspect = true; art.raycastTarget = false;
    }

    private void ChoiceRow(string[] choices, float y, int selected, Action<int> changed)
    {
        var images = new Image[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            bool stacked = choices.Length > 2;
            var position = stacked ? new Vector2(0, y - i * 92) : new Vector2((i - .5f) * 272, y);
            var button = MakeButton(choices[i], position, new Vector2(stacked ? 532 : 260, 80), () => {
                changed(index);
                for (int j = 0; j < images.Length; j++) StyleChoice(images[j], j == index);
                Refresh();
            });
            images[i] = button.GetComponent<Image>();
            StyleChoice(images[i], i == selected);
        }
    }
    private void ReplacementChoices(float y)
    {
        var labels = new[] { "In place", "South (down)", "North (up)", "West (left)", "East (right)" };
        var images = new Image[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var position = i == 0 ? new Vector2(0, y) :
                new Vector2(((i - 1) % 2 - .5f) * 272, y - 92 * (1 + (i - 1) / 2));
            var button = MakeButton(labels[i], position, new Vector2(i == 0 ? 532 : 260, 80), () => {
                replacement = (ReplacementStrategy)index;
                for (int j = 0; j < images.Length; j++) StyleChoice(images[j], j == index);
                Refresh();
            });
            images[i] = button.GetComponent<Image>();
            StyleChoice(images[i], i == (int)replacement);
        }
    }

    private static void StyleChoice(Image image, bool selected)
    {
        image.color = selected ? PrismatchTheme.Green : PrismatchTheme.RaisedNavy;
        var outline = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.effectColor = selected ? PrismatchTheme.Cream : new Color(.35f, .65f, 1f, .18f);
        image.GetComponentInChildren<TextMeshProUGUI>().color = selected ? PrismatchTheme.Navy : PrismatchTheme.Cream;
    }

    private TMP_InputField Number(string name, float x, float y, int value, int min, int max, Action<int> changed)
    {
        var label = Label(name, y, 28); label.name = name + " label";
        label.rectTransform.anchoredPosition = new Vector2(x - 195, y); label.rectTransform.sizeDelta = new Vector2(140, 80);
        var rect = Rect(name + " input", panel, new Vector2(x + 85, y), new Vector2(350, 80));
        // TMP creates its caret and registers text callbacks in OnEnable.
        // Keep the hierarchy inactive until all required references are assigned.
        rect.gameObject.SetActive(false);
        var background = rect.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(background, PrismatchTheme.RaisedNavy);
        background.raycastTarget = true;
        var viewport = Rect("Text Area", rect, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12, 4); viewport.offsetMax = new Vector2(-12, -4);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Text(Rect("Value", viewport, Vector2.zero, Vector2.zero), "", 32);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.richText = false;
        text.fontStyle = FontStyles.Bold;
        var field = rect.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = viewport; field.textComponent = text; field.targetGraphic = background;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.readOnly = false;
        field.interactable = true;
        field.shouldActivateOnSelect = true;
        field.onFocusSelectAll = true;
        field.shouldHideSoftKeyboard = false;
        field.shouldHideMobileInput = false;
        field.customCaretColor = true;
        field.caretColor = PrismatchTheme.Cream;
        field.caretWidth = 3;
        field.selectionColor = new Color(.35f, .65f, 1f, .5f);
        field.transition = Selectable.Transition.None;
        field.onSelect.AddListener(_ => background.color = Color.Lerp(PrismatchTheme.RaisedNavy, PrismatchTheme.Sky, .4f));
        field.onDeselect.AddListener(_ => background.color = PrismatchTheme.RaisedNavy);
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.characterLimit = 7;
        field.keyboardType = TouchScreenKeyboardType.NumberPad;
        field.SetTextWithoutNotify(value.ToString());
        field.onEndEdit.AddListener(raw => {
            int parsed = int.TryParse(raw, out int n) ? Mathf.Clamp(n, min, max) : value;
            value = parsed; field.SetTextWithoutNotify(parsed.ToString()); changed(parsed); Refresh();
        });
        rect.gameObject.SetActive(true);
        return field;
    }
    private TextMeshProUGUI Label(string value, float y, float size) => Text(Rect(value, panel, new Vector2(0, y), new Vector2(540, 52)), value, size);
    private TextMeshProUGUI Text(RectTransform rect, string value, float size)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value;
        text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = PrismatchTheme.Cream; text.raycastTarget = false;
        return text;
    }
    private Button MakeButton(string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(label, panel, position, size);
        PrismatchTheme.Round(rect.gameObject.AddComponent<Image>(), PrismatchTheme.RaisedNavy);
        var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
        rect.gameObject.AddComponent<PowerCardFeedback>();
        Text(Rect("Label", rect, Vector2.zero, size - new Vector2(20, 8)), label, 30);
        return button;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        if (parent is RectTransform parentRect && parentRect.pivot.y == 1f)
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    public void PlayLevel()
    {
        if (!Application.isPlaying || GameState.Instance == null) return;
        GameState.Instance.CurrentLevel = new FreePlayConfig(matching, width, height, goal,
            goal == GoalType.Score ? points : blocks, mode, seconds, moves, minimumMatchLength, replacement, levels, growthPercent);
        SceneManager.LoadScene("prismatch");
    }
}

public static class CampaignCalendar
{
    public const int PageSize=20;
    public static int PageCount(int total)=>Math.Max(1,(Math.Max(0,total)+PageSize-1)/PageSize);
    public static int PageForLevel(int index,int total)=>Math.Clamp(Math.Max(0,index)/PageSize,0,PageCount(total)-1);
}
