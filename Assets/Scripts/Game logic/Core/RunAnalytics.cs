using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class AnalyticsPick
{
    public int order, round, attempt;
    public string powerId, name, description, tags;
}

[Serializable]
public sealed class AnalyticsPlay
{
    public int order, points, goalCredit, groups, playerGroups, cascadeGroups, physicalClears;
    public float seconds;
    public bool interrupted;
    public List<string> scoringBreakdown = new();
}

[Serializable]
public sealed class AnalyticsRound
{
    public int round, attempt, target, score, goalProgress, movesRemaining, shuffles;
    public float seconds;
    public string status = "active", objective, rules, bosses, roster, cohort;
    public List<int> activePickOrders = new();
    public List<AnalyticsPlay> plays = new();
    public double PointsPerPlay => plays.Count == 0 ? 0 : (double)score / plays.Count;
    public double CreditPerPlay => plays.Count == 0 ? 0 : (double)goalProgress / plays.Count;
    public int SuccessfulPlays => plays.Count(p => p.playerGroups > 0);
}

[Serializable]
public sealed class AnalyticsRun
{
    public int schemaVersion = 1, level, width, height, baseTarget, growthPercent, secondsBudget, movesBudget;
    public string id, startedUtc, endedUtc, status = "active", version, platform, levelName, habitat, goal, mode, replacement;
    public List<AnalyticsPick> picks = new();
    public List<AnalyticsRound> rounds = new();
}

/// <summary>Append-only attempt history: retries never remove earlier picks or failed attempts.</summary>
public sealed class RunAnalytics
{
    public AnalyticsRun Data { get; private set; }
    public AnalyticsRound Current => Data?.rounds.LastOrDefault();
    private AnalyticsPlay play;
    private int startScore, startCredit;
    private float startSeconds;
    public void Start(LevelConfig level, string version, string platform)
    {
        var settings = level as ILevelSettings;
        var config = level is CampaignLevel campaign ? campaign.Settings : level as FreePlayConfig;
        Data = new AnalyticsRun { id = Guid.NewGuid().ToString("N"), startedUtc = DateTime.UtcNow.ToString("O"),
            level = level.GetLevelNumber, levelName = level is CampaignLevel named ? named.Name : "Free Play",
            habitat = (level as CampaignLevel)?.Habitat?.ToString() ?? "None", version = version, platform = platform,
            goal = (settings?.Goal ?? GoalType.Score).ToString(), mode = (settings?.Mode ?? FreePlayMode.Timed).ToString(),
            replacement = (settings?.Replacement ?? ReplacementStrategy.InPlace).ToString(), width = level.GetWidth(1), height = level.GetHeight(1),
            baseTarget = level.GetTargetScore(1), growthPercent = config?.GrowthPercent ?? 0, secondsBudget = level.GetTotalTime, movesBudget = settings?.Moves ?? 0 };
        play = null;
    }
    public void BeginRound(int round, int target, string objective, string rules, string bosses, IEnumerable<ItemType> roster, IEnumerable<PowerUp> active)
    {
        if (Data == null) return;
        var attempt = new AnalyticsRound { round = round, attempt = Data.rounds.Count(r => r.round == round) + 1,
            target = target, objective = objective, rules = rules, bosses = bosses, roster = string.Join("|", roster.OrderBy(a => a)) };
        // Cohorts exclude powers/changed roster: those are effects being compared, not baseline difficulty.
        attempt.cohort = string.Join(" / ", Data.version, Data.levelName, Data.goal, Data.mode, Data.width + "x" + Data.height,
            Data.replacement, "base=" + Data.baseTarget, "growth=" + Data.growthPercent, "sec=" + Data.secondsBudget,
            "moves=" + Data.movesBudget, "round=" + round, "target=" + target, objective, rules, bosses);
        Data.rounds.Add(attempt); Data.status = "active"; Data.endedUtc = null; play = null;
        SyncBuild(active);
    }
    public void Pick(PowerUp power, IEnumerable<PowerUp> active, IEnumerable<ItemType> roster)
    {
        if (Current == null) return;
        Data.picks.Add(new AnalyticsPick { order = Data.picks.Count + 1, round = Current.round, attempt = Current.attempt,
            powerId = power.Id.ToString("N"), name = power.Name, description = power.GetCurrentPowerDescription(), tags = power.Tags.ToString() });
        SyncBuild(active); Current.roster = string.Join("|", roster.OrderBy(a => a));
    }
    private void SyncBuild(IEnumerable<PowerUp> active)
    {
        var ids = new HashSet<string>(active.Select(p => p.Id.ToString("N")));
        Current.activePickOrders = Data.picks.Where(p => ids.Contains(p.powerId)).Select(p => p.order).ToList();
    }
    public void BeginPlay(int score, int credit, float seconds)
    {
        if (Current == null || Current.status != "active" || play != null) return;
        startScore = score; startCredit = credit; startSeconds = seconds;
        play = new AnalyticsPlay { order = Current.plays.Count + 1 }; Current.plays.Add(play);
    }
    public void Group(bool playerCreated, int physicalTiles, IEnumerable<string> contributions)
    {
        if (play == null) return;
        play.groups++; if (playerCreated) play.playerGroups++; else play.cascadeGroups++;
        play.physicalClears = ScoreManager.SaturatingAdd(play.physicalClears, physicalTiles);
        if (contributions != null) play.scoringBreakdown.AddRange(contributions);
    }
    public void BonusClears(int count) { if (play != null) play.physicalClears = ScoreManager.SaturatingAdd(play.physicalClears, count); }
    public void Checkpoint(int score, int credit, float seconds, int moves)
    {
        if (Current == null || Current.status != "active") return;
        Current.score = score; Current.goalProgress = credit; Current.seconds = seconds; Current.movesRemaining = moves;
        if (play == null) return;
        play.points = Math.Max(0, score - startScore); play.goalCredit = Math.Max(0, credit - startCredit);
        play.seconds = Math.Max(0, seconds - startSeconds);
    }
    public void EndPlay(int score, int credit, float seconds, int moves, bool interrupted = false)
    {
        Checkpoint(score, credit, seconds, moves);
        if (play != null) play.interrupted = interrupted;
        play = null;
    }
    public void FinishRound(string status, int score, int credit, float seconds, int moves)
    {
        if (Current == null || Current.status != "active") return;
        EndPlay(score, credit, seconds, moves, interrupted: play != null);
        Current.status = status;
    }
    public void Close(string status)
    {
        if (Data == null) return;
        if (Current?.status == "active") Current.status = "abandoned";
        Data.status = status; Data.endedUtc = DateTime.UtcNow.ToString("O"); play = null;
    }
    public static string BuildOrder(AnalyticsRun run, AnalyticsRound round) => string.Join(" > ", round.activePickOrders
        .Select(order => run.picks.FirstOrDefault(p => p.order == order)).Where(p => p != null).Select(p => p.order + ":" + p.name));
}

