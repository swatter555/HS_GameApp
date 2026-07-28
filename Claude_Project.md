# Hammer and Sickle — AI Agent Codebase Context

Unity Version = 6000.2.6f2
URP Version = 17.2.0

_Last reconciled against the codebase: 2026-07-27._

**Large files (>1000 lines):** WeaponProfileDB.cs (6,677), CombatUnitDB.cs (5,224), CombatUnit.cs (2,469), GameData.cs (1,939), GameAudioManager.cs (1,682), InputService_BattleMap.cs (1,431), SpriteManager.cs (1,420), BattleManager.cs (1,171), HexGridRenderer.cs (1,096), LeaderSkillCatalog.cs (1,096), GameIconRenderer.cs (1,084). Read these in chunks.

---

## 1. Directory Structure

```
Assets/Scripts/
├── Controllers/         BattleManager, EventManager, GameDataManager, ReactivePanelManager,
│                        SceneManager, GameAudioManager, SpriteManager,
│                        MovementController, UnitMoveAnimator, CursorController
├── Core/
│   ├── Campaign/        CampaignDateCalendar.cs
│   ├── Game Data/       GameData.cs (enums/constants), ScenarioManifest.cs
│   ├── Helpers/         MapLoader.cs, OOBFileLoader.cs, AtlasHelper.cs
│   ├── Patterns/        Singleton.cs
│   ├── Persistence/     GameDataObjects.cs, GameStateSnapshot.cs, SaveLoad.cs, SnapshotMapper.cs
│   ├── Prefab Scripts/  Prefab_CombatUnitIcon, _CityIcon, _BridgeIcon, _MapIcon, _MapText,
│                        _TerrainPanel, _UnitPanel, ParkedCode (commented graveyard)
│   └── UI/              PrinterControl, PrinterMessage, UIPanel, UIListBox,
│                        UIButtonAudio, UIButtonHoverScale
├── Models/
│   ├── AI/              BoardAnalysis, MobilityMap, RegionGraph, ChokepointAnalysis, AvenueAnalysis,
│   │                    AmbushSiteCatalog, AIPerceptionState, Pmf, CombatOracle
│   ├── Combat/          CombatEngine, CombatResolver, CombatMath, CombatEnums, ICombatRandom, HexArc,
│   │                    StandCheck, SurrenderCheck, DegradationCheck, RetreatResolver,
│   │                    GroundCombatAction, IndirectCombatAction, AirCombatEngine, AirStandCheck,
│   │                    AirAmbushCheck, HeloTransitStandCheck, ReconMissionEngine,
│   │                    AOBMissionResolver, AOBStatus
│   ├── CombatUnit/      CombatUnit.cs, RegimentProfile.cs, WeaponProfile.cs, CombatUnitDB.cs, WeaponProfileDB.cs
│   │   └── Traits/      ProfileStat, WeaponCapability, WeaponTrait, WeaponTraitCatalog, TraitDef,
│   │                    TraitEffect, TraitTaxonomy, TraitResolver, Archetype (+TankArchetypes/ProfileDef),
│   │                    FamilyArchetypes
│   ├── General/         Position2D.cs, StatsMaxCurrent.cs
│   ├── Leader/          Leader.cs, LeaderSkillTree.cs, LeaderSkillCatalog.cs, LeaderData.cs, SkillBranchExtensions.cs
│   └── Map/             HexMap.cs, HexTile.cs, HexGridSystem.cs, JsonMapData.cs, JsonMapHeader.cs, JSONFeatureBorders.cs
├── Renderers/           HexGridRenderer.cs, HexLayer.cs, GameIconRenderer.cs, SortingConfig.cs,
│                        BattleBackgroundFitter.cs
│   └── Chunked/         HexChunkRenderer.cs, HexChunk.cs, HexChunkMeshBuilder.cs,
│                        HexChunkVariantSelector.cs
├── Scene Management/
│   ├── Controllers/     Scene0_Controller.cs, Scene1_Controller.cs
│   └── Dialogs/         DefaultDialog_Scene0.cs, DefaultDialog_Scene1.cs, OrdersDialog_Scene1.cs, ScenarioDialog_Scene0.cs
├── Services/            AppService.cs, CameraService.cs, HexDetectionService.cs,
│                        InputService_BattleMap.cs, NameGenService.cs, SpottingService.cs,
│                        TerritoryService.cs
└── Utils/               HexMapUtil.cs, NationalityUtils.cs
```

```
Assets/Art/               Backgrounds, HexTiles/MiddleEast, Maps/Khost, Materials (Map/Shader),
                          Prefabs (Map/UI/Units), Sprite Atlases, Sprites (Bridges, Control Flags,
                          Hex Sprites, Map Icons, National Flags, National Symbols, Nato Symbols,
                          Unit Icons [Arab/Chinese/Generic/NATO/Soviet], UnitPrefab Icons, Utility Icons),
                          Textures, UI Graphics (Buttons, Controls, Icons, Logos, Officer Portraits,
                          Printer, Scenario Thumbs, Shoulder Boards/Rank Boards, Terrain Portraits, Words)
Assets/Editor/            Chunked/ (HexBlendTestAssetBuilder.cs, TextureArrayBuilder.cs),
                          Rivers/ (RiverSymmetryVerifier.cs)
Assets/Generated Data/    aii/, brf/, cmp/, manifests/, map/, oob/
Assets/Input/             Unity Input System config
Assets/LeanTween/         Third-party tween library (Documentation, Editor, Examples, Framework, Testing)
Assets/NuGet/             NuGet for Unity (Editor, Resources)
Assets/Packages/          NuGet packages (Microsoft.Bcl.AsyncInterfaces, System.IO.Pipelines,
                          System.Runtime.CompilerServices.Unsafe, System.Text.Encodings.Web, System.Text.Json)
Assets/Resources/         Chunked/ (TestArray_RGB.asset, NoiseTexture.png, terrain arrays), Fonts/ (Alumni_Sans, Bevan, CourierPrime, Fredericka_the_Great,
                          Protest_Guerrilla, Spectral_SC, Tinas, xFont Assets/*)
Assets/Scenes/            MainMenu.unity, BattleScene.unity (+ auto-generated Battle Scene/ lighting folder)
Assets/Shaders/           Chunked/ (HexTerrainBlend.shader, Includes/HexNoise.hlsl)
Assets/Settings/          URP pipeline assets
Assets/StreamingAssets/   Audio (ambient, briefings, music, SFX)
Assets/Tests/             EditorTests/ (42 NUnit files: combat/AI/spotting/movement/leader/weapon-profile
                          suites + TestFixture.cs base + CombatTestDice); RuntimeTests/ currently unused
Assets/Tools/             (empty — BinaryToJsonConverter deleted 2026-06-15)
```

### Key Namespaces

`HammerAndSickle.Controllers`, `.Services`, `.Models`, `.Persistence`, `.Core`, `.SceneManagement` (dialogs), `.SceneManagement.Controllers` (scene controllers)

### File System (Runtime)

`Documents/My Games/Hammer and Sickle/`: `scenario/` (.manifest), `map/` (.map), `oob/` (.oob), `cmp/` (.cmp saves), `logs/`

---

## 2. Architecture

### 2.1 Singleton Managers

Two patterns coexist:

- **`Singleton<T> : MonoBehaviour`** base (lazy init via `FindAnyObjectByType<T>()`, `DontDestroyOnLoad`): `HexGridSystem`, `HexChunkRenderer`, `HexGridRenderer`, `Scene0_Controller`, `Scene1_Controller`.
- **Plain `MonoBehaviour` + custom `static Instance`** property (assigned in `Awake`): `BattleManager`, `GameDataManager`, `EventManager`, `SceneManager`, `GameAudioManager`, `SpriteManager`, `MovementController`, `ReactivePanelManager`, `GameIconRenderer`, `HexDetectionService`, `CursorController` (self-bootstraps via `[RuntimeInitializeOnLoadMethod]` if no scene instance exists).

Either way, access from gameplay code is `XxxManager.Instance.Foo()`.

### 2.2 Event Bus

`EventManager` is the single repository for game events. C# `Action` delegates with exception-wrapped `Raise*()` methods. `ClearAllSubscriptions()` for scene transitions. See `EventManager.cs` for the definitive event list.

### 2.3 Repository Pattern

`GameDataManager` — central entity registry. `Dictionary<string, CombatUnit>`, `Dictionary<string, Leader>`, `List<ScenarioManifest>`. Registration, retrieval, filtered queries (`GetUnits(predicate)`), bidirectional leader-unit assignment management.

### 2.4 Snapshot Serialization

Three layers: Runtime objects → GameStateSnapshot (serializable) → JSON (System.Text.Json, `ReferenceHandler.Preserve`). `SnapshotMapper.ToSnapshot()` / `.ApplySnapshot()` handles conversion.

### 2.5 MVC-Adjacent

Models: CombatUnit, Leader, HexMap, HexTile, Position2D, StatsMaxCurrent, plus the pure model layers `Models/Combat` (combat engine) and `Models/AI` (board analysis + belief + EV oracle). Views: Renderers, Prefab scripts, UI panels. Controllers: BattleManager, SceneManager, ReactivePanelManager, Scene controllers.

### 2.6 Static Databases

