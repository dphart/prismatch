using System.Collections.Generic;
using UnityEngine;

public interface LevelConfig 
{
    public List<PowerUp> GetPowerUpsForRound(int round);

    public int GetTotalTime { get; }
    public int GetHeight(int round);

    public int GetWidth(int round);
    public int GetTargetScore(int round);

    public int GetTotalRounds { get; }

    public int GetLevelNumber { get; }

    public List<BossPowers> GetBossEffectsByRound(int round);

    public string GetLevelDescription();


}