/// <summary>Local device storage only. No account IDs, network service or external telemetry.</summary>
public static class AnalyticsStore
{
    public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Analytics");
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PrismatchAnalyticsDownload(string filename, string text);
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void PrismatchAnalyticsSync();
#endif
    public static void Save(AnalyticsRun data)
    {
        if (data == null || data.rounds.Count == 0) return;
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var path = Path.Combine(DirectoryPath, "run-" + data.id + ".json");
            var temp = path + ".tmp"; File.WriteAllText(temp, JsonUtility.ToJson(data, true), Encoding.UTF8);
            if (!File.Exists(path)) File.Move(temp, path);
            else
            {
                try { File.Replace(temp, path, path + ".bak"); }
                catch (PlatformNotSupportedException) { File.Copy(path, path + ".bak", true); File.Copy(temp, path, true); File.Delete(temp); }
                catch (NotImplementedException) { File.Copy(path, path + ".bak", true); File.Copy(temp, path, true); File.Delete(temp); }
                catch (IOException) { File.Copy(path, path + ".bak", true); File.Copy(temp, path, true); File.Delete(temp); }
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            PrismatchAnalyticsSync();
#endif
        }
        catch (Exception error) { Debug.LogWarning("Analytics could not be saved: " + error.Message); }
    }
    public static List<AnalyticsRun> ReadAll()
    {
        var runs = new List<AnalyticsRun>();
        if (!Directory.Exists(DirectoryPath)) return runs;
        foreach (var file in Directory.GetFiles(DirectoryPath, "run-*.json"))
        {
            AnalyticsRun data = null;
            foreach (var candidate in new[] { file, file + ".bak" })
            {
                try
                {
                    if (!File.Exists(candidate)) continue;
                    var parsed = JsonUtility.FromJson<AnalyticsRun>(File.ReadAllText(candidate));
                    if (parsed?.schemaVersion != 1 || string.IsNullOrEmpty(parsed.id) || parsed.rounds == null || parsed.picks == null) continue;
                    if (parsed.picks.Any(p => p == null) || parsed.rounds.Any(r => r == null || r.plays == null || r.activePickOrders == null ||
                        r.plays.Any(p => p == null || p.scoringBreakdown == null))) continue;
                    data = parsed; break;
                }
                catch (Exception error) { Debug.LogWarning("Could not read analytics file: " + error.Message); }
            }
            if (data != null) runs.Add(data);
        }
        return runs.OrderBy(r => r.startedUtc).ToList();
    }
    public static string Export()
    {
        var runs = ReadAll();
        var report = AnalyticsExport.Report(runs);
#if UNITY_WEBGL && !UNITY_EDITOR
        PrismatchAnalyticsDownload("prismatch-analytics.html", report);
        return "Downloaded analytics report. Open it for comparisons and CSV downloads.";
#else
        var folder = Path.Combine(DirectoryPath, "Export"); Directory.CreateDirectory(folder);
        foreach (var table in AnalyticsExport.Tables(runs)) File.WriteAllText(Path.Combine(folder, table.Key + ".csv"), AnalyticsExport.Csv(table.Value), Encoding.UTF8);
        var path = Path.Combine(folder, "prismatch-analytics.html"); File.WriteAllText(path, report, Encoding.UTF8);
        Application.OpenURL(new Uri(path).AbsoluteUri);
        return "Analytics exported to " + folder;
#endif
    }
}