`CombatUnitDB` (~5,393 lines) and `WeaponProfileDB` — static classes with hard-coded weapon profiles and unit templates. `LeaderSkillCatalog` (~1,094 lines) — complete skill database.

**Weapon-rating trait system (`Models/CombatUnit/Traits/`).** `WeaponProfileDB` profiles are built via `WeaponProfile.FromProfileDef(longName, shortName, type, new ProfileDef(archetype, deltas, traits), [upgradePath, turnAvailable])` — the **Archetype + Delta + Trait** model. Archetypes: `TankArchetypes` (tank generations) + `FamilyArchetypes` (all other families, incl. air + a `Recon` archetype). `WeaponTraitCatalog` maps each `WeaponTrait` to stat-deltas / ICM multipliers / capabilities; `TraitResolver` resolves a `ProfileDef` → final 17-stat line + stored ICM + capability set. Authoritative spec: external `Coding Files/HS_DesignDoc_AppendixW_WeaponRatingSystem.md` + `WeaponTrait_Catalog_01.md`. Migration is COMPLETE (all 4 factions + generic bases, zero legacy ctors; per-faction test suites are the source of truth for resolved statlines). Keep the catalog `.md` in sync when adding code traits.

### 2.7 Combat Engine (`Models/Combat` — pure, seedable)

Three layers. **Pure resolvers** (seedable via `ICombatRandom`, stat-struct inputs, no scene state): `CombatEngine.ResolveLane` (§7.6 band engine), `StandCheck`/`SurrenderCheck`/`DegradationCheck`, `RetreatResolver` (displacement §7.9), `AirCombatEngine` (dogfight/breakthrough/stealth §11.4.8), `AirStandCheck`, `AirAmbushCheck`, `HeloTransitStandCheck`, `ReconMissionEngine` (§11.11), `AOBMissionResolver` (operative→box type-flip + §11.1.1a target pre-filter). **`CombatResolver`** bridges real `CombatUnit`s into the pure layer: direct §7.7.3, indirect + counter-battery §7.13, airstrike §11.6, base attack §11.7, AD opportunity fire §11.8, ambush §6.9, plus the live leader terms (`CommandValue`, `LeaderStandMod`, `AmbushScalar`). **Orchestrators** above it: `GroundCombatAction.Execute` and `IndirectCombatAction.Execute` — validate eligibility → spend action economy (§8.2.1) → resolve → probabilistic efficiency/supply degradation (§7.15) → displacement → unregister casualties → outcome struct (+ REP awards). Called from `MovementController` input handlers. Prestige owed is REPORTED, not credited (no §18 economy yet).

### 2.8 AI Layer (`Models/AI` — authoritative design: `Design Docs/Supplements/AI-Design-Supplement.md`)

Option-B honest-spotting model: the AI plays by the same spotting rules as the player via `AIPerceptionState` (belief store — AI-side spotted levels, ghost contacts, §12.6 decay, cheat-ladder dials R0–R4). `BattleManager` owns the instance; the AI_Refresh phase branch runs `SpottingService.StepAIPerceptionDecay` + `RecomputeAIPerception` (an ADDITIVE AI-side region in SpottingService — player-side paths untouched). Board analysis (pure, map-derived, never serialized): `BoardAnalysis` façade over `MobilityMap`/`RegionGraph`/`ChokepointAnalysis`, plus `AvenueAnalysis` (k-diverse corridors) and `AmbushSiteCatalog` (§6.9.1 trigger geometry). `CombatOracle` + `Pmf` = exact analytic EV mirror of the combat engine — its drift guards (`CombatOracleTests`) enumerate the real engine and MUST be re-run after any combat-constant change. Planning detail lives in `Claude_AI_TODO.md`.

---

## 3. Core Systems

### 3.1 GameDataManager

Central state registry. **Selection:** `SelectedHex` (Position2D), `SelectedHexData`, `SelectedUnit`, `SelectedLeader`. Sentinel: `NoHexSelected = (-1,-1)`. **Map:** `CurrentHexMap`, `CurrentMapSize`, `CurrentMapTheme`. **Campaign:** `CurrentCampaignData`, `CurrentScenarioData`, `CurrentManifest`. **Database passthroughs:** `GetUnitTemplate()`, `GetWeaponProfile()`.

### 3.1b BattleManager (~1,171 lines)

Battle-scene lifecycle owner. Scenario setup (`SetupBattleManagerData`: manifest/map/OOB load, zeroes AI SpottedLevels then runs the initial `RecomputeAllSpotting` sweep BEFORE the first icon draw), turn/phase state machine (Turn HUD + phase transitions, `ProcessRefresh` with per-side branches — player refresh vs AI_Refresh AI-perception decay/recompute), weather (`SetWeather` → `OnWeatherChanged`), objectives, statistics, battle status. Owns the `AIPerception` belief store (§2.8). The full reaction-yielding turn loop is M13 — still ahead.

### 3.2 CombatUnit (~2,469 lines, single class — the old ExperienceSystem/DeploymentSystem/Facility partials are consolidated)

**Identity:** UnitID, UnitName, Classification (45+ types), Role (11 types), Nationality (15), Side (Player/AI).
**Position:** MapPos (Position2D), Facing (HexDirection), DeploymentPosition (Fortified→Embarked), EmbarkmentState.
**Stats:** HitPoints (StatsMaxCurrent, max 40), DaysSupply, MovementPoints, Experience (6 levels, 0.8x–1.3x), Efficiency (5 levels).
**Weapons:** RegimentProfile → Deployed/Mobile/Embarked WeaponType configs → WeaponProfileDB lookup.
**Facilities:** IsBase, FacilityType (HQ, Airbase, SupplyDepot, Fort), depot size, generation rate, projection range.
**Air attachment:** Airbases maintain attached air unit lists. **Actions:** 5 types (Move, Combat, Deploy, Opportunity, Intel).

### 3.3 Leader (~902 lines)

**⚠ PLAYER-ONLY, PERMANENTLY (§14.1.1 / §14.2.3, firmed up 2026-07-25).** There are NO enemy leaders in the game
and none are planned. Every `Leader` construction path passes `Side.Player`, so **`Leader.Side` is a vestigial
field** — kept only because it serializes into saves. Never write side-gating logic against it. Consequences
worth holding onto: the AI never receives `Leader_mod`, command shock (§7.9.4a), Command Mitigation (§7.7.12) or
the §6.9 ambush ladder — AI combat resolves at ICM 1.0 unless terrain/weather/scenario says otherwise; leader
mortality (§14.15.4) is a player-side-only mechanic; and no display surface needs to gate leader information by
side, because an enemy unit never reports `IsLeaderAssigned`.

**Identity:** LeaderID, Name, Nationality, Side, PortraitId. **Career:** CommandGrade (Junior/Senior/Top), CommandAbility (Average/Good/Superior/Genius), ReputationPoints. **Skill tree:** 13 branches (Foundation/Doctrine/Specialization), tier progression T1–T5, 50+ bonus types. **Reputation:** Move(1), Intel(2), Combat(3), Airborne(3), Retreat(5), Destroy(8). Promotions: Senior(100), Top(250). **Assignment:** Bidirectional via GameDataManager.

**Combat mechanics are LIVE (2026-07-03 M14 safe slice, R-L1..R-L10):** `EffectiveCommand` (CC + CommandTier skills, cap 3) feeds command shock §7.9.4a + Command Mitigation §7.7.12; `StandValueContribution` (§14.13 Leader_mod, cap +3) derived internally by CombatResolver; six doctrine +2 Δ-side stat deltas, class-gated (`DoctrineDeltaAppliesTo`); ambush ladder 1.5/1.75/2.0 + NCO immunity; UndergroundBunker Level-3 intel cap; SuperiorCamouflage spotting −1. `SyncFromSkillTree()` keeps Leader-side REP/grade mirroring the tree (the tree is the source of truth). REP earning is wired (combat via `GroundCombatAction`, move via `MovementController`).

### 3.4 Hex Map System

**HexMap:** `Dictionary<Position2D, HexTile>`. Small: 32x21 (8192x4096px), Large: 32x42 (8192x8192px). Pointy-top, odd-r. 256px hex size. Neighbor building, pathfinding, 3-phase validation.

**HexTile:** Position, TerrainType (9), MovementCost, TileControl (Red/Blue/Grey/None), IsObjective, VictoryValue, HexControlLevel, IsDeploymentZone, IsBeachhead. Infrastructure: IsRail, IsRoad, IsFort, IsAirbase, IsPort (Fort/Airbase/Port three-way mutually exclusive). Borders: River/Bridge/DestroyedBridge/PontoonBridge per edge via JSONFeatureBorders. IDisposable.

**Terrain costs:** Water(1), Clear(1), Forest(2), Rough(3), Marsh(4), Mountains(5), Cities(1), Impassable(0). **Defense:** Forest(+1), Rough(+2), Marsh(+3), Mountains(+4), MinorCity(+1), MajorCity(+3).

### 3.5 Rendering

