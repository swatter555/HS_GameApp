using HammerAndSickle.Audio;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Renderers;
using HammerAndSickle.SceneManagement;
using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using SFX = HammerAndSickle.Controllers.GameAudioManager.SoundEffect;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Movement state machine for player-controlled unit movement during BattlePhase.PlayerTurn.
    /// (AwaitingTarget removed 2026-07-06 — §5.10.4 has no order-confirmation step: right-click moves immediately.)
    /// </summary>
    public enum MovementState
    {
        Idle,
        UnitSelected,
        Executing
    }

    /// <summary>
    /// Controls player-side unit movement: selection, pathfinding, execution, spotting,
    /// ZoC handling, ambush resolution, and next/previous unit cycling.
    /// </summary>
    public class MovementController : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(MovementController);

        #region Singleton

        private static MovementController _instance;

        public static MovementController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<MovementController>();
                    if (_instance == null)
                    {
                        GameObject go = new("MovementController");
                        _instance = go.AddComponent<MovementController>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Fields

        private MovementRangeResult _currentRange;
        private List<HexTile> _currentPath;
        private BattlePhase _currentPhase = BattlePhase.NotStarted;
        private HashSet<Position2D> _enemyZocSet;

        // Unit cycling (Task 7)
        private List<CombatUnit> _eligibleUnits = new();
        private int _cycleIndex = -1;

        // Hover path preview (§5.10.3) — last hex the pointer was over; NoHexSelected = no preview showing.
        private Position2D _lastHoverHex = GameDataManager.NoHexSelected;

        #endregion // Fields

        #region Properties

        public CombatUnit CurrentUnit { get; private set; }
        public MovementState State { get; private set; } = MovementState.Idle;

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Subscribe in Start, NOT OnEnable: HexDetectionService (SEO 150) and InputService (SEO 140)
        // set their Instances in Awake AFTER every default-order script's Awake/OnEnable pair has run,
        // so an OnEnable subscribe here deterministically finds them null. Start is guaranteed to run
        // after ALL Awake/OnEnable regardless of Script Execution Order.
        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdatePathPreviewHover();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (_instance == this) _instance = null;
        }

        #endregion // Unity Lifecycle

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            if (HexDetectionService.Instance != null)
            {
                HexDetectionService.Instance.OnHexSelected += HandleHexSelected;
                HexDetectionService.Instance.OnHexRightClicked += HandleHexRightClicked;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnBattlePhaseChanged += HandlePhaseChanged;
                EventManager.Instance.OnNextUnitRequested += CycleNext;
                EventManager.Instance.OnPreviousUnitRequested += CyclePrevious;
                EventManager.Instance.OnUnitMoveCompleted += HandleMoveCompleted;
                EventManager.Instance.OnIntelActionRequested += HandleIntelActionRequested;
                EventManager.Instance.OnDeployUpRequested += HandleDeployUpRequested;
                EventManager.Instance.OnDeployDownRequested += HandleDeployDownRequested;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (HexDetectionService.Instance != null)
            {
                HexDetectionService.Instance.OnHexSelected -= HandleHexSelected;
                HexDetectionService.Instance.OnHexRightClicked -= HandleHexRightClicked;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnBattlePhaseChanged -= HandlePhaseChanged;
                EventManager.Instance.OnNextUnitRequested -= CycleNext;
                EventManager.Instance.OnPreviousUnitRequested -= CyclePrevious;
                EventManager.Instance.OnUnitMoveCompleted -= HandleMoveCompleted;
                EventManager.Instance.OnIntelActionRequested -= HandleIntelActionRequested;
                EventManager.Instance.OnDeployUpRequested -= HandleDeployUpRequested;
                EventManager.Instance.OnDeployDownRequested -= HandleDeployDownRequested;
            }
        }

        #endregion // Event Subscriptions

        #region Deployment Actions

        // ────────────────────────────────────────────────────────────────────────────────────────────
        // Deploy up / down (§8.2 action economy). The MODEL owns the rules — CombatUnit.TryDeployUP /
        // TryDeployDOWN do all validation and cost application — so these handlers only supply the
        // pieces of MAP context the model cannot see (airbase adjacency, port hex, beachhead hex),
        // enforce turn/side ownership, and publish the result.
        //
        // ⚠ THE RAISE LIVES HERE, NOT IN CombatUnit. No class under Models/ raises events, and
        // EventManager.Instance LAZY-CREATES a GameObject — a raise from the model would spawn an
        // EventManager in every headless EditorTest that changes deployment. `?.` does NOT help: the
        // getter creates the object and never returns null.
        // ────────────────────────────────────────────────────────────────────────────────────────────

        private void HandleDeployUpRequested(CombatUnit unit) => TryChangeDeployment(unit, deployUp: true);

        private void HandleDeployDownRequested(CombatUnit unit) => TryChangeDeployment(unit, deployUp: false);

        private void TryChangeDeployment(CombatUnit unit, bool deployUp)
        {
            try
            {
                if (unit == null) return;
                if (_currentPhase != BattlePhase.PlayerTurn) return;
                if (unit.Side != Side.Player) return;

                string error;
                bool changed = deployUp
                    ? unit.TryDeployUP(out error, IsAdjacentToActiveFriendlyAirbase(unit), IsOnPortHex(unit))
                    : unit.TryDeployDOWN(out error, IsOnPortHex(unit), IsOnBeachheadHex(unit));

                if (!changed)
                {
                    // ⚠ REFUSALS ARE NOT PRINTER DISPATCHES (§24.8.5) — a denial is feedback about the
                    // player's own order, not a report of something they could not see. It belongs in the
                    // UI message channel (and eventually a denial SFX), never in the HQ dispatch feed.
                    AppService.CaptureUiMessage(string.IsNullOrWhiteSpace(error)
                        ? $"{unit.UnitName} cannot change deployment right now."
                        : error);
                    GameAudio.Play(SFX.ButtonDenied);
                    return;
                }

                if (EventManager.Instance != null)
                {
                    EventManager.Instance.RaiseUnitActionsChanged(unit);
                    EventManager.Instance.RaiseUnitMovementPointsChanged(unit);

                    // ⚠ A FULL REDRAW, NOT RaiseUnitDeploymentChanged. That event only refreshes the deploy
                    // BADGE (Prefab_CombatUnitIcon.RefreshDeployIcon), but a deployment change also swaps
                    // the unit's MAIN ART — GameIconRenderer resolves it via
                    // EquipmentBays.GetIcon(DeploymentPosition, facing), so Mobile and Deployed are
                    // different sprites. Refreshing only the badge would leave a mounted unit drawn as
                    // infantry. The redraw rebuilds icons from live unit state and covers both.
                    EventManager.Instance.RaiseRedrawMapIcons();
                }

                // Deployment spends MP and changes the movement profile's max, so the range overlay for the
                // selected unit is stale the moment the transition lands.
                if (CurrentUnit == unit) RecomputeRangeAndRaise(GameDataManager.CurrentHexMap);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TryChangeDeployment), e);
            }
        }

        /// <summary>
        /// True when the unit stands on a port hex (§5.4.2 sealift embarkation).
        /// </summary>
        private static bool IsOnPortHex(CombatUnit unit)
        {
            HexMap map = GameDataManager.CurrentHexMap;
            if (map == null) return false;

            return map.GetHexAt(unit.MapPos)?.IsPort ?? false;
        }

        /// <summary>
        /// True when the unit stands on a beachhead hex — the §9.10.6.1 marine debark site.
        /// </summary>
        private static bool IsOnBeachheadHex(CombatUnit unit)
        {
            HexMap map = GameDataManager.CurrentHexMap;
            if (map == null) return false;

            return map.GetHexAt(unit.MapPos)?.IsBeachhead ?? false;
        }

        /// <summary>
        /// True when the unit is adjacent to an ACTIVE friendly airbase — the §21.3.1 condition that lets
        /// AB/MAB (and SPECF with air transport) skip Mobile and embark directly.
        ///
        /// ⚠ "Active" is checked, not merely "present": an airbase that has been bombed
        /// <see cref="OperationalCapacity.OutOfOperation"/> cannot mount an airborne operation, and a
        /// destroyed one certainly cannot. Presence alone would let a wrecked airfield launch paratroopers.
        /// </summary>
        private static bool IsAdjacentToActiveFriendlyAirbase(CombatUnit unit)
        {
            HexMap map = GameDataManager.CurrentHexMap;
            if (map == null) return false;

            HexTile tile = map.GetHexAt(unit.MapPos);
            if (tile == null) return false;

            foreach (var neighbor in tile.GetAllNeighbors())
            {
                if (neighbor.Value == null) continue;

                foreach (CombatUnit occupant in GameDataManager.Instance.GetUnitsAtHex(neighbor.Value.Position))
                {
                    if (occupant == null || occupant.Side != unit.Side) continue;
                    if (!occupant.IsBase || occupant.FacilityType != FacilityType.Airbase) continue;
                    if (occupant.IsDestroyed()) continue;
                    if (occupant.OperationalCapacity == OperationalCapacity.OutOfOperation) continue;

                    return true;
                }
            }

            return false;
        }

        #endregion // Deployment Actions

        #region Intel Action

        /// <summary>
        /// Handles a GatherIntel request (§12.4.5): spends the unit's IntelAction, then raises every ADJACENT
        /// enemy one rung (ceiling Level 5). This is the only route to Level 5 and the deliberate alternative
        /// to attacking — three IntelActions walk a contact to a full picture without firing a shot.
        ///
        /// Player turn only, and the action is spent BEFORE the intel is applied so a unit that cannot pay
        /// (no IntelAction, or below the §8.2.4 MP/supply floor) learns nothing.
        /// </summary>
        private void HandleIntelActionRequested(CombatUnit unit)
        {
            try
            {
                if (unit == null) return;
                if (_currentPhase != BattlePhase.PlayerTurn) return;
                if (unit.Side != Side.Player) return;

                if (!unit.PerformIntelAction())
                {
                    AppService.CaptureUiMessage($"{unit.UnitName} cannot gather intel right now.");
                    GameAudio.Play(SFX.ButtonDenied);
                    return;
                }

                SpottingService.ApplyGroundIntelAction(unit);

                if (EventManager.Instance != null)
                {
                    EventManager.Instance.RaiseUnitActionsChanged(unit);
                    EventManager.Instance.RaiseUnitMovementPointsChanged(unit);
                    EventManager.Instance.RaiseRedrawMapIcons();
                }

                // Intel spends MP, so the movement overlay must be re-derived for the selected unit.
                if (CurrentUnit == unit) RecomputeRangeAndRaise(GameDataManager.CurrentHexMap);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleIntelActionRequested), e);
            }
        }

        #endregion // Intel Action

        #region Phase Handling

        private void HandlePhaseChanged(BattlePhase newPhase)
        {
            _currentPhase = newPhase;

            if (newPhase == BattlePhase.PlayerTurn)
            {
                GameDataManager.Instance.BuildOccupancyCache();
                BuildEligibleUnitsList();
            }
            else
            {
                DeselectUnit();
            }
        }

        #endregion // Phase Handling

        #region Selection Flow

        private void HandleHexSelected(Position2D hexPos)
        {
            try
            {
                if (_currentPhase != BattlePhase.PlayerTurn) return;
                if (State == MovementState.Executing) return;

                var gdm = GameDataManager.Instance;
                var map = GameDataManager.CurrentHexMap;
                if (map == null) return;

                // Modifier family (§5.10.6): Shift+click = facing, Ctrl+click = engage.
                // Input System API — the project runs Input System-only; legacy UnityEngine.Input throws.
                var kb = Keyboard.current;
                bool shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
                bool ctrl = kb != null && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);

                if (State == MovementState.UnitSelected && CurrentUnit != null && shift)
                {
                    HandleFacingRotation(hexPos);
                    return;
                }

                if (ctrl)
                {
                    HandleCtrlClick(hexPos);
                    return;
                }

                switch (State)
                {
                    case MovementState.Idle:
                        TrySelectUnit(hexPos);
                        break;

                    case MovementState.UnitSelected:
                        HandleUnitSelectedClick(hexPos);
                        break;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleHexSelected), e);
            }
        }

        private void TrySelectUnit(Position2D hexPos)
        {
            var gdm = GameDataManager.Instance;
            var ground = gdm.GetGroundUnitAtHex(hexPos);
            var air = gdm.GetAirUnitAtHex(hexPos);
            var unit = ground ?? air;

            if (unit == null || unit.Side != Side.Player) return;

            SelectUnit(unit);
        }

        private void SelectUnit(CombatUnit unit)
        {
            try
            {
                CurrentUnit = unit;
                var map = GameDataManager.CurrentHexMap;

                State = MovementState.UnitSelected;

                // Ungated: this path only ever selects the player's own units, and the sound is a response
                // to the click rather than information about anything on the map.
                GameAudio.Play(SFX.UnitSelect);

                EventManager.Instance?.RaisePlayerUnitSelected(unit);

                // Empty range for a unit with no move left (spent actions/MP, dug-in posture, base) — no
                // overlay, no hover preview, and right-click can never match a reachable hex (Bob 2026-07-21).
                RecomputeRangeAndRaise(map);

                CameraService.Instance?.CenterOnPosition(unit.MapPos);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SelectUnit), e);
            }
        }

        /// <summary>
        /// Recomputes the movement range for the selected unit and raises the matching overlay event.
        /// A unit that can no longer BEGIN a move (spent MoveActions/MP, dug-in, base) gets an EMPTY range —
        /// overlay and hover preview stay dark and right-click can never match. NOT used by the mid-move
        /// per-hex recompute: the MoveAction is already spent there, and its ZocTerminals drive the halt rule.
        /// </summary>
        private void RecomputeRangeAndRaise(HexMap map)
        {
            if (CurrentUnit == null || map == null) return;

            _currentRange = CurrentUnit.CanBeginMoveOrder()
                ? HexMapUtil.GetValidMoveDestinations(map, CurrentUnit)
                : new MovementRangeResult
                {
                    Reachable = new Dictionary<Position2D, int>(),
                    ZocTerminals = new HashSet<Position2D>()
                };

            if (EventManager.Instance == null) return;
            if (_currentRange.Reachable.Count > 0)
                EventManager.Instance.RaiseMovementRangeComputed(CurrentUnit, _currentRange.Reachable, _currentRange.ZocTerminals);
            else
                EventManager.Instance.RaiseMovementRangeCleared();
        }

        /// <summary>
        /// Plain left-click with a unit selected — UNIVERSAL SELECTION (§5.10.6): the click selects whatever is
        /// under the cursor, never moves and never attacks. Friendly unit → re-select; enemy unit or terrain →
        /// the movement selection drops (HexDetectionService already set SelectedHex, so the panels/printer show
        /// the enemy intel report / terrain — that pipeline needs nothing from us here).
        /// </summary>
        private void HandleUnitSelectedClick(Position2D hexPos)
        {
            var gdm = GameDataManager.Instance;
            var ground = gdm.GetGroundUnitAtHex(hexPos);
            var air = gdm.GetAirUnitAtHex(hexPos);
            var clickedUnit = ground ?? air;

            // Another friendly unit → re-select it.
            if (clickedUnit != null && clickedUnit.Side == Side.Player && clickedUnit != CurrentUnit)
            {
                SelectUnit(clickedUnit);
                return;
            }

            // The already-selected unit → keep it selected (no-op).
            if (clickedUnit == CurrentUnit && clickedUnit != null)
                return;

            // Enemy unit or terrain (inside OR outside the radius) → drop the movement selection; the clicked
            // hex/unit is now the inspection target via SelectedHex (§5.10.6 — terrain click implicitly deselects).
            DeselectUnit();
        }

        /// <summary>
        /// Right-click (§5.10.4 / §5.10.5): inside the movement radius with a unit selected → commit the move
        /// immediately (no confirmation step); anywhere else → clear the unit AND terrain selection.
        /// </summary>
        private void HandleHexRightClicked(Position2D hexPos)
        {
            try
            {
                if (_currentPhase != BattlePhase.PlayerTurn) return;
                if (State == MovementState.Executing) return;

                var map = GameDataManager.CurrentHexMap;

                if (State == MovementState.UnitSelected && CurrentUnit != null && map != null
                    && _currentRange.Reachable.ContainsKey(hexPos))
                {
                    _currentPath = HexMapUtil.FindPath(map, CurrentUnit, CurrentUnit.MapPos, hexPos);
                    if (_currentPath != null && _currentPath.Count > 0)
                    {
                        StartCoroutine(ExecuteMovement());
                        return;
                    }
                }

                // Outside the radius (or nothing selected) → clear unit + terrain selection (§5.10.5).
                DeselectUnit();
                HexDetectionService.Instance?.ClearSelectionAndNotify();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleHexRightClicked), e);
            }
        }

        /// <summary>
        /// Ctrl+left-click — the ONLY combat trigger (§5.10.6). A legal enemy target → attack (direct or
        /// indirect by the firer's class); anything else is a NO-OP with denial feedback — it never falls
        /// through to selection, so a missed Ctrl+click can never move or deselect anything.
        /// </summary>
        private void HandleCtrlClick(Position2D hexPos)
        {
            if (State != MovementState.UnitSelected || CurrentUnit == null)
            {
                // No attacker selected → nothing to engage with. ⚠ Ungated: a refusal concerns the
                // player's own order and reveals nothing about a unit (§24.8.5 / §27.7.4).
                GameAudio.Play(SFX.ButtonDenied);
                return;
            }

            var gdm = GameDataManager.Instance;
            var target = gdm.GetGroundUnitAtHex(hexPos) ?? gdm.GetAirUnitAtHex(hexPos);

            if (target == null || target.Side == Side.Player)
            {
                GameAudio.Play(SFX.ButtonDenied);
                return;
            }

            TryAttack(target);
        }

        private void DeselectUnit()
        {
            // ⚠ Before the field is cleared — nothing here is unit-attributed, but the ordering keeps this
            // readable as "the selection ends" rather than "something happened to no unit".
            if (CurrentUnit != null) GameAudio.Play(SFX.UnitDeselect);

            CurrentUnit = null;
            _currentPath = null;
            State = MovementState.Idle;
            _lastHoverHex = GameDataManager.NoHexSelected;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaisePlayerUnitDeselected();
                EventManager.Instance.RaiseMovementRangeCleared();
                EventManager.Instance.RaiseMovementPathPreviewCleared();
            }
        }

        /// <summary>
        /// Hover-driven path preview (§5.10.3): while a unit is selected, the hex under the pointer — if
        /// reachable — previews the exact path a right-click would commit. Poll-based from Update() (no hover
        /// event exists in the input chain); FindPath runs only when the hovered hex CHANGES. Suppressed over
        /// HUD panels and outside the UnitSelected/PlayerTurn state.
        /// </summary>
        private void UpdatePathPreviewHover()
        {
            try
            {
                if (State != MovementState.UnitSelected || _currentPhase != BattlePhase.PlayerTurn || CurrentUnit == null)
                {
                    ClearPathPreviewIfShown();
                    return;
                }

                var mouse = Mouse.current;
                if (mouse == null) return;

                Vector2 screenPos = mouse.position.ReadValue();
                if (DefaultDialog_Scene1.Instance != null && DefaultDialog_Scene1.Instance.IsScreenPointOverUI(screenPos))
                {
                    ClearPathPreviewIfShown();
                    return;
                }

                Position2D hex = HexGridSystem.Instance.ScreenToHex(new Vector3(screenPos.x, screenPos.y, 0f), Camera.main);
                if (hex == _lastHoverHex) return;
                _lastHoverHex = hex;

                var map = GameDataManager.CurrentHexMap;
                if (map == null || hex == CurrentUnit.MapPos || !_currentRange.Reachable.ContainsKey(hex))
                {
                    EventManager.Instance?.RaiseMovementPathPreviewCleared();
                    return;
                }

                var path = HexMapUtil.FindPath(map, CurrentUnit, CurrentUnit.MapPos, hex);
                if (path == null || path.Count == 0)
                {
                    EventManager.Instance?.RaiseMovementPathPreviewCleared();
                    return;
                }

                EventManager.Instance?.RaiseMovementPathPreviewShown(path.ConvertAll(t => t.Position));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdatePathPreviewHover), e);
            }
        }

        /// <summary>Clears the hover preview once when leaving a preview-capable state.</summary>
        private void ClearPathPreviewIfShown()
        {
            if (_lastHoverHex == GameDataManager.NoHexSelected) return;
            _lastHoverHex = GameDataManager.NoHexSelected;
            EventManager.Instance?.RaiseMovementPathPreviewCleared();
        }

        #endregion // Selection Flow

        #region Combat

        /// <summary>
        /// Attack legality for the CURRENT unit against <paramref name="target"/> — null if legal, else the
        /// reason. Routes by the firer's class (ratified 2026-07-06): indirect-fire classes (ART/SPA/ROC/BM)
        /// ALWAYS use the §7.13 indirect pipeline (adjacent included); everyone else the §7.7.3 direct one.
        /// PUBLIC because the cursor feedback (§24.11.3) must run the SAME gate the click runs — it never lies.
        /// </summary>
        public string AttackLegality(CombatUnit target)
        {
            if (CurrentUnit == null) return "No unit selected.";
            var map = GameDataManager.CurrentHexMap;
            return CombatResolver.IsIndirectFireClass(CurrentUnit.Classification)
                ? IndirectCombatAction.CanExecute(CurrentUnit, target, map)
                : GroundCombatAction.CanExecute(CurrentUnit, target, map);
        }

        /// <summary>
        /// Resolves an attack by the selected unit against <paramref name="target"/> through the model-layer
        /// orchestrators — <see cref="IndirectCombatAction"/> for ART/SPA/ROC/BM firers (§7.13, any range in
        /// [1, IR]), <see cref="GroundCombatAction"/> for everyone else (§7.7.3, adjacent) — then refreshes the
        /// board: HP overlays, removed/displaced icons, spent actions/MP, and the movement overlay. The
        /// orchestrators own all eligibility gates and report the rejection reason when the attack is illegal.
        /// Automatic Advance (§7.9.9, direct only) is reported but not yet executed here (TODO — player prompt).
        /// </summary>
        private void TryAttack(CombatUnit target)
        {
            try
            {
                if (CurrentUnit == null || target == null) return;
                var map = GameDataManager.CurrentHexMap;
                if (map == null) return;

                bool executed;
                string message;
                bool attackerDestroyed;
                bool targetDestroyed;

                // Where the engagement happens, captured BEFORE resolution: a defender that retreats or routs
                // has already moved by the time the outcome returns, and the dispatch must name the hex the
                // fight was at, not the one the enemy fell back to.
                Position2D contactHex = target.MapPos;

                // Combat awards experience (§14 REP/XP), so a promotion can fall out of this attack. CombatUnit
                // is a pure model and raises no events, so the level change is detected here by comparison.
                ExperienceLevel expBefore = CurrentUnit.ExperienceLevel;

                if (CombatResolver.IsIndirectFireClass(CurrentUnit.Classification))
                {
                    IndirectCombatOutcome o = IndirectCombatAction.Execute(CurrentUnit, target, map, new CombatRandom());
                    executed = o.Executed;
                    message = o.Executed ? BuildIndirectMessage(CurrentUnit, target, o) : o.Reason;
                    attackerDestroyed = o.FirerDestroyed;
                    targetDestroyed = o.TargetDestroyed;

                    // §24.8.6 dispatch. Filed AFTER the whole action resolves so counter-battery losses are
                    // included — reporting mid-action would print "no losses" and then be contradicted.
                    // PrinterDispatch decides whether it is worth printing and handles both sides.
                    if (o.Executed) PrinterDispatch.ReportIndirectCombat(CurrentUnit, target, contactHex, o);
                }
                else
                {
                    // TODO §7.5.6.9.1 — compute contestedCrossing from river/bridge geometry between the two hexes.
                    GroundCombatOutcome o = GroundCombatAction.Execute(CurrentUnit, target, map, new CombatRandom());
                    executed = o.Executed;
                    message = o.Executed ? BuildCombatMessage(CurrentUnit, target, o) : o.Reason;
                    attackerDestroyed = o.AttackerDestroyed;
                    targetDestroyed = o.DefenderDestroyed;

                    // See EventManager / §24.8.6 — one call files whichever side's report the player owns.
                    if (o.Executed) PrinterDispatch.ReportGroundCombat(CurrentUnit, target, contactHex, o);
                }

                if (!executed)
                {
                    AppService.CaptureUiMessage(message);
                    GameAudio.Play(SFX.ButtonDenied);
                    return;
                }

                AppService.CaptureUiMessage(message);

                /* ═══ AUDIO (§27.7.4 fog gate, §27.7.5 family mapping) ═══
                 *
                 * ⚠ ORDERING IS LOAD-BEARING. PlayWeaponFire runs AFTER the orchestrator returns, so the
                 * firing-reveal spotting change (§7.13.5.4 / §12.4.9) has already landed and the gate sees
                 * the POST-reveal level. Called before Execute it would read the pre-reveal level and
                 * suppress a shot the player is entitled to hear — invisible today because the firer is
                 * always the player's own unit, and a real bug the moment the AI turn calls this path.
                 *
                 * ⚠ ATTRIBUTION: the FIRING sound is the firer's, the IMPACT is the target's. That split
                 * is what lets an unseen battery shell the player audibly without identifying itself, and
                 * it is why no "generic substitute sound" is needed anywhere. */
                GameAudio.PlayWeaponFire(CurrentUnit);
                GameAudio.PlayImpact(target);

                // A kill is attributed to the unit that DIED — you hear your own regiment go, and an
                // unspotted enemy dies silently, which is the same information the icon already gives.
                if (targetDestroyed) GameAudio.PlayFrom(SFX.UnitDestroyed, target);
                if (attackerDestroyed) GameAudio.PlayFrom(SFX.UnitDestroyed, CurrentUnit);

                // §24.8.6 — announce a promotion earned in this engagement. Guarded on the attacker surviving:
                // a destroyed regiment has already filed its own loss report and cannot also report good news.
                if (!attackerDestroyed && CurrentUnit.ExperienceLevel != expBefore)
                    PrinterDispatch.ReportUnitHardened(CurrentUnit);

                // Refresh the board off the new unit state.
                GameDataManager.Instance.BuildOccupancyCache();
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.RaiseRedrawMapIcons();             // HP %, removals, defender displacement
                    EventManager.Instance.RaiseUnitActionsChanged(CurrentUnit);
                    EventManager.Instance.RaiseUnitMovementPointsChanged(CurrentUnit);
                }

                // Attacker killed (return fire §7.4.2.3 / counter-battery §7.13.5) → nothing left to keep selected.
                if (attackerDestroyed)
                {
                    DeselectUnit();
                    return;
                }

                // Keep the unit selected and refresh its movement overlay (combat spent 25% MP).
                State = MovementState.UnitSelected;
                RecomputeRangeAndRaise(map);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TryAttack), e);
            }
        }

        /// <summary>Short HUD line summarizing a resolved direct attack.</summary>
        private static string BuildCombatMessage(CombatUnit attacker, CombatUnit target, GroundCombatOutcome o)
        {
            if (o.DefenderDestroyed) return $"{attacker.UnitName} destroyed {target.UnitName}.";
            if (o.DefenderRemovedFromMap) return $"{attacker.UnitName} broke {target.UnitName} — it withdrew from the field.";
            if (o.DefenderMoved) return $"{attacker.UnitName} hit {target.UnitName} for {o.DamageToDefender} — it fell back.";
            return $"{attacker.UnitName} hit {target.UnitName} for {o.DamageToDefender} (held).";
        }

        /// <summary>Short HUD line summarizing a resolved indirect fire mission (§7.13).</summary>
        private static string BuildIndirectMessage(CombatUnit firer, CombatUnit target, IndirectCombatOutcome o)
        {
            string cb = o.CounterBatteryFired ? $" Counter-battery hit back for {o.DamageToFirer}." : string.Empty;
            if (o.TargetDestroyed) return $"{firer.UnitName} destroyed {target.UnitName} with indirect fire.{cb}";
            if (o.TargetRemovedFromMap) return $"{firer.UnitName} broke {target.UnitName} — it withdrew from the field.{cb}";
            if (o.TargetMoved) return $"{firer.UnitName} shelled {target.UnitName} for {o.DamageToTarget} — it fell back.{cb}";
            return $"{firer.UnitName} shelled {target.UnitName} for {o.DamageToTarget} (held).{cb}";
        }

        #endregion // Combat

        #region Movement Execution

        private IEnumerator ExecuteMovement()
        {
            if (CurrentUnit == null || _currentPath == null || _currentPath.Count == 0)
                yield break;

            State = MovementState.Executing;

            if (!CurrentUnit.BeginMoveOrder())
            {
                GameAudio.Play(SFX.OutOfMP);
                State = MovementState.UnitSelected;
                yield break;
            }

            if (EventManager.Instance != null)
            {
                var pathPositions = _currentPath.ConvertAll(t => t.Position);
                EventManager.Instance.RaiseUnitMoveStarted(CurrentUnit, pathPositions);
            }

            var map = GameDataManager.CurrentHexMap;

            /* ⚠ HOW THE UNIT IS TRAVELLING RIGHT NOW, read from the profile carrying it — never from its
             * classification (§3.7 MovementModeService). These two booleans must agree with the pair inside
             * HexMapUtil's range and path passes; when they disagree the overlay draws hexes the move
             * cannot reach. An air-assault regiment riding Mi-8s is not `UnitClassification.HELO`, so the
             * old classification test walked it over the mountains it was flying above. */
            var medium = MovementModeService.CurrentMedium(CurrentUnit);
            bool isAir = MovementModeService.IsAirborneNow(CurrentUnit);
            bool isFixedWing = medium == MovementMedium.FixedWing;
            Position2D previousPos = CurrentUnit.MapPos;
            Position2D originPos = CurrentUnit.MapPos;   // for the post-move stacking refresh

            // Hexes actually entered this move (in order; last = where the unit ends). Drives the
            // §6.13 tile-control flips after the move settles. May be shorter than the planned path
            // if an ambush / ZoC halt cuts the move short.
            var enteredHexes = new List<Position2D>();

            /* §11.8.9 Shock input — cumulative HP this helicopter has lost during THIS move order. Shock
             * accumulates across events, so a second burst is far likelier to break the sortie than the
             * first. Reset per move order, never per hex. */
            int hpLostThisMove = 0;

            /* §11.8.6 anti-dogpile, extended to ambush — one bite per ENEMY per move order. Matters now
             * that a helicopter flies ON through an ambush: without this the same regiment engages it again
             * at every hex still in its reach, and accumulating Shock breaks the sortie on geometry alone.
             * ⚠ SHARED BY AMBUSH AND OVERHEAD FIRE (2026-08-11), because the ratified overhead-GAD rule is
             * "one engagement per unit per move under the same anti-dogpile rule as ambush" — a regiment that
             * has already sprung on a passing helo does not also shoot at it overhead, and vice versa.
             * ⚠ NOT the §11.8.3 ranged air-defence budget, which is per TURN and lives on the firing unit
             * (CombatUnit.MarkAircraftEngaged). Different scope, different owner, deliberately not merged. */
            var enemiesEngagedThisMove = new HashSet<string>();

            /* ⚠ ONE SPELLING of the per-hex tween length, because the movement SOUND is chosen from it.
             * It used to be re-declared inside the loop; two copies would let the audio pick a clip for a
             * duration the animation no longer runs at. */
            float stepSeconds = isFixedWing ? 0.08f : 0.18f;

            /* §27.7.7 — ONE fire-and-forget shot for the WHOLE move. Not a per-hex blip (which would
             * machine-gun at ~0.18 s/hex) and not a loop (R3 dissolved: nothing would own stopping it when
             * an ambush halts the move or the unit dies mid-path).
             * ⚠ The medium and the clip choice both live in the audio layer now — this call site just
             * supplies the mover and how long its committed path will take. Fog-gated inside, so it is
             * already correct for the AI turn. */
            GameAudio.PlayMovement(CurrentUnit, _currentPath.Count * stepSeconds);

            // TODO: Move undo — allowed only when no new spotting events fired during the move

            for (int i = 0; i < _currentPath.Count; i++)
            {
                var targetTile = _currentPath[i];
                var targetPos = targetTile.Position;

                /* ⚠ CONTACT HALT — THE OVERRUN FIX (2026-08-10). Before stepping in, is an enemy standing
                 * there? The movement RANGE deliberately ignores unspotted enemies so the overlay cannot
                 * leak their position through fog (§12) — `HexMapUtil` says as much and promises the
                 * mid-move sweep "reveals and halts on contact instead". That halt was never written, so a
                 * regiment walked straight over an unspotted enemy and kept going.
                 * The unit stops BEFORE entering, the enemy is revealed to contact, and movement points
                 * survive for a combat or intel action — you found them, now decide what to do about it. */
                /* ⚠ GROUND ONLY. Anything AIRBORNE overflies an occupied hex (ratified 2026-08-10) — the
                 * price of overflight is fire and a stand check, not a wall. */
                if (!isAir)
                {
                    var blocker = GameDataManager.Instance?.GetGroundUnitAtHex(targetPos);
                    if (blocker != null && blocker.Side != CurrentUnit.Side)
                    {
                        SpottingService.RevealToContact(blocker);
                        ApplyMovementHalt(CurrentUnit, MovementHalt.Contact);

                        PrinterDispatch.ReportMoveBlockedByContact(CurrentUnit, targetPos);
                        GameAudio.Play(SFX.UnitMoveBlocked);
                        break;
                    }
                }

                // Compute step cost
                var currentTile = map.GetHexAt(CurrentUnit.MapPos);
                var dir = HexMapUtil.GetDirectionBetween(CurrentUnit.MapPos, targetPos);
                int stepCost = isAir ? 1 : targetTile.MovementCost;

                // Road bonus
                if (!isAir && currentTile != null && currentTile.IsRoad && targetTile.IsRoad)
                    stepCost = Math.Max(1, stepCost / 2);

                // Deduct MP
                if (!CurrentUnit.DeductMovementCost(stepCost))
                {
                    break;
                }

                // Update position
                previousPos = CurrentUnit.MapPos;
                HexMapUtil.MoveUnitTo(map, CurrentUnit, targetPos);

                // §3.5.8 recovery input: the unit moved this turn (set per step; read at Upkeep).
                CurrentUnit.MarkMovedThisTurn();

                // Record the entered hex for the post-move §6.13 tile-control pass.
                enteredHexes.Add(targetPos);

                // Animate the icon a single hex step and WAIT for the tween before running the arrival
                // checks below — the unit visibly enters the hex, then spotting/ambush/ZoC resolve there.
                var iconRenderer = GameIconRenderer.Instance;
                if (iconRenderer != null)
                {
                    // Turn the icon INTO the step direction before it glides — MoveUnitTo above already
                    // rotated unit.Facing toward this step; the icon re-resolves sprite + flip from it.
                    iconRenderer.RefreshIconFacing(CurrentUnit.UnitID);

                    bool stepDone = false;
                    iconRenderer.AnimateIconStep(CurrentUnit.UnitID, targetPos, stepSeconds, () => stepDone = true);
                    yield return new WaitUntil(() => stepDone);
                }
                else
                {
                    // Headless / no renderer (tests) — keep the cadence without animating.
                    yield return new WaitForSeconds(stepSeconds);
                }

                /* ⚠ THE MOVE IS COMMITTED BLIND (§12.4.4a, ratified 2026-08-10 — the Panzer General
                 * rule). The mover's own passive spotting does NOT run in this loop; it applies once at
                 * settlement, below. That is what makes this check REACHABLE at all: a mover must stand at
                 * distance 2 before it can stand at distance 1, and the per-hex sweep that used to run
                 * here raised every ambusher to Level1 one hex before adjacency — the trigger's Level0
                 * requirement could never be met, for any unit, ever (the earlier ambush-before-sweep
                 * ordering fix only mattered within a single hex; the sweep from the PREVIOUS hex had
                 * already disarmed the trap). Mid-move, the only reveals are event-driven: the contact
                 * halt above, the ambush itself (§6.9.3), and air-ambush detection below.
                 *
                 * WHO IS SUBJECT TO IT, BY THE MEDIUM CARRYING THE UNIT (Bob, 2026-08-10):
                 *   GROUND      → the full §6.9 ambush, combat and all.
                 *   HELO        → the same, minus the §6.9.4 surprise multiplier (handled in the lane
                 *                 builder). ⚠ A helo-borne regiment is A SPECIAL KIND OF GROUND UNIT — it
                 *                 stays on the map, so ground troops can catch it.
                 *   FIXED-WING  → NOTHING, and that is the rule rather than an omission. It is only ever
                 *                 crossing the map to reach the air ops box; it does not look at the ground
                 *                 on the way (see SpottingService §12.3) and ground troops cannot touch it.
                 *                 Air defence engages it through the SEPARATE air-ambush path below, which
                 *                 is how an unspotted SAM reveals itself by firing. */
                if (!isFixedWing)
                {
                    var ambusher = SpottingService.CheckGroundAmbush(CurrentUnit, targetPos, enemiesEngagedThisMove);
                    if (ambusher != null)
                    {
                        enemiesEngagedThisMove.Add(ambusher.UnitID);

                        /* ⚠ A GROUND UNIT STOPS DEAD; A HELICOPTER TAKES THE FIRE AND FLIES ON (ratified
                         * 2026-08-10, superseding §5.13.2.2's "turn ends"). THE DIVERGENCE IS THE RULE, not
                         * an inconsistency to tidy away: a helicopter is not stopped by troops on the
                         * ground, it is SHOT AT by them, and whether the sortie survives that is decided by
                         * the §11.8.9 transit stand check below — hold and continue, or abort home. */
                        if (!isAir)
                            ApplyMovementHalt(CurrentUnit, MovementHalt.GroundAmbush);

                        /* ⚠ THE ORCHESTRATOR, NOT THE EVENT. `RaiseAmbushTriggered` has never had a
                         * subscriber, so until 2026-08-10 an ambush dealt NO DAMAGE — it halted the mover
                         * and printed a dispatch about a fight that never happened. `AmbushAction` is the
                         * caller `CombatResolver.ResolveAmbush` was written for. */
                        var ambush = AmbushAction.Execute(ambusher, CurrentUnit, map, new CombatRandom());
                        hpLostThisMove += ambush.DamageToMover;

                        if (AMBUSH_DEBUG)
                            Debug.Log($"[AMBUSH DEBUG] {ambusher.UnitName} ({ambusher.Classification}) sprang " +
                                      $"{CurrentUnit.UnitName} entering {targetPos}: executed={ambush.Executed}, " +
                                      $"dmg={ambush.DamageToMover} (0 with no error above = a MISS band, legitimate §7.6), " +
                                      $"stand={ambush.MoverOutcome}, displaced={ambush.MoverMoved} " +
                                      $"back {ambush.MoverHexesRetreated} to {ambush.MoverFinalPosition}, " +
                                      $"removed={ambush.MoverRemovedFromMap}" +
                                      (ambush.Executed ? "" : $", REASON={ambush.Reason}"));

                        /* ⚠ NARRATION ONLY IF THE RESOLUTION ACTUALLY RAN. If the orchestrator failed
                         * internally (its own catch — already logged), the halt above stands (§6.9.2: the
                         * move is over on the trigger hex) but no event, dispatch or sound fires — a fight
                         * that never resolved must not be narrated as one. Without this gate a swallowed
                         * failure is indistinguishable in play from a whiffed ambush. */
                        if (ambush.Executed)
                        {
                            if (EventManager.Instance != null)
                                EventManager.Instance.RaiseAmbushTriggered(ambusher, CurrentUnit);

                            // §24.8.6 — the attribution case: fire came from a hex the player had no contact on.
                            PrinterDispatch.ReportAmbush(ambusher, CurrentUnit, targetPos);

                            /* ⚠ ATTRIBUTED TO THE VICTIM, NOT THE AMBUSHER — the sharpest case §27.7.4.2
                             * exists for. The ambusher is BY DEFINITION unspotted (§6.9.0), so attributing
                             * the sound to it would gate the player's own regiment being hit into silence;
                             * playing it ungated would announce a hidden unit. You always hear your own men
                             * take fire, and you learn nothing about who fired. */
                            GameAudio.PlayFrom(SFX.AmbushTriggered, CurrentUnit);
                        }

                        /* ⚠ UNCONDITIONAL, mirroring the direct-combat path ("HP %, removals, defender
                         * displacement"): the coarse redraw is the ONLY thing that refreshes the victim's
                         * HP box — `RaiseUnitHitPointsChanged` is unused scaffolding (§3.6e). Gating this
                         * on displacement/removal left a HOLD outcome showing pre-ambush HP: the model had
                         * taken the damage and the ledger had booked it, but the icon never heard
                         * (play-test 2026-08-11 — log said dmg=9, HP box said nothing). */
                        EventManager.Instance?.RaiseRedrawMapIcons();

                        if (ambush.MoverRemovedFromMap)
                        {
                            State = MovementState.Idle;
                            yield break;
                        }

                        /* §11.8.9 — the transit stand check, per DAMAGING event, with Shock accumulating
                         * across the whole move. A helicopter that holds carries on with what it has left;
                         * one that breaks flies home free. Ground units never reach this: they already
                         * halted above. */
                        if (isAir && ambush.DamageToMover > 0
                            && !HoldsTransitStand(CurrentUnit, hpLostThisMove))
                        {
                            AbortFlightToOrigin(CurrentUnit, originPos);
                            break;
                        }

                        // A helicopter that held is still flying — do NOT break; the move continues.
                        if (!isAir) break;
                    }
                    else if (AMBUSH_DEBUG)
                    {
                        // Diagnostic: name why any adjacent enemy did NOT spring on this hex (see the flag's note).
                        DebugLogAmbushScan(CurrentUnit, targetPos, enemiesEngagedThisMove);
                    }
                }

                /* §11.8 TRANSIT FIRE — everything the ground throws at an aircraft crossing this hex:
                 * ranged air-defence opportunity fire from every eligible battery whose envelope covers it,
                 * and (helicopters only) OVERHEAD fire from whatever it just flew directly above. Both feed
                 * the one §11.8.9 Shock accumulator, so a sortie is broken by its total punishment across
                 * the move rather than by any single hit. */
                if (isAir)
                {
                    var transit = ResolveTransitFire(
                        CurrentUnit, targetPos, isFixedWing, enemiesEngagedThisMove, hpLostThisMove);

                    hpLostThisMove = transit.HpLostThisMove;

                    if (transit.MoverRemovedFromMap)
                    {
                        State = MovementState.Idle;
                        yield break;
                    }

                    if (transit.Aborted)
                    {
                        AbortFlightToOrigin(CurrentUnit, originPos);
                        break;
                    }
                }

                /* ZoC-to-ZoC check. ⚠ GROUND ONLY, AND THAT IS THE RULE, NOT AN OVERSIGHT: zones of control
                 * never stop a flight (ratified 2026-08-04). Ambush is the single mechanism by which an
                 * enemy halts an airborne move. */
                if (!isAir && _currentRange.ZocTerminals.Contains(targetPos))
                {
                    ApplyMovementHalt(CurrentUnit, MovementHalt.ZoneOfControl);

                    // Ungated: the halt is a fact about the player's own order, and the enemy ZoC that
                    // caused it belongs to a unit they have already spotted.
                    GameAudio.Play(SFX.UnitMoveBlocked);
                    break;
                }

                // Recompute range display for ground/helo (not fixed-wing)
                if (!isFixedWing && EventManager.Instance != null)
                {
                    var updatedRange = HexMapUtil.GetValidMoveDestinations(map, CurrentUnit);
                    _currentRange = updatedRange;
                    EventManager.Instance.RaiseMovementRangeComputed(CurrentUnit, updatedRange.Reachable, updatedRange.ZocTerminals);
                }

                if (EventManager.Instance != null)
                    EventManager.Instance.RaiseUnitMovementPointsChanged(CurrentUnit);
            }

            /* ⚠ LANDED ON SOMEONE? Displace to the nearest legal hex (Bob, play-tested 2026-08-10). The
             * range overlay keeps UNSPOTTED enemy hexes selectable on purpose — hiding them would leak
             * their position through fog (§12) — and a helicopter overflies the contact halt by design, so
             * a move ordered onto a hidden enemy puts the two in one ground stack. The ruling is "nearest
             * legal space, however ridiculous"; unit density makes it rare enough not to matter. */
            var settled = HexMapUtil.FindNearestLegalRestingHex(map, CurrentUnit, CurrentUnit.MapPos);
            if (settled != CurrentUnit.MapPos)
            {
                HexMapUtil.MoveUnitTo(map, CurrentUnit, settled);
                GameDataManager.Instance?.BuildOccupancyCache();
            }

            /* §12.4.4a — THE COLUMN REPORTS IN. The move was committed blind (see the loop comment); now
             * that it has settled — path done, or ended by ambush/contact/ZoC/exhausted MP, displacement
             * included — one passive pass covers every hex entered plus the resting hex, each at its own
             * distance ceiling. Dispatches, icons and the contact sound all land here, once per newly
             * revealed unit — never per hex. A mover DESTROYED mid-move never reaches this line and files
             * no report (the ambusher's own §6.9.3 reveal already fired inside AmbushAction).
             * ⚠ No fixed-wing skip here, deliberately: SpottingRangeAgainst already resolves a transiting
             * jet to 0 against ground targets (§12.3.7a), while RECONA/AWACS look-down and helo-borne
             * ground vision flow through this same call. The range function is the policy. */
            var observedFrom = new List<Position2D>(enteredHexes) { CurrentUnit.MapPos };
            var newlySpotted = SpottingService.ApplyPostMoveSpotting(CurrentUnit, observedFrom);

            /* First contact. ⚠ PlayFrom on the SPOTTED unit rather than an ungated Play, even though
             * everything in this list is by definition now spotted: if the meaning of "newly spotted"
             * ever drifts, the gate suppresses the sound instead of announcing a hidden unit. Fails
             * closed, like AudioFogPolicy itself. */
            if (newlySpotted.Count > 0)
                GameAudio.PlayFrom(SFX.UnitSpotted, newlySpotted[0]);

            // Move complete. Snap the icon to its final hex (defends against tween rounding or a halted
            // last step) and refresh air/ground stacking at both ends (a departed origin may reveal a
            // hidden stack; the destination may form a new one).
            var finalRenderer = GameIconRenderer.Instance;
            if (finalRenderer != null)
            {
                finalRenderer.SnapIcon(CurrentUnit.UnitID, CurrentUnit.MapPos);
                if (originPos != CurrentUnit.MapPos)
                    finalRenderer.CheckForStacking(originPos);
                finalRenderer.CheckForStacking(CurrentUnit.MapPos);
            }

            CameraService.Instance?.CenterOnPosition(CurrentUnit.MapPos);

            // §6.13 / §17.5 — movement-driven tile control. Ground + helicopters flip terrain;
            // fixed-wing fly over and never flip (§6.13.2). Applied once the move has settled.
            if (!isFixedWing && enteredHexes.Count > 0)
            {
                var territory = TerritoryService.ApplyMoveControl(map, CurrentUnit, enteredHexes);
                ApplyTerritoryAccounting(territory);

                // Repaint the Map layer so city/objective control flags reflect the flips.
                // RefreshMap touches only the Map layers (not units or the movement overlay).
                // Full redraw per move order — fine at this tempo; targeted refresh is a later optimization.
                if (territory.AnyChange)
                    HexGridRenderer.Instance?.RefreshMap();
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaiseUnitMoveCompleted(CurrentUnit);
                EventManager.Instance.RaiseUnitActionsChanged(CurrentUnit);
            }

            // Leader reputation for the move order (§14.5.1, wired 2026-07-03) — one award per completed
            // move order (not per hex); Veteran/Elite units earn ×1.5 (§14.5.10).
            if (enteredHexes.Count > 0)
            {
                var moveLeader = CurrentUnit.GetAssignedLeader();
                moveLeader?.AwardReputationForAction(GameData.ReputationAction.Move,
                    CurrentUnit.ExperienceLevel >= ExperienceLevel.Veteran ? GameData.REP_EXPERIENCE_MULTIPLIER : 1.0f);
            }

            GameDataManager.Instance.BuildOccupancyCache();

            // Panzer-General-style: the unit STAYS selected after a move (2026-07-24). A unit with move left
            // keeps its range overlay; a spent one stays selected with an empty overlay (still Ctrl-attackable,
            // still deselectable by right-click). RecomputeRangeAndRaise already yields an empty range when the
            // unit can no longer begin a move.
            State = MovementState.UnitSelected;
            RecomputeRangeAndRaise(map);

            // Make the hex selection FOLLOW the unit to its new position so the panels + hex highlight track
            // it (re-selecting the same unit is a no-op in HandleUnitSelectedClick).
            HexDetectionService.Instance?.SelectHex(CurrentUnit.MapPos);
        }

        /// <summary>
        /// The ways a move can end before its path does. Each kind spends a DIFFERENT set of resources,
        /// which is why this is an enum rather than the bool it replaced.
        /// </summary>
        /// <remarks>
        /// `internal` so EditorTests can pin the composition of each halt. The FlightEvasion rule is
        /// "movement points and the move action, and nothing else" — a rule whose violation is invisible in
        /// play (a flight that also lost its combat action just looks like a flight that was ambushed) and
        /// which someone will eventually "tidy" into matching the ground branch.
        /// </remarks>
        internal enum MovementHalt
        {
            /// <summary>ZoC-to-ZoC (§6.2): movement points survive for a combat or intel action.</summary>
            ZoneOfControl,

            /// <summary>
            /// Walked into an enemy that was not there a moment ago — an unspotted unit standing in the
            /// next hex. Same consequence as a ZoC halt (the move is over, but you may still fight), a
            /// different cause, and worth naming separately so the reason is legible at the call site.
            /// </summary>
            Contact,

            /// <summary>
            /// Ground ambush (§6.9): the unit is in contact and everything is spent. ⚠ Applies to
            /// HELICOPTERS TOO — §5.13.2.2, the helicopter's turn ends after taking the attack. The
            /// narrower `FlightEvasion` kind was retired 2026-08-10 with the evade-without-damage rule.
            /// </summary>
            GroundAmbush
        }

        /// <summary>
        /// What one hex of ground fire did to a transiting aircraft.
        /// </summary>
        /// <remarks>`internal` so EditorTests can drive <see cref="ResolveTransitFire"/> directly.</remarks>
        internal struct TransitFireResult
        {
            /// <summary>The running §11.8.9 Shock total, updated with anything taken here.</summary>
            public int HpLostThisMove;

            /// <summary>The §11.8.9 stand check broke the sortie — the caller flies it home.</summary>
            public bool Aborted;

            /// <summary>Shot down: already unregistered, so the caller must stop the move coroutine.</summary>
            public bool MoverRemovedFromMap;
        }

        /// <summary>
        /// Everything the ground throws at an aircraft entering <paramref name="hex"/>: §11.8 ranged
        /// air-defence opportunity fire from every eligible battery covering it, then — for a helicopter —
        /// overhead fire from whatever it flew directly above.
        /// </summary>
        /// <remarks>
        /// ⚠ REPLACED A `UnityEngine.Random.Range(0, 2)` COIN FLIP (2026-08-11). The old path found at most
        /// one UNSPOTTED air-defence unit, rolled the fixed-wing detection check for EVERY mover including
        /// helicopters, and then — under a `// TODO: Combat resolution for air ambush` — decided the sortie
        /// on a 50/50 with no damage, no stand check and no reveal. `CombatResolver.ResolveAirDefenseFire`
        /// and `HeloTransitStandCheck` were both already built and tested with zero callers; this is their
        /// wiring, not new rules.
        /// </remarks>
        internal static TransitFireResult ResolveTransitFire(
            CombatUnit mover, Position2D hex, bool isFixedWing,
            ISet<string> enemiesEngagedThisMove, int hpLostThisMove)
        {
            var result = new TransitFireResult { HpLostThisMove = hpLostThisMove };

            // ── §11.8 ranged air-defence opportunity fire ──────────────────────────────────────────────
            foreach (var contact in SpottingService.FindTransitAirDefense(mover, hex))
            {
                var firer = contact.Firer;
                if (firer == null || firer.IsDestroyed()) continue;

                /* ⚠ THE SPLIT THIS PASS EXISTS FOR — §5.13.3.2 vs §5.13.2.4. A FIXED-WING mover gets one
                 * 1d6-vs-experience look at a battery that was still unspotted, and a success averts the
                 * shot outright. A HELICOPTER GETS NO ROLL: it takes the hit whenever the battery has shots
                 * available, and its only escape is the stand check afterwards, on damage already taken. */
                if (isFixedWing && contact.WasUnspotted
                    && SpottingService.RollFixedWingAmbushDetection(firer, mover))
                    continue;

                /* §11.8.3 — spend the shot. A refusal (no opportunity action, or not the supply to pay for
                 * one) fires nothing and leaves no trace, which is why the scan asked with the silent
                 * predicate first: this call announces its refusals to the player's message log. */
                if (!firer.PerformOpportunityAction()) continue;

                // §11.8.6 — this aircraft is off this battery's list for the rest of the turn.
                firer.MarkAircraftEngaged(mover.UnitID);

                var fire = CombatResolver.ResolveAirDefenseFire(firer, mover, new CombatRandom());
                if (!fire.Engaged) continue;

                // §11.8.4 / §12.4.9.1 — radars hot, shooting from the open: identified, not merely located.
                SpottingService.RevealByOpportunityFire(firer);

                PrinterDispatch.ReportAirDefenseFire(mover, hex);
                GameAudio.PlayFrom(SFX.AmbushTriggered, mover);

                if (ApplyTransitDamage(mover, fire.DamageToAircraft, fire.AircraftDestroyed, isFixedWing, ref result))
                    return result;
            }

            // ── Overhead fire (the GAD rule) — helicopters only ────────────────────────────────────────
            if (!isFixedWing)
                ApplyOverheadFire(mover, hex, enemiesEngagedThisMove, ref result);

            return result;
        }

        /// <summary>
        /// Books one transit damage event: accumulates §11.8.9 Shock, refreshes the HP box, and answers
        /// whether the move is over — shot down, or the sortie broken by the stand check.
        /// </summary>
        private static bool ApplyTransitDamage(
            CombatUnit mover, int damage, bool destroyed, bool isFixedWing, ref TransitFireResult result)
        {
            result.HpLostThisMove += damage;

            /* ⚠ UNCONDITIONAL, for exactly the reason the ambush branch was fixed on 2026-08-11: the coarse
             * redraw is the ONLY thing that refreshes a unit's HP box (§3.6e, `RaiseUnitHitPointsChanged` is
             * unused scaffolding). Gating it on destruction leaves a damaged-but-still-flying aircraft
             * showing its pre-hit strength while the model and the loss ledger both know better. */
            EventManager.Instance?.RaiseRedrawMapIcons();

            if (destroyed)
            {
                GameDataManager.Instance?.UnregisterCombatUnit(mover.UnitID);
                result.MoverRemovedFromMap = true;
                return true;
            }

            /* §11.8.9 — the transit stand check, per DAMAGING event, with Shock accumulating across the
             * whole move. ⚠ HELICOPTERS ONLY, per the check's own scope line: a fixed-wing aircraft crossing
             * to its ops box takes the damage and presses on, because its stand checks belong to the
             * air-to-air venues (§7.9.8 in the AOB/AIB), not to transit. */
            if (!isFixedWing && damage > 0 && !HoldsTransitStand(mover, result.HpLostThisMove))
            {
                result.Aborted = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The overhead GAD rule (ratified 2026-08-10): a helicopter crossing directly OVER an enemy ground
        /// unit's hex is fired on by that unit, Δ = its GAD − the helo's GAD, then rolls the §11.8.9 check.
        /// </summary>
        /// <remarks>
        /// ⚠ THE SAME HEX, NEVER ADJACENCY AND NEVER A RADIUS — and that narrowness is what makes the rule
        /// safe. Overflight is avoidable by ROUTING, so this makes the flight PATH a decision instead of
        /// making flight suicidal, and it is what gives recon units real work: you need to know what is
        /// UNDER the path, not merely where you intend to land. A radius version would break every sortie on
        /// accumulated Shock alone.
        /// ⚠ Bases are excluded (§7A.20 — facilities never initiate attacks), and so is an Embarked unit,
        /// which is riding in someone else's vehicle and in no position to shoot at anything.
        /// </remarks>
        private static void ApplyOverheadFire(
            CombatUnit helo, Position2D hex, ISet<string> enemiesEngagedThisMove, ref TransitFireResult result)
        {
            var gdm = GameDataManager.Instance;
            if (gdm == null) return;

            foreach (var below in gdm.GetUnitsAtHex(hex))
            {
                if (below == null || below.IsDestroyed()) continue;
                if (below.UnitID == helo.UnitID) continue;      // the helo is itself in this ground stack
                if (below.Side == helo.Side) continue;
                if (below.OccupiesDomain != Domain.Ground) continue;
                if (below.IsBase) continue;
                if (below.DeploymentPosition == DeploymentPosition.Embarked) continue;

                // One engagement per enemy per move order, shared with ambush (see the set's declaration).
                if (enemiesEngagedThisMove.Contains(below.UnitID)) continue;
                enemiesEngagedThisMove.Add(below.UnitID);

                var over = CombatResolver.ResolveOverheadFire(below, helo, new CombatRandom());
                if (!over.Engaged) continue;

                // It shot upward from its own hex at something directly above — position and equipment both.
                SpottingService.RevealByOpportunityFire(below);

                PrinterDispatch.ReportOverheadFire(helo, hex);
                GameAudio.PlayFrom(SFX.AmbushTriggered, helo);

                if (ApplyTransitDamage(helo, over.DamageToHelo, over.HeloDestroyed, isFixedWing: false, ref result))
                    return;
            }
        }

        /// <summary>
        /// §11.8.9 — the helicopter transit stand check. True if the sortie holds and the move continues.
        /// </summary>
        private static bool HoldsTransitStand(CombatUnit helo, int hpLostThisMove)
        {
            var input = new HeloTransitStandInput
            {
                Experience = helo.ExperienceLevel,
                HpLostThisMove = hpLostThisMove,
            };

            int sv = HeloTransitStandCheck.ComputeStandValue(input);
            return HeloTransitStandCheck.ResolveStand(sv, new CombatRandom()) == HeloTransitOutcome.Hold;
        }

        /// <summary>
        /// §11.8.9 ABORT — the sortie breaks off and flies home. A FREE return to the hex the move order
        /// began on (no opportunity fire on the return leg, mirroring §5.13.5), movement points and every
        /// action to zero, and an embarked transport sets its passengers down at the origin.
        /// </summary>
        /// <remarks>
        /// ⚠ THE ORIGIN HEX IS GUARANTEED FREE, and that is why there is no fallback here. A move order is
        /// atomic with respect to input — nothing else can move between departure and abort — so the hex
        /// the unit left cannot have been taken in the interim. ⚠ That guarantee holds only while input
        /// stays gated during `MovementState.Executing`; if that ever changes, this needs a fallback hex.
        /// </remarks>
        private void AbortFlightToOrigin(CombatUnit helo, Position2D originPos)
        {
            var map = GameDataManager.CurrentHexMap;
            HexMapUtil.MoveUnitTo(map, helo, originPos);

            helo.ForceSetMovementPoints(0);
            helo.ForceSetActions(0, 0, 0);
            helo.MoveActions.SetCurrent(0);

            // An air-assault lift that breaks off puts its troops down where they started (§11.8.9).
            if (helo.DeploymentPosition == DeploymentPosition.Embarked
                && MovementModeService.CurrentMedium(helo) == MovementMedium.Helo)
            {
                helo.TryDeployDOWN(out _);
            }

            GameIconRenderer.Instance?.SnapIcon(helo.UnitID, originPos);
            PrinterDispatch.ReportFlightAborted(helo, originPos);
            GameAudio.Play(SFX.UnitMoveBlocked);
        }

        /// <summary>
        /// Applies a movement halt. Every kind ends the move order; they differ in what else they cost.
        /// </summary>
        internal static void ApplyMovementHalt(CombatUnit unit, MovementHalt kind)
        {
            // Common to every halt: the move order is over.
            unit.MoveActions.SetCurrent(0);

            switch (kind)
            {
                case MovementHalt.GroundAmbush:
                    // Caught in contact — movement and every action gone.
                    unit.ForceSetMovementPoints(0);
                    unit.ForceSetActions(0, 0, 0);
                    break;

                case MovementHalt.ZoneOfControl:
                case MovementHalt.Contact:
                    // Two causes, one consequence: the move is over, but preserve enough MP to still fight
                    // or scout from where it stopped if an action remains.
                    bool hasCombat = unit.CombatActions.Current >= 1;
                    bool hasIntel = unit.IntelActions.Current >= 1;

                    if (hasCombat || hasIntel)
                    {
                        float preservedMP = Math.Max(unit.GetCombatMovementCost(), unit.GetIntelMovementCost());
                        unit.ForceSetMovementPoints(preservedMP);
                    }
                    else
                    {
                        unit.ForceSetMovementPoints(0);
                    }
                    break;
            }
        }

        /* ─────────────────────────────────────────────────────────────────────────────────────────
         * ⚠ DIAGNOSTIC PASS 2026-08-11 — flip false (or delete) once §6.9 ambush is CONFIRMED IN PLAY.
         * A play-test cannot tell a WHIFFED ambush (natural 0 on the band roll = a miss, a legitimate
         * §7.6 outcome — the halt still costs the whole turn) from a swallowed resolution failure, and
         * cannot tell "no ambush" from "ambush skipped by a filter". These logs name the case exactly.
         * Filter the Console on [AMBUSH DEBUG].
         * ───────────────────────────────────────────────────────────────────────────────────────── */
        private static readonly bool AMBUSH_DEBUG = true;

        /// <summary>
        /// Diagnostic (see the flag above): for the hex just entered, names WHY each adjacent enemy did
        /// not spring an ambush — mirrors <see cref="SpottingService.CheckGroundAmbush"/>'s filters in
        /// the same order. Silent when no enemy is adjacent.
        /// </summary>
        private static void DebugLogAmbushScan(CombatUnit mover, Position2D enteredHex, ISet<string> alreadySprung)
        {
            var gdm = GameDataManager.Instance;
            if (gdm == null) return;

            foreach (var neighborPos in HexMapUtil.GetAllNeighborPositions(enteredHex))
            {
                var ground = gdm.GetGroundUnitAtHex(neighborPos);
                if (ground == null || ground.Side == mover.Side) continue;

                string reason =
                    ground.SpottedLevel != SpottedLevel.Level0
                        ? $"already spotted ({ground.SpottedLevel}) — a Level0-at-adjacency miss here would be the old disarm bug"
                    : !ground.ProjectsZoC
                        ? "projects no ZoC (embarked or base)"
                    : !GameData.IsAmbushEligible(ground.Classification)
                        ? $"ineligible class per §6.9.9 ({ground.Classification})"
                    : alreadySprung.Contains(ground.UnitID)
                        ? "already sprang this move (anti-dogpile §11.8.6)"
                        : "NO FILTER MATCHES — CheckGroundAmbush disagrees with this scan; investigate";

                Debug.Log($"[AMBUSH DEBUG] {ground.UnitName} ({ground.Classification}) at {neighborPos}, " +
                          $"adjacent to {mover.UnitName} entering {enteredHex}, did NOT spring: {reason}");
            }
        }

        /// <summary>
        /// Applies the objective-capture consequences of a move's territory changes (§17.5.3 / §18.2.1).
        /// A PLAYER capture credits the hex's VictoryValue in prestige and bumps the held-objective count
        /// (which runs the immediate-win check); an AI capture of a player-held (Red) objective decrements
        /// it. Plain tile flips (non-objective) carry no prestige. Routed through BattleManager so the HUD,
        /// victory checks, and prestige counters stay in sync.
        /// </summary>
        private void ApplyTerritoryAccounting(TerritoryChangeResult territory)
        {
            if (territory.CapturedObjectives == null || territory.CapturedObjectives.Count == 0)
                return;

            var bm = BattleManager.Instance;
            if (bm == null) return;

            foreach (var cap in territory.CapturedObjectives)
            {
                if (CurrentUnit.Side == Side.Player)
                {
                    int prestige = Mathf.RoundToInt(cap.VictoryValue);
                    bm.AddPrestige(prestige);
                    bm.CaptureObjective();

                    // §24.8.6 — see PrinterDispatch.
                    PrinterDispatch.ReportObjectiveCaptured(cap.Position, prestige);

                    // Ungated (§27.7.4): an objective flip is a fact about the MAP, not about a unit.
                    GameAudio.Play(SFX.ObjectiveCaptured);
                }
                else if (cap.PreviousControl == TileControl.Red)
                {
                    bm.LoseObjective();
                    PrinterDispatch.ReportObjectiveLost(cap.Position);
                    GameAudio.Play(SFX.ObjectiveLost);
                }
            }
        }

        #endregion // Movement Execution

        #region Facing Rotation

        private void HandleFacingRotation(Position2D hexPos)
        {
            try
            {
                if (CurrentUnit == null) return;

                var dir = HexMapUtil.GetDirectionBetween(CurrentUnit.MapPos, hexPos);
                if (!dir.HasValue) return;

                if (CurrentUnit.TryRotateFacing(dir.Value))
                {
                    // The icon's sprite variant + flip derive from Facing — refresh it (same gap as
                    // movement: icons only resolved facing at create time before 2026-07-22).
                    GameIconRenderer.Instance?.RefreshIconFacing(CurrentUnit.UnitID);

                    if (EventManager.Instance != null)
                        EventManager.Instance.RaiseUnitMovementPointsChanged(CurrentUnit);

                    // Recompute range with updated MP (clears the overlay if rotation spent the last MP)
                    RecomputeRangeAndRaise(GameDataManager.CurrentHexMap);

                    // ⚠ INSIDE the success branch. A rotation the unit could not pay for is a refusal, and
                    // sounding it as a rotation would tell the player something happened when nothing did.
                    GameAudio.PlayFrom(SFX.FacingChange, CurrentUnit);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleFacingRotation), e);
            }
        }

        #endregion // Facing Rotation

        #region Unit Cycling (Task 7)

        private void BuildEligibleUnitsList()
        {
            try
            {
                _eligibleUnits = GameDataManager.Instance.GetPlayerUnits()
                    .Where(u => u.CanMove()
                             && u.MoveActions.Current > 0
                             && u.MovementPoints.Current > 0
                             && !u.IsBase
                             && u.EfficiencyLevel != EfficiencyLevel.StaticOperations)
                    .ToList();
                _cycleIndex = -1;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildEligibleUnitsList), e);
            }
        }

        private void HandleMoveCompleted(CombatUnit unit)
        {
            // Remove unit from eligible list if it can no longer move
            if (unit != null)
            {
                _eligibleUnits.RemoveAll(u => u == unit
                    && (u.MoveActions.Current <= 0 || u.MovementPoints.Current <= 0));
            }
        }

        private void CycleNext()
        {
            try
            {
                if (_eligibleUnits.Count == 0) return;

                // Find next eligible unit (skip exhausted)
                int startIndex = _cycleIndex;
                for (int i = 0; i < _eligibleUnits.Count; i++)
                {
                    _cycleIndex = (_cycleIndex + 1) % _eligibleUnits.Count;
                    var candidate = _eligibleUnits[_cycleIndex];

                    if (candidate.MoveActions.Current > 0 && candidate.MovementPoints.Current > 0)
                    {
                        SelectUnit(candidate);
                        if (EventManager.Instance != null)
                            EventManager.Instance.RaiseCurrentUnitChanged(candidate);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CycleNext), e);
            }
        }

        private void CyclePrevious()
        {
            try
            {
                if (_eligibleUnits.Count == 0) return;

                for (int i = 0; i < _eligibleUnits.Count; i++)
                {
                    _cycleIndex = (_cycleIndex - 1 + _eligibleUnits.Count) % _eligibleUnits.Count;
                    var candidate = _eligibleUnits[_cycleIndex];

                    if (candidate.MoveActions.Current > 0 && candidate.MovementPoints.Current > 0)
                    {
                        SelectUnit(candidate);
                        if (EventManager.Instance != null)
                            EventManager.Instance.RaiseCurrentUnitChanged(candidate);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CyclePrevious), e);
            }
        }

        #endregion // Unit Cycling

        // TODO: Future keybindings — Tab/Shift-Tab for next/prev, Space for end-unit-turn, Esc for cancel target
    }
}
