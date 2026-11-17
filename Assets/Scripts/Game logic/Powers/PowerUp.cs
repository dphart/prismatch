using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum PowerTags
{
    None = 0, Scoring = 1, Clearing = 2, Animal = 4, Growth = 8, Matching = 16,
    Tradeoff = 32, Pattern = 64, Utility = 128,
    Roster = 256, AddAnimal = 512, RemoveAnimal = 1024, Budget = 2048,
    TimeReward = 4096, MoveReward = 8192, Shuffle = 16384, Starter = 32768,
    Square = 65536, Edge = 131072, LargeMatch = 262144,
    AnimalPair = 524288, BasePoints = 1048576, Multiplier = 2097152,
    Cascade = 4194304, Variety = 8388608, Premium = 16777216,
    Collection = 33554432, Transform = 67108864, Admission = 134217728, GoalCredit = 268435456,
    AnimalBaseDouble = 536870912, MatchFoundation = 1073741824
}

[Flags]
public enum PowerGoals { Points = 1, Blocks = 2, Both = Points | Blocks }
[Flags]
public enum PowerModes { Timed = 1, LimitedMoves = 2, Both = Timed | LimitedMoves }

[Flags]
public enum PowerStatEffects { None = 0, Base = 1, Multiplier = 2, Both = Base | Multiplier }

[Flags]
public enum PowerTriggerDisplay { None = 0, Icon = 1, MatchedTiles = 2, IconAndMatchedTiles = Icon | MatchedTiles }

public static class PowerTriggerFeedback
{
    // Missing metadata retains the existing presentation for older content.
    public static PowerTriggerDisplay Parse(string value, PowerTriggerDisplay fallback = PowerTriggerDisplay.IconAndMatchedTiles) => value switch
    {
        "none" => PowerTriggerDisplay.None,
        "icon" => PowerTriggerDisplay.Icon,
        "tiles" => PowerTriggerDisplay.MatchedTiles,
        "iconAndTiles" => PowerTriggerDisplay.IconAndMatchedTiles,
        _ => fallback
    };
}

public enum PowerTier { Basic, Rare, Legendary }

public abstract class PowerUp
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public TimeProvider TimeProvider;
    public LevelInfoProvider LevelInfoProvider;
    public ItemInfoProvider ItemInfoProvider;
    public MatchInfo MatchInfo;
    public RoundAnimalHistory AnimalHistory;
    private string nativeName, nativeDescription;
    private Newtonsoft.Json.Linq.JObject sharedDefinition;
    private bool sharedConfigured;
    public string NativeContentKey => GetType().Name + ":" + nativeName;
    public string SharedDefinitionId { get; private set; }
    internal PowerUp WithSharedDefinition(string id) { SharedDefinitionId=id; sharedConfigured=false; ConfigureSharedContent(); return this; }
    protected PowerUp SharedClone(PowerUp clone) { clone.nativeName=nativeName; clone.SharedDefinitionId=SharedDefinitionId; clone.sharedConfigured=false; clone.ForGoal(RewardGoal); clone.ConfigureSharedContent(); return clone; }
    public void ConfigureSharedContent()
    {
        if (sharedConfigured || !SharedGameContent.IsLoaded) return;
        sharedDefinition = SharedGameContent.Power(this) ?? SharedGameContent.Boss(this);
        SharedGameContent.Configure(this);
        sharedConfigured = true;
    }
    public string Name { get { ConfigureSharedContent(); return (string)sharedDefinition?["name"] ?? nativeName; } protected set { nativeName=value; sharedConfigured=false; } }
    public virtual string PowerUpDescription { get { ConfigureSharedContent(); return UsesBlockReward ? SharedPowerRewards.Description(this, sharedDefinition) : sharedDefinition != null ? SharedGameContent.RenderForGoal(sharedDefinition,RewardGoal) : nativeDescription; } protected set => nativeDescription=value; }
    public Sprite Icon { get; protected set; }
    public PowerTier Rarity {
        get {
            ConfigureSharedContent();
            string value=(string)sharedDefinition?["tier"];
            if(value==null && this is SharedRulePower rule)value=(string)SharedGameContent.Power(rule)?["tier"];
            return Enum.TryParse<PowerTier>(value,true,out var tier)?tier:PowerTier.Basic;
        }
    }

    // Positive scoring rewards may adapt to blocks; specialized mechanics retain their goal restriction.
    // Read-only HUD contributions. Never evaluate scoring hooks to inspect powers.
    // Passive effects still score normally, but are not match-trigger feedback.
    public virtual bool HasConditionalTrigger => true;
    public virtual PowerTriggerDisplay DisplayWhenTriggered
    {
        get
        {
            ConfigureSharedContent();
            return PowerTriggerFeedback.Parse((string)sharedDefinition?["displayWhenTriggered"]);
        }
    }
    public virtual float FlatBasePerTile => 0;
    public virtual bool OpeningOnly => false;
    public virtual float FlatMultiplierBonus => 0;
    public virtual float FlatMultiplierFactor => 1;
    public virtual string ConditionalStatDescription => GetCurrentPowerDescription();
    public virtual PowerStatEffects StatEffects => PowerStatEffects.None;
    public bool AffectsStat(bool multiplier) => (StatEffects & (multiplier ? PowerStatEffects.Multiplier : PowerStatEffects.Base)) != 0;
    public virtual string BaseCondition => "";
    public virtual string MultiplierCondition => "";
    public string ConditionForStat(bool multiplier) => AffectsStat(multiplier) ? (multiplier ? MultiplierCondition : BaseCondition) : "";
    public virtual PowerTags Tags => PowerTags.Scoring;
    // Contextual effects are queried without consulting global GameState.
    public virtual PowerTags OfferTags(FreePlayMode mode) => Tags |
        (UsesBlockReward ? (AffectsStat(false)?PowerTags.GoalCredit:PowerTags.None) |
            (AffectsStat(true)?PowerTags.Clearing:PowerTags.None) : PowerTags.None) |
        (AffectsStat(false) ? PowerTags.BasePoints : PowerTags.None) |
        (AffectsStat(true) ? PowerTags.Multiplier : PowerTags.None);
    public virtual bool CanOfferWith(IReadOnlyList<PowerUp> active) => true;
    public virtual bool SupportsItems(IList<ItemType> items) => true;
    public virtual int AdjustFinalScore(List<Tile> tiles, int score) => score;
    public GoalType RewardGoal { get; private set; } = GoalType.Score;
    internal double BlockClearRemainder;
    internal int QueuedBlockClears;
    public PowerUp ForGoal(GoalType goal) { RewardGoal = goal; return this; }
    public bool UsesBlockReward => RewardGoal == GoalType.BlocksCleared && SharedPowerRewards.CanAdapt(this);
    public virtual PowerGoals SupportedGoals => SharedPowerRewards.CanAdapt(this) ? PowerGoals.Both : PowerGoals.Points;
    public virtual PowerModes SupportedModes => PowerModes.Both;
    public bool Supports(GoalType goal, FreePlayMode mode) =>
        (SupportedGoals & (goal == GoalType.Score ? PowerGoals.Points : PowerGoals.Blocks)) != 0 &&
        (SupportedModes & (mode == FreePlayMode.Timed ? PowerModes.Timed : PowerModes.LimitedMoves)) != 0;
    public abstract string GetCurrentPowerDescription();

    public abstract void Apply(GameState state);
    public virtual int AdditiveBaseMod(List<Tile> tiles) => 0;
    public virtual int CurrentMatchBaseMultiplier(List<Tile> tiles) => 1;
    public virtual void OnRoundStarted() { }
    // Called for resolved groups independently of score and boss penalties.
    public virtual int EarnBudgetRewards(List<Tile> tiles) => 0;
    public virtual float FractionalMultiplierMod(List<Tile> tiles) => 0f;
    public virtual int AdditiveMultiplierMod(List<Tile> tiles) => 0;
    public virtual int NextMatchMulti(List<Tile> tiles) => 1;
    public virtual int CurrentMatchMultiplier(List<Tile> tiles) => 1;
    public virtual bool IsScoreValid(List<Tile> tiles) => true;
    public virtual void ModifyMatchRules(List<MatchRule> rules) { }
    public virtual bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) => true;

    protected static bool CanMatchExactly(IReadOnlyList<MatchRule> rules, int count)
    {
        if (rules == null) return false;
        foreach (var rule in rules)
            if (rule != null && rule.minimumLength <= count) return true;
        return false;
    }

    public abstract PowerUp Clone();
}

