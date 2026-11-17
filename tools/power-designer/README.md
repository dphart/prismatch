# Prismatch shared power and level editor

Start from the repository root:

```powershell
node tools/power-designer/serve.cjs
```

Open http://127.0.0.1:8787/. The local server is required; opening index.html directly redirects there.

**Card layout & artwork** opens `card-workshop.html`. Select any power, switch Points/Blocks, filter to a level's configured pool, and adjust card width. The preview uses cream cards, tier borders, current wording, animal portraits, and condition → reward diagrams. This is a browser approximation of the Unity layout; typography and special-case mechanics can differ. Library cards also link directly to their layout preview.

The image gallery lists generated animal, starter, navigation, and level PNGs. Select an image to see its path and dimensions, then choose a replacement PNG. The editor previews it before **Save image to game** replaces the shared file. Dimensions must match to preserve Unity sprite layout. The previous exact PNG is backed up under `outputs/artwork-backups/`; stale edits are rejected. Shared animal replacements affect every power using that portrait. Procedural arrows, tile shapes, and borders are not separate PNG assets. No artwork is changed just by opening the preview. Unity reimports saved images; installed builds require rebuilding.

Common procedural power symbols now have one source: `Assets/Resources/Content/power-icons.json`. Unity's `PowerSymbolLibrary` and the browser's `power-symbols.js` render those same normalized primitives and palette. Edge icons (including Edge Explorer and Edge Balance), match shapes, plus, clock, move/interchange arrows, and half rewards reuse named definitions. Change a definition once; refresh the designer and restart Unity Play or rebuild to pick it up. Animal portraits remain shared PNGs. Check propagation with `node --test tools/power-designer/shared-symbols.test.cjs`.

The icon meanings and authoring rules are documented in `feature_descriptions/icon_design_language.md` and summarized under **Icon design language** in the card workshop. Shuffle references the same `nav-restart.png` as the game's Shuffle action. The old ascending growth bars are retired; use concise text and progress counters for growth.

The power library defaults to **Condensed** view: equivalent animal variants appear as one generic card with a copy count. Open it to see and edit individual copies. Search and filters apply to the copies, and a partial match shows how many copies match. Different tuning, availability, level scope, and multi-animal pacts stay separate. Switch to **All powers** to list every card; the view choice is remembered in this browser.

Shared goal powers show their Points and Blocks rewards together. Their IDs, point effects and level settings stay the same; 53 existing powers can now be selected for either goal without making a level copy. Base rewards become per-match block credit, and multi rewards become neighboring clears. Existing draft restrictions still apply. Budget powers already adapt between time and moves.

Each level shows **Power variety for this level**, with generic mechanics, copy counts and configured round availability. It flags identical pools across rounds and offer groups containing only one mechanic. The same overview is available in the round editor and updates with unsaved offer changes. This is a pool analysis before runtime roster, rules, mode and ownership checks; standard-catalog fallbacks are approximate, and availability is not an offer probability.

In the round editor, **Matching powers view** switches inherited and custom-offer result lists between **Condensed** families and **All powers**. Condensed families show copy counts and expand to individual animal copies with their existing edit actions. The browser remembers this preference; changing it does not change the offer or mark the level as edited.

`Assets/Resources/Content/prismatch.json` is the source for the live catalog, names, card wording, supported native tuning values, level settings, opening pools, later reward pools, and boss selections. The server reads it on every request. Unity loads the same Resources file before each Play session; builds include that file. Restart Play or rebuild to use saved changes. Existing builds and active runs keep their loaded version.

**Save to game** validates the document and writes that file. Each write backs up the previous exact file under `outputs/shared-content-backups/`. A stale browser revision cannot overwrite newer edits. External changes refresh the library every five seconds while no editor dialog is open. Close and reload before saving if another tool edited the file during an open dialog.

Native powers expose only implemented tuning fields. Their wording is generated from those values where a template is available. Special C# powers can have their name and wording edited, but changing their behavior still requires code. Keep freeform wording faithful to that behavior. Boss mechanics are selected from implemented handlers; writing an invented boss description does not create a mechanic.

Powers shown inside a level's round panel have two edit actions. **Edit everywhere** opens the shared definition and saves changes to every use of that definition. **Edit a copy for this level** opens an independent copy, named with the level number by default. Saving creates the copy and links it throughout that level in one backed-up write. Other levels keep their existing definitions. A new level must be saved before it can own a copy. **Back to level**, the close button and saving return to the same round while retaining unsaved level fields and offer edits; saving a power does not publish those other level edits.

Native copies reuse the original C# handler and expose the same supported tuning fields. They have separate IDs, parameters, names and wording, without duplicating the native handler key. Structured powers can also be copied. `powerOverrides` maps original power IDs to a level's active copies, including use through default drafts or filters. Copied powers are excluded from other levels and ordinary global catalogs. Existing copies retain independent values when their original is edited later. Recopying a power replaces that level's previous copy references. Starting-foundation match size and points/block credits per tile are editable; default values remain unchanged.

New structured powers use the condition/reward fields in this editor and run through `SharedRulePower` in Unity. The sandbox uses the same rule model; it does not simulate existing C# powers, gravity, cascades, or bosses. Base points, credits and score factors use whole numbers; multiplier bonuses can be fractional. Permanent bonuses benefit later matches and persist across rounds in a run. Starting powers cannot be added to ordinary reward pools.

