using System.Collections.Generic;

public class UnderdogPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int plusMulti = 2;
    private int maxTiles = 3;

    public UnderdogPower()
    {
        Name = "Smaller is Better";
        PowerUpDescription = $"Matches of {maxTiles} or fewer gain {plusMulti} multiplier.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveMultiplierMod(List<Tile> tiles) =>
        tiles.Count <= maxTiles ? plusMulti : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new UnderdogPower());
}
