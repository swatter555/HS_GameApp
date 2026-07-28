# todo.md — CONTENT PIPELINE PASS: shipping scenarios & campaigns, patch-safe

> **STATUS 2026-07-28 — PHASES 0, 1, 3 and 4 ARE CLOSED. Only PHASE 2 (campaign as data) remains, and Bob
> deferred it:** the `Campaigns/grand_campaign/m01_khost/` files are a layout MOCKUP, and only one real
> scenario exists. What shipped: shipped content lives read-only in StreamingAssets, **a scenario is a
> folder and nothing else** (no code change to add one), all content is name-form, the save carries a
> provenance header and addresses campaign progress by string id, and the migration ladder is tested.
> Everything below is kept as the reasoning record; the Phase 2 plan and its findings are at the bottom.

**Goal (Bob, 2026-07-27):** one comprehensive design for scenarios AND campaign scenarios that makes
post-release patching as cheap and safe as possible. **Explicit non-goal: user editing/modding.**
(Prior PRINTER PASS slice is landed, green and Bob-confirmed — record lives in Claude_TODO.)

---

## TWO FINDINGS THAT SET THE AGENDA

Both are in the persistence layer, both are cheap to fix now and expensive-to-impossible after 1.0 ships.

### F1 — Enums serialize as INTEGERS. This is the single biggest patch hazard in the project.

⚠ **SCOPED 2026-07-27 AND IT IS WIDER THAN FIRST REPORTED — FOUR options objects, none with a converter:**
`SaveLoad._opts` · `MapLoader` :101 · `OOBFileLoader` :310 · `MapChecksumUtility` :36. So this is not only a
SAVE problem: the **shipped `.map` and `.oob` files store enums as ordinals too.** Insert a `TerrainType` and
every shipped map reinterprets its terrain; insert a `WeaponType` and every shipped OOB reinterprets its
units — silently, in content already sold.

System.Text.Json writes enums as numbers by default. So every `WeaponType`, `UnitClassification`,
`Nationality`, `TerrainType`, `DeploymentPosition`, `SpottedLevel` on disk is stored as an ORDINAL.

Insert `T72B` between `T72A` and `T80B` in `WeaponType` — an utterly routine patch for this genre — and every
existing save's tanks silently become different vehicles. The numbers still parse, so there is no exception and
no warning; the player just finds their veteran T-72 regiment is now something else. Adding units in patches is
not a risk for a Panzer-General-style game, it is a certainty.

Fix: `Converters = { new JsonStringEnumConverter() }`. Names survive insertion and reordering; only a RENAME
breaks, and a rename is visible, compile-checked and deliberate.

### F2 — There is no migration path for OLDER saves.

`SnapshotMapper.ApplySnapshot` throws only when `snap.SaveVersion > CURRENT_SAVE_VERSION` (:304). An older save
is loaded with no transformation at all — every field added since simply takes its default, silently. Two
version bumps are already queued (AI2's `AIPerceptionState`, P5's loss ledger) with no ladder to run them on.

Fix: an explicit `Migrate(snap)` chain — v3→v4→v5, each step a named method with a round-trip test.

---

## PRINCIPLES

- **P1 — One source of truth per artifact.** Shipped content lives in exactly ONE place. No copy step, no
  second root, no precedence rules. Duplication is not a backup, it is a divergence waiting to happen — which
  is exactly what `Assets/Generated Data` vs `Documents/.../scenario data` already became.
- **P2 — Address by stable identity, never by position.** IDs are strings; enums persist by NAME; no array
  index, enum ordinal or file offset is ever written to disk.
- **P3 — Every persisted artifact declares its version, and loading is an explicit migration.** Never a hope
  that the shape still matches.
- **P4 — References point ONE way: campaign → scenario.** A scenario must not know whether it belongs to a
  campaign. That is what allows campaign structure to be patched without touching scenario files, and one
  scenario to appear in two campaigns.
