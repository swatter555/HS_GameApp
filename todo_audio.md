# todo_audio.md — AUDIO REBUILD

> **Why a separate file:** `todo.md` holds the CONTENT PIPELINE pass, whose Phase 2 is paused but live.

**Goal (Bob, 2026-08-03):** a tight, low-latency audio system that is easy to author against. Agent has
the call on design decisions. **No band-aids.**

**Enabling condition:** the game has 6 SFX, 1 music, 1 ambient, 1 briefing. Format, storage and API are
all still free to change. This will never be cheaper.

---

## 1. WHY THE OLD SYSTEM IS BEING REPLACED RATHER THAN TUNED

Measured, not assumed. A button click cost **~137–160 ms** end to end:

| Source | Cost | Status |
|---|---|---|
| Leading silence baked into the WAV | **~114 ms** | Phase 0 |
| DSP buffer at 1024 ("Best performance") | ~23–46 ms | ✅ FIXED by Bob 2026-08-03 |
| Code path (warm) | ~0 ms | already fixed |

⚠ **The root architectural error: SFX were loaded on demand at runtime** via `UnityWebRequest` out of
`StreamingAssets`. Every piece of machinery in the SFX path — the cache, the negative cache, the in-flight
guard, the preload step, the UI-vs-gameplay API split, the drop-if-not-resident rule — exists ONLY to
manage the consequences of clips not being in memory. Remove the runtime load and all of it deletes.
That is the whole rebuild.

⚠ Two fixes landed earlier the same day (negative caching; in-flight guard + synchronous fast path). Both
were real defects, both correctly fixed, and **neither could have removed the symptom** — the time was in
the assets and the DSP buffer. Their machinery is now deleted by this pass. Recorded so it reads as a
model change, not a silent revert.

---

## 2. DECISIONS (agent's call, per Bob)

| # | Decision | Rationale |
|---|---|---|
| D1 | **SFX become project assets** under `Assets/Audio/SFX/`, out of StreamingAssets | Kills runtime loading outright. Enables import settings, which StreamingAssets files do not get. |
| D2 | **Music / ambient / briefings stay in StreamingAssets, untouched** | Large, streamed, no latency requirement — and briefings are genuinely per-scenario content (§7.1, §20.4.2). Split by ROLE, not folder. |
| D3 | **`SoundEffect` enum stays as the identity**, mapped by a ScriptableObject catalog | Code stays compile-safe and greppable; zero Inspector re-wiring. Per-sound cue assets would kill the ordinal hazard but cost a re-wire and stop code naming a sound. |
| D4 | **`PlayUISFX` deleted** — but the facade still has TWO methods, for a DIFFERENT reason | ⚠ AMENDED 2026-08-03 after implementation. The original wording said "one play method", and that was right about the OLD split: the UI-vs-gameplay divide existed only because loading could stall, and died with the load path. A new split then earned its place — `Play` (ungated) vs `PlayFrom(id, source)` (fog-gated) — because a single method with an optional source would DEFAULT TO UNGATED, so forgetting the argument leaks an unspotted enemy silently. The count is the same; the reason is not. |
| D5 | **Debounce becomes per-sound DATA, default 0 (off)** | A global debounce suppresses legitimate audio (double-clicks, several units firing). It was a band-aid for stacking whose real cause was duplicate loads + the 114 ms head. Sounds that need it opt in. |
| D6 | **Hover audio DELETED** (not just defaulted off) | Fires on pointer motion, not intent; `UIButtonHoverScale` already gives hover feedback visually. A disabled-but-present option is the exact "looks wired, does nothing" trap this project keeps paying for. |
| D7 | **`PlayOneShot`, not `clip=`/`Play()`** | One-shots mix instead of replacing, so the current `Stop()` steal that truncates a sound mid-playback disappears. |
| D8 | **Catalog loaded via `Resources.Load`** | `GameAudioManager` self-creates via `new GameObject()`, so a `[SerializeField]` reference would be null on that path. Mirrors `HexChunkRenderer`'s terrain-array load. Only the catalog sits in Resources; clips are ordinary assets it references, so the build pulls them in automatically. |
| D9 | **Force-to-mono + PCM + preload on SFX import** | 2D blips gain nothing from stereo. Preload means no code-side warm-up at all. |
| D10 | **No AudioMixer, no positional audio** | Neither buys latency. Mixer costs Bob a setup pass; positional is a real feature to be scoped on its own. Seam noted, not built. |

