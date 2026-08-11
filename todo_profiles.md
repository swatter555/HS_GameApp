# todo_profiles.md — THE PROFILE-SLOT REBUILD (RATIFIED)

> **STATUS: RATIFIED 2026-08-07 — all eight decision boxes answered by Bob (see §10). NOTHING IS
> IMPLEMENTED YET.** Holding on **P0 (Bob): run the pending M3 suite, re-export both khost `.oob`s,
> commit the tree.** P1 does not start until P0 is green. This doc is the synthesis of (a) Bob's
> rulings in the 2026-08-07 design conversation and (b) a five-sweep full-tree dive (state machine,
> data model, every consumer, persistence/content, design docs).
>
> ⚠ Two of Bob's answers were interpreted (flagged inline and in §10): #3 "Y" read as trucks AND
> tracked tractors; #7 read as a requisition-eligibility bar plus refuse-on-insufficient. Cheap to
> correct if misread — say so before P1.
>
> **The one-line version:** the three profile slots become what §3.2b already claims they are —
> Panzer-General-style purchasable equipment bays — by making bay CAPACITY a **derived fact**
> (from the deployed profile's `MovementMedium` + the unit's immutable identity) instead of an
> authored declaration, making naval lift a **transient state** instead of a possession, and
> building the buy/sell/upgrade rules the §18 shop will later drive.

---

## 1. THE RULE TREE (Bob, 2026-08-07 — the source rulings)

1. A regiment has exactly ONE immutable thing: its **identity** (`UnitClassification`). A tank is
   always a tank; an engineer with helicopters is still an engineer.
2. The **Deployed bay never changes in nature** — only *vertical* upgrades within the identity's
   line (T-55A→T-62A, BRDM-2→BRDM-2AT, S-75→S-300).
3. **Mobile and Embarked bays are purchases** — a foot unit may buy neither, either, or both.
   Selling back for prestige is allowed.
4. **A vertical upgrade may change the unit's MODE** (S-75 towed → S-300 self-contained). The bays
   revalidate; newly-illegal contents are force-sold at the normal refund rate.
5. **All infantry** may buy ground transport and helo lift. **Only AB/MAB and SPECF** may buy
   fixed-wing lift (either kind for them — "super air-mobile paratroopers"). Owning APCs/IFVs never
   blocks helo lift (all APCs/IFVs are air-mobile — the MT-LB air-assault shape generalizes).
6. **Airborne ground transport is BMD-line only.**
7. **Towed guns:** ground transport yes. Air lift **by equipment tag** (⚠ REVERSED 2026-08-08,
   supersedes the earlier blanket "no"): light towed tubes carry BOTH `HeloTransportable` and
   `AirDroppable` — proper airborne/air-assault force composition needs its artillery along.
   Heavy towed pieces stay untagged; the per-profile Y/N list lands in P1.
8. **Recon is a vehicle class like tanks** — deployed-only, vertical upgrades.
9. **Naval transport is universal and transient** — never owned, never authored, drawn from the
   shared naval profile while in the state. (Already ratified as DesignDoc §9.10.6, never coded.)

---

## 2. THE MODEL — four layers

| Layer | What it is | Where it lives | Mutability |
|---|---|---|---|
| **Identity** | `UnitClassification` | authored + persisted | **immutable, forever** |
| **Contents** | 3 × `WeaponType` (Deployed / Mobile / Embarked) | authored + persisted | Deployed: vertical upgrade only. Mobile/Embarked: buy / sell / upgrade |
| **Capacity** | which bays are open + what may fill them | **DERIVED — never authored, never persisted** | recomputed on demand |
| **Transient** | naval embarkation | one persisted bool on `CombatUnit` | set/cleared by the state machine |

**The derivation is the whole design:**

- **Mobile bay open ⟺ deployed profile's `MovementMedium == Foot`.** Rifle/engineer/Spetsnaz
  infantry, towed guns, S-75 crews: open. Tanks, SP guns, recon vehicles, S-300: their deployed
  profile is Tracked/Wheeled — the unit IS its vehicles — closed. *The S-300 ruling then requires
  no rule at all: author its deployed medium correctly and the bay closes by physics.*
- **Embarked bay capacity from identity, plus equipment-tag exceptions:** infantry-family ⇒ may
  buy Helo lift; AB/MAB/SPECF ⇒ Helo or FixedWing; everyone else ⇒ closed UNLESS the deployed
  kit carries `HeloTransportable`/`AirDroppable` (light towed tubes carry both — ratified
  2026-08-08). Facilities/air/HELO: no bays at all (§8.5.6 unchanged).
- **Naval is not capacity** — any ground unit at a friendly port may enter the naval state (§5).

Why derived wins, in one sentence per sweep finding: the authored declarations are *already
fiction* — `RegimentProfileType` and `isMountable` have **zero** behavioral readers, `isEmbarkable`
has two (both redundant with an adjacent null-check), and 36 of 169 templates carry an
`isEmbarkable` that contradicts their own profile type.

---

## 3. WHAT DIES, WHAT LIVES

### Dies (with evidence it's safe)

| Thing | Readers today | Notes |
|---|---|---|
| `RegimentProfileType` (enum, `RegimentProfile.ProfileType`, `.oob` `IntelProfileType`, save `profileType`) | **0 behavioral** | 169 DB authoring sites + loader/mapper/tests = compile churn only |
| `CombatUnit.IsMountable` | **0** | dead flag |
| `CombatUnit.IsEmbarkable` | 2, both redundant | `CombatUnit.cs:1273`, `:1354` — each sits beside `GetEmbarkedProfile() != null` |
| `EmbarkmentState` enum | 3, **all permanently dead** (never written in gameplay) | replaced by one bool + derivation, see §5 |
| `SpecialEmbarkmentChecks`' classification cases | — | AB/MAB, SPECF+`TRN_AN8_SV` literal, MAR/MMAR, AM/MAM+`UpgradePath.HELT` — ALL replaced by gates on bay contents (§6) |
| `TRN_NAVAL` in any Embarked slot | 2 templates (`USSR_NAV_BTR70/80`) | contradicts ratified §9.10.6; becomes the shared naval-state profile |
| `DEP_EMB_HELO`/`DEP_EMB_AIR` (added 2026-08-04) | — | die with the enum; the M3 *behavior* (skip Mobile when no ground transport) survives — it never read the enum anyway |

### Lives and becomes load-bearing

- `MovementMedium` (per profile, M1) — **the capacity input.** Its `None`-means-undeclared discipline
  escalates from "silent audio" to "wrong bay set": the coverage test becomes a hard guard.
