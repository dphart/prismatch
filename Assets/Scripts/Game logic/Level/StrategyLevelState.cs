using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct StrategyReward
{
    public readonly int Numerator, Denominator;
    public StrategyReward(int numerator,int denominator){Numerator=numerator;Denominator=Math.Max(1,denominator);}
    public static StrategyReward Identity => new(1,1);
    public int Percent => (int)Math.Min(int.MaxValue,(long)Numerator*100/Denominator);
    public int Apply(int value)=>(int)Math.Min(int.MaxValue,(long)Math.Max(0,value)*Numerator/Denominator);
}

/// <summary>State for the five strategy powers; no Unity objects or scoring hook re-evaluation.</summary>
public sealed class StrategyLevelState
{
    public BoardModel.Cell[] Rods { get; private set; } = Array.Empty<BoardModel.Cell>();
    public readonly int[] RodCharges = new int[3];
    public bool RodChainReady => Bonus==LevelBonus.LightningRods && Rods.Length==3 && RodCharges.All(n=>n>=3);
    public bool DischargeRods(BoardModel board)
    {
        if(!RodChainReady)return false;
        board.ChargeAllAnimalSquares();Array.Clear(RodCharges,0,RodCharges.Length);return true;
    }
    public readonly HashSet<BoardModel.Cell> Hidden = new();
    public readonly HashSet<BoardModel.Cell> ClearedDarkness = new();
    public int BlackoutMatches { get; private set; }
    private int boardWidth, boardHeight;
    private bool blackoutPending;
    private BoardModel visibilityBoard;
    private int spotlightIndex, spotlightChangesPending, darknessPending, darknessTileRemainder, darknessOffset;
    public int DarknessLines { get; private set; }
    public ItemType[] SpotlightAnimals => animals.Length==0?Array.Empty<ItemType>():new[]{animals[spotlightIndex%animals.Length],animals[(spotlightIndex+1)%animals.Length]}.Distinct().ToArray();
    public bool IsHidden(BoardModel.Cell cell) => Bonus==LevelBonus.AnimalSpotlight
        ? visibilityBoard!=null&&visibilityBoard[cell.X,cell.Y].HasValue&&SpotlightAnimals.Contains(visibilityBoard[cell.X,cell.Y].Value)
        : (Bonus==LevelBonus.Blackout||Bonus==LevelBonus.RollingDarkness)&&Hidden.Contains(cell);
    public LevelBonus Bonus { get; }
    public bool BossActive { get; set; }
    public readonly HashSet<BoardModel.Cell> Boosted = new(), Glowing = new(), Caged = new();
    public readonly HashSet<BoardModel.Cell> Golden = new();
    public readonly int[] VaultHits = new int[4]; // top, right, bottom, left
    private ItemType[] animals = Array.Empty<ItemType>();
    private readonly Random cageRandom = new();
    private int markedIndex, pendingMoves;
    private bool vaultPending, rescuedThisPlay;
    private readonly HashSet<ItemType> rescued = new();
    public ItemType Marked => animals.Length==0 ? ItemType.Duck : animals[markedIndex%animals.Length];
    public BoardModel.Cell Vault { get; private set; }
    public BoardModel.Cell ReleaseCage { get; private set; }
    public const int CageMatchesRequired = 10;
    public int CageMatches { get; private set; }
    public bool CageCapturePending { get; private set; }
    public ItemType? PendingCageAnimal { get; private set; }
    public readonly HashSet<ItemType> CapturedAnimals = new();
    public bool CagePoolExhausted => Bonus==LevelBonus.WildRelease && animals.Count(a=>!CapturedAnimals.Contains(a))<=3;
    public bool CageOpen => Bonus==LevelBonus.WildRelease && !CagePoolExhausted && CageMatches>=CageMatchesRequired && !CageCapturePending;
    public void CancelCageCapture() { CageCapturePending=false;PendingCageAnimal=null; }
    public int CrossMatches { get; private set; }
    public bool CrossPending => CrossMatches>=3;
    public bool CrossWarning => CrossMatches>=2;
    public bool CrossCascades { get; private set; }
    public ReplacementStrategy Direction { get; private set; } = ReplacementStrategy.South;
    public bool PowerTriggered { get; private set; }
    public bool BossTriggered { get; private set; }
    public static bool Supports(LevelBonus bonus) => bonus is LevelBonus.MigrationSeason or LevelBonus.CrackTheVault or LevelBonus.SteppingStones or LevelBonus.AnimalRescue or LevelBonus.Crosswinds or LevelBonus.GoldenRescue or LevelBonus.WildRelease or LevelBonus.Blackout or LevelBonus.AnimalSpotlight or LevelBonus.RollingDarkness or LevelBonus.LightningRods;
    public StrategyLevelState(LevelBonus bonus) { Bonus=bonus; }
    public void Initialize(BoardModel board,IEnumerable<ItemType> pool)
    {
        Array.Clear(RodCharges,0,RodCharges.Length);
        Rods=Bonus==LevelBonus.LightningRods?new[]{new BoardModel.Cell(1,1),new BoardModel.Cell(board.Width-2,1),new BoardModel.Cell(board.Width/2,board.Height-2)}:Array.Empty<BoardModel.Cell>();
        visibilityBoard=board;spotlightIndex=spotlightChangesPending=darknessPending=DarknessLines=0;
        boardWidth=board.Width;boardHeight=board.Height;Hidden.Clear();ClearedDarkness.Clear();darknessTileRemainder=darknessOffset=0;BlackoutMatches=0;blackoutPending=false;
        animals=pool.Distinct().ToArray();markedIndex=0;pendingMoves=0;vaultPending=rescuedThisPlay=false;
        Golden.Clear();Boosted.Clear();Glowing.Clear();Caged.Clear();rescued.Clear();Array.Clear(VaultHits,0,4);
        CrossMatches=0;CrossCascades=false;Direction=ReplacementStrategy.South;
        Vault=new BoardModel.Cell(board.Width/2,board.Height/2);
        ReleaseCage=Vault;CageMatches=0;CageCapturePending=false;PendingCageAnimal=null;CapturedAnimals.Clear();
        if(Bonus==LevelBonus.SteppingStones)
            foreach(var c in new[]{new BoardModel.Cell(2,3),new BoardModel.Cell(3,3),new BoardModel.Cell(4,3),new BoardModel.Cell(3,4)})
                if(c.X<board.Width&&c.Y<board.Height)Glowing.Add(c);
    }
    public bool IsProtected(BoardModel.Cell cell) => Bonus==LevelBonus.LightningRods&&Rods.Contains(cell)|| Bonus==LevelBonus.CrackTheVault&&cell.Equals(Vault)||
        Bonus==LevelBonus.WildRelease&&cell.Equals(ReleaseCage);
    public bool IsBlocked(BoardModel.Cell cell) => IsProtected(cell)||Caged.Contains(cell);
    public bool CanFeedCage(BoardModel board,BoardModel.Cell source,BoardModel.Cell destination) => CageOpen &&
        destination.Equals(ReleaseCage) && source.X>=0 && source.Y>=0 && source.X<board.Width && source.Y<board.Height &&
        !IsBlocked(source) && board[source.X,source.Y].HasValue && animals.Contains(board[source.X,source.Y].Value) && !CapturedAnimals.Contains(board[source.X,source.Y].Value);
    public List<BoardModel.Cell> CaptureCageAnimal(BoardModel board,BoardModel.Cell source,BoardModel.Cell destination)
    {
        if(!CanFeedCage(board,source,destination))return new();
        ItemType animal=board[source.X,source.Y].Value;
        var captured=Enumerable.Range(0,board.Width*board.Height).Select(n=>new BoardModel.Cell(n%board.Width,n/board.Width))
            .Where(c=>!IsBlocked(c)&&board[c.X,c.Y]==animal).ToList();
        PendingCageAnimal=animal;CageCapturePending=true;
        return captured;
    }
    public bool IsCross(BoardModel.Cell cell,BoardModel board) => cell.X==board.Width/2||cell.Y==board.Height/2;
    public List<StrategyReward> PrepareWave(BoardModel board,List<List<BoardModel.Cell>> groups,ISet<BoardModel.Cell> swapped)
    {
        PowerTriggered=BossTriggered=false;
        var initial=groups.Where(g=>swapped!=null&&g.Any(swapped.Contains)).ToList();
        var all=new HashSet<BoardModel.Cell>(groups.SelectMany(g=>g));
        if(Bonus==LevelBonus.WildRelease&&!CagePoolExhausted&&!CageOpen&&!CageCapturePending)
            foreach(var group in groups)
                if(CageMatches<CageMatchesRequired&&group.Any(c=>Math.Abs(c.X-ReleaseCage.X)+Math.Abs(c.Y-ReleaseCage.Y)==1))
                { CageMatches++;PowerTriggered=true; }
        if(Bonus==LevelBonus.LightningRods&&!RodChainReady) {
            for(int i=0;i<Rods.Length;i++) {
                int before=RodCharges[i];
                RodCharges[i]=Math.Min(3,before+all.Count(c=>!IsBlocked(c)&&Math.Abs(c.X-Rods[i].X)+Math.Abs(c.Y-Rods[i].Y)==1));
                if(RodCharges[i]>before)PowerTriggered=true;
            }
        }
        bool breakVault=BossActive&&Bonus==LevelBonus.CrackTheVault&&Boosted.Count>0&&initial.Any(g=>g.Any(c=>!Boosted.Contains(c)));
        if(breakVault){Boosted.Clear();BossTriggered=true;}
        var percentages=groups.Select(group=>Reward(board,group)).ToList();
        for(int i=0;i<groups.Count;i++)if(percentages[i].Percent>100)PowerTriggered=true;
        if(Bonus==LevelBonus.MigrationSeason&&BossActive&&groups.Any(g=>g.Any(c=>board[c.X,c.Y]!=Marked)))BossTriggered=true;
        if(Bonus==LevelBonus.CrackTheVault&&initial.Count>0) {
            var matched=new HashSet<BoardModel.Cell>(initial.SelectMany(g=>g));
            var sides=new[]{new BoardModel.Cell(Vault.X,Vault.Y-1),new BoardModel.Cell(Vault.X+1,Vault.Y),new BoardModel.Cell(Vault.X,Vault.Y+1),new BoardModel.Cell(Vault.X-1,Vault.Y)};
            for(int side=0;side<4;side++)if(matched.Contains(sides[side])&&VaultHits[side]<2){VaultHits[side]++;PowerTriggered=true;}
            vaultPending=VaultHits.All(h=>h==2);
        }
        if(Bonus==LevelBonus.SteppingStones)foreach(var group in initial) {
            bool miss=!group.Any(Glowing.Contains);
            if(BossActive&&miss) { if(Glowing.Count>0)Glowing.Remove(Glowing.OrderBy(c=>c.Y).ThenBy(c=>c.X).First());BossTriggered=true; }
            else { foreach(var c in group)Glowing.Add(c);PowerTriggered=true; }
        }
        if(Bonus==LevelBonus.AnimalRescue&&initial.Count>0) {
            foreach(var group in initial) {
                bool target=group.Any(c=>board[c.X,c.Y]==Marked);
                if(BossActive&&!target) {
                    var candidate=Enumerable.Range(0,board.Width*board.Height).Select(n=>new BoardModel.Cell(n%board.Width,n/board.Width))
                        .Where(c=>board[c.X,c.Y]==Marked&&!Caged.Contains(c)&&!all.Contains(c)).Select(c=>(BoardModel.Cell?)c).FirstOrDefault();
                    if(candidate.HasValue){Caged.Add(candidate.Value);BossTriggered=true;}
                }
                // Adjacent matches release cages; rescuing the target also releases one.
                var freed=Caged.Where(c=>group.Any(m=>Math.Abs(c.X-m.X)+Math.Abs(c.Y-m.Y)==1)).ToList();
                foreach(var c in freed){Caged.Remove(c);PowerTriggered=true;}
                if(target) {
                    if(Caged.Count>0)Caged.Remove(Caged.OrderBy(c=>c.Y).ThenBy(c=>c.X).First());
                    if(!rescuedThisPlay){rescued.Add(Marked);rescuedThisPlay=true;}
                    if(rescued.Count>=3){pendingMoves++;rescued.Clear();}
                    PowerTriggered=true;
                }
            }
        }
        if(Bonus==LevelBonus.GoldenRescue) {
            // Resolve only pre-existing cages, then create cages away from this wave's clears.
            foreach(var cage in Caged.Where(c=>all.Any(m=>Math.Abs(c.X-m.X)+Math.Abs(c.Y-m.Y)==1)).ToList()) {
                Caged.Remove(cage);Golden.Add(cage);PowerTriggered=true;
            }
            int count=groups.Sum(g=>g.Count/(BossActive?1:2));
            var candidates=Enumerable.Range(0,board.Width*board.Height)
                .Select(n=>new BoardModel.Cell(n%board.Width,n/board.Width))
                .Where(c=>board[c.X,c.Y].HasValue&&!all.Contains(c)&&!Caged.Contains(c)&&!Golden.Contains(c)).ToList();
            // Shuffle eligible tiles to avoid repeatedly caging the same edge.
            for(int i=candidates.Count-1;i>0;i--) {
                int j=cageRandom.Next(i+1);var temp=candidates[i];candidates[i]=candidates[j];candidates[j]=temp;
            }
            foreach(var c in candidates.Take(count))Caged.Add(c);
            if(count>0&&candidates.Count>0){if(BossActive)BossTriggered=true;else PowerTriggered=true;}
        }
        if(Bonus==LevelBonus.Blackout) {
            BlackoutMatches+=initial.Count;
            if(BlackoutMatches>=5){BlackoutMatches%=5;blackoutPending=true;}
        }
        if(Bonus==LevelBonus.AnimalSpotlight) {
            BlackoutMatches+=initial.Count;
            spotlightChangesPending+=BlackoutMatches/5;BlackoutMatches%=5;
        }
        if(Bonus==LevelBonus.RollingDarkness) {
            int tiles=darknessTileRemainder+initial.SelectMany(g=>g).Distinct().Count();
            darknessPending+=tiles/2;darknessTileRemainder=tiles%2;
        }
        if(Bonus==LevelBonus.Crosswinds)CrossMatches+=initial.Count;
        return percentages;
    }
    public int Percent(BoardModel board,List<BoardModel.Cell> group)=>Reward(board,group).Percent;
    public StrategyReward Reward(BoardModel board,List<BoardModel.Cell> group)
    {
        if(group.Count==0)return StrategyReward.Identity;
        if(Bonus==LevelBonus.GoldenRescue)return new(group.Count+2*group.Count(Golden.Contains),group.Count);
        if(Bonus==LevelBonus.MigrationSeason)return new(group.Sum(c=>board[c.X,c.Y]==Marked?200:BossActive?50:100),100*group.Count);
        if(Bonus==LevelBonus.CrackTheVault)return new(group.Count+group.Count(Boosted.Contains),group.Count);
        if(Bonus==LevelBonus.SteppingStones)return new(group.Count(Glowing.Contains)>=2?3:1,1);
        if(Bonus==LevelBonus.AnimalRescue)return new(group.Count+group.Count(c=>board[c.X,c.Y]==Marked),group.Count);
        if(Bonus==LevelBonus.Crosswinds&&CrossCascades)return new(5,1);
        return StrategyReward.Identity;
    }
    public void SetAnimals(IEnumerable<ItemType> pool)
    {
        var current=Marked;animals=pool.Distinct().ToArray();
        markedIndex=Math.Max(0,Array.IndexOf(animals,current));
    }
    public bool AfterClear(BoardModel board,IEnumerable<BoardModel.Cell> cleared)
    {
        var holes=new HashSet<BoardModel.Cell>(cleared);Boosted.ExceptWith(holes);Caged.ExceptWith(holes);
        if(Bonus==LevelBonus.WildRelease&&CageCapturePending) {
            if(PendingCageAnimal.HasValue)CapturedAnimals.Add(PendingCageAnimal.Value);
            PendingCageAnimal=null;
            var places=Enumerable.Range(0,board.Width*board.Height).Select(n=>new BoardModel.Cell(n%board.Width,n/board.Width))
                .Where(c=>!c.Equals(ReleaseCage)).ToList();
            var available=places.Where(c=>!holes.Contains(c)).ToList();
            if(available.Count==0)available=places;
            ReleaseCage=available[cageRandom.Next(available.Count)];CageMatches=0;CageCapturePending=false;
            return true;
        }
        if(!vaultPending)return false;
        vaultPending=false;Array.Clear(VaultHits,0,4);
        for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++) {
            var c=new BoardModel.Cell(x,y);if(!holes.Contains(c)&&!c.Equals(Vault))Boosted.Add(c);
        }
        return true;
    }
    public void SwapTiles(BoardModel.Cell a,BoardModel.Cell b)
    {
        if(Bonus!=LevelBonus.Blackout) {
            bool hiddenA=Hidden.Remove(a),hiddenB=Hidden.Remove(b);
            if(hiddenA)Hidden.Add(b);if(hiddenB)Hidden.Add(a);
        }
        bool first=Golden.Remove(a),second=Golden.Remove(b);
        if(first)Golden.Add(b);if(second)Golden.Add(a);
    }
    public void RefillTiles(IEnumerable<BoardModel.Cell> cleared,IEnumerable<BoardModel.RefillMove> moves)
    {
        if(Bonus==LevelBonus.Blackout) {
            // Protection belongs to board squares, not the animals occupying them.
            foreach(var cell in cleared)RevealBlackout(cell);
            foreach(var move in moves.Where(m=>m.IsNew)) {
                RevealBlackout(move.To);
                if(!BossActive)foreach(var neighbor in new[]{new BoardModel.Cell(move.To.X-1,move.To.Y),new BoardModel.Cell(move.To.X+1,move.To.Y),new BoardModel.Cell(move.To.X,move.To.Y-1),new BoardModel.Cell(move.To.X,move.To.Y+1)})RevealBlackout(neighbor);
            }
        }
        if(Bonus==LevelBonus.RollingDarkness) {
            var refill=moves.ToList();
            var oldHidden=new HashSet<BoardModel.Cell>(Hidden);oldHidden.ExceptWith(cleared);
            Hidden.ExceptWith(cleared);
            foreach(var move in refill)if(!move.IsNew)Hidden.Remove(move.From);
            foreach(var move in refill) {
                Hidden.Remove(move.To);
                if(!move.IsNew&&oldHidden.Contains(move.From))Hidden.Add(move.To);
            }
            foreach(var move in refill.Where(m=>m.IsNew)) {
                Hidden.Remove(move.To);
                if(!BossActive)foreach(var neighbor in new[]{new BoardModel.Cell(move.To.X-1,move.To.Y),new BoardModel.Cell(move.To.X+1,move.To.Y),new BoardModel.Cell(move.To.X,move.To.Y-1),new BoardModel.Cell(move.To.X,move.To.Y+1)})Hidden.Remove(neighbor);
            }
        }
        if(Bonus==LevelBonus.GoldenRescue) {
            var cages=new HashSet<BoardModel.Cell>(Caged);cages.ExceptWith(cleared);Caged.Clear();
            var sources=new HashSet<BoardModel.Cell>();
            foreach(var move in moves)if(!move.IsNew) {
                sources.Add(move.From);if(cages.Contains(move.From))Caged.Add(move.To);
            }
            cages.ExceptWith(sources);Caged.UnionWith(cages);
        }
        var old=new HashSet<BoardModel.Cell>(Golden);old.ExceptWith(cleared);Golden.Clear();
        var moved=new HashSet<BoardModel.Cell>();
        foreach(var move in moves) {
            if(!move.IsNew){moved.Add(move.From);if(old.Contains(move.From))Golden.Add(move.To);}
        }
        old.ExceptWith(moved);Golden.UnionWith(old);
    }
    private void RevealBlackout(BoardModel.Cell cell)
    {
        if(Hidden.Remove(cell))ClearedDarkness.Add(cell);
    }
    public List<BoardModel.Cell> RotateCross(BoardModel board,Random random)
    {
        if(Bonus!=LevelBonus.Crosswinds||!CrossPending)return new();
        var directions=new[]{ReplacementStrategy.South,ReplacementStrategy.West,ReplacementStrategy.North,ReplacementStrategy.East};
        int current=Array.IndexOf(directions,Direction);
        Direction=BossActive?directions[(current+1+random.Next(3))%4]:directions[(current+1)%4];
        CrossMatches-=3;CrossCascades=true;
        return Enumerable.Range(0,board.Width*board.Height).Select(n=>new BoardModel.Cell(n%board.Width,n/board.Width)).Where(c=>IsCross(c,board)).ToList();
    }
    public int TakeMoves(){int moves=pendingMoves;pendingMoves=0;return moves;}
    public void FinishPlay()
    {
        if(Bonus==LevelBonus.MigrationSeason||Bonus==LevelBonus.AnimalRescue&&rescuedThisPlay)markedIndex++;
        if(Bonus==LevelBonus.Blackout&&blackoutPending) {
            Hidden.Clear();
            for(int y=0;y<boardHeight;y++)for(int x=0;x<boardWidth;x++)if(!ClearedDarkness.Contains(new BoardModel.Cell(x,y)))Hidden.Add(new BoardModel.Cell(x,y));
            blackoutPending=false;
        }
        if(Bonus==LevelBonus.AnimalSpotlight&&animals.Length>0) {
            spotlightIndex=(spotlightIndex+2*spotlightChangesPending)%animals.Length;spotlightChangesPending=0;
        }
        if(Bonus==LevelBonus.RollingDarkness) {
            while(darknessPending>0&&Hidden.Count<boardWidth*boardHeight) {
                bool row=DarknessLines%2==0;
                int line=(DarknessLines/2)%(row?boardHeight:boardWidth);
                var cell=row?new BoardModel.Cell(darknessOffset,line):new BoardModel.Cell(line,darknessOffset);
                if(Hidden.Add(cell))darknessPending--;
                if(++darknessOffset>=(row?boardWidth:boardHeight)){darknessOffset=0;DarknessLines++;}
            }
            darknessPending=0;
        }
        rescuedThisPlay=false;CrossCascades=false;
    }
    public string Status => Bonus switch {
        LevelBonus.LightningRods => RodChainReady?"Rods ready":"Rods  "+string.Join("  ·  ",RodCharges.Select(n=>n+"/3")),
        LevelBonus.AnimalSpotlight => "Dark: "+string.Join(" + ",SpotlightAnimals.Select(a=>a.ToString()))+"  ·  "+(5-BlackoutMatches)+" to switch",
        LevelBonus.RollingDarkness => "Tiles "+darknessTileRemainder+"/2 · Next "+(DarknessLines%2==0?"ROW "+(DarknessLines/2%Math.Max(1,boardHeight)+1):"COLUMN "+(DarknessLines/2%Math.Max(1,boardWidth)+1)),
        LevelBonus.Blackout => "Blackout in "+(5-BlackoutMatches)+"  ·  "+ClearedDarkness.Count+" safe",
        LevelBonus.WildRelease => CagePoolExhausted?"Cage resting":CageCapturePending?"Releasing…":CageOpen?"Cage ready":"Cage "+CageMatches+"/10",
        LevelBonus.GoldenRescue => Caged.Count+" caged  ·  "+Golden.Count+" gold",
        LevelBonus.MigrationSeason => "Target: "+Marked,
        LevelBonus.CrackTheVault => "Vault  "+string.Join("  ·  ",VaultHits.Select((h,i)=>new[]{"T ","R ","B ","L "}[i]+h+"/2")),
        LevelBonus.SteppingStones => Glowing.Count+" glowing spaces",
        LevelBonus.AnimalRescue => Marked+"  ·  "+rescued.Count+"/3 rescued",
        LevelBonus.Crosswinds => Direction+"  ·  Cross in "+Math.Max(0,3-CrossMatches),
        _ => "" };
}