---

## 3. DESIGN

Three small pieces replace ~250 lines of loading machinery.

### 3.1 `AudioCatalog` — ScriptableObject, the authoring surface
One asset at `Assets/Resources/Audio/AudioCatalog.asset`. Serialized rows:

```
SoundEffect id | AudioClip[] clips (variants, R4) | float volume | float pitchVariation | float minRetriggerSeconds
```

Flattened to a `Dictionary<SoundEffect, Entry>` on load. Adding a sound = drop the wav in, add a row,
assign the clip. No filename strings, no parallel dictionary, no code change to TUNE a sound.

### 3.2 `SfxPlayer` — plain C# class, not a MonoBehaviour
Owned by `GameAudioManager`, given the AudioSources it creates. No new scene object, no growth of an
already-1700-line file, and unit-testable.

⚠ **Two source groups, deliberately.** Pitch is a per-SOURCE property, so changing it for a new one-shot
also warps any one-shot still ringing on that source. Sounds with `pitchVariation == 0` (all UI) therefore
play on a dedicated FLAT source whose pitch is never touched; varied sounds round-robin a small pool.
A UI click can never be detuned by a gameplay sound landing on top of it.

### 3.3 `GameAudio` — static facade
```csharp
GameAudio.Play(SoundEffect.ButtonClick);        // nothing caused it — ungated
GameAudio.PlayFrom(SoundEffect.X, sourceUnit);  // a unit caused it — FOG GATED
GameAudio.PlayWeaponFire(firer);                // family resolved via WeaponSoundClassifier, gated
```
⚠ **Never lazy-creates; no-ops when there is no instance.** This permanently kills the trap that
`.Instance` builds a GameObject — the reason `EventManager` and `GameDataManager` cannot be touched from
`Models/`. Audio becomes safe to call from anywhere, including headless EditorTests.

### 3.4 Deleted
`LoadSFX` · `SoundEffectFiles` · `_sfxCache` · `_sfxLoading` · SFX negative caching · `PreloadSFX` ·
`UiSoundEffects` · `PlaySFXCoroutine` · `TryPlayCached` · `EnsureSfxLoaded` · `PlayUISFX` ·
`PlaySFXWithVariation` · `SFX_UI_RETRIGGER_SECONDS` · `_lastSfxPlayTime` · the whole `UIButtonAudio` hover
path (`enableHoverSound`, `hoverSound`, `hoverVolumeScale`, `PlayHoverSound`, `TriggerHoverSound`,
`SetHoverSound`, `OnPointerEnter`, `OnPointerExit`, `_isHovering`, `OnDisable`) · `SFX_ButtonHover.wav`.

Music / ambient / briefing equivalents all STAY.

---

## 4. PHASES

### Phase 0 — Latency ✅ COMPLETE 2026-08-03 (pending Bob's ear)
- [x] 0.1 DSP Buffer Size → Best latency. ✅ Bob, 2026-08-03.
- [x] 0.2 Leading silence trimmed from 5 SFX. ✅ Agent, 2026-08-03.

      ⚠ **THE FIRST MEASUREMENT WAS WRONG AND THE SECOND ONE MATTERED.** A −46 dBFS threshold put
      ButtonClick's onset at 114.5 ms; at −60 dBFS it was 56 ms. Neither is the answer — the ENVELOPE is:
      flat at −70…−55 dBFS for 110 ms, then vertical to −1 dBFS in the last 15 ms. So the region is a
      NOISE FLOOR, not a designed swell, and the real transient begins ~114 ms. Had it been a slow attack,
      trimming would have destroyed the sound's character. Profile before cutting.

      Method: cut at (first sample above −50 dBFS) − 5 ms pre-roll, so no attack edge is clipped; a 1 ms
      fade-in was available if the first retained sample was not near zero, and was not needed on any file
      (largest retained sample 34/32768). Verified after: sample rate / channels / bit depth unchanged on
      every file, onset now 5.0 ms (the deliberate pre-roll), and **the retained audio is byte-identical
      to the original tail** — provably only leading samples were removed.

      | File | Trimmed | Before → After |
      |---|---|---|
      | SFX_ButtonClick | **109.3 ms** | 298.5 → 189.2 ms |
      | SFX_MediumSnareDrum | 55.3 ms | 4049.3 → 3994.0 ms |
      | SFX_RadioButtonClick | 31.2 ms | 154.7 → 123.6 ms |
      | SFX_MenuOpen | 17.4 ms | 197.8 → 180.4 ms |
      | SFX_MenuClose | 13.4 ms | 185.3 → 171.9 ms |
      | SFX_PrinterTick | — | no lead-in, untouched |
      | SFX_ButtonHover | — | SKIPPED, D6 deletes it — no point spending LFS quota on a doomed file |

      Originals backed up to the session scratchpad; all 7 were committed and clean beforehand, so
      `git checkout -- Assets/StreamingAssets/Audio/SFX` is a one-command rollback.

