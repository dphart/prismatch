using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PowerUp
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public TimeProvider TimeProvider;
    public LevelInfoProvider LevelInfoProvider;
    public ItemInfoProvider ItemInfoProvider;
    public MatchInfo MatchInfo;
    public string Name { get; protected set; }
    public string PowerUpDescription { get; protected set; }
    public Sprite Icon { get; protected set; }

    // ✅ Each subclass must return a description (static or dynamic)
    public abstract string GetCurrentPowerDescription();

    public abstract void Apply(GameState state);
    public virtual void Tick(GameState state) { }
    public virtual int AdditiveBaseMod(List<Tile> tiles) => 0;
    public virtual int AdditiveMultiplierMod(List<Tile> tiles) => 0;
    public virtual int NextMatchMulti(List<Tile> tiles) => 1;
    public virtual bool IsScoreValid(List<Tile> tiles) => true;

    public abstract PowerUp Clone();
}

// ----------------- INDIVIDUAL POWER-UPS -----------------

public class FlexibleMatchPower : PowerUp
{
    public int Increase { get; private set; }

    public FlexibleMatchPower(int amount)
    {
        Name = "More Power!";
        PowerUpDescription = $"Receive +{amount} per tile";
        Increase = amount;
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * Increase;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FlexibleMatchPower(Increase);
}

public class BasePowerUp : PowerUp
{
    public BasePowerUp()
    {
        Name = "Base Scoring";
        PowerUpDescription = "Each tile match gives base points.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * GameState.Instance.PlayerConfigs.basePoints;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new BasePowerUp();
}

public class ComboCrazePower : PowerUp
{
    private int comboCount = 0;
    private int plusMulti = 1;
    private int successfulCombosRequired = 5;

    public ComboCrazePower()
    {
        Name = "Combo Craze";
        PowerUpDescription = $"{plusMulti} multiplier every {successfulCombosRequired} successful combos.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{plusMulti} multiplier every {successfulCombosRequired} combos. Current: {comboCount}/{successfulCombosRequired} combos.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        comboCount++;
        return comboCount / successfulCombosRequired;
    }

    public override void Apply(GameState state) => comboCount = 0;
    public override PowerUp Clone() => new ComboCrazePower();
}

public class BasePerTile : PowerUp
{
    private double turnMatches = 0;
    private double basePointModifier = 0.1;

    public BasePerTile()
    {
        Name = "Momentum Bonus";
        PowerUpDescription = $"+{basePointModifier} base per tile matched.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Current bonus: +{turnMatches:F1} per tile.";

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        turnMatches += basePointModifier;
        return (int)(tiles.Count * turnMatches);
    }

    public override void Apply(GameState state) => turnMatches = 0;
    public override PowerUp Clone() => new BasePerTile();
}

public class BaseForExactlyThree : PowerUp
{
    private int exactMatchNumber = 3;
    private int basePointsAdded = 5;

    public BaseForExactlyThree()
    {
        Name = "Precision Points";
        PowerUpDescription = $"{basePointsAdded} base points if match is exactly {exactMatchNumber}.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) =>
        tiles.Count == exactMatchNumber ? basePointsAdded * tiles.Count : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new BaseForExactlyThree();
}

public class FourExactlyPower : PowerUp
{
    private int exactMatchNumber = 4;
    private int basePointsMulti = 2;

    public FourExactlyPower()
    {
        Name = "Four is Good";
        PowerUpDescription = $"x{basePointsMulti} base multi for {exactMatchNumber} in a row.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count == exactMatchNumber ? basePointsMulti : 0;
    }
        

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FourExactlyPower();
}

public class HighStakesPower : PowerUp
{
    private int atLeastXTiles = 5;
    private int bonusPoints = 20;

    public HighStakesPower()
    {
        Name = "High Stakes";
        PowerUpDescription = $"+{bonusPoints} points if match has {atLeastXTiles} or more tiles.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) =>
        tiles.Count >= atLeastXTiles ? bonusPoints : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new HighStakesPower();
}

public class UnderdogPower : PowerUp
{
    private int plusMulti = 2;
    private int maxTiles = 3;

    public UnderdogPower()
    {
        Name = "Smaller is Better";
        PowerUpDescription = $"+{plusMulti} Multi if match is {maxTiles} or fewer tiles.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveMultiplierMod(List<Tile> tiles) =>
        tiles.Count <= maxTiles ? plusMulti : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new UnderdogPower();
}

public class StackedOddsPower : PowerUp
{
    private int baseMulti = 2;
    private int requiredTiles = 4;

    public StackedOddsPower()
    {
        Name = "Stacked Odds";
        PowerUpDescription = $"x{baseMulti} base for {requiredTiles}+ in a row.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count >= requiredTiles ? baseMulti : 0;
    }



    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new StackedOddsPower();
}

public class ConsistencyIsKey : PowerUp
{
    private int additiveMulti = 1;
    public int numberOfTilesPreviouslyMatched = 0;
    public int numberOfMatchesInARow = 0;

    public ConsistencyIsKey()
    {
        Name = "Consistency Is Key";
        PowerUpDescription = $"Each match of the same length as one previous gains a stacking +{additiveMulti} Multi.";
    }