Each level now has **Round-by-round powers**. Pick a round and turn on **Set this round’s offer**. A group can use specific saved powers or include/exclude filters from the finder, plus native game tags. Match all or any included qualities; exclusions always win. Filters are stored as rules, so newly saved powers that fit are included automatically. Use several groups to guarantee a mix (for example, one animal power and two growth powers), with at most three total cards. Copy an offer to the next round to reuse it. Choose whether to prefer unowned powers or allow only unowned powers. The game assigns distinct cards across groups; overlapping pools cannot duplicate a card in one offer.

The preview lists matching catalog powers and warns when the groups cannot supply enough distinct cards. Unity also checks the current roster, goal, mode, match rules and owned powers, so the actual pool can be smaller. Short groups show available cards and never fill from unrelated powers. Starting powers remain restricted to round 1.

Round overrides take priority over default opening/reward pools and existing special round drafts. **Use level default** removes an override. The previous opening/reward pools are under a collapsed default-pools section: empty later reward pools preserve special round drafts; an explicit reward pool overrides them for rounds without a custom offer. No existing level schedules are changed by adding this feature. Existing level IDs and numbers stay stable; new levels append. Published content is saved to the shared file. Earlier browser-only designs and imported workspace JSON remain drafts until individually saved with **Save to game**. Export drafts preserves those unpublished designs; back up the shared JSON for the complete game catalog.

The shared file stores `roundOffers` with round, title and counted groups. Filter groups store all/any and include/exclude selections, not a fixed list of matching IDs. Derived power `qualities` and `offerFilterKeys` are regenerated by the save API and consumed by Unity, keeping the finder and game on the same vocabulary. Hand-authored JSON should go through the editor save API to refresh this derived metadata.

The round filter picker uses the same friendly categories as the power finder. Raw game tags are internal draft selectors and no longer appear as a second, duplicate list. Existing drafts using raw tags retain their exact matching behavior; those selected tags appear under **Saved draft filters** so they can still be inspected or removed.

Use **Prismatch > Validate shared content** in Unity. Validation also runs before builds and at Play startup. A new native power missing from the shared catalog fails validation instead of silently disappearing from the viewer. Native identifiers stay separate from editable names.

Do not regenerate the catalog snapshot. `catalog.js` is now a dynamic server response from the shared JSON. The legacy C# exporter writes a review candidate under `outputs/shared-content-review/`; it never overwrites authored data. `refresh-catalog.ps1` checks the shared document.

Checks:

```powershell
node tools/power-designer/model.test.cjs
node tools/power-designer/shared.test.cjs
node tools/power-designer/round-offers.test.cjs
node tools/power-designer/power-links.test.cjs
```

Unity tests in `Assets/Scripts/Tests/SharedContentTests.cs` check catalog coverage, level reconstruction, native score/wording tuning and structured growth.

## Portable level scaling

In the game, open a campaign card?s **Edit level / scaling** button. Adjust starting target and compounded growth, then **Save scaling** to retain those values on this device. **Reset to level defaults** removes that local override. **Save & export changes** saves the current edit and exports all locally edited levels as `prismatch-level-scaling.json`. Web builds download it; desktop writes it to Unity?s persistent data folder and copies the path.

After reconnecting to the project, use **Import JSON** in this design studio and select that file. The importer previews the level count for confirmation and writes only starting targets and growth to the shared game file, with the normal backup and revision checks. Unknown levels, duplicate IDs and invalid values are rejected before saving. Other powers and level settings are retained. Changes become the project defaults on the next Unity Play or build; an already published build needs rebuilding.

Four-block foundations are the campaign default. Levels 4 and 5 retain the explicit 3/4/5 introduction. Point foundations now pay 2/4/8 base points per tile (50%/100%/200%); block-credit foundations retain 1/2/4 credits per tile. Later block levels keep normal 4+ matching and offer three themed powers instead of foundations. Each matched tile counts as one block before bonuses; their starting targets were halved when the former double-credit foundation was removed. Shuffle levels reserve their shuffle tools and payoffs for rounds 2 and 3 so opening picks cannot exhaust those offers.

Boss choices and comparison entries are filtered by goal, budget mode and required level mechanic. Settings changes clear an incompatible boss to No boss with an explanation. Shared-file validation and Unity loading also reject incompatible combinations. Boss descriptions use block-credit wording for block games. Check with node tools/power-designer/boss-compatibility.test.cjs.


Boss-round target multiplier (1-10, default 1) raises only the final round goal when a boss is selected. Preview targets include this factor, and scaling imports preserve it. Current point-based bonus-boss levels use 2.


Save failures display inside the editor dialog. Stale browser revisions retry by merging only changed power/level records onto the latest game content; concurrent edits to the same record are rejected. Each server save attempt is recoverable at outputs/designer-save-recovery/latest-save-attempt.json, including rejected requests. New levels append after concurrent additions. Check with node tools/power-designer/save-recovery.test.cjs.


The finder groups availability under **Where it appears**, rewards under **What it gives**, and conditions under **How it works**. Points and Blocks indicate supported goals. Reward filters identify positive effects, so score penalties stay under Tradeoffs. Animal conversions find Bloom powers, and Animal spawn rates find Magnets. Add or remove animals finds roster changes. Shape filters include squares, plus shapes and straight lines. Match counters cover powers that track progress. The same quality metadata drives editor previews and Unity round offers. Existing raw tag filters remain available on saved drafts. Check with `node tools/power-designer/filter-qualities.test.cjs`.
