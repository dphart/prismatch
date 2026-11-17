using System.Collections.Generic;
using System.Linq;

public enum LevelBonus { None, Cascade, LargeGroup, Square, ChainReaction, GardenPlots, AlternatingCurrent, LeastMatched, BorderPatrol, TreasureTrail, ChangingTides, MigrationSeason, CrackTheVault, SteppingStones, AnimalRescue, Crosswinds, GoldenRescue, WildRelease, Blackout, AnimalSpotlight, RollingDarkness, LightningRods }

public static class LevelBonuses
{
    public static string ShortDescription(LevelBonus bonus) => bonus switch
    {
        LevelBonus.LightningRods => "Charge each rod 3 times to charge all spaces for 2x points. Cascades count.",
        LevelBonus.AnimalSpotlight => "Every 5 matches, two different animals go dark.",
        LevelBonus.RollingDarkness => "Every 2 matched tiles darken a space. Match beside darkness to reveal it.",
        LevelBonus.Blackout => "Every 5 matches, the board goes dark. Reveal spaces to protect them.",
        LevelBonus.WildRelease => "Match beside the cage 10 times, then feed an animal to clear its type. Cascades count.",
        LevelBonus.GoldenRescue => "Every 2 matched tiles cage an animal. Free it for 3x block credit.",
        LevelBonus.MigrationSeason => "Highlighted animals give 2x points. The target changes each move.",
        LevelBonus.CrackTheVault => "Hit each vault side twice for double block credit on remaining tiles.",
        LevelBonus.SteppingStones => "Touch 2 glowing spaces in one match for 3x points. Matches leave glowing spaces.",
        LevelBonus.AnimalRescue => "Marked animals give double block credit. Rescue 3 types for +1 move.",
        LevelBonus.Crosswinds => "Every 3 matches, gravity turns and the cross clears. Its cascades score 5x.",
        LevelBonus.LeastMatched => "Your least matched animals give 2x points.",
        LevelBonus.BorderPatrol => "Light all 4 edges for 4x points on that match.",
        LevelBonus.TreasureTrail => "Every 3 treasures make your next play score 5x points.",
        LevelBonus.ChangingTides => "Every 4 moves turn gravity. Your next play scores 3x points.",
        LevelBonus.ChainReaction => "Player matches score 0. Cascades score 4x points.",
        LevelBonus.GardenPlots => "Match a 2x2 square to plant green spaces. Green spaces give 2x points.",
        LevelBonus.AlternatingCurrent => "Duck and Bear take turns giving 3x points after each move.",
        LevelBonus.Cascade => "Cascades give 25% extra goal reward.",
        LevelBonus.LargeGroup => "Matches of 6+ give 50% extra goal reward.",
        LevelBonus.Square => "Matches with a 2x2 square give 25% extra goal reward.",
        _ => "Match animals and use your powers to reach the goal."
    };
    public static string Description(LevelBonus bonus) => bonus switch {
        LevelBonus.LightningRods => "Three fixed lightning rods each need 3 charges. Each matched tile directly beside a rod adds 1 charge; touching three sides can fill it in one match. Cascades count. When all three are full, lightning chains through them after the play settles and charges every animal square for 2x points until it clears. The rods reset and can charge again.",
        LevelBonus.AnimalSpotlight => "Two animal types are dark; all others are visible. Every 5 player matches, the next two animal types become dark after the play finishes. Hidden animals still match. Cascades do not advance the counter.",
        LevelBonus.RollingDarkness => "Every 2 tiles in player-created matches darken 1 square after the play finishes. Odd tiles carry over. Darkness advances through alternating rows and columns, skipping already dark squares. Matches reveal replacement tiles and adjacent neighbors. Cascades reveal tiles without spreading darkness.",
        LevelBonus.Blackout => "Every 5 player matches, unprotected squares go black. Clearing darkness colors a square green and protects it for the rest of the round. Matches reveal replacement squares and adjacent neighbors; cascades reveal squares too. Boss: only replacement squares are revealed.",
        LevelBonus.WildRelease => "Make 10 matches directly beside the cage to open it. Feed an animal to score every animal of its type and remove that type from the refill pool for the rest of the round. The cage moves and resets. It rests when only 3 animal types remain. Captured types return next round or on retry. Cascades charge the cage. Feeding costs one move.",
        LevelBonus.GoldenRescue => "Every 2 tiles in a match cage one animal. Match directly beside cages to free them. Freed tiles have a gold border and give 3x block credit until matched; the bonus moves with the tile. Boss: every tile matched cages one animal.",
        LevelBonus.MigrationSeason => "Highlighted animals score 2x. The highlight changes after every successful move.",
        LevelBonus.CrackTheVault => "Hit each side of the vault twice. Opening it doubles the block credit of all remaining tiles. New tiles are unboosted.",
        LevelBonus.SteppingStones => "Matches leave glowing spaces. Touch at least two glowing spaces with your next match for 3x points.",
        LevelBonus.AnimalRescue => "Marked animals give double block credit. Rescue three different animals for an extra move. Match beside cages to open them.",
        LevelBonus.Crosswinds => "Every 3 player matches, gravity turns and the middle cross clears for no points. Its cascades score 5x. The cross glows one match before clearing.",
        LevelBonus.LeastMatched => "Your least matched animal gives 2x points. Tied animals share the bonus.",
        LevelBonus.BorderPatrol => "Touch each outside edge with a match to light it. Light all four for 4x points on that match, then start again.",
        LevelBonus.TreasureTrail => "Match the golden tile to collect treasure. Every third treasure makes your next successful play worth 5x points, including falling matches.",
        LevelBonus.ChangingTides => "After four successful moves, gravity turns: down, left, up, right. Your first play after each turn scores 3x points, including falling matches.",
        LevelBonus.ChainReaction => "Matches you make score 0. Matches made by falling animals score 4x.",
        LevelBonus.GardenPlots => "Match a 2x2 square to turn its spaces green. Green spaces give double points for the rest of the round.",
        LevelBonus.AlternatingCurrent => "Duck and Bear take turns giving 3x points. Check the animal below the grid. They switch after your matches finish.",
        LevelBonus.Cascade => "Cascades earn 25% extra goal reward",
        LevelBonus.LargeGroup => "Groups of 6+ earn 50% extra goal reward",
        LevelBonus.Square => "Groups containing a 2x2 square earn 25% extra goal reward",
        _ => "" };
    public static int AnimalMultiplier(LevelBonus bonus,RoundAnimalHistory history,IEnumerable<ItemType> pool,IEnumerable<ItemType> matched)
    {
        if(bonus!=LevelBonus.LeastMatched||history==null||pool==null||matched==null)return 1;
        var least=new HashSet<ItemType>(history.LeastMatched(pool));
        return matched.Any(least.Contains)?2:1;
    }
    public static int RewardPercent(LevelBonus bonus, IReadOnlyList<BoardModel.Cell> cells, bool playerMatch)
    {
        if (cells == null || cells.Count == 0) return 100;
        if (bonus == LevelBonus.ChainReaction) return playerMatch ? 0 : 400;
        if (bonus == LevelBonus.Cascade && !playerMatch) return 125;
        if (bonus == LevelBonus.LargeGroup && cells.Count >= 6) return 150;
        if (bonus == LevelBonus.Square)
        {
            var set = new HashSet<BoardModel.Cell>(cells);
            foreach (var cell in cells)
                if (set.Contains(new BoardModel.Cell(cell.X + 1, cell.Y)) &&
                    set.Contains(new BoardModel.Cell(cell.X, cell.Y + 1)) &&
                    set.Contains(new BoardModel.Cell(cell.X + 1, cell.Y + 1))) return 125;
        }
        return 100;
    }
}

