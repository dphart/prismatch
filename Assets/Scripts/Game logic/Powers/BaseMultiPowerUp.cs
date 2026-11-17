using System.Collections.Generic;

public class BaseMultiPowerUp : PowerUp
{
    public override bool HasConditionalTrigger => false;
    public override bool OpeningOnly => true;
    public override PowerStatEffects StatEffects => PowerStatEffects.Both;
    private int pointsPerTile;
    private int baseMulti;

    public BaseMultiPowerUp(int points, int baseMulti)
    {
        Name = points == 1 ? "Multiplier Start" : "Heavy Start";
        pointsPerTile = points;
        this.baseMulti = baseMulti;

        PowerUpDescription = $"All matches gain {pointsPerTile} base points per tile and {baseMulti} multiplier.";
    }

    public override float FlatBasePerTile => pointsPerTile;
    public override float FlatMultiplierBonus => baseMulti;
    public override string ConditionalStatDescription => "";
    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * pointsPerTile;

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        return baseMulti;
    }
    public override void Apply(GameState state) { }

    public override PowerUp Clone() => SharedClone(new BaseMultiPowerUp(pointsPerTile, baseMulti));
}
