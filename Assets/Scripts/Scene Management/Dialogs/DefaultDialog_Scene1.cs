using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HammerAndSickle.SceneManagement
{
    /// <summary>
    /// Default (home) dialog for Scene 1 (Battle).
    /// Always visible — represents the battle HUD. When focused, map input is enabled.
    /// When an overlay opens, this dialog loses focus and map input is disabled.
    ///
    /// Also acts as the click-through controller: holds references to HUD panel
    /// RectTransforms and provides hit-testing so InputService_BattleMap can determine
    /// whether a click lands on a UI panel (and should be blocked from hex selection).
    ///
    /// Implements its own singleton pattern (rather than extending Singleton&lt;T&gt;)
    /// because it must extend UIPanel for the dialog flow system's Show/Hide/SetFocus.
    /// </summary>
    public class DefaultDialog_Scene1 : UIPanel
    {
        private const string CLASS_NAME = nameof(DefaultDialog_Scene1);

        #region Singleton

        // Manual singleton — can't use Singleton<T> base class because we need UIPanel
        // as the base for Show/Hide/SetFocus in the dialog flow system.
        public static DefaultDialog_Scene1 Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion // Singleton

        #region Serialized Fields

        [Header("Canvas Camera (required for Screen Space - Camera)")]
        [SerializeField] private Camera _uiCamera;

        [Header("Click-Through Panels")]
        [SerializeField] private RectTransform _topMenuBar;
        [SerializeField] private RectTransform _terrainPanel;

        // ONE unit panel since the consolidation (§4.3) — the separate ground/air panels and the leader
        // panel are gone. FormerlySerializedAs migrates a live _unitGroundPanel binding rather than
        // silently dropping it. Slots left null fail OPEN (see WarnOnUnassignedPanels).
        [FormerlySerializedAs("_unitGroundPanel")]
        [SerializeField] private RectTransform _unitPanel;
        [SerializeField] private RectTransform _printerPanel;

        // The two order panels (added 2026-07-28 with the HUD button pass). UnitOps carries the
        // per-unit orders (deploy, resupply, intel, undo…), BattleOps the battle-wide ones (end turn,
        // requisition, screens). Both MUST be listed here or clicks pass straight through them to the
        // map — and a right-click that reaches the map is a MOVE ORDER issued under the panel the
        // player thought they were pressing.
        [SerializeField] private RectTransform _unitOpsPanel;
        [SerializeField] private RectTransform _battleOpsPanel;

        [Header("Debug")]
        [SerializeField] private bool _debug;

        #endregion // Serialized Fields

        #region Fields

        // Cached array of all HUD panels that should block map clicks.
        // Built once in Start() from the serialized references.
        private RectTransform[] _panels;

        #endregion // Fields

        #region Unity Lifecycle

        private void Start()
        {
            // Build the panel array once for efficient iteration during hit-testing.
            _panels = new[]
            {
                _topMenuBar,
                _terrainPanel,
                _unitPanel,
                _printerPanel,
                _unitOpsPanel,
                _battleOpsPanel
            };

            WarnOnUnassignedPanels();
        }

        /// <summary>
        /// Warns for every unassigned click-through slot. <see cref="IsScreenPointOverUI"/> skips nulls, so a
        /// missing reference FAILS OPEN — the click reaches the map, and for right-click that is a move order
        /// issued under the panel the player thought they were clicking. Silent for four days once already.
        /// </summary>
        private void WarnOnUnassignedPanels()
        {
            (string name, RectTransform rect)[] slots =
            {
                (nameof(_topMenuBar), _topMenuBar),
                (nameof(_terrainPanel), _terrainPanel),
                (nameof(_unitPanel), _unitPanel),
                (nameof(_printerPanel), _printerPanel),
                (nameof(_unitOpsPanel), _unitOpsPanel),
                (nameof(_battleOpsPanel), _battleOpsPanel)
            };

            foreach ((string name, RectTransform rect) in slots)
            {
                if (rect == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: click-through slot '{name}' is UNASSIGNED — clicks over that " +
                        "panel will reach the map (right-click there = move order).");
                }
            }

            // Screen Space - Camera canvases need the camera for the rect test; without it every panel
            // mis-tests and the whole HUD leaks clicks, not just one slot.
            if (_uiCamera == null)
            {
                Debug.LogWarning($"{CLASS_NAME}: _uiCamera is UNASSIGNED — click-through hit-testing will be " +
                    "wrong for any Screen Space - Camera canvas.");
            }
        }

        #endregion // Unity Lifecycle

        #region UIPanel Overrides

        /// <summary>
        /// Enables or disables map input based on whether the HUD has focus.
        /// When an overlay dialog is open, focus is removed and all map
        /// scrolling, clicking, and zooming ceases.
        /// </summary>
        protected override void OnFocusChanged(bool hasFocus)
        {
            // Gate all map input through the InputService.
            // hasFocus == true:  overlay closed, map becomes interactive
            // hasFocus == false: overlay open, map input disabled and state reset
            InputService_BattleMap.Instance.SetInputEnabled(hasFocus);
        }

        #endregion // UIPanel Overrides

        #region Click-Through Detection

        /// <summary>
        /// Returns true if the screen position is inside any registered HUD panel.
        /// Called by InputService_BattleMap during mouse click processing to decide
        /// whether the click should be consumed by the UI or passed through to hex selection.
        /// </summary>
        public bool IsScreenPointOverUI(Vector2 screenPoint)
        {
            try
            {
                foreach (RectTransform panel in _panels)
                {
                    if (panel == null) continue;

                    bool isActive = panel.gameObject.activeInHierarchy;
                    bool containsPoint = isActive
                        && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPoint, _uiCamera);

                    if (_debug && isActive)
                    {
                        Debug.Log($"{CLASS_NAME}: Panel={panel.name}, Active={isActive}, " +
                            $"Contains={containsPoint}, ScreenPoint={screenPoint}, " +
                            $"PanelRect={panel.rect}, PanelPos={panel.position}");
                    }

                    if (containsPoint)
                        return true;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsScreenPointOverUI), e);
            }

            return false;
        }

        #endregion // Click-Through Detection

        #region Unit Cycling Button Callbacks

        /// <summary>Selects the next eligible unit. See EventManager.</summary>
        public void OnNextUnitButton() => EventManager.Instance?.RaiseNextUnitRequested();

        /// <summary>Selects the previous eligible unit. See EventManager.</summary>
        public void OnPreviousUnitButton() => EventManager.Instance?.RaisePreviousUnitRequested();

        #endregion // Unit Cycling Button Callbacks

        #region Printer Button Callbacks

        // ----------------------------------------------------------------------------
        // The six on-CRT navigation buttons of the HQ dispatch feed (§24.8.4.1).
        //
        // Inspector-wired like every other button since 2026-07-27 (§3.6b): Bob assigns
        // each Button's onClick to the matching method here. They must NOT also be hooked
        // with AddListener — a code listener on top of the Inspector wiring would fire
        // every press twice.
        //
        // Each callback does exactly one thing: raise its event. PrinterControl owns all
        // history/cursor/filter state and subscribes to these. See EventManager.
        // ----------------------------------------------------------------------------

        /// <summary>Jumps the dispatch feed to the oldest message in the current filter.</summary>
        public void OnPrinterOldestButton() => EventManager.Instance?.RaisePrinterOldestRequested();

        /// <summary>Steps the dispatch feed back one message.</summary>
        public void OnPrinterPreviousButton() => EventManager.Instance?.RaisePrinterPreviousRequested();

        /// <summary>Steps the dispatch feed forward one message.</summary>
        public void OnPrinterNextButton() => EventManager.Instance?.RaisePrinterNextRequested();

        /// <summary>Jumps the dispatch feed to the newest message.</summary>
        public void OnPrinterLatestButton() => EventManager.Instance?.RaisePrinterLatestRequested();

        /// <summary>Advances the feed filter: All → Combat → Intel → Supply → Personnel → All.</summary>
        public void OnPrinterFilterButton() => EventManager.Instance?.RaisePrinterFilterCycleRequested();

        /// <summary>Purges the dispatch history.</summary>
        public void OnPrinterClearButton() => EventManager.Instance?.RaisePrinterClearRequested();

        #endregion // Printer Button Callbacks

        // ════════════════════════════════════════════════════════════════════════════════════════════
        // BATTLE HUD BUTTON CALLBACKS — STUBBED 2026-07-28 for Bob's wiring pass.
        //
        // ⚠ EVERY METHOD NAME BELOW IS A PUBLIC CONTRACT. A UnityEvent binds by method-name STRING, so
        // renaming one silently breaks the scene wiring with NO compile error (CLAUDE.md §2.13). Once Bob
        // has wired these, they do not get renamed without telling him.
        //
        // ⚠ THESE ARE DELIBERATELY STUBS, and they deliberately REPORT rather than doing nothing. A
        // silent no-op stub is untestable: after wiring twenty buttons there would be no way to tell a
        // correct binding from a missed one, and a missed one only surfaces much later as "that button
        // does nothing". Each stub announces itself to the Console and the UI message log, so Bob's
        // wiring pass is self-verifying — click it, see it name itself, move on.
        //
        // ⚠ NOTHING HERE RAISES ITS REAL EVENT YET. Most of the matching EventManager events already
        // exist (named per method below) but have ZERO subscribers, so raising them today would be an
        // elaborate no-op that merely LOOKS implemented — which is the failure mode this codebase has
        // twice cleaned up (§7.1). Each becomes real by replacing its Report(...) line with the raise,
        // at the same time as the subscriber that services it.
        //
        // NOT HERE, ON PURPOSE:
        //   • "Next turn" → BattleManager.OnEndTurnButton(), already fully implemented. Wire the button
        //     to BattleManager, not to this class. A second callback would double-fire the turn sequence.
        //   • "Next unit" / "Previous unit" → already live above, and their events have subscribers.
        // ════════════════════════════════════════════════════════════════════════════════════════════

        #region Unit Order Button Callbacks

        /// <summary>
        /// Moves the selected unit one step toward Embarked (Fortified → Entrenched → Hasty → Deployed →
        /// Mobile → Embarked). LIVE as of 2026-07-28 — `MovementController` services the event.
        /// </summary>
        public void OnDeployUpButton() => RequestDeploymentChange(deployUp: true);

        /// <summary>
        /// Moves the selected unit one step toward Fortified (dug in). LIVE as of 2026-07-28.
        /// </summary>
        public void OnDeployDownButton() => RequestDeploymentChange(deployUp: false);

        private void RequestDeploymentChange(bool deployUp)
        {
            CombatUnit unit = GameDataManager.SelectedUnit;

            if (unit == null)
            {
                AppService.CaptureUiMessage("No unit selected.");
                return;
            }

            // See EventManager: MovementController validates against the map and applies the §8.2 costs.
            if (deployUp)
                EventManager.Instance?.RaiseDeployUpRequested(unit);
            else
                EventManager.Instance?.RaiseDeployDownRequested(unit);
        }

        /// <summary>Resupplies the selected unit. Will raise EventManager.RaiseResupplyRequested(unit, includeReplacements: false).</summary>
        public void OnResupplyUnitButton() => ReportForSelectedUnit("Resupply unit");

        /// <summary>Buys replacements for the selected unit's losses. Will raise EventManager.RaiseReplaceLossesRequested(unit).</summary>
        public void OnReplacementsButton() => ReportForSelectedUnit("Replacements");

        /// <summary>Spends the unit's IntelAction (§8.2.4). Will raise EventManager.RaiseIntelActionRequested(unit)
        /// — ⚠ this is the ONE unit-order event that already has a subscriber, so wire it last and expect
        /// real behaviour the moment the stub is replaced.</summary>
        public void OnGatherIntelButton() => ReportForSelectedUnit("Gather intel");

        /// <summary>Reverts the selected unit's last move. Will raise EventManager.RaiseMoveUndoRequested(unit).</summary>
        public void OnUndoButton() => ReportForSelectedUnit("Undo move");

        #endregion // Unit Order Button Callbacks

        #region Information Button Callbacks

        /// <summary>Opens the unit details modal. Will raise EventManager.RaiseUnitDetailsRequested(unit).</summary>
        public void OnUnitDetailsButton() => ReportForSelectedUnit("Unit details");

        /// <summary>
        /// Opens the leader details modal for the selected unit's leader (the future modal that replaced the
        /// leader panel removed 2026-07-23, §4.3).
        /// ⚠ NO EVENT EXISTS FOR THIS YET. `RaiseLeaderPoolRequested()` is NOT it — that is the pool of
        /// unassigned leaders, a different screen. A `LeaderDetailsRequested(Leader)` event gets added when
        /// the modal is designed, rather than being guessed at now.
        /// </summary>
        public void OnLeaderDetailsButton()
        {
            Leader leader = GameDataManager.SelectedLeader;

            if (leader == null)
            {
                Report(GameDataManager.SelectedUnit == null
                    ? "Leader details: no unit selected."
                    : "Leader details: selected unit has no leader assigned.");
                return;
            }

            Report($"Leader details: {leader.Name} — not implemented yet.");
        }

        #endregion // Information Button Callbacks

        #region Overlay Toggle Button Callbacks

        /// <summary>Toggles the supply overlay. Will raise EventManager.RaiseSupplyOverlayToggled(visible).
        /// ⚠ Whoever OWNS the overlay owns its visible/hidden state — this button only asks for a toggle,
        /// so no state is tracked here.</summary>
        public void OnShowSupplyButton() => Report("Show supply overlay");

        /// <summary>Toggles the terrain overlay.
        /// ⚠ No EventManager event exists for this yet (the nearest, `RaiseUnitIconsToggled`, is a different
        /// overlay). It gets added with the overlay itself.</summary>
        public void OnShowTerrainButton() => Report("Show terrain overlay");

        #endregion // Overlay Toggle Button Callbacks

        #region Screen and Navigation Button Callbacks

        /// <summary>Opens the requisition screen (§18.9). Will raise EventManager.RaiseRequisitionPanelRequested().</summary>
        public void OnRequisitionScreenButton() => Report("Requisition screen");

        /// <summary>
        /// Prints the CUMULATIVE loss report to the HQ dispatch feed (§24.8 / printer P6) — live as of
        /// 2026-07-28, reading the P5 equipment ledger.
        ///
        /// TWO-BUTTON MODEL, ratified 2026-08-20 (Bob): this and <see cref="OnDisplayDailyLossesButton"/>
        /// each get their OWN Inspector-wired button — cumulative and per-turn are different questions
        /// asked at different moments, and a cycle toggle would hide the second behind an undiscoverable
        /// double-press. The report is built here and raised directly as a printer message; the old
        /// RaiseDailyLossesRequested/RaiseTotalLossesRequested events were deleted with the decision.
        /// </summary>
        public void OnDisplayLossesButton() => PrintLossReport(dailyOnly: false);

        /// <summary>
        /// Prints losses for the CURRENT TURN ONLY to the HQ dispatch feed (§24.8 / printer P6).
        ///
        /// The daily ledger is a second accumulator fed by the same booking as the cumulative one and
        /// reset by `BattleManager.SetTurn`, so "this turn" always means the turn shown on the HUD.
        /// ⚠ Distinct button from <see cref="OnDisplayLossesButton"/> — wire them to different buttons.
        /// </summary>
        public void OnDisplayDailyLossesButton() => PrintLossReport(dailyOnly: true);

        private void PrintLossReport(bool dailyOnly)
        {
            try
            {
                PrinterMessage report = dailyOnly
                    ? PrinterMessage.CreateLossReport(
                        GameDataManager.GetDailyLossLedger(Side.Player),
                        GameDataManager.GetDailyLossLedger(Side.AI),
                        dailyOnly: true)
                    : PrinterMessage.CreateLossReport(
                        GameDataManager.GetLossLedger(Side.Player),
                        GameDataManager.GetLossLedger(Side.AI));

                // See EventManager: PrinterControl owns the feed and renders whatever is raised.
                EventManager.Instance?.RaisePrinterMessage(report);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PrintLossReport), e);
            }
        }

        /// <summary>
        /// Reopens the scenario briefing overlay (`OrdersDialog_Scene1`, the same panel shown at battle start).
        /// ⚠ Becomes real via EventManager.RaiseScene1DialogRequested(ordersDialog) — which needs a serialized
        /// UIPanel reference to that dialog. Left unadded so Bob has no extra Inspector slot to fill during a
        /// wiring pass; it goes in when the button is made real.
        /// </summary>
        public void OnBriefingScreenButton() => Report("Briefing screen");

        /// <summary>Opens the strategic/theatre view. ⚠ No event and no screen exist yet.</summary>
        public void OnStrategicViewButton() => Report("Strategic view");

        /// <summary>Opens the unit database browser. ⚠ No event and no screen exist yet.</summary>
        public void OnUnitDBButton() => Report("Unit database");

        /// <summary>Opens the in-battle options screen. ⚠ No event and no screen exist yet.</summary>
        public void OnOptionsButton() => Report("Options screen");

        /// <summary>
        /// Leaves the battle and returns to the main menu.
        /// ⚠ DO NOT make this a bare `SceneManager.LoadScene(SceneID.MainMenu)` when it is implemented. It
        /// abandons a battle in progress, so it needs a confirmation prompt first, and — once saving exists —
        /// a decision about whether the battle is saved, discarded, or offered as a choice. That is why it is
        /// a stub rather than the two-line version it superficially looks like.
        /// </summary>
        public void OnMainMenuButton() => Report("Main menu (needs confirm-and-discard handling)");

        #endregion // Screen and Navigation Button Callbacks

        #region Stub Reporting

        /// <summary>
        /// Announces an unimplemented button to the Console AND the UI message log.
        ///
        /// Two channels on purpose: the Console is what Bob watches while wiring, and
        /// <see cref="AppService.CaptureUiMessage"/> is what the automated/UI-facing side reads, so a stub
        /// press is visible whichever way the scene is being exercised.
        /// </summary>
        private void Report(string label)
        {
            string message = $"{label}: not implemented yet.";

            Debug.Log($"{CLASS_NAME}: {message}");
            AppService.CaptureUiMessage(message);
        }

        /// <summary>
        /// Stub reporter for the buttons that act on the SELECTED UNIT.
        ///
        /// It names the unit, which makes each press verify two things at once: that the button is wired,
        /// and that selection is reaching this class. "No unit selected" is reported as its own case rather
        /// than being silently ignored — when these become real, refusing an order with no unit selected is
        /// the correct behaviour, so the shape is already right.
        /// </summary>
        private void ReportForSelectedUnit(string label)
        {
            CombatUnit unit = GameDataManager.SelectedUnit;

            if (unit == null)
            {
                Report($"{label}: no unit selected");
                return;
            }

            Report($"{label}: {unit.UnitName}");
        }

        #endregion // Stub Reporting
    }
}
