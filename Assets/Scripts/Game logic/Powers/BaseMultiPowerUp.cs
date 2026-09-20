using System.Collections.Generic;

public class BaseMultiPowerUp : PowerUp
{
    private int pointsPerTile;
    private int baseMulti;

    public BaseMultiPowerUp(int points, int baseMulti)
    {
        Name = points == 1 ? "Multiplier Start" : "Heavy Start";
        pointsPerTile = points;
        this.baseMulti = baseMulti;

        PowerUpDescription = $"+{pointsPerTile} points per tile and +{baseMulti} multiplier (×{1 + baseMulti} alone).";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * pointsPerTile;

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        return baseMulti;
    }
    public override void Apply(GameState state) { }

    public override PowerUp Clone() => new BaseMultiPowerUp(pointsPerTile, baseMulti);
}
