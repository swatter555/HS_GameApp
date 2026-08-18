# Victory Scoring, Prestige Economy & Objective Obsolescence — Change Request
**From:** Editor side (Cowork / Lead Software Engineer)
**Date:** 2026-08-17 (PM)
**Ratified by:** Bob, this session
**Items:** **V1 – V16**

> ## ⚠ THIS SUPERSEDES `PrestigeVictory_Handoff_to_GameAgent_2026-08-17.md` (AM) ENTIRELY.
> If you have read the AM version, **discard it**. Three things in it were wrong and are corrected here:
> 1. It said *"Do not change `TerritoryService`'s flip rules."* **Withdrawn.** Bob has since ruled that
>    `isObjective` is to be obsoleted; the flip rules are now the primary surgical site (V3).
> 2. It said *"stop asserting on shipped Khost numbers — move those tests to a synthetic fixture."*
>    **Unnecessary.** I have since verified that **zero tests read any real content file** — no test in
>    `Assets/Tests` touches `StreamingAssets`, `MapLoader`, or `File.ReadAllText`. The only hit is the string
>    literal `"test.map"` at `MapStandardTests.cs:35`. You have no migration to do. Sorry for the noise.
> 3. It proposed `victoryValue > 0 ⟺ isObjective` as a hard invariant. **Reversed by Bob's ruling** — every
>    hex may carry value regardless of any flag.

---

## 0. Scope and non-scope

**Game-side code only.** Map data is ours: Bob and the editor side are rewriting `Hamburg.map` and
rebalancing `khost.map` (victory values, airbases, terrain, labels). Do not author, migrate or hand-patch
`.map` content — we will hand you rebalanced files. **Nothing in your test suite depends on that data**
(verified, see the box above), so our rewrite cannot break you.

**Out of scope here:** purchase/requisition UI, campaign progression, the `.cmp` format, core rosters, the
briefing. V14 specifies only the *interface* the spend sink needs so `todo_profiles.md` P4 plugs in without
rework.

### In-flight work I checked against, so this does not collide

| Thread | State | Interaction |
|---|---|---|
| `todo_domains.md` D-ladder | D0–D3 closed, **D3's suite run still owed**; D4 fixed-wing → N0–N3 naval queued | None. Different subsystems. |
| `todo_profiles.md` P4/P5 | **P4 requisition still open** | **V14 is P4's upstream.** Land V1–V13 first and P4 gets a working currency. |
| `todo.md` Phase 2 (campaign as data) | **PAUSED** by Bob | V13 flags a farming exploit that goes live when Phase 2 lands. Recorded, not solved. |
| `Claude_AI_TODO.md` AI2b-3 | Queued, **needs its own `SAVE_VERSION` bump** for `AIPerceptionState` | ⚠ **See V12.** `GameData.cs:1621` says D3 was meant to be the plan's only persistence bump. You now have two more queued. Consider riding them together. |
| `todo_census.md` | Blocks 1–7 landed | None. |

**Working constraint I have honoured throughout:** you cannot run Unity. Per `Claude_TODO.md:8-11` the
protocol is to say *"Please run Unity Test Runner for me"* and wait. **V16 names the four gates where a
suite run is genuinely needed**, so you are not asking Bob six times.

---

## 1. The design, in one page

Bob's model, ratified:

1. **Any hex may carry `victoryValue`** — an economic weight, not a badge. No flag gates it.
2. **Score = the player's share of total map victory value at scenario end**, onto the eight-rung
   `BattleResult` ladder (`GameData.cs:1268-1278`), which is currently declared and 100% unused.
3. **Income = prestige per turn for victory value currently controlled**, plus a flat stipend floor.
4. **A scenario declares which rung it demands and by when** — *"achieve a Major Victory within 18 days"* —
   replacing *"capture all objectives"*.
5. **Maps start as a stalemate by convention**, but scoring must not assume it: thresholds mirror around the
   **actual** starting share (V9).
6. **`isObjective` is obsoleted this pass and ripped later.** Gameplay stops reading it; the UI keeps
   reading it until its replacement exists (V4).

### Why the ledger, and not an accumulator

The current code is an accumulator: `TerritoryService.cs:104` builds an `ObjectiveCapture`,
`MovementController.cs:1469` turns it into `AddPrestige`, and `BattleManager` maintains three counters by
`++`/`--`. Extending that means instrumenting four flip rules, keeping them in sync forever, and adding
per-hex "already captured" state to defeat farming.

**Replace it with a recomputed ledger (V5): one pass over the hex map per turn.** It cannot drift, cannot
double-count, cannot miss a flip path added later, needs no anti-farm state, and is derived — so save/load
gets it free by recomputing on load. Cost is ~924 float adds on the largest authored preset, once per turn.

There is a second, sharper reason. `BattleManager.TotalObjectiveHexes` is seeded once at
`InitializeObjectivesFromMap()` (`:373-398`) and thereafter maintained only by `CaptureObjective()`/
`LoseObjective()` deltas that fire **exclusively** through `MovementController.ApplyTerritoryAccounting`.
Any control change by another path — and V3 creates several — silently desyncs it. A recomputed ledger has
no such failure mode by construction.

---

## 2. V1 — `IsStronghold`: a derived predicate to replace `isObjective` in gameplay ⭐