public abstract class IconBonusPower : PowerUp
{
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Animal;
    public ItemType ItemType;
}

/// <summary>Goal variants reuse native conditions once per resolved group. Points hooks are unchanged.</summary>
public static class SharedPowerRewards
{
    public static bool CanAdapt(PowerUp power) => power is IconTypeGivesBase or IconTypeGivesMulti or
        IconTypeStreakGivesBasePointsOrMulti or HabitatPower or EdgeBalancePower or FourExactlyPower or
        HighStakesPower or StackedOddsPower or UnderdogPower or ConsistencyIsKey or
        TwinShotPower or ColorFocusPower or JackpotFivePower or FourFuryPower or PrecisionStrikePower or
        AnimalDoublePower or DirectionalMatchPower or TacticalStarter or FlexibleMatchPower or BasePowerUp or
        BiomeOpeningPower or SpotlightOpeningPower || power is PatternAnimalPower pattern && !pattern.HasCurse;

    public static bool BaseWasPerTile(PowerUp power) => power is IconTypeGivesBase or
        IconTypeStreakGivesBasePointsOrMulti or EdgeBalancePower or
        TacticalStarter or FlexibleMatchPower or BasePowerUp or PatternAnimalPower ||
        power is HabitatPower habitat && !(habitat.Style == HabitatStyle.Shore && habitat.Tier == 0);

    public static string Description(PowerUp power, Newtonsoft.Json.Linq.JObject definition)
    {
        if(definition?["blockDescriptionTemplate"] != null)
        {
            string text=(string)definition["blockDescriptionTemplate"];
            if(definition["parameters"] is Newtonsoft.Json.Linq.JObject values)
                foreach(var pair in values)text=text.Replace("{"+pair.Key+"}",pair.Value.ToString());
            return text;
        }
        return "Use this power's matching condition to earn extra block credit or clear extra tiles.";
    }

    public static void Resolve(PowerUp power,List<Tile> tiles,int basePoints,float multi,int factor,int queued,ScoreResult result)
        => Resolve(power,tiles.Count,power is not IconTypeStreakGivesBasePointsOrMulti animal ||
            tiles.Any(tile=>tile.item.ItemType==animal.ItemType),basePoints,multi,factor,queued,result);

    public static void Resolve(PowerUp power,int tileCount,bool rewardAnimal,int basePoints,float multi,int factor,int queued,ScoreResult result)
    {
        int credit=BaseWasPerTile(power)?basePoints/Math.Max(1,tileCount):basePoints;
        if(power is BiomeOpeningPower)credit=basePoints/6;
        double clears=Math.Max(0,multi)+Math.Max(0,factor-1)+power.QueuedBlockClears;
        power.QueuedBlockClears=Math.Max(0,queued-1);
        // Animal growth keeps its original earning condition; its block reward belongs to that animal.
        if(!rewardAnimal)
        { credit=0;clears=0; }
        if(credit>0)result.BlockCredits[power]=credit;
        double available=power.BlockClearRemainder+clears;
        int whole=(int)Math.Min(int.MaxValue,Math.Floor(available));
        power.BlockClearRemainder=available-Math.Floor(available);
        if(whole>0)result.ExtraClears[power]=whole;
    }

    // Select each physical extra tile once. Extra clears do not invoke match-triggered powers.
    public static int ExpandClears(BoardModel board,IEnumerable<BoardModel.Cell> group,List<BoardModel.Cell> cleared,int amount)
    {
        var seen=new HashSet<BoardModel.Cell>(cleared);
        int before=cleared.Count;
        foreach(var cell in group)
        foreach(var neighbor in new[]{new BoardModel.Cell(cell.X-1,cell.Y),new BoardModel.Cell(cell.X+1,cell.Y),
            new BoardModel.Cell(cell.X,cell.Y-1),new BoardModel.Cell(cell.X,cell.Y+1)})
        {
            if(cleared.Count-before>=amount)return cleared.Count-before;
            if(neighbor.X<0||neighbor.Y<0||neighbor.X>=board.Width||neighbor.Y>=board.Height||
                !board[neighbor.X,neighbor.Y].HasValue||board.Strategy?.IsProtected(neighbor)==true||!seen.Add(neighbor))continue;
            cleared.Add(neighbor);
        }
        return cleared.Count-before;
    }
}

