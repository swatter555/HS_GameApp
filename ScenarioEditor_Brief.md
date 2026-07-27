# Brief for the Scenario Editor agent — content format changes

**From:** the Hammer & Sickle game-side agent · **Date:** 2026-07-27
**Audience:** whoever maintains the external scenario editor that authors `.map`, `.oob` and `.manifest` files.
**Assume no shared context.** Everything needed is below.

---

## 1. What changed on the game side, and why you care

The game used to store enums in JSON as **integers**. That is a latent, silent data-corruption bug, and the game
side has now been fixed — but the fix is only half-effective until the editor changes too, because the editor is
what actually writes these files.

**The failure it prevents:** `WeaponType`, `UnitClassification`, `Nationality`, `TerrainType` and friends are C#
enums. Serialized as ordinals, `"Classification": 12` means "whatever member happens to sit at index 12 today".
Insert a new member in the middle of one of those enums — an utterly routine thing to do when adding a unit or a
terrain type — and every previously-authored file silently re-interprets. The integers still parse, so there is
no exception and no warning; a Mujahideen infantry regiment simply becomes something else. For a game that will
add units in post-release patches, this is a question of when, not if.

**The fix:** enums persist **by name**, not by number. Names survive insertion and reordering. Only a *rename*
breaks them, and a rename is visible, deliberate, and can ship with a migration.

The game's readers now accept **both** forms, so nothing breaks the moment you change — old files keep loading.
But content only gains the protection once it is **written** with names.

---

## 2. Change #1 — write enum values as names

If the editor is C# using `System.Text.Json`, this is one line on the serializer options:

```csharp
options.Converters.Add(new JsonStringEnumConverter());
```

If it is not C#, the requirement is: **emit each enum-valued field as a JSON string containing the exact C#
member name**, not its integer.

### Fields affected

**`.map` → `header`:**

| Field | Enum |
|---|---|
| `mapConfiguration` | `MapConfiguration` |

**`.map` → each entry in `hexes`:**

| Field | Enum |
|---|---|
| `terrain` | `TerrainType` |
| `tileControl` | `TileControl` |
| `defaultTileControl` | `DefaultTileControl` |
| `hexControlLevel` | `HexControlLevel` |
| `labelSize` | `TextSize` |
| `labelWeight` | `FontWeight` |
| `labelColor` | `TextColor` |

Bridge/border sub-objects carry `BridgeType` where present.

**`.oob` → each entry in `units`:**

| Field | Enum |
|---|---|
| `Side` | `Side` |
| `Nationality` | `Nationality` |
| `Classification` | `UnitClassification` |
| `Role` | `UnitRole` |
| `IntelProfileType` | `ProfileType` |
| `Experience` | `ExperienceLevel` |
| `Efficiency` | `EfficiencyLevel` |
| `Deployment` | `DeploymentPosition` |
| `Spotted` | `SpottedLevel` |
| `DepotCategory` | `DepotCategory` |
| `DepotSize` | `DepotSize` |

Leader entries carry `CommandGrade`, `CommandAbility`, `Nationality`, `Side` and skill-branch/tier enums —
same rule.

**`.manifest`:** `mapTheme` and `difficultyLevel`.

### Before / after

```jsonc
// before
{ "terrain": 3, "tileControl": 1, "labelColor": 4 }
// after
{ "terrain": "Rough", "tileControl": "Red", "labelColor": "Green" }
```

```jsonc
// before
{ "Side": 1, "Nationality": 8, "Classification": 12, "Experience": 3 }
// after
{ "Side": "AI", "Nationality": "MJ", "Classification": "INF", "Experience": "Experienced" }
```

**Precedent already in your files:** `"DeployedProfileID": "INF_REG_MJ"` is already written by name. This change
makes the rest consistent with that.

---

## 3. ⚠ DO NOT CHANGE THE CHECKSUM COMPUTATION

**This is the one way this task can break every existing map, so read it before touching anything.**