- `TransportCategory` — the bay-content invariant input (`!= None` ⇒ Embarked bay only) and the
  fixed-wing-vs-helo distinction for embark gates. ⚠ Naval derivation must key on
  `MovementMedium.Naval`, never `TransportCategory` (`TRN_NAVAL` deliberately has `None`).
- `UpgradePath` + `TurnAvailable` + `PrestigeCost` — **the shop table, already authored on 144/177
  profiles.** A vertical upgrade list = same `UpgradePath` + same nationality + `turnAvailable ≤
  current turn`. No new data structure needed.
- The three slot accessors + `GetActiveWeaponProfile()` — unchanged shape, one naval branch added.
- `MovementModeService` — gains `IsNavalEmbarked` awareness; stays pure/headless.

---

## 4. THE NEW RULES

### 4.1 Capacity (derived, one static home — extend `MovementModeService` or new `BayRules`)

```
MobileBayOpen(unit)      = DeployedMedium(unit) == Foot
                           && unit is not Facility/fixed-wing/HELO class
EmbarkedKinds(unit)      = AB|MAB|SPECF        -> { Helo, FixedWing }
                           infantry-family     -> { Helo }
                           anything else       -> { }        // towed guns land here
                           + FixedWing when the DEPLOYED profile carries
                             WeaponCapability.AirDroppable       ✅ RATIFIED (box 9, Option A)
                           + Helo when the DEPLOYED profile carries
                             WeaponCapability.HeloTransportable  ✅ RATIFIED (Bob, 2026-08-08 —
                             replaces the MANPAD name-prefix check; same shape as AirDroppable)
```

⚠ **The two capability tags are EXCEPTION mechanisms only — never tag what identity already
covers.** Plain infantry is NEVER tagged `HeloTransportable` (its helo lift flows from identity;
a duplicate per-profile tag = two sources of truth, and a forgotten tag on a future profile
silently closes the bay). And the tags are **doctrine grants, not physics**: light towed tubes
carry BOTH tags (D-30s sling-load and air-drop — ratified 2026-08-08, reversing the earlier
blanket "towed guns: no air lift"), while HEAVY towed pieces stay untagged as a deliberate
doctrine statement, not an oversight. Accepted consequence: regular (non-VDV) light-artillery
regiments may buy helo or fixed-wing lift too. Naming note: `TransportCategory.HeloTransport`
marks the CARRIER (Mi-8); `HeloTransportable` marks the CARGO.

```
NavalEligible(unit)      = ground unit (not facility, not air/HELO class)
```

**The rules' home (✅ RATIFIED 2026-08-08 — the `EquipmentBays` split):** `RegimentProfile` is
RENAMED **`EquipmentBays`** in P1 (it is a container of three profiles, not a profile; `.oob`
field names `DeployedProfileID`/`MobileProfileID`/`EmbarkedProfileID` stay STABLE so the editor
contract does not churn for rhetoric). It owns the DOCTRINE half, statelessly: slot storage, the
§4.3 physical invariants, `CanAccept(identity, bay, candidate)` (capacity + compatibility, with
identity passed IN — the container holds no unit state), the mutation API
`TrySetSlot`/`TryClearSlot` (re-validates invariants — a buggy caller cannot assemble an illegal
loadout), and the intel-stats rebuild on every change. It never knows about prestige, turns, or
windows — that is `RequisitionService` (§4.7), which ASKS EquipmentBays for admissibility rather
than re-implementing any bay rule. ⚠ NOT a `*Manager` and not a one-stop shop: `*Manager` in this
codebase means MonoBehaviour singleton, and the deciding state for "now" questions (posture,
supply, wallet, phase) lives on `CombatUnit`/`BattleManager`/the controller — a container that
ingested it would be a shadow CombatUnit. Sounds keep their existing one-stop answer:
`GameAudio` → `MovementModeService.CurrentMedium` → the active slot.

Infantry-family = the leg-carried identities: INF, MOT, MECH, AB, MAB, MAR, MMAR, AM, MAM, SPECF,
ENG, AT-if-foot. **✅ RATIFIED (Bob, box 4): CAV is tank-like** — a vehicle class: deployed-only,
vertical upgrades, no bays — **and MANPAD-armed units are infantry-like** (foot deployed medium ⇒
mobile bay opens by derivation; helo lift eligible despite their SAM/AAA-side classification).
Implementation note for P1: MANPAD units are identified by their deployed profile carrying
`WeaponCapability.HeloTransportable` (ratified 2026-08-08, superseding the earlier MANPAD_
name-prefix idea) — the helo-lift rule gains that one capability-keyed inclusion.

### 4.2 Compatibility (what may FILL an open bay)

```
Mobile bay:   ground transports of own nationality whose Medium is Wheeled|Tracked
              AB/MAB           -> BMD line only                       ✅ RATIFIED (box 2)
              towed guns       -> trucks + tracked tractors (MT-LB)   ✅ RATIFIED (box 3 ⚠ see note)
              other infantry   -> APC / IFV / truck, BMDs EXCLUDED    ✅ RATIFIED (box 2: BMDs are
                                                                         airborne-exclusive)
Embarked bay: transport profiles (TransportCategory != None) of own nationality,
              kind ∈ EmbarkedKinds(unit)
```

⚠ Box 3 was an either/or ("trucks only, or also MT-LB?") answered "Y" — booked as the INCLUSIVE
reading (trucks AND tracked tractors). If "trucks only" was meant, say so before P1; one line changes.

### 4.3 Hard invariants (EditorTest over every template + init-time warning; extends the M3 guard)

1. A profile with `TransportCategory != None` occupies ONLY the Embarked bay (existing, kept).
2. Mobile bay content, if any, has `Medium ∈ {Wheeled, Tracked}`.
3. Embarked bay content, if any, has `TransportCategory != None` (helo/FW lift only — naval never
   sits in a slot).
4. Every populated slot declares a medium (existing coverage test, message strengthened).
5. A populated Mobile bay on a unit whose deployed medium ≠ Foot is a defect (the S-300 class of
   error becomes machine-checkable).

### 4.4 Embark gates — `SpecialEmbarkmentChecks` rewritten, zero classification cases

The gate keys on **what is in the bay being boarded**, not who is boarding:

```
target == Embarked (organic):
    GetEmbarkedProfile() == null            -> refuse
    content is FixedWingTransport           -> require adjacency to ACTIVE friendly airbase
    content is HeloTransport                -> no positional gate
target == Embarked (naval path, §5):        -> require friendly port hex
```

The AB/MAB airbase rule survives *as a consequence* (their lift is an An-12). The MAR/MMAR port
rule survives as the universal naval-port rule. The `TRN_AN8_SV` literal and the `UpgradePath.HELT`
check die; invariant 3 makes them unnecessary.

