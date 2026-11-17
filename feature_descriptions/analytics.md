# Local analytics for power testing

Open **Settings → Analytics / Power Testing**, or **Analytics / Export** on the results screen. The in-game panel shows the current run's round attempts, plays, successful plays, points per play, goal credit per play, and chronological power picks. **Export All Runs** creates a searchable offline report with CSV downloads. Local analytics stay on the device. An optional [Unity Analytics integration](unity_analytics.md) can send new summary events after dashboard setup and player opt-in; it does not upload the historical local files.

Start a new run after updating the game so the entire power-pick order is captured. Earlier games cannot be backfilled from the old latest-run summary.

## What is counted

- **Play:** one swap attempt, including swaps that produce no match. All resulting groups and cascades belong to that play. Shuffles are counted separately.
- **Successful play:** a swap that produces at least one player-created matched group. Cascade groups are recorded separately; several groups are not several plays.
- **Points per play:** total round points / swap attempts. Zero-play rounds report zero.
- **Goal credit per play:** total progress toward the round objective / swap attempts. Use this for block/collection builds, where points can legitimately stay zero.
- **Physical clears:** actual matched and bonus tiles, separate from multiplied or restricted goal credit.
- **Power order:** every chosen power has a global pick number, round, attempt number, description snapshot and tags. Round records reference exactly the picks still active in that build, including repeated copies.

Retrying preserves the failed attempt and its choices. A replacement choice receives a new pick number. Removed powers are absent from the retry's active build but remain in the historical pick log. Starting a new run creates a new run ID; it does not erase earlier runs.

Round records include objective, target, matching rules, bosses, roster, time/move budget and shuffle count. Run metadata includes habitat, game version, platform and UTC timestamps. Play records include group/cascade counts, points, credit, clear count, resolution time and scoring breakdown strings. Resolution time measures game-active time during resolution, not thinking time; round seconds include active thinking time. Pauses and power selection do not advance gameplay elapsed time.

## Export

Desktop exports go to `Application.persistentDataPath/Analytics/Export`, and the HTML report opens in the browser. WebGL downloads the HTML report, which contains individual CSV download links. WebGL history uses browser-local persistent storage and is flushed through the included plugin; clearing site data removes that history. Mobile builds write the same local files, though opening/sharing a local HTML file depends on the platform's installed viewer; native mobile sharing is not implemented.

| CSV | Contents |
|---|---|
| runs.csv | Run IDs, dates, outcomes, level/settings and game version |
| rounds.csv | One row per round attempt, including failed, active and abandoned attempts; averages and active pick order |
| power-picks.csv | Complete chronological choices, including discarded retry choices |
| plays.csv | One row per swap attempt with all resulting cascade totals and scoring explanations |
| power-exposure.csv | Whole-build performance while each power was owned, grouped by comparable round conditions and copy count |

The power-exposure table includes both won and failed rounds. Active and abandoned rounds remain in raw exports but are excluded from that comparison. A play interrupted by timeout retains the points and credit already earned and is marked interrupted. Unexpected app termination may leave a run marked active; it is still available in raw exports and excluded from comparisons.

Means are **weighted by total plays**. For example, 100 points in one play plus 0 points in nine plays yields 10 points/play, not 50. Duplicate copies of a power form separate comparison groups. The table also shows run/attempt/play sample sizes and round win rate.

## Comparing strength

Replay the same level and round with different choices, keeping the rest of the build similar. Compare matching cohorts, average points/play for point goals, credit/play for collection goals, and win rate. Inspect chronological picks to check when a power entered the build. Repeat runs to reduce board randomness and learning effects.

Power exposure is an association with the **whole build**, not a causal score assigned to one power. Several powers can multiply one another, and a later round may be harder. Cohorts separate level, game version, goal, mode, board, gravity, starting target/growth, budget, round target, objective, baseline matching rules and bosses. They deliberately do not separate the changed roster or active powers, since those are part of what is being evaluated. Changing game code without bumping the game version can mix balance revisions; increment the Unity player version when comparing different releases.

Scoring breakdown strings are diagnostic explanations, not additive independent contribution estimates. There is no forced-power picker or deterministic board replay in this update; choose different offered powers using the existing game flow.

## Storage and verification

One versioned JSON file per run is checkpointed after power selection, resolved plays, shuffles, round outcomes and application pause. Graceful scene exit/new run marks unfinished sessions abandoned. Save replacement uses a temporary file and a previous-file backup; malformed records are skipped or recovered from backup. Save/export failures are reported without stopping gameplay.

The game and edit-mode test assemblies compile. Thirty-four focused standalone checks pass, including eight analytics tests covering retry history, exact active pick order, failed swaps, cascades, interrupted plays, separate block credit, weighted comparisons, copy counts, cohort separation, CSV quoting/locale handling and report HTML escaping. Two JavaScript checks also verify local report download and coalesced browser-storage flush behavior using browser mocks. Unity UI integration, native persistence and actual WebGL downloads require an in-game/device pass; those were not executed by the standalone runner.
