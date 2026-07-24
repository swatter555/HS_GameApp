using UnityEngine;
using System;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Models;
using System.Collections.Generic;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// Manages hex grid click detection and validation, converting screen coordinates
    /// to valid hex positions and updating GameDataManager.SelectedHex. Provides events
    /// for hex selection changes.
    /// </summary>
    public class HexDetectionService : MonoBehaviour, IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexDetectionService);

        #endregion // Constants

        #region Singleton

        private static HexDetectionService _instance;

        /// <summary>
        /// Singleton instance of the service.
        /// </summary>
        public static HexDetectionService Instance
        {
            get { return _instance; }
            private set { _instance = value; }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Enable debug logging for hex detection and validation")]
        private bool enableDebugLogging = false;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Indicates if the service is properly initialized with required components.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates if the service has been disposed and cleaned up.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion // Properties
        
        #region Private Fields

        // Tracks if the service has completed its Start phase
        private bool isStarted;

        #endregion // Private Fields

        #region Events and Delegates

        // Dictionary to track subscribers for leak detection
        private readonly Dictionary<string, HashSet<object>> eventSubscribers =
            new Dictionary<string, HashSet<object>>();

        private Action<Position2D> _onHexSelected;

        // Fired when a hex is selected or deselected. Position will be NoHexSelected for deselection.
        public event Action<Position2D> OnHexSelected
        {
            add { AddSubscriber("OnHexSelected", value.Target, ref _onHexSelected, value); }
            remove { RemoveSubscriber("OnHexSelected", value.Target, ref _onHexSelected, value); }
        }

        private Action<Position2D> _onHexRightClicked;

        // Fired on a right-click with the hex under the cursor (NoHexSelected when off-map). The subscriber
        // decides what the right-click MEANS — move inside the radius vs clear otherwise (§5.10.4 / §5.10.5);
        // this service no longer clears unconditionally (input rework 2026-07-06).
        public event Action<Position2D> OnHexRightClicked
        {
            add { AddSubscriber("OnHexRightClicked", value.Target, ref _onHexRightClicked, value); }
            remove { RemoveSubscriber("OnHexRightClicked", value.Target, ref _onHexRightClicked, value); }
        }

        #endregion // Events and Delegates

        #region Unity Lifecycle

        /// <summary>
        /// Initializes the singleton instance and validates components.
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializeService();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts service operations if initialization was successful.
        /// </summary>
        private void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"{CLASS_NAME}.Start: Service failed to initialize properly.");
                return;
            }

            StartService();
        }

        /// <summary>
        /// Updates service state and validates subscribers for memory leaks.
        /// </summary>
        private void Update()
        {
            if (!IsInitialized || !isStarted || IsDisposed) return;

            // Check for dead subscribers if debug logging is enabled
            if (enableDebugLogging)
            {
                ValidateSubscribers();
            }
        }

        /// <summary>
        /// Handles cleanup when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Dispose(true);
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Initializes the service and validates required components.
        /// </summary>
        private void InitializeService()
        {
            try
            {
                ValidateComponents();
                SubscribeToEvents();
                IsInitialized = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Service initialized successfully.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeService), e);
                IsInitialized = false;
            }
        }

        /// <summary>
        /// Validates that all required components are properly referenced.
        /// </summary>
        private void ValidateComponents()
        {
            if (HexGridSystem.Instance == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: HexGridSystem singleton not found.");
        }

        /// <summary>
        /// Starts service operations after successful initialization.
        /// </summary>
        private void StartService()
        {
            if (enableDebugLogging)
            {
                Debug.Log($"{CLASS_NAME}: Service starting operations.");
            }
            isStarted = true;
        }

        #endregion // Initialization

        #region Event Management

        /// <summary>
        /// Subscribes to required input events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (InputService_BattleMap.Instance != null)
            {
                // Subscribe to left mouse click events
                InputService_BattleMap.Instance.OnLeftMouseClick += HandleLeftMouseClick;

                // Subscribe to right mouse click events
                InputService_BattleMap.Instance.OnRightMouseClick += HandleRightMouseClick;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Subscribed to InputService_BattleMap events");
                }
            }
            else
            {
                Debug.LogError($"{CLASS_NAME}.SubscribeToEvents: InputService_BattleMap instance not found.");
            }
        }

        /// <summary>
        /// Unsubscribes from all events and cleans up subscriber tracking.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (InputService_BattleMap.Instance != null)
            {
                InputService_BattleMap.Instance.OnLeftMouseClick -= HandleLeftMouseClick;
                InputService_BattleMap.Instance.OnRightMouseClick -= HandleRightMouseClick;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Unsubscribed from InputService_BattleMap events");
                }
            }

            // Clear all event subscriptions
            eventSubscribers.Clear();
        }

        /// <summary>
        /// Adds a subscriber to event tracking with comprehensive validation.
        /// </summary>
        private void AddSubscriber<T>(string eventName, object subscriber, ref Action<T> eventDelegate, Action<T> handler)
        {
            if (subscriber == null)
            {
                Debug.LogError($"{CLASS_NAME}: Attempted to add null subscriber to {eventName}");
                return;
            }

            if (!eventSubscribers.ContainsKey(eventName))
            {
                eventSubscribers[eventName] = new HashSet<object>();
            }

            eventSubscribers[eventName].Add(subscriber);
            eventDelegate += handler;

            if (enableDebugLogging)
            {
                Debug.Log($"{CLASS_NAME}: {subscriber} subscribed to {eventName}");
            }
        }

        /// <summary>
        /// Removes a subscriber from event tracking with cleanup validation.
        /// </summary>
        private void RemoveSubscriber<T>(string eventName, object subscriber, ref Action<T> eventDelegate, Action<T> handler)
        {
            if (subscriber == null)
            {
                Debug.LogError($"{CLASS_NAME}: Attempted to remove null subscriber from {eventName}");
                return;
            }

            if (eventSubscribers.ContainsKey(eventName))
            {
                eventSubscribers[eventName].Remove(subscriber);
            }

            eventDelegate -= handler;

            if (enableDebugLogging)
            {
                Debug.Log($"{CLASS_NAME}: {subscriber} unsubscribed from {eventName}");
            }
        }

        /// <summary>
        /// Validates all event subscribers and removes any that have been destroyed.
        /// </summary>
        private void ValidateSubscribers()
        {
            foreach (var kvp in eventSubscribers)
            {
                var deadSubscribers = new List<object>();

                foreach (var subscriber in kvp.Value)
                {
                    if (subscriber is MonoBehaviour mono && mono == null)
                    {
                        deadSubscribers.Add(subscriber);
                        Debug.LogWarning($"{CLASS_NAME}: Dead subscriber detected on {kvp.Key}: {subscriber}");
                    }
                }

                foreach (var dead in deadSubscribers)
                {
                    kvp.Value.Remove(dead);
                }
            }
        }

        #endregion // Event Management

        #region Event Handlers

        /// <summary>
        /// Handles left mouse click events, converting screen coordinates to valid hex positions.
        /// </summary>
        /// <param name="mousePosition">Screen position of the mouse click</param>
        private void HandleLeftMouseClick(Vector2 mousePosition)
        {
            try
            {
                Position2D gridPosition = GetValidGridCoordinates(mousePosition);

                if (gridPosition != GameDataManager.NoHexSelected)
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"{CLASS_NAME}: Valid hex clicked at grid position: {gridPosition}");
                    }

                    // Update GameDataManager and raise event
                    GameDataManager.SelectedHex = gridPosition;
                    _onHexSelected?.Invoke(gridPosition);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleLeftMouseClick), e);
            }
        }

        /// <summary>
        /// Handles right mouse click events. Reports the hex under the cursor via OnHexRightClicked and lets
        /// the subscriber (MovementController) decide: move inside the radius, clear otherwise (§5.10.4/.5).
        /// The old unconditional clear lives on as <see cref="ClearSelectionAndNotify"/> for that clear branch.
        /// </summary>
        /// <param name="mousePosition">Screen position of the mouse click</param>
        private void HandleRightMouseClick(Vector2 mousePosition)
        {
            try
            {
                Position2D gridPosition = GetValidGridCoordinates(mousePosition);
                _onHexRightClicked?.Invoke(gridPosition);

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Right-click at grid position: {gridPosition}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleRightMouseClick), e);
            }
        }

        /// <summary>
        /// Programmatically selects a hex — the same pipeline a left-click drives (sets
        /// GameDataManager.SelectedHex + notifies OnHexSelected), without the screen→hex conversion. Used by
        /// MovementController to make the selection FOLLOW a unit to its new hex after a move, so the panels
        /// and hex highlight track it (Panzer-General-style stay-selected, 2026-07-24). Re-selecting the same
        /// already-selected unit is a no-op in MovementController's handler.
        /// </summary>
        public void SelectHex(Position2D gridPosition)
        {
            try
            {
                if (gridPosition == GameDataManager.NoHexSelected) return;

                GameDataManager.SelectedHex = gridPosition;
                _onHexSelected?.Invoke(gridPosition);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SelectHex), e);
            }
        }

        /// <summary>
        /// Clears all selection state (hex, unit, leader) and notifies OnHexSelected subscribers — the §5.10.5
        /// "right-click outside the radius" branch, invoked by MovementController. Behavior-identical to the
        /// pre-rework unconditional right-click clear, so panels/printer reset exactly as before.
        /// </summary>
        public void ClearSelectionAndNotify()
        {
            try
            {
                GameDataManager.ClearSelection();
                _onHexSelected?.Invoke(GameDataManager.NoHexSelected);

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Hex selection cleared");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClearSelectionAndNotify), e);
            }
        }

        #endregion // Event Handlers

        #region Private Methods

        /// <summary>
        /// Converts screen coordinates to valid hex grid coordinates.
        /// </summary>
        /// <param name="mousePosition">Screen position to convert</param>
        /// <returns>Valid hex coordinates or NoHexSelected if invalid</returns>
        private Position2D GetValidGridCoordinates(Vector2 mousePosition)
        {
            // Convert screen position to hex via HexGridSystem
            Position2D gridPosition = HexGridSystem.Instance.ScreenToHex(
                new Vector3(mousePosition.x, mousePosition.y, 0), Camera.main);

            // Validate the coordinates are within the map bounds
            if (HexGridSystem.Instance.IsInBounds(gridPosition))
            {
                return gridPosition;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"{CLASS_NAME}: Click detected outside valid hex map bounds at: {gridPosition}");
            }

            return GameDataManager.NoHexSelected;
        }

        #endregion // Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Implements IDisposable pattern for resource cleanup.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handles resource cleanup and service shutdown.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    UnsubscribeFromEvents();

                    // Clear event delegates
                    _onHexSelected = null;
                    _onHexRightClicked = null;

                    if (enableDebugLogging)
                    {
                        Debug.Log($"{CLASS_NAME}: Service disposed.");
                    }
                }

                IsDisposed = true;
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }

        #endregion // IDisposable Implementation
    }
}
