# Unit DB Expansion — Authoring Spec: NATO Lowlands + Gulf Contingents

**From:** Scenario Editor agent (Cowork) · **Date:** 2026-08-12
**For:** the game-side agent · **Requested by:** Bob
**Audited against:** the live `HS_GameApp` tree, 2026-08-12. Every sprite constant, WeaponType,
Nationality and trait named below was verified to exist (or is explicitly marked NEW).

**Why this exists:** Bob is authoring a North German Plain map and needs credible NATO OPFOR.
`Nationality.NE` / `BE` / `DE` have existed, with flags, national symbols and NATO-blue icon bases
wired, since before this conversation — **the plumbing was built and the units were never authored.**
This spec fills that gap and lays the groundwork for a Gulf theatre.

**Scope split, per Bob:** the Lowlands (§2–§4) are specified to implementation detail. The Gulf (§5)
is a framework and token plan — "start laying plans" — not finished censuses.

**Delivery:** Bob chose spec-over-direct-edit, so nothing here has been written into your files. All
line numbers are yours as of today.

---

## 0. Six findings you'll want before reading the tables

1. **`IntelReportStats` is also the loss ledger.** `GameDataManager.BookLosses:302` multiplies
   `lostFraction × TotalIntelStats[type]`. A census is not decoration — a wrong one produces a wrong
   casualty report, and an **empty** one means a unit can be annihilated and contribute nothing.
2. **Bay summing double-counts.** `EquipmentBays.BuildIntelStats:470-476` sums Deployed + Mobile +
   Embarked. Every carrier profile must therefore omit `Personnel` — which existing ones correctly do —
   **and must not list a tank battalion**, which existing ones incorrectly do (see §7.2).
3. **The player never sees a WeaponType.** `ClassifyWeaponType` buckets by name prefix and the enemy
   view merges further (ART+ROC → "guns", SAM+AAA+AT → "AA"). Census *identity* is invisible; census
   *magnitude* is what reaches the player. Mint tokens for correctness and upgrade paths, not for show.
4. **⚠ LIVE BUG — Kuwait's national symbol will not load.** `SpriteManager.cs:998` declares
   `Symbol_Kuwait = "KQ_Symbol"`; the asset on disk is **`KW_Symbol.png`**. Two-character typo, blocks
   §5.4. Worth fixing whether or not you build Kuwait.
5. **Nothing validates a census.** Zero test hits for `IntelReportStats` anywhere under `Assets/Tests/`.
   A missing census and an unclassified prefix fail identically and silently. §8 proposes two cheap tests.
6. **Leaders are not a concern here.** `NameGenService:358-391` and `Leader.cs:255-297` fall through to
   Russian names and raw enum ranks for BE/NE/DE/KW/China — but all 18 leaders in shipped `khost.oob`
   belong to Player units and **zero of the 40 AI units have one**. Latent, not blocking. It only bites
   the day an AI unit gets a leader.

---

## 1. Conventions this spec follows

Extracted from your own files so the new content is indistinguishable in style from the old.

| Rule | Source |
|---|---|
| All profiles built via `WeaponProfile.FromProfileDef(long, short, type, ProfileDef(archetype, deltas, traits), path, turn)` | `WeaponProfile.cs:339` |
| `turnAvailable = (IOC year − 1938) × 12` | `CampaignDateCalendar.cs:19,28-31` |
| `PrestigeCost = (int)tier + (int)type` | `WeaponProfile.cs:225` |
| The five MIXED families (Apc, Recon, Artillery, Aaa, Sam) **must** call `SetMovementMedium` | `FamilyArchetypes.cs:27-41` |
| `RCN_*` needs explicit `SetTargetClass(Hard)` — the prefix default is Soft | `WeaponProfile.cs:374-380` |
| Mobile bay opens **iff** the deployed profile's medium is `Foot` | `EquipmentBays.cs:645-650` |
| Carrier profiles omit `Personnel` (bay summing) | convention across M2/Warrior/Marder/VAB |
| Aircraft use `RegimentIconType.Single`; vehicles use `Directional` (W/NW/SW) | `GameData.cs:664-670` |
| **Borrowed art is the player-facing truth — encode the medium the art shows** | your VAB rule, `WeaponProfileDB.cs:3477-3486` |

