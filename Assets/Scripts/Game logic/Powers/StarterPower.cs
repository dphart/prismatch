using System.Collections.Generic;
using System.Linq;

public enum HabitatStyle { Shore, Rapids, Giants, Menagerie, Garden }

/// <summary>Three distinct rewards per habitat; premium cards reward the steeper campaign runs.</summary>
public sealed class HabitatPower : PowerUp
{
    public HabitatStyle Style { get; }
    public int Tier { get; }
    private ItemType? previous;
    private float growth;
    private readonly HashSet<ItemType> matchedAnimals = new();
    public static PowerTags ThemeTag(HabitatStyle style) => style switch {
        HabitatStyle.Shore => PowerTags.Edge, HabitatStyle.Rapids => PowerTags.Cascade,
        HabitatStyle.Giants => PowerTags.LargeMatch, HabitatStyle.Menagerie => PowerTags.Variety,
        _ => PowerTags.Square };
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Pattern | ThemeTag(Style) |
        (Tier == 2 ? PowerTags.Premium | PowerTags.Growth : PowerTags.None);
    public override PowerStatEffects StatEffects => Tier == 0 ? PowerStatEffects.Base : PowerStatEffects.Multiplier;
    public override float FlatMultiplierBonus => growth;
    public override string BaseCondition => Tier != 0 ? "" : Style == HabitatStyle.Shore
        ? "Matched tiles gain 10 base points per side touching the board edge. Cascades count."
        : Trigger + " gain 10 base points per tile.";
    public override string MultiplierCondition => Style == HabitatStyle.Menagerie && Tier == 2
        ? "Match every animal to gain +1 multiplier permanently."
        : Tier == 0 ? "" : Trigger + (Tier == 1 ? " gain 5 multiplier." :
        Style == HabitatStyle.Rapids ? " add 0.5 multiplier permanently." : " you make add 0.5 multiplier permanently.");
    private string Trigger => Style switch {
        HabitatStyle.Shore => "Matches touching the edge", HabitatStyle.Rapids => "Matches made by falling tiles",
        HabitatStyle.Giants => "Matches of 6 or more", HabitatStyle.Menagerie => "Matches of a different animal than your last match",
        _ => "Matches with a 2x2 square" };
    public HabitatPower(HabitatStyle style, int tier)
    {
        if (tier < 0 || tier > 2) throw new System.ArgumentOutOfRangeException(nameof(tier));
        Style = style; Tier = tier;
        string habitat = style switch { HabitatStyle.Shore => "Shore", HabitatStyle.Rapids => "Rapids",
            HabitatStyle.Giants => "Giant", HabitatStyle.Menagerie => "Menagerie", _ => "Garden" };
        Name = habitat + (tier == 0 ? " Bounty" : tier == 1 ? " Amplifier" : " Engine");
        PowerUpDescription = tier == 0 ? BaseCondition : MultiplierCondition;
        if (style == HabitatStyle.Rapids && tier == 2)
            PowerUpDescription = "Matches made by falling tiles add 0.5 multiplier permanently.";
    }
    private bool Triggered(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0) return false;
        var move = MatchInfo as PlayerMoveInfo;
        return Style switch {
            HabitatStyle.Shore => move != null && tiles.Exists(t => t.x == 0 || t.y == 0 || t.x == move.BoardWidth - 1 || t.y == move.BoardHeight - 1),
            HabitatStyle.Rapids => MatchInfo != null && !MatchInfo.wasUserInitiatedMatch(),
            HabitatStyle.Giants => tiles.Count >= 6,
            HabitatStyle.Menagerie => previous.HasValue && previous.Value != tiles[0].item.ItemType,
            _ => TacticalStarter.CountShapes(tiles, false) > 0 };
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (Tier != 0 || tiles == null) return 0;
        if (Style != HabitatStyle.Shore) return Triggered(tiles) ? tiles.Count * 10 : 0;
        if (MatchInfo is not PlayerMoveInfo board) return 0;
        int contacts = 0;
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            if (tile.x == 0) contacts++;
            if (tile.x == board.BoardWidth - 1) contacts++;
            if (tile.y == 0) contacts++;
            if (tile.y == board.BoardHeight - 1) contacts++;
        }
        return contacts * 10;
    }
    public override float FractionalMultiplierMod(List<Tile> tiles)
    {
        if (Style == HabitatStyle.Menagerie && Tier == 2)
        {
            if (MatchInfo?.wasUserInitiatedMatch() == true && tiles != null && tiles.Count > 0)
                RecordMenagerieMatch(tiles);
            return growth;
        }
        bool triggered = Triggered(tiles);
        if (Tier == 2 && triggered && (Style == HabitatStyle.Rapids || MatchInfo?.wasUserInitiatedMatch() == true)) growth += .5f;
        if (Style == HabitatStyle.Menagerie && tiles != null && tiles.Count > 0) previous = tiles[0].item.ItemType;
        return growth + (Tier == 1 && triggered ? 5 : 0);
    }
    private void RecordMenagerieMatch(List<Tile> tiles)
    {
        var roster = ItemInfoProvider?.GetActiveItems;
        if (roster == null) return;
        var requiredAnimals = roster.Where(item => item != null).Select(item => item.ItemType).ToHashSet();
        matchedAnimals.IntersectWith(requiredAnimals);
        foreach (var tile in tiles)
            if (tile != null && tile.item != null && requiredAnimals.Contains(tile.item.ItemType))
                matchedAnimals.Add(tile.item.ItemType);
        if (requiredAnimals.Count > 0 && matchedAnimals.IsSupersetOf(requiredAnimals))
        {
            growth += 1;
            matchedAnimals.Clear();
        }
    }
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        Style != HabitatStyle.Garden || (rules != null && System.Linq.Enumerable.Any(rules, r => r.type == MatchType.Snake));
    public override string GetCurrentPowerDescription() => PowerUpDescription + (Tier == 2 ? $" Bonus now: +{growth:0.##} "+(UsesBlockReward?"clears.":"multiplier.") : "");
    public override void OnRoundStarted() { previous = null; }
    public override void Apply(GameState state) { growth = 0; previous = null; matchedAnimals.Clear(); }
    public override PowerUp Clone() => SharedClone(new HabitatPower(Style, Tier));
}

