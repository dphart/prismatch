using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BossPowers : IconBonusPower
{
    public virtual int ModifyAndReturnScore(List<Tile> tiles, int score) {  return  score; }
    
  
}

public class HalfPointsForFirstHalfOfLevel : BossPowers
{

    public HalfPointsForFirstHalfOfLevel()
    {
        Name = "Slow Down Buddy";
        PowerUpDescription = $"You receive half points for the first 30 seconds";
    }
    public override void Apply(GameState state){}

    public override PowerUp Clone()
    {
        return new HalfPointsForFirstHalfOfLevel();
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int ModifyAndReturnScore(List<Tile> tiles, int score)
    {
        if(TimeProvider.TimeRemaining >= 30)
        {
            Debug.Log($"Time remaining: {TimeProvider.TimeRemaining}, returning {score / 2}");

            return (score / 2);
        }
        Debug.Log($"Time remaining: {TimeProvider.TimeRemaining}, Not modifying");

        return score;
    }
}

public class FirstItemsGivesNoPoints : BossPowers
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
        Debug.Log($"You matched {tiles.First()._item.ItemType}");

        if (selectedItems.Count == 0 && MatchInfo.wasUserInitiatedMatch())
        {
            foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
            {
                selectedItems.Add(item);
                Debug.Log($"Adding Type {item}");
            }
            return false;
        }


        foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
        {
            Debug.Log($"Checking {item}");

            if ((selectedItems).Contains(item))
            {
                Debug.Log($"Invalidated due to {item}");
                return false;
            }
        }
        return true;
    }
}

public static class BossLibrary
{

}

