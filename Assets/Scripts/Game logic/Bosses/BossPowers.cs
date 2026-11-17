using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BossStyle
{
    Coach, Lighthouse, Seesaw, Wallflower, CenterStage, BoxTroll, Stretch,
    Oddball, EvenSteven, CascadeSurfer, OpeningAct, CornerCollector, TwinKeeper,
    DuckEmbargo, TrophyHunter, CascadeTax, Jackpot, MostMatched, Overgrowth, Blackout,
    FenceFox, TreasureCrab, TideTurtle, CrowdTax, SmallChange, InnerToll, RepeatToll, MigrationWarden, VaultWarden, StoneKeeper, RescueKeeper, UnstableWinds, CageWarden, NightKeeper
}

/// <summary>Stateless rules make retries, previews and score/block evaluation consistent.</summary>
public sealed class ChallengeBoss : BossPowers
{
    public BossStyle Style { get; }
    public GoalType Goal { get; }
    private int startingMove;
    public static LevelBonus StrategyBonusFor(BossStyle style) => style switch {
        BossStyle.MigrationWarden=>LevelBonus.MigrationSeason, BossStyle.VaultWarden=>LevelBonus.CrackTheVault,
        BossStyle.StoneKeeper=>LevelBonus.SteppingStones, BossStyle.RescueKeeper=>LevelBonus.AnimalRescue,
        BossStyle.NightKeeper=>LevelBonus.Blackout,
        BossStyle.CageWarden=>LevelBonus.GoldenRescue,
        BossStyle.UnstableWinds=>LevelBonus.Crosswinds, _=>LevelBonus.None };
    public bool UsesMoveTax => Style is BossStyle.CrowdTax or BossStyle.SmallChange or BossStyle.InnerToll or BossStyle.RepeatToll;
    public readonly BossMoveTax MoveTax = new();
    public override PowerModes SupportedModes => UsesMoveTax || Style is BossStyle.MigrationWarden or BossStyle.VaultWarden or BossStyle.RescueKeeper or BossStyle.CageWarden ? PowerModes.LimitedMoves : PowerModes.Both;
    public override PowerStatEffects StatEffects => UsesMoveTax || StrategyBonusFor(Style)!=LevelBonus.None && Style!=BossStyle.MigrationWarden ? PowerStatEffects.None : PowerStatEffects.Multiplier;
    public static bool CanAffect(BossStyle style, GoalType goal, FreePlayMode mode, LevelBonus bonus)
    {
        var strategy=StrategyBonusFor(style);
        if(strategy!=LevelBonus.None && (bonus!=strategy || (strategy is LevelBonus.CrackTheVault or LevelBonus.AnimalRescue or LevelBonus.GoldenRescue ? goal!=GoalType.BlocksCleared : goal!=GoalType.Score)))return false;
        if(strategy is LevelBonus.MigrationSeason or LevelBonus.CrackTheVault or LevelBonus.AnimalRescue or LevelBonus.GoldenRescue && mode!=FreePlayMode.LimitedMoves)return false;
        if (style is BossStyle.CrowdTax or BossStyle.SmallChange or BossStyle.InnerToll or BossStyle.RepeatToll && mode != FreePlayMode.LimitedMoves) return false;
        if (style is BossStyle.DuckEmbargo or BossStyle.MostMatched or BossStyle.TreasureCrab && goal != GoalType.Score) return false;
        if (style == BossStyle.Overgrowth && bonus != LevelBonus.GardenPlots) return false;
        if (style == BossStyle.TreasureCrab && bonus != LevelBonus.TreasureTrail) return false;
        return true;
    }
    public string BriefRule => Style switch {
        BossStyle.CrowdTax => "Match the board's most common animal: costs 2 moves.",
        BossStyle.SmallChange => "Make a group of 6+, or your match costs 2 moves.",
        BossStyle.InnerToll => "Touch an outside edge, or your match costs 2 moves.",
        BossStyle.RepeatToll => "Match the same animal twice in a row: costs 2 moves.",
        BossStyle.Lighthouse => "Use 2+ tiles on the rotating lit edge, or earn 25% less.",
        _ => PowerUpDescription
    };
    public override PowerGoals SupportedGoals => Style is BossStyle.VaultWarden or BossStyle.RescueKeeper or BossStyle.CageWarden ? PowerGoals.Blocks :
        Style is BossStyle.DuckEmbargo or BossStyle.MostMatched or BossStyle.TreasureCrab or BossStyle.MigrationWarden or BossStyle.StoneKeeper or BossStyle.UnstableWinds ? PowerGoals.Points : PowerGoals.Both;
    public override string PowerUpDescription {
        get => Goal == GoalType.BlocksCleared && (SupportedGoals & PowerGoals.Blocks) != 0
            ? base.PowerUpDescription.Replace("points","block credit") : base.PowerUpDescription;
        protected set => base.PowerUpDescription = value;
    }
    public override PowerTags Tags => PowerTags.Pattern | PowerTags.Tradeoff;
    public ChallengeBoss(BossStyle style, GoalType goal)
    {
        if (!System.Enum.IsDefined(typeof(BossStyle), style)) throw new System.ArgumentOutOfRangeException(nameof(style));
        Style = style; Goal = goal;
        string reward = goal == GoalType.Score ? "points" : "block credit";
        string rule;
        (Name, rule) = style switch
        {
            BossStyle.NightKeeper => ("Night Keeper", "Matches reveal only replacement tiles. Adjacent tiles stay hidden. The whole board goes black every 5 player matches."),
            BossStyle.CageWarden => ("Cage Warden", "Every tile matched cages one animal instead of every two. Freed gold tiles still give 3x block credit and cannot be caged again."),
            BossStyle.MigrationWarden => ("Migration Warden", "Non-highlighted animals score half points."),
            BossStyle.VaultWarden => ("Vault Warden", "A player match touching any unboosted slot removes all remaining boosts. Cascades are safe."),
            BossStyle.StoneKeeper => ("Stone Keeper", "A player match missing every glowing space removes one glow and leaves no new path."),
            BossStyle.RescueKeeper => ("Rescue Keeper", "Matching a non-marked animal cages a marked animal. Rescue a marked animal or match beside a cage to free it."),
            BossStyle.UnstableWinds => ("Unstable Winds", "Gravity turns to an unpredictable different direction. It stays hidden until the turn."),
            BossStyle.CrowdTax => ("Crowd King", "Matching the most common animal on the board costs 2 moves total. Counts are checked before the swap; tied animals all qualify. Cascades are free; at most 1 extra move per swap."),
            BossStyle.SmallChange => ("Tiny Tyrant", "A successful swap costs 2 moves unless at least one player-created group has 6+ tiles. Cascades are free; failed swaps cost 1 move."),
            BossStyle.InnerToll => ("Border Hippo", "A successful swap costs 2 moves unless a player-created group touches an outside edge. Cascades are free; failed swaps cost 1 move."),
            BossStyle.RepeatToll => ("Echo Panda", "Repeating an animal from your previous successful swap costs 2 moves total. The first match costs 1. Failed swaps do not reset the previous animal. Cascades are free; at most 1 extra move per swap."),
            BossStyle.FenceFox => ("Fence Fox", "Matches touching fewer than 2 different outside edges earn 25% fewer " + reward + "."),
            BossStyle.TreasureCrab => ("Treasure Crab", "Plays that collect no treasure score half points. A ready 5x play keeps full points."),
            BossStyle.TideTurtle => ("Tide Turtle", "Only falling matches touching an outside edge keep full value. Other matches earn 25% fewer " + reward + "."),
            BossStyle.Coach => ("Coach Cub", "Practice matching groups of four. No boss penalty."),
            BossStyle.Overgrowth => ("Overgrowth", "Matches missing every planted space earn 25% fewer " + reward + "."),
            BossStyle.Blackout => ("Blackout", "Matches containing an animal other than Duck or Bear earn 25% fewer " + reward + "."),
            BossStyle.Lighthouse => ("Lighthouse Lynx", "Matches need 2+ tiles on the lit edge, or earn 25% fewer " + reward + ". The edge rotates top, right, bottom, left with each swap attempt."),
            BossStyle.Seesaw => ("Seesaw Seal", "Matches need 2+ tiles on the top edge on odd swap attempts, or the bottom edge on even attempts. Other matches earn 25% fewer " + reward + "."),
            BossStyle.Wallflower => ("Wallflower Wolf", "Matches need at least 3 tiles touching an outside edge, or earn 25% fewer " + reward + "."),
            BossStyle.CenterStage => ("Center Stage Stag", "Groups need 5+ tiles entirely inside the board, or earn 25% fewer " + reward + "."),
            BossStyle.BoxTroll => ("Box Troll", "Matches without a 2x2 square earn 25% fewer " + reward + "."),
            BossStyle.Stretch => ("Stretch Giraffe", "Matches spanning fewer than 4 rows or columns earn 25% fewer " + reward + "."),
            BossStyle.Oddball => ("Oddball Otter", "Groups need an odd number of tiles, at least 5, or earn 25% fewer " + reward + "."),
            BossStyle.EvenSteven => ("Even Steven", "Groups need an even number of tiles, at least 6, or earn 25% fewer " + reward + "."),
            BossStyle.CascadeSurfer => ("Cascade Surfer", "Player-created matches earn 25% fewer " + reward + ". Falling matches keep full value."),
            BossStyle.OpeningAct => ("Opening Act", "On every third swap attempt, player-created groups below 6 tiles earn 25% fewer " + reward + ". Other matches keep full value."),
            BossStyle.CornerCollector => ("Corner Collector", "Matches need at least 2 board corners, or earn 25% fewer " + reward + "."),
            BossStyle.DuckEmbargo => ("Duck Customs", "Ducks give no points. They still clear normally and count toward block goals."),
            BossStyle.MostMatched => ("Popularity Tax", "The animal you match most this round scores no points."),
            BossStyle.TrophyHunter => ("Trophy Hunter", "Groups below 6 tiles earn 25% fewer " + reward + "."),
            BossStyle.CascadeTax => ("Rapids Toll", "Only falling groups of exactly 4 keep full value. Other matches earn 25% fewer " + reward + "."),
            BossStyle.Jackpot => ("High Roller", "Only groups of 8+ on every third swap attempt keep full value. Other matches earn 25% fewer " + reward + "."),
            _ => ("Twin Keeper", "Player-created matches earn 25% fewer " + reward + " unless both swapped tiles match. Cascades keep full value.")
        };
        PowerUpDescription = rule;
    }

