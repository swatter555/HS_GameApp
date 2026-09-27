# HS Game Repository Map

**Status:** Static inventory and architecture map, scanned 2026-09-04 against code commit `3123caf`.

**Owner:** HS Game Lead Agent. Update affected sections with structural changes.

**Start here:** [AGENTS.md](../AGENTS.md) → company standard → job description → [current TODO][todo].

**Current roster/art work:** [Completion-plan and local checklist navigation](<Roster and Art Completion.md>) links the reconciled vault plan and Robert's file-backed sprite creation checklist. Its progress is separate from Unity import and gameplay verification.

This map describes the implementation that exists. The [design document][design] and its supplements describe intended behavior, which is not all implemented. The scan covered the tracked/nonignored file inventory, all first-party C# files for types/imports and unfinished-work markers, runtime call sites, assemblies, project settings, scene callback/script references, content headers and assets. Key load, turn, combat, persistence and rendering paths were inspected in detail. This was not a line-by-line correctness audit of every asset/vendor file, a Unity import, a build, or a test run.

## Project at a glance

| Item | Verified baseline |
|---|---|
| Product | Great Campaigns: Hammer and Sickle / `HS_GameApp`; OPFOR Games; Windows/Steam target |
| Engine/rendering | [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt): Unity `6000.2.6f2`; [manifest.json](../Packages/manifest.json): URP `17.2.0`, Input System `1.14.2`; Linear color; ForceText asset serialization |
| Scene entry points | [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset): index 0 `MainMenu`, index 1 `BattleScene`; no scene per mission |
| First-party source | 127 C# files under `Assets/Scripts`, 7 under `Assets/Editor`; 66 test/helper C# files, including 63 `*Tests.cs` files |
| Assemblies | Runtime `Main`; `EditorTests`; `RuntimeTests` is Editor-only and currently contains no C# tests; LeanTween has its own vendor assembly |
| Playable content | One standalone Khost folder; campaign Khost is a separate, undiscovered mockup folder |
| Persistent versions | `GameData.SAVE_VERSION = 11`; minimum supported save tracks it. Map data version = 2, exact-version read gate |
| Known frontier | Remaining icon/roster art, combat presentation, air mission integration, logistics, requisition, AI order execution, campaign/save UI |

## Repository layout

| Location | Purpose / handling |
|---|---|
| [Assets/Scripts](../Assets/Scripts/) | Game source; navigation below |
| [Assets/Tests](../Assets/Tests/) | NUnit/Unity tests, fixtures and deterministic dice |
| [Assets/Editor](../Assets/Editor/) | Content/audio/UI validation and terrain generation tools |
| [Assets/Scenes](../Assets/Scenes/) | Main menu and shared battle scene; preserve serialized references |
| [Assets/Art](../Assets/Art/) | Source sprites, terrain tiles, atlases and prefabs; source art and `.meta` files belong together |
| [Assets/Resources](../Assets/Resources/) | Runtime Resources loads: audio catalog, fonts, scenario thumbnails, chunk material/noise and locally generated terrain arrays |
| [Assets/Audio/SFX](../Assets/Audio/SFX/) | Imported, preloaded short effects; deliberately not streamed |
| [Assets/StreamingAssets](../Assets/StreamingAssets/) | Read-only scenario/campaign files and streamed music/ambience/narration |
| [Assets/Input](../Assets/Input/), [Assets/Settings](../Assets/Settings/) | Input assets and URP/volume configuration; battle input also has inline serialized actions |
| [Assets/Shaders/Chunked](../Assets/Shaders/Chunked/) | Handwritten URP terrain shader and HLSL noise support |
| [Packages](../Packages/), [Assets/Packages](../Assets/Packages/) | Unity manifest/lock and extracted NuGet libraries; package layouts contain different versions, so use actual assembly/plugin references before changing dependencies |
| [Assets/LeanTween](../Assets/LeanTween/), [Assets/TextMesh Pro](../Assets/TextMesh%20Pro/), [Assets/NuGet](../Assets/NuGet/) | Vendor code, examples, resources and package tooling; vendor demo scenes are not game build scenes |
| [Assets/Road List](../Assets/Road%20List/) | Ancillary road reference files; do not confuse with active runtime code |
| [ProjectSettings](../ProjectSettings/) | Versioned engine, build, input, layers and serialization settings |
| [Planning Docs](../Planning%20Docs/) | Existing implementation plans, surveys and courier history; old status headings may be stale. Current work is in the vault TODO |
| `Claude Backup/` | Robert's intentionally moved historical instructions/context/TODO. Local historical reference, not active agent authority. At scan time this folder is untracked and the three old root files have unstaged deletions |
| `.codex/`, `.claude/`, `.github/` | Local agent configuration/history and Copilot instructions. Preserve unrelated files. The existing Copilot file contains Azure-specific advice, not HS runtime rules |
| `Library/`, `Logs/`, `obj/`, `UserSettings/`, `.vs/`, generated `.csproj`/`.sln` | Local generated/editor state, excluded by Git; not authoritative source or deliverables |
| `GeneratedAssets/` | Local top-level directory with no nonignored files in the inventory; inspect its actual producer before using it |

