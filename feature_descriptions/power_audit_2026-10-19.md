# Power philosophy audit — October 19, 2026

This is a dated audit of implemented behavior, not a second design specification. [Powers and scoring](powers.md) remains the source of truth for intended design and current mechanics. No gameplay or balance changes were made during this audit.

## Subsequent implementation note

After this snapshot, ordinary habitat base allowances were removed, More Power! was restricted to opening authoring, passive any-match growth cards were removed from the live catalog, and opening-only eligibility was enforced across picking and registration with explicit special-level opt-in. The measurements and findings below describe the earlier audited snapshot; they are not current catalog totals. Shared-pool reach and remaining growth-task difficulty still need follow-up. See [Powers and scoring](powers.md) for current rules.

## Verdict

The implementation does not yet match the intended philosophy. There are good conditional powers, but most campaign drafts hide them behind habitat filters. Many prominent rewards include unconditional base points, and several permanent-growth powers reward ordinary matching rather than a harder deliberate task.

The target is a broadly shared, compatible power catalog with substantial short-term and long-term variety. Most scoring effects should require a condition; permanent gains should require harder repeated behavior. Unconditional base/multi is reserved for explicitly allowed openings or particular levels. Themes should bias offers rather than replace most of the shared pool.

## Method and limits

Inspected power implementations, actual catalog construction, picker overrides, level schedules, matching compatibility, score timing and reset hooks. A reflection exporter enumerated PowerOfferBuilder.Catalog() from the current compiled game assembly and compared each power against every later-round scheduled slot in points campaigns.

Generated evidence: [inventory CSV](../outputs/power-audit/inventory.csv) and [inventory JSON](../outputs/power-audit/inventory.json). The exporter source is tmp/power-audit/Audit.cs; rerun after implementation changes rather than treating this snapshot as live balance data.

Reach means a power passes goal/mode, baseline match-rule compatibility, and a slot's tag query in at least one round after the opening. It is an **upper bound**, not actual offer probability: current rosters, owned powers, unique-card assignment, reservations, random selection and changed matching rules can reduce availability. Points powers are compared with points levels, not incompatible block levels. These measurements do not establish gameplay strength or optimality.

## Measured catalog and reach

- 158 distinct named catalog entries, including animal variants.
- 101 points-only entries, 6 both-goal utility entries, 16 block-clearing entries and 35 collection entries.
- 35 points campaign challenges; the guided tutorial is excluded.
- Only 25 of 101 points-only entries can reach a majority of points levels even before roster/build filtering. Those 25 are the 20 roster changes, 3 animal pacts and 2 directional tradeoffs—not the usual conditional base/multi rewards.
- All 15 habitat variants and 8 tactical opening powers give unconditional base points on acquisition. More Power! adds one more: 24 catalog entries supply positive flat base without first satisfying their advertised tactical condition. Openings can be approved exceptions; later drafts are not automatically exceptions.

| Family / examples | Distinct entries | Later points-level reach, out of 35 |
|---|---:|---:|
| High Stakes, Four is Good, Twin Shot, Four Fury, Jackpot Five, Color Focus, Stacked Odds, Consistency Is Key | 1 each | 1 each: Lightning Lanes |
| More Power!, Momentum Bonus, Snowball, Combo Craze | 1 each | 1 each: Lightning Lanes |
| Precision Points; Precision Strike | 1 each | 0 |
| Animal conditional base or additive multi | 7 + 7 | 2 each: Animal Friends and Lightning Lanes |
| Animal Double | 10 | 2 each: Animal Friends and Lightning Lanes |
| Animal earned base/multi growth | 12 | 1 each: Lightning Lanes |
| Habitat Bounties / Amplifiers / Engines | 15 | 4–11 each |
| Tactical opening powers | 8 | 1–11 each as later rewards; opening availability measured separately |
| Roster changes | 20 | 35 each |
| Animal pacts; directional tradeoffs | 3 + 2 | 31 each |
| Budget and shuffle utilities | 6 | 34 each |

Counts are card variants, not independent mechanics. Seven animal copies of one trigger do not provide seven distinct strategic decisions.

## Findings, ordered by impact

### 1. Campaign drafts override the shared catalog — high priority

