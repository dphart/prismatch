using System;
using System.Collections.Generic;

public class RoundContext
{
    public int RoundNumber { get; }
    public int TargetScore { get; }
    public int RoundTime { get; }
    public List<BossPowers> BossPowers { get; }
    public bool BossPowersPersist { get; }

    public RoundContext(int roundNumber, int targetScore, int roundTime, List<BossPowers> bossPowers, bool bossPowersPersist)
    {
        RoundNumber = roundNumber;
        TargetScore = targetScore;
        RoundTime = roundTime;
        BossPowers = bossPowers ?? new List<BossPowers>();
        BossPowersPersist = bossPowersPersist;
    }
}

/// <summary>
/// Encapsulates round progression and level-derived data (target score, timer, boss powers).
/// </summary>
public class RoundManager
{
    private readonly Func<LevelConfig> _levelProvider;
    public int CurrentRound { get; private set; }

    private LevelConfig Level => _levelProvider();

    public RoundManager(Func<LevelConfig> levelProvider)
    {
        _levelProvider = levelProvider;
    }

    /// <summary>
    /// Begins a round, optionally incrementing the round counter.
    /// </summary>
    public RoundContext BeginRound(bool incrementRound)
    {
        if (incrementRound)
        {
            CurrentRound++;
        }
        else if (CurrentRound == 0)
        {
            CurrentRound = 1; // Ensure we start at round 1 if uninitialized
        }

        var target = Level.GetTargetScore(CurrentRound);
        var time = Level.GetTotalTime;
        var bossPowers = Level.GetBossPowersMap().GetBossPowersForRound(CurrentRound);
        var persistBossPowers = Level.BossPowersPersist;

        return new RoundContext(CurrentRound, target, time, bossPowers, persistBossPowers);
    }

    public void ResetRounds()
    {
        CurrentRound = 0;
    }
}
