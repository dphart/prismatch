using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Board : MonoBehaviour, MatchInfo
{
    public static Board Instance { get; private set; }

    public GameObject rowPrefab;
    public GameObject tilePrefab;

    private int maxItemTypes = 8; // TODO - move to level

    public GameController gameController;

    public bool isSwapping = false;

    // ✅ Used by PowerUps to know if last match was caused by player action
    public bool wasLastMatchUserInitiated = false;

    public Tile[,] Tiles { get; private set; }
    public int Width => GameState.Instance.CurrentLevel.GetWidth(gameController.CurrentRound);
    public int Height => GameState.Instance.CurrentLevel.GetHeight(gameController.CurrentRound);

    private readonly List<Tile> _selection = new();
    private const float TweenDuration = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        DOTween.KillAll(); // When resetting the whole board

        gameController.GenerateItemPool(maxItemTypes);
        Debug.Log($"[Board] Generating board with {maxItemTypes} types");

        // Clear old board
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        Tiles = new Tile[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            GameObject rowObj = Instantiate(rowPrefab, transform);
            rowObj.name = $"Row_{y + 1}";

            for (int x = 0; x < Width; x++)
            {
                GameObject tileObj = Instantiate(tilePrefab, rowObj.transform);
                tileObj.name = $"Tile_{x + 1}";

                Tile tile = tileObj.GetComponent<Tile>();
                tile.x = x;
                tile.y = y;

                Tiles[x, y] = tile;

                if (ItemDatabase.items != null && ItemDatabase.items.Length > 0)
                {
                    tile.item = gameController.limitedItemPool[
                        UnityEngine.Random.Range(0, gameController.limitedItemPool.Count())
                    ];

                    if (tile.icon != null && tile.item.sprite != null)
                    {
                        tile.icon.sprite = tile.item.sprite;
                    }
                }
                else
                {
                    Debug.Log("[Board] ItemDatabase is null or empty!");
                }
            }
        }

        Debug.Log("[Board] Board generation complete.");

    }

    public void ResetBoard()
    {
        DOTween.KillAll(); // When resetting the whole board

        if (Tiles == null)
        {
            Debug.LogError("[Board] Can't reset: Tiles not initialized.");
            return;
        }

        Debug.Log("[Board] Resetting tile contents...");

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = Tiles[x, y];

                tile.item = gameController.limitedItemPool[
                    UnityEngine.Random.Range(0, gameController.limitedItemPool.Count)
                ];

                if (tile.icon != null && tile.item.sprite != null)
                {
                    tile.icon.sprite = tile.item.sprite;
                }
            }
        }

        Debug.Log("[Board] Board reset complete.");
    }

    public async void Select(Tile tile)
    {
        // ✅ Toggle selection
        if (!_selection.Contains(tile))
        {
            _selection.Add(tile);
            tile.SetSelected(true);   // highlight clicked tile
        }
        else
        {
            _selection.Remove(tile);
            tile.SetSelected(false);  // unhighlight if deselected
            return;
        }

        // ✅ If we don’t have 2 tiles, stop here (just show highlight)
        if (_selection.Count < 2) return;

        // ✅ We have 2 tiles — clear highlights right before swap
        Tile firstTile = _selection[0];
        Tile secondTile = _selection[1];

        firstTile.SetSelected(false);
        secondTile.SetSelected(false);

        _selection.Clear();

        await Swap(firstTile, secondTile);

        try
        {
            if (CanPop())
            {
                await Pop(firstTile, secondTile);
            }
            else
            {
                await Swap(firstTile, secondTile); // swap back if no match
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        if (isSwapping) return;
        isSwapping = true;

        // ✅ The swap action means the first match could be user-initiated
        wasLastMatchUserInitiated = true;

        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        if (icon1 == null || icon2 == null)
        {
            Debug.LogWarning("⚠️ Tried to swap a tile, but one of the icons was already destroyed.");
            isSwapping = false;
            return;
        }
        var icon1Transform = icon1.transform;
        var icon2Transform = icon2.transform;

        var sequence = DOTween.Sequence();
        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration))
                .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();
        try
        {
            icon1Transform.SetParent(tile2.transform);
            icon2Transform.SetParent(tile1.transform);

            tile1.icon = icon2;
            tile2.icon = icon1;

            var tempItem = tile1.item;
            tile1.item = tile2.item;
            tile2.item = tempItem;

            isSwapping = false;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
       
    }

    void Update()
    {
        if (!Keyboard.current.aKey.wasPressedThisFrame) return;

        foreach (var connectedTile in Tiles[0, 0].GetConnectedTiles())
        {
            connectedTile.icon.transform.DOScale(1.25f, TweenDuration).Play();
        }
    }

    private bool CanPop()
    {
        for (var y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (Tiles[x, y].GetConnectedTiles().Count >= 3)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks the swapped tiles first (user-initiated match), then cascades.
    /// </summary>
    private async Task Pop(Tile firstTile, Tile secondTile)
    {
        // ✅ First, check the swapped tiles for a match (user initiated)
        await CheckTile(firstTile.x, firstTile.y, true);
        await CheckTile(secondTile.x, secondTile.y, true);

        // ✅ Then loop until no more matches (these are cascades, NOT user-initiated)
        bool matchFound;
        do
        {
            matchFound = false;

            for (var y = 0; y < Height; ++y)
            {
                for (var x = 0; x < Width; ++x)
                {
                    bool cleared = await CheckTile(x, y, false);
                    if (cleared)
                    {
                        matchFound = true;
                    }
                }
            }
        } while (matchFound);
    }

    /// <summary>
    /// Checks a single tile for a match and clears it if needed.
    /// </summary>
    private async Task<bool> CheckTile(int x, int y, bool userInitiated)
    {
        var tile = Tiles[x, y];
        var connectedTiles = tile.GetConnectedTiles();

        // Needs at least 3 tiles to clear
        if (connectedTiles.Skip(1).Count() < 2)
            return false;

        // ✅ Mark this match for powers to read
        wasLastMatchUserInitiated = userInitiated;

        // 🔽 1) Fade out animation
        var fadeOut = DOTween.Sequence();
        foreach (var connectedTile in connectedTiles)
        {
            fadeOut.Join(connectedTile.icon.DOFade(0f, TweenDuration));
            fadeOut.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
        }

        try
        {
            gameController?.HandleTileMatch(connectedTiles);
        }
        catch (Exception e)
        {
            Debug.Log("Scoring Error: " + e);
        }

        await fadeOut.Play().AsyncWaitForCompletion();

        // 🔄 2) Assign new items AFTER fade out
        foreach (var connectedTile in connectedTiles)
        {
            var newItem = gameController.limitedItemPool[
                UnityEngine.Random.Range(0, gameController.limitedItemPool.Count)
            ];
            connectedTile.item = newItem;

            if (connectedTile.icon != null && connectedTile.item != null && connectedTile.item.sprite != null)
            {
                connectedTile.icon.sprite = connectedTile.item.sprite;
            }
            else
            {
                Debug.LogWarning($"[Board] Missing sprite assignment for tile {connectedTile.name}");
            }
        }

        // 🔼 3) Fade in new sprites
        var fadeIn = DOTween.Sequence();
        foreach (var connectedTile in connectedTiles)
        {
            fadeIn.Join(connectedTile.icon.DOFade(1f, TweenDuration));
            fadeIn.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
        }

        await fadeIn.Play().AsyncWaitForCompletion();

        return true; // ✅ Tell Pop() that we cleared something
    }

    public bool wasUserInitiatedMatch()
    {
        return wasLastMatchUserInitiated;
    }

    public void ResetBoardState()
    {
        Debug.Log("[Board] Resetting full board state.");

        wasLastMatchUserInitiated = false;
        isSwapping = false;
        _selection.Clear();

        // ✅ Kill all tweens on the board (prevents stuck animations)
        DOTween.KillAll();

        // ✅ Wipe and regenerate the board
        GenerateBoard();
    }

}
