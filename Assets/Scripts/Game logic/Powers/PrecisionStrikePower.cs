using System.Collections.Generic;
using UnityEngine;

public class PrecisionStrikePower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private bool streakActive = false;
    private int streakCount = 0;
    private int multi = 1;

    public PrecisionStrikePower()
    {
        Name = "Precision Strike";
        PowerUpDescription = "Match exactly 3 twice in a row to give your next match 4x points. Keep matching 3 to earn it again.";
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
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) => CanMatchExactly(rules, 3);
    public override PowerUp Clone() => SharedClone(new PrecisionStrikePower());
}
