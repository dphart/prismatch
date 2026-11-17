using System;
using System.Collections.Generic;

public enum LevelLessonKind
{
    Matching, AnimalFriends, Lightning, MatchSize, BlockSize, ChainReaction,
    GardenPlots, AlternatingCurrent, BorderPatrol, TreasureTrail, ChangingTides, CascadeBonus, LargeBonus, SquareBonus, MigrationSeason, CrackTheVault, SteppingStones, AnimalRescue, Crosswinds, GoldenRescue, WildRelease, Blackout, AnimalSpotlight, RollingDarkness, LightningRods
}

public enum LessonTint { Normal, Match, Charged, Planted, Hidden }

/// <summary>Presentation data only. Lessons never move, clear or score the live board.</summary>
public sealed class LessonTileFrame
{
    public bool Visible, IconVisible, Treasure;
    public bool Cage, CageOpen, Rod;
    public ItemType Animal;
    public LessonTint Tint;
    public float X, Y, IconX, IconY, IconScale = 1;
    public string Badge = "";
}

public sealed class LevelLessonFrame
{
    public readonly LessonTileFrame[] Tiles = new LessonTileFrame[64];
    public readonly string[] RowLabels = new string[3];
    public int Columns, Rows, LitEdges;
    public float Step, GridX, Opacity = 1, BoardOpacity = 1, Lightning;
    public int LightningRow;
    public bool ShowEdges, ShowDirection, RodChain;
    public ReplacementStrategy Direction;
    public string Caption = "", Status = "", Footer = "";
    public LevelLessonFrame() { for (int i = 0; i < Tiles.Length; i++) Tiles[i] = new LessonTileFrame(); }
    public LessonTileFrame Cell(int x, int y) => Tiles[y * Columns + x];
    public void Reset()
    {
        foreach (var tile in Tiles) {
            tile.Visible = false; tile.IconVisible = true; tile.Treasure = false;
            tile.Cage=tile.CageOpen=tile.Rod=false;
            tile.Tint = LessonTint.Normal; tile.IconX = tile.IconY = 0; tile.IconScale = 1; tile.Badge = "";
        }
        for (int i = 0; i < RowLabels.Length; i++) RowLabels[i] = "";
        LitEdges = 0; GridX = 0; Lightning = 0; ShowEdges = ShowDirection = RodChain = false;
        Opacity = BoardOpacity = 1; Caption = Status = Footer = "";
    }
}

/// <summary>Deterministic, seekable storyboards shared by playback and tests.</summary>
public sealed class LevelLessonStoryboard
{
    public LevelLessonKind Kind { get; }
    public int Minimum { get; }
    public MatchType Matching { get; }
    public string Heading { get; }
    public float Duration { get; }
    private readonly bool blocks;

    public LevelLessonStoryboard(LevelLessonKind kind, int minimum = 4, MatchType matching = MatchType.Snake, bool blockGoal = false)
    {
        Kind = kind; Minimum = Math.Max(3, Math.Min(10, minimum)); Matching = matching; blocks = blockGoal;
        Heading = kind switch {
            LevelLessonKind.LightningRods=>"CHARGE THREE RODS. LIGHT THE BOARD.",
            LevelLessonKind.AnimalSpotlight=>"TWO ANIMALS IN THE DARK",
            LevelLessonKind.RollingDarkness=>"WATCH THE DARKNESS SPREAD",
            LevelLessonKind.Blackout=>"REMEMBER. MATCH. REVEAL.",
            LevelLessonKind.WildRelease=>"OPEN THE CAGE. MATCH EVERY ANIMAL.",
            LevelLessonKind.GoldenRescue=>"FREE THE GOLDEN ANIMALS: 3x",
            LevelLessonKind.MigrationSeason=>"FOLLOW THE HIGHLIGHT: 2x",
            LevelLessonKind.CrackTheVault=>"TWO HITS ON EVERY SIDE",
            LevelLessonKind.SteppingStones=>"FOLLOW THE GLOW: 3x",
            LevelLessonKind.AnimalRescue=>"RESCUE THREE DIFFERENT ANIMALS",
            LevelLessonKind.Crosswinds=>"CROSS CLEAR / 5x CASCADES",
            LevelLessonKind.CascadeBonus => "CASCADES: +25%",
            LevelLessonKind.LargeBonus => "GROUPS OF 6+: +50%",
            LevelLessonKind.SquareBonus => "2x2 SQUARES: +25%",
            LevelLessonKind.AnimalFriends => "LEAST MATCHED: 2x",
            LevelLessonKind.Lightning => "MATCH FIVE. CLEAR THE LINE.",
            LevelLessonKind.MatchSize => "CHOOSE YOUR MATCH SIZE",
            LevelLessonKind.BlockSize => "BIGGER MATCHES, MORE BLOCKS",
            LevelLessonKind.ChainReaction => "SET UP A CHAIN REACTION",
            LevelLessonKind.GardenPlots => "PLANT A SQUARE",
            LevelLessonKind.AlternatingCurrent => "DUCK / BEAR: 3x",
            LevelLessonKind.BorderPatrol => "LIGHT ALL FOUR SIDES",
            LevelLessonKind.TreasureTrail => "FIND THREE TREASURES",
            LevelLessonKind.ChangingTides => "WATCH THE TIDE TURN",
            _ => "SWAP. MATCH. CLEAR."
        };
        Duration = kind switch {
            LevelLessonKind.WildRelease => 8.6f,
            LevelLessonKind.TreasureTrail => 8.8f,
            LevelLessonKind.ChangingTides => 8f,
            LevelLessonKind.MatchSize or LevelLessonKind.BlockSize => 6.6f,
            LevelLessonKind.AlternatingCurrent or LevelLessonKind.BorderPatrol => 6.4f,
            _ => 5.8f
        };
    }

