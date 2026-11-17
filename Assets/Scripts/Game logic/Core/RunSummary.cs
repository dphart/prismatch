using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoundStats
{
    public int round, score, target, matches, cascades, zeroPointMatches;
    public GoalType goalType;
    public int goalProgress, blocksCleared, movesRemaining, tilesCleared;
    public float seconds, scorePerSecond;
    public bool completed;
}

[Serializable]
public sealed class RunSummary
{
    public int TotalScore, Matches, Cascades, ZeroPointMatches, Shuffles;
    public List<RoundStats> rounds = new();
    private int roundMatches, roundCascades, roundZero, roundTiles;
    public int TotalTilesCleared
    {
        get { int total = 0; foreach (var round in rounds) total = ScoreManager.SaturatingAdd(total, round.tilesCleared); return total; }
    }
    public int CompletedRounds => rounds.FindAll(r => r.completed).Count;
    public void Reset()
    {
        TotalScore = Matches = Cascades = ZeroPointMatches = Shuffles = 0;
        rounds.Clear(); BeginRound();
    }
    public void BeginRound() { roundMatches = roundCascades = roundZero = roundTiles = 0; }
    public void RecordClearedTiles(int count) => roundTiles = ScoreManager.SaturatingAdd(roundTiles, Math.Max(0, count));
    public void RecordMatch(int score, bool userInitiated)
    {
        TotalScore = ScoreManager.SaturatingAdd(TotalScore, score);
        Matches++; roundMatches++;
        if (!userInitiated) { Cascades++; roundCascades++; }
        if (score == 0) { ZeroPointMatches++; roundZero++; }
    }
    public void FinishRound(int round, int score, int target, float seconds, bool completed, GoalType goalType = GoalType.Score,
        int goalProgress = -1, int blocksCleared = 0, int movesRemaining = 0)
    {
        rounds.Add(new RoundStats { round = round, score = score, target = target, seconds = seconds,
            goalType = goalType, goalProgress = goalProgress < 0 ? score : goalProgress,
            blocksCleared = blocksCleared, movesRemaining = movesRemaining, tilesCleared = roundTiles,
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
        PlayerPrefs.SetInt("Prismatch.LastRun.Difficulty", (GameState.Instance?.CurrentLevel as CampaignLevel)?.Difficulty ?? 1);
        PlayerPrefs.SetInt("Prismatch.LastRun.Won", won ? 1 : 0);
        PlayerPrefs.Save();
    }
}

public static class ProgressStore
{
    public const string SelectedCampaignLevelKey = "Prismatch.CampaignV2.SelectedLevel";
    public static int SelectionAfterWin(int selected, int level, bool alreadyCompleted, int totalLevels) =>
        alreadyCompleted ? selected : Math.Min(totalLevels, level + 1);
    public static void ResetLocalPlayerData()
    {
        for(int level=1;level<=LevelLibrary.TotalLevels;level++) {
            int completed=HighestCompletedDifficulty(level);
            string progress=$"Prismatch.CampaignV2.Level.{level}.";
            for(int difficulty=1;difficulty<=completed;difficulty++)
                PlayerPrefs.DeleteKey(progress+$"Difficulty.{difficulty}.Best");
            foreach(string suffix in new[]{"Completed","Best","DifficultyCompleted"})PlayerPrefs.DeleteKey(progress+suffix);
            PlayerPrefs.DeleteKey($"Prismatch.Level.{level}.SelectedDifficulty");
            PlayerPrefs.DeleteKey($"Prismatch.LevelNotes.{level}");
            PlayerPrefs.DeleteKey($"Prismatch.LevelScaling.{level}.Target");
            PlayerPrefs.DeleteKey($"Prismatch.LevelScaling.{level}.Growth");
        }
        foreach(string key in new[]{"Prismatch.CampaignV2.SelectedLevel","Prismatch.LastRun","Prismatch.LastRun.Level","Prismatch.LastRun.Difficulty","Prismatch.LastRun.Won","VisualTutorialSeen"})PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    public static int UnlockedLevel()
    {
        int level = 1;
        while (level < LevelLibrary.TotalLevels && IsCompleted(level)) level++;
        return level;
    }
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt($"Prismatch.CampaignV2.Level.{level}.Completed", 0) == 1;
    public static int BestScore(int level) => PlayerPrefs.GetInt($"Prismatch.CampaignV2.Level.{level}.Best", 0);
    public static int HighestCompletedDifficulty(int level) => Math.Max(IsCompleted(level) ? 1 : 0,
        PlayerPrefs.GetInt($"Prismatch.CampaignV2.Level.{level}.DifficultyCompleted", 0));
    public static int UnlockedDifficulty(int level) => NextDifficulty(HighestCompletedDifficulty(level));
    public static int NextDifficulty(int completed) => completed >= int.MaxValue ? int.MaxValue : Math.Max(1, completed + 1);
    public static int CompletedAfterWin(int completed, int difficulty) => difficulty >= 1 && difficulty <= NextDifficulty(completed)
        ? Math.Max(completed, difficulty) : Math.Max(0, completed);
    public static bool IsDifficultyCompleted(int level, int difficulty) => difficulty >= 1 && difficulty <= HighestCompletedDifficulty(level);
    public static int BestScore(int level, int difficulty) => difficulty == 1 ? BestScore(level) :
        PlayerPrefs.GetInt($"Prismatch.CampaignV2.Level.{level}.Difficulty.{difficulty}.Best", 0);
    public static void MarkCompleted(int level, int score, int difficulty = 1)
    {
        difficulty = Math.Max(1, difficulty);
        if (difficulty > UnlockedDifficulty(level)) return;
        bool alreadyCompleted=IsCompleted(level);
        if(!alreadyCompleted)
            PlayerPrefs.SetInt(SelectedCampaignLevelKey,SelectionAfterWin(
                PlayerPrefs.GetInt(SelectedCampaignLevelKey,1),level,alreadyCompleted,LevelLibrary.TotalLevels));
        PlayerPrefs.SetInt($"Prismatch.CampaignV2.Level.{level}.DifficultyCompleted", CompletedAfterWin(HighestCompletedDifficulty(level), difficulty));
        PlayerPrefs.SetInt($"Prismatch.CampaignV2.Level.{level}.Difficulty.{difficulty}.Best", Math.Max(score, BestScore(level, difficulty)));
        PlayerPrefs.SetInt($"Prismatch.CampaignV2.Level.{level}.Completed", 1);
        PlayerPrefs.SetInt($"Prismatch.CampaignV2.Level.{level}.Best", Math.Max(score, BestScore(level)));
        PlayerPrefs.Save();
    }
}
