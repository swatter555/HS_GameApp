# Prestige Pass — Soviet Economy Analysis (2026-08-22)

**Scope (Bob's brief):** straight analysis of the prestige economy with the SOVIET side as the
subject — the player faction is the only one where the economics has to be *right*; every other
faction's prices only need to be bounty-grade (V19 kill fraction) and rough-analysis-grade.
**Goal: a Soviet economy that is strong and grounded** — every price justified against what it
buys, every income source tied to live numbers, every open rule named. Data: the validated
resolver tables from the ICM-pass survey (post-`d6734f3` values) + the live code + Khost's
shipped manifests.

---

## 1. The economy as actually built (what exists vs what's paper)

**LIVE (code, shipping):**
- **Wallet:** `PrestigeWallet`, seeded from `manifest.prestigePool` at battle setup
  (BattleManager:462); `AddPrestige`/`SpendPrestige(bool)` atomic. ⚠ **Nothing calls
  SpendPrestige yet** — P4 requisition is the first spender; every sink below is a rule
  waiting on it.
- **Income (§18.2, per PlayerUpkeep):** `stipend + PlayerValue × incomeRate`, plus the
  high-water progress bonus `(PlayerValue − highWater) × progressBonusRate`, plus the §17.9.3
  early-finish bonus. AI earns nothing (scripted-economy ruling).
- **Kill bounty (§18.2.3 / V19):** `0.5 × killed.ActiveProfile.PrestigeCost` — **reported, not
  credited** (crediting lands with M13).

**PAPER (design §18, no code):** unit purchase (§18.3), replacements (§18.4:
`damage% × 0.5 × unit purchase cost`, ×0.7 with Direct Line To HQ), upgrades (§18.5),
airbase repurchase (§31.4a.21, 300 — constant not in GameData yet), caps/carryover (§18.8/§19.4).

**NOT prestige at all (doc/code divergence):** §18.6/§18.7 list leader skill tiers and
promotions under "18. Prestige," but the code spends **ReputationPoints** for both
(`LeaderSkillTree`, `REP_COST_FOR_*_PROMOTION`). The prestige economy's sink list is exactly:
**new units · bays · replacements · upgrades · airbase repurchase.** That's the clean loop.

⚠ **§18.3's price table is the PRE-2026-06-18 table** (TANK 55, BMS 215, AWACS 300, tiers
50/100/150, air-tier shortcuts, a WW type) — none of it matches the code's ratified
price-to-potential pass (TANK 65, BMS/AWACS 450, tiers 60/120/180, no WW member, no air
shortcuts). The doc was never amended. Reconcile with this pass.

---

## 2. Grounding: what a battle actually pays (Khost, shipped numbers)

Khost manifests (both variants): **pool 500 · stipend 20 · rate 0.05 · progress 0.5 ·
early-finish ×1.25 · 21 turns · map value 1,550.**

| Player state | PlayerValue | Income/turn |
|---|---|---|
| Start (attacker foothold, ~⅓ of map) | ~500 | 20 + 25 = **45** |
| Minor-victory share (55%) | ~850 | 20 + 43 = **63** |
| Decisive share (80%) | ~1,240 | 20 + 62 = **82** |
| Collapsed to nothing (the floor) | 0 | **20** |

A competently won Khost ≈ 21 × ~60 avg = **~1,260 steady income**, + progress bonus
(0.5 × ~700 value gained above the start ≈ **~350**, paid once as ground is first taken),
+ early-finish if declared, + pool 500 → **~2,000–2,300 prestige per battle**, before kill
bounties are ever credited.

Held against the Soviet price ladder that is a **strong but not inflationary** economy:
- One battle's surplus buys roughly **one apex regiment (T-80U, 245) + one mid regiment
  (MRR BMP-2, 165 as Σ-bays) + a generation upgrade + replacements**, or ~8–12 cheap actions.
- The **stipend floor works**: a player scraped down to zero territory still makes 20/turn;
  a 50%-damaged MRR costs `0.5 × 0.5 × 165 ≈ 41` to restore — one replacement every ~2 turns
  on floor income. The anti-death-spiral dial does what §18.2.1 says.
- Prestige carries between missions with the end bonus (§19.4), so per-battle balance is the
  correct tuning frame; surpluses compound deliberately.
- Note the **dual currency**: prestige BUYS a unit; `deploymentPointCap` (Khost: 1,000) FIELDS
  it (§35.3/35.4). Prestige tuning never has to police force size alone.

**Verdict on the income side: grounded as-is.** The Khost dials are placeholders but they land
in the right decade; the knobs (stipend/rate split) are per-scenario and already expressive.

