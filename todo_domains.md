# todo_domains.md — THE AIR / GROUND / NAVAL DOMAIN PASS

---

## 🔖 SESSION HANDOFF — updated 2026-08-12, END OF SESSION. START HERE.

**▶ NEXT SESSION: THE MAP-STANDARD PASS (interrupts the D-ladder; Bob's direction 2026-08-12).**
D3's suite run came back GREEN 2026-08-12 (Bob: "all tests green"), so D0–D3 are fully closed
(D3 play-blocked on a water map, as designed). The next work is NOT D4 — it is the map-standard
change request from the Scenario Editor agent:
- **Read first:** `MapStandard_Handoff_to_GameAgent_2026-08-12.md` (their ask) and
  `MapStandard_Response_to_EditorAgent_2026-08-12.md` (our verified reply — the AMENDED plan is its §5).
  ⚠ The editor agent ENDORSED the response and "adjusted where he needed to" — **check for a revised
  editor doc before starting; their handoff may have been re-issued.**
- **The pass, one commit's worth:** G1 at THREE sites (header props + `MapLoader` + `SnapshotMapper`
  read AND the `ToSnapshot` WRITE site — the gap we found) → G3 (delete `MapConfig` geometry machinery
  + eleven test-fixture constructor swaps) → G6 (the failCount throw, BOTH populate loops) → G5
  (derive scroll bounds in `SetupBattleManagerData`) → G7 (docs incl. CLAUDE.md, with the
  interaction-vs-file odd-row qualifier). G2 SKIPPED per the editor's own downgrade. G4 = Bob's scene
  hands. Verify against existing Khost — behaviour must be byte-identical (its header says 32x21).
- Bob is building an **AD-heavy test scenario**; if it gains a coastline it unblocks D3 play-testing
  AND the N0–N3 naval phases in one artifact.

The D-ladder resumes after the map pass: **D4 is deliberately deferred until the AOB/air-mission layer
exists** (recommended 2026-08-11 — staging with no mission consumer is untestable, like D2's
fixed-wing half). The block below is the 2026-08-10/11 history.

---

## 🔖 PRIOR HANDOFF — 2026-08-10 (history; superseded above)