    // Pure rule evaluation is shared by points and block goals and is testable without scene objects.
    public static int RewardPercent(BossStyle style, IReadOnlyList<BoardModel.Cell> cells,
        int width, int height, int move, bool playerMatch, bool bothSwapped, bool hasPlanted = false, bool allSpotlightPair = false)
    {
        if (cells == null || cells.Count == 0) return 100;
        cells = cells.Distinct().ToArray();
        move = System.Math.Max(1,move);
        int Count(System.Func<BoardModel.Cell,bool> predicate) => cells.Count(predicate);
        bool Edge(BoardModel.Cell c) => c.X == 0 || c.Y == 0 || c.X == width-1 || c.Y == height-1;
        bool edge = cells.Any(Edge);
        int sides = (cells.Any(c=>c.X==0)?1:0)+(cells.Any(c=>c.X==width-1)?1:0)+
            (cells.Any(c=>c.Y==0)?1:0)+(cells.Any(c=>c.Y==height-1)?1:0);
        bool square = cells.Any(c=>cells.Contains(new BoardModel.Cell(c.X+1,c.Y)) &&
            cells.Contains(new BoardModel.Cell(c.X,c.Y+1)) && cells.Contains(new BoardModel.Cell(c.X+1,c.Y+1)));
        int span = System.Math.Max(cells.Max(c=>c.X)-cells.Min(c=>c.X),cells.Max(c=>c.Y)-cells.Min(c=>c.Y))+1;
        return style switch {
            BossStyle.Coach => 100,
            BossStyle.Wallflower => Count(Edge)>=3 ? 100 : 75,
            BossStyle.Lighthouse => Count(c=>((move-1)%4) switch {
                0=>c.Y==0,1=>c.X==width-1,2=>c.Y==height-1,_=>c.X==0 })>=2 ? 100 : 75,
            BossStyle.Seesaw => Count(c=>c.Y==(move%2==1?0:height-1))>=2 ? 100 : 75,
            BossStyle.CenterStage => !edge && cells.Count>=5 ? 100 : 75,
            BossStyle.BoxTroll => square ? 100 : 75,
            BossStyle.Stretch => span>=4 ? 100 : 75,
            BossStyle.Oddball => cells.Count>=5 && cells.Count%2==1 ? 100 : 75,
            BossStyle.EvenSteven => cells.Count>=6 && cells.Count%2==0 ? 100 : 75,
            BossStyle.CascadeSurfer => !playerMatch ? 100 : 75,
            BossStyle.OpeningAct => move%3==0 && playerMatch && cells.Count<6 ? 75 : 100,
            BossStyle.CornerCollector => Count(c=>(c.X==0||c.X==width-1)&&(c.Y==0||c.Y==height-1))>=2 ? 100 : 75,
            BossStyle.TwinKeeper => !playerMatch || bothSwapped ? 100 : 75,
            BossStyle.TrophyHunter => cells.Count>=6 ? 100 : 75,
            BossStyle.CascadeTax => !playerMatch && cells.Count==4 ? 100 : 75,
            BossStyle.Jackpot => move%3==0 && cells.Count>=8 ? 100 : 75,
            BossStyle.Overgrowth => hasPlanted ? 100 : 75,
            BossStyle.Blackout => allSpotlightPair ? 100 : 75,
            BossStyle.FenceFox => sides>=2 ? 100 : 75,
            BossStyle.TideTurtle => !playerMatch && edge ? 100 : 75,
            _ => 100
        };
    }