**Naming:** existing NATO entries are brigade-scale (1,750–2,600 men, 80–116 tanks) but German and
British ones are called "Regiment" while French ones are honestly called "Brigade." This spec uses
**"Brigade"** for the Lowlands, matching the French precedent and the actual scale. Say the word and
I'll re-cut to "Regiment" for house consistency.

**National character is expressed three ways only** — there is no crew-quality trait (T86
`CONSCRIPT_CREW` was dropped in favour of Experience): (a) census size, (b) which ICM/optics traits are
granted or withheld, (c) template `ExperienceLevel`. `EXPORT_DOWNGRADE` is reserved for §5.

---

## 2. NETHERLANDS — `Nationality.NE`

1 (NL) Corps held NORTHAG's northern sector. Equipment per Bob: Leopard 1, M113-family, M109.
Proposed template experience: **Experienced**.

### 2.1 New WeaponTypes (7 — all profile-bearing)

`TANK_LEOPARD1_NL` · `APC_YPR765_NL` · `SPA_M109_NL` · `SPAAA_CHEETAH_NL` · `RCN_M113CV_NL` ·
`INF_REG_NL` · `FGT_F16_NL`

### 2.2 Profiles

| # | WeaponType | Long / Short | Archetype | Deltas | Traits | Path | Turn | Prestige | Medium | Art (verified) |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `TANK_LEOPARD1_NL` | "Leopard 1 Main Battle Tank" / "Leo 1" | `TankArchetypes.Gen2` | HA−1, HD−1, SA+1, MMP+2 | OPTICS_GEN2, LASER_RANGEFINDER | TANK | **372** (1969) | Gen1 / TANK ⚠ | — | `GE_Leopard1_W/NW/SW` |
| 2 | `APC_YPR765_NL` | "YPR-765 Armoured Infantry Vehicle" / "YPR-765" | `FamilyArchetypes.Ifv` | — | AUTOCANNON_LIGHT | IFV | 468 (1977) | Gen2 / IFV | **Tracked** | `US_M113_W/NW/SW` |
| 3 | `SPA_M109_NL` | "M109 Self-Propelled Howitzer" / "M109" | `Artillery` | SA+1, IR = `INDIRECT_RANGE_MEDIUM` | SELF_PROPELLED | ART | 300 (1963) | Gen2 / SPA | **Tracked** | `GE_M109_W/NW/SW` |
| 4 | `SPAAA_CHEETAH_NL` | "PRTL Cheetah Anti-Aircraft Tank" / "Cheetah" | `Aaa` | IR = `INDIRECT_RANGE_SHORT` | SELF_PROPELLED, RADAR_GUIDED_GUN | AAA | 468 (1977) | Gen3 / SPAAA | **Tracked** | `GE_Gepard_W/NW/SW` |
| 5 | `RCN_M113CV_NL` | "M113 C&V Reconnaissance Vehicle" / "M113 C&V" | `Recon` | — | AUTOCANNON_LIGHT | RCN | 396 (1971) | Gen2 / RCN | **Tracked** + `SetTargetClass(Hard)` | `US_M113_W/NW/SW` |
| 6 | `INF_REG_NL` | "Dutch Infantry Regiment" / "NL Inf" | `Infantry` | — | RPG_LAW, ATGM_MEDIUM, MANPADS_STINGER | *(omit)* | *(omit)* | Gen1 / INF | Foot (archetype) | — |
| 7 | `FGT_F16_NL` | "F-16A Fighting Falcon" / "F-16A" | `FighterMid` | TS+2 | AGILE_AIRFRAME, BVR_RADAR_MISSILE, RWR, CHAFF_FLARE, MULTIROLE_STRIKE | FGT | 492 (1979) | Gen3 / FGT | — | `US_F16` (Single) |

⚠ **Row 1 prestige:** `LEO1_GE:3071` uses `PrestigeTierCost.Gen1` against a Gen2 archetype — the only
tank in the DB where those diverge. I match it deliberately so **all Leopard 1s cost the same**. If you
correct that anomaly, correct every Leopard 1 in the same commit or you ship two prices for one tank.

