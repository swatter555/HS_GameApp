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
            }
        }

        #endregion // Event Subscriptions

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
                // No attacker selected → nothing to engage with.
                // TODO: Play denial SFX
                return;
            }

            var gdm = GameDataManager.Instance;
            var target = gdm.GetGroundUnitAtHex(hexPos) ?? gdm.GetAirUnitAtHex(hexPos);

            if (target == null || target.Side == Side.Player)
            {
                // TODO: Play denial SFX
                return;
            }

            TryAttack(target);
        }

        private void DeselectUnit()
        {
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

                if (CombatResolver.IsIndirectFireClass(CurrentUnit.Classification))
                {
                    IndirectCombatOutcome o = IndirectCombatAction.Execute(CurrentUnit, target, map, new CombatRandom());
                    executed = o.Executed;
                    message = o.Executed ? BuildIndirectMessage(CurrentUnit, target, o) : o.Reason;
                    attackerDestroyed = o.FirerDestroyed;
                }
                else
                {
                    // TODO §7.5.6.9.1 — compute contestedCrossing from river/bridge geometry between the two hexes.
                    GroundCombatOutcome o = GroundCombatAction.Execute(CurrentUnit, target, map, new CombatRandom());
                    executed = o.Executed;
                    message = o.Executed ? BuildCombatMessage(CurrentUnit, target, o) : o.Reason;
                    attackerDestroyed = o.AttackerDestroyed;
                }

                if (!executed)
                {
                    AppService.CaptureUiMessage(message);
                    // TODO: Play denial SFX
                    return;
                }

                AppService.CaptureUiMessage(message);

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
                // TODO: Play OutOfMP SFX
                State = MovementState.UnitSelected;
                yield break;
            }

            if (EventManager.Instance != null)
            {
                var pathPositions = _currentPath.ConvertAll(t => t.Position);
                EventManager.Instance.RaiseUnitMoveStarted(CurrentUnit, pathPositions);
            }

            var map = GameDataManager.CurrentHexMap;
            bool isAir = CurrentUnit.IsAirUnit || CurrentUnit.IsHelicopter;
            bool isFixedWing = CurrentUnit.IsFixedWingAirUnit;
            Position2D previousPos = CurrentUnit.MapPos;
            Position2D originPos = CurrentUnit.MapPos;   // for the post-move stacking refresh

            // Hexes actually entered this move (in order; last = where the unit ends). Drives the
            // §6.13 tile-control flips after the move settles. May be shorter than the planned path
            // if an ambush / ZoC halt cuts the move short.
            var enteredHexes = new List<Position2D>();

            // TODO: Move undo — allowed only when no new spotting events fired during the move

            for (int i = 0; i < _currentPath.Count; i++)
            {
                var targetTile = _currentPath[i];
                var targetPos = targetTile.Position;

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
                float stepDuration = isFixedWing ? 0.08f : 0.18f;
                var iconRenderer = GameIconRenderer.Instance;
                if (iconRenderer != null)
                {
                    // Turn the icon INTO the step direction before it glides — MoveUnitTo above already
                    // rotated unit.Facing toward this step; the icon re-resolves sprite + flip from it.
                    iconRenderer.RefreshIconFacing(CurrentUnit.UnitID);

                    bool stepDone = false;
                    iconRenderer.AnimateIconStep(CurrentUnit.UnitID, targetPos, stepDuration, () => stepDone = true);
                    yield return new WaitUntil(() => stepDone);
                }
                else
                {
                    // Headless / no renderer (tests) — keep the cadence without animating.
                    yield return new WaitForSeconds(stepDuration);
                }

                // Spotting pass
                var newlySpotted = SpottingService.CheckSpottingForMover(CurrentUnit, targetPos);

                // Ground ambush check
                if (!isAir && newlySpotted.Count > 0)
                {
                    var ambusher = SpottingService.CheckGroundAmbush(CurrentUnit, targetPos);
                    if (ambusher != null)
                    {
                        ApplyAbnormalHalt(CurrentUnit, true);

                        if (EventManager.Instance != null)
                            EventManager.Instance.RaiseAmbushTriggered(ambusher, CurrentUnit);

                        break;
                    }
                }

                // Air ambush check
                if (isAir)
                {
                    var airResult = SpottingService.CheckAirAmbush(CurrentUnit, targetPos);
                    if (airResult == AirAmbushResult.Ambushed)
                    {
                        // TODO: Combat resolution for air ambush
                        // 50% chance to continue
                        bool continues = UnityEngine.Random.Range(0, 2) == 0;
                        if (!continues)
                        {
                            CurrentUnit.ForceSetMovementPoints(0);
                            CurrentUnit.ForceSetActions(0, 0, 0);
                            break;
                        }
                    }
                }

                // ZoC-to-ZoC check (ground only)
                if (!isAir && _currentRange.ZocTerminals.Contains(targetPos))
                {
                    ApplyAbnormalHalt(CurrentUnit, false);
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

            // Return to UnitSelected if the unit could still begin another move, otherwise Idle
            if (CurrentUnit.CanBeginMoveOrder())
            {
                State = MovementState.UnitSelected;
                RecomputeRangeAndRaise(map);
            }
            else
            {
                DeselectUnit();
            }
        }

        /// <summary>
        /// Applies the abnormal movement halt rules (ZoC-to-ZoC, ground ambush).
        /// </summary>
        private void ApplyAbnormalHalt(CombatUnit unit, bool isAmbush)
        {
            unit.MoveActions.SetCurrent(0);

            if (isAmbush)
            {
                // Full ambush: everything zeroed
                unit.ForceSetMovementPoints(0);
                unit.ForceSetActions(0, 0, 0);
            }
            else
            {
                // ZoC halt: preserve MP for combat/intel if actions remain
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
                    bm.AddPrestige(Mathf.RoundToInt(cap.VictoryValue));
                    bm.CaptureObjective();
                }
                else if (cap.PreviousControl == TileControl.Red)
                {
                    bm.LoseObjective();
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
                }
                // TODO: Play FacingChange SFX
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
