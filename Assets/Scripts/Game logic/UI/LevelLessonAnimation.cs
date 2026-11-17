using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>One reusable, unscaled animation player for every level briefing.</summary>
public sealed class LevelLessonAnimation : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private sealed class TileView
    {
        public Image Background, Icon;
        public RectTransform Treasure;
        public RectTransform Cage, CageDoor, Rod;
        public RectTransform BadgeRoot;
        public TextMeshProUGUI Badge;
    }
    public LevelLessonStoryboard Storyboard { get; private set; }
    public float PlaybackTime { get; private set; }
    public int CurrentStep { get; private set; }
    public int StepCount => (Storyboard?.StepStarts.Length ?? 0) + instructions.Count;
    public bool IsPlaying => playback != null && playback.IsActive() && playback.IsPlaying();
    private readonly LevelLessonFrame frame = new();
    private readonly TileView[] tiles = new TileView[64];
    private readonly Image[] edges = new Image[4];
    private readonly Image[] bolts = new Image[48];
    private readonly TextMeshProUGUI[] rowLabels = new TextMeshProUGUI[3];
    private readonly Dictionary<ItemType, Sprite> sprites = new();
    private RectTransform stage, board, backgrounds, icons, lightning;
    private CanvasGroup stageAlpha, boardAlpha;
    private TextMeshProUGUI heading, caption, status, footer, direction, stepLabel;
    private Tween playback;
    private Button previousButton, nextButton;
    private readonly List<(string Heading, string Text)> instructions = new();

    public void Initialize(LevelLessonStoryboard storyboard)
    {
        Stop(); Storyboard = storyboard; CurrentStep = 0; instructions.Clear();
        if (stage == null) Build();
        heading.text = Storyboard.Heading.Substring(0,1)+Storyboard.Heading.Substring(1).ToLowerInvariant();
        Render(0); Fit(); Play();
    }

    public void Play()
    {
        if (Storyboard == null || !isActiveAndEnabled) return;
        Stop();
        // DOTween automatic playback is disabled in this project. Play explicitly.
        if (CurrentStep >= Storyboard.StepStarts.Length) { ShowInstruction(); return; }
        var starts = Storyboard.StepStarts;
        float start = starts[CurrentStep];
        float end = (CurrentStep + 1 < starts.Length ? starts[CurrentStep + 1] : Storyboard.Duration) - .001f;
        Render(start);
        playback = DOVirtual.Float(start, end, end - start, Render)
            .SetEase(Ease.Linear).SetUpdate(true).SetLoops(-1, LoopType.Restart)
            .SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDestroy).Play();
    }
    public void Stop() { playback?.Kill(); playback = null; }
    public void Seek(float seconds) { playback?.Pause(); CurrentStep = Storyboard.StepAt(seconds); Render(seconds); }
    public void NextStep() => SelectStep(CurrentStep + 1);
    public void PreviousStep() => SelectStep(CurrentStep - 1);
    public void SelectStep(int step)
    {
        if (step < 0 || step >= StepCount) return;
        CurrentStep = step;
        Play();
    }
    public void AddInstructionStep(string title, string text)
    {
        instructions.Add((title, text));
        RefreshNavigation();
    }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - eventData.pressPosition;
        if (Mathf.Abs(delta.x) < Mathf.Max(40, Screen.width * .08f) || Mathf.Abs(delta.x) <= Mathf.Abs(delta.y)) return;
        if (delta.x < 0) NextStep(); else PreviousStep();
    }
    private void RefreshNavigation()
    {
        stepLabel.text = $"STEP {CurrentStep + 1} OF {StepCount}";
        previousButton.interactable = CurrentStep > 0;
        nextButton.interactable = CurrentStep + 1 < StepCount;
    }
    private void ShowInstruction()
    {
        var instruction = instructions[CurrentStep - Storyboard.StepStarts.Length];
        stageAlpha.alpha = 1;
        board.gameObject.SetActive(false);
        direction.gameObject.SetActive(false);
        foreach (var label in rowLabels) label.gameObject.SetActive(false);
        heading.text = instruction.Heading;
        caption.rectTransform.sizeDelta = new Vector2(650, 680);
        caption.rectTransform.anchoredPosition = new Vector2(0, 20);
        caption.text = instruction.Text;
        status.text = footer.text = "";
        RefreshNavigation();
    }
    private void OnEnable() { if (stage != null) Play(); }
    private void OnDisable() => Stop();
    private void OnDestroy() => Stop();
    private void LateUpdate() => Fit();
    private void OnRectTransformDimensionsChange() => Fit();

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static Image Image(string name, Transform parent, Vector2 size)
    {
        var image = Rect(name, parent, size, Vector2.zero).gameObject.AddComponent<Image>();
        image.raycastTarget = false; return image;
    }
    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 size, Vector2 position, float font, Color color)
    {
        var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = font; label.color = color; label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false;
        return label;
    }
    private void Build()
    {
        stage = Rect("Looping lesson stage", transform, new Vector2(720, 1140), Vector2.zero);
        stageAlpha = stage.gameObject.AddComponent<CanvasGroup>();
        heading = Text("Lesson heading", stage, new Vector2(660, 90), new Vector2(0, 515), 30, PrismatchTheme.Cream);
        heading.fontStyle = FontStyles.Bold;
        stepLabel=Text("Numbered lesson step",stage,new Vector2(660,34),new Vector2(0,450),26,PrismatchTheme.Orange);
        stepLabel.fontStyle=FontStyles.Bold;
        caption = Text("Lesson action", stage, new Vector2(650, 128), new Vector2(0, 365), 34, PrismatchTheme.Cream);
        board = Rect("Example board", stage, Vector2.zero, new Vector2(0, -45));
        board.gameObject.AddComponent<RectMask2D>();
        boardAlpha = board.gameObject.AddComponent<CanvasGroup>(); boardAlpha.blocksRaycasts = boardAlpha.interactable = false;
        backgrounds = Rect("Fixed boxes", board, Vector2.zero, Vector2.zero);
        icons = Rect("Moving animals", board, Vector2.zero, Vector2.zero);
        var animals = ItemDatabase.items ?? Resources.LoadAll<Item>("Items/");
        foreach (var animal in animals) if (animal != null && animal.sprite != null) sprites[animal.ItemType] = animal.sprite;
        for (int i = 0; i < tiles.Length; i++) {
            var view = new TileView(); tiles[i] = view;
            view.Background = Image("Example box " + i, backgrounds, new Vector2(50, 50));
            PrismatchTheme.Round(view.Background, PrismatchTheme.RaisedNavy);
            view.Icon = Image("Example animal " + i, icons, new Vector2(40, 40));
            view.Icon.preserveAspect = true; ItemUtils.StyleIcon(view.Icon);
            view.Treasure = Tile.AddTreasureFrame(view.Icon.rectTransform, true);
            var badgeBackground = Image("Reward badge", view.Icon.transform, new Vector2(31, 21));
            PrismatchTheme.Round(badgeBackground, new Color(.9f, .97f, 1, .96f));
            view.BadgeRoot = badgeBackground.rectTransform; view.BadgeRoot.anchoredPosition = new Vector2(13, -14);
            view.Badge = Text("Badge label", view.BadgeRoot, new Vector2(32, 22), Vector2.zero, 17, PrismatchTheme.Navy);
            view.Badge.fontStyle = FontStyles.Bold;
        }
        // A separate top layer prevents moving icons from passing behind other boxes.
        for (int i = 0; i < edges.Length; i++) edges[i] = Image("Example edge " + i, board, Vector2.zero);
        lightning = Rect("Lightning zigzag", board, Vector2.zero, Vector2.zero);
        for (int i = 0; i < bolts.Length; i++) bolts[i] = Image("Bolt segment " + i, lightning, Vector2.zero);
        for (int i = 0; i < rowLabels.Length; i++)
            rowLabels[i] = Text("Match size reward " + i, stage, new Vector2(175, 66), Vector2.zero, 26, PrismatchTheme.Cream);
        direction = Text("Gravity arrow", stage, new Vector2(104, 104), new Vector2(285, -45), 56, PrismatchTheme.Sky);
        status = Text("Lesson result", stage, new Vector2(660, 48), new Vector2(0, -410), 29, PrismatchTheme.Green);
        status.fontStyle = FontStyles.Bold;
        footer = Text("Lesson reminder", stage, new Vector2(650, 52), new Vector2(0, -466), 26, PrismatchTheme.Mist);
        // Fit each text block independently inside the space around the example board.
        foreach (var label in new[] { heading, caption, status, footer })
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * .7f;
        }
        foreach (var label in rowLabels)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = 26;
            label.fontSizeMin = 18;
        }
        previousButton = NavigationButton("Previous step", "Back", -240, PreviousStep);
        nextButton = NavigationButton("Next step", "Next", 240, NextStep);
        Text("Swipe instruction", stage, new Vector2(260, 64), new Vector2(0, -532), 23, PrismatchTheme.Mist).text = "Swipe left\nor right";
    }
    private Button NavigationButton(string name, string label, float x, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, stage, new Vector2(200, 72), new Vector2(x, -532));
        var background = rect.gameObject.AddComponent<Image>();
        PrismatchTheme.Round(background, PrismatchTheme.RaisedNavy);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        Text("Label", rect, new Vector2(192, 64), Vector2.zero, 30, PrismatchTheme.Cream).text = label;
        return button;
    }
    private void Fit()
    {
        if (stage == null || transform is not RectTransform rect) return;
        float scale = Mathf.Min(Mathf.Max(.1f, (rect.rect.width - 16) / 720), Mathf.Max(.1f, (rect.rect.height - 16) / 1140));
        if (Mathf.Abs(stage.localScale.x - scale) > .001f) stage.localScale = Vector3.one * scale;
    }
    private static void BuildLessonCage(TileView view)
    {
        view.Cage=new GameObject("Lesson cage",typeof(RectTransform)).GetComponent<RectTransform>();
        view.Cage.SetParent(view.Background.transform,false);view.Cage.anchorMin=Vector2.zero;view.Cage.anchorMax=Vector2.one;
        view.Cage.offsetMin=view.Cage.offsetMax=Vector2.zero;
        view.CageDoor=Tile.BuildCageArtwork(view.Cage);
    }

    private void Render(float time)
    {
        if (Storyboard == null || stage == null) return;
        PlaybackTime = time; Storyboard.WriteFrame(time, frame);
        board.gameObject.SetActive(true);
        heading.text = Storyboard.Heading.Substring(0,1)+Storyboard.Heading.Substring(1).ToLowerInvariant();
        caption.rectTransform.sizeDelta = new Vector2(650, 128);
        caption.rectTransform.anchoredPosition = new Vector2(0, 365);
        stageAlpha.alpha = 1; boardAlpha.alpha = frame.BoardOpacity;
        RefreshNavigation();
        caption.text = frame.Caption; status.text = frame.Status; footer.text = frame.Footer;
        bool hasLabels=!string.IsNullOrEmpty(frame.RowLabels[0]);
        float availableWidth=hasLabels?480:frame.ShowDirection?590:680;
        float boardScale=Mathf.Min(availableWidth/(frame.Columns*frame.Step+12),680/(frame.Rows*frame.Step+12));
        board.localScale=Vector3.one*boardScale;
        board.anchoredPosition = new Vector2(hasLabels?-95:frame.ShowDirection?-45:0, -45);
        board.sizeDelta = new Vector2(frame.Columns * frame.Step + 12, frame.Rows * frame.Step + 12);
        for (int i = 0; i < tiles.Length; i++) {
            var state = frame.Tiles[i]; var view = tiles[i];
            view.Background.gameObject.SetActive(state.Visible);
            view.Icon.gameObject.SetActive(state.Visible && state.IconVisible && state.IconScale > .001f);
            if (!state.Visible) continue;
            view.Background.rectTransform.anchoredPosition = new Vector2(state.X, state.Y);
            float tileSize=frame.Step*(78f/86f);
            view.Background.rectTransform.sizeDelta = Vector2.one * tileSize;
            // Keep the tutorial's corner radius at every example-board scale.
            view.Background.pixelsPerUnitMultiplier=78f/tileSize;
            view.Background.color = state.Tint switch {
                LessonTint.Hidden => Color.black,
                LessonTint.Match => PrismatchTheme.Green,
                LessonTint.Charged => PrismatchTheme.Sky,
                LessonTint.Planted => new Color(.18f,.55f,.35f),
                _ => PrismatchTheme.RaisedNavy
            };
            if(state.Cage&&view.Cage==null)BuildLessonCage(view);
            if(view.Cage!=null) {
                view.Cage.gameObject.SetActive(state.Cage);
                view.CageDoor.localScale=new Vector3(state.CageOpen?.12f:1,1,1);
            }
            if(state.Rod&&view.Rod==null) {
                view.Rod=Rect("Example lightning rod",view.Background.transform,Vector2.zero,Vector2.zero);
                var artwork=Image("Lightning rod artwork",view.Rod,Vector2.one*(frame.Step-4)*.78f);
                artwork.sprite=Tile.LightningRodSprite;artwork.preserveAspect=true;
                artwork.rectTransform.anchoredPosition=new Vector2(0,frame.Step*.09f);

            }
            if(view.Rod!=null)view.Rod.gameObject.SetActive(state.Rod);
            if(state.Rod)view.Background.color=PrismatchTheme.Navy;
            if(state.Cage)view.Background.color=state.CageOpen?new Color(.12f,.43f,.3f):PrismatchTheme.Navy;
            view.Icon.rectTransform.anchoredPosition = new Vector2(state.X + state.IconX, state.Y + state.IconY);
            view.Icon.rectTransform.sizeDelta = Vector2.one * (tileSize*65f/78f);
            view.Icon.rectTransform.localScale = Vector3.one * state.IconScale;
            if (sprites.TryGetValue(state.Animal, out var sprite)) view.Icon.sprite = sprite;
            view.Treasure.gameObject.SetActive(state.Treasure);
            float badgeScale = Mathf.Min(1, frame.Step / 56);
            var badgeParent=state.Cage||state.Rod?view.Background.transform:view.Icon.transform;
            if(view.BadgeRoot.parent!=badgeParent)view.BadgeRoot.SetParent(badgeParent,false);
            view.BadgeRoot.localScale = Vector3.one * badgeScale;
            view.BadgeRoot.anchoredPosition = (state.Cage||state.Rod?new Vector2(0,-17):new Vector2(13,-14)) * badgeScale;
            if(state.Rod) {
                view.BadgeRoot.localScale=Vector3.one;
                view.BadgeRoot.sizeDelta=new Vector2(frame.Step-4,frame.Step*.27f);
                view.BadgeRoot.anchoredPosition=new Vector2(0,-frame.Step*.3f);
                view.Badge.rectTransform.sizeDelta=view.BadgeRoot.sizeDelta;
                view.Badge.fontSize=frame.Step*.19f;view.Badge.textWrappingMode=TextWrappingModes.NoWrap;
            } else {
                view.BadgeRoot.sizeDelta=new Vector2(31,21);view.Badge.rectTransform.sizeDelta=new Vector2(32,22);
                view.Badge.fontSize=17;view.Badge.textWrappingMode=TextWrappingModes.Normal;
            }
            view.Badge.text = state.Badge; view.BadgeRoot.gameObject.SetActive(!string.IsNullOrEmpty(state.Badge));
        }
        RenderEdges(); RenderLightning();
        for (int i = 0; i < rowLabels.Length; i++) {
            rowLabels[i].gameObject.SetActive(!string.IsNullOrEmpty(frame.RowLabels[i]));
            rowLabels[i].text = frame.RowLabels[i];
            rowLabels[i].rectTransform.anchoredPosition = new Vector2(255, ((frame.Rows - 1) * .5f - i) * frame.Step * boardScale - 45);
        }
        direction.gameObject.SetActive(frame.ShowDirection);
        direction.text = frame.Direction switch {
            ReplacementStrategy.West => "←\n<size=18>LEFT</size>", ReplacementStrategy.North => "↑\n<size=18>UP</size>",
            ReplacementStrategy.East => "→\n<size=18>RIGHT</size>", _ => "↓\n<size=18>DOWN</size>"
        };
    }
    private void RenderEdges()
    {
        float width = frame.Columns * frame.Step, height = frame.Rows * frame.Step;
        for (int i = 0; i < edges.Length; i++) {
            var edge = edges[i]; edge.gameObject.SetActive(frame.ShowEdges);
            edge.color = (frame.LitEdges & (1 << i)) != 0 ? new Color(1, .8f, .16f) : new Color(.35f, .43f, .55f, .5f);
            edge.rectTransform.sizeDelta = i % 2 == 0 ? new Vector2(width + 5, 5) : new Vector2(5, height + 5);
            edge.rectTransform.anchoredPosition = i switch {
                0 => new Vector2(0, height * .5f), 1 => new Vector2(width * .5f, 0),
                2 => new Vector2(0, -height * .5f), _ => new Vector2(-width * .5f, 0)
            };
        }
    }
    private void RenderLightning()
    {
        lightning.gameObject.SetActive(frame.Lightning > 0);
        if (frame.Lightning <= 0) return;
        foreach(var bolt in bolts)bolt.gameObject.SetActive(false);
        if(frame.RodChain) {
            var rods=new List<LessonTileFrame>();foreach(var tile in frame.Tiles)if(tile.Visible&&tile.Rod)rods.Add(tile);
            for(int link=0;link<rods.Count;link++) {
                Vector2 start=new(rods[link].X,rods[link].Y),end=new(rods[(link+1)%rods.Count].X,rods[(link+1)%rods.Count].Y);
                Vector2 delta=end-start,normal=new Vector2(-delta.y,delta.x).normalized,chainPrevious=start;
                for(int n=1;n<=8;n++) {
                    Vector2 next=Vector2.Lerp(start,end,n/8f)+(n==8?Vector2.zero:normal*(n%2==0?-5:5));
                    for(int layer=0;layer<2;layer++) {
                        var image=bolts[link*16+(n-1)*2+layer];image.gameObject.SetActive(true);var rect=image.rectTransform;
                        rect.anchoredPosition=(chainPrevious+next)*.5f;rect.sizeDelta=new Vector2(Vector2.Distance(chainPrevious,next)+2,layer==0?7:2);
                        rect.localEulerAngles=new Vector3(0,0,Mathf.Atan2(next.y-chainPrevious.y,next.x-chainPrevious.x)*Mathf.Rad2Deg);
                        image.color=layer==0?new Color(.2f,.75f,1,.8f):Color.white;
                    }
                    chainPrevious=next;
                }
            }
            return;
        }
        float width = frame.Columns * frame.Step;
        float y = ((frame.Rows - 1) * .5f - frame.LightningRow) * frame.Step;
        var previous = new Vector2(-width * .5f, y);
        for (int segment = 0; segment < 12; segment++) {
            var next = new Vector2(-width * .5f + width * (segment + 1) / 12f, y + (segment == 11 ? 0 : segment % 2 == 0 ? 9 : -9));
            for (int layer = 0; layer < 2; layer++) {
                var image = bolts[segment * 2 + layer];image.gameObject.SetActive(true); var rect = image.rectTransform;
                rect.anchoredPosition = (previous + next) * .5f;
                rect.sizeDelta = new Vector2(Vector2.Distance(previous, next) + 2, layer == 0 ? 13 : 4);
                rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(next.y - previous.y, next.x - previous.x) * Mathf.Rad2Deg);
                image.color = layer == 0 ? new Color(.2f, .75f, 1, frame.Lightning * .8f) : new Color(1, 1, 1, frame.Lightning);
            }
            previous = next;
        }
    }
}
