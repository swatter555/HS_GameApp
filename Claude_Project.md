# Hammer and Sickle — AI Agent Codebase Context

Unity Version = 6000.2.6f2
URP Version = 17.2.0

_Last reconciled against the codebase: 2026-08-19 — **THE THEME-ART PASS** (Bob authored full EU + CH art sets; compile + suites + Khost regression green): map icons are per-theme for ALL THREE themes — `HexGridRenderer.CreateMapIcon` is a 9-arm (theme × icon type) switch, the sprite resolves BEFORE Instantiate with a debounced warn-and-skip for both a missing arm AND an atlas-packing miss; SpriteManager +6 constants (EU_/CH_ × Sprawl/Fort/Airbase); `TextureArrayBuilder`'s China prefix fixed `CN`→`CH` (matches the codebase-wide China prefix and the authored `CH_*.png` tiles); all three `TerrainArray_<Theme>.asset` are baked (~289 MB each, ALL gitignored — §3.5c rebuild-after-clone now covers three arrays). ⚠ Two truths pinned in comments: `BASE_AIRBASE.IconProfile`'s `ME_Airbase` is UNREAD at render (`GameIconRenderer.GetSpriteNameForUnit` short-circuits AIRB to the theme-agnostic `AirbaseStack_N` badges), and fort facility units DO NOT EXIST (forts are tile infrastructure; themed fort art is the map icon). City icons/nameplates/terrain portraits were already theme-wired and needed zero code. EU/CH in-play verification is ⚑-gated on the first non-ME scenario export._
_Prior: 2026-08-17 — **THE PRESTIGE/VICTORY PASS, Stages 1–2** (Bob-ratified via the editor-side change request V1–V16 + its two reply rounds; plan `todo_prestige.md`; GATES 1–2 both GREEN incl. Khost played): NEW `VictoryLedger` (derived, recomputed once per turn + at battle start, NEVER accumulated) + `PrestigeWallet` (`AddPrestige` finally credits the spendable balance; `SpendPrestige` → bool atomic) + `EventManager.OnPrestigeChanged`; `ScenarioManifest` → parameterless ctor (the 16-param `[JsonConstructor]` is DELETED — it silently defaulted any property lacking a ctor param) + 8 scoring/economy keys (§7.2; two-state thresholds, `requiredResult` by NAME — **`BattleResult` is rename-frozen**; Draw = the defensive scenario, C5); **capture stickiness now DERIVES from `HexTile.IsStronghold`** (cities/fort/airbase/port — `isObjective` is GAMEPLAY-DEAD, a UI marker with exactly two readers until the V15 rip); `TerritoryService` renamed to `StrongholdCapture`/`CapturedStrongholds`; `RegionGraph` → `StrongholdCount` + ungated `VictoryValue`; **the all-objectives instant win is RETIRED** (interim: NO early end exists — battles run to the turn limit until the Stage 4 share-based rule); SV control-flag arm added; map icons warn-and-skip on missing theme art. **THE PASS CLOSED THE SAME DAY — Stages 3–5 (all gates green incl. play):** §18.2 per-turn INCOME is live in the player Upkeep (`BattleManager.ComputeIncome`: stipend + rate × held value + high-water progress bonus — the capture award and the objective counters are DELETED; `ReportStrongholdTaken/Lost` print ledger roll-ups instead); `CompleteBattle` GRADES for real (V9 mirrored ladder around the persisted `StartingPlayerShare`, C1 no-scoring guard, arithmetic logged) with **the C6 MISSION-OBJECTIVE GATE applied last** — manifest `missionObjectives` [{x,y,label?}] are CLEARED-then-STAMPED onto `hex.IsObjective` by `MapLoader.ApplyMissionObjectiveStamp` at scenario load (authored flag value DEAD; out-of-bounds objective REFUSES the load, non-stronghold warns), `HexMapUtil.AllMissionObjectivesHeld` is the gate, and an unmet gate caps the grade one rung below `requiredResult`; V10: share-based nothing-further-to-gain auto-end at TurnBoundary + voluntary early finish via `BattleManager.OnEndScenarioButton` (⚠ NO BUTTON WIRED YET — Bob, Inspector; gate = scoring declared ∧ share ≥ minor ∧ objectives held; bonus = unusedTurns × steady income × multiplier, computed live); **SAVE_VERSION 7** (wallet wiring + `startingPlayerShare` + `highWaterVictoryValue` + the 8 manifest knob mirrors into `ScenarioData` via `CaptureScenarioState`/`RestoreScenarioState` — prestige-pass slice ONLY, full battle-state sync still belongs to the unbuilt save-wiring feature; counter fields dropped; no migration arm). HS_DesignDoc amended in step (§4.7.2, §6.13.8, §17 rework + NEW §17.8/§17.9, §18.2 income model). Both khost manifests carry placeholder scoring + the 12 legacy objective hexes — Khost runs fully scored (1550 value / 36 hexes) on placeholder numbers._
_Prior: 2026-08-13 — **THE CENSUS FIX PASS** (doctrine v2, Bob-ratified; plan `todo_census.md`, findings `UnitDB_CensusAudit_2026-08-13.md`): every carrier census stripped to own-platform-count (rule 2), lift censuses emptied (rule 4 — Mi-8T, An-12, UH-60), organic tank battalions moved onto the mech BASES (SV +40 T-62A on INF_REG_SV, US/GE/UK +28, NL/BE/DK +32/28/24, IQ/IR +31, CH +40, FR +40 AMX-30), US/GE/UK bases re-cut to battle-group scale (§1a amended — one counter = one maneuver formation; France = division-as-counter, 80 tanks kept), the Iraqi 209-tank copy-paste bug deleted, UK token bugs fixed (recon `SPSAM_RAPIER_UK`→`RCN_FV105_UK`; `MANPAD_RAPIER`/UK Stinger→new census-only `MANPAD_JAVELIN`; MANPAD_RAPIER now census-unreferenced), `INF_REG_NL/BE/DK`→**`INF_MECH_NL/BE/DK`** (renamed while content-free; scheme: INF_LEG_*/INF_MECH_*, shipped names grandfathered), and `CensusIntegrityTests` grown 2→6 (carrier-clean · lift-empty · truck-empty · classification coherence; the name-based CensusExempt allow-list REPLACED by rules). Zero gameplay-behaviour change. See §9 vocabulary, §10.7.9 in the design doc._
_Prior: 2026-08-12 — **THE MAP-STANDARD PASS**: map size is now per-scenario and arbitrary, read from the `.map` header (G1, three sites incl. the save WRITER); `MapConfig`'s geometric role deleted outright (G3); truncation throws in both populate loops (G6); camera scroll bounds derived from the loaded map (G5); docs de-two-sized (G7). See §3.4 HexMap, §3.7 CameraService and the MapLoader entry. Prior: 2026-08-11. **D2 transit air defence** (play-confirmed): `ResolveTransitFire` replaces the `Random.Range(0,2)` coin flip, ranged-fire eligibility ruled to be the CLASSIFICATION (§11.8.2a — SAM/SPSAM/AAA/SPAAA, never GAT), §11.8.11 overhead GAD fire built, and the §3.5b ambush-against-a-flight text corrected to the 2026-08-10 ruling it had fallen behind. **D3 over-water grace**: helicopters may now rest on water, `EndedTurnOverWater` persists, and **`SAVE_VERSION` is 6**. Earlier the same day: post-hoc spotting §12.4.4a and §6.9.9 eligibility. See §3.5b and §3.7; P3 movement-medium work and P1+P2 are in §3.2b._

**Large files (>1000 lines):** WeaponProfileDB.cs (6,677), CombatUnitDB.cs (5,224), CombatUnit.cs (2,469), GameData.cs (1,939), GameAudioManager.cs (1,627 — SFX path removed 2026-08-03), InputService_BattleMap.cs (1,431), SpriteManager.cs (1,420), BattleManager.cs (1,171), HexGridRenderer.cs (1,096), LeaderSkillCatalog.cs (1,096), GameIconRenderer.cs (1,084). Read these in chunks.

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
├── Audio/               AudioFogPolicy, WeaponSoundFamily (policy — pure, headless-safe) +
│                        AudioCatalog, SfxPlayer, GameAudio (the SFX system). §3.7b
├── Models/
│   ├── AI/              BoardAnalysis, MobilityMap, RegionGraph, ChokepointAnalysis, AvenueAnalysis,
│   │                    AmbushSiteCatalog, AIPerceptionState, Pmf, CombatOracle
│   ├── Combat/          CombatEngine, CombatResolver, CombatMath, CombatEnums, ICombatRandom, HexArc,
│   │                    StandCheck, SurrenderCheck, DegradationCheck, RetreatResolver,
│   │                    GroundCombatAction, IndirectCombatAction, AirCombatEngine, AirStandCheck,
│   │                    AirAmbushCheck, HeloTransitStandCheck, ReconMissionEngine,
│   │                    AOBMissionResolver, AOBStatus
│   ├── CombatUnit/      CombatUnit.cs, EquipmentBays.cs (was RegimentProfile.cs, renamed P1 2026-08-08),
│   │                    WeaponProfile.cs, CombatUnitDB.cs, WeaponProfileDB.cs
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
│                        InputService_BattleMap.cs, MovementModeService.cs, NameGenService.cs,
│                        SpottingService.cs, TerritoryService.cs
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
Assets/Audio/SFX/         Sound EFFECTS as imported project assets (NOT StreamingAssets — see §7.1a);
                          import settings enforced by Assets/Editor/Audio/SfxImportSettings.cs
Assets/StreamingAssets/   STREAMED shipped content (§7.1): Audio/ (ambient, briefings, music — NOT SFX),
                          Scenarios/<scenario>/, Campaigns/<campaign>/<mission>/
                          (`Assets/Generated Data/` DELETED 2026-07-28 — it was the old second content
                          root, unreachable in a build; do not recreate it, see P1 in todo.md)
Assets/Tests/             EditorTests/ (48 NUnit files: combat/AI/spotting/movement/leader/weapon-profile/
                          audio/movement-medium/deployment suites + TestFixture.cs base + CombatTestDice);
                          RuntimeTests/ currently unused
