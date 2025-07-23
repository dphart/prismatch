using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public int Score { get; private set; }
    public int TargetScore { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool TimerIsRunning { get; set; }
    public PlayerConfigs PlayerConfigs { get; private set; } = new PlayerConfigs();
    public Scorables Scorables { get; private set; }

    public List<PowerUp> ActivePowers { get; private set; } = new();

    public void RegisterPowerUp(PowerUp modifier)
    {
        ActivePowers.Add(modifier);
    }

    public void ClearScoreModifiers()
    {
        ActivePowers.Clear();
    }


    public event Action<int> OnScoreChanged;
    public event Action<float> OnTimeChanged;
    public event Action<int> OnTargetChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate GameState detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("GameState initialized.");
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTimer(float time)
    {
        TimeRemaining = time;
        TimerIsRunning = true;
    }


    public void SetTargetScore(int target)
    {
        TargetScore = target;
        OnTargetChanged?.Invoke(TargetScore);
    }

    private void Update()
    {
        if (!TimerIsRunning) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0)
        {
            TimeRemaining = 0;
            TimerIsRunning = false;
        }
        OnTimeChanged?.Invoke(TimeRemaining);
    }
    public void IncrementScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    public void ResetGame()
    {
        Score = 0;
        TargetScore = 0;
        TimerIsRunning = false;
        TimeRemaining = 0;

        OnScoreChanged?.Invoke(Score);
        OnTargetChanged?.Invoke(TargetScore);
        OnTimeChanged?.Invoke(TimeRemaining);

        ActivePowers.Clear(); // Assuming you're tracking powerups this way

        Debug.Log("Game state has been reset.");
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

