using System.Collections.Generic;

public class StackedOddsPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => GetCurrentPowerDescription();
    private int baseMulti = 40;
    private int requiredTiles = 4;

    public StackedOddsPower()
    {
        Name = "Stacked Odds";
        PowerUpDescription = $"Matches of exactly {requiredTiles} gain {baseMulti} base points.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count == requiredTiles ? baseMulti : 0;
    }

    public override void Apply(GameState state) { }
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) => CanMatchExactly(rules, requiredTiles);
    public override PowerUp Clone() => SharedClone(new StackedOddsPower());
}
