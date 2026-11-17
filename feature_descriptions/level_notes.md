# Level playtest notes

Select a level in the game's level picker, then choose **Notes** beneath its portrait. Each level, including the tutorial, has a separate multiline note of up to 4,000 characters. Notes save automatically on this device as they are edited and remain across scene changes, restarts, and scaling resets. Clearing the field clears that level's note. Notes belong to the level across all difficulties; include a difficulty and round in the text when relevant.

**Done** returns to the same selected level. Calendar swipes and wheel paging are disabled while editing notes.

**Export all level notes** produces `prismatch-level-notes.json`, containing all nonblank notes, each level's number and name, and an export timestamp. Desktop and Android write to Unity's persistent data directory and copy the path to the clipboard. WebGL downloads the JSON using the existing download bridge. Share this file for feedback review. Export errors leave saved notes intact.

Notes are local playtest feedback and are not uploaded or automatically synchronized between devices. The level designer is unchanged.

Validation: compiled the complete game assembly with the level picker changes. Touch keyboard, layout, and restart persistence still need an in-game device check.
