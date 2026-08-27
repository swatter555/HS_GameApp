# WeaponProfile ICM + Prestige Survey — 2026-08-22

Survey of all **184 WeaponProfileDB profiles** and **185 CombatUnitDB templates**, resolved
statlines + ICMs + prestige costs, in preparation for the ICM pass (first) and the prestige/purchase
pass (second). Extraction was scripted against the live DB source and the resolver was replicated
exactly (archetype + deltas + live trait effects + floors + clamps); the output was validated against
`WeaponProfileNatoTests` — every asserted statline and both asserted ICMs match. Full tables at the
end of this doc's companion TSVs (scratchpad) — the load-bearing rows are inlined below.

**Bob's two premises, verified against code:**
- ✅ **ICM lives on the WeaponProfile.** `WeaponProfile.ICM`, set ONLY by `TraitResolver` as the
  product of live ICM-quality trait multipliers (default 1.0, clamp `ICM_MIN 0.1`–`ICM_MAX 10.0`).
  There is no `SetICM` call anywhere in WeaponProfileDB — no hand-set values exist. CombatUnitDB
  still carries vestigial empty `// Set the ICM` comments from the pre-migration model (~30 sites,
  cleanup candidate).
- ✅ **NATO re-cut to battle groups.** `US_ARMOR_BRIGADE` is now "US Armor Battle Group" — census
  = a battalion task force (58 M1 + 13 M2 + 6 M3 + 16 TOW + 18 M109 + Stinger/Vulcan slices,
  1,000 men) per the 2026-08-13 census pass (§1a: one counter = one maneuver formation).
  ⚠ The census was re-cut; **the stat line was not** — `TANK_M1_US` still resolves as a pure
  M1-with-105mm platform line. That gap is the root of the problem this pass is about (see §3).

⚠ Stale doc claims found in passing: `Claude_Project.md` §8 says "ICM 0.5–2.0" (code: 0.1–10.0)
and §6 lists the pre-2026-06-18 prestige constants (Gen4=150, TANK=55; code: Gen4=180, TANK=65).
Reconcile with this pass.

---

## 1. How ICM works today (mechanics)

- `finalStat = clamp(archetype + Σdeltas + Σ(live trait deltas), 1, 25)`;
  `storedICM = Π(live IcmMultiply traits)`, default 1.0. (Appendix W §1, `TraitResolver`.)
- ICM enters combat once, in the lane quality product (engine step 4):
  `quality = Experience × Strength × Efficiency × ICM`. It **multiplies damage**; it never moves
  the band Δ. The band ladder is the big lever (bands are 3-Δ wide; Even→Favorable roughly +57%
  expected damage); ICM ×1.10 ≈ +10%.
- **Ratified 2026-06-13 (WeaponTrait_Supplement §15.1, Bob):** trait-ICM *replaced* the old
  hand-set per-template ICM **entirely** — both the doctrinal value (NATO 1.2) and the size
  baselines (0.75/1.25) were retired. NATO's edge is deliberately the *itemized* fire-control
  stack (§13: "no hand-tuned per-profile bumps"). Guideline: aggregate ICM ≲ 1.6.
- `CONSCRIPT_CREW` was **dropped** in the same ruling — crew quality is Experience's job.

### Current ICM census (184 profiles)

| ICM | Count | Who |
|---|---|---|
| 0.60 | 1 | BRDM-2 (`RECON_FRAGILE` — the only carrier in the DB) |
| 0.90 | 2 | Iraqi T-55A / T-62A (`EXPORT_DOWNGRADE`) |
| 1.00 | 144 | everything else — incl. **every Soviet tank below T-64B**, all artillery, all AD, all APC/IFV |
| 1.05 | 15 | T-72A/T-72B, M60A3(IR), Type 80, Mi-28, AH-64, Stinger-carrying infantry (US/GE/FR/NL) |
| 1.10 | 17 | T-64B/T-80B/T-80U, Leo 1 ×4, Type 95, Spetsnaz, MJ Elite, MiG-31/Su-27/Su-47, F-15/F-16/F-14, Mirage 2000 |
| 1.155 | 1 | M60A3 (US — LRF + thermal) |
| 1.21 | 2 | T-80BVM, Challenger 1 |
| 1.33 | 2 | **M1 Abrams, Leopard 2** — the intended NATO apex stack |

