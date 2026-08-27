# todo_census.md — The Census Fix Pass (hand-off plan, 2026-08-13)

**For:** the implementing agent. **Authority:** `UnitDB_CensusAudit_2026-08-13.md` (findings) + this
plan (rulings, all Bob-approved 2026-08-13). Read both before editing.
**Vocabulary:** census = a profile's authored `IntelReportStats` (Claude_Project §9). Censuses have
ZERO combat effect — they feed the intel report and the §24.8 loss ledger only.

## ⛔ SCOPE BOUNDARY (Bob's ruling — do not exceed)
Data + tests + docs ONLY: censuses, profile display names, safe enum renames, the doctrine-guard
tests, doc amendments, editor relay. **NO template Classification changes** (BMD-VDV stays AB) and
**NO mount-transition machinery** — both are P4. Zero gameplay-behaviour change is the invariant.

## ⚠ STANDING RULES
- Line numbers below are 2026-08-13; they SHIFT as you edit. **Match by profile variable name**
  (e.g. `BMP1.AddIntelReportStat`), never by raw line.
- `AddIntelReportStat` ASSIGNS (last write wins). Check the receiver variable on every block —
  the Humvee/M113 defect was a block aimed at the previous block's variable.
- **Enum renames:** `WeaponType` persists by name. A token in shipped content (`khost.oob` — Soviet
  + MJ only) must NEVER be renamed. NATO/Lowlands tokens are content-free until the first Lowlands
  export — their renames are authorized below, and ONLY those.
- Agent cannot run Unity. After each block compiles-by-inspection, say **"Please run Unity Test
  Runner for me"** and WAIT. Suites: `CensusIntegrityTests` + `EquipmentBaysTests` minimum.
- Commit per block, strips + enrichment for a faction in the SAME commit (bay-sums never regress
  mid-history). End every commit: `Co-Authored-By:` line per CLAUDE.md.

