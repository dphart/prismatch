using System.Collections.Generic;
using System.Linq;

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
        if (MatchInfo.wasUserInitiatedMatch())
        {
            ItemType itemType = tiles.First().item.ItemType;
            if (lastMatchType == itemType)
            {
                multi = 2;
            }
            else
            {
                multi = 1;
            }
        }
        return multi;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new ColorFocusPower();
}