**HexGridSystem** (singleton, ~250 lines): Authoritative coordinate math for the pointy-top, odd-r grid with top-left origin (row 0 = highest world Y). Owns hex geometry constants — HEX_WIDTH=2.56, HALF_HEX_WIDTH=1.28, VERTICAL_SPACING=2.217 (width×√3/2), HEX_HEIGHT=2.956 (width×2/√3 — a REGULAR pointy-top hex, taller than wide; hex art in a square canvas is ~13.5% too short and gets fit-to-cell-stretched at stamp time). Sprites are 256px wide at 100 PPU. Provides grid-to-world conversion and direction offset tables (NE, E, SE, SW, W, NW for even/odd rows). All renderers and movement code delegate position math here.

**HexGridRenderer** (singleton, ~1,096 lines, replaces the old HexMapRenderer): Layer-based renderer that owns 16 `HexLayer` instances across three sorting layers — **Map** (bottom→top: hexOutline, mapIcon, riverBank, riverWater, road, bridgeIcon, cityIcon, impassable, hexSelect, mapText — the selection ring sits ABOVE every terrain feature since 2026-07-22; only labels render over it), **Units** (groundUnit, airUnit), **Overlay** (utility1, utility2, movementRange, movementPath). Manages prefab drawing for cities/bridges/icons/text plus direct-stamp overlays for outlines/rivers/roads/impassable, handles event subscriptions, delegates all position math to `HexGridSystem`. `RefreshMap()` full redraw.

**HexLayer** (~110 lines): MonoBehaviour managing a dictionary of child SpriteRenderer GameObjects for one visual concern (outlines, selection, movement range, etc.). It no longer authors sorting itself — it holds a `SortSlot` assigned in code via `Configure()` (HexGridRenderer wires all 16 in `Awake` → `ConfigureLayerSorting`), and `SetSprite` stamps sorting through `SortingConfig`.

**⚠ Render-pass dimension (Unity layer, NOT sorting layer):** the Forward Renderer's transparent mask (119) EXCLUDES Unity layer 7 ("No Volume Layer"); the `NoVolumeRendering` RenderObjects feature (Forward Renderer_Renderer.asset) redraws layer-7 transparents at Event 600 = AfterRenderingPostProcessing. So **Unity layer picks the PASS** (layer 7 = drawn after post-FX, on top of everything in the early pass); **sorting layers only order within a pass**. ALL map-visual objects — prefabs and HexLayer stamps — must live on layer 7; `HexLayer.SetSprite` inherits the host object's layer for exactly this reason (a bare `new GameObject()` defaults to layer 0 and would render under every prefab icon regardless of sorting layer — this was the movement-overlay-under-cities bug, 2026-07-21).

**Sprite sorting — SortingConfig is the SINGLE authority (`Renderers/SortingConfig.cs`).** One static file maps every `SortSlot` (the 16 concerns: HexOutline…MapText on Map, GroundUnit/AirUnit on Units, Utility1/2 + MovementRange/MovementPath on Overlay) to a Unity sorting layer + base order (spaced by 10). `SortingConfig.Apply(renderer, slot, subOrder)` stamps `sortingLayerName`/`sortingOrder`, **overriding whatever was baked into a prefab asset** — nothing else in render code sets sorting. Split of ownership: the sorting **layer + base order** live in SortingConfig; a **multi-part prefab's internal element order** lives as `const … SubOrder` fields **in that prefab's own script** (Prefab_CityIcon, Prefab_CombatUnitIcon, etc.), passed as `subOrder`. Direct HexLayer stamps use subOrder 0. Every prefab is stamped at spawn: map prefabs via `HexGridRenderer` (`prefab.ApplySorting(slot)`), unit icons via `GameIconRenderer` (`ApplySorting(GroundUnit|AirUnit)`). Prefab-baked sorting layers/orders are now dead data. ⚠ This replaced the old dual system (prefab-baked sorting vs per-HexLayer Inspector sorting) that let a stale prefab sorting layer render cities above the movement overlays.

**BattleBackgroundFitter** (~130 lines, on the "Background Room" GameObject under `World Space/Hex Map/Background`): scales/positions the bunker-room background sprite so the map window baked into the art (glowing table surface inside the green tube border — the border is deliberate padding) frames the loaded hex map at ANY map size. Serialized calibration = the window's center offset + size in normalized image coordinates, reverse-engineered from the hand-tuned 32x21 setup (2026-07-22). `BattleManager.SetupBattleManagerData` calls `FitToMap(w,h)` right after `HexGridSystem.Initialize`. Moves the background only, never the map; per-axis scale means maps should stay ~16:9 by authoring convention (Bob). Background Room lives on Unity layer 6 (EARLY pass — under all layer-7 map content by design).

**GameIconRenderer:** Ground/air layers. `RefreshIconFacing(unitId)` re-resolves an existing icon's sprite variant + easterly flipX from the unit's CURRENT `Facing` — movement steps and Shift+click rotation call it (before 2026-07-22 icons resolved facing only at create time). **Helo motion flipbook (2026-07-22):** `Prefab_CombatUnitIcon.StartMotionAnimation`/`StopMotionAnimation` cycle the 6 `<unit>_FrameN` atlas frames at a serialized fps (default 40) while the icon tween-moves — `AnimateIconStep` starts it, `SnapIcon` stops it (rests on Frame0); detection is sprite-name-based, so embarked air-mobile riding helo art animates too and non-animated icons no-op. Sprite resolution from RegimentProfile, directional flipping. Stacking: air/ground same hex → dominant at 1.0 opacity, recessive at 0.6. Toggle via EventManager. Movement animation: `AnimateIconStep(unitId, to, duration, onComplete)` (LeanTween via UnitMoveAnimator) + `SnapIcon` (cancel tween + hard-place) — driven per-hex by `MovementController.ExecuteMovement`. ⚠ Carries the tilde (~) debug enemy-reveal cheat (rendering-only; REMOVE BEFORE SHIPPING — tracked in Claude_TODO Cleanup).

### 3.5c Chunked Terrain Renderer (POC / in progress)

Mesh-based terrain rendering that replaces per-hex sprite stamping with GPU-blended Texture2DArray chunks. Namespace: `HammerAndSickle.Renderers.Chunked`. Multi-phase build: Phase 1 (shader validation), Phase 2 (mesh + chunk grid), Phase 3 (real terrain textures), Phase 4 (variant selection), Phase 6 (integration).

**HexChunkRenderer** (singleton, ~120 lines): Owns the chunk grid and terrain `Material`. `BuildAllChunks(HexMap, HexGridSystem)` clears existing chunks, calculates grid dimensions from map size / ChunkSize, and delegates per-chunk mesh construction to `HexChunkMeshBuilder`. Stores chunks in a flat `List<HexChunk>`.

**HexChunk** (~39 lines): Data container for one 16x16 hex chunk. Owns a `GameObject`, `MeshFilter`, `MeshRenderer`, and generated `Mesh`. Constructor parents under a given transform and assigns shared material. `Destroy()` cleans up Mesh and GameObject.

**HexChunkMeshBuilder** (static, ~384 lines): Builds a Unity `Mesh` for one 16x16 chunk. Each hex = 6 fan triangles (center + 6 Voronoi corners). Corner vertices are deduplicated across hexes sharing the same corner; corner blend weights mix the terrain slots of the 1–3 hexes meeting at that corner. Vertex layout: `POSITION(float3)`, `TEXCOORD0(float2 UV)`, `TEXCOORD1(float4 terrain indices)`, `TEXCOORD2(float4 blend weights)`.

**HexChunkVariantSelector** (static, ~32 lines): Deterministic per-hex variant selection. Hashes `Position2D` to produce a stable variant index [0..11]. `GetSlot(pos, terrain)` = `(int)terrainType * 12 + variant`. 12 variants per terrain type, 9 terrain types = 108 total Texture2DArray slices.

**HexTerrainBlend.shader** (~126 lines, URP unlit handwritten HLSL): Samples a `Texture2DArray` up to 4 times per fragment, blended by per-vertex weights. Optional world-space noise perturbation of weights via shader keyword. Includes `HexNoise.hlsl` for noise functions.

**Editor tools** (`HammerAndSickle.EditorTools.Chunked`): `TextureArrayBuilder` builds a 108-slice Texture2DArray from terrain tile PNGs in `Assets/Art/HexTiles/` (slot math: terrainType * 12 + variant, 512px tiles). `HexBlendTestAssetBuilder` generates Phase 1 POC test assets (3-slice RGB Texture2DArray + tileable noise PNG).

⚠ **REBUILD AFTER A FRESH CLONE — these two outputs are NOT in git.** `Assets/Resources/Chunked/TerrainArray_MiddleEast.asset` is 288 MB, over GitHub's 100 MB hard per-file limit, and `TestArray_RGB.asset` is 8 MB; both are `.gitignore`d (see the rationale block there) while the source PNGs they are built from ARE tracked. A clone therefore has no terrain array until the tools are re-run, and the rebuilt asset carries a NEW GUID — any serialized reference to it must be re-pointed.

**POC drivers** (removed): earlier prototypes `HexChunkPOCDriver`, `HexBlendShaderTest`, and `POCCameraController` have been deleted now that the chunked renderer is driven from `BattleManager` against real scenario data.

### 3.5b Movement System

