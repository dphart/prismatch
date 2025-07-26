using System.Collections.Generic;
using UnityEngine;

public class PowerManager
{
    private readonly TimeProvider _timeProvider;
    private readonly LevelInfoProvider _LevelInfoProvider;
    private readonly ItemInfoProvider _ItemInfoProvider;

    private readonly List<PowerUp> _activePowers = new();
    private readonly List<BossPowers> _activeBossPowers = new();

    public  List<PowerUp> ActivePowers() => _activePowers;
    public  List<BossPowers> ActiveBossPowers() => _activeBossPowers;

    // ✅ Reference to ActivePowerUpsUI (assign this when creating PowerManager)

    public PowerManager(TimeProvider timeProvider, LevelInfoProvider levelInfoProvider, ItemInfoProvider itemInfoProvider)
    {
        _timeProvider = timeProvider;
        _LevelInfoProvider = levelInfoProvider;
        _ItemInfoProvider = itemInfoProvider;
    }


    /// <summary>
    /// Registers a new power-up and tells UI to create a card.
    /// </summary>
    public void RegisterPower(PowerUp power)
    {
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;

        _activePowers.Add(power);

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

    /// <summary>
    /// Clears all powers and UI cards, then re-adds the base power.
    /// </summary>
    public void ClearPowers()
    {
        Debug.Log($"🧹 Cleared {_activePowers.Count} powers");
        _activePowers.Clear();

        RegisterPower(new BasePowerUp());    // Ensure we always have a base power
    }

    // -------- Boss Powers (UI probably won’t show these, but could) --------
    public void RegisterBossPower(BossPowers power)
    {
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;
        _activeBossPowers.Add(power);
    }

    public void ClearBossPowers()
    {
        Debug.Log($"🧹 Cleared {_activeBossPowers.Count} boss powers");
        _activeBossPowers.Clear();
    }
}
