using System.Collections.Generic;

public class ConsistencyIsKey : PowerUp
{
    private int additiveMulti = 1;
    public int numberOfTilesPreviouslyMatched = 0;
    public int numberOfMatchesInARow = 0;

    public ConsistencyIsKey()
    {
        Name = "Consistency Is Key";
        PowerUpDescription = $"Each match of the same length as one previous gains a stacking +{additiveMulti} Multi.";
    }

    public override string GetCurrentPowerDescription() =>
        $"Matches of same length add +{additiveMulti} Multi. Current streak: {numberOfMatchesInARow} in a row.";

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
    public override PowerUp Clone() => new ConsistencyIsKey();
}
