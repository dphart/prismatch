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
    public virtual int AdditiveBaseMod(List<Tile> tiles) => 0;

    // Optional score hook — can override this
    public virtual int MultiplicativeBaseMod(List<Tile> tiles) => 1;

    // Optional score hook — can override this
    public virtual int AdditiveMultiplierMod(List<Tile> tiles) => 0;

    // Optional score hook — can override this
    public virtual int MultiplicativeMultiMod(List<Tile> tiles) => 1;
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
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count * 2;
    }

    public override void Apply(GameState state)
    {
        //throw new System.NotImplementedException();
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

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        int basePoints = GameState.Instance.PlayerConfigs.basePoints;
        return tiles.Count * basePoints;
    }
}


public static class PowerUpLibrary
{
    public static List<PowerUp> All = new List<PowerUp>()
    {
        new FlexibleMatchPower(2),
        new FlexibleMatchPower(3),
        new FlexibleMatchPower(4),

        // Add more here
    };
}
