# todo_missionpack.md — THE MISSION PACK: single-file content format + folder reorganization

> **Status: PLAN DRAFTED 2026-08-21, awaiting Bob's review. NOT YET SHARED WITH THE EDITOR AGENT**
> (Bob's call — the AII schema must be nailed down first). Every item marked ⌛AII is the hole the
> AII schema fills; the plan is written so that update is a bounded insert, not a rewrite.
>
> **The goal in one sentence (Bob, 2026-08-21):** the editor generates a single folder Bob drags
> into StreamingAssets and it just works — Scenarios pop up in the menu, Campaign Missions are seen
> by the campaign engine (when Phase 2 builds it).

---

## §0 — SEMANTICS (RATIFIED 2026-08-21, Bob)

| Term | Meaning |
|---|---|
| **Mission** | Either kind — the umbrella term. A pack file = one Mission. |
| **Scenario** | A stand-alone scenario (menu-listed, self-contained). |
| **Campaign Mission** | A campaign scenario (a node in a campaign, core force carryover). |

Supersedes the §20.4.1 two-term vocabulary ("standalone scenario" / "campaign scenario") — the old
rule's *intent* stands (never write an ambiguous bare word where the kinds differ), but the words
change: bare "scenario" now MEANS stand-alone. Design-doc amendment rides with this pass (§13).

---

## §1 — DECISIONS

### Ratified by Bob (2026-08-21)
1. **Manifest lives IN the pack** as its first section (kills manifest↔map mis-pairing — the C6
   objectives-reference-map-hexes class — structurally, not defensively).
2. **Three text artifacts, all in the pack:** (2a) objectives description — ONE field serving both
   kinds; (2b) short description — one/two-line summary; (2c) briefing text — Scenario briefing OR
   Campaign Mission narration transcript, one slot, kind-dependent content.
3. **Clean break.** No dual-format loading, no legacy fallbacks in the pack reader. The editor
   packs AND unpacks, so nothing is trapped in the old format.
4. Thumbnails + narration audio stay LOOSE in the mission folder (audio is technically forced
   loose — `UnityWebRequestMultimedia` needs a real file path; packing would force an
   extract-to-temp second content root).

### Delegated to the agent, taken (2026-08-21)
5. **One unified `MapSection` shape for BOTH the pack and the save embed** (Bob: "lean away from
   band-aids"). `JsonMapData`/`JsonMapHeader` are REPLACED, not wrapped: the vestigial `MapConfig`
   tag, the per-map `saveVersion`, the checksum slot (which the save path fills with a fake
   timestamp string, never validated) and the pre-2026-08 legacy dimension fallback all die in the
   same stroke. This is a **SAVE_VERSION ride** (§9) — free while saving has zero callers, no
   migration arm pre-1.0. G1 is preserved: dimensions are REQUIRED, explicit-or-throw, one
   authority shared by both readers.
6. **Pack extension: `.mission`** — one pack = one Mission, per §0. Discovery keys on it.
7. **`missionKind` is a manifest field** (`Scenario` | `CampaignMission`), not inferred from
   folder location. A pack is self-describing for the same reason a map states its own size (G1):
   a file whose meaning depends on where it sits can be mis-filed with nothing to detect it.
   Folder location stays the FILING convention (`Scenarios/` vs `Campaigns/<campaign>/`);
   discovery warns on a kind/location mismatch rather than guessing.
8. **`missionId` replaces `scenarioId`** as the identity key, per §0 semantics (a Campaign Mission
   carrying a field named "scenarioId" is now wrong vocabulary). Clean-break rename sweep in §11 —
   cheap now precisely because saves have no callers. Folder name remains the filing identity;
   `missionId` remains the machine identity; the permanence rule (never renumber/rename after
   ship) transfers unchanged.

---

## §2 — THE PACK FORMAT (`<missionId>.mission`)

One JSON document, written and read through `JsonPolicy.Content` (string enums, lenient parse).
**Section order is part of the spec** — the editor MUST emit in this order so the menu can
partial-read with `Utf8JsonReader` and stop after `manifest`:

```jsonc
{
  "packVersion": 1,            // exact-match gate, replaces CurrentMapDataVersion's job
  "fingerprint": "…",          // editor's content fingerprint. GAME NEVER VALIDATES (ratified
                               // 2026-07-28, unchanged — it exists to prove content identity)
  "createdAt": "2026-08-21T…", // editor's export stamp
  "manifest":  { …§3… },       // identity + listing metadata + gameplay knobs — FIRST, always
  "briefing":  { "text": "…" },// §6 — the 2c slot (briefing / transcript)
  "map":       { …§4… },
  "oob":       { …§5… },
  "aii":       null            // ⌛AII — reserved, null until the schema lands. Null = clean no-op,
                               // the same rule "missing .aii file" enforces today, now structural.
}
```

- **Version gate:** `packVersion != PACK_VERSION` is a refused load naming both numbers and
  "regenerate from the Scenario Editor" — the same shape as today's map-format gate, whose
  constant (`CurrentMapDataVersion`) this replaces.
- **Read whole, no extraction.** The pack is deserialized straight into memory; nothing is ever
  written outside `Documents/My Games` (the one-source-of-truth doctrine survives intact).
- Size is a non-issue: a 44×21 mission is ~1–2 MB of JSON.

---

## §3 — MANIFEST SECTION (the schema is the agent's to own, per Bob)

```jsonc
"manifest": {
  "missionId":          "khost_standalone",   // machine identity, permanent once shipped
  "missionKind":        "Scenario",           // Scenario | CampaignMission (enum, by name)
  "missionType":        "Assault",            // The operational ARCHETYPE. Value list RATIFIED
                                              // (Bob, 2026-08-21): Assault | Defense |
                                              // MeetingEngagement | Breakthrough |
                                              // DelayingAction | Security.
                                              // ⚠⚠ ALWAYS FROM THE PLAYER'S PERSPECTIVE (Bob):
                                              // Assault = the PLAYER attacks, Defense = the
                                              // PLAYER holds. A known AI-agent failure mode is
                                              // flipping this — the catalog, the design-doc
                                              // prose, the editor spec, and the conformance
                                              // check must each state it explicitly.
                                              // ⚠ Persisted by name → rename-frozen from ship.
  "displayName":        "The Khost Pocket",
  "shortDescription":   "…",                  // 2b — one/two-line summary for lists/tooltips
  "objectivesText":     "…",                  // 2a — prose objective description, both kinds
  "thumbnailFilename":  "khost.png",          // Scenario only; loose file in the mission folder
  "narrationFilename":  "",                   // CampaignMission only; loose .ogg in the folder.
                                              // Replaces the dormant BriefingNarration ENUM —
                                              // exactly the "should be a manifest filename"
                                              // conversion the code comment already prescribes.
  "mapTheme":           "MiddleEast",
  "difficultyLevel":    "Colonel",            // authored default; still UI-only today (no
                                              // gameplay reader — unchanged by this pass)
  "maxTurns":           18,
  "deploymentPointCap": 0,

  // Economy + scoring block — carried over UNCHANGED from today's manifest (V11 + C6/C7):
  "prestigePool":              0,
  "prestigeStipend":           0,
  "prestigeIncomeRate":        0.0,
  "prestigeProgressBonusRate": 0.0,
  "earlyFinishMultiplier":     1.25,
  "victoryThresholdMinor":     0.0,           // two-state rule unchanged: all-zero = no scoring,
  "victoryThresholdMajor":     0.0,           // else full strictly-ascending ladder in (0,1]
  "victoryThresholdDecisive":  0.0,
  "requiredResult":            "MinorVictory",// BattleResult stays rename-frozen
  "missionObjectives":         [ {"x":10,"y":4,"label":"Khost airfield"} ],
  "missionObjectiveFraction":  1.0            // C7 rule unchanged
}
```

**Dropped from today's `ScenarioManifest`, and why:**
- `mapFilename` / `oobFilename` / `aiiFilename` / `briefingFilename` — sections, not files.
- `mapWidth` / `mapHeight` — the copy existed for pre-parse consumers and the mis-pair
  cross-check; in-pack, the map section is right there and mis-pairing is impossible. Objective
  rectangle validation now checks against the ACTUAL map section — strictly stronger.
- `isCampaignScenario` — superseded by `missionKind` (and it was already inert; this completes
  the Phase 2.2 deletion early).
- `description` — superseded by the 2a/2b split (`objectivesText` + `shortDescription`).

**Validation (`IsValid` rewrite):** everything today's checks enforce, minus the dimension gate
(moves to MapSection), plus: `missionKind` defined; `missionType` defined; a `Scenario` SHOULD
name a thumbnail (warn, not refuse); a `CampaignMission` MAY name narration (absent = normal,
§20.4.2 unchanged); objective coordinates validated against the pack's own map section.

**`missionType` — ONE DEFINITION, THREE CONSUMERS, CHECKED AT THE BOUNDARY (Bob's drift concern,
2026-08-21).** The editor will author objectives + victory conditions FROM this flag and the AI
will operate on the same assumptions — so a loose label would let game, editor, and AI agents
drift on the load-bearing question "what kind of mission is this?". The anti-drift design:

1. **`MissionTypeCatalog`** (static, `Core/Game Data/` — the WeaponTraitCatalog/SortingConfig
   pattern: one file owns the definition). Per archetype, the MACHINE-CHECKABLE expectations:
   - `ObjectiveExpectation` — where mission objectives sit at battle start relative to
     `TileControl`: `PlayerHeld` (hold), `AIHeld` (take), `Mixed` (contested).
   - `AllowedRequiredResults` — which rungs fit (Defense admits Draw per the C5 convention;
     DelayingAction admits the defeat rungs; Assault demands a victory rung; …).
   - `ExpectsScoring` — whether a scored ladder is expected for the archetype.
   - Prose definition string (the human-readable meaning, mirrored from the design doc).
   ⚠ Keep the v1 checkable set SMALL — only what Bob ratifies; an over-constrained catalog
   invents rules nobody agreed to. Finer expectations (objective depth, share bands) can join
   later, each with its own ratification.
2. **The editor receives the CATALOG FILE VERBATIM** in the spec courier — and (grounded against
   the editor source, 2026-08-21) this is REAL, not aspirational: the editor is a JS app that
   already PARSES the game's C# sources directly (`parseCombatUnitDB` imports CombatUnitDB.cs,
   `parseWeaponTypeEnum` imports the WeaponType enum). So `MissionTypeCatalog.cs` MUST be written
   as a flat, machine-parseable literal table — no computed values, no helpers between the data
   and the parser — and the editor imports it through its established C#-import pattern. Same
   table, literally.
3. **The game VALIDATES CONFORMANCE at pack load** (rides with the C6 stamp, which already walks
   the objectives with the loaded map in hand): archetype expectations violated → LOUD WARNING
   naming the rule ("manifest says Defense but 5/6 objectives are AI-held — mis-authored or
   mis-typed"). Same refuse/warn split as C6: impossible = refuse, convention = warn. This is
   the teeth — editor drift surfaces on FIRST LOAD, not in play. Double-entry bookkeeping: the
   editor generates from the table, the game independently checks against it.
4. **AI doctrine default (AI3+)** reads the same catalog when it lands — the AI supplement
   references it, never restates it. ⌛AII: when the schema arrives, fix the seam — missionType
   stays the coarse archetype (the one-word summary), the AII carries the fine-grained plan;
   the AII must not re-declare posture at the same altitude.

Reader discipline: the load-time VALIDATOR reads it (a lint, not behavior); menu/briefing
surfaces DISPLAY it; AI doctrine is the one planned BEHAVIOR reader and enters only through a
ratified design-doc section. No other gameplay rule may key off it — the §17 scoring machinery
already encodes posture through thresholds/requiredResult, and two behavioral sources of posture
truth that can disagree is the exact shape this catalog exists to prevent.

---

## §4 — MAP SECTION (`MapSection` — the unified shape, decision #5)

```jsonc
"map": {
  "mapName":    "Khost Province",
  "mapColumns": 32,        // REQUIRED, >= 10 — explicit-or-refused, no fallback table
  "mapRows":    21,        // REQUIRED, >= 10
  "hexes":      [ … ]      // today's HexTile array shape, unchanged
}
```

- **Gone:** `mapConfiguration` (vestigial since G3), per-map `saveVersion` (job → `packVersion`),
  `checksum` (job → pack `fingerprint`), `createdAt` (job → pack `createdAt`), and the legacy
  Small/Large dimension fallback (no pre-2026-08 file can exist inside a pack).
- **Hex shape is untouched** — deliberately. The 672-vs-662 odd-row overhang stays as-is: that is
  a documented two-layer truth, not a band-aid, and making the file ragged is a separate open
  decision this pass does NOT take.
- The editor **stops emitting the dead authored `objective` key** (C6 made it gameplay-dead;
  absent deserializes to false and the clear-then-stamp survives as defence-in-depth).
- **Everything MapLoader enforces survives as MapSection application logic:** null-hex refusal,
  negative-victoryValue warning, G6 out-of-bounds hard throw, C6/C7 objective stamp (missing hex
  = refused load, non-stronghold = loud warn), neighbor build, border symmetrization,
  `ValidateIntegrity`.
- `MapSection.Dimensions()` is the single geometry authority for both readers (pack apply + save
  restore), replacing `ResolveMapDimensions` — same one-spelling rule, smaller surface.

---

## §5 — OOB SECTION

```jsonc
"oob": { "units": [ … ], "leaders": [ … ] }
```

- Wrapper form ONLY — the legacy flat-array autodetect dies with the clean break.
- Unit/leader entry shapes carry over from `OobUnitData`/`OobLeaderData` minus the crutches:
  - **`ClassificationName` dropped** — it existed to survive pre-name-form ordinal shift (the M0
    WW/TRN insert); a pack is name-form from birth, so `Classification` (enum, by name) is
    reliable alone.
  - HP/supply stay 0–1 ratios; the three profile slots stay strings resolved against `WeaponType`
    (unknown → NONE with a warning, unchanged); `AttachedAirUnitIDs` unchanged; the 3-pass load
    order (units → air attachments → leaders) unchanged.
  - **`Spotted` DROPPED (Bob ruled 2026-08-21).** Verified before encoding: every game-side
    `SpottedLevel` reader is the PLAYER'S view of AI units (side-gated in combat/icons/panel/
    audio-fog; the AI's own knowledge is `AIPerceptionState`), battle start zeroes AI units and
    recomputes, and the authored player-side value has no reader. Pack units always start
    Level0; the DTO field goes with the schema.

---

## §6 — BRIEFING SECTION + THE THREE TEXTS

| Artifact | Home | Consumer |
|---|---|---|
| 2b short description | `manifest.shortDescription` | menu list / tooltips (partial-read reachable) |
| 2a objectives description | `manifest.objectivesText` | briefing screen, objectives HUD surface (future) |
| 2c briefing / transcript | `briefing.text` | briefing pane on selection (Scenario); narration transcript + on-screen text (Campaign Mission) |

`briefing` is its own top-level section (not a manifest field) so the menu's partial read stops
before it, and so it has room to grow (per-side briefings, localization) without touching the
manifest schema. `.brf` files cease to exist.

---

## §7 — ⌛AII: THE RESERVED SECTION AND THE EXACT UPDATE HOOK

The pack ships with `"aii": null` and the game treats null as a clean no-op (today's missing-file
rule, now structural). When Bob delivers the AII schema, the update touches EXACTLY:

1. §2: replace `null` with the section sketch.
2. NEW `AiiSection` DTO + its validation rules (this file, new subsection here).
3. `MissionPackLoader`: one `ApplyAii(...)` call after the OOB pass (site reserved in §10 stage 2).
4. The editor spec courier (§12) gains the AII chapter.
5. Tests (§13): an aii round-trip + null-tolerance case.

Nothing else in this plan depends on the AII shape — that is the design intent of the reserved
section.

---

## §8 — FOLDER LAYOUTS (the drag-and-drop contract)

```
StreamingAssets/
  Scenarios/
    khost_standalone/
      khost_standalone.mission      ← the pack
      khost.png                     ← thumbnail (loose, loaded at runtime from the folder)
  Campaigns/
    afghan_82/
      campaign.manifest             ← the mission GRAPH — Phase 2.1 deliverable, NOT this pass;
                                      the pack design must not preclude it and doesn't
      m01_khost/
        m01_khost.mission
        narration.ogg               ← campaign briefing audio (loose, forced — see decision #4)
```

- **Thumbnail moves OUT of `Resources/Scenario Thumbs/`** and into the mission folder, loaded via
  `File.ReadAllBytes` → `Texture2D.LoadImage` → `Sprite.Create` at menu time. This closes the one
  standing violation of "a mission is a folder and nothing else" — today a new Scenario needs a
  build-side Resources asset, which defeats drag-and-drop outright.
- `ContentRoot` (transient, stamped at discovery) survives — it is how the two loose files
  resolve. Its job shrinks from five files to two.
- Audio channels other than narration (music/ambient) are untouched — they are not per-mission
  content.

---

## §9 — THE SAVE-FORMAT RIDE

- `GameStateSnapshot.MapData` retypes `JsonMapData` → **`MapSection`** (decision #5). The
  SnapshotMapper embed writes name + dims + hexes; the fake checksum concat and the
  `Configuration` pass-through are deleted. The restore path calls the same
  `MapSection.Dimensions()` authority and keeps its G6 hard-fail.
- **SAVE_VERSION bumps to the next number when this lands** (9 as of this writing — AI2 and P5
  still take their own later). Pre-1.0 rule: `MINIMUM_SUPPORTED` tracks it, no migration arm,
  extend the no-arm comment in `SnapshotMapper.MigrateStep` + the ladder history at the constant.
- `GameDataHeader.ScenarioId` → `MissionId` in the same ride (rename sweep §11) — the save header
  key is free to move while zero callers exist, and never again after.
- The C6 restore rule is UNCHANGED: the stamped objective flags ride the embedded map; the
  restore path never re-stamps; an in-battle save still loads with its mission uninstalled.

---

## §10 — GAME-SIDE WORK, STAGED

> Each stage compiles + suites green before the next. ⚑ = Bob gate (test runner / play / relay).

**Stage 0 — Schema + reader (headless, no scene contact).**
- [ ] New DTOs: `MissionPack` (packVersion/fingerprint/createdAt + sections), `MissionManifest`
      (§3), `MapSection` (§4), `OobSection` (§5 — reshaped from today's `OobFileData` DTOs),
      `BriefingSection`, `MissionKind` + `MissionType` enums. `PACK_VERSION = 1` in GameData;
      file-extension constant `.mission` (MANIFEST/MAP/OOB/BRF extensions die in stage 4).
- [ ] `MissionTypeCatalog` (static — the §3 anti-drift table): per-archetype checkable
      expectations + prose. Pure, headless; EditorTests pin every member has an entry (the
      ValidateSkillTreeSystem pattern) so adding an enum member without its definition throws.
- [ ] `MissionPackLoader` (static, `Core/Helpers`): `ReadPack(path)` full parse via
      `JsonPolicy.Content` + version gate + `IsValid` chain. **No partial-read path (Bob ruled
      2026-08-21):** discovery full-parses every pack at menu open and caches — the list stays
      short and the game is doing little else at open. The manifest-first section ORDER stays in
      the spec anyway (human readability + it keeps the fast path buildable later without an
      editor change), it is just no longer load-bearing.
- [ ] EditorTests: pack round-trip, partial-read equivalence, version-gate refusal, null-aii
      no-op, objective-outside-map refusal, section-missing refusals. (Fixture packs built
      in-memory — no file I/O in suites where avoidable.)

**Stage 1 — Discovery + menu.**
- [ ] Extract discovery out of the dialog into static `MissionDiscovery` (`Services/`): scans
      `Scenarios/` + `Campaigns/` for `*.mission` via full `ReadPack` (cached), stamps `ContentRoot`,
      sorts, warns on duplicate `missionId` and on kind/location mismatch (decision #7), caches
      via `GameDataManager.SetLoadedManifests`. ⚠ This also fixes the standing latent trap: the
      manifest cache being a UI side effect, which would make a future Load-Game entry point
      refuse valid between-battle saves as "not installed" if the player never opened the
      scenario list. Discovery runs from `Scene0_Controller.Start`, dialog consumes the cache.
- [ ] `ScenarioDialog_Scene0`: lists `missionKind == Scenario` only; thumbnail from the folder
      (§8); briefing pane reads `briefing.text` from the selected pack (full read of ONE pack on
      selection — cheap); `.brf` reading deleted.
- [ ] `GameDataManager.FindManifestById` → `FindManifestByMissionId`, now covering both kinds —
      which is what a between-battle Campaign Mission save will need on the day Phase 2 wires one.

**Stage 2 — The battle-scene load path.**
- [ ] `BattleManager.SetupBattleManagerData`: `MissionPackLoader.ReadPack` once, then apply
      sections in today's order — `ApplyMapSection` (everything §4 lists, including the C6/C7
      stamp), renderer chain unchanged, `ApplyOobSection` (3-pass, `ClearAll` stays where it is),
      `GrabManifestData` (reads `missionKind` → `IsCampaignBattle`), fog reset, ledger capture —
      all behavior-identical downstream of the read.
      ⌛AII: `ApplyAii(pack.Aii)` slot reserved after the OOB pass, no-op on null.
- [ ] **MissionType conformance check** (§3 item 3): runs with/after the C6 stamp (map + tile
      control loaded, objectives known) against `MissionTypeCatalog` — loud warnings naming the
      violated expectation; joins the battle-start diagnostic family (gate/ladder audit).
- [ ] `MapLoader`/`OOBFileLoader` shrink to section appliers (or fold into `MissionPackLoader` —
      decide at implementation by size; the VALIDATION bodies move verbatim either way).

**Stage 3 — Save ride (§9).** Retype `MapData`, rename header key, bump SAVE_VERSION, extend the
      no-arm comment, re-green `SaveMigrationLadderTests` + the C7 round-trip suites.

**Stage 4 — Deletions (the clean break, all in one commit so nothing half-dies).**
- [ ] `JsonMapData`, `JsonMapHeader`, `MapConfig` (enum + every reader), `CurrentMapDataVersion`,
      `ResolveMapDimensions` + legacy table, the manifest↔header dimension cross-check, the
      "not exported together" warning, `ScenarioManifest` (old class), `OobFileData` legacy-array
      autodetect + `ClassificationName` resolution, `GetMapFilePath`/`GetOobFilePath`/
      `GetAiiFilePath`/`GetBriefingFilePath` + `ResolveContentFile` (ContentRoot keeps only
      thumbnail/narration resolution), `BriefingNarration` enum + `BriefingFiles` dict (the
      manifest field replaces them; `PlayBriefing` machinery itself stays, dormant, for the
      campaign engine), `Resources/Scenario Thumbs` load path + `ScenarioThumbnailPath` const,
      MANIFEST/MAP/OOB/BRF extension constants.
- [ ] Sweep for stragglers: any reference to the four old extensions or old class names.

**Stage 5 — ⚑ Bob gates.**
- [ ] ⚑ Full EditorTest suite run (stages 0–4 accumulate; one run at the end is fine unless a
      stage feels risky).
- [ ] ⚑ Play verify: Khost (re-exported as a pack) loads, plays, scores identically; menu shows
      thumbnail + briefing; a deliberately mis-versioned pack refuses with the named message.
- [ ] ⚑ Drag-and-drop test: copy the Khost pack folder under a new name/id, relaunch — it appears
      in the menu with zero code/build changes. (The point of the whole pass.)

---

## §11 — RENAME SWEEP (semantics ruling, §0 — all in stage 4's commit)

| Old | New |
|---|---|
| `ScenarioManifest` | `MissionManifest` |
| `scenarioId` / `ScenarioId` (manifest, save header, campaign data) | `missionId` / `MissionId` |
| `GameDataManager.CurrentManifest` | unchanged (already kind-neutral) |
| `FindManifestById` | `FindManifestByMissionId` |
| `CampaignData.CurrentScenarioId` / `CompletedScenarioIds` | `CurrentMissionId` / `CompletedMissionIds` |
| `ScenarioData` (save DTO) | `MissionData` — **RULED YES (Bob 2026-08-21):** "make everything consistent and clear now; too much confusion in the semantics before" |
| `GameDataManager.CurrentScenarioData` | `CurrentMissionData` (rides the DTO rename) |
| `ScenarioDialog_Scene0` | stays (it lists Scenarios, which is now precise vocabulary) |

Bob's ruling is a MANDATE for full consistency: during stage 4, sweep for any remaining
identifier where "scenario" is used in the umbrella sense (should be "mission") or vice versa,
and fix it in the same commit. Names that are correct under §0 (`ScenariosRootPath`,
`ScenarioDialog_Scene0`) stay.

⚠ `On*Button()` names and `BattleResult`/persisted-enum freezes are untouched by the sweep.
⚠ `AppService.ScenariosRootPath`/`CampaignsRootPath` keep their names — the folders are correctly
named under the new semantics.

---

## §12 — EDITOR DELIVERABLE (blocked on ⌛AII; Bob relays when ready)

The agent writes `MissionPack_Spec_for_Editor_<date>.md` covering: the §2 document + section
order requirement, §3–§6 schemas field-by-field, the §8 folder contract, what the editor STOPS
emitting (dead `objective` key, `ClassificationName`, the four loose content files, ordinal
enums anywhere), pack/unpack expectations (round-trip fidelity), and the fingerprint's role.
**The courier CARRIES `MissionTypeCatalog.cs` verbatim** — the editor imports it via its
existing C#-source parser pattern (§3 item 2), so its parameter generation reads the same table
the game's conformance check reads. Any future catalog change is re-couriered as the file,
never as prose.

### Editor ground truth (agent read the editor source 2026-08-21 — `ScenarioEditor/index.html`,
### single-file JS app, ~405 KB, File System Access API). What it means for this pass:
- **Most of the "editor stops emitting" list is ALREADY their practice**: name-form enums
  everywhere, `classificationName` retired 2026-07-28, new maps write `mapConfiguration: None`,
  manifest dims cross-stamped from MapStore with a point-of-no-return bug check. The pack spec
  formalizes; it barely asks for new discipline.
- **E15 landed editor-side** (per-variant `missionObjectiveFraction`, the C7 gate mirror with
  the round-then-ceil arithmetic, the min-share-at-gate instrument). Only the Khost RE-EXPORT
  remains outstanding — which the pack supersedes, as §12 already records.
- **The E13 variant machinery maps 1:1 onto packs**: one shared body + `VARIANT_FIELDS`
  (scenarioId, displayName, description, briefingFilename, isCampaignScenario,
  missionObjectives, missionObjectiveFraction, briefingText), exported separately per variant →
  becomes "emit two packs". `briefingText` ALREADY lives in editor memory per-variant (the .brf
  is just its write-out), so the briefing section costs them nothing. ⚠ The 2a/2b text split
  (objectivesText/shortDescription) changes their VARIANT_FIELDS list — call it out in the spec.
- **`saveScenarioFolder` already validates-then-writes a folder** (manifest gate + map gate +
  OOB gate, then four writes). The pack collapses four writes into one `write()` — structurally
  a small editor change.
- **The double-entry pattern already exists**: their ObjectiveGate/LadderAnalysis explicitly
  self-describe as "the authoring-side catch, the game the runtime backstop." The
  MissionTypeCatalog conformance check extends a philosophy they already practice.
- **Fingerprint definition is theirs today**: SHA-256 over the INT-form hexes array (hashed
  before name-form conversion — their "firewall" ordering). Spec carries this forward as
  editor-defined; the game's never-validates stance is unchanged. The pack fingerprint's input
  set is the editor's to define and document.
- **⚠ MAP_PRESETS exist editor-side** (Raid/Valley/Standard/Corridor/Front/Offensive — SIZE
  templates with blurbs) and are DELIBERATELY never persisted ("a persisted size name is exactly
  what MapConfig was"). The spec must pre-empt the obvious objection: `missionType` is NOT that
  mistake — nothing mechanical derives from it (geometry, costs, scoring all stay explicit
  fields); it persists SEMANTICS, checked as lint, consumed behaviorally only by the future
  ratified AI-doctrine reader. Presets stay editor-only and orthogonal (a Raid-SIZED map can
  host a Defense mission).
- **Thumbnails: the editor never touches them** (field defaults empty, nothing written). v1
  contract: Bob drops the PNG into the mission folder by hand; the editor MAY grow a copy-in
  step later. The spec should say so explicitly so nobody assumes the editor authors art.
- **AII is confirmed vapor on both sides**: the editor UI labels it "(optional — editor does not
  author .aii)" and their own comment notes shipped Khost names a `khost.aii` that exists
  nowhere. The pack's null section replaces a dangling filename with an honest null.

**Sequencing supersessions for Bob's relay (do not let them do dead work):**
- E15 + the Khost manifest re-export (C7 fraction) as separate files are SUPERSEDED — the
  fraction ships inside the two re-exported Khost packs instead.
- The old Bob's-queue relay items about `mapConfiguration: None` and manifest `mapWidth`/
  `mapHeight` cross-stamps are OBSOLETE under the pack (both fields die).
- Both shipped Khost variants get re-exported as packs (the ⚑ play gate's input).

---

## §13 — DOCS RIDING THE PASS

- [ ] HS_DesignDoc: §0 semantics as a ratified amendment (supersedes §20.4.1's wording); §7.x
      content-format sections rewritten for the pack; briefing-narration field note (§20.4.2
      mechanics unchanged); NEW mission-archetype section — the prose definitions the catalog
      mirrors (agent drafts per archetype: what it means, objective placement, fitting
      requiredResults, AI posture assumption; BOB RATIFIES before the catalog is written —
      the catalog encodes ratified text, never invents it).
- [ ] Claude_Project: §1 layout, §3.8 loaders, §7 content pipeline rewritten; large-file list if
      line counts move.
- [ ] Claude_TODO: thread-board row (added 2026-08-21), pass ledger on close, Bob's-queue
      supersessions from §12.

---

## §14 — EXPLICITLY OUT OF SCOPE

- The campaign ENGINE: `campaign.manifest` graph schema, discovery of campaigns, campaign menu,
  core-force carryover (Phase 2 — this pass only defines the folder contract it will land on).
- AII SEMANTICS — only the reserved slot ships here.
- Save/Load UI wiring (still its own unbuilt feature; this pass only moves the embed shape).
- The odd-row ragged-file question (deliberately not taken, §4).
- `CurrentScenarioData`-never-constructed gap — belongs to save wiring, noted here so the pack
  pass is not blamed for it.

---

## §15 — RULINGS (all four questions ANSWERED, Bob 2026-08-21)

1. **OOB `Spotted` field: DROPPED.** Agent verified before encoding (§5): every reader is the
   player's-view-of-AI-units, side-gated; the authored player-side value has no reader.
2. **`ScenarioData` → `MissionData`: YES, and the ruling generalizes** — "make everything
   consistent and clear now; too much confusion in the semantics before." Full-consistency
   sweep mandated (§11).
3. **Partial read: NOT required.** Full-parse + cache at menu open ("the scenario list won't be
   that long; there is little else the game does as it opens"). Section order stays in the spec
   as convention (§10 stage 0).
4. **`missionType` value list RATIFIED as proposed:** Assault · Defense · MeetingEngagement ·
   Breakthrough · DelayingAction · Security. **⚠ WITH THE PERSPECTIVE RULE: mission types are
   ALWAYS from the PLAYER'S perspective** (Bob: "a little niggle that gets AI agents at times") —
   Assault = the player attacks, Defense = the player holds, DelayingAction = the player trades
   ground. Stated explicitly in the §3 schema, the catalog, the design-doc prose, and the
   editor spec; the conformance check's expectations are all written player-relative.
