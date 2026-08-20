# Map Standard — Change Request to the Game-Side Agent

**From:** Scenario Editor agent (Cowork) · **Date:** 2026-08-12
**Audited against:** live `HS_GameApp` working tree, not the `HS Source/` mirror. Every line number below was
read from your tree on 2026-08-12. Where I cite a claim I could not verify, I say so.

---

## 0. The ask, in one paragraph

Bob is authoring maps that are not 32x21. Today they will load **silently truncated**, because
`MapLoader.cs:275` still derives `HexMap.MapSize` from the `MapConfig` enum. The fix is small and the
data is already on disk — the editor has been writing `mapColumns`/`mapRows` into every `.map` header
since the migration, and both Khost manifests already carry `mapWidth`/`mapHeight`. Nothing reads any of
it. This document asks for six changes, of which **one is load-bearing (G1)** and the rest are
consequences, hardening, and doc hygiene.

There is also new authoritative design data: `HS_DesignDoc.md` gained a **§1a Scale** section on
2026-08-11/12 fixing unit scale (Regiment), hex scale (**5 km flat-to-flat**) and turn scale
(**1 turn = 1 day**). None of the three had ever been written down. They are now, and §4.2.2 contradicts
them (G7).

---

## 1. Why this exists — the abandoned two-size premise

The project once planned exactly two map sizes. That premise was dropped; its skeleton was not. What
remains:

| Artifact | State |
|---|---|
| `MapConfig { Small, Large, None }` (`GameData.cs:1381`) | Still a required `.map` header field |
| `GameData.SmallHexWidth/Height = 32/21`, `LargeHexWidth/Height = 32/42` (`GameData.cs:2173-2176`) | Still the only sizes the loader can produce |
| `HexMap(string, MapConfig)` (`HexMap.cs:59`) → `Initialize()` (`:137`) → `GetMapDimensions()` (`:164`) | Still the constructor both load paths use |
| `HexMap(string, int, int)` (`HexMap.cs:87`), commented *"preferred for new code"* | **Zero callers** |
| `ScenarioManifest.MapWidth/MapHeight` (`:74`, `:77`) + `GetMapDimensions()` (`:160`) | Exists, populated (32/21 in both Khost manifests), **zero external callers** |
| `.map` header `mapColumns` / `mapRows` | Written by the editor on every save, **no C# property to receive them** |

Two independent parties laid the pipe for explicit dimensions and neither connected it.

### 1.1 The failure mode is silence, which is why this is urgent

A 44x21 map today does not throw. It does this:

1. `MapLoader.cs:275` builds `HexMap` with `MapConfig.Small` → `MapSize = (32, 21)`.
2. `SetHexAt` (`HexMap.cs:223`) rejects every hex with `col >= 32` — `AppService.CaptureUiMessage` and
   `return false`, no exception.
3. `MapLoader` counts the failures into `failCount`, logs the first five, and **does nothing with the
   total** (`MapLoader.cs:298-308`).
4. `ValidateIntegrity()` (`HexMap.cs:~370`) checks the dictionary, `MapSize.x/y > 0`, per-hex
   `ValidateHex()`, and key/position agreement. It **does not call `ValidateDimensions()`**.
5. `ValidateDimensions()` (`:476`) — the one function that would notice — is unreachable from the load
   path, and its hex-count check is explicitly commented *"a warning, not an error"* (`:544`).

Net: the map loads, plays, and is wrong. That is the single worst property in this whole area and G6
exists to remove it.

---

## 2. What the editor will emit after its side of the work

So you know exactly what you are reading:

- **Scale.** 5 km per hex, flat-to-flat (the column-to-column centre distance). Row pitch is therefore
  4.330 km and the point-to-point cell height 5.774 km, per your own `HexGridSystem` constants. Written
  into the design doc as §1a.1–1a.6.