**File:** `Assets/Scripts/Models/Map/HexTile.cs`

Today `isObjective` is doing three unrelated jobs. One is dead, one is derivable, one is in the wrong file:

| Job | Disposition |
|---|---|
| Gate on scoring | **Dead** — removed by Bob's ruling (any hex may carry value) |
| **Capture difficulty** — exempt from transit flip and ZoC sweep; flips only when ended on | **Derive it** (V1) |
| **Mission focus** — the objective flag icon, "the briefing is about this place" | Scenario data, belongs in the manifest. **Deferred to the rip (V15).** |

### V1.1 — Add the predicate

Follow the house precedent for derived state — `HexTile.IsRiver` (`:142-148`) and `CombatUnit.ProjectsZoC`
(`:107-115`), both `[JsonIgnore]`, both commented with the single source of truth they defend:

```csharp
        /// <summary>
        /// True when this hex is an installation or built-up area that must be physically occupied to
        /// change hands (§6.13.8): exempt from transit flip and from the end-of-move ZoC sweep, taken only
        /// by a ground/helo unit that ENDS its move here.
        ///
        /// Derived from terrain and infrastructure so it cannot disagree with the map — the same reason
        /// MovementCost is recomputed from Terrain rather than trusted from file. ⚠ REPLACES the authored
        /// `isObjective` flag as the gameplay source of truth, 2026-08-17 (V1). `isObjective` survives as
        /// a UI-only marker until the manifest gains a mission-objective list; see V15.
        /// </summary>
        [JsonIgnore]
        public bool IsStronghold =>
            Terrain == TerrainType.MajorCity ||
            Terrain == TerrainType.MinorCity ||
            IsFort || IsAirbase || IsPort;
```

`TerrainType` members, confirmed (`GameData.cs:1342-1353`): `Water, Clear, Forest, Rough, Marsh, Mountains,
MinorCity, MajorCity, Impassable`. Note the ordinal order differs from alphabetical; `.map` files serialise
terrain by **name**, so ordinals are not on the wire.

### V1.2 — Impact, measured

I ran this predicate against the shipped `khost.map`: terrain is `Clear` 207 / `Rough` 171 / `Mountains` 162
/ `Impassable` 96 / `MinorCity` 34 / `MajorCity` 2. **36 hexes become strongholds where 12 are authored
objectives today — 3×, but still only 5% of 672.** Bob is rebalancing Khost regardless, so treat that as
information, not a regression.

### V1.3 — Why these five terms

Cities and forts are self-evident. Airbases and ports are included because an installation should not change
hands because a recon element drove past — and because on the new Hamburg map **all nine airbases sit on
`Clear` terrain**, so a cities-only rule would leave every airfield on the map bypassable.

⚠ **Known cosmetic consequence, flagged not solved.** `HexGridRenderer.DrawCityIconForHex` (`:673-681`)
instantiates the city prefab **only** for `MajorCity || MinorCity`, so a stronghold that is an airbase or
port on open ground gets no prefab and therefore no icon at all. That is already true today for authored
objectives on non-city terrain — it is not a new defect. If you want the render gate widened, that is a
separate small item; I did not fold it in because it touches prefab wiring and Bob has not asked for it.

---

## 3. V2 — Obsolete `isObjective` in the house style

Per the repo's dominant convention (prose tombstone, not `[Obsolete]` — which this codebase uses only on
methods with a named replacement and zero callers, never on fields or properties), amend the property at
`HexTile.cs:68-69`:

```csharp
        // ⚠ GAMEPLAY-DEAD SINCE 2026-08-17 (V2). This flag no longer drives tile control, scoring, or the
        // win condition — stickiness is derived (`IsStronghold`, V1) and score comes from `victoryValue`
        // summed over controlled hexes (V5). It survives ONLY as a UI marker: the objective flag sprite
        // (HexGridRenderer:696) and the info-panel feature line (Prefab_TerrainPanel:234).
        // It is scheduled for deletion once ScenarioManifest carries a mission-objective hex list — the
        // mission's focus is SCENARIO data and does not belong in the .map, which must serve several
        // scenarios. See V15. Do not add new readers.
        [JsonPropertyName("isObjective")]
        public bool IsObjective { get; set; } = false;
```

**V2.1** — Add the mirrored note at the other two sites a reader would look, matching the three-site
discipline used for the retired `maxCoreUnits` (`ScenarioManifest.cs:68-72` + `GameDataObjects.cs:91-94` +
`BattleManager.cs:134-136`): `HexGridRenderer.cs:696` and `Prefab_TerrainPanel.cs:234`.

**V2.2** — `HexTile.SetIsObjective(bool)` (`:528-546`) has exactly one caller in the whole repo —
`BoardAnalysisTests.cs:182`. Leave it, tombstoned, until V15; deleting it now only churns a test.

**V2.3** — **Do not rename or remove the JSON property.** The editor still writes it and both shipped maps
carry it on every hex. Per `CLAUDE.md` §2.11 the persisted name is frozen.

---

## 4. V3 — Move the flip rules onto `IsStronghold` ⭐ LOAD-BEARING

**File:** `Assets/Scripts/Services/TerritoryService.cs` (146 lines total). Three conditionals.

