# Roster Expansion — per-nation art + WeaponProfiles

> **Historical record — execution plan superseded 2026-09-09.** Use the [current completion-plan and checklist pointers](<../Docs/Roster and Art Completion.md>). The original decisions and notes below are preserved; their status headlines and work order are not the current implementation queue.

> **Status: ALL ELEVEN NATIONS SETTLED 2026-08-29. NO CODE WRITTEN YET — see §5 for the work order.**
> Art checklist (self-saving, checkboxes persist): https://claude.ai/code/artifact/a9d4cc37-a7c9-4a68-865c-2428d8f2969c
> Opened out of the top-down icon work: Bob is authoring new unit art, which surfaced shared sprites,
> roster holes and four content errors. This file is the record; `Claude_TODO.md` carries the pointer row.
>
> ⚠ **SEQUENCING (Bob-approved): the TOP-DOWN ICON PASS LANDS FIRST** (`Top-Down Icons.md`, T-1…T-4 +
> the two guard tests). Everything here is then authored directly in the new shape — `RegimentIconType.Single`
> with one suffix-free `Icon` field. Building this first would write ~40 profiles in the old shape and
> convert them again in T-3.

---

## 1. THE RULING SET

| # | Ruling | Ratified |
|---|---|---|
| R1 | **One profile = one picture.** A WeaponProfile carries exactly one sprite, so anything needing its own art needs its own profile. The reverse is NOT true — profiles may share a sprite freely. | 2026-08-28 |
| R2 | **No icon "picker".** Art stays resolved through the profile only. A nationality-keyed override was considered and REJECTED — see §1.1. | 2026-08-28 |
| R3 | **Commodity stat lines are authored ONCE** in a shared `ProfileDef` helper and handed to each national profile. Separate profiles ≠ separate combat numbers. | 2026-08-28 |
| R4 | Sprite names are **`NATION_WEAPON`**. Helicopters add `_Frame0..5`. No directional or firing suffixes. | 2026-08-28 |
| R5 | WeaponTypes are **`CATEGORY_MODEL_NATION`**, uppercase. Generics use a role word for MODEL (`LIGHT`, `HEAVY`, `GEN`, `REG`). | 2026-08-28 |
| R6 | **No generic artillery or AAA art.** Each nation gets its own light artillery, heavy artillery and towed AAA. NL/BE/DK share one NATO set. | 2026-08-28 |
| R7 | **The `AR_` regional convention is KILLED.** Iraq and Iran each own their art. Saudi prefix is `SA_`. | 2026-08-28 |
| R8 | Bases (HQ, depot, airbase) **stay shared** — no per-nation base art. Naval flotilla likewise (Soviets only in game one). | 2026-08-28 |

### 1.1 ⚠ WHY THERE IS NO PICKER — do not re-propose one

Icon resolution is a single path: `GameIconRenderer.GetSpriteNameForUnit` → `unit.EquipmentBays.GetIcon(...)`
→ `profile.IconProfile`. The ONLY special case is `AIRB`, which short-circuits to the airbase stack badges.

A nationality-keyed picker was considered because Bob wanted "generic profiles for combat, unique graphics
per unit." It was rejected for four reasons:

1. **The per-nation profile split ALREADY EXISTS everywhere else.** `TANK_LEOPARD1_GE/_NL/_BE/_DK` are four
   profiles on one sprite; `INF_MECH_NL/_BE/_DK` are three on one. The generic artillery profiles are the
   EXCEPTION, not the rule — a picker would make artillery uniquely different from the other ~180 profiles.
2. **It creates a second home for art**, needing a precedence rule invisible at the call site. This project
   has eaten that defect repeatedly (four disagreeing is-fixed-wing lists; two hex-geometry spellings; the
   dual sprite-sorting system).
3. **It fights the icon pass**, whose T-2 collapses `RegimentIconProfile` to ONE field.
4. **Profiles buy per-nation STATS, which a picker cannot.** Bob: "the Arabs tended to have export versions."
   An export downgrade is a stat change; a shared profile can differ in picture but never in gun.

⚠ **A picker also does NOT solve Saudi Arabia.** Saudi's problem is that it has ZERO units, not that it
cannot get art. It needs profiles and templates under either architecture.

### 1.2 Naming detail

- **Nations:** `US GE UK FR NATO SV IQ IR SA CH MJ GEN`
- **Weapon token:** designation with no spaces/hyphens (`T55A`, `Leopard1`, `UH1D`, `AMX10P`, `FV432`), or a
  role word where there is no single model (`AAA`, `LightArt`, `HeavyArt`, `Truck`, `Regulars`, `AirMobile`)
