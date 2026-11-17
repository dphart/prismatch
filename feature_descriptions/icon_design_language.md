# Power-card icon language

The shared symbol source is `Assets/Resources/Content/power-icons.json`. Unity and the designer render the same definitions. A symbol describes a specific condition, action, or reward; it must not be decorative filler.

## Card structure

- Top-left portrait: animal identity or the power's main mechanic.
- Centered description: the complete concise rule.
- Bottom graphic: condition → reward. The arrow means “triggers,” never “interchangeable.”
- Captions: counts, scope, units, and timing that the image alone cannot express. Always state NEXT MATCH/NEXT PLAY for deferred rewards. Distinguish tiles from successful matching moves.
- Passive modifiers may use one centered composite instead of a condition → reward diagram. Magnet powers combine the shared refill symbol with their animal portrait, captioned “2x [animal] chance” and “Refills + shuffles.” The layout must not imply a match trigger for a passive refill rule.
- Simple animal Mastery also uses one composite: animal portrait plus an overlapping `+1M` or `+1B` badge. Captions explicitly say “Every 2 player matches” and “[animal] only.” Cascades neither advance Mastery nor receive its bonus. M means added multiplier; B means one base point per matched tile. This is the upgrade size, not the current earned total. Rivalry retains its distinct rival tradeoff presentation.
- Mastery multiplier badges (`+1M`) use light purple; base-point badges (`+1B`) use blue. Both retain navy labels, so reward types remain identifiable without relying on color. These reward colors are separate from rarity borders.
- The `+1M` badge has a navy outline separating it from the animal portrait, including at header size. Its outline is part of the shared symbol definition.
- Border and tier label: Basic green, Rare purple, Legendary gold. Tier colors do not change icon meanings or card backgrounds.

## Vocabulary

| Symbol | Meaning | Use |
|---|---|---|
| Animal portrait | That animal | Shared animal PNG in title, trigger, or reward |
| Four matching green tiles in a down-right-down zigzag | A generic match | Shared `match-tiles-v2.png`; an example match, not a minimum-size rule |
| Four large animal portraits in the matching zigzag | A match of that animal | Shared `animal-match` composite uses existing animal PNGs with no green tile background, keeping faces recognizable at card size |
| Six animals in two rows of three | Every animal's matches | Global reward scope |
| Orange board boundary + green matched tiles | Match touching any board edge | All edge powers use `edge`; never a rotating edge unless specified |
| Tile group + count | Required match size | Draw the actual count. `+` means at least; ≤ means at most |
| Filled 2×2 | Square requirement | Not a generic four-tile requirement |
| Green plus | Generic additive effect | Prefer a specific unit symbol when known |
| B with plus | Added base points | Not final points or a score multiplier |
| Animal portrait with blue +5B badge (amount follows tuning) | Added base points per matched tile for that animal | Fixed animal bonus powers use one centered animal-and-badge composite, without an arrow or ALL icon. Description spells out “base points per tile.” |
| Blue numbered +B badge overlapping the bottom-right corner of reward/scope artwork | Any added base-point reward | Include it on global Growth, conditional bonuses, and other base-adding powers. Keep the animal/ALL scope visible. Use the authored amount (for example +2B); variable amounts may use +B. Captions distinguish per tile, per match, growth steps, and timing. Do not use +B for block credits or multiplicative base effects. |
| +M | Added multiplier | Distinct from multiplying the final score |
| ×2, ×3, ×4, ×5 | Score factor | Caption states when it applies |
| Tile with plus | Block credits | Caption distinguishes per tile and per match |
| Tile with outward marks | Extra physical clears | Not bonus block credits |
| Clock | Extra time | Caption gives seconds |
| Opposing blue arrows | Extra move / swap action | Caption gives moves |
| Opposing navy arrows between animals | Interchangeable animals | Pacts; never a plus sign |
| Coral 50% badge | Half value / half cost | Caption states which value is halved |
| Orange circular shuffle artwork | Shuffle action or free shuffle | Exact `UI/Generated/nav-restart.png` used by the in-game Shuffle button |
| New tiles entering board slots | Refill / spawn chance | Shared `refill` symbol; combine with the affected animal for Magnets |

Growth, permanence, and progress are stated in words and the active power's counters. The signal-like ascending bars are retired; do not add them back as an unexplained badge. A color or ornament must not imply an additional mechanic.

## Asset rules

Use named shared definitions for power-card symbols. Do not redraw an existing icon inside an individual power or in browser CSS. Raster references live in the same shared definitions; replacing the shuffle PNG updates its uses without creating a second illustration. Keep animal portraits consistent by reusing their shared PNGs.

When adding an effect: choose the existing symbol for its meaning, add a new shared symbol only if necessary, write a short caption with units and timing, and verify Points/Blocks and Timed/Limited Moves. Art changes must not change gameplay. Preview refreshes immediately; Unity needs a new Play session or build.
