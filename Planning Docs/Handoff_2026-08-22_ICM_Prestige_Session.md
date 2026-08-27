# Session Handoff — 2026-08-22 (ICM pass + Prestige pass, both CLOSED)

For the next agent. This session ran two full Bob-ratified balance passes to green commits, found
one live defect (spun off, likely YOUR first task), and left three named open threads. Read this,
then `Claude_TODO.md`, before touching anything.

---

## 1. What happened (two commits, both suites green via Bob-run Test Runner)

**`d6734f3` — THE ICM PASS.** Ratified the **closed-bay ICM doctrine**: on a unit's SOLE profile
(all other bays closed) the ICM prices the WHOLE FORMATION, not the platform; paired
(Mobile/Embarked-bay) profiles stay 1.0. Shipped: four new `WeaponTrait` members
(`COMBINED_ARMS_TF` ×1.15 → M1 1.53 · `NATO_FIRST_LINE` ×1.10 → Leo 2 1.47, Leo 1 GE 1.21,
Challenger 1.33 · `AIR_CAVALRY` ×1.21 → M60A3 ACR 1.40 · `FIRE_DIRECTION_NET` ×1.05 → M109 ×4 +
MLRS); the **recon ruling** (all six recon profiles fight as SOFT — the five `SetTargetClass(Hard)`
overrides are DELETED, so the archetype's HD5/SD9 soak-and-withdraw design finally functions;
`RECON_FRAGILE` parked DORMANT; scouts are "skirmishers with staying power", M3 included);
artillery rulings (US M109 +SMART_MUNITION → HA8/SA11; PHZ-89 → 175, Type 82 → 190;
ART_HEAVY_CH IR 6→5; MJ mortar/light-arty gained their MISSING prestige, 90 each); five SP-gun
templates ART→SPA (verified behavior-identical); US_ARMOR_BRIGADE Trained→Experienced; ~156
vestigial `// Set the ICM` comments stripped. Full record: `todo_icm.md` (rulings 1–11 + review).

**`14fa5d5` — THE PRESTIGE PASS.** Soviet-menu re-tiers (BTR-70 → 40, BTR-80 → 100 — the BTR line
was dead stock above the BMPs; BRDM-2 AT → 105; Su-24 → 300, BMB type cost; T-80B → 125) +
M60A3 ACR → explicit 250 (`GameData.PRESTIGE_ACR_SQUADRON`). **NEW `CombatUnit.PurchaseCost`**
(Σ populated bays, `[JsonIgnore]`, naval transient excluded) = the ratified §18.3.1 whole-unit
price, COMPOSABLE (buy bare, add bays later at profile price) — and now the **V19 kill-bounty
basis** (all three `PrestigeOnKill` sites; bounty no longer moves with posture; test-pinned).
§18.5.1 upgrade formula ratified: `max(target − current, PRESTIGE_UPGRADE_MIN 20)` × 0.7 with
Connections At The Top (constants in GameData). §18.5.2: same-price turn-gated successors
(T-80U→T-80BVM, Su-27→Su-47) are deliberate; upgrading into them costs the floor. HS_DesignDoc
§18 fully reconciled to code (the stale pre-2026-06-18 price table replaced; WW type + air-tier
shortcuts formally retired; 18.6/18.7 marked REP-not-prestige; 18.4.1 re-based). Income side
deliberately untouched — it is live and grounded. Full record: §7 of
`Prestige_SovietEconomy_Analysis_2026-08-22.md`.

**Analysis artifacts in repo root** (keep — they are the audit trail):
`WeaponProfile_ICM_Prestige_Survey_2026-08-22.md` (all 184 profiles resolved + duel math),
`todo_icm.md`, `Prestige_SovietEconomy_Analysis_2026-08-22.md`,
`Handoff_2026-08-22_ICM_Prestige_Session.md` (this file).

---

## 2. ⚠ YOUR LIKELY FIRST TASK — the §11.8 AD engagement-range DEFECT (Bob wants its own session)