- **P5 — In-battle saves are SELF-CONTAINED; between-battle saves are CONTENT-INDEPENDENT.** This is the
  property that makes content patching safe, and it is already half-true: `GameStateSnapshot.MapData` is
  embedded and documented "Null for between-battle saves". Formalise it. A patch then cannot corrupt a battle
  in progress (it carries its own map and units) and cannot corrupt a campaign between battles (it holds only
  the core roster, results, and a scenarioId).
- **P6 — Fail loudly at the boundary.** Version and checksum validated on load, with a message naming the file
  and the mismatch. Silent defaulting is the enemy — it is how F1, F2 and the click-through slot bug all hide.

---

## PHASE 0 — Pre-ship blockers — **CODE DONE 2026-07-27, builds clean, pending Bob**

- [x] **0.1 One serialization policy + string enums.** NEW `Core/Persistence/JsonPolicy.cs` with two presets:
      `Save` (adds `ReferenceHandler.Preserve` — the snapshot is an object GRAPH and emits `$id`/`$ref`) and
      `Content` (a plain tree; deliberately the UNION of the settings the loaders each carried, so it is
      strictly more permissive and nothing that parsed before can stop parsing). Both register
      `JsonStringEnumConverter`. `SaveLoad`, `MapLoader` and `OOBFileLoader` now route through it.
      ✅ Reading old files stays safe — the converter accepts BOTH a string and a number on read.
      ⚠ **`MapChecksumUtility` DELIBERATELY NOT ROUTED.** Its options are a HASH INPUT: the bytes they
      produce ARE the checksum, so adding a converter would change every hash and invalidate the stored
      checksum in every `.map` ever written. Warning comment added at the site so it is not "tidied" later.
