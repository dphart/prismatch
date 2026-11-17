# Block collection levels

Power mechanics and credit calculation are specified only in [Powers and scoring](powers.md#clearing-and-collection-powers). This document owns campaign objective scheduling and level presentation.

Collection-based campaign block levels alternate animal collection with shape challenges. Clear Commitments (campaign ID 5, fourth challenge) instead counts any matched animal and uses opening powers that trade match minimum against credit per tile; see [Powers and scoring](powers.md#clear-commitments-fourth-playable-challenge). Starting targets are half their seed targets, with a minimum of 8, to account for restricted credit. Ordinary block targets grow 25% per round; levels with a positive level bonus grow 35%. These are provisional difficulty values.

Clear Commitments replaces the former Make It Count animal-collection level. Clear Priorities opens with Elephant collection. Other levels use other animals, including Moose and Panda. Every required animal is guaranteed in the refill roster, even if outside the original seven.

From round 3, odd non-final rounds use a shape objective. Other rounds, including the finale, return to the level's animal. Campaign shapes are squares, pluses and groups of six or more. Campaign levels use connected matching, with no horizontal-only or vertical-only line requirements. Custom definitions can still use line shapes or combined animal/shape restrictions.

The briefing shows the current objective. The target HUD names the animal or shape; the picker target badge shows the opening animal. Added eligible animals appear in briefings and the HUD.

## Authoring

CampaignLevel accepts an immutable BlockObjectives schedule. Each BlockObjective has an optional animal and BlockShape; omitted rounds default to unrestricted counting. Validation rejects incompatible matching rules, out-of-range rounds, points-goal use and six-tile line requirements on lines shorter than six. Cloning preserves objectives.

Collection drafts use BlockObjective.CatalogFor(objective, activePowers), the same filtered catalog used by the live picker. Each round offers a credit power, a transformation or clearing option, and another eligible animal, tool or clearing option. Required and admitted animals are restored whenever the roster is rebuilt. Exact power effects, eligibility and credit ordering live in the canonical power document.

Unrestricted Free Play block goals keep ordinary counting behavior. The Free Play editor does not currently expose custom collection-objective controls.

Pure objective/draft checks and compilation do not establish completion-rate balance or scene correctness; full gameplay and visual checks remain necessary.