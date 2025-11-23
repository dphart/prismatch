using System.Collections.Generic;

public class JackpotFivePower : PowerUp
{
    private bool streakActive = false;
    private int multi = 1;

    public JackpotFivePower()
    {
        Name = "Jackpot Five";
        PowerUpDescription = "Match 5 or more tiles to trigger x5. Keeps working until you match fewer than 5.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Jackpot Five active: keep hitting 5+ for x5!"
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count >= 5)
            {
                streakActive = true;
                multi = 5;
            }
            else
            {
                streakActive = false;
                multi = 1;
            }
        }
        return multi;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new JackpotFivePower();
}
