using System.Collections.Generic;
using System.Linq;

public class ColorFocusPower : PowerUp
{
    private ItemType? lastMatchType;

    public ColorFocusPower()
    {
        Name = "Color Focus";
        PowerUpDescription = "×2 on a player-created match of the same animal as your previous player-created match. Cascades do not change the animal.";
    }

    public override string GetCurrentPowerDescription()
    {
        return PowerUpDescription;
    }

    public override int CurrentMatchMultiplier(List<Tile> tiles)
    {
        if (MatchInfo?.wasUserInitiatedMatch() == true)
        {
            ItemType itemType = tiles.First().item.ItemType;
            int multiplier = lastMatchType == itemType ? 2 : 1;
            lastMatchType = itemType;
            return multiplier;
        }
        return 1;
    }

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new ColorFocusPower();
}
