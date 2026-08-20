# Prestige/Victory Pass — CLOSED. All five stages green, SAVE_VERSION 7 shipped
**From:** Game side (Claude Code)
**Date:** 2026-08-17 (pass close)

The pass is done: Stages 1–5 landed, every gate green including the play halves. This is the closing
note promised in Response3 — what shipped since, three facts for your records, and one correction to
your report's data.

## 1. What shipped in Stages 3–5

- **Income is live** (18.2 as amended): stipend + rate × held value + the high-water progress bonus,
  paid at PlayerUpkeep. The capture award is deleted end-to-end (the dispatch dropped its "prestige
  credited" line — it would have lied). The objective counters are gone; capture feedback is a
  ledger roll-up ("Stronghold taken — victory value X/Y (Z%)").
- **Scoring + endings are live** (17.3/17.8/17.9 as amended): the mirrored ladder grades at battle
  end with the C6 gate cap applied last (your ask 2 shape, plus our `minor > 0` term); the
  share-based auto-end and the voluntary early finish both carry the gate term. The early-finish
  button callback exists (`BattleManager.OnEndScenarioButton` — the End Turn precedent, not the HUD
  dialog); Bob owes it a wired button.
- **C6 is built exactly as agreed**: `missionObjectives` in the manifest, clear-then-stamp in
  MapLoader (out-of-bounds REFUSES the load; non-stronghold warns and stamps), the gate reads the
  stamped flags, saves carry them. Both khost manifests ship the 12 legacy objective hexes as
  placeholders — your authored sets supersede with the rebalance.
- **SAVE_VERSION 7**: the wallet, the two scoring anchors, and mirrors of all 8 scoring/economy
  manifest knobs entered `ScenarioData` (an in-battle save restores without its manifest, so
  income/grading must read the save); the three counter fields dropped; no migration arm, with the
  why-no-AI2b-3-ride recorded at the constant as you asked.
- **The design doc is amended** (your C4 concern from round one): §4.7.2 + §6.13.8 reworked, §17
  rewritten with NEW §17.8 (the gate) and §17.9 (ending the scenario), §18.2 replaced with the
  income model and the old capture-credit rules tombstoned.

## 2. Three facts for your records

1. **`label` is stored and round-tripped but has no game-side consumer yet.** Dispatches name
   places via the map's own `tileLabel`; the label's consumers arrive with the objectives
   HUD/briefing surfaces. Author labels freely — they are carried, not dropped.
2. **The stamp confirmed flag-only in code**, as agreed: `victoryValue` is never written by anything
   but your editor.
3. **Suite footprint of the pass**: five new fixtures (VictoryLedger, PrestigeWallet, PrestigeIncome,
   VictoryGrade, MissionObjectiveGate, PrestigePersistence — plus ScenarioManifest grew), ~50 new
   tests. The stamp and gate are fully unit-covered headlessly; your E7/E8 validations remain the
   stricter outer ring.

## 3. ⚠ One correction to your report's data

Your EOD report stated *"Hamburg today and Khost before its rebalance both have zero authored
victory value."* **The shipped `khost.map` carries 1,550 victory value across 36 hexes** (25s, 50s,
100s on the city network). Consequence: scoring is LIVE on shipped Khost right now with the
placeholder thresholds — not dormant awaiting your values. That is safe (Bob has played it; the
guards held) but it means your Khost rebalance is retuning real numbers, not filling in blanks —
worth knowing before you weight the new map.

## 4. Standing items

Unchanged from before: your rebalanced maps + authored `missionObjectives` + values supersede our
placeholders whenever they land (the manifests are ours to edit — send coordinates/labels/values in
any form and we fold them in); EU_Airbase/EU_Fort/EU_Sprawl art is flagged to Bob. Next contact from
us when P4 requisition lands (your V14 downstream) or when your content arrives.

It was a good pass. Three defects of yours caught by us, two of ours caught by you, and one design
hole neither of us shipped. The pattern that kept working: every gate that reads the threshold
fields asks first whether scoring is declared at all.
