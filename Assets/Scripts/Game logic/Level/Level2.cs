using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level2 : LevelConfig
{
    public int GetTotalTime => 60;

    public int GetTotalRounds => 10;

    public int GetLevelNumber => 2;

    public int GetHeight(int round) => 7;

    public string GetLevelDescription()
    {
        return "Beat the target score for all 10 rounds to advance!";
    }

    public List<PowerUp> GetPowerUpsForRound(int round)
    {
        return PowerUpLibrary.All.ToList();
    }

    public int GetTargetScore(int round)
    {
        int baseScore = 250;
        float multiplier = Mathf.Pow(2, round - 1); // round 1 = x1, round 2 = x2, round 3 = x4...
        return Mathf.RoundToInt(baseScore * multiplier);
    }

    public int GetWidth(int round) => 7;


    public Dictionary<int, List<BossPowers>> GetBossPowersMap()
    {
        return new Dictionary<int, List<BossPowers>>
        {
            { 2, new List<BossPowers> {new FirstItemsGivesNoPoints() } },

        };
    }

    BossPowerMap LevelConfig.GetBossPowersMap()
    {
        return new BossPowerMap();
    }

    public LevelConfig CloneNew()
    {
        return new Level2();
    }
}
