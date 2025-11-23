using System.Collections.Generic;

public class SnowballPower : PowerUp
{
    private int matchCount = 0;
    private int baseMultiIncrease = 1;
    private int perXMatches = 10;

    public SnowballPower()
    {
        Name = "Snowball";
        PowerUpDescription = $"+{baseMultiIncrease} multiplier per {perXMatches} matches.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Current bonus: +{(matchCount / perXMatches) * baseMultiIncrease} Multi from {matchCount} matches.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matchCount++;
        return (matchCount / perXMatches) * baseMultiIncrease;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new SnowballPower();
}
