using System.Collections.Generic;

public class ComboCrazePower : PowerUp
{
    private int comboCount = 0;
    private int plusMulti = 1;
    private int successfulCombosRequired = 15;

    public ComboCrazePower()
    {
        Name = "Combo Craze";
        PowerUpDescription = $"{plusMulti} multiplier every {successfulCombosRequired} successful combos.";
    }

    public override string GetCurrentPowerDescription() =>
        $"{plusMulti} multiplier every {successfulCombosRequired} combos. Current: {comboCount}/{successfulCombosRequired} combos.";

    public override int AdditiveMultiplierMod(List<Tile> tiles)
    {
        comboCount++;
        return comboCount / successfulCombosRequired;
    }

    public override void Apply(GameState state) => comboCount = 0;
    public override PowerUp Clone() => new ComboCrazePower();
}
