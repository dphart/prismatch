using System.Collections.Generic;
using System.Linq;

public class MatchFoundationPower : PowerUp
{
    public enum FoundationStyle { StreakFive, SpeedFive, GiantSix, Legacy }
    public FoundationStyle Style { get; private set; }
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override bool HasConditionalTrigger => Style != FoundationStyle.Legacy;
    public override bool OpeningOnly => true;
    protected int minimumMatch, pointsPerTile;
    public int Minimum => minimumMatch;
    public int BasePerTile => pointsPerTile;
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Matching | PowerTags.MatchFoundation |
        (Style == FoundationStyle.StreakFive ? PowerTags.Growth | PowerTags.Multiplier :
         Style == FoundationStyle.SpeedFive || Style == FoundationStyle.GiantSix ? PowerTags.Growth | PowerTags.BasePoints : PowerTags.None);
    public override PowerTags OfferTags(FreePlayMode mode) => base.OfferTags(mode) | (RewardGoal==GoalType.BlocksCleared?PowerTags.GoalCredit:PowerTags.None);
    public override PowerStatEffects StatEffects => Style switch {
        FoundationStyle.StreakFive => PowerStatEffects.Multiplier,
        FoundationStyle.SpeedFive => PowerStatEffects.Base,
        FoundationStyle.GiantSix => PowerStatEffects.Base,
        _ => PowerStatEffects.Base
    };
    public override float FlatBasePerTile => Style switch {
        FoundationStyle.StreakFive => 0,
        FoundationStyle.SpeedFive => earnedBase,
        FoundationStyle.GiantSix => earnedBase,
        _ => BasePerTile
    };
    public override float FlatMultiplierBonus => Style == FoundationStyle.StreakFive ? earnedMulti : 0;
    public override string ConditionalStatDescription => GetCurrentPowerDescription();

    private int streak;
    private int earnedMulti;
    private int giantMatches;
    private int earnedBase;
    private readonly List<float> recentMatchTimes = new();

    public int Streak => streak;
    public int EarnedMultiplier => earnedMulti;
    public int GiantMatchCount => giantMatches;
    public int EarnedBasePoints => earnedBase;

    public MatchFoundationPower(FoundationStyle style)
    {
        Style = style;
        switch (style)
        {
            case FoundationStyle.StreakFive:
                minimumMatch = 3; pointsPerTile = 2;
                Name = "Small Steps";
                PowerUpDescription = "You only need 3 tiles to match. Every 4 matches of 5+ in a row add 1 multiplier permanently.";
                break;
            case FoundationStyle.SpeedFive:
                minimumMatch = 3; pointsPerTile = 4;
                Name = "Steady Matches";
                PowerUpDescription = "You only need 3 tiles to match. Make 5 matches in 5 seconds to add 2 base points per tile permanently.";
                break;
            case FoundationStyle.GiantSix:
                minimumMatch = 5; pointsPerTile = 8;
                Name = "Big Commitment";
                PowerUpDescription = "Matches require 5 or more tiles. Every 2 matches of 6+ tiles add 1 base point per tile permanently.";
                break;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(style));
        }
    }

    public MatchFoundationPower(int minimum, int basePerTile)
    {
        if (minimum < 2 || basePerTile < 1) throw new System.ArgumentOutOfRangeException();
        minimumMatch = minimum; pointsPerTile = basePerTile;
        Style = FoundationStyle.Legacy;
        Name = minimum == 3 ? "Small Steps" : minimum == 4 ? "Steady Matches" : "Big Commitment";
        PowerUpDescription = $"All matches gain {basePerTile} base points per tile. You need {minimum} or more tiles to match for this level.";
    }

    public override void ModifyMatchRules(List<MatchRule> rules)
    {
        foreach (var rule in rules) rule.minimumLength = Minimum;
    }

    public override bool CanOfferWith(IReadOnlyList<PowerUp> active) => active == null || active.Count == 0;

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        if (Style == FoundationStyle.StreakFive)
        {
            bool isUserMatch = MatchInfo == null || MatchInfo.wasUserInitiatedMatch();
            if (isUserMatch && tiles != null)
            {
                if (tiles.Count >= 5)
                {
                    streak++;
                    if (streak >= 4)
                    {
                        earnedMulti++;
                        streak = 0;
                    }
                }
                else
                {
                    streak = 0;
                }
            }
            return earnedMulti;
        }
        return 0;
    }

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (Style == FoundationStyle.SpeedFive)
        {
            bool isUserMatch = MatchInfo == null || MatchInfo.wasUserInitiatedMatch();
            if (isUserMatch && tiles != null)
            {
                float now = UnityEngine.Time.time;
                recentMatchTimes.Add(now);
                recentMatchTimes.RemoveAll(t => now - t > 5.0f);
                if (recentMatchTimes.Count >= 5)
                {
                    earnedBase += 2;
                    recentMatchTimes.Clear();
                }
            }
            return (tiles != null ? tiles.Count : 0) * earnedBase;
        }
        if (Style == FoundationStyle.GiantSix)
        {
            bool isUserMatch = MatchInfo == null || MatchInfo.wasUserInitiatedMatch();
            if (isUserMatch && tiles != null && tiles.Count >= 6)
            {
                giantMatches++;
                if (giantMatches >= 2)
                {
                    earnedBase += 1;
                    giantMatches = 0;
                }
            }
            return (tiles != null ? tiles.Count : 0) * earnedBase;
        }
        return Style == FoundationStyle.Legacy ? (tiles != null ? tiles.Count : 0) * BasePerTile : 0;
    }

    public override void Apply(GameState state)
    {
        streak = 0;
        earnedMulti = 0;
        giantMatches = 0;
        earnedBase = 0;
        recentMatchTimes.Clear();
    }

    public override string GetCurrentPowerDescription() => Style switch {
        FoundationStyle.StreakFive => $"{PowerUpDescription} Streak: {streak}/4. Earned: +{earnedMulti} multi.",
        FoundationStyle.SpeedFive => $"{PowerUpDescription} Recent: {recentMatchTimes.Count}/5. Earned: +{earnedBase} base/tile.",
        FoundationStyle.GiantSix => $"{PowerUpDescription} Progress: {giantMatches % 2}/2. Earned: +{earnedBase} base/tile.",
        _ => PowerUpDescription
    };

    public override string MultiplierCondition => Style == FoundationStyle.StreakFive
        ? $"Make 4 matches of 5+ in a row to gain 1 multiplier permanently. Current streak: {streak}/4. Earned: +{earnedMulti} multi."
        : "";

    public override string BaseCondition => Style switch {
        FoundationStyle.SpeedFive => $"Make 5 matches in 5 seconds to gain 2 base points per tile permanently. Progress: {recentMatchTimes.Count}/5. Earned: +{earnedBase} base.",
        FoundationStyle.GiantSix => $"Every 2 matches of 6+ tiles add 1 base point per tile permanently. Progress: {giantMatches % 2}/2. Earned: +{earnedBase} base.",
        _ => GetCurrentPowerDescription()
    };

    public override PowerUp Clone()
    {
        if (Style == FoundationStyle.Legacy)
            return SharedClone(new MatchFoundationPower(Minimum, BasePerTile));
        var copy = new MatchFoundationPower(Style)
        {
            streak = this.streak,
            earnedMulti = this.earnedMulti,
            giantMatches = this.giantMatches,
            earnedBase = this.earnedBase
        };
        copy.recentMatchTimes.AddRange(this.recentMatchTimes);
        return SharedClone(copy);
    }

    public static List<PowerUp> Choices() => new() {
        new MatchFoundationPower(FoundationStyle.StreakFive),
        new MatchFoundationPower(FoundationStyle.SpeedFive),
        new MatchFoundationPower(FoundationStyle.GiantSix)
    };
}

