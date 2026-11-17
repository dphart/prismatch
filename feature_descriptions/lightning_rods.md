# Lightning Rods - level 35

Three fixed rod spaces on a 7x7 board: (1,1), (5,1), (3,5), using zero-based coordinates. Rods cannot be swapped, matched or swept away. Each distinct matched tile orthogonally adjacent to a rod adds one charge, capped at three. Diagonals and non-matching bonus clears do not count; cascades do. A tile adjacent to more than one rod may charge each.

After all three rods fill and the current play settles, a timer-free one-second lightning chain connects the rods, then charges every occupied non-rod square. This reuses the existing 2x lightning charge: it is not an additional stacking multiplier, and clears/refills consume the charge. No animals are cleared by the chain itself. Rod meters reset to zero and remain in the same locations. A new round or retry resets the board and rod progress.

The level uses six rounds, 36 moves, four-tile connected matching and in-place replacement. Initial target 1,000 with 30% growth is provisional. Existing shared power drafts are retained. The briefing shows the three-side charge example, chain and board reward as four numbered steps.
