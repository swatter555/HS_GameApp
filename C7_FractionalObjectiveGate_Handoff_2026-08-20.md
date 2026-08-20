# C7 — FRACTIONAL MISSION-OBJECTIVE GATE + V19 KILL-PRESTIGE CONSTANT

**From:** Cowork (Scenario Editor) · **To:** the `HS_GameApp` agent · **Date:** 2026-08-20
**Ratified by Bob** in session, 2026-08-20. Two asks, independent — C7 is the substantial one.
**Written against live source**, not against my notes: every file, line and behaviour below was read out
of the tree today. Where I quote a line number it is from the file as it stands this morning.

---

## 0. TL;DR

1. **C7** — the C6 victory gate becomes **fractional**: hold at least a declared fraction of the mission
   objectives rather than all of them. One new manifest float, mirrored into `ScenarioData`, consumed at
   **three** call sites that must not be allowed to disagree. **Needs `SAVE_VERSION` 7 → 8, and per
   CLAUDE.md §2.12's pre-1.0 exception it ships WITHOUT a migration arm.**
2. **V19** — `PrestigeOnKill`'s `cost / 2` becomes a named `GameData` constant. Three identical private
   copies today. Trivial, and it unblocks Bob tuning the kill reward without a code hunt.

Backward compatibility for C7 is exact: the new field defaults to `1.0`, and `ceil(total × 1.0) == total`,
which is today's all-or-nothing behaviour. **No shipped content changes meaning.**

---

## 1. WHY — and the measurement that forced it

Khost was re-priced today (editor side, `ScenarioEditor/Maps/Khost.map`): 42 strongholds now carry value,
total **3,975**, player start **1,200 ⇒ `s0 = 0.3019`**, ladder **0.38 / 0.47 / 0.56**, all seven rungs
live under your own reachability rule. That part is healthy.

Then this fell out, and it is the reason for the ask:

> The nine mission objectives are worth **1,050 — 26.4% of the map**. Taking **all nine and nothing else**
> puts the player at **56.6%**, which is past the 56% decisive cut. Combine that with C6's absolute gate —
> miss one objective and the grade caps one rung below `requiredResult` — and the scenario collapses to
> **binary: hold all nine → DecisiveVictory; miss one → Draw or worse.**

`AuditLadderReachability` reports 7/7 and is **correct** to: the ladder is sound in isolation. What kills
MinorVictory and MajorVictory is the *interaction* between the objective set's share of map value and the
thresholds, and nothing on either side of the fence currently looks at both at once. I found it by hand.

⚠ **This is not a Khost quirk.** Any scenario whose objectives carry a large share of map value has the
same shape. It will recur on Hamburg.

**Bob's ruling:** the gate becomes a percentage, and — separately, already built — the player may end the
scenario once he is at or above `victoryThresholdMinor` with the gate met, choosing between banking the
result plus the unused-turn bonus and pushing on for a better rung. Measured on Khost, that trade is worth
roughly 820 prestige (≈3–4 regiments) to move two rungs, so it is a real decision.

### 1.1 ⭐ Scope reduction: ONE fraction, not a per-rung requirement

Bob's phrasing was "half the objectives for a minor, three-quarters for a major, all nine for a decisive."
**Do not build a per-rung objective requirement — the share ladder already produces exactly that**, because
objectives carry value like any other hex. Measured on the re-priced Khost, objectives taken cheapest-first
and nothing else:

| objectives held | share | grade |
|---|---|---|
| 3 of 9 | 35.8% | Draw |
| **5 of 9** | **39.6%** | **MinorVictory** |
| 7 of 9 | 45.3% | MinorVictory |
| 8 of 9 | 49.1% | MajorVictory |
| 9 of 9 | 56.6% | DecisiveVictory |

The graded progression is **already there**. The gate's only job is to set the floor below which the
mission counts as failed. **A single fraction is sufficient and a per-rung system would be redundant
machinery.** Bob's working number is **0.5 → any 5 of 9**, but that is authored per scenario, not fixed.

---

## 2. C7 — THE CHANGE

### 2.1 New manifest field

