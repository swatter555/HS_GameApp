# Claude TODO — Hammer and Sickle

> **⚠ AGENT REMINDER — challenge design-doc contradictions.** `HS_DesignDoc.md` + Appendix W are very detailed and
> heavily cross-referenced. When an instruction appears to contradict a ratified value/decision in them, STOP, verify
> against the doc, and flag it (with the counter-argument) BEFORE implementing — do not silently encode it. Settled
> points sometimes get relitigated; catching them is wanted.

> **⚙ TESTING HANDOFF (Bob, 2026-06-23).** The agent CANNOT run Unity test suites or play-test. When code is ready to
> validate, the agent says exactly **"Please run Unity Test Runner for me"** (or requests a play-test) and WAITS for
> Bob's result before marking a milestone `[x]` or proceeding. Write code + tests, verify by inspection, then hand
> off — never claim GREEN unrun.

The living work file. History lives in `Claude_TODO_Archive.md` (DONE records + pre-2026-08-20 change log) and git.

- **Authoritative spec:** `HS_DesignDoc.md` · **Rating model:** Appendix W + `WeaponTrait_Supplement.md`
- **AI design (authoritative):** `Design Docs/Supplements/AI-Design-Supplement.md` · planning detail: `Claude_AI_TODO.md` (this file gets only brief sync notes)
- **Codebase context:** `Claude_Project.md` — keep reconciled; update it in the same session that lands structural changes

**Legend:** `[ ]` todo · `[~]` in progress · `[x]` done · `[-]` deferred/dropped

---

## ⚡ CURRENT STATUS (2026-08-20 — full rewrite of this file; prior text verbatim at commit `55587d2`)

**▶ NEXT: P4 REQUISITION (`todo_profiles.md`).** The prestige/victory pass gave it a live currency
(`PrestigeWallet`, atomic `SpendPrestige`) — the buy/sell/upgrade bay API is the natural next frontier.
There is no other "start here" pointer; older ▶ arrows in `todo_profiles.md`/`todo_domains.md` headers are
history, not directions.

