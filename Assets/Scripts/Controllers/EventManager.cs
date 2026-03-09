using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
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

        #region Events

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

        #endregion // Events

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

        #endregion // Event Raising Methods

        #region Utility Methods

        /// <summary>
        /// Clears all event subscriptions. Use with caution - typically only needed during scene transitions.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            OnRedrawMapIcons = null;
            OnUnitHitPointsChanged = null;
            OnUnitDeploymentChanged = null;
            OnStackingToggleRequested = null;
        }

        #endregion // Utility Methods
    }
}
