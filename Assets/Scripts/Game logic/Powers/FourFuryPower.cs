using System.Collections.Generic;

public class FourFuryPower : PowerUp
{
    private bool streakActive = false;
    private int multi = 1;

    public FourFuryPower()
    {
        Name = "Four Fury";
        PowerUpDescription = "Match 4 tiles to trigger x3 on your next match. Keeps working until you match a different number.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Four Fury active: matching 4 tiles keeps x3 going!"
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
    public override PowerUp Clone() => new FourFuryPower();
}
