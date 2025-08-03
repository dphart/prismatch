using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(menuName = "Match-3/Item")]
public class Item : ScriptableObject
{
    public Sprite sprite;
    public ItemType ItemType;

}

public enum ItemType { Bear, Sloth, Chicken, Duck, Hippo, Girraffe, Walrus}

public static class ItemUtils
{
    public static List<ItemType> GetItemTypes(List<Item> items)
    {
        return items.Select(item => item.ItemType).ToList();
    }
}