`Assets/Scripts/Core/Game Data/ScenarioManifest.cs`, in the SCORING + ECONOMY region (after
`RequiredResult`, before `MissionObjectives` — it reads better adjacent to the list it governs):

```csharp
/// <summary>
/// C7 — the FRACTION of <see cref="MissionObjectives"/> that must be player-held for the gate to be
/// met: required = ceil(total × fraction). 1.0 (the default) is C6's original all-or-nothing rule and
/// is what an absent key deserializes to, so every pre-C7 manifest keeps its exact behaviour.
/// Range (0, 1]. Bob's Khost setting is 0.5 — any 5 of 9.
/// ⚠ The fraction is mirrored into ScenarioData: an in-battle save restores without a manifest
/// (§7.3) and the gate is evaluated every turn boundary.
/// </summary>
[JsonPropertyName("missionObjectiveFraction")]
public float MissionObjectiveFraction { get; set; } = 1.0f;
```

**`IsValid()`** (currently `ScenarioManifest.cs:183`): refuse `<= 0f` and `> 1f`. A zero fraction is a gate
that is always met, which is indistinguishable from declaring no objectives and is better said that way;
above 1 is unsatisfiable. Both are authoring errors, not content states.

⚠ **Do NOT refuse the case `MissionObjectives.Count == 0 && MissionObjectiveFraction < 1`.** It is
meaningless but harmless (`ceil(0 × f) == 0`, gate vacuously met) and refusing it would make a manifest
vanish from the menu for a field nobody set on purpose. The editor hard-blocks it at authoring instead.

### 2.2 The predicate — and I'd ask you to return counts, not a bool

`Assets/Scripts/Utils/HexMapUtil.cs:212` currently:

```csharp
public static bool AllMissionObjectivesHeld(HexMap map)
```

Suggested replacement — **counts, with the threshold arithmetic living in one place in `BattleManager`**:

```csharp
/// <summary>
/// C7: (held, total) over the RUNTIME stamped IsObjective flags. Reads the map, NEVER the manifest —
/// an in-battle save restores without one (§7.3). Fresh every call, no cached gate state (the same
/// anti-drift rule as VictoryLedger). Fails OPEN on a null/broken map: (0, 0) reads as "no gate".
/// </summary>
public static (int held, int total) CountMissionObjectives(HexMap map)
```

Why counts rather than a bool: the HUD wants to say **"objectives 5 / 9"**, the debrief wants the same
string, and `AuditLadderReachability`'s sibling diagnostics want the numbers. A bool forces three
recomputations of the same loop. Keep `AllMissionObjectivesHeld` as a thin wrapper if anything outside
this pass still wants it — but note the three call sites below are, as far as I can see, its only callers.

**The gate test itself, one helper on `BattleManager`:**

```csharp
/// <summary>C7 gate: held >= ceil(total × fraction). Vacuously met when the scenario stamped none.</summary>
private bool MissionObjectiveGateMet(HexMap map)
{
    (int held, int total) = HexMapUtil.CountMissionObjectives(map);
    if (total <= 0) return true;
    return held >= RequiredObjectiveCount(total, MissionObjectiveFraction);
}

internal static int RequiredObjectiveCount(int total, float fraction)
{
    if (total <= 0) return 0;
    if (fraction >= 1f) return total;                    // exact, and the C6 default
    int required = (int)Math.Ceiling(total * (double)fraction - GATE_EPSILON);
    return Math.Clamp(required, 1, total);               // a declared gate always demands at least one
}
```

### 2.3 ⚠⚠ THE FLOAT TRAP — please do not skip this

`Math.Ceiling(total * fraction)` is wrong, and it is wrong in a way that only bites on specific pairs:

```
0.3f × 10  →  3.0000000000000004  →  Ceiling = 4     ✖  should be 3
0.7f × 10  →  6.999999999999999    →  Ceiling = 7     ✔  right by luck
0.5f × 9   →  4.5                  →  Ceiling = 5     ✔  exact
```

Hence `GATE_EPSILON` (1e-6 is ample; the inputs are two-decimal authored values). **Clamp to `[1, total]`
afterwards** so a rounding-down accident can never produce a gate of zero on a scenario that declared one.

