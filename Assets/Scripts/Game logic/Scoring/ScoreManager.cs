using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ScoreResult
{
    public readonly Dictionary<PowerUp,int> BlockCredits = new();
    public readonly Dictionary<PowerUp,int> ExtraClears = new();
    public int Score, BasePoints, NextMultiplier = 1;
    public float Multiplier = 1;
    public string Reason;
    public readonly List<string> Contributions = new();
    public readonly List<PowerActivation> Activations = new();
    public void Activate(PowerUp power,string effect,bool progress=false) { Activations.Add(new PowerActivation(power,effect,progress)); }
}

public class ScoreManager
{
    public GoalType RewardGoal { get; set; } = GoalType.Score;
    private readonly PowerManager powerManager;
    public int nextMatchMulti = 1;
    public ScoreResult LastResult { get; private set; } = new();
    public LevelBonus LevelBonus { get; set; }
    public int LevelBasePointsPerTile { get; set; }
    public MatchInfo LevelMatchInfo { get; set; }
    public IReadOnlyList<ItemType> LevelAnimalPool { get; set; } = Array.Empty<ItemType>();
    // Base scoring is supplied by powers; the legacy constructor remains compatible.
    public ScoreManager(PlayerConfigs configs, PowerManager powerManager) { this.powerManager = powerManager; }
    public void Reset() { powerManager.AnimalHistory.Reset(); foreach (var power in powerManager.ActivePowers()) { power.OnRoundStarted(); power.QueuedBlockClears=0; } nextMatchMulti = 1; LastResult = new ScoreResult(); }

    public static int SaturatingAdd(int a, int b) => Clamp((long)a + b);
    public static int ApplyTileFactors(int score, IReadOnlyList<int> factors)
    {
        if (factors == null || factors.Count == 0) return 0;
        long total = 0; foreach (int factor in factors) total += Math.Max(1,factor);
        return Clamp((long)Math.Max(0,score)*total/factors.Count);
    }
    private static int Clamp(long value) => (int)Math.Max(0, Math.Min(int.MaxValue, value));
    public static int Multiply(int a, int b) => Clamp((long)a * b);
    public static int ApplyChargedTiles(int score, int tileCount, int chargedCount) => tileCount <= 0 ? 0 :
        Clamp((long)Math.Max(0,score) * (tileCount + (long)Math.Clamp(chargedCount,0,tileCount)) / tileCount);

    public int CalculateScore(List<Tile> tiles) => CalculateScore(tiles,null);
    public int CalculateScore(List<Tile> tiles,IReadOnlyDictionary<PowerUp,int> sweepRewards)
    {
        int score = CalculateScoreBeforeHistory(tiles,sweepRewards);
        if (tiles != null) powerManager.AnimalHistory.Record(tiles);
        return score;
    }

