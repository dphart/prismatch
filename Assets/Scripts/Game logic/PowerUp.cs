using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PowerUp
{
    public TimeProvider TimeProvider;
    public LevelInfoProvider LevelInfoProvider;
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public Sprite Icon { get; protected set; }

    public abstract void Apply(GameState state);
    public virtual void Tick(GameState state) { }
    public virtual int AdditiveBaseMod(List<Tile> tiles) => 0;
    public virtual int MultiplicativeBaseMod(List<Tile> tiles) => 1;
    public virtual int AdditiveMultiplierMod(List<Tile> tiles) => 0;
    public virtual int MultiplicativeMultiMod(List<Tile> tiles) => 1;
    public virtual bool IsScoreValid(List<Tile> tiles) => true;


    public abstract PowerUp Clone();
}

public class FlexibleMatchPower : PowerUp
{
    public int Increase { get; private set; }

    public FlexibleMatchPower(int amount)
    {
        Name = "More Power!";
        Description = $"Receive +{amount} per tile";
        Increase = amount;
    }

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * Increase;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new FlexibleMatchPower(Increase);
}

public class BasePowerUp : PowerUp
{
    public BasePowerUp()
    {
        Name = "Base Scoring";
        Description = "Each tile match gives base points.";
    }

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
        Description = $"{plusMulti} multiplier every {successfulCombosRequired} successful combos";
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        comboCount++;
        return comboCount / 5;
        
    }

    public override void Apply(GameState state) => comboCount = 0;
    public override PowerUp Clone() => new ComboCrazePower();
}

public class BasePerTile : PowerUp
{
    private double turnMatches = 0;
    private double basePointModifier = .1;

    public BasePerTile()
    {
        Name = "Momentum Bonus";
        Description = $"+{basePointModifier} Base per tile matched";
    }

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
        Description = $"{basePointsAdded} base points if match is exactly {exactMatchNumber}";
    }

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count == exactMatchNumber ? basePointsAdded : 0;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new BaseForExactlyThree();
}

public class FourExactlyPower : PowerUp
{
    private int exactMatchNumber = 4;
    private int basePointsMulti = 2;

    public FourExactlyPower()
    {
        Name = "Four is good";
        Description = $"x{basePointsMulti} Base points for {exactMatchNumber} in a row";
    }

    public override int MultiplicativeBaseMod(List<Tile> tiles) => tiles.Count == exactMatchNumber ? basePointsMulti : 1;
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
        Description = $"+{bonusPoints} points if match has {atLeastXTiles} or more tiles.";
    }

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count >= atLeastXTiles ? bonusPoints : 0;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new HighStakesPower();
}

public class UnderdogPower : PowerUp
{
    private int plusMulti = 2;
    private int maxTiles = 3;
    public UnderdogPower()
    {
        Name = "Smaller is better";
        Description = $"+{plusMulti} Multi if match is {maxTiles} or fewer tiles.";
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        return tiles.Count <= maxTiles ? plusMulti : 0;
    }
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
        Description = $"x{baseMulti} base for {requiredTiles}+ in a row.";
    }

    public override int MultiplicativeBaseMod(List<Tile> tiles) => tiles.Count >= requiredTiles ? baseMulti : 1;
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
        Name = "ConsistencyIsKey";
        Description = $"Each match of the same length as one previous gains a stacking +{additiveMulti} Multi";
    }

    public override int MultiplicativeBaseMod(List<Tile> tiles)
    {
        if (tiles.Count == numberOfTilesPreviouslyMatched)
        {
            numberOfMatchesInARow++;
            return numberOfTilesPreviouslyMatched * additiveMulti;
        } else
        {
            numberOfMatchesInARow = 0;
            numberOfTilesPreviouslyMatched = tiles.Count;
            return 1;
        }
    }
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new StackedOddsPower();
}

public class SnowballPower : PowerUp
{
    private int matchCount = 0;
    private int baseMultiIncrease = 1;
    private int perXMatches = 2;

    public SnowballPower()
    {
        Name = "Snowball";
        Description = $"+{baseMultiIncrease} multiplier per {perXMatches} matches";
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matchCount++;
        return (matchCount / perXMatches) * baseMultiIncrease;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new SnowballPower();
}
public class IconBonusPower : PowerUp
{
    public ItemType IconType { get; private set; }
    public int BonusPoints { get; private set; }

    public IconBonusPower(ItemType iconType, int bonusPoints)
    {
        IconType = iconType;
        BonusPoints = bonusPoints;
        Name = $"{iconType} Bonus";
        Description = $"Gain +{bonusPoints} points for each {iconType} matched.";
    }

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        return tiles.Count(t => t.item.ItemType == IconType) * BonusPoints;
    }

    public override void Apply(GameState state) { }

    public override PowerUp Clone()
    {
        return new IconBonusPower(IconType, BonusPoints);
    }
}


public static class PowerUpLibrary
{
    public static List<PowerUp> All = new List<PowerUp>()
    {
        new FlexibleMatchPower(3),
        new FourExactlyPower(),
        new BaseForExactlyThree(),
        new BasePerTile(),
        new ComboCrazePower(),
        new BasePowerUp(),
        new HighStakesPower(),
        new UnderdogPower(),
        new StackedOddsPower(),
        new SnowballPower(),
        new ConsistencyIsKey()
    };
}