public enum StarterStrategy { Precision, Momentum }
public sealed class StarterPower : PowerUp
{
    public override bool OpeningOnly => true;
    private readonly StarterStrategy strategy;
    public StarterStrategy Strategy => strategy;
    public override PowerStatEffects StatEffects => PowerStatEffects.Both;
    public override string MultiplierCondition => strategy == StarterStrategy.Precision ? "Matches that are exactly 3 gain 1 multiplier." : $"Every 5 matches add 1 multiplier permanently. Progress: {matches % 5}/5.";
    public override float FlatBasePerTile => 3;
    public override float FlatMultiplierBonus => strategy == StarterStrategy.Momentum ? matches / 5 : 0;
    private int matches;
    public StarterPower(StarterStrategy strategy)
    {
        this.strategy = strategy;
        Name = strategy == StarterStrategy.Precision ? "Precision Start" : "Momentum Start";
        PowerUpDescription = strategy == StarterStrategy.Precision
            ? "All matches gain 3 base points per tile. Matches that are exactly 3 gain 1 multiplier."
            : "All matches gain 3 base points per tile. Every 5 matches add 1 multiplier permanently.";
    }
    public override int AdditiveBaseMod(List<Tile> tiles) => 3 * tiles.Count;
    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matches++;
        return strategy == StarterStrategy.Precision ? (tiles.Count == 3 ? 1 : 0) : matches / 5;
    }
    public override string GetCurrentPowerDescription() => PowerUpDescription +
        (strategy == StarterStrategy.Momentum ? $" Earned: +{matches / 5} multi. Progress: {matches % 5}/5." : "");
    public override void Apply(GameState state) { matches = 0; }
    public override PowerUp Clone() => SharedClone(new StarterPower(strategy));
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        strategy != StarterStrategy.Precision || CanMatchExactly(rules, 3);
}

public enum TacticalStyle { Square, Plus, Large, FourStreak, Opening, DoubleSwap, Alternating, Edge }