The vault is `C:/Users/coder/Desktop/Codex Projects`. Company standards, job descriptions, the active TODO, design and cross-project correspondence live there. Versioned setup, implementation architecture and data-contract details live here. The vault is outside this Git repository; repository commits do not back it up.

## Runtime ownership and navigation

Paths below are relative to `Assets/Scripts`.

| Area | Entry points and responsibilities |
|---|---|
| Session state | [Controllers/GameDataManager.cs](../Assets/Scripts/Controllers/GameDataManager.cs): unit/leader registries, map/static references, manifest cache, loss ledgers, occupancy and transient links; `AssignLeaderToUnit`/`UnassignLeader` maintain both directions |
| Battle state | [Controllers/BattleManager.cs](../Assets/Scripts/Controllers/BattleManager.cs): scenario setup, phases, player prestige wallet, scoring, victory gate, AI belief ownership and refresh/upkeep orchestration |
| Events and scene flow | [Controllers/EventManager.cs](../Assets/Scripts/Controllers/EventManager.cs), [SceneManager.cs](../Assets/Scripts/Controllers/SceneManager.cs); [Scene Management/Controllers](../Assets/Scripts/Scene%20Management/Controllers/) own menu/battle overlays and startup checks |
| UI surfaces | [Scene Management/Dialogs](../Assets/Scripts/Scene%20Management/Dialogs/): default menu, scenario selection, battle HUD and orders; [Core/UI](../Assets/Scripts/Core/UI/): panels/list, button audio/hover, printer history/dispatch/message formatting; [ReactivePanelManager.cs](../Assets/Scripts/Controllers/ReactivePanelManager.cs) manages reactive panels |
| Input → orders | [Services/InputService_BattleMap.cs](../Assets/Scripts/Services/InputService_BattleMap.cs) reads input; [HexDetectionService.cs](../Assets/Scripts/Services/HexDetectionService.cs) translates pointer hits; [MovementController.cs](../Assets/Scripts/Controllers/MovementController.cs) owns selection, movement, attack/deployment/intel commands and transit-fire integration |
| Camera and cursors | [Services/CameraService.cs](../Assets/Scripts/Services/CameraService.cs), [Controllers/CursorController.cs](../Assets/Scripts/Controllers/CursorController.cs), [UnitMoveAnimator.cs](../Assets/Scripts/Controllers/UnitMoveAnimator.cs) |
| Units/equipment | [Models/CombatUnit](../Assets/Scripts/Models/CombatUnit/): `CombatUnit` state/actions/supply/attachments; `EquipmentBays` eligibility, active profile, icon and aggregate census; `WeaponProfile` resolved equipment definition; `WeaponProfileDB` profiles; `CombatUnitDB` templates |
| Traits/rating | [Models/CombatUnit/Traits](../Assets/Scripts/Models/CombatUnit/Traits/): archetypes/families, stat axes, trait/capability identifiers/catalogs and `TraitResolver`; central enums/constants in [Core/Game Data/GameData.cs](../Assets/Scripts/Core/Game%20Data/GameData.cs) |
| Combat | [Models/Combat](../Assets/Scripts/Models/Combat/): `CombatMath`/`CombatEngine` lanes and dice; `CombatResolver` direct/indirect/air/base/AD resolution; `GroundCombatAction`, `IndirectCombatAction`, `AmbushAction` perform domain actions; stand/surrender/degradation/retreat, recon, dogfight and helo checks; `ICombatRandom` injects randomness |
| Spotting/territory | [Services/SpottingService.cs](../Assets/Scripts/Services/SpottingService.cs) owns player and AI sweeps, decay and transit AD contacts; [TerritoryService.cs](../Assets/Scripts/Services/TerritoryService.cs) owns movement control flips; [MovementModeService.cs](../Assets/Scripts/Services/MovementModeService.cs) centralizes current movement medium |
| Map/coordinates | [Models/Map](../Assets/Scripts/Models/Map/): `HexMap`, `HexTile`, header/data/borders, world/grid geometry and `VictoryLedger`; [Utils/HexMapUtil.cs](../Assets/Scripts/Utils/HexMapUtil.cs) movement/range/path/terrain rules; [Models/General/Position2D.cs](../Assets/Scripts/Models/General/Position2D.cs) coordinates |
| Leaders | [Models/Leader](../Assets/Scripts/Models/Leader/): leader model, data/snapshot helpers, skill catalog/tree and branch helpers; awards/recruitment/details surfaces remain work owed |
| AI foundations | [Models/AI](../Assets/Scripts/Models/AI/): `CombatOracle`/`Pmf`, `MobilityMap`, `RegionGraph`, `ChokepointAnalysis`, `BoardAnalysis`, `AvenueAnalysis`, `AmbushSiteCatalog`, `AIPerceptionState`; no tactical turn executor yet |
| Persistence/content | [Core/Persistence](../Assets/Scripts/Core/Persistence/) and [Core/Helpers](../Assets/Scripts/Core/Helpers/): snapshot/DTO/policies, save IO, map/OOB loaders; [ScenarioManifest.cs](../Assets/Scripts/Core/Game%20Data/ScenarioManifest.cs) discovery metadata and paths |
| Other domain/support | [Models/General](../Assets/Scripts/Models/General/): `StatsMaxCurrent`, `PrestigeWallet`, `Position2D`; [Core/Campaign/CampaignDateCalendar.cs](../Assets/Scripts/Core/Campaign/CampaignDateCalendar.cs); [Services/AppService.cs](../Assets/Scripts/Services/AppService.cs) paths/errors/test handler; `NameGenService` and [Utils/NationalityUtils.cs](../Assets/Scripts/Utils/NationalityUtils.cs) identity/display helpers |
| Rendering | [Renderers/Chunked](../Assets/Scripts/Renderers/Chunked/) terrain mesh/variants; [HexGridRenderer.cs](../Assets/Scripts/Renderers/HexGridRenderer.cs) overlays; [GameIconRenderer.cs](../Assets/Scripts/Renderers/GameIconRenderer.cs) unit/map icons; `HexLayer`, `SortingConfig`, `BattleBackgroundFitter`; [SpriteManager.cs](../Assets/Scripts/Controllers/SpriteManager.cs) atlas lookups and names |
| Prefab behavior | [Core/Prefab Scripts](../Assets/Scripts/Core/Prefab%20Scripts/): unit/terrain panels, unit/city/map/bridge/text visuals. `ParkedCode.cs` contains inactive historical code, not a separate system |
| Audio | [Audio](../Assets/Scripts/Audio/): `GameAudio` facade, shared `AudioFogPolicy`, weapon-family classification, catalog and SFX player; [GameAudioManager.cs](../Assets/Scripts/Controllers/GameAudioManager.cs) audio lifecycle/streaming/settings |