---

## 3. The Soviet price ladder, audited (the core of this pass)

Per-profile `cost = tier(0/60/120/180) + type`. Judged as the PLAYER'S MENU: what competes with
what at each price point, using the post-ICM-pass resolved stats.

### 3.1 Tanks (type 65) — mostly sound, two flags

| Profile | Cost | Line | Verdict |
|---|---|---|---|
| T-55A / T-62A | 65 | HA7-8 HD6 | ✓ floor |
| T-64A / T-72A | 125 | HA12 HD9 (T-72A +ICM 1.05, −1 SD) | ✓ big step for +60, twins priced equal |
| T-64B / T-72B | 185 | HA15 HD14-15, ICM 1.05–1.10 | ✓ twins again |
| **T-80B** | **185** | **HA13 HD10** MMP12 ICM 1.10 | ⚠ **dominated at its price** — pays Gen3 money for a Gen2 hull whose only edge is +2 MMP; HD 10 vs its 185-tier peers' 14–15 makes it a trap purchase |
| T-80U | 245 | HA17 HD15 MMP12 | ✓ apex step (+2 HA +2 MMP over T-72B for +60) |
| **T-80BVM** | **245** | HA20 HD20 ICM 1.21 + APS | ⚠ same price as T-80U, strictly better — separated only by turnAvailable (564 vs 584). A policy question, not an accident (see R-P6) |

### 3.2 The mounted-infantry menu (the player's bread and butter) — ⚠ the BTR line is dead stock

| Mount | Cost | Line | Verdict |
|---|---|---|---|
| MT-LB | 40 | HA3 SA6 tracked | ✓ utility floor |
| **BMP-1** | **55** | HA8 SA8 amphib | ✓ — and it makes both BTRs absurd: |
| **BTR-70** | **100** | HA3 SA6 SD8 | ⚠ **costs 2× the BMP-1 for half the combat power** (Gen2+APC vs Gen1+IFV tier accident) |
| BMP-2 | 115 | HA9 SA9 | ✓ |
| **BTR-80** | **160** | HA3 SA7 | ⚠ **costs more than the BMP-2 and fights like an MT-LB** |
| BMP-3 | 175 | HA9 SA10 HD5 | ✓ |
| BMD-2 / BMD-3 | 115 / 175 | = BMP-2/3, HD−1, +AIR_DROPPABLE | ✓ capability offset |

No player ever buys a BTR at these prices. The tiers came from hardware generations
(BTR-70 "Gen2", BTR-80 "Gen3") rather than potential. **Recommend: BTR-70 → Gen1 (40),
BTR-80 → Gen2 (100)** — wheels as the budget mount, BMPs as the fighting mount, which is
also historically how motor-rifle divisions actually tiered.

### 3.3 Recon — one flag

BRDM-2 and BRDM-2 AT both 45; the AT variant is strictly better after the recon ruling
(HA6 vs HA2, same class, +AIR_DROPPABLE). **Recommend BRDM-2 AT → Gen2 (105)** — +60 for +4 HA
matches the ladder's price-per-step everywhere else, and the AT-5 fit is later kit anyway.

### 3.4 Artillery — the ladder holds once RANGE is counted; one balance question

- Towed light = heavy at 90: heavy is +1 SA, light is helo/air-droppable — deliberate offset ✓.
- SPA: 2S1 130 → 2S3 190 → 2S5 250 → 2S19 310. Each +60 buys +1 SA and/or **+1 IR** and the
  2S19's Krasnopol (HA8). Reach is the real product (IR 4→5→6); priced as such the ladder is
  sound. (2S19's IR 5 < 2S5's 6 is the ratified inversion — stands.)
- MRLs: BM-21 175 → BM-27 235 → BM-30 295; Scud 450 apex ✓.
- ⚠ **The tube-vs-rocket question:** BM-21 (175, SA9, IR4, **double-fire**) vs 2S3 (190, SA10,
  IR5, single): the MRL's +1 CombatAction is worth far more than −1 SA/−1 IR, at −15 prestige.
  The real governor is SUPPLY — each combat action costs 1 supply (§8.2.1), so double-fire
  burns a 5-day loadout in ~2 turns of maximum effort. If that supply bite is intended as the
  balancing cost, the pricing stands; if not, ROC type (175) wants to sit higher. **Ruling
  wanted, no change recommended until the supply pass proves the bite (R-P5c).**

### 3.5 Air defense — internally fine, one dependency and two nits