- **Towed AAA sprite is `XX_AAA`** (Bob's convention). The WeaponType stays `AAA_GEN_<NATION>` to match the
  existing `AAA_GEN_SV`. ⚠ Confirm Bob did not mean the WeaponType too.
- **Trucks — SEVEN, ratified 2026-08-29:** `US_Truck` (USA + Kuwait) · `NATO_Truck` (replaces WEST; **every
  European NATO nation including Germany**) · `SV_Truck` (exists) · `CH_Truck` · `IQ_Truck` · `IR_Truck` ·
  `SA_Truck` — the last three are **desert-scheme variants** Bob is drawing. WeaponType form standardises to
  `TRK_GEN_<NATION>` (today `TRK_GEN_SV` vs `TRK_WEST` disagree).
  ✅ Both earlier truck contradictions are CLOSED: there is **no `GE_Truck`**, and Iraq and Iran get
  **separate** trucks rather than one shared. Saudi gets its own too, superseding the earlier
  "Saudi rides `US_Truck`" ruling.
- ⚠ **PREFIX vs SUFFIX — two systems, both already correct.** Unit SPRITES and their `SpriteManager`
  constants take the nationality as a PREFIX (`CH_Type59`); WeaponTypes take it as a SUFFIX
  (`TANK_TYPE59_CH`). Bob ratified the sprite-prefix rule 2026-08-29; it was already universally true, so
  no work follows from it. The shared prefixes `NATO_`, `GEN_` and `ME_Airbase` are the deliberate
  exceptions. Do not "fix" WeaponTypes to prefix form — that is a 184-member rename with no benefit.
- **Fixed-wing aircraft sprites already conform** — no renames needed there (Bob, 2026-08-28).
- **Casing is NOT being normalised.** `SV_Mig21` sits beside `SV_MI8` and `SV_SU17`; re-casing ~200 files on
  top of the icon pass's suffix strip is risk for no functional gain. New art follows the maker's spelling.
- **Template IDs are FREE to rename** — verified: `.oob` files reference WeaponTypes
  (`"DeployedProfileID": "INF_REG_MJ"`), never template IDs.

---

## 2. RENAME BATCH — APPROVED BY BOB 2026-08-28

Persisted enums (CLAUDE.md item 11): breaking, so these ride ONE `SAVE_VERSION` bump. Free right now —
`MINIMUM_SUPPORTED_SAVE_VERSION` tracks `SAVE_VERSION` pre-1.0 so no migration step is reachable, and
`SaveLoad` still has zero callers. Needs an editor DB re-import (the Chieftain forces one anyway).

| From | To | Why |
|---|---|---|
| `TRN_AN8_SV` | `TRN_AN12_SV` | profile IS the An-12; only the identifier was wrong. ⚠ **RENAME, NEVER DELETE** — see §2.1 |
| `FGT_TORNADO_IDS_UK` | `FGT_TORNADO_GE` | see §2.2 |
| `FGT_TORNADO_GR1_US` | `FGT_TORNADO_UK` | see §2.2 |
| `SPA_M109_FR` | `SPA_AUF1_FR` | the profile is an AUF1, not an M109 |
| `APC_FV432` | `APC_FV432_UK` | census-only token gaining a real profile |
| `HEL_AH1` | `HEL_AH1_US` | no nation suffix today |
| `ART_LIGHT_WEST` / `ART_HEAVY_WEST` / `TRK_WEST` | `*_NATO` | `WEST` is the only non-nation suffix |

⚠ **`TRN_AN8_SV` is the ONLY renamed type present in shipped content** (`khost.oob`, one reference) — that
file needs a re-export. The others touch code only.

### 2.1 ⚠ THE An-12 TRAP

Bob said "delete the AN8 as a WeaponProfile." That must be read as **rename**, not remove. `TRN_AN8_SV` is
the sole fixed-wing lift for all four VDV templates (`USSR_VDV_BMD2`, `USSR_VDV_BMD3`, `USSR_VDV_ART`,
`USSR_VDV_SUP`) plus one `khost.oob` unit. Deleting it grounds the Soviet airborne arm and breaks the
shipped scenario. The profile is already correct in every respect but its name.

### 2.2 THE TORNADO FIX — art pointers were right, the templates were crossed

Found state: `GE_TORNADO_FIGHTER_SQUADRON` → `FGT_TORNADO_GR1_US` → drew `UK_TornadoGR1`; and
`UK_TORNADO_FIGHTER_SQUADRON` → `FGT_TORNADO_IDS_UK` → drew `GE_Tornado`. Both profile comment headers say
"UK", so these were authored as two BRITISH variants and the German template later borrowed the spare.

⚠ **GR.1 is the RAF designation for the Tornado IDS** — same airframe, British name. The Luftwaffe flew it
as plain IDS. Bob twice stated it the other way round and corrected himself; the ratified fix is:

- **Rename each profile to the nation it already serves** (above) and **swap only the two art pointers**.
- ⚠ **NO STAT LINE MOVES.** Germany keeps DF13/OL6 on `UpgradePath.ATT`; the UK keeps DF12/OL9 with
  `HEAVY_PAYLOAD` on `UpgradePath.FGT`. Bob may later want the heavier aircraft on Germany — that is a
  separate one-line change, deliberately not bundled.
- Sprite `UK_TornadoGR1` → `UK_Tornado`; `GE_Tornado` unchanged.
- ⚠ Pre-existing and NOT fixed here: both templates are `UnitClassification.FGT`/`AirSuperiority` but only
  one Tornado profile sits on `UpgradePath.FGT`, so one nation runs an FGT unit on an ATT-path profile.

---

## 3. ⚠ FOUR CONTENT ERRORS FOUND IN THE AUDIT

| Template | Problem | Disposition |
|---|---|---|
| `UK Air Defense Regiment (M163)` | Britain never operated the M163 Vulcan. UK SHORAD was Rapier + Blowpipe/Javelin. | **DELETE the template** (Bob 2026-08-28). `SPAAA_M163_US` profile stays — the US keeps it. |
| `Iranian SAM Regiment (S-75)` | Iran never operated the SA-2; it was US/UK-equipped (Hawk, Rapier). The S-75 was Iraq's. | Pending — Iran section |
| `Iranian Air Defense Regiment` | Draws Soviet generic AAA; Iran's guns were US M167 Vulcan and Bofors. | Pending — Iran section |
| `Chinese SAM Regiment (S-125)` | China's SAM was the **HQ-2**, a copy of the S-75/SA-2, not the S-125/SA-3. | Pending — China section |

---

## 4. PER-NATION DECISIONS — SETTLED

### 4.1 USA
**New profiles:** `ART_LIGHT_US` · `ART_HEAVY_US` · `AAA_GEN_US` · `TRK_GEN_US` · `HEL_AH1_US` (retag)
**New art:** `US_LightArt` · `US_HeavyArt` · `US_AAA` · `US_Truck` · `US_AH1_Frame0..5`
**Known holes NOT yet ruled on:** no transport aircraft at all (C-130); no engineers; no special forces.

### 4.2 West Germany
**New profiles:** `APC_M113_GE` · `SPSAM_ROLAND_GE` (tracked, on Marder) · `ATT_ALPHAJET_GE` ·
`HEL_UH1D_GE` · `INF_AM_GE` · `ART_LIGHT_GE` · `ART_HEAVY_GE` · `AAA_GEN_GE` · `ROC_MLRS_GE`
**New art:** `GE_M113` · `GE_Roland` · `GE_AlphaJet` · `GE_AirMobile` · `GE_LightArt` · `GE_HeavyArt` ·
`GE_AAA` · `GE_MLRS` · `GE_UH1D_Frame0..5`
**Replace in place:** `GE_F4`. **Rename:** `GER_Regulars`→`GE_Regulars`, `GER_Airborne`→`GE_Airborne`.
**New templates:** second Panzergrenadier on the M113 · ~~air-mobile brigade~~ ✅ **BUILT 2026-09-03** ·
Alpha Jet squadron · Roland regiment · towed light + heavy artillery · towed AAA · MLRS regiment.

✅ **AIR-MOBILE SLICE LANDED 2026-09-03 (the first RE-5/RE-6 work).** `GE_AIRMOBILE_BRIGADE` —
Deployed `INF_AM_GE` / Mobile `APC_M113_GE` / Embarked `HEL_UH1D_GE`, mirroring `US_AIRMOBILE_BRIGADE`.
⚠ **ART-GATED: all three sprites are still to-draw** (`GE_AirMobile`, `GE_M113`, `GE_UH1D_Frame0..5`), so
the unit renders the mismatch placeholder until Bob's German drop lands — the deliberate `SV_2S5` state.
⚠ **`INF_AM_GE` differs from `INF_AB_GE` by exactly ONE trait** — `MOUNTAIN_TRAINED` in place of
`AIR_DROPPABLE`, the same single-trait split the US pair uses. Both halves are pinned in
`WeaponProfileNatoTests`, because without them the two profiles could silently converge into a re-skin.
⚠ **HISTORICAL NOTE, recorded not objected:** the Bundeswehr had no separate air-mobile brigade. The
Luftlandebrigaden WERE both the parachute and the helicopter-borne force, lifted by Heeresflieger UH-1Ds.
This is a gameplay slot Bob asked for on 2026-08-29, not a formation that stood on its own.
⚠ **The UH-1D is TRANSPORT ONLY** — Germany never armed its Hueys, so there is no `HEL_UH1C_GE` to match
the US and Saudi gunships. The Bundeswehr's gunship is the Bo 105 PAH-1, which is already in the game.
**Hawk stays shared** on `SAM_HAWK_US` — "not enough differentiation to matter" (Bob).
⚠ **OPEN: `GE_Truck`** — see §6.

### 4.3 UK
**New profiles:** `ART_LIGHT_UK` · `ART_HEAVY_UK` · `AAA_GEN_UK` · `ATT_JAGUAR_UK` · `INF_AM_UK` ·
`HEL_PUMA_UK` · `HEL_LYNX_UK` · `APC_FV432_UK` · `TANK_CHIEFTAIN_UK` · `FGT_F4_UK` (FG.1)
**New art:** `UK_LightArt` · `UK_HeavyArt` · `UK_AAA` · `UK_Jaguar` · `UK_AirMobile` · `UK_FV432` ·
`UK_Chieftain` · `UK_F4` · `UK_Rapier` · `UK_Puma_Frame0..5` · `UK_Lynx_Frame0..5`
**Rename:** `UK_TornadoGR1` → `UK_Tornado`. **Delete:** the M163 template (§3).
**New templates:** mech infantry on the FV432 · air-mobile infantry · Puma transport squadron · Lynx attack
squadron · Jaguar squadron · towed artillery ×2 · towed AAA · Chieftain armoured regiment · FG.1 squadron.
**Truck:** shared `NATO_Truck`.
⚠ **Lynx, not Gazelle.** Bob assumed the Gazelle; in British service that was a scout. Britain's anti-tank
helo was the TOW-armed Lynx AH.1. Bob ruled Lynx, owning its own art.

### 4.4 France
**New profiles:** `ART_LIGHT_FR` · `ART_HEAVY_FR` · `AAA_GEN_FR` · `IFV_AMX10P_FR` · `HEL_GAZELLE_FR` ·
`INF_AM_FR` · `HEL_PUMA_FR`
**New art:** `FR_LightArt` · `FR_HeavyArt` · `FR_AAA` · `FR_AMX10P` · `FR_AirMobile` · `FR_VAB` ·
`FR_Crotale` (wheeled) · `FR_AUF1` · `FR_Puma_Frame0..5` · `FR_Gazelle_Frame0..5`
**Delete:** the six orphan `FR_Gepard` PNGs + constants (France never operated the Gepard).
**New templates:** mech infantry on the AMX-10P (the existing brigade keeps the VAB) · air-mobile infantry ·
Puma transport · Gazelle attack squadron · towed artillery ×2 · towed AAA.
**Truck:** shared `NATO_Truck`.
⚠ **Crotale is WHEELED and the code already says so** (`MovementMedium.Wheeled`). SPSAM in this codebase
means self-propelled, not tracked. The French Army Crotale was the wheeled P4R; the tracked one was the
Saudi Shahine on an AMX-30 hull — which is Saudi's SAM, so that art serves both.

### 4.5 NATO generic (Netherlands, Belgium, Denmark draw from this)
**New profiles:** `ART_LIGHT_NATO` · `ART_HEAVY_NATO` · `AAA_GEN_NATO` · `IFV_YPR765_NATO` ·
`FGT_F16_NATO` · `TRK_GEN_NATO`
**New art:** `NATO_LightArt` · `NATO_HeavyArt` · `NATO_AAA` · `NATO_YPR765` · `NATO_F16` · `NATO_Truck`
`APC_M113_NATO` stays alongside the YPR — Denmark ran plain M113s. `NATO_Regulars` infantry unchanged
("NATO infantry is fine" — Bob).
⚠ Today NL/BE/DK templates badly outrun their profiles: the YPR-765 and AIFV have NO profile at all, and
all three borrow Britain's `RCN_FV105_UK` for recon. Filling NATO generic resolves most of it; **NATO recon
is still unruled.**

### 4.6 Already agreed, not nation-specific
- **Free art re-points** (art exists on disk, profile points elsewhere — no new art needed):
  `US_AirMobile` for `INF_AM_US` · `CH_LightArt`/`CH_HeavyArt` for `ART_LIGHT_CH`/`ART_HEAVY_CH` ·
  `SV_AA`→`SV_AAA` for `AAA_GEN_SV`. ⚠ `AR_HeavyArt`/`AR_LightArt` are SUPERSEDED by R7 (the AR_ split).
- **Soviet:** `TANK_T55MV_SV` + `TANK_T62MV_SV`, Gen1 + `ERA_LIGHT` (the trait already exists — T-72A uses
  it). **Soviet only — Bob ruled no MV for the Arabs.**
- **Iraq:** `FGT_MIRAGEF1_IQ` — art `IQ_MirageF1` ALREADY EXISTS on disk. Export downgrade via DF/SUR
  residuals, not the `EXPORT_DOWNGRADE` trait (that is tanks-only per the design flags).
- **China:** `APC_TYPE63_CH` (the YW531 tracked APC — art `CH_Type63` already exists) + `TRK_GEN_CH`.
  ⚠ The Type 63 is NOT a 2S1 variant; China's 122mm SPH is already `SPA_TYPE82`, correctly arted.
- **PHZ-89 is Chinese** (Type 89 122mm SP MRL), left exactly as is per Bob.

### 4.6a ⚠ FOUND 2026-08-29 — THE SOVIET 2S5 HAS NO ART AT ALL
`SPA_2S5_SV` ("2S5 Giatsint-S Self-Propelled Artillery") has a registered profile AND a template
(`Self-Propelled Artillery Regiment (2S5)`), and `SpriteManager` declares all six `SV_2S5_*` constants —
but **not one 2S5 PNG exists anywhere under `Assets/Art`.** The unit renders the mismatch placeholder in
play today, and has done since before this pass.

⚠ **`IconIntegrityTests` cannot catch this and never will.** It proves an icon NAME is present and valid;
whether a file backs that name is a Unity asset question invisible to a headless EditorTest. This is
exactly the class of defect Bob's requested directory cross-check exists for — and it found one on the
first pass, before the new art has even landed.

Surfaced by a `SpriteManager` audit: of 814 constants, only ten resolve to no file — the six `SV_2S5_*`,
three `Weather_*` (planned, weather is single-state) and the `VoidSpriteName` sentinel ("None", correct).
**Added to the manifest as `SV_2S5` / status NEW.**

### 4.7 Soviet Union — CLOSED 2026-08-29
Most complete nation in the game; everything the Afghan setting needs is already present. Bob declined the
optional Mi-6 heavy lift, the towed AT gun and the BTR-60.
**New profiles:** `TANK_T55MV_SV` · `TANK_T62MV_SV` — Gen1 + `ERA_LIGHT` (trait already exists, T-72A uses it)
**New art (2 only):** `SV_T55MV` · `SV_T62MV`
**Renames/re-points, no new art:** `SV_AA`→`SV_AAA` (+ `AAA_GEN_SV` off `GEN_AA`) · `SV_AN8`→`SV_AN12`

### 4.8 Iraq — CLOSED 2026-08-29
**Split art required by R7 (15 sprites):** `IQ_T55` · **`IQ_T62`** *(genuinely missing — the T-62 profile
draws the T-55 sprite today)* · `IQ_BMP1` · `IQ_MTLB` · `IQ_2S1` · `IQ_ZSU57` · `IQ_2K12` · `IQ_Mig21` ·
`IQ_Mig23` · `IQ_SU17` · `IQ_S75` · `IQ_LightArt` · `IQ_HeavyArt` · `IQ_AAA` · `IQ_Truck`
**New profiles + art:** `TANK_T72M_IQ` + `IQ_T72` (deliveries began Jan 1982 — Poland, then Soviet exports
that September; the Republican Guard's spearhead and Iraq's only modern tank) · `HEL_MI24_IQ` +
`IQ_Mi24_Frame0..5` (Iraq had NO helicopters at all before this) · `RCN_BRDM2_IQ` + `IQ_BRDM2`
**Profile only, art already on disk:** `FGT_MIRAGEF1_IQ` → `IQ_MirageF1`
**Declined by Bob:** Gazelle, Mi-8 transport, BTR-60/EE-11, Chinese Type 59/69.

### 4.9 Iran — CLOSED 2026-08-29
**Split art required by R7 (8 sprites):** `IR_M60` · `IR_M113` · `IR_F4` · `IR_F14` · `IR_LightArt` ·
`IR_HeavyArt` · `IR_AAA` · `IR_Truck`
**New profiles + art:** `TANK_CHIEFTAIN_IR` + `IR_Chieftain` (~875 Mk3/Mk5 — Iran's signature vehicle; the
UK Chieftain profile is being built anyway, so this may point at `UK_Chieftain` until Iranian art exists) ·
`HEL_AH1_IR` + `IR_AH1_Frame0..5` (largest export operator, ~200 airframes; may ride `US_AH1` frames
initially) · `FGT_F5_IR` + `IR_F5` · `SPA_M109_IR` + `IR_M109` · **`HEL_UH1_IR` + `IR_Huey_Frame0..5`**
(⚠ see §6.7 — "I probably need to do a Huey too. Book it.") · `RCN_M113_IR` — recon unit; same vehicle, so
it can point at `IR_M113` and needs no extra sprite
**Shared, zero art:** `SAM_HAWK_US` for the Iranian Hawk regiment
**Content fixes (§3):** DELETE `Iranian SAM Regiment (S-75)` — Iran never operated the SA-2 — and repoint
`Iranian Air Defense Regiment` off Soviet generic AAA onto `AAA_GEN_IR`.

### 4.10 China — CLOSED 2026-08-29
**Setting ruled by Bob: the REFORMED Group Army, late 1980s** — "seeing Soviet aggression, they push the
reform harder and more quickly." The 1985–87 reform cut 35 infantry corps to 24 Group Armies and folded
formerly independent armour/artillery/AD arms into combined-arms formations. So Chinese infantry **keeps**
its organic armour; the quality gap is carried by ICM, not by stripping equipment.

**NEW TRAIT `SECOND_LINE_FORMATION` — §13 Formation quality, `Icm(0.9f)`.** The mirror of `NATO_FIRST_LINE`
(×1.10). Deliberately generic in name because Iraq and Iran are future candidates — but **applied to China
ONLY for now.** Result: China 0.9 · Soviet 1.0–1.21 · NATO 1.21–1.53.
⚠ **CLOSED-BAY DOCTRINE GOVERNS WHERE IT GOES.** The ICM prices the whole formation only on a unit's SOLE
profile; Mobile/Embarked-bay profiles stay 1.0. So the trait goes on deployed profiles (tanks, IFV,
artillery, AAA, SAM, aircraft, infantry) and **NOT** on the Type 63 APC or the truck when they ride a
transport bay. Blanket-applying it would double-dip.
⚠ Doc obligation, same pattern as the 2026-08-22 ICM pass: HS_DesignDoc §13 amendment + a
`WeaponTrait_Supplement` entry + ICM pins in the Chinese profile suite.

**Delete:** `TANK_TYPE95` — no such tank (Type 96 is 1997, Type 99 is 2001). 3 art files.
**Rename:** `HEL_H9` → `HEL_Z9_CH` — "H" is *Hongzhaji*, bomber; there is no H-9 helicopter. The real
aircraft is the Harbin Z-9 (licence Dauphin, in service 1982; armed Z-9W ~1987). · `SPA_TYPE82` →
`SPA_TYPE83_CH` — the Type 82 is a 130mm MRL; China's mid-80s 152mm SPH is the PLZ-83.
**Content fix (§3):** the Chinese SAM regiment moves off the borrowed `SAM_S125_SV` onto a new
`SAM_HQ2_CH` — China's area SAM was the HQ-2, a domestic SA-2 copy.
**New profiles:** `TANK_TYPE69_CH` (accepted 1982) · `TANK_TYPE62_CH` (light tank) · **`FGT_J6_CH`**
(MiG-19 copy, ~3,000 airframes — the most numerous PLAAF fighter of the period and the roster's biggest
hole) · `AAA_GEN_CH` · `SAM_HQ2_CH` · `APC_TYPE63_CH` · `TRK_GEN_CH`
**New art (6):** `CH_Type69` · `CH_Type62` · `CH_J6` · `CH_AAA` · `CH_HQ2` · `CH_Truck`
**Free re-points, art already on disk:** `CH_LightArt` · `CH_HeavyArt` · `CH_Type63`
**Kept:** Type 59 (backbone, 10,000+ built), Type 80, Type 86, PHZ-89, HQ-7, J-7, J-8, Q-5, H-6, infantry,
airborne. `SPAAA_TYPE53` keeps its name — Bob's call, still an SPAAA.

**THREE SECOND-LINE TEMPLATES (Bob 2026-08-29)** so a scenario can mix fodder with quality:
Chinese Tank Regiment (Second Line) on the Type 59 · Chinese Motorised Regiment (Second Line) on infantry +
Type 63 · Chinese Fighter Regiment (Second Line) on the J-6.
⚠ **Implemented via `ExperienceLevel` ON THE TEMPLATE, not via new profiles.** ICM lives on the profile, so
two templates sharing a profile necessarily share its ICM — experience is the only per-template lever, and
it already exists. Raw (0.8×) or Green (0.9×) for second line, Trained/Experienced for the quality units.
Against the national 0.9 that lands second-line Chinese at ~0.72 of baseline and quality Chinese at ~0.99 —
parity with a Trained Soviet unit. Template IDs are free to add (nothing in content references them).

**Census:** `INF_REG_CH` personnel **2,200 → 2,900** — PLA infantry regiments were rifle-heavy, which puts
them above the Soviet 2,523. Organic armour STAYS under the reformed model; `TANK_TYPE59`'s 80 tanks is
already accurate and is unchanged.
⚠ **Mass belongs in the scenario OOB, not in fatter censuses.** PLA strength was the NUMBER of formations,
not denser ones — Chinese divisions matched Soviet headcount and carried a fraction of the equipment. More
counters on the map is the accurate expression and costs nothing.
⚠ Any density edit must keep `CensusIntegrityTests` rule 2 (own-platform-count) — do not inflate carriers.

---

### 4.11 Saudi Arabia — CLOSED 2026-08-29 (was greenfield: zero profiles, templates and art)
Grounded in the verified 1980–83 inventory. Reuses five existing profiles, so the art bill is 8, not 12.
**Shared outright, ZERO art:** `SAM_HAWK_US` (Hawk — as Germany and the Netherlands already do) ·
`SPAAA_M163_US` (M163 Vulcan — which also gives that profile a home now the UK template is deleted) ·
the transport helo rides the **shared Huey art** (§4.12a).
**Own truck:** `TRK_GEN_SA` + `SA_Truck`, desert scheme (2026-08-29 — supersedes the earlier
"Saudi rides `US_Truck`" ruling).
**Roster — 12 own sprites (expanded 2026-08-29, Bob took nearly all the optionals):** `TANK_AMX30_SA` +
`SA_AMX30` (190 delivered 1973–79) · `SPSAM_SHAHINE_SA` + `SA_Shahine` (**the tracked Crotale** — AMX-30
hull, in service 1980) · `FGT_F15_SA` + `SA_F15` (delivered from January 1982) · `FGT_F5_SA` + `SA_F5` ·
`INF_REG_SA` + `SA_Regulars` · **`INF_GUARD_SA` + `SA_Guard`** (the National Guard was a genuinely separate
force on V-150 carriers) · `ART_LIGHT_SA` + `SA_LightArt` · `ART_HEAVY_SA` + `SA_HeavyArt` · `AAA_GEN_SA` +
`SA_AAA` · **`SPA_AUF1_SA` + `SA_AUF1`** (own art — NOT shared with France, Bob's call) ·
**`APC_M113_SA` + `SA_M113`** (own art) · **`IFV_AMX10P_SA` + `SA_AMX10P`** (Saudi skin of the French vehicle)
**Still optional:** `SA_Lightning` (flew to ~1985).
⚠ **No attack helicopter** — Saudi's AH-64s arrived in the 1990s; nothing to draw.
⚠ **Kuwait gets NOTHING** — Bob will "plunk a US unit in Kuwait."

### 4.12 Mujahideen — CLOSED 2026-08-29
Approved as it stands; complete for irregulars. One change only: `MJ_AA` → **`MJ_AAA`** per the towed-AAA
naming convention. Regulars, commandos, horse cavalry, RPG teams, light artillery, heavy mortar and Stinger
teams all already have their own art.

### 4.12a THE HUEY — REVISED 2026-09-02, three nations now OWN their sets
⚠ **SUPERSEDES the original one-shared-set ruling.** Bob: *"give the UH-1 transport version and gunship
versions to NATO, the US, and SA their own versions, owned by each."* Six new sprite sets, **36 frames**:

⚠ **REVISED SAME DAY, after Bob looked at the art.** NATO generic no longer owns a set — it borrows the
US pair — and Iran does, because its Hueys are getting a desert reskin. Net: **four** sets, 24 frames.

| Owner | Transport | Gunship | Note |
|---|---|---|---|
| USA | `US_UH1_Frame0..5` | `US_UH1C_Frame0..5` | Owns both |
| NATO generic | *(shares `US_UH1`)* | *(shares `US_UH1C`)* | Profiles are NATO's, art is America's — R1 sharing |
| Saudi Arabia | `SA_UH1_Frame0..5` | `SA_UH1C_Frame0..5` | Owns both |
| Iran | `IR_UH1_Frame0..5` | — | **Desert reskin — must own it.** Off `GE_UH1D` as of today |
| West Germany | `GE_UH1D_Frame0..5` | — | Owns it; now its only rider |

⚠ Iran's gunship slot stays empty on purpose: Iran's attack helo is the **AH-1J Cobra** (`IR_Cobra`,
§4.9), which it flew in numbers. An `IR_UH1C` would be a second gunship it never operated.

⚠ **The gunship is a DIFFERENT AIRFRAME, not a repaint.** Transport = long-body UH-1D/H (Bell 205);
gunship = **short-body UH-1C** (Bell 204) — shorter cabin, different rotor mast, visibly distinct at icon
scale. This is the one place the parent/variant pattern of §4.12b does NOT apply.

**`GE_UH1D_Frame0..5` is now Germany's alone.** Saudi left it when it took its own pair; Iran leaves it for
the desert reskin. The profile split is unchanged — `HEL_UH1D_GE` and `HEL_UH1_IR` were always separate
profiles, and only the art pointer moves.

⚠ **Britain is NOT on any Huey list.** The RAF and Army Air Corps never flew them; their medium lift was
the Puma and the Wessex, which is why the UK carries `HEL_PUMA_UK` + `UK_Puma`.

**Two accuracy notes, recorded but NOT objections — Bob's call stands:**
- **`NATO_UH1` is fielded by nobody historically.** None of NL / BE / DK flew Hueys: Dutch aviation was
  Alouette III then Bo-105, Belgian was Alouette II + A109, Danish was Hughes 500 + Lynx. It is a gameplay
  asset, which is fine — it just will not appear in a real 1980s NATO OOB.
- **The US already covers both roles** (`US_UH60` transport, `US_AH1` gunship); by the 1980s the American
  gunship WAS the Cobra and armed Hueys were a Vietnam-era holdover. The UH-1 pair is therefore an
  older/cheaper tier, not a replacement.

✅ **CLOSED same day:** Iran gets `IR_UH1`. Bob: *"Iran is going to get desert reskinned versions, so Iran
must own its own."* Which is also the historically strongest claim on the list — ~300 AB-205s.

### 4.12b Iran addendum — the scout M113
Bob is drawing a **desert-scheme M113** for the Iranian recon unit, so `RCN_M113_IR` gets its OWN sprite
**`IR_M113Recon`** (name confirmed 2026-08-29) rather than sharing `IR_M113`. Supersedes the §4.9 note that
the recon unit needs no extra sprite.

⚠ **This sets the project-wide pattern: a recon variant is a VARIANT OF ITS PARENT CARRIER, not a separate
vehicle.** `IR_M113` / `IR_M113Recon` and `NATO_YPR765` / `NATO_YPR765Recon` read identically, and the second
sprite of each pair is a light edit of the first.

---

## 5. IMPLEMENTATION ORDER — the code work, sequenced

⚠ **RE-0 first: the TOP-DOWN ICON PASS ships as its own commit** (`Top-Down Icons.md`, T-1…T-4 + the two
guard tests). Everything below is then authored directly as `RegimentIconType.Single`.

**RE-1 through RE-4 need NO ART and can run back-to-back the moment RE-0 lands.**

| Step | Work | Art-gated? |
|---|---|---|
| ~~**RE-1a**~~ | ✅ **DONE 2026-08-29 — suite GREEN, Khost loads clean (Bob-run).** 20 renames / 169 replacements across 6 files; Tornado designations + art pointers swapped with NO stat movement; `TANK_TYPE95` deleted (enum + profile + template + 3 test pins); `UK_AIR_DEFENSE_REGIMENT` and `IR_SAM_REGIMENT` templates deleted as content errors; 6 `FR_Gepard` constants deleted; `HEL_AH1_US` moved out of the enum's Non-Profile region (it has a real profile); **`SAVE_VERSION` 9 → 10**. Result: 183 profiles, 183 templates, 212 WeaponType members, no duplicates, no dangling references. | No |
| **RE-1b** | **`TRN_AN8_SV` → `TRN_AN12_SV` — the ONE renamed type in shipped content** (`khost.oob`). Rides the next editor re-export as its own bump. ⚠ Verified 2026-08-29: it is the only one; every other rename was code-only, which is what let RE-1a proceed without touching content or the editor. | No, but content-gated |
| **RE-2** | **Free re-points + the Tornado fix.** `US_AirMobile` · `CH_LightArt` / `CH_HeavyArt` · `SV_AA`→`SV_AAA` for `AAA_GEN_SV`. Swap the two Tornado `deployedProfile` lines and the two art pointers; **move no stat lines** (§2.2). Repoint `Iranian Air Defense Regiment` onto `AAA_GEN_IR`. | No |
| ~~**RE-3a**~~ | ✅ **DONE 2026-08-29 — suite GREEN (Bob-run 2026-09-02).** Four blueprint methods added to `WeaponProfileDB` — `LightTowedArtilleryDef` · `HeavyTowedArtilleryDef` · `TowedAaaDef` · `TransportTruckDef` — and **12 existing commodity profiles re-expressed through them** (4 light, 4 heavy, 1 AAA, 3 truck). The three byte-identical light-artillery copies are gone. NEW `CommodityProfileTests` (6) pins that every national member of a family resolves to ONE stat line, that only the light gun keeps its `AirDroppable`/`HeloTransportable` tags, and that the two Mujahideen irregular variants stay DIFFERENT. Zero behaviour change — the blueprints reproduce the authored lines exactly. | No |
| **RE-3b** | **Mint the ~28 new national commodity profiles.** ⚠ **RE-ORDERED 2026-08-29 — this is NOT art-free after all, and it partly depends on RE-5.** Two findings from RE-3a: (1) each profile needs a `SpriteManager` constant, so it wants its sprite name settled; (2) **a census is genuinely NATIONAL and cannot come from a blueprint** — `ART_LIGHT_NATO` carries 950 men + `ART_105MM_FG` + `APC_HUMVEE_US`, while `ART_LIGHT_ARAB` carries 700 + `APC_MTLB_IQ` + `MANPAD_STRELA`. A Saudi census would name `APC_M113_SA`, which RE-5 mints. So: do RE-3b per-nation as that nation's equipment and art land, not as one batch. | Yes |
| ~~**RE-4**~~ | ✅ **DONE 2026-09-02 — suite GREEN (Bob-run same day).** `SECOND_LINE_FORMATION` added to §13 Formation quality at `Icm(0.9f)` and applied to **15 Chinese profiles** — every one a template names in its DEPLOYED bay. ⚠ **`IFV_TYPE86_CH` deliberately excluded**: it exists only as the Mobile-bay ride of the mechanised regiment, so under closed-bay doctrine the formation would be priced twice. Result: China 0.90 across the board, Type 80 0.945 (LRF 1.05 × 0.9), Type 86 unchanged at 1.00. The four commodity blueprints now take `params WeaponTrait[]` via a new `Plus()` helper, so a national trait rides on a shared stat line without forking it. Docs amended: `HS_DesignDoc` §7.5.5 + `WeaponTrait_Supplement` §13b (new T91 row; the "Chinese formations stay ICM 1.0" anchor explicitly retired). Tests: 5 existing Chinese ICM pins corrected, **3 new** — the 16-profile sweep, the China-only tripwire, and a blueprint guard proving the Chinese gun keeps the Soviet stat line while carrying a different ICM. | No |
| **RE-5** | **New national profiles**, per nation, as Bob's sprites land. Order of value: Iraq (T-72M, Mi-24, BRDM-2) → Iran (Chieftain, Cobra, F-5, M109, scout M113) → UK (Chieftain, FV432, Rapier, Jaguar, FG.1, Lynx, Puma) → Germany → France → NATO → China → Saudi → Soviet MVs. | Yes, per nation |
| **RE-6** | **New templates**, including the three Chinese second-line variants (`ExperienceLevel` Raw/Green — NOT new profiles, §4.10) and the second Panzergrenadier / UK FV432 / French AMX-10P mech options. Template IDs are free — no content references them. | Follows RE-5 |
| **RE-7** | **Census edits.** `INF_REG_CH` 2,200 → 2,900. ⚠ Keep `CensusIntegrityTests` rule 2 (own-platform-count) intact. | No |
| **RE-8** | **Tests.** The icon-integrity guard from RE-0 covers the new profiles automatically; add ICM pins for the 0.9 trait and re-run `CensusIntegrityTests` + the four per-faction profile suites. ⚠ Bob-run — the agent cannot run Unity. | No |

---

## 6. OPEN QUESTIONS FOR BOB

1. ✅ **CLOSED 2026-08-29 — no `GE_Truck`.** "All trucks for NATO (excluding US) should just be NATO_Truck.
   We can safely remove the GE_Truck" (Bob). Germany rides `NATO_Truck`.
2. ✅ **CLOSED 2026-08-29 — Iraq, Iran and Saudi each get their own desert truck.** See §1.2.
3. ✅ **CLOSED 2026-08-29 — YES, suffix all thirteen.** Bob: "make them consistent with the other nations.
   Having strict consistency is the way to go." `TANK_TYPE59/80` (95 is deleted), `IFV_TYPE86`,
   `SPA_TYPE82`→`SPA_TYPE83_CH`, `SPAAA_TYPE53`, `SPSAM_HQ7`, `ROC_PHZ89`, `FGT_J7/J8`, `ATT_Q5`, `BMB_H6`,
   `HEL_H9`→`HEL_Z9_CH` all take `_CH`. **Joins the RE-1 rename batch** — code-only, no shipped content
   references any of them.
4. ✅ **CLOSED 2026-09-02 — THE `XX_XXXX` RULE IS ABOUT ART, NOT CODE.** Bob: *"I was only referring to
   the sprite constants and the sprite names as the form NATION_PLATFORM (XX_XXXX). The naming conventions
   for the WeaponTypes is completely up to you."* So: **sprites** are `NATION_Platform` (`CH_Type59`,
   `IR_UH1`); **WeaponTypes** keep the existing `ROLE_PLATFORM_NATION` form (`AAA_GEN_CH`, `TANK_TYPE59_CH`).
   The towed-AAA recording stands as `AAA_GEN_<NATION>` + sprite `XX_AAA`.
   ⚠ **The two conventions run in OPPOSITE directions and that is correct** — nationality leads in art
   because files sort by nation, and trails in code because the enum sorts by role. Do not "fix" either to
   match the other; §1.2 carries the same warning.
5. **NATO generic recon** — NL/BE/DK all borrow `RCN_FV105_UK` today. Unruled.
6. **Orphan art still undecided:** `CH_Type63` is now spoken for (APC); `IQ_MirageF1` is spoken for. Nothing
   else outstanding — the `FR_Gepard` six are marked for deletion.
7. ✅ **CLOSED 2026-09-02 — FOUR Huey sets, not one and not six** (§4.12a). The US and Saudi each own a
   transport + gunship pair; **NATO generic borrows the US art** (profiles still its own); **Iran owns
   `IR_UH1`** because its Hueys are a desert reskin; Germany keeps `GE_UH1D` and is now its only rider.
   Britain remains off every Huey list. 24 new frames.

---

## 7. THE MASTER GRAPHICS LIST

**PUBLISHED 2026-08-29 as a self-saving Artifact:**
https://claude.ai/code/artifact/a9d4cc37-a7c9-4a68-865c-2428d8f2969c

Every sprite in the game listed by nation, each row tagged **new / on disk / rename / delete**, with
checkboxes whose state persists (the page republishes itself via the `artifact` runtime capability, with a
`localStorage` fallback for a read-only view). Filters for to-draw / renames / deletions / unticked.
⚠ Saudi Arabia and Mujahideen rows are marked **provisional** until §5 closes. To update it, republish the
same scratchpad file path from this conversation, or pass that URL as `url` from another one.

---

## 8. PROGRESS LOG

- **2026-09-03, later** — **THE MASTER LIST IS NOW VERIFIED, not just asserted.** Bob: *"This is my master
  list to follow."* So it was audited against the code rather than added to: 172 sprite-bearing profiles
  walked, every `SpriteManager` constant resolved, cross-checked against both the manifest rows and the
  PNGs on disk. Zero dangling constants, zero genuine gaps, zero false On-disk rows. The 22 code names
  with no row are all pre-rename spellings whose replacements are already listed — which is itself the
  proof that §2's rename batch is completely captured here.
  Two findings kept: **`SV_2S5` is ticked but has no file anywhere** (row annotated; the tick left alone),
  and **`ME_Airbase` is the only unit profile drawing from `Map Icons/` instead of `Unit Icons/`** — a
  standing trap for T-4/T-5, whose suffix sweep is scoped to `Unit Icons/` alone.
  ⚠ The audit ran as a throwaway script, NOT committed. Its durable home is an Editor test using
  `AssetDatabase.FindAssets` — the one check `IconIntegrityTests` cannot make. Offered, not built.

- **2026-09-03** — **FIRST RE-5/RE-6 SLICE: the German air-mobile brigade.** Three profiles
  (`INF_AM_GE`, `APC_M113_GE`, `HEL_UH1D_GE`), three WeaponType members, eight `SpriteManager` constants
  and one template. Now **186 profiles / 183 templates / 215 WeaponType members**.
  ⚠ **COUNT CORRECTION:** the RE-1a summary recorded "183 profiles / 183 templates". Profiles were right;
  templates were **182**, not 183. Verified by counting call sites before and after this change. The
  corrected figures are above.
  Two things worth keeping: (1) the three existing `EquipmentBaysTests` template audits walk the REAL
  database, so the new three-bay unit is swept automatically — no new bay test was needed, which is what
  a database-walking audit is FOR; (2) `APC_M113_GE` is the same vehicle as `APC_M113_US` and is pinned
  to resolve identically — it exists only because art lives on the profile, so divergence is a bug, not a
  national variant.
  ⚠ Art-gated: all three sprites are still to-draw, so the unit renders the placeholder until they land.

- **2026-09-02, later** — **RE-4 suite GREEN (Bob-run).** RE-1a and RE-3a cleared in the same run. The
  console audit that followed found nothing wrong with the roster work, but did surface a test-quality
  hole worth remembering here because it is the same failure mode this plan keeps guarding against:
  **three tests were named for a warning they never asserted**, so deleting the warning would have left
  them green with the guard gone. Now pinned with `LogAssert.Expect`. See `Claude_TODO.md` for the detail.

- **2026-09-02** — **RE-4 LANDED: `SECOND_LINE_FORMATION`.** The formation-quality layer gains its first
  sub-1.0 member and China leaves the ICM-1.0 baseline. 15 of the 16 Chinese profiles carry it; the Type 86
  IFV does not, because it is a Mobile-bay ride and closed-bay doctrine prices the formation once, on the
  sole/deployed profile. Two findings worth keeping: (1) the commodity blueprints from RE-3a had to learn
  an optional trait argument — a national trait must ride ON a shared stat line, never fork it, so
  `Plus()` was added and the blueprint tests now pin that the Chinese gun still fires the Soviet shell;
  (2) **`SPA_TYPE83_CH` has a profile but no template** — it is the only Chinese profile nothing fields.
  It got the trait on deployed-intent; the missing SPH regiment is an RE-6 item.
  Docs: `HS_DesignDoc` §7.5.5 and `WeaponTrait_Supplement` §13b both amended — the latter's
  "Soviet/Chinese/Arab formations stay the ICM-1.0 baseline" anchor was a direct contradiction and is now
  retired in place rather than quietly left standing.

- **2026-09-02** — **HUEY SPLIT (§4.12a rewritten).** Bob: the US, NATO generic and Saudi each own a
  transport + gunship pair. Six new sprite sets / **36 frames** added to the master Artifact
  (`US_UH1`, `US_UH1C`, `NATO_UH1`, `NATO_UH1C`, `SA_UH1`, `SA_UH1C`), published as `uh1-per-nation` with
  Bob's 101 existing ticks preserved. Gunship recorded as the **short-body UH-1C**, a separate drawing
  rather than a repaint of the long-body transport. Germany + Iran still share `GE_UH1D`. Two accuracy
  notes recorded without objection (no NATO-generic nation flew Hueys; the 1980s US gunship was the Cobra).
  **Revised the same day once Bob saw the art:** NATO generic drops its own pair and borrows the US art
  (its profiles stay its own); Iran takes `IR_UH1` because its Hueys are a desert reskin. Four owned sets,
  **24 frames**, not six/36 — and the Iran gap the accuracy note flagged is now closed. Also closed:
  the `XX_XXXX` convention governs SPRITES only; WeaponType names are the agent's call (§6.4).
  Profiles are RE-5 work and stay art-gated — no code written for the Hueys today.

- **2026-08-29** — **ALL ELEVEN NATIONS CLOSED.** Soviet (2 sprites only), Iraq (15 split + T-72M + Mi-24 +
  BRDM-2), Iran (8 split + Chieftain/Cobra/Hawk/F-5/M109 + scout M113), China (reformed Group Army; Type 95
  deleted as fictional, Z-9 and Type 83 renames, HQ-2 fix, J-6 + Type 69 + Type 62 added, `INF_REG_CH`
  2,200→2,900), Saudi Arabia (8 core sprites, five profiles shared from the US and French sets), Mujahideen
  (approved as-is, one rename). NEW `SECOND_LINE_FORMATION` trait at ICM 0.9 for China, with three
  second-line templates driven by `ExperienceLevel` rather than new profiles — ICM lives on the profile, so
  templates sharing one necessarily share its ICM. Shared-Huey ruling (§4.12a) saves 12 helo frames. The
  master art checklist shipped as a self-saving Artifact. §5 rewritten as the implementation work order:
  RE-1 through RE-4 need no art at all and can run straight after the icon pass.
  **Later the same day — trucks ratified at SEVEN** (§1.2): `US_Truck` (+ Kuwait) · `NATO_Truck` for every
  European NATO nation including Germany · `SV_Truck` · `CH_Truck` · plus desert-scheme `IQ_Truck`,
  `IR_Truck` and `SA_Truck`. Both truck contradictions closed; `GE_Truck` is dead and Saudi no longer rides
  `US_Truck`. Bob also ratified the sprite nationality-PREFIX rule — already universally true, so no work
  follows; recorded in §1.2 alongside the warning not to "fix" WeaponTypes into prefix form.

- **2026-08-28** — Pass opened; decisions recorded, no code. Session covered: the shared-sprite audit
  (11 shared sets, 17 profiles on borrowed art); the profile-vs-picker architecture question (settled, §1.1);
  the naming conventions (§1.2); the rename batch (§2, Bob-approved); four content errors (§3); the
  nation-by-nation roster audit; and settled art/profile lists for USA, West Germany, UK, France and NATO
  generic. Two Bob self-corrections recorded: the Tornado GR.1/IDS assignment (§2.2) and the `WG_`→`GE_`
  prefix. Two later-supersedes-earlier contradictions flagged for confirmation (§6.1, §6.2).
  Integrity check run: all 184 profiles have an icon, and every `Helo_Animation` profile satisfies the
  `_Frame0` flipbook contract.
