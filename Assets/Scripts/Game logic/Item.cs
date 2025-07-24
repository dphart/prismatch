using UnityEngine;


[CreateAssetMenu(menuName = "Match-3/Item")]
public class Item : ScriptableObject
{
    public Sprite sprite;
    public ItemType ItemType;

}

public enum ItemType { Bear, Sloth, Chicken, Duck, Hippo, Gir}
