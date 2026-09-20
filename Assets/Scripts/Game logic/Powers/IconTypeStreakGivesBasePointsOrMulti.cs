using System.Collections.Generic;

public class IconTypeStreakGivesBasePointsOrMulti : IconBonusPower
{
    private int BasePoints;
    private int NumRequired;
    private int BaseMulti;
    private int NumOfMatchesOfType = 0;

    public IconTypeStreakGivesBasePointsOrMulti(ItemType type, int basePoints, int numRequired, int baseMulti)
    {
        var descString = $"When you match {numRequired} {type} tiles, permanently ";

        if (basePoints > 0)
        {
            descString += $"get {basePoints} points per tile";
        }
        if (baseMulti > 0)
        {
            if (basePoints > 0)
            {
                descString += $" and ";
            }
            descString += $"get {baseMulti} multi";
        }

        Name = $"{type} Growth ({(basePoints > 0 ? "Points" : "Multiplier")})";
        PowerUpDescription = descString;
        BasePoints = basePoints;
        ItemType = type;
        NumRequired = numRequired;
        BaseMulti = baseMulti;
    }

    public override string GetCurrentPowerDescription()
    {
        var descString = $"{NumOfMatchesOfType} {ItemType} matches are granting ";

        if (BasePoints > 0)
        {
            descString += $"{BasePoints} points per tile";
        }
        if (BaseMulti > 0)
        {
            if (BasePoints > 0)
            {
                descString += $" and ";
            }
            descString += $"get {BaseMulti} multi";
        }

        return PowerUpDescription + descString;
    }

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            if (tile.item.ItemType == ItemType)
            {
                NumOfMatchesOfType++;
            }
        }

        if (BasePoints > 0)
        {
            return (NumOfMatchesOfType / NumRequired) * BasePoints * tiles.Count;
        }
        return 0;
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        if (BaseMulti > 0) { return (NumOfMatchesOfType / NumRequired) * BaseMulti; }
        return 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new IconTypeStreakGivesBasePointsOrMulti(ItemType, BasePoints, NumRequired, BaseMulti);
}