public static class PowerUpLibrary
{
    public static List<PowerUp> BlockStarters() => new()
    {
        new BlockClearPower(BlockClearStyle.Neighbor),
        new BlockClearPower(BlockClearStyle.LargeMatch),
        new BlockClearPower(BlockClearStyle.Cascade)
    };
    public static List<PowerUp> BlockPowers()
    {
        var powers = new List<PowerUp>();
        powers.AddRange(GetAll().Where(SharedPowerRewards.CanAdapt));
        powers.AddRange(BudgetPowers());
        powers.AddRange(ShufflePowers());
        powers.AddRange(AnimalSpawnPowers());
        powers.Add(new BlockClearPower(BlockClearStyle.Edge));
        powers.Add(new BlockClearPower(BlockClearStyle.Square));
        powers.Add(new BlockClearPower(BlockClearStyle.LongLine));
        foreach (ItemType animal in Enum.GetValues(typeof(ItemType)))
            powers.Add(new BlockClearPower(BlockClearStyle.Animal, animal));
        return powers;
    }
    public static List<PowerUp> Starters() => new()
    {
        new TacticalStarter(TacticalStyle.Square), new TacticalStarter(TacticalStyle.Plus),
        new TacticalStarter(TacticalStyle.Large), new TacticalStarter(TacticalStyle.FourStreak),
        new TacticalStarter(TacticalStyle.Opening), new TacticalStarter(TacticalStyle.DoubleSwap),
        new TacticalStarter(TacticalStyle.Alternating), new TacticalStarter(TacticalStyle.Edge)
    };
    public static List<PowerUp> GetAll() => AddRosterPowers(new List<PowerUp>()
    {
        new AnimalPairPower(ItemType.Duck, ItemType.Walrus),
        new AnimalPairPower(ItemType.Bear, ItemType.Sloth),
        new AnimalPairPower(ItemType.Chicken, ItemType.Giraffe),
        new PatternAnimalPower(true),
        new PatternAnimalPower(false),
        new FourExactlyPower(),
        new HighStakesPower(),
        new UnderdogPower(),
        new StackedOddsPower(),
        new ConsistencyIsKey(),
        new TwinShotPower(),
        new ColorFocusPower(),
        new JackpotFivePower(),
        new FourFuryPower(),
        new PrecisionStrikePower(),
        new NoVerticalDoublePower(),
        new VerticalMatchPower(),
        new IconTypeGivesMulti(ItemType.Sloth, 5),
        new IconTypeGivesMulti(ItemType.Giraffe, 5),
        new IconTypeGivesMulti(ItemType.Duck, 5),
        new IconTypeGivesMulti(ItemType.Chicken, 5),
        new IconTypeGivesMulti(ItemType.Hippo, 5),
        new IconTypeGivesMulti(ItemType.Walrus, 5),
        new IconTypeGivesMulti(ItemType.Bear, 5),
        new IconTypeGivesBase(ItemType.Sloth, 5),
        new IconTypeGivesBase(ItemType.Giraffe, 5),
        new IconTypeGivesBase(ItemType.Duck, 5),
        new IconTypeGivesBase(ItemType.Chicken, 5),
        new IconTypeGivesBase(ItemType.Hippo, 5),
        new IconTypeGivesBase(ItemType.Walrus, 5),
        new IconTypeGivesBase(ItemType.Bear, 5),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Walrus, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Sloth, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Giraffe, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Bear, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Walrus, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Sloth, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Giraffe, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Bear, 0, 10, 1),
    });

    private static List<PowerUp> AddRosterPowers(List<PowerUp> powers)
    {
        powers.Add(new EdgeBalancePower());
        foreach (HabitatStyle style in Enum.GetValues(typeof(HabitatStyle)))
            for (int tier = 0; tier < 3; tier++) powers.Add(new HabitatPower(style, tier));
        powers.AddRange(BudgetPowers());
        powers.AddRange(ShufflePowers());
        powers.AddRange(AnimalSpawnPowers());
        foreach (ItemType animal in Enum.GetValues(typeof(ItemType)))
        {
            var animals = (ItemType[])Enum.GetValues(typeof(ItemType));
            var rival = animals[(Array.IndexOf(animals, animal) + 1) % animals.Length];
            powers.Add(new AnimalMasteryPower(animal, true));
            powers.Add(new AnimalMasteryPower(animal, false));
            powers.Add(new AnimalMasteryPower(animal, true, rival));
            powers.Add(new AnimalMasteryPower(animal, false, rival));
            powers.Add(new AnimalDoublePower(animal));
            powers.Add(new RosterTradeoffPower(animal, true));
            powers.Add(new RosterTradeoffPower(animal, false));
        }
        powers.AddRange(SharedGameContent.CustomPowers());
        return powers;
    }
    public static List<PowerUp> All = GetAll();
    public static IEnumerable<PowerUp> AnimalSpawnPowers() => Enum.GetValues(typeof(ItemType)).Cast<ItemType>().Select(animal => (PowerUp)new AnimalSpawnPower(animal));
    public static List<PowerUp> ShufflePowers() => new()
    {
        new ShufflePower(ShuffleStyle.Earned), new ShufflePower(ShuffleStyle.Discount),
        new ShufflePower(ShuffleStyle.Opening), new ShufflePower(ShuffleStyle.SquareTicket),
        new ShufflePower(ShuffleStyle.LargeTicket), new ShufflePower(ShuffleStyle.Afterglow),
        new ShufflePower(ShuffleStyle.Salvage)
    };
    public static List<PowerUp> BudgetPowers() => new()
    {
        new BudgetPower(BudgetStyle.Edge), new BudgetPower(BudgetStyle.Square),
        new BudgetPower(BudgetStyle.Large)
    };
}

/// <summary>A nonnegative base bank; a group's net change benefits subsequent matches.</summary>
public sealed class EdgeBalancePower : PowerUp
{
    public int StoredBase { get; private set; }
    public override float FlatBasePerTile => StoredBase;
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.BasePoints | PowerTags.Growth | PowerTags.Edge | PowerTags.Tradeoff;
    public override string BaseCondition => GetCurrentPowerDescription();
    public EdgeBalancePower()
    {
        Name="Edge Balance";
        PowerUpDescription="Starts at 0 bonus base per tile. Each matched edge tile adds 1; each matched interior tile removes 1. Apply each match's net change for later matches, never below 0. Corners count once. Includes cascades; the bonus carries across rounds.";
    }
    public int ResolveMatch(IEnumerable<BoardModel.Cell> cells,int width,int height)
    {
        int before=StoredBase;
        if(cells==null||width<1||height<1)return before;
        long change=0;
        foreach(var cell in new HashSet<BoardModel.Cell>(cells)) {
            if(cell.X<0||cell.Y<0||cell.X>=width||cell.Y>=height)continue;
            change+=cell.X==0||cell.Y==0||cell.X==width-1||cell.Y==height-1?1:-1;
        }
        StoredBase=(int)Math.Max(0,Math.Min(int.MaxValue,(long)StoredBase+change));
        return before;
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if(tiles==null||tiles.Count==0)return 0;
        if(MatchInfo is not PlayerMoveInfo board)return (int)Math.Min(int.MaxValue,(long)StoredBase*tiles.Count);
        int before=ResolveMatch(tiles.Where(tile=>tile!=null).Select(tile=>new BoardModel.Cell(tile.x,tile.y)),board.BoardWidth,board.BoardHeight);
        return (int)Math.Min(int.MaxValue,(long)before*tiles.Count);
    }
    public override string GetCurrentPowerDescription()=>PowerUpDescription+" Current bonus: +"+StoredBase+(UsesBlockReward?" block credits per match.":" base per tile.");
    public override void Apply(GameState state)=>StoredBase=0;
    public override PowerUp Clone()=>SharedClone(new EdgeBalancePower());
}

public enum CollectionStyle { AnimalCredit, Transform, Admission, ShapeCredit }
public enum BloomCondition { Any, Edge, Square, Large }

