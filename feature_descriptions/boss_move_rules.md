# Boss intros and move rules

Boss intros show a crowned animal portrait, boss name, one brief rule and one Choose your power action. The generic warning and repeated boss name are removed. Detailed rules remain in Boss Rules. The level selector and rule cards use the same portraits; badges distinguish every implemented boss style.

| Final-round boss | Levels | Rule |
| --- | --- | --- |
| Crowd King | 17 | Matching any of the board's most common animals costs 2 moves total. |
| Tiny Tyrant | 13, 22 | A successful swap costs 2 unless one initial group contains 6+ tiles. |
| Border Hippo | 15 | A successful swap costs 2 unless one initial group touches an outside edge. |
| Echo Panda | 19 | Matching an animal from the previous successful swap costs 2 moves total. |

All these rules require LimitedMoves. They replace the previous final boss; they do not add an extra scoring penalty. Level 10 retains Treasure Crab. Goals, growth and move budgets are retained, including the imported Android tuning.

Crowd King freezes board population before the swap; all tied leaders qualify. Echo Panda compares initial groups across successful swaps and ignores intervening failed swaps. Its first successful swap is untaxed. The board HUD shows the current taxed animal(s), or the shape/edge condition. Tidal Tactics also retains its gravity/turn indicator.

The normal one-move swap cost is paid first. A boss observes only player-created groups and adds at most one extra move after the entire play resolves, even if multiple groups qualify. Cascades and bonus clears are free. Failed swaps cost only their usual one move. Large-group and edge exemptions consider every player-created group, so one qualifying initial group exempts the swap. A cascade cannot grant the exemption. Move counts never go negative, and reaching the target on the last move still wins after resolution. New rounds, retries and clones reset boss history. Shuffles retain their existing cost and never trigger these match taxes.

The shared JSON includes all four new boss handlers and limits their use to move-based levels. The design editor and Unity both reject a move-tax boss on a timed level. Existing icon art is composed into portraits at runtime, so there are no missing external image dependencies.