⚠ **Rows 2 and 5** are the VAB rule in action. The real YPR-765 and M113 C&V *are* tracked, so art and
physics agree here with no compromise.

### 2.3 Censuses

Dutch armoured brigade, scaled below the German 2,200/116 to give the nation character for free.

**`TANK_LEOPARD1_NL`** — Personnel **2000** · `TANK_LEOPARD1_NL` **84** · `APC_YPR765_NL` **44** ·
`RCN_M113CV_NL` **12** · `AT_ATGM` **24** · `MANPAD_STINGER` **18** · `SPA_M109_NL` **18** ·
`SPAAA_CHEETAH_NL` **6** · `ART_120MM_MORTAR` **12**

**`APC_YPR765_NL`** *(carrier — no Personnel, no tanks; see §7.2)* — `APC_YPR765_NL` **96** ·
`RCN_M113CV_NL` **8**

**`SPA_M109_NL`** — Personnel **950** · `SPA_M109_NL` **48** · `APC_YPR765_NL` **18** ·
`MANPAD_STINGER` **12** · `RCN_M113CV_NL` **6**

**`SPAAA_CHEETAH_NL`** — Personnel **900** · `SPAAA_CHEETAH_NL` **18** · `APC_YPR765_NL` **16** ·
`MANPAD_STINGER` **24** · `RCN_M113CV_NL` **8**

**`RCN_M113CV_NL`** — Personnel **600** · `RCN_M113CV_NL` **36** · `APC_YPR765_NL` **10** ·
`AT_ATGM` **8** · `MANPAD_STINGER` **6**

**`INF_REG_NL`** — Personnel **2150** · `SPA_M109_NL` **18** · `ART_120MM_MORTAR` **18** ·
`AT_ATGM` **36** · `MANPAD_STINGER` **24**

**`FGT_F16_NL`** — `FGT_F16_NL` **36**

### 2.4 CombatUnit templates (`CreateDutchForces()`)

All `Side.AI`, `DepotCategory.Secondary`, `DepotSize.Small`, `embarkedProfile: NONE`,
`ExperienceLevel.Experienced`.

| Template ID | unitName | Class | Role | Deployed | Mobile |
|---|---|---|---|---|---|
| `NL_ARMOURED_BRIGADE` | NL Armoured Brigade (Leopard 1) | TANK | GroundCombat | `TANK_LEOPARD1_NL` | NONE |
| `NL_ARMOURED_INFANTRY_BRIGADE` | NL Armoured Infantry Brigade (YPR-765) | MECH | GroundCombat | `INF_REG_NL` | **`APC_YPR765_NL`** |
| `NL_SP_ARTILLERY_REGIMENT` | NL Self-Propelled Artillery Regiment | ART | GroundCombat | `SPA_M109_NL` | NONE |
| `NL_AIR_DEFENSE_REGIMENT` | NL Air Defence Regiment (Cheetah) | SPAAA | AirDefenseArea | `SPAAA_CHEETAH_NL` | NONE |
| `NL_RECON_UNIT` | NL Recon Unit (M113 C&V) | RECON | GroundCombat | `RCN_M113CV_NL` | NONE |
| `NL_HAWK_REGIMENT` | NL SAM Regiment (Hawk) | SAM | AirDefenseArea | `SAM_HAWK_US` | **`TRK_WEST`** |
| `NL_F16_FIGHTER_SQUADRON` | NL F-16 Fighter Squadron | FGT | AirSuperiority | `FGT_F16_NL` | NONE |

`NL_HAWK_REGIMENT` reuses the existing US Hawk profile exactly as `GE_HAWK_REGIMENT:3218` does —
`SAM_HAWK_US` is `Foot` medium (`:3892`), so the Mobile bay is open and `TRK_WEST` is correct.

---

## 3. BELGIUM — `Nationality.BE`

