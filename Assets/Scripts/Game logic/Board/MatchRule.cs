using System;
using System.Collections.Generic;
using System.Linq;

public enum MatchType { Horizontal, Vertical, Snake }

[Serializable]
public sealed class MatchRule
{
    public MatchType type = MatchType.Snake;
    public int minimumLength = 3;

    public MatchRule() { }
    public MatchRule(MatchType type, int minimumLength)
    {
        this.type = type;
        this.minimumLength = Math.Max(2, minimumLength);
    }

    public MatchRule Clone() => new(type, minimumLength);
    public override string ToString() => $"{type} {minimumLength}+";

    public static List<MatchRule> Normalize(IEnumerable<MatchRule> rules)
    {
        var result = (rules ?? Enumerable.Empty<MatchRule>())
            .Where(rule => rule != null)
            .GroupBy(rule => rule.type)
            .Select(group => new MatchRule(group.Key, group.Min(rule => Math.Max(2, rule.minimumLength))))
            .ToList();
        return result.Count > 0 ? result : new List<MatchRule> { new(MatchType.Snake, 3) };
    }
}
