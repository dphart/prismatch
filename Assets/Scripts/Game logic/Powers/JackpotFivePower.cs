using System.Collections.Generic;

public class JackpotFivePower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private bool streakActive = false;
    private int multi = 1;

    public JackpotFivePower()
    {
        Name = "Jackpot Five";
        PowerUpDescription = "Matches of 5 or more give your next match 5x points.";
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
        return MatchInfo?.wasUserInitiatedMatch() == true ? multi : 1;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new JackpotFivePower());
}
