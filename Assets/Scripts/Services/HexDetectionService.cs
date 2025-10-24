// ============================================================================
// ⚠️ ⚠️ ⚠️  JSON SERIALIZATION WARNING  ⚠️ ⚠️ ⚠️
// ============================================================================
// This project uses NEWTONSOFT.JSON (Json.NET) for ALL JSON serialization!
//
// DO NOT USE: System.Text.Json
// DO USE:     Newtonsoft.Json
//
// Why? Unity compatibility, private field serialization, and existing
// codebase consistency. System.Text.Json does NOT work properly with Unity's
// [SerializeField] attributes and private field patterns used throughout
// this project.
//
// Correct usage:
//   using Newtonsoft.Json;
//   var obj = JsonConvert.DeserializeObject<T>(json);
//   var json = JsonConvert.SerializeObject(obj);
// ============================================================================

using UnityEngine;
using System;
using HammerAndSickle.Controllers;
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

        [SerializeField]
        [Tooltip("Unity Grid component used for coordinate conversion")]
        private Grid hexGrid;

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
        /// <summary>
        /// Fired when a hex is selected or deselected. Position will be NoHexSelected for deselection.
        /// </summary>
        public event Action<Position2D> OnHexSelected
        {
            add { AddSubscriber("OnHexSelected", value.Target, ref _onHexSelected, value); }
            remove { RemoveSubscriber("OnHexSelected", value.Target, ref _onHexSelected, value); }
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
            if (hexGrid == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(hexGrid)} is missing.");
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
        /// Handles right mouse click events, resetting the hex selection.
        /// </summary>
        /// <param name="mousePosition">Screen position of the mouse click</param>
        private void HandleRightMouseClick(Vector2 mousePosition)
        {
            try
            {
                // Reset hex selection to NoHexSelected
                GameDataManager.SelectedHex = GameDataManager.NoHexSelected;
                _onHexSelected?.Invoke(GameDataManager.NoHexSelected);

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Hex selection cleared");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleRightMouseClick), e);
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
            // Convert mouse position to world position
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));

            // Convert world position to cell position
            Vector3Int cellPosition = hexGrid.WorldToCell(worldPosition);
            Position2D gridPosition = new Position2D(cellPosition.x, cellPosition.y);

            // Validate the coordinates are within the map bounds
            if (IsValidHexPosition(gridPosition))
            {
                return gridPosition;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"{CLASS_NAME}: Click detected outside valid hex map bounds at: {gridPosition}");
            }

            return GameDataManager.NoHexSelected;
        }

        /// <summary>
        /// Validates if a position is within the hex map bounds.
        /// </summary>
        /// <param name="position">Position to validate</param>
        /// <returns>True if position is valid, false otherwise</returns>
        private bool IsValidHexPosition(Position2D position)
        {
            if (GameDataManager.CurrentHexMap == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"{CLASS_NAME}: CurrentHexMap is null, cannot validate position");
                }
                return false;
            }

            // Check basic bounds
            if (position.X < 0 || position.Y < 0 ||
                position.X >= GameDataManager.CurrentHexMap.MapSize.x ||
                position.Y >= GameDataManager.CurrentHexMap.MapSize.y)
            {
                return false;
            }

            // Check the extra rule for odd rows (they have one less column in odd-r alignment)
            if (position.Y % 2 != 0 && position.X >= GameDataManager.CurrentHexMap.MapSize.x - 1)
            {
                return false;
            }

            return true;
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

                    // Clear event delegate
                    _onHexSelected = null;

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