There is **no bypass-prevention or must-assault logic anywhere in the codebase** — I searched for
`MustAssault`, `NoBypass`, `IsSticky`. "Stickiness" is 100% a tile-control exemption, nothing more. These
three lines are its entire implementation.

```diff
  // :85  — transit exemption (§6.13.2 / §6.13.8)
- if (tile == null || tile.IsObjective) continue;
+ if (tile == null || tile.IsStronghold) continue;

  // :94  — final-hex branch (§17.5)
- if (destTile.IsObjective)
+ if (destTile.IsStronghold)

  // :119 — ZoC-sweep exemption (§6.13.3 / §6.13.8)
- if (nt == null || nt.IsObjective) continue;
+ if (nt == null || nt.IsStronghold) continue;
```

**V3.1 — Naming.** `ObjectiveCapture` and `TerritoryChangeResult.CapturedObjectives` (`:13-34`) now mean
"stronghold taken". Rename to `StrongholdCapture` / `CapturedStrongholds` if you like — these types are
internal to the service and its one caller (`MovementController.cs:1125`, `:1457-1486`), so it is safe.
Your call; I have kept the old names below for diffability.

**V3.2 — Do NOT touch `FlipTo` (`:138-144`)** or the ZoC neighbour enumeration. Note for the record: the
"ZoC sweep" at `:114-122` is **not** the real ZoC machinery — it is the six geometric neighbours via
`HexMapUtil.GetAllNeighborPositions`, with no enemy unit, spotting, or `ProjectsZoC` involved. The real ZoC
lives in two other places (`HexMapUtil.cs:337-350`, `RetreatResolver.cs:228-241`) and is untouched by this
change. The name at `:114` is rules-jargon.

**V3.3 — Delete the prestige award at `MovementController.cs:1469-1470`.** Income moves to upkeep (V7);
leaving both double-pays. Keep `PrinterDispatch.ReportObjectiveCaptured` (`:1474`) and the
`GameAudio.Play(SFX.ObjectiveCaptured)` at `:1477` — that feedback is the only outward signal a capture
produces and it is worth keeping. (Note `todo_audio.md` Phase 3 lists objective hooks as unwired; this one
is wired.)

**V3.4 — `bm.CaptureObjective()` / `bm.LoseObjective()` at `:1471` / `:1481`** currently mutate the three
counters and trigger the Panzer-Corps instant end. See V6.

---

## 5. V4 — `RegionGraph` (the second scoring gate)

**File:** `Assets/Scripts/Models/AI/RegionGraph.cs:229`

```csharp
if (tile.IsObjective) { region.ObjectiveCount++; region.ObjectiveValue += tile.VictoryValue; }
```

`Region.ObjectiveValue` is documented at `:31` as *"Σ VictoryValue over IsObjective hexes"*. Under Bob's
ruling that sum is wrong — it misses every valued non-stronghold. Split the two concepts:

```csharp
if (tile.IsStronghold) region.StrongholdCount++;
region.VictoryValue += tile.VictoryValue;      // every hex, no gate
```

`Region.ObjectiveValue` is read by nothing except `BoardAnalysisTests.cs:189` — no AI decision consumes it —
so this is a free rename. Update `RegionGraph.cs:30-31` declarations and the test (V16).

---

## 6. V5 — The victory ledger ⭐ LOAD-BEARING

**New type.** Suggested home: `Assets/Scripts/Models/Map/VictoryLedger.cs`.

```csharp
/// <summary>
/// Derived snapshot of victory-value distribution across the map. RECOMPUTED, never accumulated —
/// an incremental counter desyncs the moment a control change takes a path that forgot to update it,
/// which is exactly how TotalObjectiveHexes failed (§V5 rationale). Nothing here is serialized.
/// </summary>
public readonly struct VictoryLedger
{
    public float PlayerValue  { get; }   // Σ VictoryValue over TileControl.Red
    public float EnemyValue   { get; }   // ... Blue
    public float NeutralValue { get; }   // ... Grey / None
    public float TotalValue   { get; }
    public float PlayerShare  { get; }   // PlayerValue / TotalValue; 0f when TotalValue <= 0f
}
```

**Computation.** `HexMap` implements `IEnumerable<HexTile>` (`HexMap.cs:712-724`), so `foreach (var hex in
map)` works — the same pattern as `BattleManager.cs:382` and `HexGridRenderer.cs:319`. There is **no**
`AllTiles()`, no indexer, no public `Tiles` collection; `GetHexAt(Position2D)` is the only lookup.

```csharp
public static VictoryLedger Compute(HexMap map)
{
    double player = 0, enemy = 0, neutral = 0;      // ⚠ double — see V5.1
    if (map != null)
    {
        foreach (HexTile t in map)
        {
            if (t == null) continue;
            float v = t.VictoryValue;
            if (v <= 0f) continue;                   // most hexes; cheap skip
            switch (t.TileControl)
            {
                case TileControl.Red:  player  += v; break;
                case TileControl.Blue: enemy   += v; break;
                default:               neutral += v; break;   // Grey, None
            }
        }
    }
    return new VictoryLedger((float)player, (float)enemy, (float)neutral);
}
```

