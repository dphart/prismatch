using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{

    public int x, y;

    private Item _item;

    public bool isAnimating = false;

    public Item item
    {
        get => _item; 
        set
        {
            if( _item != value)
            {
                _item = value;
                icon.sprite = _item.sprite;
            }
        }
    }

    public Image icon;

    public Button button;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        button.onClick.AddListener(() =>
        {
            Board.Instance.Select(this);
        });
    
    }

    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y] : null;
    public Tile Right => x < Board.Instance.Width - 1 ? Board.Instance.Tiles[x + 1, y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1] : null;
    public Tile Bottom => y < Board.Instance.Height - 1 ? Board.Instance.Tiles[x, y + 1] : null;

    public Tile[] Neighbours => new Tile[] { Left, Right, Top, Bottom };

    public List<Tile> GetConnectedTiles(List<Tile> exclude = null)
    {
        var result = new List<Tile> { this };
        if (exclude == null)
        {
            exclude = new List<Tile> { this, };

        }
        else
        {
            exclude.Add(this);
        }

        foreach (var neighbour in Neighbours)
        {
            if (neighbour == null || exclude.Contains(neighbour) || neighbour.item.sprite != _item.sprite) continue;

            result.AddRange(neighbour.GetConnectedTiles(exclude));
        }

        return result;

    }




}