/// <summary>Built-in level powers share the activation contract with acquired powers.</summary>
public sealed class LevelCharacteristicPower : PowerUp
{
    public LevelBonus Bonus { get; }
    public StrategyLevelState Strategy { get; }
    public bool Lightning { get; }
    public override PowerGoals SupportedGoals => PowerGoals.Both;
    public override PowerTags Tags => PowerTags.Utility;
    public LevelCharacteristicPower(LevelBonus bonus, bool lightning = false)
    {
        Bonus = bonus; Lightning = lightning;
        if(StrategyLevelState.Supports(bonus))Strategy=new StrategyLevelState(bonus);
        Name = lightning ? "Lightning" : bonus switch {
            LevelBonus.LightningRods => "Lightning Rods",
            LevelBonus.GoldenRescue => "Golden Rescue",
            LevelBonus.MigrationSeason => "Migration Season", LevelBonus.CrackTheVault => "Crack the Vault",
            LevelBonus.SteppingStones => "Stepping Stones", LevelBonus.AnimalRescue => "Animal Rescue", LevelBonus.Crosswinds => "Crosswinds",
            LevelBonus.GardenPlots => "Garden", LevelBonus.AlternatingCurrent => "Alternating Current",
            LevelBonus.LeastMatched => "Least Matched", LevelBonus.BorderPatrol => "Border Patrol",
            LevelBonus.TreasureTrail => "Treasure Trail", LevelBonus.ChangingTides => "Changing Tides",
            LevelBonus.ChainReaction => "Chain Reaction", LevelBonus.LargeGroup => "Large Groups",
            _ => bonus.ToString() };
        PowerUpDescription = lightning ? "Match five to strike a line. Charged tiles score double." : LevelBonuses.Description(bonus);
    }
    public bool CollectTreasure(BoardModel board,IEnumerable<BoardModel.Cell> cells)
        => Bonus==LevelBonus.TreasureTrail && board.CollectTreasure(cells);
    public int Plant(BoardModel board,IEnumerable<List<BoardModel.Cell>> groups)
        => Bonus==LevelBonus.GardenPlots ? board.PlantSquares(groups) : 0;
    public int BorderReward(BoardModel board,List<BoardModel.Cell> group)
        => Bonus==LevelBonus.BorderPatrol ? board.Campaign.BorderPercent(group,board.Width,board.Height) : board.Campaign.PlayPercent(Bonus);
    public int AnimalMultiplier(RoundAnimalHistory history,IEnumerable<ItemType> pool,IEnumerable<ItemType> matched) =>
        LevelBonuses.AnimalMultiplier(Bonus,history,pool,matched);
    public int RewardPercent(IReadOnlyList<BoardModel.Cell> cells,bool playerMatch) => LevelBonuses.RewardPercent(Bonus,cells,playerMatch);
    public int ApplyReward(int reward,IReadOnlyList<BoardModel.Cell> cells,bool playerMatch,ScoreResult result)
    {
        int percent=RewardPercent(cells,playerMatch);
        int adjusted=ChallengeBoss.ScaleReward(reward,percent);
        if(adjusted!=reward)result?.Activate(this,percent+"%");
        return adjusted;
    }
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new LevelCharacteristicPower(Bonus,Lightning);
}

