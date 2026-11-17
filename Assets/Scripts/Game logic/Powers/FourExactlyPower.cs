using System.Collections.Generic;

public class FourExactlyPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int exactMatchNumber = 4;
    // Keep the serialized tuning key used by existing shared power definitions.
    private int basePointsMulti = 5;

    public FourExactlyPower()
    {
        Name = "Four is Good";
        PowerUpDescription = $"Matches that are exactly {exactMatchNumber} gain {basePointsMulti} multiplier.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        return tiles.Count == exactMatchNumber ? basePointsMulti : 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new FourExactlyPower());
}
