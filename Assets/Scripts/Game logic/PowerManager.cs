using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class PowerManager
{
    private readonly TimeProvider _timeProvider;
    private LevelInfoProvider _LevelInfoProvider;
    private ItemInfoProvider _ItemInfoProvider;

    public PowerManager(TimeProvider timeProvider, LevelInfoProvider levelInfoProvider, ItemInfoProvider itemInfoProvider)
    {
        _timeProvider = timeProvider;
        _LevelInfoProvider = levelInfoProvider;
        _ItemInfoProvider = itemInfoProvider;
    }
    private readonly List<PowerUp> _activePowers = new();
    private readonly List<BossPowers> _activeBossPowers = new();

    public void RegisterPower(PowerUp power)
    {
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;
        _activePowers.Add(power);
    }

    public void ClearPowers()
    {
        UnityEngine.Debug.Log($"Cleared {_activePowers.Count} powers");
        _activePowers.Clear();
        RegisterPower(new BasePowerUp());
    }

    public void RegisterBossPower(BossPowers power)
    {
        power.ItemInfoProvider = _ItemInfoProvider;
        power.LevelInfoProvider = _LevelInfoProvider;
        power.TimeProvider = _timeProvider;
        _activeBossPowers.Add(power);
    }

    public void ClearBossPowers()
    {
        UnityEngine.Debug.Log($"Cleared {_activeBossPowers.Count} powers");
        _activeBossPowers.Clear();
    }

    public IReadOnlyList<PowerUp> ActivePowers => _activePowers.AsReadOnly();
    public IReadOnlyList<PowerUp> BossPowers => _activeBossPowers.AsReadOnly();

}
