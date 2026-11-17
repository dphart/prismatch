using System;
using System.Collections.Generic;
using System.Linq;

public enum ReplacementStrategy { InPlace, South, North, West, East }

/// <summary>Board rules without scene objects, sprites, or animation.</summary>
public sealed class BoardModel
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => X * 397 ^ Y;
    }

    public readonly CampaignMechanicState Campaign = new();
    public StrategyLevelState Strategy { get; set; }
    public bool IsBlocked(Cell cell) => Strategy?.IsBlocked(cell)==true;
    public Cell? Treasure { get; private set; }
    public void EnsureTreasure(Random random)
    {
        if (Treasure.HasValue && this[Treasure.Value.X,Treasure.Value.Y].HasValue) return;
        var candidates = new List<Cell>();
        for (int y=0;y<Height;y++) for(int x=0;x<Width;x++) if(items[x,y].HasValue) candidates.Add(new Cell(x,y));
        Treasure = candidates.Count == 0 ? (Cell?)null : candidates[random.Next(candidates.Count)];
    }
    public bool CollectTreasure(IEnumerable<Cell> clears)
    {
        if (!Treasure.HasValue || !clears.Contains(Treasure.Value)) return false;
        Treasure = null; Campaign.CollectTreasure(); return true;
    }
    private readonly ItemType?[,] items;
    private readonly bool[,] charged;
    private readonly bool[,] planted;
    public bool IsPlanted(int x, int y) => planted[x,y];
    public bool SecondSpotlight { get; private set; }
    public ItemType SpotlightAnimal => SecondSpotlight ? ItemType.Bear : ItemType.Duck;
    public ItemType NextSpotlightAnimal => SecondSpotlight ? ItemType.Duck : ItemType.Bear;
    public void FinishSpotlightPlay(bool successful) { if (successful) SecondSpotlight = !SecondSpotlight; }
    public int PlantSquares(IEnumerable<List<Cell>> groups)
    {
        int added = 0;
        foreach (var group in groups)
        {
            var set = new HashSet<Cell>(group);
            foreach (var c in group)
                if (set.Contains(new Cell(c.X+1,c.Y)) && set.Contains(new Cell(c.X,c.Y+1)) && set.Contains(new Cell(c.X+1,c.Y+1)))
                    foreach (var cell in new[] { c,new Cell(c.X+1,c.Y),new Cell(c.X,c.Y+1),new Cell(c.X+1,c.Y+1) })
                        if (!planted[cell.X,cell.Y]) { planted[cell.X,cell.Y] = true; added++; }
        }
        return added;
    }
    // Charge belongs to board spaces and lasts until those spaces clear.
    public bool IsCharged(int x, int y) => charged[x,y];
    private List<MatchRule> matchRules;
    private readonly Dictionary<ItemType, ItemType> matchAliases = new();
    public void SetAnimalPairs(IEnumerable<AnimalPairPower> pairs)
    {
        matchAliases.Clear();
        foreach (var pair in pairs)
        {
            if (matchAliases.ContainsKey(pair.First) || matchAliases.ContainsKey(pair.Second))
                throw new ArgumentException("Animal pairs cannot overlap.");
            matchAliases[pair.First] = pair.First;
            matchAliases[pair.Second] = pair.First;
        }
    }
    private bool SameMatchType(ItemType? a, ItemType? b)
    {
        if (!a.HasValue || !b.HasValue) return false;
        var first = matchAliases.TryGetValue(a.Value, out var aliasA) ? aliasA : a.Value;
        var second = matchAliases.TryGetValue(b.Value, out var aliasB) ? aliasB : b.Value;
        return first == second;
    }
    public int Width => items.GetLength(0);
    public int Height => items.GetLength(1);
    public ItemType? this[int x, int y] { get => items[x, y]; set { if (!value.HasValue) charged[x,y] = false; items[x,y] = value; } }
    public BoardModel(int width, int height, IEnumerable<MatchRule> rules = null)
    {
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
        items = new ItemType?[width, height];
        charged = new bool[width,height];
        planted = new bool[width,height];
        matchRules = MatchRule.Normalize(rules);
    }

    public void SetMatchRules(IEnumerable<MatchRule> rules) =>
        matchRules = (rules ?? Enumerable.Empty<MatchRule>()).Where(rule => rule != null)
            .Select(rule => rule.Clone()).ToList();

    public List<Cell> Connected(int x, int y)
    {
        var result = new List<Cell>();
        if (x < 0 || y < 0 || x >= Width || y >= Height || !items[x, y].HasValue) return result;
        var type = items[x, y];
        var visited = new HashSet<Cell>();
        var pending = new Stack<Cell>();
        pending.Push(new Cell(x, y));
        while (pending.Count > 0)
        {
            var cell = pending.Pop();
            if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height ||
                IsBlocked(cell) || !SameMatchType(items[cell.X, cell.Y], type) || !visited.Add(cell)) continue;
            result.Add(cell);
            pending.Push(new Cell(cell.X - 1, cell.Y));
            pending.Push(new Cell(cell.X + 1, cell.Y));
            pending.Push(new Cell(cell.X, cell.Y - 1));
            pending.Push(new Cell(cell.X, cell.Y + 1));
        }
        return result;
    }

    public List<List<Cell>> Matches()
    {
        var candidates = new List<List<Cell>>();
        foreach (var rule in matchRules)
        {
            int minimum = Math.Max(2, rule.minimumLength);
            if (rule.type == MatchType.Snake) FindSnakeMatches(minimum, candidates);
            else if (rule.type == MatchType.Horizontal) FindLineMatches(true, minimum, candidates);
            else if (rule.type == MatchType.Vertical) FindLineMatches(false, minimum, candidates);
        }
        return MergeOverlapping(candidates);
    }

    public List<(MatchType Direction, int Index)> LightningLines(IEnumerable<List<Cell>> groups)
    {
        var matched = new HashSet<Cell>(groups.SelectMany(group => group));
        var lines = new List<(MatchType Direction, int Index)>();
        foreach (bool horizontal in new[] { true, false })
        {
            int outer = horizontal ? Height : Width, inner = horizontal ? Width : Height;
            for (int line = 0; line < outer; line++)
            {
                int run = 0;
                ItemType? previous = null;
                for (int position = 0; position < inner; position++)
                {
                    var cell = horizontal ? new Cell(position, line) : new Cell(line, position);
                    var animal = this[cell.X, cell.Y];
                    run = matched.Contains(cell) && animal.HasValue ? SameMatchType(previous, animal) ? run + 1 : 1 : 0;
                    previous = run > 0 ? animal : null;
                    if (run < 5) continue;
                    lines.Add((horizontal ? MatchType.Horizontal : MatchType.Vertical, line));
                    break;
                }
            }
        }
        return lines;
    }

    public List<Cell> ExpandLightning(IEnumerable<Cell> clears, IEnumerable<(MatchType Direction, int Index)> lines)
    {
        var result = new HashSet<Cell>(clears);
        foreach (var line in lines)
        for (int position = 0; position < (line.Direction == MatchType.Horizontal ? Width : Height); position++)
        {
            var cell = line.Direction == MatchType.Horizontal ? new Cell(position, line.Index) : new Cell(line.Index, position);
            if (this[cell.X, cell.Y].HasValue) result.Add(cell);
        }
        return result.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToList();
    }

    public void ChargeAllAnimalSquares()
    {
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            if(items[x,y].HasValue&&!IsBlocked(new Cell(x,y)))charged[x,y]=true;
    }

    public void ChargeLightning(IEnumerable<(MatchType Direction, int Index)> lines)
    {
        var struck = new HashSet<Cell>(ExpandLightning(new Cell[0],lines));
        if (struck.Count == 0) return;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            if (items[x,y].HasValue && !struck.Contains(new Cell(x,y))) charged[x,y] = true;
    }

    private void FindSnakeMatches(int minimum, List<List<Cell>> matches)
    {
        var visited = new HashSet<Cell>();
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            var cell = new Cell(x, y);
            if (visited.Contains(cell) || !items[x, y].HasValue) continue;
            var group = Connected(x, y);
            foreach (var member in group) visited.Add(member);
            if (group.Count >= minimum) matches.Add(group);
        }
    }

    private void FindLineMatches(bool horizontal, int minimum, List<List<Cell>> matches)
    {
        int outer = horizontal ? Height : Width;
        int inner = horizontal ? Width : Height;
        for (int line = 0; line < outer; line++)
        {
            int start = 0;
            while (start < inner)
            {
                int x = horizontal ? start : line;
                int y = horizontal ? line : start;
                ItemType? type = items[x, y];
                if (!type.HasValue || IsBlocked(new Cell(x,y))) { start++; continue; }
                int end = start + 1;
                while (end < inner)
                {
                    int nextX = horizontal ? end : line;
                    int nextY = horizontal ? line : end;
                    if (IsBlocked(new Cell(nextX,nextY)) || !SameMatchType(items[nextX, nextY], type)) break;
                    end++;
                }
                if (end - start >= minimum)
                {
                    var group = new List<Cell>();
                    for (int position = start; position < end; position++)
                        group.Add(horizontal ? new Cell(position, line) : new Cell(line, position));
                    matches.Add(group);
                }
                start = end;
            }
        }
    }

    private List<List<Cell>> MergeOverlapping(List<List<Cell>> candidates)
    {
        var merged = new List<HashSet<Cell>>();
        foreach (var candidate in candidates)
        {
            var set = new HashSet<Cell>(candidate);
            var overlaps = merged.Where(existing => existing.Overlaps(set)).ToList();
            foreach (var overlap in overlaps)
            {
                set.UnionWith(overlap);
                merged.Remove(overlap);
            }
            merged.Add(set);
        }
        return merged.Select(group => group.ToList()).ToList();
    }

    public void Swap(Cell a, Cell b)
    {
        if (Treasure.HasValue) {
            if (Treasure.Value.Equals(a)) Treasure=b;
            else if (Treasure.Value.Equals(b)) Treasure=a;
        }
        Strategy?.SwapTiles(a,b);
        var temp = items[a.X, a.Y];
        items[a.X, a.Y] = items[b.X, b.Y];
        items[b.X, b.Y] = temp;
    }

    public bool HasLegalMove()
    {
        for (int a = 0; a < Width * Height; a++)
        for (int b = a + 1; b < Width * Height; b++)
        {
            var first = new Cell(a % Width, a / Width);
            var second = new Cell(b % Width, b / Width);
            if (IsBlocked(first)||IsBlocked(second)||items[first.X, first.Y] == items[second.X, second.Y]) continue;
            Swap(first, second);
            bool valid = Matches().Any(group => group.Contains(first) || group.Contains(second));
            Swap(first, second);
            if (valid) return true;
        }
        return false;
    }

    public readonly struct RefillMove
    {
        public readonly Cell From, To;
        public readonly ItemType Type;
        public readonly bool IsNew;
        public RefillMove(Cell from, Cell to, ItemType type, bool isNew)
        { From = from; To = to; Type = type; IsNew = isNew; }
    }

    /// <summary>Clear one wave, compact toward gravity, then fill from the opposite edge.
    /// Coordinates start at the top left; spawn sources may be outside the board.</summary>
    public List<RefillMove> Refill(IEnumerable<Cell> cleared, ReplacementStrategy strategy, Func<ItemType> nextItem)
    {
        if (cleared == null) throw new ArgumentNullException(nameof(cleared));
        if (nextItem == null) throw new ArgumentNullException(nameof(nextItem));
        if (!Enum.IsDefined(typeof(ReplacementStrategy), strategy)) throw new ArgumentOutOfRangeException(nameof(strategy));
        var holes = new HashSet<Cell>(cleared);
        foreach (var cell in holes)
            if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height)
                throw new ArgumentOutOfRangeException(nameof(cleared));
        var oldTreasure=Treasure;
        if (Treasure.HasValue && holes.Contains(Treasure.Value)) Treasure=null;
        var moves = new List<RefillMove>();
        if (holes.Count == 0) return moves;
        foreach (var cell in holes) { items[cell.X, cell.Y] = null; charged[cell.X,cell.Y] = false; }
        if (strategy == ReplacementStrategy.InPlace)
        {
            foreach (var cell in holes.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                var type = nextItem(); items[cell.X, cell.Y] = type;
                charged[cell.X,cell.Y] = false;
                moves.Add(new RefillMove(cell, cell, type, true));
            }
            Strategy?.RefillTiles(holes,moves);
            return moves;
        }
        bool vertical = strategy == ReplacementStrategy.North || strategy == ReplacementStrategy.South;
        int lines = vertical ? Width : Height;
        int length = vertical ? Height : Width;
        Cell Position(int line, int index) => strategy switch
        {
            ReplacementStrategy.South => new Cell(line, Height - 1 - index),
            ReplacementStrategy.North => new Cell(line, index),
            ReplacementStrategy.East => new Cell(Width - 1 - index, line),
            _ => new Cell(index, line)
        };
        for (int line = 0; line < lines; line++)
        {
            int write = 0;
            for (int read = 0; read < length; read++)
            {
                var source = Position(line, read);
                var type = items[source.X, source.Y];
                if (!type.HasValue) continue;
                var destination = Position(line, write++);
                if (oldTreasure.HasValue && source.Equals(oldTreasure.Value) && !holes.Contains(source)) Treasure=destination;
                items[destination.X, destination.Y] = type;
                if (!source.Equals(destination))
                    moves.Add(new RefillMove(source, destination, type.Value, false));
            }
            int spawn = 0;
            while (write < length)
            {
                var destination = Position(line, write++);
                var source = Position(line, length + spawn++);
                var type = nextItem(); items[destination.X, destination.Y] = type;
                moves.Add(new RefillMove(source, destination, type, true));
            }
        }
        Strategy?.RefillTiles(holes,moves);
        return moves;
    }

    public static double CascadeMatchWeight(int completedCascades) =>
        Math.Pow(.5, Math.Max(0, completedCascades));

    // Reweight only newly spawned animals. Existing falling tiles are never changed.
    // A match-making candidate has half the weight for each completed cascade.
    public void ApplyCascadeDamping(List<RefillMove> moves, IReadOnlyList<ItemType> types,
        int completedCascades, Random random, Func<ItemType,int> spawnWeight = null)
    {
        if (completedCascades <= 0) return; // First cascade uses the normal random refill.
        var palette = types.Distinct().ToArray();
        if (palette.Length == 0) throw new ArgumentException("Refill needs animal types.", nameof(types));
        double matchWeight = CascadeMatchWeight(completedCascades);
        var weights = new double[palette.Length];
        for (int index = 0; index < moves.Count; index++)
        {
            var move = moves[index];
            if (!move.IsNew) continue;
            double total = 0;
            for (int type = 0; type < palette.Length; type++)
            {
                items[move.To.X, move.To.Y] = palette[type];
                bool createsMatch = Matches().Any(group => group.Contains(move.To));
                weights[type] = (createsMatch ? matchWeight : 1d) * Math.Max(1,spawnWeight?.Invoke(palette[type]) ?? 1);
                total += weights[type];
            }
            int selected = palette.Length - 1;
            if (total > 0)
            {
                double roll = random.NextDouble() * total;
                for (int type = 0; type < palette.Length; type++)
                {
                    roll -= weights[type];
                    if (roll < 0) { selected = type; break; }
                }
            }
            else selected = random.Next(palette.Length);
            items[move.To.X, move.To.Y] = palette[selected];
            moves[index] = new RefillMove(move.From, move.To, palette[selected], true);
        }
    }

    public void Generate(IReadOnlyList<ItemType> types, Random random, Func<ItemType,int> spawnWeight = null,
        bool lightningOpening = false)
    {
        Treasure=null;
        Array.Clear(charged,0,charged.Length);
        if (types == null || new HashSet<ItemType>(types).Count < 3)
            throw new ArgumentException("Board generation needs at least three different tile types.", nameof(types));
        if (Width * Height < 4) throw new InvalidOperationException("Board is too small for a legal swap.");
        for (int attempt = 0; attempt < 100; attempt++)
        {
            Array.Clear(items, 0, items.Length);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (spawnWeight != null)
                {
                    var candidates = types.Distinct().ToList();
                    bool found = false;
                    while (candidates.Count > 0) {
                        var selected = AnimalSpawnPower.Select(candidates,random.NextDouble(),spawnWeight);
                        items[x,y] = selected;
                        if (Matches().Count == 0) { found = true; break; }
                        candidates.Remove(selected);
                    }
                    if (!found) throw new InvalidOperationException("No valid tile type for generation.");
                    continue;
                }
                int start = random.Next(types.Count);
                bool assigned = false;
                for (int i = 0; i < types.Count; i++)
                {
                    items[x, y] = types[(start + i) % types.Count];
                    if (Matches().Count == 0) { assigned = true; break; }
                }
                if (!assigned) throw new InvalidOperationException("No valid tile type for generation.");
            }
            if (lightningOpening && !TrySeedLightningOpening(types, random)) continue;
            if (HasLegalMove()) return;
            if (TrySeedSingleRuleMove(types, random)) return;
        }
        throw new InvalidOperationException("Could not generate a playable board.");
    }

    private bool TrySeedLightningOpening(IReadOnlyList<ItemType> types, Random random)
    {
        if (Width < 5 || Height < 2) return false;
        int x = (Width - 5) / 2, y = Height / 2;
        var gap = new Cell(x + 3, y);
        var donor = new Cell(x + 3, y + 1 < Height ? y + 1 : y - 1);
        var cells = Enumerable.Range(0, 5).Select(i => new Cell(x + i, y)).Concat(new[] { donor }).ToArray();
        var original = cells.Select(c => items[c.X,c.Y]).ToArray();
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var animal = types[random.Next(types.Count)];
            var different = types.Where(type => !SameMatchType(type, animal)).ToArray();
            if (different.Length == 0) return false;
            foreach (var cell in cells) items[cell.X,cell.Y] = animal;
            items[gap.X,gap.Y] = different[random.Next(different.Length)];
            bool ready = Matches().Count == 0;
            if (ready)
            {
                Swap(gap, donor);
                ready = LightningLines(Matches()).Contains((MatchType.Horizontal, y));
                Swap(gap, donor);
            }
            if (ready) return true;
            for (int i = 0; i < cells.Length; i++) items[cells[i].X,cells[i].Y] = original[i];
        }
        return false;
    }
    // Larger minimum lengths rarely occur by chance. Seed a reversible near-match
    // for a single custom rule instead of exhausting random generation attempts.
    private bool TrySeedSingleRuleMove(IReadOnlyList<ItemType> types, Random random)
    {
        if (matchRules.Count != 1) return false;
        var rule = matchRules[0];
        int length = rule.minimumLength;
        int capacity = rule.type == MatchType.Horizontal ? Width :
            rule.type == MatchType.Vertical ? Height : Width * Height;
        if (length < 2 || length > capacity || length >= Width * Height) return false;
        var palette = types.Distinct().OrderBy(_ => random.Next()).ToArray();
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            items[x, y] = palette[1 + (x + y) % 2];
        var path = new List<Cell>();
        for (int i = 0; i < length; i++)
        {
            var cell = rule.type == MatchType.Vertical ? new Cell(0, i) :
                rule.type == MatchType.Horizontal ? new Cell(i, 0) :
                new Cell((i / Width) % 2 == 0 ? i % Width : Width - 1 - i % Width, i / Width);
            path.Add(cell);
            items[cell.X, cell.Y] = palette[0];
        }
        for (int i = path.Count - 1; i >= 0; i--)
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            var donor = new Cell(x, y);
            if (path.Contains(donor)) continue;
            Swap(path[i], donor);
            // Swapping these two cells back is the guaranteed legal move.
            if (Matches().Count == 0) return true;
            Swap(path[i], donor);
        }
        return false;
    }

}