I am flagging this with some feeling because **E14 shipped with exactly this bug on my side yesterday** —
`2×0.3 − 0.45` is `0.14999999999999997`, and an exact comparison reported a band 3e-17 wide as a live
ladder rung. Brute force caught it; reading did not.

**Test it directly** — `RequiredObjectiveCount(10, 0.3f) == 3` is the one that fails without the epsilon.

### 2.4 THE THREE CALL SITES — they must never disagree

All three are in `Assets/Scripts/Controllers/BattleManager.cs` and all three call
`AllMissionObjectivesHeld` today:

| # | site | line | what it does now | after C7 |
|---|---|---|---|---|
| 1 | `CompleteBattle` → `GradeBattleResult` | **1193** (`bool objectivesHeld = …`) | supplies the `objectivesHeld` arg that drives the one-rung-below cap | `MissionObjectiveGateMet(finalMap)` |
| 2 | `OnEndScenarioButton` | **1095** | refuses the early end unless share ≥ minor **and** all objectives held | `MissionObjectiveGateMet(map)` |
| 3 | `CheckVictoryConditions` | **1152** | refuses the auto-end-at-decisive unless all objectives held | `MissionObjectiveGateMet(map)` |

⚠ **The consistency requirement is most of the value of this ask.** Any two of these disagreeing produces
a specific, ugly bug:

- **1 stricter than 2** → the player is offered the early-end button, takes it, and is graded a rung below
  what the button implied. He will read that as the button lying to him.
- **3 looser than 1** → the battle auto-ends at a decisive share while the gate is unmet, and then grades
  capped. Your own comment at `CheckVictoryConditions` calls this out explicitly — *"the battle must never
  auto-end at a rung the gate would then deny"* — and that guard must survive the change.

The single shared helper in §2.2 is the cheapest way to make disagreement unrepresentable. **Please don't
inline the arithmetic three times.**

### 2.5 Persistence — `SAVE_VERSION` 7 → 8

This is the part that makes C7 more than a one-line change, and it follows directly from C6's own design.

`AllMissionObjectivesHeld` reads the runtime flags and never the manifest, deliberately, because an
in-battle save is self-contained (§7.3). The objective *flags* ride the embedded map — that is the C6
architecture paying off, as `todo_prestige.md` Stage 5 records. **The fraction has nothing to ride.** It
must therefore join the eight knobs already mirrored for exactly this reason.

`Assets/Scripts/Core/Persistence/GameDataObjects.cs`, `ScenarioData`, beside the threshold block at
lines 119–122:

```csharp
/// <summary>C7 gate fraction, mirrored from the manifest (V11.6 rationale) — the stamped objective
/// flags ride the embedded map, but the required FRACTION has no other carrier.</summary>
[JsonPropertyName("missionObjectiveFraction")] public float MissionObjectiveFraction { get; set; } = 1.0f;
```

Then the sync glue, which already has the shape:
- `BattleManager.CaptureScenarioState` (**1416**) — beside `data.EarlyFinishMultiplier` at **1430**
- `BattleManager.RestoreScenarioState` (**1447**) — beside the read at **1459**
- `BattleManager.GrabManifestData` (**~461–470**) — seed from the manifest beside `EarlyFinishMultiplier`
  at **467**
- a `public float MissionObjectiveFraction { get; private set; } = 1.0f;` beside **155**

**`GameData.SAVE_VERSION` 7 → 8** (`GameData.cs:1640`).

⚠ **And no migration arm — this is the CLAUDE.md §2.12 pre-1.0 exception, not an oversight.**
`SnapshotMapper.cs:32` has `MINIMUM_SUPPORTED_SAVE_VERSION = GameData.SAVE_VERSION`, so a v7 save is
**refused by the `RunMigrationLadder` floor check at `SnapshotMapper.cs:773` — which throws before any
step is looked up**; a `7 => MigrateV7ToV8` arm would be unreachable code impersonating a migration.
(The `snap.SaveVersion < CURRENT_SAVE_VERSION` branch at line 335 is the *upgrade* entry point, not the
refusal — the refusal is inside the ladder.) Please extend the deliberately-no-arm comment block
at `SnapshotMapper.cs:738-746` with the C7 line, the same way the prestige pass did — that comment is the
only thing standing between this rule and someone "fixing" the gap in six months.

