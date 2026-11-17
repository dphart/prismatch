# Power activation feedback

Triggered powers launch as an icon-only fountain from behind the footer into the empty space beneath the board. Every distinct power from the same move is queued; multiple effects from one power produce one icon per move. Later moves can emit that power again while older icons are still flying. Bursts fan left and right with a small stagger, arc upwards and fade. They respect animation speed and gameplay pause.

A RectMask2D clips the entire fountain to the gap between the actual board tiles and the highest visible footer control or message. Icons cannot cover the grid, timer, stats, buttons or notices. The fountain and all child graphics ignore touches; icons have no buttons or automatic text labels. When a temporary message fills the gap, pending icons wait for space. Existing synchronized hollow tile highlights remain controlled by each power's display setting.

The same activation path covers scoring powers (excluding passive flat bonuses), growth progress/upgrades, collection and admission credit, committed extra clears and transformations, budget rewards, shuffle progress/free uses/discounts, favored magnet refills, and boss score/block/move effects. Feedback reads resolved effects and does not reevaluate scoring or reward hooks.

Level characteristics have runtime LevelCharacteristicPower instances in PowerManager.LevelPowers, separate from selectable rewards. Their rule adapters handle animal rewards, pattern rewards, garden planting, treasure collection and border progression. Lightning strikes/charged tiles, garden/spotlight bonuses, least-matched rewards, edge lighting, treasure collection/readiness and tide turns all emit ordinary PowerActivation events. Existing board state and reward formulas are retained. The old level-specific text panel is replaced by the icon row.

Icons have independent flight lifetimes. Round transitions, disable and scene destruction clear both pending icons and active flights. Highlights share one animation clock and lifetime. Backup real-time cleanup prevents abandoned transient overlays.

Compilation and headless regression checks cover existing mechanics, runtime characteristic power creation/cloning, actual pattern rewards, garden planting without repeated state advancement, admission credit, scoring feedback without hook reevaluation, extra clears and transformations. Layout and touch readability still need verification on Android.

## Standard display contract

PowerUp.DisplayWhenTriggered and the optional shared-content displayWhenTriggered field choose None (none), Icon (icon), matched tile highlight (tiles), or both (iconAndTiles). PowerActivation snapshots this presentation metadata separately from the resolved gameplay result. The common renderer filters icons and tile highlights independently, including mixed batches. None suppresses transient trigger feedback only; it does not disable the power or its existing board changes, planted/charged backgrounds, scoring, counters, or rewards.

Missing metadata retains the existing icon/highlight behavior for all older powers and level characteristics. No existing catalog values are migrated or changed. New designer powers start with None. Both native and custom power editors expose Display when triggered; exports, saves, clones and level-specific copies preserve the field. Unknown display modes are rejected on save.

Fountain queue regression coverage verifies 24 powers from one move without replacement or truncation, duplicate suppression within that move, retriggering on later moves, preservation of display-mode choices, reset cleanup, and no scoring-hook calls. Flight layout and readability still need on-device verification.

Readability tuning: flights last 2.8 seconds at normal animation speed. Icons begin spread across their lanes with only a small outward drift, a lower arc, nearly constant scale and at most four degrees of tilt. They stay opaque longer before fading.

Score gains now rise from the same masked gap beneath the board instead of the old top HUD anchor. A centered, dark-backed points label rises for 0.35 seconds, holds for 3 seconds, then fades over 0.45 seconds. Its timing uses real seconds independently of the gameplay animation-speed setting. Cascades from the same move add into one displayed total and refresh the hold; a later move starts a new total. The score label draws above the power icons, stays inside the gap, ignores touches, and clears with the other feedback when the board is hidden or the UI is disabled. Compilation verified; placement and readability still need an in-game check.

Passive-feedback policy: always-on base points, flat multipliers, roster-wide Welcome/Farewell modifiers, foundations, unconditional per-match base growth and unconditional extra-neighbor clears produce no trigger icon or background flash. Shared always/Any rules follow the same policy. Conditional effects subtract existing flat contributions; previously earned permanent bonuses remain quiet between upgrades. Conditional animal, shape and timing rewards and actual earned upgrades retain feedback. Scoring, credits and clearing behavior are unchanged.
