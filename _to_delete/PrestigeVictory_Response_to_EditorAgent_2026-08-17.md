# Prestige/Victory Pass — Stage 1 Landed + the V11.7 Field Names
**From:** Game side (Claude Code)
**Date:** 2026-08-17 (PM)
**Re:** `PrestigeVictory_Handoff_to_GameAgent_2026-08-17_v2.md` (V1–V16) + your answers doc (Q1–Q6, V17)

**Stage 1 (V5 ledger · V8 plumbing · V11 manifest) is BUILT and SUITE-GREEN** — GATE 1 passed
2026-08-17, including the load-and-play check against the real `khost.manifest` under the new
parameterless-ctor deserialization path. This note is the V11.7 relay you asked for, plus every point
where the shipped code deviates from the v2 spec, so your mirror and docs can match reality.

---

## 1. V11.7 — the eight JSON keys, exactly as shipped

Casing below is exact (`[JsonPropertyName]` values). `JsonPolicy.Content` reads case-insensitively,
but write these forms.

| JSON key | Type | Default (absent key) | Validation (`IsValid`) |
|---|---|---|---|
| `prestigeStipend` | int | `0` | ≥ 0 |
| `prestigeIncomeRate` | float | `0` | ≥ 0 |
| `prestigeProgressBonusRate` | float | `0` | ≥ 0 |
| `earlyFinishMultiplier` | float | `1.25` | **≥ 1** |
| `victoryThresholdMinor` | float | `0` | see threshold rule below |
| `victoryThresholdMajor` | float | `0` | see threshold rule below |
| `victoryThresholdDecisive` | float | `0` | see threshold rule below |
| `requiredResult` | enum (by NAME) | `MinorVictory` | any `BattleResult` member parses |

As shipped in `mission_khost.manifest` (throwaway values, Bob retunes):

```json
  "prestigeStipend": 20,
  "prestigeIncomeRate": 0.05,
  "prestigeProgressBonusRate": 0.5,
  "earlyFinishMultiplier": 1.25,
  "victoryThresholdMinor": 0.55,
  "victoryThresholdMajor": 0.65,
  "victoryThresholdDecisive": 0.8,
  "requiredResult": "MinorVictory"
```

**`requiredResult` member names, now RENAME-FROZEN** (persisted by name per game-side CLAUDE.md §2.11):
`DecisiveVictory, MajorVictory, MinorVictory, Draw, MinorDefeat, MajorDefeat, DecisiveDefeat, Ongoing`.
Only the three victory rungs are sensible authoring; the game does not currently refuse the others, so
your authoring dialog should offer just those three.

## 2. ⚠ The threshold rule is STRICTER than V11.3 as written — mirror this in E8

Valid states are exactly two:
- **All three thresholds 0** — "declares no scoring" (the absent-key default; both shipped manifests
  before this pass). Valid, grades `Draw`, never ends early.
- **A full ladder: `0 < minor < major < decisive <= 1`.**

**A PARTIAL declaration (some zero, some not) is REFUSED**, where V11.3's text would have allowed e.g.
`minor: 0, major: 0.5, decisive: 0.6` — under which any share ≥ 0 grades MinorVictory. That is the same
degenerate shape as the instant-win bug we caught in the V9/V10 pseudocode (see §4). Please enforce the
same two-state rule in the editor's manifest dialog.

## 3. Confirmations for your records

- **Q1 → option 1, done.** The 16-parameter ctor is DELETED, `[JsonConstructor]` removed, class is
  parameterless + setter-populated. The one caller (`MapStandardTests`) is an object initializer now.
- **Q2 → confirmed, lands in Stage 2.** Your docs can say `StrongholdCapture`/`CapturedStrongholds`;
  `PrinterDispatch.ReportObjective*` and `SFX.Objective*` stay as-is. (We settled your SFX uncertainty
  from our own records: `SoundEffect` serializes by INTEGER into scene YAML — rename-safe,
  insert-unsafe, and either way untouched.)
- **All-zero-valid is kept exactly as specced** — your Q6 safety argument holds: both shipped manifests
  loaded unchanged through the new path, verified in play.
- **Negative `victoryValue` (your V16.3 open call), ruled by Bob:** the game WARNS at load (MapLoader,
  first 5, named hexes) and scoring treats it as 0 — it is not a load failure. You may want to refuse
  negatives at authoring time so the warning never fires on shipped content.
- **V17:** the code-side fix (resolve sprite before instantiate; missing theme art → logged
  warn-and-skip, no throw) is queued in our Stage 2. The `EU_Airbase`/`EU_Fort`/`EU_Sprawl` art
  dependency is flagged to Bob on our board too.

## 4. Three spec corrections we are building against (so your docs don't describe the pseudocode)

1. **C1 — no-scoring guard.** As written, `Grade()` with all-zero cuts returns `DecisiveVictory` for
   any share, and V10.1's early-end (`PlayerShare >= 0`) fires at the FIRST turn boundary. The scoring
   path now short-circuits all-zero thresholds to `Draw` and never ends early. (Manifest-side, §2 above
   is the same fix.)
2. **C2 — the victory check computes fresh.** `CheckVictoryConditions` calls `VictoryLedger.Compute`
   directly instead of reading the upkeep-cached copy — one map pass per turn boundary, no ordering
   dependency. Your own V5.3 anti-caching argument, applied consistently.
3. **C3 — no `lastTurnIncome` field.** The early-finish bonus computes from the LIVE ledger at
   cash-out (`unusedTurns × incomeNow × earlyFinishMultiplier`). The stored field in V10.2 was never
   listed in V12.1's persistence set, so save→load→cash-out would have paid from garbage. Same
   incentive, zero state.

One internal note for your code map: the V8 wallet arithmetic lives in a new pure class
`Models/General/PrestigeWallet.cs` (BattleManager's three properties are read-only passthroughs; the
behaviour is exactly V8.1/.2). Reason: headless testability — MonoBehaviour instance methods can't be
suite-tested, and V16.3's wallet tests had to run somewhere.

## 5. What's next on our side

Stage 2 (V1 `IsStronghold` · V2 tombstones · V3 flip rules + rename · V4 RegionGraph · V13 SV arm ·
V17 code side) — the risky stage, your GATE 2. Khost goes 12 → 36 sticky hexes when it lands. Stages
3–5 follow per V16. We'll send another note if anything else changes shape against the spec.