**V5.1 — ⚠ Accumulate in `double`, not `float`.** `HexMap.GetEnumerator` returns
`hexDictionary.Values.GetEnumerator()` — **dictionary insertion order, which is not coordinate order and is
not stable across a save/load round-trip.** Float addition is not associative, so a `float` accumulator can
produce a different last-bit sum for the same map depending on iteration order. That is normally harmless,
but `PlayerShare` is compared against thresholds in V9, and a boundary case could grade differently on
reload. A `double` accumulator over `float` inputs makes the sum exact for any realistic hex count. This is
cheap insurance; take it.

**V5.2 — Neutral value stays in the denominator.** `Grey`/`None` hexes with value count toward `TotalValue`
but credit neither side, so shares need not sum to 1. A map with real neutral ground correctly starts both
sides below 50%, and V9's mirror-around-the-starting-share handles it with no special case.

**V5.3 — Two call sites, exactly:**
- **Once at battle start**, after map + OOB load and before turn 1, to capture
  `BattleManager.StartingPlayerShare` (new `float`, `private set`). This is V9's mirror anchor and
  **cannot be recomputed later** — it must persist (V12).
- **Once per turn in `ProcessUpkeep`** (V7).

Not per move, not per flip, not in `Update()`. **Do not cache it behind a dirty flag** — caching is how this
class of bug returns.

**V5.4 — `TotalValue == 0` is legitimate, not exceptional.** Hamburg today and Khost before its rebalance
both have zero authored victory value. `PlayerShare` returns `0f`. V9 short-circuits to `Draw` and logs via
`AppService.CaptureUiMessage`. **Do not throw and do not divide.** This state will be hit on day one.

**V5.5 — Enumeration failure is silent.** `HexMap.GetEnumerator` catches, calls `AppService.HandleException`
and returns an **empty** enumerator (`:722`). A scan over a disposed map therefore yields a zero ledger
rather than an error — which V5.4 already handles safely. Keep the `TotalValue > 0` guards everywhere.

**V5.6 — Odd-row filler.** The `.map` carries the odd-row overhang as `Impassable` filler with
`tileControl: None` (`CLAUDE.md` map notes). It carries `victoryValue: 0`, so the `v <= 0f` skip drops it.
Do not "optimise" by iterating a computed rectangle that assumes those hexes are absent.

---

## 7. V6 — Retire the objective counters

**File:** `Assets/Scripts/Controllers/BattleManager.cs`

Three counters (`:142-144`) — `ObjectiveHexesOccupied`, `ObjectiveHexesUnoccupied`, `TotalObjectiveHexes` —
plus `InitializeObjectivesFromMap()` (`:373-398`), `CaptureObjective()` (`:1136`), `LoseObjective()`
(`:1163`), `SetTotalObjectiveHexes()` (`:1108`, **zero callers**) and `UpdateObjectiveStatus()` (`:1126`,
**zero callers**). All of it is superseded by the ledger.

**V6.1** — Delete the two zero-caller mutators outright.

**V6.2** — Delete `InitializeObjectivesFromMap()`; replace its call site (`:358`) with the V5.3 start-of-
battle ledger capture.

**V6.3** — `CaptureObjective()`/`LoseObjective()` exist mainly for their UI messages
(`"Objective captured! (3/12)"`). Keep a message, change its content to the ledger — e.g.
`"Stronghold taken — victory value 1240/2400 (52%)"`. Remove the counter arithmetic and the
`CheckVictoryConditions()` call inside `CaptureObjective` (`:1148-1151`); the instant-end moves to V10.

**V6.4 — ⚠ Save-shape change.** `GameDataObjects.cs:108,110` persist `objectiveHexesOccupied` and
`totalObjectiveHexes` (`ObjectiveHexesUnoccupied` is not persisted). Removing them changes the save shape —
folded into the single `SAVE_VERSION` bump in V12.

---

## 8. V7 — Per-turn income

**File:** `BattleManager.ProcessUpkeep(bool isPlayerSide)` (`:810`). It already runs once per side per turn
and already holds open stubs at `:818-819` and `:847` for §3.5.4–.6 depot generation. Income belongs there.

```csharp
// §3.5.x — Prestige income (V7). Player side only: the AI has no economy yet.
// ⚠ The symmetric AI branch is deliberately ABSENT, not forgotten — there is no AI turn today (G8/M13).
if (isPlayerSide)
{
    VictoryLedger ledger = VictoryLedger.Compute(GameDataManager.CurrentHexMap);
    CurrentLedger = ledger;                                    // new field; UI + debug read it

    double income = ManifestPrestigeStipend
                  + ledger.PlayerValue * ManifestPrestigeIncomeRate;

    if (ManifestProgressBonusRate > 0f && ledger.PlayerValue > HighWaterVictoryValue)
        income += (ledger.PlayerValue - HighWaterVictoryValue) * ManifestProgressBonusRate;

    if (ledger.PlayerValue > HighWaterVictoryValue)
        HighWaterVictoryValue = ledger.PlayerValue;

    int paid = (int)Math.Round(income);                        // round ONCE, at the end
    if (paid != 0) AddPrestige(paid);
}
```

**V7.1 — The stipend is the anti-death-spiral floor and is not optional.** Without it, a player losing
ground loses income, cannot afford replacements, and loses more ground. That spiral is worst in exactly the
defensive scenarios this design exists to enable. The stipend-to-rate ratio is the scenario's forgiveness
dial: stipend-heavy is generous, rate-heavy is brutal.