public sealed class CollectionPower : PowerUp
{
    public override bool HasConditionalTrigger => Style != CollectionStyle.ShapeCredit || Shape != BlockShape.Any;
    public CollectionStyle Style { get; }
    public ItemType Animal { get; }
    public BlockShape Shape { get; }
    public BloomCondition Bloom { get; }
    public int TransformCount => Bloom == BloomCondition.Any ? 2 : 4;
    public override PowerGoals SupportedGoals => Style==CollectionStyle.Admission?PowerGoals.Blocks:PowerGoals.Both;
    public override PowerStatEffects StatEffects => Style is CollectionStyle.AnimalCredit or CollectionStyle.ShapeCredit ? PowerStatEffects.Base : PowerStatEffects.None;
    public override PowerTags Tags => PowerTags.Collection |
        (Style == CollectionStyle.Transform ? PowerTags.Transform | PowerTags.Animal :
        Style == CollectionStyle.Admission ? PowerTags.Admission | PowerTags.Animal | PowerTags.Roster | PowerTags.AddAnimal :
        Style == CollectionStyle.ShapeCredit ? PowerTags.GoalCredit | PowerTags.Pattern : PowerTags.GoalCredit | PowerTags.Animal) |
        (Bloom == BloomCondition.Any ? PowerTags.None : PowerTags.Pattern | (Bloom switch {
            BloomCondition.Edge => PowerTags.Edge, BloomCondition.Square => PowerTags.Square, _ => PowerTags.LargeMatch }));
    public CollectionPower(CollectionStyle style, ItemType animal = ItemType.Duck, BlockShape shape = BlockShape.Any,
        BloomCondition bloom = BloomCondition.Any)
    {
        if (!Enum.IsDefined(typeof(CollectionStyle), style) || !Enum.IsDefined(typeof(ItemType), animal) ||
            !Enum.IsDefined(typeof(BlockShape), shape) || style == CollectionStyle.ShapeCredit && shape == BlockShape.Any ||
            !Enum.IsDefined(typeof(BloomCondition), bloom) || bloom != BloomCondition.Any && style != CollectionStyle.Transform)
            throw new ArgumentException("Invalid collection power.");
        Style = style; Animal = animal; Shape = shape; Bloom = bloom;
        string bloomName = bloom switch { BloomCondition.Edge => " Edge", BloomCondition.Square => " Square", BloomCondition.Large => " Giant", _ => "" };
        string bloomTrigger = bloom switch { BloomCondition.Edge => "Edge matches", BloomCondition.Square => "Matches with a 2x2 square", BloomCondition.Large => "Matches of 6+ tiles", _ => "Matches" };
        Name = style switch { CollectionStyle.AnimalCredit => animal + " Collector", CollectionStyle.Transform => animal + bloomName + " Bloom",
            CollectionStyle.Admission => "Also Count " + animal, _ => shape + " Dividend" };
        PowerUpDescription = style switch {
            CollectionStyle.AnimalCredit => $"Each matched {animal} counts as 1 extra block, if that animal counts toward the goal.",
            CollectionStyle.Transform => bloom == BloomCondition.Any
                ? $"Each match you make turns up to 2 tiles next to it into {animal}. Match them later to score."
                : $"{bloomTrigger} turn up to {TransformCount} neighboring tiles into {animal}.",
            CollectionStyle.Admission => $"{animal} also counts toward animal goals and can appear on the board. You still need the required shape.",
            _ => "Matches with " + BlockObjective.ShapeLabel(shape) + " count as 1 extra block per tile that meets the goal. Extra clears get no bonus." };
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles==null?0:PointBaseReward(BlockObjective.Snapshot(tiles));
    }
    public int PointBaseReward(IReadOnlyList<BlockMatchCell> cells)
    {
        if(RewardGoal!=GoalType.Score)return 0;
        if(Style==CollectionStyle.AnimalCredit)return ScoreManager.Multiply(4,cells.Count(t=>t.Animal==Animal));
        if(Style==CollectionStyle.ShapeCredit&&BlockObjective.HasShape(cells.Select(t=>t.Cell),Shape))return ScoreManager.Multiply(4,cells.Count);
        return 0;
    }
    public override bool SupportsItems(IList<ItemType> items) => Style == CollectionStyle.ShapeCredit || (items != null && items.Contains(Animal));
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active) =>
        (Style == CollectionStyle.AnimalCredit || Style == CollectionStyle.ShapeCredit || !active.OfType<CollectionPower>().Any(p => p.Name == Name)) &&
        !active.OfType<RosterTradeoffPower>().Any(p => !p.AddsAnimal && p.Animal == Animal && Style != CollectionStyle.ShapeCredit);
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        (Style != CollectionStyle.ShapeCredit || new BlockObjective(shape: Shape).Supports(rules)) &&
        (Bloom != BloomCondition.Square || new BlockObjective(shape: BlockShape.Square).Supports(rules));
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new CollectionPower(Style, Animal, Shape, Bloom));
    public static IEnumerable<PowerUp> Catalog()
    {
        foreach (ItemType animal in Enum.GetValues(typeof(ItemType)))
        {
            yield return new CollectionPower(CollectionStyle.AnimalCredit, animal);
            yield return new CollectionPower(CollectionStyle.Transform, animal);
            foreach (var condition in new[] { BloomCondition.Edge, BloomCondition.Square, BloomCondition.Large })
                yield return new CollectionPower(CollectionStyle.Transform, animal, bloom: condition);
            yield return new CollectionPower(CollectionStyle.Admission, animal);
        }
        foreach (BlockShape shape in Enum.GetValues(typeof(BlockShape)))
            if (shape != BlockShape.Any) yield return new CollectionPower(CollectionStyle.ShapeCredit, shape: shape);
    }
    // Read the original wave, skip all cells being cleared, and never chain transformations.
    private bool CanTransform(BoardModel board, List<BoardModel.Cell> group) => Bloom switch
    {
        BloomCondition.Edge => group.Any(cell => cell.X == 0 || cell.Y == 0 || cell.X == board.Width - 1 || cell.Y == board.Height - 1),
        BloomCondition.Square => BlockObjective.HasShape(group, BlockShape.Square),
        BloomCondition.Large => group.Count >= 6,
        _ => true
    };
    public static List<BoardModel.Cell> TransformNeighbors(BoardModel board, IEnumerable<List<BoardModel.Cell>> groups,
        IEnumerable<BoardModel.Cell> clearing, IEnumerable<PowerUp> active, bool playerWave, Action<PowerUp,int,List<BoardModel.Cell>> onActivated = null)
    {
        var changed = new List<BoardModel.Cell>();
        if (!playerWave) return changed;
        var excluded = new HashSet<BoardModel.Cell>(clearing);
        var edits = new Dictionary<BoardModel.Cell, ItemType>();
        var activations=new List<(PowerUp Power,int Amount,List<BoardModel.Cell> Group)>();
        foreach (var group in groups)
        foreach (var power in active.OfType<CollectionPower>().Where(p => p.Style == CollectionStyle.Transform))
        {
            if (!power.CanTransform(board, group)) continue;
            int remaining = power.TransformCount, before=edits.Count;
            foreach (var cell in group)
            {
                foreach (var neighbor in new[] { new BoardModel.Cell(cell.X - 1, cell.Y), new BoardModel.Cell(cell.X + 1, cell.Y),
                    new BoardModel.Cell(cell.X, cell.Y - 1), new BoardModel.Cell(cell.X, cell.Y + 1) })
                {
                    if (remaining == 0) break;
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= board.Width || neighbor.Y >= board.Height ||
                        excluded.Contains(neighbor) || edits.ContainsKey(neighbor) || !board[neighbor.X, neighbor.Y].HasValue || board[neighbor.X, neighbor.Y] == power.Animal) continue;
                    edits.Add(neighbor, power.Animal); remaining--;
                }
                if (remaining == 0) break;
            }
            if(edits.Count>before)activations.Add((power,edits.Count-before,group));
        }
        foreach (var edit in edits) { board[edit.Key.X, edit.Key.Y] = edit.Value; changed.Add(edit.Key); }
        foreach(var activation in activations)onActivated?.Invoke(activation.Power,activation.Amount,activation.Group);
        return changed;
    }
}

public enum BudgetStyle { Edge, Square, Large }

public enum ShuffleStyle { Earned, Discount, Opening, SquareTicket, LargeTicket, Afterglow, Salvage }

