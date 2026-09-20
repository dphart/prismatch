using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ScoreResult
{
    public int Score, BasePoints, Multiplier = 1, NextMultiplier = 1;
    public string Reason;
    public readonly List<string> Contributions = new();
}

public class ScoreManager
{
    private readonly PowerManager powerManager;
    public int nextMatchMulti = 1;
    public ScoreResult LastResult { get; private set; } = new();
    // Base scoring is supplied by powers; the legacy constructor remains compatible.
    public ScoreManager(PlayerConfigs configs, PowerManager powerManager) { this.powerManager = powerManager; }
    public void Reset() { nextMatchMulti = 1; LastResult = new ScoreResult(); }

    public static int SaturatingAdd(int a, int b) => Clamp((long)a + b);
    private static int Clamp(long value) => (int)Math.Max(0, Math.Min(int.MaxValue, value));
    private static int Multiply(int a, int b) => Clamp((long)a * b);

    public int CalculateScore(List<Tile> tiles)
    {
        var result = new ScoreResult();
        LastResult = result;
        if (tiles == null || tiles.Count == 0)
        {
            result.Reason = "No tiles matched.";
            return 0;
        }
        // Blocked clears preserve queued bonuses and do not advance scoring-power counters.
        foreach (var power in powerManager.ActivePowers())
            if (!power.IsScoreValid(tiles)) { result.Reason = $"Blocked by {power.Name}"; result.NextMultiplier = nextMatchMulti; return 0; }
        foreach (var boss in powerManager.ActiveBossPowers())
            if (!boss.IsScoreValid(tiles)) { result.Reason = $"Blocked by {boss.Name}"; result.NextMultiplier = nextMatchMulti; return 0; }

        int basePoints = 0, additiveMultiplier = 0, currentMultiplier = nextMatchMulti;
        nextMatchMulti = 1;
        foreach (var power in powerManager.ActivePowers())
        {
            int addedBase = power.AdditiveBaseMod(tiles);
            int addedMulti = power.AdditiveMultiplierMod(tiles);
            int immediate = power.CurrentMatchMultiplier(tiles);
            int queued = power.NextMatchMulti(tiles);
            basePoints = SaturatingAdd(basePoints, addedBase);
            additiveMultiplier = SaturatingAdd(additiveMultiplier, addedMulti);
            currentMultiplier = Multiply(currentMultiplier, Math.Max(1, immediate));
            nextMatchMulti = Multiply(nextMatchMulti, Math.Max(1, queued));
            if (addedBase != 0 || addedMulti != 0 || immediate > 1 || queued > 1)
                result.Contributions.Add($"{power.Name}:" + (addedBase != 0 ? $" +{addedBase} base" : "") +
                    (addedMulti != 0 ? $" +{addedMulti} multi" : "") + (immediate > 1 ? $" ×{immediate}" : "") +
                    (queued > 1 ? $" next ×{queued}" : ""));
        }
        result.BasePoints = basePoints;
        result.Multiplier = Multiply(SaturatingAdd(1, additiveMultiplier), currentMultiplier);
        result.NextMultiplier = nextMatchMulti;
        int score = Multiply(basePoints, result.Multiplier);
        foreach (var boss in powerManager.ActiveBossPowers())
        {
            int before = score;
            score = Math.Max(0, boss.ModifyAndReturnScore(tiles, score));
            if (before != score) result.Contributions.Add($"{boss.Name}: {before} → {score}");
        }
        result.Score = score;
        if (basePoints == 0) result.Reason = "No base points for this match — choose a base-point power.";
        else if (score == 0) result.Reason = "Boss penalty reduced this match to zero.";
        return score;
    }
}
