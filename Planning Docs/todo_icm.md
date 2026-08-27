# todo_icm.md — ICM Pass, Session 2026-08-22

Source analysis: `WeaponProfile_ICM_Prestige_Survey_2026-08-22.md` (§3 benchmark, §7 census-vs-ICM).
Status: **IMPLEMENTED 2026-08-22** (Bob approved the full list same session) — see the Review
section at the end. Outstanding: Bob's Unity Test Runner pass.

## Ratified this session (Bob, 2026-08-22, chat)

1. **Closed-bay ICM doctrine:** the ACTIVE profile represents the CombatUnit as a whole. ICM is
   meaningful only on profiles that are a unit's sole profile (all other bays closed). Profiles
   meant to be PAIRED (INF + APC/IFV, towed gun + truck, anything sitting in a Mobile/Embarked
   bay) keep ICM 1.0. Infantry ICMs untouched barring corner cases. Mechanically ICM stays
   trait-fed (per §15.1); this ruling changes its MEANING for closed-bay profiles: whole-formation
   quality, not just platform fire control.
2. **Armored Cavalry Squadron (`TANK_M60_US`) → ICM 1.4** (census: 8 AH-64 + 8 OH-58 + 36 M3 +
   24 TOW justifies it; Q9 resolved as "census is real").
3. **The §7 candidate edit list is accepted:** NATO first-line tank formations get formation-quality
   ICM; Soviet ×9, Chinese ×3, Iraqi ×2, Iranian M60A3 unchanged; AMX-30 and NL/BE/DK Leo 1
   brigades unchanged (⇒ working principle: formation MASS is normalized away — one counter = one
   maneuver formation, §1a — mass is not priced by ICM; census stays an intel/loss artifact).

**Ratified addendum (Bob, 2026-08-22, later same session):**

4. **Recon = skirmishers with staying power.** All six recon profiles become **Soft** targets
   (delete the five `SetTargetClass(TargetClass.Hard)` calls on BRDM2AT / M3 / Luchs / FV105 /
   ERC90 — the M3 explicitly included: "M3 scouts should be tough", and Soft IS the tough
   setting, SD 9 defends instead of HD 5). **`RECON_FRAGILE` is deleted** from the BRDM-2 and
   parked dormant in the catalog — with skirmisher intent the statlines (HA 2–6 / SA 5–6,
   Disadvantaged-at-best attacks) are the offense governor, and dedicated hunters (gunships
   SA13 → Favorable, artillery missions) remain the AI's efficient counter. No recon ICM edits;
   all scouts sit at 1.00. "We can always tweak if needed."

**Ratified addendum 2 — artillery rulings (Bob, 2026-08-22, chat):**

5. **PHZ-89 + Type 82 re-tier (pricing drift):** `ROC_PHZ89` → `SetPrestigeCost(Gen1, ROC)` = 175
   (was Gen3/295); `SPA_TYPE82` → `SetPrestigeCost(Gen2, SPA)` = 190 (was Gen3/250).
6. **M109 precision round — "add one":** `SMART_MUNITION` added to `SPA_M109_US` only
   (Copperhead was US-fielded) → resolves HA 5→8, SA 10→11.
7. **2S19 vs 2S5 range inversion STANDS** — deliberate, revisit only on player complaints.
8. **`ART_HEAVY_CH` IR 6→5** (`INDIRECT_RANGE_LONG` → `MEDIUM`) — range drift, aligns with every
   other heavy towed.