- [x] **0.2 Migration ladder** in `SnapshotMapper.UpgradeSnapshot`. ⚠ The old body was NOT merely a stub —
      it stamped `SaveVersion = CURRENT` and returned, i.e. it RELABELLED old data as current, destroying
      the evidence any future migration would need. Replaced with: refuse anything below
      `MINIMUM_SUPPORTED_SAVE_VERSION` (= the 1.0 baseline, per Bob's clean-break ruling), then apply steps
      one version at a time, throwing if a step is missing or fails to advance exactly one version.
- [x] **0.3 Rules recorded in CLAUDE.md §2** as items 10–12: all JSON through `JsonPolicy`; persisted enums
      are never RENAMED (add/reorder is now free — rename is the only breaking operation); every
      `SAVE_VERSION` bump ships with its migration step.
- [x] **0.4 ✅ DONE 2026-07-28 — all shipped content is name-form.** `.map`/`.oob` came back from the
      scenario editor's re-export (Bob confirmed they load in game, 07-27); `.manifest` was the third
      content type and still carried `"mapTheme": 0, "difficultyLevel": 1` — converted 07-28.
      ⚠ The reader tolerates both forms, so a file that regresses to ordinals loads silently and stays
      fragile until the day a member is INSERTED mid-enum. Name-form is what is being protected; the parse
      succeeding proves nothing. (Original note kept below for the reasoning.)
      ✅ **CORRECTION 2026-07-27 — re-emitting does NOT invalidate the checksum**, contrary to what I said
      earlier. `MapChecksumUtility.ValidateChecksum` hashes the DESERIALIZED `HexTile[]` re-serialized with
      its own frozen options (`:106`), not the file text, so the on-disk enum representation cannot affect
      it. Re-emitting is therefore just load → re-serialize with `JsonPolicy.Content` → write.
      ⚠ **THE REAL DECISION IS UPSTREAM, NOT A ONE-OFF REWRITE:** Bob's external scenario editor authors
      these files, so a one-time re-emit gets clobbered on the next export. Either the editor learns to write
      string enums, or the game gets a `Tools/Content/Normalize` menu item run after each export. Ask before
      building either.
- [x] **0.5 ✅ DONE 2026-07-28. NEW `SaveMigrationLadderTests` (9).** Below-minimum refused (naming both
      versions) · at-minimum accepted · missing step throws · missing step PART WAY UP throws at the right
      rung · step that fails to advance throws · step that SKIPS a version throws · step returning null
      throws · well-formed 3→6 chain visits 3,4,5 in order · already-current runs no steps.
      ⚠ **REQUIRED A TEST SEAM, and the reason is the point:** with the shipping constants
      `MINIMUM_SUPPORTED == CURRENT`, so every older save is refused by the floor check and the ladder loop
      is UNREACHABLE — its guards could not be reached through the production entry point at all, and would
      have been first exercised by the real migration they exist to protect. `UpgradeSnapshot` now delegates
      to `internal RunMigrationLadder(snap, minimumSupported, currentVersion, stepLookup)`; production passes
      the real constants, tests inject their own. The switch became `MigrateStep(from)` returning a step or
      null. NEW `Assets/Scripts/AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("EditorTests")]` —
      this is the only thing it is for.

## PHASE 1 — Content location

**LAYOUT — REVISED after Bob's 2026-07-27 answer that standalone scenarios differ from the campaign in
BRIEFING AND OOB.** That rules out flat type-folders: a standalone `Khost.map`/`khost.oob` and the campaign's
differently-tuned versions would collide on filename. Per-SCENARIO folders instead, which also makes a
scenario an atomic content unit — patching one touches one folder, and Steam patches at file level.
```
Assets/StreamingAssets/
  Audio/                       ← existing, untouched
  Scenarios/                   ← the 3 standalone
    <scenarioId>/  manifest · map · oob · aii · brf
  Campaigns/
    khost/
      campaign.manifest        ← the 25–30 node graph
      <scenarioId>/  manifest · map · oob · aii · brf
```
Sizing: ~30 scenarios × ~1 MB ≈ 30 MB on top of the 7.9 MB audio. Nothing for a Steam depot, and JSON
both compresses and delta-patches well.

**✅ PHASE 1 COMPLETE AND CONFIRMED IN GAME (Bob, 2026-07-27).** Content structure on disk is correct —
`Scenarios/khost/` and `Campaigns/grand_campaign/m01_khost/`, both holding the NEWER map/oob (1,056.8 KB /
40.3 KB) with the campaign-specific manifest and briefing. Scenario loads and plays from StreamingAssets.

- [x] **1.1** Content moved to the layout above (Bob) — `Scenarios/khost/` holds the newer files, lowercase
      `khost.map` now matching the manifest.
      `Application.streamingAssetsPath` resolves in BOTH Editor and player with no `#if UNITY_EDITOR`, and the
      folder sits inside `Assets/` so it is covered by the tracking policy set 2026-07-27.
      ⚠ Take the **Documents** copies — they are the newer ones (Khost.map 1,056 KB / 24 Jun 2026 vs the repo's
      968 KB / 12 Nov 2025).
- [x] **1.2 DONE.** One path family. NEW `ScenarioManifest.ContentRoot` (transient) is stamped with the
      manifest's own directory at load; `GetMapFilePath()` and friends resolve against it. DELETED:
      `AppService.GDP_*` · `ScenarioDataPath` · `ManifestsPath`/`MapPath`/`OobPath`/`AiiPath`/`BrfPath` ·
      the four `*_GDP()` methods · `OOBFileLoader.LoadStandaloneOob`/`LoadCampaignOob` (now one
      `LoadOob(manifest)`) · the `IsCampaignScenario` branches in `MapLoader` and `BattleManager`.
      NEW `AppService.StreamingContentPath`/`ScenariosRootPath`/`CampaignsRootPath`.
      ⚠ `ContentRoot` has ONE point of entry (`ScenarioDialog_Scene0`); MapLoader and OOBFileLoader both name
      it in their failure messages so a manifest built elsewhere fails loudly instead of resolving to nothing.
- [x] **1.3 DONE.** Manifest discovery is now a RECURSIVE scan of `Scenarios/` (a scenario is a folder, not a
      file in a shared `manifests/` dir). The three "consult settings to rebuild scenario data and manifests"
      error paths are gone — they promised a feature that never existed and is now meaningless, replaced by
      "verify the game files", which is the actual remedy for missing shipped content.
      `RiverSymmetryVerifier`'s file-picker default re-pointed at StreamingAssets.
- [x] **1.4 SCOPED 2026-07-27 — StreamingAssets IS already in use and the answer is favourable.**
      (An earlier note claimed no C# referenced it; that grep hit its result cap on documentation matches
      before reaching the code.) `GameAudioManager` uses it at :1338/:1370/:1402/:1432 —
      `Path.Combine(Application.streamingAssetsPath, MUSIC_FOLDER, filename)` → `"file:///" + …` →
      `UnityWebRequestMultimedia.GetAudioClip`, async, cached per category, folder consts at :257–260.
      ⚠ **Scenario JSON does NOT need UnityWebRequest.** The audio uses it because `GetAudioClip` is how you
      DECODE audio into an `AudioClip` at runtime — an audio requirement, not a StreamingAssets one. On
      Windows standalone `streamingAssetsPath` is an ordinary filesystem path, so `File.ReadAllText` works
      synchronously (UnityWebRequest is only mandatory on Android, where StreamingAssets sits inside the
      compressed APK, and WebGL — neither is a target). `MapLoader` (:76/:92) and `OOBFileLoader` already do
      exactly that, so **only the root path changes**: no async rewrite, no coroutine plumbing.
      Unity copies StreamingAssets verbatim into the build and STRIPS `.meta`, so those do not ship.

## PHASE 2 — Campaign as data (P4)

- [ ] **2.1** New `CampaignManifest`: `campaignId`, `displayName`, `description`, `thumbnail`,
      `contentVersion`, `startingPrestige`, core-unit carryover rules, and an ordered list of
      `CampaignNode { scenarioId, unlockConditions, nextOnVictory, nextOnDefeat }`.
- [ ] **2.2** DELETE `ScenarioManifest.IsCampaignScenario`. The campaign owns the relationship; the scenario
      is a reusable leaf. This is what turns "rebalance the campaign path" into a content patch rather than a
      code patch.
- [ ] **2.3** Menu lists campaigns and standalone scenarios from the same root, distinguished by which
      manifest type declares them — not by a bool on the scenario.
- [ ] **2.4** Branch shape: CLAUDE.md commits to "dynamic outcomes influence future missions and unlock
      alternate paths", so build the branching DATA SHAPE now even if v1 ships a linear path. Retrofitting a
      graph onto a list is a content migration across every shipped campaign.

---

# PHASE 1 FINISHED — the last per-scenario hardcode is gone (2026-07-28)

**Bob's scoping call, 2026-07-28:** the files under `Campaigns/grand_campaign/m01_khost/` are a LAYOUT
MOCKUP — Bob dropped them in to show the shape. Only ONE real scenario exists (Khost, used to exercise
combat and the AI). **Campaign-as-data (Phase 2) is deferred**; this pass finished the directory/loading
rework instead.

- [x] **The scenarioId → SceneID switch is DELETED** (`ScenarioDialog_Scene0.OnLoadButton`). It was the last
      place a scenario had to be known to the EXECUTABLE: shipping one meant adding a scenario-id constant,
      a `SceneID` member, a switch arm and a build-settings entry, or the player got "Unknown scenario ID"
      on a scenario the menu had just listed. `SceneID` is now `{ MainMenu = 0, BattleScene = 1 }`, matching
      build settings; `SCENARIO_ID_MISSION_KHOST`/`_CAMPAIGN_KHOST` are gone. `Scene1_Controller` was
      already fully generic.
      ⚠ This also fixed a live crash: `Campaign_Khost = 2` called `LoadSceneAsync(2)` and build settings
      hold two scenes. Nobody had hit it only because campaign content was never discoverable.
- [x] **Discovery hardened for scenario #2.** Sorted by display name (`Directory.GetFiles` order is
      filesystem-dependent, invisible with one scenario), and a duplicate-`scenarioId` warning — a
      collision is otherwise completely silent, and saves reference scenarios by id.
