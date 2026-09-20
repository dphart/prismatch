using NUnit.Framework;

[TestFixture]
public class RoundAndTimerTests
{
    [Test]
    public void Timer_ClampsPenaltyAndExpiresOnlyOnce()
    {
        var timer = new TimerController(); int expired = 0;
        timer.OnExpired += () => expired++;
        timer.Reset(2); timer.Start(); timer.ApplyPenalty(10); timer.Tick(1);
        Assert.AreEqual(0, timer.Remaining); Assert.AreEqual(1, expired);
    }

    [Test]
    public void ScoreSaturation_DoesNotOverflow()
    {
        Assert.AreEqual(int.MaxValue, ScoreManager.SaturatingAdd(int.MaxValue, 100));
    }

    [Test]
    public void LevelTuning_GrowthIsMonotonicAndBounded()
    {
        var tuning = UnityEngine.ScriptableObject.CreateInstance<LevelTuning>();
        tuning.baseTarget = int.MaxValue; tuning.targetGrowth = 2;
        Assert.AreEqual(int.MaxValue, tuning.Target(10));
        UnityEngine.Object.DestroyImmediate(tuning);
    }

    [Test]
    public void LevelTwo_ExposesItsConfiguredBossSchedule()
    {
        Assert.That(new Level2().GetBossPowersMap().GetBossPowersForRound(2), Is.Not.Empty);
    }
}
