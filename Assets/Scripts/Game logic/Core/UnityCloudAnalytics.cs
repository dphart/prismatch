using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine.UnityConsent;

public sealed class CloudAnalyticsEvent
{
    public string Name { get; }
    public Dictionary<string, object> Parameters { get; }
    public CloudAnalyticsEvent(string name, Dictionary<string, object> parameters) { Name = name; Parameters = parameters; }
}

/// <summary>Pure event construction, shared by SDK dispatch, schema documentation and tests.</summary>
public static class UnityAnalyticsEvents
{
    private static Dictionary<string, object> Context(AnalyticsRun run, AnalyticsRound round) => new()
    {
        ["pm_run_id"] = run.id, ["pm_level_name"] = run.levelName, ["pm_level_number"] = run.level,
        ["pm_goal"] = run.goal, ["pm_mode"] = run.mode, ["pm_habitat"] = run.habitat,
        ["pm_revision"] = run.version, ["pm_round_number"] = round.round, ["pm_attempt"] = round.attempt
    };
    public static CloudAnalyticsEvent Pick(AnalyticsRun run, AnalyticsRound round, AnalyticsPick pick)
    {
        var data = Context(run, round);
        data["pm_pick_order"] = pick.order; data["pm_power_name"] = pick.name;
        data["pm_power_tags"] = pick.tags; data["pm_build"] = RunAnalytics.BuildOrder(run, round);
        return new CloudAnalyticsEvent("pm_power_picked", data);
    }
    public static CloudAnalyticsEvent Round(AnalyticsRun run, AnalyticsRound round)
    {
        var data = Context(run, round);
        data["pm_outcome"] = round.status; data["pm_plays"] = round.plays.Count;
        data["pm_successful_plays"] = round.SuccessfulPlays; data["pm_points"] = round.score;
        data["pm_goal_credit"] = round.goalProgress; data["pm_points_per_play"] = (float)round.PointsPerPlay;
        data["pm_credit_per_play"] = (float)round.CreditPerPlay; data["pm_duration_seconds"] = round.seconds;
        data["pm_cascade_groups"] = round.plays.Sum(p => p.cascadeGroups); data["pm_shuffles"] = round.shuffles;
        data["pm_target"] = round.target; data["pm_objective"] = round.objective;
        data["pm_rules"] = round.rules; data["pm_bosses"] = round.bosses;
        data["pm_board_width"] = run.width; data["pm_board_height"] = run.height;
        data["pm_seconds_budget"] = run.secondsBudget; data["pm_moves_budget"] = run.movesBudget;
        data["pm_growth_percent"] = run.growthPercent; data["pm_gravity"] = run.replacement;
        data["pm_build"] = RunAnalytics.BuildOrder(run, round);
        return new CloudAnalyticsEvent("pm_round_finished", data);
    }
    public static IEnumerable<CloudAnalyticsEvent> PowerRounds(AnalyticsRun run, AnalyticsRound round)
    {
        // Abandoned attempts remain in round events, but never become misleading power-comparison samples.
        if (round.status != "won" && round.status != "failed") yield break;
        foreach (var powers in run.picks.Where(p => round.activePickOrders.Contains(p.order)).GroupBy(p => p.name))
        {
            var data = new Dictionary<string, object>(Round(run, round).Parameters);
            data["pm_power_name"] = powers.Key; data["pm_copies"] = powers.Count();
            data["pm_first_pick_order"] = powers.Min(p => p.order);
            yield return new CloudAnalyticsEvent("pm_power_round", data);
        }
    }
}

