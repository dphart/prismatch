using System.Collections.Generic;
using UnityEngine;

public class PrecisionStrikePower : PowerUp
{
    private bool streakActive = false;
    private int streakCount = 0;
    private int multi = 1;

    public PrecisionStrikePower()
    {
        Name = "Precision Strike";
        PowerUpDescription = "Match exactly 3 twice to activate x4. Keeps working until you match a different number.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Precision Strike active: matching 3 tiles keeps x4 going!"
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo?.wasUserInitiatedMatch() == true)
        {
            if (tiles.Count == 3)
            {
                streakCount++;

                if (streakCount >= 2)
                {
                    streakActive = true;
                    multi = 4;
                }
            }
            else
            {
                streakActive = false;
                streakCount = 0;
                multi = 1;
            }
        }
        return MatchInfo?.wasUserInitiatedMatch() == true ? multi : 1;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new PrecisionStrikePower();
}