    private int CalculateScoreBeforeHistory(List<Tile> tiles,IReadOnlyDictionary<PowerUp,int> sweepRewards)
    {
        var result = new ScoreResult();
        LastResult = result;
        if (tiles == null || tiles.Count == 0)
        {
            result.Reason = "No tiles matched.";
            return 0;
        }
        // Blocked clears preserve queued bonuses and do not advance scoring-power counters.
        foreach (var power in powerManager.ActivePowers())
            if (!power.IsScoreValid(tiles)) { result.Activate(power,"Blocked"); result.Reason = $"Blocked by {power.Name}"; result.NextMultiplier = nextMatchMulti; return 0; }
        foreach (var boss in powerManager.ActiveBossPowers())
            if (!boss.IsScoreValid(tiles)) { result.Activate(boss,"Blocked"); result.Reason = $"Blocked by {boss.Name}"; result.NextMultiplier = nextMatchMulti; return 0; }

        long signedBasePoints = (long)Math.Max(0, LevelBasePointsPerTile) * tiles.Count;
        if (LevelBasePointsPerTile > 0) result.Contributions.Add($"Level base: +{LevelBasePointsPerTile} base / tile");
        double fractionalMultiplier = 0;
        int basePoints = 0, additiveMultiplier = 0, currentMultiplier = nextMatchMulti;
        nextMatchMulti = 1;
        foreach (var power in powerManager.ActivePowers())
        {
            power.ForGoal(RewardGoal);
            power.ConfigureSharedContent();
            float beforeBase=power.FlatBasePerTile, beforeMulti=power.FlatMultiplierBonus, beforeFactor=power.FlatMultiplierFactor;
            int beforeAnimal=(power as IconTypeStreakGivesBasePointsOrMulti)?.MatchedTiles??0;
            int beforeEarned=(power as SharedRulePower)?.EarnedActivations??0;
            int addedBase = power.AdditiveBaseMod(tiles);
            if(RewardGoal==GoalType.Score&&power is BlockClearPower&&sweepRewards!=null&&sweepRewards.TryGetValue(power,out int sweepBase))addedBase=SaturatingAdd(addedBase,sweepBase);
            int addedMulti = power.AdditiveMultiplierMod(tiles);
            float fractional = power.FractionalMultiplierMod(tiles);
            fractionalMultiplier += fractional;
            int immediate = power.CurrentMatchMultiplier(tiles);
            int queued = power.NextMatchMulti(tiles);
            if(power.UsesBlockReward)
                SharedPowerRewards.Resolve(power,tiles,addedBase,addedMulti+fractional,immediate,queued,result);
            var activation=PowerActivation.FromScoring(power,tiles.Count,addedBase,addedMulti+fractional,immediate,queued,
                beforeBase,beforeMulti,beforeFactor,beforeAnimal,beforeEarned);
            if(power.UsesBlockReward && activation!=null)
            {
                bool upgraded=power.FlatBasePerTile>beforeBase||power.FlatMultiplierBonus>beforeMulti||power.FlatMultiplierFactor>beforeFactor;
                activation=upgraded?new PowerActivation(power,"UPGRADE!"):activation.Progress?activation:null;
            }
            if(activation!=null)result.Activations.Add(activation);
            signedBasePoints += addedBase;
            additiveMultiplier = SaturatingAdd(additiveMultiplier, addedMulti);
            currentMultiplier = Multiply(currentMultiplier, Math.Max(1, immediate));
            nextMatchMulti = Multiply(nextMatchMulti, Math.Max(1, queued));
            if (addedBase != 0 || addedMulti != 0 || fractional != 0 || immediate > 1 || queued > 1)
                result.Contributions.Add($"{power.Name}:" + (addedBase != 0 ? $" +{addedBase} base" : "") +
                    (addedMulti + fractional != 0 ? $" +{addedMulti + fractional:0.##} multi" : "") + (immediate > 1 ? $" ×{immediate}" : "") +
                    (queued > 1 ? $" next ×{queued}" : ""));
        }
        var levelPower=powerManager.Characteristic(LevelBonus) ?? new LevelCharacteristicPower(LevelBonus);
        int animalMultiplier=levelPower.AnimalMultiplier(powerManager.AnimalHistory,LevelAnimalPool,
            tiles.Where(tile=>tile!=null&&tile.item!=null).Select(tile=>tile.item.ItemType));
        currentMultiplier=Multiply(currentMultiplier,animalMultiplier);
        if(animalMultiplier>1){result.Contributions.Add("Least matched animal: x2 points");result.Activate(levelPower,"x2");}
        basePoints = Clamp(signedBasePoints);
        foreach (var power in powerManager.ActivePowers())
        {
            int baseFactor = power.CurrentMatchBaseMultiplier(tiles);
            if (baseFactor <= 1) continue;
            if(power.UsesBlockReward&&power is AnimalDoublePower)result.BlockCredits[power]=Multiply(tiles.Count,baseFactor-1);
            basePoints = Multiply(basePoints, baseFactor);
            result.Contributions.Add($"{power.Name}: x{baseFactor} base points");
            result.Activate(power, $"x{baseFactor} base");
        }
        result.BasePoints = basePoints;
        result.Multiplier = (float)Math.Min(int.MaxValue, (1d + additiveMultiplier + fractionalMultiplier) * currentMultiplier);
        result.NextMultiplier = nextMatchMulti;
        int score = Clamp((long)Math.Min(int.MaxValue, Math.Floor((double)basePoints * result.Multiplier)));
        int chargedCount = tiles.FindAll(tile => tile != null && tile.IsCharged).Count;
        if (chargedCount > 0)
        {
            score = ApplyChargedTiles(score,tiles.Count,chargedCount);
            result.Contributions.Add($"Lightning: {chargedCount}/{tiles.Count} tiles worth x2");
            result.Activate(powerManager.Characteristic(LevelBonus.None,true) ?? new LevelCharacteristicPower(LevelBonus.None,true),"Charged tiles");
        }
        int factorTotal = tiles.ConvertAll(tile => tile != null ? tile.LevelPointFactor : 1).FindAll(factor => factor > 0).Sum();
        if (factorTotal > tiles.Count)
        {
            score = ApplyTileFactors(score,tiles.ConvertAll(tile => tile != null ? tile.LevelPointFactor : 1));
            result.Activate(levelPower,"Bonus tiles");
            result.Contributions.Add(LevelBonus == LevelBonus.GardenPlots ? "Garden: planted spaces worth x2" : "Spotlight: active animal worth x3");
        }
        int levelPercent = levelPower.RewardPercent(
            tiles.ConvertAll(tile => new BoardModel.Cell(tile.x, tile.y)), LevelMatchInfo?.wasUserInitiatedMatch() != false);
        if (levelPercent != 100)
        {
            if(score>0)result.Activate(levelPower,levelPercent+"%");
            score = ChallengeBoss.ScaleReward(score, levelPercent);
            result.Contributions.Add(levelPercent == 0 ? "Chain Reaction: initial matches score 0" : $"Level bonus: +{levelPercent - 100}% points");
        }
        if(LevelMatchInfo is Board campaignBoard && campaignBoard.CampaignMatchPercent!=100) {
            if(score>0)result.Activate(levelPower,"x"+(campaignBoard.CampaignMatchPercent/100));
            score=campaignBoard.Strategy!=null?campaignBoard.StrategyMatchReward.Apply(score):ChallengeBoss.ScaleReward(score,campaignBoard.CampaignMatchPercent);
            result.Contributions.Add($"{campaignBoard.CampaignBonus}: x{campaignBoard.CampaignMatchPercent/100f:0.##} points");
        }
        foreach (var boss in powerManager.ActiveBossPowers())
        {
            int before = score;
            score = Math.Max(0, boss.ModifyAndReturnScore(tiles, score));
            if(before!=score)result.Activate(boss,"Score effect");
            if (before != score) result.Contributions.Add($"{boss.Name}: {before} → {score}");
        }
        foreach (var power in powerManager.ActivePowers().OrderBy(p=>p is RosterTradeoffPower?1:0))
        {
            int before = score;
            score = Math.Max(0, power.AdjustFinalScore(tiles, score));
            if (before != score) {
                result.Contributions.Add($"{power.Name}: {before} -> {score}");
                if(power.HasConditionalTrigger)result.Activate(power,$"{score-before:+0;-0} points");
            }
        }
        result.Score = score;
        if (basePoints == 0) result.Reason = "No base points for this match — choose a base-point power.";
        else if (score == 0) result.Reason = levelPercent == 0 ? "Initial matches score 0. Set up a cascade for 4x points." : "Boss penalty reduced this match to zero.";
        return score;
    }
}

