# AD Engagement-Range Pass (§11.8 defect) — plan + record, 2026-08-22

> Spun off from the ICM pass ("THE BIG FIND", `todo_icm.md`). Status: **CLOSED 2026-08-22 —
> suites green (Bob-run), committed `83d0f99` (fix + re-band + pins together).**
>
> **Why Option A proceeded without a fresh chat ratification:** §11.4.4 already ratifies the
> envelope as the IR range ("every enemy SAM/SPSAM/AAA/SPAAA whose spotting + IR range covers the
> transit hex may fire") — so Option A is a DEFECT FIX against an existing ratified rule, and
> Option B would have been the design change needing a ruling. The handoff recommended A, Bob
> asked for an immediate pass, and nothing closes until Bob runs the suite — that run is the veto
> point. If Bob prefers Option B after all, the revert is one line + the test region.

## 1. The defect (confirmed by inspection this session)

Every SAM/SPSAM/AAA/SPAAA profile authors its air-defense engagement envelope as a
`ProfileStat.IR` delta in `WeaponProfileDB.cs` — the supplement T71 "IR high" idiom. Verified sweep:
**all ~22 Sam/Aaa-archetype profiles carry an IR delta, no gaps** (ZSU-57/23 & all `INDIRECT_RANGE_AAA`
carriers = 3 · Strela-1 = 4 · Gepard = `INDIRECT_RANGE_SHORT` · S-125 = 5 · `INDIRECT_RANGE_SAM`
carriers (2K12/2K22/Hawk/Roland/Crotale/Rapier/HQ-7/S-75) = 6 · S-300 = 10 · MJ Stinger = 3).

But no AD profile sets PR (the `Sam`/`Aaa` archetypes default PR 1), and the ONLY consumer of AD
reach — `SpottingService.FindTransitAirDefense` ([SpottingService.cs:525]) — reads
`ActivePrimaryRange` with a `≤0 → 2` fallback. `ActiveIndirectRange`'s only consumers
(`CombatResolver.IsInIndirectRange` / `IsCounterBatteryEligible`) are class-gated to ART/SPA/ROC/BM.

**Net: every AD battery interdicts at range 1; the entire authored ladder is dead data.**

The design doc already keys the envelope to IR: **§11.4.4** — "every enemy SAM/SPSAM/AAA/SPAAA whose
spotting + IR range covers the transit hex may use opportunity actions to fire (per 11.8.5)." So the
IR authoring is the doc-intended envelope, not an accident.

## 2. Fix direction — Bob to ratify

**Option A (RECOMMENDED): read the IR envelope in the scanner.** In `FindTransitAirDefense`, replace
the range line with:
```
int engagementRange = Mathf.FloorToInt(Mathf.Max(enemy.ActiveIndirectRange, enemy.ActivePrimaryRange));
if (engagementRange <= 0) engagementRange = 2;   // unchanged safety fallback
```
- One line of behavior change, inside a method already gated to AD classifications — zero risk of
  leaking into artillery/CB code (those consumers are class-gated the other way).
- Matches §11.4.4's authoritative wording and keeps the supplement T71 "IR high" idiom intact.
- `max(IR, PR)` rather than bare IR so a future direct-fire AD hybrid still works; fallback kept.

**Option B (bigger, NOT recommended): move the envelopes from IR deltas to PR deltas in the DB.**
Touches ~22 profiles + the supplement idiom + every survey/analysis doc that reads "IR high", and
contradicts §11.4.4's wording. Only worth it if Bob wants IR to mean *indirect bombardment* strictly.

## 3. Implementation steps (Option A)

- [x] `SpottingService.FindTransitAirDefense` — the range read (above) + remark update (the envelope
      is the authored IR ladder per §11.4.4; PR kept in the max for hybrids; fallback = "profile
      states no reach at all").
- [x] `AirDefenseTransitTests.Reach()` helper — mirror the new read (it exists precisely so a
      profile change moves scan + tests together).
- [x] New regression tests (`AirDefenseTransitTests`, new §11.4.4 region):
  - [x] **Real-range ladder**: S-300 (IR 10) engages at 10, refuses at 11; ZSU-23 (IR 3) engages
        at 3, refuses at 4 — pins that the ladder is read, both ends.
  - [x] **Ladder is differentiated**: S-300 envelope > ZSU envelope (kills the "S-300 has a ZSU's
        umbrella" defect by name).
  - [x] **§11.8.6 across a wide envelope**: `ResolveTransitFire` at two successive hexes deep in the
        S-300's envelope spends exactly one opportunity action (anti-dogpile holds when the envelope
        is wide — the case the old range-1 world could never exercise).
- [x] Docs, same pass: HS_DesignDoc — NEW §11.8.2d (envelope = profile IR per §11.4.4 + defect
      record + no-re-pricing + spotting-term deferral) · `Claude_Project.md` reconcile entry (header
      + §3.7 transit-AD block) · `Claude_TODO.md` change log + ⚑ `[!]` testing entry. Supplement
      unchanged (T71 idiom was right all along).
- [ ] **"Please run Unity Test Runner for me"** — full EditorTests; wait for Bob's green before
      closing. `[!]` — do not build on the transit path until green.

## 4. Balance notes (flags, not code)

- **Khost is NOT strictly unaffected**: MJ Stinger/AAA teams go from engaging helos at 1 hex to
  their authored 3. That is the *intended* envelope (prestige was tuned against it), but helo
  routing near known AD gets materially more careful. Play-feel check worth one sortie.
- **Future NATO scenarios change materially**: fixed-wing transit across a SAM belt becomes the
  danger it was designed to be (S-300/Hawk umbrellas 6–10 hexes). Deliberate; no re-pricing —
  ⚠ do NOT re-tier AD prestige around this fix (handoff instruction, reaffirmed).
- **§11.4.4 "spotting + IR" question (flag only, no change now):** the scan applies range only —
  no spotting gate (consistent with §11.8.4 automatic fire; radars see what eyes don't). If Bob
  wants a spotting term in the envelope, that belongs to the M13 `AirThreatService` item, whose
  spec already says "footprint = spotting + IR per §11.4.4" — the overlay and the walk must agree,
  so rule it there, once, not here.
- **L4 note:** when the owed `IndirectRangeBonus → ActiveIndirectRange` leader wiring lands, an
  artillery-skill leader on an AD battery would grow its air envelope too. Flag at wiring time.

## 5. Review (2026-08-22, implementation session)

**One line of behavior change, four tests, three docs.** The defect was exactly as the ICM-pass
handoff described: verified by inspection that (a) `FindTransitAirDefense` is the sole consumer of
AD reach and read `ActivePrimaryRange` (unset on every AD profile → archetype default 1 → every
battery interdicted at range 1), (b) all ~22 Sam/Aaa-archetype profiles author an IR delta with no
gaps, and (c) `ActiveIndirectRange`'s only other consumers (`IsInIndirectRange`,
`IsCounterBatteryEligible`) are class-gated to ART/SPA/ROC/BM — so giving IR its AD meaning cannot
leak into artillery or counter-battery code.

Files touched:
- `SpottingService.cs` — the range read: `max(ActiveIndirectRange, ActivePrimaryRange)`, floor,
  `≤0 → 2` fallback kept; defect-record comment at the site.
- `AirDefenseTransitTests.cs` — `Reach()` mirrors the read; new "§11.4.4" region with the four
  regression tests. Existing tests audited: all scan at distance 0–1, inside every envelope under
  both the old and new read — unaffected by inspection.
- HS_DesignDoc §11.8.2d (NEW) · `Claude_Project.md` (header + §3.7) · `Claude_TODO.md`
  (change log + ⚑ `[!]` suite-run entry).

Not done, deliberately: no AD re-pricing (prestige was tuned against the intended envelopes); no
spotting term in the envelope (§11.4.4's "spotting +" is ruled once at M13 `AirThreatService`);
no DB/supplement changes (the IR authoring was correct all along). No SAVE_VERSION bump — nothing
persisted changed shape.

Flag carried forward: when the L4 `IndirectRangeBonus → ActiveIndirectRange` leader wiring lands,
an artillery-skill leader on an AD battery would grow its AIR envelope too — decide then whether
that is a feature or needs a class gate.

**Outstanding:** Bob's full EditorTest run (the ⚑ `[!]` entry), plus one optional Khost helo
sortie past known MJ AD (envelopes now 3 hexes, the first play-visible change).

## 6. Addendum (same day) — the point-defense re-band + the Tunguska correction

**Correction first:** the survey table in this file's own session initially reported the Tunguska
at IR 3 by reading only the ProfileDef deltas. **The envelope is trait-composed** —
`GUN_MISSILE_COMBO` carries IR+2 (the only IR-bearing trait in the catalog), so the Tunguska
resolves to **5**. Bob's provisional "set it to 4" was premised on the wrong 3 and was withdrawn
when the trait term surfaced; **ruled: Tunguska STAYS 5** (apex short-range AD — above Strela-1's
4, below Kub/S-75's 6). The same mistake the ICM pass logged ("resolve `GameData.*` constants,
don't match bare ints") in a new costume: resolve TRAIT EFFECTS, don't read bare deltas.

**The re-band (Bob-ratified):** with envelopes live, the NATO point-defense systems — Chaparral
(~8 km), Roland (~6 km), Crotale (~10 km), Rapier (~7 km) — were sitting at the flat SAM 6, the
same 30 km umbrella as the area-defense Hawk. All four re-authored to `INDIRECT_RANGE_SHORT` (4),
the Strela-1 band; **HQ-7 rides along** (it is a Crotale clone); Hawk stays 6; Vulcan 3 and
Gepard 4 unchanged. Rationale: realism for the AI-side faction Bob is about to test — game
economy matters less for non-player units. NATO deliberately has no 10-tier (no Patriot profile).

**The ratified ladder (DesignDoc §11.8.2d):**
guns 3 · point-defense 4 (Gepard/Strela-1/Chaparral/Roland/Crotale/Rapier/HQ-7) ·
Tunguska + S-125 5 · area SAM 6 (Kub incl. IQ, S-75, Hawk) · S-300 10.

Files: `WeaponProfileDB.cs` (5 profiles, IR SAM→SHORT + header comments) ·
`WeaponProfileNatoTests` (+5 envelope pins) · `WeaponProfileSovietTests` (+3, incl. the
trait-composed Tunguska 5 — pinned precisely because a delta-only read reports 3) ·
`WeaponProfileChineseTests` (+1) · DesignDoc §11.8.2d ladder text · `Claude_Project.md` ·
`Claude_TODO.md`. Rides the same ⚑ suite run as the range fix.
