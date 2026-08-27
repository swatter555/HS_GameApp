# Supply Unification — one number per unit

> **The intent (Bob, 2026-08-24, verbatim in substance):** Days of Supply is a straight number the game
> deducts from as supply is used — call it supply points if that reads better. Regular units cap at 5.
> Depots have much bigger caps; airbases a sizable one. When a depot distributes supply it comes out of
> that ONE number, same as an airbase fueling sorties. It is all the same number — the "supply stockpile"
> distinction is not helping. Nail this down and §15 becomes "just deduct points."
>
> **Status: ✅ CLOSED 2026-08-24 — SUP-1 landed, suite GREEN + Khost play-confirmed (Bob-run same day).**
>
> Groundwork for the §15 supply thread (`Claude_TODO.md` thread board). Companion to the combat pass
> (`Implementing Combat.md`) — air ops consume supply, so this lands before AIR-1 spends any.

---

## 1. THE SURVEY — every supply touchpoint, verified 2026-08-24

### 1.1 The verdict up front

**The two-number model is the code's invention, not the design's.** `HS_DesignDoc.md` §15 already describes
Bob's intent exactly: §15.1.2 "each unit tracks DaysSupply (current)", ground units cap 5; §15.6/§15.7 give
facilities bigger caps (depot 30/50/80/110 by size, airbase 30); §15.4a.4 transfers "from the depot stockpile
to the unit's DaysSupply". The doc's word "stockpile" names a facility's ONE supply store — nowhere does any
unit carry two. The code deviated by giving depots BOTH a 5-day `DaysSupply` AND a separate
`StockpileInDays` field. Airbases never had the split — their 30-day store IS their `DaysSupply`, and the
whole sortie economy (§11.2.3 launch/shot costs, the §11.2.3a 5-day floor, `CanLaunchSortie`) already reads
it. **Airbases are the proof the one-number model works; depots are the one class that diverged.**

### 1.2 What makes this cheap: the depot API has ZERO external callers

Every depot supply method — `AddSupplies` · `RemoveSupplies` · `GenerateSupplies` · `SupplyUnit` ·
`CanSupplyUnitAt` · `PerformAirSupply` / `PerformNavalSupply` (→ `PerformRemoteSupply`) ·
`GetStockpilePercentage` · `IsStockpileEmpty` · `GetRemainingSupplyCapacity` · `UpgradeDepotSize` — is
**pre-built machinery with no caller anywhere** (the §15 pass that would call them is the
`BattleManager.ProcessUpkeep` stub; the §3.3.3 out-of-supply hook is commented out). The only code outside
`CombatUnit` touching `StockpileInDays` is the 2026-08-24 band-aid set itself: the loader block, the panel
branch, and `DepotStockpileTests`. Rewriting the field out is therefore an internal refactor plus three
local touchpoints — not a codebase sweep.

### 1.3 Full touchpoint inventory