Reading: the fire-control story the supplement designed **is in place and working** — NATO Gen3
armor carries ~1.33 vs Soviet Gen3's ~1.05–1.10, exactly the ~20% gap §13 called for. The air
side is deliberately at parity (supplement §16 ratified "MiG-29 vs F-15 even" as the stated
air-superiority exception; late fighters on both sides carry the same 1.10 look-down ICM).

---

## 2. Combat-power survey — the shape of the roster

Resolved key lines (full table in `resolved_profiles.tsv`; band engine: Δ→band, E[HP] per band:
Grim 2.0 · Disadv 2.5 · Even 3.5 · Favorable 5.5 · Advantaged 7.5 · Strong 9.5):

| Profile | HA | HD | SA | SD | ICM | Prestige | Note |
|---|---|---|---|---|---|---|---|
| T-55A | 7 | 6 | 5 | 7 | 1.00 | 65 | |
| T-64A | 12 | 9 | 7 | 7 | 1.00 | 125 | |
| T-72B | 15 | 15 | 9 | 7 | 1.05 | 185 | |
| T-64B | 15 | 14 | 9 | 7 | 1.10 | 185 | |
| T-80B | 13 | 10 | 7 | 6 | 1.10 | 185 | Gen2 chassis + Gen3 traits, priced Gen3 |
| **T-80U** | **17** | **15** | 9 | 7 | **1.10** | **245** | near-Gen4 by traits |
| **T-80BVM** | **20** | **20** | 10 | 7 | **1.21** | **245** | Gen4 + Relikt + APS; **same price as T-80U** |
| M60A3 | 10 | 8 | 8 | 6 | 1.155 | **65** | Gen2 archetype at a Gen1 price |
| **M1 (105)** | **10** | **13** | 8 | 6 | **1.33** | 185 | the only Abrams in the DB |
| Leopard 2 | 14 | 12 | 8 | 6 | 1.33 | 185 | |
| Challenger 1 | 13 | 14 | 8 | 6 | 1.21 | 185 | |
| AMX-30 | 10 | 7 | 8 | 7 | 1.00 | 125 | France = division-as-counter (80 tanks) |
| Type 80 / 95 | 10/13 | 8/11 | 7/9 | 6/6 | 1.05/1.10 | **65/125** | each priced one tier below its archetype |

The NATO roster **tops out at Gen3** (M1-105, Leo 2, Challenger 1). The Soviet roster runs to
Gen4 (T-80U at Gen3+heavy-stack, T-80BVM at true Gen4) — correct for a Soviet-campaign upgrade
tree, but it means late-campaign NATO opposition has no answer profile: the supplement's worked
M1A1 (120) — Gen3 + `GUN_120_SMOOTH` + `APFSDS_ADVANCED` + `COMPOSITE_DU` → HA14 HD14 ICM 1.33 —
**was designed and validated in §16 but never entered the DB.**

---

## 3. The benchmark duel — where Bob's intent and the current numbers disagree

> "While a T-80U might be better than a first gen Abrams, a T-80 regiment would still be
> inferior to a US Armored Brigade."

Exact expected values, clear terrain, both Mobile, full strength (×1.15), Full efficiency,
no leaders (US battle groups are AI — **the AI never has leaders, so no Command Mitigation ever
applies to it**), `GROUND_BALANCE_MOD` 1.0:

| Lane | Δ | Band | E[base] | Quality | E[HP dealt] |
|---|---|---|---|---|---|
| T-80U rgt → M1 battle group | 17−13 = **+4** | Favorable | 5.5 | 1.15 × 1.0 × 1.103 = 1.268 | **≈ 7.0** |
| M1 battle group → T-80U rgt | 10−15 = **−5** | Grim | 2.0 | 1.15 × 1.0 × 1.334 = 1.534 | **≈ 3.1** |

The T-80U regiment wins the exchange better than 2:1, either side attacking. Raising the US
template to Veteran only lifts 3.1 → 3.7. **The current state is the exact opposite of the
stated intent.**

⚠ Also: `US_ARMOR_BRIGADE` is **Trained** while nearly every other US template is
Experienced/Veteran — looks like an oversight from the battle-group re-cut, and it works
directly against the intent. One-line fix regardless of everything else.

### Why ICM alone cannot deliver this

ICM is a damage multiplier; the band Δ is where the 2:1 comes from. For the M1-105 group to
merely *match* the T-80U exchange it would need ICM ≈ **3.0** — double the ICM_MAX-adjacent
guideline (≲1.6) and triple any historical-quality story. No defensible ICM flips a Δ −5 vs +4
matchup. The intent needs the **stat line** to carry most of it, with ICM as the texture on top:

