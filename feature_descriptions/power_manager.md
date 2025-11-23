# Feature: Power-up Management

This document outlines the behavior of the `PowerManager`, which is responsible for tracking all active power-ups in the game.

## Expected Behavior

### Registering Power-ups

The `PowerManager` must provide a way to add new power-ups to an internal list of active powers.

-   **Scenario**: A new power-up is acquired by the player.
-   **Outcome**: The `RegisterPower` method is called, and the new power-up is added to the list of active powers.

## Testing

This feature is tested in the `PowerManagerTests.cs` test file (`Assets/Scripts/Tests/PowerManagerTests.cs`).

### Test Case: `RegisterPower_WhenCalled_AddsPowerToActivePowersList`

-   **Purpose**: To verify that calling `RegisterPower` on the `PowerManager` correctly adds the given power-up to the active list.
-   **Setup**: The test creates a new instance of the `PowerManager` and a new `PowerUp`.
-   **Execution**: It calls the `RegisterPower()` method with the new `PowerUp`.
-   **Assertion**: It asserts that the list of active powers now contains exactly one item, and that item is the one that was just registered.
