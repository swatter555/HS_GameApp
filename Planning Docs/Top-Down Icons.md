# Top-Down Icons — one sprite per profile, rotated to facing

> **Bob's ruling (2026-08-27), command decision, responsibility his.** Unit art goes to AI-generated
> **top-down** sprites: **one sprite per profile**, rotated to match facing. No per-direction variants, no
> firing variants. **No two-mode switch — rip the old path out.**
>
> **Status: PLAN FINAL 2026-08-27 — implementation is the NEXT SESSION'S first task (T-1…T-4 + §6 guard tests as one commit). Soviet + MJ art ready; Bob renames art to the no-suffix standard (R6).**

## THE RULING SET

| # | Ruling |
|---|---|
| R1 | **One sprite per profile**, rotated to facing. Directional variants and firing variants both die |
| R2 | **No switch.** The old path is deleted, not toggled. Revert is a commit, not a setting |
| R3 | **Helicopters keep their ART convention exactly** — same names, same 6 frames, same flipbook. ⚠ Their files are `<name>_Frame0..5` with **NO directional post-fix**, so R4 does not touch them at all (Bob corrected an earlier misremembering, 2026-08-27). They DO rotate: transform rotation and frame-cycling run together |
| R4 | **Strip directional post-fixes from every unit-icon name and constant.** ⚠ Post-fixes ONLY — model codes are preserved (`SV_T55A` stays `SV_T55A`; a future `SV_T55MV` must remain distinct) |
| R5 | **Everything rotates EXCEPT bases** (HQ / DEPOT / AIRB) |
| R6 | Bob renames the art files on the asset side to the no-direction standard, so a later agent cannot be misled by stale suffixes |

---

## 1. ⚠ THE TRAP — READ BEFORE TOUCHING ANY FIELD

**`Helo_Animation` stores its six frames in the directional and FIRING fields.**

```csharp
MI8T.IconProfile = new RegimentIconProfile(RegimentIconType.Helo_Animation)
{
    W    = SpriteManager.SV_MI8_Frame0,   NW   = ..._Frame1,   SW   = ..._Frame2,
    W_F  = SpriteManager.SV_MI8_Frame3,   NW_F = ..._Frame4,   SW_F = ..._Frame5
};
```

`GetAnimationFrame(int)` maps `0→W, 1→NW, 2→SW, 3→W_F, 4→NW_F, 5→SW_F`.

**So "we are dropping firing versions, delete `W_F`/`NW_F`/`SW_F`" DESTROYS THE HELICOPTER ANIMATION** —
the one thing R3 rules untouchable. The six fields are overloaded three ways (direction slots for
`Directional`, firing slots for `Directional_Fire`, frame slots for `Helo_Animation`) and nothing in the
field names says so. This is why T-2 below gives helos a real frame array **before** anything is deleted.

---

## 2. SCAN FINDINGS (2026-08-27)

### 2.1 ⚠ CORRECTED (2026-08-27 late): the firing art DOES render — it is the DUG-IN variant
The first draft claimed `_F` art was dead ("zero callers outside `EquipmentBays.cs`") — **wrong, and the
error is instructive: the caller lives INSIDE `EquipmentBays.cs`.** `GetIcon(position, direction)` routes a
`Directional_Fire` profile to `GetFiringIcon` whenever the unit is HastyDefense / Entrenched / Fortified.
So the 32 `Directional_Fire` profiles show their `_F` sprite while dug in, today, in play.
**Bob's R1 ruling stands** ("the gain is minimal for lots of work") — but the honest cost statement is:
dug-in units lose their art VARIANT, not nothing. No information is lost: the `deployIcon` child on
`Prefab_CombatUnitIcon` already displays deployment state independently of the vehicle art.

### 2.2 Art inventory — measured

| Category | Today | After |
|---|---|---|
| Vehicles, directional (`_W`/`_NW`/`_SW`) — 77 vehicles | 231 | **77** |
| Firing variants — 29 vehicles | 87 | **0** |
| Helo frames (`_Frame0..5`) — 9 helos | 54 | **54** (unchanged, R3) |
| Already-single (aircraft, artillery, MANPADS, infantry) | 88 | **88** (art replaced in place) |
| **Total** | **460** | **~219** |

Per folder: Soviet 197 · NATO 147 · Arab 54 · Chinese 53 · Generic 9.
**~241 PNGs deleted**; ~250 of the 468 unit-sprite constants in `SpriteManager` go with them.