Ladder ZSU-57 135 → ZSU-23 195 → 2K22 295; 2K12/9K31 235; S-75/S-125 145; S-300 325. Nits:
S-125 = S-75 at 145 with GAT 14 vs 15 (mildly dominated); 9K31 = 2K12 at 235 with GAT 13 vs 15
(FNF capability vs shoot-scoot — roughly a wash). Leave both.
⚠ **Dependency: every AD price presently overstates delivered value** because of the §11.8
range defect (all batteries engage at range 1 until the AD-fix session lands). Price against
the *intended* envelopes, which the fix restores — do not re-price AD around the bug.

### 3.6 Air and helicopters — one loud anomaly, one policy twin

- Helos: Mi-8T 90 (lift) → Mi-8AT 150 → Mi-24D 190 → Mi-24V 250 → Mi-28 310 — clean +60 steps ✓.
- Fighters: MiG-21 150 → MiG-23 210 → MiG-29 270 → Su-27 330; interceptors MiG-25 210 /
  MiG-31 330 ✓. **Su-47 = Su-27 at 330** while strictly better (MAN+3, +ECM) — the BVM pattern
  again, turn-gated (R-P6).
- ⚠ **Su-24 at 195 is the loudest strike-price anomaly on the Soviet side.** Typed ATT +
  Gen2 → 195, the same price as the MiG-27/Su-17 (GA6/OL6) while carrying **GA13/OL14** +
  hardened-strike + terrain-following. It out-values the Su-25 (255) and embarrasses the
  Tu-16/Tu-22 (240). It is a Fencer priced as a Fitter. **Recommend: re-type BMB (Gen2+240=300)
  — its DB section header already calls it a Bomber — or at minimum Gen3+ATT = 255.**
- Tu-22M3 400 explicit / Scud 450 / A-50 450 apexes ✓.

### 3.7 Sum-of-bays view (under the recommended R-P1 ruling)

MRR BMP-2 165 · MRR BTR-80 210→150 after R-P5a · tank rgt 65–245 · AAR BMD-3 315 ·
VDV BMD-3 375 · GRU 200 · SAM rgt 165. The "a VDV regiment costs 1.5× a T-80U regiment"
objection dissolves under composability: 375 is the FULLY-equipped price; the regiment is
50 (leg) + 175 (BMDs) + 150 (An-12 lift), each bought when wanted — exactly the §3.2b
buy-into-the-bays model. Sum-of-bays and PG-staging are the same ruling.

---

## 4. Other factions (bounty-grade check only, per the brief)

Kill bounty = half the profile cost, and only the PLAYER earns it, so other-faction prices are
reward tuning. Two items are loud enough to fix; the rest (AMX-30 vs Leo1/M60 tier noise,
Warrior/Humvee, Iranian 2K12 at 135) can stay as flavor until an expansion makes them player-facing:
- **TANK_M60_US at 65** — after the ICM pass this is a 1.40-ICM Armored Cavalry squadron with
  an organic Apache troop, paying a **T-55A bounty (33)**. Recommend explicit-cost ~250 (the
  documented exception region), comment referencing the census.
- **T-80U = T-80BVM = 245** matters on the Soviet side too (R-P6), listed here because whatever
  policy is chosen applies to the NATO late roster when Q2/Q3 land.

MJ artillery zero-bounty is already fixed (ICM pass, ruling 11).

---

## 5. The rules P4 needs ruled (decision list)

