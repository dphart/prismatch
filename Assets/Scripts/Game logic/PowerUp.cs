using System;
using System.Collections.Generic;
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

    public abstract string GetCurrentPowerDescription();

    public abstract void Apply(GameState state);
    public virtual void Tick(GameState state) { }
    public virtual int AdditiveBaseMod(List<Tile> tiles) => 0;
    public virtual int AdditiveMultiplierMod(List<Tile> tiles) => 0;
    public virtual int NextMatchMulti(List<Tile> tiles) => 1;
    public virtual bool IsScoreValid(List<Tile> tiles) => true;

    public abstract PowerUp Clone();
}

public abstract class IconBonusPower : PowerUp
{
    public ItemType ItemType;
}

public static class PowerUpLibrary
{
    public static List<PowerUp> GetAll() => new List<PowerUp>()
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
        new PrecisionStrikePower(),
        new IconTypeGivesMulti(ItemType.Sloth, 2),
        new IconTypeGivesMulti(ItemType.Girraffe, 2),
        new IconTypeGivesMulti(ItemType.Duck, 2),
        new IconTypeGivesMulti(ItemType.Chicken, 2),
        new IconTypeGivesMulti(ItemType.Hippo, 2),
        new IconTypeGivesMulti(ItemType.Walrus, 2),
        new IconTypeGivesMulti(ItemType.Bear, 2),
        new IconTypeGivesBase(ItemType.Sloth, 5),
        new IconTypeGivesBase(ItemType.Girraffe, 5),
        new IconTypeGivesBase(ItemType.Duck, 5),
        new IconTypeGivesBase(ItemType.Chicken, 5),
        new IconTypeGivesBase(ItemType.Hippo, 5),
        new IconTypeGivesBase(ItemType.Walrus, 5),
        new IconTypeGivesBase(ItemType.Bear, 5),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Walrus, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Sloth, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Girraffe, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Bear, 2, 10, 0),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Walrus, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Sloth, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Girraffe, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Duck, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Hippo, 0, 10, 1),
        new IconTypeStreakGivesBasePointsOrMulti(ItemType.Bear, 0, 10, 1),
    };

    public static List<PowerUp> All = GetAll();
}
