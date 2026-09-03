# Claude TODO — Hammer and Sickle

> **⚠ AGENT REMINDER — challenge design-doc contradictions.** `HS_DesignDoc.md` + Appendix W are very detailed and
> heavily cross-referenced. When an instruction appears to contradict a ratified value/decision in them, STOP, verify
> against the doc, and flag it (with the counter-argument) BEFORE implementing — do not silently encode it. Settled
> points sometimes get relitigated; catching them is wanted.

> **⚙ TESTING HANDOFF (Bob, 2026-06-23).** The agent CANNOT run Unity test suites or play-test. When code is ready to
> validate, the agent says exactly **"Please run Unity Test Runner for me"** (or requests a play-test) and WAITS for
> Bob's result before marking a milestone `[x]` or proceeding. Write code + tests, verify by inspection, then hand
> off — never claim GREEN unrun.

The living work file. History lives in `Planning Docs/Claude_TODO_Archive.md` (DONE records + pre-2026-08-20 change log) and git.

- **Authoritative spec:** `HS_DesignDoc.md` · **Rating model:** Appendix W + `WeaponTrait_Supplement.md`
- **AI design (authoritative):** `Design Docs/Supplements/AI-Design-Supplement.md` · planning detail: `Planning Docs/Claude_AI_TODO.md` (this file gets only brief sync notes)
- **Codebase context:** `Claude_Project.md` — keep reconciled; update it in the same session that lands structural changes

**Legend:** `[ ]` todo · `[~]` in progress · `[x]` done · `[-]` deferred/dropped

---

## ⚡ CURRENT STATUS (2026-08-24 — pointer moved to the combat pass; file last fully rewritten 2026-08-20, prior text verbatim at `55587d2`)