### 4.5 Naval — the transient state (implements ratified §5.4.2 / §9.10.6)

- New persisted `CombatUnit.IsNavalEmbarked` (bool, default false) replaces `EmbarkmentState`.
- Enter: at Deployed on a friendly port hex, deploy-up targets Embarked via the naval path
  (skips Mobile, §9.4.7), sets the bool. Exit: deploy-down → Deployed, clears it (§9.5.2);
  Marines additionally may debark onto a beachhead hex (their ONLY naval privilege, §9.10.6.1).
- While set: `GetActiveWeaponProfile()` at Embarked resolves to the **shared `TRN_NAVAL` profile**
  (movement + combat stats drawn, never owned); `MovementModeService.CurrentMedium` reports
  `Naval`; the icon shows the naval-embark art (finally reachable — today that branch is dead).
- A unit that OWNS air lift can still use naval (bool wins while set; the two are mutually
  exclusive states of the same Embarked position).
- Naval COMBAT stays deferred (§5.4.2.6) — this pass only makes the state real.

### 4.6 Vertical upgrades + the cascade

- Upgrade list for any bay = profiles sharing the content's `UpgradePath` (Deployed bay: the
  deployed profile's path; identity never changes), own nationality, `turnAvailable ≤ current`.
- **Cascade:** after a Deployed upgrade, recompute capacity; force-sell newly-illegal bay contents
  at the normal refund rate. (S-75+trucks → S-300 ⇒ trucks auto-sold, prestige credited.)
- Price of an upgrade / purchase — **✅ RATIFIED (box 5):** PG-style
  `cost = new.PrestigeCost − old.PrestigeCost` floored at a minimum fee for upgrades within a bay,
  full `PrestigeCost` for a first purchase into an empty bay, and **sell refund = 75 % of
  PrestigeCost** (rounded down). The cascade's force-sale refunds at the same 75 %.
- ⚠ §7A.22 double-pricing: TRN is both a standalone purchasable unit (150) and potential bay
  content — the same lift must not be priced twice; buying the BAY content is the §18.3 price,
  the standalone TRN unit remains a separate product.

### 4.7 Requisition API (headless now, §18 shop UI later)

New static, headless `RequisitionService` (`Services/`, zero UnityEngine, zero singletons):

```
GetDeployedUpgradeOptions(unit)         GetBayPurchaseOptions(unit, bay)
TryPurchase(unit, bay, type, wallet)    TrySell(unit, bay, wallet)
TryUpgrade(unit, bay, type, wallet)     // Deployed bay upgrades run the cascade
```

- `wallet` = the §18 prestige pool. Minimal wiring now: make `BattleManager.CurrentPrestige` a real
  balance (`AddPrestige`/`SpendPrestige` actually move it) so the API has something to debit;
  the requisition PANEL stays future work.
- **⚠ THE CONTROLLER IS THE ARBITER — the deploy up/down pattern, verbatim.** The model/service
  layer owns every RULE (capacity, compatibility, pricing, supply/posture/prestige eligibility);
  a CONTROLLER gates phase + side, supplies any map context the model cannot see, invokes the
  service, and on success raises the events (`RaiseRedrawMapIcons` etc.). Nothing under `Models/`
  or in `RequisitionService` ever raises an event or touches a singleton — the
  `EventManager.Instance` lazy-creation trap applies here exactly as it did to `TryDeploy*`.
  The future Requisition Window drives its controller; the controller drives the service.
- Every successful mutation: rebuild intel stats (`BuildIntelStats`), re-run the bay invariants,
  raise redraw (controller-side, never model-side — the no-events-under-Models rule holds).
- **✅ RATIFIED (box 6) — transaction window:** own turn, unit at Deployed-or-lower posture, not
  adjacent to an enemy ZoC source. Bob: "Different than PG, but yes." **These bay transactions roll
  into the planned Requisition Window** (§18.9 — the same screen that will sell whole new units);
  the API below is what that window will drive.
- **✅ RATIFIED (box 7) — transaction eligibility:** the unit must hold **≥ 5 days supply**, and the
  transaction is **refused** (never partially applied) for insufficient prestige OR insufficient
  supply. ⚠ Interpretation flag: box 7's question was the D7 deployment-transition supply rule;
  Bob's answer ("5 days supply req, refuse for lack of sufficient prestige or supply") reads as a
  REQUISITION eligibility bar, booked here, PLUS refuse-on-insufficient for D7 itself (§5 table).
  Correct before P1 if misread.

### 4.8 Movement rules read the resolver (absorbs old M4 + the HexMapUtil pair)

- `MovementController.ExecuteMovement` (:803–931) AND `HexMapUtil.GetValidMoveDestinations` (:321)
  AND `HexMapUtil.FindPath` (:453) — all three replace `IsAirUnit || IsHelicopter` with
  `MovementModeService.IsAirborneNow`. (Fixing execution but not pathfinding would make the range
  overlay lie; they go together.)
- Ambush-against-a-flight: as ratified 2026-08-04 — detection runs, halt = MP 0 + move actions 0
  and NOTHING else, no combat event, ambusher revealed, ZoC never stops a flight, printer dispatch
  + `UnitMoveBlocked`. Vocabulary: stopped/halted, never "aborted".
- Animation pacing + movement audio duration key on the medium.

---

## 5. DEFECTS THE DIVE FOUND — absorbed into this pass