Airfield map art and airbase unit art have separate paths: `HexGridRenderer` uses themed map icons (including `ME_Airbase`) for `IsAirbase` sites, while `GameIconRenderer` handles AIRB units with generic stack badges before equipment-bay art selection. See the [roster/art contract](<Roster and Art Completion.md>) for checklist exclusions and current editor authoring restrictions.

The [French roster contract](<French Roster Integration.md>) records the seven profiles added by `WeaponProfileDB.CreateFrenchRosterAdditions` and six templates added to `CombatUnitDB.CreateFrenchForces` (193 profiles / 189 templates overall). Puma is non-combatant organic transport in an air-mobile unit's Embarked bay; Gazelle has a separate attack template. The AMX-10P carrier option retains the existing VAB infantry template. New `WeaponType` members are appended; the old census-only `IFV_AMX10P` token remains valid. Consumer catalog adoption is tracked in the vault rather than inferred from game registration.

The subsequent [Iraqi roster contract](<Iraqi Roster Integration.md>) adds nine national profiles and six templates (202 profiles / 195 templates overall). `CreateIraqiRosterAdditions` owns the new equipment; existing Iraqi infantry/heavy-artillery/SAM templates now select national support profiles. Armed Mi-8AT is a rocket-attack counter with no lift role; no Iraqi Mi-24 is registered. Soviet and Iraqi S-75 profiles share `S75SiteDef()` while keeping distinct art/censuses. Legacy `_ARAB` registrations remain for existing content and other nations; no stored bay IDs are migrated.

The [Iranian roster contract](<Iranian Roster Integration.md>) adds nine profiles through `CreateIranianRosterAdditions` and six templates in `CreateArabForces` (211 profiles / 201 templates overall). Existing Iranian infantry/gun/AAA templates now use national support profiles. Chieftain, Cobra, F-5E, M109 and M113 recon own their national art; the Hawk template deliberately shares `SAM_HAWK_US`, including its art/census. The M113 carrier remains separate from recon. No Iranian air-mobile force or S-75 is registered; persisted identifiers and stored bay selections remain stable.

The [British roster contract](<British Roster Integration.md>) adds ten profiles through `CreateBritishRosterAdditions` and nine templates to `CreateBritishForces` (221 profiles / 210 templates overall). `APC_FV432_UK` gains a playable profile at its existing enum value; nine other IDs are appended. Puma is organic lift, Lynx is the independent TOW gunship, and Jaguar uses a shared definition preserving French statistics. Chieftain, FV432 and FG.1 add options without replacing Challenger, Warrior or Tornado. All 28 British PNGs belong to national profiles in the NATO atlas; trucks remain shared.

The [German roster contract](<German Roster Integration.md>) adds six profiles through `CreateGermanRosterAdditions` and seven templates to `CreateGermanForces` (227 profiles / 217 templates overall). Tracked Roland adds point air defense, Alpha Jet adds light strike, and MARS I shares the unchanged US MLRS combat definition with national census/art and German availability. The M113 infantry option reuses the existing carrier profile; all twelve previous German templates stay intact. Air-mobile/M113/UH-1D art is connected, Hawk and the NATO truck remain shared, and all 30 German PNGs have profile owners.

