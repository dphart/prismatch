using System.Collections.Generic;
using System.Linq;

public class ColorFocusPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private ItemType? lastMatchType;

    public ColorFocusPower()
    {
        Name = "Color Focus";
        PowerUpDescription = "Matching the same animal twice in a row doubles the second match. Only matches you make count.";
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
    public override PowerUp Clone() => SharedClone(new ColorFocusPower());
}
