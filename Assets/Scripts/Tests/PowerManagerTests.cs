using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class PowerManagerTests
{
    private PowerManager _powerManager;

    [SetUp]
    public void Setup()
    {
        // Arrange: Create a new PowerManager before each test.
        var timeProvider = new FakeTimeProvider();
        var levelInfoProvider = new FakeLevelInfoProvider();
        var itemInfoProvider = new FakeItemInfoProvider();
        _powerManager = new PowerManager(timeProvider, levelInfoProvider, itemInfoProvider);
    }

    [Test]
    public void RegisterPower_WhenCalled_AddsPowerToActivePowersList()
    {
        // Arrange
        var powerUp = new FakePowerUp();

        // Act
        _powerManager.RegisterPower(powerUp);
        var activePowers = _powerManager.ActivePowers();

        // Assert
        Assert.AreEqual(1, activePowers.Count, "The number of active powers should be 1.");
        Assert.AreSame(powerUp, activePowers.First(), "The active power should be the one we registered.");
    }
}