### Where we are
**D0 ✅, D1 ✅ and D2 ✅ are done — 514 tests green, D2 play-confirmed 2026-08-11.** Next on the ratified
ladder is **D3** (helo over-water grace + the plan's only `SAVE_VERSION` bump). Everything below D2
in §H is untouched.

⚠ **THE STANDING SHAPE OF THE PROBLEM, restated because it now governs ordering:** the air RULES are
built and tested; the air GAME is not wired. `AirCombatEngine`, `AOBMissionResolver`, `ReconMissionEngine`,
`AirStandCheck` and `CombatResolver`'s airstrike / base-attack paths are all implemented, all
EditorTest-covered, and all have ZERO live callers — there is no AOB entity, no placement input mode, no
air phase, and no fixed-wing auto-return. D2 could only be play-tested against HELICOPTERS for exactly
this reason. Every remaining air item (D4, and the untested half of D2) sits behind that same gap. The design for the whole pass — air/ground/naval domains, the naming ruling,
naval from Bob's five precepts, and the `FacilityType.Port` base type — is **RATIFIED AND CLOSED**
(§0, §D, §F). Nothing is waiting on a design decision except the ONE question in the next block.

### ✅ RESOLVED 2026-08-10 (same day, later session) — POST-HOC SPOTTING, DesignDoc §12.4.4a/§6.9.10
**Bob's ruling: the Panzer General commitment rule.** A move order is committed BLIND — the mover's own
passive spotting is not applied per hex; it applies ONCE at settlement over the union of every hex
entered plus the resting hex ("the column reports what it passed"). Event-driven reveals (contact halt,
ambush 6.9.3, air-ambush detection, firing) stay immediate. A hidden enemy is therefore still Level0 at
adjacency and the trigger fires as written — no change to `CheckGroundAmbush` geometry needed. Ships
with it: the §6.9.9 eligibility filter (was enforced NOWHERE — an unspotted ART battery would have
sprung design-illegal ambushes the moment the trigger went live). Second fix for free: RECONA/AWACS
transits were self-disarming air ambush via their own per-hex look-down (§11.11.4 said they must face
it); post-hoc restores that too. The independent re-analysis also sharpened the diagnosis: Superior
Camouflage could NEVER save the trigger (leader skill, leaders player-only, ambushers always AI-side) —
option (c) was "never", not "rare". Full rationale: DesignDoc §6.9.10. The diagnosis below is kept as
accurate history.

**✅ PLAY-CONFIRMED 2026-08-11 (Bob):** ambush fires — including through an unspotted unit's ZoC;
devastating hits rout the victim 2 hexes (that is §6.8.1 working, not a bug); a Hold outcome deals
real damage; helo abort-to-origin observed working. Two same-day fixes rode along: **the victim's
HP box showed no damage on a Hold** — the ambush branch raised `RaiseRedrawMapIcons` only on
displacement/removal, now unconditional like the direct-combat path (the coarse redraw is the ONLY
HP-box refresh, §3.6e) — and narration (event/dispatch/sound) is now gated on `AmbushOutcome.Executed`
so an internal failure can never be narrated as a fight. ⚠ **`MovementController.AMBUSH_DEBUG` is ON
at Bob's request** — `[AMBUSH DEBUG]` console lines name every sprung/skipped ambush; flip false and
delete the scan helper once §6.9 tuning settles. 🔵 Parked question: `CombatRandom`'s seedless ctor
is plain `new Random()` — a narrow same-tick correlation risk worth a look if outcomes ever feel samey.

### 🔴 THE ORIGINAL DIAGNOSIS — §6.9 GROUND AMBUSH WAS STRUCTURALLY UNREACHABLE
**The wiring is correct and proven** (`AmbushAction_ActuallyAppliesDamage_EndToEnd` is green — the
orchestrator applies real HP). **The TRIGGER can essentially never fire**, and this is arithmetic, not
a bug:

- `CheckGroundAmbush` requires the ambusher to be **`SpottedLevel.Level0` AND adjacent**.
- Ordinary ground units have **ground spotting range 2** (`GameData.GroundSpottingRange`, `_ => 2`).
- Per-hex the loop runs **ambush check (line ~949) then spotting sweep (line ~1012)** — that ordering
  was fixed this session and is correct.
- But walking toward a hidden enemy E: at distance 3 nothing; **at distance 2 the sweep spots E to
  Level1**; at distance 1 the ambush check finds E adjacent but no longer Level0 → **skipped**.
- ⚠ **So the mover always spots the ambusher one hex BEFORE reaching adjacency.** Ambush can only fire
  when something keeps the ambusher hidden inside 2 hexes — today only §14.9.4 Superior Camouflage
  (enemy spotting −1) does that, which makes a core mechanic depend on one T4 leader skill.

**Bob's in-game results that produced this diagnosis (2026-08-10):**
1. Ground unit moved through an unspotted unit's ZoC → **nothing happened except spotting it.**
2. No HP loss, no equipment losses, **no console errors** (so nothing threw — `AmbushAction` was never
   called).
3. A dispatch about **movement halting** appeared, not an ambush — that is
   `ReportMoveBlockedByContact` firing correctly for the contact halt. **Working as intended.**
4. **Helo displacement to the nearest legal hex on landing → CORRECT.** ✅

**Decision needed from Bob — four candidate resolutions:**
- **(a) Snapshot at move start.** Any enemy that was Level0 when the move ORDER began may ambush.
  Matches "you set off not knowing." ⚠ But a unit that spots them at range 2 and walks adjacent anyway
  still gets ambushed, which punishes an informed choice.
- **(b) Extend the trigger to the spotting boundary.** Ambush fires at range 2 rather than adjacency —
  the moment you enter its reach while it is still unspotted.
- **(c) Accept it: ambush requires concealment.** Rare by design, gated on Superior Camouflage and any
  future terrain-concealment rule. Cheapest — zero code — but leaves §6.9 nearly dead.
- **(d) Give ambush-capable units a concealment property** so they stay hidden inside spotting range.
  Most work, most control.
**Agent leans (a)**, with the caveat noted. This is a §6.9 design call, not a code call.

### Everything built this session (all green)
`Domain` enum · `OccupiesDomain` · `IsFixedWing` (was `IsAirUnit`+`IsFixedWingAirUnit`) · `IsSeenAsAir`
· **D9 closed** — four disagreeing fixed-wing lists collapsed to `GameData.IsAirborneClassification`,
killing two live bugs (transport aircraft filed in the ground layer; **AWACS could not attach to an
airbase**) · §12.3.7a fixed-wing does not spot ground (RECONA/AWACS exempt) · impassable closed to all
domains · **`AmbushAction.cs`** (new orchestrator — damage, §6.9.3 reveal, displacement, §7.15
degradation) · helo denied the §6.9.4 surprise multiplier · anti-dogpile (one ambush per ambusher per
move) · **the overrun fix** (pre-step contact halt + ambush-before-spotting ordering) ·
`FindNearestLegalRestingHex` + landing displacement · helo transit stand check on ambush damage with
Shock accumulating · `MovementHalt.FlightEvasion` and `ReportFlightHalted` deleted.

### Next milestones, in order
1. ~~Resolve the §6.9 trigger question~~ ✅ RESOLVED, BUILT AND **PLAY-CONFIRMED 2026-08-11** (see
   the block above). Ambush is live end-to-end. Residue: flip `AMBUSH_DEBUG` off when tuning settles.
2. ~~**D2**~~ ✅ **CLOSED 2026-08-11 — 514 tests green, play-confirmed.** Overhead GAD fire, the
   towed-posture gate, the shot meter, anti-dogpile, the coin flip deleted, and eligibility ruled to be
   the **classification** gate (SAM/SPSAM/AAA/SPAAA). ⚠ Its FIXED-WING half is play-unverified and stays
   that way until air missions exist. See § PHASE D2.
3. ~~**D3**~~ ✅ **CODE-COMPLETE 2026-08-11** — over-water grace, `CanRestAt` fixed so a helo can stop on
   water at all, both UI halves, `SAVE_VERSION` 6. ⚑ Suite run owed; not playable until a map has water.
4. **D4** fixed-wing staging at airbases (unblocked by D0's list fix).
5. **N0 → N2** naval foundations · naval combat + sea clock · the `FacilityType.Port` base type.
   **N3 supply hooks stay gated on §15.**
6. Then the original thread: **P4 requisition** (now also carrying port repurchase) and **P5 docs**,
   which should merge with §A/§B of this file.

### Standing constraints not to rediscover
- ⚠ Khost has **no water, no ports, no beachheads** — N0–N3 are suite-verifiable but NOT playable until
  Bob's coastal test map exists.
- ⚠ **Weather is single-state Clear**, so storm grounding and the doubled sea-supply cost cannot fire.
- ⚠ **§15 supply is a stub** in `BattleManager.ProcessUpkeep`.
- ⚠ `CombatOracleTests` must be re-run after any combat-constant change (§J.1) — the oracle is the AI's
  decision basis and drifts silently.
- ⚠ Editor relay (§I) is written and unsent; it goes when N2 lands.

---

> **What this file is.** The punch list to make the design doc, `Claude_Project.md` and the CODE agree
> on how air, ground, helicopter and naval units work. Opened 2026-08-10 after the air audit
> (`Claude_TODO.md` §"AIR AUDIT") and the helo/fixed-wing rulings. Naval items land in §F.
>
> **Legend:** `[ ]` todo · `[~]` in progress · `[x]` done · `[?]` needs a Bob ruling first

---

## 0. THE RATIFIED MODEL (Bob, 2026-08-10)

**Air and ground units are the SAME KIND OF THING** — units on the map, launched/moved step by step by
their controller, in the Panzer General / Panzer Corps 2 tradition. Fixed-wing aircraft take off from an
airbase, are directed hex by hex, and can be engaged. Auto-return exists to **prevent tedium**, not
because an aircraft is somehow not a real unit.

⚠ **The earlier agent framing "a fixed-wing aircraft is not a unit on the map, it's a mission being flown
across it" is WRONG and is retired.** It would have produced fixed-wing as a special case when it is the
normal case.

**What actually differs is WHICH LAYER A UNIT INTERACTS WITH:**

| | Ground layer | Air layer |
|---|---|---|
| Ground units | ✅ live here | — |
| **Fixed-wing** | — | ✅ lives here |
| **Helicopters** | ✅ **live here** (the exception) | only in that AD can shoot them |

**HELICOPTERS ARE THE ONE HYBRID.** They *move* like aircraft and *interact* like ground units:
ground stats, ground stacking (**may not share a hex**), ground ambush in both directions, ZoC
projection, tile-control flips, spotted on the ground range (nap-of-the-earth). Their two air-layer
concessions: **dedicated air-defence units can fire on them**, and [?] ambush damage — see the
contradiction below.

---

## ✅ THE AMBUSHED-HELICOPTER RULING (Bob, 2026-08-10 — contradiction resolved)

**A helicopter caught by a ground ambush is HALTED and takes an ORDINARY attack, but its speed and
altitude DENY THE AMBUSHER THE §6.9 SURPRISE MULTIPLIER.**

This reconciles the two readings in Bob's message: it is a genuine ground-layer interaction (so the
2026-08-04 "no combat at all" rule is RETIRED), while the helo still "avoids the HP damage **of an
ambush**" — the ambush premium specifically, not the attack itself.

Consequences: the helo branch raises `RaiseAmbushTriggered` like the ground branch; only the "ZoC never
halts a flight" half of the flight rule survives; and `CombatResolver` suppresses the §6.9 ambush
multiplier when the VICTIM is travelling by helo. Fixed-wing remains wholly immune — nothing on the
ground can touch it.

---

## A. DESIGN DOC AMENDMENTS (`HS_DesignDoc.md`)

- [ ] **A1 §5.13 — restructure around the LAYER model.** Add a §5.13.0 stating the ground-layer /
      air-layer split and naming helicopters as the single hybrid. Everything in §5.13.2 / §5.13.3 then
      reads as a consequence rather than a list of exceptions.
- [ ] **A2 §5.13.1 — REMOVE "and impassable".** Currently "Flat 1 MP per hex for all air units; ignores
      terrain costs **and impassable**". **RULED 2026-08-10: no air unit of any kind may enter an
      Impassable hex.** Rationale to record: impassable represents foreign, non-belligerent territory —
      overflying it is an act of war, not a shortcut — and it is what gives the map its shape
      (chokepoints, protected flanks, anchored air-defence placement).
- [ ] **A3 §5.13.2 — NEW over-water rule for helicopters.** A helo MAY end its move over Water, but
      **must reach land by the end of its next move or it is lost.** One turn of grace, no supply
      bookkeeping. (Supersedes the proposed −2 days supply version, which bled too slowly: from a
      5-day maximum it allowed ~3 turns of loitering.) UI is part of the rule: a warning in the **turn
      summary**, and an **info box when the unit is selected**.
- [ ] **A4 §5.13.2.2 — settle the ambush-damage wording** per the ruling above.
- [ ] **A5 §5.13.2.4 / §5.13.3.2 — state the detection-roll split plainly.** The 1d6-vs-experience air
      ambush roll is **fixed-wing only**. Helicopters get **no roll** — they take the hit if the
      air-defence unit has shots available, then roll the §11.8.9 Helo Transit Stand Check.
- [ ] **A6 §12.3.7 — amend "FGT / ATT / BMB / WW / TRN: 2 / 4" to "0 / 4"**, and add **§12.3.7a**: a unit
      travelling fixed-wing does not spot ground units at all. Record the two exemptions and why —
      **RECONA and AWACS keep their 8-hex ground reach** (§11.11.3 builds the recon mission's search area
      from it; §12.3.9 calls exploiting the AWACS look-down a deliberate player risk).
- [ ] **A7 §9.10.6 — mechanism paragraph is STALE.** It still describes `EmbarkmentState EmbarkedNaval`
      and explains that ground templates "show Embarked=NONE yet IsEmbarkable=true". Both symbols were
      DELETED in P1. The RULE is current; only the mechanism wording needs replacing with the derived-bay
      model.
- [ ] **A8 §9.7.1 / §11.12 — the FIXED-WING STAGING RULE (new).** Once a ground unit boards a fixed-wing
      transport it is **attached to the airbase as a loaded transport** and is committed to an air
      mission from there. Include the capacity rule from C.5. Record the ratified scope decision:
      **ONE operative slot, no mass drops** — corner-case cost is too high, and the air corridor clears
      as successive missions run, so the extra tedium is acceptable.
- [ ] **A9 §10.7.3 / §7A.14 — sweep for "air unit" used loosely** and re-word per the §D vocabulary.

---

## B. `Claude_Project.md` UPDATES

- [ ] **B1 §3.5b** — replace the current ambush paragraph with the layer model + the settled ambush
      ruling. The helo/fixed-wing table belongs here, not buried in a todo.
- [ ] **B2 §3.7 SpottingService** — already carries §12.3.7a; add the layer framing and the
      "seen as" vocabulary once §D lands.
- [ ] **B3 §3.7 MovementModeService** — add the impassable and over-water rules next to
      `IsSealiftedNow`, since all three are "where may this unit go" questions.
- [ ] **B4 §5 Enumerations** — document the new `Domain` enum (§D) alongside `MovementMedium`.
- [ ] **B5 §11 Extended Enumerations** — `IsAirUnit` / `IsFixedWingAirUnit` are renamed; update every
      mention. ⚠ Also fix the §2.8/§3.2 prose that says "air unit" where it means fixed-wing.
- [ ] **B6 Reconciliation stamp** — bump the date line when B1–B5 land.

---

## C. CODE CHANGES

- [?] **C1 Helo ambush damage** — blocked on the ruling above. If (2) or (3): the helo branch in
      `MovementController.ExecuteMovement` raises `RaiseAmbushTriggered` and keeps only the
      "ZoC never halts a flight" half of the flight rule; `PrinterDispatch.ReportFlightHalted` text
      changes from evasion to being caught. If (3), the ambush bonus is additionally suppressed for a
      helo victim in `CombatResolver`.
- [ ] **C2 Impassable is closed to air.** `HexMapUtil.ComputeStepCost` returns 1 for airborne movers
      **before** it checks `TerrainType.Impassable`, so today they fly straight over. Move the impassable
      rejection above the airborne early-out. Applies to BOTH helo and fixed-wing. Test both.
- [ ] **C3 Over-water grace for helicopters.** New persisted flag on `CombatUnit` (e.g.
      `EndedTurnOverWater`), set at end of move when the resting hex is Water, cleared on reaching land.
      Checked at Refresh: still over water ⇒ destroyed. ⚠ **`SAVE_VERSION` bump** (5 → 6) with the
      pre-1.0 clean-break note. Plus the turn-summary warning and the selection info box (A3).
      ⚠ Interaction to pin in a test: a helo halted over water by ambush or AD fire has 0 MP and dies
      next turn — the §11.8.9 abort outcome (free return to origin) is the escape hatch, and a loaded
      lift takes its embarked regiment down with it.
- [ ] **C4 Helicopters must not get the fixed-wing detection roll.** `MovementController` calls
      `SpottingService.CheckAirAmbush` for every airborne mover; §5.13.2.4 gives helos **no roll**.
      Split the path: fixed-wing → detection roll; helo → straight to air-defence fire, then the
      §11.8.9 stand check. ⚠ **`HeloTransitStandCheck` and `CombatResolver.ResolveAirDefenseFire` are
      both fully built and tested with ZERO callers** — this is wiring, not new rules, and it deletes
      the `UnityEngine.Random.Range(0,2)` coin flip currently standing in for it.
- [ ] **C5 Fixed-wing boarding + airbase capacity.** See the ruling in §E1 below.
- [ ] **C6 The §D renames** — mechanical and wide; none of the affected properties are persisted
      (all `[JsonIgnore]` computed), so there is no save impact.

---

## D. NAMING — THE DEFINITIVE RULING (proposed 2026-08-10)

**The problem:** one word, "air", answers three different questions, and `IsAirUnit` returns **false for
a helicopter** — the single most misleading thing in the codebase. Three questions, three names, and the
enum uses the vocabulary the design doc ALREADY uses for exactly this split ("dual-domain", §12.3).

| Question | Name | Type | Governs |
|---|---|---|---|
| **How does it move right now?** | `MovementMedium` via `MovementModeService.CurrentMedium` | existing enum | terrain cost, ZoC receipt, ambush exposure, impassable/water rules, pacing, audio |
| **Which layer does it occupy?** | **`OccupiesDomain`** | new `Domain` enum | hex sharing / stacking, icon layer, tile-control flips, who may engage it |
| **How is it seen?** | **`SeenAsDomain`** | same `Domain` enum | which spotting range an observer uses against it |

```
public enum Domain { Ground, Air }
```

- **Fixed-wing:** `OccupiesDomain = Air`, `SeenAsDomain = Air`.
- **Helicopter (gunship):** `OccupiesDomain = Ground`, `SeenAsDomain = Ground` (nap-of-the-earth).
- **Helo-borne lift in flight:** `OccupiesDomain = Ground`, `SeenAsDomain = **Air**` — a lift cannot hide
  the way a gunship can. This is the case that proves the two questions must be named separately.
- **Everything else:** `Ground` / `Ground`.

**Renames:**
- `IsAirUnit` + `IsFixedWingAirUnit` (identical expressions today) → **one** property `IsFixedWing`.
- `IsAirborneSpottingTarget` → `SeenAsDomain == Domain.Air`.
- `MovementModeService.IsAirborneNow` — **keep**, means "is flying right now".
- `IsHelicopter` — **keep**, means "is a gunship by classification", never "is flying".

⚠ **THE DISCIPLINE THAT PREVENTS RECURRENCE: rule code asks the QUESTION-NAMED property; only the
derivation asks the classification.** `OccupiesDomain` is derived from `IsFixedWing` today, but a rule
site must never test `IsFixedWing` directly — that is exactly how "air" got overloaded the first time.
The two are the same VALUE today and different QUESTIONS forever.

⚠ **VOCABULARY BAN: never write bare "air unit"** in code, comments or docs. Say *fixed-wing*,
*airborne now*, *air-domain*, or *aircraft* (meaning fixed-wing specifically).

---

## E. DESIGN DECISIONS TAKEN THIS PASS

- [x] **E1 FIXED-WING BOARDING AND THE 4-AIRCRAFT CAP — proposed rule.** Airborne units board while
      **adjacent** to a friendly airbase (§9.7.1), so the loaded transport must then be free-moved onto
      the airbase hex. `GameData.MAX_AIR_UNITS = 4`.
      **RULE: a loaded transport consumes one airbase slot, and boarding is REFUSED UP FRONT when the
      airbase is at capacity.** No overflow, no temporary exception. ✅ RATIFIED 2026-08-10.
      ⚠ **`MAX_AIR_UNITS` is just a constant and Bob expects to RAISE it (4 → 6 likely)** once staging
      is in play. Nothing may hard-code 4. The only place the number is visually assumed is the airbase
      stack sprite, which already caps its art at "4 or more" — check it still reads correctly at 6.
      **Why this is the version that will not bite us:** the invariant `occupants ≤ 4` is never violated,
      even transiently — the check happens BEFORE the board, not after, so there is never a moment where
      the game must decide what to do with a fifth aircraft. It also turns the cap into the interesting
      constraint rather than an obstacle: staging a paradrop **costs you air cover from that base**,
      which is a real trade-off. Breaking the cap for one special case removes the decision AND creates
      the corner cases.
      ⚠ **Implementation snag:** `CombatUnit.AddAirUnit` **throws** for a non-air classification, and a
      paratroop regiment is `AB` — so the loaded-transport case needs its own attach path (or the guard
      must accept "ground unit whose ACTIVE profile is a fixed-wing transport").
      ⚠ Open sub-questions: on the §11.7.2.4 forced evacuation (enemy ground unit adjacent), does the
      loaded transport evacuate to Reserve **with its regiment aboard**? Agent's proposal: yes — they are
      one unit while loaded, and it may redeploy later. And the slot frees the moment the para drops, the
      transport is destroyed, or the load is cancelled.
- [x] **E2 One operative slot — NO mass drops.** Ratified: too many corner cases. Sequential missions;
      the air corridor clears as they run.
- [x] **E3 Impassable closed to all air.** Ratified.
- [x] **E4 Over-water: one turn of grace.** Ratified, with turn-summary warning + selection info box.

---

## F. NAVAL — DESIGNED FROM FIRST PRINCIPLES (Bob's five precepts, 2026-08-10)

### F.0 ⚠ THIS SUPERSEDES INSTANT SEALIFT. §5.4.2 IS REWRITTEN, NOT EXTENDED.

The old model (§5.4.2.3) was **instant port-to-port teleport with the sea passage abstracted away**, and
§5.4.2.6 deferred "any over-sea movement beyond instant port-to-port". The agent's 2026-08-10 analysis
(`todo_profiles.md` §14c) correctly concluded there was no traversal to build — *against that doc*.

**Bob's precepts 3 and 5 make instant movement impossible.** You cannot bomb a convoy that teleports,
and you cannot spend a day of supply per turn at sea if no turn is ever spent there. Naval is now a
REAL MAP PRESENCE with hex-by-hex movement over several turns. Every "instant" clause in §5.4.2,
§9.10.6.3 and §21.8 is retired, and the §24.7a.3 Naval Movement Marker is retired with them — **there is
no destination picker and no new input mode**, because a naval unit is now moved like any other unit.
⚠ That also removes the M13 dependency that had naval blocked.

### F.1 THE PRECEPTS (Bob, verbatim intent)

1. **No ship-to-ship combat in this version.**
2. **A ground unit embarked IS a naval unit.**
3. **Fixed-wing and ATTACK HELOS may attack naval units** — helos attacking as ground units, fixed-wing
   flying air missions.
4. **Only Marines (mechanized or otherwise) may disembark outside a friendly port.**
5. **1 day of supply per turn at sea**, plus any combat supply. Running out at sea degrades efficiency
   and eventually loses the unit.

### F.2 NAVAL IS THE THIRD DOMAIN

`Domain` gains **`Naval`**. A naval unit occupies WATER hexes plus the single land hex that is a
friendly PORT. It is the transported regiment itself — same `UnitID`, same HP pool, drawing the shared
`TRN_NAVAL` profile while `IsNavalEmbarked` (already built, P1/P2).

| Question | Naval unit |
|---|---|
| Occupies | `Domain.Naval` — water hexes, and a port hex |
| Seen as | **surface** (ordinary ground spotting ranges) — a ship is not an air target |
| Stacking | one naval unit per water hex. ⚠ **ONLY FIXED-WING MAY EVER TEMPORARILY SHARE A HEX** (Bob 2026-08-10) — helicopters may not hover over a naval unit, and nothing else may share with anything. This is now the single stacking exception in the whole game |
| Zone of control | **projects none, receives none.** No ship-to-ship combat means no sea control |
| Tile control | **never flips a hex.** Sea control is not modelled |
| Objectives | **cannot capture.** §17.5.2 needs a GROUND unit to end movement on the objective — you must land first. A port cannot be taken from the sea |
| Ambush | immune. Ambush is a ground-layer mechanic and no ground unit can stand on water |
| ZoC halt | none |

### F.3 MOVEMENT

- **Water hexes only, flat 1 MP per hex** (same shape as the air rule — the sea has no terrain).
- **A friendly PORT hex is the one land hex a naval unit may enter.** It is the interface: embark there,
  sail out, sail back, debark there.
- **Movement points come from `TRN_NAVAL`** (currently the Truck-archetype 8). ⚠ Knob. Sized so a
  typical crossing is 2–3 turns — long enough for the supply clock and enemy air to matter, short
  enough not to be tedious.
- **Impassable is closed** (§A2 applies to every domain).
- No naval unit may enter a non-port land hex; no ground unit may enter water (already enforced).

### F.4 COMBAT — precepts 1 and 3

- **A naval unit NEVER initiates combat and NEVER returns fire.** It is a loaded transport, not a
  warship. This is precept 1 stated mechanically, and it is what keeps the whole system small.
- ⚠ **THE SHIP'S OWN STATS DEFEND, THE REGIMENT'S HP IS THE POOL — ✅ SETTLED 2026-08-10.**
  **"The naval HP IS the unit HP, nothing hidden."** ONE pool, no second HP track, no deferred transfer,
  no hidden-damage reveal. The NAVAL profile supplies the defensive STATS for the calculation — you are
  shooting at a ship, not at infantry — and the HP that comes off is the regiment's, immediately. A
  regiment mauled at sea comes ashore mauled, and its strength is visible the whole way.
- ⚠ **WHICH DEFENSIVE STAT APPLIES DEPENDS ON THE ATTACKER, and it is NOT all GAD:**
  - **Fixed-wing air strike** → air-to-ground always resolves **GA vs GAD** (§7.7.5). GAD is the one
    Bob asked about and it must be strong.
  - **Attack helo** → attacks "as a ground unit" (precept 3), so the ordinary §7.7 pipeline: the helo's
    HA/SA against the ship's **HD/SD**.
  - **Coastal artillery** → §7.13 indirect, likewise against **HD/SD**.
  So "decent defences all round" is literally right: **GAD strong** (the airstrike Δ) **AND HD/SD
  decent** (the helo and artillery Δ). A ship with a great GAD and thin HD would be untouchable from the
  air and free meat for a gunship.
- **Who may attack it:** fixed-wing via an air mission (an ASB whose target hex holds the naval unit),
  ATTACK HELOS using the ordinary §7.7 direct-combat pipeline as ground units, **and INDIRECT FIRE
  (ART / SPA / ROC) from shore** — ratified 2026-08-10, the agent's push-back accepted. Coastal
  artillery makes shore defence meaningful and adds no ship-to-ship rules; the ship still never
  answers.
- ⚠ **BALLISTIC MISSILES MAY NOT TARGET NAVAL UNITS, AND THIS MUST BE A REVERSIBLE RULE.**
  Anti-ship ballistic missiles are technologically out of reach in the 1980s setting — but Bob expects
  later games in this engine to have them. So it is a NAMED, FLIPPABLE rule
  (e.g. `BALLISTIC_MISSILES_MAY_TARGET_NAVAL = false`), never an inline `if (BM) return false;` buried
  in a targeting check. Amends §11.7.5.2's dual-targeting list (ground unit OR base — not naval).
- ⚠ **ALL COMBAT AGAINST A NAVAL UNIT IS DAMAGE-ONLY.** No stand check, no surrender check, no retreat,
  no displacement, no advance-after-combat. This mirrors §11.6.1.6 (air-to-ground is damage-only) and it
  is a large simplification: **`RetreatResolver` never has to answer "where does a ship retreat to?"**
  A helo attack and an air strike therefore resolve identically against a ship.
- **0 HP = sunk = the regiment is destroyed**, its full remaining equipment booked to the loss ledger.
- **Defence stats come from `TRN_NAVAL`** (HD / SD / GAD). ⚠ Its GAD in particular needs a deliberate
  value — it is the entire air-attack Δ.
- **NO §7.10.1 embarkment damage malus** (proposed). That malus exists because "lift aviation is glass";
  a ship is a large, comparatively survivable platform. Applying +1 band ×2.0 would make one airstrike
  annihilate a loaded regiment and nobody would ever sail. ⚠ Knob, and the single biggest balance lever.
- **In a PORT hex the unit is treated as an ordinary ground unit for combat** — alongside the dock,
  troops able to fight. Only ON WATER is it air-attackable-only. Clean boundary, and it stops a port
  from being a sanctuary.

### F.5 EMBARK / DEBARK — precept 4

⚠ **NON-MARINE UNITS ARE FRIENDLY-PORT-TO-FRIENDLY-PORT, FULL STOP** (Bob 2026-08-10). No exceptions,
no coastal landings. Beachhead hexes exist for Marines and only Marines.

⚠ **EMBARK AND DEBARK BOTH HAPPEN ACROSS THE PORT UNIT, NOT ON IT — see §F.11.** A unit embarks while
ADJACENT to a friendly port and is placed on a free WATER hex adjacent to that port; it debarks from a
water hex adjacent to the port onto a free GROUND hex adjacent to the port. **If no free hex exists at
either end, the operation is prohibited** — ports congest, and a besieged port cannot be used as an
escape hatch.

**THE ACTION ECONOMY (Bob 2026-08-10) — embarking costs a turn, landing is free but ends one:**
- **Embarking takes a FULL TURN.** The final deploy-up to Embarked consumes whatever move actions and MP
  remain; the unit then becomes a naval unit with **0 MP and 0 move actions**. You board this turn and
  sail next turn.
- **Debarking is FREE** — no action cost, and it may happen in the same turn as naval movement. But on
  landing the unit becomes a ground unit again with **0 MP and 0 move actions**. So you can sail in and
  land, and that is your turn; you cannot land and then advance.
- ⚠ Consequence worth holding: a landing force is **stationary and exposed on the turn it lands**. That
  is the amphibious risk, and it is created by the action economy rather than by a special rule.

- **Embark:** any ground unit, on a friendly port hex. (Built, P2 — universal port rule, organic lift
  wins over naval.)
- **Debark, everyone:** a friendly port hex.
- **Debark, MARINES ONLY (MAR / MMAR):** additionally onto a coastal `IsBeachhead` hex. (Built, P2 —
  identity doctrine, the privilege attaches to marines, not to equipment.)
- **Arrives DEPLOYED** in both cases (§5.4.2.5 survives the rewrite).
- ⚠ **A landing is BLOCKED by enemy OCCUPANCY, not by enemy CONTROL.** You may storm an enemy-held
  coastline — that is the point of an amphibious assault — but you may not land on top of a unit.
  Mirrors the paradrop rule (§5.4.1.6).

### F.6 SUPPLY AND THE SEA CLOCK — precept 5

- **1 day of supply at Upkeep for EVERY naval unit, every turn, from the moment it embarks.**
- ⚠ **THERE ARE NO HARBOURS — RULED 2026-08-10.** *"There won't be harbors per-se; once you embark, you
  are on your own until you arrive somewhere."* The agent's proposed alongside-the-dock exemption is
  REJECTED. Being adjacent to a friendly port is worth nothing: water is water. **The clock starts the
  turn you board**, so boarding itself costs a day, and a voyage is 5 turns from full — no parking, no
  topping up, no waiting offshore for an escort. This is simpler than the exemption AND it is the rule
  that makes sailing a commitment rather than a manoeuvre.
- ⚠ **STORM DOUBLES IT — 2 days per turn at sea (Bob 2026-08-10).** Movement supply only; combat supply
  is unaffected. Storm does not stop a ship, it just punishes being caught out in one. This is the first
  weather effect that reaches something other than aircraft (§5.13.4 grounds all air), so weather now
  matters to the sea as well as the sky.
- **No resupply at sea, ever.** Supply traces through friendly-owned land hexes to a depot; there is no
  trace over water. The clock is strictly one-way. Reaching a friendly port restores normal supply.
### F.6a ⚓ THE BEACHHEAD IS A SUPPLY SOURCE (Bob, 2026-08-10) — the rule that makes landings viable

**THE BEACHHEAD HEX ITSELF IS TREATED AS RANGE 1 FROM THE NEAREST DEPOT, and supply then flows THROUGH
it by the normal rules — "as if a depot was on a narrow peninsula"** (Bob, final wording 2026-08-10).

⚠ Note what this is and is not. It is **not** a special "marines are supplied" exception — it is a
property of the HEX. The beachhead becomes a supply ENTRY POINT, and everything inland traces through it
under the ordinary §15.3 rules, attenuating normally as it goes. Friendly ownership still applies, so a
beachhead the enemy controls feeds nobody. That makes it strictly simpler than a bespoke rule AND it
gives the right shape: the lodgement is only as good as the ground you hold behind the beach.

⚠ **WITHOUT THIS, AMPHIBIOUS ASSAULT IS A SUICIDE MECHANIC AND NOTHING ELSE.** A Marine regiment lands
on a hostile shore with no friendly territory behind it, so it can trace to no depot; §15 then starts
degrading it the moment it arrives, and the whole feature is a way to lose regiments slowly. This rule
is what turns a landing into a LODGEMENT. It also models the real thing exactly: an amphibious force is
supplied **over the beach from the sea**, which is precisely why the overland distance to a port is
irrelevant and gets treated as zero.

Consequences: a held beachhead is effectively a forward supply node, so the amphibious sequence is
*land → hold the beach → build out*, which is the correct shape. Losing the beachhead cuts the lodgement
off immediately.

⚠ Open (§F.9d): must MARINES specifically keep holding it, or any friendly unit once ashore? And must a
friendly port exist somewhere on the map for "the nearest port" to resolve?

- **At 0 supply, one EFFICIENCY rung per turn** (Full → Combat → Normal → Degraded → Static).
- **At the bottom rung, the unit is LOST AT SEA** — removed, full remaining equipment booked as losses.
  ⚠ **Use the SURRENDER machinery, rename the narrative.** Bob asked surrender vs shatter:
  *shatter is wrong* — it withdraws to Reserve, meaning the unit comes back, which deletes the risk
  precept 5 exists to create. *Surrender* has the right MECHANICS (removal + remaining equipment booked)
  but the wrong story — there is nobody to surrender to mid-ocean. So reuse `RetreatResolver`'s
  surrender path and present it to the player as **"lost at sea."**
- **Total clock from full supply: 5 turns of fuel + 4 turns of degradation = 9 turns.** ⚠ **The real
  deterrent is not the death, it is ARRIVING BROKEN** — a regiment that dawdles lands at Static
  efficiency (×0.5 combat strength). That self-regulates far better than the loss does, and it means the
  9-turn tail is acceptable rather than too forgiving.

### F.7 NAMING — naval stress-tests §D, and one adjustment is needed

`Domain { Ground, Air }` becomes **`Domain { Ground, Air, Naval }`** for `OccupiesDomain`. But
`SeenAsDomain` then reads badly — a ship is not "Ground". **Adjustment: the spotting axis goes back to a
boolean, `IsSeenAsAir`**, because there are exactly TWO spotting ranges (air range, ground range) and
tanks, helicopters and ships all use the same one. Leaner than a second enum, and no ship has to be
labelled "Ground" to be seen.

So the final vocabulary is: `MovementMedium` (how it moves) · `OccupiesDomain` (where it is) ·
`IsSeenAsAir` (how it is ranged against).

### F.8 WHAT THIS CHANGES IN ALREADY-BUILT CODE

- ✅ **P2 keeps everything** — embark, debark, the marine beachhead privilege, `IsNavalEmbarked`,
  `EmbarkmentChecks`, the shared `TRN_NAVAL` draw. All of it matches these precepts unchanged.
- ❌ **P3's `IsSealiftedNow` prohibition is REVERSED.** It returned an empty range and empty path
  because the doc said the sea passage was abstracted. Naval now needs real per-hex movement over water.
  ⚠ **The guard was still right when it was written** — without it a sealifted unit fell through to the
  GROUND rules and could have walked inland aboard its ships. The replacement must keep that half:
  water yes, non-port land no.

### F.11 THE PORT BASE TYPE (Bob's design, 2026-08-10) — and why it solves the supply problem

**A PORT is a new base type**, a unit sitting on the port-city hex exactly as an airbase sits on an
airbase site. It is **also a supply depot** (a sub-type of one). Units embark and debark *across* it,
never *onto* it.

⚠ **THIS IS WHAT ACTUALLY FIXES THE CROSS-OCEAN SUPPLY TRACE (§F.9b.4).** The problem was that
excluding water from the supply trace would strand any force that arrived by sea. With a port that is
itself a depot, supply **originates at the coast** and never needs to cross water at all — which is also
how it works in reality: the harbour IS where the supply comes from. So the two rules can now both hold
cleanly: **the trace never crosses water, and a landed force is supplied by the port it landed at.**

**It slots into an existing, ratified pattern — the airbase.** §11.7.2.7 already establishes
**map flag = the SITE, base unit = the INSTALLATION**: an airbase hex keeps `isAirbase` after the
installation is destroyed, and re-establishment is by repurchase on the persisting site. Port should be
identical: `HexTile.IsPort` stays as the site flag, and a `FacilityType.Port` unit is the installation.
That inherits, for free: HP 60 and destructibility (§11.7.2.1), OperationalCapacity degradation and the
strategic OC premium (§11.7.2.2a), no capture (§11.7.2.8), ZoC repair-lock, site persistence, and
supply salvage on destruction (§17.6.4).

**Consequences worth having:**
- **Destroying a port strands every ship bound for it.** They are left at sea burning the §F.6 clock and
  must find another friendly port — or, if they are Marines, a beach. That is excellent pressure and it
  arrives for free from the base rules.
- **A port cannot be captured, only destroyed** (§11.7.2.8), then repurchased on the surviving site. So
  you can never seize a working enemy harbour intact — you wreck it and rebuild. ⚠ Confirm that is
  intended for ports; it is the ratified rule for every other base type.
- **Congestion is real.** No free adjacent water hex means no embarking; no free adjacent ground hex
  means no landing. A crowded or besieged port stops working.

**Open mechanics for §F.9b:** which free hex is chosen when several qualify, whether a harbour-adjacent
ship is in supply, and whether a port can be repurchased like an airbase.

### F.9a ✅ ANSWERED (Bob, 2026-08-10)

1. **Artillery may shell naval units — YES.** Ballistic missiles may NOT, as a reversible rule (§F.4).
2. **Storm: double the movement supply at sea (2/turn), not combat supply.** Movement continues.
3. **Helicopters may NOT hover over a naval unit. ONLY FIXED-WING may temporarily share a hex** — the
   single stacking exception in the game.
4. **Embark costs a full turn; debark is free but lands with 0 MP / 0 move actions** (§F.5).
5. **No penalty for having been broken at sea — normal §3.5.8 recovery.**
6. **The AI does NOT sail.** Naval is player-only; AI amphibious forces arrive as scenario
   REINFORCEMENTS instead. Deliberate scope control — no AI naval planning, no EV model, no headaches.
7. **`TRN_NAVAL` gets a profile of its own, with decent defences all round** — not the inherited Truck
   archetype. See §F.9b.5.

### F.9b ✅ SECOND ROUND ANSWERED (Bob, 2026-08-10)

1. **ONE HP POOL.** "The naval HP IS the unit HP, nothing hidden." Settled (§F.4).
2. **Embark costs a DeploymentAction; debark is free.** Plus two structural rules:
   - ⚠ **A naval unit may NOT spend deployment actions while AT SEA.** Falls out of the debark gate
     already built in P2 (port or, for Marines, beachhead) — but it must also be impossible to
     deploy-DOWN to Mobile in mid-ocean. One guard, not a new mechanism.
   - ⚠ **A moving naval unit must ALWAYS retain at least one deployment action**, so arrival can never
     be blocked by an exhausted action budget.
3. **THE NAVAL ACTION BUDGET: 1 Move, 1 Deployment, 0 Intel, 0 Opportunity** (Bob). Agent adds
   **0 Combat** — a naval unit never initiates combat (precept 1) — flagged in §F.9c.
4. **ANTI-AIR: give it a REAL, fairly strong GAT — and ZERO opportunity actions.** ⚠ This is a
   deliberate door left open, and the mechanism is exact: §11.8 air-defence opportunity fire costs
   **1 OpportunityAction per shot**, and §11.4.8.5 in-hex fire against strike aircraft costs one too.
   A unit with a strong GAT and no opportunity actions therefore **declares the capability but can never
   spend it** — naval air-defence is switched off by the action budget, not by a rule that would have to
   be found and unwritten later. A future game grants opportunity actions and naval AA comes alive with
   **no rule changes at all.** Clean, and it costs nothing now.
5. **Damage-only against ships — CONFIRMED.** "Once they go out to sea, they need to be directed by
   hand; no retreats, only arrival or surrender." No stand check, no retreat, no displacement — which is
   what spares `RetreatResolver` from ever answering "where does a ship retreat to?"
6. **Supply trace / water:** solved structurally by the PORT being a depot (§F.11). The trace never
   crosses water; supply originates at the harbour.

### F.9c ✅ THIRD ROUND ANSWERED (Bob, 2026-08-10)

1. **Deterministic free-hex auto-pick — AGREED.** Prefer the hex adjacent to BOTH the port and the
   unit's current hex; otherwise the lowest-index free one. No player prompt, replays identically from
   a save.
2. **No harbour supply.** REJECTED — see §F.6. Water is water.
3. **Naval combat actions: ZERO in this game**, with navy planned as a serious component in the next.
   Same door-left-open shape as the GAT/opportunity pairing — the capability is expressible, the budget
   is what switches it off.
4. **Ports ARE a subset of depots** and carry depot parameters.
5. **Naval units get a FULL slate of ratings**, like any ground unit — a complete 17-stat line, not a
   defensive stub.
6. **Map validation — yes, a cursory check.**

### F.9d ✅ FINAL ROUND ANSWERED (Bob, 2026-08-10) — design is CLOSED

1. **Beachhead supply:** the hex is range 1 from the nearest depot and supply flows through it normally
   (§F.6a). A property of the hex, not of who stands on it.
2. **PORTS: repairable while standing, REPURCHASABLE once destroyed.** ⚠ **AMENDED LATER THE SAME DAY —
   the earlier "once destroyed they are gone" ruling is SUPERSEDED** by the §J.2 objective fix. Final
   position, which is the airbase model (§11.7.2.7) with a shorter fuse:
   - Scenario designers place the initial ports.
   - A standing port is **REPAIRED like a ground unit**.
   - A DESTROYED port may be **REPURCHASED at heavy prestige** via Requisition, placed on the
     friendly-controlled surviving `IsPort` site, **inert for 3 turns** before it allows supply or naval
     operations.
   - **Non-core, battle-only** — never enters the CFR Reserve, never carries across a campaign, counts
     against neither `coreForcePointCap` nor `deploymentPointCap`.
3. **Port art:** Bob is making one sprite. Needs `MapIconType.Port` alongside it.
4. **`FacilityType` append** — safe, relay to the editor.
5. **No friendly stacking on the port hex** — units embark from adjacent, same as any other base.
6. **Supply is designed but unimplemented, and that is FINE** — these structural changes land without
   it. Only the trace-dependent items (§F.6a beachhead conduit, F-C11 water exclusion) wait for §15.

### F.10 DOC + CODE WORK THIS CREATES (folds into §A / §C)

- [ ] **F-A1** Rewrite §5.4.2 end to end: delete instant resolution, delete the destination marker,
      add hex-by-hex sea movement, the sea supply clock, and the damage-only combat rule.
- [ ] **F-A2** Retire §24.7a.3 (Naval Movement Marker) and the §21.8 instant-sealift summary.
- [ ] **F-A3** Amend §9.10.6.3 SCOPE — "instant port-to-port is IN, finer over-sea movement is
      DEFERRED" is exactly backwards now.
- [ ] **F-A4** §9.10.6 mechanism paragraph — already owed (§A7), same edit.
- [ ] **F-C1** Replace `MovementModeService.IsSealiftedNow`'s prohibition with naval movement rules in
      both `HexMapUtil` passes: water passable, non-port land blocked, impassable blocked, flat 1 MP.
- [ ] **F-C2** Sea supply tick + efficiency ladder + "lost at sea" in `BattleManager`'s Upkeep.
- [ ] **F-C3** Damage-only combat against naval in `CombatResolver` (no stand/surrender/retreat).
- [ ] **F-C4** Naval as a legal air-mission and attack-helo target; port-hex combat exception.
- [ ] **F-C5** `Domain` enum + the §D/§F.7 renames.
- [ ] **F-C6** Naval profile pass — a real `FamilyArchetypes.Naval` + `TRN_NAVAL` stat line (§F.9b.5).
- [ ] **F-C8** `BALLISTIC_MISSILES_MAY_TARGET_NAVAL = false` as a named, flippable rule (§F.4), plus the
      §11.7.5.2 amendment.
- [ ] **F-C9** Storm doubles the at-sea supply tick (§F.6).
- [ ] **F-C10** Stacking: only fixed-wing may temporarily share a hex — tighten the stop-test so helos
      are excluded from sharing with naval as well as ground (§F.2).
- [ ] **F-C11** ⚠ **Exclude water from the supply trace and from HCL/ownership** — a latent cross-ocean
      supply path. Safe to do now ONLY because the port is a depot (§F.11); without that it would
      strand every landed force.
- [ ] **F-C12** ⚠ **NEW BASE TYPE: `FacilityType.Port`** (§F.11) — the heavy lift of this pass. Append
      to the persisted enum; site flag `HexTile.IsPort` persists as the installation's site, exactly as
      `isAirbase` does. Inherits base HP/OC/destruction/no-capture/repair-lock. Acts as a supply depot.
      ⚠ Touches: `CombatUnit` facility init, `SnapshotMapper`, the `.oob`/`.map` contract, the scenario
      editor (relay!), `GameIconRenderer` (a port icon), and §15 supply generation.
- [ ] **F-C13** Embark/debark across the port: adjacency gating, the deterministic free-hex pick, and
      the "no free hex ⇒ prohibited" refusal on both sides (§F.9c.1).
- [ ] **F-C14** Naval action budget — 1 Move / 1 Deployment / 0 Intel / 0 Opportunity / 0 Combat, the
      no-deployment-at-sea guard, and the always-retains-a-deployment-action guarantee (§F.9b.2–3).
- [ ] **F-C15** ⚓ **Beachhead-as-supply-source** (§F.6a) — a Marine-held `IsBeachhead` hex is in supply
      from the nearest friendly port at zero distance penalty. ⚠ Without this, amphibious assault is a
      mechanic for losing regiments slowly.
- [ ] **F-C16** Port icon + `MapIconType.Port` (§F.9d.3) — **needs art from Bob.**
- [ ] **F-C7** Tests: sea movement and the land/port boundary · the supply clock to loss · damage-only ·
      marine-vs-everyone debark · landing blocked by occupancy not control · no ZoC, no tile flip,
      no objective capture from the sea.
- ⚠ **No `SAVE_VERSION` bump needed for naval itself** — `IsNavalEmbarked` is already persisted and the
  clock rides existing supply/efficiency fields. (The helo over-water flag in §C3 still needs 5 → 6.)

---

## H. ▶ THE EXECUTION PLAN (2026-08-10)

**Where this sits.** The original thread is the **profile-slot rebuild** (`todo_profiles.md`): P0–P3
done and committed, **P4 (requisition) and P5 (content/docs/editor) still owed**. P3's naval question
opened the air audit, which produced this file. So there are TWO live threads and this plan sequences
both — the domain pass first (it changes vocabulary everything else would otherwise be written in),
then the profile-rebuild remainder.

**Gate discipline (unchanged):** the agent cannot run Unity. Every phase ends with
**"Please run Unity Test Runner for me"** and waits. A phase is not `[x]` until Bob reports green.

**Play-testing gate:** ⚠ **Khost has no water, no ports and no beachheads.** Phases N0–N3 are
code-and-suite-verifiable but **not playable** until Bob's coastal test map exists. Build them anyway;
just do not expect to see them run.

---

### PHASE D0 — ✅ CODE COMPLETE 2026-08-10, ⚑ SUITE RUN OWED
**Landed:** `Domain { Ground, Air, Naval }` in GameData · `CombatUnit.OccupiesDomain` ·
`IsAirUnit` + `IsFixedWingAirUnit` → **`IsFixedWing`** (delegating to the canonical GameData list) ·
`IsAirborneSpottingTarget` → **`IsSeenAsAir`** · occupancy re-keyed on `OccupiesDomain` in
`GameDataManager` and `HexMapUtil` · `CombatResolver` ×4, `SpottingService` ×2, `GameIconRenderer`
and `CombatUnit` internals re-pointed · the two private helper spellings folded into the canonical
list · docs `Claude_Project` §5 + §11. **Zero live references to the old names remain.**
⚠ **D9 was worse than recorded: FOUR lists, not three** — `IsAirUnitClassification` had only 4
members, so **an AWACS could never attach to an airbase** (`AddAirUnit` throws on rejection) and a
**transport aircraft sat in the GROUND layer**. Both die with the consolidation.

### PHASE D0 — VOCABULARY FIRST (mechanical, no behaviour change)
*Why first: every later phase would otherwise be written in the vocabulary we are replacing, and then
have to be re-touched.*
- [ ] `Domain { Ground, Air, Naval }` enum; `OccupiesDomain`; `IsSeenAsAir` (boolean — two spotting
      ranges only, §F.7)
- [ ] Collapse `IsAirUnit` + `IsFixedWingAirUnit` → **`IsFixedWing`** (identical expressions today)
- [ ] Re-point every rule site: `HexMapUtil` stop-test, `GameDataManager` occupancy,
      `GameIconRenderer` layers, `CombatResolver`, `SpottingService`
- [ ] Enforce the §D discipline: **rule code asks the question-named property; only the derivation
      asks the classification**
- [ ] ⚠ **ABSORB DEFECT D9 while here** (`todo_profiles.md` §5): there are THREE disagreeing "is
      fixed-wing" lists — `GameData.IsAirborneClassification` (7 members, incl. WW + TRN),
      `CombatUnit.IsAirUnit` (5, missing WW + TRN), and a third in `GameIconRenderer`. Consolidate to ONE
      helper in `GameData`. ⚠ **The 7-member list is the correct one** — §12.3.7 lists WW and TRN as
      fixed-wing, and the An-12 is literally a transport aircraft.
- [ ] Docs B4, B5; vocabulary ban on bare "air unit"
- **Test:** whole suite green.
  ⚠ **SUCCESS CRITERION IS "NO BEHAVIOUR CHANGE — WITH ONE DELIBERATE EXCEPTION."** Consolidating D9's
  lists FIXES A LIVE BUG: because `IsAirUnit` omits WW and TRN, a transport aircraft is currently filed
  in the GROUND occupancy layer — so it projects zone of control, cannot share a hex, and is exposed to
  ground ambush. After D0 it is correctly air-domain. Any test that moves or stacks a TRN/WW unit may
  legitimately change result; anything else changing is a regression.

### 🔴 D1 BLOCKER FOUND 2026-08-10 — **GROUND AMBUSH HAS NEVER DEALT DAMAGE**

`CombatResolver.ResolveAmbush` is fully implemented and covered by `CombatResolverTests` +
`LeaderSkillCombatTests` — and has **ZERO live callers**. `EventManager.OnAmbushTriggered` has
**ZERO subscribers**. So `MovementController` raises an inert event: today an ambush **halts the mover,
prints a dispatch and plays a sound, and deals NO DAMAGE AT ALL.** ⚠ The P3 comment claiming "that
event is what resolves the DAMAGE" was WRONG — it resolves nothing.

**This blocks the helo ruling**, which is "the helo takes an ORDINARY attack minus the surprise
multiplier" — there is no ordinary attack to take. It is also much bigger than the helo question:
**§6.9 ambush is a core mechanic that has been inert for the whole project**, and the §12.4.9 /
§6.9.3 reveal that should follow it never fires either.

**Recommended fix — a new `AmbushAction` orchestrator**, sibling to `GroundCombatAction` /
`IndirectCombatAction`, matching the ratified §2.7 shape (Unity layer → orchestrator → pure resolver).
It would: call `ResolveAmbush` · apply the §6.9.3 Level-1 reveal (which is exactly what P3's
`SpottingService.RevealAmbusherToContact` already does — it turns out NOT to be dead) · displace via
`RetreatResolver.ResolveDisplacement` on a non-Hold outcome, mirroring `GroundCombatAction` · end the
mover's turn (§6.9.6) · return an outcome the controller reports.
The helo rule then becomes ONE line inside `BuildAmbushLane`: `AmbushScalar` → 1.0 when the mover's
medium is `Helo`. ⚠ Keep the §7.10.1 embarkment malus (`moverEmbarked ? 2.0f` + `BandShift`) — that is
"lift aviation is glass", a DIFFERENT rule from the surprise multiplier, and Bob's ruling named only
the latter.

✅ **SCOPE CALL TAKEN (agent, 2026-08-10 — Bob delegated it): FOLDED INTO D1 AND BUILT.** It is the same
call site, the resolver was already finished and tested, and leaving §6.9 inert to protect a phase
boundary would have been bookkeeping over correctness. Bob confirms the missing caller was an unfinished
pass, not a deliberate exclusion.

**What landed:** new `Assets/Scripts/Models/Combat/AmbushAction.cs` — damage + stand check via
`ResolveAmbush`, the §6.9.3 Level-1 reveal, `RetreatResolver` displacement on a non-Hold outcome,
unregister-on-removal, prestige reported. `MovementController` calls it and collapses to ONE path for
ground and helo alike. `SurpriseScalarAgainst` in `CombatResolver` denies the ×1.5 to a helo-medium
mover while leaving the §7.10.1 embarkment malus intact. `MovementHalt.FlightEvasion` and
`PrinterDispatch.ReportFlightHalted` DELETED with the rule they served.

✅ **§7.15 DEGRADATION — RULED YES (Bob).** Applied to the VICTIM ONLY: the ambusher takes no return
fire (§6.9.5) and was never under pressure.

### 🔴 THE OVERRUN BUG — DIAGNOSED AND FIXED 2026-08-10 (Bob's "movement is a little borked")

**Symptom (Bob):** ground units overrun unspotted ground units — moving over them without interference.
**It was TWO faults in the same loop, and BOTH also disabled the ambush**, which is why it had to be
fixed before D1 could be play-tested at all.

1. **No pre-step occupancy check.** `GetValidMoveDestinations` deliberately ignores unspotted enemies so
   the overlay cannot leak their position through fog (§12), and `HexMapUtil`'s own comment promises the
   mid-move sweep "reveals and halts on contact instead". **That halt was never written.** The execution
   loop deducted movement points and called `MoveUnitTo` with no occupancy test, so a regiment walked
   straight onto and through a hidden enemy.
   **Fix:** a pre-step contact check — reveal the blocker to contact, halt BEFORE entering, keep movement
   points for a combat or intel action. New `MovementHalt.Contact` (same consequence as a ZoC halt, named
   for its own cause) + `PrinterDispatch.ReportMoveBlockedByContact`.
2. **⚠ THE SPOTTING SWEEP RAN BEFORE THE AMBUSH CHECK AND SPOTTED THE AMBUSHER OUT OF EXISTENCE.**
   `CheckGroundAmbush` requires an adjacent enemy still at `Level0` — but `CheckSpottingForMover` ran one
   line earlier and raised that very unit to Level1. Worse, the branch was gated on
   `newlySpotted.Count > 0`, which is the wrong precondition entirely (the rule is "an unspotted enemy is
   adjacent", not "I just saw something"). Between them, §6.9 could effectively fire only when the mover
   spotted one enemy while a DIFFERENT one lay in wait.
   **Fix:** ambush check moved ABOVE the spotting sweep and the `newlySpotted` gate dropped. If it was
   still hidden when you arrived, you blundered into it.

⚠ **Both faults were invisible in EditorTests** — the tests see all units and never walk a path past a
Level0 enemy. This is the fog-of-war blind spot the suite structurally cannot cover, and it wants a
play-test rather than a unit test.

### PHASE D1 — AIR RULE CORRECTIONS (small, closes doc/code gaps)
- [ ] **Helo ambush:** raise `RaiseAmbushTriggered` on the helo branch; suppress the §6.9 ambush
      multiplier when the victim travels by helo; fixed-wing stays wholly immune
- [ ] **Impassable closed to ALL air** — move the rejection above the airborne early-out in
      `ComputeStepCost`
- [ ] **Only fixed-wing may share a hex** — tighten the stop-test so helos share with nothing
- [ ] Docs A2, A4, A5, B1
- **Test:** suite + the P3 `MovementTests` region. ⚠ **`CombatOracleTests` MUST be re-run** — suppressing
  the ambush multiplier is a combat-constant change and the oracle is the AI's decision basis (§J.1).
  **Play-test:** ambush a helo, fly into a mountain wall.

### ⚓ HELO TRANSIT — THE RULE THAT SETTLED IT (ratified 2026-08-10 after a play-test)

**A helicopter is never STOPPED by ground troops — it is SHOT AT by them, and the §11.8.9 transit stand
check decides whether the sortie survives.** It overflies occupied hexes, takes ambush and air-defence
fire on the way, and for each DAMAGING event rolls: **hold** → carries on with what it has left;
**abort** → free return to the launch hex, movement points and actions to zero, and an embarked lift
sets its troops down at the origin. Shock accumulates across the move, so the second burst is far
likelier to break the sortie than the first.

⚠ **THIS IS BOB'S OWN §11.8.9, GENERALISED** from air-defence fire to ground ambush as well — not a new
mechanic. It also explains why `HeloTransitStandCheck` was written and left callerless.

⚠ **THE GROUND/HELO DIVERGENCE IS THE RULE, NOT AN INCONSISTENCY TO TIDY AWAY.** At the identical
trigger — an unspotted enemy adjacent — a GROUND unit stops dead and a HELICOPTER flies on. Anyone
"fixing" one to match the other breaks both. Recorded here because it is exactly the kind of asymmetry
that reads as a bug to a future reader.

⚠ **NO FALLBACK FOR AN OCCUPIED ORIGIN, and that is deliberate** (Bob): a move order is ATOMIC with
respect to input, so nothing can occupy the launch hex between departure and abort. The guarantee holds
only while input stays gated during `MovementState.Executing` — noted at the method.

⚠ **SUPERSEDES §5.13.2.2** ("the helicopter's turn ends after taking the ambusher's attack") and the
short-lived "helos may not pass through an occupied hex" reading, which was withdrawn the same day: it
would have made the entire overflight risk model unreachable, since a helicopter blocked from crossing
hostile ground can never be shot at while crossing it.

✅ **ANTI-DOGPILE — RATIFIED AND BUILT (Bob, 2026-08-10).** One ambush per ambusher per aircraft per
move order, mirroring §11.8.6's existing one-shot-per-aircraft limit for air-defence fire rather than
inventing a parallel rule. `CheckGroundAmbush` takes an exclusion set; `MovementController` keeps it per
move order. Matters specifically BECAUSE a helicopter now flies on: without it the same regiment engages
it at every hex still in reach and accumulating Shock breaks the sortie on geometry alone.

### ✅ OVERHEAD FIRE — THE GAD RULE (ratified Bob, 2026-08-10). D2 builds it.

**A helicopter that flies DIRECTLY OVER an enemy ground unit's hex is fired on by that unit using its
GAD as the attack value** — Δ = the ground unit's **GAD** − the helo's **GAD** — followed by the
§11.8.9 transit stand check and feeding the same `hpLostThisMove` Shock accumulator. Fixed-wing is
exempt (too high for organic weapons). One engagement per unit per move under the same anti-dogpile
rule as ambush.

**Bob's reasoning, worth keeping:** the game is REGIMENTAL scale, so nearly every formation carries
organic and fairly serious air defence. GAT was deliberately scoped to *dedicated* air-defence systems
because it was written with jets in mind. A helicopter passing directly over a US tank brigade would
realistically eat a moderate amount of fire or worse. And air-mobile operations should punish
carelessness — **this is what gives recon units real work**, because you now need to know what is under
your flight path, not merely where you are going.

⚠ **GAD IS A DEFENSIVE STAT USED HERE AS AN ATTACK VALUE, AND THAT IS DELIBERATE.** It already encodes
"how much organic anti-air does this formation have", so it is the right number with no new field to
author or balance. Do not "correct" it to GAT — GAT is the dedicated-AD path (§11.8, ranged, GAT ≥ 6),
and this is the separate overhead case.

⚠ **THE TRIGGER IS THE SAME HEX, NOT ADJACENCY OR RANGE** — and that narrowness is what makes it safe.
The agent had argued against letting ordinary ground units shoot at helicopters, on the grounds that
Shock would break every sortie on geometry alone. That objection does NOT apply here: overflight is
avoidable by ROUTING, so this makes the flight PATH a decision rather than making flight suicidal.
Bob's version is better than what the agent proposed.

🔵 **CALIBRATION OWED:** whether GAD-as-attack yields sensible deltas against real profiles is a numbers
question nobody has checked. A truck archetype sits at GAD 6; a helo's own GAD carries its evasion role.
Worth a pass over actual values before this is balance-tested.

### 🔵 RANGED AD FIRE — the existing rule is STAT-GATED not CLASS-GATED

Bob (2026-08-10): *"if the helo flies over a non-air defense ground unit, it should face a GAD attack."*
**Agreed in direction, and §11.8 already ratifies it — with one important qualifier.**
- The lane is §11.8.1: **Δ = firer's GAT − helo's GAD** (helo air stats are zero, so GAD carries the
  evasion role, §7A.14). ⚠ The ATTACK stat is **GAT**; GAD is what the helo DEFENDS with.
- Who may fire is §11.8.2: **any unit with GAT ≥ 6** (`GAT_INTERDICT_THRESHOLD`). Below that, GAT is
  treated as 0 and the unit does not engage at all. It is NOT restricted to SAM/AAA classifications —
  §11.4.8.5 even calls the category "air-defense-CAPABLE units (GAT ≥ 6)". Infantry reach it through the
  MANPADS trait's GAT floor.
- ⚠ **THE CODE IS NARROWER THAN THE DESIGN.** `SpottingService.CheckAirAmbush` filters on
  `Classification == SAM/SPSAM/AAA/SPAAA` — the same class-instead-of-property mistake as the old
  `IsAirUnit`. **Re-key it on the GAT threshold in D2.**
- ⚠ **KEEP THE THRESHOLD; do NOT let every ground unit shoot.** If a plain rifle regiment engages
  overflying helicopters, no helicopter can cross contested ground at all, Shock breaks every sortie, and
  air-mobile play is dead. The GAT ≥ 6 gate is precisely what keeps overflight a calculated risk instead
  of suicide — it is the counterweight that makes the whole "fly on and take the fire" model work.
- Also owed in D2: the §11.8.8 towed-posture gate (towed AD cannot fire while limbered) and the §11.8.3
  per-turn shot budgets.

### PHASE D2 — THE HELO AIR-DEFENCE PATH ✅ CLOSED 2026-08-11 — 514 TESTS GREEN + PLAY-CONFIRMED (Bob)
**Bob played several turns as a real game: AAA and SAM fire on helos in transit works.** No tuning notes
taken — deliberately deferred, the pass is accepted as-is.

⚠ **THE FIXED-WING HALF IS BUILT BUT HAS NEVER RUN IN PLAY, and cannot yet.** Bob, 2026-08-11: *"the
mechanisms to run air missions are not in the game yet, so I can only subject helos to the opportunity
fire."* So the §5.13.3.2 1d6 detection roll, the fixed-wing (MAN+SUR)/2 Δ axis and the "takes the damage
and presses on, no transit stand check" branch are all suite-covered and **play-unverified**. They go
live the day the AOB/air-mission layer does — re-test them then rather than assuming D2 covered them.

⚠ **Bob is building an AD-heavy TEST SCENARIO** (2026-08-11) — Khost "simply doesn't have too much air
defense". 🔵 If it can also be made COASTAL it retires the standing N0–N3 blocker in one artifact.
⚠ **MERGED WITH THE ABOVE** — ambush and air-defence fire share one call site and one stand check,
so this was the same pass, not a second one.
- [x] Split the transit path: **fixed-wing → 1d6 detection roll; helo → NO roll, takes the hit**
      (`SpottingService.RollFixedWingAmbushDetection`, named so the helo case cannot call it by accident)
- [x] Wire `CombatResolver.ResolveAirDefenseFire` then `HeloTransitStandCheck` — both had zero callers
- [x] **DELETED the `UnityEngine.Random.Range(0,2)` coin flip** and its TODO
- [x] Abort outcome — reuses the existing `AbortFlightToOrigin` (free return, MP/actions 0, lift sets down)
- [x] Eligibility = `GameData.IsAirDefenseClassification` (SAM/SPSAM/AAA/SPAAA). ⚠ **The punch list's
      "re-key onto `GAT_INTERDICT_THRESHOLD` ≥ 6" was WRONG and is RETIRED** — see the ruling below.
- [x] §11.8.8 towed-posture gate · §11.8.3 shot budget · §11.8.6 anti-dogpile
- [x] **Overhead fire (the GAD rule)** — `CombatResolver.ResolveOverheadFire` + same-hex trigger
- [x] `AirDefenseTransitTests` (17 tests) · doc §11.8.3a/b + §11.8.11 written
- [-] **Doc A5 was ALREADY SATISFIED** — §5.13.2.4 and §5.13.3.2 both state the split plainly and
      always did. The DOC was right and the CODE was wrong; nothing to amend.
- [x] **Test:** full suite — 514 GREEN. **Play-test:** helo past a SAM — CONFIRMED WORKING.

#### What D2 changed beyond the punch list, and why
1. **⚠ SPOTTED AIR DEFENCE NOW FIRES.** The old scan required `SpottedLevel.Level0` and returned on the
   FIRST match, so a SAM the player had already located was completely harmless and only one battery
   could ever engage. §11.8.4 makes the opportunity automatic for any eligible unit in range; §6.10 air
   ambush is the narrower *unspotted* case that buys a fixed-wing mover one detection roll. This is a
   real difficulty increase and the biggest thing to watch in the play-test.
2. ✅ **RESOLVED — THE GATE IS THE CLASSIFICATION (Bob, 2026-08-11).** Only **SAM / SPSAM / AAA / SPAAA**
   may interdict a transiting aircraft at range. The agent's GAT re-key (which this file had asked for)
   is REVERTED; `GameData.IsAirDefenseClassification` is the gate.
   **Bob's reasoning, which is the part to keep:** making GAT > 0 exclusive to true air-defence units
   would have expressed the gate through the stat, *but that breaks the stat-comparison paradigm* — every
   unit must carry every stat for a Δ to be well defined. So GAT stays a universal ATTACK VALUE and
   "is this an air-defence unit" is asked separately.
   **The agent's supporting finding stands and reinforces it:** a `GAT ≥ 6` test would not have produced
   the intended set anyway, because `MANPADS_BASIC` floors *infantry* GAT at exactly 6 (STINGER/IGLA: 8).
   ⚠ **Correction to Bob's message: MANPADS units are NOT classified SAM.** MANPADS is a TRAIT on
   `INF_*` profiles (regular / airborne / air-mobile / marine / Spetsnaz, every faction); no SAM- or
   AAA-classified template carries one. That makes the classification gate exactly right with no
   special case — and infantry organic anti-air is still modelled, as §11.8.11 overhead GAD fire.
   Recorded as DesignDoc §11.8.2a/b/c; pinned by `ManpadsInfantry_HasRealGat_ButIsStillNotAnAirDefenceUnit`.
   🔵 One consequence worth knowing: the MANPADS traits' GAT floor now has **no live consumer** — GAT is
   read only in the §11.8.1 lane, which only dedicated batteries reach. The traits still cost prestige
   and still read as flavour; whether they should confer something else is an open (small) question.
3. **§11.8.3's per-system table is unbuildable as written** — "Tunguska 3" names a weapon profile, not a
   classification, and no per-profile shot stat exists. The budget is §8.5.8's flat 2 via
   `OpportunityActions`; recorded as §11.8.3a rather than faked.
4. **§11.8.8 is coded as a POSTURE test, not the doc's towed/self-propelled class split**, because a
   self-propelled type has no Mobile bay and can never be limbered — excluding Mobile reproduces both
   halves with no list to drift. ⚠ Breaks if an SP air-defence unit ever gains a Mobile bay.
5. **The ambush anti-dogpile set is now shared with overhead fire** (renamed `enemiesEngagedThisMove`),
   per the ratified "one engagement per unit per move". The §11.8.6 *ranged* cap is separate and lives
   on the firing unit (`CombatUnit.MarkAircraftEngaged`) because it is per TURN and spans aircraft.
- 🔵 **Watch in the suite run:** `ResolveTransitFire` reaches `EventManager.Instance`, which lazy-creates
  a GameObject and calls `DontDestroyOnLoad` — harmless in edit mode but it logs a warning. If the two
  integration tests are noisy, the fix is a non-creating `EventManager.Existing` accessor.

### PHASE D3 — HELO OVER WATER + `SAVE_VERSION` 6 ✅ CODE-COMPLETE 2026-08-11 (⚑ suite run owed)
- [x] Persisted `EndedTurnOverWater` on `CombatUnit`; still over water on the SECOND Upkeep ⇒ lost,
      full remaining equipment booked to the ledger, unit unregistered
- [x] **`HexMapUtil.CanRestAt` now lets a helicopter stop over water** — this was the real blocker and it
      was not on the punch list. `CanRestAt` rejected Water for anything in the GROUND domain, and a helo
      occupies the ground domain for stacking, so a helo ordered onto water was silently displaced back to
      land by the post-move settlement and the rule could never come up at all. Keyed on
      `MovementModeService.IsAirborneNow` — "is it flying right now" — so nothing walking is affected
- [x] **Turn warning + selection info box** — `PrinterDispatch.ReportStrandedOverWater` /
      `ReportLostAtSea`, plus a persistent line in `Prefab_UnitPanel.BuildFriendlyLines`
- [x] `SAVE_VERSION` 5 → 6 with the pre-1.0 clean-break note; `MigrateStep`'s NOTE updated to name all
      three step-less bumps. ⚠ `CombatUnit` serialises DIRECTLY into `GameStateSnapshot.Units`, so the new
      field needed no snapshot plumbing — but that is also exactly why it forces a version bump
- [x] Doc A3 → DesignDoc **§5.13.2.7** (+ .1–.5)
- **⚑ Test:** full suite. `OverWaterGraceTests` (10 tests) added; `SaveMigrationLadderTests` is
  version-agnostic (it injects versions) so the bump needs nothing there.

#### ⚠ ONE DEVIATION FROM THE PUNCH LIST, AND IT IS THE WHOLE RULE
This file said "checked at Refresh". **Refresh is wrong and gives ZERO turns of grace** — it fires at the
START of a turn, before the helicopter has had the very move the rule exists to give it. The check runs at
**Upkeep** ("the end of your turn"), which is what "must reach land by the end of its next move" actually
says. Recorded as DesignDoc §5.13.2.7.2 so the Refresh version is not restored later.

🔵 **Not playable on Khost (no water).** Suite-verified only until Bob's test scenario has a coastline —
the same artifact that unblocks N0–N3.

### PHASE D4 — FIXED-WING STAGING AT AIRBASES
- [ ] Boarding while adjacent to a friendly airbase free-moves the loaded transport onto the base
- [ ] **Consumes one slot; REFUSED UP FRONT at capacity** — invariant never violated even transiently
- [ ] `AddAirUnit` currently **throws** for a non-air classification; a loaded `AB` regiment needs its
      own attach path
- [ ] `MAX_AIR_UNITS` stays a constant Bob can raise (4 → 6); check the stack sprite still reads
- [ ] Forced-evac (§11.7.2.4) takes the loaded transport **with its regiment aboard**
- [ ] Doc A8
- **Test:** suite. **Play-test:** load paras at a full base and at a base with room.

---

### PHASE N0 — NAVAL FOUNDATIONS
- [ ] `FamilyArchetypes.Naval` (`medium: MovementMedium.Naval`) + a **full 17-stat line** for the naval
      profile: **GAD strong** (airstrike Δ), **HD/SD decent** (helo + artillery Δ), **GAT fairly strong
      but unusable** (0 opportunity actions), **no intel stats** so a sinking books the REGIMENT's
      equipment
- [ ] **Action budget: 1 Move · 1 Deployment · 0 Combat · 0 Intel · 0 Opportunity**, the
      no-deployment-at-sea guard, and the always-retains-a-deployment-action guarantee
- [ ] **Replace P3's `IsSealiftedNow` prohibition with real movement:** water passable at flat 1 MP,
      non-port land blocked, impassable blocked. ⚠ Keep the half that was right — a sealifted unit must
      never walk inland
- [ ] Doc F-A1, F-A3
- **Test:** new naval suite — sea movement, the land/water boundary, the action budget.

### PHASE N1 — NAVAL COMBAT + THE SEA CLOCK
- [ ] **Damage-only:** no stand check, no surrender check, no retreat, no displacement. `RetreatResolver`
      never asks where a ship retreats to
- [ ] Legal attackers: fixed-wing air mission · attack helo (ground pipeline) · **artillery indirect**
- [ ] **`BALLISTIC_MISSILES_MAY_TARGET_NAVAL = false`** — a named, flippable rule, never an inline check
- [ ] **Sea clock:** 1 day/turn from the turn of embarking (**no harbours**), doubled in Storm; 0 supply
      ⇒ one efficiency rung per turn; bottom rung ⇒ **lost at sea** via the surrender machinery,
      equipment booked, renamed in player-facing text
- [ ] ⚠ **`CombatOracle` FAIL-CLOSED WORK LANDS HERE (§J.1)** — add an explicit `Unsupported` result,
      teach it damage-only resolution (structurally SIMPLER: no stand branch, no retreat branch), and
      re-point the drift guard to enumerate from the real rule surface so a new target class fails as
      missing coverage instead of quietly passing.
- [ ] Doc F-A1, F-A2, §11.7.5.2 amendment
- **Test:** damage-only pins, the full clock to loss, BM refusal, artillery permission, **+ the oracle
  drift guards re-run**.

### PHASE N2 — THE PORT BASE TYPE *(the heavy lift)*
- [ ] **`FacilityType.Port`** — append to the persisted enum. Site flag `HexTile.IsPort` persists as the
      SITE, the unit is the INSTALLATION (the ratified airbase pattern, §11.7.2.7)
- [ ] Inherits base HP 60, OC degradation + strategic premium, no capture, ZoC repair-lock, salvage
- [ ] ✅ **Repairable like a ground unit while standing; REPURCHASABLE once destroyed** (the §F.9d.2/§J.2
      amended ruling, made authoritative by Bob 2026-08-10 — the earlier "destroyed is permanent" bullet
      that stood here was the superseded first ruling)
- [ ] Depot sub-type carrying depot parameters
- [ ] **Embark/debark across the port:** adjacency gate · deterministic free-hex pick (prefer the hex
      adjacent to both port and unit, else lowest index) · **no free hex ⇒ prohibited**, both sides
- [ ] `MapIconType.Port` + Bob's sprite; `GameIconRenderer` branch
- [ ] Map validation: port with no adjacent water, or no adjacent free ground
- [ ] ⚠ **RE-POINT THE EMBARK GATE (§J.4).** `MovementController.IsOnPortHex` reads the SITE FLAG; the
      rule is "adjacent to an ACTIVE FRIENDLY PORT UNIT" (Bob). It will keep working while being wrong —
      pin it with a test. Same flag-vs-installation fix in `RegionGraph.HasPort` (§J.11).
- [ ] ⚠ **PORT REPURCHASE (§J.2, amended ruling):** destroyed ports may be REBOUGHT at heavy prestige on
      the friendly-controlled surviving site, **inert 3 turns**, non-core / battle-only. Needs
      `PORT_REPURCHASE_COST` and a call on whether an inert port is untargetable (§11.7.2.7.4).
      ⚠ **The shop UI is P4's** — this phase builds the mechanics, P4 grows by one purchase path.
- [ ] Doc F-A3; **editor relay (§I)**
- **Test:** embark/debark both directions, congestion refusal, port destruction stranding ships,
  repurchase + the inert countdown.

### ⏭ FOLLOW-ON (not in this pass, but ruled) — MOVE UNDO
§J.5 records the ratified design: an **undo-barrier accumulator** that stores a REASON, set by
information change, consequential dice, world change, other-unit change, or a phase boundary. Undo is
offered only when a move touched nothing but the mover's own position, facing, MP and move action.
Naval is covered for free. ⚠ Reconcile with DesignDoc §5.11 when it is written up.

### PHASE N3 — SUPPLY HOOKS ⛔ GATED ON §15
*Everything trace-dependent. §15 supply is designed but `BattleManager.ProcessUpkeep` is still a stub.*
- [ ] **Water excluded from the supply trace and from HCL/ownership** — safe ONLY because the port is a
      depot; without that it strands every landed force
- [ ] **Beachhead = range 1 from the nearest depot**, supply flowing through it normally
      ("as if a depot was on a narrow peninsula")
- **Do not start before the supply pass.** Build N0–N2 without it.

---

### THEN: THE ORIGINAL THREAD
- [ ] **P4 — Requisition API** (`todo_profiles.md` §4.6/§4.7): wallet arithmetic, cascade, transaction
      window, `RequisitionService` headless
- [ ] **P5 — Content / docs / editor** (`todo_profiles.md` §7 amendments table, `Transition.md` rewrite).
      ⚠ Merge with §A/§B of this file — they are the same doc-reconciliation job and should be one pass.

---

## I. 📨 SCENARIO EDITOR RELAY (Bob is the courier — send when N2 lands)

**Format/contract changes the editor must know about:**
1. **NEW `FacilityType.Port`** — a persisted enum APPEND (safe; nothing renamed or reordered). Ports are
   **placed by the scenario designer as base units** in the `.oob`, exactly like HQ / DEPOT / AIRB.
2. **A port is a base unit ON a port-city hex**, with `HexTile.IsPort` remaining the SITE flag — the same
   two-part model as `isAirbase` + the airbase unit. Both are authored.
3. **Ports are repairable while standing and REPURCHASABLE once destroyed** (authoritative ruling,
   2026-08-10): rebought at heavy prestige on the friendly-controlled surviving `IsPort` site, inert
   3 turns before allowing supply or naval operations, non-core/battle-only. Initial ports are still
   authored in the `.oob`; the SITE flag persists after destruction, exactly like `isAirbase`.
4. **`HexTile.IsBeachhead` becomes load-bearing** (§9.10.6.2 already defines it): Marine landing sites
   AND, once §15 lands, supply entry points. Scenarios with a coastline should author them deliberately.
5. **Water hexes** will be excluded from ownership/HCL and from the supply trace — so authoring
   ownership on water is meaningless and should not be relied on.
6. **A coastal map needs:** a coastline, **at least two friendly-reachable ports** (embark and debark
   ends), some beachhead hexes, and open water wide enough for a 2–3 turn crossing.
7. **Map validation to add on their side if cheap:** a port with no adjacent water hex is inert; a port
   with no adjacent free ground hex can never unload.
8. Still owed from the previous pass (`Claude_TODO.md` Bob's queue): the `IsEmbarkable` flip is
   authorised, `classificationName` removal is green-lit, leaders may go name-form, narration is
   campaign-only, and always say WHICH KIND of scenario.

---

## J. ⚠ RISK REVIEW — what this pass touches that nobody has flagged (2026-08-10)

*Excludes the known §15 supply gap. These are things that will bite during D0–N3 if not planned for.*

1. **⚠⚠ `CombatOracle` DRIFT GUARDS MUST BE RE-RUN — this pass changes combat constants.**
   `Claude_Project` §2.8 is explicit: `CombatOracle` + `Pmf` are an **exact analytic EV mirror of the
   combat engine**, and `CombatOracleTests` "enumerate the real engine and MUST be re-run after any
   combat-constant change." This pass makes at least four: the helo ambush multiplier suppression (D1),
   damage-only resolution against naval (N1), artillery gaining a new legal target class (N1), and an
   entirely new stat line (N0). **The oracle is the AI's decision basis — if it drifts, the AI makes
   confidently wrong choices and nothing throws.** Add an oracle re-run to the D1 and N1 gates.

   **✅ THE FIX (ratified approach, 2026-08-10) — make the oracle FAIL CLOSED, not silently wrong.**
   Today the oracle answers every question with a number. The durable fix is to give it an explicit
   **`Unsupported`** result for any case it does not model, and have the AI treat that as "do not plan
   around this" rather than acting on a fabricated expected value. That converts every future drift —
   not just this pass's — from a silent mis-decision into a visible gap. Same discipline as
   `AudioFogPolicy` failing closed on a null source.
   Two supporting moves: **(a)** the drift guard should ENUMERATE FROM THE REAL RULE SURFACE (target
   classes, resolution modes) rather than a hand-written list, so a NEW case fails as missing coverage
   instead of quietly not being covered — the same lesson `MovementMediumTests` records about walking
   the real database; **(b)** wherever the oracle and the engine each spell a constant, collapse them to
   one source.
   ⚠ Good news for this pass specifically: **damage-only resolution is STRUCTURALLY SIMPLER than normal
   combat** — no stand branch, no retreat branch — so teaching the oracle about naval targets is less
   work than a normal target, not more.

2. **⚠⚠ AN OBJECTIVE ON A PORT HEX BECOMES UNCAPTURABLE.** §5.5.11 — a standing base blocks enemy entry;
   §11.7.2.8 — bases cannot be captured; §17.5.2 — an objective flips only when a ground unit ENDS
   MOVEMENT on it. So a port city that is also an objective can only be taken by **destroying the port
   first.** **Port cities are exactly the hexes a designer will mark as objectives**, so this would
   surface in the first coastal scenario.

   **✅ RESOLVED (Bob, 2026-08-10) — and the logic checks out.** ⚠ Note the mechanism precisely, because
   it is NOT the purchase that fixes the capture: **DESTROYING the port is what unblocks the hex.** A
   destroyed installation is no longer "standing", so §5.5.11 stops blocking entry, a ground unit ends
   movement there, and the objective flips by the ordinary §17.5.2 rule. The PURCHASE is what makes the
   captured harbour useful again afterwards. Sequence: **destroy → occupy (objective flips) → repurchase
   on the now friendly-controlled site → 3 turns inert → operational.**
   Verified against the existing base rules: placement requires a FRIENDLY-CONTROLLED flagged site
   (§11.7.2.7.2), the site persists as map data after destruction (§11.7.2.7), and non-core/battle-only
   matches §11.7.2.7.1 exactly. Only the fuse differs — 3 turns for a port versus 5 for an airbase.
   ⚠ **Scope note: this puts FACILITY PURCHASE into P4's requisition menu**, which was scoped for
   equipment bays only. P4 grows by one purchase path. ⚠ Needs a `PORT_REPURCHASE_COST` constant
   ("heavy" — the airbase is 300 provisional, so likely 400+) and a ruling on whether an inert port is
   untargetable like an inert airbase (§11.7.2.7.4).

3. **⚠ `IsPort` / `IsAirbase` / `IsFort` ARE MUTUALLY EXCLUSIVE ON A HEX** (`Claude_Project` §3.4,
   enforced in `HexTile.SetIsPort`). So **a major port city cannot also host an airbase** — not
   expressible today. That is a real content constraint on exactly the kind of map this pass exists for
   (a coastal invasion usually wants an air base near the harbour). Either accept it as a design
   constraint, put the airbase on an adjacent hex, or revisit the exclusivity.

4. **✅ RULED (Bob): the gate must check for an ACTIVE FRIENDLY PORT UNIT, not the site flag.**
   ⚠ THE EXISTING GATE READS THE SITE FLAG — and will keep working while being wrong. `MovementController.IsOnPortHex` is `map.GetHexAt(unit.MapPos)?.IsPort`. After N2 the
   rule is "ADJACENT to a friendly OPERATIONAL port UNIT", which is a different question in three ways:
   adjacency vs on-hex, unit vs flag, and operational vs present. ⚠ It will not throw or fail a test —
   it will silently let units embark at a destroyed or enemy port on a flagged hex. Re-point it in N2 and
   pin it with a test.

5. **MOVE UNDO SURVIVES — ⚠ THE AGENT'S CONCERN WAS OVERSTATED, and Bob's approach is right.**
   Correction: most of what was listed as a threat happens at **UPKEEP, not during a move**. The sea
   supply tick and supply-driven efficiency degradation are Upkeep-phase, so they never sit inside a
   move at all; the over-water flag is set at end-of-move but is trivially reversible state; and
   beachhead supply is a property of a HEX that a move does not mutate. Undo is not in danger.

   **✅ THE RULE (Bob's design): enumerate what POISONS undo, and let the undo system follow that list.**
   Undo is offered only when a move changed **nothing but the moving unit's own position, facing,
   movement points and move action**. Anything below sets an undo barrier for that move:
   - **INFORMATION GAINED OR LOST** — any `SpottedLevel` change in either direction, on either side. You
     cannot un-know a contact, and offering undo after one is a fog-of-war exploit.
   - **DICE ROLLED WITH A CONSEQUENCE** — ground ambush, air-defence opportunity fire, the fixed-wing
     detection roll, air ambush, any combat. Undo-then-redo would be save-scumming a roll.
   - **THE WORLD CHANGED** — tile control flips (§6.13.2), objective capture, prestige credited, loss
     ledger entries, HCL changes.
   - **ANOTHER UNIT CHANGED** — an ambusher revealed, an enemy damaged, an airbase slot freed or filled,
     a transport loaded.
   - **A PHASE BOUNDARY WAS CROSSED** — anything Refresh or Upkeep has already consumed.

   ⚠ Implementation shape: a per-move **undo-barrier accumulator** that each of those sites sets with a
   REASON, rather than a single boolean — the reason is what lets the UI say *why* undo is unavailable
   instead of silently greying out. Empty at move end ⇒ undo offered. This also covers naval moves with
   no new rules: a quiet sea transit poisons nothing and is undoable.
   ⚠ Reconcile with DesignDoc §5.11 when it is written up; the existing "no new spotting events" gate is
   the first entry on this list, not a different rule.

6. **⚠ THE STORM RULES ARE DORMANT ON ARRIVAL.** Weather is single-state Clear in v1, so §5.13.4 air
   grounding and the new doubled sea-supply cost **can never fire**. Build them, but do not expect to
   validate them in play, and do not read their silence as a bug.

7. **⚠ PORTS' DEPLOYMENT-COST STORY — mostly resolved by the repurchase ruling.** §35.4.2 gives HQ and
   DEPOT a flat deployment cost of 2; airbases have a 300-prestige repurchase price instead. With ports
   now REPURCHASABLE (§J.2 authoritative), a port follows the AIRBASE shape: no deployment cost,
   priced via `PORT_REPURCHASE_COST` ("heavy" — likely 400+). Still owed: the §35.4 entry recording
   this, alongside the constant itself (N2/P4).

8. **⚠ AN AI UNIT AUTHORED NAVAL-EMBARKED WOULD BE STRANDED.** The AI does not sail (ruled), so a
   scenario that starts an AI regiment embarked gives it a unit it can never move or land. Either refuse
   it at OOB load with a clear message, or note it as an authoring rule in the editor relay (§I).

9. **⚠ RENDER LAYER FOR NAVAL.** `HexGridRenderer` has exactly two unit layers — `groundUnit` and
   `airUnit` — and stacking display assumes air-over-ground (dominant 1.0 / recessive 0.6). Naval will
   ride the ground layer, and the one legal stack becomes **fixed-wing over naval**, which is the same
   shape. Should just work; confirm rather than assume, and check the naval icon art exists (the P1 D1
   re-point created a naval icon BRANCH — verify it has a sprite behind it).

10. **⚠ THE NAVAL PROFILE MUST BE EXCLUDED FROM REQUISITION (P4).** `TRN_NAVAL` is the shared sealift
    profile — "never owned, never in a bay". Now that it gains a full stat line and prestige-shaped
    fields, make sure P4's shop cannot list or sell it.

11. **⚠ `RegionGraph` ALREADY TRACKS `HasPort`** (`RegionGraph.cs:233`, from the tile flag). Once ports
    are units, the AI's region analysis is reading the SITE and not the INSTALLATION — the same
    flag-vs-unit confusion as item 4. Cheap to fix, easy to miss.

## G. THE UNBUILT AIR LAYER (context, not this pass)

From the 2026-08-10 audit: `AirCombatEngine`, `AirStandCheck`, `HeloTransitStandCheck`,
`AOBMissionResolver`, `ReconMissionEngine` and `CombatResolver`'s airstrike / base-attack / AD-fire paths
are ALL implemented and EditorTest-covered with **ZERO live callers**. There is no AOB entity, no
placement input mode, no air phase in `BattleManager`, and no fixed-wing auto-return. Sortie supply is
never deducted. This is the M13 gap — C4 above is the one piece of it worth doing early, because it is
pure wiring of finished classes.
