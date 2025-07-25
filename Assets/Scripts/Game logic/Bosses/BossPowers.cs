using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BossPowers : IconBonusPower
{

  
}

class FirstItemsGivesNoPoints : BossPowers
{
    List<ItemType> selectedItems = new List<ItemType>();

    public FirstItemsGivesNoPoints()
    {
        Name = "First In None Out";
        PowerUpDescription = $"Your first match type gives no points for the remaining rounds";
    }
    public override void Apply(GameState state) {}
    
    public override PowerUp Clone()
    {
        return new FirstItemsGivesNoPoints();
    }

    public override string GetCurrentPowerDescription()
    {
        return "";
    }

    public override bool IsScoreValid(List<Tile> tiles)
    {
        if(selectedItems.Count == 0)
        {
            foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
            {
                selectedItems.Add(item);
                Debug.Log($"Adding Type {item.GetType().Name}");
            }
            return false;
        }


        foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
        {
            if ((selectedItems).Contains(item))
            {
                Debug.Log($"Invalidated due to {item.GetType().Name}");
                return false;
            }
        }
        return true;
    }
}