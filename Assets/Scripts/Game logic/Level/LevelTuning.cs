using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BossRule { FirstAnimalBlocked, HalfPointsInitially }
[Serializable]
public class BossSchedule
{
    public int round;
    public BossRule rule;
}

[CreateAssetMenu(menuName = "Prismatch/Level Tuning")]
public sealed class LevelTuning : ScriptableObject
{
    [Min(3)] public int width = 7, height = 7;
    [Min(1)] public int rounds = 10, seconds = 60, baseTarget = 400;
    [Min(1)] public float targetGrowth = 1.35f;
    [Range(3, 7)] public int itemTypes = 7;
    public bool bossesPersist;
    [TextArea] public string description = "Swap any two animals. Clear connected groups of 3+ and beat all 10 targets.";
    [Tooltip("Empty allows all powers. Otherwise enter their display names.")]
    public string[] allowedPowerNames = Array.Empty<string>();
    public List<BossSchedule> bosses = new();
    [Tooltip("A match may satisfy any enabled rule. Overlapping matches clear as one group.")]
    public List<MatchRule> matchRules = new() { new MatchRule(MatchType.Snake, 3) };
    private static readonly Dictionary<int, LevelTuning> cache = new();

    public int Target(int round) => (int)Math.Min(int.MaxValue,
        Math.Round((double)Math.Max(1, baseTarget) * Math.Pow(Math.Max(1, targetGrowth), Math.Max(0, round - 1))));
    public List<PowerUp> Powers(int round) => round == 1 ? PowerUpLibrary.Starters() :
        PowerUpLibrary.GetAll().Where(p => allowedPowerNames.Length == 0 || allowedPowerNames.Contains(p.Name)).ToList();
    public List<MatchRule> MatchRules(int round) => MatchRule.Normalize(matchRules)
        .Select(rule => rule.Clone()).ToList();
    public BossPowerMap Bosses()
    {
        var map = new BossPowerMap();
        foreach (var group in bosses.GroupBy(b => b.round))
            map.AddPower(group.Key, group.Select(b => b.rule == BossRule.FirstAnimalBlocked
                ? (BossPowers)new FirstItemsGivesNoPoints() : new HalfPointsForFirstHalfOfLevel()).ToList());
        return map;
    }

    public static LevelTuning For(int level)
    {
        if (cache.TryGetValue(level, out var found) && found != null) return found;
        var tuning = Resources.Load<LevelTuning>($"Levels/Level{level}");
        if (tuning == null)
        {
            tuning = CreateInstance<LevelTuning>();
            tuning.hideFlags = HideFlags.DontSave;
            if (level == 2)
            {
                tuning.width = 8; tuning.seconds = 55; tuning.baseTarget = 500; tuning.targetGrowth = 1.4f;
                tuning.bosses.Add(new BossSchedule { round = 2, rule = BossRule.FirstAnimalBlocked });
                tuning.bosses.Add(new BossSchedule { round = 8, rule = BossRule.FirstAnimalBlocked });
            }
            else tuning.bosses.Add(new BossSchedule { round = 5, rule = BossRule.FirstAnimalBlocked });
            tuning.bosses.Add(new BossSchedule { round = 10, rule = BossRule.HalfPointsInitially });
        }
        cache[level] = tuning;
        return tuning;
    }
}
