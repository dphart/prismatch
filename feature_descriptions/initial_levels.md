# Showcase levels

These are independent demonstrations, not a difficulty ladder. All are available immediately, last 10 rounds and use 50% compounded target growth per round. Powers still accumulate. Budgets and targets aim for moderate difficulty but require playtesting. Free Play retains its configurable round count and growth defaults.

| Level | Main feature | Starting target | Budget per round | Gravity |
|---|---|---|---|---|
| Connected Crew | Connected 4+, 6x6 | 400 points | 60 seconds | In place |
| In a Row | Horizontal 3+, 7x6 | 350 points | 70 seconds | In place |
| Stand Tall | Vertical 3+, 6x7 | 36 blocks | 70 seconds | In place |
| Make It Count | Connected 4+, 6x6 | 45 blocks | 16 moves | In place |
| Falling Friends | Connected 4+, 7x7 | 450 points | 60 seconds | Down |
| Rising Tide | Connected 4+, 7x7 | 48 blocks | 60 seconds | Up |
| Westward | Connected 4+, 7x7 | 48 blocks | 16 moves | Left |
| Eastward | Connected 4+, 7x7 | 450 points | 18 moves | Right |
| Four Together | Horizontal 4+, 8x7 | 300 points | 80 seconds | Down |
| Boss Encounter | Connected 4+, 7x7; final-round half-points boss | 350 points | 80 seconds | Down |

Picker cards show matching, grid, goal, budget and refill direction with its entry side. Numbering identifies levels, not difficulty. The level briefing appears at level start and on boss rounds.

## Model

CampaignLevel supplies identity, name and description and delegates settings to FreePlayConfig. ILevelSettings shares goal, mode, move budget and gravity with gameplay. LevelConfig supplies rounds, targets, matching rules, board dimensions, powers and boss schedules. Definitions live in Assets/Scripts/Game logic/Level/LevelLibrary.cs. Internal FreePlayConfig Levels/levels names mean rounds for compatibility.

## Playtesting

Compare completion rates across rule types instead of expecting later levels to be harder. Tune starting targets and budgets independently. Check block powers and directional cascades, horizontal four-matches and the final boss. The requested 50% growth means round 10 targets are approximately 38.44 times the starting target; later rounds will need playtesting against accumulated powers.