| Area | File(s) | What it does today | Under unification |
|---|---|---|---|
| **The unit-side pool** | `CombatUnit` — `DaysSupply` (StatsMaxCurrent), `ConsumeSupplies`, `ReceiveSupplies`, `GetSupplyStatus`, `CanMove`, action-economy gates (`CanConsume*` + thresholds) | Already the one number for every non-depot use | **Unchanged.** `ReceiveSupplies` clamps at `DaysSupply.Max`, so bigger caps just work |
| **Caps** | `CombatUnit` ctor line ~287; `GameData`: `MaxDaysSupplyUnit` 5, `MaxDaysSupplyAirbase` 30, `MaxStockpileBySize` 30/50/80/110 | Ctor: airbase 30, EVERYTHING else 5 — including depots | Ctor three-way: depot gets its size cap. Constants all stay |
| **The parallel depot store** | `CombatUnit.StockpileInDays` + `GetMaxStockpile` + `SetDepotSize` + `UpgradeDepotSize` + `CopyTemplateFrom` + the `[JsonConstructor]` | The duplicate number | **`StockpileInDays` DELETED**; all internal uses become `DaysSupply`. `SetDepotSize` sets `DaysSupply.SetMax(cap)` (SetMax already clamps Current down — verified) |
| **Depot distribution API** | the §1.2 method list | Reads/writes `StockpileInDays`; zero callers | Same methods, same signatures, reading `DaysSupply` — the §15 pass calls them later unchanged |
| **Generation** | `GetCurrentGenerationRate` = `GenerationRateValues[rate] × GetMaxStockpile() × facility-eff` (§10.8.4 fraction-of-own-cap — code already conforms) | fraction × stockpile cap | fraction × `DaysSupply.Max` — same value |
| **Sortie economy** | `CanLaunchSortie` (§11.2.3a floor 5), `SORTIE_LAUNCH_COST`/`SORTIE_SHOT_COST`, `AIR_SUPPLY_LOAD` | Already reads airbase `DaysSupply` | **Unchanged** — already the intent |
| **Probabilistic burn** | `DegradationCheck` §7.15 rolls, wired in `GroundCombatAction`/`AmbushAction`/`IndirectCombatAction`; `CombatOracle` mirrors the chances | Burns 1 `DaysSupply` on the roll | **Unchanged** (⚠ any §7.15 change still re-runs `CombatOracleTests` — not touched here) |
| **Persistence** | `CombatUnit` `[JsonInclude] stockpileInDays`; `SnapshotMapper` restore | ⚠ **LATENT SAVE BUG:** restore rebuilds units and never copies `StockpileInDays`, then FORCE-RESETS a depot's `DaysSupply` to Max (lines ~172-175) — a half-spent depot reloads FULL | Field leaves the save shape ⇒ **SAVE_VERSION 9**, no arm (pre-1.0). Delete the force-reset; the generic `DaysSupply.Current` copy is now correct — **the bug dies as a side effect** |
| **Content** | `OOBFileLoader` (`DaysSupply` real days + clamp + ratio tripwire; `StockpileInDays` optional field added 2026-08-24); `khost.oob` | The days ruling landed; the depot field is hours old | **`StockpileInDays` field + loader block DELETED** (never reached the editor). Depots author their big number IN `DaysSupply`; the existing clamp now clamps against the size cap. khost depots: `DaysSupply` 5.0 → 30.0 / 80.0, the 8 `StockpileInDays` keys removed |
| **Display** | `Prefab_UnitPanel` — the 2026-08-24 depot branch ("Stockpile: N/Max (Size)"); `PrinterMessage` friendly supply line | The branch is the band-aid | **Branch DELETED** → ONE line for every unit: `Supply: {cur:F1}/{max:F0} days` (regiment 5.0/5 · airbase 30.0/30 · depot 80.0/80). PrinterMessage line gains the same /max |
| **Tests** | `DepotStockpileTests` (6); other suites touch `DaysSupply` only on regular units | Pins the model being deleted | **Rewritten** (§3). No other suite constructs a depot — verified |
| **Future §15 hosts** | `ProcessUpkeep` stub + §3.5.4-.6 chain, `OnResupplyRequested` (ZERO subscribers; HUD button is a stub), `OnSupplyOverlayToggled`, leader supply skills (M14), `EmergencyResupply`, §15.10 | All unbuilt/unwired | Untouched — they read one number when they arrive, which is the point |

### 1.4 Findings worth recording beyond the refactor

1. ⚠ **The latent snapshot bug** (§1.3 Persistence) — real today, dies free with the unification.
2. ⚠ **`SupplyUnit` deduct/deliver mismatch vs §15.4a.7:** the code deducts a FLAT 5 from the depot but
   delivers `5 × efficiency`, while the doc says the stockpile is "reduced by the days DELIVERED". Zero
   callers, so dormant — **NOT fixed in this pass** (it is §15-pass behavior, and the doc may want the
   inefficiency to be real loss); logged here so the §15 pass rules it deliberately.
3. **Naming:** keep `DaysSupply`. The doc speaks days everywhere, 1 turn = 1 day makes days the honest
   unit, and the persisted JSON name (`daysSupply`) stays put. "Supply points" can be display copy later if
   Bob wants it; no code reason to rename.
4. **HQs** keep the 5-day cap (nothing rules them bigger; flag at §15 if wanted).

### 1.5 FIXED-WING CARRY NO SUPPLY OF THEIR OWN (Bob, 2026-08-24 — doc intent CONFIRMED)

**The rule:** a fixed-wing air unit has no supply state. It is part of its airbase for supply purposes —
launch deducts 1 from the AIRBASE (§11.2.3), each shot 0.5, gate is the airbase's §11.2.3a floor. The unit's
own `DaysSupply` should read ZERO, **which means its Max must be 0** — not merely its Current.

**Doc scan — the intent already exists, verbatim, twice:**
- §10.3.1: "fixed-wing carry NO individual DaysSupply — supply lives on the launching airbase per 11.2.3 / 24.5.2.1"
- §15.1.2: "Fixed-wing aircraft carry NO individual DaysSupply — their supply is drawn from the launching
  airbase stockpile per 11.2.3 / 15.7.2"
- Consistent scope: helicopters are NOT included — §15.1.2 caps "ground combat units (incl. helicopters)"
  at 5, and a helo is a ground-domain regiment. Fixed-wing = the 7-member `IsAirborneClassification` list
  (FGT / ATT / BMB / RECONA / AWACS / WW / TRN).
