using System.Collections.Generic;
using System.Linq;

public abstract class DirectionalMatchPower : PowerUp
{
    public abstract MatchType Direction { get; }
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Matching | PowerTags.Tradeoff | PowerTags.Premium;

    public override float FlatMultiplierFactor => 4;
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override int CurrentMatchMultiplier(List<Tile> tiles) => 4;
    public override void ModifyMatchRules(List<MatchRule> rules)
    {
        int minimum = rules.Where(rule => rule.type == Direction || rule.type == MatchType.Snake)
            .Select(rule => rule.minimumLength).DefaultIfEmpty(4).Min();
        rules.Clear();
        rules.Add(new MatchRule(Direction, minimum));
    }
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        rules != null && rules.Any(rule => rule.type == MatchType.Snake);
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active) =>
        active == null || !active.Any(power => power is DirectionalMatchPower);
    public override void Apply(GameState state) { }
}

// Keep the existing type for saved references and card artwork.
public sealed class NoVerticalDoublePower : DirectionalMatchPower
{
    public override MatchType Direction => MatchType.Horizontal;
    public NoVerticalDoublePower()
    {
        Name = "Sideways Thinking";
        PowerUpDescription = "Only straight rows can match. All matches score 4x points. The match size stays the same.";
    }
    public override PowerUp Clone() => SharedClone(new NoVerticalDoublePower());
}

public sealed class VerticalMatchPower : DirectionalMatchPower
{
    public override MatchType Direction => MatchType.Vertical;
    public VerticalMatchPower()
    {
        Name = "Skyward Thinking";
        PowerUpDescription = "Only straight columns can match. All matches score 4x points. The match size stays the same.";
    }
    public override PowerUp Clone() => SharedClone(new VerticalMatchPower());
}
