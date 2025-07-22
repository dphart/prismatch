using System.Collections.Generic;
using UnityEngine;

public abstract class PowerUp
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }

    public Sprite Icon { get; protected set; } // ✅ image for UI


    // Called once when the player selects this power-up
    public abstract void Apply(GameState state);

    // Optional: used to show live impact per turn or update
    public virtual void Tick(GameState state) { }

    // Optional score hook — can override this
    public virtual int ModifyScore(List<Tile> tiles, int current) => current;
}

public class FlexibleMatchPower : PowerUp
{
    public int increase;

    public FlexibleMatchPower(int amount)
    {
        Name = "More Power!";
        Description = $"Receive +{amount} per tile";
        increase = amount;
    }

    public override void Apply(GameState state)
    {
        state.PlayerConfigs.basePoints = state.PlayerConfigs.basePoints + 2;
    }

}

public class AddDiagonalScoring : PowerUp
{
    public AddDiagonalScoring()
    {
        Name = "Diagonal Scoring";
        Description = "You can now score diagonally.";
    }

    public override void Apply(GameState state)
    {
        if (!state.Scorables.scoringMethods.Contains(ScoringDirection.Diagonol))
            state.Scorables.scoringMethods.Add(ScoringDirection.Diagonol);
    }
}
public class BasePowerUp : PowerUp
{
    public BasePowerUp()
    {
        Name = "Base Scoring";
        Description = "Each tile match gives base points.";
        Icon = null; // Optional: assign a sprite if needed
    }

    public override void Apply(GameState state)
    {
        // No special logic, just base scoring
    }

    public override int ModifyScore(List<Tile> tiles, int current)
    {
        int basePoints = GameState.Instance.PlayerConfigs.basePoints;
        return tiles.Count * basePoints;
    }
}


public static class PowerUpLibrary
{
    public static List<PowerUp> All = new List<PowerUp>()
    {
        new AddDiagonalScoring(),
        new FlexibleMatchPower(2),
        new BasePowerUp(),
        // Add more here
    };
}
