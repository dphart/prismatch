using System.Collections.Generic;

public class TwinShotPower : PowerUp
{
    private int multi = 1;

    public TwinShotPower()
    {
        Name = "Twin Shot";
        PowerUpDescription = "Match 4 tiles, and your next match counts twice for 2x";
    }

    public override string GetCurrentPowerDescription()
    {
        return "Twin Shot ready: next match counts double for combo bonuses!";
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count == 4)
            {
                multi = 2;
            }
            else
            {
                multi = 1;
            }
        }
        return multi;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new TwinShotPower();
}