    public static int ScaleReward(int value, int percent) => percent == 75 ? ApplyQuarterPenalty(value) :
        (int)System.Math.Min(int.MaxValue, (long)System.Math.Max(0, value) * percent / 100);

    private int Modify(List<Tile> tiles, int value)
    {
        if (tiles == null) return value;
        var cells = tiles.Where(t => t != null).Select(t => new BoardModel.Cell(t.x, t.y)).Distinct().ToList();
        var move = MatchInfo as PlayerMoveInfo;
        int percent = RewardPercent(Style, cells, move?.BoardWidth ?? 7, move?.BoardHeight ?? 7,
            (move?.MoveId ?? 1) - startingMove, MatchInfo?.wasUserInitiatedMatch() != false, move?.BothSwapTilesMatched ?? false,
            tiles.Any(tile => tile != null && tile.IsPlanted),
            tiles.All(tile => tile != null && tile.item != null && (tile.item.ItemType == ItemType.Duck || tile.item.ItemType == ItemType.Bear)));
        return ScaleReward(value, percent);
    }
    public override int ModifyAndReturnScore(List<Tile> tiles, int score) => Modify(tiles, score);
    public ItemType? BlockedAnimal => Style == BossStyle.MostMatched
        ? AnimalHistory?.Leader(ItemInfoProvider?.GetActiveItems?.Select(item => item.ItemType) ?? System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()) : null;
    public override bool IsScoreValid(List<Tile> tiles)
    {
        ItemType? blocked = Style == BossStyle.DuckEmbargo ? ItemType.Duck : BlockedAnimal;
        return !blocked.HasValue || tiles == null || !tiles.Exists(tile => tile != null && tile.item != null && tile.item.ItemType == blocked.Value);
    }
    public override int ModifyBlockCredit(List<Tile> tiles, int blocks) => Modify(tiles, blocks);
    public override void Apply(GameState state) { startingMove = (MatchInfo as PlayerMoveInfo)?.MoveId ?? 0; MoveTax.Reset(); }
    public override string GetCurrentPowerDescription() => Style != BossStyle.MostMatched ? PowerUpDescription :
        PowerUpDescription + (BlockedAnimal.HasValue ? $"\nNo points: {BlockedAnimal.Value}." : "");
    public override PowerUp Clone() => new ChallengeBoss(Style, Goal);
}