public static class AnalyticsExport
{
    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    public static Dictionary<string, List<string[]>> Tables(IEnumerable<AnalyticsRun> source)
    {
        var runs = source.ToList();
        var runRows = new List<string[]> { new[] { "run_id", "started_utc", "ended_utc", "status", "level_id", "level", "habitat", "goal", "mode", "width", "height", "replacement", "base_target", "growth_percent", "seconds_budget", "moves_budget", "game_version", "platform" } };
        var picks = new List<string[]> { new[] { "run_id", "level", "pick_order", "round", "attempt", "power", "description", "tags" } };
        var rounds = new List<string[]> { new[] { "run_id", "level", "round", "attempt", "status", "plays", "successful_plays", "points", "points_per_play", "goal_credit", "credit_per_play", "seconds", "target", "shuffles", "objective", "roster", "active_pick_order", "cohort" } };
        var plays = new List<string[]> { new[] { "run_id", "round", "attempt", "play", "points", "goal_credit", "player_groups", "cascade_groups", "physical_clears", "seconds", "interrupted", "active_pick_order", "scoring_breakdown" } };
        foreach (var run in runs)
        {
            runRows.Add(new[] { run.id, run.startedUtc, run.endedUtc, run.status, N(run.level), run.levelName, run.habitat, run.goal, run.mode, N(run.width), N(run.height),
                run.replacement, N(run.baseTarget), N(run.growthPercent), N(run.secondsBudget), N(run.movesBudget), run.version, run.platform });
            foreach (var pick in run.picks) picks.Add(new[] { run.id, run.levelName, N(pick.order), N(pick.round), N(pick.attempt), pick.name, pick.description, pick.tags });
            foreach (var round in run.rounds)
            {
                var build = RunAnalytics.BuildOrder(run, round);
                rounds.Add(new[] { run.id, run.levelName, N(round.round), N(round.attempt), round.status, N(round.plays.Count), N(round.SuccessfulPlays), N(round.score), N(round.PointsPerPlay),
                    N(round.goalProgress), N(round.CreditPerPlay), N(round.seconds), N(round.target), N(round.shuffles), round.objective, round.roster, build, round.cohort });
                foreach (var play in round.plays) plays.Add(new[] { run.id, N(round.round), N(round.attempt), N(play.order), N(play.points), N(play.goalCredit), N(play.playerGroups), N(play.cascadeGroups),
                    N(play.physicalClears), N(play.seconds), play.interrupted.ToString(), build, string.Join(" | ", play.scoringBreakdown) });
            }
        }
        var exposure = new List<string[]> { new[] { "cohort", "power", "copies", "runs", "round_attempts", "plays", "points_per_play", "credit_per_play", "round_win_rate" } };
        var samples = runs.SelectMany(run => run.rounds.Where(r => r.status == "won" || r.status == "failed").SelectMany(round =>
            run.picks.Where(p => round.activePickOrders.Contains(p.order)).GroupBy(p => p.name).Select(p => new { run, round, name = p.Key, copies = p.Count() })));
        foreach (var group in samples.GroupBy(s => new { s.round.cohort, s.name, s.copies }).OrderBy(g => g.Key.cohort).ThenBy(g => g.Key.name))
        {
            long count = group.Sum(s => (long)s.round.plays.Count);
            exposure.Add(new[] { group.Key.cohort, group.Key.name, N(group.Key.copies), N(group.Select(s => s.run.id).Distinct().Count()), N(group.Count()), N(count),
                N(count == 0 ? 0 : group.Sum(s => (double)s.round.score) / count), N(count == 0 ? 0 : group.Sum(s => (double)s.round.goalProgress) / count), N((double)group.Count(s => s.round.status == "won") / group.Count()) });
        }
        return new Dictionary<string, List<string[]>> { ["runs"] = runRows, ["rounds"] = rounds, ["power-picks"] = picks, ["plays"] = plays, ["power-exposure"] = exposure };
    }
    public static string Csv(IEnumerable<string[]> rows) => string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(value =>
    {
        value ??= "";
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0].ToString())) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    })))) + "\r\n";
    private static string Html(string text) => (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
    public static string Report(IEnumerable<AnalyticsRun> runs)
    {
        var tables = Tables(runs);
        var html = new StringBuilder("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Prismatch analytics</title><style>body{background:#0b1f3a;color:#fff7e8;font:15px system-ui;margin:32px}h1,h2{color:#67dacf}a{display:inline-block;color:#0b1f3a;background:#67dacf;padding:12px;margin:8px 8px 8px 0;border-radius:8px}table{border-collapse:collapse;font-size:13px}th,td{padding:9px;border:1px solid #345171;text-align:left;max-width:480px;overflow-wrap:anywhere}th{background:#1d3b68;position:sticky;top:0}.scroll{overflow:auto;max-height:600px}input{padding:12px;width:90%;margin:15px 0}p{max-width:950px;line-height:1.6}</style></head><body><h1>Prismatch / power testing</h1><p>A play is one swap attempt, including failed swaps. Points and goal credit per play include all resulting cascades. Shuffles are separate. Failed attempts and replacement picks on retries are preserved. Incomplete/abandoned rounds remain in raw exports but are excluded from power comparisons.</p><p><b>Power exposure measures the whole build while a power was owned, not that power's isolated contribution.</b> Compare the same cohort, inspect pick order and sample counts, and repeat runs. Means are weighted by plays. Copy counts are separated; win rate is round attempts won / terminal attempts. Scoring breakdowns describe interacting effects and should not be summed as independent power value.</p><input id='filter' placeholder='Filter tables by power, level, or objective' aria-label='Filter analytics'>");
        foreach (var table in tables)
        {
            var csv = Convert.ToBase64String(Encoding.UTF8.GetBytes(Csv(table.Value)));
            html.Append("<a download='" + table.Key + ".csv' href='data:text/csv;charset=utf-8;base64," + csv + "'>Download " + table.Key + ".csv</a>");
        }
        foreach (var name in new[] { "power-exposure", "rounds", "power-picks", "plays", "runs" })
        {
            html.Append("<h2>" + name + "</h2><div class='scroll'><table><thead><tr>");
            foreach (var cell in tables[name][0]) html.Append("<th>" + Html(cell) + "</th>");
            html.Append("</tr></thead><tbody>");
            foreach (var row in tables[name].Skip(1)) { html.Append("<tr>"); foreach (var cell in row) html.Append("<td>" + Html(cell) + "</td>"); html.Append("</tr>"); }
            html.Append("</tbody></table></div>");
        }
        html.Append("<script>document.getElementById('filter').addEventListener('input',function(){const q=this.value.toLowerCase();document.querySelectorAll('tbody tr').forEach(r=>r.hidden=!r.textContent.toLowerCase().includes(q));});</script></body></html>");
        return html.ToString();
    }
}