public sealed class FlatScoreStats
{
    public float BasePerTile, Multiplier = 1;
    public string BaseDetails, MultiplierDetails;
    public static FlatScoreStats Read(IEnumerable<PowerUp> powers, int queuedMultiplier = 1, int levelBasePointsPerTile = 0)
    {
        var result = new FlatScoreStats();
        var bases = new System.Text.StringBuilder();
        result.BasePerTile = Math.Max(0, levelBasePointsPerTile);
        if (levelBasePointsPerTile > 0) bases.AppendLine($"Level base: +{levelBasePointsPerTile} per tile");
        var multis = new System.Text.StringBuilder("Starting multiplier: x1\n");
        var baseConditions = new System.Text.StringBuilder();
        var multiConditions = new System.Text.StringBuilder();
        float additive = 1, factor = 1;
        foreach (var power in powers)
        {
            result.BasePerTile += power.FlatBasePerTile;
            additive += power.FlatMultiplierBonus;
            factor *= power.FlatMultiplierFactor;
            if (power.FlatBasePerTile != 0) bases.AppendLine($"{power.Name}: {power.FlatBasePerTile:+0.##;-0.##;0} per tile");
            if (power.FlatMultiplierBonus != 0) multis.AppendLine($"{power.Name}: +{power.FlatMultiplierBonus:0.##}");
            if (power.FlatMultiplierFactor != 1) multis.AppendLine($"{power.Name}: x{power.FlatMultiplierFactor:0.##}");
            if (!string.IsNullOrEmpty(power.ConditionForStat(false))) baseConditions.AppendLine($"\n<b>{power.Name}</b>\n{power.ConditionForStat(false)}");
            if (!string.IsNullOrEmpty(power.ConditionForStat(true))) multiConditions.AppendLine($"\n<b>{power.Name}</b>\n{power.ConditionForStat(true)}");
        }
        result.BasePerTile = Math.Max(0, result.BasePerTile);
        result.Multiplier = Math.Max(0, additive * factor);
        if (bases.Length == 0) bases.AppendLine("No flat base-point powers yet.");
        string Conditional(System.Text.StringBuilder list) => "\n<b>CONDITIONS & GROWTH</b>\n" + (list.Length == 0 ? "None." : list.ToString());
        result.BaseDetails = $"<b>FLAT BASE: {result.BasePerTile:0.##} PER TILE</b>\n\n" + bases +
            "\nMultiplied by the number of matched tiles. Fractional base bonuses round down within each power." + Conditional(baseConditions);
        result.MultiplierDetails = $"<b>FLAT MULTIPLIER: x{result.Multiplier:0.##}</b>\n\n" + multis +
            "\nAdditive bonuses combine first, then percentage/factor bonuses multiply the total. Final-score effects may round separately." +
            (queuedMultiplier > 1 ? $"\n\n<b>NEXT MATCH ONLY: x{queuedMultiplier}</b>\nNot included in the flat total." : "") + Conditional(multiConditions);
        return result;
    }
}