public abstract class BossPowers : IconBonusPower
{
    // Keep 75%, rounded up, without overflowing large scores.
    protected static int ApplyQuarterPenalty(int value) => value - value / 4;
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    public virtual int ModifyAndReturnScore(List<Tile> tiles, int score) {  return  score; }
    public virtual int ModifyBlockCredit(List<Tile> tiles, int blocks) => blocks;
    
  
}

public class HalfPointsForFirstHalfOfLevel : BossPowers
{

    public HalfPointsForFirstHalfOfLevel()
    {
        Name = "Slow Down Buddy";
        PowerUpDescription = "25% fewer points during the first half of this round, rounded up.";
    }
    public override void Apply(GameState state){}

    public override PowerUp Clone()
    {
        return new HalfPointsForFirstHalfOfLevel();
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int ModifyAndReturnScore(List<Tile> tiles, int score)
    {
        if(TimeProvider.TimeRemaining >= (GameState.Instance?.CurrentLevel?.GetTotalTime ?? 60) / 2f)
        {
            return ApplyQuarterPenalty(score);
        }
        return score;
    }
}

public class FirstItemsGivesNoPoints : BossPowers
{
    List<ItemType> selectedItems = new List<ItemType>();

    public FirstItemsGivesNoPoints()
    {
        Name = "First In None Out";
        PowerUpDescription = "Your first player-created match selects an animal. That animal scores no points while this boss is active.";
    }
    public override void Apply(GameState state) {}
    
