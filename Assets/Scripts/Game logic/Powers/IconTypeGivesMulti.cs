using System.Collections.Generic;
using System.Linq;

public sealed class AnimalDoublePower : IconBonusPower
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override PowerTags Tags => base.Tags | PowerTags.BasePoints | PowerTags.AnimalBaseDouble;
    public override string BaseCondition => PowerUpDescription;
    public AnimalDoublePower(ItemType animal)
    {
        ItemType = animal;
        Name = animal + " Double";
        PowerUpDescription = $"Matches with {animal} double their base points.";
    }
    public override int CurrentMatchBaseMultiplier(List<Tile> tiles) =>
        tiles != null && tiles.Any(tile => tile != null && tile.item != null && tile.item.ItemType == ItemType) ? 2 : 1;
    public override bool CanOfferWith(IReadOnlyList<PowerUp> active) =>
        !active.Any(power => power is AnimalDoublePower animal && animal.ItemType == ItemType);
    public override string GetCurrentPowerDescription() => PowerUpDescription;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new AnimalDoublePower(ItemType));
}

public class IconTypeGivesMulti : IconBonusPower
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int multi;

    public IconTypeGivesMulti(ItemType type, int _multi)
    {
        Name = $"{type} {type} {type}";
        PowerUpDescription = $"Matches of {type} gain {_multi} multiplier.";
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
    public override PowerUp Clone() => SharedClone(new IconTypeGivesMulti(ItemType, multi));
}
