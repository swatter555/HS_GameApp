# Transition.md — Scenario Editor Update Guide (Map + CombatUnit model changes)

**Audience:** an AI agent updating the external **Scenario Editor** that emits `.map`, `.oob`, and `.manifest` files for *Hammer and Sickle*.
**Purpose:** describe what changed in the **map model (`HexTile` / `.map`)** and the **CombatUnit model (`.oob`)** during the "pull the band-aid" migration, so the editor emits files the current game can load. Authoritative source is always the live code under `Assets/Scripts/` — this doc is the delta + the current contract.

> **Serialization engine:** the game uses `System.Text.Json`. Property names below are the exact `[JsonPropertyName]` JSON keys. Enum-typed fields serialize by the game's existing convention — **keep matching whatever encoding the editor already uses for these files** (the migration did not change the encoding; it changed *fields* and *enum members*). The danger area is **enum members that were inserted in the middle of an enum**, which shifts every later integer value (see the ⚠ CRITICAL items).

---

## 0. TL;DR — the things most likely to break the editor

1. ⚠ **`UnitClassification` gained `WW` (after `ATT`) and `TRN` (after `RECONA`)** — *mid-enum insertions*. Every classification from `AWACS` onward now has a different integer. **Emit the new `.oob` string field `classificationName` (enum NAME, e.g. `"BMB"`)**; the integer `Classification` is now only a legacy fallback. Add `WW` and `TRN` to the editor's unit palette.
2. ⚠ **`UnitRole` gained `GroundCombatRecon` (after `GroundCombatStatic`)** — also a mid-enum insertion; every role from `AirDefenseArea` onward shifted. The `.oob` `Role` field is still an int — **regenerate the editor's role-int table from the current enum** (string `roleName` is not yet supported by the loader; on the backlog).
3. **`.manifest`: `maxCoreUnits` is REMOVED → use `deploymentPointCap`.** (`maxDeployLand`/`maxDeployAir` are also retired; `coreForcePointCap` lives in the `.cmp`, not the manifest.)
4. **`.map` `HexTile` ADDED:** `isPort`, `isDeploymentZone`, `isBeachhead`, `hexControlLevel`, `reservedInt1/2`, `reservedFlag1/2`. **REMOVED:** `airbaseDamage`.
5. **`.oob` REMOVED fields:** ICM (relocated to the WeaponProfile) and Silhouette (mechanic deleted) — drop them if the editor still emits them.
6. **WeaponType renames** (`SPSAM_GEPARD_GE`→`SPAAA_GEPARD_GE`, `SPAAA_ROLAND_FR`→`SPSAM_ROLAND_FR`, others possible) — **regenerate the editor's WeaponType name list from the current `WeaponType` enum**; the `.oob` references profiles by string name.
7. **The player's main supply depot must be `DepotCategory.Main`** (the game centers the opening camera on it and keys supply behavior off it).

---

## 1. Manifest (`.manifest`)

`ScenarioManifest` — current fields (unchanged unless noted):

| JSON key | Type | Notes |
|---|---|---|
| `scenarioId` | string | |
| `displayName` | string | |
| `description` | string | |
| `thumbnailFilename` | string | |
| `mapFilename` | string | |
| `oobFilename` | string | |
| `aiiFilename` | string | |
| `briefingFilename` | string | |
| `prestigePool` | int | |
| `isCampaignScenario` | bool | |
| `mapTheme` | enum `MapTheme` | e.g. MiddleEast / Europe / China |
| `difficultyLevel` | enum `DifficultyLevel` | Colonel / MjGeneral / LtGeneral |
| `maxTurns` | int | |
| **`deploymentPointCap`** | int | **NEW — replaces `maxCoreUnits`.** Per-scenario point budget for fielding units in the Deployment phase. |
| `mapWidth` | int | explicit dimensions (≥10) |
| `mapHeight` | int | explicit dimensions (≥10) |

**REMOVED:** `maxCoreUnits`, `maxDeployLand`, `maxDeployAir`. Old manifests still load (extra keys are ignored, missing `deploymentPointCap` defaults to 0) but the editor should emit the new field.

---

## 2. Map model — `.map` file

### 2.1 Envelope (`JsonMapData`)
```
{
  "header": { ... JsonMapHeader ... },
  "hexes":  [ ... HexTile ... ]
}
```

**`header` (`JsonMapHeader`):**

| JSON key | Type | Notes |
|---|---|---|
| `mapName` | string | |
| `mapConfiguration` | enum `MapConfig` | Small (32×21) or Large (32×42) |
| `saveVersion` | int | **stamp `2`** (current `GameData.CurrentMapDataVersion`). The loader now **HARD-REJECTS** any other version (`MapLoader` → `JsonMapHeader.IsCompatibleVersion`) — pre-v2 maps fail to load and must be regenerated. |
| `checksum` | string | SHA-256 over the hex data (see `MapChecksumUtility`); must validate at load |
| `createdAt` | DateTime | ISO timestamp |

