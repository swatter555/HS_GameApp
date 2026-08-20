# Answers to the Six Open Questions — V1–V16
**From:** Editor side (Cowork / Lead Software Engineer)
**Date:** 2026-08-17 (PM)
**Re:** `PrestigeVictory_Handoff_to_GameAgent_2026-08-17_v2.md` §18

Bob's answers, confirmed against the live tree. **Five accepted as written. One refinement on Q1, one
scope boundary on Q2, and one NEW blocker found while checking Q4 (see V17 at the end).**

---

## Q1 — `ScenarioManifest` constructor → **(a), CONFIRMED, with one refinement**

Bob's citation is real and I verified it. `Claude_Project.md` §15.2:

> **Two patterns:** Small (<=6 props): explicit constructor with matching params.
> **Large (7+): parameterless constructor, `[JsonInclude]` on private setters**, transient state in `Initialize()`.

`ScenarioManifest` goes 16 → 24 serialized properties with V11.2. It is squarely in the "Large" pattern and
has been on the wrong side of your own rule since before this change request existed. (Its properties are
all `public { get; set; }`, so the `[JsonInclude]` half does not apply — nothing to do there.)

### ⚠ The refinement: do not keep the 16-param ctor as a *public* convenience

That collides with **§15.2 rule 3** — *"`[JsonConstructor]` mandatory with multiple constructors — wrong
constructor = garbage data."* A public parameterless ctor plus a public 16-param ctor with no
`[JsonConstructor]` is exactly the shape that rule warns about. In practice System.Text.Json resolves it
deterministically (a public parameterless constructor wins), so it would work — but it leaves your codebase
contradicting its own written rule, and the next reader has to know an unwritten STJ detail to see why it is
safe.

Three ways out, ranked:

1. **Delete the 16-param ctor; rewrite its one call site as an object initializer.** The only caller is
   `MapStandardTests.cs:32-39`. This is strictly better than keeping it: that test currently passes 16
   positional arguments and **would have to change every time a field is added** — which defeats the
   "touch one place forever" goal the parameterless ctor is for. An object initializer never changes.
2. **Make it `internal`.** `AssemblyInfo.cs` already grants `InternalsVisibleTo("EditorTests")`, so the test
   still compiles and STJ stops considering it. Precedented — that seam exists for `RunMigrationLadder`.
3. Keep both public and amend §15.2 rule 3 to note the parameterless-wins exception.

**We recommend 1.** Either way, `IsValid()` still needs the V11.3 additions.

Related, and it confirms V1: **§15.2 rule 4** — *"`[JsonIgnore]` on every computed/transient property"* — is
exactly why `IsStronghold` is specced `[JsonIgnore]`. Same rule, same reason.

---

## Q2 — Rename to `StrongholdCapture` / `CapturedStrongholds` → **CONFIRMED**

Verified the blast radius. `ObjectiveCapture`, `TerritoryChangeResult` and `CapturedObjectives` appear in
**exactly four files**, and **none of them is a persistence path**:

| File | Sites |
|---|---|
| `Scripts/Services/TerritoryService.cs` | `:13`, `:23`, `:29`, `:33`, `:65`, `:67`, `:70`, `:101` |
| `Scripts/Controllers/MovementController.cs` | `:1457`, `:1459`, `:1465` |
| `Tests/EditorTests/TerritoryServiceTests.cs` | `:69`, `:90`, `:91`, `:92` |

Nothing in `Core/Persistence/`, nothing in a `[JsonPropertyName]`, no enum involved — so `CLAUDE.md` §2.11
does not apply and the rename is free. Bob's reason ("the old names would lie") stands.

### Scope boundary — three things to change WITH it, and two NOT to

**Also update:**
- `TerritoryChangeResult.FlippedHexes` doc comment (`:25`) — it says *"Non-objective hexes"*, which becomes
  wrong. → *"Non-stronghold hexes"*.
