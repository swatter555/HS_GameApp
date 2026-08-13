# UnitDB Census Audit — 2026-08-13

**Status: IN PROGRESS.** Findings land per faction below. No code changes until Bob approves the fix plan.
**For:** Bob (arbiter) → then relayed to the Scenario Editor agent.

---

## 0. THE ADOPTED DOCTRINE (forged 2026-08-13 — audit measures against THIS)

### Ruleset v2 (Bob's four rules + corollaries, all sides)
1. **Bay-sum = formation.** A template's summed census is its full-strength formation roster.
2. **Carrier profiles: own platform count, nothing else.** (No Personnel, no recon, no tanks.)
3. **Base profile owns everything else** — men, organic tanks, tubes, AT, MANPADS, recon, AD sections.
4. **Lift (`TransportCategory != None`) carries no census, ever.** Lift losses are unreported, by design
   (a debarked VDV regiment must not lose transport planes in ground combat).
5. **Trucks are never counted** (permanent law; already shipped reality).
6. **One base, one formation shape.** Mounting a carrier TRANSITIONS the unit: new base profile, new
   Classification, new icon (the AB/MAB · MAR/MMAR pattern, completed for INF→MECH/MOT). P4 machinery;
   this pass authors the profile pairs only. AI armies never transition — pairs only where both
   formations appear on maps.
7. **An open-bay base never lists a type that could occupy its own Mobile bay** (keeps carrier upgrades
   one-line census changes). Closed-bay bases (tanks etc.) list attached carriers as tokens — correct.
8. **Doctrine guards** extend `CensusIntegrityTests`, written WITH the fix pass: carrier census
   single-key · lift empty · trucks empty · classification coherence (MECH ⇒ tanks+carriers, ART ⇒ guns…).

### The scale table (ADOPTED 2026-08-13 — supersedes the earlier resize-to-names ruling)
One counter = that nation's real maneuver element. Fits the 5 km hex (a battle group holds ~5 km of
front; a full US brigade holds 15–20 and never fit one hex). Produces quantity-vs-quality: a Soviet
regiment outnumbers each NATO counter, NATO quality lives in the stat line.

| Nation | One counter = | Tanks/counter (heavy) |
|---|---|---|
| USSR | Regiment | ~94 |
| USA | Battalion task force | ~58 |
| FRG, UK | Kampfgruppe / battle group | ~55–60 |
| FRA | Battle group (their brigades were small/odd — interpretation below) | ~40 |
| NL, BE, DK | Brigade (already battle-group mass) | 60–84 |
| Arab/IR/IQ/China | Regiment/brigade as-is, audit per case | — |

### Other standing rulings
- **Precision:** plausible everywhere; highest fidelity on Soviets (player reads them EXACTLY — friendly
  view, zero error; enemies are bucket-merged + fuzzed 8–16%).
- **Names:** US/FRG/UK re-cut to battle-group names; renames FREE until first NATO `.oob` exists.
  Naming scheme proposal in §5. Census-only tokens + new profiles both authorized.