| # | Defect | Fix home |
|---|---|---|
| D1 | `EmbarkmentState` never written in gameplay ⇒ §12.3 airborne-spotting arm, `CombatResolver.IsHeloAirDefenseTarget` arm, and the naval icon branch are permanently dead | retired enum; re-point all three at derived questions (`CurrentMedium == Helo` / `IsNavalEmbarked`) |
| D2 | No ceiling clamp on deploy-up — at Embarked, `+1` writes undefined enum value 6, charges costs | clamp in `TryDeployUP` |
| D3 | Deployed→Mobile not gated on a mobile profile existing — DEP-only unit "mounts" nothing, pays costs | gate = `MobileBayOpen && GetMobileProfile() != null` |
| D4 | Two MP-cost formulas (model raw `0.5f*Max` vs UI `CeilToInt`) — button greys out when the model would allow | one shared formula |
| D5 | OOB load skews MP: `Deployment` set without re-running `UpdateMovementPointsForProfile` — a unit authored at Mobile/Embarked starts on the foot ceiling | loader calls the update after positioning |
| D6 | `ApplySnapshot` never re-runs `InitializeRegimentProfile` ⇒ loaded units have EMPTY `TotalIntelStats` (intel report + loss ledger break on load) | post-load rehydrate step in `SnapshotMapper` |
| D7 | `ConsumeSupplies` return ignored — transition completes having charged no supply | **✅ RATIFIED: REFUSE** — a deployment transition that cannot pay its supply cost does not happen (matches the critical-supply gate's spirit). See also §4.7: requisition transactions carry their own ≥5-days-supply bar |
| D8 | Generalized Spetsnaz path lost its airbase gate (FW-lift SPECF can embark anywhere) | §4.4 gates fix this structurally |
| D9 | Three disagreeing "is fixed-wing" classification lists (`GameData:1781` incl. WW+TRN vs `CombatUnit:71` vs `GameIconRenderer:674`) | consolidate to one helper in `GameData`, in-pass |
| D10 | `RaiseUnitDeploymentChanged` never raised; icon deploy-badge refreshes only via coarse redraw | **✅ RATIFIED: DELETE** the event + its subscriber path (coarse redraw is the ratified mechanism) |

---

## 6. PERSISTENCE, CONTENT, EDITOR CONTRACT

- **Save:** drop `profileType`, `isMountable`, `isEmbarkable`, `currentEmbarkmentState`; add
  `isNavalEmbarked`. `SAVE_VERSION` 4 → **5**, no migration step (pre-1.0 clean break; also ends
  the amend-v4-in-place allowance noted at the constant).
- **`.oob` format:** drop `IntelProfileType`, `IsMountable`, `IsEmbarkable`. Keep
  `DeployedProfileID`/`MobileProfileID`/`EmbarkedProfileID` + `Deployment`. Old keys in shipped
  files are ignored (verify unmapped-member handling stays permissive — one test).
- **`CombatUnitDB`:** constructor loses 3 params — **replace call sites wholesale, never edit
  in place** (OOBFileLoader passes adjacent positional bools; a signature edit could silently
  re-bind them). 169 templates re-authored mechanically; the 2 naval-infantry templates drop
  `TRN_NAVAL` from Embarked; S-300 per decision box.
- **Scenario editor (Bob relays):** `Transition.md` §3.1/§3.5 rewritten; the editor stops
  emitting the three fields (its exports stay loadable meanwhile — unknown keys are ignored);
  note this supersedes the never-relayed `DEP_EMB_*` addition.
- **khost `.oob` state (UPDATED 2026-08-08 — the "byte-identical pair" claim is obsolete):**
  the STANDALONE copy is now the editor's 58-unit NEW-FORMAT export (md5 `219D4F1F…`, no
  `IntelProfileType`/`IsMountable`, fresh template-sourced Spetsnaz included) — **play-tested in
  game by Bob, which also proves the new format loads.** The CAMPAIGN copy is a stale pre-07-28
  export (md5 `3A3F401D…`, hand-patched) — unreachable in-game until content-pipeline Phase 2,
  left untouched, regenerated from the canonical (standalone) roster when Phase 2 lands.
- **Editor coordination (their Reply 4 + our response, 2026-08-08):** the editor already dropped
  `IntelProfileType`/`IsMountable` from its writer and holds `IsEmbarkable` until we signal the
  P1 re-key (see P1). Their validator runs the §4.3 bay invariants with an effective-medium
  resolution rule we supplied (explicit override else archetype family default). Relay protocol
  re-committed: every enum/format change relayed same session; P1 gets a format brief BEFORE landing.

## 7. DESIGN-DOC AMENDMENTS OWED (drafted at ratification, applied per-phase)

§10.7 rewritten around bays + derived capacity (absorbs Claude_Project §3.2b, which currently has
no doc home) · §10.7.3 table retired · §9.3.2/9.3.3/9.4.3/9.4.4 gates re-keyed on contents ·
§9.10.6/§5.4.2 restated as the transient state · §9.10.5 "carries three profiles" → purchased
end-state, not birthright · §9.10.3 "tanks never Mobile" → derived consequence, not class law ·
§10.3.5/§10.3.13 flag/validation language replaced by the invariants · §18.3/§18.5 extended with
bay purchase/sale/upgrade pricing · §35.2.1 UI action list + §35.4.1 deployment-cost note ·
fix the dangling Appendix W citation (§34.5) while in the file.

## 8. TEST PLAN

- **Acceptance pins, must pass UNCHANGED:** `DeploymentTransitionTests` (skip-to-Embarked, through-
  Mobile, down-to-Deployed, MP scaling), `MovementMediumTests` (coverage, no-transport-in-Mobile,
  VAB/MT-LB/BTR rulings), the 7 audio posture tests, `EmbarkedInfantry_IsAirborneNow…`.
- **New suites:** capacity-derivation table over all 169 templates; the five §4.3 invariants;
  naval enter/exit path incl. Marines' beachhead debark; embark gates (FW-needs-airbase,
  helo-anywhere) with zero classification mentions; cascade force-sale; purchase/sell/upgrade
  arithmetic + wallet; D2/D3/D4/D5/D6 regression pins; movement: airborne pays cost 1, ignores
  ZoC, ambush-halts without combat (range AND path AND execute agree).

## 9. PHASING

- **P0 (Bob, first):** run the pending M3 suite; re-export both khost `.oob`s; commit the tree.
  This pass builds on M3 and must start from a green, committed base.
