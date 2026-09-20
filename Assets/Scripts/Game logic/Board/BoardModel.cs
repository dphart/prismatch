using System;
using System.Collections.Generic;
using System.Linq;

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

    private readonly ItemType?[,] items;
    private List<MatchRule> matchRules;
    public int Width => items.GetLength(0);
    public int Height => items.GetLength(1);
    public ItemType? this[int x, int y] { get => items[x, y]; set => items[x, y] = value; }
    public BoardModel(int width, int height, IEnumerable<MatchRule> rules = null)
    {
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
        items = new ItemType?[width, height];
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
                items[cell.X, cell.Y] != type || !visited.Add(cell)) continue;
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
                if (!type.HasValue) { start++; continue; }
                int end = start + 1;
                while (end < inner)
                {
                    int nextX = horizontal ? end : line;
                    int nextY = horizontal ? line : end;
                    if (items[nextX, nextY] != type) break;
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
            if (items[first.X, first.Y] == items[second.X, second.Y]) continue;
            Swap(first, second);
            bool valid = Matches().Any(group => group.Contains(first) || group.Contains(second));
            Swap(first, second);
            if (valid) return true;
        }
        return false;
    }

    public void Generate(IReadOnlyList<ItemType> types, Random random)
    {
        if (types == null || new HashSet<ItemType>(types).Count < 3)
            throw new ArgumentException("Board generation needs at least three different tile types.", nameof(types));
        if (Width * Height < 4) throw new InvalidOperationException("Board is too small for a legal swap.");
        for (int attempt = 0; attempt < 100; attempt++)
        {
            Array.Clear(items, 0, items.Length);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int start = random.Next(types.Count);
                bool assigned = false;
                for (int i = 0; i < types.Count; i++)
                {
                    items[x, y] = types[(start + i) % types.Count];
                    if (Matches().Count == 0) { assigned = true; break; }
                }
                if (!assigned) throw new InvalidOperationException("No valid tile type for generation.");
            }
            if (HasLegalMove()) return;
        }
        throw new InvalidOperationException("Could not generate a playable board.");
    }
}