/// <summary>Opt-in UGS adapter. Historical local files are never uploaded. SDK batching handles delivery.</summary>
public static class UnityCloudAnalytics
{
    [Serializable] private sealed class Config { public bool schemaReady; public string environment = "production"; }
    private static Config config;
    private static Config Settings => config ??= ReadConfig();
    private static Config ReadConfig()
    {
        try
        {
            var asset = Resources.Load<TextAsset>("UnityAnalyticsConfig");
            return asset != null ? JsonUtility.FromJson<Config>(asset.text) ?? new Config() : new Config();
        }
        catch (Exception error) { Debug.LogWarning("Unity Analytics configuration is invalid: " + error.Message); return new Config(); }
    }
    private static bool ready;
    private static Task initialization;
    private static readonly Queue<CloudAnalyticsEvent> pending = new();
    private static string lastError;
    public static bool SchemaReady => Settings.schemaReady;
    public static bool ConsentGranted => EndUserConsent.GetConsentState().AnalyticsIntent == ConsentStatus.Granted;
    public static bool SdkAvailable => true;
    public static string Status => !SchemaReady ? "Unity Dashboard event setup required" :
        lastError != null ? "Unity Analytics: " + lastError : !ConsentGranted ? "Unity cloud analytics OFF" : ready ? "Unity cloud analytics ON / " + Settings.environment : "Connecting to Unity Analytics";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { ready = false; initialization = null; pending.Clear(); config = null; lastError = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Startup() { if (SchemaReady) _ = InitializeAsync(); }

    private static Task InitializeAsync()
    {
        if (initialization != null && !initialization.IsCompleted) return initialization;
        return initialization = InitializeSdkAsync();
    }
    private static async Task InitializeSdkAsync()
    {
        if (!SchemaReady || ready) return;
        try
        {
            lastError = null;
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(Settings.environment));
            ready = true;
            if (ConsentGranted) while (pending.Count > 0) Send(pending.Dequeue());
            else pending.Clear();
        }
        catch (Exception error) { lastError = error.Message; pending.Clear(); Debug.LogWarning("Unity Analytics initialization failed: " + error.Message); }
    }
    public static async Task<string> SetConsent(bool allow)
    {
        if (allow && !SchemaReady) return Status + ". Complete feature_descriptions/unity_analytics.md first.";
        var consent = EndUserConsent.GetConsentState();
        consent.AnalyticsIntent = allow ? ConsentStatus.Granted : ConsentStatus.Denied;
        EndUserConsent.SetConsentState(consent); // Preserve AdsIntent; this toggle controls Analytics only.
        if (!allow) pending.Clear();
        if (SchemaReady) await InitializeAsync();
        return Status;
    }
    public static async Task<string> DeleteCloudData()
    {
        await SetConsent(false);
        if (!SchemaReady) return "Cloud setup is incomplete; finish setup to request Unity data deletion.";
        await InitializeAsync();
        if (!ready) return Status;
        try { AnalyticsService.Instance.RequestDataDeletion(); return "Unity data deletion requested. Cloud analytics is OFF. Local CSV history is retained."; }
        catch (Exception error) { return "Could not request deletion: " + error.Message; }
    }
    public static void Record(CloudAnalyticsEvent data)
    {
        if (!Application.isPlaying || !SchemaReady || !ConsentGranted) return;
        if (ready) Send(data);
        else
        {
            if (pending.Count < 128) pending.Enqueue(data);
            _ = InitializeAsync();
        }
    }
    private static void Send(CloudAnalyticsEvent data)
    {
        if (!ConsentGranted) return;
        try
        {
            var item = new CustomEvent(data.Name);
            foreach (var parameter in data.Parameters) item.Add(parameter.Key, parameter.Value ?? "");
            AnalyticsService.Instance.RecordEvent(item);
        }
        catch (Exception error) { lastError = error.Message; Debug.LogWarning("Unity Analytics event failed: " + error.Message); }
    }
    public static void Pick(AnalyticsRun run, AnalyticsRound round, AnalyticsPick pick)
    {
        if (run != null && round != null && pick != null) Record(UnityAnalyticsEvents.Pick(run, round, pick));
    }
    public static void FinishRound(AnalyticsRun run, AnalyticsRound round)
    {
        if (run == null || round == null) return;
        Record(UnityAnalyticsEvents.Round(run, round));
        foreach (var item in UnityAnalyticsEvents.PowerRounds(run, round)) Record(item);
        if (ready && ConsentGranted)
        {
            try { AnalyticsService.Instance.Flush(); }
            catch (Exception error) { Debug.LogWarning("Unity Analytics flush failed: " + error.Message); }
        }
    }
}