### 2.3 `Single` is already the target shape — and it is the majority

⚠ **Counts CORRECTED 2026-08-27** — the first pass over-counted by grepping enum *references* across all
files (validation arms, the enum declaration) instead of `WeaponProfileDB` *declarations*. Authoritative:

| `RegimentIconType` | Declarations | Fate |
|---|---|---|
| `Single` | **89** | **SURVIVES, zero code change.** Bob replaces art at the same names |
| `Directional` | **53** | **DELETED** → becomes `Single` |
| `Directional_Fire` | **32** | **DELETED** → becomes `Single` |
| `Helo_Animation` | **10** | **SURVIVES** — art untouched (R3); field storage cleaned (§1) |
| **Total** | **184** | matches the 184 registered profiles in `Claude_Project.md` §2.6 |

End state is **two icon types, not four**. Only **85 of 184** declarations convert; 99 are untouched.

### 2.4 Nothing rotates a unit icon today — the axis is free
No `localRotation` / `localEulerAngles` / `Quaternion` in `GameIconRenderer`, `Prefab_CombatUnitIcon` or
`UnitMoveAnimator`. No conflict with movement tweens (they drive the ROOT position), `SortingConfig`
(rotation does not affect sort order) or `SetOpacity`.

### 2.5 ⚠ NO TEST TOUCHES ICONS. AT ALL.
Zero hits for `IconProfile` / `GetSpriteNameForUnit` / `RegimentIconType` / `flipX` across `Assets/Tests/`.
**Nothing in the suite will catch a mistake here.** Mitigation in §6.

### 2.6 `flipX` is set by the RENDERER and must be actively cleared
`GameIconRenderer` writes it at two sites (icon creation ~341, `RefreshIconFacing` ~405);
`Prefab_CombatUnitIcon` never touches it. **Ceasing to set it is not enough** — a re-created icon could
carry a stale `true`. Set it `false` explicitly in the new path.

### 2.7 ⚠ THE STRIP HAS A HARD BOUNDARY — 86 DIRECTIONAL SPRITES MUST KEEP THEIR SUFFIXES
R4 says "strip from everything," and read literally that would break the map. **86 directional PNGs
outside `Unit Icons/` are genuine hex-EDGE features, not facings:** `Bridge_{W,NW,SW,E,NE,SE}`,
`DamagedBridge_*`, `Pont_*`, the `RiverEdge_*` / `RiverTerm_*` family, and `FacingChevron_*` (which is the
overlay that *shows* facing). **These keep their suffixes.**

**The strip is scoped to unit icons only** — the 12 prefixes actually present in
`Art/Sprites/Unit Icons/`, with their file counts:
`SV_` 197 · `US_` 68 · `CH_` 53 · `AR_` 43 · `GE_` 32 · `FR_` 26 · `UK_` 18 · `GEN_` 9 · `MJ_` 8 ·
`IQ_` 2 · `GER_` 2 (→ folds into `GE_`, §2.8) · `IR_` 1.

### 2.8 Free fix while renaming: the `GER_` / `GE_` anomaly
32 German sprites use `GE_`; **two use `GER_`** (`GER_Airborne`, `GER_Regulars`) — both real, both
referenced in `SpriteManager` and `WeaponProfileDB`. Since every unit-icon name is being rewritten anyway,
normalise to `GE_Airborne` / `GE_Regulars`. Costs nothing now, costs a puzzled agent later.

### 2.8b Helo art: 10 profiles, 9 art sets — **AH-1 reuses the AH-64's frames**
`AH1` and `AH64` both point at `US_AH64_Frame0..5`. Harmless in code (two profiles, one art set) and it
explains why 10 declarations map to only 9 `_Frame0` files. Flagged because Bob is re-authoring all art:
if the AH-1 Cobra should look like a Cobra rather than an Apache, this is the moment — otherwise the share
is fine and needs nothing.

### 2.9 Pre-existing dead icon data
`BASE_AIRBASE.IconProfile` is never read — `GetSpriteNameForUnit` short-circuits `AIRB` to the
`AirbaseStack_N` badges by attached-aircraft count. Sweep it in T-6.

---

## 3. THE END STATE

**Resolution becomes:** active profile → its ONE icon name → set sprite → set rotation from facing →
`flipX = false`.