The [Generic NATO roster contract](<Generic NATO Roster Integration.md>) adds seven profiles through `CreateGenericNatoRosterAdditions` and eleven national templates (234 profiles / 228 templates overall). Five Dutch/Belgian defaults now use YPR/AIFV, M113 C&V, PRTL and NATO F-16; existing national census quantities remain with corrected equipment keys. Robert chose the older 105mm Centurion Mk 5/2, 60 tanks and cost55 for Denmark. All thirteen NATO PNGs have profile/formation owners. Shared M109/Gepard/F-16 definitions preserve existing combat behavior; stored bay selections are not migrated. Belgian/Danish recon proxies and the approved NATO Huey follow-up remain explicit open work.

### Assembly and lifecycle constraints

[Main.asmdef](../Assets/Scripts/Main.asmdef) references TextMeshPro and Input System by GUID, LeanTween by name, and explicit `System.Text.Json.dll`/`System.IO.Pipelines.dll` precompiled references. Its `noEngineReferences` is false. [AssemblyInfo.cs](../Assets/Scripts/AssemblyInfo.cs) exposes internals to `EditorTests` for actual test seams. Do not infer package availability from a generated IDE project file.

`GameDataManager` persists with `DontDestroyOnLoad`; `BattleManager` is scene state. Both `Instance` getters may find/create GameObjects. `Core/Patterns/Singleton<T>` instead stores an instance in `Awake` and clears it in `OnDestroy`. These are different existing patterns. A null check on a lazy getter can initialize a service; comments claiming otherwise are stale. New services follow the company rule of explicit ownership/wiring; changing existing lifetimes requires a scoped migration.

The nondefault script execution orders in `.meta` files are `GameIconRenderer` 100, `BattleManager` 120, `InputService_BattleMap` 140 and `HexDetectionService` 150. Preserve those metadata files. Event declarations can exist before consumers; a declared AOB/resupply event is not an implemented feature. Pair subscriptions with teardown and account for singleton destruction order.

### Scenario and turn flow

1. `Scene0_Controller` validates game databases and opens menu UI. `ScenarioDialog_Scene0` recursively discovers manifests below `ScenariosRootPath`, stamps `ContentRoot`, validates and lists them, and selects `GameDataManager.CurrentManifest` before loading `SceneID.BattleScene`.
2. `Scene1_Controller` initializes the printer and invokes `BattleManager.SetupBattleManagerData`. Setup loads the map, initializes grid geometry, fits the background, builds terrain chunks, refreshes overlays, loads OOB entities/relationships, reads manifest scoring, resets player-visible enemy spotting and redraws icons.
3. Setup enters Deployment at turn 0 and captures the starting victory ledger. Closing the orders overlay returns focus/input to the HUD. End Turn leaves Deployment through PlayerRefresh and enters turn 1.
4. The existing coroutine cycles PlayerUpkeep → AI_Refresh → AI_Turn → AI_Upkeep → TurnBoundary → PlayerRefresh → PlayerTurn. It yields for presentation delays. AI_Turn is only a timed placeholder; there is no human reaction-window suspension model yet.
5. Refresh resets unit actions/MP/flags and runs the appropriate spotting perspective. Out-of-supply consequences are commented-out hooks; weather remains Clear. Upkeep already performs efficiency recovery, helo over-water consequences and player income; supply generation/replenishment and control decay remain stubs.

`GameDataManager.IsReady` only establishes database initialization. Scene setup currently logs some failures and continues. Do not describe this startup path as an atomic validation gate; see findings below.

### Orders, combat, information and presentation

Pointer input reaches `HexDetectionService`, then `MovementController`; HUD callbacks raise named events into the same action layer. Movement uses `HexMapUtil` and current movement medium, resolves path steps/ambush/transit fire, applies territory/spotting effects and refreshes presentation. A reusable Unity-free move-order API still needs extraction for AI.

Direct/indirect/ambush actions call combat resolvers, update entities and report outcomes. Seedable calculation classes coexist with action classes that access the global manager. A source file lacking `using UnityEngine` can still depend on Unity transitively. The shared game/editor/AI domain assembly is future work, not an existing library.

Air combat resolution and per-hex transit AD/overhead fire exist. `CanLaunchSortie` and `AnimateAutoReturn` have no callers; `AirOperationsBox`, `AirThreatService` and `ReactionWindowController` do not exist in current source. `AOBStatus`/`AOBMissionResolver` are supporting shapes/rules, not the full feature.

Spotting is a six-level contract. Player knowledge is reflected on enemy units; AI knowledge is held separately in `AIPerceptionState`, owned by `BattleManager`. `Region.VictoryValue` sums terrain victory values; strongholds and manifest mission-objective gates are distinct concepts. Do not infer scoring value from an objective flag.

Losses accumulate in `GameDataManager` by weapon type, fed by actual damage and explicit surrender handling, with separate daily/cumulative accumulators. Combat outcomes report kill-prestige amounts, but their wallet-crediting path is still owed. The snapshot does not contain the loss ledgers or AI belief state.