**▶ NEXT: TOP-DOWN ICONS — `Planning Docs/Top-Down Icons.md` (ruled 2026-08-27, Bob's command decision).**
One sprite per profile, rotated to facing; old directional/firing path RIPPED OUT, no switch. Plan is
FINAL (post-scan, all rulings R1–R6 in, counts verified 184 = 89 Single · 53 Directional · 32
Directional_Fire · 10 Helo). **Implementation T-1…T-4 + the two §6 guard tests, as ONE commit, is the
next session's first task** — Soviet + MJ art is ready on Bob's side. ⚠ Read that plan's §1 trap and §2.1
correction BEFORE touching a field.

**⏸ ROSTER EXPANSION — `Planning Docs/Roster Expansion.md`. ALL ELEVEN NATIONS SETTLED 2026-08-29; no code
written yet.** Per-nation art + WeaponProfiles: generic artillery/AAA art abolished, the `AR_` regional
convention killed, Saudi Arabia added as a real nation, Kuwait deliberately empty. **Architecture SETTLED —
profiles for everything, no icon picker** (that plan's §1.1 records why; do not re-propose one). Carries a
Bob-approved **rename batch** (7 types, one `SAVE_VERSION` bump, one `khost.oob` re-export), **four content
errors** found in the audit, and a NEW `SECOND_LINE_FORMATION` ICM trait (0.9) for China.
⚠ **Sequenced AFTER the icon pass** so ~40 new profiles are authored in the new shape once, not converted
twice. **The work order is that plan's §5.**
✅ **THE ART-FREE STEPS ARE DONE: RE-1a, RE-2, RE-3a and RE-4 have all landed** (RE-4 on 2026-09-02, suite
run owed). What remains is gated on something outside the code: **RE-1b** on the next `khost.oob`
re-export, **RE-3b / RE-5 / RE-6** on Bob's art, per nation.
Art checklist (self-saving, checkboxes persist): https://claude.ai/code/artifact/a9d4cc37-a7c9-4a68-865c-2428d8f2969c

**⏸ THE COMBAT PASS — `Planning Docs/Implementing Combat.md` (opened 2026-08-24) resumes after icons.**
Four workstreams in one focus file: **audio tighten-up → combat animation → air operations → combat constants.**
The detail lives entirely in that file; this one keeps the thread-board row and the change-log lines only.
Start point is **AIR-0** for air (`AirThreatService` + the §24.7a.8 threat overlay — no gates, no new art) and
**A-2** for audio (Bob runs `Tools/Audio/Audit Catalog`; that output is a prerequisite, not a verification).
⚠ **Eight decisions are owed by Bob before parts of it can start** — §7 of that file, D3 in particular blocks AIR-0.

**⏸ P4 REQUISITION (`Planning Docs/todo_profiles.md`) is still queued and still unblocked** — the wallet and
atomic `SpendPrestige` are live and `CombatUnit.PurchaseCost` is the ratified basis. It was the ▶ pointer from
2026-08-20 until the combat pass superseded it; nothing about it regressed. Older ▶ arrows inside
`Planning Docs/todo_profiles.md` / `Planning Docs/todo_domains.md` headers are history, not directions.

⚠ **DOCS MOVED 2026-08-24:** every plan/pass/courier document now lives in **`Planning Docs/`**; the repo root
holds only `CLAUDE.md`, `Claude_Project.md`, `Claude_TODO.md` and `README.md`. See `Claude_Project.md` §1.1 for
the rule. Paths in this file were re-pointed in the same commit; paths *inside* `Planning Docs/` are bare
filenames and are correct as they stand.

**Pass ledger (newest first; each closed pass's detail lives in its plan file + the archive change log):**
- **2026-08-22 — AIR-DEFENSE RANGE, CLOSED** (committed `83d0f99`, suites green Bob-run; record
  `Planning Docs/todo_adrange.md`): the §11.8 engagement envelope now reads the authored IR ladder (was `PrimaryRange`
  = 1 on every AD unit — the whole ladder was dead data) + Bob's point-defense re-band (Chaparral/
  Roland/Crotale/Rapier/HQ-7 6→4; Tunguska STAYS 5 — trait-composed 3+2; Hawk stays 6). Ratified ladder
  in DesignDoc §11.8.2d; 9 envelope pins across the profile suites; 4 transit regressions. No re-pricing.
- **2026-08-22 — PRESTIGE / UNIT ECONOMY, CLOSED** (committed `14fa5d5`, suites green Bob-run; record
  §7 of `Planning Docs/Prestige_SovietEconomy_Analysis_2026-08-22.md`): Soviet-menu re-tiers (BTR-70 40 / BTR-80 100 /
  BRDM-2 AT 105 / Su-24 300 / T-80B 125; ACR explicit 250); NEW `CombatUnit.PurchaseCost` (Σ populated
  bays) = the ratified §18.3.1 whole-unit price and the V19 kill-bounty basis; §18.5.1 upgrade formula +
  §18.5.2 successor rule ratified; HS_DesignDoc §18 fully reconciled. Income side untouched (live).
- **2026-08-22 — ICM / FORMATION QUALITY, CLOSED** (committed `d6734f3`, suites green Bob-run; record
  `Planning Docs/todo_icm.md` rulings 1–11): closed-bay ICM doctrine (sole profile prices the FORMATION); 4 new
  formation-quality traits (M1 1.53 · Leo 2 1.47 · Challenger 1.33 · ACR 1.40 · M109/MLRS ×1.05); recon
  ruling (all six scouts fight Soft, `RECON_FRAGILE` dormant); artillery fixes (M109 +SMART, PHZ-89/
  Type 82 re-tiers, MJ arty prestige); 5 SP-gun templates ART→SPA. Open follow-ons: NATO late roster
  (Q2/Q3, never ruled) + 2 ICM outliers (export idiom, LOOKDOWN live-vs-dormant) — see the handoff.
- **2026-08-19 — THEME-ART** (committed `55587d2`): EU + CH map icons + hex tile sets, 9-arm `CreateMapIcon`,
  CN→CH prefix fix, all three terrain arrays baked (gitignored). ⏸ In-play verify gated on first non-ME export.
- **2026-08-17 — PRESTIGE/VICTORY, CLOSED** (`Planning Docs/todo_prestige.md`, all gates green incl. play): `VictoryLedger` +
  `PrestigeWallet` + §17.2/17.3 share grading + §17.8 mission-objective gate + §17.9 scenario end + §18.2
  per-turn income (capture awards RETIRED) + **SAVE_VERSION 7**. Khost runs fully scored on placeholders
  (editor-side E10 rebalance owed — see the ladder audit, `Planning Docs/Reply_LadderAudit_2026-08-17.md`).
- **2026-08-13 — CENSUS DOCTRINE v2, CLOSED** (`Planning Docs/todo_census.md`): all censuses own-platform-count, organic
  tanks on mech bases, lift censuses empty; machine-enforced by `CensusIntegrityTests` (6). ⚠ Do not re-open
  the carrier question in either direction without touching that guard (supersedes the 08-12 "leave them" ruling).
- **2026-08-12 — MAP-STANDARD**: map size per-scenario from the `.map` header, `MapConfig` geometry deleted,
  truncation throws, derived scroll bounds. `MapStandardTests` (14, green 2026-08-20).
- **2026-08-10/11 — DOMAINS D0–D3** (`Planning Docs/todo_domains.md`): domain vocabulary, post-hoc spotting (§12.4.4a),
  transit air defence (D2, play-confirmed), helo over-water grace (D3) + **SAVE_VERSION 6**. All suites green 2026-08-20.
- **2026-08-08/10 — PROFILE REBUILD P0–P3** (`Planning Docs/todo_profiles.md`): `EquipmentBays`, derived bay capacity, naval
  sealift, movement-medium rules + **SAVE_VERSION 5**. P4 is the remainder.
- **2026-08-03/04 — AUDIO REBUILD Phases 0–3** (`Planning Docs/todo_audio.md`): SFX as imported assets, catalog + facade +
  fog gate, battle-map sounds WIRED (movement/fire/impact/ambush/objectives/denied). Remaining audio work is
  clips + Bob's Inspector items + host-blocked sounds — see the thread board below.
- Earlier (weapon-rating migration, combat engine M0–M9, orchestrators, intel ladder, printer, AI0–AI2b,
  content pipeline 0/1/3/4, HUD pass): `Planning Docs/Claude_TODO_Archive.md` + `Claude_Project.md`.

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
| **▶ TOP-DOWN ICONS** | Ruled 2026-08-27 (Bob, command decision): ONE sprite per profile rotated to facing; bases don't rotate; helos keep names+flipbook (files are `_Frame0..5`, no directional suffix); ALL directional/firing variants + `flipX` path DELETED, no switch; suffix-strip scoped to `Unit Icons/` ONLY (86 bridge/river/chevron sprites KEEP suffixes); `GER_`→`GE_` fold. Plan FINAL in `Planning Docs/Top-Down Icons.md` — read its §1 trap (helo frames ride the firing fields) + §2.1 correction (firing art DOES render when dug in) first. | Agent: implement T-1…T-4 + 2 guard tests as one commit, NEXT SESSION. Bob: art renames (R6), play-verify per plan §6. ✅ §2.8b CLOSED 2026-08-28 — Bob is drawing a Cobra, so the AH-1 stops sharing Apache frames. |
| **⏸ ROSTER EXPANSION** | Opened 2026-08-28, **ALL ELEVEN NATIONS SETTLED 2026-08-29** (`Planning Docs/Roster Expansion.md`) — decisions complete, NO code yet. Per-nation art + profiles; generic artillery/AAA art abolished; `AR_` convention killed; Saudi added as a real nation; Kuwait deliberately gets nothing. **Architecture settled: one profile per picture, NO icon picker** (§1.1 — do not re-propose). Carries a 7-type rename batch (one `SAVE_VERSION` bump + one `khost.oob` re-export), 4 content errors, and a NEW `SECOND_LINE_FORMATION` ICM trait for China. Art checklist published as a self-saving Artifact. | ✅ **RE-1a / RE-2 / RE-3a / RE-4 all landed** — the art-free half of §5 is complete (RE-4 2026-09-02: `SECOND_LINE_FORMATION` on 15 Chinese profiles, Type 86 IFV excluded by closed-bay doctrine; suite run owed). Remaining agent work is art-gated per nation (RE-3b/RE-5/RE-6) or content-gated (RE-1b, next `khost.oob` re-export). All §6 open questions are now CLOSED. Bob: draw per the checklist. |
| **COMBAT PASS — audio · animation · air ops · constants** | ▶ NEXT, opened 2026-08-24 (Bob's direction). Plan DRAFTED in `Planning Docs/Implementing Combat.md`, no code yet. Air RULES built + tested and the transit half is live; the AOB/GAME half is unwired. Combat has NO visual at all. 49 sounds declared vs 14 catalog rows. Constants are solid per Bob — light touch only. | Bob: 8 decisions in §7 of that file (**D3 blocks AIR-0**), + run `Tools/Audio/Audit Catalog` (A-2) and the D-1 combat baseline play. Agent: AIR-0 and A-1 need nothing. |
| **Requisition (P4)** | ⏸ QUEUED — was ▶ NEXT until the combat pass superseded it 2026-08-24; nothing regressed. Wallet + atomic spend LIVE (08-17); pricing rules RATIFIED §18.3.1/§18.5.1 + `CombatUnit.PurchaseCost` LIVE (08-22, prestige pass); bay buy/sell/upgrade API + UI unbuilt. | Agent: build per `Planning Docs/todo_profiles.md` P4 (§4.7 header carries the rules — do not re-derive prices). No gates. |
| **Campaigns + Save/Load** | Pipeline Phase 2 paused CLEAN, all decisions settled. Campaign folders invisible to discovery; `SaveLoad` has ZERO callers — no Save button exists. | Agent: resume trio in OPEN WORK. Cost grows per mission authored (25–30 planned). Menu listing is Bob-gated (prefab). |
| **M13 — turn loop / air missions / AOB** | The big frontier. **NEW 2026-08-27: the AI RESERVE ruling (§20.2.1) is ratified and unblocks the reinforcement-arrivals item** — arrival = enters the AI's Reserve (no timer, retry cap moot); staged build does not wait on the AI brain. Air RULES built + tested; air GAME unwired. Turn loop is straight-through; reaction yields are a day-one requirement (retrofit = rewrite). ⚠ **The AIR half is now phased inside the combat pass** (`Planning Docs/Implementing Combat.md` §5, AIR-0→AIR-4) — the turn-loop half stays here. | Agent-led, large. Gates: D4, I8, most printer emitters, M14 remainder, D2 fixed-wing play-verify all sit behind it. ⚠ Finding 2026-08-24: **AIR-2 does NOT need the reaction-yielding loop** — in Khost the reaction windows belong to an AI with nothing to fly, so v1 never suspends. |
| **Audio** | System + policy + wiring DONE through Phase 3. **49 `SoundEffect` members vs 14 catalog rows vs 11 wavs** — ~35 declared sounds have no row. Battle-HUD buttons silent. Two carried-forward defects open (JsonPolicy violation; briefing-absent logged as an exception). ⚠ **Now workstream A of the combat pass** — plan in `Planning Docs/Implementing Combat.md` §3; `Planning Docs/todo_audio.md` remains the system's design record. | Bob: run `Tools/Audio/Audit Catalog` (A-2, a prerequisite), rule D1/D2/D8, author wavs (helo/jet long cuts too), put `UIButtonAudio` on HUD buttons. Agent: A-1 + A-3 need nothing. |
| **Leaders (L1–L4)** | Combat mechanics LIVE (M14 slice). Awards engine, pool/recruitment, details UI all unbuilt. Recruitment economy UNBLOCKED by the wallet (08-17). | Agent: L1+L4 approved + headless-safe, can start anytime. Art dependency: portraits + deco layers (Bob). |
| **AI** | AI0–AI2b live (board analysis, EV oracle, honest-spotting belief store). AI takes no turn yet. | Agent: AI3+ per `Planning Docs/Claude_AI_TODO.md` (irregular doctrine first, for Khost). AI2 state still owed a SAVE_VERSION ride. |
| **Domains: naval + D4** | D0–D3 CLOSED. D4 fixed-wing staging gated on M13/AOB. N0–N3 designed (`Planning Docs/todo_domains.md` §F/§H), suite-verifiable, unplayable without a coastal map. | Bob: coastal test map when convenient. N3 additionally gated on §15 supply. |
| **Intel** | Six-rung ladder LIVE + play-confirmed. Open: I7 HQ SIGINT sweep (= M15, unblocked, slot-in-anywhere), I8 RB tiers (M13-gated). | Agent: I7 whenever convenient. Deferred skill re-homes need Bob ratification. |
| **Printer / dispatch feed** | CRT + emitters LIVE for every existing host. Owed: P5 ledger persistence (SAVE_VERSION bump), P8b tests. Air/logistics/leader emitters host-blocked. | Agent: P5 persistence is small and self-contained. §11.7.2 evac revision awaits Bob's eyeball. |
| **Supply (§15)** | Designed; `ProcessUpkeep` is a stub. **NEW 2026-08-24: the SUPPLY UNIFICATION is this thread's groundwork** — Bob's one-number ruling (every unit ONE `DaysSupply`, caps 5/30/size; the `StockpileInDays` split was the code's invention, §15 never had it). Survey DONE, plan drafted (`Planning Docs/Supply Unification.md` — cheap: the depot API has zero callers; carries SAVE_VERSION 9 + a free fix for the latent depot-reloads-full snapshot bug). **SUP-1 CLOSED 2026-08-24 — suite GREEN + Khost play-confirmed (Bob-run): one `DaysSupply` pool per unit, SAVE_VERSION 9, fixed-wing Max 0.** The model is settled; §15 is now genuinely "deduct points." Gates N3, HCL decay/recovery, logistics dispatches, depot REP award. | Agent: §15 as its own pass when scheduled — `ProcessUpkeep` chain (§3.5.4-.6), §15.4a resupply (`OnResupplyRequested` has zero subscribers), §15.5.3 OOS incl. the airbase-keyed fixed-wing arm. No blockers besides size. |
| **Weather** | Single-state Clear. Rich model deferred by design ("revisit before ship"). Several built rules dormant until it exists. | Design pass first (Bob + doc), then code. Nothing else gated on it except the dormant rules. |
| **Mission Pack (content reorg)** | NEW 2026-08-21. Plan DRAFTED (`Planning Docs/todo_missionpack.md`, awaiting Bob's review): single `.mission` JSON pack (manifest+briefing+map+oob+aii sections), manifest in-pack, unified MapSection = a SAVE_VERSION ride, clean break, thumbnails/narration loose, `Mission`/`Scenario`/`Campaign Mission` semantics ratified. ⚠ NOT shared with the editor yet. | Bob: review plan; define the AII schema (the ⌛ gate for the editor spec + serious build). Agent: §7 update hook is bounded. |
| **Content / editor coordination** | Khost re-priced editor-side (s0 0.302, ladder .38/.47/.56, 7/7 rungs). C7 landed game-side (SAVE_VERSION 8). ⚠ E15 + the Khost manifest re-export are SUPERSEDED by the Mission Pack plan (fraction ships inside the re-exported packs) — relay is Bob's, AFTER the AII schema lands. Hamburg (44×21 EU) in authoring. | Bob: hold the editor off E15/re-export dead work. Game-side: EU/CH ⏸ verify fires on their first export (will likely arrive as a pack). |
| **Ship-blockers (small, must not be forgotten)** | Tilde (~) reveal cheat in `GameIconRenderer`. ✅ The `AudioSettings` JsonPolicy violation is FIXED 2026-08-24 (new `JsonPolicy.Settings`; ⚑ rides the fix-batch suite run). | Agent: the tilde cheat is a quick delete before any external build. Tracked in Cleanup. |

---

## BOB'S QUEUE (nobody else can do these)

- [ ] **COURIER READY TO SEND: `Planning Docs/Reply_AIReserve_to_EditorAgent_2026-08-27.md`**
      → the Scenario Editor agent. Tells them the blocked-arrival **retry cap is MOOT** (you ruled the
      model instead of the number — §20.2.1), banks their two yeses, and suggests they DROP the
      arrival-overlap validator cases they'd planned (the collision those guard cannot occur once arrival
      means "enters the Reserve"). Their `maxTurns` warn survives. Nothing in it needs an answer unless
      they disagree. ⚠ The earlier `SupplyContract_Reply2_…` courier is ANSWERED — no need to send it
      again; this one supersedes its open items.
- [ ] **⚠ ONE DECISION STILL YOURS — where may the AI deploy FROM its Reserve? (design doc §20.2.1.4)**
      Not blocking anything; the staged build works under any answer. But it decides content work:
      **Khost authors 14 deployment zones and all 14 are player-controlled**, so the player's rule
      (§35.3.8.1) gives the AI ZERO legal hexes. (a) author AI-side zones on every scenario — real editor
      work; (b) the unit's authored `.oob` hex is its entry point — no new content, expresses
      reinforcement axis (agent leans here); (c) free choice in friendly territory. Answer whenever.
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
- [~] **Tell the Scenario Editor agent G1 HAS LANDED** — FOLDED INTO the drafted supply courier (§6, last
      bullet), flagged there as YOUR call whether the "once a build ships with it" trigger has fired. Do not
      send separately; close this when the courier goes.
- [~] **Relay to the Scenario Editor agent — FOLDED INTO the drafted supply courier (§6); do not send
      separately, close this when the courier goes.** (`Planning Docs/ScenarioEditor_Status_2026-07-28.md`
      covers most of it):
      (a) checksum decision SETTLED — header field stays as their fingerprint, game never validates;
      (b) `classificationName` green-lit for removal; (c) leaders can go name-form;
      (d) briefing narration is CAMPAIGN-SCENARIO ONLY (§20.4.2) — missing narration is normal, not an error;
      (e) always say WHICH KIND of scenario (§20.4.1). ⚠ Check whether the 08-14 census courier already
      carried any of this before re-sending.
- [~] **Possibly still owed to them: `JsonPolicy.cs`** — FOLDED INTO the courier (§6) as an offer rather
      than an attachment; send the file if they ask. Low urgency; they inferred the one property that matters.
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

- [!] **RE-4 SECOND_LINE_FORMATION — BLOCKING, landed 2026-09-02.**
      **DO:** compile, then run the full EditorTest suite — especially `WeaponProfileChineseTests` (now 8,
      two of them new) and `CommodityProfileTests` (now 7).
      **PASS:** suite green. The Chinese ICM pins MOVED on purpose — 0.90 everywhere, 0.945 for the Type 80,
      **1.00 for the Type 86 IFV**. If the Type 86 comes back 0.90 the trait has leaked onto a Mobile-bay
      profile and the formation is being priced twice; fix the profile, not the test.
      ⚠ If a NON-Chinese suite reports a moved number, the trait has leaked through a shared commodity
      blueprint — `FormationQuality_SecondLineIsChinaOnly` is the tripwire for exactly that.
      **WHY:** the four blueprints now take `params WeaponTrait[]`, so for the first time a national trait
      rides a shared stat line. This is the run that proves it rides ON it rather than forking it.

- [⏸] **RE-2 FREE RE-POINTS — four sprites, PLAY-CHECK ONLY, ride the T-5 art verification.**
      Landed 2026-08-29. No suite value: `IconIntegrityTests` proves an icon is present and valid, not that
      it is the RIGHT one, so this is eyes-only.
      **DO:** look at a US air-mobile brigade, a Chinese towed artillery regiment (light and heavy), and the
      Soviet generic AAA regiment.
      **PASS:** the US air-mobile unit no longer wears the AIRBORNE sprite · the two Chinese artillery units
      draw Chinese art, not the generic · Soviet AAA draws `SV_AA`.
      **WHY:** all four sprites were sitting on disk unused while the profiles drew something else. Nothing
      new was authored — this is purely pointing at art that already shipped.

- [⏸] **ICON PASS — final art verification, GATED ON BOB'S T-5 ASSET DROP.**
      ✅ Suite green, Khost loads clean, and **rotation confirmed working on the pre-pass art** (Bob,
      2026-08-29) — so the mechanism is proven and `ICON_HEADING_OFFSET_DEGREES = 0f` is correct for
      west-facing art. What is left fires when the new sprites land.
      **DO:** after dropping the converted art, load Khost and turn units to EASTERLY facings.
      **PASS:** no magenta and no mismatch placeholder anywhere · vehicles point the right way at all six
      facings · **HQs/depots/airbases upright** · and on an easterly facing the HP box, nationality flag,
      deploy chevron and stacking badge are still upright and readable.
      **WHY:** easterly is 180° of rotation — the only facing that exposes a root-rotation bug, which a
      north-facing unit hides completely. And if the NEW art has a different canonical heading than the
      old `_W` sprites, this is where it shows: fix `ICON_HEADING_OFFSET_DEGREES`, not the geometry.
      ⚠ Agent owes a directory cross-check against the manifest at the same time (Bob's ask, 2026-08-29).

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

### ▶ THE COMBAT PASS — audio · animation · air ops · constants (`Planning Docs/Implementing Combat.md`)
**The live frontier, opened 2026-08-24.** Four workstreams, deliberately in one focus file so this one does not
have to carry them: **A** audio tighten-up · **B** combat animation · **C** air operations (phased AIR-0→AIR-4;
only AIR-0/AIR-1 and the start of AIR-2 are in scope this pass) · **D** combat constants. Recommended order is
A → B → C with **D kept in its own session** — every combat-constant change forces a `CombatOracleTests` re-run,
and bundling one into an animation commit means a red suite that could be either.
⚠ **The one architectural point:** B and C need the same missing thing — a presentation step gameplay can hand
control to and get back. §11.1.8.6 makes reaction yields a day-one requirement and warns *retrofit = rewrite*, so
**B ships the suspendable sequencer that C's reaction windows reuse.** Do not build a fire-and-forget FX layer.
⚠ **Eight decisions are owed by Bob** (§7 of that file). **D3 — the §11.4.4 "spotting +" term — blocks AIR-0**,
and it is the term the AD range pass deliberately deferred to `AirThreatService` so the overlay and the walk
could not disagree. Everything else in this section is unchanged and unblocked.

### P4 — REQUISITION (`Planning Docs/todo_profiles.md`) — ⏸ queued, still unblocked
The bay purchase/upgrade economy on top of `EquipmentBays`: buy/sell/upgrade API (headless), prestige pricing
(`PrestigeCost`/`TurnAvailable` fields exist on every profile), and the purchase UI surface. The wallet and
atomic `SpendPrestige` are LIVE (2026-08-17) — this pass finally has a currency. Spec: `Planning Docs/todo_profiles.md` P4
+ DesignDoc §18. ⚠ P5 (content/docs) was merged into the domain doc pass; do not resurrect it separately.

### CONTENT PIPELINE Phase 2 — campaigns (PAUSED 2026-07-28, clean; full detail `Planning Docs/todo.md`)
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
- [ ] **⭐ AI RESERVE + REINFORCEMENT ARRIVALS — RULING RATIFIED 2026-08-27 (Bob); design doc §20.2.1
      NEW. Ready to build; no gates.** Supersedes the 2026-08-24 scripted-arrival framing entirely.
      **The model:** AI reinforcements arrive **into the AI's Reserve**, not onto a hex; the AI chooses
      when and where to commit them. Both sides now hold undeployed forces in a **Reserve** — that is the
      vocabulary on both sides (Bob 2026-08-27: "deployment box" is the UI surface, NOT a second concept;
      do not let a parallel name grow). Not a new container either: §11.7.2.4 already evacuates aircraft
      to "the owner's Reserve" for player AND AI.
      ⚠ **The blocked-arrival RETRY CAP is MOOT and must not be reintroduced** (§20.2.1.1) — a Reserve has
      no timer, so a unit whose hex is busy simply waits. That question is closed, not deferred.
      **Build list:**
      - `.oob` gains **`ArrivalTurn`** (PascalCase, matching every other field; default 0 = present at
        start, so no existing content is touched). Editor has accepted the casing and is ready to author.
      - A **Reserve container** per side, holding units off-map with no timer. Undeployed Reserve units
        are NOT lost at scenario end — they return to the roster (§20.2.1.6; carryover is campaign-gated).
      - **Arrival idempotency = a persisted fired-set of unit IDs** in scenario state ⇒ **its own
        `SAVE_VERSION`**. ⚠ Do NOT use the loss ledger to answer "did this already fire?" — that ledger is
        itself unpersisted (P5), so it would be one unpersisted structure vouching for another. ⚠ Take the
        bump while save/load still has zero UI callers and bumps are free.
      - **Staged build (§20.2.1.3), so this does NOT wait on the AI brain:** v1 places an arriving unit at
        its authored `MapPosX/Y` when free and HOLDS it in Reserve when not — behaviour-identical to the
        old model in the normal case, no retry cap, upgrades to real AI judgement with no format or
        content change.
      ⚠ **ONE OPEN SUB-QUESTION, Bob's (§20.2.1.4): where may the AI deploy FROM Reserve?** The player's
      rule (§35.3.8.1, friendly-controlled `IsDeploymentZone`) does NOT transfer — **verified 2026-08-27:
      Khost authors 14 deployment-zone hexes and ALL 14 are player-controlled**, so applying it to the AI
      yields ZERO legal hexes. Candidates: (a) mirror §35.3.8.1 + author AI-side zones (content work on
      every scenario); (b) the unit's authored `.oob` hex is its entry point — no new content, and it
      expresses reinforcement AXIS (agent leans here); (c) free choice in friendly territory. Not blocking
      — the staged build is compatible with all three.
      ⚠ Scope: **AI/OPFOR only.** Player reinforcement is the Reserve purchased per §35.4 and brought on
      via §35.3.8 — a player never receives a scheduled arrival.
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

### DOMAINS — D4 + naval N0–N3 (`Planning Docs/todo_domains.md` §H is the plan; §I the editor relay list)
D0–D3 CLOSED (see pass ledger). Remaining ladder:
- [ ] **D4 — fixed-wing staging.** GATED ON M13/AOB — Bob 2026-08-11: "the mechanisms to run air missions are
      not in the game yet." D2's fixed-wing half (1d6 detection, transit AD vs jets) stays play-unverified
      until then; the code paths are suite-covered.
- [ ] **N0–N3 — naval foundations → naval combat + sea clock → port heavy lift → supply hooks.** Designed from
      Bob's five precepts; `FacilityType.Port` exists. Suite-verifiable but NOT playable until a coastal test
      map exists (Khost has no water); N3 additionally ⛔ gated on §15 supply. ⚠ There is NO hex-by-hex sea
      movement — §5.4.2.3 makes naval movement an instant port-to-port jump; do not add one.

### AI track → `Planning Docs/Claude_AI_TODO.md`
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
- [x] ✅ **`AudioSettings` JsonPolicy violation FIXED 2026-08-24, suite green (Bob-run same day)** — NEW third named policy
      `JsonPolicy.Settings` (player-written flat tree, lenient read); AudioSettings reads AND writes through
      it, and the same sweep repaired `RiverSymmetryVerifier` (its local options had no string-enum converter
      — broken against every name-form map since 2026-07-28; now reads via `JsonPolicy.Content`).
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
> older than the last two passes migrate to `Planning Docs/Claude_TODO_Archive.md` when this section is pruned.
> **Entries 2026-07-21 → 2026-08-19 (incl. the theme-art pass and everything before it) are in the archive.**

- 2026-09-02 — **RE-4 `SECOND_LINE_FORMATION` LANDED (roster expansion). ⚑ Suite run owed.** The
  formation-quality layer (§13) gains its first sub-1.0 member at `Icm(0.9f)` and **China leaves the
  ICM-1.0 baseline** it had shared with the Soviets and the Arabs. Applied to **15 of the 16 Chinese
  profiles** — every one a template names in its DEPLOYED bay. ⚠ **`IFV_TYPE86_CH` is deliberately
  excluded:** it exists only as the Mobile-bay ride of the mechanised regiment, and closed-bay doctrine
  prices the formation ONCE, on the sole/deployed profile. Result: China 0.90, Type 80 0.945 (LRF
  1.05 × 0.9), Type 86 unchanged at 1.00.
  ⚠ **The RE-3a blueprints had to learn an optional trait argument.** `ART_LIGHT_CH` and `ART_HEAVY_CH`
  resolve from methods shared with the Soviet, NATO and Arab guns, so a new `Plus()` helper lets a national
  trait ride ON the shared stat line instead of forking it — all four blueprints now take
  `params WeaponTrait[]`. A new test pins that the Chinese gun keeps the Soviet stat line while carrying a
  different ICM; another is the tripwire for the trait leaking into a blueprint's base list.
  Docs amended in the same commit: `HS_DesignDoc` §7.5.5 and `WeaponTrait_Supplement` §13b (new T91 row).
  The supplement's "Soviet/**Chinese**/Arab formations stay the ICM-1.0 baseline" anchor was a direct
  contradiction of this ruling and is retired in place, not left standing.
  ⚠ **FOUND IN PASSING: `SPA_TYPE83_CH` has a profile but NO template** — the only Chinese profile
  nothing fields. It took the trait on deployed-intent; the missing SPH regiment is booked as RE-6 work.
  **Bob-side follow-ups unchanged:** RE-1b still rides the next `khost.oob` re-export; RE-5+ stays
  art-gated per nation.
- 2026-09-02 — **HUEY OWNERSHIP SETTLED at FOUR sprite sets (docs + manifest only, no code).** Bob:
  the US and Saudi each own a transport + gunship pair; **NATO generic borrows the US art** (its profiles
  stay its own — R1 permits sharing in that direction); **Iran owns `IR_UH1`** because its Hueys are a
  desert reskin; Germany keeps `GE_UH1D` and is now its only rider. 24 new frames, down from the 36 the
  first cut implied. ⚠ Transport and gunship are the long-body UH-1H and the **short-body UH-1C** — two
  different airframes, not a repaint. Recorded without objection: no NATO-generic nation (NL/BE/DK) ever
  flew a Huey, and the 1980s US gunship was the Cobra, so the pair is an older/cheaper tier rather than a
  historical fielding. Iran's gunship slot stays empty on purpose — its attack helo is the AH-1J.
  **Naming question closed:** the `XX_XXXX` (NATION_PLATFORM) rule governs SPRITE names and `SpriteManager`
  constants ONLY; WeaponType names keep the existing `ROLE_PLATFORM_NATION` form. The two conventions run in
  opposite directions on purpose — do not "fix" either to match the other.

- 2026-08-27 — **TOP-DOWN ICONS ruled + planned (Bob, command decision; plan `Planning Docs/Top-Down
  Icons.md`, FINAL; docs only, no code):** unit art moves to AI-generated top-down sprites — one per
  profile, rotated to facing (bases excepted), directional + firing variants deleted outright, no
  fallback switch. Scan findings worth the ledger: helo frames are stored in the DIRECTIONAL+FIRING
  fields (naive `_F` deletion kills the flipbook — the trap the plan's T-2 ordering exists for);
  `GetAnimationFrame` has zero callers (flipbook is sprite-name-based, so `RegimentIconProfile` collapses
  to ONE `Icon` field); §2.1 self-correction — firing art DOES render (dug-in postures), the cost of
  dropping it is a variant covered by the deployIcon chevron; suffix-strip is scoped to `Unit Icons/`
  (bridge/river/chevron sprites are hex-EDGE features and keep suffixes); no test touches icons at all,
  so two guard tests ship with the work. Counts verified against the 184 registered profiles. Soviet + MJ
  art converted Bob-side; implementation is next session's first task.
- 2026-08-29 — **RE-3a COMMODITY BLUEPRINTS (refactor, zero behaviour change). ⚑ CLEARED 2026-09-02 (Bob ran it): suite GREEN.** Four
  blueprint methods in `WeaponProfileDB` — `LightTowedArtilleryDef` / `HeavyTowedArtilleryDef` /
  `TowedAaaDef` / `TransportTruckDef` — and **12 profiles re-expressed through them** (4 light, 4 heavy,
  1 AAA, 3 truck). The three byte-identical light-artillery copies whose own comments admitted it ("the
  same gun for anyone") are now one authored line. NEW `CommodityProfileTests` (6): every national member
  of a family must resolve to ONE stat line · only the light gun keeps `AirDroppable`/`HeloTransportable`
  (the tags `EquipmentBays.CanAccept` routes airborne artillery by) · and the two **Mujahideen irregular
  variants must stay DIFFERENT** — folding them onto the blueprint would erase what makes them Mujahideen.
  Turn stays a per-nation parameter (Soviet artillery is authored at 60, everyone else 144).
  ⚠ **RE-3b RE-ORDERED — minting the ~28 national commodity profiles is NOT art-free after all.** Two
  findings: each needs a `SpriteManager` constant, and **a census is genuinely NATIONAL and cannot come
  from a blueprint** — `ART_LIGHT_NATO` is 950 men + `ART_105MM_FG` + `APC_HUMVEE_US`, `ART_LIGHT_ARAB` is
  700 + `APC_MTLB_IQ` + `MANPAD_STRELA`. A Saudi census names `APC_M113_SA`, which RE-5 mints. So RE-3b
  goes per-nation alongside RE-5, not as one batch ahead of it.
- 2026-08-29 — **RE-2 free re-points (4 sprites, no new art).** `INF_AM_US` off `US_Airborne` onto
  `US_AirMobile` · `ART_LIGHT_CH`/`ART_HEAVY_CH` off the generics onto `CH_LightArt`/`CH_HeavyArt` ·
  `AAA_GEN_SV` off `GEN_AA` onto `SV_AA`. All four sprites had been shipping unused while the profiles drew
  something else. ⚠ The `SV_AA`→`SV_AAA` and `GEN_*` retirements are ART renames and belong to T-4/T-5 —
  this step only re-points at files that exist TODAY, so it is safe ahead of Bob's asset drop. The Iranian
  AAA re-point is NOT here: it needs `AAA_GEN_IR`, which RE-3 mints. Play-check only, no suite value.
  ⚠ Post-conversion orphan sweep run: ~250 surplus `_NW`/`_SW`/`_F` constants now reference nothing, which
  is exactly T-4's deletion list and confirms its scope. `FR_Gepard` and `CH_Type95` are gone from it.
- 2026-08-29 — **RE-1a RENAME BATCH LANDED (roster expansion; `SAVE_VERSION` 9 → 10). ⚑ CLEARED (Bob ran
  it): suite GREEN, Khost loads fine.**
  20 WeaponType renames, 169 replacements across 6 files. The two Tornado types now name the nation each
  already served (GR.1 is the RAF designation for the IDS — the profiles were authored as two BRITISH
  variants and the German squadron borrowed the spare), with designations and art pointers swapped and
  **no stat line moved**. `SPA_M109_FR`→`SPA_AUF1_FR`; `APC_FV432`/`HEL_AH1` gain nation suffixes; the
  three `WEST` types become `_NATO`; thirteen Chinese types gain `_CH`, two with a model correction —
  `SPA_TYPE82`→`SPA_TYPE83_CH` (the Type 82 is a 130mm MRL, the SPH is the PLZ-83) and `HEL_H9`→`HEL_Z9_CH`
  ("H" is Hongzhaji, bomber; the real aircraft is the Harbin Z-9). **Deleted:** `TANK_TYPE95` outright
  (fictional — the enum comment already admitted it), plus two content-error templates —
  `UK_AIR_DEFENSE_REGIMENT` (Britain never operated the M163) and `IR_SAM_REGIMENT` (Iran never operated
  the SA-2) — and six `FR_Gepard` constants. `HEL_AH1_US` moved out of the enum's Non-Profile region,
  where it had been misfiled despite having a real profile. Ends at 183 profiles / 183 templates /
  212 WeaponType members, no duplicates, no dangling references.
  ⚠ **`TRN_AN8_SV` deliberately excluded** — verified as the ONLY renamed type present in shipped content
  (`khost.oob`), so it becomes RE-1b and rides the next editor re-export. That check is what let the rest
  proceed without touching content, the editor, or Bob's art.
- 2026-08-29 — **TOP-DOWN ICONS T-1…T-3 + guard suite LANDED; ⚑ CLEARED (Bob ran it): full EditorTest
  suite GREEN and Khost loads clean.** Unit art is now ONE sprite per profile, rotated to facing.
  `RegimentIconProfile` six fields → one `Icon`; `GetDirectionalIcon`/`GetFiringIcon`/`GetAnimationFrame`
  deleted (all three had ZERO callers outside the class, as the plan predicted); `RegimentIconType` down to
  two members. All **184** declarations converted mechanically, counts matching the plan exactly
  (89 Single + 53 Directional + 32 Directional_Fire + 10 Helo → 174 Single + 10 Helo). ✅ **The §1 trap did
  not bite** — all ten helos verified still on their `_Frame0` sprite. Renderer: `NormalizeDirection`,
  `ShouldFlipSprite`, the `out bool shouldFlip` and both `flipX = true` writes deleted; NEW
  `ApplyIconFacing` rotates **`unitIcon` only, never the prefab root** (six renderers) and gates on
  `unit.IsBase`. NEW `GameData.ICON_MOTION_FRAME0_SUFFIX` — the prefab's suffix was `private` so the
  headless validator could not reach it; rather than spell it twice it moved to GameData and the prefab
  aliases it const-from-const, so the compiler enforces agreement. NEW `IconIntegrityTests` (8) closes the
  §2.5 finding that NOTHING in the suite referenced icons; a pure `internal static IconRotationDegrees`
  seam was extracted so the rotation rule tests with no MonoBehaviour. Net −806/+485 lines.
  ⚠ **T-4/T-5 deliberately NOT done and they land TOGETHER** — renaming the ~250 `SpriteManager` constants
  breaks every lookup until the PNGs are renamed, so profiles still point at the pre-pass `_W` constants,
  which resolve fine and are now rotated. ⚠ `ICON_HEADING_OFFSET_DEGREES` assumes a WEST heading; rotation
  play-check still queued.
- 2026-08-28 — **ROSTER EXPANSION opened (docs only, no code; plan `Planning Docs/Roster Expansion.md`).**
  Bob authoring new unit art surfaced three things: 11 shared sprite sets (17 profiles on borrowed art),
  real roster holes per nation, and **four content errors** — a UK M163 Vulcan regiment (Britain never
  operated it; template DELETED), an Iranian S-75 SAM regiment (Iran was Hawk/Rapier-equipped), Iranian AAA
  drawing Soviet art, and a Chinese S-125 regiment (China's SAM was the HQ-2). **Architecture settled after
  Bob raised an icon picker: profiles for everything, no picker** — art resolves through the profile alone
  (`GetSpriteNameForUnit` → `EquipmentBays.GetIcon` → `IconProfile`, AIRB the only special case), the
  per-nation split ALREADY exists everywhere else (4 Leopard 1 profiles on 1 sprite), a picker would give
  art a second home right as the icon pass collapses `RegimentIconProfile` to one field, and only separate
  profiles can carry per-nation export stats. Commodity stat lines get authored ONCE in a shared
  `ProfileDef` helper — the three light-artillery profiles are already byte-identical copies today.
  Naming ratified (sprites `NATION_WEAPON`, types `CATEGORY_MODEL_NATION`, towed AAA sprite `XX_AAA`,
  `AR_` killed, Saudi = `SA_`); casing deliberately NOT normalised. Rename batch approved (7 types incl.
  `TRN_AN8_SV`→`TRN_AN12_SV` and the two Tornados) — ⚠ **An-8 is a RENAME not a delete**, it is the only
  fixed-wing lift for all four VDV templates. **Tornado fix: the art pointers were always right, the two
  TEMPLATES were crossed** (GR.1 is the RAF name for the IDS; Bob self-corrected twice) — swap the profile
  assignment, move no stat lines. Verified `.oob` references WeaponTypes not template IDs, so template
  renames are content-free. Sequenced AFTER the icon pass. Six nations still to specify with Bob.
- 2026-08-27 — Khost `.oob` ownership loop CLOSED: Bob hand-moved the editor's bundle; our copy now
  carries the editor writer's bytes (`30` not `30.0`, exactly the 54 DaysSupply lines). One SHA end to
  end from here. The `khost.oob.pre-supplydays-2026-08-27` backup rode along into StreamingAssets — kept
  (Bob's file); delete when he confirms, it ships in builds otherwise.
- 2026-08-27 — **AI RESERVE RATIFIED (Bob) — NEW DesignDoc §20.2.1 + §35.3.8 cross-ref; docs only.**
  AI reinforcements arrive INTO the AI's Reserve, not onto a hex; the AI chooses when and where ("the AI
  will choose the best place"). §20.2's scripted-arrival reading is SUPERSEDED. Consequences recorded:
  the blocked-arrival **retry cap is MOOT** (§20.2.1.1 — a Reserve has no timer; must not be
  reintroduced), the container is not new (§11.7.2.4 already gives player AND AI an "owner's Reserve"),
  **vocabulary is RESERVE on both sides** (Bob's own call — "deployment box" is the UI surface, not a
  second name), and a **staged build** (§20.2.1.3) lets the machinery land without the AI brain: place at
  the authored hex when free, hold in Reserve when not. Undeployed Reserve survives scenario end
  (§20.2.1.6). ⚠ NEW OPEN sub-question §20.2.1.4 — where the AI may deploy FROM Reserve — with the
  finding that forced it: **Khost's 14 deployment zones are ALL player-controlled**, so the player's
  §35.3.8.1 rule gives the AI zero legal hexes. M13 arrivals item rewritten to the ratified model
  (fired-set + own SAVE_VERSION; loss-ledger route rejected as circular). Both editor concerns came back
  YES — ownership ratified (Bob bundles, moves, one SHA end-to-end) and the drift trigger accepted, with
  the editor adding a read-side parity check that re-derives our cap table from `GameData.cs` — so those
  four symbols are now a contract with their harness and a rename is a courier event.
- 2026-08-27 — EDITOR REPLY RECEIVED + REPLY-2 DRAFTED (`Planning Docs/SupplyContract_Reply2_to_
  EditorAgent_2026-08-27.md`; docs only). **Supply contract CLOSED both sides** — editor adopted real-days
  end-to-end (36/36 harness + live pass), mirrored the caps/fixed-wing set/clamp/tripwire, re-authored
  Khost through its own writer (SHA 3be5eaf6… across their 3 copies, values matching our §4 table), and
  confirmed Hamburg was never exported on the old contract. **§5 RATIFIED BY BOB: `HitPoints` stays a
  ratio** ("I do not want the player counting hit points") — their decoupling argument (HP maxes are
  balance knobs; a real-HP file couples every .oob to a tunable) is stronger than the "wart" framing we
  sent, accepted as such. Backlog closed: `classificationName` already removed write-side (verified our
  end — khost.oob carries none and loads clean), `JsonPolicy.cs` receipt CONFIRMED, **G1/E3 shipped
  2026-08-12** so our "Bob's call" flag was stale. Cross-checked their AIRB-carries-DepotSize-Large trap
  against our data: we carry it too (both Khost airbases) and are guarded twice (ctor tests AIRB before
  any depot fallthrough; SetDepotSize early-returns on non-depot FacilityType) — both load at 30.
  Reply-2 raises the three items Bob delegated to the agents: content ownership, rule-table drift, and a
  substantive `arrivalTurn` proposal. Residual for Bob: the blocked-arrival retry cap.
- 2026-08-24 — EDITOR COURIER DRAFTED (`Planning Docs/SupplyContract_to_EditorAgent_2026-08-24.md`,
  docs only): the `.oob` supply contract change, written for the editor agent — §1 real-days ladder
  (blocking Hamburg; an old-contract export loads SILENTLY with every unit at ≤1 day), §2 `StockpileInDays`
  rescission + why the split was wrong, §3 fixed-wing 0 incl. the helo and TRN-profile-vs-classification
  traps, §4 the re-authored khost.oob values (verified: 54 = 8 depot + 2 airbase + 4 fixed-wing + 40),
  §5 the open `HitPoints` ratio-or-real question back to them, §6 the older backlog bundled so it stops
  accumulating, §7 an explicit do-not-build-yet list (Mission Pack pending the AII schema; `arrivalTurn`
  pending its own SAVE_VERSION decision). Three older Bob's-queue relay items marked `[~]` folded-in so
  nothing is sent twice.
- 2026-08-24 — ⚑ CLEARED (Bob ran it): SUP-1 suite GREEN + Khost panel play-confirmed across the board
  (regiment 5/5 · airbase 30/30 · cache 30/30 · depot 80/80 · Su-17 0/0, units moving normally — the
  fixed-wing `CanMove` guard verified in play, which was the silent-failure risk). **SUPPLY UNIFICATION
  PASS CLOSED**; the one-number model is the settled foundation §15 builds on.
- 2026-08-24 — SUP-1 SUPPLY UNIFICATION LANDED (Bob's go; **SAVE_VERSION 9**; record `Planning
  Docs/Supply Unification.md` §3): one `DaysSupply` pool per unit — depot Max = size cap via `SetDepotSize`
  (single sizing authority; upgrade refill preserved), airbase 30, **fixed-wing 0** + the `CanMove`
  `Max > 0` guard; `StockpileInDays` + both band-aids DELETED; ten depot methods re-pointed; SnapshotMapper
  depot force-reset deleted (half-spent-depot-reloads-full bug dead); loader field/block deleted +
  warnings re-worded; khost.oob depots 30/80 + Su-17s 0 + rescinded keys removed; panel/printer uniform
  `cur/max`; `DepotSupplyTests` (9) replaces `DepotStockpileTests`; HS_DesignDoc gains §15.1.2a (ratified
  in step). Template depots still arrive empty (preserved, P4/§15 note). ⚑ suite + Khost look, blocking.
- 2026-08-24 — FIXED-WING SUPPLY ruled (Bob) + doc-confirmed (§10.3.1, §15.1.2 verbatim): fixed-wing carry
  NO own supply — `DaysSupply` Max 0, the airbase pays (§11.2.3). Recorded as `Supply Unification.md` §1.5
  + SUP-1 step 6a — docs only, rides the awaited SUP-1 go. ⚠ Found the bite in advance: `CanMove` refuses
  any unit under 1 supply, so the zeroing ships WITH a `Max > 0` guard or no aircraft can ever be ordered
  to move and unit-cycling skips them. §15.5.3.1's "every unit ≤ 0" OOS wording flagged for the §15 pass
  (fixed-wing OOS is airbase-keyed, §15.7.6). Khost's 4 Su-17s re-author to 0.0 with SUP-1; editor relay
  item gains the author-0 line. Airbase-unit CYCLING itself is unbuilt (AIR-1/AIR-2 UI) — §1.5 is the
  record of intent so nobody "fixes" a 0 back to 5.
- 2026-08-24 — ⚑ CLEARED (Bob ran it): depot round-2 GREEN, panel shows stockpiles correctly in play.
  Immediately superseded in MODEL terms by the ruling below — the display fix was correct and is the first
  thing the unification deletes.
- 2026-08-24 — SUPPLY UNIFICATION ruled + surveyed (Bob: ONE supply number per unit — the game just deducts;
  caps regiment 5 / airbase 30 / depot 30/50/80/110; the "stockpile" distinction is not helping. Docs only,
  no code; plan `Planning Docs/Supply Unification.md`, ⏳ awaiting go): survey verified §15 already means
  one-number-per-unit (the doc's "stockpile" names a facility's single store) — the code's dual field was
  the deviation, and the whole depot distribution API has ZERO external callers, so SUP-1 is an internal
  refactor + three local touchpoints. Carries SAVE_VERSION 9 (stockpileInDays leaves the save shape) and
  kills a latent bug free (SnapshotMapper never restored stockpile + force-reset depot supply to Max — a
  half-spent depot reloaded FULL). Editor-relay item REWRITTEN: StockpileInDays RESCINDED same-day, before
  the editor built it. NEW memory: no band-aids — root-cause the model; sweep own diffs for band-aids;
  flag editor impact unprompted (Bob's process ruling, this session).
- 2026-08-24 — DEPOT STOCKPILE round 2 (Bob's catch: the Soviet depot read "5 days supply" in play): the
  suite was green because the MODEL held 80 — but the stockpile had NO display surface; `Prefab_UnitPanel`
  printed every unit's 5-day operating `DaysSupply`, depots included. Panel now branches: depots show
  "Stockpile: N/Max days (Size)" and deliberately NOT the operating supply (two supply numbers was the
  confusion); NEW `CombatUnit.MaxStockpileInDays` (display reads the same table the clamp uses — no second
  spelling; the panel's `HammerAndSickle.Core` namespace cannot reach the GameData class cleanly anyway).
  `DepotStockpileTests` 5→6. Round-1 ⚑ superseded by round 2 (its play half was uncheckable as written —
  recorded as the lesson: a PASS criterion must name a surface that exists).
- 2026-08-24 — ⚑ CLEARED (Bob ran it): fix-batch suite GREEN + Khost play confirmed (units 5/5, airbases
  30/30, no tripwire, settings survive). A-1 and D-3 are `[x]`; the AudioSettings ship-blocker is closed
  for good. Entry deleted from the testing queue per its rules; record in `Planning Docs/Implementing
  Combat.md` §9.
- 2026-08-24 — DEPOT STOCKPILE follow-on (Bob's ask on the green report — "list the large supply totals"):
  `.oob` depots now carry explicit `StockpileInDays` in REAL DAYS — new optional field (−1/absent = the
  ctor's full-for-size default, exactly the old behavior; authored 0 = deliberately empty depot, which is
  why the sentinel is −1) + new `CombatUnit.SetStockpile` (clamps [0, size max] INSIDE the model so
  `GetMaxStockpile` stays the single capacity authority; loader detects a clamp by comparison and warns;
  non-depot authoring warns and is ignored — an airbase's stockpile is its DaysSupply). `khost.oob`
  re-authored: 7 caches list 30.0, the Soviet Large depot 80.0 — behavior-identical. NEW
  `DepotStockpileTests` (5). Editor relay item extended. ⚑ suite + Khost look queued.
- 2026-08-24 — `.oob` `DaysSupply` → REAL DAYS (Bob's ruling; content format): the ratio form made Khost's
  full airbases author "1" and read as grounded under the §11.2.3a 5-day floor. Loader reads days, clamps
  [0, Max] with warning (`StatsMaxCurrent.SetCurrent` does not clamp to Max), per-file tripwire when every
  value ≤ 1 (un-migrated ratio signature); `khost.oob` re-authored in step (units 5.0, airbases 30.0 —
  behavior-identical). `HitPoints` STAYS a ratio — question carried in the relay, not drift. ⚠ URGENT editor
  relay queued in Bob's queue (Hamburg OOB in authoring). ⚑ rides the fix-batch run.
- 2026-08-24 — Combat-pass day-one fixes (Bob's blanket permission; record `Planning Docs/Implementing
  Combat.md` §9): NEW `JsonPolicy.Settings` closes the last local `JsonSerializerOptions` (AudioSettings
  read+write; the ship-blocker); briefing absent-vs-corrupt split per §20.4.2 (`File.Exists` first — absent =
  info no-op, unloadable = real exception); `RiverSymmetryVerifier` repaired (was silently broken vs
  name-form maps since 2026-07-28 — no string-enum converter — now `JsonPolicy.Content`, same read as
  MapLoader); efficiency constants renamed to spell their `EfficiencyLevel` member (D-3 —
  `EFFICIENCY_MOD_FULL` had been the CombatOperations value; single consumer, values untouched). Plan
  RATIFIED by Bob with riders (ground animations first in B; AIR-2 no-AI case test-pinned; §7 decisions stand
  as notes, defaults proceed). ⚑ one suite run + Khost load queued, blocking.
- 2026-08-24 — COMBAT PASS opened (docs only, no code; plan `Planning Docs/Implementing Combat.md`): Bob's
  direction — tighten audio, add combat animations, begin air ops, touch up the global combat constants. Four
  workstreams A/B/C/D scoped against verified ground truth; order argued (A→B→C, D in its own session because
  of the `CombatOracleTests` re-run); AIR-0 (`AirThreatService` + §24.7a.8 overlay) named as the air start
  point; 8 decisions raised for Bob. ▶ NEXT moved here from P4 (which is ⏸ queued, not regressed).
  Findings recorded in that file's §9: **(a)** `.oob` `DaysSupply` is a RATIO not days
  (`OOBFileLoader.cs:415`), so Khost's airbases load 30/30 and are NOT under the §11.2.3a launch floor — the
  literal `1` reads as "grounded" and is not; **(b)** the efficiency constants are named one rung off the enum
  they serve (`EFFICIENCY_MOD_FULL` serves `CombatOperations`, `_PEAK` serves `FullOperations`) — free rename
  proposed as D-3, do it BEFORE any tuning; **(c)** Khost has **no enemy aircraft**, so ASB ground-strike is
  fully playable there and interception is structurally untestable until Hamburg; **(d)** AIR-2 does NOT
  require the reaction-yielding turn loop — in Khost the reaction windows belong to an AI with nothing to fly,
  so v1 never suspends (the interface and the resumable SHAPE still ship, per §11.1.8.6).
- 2026-08-24 — DOCS REORG (Bob's ask): NEW **`Planning Docs/`** holds all 21 plan/pass/courier documents plus
  the new focus file; the repo root keeps exactly `CLAUDE.md`, `Claude_Project.md`, `Claude_TODO.md`,
  `README.md`. `Claude_AI_TODO.md` and `Claude_TODO_Archive.md` moved with the rest despite the `Claude_`
  prefix — they are working documents. Every path reference in `Claude_Project.md` (12) and this file (31) was
  re-pointed in the same commit; references *inside* `Planning Docs/` stay bare filenames and are correct.
  Rule + rationale recorded as NEW `Claude_Project.md` §1.1, and its reconcile stamp updated.
  ✅ **`CLAUDE.md`'s Standard Workflow amended same day** (Bob's "eliminate confusion" permission): items
  1/7 now name the pass's plan file under `Planning Docs/` — a root `todo.md` can no longer be re-created by
  the letter of the rules.
- 2026-08-22 — ⚑ CLEARED (Bob ran it): full EditorTest suite GREEN for the AD range pass — the 4 new
  `AirDefenseTransitTests`, all 9 envelope pins, no movement elsewhere. Pass CLOSED and committed
  (`83d0f99`); the day's three passes (ICM `d6734f3` · prestige `14fa5d5` · AD `83d0f99`) all in the
  pass ledger. Optional play check still worth one sortie: Khost MJ AD envelopes are now 3 hexes.
- 2026-08-22 — AD POINT-DEFENSE RE-BAND (Bob-ratified, same session as the range fix; record
  `Planning Docs/todo_adrange.md` addendum): Chaparral/Roland/Crotale/Rapier + HQ-7 IR 6→4 (`INDIRECT_RANGE_SHORT` —
  they are ~6–12 km point-defense systems that had been authored with the Hawk's area umbrella);
  Tunguska ruled to STAY 5 (⚠ it was never 3 — base 3 + GUN_MISSILE_COMBO trait +2; the "set it to 4"
  instruction was withdrawn when the trait term surfaced); Hawk stays 6. Ratified ladder → NEW DesignDoc
  §11.8.2d text: guns 3 · point-defense 4 · Tunguska/S-125 5 · area SAM 6 · S-300 10. Envelope pins
  added to NATO (×5) / Soviet (×3) / Chinese (×1) profile suites. ⚑ rides the same suite run.
- 2026-08-22 — AD ENGAGEMENT-RANGE FIX (the ICM pass's "BIG FIND", own session; record `Planning Docs/todo_adrange.md`):
  `FindTransitAirDefense` reads the envelope as `max(ActiveIndirectRange, ActivePrimaryRange)` (fallback
  kept) — per §11.4.4 the AD envelope IS the authored IR stat (ZSU 3 … S-300 10; all ~22 AD profiles
  verified, no gaps); the old PR read made every battery interdict at range 1 (authored ladder dead data).
  Defect fix against ratified §11.4.4, not a new ruling; recorded as NEW DesignDoc §11.8.2d.
  `AirDefenseTransitTests`: Reach() mirrors the read + 4 real-range regressions (S-300 10/11, ZSU 3/4,
  ladder differentiation, §11.8.6 one-shot across a wide envelope). Khost: MJ AD envelopes 1→3 hexes
  (intended). No re-pricing; §11.4.4 "spotting +" term deferred to M13 AirThreatService. ⚑ suite run owed.
- 2026-08-21 — MISSION PACK plan drafted (`Planning Docs/todo_missionpack.md`; docs only, no code): single-file
  `.mission` content format — manifest/briefing/map/oob/aii as sections of one JSON doc read via
  JsonPolicy.Content; manifest IN-pack (Bob), clean break (Bob), three text artifacts ruled (Bob);
  agent-decided: unified `MapSection` for pack + save embed (SAVE_VERSION ride, kills MapConfig/
  map-saveVersion/fake-checksum), `.mission` extension, `missionKind` self-describing, `missionId`
  rename sweep, thumbnails out of Resources into the mission folder. Semantics RATIFIED:
  Mission = either kind · Scenario = stand-alone · Campaign Mission = campaign scenario.
  ⌛ Gated on Bob's AII schema; NOT shared with the editor (E15/Khost re-export supersession
  relay waits with it). Thread-board row added.
- 2026-08-20 — ⚑ CLEARED (Bob ran it): C7/V19 suite GREEN — `MissionObjectiveGateTests` (15) incl. the
  float-trap case, the two fractional grade compositions, the non-default fraction round-trip, and the
  migration ladder all pass. The C7 pass is CLOSED game-side; editor's E15 + Khost manifest re-export
  are go (Bob couriers `Planning Docs/C7_Response_to_EditorAgent_2026-08-20.md` + the green signal).
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
  log moved to NEW `Planning Docs/Claude_TODO_Archive.md`; superseded courier files swept to `_to_delete/`; theme-art pass
  committed (`55587d2`); `OnEndScenarioButton` given a tracked home in Bob's queue; ⚑ P3b entry now bundles the
  owed map-standard + D3 suite runs. Pre-rewrite text verbatim at `55587d2`.