- **P1 — Derive + delete.** Capacity/compatibility rules + invariants land; enum, two flags,
  `EmbarkmentState` deleted; constructor/loader/mapper/DB/tests churn; SAVE_VERSION 5.
  Zero intended behavior change (the pins prove it). Additionally in P1:
  - **`SAM_S300_SV` medium `Foot` → self-propelled** (`WeaponProfileDB.cs:1790` — editor's catch;
    as authored it would OPEN the S-300's mobile bay, the inverse of box 1), delivered with the
    SAM/AAA self-contained candidate list for Bob's Y/N.
  - **Signal the scenario editor** the moment `CombatUnit.cs:1273`/`:1354` are re-keyed onto bay
    contents — their writer drops `IsEmbarkable` only on that explicit signal (their Reply 4 §1;
    until the re-key, a field-less `.oob` would silently brick every embark path).
  - **Rename `RegimentProfile` → `EquipmentBays`** (§4.1 ratified split) — class + save JSON key;
    `.oob` field names untouched. Renames are free exactly now (SAVE_VERSION already bumping,
    pre-1.0 clean break) and never again.
  - **`WeaponCapability.AirDroppable`** added; tagged on `ART_LIGHT_SV` + `RCN_BRDM2AT_SV`
    (box 9 Option A).
  - **`WeaponCapability.HeloTransportable`** added; tagged on the MANPAD deployed profiles and
    the light towed tubes — exception tag only, plain infantry rides identity.
    Keep the `WeaponTrait_Supplement.md` catalog in sync with both new capabilities.
  - **Towed-tube tag census for Bob's Y/N** (same pattern as the SAM/AAA self-contained list):
    every towed ART/mortar profile proposed as HeloTransportable and/or AirDroppable or neither —
    agent proposes light guns + mortars BOTH, heavy pieces NEITHER; whether any towed AAA/SAM
    (ZU-23 is genuinely helo-portable) joins the list is Bob's call on the same census.
- **P2 — State machine.** Naval transient state + rewritten embark gates + D2–D7.
  First gameplay changes: naval sealift exists; FW-lift airbase gate restored.
- **P3 — Movement.** §4.8 (old M4 + the HexMapUtil pair + ambush-vs-flight).
- **P4 — Requisition.** `RequisitionService` + cascade + wallet arithmetic, EditorTest-driven;
  no UI beyond making the stub button report honestly.
- **P5 — Content + docs + relay.** Template re-author, re-exports, design-doc amendments,
  `Transition.md`, editor relay note.
  Full suite after every phase (weapon-profile suites pin the archetype layer).

## 10. ✅ DECISION BOXES — ANSWERED BY BOB, 2026-08-07 (verbatim answers booked into §4/§5 above)

1. **S-300 & friends — Y.** Self-contained = author deployed medium Wheeled/Tracked; bay closes by
   derivation. Agent brings the candidate list (S-300 + any SAM/AAA whose real system is
   self-propelled) for a Y/N pass during P1.
2. **BMD exclusivity — Y.** Only airborne may buy BMDs; excluded from every other purchase list.
3. **Towed-gun transports — "Y"**, booked as trucks AND tracked tractors (MT-LB). ⚠ Inclusive
   reading of an either/or answer — flag to confirm before P1.
4. **Family membership — "CAV is tank-like, MANPADS like infantry."** CAV: vehicle class, no bays.
   MANPAD-armed units: infantry-like (mobile bay by medium, helo lift eligible).
5. **Pricing — sell refund 75 %** (not the proposed 50). Difference-priced upgrades stand.
6. **Transaction window — Yes** (own turn, Deployed-or-lower, ZoC-adjacency bar). Bay transactions
   roll into the planned Requisition Window alongside whole-unit purchases.
7. **Supply rule — "5 days supply req, refuse for lack of sufficient prestige or supply."** Booked:
   requisition eligibility bar ≥5 days supply + refuse-on-insufficient; D7 transition rule = REFUSE.
   ⚠ Interpretation flagged in §4.7.
8. **D10 — Delete** the `RaiseUnitDeploymentChanged` scaffolding.

9. **✅ RATIFIED (Bob, 2026-08-08) — VDV lift expressibility, OPTION A.** The eligibility fact
   lives on the EQUIPMENT: new `WeaponCapability.AirDroppable`, tagged on the two shared deployed
   profiles `ART_LIGHT_SV` and `RCN_BRDM2AT_SV` (two lines via the existing trait/capability
   machinery). Rule: *may buy fixed-wing lift ⟺ airborne identity (AB/MAB/SPECF) OR deployed kit
   is AirDroppable.* Accepted consequence: the regular light-artillery and BRDM-2AT recon
   regiments become An-12-eligible too (historically defensible — the kit genuinely flies).
   ⚠ Reclassification was rejected for a hard reason: classification drives combat ROUTING
   (ART fires §7.13 indirect) — VDV Artillery reclassified would stop being artillery in combat.
   b) **Load policy: GRANDFATHER** — capacity rules govern the SHOP, never the loader; §4.3
      invariants police physicality only. Cascade force-sells on upgrade, never at load.
   c) **`USSR_VDV_SUP` keeps its TANK classification** — expressible under (a), no redefinition
      needed unless Bob later wants one for flavor.

## 11. HOLD STATE (updated 2026-08-08)

**P0 progress:**
- **(b) OOB check — ✅ DONE, and better than asked.** The load→save round-trip could never test the
  templates (the editor reads every field off the file); instead the editor ran the REAL check —
  a fresh Spetsnaz placed from the re-imported 169-template DB, field-for-field identical to the
  hand-patch. Template pass confirmed correct. Bob shipped the 58-unit new-format standalone to
  StreamingAssets and **play-tested it in game** — first no-`IntelProfileType`/no-`IsMountable`
  file ever loaded, works. Canonical roster = the STANDALONE (campaign copy stale + unreachable
  until Phase 2, untouched by agreement with the editor).
- **(a) SUITE RUN STILL OWED** — the in-game test is not the Test Runner. Please run Unity Test
  Runner: `DeploymentTransitionTests`, `MovementMediumTests`, `AudioSystemTests` + the four
  weapon-profile suites.
- **(d) COMMIT STILL OWED** — everything after `d6abfcb` remains uncommitted.
- **NEW gate into P1: decision box 9** (VDV expressibility/load policy) — P1's capacity rules
  cannot land with two shipped templates in an undefined state.

**P0 CLOSED (2026-08-08):** suite green (452) · OOB check done · box 9 ruled · committed `36175e2`.

## 12. P1 — ✅ CLOSED: SUITE GREEN 2026-08-08 (Bob ran the full Test Runner), committed

**The editor's `IsEmbarkable` flip is AUTHORIZED** — green confirmation delivered to their
Markdowns per the Reply-4 contract.

## 13. P2 — ✅ CLOSED: SUITE GREEN 2026-08-08 (Bob, full Test Runner), committed with census A

**Census A implemented first** (§12 table): all towed SAM/AAA + light tubes tagged BOTH
(11 profiles), heavies/SP untagged, pinned in `EquipmentBaysTests`.

**Then the state machine:**
- **Naval embark (§9.4.7, universal):** at Deployed with no ground transport, no owned lift, on a
  friendly port → sealift (skips Mobile); from Mobile, +1 with no owned lift at a port → sealift.
  **Organic lift always wins over naval.** `IsNavalEmbarked` is written BEFORE costs so the MP
  rescale lands on `TRN_NAVAL`'s ceiling — after, and units would board ships on helicopter MP.
- **Naval debark (§9.5.2/§9.10.6.1):** friendly port for everyone; beachhead for MAR/MMAR only
  (deliberate identity doctrine, not the deleted rot — the privilege attaches to marines, not
  equipment). State clears on debark; lands Deployed. Controller supplies the third map fact
  (`IsOnBeachheadHex`).
- **`SpecialEmbarkmentChecks` → `EmbarkmentChecks`, ZERO classification cases:** FW lift needs an
  active friendly airbase (AB/MAB keep their rule as a consequence of the An-12 — and D8 closes:
  FW-lifted SPECF is finally gated too); helo lift boards anywhere; naval needs the port. The
  AB/MAB, SPECF+`TRN_AN8_SV`, MAR/MMAR, and AM/MAM+`UpgradePath.HELT` cases are all deleted.
