# Campaign boss penalties

Bosses never increase points or block credit. Reward-only pattern bosses now retain 100% for qualifying matches and 75% for other matches, rounded up. Quarter penalties apply after ordinary level and power rewards. Zero stays zero; physical clears are unchanged. The tutorial Coach has no penalty.

| Boss | Keep full reward by... |
|---|---|
| Lighthouse Lynx | Matching 2+ tiles on the rotating lit edge |
| Seesaw Seal | Matching 2+ top-edge tiles on odd attempts or bottom-edge tiles on even attempts |
| Wallflower Wolf | Matching at least 3 outside-edge tiles |
| Center Stage Stag | Matching 5+ tiles entirely inside the board |
| Box Troll | Including a 2x2 square |
| Stretch Giraffe | Spanning 4+ rows or columns |
| Oddball Otter | Matching an odd group of 5+ |
| Even Steven | Matching an even group of 6+ |
| Cascade Surfer | Building falling matches; player matches lose 25% |
| Opening Act | Making 6+ player groups on every third attempt; other attempts and cascades are safe |
| Corner Collector | Touching at least 2 board corners |
| Twin Keeper | Making both swapped tiles match; cascades are safe |
| Trophy Hunter | Matching 6+ tiles |
| Rapids Toll | Creating a falling group of exactly 4 |
| High Roller | Matching 8+ on every third attempt |
| Overgrowth | Touching a planted space |
| Blackout | Matching only Duck/Bear |
| Fence Fox | Touching at least 2 different outside edges |
| Tide Turtle | Creating falling matches touching an outside edge |

Existing move taxes, animal restrictions, Treasure Crab, and strategy bosses retain their mechanics. Boss cycles start with attempt one on registration and reset on retry. Score/block evaluation does not advance history. Clones retain boss identity. Ordinary level bonuses remain active through the finale.

The nine reward-era 2x finale target multipliers were removed from the shared campaign. Targets use normal round growth and selected difficulty. The general authoring option for finale target multipliers remains available.

Selection/briefing and designer descriptions use the shared boss catalog. Pattern, cascade, duplicate-cell, rounding, overflow, mode-wording, target, and clone checks cover the new behavior. Device balance testing remains outstanding.

## Historical design notes (superseded)

The descriptions below record earlier iterations; the rules above are authoritative.

# Campaign bosses: first iteration

All 51 campaign levels have one final-round boss. Bosses do not persist into other rounds. Level 1 is a friendly Coach Cub practice duel: the visual tutorial now ends with a required match-of-four interaction before completion. It has no timer or penalty. Levels 2–11 retain their existing bosses; levels 12–51 cycle through the new styles below.

New challenge penalties retain 75% of points or block credit, rounded up. Bonus bosses award 125%, rounded down with overflow protection. Goal descriptions explicitly say points or block credit. They do not alter the physical number of removed tiles or the time/move budget.

| Boss | Counterplay / twist |
|---|---|
| Coach Cub | Complete the guided match of four; no penalties |
| Lighthouse Lynx | Touch top, right, bottom, left on successive swap attempts |
| Seesaw Seal | Top row on odd attempts; bottom row on even attempts |
| Wallflower Wolf | Touch any board edge |
| Center Stage Stag | Keep the whole match off the outer edges |
| Box Troll | Include a complete 2x2 square |
| Stretch Giraffe | Span at least four rows or columns |
| Oddball Otter | Make odd-sized matches |
| Even Steven | Make even-sized matches |
| Cascade Surfer | Cascades gain 25%; player-created groups keep full value |
| Opening Act | Every third swap attempt gains 25%; other attempts keep full value |
| Corner Collector | Include a board corner |
| Twin Keeper | Both swapped tiles must match; cascades are exempt |

Unless a bonus or no-penalty rule is specified, failing the counterplay triggers the 25% penalty. Move cycles start at attempt one on boss registration and therefore restart on retry. Failed swaps advance the cycle; cascades keep the originating attempt. Repeated score/block evaluation does not advance a boss's state.

Box Troll is never automatically assigned to horizontal/vertical-only matching; it is replaced with Stretch Giraffe. Explicit incompatible Box Troll configurations fail validation. Current campaign grids are large enough for the spatial challenges.

## Authoring and iteration

Use `finalBossStyle: BossStyle.Lighthouse` (or another enum value) on CampaignLevel to pin a boss independently of its level number. The setting survives cloning. Default assignments come from BossLibrary.ForLevel; boss behavior and text live in ChallengeBoss in BossPowers.cs. Custom non-campaign content may still explicitly opt out with bossFinal: false.

Suggested playtest priorities:
- Lighthouse / Seesaw: is the move-count rule easy to follow, including failed swaps?
- Oddball / Even Steven: can players intentionally create the preferred size?
- Box Troll / Center Stage: are there enough useful matches after roster removals?
- Cascade Surfer: does the bonus feel good on directional gravity boards?
- Blocks: rounding makes small penalties forgiving; compare actual completion rates.
- Tutorial: confirm the final practice duel is clear and finishing it unlocks level 2.

Potential next iterations: animated edge markers for Lighthouse, a visible three-beat meter for Opening Act, and a boss-specific portrait for each style. These visuals are not implemented in this pass.

Validation covers boss presence and final-round-only scheduling across all 51 levels, clone preservation, compatibility, each new rule's reward calculations, rounding and saturation. Full phone playtesting and difficulty balancing remain outstanding.

## Animal Friends finale

Popularity Tax blocks the current round's most-matched animal, with a live target displayed in the boss button and active rules. See [Powers and scoring](powers.md#animal-friends-first-playable-challenge) for counting, tie and reset semantics. This is the finale of the first challenge after the guided tutorial (campaign ID 2).

Garden Plots ends with Overgrowth: groups touching no planted space retain 50% points, rounded down. Alternating Current ends with Blackout: groups containing any animal outside Duck/Bear score zero. These are final reward penalties, so normal matching-power hooks still run. Neither boss grants rewards.