### 2.2 `HexTile` — current field contract

| JSON key | Type | Status | Notes |
|---|---|---|---|
| `position` | `Position2D` | | hex coords `{ "x":.., "y":.. }` |
| `terrain` | enum `TerrainType` | | 9 types (§2.4) |
| `movementCost` | int | CHANGED | per-terrain cost (§2.4). **Water is now impassable to ground** — its cost 1 is vestigial. MinorCity = MajorCity = 1. |
| `isRail` | bool | | |
| `isRoad` | bool | | |
| `isFort` | bool | | **mutually exclusive** with `isAirbase`, `isPort` |
| `isAirbase` | bool | | mutually exclusive with `isFort`, `isPort` |
| **`isPort`** | bool | **NEW** | naval port hex; mutually exclusive with `isFort`/`isAirbase` |
| `isObjective` | bool | | sticky objective; flips only by a ground unit ending on it (§17.5) |
| `isVisible` | bool | | |
| **`isDeploymentZone`** | bool | **NEW** | scenario-authored: hexes where the player may place ground/helo units during Deployment (§35.3). Deployment zones live HERE on the map, not in the manifest. |
| **`isBeachhead`** | bool | **NEW** | Marine coastal-landing flag (§9.10.6.2). Data-only for now (no consumer wired yet). |
| `tileControl` | enum `TileControl` | CHANGED | runtime owner: **Red / Blue / Grey / None**. Binary-ownership model — every hex is owned at play time. `None` is an authoring placeholder only. |
| `defaultTileControl` | enum `DefaultTileControl` | | per-hex nationality control code (authoring; §2.5) |
| **`hexControlLevel`** | float | **NEW** | ownership-persistence scalar, range `(0, 1.0]`, **default 1.0**. Underlies `tileControl`; decays ±0.4/Upkeep (HCL). Author as 1.0. |
| `tileLabel` | string | | |
| `largeTileLabel` | string | | |
| `labelSize` | enum `TextSize` | | |
| `labelWeight` | enum `FontWeight` | | |
| `labelColor` | enum `TextColor` | | |
| `labelOutlineThickness` | float | | |
| `victoryValue` | float | | objective value; also the prestige awarded on capture (§17.5.3) |
| `urbanDamage` | int | CHANGED | repurposed: now a proxy value for urban-sprawl tiles |
| `riverBorders` | `JSONFeatureBorders` | | §2.3 |
| `bridgeBorders` | `JSONFeatureBorders` | | §2.3 |
| `pontoonBridgeBorders` | `JSONFeatureBorders` | | §2.3 |
| `damagedBridgeBorders` | `JSONFeatureBorders` | | §2.3 |
| **`reservedInt1`** | int | **NEW** | reserved (no behavior); keep for forward-compat |
| **`reservedInt2`** | int | **NEW** | reserved |
| **`reservedFlag1`** | bool | **NEW** | reserved |
| **`reservedFlag2`** | bool | **NEW** | reserved |

**REMOVED:** `airbaseDamage` — delete it from editor output (airbases now track HP/OperationalCapacity at runtime; nothing authored).

### 2.3 Border features (`JSONFeatureBorders`)
Each of the four border collections is an object with six edge bools + a type:
```
{ "northwest":bool, "northeast":bool, "east":bool, "southeast":bool,
  "southwest":bool, "west":bool, "type": <BorderType> }
```
`BorderType` enum: `None=0, River=1, Bridge=2, DestroyedBridge=3, PontoonBridge=4`. Map each collection to its type: `riverBorders`→River, `bridgeBorders`→Bridge, `pontoonBridgeBorders`→PontoonBridge, `damagedBridgeBorders`→DestroyedBridge. Edges are **per-shared-edge** — both hexes of a shared river/bridge edge set their corresponding edge bit (the game cross-checks both sides).

### 2.4 `TerrainType` + movement cost
9 terrains. Movement cost per `HexTile.movementCost` (ground): Water **(impassable to ground; cost 1 vestigial)**, Clear 1, Forest 2, Rough 3, Marsh 4, Mountains 5, MinorCity 1, MajorCity 1, Impassable 0. Defensive use and rendering read `terrain` directly. (Confirm the integer ordinal against the `TerrainType` enum in `GameData.cs` if emitting ints.)

