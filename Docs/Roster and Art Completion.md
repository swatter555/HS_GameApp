# Roster and art completion — navigation

The current execution plan is in the vault: [HS Game — Roster and Art Completion Plan](<C:/Users/coder/Desktop/Codex Projects/HS Game/Roster and Art Completion Plan.md>). It reconciles the historical plan against game commit `38ff062` and separates implemented behavior, missing artwork, roster additions, compatibility decisions and verification still owed.

The [active TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>) owns work status. [Roster Expansion](<../Planning Docs/Roster Expansion.md>) and [Top-Down Icons](<../Planning Docs/Top-Down Icons.md>) preserve the earlier decisions and reasoning; their old work order and completion claims are superseded by the reconciled plan.

Robert's local [sprite checklist](<C:/Users/coder/Desktop/Codex Projects/helpers/hs-sprite-checklist/README.md>) lists required final PNG names by nation, weapon names, and existing/planned WeaponType constants. [Launch it](<C:/Users/coder/Desktop/Codex Projects/helpers/Open Sprite Checklist.cmd>). Its catalog and saved status are under `helpers/hs-sprite-checklist/data` in the vault; they are not runtime game data or included in this repository's commits. Read those files when reviewing current artwork readiness. A checked PNG means artwork created, not imported or verified in Unity. The executable integration guard is [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs); its EditMode inventory test writes `Temp/unit-icon-reachability.tsv` from the current profiles, formations, assets and saved atlas references. See the [repository rendering contract](<Repository Map.md#rendering-and-asset-contracts>) for pending exceptions.

## French roster contract — 2026-09-26

The [French integration contract](<French Roster Integration.md>) defines the new profiles, formation bays, census choices and compatibility rules. Puma supplies organic Embarked lift; Gazelle has a separate attack formation. The AMX-10P adds a carrier option, and national guns/AAA use the shared definitions. French helicopter frames remain provisional. Use the vault TODO for verification and the linked consumer packet for Editor/AI adoption.

## Iraqi roster contract — 2026-09-26

The [Iraqi integration contract](<Iraqi Roster Integration.md>) defines nine national profiles, six new formations, three existing formation reassignments and national census references. Armed Mi-8AT replaces the planned Mi-24 and has no transport role. Legacy shared regional identifiers remain supported; existing saved/OOB bay selections are not migrated by template changes.

## Approved NATO expansion — 2026-09-23

Robert approved dedicated NATO Leopard 1 and M113 art plus M109, M113 C&V reconnaissance, PRTL radar AAA and Centurion; the YPR-765/AIFV was already approved. Existing Leopard profile IDs (`TANK_LEOPARD1_NL`, `_BE`, `_DK`) and `APC_M113_NATO` stay stable. Proposed additions are `SPA_M109_NATO`, `RCN_M113CV_NATO`, `SPAAA_PRTL_NATO` and `TANK_CENTURION_NATO`; these are planning names, not registered game keys.

The shared art set does not imply universal national availability. Use the current plan and [NATO coordination packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-23 HS Game NATO roster and art expansion.md>) for filenames, assignments and pending Centurion variant decisions. Profile-owned icon selection remains the contract. Runtime profiles, imports and verification are still owed; do not export proposed keys until the game supports them.

## Current authoring restrictions and rendering contract

Robert withdrew Iranian and Saudi air-mobile forces on 2026-09-23. Do not add the previously proposed `HEL_UH1_IR`, `HEL_UH1_SA`, or `HEL_UH1C_SA`, their templates, or equivalent formations using shared foreign lift. Their `IR_UH1_Frame0..5`, `SA_UH1_Frame0..5`, and `SA_UH1C_Frame0..5` drawings are no longer requirements. The planned Iranian AH-1 Cobra remains approved. None of these withdrawn identifiers exists in the checked game or editor source; no save-version change is required. See the [coordination record](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-23 HS Game roster withdrawals and map airfield art.md>).

`ME_Airbase` is map art for a potential airfield, selected from `HexTile.IsAirbase` by [HexGridRenderer](<../Assets/Scripts/Renderers/HexGridRenderer.cs>). It must remain available even without an airbase unit. `BASE_AIRBASE` remains valid: [GameIconRenderer](<../Assets/Scripts/Renderers/GameIconRenderer.cs>) handles AIRB units before bay-based icon selection, using the existing generic `AirbaseStack_0..4` hex badges. Its profile fallback to `ME_Airbase` must not cause a unit-art checklist requirement. Broader asset organization is deferred.

This pointer deliberately does not duplicate the plan or progress ledger. Game source/assets/tests remain the authority for actual profile registration, bay selection, sprite resolution, and compatibility behavior.
