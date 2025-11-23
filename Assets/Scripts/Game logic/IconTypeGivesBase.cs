using System.Collections.Generic;
using System.Linq;

public class IconTypeGivesBase : IconBonusPower
{
    private int BasePoints;

    public IconTypeGivesBase(ItemType type, int basePoints)
    {
        Name = $"++{type}++";
        PowerUpDescription = $"Get {basePoints} per tile for matching {type}s";
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
            return base.AdditiveMultiplierMod(tiles);
        }
        return 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new IconTypeGivesBase(ItemType, BasePoints);
}
