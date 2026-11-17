using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

[TestFixture]
public class UnityAnalyticsEventTests
{
    public static AnalyticsRun Sample()
    {
        var run = new AnalyticsRun { id = "test-run", level = 5, levelName = "Collection test", habitat = "Garden", goal = "BlocksCleared",
            mode = "LimitedMoves", version = "test-v1", replacement = "InPlace", width = 6, height = 6, secondsBudget = 60, movesBudget = 16, growthPercent = 25 };
        run.picks.Add(new AnalyticsPick { order = 1, round = 1, attempt = 1, name = "Duck Collector", tags = "Collection, GoalCredit" });
        run.picks.Add(new AnalyticsPick { order = 2, round = 2, attempt = 1, name = "Duck Collector", tags = "Collection, GoalCredit" });
        run.picks.Add(new AnalyticsPick { order = 3, round = 2, attempt = 2, name = "Also Count Elephant", tags = "Collection, Admission" });
        run.rounds.Add(new AnalyticsRound { round = 2, attempt = 2, target = 30, score = 0, goalProgress = 18, seconds = 12,
            status = "won", objective = "Duck ONLY", rules = "Snake 3+", bosses = "", activePickOrders = new List<int> { 1, 2, 3 },
            plays = new List<AnalyticsPlay> { new() { playerGroups = 1, cascadeGroups = 2 }, new() } });
        return run;
    }
    [Test]
    public void PickEventsCarryRunRoundAttemptAndExactOrder()
    {
        var run = Sample(); var round = run.rounds[0];
        var item = UnityAnalyticsEvents.Pick(run, round, run.picks[2]);
        Assert.That(item.Name, Is.EqualTo("pm_power_picked"));
        Assert.That(item.Parameters["pm_run_id"], Is.EqualTo("test-run"));
        Assert.That(item.Parameters["pm_attempt"], Is.EqualTo(2));
        Assert.That(item.Parameters["pm_pick_order"], Is.EqualTo(3));
        Assert.That(item.Parameters["pm_power_name"], Is.EqualTo("Also Count Elephant"));
        Assert.That(item.Parameters["pm_build"].ToString(), Does.Contain("1:Duck Collector > 2:Duck Collector > 3:Also Count Elephant"));
    }
    [Test]
    public void RoundEventsKeepCreditSeparateAndProvideRawTotalsForWeightedAverages()
    {
        var run = Sample(); var item = UnityAnalyticsEvents.Round(run, run.rounds[0]);
        Assert.That(item.Name, Is.EqualTo("pm_round_finished"));
        Assert.That(item.Parameters["pm_plays"], Is.EqualTo(2));
        Assert.That(item.Parameters["pm_successful_plays"], Is.EqualTo(1));
        Assert.That(item.Parameters["pm_points"], Is.EqualTo(0));
        Assert.That(item.Parameters["pm_goal_credit"], Is.EqualTo(18));
        Assert.That(item.Parameters["pm_credit_per_play"], Is.EqualTo(9f));
        Assert.That(item.Parameters["pm_cascade_groups"], Is.EqualTo(2));
        Assert.That(item.Parameters["pm_board_width"], Is.EqualTo(6));
        Assert.That(item.Parameters["pm_moves_budget"], Is.EqualTo(16));
        Assert.That(item.Parameters["pm_objective"], Is.EqualTo("Duck ONLY"));
    }
    [Test]
    public void PowerRoundEventsAggregateCopiesWithoutDoubleCountingOneBuild()
    {
        var run = Sample(); var round = run.rounds[0];
        var items = UnityAnalyticsEvents.PowerRounds(run, round).ToList();
        Assert.That(items.Count, Is.EqualTo(2));
        var duck = items.Single(e => (string)e.Parameters["pm_power_name"] == "Duck Collector");
        Assert.That(duck.Parameters["pm_copies"], Is.EqualTo(2));
        Assert.That(duck.Parameters["pm_first_pick_order"], Is.EqualTo(1));
        Assert.That(duck.Parameters["pm_goal_credit"], Is.EqualTo(18));
        round.activePickOrders.Remove(2);
        Assert.That(UnityAnalyticsEvents.PowerRounds(run, round).Single(e => (string)e.Parameters["pm_power_name"] == "Duck Collector")
            .Parameters["pm_copies"], Is.EqualTo(1));
        round.status = "abandoned";
        Assert.That(UnityAnalyticsEvents.PowerRounds(run, round), Is.Empty);
        round.status = "active";
        Assert.That(UnityAnalyticsEvents.PowerRounds(run, round), Is.Empty);
    }
    [Test]
    public void CustomEventsUseValidNamesAndPrimitiveSdkParameters()
    {
        var run = Sample(); var round = run.rounds[0];
        var events = new[] { UnityAnalyticsEvents.Pick(run, round, run.picks[0]), UnityAnalyticsEvents.Round(run, round) }
            .Concat(UnityAnalyticsEvents.PowerRounds(run, round));
        foreach (var item in events)
        {
            Assert.That(Regex.IsMatch(item.Name, "^[a-zA-Z][a-zA-Z0-9_]*$"), Is.True);
            foreach (var pair in item.Parameters)
            {
                Assert.That(Regex.IsMatch(pair.Key, "^pm_[a-z0-9_]+$"), Is.True);
                Assert.That(pair.Value is string || pair.Value is int || pair.Value is float, Is.True, pair.Key);
            }
            Assert.That(item.Parameters.ContainsKey("userID"), Is.False, "The SDK supplies its own identifiers; custom code does not upload local account details.");
        }
    }
}