/// <summary>A named level containing rounds; shares the same knobs as Free Play.</summary>
public sealed class CampaignLevel : LevelConfig, ILevelSettings, IRoundPowerOffers, IConfiguredPowerOffers
{
    public bool IsTutorial => number == 1 && Difficulty == 1;
    public int Difficulty { get; private set; } = 1;
    public CampaignLevel WithDifficulty(int difficulty)
    {
        var copy = (CampaignLevel)CloneNew();
        copy.Difficulty = System.Math.Max(1, difficulty);
        return copy;
    }
    public static int DifficultyTarget(int target, int difficulty)
    {
        decimal scaled = System.Math.Max(1, target);
        for (int step = 1; step < System.Math.Max(1, difficulty); step++)
        {
            scaled *= 1.1m;
            if (scaled >= int.MaxValue) return int.MaxValue;
        }
        return (int)System.Math.Ceiling(scaled);
    }
    public string Name { get; }
    public string Description { get; }
    public FreePlayConfig Settings { get; }
    public LevelPowerOffers PowerOffers { get; }
    public CampaignOfferTheme OfferTheme { get; }
    public CampaignOfferTheme OfferThemeForRound(int round) => round == 2 || round == 4 ? OfferTheme : CampaignOfferTheme.None;
    private readonly int number;
    private readonly bool bossFinal;
    public BossStyle? FinalBossStyle { get; }
    public BossStyle? RoundRule { get; }
    public BlockObjectives BlockObjectives { get; }
    public HabitatStyle? Habitat { get; }
    public LevelBonus Bonus { get; }
    public int BasePointsPerTile { get; }
    public int BossTargetMultiplier { get; }
    public bool LightningLines { get; }
    public bool AllowStarterRewards { get; }
    private readonly Dictionary<int, PowerTags> roundOffers;
    public PowerTags RequiredPowerTags(int round) => roundOffers.TryGetValue(round, out var tags) ? tags : PowerTags.None;
    public CampaignLevel(int number, string name, string description, FreePlayConfig settings, bool bossFinal = true, IReadOnlyDictionary<int, PowerTags> roundOffers = null, CampaignOfferTheme offerTheme = CampaignOfferTheme.None, LevelPowerOffers powerOffers = null, BossStyle? finalBossStyle = null, BossStyle? roundRule = null, BlockObjectives blockObjectives = null, HabitatStyle? habitat = null, LevelBonus bonus = LevelBonus.None, int basePointsPerTile = 0, bool lightningLines = false, bool allowStarterRewards = false, int bossTargetMultiplier = 1)
    {
        this.number = number; Name = name; Description = description; Settings = settings; this.bossFinal = bossFinal; OfferTheme = offerTheme;
        if (finalBossStyle == BossStyle.BoxTroll && settings.Matching != MatchType.Snake)
            throw new System.ArgumentException("Box Troll requires connected matching.");
        if (settings.Mode != FreePlayMode.LimitedMoves &&
            (finalBossStyle is BossStyle.CrowdTax or BossStyle.SmallChange or BossStyle.InnerToll or BossStyle.RepeatToll ||
             roundRule is BossStyle.CrowdTax or BossStyle.SmallChange or BossStyle.InnerToll or BossStyle.RepeatToll))
            throw new System.ArgumentException("Move-tax bosses require a limited-moves level.");
        FinalBossStyle = finalBossStyle;
        RoundRule = roundRule;
        blockObjectives?.Validate(settings);
        BlockObjectives = blockObjectives;
        if (habitat.HasValue && !System.Enum.IsDefined(typeof(HabitatStyle), habitat.Value)) throw new System.ArgumentOutOfRangeException(nameof(habitat));
        Habitat = habitat;
        if (!System.Enum.IsDefined(typeof(LevelBonus), bonus)) throw new System.ArgumentOutOfRangeException(nameof(bonus));
        Bonus = bonus;
        if (basePointsPerTile < 0) throw new System.ArgumentOutOfRangeException(nameof(basePointsPerTile));
        BasePointsPerTile = basePointsPerTile;
        if (bossTargetMultiplier < 1 || bossTargetMultiplier > 10) throw new System.ArgumentOutOfRangeException(nameof(bossTargetMultiplier));
        BossTargetMultiplier = bossTargetMultiplier;
        LightningLines = lightningLines;
        AllowStarterRewards = allowStarterRewards;
        this.roundOffers = new Dictionary<int, PowerTags>();
        if (roundOffers != null) foreach (var offer in roundOffers)
        {
            if (offer.Key < 1 || offer.Key > settings.GetTotalRounds) throw new System.ArgumentOutOfRangeException(nameof(roundOffers));
            this.roundOffers.Add(offer.Key, offer.Value);
        }
        if (powerOffers != null && (this.roundOffers.Count > 0 || offerTheme != CampaignOfferTheme.None))
            throw new System.ArgumentException("Use an explicit offer schedule or a legacy preset, not both.");
        if (offerTheme != CampaignOfferTheme.None && this.roundOffers.Count > 0)
            throw new System.ArgumentException("Use one offer schedule source.");
        if (powerOffers == null && this.roundOffers.Count > 0)
        {
            var definitions = new Dictionary<int, PowerOfferDefinition>();
            foreach (var pair in this.roundOffers)
                if (pair.Value != PowerTags.None)
                    definitions.Add(pair.Key, new PowerOfferDefinition("ROUND " + pair.Key + " / " + pair.Value,
                        new[] { new PowerOfferSlot(new PowerTagQuery(all: pair.Value), 3) }));
            powerOffers = new LevelPowerOffers(definitions);
        }
        if (settings.PowerOffers != null && (powerOffers != null || offerTheme != CampaignOfferTheme.None || this.roundOffers.Count > 0))
            throw new System.ArgumentException("Specify the offer schedule on the campaign or its settings, not both.");
        PowerOffers = powerOffers ?? settings.PowerOffers ?? CampaignExpansion.Schedule(offerTheme, settings.Goal, settings.Mode);
        PowerOffers.ValidateRounds(settings.GetTotalRounds);
    }
    public GoalType Goal => Settings.Goal;
    public FreePlayMode Mode => Settings.Mode;
    public int Moves => Settings.Moves;
    public ReplacementStrategy Replacement => Settings.Replacement;
    public int GetLevelNumber => number;
    public int GetTotalRounds => Settings.GetTotalRounds;
    public int GetTotalTime => Settings.GetTotalTime;
    public bool BossPowersPersist => false;
    public int GetHeight(int round) => Settings.GetHeight(round);
    public int GetWidth(int round) => Settings.GetWidth(round);
    public int GetTargetScore(int round) => DifficultyTarget((int)System.Math.Min(int.MaxValue,
        (long)Settings.GetTargetScore(round) * (bossFinal && round == GetTotalRounds ? BossTargetMultiplier : 1)), Difficulty);
    public string GetLevelDescription() => Description;
    public List<MatchRule> GetMatchRules(int round) => Settings.GetMatchRules(round);
    public List<PowerUp> GetPowerUpsForRound(int round) => SharedGameContent.Pool(GetLevelNumber,round) ?? (round == 1 && (Bonus == LevelBonus.GardenPlots || Bonus == LevelBonus.AlternatingCurrent)
        ? Bonus == LevelBonus.GardenPlots
            ? new List<PowerUp> { new BasePowerUp(2),new BiomeOpeningPower(true),new BiomeOpeningPower(false) }
            : new List<PowerUp> { new AnimalDoublePower(ItemType.Duck),new AnimalDoublePower(ItemType.Bear),new SpotlightOpeningPower() }
        : Settings.GetPowerUpsForRound(round));
    public BossPowerMap GetBossPowersMap()
    {
        var map = new BossPowerMap();
        for (int round = 1; round <= GetTotalRounds; round++)
        {
            var rules = new List<BossPowers>();
            if (RoundRule.HasValue) rules.Add(new ChallengeBoss(RoundRule.Value, Goal));
            if (bossFinal && round == GetTotalRounds) rules.Add(BossLibrary.ForLevel(number - 1, Goal, FinalBossStyle, Settings.Matching));
            if (rules.Count > 0) map.AddPower(round, rules);
        }
        return map;
    }
    public CampaignLevel WithScaling(int target, int growthPercent) => new CampaignLevel(number, Name, Description,
        new FreePlayConfig(Settings.Matching, Settings.Width, Settings.Height, Settings.Goal, target,
            Settings.Mode, Settings.Seconds, Settings.Moves, Settings.MinimumMatchLength, Settings.Replacement,
            Settings.Levels, growthPercent), bossFinal, powerOffers: PowerOffers, finalBossStyle: FinalBossStyle,
        roundRule: RoundRule, blockObjectives: BlockObjectives, habitat: Habitat, bonus: Bonus,
        basePointsPerTile: BasePointsPerTile, lightningLines: LightningLines, allowStarterRewards: AllowStarterRewards, bossTargetMultiplier: BossTargetMultiplier) { Difficulty = Difficulty };

