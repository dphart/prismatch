# Prismatch

Prismatch is a timed, build-focused matching game made with Unity 6. Swap any two animal tiles to form a connected group of three or more. Bent groups count. Each round begins with a choice of scoring power; powers combine across the run, while boss rounds restrict how points are earned.

## Game flow

1. Choose one of three powers.
2. Reach the score target before time expires.
3. Continue through ten rounds, including boss rounds.
4. Complete the final round to save the level as completed and record the best run score.

Shuffle creates a playable board and costs five seconds. A failed round can be retried once; doing so removes the most recently selected power. Escape pauses play. The active-power and boss-power panels also pause play while open.

## Project structure

- `Assets/Scripts/Game logic/Board`: board model, scene view, tiles, and animal data.
- `Assets/Scripts/Game logic/Core`: run lifecycle, rounds, timer, pause, audio feedback, and progress summary.
- `Assets/Scripts/Game logic/Scoring`: score calculation and visible score results.
- `Assets/Scripts/Game logic/Level`: level interface and data-backed level implementations.
- `Assets/Resources/Levels`: editable level tuning assets for dimensions, timer, targets, animal count, power pool, and boss schedule.
- `Assets/Scripts/Tests`: edit-mode regression tests for board generation, scoring, powers, timer, and controller behavior.

The board's plain C# `BoardModel` owns matching and legal-move rules. `Board` animates that model and rejects overlapping input. `GameController` uses explicit phases so input, pause, round completion, failure, and victory cannot run at the same time.

## Tuning

Open `Assets/Resources/Levels/Level1.asset` or `Level2.asset` in Unity. Target score is calculated as:

`baseTarget × targetGrowth ^ (round - 1)`

The end-of-run panel reports score pace, match count, cascades, shuffles, and zero-point clears. The latest run is also serialized to the `Prismatch.LastRun` PlayerPrefs key for quick balance inspection.
