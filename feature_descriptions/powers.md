# Powers and scoring

**Edge Balance:** starts at 0 bonus base points per tile. Each resolved match (including cascades) adjusts its stored bonus by edge-tile count minus interior-tile count, with a floor of 0. Corners count once; the group's net change is applied once, independent of tile order. The match uses the previously stored bonus; the new amount benefits later matches. This bonus carries across rounds and resets for a fresh acquisition/run. It never contributes negative base or subtracts from other powers. Registered in the native catalog and shared editor; level-specific pools can select it explicitly.

Live power names, card text, supported tuning values and level settings now live in [`Assets/Resources/Content/prismatch.json`](../Assets/Resources/Content/prismatch.json). Both Unity and the [local power editor](../tools/power-designer/README.md) read that file. This document owns design philosophy; numeric examples below are explanatory and should be checked against the shared catalog before balancing. Special native mechanics remain in C#; new native powers must also be registered in the shared file or Play/build validation fails. Editor saves apply on the next Unity Play session or build.

Word every power as simply and briefly as possible. Use the fewest plain words that clearly state the trigger, reward and drawback. Prefer numerals and short sentences. For example, "Matches of exactly 4 gain +2 base points." Say "base points per tile" only for bonuses paid on each tile; whole-match bonuses say "base points". Say "gain 2 multiplier" for an additive bonus, "score double points" for a factor, and "your next match" for a queued effect. Growth must state its task, affected matches and duration. Rivalry cards use "Lasts all run" for bonuses that carry across rounds and state the rival animal and minimum bonus of 0. Reference cards in the design studio show the rule, without zero-progress or zero-earned status; the in-game active-power view can still show current progress. The wording audit changes text only, not scoring amounts, conditions, timing or power pools.

Player-facing language should be short and easy for an elementary-school reader. Say what to do and what happens. Prefer "double points", "half points", "in a row", "green spaces" and animal names over "x1.3 multi", "qualifying activations", "counterplay" or "establish the leader". Keep round counts and target growth in their own fields. Keep rounding, tie-break rules, implementation details and repeated instructions in this reference, rather than the main level or boss description. Preserve exact amounts and distinguish base points from final points when it affects a choice. The live boss target should simply say "No points: Duck."

This is the canonical design and behavior reference for player powers, scoring, collection credit, power offers and card wording. Update this document when those rules change; do not maintain parallel specifications in the older power documents. Campaign documents own level layouts and schedules; analytics documents own telemetry and measurement. Neither should redefine power mechanics.

Last reconciled against the implementation and design discussion: October 19, 2026. Balance numbers below describe the current implementation, not validated difficulty targets. This consolidation changes documentation only.

