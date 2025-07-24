using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public PlayerConfigs PlayerConfigs { get; private set; } = new();
    public Scorables Scorables { get; private set; } = new();

    public LevelConfig CurrentLevel { get; private set; } = new Level1();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

public class PlayerConfigs
{
    public int rows = 5;
    public int columns = 5;
    public int baseMulti = 1;
    public int basePoints = 5;

}

public class Scorables
{
    public int requiredInARow = 3;
    public int maximumInARow = 20;
    public List<ScoringDirection> scoringMethods = Enum.GetValues(typeof(ScoringDirection))
                                               .Cast<ScoringDirection>()
                                               .ToList();
    public List<BlockEntryDirections> blockEntryDirections = Enum.GetValues(typeof(BlockEntryDirections)).Cast<BlockEntryDirections>().ToList();
}

public enum ScoringDirection  { Horizontal, Vertical, Diagonol, Snake,  }
public enum BlockEntryDirections { Pop, Left, Right, Top, Bottom }

