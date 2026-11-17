# Calendar level picker

The campaign selector presents 20 levels in a five-column, four-row grid. Page arrows, horizontal swipes and mouse-wheel paging move through ranges of 20, including future locked ranges. Only unlocked levels can be selected. Completed, ready and locked cells have text labels as well as color; the selected cell uses the orange accent.

A compact panel below the grid shows the selected level's portrait, name, completion state, round count and timed/move mode, with Play and Edit actions. Level 1 starts the existing guided tutorial; its Edit action is disabled. Target, growth, matching and gravity details live in the opening screen or scaling editor instead of the selector.

Jump to level accepts any unlocked level number. The saved selection survives leaving the scene and app restarts. Initial paging follows that selection, so winning never automatically advances to the latest unlocked level. Browsing another page does not change the saved selection. The layout scales to the available safe area. Scaling edits, exports, unlocks and completion records are retained.

Pagination regression tests cover boundaries, the current 24 levels, 500+ levels, invalid indices and complete nonduplicated page coverage. Campaign regression checks permit additional designer-authored levels instead of hard-coding the original 23-level limit. Phone layout/interaction validation still requires the next Android build.