Assets/Tools/             (empty — BinaryToJsonConverter deleted 2026-06-15)
```

### Key Namespaces

`HammerAndSickle.Controllers`, `.Services`, `.Models`, `.Persistence`, `.Core`, `.SceneManagement` (dialogs), `.SceneManagement.Controllers` (scene controllers)

### File System (Runtime)

`Documents/My Games/Hammer and Sickle/` holds ONLY player-written data: `cmp/` (.cmp saves), `logs/`. `AppService` creates exactly these two (`MainAppPath`, `LogsPath`) and nothing else.

⚠ It no longer holds `scenario/`, `map/` or `oob/` — shipped content moved to StreamingAssets in Phase 1 (§7.1), and the leftover `scenario data` folder was DELETED from Bob's machine 2026-07-28. Documents is now saves and logs only, which is the end state P1 was after: one source of truth per artifact, with no second content root to diverge from.

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

**Counts (2026-08-13):** 184 registered weapon profiles, 186 unit templates. Censuses across all of them conform to doctrine v2 (§9 vocabulary; design doc §10.7.9), guarded by the six-test `CensusIntegrityTests`.

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
**Weapons:** EquipmentBays (three purchasable bays) → Deployed/Mobile/Embarked WeaponType slots → WeaponProfileDB lookup; naval sealift is a transient state (`IsNavalEmbarked` + shared TRN_NAVAL), never a bay.
**Facilities:** IsBase, FacilityType (HQ, Airbase, SupplyDepot, Fort), depot size, generation rate, projection range.
**Air attachment:** Airbases maintain attached air unit lists. **Actions:** 5 types (Move, Combat, Deploy, Opportunity, Intel).

### 3.2b THE PROFILE-SLOT RULE — three EQUIPMENT BAYS, not three loadouts (RATIFIED 2026-08-04)

> **⚠ SUPERSEDED IN MECHANISM 2026-08-08 — P1+P2 of the profile-slot rebuild (`todo_profiles.md`,
> the current authority).** The RULE below stands; its MECHANISM changed: `RegimentProfile` is renamed
> **`EquipmentBays`**, and `RegimentProfileType` + `isMountable`/`isEmbarkable` + `EmbarkmentState`
> are **DELETED** (all had zero live readers — capacity was already fiction). Which bays a regiment
> has is now **DERIVED**: Mobile bay open ⟺ deployed medium is `Foot`; Embarked kinds from identity
> (`GameData.IsInfantryFamily`, AB/MAB/SPECF) + equipment tags (`AirDroppable`/`HeloTransportable` —
> all towed SAM/AAA and light tubes carry both, census A). Naval is a transient state
> (`IsNavalEmbarked` + shared `TRN_NAVAL`, never a bay) — P2 made it REAL: universal port embark
> (§9.4.7, organic lift wins), port debark for all + beachhead for MAR/MMAR (§9.10.6.1 identity
> doctrine), and `EmbarkmentChecks` gates by WHAT IS BOARDED (FW lift → active friendly airbase;
> helo → anywhere; naval → port) with ZERO classification cases. One address:
> `EquipmentBays.CanAccept`/`TrySetSlot`/`TryClearSlot`; audited by `EquipmentBaysTests`.
> Point 2 below ("flags declare capability") is RETIRED — nothing declares; physics + doctrine derive.

⚠ **The absence of this rule caused every defect in the 2026-08-04 movement/audio pass.** Write new code against it.

A regiment has three bays: **Deployed** (how it fights dismounted or emplaced — always populated), **Mobile** (its GROUND transport), **Embarked** (its AIR or NAVAL lift).

**1. AN EMPTY BAY IS NORMAL, NEVER A DEFECT.** Slots are Panzer-General-style **upgrade targets the player buys into**: a plain Spetsnaz regiment starts foot-only, the player later buys it an MT-LB for the Mobile bay, later still an Mi-8 for the Embarked bay. `mobileProfile: NONE` means *not purchased yet*.

**2. THE FLAGS DECLARE CAPABILITY; THE SLOTS DECLARE CONTENTS.** `isMountable` / `isEmbarkable` / `profileType` say which bays the unit **has**. The `WeaponType` in each slot says what is **in** it right now. Different questions — never conflate them. ⚠ `isEmbarkable: true` on essentially every ground unit is CORRECT, because all ground units are naval-transportable (§5.4.2); it is not a claim that an embarked profile exists. An audit that flags "isEmbarkable true but embarkedProfile NONE" is measuring the wrong thing — 35 templates were nearly "fixed" on that mistake.

**3. ⚠ RUNTIME BEHAVIOUR KEYS ON CONTENTS, NEVER ON FLAGS.** Ask `GetMobileProfile() != null`, not `IsMountable`. This is precisely what makes the upgrade model work with no special cases: a Spetsnaz with an empty Mobile bay skips Deployed→Embarked, and the day the player buys it an MT-LB the *same* code stops at Mobile instead. `TryDeployUP` was generalised to this rule on 2026-08-04, replacing a hardcoded list of classifications plus one literal `WeaponType`.

**4. ⚠ THE ONE HARD INVARIANT: a profile whose `TransportCategory != None` may occupy ONLY the Embarked bay.** In the Mobile bay the unit rides it as its GROUND posture — paying terrain costs and halting for zones of control while airborne. That was the Spetsnaz (GRU) defect: `HEL_MI8T_SV` sat in Mobile with Embarked empty, because `RegimentProfileType` had no `DEP_EMB_*` shape to express "foot infantry whose only transport is airborne". Both `DEP_EMB_HELO`/`DEP_EMB_AIR` and a guard now exist — `RegimentProfile` warns at init, and `MovementMediumTests` fails over every template.

**Units with NO Mobile bay** are the ones where the unit IS its vehicle — tanks, SP guns, SPAAA, SPSAM: `isMountable: false`, and a dug-in tank still moves and sounds tracked.

⚠ **`CombatUnitDB` is the source of truth; a `.oob` is a SNAPSHOT of it.** The scenario editor builds OOBs from these templates, so a template defect gets frozen into shipped content and fixing the template alone does not fix a scenario already exported. Fix the template, then re-export. (The Spetsnaz bug survived a template fix for exactly this reason — `OOBFileLoader` reads every slot straight from the JSON and never consults the DB.)

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

**HexMap:** `Dictionary<Position2D, HexTile>`. Pointy-top, odd-r. Neighbor building, pathfinding, 3-phase validation. **MAP SIZE IS PER-SCENARIO AND ARBITRARY (min 10x10)** — it comes from the `.map` header via `JsonMapHeader.ResolveMapDimensions()`, and `HexMap(string, int, int)` is the ONLY constructor that sizes a map.

⚠ **THERE ARE NO BLESSED MAP SIZES, AND RE-ADDING ONE IS THE MISTAKE TO AVOID (G3, 2026-08-12).** The project once planned exactly two sizes; the premise was dropped and its skeleton was not, so `HexMap(string, MapConfig)` kept mapping `Small => 32x21` — which meant any map that was not one of those two loaded **silently truncated**: `SetHexAt` refused every hex past column 31 with a UI message and a `false` return, `MapLoader` counted those into a `failCount` it then discarded, and the map loaded, played, and was wrong. DELETED: the `MapConfig` constructor, `HexMap.Initialize()`, `HexMap.GetMapDimensions(MapConfig)`, and `GameData.SmallHexWidth/Height`+`LargeHexWidth/Height`. There is no longer any path from a configuration ENUM to a grid SIZE. `MapConfig` survives only as a vestigial `.map` header tag (kept for the same reason as `checksum` — removing it costs a format bump, an editor change and a re-export for zero gain); the legacy 32x21/32x42 table lives ONLY inside `ResolveMapDimensions` as a compatibility shim for pre-2026-08 files.

⚠ **Odd rows: two correct numbers at two layers.** The `.map` FILE carries the full rectangle with the odd-row overhang as Impassable filler (32x21 = **672** hexes); interaction treats odd rows as one column short (`HexGridSystem.IsInBounds`, so **662** playable). Do not "fix" either to match the other — an audit that flags 672 ≠ 662 is measuring across layers. Making the file ragged too is an open decision needing a coordinated editor change plus a re-export of every map.

**HexTile:** Position, TerrainType (9), MovementCost, TileControl (Red/Blue/Grey/None), VictoryValue (⚠ ANY hex may carry value — an economic weight, no flag gates it), HexControlLevel, IsDeploymentZone, IsBeachhead. Infrastructure: IsRail, IsRoad, IsFort, IsAirbase, IsPort (Fort/Airbase/Port three-way mutually exclusive). **`IsStronghold` (derived, `[JsonIgnore]`, 2026-08-17): cities ∥ fort ∥ airbase ∥ port — the hexes that must be physically ENDED ON to change hands (§6.13.8).** ⚠ `IsObjective` is GAMEPLAY-DEAD *as authored* — today a UI marker (flag sprite + terrain-panel line). **REDEFINED by the C6 ruling (2026-08-17 late, builds in prestige-pass Stage 4): it becomes a load-time PROJECTION of `manifest.missionObjectives`** — MapLoader clears every authored value then stamps the manifest list, gameplay/UI/saves read the stamped RUNTIME value, and the V15 "rip" is cancelled (the authored value is what dies). Until Stage 4 lands, the flag is inert. Borders: River/Bridge/DestroyedBridge/PontoonBridge per edge via JSONFeatureBorders. IDisposable.

**Terrain costs:** Water(1), Clear(1), Forest(2), Rough(3), Marsh(4), Mountains(5), Cities(1), Impassable(0). **Defense:** Forest(+1), Rough(+2), Marsh(+3), Mountains(+4), MinorCity(+1), MajorCity(+3).

### 3.5 Rendering

**HexGridSystem** (singleton, ~250 lines): Authoritative coordinate math for the pointy-top, odd-r grid with top-left origin (row 0 = highest world Y). Owns hex geometry constants — HEX_WIDTH=2.56, HALF_HEX_WIDTH=1.28, VERTICAL_SPACING=2.217 (width×√3/2), HEX_HEIGHT=2.956 (width×2/√3 — a REGULAR pointy-top hex, taller than wide; hex art in a square canvas is ~13.5% too short and gets fit-to-cell-stretched at stamp time). Sprites are 256px wide at 100 PPU. Provides grid-to-world conversion and direction offset tables (NE, E, SE, SW, W, NW for even/odd rows). All renderers and movement code delegate position math here.

⚠ **These four constants are the SINGLE GEOMETRY AUTHORITY and are `public` for exactly that reason.** `HexGridRenderer.FitToCellScale`, `BattleBackgroundFitter`, and the chunked renderer all read them rather than carrying literals — `HexChunkMeshBuilder` even derives its Voronoi corner offsets from `HALF_HEX_WIDTH`/`VERTICAL_SPACING` at static-init so the terrain mesh cannot drift from `HexToWorld` (§3.5c). Never re-spell a hex dimension anywhere else.

⚠ **THERE IS NO LONGER A SECOND SPELLING, AND ADDING ONE BACK IS THE MISTAKE TO AVOID.** `GameData` kept a pixel-scaled duplicate, `GetVerticalSpacing()` → `HexSize × √3/2` = **221.7 pixels** against this file's 2.217 **world units** — the same ratio at a different scale, i.e. a ready-made way to copy the wrong one into new code. It had zero callers, yet was still being maintained: the chunk-renderer commit corrected its formula from `0.75` to `√3/2`, fixing a number nothing read. Worse, its original `0.75` form is what put the wrong `VERTICAL_SPACING 1.92` into DesignDoc §4.2.1, where it survived until 2026-08-03 — the `MapChecksumUtility` failure mode exactly (dead code that reads authoritative, feeding a false claim into a doc, which then shapes a plan). **DELETED 2026-08-03** along with `IsPointyTop`, `PixelScaleX`, `PixelScaleY` and `SpritePPU`, all likewise zero-caller; `SpritePPU = 256` was additionally FALSE — 931 of 935 PNGs import at 100 PPU and the four exceptions are ocean textures and UI icons, no hex art. `GameData` now owns exactly two geometry inputs, `HexSize` and `MapPPU`, and everything else derives here.

**HexGridRenderer** (singleton, ~1,096 lines, replaces the old HexMapRenderer): Layer-based renderer that owns 16 `HexLayer` instances across three sorting layers — **Map** (bottom→top: hexOutline, mapIcon, riverBank, riverWater, road, bridgeIcon, cityIcon, impassable, hexSelect, mapText — the selection ring sits ABOVE every terrain feature since 2026-07-22; only labels render over it), **Units** (groundUnit, airUnit), **Overlay** (utility1, utility2, movementRange, movementPath). Manages prefab drawing for cities/bridges/icons/text plus direct-stamp overlays for outlines/rivers/roads/impassable, handles event subscriptions, delegates all position math to `HexGridSystem`. `RefreshMap()` full redraw.

**HexLayer** (~110 lines): MonoBehaviour managing a dictionary of child SpriteRenderer GameObjects for one visual concern (outlines, selection, movement range, etc.). It no longer authors sorting itself — it holds a `SortSlot` assigned in code via `Configure()` (HexGridRenderer wires all 16 in `Awake` → `ConfigureLayerSorting`), and `SetSprite` stamps sorting through `SortingConfig`.

**⚠ Render-pass dimension (Unity layer, NOT sorting layer):** the Forward Renderer's transparent mask (119) EXCLUDES Unity layer 7 ("No Volume Layer"); the `NoVolumeRendering` RenderObjects feature (Forward Renderer_Renderer.asset) redraws layer-7 transparents at Event 600 = AfterRenderingPostProcessing. So **Unity layer picks the PASS** (layer 7 = drawn after post-FX, on top of everything in the early pass); **sorting layers only order within a pass**. ALL map-visual objects — prefabs and HexLayer stamps — must live on layer 7; `HexLayer.SetSprite` inherits the host object's layer for exactly this reason (a bare `new GameObject()` defaults to layer 0 and would render under every prefab icon regardless of sorting layer — this was the movement-overlay-under-cities bug, 2026-07-21).

**Sprite sorting — SortingConfig is the SINGLE authority (`Renderers/SortingConfig.cs`).** One static file maps every `SortSlot` (the 16 concerns: HexOutline…MapText on Map, GroundUnit/AirUnit on Units, Utility1/2 + MovementRange/MovementPath on Overlay) to a Unity sorting layer + base order (spaced by 10). `SortingConfig.Apply(renderer, slot, subOrder)` stamps `sortingLayerName`/`sortingOrder`, **overriding whatever was baked into a prefab asset** — nothing else in render code sets sorting. Split of ownership: the sorting **layer + base order** live in SortingConfig; a **multi-part prefab's internal element order** lives as `const … SubOrder` fields **in that prefab's own script** (Prefab_CityIcon, Prefab_CombatUnitIcon, etc.), passed as `subOrder`. Direct HexLayer stamps use subOrder 0. Every prefab is stamped at spawn: map prefabs via `HexGridRenderer` (`prefab.ApplySorting(slot)`), unit icons via `GameIconRenderer` (`ApplySorting(GroundUnit|AirUnit)`). Prefab-baked sorting layers/orders are now dead data. ⚠ This replaced the old dual system (prefab-baked sorting vs per-HexLayer Inspector sorting) that let a stale prefab sorting layer render cities above the movement overlays.

**BattleBackgroundFitter** (~130 lines, on the "Background Room" GameObject under `World Space/Hex Map/Background`): scales/positions the bunker-room background sprite so the map window baked into the art (glowing table surface inside the green tube border — the border is deliberate padding) frames the loaded hex map at ANY map size. Serialized calibration = the window's center offset + size in normalized image coordinates, reverse-engineered from the hand-tuned 32x21 setup (2026-07-22). `BattleManager.SetupBattleManagerData` calls `FitToMap(w,h)` right after `HexGridSystem.Initialize`. Moves the background only, never the map; per-axis scale means maps should stay ~16:9 by authoring convention (Bob). Background Room lives on Unity layer 6 (EARLY pass — under all layer-7 map content by design).

**GameIconRenderer:** Ground/air layers. `RefreshIconFacing(unitId)` re-resolves an existing icon's sprite variant + easterly flipX from the unit's CURRENT `Facing` — movement steps and Shift+click rotation call it (before 2026-07-22 icons resolved facing only at create time). **Helo motion flipbook (2026-07-22):** `Prefab_CombatUnitIcon.StartMotionAnimation`/`StopMotionAnimation` cycle the 6 `<unit>_FrameN` atlas frames at a serialized fps (default 40) while the icon tween-moves — `AnimateIconStep` starts it, `SnapIcon` stops it (rests on Frame0); detection is sprite-name-based, so embarked air-mobile riding helo art animates too and non-animated icons no-op. Sprite resolution from RegimentProfile, directional flipping. Stacking: air/ground same hex → dominant at 1.0 opacity, recessive at 0.6. Toggle via EventManager. Movement animation: `AnimateIconStep(unitId, to, duration, onComplete)` (LeanTween via UnitMoveAnimator) + `SnapIcon` (cancel tween + hard-place) — driven per-hex by `MovementController.ExecuteMovement`. ⚠ Carries the tilde (~) debug enemy-reveal cheat (rendering-only; REMOVE BEFORE SHIPPING — tracked in Claude_TODO Cleanup).

### 3.5c Chunked Terrain Renderer — THE LIVE TERRAIN PATH (reconciled to code 2026-08-03)

⚠ **THE HEX MAP'S TERRAIN IS BUILT IN CHUNKS. This is not a POC and the phase plan is spent** — the earlier "POC / in progress, Phases 1/2/3/4/6" framing described a prototype that no longer exists and is superseded in full. Mesh-based terrain rendering replaced per-hex terrain sprite stamping. Namespace: `HammerAndSickle.Renderers.Chunked`.

**Who draws what — the split that matters.** `HexChunkRenderer` draws the TERRAIN SURFACE (the 6 base terrains, GPU-blended). `HexGridRenderer` draws everything ON TOP of it — outlines, rivers, roads, bridges, cities, impassable marks, map icons, labels, selection and movement overlays (§3.5). Neither draws the other's content, and `HexGridRenderer` has no terrain layer. Both read their geometry from `HexGridSystem`, which is why they register.

**Driven from `BattleManager.SetupBattleManagerData`**, in this order: `HexGridSystem.Initialize` → `BattleBackgroundFitter.FitToMap` → `HexChunkRenderer.SetActiveTerrainSet(CurrentMapTheme)` + `BuildAllChunks(CurrentHexMap, HexGridSystem.Instance)` → `HexGridRenderer.RefreshMap()`. ⚠ Both renderer calls are **null-tolerant by design** — a missing `HexChunkRenderer` logs a warning and the scene still runs with no terrain, rather than throwing during scenario load.

**HexChunkRenderer** (`Singleton<T>`, ~296 lines): owns the chunk grid and the terrain `Material`. Chunks live in a `Dictionary<(int cx, int cy), HexChunk>` — NOT a flat list; the keying is what makes `RebuildChunk(cx, cy, …)` a single-chunk operation. Caches the last map+grid so `Rebuild()` re-issues a full build with no driver involvement. `SetActiveTerrainSet(MapTheme)` + `BindTerrainArray()` load `Resources/Chunked/TerrainArray_<MapTheme>.asset` and bind it to `_TerrainArray`, warning on a slice-count mismatch (`TerrainType` count × `VariantsPerTerrain`) — the tell that the array was baked against a stale enum. Inspector knobs split two ways and the distinction is real: **shader-side** (`blendNoiseStrength`, `noiseScale`, `disableNoise`) push live through `ApplyShaderTuning()`; **builder-side** (`cornerWeightPower`, `centerHexBias`, `uvInset`, `variantSeedOffset`) are captured into a `HexChunkBuildSettings` struct per build pass and REQUIRE a rebuild. Context-menu entries expose rebuild/rebind/reset; `autoRebuildOnValidate` rebuilds from the cached map while playing.

**HexChunk** (~39 lines): one 16×16-hex chunk — `GameObject`, `MeshFilter`, `MeshRenderer`, generated `Mesh`. Parents under the renderer and shares its material; `Destroy()` cleans up both.

**HexChunkMeshBuilder** (static, ~384 lines): builds one chunk's `Mesh`. Each hex = 6 fan triangles (centre + 6 **Voronoi** corners); corner vertices are deduplicated across the hexes sharing them, and each corner's blend weights mix the terrain slots of the 1–3 hexes meeting there. ⚠ **Corner offsets are DERIVED at static-init from `HexGridSystem.HALF_HEX_WIDTH`/`VERTICAL_SPACING`, never literals** — they are circumcentres of the three hex centres at each corner, so the mesh cannot drift from `HexToWorld`. Vertex layout: `POSITION(float3)` · `TEXCOORD0(float2 UV)` · `TEXCOORD1(float4 terrain indices)` · `TEXCOORD2(float4 blend weights)`. Returns null for an all-off-map chunk region, and the renderer then skips the GameObject too.

**HexChunkVariantSelector** (static): deterministic per-hex variant, a pure function of (`Position2D`, seed) — a **wymix-style integer mixer**, deliberately not an `(x*P1)^(y*P2)` hash, whose diagonal correlations surface the moment adjacent-variant deduplication is added. `VariantsPerTerrain = 12`; `GetSlot(pos, terrain)` = `(int)baseTerrain * 12 + variant`. ⚠ **MinorCity, MajorCity and Impassable have NO chunk art and fall back to `Clear`** so the surrounding terrain still blends through — their visuals are overlay-only (`HexGridRenderer` cities/impassable). So the array is sized 9 terrains × 12 = 108 slices, but only **6 terrains are ever selected**: Water, Clear, Forest, Rough, Marsh, Mountains. `HexChunkRenderer` mirrors its seed into the static `SeedOffset` so editor tools and gizmos see the same shuffle as the build pipeline; new code should prefer the explicitly-seeded overloads.

**HexTerrainBlend.shader** (~126 lines, URP unlit handwritten HLSL): samples the `Texture2DArray` up to 4× per fragment, blended by per-vertex weights, with optional world-space noise perturbation behind the `_HEXBLEND_NOISE_OFF` keyword. Includes `HexNoise.hlsl`.

**Editor tools** (`HammerAndSickle.EditorTools.Chunked`): `TextureArrayBuilder` bakes the 108-slice Texture2DArray from terrain tile PNGs in `Assets/Art/HexTiles/` (512 px tiles, same slot math). `HexBlendTestAssetBuilder` generates the old shader-validation assets (3-slice RGB array + tileable noise PNG) and is now a diagnostic, not part of the build path.

⚠ **REBUILD AFTER A FRESH CLONE — the generated arrays are NOT in git.** All three `Assets/Resources/Chunked/TerrainArray_<Theme>.asset` bakes (MiddleEast/Europe/China, ~289 MB each — over GitHub's 100 MB hard per-file limit; Europe + China first baked 2026-08-19) and `TestArray_RGB.asset` (8 MB) are `.gitignore`d (see the rationale block there) while the source PNGs they are built from ARE tracked. A clone therefore has no terrain arrays until `Tools/Hex Chunk/Rebuild All Terrain Arrays` is re-run — `BindTerrainArray` logs a `Resources.Load failed` error naming the expected path when that happens — and a rebuilt asset carries a NEW GUID, so any serialized reference to it must be re-pointed. ⚠ China tile PNGs are prefixed `CH_` like every other China asset; the builder's original `CN` prefix never matched a real file and was fixed 2026-08-19.

**POC drivers** (removed): `HexChunkPOCDriver`, `HexBlendShaderTest` and `POCCameraController` are deleted; the renderer is driven from `BattleManager` against real scenario data.

### 3.5b Movement System

**MovementController** (singleton, ~859 lines): State machine for player unit movement AND combat input during `BattlePhase.PlayerTurn`. States: `Idle`, `UnitSelected`, `Executing` — `AwaitingTarget` was DELETED 2026-07-06 (no order-confirmation step). Ratified input model (§5.10.6): **left-click = universal select** (enemy click = intel print, never an attack); **right-click inside the movement radius = immediate move**; **Ctrl+left-click = the ONLY combat trigger** — `HandleCtrlClick` → `AttackLegality` (public, shared with CursorController so the cursor never lies) → `TryAttack`, which routes by firer class: ART/SPA/ROC/BM ALWAYS fire §7.13 indirect (even adjacent) via `IndirectCombatAction`; everything else direct via `GroundCombatAction`. Owns range calculation (`MovementRangeResult`), pathfinding, stepped execution (per-hex icon tween + the EVENT checks — contact halt, ground/air ambush, ZoC halt), the §12.4.4a settlement sweep, post-move §6.13/§17.5 tile-control via `TerritoryService`, REP move award, and next/previous eligible-unit cycling. ⚠ **A MOVE IS COMMITTED BLIND (§12.4.4a, ratified 2026-08-10):** the mover's own passive spotting is NOT applied per hex — one `ApplyPostMoveSpotting` pass at settlement covers every entered hex plus the resting hex, and first-contact sound/dispatch/icons land there, once. Per-hex application is what made §6.9 ambush STRUCTURALLY UNREACHABLE (the mover always spotted the ambusher at distance 2, one hex before adjacency); do not reintroduce it. `CheckGroundAmbush` additionally enforces §6.9.9 eligibility via `GameData.IsAmbushEligible` — a hidden tube battery is passed unmolested, never an ambusher. Public surface: `CurrentUnit`, `State`, `AttackLegality`. Pending: Move Undo (§5.11).

**Halts are enum-keyed (`ApplyMovementHalt`, P3 2026-08-10), because the kinds spend DIFFERENT resources** — `ZoneOfControl` and `Contact` keep enough MP for a combat or intel action (two causes, one consequence, named separately so the reason is legible at the call site); `GroundAmbush` zeroes everything. ⚠ **`FlightEvasion` was DELETED 2026-08-10** with the evade-without-damage rule it served; a helicopter now takes the ambush attack like any other victim. `internal` and pinned by `MovementTests`. Each is written as "zero exactly what means this move is over" so it survives Bob's action/movement cost rebalance; a rule leaning on a particular cost would quietly change meaning under it.

**AMBUSH AGAINST A FLIGHT — WHO IS SUBJECT, BY MEDIUM (§6.9; RESOLVED 2026-08-10, superseding the 2026-08-04 reading):** ground → the full ambush, combat and all · **HELO → an ORDINARY attack, but the ambusher is DENIED the §6.9.4 surprise multiplier** · **FIXED-WING → nothing at all.**

⚠ **The helo/fixed-wing split is the load-bearing part and it follows from what each one IS.** A helo-borne regiment is **a special kind of GROUND unit** — it remains on the map, so ground troops can catch it, and it may NOT share a hex with a ground unit. A fixed-wing asset only ever *traverses* the map on its way to the air ops box: it does not spot ground units in transit (§3.7), ground troops cannot touch it, and it **CAN temporarily occupy a ground unit's hex**. Air defence reaches it only through the separate §11.8 transit path, which is how a SAM reveals itself by firing.

**A HELICOPTER IS SHOT AT, NOT STOPPED** (the 2026-08-04 "the ambush triggers, the combat does not" rule is RETIRED). The same `CheckGroundAmbush` detection runs and `AmbushAction.Execute` applies real damage; only `ApplyMovementHalt` is skipped, so the sortie **flies on** unless the §11.8.9 `HeloTransitStandCheck` breaks it — hold and continue, or abort free to the origin hex via `AbortFlightToOrigin`. Speed and altitude deny the ambusher the surprise premium, which is what "avoids the HP damage *of an ambush*" means mechanically. ⚠ **ZoC never stops a flight; fire is the only thing that ends one.** ⚠ Vocabulary: code and comments say halted/stopped — "aborted" is narrative framing allowed only in player-facing dispatch text.

**TRANSIT FIRE (D2, 2026-08-11) — `ResolveTransitFire` is the single per-hex entry point for everything the ground throws at an aircraft.** Two mechanisms, one Shock accumulator (`hpLostThisMove`) and one §11.8.9 stand check: **(1) §11.8 ranged air-defence opportunity fire** from every eligible battery covering the hex — `SpottingService.FindTransitAirDefense` scans, `CombatResolver.ResolveAirDefenseFire` resolves, and **only a FIXED-WING mover gets the §6.10 1d6 detection roll**, and only against a battery that was still unspotted (§5.13.2.4 gives a helicopter no roll at all); **(2) §11.8.11 OVERHEAD FIRE**, the ratified GAD rule — a helicopter crossing **directly over** an enemy ground unit's hex is fired on at Δ = that unit's GAD − the helo's GAD. ⚠ **Same hex only, never a radius** — overflight is avoidable by ROUTING, which makes the flight path a decision instead of making flight suicidal, and gives recon units real work. ⚠ This replaced a `UnityEngine.Random.Range(0, 2)` coin flip that stood in for the entire engagement. ⚠ **Two anti-dogpile records, deliberately not merged:** `enemiesEngagedThisMove` (per MOVE ORDER, shared by ambush and overhead fire) and `CombatUnit.MarkAircraftEngaged` (§11.8.6, per TURN, spans aircraft, lives on the firing unit).

**UnitMoveAnimator** (static, ~104 lines): LeanTween-based hex-to-hex movement animation for combat unit icons. `AnimateHexStep(icon, to, duration, onComplete)` tweens with `EaseInOutQuad`; per-step `onComplete` callback lets `MovementController` run spotting between hexes. `CancelAndSnap(icon, pos)` kills an in-flight tween and hard-places. Suggested durations: 0.15–0.25s ground, 0.08s fixed-wing.

**CursorController** (~200 lines, self-bootstrapping singleton): §24.11.3 live combat feedback (AMENDED 2026-07-22 — crosshair cursor RETIRED) — poll-based (no EventManager subscriptions, survives `ClearAllSubscriptions`): while Ctrl is held, a LEGAL combat target gets the TargetPickOutline hex stamp on its hex (driven through `HexGridRenderer.ShowCombatTargetPick`/`ClearCombatTargetPick` on the utility1 layer, fit-scaled + serialized tint × opacity; cursor stays the default arrow); anything illegal shows the DENIED cursor. Legality comes from `MovementController.AttackLegality` — the same gate the click runs. Procedural placeholder denied texture until real art is assigned; per-mode cursors (unit-pick §24.5.5, AOB placement §24.7a.1) slot in as those input modes land.

### 3.6 Scene Management & Dialog Flow

**Namespace:** `HammerAndSickle.SceneManagement(.Controllers)`. Each scene: one controller (`Singleton<T>`), one always-visible HOME dialog, zero/one overlay. All switching via EventManager dialog events — `OnScene0DialogRequested` / `OnScene1DialogRequested`, payload `Action<UIPanel>` (actual panel references, no enums/strings). Dialogs never reference controllers; each dialog holds Inspector-assigned `UIPanel` refs for the targets it can request.

**UIPanel base:** `Show()`/`Hide()` toggle a serialized `root` GameObject → `OnShow()`/`OnHide()` hooks. `SetFocus(bool)` → `OnFocusChanged(bool)`. Focus semantics differ by scene:
- **Scene 0 home** (`DefaultDialog_Scene0`): `OnFocusChanged` toggles menu-button `interactable` — buttons die while an overlay is up.
- **Scene 1 home** (`DefaultDialog_Scene1` = the battle HUD): `OnFocusChanged(hasFocus)` → `InputService_BattleMap.SetInputEnabled(hasFocus)`. **This is the single map-input gate** — overlay open = ALL map input dead (scroll, zoom, clicks), and the InputActions themselves are disabled.

**Switch flow:** button onClick → dialog callback → `EventManager.RaiseSceneXDialogRequested(target)` → controller hides `_activeOverlay`; if target == home → restore home focus, else → show target + defocus home.

**Scene 1 startup sequence:** `Scene1_Controller.Start` → validate GameDataManager → `PrinterControl.Initialize()` → `BattleManager.SetupBattleManagerData()` → HUD shown DEFOCUSED → Orders overlay opened via the normal event path. Map input first enables when the player clicks **Begin** (Orders → home switch). Exiting Deployment happens via BattleManager's End Turn button (Turn 0 → Turn 1), not the dialog system.

**DefaultDialog_Scene1 extras:** manual singleton (must extend UIPanel, so can't use `Singleton<T>`). Owns click-through hit-testing: `IsScreenPointOverUI(screenPoint)` tests SIX Inspector-assigned HUD panel rects (top menu bar, terrain, unit, printer, **unit ops, battle ops** — the last two added 2026-07-28 with the HUD button pass) against `_uiCamera`; `InputService_BattleMap` consults it for BOTH mouse buttons — clicks over HUD panels never reach the map. ⚠ **A null slot FAILS OPEN** — the loop `continue`s past it and the click reaches the map, so a right-click over an unwired panel issues a move order to the hex underneath. That is how the unit panel leaked from the 2026-07-23 panel consolidation until 2026-07-27 (the slot list still said `_unitGroundPanel`/`_unitAirPanel`/`_leaderPanel`, none of which the surviving single unit panel was assigned to). `WarnOnUnassignedPanels()` now names every empty slot — and a null `_uiCamera`, which mis-tests every panel at once — at `Start`. New battle-HUD button callbacks live here (ratified 2026-07-20).

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

### 3.6e Battle HUD Button Callbacks + Deploy Up/Down (CONFIRMED IN PLAY 2026-07-28)

**Twenty battle-scene buttons are wired.** All callbacks live on `DefaultDialog_Scene1` except **End Turn**, which is `BattleManager.OnEndTurnButton` — do not add a HUD copy, two callbacks on one button would double-fire the turn sequence. ⚠ Names are a contract (§3.6b).

**Most are deliberate STUBS** that announce themselves to the Console and the UI message log rather than doing nothing — a silent stub is untestable, and after wiring twenty buttons there is no way to tell a good binding from a missed one. Unit-scoped stubs name the selected unit, so one press verifies both the wiring and that selection reaches the class. ⚠ **None of the stubs raise their real `EventManager` events**: those events exist but have ZERO subscribers, so raising them would be an elaborate no-op that merely LOOKS implemented. Each becomes real by swapping one `Report(...)` line, alongside the subscriber that services it.

**LIVE (not stubs):** next/previous unit · the six printer nav buttons · display losses / daily losses (§3.6d) · **deploy up / deploy down**.

**Deploy up/down (§8.2, §21.3.1).** `MovementController` services `OnDeployUpRequested`/`OnDeployDownRequested`, modelled on `HandleIntelActionRequested`: phase + side gate → `CombatUnit.TryDeployUP`/`TryDeployDOWN` → publish. The model owns every rule; the controller supplies only the two pieces of map context the model cannot see — `onPort` from `HexTile.IsPort`, and `onAirbase` from adjacency to a friendly airbase that is neither destroyed nor `OutOfOperation` (⚠ **active** is checked, not merely present — a wrecked airfield must not launch paratroopers).

⚠ **THE RAISE LIVES IN THE CONTROLLER, NEVER THE MODEL.** Nothing under `Models/` raises events, and `EventManager.Instance` **lazy-creates a GameObject** — a model-side raise would spawn an EventManager in every headless EditorTest that changes deployment. `?.` does not help: the getter creates the object and never returns null. Same species as the `PrinterMessage.HeaderProvider` and static-loss-ledger workarounds.

⚠ **Success raises a full `RaiseRedrawMapIcons`, deliberately NOT `RaiseUnitDeploymentChanged`.** That event refreshes only the deploy BADGE, but deployment also swaps the unit's MAIN ART — `GameIconRenderer` resolves it through `RegimentProfile.GetIcon(DeploymentPosition, facing)`, so Mobile and Deployed are different sprites. A badge-only refresh leaves a mounted unit drawn as infantry. `RaiseUnitDeploymentChanged` and `RaiseUnitHitPointsChanged` therefore remain unused scaffolding — the coarse redraw covers both.

⚠ **Refusals are NOT printer dispatches** (§24.8.5) — a denial concerns the player's own order, not something they could not see. They go to `AppService.CaptureUiMessage` (and eventually a denial SFX).

⚠ **`CanChangeToState` gained the missing action-economy gate 2026-07-28.** It checked supply, efficiency and movement points but never that a `DeploymentAction` remained, while `ApplyDeploymentTransitionCosts` decremented one unconditionally — a unit could dig in and un-dig all turn for free. The MP check does not cover for it: MP is re-maxed from the newly active profile on every transition, so a unit can hold plenty of MP with no action left.

### 3.6d Equipment Loss Ledger + Loss Report (printer P5/P6 — CONFIRMED IN PLAY 2026-07-28)

**The model:** hit points ARE equipment. `RegimentProfile.TotalIntelStats` is a unit's FULL-STRENGTH roster of weapon systems (the `IntelReportStats` of its deployed/mobile/embarked `WeaponProfile`s, summed) and is never HP-scaled at rest — the `currentHP/maxHP` scaling happens at display time in `CombatUnit.ApplyEquipmentBuckets`. That is precisely what makes it the correct multiplicand: `lost[type] = TotalIntelStats[type] × (hpLost / HitPoints.Max)`.

**`GameDataManager` owns two static ledgers**, both `Dictionary<Side, Dictionary<WeaponType, float>>`: cumulative and daily. Daily is a SECOND accumulator fed by the same booking (never a diff against a snapshot, which breaks silently when the cumulative ledger is cleared or restored) and reset by `BattleManager.SetTurn`, the one place the turn number changes. Both cleared by `ClearAll` — losses are per-battle.

⚠ **Booking is hooked in `CombatUnit.TakeDamage`, the single funnel every damage source already passes through** — direct combat, return fire, ambush, counter-battery, AD fire, air strikes, base attacks, shatter. Hooking anywhere narrower ("after combat") silently misses the ones resolved outside the main exchange, and has to be remembered for every future damage source. **Surrender is the one case it cannot see** (§7.9.6a — the unit is removed intact, so no damage event fires): `RetreatResolver` books its remaining equipment explicitly. Shatter needs nothing — its extra damage is booked, but the withdrawal is not a loss.

⚠ **THE VALUES ARE `float` AND THAT IS LOAD-BEARING.** Rounding per damage event destroys everything small: 3 tanks taking 1 HP of 40 contributes 0.075, rounds to zero, and a regiment can be ground to death reporting no losses. Accumulate fractional; round ONCE per row at render. `TakeDamage` also books HP *actually removed*, not damage *requested* — the two diverge on exactly the blow that kills, so using the request over-reports on every kill.

⚠ **The report rolls up through `RegimentProfile.ClassifyWeaponType`, the SAME classifier the intel report uses**, so the two can never disagree about what counts as a tank. Six ratified rows: Men · Tanks · AFVs · Guns · Aircraft · Helicopters. **Trucks are absent from the intel model entirely** (no truck profile declares intel stats) so there is nothing to report; `EquipmentBucket.TRN` sits in **Aircraft** because its only stat-bearing profile is the An-12 transport plane — revisit if a truck or naval transport ever gains intel stats.

⚠ **THE CRT SHOWS ABOUT TEN LINES INCLUDING THE FRAME HEADER, AND AN OVERRUN CLIPS SILENTLY** — it never throws and is invisible outside the editor. The report is 8 lines: heading and column header share one row (the heading occupies the otherwise-empty row-label column). An empty report REPLACES the table with a 3-line notice rather than trailing below it, which is where "No losses reported." was being clipped away in exactly the case it existed to explain. `LossLedgerTests.Report_FitsTheCrtHeightBudget` pins this. Font is monospace — column alignment depends on it.

Buttons: `DefaultDialog_Scene1.OnDisplayLossesButton` (cumulative) / `.OnDisplayDailyLossesButton` (this turn) — **TWO separate Inspector-wired buttons, ratified 2026-08-20 (Bob)**; a cycle toggle was rejected as hiding the daily report behind an undiscoverable double-press. The orphan `RaiseDailyLossesRequested`/`RaiseTotalLossesRequested` events (zero subscribers ever) were DELETED with the decision — the callbacks read the ledgers directly.

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
(both sides) · HQ unit-lost · ambush (both directions) · **flight halted by ambush (`ReportFlightHalted`,
P3 2026-08-10 — gate B attribution: the move stopped short of the hex the player picked, which without the
dispatch reads as an ignored order)** · objective captured/lost · unit hardened · weather ·
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

**AppService** (static): Exception handling, UI message ring buffer (100), directory paths, test handler. **HexDetectionService:** Mouse → hex via raycast, fires `OnHexSelected` + `OnHexRightClicked` (positioned right-clicks); `ClearSelectionAndNotify` public per §5.10.5. **CameraService:** Movement/zoom, plus `CenterOnPosition(hex)` (called on unit select and after a move). Scroll bounds are a two-part gate: `InputService_BattleMap.ApplyBoundaryConstraints` damps input PER AXIS and only outward (`ConstrainScrollAxis` — comparing headroom to SoftStopDistance in world units, no landing-position prediction), and `ClampCameraToBounds` clamps the transform after each scroll step so out-of-bounds is unreachable. ⚠ Fixed 2026-07-27: one isotropic multiplier off the NEAREST edge used to zero the whole vector at a boundary, stranding the camera until a `CenterOnPosition` teleport. ⚠ `CenterOnPosition` is deliberately NOT clamped (refusing to centre on a unit near the map edge is worse than a briefly out-of-bounds camera, which the next scroll step clamps back). **Scroll bounds are DERIVED from the loaded map since 2026-08-12 (G5)** — `BattleManager.ApplyDerivedScrollBounds` calls `SetScrollBounds` from `SetupBattleManagerData`, converting corner HEXES through `HexGridSystem.HexToWorld` (never re-spelling a hex dimension, §3.5) plus a 10-unit margin, sampling both row parities for the right edge because odd rows are staggered. Before that `SetScrollBounds` had zero callers and bounds were hand-set Inspector values (±100) calibrated for 32x21 Khost — wrong in both directions at any other size. **InputService_BattleMap:** Battle input. **NameGenService:** Random names by nationality.

**SpottingService** (static, ~406 lines): All spotting, fog-of-war, and ambush detection logic for the battle scene. **Dual-domain (§12.3):** `SpottingRangeAgainst(spotter, target)` picks the spotter's AIR vs GROUND range by the TARGET's domain (`IsAirborneSpottingTarget`; attack helos = GROUND targets via NOE). Player side: `RecomputeAllSpotting()` full sweep at turn start + `ApplyPostMoveSpotting(mover, observedFrom)` — the §12.4.4a POST-HOC settlement sweep over the move's path union (REPLACED the per-hex `CheckSpottingForMover` 2026-08-10; per-hex application disarmed §6.9 ambush and self-disarmed §11.11.4 air ambush for RECONA/AWACS transits). ⚠ No fixed-wing skip at the call site — `SpottingRangeAgainst` resolving a transit jet to 0 IS the policy, and RECONA/AWACS look-down rides the same path. AI side (ADDITIVE region, player paths untouched): `RecomputeAIPerception` / `StepAIPerceptionDecay` write the `AIPerceptionState` belief store instead of unit SpottedLevels. ⚠ Any §12 spotting change must update BOTH sides + the `AIPerceptionState.StepDecay` mirror.

**Transit air-defence surface (D2, 2026-08-11)** — `FindTransitAirDefense(mover, hex)` returns EVERY air-defence unit eligible to fire on an aircraft entering that hex, as `TransitAirDefenseContact { Firer, WasUnspotted }`; `RollFixedWingAmbushDetection(firer, mover)` is the §6.10 1d6 check (reveals at Level1 on success); `RevealByOpportunityFire(firer)` is the §12.4.9.1 Level4 reveal for a unit that SHOT. ⚠ The old `CheckAirAmbush` + `AirAmbushResult` enum are DELETED — that shape could describe only ONE battery per hex and only the unspotted half of §11.8, which is why the path it served ended in a coin flip. **Eligibility is the CLASSIFICATION — `GameData.IsAirDefenseClassification`: SAM / SPSAM / AAA / SPAAA and nothing else (§11.8.2a, ruled 2026-08-11).** ⚠ **NOT GAT, and the reason is worth holding:** restricting GAT to true air-defence units would break the stat-comparison paradigm (every unit needs every stat for a Δ to exist), and a `GAT ≥ 6` test does not even produce the intended set — `MANPADS_BASIC` floors *infantry* GAT at exactly 6, so it admits nearly every line regiment. GAT stays the ATTACK VALUE in the lane; "who may shoot" is a different question. Infantry organic anti-air is instead the §11.8.11 overhead GAD rule. The `OpportunityAction` is the §11.8.3 shot METER, not the gate. The scan is side-agnostic, so player air defence engages AI aircraft the day the AI flies. ⚠ **A SPOTTED battery fires too** (§11.8.4) — `WasUnspotted` marks only the narrower §6.10 ambush case; requiring Level0 used to make a located SAM harmless.

⚠ **§12.3.7a — A FIXED-WING AIRCRAFT IN TRANSIT DOES NOT LOOK AT THE GROUND (Bob, 2026-08-10).** `SpottingRangeAgainst` returns 0 against a GROUND target when the spotter is flying fixed-wing. Fixed-wing assets only cross the map to reach the air ops box; they neither see ground units nor are seen by them. The traffic that DOES flow is the reverse — an air-defence unit fires on them and reveals ITSELF (`FindTransitAirDefense`). ⚠ **Keyed on the MEDIUM, not the classification**, because a paratroop regiment riding an An-12 is classification `AB` and would otherwise keep spotting at range 2 from inside the cargo hold. ⚠ **RECONA and AWACS are EXEMPT** — ratified look-down platforms whose 8-hex ground reach other systems are built on (§11.11.3 derives the recon mission's search area from it; §12.3.9 calls exploiting the AWACS look-down a deliberate player risk). ⚠ Helicopters are unaffected: a helo-borne unit is a special kind of ground unit and sees the ground normally. **DESIGN-DOC AMENDMENT OWED: §12.3.7 becomes 0 / 4** for FGT/ATT/BMB/WW/TRN. Because the rule sits at the single §12.3.10 comparison, the sweep, per-hex transit checks, decay floors and the AI mirror all inherit it together. **Two public reveals, and the level is the rule, not a detail.** `RevealToContact(unit)` → **Level1** for a presence learned without the unit firing from the open: the §6.9.3 sprung ambusher (fired FROM CONCEALMENT — you learn where, not what) and the contact-halt blocker. `RevealByOpportunityFire(unit)` → **Level4** (§12.4.9.1 / §11.8.4) for a unit that SHOT from the open — radars hot, position obvious, so equipment is exposed too. ⚠ Renamed from `RevealAmbusherToContact` when the flight-evasion halt it was built for was retired (2026-08-10); it outlived that caller because §6.9.3 and the contact halt both need exactly it.

**MovementModeService** (static, pure, ~90 lines, `Services/MovementModeService.cs`) — **the single authority on HOW A REGIMENT IS PHYSICALLY MOVING RIGHT NOW.** `CurrentMedium(unit)` reads the ACTIVE profile's `MovementMedium`; `IsAirborneNow` / `IsGroundborneNow`; `MaxMovementPoints`; `ScaleMovementPoints(current, oldMax, newMax)` for posture changes. ⚠ **No Unity types, no singletons, no events, and NOTHING reads a balance constant** — safe from model code, audio and headless tests, and immune to Bob's movement/action cost rebalance. Keep it that way.

⚠ **WHY IT EXISTS: the question used to be answered in five places and four were wrong.** `MaxMovementPoints` read the active profile and was right; `IsAirUnit`, `IsHelicopter`, the `isAir` terrain/ZoC/ambush branch and the movement sound all keyed on `UnitClassification`. An air-assault regiment riding Mi-8s is not `UnitClassification.HELO`, so it correctly received 24 movement points and then spent them paying **ground terrain costs while being halted by zones of control it was flying over**. Classification says what a regiment IS; only the active profile says what is CARRYING it. ⚠ `IsAirUnit`/`IsHelicopter` still answer a legitimate DIFFERENT question ("is this fundamentally an air unit" — stacking, icon layers); do not delete them, but never use them for movement.

⚠ **P3 CLOSED THE LOOP 2026-08-10 — the movement rules now read the service, and all THREE sites moved together**: `HexMapUtil.GetValidMoveDestinations` (range), `HexMapUtil.FindPath` (A*), and `MovementController.ExecuteMovement` (step cost, road bonus, ZoC, ambush branch, tween pacing). They must STAY together: fixing execution without pathfinding makes the overlay promise hexes the move cannot deliver, which reads to the player as an ignored order. Pinned by `MovementTests` §P3.

⚠ **THE ONE LINE THAT IS NOT MEDIUM-KEYED, DELIBERATELY: where a unit may STOP.** "May I fly over this hex?" is a medium question; "may I come to rest on it?" is an OCCUPANCY question, and it stays on `IsAirUnit` — because everything that is not fixed-wing (helicopters and lifts included) files in the GROUND stack via `GetGroundUnitAtHex` and the icon layers. Keying the stop test on the medium too would let a flight land on an occupied hex and put two units in one ground stack, which the stacking model cannot draw.

**`IsSealiftedNow` is a PROHIBITION, not a traversal mode.** While `IsNavalEmbarked` the medium is `Naval`, which is neither airborne nor groundborne — so without a guard the unit falls through to the GROUND rules, which block water but happily allow LAND, letting a regiment that boarded ships at a port stroll inland still aboard them. Range and path both return empty: §5.4.2.3 makes naval movement an INSTANT port-to-port jump with the sea passage abstracted away, chosen with the §24.7a.3 Naval Movement Marker (an input mode that does not exist yet). ⚠ **There is no hex-by-hex sea movement to implement** — §5.4.2.6 defers everything finer than port-to-port. Do not add one.

**§5.13.2.7 OVER-WATER GRACE (D3, 2026-08-11).** A helicopter may END a turn over Water but must reach land by the end of its next turn or it is lost. Three parts: `HexMapUtil.CanRestAt` permits water when `MovementModeService.IsAirborneNow` (it previously refused, because a helo occupies the GROUND domain for stacking — so a helo ordered onto water was silently displaced back to land and the rule was unreachable); the persisted `CombatUnit.EndedTurnOverWater` bool; and `BattleManager.ApplyOverWaterGrace`, run from `ProcessUpkeep`. ⚠ **UPKEEP, NOT REFRESH** — Refresh fires before the unit has had the move the rule exists to give it, so a Refresh check means zero turns of grace, not one. ⚠ **One bool gives exactly one turn because it is READ BEFORE IT IS WRITTEN**; landfall clears it and fully restores the grace. ⚠ Losses are booked explicitly via `RecordRemainingEquipmentAsLost` — no damage event fires, and `TakeDamage` is the only automatic ledger hook (§3.6d), the same reason surrender books its own. ⚠ **Helicopters only** — a fixed-wing parked over water is the unbuilt §5.13.5 auto-return gap. ⚠ Not playable until a map has water (Khost has none).

**TerritoryService** (static, ~150 lines): Movement-driven tile control (§6.13 + §17.5) — transit/occupation/ZoC-sweep ownership flips + end-on-STRONGHOLD captures, returned as `TerritoryChangeResult` with `CapturedStrongholds` (`StrongholdCapture` — renamed from `ObjectiveCapture` 2026-08-17; caller applies capture accounting + redraw). ⚠ **All three flip exemptions key on the derived `HexTile.IsStronghold`, not the dead `isObjective` flag** — the "ZoC sweep" here is the six geometric neighbors, not the real ZoC machinery. Fixed-wing transit never flips. HCL decay/recovery (§6.13.5, the Upkeep half) lands with the supply pass.

### 3.7b Audio System (`GameAudioManager` ~1,700 lines + `UIButtonAudio`)

**Shape.** Plain MonoBehaviour + lazy self-creating `static Instance` + `DontDestroyOnLoad`; `EnsureExists()` forces creation. ⚠ **`.Instance` LAZY-CREATES a GameObject** — the same trap as `EventManager`/`GameDataManager`, so nothing under `Models/` may touch it or every headless EditorTest spawns an audio manager. Five channels, all built in code as child objects: **Music** + a **Crossfade** twin (two sources so `CrossfadeToMusic` can run them against each other), **Ambient** (looping), **Briefing** (non-looping, `onComplete` callback + `IsBriefingPlaying()`), and an **SFX pool of 10** allocated round-robin — when all ten are busy the next call **steals the oldest**, which is the practical concurrency ceiling.

⚠ **ALL AUDIO IS 2D.** `spatialBlend` is never set (defaults 0), and there is no `AudioMixer` and no `AudioListener` management anywhere in the project. Positional/panned audio — a combat sound placed at its map hex — does not exist and is a feature, not a setting.

**Content path.** Audio ships under `StreamingAssets/Audio/{Music,Ambient,SFX,Briefings}`, read-only inside the build like all other content (§7.1). Clips are NOT Unity asset references: each is fetched at runtime by `UnityWebRequestMultimedia.GetAudioClip` from a `file:///` URL and cached in one dictionary per channel. ⚠ **SFX must be `.wav`** (`AudioType.WAV` is hard-coded in `LoadSFX`); music/ambient/briefings are `.ogg`. Adding a sound is therefore THREE steps, not one — drop the file, add the enum member, add the `SoundEffectFiles` entry. Unlike a scenario, the enum is still the index.

