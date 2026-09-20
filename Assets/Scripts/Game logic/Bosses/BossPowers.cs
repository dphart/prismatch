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
        PowerUpDescription = "Half points during the first half of this round.";
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
        if(TimeProvider.TimeRemaining >= (GameState.Instance?.CurrentLevel?.GetTotalTime ?? 60) / 2f)
        {
            return (score / 2);
        }
        return score;
    }
}

public class FirstItemsGivesNoPoints : BossPowers
{
    List<ItemType> selectedItems = new List<ItemType>();

    public FirstItemsGivesNoPoints()
    {
        Name = "First In None Out";
        PowerUpDescription = "Your first player-created match selects an animal. That animal scores no points while this boss is active.";
    }
    public override void Apply(GameState state) {}
    
    public override PowerUp Clone()
    {
        return new FirstItemsGivesNoPoints();
    }

    public override string GetCurrentPowerDescription()
    {
        return selectedItems.Count == 0 ? PowerUpDescription : $"Blocked animal: {string.Join(", ", selectedItems)}";
    }

    public override bool IsScoreValid(List<Tile> tiles)
    {
        if (selectedItems.Count == 0 && MatchInfo.wasUserInitiatedMatch())
        {
            foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
            {
                selectedItems.Add(item);
            }
            return false;
        }


        foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
        {
            if ((selectedItems).Contains(item))
            {
                return false;
            }
        }
        return true;
    }
}

public static class BossLibrary
{

}

