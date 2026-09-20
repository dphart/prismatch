using System.Collections.Generic;
using System.Linq;

public sealed class NoVerticalDoublePower : PowerUp
{
    public NoVerticalDoublePower()
    {
        Name = "Sideways Thinking";
        PowerUpDescription = "All matches score ×2, but vertical matches are disabled.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override int CurrentMatchMultiplier(List<Tile> tiles) => 2;
    public override void ModifyMatchRules(List<MatchRule> rules) =>
        rules.RemoveAll(rule => rule.type == MatchType.Vertical);
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        rules.Any(rule => rule.type != MatchType.Vertical);
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new NoVerticalDoublePower();
}