- ⚠ Vocabulary guard: the TRN dual-use (§7A.22) does NOT leak this rule onto ground units — the An-12
  PROFILE serving as an AB regiment's Embarked bay belongs to the REGIMENT (a ground unit, cap 5); only a
  TRN-classified UNIT is supply-less.

**⚠ THE BITE, FOUND IN CODE — the zeroing cannot ship alone:** `CanBeginMoveOrder` → `CanMove()` refuses any
unit with `DaysSupply.Current < 1`, and `MovementController.BuildEligibleUnitsList` (next/prev cycling)
filters on `CanMove()` too. A fixed-wing at 0/0 could not be ordered to MOVE — the AIR-1 transit walk would
be dead on arrival, and unit cycling would skip every aircraft. **The guard is the unified model's own
idiom: a unit with `DaysSupply.Max == 0` does not check supply** ("no supply store" ⇒ no supply gate —
self-describing, no new classification list at rule sites). `ConsumeSupplies`/§7.15 burn rolls on a 0-max
pool already no-op harmlessly; the airbase-side §11.2.3 costs are AIR-1's wiring, unaffected.

**⚠ ONE DOC WRINKLE, deferred to the §15 pass (flagged per the challenge-contradictions rule, NOT changed
here):** §15.5.3.1 fires out-of-supply consequences for "every unit whose DaysSupply ≤ 0" — read literally,
a 0/0 fixed-wing is punished every turn forever. Yet §7.9.8.5 expects fixed-wing OOS to be REACHABLE. The
resolution the doc itself implies: a fixed-wing is out of supply through its AIRBASE (§15.7.6 — exhausted or
isolated base), never through its own number. The §15 pass implements the OOS check with a `Max > 0` guard
plus an airbase-keyed fixed-wing arm; §15.5.3.1's wording gets a clarifying amendment then.

**Display (the case that prompted this):** cycling through an airbase's attached aircraft is NOT BUILT yet
(AIR-1/AIR-2 UI). When it exists, a fixed-wing's supply line reads zero — under SUP-1's uniform line that is
"Supply: 0.0/0 days", satisfying "should probably say zero"; a later nicety may re-label it "(supply: see
airbase)" per §24.5.2.1, decided when the cycling UI is designed. **Until then this section is the record of
intent so no session "fixes" a fixed-wing's 0 supply back to 5.**

---

## 2. THE PLAN — one milestone, one suite run

### SUP-1 — Unify `[x]` — DONE 2026-08-24, suite GREEN + play-confirmed (Bob-run)

All in one change set, because the intermediate states are incoherent:

1. **`CombatUnit`:** ctor sizes `DaysSupply` three-way (depot → `MaxStockpileBySize[size]` · airbase → 30 ·
   else 5). ⚠ Ctor-order note: `InitializeFacility` runs BEFORE `DaysSupply` is constructed, so
   `SetDepotSize` must not touch `DaysSupply` during construction — the ctor sizes it; `SetDepotSize`'s
   `DaysSupply.SetMax` matters on the (zero-caller) `UpgradeDepotSize` path.
2. Delete `StockpileInDays`, `SetStockpile`, `MaxStockpileInDays`; re-point every internal use to
   `DaysSupply`; `GetMaxStockpile` survives as the private size-table read that `SetMax` and generation use.
3. **SAVE_VERSION 8 → 9** (`stockpileInDays` leaves the CombatUnit JSON shape; pre-1.0 — floor tracks, no
   ladder arm, per CLAUDE.md §2.12). Delete the SnapshotMapper depot force-reset.
4. **`OOBFileLoader`:** delete the `StockpileInDays` field + handling; update the clamp/tripwire wording
   (full regiment 5 · airbase 30 · depot 30/50/80/110).
5. **`khost.oob`:** depot `DaysSupply` → 30.0 ×7 / 80.0 ×1; delete the 8 `StockpileInDays` keys.
   Behavior-identical (everything full).
6. **`Prefab_UnitPanel`:** delete the depot branch; one uniform `Supply: cur/max days` line.
   `PrinterMessage` friendly line matches.
6a. **Fixed-wing zeroing (§1.5):** ctor's three-way becomes four-way — fixed-wing (`IsFixedWing`) →
   `DaysSupply` Max 0; `CanMove` supply clause guarded `DaysSupply.Max > 0`; `khost.oob`'s 4 Su-17s
   author `DaysSupply` 0.0 (currently 5.0 — would clamp to 0 with a warning otherwise); tests pin the
   0-cap, the move-order guard, and that a helo still carries 5.
7. **Tests:** `DepotStockpileTests` → `DepotSupplyTests`: caps by classification (regiment 5 / airbase 30 /
   depot by size ×2), `ReceiveSupplies` clamps at cap, `ConsumeSupplies` deducts, `UpgradeDepotSize` raises
   the cap, template-copy resets to full cap.