### 2.5 `TileControl` / `DefaultTileControl`
- `TileControl` (runtime owner): `Red=0, Blue=1, Grey=2, None=3`. Player faction = **Red**, opposing = **Blue**, neutral = **Grey**.
- `DefaultTileControl` (authoring nationality code): `None` + short codes `SV, US, UK, FR, GE, BE, NE, DE, CH, IR, IQ, SA, KW, MJ`. `SV` → Red; the rest → Blue; Grey is scenario-specific. Regenerate exact ints from the `DefaultTileControl` enum.

---

## 3. CombatUnit model — `.oob` file

`.oob` is `{ "units": [OobUnitData...], "leaders": [OobLeaderData...] }` (a legacy bare `[OobUnitData...]` array is still accepted, but emit the wrapper form).

### 3.1 `OobUnitData` — current field contract

| JSON key | Type | Status | Notes |
|---|---|---|---|
| `UnitID` | string | | unique within the file; referenced by leaders + airbase attachments |
| `UnitName` | string | | |
| `MapPosX` / `MapPosY` | float | | |
| `Side` | int (`Side`) | | `Player=0, AI=1` |
| `Nationality` | int (`Nationality`) | | regenerate ints from the `Nationality` enum (15 values) |
| `Classification` | int (`UnitClassification`) | ⚠ LEGACY | kept as fallback ONLY; **fragile** (WW/TRN insertions, §3.3). Prefer `classificationName`. |
| **`classificationName`** | string | **NEW — PREFERRED** | the `UnitClassification` enum **name** (e.g. `"FGT"`, `"WW"`, `"DEPOT"`). The loader resolves this first and ignores the int when present. |
| `Role` | int (`UnitRole`) | ⚠ CHANGED | int still; **regenerate the role-int table** (GroundCombatRecon insertion, §3.4). |
| `IntelProfileType` | int (`RegimentProfileType`) | | which weapon slots are populated (§3.5) — despite the name, it is the `RegimentProfileType` value |
| `DeployedProfileID` | string (`WeaponType` name) | | e.g. `"INF_REG_SV"`; `"NONE"`/empty = unused |
| `MobileProfileID` | string (`WeaponType` name) | | mounted/transport profile; `"NONE"` if none |
| `EmbarkedProfileID` | string (`WeaponType` name) | | air/helo/naval embark profile; `"NONE"` if none |
| `IsMountable` | bool | | true iff a Mobile profile is populated |
| `IsEmbarkable` | bool | | organic embark; naval embark is universal regardless |
| `Experience` | int (`ExperienceLevel`) | | `Raw=0, Green=1, Trained=2, Experienced=3, Veteran=4, Elite=5` |
| `Efficiency` | int (`EfficiencyLevel`) | | `StaticOperations=0, DegradedOperations=1, NormalOperations=2, CombatOperations=3, FullOperations=4` |
| `Deployment` | int (`DeploymentPosition`) | | **explicit values:** `Fortified=0, Entrenched=1, HastyDefense=2, Deployed=3, Mobile=4, Embarked=5` (stable, not ordinal) |
| `Spotted` | int (`SpottedLevel`) | | `Level0=0 … Level4=4` (author player units' enemies-spotted state as needed; usually 0) |
| `HitPoints` | float | CHANGED | **0.0–1.0 RATIO** of the unit's max HP, applied as `Max × ratio` at load. Max is now **40** for mobile units, **60** for bases (HQ/DEPOT/AIRB). Author 1.0 for full. |
| `DaysSupply` | float | CHANGED | **0.0–1.0 RATIO** of the unit's max supply. Max is now **5** for combat units (was 7), **30** for airbases, depot size caps 30/50/80/110. Author 1.0 for full. |
| `DepotCategory` | int (`DepotCategory`) | | `Main=0, Secondary=1`. **The player's main supply depot MUST be `Main`** (camera + supply key off it). |
| `DepotSize` | int (`DepotSize`) | | `Small=0 (30d), Medium=1 (50d), Large=2 (80d), Huge=3 (110d)` |
| `AttachedAirUnitIDs` | string[] | | for airbases: `UnitID`s of attached fixed-wing aircraft |

**REMOVED from the unit model (drop if the editor still emits them):**
- **ICM / IndividualCombatModifier** — relocated to the WeaponProfile (now a property of equipment, not the unit). No per-unit ICM in the `.oob`.
- **Silhouette** — the mechanic was deleted entirely. No silhouette field, tier, or constant.
- Per-unit **action counts** were never authored and still aren't — they are derived from `Classification` at construction (§8.5.8 action table). Do not emit them.

### 3.2 `OobLeaderData` (unchanged)
```
{ "LeaderName":string, "UnitID":string, "Side":int, "Nationality":int,
  "CommandGrade":int, "CommandAbility":int, "ReputationPoints":int,
  "PortraitId":string, "UnlockedSkills":string[] }
```
`UnitID` links to the unit; `UnlockedSkills` is typically empty (skills unlock in-game). Leaders are player-side only in v1. Note HQ and DEPOT units are now **leader-eligible** (you may attach a leader to a player HQ/DEPOT), but airbases are not.

### 3.3 ⚠ `UnitClassification` — full current order (WW/TRN inserted)
```
TANK MECH MOT AB MAB MAR MMAR RECON CAV AT AM MAM INF SPECF ART SPA ROC BM
SAM SPSAM AAA SPAAA ENG HELO FGT ATT  ‹WW›  AWACS BMB RECONA  ‹TRN›  HQ DEPOT AIRB
```
`WW` (Wild Weasel / SEAD aircraft) inserted after `ATT`; `TRN` (fixed-wing transport / airborne resupply) inserted after `RECONA`. Because these are mid-enum, every class from `AWACS` onward changed integer value vs. the pre-migration enum. **This is exactly why `classificationName` (string) was added — emit it.** Add WW and TRN to the editor's unit palette (air-layer units, like FGT/ATT).

### 3.4 ⚠ `UnitRole` — full current order (GroundCombatRecon inserted)
```
GroundCombat GroundCombatIndirect GroundCombatStatic  ‹GroundCombatRecon›
AirDefenseArea AirSuperiority AirMultirole AirGroundAttack AirStrategicAttack
AirRecon AirborneEarlyWarning
```
`GroundCombatRecon` inserted after `GroundCombatStatic`; every role from `AirDefenseArea` onward shifted. The loader still reads `Role` as an int, so **regenerate the editor's role→int mapping from this list.** (A string `roleName` field, mirroring `classificationName`, is on the game's backlog but not yet supported.)

