using System.Collections.Generic;

public class BasePerTile : PowerUp
{
    private double turnMatches = 0;
    private double basePointModifier = 0.05;

    public BasePerTile()
    {
        Name = "Momentum Bonus";
        PowerUpDescription = $"+{basePointModifier} base tile points per match.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Current bonus: +{turnMatches:F1} per tile.";

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        turnMatches += basePointModifier;
        return (int)(tiles.Count * turnMatches);
    }

    public override void Apply(GameState state) => turnMatches = 0;
    public override PowerUp Clone() => new BasePerTile();
}