**Naming (R4):** existing name **minus the direction/firing post-fix**, nothing else touched.
`SV_T55A_W` → `SV_T55A`. `SV_T55A_NW_F` → deleted. Model codes are load-bearing — `SV_T55A` and a future
`SV_T55MV` must stay distinct, so the rule is *strip the suffix*, never *shorten the name*.

**Rotation (R5):** 6 facings at 60°, pointy-top odd-r. **Bases do not rotate** — gate on the existing
derived `CombatUnit.IsBase` (covers HQ / DEPOT / AIRB), which also spares them the meaningless
side-derived `Facing` the constructor assigns. Everything else rotates, infantry included.
⚠ The canonical art heading at rotation 0 is a one-line constant — take it from Bob's first converted
sprite rather than guessing.

### ⚠ THE ONE ARCHITECTURAL RULE
**Rotate `unitIcon` ONLY — never the prefab root.** `Prefab_CombatUnitIcon` carries six renderers:
`unitIcon`, `nationIcon`, `boxIcon`, `boxText` (HP readout), `deployIcon`, `stackingIcon`. Rotating the
root spins the HP number, nationality flag, deployment chevron and stacking badge with the vehicle. Bob
called this on sight; recorded because it is invisible until wrong, and a later "simplification" would
reach for the root.

---

## 4. WORK PLAN — one reviewable commit (T-1…T-4), then the sweep

**T-1 — Resolution + rotation (`GameIconRenderer`).** Drop the `out bool shouldFlip` from
`GetSpriteNameForUnit`; delete `NormalizeDirection` and `ShouldFlipSprite`; set `flipX = false` explicitly
(§2.6); add the facing→rotation map gated on `!unit.IsBase` (R5). `RefreshIconFacing` rotates instead of
re-resolving a variant, staying the single facing entry point so movement steps and Shift+click keep
working untouched.

**T-2 — Model layer (`EquipmentBays`), SIMPLIFIED by a late find: helos need ONE string, not six.**
**`GetAnimationFrame` has ZERO callers** — the flipbook lives in `Prefab_CombatUnitIcon` and resolves
frames by SPRITE NAME (`<name>_Frame0` → suffix swap at `MOTION_FRAME0_SUFFIX`), never through the
profile. The profile only ever supplies the INITIAL sprite: `GetIcon()` returns `W`, which for helos is
`Frame0`. So the planned `string[] Frames` array is retired — over-engineering for a consumer that does
not exist. Instead:
1. `RegimentIconProfile` collapses to ONE field, `Icon`. A helo declaration becomes
   `Icon = SpriteManager.SV_MI8_Frame0` — the name-convention carries the other five frames, as it
   already does at runtime today.
2. Delete `W/NW/SW/W_F/NW_F/SW_F`, `GetDirectionalIcon`, `GetFiringIcon`, **`GetAnimationFrame`**, and the
   `Directional`/`Directional_Fire`/frame arms of `IsValid`. `IsValid`'s `Helo_Animation` arm becomes:
   `Icon` must end in `_Frame0` — that suffix IS the flipbook contract, so the validator should pin it.
⚠ The §1 trap still governs the ORDER: move each helo's `Frame0` into `Icon` before the six fields die.

**T-3 — `WeaponProfileDB`: 85 declarations.** 53 `Directional` + 32 `Directional_Fire` → `Single` with one
suffix-free name. Mechanical, scriptable. The 89 `Single` blocks are untouched; the 10 helo blocks were
handled in T-2.

**T-4 — `SpriteManager`: ~250 constants deleted, remainder renamed.** One constant per vehicle, suffix
stripped; `GER_` → `GE_` (§2.8). ⚠ Scoped to the 12 unit-icon prefixes — bridges, rivers and chevrons are
untouched (§2.7).

**T-5 — Art + atlases (Bob).** Drop ~241 PNGs; add the new singles at suffix-free names. Unity repacks the
five atlases automatically — the atlas assets changing is expected, not a defect.

**T-6 — Sweep.** `RegimentIconType` down to two members; `BASE_AIRBASE`'s dead IconProfile (§2.9); any
orphaned constants; update `Claude_Project.md` §3.5 (it documents the variant+flip model, which stops
being true).

---

## 5. DELETION MANIFEST (R2 — the old path goes, so this is the checklist)

