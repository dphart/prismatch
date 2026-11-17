using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(menuName = "Match-3/Item")]
public class Item : ScriptableObject
{
    public Sprite sprite;
    public ItemType ItemType;

}

public enum ItemType { Bear, Sloth, Chicken, Duck, Hippo, Giraffe, Walrus, Moose, Panda, Elephant}

public static class ItemUtils
{
    // Outline the rendered silhouette so every animal stays readable on pale tiles.
    // Shared by board tiles, falling copies and composed power icons.
    public static void StyleIcon(UnityEngine.UI.Image image)
    {
        if (image == null) return;
        var outline = image.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(.035f, .10f, .18f, .95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    public static List<ItemType> GetItemTypes(List<Item> items)
    {
        return items.Select(item => item.ItemType).ToList();
    }
}