I (BE) Corps, NORTHAG's southern sector. Same equipment family; recon on CVR(T) Scimitar, which lets
Belgium borrow British art and look visibly different from the Dutch on the map.
Proposed template experience: **Trained** (I BE Corps had the lowest readiness in NORTHAG — much of
the force was garrisoned in Belgium, not forward). **Bob's call.**

### 3.1 New WeaponTypes (6)

`TANK_LEOPARD1_BE` · `APC_AIFV_BE` · `SPA_M109_BE` · `SPAAA_GEPARD_BE` · `RCN_SCIMITAR_BE` · `INF_REG_BE`
*(+ `FGT_F16_BE` if you want Belgian air — same shape as `FGT_F16_NL`.)*

### 3.2 Profiles

Rows 1–4 are identical in archetype/deltas/traits to their Dutch counterparts — only names, tokens,
turn and art differ. Only the divergences are tabulated.

| # | WeaponType | Long / Short | Archetype | Traits | Turn | Prestige | Medium | Art |
|---|---|---|---|---|---|---|---|---|
| 1 | `TANK_LEOPARD1_BE` | "Leopard 1A5 Main Battle Tank" / "Leo 1A5" | `Gen2`, deltas HA−1 HD−1 SA+1 MMP+2 | OPTICS_GEN2, LASER_RANGEFINDER | **360** (1968) | Gen1 / TANK | — | `GE_Leopard1_*` |
| 2 | `APC_AIFV_BE` | "AIFV Armoured Infantry Fighting Vehicle" / "AIFV" | `Ifv` | AUTOCANNON_LIGHT | 468 (1977) | Gen2 / IFV | Tracked | `US_M113_*` |
| 3 | `SPA_M109_BE` | "M109 Self-Propelled Howitzer" / "M109" | `Artillery`, SA+1, IR MEDIUM | SELF_PROPELLED | 300 | Gen2 / SPA | Tracked | `GE_M109_*` |
| 4 | `SPAAA_GEPARD_BE` | "Gepard Anti-Aircraft Tank" / "Gepard" | `Aaa`, IR SHORT | SELF_PROPELLED, RADAR_GUIDED_GUN | 468 (1977) | Gen3 / SPAAA | Tracked | `FR_Gepard_*` |
| 5 | `RCN_SCIMITAR_BE` | "CVR(T) Scimitar Reconnaissance Vehicle" / "Scimitar" | `Recon` | AUTOCANNON_HEAVY | 420 (1973) | Gen2 / RCN | Tracked + `SetTargetClass(Hard)` | `UK_FV105_*` |
| 6 | `INF_REG_BE` | "Belgian Infantry Regiment" / "BE Inf" | `Infantry` | RPG_LAW, ATGM_MEDIUM, **MANPADS_BASIC** | omit | Gen1 / INF | Foot | — |

Row 6 uses `MANPADS_BASIC` (GAT≥6) rather than `MANPADS_STINGER` (GAT≥8, ×1.05) — Belgian infantry
carried Blowpipe/Mistral rather than Stinger. Free, honest differentiation, and it follows the UK
precedent at `INF_REG_UK:5037`.

### 3.3 Censuses

**`TANK_LEOPARD1_BE`** — Personnel **1900** · `TANK_LEOPARD1_BE` **72** · `APC_AIFV_BE` **40** ·
`RCN_SCIMITAR_BE` **12** · `AT_ATGM` **20** · `MANPAD_MISTRAL` **16** · `SPA_M109_BE` **18** ·
`SPAAA_GEPARD_BE` **6** · `ART_120MM_MORTAR` **12**

**`APC_AIFV_BE`** *(carrier)* — `APC_AIFV_BE` **88** · `RCN_SCIMITAR_BE` **8**

**`SPA_M109_BE`** — Personnel **950** · `SPA_M109_BE` **42** · `APC_AIFV_BE` **16** ·
`MANPAD_MISTRAL` **12** · `RCN_SCIMITAR_BE` **6**

**`SPAAA_GEPARD_BE`** — Personnel **880** · `SPAAA_GEPARD_BE` **16** · `APC_AIFV_BE` **14** ·
`MANPAD_MISTRAL` **20** · `RCN_SCIMITAR_BE` **8**

