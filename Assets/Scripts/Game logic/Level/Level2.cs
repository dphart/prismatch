using System.Collections.Generic;

public class Level2 : LevelConfig
{
    private LevelTuning Tuning => LevelTuning.For(GetLevelNumber);
    public int GetLevelNumber => 2;
    public int GetTotalTime => Tuning.seconds;
    public int GetTotalRounds => Tuning.rounds;
    public int GetWidth(int round) => Tuning.width;
    public int GetHeight(int round) => Tuning.height;
    public int GetTargetScore(int round) => Tuning.Target(round);
    public string GetLevelDescription() => Tuning.description;
    public bool BossPowersPersist => Tuning.bossesPersist;
    public List<PowerUp> GetPowerUpsForRound(int round) => Tuning.Powers(round);
    public BossPowerMap GetBossPowersMap() => Tuning.Bosses();
    public List<MatchRule> GetMatchRules(int round) => Tuning.MatchRules(round);
    public LevelConfig CloneNew() => new Level2();
}
