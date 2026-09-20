using System.Collections.Generic;

public class HighStakesPower : PowerUp
{
    private int atLeastXTiles = 5;
    private int bonusPoints = 50;

    public HighStakesPower()
    {
        Name = "High Stakes";
        PowerUpDescription = $"+{bonusPoints} points if match has {atLeastXTiles} or more tiles.";
    }

    public override string GetCurrentPowerDescription() => PowerUpDescription;

    public override int AdditiveBaseMod(List<Tile> tiles) =>
        tiles.Count >= atLeastXTiles ? bonusPoints : 0;

    public override void Apply(GameState state) { }
    public override PowerUp Clone() => new HighStakesPower();
}