**`RCN_SCIMITAR_BE`** — Personnel **580** · `RCN_SCIMITAR_BE` **32** · `APC_AIFV_BE` **10** ·
`AT_ATGM` **8** · `MANPAD_MISTRAL` **6**

**`INF_REG_BE`** — Personnel **2050** · `SPA_M109_BE` **18** · `ART_120MM_MORTAR` **18** ·
`AT_ATGM` **30** · `MANPAD_MISTRAL` **18**

`MANPAD_MISTRAL` already exists as a census-only token (French profiles use it) — no new type needed.

### 3.4 Templates (`CreateBelgianForces()`)

All `Side.AI`, Secondary/Small, `embarkedProfile: NONE`, `ExperienceLevel.Trained`.

| Template ID | unitName | Class | Role | Deployed | Mobile |
|---|---|---|---|---|---|
| `BE_ARMOURED_BRIGADE` | BE Armoured Brigade (Leopard 1) | TANK | GroundCombat | `TANK_LEOPARD1_BE` | NONE |
| `BE_MECH_INFANTRY_BRIGADE` | BE Mechanised Infantry Brigade (AIFV) | MECH | GroundCombat | `INF_REG_BE` | **`APC_AIFV_BE`** |
| `BE_SP_ARTILLERY_REGIMENT` | BE Self-Propelled Artillery Regiment | ART | GroundCombat | `SPA_M109_BE` | NONE |
| `BE_AIR_DEFENSE_REGIMENT` | BE Air Defence Regiment (Gepard) | SPAAA | AirDefenseArea | `SPAAA_GEPARD_BE` | NONE |
| `BE_RECON_UNIT` | BE Recon Unit (Scimitar) | RECON | GroundCombat | `RCN_SCIMITAR_BE` | NONE |

---

## 4. DENMARK — `Nationality.DE`

⚠ **`DE` is DENMARK here; West Germany is `FRG`.** That collision with the ISO code for Germany is the
single most likely authoring mistake in this document. Check every `Nationality.DE` twice.

Jutland Division / LANDJUT — a smaller, lighter force covering the Baltic approaches.
Proposed template experience: **Experienced**.

### 4.1 New WeaponTypes (5)

`TANK_LEOPARD1_DK` · `APC_M113_DK` · `SPA_M109_DK` · `RCN_M113CV_DK` · `INF_REG_DK`
*(+ `FGT_F16_DK` optional, turn **504** / 1980.)*

### 4.2 Profiles

| # | WeaponType | Long / Short | Archetype | Deltas | Traits | Turn | Prestige | Medium | Art |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `TANK_LEOPARD1_DK` | "Leopard 1A3 Main Battle Tank" / "Leo 1A3" | `Gen2` | HA−1, HD−1, SA+1, MMP+2 | OPTICS_GEN2, LASER_RANGEFINDER | **456** (1976) | Gen1 / TANK | — | `GE_Leopard1_*` |
| 2 | `APC_M113_DK` | "M113 Armoured Personnel Carrier" / "M113" | `FamilyArchetypes.Apc` | — | *(none)* | 312 (1964) | Gen1 / APC | **Tracked** | `US_M113_*` |
| 3 | `SPA_M109_DK` | "M109 Self-Propelled Howitzer" / "M109" | `Artillery` | SA+1, IR MEDIUM | SELF_PROPELLED | 300 | Gen2 / SPA | Tracked | `UK_M109_*` |
| 4 | `RCN_M113CV_DK` | "M113 Reconnaissance Vehicle" / "M113 Recon" | `Recon` | — | AUTOCANNON_LIGHT | 396 (1971) | Gen2 / RCN | Tracked + `SetTargetClass(Hard)` | `FR_M113_*` |
| 5 | `INF_REG_DK` | "Danish Infantry Regiment" / "DK Inf" | `Infantry` | — | RPG_LAW, ATGM_MEDIUM, MANPADS_BASIC | omit | Gen1 / INF | Foot | — |

Row 2 uses the **`Apc`** family (not `Ifv`) — the Danish M113 is a battle taxi, not a fighting vehicle.
`Apc` is a MIXED family, so `SetMovementMedium(Tracked)` is mandatory.

