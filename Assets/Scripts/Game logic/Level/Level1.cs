using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level1 : LevelConfig
{
    public int GetTotalTime => 60;

    public int GetTotalRounds => 10;

    public int GetLevelNumber => 1;

    public int GetHeight(int round) =>7 ;


    public List<PowerUp> GetPowerUpsForRound(int round)
    {
        if(round == 1)
        {
            return new List<PowerUp> { new BasePowerUp(5), new BaseMultiPowerUp(1, 5), new BaseMultiPowerUp(2, 3) };
        }
        return PowerUpLibrary.All.ToList();
    }

    public int GetTargetScore(int round)
    {
        int baseScore = 200;
        float multiplier = Mathf.Pow(2, round - 1); // round 1 = x1, round 2 = x2, round 3 = x4...
        return Mathf.RoundToInt(baseScore * multiplier);
    }

    public int GetWidth(int round) => 7;

    List<BossPowers> LevelConfig.GetBossEffectsByRound(int round)
    {
        if(round == 2)
        {
            return new List<BossPowers> { new FirstItemsGivesNoPoints() };
        }
        else
        {
            return new List<BossPowers>();
        }
    }

    public string GetLevelDescription()
    {
        return "Beat the target score for all 10 rounds to win!";
    }

    
}