/// <summary>Records resolved effects; feedback never calls a power's scoring or progress hooks.</summary>
public sealed class PowerActivation
{
    public readonly PowerUp Power;
    public readonly string Effect;
    public readonly bool Progress;
    public readonly PowerTriggerDisplay Display;
    public PowerActivation(PowerUp power,string effect,bool progress=false,PowerTriggerDisplay? display=null)
    {
        Power=power;Effect=effect;Progress=progress;
        Display=power?.HasConditionalTrigger==true ? display ?? power.DisplayWhenTriggered : PowerTriggerDisplay.None;
    }
    public static PowerActivation FromScoring(PowerUp power,int tiles,int basePoints,float multi,int factor,int queued,
        float beforeBase,float beforeMulti,float beforeFactor,int beforeAnimal=0,int beforeEarned=0)
    {
        if(!power.HasConditionalTrigger)return null;
        if(power is EdgeBalancePower balance) {
            float change=balance.FlatBasePerTile-beforeBase;
            return Math.Abs(change)<.001f?null:new PowerActivation(power,$"{change:+0;-0} base / tile (now +{balance.StoredBase})");
        }
        if(power.FlatBasePerTile>beforeBase || power.FlatMultiplierBonus>beforeMulti || power.FlatMultiplierFactor>beforeFactor ||
            power is SharedRulePower shared && shared.EarnedActivations>beforeEarned)
            return new PowerActivation(power,"UPGRADE!");
        if(power is IconTypeStreakGivesBasePointsOrMulti animal) {
            if(animal.MatchedTiles==beforeAnimal)return null;
            return new PowerActivation(power,$"{animal.MatchedTiles%animal.RequiredTiles}/{animal.RequiredTiles} matched",true);
        }

        if(power is SharedRulePower persistent && persistent.Permanent)return null;
        var effects=new List<string>();
        float conditionalBase=basePoints-beforeBase*tiles,conditionalMulti=multi-beforeMulti;
        if(Math.Abs(conditionalBase)>.001f)effects.Add($"{conditionalBase:+0.##;-0.##} base");
        if(Math.Abs(conditionalMulti)>.001f)effects.Add($"{conditionalMulti:+0.##;-0.##} multiplier");
        if(factor>Math.Max(1,beforeFactor))effects.Add($"x{factor}");
        if(queued>1)effects.Add($"Next x{queued}");
        return effects.Count==0?null:new PowerActivation(power,string.Join(" / ",effects));
    }
}

/// <summary>Keep every distinct power from a move; do not replace earlier activations.</summary>
public sealed class PowerFountainQueue
{
    private readonly Queue<PowerActivation> pending = new();
    private readonly HashSet<Guid> seen = new();
    private int? move;
    public int Count => pending.Count;
    public void Enqueue(int moveId,IEnumerable<PowerActivation> activations)
    {
        if(move!=moveId) { move=moveId;seen.Clear(); }
        foreach(var activation in activations)
            if(activation?.Power!=null && (activation.Display & PowerTriggerDisplay.Icon)!=0 && seen.Add(activation.Power.Id))
                pending.Enqueue(activation);
    }
    public PowerActivation Dequeue() => pending.Dequeue();
    public void Clear() { pending.Clear();seen.Clear();move=null; }
}
