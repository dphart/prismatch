using System.Collections.Generic;

public class TwinShotPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int multi = 1;

    public TwinShotPower()
    {
        Name = "Twin Shot";
        PowerUpDescription = "Matches that are exactly 4 give your next match double points.";
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo?.wasUserInitiatedMatch() != true) return 1;
        multi = tiles.Count == 4 ? 2 : 1;
        return multi;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new TwinShotPower());
}