/// <summary>Deliberate player matches earn bonuses; cascades receive earned growth but cannot advance it.</summary>
public sealed class TacticalStarter : PowerUp
{
    public override bool OpeningOnly => true;
    public TacticalStyle Style { get; }
    public override PowerStatEffects StatEffects => PowerStatEffects.Both;
    public override string MultiplierCondition =>
        (Style == TacticalStyle.Square ? "Each 2x2 square you match adds 0.5 multiplier permanently." :
         Style == TacticalStyle.Plus ? "Each plus shape you match adds 0.5 multiplier permanently." :
         TriggerText + " to gain " + RewardText.TrimStart('+') + " multiplier permanently.") + " Only matches you make count.";
    public override float FlatBasePerTile => 4;
    public override float FlatMultiplierBonus => growth;
    private float growth;
    private int streak, progress, lastMove = -1;
    private bool opened;
    private ItemType? previousAnimal;
    public bool IsGrowth => true;
    public string TriggerText => Style switch {
        TacticalStyle.Square => "Match a 2x2 square", TacticalStyle.Plus => "Match a plus shape",
        TacticalStyle.Large => "Match 6 or more", TacticalStyle.FourStreak => "Make 4 matches of exactly 4 in a row",
        TacticalStyle.Opening => "Match 6 or more on your first match each round",
        TacticalStyle.DoubleSwap => "Make both swapped tiles match",
        TacticalStyle.Alternating => "Switch animals 3 times", _ => "Make 3 matches touching the edge" };
    public string RewardText => Style == TacticalStyle.Opening || Style == TacticalStyle.FourStreak ? "+1" : "+0.5";
    public string IconLabel => Style switch {
        TacticalStyle.Square => "2x2", TacticalStyle.Plus => "+", TacticalStyle.Large => "6+",
        TacticalStyle.FourStreak => "4x4", TacticalStyle.Opening => "1st",
        TacticalStyle.DoubleSwap => "DUAL", TacticalStyle.Alternating => "A/B", _ => "EDGE" };
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Pattern | PowerTags.Starter |
        (IsGrowth ? PowerTags.Growth : PowerTags.None) |
        (Style == TacticalStyle.Square ? PowerTags.Square : Style == TacticalStyle.Edge ? PowerTags.Edge :
            Style == TacticalStyle.Large || Style == TacticalStyle.Opening ? PowerTags.LargeMatch : PowerTags.None);
    public TacticalStarter(TacticalStyle style)
    {
        Style = style;
        Name = style switch {
            TacticalStyle.Square => "Box Builder", TacticalStyle.Plus => "Plus Architect",
            TacticalStyle.Large => "Big Game", TacticalStyle.FourStreak => "Four by Four",
            TacticalStyle.Opening => "Opening Gambit", TacticalStyle.DoubleSwap => "Double Play",
            TacticalStyle.Alternating => "Variety Show", _ => "Border Patrol" };
        PowerUpDescription = "All matches gain 4 base points per tile.\n" + MultiplierCondition;

    }
    public override int AdditiveBaseMod(List<Tile> tiles) => 4 * tiles.Count;
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules)
    {
        foreach (var rule in rules)
        {
            if (Style == TacticalStyle.Square || Style == TacticalStyle.Plus)
            { if (rule.type == MatchType.Snake) return true; }
            else if (Style != TacticalStyle.FourStreak || rule.minimumLength <= 4) return true;
        }
        return false;
    }
    public override float FractionalMultiplierMod(List<Tile> tiles)
    {
        if (MatchInfo == null || !MatchInfo.wasUserInitiatedMatch()) return growth;
        var move = MatchInfo as PlayerMoveInfo;

        switch (Style)
        {
            case TacticalStyle.Square: growth += .5f * CountShapes(tiles, false); break;
            case TacticalStyle.Plus: growth += .5f * CountShapes(tiles, true); break;
            case TacticalStyle.Large: if (tiles.Count >= 6) growth += .5f; break;
            case TacticalStyle.FourStreak:
                // Multiple matching groups in a swap never count as multiple consecutive swaps.
                if (tiles.Count != 4 || (move != null && !move.AllPlayerGroupsExactlyFour)) streak = 0;
                else if (move == null || move.MoveId != lastMove)
                { if (++streak == 4) { growth += 1; streak = 0; } }
                if (move != null) lastMove = move.MoveId;
                break;
            case TacticalStyle.Opening:
                if (!opened) { if (tiles.Count >= 6) growth += 1; opened = true; }
                break;
            case TacticalStyle.DoubleSwap:
                if (move != null && move.BothSwapTilesMatched && move.MoveId != lastMove)
                { growth += .5f; lastMove = move.MoveId; }
                break;
            case TacticalStyle.Alternating:
                var animal = tiles[0].item.ItemType;
                if (previousAnimal.HasValue && previousAnimal.Value != animal && ++progress == 3) { growth += .5f; progress = 0; }
                previousAnimal = animal; break;
            case TacticalStyle.Edge:
                if (move != null && tiles.Exists(t => t.x == 0 || t.y == 0 || t.x == move.BoardWidth - 1 || t.y == move.BoardHeight - 1))
                { if (++progress == 3) { growth += .5f; progress = 0; } }
                break;
        }
        return growth;
    }
    public static int CountShapes(List<Tile> tiles, bool plus)
    {
        var cells = new HashSet<(int, int)>();
        foreach (var tile in tiles) cells.Add((tile.x, tile.y));
        int count = 0;
        foreach (var cell in cells)
        {
            int x = cell.Item1, y = cell.Item2;
            if (cells.Contains((x + 1, y)) && cells.Contains((x, y + 1)) &&
                (plus ? cells.Contains((x - 1, y)) && cells.Contains((x, y - 1)) : cells.Contains((x + 1, y + 1)))) count++;
        }
        return count;
    }
    public override void OnRoundStarted() { streak = progress = 0; opened = false; previousAnimal = null; lastMove = -1; }
    public override void Apply(GameState state) { growth = 0; OnRoundStarted(); }
    public override PowerUp Clone() => SharedClone(new TacticalStarter(Style));
    public override string GetCurrentPowerDescription() => PowerUpDescription +
        (IsGrowth ? $"\nBonus now: +{growth:0.##} multiplier." : "") +
        (Style == TacticalStyle.FourStreak ? $" Streak: {streak}/4." : "") +
        (Style == TacticalStyle.Edge || Style == TacticalStyle.Alternating ? $" Progress: {progress}/3." : "");
}
