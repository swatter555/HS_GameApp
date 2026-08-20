# NEW RULING — the Mission-Objective Gate (C6). V15 is redefined; E8 gains a field
**From:** Game side (Claude Code)
**Date:** 2026-08-17 (late)
**Ratified by:** Bob, this session
**Supersedes:** the V15 rip plan, and the "isObjective is fully obsoleted" premise from V1–V16 —
**in part**. Read §3 before updating your docs; your E8 mirror gains one field (§2).

## 1. The design

Share-based scoring alone lets a player avoid fortified strongpoints and farm weakly defended
sectors. Bob's ruling closes that: **every scenario authors one small set of MISSION OBJECTIVES, and
the player cannot reach the scenario's `requiredResult` until ALL of them are Red-controlled.**

- Defensive scenario → objectives sit on the player's side, held from turn 1: the mission is to keep
  them. Offensive → on the AI side: the mission is to take them. Sparse by convention — victory
  value keeps most of the weight; the gate is a necessary condition, not the score.
- **Gate unmet → the grade is capped ONE RUNG BELOW `requiredResult`** (not merely "below victory"):
  offensive/required-Minor caps at Draw = failed; defensive/required-Draw caps at MinorDefeat =
  failed; withdrawal/required-MinorDefeat caps at MajorDefeat. One rule, correct teeth in all three
  scenario shapes — a cap at bare Draw would have let a defender lose their objectives and still
  "pass" a Draw-required scenario.
- The gate also joins the early-finish availability (now three terms: `minor > 0` ∧ `share >= minor`
  ∧ `allObjectivesHeld`) and the nothing-further-to-gain auto-end.
- The share/ledger math is COMPLETELY untouched — value stays ungated on every hex.

## 2. Your E8 mirror — the ninth manifest key

```json
"missionObjectives": [
  { "x": 12, "y": 7, "label": "Khost airfield" },
  { "x": 20, "y": 14 }
]
```

- `x`/`y`: int hex coordinates (odd-r grid, same space as everything else). `label`: OPTIONAL string
  — feeds dispatches and UI ("Objective lost: Khost airfield"); omit freely.
- **Absent key or empty list = no gate, VALID** — same philosophy as all-zero thresholds, and what
  keeps every pre-C6 manifest loading. "Every scenario has at least one" is an authoring convention.
- `IsValid` refuses duplicates and entries outside the declared `mapWidth`/`mapHeight`.

## 3. ⚠ Why this is manifest data, and what happens to `isObjective` — UPDATE YOUR DOCS

The objectives live in the MANIFEST, not the `.map`, for the reason we ratified in V1–V16 and one
Bob's design adds: the same map must serve a defensive AND an offensive scenario with **different**
objective sets, which a flag baked into the map cannot express.

**But `isObjective` is NOT ripped — V15 as written is cancelled.** The runtime mechanism is a
**load-time stamp**: `MapLoader` (which already receives the manifest) CLEARS every authored
`isObjective` on the loaded map, then STAMPS the manifest's list onto the hexes. Gameplay (the gate),
UI (the existing city-prefab flag wiring, unchanged) and SAVES all read the stamped runtime flag.

The stamp-and-persist is *required*, not stylistic: our save contract makes in-battle saves
self-contained — loadable with the scenario uninstalled — so the gate cannot read the live manifest;
the embedded map must carry the objectives. Same snapshot doctrine as ".oob is a snapshot of the
unit DB": patching a manifest's objectives does not retroactively change an in-battle save.

**Your side, concretely:**
- **The authored `isObjective` value is now DEAD AND IGNORED by the game** — the loader clears it
  unconditionally. Your map-side objective-marking UI is obsolete; keep writing the key (any value)
  or eventually drop it per the usual format-bump rules — the game does not care. No re-export is
  needed for this; stale flags in shipped maps are harmless.
- Your in-progress map rewrite is unaffected (objectives are not map data anymore).
- Validation to mirror at authoring (E7-adjacent): an objective outside the map bounds is a
  game-side LOAD REFUSAL (G6 doctrine — manifest and map not exported together); an objective on a
  non-stronghold hex draws a loud game-side warning (convention: objectives belong on
  cities/installations — an open-ground objective flips by mere transit and the gate flickers).

## 4. Sequencing

Builds in our Stage 4 (grading/win-condition code + the manifest field + the MapLoader stamp) —
nothing in landed Stages 1–2 changes, Stage 3 (income) is unaffected and proceeds first. Khost's 12
formerly-authored objective hexes are the natural candidate seed for its manifest; Bob authors the
real set alongside your rebalance. Design-doc gains a new §17.x for the gate in the same Stage-4
amendment batch already owed.
