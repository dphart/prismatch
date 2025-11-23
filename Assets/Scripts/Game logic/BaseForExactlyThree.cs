using System.Collections.Generic;

public class BaseForExactlyThree : PowerUp
{
    private int exactMatchNumber = 3;
    private int basePointsAdded = 5;

    public BaseForExactlyThree()
    {
        Name = "Precision Points";
        PowerUpDescription = $"{basePointsAdded} base points if match is exactly {exactMatchNumber}.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) =>
        tiles.Count == exactMatchNumber ? basePointsAdded * tiles.Count : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new BaseForExactlyThree();
}
