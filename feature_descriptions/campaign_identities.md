# Campaign identities and balance

The playable campaign contains Tutorial (1), Animal Friends (2), Lightning Lanes (3), Size Matters (4), Clear Commitments (5), Chain Reaction (6), Garden Plots (7), Alternating Current (8), Border Patrol (9), Treasure Trail (10), and Changing Tides (11). All older generated levels after ID 6 have been removed from the live catalog and picker. Their seed definitions remain archived in code; they are not registered at startup. Existing completion data is retained. Garden Plots and Alternating Current are implemented as new authored entries, replacing the old IDs 7 and 8. Older broad-campaign descriptions below are historical unless explicitly covered by these eleven level specifications.

This document owns current campaign progression and draft pacing. Older initial/expansion tables are historical seed layouts. [Powers and scoring](powers.md) owns power mechanics and design principles; [campaign bosses](campaign_bosses.md) owns the current penalty roster. The tutorial remains one round. Levels 3–51 retain their names, unlock IDs, time/move budgets and gravity; level 2 is now Animal Friends. The later [block collection update](block_collection.md) halves starting block targets to account for restricted credit.

The live shared catalog, `Assets/Resources/Content/prismatch.json`, owns final campaign tuning; `CampaignIdentity` in `LevelLibrary.cs` retains seed definitions. All non-tutorial campaign boards now use 6x7 or 7x7 (width x height), including every round and cloned run. Former wider boards use seven columns; narrower boards use six. The guided tutorial keeps its original layout. Targets and time/move budgets are unchanged; difficulty after resizing still needs playtesting. Animal Friends (campaign ID 2, first challenge after the tutorial) has a bespoke four-round animal draft; see [Powers and scoring](powers.md#animal-friends-first-playable-challenge). Other named levels have an authored habitat, a 6–9 round run, a curated draft every round after its initial starter, and a compatible final boss. Free Play settings remain configurable and unchanged.

Habitats also have distinct gameplay backgrounds: teal shore waves and sand, green garden leaves, blue rapids and water patterns, mauve mountain silhouettes, and a purple/gold menagerie canopy. Static scenery stays around the perimeter; animal tiles and their colors are unchanged. Matching progress-bar accents and a habitat label connect the HUD to the scenery. The picker shows each level's habitat name and accent stripe. Boss rounds slightly darken the habitat; Free Play and the tutorial use the neutral blue backdrop. Habitat metadata survives level cloning, and the backdrop updates in place across rounds without adding extra background objects. Background graphics never capture input.

`HabitatArtwork` supplies shared palettes and polygon geometry; `HabitatScenery` renders them as a Unity UI mesh. A visually reviewed [contact sheet](../outputs/habitats/habitat-backgrounds.png) exports that same geometry with a schematic board. It is a scenery preview, not a screenshot of Unity gameplay. Game and test assemblies compile and the 26 focused standalone checks pass; a full Unity device/layout pass remains outstanding.

| Habitat | Strategy | New points powers | Associated bosses |
|---|---|---|---|
| Shore | Match on board edges | Shore Bounty, Amplifier, Engine | Lighthouse, Corner Collector, Wallflower, Seesaw |
| Garden | Include 2x2 squares | Garden Bounty, Amplifier, Engine | Box Troll, Center Stage |
| Rapids | Trigger cascades | Rapids Bounty, Amplifier, Engine | Cascade Surfer, Rapids Toll, Opening Act |
| Giants | Match groups of 6+ | Giant Bounty, Amplifier, Engine | Trophy Hunter, Stretch |
| Menagerie | Switch animals between resolved groups | Menagerie Bounty, Amplifier, Engine | Twin Keeper, Oddball, Even Steven |

Habitat reward mechanics, clearing effects and compatibility are specified in [Powers and scoring](powers.md#habitat-scoring-powers).

Block campaigns now use per-round animal and shape objectives and collection drafts instead of the original habitat-only block offers. See [block collection](block_collection.md) for the current credit, transformation and additional-animal rules.

## Targets and risk

Ordinary block targets grow **25% per round**; positive-bonus block levels grow **35%**. Positive-bonus points levels add **15 percentage points** to the ordinary or high-stakes rates below. Ordinary point targets grow **55%, 60% or 65%**, depending on the level. These are compounded from the starting target, rounded up by the existing target calculator.

High-stakes point levels grow **85%** and guarantee a matching premium Engine among every draft's choices from round 2: Boss Encounter (11), Time Is Money (21), Last Move Market (25), Big Group Grove (38), Double Trouble (41), Falling Fortune (46), and Row Rewind (50). Their final boss is High Roller: every third swap attempt keeps normal value; other attempts award half, rounded down. Failed swaps advance the cycle; cascades inherit the originating attempt.

Small Circle (13), West Side Wilds (17), Alternating Avenue (39), and Eastward Escape (49) have **Ducks give no points** throughout the run, including the finale. Ducks still clear normally and retain block credit. The rule is shown in the briefing and active rules. It does not turn every ordinary round into a boss finale. Animal-pair groups containing ducks are also blocked.

Trophy Hunter awards zero credit below six matched tiles and normal credit on six or more. Rapids Toll awards zero for cascades and normal credit for player groups. These modify the level's goal credit, including blocks where assigned; they do not change physical clears. Duck Customs only vetoes points.

## Draft pacing

Ordinary point runs use round 3 to offer an animal addition, an animal removal, and a habitat scoring alternative. Other rounds offer two habitat scoring rewards plus tools or another habitat reward. Block runs offer a habitat clearing reward, another clearing alternative, and tools or clearing. The final draft is labeled boss preparation. High-stakes runs offer an Engine, another habitat reward, and a roster/budget/shuffle alternative.

Tool powers can run out in longer runs. Habitat alternatives keep ordinary drafts fillable without bypassing compatibility. Drafts use the existing immutable tag-query schedule and shared offer builder; cloning preserves the full schedule and persistent rules.

## Validation

Game and edit-mode test assemblies compile against installed Unity references. Nineteen focused checks pass in a standalone .NET runner, including 1,500 seeded draft paths (50 levels × 30 seeds), realistic roster changes, compatibility, distinct cards, premium guarantees, cloning, Engine growth/reset behavior, boss percentages, overflow, and physical block-clearing geometry. Unity scene-dependent duck-veto coverage is added to the existing power fixture but was not executed in that standalone runner.

Difficulty values are a first balance pass, not measured completion-rate tuning. Full Unity gameplay, phone layout review and completion-rate playtesting remain outstanding.

## Optional directional powers

All campaign levels now use connected matching, retaining each board, minimum match length, budget, gravity and level ID. Former line-only collection rounds now use large-group objectives. Free Play still supports manually selected line rules.

Optional directional tradeoffs and their restrictions are specified in [Powers and scoring](powers.md#optional-directional-matching).

## Positive level effects

Bosses now only penalize. See campaign_bosses.md for the current positive level modifiers and their higher target growth. Bonus block levels grow by 35% per round; ordinary block levels remain at 25%. Bonus points levels add 15 percentage points to their normal or premium growth. Match analytics include the level bonus in rule context.

## Lightning Lanes: second playable challenge

Level briefings show unique mechanics, unusual match requirements and boss effects. They omit the level-description paragraph, default connected 4+ matching, goal/budget/grid/refill/shuffle metrics and the owned-power list. Active penalties are marked NOW; the final boss's name and effect are previewed before the finale. Restricted collection objectives remain visible. Every level briefing includes an automatically looping visual lesson; later lightning rounds also retain a short rule reminder.

Lightning Lanes replaces In a Row at campaign ID 3, after the guided tutorial and Animal Friends. It uses a 7x7 connected-matching board, South refill, 70 seconds per round and seven rounds. Starting target is 350 with 70% growth: the ordinary 55% for this slot plus 15 percentage points for its positive lightning effect.

A matched group containing a continuous horizontal or vertical run of at least five equivalent animals triggers lightning across that entire row or column. Animal-pact matching equivalence is respected. Bent groups and adjacent different animals do not qualify on size alone. Multiple qualifying rows/columns clear together, with shared cells cleared once. Cascades may also trigger lightning. Only genuine matched groups trigger a strike.

A brief blue-and-white zigzag flash lasts approximately 0.28 seconds before scoring and the existing clear/refill animation. It uses non-interactive UI graphics and the board's cancellation lifecycle so restarts and scene changes clean it up. Lightning clears the entire struck line and charges every tile elsewhere on the board. A blue pulse spreads outward from the struck lines, highlighting tiles in distance order. Charged tiles have a pale blue background and contribute twice their share of match points, if they participate in a scoring match. Extra line clears retain normal bonus-clear behavior and do not become separate scoring matches. Charge stays on board spaces through swaps and gravity until those spaces clear; clearing a space removes its bonus. Repeated strikes do not stack beyond 2x. A partially charged group gains a proportional bonus, rounded down, before the other level and boss adjustments.

The briefing automatically loops a visual lesson: a Duck swaps into a straight run of five, the match lights up, a blue-and-white zigzag strikes across the whole row, all seven animals in that row clear, and the remaining rows turn blue with x2 badges. It resets and repeats without a Show lightning button. All eleven campaign levels and Free Play use the same visual lesson player with their own storyboard; see [level lessons](level_lessons.md). The loops use unscaled time, explicitly start despite disabled DOTween autoplay, fit within the briefing card, and stop when the briefing closes.

Round 1 uses the existing standard tactical opening power pool. Its starting board has a centered horizontal three-plus-gap-plus-one arrangement, with a matching animal directly below the gap. One adjacent swap completes five and triggers lightning; the opening has no existing matches. This board setup applies only when Level 3 round 1 is created, including retries; later rounds, shuffles and board recovery use ordinary generation. Every later draft offers two compatible scoring choices and one budget, shuffle or roster tool from the broad catalog. The final boss remains a penalty-only Stretch Giraffe. The lightning rule survives cloning and appears in level briefing and analytics rule context. Other levels do not inherit lightning.

Pure rule/clone tests and seeded draft checks pass; scene animation, phone layout and difficulty still need Unity playtesting.
## Size Matters: third playable challenge

Campaign ID 4 is a points challenge named Size Matters with a base 4 points per tile. Its three opening choices set 3+ matching with a 5+ streak multiplier bonus, 3+ matching with a rapid-fire speed base bonus, or 5+ matching with a 6+ giant group base bonus. See [Powers and scoring](powers.md#size-matters-third-playable-challenge) for the canonical power rules and level settings.

## Clear Commitments: fourth playable challenge

Campaign ID 5 now uses unrestricted block credit and three opening match-size/credit tradeoffs. Its normal default remains four tiles until a foundation is chosen. See [Powers and scoring](powers.md#clear-commitments-fourth-playable-challenge) for the canonical rules, exact opening values and level settings.

## Chain Reaction: fifth playable challenge

Campaign ID 6 replaces Falling Friends with Chain Reaction: 7x7, downward gravity, 70 seconds per round, six rounds, starting target 400 and 70% target growth. Initial player-created matches clear normally but award zero points. Follow-up cascade groups score 4x their normal points, before boss penalties and final power adjustments. This is a fixed factor for every cascade, not an escalating chain multiplier. Initial matches still activate eligible powers and advance their conditions; their zero reward is a level scoring rule, not a score veto.

The opening uses standard starter powers. Later rounds offer two shared scoring choices and one budget, shuffle or roster tool. Wallflower Wolf is the final boss. The concise briefing explains zero initial points, 4x cascades and downward gravity. Existing cascade damping remains: the first refill is normal; later refills reduce the selection weight of newly spawned animals that would create another match. Planned cascades from existing falling animals are preserved. Targets and difficulty need Unity playtesting.

## Garden Plots: sixth playable challenge

Campaign ID 7 uses a 6x7 downward-gravity board, 75 seconds, seven rounds, starting target 400 and 75% growth. A matched group containing any complete 2x2 square plants those four board spaces after that wave scores. Overlapping squares plant each space once. Starting with later waves, tiles on planted spaces contribute twice their equal share of match points; partial coverage is prorated and rounded down. Green backgrounds identify plots, without plant icons. Plots stay at their board coordinates through swaps, gravity, refills and shuffles, then reset when a new round creates a board.

The level supplies 4 base per tile. Openings are +2 base per tile, Square Roots (+6 base per tile for groups containing a square), or Garden Harvest (+6 base per matched planted space). Later drafts use two shared scoring choices and one tool. Overgrowth halves points for groups entirely outside planted spaces; touching any planted space preserves normal score. Newly planted spaces do not protect their own planting match from this boss.

## Alternating Current: seventh playable challenge

Animal Friends and Alternating Current share a large below-grid bonus panel with readable animal icons and names. Temporary feedback (including no-match errors) replaces its contents, using the same background and position, then restores the latest bonus state. There is no separate error box on these levels. Larger tied sets use two rows of icons.

Campaign ID 8 uses a 7x7 downward-gravity board, 75 seconds, eight rounds, starting target 400 and 80% growth. Duck begins as NOW x3, Bear as NEXT x3. The spotlight remains unchanged throughout all scoring waves of a play and switches once after a successful play resolves. Failed swaps do not switch it. New rounds restart with Duck. The same below-grid animal bonus panel as Animal Friends shows the active animal's icon and name with `NOW: 3x points`, plus the next animal's name. Tiles have no bonus highlight or switching animation; players locate the active animal themselves. The logical per-tile 3x score factor is unchanged.

The level supplies 4 base per tile. Openings are Duck x2, Bear x2, or Both Sides (+1 additive multi for matches containing either animal). Later drafts use shared scoring/tools; removal cards are excluded from both scheduled slot types so the pair stays available. Blackout makes any group containing an animal outside Duck/Bear score zero; both members of the pair remain eligible, even when not currently active. The x3 reward is applied only to the active animal's equal share of group score, before boss and final power adjustments. This also defines mixed-animal pact groups.

Both levels and their rules survive cloning and appear in the picker and concise briefing. Standalone rules and draft checks pass; Unity layout, animation and balance playtesting remain pending.

## Border Patrol, Treasure Trail and Changing Tides

These authored additions occupy campaign IDs 9?11, replacing none of the current eight levels. All use connected 4+ matching, six rounds, a starting target of 400 points, 4 base points per tile, and one final-round boss. Every later draft offers two compatible scoring choices and one budget, shuffle or roster tool. Values below are initial tuning and need gameplay balance testing.

| ID | Level | Board | Budget | Target growth | Habitat | Final boss |
|---|---|---|---|---|---|---|
| 9 | Border Patrol | 7x7 | 70 seconds | 60% | Shore | Fence Fox |
| 10 | Treasure Trail | 6x7 | 20 moves | 55% | Giants | Treasure Crab |
| 11 | Changing Tides | 7x7 | 75 seconds | 65% | Rapids | Tide Turtle |

**Border Patrol:** Each genuine matched group lights every outside edge it touches. A corner lights two edges. The group completing all four scores 4x, then the cycle resets; subsequent groups in that wave begin a new cycle. Cascades qualify, extra bonus clears do not. Lit sides have gold border strips and four named progress indicators below the board. The opening choices are Shore Bounty, Corner Cache (12 conditional base points per tile for corner matches), and Giant Bounty. Corner Cache uses the existing structured-power engine and remains available in compatible shared drafts. Fence Fox halves points for groups touching no outside edge.

**Treasure Trail:** A gold frame marks one animal tile. It follows swaps and all gravity directions; clearing it collects one treasure, including eligible extra clears. A new tile is marked after refill. Every third treasure arms one 5x reward for the next successful play and all its cascade groups, rather than the collection play. Failed swaps preserve the reward. Rewards do not stack; collecting another three during a boosted play can arm the following play. The footer shows collection progress or the ready bonus. Openings are High Stakes, Twin Shot, and Color Focus. Treasure Crab halves the whole play's points if no treasure is collected, with boosted plays exempt. While resolving, half points are held back; finding treasure in a later cascade releases those points. The penalty rounds the whole play down once, preserves correct run totals, and cannot overflow.

**Changing Tides:** Gravity starts down and turns left, up, right, down after every four successful plays. The entire cascade chain finishes in the current direction before the turn. Failed swaps and cascade groups do not advance the counter. The first successful play after a turn scores 3x, including its cascades; failed attempts preserve the reward. The footer shows direction arrows, moves until the turn, the next direction, and bonus readiness. Openings are Rapids Bounty, Rapids Amplifier, and High Stakes. Tide Turtle halves player-created group points while leaving cascades at full value.

Each new round creates fresh mechanic state. Shuffles and dead-board recovery preserve round progress and replace the treasure marker after generating the board. Ordinary refills keep grid backgrounds fixed. Bonuses apply before ordinary group boss penalties; Treasure Crab's whole-play penalty accounts for final awarded group points. Cloning preserves catalog settings, mechanics, opening pools, drafts and bosses. Existing campaign unlocks naturally extend through ID 11.

Validation: game, edit-mode tests, and play-mode tests compile against installed Unity references. Ten focused mechanic tests cover border cycles, queued play rewards, failed attempts, rearming, treasure transport in all directions, whole-play penalties and saturation, recovery, boss rules, cloning, and 90 seeded six-round builds. Shared-catalog and matching-default checks pass. The in-editor footer/refill test is compiled but cannot run in a second Unity process while the project is open; visual and balance review remain pending.