**PLAYING A SOUND — `GameAudio`, and it has TWO methods on purpose.**
```csharp
GameAudio.Play(SoundEffect.ButtonClick);        // nothing caused it: UI, turn, weather, objectives
GameAudio.PlayFrom(SoundEffect.X, sourceUnit);  // a UNIT caused it — FOG GATED (§27.7.4)
GameAudio.PlayWeaponFire(firer);                // resolves the family via WeaponSoundClassifier, gated
```
⚠ **THE SPLIT IS WHAT ENFORCES THE FOG GATE, and is not a convenience.** A single `Play` with an optional source would default to UNGATED, so forgetting the argument would leak an unspotted enemy through audio — silently, with no compile error. Two methods force "which unit is this sound attributed to?" to be answered at every call site that has one. ⚠ Pass the unit the sound BELONGS to: the FIRING sound is the firer's, the IMPACT is the target's (§27.7.4.2). ⚠ `PlayWeaponFire` must be called AFTER the firing-reveal spotting change (§7.13.5.4 / §12.4.9), or the gate sees a level that is about to change and suppresses a shot the player is entitled to hear.

⚠ **`GameAudio` NEVER LAZY-CREATES.** It reaches the manager through `GameAudioManager.Existing` (returns the instance or null), not `Instance` — whose getter BUILDS a GameObject. Playing a sound can therefore never construct anything, which is what makes audio safe to call from model code and headless EditorTests. This is the trap `EventManager`/`GameDataManager` still carry.