| Item | Where |
|---|---|
| `RegimentIconType.Directional`, `.Directional_Fire` | `GameData.cs` |
| `RegimentIconProfile.W/NW/SW/W_F/NW_F/SW_F` | `EquipmentBays.cs` — ⚠ **after** T-2 step 1 |
| `GetDirectionalIcon`, `GetFiringIcon`, `GetAnimationFrame` (zero callers — flipbook is name-based) | `EquipmentBays.cs` |
| `IsValid` directional + firing arms | `EquipmentBays.cs` |
| `NormalizeDirection`, `ShouldFlipSprite` | `GameIconRenderer.cs` |
| `out bool shouldFlip` + both `flipX = true` writes | `GameIconRenderer.cs` |
| ~250 `_W`/`_NW`/`_SW`/`_F` unit constants | `SpriteManager.cs` |
| ~241 PNGs | `Art/Sprites/Unit Icons/` (Bob) |
| `BASE_AIRBASE.IconProfile` (already dead) | `WeaponProfileDB.cs` |

---

## 6. RISK + VERIFICATION

⚠ **Suite-invisible (§2.5).** Two cheap guards worth adding with the work:
1. **Profile-icon integrity test** — every registered profile's `IconProfile` is valid for its type; every
   `Single` profile has a non-empty icon; every `Helo_Animation` profile's `Icon` ends in `_Frame0` (the suffix IS the flipbook contract). This is the one
   that catches a botched bulk edit across 85 declarations, where a single profile silently loses its art.
2. **Rotation-mapping test** on the pure facing→degrees function, plus "a base maps to zero rotation"
   (R5). No Unity types needed.

**Play-verify (Bob):** every faction on Khost · all six facings via Shift+click · a moving unit (rotation
holds through the tween) · a helo (flipbook AND rotation together) · **a base at both sides' facings —
must NOT rotate** · air/ground stacking opacity · and the HP box, flag, deploy chevron and stacking badge
upright at every facing.

**Rollback** is a commit, not a toggle (R2) — which argues for landing T-1…T-4 as one reviewable commit
with the guard tests in it.

---

## 7. PROGRESS LOG

- 2026-08-27 (final check, pre-restart) — **Two corrections from the last verification pass.** (a) §2.1
  was WRONG: firing art DOES render — `GetIcon` routes `Directional_Fire` to the `_F` sprite for dug-in
  postures; the grep that "proved" it dead excluded the file the caller lives in. R1 stands, but the cost
  is the dug-in art variant, covered by the deployIcon chevron. (b) T-2 SIMPLIFIED: `GetAnimationFrame`
  has zero callers — the flipbook is sprite-name-based in `Prefab_CombatUnitIcon`, so helos need only
  `Icon = <name>_Frame0` and the frames-array idea is retired. `RegimentIconProfile` ends at ONE field.
  Plan is final; implementation (T-1…T-4 + guard tests, one commit) is the NEXT SESSION's first task —
  Soviet + MJ art is ready on Bob's side.

- 2026-08-27 (late) — **Self-correction after Bob asked for a consistency check.** Three declaration counts
  in the first draft were inflated (91/56/38/14) because the grep counted enum REFERENCES across all files
  rather than `WeaponProfileDB` DECLARATIONS. Authoritative: **89 `Single` · 53 `Directional` · 32
  `Directional_Fire` · 10 `Helo_Animation` = 184**, which reconciles with the 184 registered profiles.
  **85 declarations convert, not 94.** Also: helo art carries NO directional post-fix (`_Frame0..5` only),
  so R4 never touched it — Bob's correction confirmed against the files; and AH-1 shares the AH-64's frame
  set (§2.8b). Art-file math in §2.2 was measured from disk and is unaffected.

- 2026-08-27 — Plan finalised; all rulings in (R1–R6). Scan headlines: **helo frames ride the firing
  fields** (naive `_F` deletion kills the animation R3 protects — T-2 is ordered to prevent it); **firing
  art has zero callers and never rendered**, so dropping it is free; `Single` already is the target shape
  and covers 91 of 199 profiles, so only 94 convert; nothing rotates an icon today so the axis is free;
  **no test touches icons at all** — play-verified only unless the §6 guards ship. Two scope catches on
  R4: **86 bridge/river/chevron sprites are hex-EDGE features and must KEEP their suffixes**, and the
  `GER_`/`GE_` two-file anomaly is a free fix while renaming. Art math measured: 460 → ~219 files.