    public float[] StepStarts => Kind switch {
        LevelLessonKind.LightningRods => new[]{0f,1.5f,3f,4f},
        LevelLessonKind.WildRelease => new[]{0f,2.5f,3.2f,4.3f,6f},
        LevelLessonKind.Blackout => new[]{0f,1.3f,2.4f,3.4f,4.5f},
        LevelLessonKind.AnimalSpotlight => new[]{0f,2.8f},
        LevelLessonKind.RollingDarkness => new[]{0f,1f,2f,3f,4.3f},
        LevelLessonKind.GoldenRescue => new[]{0f,1f,2f,3f},
        LevelLessonKind.Crosswinds => new[]{0f,1.4f,3f,3.6f},
        LevelLessonKind.CrackTheVault => new[]{0f,3f},
        LevelLessonKind.SteppingStones => new[]{0f,2f},
        LevelLessonKind.MigrationSeason or LevelLessonKind.AnimalRescue => new[]{0f,2f,4f},
        LevelLessonKind.Lightning => new[]{0f,1.3f,1.95f,2.55f},
        LevelLessonKind.AnimalFriends => new[]{0f,1.5f,3f},
        LevelLessonKind.MatchSize or LevelLessonKind.BlockSize => new[]{0f,2f,4f},
        LevelLessonKind.ChainReaction => new[]{0f,1.75f,2.25f,3f},
        LevelLessonKind.GardenPlots => new[]{0f,1.8f,2.65f,3.6f},
        LevelLessonKind.AlternatingCurrent => new[]{0f,1.8f,2.65f,4.45f,5.2f},
        LevelLessonKind.BorderPatrol => new[]{0f,1.25f,2.9f,3.75f,5.3f},
        LevelLessonKind.TreasureTrail => new[]{0f,1.9f,3.8f,5.7f,6.6f},
        LevelLessonKind.ChangingTides => new[]{0f,4.8f,6.15f},
        LevelLessonKind.CascadeBonus => new[]{0f,1.75f,3f},
        LevelLessonKind.LargeBonus or LevelLessonKind.SquareBonus => new[]{0f,1.4f},
        _ => new[]{0f,1.4f,2.4f}
    };
    public int StepAt(float time)
    {
        float t=((time%Duration)+Duration)%Duration;var starts=StepStarts;
        for(int i=starts.Length-1;i>=0;i--)if(t>=starts[i])return i;
        return 0;
    }
    public float ReadingDuration {
        get { var starts=StepStarts;float total=0;for(int i=0;i<starts.Length;i++)total+=Math.Max(3f,(i+1<starts.Length?starts[i+1]:Duration)-starts[i]);return total; }
    }
    public float StoryTimeForReading(float time)
    {
        var starts=StepStarts;float remaining=((time%ReadingDuration)+ReadingDuration)%ReadingDuration;
        for(int i=0;i<starts.Length;i++) {
            float span=(i+1<starts.Length?starts[i+1]:Duration)-starts[i],reading=Math.Max(3f,span);
            if(remaining<reading)return starts[i]+span*remaining/reading;
            remaining-=reading;
        }
        return 0;
    }

    public static LevelLessonStoryboard ForLevel(LevelConfig level, IReadOnlyList<MatchRule> rules)
    {
        var campaign = level as CampaignLevel;
        var kind = campaign?.LightningLines == true ? LevelLessonKind.Lightning : campaign?.Bonus switch {
            LevelBonus.LightningRods=>LevelLessonKind.LightningRods,
            LevelBonus.AnimalSpotlight=>LevelLessonKind.AnimalSpotlight,
            LevelBonus.RollingDarkness=>LevelLessonKind.RollingDarkness,
            LevelBonus.Blackout=>LevelLessonKind.Blackout,
            LevelBonus.WildRelease=>LevelLessonKind.WildRelease,
            LevelBonus.GoldenRescue=>LevelLessonKind.GoldenRescue,
            LevelBonus.MigrationSeason=>LevelLessonKind.MigrationSeason,LevelBonus.CrackTheVault=>LevelLessonKind.CrackTheVault,
            LevelBonus.SteppingStones=>LevelLessonKind.SteppingStones,LevelBonus.AnimalRescue=>LevelLessonKind.AnimalRescue,LevelBonus.Crosswinds=>LevelLessonKind.Crosswinds,
            LevelBonus.Cascade => LevelLessonKind.CascadeBonus,
            LevelBonus.LargeGroup => LevelLessonKind.LargeBonus,
            LevelBonus.Square => LevelLessonKind.SquareBonus,
            LevelBonus.LeastMatched => LevelLessonKind.AnimalFriends,
            LevelBonus.ChainReaction => LevelLessonKind.ChainReaction,
            LevelBonus.GardenPlots => LevelLessonKind.GardenPlots,
            LevelBonus.AlternatingCurrent => LevelLessonKind.AlternatingCurrent,
            LevelBonus.BorderPatrol => LevelLessonKind.BorderPatrol,
            LevelBonus.TreasureTrail => LevelLessonKind.TreasureTrail,
            LevelBonus.ChangingTides => LevelLessonKind.ChangingTides,
            _ => campaign?.GetLevelNumber == 4 ? LevelLessonKind.MatchSize :
                campaign?.GetLevelNumber == 5 ? LevelLessonKind.BlockSize : LevelLessonKind.Matching
        };
        var rule = rules != null && rules.Count > 0 ? rules[0] : new MatchRule(MatchType.Snake, 4);
        return new LevelLessonStoryboard(kind, rule.minimumLength, rule.type, (level as ILevelSettings)?.Goal == GoalType.BlocksCleared);
    }

