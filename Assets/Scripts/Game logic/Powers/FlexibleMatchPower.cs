using System.Collections.Generic;

public class FlexibleMatchPower : PowerUp
{
    public int Increase { get; private set; }

    public FlexibleMatchPower(int amount)
    {
        Name = "More Power!";
        PowerUpDescription = $"Receive +{amount} per tile";
        Increase = amount;
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * Increase;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FlexibleMatchPower(Increase);
}
