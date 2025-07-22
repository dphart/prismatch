using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)] // Ensures it initializes early
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ScoreManager detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("✅ ScoreManager initialized.");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureScoreManagerExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("AutoCreated_ScoreManager");
            go.AddComponent<ScoreManager>();
            Debug.Log("⚠️ ScoreManager was missing. Auto-created.");
        }
    }

    public void HandleScoring(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0)
        {
            Debug.LogWarning("HandleScoring called with empty or null tile list.");
            return;
        }

        if (GameState.Instance == null)
        {
            Debug.LogError("GameState.Instance is null!");
            return;
        }

        if (GameState.Instance.PlayerConfigs == null)
        {
            Debug.LogError("PlayerConfigs is null on GameState.Instance!");
            return;
        }

        int basePerTile = GameState.Instance.PlayerConfigs.basePoints;
        int basePoints = tiles.Count * basePerTile;
        int modifiedPoints = basePoints;

        if (GameState.Instance.ActivePowers != null)
        {
            foreach (var power in GameState.Instance.ActivePowers)
            {
                modifiedPoints = power.ModifyScore(tiles, modifiedPoints);
            }
        }

        GameState.Instance.IncrementScore(modifiedPoints);
        Debug.Log($"✅ Scored {modifiedPoints} points for {tiles.Count} tiles.");
    }
}
