using System;
using System.Linq;
using System.Collections.Generic;

public interface LevelConfig 
{
    public List<PowerUp> GetPowerUpsForRound(int round);

    public BossPowerMap GetBossPowersMap();

    public int GetTotalTime { get; }
    public int GetHeight(int round);

    public int GetWidth(int round);
    public int GetTargetScore(int round);

    public int GetTotalRounds { get; }

    public int GetLevelNumber { get; }

    public string GetLevelDescription();

    public LevelConfig CloneNew();

    public bool BossPowersPersist { get; }

    public List<MatchRule> GetMatchRules(int round);

}

// Optional shared settings preserve compatibility with legacy level implementations.
public interface ILevelSettings
{
    GoalType Goal { get; }
    FreePlayMode Mode { get; }
    int Moves { get; }
    ReplacementStrategy Replacement { get; }
}

public interface IRoundPowerOffers
{
    PowerTags RequiredPowerTags(int round);
}

// Immutable authoring objects: safe to share between level definitions and run clones.
public sealed class PowerTagQuery
{
    public PowerTags All { get; }
    public PowerTags Any { get; }
    public PowerTags None { get; }
    public PowerTagQuery(PowerTags all = PowerTags.None, PowerTags any = PowerTags.None, PowerTags none = PowerTags.None)
    {
        if ((all & none) != 0 || (any != PowerTags.None && (any & ~none) == 0))
            throw new ArgumentException("Required tags cannot all be excluded.");
        All = all; Any = any; None = none;
    }
    public bool Matches(PowerTags tags) => (tags & All) == All &&
        (Any == PowerTags.None || (tags & Any) != 0) && (tags & None) == 0;
    public override string ToString() => $"all={All}; any={Any}; none={None}";
}

public enum OfferRepeatPolicy { PreferNew, NewOnly }
public enum OfferShortagePolicy { ShowAvailable, RequireComplete }

public sealed class PowerOfferSlot
{
    public PowerTagQuery Query { get; }
    public int Count { get; }
    public OfferRepeatPolicy Repeats { get; }
    private readonly Func<PowerUp,bool> predicate;
    public bool Matches(PowerUp power,FreePlayMode mode) => Query.Matches(power.OfferTags(mode)) && (predicate==null||predicate(power));
    public PowerOfferSlot(PowerTagQuery query, int count = 1, OfferRepeatPolicy repeats = OfferRepeatPolicy.PreferNew,Func<PowerUp,bool> predicate = null)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
        if (count < 1 || count > 3) throw new ArgumentOutOfRangeException(nameof(count));
        Count = count; Repeats = repeats; this.predicate=predicate;
    }
}

public sealed class PowerOfferDefinition
{
    public string Label { get; }
    public IReadOnlyList<PowerOfferSlot> Slots { get; }
    public OfferShortagePolicy Shortage { get; }
    public int Count => Slots.Sum(s => s.Count);
    public PowerOfferDefinition(string label, IEnumerable<PowerOfferSlot> slots,
        OfferShortagePolicy shortage = OfferShortagePolicy.ShowAvailable)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("An offer needs a player-facing label.");
        var copy = slots?.ToArray() ?? throw new ArgumentNullException(nameof(slots));
        if (copy.Length == 0 || copy.Any(s => s == null) || copy.Sum(s => s.Count) > 3)
            throw new ArgumentException("An offer must request one to three cards.");
        Label = label; Slots = Array.AsReadOnly(copy); Shortage = shortage;
    }
}

public sealed class LevelPowerOffers
{
    private readonly Dictionary<int, PowerOfferDefinition> rounds;
    public IReadOnlyList<PowerTagQuery> ReservedFromOrdinaryOffers { get; }
    public LevelPowerOffers(IReadOnlyDictionary<int, PowerOfferDefinition> rounds,
        IEnumerable<PowerTagQuery> reservedFromOrdinaryOffers = null)
    {
        this.rounds = new Dictionary<int, PowerOfferDefinition>();
        if (rounds == null) throw new ArgumentNullException(nameof(rounds));
        foreach (var pair in rounds)
        {
            if (pair.Key < 1 || pair.Value == null) throw new ArgumentException("Offer rounds must be positive and definitions non-null.");
            this.rounds.Add(pair.Key, pair.Value);
        }
        var reserved = reservedFromOrdinaryOffers?.ToArray() ?? Array.Empty<PowerTagQuery>();
        if (reserved.Any(q => q == null)) throw new ArgumentException("Reservation queries cannot be null.");
        ReservedFromOrdinaryOffers = Array.AsReadOnly(reserved);
    }
    public void ValidateRounds(int totalRounds)
    {
        if (rounds.Keys.Any(r => r > totalRounds)) throw new ArgumentException("A power offer is scheduled after the final round.");
    }
    public PowerOfferDefinition ForRound(int round) => rounds.TryGetValue(round, out var offer) ? offer : null;
    public bool AllowsOrdinary(PowerUp power, FreePlayMode mode) =>
        !ReservedFromOrdinaryOffers.Any(q => q.Matches(power.OfferTags(mode)));
}

