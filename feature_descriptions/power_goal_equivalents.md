# Power goal equivalents

98 additional powers now support both goals. 169 of 181 total powers support both.

## Needs a design decision

- **Change of Taste** (Points only): Choose a block-credit penalty for most-matched animals; the existing positive-reward adapter would discard this downside.
- **Also Count Bear** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Sloth** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Chicken** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Duck** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Hippo** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Giraffe** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Walrus** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Moose** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Panda** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Also Count Elephant** (Blocks only): Points goals already count every animal. Needs a new reward to compensate for adding an animal to the board.
- **Corner Cache** (Points only): Structured designer currently enforces a single goal per reward type. Generalize its reward schema before converting this card.

## Conversion rules

- Positive scoring powers retain their matching triggers and use block credits or adjacent clears through the existing adapter. Animal Double gives one extra credit per matched tile. Tactical opening growth carries fractional clears forward.
- Points foundations give 1/2/4 block credit per eligible tile for minimum lengths 3/4/5. Block foundations give 2/4/8 base points per tile in points mode.
- Collectors and shape dividends give 4 base points per qualifying matched tile. Blooms retain their transformation mechanic in both modes.
- Sweep powers retain their physical clears and give 4 base points per actual extra tile in points mode. The reward belongs to its triggering match, so normal multipliers and level/boss rules apply. Extra clears do not retrigger powers.
- Shuffle Spark gives 8 block credits once on the next successful play. Clean Slate gives 32 base points once on that play in points mode.

Values are starting equivalents and need playtesting. Flagged cards retain their current goal restriction; the catalog records a goalReview reason.

## Welcome and Farewell

Both use 25%. Round down the size of the bonus or penalty, then add or subtract it. For block goals, apply this once per resolved match wave after all matched groups, extra clears, objective credits, and other power rewards. A 5-tile match plus 5 eligible extra clears gives 10 credits: Welcome gives 12 and Farewell gives 8. Multiple roster cards each use the same pre-roster total, so their bonuses and penalties combine independently of acquisition order. Rounding starts fresh for each cascade wave. Points rewards use the same 25% bonus/penalty rule after other power adjustments.

## Animal Pacts

Pacts retain mixed-species matching in both modes. In tile mode, a resolved wave containing either pact animal earns half its final eligible block credits, rounded down, including credit from other powers and extra clears. One credit becomes zero. Each distinct matching pact applies once per wave, even if multiple groups involve that pact; disjoint pacts can each halve the total. Waves with no matched pact animals keep their credit. Welcome/Farewell adjustments run afterward.
