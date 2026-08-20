# Prestige/Victory Pass — end-of-day status: Stages 1–2 GREEN, renames final
**From:** Game side (Claude Code)
**Date:** 2026-08-17 (end of day)
**Re:** V1–V16 + both reply rounds

## Where the pass stands

**GATE 1 and GATE 2 are both GREEN, including the play halves** — Bob played Khost after Stage 2, not
just the suite: pass-by no-flip, end-on capture with dispatch + SFX, clean Console, correct control
flags both directions. Stages 1, 1b and 2 are committed. Remaining: Stage 3 (income switch-over),
Stage 4 (scoring + win condition + the §17/§18 design-doc amendments), Stage 5 (`SAVE_VERSION` 7).

## Locked in as of today — safe to finalize your docs against

- **The renames are final:** `StrongholdCapture` / `CapturedStrongholds`; stickiness =
  `HexTile.IsStronghold` (MajorCity ∥ MinorCity ∥ IsFort ∥ IsAirbase ∥ IsPort); `Region.StrongholdCount`
  + `Region.VictoryValue` (ungated). `isObjective` is tombstoned gameplay-dead with exactly two UI
  readers, V15 recorded as its exit.
- **The V11 schema as relayed in my first note is unchanged** — plus the two Stage 1b amendments from
  your reply: `requiredResult: "Ongoing"` is refused; `earlyFinishMultiplier` exactly 1.0 loads with a
  named warning. C5 (early finish gates on `minor > 0 && share >= minor`) is in the plan for Stage 4.
- **V13 and the V17 code side shipped with Stage 2.** V17 detail for your records: the missing-art
  warning is debounced to ONE line per (theme, iconType) per session, and UrbanSprawl no longer falls
  through to Middle-East art on other themes — it skips like Airbase/Fort. So on Hamburg, expect
  exactly one warning per icon type and NO icons of that type until the EU_ art lands. Not a bug.

## ⚠ One deviation from your V16 staging, for your records

**Your staging table has a latent inter-stage bug we hit in implementation: Stage 2 was NOT
independently shippable as specced.** V3 widens capture accounting to strongholds (36 on Khost) while
`TotalObjectiveHexes` still counts the 12 authored objectives — with the old
`Occupied >= Total` instant-win rule still live (its deletion was staged at V10, two stages later),
**any 12 stronghold captures spuriously auto-won the battle.** We pulled the win-rule retirement
forward into Stage 2: `CheckVictoryConditions` returns false with a dated comment. Interim behaviour
until Stage 4: no early end of any kind — battles run to the turn limit. Same end state, different
order; noting it so your copy of the staging table doesn't read as what shipped.

## Nothing needed from you

Map data work proceeds on your side as planned — note the stronghold COUNT on Khost will move with
your rebalance; what GATE 2 validated is the derivation, not the number 36. E7 (negative-value block)
and E8 (schema + two-state thresholds) as agreed. Next note from us lands when Stage 4 changes
anything shape-wise, or at pass close with the design-doc amendment list.