    private static float Progress(float time, float start, float end) => Math.Max(0, Math.Min(1, (time - start) / (end - start)));
    private static float Smooth(float p) => p * p * (3 - 2 * p);
    private static float Window(float t, float start, float end) => t >= start && t < end ? 1 : 0;
    private static void Grid(LevelLessonFrame f, int columns, int rows, ItemType first = ItemType.Chicken, ItemType second = ItemType.Hippo)
    {
        f.Columns = columns; f.Rows = rows; f.Step = Math.Min(56, Math.Min(400f / columns, 196f / rows));
        for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++) {
            var tile = f.Cell(x, y); tile.Visible = true;
            tile.Animal = (x + y) % 2 == 0 ? first : second;
            tile.X = (x - (columns - 1) * .5f) * f.Step;
            tile.Y = ((rows - 1) * .5f - y) * f.Step;
        }
    }
    private static void Animal(LevelLessonFrame f, ItemType animal, params int[] xy)
    { for (int i = 0; i < xy.Length; i += 2) f.Cell(xy[i], xy[i + 1]).Animal = animal; }
    private static void Swap(LevelLessonFrame f, int ax, int ay, int bx, int by, float progress)
    {
        var a = f.Cell(ax, ay); var b = f.Cell(bx, by);
        if (progress >= 1) { var animal = a.Animal; a.Animal = b.Animal; b.Animal = animal; return; }
        a.IconX = (b.X - a.X) * Smooth(progress); a.IconY = (b.Y - a.Y) * Smooth(progress);
        b.IconX = -a.IconX; b.IconY = -a.IconY;
        if (progress > 0) a.Tint = b.Tint = LessonTint.Match;
    }
    private static void Match(LevelLessonFrame f, float t, float highlight, float clear, params int[] xy)
    {
        foreach (int index in Indices(f, xy)) {
            var tile = f.Tiles[index];
            if (t >= highlight) { tile.Tint = LessonTint.Match; tile.IconScale = 1 + .12f * (1 - Progress(t, highlight, clear)); }
            if (t >= clear) tile.IconScale = 1 - Smooth(Progress(t, clear, clear + .28f));
            if (t >= clear + .28f) tile.Tint = LessonTint.Normal;
        }
    }
    private static IEnumerable<int> Indices(LevelLessonFrame f, int[] xy)
    { for (int i = 0; i < xy.Length; i += 2) yield return xy[i + 1] * f.Columns + xy[i]; }
    private static int[] Row(int y, int count)
    { var xy = new int[count * 2]; for (int i = 0; i < count; i++) { xy[i * 2] = i; xy[i * 2 + 1] = y; } return xy; }
    private static void FourInRow(LevelLessonFrame f, ItemType animal, ItemType other, float t)
    {
        Grid(f, 4, 3);
        Animal(f, animal, 0, 1, 1, 1, 2, 1, 3, 0); Animal(f, other, 3, 1);
        Swap(f, 3, 0, 3, 1, Progress(t, .25f, .8f));
        Match(f, t, .9f, 1.2f, Row(1, 4));
    }

    public LevelLessonFrame Sample(float time)
    { var frame = new LevelLessonFrame(); WriteFrame(time, frame); return frame; }

    public void WriteFrame(float time, LevelLessonFrame frame)
    {
        frame.Reset();
        float t = ((time % Duration) + Duration) % Duration;
        switch (Kind) {
            case LevelLessonKind.LightningRods: RodLesson(frame,t);break;
            case LevelLessonKind.AnimalSpotlight: SpotlightLesson(frame,t);break;
            case LevelLessonKind.RollingDarkness: RollingLesson(frame,t);break;
            case LevelLessonKind.Blackout: BlackoutLesson(frame,t);break;
            case LevelLessonKind.WildRelease: WildReleaseLesson(frame,t);break;
            case LevelLessonKind.GoldenRescue: GoldenRescueLesson(frame,t);break;
            case LevelLessonKind.CascadeBonus: CascadeBonus(frame, t); break;
            case LevelLessonKind.LargeBonus: case LevelLessonKind.SquareBonus: ShapeBonus(frame, t); break;
            case LevelLessonKind.MigrationSeason: case LevelLessonKind.CrackTheVault: case LevelLessonKind.SteppingStones:
            case LevelLessonKind.AnimalRescue: case LevelLessonKind.Crosswinds: StrategyLesson(frame,t);break;
            case LevelLessonKind.Lightning: Lightning(frame, t); break;
            case LevelLessonKind.AnimalFriends: AnimalFriends(frame, t); break;
            case LevelLessonKind.MatchSize: case LevelLessonKind.BlockSize: Sizes(frame, t); break;
            case LevelLessonKind.ChainReaction: ChainReaction(frame, t); break;
            case LevelLessonKind.GardenPlots: Garden(frame, t); break;
            case LevelLessonKind.AlternatingCurrent: Alternating(frame, t); break;
            case LevelLessonKind.BorderPatrol: Border(frame, t); break;
            case LevelLessonKind.TreasureTrail: Treasure(frame, t); break;
            case LevelLessonKind.ChangingTides: Tides(frame, t); break;
            default: MatchingLesson(frame, t); break;
        }
        frame.Opacity = Progress(t, 0, .18f) * (1 - Progress(t, Duration - .3f, Duration));
    }

    private void RodLesson(LevelLessonFrame f,float t)
    {
        Grid(f,7,7);
        var rods=new[]{new BoardModel.Cell(1,1),new BoardModel.Cell(5,1),new BoardModel.Cell(3,5)};
        for(int i=0;i<rods.Length;i++) {
            var c=rods[i];var tile=f.Cell(c.X,c.Y);tile.Rod=true;tile.IconVisible=false;
            int charge=t>=4?0:t>=3?3:i==0&&t>=1.5f?3:0;
            tile.Badge=(i+1)+": "+charge+"/3";
        }
        foreach(var c in new[]{new BoardModel.Cell(0,1),new(0,0),new(1,0),new(2,0),new(2,1)}) {
            f.Cell(c.X,c.Y).Animal=ItemType.Duck;
            if(t<3)f.Cell(c.X,c.Y).Tint=LessonTint.Match;
        }
        f.RodChain=t>=3&&t<4;f.Lightning=f.RodChain?1:0;
        if(t>=4)foreach(var tile in f.Tiles)if(tile.Visible&&!tile.Rod){tile.Tint=LessonTint.Charged;tile.Badge="x2";}
        f.Caption=t<1.5f?"Each matched tile beside a rod adds one charge.":t<3?"This match touches three sides: one rod is full!":t<4?"Fill all three rods. Lightning chains through them.":"Every animal square gets 2x until it clears.";
        f.Status=t<3?"3 CHARGES PER ROD":t<4?"ALL THREE READY":"RODS RESET / CHARGE THEM AGAIN";
        f.Footer="Cascades charge rods too. Diagonals do not count.";
    }

    private void SpotlightLesson(LevelLessonFrame f,float t)
    {
        Grid(f,5,4);
        var pool=new[]{ItemType.Duck,ItemType.Bear,ItemType.Chicken,ItemType.Giraffe};
        int start=t<2.8f?0:2;
        for(int y=0;y<4;y++)for(int x=0;x<5;x++) {
            var tile=f.Cell(x,y);tile.Animal=pool[(x+y)%4];
            tile.IconVisible=tile.Animal!=pool[start]&&tile.Animal!=pool[start+1];
            if(!tile.IconVisible)tile.Tint=LessonTint.Hidden;
        }
        f.Caption=t<2.8f?"Duck and Bear are dark. All other animals are visible.":"After 5 player matches, Chicken and Giraffe become dark.";
        f.Status="DARK: "+pool[start]+" + "+pool[start+1];
        f.Footer="Two dark animal types at a time. Hidden animals still match.";
    }
    private void RollingLesson(LevelLessonFrame f,float t)
    {
        Grid(f,5,4);
        int dark=t<1?0:t<2?2:t<3?4:t<4.3f?0:2;
        for(int x=0;x<dark;x++){f.Cell(x,0).IconVisible=false;f.Cell(x,0).Tint=LessonTint.Hidden;}
        if(t>=3&&t<4.3f)for(int x=0;x<4;x++)f.Cell(x,1).Tint=LessonTint.Match;
        f.Caption=t<1?"The board starts visible.":t<2?"Match 4 tiles: 2 squares go dark.":t<3?"Match 4 more: the next 2 squares go dark.":t<4.3f?"Matches reveal new tiles and their neighbors.":"Only half as many squares darken as tiles matched.";
        f.Status="2 MATCHED TILES = 1 DARK SQUARE";
        f.Footer="Cascades reveal tiles without spreading darkness.";
    }
    private void BlackoutLesson(LevelLessonFrame f,float t)
    {
        Grid(f,5,4);
        Animal(f,ItemType.Bear,0,1,1,1,2,1,3,1);
        if(t>=1.3f)foreach(var tile in f.Tiles)if(tile.Visible){tile.IconVisible=false;tile.Tint=LessonTint.Hidden;}
        if(t>=2.4f)for(int x=0;x<4;x++) {
            var tile=f.Cell(x,1);tile.IconVisible=true;tile.Tint=LessonTint.Match;
        }
        if(t>=3.4f)for(int x=0;x<4;x++) {
            var tile=f.Cell(x,1);tile.Animal=x%2==0?ItemType.Duck:ItemType.Chicken;tile.Tint=LessonTint.Planted;
            if(t<4.5f)foreach(int y in new[]{0,2}){f.Cell(x,y).IconVisible=true;f.Cell(x,y).Tint=LessonTint.Planted;}
        }
        if(t>=3.4f&&t<4.5f){f.Cell(4,1).IconVisible=true;f.Cell(4,1).Tint=LessonTint.Planted;}
        f.Caption=t<1.3f?"Study the animals. Every 5 player matches: blackout.":t<2.4f?"The whole board goes black. Hidden animals still match.":t<3.4f?"Make a match from memory.":t<4.5f?"Cleared squares turn green and cannot go dark again.":"Boss: only the new tiles are revealed.";
        f.Status=t<1.3f?"BLACKOUT IN 1 MATCH":t<4.5f?"MATCH TO REVEAL":"NIGHT KEEPER / NO NEIGHBOR REVEALS";
        f.Footer="Green squares stay safe for this round, including future blackouts.";
    }

    private void WildReleaseLesson(LevelLessonFrame f,float t)
    {
        Grid(f,5,4);
        Animal(f,ItemType.Duck,0,0,3,0,1,1,4,1,0,3,3,3);
        Animal(f,ItemType.Bear,0,1,1,1,2,1,3,1);
        var cage=f.Cell(2,2);cage.Cage=true;cage.IconVisible=false;cage.CageOpen=t>=2.5f;
        cage.Tint=cage.CageOpen?LessonTint.Planted:LessonTint.Normal;
        cage.Badge=t<1?"8/10":t<2.5f?"9/10":"OPEN";
        if(t>=1&&t<2.5f)for(int x=0;x<4;x++)f.Cell(x,1).Tint=LessonTint.Match;
        if(t>=3.2f&&t<4.3f) {
            var duck=f.Cell(0,0);float p=Smooth(Progress(t,3.2f,4.3f));
            duck.IconX=(cage.X-duck.X)*p;duck.IconY=(cage.Y-duck.Y)*p;
        }
        if(t>=4.3f)foreach(var tile in f.Tiles)if(tile.Visible&&tile.Animal==ItemType.Duck&&!tile.Cage) {
            tile.Tint=LessonTint.Match;tile.IconScale=1-Smooth(Progress(t,4.9f,5.4f));
        }
        if(t>=6) {
            cage.Cage=false;cage.IconVisible=true;cage.Badge="";
            var next=f.Cell(4,2);next.Cage=true;next.IconVisible=false;next.Badge="0/10";
        }
        f.Caption=t<2.5f?"Make 10 matches directly beside the cage.":t<3.2f?"The door opens. Choose an animal.":
            t<4.3f?"Drag it into the open cage.":t<6?"Every Duck on the board scores as one big match!":"Ducks leave the refill pool until next round.";
        f.Status=t<2.5f?"EACH NEARBY MATCH: +1 / CASCADES COUNT":t<6?"ALL MATCHING ANIMALS / FULL POINTS":"NEW CAGE: 0/10";
        f.Footer="Costs 1 move. Cage rests at 3 animal types; retry restores the pool.";
    }

    private void GoldenRescueLesson(LevelLessonFrame f,float t)
    {
        Grid(f,5,4);
        var rescued=f.Cell(2,2);rescued.Animal=ItemType.Bear;
        rescued.Badge=t<2?"CAGE":"x3";rescued.Treasure=t>=2;
        if(t>=1&&t<2)for(int x=0;x<4;x++)f.Cell(x,1).Tint=LessonTint.Match;
        if(t>=3) {
            float progress=Progress(t,3,3.7f);Swap(f,2,2,4,3,progress);
            if(progress>=1){rescued.Badge="";rescued.Treasure=false;f.Cell(4,3).Badge="x3";f.Cell(4,3).Treasure=true;}
        }
        f.Caption=t<1?"Every 2 matched tiles cage one animal.":t<2?"Match directly beside a cage to open it.":t<3?"The freed tile earns 3x block credit. Look for its gold border.":"Swap the gold animal: its 3x bonus moves with it.";
        f.Status="6 MATCHED = 3 CAGES / GOLD TILES: x3";
        f.Footer="Boss: 1 cage per matched tile. Gold animals cannot be caged again.";
    }

    private void StrategyLesson(LevelLessonFrame f,float t)
    {
        if(Kind==LevelLessonKind.Crosswinds) {
            Grid(f,7,7);f.ShowDirection=true;f.Direction=t<3?ReplacementStrategy.South:ReplacementStrategy.West;
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)if(x==3||y==3) {
                var c=f.Cell(x,y);if(t>=1.4f)c.Tint=LessonTint.Charged;
                if(t>=3)c.IconScale=1-Smooth(Progress(t,3,3.4f));
            }
            if(t>=3.6f)for(int x=0;x<4;x++){var c=f.Cell(x,1);c.Animal=ItemType.Bear;c.IconScale=1;c.Tint=LessonTint.Match;c.Badge="x5";}
            f.Caption=t<1.4f?"Make three matches to turn gravity.":t<3?"One match to go: the middle cross glows.":t<3.6f?"Turn gravity. Clear the cross for 0 points.":"The resulting cascades earn 5x points.";
            f.Status=t<3?"CROSS IN "+(t<1.4f?2:1)+" MATCHES":"CROSS: 0 / CASCADES: x5";
            f.Footer="Boss: the next gravity direction is hidden and unpredictable.";return;
        }
        bool animalLesson=Kind is LevelLessonKind.MigrationSeason or LevelLessonKind.AnimalRescue;
        Grid(f,5,4,animalLesson?ItemType.Duck:ItemType.Chicken,animalLesson?ItemType.Bear:ItemType.Hippo);
        if(Kind==LevelLessonKind.CrackTheVault) {
            var vault=f.Cell(2,2);vault.IconVisible=false;vault.Badge="VAULT";
            foreach(var xy in new[]{(2,1),(3,2),(2,3),(1,2)})f.Cell(xy.Item1,xy.Item2).Tint=LessonTint.Match;
            if(t>=3)foreach(var tile in f.Tiles)if(tile.Visible&&tile.IconVisible){tile.Tint=LessonTint.Planted;tile.Badge="x2";}
            f.Caption=t<3?"Hit each side twice to open the vault.":"Every remaining tile gives double block credit.";
            f.Status=t<3?"TOP 2/2 / RIGHT 2/2 / BOTTOM 1/2 / LEFT 2/2":"VAULT OPEN / NEW TILES: NORMAL";
            f.Footer="Boss: player matches on unboosted slots erase all remaining boosts.";return;
        }
        if(Kind==LevelLessonKind.SteppingStones) {
            foreach(var xy in new[]{(0,1),(1,1),(1,2),(2,2)})f.Cell(xy.Item1,xy.Item2).Tint=LessonTint.Charged;
            Animal(f,ItemType.Duck,0,1,1,1,2,1,3,1);
            if(t>=2)for(int x=0;x<4;x++){f.Cell(x,1).Tint=LessonTint.Match;f.Cell(x,1).Badge="x3";}
            f.Caption=t<2?"Touch two glowing spaces with a match.":"Earn 3x points and extend the glowing path.";
            f.Status="TWO GLOWING SPACES: x3";f.Footer="Boss: miss the path and lose one glowing space.";return;
        }
        bool rescue=Kind==LevelLessonKind.AnimalRescue;
        var target=t<3?ItemType.Duck:ItemType.Bear;
        for(int y=0;y<4;y++)for(int x=0;x<5;x++)if(f.Cell(x,y).Animal==target){f.Cell(x,y).Tint=LessonTint.Planted;f.Cell(x,y).Badge="x2";}
        f.Caption=rescue?"Match the marked animal for double block credit.":"Match the highlighted animal for double points.";
        f.Status=(rescue?"RESCUE ":"FOLLOW ")+target.ToString().ToUpperInvariant()+" / x2";
        f.Footer=rescue?"Three different rescues: +1 move. Boss: other animals add cages.":"The highlight changes after your move. Boss: other animals score half.";
    }

    private static void CascadeBonus(LevelLessonFrame f, float t)
    {
        ChainReaction(f,t);
        foreach(var tile in f.Tiles) if(tile.Badge == "x4") tile.Badge = "+25%";
        f.Caption = t < 1.75f ? "Make a match." : t < 3 ? "Animals fall into place..." : "The falling match earns 25% extra blocks.";
        f.Status = t >= 3 ? "CASCADE: +25%" : "";
        f.Footer = "Every matched animal counts.";
    }

    private void ShapeBonus(LevelLessonFrame f, float t)
    {
        bool large = Kind == LevelLessonKind.LargeBonus;
        Grid(f,6,3);
        int[] group = large ? Row(1,6) : new[] { 1,1,2,1,1,2,2,2 };
        foreach(int index in Indices(f,group)) f.Tiles[index].Animal=ItemType.Duck;
        int x = large ? 5 : 2, y = large ? 1 : 2;
        f.Cell(x,y).Animal=ItemType.Bear;
        int sx = large ? x : 3, sy = large ? 0 : y;
        f.Cell(sx,sy).Animal=ItemType.Duck;
        Swap(f,sx,sy,x,y,Progress(t,.6f,1.2f));
        Match(f,t,1.4f,2.6f,group);
        f.Caption = t < 1.4f ? large ? "Connect six animals." : "Make a 2x2 square." : "Earn extra block credit.";
        f.Status = t >= 1.4f && t < 2.6f ? large ? "6+ GROUP: +50%" : "SQUARE: +25%" : "";
        f.Footer = "Other matches still count.";
    }

    private static void Lightning(LevelLessonFrame f, float t)
    {
        Grid(f, 7, 3);
        Animal(f, ItemType.Duck, 0, 1, 1, 1, 2, 1, 3, 1, 4, 0); Animal(f, ItemType.Bear, 4, 1);
        Swap(f, 4, 0, 4, 1, Progress(t, .65f, 1.25f));
        Match(f, t, 1.4f, 2.25f, Row(1, 5));
        f.LightningRow = 1;
        f.Lightning = Window(t, 1.95f, 2.32f) * (1 - Progress(t, 2.08f, 2.32f));
        if (t >= 2.25f) for (int x = 0; x < 7; x++) f.Cell(x, 1).IconScale = 1 - Smooth(Progress(t, 2.25f, 2.53f));
        if (t >= 2.55f) for (int y = 0; y < 3; y++) for (int x = 0; x < 7; x++) {
            if (y == 1 || t < 2.55f + Math.Abs(x - 3) * .045f) continue;
            f.Cell(x, y).Tint = LessonTint.Charged; f.Cell(x, y).Badge = "x2";
        }
        f.Caption = t < 1.3f ? "Swap to make five Ducks in a straight line." : t < 1.95f ? "Five in a row triggers lightning!" :
            t < 2.55f ? "Lightning clears the WHOLE row." : "All other tiles turn blue: double points!";
        f.Status = t >= 1.4f && t < 1.95f ? "5 IN A ROW" : t >= 2.55f ? "BLUE TILES = 2x POINTS" : "";
        f.Footer = "Charge stays with each animal until it clears. New tiles start normal.";
    }

    private static void AnimalFriends(LevelLessonFrame f, float t)
    {
        Grid(f, 4, 3, ItemType.Duck, ItemType.Chicken);
        Animal(f, ItemType.Bear, 0, 1, 1, 1, 2, 1, 3, 0); Animal(f, ItemType.Duck, 3, 1);
        Swap(f, 3, 0, 3, 1, Progress(t, .8f, 1.4f));
        Match(f, t, 1.55f, 2.5f, Row(1, 4));
        bool tied = t >= 3;
        foreach (var tile in f.Tiles) if (tile.Visible && (tile.Animal == ItemType.Bear || tied && tile.Animal == ItemType.Duck)) tile.Badge = "x2";
        f.Caption = t < 1.5f ? "Bear is your least matched animal." : t < 3 ? "Match four Bears for double points." : "Now Duck and Bear are tied. Both give double points!";
        f.Status = "Matched: Duck 8   Bear " + (tied ? "8" : "4") + "   Chicken 12";
        f.Footer = "The least matched animal gives double points. Ties share the bonus.";
    }

    private void Sizes(LevelLessonFrame f, float t)
    {
        Grid(f, 5, 3); f.GridX = -35;
        int active = Math.Min(2, (int)(t / 2f)); float local = t - active * 2f;
        for (int y = 0; y < 3; y++) {
            int count = y + 3, value = Kind == LevelLessonKind.BlockSize ? new[] { 1, 2, 4 }[y] : new[] { 2, 4, 8 }[y];
            for (int x = 0; x < 5; x++) { f.Cell(x, y).Visible = x < count; f.Cell(x, y).Animal = ItemType.Duck; }
            f.RowLabels[y] = value + (Kind == LevelLessonKind.BlockSize ? " block" + (value == 1 ? "" : "s") : " base") + " each";
        }
        Match(f, local, .35f, 1.3f, Row(active, active + 3));
        int tiles = active + 3;
        int perTile = Kind == LevelLessonKind.BlockSize ? new[] { 1, 2, 4 }[active] : new[] { 2, 4, 8 }[active];
        string unit = Kind == LevelLessonKind.BlockSize ? (perTile == 1 ? "block" : "blocks") : (perTile == 1 ? "base point" : "base points");
        f.Caption = $"Match {tiles}+: {perTile} {unit} per tile.";
        f.Status = tiles + " tiles = " + tiles * perTile + (Kind == LevelLessonKind.BlockSize ? " block credits" : " base points");
        f.Footer = "Pick ONE match size for the whole run.";
    }

    private static void ChainReaction(LevelLessonFrame f, float t)
    {
        Grid(f, 4, 4);
        Animal(f, ItemType.Duck, 0, 2, 1, 2, 0, 3, 2, 3);
        Animal(f, ItemType.Bear, 1, 3, 0, 0, 1, 0, 2, 2, 3, 2);
        Swap(f, 2, 3, 1, 3, Progress(t, .5f, 1.1f));
        Match(f, t, 1.25f, 1.75f, 0, 2, 1, 2, 0, 3, 1, 3);
        float fall = Smooth(Progress(t, 2.25f, 2.9f));
        for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) f.Cell(x, y).IconY = -2 * f.Step * fall;
        if (t >= 3) {
            float scale = 1 - Smooth(Progress(t, 4.2f, 4.48f));
            foreach (var tile in f.Tiles) if (tile.Visible && tile.Animal == ItemType.Bear) { tile.IconScale = scale * 1.1f; tile.Badge = "x4"; }
            foreach (int index in Indices(f, new[] { 0, 2, 1, 2, 2, 2, 3, 2, 2, 3 })) f.Tiles[index].Tint = LessonTint.Match;
        }
        f.Caption = t < 1.75f ? "Make a match to move the animals above it." : t < 2.25f ? "Your match clears, but scores ZERO points." :
            t < 3 ? "Falling Bears land together..." : "The falling match scores FOUR times the points!";
        f.Status = t >= 1.75f && t < 3 ? "YOUR MATCH: 0 POINTS" : t >= 3 ? "CASCADE: 4x POINTS" : "";
        f.Footer = "Set up falling matches. Every cascade scores 4x.";
    }

    private static void Garden(LevelLessonFrame f, float t)
    {
        Grid(f, 4, 3);
        Animal(f, ItemType.Duck, 0, 1, 1, 1, 0, 2, 2, 2); Animal(f, ItemType.Bear, 1, 2);
        Swap(f, 2, 2, 1, 2, Progress(t, .5f, 1.1f));
        Match(f, t, 1.25f, 1.8f, 0, 1, 1, 1, 0, 2, 1, 2);
        if (t >= 2.15f) foreach (int index in Indices(f, new[] { 0, 1, 1, 1, 0, 2, 1, 2 })) {
            var tile = f.Tiles[index]; tile.Tint = LessonTint.Planted;
            tile.Animal = ItemType.Bear; tile.IconScale = Progress(t, 2.65f, 3.1f);
            tile.IconY = 20 * (1 - Smooth(Progress(t, 2.65f, 3.1f)));
            if (t >= 3.5f) { tile.Badge = "x2"; tile.IconScale = 1.1f * (1 - Smooth(Progress(t, 4.65f, 4.95f))); }
        }
        f.Caption = t < 1.8f ? "Match a 2 by 2 square." : t < 2.65f ? "Those four spaces turn green." :
            t < 3.5f ? "New animals arrive. The green spaces stay." : "Match here again for double points!";
        f.Status = t >= 3.5f ? "GREEN SPACES = 2x POINTS" : t >= 2.15f ? "FOUR SPACES PLANTED" : "";
        f.Footer = "Green spaces stay planted for the rest of the round.";
    }

    private static void Alternating(LevelLessonFrame f, float t)
    {
        Grid(f, 5, 3);
        Animal(f, ItemType.Duck, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2);
        Animal(f, ItemType.Chicken, 3, 0);
        Animal(f, ItemType.Bear, 0, 2, 1, 2, 2, 2, 4, 2);
        Swap(f, 3, 1, 3, 0, Progress(t, .5f, 1.1f));
        Match(f, t, 1.25f, 1.8f, Row(0, 4));
        Swap(f, 4, 2, 3, 2, Progress(t, 3.15f, 3.75f));
        Match(f, t, 3.9f, 4.45f, Row(2, 4));
        bool bear = t >= 2.65f && t < 5.2f;
        foreach (var tile in f.Tiles) if (tile.Visible && tile.Animal == (bear ? ItemType.Bear : ItemType.Duck)) tile.Badge = "x3";
        f.Caption = t < 1.8f ? "Duck is NOW: match Ducks for triple points." : t < 2.65f ? "Finish the whole play, including falling matches." :
            t < 4.45f ? "Now Bear gives triple points. Match Bears!" : t < 5.2f ? "Finish this play..." : "Back to Duck. Take turns!";
        f.Status = bear ? "NOW: BEAR x3     NEXT: DUCK" : "NOW: DUCK x3     NEXT: BEAR";
        f.Footer = "The bonus switches after a successful play finishes.";
    }

    private static void Border(LevelLessonFrame f, float t)
    {
        Grid(f, 5, 4); f.ShowEdges = true;
        Animal(f, ItemType.Duck, 0, 0, 1, 0, 0, 1, 2, 1, 3, 3);
        Animal(f, ItemType.Bear, 1, 1, 3, 2, 4, 2, 4, 3, 2, 3);
        Swap(f, 2, 1, 1, 1, Progress(t, .45f, 1.05f));
        Match(f, t, 1.2f, 1.8f, 0, 0, 1, 0, 0, 1, 1, 1);
        Swap(f, 2, 3, 3, 3, Progress(t, 2.9f, 3.5f));
        Match(f, t, 3.65f, 4.4f, 3, 2, 4, 2, 3, 3, 4, 3);
        f.LitEdges = t < 1.25f || t >= 5.3f ? 0 : t < 3.75f ? 9 : 15;
        f.Caption = t < 1.25f ? "Match in a corner to touch TWO sides." : t < 2.9f ? "Top and left are lit. Find the other sides." :
            t < 3.75f ? "Match at the opposite corner..." : t < 5.3f ? "All four sides! This match scores FOUR times." : "The border resets. Light it again!";
        f.Status = t >= 3.75f && t < 5.3f ? "ALL FOUR SIDES = 4x POINTS" : t >= 1.25f && t < 3.75f ? "TOP + LEFT LIT" : "";
        f.Footer = "Corners light two sides. Complete the border for a 4x match.";
    }

    private static void Treasure(LevelLessonFrame f, float t)
    {
        int play = Math.Min(3, (int)(t / 1.9f)); float local = t - play * 1.9f;
        FourInRow(f, play % 2 == 0 ? ItemType.Duck : ItemType.Bear, ItemType.Chicken, local);
        if (play < 3) f.Cell(1, 1).Treasure = true;
        else f.Cell(0, 2).Treasure = true;
        int collected = Math.Min(3, play + (local >= 1.2f ? 1 : 0));
        f.Caption = play < 3 ? "Match the golden tile to collect treasure." : local < .9f ? "Three treasures! Your NEXT play gets the reward." : "This whole play scores FIVE times the points!";
        f.Status = play < 3 ? $"TREASURE: {collected} / 3" : "NEXT PLAY REWARD: 5x POINTS";
        if (play == 3 && local >= .9f) for (int x = 0; x < 4; x++) f.Cell(x, 1).Badge = "x5";
        f.Footer = "Every third treasure arms a 5x play. Falling matches share the reward.";
        if (play < 3) f.BoardOpacity = Progress(local, 0, .1f) * (1 - Progress(local, 1.75f, 1.9f));
    }

    private static void Tides(LevelLessonFrame f, float t)
    {
        bool turned = t >= 4.8f;
        int play = turned ? 4 : Math.Min(3, (int)(t / 1.2f));
        float local = turned ? t - 5.25f : (t - play * 1.2f) * 1.4f;
        FourInRow(f, play % 2 == 0 ? ItemType.Duck : ItemType.Bear, ItemType.Chicken, local);
        f.ShowDirection = true; f.Direction = turned ? ReplacementStrategy.West : ReplacementStrategy.South;
        int completed = turned ? 4 : Math.Min(4, play + (local >= 1.2f ? 1 : 0));
        f.Caption = !turned ? "Make four successful moves to turn the tide." : local < .9f ? "Gravity turns LEFT. Your next play gets 3x!" : "First play after the turn: TRIPLE points!";
        f.Status = turned ? "LEFT GRAVITY     3x PLAY" : $"SUCCESSFUL MOVES: {completed} / 4";
        f.Footer = "Down, left, up, right. The tide turns after all falling matches finish.";
        if (turned && local >= 1.65f) for (int x = 0; x < 4; x++) {
            var tile = f.Cell(x, 1); tile.Animal = x % 2 == 0 ? ItemType.Chicken : ItemType.Hippo;
            tile.IconScale = 1; tile.IconX = (1 - Smooth(Progress(local, 1.65f, 2.2f))) * 180;
            tile.Badge = ""; tile.Tint = LessonTint.Normal;
        }
        else if (turned && local >= .9f) for (int x = 0; x < 4; x++) f.Cell(x, 1).Badge = "x3";
        if (!turned) f.BoardOpacity = Progress(local, 0, .09f) * (1 - Progress(local, 1.48f, 1.68f));
    }

    private void MatchingLesson(LevelLessonFrame f, float t)
    {
        bool vertical = Matching == MatchType.Vertical;
        int count = Minimum;
        Grid(f, vertical ? 3 : Math.Max(4, count), vertical ? count : 3);
        var group = new int[count * 2];
        for (int i = 0; i < count; i++) {
            int x = vertical ? 1 : i, y = vertical ? i : 1;
            group[2 * i] = x; group[2 * i + 1] = y; f.Cell(x, y).Animal = i == count - 1 ? ItemType.Bear : ItemType.Duck;
        }
        int targetX = vertical ? 1 : count - 1, targetY = vertical ? count - 1 : 1;
        int sourceX = vertical ? 2 : targetX, sourceY = vertical ? targetY : 0;
        f.Cell(sourceX, sourceY).Animal = ItemType.Duck;
        Swap(f, sourceX, sourceY, targetX, targetY, Progress(t, .6f, 1.2f));
        Match(f, t, 1.4f, 2.4f, group);
        f.Caption = t < 1.4f ? $"Swap to connect {count} matching animals." : t < 2.4f ? "Matching animals clear together!" : blocks ? "Each cleared tile counts toward your block goal." : "Clear matches to earn points.";
        f.Status = t >= 1.4f && t < 2.4f ? count + " MATCHING ANIMALS" : "";
        f.Footer = Matching == MatchType.Horizontal ? "Match animals in a straight row." : vertical ? "Match animals in a straight column." : "Animals connect up, down, left and right.";
    }
}
