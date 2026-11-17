using System.Collections.Generic;

public class FourFuryPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private bool streakActive = false;
    private int multi = 1;

    public FourFuryPower()
    {
        Name = "Four Fury";
        PowerUpDescription = "Matches that are exactly 4 give your next match 3x points.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? PowerUpDescription + " Active."
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo?.wasUserInitiatedMatch() == true)
        {
            if (tiles.Count == 4)
            {
                streakActive = true;
                multi = 3;
            }
            else
            {
                streakActive = false;
                multi = 1;
            }
        }
        return MatchInfo?.wasUserInitiatedMatch() == true ? multi : 1;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new FourFuryPower());
}
