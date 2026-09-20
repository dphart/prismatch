using System.Collections.Generic;

public class FourExactlyPower : PowerUp
{
    private int exactMatchNumber = 4;
    private int basePointsMulti = 2;

    public FourExactlyPower()
    {
        Name = "Four is Good";
        PowerUpDescription = $"x{basePointsMulti} base multi for {exactMatchNumber} in a row.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count == exactMatchNumber ? basePointsMulti : 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FourExactlyPower();
}
