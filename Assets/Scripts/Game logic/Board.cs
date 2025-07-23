using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [Header("Prefabs & Settings")]
    public GameObject rowPrefab;
    public GameObject tilePrefab;
    public int numRows = 8;
    public int numCols = 8;

    public GameController gameController;

    public bool isSwapping = false;

    public Tile[,] Tiles { get; private set; }
    public int Width => numCols;
    public int Height => numRows;

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
        Debug.Log("[Board] Generating board...");

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
                    tile.item = ItemDatabase.items[UnityEngine.Random.Range(0, ItemDatabase.items.Length)];

                    if (tile.icon != null && tile.item.sprite != null)
                    {
                        tile.icon.sprite = tile.item.sprite;
                    }

                    Debug.Log($"[Board] Tile ({x},{y}) initialized with item: {tile.item.name}");
                }
                else
                {
                    Debug.LogError("[Board] ItemDatabase is null or empty!");
                }
            }
        }

        Debug.Log("[Board] Board generation complete.");
    }

    public void ResetBoard()
    {
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

                if (ItemDatabase.items != null && ItemDatabase.items.Length > 0)
                {
                    tile.item = ItemDatabase.items[UnityEngine.Random.Range(0, ItemDatabase.items.Length)];

                    if (tile.icon != null && tile.item.sprite != null)
                    {
                        tile.icon.sprite = tile.item.sprite;
                    }

                    Debug.Log($"[Board] Tile ({x},{y}) re-initialized with item: {tile.item.name}");
                }
            }
        }

        Debug.Log("[Board] Board reset complete.");
    }

    public async void Select(Tile tile)
    {
        if (!_selection.Contains(tile)) _selection.Add(tile);
        if (_selection.Count < 2) return;

        await Swap(_selection[0], _selection[1]);

        if (CanPop())
        {
            await Pop();
        }
        else
        {
            await Swap(_selection[0], _selection[1]);
        }

        _selection.Clear();
    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        if (isSwapping) return;
        isSwapping = true;

        var icon1 = tile1.icon;
        var icon2 = tile2.icon;

        var icon1Transform = icon1.transform;
        var icon2Transform = icon2.transform;

        var sequence = DOTween.Sequence();
        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration))
                .Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        var tempItem = tile1.item;
        tile1.item = tile2.item;
        tile2.item = tempItem;

        isSwapping = false;
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

    private async Task Pop()
    {
        for (var y = 0; y < Height; ++y)
        {
            for (var x = 0; x < Width; ++x)
            {
                var tile = Tiles[x, y];
                var connectedTiles = tile.GetConnectedTiles();
                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deflateSequence = DOTween.Sequence();
                foreach (var connectedTile in connectedTiles)
                {
                    deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
                }

                try
                {
                    gameController?.HandleTileMatch(connectedTiles);
                }
                catch (Exception e)
                {
                    Debug.LogError("Scoring Error: " + e);
                }

                await deflateSequence.Play().AsyncWaitForCompletion();

                var inflateSequence = DOTween.Sequence();
                foreach (var connectedTile in connectedTiles)
                {
                    connectedTile.item = ItemDatabase.items[UnityEngine.Random.Range(0, ItemDatabase.items.Length)];
                    inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await inflateSequence.Play().AsyncWaitForCompletion();

                // Restart pop detection
                x = 0;
                y = 0;
            }
        }
    }
}