    public LevelConfig CloneNew() => OfferTheme != CampaignOfferTheme.None || roundOffers.Count > 0
        ? new CampaignLevel(number, Name, Description, (FreePlayConfig)Settings.CloneNew(), bossFinal, roundOffers, OfferTheme, finalBossStyle: FinalBossStyle, roundRule: RoundRule, blockObjectives: BlockObjectives, habitat: Habitat, bonus: Bonus, basePointsPerTile: BasePointsPerTile, lightningLines: LightningLines, allowStarterRewards: AllowStarterRewards, bossTargetMultiplier: BossTargetMultiplier) { Difficulty = Difficulty }
        : new CampaignLevel(number, Name, Description, (FreePlayConfig)Settings.CloneNew(), bossFinal,
            powerOffers: Settings.PowerOffers == null ? PowerOffers : null, finalBossStyle: FinalBossStyle, roundRule: RoundRule, blockObjectives: BlockObjectives, habitat: Habitat, bonus: Bonus, basePointsPerTile: BasePointsPerTile, lightningLines: LightningLines, allowStarterRewards: AllowStarterRewards, bossTargetMultiplier: BossTargetMultiplier) { Difficulty = Difficulty };
}

public static class LevelLibrary
{
    static LevelLibrary()
    {
        // Author the seed identities, then load the live campaign from shared content.
        for (int i = 1; i < AllLevels.Count; i++)
            AllLevels[i] = CampaignIdentity.Create((CampaignLevel)AllLevels[i]);
        ReloadSharedContent();
    }
    public static void ReloadSharedContent()
    {
        if (!SharedGameContent.IsLoaded) return;
        foreach (var record in SharedGameContent.Document["levels"].Cast<Newtonsoft.Json.Linq.JObject>())
        {
            int number=(int)record["number"];
            if(number<=AllLevels.Count) AllLevels[number-1]=SharedGameContent.ApplyLevel((CampaignLevel)AllLevels[number-1],record);
            else if(number==AllLevels.Count+1) AllLevels.Add(SharedGameContent.ApplyLevel(new CampaignLevel(number,"Custom","",new FreePlayConfig()),record));
            else throw new System.InvalidOperationException("Shared level numbers must be consecutive.");
        }
    }
    public static readonly List<LevelConfig> AllLevels = new()
    {
        new CampaignLevel(1, "Tutorial", "Make a match of four and learn the basics.",
            new FreePlayConfig(levels: 1), bossFinal: true),
        new CampaignLevel(2, "Connected Crew", "Link matching animals into connected groups.",
            new FreePlayConfig(width: 6, height: 6, target: 400, seconds: 60, levels: 5, growthPercent: 50)),
        new CampaignLevel(3, "In a Row", "Build connected groups across a wide board.",
            new FreePlayConfig(MatchType.Snake, 7, 6, target: 350, seconds: 70, levels: 5, growthPercent: 50)),
        new CampaignLevel(4, "Stand Tall", "Collect matching animals on a tall board.",
            new FreePlayConfig(MatchType.Snake, 6, 7, GoalType.BlocksCleared, 36, seconds: 70, levels: 5, growthPercent: 50)),
        new CampaignLevel(5, "Make It Count", "Clear blocks with a carefully planned move budget.",
            new FreePlayConfig(width: 6, height: 6, goal: GoalType.BlocksCleared, target: 45,
                mode: FreePlayMode.LimitedMoves, moves: 16, levels: 5, growthPercent: 50)),
        new CampaignLevel(6, "Falling Friends", "Watch new animals tumble in from above.",
            new FreePlayConfig(target: 450, seconds: 60, replacement: ReplacementStrategy.South, levels: 5, growthPercent: 50),
            roundOffers: new Dictionary<int, PowerTags> { [3] = PowerTags.Animal | PowerTags.Tradeoff }),
        new CampaignLevel(7, "Garden Plots", "", new FreePlayConfig()),
        new CampaignLevel(8, "Alternating Current", "", new FreePlayConfig()),
    };
    public static LevelConfig GetLevel(int number) => number >= 1 && number <= AllLevels.Count ? AllLevels[number - 1] : null;
    public static int TotalLevels => AllLevels.Count;
}

