using System;
using System.Collections.Generic;

public enum GoalType { Score, BlocksCleared }
public enum FreePlayMode { Timed, LimitedMoves }

/// <summary>A custom run with configurable level count and target growth. Level zero is reserved for free play.</summary>
public sealed class FreePlayConfig : LevelConfig, ILevelSettings, IConfiguredPowerOffers
{
    public MatchType Matching { get; }
    public int Width { get; }
    public int Height { get; }
    public GoalType Goal { get; }
    public FreePlayMode Mode { get; }
    public int Target { get; }
    public int Seconds { get; }
    public int Moves { get; }
    public int MinimumMatchLength { get; }
    public ReplacementStrategy Replacement { get; }
    public int Levels { get; }
    public int GrowthPercent { get; }
    public LevelPowerOffers PowerOffers { get; }
    public static int MaxMatchLength(MatchType matching, int width, int height) =>
        matching == MatchType.Horizontal ? width : matching == MatchType.Vertical ? height : Math.Max(width, height);

    public FreePlayConfig(MatchType matching = MatchType.Snake, int width = 7, int height = 7,
        GoalType goal = GoalType.Score, int target = 400, FreePlayMode mode = FreePlayMode.Timed, int seconds = 60, int moves = 20, int? minimumMatchLength = null, ReplacementStrategy replacement = ReplacementStrategy.InPlace, int levels = 10, int growthPercent = 25, LevelPowerOffers powerOffers = null)
    {
        Levels = Math.Clamp(levels, 1, 100);
        PowerOffers = powerOffers;
        PowerOffers?.ValidateRounds(Levels);
        GrowthPercent = Math.Clamp(growthPercent, 0, 1000);
        Replacement = replacement;
        Moves = Math.Clamp(moves, 1, 999);
        Matching = matching;
        Width = Math.Clamp(width, 3, 10);
        Height = Math.Clamp(height, 3, 10);
        MinimumMatchLength = Math.Clamp(minimumMatchLength ?? MatchRule.DefaultMinimum(Matching), 2, MaxMatchLength(Matching, Width, Height));
        Goal = goal;
        Target = Math.Clamp(target, 1, 1000000);
        Mode = mode;
        Seconds = Math.Clamp(seconds, 10, 3600);
    }
    public int GetLevelNumber => 0;
    public int GetTotalRounds => Levels;
    public int GetTotalTime => Seconds;
    public bool BossPowersPersist => false;
    public int GetWidth(int round) => Width;
    public int GetHeight(int round) => Height;
    public int GetTargetScore(int round) => TargetForLevel(Target, GrowthPercent, round);
    public static int TargetForLevel(int startingTarget, int growthPercent, int level)
    {
        decimal target = Math.Max(1, startingTarget);
        decimal factor = 1m + Math.Clamp(growthPercent, 0, 1000) / 100m;
        for (int current = 1; current < Math.Clamp(level, 1, 100); current++)
        {
            target *= factor;
            if (target >= int.MaxValue) return int.MaxValue;
        }
        // Keep the unrounded base between levels; decimal avoids 440 becoming 441
        // from binary floating-point error when a player chooses 10% growth.
        return (int)Math.Min(int.MaxValue, Math.Ceiling(target));
    }
    public List<MatchRule> GetMatchRules(int round) => new() { new(Matching, MinimumMatchLength) };
    public List<PowerUp> GetPowerUpsForRound(int round) => round <= 1 ? PowerUpLibrary.Starters() : PowerUpLibrary.GetAll();
    public BossPowerMap GetBossPowersMap() => new();
    public string GetLevelDescription() => $"{Width} x {Height} / {Matching} {MinimumMatchLength}+ / {Replacement} refill / {Levels} rounds / +{GrowthPercent}% per round / {Target:N0} {(Goal == GoalType.Score ? "points" : "blocks")}";
    public LevelConfig CloneNew() => new FreePlayConfig(Matching, Width, Height, Goal, Target, Mode, Seconds, Moves, MinimumMatchLength, Replacement, Levels, GrowthPercent, PowerOffers);
}
