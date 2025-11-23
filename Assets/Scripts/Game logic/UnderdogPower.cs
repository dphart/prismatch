using System.Collections.Generic;

public class UnderdogPower : PowerUp
{
    private int plusMulti = 2;
    private int maxTiles = 3;

    public UnderdogPower()
    {
        Name = "Smaller is Better";
        PowerUpDescription = $"+{plusMulti} Multi if match is {maxTiles} or fewer tiles.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveMultiplierMod(List<Tile> tiles) =>
        tiles.Count <= maxTiles ? plusMulti : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new UnderdogPower();
}