`.map` headers carry a SHA-256 `checksum` that the game validates on load. It is computed over the **hex array
re-serialized in memory**, *not* over the file text. On the game side (`MapChecksumUtility.CalculateChecksum`)
it uses its own deliberately frozen options:

- `WriteIndented = false`
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- **no enum converter — enums are serialized as INTEGERS here**
- `SHA256` over `SerializeToUtf8Bytes(hexes, options)`, rendered lowercase hex

So: **the file representation and the hash representation are deliberately decoupled.** Switching the *file* to
string enums must not change the *hash* input. If the editor mirrors this routine, do **not** add the enum
converter to it. If you add `JsonStringEnumConverter` globally to a shared options object that the checksum path
also uses, every `.map` in the project will fail validation on load with no obvious cause.

Whatever the editor does today to produce a checksum the game accepts — leave that code path exactly as it is.

**Verification:** after the change, the checksum value written into a re-exported `.map` should be **byte-identical**
to the one it had before, for unmodified map data. If it differs, the hash path was contaminated.

---

## 4. Change #2 — export folder layout

Content now ships inside the game build under `StreamingAssets`, and **a scenario is a self-contained folder**
holding its own manifest, map, oob, aii and brf. The old split (a shared `manifests/`, `map/`, `oob/`, `brf/`
set of directories) is retired — it made two scenarios unable to share a filename, and forced the game to guess
which directory a given scenario's files lived in.

```
<game>/Assets/StreamingAssets/
  Scenarios/
    khost/
      mission_khost.manifest
      khost.map
      khost.oob
      mission_khost.brf
  Campaigns/
    grand_campaign/
      campaign.manifest              <- campaign-level, not yet defined; ignore for now
      m01_khost/
        campaign_khost.manifest
        khost.map
        khost.oob
        campaign_khost.brf
```

The manifest's `mapFilename` / `oobFilename` / `aiiFilename` / `briefingFilename` are resolved **relative to the
manifest's own folder**. They stay bare filenames — do not write paths into them.

**Naming rules:**
- Folder names are lowercase with underscores (`grand_campaign`, `m01_khost`). They become machine-facing IDs.
- **A scenario's folder name is permanent once shipped** — saves reference the scenario by id, so renaming one
  breaks saves. The `m01_`/`m02_` prefix records authoring order and is *not* the campaign's play order; play
  order lives in `campaign.manifest`. Never renumber a folder after release.
- Display names (`"Operation Molot"`) live inside the manifest and can change freely, including in a patch.
- Filenames are case-sensitive on some targets — keep them lowercase. (A `mapFilename` of `khost.map` pointing
  at a file named `Khost.map` worked only because Windows is case-insensitive.)

---

## 5. Where the authoritative enum names live

All of these are declared in the game repo at:

```
Assets/Scripts/Core/Game Data/GameData.cs
```

Member names must match **exactly**. When the game adds an enum member, the editor's copy needs updating —
which is a much safer failure than today's, because an unknown *name* fails loudly at parse time, whereas an
out-of-range *integer* is often silently accepted.

---

## 6. Optional cleanup

`.oob` unit entries currently carry both `"Classification": 12` and `"classificationName": "INF"` — the second
looks like a human-readable mirror added because the integer was unreadable. Once `Classification` is written by
name, `classificationName` is redundant. The game does not require it either way; drop it if it is not doing
something else for you.

Note also that the `.map` header's `saveVersion` is the **map format** version (currently `2`), independent of
the game's save-file version. It is yours to bump if the map format itself changes.

---

## 7. How to verify you are done

1. Re-export Khost with the changes.
2. The `.map` header `checksum` is **unchanged** from the previous export of the same data. *(If not: the hash
   path was contaminated — see §3.)*
3. `.map` and `.oob` show quoted names where integers used to be.
4. The game loads the scenario and plays it. Both readers accept old and new form, so a partial migration is
   safe — mixed-format content will load.