CampaignIdentity.Schedule usually requires the habitat tag on both scoring slots. Its third slot admits tools or more habitat rewards. Even a strong shared conditional power cannot appear if it lacks that tag. Special schedules replace the ordinary picker result entirely, so expanding GetAll() alone would not solve this.

Animal Friends is an intentional animal-only exception. Lightning Lanes is the broad-pool exception. Most other points levels remain habitat-locked. Block collection drafts also filter by the actual objective; that is valid compatibility, but their credit/transformation/admission slot templates are repetitive.

Recommendation: make the shared eligible catalog the default on ordinary levels. Use a theme preference or one themed slot rather than two mandatory habitat scoring slots. Preserve explicit authored exceptions such as Animal Friends. One candidate layout is two shared choices plus one themed/tool choice; exact weights and frequency are proposals, not approved rules. Keep goal, mode, roster and geometry compatibility authoritative.

Evidence: CampaignIdentity.Schedule in LevelLibrary.cs; PowerUpSelector.ShowChoices; PowerOfferBuilder.IsEligible/Build in LevelConfig.cs.

### 2. Specialized cards carry unconditional permanent base — high priority

Every Bounty and Amplifier grants +2 base/tile even when its condition fails. Every Engine starts with +6 base/tile before earning any growth. More Power! grants +3 base/tile without a condition. These are regular reward cards, not just opening exceptions.

All eight tactical opening cards supply +4 base/tile. That is allowed when explicitly used as the opening foundation, but those same cards can appear in later drafts through the full catalog and starter-themed queries. Their names and Starter tags do not confine them to round one. Legacy Steady Start, Multiplier Start, Heavy Start, Precision Start and Momentum Start exist in code, but are not in the current canonical catalog; they should not be counted as active draft offenders.

Recommendation: remove the passive allowance from ordinary conditional/growth cards. Give Bounties base only when their condition is met; give Amplifiers multi only when it is met; give Engines earned growth without a free base package. Put any needed scoring foundation in an explicitly authorized opening or level setting, as Animal Friends already does. Do not remove foundations everywhere until score viability is resolved for the remaining levels.

Evidence: HabitatPower and TacticalStarter in StarterPower.cs; FlexibleMatchPower.cs; PowerUpLibrary.GetAll/Starters; ScoreManager.cs.

### 3. Several permanent-growth triggers require no special play — high priority

| Power | Implemented earning condition | Assessment |
|---|---|---|
| Momentum Bonus | Every resolved match adds 0.05 permanent base/tile | Passive growth; normal matching and cascades suffice |
| Snowball | Every 10 resolved groups adds +1 permanent multi | Passive match counter; no difficult condition |
| Combo Craze | Every 15 resolved groups adds +1 permanent multi | Same basic strategy as Snowball, different rate |
| Animal Growth variants | Every 10 tiles of the animal adds +2 permanent base/tile or +1 permanent multi | Real specialization, but often only a few matching groups; no streak/sequence requirement |
| Habitat Engines | Each qualifying group adds +0.5 permanent multi | Real conditions, but no repeated challenge before a payout; also supplies free +6 base/tile |
| Tactical growth cards | Squares, pluses, 6+, dual matches, opening large match, switching animals or edge progress | Some deliberate play; difficulty varies significantly |
| Four by Four | Four exact-four successful swaps in succession | Closest existing repeated-task example |

Cascades advance Momentum Bonus, Snowball, Combo Craze and animal tile-count growth because generic scoring hooks run for cascade groups too. Engine and tactical trigger scopes differ. A slow number alone does not turn automatic growth into a deliberate challenge.

Recommendation: redesign passive counters into repeated conditions that can be missed or interrupted. The discussed 4→5→6 challenge and two consecutive five-tile straight lines are appropriate candidates, but their exact progress/reset rules still need authoring. Keep some reliable conditional instant rewards competitive with these investments. Add permanent-base challenges as well as permanent-multi challenges.

Evidence: BasePerTile.cs, SnowballPower.cs, ComboCrazePower.cs, IconTypeStreakGivesBasePointsOrMulti.cs, StarterPower.cs.

### 4. Growth rarity cannot currently be enforced reliably — high priority