- **R-P1 — Whole-unit price = Σ populated bays, composable.** A new unit may be bought bare
  (deployed bay only) or configured at purchase; bays add later at their profile price
  (§3.2b's model, priced). This single definition serves §18.3, §18.4.1's "unit purchase
  cost," and P4's `RequisitionService`. *Recommended.*
- **R-P2 — Replacement basis** = Σ currently-populated bays (formula §18.4.1 unchanged:
  `damage% × 0.5 × Σ`, ×0.7 Direct Line To HQ). Replacing a mounted regiment costs more than a
  leg one — you are replacing the BMPs too. *Recommended.*
- **R-P3 — Kill-bounty basis** = Σ populated bays (amend V19's active-profile read). Kills the
  posture quirk (mounted MRR bounty 80 vs dismounted 25) and matches §18.2.3's "purchase cost"
  language. Cheap: one helper on CombatUnit. *Recommended; keeping active-profile is defensible
  if the posture-bounty is wanted texture.*
- **R-P4 — Upgrade formula (the §18.5 hole — nothing in code, one vague doc line).**
  Recommended: `upgradeCost = max(targetProfileCost − currentProfileCost, UPGRADE_MIN ~20)`
  (×0.7 Connections At The Top), same UpgradePath + turnAvailable gates. T-72A→T-72B = 60;
  T-55A→T-80U = 180; same-price sidegrade (T-80U→BVM) = the floor — which is also what makes
  R-P6's same-price successors work as PG-style free-ish refits.
- **R-P5 — Soviet re-tiers:** (a) BTR-70 → Gen1 (40), BTR-80 → Gen2 (100); (b) BRDM-2 AT →
  Gen2 (105); (c) Su-24 → BMB type (300) or Gen3 ATT (255); (d) T-80B → Gen2 (125) *or* keep
  185 as a deliberate speed premium — Bob's call; (e) leave 2S3/2S19, S-125, 9K31 as-is.
- **R-P6 — Same-price turn-gated successors (T-80BVM, Su-47):** accept as the PG idiom (later
  = better at the same list price; the upgrade transaction still costs R-P4's floor), or split
  by explicit cost (+35–55 in the exception region). *Recommend accept — it rewards campaign
  progress without inflating the ladder.*
- **R-P7 — Doc/code reconcile:** rewrite §18.3's table to the code values; move §18.6/§18.7 out
  of the prestige section (they are REP); add `AIRBASE_REPURCHASE_COST = 300` to GameData when
  §11.7.2.7 lands; define §18.5 per R-P4.
- **R-P8 — Bounty fixes:** M60A3/ACR explicit ~250. Everything else non-Soviet stays.

## 6. What implementation looks like (after Bob rules)

Small and mostly data: 4–6 `SetPrestigeCost` re-tiers + 1–2 explicit costs (R-P5/R-P8), a
`CombatUnit.PurchaseCost` Σ-bays helper + the V19 read swap (R-P3), the §18 doc amendments
(R-P7), and the R-P4 formula as a pure static ready for `RequisitionService` (P4 proper stays
its own pass). No SAVE_VERSION impact. The income side needs **nothing** — it is live, grounded,
and correctly dialed per scenario.

---

## 7. Implementation record (same day — Bob approved all 12 items)

**Prices (WeaponProfileDB, verified by the extraction pipeline):**
1. BTR-70 100 → **40** (Gen1+APC) ✅
2. BTR-80 160 → **100** (Gen2+APC) ✅
3. BRDM-2 AT 45 → **105** (Gen2+RCN) ✅
4. Su-24 195 → **300** (Gen2+**BMB** type — priced as the bomber it is; UpgradePath untouched) ✅
5. T-80B 185 → **125** (Gen2+TANK — the +2 MMP is a fair trade at the T-64A/72A tier) ✅
6. M60A3 ACR 65 → **250** (explicit — new `GameData.PRESTIGE_ACR_SQUADRON`, exceptions region) ✅

**Rules (code where cheap, §18 for the rest):**
7. §18.3.1 ratified: whole-unit price = Σ populated bays, composable — and the Σ exists in code
   now as **`CombatUnit.PurchaseCost`** (deployed + mobile + embarked; naval transient excluded). ✅
8. §18.4.1 amended: replacement basis = PurchaseCost. (Formula stays paper until P4.) ✅
9. **V19 kill bounty re-based on PurchaseCost** — all three `PrestigeOnKill` sites
   (Ground/Indirect/Ambush actions) swapped off the active-profile read; posture no longer moves
   a victim's bounty. New `PurchaseCost_SumsPopulatedBays_PostureIndependent` test pins it. ✅
10. §18.5.1 ratified: `upgradeCost = max(target − current, PRESTIGE_UPGRADE_MIN 20)` × 0.7 with
    Connections At The Top — constant added to GameData; `RequisitionService` consumes it in P4. ✅
11. §18.5.2 ratified: same-price turn-gated successors (T-80BVM, Su-47) are deliberate; upgrading
    into them costs the floor. ✅
12. §18 reconciled: 18.3's stale pre-2026-06-18 table replaced with the code values (WW type and
    air-tier shortcuts formally retired); 18.6/18.7 marked REP-not-prestige; airbase-repurchase
    constant noted as arriving with §11.7.2.7. `todo_profiles.md` §4.7 now carries the ratified
    pricing rules so P4 builds against them, not around them. ✅

**Verification:** extraction + resolver re-run over the edited DB — all six prices exact, 184
profiles parse clean, ICM distribution unchanged. No SAVE_VERSION impact (prices/rules are
DB-build data; PurchaseCost is derived, `[JsonIgnore]`).

**Outstanding:** Bob runs Unity Test Runner (the V19 basis swap touches three combat actions +
one new test), then commit. P4 requisition remains its own pass, now fully specified.
