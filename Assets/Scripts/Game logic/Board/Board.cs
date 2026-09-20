using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Board : MonoBehaviour, MatchInfo
{
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
    private readonly List<Tile> selection = new();
    private int generation;
    private RectTransform boardRect;
    private Vector2 homePosition;
    private const float TweenDuration = 0.16f;
    private const float MatchPopScale = 1.22f;
    private const float MatchPopDuration = 0.08f;
    private const float MatchCollapseDuration = 0.12f;
    private const int MaxCascadeWaves = 6;
    private CanvasGroup canvasGroup;

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
        DOTween.Kill(this);
        foreach (var tile in selection) if (tile != null) tile.SetSelected(false);
        selection.Clear();
        isSwapping = false;
        wasLastMatchUserInitiated = false;
    }

    public void GenerateBoard()
    {
        CancelResolution();
        ResetTransitionPosition();
        gameController.GenerateItemPool(LevelTuning.For(GameState.Instance.CurrentLevel.GetLevelNumber).itemTypes);
        foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        int width = GameState.Instance.CurrentLevel.GetWidth(gameController.CurrentRound);
        int height = GameState.Instance.CurrentLevel.GetHeight(gameController.CurrentRound);
        var arf = GetComponentInParent<UnityEngine.UI.AspectRatioFitter>();
        if (arf != null && height > 0)
        {
            arf.aspectRatio = (float)width / height;
        }
        Tiles = new Tile[width, height];
        Model = new BoardModel(width, height, gameController.GetEffectiveMatchRules());
        GenerateContents();
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

    private void GenerateContents() => Model.Generate(
        gameController.limitedItemPool.Select(i => i.ItemType).Distinct().ToList(),
        new System.Random(UnityEngine.Random.Range(0, int.MaxValue)));

    private void SyncVisuals()
    {
        var byType = gameController.limitedItemPool.GroupBy(i => i.ItemType).ToDictionary(g => g.Key, g => g.First());
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            var tile = Tiles[x, y];
            tile.item = byType[Model[x, y].Value];
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

    private async Task ResolveMove(Tile first, Tile second)
    {
        if (isSwapping || first == null || second == null || first == second || !gameController.CanInteract) return;
        int version = generation;
        isSwapping = true;
        gameController.BeginResolution();
        try
        {
            await AnimateSwap(first, second, version);
            CheckGeneration(version);
            var a = new BoardModel.Cell(first.x, first.y); var b = new BoardModel.Cell(second.x, second.y);
            Model.Swap(a, b);
            SyncVisuals();
            var groups = Model.Matches();
            if (groups.Count == 0)
            {
                await AnimateSwap(first, second, version);
                CheckGeneration(version);
                Model.Swap(a, b);
                SyncVisuals();
                gameController.ShowMessage("No match — try a connected group of 3 or more.");
            }
            else
            {
                gameController.PlaySound(GameSound.Swap);
                bool firstWave = true;
                int waves = 0;
                while (groups.Count > 0)
                {
                    CheckGeneration(version);
                    // Score a snapshot before refill; newly created groups are cascades.
                    foreach (var group in groups)
                    {
                        wasLastMatchUserInitiated = firstWave && (group.Contains(a) || group.Contains(b));
                        gameController.HandleTileMatch(group.Select(c => Tiles[c.X, c.Y]).ToList());
                    }
                    var cells = groups.SelectMany(g => g).ToList();
                    await AnimateClear(cells, false, version);
                    CheckGeneration(version);
                    foreach (var cell in cells)
                    {
                        var pool = gameController.limitedItemPool;
                        Model[cell.X, cell.Y] = pool[UnityEngine.Random.Range(0, pool.Count)].ItemType;
                    }
                    SyncVisuals();
                    foreach (var cell in cells)
                    {
                        var icon = Tiles[cell.X, cell.Y].icon;
                        icon.color = new Color(1, 1, 1, 0); icon.transform.localScale = Vector3.zero;
                    }
                    await AnimateClear(cells, true, version);
                    CheckGeneration(version);
                    firstWave = false;
                    if (gameController.TargetReached) break;
                    if (++waves >= MaxCascadeWaves)
                    {
                        GenerateContents(); SyncVisuals();
                        gameController.ShowMessage("Cascade limit reached — fresh board, no time penalty.");
                        break;
                    }
                    groups = Model.Matches();
                }
                if (!gameController.TargetReached && !Model.HasLegalMove())
                {
                    GenerateContents(); SyncVisuals();
                    gameController.ShowMessage("No moves left — fresh board, no time penalty.");
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

    private async Task AnimateSwap(Tile a, Tile b, int version)
    {
        if (a.icon == null || b.icon == null) throw new InvalidOperationException("Tile icon is missing.");
        var first = a.icon.transform; var second = b.icon.transform;
        var firstPosition = first.position; var secondPosition = second.position;
        // Board resolution must finish even if Unity briefly reports a zero time scale
        // (for example after the editor loses focus during a click).
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
        sequence.Join(first.DOMove(secondPosition, TweenDuration)).Join(second.DOMove(firstPosition, TweenDuration));
        await sequence.Play().AsyncWaitForCompletion();
        CheckGeneration(version);
        first.position = firstPosition; second.position = secondPosition;
    }

    private async Task AnimateClear(List<BoardModel.Cell> cells, bool refill, int version)
    {
        var sequence = DOTween.Sequence().SetId(this).SetUpdate(true);
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
                sequence.Join(icon.transform.DOScale(Vector3.one * MatchPopScale, MatchPopDuration)
                    .SetEase(Ease.OutQuad));
                sequence.Insert(MatchPopDuration,
                    icon.transform.DOScale(Vector3.zero, MatchCollapseDuration).SetEase(Ease.InBack));
                sequence.Insert(MatchPopDuration, icon.DOFade(0f, MatchCollapseDuration));
            }
        }
        await sequence.Play().AsyncWaitForCompletion();
        CheckGeneration(version);
    }

    private void CheckGeneration(int version)
    {
        if (version != generation || this == null) throw new OperationCanceledException();
    }

    public List<Tile> GetConnectedTiles(Tile tile) => Model == null || tile == null
        ? new List<Tile>() : Model.Connected(tile.x, tile.y).Select(c => Tiles[c.X, c.Y]).ToList();
    public bool wasUserInitiatedMatch() => wasLastMatchUserInitiated;
    public void ResetBoardState() => GenerateBoard();
    public void SetMatchRules(IEnumerable<MatchRule> rules) => Model?.SetMatchRules(rules);
}
