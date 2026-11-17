using System.Collections.Generic;

public class ConsistencyIsKey : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int additiveMulti = 1;
    public int numberOfTilesPreviouslyMatched = 0;
    public int numberOfMatchesInARow = 0;

    public ConsistencyIsKey()
    {
        Name = "Consistency Is Key";
        PowerUpDescription = $"Each match of the same size as your last adds {additiveMulti} multiplier. A different size resets this bonus.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Streak: {numberOfMatchesInARow}.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        if (tiles.Count == numberOfTilesPreviouslyMatched)
        {
            numberOfMatchesInARow++;
            return numberOfMatchesInARow * additiveMulti;
        }
        else
        {
            numberOfMatchesInARow = 0;
            numberOfTilesPreviouslyMatched = tiles.Count;
            return 0;
        }
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new ConsistencyIsKey());
}