public interface IConfiguredPowerOffers { LevelPowerOffers PowerOffers { get; } }

public enum BlockShape { Any, Square, Plus, Horizontal, Vertical, Large }

public readonly struct BlockMatchCell
{
    public readonly BoardModel.Cell Cell;
    public readonly ItemType Animal;
    public BlockMatchCell(int x, int y, ItemType animal) { Cell = new BoardModel.Cell(x, y); Animal = animal; }
}

/// <summary>Goal credit is separate from physical clears. Animal and shape constraints combine with AND.</summary>
public sealed class BlockObjective
{
    public static readonly BlockObjective Any = new();
    public ItemType? Animal { get; }
    public BlockShape Shape { get; }
    public bool IsRestricted => Animal.HasValue || Shape != BlockShape.Any;
    public BlockObjective(ItemType? animal = null, BlockShape shape = BlockShape.Any)
    {
        if (animal.HasValue && !Enum.IsDefined(typeof(ItemType), animal.Value) || !Enum.IsDefined(typeof(BlockShape), shape))
            throw new ArgumentOutOfRangeException(nameof(animal));
        Animal = animal; Shape = shape;
    }
    public string Label => (Animal.HasValue ? Animal + " ONLY" : "ANY ANIMAL") +
        (Shape == BlockShape.Any ? "" : " / " + ShapeLabel(Shape));
    public string Description => "Earn goal credit from " + (Animal.HasValue ? Animal.Value.ToString() + " tiles" : "any animal") +
        (Shape == BlockShape.Any ? "." : " in matches containing " + ShapeLabel(Shape) + ".") +
        " Other matches still clear. Collection powers can add eligible animals or bonus credit.";
    public static string ShapeLabel(BlockShape shape) => shape switch {
        BlockShape.Square => "a 2x2 square", BlockShape.Plus => "a plus shape", BlockShape.Horizontal => "a horizontal line of 3+",
        BlockShape.Vertical => "a vertical line of 3+", BlockShape.Large => "6+ tiles", _ => "any shape" };
    public static bool HasShape(IEnumerable<BoardModel.Cell> source, BlockShape shape)
    {
        var cells = new HashSet<BoardModel.Cell>(source);
        if (shape == BlockShape.Any) return true;
        if (shape == BlockShape.Large) return cells.Count >= 6;
        return cells.Any(c => shape switch {
            BlockShape.Square => cells.Contains(new BoardModel.Cell(c.X + 1, c.Y)) && cells.Contains(new BoardModel.Cell(c.X, c.Y + 1)) && cells.Contains(new BoardModel.Cell(c.X + 1, c.Y + 1)),
            BlockShape.Plus => cells.Contains(new BoardModel.Cell(c.X + 1, c.Y)) && cells.Contains(new BoardModel.Cell(c.X - 1, c.Y)) && cells.Contains(new BoardModel.Cell(c.X, c.Y + 1)) && cells.Contains(new BoardModel.Cell(c.X, c.Y - 1)),
            BlockShape.Horizontal => cells.Contains(new BoardModel.Cell(c.X + 1, c.Y)) && cells.Contains(new BoardModel.Cell(c.X + 2, c.Y)),
            BlockShape.Vertical => cells.Contains(new BoardModel.Cell(c.X, c.Y + 1)) && cells.Contains(new BoardModel.Cell(c.X, c.Y + 2)), _ => false });
    }
    public bool Supports(IReadOnlyList<MatchRule> rules) => rules.Any(r =>
        r.type == MatchType.Snake || Shape == BlockShape.Any || Shape == BlockShape.Large ||
        Shape == BlockShape.Horizontal && r.type == MatchType.Horizontal || Shape == BlockShape.Vertical && r.type == MatchType.Vertical);
    public int Credit(IEnumerable<BlockMatchCell> source, IEnumerable<PowerUp> active, bool bonusClear = false, Action<PowerUp,int> onActivated = null)
    {
        var cells = source.GroupBy(c => c.Cell).Select(g => g.First()).ToList();
        if (bonusClear && Shape != BlockShape.Any || !HasShape(cells.Select(c => c.Cell), Shape)) return 0;
        var powers = active.OfType<CollectionPower>().ToList();
        var admitted = new HashSet<ItemType>(powers.Where(p => p.Style == CollectionStyle.Admission).Select(p => p.Animal));
        int credit = 0;
        var activations=new Dictionary<CollectionPower,int>();
        foreach (var cell in cells)
        {
            if (Animal.HasValue && cell.Animal != Animal.Value && !admitted.Contains(cell.Animal)) continue;
            if(Animal.HasValue && cell.Animal!=Animal.Value)
                foreach(var admission in powers.Where(p=>p.Style==CollectionStyle.Admission&&p.Animal==cell.Animal))
                    activations[admission]=activations.GetValueOrDefault(admission)+1;
            int value = 1;
            if (!bonusClear)
                foreach (var power in powers)
                    if (power.Style == CollectionStyle.AnimalCredit && power.Animal == cell.Animal ||
                        power.Style == CollectionStyle.ShapeCredit && HasShape(cells.Select(c => c.Cell), power.Shape)) {
                        value++;activations[power]=activations.GetValueOrDefault(power)+1;
                    }
            credit = ScoreManager.SaturatingAdd(credit, value);
        }
        foreach(var activation in activations)onActivated?.Invoke(activation.Key,activation.Value);
        return credit;
    }
    public static List<BlockMatchCell> Snapshot(IEnumerable<Tile> tiles) => tiles.Where(t => t != null && t.item != null)
        .Select(t => new BlockMatchCell(t.x, t.y, t.item.ItemType)).ToList();
    public static List<PowerUp> CatalogFor(BlockObjective objective, IEnumerable<PowerUp> active = null)
    {
        var eligibleAnimals = new HashSet<ItemType>((active ?? Enumerable.Empty<PowerUp>()).OfType<CollectionPower>()
            .Where(p => p.Style == CollectionStyle.Admission).Select(p => p.Animal));
        if (objective.Animal.HasValue) eligibleAnimals.Add(objective.Animal.Value);
        return PowerOfferBuilder.Catalog().Where(p => p is not CollectionPower collection ||
            (collection.Style == CollectionStyle.Admission ? objective.Animal.HasValue && collection.Animal != objective.Animal.Value :
            collection.Style == CollectionStyle.ShapeCredit ? collection.Shape == objective.Shape :
            objective.Animal.HasValue && eligibleAnimals.Contains(collection.Animal))).ToList();
    }
}