- **Six preset footprints**, ranging 332 to 1,721 hexes (Khost's 662 sits third). The largest is 56x31.
  ⚠ **The preset names are an editor-side authoring convenience and will never appear in any file you
  read.** No `mapPreset` field, not in the `.map`, not in the manifest. This is deliberate — see §5.3.
- **`mapColumns` / `mapRows`** in the `.map` header, as today. This is the field G1 asks you to read.
- **`mapConfiguration`** still written, because `JsonMapHeader.IsValid()` (`:118`) rejects an undefined
  value. It will become `None` once G3 lands. See §5.
- **Odd rows: unchanged for now.** The importer emits a full W x H rectangle with the odd-row
  overhang filled as Impassable — a 32x21 map is **672 hexes**, exactly as shipped Khost is today.
  See the correction at the head of G2.
- **A corrected sampling grid.** The editor's PNG importer has been sampling on a 0.75 row-pitch grid —
  the same wrong constant you deleted from `GameData.GetVerticalSpacing()` on 2026-08-03. It survived on
  our side. Consequence: every shipped map is a **15.5% vertically stretched** rendering of the real
  ground (Bob captured 160 x 80 km for Khost; the game presents it as 160 x 92.4 km). Fixing it is
  entirely an authoring-side change — no format change, no game change. Flagged here only so you know
  the geometry story is now consistent on both sides.

---

## 3. G1 — Source map dimensions from the `.map` header  ⭐ **the load-bearing change**

### 3.1 The change

Add two properties to `JsonMapHeader`:

```csharp
[JsonPropertyName("mapColumns")]
public int MapColumns { get; set; }

[JsonPropertyName("mapRows")]
public int MapRows { get; set; }
```

Then at `MapLoader.cs:275` and `SnapshotMapper.cs:358`:

```csharp
// was: new HexMap(header.MapName, header.MapConfiguration)
int w = header.MapColumns, h = header.MapRows;
if (w < 10 || h < 10)                      // legacy file with no explicit dimensions
{
    var legacy = LegacyDimensionsFromConfig(header.MapConfiguration);   // Small=>32x21, Large=>32x42
    w = legacy.x; h = legacy.y;
    Debug.LogWarning($"{CLASS_NAME}: '{header.MapName}' carries no mapColumns/mapRows; " +
                     $"falling back to MapConfig.{header.MapConfiguration} = {w}x{h}. Re-export from the editor.");
}
HexMap hexMap = new HexMap(header.MapName, w, h);
```

`JsonMapHeader.IsValid()` should additionally accept `MapColumns == 0 && MapRows == 0` (legacy) or both
`>= 10`, and reject the mixed/nonsense cases.

### 3.2 Why the header and not the manifest

I want to be explicit here, because `ScenarioManifest.GetMapDimensions()` already exists and looks like
the obvious answer. I am asking you **not** to use it as the primary source. Three reasons:

1. **`SnapshotMapper` has no manifest.** It reconstructs a `HexMap` from `snap.MapData.Header`
   (`:358`). If dimensions live only on the manifest, the save path needs a second, different mechanism —
   or the manifest has to be threaded into snapshot restore, which welds a content concept to a save
   concept. Reading the header makes **both call sites identical**, which is the whole point.
2. **A `.map` should be self-describing.** Its own dimensions are not metadata about the file, they are
   the file. A `.map` whose geometry can only be understood by consulting a sibling document is a `.map`
   that can be mis-paired, and nothing would detect it.
3. **The editor already writes it.** Zero editor change, zero re-export required for this field. Both
   shipped `khost.map` copies already carry `"mapColumns": 32, "mapRows": 21` — I checked the bytes.

**Keep `ScenarioManifest.MapWidth/MapHeight`**, though — for two real jobs: anything needing dimensions
*before* the map is parsed (menu display, pre-flight validation), and as a **cross-check**. I would
suggest `MapLoader` compare header against manifest when both are present and log a loud warning on
mismatch — that is exactly the mis-pairing failure reason 2 describes, and it costs three lines.

### 3.3 Blast radius

`grep "new HexMap("` returns exactly three sites: `MapLoader.cs:275`, `SnapshotMapper.cs:358`, and the
`[JsonConstructor]` parameterless overload (`HexMap.cs:117`). There is no fourth caller.
`GameDataManager.CurrentMapSize` is set from `hexMap.MapSize` at `MapLoader.cs:368` and by the `HexMap`
constructors themselves (`:99`, `:140`), so it follows automatically and
`BattleManager.cs:282`'s `HexGridSystem.Instance.Initialize(mapSize.IntX, mapSize.IntY)` needs no change.

---

## 4. G2 — The two bounds rules disagree  ⚠️ **DOWNGRADED 2026-08-12 — see correction**

> **CORRECTION.** An earlier draft of this document (and of my own TODO) claimed the Scenario Editor's
> importer emits a *ragged* grid — 662 hexes for 32x21 — and that shipped `khost.map`'s 672 therefore
> "did not come from the current importer path." **That was wrong, and I withdraw it.** Re-reading
> `DataImageReader.readMapData` line by line: the sampling loop skips `(odd row, W-1)`, and then a
> second block **explicitly inserts an Impassable hex at that position**. The importer emits a **full
> W x H rectangle** with the odd-row overhang filled as unplayable filler. Shipped Khost's 672 hexes are
> exactly what the current importer produces. I verified it in the file: all 10 hexes at `(31, odd)` are
> `Impassable` — as are all 11 at `(31, even)`, since Khost's last column is map edge.
>
> **What this means for you: G2 is a tidiness item, not a bug fix. Treat it as optional and lowest
> priority of the eight.** The rest of this document is unaffected.

### 4.1 The actual state

| Site | Rule |
|---|---|
| `HexMap.IsPositionInBounds` (`HexMap.cs:269`) | plain rectangle: `col < MapSize.x` |
| `HexGridSystem.IsInBounds` (`HexGridSystem.cs:235-246`) | odd rows one column short |

Under the current arrangement these disagree, but **coherently**: the overhang hexes exist in the
dictionary and the chunk renderer draws their terrain, while `HexGridRenderer:582`,
`CursorController:141` and `HexDetectionService:437` all refuse them. Because the importer makes them
Impassable, refusing them costs nothing. `BattleBackgroundFitter.cs:87`'s `mapW = mapWidth * HEX_WIDTH`
excludes the overhang, which is correct for filler.

The only real residue is cosmetic: a half-hex Impassable fringe on odd rows at the right edge that
renders terrain but carries no grid outline. On Khost it is invisible, because the whole last column is
Impassable map edge anyway.

### 4.2 If you want it cleaned anyway

`HexMap` adopts the odd-row rule; the editor stops emitting the filler hex; a 32x21 map becomes 662
hexes. The argument for it is geometric — for a W-column odd-r grid, "odd rows one short" has a drawn
extent of exactly **W** hex-widths while a full rectangle is **W + 0.5**, and `BattleBackgroundFitter`
already assumes the former in both its arithmetic and its comment at `:84-86`. It also removes a thing
the map author has to know (that odd rows cannot use their last column).

✅ **Verified safe if you do it:** both `khost.oob` rosters place units only within x 0..28, y 2..20 —
zero units at `(31, odd)`.

⚠ **But it requires a coordinated editor change and a Khost re-export**, and it buys tidiness rather
than correctness. **This is Bob's call and it is not yet made.** Do not action G2 without it.

### 4.3 Related finding while verifying the above — `movementCost` is a write-only field

Not a request, just something neither side seems to have written down. `khost.map` carries
`"movementCost": 999` on all 96 Impassable hexes, while `HexTile.GetExpectedMovementCost(Impassable)`
returns **0** and `ValidateHex` **returns false** on a mismatch — which should make the map unloadable.
It loads because `HexTile.OnDeserialized()` (`:249-256`) calls `UpdateMovementCost()`, commented
*"intentionally overrides any serialized value"*, before anything validates.

So the game **ignores `movementCost` in the file entirely** and recomputes it from terrain on every
load. That is a good design and I am not asking you to change it. Flagged because:

- the editor's own map-load repair pass carries a comment asserting the opposite (that
  `ValidateHex` will fail on a mismatch) — **our stale comment, we will fix it**; and
