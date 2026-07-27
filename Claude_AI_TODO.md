# Claude AI TODO — Hammer and Sickle (AI track)

> **Authoritative design doc:** `C:\Users\coder\Desktop\AI_TODO\Design Docs\Supplements\AI-Design-Supplement.md`
> (architecture RATIFIED 2026-07-07; layer specs PROPOSED-DETAILED, pending Bob's read-through).
> `HS_DesignDoc.md` §23 is a pointer stub; cross-system rule changes amend the main doc at their
> home sections as they ratify. This file carries the REAL planning detail for the AI track;
> `Claude_TODO.md` gets only brief one-line sync notes.

> **⚠ AGENT REMINDER — same rules as the main TODO.** Challenge design-doc contradictions before
> implementing; the agent CANNOT run Unity tests — say **"Please run Unity Test Runner for me"** and
> wait for Bob before marking anything GREEN. Keep EditorTests loop-light (CLAUDE.md §2.9); the AI
> harness (AI10) is a separate headless runner, not an EditorTest suite.

**Legend:** `[ ]` todo · `[~]` in progress · `[x]` done · `[-]` deferred/dropped

---

## Status

> **⟳ 2026-07-07 (session end / restart point).** AI0 ✅ AI1 ✅ AI2a ✅ AI2b-sweep ✅ GREEN; AI2b
> WIRING LANDED (BattleManager.AIPerception property + ProcessRefresh AI-side branch — needs a
> compile check + play sanity next session). ⏭ NEXT: **AI2b-3** — snapshot serialization of
> AIPerceptionState (GameStateSnapshot DTOs per CLAUDE.md §15.2, SAVE_VERSION bump, round-trip
> test), review SetupBattleManagerData for a Clear/initial-sweep need, main-doc §12.9 two-sided
> note; then **AI2c** (influence fields + overlays), then AI3 (tactical executor — needs the M13
> headless move-order ask). ⚠ Bob flagged a SPOTTING-LEVEL rework pass (main TODO) — any §12
> change must update both sweeps + StepDecay together.

> **⟳ 2026-07-07 — DESIGN PASS 1 COMPLETE.** Architecture + five rulings RATIFIED (see supplement
> decision log): Option-B information model (symmetric spotting + dial-ready belief layer),
> scripted-only AI economy, hints-bias-never-bind .aii, cheat ladder mandated (rungs PROPOSED),
> **irregular doctrine = v1 REQUIREMENT** (Khost's MJ must fight as ambush-web irregulars —
> build order adjusted: irregular manager BEFORE conventional line manager). All layer parts now
> PROPOSED-DETAILED in the supplement.
> ⏭ NEXT: Bob reads the supplement → ratify/red-pen the detailed parts (esp. Q2 posture vocabulary,
> Q9 own-exposure reading, R0–R4 rung definitions). Implementation may start at AI0 (EV oracle) —
> it has no open design dependency.

---

## D — Design gates

- [x] **D1 — Information model (Q1).** RATIFIED 2026-07-07: Option B. Consequences: SpottingService
      symmetric sweep, `AIPerceptionState` store (NOT on CombatUnit), ghosts/threat fields,
      serialize-in-snapshot (Q6 agent-ruled) → SAVE_VERSION bump when AI2 lands.
- [x] **D2 — Architecture + layer boundaries.** RATIFIED 2026-07-07 (direction): 5 layers + S1/S2,
      no RL, build-order-as-degradation-order, ORDER-API-ONLY rule (supplement 1.3). ⚠ M13
      consequence: headless move-order path must be extracted from MovementController (input layer
      and AI both call it) — flag when M13 planning starts.
- [ ] **D3 — Posture vocabulary (Q2, Part 6.1).** DRAFTED in detail (DefendForward / DefendMain /
      Delay / Screen / Attack / Counterattack / Harass / Reserve + personality axes) — ratify on
      Bob's read-through. Blocks AI8 (.aii loader), not earlier milestones.
- [x] **D4 — AI economy (Q3).** RATIFIED 2026-07-07: scripted-only v1. No wallet; .oob schedules;
      facility policies; purchase hooks dormant. `.aii economy{}` = stipend escape hatch only.
- [~] **D5 — .aii format.** Precedence RATIFIED 2026-07-07 (hints bias, never bind; expiring locks
      flagged). Schema DRAFTED (Part 11.2) — freeze at AI8 start.
- [ ] **D6 — Cheat-ladder rungs + difficulty mapping (Q5, Part 12.3).** Ladder RATIFIED; rung
      definitions R0–R4 PROPOSED. Ratify definitions now; per-difficulty enablement can wait for
      balancing (§11.10.4 amendment when any R2+ rung ties to difficulty). ENGINEERING CONSEQUENCE
      for AI2: belief layer built DIAL-READY (decay rate, region-truth injection, belief := truth)
      — rungs are config, not code forks.
- [ ] **D7 — Reaction-module risk model (Part 10.2).** EV−λ·loss shape DRAFTED; λ per personality/
      difficulty; v1 stub = λ→∞ config sharing the production interface. Ratify with D6.
- [ ] **D8 (NEW) — Q9 own-exposure reading.** AGENT-RULED yes (AI may read its own units'
      player-side SpottedLevel — drives irregular displacement + evac timing). Veto window open.

## AI0 — S1 EV Oracle (supplement Part 5) — ✅ GREEN 2026-07-07 (Bob ran Unity Test Runner, all suites)

> **Q7 RESOLVED (with code in hand):** no new builder layer needed — ALL CombatResolver lane
> builders are already public (`BuildForwardLane`/`BuildReturnLane`/`BuildIndirectForwardLane`/
> `BuildCounterBatteryLane`/`BuildAirStrikeLane`/`BuildAirDefenseFireLane`/`BuildAmbushLane` +
> the `Build*Stand` builders). The oracle consumes the SAME `LaneInput`/`StandValueInput` structs
> the engine rolls; higher AI layers build specs via those builders → forecasts price exactly what
> execution fires. Resolvers unmodified. Files: `Models/AI/Pmf.cs` + `Models/AI/CombatOracle.cs`
> (namespace `HammerAndSickle.Models.AI`), tests `EditorTests/CombatOracleTests.cs` (12).

- [~] Exact PMF math — DONE (pending test run): `Pmf` (exact convolution/transform, no sampling);
      `ForecastLane` mirrors `ResolveLane` line-for-line (command mitigation, band shift, float
      stack order, round-half-up points, terrain+crossing convolution, balance mod, connecting-hit
      floor); miss chance = natural-0 mass.
- [~] Downstream — DONE (pending test run): `ForecastStand` (1d10 split over damage-marginalized
      SV via `StandCheck.ComputeStandValue` — player leader terms read honestly);
      `ForecastDefenderFate` (full RetreatResolver tree: kill-by-fire, retreat/rout, Static
      collapse §7.9.7, Surrender Check + survival-loss death §7.9.6a, shatter extra-damage/
      quit-field/surrender §7.9.6 — reports PDestroyed/PVacatesHex/PQuitsField/PStaysInHex);
      `ForecastDirectEngagement` (+ return-lane kill odds, ceil-HP semantics); §7.15 degradation
      odds passthroughs. Retreat-path validity is a caller-supplied bool (`DefenderFateContext`).
- [x] No UnityEngine dependency — Pmf/CombatOracle are pure C# (AppService + Models.Combat only).
- [-] `SectorExchangeRate` aggregate — DEFERRED to AI8 (L3 rollout fuel; no consumer until then).
- [~] EditorTests `CombatOracleTests` (12) — hand goldens (uniform Even lane, terrain floor,
      round-half-up gaps, command mitigation, stand split, surrender math, shatter-quit,
      return-fire kill odds, degradation tables) + 3 EXHAUSTIVE drift guards enumerating every
      dice combo through the REAL `CombatEngine.ResolveLane` (16/576/32 combos — exact match, not
      statistical). ⚠ The enumeration helper uses bounded loops (≤576 engine calls) — deliberate
      exception to the no-loops guideline; these ARE the drift guard. Flag if unwanted.
- [x] ✅ GREEN 2026-07-07 — Bob ran Unity Test Runner: `CombatOracleTests` (12) + existing suites
      all pass. Bounded-loop drift guards accepted.

## AI1 — L1 Board Analysis (Part 4) — SLICED: AI1a code complete 2026-07-07 (⚠ pending test run), AI1b next

### AI1a — ✅ GREEN 2026-07-07 (Bob, after the 10×10-min fixture fix) — mobility + regions + chokepoints (`Models/AI/`: MobilityMap, RegionGraph, ChokepointAnalysis, BoardAnalysis + `BoardAnalysisTests` 10)
- [~] Mobility: `MobilityMap.GroundStepCost` (unit-agnostic mirror of HexMapUtil.ComputeStepCost's
      terrain/river/road rules — ⚠ DRIFT NOTE in code; keep in sync if movement rules change) +
      `EdgeHasRiver`/`EdgeHasBridge` + multi-source `GroundDistanceField` (Dijkstra). Air needs no
      field (flat 1 MP → hex distance). Rail EXCLUDED (strategic mode, not tactical mobility).
- [~] Region graph: flood fill by terrain class (Open/Broken/Mountain/Urban; Water/Impassable =
      barriers) over TRAVERSABLE adjacency (unbridged rivers split banks — emergent); undersized-
      fragment merge (majority-vote class, deterministic ids); edges with ConnectionWidth/RiverPairs/
      BridgePairs/RoadLink (edge requires ≥1 traversable pair); region metadata (objectives/VP,
      roads, fort/airbase-site/port, centroid seed for .aii references).
- [~] Chokepoints: articulation hexes (Hopcroft–Tarjan over the traversable graph) + all bridged
      crossings. `BoardAnalysis.Build(map)` orchestrator; rebuild triggers documented (bridge/fort
      events — wiring lands with AI2/AI3 integration).
- [ ] ⚠ PENDING: Bob runs Unity Test Runner (`BoardAnalysisTests`, 10 — synthetic strip/field maps).

### AI1b — avenues + ambush catalog — CODE COMPLETE 2026-07-07, ⚠ PENDING Bob's Unity Test Runner
- [~] `AvenueAnalysis.FindAvenues` (`Models/AI/AvenueAnalysis.cs`): k diverse corridors, multi-source
      Dijkstra to first-reached target, diversity via flat REUSE_PENALTY(2)/hex on found paths,
      identical-repeat dedupe; Avenue = path + true (unpenalized) cost + CoverFraction. AD-exposure
      rating joins at L2 (needs unit data) as designed.
- [~] `AmbushSiteCatalog.Build` (`Models/AI/AmbushSiteCatalog.cs`): covered occupiable hexes flanking
      each avenue; TriggerHex = earliest flanked path hex (§6.9.1 geometry), PathAdjacency (kill-zone
      breadth), HasDisplaceRoute (exit clearing the avenue's adjacency halo), Score = cover×2 +
      breadth + displace bonus (tunable). Terrain-only — class eligibility (§6.9.9) + spotting
      exposure are AI4/AI2 concerns.
- [~] `AvenueAndAmbushTests` (6): single-corridor dedupe, fork divergence under penalty, cover
      fraction, site w/ trigger+displace, no-cover→no-sites, boxed-in→no-displace.
- [-] Defensive-trace ladder — MOVED to AI5 (its consumer, the conventional line manager; same
      pattern as SectorExchangeRate→AI8). Supplement Part 4.5 design unchanged.
- [-] Debug overlays — MOVED to AI2/AI3 integration (need the renderer running in-scene).
- [-] Khost-map smoke assertions — deferred to the AI10 harness (map-load path).
- [ ] ⚠ PENDING: Bob runs Unity Test Runner (`AvenueAndAmbushTests`).

## AI2 — L2 Perception & Influence (Part 3) — SLICED; AI2a code complete 2026-07-07 (⚠ pending test run)

### AI2a — belief store (`Models/AI/AIPerceptionState.cs` + `AIPerceptionTests` 8)
- [~] `AIPerceptionState`: ContactRecords (AI-side SpottedLevel mirror, off CombatUnit), §12.4
      incremental RecordSpot (cap L4) + RefreshContact, §12.6-exact StepDecay (in-range holds;
      L2+→L1; L1→ghost), GhostContact (uncertainty radius = turns-lost × est. MP; lifetime cull),
      re-acquire resets (§12.6.6), RemoveUnit (watched kill = no ghost), Clear.
      DIALS LIVE: DecayGraceTurns (R2), BeliefIsTruth (R3 — store keeps running so the dial can
      turn back mid-campaign), GhostLifetimeTurns. Own-exposure (Q9) = read CombatUnit.SpottedLevel
      directly, no code needed.
- [ ] ⚠ PENDING: Bob runs Unity Test Runner (`AIPerceptionTests`, 8).

### AI2b — SpottingService symmetric sweep — SWEEP CODE COMPLETE 2026-07-07, ⚠ PENDING Bob's Unity Test Runner
- [~] SpottingService gains `RecomputeAIPerception(perception, turn)` + `StepAIPerceptionDecay`
      (new "AI-Side Perception" region, PURELY ADDITIVE — zero changes to player-side code paths):
      AI spotters vs player units under the same private `SpottingRangeAgainst` (dual-domain §12.3;
      camouflage symmetric), hits feed the belief store — CombatUnit.SpottedLevel never touched.
      MP-per-turn ghost estimate = MovementPoints.Max; HP read = rounded percent.
- [~] `AIPerceptionSweepTests` (3, BaseTestFixture harness with sides swapped): in-range feeds
      belief-not-CombatUnit, out-of-range no contact, lost contact → ghost at last-known pos.
- [ ] REMAINING AI2b-2: ownership/lifecycle (who owns the instance — BattleManager vs AI driver;
      Clear on load; RemoveUnit on destruction events); snapshot serialization (GameStateSnapshot
      DTOs per CLAUDE.md §15.2, SAVE_VERSION bump, round-trip test); call-site wiring at AI_Refresh
      (BattleManager.ProcessRefresh isPlayerSide:false); main-doc §12.9 two-sided note.
- [ ] ⚠ PENDING: Bob runs Unity Test Runner (`AIPerceptionSweepTests` + `SpottingServiceTests`
      regression — the touched file's existing suite).

### AI2c — influence fields + overlays (after AI2b)
- [ ] Influence fields: known/ghost threat, per-axis power projection, ambush-risk, supply-security,
      objective gravity; region-level force estimates for L3; R2 region-truth injection hook.
- [ ] Debug overlays (regions/avenues/sites/influence) on HexGridRenderer utility layers.

## AI3 — L5 Tactical Executor (Part 9) — first SHIPPABLE AI (greedy baseline)

- [ ] Utility framework (considerations, response curves, noise temperature = R0 knob; weights
      .aii/difficulty-addressable).
- [ ] Order-API integration (1.3): needs the M13 headless move-order path — COORDINATE with M13.
- [ ] Discipline set: facing (threat-bearing), reactive-facing policy (free once/enemy turn —
      quick-call via S2 interface), entrench-when-idle, resupply timing (§15.4a + gates), Degraded
      rest, mount/dismount doctrine (DEP_MOB), ambush-aware pathing (belief 3.5).
- [ ] AI-turn order of operations (9.1) in the M13 driver (replaces placeholder dwell).
- [ ] Per-class action-economy compliance (§8.5.8).

## AI4 — L4 Framework + IRREGULAR Web Manager (Parts 7 shared machinery + 7b) — KHOST PRIORITY

- [ ] Task/assignment framework (bids: travel + suitability + disruption; greedy+swaps; hysteresis)
      — shared by 7/7b/8.
- [ ] Ambush-web construction from L1 site catalog (eligibility per §6.9.9 — AAA yes, SAM/ART no;
      unspotted approach routing via own-exposure + belief).
- [ ] Fire discipline/displacement (shoot-and-scoot on ambush-fired or exposure ≥ Level 2);
      web self-healing; must-hold objective exceptions.
- [ ] Raid tasking (target scoring from belief/ghosts, escape-route weighting) — uses the Part 8.6
      Raid template (built here, reused by AI7).
- [ ] Melt-away thresholds + dormancy (personality knobs).
- [ ] Harness assertions (once AI10 exists): "cell displaces within 1 turn of Level-2 exposure."

## AI5 — L4 Conventional Line Manager (Part 7)

- [ ] Slot generation (avenue-crossing primaries, interlock verification, depth, ART/AD/recon
      support slots, junction reserves).
- [ ] Maintenance: rotation-before-Degraded, gap-fill/contract, flank refusal, entrench cadence,
      supply/HCL watch → pre-emptive Delay, slot facing.
- [ ] Delay execution (leapfrog down the trace ladder; artillery displaces first; ambush-class
      rear cover).

## AI6 — S2 Reaction Module (Part 10) — gated on D6/D7; hosts = M13 ReactionWindowController

- [ ] EV−λ·loss framework; per-consumer forms: interception windows, AIB, CB (default-on w/
      exceptions), bombardment evac, reactive-facing quick-call.
- [ ] AirThreatService footprint reuse (shared with player §24.7a.8 overlay — build once).
- [ ] Hot-swappable policy object; v1 decline-all = λ→∞ config on the SAME interface (ship the
      interface with M13, swap the brain in here).

## AI7 — L4 Offense (Part 8)

- [ ] Package/plan objects (unit reservation, per-step preconditions/progress/abort).
- [ ] Templates: DeliberateAttack (recon-fix → air/AD prep → arty prep w/ CB-risk → sequenced
      breach sized by S1 CHAIN P(vacate) target → exploit w/ AA chains → consolidate), HastyAttack
      (counterattack punishment), Encircle (min-cut of supply-trace graph → pincers + HCL clock),
      Raid (from AI4).
- [ ] Schwerpunkt + fixing-attack selection (S1 aggregates; §7.11.2 exploitation).
- [ ] Air integration via 9.3 (ASB/SEAD/RB; AAB/SB reserved out of v1 — Q10).

## AI8 — L3 Strategic + .aii Loader (Parts 6, 11) — gated on D3, D5 freeze

- [ ] Candidate-plan generation (templates over region structure, .aii-biased, 4–8 survivors).
- [ ] Coarse rollout sim (region-level force flow via SectorExchangeRate; supply-trace checks;
      VP scoring; softmin across 2–3 player-intent assumptions).
- [ ] Commitment/abort/hysteresis + ε-sampling (R0 unpredictability).
- [ ] .aii JSON loader (schema Part 11.2; System.Text.Json rules CLAUDE.md §15.2 — every property
      `[JsonPropertyName]`), Khost .aii authored, group→doctrine assignment.
- [ ] Difficulty wiring (economy base + R0 axes; §23.5 disclosure text; §11.10.4 amendment if any
      R2+ rung ties to difficulty).

## AI9 — Cheat-Rung Dials (Part 12.3) — cheap by construction; gated on D6

- [ ] R2: belief-decay knob + region-truth injection at L3; R3: belief:=truth + targeting decorum
      filter at the order layer; R4: per-side balance-mod hook consts (documented break-glass,
      shipped OFF). R0/R1 already exist via AI3/AI8 knobs + .oob/.aii authoring.

## AI10 — Headless Harness + Tuning (Part 13)

- [ ] Headless battle driver (no Unity scene; pure resolvers + AI stack; seeded; scenario-loaded).
- [ ] Behavior assertions + exploit probes (scripted bait patterns); regression suite.
- [ ] Doubles as M13 balance rig (§7.9.5.1 distribution targets). Optional: weight-vector search.

---

## Coupling with the main TODO (`Claude_TODO.md`)

- **M13**: (a) AI-turn driver + reaction yields + AOB framework are M13 deliverables this track
  consumes; (b) NEW ASK — headless move-order path extracted from MovementController (D2
  consequence, 1.3); (c) v1 stub = decline-all λ→∞ config on the S2 interface (ship interface
  with M13); (d) AI10 harness is the balance rig; (e) EV goldens track combat-const changes.
- **SpottingService**: symmetric sweep lands with AI2 (§12.9 doc note then).
- **SAVE_VERSION**: bump with AI2 (AIPerceptionState in snapshot).
- Sync-note rule: one line in main TODO when a milestone opens/ships or forces a main-doc amendment.

---

## Change log

**Rules:** one line per change · newest first · `YYYY-MM-DD — imperative summary (area)`.

- 2026-07-07 — FIX AIPerceptionSweepTests: pin PlayerTarget SpottedLevel to Level0 at setup (CombatUnit ctor doesn't default to Level0 — same convention as SpottingServiceTests.Target); sweep code unchanged, the non-interference assertion now proves what it claims. ⚠ PENDING re-run (Tests)
- 2026-07-07 — AI2a GREEN (Bob); AI2b sweep code: SpottingService +RecomputeAIPerception/+StepAIPerceptionDecay (additive region; AI spotters → belief store via the same dual-domain SpottingRangeAgainst; player-side paths untouched) + AIPerceptionSweepTests (3). Remaining AI2b-2: ownership, snapshot+SAVE_VERSION, AI_Refresh call-site, §12.9 note. ⚠ PENDING Bob's Unity Test Runner incl. SpottingServiceTests regression (Services/Tests)
- 2026-07-07 — AI1b GREEN (Bob); AI2a code: add `Models/AI/AIPerceptionState.cs` (belief store — AI-side SpottedLevel contacts, §12.6-exact decay, ghost lifecycle w/ uncertainty growth, R2/R3 dials live, own-exposure needs no code) + `AIPerceptionTests` (8). AI2b (SpottingService sweep + snapshot, production code) and AI2c (influence + overlays) sliced. ⚠ PENDING Bob's Unity Test Runner (Models/AI/Tests)
- 2026-07-07 — AI1a GREEN (Bob); AI1b code: add `Models/AI/AvenueAnalysis.cs` (k-diverse avenues via penalty reruns, true-cost + cover rating) + `AmbushSiteCatalog.cs` (covered flanking sites w/ §6.9.1 trigger geometry, kill-zone breadth, shoot-and-scoot exits, scoring) + `AvenueAndAmbushTests` (6). Trace ladder→AI5, overlays→AI2/AI3, Khost smoke→AI10 (consumers own their tools). ⚠ PENDING Bob's Unity Test Runner (Models/AI/Tests)
- 2026-07-07 — FIX BoardAnalysisTests fixtures: HexMap enforces 10×10 minimum dimensions (ctor throw) — synthetic strips/fields now live on a 12×12 canvas with only the tiles under test populated (ctor is dictionary-backed, no prefill; unset = null = off-map to the modules). All 10 tests were failing on the ctor guard, zero module code changes. ⚠ PENDING re-run (Tests)
- 2026-07-07 — AI0 GREEN (Bob ran all suites); AI1a BOARD ANALYSIS code: add `Models/AI/MobilityMap.cs` (unit-agnostic ground step costs mirroring HexMapUtil rules + river/bridge edge checks + multi-source Dijkstra field), `RegionGraph.cs` (terrain-class flood fill over traversable adjacency, fragment merge w/ majority class, edges w/ width+river/bridge/road texture, objective/infra metadata, deterministic ids), `ChokepointAnalysis.cs` (articulation hexes + bridged crossings), `BoardAnalysis.cs` (orchestrator + rebuild-trigger doc) + `BoardAnalysisTests` (10, synthetic maps). AI1b (avenues/traces/ambush catalog/overlays) = next slice. ⚠ PENDING Bob's Unity Test Runner (Models/AI/Tests)
- 2026-07-07 — AI0 EV ORACLE code: add `Models/AI/Pmf.cs` (exact integer PMF: dice factories, convolution, pointwise transform, queries) + `Models/AI/CombatOracle.cs` (`ForecastLane` = analytic mirror of `CombatEngine.ResolveLane`; `ForecastStand`; `ForecastDefenderFate` = full RetreatResolver fate tree → PDestroyed/PVacatesHex/PQuitsField/PStaysInHex; `ForecastDirectEngagement`; §7.15 odds passthroughs) + `CombatOracleTests` (12: hand goldens + 3 exhaustive engine-enumeration drift guards). Q7 resolved: consume LaneInput/StandValueInput directly, lane builders already public, resolvers untouched. SectorExchangeRate deferred to AI8. ⚠ PENDING Bob's Unity Test Runner (Models/AI/Tests)
- 2026-07-07 — DESIGN PASS 1: supplement fleshed to PROPOSED-DETAILED across all parts; RATIFIED Option-B info model, scripted-only economy, hints-bias .aii, irregular doctrine as v1 requirement; milestones renumbered AI0–AI10 (irregular web manager pulled ahead of conventional line manager for Khost); D-gates updated (D1/D2/D4 closed, D5 partial, D8 added) (AI-Design-Supplement/Claude_AI_TODO)
- 2026-07-07 — Cheat ladder RATIFIED into supplement Part 12 (Bob's Plan-B mandate): hooks designed in, rungs R0–R4 PROPOSED ranked by player-trust cost; Q1 note strengthened (ladder requires Option B substrate); D6 rescoped (planning only, no code)
- 2026-07-07 — AI track opened: AI-Design-Supplement.md created (architecture PROPOSED, Q1–Q8 open); this TODO scaffolded; HS_DesignDoc §23 pointed at the supplement; sync note added to Claude_TODO (planning only, no code)
