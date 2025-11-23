using System.Collections.Generic;

public class BaseMultiPowerUp : PowerUp
{
    private int pointsPerTile;
    private int baseMulti;

    public BaseMultiPowerUp(int points, int baseMulti)
    {
        Name = "Getting Started";
        pointsPerTile = points;
        this.baseMulti = baseMulti;

        PowerUpDescription = $"+{pointsPerTile} points per tile, x{baseMulti} multiplier";
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