### Phase 1 — Assets ✅ COMPLETE 2026-08-03 (pending compile + play check)
- [x] 1.1 Six wavs `git mv`'d to `Assets/Audio/SFX/` — git recorded RENAMES, so history and LFS pointers
      survive. ⚠ The old `.meta` files were DELETED rather than moved: a StreamingAssets meta is a
      `DefaultImporter`, and carrying it to a path where Unity must use `AudioImporter` would have brought
      the wrong importer type. Nothing referenced these by GUID (StreamingAssets is path-based) so fresh
      GUIDs are harmless, and the catalog that WILL reference them by GUID does not exist yet. Also
      removed the orphaned `StreamingAssets/Audio/SFX.meta` left by the emptied folder.
- [x] 1.2 **NEW `Assets/Editor/Audio/SfxImportSettings.cs` — an `AssetPostprocessor`, NOT a Preset.**
      Stamps mono · PCM · Decompress On Load · Preload Audio Data ON · Load In Background OFF on anything
      under `Assets/Audio/SFX/`. ⚠ A Preset must be REMEMBERED and fails SILENTLY when it is not — the
      sound still plays, just late and at double memory. With ~80 SFX still to author that is a defect
      waiting to happen 80 times. Includes `Tools/Audio/Reimport SFX With Ratified Settings` for the
      one-time pass over clips already in the project.
- [x] 1.3 Claude_Project §7.1a written — the role-not-folder split, what the move bought, and an explicit
      do-not-fix-it-back, since "all shipped content lives in StreamingAssets" was true until today.
- [x] 1.4 **D6 — hover audio DELETED**, not merely disabled: handlers, fields, `PlayHoverSound`,
      `TriggerHoverSound`, `SetHoverSound`, the `_isHovering` latch, `OnDisable`, three now-unused
      `IPointer*Handler` interfaces, `SFX_ButtonHover.wav`, its filename mapping, its preload entry, and
      the `Scene0_Controller` preload call site (found by grep — it would have been a compile error).
      ⚠ **THE ENUM MEMBER `SoundEffect.ButtonHover` STAYS, and that is the whole point.** The enum is
      append-only because Unity serializes enum fields as INTEGERS: deleting value 2 would shift
      MenuOpen 3→2, MenuClose 4→3, and silently repoint every Inspector-assigned button sound in the
      scene. Retired in place with a comment; verified `ButtonClick` is still 1, matching the scene YAML.

### Phase 2 — System ✅ COMPLETE — in game 2026-08-03, suite GREEN 2026-08-04
- [x] 2.1 `AudioCatalog` ScriptableObject with per-row `AudioClip[]` variants (R4), volume, pitch spread
      and retrigger. ⚠ Loaded via `Resources` because the manager self-creates via `new GameObject()`, so a
      `[SerializeField]` would be null on that path. Only the CATALOG is in Resources; clips are ordinary
      assets it references, so the build pulls them in without a second Resources folder.
- [x] 2.2 `SfxPlayer` — `PlayOneShot` (mixes, so the old `Stop()` steal is gone) + a FLAT source for
      pitch-variation-0 sounds and a round-robin pool for varied ones.
- [x] 2.3 `GameAudio` facade — `Play` / `PlayFrom` / `PlayWeaponFire`, never lazy-creating via NEW
      `GameAudioManager.Existing`.
