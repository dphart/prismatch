using System.Collections.Generic;

// This file contains shared "fake" implementations of game interfaces/classes for testing purposes.
// Centralizing them here allows multiple test files to use them without causing re-definition errors.

public class FakeTimeProvider : TimeProvider
{
    public int TimeRemaining => 0;
    public int TimeElapsed => 0;
}

public class FakeLevelInfoProvider : LevelInfoProvider
{
    public bool isBossLevel => false;
    public int currentRound => 0;
    public int maxRounds => 0;
    public List<Item> IconList => new List<Item>();
}

public class FakeItemInfoProvider : ItemInfoProvider
{
    public List<Item> GetActiveItems => new List<Item>();
    public void AddOrRemoveItem(Item item, bool add) { } // Do nothing.
}

public class FakePowerUp : PowerUp
{
    public override string GetCurrentPowerDescription()
    {
        return "Fake PowerUp for testing.";
    }

    public override void Apply(GameState state)
    {
        // This is a fake implementation for testing, so it does nothing.
    }

    public override PowerUp Clone()
    {
        // For testing purposes, we can return the same instance.
        return this;
    }
}