| Package (US side) | US → T-80U | T-80U → US | Verdict |
|---|---|---|---|
| Today: M1-105, ICM 1.33, Trained | 3.1 | 7.0 | Soviets dominant |
| + M1A1 (supplement §16: HA14 HD14) | 5.4 | 7.0 | still behind |
| + formation ICM ×1.15 (→1.53) | 6.2 | 7.0 | close |
| + Experienced (the US norm) | **6.8** | **7.0** | parity |
| + Veteran instead | **7.4** | 7.0 | US edge |

So the honest calibration menu against the **apex** Soviet tank (a 1985 T-80U — the player's
late-campaign 245-point prestige sink) is: M1A1 profile + formation-quality ICM + the US
experience norm ≈ **parity to a slight US edge**, while the same package vs the T-72B/T-80B
peer group is decisively US-dominant (e.g. M1A1 → T-80B: Δ +4 Favorable, 5.5 × 1.76 ≈ 9.7 vs
return ≈ 5.5×1.27−… ≈ 4.4 — better than 2:1 US). That matches the supplement's own ratified
duels ("M1A1 vs T-80B: NATO dominant"; "the under-gunned 105 loses to a good Soviet Gen3 —
the historical M1-needed-the-120").

**Decision needed (Q2 below):** is "inferior to a US Armored formation" meant to hold even
against the T-80U/BVM apex (then NATO needs Gen4 answers — M1A1HA etc. — and/or bigger
formation deltas), or does parity-at-apex + dominance-at-peer satisfy the intent?

### The three-layer quality model (where each factor belongs)

The architecture already separates three things; the ICM pass should respect the split:

1. **Hardware quality** → trait stat-deltas + fire-control ICM traits, on the profile. Built,
   working, ratified.
2. **Crew/training quality** → Experience on the template (`CONSCRIPT_CREW` was dropped for
   exactly this). Partially applied: US/UK/GE mostly Experienced-Veteran, USSR Trained,
   IQ/IR Green/Raw — but with holes (US_ARMOR_BRIGADE Trained).
3. **Formation/organizational quality** — combined-arms integration, C3, cross-attachment,
   Air-Land Battle — **currently modeled nowhere**. This is the layer Bob's T-80-vs-brigade
   instinct lives in, and it became real the day the census pass made one US counter a
   battalion TF while a Soviet counter stayed a regiment.

### Options for layer 3 (the actual ICM-pass decision)

- **Option A — formation-quality ICM traits (recommended).** New Economy/quality-category
  traits, e.g. `COMBINED_ARMS_TF` (ICM ×1.15, QUALITY-M+) on the NATO battle-group deployed
  profiles (TANK_M1_US, INF_REG_US, the UK/GE first-line equivalents), possibly a smaller
  `NATO_C3` ×1.05 tier for the second line. Stays entirely inside the ratified §15.1 model
  (ICM = product of traits, itemized and visible), has a precedent (`SPECIAL_FORCES` is already
  a training-package ICM trait), and the census gives each carrier a factual justification.
  ⚠ Shared-profile leakage must be checked per trait: e.g. `SAM_HAWK_US` is used by US **and**
  GE/NL templates; `RCN_FV105_UK` by UK/NL/BE/DK; `FGT_F16_US` by US/NL; `SAM_S75_SV` by
  USSR **and** IQ/IR. National-quality traits can only go on nationally-owned profiles, or the
  profile gets cloned per operator (the DB already does this for M109/Leo1 variants).
- **Option B — hand-set per-profile ICM.** ⚠ Directly contradicts ratified §15.1 ("trait-ICM
  replaces the old per-template ICM entirely; no hand-tuned per-profile bumps"). Counter-argument
  before we go there: it double-counts with the existing fire-control stack, it's invisible at
  the itemization level the trait model was built for, and the magnitude required to matter
  (≥×1.5 on top of 1.33) blows the ≲1.6 aggregate guideline. And mechanically B is A with the
  label removed — if Bob wants bigger numbers, the clean vehicle is still a named trait. If we
  do overrule §15.1, it needs a design-doc amendment + a `ProfileDef` ICM-override field, not
  scattered `SetICM` calls.
- **Option C — experience defaults only.** Free (template edits), but caps at ×1.3, conflates
  crew skill with formation structure, and decays via the §18.4.3 replacement downgrade — a
  formation's structural quality shouldn't wash out because it took replacements.

Recommended package: **A + roster completion + template fixes** —
1. formation ICM trait(s) on NATO first-line battle-group profiles;
2. add the supplement-designed M1A1 (and decide Leo 2 late / Challenger up-gun) as
   `UpgradePath.TANK` successors so late-campaign NATO keeps pace with the player's Gen4s;
3. `US_ARMOR_BRIGADE` → Experienced (and audit the other battle-group templates' experience);
4. leave the air side alone (parity is ratified);
5. optional consistency sweep: `RECON_FRAGILE` is carried only by BRDM-2 — Luchs/FV105 are
   also pure scouts and per the catalog note should either carry it or the BRDM-2 shouldn't
   (the M3's omission is deliberate and test-pinned — it's an AT-recon variant).

---

## 4. Prestige survey (pass 2 — after the ICM ruling, since ICM feeds price-to-potential)

**Mechanics today:** `PrestigeCost = PrestigeTierCost(Gen1 0 · Gen2 60 · Gen3 120 · Gen4 180)
+ PrestigeTypeCost(INF 50 … TANK 65 … SPA 130 … ROC 175 … BMS/AWACS 450)`, per PROFILE, set at
DB build. One explicit exception exists (`PRESTIGE_CRUISE_BOMBER` 400, Tu-22M3). There is **no
whole-unit price anywhere** — nothing sums bays; P4 requisition (todo_profiles §4.6/§4.7) is
where that lands.

**The one live consumer is V19 kill-prestige:** `PrestigeOnKill = round(0.5 ×
killed.GetActiveWeaponProfile().PrestigeCost)` — the **active bay's** cost, in all three combat
actions. Consequences today:
- Killing a mounted MRR (BTR-80 active, 160) pays 80; the same regiment dug in on foot
  (INF_REG_SV, 50) pays 25. A VDV regiment caught embarked in its An-12 (150) pays 75.
- ⚠ **Zero-payout kills:** `ART_MORTAR_MJ` and `ART_LIGHT_MJ` have **no SetPrestigeCost at all**
  (cost 0), and the three `BASE_*` facility profiles are deliberately NONE — so killing MJ
  mortar/artillery units (live in Khost today) or any depot/airbase/HQ awards **zero** prestige.

### Whole-unit purchase question (the air-mobile example from Bob's brief)

If a new-unit purchase = **sum of its bays** (the natural reading of the three-bay upgrade
model), current numbers give:

| Template | Bays | Sum |
|---|---|---|
| USSR_TR_T80U | T-80U | **245** |
| USSR_TR_T80BV | T-80BVM | **245** |
| USSR_MRR_BMP2 | INF 50 + BMP-2 115 | 165 |
| USSR_AAR_BMD3 (air assault) | INF 50 + BMD-3 175 + Mi-8T 90 | **315** |
| USSR_VDV_BMD3 | INF 50 + BMD-3 175 + An-12 150 | **375** |
| US_ARMOR battle group | M1 | 185 |
| USSR_GRU (Spetsnaz) | 110 + Mi-8T 90 | 200 |

A VDV regiment costing 1.5× a T-80U regiment is defensible (elite + organic lift) or absurd
(paper infantry outprices the apex tank) depending on the ruling for lift: **does a purchased
unit pay full profile price for its Embarked bay, or is lift bought/priced separately** (it is
also the Panzer-General "buy the bay later" upgrade path — in which case the new-unit price is
arguably deployed-bay-only and bays are purchased as upgrades exactly as §3.2b describes)?
This decision also fixes the kill-prestige basis question above (active bay vs deployed bay vs
sum — today's active-bay reading means a unit's bounty changes with its posture).

Note `PRESTIGE_COST_MULT` (0.7, Connections At The Top) already discounts *upgrades* — the
leader-skill plumbing assumes per-profile prices continue to exist regardless of the whole-unit
ruling.

### Pricing anomalies to confirm-or-fix (tier+type audit)

1. **T-80U = T-80BVM = 245.** The BVM is strictly better (HA20/HD20 vs 17/15, ICM 1.21 vs 1.10,
   APS) at the same price — the Gen4 tier is the ceiling. Wants an explicit-cost exception
   (the documented pattern, like the Tu-22M3) or a new tier.
2. **Gen1-priced Gen2 tanks:** M60A3 (US) and all four Leo 1 variants sit at 65 — T-55A money —
   while AMX-30 (a *worse* line: HD7, ICM 1.00) costs 125. Either the M60/Leo1 tier is wrong or
   AMX-30's is (note AMX-30 is the France-as-division counter — if its price reflects counter
   scale, that's a third pricing dimension nothing else uses).
3. **Chinese tanks priced one tier under their archetypes** (Type 80: Gen2 stats at 65;
   Type 95: Gen3 stats at 125). Deliberate cheap-steel flavor or drift? Same question for
   `SPAAA_TYPE53` at 70 (AAA type-cost) when its statline is identical to the ZSU-57s priced 135.
4. **`SPSAM_2K12_IQ` = 135 vs Soviet `SPSAM_2K12_SV` = 235** for an *identical* resolved line
   (GAT15, no EXPORT_DOWNGRADE) — the Iraqi copy is also typed SPAAA and upgrade-pathed AAA.
   Inconsistent with how the Iraqi tanks were handled (same price, export-downgrade trait).
5. **BRDM-2 vs BRDM-2 AT, both 45:** the AT variant has HA6, ICM 1.0 (vs 0.6) and
   AIR_DROPPABLE — strictly dominant as a purchase at equal price.
6. **Warrior 175 vs Marder 115:** Warrior pays Gen3 money for HA5 (no ATGM) vs Marder's Gen2
   HA8. Historically defensible (no ATGM on Warrior) but as price-to-potential it's inverted.
7. **Humvee 100 vs M113 40:** the Humvee is combat-worse (THIN_TOP, GAD 6). If its price is
   "utility for AB/AM formations," fine — say so in a comment; otherwise re-tier.
8. **Zero-cost profiles:** ART_MORTAR_MJ, ART_LIGHT_MJ (missing SetPrestigeCost — looks like an
   authoring miss, not a decision) + the three facilities (deliberate NONE, but V19 makes that a
   gameplay fact: strongholds pay no bounty).
9. **BMD-2/3 priced = BMP-2/3** (115/175) with worse HD but AIR_DROPPABLE — probably fine
   (capability offsets armor), listed for completeness.

---

## 5. Proposed sequence

1. **Bob rules on the ICM questions (Q1–Q3 below).**
2. ICM pass lands as: new trait(s) in `WeaponTraitCatalog` + supplement/catalog `.md` update
   (keep-in-sync rule) + carrier assignments in WeaponProfileDB + template experience fixes +
   new NATO late profiles if ratified + test-suite updates (`WeaponProfileNatoTests` asserts
   resolved ICMs; `CombatOracleTests` drift guards must re-run after any combat-constant change).
3. **Then** the prestige pass: whole-unit price ruling → re-tier the anomaly list → V19 basis
   fix if ruled → (P4 requisition consumes the result).
4. Design-doc amendments per the record-design-decisions rule; reconcile `Claude_Project.md`
   (stale ICM range + prestige constants) in the same commit.

## 6. Questions for Bob (Q1–Q7: see §7 for Q8–Q9)

- **Q1 — Mechanism:** formation-quality ICM **traits** (Option A, recommended — stays inside
  ratified §15.1), or overrule §15.1 with hand-set per-profile ICM (Option B — needs a design-doc
  amendment and I've counter-argued it above)?
- **Q2 — Calibration target:** should a US armor battle group beat the **T-80U/BVM apex** in an
  even exchange (requires NATO Gen4 profiles and/or formation stat-deltas, not just ICM), or is
  the target "parity at apex, dominance against the T-72B/T-80B peer group" (achievable with
  M1A1 + formation trait ×1.15 + Experienced)? The math for both is in §3.
- **Q3 — NATO roster:** add the supplement-designed M1A1 (and late Leo 2 / up-gunned
  Challenger?) as upgrade-path profiles, so the AI's late-campaign armor keeps pace?
- **Q4 — Whole-unit price:** is a new-unit purchase the **sum of all populated bays**, or
  deployed-bay-only with Mobile/Embarked bought later as upgrades (the §3.2b Panzer-General
  reading)? This decides whether a VDV regiment costs 375 or 50.
- **Q5 — Kill-prestige basis:** keep V19's *active-profile* cost (bounty varies with posture),
  or re-base on the unit's purchase value once Q4 defines it?
- **Q6 — Anomaly rulings:** which of §4's items 1–8 are deliberate flavor vs fixes? (Zero-cost
  MJ artillery, item 8, looks like a plain authoring miss either way.)
- **Q7 — Scope check:** air stays at ratified parity (supplement §16) — the ICM pass touches
  ground formations only, correct?

---

## 7. Tank census vs stated ICM (added same day, after Bob's closed-bay ruling)

**The ruling this section works under (Bob, 2026-08-22, chat):** the ACTIVE profile must represent
the CombatUnit as a whole. ICM therefore only matters on profiles that are a unit's SOLE profile
(all other bays closed) — tanks, aircraft, SP guns, etc.; profiles meant to be *paired* (INF +
APC/IFV, towed gun + truck) keep neutral ICM, and infantry ICMs stay as-is barring corner cases.
The census is the declaration of what the closed-bay counter actually contains, so it is the clue
for where ICM needs editing. ⚠ When ratified, this needs recording in HS_DesignDoc (it refines,
and partly reinterprets, supplement §15.1 — ICM stays trait-fed mechanically, but its MEANING for
closed-bay profiles becomes "the whole formation," not just platform fire control.)

**Consistency check first:** the DB already conforms to the pairing half — every Mobile/Embarked
bay profile (all APCs, IFVs, trucks, lifts) sits at ICM 1.000. The only non-1.0 deployed-paired
profiles are infantry (Stinger 1.05 / SPECIAL_FORCES 1.10), which Bob has excluded from the pass.

### The data — all 24 tank profiles (census bucket totals vs resolved ICM)

Within each faction the census is IDENTICAL across generations (all nine Soviet tank profiles carry
the same 94-tank structure with era-appropriate support types), so one row per formation family:

| Formation (profile) | ICM | Men | Tanks | IFV | APC | Rcn | ATGM | MANPADS | Arty | AD veh | Helos |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Soviet tank rgt (×9: T-55A → T-80BVM) | 1.00 → 1.21 | 1143 | **94** | 45 | 21 | 12 | 12 | 12 | 42 | 8 | — |
| US M1 bn task force | **1.33** | 1000 | 58 | 13 | 16 | 6 | 16 | 9 | 27 | 2 | — |
| US M60A3 (armored cav squadron) | 1.155 | 1500 | 41 | — | 18 | **36** | **24** | 12 | 8 | 4 | **16 (8 AH-64 + 8 OH-58)** |
| GE Leo 1 / Leo 2 (same census) | 1.10 / 1.33 | 1100 | 55 | 13 | 12 | 6 | 16 | 12 | 24 | 4 | — |
| UK Challenger 1 | 1.21 | 1000 | 58 | 13 | 8 | 8 | 15 | 8 | 27 | 0 | — |
| FR AMX-30 (division-as-counter) | **1.00** | **1750** | 80 | 36 | 18 | 12 | 8 | 18 | 36 | 0 | — |
| NL / BE / DK Leo 1 (brigades) | 1.10 | 2000/1900/1600 | 84/72/60 | — | 60/55/48 | 12/12/10 | 24/20/24 | 18/16/12 | 30/30/24 | 0 | — |
| IQ T-55 / T-62 rgt | **0.90** | 950 | **105/104** | 40 | — | 12 | 6 | 8 | 36 | 8 | — |
| IR M60A3 | 1.05 | 1100 | 100 | — | 52 | 12 | 8 | 6 | 36 | 4 | — |
| CH Type 59/80/95 (same census) | 1.00/1.05/1.10 | 1050 | 80 | 40 | — | **0** | 12 | 18 | 36 | 12 | — |

### Findings

1. **Within-faction: fully consistent.** Census constant per formation family; ICM climbs purely
   with fire-control traits (Soviet 1.00 → 1.21 across nine gens on one identical formation;
   Chinese mirrors it; GE Leo 1 vs Leo 2 share one census with 1.10 vs 1.33). Under the new
   lens this reads as: formation quality is the family constant, platform FCS is the variable —
   coherent, nothing to fix inside a faction.
2. **Between factions, census mass and ICM are INVERSELY related.** The biggest censuses carry
   the lowest ICMs (IQ 105 tanks @ 0.90, Soviet 94 @ ≤1.21, NL 84 @ 1.10, FR 80 @ 1.00) and the
   thinnest carry the highest (GE 55 / US 58 @ 1.33). That is *exactly* Bob's "58 beats 94"
   doctrine — quality beats mass — but today it holds by accident of the FCS traits, and
   **nothing in the model prices mass at all**: HP is 40 for every counter, and the old
   ICM_SMALL_UNIT/LARGE_UNIT dials were retired with the trait migration. Needs an explicit
   ruling (Q8) because the census now *visibly* claims those masses to the player through the
   intel panel and books them into the loss report.
3. **The support slices are structurally parallel, so the census does NOT discriminate US vs
   Soviet formation quality.** Everyone carries roughly two artillery battalions' worth of tubes,
   an AT allotment, MANPADS, and a recon slice; the US TF is actually *leaner* than the Soviet
   regiment in everything except TOWs (16 vs 12). So the census cannot be the source of the NATO
   formation premium — that premium is integration/C3/doctrine, invisible in counts, and has to
   come in as a named formation-quality trait (§3 Option A). The census's role is narrower:
   flagging counters whose contents a platform statline badly misrepresents. Which it does, once:
4. **⚠ THE outlier — `TANK_M60_US` is an Armored Cavalry squadron with an air-cav troop.**
   Census: 41 M60A3 + **8 AH-64 + 8 OH-58** + 36 M3 scouts + 24 TOW + 12 Stinger, 1500 men —
   at ICM 1.155 (LRF + thermal only) and **prestige 65 (Gen1+TANK, T-55A money)**. Sixteen
   helicopters and the densest AT/recon content in the game are entirely invisible in an M60A3
   statline. This is the one place the census screams for an edit: either an ACR/air-cav
   formation trait (total ICM ~1.35–1.40) + a serious prestige re-tier, or the census is
   over-declared and should be trimmed (as written, a dead M60 counter books 8 Apaches into the
   §24.8.7 loss report). Q9.
5. **Mass-heavy NATO counters with flat ICM:** the AMX-30 division (80 tanks, 36 AMX-10P,
   1750 men, ICM 1.00) and the NL/BE/DK Leo 1 brigades (60–84 tanks, ICM 1.10 — same as the
   55-tank GE battalion counter). Pending Q8: if mass is normalized away, these stand; if mass
   belongs in closed-bay ICM, they take a ~×1.10 formation-mass component despite mediocre
   platforms.
6. Minor census notes: Chinese tank censuses carry **zero recon** (authoring gap or doctrine
   flavor?); UK Challenger census has no AD vehicles (8 Javelin teams only); IQ at 105 tanks is
   the biggest tank census in the game on the worst ICM — the deliberate poster child, stands.
7. **Honesty check against §3:** census-justified ICM edits still cannot flip the benchmark duel.
   Even M1 at 1.53 total (formation trait) puts the exchange at ~3.9 vs ~7.0 against a T-80U —
   the band Δ dominates. Q2/Q3 (M1A1 statline / NATO Gen4 roster) remain the load-bearing half
   of "a US armored formation is a powerful force."

### Candidate edit list (proposal — pending Q1/Q2/Q8/Q9 rulings)

| Profile | Now | Candidate | Basis |
|---|---|---|---|
| Soviet ×9, CH ×3, IQ ×2, IR | 0.90–1.21 | **unchanged** | anchor formations; internally consistent |
| TANK_M1_US | 1.334 | × formation trait ≈ 1.50–1.55 | Q1 mechanism, Q2 calibration |
| TANK_LEOPARD2_GE / LEOPARD1_GE | 1.334 / 1.103 | × ~1.10 first-line trait | same |
| TANK_CHALLENGER1_UK | 1.213 | × ~1.10–1.15 | same |
| TANK_M60_US (ACR) | 1.155 | **~1.35–1.40 via ACR/air-cav trait** (or trim census) + prestige re-tier | the census outlier, Q9 |
| TANK_AMX30_FR, Leo1 NL/BE/DK | 1.00–1.10 | unchanged, or +mass component | Q8 |

### Added questions

- **Q8 — Mass ruling:** is formation mass priced into closed-bay ICM (resurrecting what
  SMALL/LARGE once did), or normalized away (one counter = one maneuver formation, the ratified
  §1a scale-table reading — my recommendation, with capability-class exceptions like Q9)? The
  census then stays an intel/loss artifact, not a balance input.
- **Q9 — The ACR:** is `TANK_M60_US`'s air-cav census real (→ formation-trait ICM bump + prestige
  re-tier) or over-declared (→ trim the census)?
