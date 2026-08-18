# todo_prestige.md — Victory Scoring, Prestige Economy & Objective Obsolescence (V-pass)

> **Authority:** `PrestigeVictory_Handoff_to_GameAgent_2026-08-17_v2.md` (V1–V16, Bob-ratified) + the
> editor side's answers doc (Q1–Q6 + V17, 2026-08-17 PM). The AM handoff is SUPERSEDED — move it to
> `_to_delete/`. Every load-bearing claim in both docs was verified against the tree 2026-08-17 before
> this plan was written; deviations found are folded in below as C1–C4 and the Q-refinements.
>
> **⚙ Testing handoff:** the agent cannot run Unity. Each ⚑ GATE below becomes a ⚑ TESTING REQUESTS
> entry in `Claude_TODO.md` when reached — "Please run Unity Test Runner for me", then WAIT.
>
> **Legend:** `[ ]` todo · `[~]` in progress · `[x]` done · `[-]` dropped

---

## Corrections to the spec (agreed with Bob 2026-08-17 — build these, not the v2 pseudocode)

- **C1 — No-scoring guard.** All-zero thresholds are VALID (V11.3) but the v2 `Grade()` would return
  `DecisiveVictory` for any share ≥ 0, and V10.1's early-end check (`PlayerShare >= DecisiveVictoryCut`
  with cut = 0) would end the battle at the FIRST turn boundary. Both shipped manifests will carry
  zero/placeholder thresholds, so this is a day-one live bug as specced. Guard explicitly: thresholds
  all zero → scenario declares no scoring → grade `Draw`, never early-end, loud `CaptureUiMessage`.
- **C2 — `CheckVictoryConditions` computes FRESH.** It calls `VictoryLedger.Compute` directly instead
  of reading the upkeep-cached `CurrentLedger` (one map pass, once per TurnBoundary — cheap). Removes
  the phase-lag and the ordering dependency; caching-is-how-this-bug-returns is the ledger's own
  argument (V5.3), applied consistently.
- **C3 — No `lastTurnIncome` field.** The early-finish bonus computes from the LIVE ledger at the
  moment of cash-out: `unusedTurns × incomeNow × earlyFinishMultiplier`. The v2 spec's stored field
  would have needed persistence it never listed (save → reload → cash out = garbage bonus). Same
  incentive, zero state.
- **C4 — Design-doc amendments land in this pass** (house rule: ratified decisions amend the rules
  they change, same session). See Stage 4.

## Q-refinements from the editor's answers doc (all verified)

- **Q1:** parameterless ctor per §15.2, and **DELETE the 16-param ctor** (option 1) — sole caller
  `MapStandardTests.cs:33` becomes an object initializer and stops churning per new field.
- **Q2:** rename `ObjectiveCapture`→`StrongholdCapture`, `CapturedObjectives`→`CapturedStrongholds`
  (3 files, zero persistence paths). Also fix the 3 doc comments + 3 test-assertion nouns. Do NOT
  touch `PrinterDispatch.ReportObjective*` (player-facing word is right) or `SFX.Objective*`
  (SoundEffect serializes by INTEGER in scene YAML — rename safe but pointless; never insert).
- **Q5:** the `SAVE_VERSION` 7 paragraph must also record WHY it did not ride with AI2b-3 (unscheduled,
  behind M13-scale work; deferring a ready bump to wait for an unscheduled one is the v4 failure mode).
