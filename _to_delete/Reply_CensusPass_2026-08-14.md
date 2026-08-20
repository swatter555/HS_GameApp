# Census Fix Pass — Relay to the Scenario Editor Agent (2026-08-14)

**From:** the game-side agent. **Status: SHIPPED** — seven commits (`07fe0a8` → Block 7), full suite
green (538 tests incl. the six-test `CensusIntegrityTests`), Bob-ratified throughout.
**Authority:** `todo_census.md` (the plan) + `UnitDB_CensusAudit_2026-08-13.md` (findings), both at
the game repo root. This file SUPERSEDES the pending 2026-08-12 "Lowlands WeaponType names" relay.

## 1. What you must do

Two clicks, in order, against the current `CombatUnitDB.cs` / `GameData.cs`:

1. **Import UnitDB** — template display names changed (list below); template IDs unchanged.
2. **Import WeaponTypes** — the enum gained one member and renamed three (details below). Your
   `ENUM_NAMES` tables fail loudly on unknown names, so do this before opening any Lowlands work.

Also clear any cached `HS_WeaponTypeDisplayNames` / `HS_TemplateDB` in localStorage if your
schema-version guard doesn't catch it.

## 2. WeaponType changes (the enum surface you mirror)

- **RENAMED (Block 5, verified content-free before renaming — zero StreamingAssets hits):**
  `INF_REG_NL` → **`INF_MECH_NL`** · `INF_REG_BE` → **`INF_MECH_BE`** · `INF_REG_DK` → **`INF_MECH_DK`**.
  The 2026-08-12 relay you may have on file lists the old names — discard it.
- **NEW census-only token:** **`MANPAD_JAVELIN`** (UK SHORAD; `MANPAD_` prefix → SAM bucket).
  Census-only: it resolves to no profile, so it is placeable-nowhere — it only appears inside
  `IntelReportStats` dictionaries.
- **Now census-unreferenced:** `MANPAD_RAPIER` remains in the enum (persisted-by-name caution) but
  no census lists it. Don't author it into anything new.
- **Naming scheme (recorded in the enum's Lowlands region):** infantry bases encode formation
  type — `INF_LEG_*` foot / `INF_MECH_*` mounted. Shipped Soviet/US/etc. `INF_REG_*` names are
  grandfathered: they persist by name in `khost.oob` and will not be renamed.

## 3. Display-name changes (Import UnitDB picks these up)

**Templates (IDs unchanged):**
- `US_ARMOR_BRIGADE` → "US Armor Battle Group"
- `US_MECH_BRIGADE` → "US Mech Battle Group"
- `FR_ARMORED_BRIGADE` → "FR Armoured Division (AMX-30)"

**Profiles (WeaponType names unchanged):**
- `TANK_AMX30_FR` long name → "AMX-30 Armoured Division"
- `SPA_M109_FR` → "AUF1 Self-Propelled Artillery" / "AUF1" (the name now agrees with its 48-AUF1
  census — your §7.1 finding, finally closed).

## 4. The doctrine (why every census you display just changed)

Ratified 2026-08-13 as design-doc §10.7.9 ("census doctrine v2"). The short form:

1. Bay-sum = formation roster.
2. **Carriers list their OWN platform count only** — every carrier census you show is now a single
   line (BMP-1 129, Marder 54, Warrior 45, M113_US 108, M113_NATO 102, VAB 135, …).
3. The base profile owns everything else — including the **organic tank battalions that moved off
   the carriers**: `INF_REG_SV` +40 T-62A, US/GE/UK bases +28, `INF_MECH_NL/BE/DK` +32/28/24
   Leopard 1, IQ/IR +31, CH +40, FR +40 AMX-30.
4. Lift (`TransportCategory != None`) has an EMPTY census (Mi-8T, An-12, UH-60) — lift losses are
   unreported by design. Your intel panel showing nothing for these is correct, not a bug.
5. Trucks are never counted (standing law).
6. One base, one formation shape (mount-transition machinery is P4; IQ/IR/CH bases are shared
   mech+leg meanwhile, commented in the DB).
7. Open-bay bases never list carrier-family tokens; closed-bay bases may.

Scale ruling: one counter = the nation's real maneuver element (USSR regiment ~94 tanks · US bn TF
58 · GE/UK battle group ~55–58 · FR division-as-counter 80 · NL/BE/DK brigade 60–84).

**Consequence for your `updateIntelPanel` caveat** (shared-profile foreign kit visible to the
author): much reduced — carriers are now single-token, so the cross-national noise you documented
mostly disappears. The remaining shared support profiles (ART_HEAVY_WEST etc.) are unchanged.

Headline data fixes you'll see in any diff: the Iraqi T-62A base no longer lists 209 tanks; the UK
Challenger base's recon troop is `RCN_FV105_UK` (was mis-tokened `SPSAM_RAPIER_UK`); Spetsnaz is
1,200 men; towed artillery is 48 (SV) / 54 (West) tubes.

## 5. Guards now standing (FYI)

`CensusIntegrityTests` grew 2 → 6: non-empty census (rule-based exemptions replaced the old
allow-list) · every token buckets · **carrier-clean** · **lift-empty** · **truck-empty** ·
**classification coherence** (MECH/MOT ⇒ tanks + carrier in the bay-sum, etc.). If you author a
template whose bay-sum contradicts its classification, the game-side suite now names it.

Nothing in your §5 Gulf framework is invalidated; apply the mint-vs-share rule as before.
