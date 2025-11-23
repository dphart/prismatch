using System.Collections.Generic;

public class BasePowerUp : PowerUp
{
    private int pointsPerTile;

    public BasePowerUp(int points)
    {
        Name = "Getting Started";
        pointsPerTile = points;
        PowerUpDescription = $"+{pointsPerTile} points per tile";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * pointsPerTile;

    public override void Apply(GameState state) { }

    public override PowerUp Clone() => new BasePowerUp(pointsPerTile);
}
