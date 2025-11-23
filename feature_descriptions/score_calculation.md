# Feature: Score Calculation

This document outlines the behavior of the score calculation feature and how it is tested.

## Expected Behavior

### Core Rule: No Tiles, No Score

The most basic rule is that if there is no valid list of tiles to process, the score must be zero.

-   **Scenario**: The score calculation is triggered with a `null` or empty list of tiles.
-   **Outcome**: The score awarded must be 0, and the calculation should stop immediately.

## Testing

This feature is tested in `ScoreManagerTests.cs`.

### Test Cases:

-   `CalculateScore_WithEmptyTileList_ReturnsZero`
-   `CalculateScore_WithNullTileList_ReturnsZero`

-   **Purpose**: To verify that a score of 0 is returned when the tile list is empty or null.
-   **Setup**: Creates a `ScoreManager` instance.
-   **Execution**: Calls `CalculateScore()` with an empty list or a null value.
-   **Assertion**: It asserts that the returned score is exactly 0.

---

### Core Rule: Match With No Scoring Power-ups

Even if a valid match is made, the score should be zero if no active power-up specifically modifies the score.

-   **Scenario**: A player makes a match with one or more tiles.
-   **Condition**: There are no power-ups active, or the active power-ups do not apply to the current match.
-   **Outcome**: The score awarded for the match is 0.

### Test Case: `CalculateScore_WithTilesButNoPowers_ReturnsZero`

-   **Purpose**: To verify that a score of 0 is returned when a match is made, but no scoring power-ups are active.
-   **Setup**: The test initializes the `ScoreManager` and `PowerManager`. It creates a mock `Tile` to represent a match.
-   **Execution**: It calls `CalculateScore()` with a list containing the mock tile, while the `PowerManager` is empty.
-   **Assertion**: It asserts that the returned score is exactly 0.