- [x] **0.4 CLOSED — `.manifest` converted to name-form** (`"mapTheme": "MiddleEast"`,
      `"difficultyLevel": "MjGeneral"`). The 27-07 re-export covered `.map`/`.oob`; the manifest was the
      third content type and still carried ordinals.
- [x] **Stale-comment sweep:** `JsonPolicy` had a dangling `<see cref>` to the deleted `MapChecksumUtility`
      and still repeated the "re-emitting changes the checksum" claim that 0.4 disproved.

**NET EFFECT — a scenario is now a FOLDER and nothing else.** Drop one under `Scenarios/`, it is
discovered, listed and played with no code change and no rebuild.

⚠ **Still code-side, flagged not fixed:** `GameAudioManager.BriefingNarration` is an enum mapping
`Khost` → `Briefing_Khost.ogg`. It has NO callers and no manifest field, so it is dormant rather than
broken — but it is the same per-scenario-enum shape as the switch just deleted. When briefing audio is
switched on, make it a manifest filename like the other four content files.

**Left in the mockup deliberately** (`m01_khost/`): its manifest duplicates the standalone's `scenarioId`
and `displayName`, and names a briefing file that is not in its folder. Harmless while `Campaigns/` is
never scanned; all three are Phase 2's problem, recorded under Finding B below.

