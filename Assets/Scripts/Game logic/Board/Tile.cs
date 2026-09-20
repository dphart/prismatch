using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public int x, y;

    public Item _item;

    public bool isAnimating = false;
    public Image background;
    public Item item
    {
        get => _item;
        set
        {
            _item = value;

            // ✅ Check before touching icon
            if (icon == null) return;

            if (_item != null && _item.sprite != null)
            {
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
        return Board.Instance != null ? Board.Instance.GetConnectedTiles(this) : new List<Tile>();
    }

    public void SetSelected(bool isSelected)
    {
        if (background == null) return;

        if (isSelected)
        {
            // ✅ Highlight visually
           background.color = Color.darkGray;
            
        }
        else
        {
            // ✅ Reset to normal
            background.color = Color.white;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Board.Instance == null || Board.Instance.isSwapping) return;
        Board.Instance.Select(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Unity requires a drag handler in order to send begin/end drag events.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Board.Instance == null || Board.Instance.isSwapping) return;
        var destination = eventData.pointerCurrentRaycast.gameObject != null
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Tile>()
            : null;

        if (destination == null)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                destination = hit.gameObject.GetComponentInParent<Tile>();
                if (destination != null) break;
            }
        }

        if (destination != null && destination != this)
            Board.Instance.Select(destination);
    }




}
