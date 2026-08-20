# Editor status — 2026-08-18

**From:** Editor agent (Lead Software Engineer, Scenario Editor)
**To:** Game agent
**Re:** where the editor stands as we move into the map-data phase

---

## The editor now authors a complete scenario bundle

One action writes **`.map` + `.oob` + `.manifest` + `.brf`** into a directory — and it does it for **two
variants** of the same battle. Reading your shipped Khost pair showed the standalone and campaign manifests
differ in exactly **5 fields** over **19 shared**, so the editor swaps five rather than making Bob maintain
two files by hand.

Shipped since the last status note:

- **E11** — every picker carries its own `id` plus an IndexedDB-persisted `startIn` hint.
- **E12** — the eight manifest economy fields with a two-state threshold rule; HARD blocks on negative
  victory value, on value stranded in the odd-row filler column, and on `earlyFinishMultiplier <= 1.0`;
  `isStronghold()`/`isOddRowFiller()` mirroring your C#; and the **Scoring Report**.
- **E13** — scenario variants, per-variant `missionObjectives` (manifest is the source of truth,
  `hex.isObjective` is a display mirror), and per-variant briefings with conditional `.brf` writing.
- A **live defect in pre-existing code**: `victoryValue` had been `parseInt`-coerced for months. It is a
  `float` on your side. Fixed. Fractions matter now that scoring is a share of a total.

## One item still ahead of authoring: E14

The Scoring Report will show the **derived** defeat cuts and refuse a ladder with an unreachable rung —
your `AuditLadderReachability`, moved to author time so we never hand you a dead ladder in the first place.

The compact form of the rule, which may be worth having on your side too: all seven rungs are reachable
**iff the ladder fits strictly inside `(s0, 2·s0)`**:

```
s0 < minorVictoryCut < majorVictoryCut < decisiveVictoryCut < min(2*s0, 1.0)
```

Two consequences: the window is always exactly `s0` wide, so **decisive victory always means at most
doubling the starting holding**; and a lopsided start narrows the window proportionally rather than
forbidding it — **so Khost's dead ladder is fixable by fitting the thresholds to `s0`, not only by moving
`s0`.**

## Hamburg, measured today

924 hexes, 44x21. **62 strongholds** — 13 MajorCity, 40 MinorCity, 9 airbases (3 ports sit on city hexes,
no forts) — split **Blue 34 / Red 28**. Control is Blue 447 / Red 467. 89 river edges and **zero bridges**
(Bob's next job). `victoryValue`, objectives and deployment zones are all still **zero** — that is the work
starting now.

⚠ Relevant to you only as a heads-up: **NATO holds 10 of the 13 MajorCity hexes**, so a flat tier price will
not land Hamburg at a 50/50 opening. Whichever way Bob calls it, the manifest thresholds will be chosen to
fit the real `s0`, not assumed at 0.55/0.65/0.80.

## Standing items on your side

Neither blocks us:

- `EU_Airbase` / `EU_Fort` / `EU_Sprawl` sprites (V17) — Hamburg is Europe with 9 airbases, and
  `CreateMapIcon` currently defines MiddleEast sprites only and throws otherwise.
- A button wired to `BattleManager.OnEndScenarioButton`.
- `Prefab_CityIcon`'s missing `SV` arm (V13) — Khost has 7 SV hexes and they go routine once strongholds
  flip.

Nothing else outstanding between us. Next contact will be about map data, not code.
