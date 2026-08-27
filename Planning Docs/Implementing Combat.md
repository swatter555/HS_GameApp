# Implementing Combat — audio · combat animation · air operations · combat constants

> **What this file is.** The singular-focus plan for the pass Bob opened **2026-08-24**: tighten audio, add
> combat animations, begin air operations, and touch up the global combat constants. It exists so
> `Claude_TODO.md` does not have to carry the detail — the TODO keeps one thread-board row and the change-log
> lines, everything else lives here. Same pattern as `todo_icm.md` / `todo_prestige.md` / `todo_adrange.md`.
>
> **Authoritative spec is still `HS_DesignDoc.md`** (+ Appendix W, `WeaponTrait_Supplement.md`,
> `Supplements/AI-Design-Supplement.md`). This file plans; it does not rule. Anything ratified here gets written
> back into the design doc in the same session (the standing rule in `Claude_TODO.md`).
>
> **⚙ Testing handoff.** The agent cannot run Unity Test Runner or play-test. When a milestone is code-complete
> the agent says **"Please run Unity Test Runner for me"** and WAITS. Nothing is `[x]` until Bob's result lands.

**Status:** PLAN RATIFIED BY BOB 2026-08-24. Landed + suite-GREEN + play-confirmed that day: **A-1**
(both halves), **D-3** (efficiency renames), the `.oob` real-days ruling, and — spun into its own file —
the **supply unification** (`Supply Unification.md`, SUP-1 CLOSED, SAVE_VERSION 9). Nothing is owed a
suite run. **Next up: A-2** (Bob runs `Tools/Audio/Audit Catalog` — a prerequisite), **B ground
animations** (Bob's rider: feel first, defaults assumed), **AIR-0** (needs D3), **D-1** baseline play.
**Bob's riders on the plan:** ground animations come FIRST in workstream B (get a real feel before
ruling D4–D6 — the deferred decisions stand as notes, the agent proceeds on the recommended defaults
and Bob adjusts from play); finding 4's no-AI safety must be documented AND crash-proof (see AIR-2).

---

## 0. HOW TO WORK THIS FILE

- **Legend:** `[ ]` todo · `[~]` in progress · `[x]` done (Bob-verified) · `[-]` deferred/dropped · `[!]` blocked
- Every milestone states what it TOUCHES and what PROVES it. "Make it work" is not an acceptance criterion.
- Milestones needing Bob (art, wavs, Inspector, a play-test, a ruling) are marked **⚑ BOB** and are ALSO mirrored
  into `Claude_TODO.md`'s Bob's-queue / testing-queue sections — this file is not a second inbox.
- **§9 PROGRESS LOG is the record.** Append one line per landed change, newest first. When the pass closes, §9 is
  what gets summarized into the `Claude_TODO.md` pass ledger.

---

## 1. SCOPE, ORDER, AND THE ONE ARCHITECTURAL WARNING

### 1.1 The four workstreams

| # | Workstream | Size | Blocked by |
|---|---|---|---|
| **A** | **Audio tighten-up** — close the gap between 49 declared sounds and 14 catalog rows; fix the two carried-forward defects | Small–medium | Bob authors the wavs; agent does everything else |
| **B** | **Combat animation** — the game currently resolves an attack with *no visual at all* | Medium | Art (Bob). Design hosts already exist: §7.12.4 / §24.8a |
| **C** | **Air operations** — the air RULES are built and tested; the air GAME is unwired | **Large** — the real frontier | Phased; AIR-0/AIR-1 have no gates |
| **D** | **Combat constants** — light-touch tuning of the global dials | Small, but expensive to verify | Every change invalidates `CombatOracleTests`; each round needs a Bob suite run |

### 1.2 Recommended order — A → B → C, with D interleaved as its own suite-runs

Bob's stated order is right and this plan follows it. Two refinements:

1. **Do D (constants) in its own session, not woven into A/B/C.** Any combat-constant change forces a re-run of
   `CombatOracleTests` (the AI EV oracle enumerates the real engine and fails loudly when the mirror is stale).
   Mixing a constants tweak into an animation commit means a red suite that could be either. Keep them apart so a
   failure names its own cause.
2. **Build B as the presentation SPINE that C will reuse, not as a bolt-on.** See the warning below.

### 1.3 ⚠ THE ARCHITECTURAL WARNING — B AND C SHARE ONE PROBLEM, AND SOLVING IT TWICE IS THE FAILURE MODE

Both workstreams need the same thing the game does not have: **a way for gameplay to hand control to a
presentation step and get it back when the step is done.**

- Combat animation needs it so a shot can play out before the next order is accepted.
- Air operations needs it *harder*: §11.1.8.6 makes **reaction yields a DAY-ONE requirement** — the turn loop
  must be able to PAUSE for player input at a reaction point. The design doc's own words: **retrofit = rewrite.**

Today `MovementController.TryAttack` is fully synchronous: orchestrator returns → audio fires →
`RaiseRedrawMapIcons` → done, all inside one call. A fire-and-forget FX layer bolted onto that is exactly what
§11.1.8.6 warns about, and it gets rewritten the moment an AOB needs to suspend.

**So: B ships a small, honest sequencer — one that can await a presentation step — and C's reaction windows are
built on the same primitive.** It does not need to be elaborate. It needs to be *suspendable*.

⚠ This does NOT mean "delay everything until the loop rework." AIR-0 and AIR-1 are deliberately chosen to sit
entirely outside it.

---

## 2. GROUND TRUTH — VERIFIED AGAINST THE CODEBASE 2026-08-24

Read this before planning anything. Several of these are things the older docs get subtly wrong.

### 2.1 Audio — what is actually there