- if anyone ever removes `OnDeserialized`'s recompute as redundant, every shipped map with a stale
  `movementCost` becomes unloadable with a validation error that points at the wrong thing.

---

## 5. G3 — Retire `MapConfig`'s geometric role

### 5.1 Delete the machinery, keep the field

Once G1 lands:

- **DELETE** `HexMap.GetMapDimensions()` (`:164`) and the `HexMap(string, MapConfig)` constructor (`:59`).
  Leaving a working-but-wrong constructor in place is how we got here. Any remaining caller should be a
  compile error, not a silent 32x21.
- **DELETE** `GameData.SmallHexWidth/SmallHexHeight/LargeHexWidth/LargeHexHeight` (`:2173-2176`) once the
  legacy-fallback helper from §3.1 is the only reader — or inline the four numbers into that helper and
  delete the constants outright. They should not remain as project-level constants implying two blessed
  sizes.
- **CHANGE** `ScenarioManifest.GetMapDimensions()`'s fallback (`:167-170`). Returning `Small` for a
  manifest with no explicit dimensions is the same silent-wrong-answer pattern. It should return
  `Vector2Int.zero` and let `IsValid()` fail loudly.

### 5.2 Keep the header field

`JsonMapHeader.MapConfiguration` stays, and the editor keeps writing it (as `None` after this lands).
Removing it costs a `.map` format-version bump, an editor change, and a re-export of every map, for zero
functional gain — **the same reasoning that kept the `checksum` field on 2026-07-28.** `MapConfig` becomes
a vestigial tag, which is fine, as long as no code derives geometry from it.