**V7.2 — The progress bonus is high-water-marked, and that is what defeats farming.**
`HighWaterVictoryValue` is one `float` on `BattleManager`, initialised to the starting `PlayerValue` and
**persisted** (V12). Bonus is paid only on value above the highest ever held, so losing 100 and retaking it
pays nothing. No per-hex flag, no capture event, no `.map` schema change. If Bob later wants a bigger thump
on capture, this rate is the knob.

**V7.3 — Round once.** Accumulate in `double`, `Math.Round` at the end. Rounding the components separately
loses a point per turn to truncation, which over 21 turns is a real number.

---

## 9. V8 — Fix the prestige plumbing ⭐ LOAD-BEARING

This is the single defect that makes the whole feature inert. `AddPrestige` (`:1215-1225`) increments
`PrestigeEarned`; the spendable pool is `CurrentPrestige` (`:454`, seeded from `manifest.prestigePool`).
**They are different fields and they never meet.** `SpendPrestige` (`:1230`) has zero callers and touches
only `PrestigeSpent`.

**V8.1** — `AddPrestige(int amount)` credits **both**: `CurrentPrestige += amount` and
`PrestigeEarned += amount`. No-op on `amount <= 0`.

**V8.2** — `SpendPrestige(int amount)` debits `CurrentPrestige` and adds to `PrestigeSpent`. **Change the
signature to `bool`**, returning `false` and mutating nothing when `amount > CurrentPrestige`. P4's purchase
flow needs an atomic check-and-debit; do not make callers pre-check then spend in two steps.

**V8.3 — `ResetBattle()` must reset `CurrentPrestige`.** Today `:1268-1269` resets `PrestigeEarned` and
`PrestigeSpent` only. Harmless right now because nothing writes `CurrentPrestige` after `:454` — **the
moment V8.1 lands, replaying a scenario inherits a stale pool.** Reset `HighWaterVictoryValue` and
`StartingPlayerShare` in the same block.

**V8.4** — Add `EventManager.OnPrestigeChanged(int newBalance, int delta)`. `EventManager` currently has no
economic or territorial event at all; the HUD and P4 will both want one and adding it now is free.

---

## 10. V9 — Scoring in `CompleteBattle`

`CompleteBattle()` (`:1040-1074`) hardcodes `CurrentResult = BattleResult.Draw;` behind a TODO at
`:1057-1059`. **This method has zero test coverage** — it is `private`, reached only from the turn coroutine
and `TriggerImmediateVictory`, and `TurnStructureTests` deliberately drives only the static helpers. So V9
is greenfield: it needs *new* tests, not migrated ones.

**V9.1 — Mirror around the starting share, not 0.5:**

```csharp
float s0 = StartingPlayerShare;                  // captured at battle start (V5.3), persisted (V12)
float minorDefeatCut    = 2f * s0 - minorVictoryCut;
float majorDefeatCut    = 2f * s0 - majorVictoryCut;
float decisiveDefeatCut = 2f * s0 - decisiveVictoryCut;
```

Since `decisiveCut > majorCut > minorCut > s0`, the mirrored cuts come out in correct descending order
automatically. This keeps the stalemate premise a design convention Bob can deliberately break, rather than
an assumption welded into the scoring function.

**V9.2 — The ladder:**

```csharp
private static BattleResult Grade(float share, float s0,
                                  float minorCut, float majorCut, float decisiveCut) => share switch
{
    _ when share >= decisiveCut          => BattleResult.DecisiveVictory,
    _ when share >= majorCut             => BattleResult.MajorVictory,
    _ when share >= minorCut             => BattleResult.MinorVictory,
    _ when share >  2f * s0 - minorCut   => BattleResult.Draw,
    _ when share >  2f * s0 - majorCut   => BattleResult.MinorDefeat,
    _ when share >  2f * s0 - decisiveCut=> BattleResult.MajorDefeat,
    _                                    => BattleResult.DecisiveDefeat
};
```
(Switch expression per `CLAUDE.md` §2.1.)

**V9.3 — Degenerate cases log, never throw:**
- `TotalValue <= 0f` → `Draw` + a loud `CaptureUiMessage`. **Every currently shipped map is in this state.**
- Thresholds `<= s0`, or not strictly ascending → manifest invalid; see V11.3.
- Battle already ended → the existing `_battleEnded` guard at `:612` covers it.

**V9.4 — Log the whole arithmetic** on completion: starting share, final share, each cut, resulting rung.
Bob will hand-tune these numbers across two maps; an opaque verdict is unusable to him.

---

## 11. V10 — Win condition and early finish

**V10.1 — Delete the all-objectives rule.** `CheckVictoryConditions()` (`:1012-1027`):

```csharp
// DELETE — this is what makes a defensive scenario unwinnable once both sides hold objectives
if (TotalObjectiveHexes > 0 && ObjectiveHexesOccupied >= TotalObjectiveHexes) return true;
```

Replace with a *nothing-further-to-gain* end, which keeps the Panzer-Corps feel without the all-or-nothing
trap:

```csharp
private bool CheckVictoryConditions()
{
    VictoryLedger l = CurrentLedger;
    if (l.TotalValue <= 0f) return false;
    return l.PlayerShare >= DecisiveVictoryCut;    // top rung reached; nothing better exists
}
```

**V10.2 — Early finish, and why it is the farming fix.** Under hold income a player who has already banked
their required rung is paid to sit still collecting income to `maxTurns`. Auto-ending at the *required* rung
would rob them of pushing for a better one, so instead:

> **The player may end the scenario voluntarily once `requiredResult` is met, and unused turns pay a bonus:**
> `bonus = unusedTurns × lastTurnIncome × earlyFinishMultiplier` (manifest float, default `1.25f`).

Any multiplier above 1.0 makes cashing out strictly dominate sitting still, at zero risk, with **no separate
number to balance** — it is derived from the income already being earned, so it cannot drift out of tune
when Bob retunes `prestigeIncomeRate`. And the only way to raise the bonus is to hold *more* value before
cashing out, which is the behaviour we want.

⚠ This needs a UI affordance — an "End Scenario" button, enabled once `requiredResult` is met. Per
`CLAUDE.md` §2.13, expose `public void OnEndScenarioButton()` and let Bob wire it in the Inspector; **do not
call `onClick.AddListener`**, and treat the method name as a public contract.

**V10.3 — Residual exploit, flagged not solved.** Hold-farming to the turn limit is still *possible*, just
strictly worse. It is harmless today — prestige is discarded at battle end, there is no `CampaignManager`,
no `.cmp`, and `CampaignData.CurrentPrestige` is a dead field. **It goes live when `todo.md` Phase 2 lands.**
Record it there; do not solve it now.

---

## 12. V11 — `ScenarioManifest` schema

### ⚠ V11.1 — THE TRAP. READ BEFORE ADDING A FIELD.

`ScenarioManifest` has **no parameterless constructor**. Its `[JsonConstructor]` (`:89-106`) takes **16
positional parameters** (14 required + `mapWidth`/`mapHeight` defaulted). Add a property *without* a matching
constructor parameter and `System.Text.Json` **silently leaves it at its default** — no exception, no
warning, no log.

Two ways out. **I recommend (a); it is your call since it touches a load path.**

- **(a) Kill the trap permanently.** Add a public parameterless constructor and **remove the
  `[JsonConstructor]` attribute**; STJ then populates via property setters. Every one of the 16 properties
  is already `public { get; set; }`, so nothing else changes. Keep the 16-param ctor as an unattributed
  convenience so `MapStandardTests.cs:32-39` still compiles. New fields then need touching in exactly one
  place, forever.
- **(b) Append with defaults.** Add each new param at the end of the positional list with a default, matching
  the `mapWidth = 0, mapHeight = 0` precedent. Takes the ctor to 23 parameters.

Either way the new field must also be added to `IsValid()` (`:133-160`).

### V11.2 — New fields

| JSON name | Type | Default | Meaning |
|---|---|---|---|
| `prestigeStipend` | `int` | `0` | Flat prestige per turn, independent of held value. The floor (V7.1). |
| `prestigeIncomeRate` | `float` | `0f` | Prestige per turn per point of held victory value. |
| `prestigeProgressBonusRate` | `float` | `0f` | Bonus per point of *new* value above the high-water mark (V7.2). `0` disables. |
| `earlyFinishMultiplier` | `float` | `1.25f` | Multiplier on `unusedTurns × lastTurnIncome` (V10.2). |
| `victoryThresholdMinor` | `float` | `0f` | Player share required for `MinorVictory`. |
| `victoryThresholdMajor` | `float` | `0f` | ... `MajorVictory`. |
| `victoryThresholdDecisive` | `float` | `0f` | ... `DecisiveVictory`. |
| `requiredResult` | `BattleResult` | `MinorVictory` | The rung the scenario demands. Briefing + campaign branching. |

### V11.3 — `IsValid()` additions

- `PrestigeStipend >= 0`; `PrestigeIncomeRate >= 0f`; `PrestigeProgressBonusRate >= 0f`;
  `EarlyFinishMultiplier >= 1f`
- All three thresholds within `[0f, 1f]`
- Strictly ascending: `minor < major < decisive`
- **If all three are 0, that is valid** — "this scenario declares no scoring", and V9.3 falls through to
  `Draw`. Failing validation here would stop both shipped manifests from loading.

### V11.4 — Enum by name

`requiredResult` is persisted. Per `CLAUDE.md` §2.10 it goes through `JsonPolicy.Content`, which registers
`JsonStringEnumConverter` and therefore reads both forms and writes names. **Do not construct a local
`JsonSerializerOptions`.** Per §2.11, `BattleResult` members may not be renamed once this ships.

### V11.5 — Update both shipped manifests

`StreamingAssets/Scenarios/khost/mission_khost.manifest` and
`StreamingAssets/Campaigns/grand_campaign/m01_khost/campaign_khost.manifest`. Placeholder values are fine —
Bob retunes once V14 exists.

### V11.6 — `ScenarioData` mirror

`GameDataObjects.cs:75-112` is a near-duplicate of the manifest fields for the save side. Mirror anything
that must survive a save; drop the objective counters per V6.4.

### V11.7 — The editor's manifest mirror is ours