Quick navigation: [design direction](#design-direction-versus-implemented-behavior), [scoring](#scoring-contract-implemented), [current openings](#current-opening-pools-legacy-implementation), [habitat powers](#habitat-scoring-powers), [tradeoffs](#matching-and-roster-tradeoffs), [collection](#clearing-and-collection-powers), [budget](#time-and-move-rewards), [shuffles](#shuffle-powers), [authoring](#authoring-tags-and-compatibility), [wording](#card-wording-and-ui).

## Design principles

- All campaign levels start with a default minimum of 4 matched tiles; explicitly authored powers can change that minimum, such as Size Matters opening foundations. Connected, horizontal and vertical rules default to 4; rare exceptions must be explicitly authored and explained. Matching geometry remains separate from minimum size. Free Play keeps explicit custom sizes (clamped to board dimensions).
- Powers should support different matching strategies and build interactions. Generic powers remain useful alongside specialized powers.
- Most powers should be shared across most compatible levels. Biomes and themes should bias offers rather than exclude most of the common catalog; explicitly authored levels may make exceptions. Goal, mode, animal and matching-rule compatibility still apply.
- Maintain a substantial mix of short-term conditional rewards and long-term earned upgrades. Long-term power should come from harder repeated conditions, not simply accumulating ordinary matches.
- Outside explicitly permitted opening choices or specific levels, a power must not grant unconditional permanent base or multi merely for being selected. Specialized cards should not hide a free base allowance underneath their condition.
- Most scoring powers should reward a specific condition for that match. Permanent base-point or multiplier improvements should be rare, powerful and earned through deliberate play; gradual growth must not become the automatic best choice in every build.
- The level establishes the challenge; its opening draft should offer different ways to approach it. A universal, separate category of starting powers is no longer the intended design.
- Restrictions belong in optional tradeoffs when possible. Campaigns use connected matching; horizontal-only and vertical-only matching can be chosen through powers for a substantial reward.
- Boss effects are penalties only. Positive environmental effects belong to levels, and those levels have higher target growth.
- Powers must offer usable choices for the actual goal, rules, roster and owned build. A tag or scheduled slot cannot override compatibility.
- Distinguish points, physical tiles cleared and objective credit. Improving one does not automatically improve another.
- Explain triggers, rewards, duration and restrictions clearly. Tune strength using playtesting and comparable analytics; performance while owning a power is not its isolated causal contribution.

## Design direction versus implemented behavior

### Shared powers across goals and budgets (implemented November 7, 2026)

Most powers should work with either points or blocks goals and either timed or limited-move budgets. Keep the power's condition and earned progress, and adapt its reward to the active goal or budget. Goal and budget are independent choices.

| Reward in points play | Equivalent in blocks play |
|---|---|
| Base points | Extra objective block credit, without removing additional tiles |
| Multiplier | Physically clear extra tiles |

The shared catalog now gives 53 existing powers block variants: animal base/multi/growth, habitat rewards, Edge Balance, exact/large/small match rewards, and streak/queued rewards. Each retains its native point behavior and identity. Existing level definitions, opening choices, targets, tuning and explicit draft restrictions are unchanged. Blocks compatibility and clearing/credit offer tags expand the available choices; authored levels still control which groups they offer. No per-level copies are needed.

For an animal-growth power configured to earn +1 base point every 10 matched animals, its blocks version earns +1 credit per matching group of that animal; after 20 it earns +2. Existing catalog values remain unchanged (some grant +2 per upgrade). Native animal growth still applies globally in points play; the new block reward applies to that animal's groups. The earning group receives the upgrade, progress includes cascades, and growth carries across rounds.

For adapted base rewards, a per-tile source becomes that amount per group; a whole-match source retains its amount per group. Additive multi becomes the same number of neighboring clears. A factor of N grants N minus 1 clears; queued factors grant these clears on the following match. Fractional clears accumulate per power until whole, carry across rounds, and start fresh for a new acquisition. Queued clears reset at round start. These conversions only apply to newly eligible block variants.

Extra clears can change the board and create cascades. Extra block credit only advances the objective. Restricted animal/shape groups must earn ordinary objective credit to receive a per-match bonus. Bonus credit is added after foundation scaling and before level/boss adjustments. Extra clears use distinct orthogonal neighbors of the original matching group, exclude other matched/already-cleared cells and protected vault tiles, and do not recursively retrigger powers. They earn ordinary eligible bonus-clear credit, never animal-growth progress. Cascades they create are new matches and trigger normally.

Budget rewards should similarly provide seconds in timed play and moves in limited-move play. Tune those amounts separately, along with block credit and extra-clear amounts; these are equivalent reward roles, not a universal numeric exchange rate. Powers whose mechanics require a particular goal or mode may remain specialized.

Budget powers already adapt between time and moves and retain their existing amounts. Goal-specific tradeoffs, roster/base-doubling mechanics, bespoke openings, and score-only shuffle effects remain explicit exceptions. The structured custom-power engine retains its authored goal and reward. The shared editor lists both reward descriptions and allows the adapted powers in either goal's picker.

| Topic | Intended direction / current status |
|---|---|
| Opening drafts | Level-specific openings are now implemented for Animal Friends, the first challenge after the guided tutorial. Other levels still use legacy opening pools. Retain generic powers while retiring the universal starter category. |
| Existing tactical powers | Retained as opening-only choices. They cannot return in ordinary later drafts; a specific level must explicitly opt into starter rewards. |
| Baseline scoring | Animal Friends supplies 4 base points per tile so its opening powers can be pure animal multipliers. Other levels still rely on powers for base points; a universal baseline remains an open decision. |
| Opening viability | Until baseline scoring is decided, points openings must provide a scoring foundation. Offering only utility or multiplier effects can leave a run unable to score. |
| Per-level strategy | Themes exist, but the broader progression, power-strength budget and difficulty curve still need a design pass. |
| Conditional versus permanent rewards | Agreed direction: conditional base/multiplier rewards form the majority of scoring powers. Permanent improvements are rare and require meaningful behavior changes. Current universal growth openings and guaranteed premium Engines do not yet follow this direction. |
| Shared pools | Agreed direction: most powers belong to most compatible levels, with themes as a preference. Most current campaign scoring slots still require habitat tags. See the [dated power audit](power_audit_2026-10-19.md) for measured reach and implementation gaps. |

Do not describe the new opening-draft model or baseline scoring as shipped behavior. Do not delete existing tactical powers merely because their classification is changing.

## Scoring power philosophy (agreed direction; implementation pending)

The central rule is: **a power should change how the player wants to play. Ignoring its condition should provide no reward from that effect.** A card's value should depend on the player's choices, the level, the board and the rest of the build.

### Conditional rewards are the norm

The catalog should contain many distinct immediate and long-term strategies. Rarer earned-growth offers need not mean a tiny catalog of long-term designs. Exact short/long offer ratios and rarity rates remain undecided. A general match counter is not a sufficiently specific earning condition merely because it takes several matches to complete.

Most scoring powers should add base points or additive multiplier only when a stated condition is satisfied, for that matching group. Conditions can concern animal type, exact length, minimum length, geometry, board position, sequences, streaks or other deliberate actions. Avoid giving every specialized card an unconditional base-point allowance or automatic permanent growth as well.

Examples of the intended design space, not finalized cards:

| Condition | Reward style |
|---|---|
| Match exactly 4 blocks | +3 base points for the qualifying match |
| Match a particular animal | Base points or multi for that animal's qualifying match |
| Match on an edge or contain a shape | Base points or multi for the qualifying match |

Card text must distinguish **base points per match** from **base points per tile**. The exact-four example's reward unit still needs confirmation before implementation; do not silently multiply its proposed +3 by the number of tiles. Conditional multi is additive unless explicitly written as a multiplicative factor.

### Permanent improvements are rare earned investments

Permanent base or multi can produce powerful builds, but should improve gradually through a demanding condition rather than reward ordinary play automatically. Permanent describes the duration of an earned bonus, not permission for passive growth. A player who does not pursue the condition should earn no growth. Already-earned permanent bonuses can still benefit later matches that do not meet the earning condition.

User-proposed examples:

- Match lengths **4, 5 and 6** to earn **+1 additive multi on all future matches**.
- Make a **straight line of 5 twice consecutively** to earn **+1 additive multi on all future matches**.

These are design examples, not shipped effects. For the length challenge, ordering, whether intervening matches break progress, and whether each completed set can repeat remain to be decided. For the five-line streak, its orientation, failed-swap behavior, multi-group-swap behavior and cascade eligibility also need explicit rules. Neither example grants its permanent bonus to the group that completes the challenge: the stated reward is for future matches. Round/retry persistence and duplicate stacking need to be specified when authoring the actual cards.

### Balance against immediate conditional rewards

Rarity alone is insufficient. Compare the immediate reward forgone, the effort to complete the condition, achievable activations, rounds remaining, and synergy with the rest of the build. A permanent-growth choice should sometimes be worse than a reliable conditional reward, especially when its task conflicts with the level or there is little time left to benefit.

No rarity percentages, reward budgets or activation rates have been agreed yet. Do not assume that the current Premium tag or guaranteed Engine drafts implement rarity. The +4-base tactical powers are now restricted to openings unless a level explicitly opts in. Unconditional habitat base allowances have been removed, and Momentum Bonus, Snowball and Combo Craze are no longer offered by the ordinary catalog. The difficulty of remaining earned-growth conditions and guaranteed Engine schedules still needs review.

## Scoring contract (implemented)

For each resolved matching group:

1. Null or empty groups score zero.
2. Check power and boss score vetoes first. A vetoed match scores zero, preserves any queued next-match multiplier and does not advance scoring-power counters.
3. Sum the level's explicit base points (zero unless configured) and signed power contributions, then clamp the total to zero. Without any base contribution the match scores zero.
4. Start additive multiplier at 1, add integer and fractional bonuses, and multiply by current-match factors and the queued factor from the prior match. Powers may queue a factor for the next non-vetoed scoring group.
5. Multiply base points by the combined multiplier and round down. Large calculations saturate at the supported integer maximum.
6. Apply lightning charge: each charged tile doubles its equal share of the group score. Multiply by `(tile count + charged count) / tile count` and round down. Fully charged groups score 2x; charges do not stack.
7. Apply the positive level bonus, if its condition is met, rounding down.
8. Apply boss penalties. Quarter penalties retain 75%, rounded up; other percentages use their stated rounding.
9. Apply final score adjustments such as animal-pact and roster tradeoffs, rounding down at each adjustment.
10. Record round animal history after evaluation, including zero-point groups. Adjacent bonus clears do not enter this history.

Example before level, boss and final adjustments: four tiles with +4 base per tile, +0.5 additive multi, and a x4 factor score `floor(16 * (1 + 0.5) * 4) = 96`.

Base points and additive multiplier are different growth axes. A multiplier alone cannot rescue a zero base score. HUD stat inspection must read declared effects rather than call scoring hooks, which may mutate counters. Score breakdown entries explain a calculation; they are not independent causal contributions of each power.

## Scope, duration and lifecycle

Player-created matches are the default scope in card wording. Implementation hooks differ between power families: tactical starters and budget rewards explicitly reject cascades as triggers, habitat effects may include cascades, and generic scoring hooks run for resolved groups unless the power excludes them. Document exceptions on the card; do not assume all powers share trigger semantics.

- Owned powers ordinarily last through the level/run. A fresh clone starts without earned progress or growth.
- Round starts reset the counters specified by each family. Tactical permanent multiplier growth survives rounds; its streak/progress counters reset.
- Retry restores the retained build and roster according to the run lifecycle. Resettable round progress and allowances restart; do not assume that all earned state in every power is rolled back.
- PowerManager registers dependencies and applies a power when acquired. Active powers and boss powers are tracked separately. Removal and clearing update those lists; board/roster/rule updates belong to the controller.
- Failed swaps consume a move in Limited Moves. Cascades consume no extra moves. Whether a failed swap advances a particular counter is stated below.

## Current opening pools (legacy implementation)

Animal Friends, Size Matters and Clear Commitments override this behavior with their own opening drafts, described below. Other levels still draw three compatible points choices from eight tactical powers when no powers are owned. Block goals have a separate opening pool. These are current mechanics, not a requirement for future level design.

### Animal Friends: first playable challenge

Current draft configuration: every round uses one three-card offer filtered only by Animal. The shared catalog owns these rules; the old fixed Double opening and add/remove/specialize roles below are superseded. Normal roster, mode, match-rule, ownership and starting-power restrictions still apply.

The guided tutorial remains campaign ID 1. Animal Friends replaces the old Connected Crew at ID 2, preserving unlock order. It is the first actual challenge, referred to as level 1 in the design discussion.

- This level supplies 4 base points per matched tile, shown in the base stat display. This is not a permanent-base power and does not grant a universal baseline to other levels.
- Inherent level bonus: matches containing a currently least-matched active animal receive one multiplicative x2, stacking with additive multiplier bonuses and other score factors. Use the round's tile counts before scoring this group, then record it. All least-matched ties share the bonus, including zero-count animals at round start. Adding/removing an animal immediately changes the eligible roster; reset history each round/retry. A panel below the grid shows every currently boosted animal with its icon and name. The boss veto still takes precedence.
- Opening draft: three distinct animals currently in the roster. Choose one Animal Double power; any matching group containing that animal doubles its accumulated base points before match multipliers. No permanent growth is attached, and the same animal's Double cannot repeat.
- Every later draft has exactly one animal addition (x1.3 overall), one compatible animal removal (x0.5 overall), and one random eligible animal specialist. The third option excludes roster changes, broad pattern effects and matching-rule changes.
- Four rounds, 6x7 board, 60 seconds per round, starting target 400, +75% growth (60% baseline plus 15 percentage points for the inherent bonus). With seven initial animals and ten total species, three subsequent drafts keep add/remove choices possible even on all-add or all-remove paths. Owned specialists protect their animals from removal. The shared level editor can override the default offers.
- Finale: Popularity Tax makes matches containing the most-matched animal in the current round worth zero points. Count actual matched tiles, including cascades and zero-point groups, not adjacent bonus clears. Evaluate the current group against prior history; update the target afterward. No target exists before the first group. Ties select one leader by stable enum order. Round starts/retries reset this history.
- The boss button shows the blocked animal and `0 points`; the active boss description includes its matched-tile count. The target follows the current roster and updates after every resolved group.

The +30% and -50% roster factors compound with other multipliers and retain the existing final-score rounding behavior. They change overall scoring, not only the newly added/removed animal's scores.

Every tactical points opening gives **+4 base / tile**, plus:

| Power | Player-created trigger | Permanent additive multi |
|---|---|---|
| Box Builder | Each complete 2x2 square contained in a group | +0.5 per square |
| Plus Architect | Each center with four orthogonal neighbors | +0.5 per plus |
| Big Game | A group of 6+ tiles | +0.5 |
| Four by Four | Four consecutive successful swaps whose player-created groups are all exactly four | +1 |
| Opening Gambit | First player-created group each round has 6+ tiles | +1 |
| Double Play | Both swapped destinations participate in matches; once per swap | +0.5 |
| Variety Show | Three animal switches | +0.5 |
| Border Patrol | Three player-created edge matches | +0.5 |

Growth applies to the triggering group and later groups, including cascades. Cascades never advance or reset these tactical counters. Non-four player-created groups reset Four by Four; failed swaps do not count. Opening Gambit is consumed by the first player-created group even if smaller than six. Repeating an animal does not reset Variety Show progress. Mixed pact groups use their first animal for this comparison. Squares and pluses count distinct anchors, including overlapping shapes. Double Play can use one group or two separate groups; unrelated automatic groups do not qualify. Square and plus powers require connected matching.

Flat generic foundations, including More Power!, are opening-only and are excluded from the ordinary reward list. Conditional animal base/multi and earned animal growth remain ordinary rewards. Legacy Precision Start and Momentum Start still exist in code but are not in the standard tactical opening pool. Their names do not establish a separate design category.

### Size Matters: third playable challenge

Campaign ID 4 is a points challenge named Size Matters. Level base points are inherently 4 points per tile. Its opening always offers these three foundations:

| Power | Minimum matched tiles | Earned Upgrade Rule |
|---|---:|---|
| Small Steps | 3 | Every 4 matches of 5+ in a row add +1 permanent multiplier (cascades ignored) |
| Steady Matches | 3 | 5 matches within 5 seconds add +2 permanent base points per tile (cascades ignored) |
| Big Commitment | 5 | Every 2 matches of 6+ tiles add +1 permanent base point per tile (cascades ignored) |

Each changes the minimum of the active matching rules without forcing a line orientation, lasts through the run, and cannot be offered after any power is already owned. Later drafts explicitly exclude foundations. The board is rebuilt immediately after the opening choice to ensure a valid board under the new threshold; later rounds regenerate with the retained power's rule. Clones preserve the selected values. Icons show the required group size and reward badge (+1M, +2B, +1B).

The level uses a 6x7 connected board, 70 seconds per round, base 4 points per tile, starting target 400, eight rounds and +60% target growth. Later drafts offer shared scoring and tool choices. Wallflower Wolf is its penalty-only final boss.
### Clear Commitments: fourth playable challenge

Campaign ID 5 is an unrestricted block-credit counterpart to Size Matters. Its opening always offers:

| Power | Minimum matched tiles | Goal credit per matched tile |
|---|---:|---:|
| Small Clears | 3 | 1 |
| Steady Clears | 4 | 2 |
| Big Clears | 5 | 4 |

Any animal and any qualifying connected shape count. This level deliberately has no animal/shape collection schedule. The selected power changes the match minimum for the run, supplies no points or multiplier, and cannot be offered after a power is owned. The board rebuilds under the selected minimum. Its icon uses the required group size and a credit badge; the target HUD shows credit per tile.

Compute eligible matched-group credit, apply the foundation's factor, then any level bonus and boss penalty. Zero eligible credit remains zero and integer totals saturate safely. Physical tile counts are never multiplied. Adjacent bonus-cleared tiles keep their normal eligible credit and receive no foundation multiplier. Other levels retain their existing behavior without a block foundation.

The level uses a 7x7 board with South refill, 80 seconds per round, starting target 60, nine rounds and +25% target growth. Later drafts offer two clearing choices and a utility/clearing alternative; additional clearing keeps drafts fillable if unique tools are exhausted. Wallflower Wolf is the final boss. The block credits were reduced to 1/2/4 independently of the points experiment. Comparative playtesting is still needed; equal difficulty is not established.
## Habitat scoring powers

| Habitat | Qualifying condition |
|---|---|
| Shore | Touch an outer board edge |
| Garden | Include a complete 2x2 square; connected matching required |
| Rapids | Cascade group |
| Giants | Group of 6+ tiles |
| Menagerie | Animal differs from the previous resolved group |

Each habitat has three variants:

- **Bounty:** +10 base / tile only when its condition is met; zero base otherwise.
- **Amplifier:** All five habitats grant +5 additive multi for a qualifying group. Their block versions grant 5 extra clears.
- **Engine:** +0.5 permanent additive multi per qualifying player-created group. Rapids earns growth on cascades. Menagerie instead grants +1 permanent additive multi after matching every animal in the current roster, then starts a new set. Its block version earns +1 permanent extra clear per set. Engines carry the Premium tag.

Engine growth survives rounds and resets on a fresh run. Menagerie Engine also retains partial set progress across rounds. Only player-created groups advance its sets, and mixed groups count each animal present. Required animals come from the live roster on each match, so additions join the current set and removed animals cease to be required. Menagerie Bounty and Amplifier compare with the previous animal, reset that comparison each round and include resolved cascade groups.

## Matching and roster tradeoffs

### Optional directional matching

| Power | Restriction | Reward |
|---|---|---|
| Sideways Thinking | Horizontal-only matching for the rest of the run | x4 points |
| Skyward Thinking | Vertical-only matching for the rest of the run | x4 points |

These replace connected matching with the selected line rule and preserve the existing minimum match length. They are points-only, cannot duplicate or combine, and are excluded when they would disable an owned shape power. Later offers respect the changed rules. They can appear in non-Garden campaign tool slots and the Free Play power pool; Garden schedules exclude them to preserve square progression. Campaign levels themselves no longer require directional-only matching. Free Play can still explicitly configure line matching.

### Animal pacts

Current pairs: Duck + Walrus, Bear + Sloth, Chicken + Giraffe. Both animals must be present. Pacts are points-only; duplicate and overlapping pairs are excluded.

The pair matches as one type for connected and line matching, legal-move detection, generation and cascade damping. Actual identities remain unchanged for animal bonuses. Any group containing either animal earns half its final score, rounded down, even if the group contains only one of the paired species.

A newly selected pact takes effect immediately; resulting matches resolve on the next move rather than forcing an automatic reshuffle.

### Add/remove animals

- **Welcome Animal:** add an absent animal to refills; all match scores receive a x1.3 final adjustment.
- **Farewell Animal:** remove a present animal; all match scores receive a x0.75 final adjustment (25% fewer points).

These are points-only tradeoffs. Each animal can be affected by a roster power only once per run. Removal requires more than three active types and cannot remove an animal needed by an owned specialist, pact, animal-clearing effect or collection requirement. Added animals enter subsequent refills; removed animals already on the board are replaced in place. Rebuilding the roster uses the original pool, required animals and retained powers.

Moose, Panda and Elephant are implemented alongside the original seven animals. Required collection animals can be added outside that original roster.

### History-based animal bonuses

- **Change of Taste:** least-matched animals receive +10 base / tile; most-matched animals receive -5 base / tile.
- **Hidden Favorites:** least-matched animals receive +5 base / tile.

Each is points-only and individually non-repeatable; both can coexist. Shared round history counts matched tiles, including cascades and vetoed scores, but not adjacent bonus clears. Evaluate against history before the current group, then record that group once. Only animals in the active pool participate in rankings. Ties all qualify; when all counts are equal, apply the bonus only. Mixed groups use actual tile identities. Signed base contributions are combined before clamping so penalties do not depend on registration order.

## Clearing and collection powers

Physical clears remove tiles. Goal credit advances a collection objective. Points use the separate scoring pipeline. A physically cleared tile may give zero points and zero collection credit.

### Adjacent clearing

| Power / condition | Extra adjacent clears |
|---|---|
| Helping Hand (opening only) | 1 per matched group |
| Big Sweep (opening only) | 2 for groups of 4+ |
| Chain Reaction (opening only) | 2 per cascade group; not the initial swap wave |
| Animal Sweep | 2 when matching its animal |
| Shore Sweep | 2 when touching an edge |
| Garden Excavator | 2 when containing a square |
| Trail Blazer | 2 for groups of 6+ |

Trail Blazer is the internal LongLine variant; its trigger is group size, not a straight-line requirement. These are block-goal effects, not point multipliers.

Extra clears per activation have no gameplay cap, including queued and growing rewards. Fractional clear progress carries forward. Choose real orthogonal neighbors deterministically. Effects can stack only while unused neighboring cells exist. Never count matched or already-selected cells twice. Extra clears award their normal points or eligible block credit. They do not trigger other powers, advance animal history or counters, or contribute to match size and shape unless a power explicitly states that it applies to extra clears. They animate and refill with the original clear. Newly formed matches resolve under the normal cascade rules.

### Objective credit, transformation and admission

Animal objectives count only eligible animals. Shape objectives require a qualifying matched group. Both restrictions can be combined. The entire group need not exactly equal the shape; it must contain it.

| Family | Effect |
|---|---|
| Animal Collector | Eligible matched tiles of its animal give +1 additional credit each; copies stack additively |
| Animal Bloom | Each player-created matched group transforms up to two adjacent surviving tiles into its animal |
| Also Count Animal | Its animal becomes eligible alongside the primary animal and is added to refills if absent |
| Shape Dividend | Eligible groups containing its shape give +1 additional credit per eligible matched tile; copies stack additively |

Every animal can be the focus; these are not duck-specific mechanics. Five non-Any shapes have Dividends. Collectors do not make an ineligible animal count: admission does. Admission never bypasses a shape requirement. After admission, animal-round offers can include that animal's Collector and Bloom.

Blooms operate only on the initial player-created wave. They skip empty/out-of-bounds cells, already-target animals, all cells clearing in that wave and survivors already claimed by another Bloom. Cascades, bonus clears and transformations cannot trigger additional Blooms. A transformed survivor earns no immediate credit.

Matched groups receive objective credit and applicable Collector/Dividend bonuses, then the level bonus, then boss penalties. Physical-clear statistics stay separate. Adjacent bonus cells count only when their actual animal is eligible; they get no Collector/Dividend match bonus. They get zero credit for shape-restricted objectives because they were outside the matched shape. A count-only callback cannot bypass a restricted objective.

See [block collection levels](block_collection.md) for the campaign's objective schedule rather than another definition of these mechanics.

## Time and move rewards

These support both goals and both modes; the current picker offers them after the opening selection.

| Power | Player-created qualifying swaps per reward |
|---|---|
| Edge Explorer | 3 groups touching an outer edge |
| Square Deal | 2 groups containing a complete 2x2 square |
| Big Breather | 2 groups of 6+ distinct tiles |

Each completed set gives **3 seconds** in Timed or **1 move** in Limited Moves. Count at most once per swap per power, regardless of group/square count. Matches need not be consecutive. Cascades and bonus clears do not count; score penalties do not prevent progress. Rewards have no per-round payout cap. Progress and displayed rewards reset each round/retry. Duplicate copies are excluded. Square Deal requires connected matching.

Move rewards apply before move-exhaustion checks; time rewards cannot revive an expired round. Cards show only the current mode's reward.

## Animal spawn powers

Each animal has a Magnet power (Bear, Sloth, Chicken, Duck, Hippo, Giraffe, Walrus, Moose, Panda and Elephant). It gives that animal spawn weight 2 instead of the normal 1, without adding or removing any species. With seven unboosted species, a Duck Magnet changes the unconstrained Duck draw chance from 1/7 to 2/8; owning Duck and Bear Magnets gives each 2/9. These are relative weights, not guaranteed tile percentages. Initial-board match prevention, legal-move seeding and cascade damping can affect the final board distribution.

Magnets support points and block goals in both budget modes. Each species' magnet can be owned once; different species combine by independently raising their weights to 2. A magnet is only offered while its animal is present. Removing that animal conflicts with its magnet in either acquisition order. Magnets do not change current tiles, matching rules, base points, credit per tile or the number of distinct animals.

Weighting applies to new-round generation, ordinary refills in every gravity direction, player shuffles, automatic board recovery and replacement spawns for removed animals. Initial generation uses weighted draws with rejected match-making candidates removed from that cell's choices. Later cascade damping multiplies spawn weight by the existing match-making penalty, rather than replacing the magnet's effect. Legal-move repair still takes priority over randomness. Acquired magnets persist through rounds and retries as other owned powers do; a new run clears them.

Magnet cards show their animal and a MORE badge. They are ordinary utility/animal/roster powers in both goal catalogs. Block utility slots include roster options; shuffle levels retain their guaranteed shuffle slot and offer a separate progress-or-roster choice in rounds 4–6. Shared metadata includes the exact animal filter so authoring can target a species' magnet alongside its other powers.

## Shuffle powers

Shuffle powers appear after the opening selection. All support both modes; Shuffle Spark is points-only and Clean Slate is blocks-only. The other shuffle powers support both goals. Each type can be owned once; different types combine.

| Power | Effect |
|---|---|
| Shuffle Streak | Every 5 player-created matches earns a free shuffle; once per swap, no cascade triggers, no earning/storage cap |
| Half-Price Shuffle | Paid cost becomes 2.5 seconds instead of 5, or 1 move instead of 2 |
| Fresh Start | First 3 shuffles each round are free |
| Square Ticket | Every 2 player-created matching moves containing a complete 2x2 square earns a free shuffle; connected matching required |
| Big Ticket | Every 2 player-created matching moves containing a group of 6+ distinct tiles earns a free shuffle |
| Shuffle Spark | A manual shuffle arms double points for the next successful play, including all its player-created groups and cascades |
| Clean Slate | A manual shuffle arms 8 extra block credits for the next successful matching move; paid once, regardless of group or cascade count |

Use free allowances before charging a discounted cost. Spend Fresh Start's allowance before stored earned uses. A successful manual shuffle spends one use or the displayed cost; blocked presses spend nothing. Automatic dead-board refreshes are free and consume no uses. Round starts/retries reset progress and unused earned uses and refill Fresh Start to three. Clones start fresh. Show the next shuffle cost, available uses and progress in the UI.

Square Ticket and Big Ticket count at most once per swap per power. Their qualifying moves need not be consecutive; cascades and bonus clears do not earn progress. Different ticket powers can progress on the same move. Stored free uses have no cap; spending priority is Fresh Start, Shuffle Streak, Square Ticket, then Big Ticket.

Paid and free manual shuffles both arm Shuffle Spark/Clean Slate. Repeated shuffles replace the pending reward without stacking it. Failed swaps preserve the pending reward; the first successful player-created group identifies the rewarded play. Spark boosts every group from that play, including cascades, and expires even if a boss blocks its points. Slate grants its credit once after foundation credit, before level bonuses and boss penalties; it never clears tiles or recursively triggers powers. Automatic recovery and other board resets do not arm rewards. Round starts, retries and fresh clones clear pending rewards. The shuffle feedback message announces when the next-play reward is armed; active power descriptions show pending state and ticket progress.

## Bosses and positive level effects

Bosses can reduce or veto rewards but never increase them. Duck Customs vetoes duck-containing groups' points, not physical clears or block credit. Current pattern and timing bosses retain normal value when their conditions are satisfied and impose penalties otherwise. See [campaign bosses](campaign_bosses.md) for the penalty roster.

Positive level effects are independent of player powers and remain active through the finale. Lightning Lanes clears qualifying lines and charges the rest of the board for 2x points; see [its level specification](campaign_identities.md#lightning-lanes-second-playable-challenge) for triggering, charge lifetime, highlighting and tutorial rules. Its opening uses the current standard tactical pool; later drafts use broad scoring/tool choices.

Reward-percentage level effects:

| Level effect | Reward |
|---|---|
| Rapids cascade bonus | Cascades: +25% goal reward |
| Selected Giants levels | Groups of 6+: +50% goal reward |
| Selected Garden levels | Groups containing a square: +25% goal reward |

Apply these after power-derived rewards and before boss penalties, rounding down. They cannot turn zero credit into positive credit. Bonus levels add 15 percentage points to points-target growth or 10 to block-target growth. Ordinary block growth remains 25%; bonus block growth is 35%. This is provisional balance, not a universal conversion formula for future bonuses. Level briefings and analytics rule context identify the effect separately from bosses.

## Authoring, tags and compatibility

### Shared edits and level-specific power copies

The level editor links each displayed power to **Edit everywhere** or **Edit a copy for this level**. Global edits change that definition wherever it is used. A level copy keeps the same mechanic and compatibility requirements, with independent supported tuning values, name and wording. Native copies store a `basePowerId`, `levelId` and separate definition ID; they never register a second native handler key. Structured copies retain their structured rule fields.

Each level's `powerOverrides` replaces its original power with its active copy in opening/reward pools, native draft queries and round offers. Copies are excluded from ordinary global catalogs and other levels. Copy creation and the level link save atomically. Cloning preserves the copied definition and its parameters while resetting runtime progress as usual. Later global edits do not change an existing copy's values. Recopying a power rewires that level's previous explicit references. Starting foundations expose match minimum and points/block credit per tile through the same validated tuning system.

The following section specifies the current authoring API. In particular, the legacy Starter tag still exists, but explicit OpeningOnly metadata and the actual round enforce opening restrictions. The pool can still vary by level.

Level authors can define offers without changing the picker or introducing a new theme enum. CampaignLevel and FreePlayConfig accept an optional immutable LevelPowerOffers schedule. Existing campaign themes are convenience presets translated into exactly the same definitions; legacy roundOffers tag schedules remain supported.

This is the level-definition API, not a new player-facing Free Play editor. The Free Play UI retains its existing controls.

### Opening-only eligibility and explicit level exceptions

PowerUp.OpeningOnly marks tactical starters, flat base/base-plus-multi foundations, the custom match-size foundations and the three original block starters. They are eligible only in round 1 with an empty owned build. The normal picker, scheduled offer builder, fallback pool and controller registration all enforce this policy using actual round context. Removing all powers later in a run does not make that later round an opening.

CampaignLevel accepts allowStarterRewards: true for an explicitly authored special level. This allows eligible starter powers in later scheduled slots and survives cloning; ordinary goal, roster, rule and power-conflict checks still apply. No existing campaign level currently enables it. Merely requesting the Starter tag does not bypass the policy. Match-size foundations retain their separate one-opening-choice conflict rule.

PowerOfferBuilder.Build/IsEligible and PowerUpSelector.BuildChoices accept round and allowStarterRewards context. Preview callers must supply the actual round when validating later offers; the backwards-compatible default of round 1 represents an opening preview. Legacy scoring/pattern presets now request effect tags rather than requiring the Starter tag. Ordinary fallback rewards come from ordinary point/block pools instead of reintroducing starters.

More Power! remains available for explicit opening authoring but is not in PowerUpLibrary.GetAll(). Momentum Bonus, Snowball and Combo Craze retain legacy implementations/tests but are removed from the live catalog and ordinary pools because their growth comes from unrestricted match counts. Earned permanent upgrades that require a specific animal or pattern remain available. Structural roster/matching tradeoffs and explicit level bonuses retain their existing rules.
### Tags describe effects; compatibility is separate

Queries use PowerUp.OfferTags(mode). They combine declared Tags with base-point/multiplier effects and mode-dependent budget rewards.

| Tags | Meaning |
|---|---|
| Scoring / Clearing | Points effects / extra block clearing |
| Animal | Involves particular animals; does not imply a roster change |
| Roster + AddAnimal / RemoveAnimal | Changes the refill roster |
| AnimalPair | Lets two animal types match together |
| Budget | Earns more of the round budget |
| TimeReward / MoveReward | Budget reward in the supplied mode; never both for the existing budget powers |
| Shuffle | Improves shuffle behavior |
| Starter | Legacy pool tag; OpeningOnly and the current round enforce acquisition restrictions |
| Pattern, Square, Edge, LargeMatch | Matching triggers; combine with reward tags to target a strategy |
| Growth, Tradeoff, Matching, Utility | Existing gameplay categories |
| BasePoints / Multiplier | Declared scoring stat effects |
| Collection / GoalCredit | Collection-related effects / extra objective credit |
| Transform / Admission | Changes nearby survivors / adds another eligible collection animal |
| Cascade / Variety | Cascade or animal-switch strategy |
| Premium | Stronger reward tier used by high-stakes schedules |
| AnimalBaseDouble | Animal matches double their base points before match multipliers; used by Animal Friends' opening draft |

TimeReward is contextual metadata, not a time-only compatibility declaration. A BudgetPower supports both modes but earns time in Timed and moves in LimitedMoves. Querying TimeReward in LimitedMoves produces no candidates.

All queries still enforce goal, mode, match-rule compatibility, the actual animal roster, and conflicts with active powers. Tags never bypass those checks. New powers should declare their categories, specific effects and trigger tags, implement context-dependent OfferTags only when necessary, and remain responsible for their compatibility methods.

### Query semantics

- All: every specified bit must be present.
- Any: at least one specified bit must be present; None means no OR constraint.
- None: none of these bits may be present.
- The three clauses combine with AND.
- Contradictory required/excluded tags are rejected when constructing the query.

For example, an animal-related tradeoff excluding roster changes:

```csharp
new PowerTagQuery(
    all: PowerTags.Animal | PowerTags.Tradeoff,
    none: PowerTags.Roster)
```

### Authoring exact rounds and card counts

This schedule offers three roster changes on round 2 and two scoring alternatives plus one earned-time alternative on round 4:

```csharp
var roster = new PowerOfferDefinition("RESHAPE YOUR ZOO", new[] {
    new PowerOfferSlot(new PowerTagQuery(
        all: PowerTags.Roster,
        any: PowerTags.AddAnimal | PowerTags.RemoveAnimal), count: 3)
});

var pointsOrTime = new PowerOfferDefinition("POINTS OR MORE TIME?", new[] {
    new PowerOfferSlot(new PowerTagQuery(
        all: PowerTags.Scoring,
        none: PowerTags.Tradeoff | PowerTags.Budget), count: 2),
    new PowerOfferSlot(new PowerTagQuery(all: PowerTags.TimeReward))
});

var schedule = new LevelPowerOffers(
    new Dictionary<int, PowerOfferDefinition> {
        [2] = roster,
        [4] = pointsOrTime
    },
    reservedFromOrdinaryOffers: new[] {
        new PowerTagQuery(all: PowerTags.Budget)
    });

var level = new CampaignLevel(52, "Zoo Decisions",
    "Reshape the zoo, then choose points or time.",
    new FreePlayConfig(levels: 5, mode: FreePlayMode.Timed),
    bossFinal: false, powerOffers: schedule);
```

Use separate AddAnimal and RemoveAnimal slots to guarantee a mix rather than merely allow either. For example, count 1 for AddAnimal and count 2 for RemoveAnimal. Use MoveReward for a moves-only offer, or Budget to adapt to either mode.

One to three total cards are supported by the current picker. Counts outside this range, null definitions, empty labels, contradictory queries and rounds outside the level are rejected. Supply one schedule source: explicit powerOffers, legacy roundOffers, a theme preset, or the wrapped FreePlayConfig schedule.

Unspecified rounds use ordinary offers. Reservations apply only to ordinary rounds, including fallback candidates; they cannot suppress an explicitly scheduled slot. Schedule dictionaries and slot lists are copied, their contents are immutable, and cloning preserves the schedule.

### Repeats and shortages

Each slot can choose PreferNew (default) or NewOnly. PreferNew prioritizes unowned names but only permits repeats when the power's own compatibility rules allow them. NewOnly also checks active powers, even if the caller's selected-name set is incomplete. Cards within one offer always have distinct names.

The builder assigns candidates across all slots together using augmenting paths. A broad slot therefore cannot steal the only eligible power needed by a narrow slot when a complete assignment exists.

ShowAvailable (default) returns as many distinct valid cards as possible. RequireComplete returns no cards unless the whole offer can be filled. Neither policy substitutes unrelated powers. In the live picker an incomplete offer logs its missing-slot diagnostics; an empty offer continues the round with existing powers and a player-facing explanation.

Reservations help avoid exhausting special categories before their scheduled rounds. They cannot guarantee availability after arbitrary roster changes or active-power conflicts; inspect diagnostics for those cases.

### Preview and verification

Call PowerOfferBuilder.Build with the definition, PowerOfferBuilder.Catalog(), selected names, actual roster, a seeded Random, goal, mode, matching rules and active powers. It returns Choices, IsComplete and per-slot Diagnostics. It clones returned powers and does not mutate the catalog, roster, schedule or active build. This is the same builder used in gameplay.

Validate using realistic states at each scheduled round, not only an empty starting build. Offer checks cover explicit add/remove quotas, overlapping slot assignment, contextual budget tags, repeat policies, shortages, reservations, schedule copying and clone preservation. These checks validate offers, not gameplay difficulty.

Animal match multiplier powers grant +5 multiplier to matches of their named animal. This is an additive match bonus.

## Card wording and UI

Keep every power description as simple and short as possible. Include what a player needs to choose and use it. State the trigger, exact reward, affected animal and meaningful drawback. Avoid repeated duration clauses and explanations of internal counters. Keep edge cases in this reference. Show current bonus and progress separately on active cards.

Animal Rivalry counts matching groups of any size, including cascades. Bonuses apply to later matches of the named animal and carry across rounds. Each rival match reduces the earned bonus by 1, with a floor of 0, and preserves progress toward the next bonus.

Every current catalog power has explicit icon metadata instead of falling back to the unknown POWER placeholder. Animal Double, animal collection and roster effects use their actual animal artwork; pacts show both animals. Habitat and shape effects use edge grids, squares, pluses, large groups, cascade steps or animal-switch diagrams. Shuffles use opposing arrows; directional powers show the relevant line orientation. Reward badges remain visible on geometric icons as well as animal icons. Permanent upgrades carry an additional growth marker. Animal collection badges distinguish credit, transformation and admission; points/base bonuses must not use a multiplier symbol. Selection cards and active-power cards share PowerUpIconLibrary, so their visual meanings remain consistent. Unknown future power types still have a neutral fallback and should gain an explicit mapping before joining the catalog.

The six-page visual tutorial includes an interactive scoring lesson after matching and before power drafting. It explains base points times multi using illustrative powers: +4 base per tile and +1 additive multi for exactly four tiles. Switching between four and five tiles shows 32 versus 20 points; the nonqualifying match keeps the starting x1 multiplier. It also distinguishes block-objective credit from points. These are teaching examples, not additional catalog powers or a change to baseline scoring.

Use the same trigger/reward vocabulary on selection cards, active cards and stat breakdowns:

- `+4 base / tile.` for per-tile base points.
- `+0.5 multi` for additive multiplier; never `+0.5x`.
- `x2` or `x4` only for multiplicative factors.
- `permanent` for growth lasting through the level; `this match` or `next match` for temporary effects.
- `Trigger: effect.` for rules, with concise `Earned:`, `Progress:` and `Streak:` state.

Use concrete conditions and numerals. Player-created matches are the default wording scope. Omit sentences saying falling matches or cascades do not count. Explicitly state when cascades count toward a bonus or trigger. This wording default does not change individual power mechanics. Omit repeated duration boilerplate but retain scope exceptions and tradeoffs, especially run-long matching restrictions. Do not add flavor prose that hides mechanics. Fractional multiplier displays support up to two decimal places. Cards identify POINTS, BLOCKS or ANY GOAL and use icons/badges consistent with the effect; mode-dependent rewards show only the active mode.

## Implementation and validation map

| Source | Responsibility |
|---|---|
| PowerUp.cs and individual power classes | Actual catalog, effects, tags, cloning and compatibility |
| StarterPower.cs | Legacy tactical openings and habitat variants |
| NoVerticalDoublePower.cs | Both optional directional tradeoffs |
| ScoreManager.cs | Points pipeline and readable score results |
| PowerManager.cs | Separate active player/boss lists, dependency assignment and registration |
| LevelConfig.cs | Objective credit, tag queries, immutable offer schedules and assignment builder |
| LevelLibrary.cs | Authored campaign drafts, objectives, positive bonuses and target growth |
| GameController.cs / PowerUpSelector.cs | Live selection, registration, round resets and goal integration |
| Board.cs / BoardModel.cs | Real matching, clearing, transformations and refill behavior |
| Tests/PowerUpBehaviorTests.cs, RoundAndTimerTests.cs, ScoreManagerTests.cs, GameControllerTests.cs, BoardModelTests.cs | Effect, rule, lifecycle, scoring and integration coverage |

Validate offer paths after realistic earlier choices, roster edits and rule changes, not only an empty build. Compilation and pure seeded checks do not establish scene correctness, visual quality or gameplay balance. Playtest goals/modes and compare points per play, eligible credit per play and completion rates. Use [analytics](analytics.md) and [Unity Analytics](unity_analytics.md) for measurement details.

When changing a power: update its implementation and relevant tests, then this document. Update campaign schedules only where affected. Leave the retired documents as redirects so old links remain valid. Keep unimplemented decisions explicitly labeled instead of presenting proposals as runtime guarantees.

Chain Reaction (campaign ID 6) applies a level reward factor of 0 to initial player-created groups and 4 to cascade groups. Apply this after ordinary match scoring and lightning charge, before boss penalties and final adjustments. Initial groups still activate powers normally; this is not an early score veto. The factor is multiplicative with the build and never escalates with cascade depth.

Garden Plots and Alternating Current supply 4 base per tile and use conditional per-tile level factors after lightning charge, before other level percentages, bosses and final adjustments. Garden plots double the equal score share of tiles on planted positions; spotlight animals triple theirs. Round down the combined weighted score once. Plant gardens after the current scoring wave, and change the spotlight only after the entire successful play resolves. Garden Harvest, Square Roots and Both Sides are opening-only and are absent from ordinary catalogs. Later rewards use shared pools.

### Campaign tag-based offer defaults

Ordinary campaign openings use three filter groups: one permanent base-growth option, one multiplier option and one score-boost option. Opening-only powers and match-rule changes are excluded from these defaults. Points levels have a 4-base-points-per-tile baseline where they previously depended on a tactical starter, so a multiplier or score-factor pick can score immediately. Block levels instead offer one permanent block-credit growth option, one other block-credit option and one extra-clearing option. Compatibility still filters by goal, budget, match rules, roster and owned powers.

Intentional opening exceptions remain: level 2 animal growth, levels 4 and 5 match-size foundations, level 7 garden starters, level 8 alternating-animal starters, and level 24's authored growth opening. Shuffle levels 20-23 exclude shuffle powers at the opening and retain explicit teaching choices in rounds 2 and 3.

Existing native tag schedules remain active for later rounds. Levels 25-30 replace fixed reward lists with filters combining their animal, square, edge or cascade theme with scoring/credit, budget and shuffle choices. New qualifying powers automatically join these pools. `campaign-tag-offers.test.cjs` verifies the default policy; runtime tests exercise all 35 levels across 12 roster/pick seeds and all rounds.

Animal Mastery gains +1 animal-specific base point per tile or multiplier every 2 matches of that animal. Cascades count. Earned bonuses apply to future matches and carry across rounds.

Every level uses a persistent panel above the board with a short level-power rule and a separate live status line. Counters show progress toward the next trigger. Ongoing bonuses show the affected animal, spaces or ready condition. Levels with standard matching show goal progress. The panel reserves its own space below the timer and goal bar, grows to fit the text, and hides with the play HUD.

Four is Good grants +5 additive multiplier to matches of exactly 4 tiles. Its block version grants 5 extra clears.

Precision Points has been removed from the power catalog.

Stacked Odds adds a flat 40 base points to matches of exactly 4 tiles. Its block version adds 40 block credits per qualifying match.

When a play ends with no legal moves, request the same shuffle as the player button after resolution and round-outcome checks. Automatic shuffles use the same affordability check, time/move cost, free tickets, discounts, payoff triggers, statistics and analytics. Winning or finished rounds do not shuffle. If resolution finishes while paused, check for a shuffle when play resumes.

Shore Bounty adds 10 base points for each side of a matched tile touching the board boundary. Ordinary edge tiles contribute 10, corner tiles 20, and interior tiles 0. Sum contacts across the matched group. Cascades qualify. Its block version awards 10 block credits per contact.

Each animal has three conditional Bloom variants. Edge Bloom needs a match touching the board edge, Square Bloom needs a 2x2 square, and Giant Bloom needs 6+ tiles. Each converts up to 4 neighboring tiles into its animal after a player-created match. Clearing tiles and cells reserved by earlier transformations are excluded. Square Bloom requires connected matching. Offers require the target animal in the active roster.
