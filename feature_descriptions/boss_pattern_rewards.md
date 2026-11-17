# Boss pattern rewards

Generic percentage penalties now reward deliberate patterns. Missing a pattern keeps normal value. The same multipliers apply to points or block credit, with goal-appropriate wording in the designer and game. Saved goals, growth and budgets are unchanged.

| Boss | Rule |
| --- | --- |
| Coach Cub | Groups of exactly 4 earn 2x points. |
| Lighthouse Lynx | Matches with 2+ tiles on the lit edge earn 4x points. The edge rotates top, right, bottom, left with each swap. |
| Seesaw Seal | Matches with 2+ tiles on the top edge earn 4x points on odd swaps; use the bottom edge on even swaps. |
| Wallflower Wolf | Matches with at least 3 tiles touching an outside edge earn 5x points. |
| Center Stage Stag | Groups of 5+ entirely inside the board earn 4x points. |
| Box Troll | Matches containing a 2x2 square earn 4x points. |
| Stretch Giraffe | Matches spanning 4+ rows or columns earn 4x points. |
| Oddball Otter | Odd-sized groups of 5+ earn 3x points. |
| Even Steven | Even-sized groups of 6+ earn 3x points. |
| Cascade Surfer | Matches made by falling animals earn 3x points. |
| Opening Act | On every third swap, player-created groups of 6+ earn 5x points. |
| Corner Collector | Matches touching at least 2 board corners earn 5x points. |
| Twin Keeper | When both swapped tiles make matches, player-created groups earn 4x points. |
| Trophy Hunter | Groups of 6+ earn 5x points. |
| Rapids Toll | Falling matches of exactly 4 earn 4x points. |
| High Roller | On every third swap, groups of 8+ earn 7x points. |
| Overgrowth | Matches touching a planted space earn 4x points. |
| Blackout | Duck and Bear matches earn 3x points. |
| Fence Fox | Matches touching at least 2 different outside edges earn 4x points. |
| Tide Turtle | Falling matches touching an outside edge earn 4x points. |

Edge counts use distinct tiles on any outside edge; a corner counts as one tile for Wallflower Wolf. Fence Fox instead counts different sides, so a corner touches two sides. Corner Collector needs two distinct corners. Each group is evaluated separately; separate groups cannot combine their edge counts or shapes. Bonuses multiply once per qualifying group; ordinary matches are not discounted. Rotating edge/row and third-swap rules use swap attempts, including failed swaps; cascades do not advance the counter. Twin Keeper rewards only initial player matches, not later cascades. Scaling saturates at the maximum integer score.

The four move-tax bosses retain their budget rules. Duck Customs and Popularity Tax retain their animal restrictions, and Treasure Crab retains its treasure-dependent whole-play accounting. These have specific planning consequences instead of being generic percentage reductions.

## Bigger boss-round goals

Point-based bonus-boss levels 3, 4, 6, 7, 8, 9, 11, 18 and 23 now use bossTargetMultiplier = 2. Only the final round target is doubled. Earlier rounds, starting targets, growth, budgets, tutorial, block goals and move-tax levels retain their existing values. The designer exposes a whole-number multiplier from 1 to 10 and includes it in target previews. Unity clones and local scaling overrides retain it; No boss ignores it. Goal multiplication is applied after rounding each round target and saturates at the maximum integer score. This is an initial balance pass, not a measured difficulty calibration.

