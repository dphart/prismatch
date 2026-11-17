# Prismatch

Prismatch is a matching game made with Unity 6. Swap any two animal tiles and build a custom challenge in Free Play.

Open this project with Unity **6000.6.2f1** from Unity Hub. The installed 2D packages require Unity 6000.6; opening it with 6000.1 produces package compilation errors such as missing `EntityId` and `SpriteFitInfo` types.

## Power design reference

[Powers and scoring](feature_descriptions/powers.md) is the single source of truth for power philosophy, implemented scoring and effects, compatibility, draft authoring and wording. It distinguishes current mechanics from the pending move to level-specific opening drafts and the unresolved baseline-scoring decision.

## Game flow

The live campaign contains a tutorial and 22 challenges. Eight six-round remixes follow Changing Tides, followed by four shuffle-focused levels with reserved shuffle-tool and payoff offers. See [campaign remixes](feature_descriptions/campaign_remixes.md) and [shuffle levels](feature_descriptions/shuffle_levels.md); older expansion catalogs describe legacy seed layouts.

The ten live block-count levels count every matched animal and open with a choice of 3+, 4+ or 5+ block foundations. Clearing, budget and shuffle powers develop the run. The new remixes also reward cascades, larger groups or squares. Legacy collection objectives remain supported; power effects and credit rules live in [Powers and scoring](feature_descriptions/powers.md).

Animal Magnet powers favor a chosen animal in future spawns, including refills and shuffles, to complement animal scoring and clearing powers. Each animal has its own magnet; different magnets combine and are offered only for animals currently present. See [spawn odds](feature_descriptions/powers.md#animal-spawn-powers).

Local analytics preserve power-pick order and round/play performance, including failures and retries. Open **Settings → Analytics / Power Testing → Export All Runs** for an offline comparison report and CSV downloads. See [analytics and power testing](feature_descriptions/analytics.md).

Optional Unity Analytics SDK support sends power picks, round results and per-power round summaries after dashboard setup and player opt-in. Cloud uploads are off until the event schemas are configured. See [Unity Analytics activation](feature_descriptions/unity_analytics.md).

Level definitions support exact per-round power slots with required, optional and excluded tags, repeat policies and shortage diagnostics. See [Powers and scoring](feature_descriptions/powers.md#authoring-tags-and-compatibility) for authoring examples.

1. Choose horizontal, vertical, or connected (snake) matching, and set the minimum match length (default 4; custom minimum 2). Horizontal is capped by columns, vertical by rows, and snake by the larger grid dimension.
2. Set grid width and height from 3 to 10.
3. Choose a goal: points or blocks matched, and enter a target.
4. Choose a mode: Timed (10?3,600 seconds) or Limited moves (1?999 moves).
5. Choose a scoring power and reach the goal within your budget.

Each swap attempt uses one move in Limited moves, including swaps that fail to match. Cascades use no extra moves, and all matched blocks count even when they award zero points. The last move finishes resolving before the outcome is checked; reaching the goal wins. Shuffle costs five seconds in Timed or one move in Limited moves. Automatic dead-board refreshes are free.

A failed round can be retried once. Retry and New Run reset goal progress and restore the configured budget. Returning to Free Play retains the last played setup for the session. Escape and the power/settings panels pause play. Free Play runs do not update campaign completion records; the legacy level configurations remain available in code.

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

## Block replacement

Free Play offers In place (the default), South, North, West, and East. Gravity names the direction surviving blocks move. South draws new blocks from above; North from below; West from the right; East from the left. Each lane keeps surviving blocks in order, and newly spawned blocks animate in from outside the board edge. Cascades use the same strategy and cost no extra moves. Retry, New Run, and returning to the selector preserve the chosen strategy.

## Custom level progression

Set Levels (1-100), Starting target, and Growth rate (%) in the builder. Point growth switches to Block growth when the goal is blocks matched. Each target is the starting target multiplied by `(1 + growth / 100)^(level - 1)`, rounded up to a whole number; 0% keeps the target constant. For example, 400 with 25% growth gives 400, 500, 625. Targets cap at 2,147,483,647. Every level resets the time/move budget and goal progress, and the run ends in victory after the configured final level. The builder previews the first targets and the final target.