8. **Design-doc amendment (with Bob):** one clarifying line in §15.1.2 — "a facility's 'stockpile' IS its
   `DaysSupply`; one number per unit, caps differ" — plus §15.6/15.7 pointers. The doc already means this;
   the line stops the code's old split from ever being rebuilt from a misreading.

**Proves it:** suite green (rewritten depot tests + no movement elsewhere); Khost shows Supply 5.0/5 on a
regiment, 30.0/30 on an airbase AND a cache, 80.0/80 on the Soviet depot — one label, no "Stockpile" word
anywhere in play.

### ⚠ SCENARIO EDITOR IMPACT (Bob relays — the queue item is REWRITTEN, do not send the old one)

- `DaysSupply` real days: **unchanged** from the earlier relay — still urgent before the Hamburg export.
- **`StockpileInDays` is RESCINDED** before the editor ever implemented it — it must NOT appear in the
  editor's OOB inspector. If the earlier relay text already went out, this supersedes it.
- **Depots author their big number directly in `DaysSupply`** (full = 30/50/80/110 by size). Suggested
  editor validation: cap the field by classification (5 / 30 / size-cap).
- **Fixed-wing units author `DaysSupply` 0** (FGT/ATT/BMB/RECONA/AWACS/WW/TRN — they carry no supply;
  it lives on the airbase, §10.3.1/§15.1.2). Suggested editor validation: force the field to 0 for these
  classifications. Helicopters are NOT included — they cap at 5 like other ground units.
- The `HitPoints` ratio-or-real question from the earlier relay still stands.

---

## 3. PROGRESS LOG

- 2026-08-24 — ⚑ CLEARED / **PASS CLOSED**: suite GREEN and Khost play-confirmed across every unit class
  (5/5 · 30/30 · 30/30 · 80/80 · Su-17 0/0, movement normal). The fixed-wing guard was the silent-failure
  risk and it verified in play. Remaining supply work is the §15 pass itself, on a settled model; the two
  deliberate leave-alones stay logged for it (§1.4-2 `SupplyUnit` deduct/deliver vs §15.4a.7; template
  depots arriving empty) plus the §15.5.3.1 airbase-keyed fixed-wing OOS arm (§1.5).
- 2026-08-24 — SUP-1 LANDED (Bob's go; one change set, verified by inspection — agent cannot compile):
  `StockpileInDays` deleted with `SetStockpile`/`MaxStockpileInDays`; ctor sizes `DaysSupply` four-way
  (fixed-wing 0 / airbase 30 / else 5, constructed BEFORE InitializeFacility) and `SetDepotSize` raises a
  depot to its cap (single sizing authority, ctor + upgrade — upgrade refill preserved); all ten depot
  methods re-pointed to `DaysSupply` (the §1.4-2 deduct/deliver mismatch left in place, now commented at
  the site); `CanMove` gains the `Max > 0` guard; SnapshotMapper depot force-reset deleted (bug dead);
  SAVE_VERSION 9 (floor auto-tracks); loader field + block deleted, warnings re-worded; khost.oob depots
  30.0×7/80.0, Su-17s 0.0, rescinded keys removed; panel + printer show uniform `cur/max`;
  `DepotSupplyTests` (9) replaces `DepotStockpileTests`; DESIGN DOC AMENDED — NEW §15.1.2a ratifies
  one-number-per-unit + fixed-wing zero + the §15.5.3 airbase-keyed OOS arm, and tombstones the old split.
  Template-instantiated depots still arrive EMPTY (pre-SUP-1 behavior preserved, noted for P4/§15).
- 2026-08-24 — Fixed-wing supply ruling recorded (Bob; §1.5): fixed-wing carry NO own supply — Max 0,
  airbase pays (doc intent confirmed at §10.3.1 + §15.1.2). Found the bite: `CanMove` would refuse a 0/0
  aircraft any move order and cycling would skip it — the `Max > 0` guard ships WITH the zeroing (SUP-1
  step 6a). §15.5.3.1's "every unit ≤ 0" wording flagged as the deferred §15-pass wrinkle (airbase-keyed
  fixed-wing OOS). Editor relay gains the author-0 line. Still awaiting Bob's go on SUP-1.
- 2026-08-24 — Survey complete (§1); plan drafted (§2); NOT implemented — awaiting Bob's go. Editor-relay
  item in `Claude_TODO.md` rewritten to the §2 form so the rescinded field cannot be relayed by accident.
  Round-2 depot ⚑ (suite + panel display) came back GREEN before this — the display band-aid worked, and is
  the first thing SUP-1 deletes.
