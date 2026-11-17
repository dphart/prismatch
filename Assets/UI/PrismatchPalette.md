# Prismatch UI Palette

This palette is derived from the existing animal icons and is intended for the game's menus, power cards, feedback, and navigation.

| Role | Color | Hex |
| --- | --- | --- |
| Deep background | Midnight navy | `#0B1F3A` |
| Panel | Game navy | `#132A4F` |
| Raised panel | Soft navy | `#1D3B68` |
| Border/highlight | Sky blue | `#58A6F6` |
| Primary action | Warm orange | `#FF9F1C` |
| Momentum power | Coral | `#FF6B6B` |
| Precision power | Leaf green | `#42CC72` |
| Reward/accent | Giraffe yellow | `#F6C915` |
| Primary text | Warm cream | `#FFF7E8` |
| Secondary text | Mist blue | `#AFC7E8` |
| Card surface | Soft cream | `#F7F3EA` |
| Disabled/quiet | Slate | `#7185A3` |

## Usage

- Use midnight navy for the page background and game navy for large panels.
- Use soft navy for raised counters and secondary containers.
- Reserve orange for primary actions, progress, and celebratory emphasis.
- Use coral, sky blue, and leaf green to distinguish power families.
- Use warm cream instead of pure white for text and symbols.
- Animal primary colors are reserved identities: preserve them in board tiles, power icons and picker decorations. Keep species silhouettes and facial features readable as a second cue.

## Animal identities

Generated with built-in ImageGen using existing animal sprites as edit targets. Transparent originals are preserved in Assets/Sprites; active Item assets reference the new set in Assets/Resources/UI/Generated/Animals. Exact prompts are in feature_descriptions/animal_icon_prompts.json.

| Animal | Primary color |
|---|---|
| Bear | vivid orange #F28C28 |
| Chicken | vivid crimson red #E74747 |
| Duck | bright emerald green #2CCB6F |
| Elephant | deep teal #168D83 |
| Giraffe | bright golden yellow #F3D522 |
| Hippo | royal blue #397CDD |
| Moose | vivid magenta pink #DA4DA1 |
| Panda | ivory white #FFF4DC with charcoal ears and eye patches |
| Sloth | rich violet purple #9254D8 |
| Walrus | bright cyan turquoise #35C9E8 |

Use different hue and lightness as well as recognizable silhouettes. Ten colors alone are not a guarantee of color-vision accessibility. Check at actual phone tile size.