**Pass ledger (newest first; each closed pass's detail lives in its plan file + the archive change log):**
- **2026-08-19 — THEME-ART** (committed `55587d2`): EU + CH map icons + hex tile sets, 9-arm `CreateMapIcon`,
  CN→CH prefix fix, all three terrain arrays baked (gitignored). ⏸ In-play verify gated on first non-ME export.
- **2026-08-17 — PRESTIGE/VICTORY, CLOSED** (`todo_prestige.md`, all gates green incl. play): `VictoryLedger` +
  `PrestigeWallet` + §17.2/17.3 share grading + §17.8 mission-objective gate + §17.9 scenario end + §18.2
  per-turn income (capture awards RETIRED) + **SAVE_VERSION 7**. Khost runs fully scored on placeholders
  (editor-side E10 rebalance owed — see the ladder audit, `Reply_LadderAudit_2026-08-17.md`).
- **2026-08-13 — CENSUS DOCTRINE v2, CLOSED** (`todo_census.md`): all censuses own-platform-count, organic
  tanks on mech bases, lift censuses empty; machine-enforced by `CensusIntegrityTests` (6). ⚠ Do not re-open
  the carrier question in either direction without touching that guard (supersedes the 08-12 "leave them" ruling).
- **2026-08-12 — MAP-STANDARD**: map size per-scenario from the `.map` header, `MapConfig` geometry deleted,
  truncation throws, derived scroll bounds. `MapStandardTests` (14, green 2026-08-20).
- **2026-08-10/11 — DOMAINS D0–D3** (`todo_domains.md`): domain vocabulary, post-hoc spotting (§12.4.4a),
  transit air defence (D2, play-confirmed), helo over-water grace (D3) + **SAVE_VERSION 6**. All suites green 2026-08-20.
- **2026-08-08/10 — PROFILE REBUILD P0–P3** (`todo_profiles.md`): `EquipmentBays`, derived bay capacity, naval
  sealift, movement-medium rules + **SAVE_VERSION 5**. P4 is the remainder.
- **2026-08-03/04 — AUDIO REBUILD Phases 0–3** (`todo_audio.md`): SFX as imported assets, catalog + facade +
  fog gate, battle-map sounds WIRED (movement/fire/impact/ambush/objectives/denied). Remaining audio work is
  clips + Bob's Inspector items + host-blocked sounds — see the thread board below.
- Earlier (weapon-rating migration, combat engine M0–M9, orchestrators, intel ladder, printer, AI0–AI2b,
  content pipeline 0/1/3/4, HUD pass): `Claude_TODO_Archive.md` + `Claude_Project.md`.

**📌 Dormant-on-arrival (so their silence is never read as a bug):**
- **Weather is single-state Clear** — §5.13.4 air grounding, storm sea costs and every weather ICM can never
  fire until the weather pass exists. Built where cheap; none of it validatable in play.
- **§15 supply is designed but `BattleManager.ProcessUpkeep` is a stub** (depot generation, minor-depot,
  airbase replenishment). Everything trace-dependent — beachhead conduit, water exclusion, HCL decay — waits on it.

**⏸ Deferred: ROUGH-EDGES PASS** (Bob's call 2026-07-28; superseded twice, never started). Consolidate battle-scene
rough edges before adding functionality. ⚠ Start by ASKING Bob which edges he means — the note records intent, not
a diagnosis.

---

## 🧭 MAJOR OPEN THREADS — AT A GLANCE

> **For Bob.** One entry per major thread: where it stands, what moves it. No implementation detail here —
> that lives in OPEN WORK below and the plan files.
>
> **⚠ MAINTENANCE RULE — THIS SECTION MUST BE UPDATED WHENEVER IT IS TOUCHED (Bob's demand, 2026-08-20).**
> Any session that lands work in one of these areas updates that thread's entry in the SAME session, before
> the session ends — status line, gate, and "next move" all three. A stale glance-board is worse than none:
> Bob reads THIS to decide what to do with an evening. Adding a new major thread = add a row here; closing
> one = mark it CLOSED with the date and move it to the pass ledger next session.

| Thread | Stands at | Next move / gate |
|---|---|---|
| **Requisition (P4)** | ▶ NEXT. Wallet + atomic spend LIVE (08-17); bay buy/sell/upgrade API + UI unbuilt. | Agent: build per `todo_profiles.md` P4. No gates. |
| **Campaigns + Save/Load** | Pipeline Phase 2 paused CLEAN, all decisions settled. Campaign folders invisible to discovery; `SaveLoad` has ZERO callers — no Save button exists. | Agent: resume trio in OPEN WORK. Cost grows per mission authored (25–30 planned). Menu listing is Bob-gated (prefab). |
| **M13 — turn loop / air missions / AOB** | The big frontier. Air RULES built + tested; air GAME unwired. Turn loop is straight-through; reaction yields are a day-one requirement (retrofit = rewrite). | Agent-led, large. Gates: D4, I8, most printer emitters, M14 remainder, D2 fixed-wing play-verify all sit behind it. |
| **Audio** | System + policy + wiring DONE through Phase 3. Most wired sounds have NO CLIP yet; battle-HUD buttons silent. | Bob: author wavs (helo/jet long cuts too), put `UIButtonAudio` on HUD buttons. Host-blocked sounds arrive with M13/supply/leader/§17. |
| **Leaders (L1–L4)** | Combat mechanics LIVE (M14 slice). Awards engine, pool/recruitment, details UI all unbuilt. Recruitment economy UNBLOCKED by the wallet (08-17). | Agent: L1+L4 approved + headless-safe, can start anytime. Art dependency: portraits + deco layers (Bob). |
| **AI** | AI0–AI2b live (board analysis, EV oracle, honest-spotting belief store). AI takes no turn yet. | Agent: AI3+ per `Claude_AI_TODO.md` (irregular doctrine first, for Khost). AI2 state still owed a SAVE_VERSION ride. |
| **Domains: naval + D4** | D0–D3 CLOSED. D4 fixed-wing staging gated on M13/AOB. N0–N3 designed (`todo_domains.md` §F/§H), suite-verifiable, unplayable without a coastal map. | Bob: coastal test map when convenient. N3 additionally gated on §15 supply. |
| **Intel** | Six-rung ladder LIVE + play-confirmed. Open: I7 HQ SIGINT sweep (= M15, unblocked, slot-in-anywhere), I8 RB tiers (M13-gated). | Agent: I7 whenever convenient. Deferred skill re-homes need Bob ratification. |
| **Printer / dispatch feed** | CRT + emitters LIVE for every existing host. Owed: P5 ledger persistence (SAVE_VERSION bump), P8b tests. Air/logistics/leader emitters host-blocked. | Agent: P5 persistence is small and self-contained. §11.7.2 evac revision awaits Bob's eyeball. |
| **Supply (§15)** | Designed; `ProcessUpkeep` is a stub. Gates N3, HCL decay/recovery, logistics dispatches, depot REP award. | Agent: its own pass, unscheduled. No blockers besides size. |
| **Weather** | Single-state Clear. Rich model deferred by design ("revisit before ship"). Several built rules dormant until it exists. | Design pass first (Bob + doc), then code. Nothing else gated on it except the dormant rules. |
| **Content / editor coordination** | Khost re-priced editor-side (s0 0.302, ladder .38/.47/.56, 7/7 rungs). C7 fractional gate + V19 kill constant LANDED game-side 2026-08-20 (SAVE_VERSION 8) — E15 (fraction authoring + collision check) unblocked. Hamburg (44×21 EU) in authoring. | Editor-side: E15, Khost manifest re-export with the fraction, E14, Hamburg values. Game-side: ⚑ C7 suite run; EU/CH ⏸ verify fires on their first export. |
| **Ship-blockers (small, must not be forgotten)** | Tilde (~) reveal cheat in `GameIconRenderer`; `AudioSettings` local `JsonSerializerOptions` (JsonPolicy rule violation). | Agent: both are quick deletes/redirects; do before any external build. Tracked in Cleanup. |

---

## BOB'S QUEUE (nobody else can do these)

- [ ] **Wire the End Scenario button** → `BattleManager.OnEndScenarioButton` (Inspector, like End Turn — do NOT
      add a HUD copy; the name is a contract). Owed since the prestige pass closed (2026-08-17); the editor's
      status memo lists it too. Until wired, voluntary early finish (§17.9.2) is unreachable in play.
- [ ] **Wire the TWO loss-report buttons (decided 2026-08-20: two buttons, not a toggle).**
      `OnDisplayLossesButton` (cumulative) and `OnDisplayDailyLossesButton` (this turn) each get their own
      Inspector-wired button. The orphan `RaiseDailyLossesRequested`/`RaiseTotalLossesRequested` events were
      deleted with the decision; the callbacks read the ledgers directly.
- [ ] **Run `Tools/UI/Audit Button Wiring`** — ~20 buttons wired, tool never exercised. Should come back clean.
- [ ] **Build versioning (Bob, 2026-08-08):** pick a scheme (proposal: `0.<pass>.<hotfix>` pre-1.0), set
      Project Settings → Player → Version. Agent then surfaces `Application.version` in menu + logs and stamps
      it into the save header when saving gets wired.
- [ ] **Tell the Scenario Editor agent G1 HAS LANDED** once a build ships with it — their stated trigger to
      start writing `mapConfiguration: None` and to open their E3 phase (manifest `mapWidth`/`mapHeight` +
      cross-stamp).
- [ ] **Relay to the Scenario Editor agent** (`ScenarioEditor_Status_2026-07-28.md` covers most of it):
      (a) checksum decision SETTLED — header field stays as their fingerprint, game never validates;
      (b) `classificationName` green-lit for removal; (c) leaders can go name-form;
      (d) briefing narration is CAMPAIGN-SCENARIO ONLY (§20.4.2) — missing narration is normal, not an error;
      (e) always say WHICH KIND of scenario (§20.4.1). ⚠ Check whether the 08-14 census courier already
      carried any of this before re-sending.
- [ ] **Possibly still owed to them: `JsonPolicy.cs`** — flagged for sending twice, receipt unconfirmed.
      Low urgency; they inferred the one property that matters.
- [ ] **ART owed (accumulating, no rush):** solid-white swaps for MoveRangeFill/ZocStop/MovePathStep/MovePathEnd ·
      real cursor art (§24.11.3) · Leader Pool + Upgrade button art · leader base portraits (3) + deco layers (~14) ·
      `UIButtonAudio` onto battle-HUD buttons · movement long-cut wavs for helo + jet.
- [ ] **Scene work, gated on the first non-32x21 map:** add `BattleBackgroundFitter` to the Background Room
      object (pre-calibrated) — pairs with the ⏸ auto-fit test below.

---

## ⚑ TESTING REQUESTS — the agent's queue for Bob

> **Why this exists (Bob's idea, 2026-07-27):** the agent cannot run Unity, play-test, or see the Inspector, so
> every question only a human at the keyboard can answer gets queued HERE instead of being scattered across the
> milestones where it gets lost. **Read this section first when you sit down to test.** It should normally be
> short — if it is long, the agent has been writing code faster than it can be validated.
>
> **Rules of the section:**
> 1. The AGENT adds entries and NEVER ticks them. Only Bob's result closes an entry.
> 2. Every entry states **DO** (the exact steps), **PASS** (what a correct result looks like — never just
>    "check that it works"), and **WHY** (what breaks if it is wrong, so Bob can judge whether to bother).
> 3. `[!]` = blocking, the agent should not build further on top of it · `[ ]` = normal · `[⏸]` = gated on
>    something that does not exist yet, with the gate named.
> 4. A FAILED test does NOT get deleted: Bob writes what he saw under the entry, the agent writes the diagnosis
>    under that, and it stays open until it passes.
> 5. A PASSED entry is deleted from this section the same session, after its result is recorded in the change
>    log and, if it is a shipped behaviour, in Claude_Project. **This section is a queue, never an archive.**

- [!] **C7 SUITE RUN — the fractional objective gate + V19 (2026-08-20).**
      **DO:** run the full EditorTest suite — in particular `MissionObjectiveGateTests` (grown 7 → 15:
      counts API, `RequiredObjectiveCount` arithmetic, fractional gate), `VictoryGradeTests` (+2
      fractional-gate compositions), `PrestigePersistenceTests` (fraction round-trip at a NON-default
      value), `SaveMigrationLadderTests`, and `ScenarioManifestTests` if one exists (IsValid gained the
      fraction range check).
      **PASS:** green. The one to watch is `RequiredObjectiveCount_FloatEdge_DoesNotOverCount` —
      `(10, 0.3f)` must be 3, not 4 (the float trap the editor hit in E14).
      **WHY:** the gate runs at grading, the early-end button and the auto-end through ONE shared
      predicate — wrong here is wrong at all three, and `SAVE_VERSION` is now 8 (v7 saves refuse —
      correct pre-1.0 behaviour, but a surprise if you had a test save lying around).

- [ ] **Fog-of-war movement range (owed since 2026-07-21; LOW priority, not blocking).**
      **DO:** move a unit along a path passing near an enemy at SpottedLevel 0 (tilde reveal OFF, known OOB
      position). Watch the range overlay before and during the move.
      **PASS:** the unspotted enemy neither blocks the displayed range nor carves a hole in it. A CONTACT halt
      during traversal is correct and is not this test.
      **WHY:** if `HexMapUtil` range generation consults unspotted units, the overlay leaks their position — a
      fog breach invisible to EditorTests (they see all units). Bob 2026-07-28: "functional thus far, can't
      claim it's perfect" — incidental non-observation is not this test; it needs the deliberate geometry.

- [⏸] **Background auto-fit — GATED ON THE FIRST NON-32x21 MAP** (Bob's call, 2026-07-27).
      **DO:** add `BattleBackgroundFitter` to `World Space/Hex Map/Background/Background Room` (defaults
      pre-calibrated), then load Khost AND the new map.
      **PASS:** Khost looks IDENTICAL to the hand-tuned state; the new map frames inside the table window with
      the green tube padding intact.
      **WHY:** the component exists but is not in the scene — nothing auto-fits today; Khost only looks right
      because it was hand-tuned. Same pass validates the derived scroll bounds (G5, code landed 2026-08-12):
      camera limits must hug the new map, not ±100.

- [⏸] **EU/CH THEME VERIFICATION — GATED ON THE FIRST EUROPE- OR CHINA-THEMED EXPORT (wired 2026-08-19).**
      **DO:** export any scenario with `"mapTheme": "Europe"` (or `"China"`) and load it. Likely pairs with the
      auto-fit item — the first non-ME map is probably also the first non-32x21 map (Hamburg is 44×21).
      **PASS:** chunk terrain draws the theme's tiles (not magenta, not ME art); airbase/fort/sprawl icons, city
      icons and nameplates all draw the theme's art; terrain portraits match; console shows NO `CreateMapIcon`
      warn-and-skip and NO `GetSprite ... not found`. Then reload Khost — ME unchanged.
      **WHY:** the wiring is switch-driven and suite-invisible (EditorTests don't render) — a wrong arm, a
      misnamed sprite or a stale atlas shows up ONLY in play. Khost-green exercises just the ME arms.

---

## OPEN WORK

### ▶ P4 — REQUISITION (`todo_profiles.md`, the live frontier)
The bay purchase/upgrade economy on top of `EquipmentBays`: buy/sell/upgrade API (headless), prestige pricing
(`PrestigeCost`/`TurnAvailable` fields exist on every profile), and the purchase UI surface. The wallet and
atomic `SpendPrestige` are LIVE (2026-08-17) — this pass finally has a currency. Spec: `todo_profiles.md` P4
+ DesignDoc §18. ⚠ P5 (content/docs) was merged into the domain doc pass; do not resurrect it separately.

### CONTENT PIPELINE Phase 2 — campaigns (PAUSED 2026-07-28, clean; full detail `todo.md`)
Phases 0/1/3/4 CLOSED: a standalone scenario is a folder — discovered, listed, played, no code. What remains:
- **Campaign scenarios are NOT REACHABLE** — discovery scans `Scenarios/` only; `Campaigns/...` is invisible.
- **Saving/loading is NOT WIRED** — `SaveLoad.SaveAsync`/`LoadAsync` have ZERO callers, no Save button. The
  save CONTRACT (shape, refusal, migration ladder, provenance) is finished and tested; the FEATURE is unbuilt.
  ⚠ While zero callers exist, `SAVE_VERSION` bumps are free (three shipped since: 5, 6, 7, each with its
  rationale at the constant in `GameData.cs`). **The moment saving gets a caller that discipline expires** —
  every shape change then needs its own version + ladder step.
- **Settled decisions:** §19.1.6 amended (scenario owns thresholds, campaign owns routing as `BattleResult`
  edges); manifests are the agent's to maintain; `contentVersion` deleted from `ScenarioManifest` + save header,
  **still OPEN for `CampaignManifest`** on one question: will a rebalanced campaign graph ever reach a player
  WITHOUT a new build? (Bob expects remote-tester rebalancing — if revisions go out as bare files, the field
  comes back, and to the save header too.)
- **Resume order:** (1) 2.1+2.4 `CampaignManifest` + `CampaignNode` (branch/edge shape) → (2) 2.2 delete inert
  `ScenarioManifest.IsCampaignScenario` (re-source `BattleManager.IsCampaignBattle`, currently write-only) →
  (3) 2.3 discovery + menu (`.campaign`, `CampaignLoader`, `GameDataManager.CurrentCampaign`; ⚠ BOB-GATED:
  agent writes `CampaignDialog_Scene0` as a structural twin of `ScenarioDialog_Scene0`, Bob duplicates the
  prefab — nothing lists campaigns until he wires it).
- **Cost of waiting:** grows with every mission authored (25–30 planned) — each pre-Phase-2 mission must be
  retrofitted into the graph. Nothing else is gated on this; paused is safe, not free.

### INTEL — remaining rungs (I1–I6, I9 + AI mirror CLOSED 2026-07-25; records in the archive)
⚠ **Coupling (every item):** the AI plays by these rules — each change lands in BOTH SpottingService sweeps
AND the `AIPerceptionState.StepDecay` mirror (floor MAP, not an in-range set).
- [ ] **I7 — HQ SIGINT sweep (§12.7).** Map-wide roll per enemy on an HQ IntelAction, +1 rung ceiling L3,
      gated on `SIGINT_Rating`, bounded by RADIO SILENCE (no move/fire/resupply last turn = untargetable).
      This IS the M15 milestone — unblocked, can slot in anywhere.
- [ ] **I8 — RB tier rewrite (§11.11.11).** `ReconMissionEngine` callers: 100/50/25 = coverage probability,
      each success +1 rung ceiling L3; the old "floor of Level 2" is retired. Gated on the M13/AOB caller.
- [-] **I10 — resolved NO (§24.3.2.5 ratified):** strength % lives in the icon HP box ONLY. Do not re-propose.
**Deferred out of the pass (need Bob ratification, write-ups in §14.8.7 / Leader_Supplement §3.7/§3.7a):**
Concealed Operations Base re-home (→ Radio Discipline; settle WITH Satellite Recon → sweep-ignores-silence) ·
new skill candidates (Field Interrogation ⚠ collapses the safe/fast tradeoff — price late or drop; Trained
Observers; Persistent Surveillance) · SigInt T4 Communications Decryption sharpening (sweep ceiling L3→L4).

### PRINTER — remaining slices (P1–P4, P6, P7, P8a DONE + play-confirmed; records in the archive)
- [~] **P5 — LOSS LEDGER: built, green, play-confirmed 2026-07-28; only PERSISTENCE remains.**
      ⏳ Owed: snapshot field + its own `SAVE_VERSION` bump so the ledger survives save/load. While in there,
      **DELETE the second-home stubs** `BattleManager.RecordPlayerUnitLoss`/`RecordAIUnitDestroyed` (empty,
      zero callers, under a `// TODO` — the ledger lives in GameDataManager).
      Constraints that shaped it (full detail Claude_Project §3.6d — keep them true): booked in
      `CombatUnit.TakeDamage`, the single damage funnel (+ explicit surrender booking in `RetreatResolver`);
      keyed by `WeaponType`, floats accumulated, rounded ONCE at render; HP actually removed, not requested;
      removals (shatter/withdraw/evac) are not losses; daily ledger = second accumulator, never a diff.
- [ ] **P8b — Tests.** History cursor bounds, dedup, filter, ledger arithmetic (proportional maths + save
      round-trip once P5 persistence lands), and unearned-rung line omission.
- [ ] **§11.7.2 air-displacement revision awaiting Bob's eyeball (flagged 2026-06-25):** adjacency = FORCED
      evac, indirect/air bombardment = OPTIONAL owner evac (2-strike auto-evac safety net REMOVED —
      Pearl-Harbor-under-bombardment is a player decision).
> **Emitters still unwired, blocked on hosts that do not exist:** air operations (M13/AOB — the class Bob
> expects to carry the feed) · logistics (§15.4a) · decorations/promotions/leader-killed (L1/L2) ·
> opportunity + AD fire (the §11.8 transit walk) · turn-boundary divider (M13).
> ⚠ §24.8.5 exclusions STAY — out-of-MP / terrain-blocked / deployment refusals are denial SFX, not dispatches.

### INPUT / UI (battle map)
> **🔷 OVERLAY-SPRITE CONVENTION (ratified 2026-07-21):** the hex cell is a REGULAR pointy-top hex, 2.56 wide ×
> 2.956 tall — square-canvas hex art renders ~13.5% short. ALL hex-shaped overlay sprites stamp through
> `HexGridRenderer.FitToCellScale` (owed: ThreatFill_* in the M13 threat overlay). Point markers render
> authored-size. Overlay art ships SOLID WHITE — HexGridRenderer applies serialized tint × per-overlay opacity.
> **When planning any NEW overlay sprite, ASK Bob whether it is hex-shaped (fit-scaled) or a marker.**
- [ ] **Move Undo (§5.11 — v1 CONFIRMED, HUD button art exists):** pre-move snapshot (MP, actions, position,
      facing, deployment profile) + spotting-dirty flag (§5.11.1: undo only if no enemy SpottedLevel rose);
      voided by ambush / ZoC-halt / extra supply (§5.11.4); single undo per move; wire `OnMoveUndoRequested`.
- [ ] **Cursor system completion (§24.11.3):** real art (Bob) replacing procedural placeholders; per-mode
      cursors as each input mode lands (unit-pick §24.5.5, AOB placement §24.7a.1).
- [ ] **Denial SFX asset:** the `ButtonDenied` hook is WIRED on all five refusal paths (2026-08-04) — what is
      missing is the wav (Bob's art queue) and confirming the illegal-Ctrl+click path plays it in play.
- [ ] **Input-mode state machine** (Normal / CtrlCombat / CombatTargeting / UnitPick / AOBPlacement / AOBMode /
      ReactionInterceptorPick) — cursors key off it; the AOB save-block and §24.11.1 universal-Esc stack live here.
- [ ] **HUD button wiring** as Bob's layout lands — callbacks grow on `DefaultDialog_Scene1`. ⚠ The Inspector
      owns EVERY onClick; code never `AddListener`s; `On*Button()` names are a contract (Claude_Project §3.6b).
- `ButtonWiringAudit` + `Find Unwired Button Callbacks` are BUILT (2026-07-27), never run — queued for Bob.

### LEADERS — completion (§9: L1–L4; mechanics live via the M14 safe slice)
**Spec:** §14.14 (awards, RATIFIED) + §14.15 (recruitment) + §24.5.5/.6 (UIs, RATIFIED). **Intent (Bob):**
campaign leaders build FROM ZERO — first-scenario leaders and recruits both arrive Average with 60 REP.
**L1 — Awards & decorations engine (§14.14, three channels):**
- [ ] `LeaderAwardCatalog` (static, mirrors LeaderSkillCatalog): AwardId / channel / trigger predicate / layer
      asset / display name / chest slot + precedence. A badges (computed from tree, replace-chains) · B combat
      orders (deed counters, sticky) · C service awards (LifetimeRepSpent, sticky).
- [ ] Leader counters (serialized, snapshot round-trip): `CombatActionsLed`, `EnemiesDestroyed`, `RetreatsForced`,
      `AttacksWithstood`, `ScenariosServed`, `LifetimeRepSpent` + append-only `EarnedAwards`. Increment sites:
      `GroundCombatAction` (attacker deeds + defender Hold), `Leader.UnlockSkill`, battle end (M13).
- [ ] Award-check pass + `OnLeaderDecorated(leader, awardId)` → printer citation + UI toast.
- [ ] Portrait composition prefab: base-by-grade + stacked deco layers (display cap ~6 by precedence).
      ART DEPENDENCY (Bob): 3 base portraits + ~14 curated deco layers.
- [ ] EditorTests: per-channel thresholds, replace-chains, respec keeps B/C + strips-and-recomputes A,
      LifetimeRepSpent never decrements.
**L2 — Leader Pool + recruitment (§14.15 + §24.5.5):**
- [ ] Pool model: roster view over GameDataManager leaders + seeding path for test leaders.
- [ ] **Leader mortality (§14.15.4, RATIFIED — no rolls):** unit DESTROYED or SURRENDERS → leader DIES,
      permanent (GroundCombatAction/RetreatResolver removal paths); shatter → survives to WITHDRAWN-RESERVE.
- [ ] Recruitment + assignment economy: `LEADER_RECRUIT_COST 50` / `LEADER_ASSIGN_COST 30` (knobs); recruit =
      Average + 60 REP; every placement pays 30; Remove free; dismissal NOT in v1.
      ✅ **The wallet dependency is RESOLVED (2026-08-17)** — `PrestigeWallet.SpendPrestige` is live and atomic.
- [ ] Pool UI per §24.5.5: HUD button → UIListBox dialog; Assign → UNIT-PICK input mode (TargetPickOutline,
      hex-shaped → `FitToCellScale`; Esc/right-click abandons; blocked while an AOB is open; swap-on-led-target).
- [ ] OPEN (§14.15.5): how Good/Superior/Genius CC enters play (campaign rewards vs recruit rarity) — Bob.
**L3 — Leader Details UI (§24.5.6):**
- [ ] Layout: composited portrait (per-decoration tooltips) + identity block (rank, CC, EffectiveCommand
      talent-gap) + REP block + service record (counters + next-award progress) + skill tree.
- [ ] Skill tree per §24.5.6.1/.2: FOUR node states (grayed-with-reason / lit / highlighted / STRUCK);
      tier-aligned constellation — Foundation (2 cols) | Doctrine (7) | Specialization (4), T1→T5 rows,
      connector lines, live striking of sibling branches.
- [ ] Purchase flow: lit-node click → confirm → instant; anytime in player turn; promotions as gate-nodes;
      Respec IN V1 (§24.5.6.4).
- [ ] EventManager contract for both UIs (Pool/Recruit/Assign/Unassign/Details/SkillPurchase/Respec/Decorated).
- [ ] Test-leader seeding once L2 lands (OOB pass-3 leader loading still deserves its own exercise later).
**L4 — Remaining safe wirings:** 
- [ ] `SpottingRangeBonus` → both dual-domain ranges (§12.3.11); `IndirectRangeBonus` → `ActiveIndirectRange`.
      Hosts locked & green; small slice, do with L1.

### M13 — TURN LOOP, MOVEMENT DYNAMICS & AOB (the combat-engine frontier)
Consolidates the caller debt from M2–M12, the ratified AOB input package, and the AI track's asks. Headline
(2026-08-10 audit, still true): **the air RULES are built and EditorTest-covered; the air GAME is not wired** —
no AOB entity, no placement mode, no air phase, no fixed-wing auto-return. (The audit's two rulings are both
RESOLVED AND BUILT: ambushed helo takes an ordinary attack minus the surprise multiplier + transit stand check,
2026-08-10; helo gets no 1d6 detection roll — D2 gives it only to fixed-wing, 2026-08-11. Do not re-open.)
**Turn loop core:**
- [ ] BattlePhase loop (§3): Refresh (efficiency recovery §7.15.8, action/MP reset, supply), orders, Upkeep,
      AI turn (v1 stub), TurnBoundary. Spotting decay + AI_Refresh hooks already exist.
- [ ] ⚠ DAY-ONE REQUIREMENT (§11.1.8.6): the loop must support REACTION YIELDS — an async/state machine that
      pauses for player input at reaction points (hosted by `ReactionWindowController`), NOT a straight-through
      sequence. **Retrofit = rewrite.** AI-turn AOBs prompt the HUMAN; ship the S2 reaction-policy INTERFACE
      with M13 (v1 = decline-all config, AI brain swaps in later).
- [ ] Headless move-order path extracted from `MovementController` (AI + input both call it) — AI-track ask.
- [ ] Automatic Advance (§7.9.9): attacker's free advance into the vacated hex (RetreatResolver already reports
      `AutomaticAdvanceAvailable`/`VacatedHex`; prompt + move are the loop's).
- [ ] WITHDRAWN-RESERVE roster placement on shatter quit-field (§7.9.6.4/§35.2). **Kill prestige crediting
      (§18.2.3, half purchase cost)** — the wallet is LIVE; wire the credit at the kill sites. ⚠ The old
      "objective prestige crediting" half of this item is RETIRED — capture awards were deleted 2026-08-17 in
      favour of the §18.2 income model, which already runs in Upkeep.
- [ ] §7.15.7 move-path supply: replace the deterministic per-hex consume (`CombatUnit.cs`) with the §7.15.4
      probabilistic roll (combat path already converted); §7.15.2.4 Degraded move gate (controller + UI).
- [ ] Reactive facing (§5.8.8, free once/enemy-turn): HasReactiveFaced flag + rotation + flank negation
      (+ exemptions for bases/indirect/air at call sites).
- [ ] Contested-crossing caller geometry (§7.5.6.9.1) — also feeds the M14 RiverAssault ICM.
- [ ] Confirm GroundFire opp-fire stays retired (§8.3.2) — ambush is the only ground-vs-ground reaction.
- [ ] ROC two-CombatAction salvo (§7.14, different targets allowed); Scud single-shot, no bonus (§7A.11).
- [ ] Battle result evaluation integration (§7.16 — grading itself is LIVE since 2026-08-17; this is the smoke
      scenario + UI hooks: opp-fire highlight, Level-4 reveal §7.12.4/§11.8.4).
- [ ] Balance pass: GroundBalanceMod/AirBalanceMod vs §7.9.5.1 distribution targets; AD GAT lethality;
      prestige/DeploymentPointCost. ⚠ After ANY combat-const change re-run `CombatOracleTests` — the AI EV
      oracle's drift guards enumerate the real engine and fail loudly if the mirror is stale.
**Air transit & AD walk (movement-dynamics core):**
- [ ] Air transit walk — per hex: MP §5.13.1, spotting, shot budget §11.8.3, anti-dogpile §11.8.6, towed
      posture gate §11.8.8, AD fire, helo transit stand check §11.8.9 (abort → force-disembark at ORIGIN);
      fixed-wing auto-return §5.13.5 (`AnimateAutoReturn` + `OnAirUnitReturning` exist, zero callers); transit
      spotting + §12.7.2 forward-spotting recency window for ASB targeting. Per-sortie supply deduction
      (consts + `CanLaunchSortie` exist, zero consumers).
- [ ] `AirThreatService` — shared eligibility/footprint (CanInterdict = AD-class + GAT ≥ 6 + posture gate;
      footprint = spotting + IR per §11.4.4), consumed by BOTH the §24.7a.8 AD threat overlay (GAT bands,
      overlap = darkened worst band; ThreatFill sprites exist — hex-shaped → `FitToCellScale`) AND the §11.8
      walk — same helper, the overlay never lies.
- [ ] In-hex ground fire (§11.4.8.5) + egress opp fire (§11.4.8.7); helo direct-attack path (GA vs GAD §7A.14,
      no OL §11.6.1.5).
- [ ] Air-ambush reveal branches: detection SUCCESS → L1 (done); ambusher FIRES → L4 (§11.8.4) — wire when
      AD-fire lands.
**AOB framework (§11.1 + the ratified input package):**
- [ ] `AirOperationsBox` model + populate pipeline: order-time validation (slot pre-check, `CanLaunchSortie`,
      CombatAction; RB pays CombatAction §8.5.2), arrival sequence §11.1.3 → off-map (icon + spotting
      exclusion) + slot fill + type-flip via `AOBMissionResolver` + `OnAOBStateChanged` snapshot; pay-at-launch
      BOTH sides (§11.1.8); cancel/end-of-turn = free auto-return, actions lost; one-AOB-at-a-time (§11.1.6);
      WW pre-lock slot; AEWB 1/turn cap; per-sortie supply deduction.
- [ ] Placement input (§24.7a.1): AOB button → box-on-cursor → Ctrl+left-click place; §11.1.9 AOB-Mode lockdown
      (air-only input, SAVE DISABLED while a box is open, Esc never cancels a box); §24.7a.7 Resolve/Cancel row.
- [ ] `ReactionWindowController` + §24.13 Phase Control Bar: per-arrival interception windows §11.1.8 — WW
      arrivals excluded (bait-proof), 1 interceptor per window, decline loses that window only, dead
      interceptor ≠ filled slot.
- [ ] WW / SEAD orchestration (M10; damage primitive = `ResolveAirStrike(ww, sam)`): WW slot gate, per-SAM-shot
      counter-fire, 1 OppAction/shot §11.1.2.3, `WildWeaselAlive` on strike lanes, firing SAM revealed Level 4.
- [ ] Other AOB missions (M11): AAB airborne assault §11.12 · AEWB §11.13 (+1 Δ offensive lanes rest-of-turn,
      symmetric, 1/turn) · SB air supply §11.9 (5-day load, Replacements rider §15.4a.4a, ferry-neutral refund)
      · RB caller bits (per-tier sweep — see I8; HP application, CombatAction cost, no-defender auto-100%,
      auto-return).
- [ ] Helo AIB (M12, §11.8.10): 1 helo + 2 escort + 2 interceptor; helo defends on GAD; dual-phase economy
      §8.5.1a (phasing pays Combat, reacting pays Opp — do NOT hard-code roles); automatic + declinable, no
      safety roll; reuses M7 dogfight + §7.9.8 air stand. Triggers: (a) reaction interception vs transport
      helos, own turn, spotted, once/turn; (b) enemy-turn AIB, all helos. Lose → transport disembarks Deployed
      at the intercept hex / attack helo damaged + efficiency hit, stays. Reactive box; obeys one-AOB.
- [ ] `LoiterReattack` rider (CAS re-attack / extra-Opp hook); conditional strike maluses (LOW_LEVEL_STRAFE /
      STANDOFF_PGM / HIGH_ALTITUDE_BOMBER) need the AD-interaction layer — Dormant until it exists.
- [ ] Base-combat callers (M9 debt): ground-attack-on-base (OC 100 §11.7.2.5 + base return fire §7A.20);
      air-displacement evac per the REVISED §11.7.2 (Bob's-eyeball flag, Printer section); destruction loses
      attached aircraft; ZoC repair-lock; repurchase + 5-turn activation; bridge strike §11.7.3;
      SAM-suppression SEAD §11.7.4; BM dual-targeting / un-interceptable routing §11.7.5.2/.3.
- [ ] WW/TRN air-unit treatment in the ~7 hardcoded air/action checks (C1/C2 debt).
**Also gated here:** §5.13.4 Storm grounding + §5.13.3.3 fixed-wing no-deployment-change rule (currently an
accident of empty bays, not a rule) — both land with the air walk; Storm additionally needs weather to exist.

### M14 — Leader-skill pass remainder (safe slice DONE 2026-07-03; rest gated on M13 hosts)
**Gate & policy:** every leader effect is a MODIFIER on a combat/movement/supply primitive — wire only against
locked hosts. Prereq nodes are kept-and-neutralized, never deleted (`ValidateSkillTreeSystem` throws).
`ForeignTechnology_NVG` stays neutralize-keep (dormant until the night/weather pass).
- [ ] Runtime `leader_skill_mod` ICM layer (§7.5.5.6, multiplier-effects only): RiverAssault ×1.4 (needs M13
      contested-crossing geometry), NVG asymmetry §21.5 (dormant), NBC zones §21.4 (not generated v1), scenario
      mods. DEFERRED until a live consumer exists — no dead scaffolding (deliberate).
- [ ] System-gating booleans into their hosts as M13 lands: Breakthrough (→ Automatic Advance), ShootAndScoot +
      AdvancedTargetting (→ indirect economy; R-L2 gated ART/SPA), AirDefense T3 +1 Opp (→ AD opp fire),
      Airborne/AirMobile post-jump retention (→ M11/M12), Engineering river/bridge/fort, SignalIntel
      decryption/EW/pattern, SpecialForces infiltration/concealment/ambush.
- [ ] EmergencyResupply UI (R-L3: once/scenario instant 5-day delivery); DirectLineToHQ `ReplacementCost` ×0.7
      (host = the replacement/requisition flow — lands with P4/§15.4a; the wallet itself is live); depot REP
      award wiring (R-L10, lands with §15.4a Resupply).
- [ ] HQ/DEPOT-attached-leader facility skill map (§35.4.3, R-L4 ratified). Wire when facility systems land.
- [ ] Per-skill EditorTest suite; `ValidateSkillTreeSystem` stays green through every re-home.

### M15 — SIGINT reintroduction (= INTEL I7; REDEFINED 2026-07-24 as the HQ sweep)
⚠ The old scope (SpottedLevel bonus inside HQ projection) is RETIRED — with it died the §12.3.6 "6/6" range entry.
- [ ] `SIGINT_Rating` onto `CombatUnit` (NOT WeaponProfile) — enum parked in GameData, unreferenced.
- [ ] Rating gates the map-wide sweep (§12.7): UnitLevel none / HQLevel prov. 15% / SpecializedLevel prov. 25%;
      +1 rung per success, ceiling L3, bounded by radio silence. Full item = I7 above.
- [ ] OPEN KNOB (§12.7.8): multiple HQs = multiple sweeps, or 1-per-side cap like AEWB. Prov. uncapped.
- [ ] EditorTests: rating→sweep path incl. radio-silence exclusion; SpottingService regressions green.

### DOMAINS — D4 + naval N0–N3 (`todo_domains.md` §H is the plan; §I the editor relay list)
D0–D3 CLOSED (see pass ledger). Remaining ladder:
- [ ] **D4 — fixed-wing staging.** GATED ON M13/AOB — Bob 2026-08-11: "the mechanisms to run air missions are
      not in the game yet." D2's fixed-wing half (1d6 detection, transit AD vs jets) stays play-unverified
      until then; the code paths are suite-covered.
- [ ] **N0–N3 — naval foundations → naval combat + sea clock → port heavy lift → supply hooks.** Designed from
      Bob's five precepts; `FacilityType.Port` exists. Suite-verifiable but NOT playable until a coastal test
      map exists (Khost has no water); N3 additionally ⛔ gated on §15 supply. ⚠ There is NO hex-by-hex sea
      movement — §5.4.2.3 makes naval movement an instant port-to-port jump; do not add one.

### AI track → `Claude_AI_TODO.md`
AI0–AI2b landed, suites GREEN (2026-07-27). Next: AI3+ per the AI TODO (irregular doctrine ahead of the line
manager, for Khost). ⚠ AI2 snapshot serialization still owed its own `SAVE_VERSION` ride.

---

## OPEN DESIGN / TUNING FLAGS — deliberate, tunable calls to revisit at playtest

- **Lethality & economy:** AD GAT lethality (post-rebalance 7/10) vs real airstrikes. Prestige: DeploymentPointCost
  side (§35 CFR) untouched; Tu-22 Blinder left at formula 240 (flag if the Gen1 cruise carrier should be premium
  like the Tu-22M3).
- **Recon:** `Recon` archetype values invented (hardened HD5/SD9 = "soak first hit & withdraw" — revisit at combat
  rework). `RECON_FRAGILE` ×0.6 = doc's proposed magnitude. ERC-90 90mm = one-off residual HA+4 (promote to a trait
  if a 2nd 90mm scout lands — calibre traits are tanks-only).
- **Amphibious:** restored to T-72A/B via trait; not extended to the rest of the snorkel family (T-64/T-80).
- **Artillery:** chassis-trait magnitudes (SELF_PROPELLED/TRUCK_MOUNTED) invented but doc-sanctioned. Scud HA+6/SA+6 =
  big one-off deltas (promote to BALLISTIC_MISSILE if a 2nd lands). M109 ×4 identical & deliberately NOT smart (a
  Copperhead "precision M109" is the option to differentiate US); MLRS is smart. 2S5 = reach vs 2S19 = precision.
- **Air defense:** which guidance trait each SAM/AAA carries (→GAT) is a judgment call; some IR values literal.
  MOBILE_SHOOT_SCOOT only on Soviet Kub — candidate add for NATO Roland/Crotale/Rapier.
- **IFV/APC/recon/helo:** `ATGM_RAIL` normalizes vehicle HA (de-inflates old DB, e.g. M2 10→8). Humvee soft via
  `THIN_TOP` (GAD-only). Helo protection split (AH-64 GAD12 vs AH-1/Bo-105 GAD10); Bo-105 SA10 archetype-inherited.
- **Infantry:** ATGM ceiling role-based & restrained (REG/MAR→ATGM_LIGHT HA8, AB/AM→ATGM_MEDIUM HA9; US TOW/HA11
  rejected so foot infantry don't out-gun IFVs). MANPADS: US/FRG/FR→Stinger (GAT8/ICM1.05), UK→Basic (GAT6).
  BODY_ARMOR skipped (late-80s kit; setting is early-80s). No NATO Marines beyond US / no NATO SPECF profiles.
  MJ: all MJ infantry MOUNTAIN_TRAINED (final-intent); MJ AAA/SAM + artillery = invented improvised lines.
- **Jets:** pure fighters at Rule-A GA floor 2 (big move, accepted). `TARGETING_POD` HELD from all jets (precision
  jets use LASER_GUIDED_MUNITIONS). NATO GA ladder A-10 15 > F-111/F-117 13 > F-16 9 > strike 8 > Mirage F1 6 >
  pure 2. Chinese: J-8 agility below old MID (avionics lag); Q-5 GA floor 10; H-9 ATGM-only.
- **Export downgrade:** `EXPORT_DOWNGRADE` (HD-2/SD-1/ICM×0.9) ONLY on the 2 Iraqi tanks; other Iraqi exports mirror
  Soviet/NATO lines; aircraft export downgrade via DF/SUR residuals.
- **Weather ICM:** `IcmWeather(clear, poor)` stores both values; the resolver applies Clear — the poor-weather value
  is parked for the weather pass.
- **Stat-delta stacking cap per axis:** still open (resolver clamps final stats [1,25] only).

---

## Cleanup / housekeeping

- [ ] ⚠ **REMOVE BEFORE SHIPPING: the tilde (~) debug enemy-reveal cheat** (added 2026-07-06 at Bob's request).
      `GameIconRenderer.DebugRevealAllEnemies` + its `Update()` poller + the two fog-filter bypasses — all
      marked "REMOVE BEFORE SHIPPING". Rendering-only (SpottedLevel untouched).
- [ ] ⚠ **`AudioSettings.SaveSettings` builds a local `JsonSerializerOptions`** — violates CLAUDE.md item 10
      now that the one sanctioned exception (`MapChecksumUtility`) is deleted. Route through `JsonPolicy`
      (or add a third named policy if the settings format genuinely differs).
- [ ] **`_to_delete/` review (Bob):** superseded courier/handoff files swept there 2026-08-20 (plus 13
      zero-byte git-lock droppings from 08-13). Everything is in git history; delete the folder when ready.
- **Repo tracks the WHOLE PROJECT as of 2026-07-27** (standard-Unity opt-out `.gitignore`; `.meta`,
  `ProjectSettings`, scenes, `Packages` all in — a clone rebuilds a working project).
  ⚠ **THE GENERATED CHUNK ARRAYS ARE DELIBERATELY EXCLUDED AND MUST BE REBUILT AFTER A FRESH CLONE:** all three
  `Assets/Resources/Chunked/TerrainArray_<Theme>.asset` bakes (MiddleEast/Europe/China, ~289 MB each — over
  GitHub's 100 MB hard limit; EU + CH first baked 2026-08-19) and `TestArray_RGB.asset`. Rebuild via
  `Tools/Hex Chunk/Rebuild All Terrain Arrays`; a rebuilt asset gets a NEW GUID, so serialized references need
  re-pointing. ⚠ **LFS quota:** free tier 1 GB storage + 1 GB/month bandwidth; payload ~24% of storage and each
  binary revision adds more. History rewritten 2026-06-15 via git-filter-repo; older clones must be re-cloned.

---

## Change log

> **Rules:** one line per change · newest first · format `YYYY-MM-DD — imperative summary (area)` · entries
> older than the last two passes migrate to `Claude_TODO_Archive.md` when this section is pruned.
> **Entries 2026-07-21 → 2026-08-19 (incl. the theme-art pass and everything before it) are in the archive.**

- 2026-08-20 — C7 FRACTIONAL OBJECTIVE GATE + V19 (editor's ask, Bob-ratified; **SAVE_VERSION 8**): the
  §17.8 gate becomes held ≥ ceil(total × `missionObjectiveFraction`) — new manifest float (0,1], default
  1.0 = the C6 all-of-them rule, mirrored into `ScenarioData` (no migration arm, pre-1.0 rule; SnapshotMapper
  no-arm comment extended). `HexMapUtil.CountMissionObjectives` (counts, fail-open) REPLACES the deleted
  `AllMissionObjectivesHeld`; ONE predicate (`BattleManager.MissionObjectiveGateMet` + `RequiredObjectiveCount`,
  round-then-ceil float-trap defence, clamp [1,total]) serves grading + early end + auto-end so the three can
  never disagree. Battle-start diagnostic: held/total/required log + gate/ladder collision warning (min
  gate-met share ≥ decisive → middle rungs decorative — the re-priced-Khost defect). V19:
  `GameData.PRESTIGE_KILL_FRACTION` (0.5, away-from-zero rounding) replaces three `cost / 2` copies.
  Tests: gate suite 7→15, +2 grade compositions, non-default fraction round-trip. HS_DesignDoc §17.8
  (NEW 17.8.0/17.8.5) + §18.2.3 amended in step. Editor Q&A: C7 takes 8 (AI2b-3 takes its own later);
  wrapper DELETED; both diagnostics built. ⚑ suite run owed.
- 2026-08-20 — DESIGN-DOC AMENDMENT PASS (`HS_DesignDoc.md`, outside the repo — not in this commit): 12.3.7
  amended 2/4 → **0/4** + NEW 12.3.7a (fixed-wing transit is ground-blind, medium-keyed, RECONA/AWACS exempt —
  clears the amendment owed since 2026-08-10); 4.8 infrastructure list gains `IsPort` (Fort/Airbase/Port
  three-way exclusive) + `IsBeachhead`, matching the built HexTile; 19.1.6.4 status corrected (scoring IS
  BUILT since 08-17, routing data shape still the owed half); 27.2.1 records all-three-themes art
  (theme-art pass); 6.13.11 perf example de-32×42'd. Master Log left as-is (dormant since June by
  convention — recent ratifications live inline, dated).
- 2026-08-20 — Loss report: TWO-BUTTON model RATIFIED (Bob) — cumulative and daily each get their own
  Inspector-wired button (cycle toggle rejected). Orphan `RaiseDailyLossesRequested`/
  `RaiseTotalLossesRequested` deleted from EventManager (declarations, raisers, ClearAllSubscriptions
  nulls — zero subscribers ever); ruling recorded at the callbacks in `DefaultDialog_Scene1`. Bob's-queue
  item flipped from "decide" to "wire the two buttons."
- 2026-08-20 — ⚑ CLEARED (Bob ran it): full EditorTest suite GREEN — closes the `[!]` P3b air-rulings run
  (§12.3.7a fixed-wing-blind spotting incl. the RECONA/AWACS exemption, now confirmed), the map-standard run
  (`MapStandardTests` 14) and the D3 over-water run. The agent may build on all three again. Fog-of-war
  overlay check stays open (play-test geometry, low priority).
- 2026-08-20 — FULL REWRITE of this file (docs): staleness audit vs code + design docs; single ▶ NEXT pointer
  (P4 requisition); NEW 🧭 at-a-glance thread board with a same-session maintenance rule; corrected stale
  claims (wallet-dependency on L2/M13 resolved, objective-crediting retired, audio Phase 3 wired, SAVE_VERSION
  7 everywhere, air-audit rulings both resolved, `Generated Data` reference dropped); DONE records + old change
  log moved to NEW `Claude_TODO_Archive.md`; superseded courier files swept to `_to_delete/`; theme-art pass
  committed (`55587d2`); `OnEndScenarioButton` given a tracked home in Bob's queue; ⚑ P3b entry now bundles the
  owed map-standard + D3 suite runs. Pre-rewrite text verbatim at `55587d2`.
