using System.Collections.Generic;

public class ComboCrazePower : PowerUp
{
    public override PowerStatEffects StatEffects => PowerStatEffects.Multiplier;
    public override string MultiplierCondition => GetCurrentPowerDescription();
    private int comboCount = 0;
    private int plusMulti = 1;
    private int successfulCombosRequired = 15;

    public ComboCrazePower()
    {
        Name = "Combo Craze";
        PowerUpDescription = $"Every {successfulCombosRequired} matches: +{plusMulti} permanent multi.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{PowerUpDescription} Earned: +{comboCount / successfulCombosRequired} multi. Progress: {comboCount % successfulCombosRequired}/{successfulCombosRequired}.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        comboCount++;
        return comboCount / successfulCombosRequired;
    }

    public override void Apply(GameState state) => comboCount = 0;
    public override PowerUp Clone() => SharedClone(new ComboCrazePower());
}