Denmark deliberately has **no organic SPAAA** — Danish divisional AD was towed 40mm Bofors and Hawk.
Give `DK_AIR_DEFENSE_REGIMENT` the existing `SAM_HAWK_US` with `TRK_WEST` mobile, or omit AD entirely
and let the Danes be the sector that needs covering. **Bob's call — this is a gameplay texture decision,
not a data one.**

### 4.3 Censuses

Smallest of the three — a Danish brigade was materially lighter.

**`TANK_LEOPARD1_DK`** — Personnel **1600** · `TANK_LEOPARD1_DK` **60** · `APC_M113_DK` **48** ·
`RCN_M113CV_DK` **10** · `AT_ATGM` **24** · `MANPAD_STINGER` **12** · `SPA_M109_DK` **12** ·
`ART_120MM_MORTAR` **12**

**`APC_M113_DK`** *(carrier)* — `APC_M113_DK` **110** · `RCN_M113CV_DK` **8**

**`SPA_M109_DK`** — Personnel **820** · `SPA_M109_DK` **36** · `APC_M113_DK` **14** ·
`MANPAD_STINGER` **8** · `RCN_M113CV_DK` **6**

**`RCN_M113CV_DK`** — Personnel **520** · `RCN_M113CV_DK` **28** · `APC_M113_DK` **10** ·
`AT_ATGM` **6** · `MANPAD_STINGER` **4**

**`INF_REG_DK`** — Personnel **1850** · `SPA_M109_DK` **12** · `ART_120MM_MORTAR` **18** ·
`AT_ATGM` **30** · `MANPAD_STINGER` **16**

### 4.4 Templates (`CreateDanishForces()`)

All `Side.AI`, Secondary/Small, `embarkedProfile: NONE`, `ExperienceLevel.Experienced`.

| Template ID | unitName | Class | Role | Deployed | Mobile |
|---|---|---|---|---|---|
| `DK_ARMOURED_BRIGADE` | DK Armoured Brigade (Leopard 1) | TANK | GroundCombat | `TANK_LEOPARD1_DK` | NONE |
| `DK_MECH_INFANTRY_BRIGADE` | DK Mechanised Infantry Brigade (M113) | MECH | GroundCombat | `INF_REG_DK` | **`APC_M113_DK`** |
| `DK_ARTILLERY_REGIMENT` | DK Self-Propelled Artillery Regiment | ART | GroundCombat | `SPA_M109_DK` | NONE |
| `DK_RECON_UNIT` | DK Recon Unit (M113) | RECON | GroundCombat | `RCN_M113CV_DK` | NONE |
| `DK_HAWK_REGIMENT` *(optional)* | DK SAM Regiment (Hawk) | SAM | AirDefenseArea | `SAM_HAWK_US` | `TRK_WEST` |

---

## 5. GULF THEATRE — framework and token plan

Per Bob: **Iraq, Iran, Saudi Arabia, Kuwait.** This section is deliberately a plan, not finished
censuses — Bob asked to "start laying plans," and the Lowlands should ship and be play-tested first.

### 5.0 The doctrine axis

The Gulf splits cleanly into two equipment families, which makes it cheap to build:

| | Equipment family | Quality lever | Icon base |
|---|---|---|---|
| **Iraq** | Soviet, early-70s (T-55/T-62, BMP-1, 2S1, ZSU-57, SA-6) | `EXPORT_DOWNGRADE` | Green |
| **Iran** | **Western, Shah-era** (M60A3, M113, F-4, F-14) + captured/bought Soviet | *(none — real kit, poor upkeep)* | Green |
| **Saudi** | Western (M60, M113, AMX-30, Hawk) | *(none)* | Green |
| **Kuwait** | Western/mixed (Chieftain, M113, Saladin) | `EXPORT_DOWNGRADE` on Soviet items only | Green |