    public override PowerUp Clone()
    {
        return new FirstItemsGivesNoPoints();
    }

    public override string GetCurrentPowerDescription()
    {
        return selectedItems.Count == 0 ? PowerUpDescription : $"Blocked animal: {string.Join(", ", selectedItems)}";
    }

    public override bool IsScoreValid(List<Tile> tiles)
    {
        if (selectedItems.Count == 0 && MatchInfo.wasUserInitiatedMatch())
        {
            foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
            {
                selectedItems.Add(item);
            }
            return false;
        }


        foreach (ItemType item in tiles.Select(i => i._item.ItemType).Distinct())
        {
            if ((selectedItems).Contains(item))
            {
                return false;
            }
        }
        return true;
    }
}

public static class BossLibrary
{
    public static BossPowers ForLevel(int number, GoalType goal = GoalType.Score, BossStyle? style = null, MatchType matching = MatchType.Snake)
    {
        if (style.HasValue) return new ChallengeBoss(style.Value, goal);
        if (number == 0) return new ChallengeBoss(BossStyle.Coach, goal);
        if (number <= 10) return new CampaignBoss(number);
        var selected = (BossStyle)(1 + (number - 11) % 12);
        if (selected == BossStyle.BoxTroll && matching != MatchType.Snake) selected = BossStyle.Stretch;
        return new ChallengeBoss(selected, goal);
    }
}

/// <summary>Final-round challenges affect the level's goal, including block goals.</summary>
public sealed class CampaignBoss : BossPowers
{
    private readonly int level;
    public CampaignBoss(int level)
    {
        this.level = level;
        (Name, PowerUpDescription) = level switch
        {
            1 => ("The Gatekeeper", "Groups smaller than 5 score 25% fewer points, rounded up."),
            2 => ("Line Judge", "Groups of exactly 3 score 25% fewer points, rounded up."),
            3 => ("Sky Sentinel", "Groups below the top row give 25% less block credit, rounded up."),
            4 => ("Corner Guard", "Groups without a corner give 25% less block credit, rounded up."),
            5 => ("Cascade Catcher", "Cascades score 25% fewer points, rounded up."),
            6 => ("Deep Anchor", "Groups above the bottom row give 25% less block credit, rounded up."),
            7 => ("West Watch", "Groups away from the left edge give 25% less block credit, rounded up."),
            8 => ("East Watch", "Groups away from the right edge score 25% fewer points, rounded up."),
            9 => ("Longline", "Groups smaller than 4 score 25% fewer points, rounded up."),
            _ => ("The Crown", "Groups smaller than 6 and cascades score 25% fewer points, rounded up.")
        };
    }
    private bool Penalized(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0) return false;
        int width = (MatchInfo as PlayerMoveInfo)?.BoardWidth ?? 7;
        int height = (MatchInfo as PlayerMoveInfo)?.BoardHeight ?? 7;
        return level switch
        {
            1 => tiles.Count < 5,
            2 => tiles.Count == 3,
            3 => !tiles.Exists(t => t.y == 0),
            4 => !tiles.Exists(t => (t.x == 0 || t.x == width - 1) && (t.y == 0 || t.y == height - 1)),
            5 => MatchInfo?.wasUserInitiatedMatch() == false,
            6 => !tiles.Exists(t => t.y == height - 1),
            7 => !tiles.Exists(t => t.x == 0),
            8 => !tiles.Exists(t => t.x == width - 1),
            9 => tiles.Count < 4,
            _ => tiles.Count < 6 || MatchInfo?.wasUserInitiatedMatch() == false
        };
    }
    public override int ModifyAndReturnScore(List<Tile> tiles, int score) => Penalized(tiles) ? ApplyQuarterPenalty(score) : score;
    public override int ModifyBlockCredit(List<Tile> tiles, int blocks) => Penalized(tiles) ? ApplyQuarterPenalty(blocks) : blocks;
    public override void Apply(GameState state) { }
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override PowerUp Clone() => new CampaignBoss(level);
}


