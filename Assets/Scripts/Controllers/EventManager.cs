using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Centralized event management system for game-wide event dispatching and subscription.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(EventManager);

        #region Singleton

        private static EventManager _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<EventManager>();

                    if (_instance == null)
                    {
                        GameObject go = new("EventManager");
                        _instance = go.AddComponent<EventManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Battle Scene Events

        /// <summary>
        /// Fired when all map icons need to be redrawn.
        /// </summary>
        public event Action OnRedrawMapIcons;

        /// <summary>
        /// Fired when a unit's hit points change. Carries the unit ID and current HP as a percentage (1-100).
        /// </summary>
        public event Action<string, int> OnUnitHitPointsChanged;

        /// <summary>
        /// Fired when a unit's deployment position changes. Carries the unit ID, the new deployment position,
        /// and the embarkment state (relevant when position is Embarked).
        /// </summary>
        public event Action<string, DeploymentPosition, EmbarkmentState> OnUnitDeploymentChanged;

        /// <summary>
        /// Fired when the player toggles air/ground stacking dominance at a hex position.
        /// </summary>
        public event Action<Position2D> OnStackingToggleRequested;

        /// <summary>
        /// Fired when a structured message should be displayed on the HQ printer.
        /// </summary>
        public event Action<PrinterMessage> OnPrinterMessage;

        /// <summary>
        /// Fired whenever the BattleManager transitions into a new BattlePhase.
        /// Carries the new phase. Subscribers can react to phase entry (e.g. UI gating, audio cues).
        /// </summary>
        public event Action<BattlePhase> OnBattlePhaseChanged;

        /// <summary>
        /// Fired whenever the BattleManager advances the turn counter.
        /// Carries the new (current) turn number. Turn 0 is the deployment phase, Turn 1+ are played turns.
        /// </summary>
        public event Action<int> OnBattleTurnAdvanced;

        /// <summary>
        /// Fired exactly once when the battle ends, either via reaching the final turn,
        /// an immediate victory (all objectives held), or other terminal condition.
        /// Carries the final BattleResult.
        /// </summary>
        public event Action<BattleResult> OnBattleEnded;

        #endregion // Battle Scene Events

        #region Movement Events

        // See EventManager for all game events
        public event Action<CombatUnit> OnPlayerUnitSelected;
        public event Action OnPlayerUnitDeselected;
        public event Action<CombatUnit, List<Position2D>> OnUnitMoveStarted;
        public event Action<CombatUnit> OnUnitMoveCompleted;
        public event Action<CombatUnit> OnUnitMovementPointsChanged;
        public event Action<CombatUnit> OnUnitActionsChanged;

        #endregion // Movement Events

        #region Spotting Events

        // See EventManager for all game events
        public event Action<CombatUnit, SpottedLevel, SpottedLevel> OnUnitSpottedLevelChanged;

        #endregion // Spotting Events

        #region Movement Range Display Events

        // See EventManager for all game events
        public event Action<CombatUnit, Dictionary<Position2D, int>> OnMovementRangeComputed;
        public event Action OnMovementRangeCleared;

        #endregion // Movement Range Display Events

        #region Unit Cycling Events

        // See EventManager for all game events
        public event Action OnNextUnitRequested;
        public event Action OnPreviousUnitRequested;
        public event Action<CombatUnit> OnCurrentUnitChanged;

        #endregion // Unit Cycling Events

        #region Ambush Events

        // See EventManager for all game events
        public event Action<CombatUnit, CombatUnit> OnAmbushTriggered;
        public event Action<CombatUnit, CombatUnit> OnAirAmbushDetected;

        #endregion // Ambush Events

        #region Air Auto-Return Events

        // See EventManager for all game events
        public event Action<CombatUnit> OnAirUnitReturning;

        #endregion // Air Auto-Return Events

        #region Dialog Events

        /// <summary>
        /// Fired when a dialog change is requested in Scene 0 (Main Menu).
        /// The UIPanel parameter is the target dialog to show.
        /// </summary>
        public event Action<UIPanel> OnScene0DialogRequested;

        /// <summary>
        /// Fired when a dialog change is requested in Scene 1 (Battle).
        /// The UIPanel parameter is the target dialog to show.
        /// </summary>
        public event Action<UIPanel> OnScene1DialogRequested;

        #endregion // Dialog Events

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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Event Raising Methods

        /// <summary>
        /// Raises the redraw map icons event.
        /// </summary>
        public void RaiseRedrawMapIcons()
        {
            try
            {
                OnRedrawMapIcons?.Invoke();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseRedrawMapIcons), e);
            }
        }

        /// <summary>
        /// Raises the unit hit points changed event.
        /// </summary>
        /// <param name="unitId">The ID of the unit whose hit points changed</param>
        /// <param name="currentPercent">Current hit points as a percentage (1-100)</param>
        public void RaiseUnitHitPointsChanged(string unitId, int currentPercent)
        {
            try
            {
                OnUnitHitPointsChanged?.Invoke(unitId, currentPercent);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitHitPointsChanged), e);
            }
        }

        /// <summary>
        /// Raises the unit deployment changed event.
        /// </summary>
        /// <param name="unitId">The ID of the unit whose deployment changed</param>
        /// <param name="newPosition">The new deployment position</param>
        /// <param name="embarkmentState">The embarkment state (relevant when position is Embarked)</param>
        public void RaiseUnitDeploymentChanged(string unitId, DeploymentPosition newPosition, EmbarkmentState embarkmentState)
        {
            try
            {
                OnUnitDeploymentChanged?.Invoke(unitId, newPosition, embarkmentState);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitDeploymentChanged), e);
            }
        }

        /// <summary>
        /// Raises the stacking toggle requested event.
        /// </summary>
        /// <param name="position">The hex position to toggle stacking dominance at</param>
        public void RaiseStackingToggleRequested(Position2D position)
        {
            try
            {
                OnStackingToggleRequested?.Invoke(position);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseStackingToggleRequested), e);
            }
        }

        /// <summary>
        /// Raises the printer message event.
        /// </summary>
        /// <param name="message">The structured message to print</param>
        public void RaisePrinterMessage(PrinterMessage message)
        {
            try
            {
                OnPrinterMessage?.Invoke(message);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaisePrinterMessage), e);
            }
        }

        /// <summary>
        /// Raises the battle phase changed event.
        /// </summary>
        /// <param name="newPhase">The phase the BattleManager has just entered</param>
        public void RaiseBattlePhaseChanged(BattlePhase newPhase)
        {
            try
            {
                OnBattlePhaseChanged?.Invoke(newPhase);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseBattlePhaseChanged), e);
            }
        }

        /// <summary>
        /// Raises the battle turn advanced event.
        /// </summary>
        /// <param name="newTurn">The new (current) turn number after the advance</param>
        public void RaiseBattleTurnAdvanced(int newTurn)
        {
            try
            {
                OnBattleTurnAdvanced?.Invoke(newTurn);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseBattleTurnAdvanced), e);
            }
        }

        /// <summary>
        /// Raises the battle ended event.
        /// </summary>
        /// <param name="finalResult">The final BattleResult that ended the scenario</param>
        public void RaiseBattleEnded(BattleResult finalResult)
        {
            try
            {
                OnBattleEnded?.Invoke(finalResult);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseBattleEnded), e);
            }
        }

        #region Movement Event Raising Methods

        public void RaisePlayerUnitSelected(CombatUnit unit)
        {
            try { OnPlayerUnitSelected?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaisePlayerUnitSelected), e); }
        }

        public void RaisePlayerUnitDeselected()
        {
            try { OnPlayerUnitDeselected?.Invoke(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaisePlayerUnitDeselected), e); }
        }

        public void RaiseUnitMoveStarted(CombatUnit unit, List<Position2D> path)
        {
            try { OnUnitMoveStarted?.Invoke(unit, path); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseUnitMoveStarted), e); }
        }

        public void RaiseUnitMoveCompleted(CombatUnit unit)
        {
            try { OnUnitMoveCompleted?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseUnitMoveCompleted), e); }
        }

        public void RaiseUnitMovementPointsChanged(CombatUnit unit)
        {
            try { OnUnitMovementPointsChanged?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseUnitMovementPointsChanged), e); }
        }

        public void RaiseUnitActionsChanged(CombatUnit unit)
        {
            try { OnUnitActionsChanged?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseUnitActionsChanged), e); }
        }

        public void RaiseUnitSpottedLevelChanged(CombatUnit unit, SpottedLevel oldLevel, SpottedLevel newLevel)
        {
            try { OnUnitSpottedLevelChanged?.Invoke(unit, oldLevel, newLevel); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseUnitSpottedLevelChanged), e); }
        }

        public void RaiseMovementRangeComputed(CombatUnit unit, Dictionary<Position2D, int> reachable)
        {
            try { OnMovementRangeComputed?.Invoke(unit, reachable); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseMovementRangeComputed), e); }
        }

        public void RaiseMovementRangeCleared()
        {
            try { OnMovementRangeCleared?.Invoke(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseMovementRangeCleared), e); }
        }

        public void RaiseNextUnitRequested()
        {
            try { OnNextUnitRequested?.Invoke(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseNextUnitRequested), e); }
        }

        public void RaisePreviousUnitRequested()
        {
            try { OnPreviousUnitRequested?.Invoke(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaisePreviousUnitRequested), e); }
        }

        public void RaiseCurrentUnitChanged(CombatUnit unit)
        {
            try { OnCurrentUnitChanged?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseCurrentUnitChanged), e); }
        }

        public void RaiseAmbushTriggered(CombatUnit ambusher, CombatUnit victim)
        {
            try { OnAmbushTriggered?.Invoke(ambusher, victim); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseAmbushTriggered), e); }
        }

        public void RaiseAirAmbushDetected(CombatUnit aaUnit, CombatUnit airUnit)
        {
            try { OnAirAmbushDetected?.Invoke(aaUnit, airUnit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseAirAmbushDetected), e); }
        }

        public void RaiseAirUnitReturning(CombatUnit unit)
        {
            try { OnAirUnitReturning?.Invoke(unit); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(RaiseAirUnitReturning), e); }
        }

        #endregion // Movement Event Raising Methods

        #region Dialog Event Raising Methods

        /// <summary>
        /// Raises a Scene 0 dialog change request.
        /// </summary>
        /// <param name="dialog">The target dialog panel to show</param>
        public void RaiseScene0DialogRequested(UIPanel dialog)
        {
            try
            {
                OnScene0DialogRequested?.Invoke(dialog);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseScene0DialogRequested), e);
            }
        }

        /// <summary>
        /// Raises a Scene 1 dialog change request.
        /// </summary>
        /// <param name="dialog">The target dialog panel to show</param>
        public void RaiseScene1DialogRequested(UIPanel dialog)
        {
            try
            {
                OnScene1DialogRequested?.Invoke(dialog);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseScene1DialogRequested), e);
            }
        }

        #endregion // Dialog Event Raising Methods

        #endregion // Event Raising Methods

        #region Utility Methods

        /// <summary>
        /// Clears all event subscriptions. Use with caution - typically only needed during scene transitions.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            // Battle scene events
            OnRedrawMapIcons = null;
            OnUnitHitPointsChanged = null;
            OnUnitDeploymentChanged = null;
            OnStackingToggleRequested = null;
            OnPrinterMessage = null;
            OnBattlePhaseChanged = null;
            OnBattleTurnAdvanced = null;
            OnBattleEnded = null;

            // Movement events
            OnPlayerUnitSelected = null;
            OnPlayerUnitDeselected = null;
            OnUnitMoveStarted = null;
            OnUnitMoveCompleted = null;
            OnUnitMovementPointsChanged = null;
            OnUnitActionsChanged = null;

            // Spotting events
            OnUnitSpottedLevelChanged = null;

            // Movement range display events
            OnMovementRangeComputed = null;
            OnMovementRangeCleared = null;

            // Unit cycling events
            OnNextUnitRequested = null;
            OnPreviousUnitRequested = null;
            OnCurrentUnitChanged = null;

            // Ambush events
            OnAmbushTriggered = null;
            OnAirAmbushDetected = null;

            // Air auto-return events
            OnAirUnitReturning = null;

            // Dialog events
            OnScene0DialogRequested = null;
            OnScene1DialogRequested = null;
        }

        #endregion // Utility Methods
    }
}
