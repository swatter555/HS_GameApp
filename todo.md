# todo.md — PRINTER PASS, slice 1 (P1–P4): the CRT and its navigation

(Prior INTEL PASS slice is landed, green, and Bob-confirmed — record lives in Claude_TODO's change log.)

**Spec:** HS_DesignDoc §24.8 (rewritten 2026-07-25) · **Plan of record:** Claude_TODO "PRINTER PASS" P1–P8
**Scope (Bob, 2026-07-25):** P1–P4 only. P5 (loss ledger) / P6 (loss report) / P7 (emitters) / P8b (tests) are
the following slices — the ledger lands on a CRT Bob has already confirmed works.

**Bob's three calls this session:**
1. The six CRT button callbacks live on `DefaultDialog_Scene1` (the ratified §3.6b HUD rule), each raising an
   EventManager event that `PrinterControl` subscribes to. Bob wires onClick in the Inspector.
2. Date line = month + year from the existing campaign calendar ("TURN 4 — JUNE 1981").
3. This slice is P1–P4.

⚠ **FLAGGED — the ratified frame cannot be built as written.** §24.8.5a specifies `TURN 4 — 14 JUNE 1980`, a
day-level date. `CampaignDateCalendar.GetCurrentDateString()` returns month+year only, is driven by the CAMPAIGN
turn (1981–1989 span) rather than the battle turn, and `ScenarioManifest` has no start-date field, so the day is
not derivable from any current data. Bob's call: month+year for v1. Isolated behind
`PrinterMessage.BuildHeader(turn, dateString)` so a day-level date drops in later without touching an emitter.
DesignDoc §24.8.5a owes an amendment noting the v1 form.

## In this slice

- [ ] **T1 Enums** — `PrinterCategory` { Combat, Intel, Supply, Personnel, General } (message tag) +
      `PrinterFilter` { All, Combat, Intel, Supply, Personnel } (FILTER button cycle state) into GameData.cs.
      Catalogue → category map (§24.8.6): Battle/Objectives/Air → Combat · Intel → Intel · Logistics → Supply ·
      Personnel → Personnel · Weather → General (General visible only under All).
- [ ] **T2 EventManager** — six discrete nav events + `Raise*` + `ClearAllSubscriptions` entries, house style.
- [ ] **T3 `DefaultDialog_Scene1`** — six public `OnPrinter*Button()` callbacks for Bob's Inspector onClick.
      NO `AddListener` for these: Bob owns the onClick, so a code listener as well would double-fire every press.
- [ ] **T4 `PrinterMessage` (P2)** — `Header`/`Source`/`Lines`/`Category`; `BuildHeader(turn, date)` pure +
      testable, `CurrentHeader()` null-tolerant off the live singletons; letterhead constants; `FullText`;
      `FlowIntoColumns` so equipment lists pack to CRT width instead of being truncated. Keep `CreateUnitReport`'s
      rung gating exactly as-is (2026-07-24), re-fronted with the new frame.
- [ ] **T5 `PrinterControl` (P1)** — delete the row pool / greenbar sprites / ScrollRect / `_printDelay`;
      one message, one TMP, typewriter at `Time.deltaTime * _charsPerSecond` (default 120), blink at rest,
      nav-during-typing completes (§24.8.4.2). Font FIXED, auto-size OFF, anchored top-left.
- [ ] **T6 Nav + filter + readout (P4)** — cursor indexes the FILTERED view; `MSG n / N`; clamp on filter change;
      auto-follow a new dispatch only when already on the newest, so the latest-indicator works as the unread flag.
- [ ] **T7 Visibility (P3)** — always-active host + serialized `_panelRoot`; OPEN FROM SCENE START and never
      closes (final model, 2026-07-27); subscribe in `Start()`; drop RPM's `_messagePanelObject`.
- [ ] **T8 Debug harness** — serialized `_debugSeedMessages` toggle enqueuing representative dispatches so Bob can
      exercise nav/filter/typewriter before P7 emitters exist. Off by default.
- [ ] **T9 Docs** — Claude_Project §3.6 + Claude_TODO P1–P4 + change log. Do NOT delete the P1–P8 plan block yet.

## Judgment calls to flag

- **THREE-PANEL MODEL — FINAL FORM (Bob, 2026-07-27).** Terrain, unit and printer CRT are open from scene start
  and never close; right-click CLEARS the terrain and unit panels, the printer keeps its history. Visibility is
  not a behaviour anywhere in the HUD. Two earlier models were tried: hide-on-right-click (mine, wrong —
  inherited from RPM where the message panel tracked the selected hex; the dispatch feed is not contextual, so
  dismissing it stranded the history behind nav buttons inside the hidden root) and open-on-first-hex-click.
- **No enemy leaders, permanently (Bob, 2026-07-25).** DesignDoc §14.2.3 amended; §14.1.1 already said it.
  `Leader.Side` is vestigial — never side-gate on it. The enemy-leader leak I flagged in `ResolveSelection`
  was a non-issue and the comment there is corrected: an enemy unit never reports `IsLeaderAssigned`.
- `Weather` gets its own `General` category rather than being forced into one of the four ratified filters.
  §24.8.4.1 names only All/Combat/Intel/Supply/Personnel, so weather would otherwise be unreachable under any
  filter but All — which is the behaviour I have implemented, just made explicit.

## Review

**T1–T9 done. Compiles clean — `dotnet build Main.csproj` and `EditorTests.csproj`, 0 errors.**
NOT yet play-confirmed: needs Bob's Inspector rewiring first.

Files touched (9):
- `ReactivePanelManager.cs` — three-panel model: one-way `OpenPanels` latch + `ClearSelectionPanels`.
- `Prefab_TerrainPanel.cs` — new `Clear()`; portrait Image disabled on clear, re-enabled on update.
- `Prefab_UnitPanel.cs` — new `Clear()`.
- `GameData.cs` — new `PrinterCategory` + `PrinterFilter`.
- `EventManager.cs` — six printer nav events + raisers + `ClearAllSubscriptions` entries.
- `DefaultDialog_Scene1.cs` — six public `OnPrinter*Button()` callbacks.
- `PrinterMessage.cs` — rebuilt to the §24.8.5a frame; `HeaderProvider` seam; `FlowIntoColumns`;
  `CreateUnitReport` re-fronted with its rung gating intact; seven dead ad-hoc factories deleted.
- `PrinterControl.cs` — rewritten as the one-message CRT.

**Two traps worth remembering:**
- `BattleManager.Instance` and `GameDataManager.Instance` LAZY-CREATE a GameObject. A plain data class reading
  them spawns managers out of headless tests, so the header goes through `PrinterMessage.HeaderProvider`.
- Hiding the printer by SetActive on its own GameObject would unsubscribe the component that receives the
  message that shows it again. Hence always-active host + `_panelRoot`, plus a startup warning if the two are
  the same object.

**Slice 2 (2026-07-26) — combat dispatches + the Verbose switch.** New `PrinterDispatch` static owning the
§24.8.6 text and the three-gate volume model; `Verbose` serialized on PrinterControl (ON = narrate everything,
OFF = report by exception). Frame revised to `12: Message from 3rd Tank Rgt` with render-time name
abbreviation — which retired the day-level-date debt, since no date is rendered at all now. `LossBand` +
thresholds. Ground and indirect combat wired through `MovementController.TryAttack`, both sides in one call.
`_fontSize` added and a 9-line calibration seed. Compiles clean.

**Slice 3 (2026-07-26) — remaining emitters.** Ambush (both directions), objectives captured/lost, unit
hardened, weather, first contact + intel rungs. `PrinterDispatch.Attach()/Detach()` for the broadcast triggers.
Three guards worth remembering: promotion detected by caller-side comparison (CombatUnit raises no events and
must not start); first contact suppressed at turn 0 (scenario load runs a full spotting sweep); weather text
truncated because its ratified sentences claim mechanics that do not exist. Compiles clean, Main + EditorTests.

**Slice 4 (2026-07-27) — all three panels open from scene start.** The open-on-first-click latch is removed;
terrain and unit come up active-and-cleared, the CRT comes up showing the empty placeholder. Visibility is no
longer a behaviour anywhere in the HUD. Takes the lazy-singleton hazard with it — that only mattered while the
panels started inactive. Third and final model: hide-on-right-click → open-on-first-click → open-always.

**✅ DOC AMENDMENTS LANDED 2026-07-27 — HS_DesignDoc is reconciled with the code.**
- **§12.5.3** rewritten: unit panel shows BOTH sides, enemy filtered by rung, same layout. +12.5.3.1 the
  Level0 display gate, +12.5.3.2 the printer no longer carries the enemy selection readout.
- **§24.5a** re-homed: rung gates now govern the panel AND the intel dispatch, not a per-selection printout.
- **§24.8.2** sharpened to the subordinates/three-gates model, +24.8.2.3 the combat receipts carve-out,
  +24.8.2.4 report-by-exception, +24.8.2.5 verbose mode.
- **§24.8.4.3** NEW: panel visibility for the whole HUD, incl. the two rejected models so neither returns.
- **§24.8.5a** reframed to `12: Message from X`; +.1 the date removal (retires the day-date conflict),
  +.2 name abbreviation.
- **§24.8.6 Battle** rewritten to the two templates + special cases; NEW §24.8.6.1 loss bands (3/6/12/24),
  §24.8.6.2 intel verbose-only, §24.8.6.3 turn-0 suppression, §24.8.6.4 weather text held back.
- **§14.2.3** (earlier this thread) no enemy leaders, permanent.

**Design deviations, all deliberate and flagged:**
- Date is month+year, not the doc's day-level example (Bob's call; not derivable from current data).
  DesignDoc §24.8.5a owes an amendment.
- Panels start closed, open together on the first hex selection, never close; deselect clears terrain+unit
  content but not the printer (Bob's call after the first play-test). The DesignDoc is silent on panel
  visibility and owes a line recording the model.
- `Weather` sits in a `General` category, reachable only under the All filter — §24.8.4.1 ratifies four filter
  values and weather is not among them.
- CLEAR leaves the panel up showing an empty placeholder rather than hiding it: the player pressed a button and
  should see its result.

**Owed by Bob:** Inspector rewiring (always-active host, `_panelRoot`, `_messageText` with auto-size off and
sized for 9 lines, optional readout/filter/indicator refs, six Button onClicks) then a play-test.
`_debugSeedMessages` exercises the CRT before P7 emitters exist.

**Next slices:** P5 loss ledger (+ `SAVE_VERSION` bump) → P6 loss report → P7 emitters → P8b tests.