Seven high-stakes levels guarantee a premium Engine in every draft from round two. This makes growth a recurring guaranteed choice rather than a rare earned investment. Growth also appears on every standard tactical opening.

Growth metadata is incomplete: Momentum Bonus, Snowball and Combo Craze do not declare PowerTags.Growth. The opening-specific pool does not control all later appearances of tactical powers. There is no general acquisition-scope or rarity-weight system; Premium is a tag and guaranteed slot category, not a probability model.

Recommendation: declare reward lifetime and earning category explicitly for all powers, separate opening/level exceptions from normal eligibility, and make growth limits/weights operate on that metadata. Revisit guaranteed Engine schedules and their target curves together. Do not change rarity independently of levels currently balanced around repeated growth.

### 5. Some cards need correctness/clarity fixes before balance comparisons — medium priority

- **Smaller is Better:** rewards groups of three or fewer, but does not reject minimum-four rules. It can be offered in Lightning Lanes, where its trigger is unreachable with the baseline rules.
- **Precision Points:** says +5 base this match; code adds `5 * tile count`, so an exact-three match receives +15 base. Specify match versus tile consistently.
- **Earned-growth timing:** animal growth, habitat growth and tactical growth generally improve the same group that completes the condition. The proposed examples promise all future matches. This is not one consistent timing contract yet.
- **Cascades and resets:** generic counters/history can advance on cascades, and several generic stateful powers lack round reset hooks. The effect may be legitimate, but must be intentional and reflected in wording.
- **Flat-stat visibility:** Combo Craze returns earned permanent multi in the scoring hook but does not expose it via FlatMultiplierBonus, so the flat HUD underreports that earned state.
- **Animal conditions:** Animal Double checks whether a group contains the animal; older animal base/multi cards inspect only the first tile. Mixed pact groups therefore have inconsistent trigger semantics.

Recommendation: settle reward units, earning/consumption timing, player-created versus cascade scope, and round/retry reset behavior per family before evaluating strength from telemetry.

### 6. Some permanent factors are meaningful tradeoffs, not free stats — policy decision

Welcome Animal's x1.3 persists after adding a species. Directional powers give x4 after restricting matching. These impose ongoing structural costs, unlike a free base allowance. Farewell Animal reduces the multiplier while simplifying the roster; animal pacts halve points in exchange for matching equivalence.

The latest no-passive-stat rule explicitly permits openings and specific levels, but does not explicitly decide whether structural tradeoffs are another exception. Animal Friends' roster choices are an approved level-specific exception. Preserve them. Confirm broader ordinary-pool eligibility before treating every roster/directional factor as either compliant or forbidden. A meaningful downside should be visible and relevant to the current goal.

## What already fits

- High Stakes, Four is Good and Stacked Odds reward group size for the current match without passive base attached.
- Animal Double and conditional animal base/multi reward specialization.
- Twin Shot, Four Fury and Jackpot Five reward a current condition with a queued next-match effect.
- Color Focus and Consistency Is Key encourage repeat choices without permanent unconditional gain.
- Budget rewards require repeated edge, square or large-group conditions and pay in the active mode's resource.
- Clearing, transformation and collection admission change board/goal strategy; they should not be forced into a base/multi taxonomy.

These are good foundations, not a claim that their numbers, compatibility or reset details are already balanced.

## Suggested implementation order

1. Agree ordinary shared-pool versus themed-slot policy and define explicit opening/level exceptions.
2. Fix unreachable conditions, reward-unit discrepancies and growth metadata/HUD reporting.
3. Remove passive base packages from ordinary specialized rewards while preserving viable openings.
4. Replace automatic growth counters with difficult repeatable conditions; distinguish permanent-base and permanent-multi paths.
5. Replace most habitat-only drafts with broad pools and mild thematic bias; retain Animal Friends' explicit format.
6. Retune target growth and guaranteed-Engine levels using match-level scoring, weighted points/play and completion rates.

Acceptance checks: an ordinary non-exception card with no earned state and unmet condition should add no base/multi; earning progress should require its intended action; ordinary compatible conditional powers should reach most points levels; basic utility should be broadly shared; a selected build should remain viable without guaranteeing that permanent growth is always offered or optimal. No exact pool ratio, rarity rate or completion target has been approved yet.