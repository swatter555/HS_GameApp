using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Movement state machine for player-controlled unit movement during BattlePhase.PlayerTurn.
    /// </summary>
    public enum MovementState
    {
        Idle,
        UnitSelected,
        AwaitingTarget,
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

        private void OnEnable()
        {
            SubscribeToEvents();
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
                HexDetectionService.Instance.OnHexSelected += HandleHexSelected;

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
                HexDetectionService.Instance.OnHexSelected -= HandleHexSelected;

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

                // Check for Shift+click facing rotation
                if (State == MovementState.UnitSelected && CurrentUnit != null && Input.GetKey(KeyCode.LeftShift))
                {
                    HandleFacingRotation(hexPos);
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

                    case MovementState.AwaitingTarget:
                        HandleAwaitingTargetClick(hexPos);
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

                _currentRange = HexMapUtil.GetValidMoveDestinations(map, unit);
                State = MovementState.UnitSelected;

                if (EventManager.Instance != null)
                {
                    EventManager.Instance.RaisePlayerUnitSelected(unit);
                    EventManager.Instance.RaiseMovementRangeComputed(unit, _currentRange.Reachable);
                }

                CameraService.Instance?.CenterOnPosition(unit.MapPos);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SelectUnit), e);
            }
        }

        private void HandleUnitSelectedClick(Position2D hexPos)
        {
            var gdm = GameDataManager.Instance;

            // Click on a different friendly unit → re-select
            var ground = gdm.GetGroundUnitAtHex(hexPos);
            var air = gdm.GetAirUnitAtHex(hexPos);
            var clickedUnit = ground ?? air;

            if (clickedUnit != null && clickedUnit.Side == Side.Player && clickedUnit != CurrentUnit)
            {
                SelectUnit(clickedUnit);
                return;
            }

            // Click within range → compute path and show preview
            if (_currentRange.Reachable.ContainsKey(hexPos))
            {
                var map = GameDataManager.CurrentHexMap;
                _currentPath = HexMapUtil.FindPath(map, CurrentUnit, CurrentUnit.MapPos, hexPos);
                State = MovementState.AwaitingTarget;
                // TODO: Show path preview via renderer
                return;
            }

            // Click on empty hex outside range → deselect
            if (clickedUnit == null)
            {
                DeselectUnit();
                return;
            }

            // Click on non-reachable hex with a unit → SFX denial
            // TODO: Play UnitMoveBlocked SFX
        }

        private void HandleAwaitingTargetClick(Position2D hexPos)
        {
            if (_currentPath == null || _currentPath.Count == 0)
            {
                State = MovementState.UnitSelected;
                return;
            }

            var destination = _currentPath[_currentPath.Count - 1].Position;

            // Confirm: click on the same destination → execute
            if (hexPos == destination)
            {
                StartCoroutine(ExecuteMovement());
                return;
            }

            // Click on a different reachable hex → recompute path
            if (_currentRange.Reachable.ContainsKey(hexPos))
            {
                var map = GameDataManager.CurrentHexMap;
                _currentPath = HexMapUtil.FindPath(map, CurrentUnit, CurrentUnit.MapPos, hexPos);
                return;
            }

            // Click outside range → cancel back to UnitSelected
            State = MovementState.UnitSelected;
            _currentPath = null;
        }

        private void DeselectUnit()
        {
            CurrentUnit = null;
            _currentPath = null;
            State = MovementState.Idle;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaisePlayerUnitDeselected();
                EventManager.Instance.RaiseMovementRangeCleared();
            }
        }

        #endregion // Selection Flow

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

                // Animate hex step
                // TODO: Integrate UnitMoveAnimator.AnimateHexStep here
                yield return new WaitForSeconds(isFixedWing ? 0.08f : 0.18f);

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
                    EventManager.Instance.RaiseMovementRangeComputed(CurrentUnit, updatedRange.Reachable);
                }

                if (EventManager.Instance != null)
                    EventManager.Instance.RaiseUnitMovementPointsChanged(CurrentUnit);
            }

            // Move complete
            CameraService.Instance?.CenterOnPosition(CurrentUnit.MapPos);

            if (EventManager.Instance != null)
            {
                EventManager.Instance.RaiseUnitMoveCompleted(CurrentUnit);
                EventManager.Instance.RaiseUnitActionsChanged(CurrentUnit);
            }

            GameDataManager.Instance.BuildOccupancyCache();

            // Return to UnitSelected if unit still has actions, otherwise Idle
            bool hasActionsLeft = CurrentUnit.MoveActions.Current > 0
                               && CurrentUnit.MovementPoints.Current > 0;
            if (hasActionsLeft)
            {
                _currentRange = HexMapUtil.GetValidMoveDestinations(map, CurrentUnit);
                State = MovementState.UnitSelected;
                if (EventManager.Instance != null)
                    EventManager.Instance.RaiseMovementRangeComputed(CurrentUnit, _currentRange.Reachable);
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
                    if (EventManager.Instance != null)
                        EventManager.Instance.RaiseUnitMovementPointsChanged(CurrentUnit);

                    // Recompute range with updated MP
                    var map = GameDataManager.CurrentHexMap;
                    _currentRange = HexMapUtil.GetValidMoveDestinations(map, CurrentUnit);
                    if (EventManager.Instance != null)
                        EventManager.Instance.RaiseMovementRangeComputed(CurrentUnit, _currentRange.Reachable);
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