public sealed class ShufflePower : PowerUp
{
    public ShuffleStyle Style { get; }
    public int Progress { get; private set; }
    public int FreeUses { get; private set; }
    private int? lastMove;
    private int? rewardedMove;
    private bool blockRewardClaimed;
    public bool Armed { get; private set; }
    public const int MatchesRequired = 5;
    public int RequiredMatches => Style == ShuffleStyle.Earned ? MatchesRequired : 2;
    private bool EarnsTickets => Style == ShuffleStyle.Earned || Style == ShuffleStyle.SquareTicket || Style == ShuffleStyle.LargeTicket;
    private bool IsPayoff => Style == ShuffleStyle.Afterglow || Style == ShuffleStyle.Salvage;
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags Tags => PowerTags.Shuffle | (Style == ShuffleStyle.Afterglow ? PowerTags.Scoring :
        Style == ShuffleStyle.Salvage ? PowerTags.Clearing | PowerTags.GoalCredit : PowerTags.Utility) |
        (EarnsTickets ? PowerTags.Pattern : PowerTags.None) |
        (Style == ShuffleStyle.SquareTicket ? PowerTags.Square : Style == ShuffleStyle.LargeTicket ? PowerTags.LargeMatch : PowerTags.None);
    public override PowerStatEffects StatEffects => Style == ShuffleStyle.Afterglow ? PowerStatEffects.Multiplier : PowerStatEffects.None;
    public override string MultiplierCondition => Style == ShuffleStyle.Afterglow ? PowerUpDescription : "";
    public override string PowerUpDescription
    {
        get => SharedGameContent.Power(this) is { } shared ? SharedGameContent.RenderForGoal(shared,RewardGoal) : Style switch
        {
            ShuffleStyle.Earned => "Make 5 matching moves to earn a free shuffle. Free shuffles reset each round.",
            ShuffleStyle.Opening => "Your first 3 shuffles each round are free.",
            ShuffleStyle.SquareTicket => "Make 2 matching moves with a 2x2 square to earn a free shuffle. Free shuffles reset each round.",
            ShuffleStyle.LargeTicket => "Make 2 matching moves with 6+ tiles to earn a free shuffle. Free shuffles reset each round.",
            ShuffleStyle.Afterglow => "After you shuffle, your next successful play scores double points, including falling matches. Shuffling again does not stack this bonus.",
            ShuffleStyle.Salvage => "After you shuffle, your next successful matching move earns 8 extra block credits. Shuffling again does not stack this bonus.",
            _ => (GameState.Instance?.CurrentLevel as ILevelSettings)?.Mode == FreePlayMode.LimitedMoves
                ? "Shuffling costs 1 move instead of 2 (50% less)."
                : "Shuffling costs 2.5 seconds instead of 5 (50% less)."
        };
        protected set => base.PowerUpDescription = value;
    }
    public ShufflePower(ShuffleStyle style)
    {
        Style = style;
        Name = style switch {
            ShuffleStyle.Earned => "Shuffle Streak", ShuffleStyle.Opening => "Fresh Start",
            ShuffleStyle.SquareTicket => "Square Ticket", ShuffleStyle.LargeTicket => "Big Ticket",
            ShuffleStyle.Afterglow => "Shuffle Spark", ShuffleStyle.Salvage => "Clean Slate",
            _ => "Half-Price Shuffle" };
        OnRoundStarted();
    }
    public bool RecordMatch(MatchInfo context, IReadOnlyList<BoardModel.Cell> cells = null)
    {
        if (!EarnsTickets || context?.wasUserInitiatedMatch() != true ||
            context is not PlayerMoveInfo move || lastMove == move.MoveId) return false;
        if (Style == ShuffleStyle.LargeTicket && (cells == null || cells.Distinct().Count() < 6)) return false;
        if (Style == ShuffleStyle.SquareTicket && (cells == null || LevelBonuses.RewardPercent(LevelBonus.Square, cells, true) != 125)) return false;
        lastMove = move.MoveId;
        if (++Progress < RequiredMatches) return false;
        Progress = 0;
        FreeUses = ScoreManager.SaturatingAdd(FreeUses, 1);
        return true;
    }
    public void NotifyShuffle() { if (IsPayoff) { Armed = true; rewardedMove = null; blockRewardClaimed = false; } }
    public void ObserveMatch(MatchInfo context) { if (IsPayoff) IsRewardedPlay(context); }
    private bool IsRewardedPlay(MatchInfo context)
    {
        if (context is not PlayerMoveInfo move) return false;
        if (Armed && context.wasUserInitiatedMatch()) { Armed = false; rewardedMove = move.MoveId; }
        return rewardedMove == move.MoveId;
    }
    public override int CurrentMatchMultiplier(List<Tile> tiles) => Style == ShuffleStyle.Afterglow && RewardGoal==GoalType.Score && IsRewardedPlay(MatchInfo) ? 2 : 1;
    public int ClaimBlockCredits(MatchInfo context)
    {
        if (!(Style==ShuffleStyle.Salvage||Style==ShuffleStyle.Afterglow&&RewardGoal==GoalType.BlocksCleared) || !IsRewardedPlay(context) || blockRewardClaimed) return 0;
        blockRewardClaimed = true; return 8;
    }
    public override int AdditiveBaseMod(List<Tile> tiles) => Style==ShuffleStyle.Salvage&&RewardGoal==GoalType.Score?ScoreManager.Multiply(4,ClaimBlockCredits(MatchInfo)):0;
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) => Style != ShuffleStyle.SquareTicket || rules.Any(rule => rule.type == MatchType.Snake);
    public override void OnRoundStarted() { Progress = 0; lastMove = rewardedMove = null; Armed = blockRewardClaimed = false; FreeUses = Style == ShuffleStyle.Opening ? 3 : 0; }
    public override void Apply(GameState state) => OnRoundStarted();
    public override PowerUp Clone() => SharedClone(new ShufflePower(Style));
    public override string GetCurrentPowerDescription() => PowerUpDescription + (IsPayoff ? (Armed ? "\nNext play bonus ready!" : "") : Style == ShuffleStyle.Discount ? "" :
        $"\nFree shuffles ready: {FreeUses}." + (EarnsTickets ? $" Progress: {Progress}/{RequiredMatches}." : ""));
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active)
    {
        foreach (var power in active) if (power is ShufflePower shuffle && shuffle.Style == Style) return false;
        return true;
    }
    public static float Cost(IReadOnlyList<PowerUp> powers, bool timed)
    {
        bool discount = false;
        foreach (var power in powers)
            if (power is ShufflePower shuffle)
            {
                if (shuffle.FreeUses > 0) return 0;
                if (shuffle.Style == ShuffleStyle.Discount) discount = true;
            }
        return (timed ? 5f : 2f) * (discount ? .5f : 1f);
    }
    public static bool SpendFreeUse(IReadOnlyList<PowerUp> powers)
    {
        // Spend starting uses before earned uses, regardless of acquisition order.
        foreach (var style in new[] { ShuffleStyle.Opening, ShuffleStyle.Earned, ShuffleStyle.SquareTicket, ShuffleStyle.LargeTicket })
        foreach (var power in powers)
            if (power is ShufflePower shuffle && shuffle.Style == style && shuffle.FreeUses > 0)
            { shuffle.FreeUses--; return true; }
        return false;
    }
}