- **V17:** map icons throw on any non-MiddleEast theme. Code-side fix only (art is Bob's): resolve the
  sprite BEFORE `Instantiate`, and on a missing theme asset warn-and-skip instead of throw.

---

## Stage 1 — Ledger, plumbing, manifest (no visible change) → ⚑ GATE 1
### ✅ CLOSED 2026-08-17 — GATE 1 GREEN (Bob ran suite + Khost load-and-play), COMMITTED

- [x] **V5** `VictoryLedger` (`Models/Map/VictoryLedger.cs`): recomputed never accumulated; `double`
      accumulator (V5.1); neutral stays in the denominator (V5.2); `TotalValue == 0` legitimate →
      share 0 (V5.4); `v <= 0` skip covers odd-row filler (V5.6). Two call sites only: battle start
      (`BattleManager.CaptureStartingLedger` — sets `StartingPlayerShare` + initial
      `HighWaterVictoryValue`, warns loudly on a zero-value map) + the player-side branch of
      `ProcessUpkeep` (V5.3). No dirty-flag caching, ever.
      **Decision (Bob, 2026-08-17):** negative `victoryValue` → MapLoader warns (first 5, same style
      as the null/fail counters), ledger's `v <= 0` skip treats it as 0. Loaded as-is so the editor
      can still see it.
- [x] **V8** prestige plumbing. ⚠ **DEVIATION, deliberate: the arithmetic lives in a new pure class
      `Models/General/PrestigeWallet.cs`** (`Seed`/`Add`/`TrySpend`), owned by BattleManager, whose
      `CurrentPrestige`/`PrestigeEarned`/`PrestigeSpent` became read-only passthroughs. Reason:
      BattleManager is a MonoBehaviour and its instance methods are not headless-testable — the same
      reason TurnStructureTests drives only static helpers — while V16.3 demands wallet tests. The
      raise stays in BattleManager (EventManager in a pure class = the lazy-create trap).
      `AddPrestige` credits both (V8.1) + raises `OnPrestigeChanged(newBalance, delta)`;
      `SpendPrestige` → `bool` atomic (V8.2); `GrabManifestData` seeds the wallet (which also zeroes
      both tallies, so the old manual `PrestigeEarned = 0` lines in `SetupBattleManagerData` are gone);
      `ResetBattle` reseeds 0 + zeroes ledger anchors (V8.3); event + raiser added to EventManager
      Battle Flow region (V8.4).
- [x] **V11** `ScenarioManifest`: parameterless ctor, `[JsonConstructor]` + 16-param ctor DELETED
      (Q1 option 1; `MapStandardTests` helper rewritten as an object initializer). Eight new fields
      per V11.2. `IsValid()` per V11.3 — all-zero thresholds VALID; a declared ladder must be
      0 < minor < major < decisive ≤ 1 (PARTIAL declarations refused — a zero minor cut grades any
      share MinorVictory, the C1 degenerate shape). `BattleResult` now rename-frozen (§2.11).
- [x] **V11.5** throwaway values in both shipped manifests (stipend 20 / rate 0.05 / bonus 0.5 /
      multiplier 1.25 / thresholds 0.55–0.65–0.8 / requiredResult MinorVictory). Inert on today's
      zero-value Khost map — the C1 guard path is what they exercise. NOT balanced.
- [x] Tests: NEW `VictoryLedgerTests` (7), `PrestigeWalletTests` (9), `ScenarioManifestTests` (10 —
      incl. pre-V11 JSON → defaults → still valid, and requiredResult persisting BY NAME).
- [x] **V16.2** `MapFixtures.cs` (`UniformMap`/`At`/`SetVictory`) — new tests build maps here; no
      fifteenth private fixture.
- [x] ⚑ **GATE 1** — ✅ GREEN 2026-08-17 (Bob ran it, incl. the Khost load-and-play check).
- [x] **V11.7 relay written** — `PrestigeVictory_Response_to_EditorAgent_2026-08-17.md` (repo root;
      Bob is the courier): the eight JSON names + casing, the STRICTER two-state threshold rule for
      their E8 dialog, Q1/Q2 confirmations, the negative-victoryValue ruling, and spec corrections
      C1–C3 so their docs don't describe the pseudocode.

## Stage 2 — Stronghold derivation (the risky stage) → ⚑ GATE 2

- [ ] **V1** `HexTile.IsStronghold` (`[JsonIgnore]`, derived): MajorCity ∥ MinorCity ∥ IsFort ∥
      IsAirbase ∥ IsPort. Comment names the single source of truth (house precedent: `IsRiver`,
      `ProjectsZoC`).
- [ ] **V2** tombstone `IsObjective` (prose, not `[Obsolete]`) at `HexTile.cs` + the two reader sites
      (`HexGridRenderer.cs:696`, `Prefab_TerrainPanel.cs:234`). JSON key untouched (V2.3). Leave
      `SetIsObjective` for `BoardAnalysisTests` until the V15 rip.
- [ ] **V3** `TerritoryService` :85/:94/:119 → `IsStronghold`. `FlipTo` and the neighbor enumeration
      untouched (V3.2). Rename per Q2 incl. doc comments. ⚠ `MovementController:1469` prestige award
      stays until Stage 3 — Stage 2 must not change prestige flow.
- [ ] **V4** `RegionGraph:229` → `StrongholdCount` from `IsStronghold`; `VictoryValue` summed over
      EVERY hex, no gate. Zero non-test readers of the old fields (verified). Sync note owed to
      `Claude_AI_TODO.md` — Region metadata semantics changed under the AI's feet.
- [ ] **V13** `Prefab_CityIcon`: add the `SV` arm (Stage 2 is what promotes the miss from latent to
      routine — folded here, not "any time").
- [ ] **V17** code side: sprite resolution before `Instantiate`; missing theme art → warn-and-skip.
- [ ] Tests: `TerritoryServiceTests` :55/:83/:114 `IsObjective = true` → `SetTerrain(MinorCity)`
      (movement cost identical, verified) + rename fallout + assertion nouns; `BoardAnalysisTests`
      :177-190 rewritten against the V4 split. New: stronghold-derivation truth table.
- [ ] ⚑ **GATE 2** — suite run. Khost goes 12 → 36 sticky hexes; Bob eyeballs feel in play.

## Stage 3 — Income switches over (no gate)

- [ ] **V7** income in `ProcessUpkeep`, player-side only (matches the ratified scripted-only-AI-economy
      ruling): stipend + `PlayerValue × rate` + high-water progress bonus (V7.2); `double` throughout,
      `Math.Round` ONCE (V7.3); `CurrentLedger` refreshed here for UI/debug.
- [ ] **V3.3** delete the capture award (`MovementController:1469-1470`); KEEP the printer dispatch +
      `ObjectiveCaptured/Lost` SFX. `CaptureObjective()`/`LoseObjective()` calls replaced by the
      ledger-content UI message (V6.3).
- [ ] **V6** retire the counters: the three fields, `InitializeObjectivesFromMap` (call site :358 →
      the Stage-1 `StartingPlayerShare` capture), `SetTotalObjectiveHexes` + `UpdateObjectiveStatus`
      (zero callers), `ResetBattle`'s counter lines. ⚠ ALL THREE are persisted (`GameDataObjects`
      :108-110, incl. `objectiveHexesUnoccupied` — v2 doc said two; verified three). Snapshot fields
      drop in Stage 5's bump; until then the DTO keeps writing defaults, which is harmless.

