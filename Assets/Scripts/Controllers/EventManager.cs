using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Centralized event management system for game-wide event dispatching and subscription.
    /// Provides type-safe events for unit state changes, combat actions, and other game events.
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
                    // Try to find existing instance in scene
                    _instance = FindAnyObjectByType<EventManager>();

                    // Create new instance if none exists
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

        #region Event Delegates

        /// <summary>
        /// Event handler for unit deployment position changes.
        /// </summary>
        public delegate void UnitDeploymentChangedHandler(UnitDeploymentChangedEventArgs args);

        /// <summary>
        /// Event handler for unit position changes on the map.
        /// </summary>
        public delegate void UnitPositionChangedHandler(UnitPositionChangedEventArgs args);

        /// <summary>
        /// Event handler for unit damage events.
        /// </summary>
        public delegate void UnitDamagedHandler(UnitDamagedEventArgs args);

        /// <summary>
        /// Event handler for unit destruction events.
        /// </summary>
        public delegate void UnitDestroyedHandler(UnitDestroyedEventArgs args);

        #endregion // Event Delegates

        #region Events

        /// <summary>
        /// Fired when a unit changes deployment position (Mobile, Deployed, Entrenched, etc.).
        /// Subscribers should update visual representation based on new deployment state.
        /// </summary>
        public event UnitDeploymentChangedHandler OnUnitDeploymentChanged;

        /// <summary>
        /// Fired when a unit moves to a new map position.
        /// Subscribers should update unit position on the map.
        /// </summary>
        public event UnitPositionChangedHandler OnUnitPositionChanged;

        /// <summary>
        /// Fired when a unit takes damage.
        /// Subscribers can update health bars, effects, etc.
        /// </summary>
        public event UnitDamagedHandler OnUnitDamaged;

        /// <summary>
        /// Fired when a unit is destroyed.
        /// Subscribers should remove unit from map and play destruction effects.
        /// </summary>
        public event UnitDestroyedHandler OnUnitDestroyed;

        #endregion // Events

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce singleton pattern
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
        /// Raises the unit deployment changed event.
        /// </summary>
        public void RaiseUnitDeploymentChanged(string unitID, DeploymentPosition oldPosition, DeploymentPosition newPosition)
        {
            try
            {
                OnUnitDeploymentChanged?.Invoke(new UnitDeploymentChangedEventArgs(unitID, oldPosition, newPosition));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitDeploymentChanged), e);
            }
        }

        /// <summary>
        /// Raises the unit position changed event.
        /// </summary>
        public void RaiseUnitPositionChanged(string unitID, Position2D oldPosition, Position2D newPosition)
        {
            try
            {
                OnUnitPositionChanged?.Invoke(new UnitPositionChangedEventArgs(unitID, oldPosition, newPosition));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitPositionChanged), e);
            }
        }

        /// <summary>
        /// Raises the unit damaged event.
        /// </summary>
        public void RaiseUnitDamaged(string unitID, float damageAmount, float currentHP, float maxHP)
        {
            try
            {
                OnUnitDamaged?.Invoke(new UnitDamagedEventArgs(unitID, damageAmount, currentHP, maxHP));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitDamaged), e);
            }
        }

        /// <summary>
        /// Raises the unit destroyed event.
        /// </summary>
        public void RaiseUnitDestroyed(string unitID)
        {
            try
            {
                OnUnitDestroyed?.Invoke(new UnitDestroyedEventArgs(unitID));
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RaiseUnitDestroyed), e);
            }
        }

        #endregion // Event Raising Methods

        #region Utility Methods

        /// <summary>
        /// Clears all event subscriptions. Use with caution - typically only needed during scene transitions.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            OnUnitDeploymentChanged = null;
            OnUnitPositionChanged = null;
            OnUnitDamaged = null;
            OnUnitDestroyed = null;
        }

        #endregion // Utility Methods
    }

    #region Event Args Classes

    /// <summary>
    /// Event arguments for unit deployment position changes.
    /// </summary>
    public class UnitDeploymentChangedEventArgs : EventArgs
    {
        public string UnitID { get; }
        public DeploymentPosition OldPosition { get; }
        public DeploymentPosition NewPosition { get; }

        public UnitDeploymentChangedEventArgs(string unitID, DeploymentPosition oldPosition, DeploymentPosition newPosition)
        {
            UnitID = unitID;
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }
    }

    /// <summary>
    /// Event arguments for unit map position changes.
    /// </summary>
    public class UnitPositionChangedEventArgs : EventArgs
    {
        public string UnitID { get; }
        public Position2D OldPosition { get; }
        public Position2D NewPosition { get; }

        public UnitPositionChangedEventArgs(string unitID, Position2D oldPosition, Position2D newPosition)
        {
            UnitID = unitID;
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }
    }

    /// <summary>
    /// Event arguments for unit damage events.
    /// </summary>
    public class UnitDamagedEventArgs : EventArgs
    {
        public string UnitID { get; }
        public float DamageAmount { get; }
        public float CurrentHP { get; }
        public float MaxHP { get; }
        public float HPPercentage => MaxHP > 0 ? CurrentHP / MaxHP : 0f;

        public UnitDamagedEventArgs(string unitID, float damageAmount, float currentHP, float maxHP)
        {
            UnitID = unitID;
            DamageAmount = damageAmount;
            CurrentHP = currentHP;
            MaxHP = maxHP;
        }
    }

    /// <summary>
    /// Event arguments for unit destruction events.
    /// </summary>
    public class UnitDestroyedEventArgs : EventArgs
    {
        public string UnitID { get; }

        public UnitDestroyedEventArgs(string unitID)
        {
            UnitID = unitID;
        }
    }

    #endregion // Event Args Classes
}
