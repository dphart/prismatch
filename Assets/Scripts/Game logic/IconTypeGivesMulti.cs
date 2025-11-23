using System.Collections.Generic;
using System.Linq;

public class IconTypeGivesMulti : IconBonusPower
{
    private int multi;

    public IconTypeGivesMulti(ItemType type, int _multi)
    {
        Name = $"{type} {type} {type}";
        PowerUpDescription = $"Get {_multi}x for matching {type}s";
        multi = _multi;
        ItemType = type;
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        if (tiles.First().item.ItemType == ItemType)
        {
            return multi;
        }
        return 0;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new IconTypeGivesMulti(ItemType, multi);
}