9. **Fires-quality ICM ratified** (Bob: "yes, this should be fixed in ruling 6" — read as
   ratifying the §5-proposed fix; flagged in case the numbering meant otherwise): new trait
   `FIRE_DIRECTION_NET` (`TraitCategory.FireControlOptics`, Icm ×1.05, "TACFIRE-era automated
   fire direction — faster, tighter missions") on `SPA_M109_US/GE/FR/UK` + `ROC_MLRS_US`.
   With ruling 6, `SPA_M109_US` totals ICM 1.05 · HA8 · SA11.
10. **SP-gun classification normalized (drift):** `US_ARTILLERY_REGIMENT`,
    `GE_SP_ARTILLERY_REGIMENT`, `UK_SP_ARTILLERY_REGIMENT`, `FR_SP_ARTILLERY_REGIMENT`,
    `IQ_SP_ARTILLERY_REGIMENT` → `UnitClassification.SPA` (were ART; only USSR used SPA).
    ⚠ Khost ships no NATO/IQ units, so no `.oob` re-export is triggered; verify at
    implementation that §12.3 spotting tables treat ART/SPA identically (if not, this is a
    behavior change to surface).
11. **MJ artillery zero prestige = authoring miss** (the two profiles simply never got a
    `SetPrestigeCost` call; every other MJ profile is priced). Recommended fix pending Bob's
    nod: `ART_MORTAR_MJ` + `ART_LIGHT_MJ` → `SetPrestigeCost(Gen1, ART)` = 90 — the formula has
    no sub-Gen1 arm; if 90 feels rich for ragtag tubes (SA 6–7), the documented explicit-cost
    exception pattern is available. Impact is V19 kill-bounty only (MJ is AI-side).

**AD range defect:** spun off to its own session (Bob confirmed) — chip
"Fix §11.8 AD engagement range reading PR not IR" / todo_icm "THE BIG FIND" section.

## Changes to make

### 1. Three new formation-quality traits (`WeaponTrait` + `WeaponTraitCatalog`)

Category: `TraitCategory.Economy` (the §12 universal bucket where `EXPORT_DOWNGRADE` /
`RECON_FRAGILE` already live — no new category enum member needed). Append-only on the enum.

| Trait | ICM | Note (catalog text) | Carriers |
|---|---|---|---|
| `COMBINED_ARMS_TF` | ×1.15 | US battalion task-force integration — habitual cross-attachment, Air-Land Battle C3 | `TANK_M1_US` |
| `NATO_FIRST_LINE` | ×1.10 | NATO first-line formation quality — integrated C3, combined-arms brigade structure | `TANK_LEOPARD2_GE`, `TANK_LEOPARD1_GE`, `TANK_CHALLENGER1_UK` |
| `AIR_CAVALRY` | ×1.21 | US Armored Cavalry — combined-arms squadron + organic attack-aviation troop (composite ≈ two QUALITY_M) | `TANK_M60_US` |

### 2. Resulting ICM totals (the only profiles that change)

| Profile | Now | After | Check |
|---|---|---|---|
| TANK_M1_US | 1.334 | **1.534** | ≤1.6 guideline ✓ |
| TANK_LEOPARD2_GE | 1.334 | **1.467** | ✓ |
| TANK_LEOPARD1_GE | 1.103 | **1.213** | ✓ |
| TANK_CHALLENGER1_UK | 1.213 | **1.334** | lands where M1 is today ✓ |
| TANK_M60_US | 1.155 | **1.398 (≈1.40)** | Bob's 1.4 target; exact-1.400 would need an off-vocabulary ×1.2121 — confirm 1.398 is acceptable |

All other 179 profiles unchanged. Every Mobile/Embarked-bay profile already sits at 1.000 —
no removals needed to conform to ruling 1.

### 3. Template fix (pending Bob's explicit yes — flagged in survey §3, not in the accepted list)

- [x] `US_ARMOR_BRIGADE` experience `Trained` → `Experienced` (every comparable US template is
      Experienced/Veteran; looks like a battle-group re-cut oversight and works against the
      "powerful US formation" intent).

### 4. Docs kept in sync (same commit)

- [x] `WeaponTrait_Supplement.md`: add T-rows for the three new traits; record the closed-bay
      reinterpretation + the three rulings above as a dated amendment to §15.
- [x] HS_DesignDoc: amend the ICM section (7.5.5.2 area) with the closed-bay doctrine (per the
      record-design-decisions rule).
- [x] `Claude_Project.md`: fix the two stale claims found in the survey (§8 "ICM 0.5–2.0" → code
      0.1–10.0; §6 prestige constants Gen4=150/TANK=55 → 180/65) + note this pass; bump the
      reconcile stamp.

### 5. Tests

- [x] Pin the five new ICM totals in the profile suites (the NATO suite already asserts `.ICM`
      for M3/AH-64 — same idiom; find the suite that guards the NATO tank batch and extend it).
- [ ] **Bob runs Unity Test Runner** (agent cannot): full EditorTests incl. `CombatOracleTests`
      drift guards (combat-constant change) + the per-faction profile suites.

### 5b. Recon ruling implementation (ratified addendum 4)

- [x] Delete the five `SetTargetClass(TargetClass.Hard)` calls (WeaponProfileDB lines ~1031,
      ~4131, ~4172, ~4215, ~4256) — all six recon profiles fall to the Soft prefix default.
- [x] Remove `WeaponTrait.RECON_FRAGILE` from `RCN_BRDM2_SV`'s trait list; mark T85a DORMANT in
      `WeaponTraitCatalog` + the supplement (do NOT delete the enum member — append-only habit).
- [x] Check for tests asserting recon TargetClass or the BRDM-2's 0.6 ICM ("M3 not fragile"
      assertion stays true); pin the new expectations (all recon ICM 1.00, class Soft).