⚠ Also note this collides with `Claude_AI_TODO.md` AI2b-3, which has its own queued `SAVE_VERSION` bump.
Whoever lands second takes 9. Worth a glance before you start so the two don't both claim 8.

### 2.6 Validation and diagnostics

- **`AuditLadderReachability`** — I would **not** extend it. It is exhaustive over the ladder (I verified
  that with a 58,140-ladder sweep) and its clean partition of the failure space is worth preserving. The
  gate interaction is a different question and lumping it in would muddy a proof that currently holds.
- **Instead, one line at battle start**, next to the existing audit call at **~496**: log
  `objectives {held}/{total}, gate requires {required}`. That is the number nobody could see today.
- **A warning worth having**, and it is the one that would have caught Khost: at battle start, compute the
  share the player would hold **if the gate were exactly met and nothing else taken**, and if that share
  already lands at or above `victoryThresholdDecisive`, say so — the middle rungs are then decorative.
  Cheap: it is one pass over the map. I am building the authoring-side version of this regardless (§4).

### 2.7 Tests

`Assets/Tests/EditorTests/MissionObjectiveGateTests.cs` (7 tests today) — extend rather than replace; the
existing seven all remain valid at fraction 1.0, which is itself the backward-compatibility proof:

- `RequiredObjectiveCount_Fraction1_IsAllOfThem` — the C6 equivalence, at several totals.
- `RequiredObjectiveCount_RoundsUp` — `(9, 0.5f) == 5`, `(4, 0.5f) == 2`, `(3, 0.5f) == 2`.
- **`RequiredObjectiveCount_FloatEdge_DoesNotOverCount` — `(10, 0.3f) == 3`.** ⭐ This is §2.3 and it is
  the one that fails without the epsilon.
- `RequiredObjectiveCount_NeverZero_WhenObjectivesDeclared` — tiny fractions clamp to 1.
- `Gate_PartialHold_MeetsFractionalGate` — 5 of 9 Red at 0.5 → met; 4 of 9 → not.
- `Gate_NoObjectives_VacuouslyMet_AtAnyFraction`.
- `Gate_NullMap_FailsOpen_AtAnyFraction` — preserve the existing fail-open contract.

`VictoryGradeTests.cs` (17 today):
- `Grade_FractionalGateMet_LeavesTheShareGrade` — the partial-hold analogue of `Grade_GateMet_…`.
- `Grade_FractionalGateUnmet_StillCapsOneRungBelow`.

New, and the point of the whole exercise:
- `EndScenarioEarly_AllowedOnFractionalGate` — share ≥ minor with 5 of 9 held permits the early end.
- **`AutoEnd_NeverFiresWhileGateUnmet` — the §2.4 invariant, asserted directly.**

Round-trip, in `PrestigePersistenceTests`:
- `MissionObjectiveFraction_SurvivesSaveLoad` — and please assert it survives at a **non-default** value,
  since a field that defaults correctly will round-trip "successfully" while being dropped on the wire.

---

## 3. V19 — kill prestige as a tunable constant

Three files carry an identical private helper returning **half** the destroyed unit's purchase cost:

- `Assets/Scripts/Models/Combat/GroundCombatAction.cs:248`
- `Assets/Scripts/Models/Combat/AmbushAction.cs:141`
- `Assets/Scripts/Models/Combat/IndirectCombatAction.cs:239`

```csharp
int cost = killed.GetActiveWeaponProfile()?.PrestigeCost ?? 0;
return cost / 2;
```

**Ask:** replace the literal with one named constant in `GameData` — the `Prestige Exceptions` region at
`GameData.cs:1560` is the obvious home, beside `PRESTIGE_CRUISE_BOMBER`:

```csharp
/// <summary>§18.2.3 — fraction of a destroyed unit's purchase cost paid to the killer. Bob's tuning dial.</summary>
public const float PRESTIGE_KILL_FRACTION = 0.5f;
```

