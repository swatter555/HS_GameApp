# todo_prestige.md — Victory Scoring, Prestige Economy & Objective Obsolescence (V-pass)

> **✅ PASS CLOSED 2026-08-17 — all five stages GREEN (suites AND play), SAVE_VERSION 7 shipped.**
> Final run: full suite green + the GATE 3 play checks (stamped flags identical, capture pays
> nothing, income at Upkeep, grade log correct). Khost runs SAFELY in its placeholder state — the
> shipped map carries 1550 victory value over 36 hexes, so scoring is LIVE with placeholder numbers
> (not dormant as the editor's report assumed); every degenerate state has a tested guard.
> **Downstream:** P4 requisition (`todo_profiles.md`, = V14 → GATE 4) now has a live currency ·
> Bob wires the End Scenario button · the editor's rebalanced maps + authored missionObjectives
> supersede the placeholders · design-doc amendments are IN (C4 done, §17.8/§17.9 new).

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
- **C5 — Early finish gates on the THRESHOLD LADDER, never on `requiredResult` (editor's catch,
  2026-08-17 PM reply; Bob-endorsed).** `requiredResult = Draw` IS the defensive scenario ("hold the
  line for 21 days") — but a defensive scenario GRADES Draw at its starting share by construction, so
  a requiredResult-met gate lights the cash-out button on TURN 1 and pays ~20 unused turns of bonus
  for doing nothing. The gate is `VictoryThresholdMinor > 0f && PlayerShare >= VictoryThresholdMinor`
  — the player has achieved an actual VICTORY. Offensive: lights at Minor, push-or-cash-out choice.
  Defensive/withdrawal: never lights; playing to the limit IS the mission. ⚠ The `minor > 0` half is
  OURS, not the editor's: their formulation (`share >= minor` alone) re-opens the C1 hole for a
  no-scoring-declared scenario paying a stipend — minor = 0 → `share >= 0` → turn-1 button again.
  `requiredResult` keeps briefing text + campaign branching ONLY; all rungs except `Ongoing` are
  legal (`IsValid` refuses the sentinel — built with Stage 1b). (C6 adds a third term to the gate:
  every mission objective held.)
- **C6 — THE MISSION-OBJECTIVE GATE (Bob, ratified 2026-08-17 late; partially supersedes the
  "isObjective is obsoleted" premise — REDEFINES it rather than reversing it).**
  *Why:* share-based scoring alone lets the player avoid fortified strongpoints and farm weak
  sectors. Every scenario authors ≥ 1 mission objective (defensive: on the player's side, to HOLD;
  offensive: on the AI side, to TAKE; sparse — victory value keeps most of the weight; authoring
  convention, not a schema requirement).
  *The rule:* **the player cannot reach the scenario's `requiredResult` until ALL mission objectives
  are Red-controlled.** Gate unmet → grade capped ONE RUNG BELOW `requiredResult` (generalizes
  correctly: offensive/required-Minor caps at Draw = failed; defensive/required-Draw caps at
  MinorDefeat = failed; withdrawal/required-MinorDefeat caps at MajorDefeat. Edge: required
  DecisiveDefeat has no rung below — cap degenerates to itself; note in spec, IsValid does not
  refuse). The gate joins BOTH the C5 early-finish availability and the V10.1 auto-end as one extra
  term, and is evaluated FRESH from the map at each check — no stored gate state.
  *Storage + runtime (ratified architecture):* authored in the MANIFEST —
  `"missionObjectives": [{ "x": int, "y": int, "label": "optional" }]` (label feeds dispatches/UI;
  absent or empty = no gate, valid — same philosophy as all-zero thresholds). At scenario load,
  `MapLoader` (which already takes the manifest) **CLEARS every authored `IsObjective`, then STAMPS
  the manifest list onto the map** — the authored flag value is DEAD AND IGNORED, so stale/legacy
  maps and editor lag are harmless, and no re-export is urgent. Gameplay, UI (the existing city-flag
  wiring works unchanged) and SAVES all read the STAMPED flag. ⚠ This is REQUIRED, not convenient:
  §7.3 makes in-battle saves self-contained (the scenario may be uninstalled), so the gate cannot
  read the live manifest — the embedded map carries the objectives, and the SnapshotMapper restore
  path never re-stamps. Same snapshot doctrine as ".oob is a snapshot of CombatUnitDB": patching a
  manifest's objectives does not change an in-battle save; between-battle saves reload fresh.
  *Validation:* objective outside the map bounds → REFUSED load (G6 — manifest and map were not
  exported together; silently skipping would make the gate quietly easier). Duplicate coordinates →
  refused in `IsValid`. Objective on a NON-stronghold hex → loud WARNING only (stronghold placement
  is an authoring convention, Bob's call — an open-ground objective flips by transit and flickers).
  *Consequence:* **V15 is REDEFINED, not executed** — the flag is never ripped; it becomes a
  load-time projection of manifest data. What dies is the AUTHORED value; the editor's map-side
  objective-marking UI becomes obsolete. The Stage-2 tombstones get REWRITTEN when this builds
  (authored = dead; runtime = stamped projection; gameplay reads the runtime value).
  *Editor-status confirmations (their EOD report, 2026-08-17):* **the stamp writes THE FLAG ONLY** —
  `victoryValue` is map data and is never stamped (their reading, confirmed; two sources of truth
  for one number is the drift we keep killing). `missionObjectives` is **PER-VARIANT** — standalone
  and campaign manifests over the same `.map` legitimately stamp different hexes; never assume a
  canonical per-map set. `victoryValue` is genuinely FRACTIONAL now (their truncation bug fixed;
  float end-to-end our side, ledger accumulates double, HUD messages format `0.#`). Their authoring
  validation is deliberately STRICTER than ours (refuses multiplier == 1.0, partial ladders,
  all-zero thresholds with a victory-rung requiredResult, filler-column objectives, negative value)
  — leave our looser checks as they are; theirs run first. ⚠ **KHOST COORDINATION:** C6b's clear
  wipes the 12 authored flags — C6a MUST seed both khost manifests IN THE SAME COMMIT as the stamp
  (placeholder set = the 12 former hexes; the editor supplies authored sets with the rebalance) or
  Khost shows no flags and runs no gate, which reads as a rendering bug.

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

### Stage 1b — amendments from the editor's reply (2026-08-17 PM; ⚑ rides GATE 2, not its own run)
- [x] `IsValid()` refuses `RequiredResult == Ongoing` (the sentinel); every other rung stays legal —
      Draw = defensive, defeat rungs = fighting withdrawal (see C5).
- [x] `earlyFinishMultiplier`: kept `>= 1` — exactly 1.0 LOADS with a named log warning rather than
      refusing (our call, documented in the reply: refusal makes the scenario silently vanish from
      the menu; the editor hard-blocks <= 1 at authoring so the warning only ever fires on
      hand-edited content).
- [x] `ScenarioManifestTests` +3 (Ongoing refused · Draw/MinorDefeat legal · 1.0 loads-but-inert).
- [x] SoundEffect append-only tombstone (their §5 ask) — ALREADY EXISTS, `GameAudioManager.cs:88-102`;
      confirmed back to them, nothing added.

## Stage 2 — Stronghold derivation (the risky stage) → ⚑ GATE 2
### ✅ CLOSED 2026-08-17 — GATE 2 GREEN (suite + Bob PLAYED Khost, flips/captures/flags all correct),
### COMMITTED. Stage 1b rode along and is green with it. ⚠ Khost map itself is under revision on the
### editor side — the 36-stronghold count will change with their rebalance; the derivation is what
### was validated, not the number.

- [x] **V1** `HexTile.IsStronghold` (`[JsonIgnore]`, derived): MajorCity ∥ MinorCity ∥ IsFort ∥
      IsAirbase ∥ IsPort. Comment names the single source of truth (house precedent: `IsRiver`,
      `ProjectsZoC`) + the Hamburg airbases-on-Clear rationale.
- [x] **V2** `IsObjective` tombstoned (prose) at `HexTile.cs` + both reader sites; `SetIsObjective`
      tombstoned, kept (its one caller now asserts the DECOUPLING in `BoardAnalysisTests`). JSON key
      untouched (V2.3).
- [x] **V3** `TerritoryService` three flip conditionals → `IsStronghold`; `FlipTo` + neighbor
      enumeration untouched (V3.2). Renamed `ObjectiveCapture`→`StrongholdCapture`,
      `CapturedObjectives`→`CapturedStrongholds` + all doc comments (service + MovementController).
      `MovementController` award + counter calls kept for Stage 3, marked.
- [x] **V4** `RegionGraph`: `StrongholdCount` (derived) + `VictoryValue` (every hex, no gate).
      Sync note WRITTEN into `Claude_AI_TODO.md` Status (includes the AI2b-3 bump coordination
      outcome). Post-change sweep confirms zero stragglers of the old names.
- [x] **V10.1 (first half) PULLED FORWARD — the all-objectives instant win is neutralized HERE, not
      Stage 4.** Found during implementation: capture accounting bumps `ObjectiveHexesOccupied` per
      STRONGHOLD (36 on Khost) while `TotalObjectiveHexes` still counts the 12 AUTHORED objectives,
      so the old rule live + V3 = any 12 stronghold captures spuriously auto-win. `CheckVictoryConditions`
      now returns false with the dated why; INTERIM: no early end, battles run to the turn limit.
      Stage 4 builds the replacement; Stage 3 retires the counters.
- [x] **V13** `Prefab_CityIcon` `SV` arm added, with the why-it-was-latent comment.
- [x] **V17** `CreateMapIcon`: sprite resolves BEFORE `Instantiate`; missing theme art →
      warn-and-skip, debounced to ONE warning per (theme, iconType) per session (nine Hamburg
      airbases × every-repaint would otherwise be hundreds of log lines). UrbanSprawl no longer
      falls through to Middle-East art on other themes — it skips like the rest.
- [x] Tests: `TerritoryServiceTests` migrated to `MapFixtures` + strongholds-by-terrain (MinorCity —
      movement cost identical to Clear) + renames; NEW `IsStronghold_TruthTable`,
      `IsStronghold_IgnoresTheDeadFlagAndValue`, `Transit_ValuedNonStronghold_StillFlips`;
      `BoardAnalysisTests` V4 rewrite (fort = stronghold, dead flag contributes nothing, value
      sums ungated).
- [x] ⚑ **GATE 2** — ✅ GREEN 2026-08-17 (suite + Bob played Khost: pass-by no-flip, end-on capture
      with dispatch + SFX, clean Console, no early end). One compile fix en route: a `*/` inside a
      block comment (`EU_*/CH_*`) terminated it early — the V17 comment is line-style now.

## Stage 3 — Income switches over (no gate of its own)
### ✅ CODE-COMPLETE 2026-08-17 — new `PrestigeIncomeTests` (6) ride ⚑ GATE 3

- [x] **V7** income in `ProcessUpkeep`, player-side only. ⚠ Arithmetic lives in
      `BattleManager.ComputeIncome` — STATIC + PURE (the TurnStructureTests precedent, same reason as
      the `PrestigeWallet` extraction): stipend + rate × held value + high-water progress bonus,
      `double` accumulation, ONE `Math.Round` at the end. The three manifest knobs cached in
      `GrabManifestData` beside `DeploymentPointCap` (⚠ Stage 5 must rule on their save mirror —
      in-battle saves restore without a manifest, §7.3).
- [x] **V3.3** capture award deleted; dispatch + SFX kept. `PrinterDispatch.ReportObjectiveCaptured`
      DROPPED its prestige parameter and the "credited to the front" line — the dispatch must not
      claim a lump sum that is no longer paid.
- [x] **V6** counters retired in full: the three fields, `InitializeObjectivesFromMap`,
      `SetTotalObjectiveHexes` + counter-`UpdateObjectiveStatus` (zero callers), the `ResetBattle`
      lines. `CaptureObjective`/`LoseObjective` → `ReportStrongholdTaken`/`ReportStrongholdLost`
      (V6.3): ledger-content HUD messages ("Stronghold taken — victory value 120/240 (50%)"), fresh
      throwaway `Compute` per message — they do NOT write `CurrentLedger` (Upkeep owns it) and run NO
      victory check. `TriggerImmediateVictory` keeps zero callers until Stage 4's End Scenario
      button. DTO fields (`GameDataObjects` :108-110) drop with Stage 5's bump as planned.
- [x] Tests: NEW `PrestigeIncomeTests` (6) — stipend floor, rate income, bonus-pays-once + ratchet,
      lose-then-retake pays nothing, round-once (0.45 + 0.45 → 1), all-zero knobs.

## Stage 4 — Scoring + win condition + doc amendments → ⚑ GATE 3
### ✅ CODE-COMPLETE 2026-08-17 — awaiting the GATE 3 run (queued in Claude_TODO ⚑)
### ⚠ Deviation, deliberate: `OnEndScenarioButton` lives on **BattleManager**, not DefaultDialog_Scene1
### — the OnEndTurnButton precedent (battle-flow callback, gate inside the callback, no HUD copy).
### Bob wires the new button's onClick to BattleManager; name is a contract (§3.6b). Until wired,
### early finish is unreachable — the auto-end and grading paths work without it.
### ⚠ C6a label note: `MissionObjective.Label` is stored + round-tripped but has no game-side
### consumer yet — dispatches already name places via the map's TileLabel; the label's consumers
### arrive with the objectives HUD/briefing surfaces. Told the editor at pass close.

- [x] **C6a — manifest schema:** `missionObjectives` list + `MissionObjective` type ({X, Y, Label} —
      label optional, feeds dispatches/UI). `IsValid`: absent/empty valid; entries within the
      declared `mapWidth`/`mapHeight` rectangle; duplicates refused. (The authoritative in-bounds +
      terrain checks live in the MapLoader stamp — IsValid only knows the rectangle, not the
      odd-row overhang or terrain.) Seed both shipped manifests — Khost's 12 formerly-authored
      objective hexes are the candidate set; Bob authors the real one (map under revision).
- [x] **C6b — the stamp:** `MapLoader` clears every authored `IsObjective` after populate, then
      stamps `manifest.missionObjectives`. Out-of-bounds objective → refuse the load (G6 style);
      non-stronghold objective → loud warning. Restore path (SnapshotMapper) untouched — stamped
      flags ride the embedded save map.
- [x] **C6c — tombstone rewrite:** `HexTile.IsObjective` + `SetIsObjective` + both reader-site notes
      get the new doctrine (authored = dead/ignored; runtime = load-time projection of the manifest;
      gameplay reads the RUNTIME value). The Stage-2 "do not add readers" wording is superseded.
- [x] **V9** `CompleteBattle` grades: mirror around `StartingPlayerShare` (V9.1), switch-expression
      ladder (V9.2), **C1 no-scoring guard first**, degenerate cases log never throw (V9.3), **C6
      gate cap applied LAST** (unmet → min(shareGrade, one rung below `requiredResult`)), full
      arithmetic + gate state logged for Bob's tuning (V9.4).
- [x] **V10.1** all-objectives rule deleted (done early, Stage 2); nothing-further-to-gain early end
      via **C2 fresh compute** + C1 guard **+ C6 gate term** (auto-end must not fire at a rung the
      gate would then deny). The boundary recompute ALSO writes `CurrentLedger` (editor's C2
      addendum, accepted) so the HUD and the verdict can never disagree about the same instant.
- [x] **V10.2** voluntary early finish per **C5 + C6** — gate is `VictoryThresholdMinor > 0f &&
      PlayerShare >= VictoryThresholdMinor && allObjectivesHeld`, NEVER `requiredResult` (turn-1
      defensive cash-out exploit). Bonus per **C3** (live ledger, no stored field).
      `public void OnEndScenarioButton()` on `DefaultDialog_Scene1` — Bob wires onClick (CLAUDE.md
      §2.13; name is a contract); the gate lives INSIDE the callback per the `CanEndTurn` precedent
      (§3.6b).
- [x] **V10.3** farming note recorded in `todo.md` Phase 2 (goes live with campaign carryover; not
      solved here — by design).
- [x] **C4 — design-doc amendments** (`HS_DesignDoc.md`): §4.7.2 (flag → derived stronghold +
      ungated value + the C6 projection doctrine), §6.13.8 (exemption keys on `IsStronghold`),
      §17.5.2/.3 (capture no longer credits prestige immediately), §18.2.1/.2 (REPLACED: per-turn
      income = stipend + rate × held value + high-water bonus; early-finish rule), §17.7 cross-ref,
      **NEW §17.x — THE MISSION-OBJECTIVE GATE** (C6 in full: manifest-authored, stamped at load,
      one-rung-below-required cap, the gate term on early finish + auto-end, defensive/offensive
      placement doctrine). §17.2/17.3 already anticipate %-of-total thresholds — reconcile wording,
      no reversal. Each amendment dated + ratified per house style.
- [x] New tests: `Grade()` all eight rungs, s0 at/off 0.5, boundary equality at each cut, C1 guard;
      **C6 gate**: cap at each scenario shape (offensive → Draw, defensive → MinorDefeat,
      withdrawal → MajorDefeat), gate term blocks early finish + auto-end, no-objectives manifest =
      gate trivially met; **C6b stamp**: authored flags cleared, manifest stamped, out-of-bounds
      refused, non-stronghold warns; early-finish bonus arithmetic; high-water pays once (lose →
      retake → no double bonus). (Stage 5 adds: stamped flags survive the save round-trip.)
- [ ] ⚑ **GATE 3** — suite run + play: battles stop always ending `Draw`.

## Stage 5 — Persistence
### ✅ CLOSED 2026-08-17 — SAVE_VERSION 7; FINAL RUN GREEN (suite + play), COMMITTED.

- [x] **V12.1** persist: `CurrentPrestige`, `PrestigeEarned`, `PrestigeSpent` (DTO fields already
      exist on `ScenarioData` :98-100 — never mapped; wire them), `StartingPlayerShare`,
      `HighWaterVictoryValue` (both NEW fields — cannot be recomputed). **Not** the ledger (V12.2).
      Drop the three objective-counter fields (V6.4).
- [x] **V12.3** `SAVE_VERSION` 6 → 7, NO migration arm (pre-1.0 clean break, CLAUDE.md §2.12): dated
      paragraph at `GameData.cs` incl. the Q5 why-it-didn't-ride-with-AI2b-3 record; extend the
      deliberately-no-arm list at `SnapshotMapper.cs:728-732`.
- [x] Round-trip test: `PrestigePersistenceTests` — the full slice through `JsonPolicy.Save`,
      dropped counters proven absent from the wire, `Wallet.Restore` verbatim + negative clamps,
      stamped objective flag survives the hex round-trip (+ `IsStronghold` re-derives on restore).
- [x] **RULED (the Stage-3 open question): the 8 manifest scoring/economy knobs ARE mirrored** into
      `ScenarioData` (V11.6) — an in-battle save restores without its manifest and income/grading
      read them every turn. Mission objectives need NO mirror: they ride the embedded map's stamped
      flags (the C6 architecture paying off). Sync glue = `BattleManager.CaptureScenarioState` /
      `RestoreScenarioState`, called null-tolerantly from ToSnapshot/ApplySnapshot — deliberately
      ONLY the prestige-pass slice; full battle-state sync belongs to the unbuilt save-wiring
      feature and these methods say so.

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
- **V15 — REDEFINED BY C6 (2026-08-17 late), no longer a rip.** `IsObjective` is never deleted: it
  becomes a load-time PROJECTION of `manifest.missionObjectives` (C6b clear-then-stamp) that
  gameplay, UI and saves all read. What dies is the AUTHORED value — the loader ignores whatever the
  file says, and the editor's map-side objective-marking UI is obsolete (relayed in Response4). The
  JSON key stays in the `.map` schema per V2.3 (removal costs a format bump for zero gain).
- **V11.7 relay:** send the editor the final JSON field names + casing the moment Stage 1 lands
  (their E8 is gated on it; until then editor-authored manifests carry no scoring keys — harmless
  by V11.3, not a bug).

## Housekeeping

- [ ] Move `PrestigeVictory_Handoff_to_GameAgent_2026-08-17.md` (AM, superseded) → `_to_delete/`.