/// <summary>Collect all initial groups, then charge at most one move after the entire swap resolves.</summary>
public sealed class BossMoveTax
{
    private readonly HashSet<ItemType> previous = new(), current = new();
    private HashSet<ItemType> common = new();
    private bool large, edge, charged;
    public static IReadOnlyList<ItemType> MostCommon(IEnumerable<ItemType> animals)
    {
        var counts = animals.GroupBy(a => a).ToArray();
        if (counts.Length == 0) return System.Array.Empty<ItemType>();
        int max = counts.Max(g => g.Count());
        return counts.Where(g => g.Count() == max).Select(g => g.Key).OrderBy(a => (int)a).ToArray();
    }
    public void Reset() { previous.Clear(); BeginPlay(System.Array.Empty<ItemType>()); }
    public void BeginPlay(IEnumerable<ItemType> animals)
    {
        common = new HashSet<ItemType>(MostCommon(animals));
        current.Clear(); large = edge = charged = false;
    }
    public void Observe(IEnumerable<ItemType> animals, IReadOnlyList<BoardModel.Cell> cells, int width, int height, bool playerMatch)
    {
        if (!playerMatch || cells == null || cells.Count == 0) return;
        current.UnionWith(animals);
        large |= cells.Distinct().Count() >= 6;
        edge |= cells.Any(c => c.X == 0 || c.Y == 0 || c.X == width - 1 || c.Y == height - 1);
    }
    public int FinishPlay(BossStyle style)
    {
        if (charged) return 0;
        charged = true;
        if (current.Count == 0) return 0;
        bool tax = style switch {
            BossStyle.CrowdTax => current.Overlaps(common),
            BossStyle.SmallChange => !large,
            BossStyle.InnerToll => !edge,
            BossStyle.RepeatToll => current.Overlaps(previous),
            _ => false
        };
        previous.Clear(); previous.UnionWith(current);
        return tax ? 1 : 0;
    }
    public string RepeatTargets => previous.Count == 0 ? "First match: 1 move" : "2 moves: " + string.Join(", ", previous.OrderBy(a => (int)a));
}