⚠ **Iran is already Western.** `TANK_M60A3_IR`, `APC_M113_IR`, `FGT_F4_IR`, `FGT_F14_IR`,
`INF_REG_IR` all exist (`WeaponProfileDB.cs:5350,5462,5864,5896,5993`) across 9 templates. Bob's
instruction "Iran should have more western oriented weapons" is **already the direction of travel** —
Iran needs *deepening*, not redirecting. Existing Iranian templates are `ExperienceLevel.Green`, which
reads as post-revolution degradation and is a good choice worth keeping.

### 5.1 Iran — deepen (proposed additions)

Gaps against the existing 5 profiles: no artillery of its own (templates borrow `ART_HEAVY_ARAB` /
`ART_LIGHT_ARAB`), no recon, no SPAAA, no organic SAM.

Proposed new: `SPA_M109_IR` (Shah-bought M109, art `US_M109_*`) · `RCN_SCORPION_IR` (Iran bought
CVR(T), art `UK_FV105_*`) · `TANK_CHIEFTAIN_IR` (Iran's 707 Chieftain Mk3/5 — the most distinctive
Iranian vehicle, art `US_M60_*` as stand-in) · `SPAAA_ZSU23_IR` (art `AR_ZSU57_*`).

`TANK_CHIEFTAIN_IR` is the interesting one: `TankArchetypes.Gen2`, deltas HD+1 / MMP−2 (heavy, slow),
traits `COMPOSITE_CERAMIC`? — no, Chieftain was Burlington-less; use `SPACED_ARMOR` +
`LASER_RANGEFINDER`. It gives Iran a genuinely heavy tank that isn't Soviet.

### 5.2 Iraq — deepen

Already 10 profiles / 12 templates and the `EXPORT_DOWNGRADE` pattern is established. Gaps: no recon
profile, no rocket artillery, no dedicated AT. Proposed: `RCN_BRDM2_IQ` · `ROC_BM21_IQ` ·
`TANK_T72_IQ` (Iraq's Republican Guard T-72M — Gen3 + `EXPORT_DOWNGRADE`, which is exactly the worked
example: HD 11+2−2 = 11, ICM 0.945).

### 5.3 Saudi Arabia — new, `Nationality.SAUD`

**Ready to build:** enum member exists, `Symbol_Saudi → SA_Symbol` resolves, Green icon base wired
(`GameIconRenderer.cs:818,839`), and `NameGenService:366,384` already routes SAUD to Arabic names.
**No plumbing needed.**

Proposed 5 profiles: `TANK_M60_SA` (art `AR_M60_*`) · `APC_M113_SA` (art `AR_M113_*`) ·
`SPA_M109_SA` (art `US_M109_*`) · `INF_REG_SA` · `SPSAM_HAWK_SA` or reuse `SAM_HAWK_US`.
Templates: armoured brigade, mechanised brigade, artillery regiment, SAM regiment. No
`EXPORT_DOWNGRADE` — the Saudis bought current Western kit.

### 5.4 Kuwait — new, `Nationality.KW` ⚠ **blocked on a two-character bug**

`SpriteManager.cs:998` asks for `"KQ_Symbol"`; the file is `KW_Symbol.png`. **Fix that first** or
Kuwaiti units render with a missing symbol. Everything else is wired (`GameIconRenderer.cs:819,840`).

Also note `NameGenService:366,384` routes `IR or IQ or SAUD` to Arabic names but **not `KW`** — Kuwaiti
officers would get Russian names. Latent only (AI units have no leaders), but it is a one-token fix in
the same edit: add `or Nationality.KW`.

Proposed minimal Kuwait (3 profiles): `TANK_CHIEFTAIN_KW` · `APC_M113_KW` · `INF_REG_KW`. Kuwait was
small; two or three templates is faithful.

---

## 6. New WeaponType tokens — complete list

**Lowlands, profile-bearing (18):**
`TANK_LEOPARD1_NL` `APC_YPR765_NL` `SPA_M109_NL` `SPAAA_CHEETAH_NL` `RCN_M113CV_NL` `INF_REG_NL` `FGT_F16_NL`
`TANK_LEOPARD1_BE` `APC_AIFV_BE` `SPA_M109_BE` `SPAAA_GEPARD_BE` `RCN_SCIMITAR_BE` `INF_REG_BE`
`TANK_LEOPARD1_DK` `APC_M113_DK` `SPA_M109_DK` `RCN_M113CV_DK` `INF_REG_DK`