Static authored UI is Inspector-wired to stable public `On…Button()` callbacks. Scene scans find `OnEndTurnButton` and cumulative `OnDisplayLossesButton`; no `OnEndScenarioButton` or `OnDisplayDailyLossesButton` binding was found. These are serialized-reference observations, not proof that every action behaves correctly in play. `BattleBackgroundFitter` is already attached in BattleScene, contrary to older TODO instructions.

## Data and compatibility contracts

| Format | Authority and current behavior |
|---|---|
| JSON policies | [JsonPolicy.cs](../Assets/Scripts/Core/Persistence/JsonPolicy.cs): `Save` uses reference-preservation metadata; `Content` is a lenient plain tree; `Settings` is a separate lenient player-settings policy. All use string-enum conversion. No surviving checksum-options exception |
| Scenario metadata | [ScenarioManifest.cs](../Assets/Scripts/Core/Game%20Data/ScenarioManifest.cs): parameterless DTO/setters, paths resolved against transient `ContentRoot`; validates economy/grade ladder/objective fraction. Current format has no independent format identifier/version gate |
| Maps | [JsonMapHeader.cs](../Assets/Scripts/Models/Map/JsonMapHeader.cs), [JsonMapData.cs](../Assets/Scripts/Models/Map/JsonMapData.cs), [MapLoader.cs](../Assets/Scripts/Core/Helpers/MapLoader.cs): map header version 2; explicit geometry; validation and refusal of out-of-range tiles |
| OOB | [OOBFileLoader.cs](../Assets/Scripts/Core/Helpers/OOBFileLoader.cs): accepts object wrapper with units/leaders and legacy flat unit list; creates/registers units, restores aircraft attachments, then creates/registers/assigns leaders. Some invalid items warn/continue. No current `ArrivalTurn` field |
| Saves | [GameStateSnapshot.cs](../Assets/Scripts/Core/Persistence/GameStateSnapshot.cs), [GameDataObjects.cs](../Assets/Scripts/Core/Persistence/GameDataObjects.cs), [SnapshotMapper.cs](../Assets/Scripts/Core/Persistence/SnapshotMapper.cs), [SaveLoad.cs](../Assets/Scripts/Core/Persistence/SaveLoad.cs): version 11, provenance header, campaign/scenario, embedded map, units/leaders; save/load entry points currently have no callers |
| Player settings | `GameAudioManager` reads/writes `audio_settings.json` under `Application.persistentDataPath` using `JsonPolicy.Settings`; this is separate from shipped content and the AppService Documents paths |
| Future mission/AI files | `.mission` pack and `.aii` schema are planning work. No `.aii` content exists in the current StreamingAssets inventory; do not require one for today's Khost load |

### French AMX-30 DCA correction (2026-09-09)

Robert replaced the French Roland entry with **AMX-30 DCA**, a tracked radar-guided twin-gun SPAAA profile. `SPSAM_ROLAND_FR` is now `SPAAA_AMX30DCA_FR`; the stable template key `FR_AIR_DEFENSE_REGIMENT` remains, with classification SPAAA and the new profile in its Deployed bay. Crotale's four supporting vehicles use the new AAA census key. The AAA archetype, SELF_PROPELLED and RADAR_GUIDED_GUN traits resolve HA4/HD6/SA9/SD8/GAD11/GAT13, movement 10, spotting 3 and the AAA engagement envelope of 3 hexes. Gen3 + SPAAA costs 255 prestige; availability remains turn 468.

Save version 11 uses Robert's explicitly approved development clean break: older saves are rejected, with no migration or old-enum alias. Editor/AI catalogs and any external OOB using the old identifier must be updated together before interchange; no tracked StreamingAssets file contains it. See the [coordination record](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-09 HS Game AMX-30 DCA profile correction.md>). Existing save-load error-reporting limitations remain as documented below.

All six `FR_Roland_*` PNGs and their matching constants became `FR_AMX30DCA_*`, preserving asset bytes, `.meta` GUIDs and existing atlas references. The live profile selects `FR_AMX30DCA_W`; facing still rotates this sprite. This focused rename does not complete the broader directional-suffix retirement. `WeaponProfileNatoTests` now covers the corrected profile, template/bay artwork, imported sprite, census and gun sound family. Agent-run Unity 6000.2.6f2 EditMode on 2026-09-09 passed the 12-test NATO suite and 203-test relevant regression selection (including CombatOracle and SaveMigrationLadder); no tests skipped. Static checks verified all six asset hashes/meta files/atlas GUID references. Build and visual play verification remain unrun.

### Geometry, identity and content boundaries