- `ApplyMoveControl`'s `<summary>` (`:53-64`) — four references to "objective" describing rules that now key
  on stronghold.
- The three test assertion messages at `TerritoryServiceTests.cs:69`, `:90`, `:91` — they cite §6.13.8 /
  §18.2.1 and should keep citing them, just with the corrected noun.

**Do NOT rename:**
- `PrinterDispatch.ReportObjectiveCaptured` / `ReportObjectiveLost` (`:437`, `:462`) — player-facing message
  helpers; "objective" is the right word to show a player regardless of the internal type name.
- `SFX.ObjectiveCaptured` / `SFX.ObjectiveLost` (used at `MovementController.cs:1477`, `:1483`). Different
  subsystem with its own in-flight thread (`todo_audio.md` Phase 3), and we have not verified whether the
  `SFX` enum is persisted anywhere. Leave it alone; renaming it buys nothing.

---

## Q3 — Final JSON names after V11 lands → **CONFIRMED**

One practical consequence, so it is not a surprise: **until you send the names, the editor's manifest
authoring dialog cannot emit the eight new keys.** Any manifest Bob authors in the editor before then will
be missing them. That is harmless *given* V11.3's all-thresholds-zero-is-valid rule (absent keys take
defaults, manifest still loads, scenario simply declares no scoring) — but it means **no editor-authored
manifest can be scored until E8 lands on our side.** Not a blocker, just don't let it read as a bug.

---

## Q4 — Render gate → **Bob is CORRECT on the facts. Defer accepted. But see V17.**

Confirmed: airbases and forts **do** get a map icon, independent of the city prefab —
`HexGridRenderer.DrawMapIconsForHex:607-615`:

```csharp
if (hex.IsAirbase) CreateMapIcon(hex, MapIconType.Airbase);
else if (hex.IsFort) CreateMapIcon(hex, MapIconType.Fort);
else if (hex.UrbanDamage > 0) CreateMapIcon(hex, MapIconType.UrbanSprawl);
```

So my V1.3 warning was **overstated** — I only checked `DrawCityIconForHex` and missed this pass. Bob's
correction stands: what is actually missing on a flipped non-city stronghold is the **control flag**, which
lives on the city prefab only. Cosmetic, becomes routinely visible after V3, worth a small follow-up rather
than silence. **Agreed — defer, with the follow-up logged.**

Two smaller notes on that same method, for whoever picks the follow-up up:
- **`IsPort` draws nothing at all.** `MapIconType` is `{ Airbase, Fort, UrbanSprawl }` — there is no port
  icon. Harmless on Hamburg (all three ports sit on city terrain and get the city prefab) but it means a
  port on open ground is an invisible stronghold.
- The chain is `else if`, so an airbase that is also a fort shows only the airbase icon. Probably intended;
  noting it so it is not rediscovered as a bug.

---

## Q5 — One save bump now (6 → 7) → **CONFIRMED**

Agreed, and Bob's reasoning is the right one: holding a ready bump hostage to an unscheduled one is how the
v4 amend-in-place shortcut happened, and that precedent is documented in the very comment block being
edited (`GameData.cs:1604-1610`).

**One addition, and please do not skip it.** `GameData.cs:1621` currently reads:

> *"⚠ D3 was designated the plan's ONLY persistence bump; anything else needing one should have ridden
> along here."*

A future reader hitting that line next to a 6→7 bump will assume someone broke the rule. **Record why it did
not ride along** in the new dated paragraph — that AI2b-3 sits behind M13-scale work with no date, and that
deferring a ready bump to wait for an unscheduled one is the worse failure. Same style as the existing
paragraphs. And extend the *"NOTE there is deliberately NO … arm"* list at `SnapshotMapper.cs:728-732` per
V12.3.

---

## Q6 — Manifests are Bob's → **CONFIRMED**

Correct, and settled 2026-07-28. Worth stating **why this is safe**, since it depends on a V11 detail:

V11.1 option (a) plus **V11.3's "all three thresholds at 0 is VALID"** rule means absent keys take their
defaults and **both shipped manifests keep loading unchanged** the moment your schema change lands. If any
new field were made *required* in `IsValid()`, the shipped content would break immediately — so please keep
that rule exactly as specced. It is the thing making V11.5 optional rather than urgent.

**Practical note:** you will still want values in those files to exercise V9's scoring during development.
Write throwaway numbers freely — Bob's authored values supersede them and neither of us is treating yours as
balanced.

---

## ⭐ V17 — NEW BLOCKER found while verifying Q4: map icons throw on any non-MiddleEast theme

Not caused by this change request, but it lands the instant Bob loads Hamburg, so it belongs here.

`HexGridRenderer.CreateMapIcon` (`:618-647`):

```csharp
MapIconType.Airbase => theme switch
{
    MapTheme.MiddleEast => SpriteManager.ME_Airbase,
    _ => throw new ArgumentException($"{CLASS_NAME}.CreateMapIcon: Airbase icon not defined for map theme '{theme}'.")
},
MapIconType.Fort => theme switch
{
    MapTheme.MiddleEast => SpriteManager.ME_Fort,
    _ => throw new ArgumentException($"{CLASS_NAME}.CreateMapIcon: Fort icon not defined for map theme '{theme}'.")
},
_ => SpriteManager.ME_Sprawl
```

`MapTheme` is `{ MiddleEast, Europe, China }` (`GameData.cs:1432-1437`). **Only MiddleEast has sprites.**

**Hamburg is `MapTheme.Europe` and carries nine airbases.** Every one throws on every `RefreshMap()`. The
throw is caught by `DrawMapIconsForHex:615` → `AppService.HandleException`, so it degrades to a missing icon
plus nine log lines per full repaint — and `RefreshMap` is a full teardown-and-rebuild called after every
territorial change, so under V3 that is *frequently*.

Third arm is worse in a quieter way: `UrbanSprawl` falls through to `_ => SpriteManager.ME_Sprawl`
unconditionally, so a **Middle-East-styled sprawl sprite renders on a German map**. No throw, no log, just
wrong art. (Hamburg's eight stray `urbanDamage` hexes are ours and we are clearing them, so this is latent
rather than live — but it will bite the first time a European city takes damage in play.)

**This is content, not code** — someone has to draw `EU_Airbase`, `EU_Fort`, `EU_Sprawl` and register them
in `SpriteManager`. **Flagging it to Bob as an art dependency for Hamburg, not asking you to invent
sprites.** What is worth doing in code: make the fallback a *logged missing-asset warning returning null*
rather than a throw, so an unthemed icon is a quiet gap instead of exception spam on every repaint.

---

## Summary

| Q | Answer | Status |
|---|---|---|
| 1 | Parameterless ctor, per §15.2 | ✅ **Confirmed** — verified §15.2 says exactly this. ⚠ Delete the 16-param ctor rather than keeping it public (§15.2 rule 3, and the test stops changing per-field). |
| 2 | Rename to `StrongholdCapture` | ✅ **Confirmed** — 4 files, zero persistence paths. Scope boundary above: update 3 doc comments, leave `PrinterDispatch` and `SFX` alone. |
| 3 | Names after V11 lands | ✅ **Confirmed** — editor can't emit the new keys until then; harmless given V11.3. |
| 4 | Defer the render gate | ✅ **Confirmed, and my V1.3 was overstated** — airbases/forts DO get icons. But see **V17**. |
| 5 | One bump now, 6→7 | ✅ **Confirmed** — also record *why* it didn't ride with AI2b-3, next to the `GameData.cs:1621` note. |
| 6 | Manifests are Bob's | ✅ **Confirmed** — safe *because* V11.3 keeps all-zero valid. Keep that rule. Write throwaway values for your own testing. |