**Confirmed live defect, found during the artillery audit.** Every SAM/SPAAA/AAA profile authors
its air-defense engagement envelope as a `ProfileStat.IR` delta in WeaponProfileDB (ZSU-57/23 = 3 ·
Gepard/Strela-1 = 4 · S-125 = 5 · 2K12/2K22/Hawk/Roland/Crotale/Rapier/HQ-7 = 5–6 · S-300 = 10 —
the WeaponTrait_Supplement T71 "IR high" idiom). But **no AD profile sets PR** (the Sam/Aaa
archetype default is PR 1), and `SpottingService.FindTransitAirDefense` (~line 525) — the ONLY
consumer of AD reach for §11.8 transit opportunity fire — reads **`ActivePrimaryRange`** with a
`≤0 → 2` fallback. `ActiveIndirectRange`'s only consumers (`CombatResolver.IsInIndirectRange` /
`IsCounterBatteryEligible`) are class-gated to ART/SPA/ROC/BM. **Net: every AD battery in the game
interdicts aircraft at range 1; the entire authored range ladder is dead data. An S-300 has a
ZSU-57's umbrella.** Never surfaced in play because Khost's AD is short-range MJ Stinger/AAA.

**Recommended fix (get Bob's ratification on direction first — standard workflow):** in
`FindTransitAirDefense`, for AD classifications read the IR envelope — e.g.
`Mathf.FloorToInt(Mathf.Max(enemy.ActiveIndirectRange, enemy.ActivePrimaryRange))`, keeping the
fallback. Alternative (bigger): move the envelopes from IR deltas to PR deltas in the DB (touches
the supplement idiom + ~22 profiles). Then extend `AirDefenseTransitTests` with real-range
regression (S-300 engages at 10, ZSU refuses beyond 3, the §11.8.6 one-shot-per-aircraft record
holds across a long transit through a wide envelope). ⚠ Balance note for Bob: wide envelopes make
fixed-wing transit across SAM belts far more dangerous — Khost unaffected, future NATO scenarios
change materially. ⚠ Do NOT re-price AD around the bug — prestige was deliberately tuned against
the *intended* envelopes. A pending task chip exists: "Fix §11.8 AD engagement range reading PR
not IR" (full brief in `todo_icm.md`, "THE BIG FIND" section).

---

## 3. Other open threads (in rough priority order)

1. **P4 requisition (`todo_profiles.md` §4.6/§4.7)** — now FULLY SPECIFIED and unblocked. The
   §4.7 header carries the ratified pricing rules (Σ-bays composable purchase, replacement basis,
   upgrade formula with `PRESTIGE_UPGRADE_MIN`); `CombatUnit.PurchaseCost` and the wallet
   (`SpendPrestige`, currently zero callers) are waiting. Build `RequisitionService` against
   §18.3.1/§18.5.1 — do not re-derive prices.
2. **NATO late roster (survey Q2/Q3 — NEVER RULED).** Even after the formation traits, the
   M1-105 battle group loses ~3.9 : 7.0 in expected HP to a T-80U regiment (band Δ dominates;
   ICM cannot fix a Δ−5 vs +4 matchup). The supplement §16 designed and validated an **M1A1
   (Gen3 + GUN_120_SMOOTH + APFSDS_ADVANCED + COMPOSITE_DU → HA14 HD14 ICM 1.33) that never
   entered the DB.** "A US armored formation is a powerful force" is a ROSTER decision
   (M1A1/M1A1HA, late Leo 2, up-gunned Challenger), not more ICM. Needs Bob's calibration ruling:
   beat the T-80U/BVM apex, or parity-at-apex + dominance-vs-T-72B-peers.
3. **Two ICM outliers presented but never ruled** (`todo_icm.md` outliers 3–4): (a) the
   export/"monkey-model" idiom is tanks-only — Iraqi 2K12/2S1/ZSU/jets sit at Soviet-grade 1.00,
   Iranian jets use bespoke stat deltas instead of the trait; either extend the idiom or rule
   "Green/Raw experience covers it"; (b) `LOOKDOWN_SHOOTDOWN` resolves LIVE in code while the
   supplement lists T65 DORMANT — this quietly tilted the ratified "MiG-29 vs F-15 even" example
   ~10% toward the F-15/F-14/F-16/MiG-31/Su-27 carriers; re-ratify or park.
4. **Watch items (deliberately left):** tube-vs-rocket pricing (BM-21's double-fire vs 2S3 —
   the supply pass will show whether the 1-supply-per-shot bite balances it); non-Soviet tier
   noise (AMX-30/Leo1/M60 tiers, Warrior/Humvee, Iranian 2K12 at 135) — bounty-grade only, leave
   until an expansion makes them player-facing; 2S19 IR5 < 2S5 IR6 range inversion — ratified
   deliberate, revisit only on player complaints.

---

## 4. Facts the next agent should not have to rediscover

- **ICM mechanics:** `storedICM = Π(live IcmMultiply trait effects)` via `TraitResolver`, clamp
  0.1–10.0, aggregate guideline ≲1.6 (current max: M1 at 1.534). NOTHING hand-sets ICM — no
  `SetICM` call exists in the DB; to change an ICM you add/remove catalog traits.
- **Combat math for duel analysis:** Δ = attack − defense → band (3-Δ wide; E[HP]: Grim 2.0 ·
  Disadv 2.5 · Even 3.5 · Favorable 5.5 · Advantaged 7.5); damage × (1.15 full-strength × XP ×
  eff × ICM); balance mods 1.0; the TARGET's class picks the axis (recon = Soft since this
  session, defended by SD 9). AI units never have leaders → no Command Mitigation ever.