- [x] 2.4 Old SFX path deleted; 4 call sites repointed.
- [x] 2.5 `Tools/Audio/Create Or Update Audio Catalog` + `Tools/Audio/Audit Catalog`.
- [x] 2.7 **`Tools/Audio/Audio Catalog Editor`** — drag-and-drop authoring window (Bob's ask, 2026-08-04).
      `Assets/Editor/Audio/AudioCatalogWindow.cs`. Three properties that are the whole point:
      **(a) driven by the ENUM, not the rows** — every `SoundEffect` member gets a line whether or not it
      has a row, so "which of the ~85 planned sounds still have no audio?" is visible rather than an audit
      you have to run. Dropping a clip CREATES the row. The default Inspector cannot answer that question
      at all, because it can only show rows that already exist.
      **(b) stray files are MOVED, not just referenced.** ⚠ Import settings are PATH-GATED — the
      `SfxImportSettings` postprocessor stamps mono/PCM/preload on `Assets/Audio/SFX/` and nothing else, so
      a clip assigned from anywhere else would play correctly but LATE and at DOUBLE MEMORY with no error
      anywhere. A drag-and-drop tool is a brand new way to reintroduce exactly the silent failure the
      postprocessor was chosen over a Preset to avoid. Drops from outside are moved + renamed to
      `SFX_<SoundEffectName>[_n]`, then **force-reimported** (a move alone does not necessarily re-run the
      importer, which would leave the file in the right folder with the wrong settings — the worst of both).
      Renaming to convention also keeps this window and `Create Or Update` from drifting apart on names.
      **(c) every mutation is DEFERRED to the end of the frame**, and file work finishes BEFORE the catalog
      is touched: adding a row mid-layout throws "Mismatched LayoutGroup", and an import fires
      `OnProjectChange`, which would rebuild the `SerializedObject` underneath a half-finished write.
      Also: variants add/remove inline (⚠ two-step delete — `DeleteArrayElementAtIndex` NULLS a populated
      object reference instead of removing it), tuning editable per row, search + "unbacked only" filter,
      duplicate/empty-row warnings in place, and `Scan Folder`/`Audit` call the existing tools rather than
      re-implementing them. External (Explorer) drops are copied in — best-effort, Project-window drag is
      the primary path.

      ⚠ **SLUGGISH ON FIRST RUN (Bob, same day) — CAUSE WAS `SerializedProperty` TRAVERSAL IN THE DRAW
      LOOP, and the fix is a row cache.** `FindPropertyRelative`/`GetArrayElementAtIndex` are
      managed→native calls that allocate on every use, and the first cut called them in nested loops: a
      full scan of the entries array per sound for the header count, then four more per row (`FindRow`,
      `CountRows`, `StatusText`'s clip walk, and `StatusStyle` → `HasUsableClip` → `FindRow` AGAIN).
      That is **O(sounds × entries) every GUI pass, twice a frame** — ~1,300 property operations at 24
      sounds, and ~30,000 at the inventory's 85. The array is now walked ONCE per pass into a `RowInfo[]`
      indexed by enum value (entry index · duplicates · clip slots · usable clips) and everything drawn
      reads that; `FindRow` survives for the mutation paths only, which run once per action rather than
      per frame. Also hoisted out of the per-pass path: `Enum.GetValues` + LINQ + enum `ToString()` (now
      static arrays built once), the warning `GUIStyle`, every `GUIContent`, and the `GUILayout.Width`
      option arrays. ⚠ Next lever if it is still heavy at 85 rows: cull rows outside the scroll viewport —
      nothing is virtualised, so every row's ObjectFields draw whether visible or not.
- [x] 2.6 EditorTests — **GREEN 2026-08-04 (Bob, 18/18, suite clean).**
      `Assets/Tests/EditorTests/AudioSystemTests.cs`, 18 tests in four regions.

      **Catalog lookup (7):** tuning survives the lookup (the "tune a sound with no code change" claim) ·
      unmapped id returns false with a null out-param · EVERY `SoundEffect` member is silent against an
      empty catalog (§27.7.5.2 — the enum runs ahead of the audio, and with 24 members against 6 clips
      that is the state the project is actually in) · a row with no usable clip fails the lookup in all
      three shapes the audit tool warns about (empty array / null array / all slots unassigned) · the
      FIRST duplicate wins · a null row is skipped rather than taking the whole catalog down with it ·
      `Invalidate` makes a later edit visible.
      **Variants, R4 (3):** null when there is nothing to play · an unassigned slot mid-array is skipped
      rather than returning silence · both variants are reachable (SEEDED, so deterministic rather than
      merely overwhelmingly likely — a `PickClip` that always returned `clips[0]` would defeat R4 with no
      visible symptom).
      **Retrigger window, D5 (6):** default 0 = OFF · suppressed inside the window, allowed FROM the
      boundary · PER SOUND, not global (the defect the old `SFX_UI_RETRIGGER_SECONDS` constant had) ·
      an unplayed sound is always allowed · `Reset` clears history · `Play` tolerates a null entry and a
      clipless entry.
      **Facade (1):** `GameAudio` never lazy-creates a manager — asserted by `Existing` still being null
      after `Play`/`PlayFrom`/`PlayWeaponFire`, with a friendly source so the calls get PAST the fog gate
      and actually reach the manager lookup.

      ⚠ **NO AudioSources ARE CREATED.** `new SfxPlayer(null, null)` is legal, and the flat path books the
      retrigger timestamp whether or not a source exists — which is what makes the pure `ShouldPlay` split
      pay off. Boundary times are powers of two (`10.25f - 10f` is exactly `0.25f`) so the inclusive
      comparison cannot fail on float fuzz.

      ⚠ **ONE reflection point:** `AudioCatalog.entries` is a private `[SerializeField]`, so a test has no
      other way to author a catalog in memory. `AudioCatalogTools.CreateOrUpdate` reaches the same field by
      the same string through `SerializedObject`, so a rename breaks both together — and `SetUp` asserts on
      the `FieldInfo` so that break reads as "the field was renamed" rather than a `NullReferenceException`
      twenty lines later.

✅ **Catalog created and confirmed in game 2026-08-03 (Bob)** — 6 rows, sounds play, "fast and
responsive". Re-run `Tools/Audio/Create Or Update Audio Catalog` after adding wavs; it never overwrites
tuning you have set, and `Tools/Audio/Audit Catalog` reports what is still unbacked.

### Phase 3 — Wire the game's sounds — CODE COMPLETE 2026-08-04, awaiting a play test
- [~] 3.1 `GetMovementSFX` finally gets a caller in `MovementController.ExecuteMovement`. ONE fire-and-
      forget shot for the whole move (§27.7.7), fired after `BeginMoveOrder` succeeds. New overload
      `GetMovementSFX(classification, predictedSeconds)` picks the LONG cut past
      `MOVEMENT_STANDARD_CLIP_SECONDS` (1.0 s); Foot has no long cut because its longest move is 0.7 s.
      ⚠ The per-hex tween length now has ONE spelling (`stepSeconds`) — it was re-declared inside the
      loop, and two copies would let the audio pick a clip for a duration the animation no longer runs at.
- [~] 3.2 Combat / ambush / spotting / objective / deploy hooks, placed at the §24.8.6 printer-emitter
      sites since the same events are worth hearing. Weapon fire via `PlayWeaponFire` (family-resolved),
      impact via the new `GameAudio.PlayImpact`, kills, first contact, ZoC halt, objective flips,
      select/deselect, facing.
- [~] 3.3 Denial SFX — `ButtonDenied` on all five refusal paths: two in `HandleCtrlClick`, the rejected
      attack in `TryAttack`, a blocked deployment change, and a spent intel action.
- [ ] 3.4 `UIButtonAudio` onto the battle HUD buttons. *(Bob — Inspector)*

**⚠ THE THREE ATTRIBUTION RULINGS THIS PASS HAD TO MAKE (§27.7.4.2), all silent if wrong:**
1. **Ambush is attributed to the VICTIM, not the ambusher** — the sharpest case the rule exists for. The
   ambusher is BY DEFINITION unspotted (§6.9.0), so attributing the sound to it gates the player's own
   regiment being hit into silence; playing it ungated announces a hidden unit. You always hear your own
   men take fire and learn nothing about who fired.
2. **Fire is the firer's, impact is the target's** — an unseen battery shelling the player produces NO gun
   report and a FULL impact. This is why no "generic substitute sound" concept is needed anywhere.
3. **`PlayWeaponFire` runs AFTER the orchestrator returns**, so the firing-reveal spotting change
   (§7.13.5.4 / §12.4.9) has already landed and the gate reads the POST-reveal level. Invisible today —
   the firer is always the player's own unit — and a real defect the moment the AI turn uses this path.

Ungated `Play` is used ONLY where nothing about a unit is revealed: refusals, the ZoC halt, objective
flips, select/deselect. Everything caused by a unit goes through `PlayFrom`/`PlayWeaponFire`/`PlayImpact`.

**Deliberately NOT sounded (R7):** `MoveOrderConfirm` (the movement one-shot IS the confirmation — both
would fire on the same click), `MoveOrderCancel` (no undo path exists yet), next/prev unit (`UIButtonAudio`
covers those in 3.4), unit-hardened (the promotion already files a dispatch, and the printer ticks).

**24 enum members appended** (append-only, at the end): `ButtonDenied` · 4 `UnitMove*Long` · 13 `Fire*`,
one per `WeaponSoundFamily` · 3 `Impact*` · `UnitDestroyed` · `ObjectiveCaptured` · `ObjectiveLost`.
`GameAudio.SoundEffectFor` is filled in — every arm was returning `None`. ⚠ `ImpactSoundFor` picks
armour/soft through `RegimentProfile.ClassifyWeaponType`, the SAME classifier behind the intel and loss
reports, so audio can never call something a tank that the loss report calls an AFV. Structures branch
first on `IsBase` — a facility is neither armour nor soft.

---

## 4b. SOUND INVENTORY — full scope (drafted 2026-08-03)

Grounded in systems that exist or are specced, not invented: the §24.8.6 dispatch catalogue (which already
enumerates every event the game considers notable), §7.9 combat outcomes, §11 air operations, §5 movement,
§8 action economy, §9 deployment. **Status:** ✅ file exists · ▢ enum declared, no file · ✗ not declared.

### A. UI / shell — 12
✅ButtonClick · ✅MenuOpen · ✅MenuClose · ✅RadioButtonClick · ✅PrinterTick · ✅BattleStartSting (snare)
✗ButtonDenied (§24.8.5 — illegal Ctrl+click, refused order; currently a silent no-op) · ✗DialogOpen ·
✗DialogClose · ✗ListSelect (UIListBox row) · ✗DispatchArrived · ✗TurnBegin/TurnEnd

### B. Selection & orders — 11
▢UnitSelect · ▢UnitDeselect · ▢NextUnit · ▢PrevUnit · ▢FacingChange · ▢MoveOrderConfirm ·
▢MoveOrderCancel · ▢UnitMoveBlocked · ▢OutOfMP · ▢UnitSpotted ✗HexSelect · ✗MoveUndo (§5.11)

### C. Movement — 7 ⚠ THESE ARE LOOPS, NOT ONE-SHOTS
▢UnitMoveTracked · ▢UnitMoveWheeled · ▢UnitMoveFoot · ▢UnitMoveHelo · ▢UnitMoveJet ✗RailMove (§5.3) ·
✗NavalMove (§5.4.2). Mapping already exists: `GetMovementSFX(UnitClassification)`.

### D. Ground combat — 20
Fire, by family: ✗TankGun · ✗Autocannon · ✗SmallArms · ✗ATGM · ✗ATGun · ✗ArtilleryOutgoing ·
✗RocketSalvo · ✗HeavyMG/AAA-vs-ground
Impact: ✗HE-on-soft · ✗AP-on-armour · ✗OnStructure
Events: ▢AmbushTriggered · ▢AmbushDetected · ✗CounterBattery · ✗UnitHardened (§13 XP tier-up)
Outcomes (§7.9.5/.6/.6a): ✗Retreat · ✗Rout · ✗Shatter · ✗Surrender · ✗Destroyed

### E. Air operations — 16 ⚠ ALL BLOCKED ON M13 / AOB
✗SortieLaunch · ✗DogfightBurst · ✗AAMLaunch · ✗SAMLaunch · ✗AAAFire · ✗BombRelease · ✗BombImpact ·
✗AircraftHit · ✗AircraftDowned · ✗Paradrop (AAB §11.12) · ✗AirSupplyDrop (SB §11.9) · ✗ReconRun (RB §11.11) ·
✗AWACS (AEWB §11.13) · ✗AOBPlaced · ✗AOBResolve · ✗ReactionWindowOpens (§11.1.8 / §24.13 — a UI cue that
control has passed to you; arguably the single most important sound in the air layer)

### F. Deployment & unit actions — 6
✗DeployUp · ✗DeployDown · ✗EntrenchComplete · ✗IntelAction · ✗Embark · ✗Disembark

### G. Logistics — 4 ⚠ blocked on §15.4a supply pass
✗ResupplyReceived · ✗OutOfSupply · ✗SupplyFailed · ✗ReplacementsDelivered

### H. Personnel / leader — 4 ⚠ blocked on leader L1/L2
✗Decoration · ✗Promotion · ✗LeaderKilled · ✗LeaderAssigned

### I. Objectives & battle end — 5 ⚠ victory/defeat blocked on §17 result evaluation
✗ObjectiveCaptured · ✗ObjectiveLost · ✗VictorySting · ✗DefeatSting · ✗PrestigeAwarded
(TerritoryFlip deliberately omitted — fires on almost every move, would be noise.)

### J. Streamed beds — 7 + N briefings (NOT SFX; stay in StreamingAssets)
✅MainMenuMusic · ✅BattleAmbient ✗VictoryMusic · ✗DefeatMusic · ✗CFR/CampaignMusic · ✗per-theme ambient
(desert wind) · ✗BriefingNarration ×N campaign missions (§20.4.2 — campaign scenarios only)

### Totals
**~85 SFX + 7 beds + N briefings.** Today: **6 files, 24 enum members.** So ~61 undeclared, ~79 without
audio. **Wireable now: ~45** (A–D, F, I-partial); ~40 are host-blocked on M13/AOB, supply, leader, §17.

⚠ **MEMORY IS A NON-ISSUE AND THAT SETTLES D1/D9.** 85 clips × ~0.4 s × 44.1 kHz × 16-bit mono ≈ **3 MB**
resident. Preload-everything stays correct well past the full inventory, so nothing here argues for
keeping a runtime load path.

---

## 4c. SCOPE RISKS — the things that get expensive if decided late

### ✅ R1 and R2 CLOSED 2026-08-03 — ratified in HS_DesignDoc §27.7.4 / §27.7.5, built, tested
`Assets/Scripts/Audio/AudioFogPolicy.cs` + `WeaponSoundFamily.cs` (namespace `HammerAndSickle.Audio`) —
pure, headless-safe, no Unity types, covered by `AudioPolicyTests`. New DesignDoc §12.10 names the three
intel channels so a future fourth surface has to declare its rung. ⚠ **POLICY ONLY — nothing enforces it
yet**, because the thing that will call it (the Phase 2 facade) does not exist. Phase 2 must therefore
ship the API shape that makes the gate unforgettable: an UNGATED `Play` for UI/turn/weather sounds, and a
SOURCE-TAKING overload for anything a unit causes. If Phase 2 ships one undifferentiated `Play`, R1 is
decorative. The original risk text is kept below as the reasoning record.

### R1 ⚠ AUDIO IS AN INTEL CHANNEL AND MUST BE FOG-GATED. Biggest correctness risk.
The icon is gated (§24.3.2) and dispatches are gated (§24.8.3); **sound is the third channel and nothing
gates it yet.** If an unspotted enemy tank fires and the player hears a tank gun, they have learned there
is a tank there — a fog-of-war breach the §12 ladder cannot compensate for, and the exact species of leak
already queued for testing on the movement-range overlay.
**Rule to ratify:** a sound caused by an enemy unit is gated on `SpottedLevel >= Level1`, EXCEPT effects
landing on the player's own units — you always hear your own regiment being shelled, which is §24.8.2's
gate-B attribution case and is already why ambush files a dispatch. ⚠ Applies symmetrically to the AI
turn, where most sound will originate.

### R2 ⚠ FIRE SOUNDS MAP BY FAMILY, NEVER PER PROFILE.
177 weapon profiles. Per-profile fire sounds are unshippable; ~8–12 families is the whole set (§D above).
Precedent exists twice in this codebase — `GetMovementSFX(UnitClassification)` and
`RegimentProfile.ClassifyWeaponType → EquipmentBucket`. **Derive the sound family from the SAME classifier
the loss report uses** so the two cannot drift, exactly as P6 did rather than copying the prefix list.

### ✅ R3 DISSOLVED 2026-08-03 — movement is a ONE SHOT, so no loops exist and no handle API is needed
Bob's call: movement is quick and crisp, so ONE sound fires per move and plays out; nothing stops it on
arrival, interruption or death, and overlap is DESIRED rather than tolerated. Ambience and music already
own their own channels, so **the entire SFX layer is now fire-and-forget** and the Phase 2 facade stays a
single `Play` with no handles to leak. Ratified §27.7.7; §27.7.2's "movement LOOP clip" superseded.

⚠ The risk did not vanish, it MOVED: one clip length cannot serve a 3-hex mountain crawl (0.5 s) and a
24-hex helicopter transit (4.3 s). Longer clips fix long moves and OVERHANG short ones. Resolved by a
STANDARD clip (~1 s) per type plus a LONG clip for the four types that can outrun it, chosen from the
PREDICTED move duration — known before the move starts, so the selection is deterministic and lives in
`GetMovementSFX`. Authoring spec (9 clips, 4 of them longer cuts of the same recording):

| Type | MMP | Max travel | Standard | Long |
|---|---:|---:|---:|---|
| Foot | 4 | 0.7 s | ~1.0 s | none needed |
| Wheeled | 8–10 | 1.8 s | ~1.0 s | ~2.0 s |
| Tracked | 10 | 1.8 s | ~1.0 s | ~2.0 s |
| Helo | 24 | 4.3 s | ~1.0 s | ~4.5 s |
| Jet | 100 | ~2.6 s (MAP-bounded, not MP-bounded) | ~1.0 s | ~2.75 s |

⚠ Implementation note for Phase 3: the `*Long` members append to the END of `SoundEffect` (the enum is
append-only, §3.7b). Original risk text kept below as the reasoning record.

### R3 ⚠ LOOPS NEED A HANDLE API — the current facade is fire-and-forget only.
Movement (C), rotor, and per-theme ambience are LOOPS with a start and a stop. `GameAudio.Play()` cannot
express that. Build the seam in Phase 2 (`PlayLoop` returning a small handle with `Stop()`); retrofitting
it later means touching every call site. ⚠ Movement is also the one that decides its own shape: a per-hex
one-shot at 0.15–0.25 s/hex would machine-gun, so movement must be loop-start-on-order /
loop-stop-on-arrival.

### R4 ⚠ VARIANT ARRAYS, DECIDED NOW — free before the catalog exists, expensive after.
Sounds heard dozens of times per turn (footsteps, small arms, impacts) fatigue badly from a single clip.
The catalog row must hold **`AudioClip[]` with random selection**, not one `AudioClip`. Pitch variation
alone is not enough and starts to sound synthetic.

### R5 ⚠ CONCURRENCY CEILING IS REAL: `m_RealVoiceCount: 32`.
An artillery barrage plus AD fire plus impacts can exceed it; Unity then virtualises by priority and drops
audibility SILENTLY. Needs a per-sound priority and a cap on simultaneous instances of the same effect.
Size the SFX pool against the worst realistic case (a multi-tube fire mission), not the average.

### R6 ⚠ ~40 OF ~85 SOUNDS HAVE NO HOST YET.
Air (M13/AOB), logistics (§15.4a), leader (L1/L2), victory/defeat (§17), turn loop (M13). Declaring their
enum members early is fine and free — the catalog treats a missing row as a silent no-op — but they cannot
be WIRED, and an inventory this size will otherwise read as 85 units of pending work when ~45 are actionable.

### R7 ⚠ NOT EVERY EVENT GETS A SOUND.
§24.8.5 already rules that some refusals are denial SFX rather than dispatches; the converse discipline is
needed here. A game that sounds every event teaches the player to stop listening — the same argument
§24.8.2.4 makes for the printer. Apply the same three-gate test before adding any sound.

---

## 5. NOT DOING, AND WHY

- **AudioMixer** — no latency benefit, costs Bob a setup pass. Revisit if ducking is wanted (music under
  briefing narration is the real use case).
- **Positional / map-located audio** — all audio is 2D (`spatialBlend` never set). A genuine feature,
  scoped on its own; it changes `SfxPlayer`'s shape.
- **Music/ambient/briefing rework** — works, not latency-sensitive, out of scope.

## 6. CARRIED FORWARD

- `SaveSettings` builds a local `JsonSerializerOptions` — CLAUDE.md item 10 violation. Fix in Phase 2.
- `audio_settings.json` lives in `Application.persistentDataPath`, a third write location outside the
  Documents saves+logs pair (Claude_Project §1).
- Briefing failure branches still log an exception where §20.4.2 says absent narration is NORMAL.
  Left until narration is wired.
