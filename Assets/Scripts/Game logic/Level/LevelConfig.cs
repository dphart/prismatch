using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public interface LevelConfig 
{
    public List<PowerUp> GetPowerUpsForRound(int round);

    public BossPowerMap GetBossPowersMap();

    public int GetTotalTime { get; }
    public int GetHeight(int round);

    public int GetWidth(int round);
    public int GetTargetScore(int round);

    public int GetTotalRounds { get; }

    public int GetLevelNumber { get; }

    public string GetLevelDescription();

    public LevelConfig CloneNew();


}
