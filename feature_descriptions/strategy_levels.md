# Strategy campaign levels (25-29)

Appended after the existing authored level 24. All five use 7x7 connected boards, four-block matching, six rounds, the existing difficulty progression, and final-round bosses. Existing level definitions and selectable power definitions are unchanged. The shared-content file was backed up before appending.

| Level | Goal | Budget per round | Starting target | Growth per round |
|---|---|---|---|---|
| 25 Migration Season | Points | 24 moves | 450 | 30% |
| 26 Crack the Vault | Blocks | 26 moves | 40 | 25% |
| 27 Stepping Stones | Points | 80 seconds | 500 | 35% |
| 28 Animal Rescue | Blocks | 24 moves | 36 | 25% |
| 29 Crosswinds | Points | 85 seconds | 600 | 35% |

These are initial tuning values; device playtesting remains necessary.

## Mechanics

Migration Season highlights the current target animal for 2x points. It advances after a successful player move and its cascades finish. The final boss halves the points from all other animals. Mixed-animal matches apply each animal's actual factor. Removed roster animals cannot remain the target.

Crack the Vault has a fixed center vault. Each of its four neighboring side cells must participate in a player match twice; simultaneous groups cannot charge the same side twice on one move. Opening the vault boosts the tiles remaining after the clearing wave to double block credit, then resets side progress so it can be opened again. Boosts belong to slots, are consumed by clears, and do not transfer to new tiles. The vault itself cannot be swapped, matched, or swept as an extra tile. The boss erases every remaining boost when any player-created match touches an unboosted slot; cascades do not trigger this penalty. Mixed boosted/unboosted credit uses an exact ratio, avoiding percentage truncation. Extra clears retain actual boosted block credit.

Stepping Stones starts with four glowing spaces. Player matches extend the glowing path and earn 3x points if at least two already-glowing spaces are included. During the boss round, a match touching no glow removes one existing glowing space and does not lay a new path. Cascades do not invoke that penalty. Simultaneous matches inspect the same original glow state for rewards.

Animal Rescue doubles block credit for the marked animal. Rescuing the target advances it after the play; three different marked rescues earn one extra move. During the boss round, a player match with another animal cages an available marked animal. Cages block direct swaps and matches. Matching beside cages opens them, and rescuing the marked animal also frees one cage. Cages stay on their animals through an in-place shuffle.

Crosswinds counts successful player-created match groups, never failed swaps or cascades. At two matches the center row and column glow. At three, after the current ordinary chain settles, gravity turns and the 13 unique center-cross tiles clear with no points, budget rewards, queued-multiplier consumption, or secondary power triggers. The resulting cascade chain receives 5x points; the next ordinary play does not. Normal turns follow down-left-up-right. The boss chooses one of the three different directions at each turn, without revealing it beforehand. Current gravity remains visible.

## Integration and verification

Runtime strategy state belongs to the built-in LevelCharacteristicPower, is bound to BoardModel, and is recreated on each round/retry. Strategy events use the existing standardized activation display and icon fountain. Tile colors mark targets, boosts, glowing spaces, and cross warnings. Vault and cage overlays are noninteractive. Concise status, dedicated boss icons, and deterministic animated opening lessons explain each mechanic.

The workshop lists all five mechanics and descriptions, and filters their bosses by mechanic, goal and budget. The shared save schema accepts the new levels and preserves existing content.

Compilation and headless game regressions pass, including all five mechanics, boss effects, 7x7 cross geometry, cascade-only multipliers, exact block credit, fresh round state, matching walls, eight seeded six-round power-selection runs per level, compatible bosses and distinct opening lessons. Workshop model, shared-save, boss-compatibility, round-offer and new-level schema tests pass. Android animation/readability and difficulty balance still require on-device playtesting.
