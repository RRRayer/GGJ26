# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Multiplayer hide-and-seek game built with **Unity 6** (6000.0.58f2) and **Photon Fusion 2** (Shared Mode). One "Seeker" hunts "Hiders" who wear colored masks (Red/Blue/Green). Hiders survive a 180-second timer while performing periodic dances; the Seeker eliminates them with a stun gun.

## Game Design Documents

Design docs are located in `../GameDesign/`. Designs change frequently — always check the latest docs before implementing.
- `Main_Design.md` — Core game design
- `Required_Resource.md` — Required resources
- `UI_Flow.md` — UI flow

## MCP Servers

- **Unity MCP** — Direct interaction with the Unity Editor (scene queries, GameObject/component manipulation, script editing, console checks, play mode control, etc.).
- **Context7 MCP** — Search up-to-date documentation and code examples for libraries/frameworks. Use when you need docs for Fusion 2, Unity API, etc.

## Build & Run

- Open in Unity 6 (version 6000.0.58f2)
- Play via Unity Editor Play button
- Build via **File > Build Profiles** using `Assets/Settings/Build Profiles/Windows.asset`
- No CLI build scripts or automated tests exist
- Input actions regenerate if `.inputactions` files change

## Verification Workflow

After any code change, follow this sequence:
1. **Compile check** — Run `read_console` via Unity MCP to verify no compilation errors
2. **Scene check** — If you modified scene-related logic, use `manage_scene(action="get_hierarchy")` to verify expected state
3. **Console monitor** — Check `read_console` for runtime errors/warnings after play-testing

## Unity Gotchas (Quick Reference)

- Never delete/move files without their `.meta` pair
- Never edit `Library/`, `Temp/`, `Logs/` directories
- Use Unity MCP tools to edit `.prefab`, `.unity`, `.asset` files — do not raw-edit
- After script changes, always `read_console` for compilation errors
- See `.claude/rules/unity-gotchas.md` for the full list

## Language

All plans, documents, and written content (including CLAUDE.md, commit messages, PR descriptions, code comments) must be written in English.

## Code Style

- Allman braces, 4-space indentation
- PascalCase for types/methods, camelCase for fields
- `[SerializeField] private` for inspector wiring
- Place new scripts in the appropriate feature folder under `Assets/01. Scripts/`

## Git Conventions

### Tags

| Tag | Description |
|-----|-------------|
| `feat` | New code / feature addition |
| `fix` | Bug fix |
| `refact` | Code refactoring |
| `comment` | Comment additions or typo fixes (no code changes) |
| `docs` | Documentation changes (README, CLAUDE.md, etc.) |
| `art` | Art asset additions |
| `merge` | Merge commits |
| `rename` | File/folder renames or moves |
| `chore` | Package additions, config changes, etc. |

### Branch Naming

```
(TAG)/(summary)/(ISSUE_NUMBER if applicable)

Examples:
  feat/player/#99
  chore/package
```

### Commit Messages

```
(TAG)(ISSUE_NUMBER if applicable) : Title (capitalize first letter if English)

Examples:
  feat(#123) : Implement player movement

  - Modified Player.cs
  - Added input handling

  ---

  chore : Add DOTween package
```

### PR Merge Convention

```
title: (TAG)/(ISSUE_NUMBER) (PR_NUMBER)

Example:
  FEAT/35 (#40)
```