**`AudioCatalog`** (ScriptableObject, `Assets/Resources/Audio/AudioCatalog.asset`) is the authoring surface: rows of `id · AudioClip[] variants · volume · pitchVariation · minRetriggerSeconds`. Adding a sound is drop-a-wav / add-a-row; **tuning needs no code change**. ⚠ Loaded via `Resources` rather than a serialized field because the manager self-creates through `new GameObject()`, where a `[SerializeField]` would be null — same reason `HexChunkRenderer` loads its terrain array that way. Only the catalog lives in Resources; clips are ordinary assets it references, so the build pulls them in. ⚠ Variants are an ARRAY because a sound heard dozens of times per turn fatigues from a single clip, and pitch jitter alone starts to sound synthetic. Maintained three ways: **`Tools/Audio/Audio Catalog Editor`** (the drag-and-drop window — drop a wav on a sound, the row is created; ⚠ **it MOVES stray files into `Assets/Audio/SFX/` and force-reimports them**, because import settings are PATH-GATED and a clip referenced from elsewhere would play late at double memory with no error; it is driven by the ENUM so unbacked sounds are visible rather than needing an audit run), `Tools/Audio/Create Or Update Audio Catalog` (bulk scan by the `SFX_<SoundEffectName>[_n].wav` convention, never overwrites existing tuning) and `Tools/Audio/Audit Catalog`. All three agree on the filename convention deliberately — the window renames on drop so the scanner cannot drift away from it.