## §0 THE DOCTRINE (ruleset v2 + scale table — final)
1. Bay-sum = formation roster. 2. Carriers: own platform count only. 3. Base profile owns all else
(men, organic tanks, tubes, AT, MANPADS, recon, AD). 4. Lift (`TransportCategory != None`): EMPTY
census; lift losses unreported by design. 5. Trucks never counted. 6. One base, one formation
shape. 7. An OPEN-bay base never lists a type that could occupy its own Mobile bay (tank tokens are
fine — tanks can't ride a Mobile bay; carrier-family tokens are not). Closed-bay bases list carrier
tokens freely.
**Scale:** USSR regiment (~94 tanks) · US bn task force (58) · GE/UK Kampfgruppe/battle group
(~55–58) · **FR division-as-counter (80, Bob's ruling — name it a Division)** · NL/BE/DK brigade
(60–84, unchanged) · Arab/China as-is.

---

## BLOCK 1 — SOVIET (one commit)
Strips (rule 2/4) and enrichment together:
| Profile (var) | Census becomes |
|---|---|
| `BMP1`,`BMP2`,`BMP3` | own IFV **129** only (delete tank/BTR/BRDM lines) |
| `BTR70`,`BTR80` | own APC **129** only |
| `MTLB` | own **68** only |
| `BMD2`,`BMD3` | own **68** only |
| `MI8T` (lift) | **EMPTY** (delete the 109 line) |
| `AN12` var (= `TRN_AN8_SV`, lift) | **EMPTY** (delete the 48 line) |
| `INF_AM` | delete `HEL_MI8T_SV 166` line; rest stands |
| `INF_REG` (= `INF_REG_SV`, the de-facto MRR mech base — MRR-only, verified) | **ADD `TANK_T62A_SV 40`** (era-typical MRR organic bn; comment the choice). ⚠ Keep the NAME — it is in shipped `khost.oob`. Add a comment: "de-facto INF_MECH_SV; rename blocked by shipped content". Do NOT add BTR/BMP tokens (rule 7 — open bay). The 26 support APCs from old carrier censuses are dropped, absorbed into the carriers' 129. |
| `INF_MAR` | **ADD `TANK_T55A_SV 31`** (Naval Infantry organic tank bn — soft enrichment, comment it) |
| `INF_SPEC` | Personnel 2300 → **1200** |
| `ArtLight`,`ArtHeavy` | 72 → **48** tubes |
| Helo regiments `MI8AT`,`MI24D`,`MI24V`,`MI28` | delete `Personnel 475` lines (convention: air = aircraft only) |
Air unchanged otherwise (36/24/12/6 ✓). Tank regiment bases untouched (audit: good).

## BLOCK 2 — US (one commit)
| Profile | Census becomes |
|---|---|
| `M1_US` (TF re-cut) | Personnel **1000** · M1 **58** · M2 **13** · M3 **6** · M113 **16** · ATGM **16** · Stinger **9** · M109 **18** · mortar **9** · Vulcan **2** |
| `M2_US` (carrier) | own **54** only |
| `M113_US` (carrier, currently unused by templates) | own **108** only (keep the 2026-08-13 fixed Humvee lines OUT — verify no tank/M2/M3 lines remain) |
| `HUMVEE_US` (carrier) | own **108** only (already fixed) |
| `LVTP7_US` (carrier) | own **45** only (delete Humvee 120 + AH-1 12) |
| `HEL_UH60_US` (lift — Embarked-only, verified :2754) | **EMPTY** |
| `M60_US` (ACR sqn) | AH-64 26 → **8**, OH-58 12 → **8**; rest stands |
| `M163_US`/`Chaparral`/`Hawk_US` | fix the swapped/wrong COMMENTS (values stand — audit §2.5) |
| `AH64` | delete `Personnel 475` (air convention) |
| `INF_REG_US_P` (mech base) | Personnel 2200 → **1100** · **ADD `TANK_M1_US 28`** · ATGM 40→**20** · Stinger 22→**11**; M109/mortar/Vulcan stand |
| `INF_MAR/AB/AM_US_P` | untouched (not mech bases; scale is their own) |
Template display names (CombatUnitDB): `US_ARMOR_BRIGADE` → "US Armor Battle Group",
`US_MECH_BRIGADE` → "US Mech Battle Group" (unitName strings only; template IDs UNCHANGED —
IDs may be referenced by the editor).

## BLOCK 3 — GE/UK/FR (one commit)
| Profile | Census becomes |
|---|---|
| `LEO1_GE`,`LEO2_GE` | Personnel **1100** · own tank **55** · Marder **13** · M113 **12** · Luchs **6** · ATGM **16** · Stinger **12** · M109 **18** · Gepard **4** · mortar **6** |
| `MARDER_GE` (carrier) | own **54** only |
| `INF_REG_GE_P` (mech base) | Personnel 2600 → **1300** · **ADD `TANK_LEOPARD1_GE 28`** (comment: Leo-1 chosen as era-typical) · ATGM 40→**20** · Stinger 28→**14** |
| `CHALL1_UK` | Personnel **1000** · Chall **58** · Warrior **13** · FV432 **8** · **`SPSAM_RAPIER_UK 8` → `RCN_FV105_UK 8`** (the mis-token BUG) · ATGM **15** · M109_UK **18** · mortar **9** · `MANPAD_STINGER 16` → **`MANPAD_JAVELIN 8`** (new token, Block 5) · `MANPAD_RAPIER 6` → delete |
| `WARRIOR_UK` (carrier) | own **45** only |
| `INF_REG_UK_P` (mech base) | Personnel 2040 → **1100** · **ADD `TANK_CHALLENGER1_UK 28`** · ATGM 48→**24** · `MANPAD_RAPIER 24` → **`MANPAD_JAVELIN 12`** |
| `INF_AB_UK_P` | `MANPAD_RAPIER 24` → **`MANPAD_JAVELIN 24`** |
| `Rapier_SP` | delete `SPAAA_M163_US 4` (US Vulcans in a UK regiment) |
| `Gepard_GE` | delete `SAM_HAWK_US 4` |
| FRANCE (Bob: keep 80) | `AMX30_FR` census UNCHANGED; display name → "AMX-30 Armoured Division" and template `FR_ARMORED_BRIGADE` unitName → "FR Armoured Division (AMX-30)" (ID unchanged). `VAB_FR` (carrier) → own **135** only. `M109_FR`: keep `SPA_AUF1 48` census; change the PROFILE display names "M109…" → "AUF1 Self-Propelled Artillery"/"AUF1" so name and census agree (WeaponType `SPA_M109_FR` stays — rename discouraged). |
| `ArtLightWest`,`ArtHeavyWest` | 72 → **54** total: Light = 105mm **54** (delete 155 line) · Heavy = 155mm **54** (delete 105 line) |
| `BO105`,`AH1` | delete `Personnel 475` |

## BLOCK 4 — ARAB + LOWLANDS (one commit)
| Profile | Census becomes |
|---|---|
| `T62A` (IQ) | **DELETE `TANK_T55A_IQ 105`** — the 209-tank bug |
| `BMP1_IQ`,`MTLB_IQ` (carriers) | own **90** only |
| `M113_IR` (carrier) | own **90** only |
| `INF_REG_IQ_P` | **ADD `TANK_T55A_IQ 31`** ⚠ FIRST verify a template pairs it with a stripped carrier (grep `deployedProfile: WeaponType.INF_REG_IQ` + its mobile); if none is mech, add nothing |
| `INF_REG_IR_P` | same check → **ADD `TANK_M60A3_IR 31`** if mech-paired |
| `F4_IR`,`F14_IR` | keep 48 (Shah-era big wings — documented texture, Bob-accepted) |
| `M113_NATO` | delete `RCN_FV105_UK 8` → own **102** only |
| `LEO1_NL/BE/DK` bases | untouched (closed-bay tokens legal; brigade scale stands) |
| `INF_REG_NL_P` | **ADD `TANK_LEOPARD1_NL 32`** · `INF_REG_BE_P` +`TANK_LEOPARD1_BE 28` · `INF_REG_DK_P` +`TANK_LEOPARD1_DK 24` |
MJ + China: no changes (audit: clean/plausible).

## BLOCK 5 — ENUM + RENAMES (one commit; BEFORE any Lowlands .oob export)
1. Append census-only token **`MANPAD_JAVELIN`** to `WeaponType` (Lowlands region or a UK comment
   block; `MANPAD_` prefix → SAM bucket, guard test #2 passes automatically).
2. Rename `INF_REG_NL/BE/DK` → **`INF_MECH_NL/BE/DK`** (enum + every reference in
   WeaponProfileDB/CombatUnitDB + comments). Verified content-free; confirm with
   `grep -r INF_REG_NL Assets/StreamingAssets` = zero hits before renaming.
3. Record the naming scheme in the Lowlands region comment: `INF_LEG_*` foot / `INF_MECH_*`
   mounted; existing shipped Soviet names grandfathered.

## BLOCK 6 — DOCTRINE GUARDS (one commit, AFTER blocks 1–5 are suite-green)
Extend `CensusIntegrityTests` (LINQ style, match the existing two tests):
1. **Carrier-clean:** every WeaponType appearing in any template's Mobile bay
   (`CombatUnitDB.GetAllTemplateIds()` → templates → mobile slot) has census keys ⊆ {its own type}.
2. **Lift-empty:** every profile with `TransportCategory != None` has null/empty census.
3. **Truck-empty:** every `TRK_`/`TRN_` prefixed profile has null/empty census — then REPLACE the
   name-based `CensusExempt` allow-list with these two rules (delete the hardcoded set).
4. **Classification coherence:** every template's bay-summed census contains the bucket its
   Classification implies — MECH/MOT ⇒ TANK+APC-or-IFV buckets present · TANK ⇒ TANK · ART/SPA ⇒
   ART · SAM/SPSAM ⇒ SAM · AAA/SPAAA ⇒ AAA · RECON ⇒ RCN. Exempt: facilities, air, HQ, MJ
   (irregulars), ENG. Keep the mapping table small and commented.

## BLOCK 7 — DOCS + RELAY (one commit + one courier file)
- `HS_DesignDoc.md` (Desktop\AI_TODO\Design Docs): amend §24.8.7.4.3 (TRN row — nothing in TRN
  declares stats now, BY RULE); rewrite §10.7.1/10.7.3 (EquipmentBays + derived bays, ProfileType
  gone); §1a → "1 unit = one maneuver formation (Soviet regiment / NATO battle group / small
  brigade; France fields division-counters)"; add the census doctrine (§0 above) as a ratified
  subsection near §10.7.
- `PrinterMessage.cs` :302-313 comment: TRN-row rationale → rule 4.
- `Claude_Project.md`: §9 already anchors vocabulary; update §2.6 profile counts; note the pass.
- `Claude_TODO.md`: change-log entries per block; delete superseded flags.
- Courier file `Reply_CensusPass_<date>.md` for the Scenario Editor agent: renamed tokens
  (`INF_MECH_NL/BE/DK`), new `MANPAD_JAVELIN`, display-name changes, the doctrine, and "click
  Import UnitDB + Import WeaponTypes".

## VALIDATION LADDER
After Block 1 and after Block 4: request a suite run (census + EquipmentBays + MovementMedium).
After Block 6: full `CensusIntegrityTests` (now 6 tests) — expected GREEN against corrected data;
any failure names a profile the blocks missed: fix the DATA, never the guard. Final: Bob eyeballs
one Soviet MRR + one US TF intel panel in the editor.