**MovementController** (singleton, ~859 lines): State machine for player unit movement AND combat input during `BattlePhase.PlayerTurn`. States: `Idle`, `UnitSelected`, `Executing` — `AwaitingTarget` was DELETED 2026-07-06 (no order-confirmation step). Ratified input model (§5.10.6): **left-click = universal select** (enemy click = intel print, never an attack); **right-click inside the movement radius = immediate move**; **Ctrl+left-click = the ONLY combat trigger** — `HandleCtrlClick` → `AttackLegality` (public, shared with CursorController so the cursor never lies) → `TryAttack`, which routes by firer class: ART/SPA/ROC/BM ALWAYS fire §7.13 indirect (even adjacent) via `IndirectCombatAction`; everything else direct via `GroundCombatAction`. Owns range calculation (`MovementRangeResult`), pathfinding, stepped execution (per-hex icon tween + spotting via `SpottingService`, enemy ZoC, ambush), post-move §6.13/§17.5 tile-control via `TerritoryService`, REP move award, and next/previous eligible-unit cycling. Public surface: `CurrentUnit`, `State`, `AttackLegality`. Pending: Move Undo (§5.11).

**UnitMoveAnimator** (static, ~104 lines): LeanTween-based hex-to-hex movement animation for combat unit icons. `AnimateHexStep(icon, to, duration, onComplete)` tweens with `EaseInOutQuad`; per-step `onComplete` callback lets `MovementController` run spotting between hexes. `CancelAndSnap(icon, pos)` kills an in-flight tween and hard-places. Suggested durations: 0.15–0.25s ground, 0.08s fixed-wing.

**CursorController** (~200 lines, self-bootstrapping singleton): §24.11.3 live combat feedback (AMENDED 2026-07-22 — crosshair cursor RETIRED) — poll-based (no EventManager subscriptions, survives `ClearAllSubscriptions`): while Ctrl is held, a LEGAL combat target gets the TargetPickOutline hex stamp on its hex (driven through `HexGridRenderer.ShowCombatTargetPick`/`ClearCombatTargetPick` on the utility1 layer, fit-scaled + serialized tint × opacity; cursor stays the default arrow); anything illegal shows the DENIED cursor. Legality comes from `MovementController.AttackLegality` — the same gate the click runs. Procedural placeholder denied texture until real art is assigned; per-mode cursors (unit-pick §24.5.5, AOB placement §24.7a.1) slot in as those input modes land.

### 3.6 Scene Management & Dialog Flow

**Namespace:** `HammerAndSickle.SceneManagement(.Controllers)`. Each scene: one controller (`Singleton<T>`), one always-visible HOME dialog, zero/one overlay. All switching via EventManager dialog events — `OnScene0DialogRequested` / `OnScene1DialogRequested`, payload `Action<UIPanel>` (actual panel references, no enums/strings). Dialogs never reference controllers; each dialog holds Inspector-assigned `UIPanel` refs for the targets it can request.

**UIPanel base:** `Show()`/`Hide()` toggle a serialized `root` GameObject → `OnShow()`/`OnHide()` hooks. `SetFocus(bool)` → `OnFocusChanged(bool)`. Focus semantics differ by scene:
- **Scene 0 home** (`DefaultDialog_Scene0`): `OnFocusChanged` toggles menu-button `interactable` — buttons die while an overlay is up.
- **Scene 1 home** (`DefaultDialog_Scene1` = the battle HUD): `OnFocusChanged(hasFocus)` → `InputService_BattleMap.SetInputEnabled(hasFocus)`. **This is the single map-input gate** — overlay open = ALL map input dead (scroll, zoom, clicks), and the InputActions themselves are disabled.

**Switch flow:** button onClick → dialog callback → `EventManager.RaiseSceneXDialogRequested(target)` → controller hides `_activeOverlay`; if target == home → restore home focus, else → show target + defocus home.

**Scene 1 startup sequence:** `Scene1_Controller.Start` → validate GameDataManager → `PrinterControl.Initialize()` → `BattleManager.SetupBattleManagerData()` → HUD shown DEFOCUSED → Orders overlay opened via the normal event path. Map input first enables when the player clicks **Begin** (Orders → home switch). Exiting Deployment happens via BattleManager's End Turn button (Turn 0 → Turn 1), not the dialog system.