    public override string GetCurrentPowerDescription() =>
        $"Matches of same length add +{additiveMulti} Multi. Current streak: {numberOfMatchesInARow} in a row.";

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (tiles.Count == numberOfTilesPreviouslyMatched)
        {
            numberOfMatchesInARow++;
            return numberOfTilesPreviouslyMatched * additiveMulti;
        }
        else
        {
            numberOfMatchesInARow = 0;
            numberOfTilesPreviouslyMatched = tiles.Count;
            return 1;
        }
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new ConsistencyIsKey(); // ✅ fixed bug here
}

public class SnowballPower : PowerUp
{
    private int matchCount = 0;
    private int baseMultiIncrease = 1;
    private int perXMatches = 2;

    public SnowballPower()
    {
        Name = "Snowball";
        PowerUpDescription = $"+{baseMultiIncrease} multiplier per {perXMatches} matches.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Current bonus: +{(matchCount / perXMatches) * baseMultiIncrease} Multi from {matchCount} matches.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matchCount++;
        return (matchCount / perXMatches) * baseMultiIncrease;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new SnowballPower();
}

// ---------- Icon Bonus Powers (extendable base class) ----------

public abstract class IconBonusPower : PowerUp
{
    public ItemType ItemType { get; private set; }
    // Subclasses will implement behavior for specific icons
}

public class PrecisionStrikePower : PowerUp
{
    private bool streakActive = false;
    private int streakCount = 0;
    private int multi = 1;

    public PrecisionStrikePower()
    {
        Name = "Precision Strike";
        PowerUpDescription = "Match exactly 3 twice to activate x4. Keeps working until you match a different number.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Precision Strike active: matching 3 tiles keeps x4 going!"
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count == 3)
            {
                streakCount++;

                if (streakCount >= 2)
                {
                    streakActive = true;
                    multi = 4 ; // Boost next match
                }
            }
            else
            {
                // Streak breaks on any non-3 match
                streakActive = false;
                streakCount = 0;
                multi = 1 ;
            }
            Debug.Log($"Precision Strike: Streak: {streakCount}");
        }
        return multi;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new PrecisionStrikePower();
}

/// <summary>
/// Match 4 tiles to trigger x3. Keeps working until you match a different number.
/// </summary>
public class FourFuryPower : PowerUp
{
    private bool streakActive = false;
    private int multi = 1;

    public FourFuryPower()
    {
        Name = "Four Fury";
        PowerUpDescription = "Match 4 tiles to trigger x3 on your next match. Keeps working until you match a different number.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Four Fury active: matching 4 tiles keeps x3 going!"
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count == 4)
            {
                streakActive = true;
                multi = 3;
            }
            else
            {
                streakActive = false;
                multi = 1;
            }
        }
        return multi;
       
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FourFuryPower();
}

/// <summary>
/// Match 5 or more tiles to trigger x5. Keeps working until you match fewer than 5.
/// </summary>
public class JackpotFivePower : PowerUp
{
    private bool streakActive = false;
    private int multi = 1; 

    public JackpotFivePower()
    {
        Name = "Jackpot Five";
        PowerUpDescription = "Match 5 or more tiles to trigger x5. Keeps working until you match fewer than 5.";
    }

    public override string GetCurrentPowerDescription()
    {
        return streakActive
            ? "Jackpot Five active: keep hitting 5+ for x5!"
            : PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count >= 5)
            {
                streakActive = true;
                multi = 5;
            }
            else
            {
                streakActive = false;
                multi = 1;
            }
        }
        return multi;
        
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new JackpotFivePower();
}

/// <summary>
/// If you match exactly 3 tiles, your next match scores double if it’s the same color.
/// </summary>
public class ColorFocusPower : PowerUp
{
    private ItemType lastMatchType;
    private int multi = 1;

    public ColorFocusPower()
    {
        Name = "Color Focus";
        PowerUpDescription = "Your next match gives you 2x points if you match the same block type";
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        // If we had a queued color and the match is that color → bonus applies
        if (MatchInfo.wasUserInitiatedMatch())
        {
            ItemType itemType = tiles.First().item.ItemType;
            if (lastMatchType == itemType)
            {

                multi = 2;
            } else
            {
                multi = 1;
            }
           
        }
        return multi;
      
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new ColorFocusPower();
}

/// <summary>
/// Match 4 tiles and your next match counts twice for combo bonuses (requires combo logic in ScoreManager).
/// </summary>
public class TwinShotPower : PowerUp
{
    private int multi = 1;

    public TwinShotPower()
    {
        Name = "Twin Shot";
        PowerUpDescription = "Match 4 tiles, and your next match counts twice for 2x";
    }

    public override string GetCurrentPowerDescription()
    {
        return "Twin Shot ready: next match counts double for combo bonuses!";

    }

    public override int NextMatchMulti(List<Tile> tiles)
    {
        if (MatchInfo.wasUserInitiatedMatch())
        {
            if (tiles.Count == 4)
            {
                multi = 2; // Queue effect for next match
            } else
            {
                multi = 1;
            }
        }


        return multi; // No scoring change by itself
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new TwinShotPower();

}
// ---------- PowerUp Library ----------

public static class PowerUpLibrary
{
    public static List<PowerUp> All = new List<PowerUp>()
    {
        new FlexibleMatchPower(3),
        new FourExactlyPower(),
        new BaseForExactlyThree(),
        new BasePerTile(),
        new ComboCrazePower(),
        new HighStakesPower(),
        new UnderdogPower(),
        new StackedOddsPower(),
        new SnowballPower(),
        new ConsistencyIsKey(),
        new TwinShotPower(),
        new ColorFocusPower(),
        new JackpotFivePower(),
        new FourFuryPower(),
        new PrecisionStrikePower()
    };
}