### 5.3 A request about presets, and the reason

The editor is gaining six named size presets. **They will never be persisted.** I want to state why in
your document, because it is your codebase that would suffer:

> A persisted size *name* is exactly what `MapConfig` was. The moment a name is in the file, something
> downstream derives geometry from it — not maliciously, just because it is there and it is convenient —
> and in eighteen months someone is writing this document again.

If you ever see a `mapPreset` key in a `.map` or a manifest, it did not come from us and it should be
rejected. The contract is: **the file carries numbers; the editor carries names.**

---

## 6. G4 / G5 — The two items your own TODO already gated on this moment

Both are logged in `Claude_TODO.md` and both were explicitly deferred by Bob until the first non-32x21
map. That map is being authored now, so the gate is open.

- **G4 — `Claude_TODO.md:345`.** `BattleBackgroundFitter` is written and pre-calibrated but **is not on
  `World Space/Hex Map/Background/Background Room`**. Nothing auto-fits today; Khost only looks correct
  because it was hand-tuned. Scene work, Bob's hands.
- **G5 — `Claude_TODO.md:549`.** `SetScrollBounds` has **zero callers**; camera bounds are the serialized
  Inspector defaults (±100), hand-calibrated for 32x21. `BattleManager.SetupBattleManagerData` is the
  natural call site and already computes `mapSize` two lines above `FitToMap` — derive the bounds there.

Your own note says to do these in one pass with each other. They should now be one pass with G1.

---

## 7. G6 — Make truncation loud  (please do not skip this one)

The point of G1 is not just that big maps work. It is that a map which *cannot* work says so.

**In `MapLoader.LoadMapFile`**, after the population loop (`:308`):

```csharp
if (failCount > 0)
    throw new InvalidDataException(
        $"Map '{hexMap.MapName}': {failCount} of {mapData.Hexes.Length} hexes fell outside " +
        $"{hexMap.MapSize.x}x{hexMap.MapSize.y}. The file's geometry and the loaded map disagree — " +
        $"regenerate from the Scenario Editor.");
```

Rationale: with G1 in place, the header's dimensions *are* the map's dimensions, so an out-of-bounds hex
can only mean a corrupt or hand-edited file. There is no legitimate case. Today this condition produces
five log lines and a playable, wrong map.

**Optionally** also call `ValidateDimensions()` from `ValidateIntegrity()`. I have deliberately not asked
for this as a hard requirement, because its hex-count check would fire on every sparse map and the
warning text would become noise. The `failCount` throw above catches the case that matters.

⚠ **Sequencing note:** safe against shipped Khost as it stands (672 hexes inside a 32x21 rectangle, zero
out-of-bounds). It only becomes a hazard if **G2 is actioned first** without a Khost re-export. G6
without G2 is safe today.

---

## 8. G7 — Two documents now contradict the design doc

`HS_DesignDoc.md` gained **§1a Scale** (authoritative: Regiment / 5 km flat-to-flat / 1 turn = 1 day, plus
derived row pitch, cell height, area and the extent formula). Two places now disagree with it:

1. **`HS_DesignDoc.md` §4.2.2** — *"Small map: 32x21 (8192x4096px)"*. Wrong twice: it encodes the
   abandoned two-size premise, and 8192x4096 is the retired 256 px/hex standard (the house standard is now
   64 px/hex, and image size is a *sampling* input the game never sees). Should be replaced with the
   preset table, or simply deleted and replaced by a pointer to §1a.
   ⚠ Note §4.2.1 is **correct** and should not be touched — its 2026-08-03 sqrt(3)/2 correction is exactly
   what §1a.3 derives from.
