using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoundStats
{
    public int round, score, target, matches, cascades, zeroPointMatches;
    public float seconds, scorePerSecond;
    public bool completed;
}

[Serializable]
public sealed class RunSummary
{
    public int TotalScore, Matches, Cascades, ZeroPointMatches, Shuffles;
    public List<RoundStats> rounds = new();
    private int roundMatches, roundCascades, roundZero;
    public int CompletedRounds => rounds.FindAll(r => r.completed).Count;
    public void Reset()
    {
        TotalScore = Matches = Cascades = ZeroPointMatches = Shuffles = 0;
        rounds.Clear(); BeginRound();
    }
    public void BeginRound() { roundMatches = roundCascades = roundZero = 0; }
    public void RecordMatch(int score, bool userInitiated)
    {
        TotalScore = ScoreManager.SaturatingAdd(TotalScore, score);
        Matches++; roundMatches++;
        if (!userInitiated) { Cascades++; roundCascades++; }
        if (score == 0) { ZeroPointMatches++; roundZero++; }
    }
    public void FinishRound(int round, int score, int target, float seconds, bool completed)
    {
        rounds.Add(new RoundStats { round = round, score = score, target = target, seconds = seconds,
            scorePerSecond = score / Mathf.Max(1f, seconds), completed = completed,
            matches = roundMatches, cascades = roundCascades, zeroPointMatches = roundZero });
        Debug.Log($"Prismatch round {round}: {score}/{target}, {seconds:F1}s, {roundMatches} matches, success={completed}");
    }
    public void UndoLastFailedRound()
    {
        if (rounds.Count == 0 || rounds[^1].completed) return;
        var failed = rounds[^1]; rounds.RemoveAt(rounds.Count - 1);
        TotalScore = Math.Max(0, TotalScore - failed.score);
        Matches = Math.Max(0, Matches - failed.matches);
        Cascades = Math.Max(0, Cascades - failed.cascades);
        ZeroPointMatches = Math.Max(0, ZeroPointMatches - failed.zeroPointMatches);
        BeginRound();
    }
    public void Save(bool won, int level)
    {
        PlayerPrefs.SetString("Prismatch.LastRun", JsonUtility.ToJson(this));
        PlayerPrefs.SetInt("Prismatch.LastRun.Level", level);
        PlayerPrefs.SetInt("Prismatch.LastRun.Won", won ? 1 : 0);
        PlayerPrefs.Save();
    }
}

public static class ProgressStore
{
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt($"Prismatch.Level.{level}.Completed", 0) == 1;
    public static int BestScore(int level) => PlayerPrefs.GetInt($"Prismatch.Level.{level}.Best", 0);
    public static void MarkCompleted(int level, int score)
    {
        PlayerPrefs.SetInt($"Prismatch.Level.{level}.Completed", 1);
        PlayerPrefs.SetInt($"Prismatch.Level.{level}.Best", Math.Max(score, BestScore(level)));
        PlayerPrefs.Save();
    }
}