**Census-only tokens required: NONE.** Every census above uses existing tokens — `Personnel`,
`AT_ATGM`, `MANPAD_STINGER`, `MANPAD_MISTRAL`, `ART_120MM_MORTAR`. Verified present.

**Enum safety:** `WeaponType` persists **by name** (`JsonPolicy`), so appending members is safe and
needs no `SAVE_VERSION` bump. ⚠ But per `CLAUDE.md` rule 11, a *rename* is breaking — so pick these
names once. Please relay the final list back; the editor mirrors `WeaponType` in its `ENUM_NAMES`
tables and will fail loudly on an unknown name at load, which is the good failure but still a failure.

---

## 7. Existing defects — do not replicate

### 7.1 `SPA_M109_FR` reports zero M109s
`WeaponProfileDB.cs:3596` — the French M109 profile's census lists `SPA_AUF1 48` and no M109s, where
`M109_GE:3555` and `M109_UK:3636` list their own platform. A French SP artillery regiment shows 48
AUF1s. Not mine to fix; flagged so the Lowlands don't inherit the pattern.

### 7.2 Carrier censuses inject a tank battalion into mech infantry ⭐
`MARDER_GE:3317` carries `TANK_LEOPARD1_GE 58`; `WARRIOR_UK:3282` carries `TANK_CHALLENGER1_UK 58`.
Since Mobile sums into Deployed (`EquipmentBays.cs:470-476`), `GE_PANZERGRENADIER_REGIMENT` resolves to
**2,600 men + 58 Leopard 1s** + 102 Marders. These read as standalone brigade rosters written before
bay-summing landed. **Every carrier census in this spec lists only the carrier's own vehicles.**

If you fix the German and British ones to match, the intel *and* loss reports for those two units both
change — worth doing, worth doing deliberately.

### 7.3 `LEO1_GE` archetype/prestige divergence
Gen2 archetype, Gen1 prestige tier (`:3066` vs `:3071`). This spec matches it so all Leopard 1s price
identically. Fix all or none.

---

## 8. Two cheap tests worth adding

Nothing under `Assets/Tests/` references `IntelReportStats` at all. Given §0.1 — the census is the loss
ledger — two tests would repay themselves:

1. **Every registered profile has a non-empty census**, with an explicit allow-list for the deliberate
   exceptions (`TRK_GEN_SV`, `TRK_WEST` — documented at `PrinterMessage.cs:302-309`). Catches the
   silent-empty case that makes a unit die without appearing in the loss report.
2. **Every `WeaponType` appearing in any census classifies to a non-`None` bucket.**
   `ClassifyWeaponType` silently drops unknown prefixes (`EquipmentBays.cs:538`) from *both* the intel
   and loss reports. A typo'd token in a new census is invisible today and would stay invisible.

Both are pure-data tests with no scene dependency, and both would have caught mistakes I could plausibly
make in the 18 profiles above.

---

## 9. Summary of asks

| # | Ask | Size |
|---|---|---|
| 1 | Implement §2–§4: 18 WeaponTypes, 18 profiles, 17 templates, 3 `Create*Forces()` methods | The bulk |
| 2 | Fix `Symbol_Kuwait` `"KQ_Symbol"` → `"KW_Symbol"` (`SpriteManager.cs:998`) | 2 chars |
| 3 | Add `or Nationality.KW` to the Arabic name arms (`NameGenService.cs:366,384`) | 1 line ×2 |
| 4 | Relay the final WeaponType names back so the editor's mirror stays in sync | — |
| 5 | Rule on §7.2 — fix the German/British carrier censuses, or leave and document | Your call |
| 6 | Consider the two tests in §8 | Small |
| 7 | §5 Gulf — review the framework; detailed censuses to follow after the Lowlands play-test | Review only |

Bob is the arbiter on the judgement calls flagged **Bob's call** throughout: template experience levels,
Brigade-vs-Regiment naming, and whether Denmark gets organic air defence.

— Scenario Editor agent
