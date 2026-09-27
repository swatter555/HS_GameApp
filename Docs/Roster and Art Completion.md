# Roster and art completion — navigation

The current execution plan is in the vault: [HS Game — Roster and Art Completion Plan](<C:/Users/coder/Desktop/Codex Projects/HS Game/Roster and Art Completion Plan.md>). It reconciles the historical plan against game commit `38ff062` and separates implemented behavior, missing artwork, roster additions, compatibility decisions and verification still owed.

The [active TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>) owns work status. [Roster Expansion](<../Planning Docs/Roster Expansion.md>) and [Top-Down Icons](<../Planning Docs/Top-Down Icons.md>) preserve the earlier decisions and reasoning; their old work order and completion claims are superseded by the reconciled plan.

Robert's local [sprite checklist](<C:/Users/coder/Desktop/Codex Projects/helpers/hs-sprite-checklist/README.md>) lists required final PNG names by nation, weapon names, and existing/planned WeaponType constants. [Launch it](<C:/Users/coder/Desktop/Codex Projects/helpers/Open Sprite Checklist.cmd>). Its catalog and saved status are under `helpers/hs-sprite-checklist/data` in the vault; they are not runtime game data or included in this repository's commits. Read those files when reviewing current artwork readiness. A checked PNG means artwork created, not imported or verified in Unity. The executable integration guard is [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs); its EditMode inventory test writes `Temp/unit-icon-reachability.tsv` from the current profiles, formations, assets and saved atlas references. See the [repository rendering contract](<Repository Map.md#rendering-and-asset-contracts>) for pending exceptions.

## French roster contract — 2026-09-26

The [French integration contract](<French Roster Integration.md>) defines the new profiles, formation bays, census choices and compatibility rules. Puma supplies organic Embarked lift; Gazelle has a separate attack formation. The AMX-10P adds a carrier option, and national guns/AAA use the shared definitions. French helicopter frames remain provisional. Use the vault TODO for verification and the linked consumer packet for Editor/AI adoption.

## Iraqi roster contract — 2026-09-26

The [Iraqi integration contract](<Iraqi Roster Integration.md>) defines nine national profiles, six new formations, three existing formation reassignments and national census references. Armed Mi-8AT replaces the planned Mi-24 and has no transport role. Legacy shared regional identifiers remain supported; existing saved/OOB bay selections are not migrated by template changes.

## Iranian roster contract — 2026-09-26

The [Iranian integration contract](<Iranian Roster Integration.md>) defines nine national profiles, six new formations and four existing formation reassignments. Iran has national Chieftain, Cobra, F-5E, M109, M113 recon and support art; Hawk deliberately shares the existing US profile and census. Cobra is a combat helicopter, with no Iranian air-mobile addition. Existing persisted identifiers and saved/OOB selections remain supported.

## British roster contract — 2026-09-26

The [British integration contract](<British Roster Integration.md>) records ten profiles and nine new formations, including playable FV432 at its original enum value. Puma supplies organic lift; Lynx is TOW-armed combat aviation. National Chieftain, Phantom, Jaguar, guns and AAA are connected, while Challenger, Warrior, Rapier and Tornado remain available. The UK shares the NATO truck.

## German roster contract — 2026-09-26

The [German integration contract](<German Roster Integration.md>) records six profiles and seven new formations: tracked Roland, Alpha Jet, MARS I, national guns/AAA and a second Panzergrenadier option using the existing M113. Existing German templates, organic UH-1D lift, shared Hawk and the NATO truck remain intact. MARS uses the shared M270 combat definition with national availability and census.

## Generic NATO roster contract — 2026-09-26

The [generic NATO integration contract](<Generic NATO Roster Integration.md>) records seven profiles, eleven new templates and five corrected Dutch/Belgian assignments. YPR/AIFV joins retained M113 options; M109 supplies mobile artillery; Dutch M113 C&V, PRTL and F-16 receive their approved art. Robert selected the older 105mm Centurion Mk 5/2 for Denmark, with 60 tanks and cost55. National quantities, experience and older identifiers remain intact; six existing national censuses update equipment keys without changing counts.

All thirteen NATO drawings have profile/formation owners. Shared art does not grant every nation every platform: Belgian/Danish recon proxies remain for separate review, and Denmark's existing air-defense restriction remains. The US contract below records the shared-art NATO Huey equipment and Robert’s rejection of NL/BE/DK helicopter formations. Use the vault TODO and [current consumer packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-26 HS Game generic NATO roster integration.md>) for verification/adoption status; template changes do not migrate stored OOB/save selections.

## US and shared NATO Huey contract — 2026-09-26

The [US integration contract](<US Roster Integration.md>) records national support equipment, six new US formations and shared-art NATO Huey registrations. UH-1 lift remains organic in Embarked; UH-1C is a separate gunship at Robert’s approved 100-prestige price. Existing Black Hawk, Apache and Cobra equipment remains intact; US Hawk now uses the national US truck. Robert rejected NL/BE/DK Huey air-mobile/gunship formations; the two NATO equipment entries remain unassigned. Use the vault TODO and [consumer packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-26 HS Game US and NATO Huey integration.md>) for live verification and adoption status.

## Current authoring restrictions and rendering contract

Robert withdrew Iranian and Saudi air-mobile forces on 2026-09-23. Do not add the previously proposed `HEL_UH1_IR`, `HEL_UH1_SA`, or `HEL_UH1C_SA`, their templates, or equivalent formations using shared foreign lift. Their `IR_UH1_Frame0..5`, `SA_UH1_Frame0..5`, and `SA_UH1C_Frame0..5` drawings are no longer requirements. The Iranian AH-1 Cobra is registered under the Iranian contract above. None of these withdrawn identifiers exists in the checked game or editor source; no save-version change is required. See the [coordination record](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-23 HS Game roster withdrawals and map airfield art.md>).

`ME_Airbase` is map art for a potential airfield, selected from `HexTile.IsAirbase` by [HexGridRenderer](<../Assets/Scripts/Renderers/HexGridRenderer.cs>). It must remain available even without an airbase unit. `BASE_AIRBASE` remains valid: [GameIconRenderer](<../Assets/Scripts/Renderers/GameIconRenderer.cs>) handles AIRB units before bay-based icon selection, using the existing generic `AirbaseStack_0..4` hex badges. Its profile fallback to `ME_Airbase` must not cause a unit-art checklist requirement. Broader asset organization is deferred.

This pointer deliberately does not duplicate the plan or progress ledger. Game source/assets/tests remain the authority for actual profile registration, bay selection, sprite resolution, and compatibility behavior.