**`SfxPlayer`** (plain C# class, owned by the manager — no extra scene object, no growth of an already-large file, unit-testable). Fully synchronous: no coroutines, no cache, no loading. ⚠ Uses **`PlayOneShot`**, which MIXES rather than replacing a source's clip — that is a behavioural fix, not a style choice: the old `clip=`/`Play()` needed a `Stop()` steal that truncated sounds mid-playback when the pool wrapped. ⚠ **Two source groups**: pitch is a per-SOURCE property, so retuning a source warps any one-shot still ringing on it. Sounds with `pitchVariation == 0` (every UI sound) play on a dedicated FLAT source whose pitch is never touched and can never be detuned by a gameplay sound landing on top; varied sounds round-robin a pool of 10.

⚠ **The retrigger debounce is PER-SOUND DATA, defaulting to 0 = OFF.** A blanket debounce suppresses legitimate audio — a double-click is two events, several units firing is several sounds. Only a sound that can genuinely fire many times in one instant opts in. `SfxPlayer.ShouldPlay` is deliberately pure so the window is testable without AudioSources.

⚠ **THE ENTIRE RUNTIME SFX LOAD PATH IS GONE (2026-08-03), AND ITS ABSENCE IS THE DESIGN.** `LoadSFX`, `SoundEffectFiles`, `_sfxCache`, negative caching, the `_sfxLoading` in-flight guard, `PreloadSFX`, `UiSoundEffects`, `PlaySFXCoroutine`, `TryPlayCached`, `EnsureSfxLoaded`, `PlayUISFX`, `PlaySFXWithVariation` and the UI retrigger constant all existed for ONE reason — clips were not in memory. Phase 1 made them imported assets with Preload Audio Data on (§7.1a), and every one of those pieces deleted together rather than needing optimisation. **Do not reintroduce runtime loading for SFX.** The streamed channels (music/ambient/briefing) keep their caches and their negative caching; they are a different problem.

⚠ **`SoundEffect` IS APPEND-ONLY — NEVER INSERT OR REORDER.** Unity serializes enum fields by INTEGER VALUE, and `UIButtonAudio` exposes two as `[SerializeField]`; the scene YAML literally reads `clickSound: 1`. Inserting mid-enum silently repoints every Inspector-assigned button sound in every scene and prefab, with no compile error. Same hazard as the persisted-enum rule (CLAUDE.md item 11) but the payload is scene YAML, not saves. **Renaming is safe** — that is how `MeduimSnareDrum` → `MediumSnareDrum` was fixed.

**`UIButtonAudio`** — per-button, `[RequireComponent(Button)]`, fully Inspector-configured (click/hover sound, volume scales, only-if-interactable). ⚠ **It does NOT use `onClick`** — it implements `IPointerDownHandler`, firing on PRESS rather than release for responsiveness. So button audio is orthogonal to the Inspector-owns-onClick contract (§3.6b) and can never cause the double-fire hazard. Hover was **commented out and inert until 2026-08-03** — every menu button already had `hoverSound` assigned and `SFX_ButtonHover.wav` always shipped, but nothing played it; re-enabled with an `OnDisable` latch reset, because uGUI does not raise `PointerExit` on an object switched off under the cursor.

**Volume.** An `AudioSettings` object holds Master/Music/Ambient/SFX/Briefing volumes, per-channel mutes and `MuteAll`; effective = master × channel, mute-gated. ⚠ Persisted to `Application.persistentDataPath/audio_settings.json` — a **THIRD write location**, distinct from the `Documents/My Games/Hammer and Sickle/` saves+logs pair in §1. ⚠ `SaveSettings` builds a local `JsonSerializerOptions`, which CLAUDE.md item 10 forbids now that its one sanctioned exception (`MapChecksumUtility`) is deleted — open cleanup.

**AUDIO POLICY (`HammerAndSickle.Audio`, `Assets/Scripts/Audio/`) — pure, headless-safe, no Unity types.** Two ratified rules, built 2026-08-03 ahead of the system that will enforce them:

⚠ **`AudioFogPolicy.CanHear(CombatUnit source)` — SOUND IS THE THIRD INTEL CHANNEL (§27.7.4).** The player learns about enemies through exactly three surfaces: the unit ICON (§24.3.2), the dispatch feed (§24.8.3), and AUDIO. The first two are gated by the §12 ladder; an ungated third defeats both, because an unspotted enemy's tank-gun report tells the player there is a tank there. Rule: a Player-side source is always audible; an AI-side source is audible iff `SpottedLevel >= Level1`. **ATTRIBUTION is the mechanism** and it removes any need for a "generic substitute sound" — the FIRING sound belongs to the firer, the IMPACT belongs to the target, so an unseen battery shelling the player produces no gun report and a full impact. ⚠ Threshold is Level1 *because* §24.3.2.1 already shows unit art from Level1, so the sound leaks nothing the icon hasn't — that is what keeps audio one threshold instead of six. ⚠ **NO AI MIRROR** — the one §12-adjacent rule that is deliberately one-sided; the AI does not listen, so §12.9's symmetry requirement does not extend here. ⚠ **FAILS CLOSED** on a null source, deliberately the opposite of `IsScreenPointOverUI` (which fails open and shipped a live defect): a missing sound is cosmetic, a leaked one is exploitable. ⚠ **NO PROXIMITY HEARING, refused in advance (§27.7.4.4)** — "you'd hear tanks next door" reveals unspotted units by position, and §6.9.0 makes ambush load-bearing on unspotted ambushers.

⚠ **`WeaponSoundClassifier.FamilyFor(...)` — FIRE SOUNDS MAP BY FAMILY, NEVER PER PROFILE (§27.7.5).** 177 profiles collapse to 14 `WeaponSoundFamily` values; unarmed classes (AWACS/TRN/RCNA) resolve to `None` = silent. ⚠ **It classifies NOTHING itself** — it maps `RegimentProfile.ClassifyWeaponType`'s output, the single prefix classifier already shared by the intel report and the §24.8.7 loss report, so audio can never call something a tank that the loss report calls an AFV. This is the discipline P6 established rather than copying the prefix list; do not "optimise" it into its own name-matching. A new `WeaponType` with an unknown prefix falls to `None` — silent, not mis-sounded. ⚠ The `FamilyFor(CombatUnit)` overload resolves through the **ACTIVE** profile, since the same regiment fires different weapons by posture (§9.10.4) — never cache a unit's family.

Covered by `AudioPolicyTests`. ⚠ Both are POLICY only — enforcement arrives with the Phase 2 facade, whose API shape is designed to make the gate unforgettable (an ungated `Play` for UI/turn/weather, a source-taking overload for anything a unit causes). See `todo_audio.md`.

**Test coverage split: `AudioPolicyTests` = the RULES, `AudioSystemTests` = the MACHINERY** (catalog lookup, variant selection, the retrigger window, and the facade's never-lazy-create guarantee — 18 tests, GREEN 2026-08-04). ⚠ **The suite exists because every failure mode in the SFX path is SILENCE.** A missing row, a duplicate row, an unassigned clip slot and a debounce swallowing a legitimate second sound all present identically in play — nothing happens — and none of them throw. Silence is also the CORRECT behaviour for an unmapped id, so "no sound" can never be treated as a bug report; pinning the intended silences is the only way to tell them from the accidental kind. ⚠ **No AudioSources are created**: `SfxPlayer` takes its sources by constructor injection and `ShouldPlay` is pure, so the whole debounce runs headlessly. ⚠ One reflection point — `AudioCatalog.entries` is a private `[SerializeField]`, reached by the same field name `AudioCatalogTools` uses through `SerializedObject`, so a rename breaks both together.

**Wired (Phase 3, 2026-08-04).** Main-menu music (Scene0) · snare + ambient combat loop (Scene1) · printer tick · button click. Plus the battle-map layer, all from `MovementController` at the §24.8.6 printer-emitter sites: unit select/deselect · facing · movement · first contact · ZoC halt · out-of-MP · ambush · weapon fire (family-resolved) · impact · kills · objective captured/lost · `ButtonDenied` on all five refusal paths.

⚠ **MOVEMENT SOUND KEYS ON `MovementMedium`, NOT CLASSIFICATION (rebuilt 2026-08-04).** `GameAudio.PlayMovement(unit, predictedSeconds)` asks `MovementModeService` for the ACTIVE profile's medium, so an air-assault regiment sounds like foot / tracked / helicopter across its three postures instead of infantry in all three. The old `GetMovementSFX(UnitClassification)` switch and the "is it dismounted?" patch that briefly sat on top of it are both DELETED — the dismount behaviour falls out of reading the active profile and needs no case. ⚠ **The long cut is chosen by MEASURING the real clip** (`AudioCatalog.Entry.ShortestClipSeconds`), never a constant: Bob's movement recordings run 1.5–2.5 s, so the original ~1 s assumption was wrong the day the first wav landed, and measuring means re-recording retunes it for free. Shortest, not longest, because any variant may be picked and a gap mid-move is the failure worth avoiding — **trailing audio past the end of a move is INTENDED** (Bob 2026-08-04: the sound frames the action rather than tracking it). Consequence: wheeled/tracked long cuts are dead (max travel 1.8 s is inside the clip) and should not be authored; helo and jet still need theirs.

⚠ **THE THREE ATTRIBUTION RULINGS, all of which fail SILENTLY if reversed.** (1) **Ambush is attributed to the VICTIM, never the ambusher** — the ambusher is by definition unspotted (§6.9.0), so attributing it there gates the player's own regiment being hit into silence, and playing it ungated announces a hidden unit. (2) **Fire is the firer's, impact is the target's**, which is why an unseen battery can shell the player audibly without identifying itself — and why no "generic substitute sound" concept is needed. (3) **`PlayWeaponFire` is called AFTER the orchestrator returns**, so the firing-reveal spotting change (§7.13.5.4 / §12.4.9) has landed and the gate reads the post-reveal level; called earlier it suppresses a shot the player is entitled to hear. That one is invisible today (the firer is always the player's own unit) and becomes a real defect the moment the AI turn uses the path. Ungated `Play` is used ONLY where nothing about a unit is revealed: refusals, the ZoC halt, objective flips, select/deselect.

⚠ **`GameAudio.SoundEffectFor` is now filled in** — every arm previously returned `None`, so `PlayWeaponFire` was a complete no-op. `GameAudio.PlayImpact` picks armour/soft via `RegimentProfile.ClassifyWeaponType` (the shared classifier — never a second prefix list), with structures branching first on `IsBase`. ⚠ `UIButtonAudio` sits on 7 MainMenu buttons and **none in BattleScene**, so HUD buttons are still silent (Inspector work, Bob's). ⚠ Most of the wired sounds have **no clip yet** — a silent no-op by design until the wavs land.

### 3.8 File Loaders

**MapLoader:** .map JSON → HexMap with neighbors. Sizes the map from the header (`ResolveMapDimensions`), warns loudly if the manifest's own `mapWidth`/`mapHeight` disagree (the header wins — it is the file being loaded; a mismatch means the `.map` and manifest were not exported together), and **THROWS if any hex falls outside the declared bounds** (G6 — with G1 in place an out-of-bounds hex can only mean a corrupt or hand-edited file, and this used to be five log lines plus a playable, wrong map). The same throw guards `SnapshotMapper`'s restore loop, which had the identical count-and-discard shape. Validates that `saveVersion` matches the current map format (hard reject), that it is > 0, and that `checksum` is non-empty. ⚠ **It does NOT compare the checksum, by design since 2026-07-28 — see §7.1.** **OOBFileLoader:** 3-pass load — (1) units, (2) air attachments, (3) leaders. Auto-detects legacy format.

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

**Domain (3, NEW 2026-08-10 — D0 of the domain pass):** Ground, Air, Naval. ⚠ **THREE SEPARATE QUESTIONS, NEVER ONE.** *How does it move?* → `MovementMedium` via `MovementModeService.CurrentMedium` (terrain cost, ZoC, ambush exposure, pacing, audio). *Where is it?* → `CombatUnit.OccupiesDomain` (hex sharing, icon layer, tile-control flips, who may engage it). *How is it seen?* → `CombatUnit.IsSeenAsAir` (which spotting range an observer uses — a boolean, because there are only two ranges and ships share the ground one). **A HELICOPTER OCCUPIES `Ground`**: it flies but stacks, holds ground, projects ZoC and is ambushed like a ground unit; only fixed-wing is `Air`, and fixed-wing is the only thing that may temporarily share a hex. ⚠ **RULE CODE ASKS THE QUESTION-NAMED PROPERTY; ONLY THE DERIVATION ASKS THE CLASSIFICATION** — testing `IsFixedWing` at a rule site is how "air" got overloaded the first time.

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

**Persistence:** SAVE_VERSION=7 (4→5 profile-slot rebuild 2026-08-08; 5→6 over-water grace 2026-08-11; 6→7 prestige/scoring 2026-08-17 — wallet wiring + scoring anchors + 8 manifest knob mirrors, objective counters dropped; the version ladder history lives at the constant in `GameData.cs`). `MINIMUM_SUPPORTED_SAVE_VERSION` tracks it pre-1.0, so older saves are refused, not migrated. Bumps still queued: AI2 (AIPerceptionState) and P5 (loss ledger), each taking its own. ⚠ `SaveLoad.SaveAsync`/`LoadAsync` still have NO callers — save/load is not wired to any UI, which is why bumps have been free.

**Prestige:** Weapon tier: Gen1(free)→Gen4(150). Unit type: TANK(55), IFV(40), APC(30), ART(100), ROC(175), SAM(145).

**Leadership:** Tier costs: T1(60)→T5(260). Promotions: Senior(100), Top(250).

**Map:** 256px-wide hex, pointy-top REGULAR (cell 2.56 × 2.956 world). Vertical spacing: width × √3/2 = 2.217. Grid size is per-scenario from the `.map` header (min 10x10) — no fixed sizes. Scale: 1 hex = 5 km flat-to-flat, 1 turn = 1 day, 1 unit = 1 regiment (DesignDoc §1a).

---

## 7. Content Pipeline & Data Formats

### 7.0 TERMINOLOGY — there are TWO kinds of scenario, always say which (§20.4.1, ratified 2026-07-28)

| Term | What it is | Ships under |
|---|---|---|
| **Standalone scenario** | A self-contained one-off battle picked from the Scenario menu. No core-force carryover, no campaign position. | `StreamingAssets/Scenarios/<id>/` |
| **Campaign scenario** (= **mission**) | A node in a campaign, played in sequence, carrying core units and prestige forward. | `StreamingAssets/Campaigns/<campaign>/<mission>/` |

⚠ **Never write bare "scenario" where the two would behave differently — name the kind.** They differ in briefing, OOB, progression and narration. The unqualified word has already caused one real conflation: the retired `IsCampaignScenario` path split welded a *gameplay* distinction to a *storage* one, so a campaign scenario could not be standalone-tested and the two content copies silently diverged by eight months. Where a statement genuinely covers both, "both kinds of scenario" is the phrasing.

**Briefing narration is CAMPAIGN-SCENARIO ONLY** (§20.4.2). Standalone scenarios get the written `.brf` text and no audio, so an absent narration asset is the normal case rather than an error. Written `.brf` text is required for both.

### 7.1 Where content lives (Phase 1, 2026-07-27)

All shipped content is READ-ONLY and lives inside the build under `StreamingAssets`. `Documents/My Games/Hammer and Sickle/` holds ONLY player-written data — saves (`cmp/`) and `logs/`. **Nothing is ever copied between them**, which is what makes a Steam patch a file replacement rather than an install-and-merge problem.

```
Assets/StreamingAssets/
  Audio/{Ambient,Briefings,Music}          loaded via UnityWebRequestMultimedia (GameAudioManager)
                                           ⚠ SFX are NOT here — they moved to Assets/Audio/SFX/ as
                                           imported assets on 2026-08-03; see §7.1a
  Scenarios/<scenario>/                    standalone — manifest · map · oob · aii · brf
  Campaigns/<campaign>/campaign.manifest   the mission graph (Phase 2, not yet built)
  Campaigns/<campaign>/<mission>/          manifest · map · oob · aii · brf
```

### 7.1a SOUND EFFECTS ARE THE ONE DELIBERATE EXCEPTION TO "StreamingAssets holds all shipped content" (2026-08-03)

⚠ **SFX live in `Assets/Audio/SFX/` as IMPORTED PROJECT ASSETS, not in StreamingAssets. This is a decision, not an oversight — do not "fix" it back.** The rule is a split **by ROLE, not by folder**:

- **StreamingAssets = STREAMED content.** Music, ambience and briefing narration: large, played one at a time, no latency requirement — and briefing narration is genuinely per-scenario (§7.0 / §20.4.2), so it belongs with the scenario that ships it.
- **Project assets = SOUND EFFECTS.** Small, must be instant, identical in every scenario, and triggered by code rather than authored per scenario. A button click is not scenario content.

**What the move bought, and why it was worth breaking the rule for:** StreamingAssets files are NOT imported by Unity, so their format cannot be controlled and they can only be fetched at runtime by `UnityWebRequest`. That forced a first-play penalty and an entire apparatus to manage it — a cache, a negative cache, an in-flight guard, a preload step, a UI-vs-gameplay API split and a drop-if-not-resident rule — **all of which existed solely because the clips were not in memory.** As imported assets with Preload Audio Data on, they are resident before anything asks to play them, and that whole apparatus deletes rather than needing optimisation.

⚠ **Import settings are enforced by an `AssetPostprocessor`** (`Assets/Editor/Audio/SfxImportSettings.cs`), not a Preset: mono · PCM · Decompress On Load · Preload Audio Data ON · Load In Background OFF, applied to anything under `Assets/Audio/SFX/`. A Preset has to be REMEMBERED, and forgetting it fails SILENTLY — the sound still plays, just late and at twice the memory. With ~80 more SFX to author that is a defect waiting to happen 80 times.

⚠ **Nothing else moves.** `AppService` paths, scenario content, the manifest resolution chain and the `UnityWebRequest` loaders for the other three channels are untouched.

**A scenario is a SELF-CONTAINED FOLDER.** `ScenarioManifest.ContentRoot` (transient, `[JsonIgnore]`) is stamped with the manifest's own directory at load, and `GetMapFilePath()`/`GetOobFilePath()`/`GetAiiFilePath()`/`GetBriefingFilePath()` resolve against it. So patching one scenario touches one folder, and two scenarios can share a filename without colliding — which they do: the standalone Khost and the campaign's Khost are separate content with different briefings and OOBs.

⚠ **`ContentRoot` has ONE point of entry** — `ScenarioDialog_Scene0.LoadScenarioManifests`, the only place a manifest is deserialized. Any new code that builds a manifest must set it, or every content path silently returns empty; `MapLoader` and `OOBFileLoader` both name `ContentRoot` in their failure messages so this surfaces loudly rather than as a mysterious not-found.

⚠ **A scenario's FOLDER NAME IS ITS IDENTITY AND IS PERMANENT ONCE SHIPPED**, because saves reference the scenario by id. Mission ORDER is not encoded in folder names — it lives in `campaign.manifest`, which is exactly what lets a post-release reshuffle be a content patch. A `m01_`/`m02_` prefix is a filing convention recording authoring order and readability at 25–30 missions; **never renumber a folder after release**. Display names (`"Grand Campaign"`, `"Operation Molot"`) live inside the manifests and can change freely, including in a patch — folder/id is machine-facing (lowercase, underscores), display name is human-facing.

**Retired here:** the two parallel path families. `GetMapFilePath()` used to resolve to Documents/My Games and `GetMapFilePath_GDP()` to `Assets/Generated Data`, with `MapLoader` and `BattleManager` choosing on `IsCampaignScenario` — welding a gameplay concept to a storage one, so a campaign could not be standalone-tested and the two copies silently diverged (they had, by eight months). `AppService.GDP_*`, `ScenarioDataPath`, `ManifestsPath`/`MapPath`/`OobPath`/`AiiPath`/`BrfPath`, `OOBFileLoader.LoadStandaloneOob`/`LoadCampaignOob` are all GONE. ⚠ The GDP family could never have worked in a build at all — `Application.dataPath` is `<Game>_Data` in a player, where `Assets/Generated Data` does not exist.

**A SCENARIO IS A FOLDER, AND NOTHING ELSE (2026-07-28).** Adding one is a pure content operation: drop a folder under `Scenarios/` and it is discovered, listed and playable with no code change, no rebuild and no rewiring. Everything that used to make a scenario known to the executable is gone — `ScenarioDialog_Scene0` no longer maps `scenarioId` → `SceneID`, and the `SCENARIO_ID_MISSION_KHOST`/`SCENARIO_ID_CAMPAIGN_KHOST` constants are deleted. ⚠ **There is ONE battle scene**, `SceneID { MainMenu = 0, BattleScene = 1 }`, matching build settings exactly. The retired members were a scene PER SCENARIO, which does not survive 25–30 missions; `Campaign_Khost = 2` was moreover a live crash, since `LoadSceneAsync(2)` had nothing to load. `Scene1_Controller` is fully generic and reads `GameDataManager.CurrentManifest`. **Do not reintroduce a per-scenario branch anywhere in the load path** — that is the coupling this pipeline exists to remove.

Discovery sorts by display name (then `scenarioId`) because `Directory.GetFiles` order is filesystem-dependent, and warns on a duplicate `scenarioId` — a collision is silent otherwise, and saves reference scenarios by id.

⚠ **No `.aii` files exist yet** — the AI pass will author them (Bob, 2026-07-27). A missing AII must stay a clean no-op, never an error.

⚠ **Briefing NARRATION is still code-side and dormant:** `GameAudioManager.BriefingNarration` is an enum mapping `Khost` → `Briefing_Khost.ogg`, with no manifest field and currently no callers at all. When briefing audio is switched on it should be a manifest filename like the other four content files, not another per-scenario enum member. ⚠ It applies to **campaign scenarios only** (§7.0 / §20.4.2), so whatever field carries it must treat "absent" as the normal standalone case and never as a missing-content error.

**Map checksums are RETIRED game-side (2026-07-28, ratified).** `MapChecksumUtility` is deleted. It had zero callers and had never validated anything, but read convincingly enough that it put a false "MapLoader checksum-validates" claim into this very document, which in turn shaped a planned phase that would have hard-failed every map on the day it shipped. **The `checksum` field STAYS in the `.map` header** and the scenario editor keeps computing it: removing it would mean a map-format version bump, an editor change and a re-export of every map, for no gain. Its remaining job is a CONTENT FINGERPRINT — it is how the 2026-07-28 name-form conversion was proved to have changed the representation and not the data. ⚠ **Do not "restore" validation.** The editor's hash input is the hex array in ITS key order, which does not match C# property-declaration order, so the two can never agree without the game permanently mirroring the editor's field layout — a standing breakage risk for a guarantee Steam's file verification already provides on shipped, read-only content.

### 7.2 Formats

**Manifest (.manifest):** JSON — scenarioId, displayName, description, thumbnailFilename, mapFilename, oobFilename, aiiFilename, briefingFilename, prestigePool, isCampaignScenario, mapTheme, difficultyLevel, maxTurns, **deploymentPointCap**, mapWidth, mapHeight, + the 8 scoring/economy keys added 2026-08-17 (prestige pass V11, `todo_prestige.md`): prestigeStipend, prestigeIncomeRate, prestigeProgressBonusRate, earlyFinishMultiplier, victoryThresholdMinor/Major/Decisive, requiredResult (a `BattleResult` NAME — the enum is rename-frozen). Thresholds are two-state: ALL zero = "declares no scoring" (valid, the absent-key default) or a full ladder 0 < minor < major < decisive ≤ 1 — partial declarations refused. A ninth key, `missionObjectives` [{x,y,label?}], is LIVE (C6): cleared-then-stamped onto `hex.IsObjective` by MapLoader at scenario load; absent/empty = no victory gate; per-VARIANT (standalone and campaign manifests over one `.map` may carry different sets). ⚠ `label` is stored + round-tripped but has no game-side consumer yet — dispatches name places via the map's own `TileLabel`; consumers arrive with the objectives HUD/briefing surfaces. ⚠ `ScenarioManifest` is PARAMETERLESS-CTOR + setter-populated since 2026-08-17 — the 16-positional-param `[JsonConstructor]` is DELETED (a property without a matching ctor param silently defaulted on every load); construct programmatically with an object initializer. (`maxCoreUnits` is RETIRED — §20.1; the campaign-wide `coreForcePointCap` lives in the .cmp, not here. `isCampaignScenario` is now inert — nothing reads it — and is scheduled for deletion in Phase 2.2.)

**Map (.map):** JSON — header (name, config, version, checksum, timestamp) + hexes array (position, terrain, movementCost, infrastructure, objective, tileControl, labels, victoryValue, borders).

**OOB (.oob):** JSON — unit definitions (ID, name, pos, nationality, side, classification, role, weapon IDs, stats) + leader definitions (ID, name, grade, ability, skills, assignment).

**Briefing (.brf):** Plain text narrative.

**Save (.cmp):** JSON — `header` (provenance, §7.3) + `campaign` + `scenario` + `mapData` (embedded, null between battles) + `units` + `leaders` + `saveVersion`. Written through `JsonPolicy.Save`, which adds `ReferenceHandler.Preserve` because the snapshot is an object GRAPH.

⚠ **Every enum in every one of these files is written BY NAME** (`"terrain": "Rough"`, `"Nationality": "MJ"`, `"mapTheme": "MiddleEast"`), via `JsonPolicy`. All shipped content was converted 2026-07-28 — `.map`/`.oob` by the scenario editor's re-export, `.manifest` by hand. The reader accepts ordinals too, so a file that regresses to numbers will load silently and stay fragile until the day a member is inserted mid-enum; name-form is the thing being protected, not the parse. See CLAUDE.md §2 items 10–11.

### 7.3 The save/content contract (Phase 3, 2026-07-28)

**A save declares what it was made against.** `GameStateSnapshot.Header` (`GameDataHeader`) carries `saveTime`, `gameVersion`, `scenarioId` and `campaignId`. ⚠ The class existed before this but was referenced by NOTHING — no save ever carried it.

It deliberately holds **three things it does not have**, each for the same underlying reason — a field that looks meaningful but is never populated or verified is worse than no field:
- **No version field.** `GameStateSnapshot.SaveVersion` is the single authority the migration ladder keys off; two version fields can disagree.
- **No checksum.** The old one was never computed or validated — exactly the shape that produced the false checksum claim in §7.1. Integrity checking lands with its verifier or not at all.
- **No `contentVersion`.** Added 2026-07-28 and deleted the same day (Bob's call), along with `ScenarioManifest.ContentVersion`. Content ships INSIDE the build, so `gameVersion` already identifies it exactly and the two could never legitimately differ; modding is designed out; and the `.map` header's editor-maintained checksum is a better content identity than a hand-kept string. It would have shipped always-empty. ⚠ **Revisit only if content ever reaches a player without a new build** — e.g. hand-patching a rebalanced campaign graph to remote testers. That condition is why `CampaignManifest.contentVersion` is still an open question in Phase 2.1 rather than settled the same way.

**P5 is now load-bearing, not descriptive.** `MapData != null` means an IN-BATTLE save: self-contained, carries its own map, loads fine even if its scenario was uninstalled. `MapData == null` means BETWEEN-BATTLE: a roster plus a `scenarioId`, so the scenario must still exist. `SnapshotMapper.VerifyContentAvailable` branches on exactly that — warn for the first, refuse-by-name for the second. It runs **before** `ClearAll()`, so a save that cannot load does not destroy the game in progress.

**Campaign progress is addressed BY ID.** `CampaignData.CurrentScenarioId` + `CompletedScenarioIds` (strings). ⚠ These were typed `CampaignScenario`, a 23-member enum of hard-coded mission names; that put campaign structure in the executable and progress at an ORDINAL, so inserting a mission shifted every later member and silently repointed every save. The enum is deleted. Also gone: `ScenarioData.IsCampaignScenario` (the header's `campaignId` supersedes it) and `ScenarioData.MaxCoreUnits` (retired by §20.1 in favour of `DeploymentPointCap`).

**The migration ladder is tested** (`SaveMigrationLadderTests`, 9). ⚠ Its guards are unreachable through the production entry point while `MINIMUM == CURRENT`, so `SnapshotMapper.RunMigrationLadder` takes injected versions and a step lookup and the tests drive that; `UpgradeSnapshot` passes the real constants. This is the only thing `[assembly: InternalsVisibleTo("EditorTests")]` exists for.

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

## 9. EquipmentBays and WeaponProfile

**EquipmentBays** (was `RegimentProfile`, renamed P1 2026-08-08 — §3.2b): links CombatUnit to weapons. Holds 3 WeaponType slots (Deployed/Mobile/Embarked) + TotalIntelStats (transient). Access: `unit.GetActiveWeaponProfile()`, `unit.GetDeployedProfile()`, etc.

⚠ **VOCABULARY — "CENSUS" IS THE STANDARD TERM (Bob, 2026-08-13).** A profile's authored `IntelReportStats` is its **census**: the full-strength equipment roster of the formation slice that profile represents. The **intel report** is the COMPUTED output — bay-summed (`BuildIntelStats`, duplicate keys ADD), HP-scaled at display time, bucketed by `ClassifyWeaponType`, enemy-merged (ART+ROC → "guns", SAM+AAA+AT → "AA"), and fuzzed per §12.5. The two are one word apart in code (`IntelReportStats` vs `IntelReport`) and conflating them is the confusion behind both the §7.2 carrier double-counts and the 2026-08-13 Humvee defect. The census is ALSO the §24.8 loss-ledger multiplicand (§3.6d), so a wrong census is a wrong casualty report, not just a wrong panel. Guarded by `CensusIntegrityTests` (non-empty for every registered profile bar the equipment-less trucks/sealift; every token buckets non-`None`). ⚠ `AddIntelReportStat` ASSIGNS rather than accumulates — a copy-pasted block aimed at the wrong receiver variable silently overwrites an unrelated profile's census.

⚠ **`RegimentProfileType` IS DELETED and nothing declares a shape.** Which bays a regiment HAS is DERIVED — the Mobile bay is open iff the deployed medium is `Foot`; Embarked eligibility comes from identity plus the `AirDroppable`/`HeloTransportable` capability tags. The doctrine layer is `IsMobileBayOpen` / `MayCarryHeloLift` / `MayCarryFixedWingLift` / `CanAccept` / `TrySetSlot` / `TryClearSlot`, audited by `EquipmentBaysTests`. Naval is a transient STATE (`CombatUnit.IsNavalEmbarked` drawing the shared `TRN_NAVAL`), never a bay.

**WeaponProfile (184 registered profiles as of the 2026-08-13 census pass, ALL built via `FromProfileDef` — the Archetype+Delta+Trait model, §2.6):** the resolver produces the 17-stat line (`ProfileStat`: HA/HD/SA/SD/GAT/GAD/DF/MAN/TS/SUR/GA/OL/STL/PR/IR/SR/MMP), a stored `ICM` (product of quality-trait multipliers, default 1.0, set only via `SetICM`), and a `WeaponCapability` set (replaces the old bool flags). Strike riders are STORED but mostly INERT until their combat-engine consumers land (M13): `GaVsHard/Soft/Base` (read via `EffectiveGroundAttack(targetClass, isBase)`), `ParkedHitBonus`, `OcSuppressionBonus`, `IgnoreAirDefense`, `LoiterReattack`. Plus Ranges (Primary/Indirect/Spotting — NOTE profile SR is UI-only; live spotting uses the GameData classification tables §12.3), Upgrades (PrestigeCost, TurnAvailable), Intel (IntelReportStats), Icons. The old AllWeather/NBC/NVG ratings and Silhouette fields are DELETED — do not reference them.

---

## 10. Intel System

Generated on-the-fly: `WeaponProfile.IntelReportStats` (the CENSUS — §9) → `EquipmentBays.BuildIntelStats()` → `EquipmentBays.GetIntelReport()` → `CombatUnit.GetIntelReport(SpottedLevel)` (filters by level, scales by HP, applies error).

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

⚠ **`IsAirUnit` / `IsFixedWingAirUnit` / `IsAirborneSpottingTarget` ARE GONE (D0, 2026-08-10)** → `IsFixedWing` / (deleted, was a duplicate) / `IsSeenAsAir`. **D9 closed with them:** there were FOUR disagreeing "is fixed-wing" lists — `GameData.IsAirborneClassification` (7 members, the correct one, pinned by `SpottingRangeTests`), `IsAirUnit` (5, missing WW+TRN), `IsAirUnitClassification` (4, missing AWACS too) and one in `GameIconRenderer` (5). All now defer to the GameData list, killing two live bugs: a **transport aircraft was filed in the GROUND layer** (projecting ZoC, ground-ambushable, drawn on the ground icon layer) and an **AWACS could not attach to an airbase at all** because `AddAirUnit` threw on it.

**StrategicMobility:** Heavy, AirLift, NavalAssault, AirDrop, AirMobile, Aviation, Aircraft.
**SIGINT_Rating:** UnitLevel, HQLevel, SpecializedLevel — PARKED (currently unreferenced; reintroduced on CombatUnit in M15).
(`NVG_Rating` / `NBC_Rating` / `AllWeatherRating` / `UnitSilhouette` were DELETED in the trait migration — weather/night/NBC effects now live in `WeaponTraitCatalog` trait effects.)
**SpottedLevel:** 0–5 — the six-rung ladder (§10.1): Level0 unspotted · Level1 contact · Level2 +name · Level3 +deployment · Level4 +coarse buckets @ MAX error · Level5 +exp/eff @ MODERATE error. `Full` is NOT a member (friendly ownership view, §12.2.7). **MapIconType:** Airbase, Fort, UrbanSprawl. **BridgeType:** Regular, DamagedRegular, Pontoon.
**DefaultTileControl:** None, BE, DE, FR, MJ, NE, SV, UK, US, GE, CH, IR, IQ, SA, KW.
**TextColor:** Black, White, Gold, Red, Blue, Grey, Yellow, Green, Teal. **TextSize:** Small, Medium, Large. **FontWeight:** Light, Medium, Bold.
**SceneID:** MainMenu=0, BattleScene=1 — TWO scenes, matching build settings exactly. ⚠ There is NO scene per scenario and none is planned: every standalone scenario AND every campaign mission plays in the one `BattleScene`, which reads `GameDataManager.CurrentManifest` (§7.1). The retired `Scenario_Khost=1`/`Campaign_Khost=2` members were a scene-per-scenario model that does not survive 25–30 missions, and `Campaign_Khost=2` was a live crash — `LoadSceneAsync(2)` with only two scenes in build settings. Do not reintroduce a per-scenario scene or a per-scenario branch in the load path.
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