## Stage 4 — Scoring + win condition + doc amendments → ⚑ GATE 3

- [ ] **V9** `CompleteBattle` grades: mirror around `StartingPlayerShare` (V9.1), switch-expression
      ladder (V9.2), **C1 no-scoring guard first**, degenerate cases log never throw (V9.3), full
      arithmetic logged for Bob's tuning (V9.4).
- [ ] **V10.1** all-objectives rule deleted; nothing-further-to-gain early end via **C2 fresh
      compute** + C1 guard.
- [ ] **V10.2** voluntary early finish once `requiredResult` met; bonus per **C3** (live ledger, no
      stored field). `public void OnEndScenarioButton()` on `DefaultDialog_Scene1` — Bob wires
      onClick (CLAUDE.md §2.13; name is a contract); the `requiredResult`-met guard lives INSIDE the
      callback per the `CanEndTurn` precedent (§3.6b).
- [ ] **V10.3** farming note recorded in `todo.md` Phase 2 (goes live with campaign carryover; not
      solved here — by design).
- [ ] **C4 — design-doc amendments** (`HS_DesignDoc.md`): §4.7.2 (flag → derived stronghold +
      ungated value), §6.13.8 (exemption keys on `IsStronghold`), §17.5.2/.3 (capture no longer
      credits prestige immediately), §18.2.1/.2 (REPLACED: per-turn income = stipend + rate × held
      value + high-water bonus; early-finish rule), §17.7 cross-ref. §17.2/17.3 already anticipate
      %-of-total thresholds — reconcile wording, no reversal. Each amendment dated + ratified per
      house style.
- [ ] New tests: `Grade()` all eight rungs, s0 at/off 0.5, boundary equality at each cut, C1 guard;
      early-finish bonus arithmetic; high-water pays once (lose → retake → no double bonus).
- [ ] ⚑ **GATE 3** — suite run + play: battles stop always ending `Draw`.

## Stage 5 — Persistence (no gate; suite piggybacks on GATE 3 if stages land together)

- [ ] **V12.1** persist: `CurrentPrestige`, `PrestigeEarned`, `PrestigeSpent` (DTO fields already
      exist on `ScenarioData` :98-100 — never mapped; wire them), `StartingPlayerShare`,
      `HighWaterVictoryValue` (both NEW fields — cannot be recomputed). **Not** the ledger (V12.2).
      Drop the three objective-counter fields (V6.4).
- [ ] **V12.3** `SAVE_VERSION` 6 → 7, NO migration arm (pre-1.0 clean break, CLAUDE.md §2.12): dated
      paragraph at `GameData.cs` incl. the Q5 why-it-didn't-ride-with-AI2b-3 record; extend the
      deliberately-no-arm list at `SnapshotMapper.cs:728-732`.
- [ ] Round-trip test: save → load → recomputed ledger matches, anchors restored, `SpendPrestige`
      state survives.

## Stage 6 — Spend sink = `todo_profiles.md` P4 (⚑ GATE 4 lives there)

V14 is P4's upstream and its interface ships in Stage 1 (V8.1/.2/.4). Prices already exist
(`WeaponProfile.PrestigeCost`, 20–450). ⚠ Income rates + thresholds CANNOT be tuned until P4 exists —
everything shipped before then is a placeholder and is labeled as such.

---

## Follow-ups logged (not this pass)

- Control flag on flipped non-city strongholds (city prefab only today) — small render item, Bob's call.
- `MapIconType` has no `Port` member — a port on open ground is an invisible stronghold (fine on
  Hamburg: all three ports sit on city terrain).
- **ART (Bob):** `EU_Airbase` / `EU_Fort` / `EU_Sprawl` sprites + `SpriteManager` registration —
  Hamburg blocker for icons, not for code (V17 warn-and-skip degrades it to a quiet gap).
- **V15 rip:** delete `IsObjective` + `SetIsObjective`, repoint the two UI readers at a manifest
  mission-objective list, `SAVE_VERSION` bump, tell the editor to stop writing the key. Trigger:
  manifest gains the objective list (Phase 2 / briefing work).
- **V11.7 relay:** send the editor the final JSON field names + casing the moment Stage 1 lands
  (their E8 is gated on it; until then editor-authored manifests carry no scoring keys — harmless
  by V11.3, not a bug).

## Housekeeping

- [ ] Move `PrestigeVictory_Handoff_to_GameAgent_2026-08-17.md` (AM, superseded) → `_to_delete/`.