- [x] Design-doc: amend §7.4.1.2 with the recon-class ruling (scouts fight as Soft targets —
      skirmisher doctrine) and note the R6 penalty's retirement.

### 5c. Artillery rulings implementation (addendum 2, rulings 5–11)

- [x] `ROC_PHZ89.SetPrestigeCost(Gen1, ROC)`; `SPA_TYPE82.SetPrestigeCost(Gen2, SPA)`.
- [x] Add `WeaponTrait.SMART_MUNITION` to `SPA_M109_US`'s trait list (US variant only).
- [x] `ART_HEAVY_CH` delta `INDIRECT_RANGE_LONG` → `INDIRECT_RANGE_MEDIUM`.
- [x] New trait `FIRE_DIRECTION_NET` (enum append + catalog def, FireControlOptics, Icm 1.05);
      add to `SPA_M109_US`, `SPA_M109_GE`, `SPA_M109_FR`, `SPA_M109_UK`, `ROC_MLRS_US`.
- [x] Reclassify the five SP-gun templates ART → SPA in CombatUnitDB (ruling 10); verify §12.3
      ART/SPA spotting-table parity first.
- [x] `ART_MORTAR_MJ` / `ART_LIGHT_MJ` → `SetPrestigeCost(Gen1, ART)` (ruling 11, on Bob's nod).
- [x] Tests: `WeaponProfileNatoTests` M109 row becomes (8,7,11,7,7,0) + ICM 1.05 assertions for
      the five FIRE_DIRECTION carriers; check the Chinese suite for ART_HEAVY_CH/PHZ-89
      assertions; pin the two MJ prestige values if the suite pins prestige anywhere.
- [x] Supplement: T18b carrier list gains the shipped M109_US; new T-row for FIRE_DIRECTION_NET;
      note rulings 5–11 with date.

### 6. Optional cleanup (cheap, same pass)

- [x] Delete the ~30 vestigial empty `// Set the ICM` comments in `CombatUnitDB.cs` (relics of
      the retired per-template model; ruling 1 makes them permanently dead).

## Explicitly NOT in this session (parked)

- **Q2/Q3 — NATO late roster** (M1A1 profile etc.). ⚠ Restated honestly: the ICM edits above do
  NOT flip the §3 benchmark — M1 at 1.534 + Experienced still loses ~3.9 : 7.0 to a T-80U
  regiment (band Δ dominates; the M1-105 is Grim vs every Soviet Gen3+). The edits DO make the
  US TF decisively dominant vs Gen2-and-below. "US formation beats even T-80U/BVM" remains a
  roster/statline decision, not an ICM one.
- **Prestige pass** (survey Q4–Q6: whole-unit price, kill-prestige basis, tier anomalies —
  including the ACR's absurd 65-prestige tag, noted for that pass).
- The other-category ICM outliers below (each needs its own ruling first).

## Other-category ICM outliers (surveyed 2026-08-22, rulings wanted — not yet changes)

1. **Recon — RATIFIED 2026-08-22** (skirmisher doctrine: all six Soft, RECON_FRAGILE retired).
   The full duel analysis lives in the session transcript; implementation is §5b above. The
   deep finding for the record: five of six scouts carried `SetTargetClass(Hard)`, routing all
   incoming fire to HD 5 and nullifying the archetype's HD5/SD9 soak-and-withdraw design; the
   lone Soft BRDM-2 was simultaneously the most survivable scout AND the only RECON_FRAGILE
   carrier.
2. **SP artillery — zero quality differentiation anywhere.** All 14 closed-bay artillery
   profiles (2S1→2S19, M109 ×4, MLRS, PHZ-89, Type 82) sit at exactly 1.00; no fire-direction
   quality lane exists at all. If the NATO C3 edge extends to fires (TACFIRE vs Soviet massed
   fire), a `FIRE_DIRECTION_NET` QUALITY_S (×1.05) on US/GE/UK M109 + MLRS is the natural move;
   ICM applies to indirect lanes (§7.13) so it works mechanically. Counter-case: the Soviet
   artillery arm was genuinely excellent — parity may be the intended statement. Ruling wanted.
3. **Export/"monkey-model" coverage is tanks-only.** Iraqi T-55/62 get EXPORT_DOWNGRADE (0.90),
   but Iraqi 2K12/2S1/ZSU-57/MiG-21/23/Su-17 are at 1.00 — Soviet-grade formation quality under
   the closed-bay lens (the Iraqi 2K12 resolves IDENTICAL to the Soviet one and is also cheaper
   in prestige). Iranian F-4/F-14 use bespoke stat deltas instead of the trait. Either extend
   the trait idiom to the rest of the export kit, or rule "Green/Raw experience already covers
   Arab quality" and accept tanks as the lone double-dip (0.9 ICM × 0.9 XP).
4. **`LOOKDOWN_SHOOTDOWN` live-vs-dormant drift.** The supplement lists T65 as DORMANT, but the
   code resolves its ×1.10 LIVE — which quietly tilted the RATIFIED "MiG-29 vs F-15 even"
   example ~10% toward the F-15/F-14/F-16/MiG-31/Su-27 carriers. Historically defensible;
   just needs a decision: re-ratify as live (update supplement) or park it dormant again.
5. **Helicopters — consistent, recommend leave.** Apex parity Mi-28 = AH-64 = 1.05 (both FNF);
   everything else 1.00. The only open question is whether US Army Aviation merits a formation
   bump under the closed-bay lens; the air-parity spirit says no.
6. **Air defense — consistent, no action.** Quality differentiation already lives in the GAT
   stat via guidance traits; Soviet AD superiority (the stated flavor) is carried there.

## Artillery survey (2026-08-22, Bob asked "any obvious concerns?") — corrected range data

The delta extraction was re-run resolving `GameData.*` constants (the first pass silently
dropped them — tank/recon/ICM analyses were unaffected, but every IR value was missing).
The corrected indirect-range ladder (hexes; 1 hex = 5 km):
mortars 3 · light towed 4 · heavy towed 5 · 2S1 4 · 2S3/M109/2S19/Type 82 5 · 2S5 6 ·
BM-21 4 · BM-27/MLRS/PHZ-89 6 · BM-30/Scud 10.

### ⚠⚠ THE BIG FIND (adjacent, not artillery): the authored AD range ladder is DEAD DATA

Every SAM/SPAAA/AAA profile encodes its engagement envelope as an **IR delta** (ZSU 3 ·
Gepard/Strela-1 4 · S-125 5 · 2K12/2K22? 5–6 · Hawk/Roland/Crotale/Rapier/HQ-7 6 · S-300 10 —
exactly the supplement T71 "IR high" idiom), but **no AD profile sets PR** (archetype default 1),
and `SpottingService.FindTransitAirDefense` (§11.8, the ONLY consumer of AD reach) reads
`ActivePrimaryRange`. `ActiveIndirectRange`'s only consumers are class-gated to ART/SPA/ROC/BM.
Net: **every air-defence battery in the game interdicts aircraft at range 1** — an S-300 has the
same umbrella as a ZSU-57 — and the authored ladder is read by nothing. Never surfaced in play
because Khost's AD is MJ Stinger/AAA teams engaging helos at short range anyway. Fix direction
(Bob to ratify): for AD classes the scanner should read the IR envelope (e.g.
`max(ActiveIndirectRange, ActivePrimaryRange)` or IR-with-PR-fallback), + `AirDefenseTransitTests`
regression coverage at real ranges. Separate work item — not part of the ICM pass.

### Artillery-specific concerns (ALL RULED 2026-08-22 → addendum 2, rulings 5–11; kept for rationale)

1. **PHZ-89 is mispriced ~two tiers:** BM-21-class stats (HA5 SA9) at BM-30 money (295) with
   BM-27 range (IR6) — strictly worse than the BM-27 (235) at +60. Same China-pricing noise as
   the tanks, opposite direction (SPA_TYPE82 likewise: 2S3-class stats at 250 vs the 2S3's 190).
2. **M109 lacks its precision round:** supplement T18b lists Copperhead (M109) as a
   SMART_MUNITION carrier, but no M109 variant has the trait (SA+1 delta only) while
   2S19/BM-27/BM-30/MLRS all carry it. Adding it (HA 5→8) restores the historical bite and
   serves the NATO-quality doctrine. Recommend: add to SPA_M109_US (all four variants? or US
   only — Copperhead was US-fielded).
3. **2S19 vs 2S5 range inversion:** the flagship 2S19 (310, SMART, HA8) has IR5 vs the cheaper
   2S5's IR6. Historically defensible (Giatsint is the range king) — confirm deliberate, since
   as an upgrade ladder the top rung loses a hex of reach.
4. **ART_HEAVY_CH IR6:** Chinese heavy towed outranges every other nation's heavy towed (IR5)
   and China's own SPA (IR5). Flavor or drift?
5. **ICM flat 1.00 across all 24 artillery-family profiles** — the fire-direction question
   (outlier list item 2). Under the closed-bay ruling only the SP guns/MRLs are eligible
   (towed = truck-paired). If NATO fires-quality is ratified: `FIRE_DIRECTION_NET` ×1.05 on
   M109 ×4 + MLRS.
6. **Classification drift, cosmetic today:** US/GE/UK/FR/IQ SP-gun templates are classified
   ART; only USSR uses SPA (fire routing is identical — both in `IsIndirectFireClass` — but
   worth normalizing before anything else keys on the split).
7. Already logged: MJ mortar/light-artillery prestige 0 (V19 zero-bounty); GAD ladder
   towed 8 / SP 7 / truck-ROC 6 is correct and good; Scud IR10 = BM-30 reach at 450 is
   consistent with §7A.11 CB-safety in practice (only 10-reach systems can answer it).

## Review (implementation 2026-08-22 — same session)

**All rulings 1–11 implemented.** Verified by re-running the scripted extraction + resolver replica
over the edited DB (the same pipeline the survey validated against WeaponProfileNatoTests): every
target value resolves exactly — M1 1.534 · Leo 2 1.467 · Leo 1 GE 1.213 · Challenger 1.334 ·
ACR 1.398 (→1.40) · all six recon ICM 1.000 · M109_US HA8/SA11/ICM 1.05 · M109 GE/FR/UK + MLRS
ICM 1.05 · PHZ-89 175 · Type 82 190 · ART_HEAVY_CH IR5 · MJ arty 90/90 (zero-cost list is now the
three facilities only). Parse-clean: 184 profiles / 185 templates / no unmatched ctors.

Files touched:
- `WeaponTrait.cs` — 4 appended members (§13 formation quality).
- `WeaponTraitCatalog.cs` — 4 new TraitDefs; RECON_FRAGILE → Dormant with retirement note.
- `WeaponProfileDB.cs` — 5 tank profiles gained formation traits (header comments updated);
  BRDM-2 lost RECON_FRAGILE; all 5 `SetTargetClass(Hard)` calls deleted; M109_US +SMART_MUNITION
  +FIRE_DIRECTION_NET; M109 GE/FR/UK + MLRS +FIRE_DIRECTION_NET; PHZ-89/Type 82 re-tiered;
  ART_HEAVY_CH IR LONG→MEDIUM; MJ mortar/light-arty gained SetPrestigeCost(Gen1, ART).
- `CombatUnitDB.cs` — US_ARMOR_BRIGADE → Experienced; 5 SP-gun templates ART→SPA (verified
  behavior-identical: same §12.3 spotting rows, same action-economy arm, census coherence maps
  both to EquipmentBucket.ART); 156 vestigial `// Set the ICM` comment blocks stripped.
- Tests — `WeaponProfileClassTests` recon-override test → all-Soft test;
  `WeaponProfileNatoTests` recon class asserts → Soft, M109_US row → (8,7,11,7,7,0), ICM pins
  added for the five FIRE_DIRECTION carriers, NEW `FormationQuality_TankIcmTotals` pins the five
  formation totals + the Leo 1 NL anchor; `WeaponProfileSovietTests` BRDM-2 → ICM 1.00 + Soft,
  BRDM-2 AT → Soft. Chinese/Arab suites untouched (no assertions on the changed values).
- Docs — WeaponTrait_Supplement (T85a dormant; new §13b T87–T90; §15 decisions 5–7);
  HS_DesignDoc (§7.4.1.2, §7.5.5, §7.5.5.2, §7A.5 amended; one stale pre-migration SetICM factory
  step fixed); Claude_Project.md (new reconcile entry; stale ICM-range and prestige-constant
  claims fixed).

**Outstanding:** Bob runs Unity Test Runner (full EditorTests incl. CombatOracle drift guards).
The AD range defect is spun off (chip: "Fix §11.8 AD engagement range reading PR not IR").
No SAVE_VERSION bump needed — nothing persisted changed shape (ICM/TargetClass are derived at DB
build; template classification only seeds new units).