- Pointy-top odd-r grid; minimum 10×10. Dimensions come from `mapColumns`/`mapRows` through `ResolveMapDimensions`. `MapConfig` is used only for the explicitly warned legacy fallback when both dimension fields are absent. Do not derive new geometry from it or treat the two fallback sizes as approved limits.
- The disk map carries the full rectangle. `HexGridSystem.IsInBounds` excludes the final column on odd rows for interaction. Standalone Khost is 32×21 with 672 hex records; impassable overhang filler is valid at the storage layer.
- Design scale is 5 km flat-to-flat per hex and one day per turn. Unit scale is a maneuver formation; “one regiment” is accurate for Soviet formations but is not universal across the national censuses (design §1a).
- Manifest identifiers are the actual saved identity. Current content uses `Mission_Khost` and `Campaign_Khost`; neither equals its folder basename. Preserve identifiers and casing; never normalize them based on historical prose about lowercase folder IDs.
- Persisted enum names, entity IDs and relationship IDs are contracts. Coordinate any rename with every content producer/consumer and the actual compatibility policy. Sound-effect enum ordering is additionally a Unity serialized-asset contract: append rather than reorder existing entries.
- `.oob` `DaysSupply` means real days; `HitPoints` remains a ratio. One unit supply pool: ordinary unit max 5, airbase 30, depot size 30/50/80/110, fixed-wing 0 (base pays for sorties). Ratio-looking all-≤1-day content warns; it is not automatically corrected.
- Map `checksum` remains the editor's fingerprint. `MapChecksumUtility` was deleted; the game does not validate that checksum. Do not restore hash validation or a serializer exception based on the old backup instruction.
- `Scenarios/<folder>` is discovered; `Campaigns/` is not yet exposed by menu discovery. Current Khost OOBs contain 54 units/22 leader rows (standalone) and 56/16 (campaign mockup). These counts describe file rows, not a verified fully loaded state.
- PNG map layers are editor authoring input, never game runtime map geometry. StreamingAssets is read-only game content; AppService provides player-data/log locations under Documents. SFX are imported assets; audio settings use Unity's persistent-data location. Do not recreate the retired Documents/GDP content paths.

### Snapshot order and known safety gaps

Current `ApplySnapshot`: reject newer saves → upgrade/reject older ones → check required content availability → clear manager state → restore campaign/scenario → restore/validate map geometry and neighbors → restore battle scoring → register units and rebuild equipment census → register/restore leaders → rebuild transient links → final validation.

Pre-release minimum currently equals current version, so older dev saves are rejected before the migration ladder can run. `RunMigrationLadder` tests inject a supported range and require exactly one version per step, with failures for missing/nonadvancing steps. Once compatibility is promised for distributed saves, record the supported floor and implement/test the required steps. A save-version bump alone does not solve renamed identifiers in editor exports.

The required “reject incomplete data without damage” policy is not fully implemented:

- `ToSnapshot` catches map-capture failure and permits `MapData = null`, although null also denotes a legitimate between-battle save.
- `ApplySnapshot` clears current state before all payload/relationship checks complete; some registration errors are logged and processing continues. `ValidateLoadedState` logs inconsistencies and catches validation exceptions without rethrowing. `AppService.HandleException` records/logs errors rather than enforcing rejection.
- `SaveAsync` uses `File.Create` directly; there is no temporary-save/atomic-replacement protocol. Safe overwrite and current-state preservation need implementation/tests.
- The existing header supplies provenance, not a universal explicit format identifier. OOB/manifest/settings governance and shared consumer validation remain work to scope.
- OOB loading also has warn-and-continue branches. `Scene1_Controller.Start` logs setup failure then opens its UI flow. These need boundary tests before claiming a fully validated load.

These observations are tracked in the [TODO][todo]; no fixes are part of the documentation foundation.

## Rendering and asset contracts

`HexChunkRenderer` draws the terrain surface; `HexGridRenderer` draws overlays, roads/rivers, labels and feature marks; `GameIconRenderer` draws unit/facility visuals. Geometry comes from `HexGridSystem`. `HexLayer`/`SortingConfig` own overlay and icon ordering; preserve layer 7 (`No Volume Layer`) and its associated `NoVolumeRendering` renderer feature.

Terrain uses 16×16-hex chunks, `HexChunkMeshBuilder`, deterministic `HexChunkVariantSelector`, and [HexTerrainBlend.shader](../Assets/Shaders/Chunked/HexTerrainBlend.shader). The baked array has terrain/variant slots; city and impassable appearance comes from overlays. [TextureArrayBuilder.cs](../Assets/Editor/Chunked/TextureArrayBuilder.cs) owns the actual slot and filename rules. Inspect that source for a bake change rather than retyping a second slot table.

Unit art resolves through `EquipmentBays` → `RegimentIconProfile.Icon` → `SpriteManager`. `Prefab_CombatUnitIcon` rotates only the unit sprite, keeping the base and information elements upright. Helos use the `_Frame0..5` name-based animation; the shared frame-0 suffix is in `GameData`. The September 26 import uses 347 top-down PNGs in `Assets/Art/Sprites/Unit Icons/{NATO,Soviet,Regional,Chinese,Generic}` and five same-named atlases under `Assets/Art/Sprite Atlases`. MainMenu holds explicit references to the recreated atlas GUIDs; `_regionalIconAtlas` retains `FormerlySerializedAs("_arabIconAtlas")`. Further asset renames must preserve the imported `.meta` GUIDs. Hex-shaped overlays go through `FitToCellScale`; point markers retain authored sizing.

