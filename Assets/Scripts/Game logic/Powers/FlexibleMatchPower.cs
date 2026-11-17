using System.Collections.Generic;

public class FlexibleMatchPower : PowerUp
{
    public override bool HasConditionalTrigger => false;
    public override bool OpeningOnly => true;
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public int Increase { get; private set; }

    public FlexibleMatchPower(int amount)
    {
        Name = "More Power!";
        PowerUpDescription = $"All matches gain {amount} base points per tile.";
        Increase = amount;
    }

    public override float FlatBasePerTile => Increase;
    public override string ConditionalStatDescription => "";
    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) => tiles.Count * Increase;
    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new FlexibleMatchPower(Increase));
}
