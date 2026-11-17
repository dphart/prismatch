using System.Collections.Generic;

/// <summary>Counts resolved match groups, and rewards future matches of one animal only.</summary>
public sealed class AnimalMasteryPower : IconBonusPower
{
    public bool GrantsBasePoints { get; }
    public ItemType? Rival { get; }
    public int RequiredMatches => Rival.HasValue ? (GrantsBasePoints ? 3 : 1) : 2;
    public int Progress { get; private set; }
    public int EarnedBonus { get; private set; }
    private int matchBonus;
    public override PowerTags Tags => base.Tags | PowerTags.Growth |
        (GrantsBasePoints ? PowerTags.BasePoints : PowerTags.Multiplier) |
        (Rival.HasValue ? PowerTags.Tradeoff | PowerTags.AnimalPair : PowerTags.None);
    public override PowerStatEffects StatEffects => GrantsBasePoints ? PowerStatEffects.Base : PowerStatEffects.Multiplier;
    public override string BaseCondition => GrantsBasePoints ? GetCurrentPowerDescription() : "";
    public override string MultiplierCondition => GrantsBasePoints ? "" : GetCurrentPowerDescription();
    public AnimalMasteryPower(ItemType animal, bool basePoints, ItemType? rival = null)
    {
        if (rival == animal) throw new System.ArgumentException("The rival must be a different animal.");
        ItemType = animal; GrantsBasePoints = basePoints; Rival = rival;
        Name = $"{animal} {(rival.HasValue ? "Rivalry" : "Mastery")} ({(basePoints ? "Points" : "Multiplier")})";
        PowerUpDescription = rival.HasValue
            ? $"{(basePoints ? $"Every {RequiredMatches} {animal} matches" : $"Each {animal} match")} gives future {animal} matches +1 {(basePoints ? "base / tile" : "multiplier")}. Each {rival.Value} match removes 1 bonus, down to 0. Lasts all run. Cascades count."
            : $"Every {RequiredMatches} {animal} matches: future {animal} matches gain +1 {(basePoints ? "base point per tile" : "multiplier")} this run. Player matches only.";
    }
    public override bool SupportsItems(IList<ItemType> items) => items != null && items.Contains(ItemType) && (!Rival.HasValue || items.Contains(Rival.Value));
    // One call per resolved group, even if a pact lets that group contain both animals.
    public int RecordMatch(bool targetMatched, bool rivalMatched, bool playerMatch = true)
    {
        if(!Rival.HasValue && !playerMatch)return 0;
        int reward = targetMatched ? EarnedBonus : 0;
        if (targetMatched && ++Progress == RequiredMatches)
        {
            EarnedBonus = ScoreManager.SaturatingAdd(EarnedBonus, 1);
            Progress = 0;
        }
        if (Rival.HasValue && rivalMatched) EarnedBonus = System.Math.Max(0, EarnedBonus - 1);
        return reward;
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        int targetTiles = tiles.FindAll(t => t.item.ItemType == ItemType).Count;
        matchBonus = RecordMatch(targetTiles > 0, Rival.HasValue && tiles.Exists(t => t.item.ItemType == Rival.Value),MatchInfo?.wasUserInitiatedMatch()!=false);
        return GrantsBasePoints ? (int)System.Math.Min(int.MaxValue, (long)matchBonus * targetTiles) : 0;
    }
    public override int AdditiveMultiplierMod(List<Tile> tiles) => GrantsBasePoints ? 0 : matchBonus;
    public override string GetCurrentPowerDescription() => PowerUpDescription +
        $"\n{ItemType} bonus: +{EarnedBonus}. Matches: {Progress}/{RequiredMatches}.";
    public override void Apply(GameState state) { Progress = EarnedBonus = matchBonus = 0; }
    public override PowerUp Clone() => SharedClone(new AnimalMasteryPower(ItemType, GrantsBasePoints, Rival));
}

public class IconTypeStreakGivesBasePointsOrMulti : IconBonusPower
{
    public override PowerStatEffects StatEffects => (GrantsBasePoints ? PowerStatEffects.Base : PowerStatEffects.None) | (GrantsMultiplier ? PowerStatEffects.Multiplier : PowerStatEffects.None);
    public override string BaseCondition => GrantsBasePoints ? $"Every {NumRequired} {ItemType} tiles you match add {BasePoints} base points per tile to ALL matches for the rest of the run. Progress: {NumOfMatchesOfType % NumRequired}/{NumRequired}." : "";
    public override string MultiplierCondition => GrantsMultiplier ? $"Every {NumRequired} {ItemType} tiles you match add {BaseMulti} multiplier to ALL matches for the rest of the run. Progress: {NumOfMatchesOfType % NumRequired}/{NumRequired}." : "";
    public bool GrantsBasePoints => BasePoints > 0;
    public bool GrantsMultiplier => BaseMulti > 0;
    public override PowerTags Tags => base.Tags | PowerTags.Growth;
    private int BasePoints;
    private int NumRequired;
    private int BaseMulti;
    private int NumOfMatchesOfType = 0;
    public int MatchedTiles => NumOfMatchesOfType;
    public int RequiredTiles => NumRequired;

    public IconTypeStreakGivesBasePointsOrMulti(ItemType type, int basePoints, int numRequired, int baseMulti)
    {
        var descString = $"Every {numRequired} {type} tiles: +";

        if (basePoints > 0)
        {
            descString += $"{basePoints} base points per tile";
        }
        if (baseMulti > 0)
        {
            if (basePoints > 0)
            {
                descString += $" and ";
            }
            descString += $"{baseMulti} multiplier";
        }

        Name = $"{type} Growth ({(basePoints > 0 ? "Points" : "Multiplier")})";
        PowerUpDescription = descString + " for all matches this run.";
        BasePoints = basePoints;
        ItemType = type;
        NumRequired = numRequired;
        BaseMulti = baseMulti;
    }

    public override float FlatBasePerTile => (NumOfMatchesOfType / NumRequired) * BasePoints;
    public override float FlatMultiplierBonus => (NumOfMatchesOfType / NumRequired) * BaseMulti;
    public override string GetCurrentPowerDescription()
    {
        int upgrades = NumOfMatchesOfType / NumRequired;
        if(UsesBlockReward)return PowerUpDescription+
            $"\nBonus +{upgrades*(GrantsBasePoints?BasePoints:BaseMulti)} {(GrantsBasePoints?"credits":"clears")} | {NumOfMatchesOfType % NumRequired}/{NumRequired} tiles";
        var bonuses = new List<string>();
        if (BasePoints > 0) bonuses.Add($"+{upgrades * BasePoints} base points per tile");
        if (BaseMulti > 0) bonuses.Add($"+{upgrades * BaseMulti} multiplier");
        return PowerUpDescription + "\nBonus: " + string.Join(" / ", bonuses) +
            $" | {NumOfMatchesOfType % NumRequired}/{NumRequired} tiles";
    }

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            if (tile.item.ItemType == ItemType)
            {
                NumOfMatchesOfType++;
            }
        }

        if (BasePoints > 0)
        {
            return (NumOfMatchesOfType / NumRequired) * BasePoints * tiles.Count;
        }
        return 0;
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        if (BaseMulti > 0) { return (NumOfMatchesOfType / NumRequired) * BaseMulti; }
        return 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new IconTypeStreakGivesBasePointsOrMulti(ItemType, BasePoints, NumRequired, BaseMulti));
}
