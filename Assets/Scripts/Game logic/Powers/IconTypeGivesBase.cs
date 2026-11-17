using System.Collections.Generic;
using System.Linq;

public class IconTypeGivesBase : IconBonusPower
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => GetCurrentPowerDescription();
    private int BasePoints;
    public int BonusPerTile => BasePoints;

    public IconTypeGivesBase(ItemType type, int basePoints)
    {
        Name = $"++{type}++";
        PowerUpDescription = $"{type} matches: +{basePoints} base points per tile.";
        BasePoints = basePoints;
        ItemType = type;
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int AdditiveBaseMod(List<Tile> tiles)
    {
        if (tiles.First().item.ItemType == ItemType)
        {
            return BasePoints * tiles.Count;
        }
        return 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new IconTypeGivesBase(ItemType, BasePoints));
}
