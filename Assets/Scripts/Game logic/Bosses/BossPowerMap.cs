using System.Collections.Generic;
using UnityEngine;

public class BossPowerMap
{
    Dictionary<int, List<BossPowers>> PowerMap =  new Dictionary<int, List<BossPowers>>();

    public void AddPower(int round, List<BossPowers> powers)
    {
        PowerMap[round] = powers;
    }

    public void RemovePower(int round)
    {
        PowerMap.Remove(round);
    }

    public List<BossPowers> GetBossPowersForRound(int round)
    {
        return PowerMap.ContainsKey(round) ? PowerMap[round] : new List<BossPowers>();
    }



}