public sealed class BlockObjectives
{
    private readonly Dictionary<int, BlockObjective> rounds;
    public BlockObjectives(IReadOnlyDictionary<int, BlockObjective> source)
    {
        rounds = new Dictionary<int, BlockObjective>();
        foreach (var pair in source ?? throw new ArgumentNullException(nameof(source)))
        {
            if (pair.Key < 1 || pair.Value == null) throw new ArgumentException("Invalid block objective round.");
            rounds.Add(pair.Key, pair.Value);
        }
    }
    public BlockObjective ForRound(int round) => rounds.TryGetValue(round, out var objective) ? objective : BlockObjective.Any;
    public IEnumerable<ItemType> RequiredAnimals => rounds.Values.Where(o => o.Animal.HasValue).Select(o => o.Animal.Value).Distinct();
    public void Validate(FreePlayConfig settings)
    {
        if (settings.Goal != GoalType.BlocksCleared || rounds.Any(p => p.Key > settings.GetTotalRounds || !p.Value.Supports(settings.GetMatchRules(p.Key))))
            throw new ArgumentException("Block objectives must fit the goal, round count and matching rules.");
        if (rounds.Values.Any(o => o.Shape == BlockShape.Large) &&
            (settings.Matching == MatchType.Horizontal && settings.Width < 6 || settings.Matching == MatchType.Vertical && settings.Height < 6))
            throw new ArgumentException("A six-tile objective needs room for a six-tile match.");
    }
}

public sealed class PowerOfferResult
{
    public IReadOnlyList<PowerUp> Choices { get; }
    public IReadOnlyList<string> Diagnostics { get; }
    public bool IsComplete { get; }
    public PowerOfferResult(IEnumerable<PowerUp> choices, IEnumerable<string> diagnostics, bool complete)
    {
        Choices = Array.AsReadOnly(choices.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        IsComplete = complete;
    }
}

public static class PowerOfferBuilder
{
    public static bool IsEligible(PowerUp power, IList<ItemType> items, GoalType goal, FreePlayMode mode,
        IReadOnlyList<MatchRule> rules, IReadOnlyList<PowerUp> active, int round = 1, bool allowStarterRewards = false) =>
        power != null && SharedGameContent.Enabled(power) && power.Supports(goal, mode) && power.SupportsItems(items) &&
        (!power.OpeningOnly || allowStarterRewards || round == 1 && (active == null || active.Count == 0)) &&
        (active == null || power.CanOfferWith(active)) &&
        (rules == null || power.CanUseWithMatchRules(rules) && PreservesActiveMatches(power, rules, active)) &&
        (power is not IconBonusPower icon || items.Contains(icon.ItemType)) &&
        (power is not BlockClearPower block || block.Style != BlockClearStyle.Animal || items.Contains(block.Animal));