- **Defects:** D2 ceiling clamp (no more enum value 6) · D3 mounting-requires-transport ·
  D4 one deploy-cost formula (raw fraction; the CeilToInt HUD/model split is gone) ·
  D5 `RefreshMovementPointsForPosture` + loader call (units authored at Mobile/Embarked start on
  the right ceiling; loader-only — the mapper restores saved MP and must not refill) ·
  D6 `ApplySnapshot` rebuilds intel stats (loaded saves no longer have empty rosters) ·
  D7 supply refuse (unreachable by the 0.5 critical gate; drift now logs).
- **Tests:** 8 new cases in `DeploymentTransitionTests` (§P2 region): sealift on/off port, D3
  refusal charges nothing, mounted-marines through Mobile, organic-beats-naval, FW airbase gate +
  helo-anywhere, port-vs-beachhead debark by identity, D2 refusal, naval MP rescale.
  `DeploymentActionTests` verified compatible (its up-tests only exercise the dug-in collapse).

Deferred to P3 by design: naval TRAVERSAL (water-hex movement rules) rides the same medium-keyed
pass as the airborne fix; the §21.8 instant port-to-port sealift mechanic needs a destination-pick
input mode (M13-adjacent) and is not in P2/P3.

## 14. P3 — ✅ CODE COMPLETE 2026-08-10, ⚑ SUITE RUN OWED (Bob)

**All three sites re-keyed on `MovementModeService`, together:** `GetValidMoveDestinations`, `FindPath`,
`ExecuteMovement` (step cost, road bonus, ZoC, ambush branch, and `isFixedWing` → medium for tween
pacing, movement-audio duration, and the §6.13.2 transit flip). Ambush-against-a-flight built as
ratified. New `MovementTests` §P3 region (8 cases) + `SpottingService.RevealAmbusherToContact` +
`PrinterDispatch.ReportFlightHalted` + `ApplyMovementHalt` enum-keyed and `internal` for testing.

### 🔴 THE NAVAL QUESTION DISSOLVED — the design doc already answers it, and the P3 note was wrong
The note below asked Bob two things before coding naval traversal. **Neither needed him:**
- *"May ground units enter Water hexes at all today (Water cost 1 suggests yes, a latent bug)?"* —
  **NO, and there is no latent bug.** `HexMapUtil.ComputeStepCost` returns −1 for `TerrainType.Water`
  on the ground branch, explicitly. The cost-1 table entry is never consulted for a walking unit.
- *"Does naval movement use the ambush-halt rule or nothing?"* — **MOOT. THERE IS NO NAVAL TRAVERSAL
  TO RULE ON.** §5.4.2.3: "Movement is instant (no per-hex traversal at this scale; naval intermediate
  hexes abstracted)"; §5.4.2.6 defers everything finer than port-to-port; §24.7a.3 picks the
  destination with a Naval Movement Marker. ⚠ **So "while `IsNavalEmbarked` the unit moves on WATER
  hexes" (P3's own framing, below) CONTRADICTS the ratified §5.4.2 and would have built the wrong
  mechanic** — hex-by-hex sea movement the doc explicitly abstracts away.

**What P3 built instead — a PROHIBITION, which turned out to be a live hole.** `Naval` is neither
airborne nor groundborne, so a sealifted unit fell through to the GROUND rules — which block water but
allow LAND. A regiment that boarded ships at a port could WALK INLAND still aboard them. P2 made that
reachable (universal port embark) on any map with a port; Khost has none, which is the only reason it
was not already visible. `MovementModeService.IsSealiftedNow` + an early return in both HexMapUtil
passes closes it. The §21.8 instant sealift stays where it was: needs the destination-pick input mode,
M13-adjacent, not P3.

### ⚑ FOR BOB
1. **Please run Unity Test Runner** — `MovementTests` (rewritten helpers + 8 new cases),
   `MovementMediumTests`, `DeploymentTransitionTests`, `AudioSystemTests`, `SpottingServiceTests`,
   `IntelLadderTests`, `TerritoryServiceTests`. ⚠ `MovementTests`' `CreateGroundUnit`/`CreateAirUnit`
   now build units WITH real weapon profiles — they had none, so under medium-keying the fixture
   fighter would have been treated as infantry. That change is why the whole suite should run.
2. **Play-test:** air assault over mountains, past enemy units, and into an ambush (the M4 ask).
3. **Two judgement calls flagged, neither blocking** — see §14a below.

## 14a. P3 — BOTH FLAGGED CALLS RULED BY BOB, 2026-08-10 (suite was green first)

**THE GOVERNING DISTINCTION, in Bob's words:** helicopters "remain on the map, whereas fixed wing
assets only ever traverse the map attempting to get to the air ops box." Everything below falls out
of that one sentence, and new code should be written against it rather than against the symptoms.

| | Helo / helo-borne | Fixed-wing |
|---|---|---|
| What it is | "a special type of GROUND unit" | a transient crossing the board |
| Stopped by ground ambush? | **YES** — ambush triggers, combat does not | **NO** — nothing on the ground can touch it |
| Spots ground units in transit? | **YES**, normally | **NO** — does not look down at all |
| May share a ground unit's hex? | **NO** | **YES**, temporarily |
| How the enemy engages it | ground ambush + air defence | **air defence only** — an unspotted AD unit fires and thereby reveals ITSELF |

**(a) FIXED-WING AMBUSH — RULED OUT, now implemented.** P3 had applied the flight-evasion halt to
ALL airborne movers because the ratified text said "exactly as for a ground unit"; the agent flagged
the MiG-21-turned-back-by-infantry case as suspect. Bob: helo yes, fixed-wing no. The ambush block is
now gated `!isFixedWing`, and the air-ambush path below it is the ONLY thing that touches a jet.

**(b) "WHERE MAY IT STOP" STAYS ON CLASSIFICATION — RULED CORRECT.** Bob: "FW units CAN temporarily
occupy a ground unit's hex, Helos cannot." That is exactly what the `IsAirUnit` stop-test delivers,
since everything that is not fixed-wing files in the ground stack. Traversal keys on the medium, rest
keys on the layer the unit occupies — the M5 "occupancy is legitimately classification" verdict,
confirmed at the one place P3 forced the question.