/// <summary>Deliberate patterns repeatedly replenish the round budget.</summary>
public sealed class BudgetPower : PowerUp
{
    public const int SecondsPerReward = 3;
    public BudgetStyle Style { get; }
    public int Progress { get; private set; }
    public int RewardsEarned { get; private set; }
    public int MatchesRequired => Style == BudgetStyle.Edge ? 3 : 2;
    private int? lastQualifyingMove;
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags Tags => PowerTags.Pattern | PowerTags.Budget |
        (Style == BudgetStyle.Edge ? PowerTags.Edge : Style == BudgetStyle.Square ? PowerTags.Square : PowerTags.LargeMatch);
    public override PowerTags OfferTags(FreePlayMode mode) => base.OfferTags(mode) |
        (mode == FreePlayMode.Timed ? PowerTags.TimeReward : PowerTags.MoveReward);
    public string IconLabel => Style == BudgetStyle.Edge ? "EDGE" : Style == BudgetStyle.Square ? "2x2" : "6+";
    public bool RewardsMoves => (GameState.Instance?.CurrentLevel as ILevelSettings)?.Mode == FreePlayMode.LimitedMoves;
    private string TriggerText => Style == BudgetStyle.Edge ? "3 matches touching any board edge"
        : Style == BudgetStyle.Square ? "2 matches containing a 2x2 square" : "2 matches of 6+ tiles";
    public override string PowerUpDescription
    {
        get
        {
            string reward = RewardsMoves ? "1 move" : $"{SecondsPerReward} seconds";
            return SharedGameContent.Power(this) is { } shared
                ? SharedGameContent.Render(shared).Replace($"{SecondsPerReward} seconds or 1 move", reward)
                : $"Make {TriggerText} to gain {reward}. Each move counts once.";
        }
        protected set => base.PowerUpDescription = value;
    }

    public BudgetPower(BudgetStyle style)
    {
        Style = style;
        Name = style == BudgetStyle.Edge ? "Edge Explorer" : style == BudgetStyle.Square ? "Square Deal" : "Big Breather";
    }

    public override int EarnBudgetRewards(List<Tile> tiles)
    {
        if (MatchInfo?.wasUserInitiatedMatch() != true ||
            MatchInfo is not PlayerMoveInfo move || lastQualifyingMove == move.MoveId || tiles == null) return 0;
        var cells = new HashSet<(int x, int y)>();
        foreach (var tile in tiles) if (tile != null) cells.Add((tile.x, tile.y));
        bool qualifies = Style == BudgetStyle.Large && cells.Count >= 6;
        foreach (var cell in cells)
        {
            if (Style == BudgetStyle.Edge && (cell.x == 0 || cell.y == 0 ||
                cell.x == move.BoardWidth - 1 || cell.y == move.BoardHeight - 1)) qualifies = true;
            if (Style == BudgetStyle.Square && cells.Contains((cell.x + 1, cell.y)) &&
                cells.Contains((cell.x, cell.y + 1)) && cells.Contains((cell.x + 1, cell.y + 1))) qualifies = true;
        }
        if (!qualifies) return 0;
        lastQualifyingMove = move.MoveId;
        if (++Progress < MatchesRequired) return 0;
        Progress = 0;
        RewardsEarned++;
        return 1;
    }

    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules)
    {
        foreach (var rule in rules)
            if (Style != BudgetStyle.Square || rule.type == MatchType.Snake) return true;
        return false;
    }
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active)
    {
        foreach (var power in active) if (power is BudgetPower budget && budget.Style == Style) return false;
        return true;
    }
    public override void OnRoundStarted() { Progress = RewardsEarned = 0; lastQualifyingMove = null; }
    public override void Apply(GameState state) => OnRoundStarted();
    public override PowerUp Clone() => SharedClone(new BudgetPower(Style));
    public override string GetCurrentPowerDescription() => PowerUpDescription +
        $"\nProgress: {Progress}/{MatchesRequired}. Rewards this round: {RewardsEarned}. Progress resets each round.";
}

public enum BlockClearStyle { Neighbor, LargeMatch, Cascade, Animal, Edge, Square, LongLine }

/// <summary>Extra clears are real board cells, not virtual goal credit.</summary>
public sealed class BlockClearPower : PowerUp
{
    public override bool HasConditionalTrigger => Style != BlockClearStyle.Neighbor;
    public override bool OpeningOnly => Style == BlockClearStyle.Neighbor || Style == BlockClearStyle.LargeMatch || Style == BlockClearStyle.Cascade;
    public BlockClearStyle Style { get; }
    public ItemType Animal { get; }
    public override PowerTags Tags => PowerTags.Clearing | (Style == BlockClearStyle.Animal ? PowerTags.Animal : PowerTags.Starter) |
        (Style == BlockClearStyle.Edge ? PowerTags.Edge : Style == BlockClearStyle.Square ? PowerTags.Square :
        Style == BlockClearStyle.Cascade ? PowerTags.Cascade : Style == BlockClearStyle.LongLine || Style == BlockClearStyle.LargeMatch ? PowerTags.LargeMatch : PowerTags.None);
    public override bool CanUseWithMatchRules(IReadOnlyList<MatchRule> rules) =>
        Style != BlockClearStyle.Square || (rules != null && System.Linq.Enumerable.Any(rules, r => r.type == MatchType.Snake));
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public BlockClearPower(BlockClearStyle style, ItemType animal = ItemType.Bear)
    {
        Style = style; Animal = animal;
        Name = style switch
        {
            BlockClearStyle.Neighbor => "Helping Hand",
            BlockClearStyle.LargeMatch => "Big Sweep",
            BlockClearStyle.Cascade => "Chain Reaction",
            BlockClearStyle.Edge => "Shore Sweep",
            BlockClearStyle.Square => "Garden Excavator",
            BlockClearStyle.LongLine => "Trail Blazer",
            _ => animal + " Sweep"
        };
        PowerUpDescription = style switch
        {
            BlockClearStyle.Neighbor => "All matches clear 1 extra tile next to them.",
            BlockClearStyle.LargeMatch => "Matches of 4 or more clear 2 extra tiles next to them.",
            BlockClearStyle.Cascade => "Matches made by falling tiles clear 2 extra tiles next to them.",
            BlockClearStyle.Edge => "Matches touching the edge clear 2 extra tiles next to them.",
            BlockClearStyle.Square => "Matches with a 2x2 square clear 2 extra tiles next to them.",
            BlockClearStyle.LongLine => "Matches of 6 or more clear 2 extra tiles next to them.",
            _ => "Matches of " + animal + " clear 2 extra tiles next to them."
        };
        PowerUpDescription += " Extra clears trigger other powers only when stated.";
    }
    public int BonusBaseForClears(int actualClears) => RewardGoal==GoalType.Score?ScoreManager.Multiply(4,Math.Max(0,actualClears)):0;
    public int ExtraClears(BoardModel board, List<BoardModel.Cell> group, bool cascade)
    {
        if (Style == BlockClearStyle.Neighbor) return 1;
        if (Style == BlockClearStyle.LargeMatch) return group.Count >= 4 ? 2 : 0;
        if (Style == BlockClearStyle.Cascade) return cascade ? 2 : 0;
        if (Style == BlockClearStyle.Edge) return group.Exists(c => c.X == 0 || c.Y == 0 || c.X == board.Width - 1 || c.Y == board.Height - 1) ? 2 : 0;
        if (Style == BlockClearStyle.LongLine) return group.Count >= 6 ? 2 : 0;
        if (Style == BlockClearStyle.Square)
        {
            var cells = new HashSet<BoardModel.Cell>(group);
            return group.Exists(c => cells.Contains(new BoardModel.Cell(c.X + 1, c.Y)) &&
                cells.Contains(new BoardModel.Cell(c.X, c.Y + 1)) && cells.Contains(new BoardModel.Cell(c.X + 1, c.Y + 1))) ? 2 : 0;
        }
        return group.Exists(cell => board[cell.X, cell.Y] == Animal) ? 2 : 0;
    }
    public override bool SupportsItems(IList<ItemType> items) => Style != BlockClearStyle.Animal || (items != null && items.Contains(Animal));
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new BlockClearPower(Style, Animal));

    public static List<BoardModel.Cell> ExpandClears(BoardModel board, List<List<BoardModel.Cell>> groups,
        IEnumerable<PowerUp> powers, bool cascade, Action<PowerUp,int,List<BoardModel.Cell>> onActivated = null)
    {
        var cleared = new HashSet<BoardModel.Cell>();
        var result = new List<BoardModel.Cell>();
        foreach (var group in groups)
            foreach (var cell in group) if (cleared.Add(cell)) result.Add(cell);
        foreach (var group in groups)
        foreach (var power in powers)
        {
            if (power is not BlockClearPower block) continue;
            int remaining = block.ExtraClears(board, group, cascade);
            int before=result.Count;
            foreach (var cell in group)
            {
                foreach (var neighbor in new[] { new BoardModel.Cell(cell.X - 1, cell.Y),
                    new BoardModel.Cell(cell.X + 1, cell.Y), new BoardModel.Cell(cell.X, cell.Y - 1),
                    new BoardModel.Cell(cell.X, cell.Y + 1) })
                {
                    if (remaining <= 0) break;
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= board.Width || neighbor.Y >= board.Height ||
                        !board[neighbor.X, neighbor.Y].HasValue || board.Strategy?.IsProtected(neighbor)==true || !cleared.Add(neighbor)) continue;
                    result.Add(neighbor); remaining--;
                }
                if (remaining <= 0) break;
            }
            if(result.Count>before)onActivated?.Invoke(block,result.Count-before,group);
        }
        return result;
    }
}