Existing profiles now select the final imported sprite names, including `SV_2S5`, German air-mobile/M113/UH-1D art and dedicated French VAB/AUF1/Crotale art. VAB movement is now Wheeled with its real carrier image. Sprite-only names such as `SV_AN12` do **not** rename persisted equipment identifiers (`TRN_AN8_SV` remains unchanged). Robert's four outer-corner centering pixels and full 512 × 512 canvas are intentional. French Puma/Gazelle frames remain provisional, and imported art for planned equipment does not register that equipment or its formations. The unit-icon prefab's default T-72A references the replacement asset. Old `GEN_Base`/`GEN_Depot` entries were removed from Map Icons so they cannot shadow the new Generic atlas art; their original map-folder PNGs/metas remain available.

[`UnitIconAssetTests`](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) complements `IconIntegrityTests` with actual PNG/import checks, unit constant coverage, complete helicopter sets, exact atlas inclusion, packed-name collisions across saved MainMenu atlases, the unit-prefab default sprite, and live profile/template/bay reachability. Run it in EditMode after importing changes. Its inventory test writes `Temp/unit-icon-reachability.tsv` from the real databases, including unused imported art. Three explicit unfinished exceptions remain: `ART_LIGHT_ARAB` → `GEN_LightArt`, `ART_HEAVY_ARAB` → `GEN_HeavyArt`, and `TRK_GEN_ARAB` → `AR_Truck_W`. They await the approved national support split; the guard rejects new gaps and stale exceptions. `BASE_AIRBASE` keeps its separate `ME_Airbase` map-art fallback. The tilde debug enemy-reveal bypass remains in `GameIconRenderer` and is tracked for removal before external builds. Verification and acceptance status live in the [TODO][todo].

Audio uses `GameAudio`/`AudioFogPolicy` to prevent hidden-unit information leaks. Unit-caused effects need attribution; preserve the shared information gate when adding visuals. SFX import policy is enforced by `SfxImportSettings`; streamed music/ambience/narration use `GameAudioManager`. Static `.asset` catalog entries, clips and enum declarations are separate things; use the catalog audit instead of stale historical counts.

## Setup, tests and tools

1. Check Git status and use the pinned Unity editor. Read [Packages/manifest.json](../Packages/manifest.json), [packages-lock.json](../Packages/packages-lock.json) and the relevant assembly definition before dependency changes. Preserve `.meta` GUIDs and Unity YAML references.
2. Ensure Git LFS assets are present for a fresh clone; [.gitattributes](../.gitattributes) defines binary LFS tracking. Preserve existing source line endings; [.editorconfig](../.editorconfig) and attributes intentionally avoid a wholesale C# renormalization.
3. Generated terrain arrays and their `.meta` files are excluded by [.gitignore](../.gitignore). In Unity run `Tools/Hex Chunk/Rebuild All Terrain Arrays` (MiddleEast/Europe/China). Verify Resources loads and any serialized references; recreated assets may have new GUIDs. Original terrain PNGs are tracked.
4. Open MainMenu to exercise normal startup into Khost; directly opening BattleScene without a selected manifest is not the normal setup path.
5. Use Unity Test Runner's EditMode tests for the `EditorTests` assembly. The repository has no CI workflow or dedicated standalone domain-test runner. No Unity executable was found in the default Unity Hub directory during this scan; that is a local discovery result, not a permanent restriction on agent testing. If a runner is unavailable, request the actual run from Robert and keep the result pending.

### Tests by affected behavior

All named suites below live in [Assets/Tests/EditorTests](../Assets/Tests/EditorTests/). Names indicate where to start, not that they cover every requirement.

| Change | Relevant suite families |
|---|---|
| Save/content/geometry | `SaveMigrationLadderTests`, `MapStandardTests`, `ScenarioManifestTests`, `PrestigePersistenceTests`, `MissionObjectiveGateTests` |
| Unit/profile/trait/census | `EquipmentBaysTests`, `CensusIntegrityTests`, `CommodityProfileTests`, `FamilyArchetypeTests`, `WeaponProfile*Tests`, `FrenchRosterTests`, `IraqiRosterTests`, `IranianRosterTests`, `BritishRosterTests`, `GermanRosterTests`, `GenericNatoRosterTests`, `IconIntegrityTests`, `UnitIconAssetTests`, `CombatUnitIntegrationTests`, `DepotSupplyTests` |
| Movement/deployment/intel | `MovementTests`, `MovementMediumTests`, `DeploymentActionTests`, `DeploymentTransitionTests`, `OverWaterGraceTests`, `SpottingServiceTests`, `SpottingRangeTests`, `IntelLadderTests`, `TerritoryServiceTests` |
| Combat/math/retreat | `CombatMathTests`, `CombatEngineTests`, `CombatResolverTests`, `DirectEngagementTests`, `GroundCombatActionTests`, `IndirectCombatActionTests`, `IndirectResolverTests`, stand/surrender/degradation/retreat/hex-arc and leader-skill suites |
| Air | `AirCombatEngineTests`, `AirStrikeResolverTests`, `AirDefenseFireResolverTests`, `AirDefenseTransitTests`, `AirStandCheckTests`, `AirAmbushCheckTests`, `HeloTransitStandCheckTests`, `AOBMissionResolverTests`, `ReconMissionEngineTests`, `BaseCombatResolverTests` |
| AI/rule consistency | `CombatOracleTests`, `BoardAnalysisTests`, `AvenueAndAmbushTests`, `AIPerceptionTests`, `AIPerceptionSweepTests`; include the oracle after any combat-constant change |
| Turn/economy/reporting/audio | `TurnStructureTests`, `PrestigeWalletTests`, `PrestigeIncomeTests`, `VictoryLedgerTests`, `VictoryGradeTests`, `LossLedgerTests`, `AudioPolicyTests`, `AudioSystemTests` |

