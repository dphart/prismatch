# Looping visual level lessons

Every level briefing now begins with a visual lesson. This covers all eleven authored campaign levels, including the tutorial, and Free Play. The first lightning demo's button and one-shot sequence have been removed.

The shared presentation uses the game's animal sprites, fixed pale grid boxes, green match highlights, blue charged tiles, gold treasure/border marks, reward badges, and short captions. Each loop shows an action followed by its result. Briefing controls remain outside the animation. Lessons never modify the real board, score, timer, history, powers, or progression.

| Level | Loop |
|---|---|
| Tutorial / Free Play | Swap an animal into a match, highlight it, then clear it. Free Play respects its actual minimum, row/column rule and goal. |
| Animal Friends | Bear starts with fewer matched tiles, scores double, then ties Duck's count so both share the bonus. |
| Lightning Lanes | A Duck swaps into a straight run of five. A blue/white zigzag strikes the entire row, all seven animals clear, and the other rows turn blue with x2 badges. |
| Size Matters | Compare three-, four- and five-tile matches with 1, 4 and 10 base points per tile, including their resulting base totals. |
| Clear Commitments | Compare three-, four- and five-tile matches with 1, 2 and 4 block credits per tile, including their resulting goal credits. |
| Chain Reaction | A four-Duck setup clears for zero points. Bears fall into a real connected match of five and score 4x. |
| Garden Plots | Complete a 2x2 square, clear it, plant its four spaces, then match new animals on those same spaces for double points. |
| Alternating Current | Complete a Duck match during Duck's 3x play, wait until the play finishes, switch to Bear, then repeat. |
| Border Patrol | Match in one corner to light two edges. Match in the opposite corner to complete all four for 4x, then reset the border. |
| Treasure Trail | Collect a marked tile in three successive plays, then use the 5x reward on the following play. |
| Changing Tides | Complete four moves before changing gravity from down to left. The first play after the turn gets 3x; replacement animals enter from the right. |

`LevelLessonStoryboard` supplies deterministic presentation frames. `LevelLessonAnimation` plays them with an explicitly started, infinite DOTween loop using unscaled time. Matching animal icons move above a separate layer of stationary boxes. The same stage scales uniformly into each briefing card, and boss cards have enough height for the lesson. The loop fades at its boundary, resets all effects, and repeats without user input. Hiding, replacing, disabling or destroying the briefing stops the loop immediately.

Eleven pure storyboard tests verify level routing, lightning timing and line clearing, real cascade geometry, delayed garden rewards, animal counts and ties, alternating bonuses, foundation values, border completion, queued treasure/tide rewards, and deterministic reset. The game, player, edit-mode test and play-mode test assemblies compile. The focused standalone runner passes 34 checks including existing campaign coverage. A play-mode lifecycle test covers automatic playback with a zero time scale, text fit, non-interactive graphics, stationary boxes, charge badges, looping reset, and cleanup across the campaign and Free Play. Its execution is pending because Unity has this project open in another process.

The contact sheets in `outputs/level-lessons/` are visual previews rendered from the actual C# storyboard frames and game sprites. They are not Unity gameplay screenshots; an in-editor/device layout review is still needed.

## Concise opening layout

The opening shows the level name, one target/budget line, the animated lesson and one Choose your power button. Repeated plan/bonus paragraphs, generic get-ready instructions, duplicate match diagrams and future-boss rule cards are omitted. Restricted collection objectives and any currently active boss rule remain visible when needed. The lesson carries the explanation.

Cascade, large-group and square remixes show their own +25%, +50% and +25% bonuses. Only levels 4 and 5 show match-size choices; point foundations use the current 2/4/8 values. The original animated lessons remain for lightning, chains, gardens, animal bonuses, borders, treasure and tides.


## Readable numbered preview

The opening card uses 94% of the available canvas height. The lesson has a 720 x 880 logical layout with a separate heading, numbered step, large action caption, 430-unit board area, result and reminder. Board sizing is independent of text: seven-row examples use the full board area rather than the old 196-unit limit. Captions use 34-unit text, results 29, reminders 26. Extra content scrolls inside the briefing while Choose your power stays outside the scroll area.

Each storyboard defines numbered action steps. Playback gives each step at least three seconds while preserving its original frame order. Pure tests check ordered step boundaries, reading-time mapping and deterministic looping for all lesson kinds. A render of actual storyboard frames checks text fit across 72 examples; outputs/level-lessons/readable-preview.png shows representative layouts. These renders are layout checks, not device screenshots.

Device screenshot correction: the lesson now uses a 720 x 1140 stage, with up to 680 units for the example board rather than 430. The card spans 98% of the canvas, and the lesson viewport 96% of the card. Width scaling can grow beyond 1x; the old 2.6x board cap is removed. Full square examples occupy roughly 88% of canvas width, close to the live board. Short screens scroll rather than shrinking the board. Rod counters no longer wrap.

The pre-level lessons now match the visual tutorial: navy rounded tile backgrounds at 78/86 of cell spacing, animal artwork at 65/78 of tile size, bright green matches and sky-blue charge highlights. Corner radii scale with tile size rather than turning small example tiles into circles. Headings use sentence case, instructions use cream text, and the inset panel is removed. The enlarged board and numbered steps are retained.