### 3.5 `RegimentProfileType` (the `IntelProfileType` field)
```
Default=0, DEP=1, DEP_MOB=2, DEP_MOB_EMB_HELO=3, DEP_MOB_EMB_AIR=4, DEP_MOB_EMB_NAVAL=5
```
Declares which weapon slots are populated: `Default`/`DEP` = Deployed only (tanks, AAA, SAM, helos, aircraft, facilities); `DEP_MOB` = Deployed+Mobile (mech/motor infantry); `DEP_MOB_EMB_HELO` = + helo transport (air-mobile AM/MAM); `DEP_MOB_EMB_AIR` = + fixed-wing transport (airborne AB/MAB, SPECF); `DEP_MOB_EMB_NAVAL` = + naval (Marines). Match it to the populated `*ProfileID` slots.

### 3.6 WeaponType (profile ID strings)
The three `*ProfileID` fields are **WeaponType enum names** (string-resolved, so reorder-safe — but the name must match exactly). The migration renamed at least: `SPSAM_GEPARD_GE` → **`SPAAA_GEPARD_GE`**, `SPAAA_ROLAND_FR` → **`SPSAM_ROLAND_FR`**. **Regenerate the editor's full WeaponType picker from the current `WeaponType` enum** rather than trusting a cached list; new profiles may also exist. (The underlying stat model changed to Archetype+Delta+Trait, but that is internal to `WeaponProfileDB` — the `.oob` only references profiles by name, so the editor needs only the up-to-date name list.)

---

## 4. Editor update checklist

- [ ] Manifest: emit `deploymentPointCap`; remove `maxCoreUnits`/`maxDeployLand`/`maxDeployAir`.
- [ ] Map: emit `isPort`, `isDeploymentZone`, `isBeachhead`, `hexControlLevel` (default 1.0), `reservedInt1/2`, `reservedFlag1/2`; remove `airbaseDamage`.
- [ ] Map: enforce fort/airbase/port mutual exclusivity; treat Water as ground-impassable; every hex gets a `tileControl` owner.
- [ ] OOB: emit `classificationName` (string) on every unit; keep `Classification` int for back-compat but treat the string as source of truth.
- [ ] OOB: regenerate `Role`→int and the `WeaponType`/`UnitClassification`/`Nationality` lists from the current enums.
- [ ] OOB: stop emitting ICM and Silhouette.
- [ ] OOB: author `HitPoints`/`DaysSupply` as 0–1 ratios; set the player's main depot to `DepotCategory.Main`.
- [ ] Add `WW` and `TRN` unit types and the `GroundCombatRecon` role to the palette.
- [ ] Stamp `saveVersion = 2` (current `GameData.CurrentMapDataVersion`) and a valid SHA-256 `checksum` in the map header — **the loader now hard-rejects any other version.**

---

*Generated 2026-06-24 against the post-migration code. When in doubt, the live `HexTile.cs`, `OOBFileLoader.cs` (`OobUnitData`/`OobLeaderData`), `ScenarioManifest.cs`, and `GameData.cs` enums are authoritative.*