**DefaultDialog_Scene1 extras:** manual singleton (must extend UIPanel, so can't use `Singleton<T>`). Owns click-through hit-testing: `IsScreenPointOverUI(screenPoint)` tests FOUR Inspector-assigned HUD panel rects (top menu bar, terrain, unit, printer) against `_uiCamera`; `InputService_BattleMap` consults it for BOTH mouse buttons — clicks over HUD panels never reach the map. ⚠ **A null slot FAILS OPEN** — the loop `continue`s past it and the click reaches the map, so a right-click over an unwired panel issues a move order to the hex underneath. That is how the unit panel leaked from the 2026-07-23 panel consolidation until 2026-07-27 (the slot list still said `_unitGroundPanel`/`_unitAirPanel`/`_leaderPanel`, none of which the surviving single unit panel was assigned to). `WarnOnUnassignedPanels()` now names every empty slot — and a null `_uiCamera`, which mis-tests every panel at once — at `Start`. New battle-HUD button callbacks live here (ratified 2026-07-20).

### 3.6b Input Architecture (battle map) — end-to-end

**The chain:**
```
InputAction (6, bindings authored in the Inspector ON InputService_BattleMap:
             WASD scroll, Q/E zoom, wheel zoom, middle-click reset, LMB, RMB — start DISABLED)
  → InputService_BattleMap   (SetInputEnabled gate + UI click-through check + double-click detection;
                              exposes OnLeftMouseClick/OnRightMouseClick/scroll/zoom/hold events)
    → HexDetectionService    (screen→hex via HexGridSystem.ScreenToHex + bounds check;
                              sets GameDataManager.SelectedHex; fires OnHexSelected / OnHexRightClicked;
                              ClearSelectionAndNotify() = the §5.10.5 clear branch)
      → MovementController   (select / move / Ctrl-combat — reads Shift/Ctrl via Keyboard.current
                              inside the click handler, §5.10.6)
      → ReactivePanelManager (terrain/unit/leader panels + printer)
```
Outside the chain, poll-based and NOT gated by `SetInputEnabled`: `CursorController` (Ctrl-legality cursor), the tilde debug reveal (GameIconRenderer), and every uGUI HUD button. The planned input-mode state machine (Normal / CtrlCombat / UnitPick / AOBPlacement / AOBMode / ReactionInterceptorPick, §24) will live in this layer when M13/AOB lands.

**Script Execution Order (Bob-owned, Project Settings; stored in .cs.meta, NOT in git):**
default 0 (everything else) → GameIconRenderer 100 → BattleManager 120 → InputService_BattleMap 140 → HexDetectionService 150. Unity runs each script's Awake+OnEnable as a pair in SEO order at scene load, so the ordered services' `Instance`s DO NOT exist yet while any default-order script runs Awake/OnEnable. **RULE: cross-singleton event subscriptions happen in `Start()`, never in Awake/OnEnable** (ReactivePanelManager and MovementController comply; HexDetectionService subscribes to InputService in Awake only because 140 < 150 makes it safe). Adding a new subscriber: subscribe in Start, or ask Bob for an SEO slot.

**Division of labor (RATIFIED 2026-07-27, supersedes the 2026-07-20 split):**
- **Bob (Inspector/scene):** scene hierarchy + component placement; ALL serialized references (controllers' dialog panels, dialogs' nav targets, HUD panel rects, `_uiCamera`, printer); InputAction bindings; the SEO list; and **every button's onClick** → a public `OnXButton()` callback method.
- **Agent (code):** everything from the callback inward — dialog-flow logic, focus semantics, EventManager raise/subscribe, input-service internals, all consumers.

**⚠ ONE RULE FOR BUTTONS: the Inspector owns onClick. There is NO code `AddListener` anywhere.** The old split (nav = Inspector, gameplay = code `AddListener`) is RETIRED — it forced a judgment call per button, and getting it wrong either double-fired or did nothing. One mechanism means the double-fire hazard cannot exist. The three code-wired holdouts (End Turn on BattleManager, next/prev unit on DefaultDialog_Scene1) were converted the same day, and their `Button` fields deleted.

**A script holds a serialized `Button` ONLY if it must drive that button's state** — `interactable`, label, visibility — never for wiring. `DefaultDialog_Scene0` is the reference case: menu buttons are Inspector-wired for onClick, and the refs exist purely so `OnFocusChanged` can grey them out while an overlay is up. Wiring and state are separate concerns.

**⚠ Public `On*Button()` NAMES ARE A CONTRACT.** A UnityEvent binds by method-name STRING, so renaming a callback silently breaks the Inspector wiring — no compile error, just a dead button. Never rename one without telling Bob (CLAUDE.md §2.13). This is the cost of the Inspector owning the wiring, and it is paid deliberately: the alternative cost was Bob needing a code change for every button while reworking the HUD.

**Where state gating went when the Button refs left:** `BattleManager.CanEndTurn` (phase + battle-over check) is now evaluated INSIDE `OnEndTurnButton`, not used to disable a button. A guard on the logic holds however the button is wired, or if it is not wired at all — strictly more robust than a UI-level gate. The visible greying-out of End Turn during non-player phases is GONE; `_turnProcessingPanel` still shows during those phases, so the player is not without feedback.

### 3.6c HQ Dispatch Feed (the printer — §24.8; CRT + emitters CONFIRMED IN PLAY 2026-07-27)

**The register:** terse field dispatches arriving from subordinate units, in the manner of MicroProse's *Decision
in the Desert*. The test for whether something belongs (§24.8.2): **the printer reports what the player did not
order and could not otherwise see.** Anything they commanded and watched resolve is a receipt, not a dispatch —
which is why the AI turn is the feed's most valuable content. Enemy dispatches are spotting-gated, so the §12
ladder pays off a second time.

**`PrinterMessage`** (~data model): the §24.8.5a frame, REVISED 2026-07-26 to `12: Message from 3rd Tank Rgt`
followed by the body — the turn/date line was folded into the source line because long unit names wrapped and
cost a line of a fixed-height CRT. `Turn` / `Source` (the filing unit, or a `SourceDivisionalHQ` /
`SourceSupplySection` / `SourceWeatherSection` letterhead — a destroyed unit cannot file its own report) /
`Lines` / `PrinterCategory` (the FILTER tag). `Abbreviate` shortens formation words (Regiment→Rgt, Motor
Rifle→Mtr Rifle …) at RENDER time only, so `Source` keeps the real name. `FlowIntoColumns` packs equipment
entries to CRT width. `CreateUnitReport` keeps the §12 rung gating built 2026-07-24, but is now only the
§24.8.6 intel-DISPATCH body — enemy selection readouts moved to the unit panel (§4.3).
⚠ **The campaign date is GONE from the frame**, which also retired the §24.8.5a day-level-date problem — nothing
needs a date now. ⚠ The turn is supplied through the static **`TurnProvider`** seam rather than read directly:
`BattleManager.Instance` LAZY-CREATES a GameObject, so a data class touching it would spawn a manager out of any
headless EditorTest. PrinterControl installs the provider in `Initialize()` and clears it in `OnDestroy`.

**`PrinterDispatch`** (static, `Core/UI`): builds and files dispatches — one place owns both the ratified §24.8.6
body text AND the decision about whether an event is worth printing, so call sites are one line and the two
cannot drift. **Three gates, of which a dispatch needs one: (A) happened OUT OF VIEW, (B) explains a state
change whose cause is off screen — ATTRIBUTION, (C) carries an ASSESSMENT the player would otherwise compute.**
A message that only restates a number readable off the icon or unit panel fails all three. `Verbose` selects how
strictly that applies: ON files everything; OFF reports by exception — defensive reports always file (gate A),
the player's own attacks file only on losses ≥ Moderate, a changed enemy state, or an attack that cannot
continue. OFF is the design intent; ON exists to compare in play, and may become a player option.
Loss bands (`LossBand` + `LOSS_BAND_*` in GameData) describe damage taken in ONE exchange as a fraction of MAX
HP. ⚠ They are NOT the Section 8 strength floors, which measure hit points REMAINING and feed combat
multipliers — do not merge them.
⚠ Combat reports take the contact hex **captured before resolution**: a defender that retreats has already moved
by the time the outcome returns. ⚠ Indirect fire reports only after the WHOLE action including counter-battery,
or the firer prints "no losses" and is then contradicted by its own tubes being killed in the same exchange.
Both `Report*` methods handle BOTH sides in one call, so the defensive branch goes live for free when the M13 AI
turn starts calling the same orchestrators.

**`PrinterControl`** (~600 lines): ONE message displayed at a time on the centre CRT. `List<PrinterMessage>`
history + a cursor into a filtered *view* + a single `TextMeshProUGUI`. Typewriter reveal advanced by
`Time.deltaTime * _charsPerSecond` (framerate-independent), blinking cursor at rest, and a nav press DURING the
reveal completes the message instead of navigating (§24.8.4.2). `MSG n / N` readout; the latest-indicator lights
while the cursor is off the newest message, doubling as the unread flag — so a new dispatch auto-follows ONLY if
the player was already on the newest. ⚠ **Auto-sizing is forced OFF in code**, not left to the Inspector: TMP fits
per text object, so size would jump between a 2-line dispatch and the 9-line loss report *and* resize mid-type as
the revealed substring grows.

**Visibility: OPEN FROM SCENE START, never closes** (2026-07-27; part of the three-panel model in §4.3). It comes
up showing `— NO MESSAGES —` with the cursor blinking. There is no close path, and right-click deselect does not
clear it either: the terrain and unit panels empty on deselect, but a dispatch log is not about the selected hex.
CLEAR empties the log without closing the CRT. Two earlier models were tried and dropped — hide-on-right-click
(inherited from the interim RPM behaviour, wrong once the printer became a non-contextual log, because dismissing
it stranded the history behind nav buttons inside the hidden root) and open-on-first-hex-click. `ShowPanel()`
survives as a no-op guard for a root switched off by hand. `ReactivePanelManager` does not touch the printer.

⚠ **The trap that remains:** `PrinterControl` must live on an ALWAYS-ACTIVE object with a serialized `_panelRoot`
for the toggled visual. It subscribes to `OnPrinterMessage`, and the panel is inactive until the first dispatch —
so hosting the component on `_panelRoot` itself would leave it disabled exactly when the message that would
enable it arrives. `Initialize()` warns if the two are the same object, is idempotent, and is called from both
`Scene1_Controller.Start` and its own `Start()`, so a dispatch raised during BattleManager setup cannot be
dropped to Start-order luck.

**Buttons:** the six on-CRT controls are Inspector-wired to public `OnPrinter*Button()` callbacks on
`DefaultDialog_Scene1`, each raising an EventManager event PrinterControl subscribes to. Per §3.6b these get NO
code `AddListener` — Bob owns the onClick, and a code listener on top would double-fire every press.

**Emitters wired (2026-07-26) — every class whose host exists:** ground direct combat + indirect/bombardment
(both sides) · HQ unit-lost · ambush (both directions) · objective captured/lost · unit hardened · weather ·
first contact + intel rungs. Direct calls from `MovementController` for what a controller does to its own
units; `PrinterDispatch.Attach()/Detach()` (lifetime owned by PrinterControl) for the broadcast triggers,
weather and spotting.
⚠ **Unit hardened is detected by comparing `ExperienceLevel` across the attack in the CALLER** — `CombatUnit`
touches EventManager nowhere and must stay that way, since it runs in headless EditorTests.
⚠ **Intel rungs L2–L5 file in VERBOSE ONLY** — with enemy reports on the unit panel (§4.3), a dispatch reciting
posture and equipment tells the player what they can already click for, so it fails all three gates. First
contact always files.
⚠ **First contact is suppressed at turn 0** — `SetupBattleManagerData` runs a full `RecomputeAllSpotting` before
the first icon draw, which would open the feed with one "new contact" per already-visible enemy.
⚠ **Weather text is deliberately truncated** — §24.8.6's "Air operations suspended. Visibility poor." are claims
about mechanics that do not exist yet (weather is single-state Clear in v1), so only the change is printed.
Safe by construction: `ApplyLevel` mutates `unit.SpottedLevel` while the AI writes to `AIPerceptionState`, so
the printer cannot narrate AI beliefs.
**Emitters NOT wired, blocked on hosts that do not exist:** air operations (M13/AOB — the class Bob expects to
carry the feed), logistics (§15.4a supply pass), decorations/promotions/leader-killed (leader L1/L2),
opportunity + AD fire (the §11.8 transit walk).
**Not built:** P5 loss ledger (`BattleManager.RecordPlayerUnitLoss`/`RecordAIUnitDestroyed` are still empty
stubs; needs a `SAVE_VERSION` bump), P6 loss report, P8b tests.

### 3.7 Services

**AppService** (static): Exception handling, UI message ring buffer (100), directory paths, test handler. **HexDetectionService:** Mouse → hex via raycast, fires `OnHexSelected` + `OnHexRightClicked` (positioned right-clicks); `ClearSelectionAndNotify` public per §5.10.5. **CameraService:** Movement/zoom, plus `CenterOnPosition(hex)` (called on unit select and after a move). Scroll bounds are a two-part gate: `InputService_BattleMap.ApplyBoundaryConstraints` damps input PER AXIS and only outward (`ConstrainScrollAxis` — comparing headroom to SoftStopDistance in world units, no landing-position prediction), and `ClampCameraToBounds` clamps the transform after each scroll step so out-of-bounds is unreachable. ⚠ Fixed 2026-07-27: one isotropic multiplier off the NEAREST edge used to zero the whole vector at a boundary, stranding the camera until a `CenterOnPosition` teleport. ⚠ `CenterOnPosition` is deliberately NOT clamped, and `SetScrollBounds` has ZERO CALLERS — bounds are a hand-set Inspector value, never derived from the loaded map, so they are calibrated for 32x21 Khost only. **InputService_BattleMap:** Battle input. **NameGenService:** Random names by nationality.

**SpottingService** (static, ~406 lines): All spotting, fog-of-war, and ambush detection logic for the battle scene. **Dual-domain (§12.3):** `SpottingRangeAgainst(spotter, target)` picks the spotter's AIR vs GROUND range by the TARGET's domain (`IsAirborneSpottingTarget`; attack helos = GROUND targets via NOE). Player side: `RecomputeAllSpotting()` full sweep at turn start + per-hex incremental checks from `MovementController`. AI side (ADDITIVE region, player paths untouched): `RecomputeAIPerception` / `StepAIPerceptionDecay` write the `AIPerceptionState` belief store instead of unit SpottedLevels. ⚠ Any §12 spotting change must update BOTH sides + the `AIPerceptionState.StepDecay` mirror. Defines `AirAmbushResult` enum (NoThreat / Detected / Ambushed).

**TerritoryService** (static, ~146 lines): Movement-driven tile control (§6.13 + §17.5) — transit/occupation/ZoC-sweep ownership flips + end-on-objective captures, returned as `TerritoryChangeResult` (caller applies prestige/objective accounting + redraw). Fixed-wing transit never flips. HCL decay/recovery (§6.13.5, the Upkeep half) lands with the supply pass.

### 3.8 File Loaders

**MapLoader:** .map JSON → HexMap with neighbors. Validates that `saveVersion` matches the current map format (hard reject), that it is > 0, and that `checksum` is non-empty. ⚠ **It does NOT compare the checksum, by design since 2026-07-28 — see §7.1.** **OOBFileLoader:** 3-pass load — (1) units, (2) air attachments, (3) leaders. Auto-detects legacy format.

### 3.9 Utilities

**HexMapUtil:** Pathfinding, neighbor queries. **NationalityUtils:** Display names, flags, rank symbols.
(`MapChecksumUtility` was DELETED 2026-07-28 — see §7.1 for why the map `checksum` field survives it.)

---

## 4. Data Flow

### 4.1 Scenario Loading

```
ScenarioManifest → MapLoader → HexMap → GameDataManager.CurrentHexMap
               → OOBFileLoader: Pass1(units) → Pass2(air attach) → Pass3(leaders)
               → HexGridRenderer.RefreshMap() → GameIconRenderer.DrawAllUnits()
               → EventManager.RaiseRedrawMapIcons()
```

### 4.2 Save/Load

```
Save: GameDataManager → SnapshotMapper.ToSnapshot() → GameStateSnapshot → JSON → file
Load: file → JSON → GameStateSnapshot → ClearAll() → ApplySnapshot() → RebuildTransientCaches()
```

### 4.3 Unit Selection

```
Click → HexDetectionService → GameDataManager.SelectedHex → ReactivePanelManager
  → resolve unit/leader → update terrain + unit panels
  (leader panel removed 2026-07-23 → future modal; SelectedLeader still resolved)
  Unit panel shows BOTH SIDES (2026-07-25 — reverses the 2026-07-24 friendly-only rule; RPM gates
  on `SpottedLevel >= Level1` for enemies so an unspotted unit never leaks through a hex click).
  Unit stays selected after a move (PG-style): MovementController keeps
  UnitSelected + HexDetectionService.SelectHex(newHex) makes selection follow the unit so panels
  track it.

PANEL MODEL (revised 2026-07-27, CONFIRMED IN PLAY same day). The three information panels — terrain, unit, printer CRT — are
  OPEN FROM SCENE START, empty, and never close. Visibility is no longer a behaviour: right-click
  deselect CLEARS terrain + unit content (`Prefab_TerrainPanel.Clear()` / `Prefab_UnitPanel.Clear()`)
  and the HUD holds one stable layout for the whole battle. A hex with no FRIENDLY unit leaves the
  unit panel blank. The printer is the exception to the CLEARING half — a dispatch log is not about
  the selected hex, so right-click leaves its history alone. RPM owns terrain + unit and brings them
  up empty in `Initialize()`; the printer opens itself the same way (§3.6c).
  (Superseded: the 2026-07-25 open-on-first-hex-click latch, and before it hide-on-deselect. The
  lazy-singleton hazard that came with starting closed — `FindAnyObjectByType` does not return
  inactive objects, so an early `.Instance` touch spawned a stray GameObject — is gone with it.)
```

---

## 5. Enumerations Reference

**UnitClassification (45+):** TANK, MECH, MOT, AB, MAB, MAR, RECON, CAV, AT, ART, SPA, ROC, BM, SAM, SPSAM, AAA, SPAAA, ENG, HELO, FGT, ATT, AWACS, BMB, RECONA, WW, TRN, HQ, DEPOT, AIRB...

**UnitRole (11):** GroundCombat, GroundCombatIndirect, GroundCombatStatic, AirDefenseArea, AirSuperiority, AirMultirole, AirGroundAttack, AirStrategicAttack, AirRecon, AirborneEarlyWarning.

**Nationality (15):** USSR, USA, FRG, UK, FRA, BE, DE, NE, MJ, IR, IQ, SAUD, KW, China, GENERIC.

**TerrainType (9):** Water, Clear, Forest, Rough, Marsh, Mountains, MinorCity, MajorCity, Impassable.

**DeploymentPosition (6):** Fortified(0), Entrenched(1), HastyDefense(2), Deployed(3), Mobile(4), Embarked(5).

**ExperienceLevel (6):** Raw(0.8x), Green(0.9x), Trained(1.0x), Experienced(1.1x), Veteran(1.2x), Elite(1.3x).

**WeatherCondition (6):** Clear, Overcast, Storm, Snow, Blizzard, Sandstorm.

**BattlePhase (10, ratified §3.2):** NotStarted, Deployment, PlayerRefresh, PlayerTurn, PlayerUpkeep, AI_Refresh, AI_Turn, AI_Upkeep, TurnBoundary, BattleComplete. (Refresh/Upkeep loop contents = M13.)

**BattleResult (8):** DecisiveVictory, MajorVictory, MinorVictory, Draw, MinorDefeat, MajorDefeat, DecisiveDefeat, Ongoing.

**SkillBranches (13):** Foundation: LeadershipFoundation, PoliticallyConnectedFoundation. Doctrine (pick ONE): Armored, Infantry, Artillery, AirDefense, Airborne, AirMobile, Intelligence. Specialization (pick ONE, requires TopGrade): CombinedArms, SignalIntelligence, Engineering, SpecialForces.

---

## 6. Key Constants

**Combat:** MAX_HP=40 (mobile units), BASE_MAX_HP=60 (facilities — HP-0 = destroyed, decoupled from OperationalCapacity), ZOC_RANGE=1, GAT_INTERDICT_THRESHOLD=6, STRATEGIC_OC_BONUS=20. Modifiers: Mobile(1.0x), Hasty(1.1x), Entrenched(1.2x), Fortified(1.3x). Tank gen scaling: Gen1(7/5)→Gen4(16/14).

**Supply:** MaxDaysSupplyUnit=5, MaxDaysSupplyAirbase=30 (depot-days model RETIRED). Generation = %-of-capacity per turn: Minimal 5% → Industrial 40%. Sorties: SORTIE_LAUNCH_COST=1, SORTIE_SHOT_COST=0.5, AIRBASE_LAUNCH_FLOOR=5, AIR_SUPPLY_LOAD=5.

**Persistence:** SAVE_VERSION=3 (next bump scheduled: AI2 adds AIPerceptionState to the snapshot).

**Prestige:** Weapon tier: Gen1(free)→Gen4(150). Unit type: TANK(55), IFV(40), APC(30), ART(100), ROC(175), SAM(145).

**Leadership:** Tier costs: T1(60)→T5(260). Promotions: Senior(100), Top(250).

**Map:** 256px-wide hex, pointy-top REGULAR (cell 2.56 × 2.956 world). Small: 32x21, Large: 32x42. Vertical spacing: width × √3/2 = 2.217.

---

## 7. Content Pipeline & Data Formats

### 7.1 Where content lives (Phase 1, 2026-07-27)

All shipped content is READ-ONLY and lives inside the build under `StreamingAssets`. `Documents/My Games/Hammer and Sickle/` holds ONLY player-written data — saves (`cmp/`) and `logs/`. **Nothing is ever copied between them**, which is what makes a Steam patch a file replacement rather than an install-and-merge problem.

```
Assets/StreamingAssets/
  Audio/{Ambient,Briefings,Music,SFX}      loaded via UnityWebRequestMultimedia (GameAudioManager)
  Scenarios/<scenario>/                    standalone — manifest · map · oob · aii · brf
  Campaigns/<campaign>/campaign.manifest   the mission graph (Phase 2, not yet built)
  Campaigns/<campaign>/<mission>/          manifest · map · oob · aii · brf
```

**A scenario is a SELF-CONTAINED FOLDER.** `ScenarioManifest.ContentRoot` (transient, `[JsonIgnore]`) is stamped with the manifest's own directory at load, and `GetMapFilePath()`/`GetOobFilePath()`/`GetAiiFilePath()`/`GetBriefingFilePath()` resolve against it. So patching one scenario touches one folder, and two scenarios can share a filename without colliding — which they do: the standalone Khost and the campaign's Khost are separate content with different briefings and OOBs.

⚠ **`ContentRoot` has ONE point of entry** — `ScenarioDialog_Scene0.LoadScenarioManifests`, the only place a manifest is deserialized. Any new code that builds a manifest must set it, or every content path silently returns empty; `MapLoader` and `OOBFileLoader` both name `ContentRoot` in their failure messages so this surfaces loudly rather than as a mysterious not-found.

⚠ **A scenario's FOLDER NAME IS ITS IDENTITY AND IS PERMANENT ONCE SHIPPED**, because saves reference the scenario by id. Mission ORDER is not encoded in folder names — it lives in `campaign.manifest`, which is exactly what lets a post-release reshuffle be a content patch. A `m01_`/`m02_` prefix is a filing convention recording authoring order and readability at 25–30 missions; **never renumber a folder after release**. Display names (`"Grand Campaign"`, `"Operation Molot"`) live inside the manifests and can change freely, including in a patch — folder/id is machine-facing (lowercase, underscores), display name is human-facing.

**Retired here:** the two parallel path families. `GetMapFilePath()` used to resolve to Documents/My Games and `GetMapFilePath_GDP()` to `Assets/Generated Data`, with `MapLoader` and `BattleManager` choosing on `IsCampaignScenario` — welding a gameplay concept to a storage one, so a campaign could not be standalone-tested and the two copies silently diverged (they had, by eight months). `AppService.GDP_*`, `ScenarioDataPath`, `ManifestsPath`/`MapPath`/`OobPath`/`AiiPath`/`BrfPath`, `OOBFileLoader.LoadStandaloneOob`/`LoadCampaignOob` are all GONE. ⚠ The GDP family could never have worked in a build at all — `Application.dataPath` is `<Game>_Data` in a player, where `Assets/Generated Data` does not exist.

⚠ **No `.aii` files exist yet** — the AI pass will author them (Bob, 2026-07-27). A missing AII must stay a clean no-op, never an error.

**Map checksums are RETIRED game-side (2026-07-28, ratified).** `MapChecksumUtility` is deleted. It had zero callers and had never validated anything, but read convincingly enough that it put a false "MapLoader checksum-validates" claim into this very document, which in turn shaped a planned phase that would have hard-failed every map on the day it shipped. **The `checksum` field STAYS in the `.map` header** and the scenario editor keeps computing it: removing it would mean a map-format version bump, an editor change and a re-export of every map, for no gain. Its remaining job is a CONTENT FINGERPRINT — it is how the 2026-07-28 name-form conversion was proved to have changed the representation and not the data. ⚠ **Do not "restore" validation.** The editor's hash input is the hex array in ITS key order, which does not match C# property-declaration order, so the two can never agree without the game permanently mirroring the editor's field layout — a standing breakage risk for a guarantee Steam's file verification already provides on shipped, read-only content.

### 7.2 Formats

**Manifest (.manifest):** JSON — scenarioId, displayName, description, thumbnailFilename, mapFilename, oobFilename, aiiFilename, briefingFilename, prestigePool, isCampaignScenario, mapTheme, difficultyLevel, maxTurns, maxCoreUnits.

**Map (.map):** JSON — header (name, config, version, checksum, timestamp) + hexes array (position, terrain, movementCost, infrastructure, objective, tileControl, labels, victoryValue, borders).

**OOB (.oob):** JSON — unit definitions (ID, name, pos, nationality, side, classification, role, weapon IDs, stats) + leader definitions (ID, name, grade, ability, skills, assignment).

**Briefing (.brf):** Plain text narrative.

---

## 8. Action Economy

| Action | Cost |
|--------|------|
| Combat | 1 CombatAction + 25% max MovementPoints + 1 supply (req >= 2) |
| Move | 1 MoveAction + per-hex MovementPoints + (cost x 0.2 supply) (req >= 1.5) |
| Deploy | 1 DeploymentAction + 50% max MovementPoints + 0.25 supply |
| Intel | 1 IntelAction + 15% max MovementPoints + 0.25 supply |
| Opportunity | 1 OpportunityAction + 0.5 supply (req >= 1.5) |

**Combat Strength:** `Final = BaseStats x Strength x Deployment x Efficiency x Experience x ICM`

| Modifier | Values |
|----------|--------|
| Strength (HP%) | Full(>=80%): 1.15x, Depleted(50-79%): 0.75x, Low(<50%): 0.4x |
| Deployment | Fortified: 1.3x, Entrenched: 1.2x, HastyDefense: 1.1x, Others: 1.0x |
| Efficiency | Full: 1.0x, Combat: 0.9x, Normal: 0.8x, Degraded: 0.7x, Static: 0.5x |
| Experience | Elite: 1.3x → Raw: 0.8x |
| ICM | 0.5–2.0 (default 1.0) |

---

## 9. RegimentProfile and WeaponProfile

**RegimentProfile:** Links CombatUnit to weapons. Holds 3 WeaponType enums (Deployed/Mobile/Embarked). ProfileType: Default, DEP, DEP_MOB, DEP_MOB_EMB_HELO/AIR/NAVAL. TotalIntelStats (transient). Access: `unit.GetActiveWeaponProfile()`, `unit.GetDeployedProfile()`, etc.

**WeaponProfile (177 profiles, ALL built via `FromProfileDef` — the Archetype+Delta+Trait model, §2.6):** the resolver produces the 17-stat line (`ProfileStat`: HA/HD/SA/SD/GAT/GAD/DF/MAN/TS/SUR/GA/OL/STL/PR/IR/SR/MMP), a stored `ICM` (product of quality-trait multipliers, default 1.0, set only via `SetICM`), and a `WeaponCapability` set (replaces the old bool flags). Strike riders are STORED but mostly INERT until their combat-engine consumers land (M13): `GaVsHard/Soft/Base` (read via `EffectiveGroundAttack(targetClass, isBase)`), `ParkedHitBonus`, `OcSuppressionBonus`, `IgnoreAirDefense`, `LoiterReattack`. Plus Ranges (Primary/Indirect/Spotting — NOTE profile SR is UI-only; live spotting uses the GameData classification tables §12.3), Upgrades (PrestigeCost, TurnAvailable), Intel (IntelReportStats), Icons. The old AllWeather/NBC/NVG ratings and Silhouette fields are DELETED — do not reference them.

---

## 10. Intel System

Generated on-the-fly: `WeaponProfile.IntelReportStats` → `RegimentProfile.BuildIntelStats()` → `RegimentProfile.GetIntelReport()` → `CombatUnit.GetIntelReport(SpottedLevel)` (filters by level, scales by HP, applies error).

17 buckets: Personnel, TANK, IFV, APC, RCN, ART, ROC, SAM, AAA, AT, HEL, AWACS, TRN, FGT, ATT, BMB, RCNA.

**Display (§12.5.3, REVISED 2026-07-25 — DesignDoc amendment owed):** a selected unit of EITHER side shows in the **Unit Panel**, friendly as the Full ownership view and enemy filtered to its `SpottedLevel`. Both sides share one layout (`Prefab_UnitPanel.BuildFriendlyLines` / `BuildEnemyLines`); the enemy view differs only by omitting lines whose rung has not been earned — never by placeholders. Supply and commanding officer are friendly-only at every rung (§24.5a.5/.6), and hit points appear on neither (§24.3.2.5 keeps strength in the icon's HP box alone). This SUPERSEDES the 2026-07-24 rule that made the panel friendly-only and routed enemy intel to the printer; the printer keeps every OTHER dispatch class, and `PrinterMessage.CreateUnitReport` survives only as the §24.8.6 intel-dispatch body, no longer a selection readout. `IntelReport.GetEquipmentEntries()` remains the shared formatter — the non-zero buckets as atomic display entries ("120 tanks", ground then air, NBSP-joined) — feeding the friendly view, the enemy view, and the printer, so none of the three can drift.

### 10.1 The six-rung ladder (§12, ratified AND coded 2026-07-24)

**Rungs.** L0 unspotted (no icon) · L1 contact, icon only (deploy icon `UnknownIcon`, HP box dash) · L2 name · L3 deployment · L4 six coarse buckets @16% err + HP as a strength band · L5 +exp/eff @8% err + exact HP. **`Full`** = friendly ownership (all 17 buckets, 0 err) — NOT a rung: unreachable by spotting, never stored in `SpottedLevel`, never decayed. Access it via `CombatUnit.GetFullIntelReport()`; `GetIntelReport(level)` is the enemy path and cannot produce it.

**Progression is SOURCE-CEILING, not "+1 per hit"** — level = the highest ceiling any source earned; repeated looks never accumulate. `SpottingService` helpers: `PassiveContactCeiling` (range→L1, adjacent→L2, adjacent RECON→L3), `RaiseToCeiling` (set-to sources), `RaiseByOneRung` (+1 sources), `SustainedFloor`. Combat → `ApplyDirectCombatContact` sets both participants L4 (called from `GroundCombatAction`). Ground IntelAction → `ApplyGroundIntelAction`, +1 rung on every ADJACENT enemy, ceiling L5 — the only route to L5, wired through `EventManager.OnIntelActionRequested` → `MovementController`.

**Decay is graduated:** −1 rung per Refresh above a per-turn re-derived sustained floor, HELD entirely while the unit is adjacent to a friendly, and still able to reach L0. ⚠ Consequence worth remembering: a contact held only by a DISTANT sensor erodes down to that sensor's floor rather than freezing where it was.

**Icon is an intel channel (§24.3.2).** `Prefab_CombatUnitIcon.SetIntelDisplay(hpMode, deploymentKnown)`; `GameIconRenderer.ApplyIntelDisplay` stamps it at spawn and re-gates on every rung change in both directions. The HP/deployment event handlers cache the TRUE value and re-render through the gate, so a fogged enemy taking damage or digging in never leaks.

**Intel error is deterministic** — `CombatUnit.IntelErrorNoise`, a stable FNV-1a over (UnitID, HP, bucket). Not `string.GetHashCode` (not reproducible across runtimes → would break save round-trips) and not `UnityEngine.Random` (jittered per click and leaked the truth to repeated sampling). SpottedLevel is deliberately NOT in the seed: the rung supplies magnitude, the seed supplies direction, so better intel tightens toward the truth instead of swinging past it.

⚠ **Symmetry:** the AI plays by these rules. Any §12 change lands in BOTH `SpottingService` sweeps (player + `RecomputeAIPerception`) AND `AIPerceptionState` (`RecordSpot` takes a ceiling; `StepDecay` takes a **sustained-floor map**, not an in-range set, plus an adjacency set). The floor map is the subtle part: "still in sensor range" must NOT freeze a contact at whatever rung it reached — being watched from six hexes away sustains L1, not the L4 an earlier engagement paid for. A boolean in-range test silently gives the AI a better memory than the player, which under Option-B honest spotting is a cheat rather than an approximation.

**Not yet built:** the map-wide HQ SIGINT sweep (§12.7 — needs `SIGINT_Rating` on CombatUnit, M15) and RB tier application (§11.11.11 — the caller is M13/AOB). `Concealed Operations Base` still caps at L3 unchanged; re-repurposing it is a deferred skills pass.

---

## 11. Extended Enumerations

**StrategicMobility:** Heavy, AirLift, NavalAssault, AirDrop, AirMobile, Aviation, Aircraft.
**SIGINT_Rating:** UnitLevel, HQLevel, SpecializedLevel — PARKED (currently unreferenced; reintroduced on CombatUnit in M15).
(`NVG_Rating` / `NBC_Rating` / `AllWeatherRating` / `UnitSilhouette` were DELETED in the trait migration — weather/night/NBC effects now live in `WeaponTraitCatalog` trait effects.)
**SpottedLevel:** 0–5 — the six-rung ladder (§10.1): Level0 unspotted · Level1 contact · Level2 +name · Level3 +deployment · Level4 +coarse buckets @ MAX error · Level5 +exp/eff @ MODERATE error. `Full` is NOT a member (friendly ownership view, §12.2.7). **MapIconType:** Airbase, Fort, UrbanSprawl. **BridgeType:** Regular, DamagedRegular, Pontoon.
**DefaultTileControl:** None, BE, DE, FR, MJ, NE, SV, UK, US, GE, CH, IR, IQ, SA, KW.
**TextColor:** Black, White, Gold, Red, Blue, Grey, Yellow, Green, Teal. **TextSize:** Small, Medium, Large. **FontWeight:** Light, Medium, Bold.
**SceneID:** MainMenu=0, Scenario_Khost=1, Campaign_Khost=2.
**SkillBranch IDs:** Leadership=1, Political=2, Armored=10, Infantry=11, Artillery=12, AirDefense=13, Airborne=14, AirMobile=15, Intelligence=16, CombinedArms=20, SignalIntel=21, Engineering=22, SpecForces=23.
**SkillTier:** None=0, Tier1–5. **XP thresholds:** Raw=0, Green=50, Trained=120, Experienced=220, Veteran=330, Elite=400.
**DifficultyLevel:** Colonel, MjGeneral, LtGeneral.

---

## 12. WeaponType Enum by Faction

**Soviet:** MBT: T55A, T62A, T64A/B, T72A/B, T80B/U/BV | IFV: BMP1/2/3, BMD2/3 | APC: MTLB, BTR70/80 | Recon: BRDM2, BRDM2AT | SPA: 2S1/2S3/2S5/2S19 | Rocket: BM21/27/30, SCUD | AAD: ZSU57/ZSU23, 2K12/2K22/9K31, S75/S125/S300 | Helo: MI8T/MI8AT, MI24D/V, MI28 | Jets: MIG21/23/25/27/29/31, SU27/47/25/25B/17/24, TU16/22/22M3, A50(AWACS) | Infantry: REG/AB/AM/MAR/SPEC/ENG

**Western:** MBT: M1/M60, Leopard1/2, Challenger1, AMX30 | IFV: M2, Warrior, Marder | APC: M113/Humvee/LVTP7, VAB | Recon: M3, FV105, Luchs, ERC90 | SPA: M109 | Rocket: MLRS | AAD: M163/Chaparral, Gepard, Roland/Crotale, Rapier | Helo: AH64/UH60, BO105 | Jets: F15/F4/F16/F14/F111, Tornado, Mirage2000/F1, A10/F117, E3(AWACS) | Infantry: REG/MAR/AB/AM

**Arab/MJ:** T55A/T62A(IQ), M60A3(IR), BMP1/MTLB/M113, MIG21/23(IQ), F4/F14(IR), REG/SPEC/CAV/RPG(MJ)

**Chinese:** Type59/80/95, Type86(IFV), Type82(SPA), PHZ89, J7/J8, Q5, H6, H9

**Generic:** BASE_LARGE(airbase), BASE_MEDIUM(HQ), BASE_SMALL(intel)

---

## 13. Cross-System Relationships

1. **Unit ↔ Leader**: Bidirectional string IDs. Always use `GameDataManager.AssignLeaderToUnit()`.
2. **Unit → WeaponProfile**: One-way via RegimentProfile → WeaponProfileDB lookup.
3. **Unit → IntelReport**: On-the-fly from RegimentProfile.TotalIntelStats.
4. **Airbase → Air Units**: Dual-list (AttachedUnitIDs serialized + AirUnitsAttached transient). RebuildTransientCaches() reconstructs.
5. **Map → Units**: Units store MapPos. Map doesn't track positions.

---

## 14. Quick Reference

| Need To... | Use This |
|------------|----------|
| Get unit/leader | `GameDataManager.Instance.GetCombatUnit(id)` / `.GetLeader(id)` |
| Get weapon profile | `GameDataManager.GetWeaponProfile(WeaponType)` |
| Create unit | `GameDataManager.CreateUnitFromTemplate(templateId, name)` |
| Assign leader | `GameDataManager.Instance.AssignLeaderToUnit(leaderId, unitId)` |
| Intel report | `unit.GetIntelReport(SpottedLevel)` |
| Save/Load | `await SaveLoad.SaveAsync(path)` / `.LoadAsync(path)` |
| Clear state | `GameDataManager.Instance.ClearAll()` |
| Rebuild links | `GameDataManager.Instance.RebuildTransientCaches()` |
| Log exception | `AppService.HandleException(className, methodName, ex)` |
| UI message | `AppService.CaptureUiMessage(message)` |

---

## 15. Important Tips — Hard-Won Lessons

### 15.1 Assembly Definition Files (.asmdef)

Three files: `Main.asmdef`, `EditorTests.asmdef`, `RuntimeTests.asmdef`. If you add a NuGet/Unity package and scripts can't see it, add the reference to `Main.asmdef` — Unity won't resolve it automatically. Test assemblies reference Main; if tests can't find game classes, check their .asmdef references.

### 15.2 JSON Serialization — The Rules That Break Saves

**System.Text.Json exclusively.** Options in `SaveLoad.cs`: `PropertyNamingPolicy = null`, `IncludeFields = false`, `ReferenceHandler.Preserve`.

**Rules:**
1. Every serialized property needs `[JsonPropertyName("camelCase")]` — without it, silently skipped on load.
2. Constructor param names must exactly match `[JsonPropertyName]` values (case-sensitive) — silent failure.
3. `[JsonConstructor]` mandatory with multiple constructors — wrong constructor = garbage data.
4. `[JsonIgnore]` on every computed/transient property — bloated saves or deserialization errors.
5. `[JsonInclude]` required for `private set` properties — defaults without it.
6. Public fields NOT serialized — convert to properties.
7. `[Serializable]`/`[DataContract]`/`[DataMember]` are ignored — false confidence.

**Two patterns:** Small (<=6 props): explicit constructor with matching params. Large (7+): parameterless constructor, `[JsonInclude]` on private setters, transient state in `Initialize()`.

### 15.3 Snapshot Load Sequence — Order Matters

Complete state replacement. No partial updates.

```
1. SaveLoad.LoadAsync(path)
2. Deserialize → GameStateSnapshot
3. GameDataManager.ClearAll()       ← MUST come first, or duplicate registrations
4. SnapshotMapper.ApplySnapshot()   ← recreate all entities
5. RebuildTransientCaches()         ← MUST follow, or broken links/null transients
6. Validate
```

Snapshot contains: campaign data, scenario data, map (nullable), all units by ID, all leaders by ID, save version.

### 15.4 Entity Relationships — Use GameDataManager

Never set `unit.LeaderID` or `leader.UnitID` directly. Always `GameDataManager.Instance.AssignLeaderToUnit()` / `.UnassignLeader()`. Same for airbase attachments: use `AddAirUnit`/`RemoveAirUnit`.

### 15.5 Event Subscription Hygiene

Always unsubscribe in `OnDestroy`. Always null-check `EventManager.Instance` first — singleton destruction order is unpredictable during scene transitions.

### 15.6 GameDataManager.IsReady

Scene controllers validate this at startup and call `AppService.UnityQuit_DataUnsafe()` on failure. New scene controllers must do the same.
