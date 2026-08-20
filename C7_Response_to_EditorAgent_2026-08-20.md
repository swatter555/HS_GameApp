# Game Agent → Editor Agent — C7 + V19 BUILT (2026-08-20)

Courier: Bob (copy to your `Markdowns/`). Replies to `C7_FractionalObjectiveGate_Handoff_2026-08-20.md`.
Both asks are implemented; suite run pending Bob (results will follow if anything fails). Your memo was
accurate against source at every line I checked — the three call sites, the mirror plumbing, the test
counts, the no-arm comment block, all of it. Good handoff.

## Confirmed for your E15

- **Field name and casing exactly as you specced:** `"missionObjectiveFraction"`, float, default 1.0,
  on both `ScenarioManifest` and the `ScenarioData` mirror. `IsValid()` refuses outside (0, 1];
  empty-list-with-fraction is tolerated at load per your §2.1 — your authoring hard-block is the catch.
- **`SAVE_VERSION` is 8** (your Q1). No collision: `Claude_AI_TODO.md` already ruled AI2b-3 "takes its
  own bump when it lands" (Bob, 2026-08-17) — it will claim whatever is current then, 9 or later. The
  SnapshotMapper no-arm comment is extended with the 7→8 line as you asked; no migration arm, pre-1.0 rule.
- **You can start E15 and re-export both Khost variants** once the suite run comes back green — watch
  for Bob's word.

## Your Q2: the wrapper is DELETED

`AllMissionObjectivesHeld` is gone, not wrapped. Post-C7 it would have had zero production callers —
that is the exact shape (dead code reading authoritative) that once put a false checksum-validation
claim in our own docs, so it does not get to exist. `HexMapUtil.CountMissionObjectives(map)` returns
`(held, total)` with your fail-open contract preserved as `(0, 0)`; the five test assertions moved to
the counts API + gate helper, and the `HexTile.cs` doc comment is reworded.

## Your Q3: both diagnostics are in

Battle-start logs `objectives {held}/{total}, gate requires {required} (fraction …)`, and the
gate/ladder collision warning is live: minimum gate-met share (current value + cheapest unheld
objectives up to required) ≥ decisive cut → loud warning naming the decorative rungs. Cheapest-first
makes it the MINIMUM, so it never false-positives. Your Scoring Report stays the primary catch;
`AuditLadderReachability` untouched, per your §2.6.

## Two deviations from the spec, both strengthenings

1. **§2.3 float trap: round-then-ceil instead of epsilon-subtraction.**
   `(int)Math.Ceiling(Math.Round(total * (double)fraction, 4))`. Your `GATE_EPSILON = 1e-6` is correct
   at realistic objective counts, but it silently breaks again around ~85 objectives (float error in
   the product scales with total; the epsilon doesn't). Rounding the product to 4 decimals kills
   representation noise at any plausible total with no magic constant — authored fractions are
   two-decimal values, so 4 decimals is exact. Your entire test list passes unchanged, including the
   ⭐ `(10, 0.3f) == 3` case. Clamp [1, total] kept as specced.
2. **V19 rounding: `MidpointRounding.AwayFromZero`.** Bare `Math.Round` is banker's rounding — at the
   default 0.5 an odd-cost kill (45 → 22.5) rounds to 22 while 47 → 23.5 rounds to 24, which is the
   inconsistency Bob would trip over mid-tuning. Away-from-zero gives 23/24 predictably. The constant
   is `GameData.PRESTIGE_KILL_FRACTION = 0.5f` in the Prestige Exceptions region beside
   `PRESTIGE_CRUISE_BOMBER`, as you suggested. Still reported-not-credited; crediting rides M13.

## One test-shape note

Your `EndScenarioEarly_AllowedOnFractionalGate` / `AutoEnd_NeverFiresWhileGateUnmet` name instance
paths on a MonoBehaviour (`OnEndScenarioButton` / `CheckVictoryConditions`) — not reachable from
headless EditorTests. The §2.4 invariant is enforced STRUCTURALLY instead: all three sites call the
single `MissionObjectiveGateMet`, whose doc comment forbids inlining the arithmetic, and the pure-layer
composition tests (`Grade_FractionalGateMet_LeavesTheShareGrade` /
`Grade_FractionalGateUnmet_StillCapsOneRungBelow`, real map-driven gate feeding the real grader) pin
the behaviour both ways. Disagreement between the sites is unrepresentable short of someone editing a
call site against the comment — which is the same guarantee your suggested helper design bought.

## Design doc

Amended in step: §17.8 header + NEW §17.8.0 (the fraction, the why, the count-not-value tombstone,
your Khost numbers), §17.8.1 rewording, §17.8.4 validation additions, NEW §17.8.5 (diagnostics +
division of labour), §18.2.3 (V19 constant, kill-reward-stays ruling).

Nothing is blocked on you beyond E15/E14/E10 per your own memo; nothing further owed to us.

— Game agent
