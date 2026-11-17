using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerManager
{
    private readonly TimeProvider _timeProvider;
    private readonly LevelInfoProvider _LevelInfoProvider;
    private readonly ItemInfoProvider _ItemInfoProvider;
    private readonly MatchInfo _MatchInfo;

    public List<LevelCharacteristicPower> LevelPowers { get; } = new();
    public void ConfigureLevelPowers(CampaignLevel level)
    {
        LevelPowers.Clear();
        if(level == null)return;
        if(level.Bonus != LevelBonus.None)LevelPowers.Add(new LevelCharacteristicPower(level.Bonus));
        if(level.LightningLines)LevelPowers.Add(new LevelCharacteristicPower(LevelBonus.None,true));
    }
    public LevelCharacteristicPower Characteristic(LevelBonus bonus, bool lightning=false) =>
        LevelPowers.FirstOrDefault(p=>p.Bonus==bonus&&p.Lightning==lightning);

    public RoundAnimalHistory AnimalHistory { get; } = new();
    private readonly List<PowerUp> _activePowers = new();
    private readonly List<BossPowers> _activeBossPowers = new();

    public  List<PowerUp> ActivePowers() => _activePowers;
    public  List<BossPowers> ActiveBossPowers() => _activeBossPowers;

    // ✅ Reference to ActivePowerUpsUI (assign this when creating PowerManager)

    public PowerManager(TimeProvider timeProvider, LevelInfoProvider levelInfoProvider, ItemInfoProvider itemInfoProvider, MatchInfo matchInfo = null)
    {
        _timeProvider = timeProvider;
        _LevelInfoProvider = levelInfoProvider;
        _ItemInfoProvider = itemInfoProvider;
        _MatchInfo = matchInfo;
    }


    /// <summary>
    /// Registers a new power-up and tells UI to create a card.
    /// </summary>
    public void RegisterPower(PowerUp power)
    {
        if (power == null) return;
        power.ConfigureSharedContent();
        power.AnimalHistory = AnimalHistory;
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;
        power.MatchInfo = _MatchInfo ?? Board.Instance;

        _activePowers.Add(power);
        power.Apply(GameState.Instance);

        Debug.Log($"✅ Added power: {power.Name}");
    }

    /// <summary>
    /// Removes a power-up and tells UI to remove its card.
    /// </summary>
    public void RemovePower(PowerUp power)
    {
        if (_activePowers.Contains(power))
        {
            _activePowers.Remove(power);
            Debug.Log($"❌ Removed power: {power.Name}");
        }
    }

    public void RemoveMostRecentPower()
    {
        if (_activePowers.Count > 0) RemovePower(_activePowers.Last());
    }

    /// <summary>
    /// Clears all powers and UI cards, then re-adds the base power.
    /// </summary>
    public void ClearPowers()
    {
        Debug.Log($"🧹 Cleared {_activePowers.Count} powers");
        _activePowers.Clear();

    }

    // -------- Boss Powers (UI probably won’t show these, but could) --------
    public void RegisterBossPower(BossPowers power)
    {
        power.AnimalHistory = AnimalHistory;
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;
        power.MatchInfo = _MatchInfo ?? Board.Instance;
        _activeBossPowers.Add(power);
        power.Apply(GameState.Instance);
    }

    public void ClearBossPowers()
    {
        Debug.Log($"🧹 Cleared {_activeBossPowers.Count} boss powers");
        _activeBossPowers.Clear();
    }
}
