using System.Collections.Generic;

public class StackedOddsPower : PowerUp
{
    private int baseMulti = 2;
    private int requiredTiles = 4;

    public StackedOddsPower()
    {
        Name = "Stacked Odds";
        PowerUpDescription = $"x{baseMulti} base for {requiredTiles}+ in a row.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count >= requiredTiles ? baseMulti : 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new StackedOddsPower();
}
