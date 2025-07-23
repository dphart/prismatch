using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ScoreManager
{
    private readonly PlayerConfigs playerConfigs;
    private readonly PowerManager powerManager;

    public ScoreManager(PlayerConfigs configs, PowerManager powerManager)
    {
        this.playerConfigs = configs;
        this.powerManager = powerManager;
    }

    public int CalculateScore(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0)
        {
            Debug.LogWarning("HandleScoring called with empty or null tile list.");
            return 0;
        }

        int basePerTile = playerConfigs.basePoints;
        int basePoints = tiles.Count * basePerTile;

        int additiveBase = 0;
        int baseMultiplier = 1;
        int additiveMultiplier = 0;
        int multMultiplier = 1;

        var breakdown = new StringBuilder();

        var powers = powerManager.ActivePowers;

        foreach (var power in powers)
        {
            int add = power.AdditiveBaseMod(tiles);
            if (add != 0) breakdown.AppendLine($"➕ {power.Name} adds {add} to base");
            additiveBase += add;
        }

        foreach (var power in powers)
        {
            int mult = power.MultiplicativeBaseMod(tiles);
            if (mult != 1) breakdown.AppendLine($"✖️ {power.Name} multiplies base by {mult}");
            baseMultiplier *= mult;
        }

        foreach (var power in powers)
        {
            int addMult = power.AdditiveMultiplierMod(tiles);
            if (addMult != 0) breakdown.AppendLine($"➕ {power.Name} adds {addMult}x to multiplier");
            additiveMultiplier += addMult;
        }

        foreach (var power in powers)
        {
            int mulMult = power.MultiplicativeMultiMod(tiles);
            if (mulMult != 1) breakdown.AppendLine($"✖️ {power.Name} multiplies multiplier by {mulMult}");
            multMultiplier *= mulMult;
        }

        int finalBase = (basePoints + additiveBase) * baseMultiplier;
        float finalMultiplier = (1 + additiveMultiplier) * multMultiplier;
        int finalScore = Mathf.RoundToInt(finalBase * finalMultiplier);

        // Logging breakdown
        var fullLog = new StringBuilder();
        fullLog.AppendLine($"✅ Final Score: {finalBase} × {finalMultiplier} = {finalScore}");
        fullLog.AppendLine();
        fullLog.AppendLine($"You have {powers.Count} active powers");
        fullLog.AppendLine($"📦 Base points: {basePoints} ({tiles.Count} × {basePerTile})");
        fullLog.AppendLine($"🔹 Additive base: {additiveBase}");
        fullLog.AppendLine($"🔸 Base multiplier: ×{baseMultiplier}");
        fullLog.AppendLine($"💠 Final base: {finalBase}");
        fullLog.AppendLine($"🔹 Additive multiplier: +{additiveMultiplier}");
        fullLog.AppendLine($"🔸 Multiplier multiplier: ×{multMultiplier}");
        fullLog.AppendLine($"💥 Final multiplier: (1 + {additiveMultiplier}) × {multMultiplier} = {finalMultiplier}");
        fullLog.AppendLine();
        fullLog.Append(breakdown);

        Debug.Log(fullLog.ToString());

        return finalScore;
    }
}