- **Lift strips (confirmed sites):** `INF_AM_SV` −166 Mi-8 · `HEL_MI8T_SV` −109 · `TRN_AN8_SV` −48.
- **Doc amendments owed:** §24.8.7.4.3 (TRN/Aircraft row rationale was the An-12's 48) ·
  §10.7.1/10.7.3 (still describe RegimentProfile/ProfileType, stale since P1) · §1a scale line becomes
  true by construction ("one maneuver element, ~1,500–2,500 men") · §11.8.11.1 rationale survives.
- **Vocabulary:** census = a profile's authored `IntelReportStats`; intel report = computed output.
  Anchored in Claude_Project §9.
- **P4 design slot:** the mount-transition table (which carrier ⇒ which classification: APC⇒MOT,
  IFV⇒MECH, BMD on VDV⇒MAB?) + a leg→mech conversion price for §18.3. Proposed in §6.

### Flag legend
**MISSING** empty/absent census · **MISASSIGNED** wrong platform or wrong receiver ·
**AHISTORICAL** egregious composition/numbers (no niggling) · **CROSS-NATIONAL** another nation's kit
visible in Bob's editor · **DOUBLE-COUNTED** bay-sum phantom (rule 2/7 violation) ·
**LIFT** rule-4 strip · **SCALE** battle-group re-cut needed

---

## 1. SOVIET PROFILES (highest precision — player-visible exactly)

### 1.1 Tank regiment bases (T55A→T80BV, :262–640) — ✅ GOOD, keep
All nine: 94 tanks · 1,143 men · one BMP/BTR motor-rifle bn (45+21) · 12 BRDM · AD battery (4+4) ·
18 2S1 · 24 mortars · 12 ATGM · 12 Strela. **94 tanks and 1,143 men are the real numbers**; the
organic MR battalion and 18-gun SP battalion are correct regiment structure; AD era-matches the tank
generation (ZSU-57→T-55 … 2K22→T-80U), which is deliberate and good. Closed-bay bases, so carrier
tokens are legal (rule 7). Only quibble (niggling, no action): a T-55 regiment with BMP-1s.

### 1.2 Ground carriers — ⚠ DOUBLE-COUNTED, all of them (rule 2)
Every MRR carrier holds a STANDALONE MRR ROSTER, so every mounted MRR double-reports today:
- `BMP1/BMP2/BMP3` (:676–754): **40 tanks** + 129 own + 26 BTR + 12 BRDM each
- `BTR70/BTR80` (:896–937): **40 tanks** + 129 own + 26 BMP + 12 BRDM each
- `MTLB` (:860), `BMD2/BMD3` (:789–825): no tanks, but 13 BRDM each (rule-2 violation, minor)

**Fix:** carriers → own platform count only (129 / 68). The 40 tanks (RIGHT number — the MRR organic
tank bn), 26 support APCs and 12 BRDM move to the new Soviet MECH BASE census (§1.5). ⚠ The 40 must
move, not vanish — a Soviet MRR with no tank battalion would be ahistorical the other way.

### 1.3 Support/AD/rocket regiment bases — ✅ mostly GOOD
- SPA 2S1/2S3/2S5/2S19 (:1077–1206): 36 tubes ✓ correct regiment scale; support plausible.
  (2S5 lists 44 BTR vs the others' 24 — niggling.)
- BM21 48 / BM27 24 / BM30 24 / SCUD 12 (:1327–1459): ✓ all plausible army-level regiments.
- ZSU/SPSAM/SAM regiments 18 systems each (:1501–1710): ✓ correct across the board.
- Recon BRDM2/BRDM2AT (:977–1035): 800 men, 36/48 recon + 12 tanks — plausible (recon bns had a
  tank company); keep.
- **SCALE (soft):** ART_LIGHT/HEAVY_SV 72 tubes (:1251, :1287) is BRIGADE mass wearing a
  "Regiment" name — either 48 or rename; Bob's call in §7.

### 1.4 Lift + air
- **LIFT strips:** `HEL_MI8T_SV` −109 (:1865) · `TRN_AN8_SV` −48 (:2057, var name AN12 — one profile
  is both An-8/An-12 lift) · `INF_AM_SV` −166 Mi-8 (:2763). AM regiments bay-sum to ~275 helos today.
- Combat air: fighters 36 ✓ · bombers 24 ✓ · MiG-25R 12 ✓ · A-50 6 ✓ · gunship/`MI8AT` regiments
  54 + 475 men ✓. **Consistency nit:** helo regiments list Personnel, jet regiments none — pick one
  convention in the fix pass (proposal: aircraft only, no Personnel, all air).
- S-125/S-300/AAA_GEN (:1743–1828): 18 systems each ✓ good.

### 1.4b Infantry bases (:2688–2861)
- `INF_REG_SV` 2,523 men, AD 4+4, 18 2S1, 24 mortars, 12 BRDM, 16 ATGM, 30 Strela — **clean, no
  carriers listed ✓, but NO organic tank bn.** It is the de-facto MRR base (the 18 2S1 are MRR
  assets). Rule-6 question for §7: if leg-infantry templates ALSO use it, split `INF_MECH_SV` (adds
  the 40 tanks from §1.2) from a leg base; if MRR-only, add the 40 here and rename at will.
- `INF_AB_SV` 2,250 ✓ clean (BMDs correctly absent — carrier supplies them) · `INF_MAR_SV` 2,750 ✓.
- `INF_SPEC_SV` **2,300 men — AHISTORICAL (soft):** Spetsnaz brigades ran ~1,000–1,300. Propose 1,200.
- `INF_ENG_SV` 340 men + 20 BTR — battalion mass; fine if the template says battalion; check name.

### 1.5 The Soviet base gap — the ONE new base profile this faction needs
MRR templates ride deployed `INF_REG_SV`(?) + carrier. Under rule 6 the mech formation needs its own
base: **`INF_MECH_SV`** (or re-purpose INF_REG_SV if it serves ONLY MRRs — pending §pages), census =
~2,200 men · 40 era-typical tanks · 26 support APC tokens · 12 BRDM · 18 2S1 · mortars/ATGM/Strela.
BTR vs BMP regiments share it; the carrier census supplies the 129 difference. VDV/AM/MAR/SPEC bases
already exist; their censuses lose lift only.

## 2. GENERIC + WESTERN PROFILES (US/FRG/UK battle-group re-cut; FR interpretation)

### 2.1 Bases audited so far
- `TANK_M1_US` (:2991) 2,200 men · 116 M1 · 54 M2 · 18 M3 · 32 M113 — historically RIGHT as a
  brigade; **SCALE → battalion task force:** propose ~1,000 men · 58 M1 · 13 M2 · 6 M3 · support
  halved. Same treatment for `IFV_M2_US` base pairing.
- `TANK_M60_US` (:3033) = the ARMORED CAV SQUADRON census. 41 M60 ✓ squadron-right. **AHISTORICAL
  (soft): 26 AH-64 + 12 OH-58 on a ground squadron** — the helos lived in the aviation squadron;
  propose cut to ~8 (air troop) or 0.
- `LEO1_GE`/`LEO2_GE` (:3075/:3118) 116 tanks — **SCALE → Kampfgruppe ~55**, men ~1,100.
- `CHALL1_UK` (:3161) — **MISASSIGNED, real bug:** :3165 `SPSAM_RAPIER_UK 8` is commented "Brigade
  reconnaissance troop (CVR(T))" — the token should be `RCN_FV105_UK 8`. Also :3167
  `MANPAD_STINGER 16` commented "Javelin teams" — UK carried Javelin/Blowpipe, never Stinger
  (CROSS-NATIONAL; UK MANPAD tokens are muddled — Rapier is a towed SAM, not a MANPAD). Fix pass
  proposes a UK SHORAD token cleanup. **SCALE → battle group ~58.**
- `AMX30_FR` (:3204) 80 tanks / 1,750 men — **the French mystery SOLVED: this is a French
  DIVISION.** France abolished the brigade echelon in 1977; its "divisions" were brigade-sized
  (~8,000 men, 80–100 AMX-30 in the armoured ones). Options for Bob: keep 80 and NAME it a
  division, or re-cut to the 40-tank regiment. Recommend: re-cut to 40 (matches scale table).

### 2.2 Carriers — same DOUBLE-COUNT disease as Soviet (rule 2)
`M2_US` (:3248) 58 M1 + 108 own + 18 M3 + 32 M113 · `WARRIOR_UK` (:3283) 58 Challenger + 58 own ·
`MARDER_GE` (:3318) 58 Leo 1 + 102 own · `APC_M113_US` 58 M1 + 108 own (known). Fix: own count
only; the tanks move to the paired MECH BASE census (which the battle-group re-cut resizes anyway).

### 2.3 More carriers (rule 2)
- `LVTP7_US` (:3441): 18 own + **120 Humvee + 12 AH-1 Cobra** — a carrier with a MEU roster.
  Own count only (~45 for a lifted Marine regiment).
- `VAB_FR` (:3478): **20 AMX-30** + 135 own + 12 ERC-90 — same disease. Own count only.

### 2.4 SP artillery bases — ✓ scale fine; one known MISASSIGNED
54/48-tube battalion groups, plausible. **`SPA_M109_FR` (:3609) confirmed: lists `SPA_AUF1 48`,
zero M109s** — the editor agent's §7.1 finding. Fix: token → its own type (or rename the profile
AUF1 — France DID field AUF1; the mismatch is profile name vs census identity; Bob's call).
`ART_LIGHT/HEAVY_WEST` (:3693–3731): 72 tubes each (54+18 mixed calibres) — same SCALE soft flag
as Soviet towed; propose 54 single-calibre-dominant.

### 2.5 Air-defence comment/token muddle (one cluster, fix together)
- `M163_US` :3816 — `MANPAD_STINGER 12` commented "Chaparral" (comment wrong).
- `Chaparral` :3854/:3856 — comments SWAPPED (says Vulcan on the Chaparral line and vice versa).
- `Hawk_US` :3896 — `APC_M113_US 24` commented "Chaparral" (comment wrong).
- `Gepard_GE` :3929 — lists **`SAM_HAWK_US 4`** (US Hawk batteries inside a German gun regiment —
  drop or swap to a Roland token).
- `Rapier_SP` :4054 — lists **`SPAAA_M163_US 4`** (US Vulcans in a UK Rapier regiment — drop).
- Values are mostly right (18 systems each ✓); this is comment hygiene + 2 cross-national tokens.

### 2.6 Recon + helo + jets
- Recon bases (:4096–4223): 36–48 own + light support — ✓ all fine.
- `AH64` 54+24 scouts ✓ · `BO105`/`AH1` 54 ✓.
- **`UH60` (:4302): 109 UH-60 + 18 AH-64 + 24 OH-58, no Personnel — smells like LIFT.**
  If it sits in any Embarked bay → rule 4 strip to empty; if it is a deployed aviation unit,
  own count only. Usage check in fix pass.
- Jets 36 / bombers 24 / E-3 12 / SR-71 12 — ✓ uniform and fine.

### 2.7 Western infantry bases (:4929–5150) — ✓ clean structure
No tanks/carriers listed anywhere ✓ (already rule-7 compliant). US/GE/UK personnel re-scale with
the battle-group cut (2,200/2,600 → ~1,100–1,300). UK's `MANPAD_RAPIER` tokens are the §2.5 muddle
(Rapier is a towed SAM; UK MANPADS was Blowpipe/Javelin — propose one census-only token
`MANPAD_JAVELIN` and retire MANPAD_RAPIER from infantry censuses).

## 3. LOWLANDS (re-check of this week's own work against v2 — rules bite their author too)
- `APC_M113_NATO` (:5399): strip the 8 recon → own 102 only (rule 2).
- Armoured-brigade bases (:5271–5354): carrier tokens LEGAL (closed bay, rule 7) ✓; numbers stand
  (already battle-group mass per the scale table).
- `INF_REG_NL/BE/DK` bases (:5435–5502): clean of carriers ✓ but MISSING their organic tank bns
  under rule 1 — fix pass adds ~30–40 era-typical Leopard 1 tokens each. Cross-national tokens
  (`RCN_FV105_UK`, `SPA_M109_US` in NL/BE/DK censuses): replace with census-only tokens if desired
  (§5); editor-view cosmetic only.

## 4. ARAB + CHINESE
- **`T62A_IQ` base (:5587–5588) — EGREGIOUS, the audit's worst find: lists BOTH `TANK_T62A_IQ 104`
  AND `TANK_T55A_IQ 105` = 209 tanks on 950 men.** A stale copy-paste line; delete the T55 row.
- Carriers `BMP1_IQ`/`MTLB_IQ`/`M113_IR` (:5675–5747): 90 own + **31 tanks** each — rule-2 strip;
  tanks move to Arab mech bases.
- IQ/IR bases otherwise plausible (Iraqi 105-tank brigades are period-correct tank-heavy).
  `RCN_FV105_UK` in Iran's census is HISTORICAL (Iran bought CVR(T)) — keep.
- `FGT_F4_IR`/`F14_IR` list 48 aircraft vs the universal 36 — soft consistency flag.
- Mujahideen (:6316–6477): all clean and plausible ✓. China (:6519+): 80-tank regiments,
  40 IFV, 18+18 tubes ✓ plausible; remaining air pages assumed uniform-36 (spot-check in fix pass).

## 5. NAMING SCHEME + NEW PROFILES/TOKENS (proposed)
- **Bases encode formation type:** `INF_LEG_*` (foot) · `INF_MECH_*` (mounted) · existing
  `INF_AB/AM/MAR/SPEC` stand. The Lowlands trio `INF_REG_NL/BE/DK` → `INF_MECH_NL/BE/DK`
  (**rename FREE only until first Lowlands `.oob` — do it in the fix pass**).
- **New bases needed:** `INF_MECH_SV` (or re-purpose INF_REG_SV if MRR-only — check
  CreateSovietInfantryForces' deployed types), US/GE/UK battle-group bases (re-census existing),
  Arab mech bases for the stripped carrier tanks.
- **New census-only tokens (sparing):** `MANPAD_JAVELIN` (UK), optional national recon/SPA tokens
  for editor-view correctness (Bob's call — cosmetic only).
- US/GE/UK unit display names → battle-group forms ("US Armored Battle Group" etc.).

## 6. TRANSITION TABLE (P4 design slot, proposed)
APC in Mobile bay ⇒ `MOT` · IFV ⇒ `MECH` · BMD on AB ⇒ `MAB` · amtrac on MAR ⇒ `MMAR` ·
carrier removed ⇒ revert to foot classification. Base profile swaps leg↔mech counterpart;
icon follows classification. Conversion price (§18.3 gap): carrier PrestigeTypeCost + tier.
AI never transitions.

## 7. FIX-PASS PLAN (order of work, one commit per block)
1. **Doctrine strips (mechanical):** all carriers → own-count-only (SV ×7, US ×3, GE/UK/FR ×3,
   IQ/IR ×3, NATO ×1) · lift censuses → empty (MI8T, AN8, UH60 pending usage check) ·
   `INF_AM_SV` −166 · delete the `T62A_IQ` T55 line · `SPA_M109_FR` token fix ·
   `CHALL1_UK` recon-token fix · §2.5 comment/token cleanup.
2. **Base enrichment:** organic tanks onto mech bases (SV 40 · Lowlands ~30–40 · Arab 31 ·
   US/GE/UK per battle-group cut) — same commit as their carrier strips so bay-sums never
   regress mid-history.
3. **Battle-group re-cut:** US/GE/UK/FR bases + display names + personnel (~1,000–1,300).
   FRENCH RULING NEEDED: keep 80-tank division-as-counter or re-cut to 40 (recommended).
4. **Renames while free:** `INF_REG_NL/BE/DK` → `INF_MECH_NL/BE/DK`; naming scheme adopted.
5. **Doctrine guards:** extend `CensusIntegrityTests` (carrier single-key · lift empty · trucks
   empty · classification coherence). Written here so they pass against corrected data.
6. **Doc amendments:** §24.8.7.4.3 · §10.7.1/.3 · §1a scale line · census vocabulary into the
   design doc. PrinterMessage TRN-row comment.
7. **Relay to Scenario Editor agent** (tokens changed/renamed + the doctrine).

### Bob's double-check items before the pass runs
US battle-group numbers (§2.1) · French 80-vs-40 (§2.1) · Soviet 72-tube towed regiments (§1.3) ·
`INF_SPEC_SV` 2,300→1,200 (§1.4b) · ACR helicopters (§2.1) · naming scheme (§5) ·
transition table (§6).