    public static bool PreservesActiveMatches(PowerUp candidate, IReadOnlyList<MatchRule> rules, IReadOnlyList<PowerUp> active)
    {
        var modified = rules.Select(rule => rule.Clone()).ToList();
        candidate.ModifyMatchRules(modified);
        // Directional powers validate their restriction when offered, not after it is active.
        return modified.Count > 0 && (active == null || active.All(power =>
            power is DirectionalMatchPower || power.CanUseWithMatchRules(modified)));
    }

    public static List<PowerUp> Catalog() => PowerUpLibrary.GetAll().Concat(PowerUpLibrary.Starters())
        .Concat(PowerUpLibrary.BlockPowers()).Concat(PowerUpLibrary.BlockStarters()).Concat(new[] { new FlexibleMatchPower(3) })
        .Concat(CollectionPower.Catalog()).Concat(MatchFoundationPower.Choices()).Concat(BlockMatchFoundationPower.Choices())
        .Concat(new PowerUp[]{new BasePowerUp(2),new BiomeOpeningPower(true),new BiomeOpeningPower(false),new SpotlightOpeningPower()})
        .GroupBy(p => p.Name).Select(g => g.First()).ToList();

    // Also serves as the authoring preview: returns exact cards and shortage diagnostics
    // for a supplied ruleset, roster and active build, without mutating any of them.
    public static PowerOfferResult Build(PowerOfferDefinition definition, IEnumerable<PowerUp> catalog,
        ISet<string> selected, IList<ItemType> items, Random random, GoalType goal, FreePlayMode mode,
        IReadOnlyList<MatchRule> rules, IReadOnlyList<PowerUp> active, int round = 1, bool allowStarterRewards = false)
    {
        if (definition == null || catalog == null || selected == null || items == null ||
            random == null || rules == null || active == null) throw new ArgumentNullException("Offer context");
        var eligible = catalog.Where(p=>p!=null).Select(p=>p.Clone().ForGoal(goal))
            .Where(p => IsEligible(p, items, goal, mode, rules, active, round, allowStarterRewards))
            .GroupBy(p => p.Name).Select(g => g.First()).OrderBy(_ => random.Next()).ToList();
        var owned = new HashSet<string>(selected);
        owned.UnionWith(active.Select(p => p.Name));
        var slots = definition.Slots.SelectMany(s => Enumerable.Repeat(s, s.Count)).ToArray();
        var candidates = slots.Select(s => eligible.Where(p => s.Matches(p,mode) &&
            (s.Repeats != OfferRepeatPolicy.NewOnly || !owned.Contains(p.Name)))
            .OrderBy(p => owned.Contains(p.Name) ? 1 : 0).ToList()).ToArray();
        var assigned = new PowerUp[slots.Length];
        var owners = new Dictionary<string, int>();

        // Augmenting paths find a full distinct assignment when one exists.
        // Greedy picking alone can steal the only candidate for a narrower slot.
        bool Assign(int slot, HashSet<string> visited)
        {
            foreach (var power in candidates[slot])
            {
                if (!visited.Add(power.Name)) continue;
                if (!owners.TryGetValue(power.Name, out var prior) || Assign(prior, visited))
                {
                    owners[power.Name] = slot;
                    assigned[slot] = power;
                    return true;
                }
            }
            return false;
        }
        foreach (int slot in Enumerable.Range(0, slots.Length).OrderBy(i => candidates[i].Count))
            Assign(slot, new HashSet<string>());
        var diagnostics = new List<string>();
        for (int i = 0; i < slots.Length; i++)
            if (assigned[i] == null)
                diagnostics.Add($"Slot {i + 1} ({slots[i].Query}): no distinct compatible power remains; {candidates[i].Count} eligible candidates.");
        bool complete = diagnostics.Count == 0;
        var choices = !complete && definition.Shortage == OfferShortagePolicy.RequireComplete
            ? new List<PowerUp>() : assigned.Where(p => p != null).Select(p => p.Clone().ForGoal(goal)).ToList();
        return new PowerOfferResult(choices, diagnostics, complete);
    }
}
