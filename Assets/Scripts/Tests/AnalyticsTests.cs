using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class AnalyticsTests
{
    private static RunAnalytics Session(GoalType goal = GoalType.Score, FreePlayMode mode = FreePlayMode.Timed)
    {
        var analytics = new RunAnalytics();
        analytics.Start(new FreePlayConfig(goal: goal, mode: mode, levels: 3), "test-v1", "test");
        Begin(analytics);
        return analytics;
    }
    private static void Begin(RunAnalytics analytics, int round = 1, int target = 400, string objective = "Points", params PowerUp[] active) =>
        analytics.BeginRound(round, target, objective, "Snake 3+", "No boss", new[] { ItemType.Duck, ItemType.Elephant }, active);

    [Test]
    public void RetryPreservesFailedAttemptAndChronologicalPicksButDropsRemovedPowerFromBuild()
    {
        var analytics = Session();
        var first = new HabitatPower(HabitatStyle.Shore, 0);
        analytics.Pick(first, new[] { first }, new[] { ItemType.Duck });
        analytics.FinishRound("failed", 30, 30, 60, 0); analytics.Close("failed");
        Begin(analytics);
        var replacement = new HabitatPower(HabitatStyle.Giants, 1);
        analytics.Pick(replacement, new[] { replacement }, new[] { ItemType.Duck });
        Assert.That(analytics.Data.picks.Select(p => p.order), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(analytics.Data.picks.Select(p => p.attempt), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(analytics.Data.rounds[0].status, Is.EqualTo("failed"));
        Assert.That(analytics.Current.activePickOrders, Is.EqualTo(new[] { 2 }));
        Assert.That(RunAnalytics.BuildOrder(analytics.Data, analytics.Current), Is.EqualTo("2:Giant Amplifier"));
        Assert.That(analytics.Data.status, Is.EqualTo("active"));
        Assert.That(analytics.Data.endedUtc, Is.Null);
    }

    [Test]
    public void FailedSwapsCountAsPlaysAndCascadesStayWithinTheirOriginatingPlay()
    {
        var analytics = Session();
        analytics.BeginPlay(0, 0, 0);
        analytics.BeginPlay(0, 0, 0); // Duplicate callback must not count another swap.
        analytics.Group(true, 4, new[] { "Bounty: +40 base" });
        analytics.Group(false, 4, null); analytics.Group(false, 4, null); analytics.BonusClears(2);
        analytics.EndPlay(100, 8, 2, 18);
        analytics.BeginPlay(100, 8, 3); // A swap without any resolved groups.
        analytics.EndPlay(100, 8, 4, 17);
        analytics.EndPlay(100, 8, 4, 17);
        analytics.FinishRound("won", 100, 8, 4, 17);
        var round = analytics.Current;
        Assert.That(round.plays.Count, Is.EqualTo(2));
        Assert.That(round.SuccessfulPlays, Is.EqualTo(1));
        Assert.That(round.PointsPerPlay, Is.EqualTo(50));
        Assert.That(round.CreditPerPlay, Is.EqualTo(4));
        Assert.That(round.plays[0].groups, Is.EqualTo(3));
        Assert.That(round.plays[0].cascadeGroups, Is.EqualTo(2));
        Assert.That(round.plays[0].physicalClears, Is.EqualTo(14));
        Assert.That(round.plays[0].points, Is.EqualTo(100));
        Assert.That(round.plays[1].points, Is.Zero);
        Assert.That(round.plays[0].scoringBreakdown, Has.Count.EqualTo(1));
    }

    [Test]
    public void BlockCreditAndPhysicalClearsRemainSeparateAndInterruptedPlayIsPreserved()
    {
        var analytics = Session(GoalType.BlocksCleared);
        analytics.BeginPlay(0, 0, 0); analytics.Group(true, 4, null); analytics.BonusClears(2);
        analytics.FinishRound("failed", 0, 9, 60, 3);
        Assert.That(analytics.Current.plays[0].interrupted, Is.True);
        Assert.That(analytics.Current.PointsPerPlay, Is.Zero);
        Assert.That(analytics.Current.CreditPerPlay, Is.EqualTo(9));
        Assert.That(analytics.Current.plays[0].physicalClears, Is.EqualTo(6));
        analytics.Checkpoint(999, 999, 999, 999);
        Assert.That(analytics.Current.goalProgress, Is.EqualTo(9), "Terminal attempts cannot be overwritten.");
    }

    private static AnalyticsRun Sample(int score, int plays, string status = "won", int copies = 1)
    {
        var analytics = Session();
        var powers = new List<PowerUp>();
        for (int i = 0; i < copies; i++)
        {
            var power = new HabitatPower(HabitatStyle.Shore, 0); powers.Add(power);
            analytics.Pick(power, powers, new[] { ItemType.Duck });
        }
        for (int i = 0; i < plays; i++)
        {
            analytics.BeginPlay(i == 0 ? 0 : score, 0, i);
            analytics.EndPlay(score, 0, i + 1, 20 - i);
        }
        analytics.FinishRound(status, score, score, 60, 0);
        return analytics.Data;
    }

    [Test]
    public void ExposureUsesWeightedMeansAndKeepsCopyCountsSeparate()
    {
        var tables = AnalyticsExport.Tables(new[] { Sample(100, 1), Sample(0, 9, "failed"), Sample(50, 1, copies: 2) });
        var exposure = tables["power-exposure"];
        var single = exposure.Skip(1).Single(row => row[2] == "1");
        Assert.That(single[3], Is.EqualTo("2"));
        Assert.That(single[4], Is.EqualTo("2"));
        Assert.That(single[5], Is.EqualTo("10"));
        Assert.That(single[6], Is.EqualTo("10"), "100 points / 10 total plays, not the unweighted average of 100 and 0.");
        Assert.That(single[8], Is.EqualTo("0.5"));
        Assert.That(exposure.Count, Is.EqualTo(3), "Two owned copies receive a separate comparison row.");
    }

    [Test]
    public void AbandonedAndActiveRoundsRemainInRawDataButNotPowerComparisons()
    {
        var abandoned = Sample(999, 1, "abandoned");
        var active = Sample(888, 1, "active");
        var tables = AnalyticsExport.Tables(new[] { abandoned, active, Sample(10, 1) });
        Assert.That(tables["rounds"].Count, Is.EqualTo(4));
        Assert.That(tables["plays"].Count, Is.EqualTo(4));
        Assert.That(tables["power-picks"].Count, Is.EqualTo(4));
        Assert.That(tables["power-exposure"][1][6], Is.EqualTo("10"));
    }

    [Test]
    public void ComparisonCohortsSeparateGoalsBudgetsAndObjectives()
    {
        var points = Session(); var blocks = Session(GoalType.BlocksCleared); var moves = Session(mode: FreePlayMode.LimitedMoves);
        Assert.That(points.Current.cohort, Is.Not.EqualTo(blocks.Current.cohort));
        Assert.That(points.Current.cohort, Is.Not.EqualTo(moves.Current.cohort));
        points.FinishRound("won", 400, 400, 20, 0);
        Begin(points, 2, 500, "Duck ONLY");
        Assert.That(points.Current.cohort, Does.Contain("target=500"));
        Assert.That(points.Current.cohort, Does.Contain("Duck ONLY"));
        var old = points.Data.id;
        points.Start(new FreePlayConfig(), "test-v2", "test");
        Assert.That(points.Data.id, Is.Not.EqualTo(old));
        Assert.That(points.Data.picks, Is.Empty);
        Assert.That(points.Data.rounds, Is.Empty);
    }

    [Test]
    public void CsvEscapesMultilineTextAndUsesInvariantNumbers()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var csv = AnalyticsExport.Csv(new[] { new[] { "duck, \"elephant\"\nnext", "=SUM(A1)", "normal" } });
            Assert.That(csv, Does.Contain("\"duck, \"\"elephant\"\"\nnext\""));
            Assert.That(csv, Does.Contain("\"'=SUM(A1)\""));
            var report = AnalyticsExport.Tables(new[] { Sample(21, 2) });
            Assert.That(report["rounds"][1][8], Is.EqualTo("10.5"));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    public void OfflineReportContainsAllDownloadsAndEscapesPowerNames()
    {
        var run = Sample(20, 2);
        run.picks[0].name = "<script>bad()</script>";
        var html = AnalyticsExport.Report(new[] { run });
        foreach (var name in new[] { "runs", "rounds", "plays", "power-picks", "power-exposure" })
            Assert.That(html, Does.Contain("download='" + name + ".csv'"));
        Assert.That(html, Does.Not.Contain("<script>bad()</script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;bad()&lt;/script&gt;"));
        Assert.That(html, Does.Contain("not that power&#39;s isolated contribution").Or.Contain("not that power's isolated contribution"));
    }
}