The Scenario Editor holds a 16-key exact mirror of this class. **Send us the final field names and JSON
casing before you ship** and we will update it in the same beat. A mismatch produces a manifest the game
silently half-reads — V11.1 all over again.

---

## 13. V12 — Save persistence

Grep for `Prestige` in `SnapshotMapper.cs` returns **nothing**. Prestige does not survive a save today.

**V12.1 — Persist (real history, not derivable):** `CurrentPrestige`, `PrestigeEarned`, `PrestigeSpent`,
`StartingPlayerShare` (V9's mirror anchor — **cannot be recomputed after turn 1**), `HighWaterVictoryValue`
(V7.2's anti-farm mark — **cannot be recomputed**).

**V12.2 — Do NOT persist the `VictoryLedger`.** Derived state; recompute on load from the restored hex map.
Serialising it re-creates exactly the drift the recompute design prevents.

**V12.3 — `SAVE_VERSION` 6 → 7, and NO migration step.** Per `CLAUDE.md` §2.12 and the comment block at
`GameData.cs:1593-1623`: while `MINIMUM_SUPPORTED_SAVE_VERSION` tracks `SAVE_VERSION`
(`SnapshotMapper.cs:32`), an older save is refused by the floor check *before* the ladder is entered, so a
step would be unreachable code pretending to be a migration. **Bump both; write no arm.** You must:
- add a dated paragraph to the `GameData.cs:1593` comment block in the established style, and
- extend the *"NOTE there is deliberately NO … arm"* list at `SnapshotMapper.cs:728-732`.

**V12.4 — ⚠ Bump coordination.** `GameData.cs:1621` records that *"D3 was designated the plan's ONLY
persistence bump; anything else needing one should have ridden along here."* There are now **two** more
queued: this one, and `Claude_AI_TODO.md` AI2b-3 (`AIPerceptionState` into the snapshot). If AI2b-3 is close,
land them as one bump. Bob's call — raise it with him rather than deciding unilaterally.

---

## 14. V13 — Bug: `Prefab_CityIcon` throws on a flipped SV tile

**File:** `Prefab_CityIcon.cs:118-153`

The switch at `:124-141` covers BE, DE, FR, MJ, NE, UK, US, GE, CH, IR, IQ, SA, KW, None — **`SV` has no
arm** and falls to `_ => throw new ArgumentException(...)` at `:140`. It survives only because `:122`
(`if (tileControl != TileControl.Red)`) short-circuits on the comment at `:145` that *"all red controlled
tiles are always SV"*, and Khost's SV hexes all start Red.

**V3 promotes this from latent to routine.** Once every valued hex scores, Red/SV city hexes flip to Blue by
transit and ZoC sweep constantly. The throw is inside the `try`, so `:151` swallows it — meaning
`controlFlagRenderer.sprite` is **never assigned** and the city keeps the *previous owner's flag* while
spamming `HandleException` once per affected city per `RefreshMap()`. A silently wrong flag is worse than a
crash.

**Fix:** add the `SV` arm. Cheap; land it any time.

---

## 15. V14 — Interface for the spend sink (spec only)

This is `todo_profiles.md` **P4**, already on your board. Three things now prevent rework:

**V14.1** — `bool SpendPrestige(int)` per V8.2 — atomic check-and-debit.
**V14.2** — `OnPrestigeChanged` per V8.4, so the purchase UI is not polling.
**V14.3 — Prices already exist; no new field needed.** `WeaponProfile.PrestigeCost` (`:142`) is computed at
`:236-239` as `(int)_tier + (int)_type`; tiers Gen1–4 = 0/60/120/180 (`GameData.cs:622-628`), types 20–450
(`:633-659`). Across 178 profiles the range is **20–450**; a line regiment is **50 (INF) to 245 (Gen4
TANK)**, median ground unit ~**125–190**. `CombatUnit` has no price of its own — it inherits via
`GetActiveWeaponProfile()` (`CombatUnit.cs:383`). `GameData.cs:2043` `PRESTIGE_COST_MULT = 0.7f` (upgrade
discount) is declared and unused.

⚠ **Calibration blocker, stated plainly:** `prestigeIncomeRate`, `prestigeStipend` and the victory
thresholds **cannot be tuned until V14 exists.** Ship placeholders; expect Bob to retune. Do not present the
first numbers as balanced.

---

## 16. V15 — The rip (NOT this pass — the trigger condition)

`isObjective` is fully removable once `ScenarioManifest` carries a **mission-objective hex list**, because
"which places this mission is about" is scenario data and the `.map` is meant to serve several scenarios.
When that lands (naturally alongside `todo.md` Phase 2 or the briefing work), the rip is:

1. Delete `HexTile.IsObjective` + `SetIsObjective` (`:528-546`).
2. Point `HexGridRenderer.cs:696` and `Prefab_TerrainPanel.cs:234` at the manifest list.
3. `SAVE_VERSION` bump; tell the editor side to stop writing the key.

**Do not do any of this now.** Recorded so the tombstones in V2 have a named exit.

---

## 17. V16 — Sequencing, tests, and the four suite-run gates

Each stage compiles and is independently shippable.

| Stage | Items | Visible change | Suite run? |
|---|---|---|---|
| **1** | V5 ledger · V8 plumbing · V11 manifest | **None.** Ledger computed + logged; plumbing correct, nothing pays in. | **GATE 1** |
| **2** | V1 `IsStronghold` · V2 tombstones · V3 flip rules · V4 RegionGraph | Stickiness derives from terrain. Khost goes 12 → 36 strongholds. | **GATE 2** — the riskiest stage |
| **3** | V7 income · V3.3 delete the capture award · V6 counters | Prestige balance moves. Still unspendable. | — |
| **4** | V9 scoring · V10 win condition + early finish | Battles stop always ending `Draw`. | **GATE 3** |
| **5** | V12 persistence | Prestige survives a save. | — |
| **6** | V13 SV bug | Exception spam stops. Land any time. | — |
| **7** | V14 spend sink | `todo_profiles.md` P4. | **GATE 4** |

### V16.1 — Exactly what breaks, and where

**Only two test files.** I grepped all 55 EditorTests fixtures (542 `[Test]` attributes).

- **`TerritoryServiceTests.cs`** — the primary casualty, and the reason Stage 2 is the risky one. Its fixture
  `CreateClearMap` (`:20-33`) builds **every hex as `TerrainType.Clear`**, so under a terrain-derived
  predicate *no* hex is a stronghold and all three exemption tests collapse. The three lines
  `At(map, obj).IsObjective = true;` at **`:55`, `:83`, `:114`** must become
  `At(map, obj).SetTerrain(TerrainType.MinorCity);`. Affected tests:
  `Transit_FlipsNonObjectivePathHexes_ObjectiveExempt_DestinationFlips` (`:44`),
  `Objective_EndedOn_IsCaptured_AndReported` (`:78`),
  `ZocSweep_FlipsEnemyNeighbors_SkipsGreyAndObjectives` (`:101`).
  ⚠ `MinorCity` has `HexMovementCost.MinorCity = 1`, same as `Clear` (`GameData.cs:1367`), so movement costs
  in those fixtures do not change — the substitution is behaviourally clean.
- **`BoardAnalysisTests.cs:177-190`** — `RegionGraph_ObjectiveMetadata_Accumulates` builds
  `Strip(TerrainType.Clear, ...)` then calls `SetIsObjective(true)`. Rewrite against V4's split
  (`StrongholdCount` from terrain, `VictoryValue` ungated).

**Nothing else breaks.** Specifically: **no test reads `khost.map` or any StreamingAssets file** (verified —
grepped `Assets/Tests` for `StreamingAssets`, `MapLoader`, `File.ReadAllText`, `Application.dataPath`,
`khost`; the only hit is a `"test.map"` string literal). And **`CompleteBattle` has no coverage at all**, so
V9/V10 need new tests rather than migrated ones.

### V16.2 — Please add a shared map fixture

**Fourteen test files each carry a private, duplicated `CreateClearMap`/`Strip`/`OpenField`.**
`BaseTestFixture` offers no map helpers, and `todo_domains.md` records "eleven test fixtures swapped" during
the G3 constructor change — the duplication has already cost you once. The ledger tests need per-hex
`VictoryValue` + `TileControl` across many hexes. **Please add `MapFixtures.cs` beside `CombatTestDice.cs`**
(namespace `HammerAndSickle.Tests`, no `[Test]` attributes) rather than a fifteenth private copy. Useful
constraints: `HexMap(name, w, h)` throws below 10×10 and does **not** prefill tiles.

### V16.3 — New coverage worth writing

- `VictoryLedger` — empty map; all-neutral; mixed Red/Blue/Grey; `TotalValue == 0`; a hex with negative
  value (should it be rejected? your call, but decide deliberately).
- `Grade()` — each of the eight rungs; `s0` at 0.5 and deliberately off it; boundary equality at each cut.
- `AddPrestige`/`SpendPrestige` — insufficient funds returns `false` and mutates nothing; reset clears.
- High-water — lose value then retake it, assert bonus paid **once**.

### V16.4 — Do not touch

- `TerritoryService.FlipTo` (`:138-144`) and the ZoC neighbour enumeration (V3.2).
- `HexTile.OnDeserialized`'s `movementCost` recompute (`:249-256`) — live on five read paths
  (`MovementController.cs:892`, `HexMapUtil.cs:748`, `MobilityMap.cs:36`, `AmbushSiteCatalog.cs:79`,
  `RetreatResolver.cs:248`).
- Any persisted enum member **name** (`CLAUDE.md` §2.11).
- `On*Button()` method names (`CLAUDE.md` §2.13) — UnityEvent binds by string.
- `MapChecksumUtility`'s frozen options (`CLAUDE.md` §2.10 exception).
- The `.map` JSON key `isObjective` — the editor still writes it (V2.3).

---

## 18. Questions back to us

1. **V11.1 — (a) parameterless ctor, or (b) append positional params?** We recommend (a).
2. **V3.1 — rename `ObjectiveCapture` → `StrongholdCapture`?** Your call; tell us so our docs match.
3. **V11.7 — final JSON field names and casing**, before you ship, so we update the editor's mirror.
4. **V1.3 — do you want the city-icon render gate widened** so airbase/port strongholds get an icon? We did
   not fold it in.
5. **V12.4 — one save bump or two?** Coordinate with AI2b-3.
6. **Anything here that is really authoring work?** We are already rewriting both maps — hand it back.