public sealed class AnimalPairPower : PowerUp
{
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public static int ApplyBlockPenalty(int credit,IEnumerable<ItemType> matchedAnimals,IEnumerable<PowerUp> active)
    {
        var animals=new HashSet<ItemType>(matchedAnimals);
        int result=Math.Max(0,credit);
        foreach(var pact in active.OfType<AnimalPairPower>().GroupBy(p=>(p.First,p.Second)).Select(g=>g.First()))
            if(animals.Any(pact.Contains))result/=2;
        return result;
    }
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => $"Groups containing {First} or {Second}: x0.5 points.";
    public ItemType First { get; }
    public ItemType Second { get; }
    public override PowerTags Tags => PowerTags.Animal | PowerTags.AnimalPair | PowerTags.Matching | PowerTags.Tradeoff | PowerTags.Scoring;
    public AnimalPairPower(ItemType first, ItemType second)
    {
        if (first == second) throw new ArgumentException("A pair needs distinct animals.");
        First = (ItemType)Math.Min((int)first, (int)second);
        Second = (ItemType)Math.Max((int)first, (int)second);
        Name = First + " + " + Second + " Pact";
        PowerUpDescription = $"{First} and {Second} can match together.\n" +
            $"Matches with either animal score half points.";
    }
    public bool Contains(ItemType animal) => animal == First || animal == Second;
    public override bool SupportsItems(IList<ItemType> items) => items.Contains(First) && items.Contains(Second);
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active)
    {
        foreach (var power in active)
            if (power is AnimalPairPower pair && (Contains(pair.First) || Contains(pair.Second))) return false;
        return true;
    }
    public override int AdjustFinalScore(List<Tile> tiles, int score) =>
        tiles.Exists(tile => tile != null && tile.item != null && Contains(tile.item.ItemType)) ? score / 2 : score;
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new AnimalPairPower(First, Second));
}

/// <summary>Relative spawn weights change future animals without changing the roster.</summary>
public sealed class AnimalSpawnPower : PowerUp
{
    public ItemType Animal { get; }
    public AnimalSpawnPower(ItemType animal)
    {
        Animal = animal; Name = animal + " Magnet";
        PowerUpDescription = $"New tiles favor {animal}: it gets 2 chances instead of 1. Includes refills and shuffles.";
    }
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags Tags => PowerTags.Utility | PowerTags.Animal | PowerTags.Roster;
    public override bool SupportsItems(IList<ItemType> items) => items != null && items.Contains(Animal);
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active) => !active.Any(p =>
        p is AnimalSpawnPower magnet && magnet.Animal == Animal ||
        p is RosterTradeoffPower roster && !roster.AddsAnimal && roster.Animal == Animal);
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new AnimalSpawnPower(Animal));
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public static int Weight(ItemType animal, IReadOnlyList<PowerUp> powers) => powers.Any(p => p is AnimalSpawnPower magnet && magnet.Animal == animal) ? 2 : 1;
    public static ItemType Select(IReadOnlyList<ItemType> types, double roll, Func<ItemType,int> weight = null)
    {
        if (types == null || types.Count == 0) throw new ArgumentException("Spawning needs animals.", nameof(types));
        int total = types.Sum(type => Math.Max(1,weight?.Invoke(type) ?? 1));
        double ticket = Math.Clamp(roll,0d,1d) * total;
        foreach (var type in types) { ticket -= Math.Max(1,weight?.Invoke(type) ?? 1); if (ticket < 0) return type; }
        return types[types.Count-1];
    }
}

public sealed class RosterTradeoffPower : PowerUp
{
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags OfferTags(FreePlayMode mode) => base.OfferTags(mode)|(RewardGoal==GoalType.BlocksCleared?PowerTags.GoalCredit:PowerTags.None);
    public static int ApplyBlockRewards(int credit,IEnumerable<PowerUp> active)
    {
        long original=Math.Max(0,credit),quarter=original/4;
        long net=active.OfType<RosterTradeoffPower>().Sum(p=>p.AddsAnimal?1L:-1L);
        return (int)Math.Min(int.MaxValue,Math.Max(0,original+quarter*net));
    }
    public override bool HasConditionalTrigger => false;
    public ItemType Animal { get; }
    public bool AddsAnimal { get; }
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override float FlatMultiplierFactor => RewardGoal==GoalType.BlocksCleared?1:AddsAnimal?1.25f:.75f;
    public override string ConditionalStatDescription => "";
    public override PowerTags Tags => PowerTags.Animal | PowerTags.Roster | PowerTags.Tradeoff | PowerTags.Scoring |
        (AddsAnimal ? PowerTags.AddAnimal : PowerTags.RemoveAnimal);
    public RosterTradeoffPower(ItemType animal, bool add)
    {
        Animal = animal; AddsAnimal = add;
        Name = (add ? "Welcome " : "Farewell ") + animal;
        PowerUpDescription = add
            ? $"Add {animal} to the board. All matches gain 25% extra points, rounded down after other powers."
            : $"Remove {animal} from the board. All matches lose 25% of their points, rounded down after other powers.";
    }
    public override bool SupportsItems(IList<ItemType> items) => AddsAnimal ? !items.Contains(Animal) : items.Contains(Animal) && items.Count > 3;
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active)
    {
        foreach (var power in active)
        {
            if (power is RosterTradeoffPower roster && roster.Animal == Animal) return false;
            if (!AddsAnimal && (power is IconBonusPower icon && icon.ItemType == Animal ||
                power is AnimalSpawnPower magnet && magnet.Animal == Animal ||
                power is CollectionPower collection && collection.Style != CollectionStyle.ShapeCredit && collection.Animal == Animal ||
                power is AnimalPairPower pair && pair.Contains(Animal) ||
                power is BlockClearPower block && block.Style == BlockClearStyle.Animal && block.Animal == Animal)) return false;
        }
        return true;
    }
    public override int AdjustFinalScore(List<Tile> tiles, int score) => RewardGoal==GoalType.BlocksCleared?score:AddsAnimal
        ? ScoreManager.SaturatingAdd(score,Math.Max(0,score)/4) : Math.Max(0,score)-Math.Max(0,score)/4;
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new RosterTradeoffPower(Animal, AddsAnimal));
}