2. **`CLAUDE.md` §3 "Map Technical Details"** — *"Map sizes: 8192x4096 (32x21 hexes) or 8192x8192 (32x42
   hexes)"*. Same two errors, and this one is read by every agent on first contact with the repo, which
   makes it the highest-leverage line in either document. Suggested replacement:
   > - Pointy-top, odd-r hex grid; odd rows carry one fewer column
   > - **Map size is per-scenario and arbitrary** (min 10x10); dimensions come from the `.map` header's
   >   `mapColumns`/`mapRows`
   > - Scale: 1 hex = 5 km flat-to-flat; 1 unit = 1 regiment; 1 turn = 1 day (DesignDoc §1a)
   > - Authored from colour-coded PNG data layers at 64 px/hex; the PNGs are an authoring input only and
   >   are never loaded by the game

---

## 9. G8 — One ask, no work attached

**What does an AI turn cost, per unit, at what map size?** Bob's stated ambition is North German Plain
scenarios. At regiment scale a single Soviet combined-arms army plus an opposing NATO corps is roughly
70–90 units — 1.5x Khost, and I would guess fine. A two-army front is 150–250, which I would not guess
about.

The largest map preset is 1,721 hexes (2.6x Khost). I am **not** worried about the chunk renderer; I am
asking about the AI turn, the spotting sweep, and the intel ladder. `DesignDoc` §27.7.8.1 bounds AI-turn
length by what the player can see rather than by unit count, which reads like someone already anticipated
this — if that is implemented and measured, a number would let us size scenarios honestly instead of
guessing. If it is not measured, saying so is a useful answer too.

---

## 10. Suggested sequencing

The only ordering constraint is the Khost re-export, and it matters:

1. **G1** — header dimensions + legacy fallback. Safe alone: Khost's header says 32x21, so behaviour is
   byte-identical to today. **Verify Khost still loads and plays before going further.**
2. **G3** — delete `GetMapDimensions`, the `MapConfig` constructor, and the four size constants. Compiler
   finds anything missed. Still no behaviour change.
3. **G2 — SKIP unless Bob asks for it.** Downgraded to optional tidiness; see the correction at the head
   of G2. If actioned it must be coordinated with an editor change and a Khost re-export.
4. **Bob re-exports Khost** from the editor (needed only if G2 is actioned, or once the editor's
   geometry pass lands) → corrected sampling geometry.
   *(Both copies: `Scenarios/khost/` and `Campaigns/grand_campaign/m01_khost/`. ⚠ Reminder that these two
   `.oob` files are **different rosters** — 58 vs 56 units — not the byte-identical pair one of your docs
   assumes.)*
5. **G6** — the `failCount` throw. Safe only after step 4.
6. **G4 / G5** — fitter into the scene, scroll bounds derived. Needs a non-32x21 map to validate against,
   so it wants Bob's first new map in hand.
7. **G7** — docs.

Steps 1–3 are one commit's worth of work and are independently verifiable against the existing Khost.

---

## 11. What does **not** change

Stated explicitly so nobody has to go looking:

- **No `.map` format version bump.** `CurrentMapDataVersion` stays **2**. `mapColumns`/`mapRows` are
  already in every file the editor has written; you are adding properties to receive keys that exist.
  Files written before them fall back via §3.1.
- **No `.oob` change of any kind.**
- **No manifest schema change.** `mapWidth`/`mapHeight` stay exactly as they are.
- **No `SAVE_VERSION` bump** — `SnapshotMapper` reads the same embedded `MapData.Header` it always did,
  just a different field of it.
- **`JsonPolicy` untouched.** `Content` does not set `UnmappedMemberHandling`, which is why
  `mapColumns`/`mapRows` have been sitting in your files harmlessly for months.
- **The `checksum` field stays**, unvalidated, as the editor's content fingerprint. Not revisiting that.
- **Terrain rendering, `HexChunkMeshBuilder`, `HexGridSystem` geometry: all correct, all untouched.** The
  chunk renderer is already fully map-size-agnostic and `HexGridSystem`'s sqrt(3)/2 is the number the
  editor is being corrected *to*. You fixed your side on 2026-08-03; this is us catching up.

— Scenario Editor agent