- `GameAudioManager` (~1,627 lines) + `SfxPlayer` (plain C#, injected sources, pure `ShouldPlay`) + `AudioCatalog`
  (ScriptableObject at `Assets/Resources/Audio/AudioCatalog.asset`) + the `GameAudio` static facade + the pure
  policy pair `AudioFogPolicy` / `WeaponSoundClassifier` in `Assets/Scripts/Audio/`.
- **Five channels:** Music + a Crossfade twin, Ambient (looping), Briefing (non-looping + `onComplete`), and an
  **SFX pool of 10**, round-robin, stealing the oldest when all ten are busy.
- **The counts, today:** `SoundEffect` declares **49 members** (48 + `None`). `AudioCatalog` holds **14 rows**.
  `Assets/Audio/SFX/` holds **11 `.wav` files**. So roughly **35 declared sounds have no catalog row at all**,
  and a few rows point at no clip.
  ⚠ Do not trust those derived numbers to stay put — run **`Tools/Audio/Audit Catalog`** for the authoritative
  gap list before authoring anything.
- **All audio is 2D.** `spatialBlend` is never set, there is no `AudioMixer`, and nothing manages an
  `AudioListener`. Positional audio is a scoped feature, not a setting (`todo_audio.md` §5).
- ⚠ **`SoundEffect` IS APPEND-ONLY.** Unity serializes enum fields by INTEGER and `UIButtonAudio` exposes two as
  `[SerializeField]` — the scene YAML literally reads `clickSound: 1`. Inserting or reordering silently repoints
  every Inspector-assigned button sound in every scene and prefab, with no compile error. **Renaming is safe.**
- **Carried-forward defects — ✅ BOTH FIXED 2026-08-24 (A-1), suite GREEN Bob-run same day:**
  - `AudioSettings` now reads AND writes through the NEW **`JsonPolicy.Settings`** (third named policy — flat
    player-written tree; lenient read matters because the file gets hand-edited). The ship-blocker is closed.
  - Briefing: `File.Exists` BEFORE the request splits the two failure kinds — absent file = clean no-op + one
    info log (§20.4.2 normal case); file present but unloadable = real `HandleException` (corrupt content).
  - **Bonus find while sweeping:** `RiverSymmetryVerifier` (editor tool) carried its own local options with NO
    string-enum converter — silently broken against every name-form map since the 2026-07-28 re-export. Now
    reads via `JsonPolicy.Content`, i.e. exactly as `MapLoader` does. The MapChecksumUtility lesson, again.

### 2.2 Combat animation — what is actually there (almost nothing)

- **What exists:** LeanTween (`Assets/LeanTween/`); `UnitMoveAnimator` (static, ~104 lines —
  `AnimateHexStep(icon, to, duration, onComplete)` + `CancelAndSnap`); `GameIconRenderer.AnimateIconStep` /
  `SnapIcon`; the **helo motion flipbook** on `Prefab_CombatUnitIcon` (`StartMotionAnimation` /
  `StopMotionAnimation`, cycling 6 `<unit>_FrameN` atlas frames at a serialized fps, default 40); the
  `Utility1`/`Utility2` overlay `HexLayer`s; `CursorController`'s `TargetPickOutline` stamp.
- **What does NOT exist: any combat visual whatsoever.** `EventManager` has no combat-presentation event of any
  kind. `MovementController.TryAttack` goes orchestrator → audio → `RaiseRedrawMapIcons`. Damage, retreat
  displacement and death all **snap**.
- **Design hosts already ratified, so this is implementation and not design work:**
  - §7.12.4 — the firing unit is briefly highlighted; an animation and sound mark the engagement source.
  - §24.8a.2 — firing unit highlighted with a **directional indicator from firer to target**.
  - §24.8a.3 — an animation plays for the shot (**tracers for AAA, missile trail for SAM**) plus a sound.
  - §24.8a.5 — the result is briefly displayed (**damage % or "miss"**).
  - §11.8.4 — AD opportunity fire is automatic; the UI animates the engagement and reveals the firer at Level 4.
- **Art on hand** (`Assets/Art/Sprites/`, all with `SpriteManager` constants): `TargetPickOutline`,
  `ThreatFill_Amber` / `_Red` / `_DeepRed`, `FacingChevron_*` ×6, `MoveRangeFill`, `MoveRangeZocStop`,
  `MovePathStep`, `MovePathEnd`, and — relevant to workstream C — **`AirMissionMarker`**.
  **No tracer, muzzle-flash, explosion or impact art exists.** That is the art ask.
- ⚠ **§27.7.8 IS THE GATE AND IT APPLIES TO PIXELS TOO.** Hidden movement is silent AND **unpaced** — an
  unspotted enemy's move resolves INSTANTLY. The same must hold for combat: an unspotted firer must not animate.
  `AudioFogPolicy.CanHear` already encodes exactly this rule, is pure and headless-safe, and **the visual gate
  must reuse it rather than spell a second threshold** (the §27.7.5.1 discipline: one classifier, never a
  parallel one). Attribution mirrors audio: **the FIRE effect belongs to the firer, the IMPACT effect belongs to
  the target** — so an unseen battery shelling the player shows an impact and no muzzle.

### 2.3 Air operations — the rules are done; the game is not

**BUILT AND EDITORTEST-COVERED (pure, seedable, `Models/Combat/`):**

| Piece | What it answers |
|---|---|
| `AirCombatEngine` | `DogfightOffense` (DF+MAN)/2 · `DogfightDefense` (MAN×2+SUR)/3 · `PairingMetric` · `ResolveDogfightPass` · `ResolveBreakthrough` · `StealthAvoidanceChance`/`RollStealthAvoidance` (§11.4.8, §11.5) |
| `AirStandCheck` | SV_air = 6 + Exp + floor((TS+MAN)/8) − Shock; binary hold/retreat (§11.4.8.2a) |
| `AirAmbushCheck` | the §6.10 1d6-vs-experience detection threshold |
| `HeloTransitStandCheck` | §11.8.9 hold-and-continue vs abort |
| `ReconMissionEngine` | §11.11 RB tiers |
| `AOBMissionResolver` | `ResolveBoxType` · `CandidateBoxTypes` · `IsOperativeLegalForTarget` · `ResolveAsbMission`; enums `AOBType{None,ASB,AAB,RB,AEWB,SB,AIB}`, `AsbMissionType`, `AOBTargetCategory` |
| `AOBStatus` | the §24.7a.7 panel snapshot DTO — already shaped, already has an event to ride |
| `CombatResolver` | `ResolveAirStrike` (§11.6) · `ResolveBaseAttack` (§11.7, incl. `ParkedAircraftDamage`) · `ResolveAirDefenseFire` (§11.8) · `ResolveOverheadFire` (§11.8.11) |

**ALREADY LIVE IN PLAY (the transit half — D2/D3 2026-08-11, plus the 08-22 envelope fix):**
`MovementController.ResolveTransitFire` is the single per-hex entry point; `SpottingService.FindTransitAirDefense`
scans with the envelope read as `max(ActiveIndirectRange, ActivePrimaryRange)`; `RollFixedWingAmbushDetection`
(§6.10, fixed-wing only); `RevealByOpportunityFire` → Level 4; §11.8.11 overhead GAD fire; the two anti-dogpile
records (`enemiesEngagedThisMove` per move-order, `CombatUnit.MarkAircraftEngaged` per turn).

**ALREADY DECLARED, ZERO IMPLEMENTATION:** `EventManager` carries all five AOB events
(`OnAOBPlacementModeRequested`, `OnAOBPlacedOnHex`, `OnAOBResolveRequested`, `OnAOBAbortRequested`,
`OnAOBStateChanged(AOBStatus)`) plus `OnAirUnitReturning`. `CombatUnit.CanLaunchSortie()` exists with **zero
consumers**. `AnimateAutoReturn` + `OnAirUnitReturning` exist with **zero callers**.

**MISSING — workstream C's actual build list:** the `AirOperationsBox` entity · the placement input mode
(§24.7a.1) and AOB-Mode lockdown (§11.1.9) · slot fill + type-flip on arrival · the Resolve sequence ·
`ReactionWindowController` (§11.1.8) · per-sortie supply deduction (§11.2.3) · fixed-wing launch / auto-return
(§5.13.5) · `AirThreatService` and the §24.7a.8 threat overlay.

**⚠ WHAT KHOST CAN AND CANNOT EXERCISE — this drives the phase order.** Khost's OOB (54 units) holds:

- **Player:** 2 × `AIRB` Soviet Airbase with **4 attached `ATT` Su-17 regiments**; 2 × `HELO` (Mi-8AT, Mi-24D);
  1 × `MAM` air-assault (Mi-8T embark); 1 × `MAB` VDV (An-8 embark).
- **AI:** 4 × `SAM_GEN_MJ`, 1 × `AAA_GEN_MJ`. **No fighters. No aircraft at all.**

So Khost **CAN** play: AOB placement, the fixed-wing transit walk, en-route AD opportunity fire against the
Su-17s, ASB **ground strike**, in-hex ground fire, egress fire, Resolve, auto-return, and the whole helo layer.
Khost **CANNOT** play: defender interception, escort dogfights, breakthrough, WW/SEAD (no Soviet WW airframe in
this OOB), AEWB (no AWACS), RB (no RECONA), or the AIB. **Those wait for Hamburg.**

✅ **RESOLVED BY RULING 2026-08-24 — `.oob` `DaysSupply` IS NOW REAL DAYS (was a 0.0–1.0 ratio).** Bob:
"we need to be working in days or it is just confusing — all units need to show the real numbers." The old
ratio form made Khost's airbases author `1` (= full) while every in-game surface, and the §11.2.3a 5-day
launch floor, speaks days — the file read as "grounded" when it meant "full". Landed: the loader reads days
and clamps to [0, Max] with a warning (`StatsMaxCurrent.SetCurrent` does NOT clamp to Max, so this mattered);
a per-file tripwire warns when EVERY value is ≤ 1 (the signature of an un-migrated ratio file); `khost.oob`
re-authored in step (units 5.0, airbases 30.0 — behavior-identical to the old full-ratio load). ⚠ **The
editor relay is URGENT** — Hamburg's OOB is in authoring right now, and an editor still emitting ratios
produces a starving scenario the tripwire will flag but not fix. ⚠ `HitPoints` deliberately STAYS a ratio —
changing it was not ruled; flagged as a question in the relay so the asymmetry is a decision, not drift.
**FOLLOW-ON (same day, on Bob's green report): depots LIST their stockpile.** A depot's `StockpileInDays`
never appeared in the `.oob` — `SetDepotSize` silently filled it to capacity (Small 30 · Medium 50 ·
Large 80 · Huge 110), so the file could neither show nor set the one number that makes a depot matter. New
optional `.oob` field `StockpileInDays` (real days; **−1/absent = full for size**, the exact old behavior;
**authored 0 = deliberately empty** — that asymmetry is why the sentinel is −1) landing through new
`CombatUnit.SetStockpile`, which clamps inside the model so the size table keeps one authority. Non-depot
authoring warns and is ignored — an airbase's stockpile is its `DaysSupply`, already explicit. `khost.oob`
lists 7 × 30.0 caches + the Soviet Large at 80.0. Pinned by `DepotStockpileTests` (5).

⚠ **Attached aircraft are OFF THE BOARD.** All four Su-17s sit at `MapPos (0,0)`;
`GameIconRenderer.IsAtFriendlyAirbase` filters them out of the map and the airbase draws an `AirbaseStack_N`
badge instead. **"Launch" therefore means detach + place at the airbase hex**, and "return" the reverse — there
is no separate parked state to invent.

### 2.4 Combat constants — where they live and what guards them

- **The two master levers, both currently `1.0f`, applied as engine step 6 (§7.7.10):**
  `GameData.GROUND_BALANCE_MOD` and `GameData.AIR_BALANCE_MOD`. Per-domain, tunable without touching a stat.
- **Stand check (§7.9):** `STAND_BASE 6` · `STAND_RETREAT_GAP 3` · `STAND_ROUT_GAP 6` · `SHOCK_DIVISOR 4` ·
  `SHOCK_MAX 8` · `LEADER_STAND_MOD_CAP 3`. ⚠ §7.9.5.1 says **tune base / gaps / shock divisor together.**
- **Flanking (§31.4a.15):** `FLANK_DAMAGE_MULT 1.15f` · `FLANK_SV_PENALTY 1`.
- **Break-up (§7.9.6–.7):** `SHATTER_EXTRA_DAMAGE 4` · `SURRENDER_CHECK_BASE 10` · `SURRENDER_CHECK_EXP_FACTOR 2`
  · `SURRENDER_SURVIVAL_LOSS 10` · `STATIC_COLLAPSE_BASE 30` · `STATIC_COLLAPSE_PER_EXP 5`.
- **Air / base:** `STRATEGIC_OC_BONUS 20` · `GAT_INTERDICT_THRESHOLD 6` (an EFFECTIVENESS floor, **never** the
  eligibility gate — that is `IsAirDefenseClassification`).
- **The multiplier stack** (`Final = Base × Strength × Deployment × Efficiency × Experience × ICM`): Strength
  1.15 / 0.75 / 0.4 · Deployment 1.0 / 1.1 / 1.2 / 1.3 · Efficiency 1.0 / 0.9 / 0.8 / 0.7 / 0.5 · Experience
  0.8 → 1.3 · ICM clamped [0.1, 10.0], aggregate kept ≲1.6 (NATO apex M1 = 1.53).
- **The dice are NOT in `GameData`** — the Δ→band ladder and every damage die live in
  `Models/Combat/CombatMath.cs` (`DeltaBand`; `RollBandDamage` Hopeless 0 … Crushing 2d8+6; `RollTerrainBlock`
  None / 1d2 / 1d4 / 1d4+2). Changing lethality has two very different levers: a **balance mod** (smooth, global)
  or a **die** (structural — it changes the shape of the distribution). Prefer the mod.
- ⚠ **`CombatOracleTests` IS THE HARD CONSTRAINT.** `CombatOracle` + `Pmf` are an exact analytic EV mirror of the
  engine; the drift guards enumerate the real engine and fail when the mirror is stale. **Re-run after ANY
  combat-constant change** — no exceptions, and it is a Bob run every time.
- ⚠ **DO NOT re-open two things that were just settled:** the **AD engagement envelope** (fixed + re-banded
  2026-08-22 — "do not re-price AD around the fix") and the **prestige / unit economy** (re-tiered 2026-08-22).

**✅ NAMING TRAP FIXED 2026-08-24 (D-3 done early, suite GREEN Bob-run same day).** The efficiency constants were named
one rung off the enum they serve (`EFFICIENCY_MOD_FULL` was the `CombatOperations` value, `_PEAK` was
`FullOperations`). Verified single consumer (`CombatUnit.GetEfficiencyModifier`), values untouched, renamed
to spell their `EfficiencyLevel` member: `_STATIC_OPS 0.5 · _DEGRADED_OPS 0.7 · _NORMAL_OPS 0.8 ·
_COMBAT_OPS 0.9 · _FULL_OPS 1.0`. Const floats — nothing persisted moved. Done BEFORE any D-2 tuning, which
was the point.

---

## 3. WORKSTREAM A — AUDIO TIGHTEN-UP

**Goal:** every sound the game already has a HOST for is declared, catalogued and wired, so Bob's wav authoring
is a pure drop-in with no code round-trip. Plus the two carried-forward defects.

### A-1 — Fix the two carried-forward defects `[x]` — DONE, suite GREEN + play-confirmed (Bob-run 2026-08-24)
- **Touches:** `GameAudioManager` (the AudioSettings save path), the briefing load branches.
- Route `AudioSettings.SaveSettings` through `JsonPolicy` (CLAUDE.md item 10). If the settings format genuinely
  differs from `Save` and `Content`, add a **third named policy** inside `JsonPolicy` — never a local options
  object.
- Briefing: an absent narration asset for a **standalone** scenario is NORMAL (§20.4.2) — log at info, not as an
  exception. A missing narration for a **campaign mission** stays a real warning.
- **Proves it:** `AudioSystemTests` stays green; no `JsonSerializerOptions` constructed outside `JsonPolicy` (a
  grep is sufficient). Clears one of the two ship-blockers in `Claude_TODO.md`.

### A-2 — Run the audit, then close the declared-vs-catalogued gap `[ ]` **⚑ BOB (one tool run)**
- **⚑ BOB:** run `Tools/Audio/Audit Catalog` and paste the output. That is the authoritative gap list; the
  49 / 14 / 11 counts in §2.1 are a snapshot, not a spec.
- Agent then adds a catalog row for every declared sound with a live host, with sensible default volume /
  pitch variation / `minRetriggerSeconds`. **Rows can exist without clips** — a row with no clip is a silent
  no-op, which is the correct behaviour and is exactly what makes the wav a drop-in.
- ⚠ `minRetriggerSeconds` defaults to **0 = OFF**, deliberately. Only opt a sound in when it can genuinely fire
  many times in one instant (candidates: `PrinterTick`, `ImpactSoft` / `ImpactArmour` under a rocket salvo).

### A-3 — Append the enum members whose hosts ALREADY exist, and wire them `[ ]`
- **⚠ APPEND ONLY, at the end of `SoundEffect`.** One append, one commit, covering A-3 *and* the air group from
  workstream C — so the enum is touched once rather than four times.
- Hosts that exist today and are silent (from `todo_audio.md` §4b, filtered to live hosts):
  - **Deployment / unit actions (§F):** `DeployUp`, `DeployDown`, `IntelAction`, `Embark`, `Disembark` — hosts
    are `MovementController` / the deploy callbacks, all live.
  - **Combat outcomes (§D, §7.9.5/.6/.6a):** `UnitRetreat`, `UnitRout`, `UnitShatter`, `UnitSurrender` — hosts
    are the `StandOutcome` arms in `GroundCombatAction`, all live. Also `CounterBattery` (§7.13 host is live)
    and `UnitHardened` (already dispatched by `PrinterDispatch.ReportUnitHardened`).
  - **Shell / UI (§A):** `DialogOpen`, `DialogClose`, `ListSelect`, `DispatchArrived`, `TurnBegin`, `TurnEnd`.
  - **Objectives / end (§I):** `PrestigeAwarded` (host live since the wallet landed), `VictorySting`,
    `DefeatSting` (hosts live since §17 grading landed 2026-08-17).
- **Attribution is not optional.** Every unit-caused sound uses `GameAudio.PlayFrom(...)` / `PlayWeaponFire`;
  only genuinely source-less sounds use `GameAudio.Play(...)`. The two-method split IS the fog gate (§27.7.4).
- **Proves it:** each new sound has a call site an EditorTest can reach, and `AudioPolicyTests` still pins the
  intended silences. ⚠ Silence is also the CORRECT outcome for an unmapped id — pinning the intended silences is
  the only way to tell them from the accidental kind.

### A-4 — Decide the two scope questions before they get expensive `[ ]` **⚑ BOB (rulings)**
Both are cheap now and structural later. Carried into §7 as decisions D1 and D2.
- **AudioMixer** — currently "not doing" (`todo_audio.md` §5). The real use case is **ducking**: music under
  briefing narration, and — new with workstream B — ambience under a combat sequence.
- **Positional audio** — all audio is 2D. Combat animation makes a hex-located shot sound natural for the first
  time. It changes `SfxPlayer`'s shape, so it is a decision, not a tweak.

### A-5 — Measure the SFX voice ceiling `[ ]` **⚑ BOB (play observation)**
- The SFX pool is **10 sources, stealing the oldest**. §27.7.7.2 says overlap is DESIRED, not tolerated.
  Workstream B (fire + impact + outcome per engagement) and C (a strike package under multi-battery AD fire) both
  multiply concurrent one-shots.
- **DO:** play a Khost turn with several artillery and rocket attacks resolving back to back and listen for
  truncation. **PASS:** no sound audibly cuts off mid-play. **WHY:** the ceiling is a one-line constant now and an
  audible defect later. Do not raise it speculatively — Unity's own `m_RealVoiceCount` is 32, so headroom exists,
  but a pool sized on a guess is how the old system got its latency problem.

### A-6 — Bob's authoring queue `[ ]` **⚑ BOB (already in `Claude_TODO.md`)**
Movement long cuts for **helo and jet**; `UIButtonAudio` onto the **battle-HUD buttons** (none are wired — 7
MainMenu buttons only, so the whole HUD is silent). Not tracked twice; listed here for completeness.

---

## 4. WORKSTREAM B — COMBAT ANIMATION

**Goal:** an attack you can watch. Minimum bar is §24.8a — you can see WHO fired, at WHOM, and WHAT happened.

### B-0 — The presentation spine (do this first; C reuses it) `[ ]`
- **New:** a `CombatPresenter` MonoBehaviour (Controllers) driven by **one new EventManager event** carrying a
  presentation DTO. Per the project rule the event is declared in `EventManager`, and call sites carry a
  `// See EventManager` comment.
- **The DTO is built from the outcome struct, at the call site, and holds nothing live** — firer id, target id,
  both hexes, weapon family, damage dealt each way, `StandOutcome`, destroyed flags, final defender hex. The
  model layer keeps knowing nothing about presentation.
- **Suspendability is the whole point (§1.3).** `CombatPresenter` exposes a completion signal the caller can
  await, and `MovementController` is restructured so the post-attack board refresh happens *after* it. Write it as
  a resumable step even though v1 never suspends for input — that is what makes §11.1.8 a wiring job later
  instead of a rewrite.
- **v1 sequencing (agent's recommendation, flagged for Bob as D4):** the board updates IMMEDIATELY and the FX
  plays OVER the updated board — Panzer General's behaviour. Delaying the HP/removal update behind the animation
  is more cinematic and considerably more dangerous: it puts the view out of sync with the model for the effect's
  duration, and every read during that window is a potential defect.

### B-1 — The fog gate for pixels `[ ]`
- Add the visual half **into the existing `Assets/Scripts/Audio/AudioFogPolicy.cs` policy family** (or rename the
  file to a neutral `FogPresentationPolicy` — the type is pure and has no Unity dependency either way).
  **Do not write a second threshold.** §27.7.4.3's Level-1 reasoning applies unchanged.
- **Attribution mirrors audio exactly:** fire FX = the firer's, impact FX = the target's. An unspotted battery
  shelling the player shows an impact and no muzzle — the same information the audio layer already gives.
- ⚠ **Fails CLOSED** on a null / unattributed source, same as `CanHear`. A missing effect is cosmetic; a leaked
  one is exploitable, and §6.9 ambush is load-bearing on unspotted ambushers.
- ⚠ **§27.7.8 extends here:** a fully hidden engagement is not merely invisible, it is **unpaced** — it must not
  consume presentation time during the AI turn. Pinned by a test.

### B-2 — The §24.8a minimum set `[ ]` **⚑ BOB (art)**

> **⚠ BOB'S RIDER (2026-08-24): GROUND ANIMATIONS FIRST.** Build the direct ground engagement (`TryAttack`
> path) end-to-end before anything else in this table, so he can get a real feel for pacing and style in play
> before ruling D4–D6. Until those rulings land, proceed on the recommended defaults (update-immediately
> sequencing, flipbook art, 0.25–0.6 s timings) — they are working assumptions, revisable from play, not
> ratified decisions.

| Element | Spec | Built from |
|---|---|---|
| Firer highlight | §7.12.4 / §24.8a.2 — brief highlight on the firing unit | existing `HexLayer` utility stamp + a tint tween |
| Direction indicator | §24.8a.2 — firer → target | ⚑ new art, or reuse `FacingChevron_*` along the line |
| Shot effect | §24.8a.3 — tracers (gun), missile trail (ATGM/SAM), arc (indirect) | ⚑ **new art** — nothing exists |
| Impact | burst on the target hex, armour vs soft, matching the audio split | ⚑ **new art** |
| Result readout | §24.8a.5 — damage or "miss" | text, no art needed |
| Hit flash | target icon flash on damage | existing `Prefab_CombatUnitIcon` renderer |
| Displacement | retreat / rout tweens instead of snapping | **existing `UnitMoveAnimator.AnimateHexStep`** |
| Kill | destruction effect before the icon is removed | ⚑ new art |

- ⚠ **The flipbook pattern already exists and should be reused** — `Prefab_CombatUnitIcon.StartMotionAnimation`
  cycles `<name>_FrameN` atlas frames at a serialized fps. An effect is the same mechanism with a different
  atlas. That keeps the art pipeline identical to the one Bob already uses (PNG frames → sprite atlas), needs no
  particle system, and stays inside the existing sorting / layer discipline.
- ⚠ **Unity layer 7, or it renders under everything.** Effects are map visuals; `HexLayer.SetSprite` inherits the
  host object's layer for exactly this reason. Sorting goes through `SortingConfig` — an effect needs a
  `SortSlot`, not a hand-set `sortingOrder`.
- **⚑ BOB — art ask, in priority order:** impact burst (soft + armour) → tracer / shot streak → destruction →
  missile trail → direction indicator. The first two carry most of the readability.

### B-3 — Timing and skip `[ ]` **⚑ BOB (feel ruling)**
- One serialized duration per effect class, defaults in the 0.25–0.6 s range, tuned in play. §27.7.8.2's reasoning
  applies: pace from the END of the previous step, never on a fixed cadence.
- **A skip / instant toggle is a day-one requirement, not a polish item.** A 30-unit AI turn at 0.5 s per
  engagement is unwatchable by mission 10. ⚑ Bob: settings toggle, held key, or both? (§7 D5.)

### B-4 — Extend to the paths that are NOT `TryAttack` `[ ]`
Once B-0/B-2 land, the same presenter serves: ground **ambush** (`AmbushAction` — ⚠ attributed to the VICTIM, per
the audio ruling and for the same reason), **indirect + counter-battery** (§7.13), **AD opportunity fire** (§11.8
— this one is *required* by §11.8.4's "UI animates the engagement"), and **overhead GAD fire** (§11.8.11). The AD
arms are the bridge into workstream C.

---

## 5. WORKSTREAM C — AIR OPERATIONS

**Goal for this pass: `AIR-0` and `AIR-1` complete and playable in Khost; `AIR-2` designed and started.**
`AIR-3` / `AIR-4` are named here so the shape is visible — they are NOT this pass's scope.

### AIR-0 — `AirThreatService` + the §24.7a.8 AD threat overlay `[ ]`  ← **START HERE**
The best first move in the whole workstream: no turn-loop dependency, no new art, and it puts the 2026-08-22
envelope fix on screen where a human can finally see it.
- **One shared helper, consumed by BOTH the overlay and the §11.8 walk — that is the point.** `CanInterdict`
  (AD classification + `GAT ≥ GAT_INTERDICT_THRESHOLD` + the §11.8.8 posture gate) and the footprint
  (`max(ActiveIndirectRange, ActivePrimaryRange)`, per §11.8.2d). **The overlay must never be able to lie**, and
  it can only be guaranteed not to by being the same code.
- ⚠ **§11.4.4's "spotting +" term is DELIBERATELY UNDECIDED, and this is where it gets ruled — once.** It was
  explicitly deferred from the AD range pass to `AirThreatService` so the overlay and the walk cannot disagree.
  **Carried into §7 as decision D3.**
- Art is **already present**: `ThreatFill_Amber` / `_Red` / `_DeepRed`, hex-shaped (needs `FitToCellScale`).
  Banding is by GAT; overlap darkens to the worst band.
- **Proves it:** in Khost the 4 Mujahideen SAMs and the AAA paint envelopes matching the ratified §11.8.2d ladder
  (point-defense 4 · guns 3), and `FindTransitAirDefense` fires at exactly the painted hexes and no others.

### AIR-1 — Fixed-wing launch, transit, and auto-return `[ ]`
The transit *fire* is already live; what is missing is getting an aircraft onto and off the map.
- **Launch:** detach from the airbase → place at the airbase hex → `CanLaunchSortie()` gate (§11.2.3a, the 5-day
  hard reserve — **finally give that method its first consumer**) → deduct `SORTIE_LAUNCH_COST` 1 supply → pay
  1 CombatAction (§11.4.7.1). Refuse below the floor with the §11.2.3a message and `ButtonDenied`.
- **Transit:** the per-hex walk at 1 MP/hex ignoring terrain (§5.13.1); `ResolveTransitFire` already runs per hex;
  add the §11.8.3 shot budget and confirm the two anti-dogpile records behave across a full sortie.
- **Per-shot supply:** `SORTIE_SHOT_COST` 0.5 deducted from the **launching airbase**, at the moment of the shot
  (§11.2.3). Needs the aircraft to remember which base launched it.
- **Auto-return (§5.13.5):** end of turn, free — no MP, action, combat, ambush or weather check, and it cannot be
  attacked on the return leg. `AnimateAutoReturn` and `OnAirUnitReturning` **already exist with zero callers** —
  this is their wiring, not a new build. §11.2.4: if the launching base died mid-mission, divert to the nearest
  friendly airbase, else the unit is lost.
- ⚠ **Fixed-wing is ground-blind in transit (§12.3.7a)** and may temporarily share a ground unit's hex, but may
  not REST anywhere — "may I stop here" stays on `IsAirUnit`, deliberately (see `MovementModeService`). A
  fixed-wing left on the map at end of turn is the §5.13.5 gap, and auto-return is what closes it.
- ⚠ **Fixed-wing carry NO own supply (Bob 2026-08-24; `Supply Unification.md` §1.5):** `DaysSupply` Max 0 —
  the airbase pays launch 1 / shot 0.5. Lands with SUP-1, and its `CanMove` guard (`Max > 0`) is a
  PREREQUISITE for this milestone — without it a 0-supply aircraft can never be ordered to move. The
  cycle-through-airbase-aircraft UI (this milestone's input work) shows a fixed-wing's supply as zero.
- **Proves it:** a Khost Su-17 launches, crosses Mujahideen SAM cover taking real opportunity fire at the ranges
  AIR-0 paints, and returns to base at end of turn with the airbase stockpile down by the right amount.
  ⚠ **This is also the play-verification D2's fixed-wing half has been waiting for since 2026-08-11.**

### AIR-2 — The AOB spine, ASB ground-strike only `[ ]`
- **`AirOperationsBox` model:** target hex, `AOBType` (None until flip), `AOBTargetCategory`, WW pre-lock, the
  §11.3.1 slots, and the commit flags. `AOBStatus` is **already the snapshot DTO** and `OnAOBStateChanged` is
  already declared — build to them.
- **Placement input (§24.7a.1):** HUD button arms placement mode → the `AirMissionMarker` sprite rides the cursor
  (**art already exists**, `SpriteManager.Utility_AirMissionMarker`) → **Ctrl+left-click places**; plain
  left-click stays universal selection so the player can inspect threats while placing; right-click or Esc exits
  *placement mode* without placing. ⚠ **Esc never cancels a placed box** (§11.1.9) — removal is the Cancel button.
- **AOB-Mode lockdown (§11.1.9):** air-only input, **SAVE DISABLED while a box is open**, one AOB at a time
  (§11.1.6).
- **Flip + slots:** the first operative arrival flips the box via `AOBMissionResolver.ResolveBoxType`; a WW
  pre-locks.
- **Resolve (§11.4.8), the arms Khost can actually reach:** stealth check → *(no interceptors in Khost, so
  .8.2 / .8.2a / .8.2.1 are structurally exercised but never fire)* → **11.4.8.5 in-hex ground fire** →
  **11.4.8.6 ground attack** (`CombatResolver.ResolveAirStrike`; OL applied as base × OL/9; WW band-shift if
  alive) → **11.4.8.7 egress fire**.
- **⚠ BOB'S RIDER (2026-08-24) — THE NO-AI CASE IS A HARD ACCEPTANCE CRITERION, NOT A FOOTNOTE.** Until the
  AI flies, every air-ops path must NO-OP CLEANLY when the other side has no aircraft, no airbases, or no
  reaction to give: the reaction-window walk yields zero windows and falls through (never throws, never waits);
  Resolve runs with zero interceptors; transit fire against a side with no AD simply finds no batteries. Each
  of those null-side cases gets its own EditorTest when the milestone is built — "it works in Khost" is not
  evidence, because Khost IS the null case; the tests are what prove the empty branches on purpose.
- **The reaction-window INTERFACE ships here, v1 decline-all (§11.1.8.6).** ⚠ **And here is the finding that
  de-risks this milestone:** during the PLAYER's turn the reaction windows belong to the DEFENDER, which in Khost
  is an AI with no aircraft — so **v1 never actually suspends**, and AIR-2 does NOT require the reaction-yielding
  turn loop to exist first. The yield becomes mandatory only when the AI flies and the HUMAN must answer, which is
  an AI-track dependency anyway. **Ship the S2 reaction-policy interface and write Resolve as a resumable state
  machine regardless** — §11.1.8.6's "retrofit = rewrite" is about the SHAPE, not about whether v1 pauses.
- **Costs are paid AT LAUNCH, not on Resolve** (§11.4.7, consistent with §24.7a.5 cancellation forfeiture).
  Cancel / end-of-turn = free auto-return, **actions lost**.
- **Presentation** rides workstream B's presenter — the second reason B comes before C.

### AIR-3 — Live interception `[-]` (not this pass; **gated on Hamburg**)
Per-arrival reaction windows firing for real, `ReactionWindowController` + the §24.13 Phase Control Bar,
escort-vs-interceptor dogfights, breakthrough, WW / SEAD orchestration. ⚠ **WW arrivals open no window** — that is
the bait-proofing, and it is easy to lose. Needs an OOB with an enemy air force; Khost has none.

### AIR-4 — The other box types `[-]` (not this pass)
AAB (§11.12) · RB (§11.11, plus the I8 RB tiers) · AEWB (§11.13) · SB (§11.9) · the reactive helo AIB (§11.8.10).
⚠ The AIB's dual-phase economy (§8.5.1a — the phasing side pays Combat, the reacting side pays Opportunity) must
NOT be hard-coded to roles.

### C-X — Things that must not be forgotten inside this workstream
- `WW` / `TRN` treatment in the ~7 hardcoded air/action checks (the C1/C2 debt).
- §5.13.4 Storm grounding and §5.13.3.3 (fixed-wing cannot change deployment state — today an accident of empty
  bays, not a rule). Storm additionally needs weather to exist.
- The air sound group (`todo_audio.md` §4b group E, 16 sounds) — declared in **A-3's single enum append**, wired
  here.
- ⚠ **The AI does not fly and will not during this pass.** `FindTransitAirDefense` is already side-agnostic, so
  player AD will engage AI aircraft the day they exist — do not add a side check to "make it work now."

---

## 6. WORKSTREAM D — COMBAT CONSTANTS

**Framing (Bob, 2026-08-24): the combat values are "surprisingly pretty solid."** This is a light-touch pass — a
few named dials, each with a stated reason and its own suite run. **It is not a rebalance.**

### D-0 — The protocol, because verification is the expensive part `[ ]`
1. Change **one** dial (or one deliberately-coupled group — §7.9.5.1 binds base / gaps / shock together).
2. Update the affected EditorTest expectations in the same edit.
3. **Re-run `CombatOracleTests`** — non-negotiable, and a ⚑ Bob run every time.
4. Play-check in Khost. Record the observed effect in §9, not just the number.
- ⚠ **Never bundle a constants change into an A/B/C commit.** A red suite must name its own cause.

### D-1 — Establish the baseline before touching anything `[ ]` **⚑ BOB (play observation)**
- **DO:** play a Khost turn and record, roughly: how many attacks it takes to kill a full-HP defender; how often a
  stand check yields hold / retreat / rout / shatter; whether terrain block feels decisive or ignorable.
- **WHY:** §7.9.5.1 defines distribution TARGETS for the stand outcomes. Without a measured "before", any change
  is a guess and the second change cannot be attributed. **Nothing else in D should start before this.**

### D-2 — The candidate dials, in the order they are worth touching `[ ]`

| Dial | Current | Reach for it when |
|---|---|---|
| `GROUND_BALANCE_MOD` | `1.0f` | ground combat is uniformly too fast or too slow. **The right first lever — smooth, global, structure-preserving** |
| `AIR_BALANCE_MOD` | `1.0f` | ⚠ hold until AIR-1 has flown. Tuning air lethality before anything flies is guessing |
| `STAND_BASE` / `_RETREAT_GAP` / `_ROUT_GAP` / `SHOCK_DIVISOR` | 6 / 3 / 6 / 4 | outcomes cluster on one rung. **Tune together (§7.9.5.1)**, never singly |
| `STRENGTH_MOD_*` | 1.15 / 0.75 / 0.4 | the 0.4 Low-strength cliff makes damaged units useless rather than fragile. Structural — the steepest curve in the stack |
| `FLANK_DAMAGE_MULT` / `_SV_PENALTY` | 1.15 / 1 | flanking never changes a decision. Cheap, well-isolated |
| terrain block dice | 1d2 / 1d4 / 1d4+2 | mountain / major-city defence is decisive or irrelevant. ⚠ `CombatMath`, not `GameData` — a **distribution-shape** change, not a scalar |
| `SHATTER_EXTRA_DAMAGE`, `SURRENDER_*`, `STATIC_COLLAPSE_*` | see §2.4 | only after D-1 shows the break-up tail is wrong |

- **Out of scope, explicitly:** the AD envelope ladder and the prestige economy — both ratified 2026-08-22.
  Re-opening either needs a Bob ruling first, recorded in §7.

### D-3 — Rename the misleading efficiency constants `[x]` — DONE, suite GREEN (Bob-run 2026-08-24)
- `EFFICIENCY_MOD_PEAK` → `EFFICIENCY_MOD_FULL_OPS` · `EFFICIENCY_MOD_FULL` → `EFFICIENCY_MOD_COMBAT_OPS` ·
  `_OPERATIONAL` → `_NORMAL_OPS` · `_DEGRADED` → `_DEGRADED_OPS` · `_STATIC` → `_STATIC_OPS`. One name per
  `EfficiencyLevel` member, spelled the same way.
- **Zero behaviour change, zero risk:** these are `const float`s with a handful of call sites, not persisted
  enums (CLAUDE.md item 11 does not apply). The current names invite exactly the wrong edit from exactly the
  person doing D-2. **Do this BEFORE D-2, not after.**

---

## 7. DECISIONS OWED — Bob only

> **Standing posture (Bob, 2026-08-24): noted, not yet ruled.** Work proceeds on the agent's recommended
> defaults — ground animations first so the feel informs D4/D5/D6 rather than the other way round. Rulings
> land here as they come; a default that survives Bob's play unprotested is still not ratified until he says
> so. **Exception: D3 genuinely blocks AIR-0's ship** — it needs an answer before that milestone closes
> (building the service with the IR-only envelope and slotting the term in later is acceptable scaffolding).

| # | Decision | Why it cannot wait |
|---|---|---|
| **D1** | **AudioMixer: yes or no?** | Free now, a retrofit later. The real case is ducking — music under briefing narration, ambience under a combat sequence (new with workstream B) |
| **D2** | **Positional / hex-located audio: yes or no?** | All audio is 2D today. Combat animation makes a located shot sound natural for the first time; it changes `SfxPlayer`'s shape, so it is a design call |
| **D3** | **§11.4.4 "spotting +": does the AD engagement envelope add a spotting term, or is it the IR stat alone?** | Deliberately deferred from the AD range pass to `AirThreatService` so the overlay and the walk cannot disagree. **AIR-0 cannot ship without it** |
| **D4** | **Combat-animation sequencing:** board updates immediately and FX plays over it (recommended), or FX plays first and the board updates after? | Decides `CombatPresenter`'s shape. Agent recommends **update-immediately** — the alternative desynchronizes view and model for the effect's duration |
| **D5** | **Animation skip:** settings toggle, held key, or both? | Day-one, not polish — a 30-unit AI turn at 0.5 s per engagement is unwatchable by mission 10 |
| **D6** | **Effect art style:** frame-sequence sprites in an atlas (reusing the `Prefab_CombatUnitIcon` flipbook pattern — agent's recommendation), or a particle system? | Decides whether B-2 needs any new tech at all. Flipbook keeps Bob's existing PNG→atlas pipeline and the current sorting discipline |
| **D7** | **Does the AI/OPFOR `arrivalTurn` reinforcement item** (added to `Claude_TODO.md` M13 on 2026-08-24 via the editor agent) **belong in this pass or its own?** | It is Hamburg-driven and turn-loop-adjacent, so it collides with workstream C. Agent's read: **its own pass** — it carries a `SAVE_VERSION` decision |
| **D8** | **Air sound naming** — confirm the 16 `todo_audio.md` §4b-group-E names before the single enum append in A-3 | `SoundEffect` is append-only. Renaming is safe; re-ordering is not. One append is much better than four |

---

## 8. TESTING QUEUE OWED TO BOB (mirrored into `Claude_TODO.md`)

**CLEARED 2026-08-24 (Bob-run):** the day-one fix batch came back GREEN with the Khost play checks as
expected — A-1, D-3 and the `.oob` days change are all confirmed.
**QUEUED 2026-08-24 (mirrored to `Claude_TODO.md` ⚑):** the depot-stockpile follow-on — suite run (new
`DepotStockpileTests`, 5) + a Khost look (caches 30/30, Soviet depot 80/80, no StockpileInDays warnings). As milestones become code-complete, each one adds a `⚑` entry to
`Claude_TODO.md`'s **TESTING REQUESTS** section with its **DO / PASS / WHY**, per that section's rules, and
records the result back here in §9. Entries expected from this pass, in the order they will arrive:

- **A-2** — `Tools/Audio/Audit Catalog` output. ⚠ **A prerequisite, not a verification** — the first thing needed
  from Bob.
- **D-1** — the combat baseline play observation. ⚠ **Blocks all of D-2.**
- **A-1** — suite run (audio + the JsonPolicy grep).
- **A-5** — SFX voice-ceiling play observation.
- **B-2** — a play-test of the first visible engagement.
- **AIR-0** — Khost threat overlay vs the ratified §11.8.2d ladder.
- **AIR-1** — a Khost Su-17 sortie end to end. Also closes the D2 fixed-wing play-verification owed since
  2026-08-11.

---

## 9. PROGRESS LOG

> One line per landed change, newest first. Format: `YYYY-MM-DD — imperative summary (workstream)`.
> This section is the pass record — it is what gets summarized into `Claude_TODO.md`'s pass ledger at close.

- 2026-08-24 — SUPPLY UNIFICATION CLOSED (own file, `Supply Unification.md`): SUP-1 green + play-confirmed,
  SAVE_VERSION 9, one `DaysSupply` pool per unit with fixed-wing Max 0. ⚠ Unblocks AIR-1 — its sortie
  economy now spends a settled model, and the `CanMove` `Max > 0` guard AIR-1 depends on is verified in
  play. A-1 / D-3 / the days ruling all cleared the same day; this pass owes no suite run.
- 2026-08-24 — SUPPLY UNIFICATION ruled by Bob (one number per unit; the round-2 display fix was a working
  band-aid over a wrong model) — surveyed + planned in its OWN file, `Planning Docs/Supply Unification.md`,
  ⏳ awaiting go. ⚠ Affects THIS pass: AIR-1's sortie supply already reads the unified number (airbase
  DaysSupply — unchanged), but SUP-1 should land BEFORE AIR-1 spends supply so the model is settled first.
  Round-2 depot ⚑ came back GREEN before the ruling.
- 2026-08-24 — DEPOT STOCKPILE round 2 (Bob's play catch): the stockpile had NO display surface — the Unit
  Panel printed every unit's 5-day operating DaysSupply, so the model's correct 80 was invisible. Panel now
  branches for depots ("Stockpile: N/Max days (Size)", operating supply deliberately hidden there); NEW
  `CombatUnit.MaxStockpileInDays` display property (single-authority size table; sidesteps the Core-namespace
  GameData trap); `DepotStockpileTests` 5→6. ⚠ Lesson recorded: round 1's play-PASS named a readout that did
  not exist — a PASS criterion must name a surface that exists. ⚑ round-2 suite + Khost look queued.
- 2026-08-24 — DEPOT STOCKPILE follow-on (Bob's ask on the green report): `.oob` depots list their
  stockpile explicitly — optional `StockpileInDays` field (−1/absent = full-for-size default = old behavior;
  0 = deliberately empty) + `CombatUnit.SetStockpile` (model-side clamp, single capacity authority);
  loader warns on clamp and on non-depot authoring; khost.oob lists 7 × 30.0 + 80.0 (behavior-identical);
  NEW `DepotStockpileTests` (5). Editor relay extended with the field. ⚑ suite + Khost look.
- 2026-08-24 — ⚑ CLEARED (Bob ran it): day-one fix batch GREEN, play checks as expected (5/5 · 30/30 · no
  tripwire · settings survive a trailing comma). A-1 `[x]`, D-3 `[x]`, ship-blocker closed.
- 2026-08-24 — `.oob` `DaysSupply` → REAL DAYS (content format; Bob's ruling on finding (a)): loader reads
  days + clamps [0, Max] with warning + per-file ratio-form tripwire (`OOBFileLoader`); `khost.oob`
  re-authored in step (52 units → 5.0, 2 airbases → 30.0 — behavior-identical to the old full-ratio load);
  class doc + field doc updated. `HitPoints` stays a ratio — flagged as a question in the editor relay, not
  changed. ⚠ Editor relay queued in `Claude_TODO.md` Bob's queue, URGENT (Hamburg OOB in authoring). ⚑ suite.
- 2026-08-24 — D-3 done (workstream D): efficiency constants renamed to spell their `EfficiencyLevel` member
  (`_STATIC_OPS/_DEGRADED_OPS/_NORMAL_OPS/_COMBAT_OPS/_FULL_OPS`); verified single consumer; values untouched. ⚑ suite.
- 2026-08-24 — A-1 done (workstream A): NEW `JsonPolicy.Settings` (third named policy) closes the last local
  `JsonSerializerOptions` — AudioSettings now reads AND writes through it (lenient read: a hand-edited trailing
  comma used to throw and silently reset settings); briefing absent-vs-corrupt split per §20.4.2 (`File.Exists`
  first — absent = info no-op, unloadable = real exception). BONUS: `RiverSymmetryVerifier` was silently broken
  against every name-form map since 2026-07-28 (local options, no string-enum converter) — now reads via
  `JsonPolicy.Content`, exactly as `MapLoader` does. ⚑ suite.
- 2026-08-24 — Plan RATIFIED by Bob same day, with riders: ground animations first in B (feel before D4–D6
  rulings); AIR-2's no-AI case is a hard, test-pinned acceptance criterion; findings (a)/(b) ruled fix-now;
  §7 decisions stand as notes, agent proceeds on recommended defaults.
- 2026-08-24 — Plan drafted; no code. Ground truth verified against the codebase (§2), four workstreams scoped,
  order argued (§1.2), the B/C shared-sequencer warning recorded (§1.3), 8 decisions raised for Bob (§7).
  Findings worth keeping even if the plan changes: **(a)** `.oob` `DaysSupply` is a **ratio**, so Khost's
  airbases load at 30/30 and are NOT below the §11.2.3a launch floor — the literal `1` is misleading;
  **(b)** the efficiency constants are named one rung off the enum they serve (`EFFICIENCY_MOD_FULL` serves
  `CombatOperations`, not `FullOperations`) — a free rename, proposed as D-3; **(c)** Khost has **no enemy
  aircraft at all**, which makes ASB ground-strike fully playable and interception structurally untestable until
  Hamburg; **(d)** AIR-2 does **not** require the reaction-yielding turn loop, because in Khost the reaction
  windows belong to an AI with nothing to fly.
