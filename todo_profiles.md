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

**P0 CLOSEOUT (2026-08-08):** (a) ✅ **suite GREEN — all 452 tests** (Bob ran the Test Runner);
(b) ✅ OOB check done (fresh-placement test + in-game load of the new format); box 9 ✅ ruled
(Option A). **(d) COMMIT is the LAST remaining gate — everything after `d6abfcb` is uncommitted.**
**P1 starts on the commit.** No code from this doc has been written.