**(c) NEW — §12.3.7a, fixed-wing does not spot the ground.** Implemented in `SpottingRangeAgainst`
(the single §12.3.10 chokepoint, so sweep + transit + decay + AI mirror inherit it together), keyed
on the MEDIUM so a paratroop regiment inside an An-12 is covered too.
⚠ **RECONA AND AWACS EXEMPTED — the agent's judgement call, flag it if wrong.** Both have a ratified
8-hex ground reach that other systems are built on (§11.11.3 derives the recon mission's whole search
area from RECONA's range; §12.3.9 calls exploiting the AWACS look-down near the front a deliberate
player risk). Zeroing them would silently delete air reconnaissance, which the ruling plainly was not
about. ⚠ **DESIGN-DOC AMENDMENT OWED: §12.3.7 "FGT / ATT / BMB / WW / TRN: 2 / 4" → "0 / 4"**, with a
new §12.3.7a stating the transit rule and the two exemptions.

## 14c. ⚓ THE NAVAL PROBLEM — full statement (written for Bob, 2026-08-10)

### The one-sentence version
**A unit can get onto the boat and off the boat, but there is no way to make the boat go anywhere** —
so naval movement today is an elaborate no-op that charges a deployment action to end up exactly where
it started.

### What the design says should happen (§5.4.2, normative; §21.8 defers to it)
1. Unit stands on a friendly **PORT** hex.
2. It deploys up to Embarked, drawing the shared generic sealift profile (§9.4.7 / §9.10.6).
3. The player places a **Naval Movement Marker** (§24.7a.3) on a destination — another friendly **port**
   for any ground unit, or a coastal/**beachhead** hex for Marines only (§9.10.6.1).
4. **Resolution is INSTANT** (§5.4.2.3): no sea hexes are crossed, the passage is abstracted away.
5. It **consumes the entire turn** (§5.4.2.4): MoveAction and MP to zero, no combat or intel.
6. The unit arrives **Deployed** on the destination hex (§5.4.2.5).

### What is actually built
| Step | State |
|---|---|
| 1–2 embark at a friendly port | ✅ **P2** — universal port rule, organic lift wins over naval, `IsNavalEmbarked` + shared `TRN_NAVAL` |
| 6 debark → Deployed (port for all, beachhead for MAR/MMAR) | ✅ **P2** — identity doctrine, state clears on debark |
| — the unit cannot walk while aboard | ✅ **P3** — `IsSealiftedNow` prohibition |
| 3 destination picking | ❌ **nothing** |
| 4 instant resolution | ❌ **nothing** |
| 5 turn consumption | ❌ **nothing** |

So the state machine is done at both ends and the middle is missing.

### Why it is not simply "add movement"
Because it is **not a movement** — it is a teleport with a target picker, and it needs three things the
project does not have yet:

1. **AN INPUT MODE.** Every battle-map input today is select / right-click-move / Ctrl-click-attack.
   There is no "now click a destination" mode. §24 plans an input-mode state machine (Normal /
   CtrlCombat / UnitPick / AOBPlacement / …) and **none of it exists** — this is why the todo kept
   calling sealift "M13-adjacent". The Naval Movement Marker is one member of that family; building it
   alone means building the first one.
2. **A DESTINATION VALIDATOR.** Both map flags already exist (`HexTile.IsPort`, `HexTile.IsBeachhead`),
   so this is the cheap part: friendly ports for anyone, friendly-controlled `IsBeachhead` additionally
   for MAR/MMAR.
3. **A RESOLUTION STEP.** Teleport, land Deployed, zero the turn, then the same post-move housekeeping a
   normal move does — icon redraw, stacking refresh, §6.13 tile control, spotting sweep.

### ⚠ It is also currently UNTESTABLE
Khost has **no port and no beachhead hexes**, so none of this can be exercised in play until a map with
a port is authored. That is a content gate, not a code gate, and it is the reason the P2/P3 naval work
has never been seen running.

### 🔴 DECISIONS NEEDED BEFORE CODING (this is the "solve it once and for all" list)
1. **Is the passage risk-free?** §5.4.2.6 defers naval combat and interception, which reads as
   *guaranteed arrival, no attrition, no interception*. Confirm — it is the difference between a
   resolution step and a resolution *engine*.
2. **Embark and sail in the same turn, or two turns?** Deploying up already costs a DeploymentAction and
   50% MP, and §5.4.2.4 says the movement consumes the whole turn. Both in one turn, or board this turn
   and sail next?
3. **What blocks a destination?** Enemy-occupied port, friendly-occupied port, unspotted destination,
   destination that is friendly-flagged but enemy-controlled. The paradrop equivalent (§5.4.1.6) blocks
   on enemy occupancy and aborts — does sealift mirror that, and does a blocked attempt refund?
4. **Is there any lift capacity?** Today `TRN_NAVAL` is a shared profile with no fleet behind it, so any
   number of regiments can sail at once. Intended, or does a scenario cap it?
5. **Is a beachhead landing opposed?** The marine arrives Deployed like everyone else per §5.4.2.5, which
   makes an amphibious assault mechanically identical to walking off a dock. Intended for v1?
6. **Does the AI ever sealift?** If yes it lands with M13; if no, say so and the resolution step can stay
   player-only for now.

### 📌 DOC DEBT found while writing this
§9.10.6 still describes the mechanism as `EmbarkmentState EmbarkedNaval` and explains that ground
templates "show Embarked=NONE yet IsEmbarkable=true" — **both of those symbols were DELETED in P1.**
The RULE it states is current; only its mechanism paragraph is stale. Fold into the §7 amendments table.

## 14b. ORIGINAL P3 BRIEF (kept for the rulings; the naval half is superseded by §14 above)

**The original bug, still live:** an embarked air-assault regiment pays ground terrain costs and
is halted by zones of control it is flying over. The classification test `IsAirUnit || IsHelicopter`
sits in THREE places and all three must change together or the range overlay lies:
- `MovementController.ExecuteMovement` (~:803) — step cost, road bonus, ambush branch, ZoC halt,
  tween pacing (`isFixedWing` at :804 feeds `stepSeconds` + the movement-audio duration).
- `HexMapUtil.GetValidMoveDestinations` (~:321) — range generation.
- `HexMapUtil.FindPath` (~:453) — A* costs.
All three re-key on `MovementModeService.IsAirborneNow` (and pacing on `CurrentMedium`).

**Rulings already made — do NOT re-derive (full text: `todo_audio.md` §3b M4, ratified 2026-08-04):**
- **Ambush-against-a-flight: the ambush TRIGGERS, the combat does NOT.** Detection runs exactly as
  for ground; halt = movement points 0 + move actions 0 and NOTHING else; no `RaiseAmbushTriggered`;
  the ambusher IS revealed; the flight halts on the entered hex. ZoC NEVER stops a flight — ambush
  is the single mechanism. Printer dispatch + `UnitMoveBlocked` sound. Vocabulary: stopped/halted,
  never "aborted" outside player-facing text.
- Helo/jet long-cut audio still unauthored (todo_audio §3b M2 note) — a flight goes silent mid-air
  until Bob authors them; not P3's problem, just don't "fix" it in code.
- **Naval traversal (NEW in P3's scope):** while `IsNavalEmbarked`, the unit moves on WATER hexes.
  ⚠ Decisions a fresh context must get from Bob before coding traversal: may ground units enter
  Water hexes at all today (check `HexMapUtil` terrain rules — Water cost 1 suggests yes, which
  would be its own latent bug for foot units), and does naval movement use the ambush-halt rule or
  nothing. If Bob is not available, implement the AIRBORNE fix only and leave naval traversal
  flagged — khost has no water, so nothing regresses.
- **M5 residue (judge each on its own terms, not a sweep):** `GameDataManager` occupancy
  (`GetGroundUnitAtHex`/`GetAirUnitAtHex` — legitimately classification for stacking),
  `CombatResolver`, `GameIconRenderer` icon layers (legitimately classification). The consumer
  sweep's verdict list is in this doc's history; most remaining reads are LEGIT class-(c) questions.

**After P3:** P4 requisition API (§4.6/§4.7 — wallet arithmetic, cascade, transaction window;
`RequisitionService` headless; the §4.1 `EquipmentBays.CanAccept` split is ratified and built).
Then P5 content/docs/editor (design-doc amendments table in §7; `Transition.md` rewrite; the
khost re-exports if the census tags should reach shipped content — they don't have to, templates
only gate the SHOP).

### What landed
- **Deleted outright:** `RegimentProfileType` (169 authoring lines + enum + save/`.oob` fields),
  `isMountable`/`isEmbarkable` (properties, ctor params, DTO fields, 338 authoring lines),
  `EmbarkmentState` (enum, property, setter, event plumbing), `RaiseUnitDeploymentChanged` +
  subscriber (D10). `SAVE_VERSION` 4 → **5**.
- **`RegimentProfile` → `EquipmentBays`** (git-mv, meta preserved): + `EquipmentBay` enum,
  `IsMobileBayOpen` / `MayCarryHeloLift` / `MayCarryFixedWingLift` / `CanAccept` /
  `TrySetSlot` / `TryClearSlot` (the §4.1–§4.3 doctrine layer, stateless, identity passed in).
  Init signature reordered DEP/MOB/EMB (the old mobile-before-deployed mis-bind trap is gone);
  every caller rewritten with NAMED slot arguments.
- **Capability tags:** new `WeaponTrait.HELO_TRANSPORTABLE` → `WeaponCapability.HeloTransportable`;
  `ART_LIGHT_SV` tagged BOTH, `RCN_BRDM2AT_SV` tagged `AIR_DROPPABLE` (already existed as T31 on
  the BMDs + five nations' airborne infantry). Capability-only traits — zero statline change.
- **`SAM_S300_SV` medium Foot → Wheeled** (box 1; the editor's catch). Statline untouched
  ("truck MMP 8" already said wheeled).
- **Naval groundwork (P2-ready):** `CombatUnit.IsNavalEmbarked` (persisted, no writers yet) +
  `GetActiveWeaponProfile` naval branch drawing the shared `TRN_NAVAL` (whose medium is now
  `Naval`, overriding its Truck family default); `TRN_NAVAL` REMOVED from the two Naval Infantry
  templates' Embarked bays (§9.10.6 finally enforced). ⚠ Interim consequence, deliberate: marines
  cannot naval-embark until the P2 universal port path lands (nothing in khost uses it).
- **D1 re-points, now LIVE rules:** §12.3 airborne-spotting (helo-riders are air targets; HELO
  gunships excluded — NoE), `CombatResolver.IsHeloAirDefenseTarget` (§11.8.9 lane fires for lifts
  in transit), the naval icon branch (keys on the bool). The two tests that pinned the dead state
  were REWRITTEN to exercise the real mechanism.
- **New suite `EquipmentBaysTests`** (12): the three §4.3 template audits over all 169 templates,
  bay capacity by physics (tank/S-300 closed, infantry/S-75 open), both eligibility routes,
  CanAccept/TrySetSlot/TryClearSlot, naval-never-a-slot + transient-state draw, D1 pins.
- ⚠ Mid-pass incident, resolved: a PowerShell `Get-Content`/`Set-Content` pass double-encoded
  `CombatUnitDB.cs`'s Unicode (UTF-8-no-BOM misread as ANSI); restored from git, redone with
  explicit UTF-8. Bulk file edits in this repo must use BOM-aware IO.

### ✅ CENSUS A — RATIFIED (Bob, 2026-08-08) AND IMPLEMENTED
**Bob's ruling: "SPSAM, SPAAA are base only. SAM and AAA should be helo and droppable"** (and
Soviet generic AAA definitely both). Booked as:
| Profile set | Ruling | Tagged |
|---|---|---|
| Light towed tubes: `ART_LIGHT_SV/WEST/ARAB/CH`, `ART_LIGHT_MJ`, `ART_MORTAR_MJ` | **BOTH** | ✅ |
| ALL towed SAM/AAA: `SAM_S75_SV`, `SAM_S125_SV`, `SAM_HAWK_US`, `SAM_GEN_MJ`, `AAA_GEN_SV`, `AAA_GEN_MJ` | **BOTH** | ✅ |
| Heavy towed: `ART_HEAVY_SV/WEST/ARAB/CH` | **NEITHER** (stood unchallenged) | — |
| Every SP system (`SPA_*`/`SPAAA_*`/`SPSAM_*`/`ROC_*`) | **base only — no lift, ever** | — (no tags; bays close by medium) |
Pinned in `EquipmentBaysTests.CapabilityTags_AreAuthoredWhereRatified` + the S-75 eligibility case.

**B. SAM/AAA self-contained check** — every non-Foot medium in those families verified sensible:
all `SPA_*`/`SPAAA_*`/`SPSAM_*`/ZSU/Tunguska/Kub/Gepard/Roland/Rapier/Chaparral = Tracked;
BM-21/27/30, Scud, Strela-1, Crotale, HQ-7, **S-300** = Wheeled. **No further Foot→self-propelled
flips found** — the S-300 was the only mis-declared one. Census closed unless Bob objects.

### Editor signal (their Reply 4 ask 1)
The re-key is DONE — `CombatUnit.cs` no longer contains the `IsEmbarkable` symbol at all — but the
signal note sent to their Markdowns says **flip only after Bob reports the suite green**, since
their firewall exists precisely to not trust unverified claims.

**P2 starts on suite green.**
