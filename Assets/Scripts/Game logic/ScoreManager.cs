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

        if (GameState.Instance?.PlayerConfigs == null)
        {
            Debug.LogError("GameState.Instance or PlayerConfigs is null!");
            return;
        }

        var activePowers = GameState.Instance.ActivePowers ?? new List<PowerUp>();
        int basePerTile = GameState.Instance.PlayerConfigs.basePoints;
        int basePoints = 0;

        int additiveBase = 0;
        int baseMultiplier = 1;
        int additiveMultiplier = 0;
        int multMultiplier = 1;

        var breakdown = new System.Text.StringBuilder();

        // Collect breakdown, but calculate first
        foreach (var power in activePowers)
        {
            int add = power.AdditiveBaseMod(tiles);
            if (add != 0) breakdown.AppendLine($"➕ {power.Name} adds {add} to base");
            additiveBase += add;
        }

        foreach (var power in activePowers)
        {
            int mult = power.MultiplicativeBaseMod(tiles);
            if (mult != 1) breakdown.AppendLine($"✖️ {power.Name} multiplies base by {mult}");
            baseMultiplier *= mult;
        }

        foreach (var power in activePowers)
        {
            int addMult = power.AdditiveMultiplierMod(tiles);
            if (addMult != 0) breakdown.AppendLine($"➕ {power.Name} adds {addMult}x to multiplier");
            additiveMultiplier += addMult;
        }

        foreach (var power in activePowers)
        {
            int mulMult = power.MultiplicativeMultiMod(tiles);
            if (mulMult != 1) breakdown.AppendLine($"✖️ {power.Name} multiplies multiplier by {mulMult}");
            multMultiplier *= mulMult;
        }

        int finalBase = (basePoints + additiveBase) * baseMultiplier;
        float finalMultiplier = (1 + additiveMultiplier) * multMultiplier;
        int finalScore = Mathf.RoundToInt(finalBase * finalMultiplier);

        GameState.Instance.IncrementScore(finalScore);

        // Print final score first
        var fullLog = new System.Text.StringBuilder();
        fullLog.AppendLine($"✅ Final Score: {finalBase} × {finalMultiplier} = {finalScore}");

        // Now append breakdown details
        fullLog.AppendLine();
        fullLog.AppendLine($"You have {activePowers.Count} active powers");
        fullLog.AppendLine($"📦 Base points before modifiers: {basePoints} ({tiles.Count} tiles × {basePerTile} per tile)");
        fullLog.AppendLine($"🔹 Total additive base: {additiveBase}");
        fullLog.AppendLine($"🔸 Total base multiplier: ×{baseMultiplier}");
        fullLog.AppendLine($"💠 Final base after all base mods: {finalBase}");
        fullLog.AppendLine($"🔹 Total additive multiplier: +{additiveMultiplier}");
        fullLog.AppendLine($"🔸 Total multiplier multiplier: ×{multMultiplier}");
        fullLog.AppendLine($"💥 Final multiplier: (1 + {additiveMultiplier}) × {multMultiplier} = {finalMultiplier}");
        fullLog.AppendLine();
        fullLog.Append(breakdown); // All individual power-up entries

        Debug.Log(fullLog.ToString());
    }




}
