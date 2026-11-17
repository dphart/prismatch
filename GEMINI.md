# Prismatch Project Rules & Context Limits

## File & Directory Exclusions
To optimize token usage and context efficiency in this Unity project, ignore and do not index, search, or read the following directories and file patterns unless explicitly asked:

- **Temporary & Cache Directories**:
  - `Library/`
  - `Temp/`
  - `obj/`
  - `Logs/`
  - `.utmp/`
  - `Build/`, `Builds/`, `WebBuild/`

- **Unity Metadata & Serialized Assets**:
  - `*.meta` (Unity asset metadata)
  - `*.asset` (ScriptableObject/asset YAML files)
  - `*.prefab` (Prefab YAML files)
  - `*.unity` (Scene YAML files)
  - Binary/media assets: `*.png`, `*.jpg`, `*.psd`, `*.wav`, `*.mp3`, `*.fbx`, `*.ttf`, `*.otf`

## Primary Focus
Focus codebase queries, semantic searches, and refactoring on:
- **C# Scripts**: `Assets/Scripts/**/*.cs` (Game logic, UI, Core, Level, Powers, Tests)
- **Documentation**: `README.md`, `feature_descriptions/**/*.md`
- **Package Manifest**: `Packages/manifest.json`

## Code Standards
- Target environment: **Unity 6 (6000.6)** with modern C# (.NET Standard 2.1).
- Maintain test coverage across `Assets/Scripts/Tests`.

## Power wording
- Write power descriptions as simply and briefly as possible. State the trigger, reward and drawback in plain words. Follow the card wording rules in `feature_descriptions/powers.md`.
- Power text assumes player-created matches. Omit cascade exclusions and explicitly say when cascades count toward a bonus or trigger.

## Level power display
- Every level shows a short explanation of its level power and live progress toward its trigger in the shared panel above the board. Show the current target or ready condition for ongoing effects.
