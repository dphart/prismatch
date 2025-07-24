using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class PowerManager
{
    private readonly TimeProvider _timeProvider;

    public PowerManager(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    private readonly List<PowerUp> _activePowers = new();

    public void RegisterPower(PowerUp power)
    {
        _activePowers.Add(power);
    }

    public void ClearPowers()
    {
        UnityEngine.Debug.Log($"Cleared {_activePowers.Count} powers");
        _activePowers.Clear();
        RegisterPower(new BasePowerUp());
    }

    public IReadOnlyList<PowerUp> ActivePowers => _activePowers.AsReadOnly();
}