[TestFixture.cs](../Assets/Tests/EditorTests/TestFixture.cs) provides `BaseTestFixture`/AppService error capture; [MapFixtures.cs](../Assets/Tests/EditorTests/MapFixtures.cs) and [CombatTestDice.cs](../Assets/Tests/EditorTests/CombatTestDice.cs) provide map/dice helpers. Some tests create/access global Unity services. Tests must reset their own relevant state; do not assume “headless” from their namespace. Tests promising a warning must assert it; `LogAssert.Expect` does not silence the Console. Source suite/file counts are not executed test-case counts.

| Editor menu | Source / effect |
|---|---|
| `Tools/UI/Audit Button Wiring`; `Tools/UI/Find Unwired Button Callbacks` | [ButtonWiringAudit.cs](../Assets/Editor/UI/ButtonWiringAudit.cs): inspect authored UI references |
| `Tools/Audio/Audit Catalog` | [AudioCatalogTools.cs](../Assets/Editor/Audio/AudioCatalogTools.cs): report catalog gaps; its separate Create/Update command mutates the catalog |
| `Tools/Audio/Audio Catalog Editor` | [AudioCatalogWindow.cs](../Assets/Editor/Audio/AudioCatalogWindow.cs): edit catalog entries |
| `Tools/Audio/Reimport SFX With Ratified Settings` | [SfxImportSettings.cs](../Assets/Editor/Audio/SfxImportSettings.cs): changes import settings/reimports clips |
| `Tools/Rivers/Verify River Symmetry...` | [RiverSymmetryVerifier.cs](../Assets/Editor/Rivers/RiverSymmetryVerifier.cs): parse maps using `JsonPolicy.Content` and inspect edge symmetry |
| `Tools/Hex Chunk/Rebuild All Terrain Arrays` | [TextureArrayBuilder.cs](../Assets/Editor/Chunked/TextureArrayBuilder.cs): regenerate excluded terrain assets; per-theme commands also exist |
| `Tools/Hex Chunk/Build ALL Phase 1 Test Assets` | [HexBlendTestAssetBuilder.cs](../Assets/Editor/Chunked/HexBlendTestAssetBuilder.cs): diagnostic RGB/noise assets, not the normal terrain path |

## Scan notes and maintenance

- The scan began on `main`, two commits ahead of locally recorded `origin/main`: `0ea43be` German air-mobile profiles/template, `3123caf` sprite-manifest documentation audit. Remote state was not fetched. Existing Claude-file moves and `.codex/` configuration were left untouched by the foundation work.
- Some backup rules are obsolete or inaccurate: unconditional singletons, prohibition of all runtime UI listeners, checksum serializer exception, four-root-document layout, claims that `ClearAll` precedes version checking, constructor-binding advice, and the old tool-specific inability to run tests. Use the current standard, job description, root instructions and actual implementation.
- Old `.gitignore` commentary still describes retired Generated Data/Documents content paths; ignore patterns are active, that historical description is not. `Planning Docs` and source comments can also contain superseded counts/status/paths. Verify before relying on them.
- The vault's design scale supersedes the old blanket “unit = regiment” description. Current manifest IDs supersede historical assumptions about IDs matching folder names. Do not silently migrate either.
- The tracked OOB backup under standalone StreamingAssets may be copied into a build; it needs a deliberate packaging review, not an automatic deletion.
- A clean clone will not have the local untracked `Claude Backup/` or the external vault. Supply the vault at the startup paths (or have Robert identify its new location) before relying on those documents. Old tracked Claude versions remain in Git history at the scan baseline.

To refresh this map, enumerate with `git ls-files` and `rg --files Assets/Scripts Assets/Editor Assets/Tests`; inspect assemblies/build settings, current call sites and relevant scene `.meta` GUID/UnityEvent bindings. Reconcile findings with the active TODO and design. Update this map with implementation changes; keep product priorities in the vault instead of duplicating them here.

[todo]: <C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>
[design]: <C:/Users/coder/Desktop/Codex Projects/HS Game/Design Docs/HS_DesignDoc.md>
