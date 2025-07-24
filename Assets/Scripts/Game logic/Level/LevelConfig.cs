using System.Collections.Generic;
using UnityEngine;

public interface LevelConfig 
{
    public List<PowerUp> GetPotentialPowerUps(int round);
    public int GetTotalTime { get; }
    public int GetHeight(int round);

    public int GetWidth(int round);
    public int GetTargetScore(int round);

    public int GetTotalRounds { get; }

    public int GetLevelNumber { get; }

}
