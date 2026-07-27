# CLAUDE.md

### Project Overview

We are working on a game called "Hammer and Sickle" (HS_GameApp), which is a Unity-based hex strategy game in the same genre as Panzer General from SSI, featuring scenarios that span the globe, with tactical combat, unit management, and campaign progression. Target platform: Windows via Steam.

## SECTION 1: Notes and Organization

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

### Standard Workflow
1. Think through the problem, read relevant files, and write a plan to todo.md
2. Create a list of todo items to check off as you complete them
3. Check in with me to verify the plan before beginning work
4. Work through todo items, marking them complete as you go
5. Provide high-level explanations of changes at each step
6. Keep every change as simple as possible - minimize code impact
7. Add a review section to todo.md summarizing changes
---

## SECTION 2: Coding Guidelines

1. Use C# 8.0 syntax, prefer switch expressions
2. Use try-catch blocks for risky code
3. Handle exceptions: `AppService.HandleException(className, methodName, e)`
4. UI messages: `AppService.CaptureUiMessage("message")`
5. Use singleton pattern for global MonoBehaviours
6. Unit tests use Unity Test Framework (NUnit)
7. Keep class comments brief, but informative.
8. Use #region formatting for distinct sections of code, #endregion // Region Name`
9. Avoid loops in Editor tests
---

## SECTION 3: Game Information

**Hammer and Sickle** is a hex-based turn-based strategy game in the tradition of Panzer General/Panzer Corps. Single-player campaign set in the early 1980s commanding Soviet forces in the Afghan War.

**Core Gameplay:**
- Hex-based tactical combat with diverse military units
- Campaign system linking battles with persistent "core" units that gain experience
- Resource management, terrain tactics, supply lines
- Dynamic outcomes influence future missions and unlock alternate paths

**Map Technical Details:**
- Pointy-top, odd-r aligned hexagonal grid
- Map sizes: 8192x4096 (32x21 hexes) or 8192x8192 (32x42 hexes)
- Created from satellite imagery with color-coded terrain layers
---

## Section 4: Important Notes

This project uses Unity [6000.2.6f2] with Universal Render Pipeline [17.2.0].
Color space is Linear. Target platform is PC standalone.
Use URP HLSL include paths from Packages/com.unity.render-pipelines.universal/ShaderLibrary/.
Do not use Lightweight Render Pipeline paths or deprecated macros.
Shader files are handwritten .shader files, not Shader Graph.
Confirm you understand before making any shader changes.