/// <summary>Per-round campaign progress. A play includes its entire cascade chain.</summary>
public sealed class CampaignMechanicState
{
    public int LitEdges { get; private set; }
    public int Treasures { get; private set; }
    public bool TreasureReady { get; private set; }
    public bool TideReady { get; private set; }
    public bool TreasurePlay { get; private set; }
    public bool TidePlay { get; private set; }
    public bool CollectedThisPlay { get; private set; }
    public int TideMoves { get; private set; }
    public int TideDirection { get; private set; }
    private long playScore, creditedScore;
    public ReplacementStrategy Gravity => new[] { ReplacementStrategy.South, ReplacementStrategy.West,
        ReplacementStrategy.North, ReplacementStrategy.East }[TideDirection];
    public void BeginPlay()
    {
        TreasurePlay=TreasureReady; TidePlay=TideReady; CollectedThisPlay=false;
        collectedCount=0;
        playScore=creditedScore=0;
    }
    public void FinishPlay(bool successful)
    {
        if (!successful) return;
        if(TreasurePlay) TreasureReady=false;
        // Treasures earned during a boosted play may arm the following play too.
        if (Treasures/3 > (Treasures-collectedCount)/3) TreasureReady=true;
        collectedCount=0;
        if(TidePlay) TideReady=false;
        TideMoves++;
        if(TideMoves==4) { TideMoves=0; TideDirection=(TideDirection+1)%4; TideReady=true; }
        TreasurePlay=TidePlay=false;
    }
    private int collectedCount;
    public void CollectTreasure() { Treasures++; collectedCount++; CollectedThisPlay=true; }
    public int BorderPercent(IReadOnlyList<BoardModel.Cell> cells,int width,int height)
    {
        foreach(var c in cells) {
            if(c.Y==0) LitEdges|=1;
            if(c.X==width-1) LitEdges|=2;
            if(c.Y==height-1) LitEdges|=4;
            if(c.X==0) LitEdges|=8;
        }
        if(LitEdges!=15) return 100;
        LitEdges=0; return 400;
    }
    public int PlayPercent(LevelBonus bonus) => bonus==LevelBonus.TreasureTrail && TreasurePlay ? 500 :
        bonus==LevelBonus.ChangingTides && TidePlay ? 300 : 100;
    // Until treasure is collected, only half the play's points are available.
    // Finding it in a later cascade releases all withheld points immediately.
    public int CreditTreasureBoss(int score)
    {
        playScore=Math.Min(int.MaxValue,playScore+Math.Max(0,score));
        long total=CollectedThisPlay||TreasurePlay ? playScore : playScore/2;
        int delta=(int)Math.Max(0,total-creditedScore); creditedScore=total; return delta;
    }
}
