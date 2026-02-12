Audit and update the `.claude/` documentation files against the actual codebase. Read all three docs, scan the codebase for discrepancies, report findings, and fix anything outdated.

## Steps

### 1. Read the current documentation
Read these files:
- `.claude/CLAUDE.md`
- `.claude/rules/architecture.md`
- `.claude/rules/unity-gotchas.md`

### 2. Gather ground truth from the codebase

Read these files for version/config data:
- `ProjectSettings/ProjectVersion.txt` — actual Unity version
- `Packages/manifest.json` — actual package versions (Fusion, Input System, Cinemachine, AI Navigation, URP, VFX Graph)
- `Assets/Settings/Build Profiles/` — verify build profile path exists

Scan the codebase for structural data:
- `Assets/00. Scenes/` — list actual scene files to verify scene flow
- `Assets/01. Scripts/` — list top-level feature folders to verify directory layout
- Check key classes mentioned in architecture.md still exist and match their described responsibilities:
  - `FusionLauncher`, `FusionSessionFlow`, `FusionSpawnService`, `FusionRoleAssignmentService`
  - `PlayerRole`, `PlayerStateManager`, `PlayerElimination`, `StunGun`
  - `DanceEventPublisher`, `GameStateController`
  - `BaseNPC`, `RedNPC`, `BlueNPC`, `GreenNPC`, `NPCController`
  - `AudioManager`, `SoundEmitter`
  - `SaveLoadSystem`, `FileManager`
- Verify enum values (e.g. GameState enum), timer values, NPC counts, cooldown values mentioned in docs by reading the relevant source files
- Check event channel SO types listed in architecture.md against actual classes in `Assets/01. Scripts/Events/`

### 3. Report findings

For each documentation file, report:
- **Accurate**: Items that match the codebase
- **Outdated/Wrong**: Items that don't match, with the actual value from the codebase
- **Missing**: Important systems or patterns in the codebase not documented

### 4. Fix discrepancies

For every outdated or wrong item found, edit the documentation file to match the current codebase. Do NOT change the codebase — only update docs.

After making changes, show a summary of all edits made.
