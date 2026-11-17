using System.Collections.Generic;

public class BasePerTile : PowerUp
{
    public override bool HasConditionalTrigger => false;
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => GetCurrentPowerDescription();
    private double turnMatches = 0;
    private double basePointModifier = 0.05;

    public BasePerTile()
    {
        Name = "Momentum Bonus";
        PowerUpDescription = $"Each match: +{basePointModifier} permanent base / tile.";
    }

    public override float FlatBasePerTile => (float)turnMatches;
    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Earned: +{turnMatches:0.##} base / tile.";

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        turnMatches += basePointModifier;
        return (int)(tiles.Count * turnMatches);
    }

    public override void Apply(GameState state) => turnMatches = 0;
    public override PowerUp Clone() => SharedClone(new BasePerTile());
}