/// <summary>Final campaign tuning pass. Seed layouts remain above; identity owns progression, drafts and rules.</summary>
public static class CampaignIdentity
{
    // One authored habitat per level, in picker order (tutorial excluded).
    private static readonly HabitatStyle[] Habitats = {
        HabitatStyle.Menagerie, HabitatStyle.Giants, HabitatStyle.Shore, HabitatStyle.Garden,
        HabitatStyle.Rapids, HabitatStyle.Shore, HabitatStyle.Shore, HabitatStyle.Menagerie,
        HabitatStyle.Giants, HabitatStyle.Giants,
        HabitatStyle.Menagerie, HabitatStyle.Menagerie, HabitatStyle.Rapids, HabitatStyle.Shore,
        HabitatStyle.Shore, HabitatStyle.Menagerie, HabitatStyle.Giants, HabitatStyle.Giants,
        HabitatStyle.Shore, HabitatStyle.Menagerie, HabitatStyle.Garden, HabitatStyle.Rapids,
        HabitatStyle.Giants, HabitatStyle.Menagerie, HabitatStyle.Giants, HabitatStyle.Shore,
        HabitatStyle.Garden, HabitatStyle.Shore, HabitatStyle.Rapids, HabitatStyle.Rapids,
        HabitatStyle.Giants, HabitatStyle.Shore, HabitatStyle.Giants, HabitatStyle.Shore,
        HabitatStyle.Garden, HabitatStyle.Shore, HabitatStyle.Giants, HabitatStyle.Menagerie,
        HabitatStyle.Giants, HabitatStyle.Menagerie, HabitatStyle.Giants, HabitatStyle.Shore,
        HabitatStyle.Menagerie, HabitatStyle.Garden, HabitatStyle.Rapids, HabitatStyle.Rapids,
        HabitatStyle.Shore, HabitatStyle.Menagerie, HabitatStyle.Giants, HabitatStyle.Shore
    };
    public static CampaignLevel Create(CampaignLevel seed)
    {
        int number = seed.GetLevelNumber;
        if (number == 2) return AnimalIntroduction();
        if (number == 3) return LightningIntroduction();
        if (number == 4) return MatchSizeIntroduction();
        if (number == 5) return BlockMatchSizeIntroduction();
        if (number == 6) return ChainReactionIntroduction();
        if (number == 7 || number == 8) return NewMechanicIntroduction(number);
        var old = seed.Settings;
        var habitat = Habitats[number - 2];
        bool points = old.Goal == GoalType.Score;
        bool highStakes = points && (number == 11 || number == 21 || number == 25 || number == 38 || number == 41 || number == 46 || number == 50);
        bool embargo = points && (number == 13 || number == 17 || number == 39 || number == 49);
        int rounds = 6 + (number - 2) % 4;
        LevelBonus bonus = habitat == HabitatStyle.Rapids ? LevelBonus.Cascade : number % 2 == 0
            ? habitat == HabitatStyle.Giants ? LevelBonus.LargeGroup : habitat == HabitatStyle.Garden ? LevelBonus.Square : LevelBonus.None
            : LevelBonus.None;
        int growth = points ? (highStakes ? 85 : 55 + number % 3 * 5) : 25;
        if (bonus != LevelBonus.None) growth += points ? 15 : 10;
        // Keep campaign boards within the preferred portrait layouts; tutorial is authored separately.
        int width = System.Math.Clamp(old.Width, 6, 7);
        var settings = new FreePlayConfig(old.Matching, width, 7, old.Goal, points ? old.Target : System.Math.Max(8, old.Target / 2),
            old.Mode, old.Seconds, old.Moves, old.MinimumMatchLength, old.Replacement, rounds, growth);
        BossStyle boss = habitat switch {
            HabitatStyle.Shore => (number % 4) switch { 0 => BossStyle.Lighthouse, 1 => BossStyle.CornerCollector, 2 => BossStyle.Wallflower, _ => BossStyle.Seesaw },
            HabitatStyle.Garden => number % 3 == 0 ? BossStyle.CenterStage : BossStyle.BoxTroll,
            HabitatStyle.Rapids => (number % 3) switch { 0 => BossStyle.CascadeSurfer, 1 => BossStyle.CascadeTax, _ => BossStyle.OpeningAct },
            HabitatStyle.Giants => number % 3 == 0 ? BossStyle.Stretch : BossStyle.TrophyHunter,
            _ => (number % 3) switch { 0 => BossStyle.TwinKeeper, 1 => BossStyle.Oddball, _ => BossStyle.EvenSteven } };
        if (highStakes) boss = BossStyle.Jackpot;
        string theme = habitat switch {
            HabitatStyle.Shore => "SHORE / work the edges",
            HabitatStyle.Garden => "GARDEN / build 2x2 squares",
            HabitatStyle.Rapids => "RAPIDS / build cascade chains",
            HabitatStyle.Giants => "GIANTS / hunt groups of 6+",
            _ => "MENAGERIE / switch animals" };
        var objectives = points ? null : CollectionObjectives(number, habitat, settings);
        string description = $"{theme}. {rounds} rounds; +{growth}% target growth each round. " +
            "Draft habitat rewards and tools, then build for the finale. " +
            (bonus != LevelBonus.None ? "LEVEL BONUS: " + LevelBonuses.Description(bonus) + " (rounded down). Higher target growth compensates for this bonus. " : "") +
            (objectives != null ? "Animal collection rounds alternate with shape challenges. Match only the requested animals or patterns for goal credit. " : "") +
            (highStakes ? "HIGH STAKES: premium Engine guaranteed in every draft from round 2; targets climb faster. " : "") +
            (embargo ? "LEVEL RULE: Ducks give no points in every round, including the finale. " : "") +
            "Final boss: " + new ChallengeBoss(boss, old.Goal).Name + ".";
        return new CampaignLevel(number, seed.Name, description, settings,
            powerOffers: objectives == null ? Schedule(seed.Name, habitat, settings, highStakes) : CollectionSchedule(seed.Name, settings, objectives), finalBossStyle: boss,
            roundRule: embargo ? BossStyle.DuckEmbargo : null, blockObjectives: objectives, habitat: habitat, bonus: bonus);
    }
    private static CampaignLevel NewMechanicIntroduction(int number)
    {
        bool garden = number == 7;
        var bonus = garden ? LevelBonus.GardenPlots : LevelBonus.AlternatingCurrent;
        var offers = new Dictionary<int, PowerOfferDefinition>();
        for (int round = 2; round <= 6 + (number-2)%4; round++)
            offers[round] = new PowerOfferDefinition("BUILD YOUR STRATEGY", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring, none: garden ? PowerTags.None : PowerTags.RemoveAnimal), 2),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle | PowerTags.Roster, none: garden ? PowerTags.None : PowerTags.RemoveAnimal)) });
        return new CampaignLevel(number,garden ? "Garden Plots" : "Alternating Current",
            "LEVEL BONUS: " + LevelBonuses.Description(bonus),
            new FreePlayConfig(width: garden ? 6 : 7,height: 7,target: 400,seconds: 75,levels: 6+(number-2)%4,
                growthPercent: 70+number%3*5,replacement: ReplacementStrategy.South),
            powerOffers: new LevelPowerOffers(offers),finalBossStyle: garden ? BossStyle.Overgrowth : BossStyle.Blackout,
            habitat: garden ? HabitatStyle.Garden : HabitatStyle.Rapids,bonus: bonus,basePointsPerTile: 4);
    }

    private static CampaignLevel ChainReactionIntroduction()
    {
        var offers = new Dictionary<int, PowerOfferDefinition>();
        for (int round = 2; round <= 6; round++)
            offers[round] = new PowerOfferDefinition("CHAIN REACTION / BUILD YOUR STRATEGY", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring), 2),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle | PowerTags.Roster)) });
        return new CampaignLevel(6, "Chain Reaction",
            "LEVEL BONUS: " + LevelBonuses.Description(LevelBonus.ChainReaction),
            new FreePlayConfig(width: 7, height: 7, target: 400, seconds: 70, levels: 6, growthPercent: 70,
                replacement: ReplacementStrategy.South),
            powerOffers: new LevelPowerOffers(offers), finalBossStyle: BossStyle.Wallflower,
            habitat: HabitatStyle.Rapids, bonus: LevelBonus.ChainReaction);
    }

    private static CampaignLevel BlockMatchSizeIntroduction()
    {
        var offers = new Dictionary<int, PowerOfferDefinition> {
            [1] = new PowerOfferDefinition("CHOOSE YOUR BLOCK FOUNDATION / 3, 4 OR 5", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.MatchFoundation), 3) })
        };
        for (int round = 2; round <= 9; round++)
            offers[round] = new PowerOfferDefinition("BUILD YOUR CLEARING STRATEGY", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Clearing, none: PowerTags.MatchFoundation), 2),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle | PowerTags.Clearing)) });
        return new CampaignLevel(5, "Clear Commitments",
            "Pick your match size. Bigger matches count for more blocks.\n3+ tiles: 1 block each. 4+: 2 each. 5+: 4 each.",
            new FreePlayConfig(width: 7, height: 7, goal: GoalType.BlocksCleared, target: 60, seconds: 80, levels: 9, growthPercent: 25,
                replacement: ReplacementStrategy.South),
            powerOffers: new LevelPowerOffers(offers), finalBossStyle: BossStyle.Wallflower, habitat: HabitatStyle.Garden);
    }

    private static CampaignLevel MatchSizeIntroduction()
    {
        var offers = new Dictionary<int, PowerOfferDefinition> {
            [1] = new PowerOfferDefinition("CHOOSE YOUR FOUNDATION", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.MatchFoundation), 3) })
        };
        for (int round = 2; round <= 8; round++)
            offers[round] = new PowerOfferDefinition("BUILD ON YOUR FOUNDATION", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring, none: PowerTags.MatchFoundation), 2),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle | PowerTags.Roster)) });
        return new CampaignLevel(4, "Size Matters",
            "Base 4 points per tile. Pick your foundation to earn permanent bonuses for big groups and quick matches.",
            new FreePlayConfig(width: 6, height: 7, target: 400, seconds: 70, levels: 8, growthPercent: 60),
            powerOffers: new LevelPowerOffers(offers), finalBossStyle: BossStyle.Wallflower, habitat: HabitatStyle.Shore,
            basePointsPerTile: 4);
    }

    private static CampaignLevel LightningIntroduction()
    {
        var offers = new Dictionary<int, PowerOfferDefinition>();
        for (int round = 2; round <= 7; round++)
            offers[round] = new PowerOfferDefinition("LIGHTNING / BUILD YOUR STRATEGY", new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring), 2),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle | PowerTags.Roster)) });
        return new CampaignLevel(3, "Lightning Lanes",
            "Match 5+ in a straight line to clear the whole row or column. All other tiles turn blue and give double points until cleared.",
            new FreePlayConfig(width: 7, height: 7, target: 350, seconds: 70, levels: 7, growthPercent: 70,
                replacement: ReplacementStrategy.South),
            powerOffers: new LevelPowerOffers(offers), finalBossStyle: BossStyle.Stretch,
            habitat: HabitatStyle.Rapids, lightningLines: true);
    }

    private static CampaignLevel AnimalIntroduction()
    {
        return new CampaignLevel(2, "Animal Friends",
            "LEVEL BONUS: Your least matched animal gives 2x points. Tied animals share the bonus.",
            new FreePlayConfig(width: 6, height: 7, target: 400, seconds: 60, levels: 4, growthPercent: 50),
            finalBossStyle: BossStyle.MostMatched,
            habitat: HabitatStyle.Menagerie, basePointsPerTile: 4, bonus: LevelBonus.LeastMatched);
    }
    private static BlockObjectives CollectionObjectives(int number, HabitatStyle habitat, FreePlayConfig settings)
    {
        var animal = number switch { 5 => ItemType.Duck, 28 => ItemType.Elephant, 29 => ItemType.Panda,
            30 => ItemType.Moose, 31 => ItemType.Sloth, 32 => ItemType.Chicken, _ => (ItemType)(number * 7 % System.Enum.GetValues(typeof(ItemType)).Length) };
        var shape = settings.Matching == MatchType.Horizontal ? BlockShape.Horizontal : settings.Matching == MatchType.Vertical ? BlockShape.Vertical :
            habitat == HabitatStyle.Garden ? BlockShape.Square : habitat == HabitatStyle.Giants ? BlockShape.Large :
            habitat == HabitatStyle.Rapids ? BlockShape.Plus : BlockShape.Large;
        var rounds = new Dictionary<int, BlockObjective>();
        for (int round = 1; round <= settings.GetTotalRounds; round++)
            rounds.Add(round, round >= 3 && round % 2 == 1 && round != settings.GetTotalRounds
                ? new BlockObjective(shape: shape) : new BlockObjective(animal));
        return new BlockObjectives(rounds);
    }
    private static LevelPowerOffers CollectionSchedule(string name, FreePlayConfig settings, BlockObjectives objectives)
    {
        var rounds = new Dictionary<int, PowerOfferDefinition>();
        for (int round = 1; round <= settings.GetTotalRounds; round++)
        {
            var objective = objectives.ForRound(round);
            rounds.Add(round, new PowerOfferDefinition(name.ToUpperInvariant() + " / " + objective.Label, new[] {
                new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Collection | PowerTags.GoalCredit)),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Transform | PowerTags.Clearing)),
                new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Admission | PowerTags.Budget | PowerTags.Clearing)) }));
        }
        return new LevelPowerOffers(rounds);
    }
    private static LevelPowerOffers Schedule(string name, HabitatStyle habitat, FreePlayConfig settings, bool premium)
    {
        var offers = new Dictionary<int, PowerOfferDefinition>();
        var theme = HabitatPower.ThemeTag(habitat);
        bool points = settings.Goal == GoalType.Score;
        for (int round = 2; round <= settings.GetTotalRounds; round++)
        {
            var slots = new List<PowerOfferSlot>();
            string phase;
            if (premium)
            {
                phase = "PREMIUM ENGINE";
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: theme | PowerTags.Premium)));
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: theme | PowerTags.Scoring, none: PowerTags.Premium)));
                slots.Add(new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Roster | PowerTags.Shuffle | (habitat == HabitatStyle.Garden ? PowerTags.None : PowerTags.Matching))));
            }
            else if (round == 3 && points)
            {
                phase = "RESHAPE THE HABITAT";
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: PowerTags.AddAnimal)));
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: PowerTags.RemoveAnimal)));
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: theme | PowerTags.Scoring)));
            }
            else
            {
                phase = round == settings.GetTotalRounds ? "BOSS PREPARATION" : round % 2 == 0 ? "BUILD YOUR THEME" : "FUEL THE RUN";
                // Menagerie has no block-specific animal-switch effect; offer animal clearing instead.
                var clearTheme = habitat == HabitatStyle.Menagerie ? PowerTags.Animal : theme;
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: points ? theme | PowerTags.Scoring : clearTheme | PowerTags.Clearing)));
                slots.Add(new PowerOfferSlot(new PowerTagQuery(all: points ? PowerTags.Scoring : PowerTags.Clearing,
                    any: points ? theme : PowerTags.None)));
                // Tools can be exhausted in long runs; an additional habitat reward remains a valid alternative.
                slots.Add(new PowerOfferSlot(new PowerTagQuery(any: PowerTags.Budget | PowerTags.Shuffle |
                    (points ? theme | (habitat == HabitatStyle.Garden ? PowerTags.None : PowerTags.Matching) : PowerTags.Clearing))));
            }
            offers.Add(round, new PowerOfferDefinition(name.ToUpperInvariant() + " / ROUND " + round + " / " + phase, slots));
        }
        return new LevelPowerOffers(offers);
    }
}