- **Prestige formula:** per-profile `cost = PrestigeTierCost(0/60/120/180) +
  PrestigeTypeCost(GameData enum)`, explicit exceptions ONLY in GameData's "Prestige Exceptions"
  region (CRUISE_BOMBER 400, ACR_SQUADRON 250). Whole-unit = `CombatUnit.PurchaseCost`.
- **Khost economy snapshot:** pool 500 · stipend 20 · rate 0.05 over 1,550 map value · progress
  0.5 · early ×1.25 · 21 turns → ~2,000–2,300/battle well-played; stipend floor sustains ~1
  replacement per 2 turns. Income is LIVE (`BattleManager.ComputeIncome`); nothing spends yet.
- **Verification method used all session** (scratchpad scripts are gone with the session, but
  the method is 30 minutes to rebuild): regex-extract `FromProfileDef` blocks + `SetPrestigeCost`
  from WeaponProfileDB.cs (⚠ deltas use `GameData.*` CONSTANTS — resolve them, don't match bare
  ints; that bug cost this session a false alarm), replicate `TraitResolver` (base + deltas +
  live trait effects + floors + band clamp [1,25]), then diff against the per-faction test
  suites' asserted lines. It matched the suites exactly and caught every edit.
- **Doc-sync obligations that bit this session:** the supplement + HS_DesignDoc live OUTSIDE the
  repo (`Desktop\AI_TODO\Design Docs\`) and drift — §18.3's table was a year stale, §7.4.1.2
  described deleted overrides, LOOKDOWN live-vs-dormant still drifts. When you land a ruling,
  amend the doc section IN THE SAME PASS, and update `Claude_Project.md`'s reconcile entry.
- **Process:** Bob runs Unity Test Runner (agent cannot) — ask and WAIT before declaring green;
  plan → check in → implement (CLAUDE.md workflow); Bob ratifies design rulings in chat and
  they get recorded in HS_DesignDoc + the pass's todo/analysis doc; commits end with the
  Co-Authored-By trailer; leave Bob's pre-existing working-tree items alone
  (`Claude_TODO.md`/`Claude_AI_TODO.md` edits, `todo_missionpack.md`, `_to_delete/`,
  `Reply_C7_V19_2026-08-20.md` were all untouched-by-agent today and still dirty).

## 5. State of the working tree at handoff

Committed through `14fa5d5` on `main`. Agent-created files all committed. Bob's own uncommitted
items (listed above) remain as he left them. No SAVE_VERSION changes today — both passes were
DB-build data + derived properties only.
