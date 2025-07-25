using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public interface LevelInfoProvider 
{
    public bool isBossLevel { get; }
    public int currentRound {  get; }
    public int maxRounds {  get; }
    public List<Item> IconList { get; }

}
