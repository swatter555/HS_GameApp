# todo.md — CONTENT PIPELINE PASS: shipping scenarios & campaigns, patch-safe

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
- [ ] **0.4 ⚠ FOLLOW-UP, NOT YET DONE — shipped content does not GAIN the protection until it is
      RE-EMITTED with string enums.** Today the reader merely tolerates both forms, so the ordinal
      fragility is still latent inside the existing `Khost.map` / `khost.oob`. Re-emitting a `.map` also
      changes its stored checksum, so this needs a deliberate pass with whatever authored those files —
      NOT a silent rewrite. Do it before 1.0 ships, and ideally alongside the Phase 1 file move.
- [ ] **0.5 EditorTest** the ladder: below-minimum refused, missing-step throws, a step that fails to
      advance throws. Cheap, and it locks the contract before any real migration exists.

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

**PHASE 1 CODE LANDED 2026-07-27 — all three assemblies build clean, PENDING BOB'S PLAY-TEST.**
⚠ Still owed on disk: `Campaigns/m01_khost` is a SIBLING of `grand_campaign` and must move INSIDE it, and both
campaign folders are still EMPTY (needs `campaign_khost.manifest` + `campaign_khost.brf` from Generated Data,
plus the NEWER `khost.map`/`khost.oob` from `Scenarios/khost`). The standalone path is complete and testable
without them — the campaign tree simply lists nothing until populated.

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

## PHASE 3 — The save/content contract (P3, P5)

- [ ] **3.1** Save records `saveVersion` + `gameVersion` (both exist on `GameDataHeader`) + NEW
      `contentVersion` and the `scenarioId`/`campaignId` it came from.
- [ ] **3.2** Formalise P5: between-battle campaign saves store core roster + completed results + campaign
      position BY scenarioId — never a pointer into any scenario's internals. In-battle saves stay
      self-contained with embedded `MapData`.
- [ ] **3.3** The one genuine hazard this leaves: a campaign save whose NEXT scenario was removed or renamed by
      a patch. Handle with a named, specific error and a way forward, not a crash — and treat scenarioId as
      permanent once shipped.

## PHASE 4 — Integrity

- [ ] **4.1** Manifest declares the SHA-256 of its map and oob; loader verifies on load
      (`MapChecksumUtility` already exists and `MapLoader` already checksum-validates).
- [ ] **4.2** Failures name the file and the mismatch (P6).

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
