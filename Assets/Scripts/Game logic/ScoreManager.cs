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

    public int nextMatchMulti = 1;

    public int CalculateScore(List<Tile> tiles)
    {
        if (tiles == null || tiles.Count == 0)
        {
            Debug.LogWarning("HandleScoring called with empty or null tile list.");
            return 0;
        }
        foreach (var power in powerManager.ActivePowers())
        {
            if (!power.IsScoreValid(tiles))
            {
                Debug.Log($"⛔️ Score invalidated by {power.Name}");
                return 0;
            }
        }
        foreach (var power in powerManager.ActiveBossPowers())
        {
            if (!power.IsScoreValid(tiles))
            {
                Debug.Log($"⛔️ Score invalidated by Boss Power {power.Name}");
                return 0;
            }
        }



        int additiveBase = 0;
        int baseMultiplier = 1;
        int additiveMultiplier = 0;
        int multMultiplier = nextMatchMulti;
        nextMatchMulti = 1;

        var breakdown = new StringBuilder();
        var powers = powerManager.ActivePowers();

        bool hasScoringPower = false;

        foreach (var power in powers)
        {
            int add = power.AdditiveBaseMod(tiles);
            if (add != 0) breakdown.AppendLine($"➕ {power.Name} adds {add} to base");
            additiveBase += add;
            if (add != 0) hasScoringPower = true;
        }

    
        foreach (var power in powers)
        {
            int addMult = power.AdditiveMultiplierMod(tiles);
            if (addMult != 0) breakdown.AppendLine($"➕ {power.Name} adds {addMult}x to multiplier");
            additiveMultiplier += addMult;
            if (addMult != 0) hasScoringPower = true;
        }

        foreach (var power in powers)
        {
            int mulMult = power.NextMatchMulti(tiles);
            breakdown.AppendLine($"✖️ {power.Name} multiplies next match by by {mulMult}, was User initiated: {power.MatchInfo.wasUserInitiatedMatch()}");
            nextMatchMulti *= mulMult;
            if (mulMult != 1) hasScoringPower = true;
        }

        // NEW: If no power is modifying anything, return 0
        if (!hasScoringPower)
        {
            Debug.Log("⚠️ No scoring powers active — 0 points awarded.");
            return 0;
        }

        int finalBase = (additiveBase) * baseMultiplier;
        float finalMultiplier = (1 + additiveMultiplier) * multMultiplier;
        int finalScore = Mathf.RoundToInt(finalBase * finalMultiplier);

        // Logging breakdown
        var fullLog = new StringBuilder();
        fullLog.AppendLine($"✅ Final Score: {finalBase} × {finalMultiplier} = {finalScore}");
        fullLog.AppendLine();
        fullLog.AppendLine($"You have {powers.Count} active powers");
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
