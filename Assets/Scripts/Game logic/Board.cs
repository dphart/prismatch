using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Board : MonoBehaviour
{

    public static Board Instance { get; private set; }

    public bool isSwapping = false;

    private void Awake() => Instance = this;

    private List<Row> rows = new List<Row>();

    public Tile[,] Tiles { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    private readonly List<Tile> _selection = new List<Tile>();

    private const float TweenDuration = .2f;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("[Board] Initializing board...");

        rows = GetComponentsInChildren<Row>().ToList();
        Height = rows.Count;

        if (Height == 0)
        {
            Debug.LogError("[Board] No rows found!");
            return;
        }

        var firstRowTiles = rows[0].GetComponentsInChildren<Tile>(false);
        Width = firstRowTiles.Length;

        if (Width == 0)
        {
            Debug.LogError("[Board] First row has no tiles!");
            return;
        }

        Debug.Log($"[Board] Expected board dimensions: {Width} x {Height}");

        Tiles = new Tile[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            var row = rows[y];
            var tilesInRow = row.GetComponentsInChildren<Tile>(false);

            if (tilesInRow.Length != Width)
            {
                Debug.LogError($"[Board] Row {y} has {tilesInRow.Length} tiles but expected {Width}. Aborting.");
                return;
            }

            for (int x = 0; x < Width; x++)
            {
                var tile = tilesInRow[x];
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

        Debug.Log("[Board] Tile grid setup complete.");
    }










    public async void Select(Tile tile)
    {
        //if (!busy) 

        if (!_selection.Contains(tile)) _selection.Add(tile);

        if (_selection.Count < 2) return;

        await Swap(_selection[0], _selection[1]);

        if (CanPop())
        {
            Pop();
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

        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration)).Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));


        await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        var tile1Item = tile1.item;
        tile1.item = tile2.item;
        tile2.item = tile1Item;
        isSwapping = false;


    }

    // Update is called once per frame
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
                   
                    if(ScoreManager.Instance != null) ScoreManager.Instance.HandleScoring(connectedTiles);
                    else Debug.LogError("ScoreManager is null: " );

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
                x = 0;
                y = 0;
            }
        }
    }
}