public sealed class BlockMatchFoundationPower : MatchFoundationPower
{
    public int CreditPerTile => BasePerTile;
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags Tags => PowerTags.Collection | PowerTags.GoalCredit | PowerTags.Matching | PowerTags.MatchFoundation;
    public override PowerStatEffects StatEffects => RewardGoal==GoalType.Score?PowerStatEffects.Base:PowerStatEffects.None;
    public override float FlatBasePerTile => RewardGoal==GoalType.Score?CreditPerTile*2:0;
    public BlockMatchFoundationPower(int minimum, int creditPerTile) : base(minimum,creditPerTile)
    {
        Name = minimum == 3 ? "Small Clears" : minimum == 4 ? "Steady Clears" : "Big Clears";
        PowerUpDescription = $"Each matched tile counts as {creditPerTile} blocks. You need {minimum} or more tiles to match for this level. " +
            "Any animal counts. Extra clears still count as 1 block each.";
    }
    public override int AdditiveBaseMod(List<Tile> tiles) => RewardGoal==GoalType.Score?ScoreManager.Multiply(ScoreManager.Multiply(CreditPerTile,2),tiles.Count):0;
    public override PowerUp Clone() => SharedClone(new BlockMatchFoundationPower(Minimum,CreditPerTile));
    public static new List<PowerUp> Choices() => new() {
        new BlockMatchFoundationPower(3,1), new BlockMatchFoundationPower(4,2), new BlockMatchFoundationPower(5,4) };
    public static int ApplyCredit(int eligibleCredit, IEnumerable<PowerUp> active)
    {
        int factor = active.OfType<MatchFoundationPower>().Select(power => power is BlockMatchFoundationPower block?block.CreditPerTile:System.Math.Max(1,power.BasePerTile/2)).DefaultIfEmpty(1).First();
        return (int)System.Math.Min(int.MaxValue, (long)System.Math.Max(0,eligibleCredit) * factor);
    }
}

public class BasePowerUp : PowerUp
{
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override bool HasConditionalTrigger => false;
    public override bool OpeningOnly => true;
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    private int pointsPerTile;

    public BasePowerUp(int points)
    {
        Name = "Steady Start";
        pointsPerTile = points;
        PowerUpDescription = $"All matches gain {pointsPerTile} base points per tile.";
    }

    public override float FlatBasePerTile => pointsPerTile;
    public override string ConditionalStatDescription => "";
    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * pointsPerTile;

    public override void Apply(GameState state) { }

    public override PowerUp Clone() => SharedClone(new BasePowerUp(pointsPerTile));
}
