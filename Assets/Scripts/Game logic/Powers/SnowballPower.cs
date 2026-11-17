using System.Collections.Generic;

public class SnowballPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int matchCount = 0;
    private int baseMultiIncrease = 1;
    private int perXMatches = 10;

    public SnowballPower()
    {
        Name = "Snowball";
        PowerUpDescription = $"Every {perXMatches} matches: +{baseMultiIncrease} permanent multi.";
    }

    public override float FlatMultiplierBonus => (matchCount / perXMatches) * baseMultiIncrease;
    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Earned: +{(matchCount / perXMatches) * baseMultiIncrease} multi. Progress: {matchCount % perXMatches}/{perXMatches}.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matchCount++;
        return (matchCount / perXMatches) * baseMultiIncrease;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new SnowballPower());
}
