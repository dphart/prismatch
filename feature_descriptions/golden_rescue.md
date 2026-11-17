# Golden Rescue (level 30)

Six rounds, 7×7 board, four-tile matching, in-place refills, limited moves and block-credit goals. Initial tuning: 48 blocks, 25% target growth, 24 moves. Opening and reward pools start from level 28.

Each match cages one eligible animal per two matched tiles, rounded down separately per match. A six-tile match creates three cages. Cascades use the same rule. The final-round Cage Warden changes the rate to one cage per matched tile.

Matching orthogonally beside an existing cage frees its animal. The individual freed tile earns triple block credit and shows a gold border. Its bonus follows swaps and gravity, ends when that tile clears, and does not transfer to new animals. Gold tiles cannot be recaged. Existing cages open before new cages are placed, so a match cannot immediately open a cage it just created. Cages choose random occupied, uncaged, non-gold tiles outside the current matches; excess cages are skipped when no eligible tiles remain.

The shared editor supports the mechanic and enforces its boss compatibility. A dedicated intro explains caging, adjacent rescues, gold value and swapping.
