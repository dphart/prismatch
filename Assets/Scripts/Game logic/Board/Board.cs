using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Board : MonoBehaviour, MatchInfo, PlayerMoveInfo
{
    public int MoveId { get; private set; }
    public bool BothSwapTilesMatched { get; private set; }
    public bool AllPlayerGroupsExactlyFour { get; private set; }
    public int BoardWidth => Width;
    public int BoardHeight => Height;
    public static Board Instance { get; private set; }
    public GameObject rowPrefab, tilePrefab;
    public GameController gameController;
    public bool isSwapping { get; private set; }
    public bool wasLastMatchUserInitiated { get; private set; }
    public Tile[,] Tiles { get; private set; }
    public int Width => Tiles?.GetLength(0) ?? 0;
    public int Height => Tiles?.GetLength(1) ?? 0;
    public float VisualWidth => boardRect != null ? boardRect.rect.width : 0f;
    public BoardModel Model { get; private set; }
    public bool SecondSpotlight => Model?.SecondSpotlight ?? false;
    public ItemType SpotlightAnimal => SecondSpotlight ? ItemType.Bear : ItemType.Duck;
    public ItemType NextSpotlightAnimal => SecondSpotlight ? ItemType.Duck : ItemType.Bear;
    public bool HasAlternatingSpotlight => (GameState.Instance?.CurrentLevel as CampaignLevel)?.Bonus == LevelBonus.AlternatingCurrent;
    private readonly List<Tile> selection = new();
    private int generation;
    private RectTransform boardRect;
    private Vector2 homePosition;
    private const float TweenDuration = 0.16f;
    private const float MatchPopScale = 1.22f;
    private const float MatchPopDuration = 0.08f;
    private const float MatchCollapseDuration = 0.12f;

    private CanvasGroup canvasGroup;
    private GameObject gravityOverlay;
    private GameObject lightningOverlay;
    private GameObject cageFeedOverlay;
    private readonly List<UnityEngine.UI.Image> gravityHiddenIcons = new();
    private readonly Dictionary<Transform, Vector3> animatedIconHomes = new();
    public CampaignMechanicState Campaign => Model?.Campaign;
    public LevelBonus CampaignBonus => (GameState.Instance?.CurrentLevel as CampaignLevel)?.Bonus ?? LevelBonus.None;
    public int CampaignMatchPercent { get; private set; } = 100;
    public StrategyLevelState Strategy => Model?.Strategy;
    public StrategyReward StrategyMatchReward { get; private set; } = StrategyReward.Identity;
    public int StrategyMatchPercent => StrategyMatchReward.Percent;
    private RectTransform campaignBorder;
    private void UpdateCampaignBorder()
    {
        if (CampaignBonus != LevelBonus.BorderPatrol) return;
        if (campaignBorder == null)
        {
            var root = new GameObject("Lit campaign edges", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
            root.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            campaignBorder = root.GetComponent<RectTransform>();
            campaignBorder.SetParent(transform, false);
            campaignBorder.anchorMin = Vector2.zero; campaignBorder.anchorMax = Vector2.one;
            campaignBorder.offsetMin = campaignBorder.offsetMax = Vector2.zero;
            for (int side = 0; side < 4; side++)
            {
                var image = new GameObject("Lit edge", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(campaignBorder, false);
                image.color = new Color(1f, .8f, .16f); image.raycastTarget = false;
                var rect = image.rectTransform;
                rect.anchorMin = side == 0 ? new Vector2(0, 1) : side == 1 ? new Vector2(1, 0) : Vector2.zero;
                rect.anchorMax = side == 2 ? new Vector2(1, 0) : side == 3 ? new Vector2(0, 1) : Vector2.one;
                rect.sizeDelta = side % 2 == 0 ? new Vector2(0, 5) : new Vector2(5, 0);
                rect.anchoredPosition = Vector2.zero;
            }
        }
        for (int side = 0; side < 4; side++) campaignBorder.GetChild(side).gameObject.SetActive((Campaign.LitEdges & (1 << side)) != 0);
        campaignBorder.SetAsLastSibling();
    }
    public ReplacementStrategy RefillStrategy => CampaignBonus==LevelBonus.Crosswinds && Strategy!=null ? Strategy.Direction : CampaignBonus == LevelBonus.ChangingTides && Model != null
        ? Campaign.Gravity : DefaultRefillStrategy;
    private ReplacementStrategy DefaultRefillStrategy =>
        (GameState.Instance?.CurrentLevel as ILevelSettings)?.Replacement ?? ReplacementStrategy.InPlace;

    private void RestoreAnimatedIcons()
    {
        foreach (var entry in animatedIconHomes)
            if (entry.Key != null) entry.Key.localPosition = entry.Value;
        animatedIconHomes.Clear();
    }

    public bool IsVisible => canvasGroup == null || canvasGroup.alpha > 0.01f;

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        boardRect = transform as RectTransform;
        if (boardRect != null) homePosition = boardRect.anchoredPosition;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        CancelResolution();
        if (Instance == this) Instance = null;
    }

    public void CancelResolution()
    {
        generation++;
        gameController?.CancelMechanicPresentation();
        Strategy?.CancelCageCapture();
        CampaignMatchPercent = 100;
        DOTween.Kill(this);
        RestoreAnimatedIcons();
        ClearGravityOverlay();
        ClearLightningOverlay();
        ClearCageFeedOverlay();
        foreach (var tile in selection) if (tile != null) tile.SetSelected(false);
        selection.Clear();
        isSwapping = false;
        wasLastMatchUserInitiated = false;
    }

    public void GenerateBoard()
    {
        CancelResolution();
        ResetTransitionPosition();
        var refillMask = GetComponent<UnityEngine.UI.RectMask2D>();
        if (RefillStrategy != ReplacementStrategy.InPlace && refillMask == null)
            refillMask = gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        if (refillMask != null) refillMask.enabled = RefillStrategy != ReplacementStrategy.InPlace;
        gameController.GenerateItemPool(GameState.Instance.CurrentLevel is ILevelSettings ? 7 : LevelTuning.For(GameState.Instance.CurrentLevel.GetLevelNumber).itemTypes);
        foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        campaignBorder = null;
        int width = GameState.Instance.CurrentLevel.GetWidth(gameController.CurrentRound);
        int height = GameState.Instance.CurrentLevel.GetHeight(gameController.CurrentRound);
        var arf = GetComponentInParent<UnityEngine.UI.AspectRatioFitter>();
        if (arf != null && height > 0)
        {
            arf.aspectRatio = (float)width / height;
        }
        Tiles = new Tile[width, height];
        Model = new BoardModel(width, height, gameController.GetEffectiveMatchRules());
        Model.Strategy=gameController.PowerManager.Characteristic(CampaignBonus)?.Strategy;
        Strategy?.Initialize(Model,gameController.limitedItemPool.Select(item=>item.ItemType));
        if(Strategy!=null)Strategy.BossActive=gameController.PowerManager.ActiveBossPowers().OfType<ChallengeBoss>().Any(b=>ChallengeBoss.StrategyBonusFor(b.Style)==CampaignBonus);
        Model.SetAnimalPairs(gameController.PowerManager.ActivePowers().OfType<AnimalPairPower>());
        GenerateContents(GameState.Instance?.CurrentLevel is CampaignLevel level &&
            level.GetLevelNumber == 3 && level.LightningLines && gameController.CurrentRound == 1);
        for (int y = 0; y < height; y++)
        {
            var row = Instantiate(rowPrefab, transform);
            row.name = $"Row_{y + 1}";
            for (int x = 0; x < width; x++)
            {
                var tile = Instantiate(tilePrefab, row.transform).GetComponent<Tile>();
                tile.name = $"Tile_{x + 1}";
                tile.x = x; tile.y = y;
                Tiles[x, y] = tile;
            }
        }
        SyncVisuals();
        gameController.GameUIManager?.UpdateAnimalBonus(gameController,this);
    }

    public async Task PlayLevelCompleteExit()
    {
        if (boardRect == null) return;
        homePosition = boardRect.anchoredPosition;
        float parentWidth = (boardRect.parent as RectTransform)?.rect.width ?? 0f;
        float distance = Mathf.Max(parentWidth, Screen.width, 900f) + boardRect.rect.width;
        var tween = boardRect.DOAnchorPosX(homePosition.x - distance, .45f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .SetId(this);
        await tween.Play().AsyncWaitForCompletion();
    }

    private void ResetTransitionPosition()
    {
        if (boardRect != null) boardRect.anchoredPosition = homePosition;
    }

    private void GenerateContents(bool lightningOpening = false)
    {
        var cagedAnimals=Strategy?.Caged.Concat(Strategy.Golden).Distinct().ToDictionary(c=>c,c=>Model[c.X,c.Y]);
        Model.Generate(gameController.limitedItemPool.Select(i => i.ItemType).Distinct().ToList(),
            new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
            gameController.HasSpawnPowers ? gameController.SpawnWeight : null, lightningOpening);
        if(cagedAnimals!=null)foreach(var pair in cagedAnimals)Model[pair.Key.X,pair.Key.Y]=pair.Value;
        Strategy?.SetAnimals(gameController.limitedItemPool.Select(item=>item.ItemType));
        EnsureTreasure();
    }
    private void EnsureTreasure()
    {
        if(CampaignBonus==LevelBonus.TreasureTrail) Model.EnsureTreasure(new System.Random(UnityEngine.Random.Range(0,int.MaxValue)));
    }

    public void ReplaceRemovedAnimals()
    {
        if (Model == null) return;
        var pool = gameController.limitedItemPool;
        if (pool.Count == 0) return;
        var allowed = new HashSet<ItemType>(pool.Select(item => item.ItemType));
        Strategy?.SetAnimals(allowed);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            if (Model[x, y].HasValue && !allowed.Contains(Model[x, y].Value))
            {
                Strategy?.Golden.Remove(new BoardModel.Cell(x,y));
                Model[x, y] = gameController.PickSpawnAnimal();
            }
        SyncVisuals();
    }

    private void SyncVisuals()
    {
        UpdateCampaignBorder();
        var byType = gameController.limitedItemPool.GroupBy(i => i.ItemType).ToDictionary(g => g.Key, g => g.First());
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            var tile = Tiles[x, y];
            tile.item = byType[Model[x, y].Value];
            tile.SetTreasure(CampaignBonus==LevelBonus.TreasureTrail && Model.Treasure.HasValue && Model.Treasure.Value.Equals(new BoardModel.Cell(x,y)));
            tile.SetGoldenRescue(Strategy?.Golden.Contains(new BoardModel.Cell(x,y))==true);
            tile.SetCharged(Model.IsCharged(x,y));
            tile.SetLevelEffects(Model.IsPlanted(x,y), HasAlternatingSpotlight && Model[x,y] == SpotlightAnimal);
            var cell=new BoardModel.Cell(x,y);
            Color? tint=null;string label="";bool vault=false,caged=Strategy?.Caged.Contains(cell)==true;
            if(Strategy!=null) {
                if(CampaignBonus==LevelBonus.MigrationSeason&&Model[x,y]==Strategy.Marked)tint=new Color(1f,.9f,.45f);
                if(CampaignBonus==LevelBonus.CrackTheVault) {
                    if(Strategy.Boosted.Contains(cell))tint=new Color(.45f,.95f,.65f);
                    if(cell.Equals(Strategy.Vault)){vault=true;tint=PrismatchTheme.Navy;label="VAULT\n"+string.Join(" ",Strategy.VaultHits);}
                }
                if(CampaignBonus==LevelBonus.SteppingStones&&Strategy.Glowing.Contains(cell))tint=new Color(.45f,.8f,1f);
                if(CampaignBonus==LevelBonus.AnimalRescue&&Model[x,y]==Strategy.Marked)tint=new Color(.65f,1f,.65f);
                if(CampaignBonus==LevelBonus.Blackout&&Strategy.ClearedDarkness.Contains(cell))tint=new Color(.45f,.95f,.65f);
                if(caged)tint=new Color(.6f,.65f,.7f);
                if(CampaignBonus==LevelBonus.Crosswinds&&Strategy.CrossWarning&&Strategy.IsCross(cell,Model))tint=new Color(1f,.72f,.25f);
            }
            if(Strategy!=null)tile.SetStrategyVisual(tint,label,vault,caged);
            int rodIndex=Strategy==null?-1:Array.IndexOf(Strategy.Rods,cell);
            tile.SetLightningRod(rodIndex>=0,rodIndex,rodIndex>=0?Strategy.RodCharges[rodIndex]:0);
            tile.SetBlackout(Strategy?.IsHidden(cell)==true);
            tile.SetReleaseCage(CampaignBonus==LevelBonus.WildRelease&&cell.Equals(Strategy.ReleaseCage),
                Strategy?.CageMatches??0,Strategy?.CageOpen==true||Strategy?.CageCapturePending==true);
            tile.SetSelected(false);
            if (tile.icon == null) continue;
            tile.icon.color = Color.white;
            tile.icon.transform.localScale = Vector3.one;
        }
    }

    public void ResetBoard()
    {
        CancelResolution();
        if (Model == null) { GenerateBoard(); return; }
        GenerateContents();
        SyncVisuals();
    }

    public async void Select(Tile tile)
    {
        if (gameController == null || tile == null || Tiles == null || Tiles[tile.x, tile.y] != tile) return;
        if (!gameController.CanInteract)
        {
            gameController.ExplainBlockedBoardInput();
            return;
        }
        if (isSwapping) return;
        if(CampaignBonus==LevelBonus.WildRelease&&new BoardModel.Cell(tile.x,tile.y).Equals(Strategy.ReleaseCage)) {
            if(selection.Count==1&&CanDragIntoCage(selection[0])) {
                var animal=selection[0];ClearSelection();await ResolveMove(animal,tile,true);
            } else gameController.ShowMessage(Strategy.CagePoolExhausted?"The cage is resting: three animal types must remain.":Strategy.CageOpen?"Drag an animal here, or tap an animal then the cage.":$"Match beside the cage: {Strategy.CageMatches}/10.");
            return;
        }
        if(Model.IsBlocked(new BoardModel.Cell(tile.x,tile.y)))return;
        if (selection.Remove(tile)) { tile.SetSelected(false); return; }
        selection.Add(tile);
        tile.SetSelected(true);
        if (selection.Count < 2) return;
        var first = selection[0]; var second = selection[1];
        foreach (var selected in selection) selected.SetSelected(false);
        selection.Clear();
        await ResolveMove(first, second);
    }

    public Task Swap(Tile first, Tile second) => ResolveMove(first, second);

    private void ClearSelection()
    {
        foreach(var tile in selection)if(tile!=null)tile.SetSelected(false);
        selection.Clear();
    }
    public bool CanDragIntoCage(Tile tile) => tile!=null && gameController!=null && gameController.CanInteract && !isSwapping &&
        CampaignBonus==LevelBonus.WildRelease && Strategy?.CageOpen==true && tile.x>=0 && tile.y>=0 &&
        tile.x<Width && tile.y<Height && Tiles[tile.x,tile.y]==tile &&
        Strategy.CanFeedCage(Model,new BoardModel.Cell(tile.x,tile.y),Strategy.ReleaseCage);
    public async void FinishCageDrag(Tile animal,Tile destination)
    {
        if(!CanDragIntoCage(animal)||destination==null||destination.x<0||destination.y<0||destination.x>=Width||destination.y>=Height||
            Tiles[destination.x,destination.y]!=destination)return;
        bool capture=new BoardModel.Cell(destination.x,destination.y).Equals(Strategy.ReleaseCage);
        if(!capture&&!GameController.SwipeToMoveEnabled)return;
        ClearSelection();await ResolveMove(animal,destination,capture);
    }

    private async Task ResolveMove(Tile first, Tile second,bool cageCapture=false)
    {
        if (isSwapping || first == null || second == null || first == second || !gameController.CanInteract) return;
        var a = new BoardModel.Cell(first.x, first.y); var b = new BoardModel.Cell(second.x, second.y);
        if(cageCapture ? Strategy?.CanFeedCage(Model,a,b)!=true : Model.IsBlocked(a)||Model.IsBlocked(b))return;
        int version = generation;
        Campaign.BeginPlay();
        MoveId++;
        gameController.GameUIManager?.BeginPlayerMoveScore();
        BothSwapTilesMatched = false;
        isSwapping = true;
        gameController.BeginResolution();
        try
        {
            List<List<BoardModel.Cell>> groups;
            if(cageCapture) {
                await PresentMechanic(()=>AnimateCageFeed(first,second,version),version);CheckGeneration(version);
                groups=new(){Strategy.CaptureCageAnimal(Model,a,b)};
                gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),
                    $"{groups[0].Count} {first.item.ItemType} matched!")},groups[0].Select(c=>Tiles[c.X,c.Y]).ToList());
            } else {
                await AnimateSwap(first, second, version);CheckGeneration(version);
                Model.Swap(a, b);groups=Model.Matches();
            }
            SyncVisuals();
            if (groups.Count == 0)
            {
                await AnimateSwap(first, second, version);
                CheckGeneration(version);
                Model.Swap(a, b);
                SyncVisuals();
                gameController.PlaySound(GameSound.Blocked);
            }
            else
            {
                gameController.PlaySound(GameSound.Swap);
                BothSwapTilesMatched = groups.Exists(g => g.Contains(a)) && groups.Exists(g => g.Contains(b));
                AllPlayerGroupsExactlyFour = groups.Where(g => g.Contains(a) || g.Contains(b)).All(g => g.Count == 4);
                bool firstWave = true;
                int waves = 0;
                while (groups.Count > 0 || Strategy?.CrossPending==true)
                {
                    CheckGeneration(version);
                    if(groups.Count==0) {
                        var cross=Strategy.RotateCross(Model,new System.Random(UnityEngine.Random.Range(0,int.MaxValue)));
                        await PresentMechanic(()=>AnimateMechanicPulse(cross,PrismatchTheme.Orange,version),version);
                        gameController.RecordUnscoredClear(cross.Count);
                        gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),"Cross clear")},null);
                        if(Strategy.BossActive)gameController.GameUIManager?.ShowPowerActivations(gameController.PowerManager.ActiveBossPowers().OfType<ChallengeBoss>().Where(b=>b.Style==BossStyle.UnstableWinds).Select(b=>new PowerActivation(b,"Unpredictable turn")),null);
                        SyncVisuals();
                        await AnimateClear(cross,false,version);CheckGeneration(version);
                        var crossRefill=Model.Refill(cross,RefillStrategy,gameController.PickSpawnAnimal);
                        await AnimateGravity(crossRefill,version);CheckGeneration(version);SyncVisuals();
                        firstWave=false;waves++;groups=Model.Matches();continue;
                    }
                    bool cageWasOpen=Strategy?.CageOpen==true;
                    var strategyRewards=Strategy?.PrepareWave(Model,groups,firstWave?new HashSet<BoardModel.Cell>{a,b}:null);
                    if(Strategy?.PowerTriggered==true)gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),
                        CampaignBonus==LevelBonus.WildRelease?(Strategy.CageOpen?"Cage open!":$"Cage {Strategy.CageMatches}/10"):"Level power")},null);
                    if(Strategy?.BossTriggered==true)gameController.GameUIManager?.ShowPowerActivations(gameController.PowerManager.ActiveBossPowers().OfType<ChallengeBoss>().Where(boss=>ChallengeBoss.StrategyBonusFor(boss.Style)==CampaignBonus).Select(boss=>new PowerActivation(boss,"Boss rule")),null);
                    if(!cageWasOpen&&Strategy?.CageOpen==true) {
                        SyncVisuals();
                        await PresentMechanic(()=>AnimateMechanicPulse(new[]{Strategy.ReleaseCage},PrismatchTheme.Green,version),version);
                    }
                    gameController.AddStrategyMoves(Strategy?.TakeMoves()??0);
                    int progressBeforeWave = gameController.GoalProgress;
                    gameController.BeginBlockRewardBatch();
                    var lightning = (GameState.Instance?.CurrentLevel as CampaignLevel)?.LightningLines == true
                        ? Model.LightningLines(groups) : new List<(MatchType Direction, int Index)>();
                    if (lightning.Count > 0)
                    {
                        Model.ChargeLightning(lightning);
                        await PresentMechanic(()=>AnimateLightning(lightning,version),version);
                        CheckGeneration(version);
                        SyncVisuals();
                        gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(LevelBonus.None,true),"Line strike")},null);
                    }
                    var sweepRewards=new Dictionary<List<BoardModel.Cell>,Dictionary<PowerUp,int>>();
                    var cells = BlockClearPower.ExpandClears(Model, groups, gameController.PowerManager.ActivePowers(), !firstWave,
                        (power,amount,group)=>{
                            if(!sweepRewards.TryGetValue(group,out var rewards))sweepRewards[group]=rewards=new();
                            power.ForGoal(gameController.CurrentGoal);
                            rewards[power]=((BlockClearPower)power).BonusBaseForClears(amount);
                            gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(power,$"+{amount} clears")},group.Select(c=>Tiles[c.X,c.Y]).ToList());
                        });
                    cells = Model.ExpandLightning(cells,lightning);
                    var characteristic=gameController.PowerManager.Characteristic(CampaignBonus);
                    if(characteristic?.CollectTreasure(Model,cells)==true)
                        gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(characteristic,"Treasure collected",true)},null);
                    // Score a snapshot before refill; newly created groups are cascades.
                    int strategyGroupIndex=0;
                    foreach (var group in groups)
                    {
                        StrategyMatchReward=strategyRewards?[strategyGroupIndex++]??StrategyReward.Identity;
                        wasLastMatchUserInitiated = firstWave && (group.Contains(a) || group.Contains(b));
                        int previousEdges=Campaign.LitEdges;
                        CampaignMatchPercent = Strategy!=null?StrategyMatchPercent:characteristic?.BorderReward(Model,group) ?? 100;
                        if(CampaignBonus==LevelBonus.BorderPatrol && (previousEdges!=Campaign.LitEdges||CampaignMatchPercent==400))
                            gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(characteristic,"Edge lit",true)},null);
                        gameController.HandleTileMatchWithRewards(group.Select(c => Tiles[c.X, c.Y]).ToList(),sweepRewards.GetValueOrDefault(group));
                        gameController.ExpandSharedPowerClears(Model,group,cells);
                        UpdateCampaignBorder();
                        if (CampaignBonus == LevelBonus.BorderPatrol && CampaignMatchPercent == 400)
                            gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(characteristic,"x4")},null);
                        gameController.GameUIManager?.UpdateAnimalBonus(gameController,this);
                    }
                    if ((GameState.Instance?.CurrentLevel as CampaignLevel)?.Bonus == LevelBonus.GardenPlots)
                    {
                        int planted=characteristic?.Plant(Model,groups) ?? 0;
                        if(planted>0)gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(characteristic,"Planted",true)},null);
                        SyncVisuals();
                    }
                    var matched = new HashSet<BoardModel.Cell>(groups.SelectMany(g => g));
                    gameController.HandleBonusBlocksCleared(cells.Where(c => !matched.Contains(c)).Select(c => Tiles[c.X, c.Y]).ToList());
                    CollectionPower.TransformNeighbors(Model, groups.Where(g => g.Contains(a) || g.Contains(b)), cells,
                        gameController.PowerManager.ActivePowers(), firstWave,
                        (power,amount,group)=>gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(power,$"{amount} animals changed")},
                            group.Select(c=>Tiles[c.X,c.Y]).ToList()));
                    gameController.CommitBlockRewardBatch();
                    gameController.ShowClearFeedback(gameController.GoalProgress - progressBeforeWave, waves,
                        cells.Select(cell => Tiles[cell.X, cell.Y]).ToList());
                    await AnimateClear(cells, false, version, new HashSet<BoardModel.Cell>(cells.Where(c=>!matched.Contains(c))));
                    CheckGeneration(version);
                    bool strategyOpened=Strategy?.AfterClear(Model,cells)==true;
                    if(strategyOpened)
                        gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),CampaignBonus==LevelBonus.WildRelease?"New cage: 0/10":"Vault open")},null);
                    if(strategyOpened&&CampaignBonus==LevelBonus.CrackTheVault)
                        await PresentMechanic(()=>AnimateMechanicPulse(new[]{Strategy.Vault},PrismatchTheme.Green,version),version);
                    if(cageCapture&&firstWave) {
                        gameController.ApplyCageRemovals(Strategy.CapturedAnimals);
                        Strategy.SetAnimals(gameController.limitedItemPool.Select(item=>item.ItemType));
                    }
                    var pool = gameController.limitedItemPool;
                    // The old cage was an empty reserved space, not a scored animal. Refill it on release.
                    if(cageCapture&&firstWave&&!cells.Contains(b))cells.Add(b);
                    var refill = Model.Refill(cells, RefillStrategy,
                        gameController.PickSpawnAnimal);
                    Model.ApplyCascadeDamping(refill, pool.Select(item => item.ItemType).ToList(), waves,
                        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)),
                        gameController.HasSpawnPowers ? gameController.SpawnWeight : null);
                    if (RefillStrategy == ReplacementStrategy.InPlace)
                    {
                        SyncVisuals();
                        foreach (var cell in cells)
                        {
                            var icon = Tiles[cell.X, cell.Y].icon;
                            icon.color = new Color(1, 1, 1, 0); icon.transform.localScale = Vector3.zero;
                        }
                        await AnimateClear(cells, true, version);
                    }
                    else await AnimateGravity(refill, version);
                    CheckGeneration(version);
                    EnsureTreasure();
                    SyncVisuals();
                    gameController.GameUIManager?.UpdateAnimalBonus(gameController,this);
                    firstWave = false;
                    if (gameController.TargetReached) break;
                    waves++;
                    groups = Model.Matches();
                }
                if(Strategy?.RodChainReady==true) {
                    SyncVisuals();
                    await PresentMechanic(()=>AnimateRodChain(version),version);
                    Strategy.DischargeRods(Model);SyncVisuals();
                    gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),"BOARD CHARGED: 2x")},null);
                }
                var previousGravity = RefillStrategy;
                bool treasureWasReady = Campaign.TreasureReady;
                Campaign.FinishPlay(true);
                Strategy?.FinishPlay();StrategyMatchReward=StrategyReward.Identity;
                SyncVisuals();
                if (CampaignBonus == LevelBonus.ChangingTides && previousGravity != RefillStrategy)
                    gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),"Tide turned",true)},null);
                if (CampaignBonus == LevelBonus.TreasureTrail && Campaign.TreasureReady && !treasureWasReady)
                    gameController.GameUIManager?.ShowPowerActivations(new[]{new PowerActivation(gameController.PowerManager.Characteristic(CampaignBonus),"Treasure ready",true)},null);
                CampaignMatchPercent=100;
                gameController.GameUIManager?.UpdateAnimalBonus(gameController,this);
                if (HasAlternatingSpotlight)
                {
                    Model.FinishSpotlightPlay(true);
                    SyncVisuals();
                    gameController.GameUIManager?.UpdateAnimalBonus(gameController,this);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (version == generation) { ResetBoard(); gameController.EndResolution(); }
        }
        finally
        {
            if (version == generation)
            {
                isSwapping = false;
                gameController.EndResolution();
            }
        }
    }

    private async Task PresentMechanic(Func<Task> animation,int version)
    {
        using(gameController.HoldMechanicPresentation()) {
            await animation();CheckGeneration(version);
        }
    }
    private async Task AnimateMechanicPulse(IEnumerable<BoardModel.Cell> cells,Color color,int version)
    {
        var root=new GameObject("Mechanic emphasis",typeof(RectTransform),typeof(UnityEngine.UI.LayoutElement));
        root.transform.SetParent(transform,false);root.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;
        try {
            var sequence=DOTween.Sequence().SetId(this).SetUpdate(true);
            foreach(var cell in cells) {
                var tile=Tiles[cell.X,cell.Y];
                var image=new GameObject("Expanding highlight",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(root.transform,false);image.rectTransform.position=tile.transform.position;
                image.rectTransform.sizeDelta=((RectTransform)tile.transform).rect.size;
                image.color=new Color(color.r,color.g,color.b,.45f);image.raycastTarget=false;
                sequence.Insert(0,image.rectTransform.DOScale(1.25f,.8f).SetEase(Ease.OutQuad));
                sequence.Insert(.2f,image.DOFade(0,.6f));
            }
            sequence.AppendInterval(.2f);
            await sequence.Play().AsyncWaitForCompletion();CheckGeneration(version);
        } finally { if(root!=null){root.SetActive(false);Destroy(root);} }
    }

    private void ClearCageFeedOverlay()
    {
        if(cageFeedOverlay!=null){cageFeedOverlay.SetActive(false);Destroy(cageFeedOverlay);cageFeedOverlay=null;}
    }
    private async Task AnimateCageFeed(Tile animal,Tile cage,int version)
    {
        ClearCageFeedOverlay();
        var ghost=new GameObject("Animal entering open cage",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.LayoutElement));
        cageFeedOverlay=ghost;ghost.transform.SetParent(transform,false);
        ghost.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;
        var image=ghost.GetComponent<UnityEngine.UI.Image>();image.sprite=animal.icon.sprite;image.preserveAspect=true;image.raycastTarget=false;
        var rect=ghost.GetComponent<RectTransform>();rect.sizeDelta=animal.icon.rectTransform.rect.size;
        rect.position=animal.icon.transform.position;ItemUtils.StyleIcon(image);
        try {
            var flight=DOTween.Sequence().SetUpdate(true).SetId(this);
            animal.icon.enabled=false;
            flight.Append(rect.DOScale(1.18f,.15f).SetEase(Ease.OutQuad));
            flight.Append(rect.DOMove(cage.transform.position,.4f).SetEase(Ease.InOutCubic))
                .Join(rect.DOScale(.55f,.4f));
            flight.Append(cage.CloseCaptureDoor());
            flight.Append(image.DOFade(0,.15f));
            flight.AppendInterval(.12f);
            foreach(var tile in Tiles)if(tile.item!=null&&tile.item.ItemType==animal.item.ItemType&&tile!=cage)
                flight.Insert(.55f,tile.background.DOColor(new Color(1f,.8f,.3f),.25f));
            await flight.Play().AsyncWaitForCompletion();CheckGeneration(version);
        } finally { if(animal!=null&&animal.icon!=null)animal.icon.enabled=true;
            if(cageFeedOverlay==ghost)ClearCageFeedOverlay();else if(ghost!=null)Destroy(ghost); }
    }

    private async Task AnimateSwap(Tile a, Tile b, int version)
    {
        if (a.icon == null || b.icon == null) throw new InvalidOperationException("Tile icon is missing.");
        var first = a.icon.transform; var second = b.icon.transform;
        var firstPosition = first.position; var secondPosition = second.position;
        animatedIconHomes[first] = first.localPosition; animatedIconHomes[second] = second.localPosition;
        // Board resolution must finish even if Unity briefly reports a zero time scale
        // (for example after the editor loses focus during a click).
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        sequence.timeScale = GameController.AnimationSpeed;
        sequence.Join(first.DOMove(secondPosition, TweenDuration)).Join(second.DOMove(firstPosition, TweenDuration));
        await sequence.Play().AsyncWaitForCompletion();
        CheckGeneration(version);
        RestoreAnimatedIcons();
    }

    private void ClearGravityOverlay()
    {
        foreach (var icon in gravityHiddenIcons) if (icon != null) {
            icon.enabled = true;
            var tile=icon.GetComponentInParent<Tile>();
            if(tile!=null) tile.ShowTreasureFrame(tile.IsTreasure);
        }
        gravityHiddenIcons.Clear();
        if (gravityOverlay == null) return;
        gravityOverlay.SetActive(false);
        Destroy(gravityOverlay);
        gravityOverlay = null;
    }

    private void ClearLightningOverlay()
    {
        if (lightningOverlay == null) return;
        lightningOverlay.SetActive(false);
        Destroy(lightningOverlay);
        lightningOverlay = null;
    }

    private async Task AnimateRodChain(int version)
    {
        ClearLightningOverlay();
        lightningOverlay=new GameObject("Rod chain lightning",typeof(RectTransform),typeof(UnityEngine.UI.LayoutElement));
        var root=lightningOverlay.GetComponent<RectTransform>();root.SetParent(transform,false);
        lightningOverlay.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;
        try {
            var sequence=DOTween.Sequence().SetId(this).SetUpdate(true);
            for(int link=0;link<3;link++) {
                var a=Strategy.Rods[link];var b=Strategy.Rods[(link+1)%3];
                Vector2 start=root.InverseTransformPoint(Tiles[a.X,a.Y].transform.position);
                Vector2 end=root.InverseTransformPoint(Tiles[b.X,b.Y].transform.position);
                Vector2 delta=end-start,normal=new Vector2(-delta.y,delta.x).normalized,previous=start;
                for(int n=1;n<=10;n++) {
                    Vector2 next=Vector2.Lerp(start,end,n/10f)+(n==10?Vector2.zero:normal*(n%2==0?-9:9));
                    for(int layer=0;layer<2;layer++) {
                        var image=new GameObject("Chain bolt",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                        var rect=image.rectTransform;rect.SetParent(root,false);rect.anchoredPosition=(previous+next)*.5f;
                        rect.sizeDelta=new Vector2(Vector2.Distance(previous,next)+2,layer==0?14:4);
                        rect.localEulerAngles=new Vector3(0,0,Mathf.Atan2(next.y-previous.y,next.x-previous.x)*Mathf.Rad2Deg);
                        image.color=layer==0?new Color(.2f,.75f,1,0):new Color(1,1,1,0);image.raycastTarget=false;
                        sequence.Insert(.1f+link*.16f,image.DOFade(layer==0?.7f:1,.06f));
                        sequence.Insert(.6f,image.DOFade(0,.4f));
                    }
                    previous=next;
                }
            }
            foreach(var tile in Tiles)if(!Model.IsBlocked(new(tile.x,tile.y)))
                sequence.Insert(.55f,tile.background.DOColor(Tile.ChargedColor,.45f));
            await sequence.Play().AsyncWaitForCompletion();CheckGeneration(version);
        } finally { ClearLightningOverlay(); }
    }

    private async Task AnimateLightning(List<(MatchType Direction, int Index)> lines, int version)
    {
        lightningOverlay = new GameObject("LineLightning", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
        var overlay = lightningOverlay.GetComponent<RectTransform>();
        overlay.SetParent(transform, false);
        overlay.anchorMin = overlay.anchorMax = new Vector2(.5f,.5f);
        overlay.anchoredPosition = Vector2.zero;
        lightningOverlay.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        overlay.SetAsLastSibling();
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        foreach (var line in lines)
        {
            bool horizontal = line.Direction == MatchType.Horizontal;
            Vector2 start = transform.InverseTransformPoint(Tiles[horizontal ? 0 : line.Index, horizontal ? line.Index : 0].icon.transform.position);
            Vector2 end = transform.InverseTransformPoint(Tiles[horizontal ? Width - 1 : line.Index, horizontal ? line.Index : Height - 1].icon.transform.position);
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            start -= direction * 25; end += direction * 25;
            Vector2 previous = start;
            for (int i = 1; i <= 12; i++)
            {
                Vector2 next = Vector2.Lerp(start, end, i / 12f) + (i == 12 ? Vector2.zero : normal * (i % 2 == 0 ? -9 : 9));
                for (int layer = 0; layer < 2; layer++)
                {
                    var segment = new GameObject("Lightning segment", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    var rect = segment.GetComponent<RectTransform>();
                    rect.SetParent(overlay, false);
                    rect.localPosition = (previous + next) * .5f;
                    rect.sizeDelta = new Vector2(Vector2.Distance(previous, next) + 2, layer == 0 ? 13 : 4);
                    rect.localEulerAngles = new Vector3(0,0,Mathf.Atan2(next.y - previous.y, next.x - previous.x) * Mathf.Rad2Deg);
                    var image = segment.GetComponent<UnityEngine.UI.Image>();
                    var strikeColor=layer == 0 ? new Color(.2f,.75f,1,.7f) : Color.white;
                    image.color=new Color(strikeColor.r,strikeColor.g,strikeColor.b,0);
                    image.raycastTarget = false;
                    sequence.Insert(.22f,image.DOFade(strikeColor.a,.06f));
                    sequence.Insert(.34f,image.DOFade(0,.35f));
                }
                previous = next;
            }
        }
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            int distance = lines.Min(line => line.Direction == MatchType.Horizontal
                ? Math.Abs(y-line.Index) : Math.Abs(x-line.Index));
            if (Tiles[x,y].background == null) continue;
            if(distance==0)sequence.Insert(0,Tiles[x,y].background.DOColor(new Color(.45f,.8f,1f),.2f));
            float delay = .35f + distance * .045f;
            sequence.Insert(delay,Tiles[x,y].background.DOColor(new Color(.35f,.8f,1f,1f),.08f));
            sequence.Insert(delay+.08f,Tiles[x,y].background.DOColor(Tile.ChargedColor,.18f));
        }
        sequence.Insert(0,DOVirtual.DelayedCall(1f,()=>{},true));
        try { await sequence.Play().AsyncWaitForCompletion();CheckGeneration(version); }
        finally { ClearLightningOverlay(); }
    }

    private async Task AnimateGravity(List<BoardModel.RefillMove> moves, int version)
    {
        // Render moving copies above every tile background, regardless of travel direction.
        // The board mask clips incoming pieces at the supplying edge.
        var positions = new Vector3[Width, Height];
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            positions[x, y] = transform.InverseTransformPoint(Tiles[x, y].icon.transform.position);
        Vector3 origin = positions[0, 0];
        Vector3 stepX = Width > 1 ? positions[1, 0] - origin : Vector3.right;
        Vector3 stepY = Height > 1 ? positions[0, 1] - origin : Vector3.down;
        SyncVisuals();
        gravityOverlay = new GameObject("GravityOverlay", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement));
        var overlay = gravityOverlay.GetComponent<RectTransform>();
        overlay.SetParent(transform, false);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        gravityOverlay.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        overlay.SetAsLastSibling();
        float maxDistance = moves.Max(move => Mathf.Abs(move.To.X - move.From.X) + Mathf.Abs(move.To.Y - move.From.Y));
        float duration = Mathf.Min(.6f, .24f + maxDistance * .035f);
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        sequence.timeScale = GameController.AnimationSpeed;
        foreach (var move in moves)
        {
            var tile = Tiles[move.To.X,move.To.Y];
            var original = tile.icon;
            var copy = new GameObject("FallingBlock", typeof(RectTransform));
            var rect = copy.GetComponent<RectTransform>();
            rect.SetParent(overlay, false);
            var iconSize = new Vector2(
                transform.InverseTransformVector(original.transform.TransformVector(Vector3.right * original.rectTransform.rect.width)).magnitude,
                transform.InverseTransformVector(original.transform.TransformVector(Vector3.up * original.rectTransform.rect.height)).magnitude);
            // Keep grid boxes fixed while animals fall; charge follows the model.
            rect.sizeDelta = iconSize;
            var movingIcon = new GameObject("Animal",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            movingIcon.transform.SetParent(rect,false);
            var image = movingIcon.GetComponent<UnityEngine.UI.Image>();
            image.rectTransform.sizeDelta = iconSize;
            image.sprite = original.sprite;
            image.enabled = Strategy?.IsHidden(move.To)!=true;
            ItemUtils.StyleIcon(image);
            image.color = original.color;
            image.preserveAspect = original.preserveAspect;
            image.raycastTarget = false;
            Tile.AddTreasureFrame(image.rectTransform, tile.IsTreasure);
            original.enabled = false;
            tile.ShowTreasureFrame(false);
            gravityHiddenIcons.Add(original);
            rect.localPosition = origin + stepX * move.From.X + stepY * move.From.Y;
            // One wave shares a duration, keeping relative order while every lane settles.
            sequence.Join(rect.DOLocalMove(positions[move.To.X, move.To.Y], duration).SetEase(Ease.InQuad));
        }
        await sequence.Play().AsyncWaitForCompletion();
        CheckGeneration(version);
        ClearGravityOverlay();
    }

    private async Task AnimateClear(List<BoardModel.Cell> cells, bool refill, int version, HashSet<BoardModel.Cell> extraCells = null)
    {
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        sequence.timeScale = GameController.AnimationSpeed;
        var markers=new List<CanvasGroup>();
        foreach (var cell in cells)
        {
            var icon = Tiles[cell.X, cell.Y].icon;
            if (refill)
            {
                sequence.Join(icon.DOFade(1f, TweenDuration));
                sequence.Join(icon.transform.DOScale(Vector3.one, TweenDuration).SetEase(Ease.OutBack));
            }
            else
            {
                if(extraCells?.Contains(cell)==true) {
                    var marker=Tiles[cell.X,cell.Y].ShowExtraClearMarker();markers.Add(marker);
                    sequence.Insert(0,marker.transform.DOScale(1.08f,MatchPopDuration).From(.86f).SetEase(Ease.OutBack));
                    sequence.Insert(MatchPopDuration,marker.DOFade(0f,MatchCollapseDuration));
                }
                sequence.Insert(0,icon.transform.DOScale(Vector3.one * MatchPopScale, MatchPopDuration)
                    .SetEase(Ease.OutQuad));
                sequence.Insert(MatchPopDuration,
                    icon.transform.DOScale(Vector3.zero, MatchCollapseDuration).SetEase(Ease.InBack));
                sequence.Insert(MatchPopDuration, icon.DOFade(0f, MatchCollapseDuration));
                if (Tiles[cell.X,cell.Y].IsCharged && Tiles[cell.X,cell.Y].background != null)
                    sequence.Insert(MatchPopDuration,Tiles[cell.X,cell.Y].background.DOColor(Color.white,MatchCollapseDuration));
            }
        }
        try {
            await sequence.Play().AsyncWaitForCompletion();
            CheckGeneration(version);
        }
        finally {
            foreach(var marker in markers)if(marker!=null){marker.transform.localScale=Vector3.one;marker.gameObject.SetActive(false);}
        }
    }

    private void CheckGeneration(int version)
    {
        if (version != generation || this == null) throw new OperationCanceledException();
    }

    public List<Tile> GetConnectedTiles(Tile tile) => Model == null || tile == null
        ? new List<Tile>() : Model.Connected(tile.x, tile.y).Select(c => Tiles[c.X, c.Y]).ToList();
    public bool wasUserInitiatedMatch() => wasLastMatchUserInitiated;
    public void ResetBoardState() => GenerateBoard();
    public void SetMatchRules(IEnumerable<MatchRule> rules)
    {
        Model?.SetMatchRules(rules);
        Model?.SetAnimalPairs(gameController.PowerManager.ActivePowers().OfType<AnimalPairPower>());
    }
}