/// <summary>Actual matched tiles this round; bonus adjacency clears are excluded.</summary>
public sealed class RoundAnimalHistory
{
    private readonly Dictionary<ItemType, int> counts = new();
    public int Count(ItemType type) => counts.TryGetValue(type, out var count) ? count : 0;
    public IReadOnlyList<ItemType> LeastMatched(IEnumerable<ItemType> pool)
    {
        var animals=pool.Distinct().OrderBy(animal=>(int)animal).ToArray();
        if(animals.Length==0)return System.Array.Empty<ItemType>();
        int minimum=animals.Min(Count);
        return animals.Where(animal=>Count(animal)==minimum).ToArray();
    }
    public ItemType? Leader(IEnumerable<ItemType> pool) => pool.Distinct().Where(animal => Count(animal) > 0)
        .OrderByDescending(Count).ThenBy(animal => (int)animal).Select(animal => (ItemType?)animal).FirstOrDefault();
    public void Reset() => counts.Clear();
    public void Record(List<Tile> tiles)
    {
        var seen = new HashSet<Tile>();
        RecordAnimals(tiles.Where(tile => tile != null && tile.item != null && seen.Add(tile)).Select(tile => tile.item.ItemType));
    }
    public void RecordAnimals(IEnumerable<ItemType> animals)
    {
        foreach (var animal in animals) counts[animal] = ScoreManager.SaturatingAdd(Count(animal), 1);
    }
    public int Bonus(ItemType type, IEnumerable<ItemType> pool, int leastBonus, int mostPenalty)
    {
        int min = int.MaxValue, max = -1;
        bool present = false;
        foreach (var animal in pool)
        {
            int count = Count(animal);
            min = Math.Min(min, count); max = Math.Max(max, count);
            if (animal == type) present = true;
        }
        if (!present) return 0;
        // All tied: reward exploration, never penalize an arbitrary animal.
        if (Count(type) == min) return leastBonus;
        return Count(type) == max ? -mostPenalty : 0;
    }
}

public sealed class PatternAnimalPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => GetCurrentPowerDescription();
    public bool HasCurse { get; }
    public override PowerTags Tags => PowerTags.Pattern | PowerTags.Animal | PowerTags.Scoring |
        (HasCurse ? PowerTags.Tradeoff : PowerTags.None);
    public PatternAnimalPower(bool hasCurse)
    {
        HasCurse = hasCurse;
        Name = hasCurse ? "Change of Taste" : "Hidden Favorites";
        PowerUpDescription = hasCurse
            ? "Animals you match least this round gain 10 base points per tile. Animals you match most lose 5 base points per tile."
            : "Animals you match least this round gain 5 base points per tile.";
        PowerUpDescription += " Tied animals share the bonus. If all are tied, none lose points.";
    }
    private List<ItemType> Pool()
    {
        var pool = new List<ItemType>();
        foreach (var item in ItemInfoProvider?.GetActiveItems ?? new List<Item>())
            if (item != null && !pool.Contains(item.ItemType)) pool.Add(item.ItemType);
        return pool;
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (AnimalHistory == null) return 0;
        var pool = Pool();
        long bonus = 0;
        foreach (var tile in tiles)
            if (tile != null && tile.item != null)
                bonus += AnimalHistory.Bonus(tile.item.ItemType, pool, HasCurse ? 10 : 5, HasCurse ? 5 : 0);
        return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, bonus));
    }
    public override string GetCurrentPowerDescription()
    {
        if (AnimalHistory == null) return PowerUpDescription;
        var pool = Pool();
        var boosted = new List<string>();
        var penalized = new List<string>();
        foreach (var animal in pool)
        {
            int bonus = AnimalHistory.Bonus(animal, pool, HasCurse ? 10 : 5, HasCurse ? 5 : 0);
            if (bonus > 0) boosted.Add(animal.ToString());
            if (bonus < 0) penalized.Add(animal.ToString());
        }
        return PowerUpDescription + "\n<b>Boosted:</b> " + (boosted.Count == 0 ? "None" : string.Join(", ", boosted)) +
            (HasCurse ? "\n<b>Penalized:</b> " + (penalized.Count == 0 ? "None" : string.Join(", ", penalized)) : "");
    }
    public override void Apply(GameState state) { }
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active)
    {
        foreach (var power in active) if (power is PatternAnimalPower pattern && pattern.HasCurse == HasCurse) return false;
        return true;
    }
    public override PowerUp Clone() => SharedClone(new PatternAnimalPower(HasCurse));
}

// Level-specific openings; ordinary reward pools never include these cards.
public sealed class BiomeOpeningPower : PowerUp
{
    public bool Square { get; }
    public override bool OpeningOnly => true;
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Starter | PowerTags.Pattern | PowerTags.BasePoints;
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => PowerUpDescription;
    public BiomeOpeningPower(bool square)
    {
        Square = square; Name = square ? "Square Roots" : "Garden Harvest";
        PowerUpDescription = square ? "Matches with a 2x2 square gain 6 base points per tile." : "Each matched tile on a green space gains 6 base points.";
    }
    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (tiles == null) return 0;
        if (!Square) return tiles.Count(tile => tile != null && tile.IsPlanted)*6;
        var cells = tiles.Where(tile => tile != null).Select(tile => new BoardModel.Cell(tile.x,tile.y)).ToList();
        return LevelBonuses.RewardPercent(LevelBonus.Square,cells,true) > 100 ? tiles.Count*6 : 0;
    }
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new BiomeOpeningPower(Square));
}
public sealed class SpotlightOpeningPower : PowerUp
{
    public override bool OpeningOnly => true;
    public override PowerTags Tags => PowerTags.Scoring | PowerTags.Starter | PowerTags.Animal | PowerTags.Multiplier;
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => PowerUpDescription;
    public SpotlightOpeningPower() { Name = "Both Sides"; PowerUpDescription = "Matches with Duck or Bear gain 1 multiplier."; }
    public override int AdditiveMultiplierMod(List<Tile> tiles) => tiles != null && tiles.Any(tile => tile != null && tile.item != null && (tile.item.ItemType == ItemType.Duck || tile.item.ItemType == ItemType.Bear)) ? 1 : 0;
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new SpotlightOpeningPower());
}
