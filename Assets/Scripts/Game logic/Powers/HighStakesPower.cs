using System.Collections.Generic;

public class HighStakesPower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Base;
    public override string BaseCondition => GetCurrentPowerDescription();
    private int atLeastXTiles = 5;
    private int bonusPoints = 50;

    public HighStakesPower()
    {
        Name = "High Stakes";
        PowerUpDescription = $"Matches of {atLeastXTiles} or more gain {bonusPoints} base points.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) =>
        tiles.Count >= atLeastXTiles ? bonusPoints : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => SharedClone(new HighStakesPower());
}
