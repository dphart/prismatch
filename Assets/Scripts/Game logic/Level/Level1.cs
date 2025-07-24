using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Level1 : LevelConfig
{
    public int GetTotalTime => 60;

    public int GetTotalRounds => 10;

    public int GetLevelNumber => 1;

    public int GetHeight(int round) => 6;

    public List<PowerUp> GetPotentialPowerUps(int round)
    {
        return PowerUpLibrary.All.ToList();
    }

    public int GetTargetScore(int round)
    {
        var baseScore = 200;
        return baseScore * round; 
    }

    public int GetWidth(int round) => 6;
    
}