---

# EXECUTION PLAN — Phase 2 + 3 (agent, 2026-07-28) — ⚠ DEFERRED BY BOB, kept for when campaign work starts

## FINDINGS FROM THE READ-THROUGH — settle these before Phase 2 code

### ⚠ FINDING A — DESIGN-DOC CONTRADICTION. §19.1.6 vs Principle P4.

**DesignDoc §19.1.6:** "Player branch selection driven by scenario victory thresholds (per-scenario
manifest); **branching paths between branches are scenario-defined**."
**todo.md P4:** "References point ONE way: campaign → scenario. A scenario must not know whether it belongs
to a campaign." And 2.1 puts `nextOnVictory`/`nextOnDefeat` on the CAMPAIGN node.

These cannot both hold. Flagging rather than silently encoding, per the standing rule.

**Counter-argument (agent's, and I think §19.1.6 is the one that should move):** if a scenario names its own
successors it is no longer reusable — the same Khost map cannot appear in two campaigns, and "reshuffle the
mission order" goes back to being a content edit across every affected SCENARIO file instead of one campaign
file. That is exactly the patch cost Phase 2 exists to remove.

**Proposed reconciliation — split SCORING from ROUTING.** They are different things and §19.1.6 conflates
them:
- **Scenario owns SCORING** — what counts as Decisive/Major/Minor victory here (objectives held, turn
  used, losses). Intrinsic to the map and its objectives; belongs in the scenario manifest. §19.1.6's
  "victory thresholds (per-scenario manifest)" is RIGHT and stays.
- **Campaign owns ROUTING** — which `BattleResult` leads to which next node. Belongs in `campaign.manifest`.
  The clause "branching paths between branches are scenario-defined" is the part that needs amending.

Under this split §19.1.6 keeps its intent — thresholds still drive branch selection — while the graph stays
patchable. ⚠ **Neither half exists in code today:** `ScenarioManifest` carries no threshold fields, and
`BattleManager.CompleteBattle` (:922-924) hardcodes `CurrentResult = BattleResult.Draw` under a TODO. So
this pass builds the SHAPE the routing reads; the scoring that feeds it is later work (M13-adjacent).

**Needs Bob:** ratify the split and let me amend §19.1.6, or overrule and tell me to put successors on the
scenario.

### ⚠ FINDING B — the campaign content folder is broken three ways, and this was invisible because
discovery never scanned `Campaigns/`.

`Campaigns/grand_campaign/m01_khost/campaign_khost.manifest` says:
1. `"scenarioId": "Mission_Khost"` — **identical to the standalone's id.** Campaign nodes address scenarios
   BY ID, so a duplicate id is unresolvable. Should be `Campaign_Khost` — the constant already exists
   (`GameData.SCENARIO_ID_CAMPAIGN_KHOST`, GameData.cs:1513) and is currently used by nothing that works.
2. `"briefingFilename": "mission_khost.brf"` — but the folder holds `campaign_khost.brf`. The briefing would
   have loaded "Briefing file not found." the first time anyone opened it.
3. `"displayName": "Operation Molot"` — also identical, so the two would have appeared as indistinguishable
   rows in one list.

⚠ **Question for Bob: does the external scenario editor author `.manifest` files, or are they hand-kept?**
The map/oob are dated 27 Jul (the re-export) but both manifests are 24 Jun — which reads like hand-kept. If
the editor writes them, my fix gets clobbered on the next export and the fix belongs on their side instead.

### ✅ FINDING C — RESOLVED 2026-07-28. See the Phase 1 section above; the switch is deleted.

### ✅ FINDING D — RESOLVED 2026-07-28 for the shipped scenario. `Scenarios/khost/mission_khost.manifest`
is name-form. The mockup campaign manifest was left as-is with the rest of the mockup.

---

## THE STEPS

- [x] **S1 — ✅ DONE 2026-07-28.** See Phase 0.5 above.
- [ ] **S2 — 2.1 + 2.4, `CampaignManifest`.** NEW `Core/Game Data/CampaignManifest.cs`:
      `campaignId · displayName · description · thumbnailFilename · contentVersion · startingPrestige ·
      coreForcePointCap · nodes[]`, plus transient `ContentRoot` exactly like `ScenarioManifest`.
      `CampaignNode { nodeId · scenarioFolder · scenarioId · requiresCompleted[] · outcomes[] }`.
      **Branch shape (2.4) = outcome EDGES, not `nextOnVictory`/`nextOnDefeat`:**
      `CampaignEdge { minResult: BattleResult, nextNodeId }`, evaluated best→worst with the first match
      winning, plus a `nextNodeDefault`. A binary victory/defeat pair cannot express "Decisive opens the
      Iran branch, Major continues the main line" — which is precisely what §19.1.6 asks for, and the enum
      to key it on (`BattleResult`, 8 members) already exists. Same cost now, and it is a content migration
      across every shipped campaign later.
- [ ] **S3 — 2.2, delete `ScenarioManifest.IsCampaignScenario`** (property + `[JsonConstructor]` param).
      `BattleManager.IsCampaignBattle` (:447) re-sourced from campaign CONTEXT
      (`GameDataManager.CurrentCampaign != null`) — note it is currently write-only, nothing reads it.
      Also delete `ScenarioData.IsCampaignScenario` (GameDataObjects.cs:48), which is a PERSISTED save field
      → see S6. Old manifests carrying the key still parse (unmapped members are skipped).
- [ ] **S4 — 2.3, campaign discovery + the menu.** NEW `GameData.CAMPAIGN_EXTENSION = ".campaign"` so the
      two manifest types are distinguished by EXTENSION, making 2.3's "which manifest type declares them"
      literal — a recursive `*.manifest` scan of `Campaigns/` would otherwise swallow the mission manifests
      too. `CampaignLoader` scans `CampaignsRootPath`, stamps `ContentRoot`, and resolves each node's
      scenario manifest from its own folder. `GameDataManager.CurrentCampaign` + `CurrentNode`.
      ✅ The SceneID switch that used to belong to this step is already gone (2026-07-28), so a campaign
      mission needs no scene work — it loads `BattleScene` exactly like a standalone.
      ⚠ **BOB-GATED UI.** `DefaultDialog_Scene0` already has an unwired `_campaignDialog` slot (:29), so the
      menu anticipated this. I will write `CampaignDialog_Scene0` as a structural TWIN of
      `ScenarioDialog_Scene0` — same `UIListBox` + briefing text + thumbnail + Start/Back buttons, same
      `On*Button()` callback names — so you can duplicate the scenario dialog's prefab and wire it without
      new layout work. **Nothing lists campaigns in game until you do.**
- [x] **S5/S6/S7 — ✅ DONE 2026-07-28.** All of Phase 3; see that section above for what shipped and the
      corrections to its premises. ⚠ ONE PART REMAINS FOR PHASE 2: 3.3 currently resolves a saved
      **scenarioId**. When campaign nodes exist, extend `VerifyContentAvailable` to also check the NEXT
      node's scenario and to offer a way back to the menu rather than only naming the failure.
- [ ] **S8 — content fixes for the campaign mockup** (Finding B): real `scenarioId`, its own `displayName`,
      the briefing filename that is actually in the folder, name-form enums. ⚠ First answer **who authors
      `.manifest` files** — if the external scenario editor emits them, the fix belongs on their side or it
      is clobbered on the next export.
- [ ] **S9 — docs.** ✅ Claude_Project §1/§7.1/§7.2 and the `JsonPolicy` comments were reconciled 2026-07-28
      with the Phase 1 finish. Still owed when Phase 2 lands: §4.1, §5, and — if Bob ratifies — the
      DesignDoc §19.1.6 amendment per Finding A.

**Not in Phase 2 either:** victory-threshold scoring (needs the §19.1.6 ruling AND a real `BattleResult`
calculation — `CompleteBattle` hardcodes `Draw` today; M13-adjacent), and the
`Documents/.../scenario data` deletion (Bob's, not in git).

**⚠ Test handoff:** S1 adds EditorTests and S2–S7 touch persistence. I cannot run Unity. When the code is in
I will ask for a Test Runner pass and will not tick anything green before your result.

## PHASE 3 — The save/content contract (P3, P5) — ✅ COMPLETE 2026-07-28

⚠ **CORRECTION TO 3.1 AS WRITTEN:** it said `saveVersion` + `gameVersion` "both exist on `GameDataHeader`",
implying saves already carried a header. They did not — `GameDataHeader` was declared in
`GameDataObjects.cs` and referenced by **nothing**; it was not on `GameStateSnapshot` and no save ever wrote
one. Only `saveVersion` was persisted. So 3.1 was not "add two fields", it was "put a header on the save at
all".

⚠ **The bump was FREE, and worth knowing why:** `SaveLoad.SaveAsync`/`LoadAsync` still have **zero callers**
— save/load is not wired to any UI — so no save exists that these shape changes could strand.

- [x] **3.1 DONE.** `GameStateSnapshot.Header` wired in, carrying `saveTime · gameVersion · contentVersion ·
      scenarioId · campaignId · combatUnitCount · leaderCount`, stamped by `SnapshotMapper.BuildHeader`.
      ⚠ It deliberately carries **no `version`** — `GameStateSnapshot.SaveVersion` is the ONE authority the
      ladder keys off, and a save reporting two versions that can disagree is worse than one reporting none.
      ⚠ And the old **`checksum` field is DELETED**: never computed, never validated — precisely the shape
      that produced the false "MapLoader checksum-validates" claim and the phase built on it. Integrity
      checking ships with its verifier or not at all.
      NEW `ScenarioManifest.ContentVersion` is the header's source; currently unset, because no authoring
      tool emits it and an absent version is honest where a defaulted `"1.0.0"` asserts one nobody set.
- [x] **3.2 DONE — P5 is now load-bearing rather than descriptive.** `MapData != null` = IN-BATTLE,
      self-contained; `MapData == null` = BETWEEN-BATTLE, content-dependent. Documented on the field itself
      and branched on in code (3.3), so it can no longer quietly stop being true.
      Campaign position is BY ID: `CampaignData.CurrentScenarioId` + `CompletedScenarioIds` (strings), plus
      `CampaignId`. ⚠ These were typed `CampaignScenario` — a **23-member enum of hard-coded mission names**
      in GameData.cs — which put campaign structure in the executable and recorded progress as an ORDINAL:
      inserting a mission shifted every later member, silently repointing every existing save. **Enum
      DELETED.** Also dropped: `ScenarioData.IsCampaignScenario` (superseded by the header's `campaignId`)
      and `ScenarioData.MaxCoreUnits` (retired by §20.1 → `DeploymentPointCap`).
      **`SAVE_VERSION` 3 → 4**, no migration step — `MINIMUM_SUPPORTED` tracks it pre-1.0, so a v3 save is
      refused by name rather than misread. CLAUDE.md §2 item 12 amended to record that exception AND that it
      expires at 1.0.
- [x] **3.3 DONE.** `SnapshotMapper.VerifyContentAvailable` + `GameDataManager.FindManifestById`.
      The verdict branches on the P5 distinction, which is what makes it right rather than merely present:
      an in-battle save whose scenario was uninstalled **still loads** (it carries its own map — refusing
      would throw away a battle the file can fully restore) and only warns; a between-battle save refuses
      with a message that NAMES the missing scenario and says the save is not damaged.
      ⚠ It runs **before `ClearAll()`** — throwing after the wipe would destroy the player's current game in
      order to report that a different one could not be loaded.

## PHASE 4 — Integrity — ✅ CLOSED 2026-07-28: RETIRED, NOT BUILT

**Decision (Bob, 2026-07-28): delete the game-side checksum, keep the header field.** `MapChecksumUtility` is
gone. The `checksum` stays in the `.map` header, maintained by the scenario editor as a content fingerprint —
removing it would cost a map-format bump, an editor change and a re-export of every map for no gain. Rationale
and the do-not-restore warning are recorded in Claude_Project §7.1. Nothing further to build here.

<details><summary>Why the original plan was wrong (kept as the reasoning record)</summary>

⚠ **`MapChecksumUtility` HAS ZERO EXTERNAL CALLERS.** Every reference to `CalculateChecksum`/`ValidateChecksum`
is inside that file. `MapLoader` checks only `saveVersion > 0`, that `checksum` is non-empty (never comparing
it), and that `saveVersion` matches the current format. **Nothing validates a map checksum, ever.** The
statement in Claude_Project that "MapLoader already checksum-validates" was wrong.

⚠ **AND THE TWO SIDES DO NOT AGREE ON THE BYTES.** The scenario-editor agent found this from the outside,
without the source: `System.Text.Json` serializes in property-declaration order, and `HexTile` declares
`reservedInt1/2` + `reservedFlag1/2` AFTER the four border objects, while every editor-authored `.map` emits
them BEFORE. So the editor's hash input is not the byte string `SerializeToUtf8Bytes(hexes, options)` would
produce. Turning validation on as originally planned would have hard-failed every existing map on day one.

- [ ] **4.1 DECIDE FIRST: are per-map checksums worth keeping at all?** Their value was integrity for
      user-supplied maps, and modding is designed OUT. Steam already verifies shipped file integrity, which is
      the same guarantee for free. Leaning: DELETE `MapChecksumUtility` and the `checksum` header field rather
      than fix a hash nobody checks.
- [ ] **4.2 IF KEPT:** the EDITOR's byte order is the ground truth — every authored map already hashes that
      way — so the game must adopt it, never the reverse. Changing the game's order invalidates nothing;
      changing the editor's invalidates every map ever written.
- [-] **4.3** Superseded — nothing validates, so there is no failure to report.

</details>

---

## DELIBERATE NON-GOALS — recorded so they are not re-litigated

- **No user editing / modding** (Bob, 2026-07-27). This is what keeps the design small: no Documents scenario
  root, no search-path overlay, no dedup, no user-wins precedence, no install/copy step, no staleness logic.
- **Balance constants stay COMPILED in `GameData`,** not data-driven. A Steam patch ships a new exe regardless,
  so externalising them buys nothing and adds a parsing and validation surface. Revisit only if live tuning
  without a build is ever wanted.
- **No AssetBundles / Addressables.** Loose JSON in StreamingAssets is simpler, inspectable and adequate at
  this scale (Khost.map ≈ 1 MB). Revisit only if load times or download size become real problems.

## SETTLED BY BOB, 2026-07-27

1. **Pre-1.0 saves — CLEAN BREAK.** Dev saves are discarded; the ladder starts at the 1.0 baseline
   (`MINIMUM_SUPPORTED_SAVE_VERSION`). This is what makes the string-enum change free of a migration step.
2. **Standalone scenarios stay SEPARATE from the campaign** — a limited list, differing from the campaign in
   briefing and OOB. Drives the per-scenario folder layout above.
3. **Volume:** 3 standalone scenarios now; the campaign will run 25–30. So the Phase 1 move is cheap today
   and gets steadily more expensive — do it before authoring the campaign.
4. **Content location:** scenario AND campaign files both ship inside `Assets/StreamingAssets`, read-only.
   `Documents/My Games` keeps saves, logs and settings ONLY. Nothing is ever copied between them — that is
   precisely what makes a Steam patch a file replacement rather than an install-and-merge problem.