public enum CampaignOfferTheme { None, Roster, PointsOrBudget, ClearsOrBudget, Patterns, Shuffle }

public static class CampaignExpansion
{
    public static bool AllowOrdinaryPower(CampaignOfferTheme theme, PowerUp power) =>
        Schedule(theme, GoalType.Score, FreePlayMode.Timed).AllowsOrdinary(power, FreePlayMode.Timed);
    public static string OfferLabel(CampaignOfferTheme theme, bool timed) => theme switch
    {
        CampaignOfferTheme.Roster => "RESHAPE YOUR ANIMAL ROSTER",
        CampaignOfferTheme.PointsOrBudget => timed ? "MORE POINTS OR EARN MORE TIME?" : "MORE POINTS OR EARN MORE MOVES?",
        CampaignOfferTheme.ClearsOrBudget => timed ? "CLEAR MORE OR EARN MORE TIME?" : "CLEAR MORE OR EARN MORE MOVES?",
        CampaignOfferTheme.Patterns => "CHOOSE YOUR MATCHING STRATEGY",
        CampaignOfferTheme.Shuffle => "STRONGER MATCHES OR BETTER SHUFFLES?",
        _ => "PICK ONE TO BOOST YOUR RUN"
    };

    // Presets are ordinary tag-based definitions, not selection logic.
    public static PowerOfferDefinition Offer(CampaignOfferTheme theme, GoalType goal, FreePlayMode mode)
    {
        var scoring = new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring), 2);
        var clearing = new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Clearing), 2);
        var budget = new PowerOfferSlot(new PowerTagQuery(all: mode == FreePlayMode.Timed ? PowerTags.TimeReward : PowerTags.MoveReward));
        PowerOfferSlot[] slots;
        switch (theme)
        {
            case CampaignOfferTheme.Roster:
                slots = new[] { new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Roster,
                    any: PowerTags.AddAnimal | PowerTags.RemoveAnimal), 3) }; break;
            case CampaignOfferTheme.PointsOrBudget: slots = new[] { scoring, budget }; break;
            case CampaignOfferTheme.ClearsOrBudget: slots = new[] { clearing, budget }; break;
            case CampaignOfferTheme.Patterns:
                slots = new[] { new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Scoring | PowerTags.Pattern), 3) }; break;
            case CampaignOfferTheme.Shuffle:
                slots = new[] { goal == GoalType.Score ? scoring : clearing,
                    new PowerOfferSlot(new PowerTagQuery(all: PowerTags.Shuffle)) }; break;
            default: return null;
        }
        return new PowerOfferDefinition(OfferLabel(theme, mode == FreePlayMode.Timed), slots);
    }

    public static LevelPowerOffers Schedule(CampaignOfferTheme theme, GoalType goal, FreePlayMode mode)
    {
        var rounds = new Dictionary<int, PowerOfferDefinition>();
        var reservations = new List<PowerTagQuery>();
        var offer = Offer(theme, goal, mode);
        if (offer != null) { rounds.Add(2, offer); rounds.Add(4, offer); }
        if (theme == CampaignOfferTheme.PointsOrBudget || theme == CampaignOfferTheme.ClearsOrBudget)
            reservations.Add(new PowerTagQuery(all: PowerTags.Budget));
        if (theme == CampaignOfferTheme.Shuffle) reservations.Add(new PowerTagQuery(all: PowerTags.Shuffle));
        return new LevelPowerOffers(rounds, reservations);
    }

    public static List<LevelConfig> Create() => new()
    {
        new CampaignLevel(12, "New Neighbors", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 6, 6, GoalType.Score, 240,
                FreePlayMode.Timed, seconds: 65, moves: 22, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(13, "Small Circle", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 7, 7, GoalType.Score, 260,
                FreePlayMode.Timed, seconds: 68, moves: 23, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(14, "Migration South", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 280,
                FreePlayMode.Timed, seconds: 71, moves: 24, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(15, "Northern Visitors", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 300,
                FreePlayMode.Timed, seconds: 74, moves: 25, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(16, "East Side Zoo", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 8, 6, GoalType.Score, 320,
                FreePlayMode.LimitedMoves, seconds: 77, moves: 26, minimumMatchLength: 4,
                replacement: ReplacementStrategy.West, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(17, "West Side Wilds", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 6, 8, GoalType.Score, 340,
                FreePlayMode.LimitedMoves, seconds: 80, moves: 27, minimumMatchLength: 4,
                replacement: ReplacementStrategy.East, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(18, "Long Line Safari", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 360,
                FreePlayMode.Timed, seconds: 80, moves: 28, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(19, "Tall Tales", "Rounds 2 and 4 offer three animal additions or removals. Trade roster variety against your points multiplier.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 380,
                FreePlayMode.Timed, seconds: 80, moves: 29, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Roster),
        new CampaignLevel(20, "Against the Clock", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 6, 6, GoalType.Score, 260,
                FreePlayMode.Timed, seconds: 65, moves: 22, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(21, "Time Is Money", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 7, 7, GoalType.Score, 280,
                FreePlayMode.Timed, seconds: 68, moves: 23, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(22, "Overtime Orchard", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 300,
                FreePlayMode.Timed, seconds: 71, moves: 24, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(23, "Seconds Upstream", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 320,
                FreePlayMode.Timed, seconds: 74, moves: 25, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(24, "Move Dividend", "Rounds 2 and 4: choose stronger scoring or earn extra moves through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 8, 6, GoalType.Score, 340,
                FreePlayMode.LimitedMoves, seconds: 77, moves: 26, minimumMatchLength: 4,
                replacement: ReplacementStrategy.West, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(25, "Last Move Market", "Rounds 2 and 4: choose stronger scoring or earn extra moves through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 6, 8, GoalType.Score, 360,
                FreePlayMode.LimitedMoves, seconds: 80, moves: 27, minimumMatchLength: 4,
                replacement: ReplacementStrategy.East, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(26, "Row Rush", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 380,
                FreePlayMode.Timed, seconds: 80, moves: 28, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(27, "Column Countdown", "Rounds 2 and 4: choose stronger scoring or earn extra time through matching patterns.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 400,
                FreePlayMode.Timed, seconds: 80, moves: 29, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.PointsOrBudget),
        new CampaignLevel(28, "Clear Priorities", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 6, 6, GoalType.BlocksCleared, 24,
                FreePlayMode.Timed, seconds: 65, moves: 22, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(29, "Breathing Room", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 7, 7, GoalType.BlocksCleared, 26,
                FreePlayMode.Timed, seconds: 68, moves: 23, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(30, "Cascade Cleanup", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.BlocksCleared, 28,
                FreePlayMode.Timed, seconds: 71, moves: 24, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(31, "Rising Rescue", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.BlocksCleared, 30,
                FreePlayMode.Timed, seconds: 74, moves: 25, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(32, "Careful Clearance", "Rounds 2 and 4: choose extra block clearing or earn extra moves.",
            new FreePlayConfig(MatchType.Snake, 8, 6, GoalType.BlocksCleared, 32,
                FreePlayMode.LimitedMoves, seconds: 77, moves: 26, minimumMatchLength: 4,
                replacement: ReplacementStrategy.West, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(33, "Sideways Sweep", "Rounds 2 and 4: choose extra block clearing or earn extra moves.",
            new FreePlayConfig(MatchType.Snake, 6, 8, GoalType.BlocksCleared, 34,
                FreePlayMode.LimitedMoves, seconds: 80, moves: 27, minimumMatchLength: 4,
                replacement: ReplacementStrategy.East, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(34, "Row Renovation", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.BlocksCleared, 36,
                FreePlayMode.Timed, seconds: 80, moves: 28, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(35, "Column Collector", "Rounds 2 and 4: choose extra block clearing or earn extra time.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.BlocksCleared, 38,
                FreePlayMode.Timed, seconds: 80, moves: 29, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.ClearsOrBudget),
        new CampaignLevel(36, "Shape School", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 6, 6, GoalType.Score, 300,
                FreePlayMode.Timed, seconds: 65, moves: 22, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(37, "Corner Club", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 7, 7, GoalType.Score, 320,
                FreePlayMode.Timed, seconds: 68, moves: 23, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(38, "Big Group Grove", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 340,
                FreePlayMode.Timed, seconds: 71, moves: 24, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(39, "Alternating Avenue", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 360,
                FreePlayMode.Timed, seconds: 74, moves: 25, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(40, "Opening Gambit", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 8, 6, GoalType.Score, 380,
                FreePlayMode.LimitedMoves, seconds: 77, moves: 26, minimumMatchLength: 4,
                replacement: ReplacementStrategy.West, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(41, "Double Trouble", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 6, 8, GoalType.Score, 400,
                FreePlayMode.LimitedMoves, seconds: 80, moves: 27, minimumMatchLength: 4,
                replacement: ReplacementStrategy.East, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(42, "Straight Thinking", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 420,
                FreePlayMode.Timed, seconds: 80, moves: 28, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(43, "Skyline Strategy", "Rounds 2 and 4 offer three scoring strategies. Build around patterns you can reliably match.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 440,
                FreePlayMode.Timed, seconds: 80, moves: 29, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Patterns),
        new CampaignLevel(44, "Fresh Start", "Rounds 2 and 4: choose stronger scoring or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 6, 6, GoalType.Score, 320,
                FreePlayMode.Timed, seconds: 65, moves: 22, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(45, "Second Chances", "Rounds 2 and 4: choose stronger scoring or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 7, 7, GoalType.Score, 340,
                FreePlayMode.Timed, seconds: 68, moves: 23, minimumMatchLength: 4,
                replacement: ReplacementStrategy.InPlace, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(46, "Falling Fortune", "Rounds 2 and 4: choose stronger scoring or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.Score, 360,
                FreePlayMode.Timed, seconds: 71, moves: 24, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(47, "Rising Remix", "Rounds 2 and 4: choose stronger clearing or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.BlocksCleared, 30,
                FreePlayMode.Timed, seconds: 74, moves: 25, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(48, "Westward Reset", "Rounds 2 and 4: choose stronger clearing or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 8, 6, GoalType.BlocksCleared, 32,
                FreePlayMode.LimitedMoves, seconds: 77, moves: 26, minimumMatchLength: 4,
                replacement: ReplacementStrategy.West, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(49, "Eastward Escape", "Rounds 2 and 4: choose stronger scoring or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 6, 8, GoalType.Score, 420,
                FreePlayMode.LimitedMoves, seconds: 80, moves: 27, minimumMatchLength: 4,
                replacement: ReplacementStrategy.East, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(50, "Row Rewind", "Rounds 2 and 4: choose stronger scoring or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 8, 7, GoalType.Score, 440,
                FreePlayMode.Timed, seconds: 80, moves: 28, minimumMatchLength: 4,
                replacement: ReplacementStrategy.South, levels: 5, growthPercent: 25),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
        new CampaignLevel(51, "Column Comeback", "Rounds 2 and 4: choose stronger clearing or improve your shuffles.",
            new FreePlayConfig(MatchType.Snake, 7, 8, GoalType.BlocksCleared, 38,
                FreePlayMode.Timed, seconds: 80, moves: 29, minimumMatchLength: 4,
                replacement: ReplacementStrategy.North, levels: 5, growthPercent: 15),
            bossFinal: true, offerTheme: CampaignOfferTheme.Shuffle),
    };
}
