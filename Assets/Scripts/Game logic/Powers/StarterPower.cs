using System.Collections.Generic;

public enum StarterStrategy { Precision, Momentum }
public sealed class StarterPower : PowerUp
{
    private readonly StarterStrategy strategy;
    private int matches;
    public StarterPower(StarterStrategy strategy)
    {
        this.strategy = strategy;
        Name = strategy == StarterStrategy.Precision ? "Precision Start" : "Momentum Start";
        PowerUpDescription = strategy == StarterStrategy.Precision
            ? "+3 points per tile. +1 multiplier on groups of exactly 3 (×2 alone)."
            : "+3 points per tile. Gain a permanent +1 multiplier every 5 scoring matches.";
    }
    public override int AdditiveBaseMod(List<Tile> tiles) => 3 * tiles.Count;
    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        matches++;
        return strategy == StarterStrategy.Precision ? (tiles.Count == 3 ? 1 : 0) : matches / 5;
    }
    public override string GetCurrentPowerDescription() => PowerUpDescription +
        (strategy == StarterStrategy.Momentum ? $" Current bonus: +{matches / 5}; {matches % 5}/5 toward next." : "");
    public override void Apply(GameState state) { matches = 0; }
    public override PowerUp Clone() => new StarterPower(strategy);
}