…and have all three compute `(int)Math.Round(cost * GameData.PRESTIGE_KILL_FRACTION)`. Rounding rather
than truncation, since a fraction like 0.35 on a 45-point unit should not silently lose the remainder the
way integer division does.

**Context, so you can judge the priority:** Bob has confirmed the kill reward stays (he considered
removing it and decided against). It is **still not credited** — the value goes to
`PrestigeOwedToAttacker` / `OwedToAmbusher` / `OwedToFirer` and nothing consumes it, exactly as your
comments say. So V19 is preparation, not a live fix, and it can ride with the M13 wallet wiring if that
suits you better. Bob's reason for asking now is that he expects to tune this number a lot, and one
constant beats three files.

**Related, deferred, and Bob's own:** he has noticed units are markedly harder to kill than in Panzer
General and intends to **reduce the mujahideen count on Khost** — the terrain will not carry 40 defenders
against 7 Soviet manoeuvre regiments. That is content, not code, and it is not asked of you here.

---

## 4. WHAT THE EDITOR DOES (my side — stated so the boundary is clear)

**E15**, mine, starting when C7's field name and casing are confirmed:

1. Author `missionObjectiveFraction` in the manifest dialog, per variant, with a live "requires N of M"
   readout beside the objective list.
2. Hard-block `fraction <= 0`, `> 1`, and the case where the objective list is empty but a fraction was
   set — the states §2.1 asks you *not* to refuse at load, caught where they are actually authored.
3. **Add the collision check to the Scoring Report**: "share if the gate is exactly met and nothing else
   is taken", against the three thresholds, with a warning when meeting the gate already lands at or above
   the decisive cut. That is the Khost defect, and it is an authoring-time question, so the editor is the
   right catch point — the same division of labour we settled for the ladder audit, where you hold the
   runtime backstop and I hold the authoring gate.
4. Re-export both Khost variants once the field exists.

**Until C7 lands** the editor writes no `missionObjectiveFraction` key at all, which deserializes to 1.0
and is exactly today's behaviour. **Nothing is blocked on you** — I can finish Khost's manifests now and
add the field later.

---

## 5. WHAT I AM *NOT* ASKING FOR

Stated explicitly so it doesn't get built by inference:

- **No per-rung objective requirements** (§1.1) — the share ladder already grades them.
- **No change to `AuditLadderReachability`** (§2.6) — it is exhaustive and I would rather not disturb it.
- **No value weighting in the gate.** A count gate treats a 75-point hamlet as equal to a 300-point base,
  and that is **deliberate**: Bob's Caperi and Arghu are cheap on purpose, so that a poor corner of the map
  still costs the player a detachment. Weighting the gate by value would let a player skip both and still
  pass, which undoes the design. If this ever changes it will be a ruling from Bob, not an optimisation.
- **No change to `MissionObjective`** — the list shape is fine.

---

## 6. OPEN QUESTIONS FOR YOU

1. **`SAVE_VERSION` collision with `Claude_AI_TODO.md` AI2b-3** — who takes 8? Say the word and I will
   write whichever number you land on into the editor's mirror.
2. Do you want `AllMissionObjectivesHeld` **kept** as a wrapper, or deleted once the three call sites move?
   I grepped the tree: the only production callers are the three above; the rest are 5 assertions in
   `MissionObjectiveGateTests` plus a doc-comment reference at `HexTile.cs:72` that will want its wording
   updated either way.
3. Is the battle-start diagnostic in §2.6 worth having on your side, or should I keep that entirely on the
   authoring side? I lean toward both — you caught the Khost ladder at runtime *and* I caught it at
   authoring, and the redundancy is what made the prestige pass close on evidence.

---

*Editor-side state for context: Khost's values are written and verified (`ScenarioEditor/Maps/Khost.map`,
checksum `3bad85d7…`, re-derived by the editor after the write). Manifests and briefings are next. Per
Bob's 2026-08-20 ruling, all authoring happens in the Scenario Editor directory and he hand-moves finished
scenarios into StreamingAssets — so nothing in this pass touches your tree's content files.*
