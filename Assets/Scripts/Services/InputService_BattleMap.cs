using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HammerAndSickle.Services
{
    public class InputService_BattleMap : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(InputService_BattleMap);
        private const float SUBSCRIBER_VALIDATION_INTERVAL = 5f; // Validate every 5 seconds
        private const float ZOOM_DEAD_ZONE = 0.01f; // Minimum zoom change to trigger event
        private const float DOUBLE_CLICK_TIME = 0.3f; // Maximum time between clicks for double-click

        #region Nested Types

        /// <summary>
        /// Defines scrollable boundary constraints for the map
        /// </summary>
        [System.Serializable]
        public class ScrollBounds
        {
            [Header("Current Camera Status")]
            [Tooltip("Current camera/view position (read-only)")]
            [SerializeField]
            private Vector2 currentPosition;

            [Header("Boundary Settings")]
            public Vector2 Min = new Vector2(-100f, -100f);
            public Vector2 Max = new Vector2(100f, 100f);
            public bool EnableSoftStops = true;
            public float SoftStopDistance = 5f;
            public bool ClampInput = false; // If true, modifies input; if false, just reports

            /// <summary>
            /// Updates the displayed camera position (for Inspector visibility)
            /// </summary>
            public void UpdateCurrentPosition(Vector2 position)
            {
                currentPosition = position;
            }

            /// <summary>
            /// Gets the current camera position
            /// </summary>
            public Vector2 CurrentPosition => currentPosition;

            public bool IsWithinBounds(Vector2 position)
            {
                return position.x >= Min.x && position.x <= Max.x &&
                       position.y >= Min.y && position.y <= Max.y;
            }

            public Vector2 GetClampedPosition(Vector2 position)
            {
                return new Vector2(
                    Mathf.Clamp(position.x, Min.x, Max.x),
                    Mathf.Clamp(position.y, Min.y, Max.y)
                );
            }

            public Vector2 GetBoundaryDirection(Vector2 position)
            {
                Vector2 dir = Vector2.zero;
                if (position.x <= Min.x) dir.x = -1;
                else if (position.x >= Max.x) dir.x = 1;
                if (position.y <= Min.y) dir.y = -1;
                else if (position.y >= Max.y) dir.y = 1;
                return dir;
            }

            public float GetBoundaryProximity(Vector2 position)
            {
                float minDist = float.MaxValue;
                minDist = Mathf.Min(minDist, position.x - Min.x);
                minDist = Mathf.Min(minDist, Max.x - position.x);
                minDist = Mathf.Min(minDist, position.y - Min.y);
                minDist = Mathf.Min(minDist, Max.y - position.y);
                return Mathf.Max(0, minDist);
            }
        }

        /// <summary>
        /// Sets the scroll boundaries for input constraints
        /// </summary>
        public void SetScrollBounds(Vector2 min, Vector2 max)
        {
            if (scrollBounds == null)
                scrollBounds = new ScrollBounds();

            scrollBounds.Min = min;
            scrollBounds.Max = max;
            if (debugLog) Debug.Log($"{CLASS_NAME}: Scroll bounds set to Min:{min} Max:{max}");
        }

        /// <summary>
        /// Updates the current view position for boundary checking
        /// Should be called by the camera controller each frame
        /// </summary>
        public void UpdateViewPosition(Vector2 position)
        {
            CurrentViewPosition = position;
            scrollBounds?.UpdateCurrentPosition(position); // Update inspector display
            UpdateBoundaryStatus();
        }

        /// <summary>
        /// Gets a modified scroll vector that respects boundaries
        /// Useful for camera controllers that want to handle bounds themselves
        /// </summary>
        public Vector2 GetConstrainedScrollVector(Vector2 requestedScroll)
        {
            return ApplyBoundaryConstraints(requestedScroll);
        }

        /// <summary>
        /// Checks if a position is within the defined scroll bounds
        /// </summary>
        public bool IsPositionInBounds(Vector2 position)
        {
            return scrollBounds?.IsWithinBounds(position) ?? true;
        }

        #endregion

        #region Singleton

        public static InputService_BattleMap Instance { get; private set; }

        #endregion // Singleton

        #region Fields

        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Enable debug logging for this service")]
        private bool debugLog = false;

        [Header("Input Actions")]
        [SerializeField]
        [Tooltip("Map scrolling WASD input")]
        private InputAction mapScrollAction;

        [SerializeField]
        [Tooltip("Map zooming Q/E input")]
        private InputAction mapZoomAction;

        [SerializeField]
        [Tooltip("Mouse wheel zoom input")]
        private InputAction mouseWheelZoomAction;

        [SerializeField]
        [Tooltip("Reset zoom to default (Middle Mouse)")]
        private InputAction resetZoomAction;

        [SerializeField]
        [Tooltip("Left mouse button interaction")]
        private InputAction leftMouseAction;

        [SerializeField]
        [Tooltip("Right mouse button interaction")]
        private InputAction rightMouseAction;

        #endregion // Fields

        #region Properties

        [Header("Service Status")]
        /// <summary>
        /// Indicates whether the service has been successfully initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates whether the service has been started
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Indicates whether the service has been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        [Header("Input State")]
        /// <summary>
        /// Current scroll input vector from WASD keys
        /// </summary>
        public Vector2 CurrentScrollVector { get; private set; }

        /// <summary>
        /// Current zoom delta value from Q/E keys
        /// </summary>
        public float CurrentZoomDelta { get; private set; }

        /// <summary>
        /// True when scroll input is active
        /// </summary>
        public bool IsScrolling => CurrentScrollVector.sqrMagnitude > 0.0001f;

        /// <summary>
        /// True when zoom input is active
        /// </summary>
        public bool IsZooming => Mathf.Abs(CurrentZoomDelta) > ZOOM_DEAD_ZONE;

        [Header("Edge Scrolling Settings")]
        /// <summary>
        /// Distance from screen edge to trigger edge scrolling (in pixels)
        /// </summary>
        [Range(5f, 50f)]
        public float EdgeScrollThreshold = 10f;

        /// <summary>
        /// Speed multiplier for edge scrolling (higher = faster)
        /// </summary>
        [Range(0.1f, 3f)]
        public float EdgeScrollSpeed = 1f;

        /// <summary>
        /// Whether to use graduated speed based on distance from edge
        /// </summary>
        public bool UseGraduatedEdgeSpeed = true;

        /// <summary>
        /// Enable or disable edge scrolling
        /// </summary>
        public bool EdgeScrollEnabled = true;

        [Header("Mouse Wheel Zoom Settings")]
        /// <summary>
        /// Speed multiplier for mouse wheel zooming (higher = faster)
        /// </summary>
        [Range(1f, 40f)]
        [Tooltip("Controls how fast the camera zooms with mouse wheel")]
        public float MouseWheelZoomSpeed = 10f;

        [Header("Scroll Boundaries")]
        /// <summary>
        /// Defines the scrollable area boundaries
        /// </summary>
        [SerializeField]
        private ScrollBounds scrollBounds = new ScrollBounds();

        /// <summary>
        /// Current camera/view position for boundary checking
        /// </summary>
        public Vector2 CurrentViewPosition { get; set; }

        /// <summary>
        /// Gets the scroll boundary configuration
        /// </summary>
        public ScrollBounds Bounds => scrollBounds;

        [Header("Boundary Status")]
        /// <summary>
        /// True when view is at any boundary edge
        /// </summary>
        public bool IsAtBoundary { get; private set; }

        /// <summary>
        /// Direction vector indicating which boundaries are hit (-1, 0, or 1 per axis)
        /// </summary>
        public Vector2 BoundaryDirection { get; private set; }

        /// <summary>
        /// Distance to nearest boundary (0 when at or beyond boundary)
        /// </summary>
        public float BoundaryProximity { get; private set; }

        /// <summary>
        /// Last known mouse position when left button was clicked
        /// </summary>
        public Vector2 LastLeftClickPosition { get; private set; }

        /// <summary>
        /// Last known mouse position when right button was clicked
        /// </summary>
        public Vector2 LastRightClickPosition { get; private set; }

        #endregion

        #region Private Fields

        // Cached references
        private Mouse cachedMouse;

        // Input state tracking
        private float lastZoomValue;
        private float nextSubscriberValidationTime;
        private bool inputEnabled = true;

        // Double-click tracking
        private float lastLeftClickTime;
        private float lastRightClickTime;

        // Mouse button hold tracking
        private float leftButtonHoldStartTime;
        private float rightButtonHoldStartTime;
        private bool isLeftButtonHeld;
        private bool isRightButtonHeld;

        #endregion

        #region Events and Delegates

        // Map scrolling events
        private Action<Vector2> _onMapScroll;
        /// <summary>
        /// Fired when map scroll input is detected (WASD keys)
        /// </summary>
        public event Action<Vector2> OnMapScroll
        {
            add { AddSubscriber("OnMapScroll", value.Target, ref _onMapScroll, value); }
            remove { RemoveSubscriber("OnMapScroll", value.Target, ref _onMapScroll, value); }
        }

        // Map zooming events
        private Action<float> _onMapZoom;
        /// <summary>
        /// Fired when map zoom input changes (Q/E keys)
        /// </summary>
        public event Action<float> OnMapZoom
        {
            add { AddSubscriber("OnMapZoom", value.Target, ref _onMapZoom, value); }
            remove { RemoveSubscriber("OnMapZoom", value.Target, ref _onMapZoom, value); }
        }

        // Mouse button events
        private Action<Vector2> _onLeftMouseClick;
        /// <summary>
        /// Fired when left mouse button is clicked
        /// </summary>
        public event Action<Vector2> OnLeftMouseClick
        {
            add { AddSubscriber("OnLeftMouseClick", value.Target, ref _onLeftMouseClick, value); }
            remove { RemoveSubscriber("OnLeftMouseClick", value.Target, ref _onLeftMouseClick, value); }
        }

        private Action<Vector2> _onRightMouseClick;
        /// <summary>
        /// Fired when right mouse button is clicked
        /// </summary>
        public event Action<Vector2> OnRightMouseClick
        {
            add { AddSubscriber("OnRightMouseClick", value.Target, ref _onRightMouseClick, value); }
            remove { RemoveSubscriber("OnRightMouseClick", value.Target, ref _onRightMouseClick, value); }
        }

        private Action _onResetZoom;
        /// <summary>
        /// Fired when reset zoom is triggered (Middle Mouse Button)
        /// </summary>
        public event Action OnResetZoom
        {
            add { AddSubscriber("OnResetZoom", value.Target, ref _onResetZoom, value); }
            remove { RemoveSubscriber("OnResetZoom", value.Target, ref _onResetZoom, value); }
        }

        // Double-click events
        private Action<Vector2> _onLeftMouseDoubleClick;
        /// <summary>
        /// Fired when left mouse button is double-clicked
        /// </summary>
        public event Action<Vector2> OnLeftMouseDoubleClick
        {
            add { AddSubscriber("OnLeftMouseDoubleClick", value.Target, ref _onLeftMouseDoubleClick, value); }
            remove { RemoveSubscriber("OnLeftMouseDoubleClick", value.Target, ref _onLeftMouseDoubleClick, value); }
        }

        private Action<Vector2> _onRightMouseDoubleClick;
        /// <summary>
        /// Fired when right mouse button is double-clicked
        /// </summary>
        public event Action<Vector2> OnRightMouseDoubleClick
        {
            add { AddSubscriber("OnRightMouseDoubleClick", value.Target, ref _onRightMouseDoubleClick, value); }
            remove { RemoveSubscriber("OnRightMouseDoubleClick", value.Target, ref _onRightMouseDoubleClick, value); }
        }

        // Hold events
        private Action<Vector2, float> _onLeftMouseHold;
        /// <summary>
        /// Fired when left mouse button is held down. Provides position and duration.
        /// </summary>
        public event Action<Vector2, float> OnLeftMouseHold
        {
            add { AddSubscriber("OnLeftMouseHold", value.Target, ref _onLeftMouseHold, value); }
            remove { RemoveSubscriber("OnLeftMouseHold", value.Target, ref _onLeftMouseHold, value); }
        }

        private Action<Vector2, float> _onRightMouseHold;
        /// <summary>
        /// Fired when right mouse button is held down. Provides position and duration.
        /// </summary>
        public event Action<Vector2, float> OnRightMouseHold
        {
            add { AddSubscriber("OnRightMouseHold", value.Target, ref _onRightMouseHold, value); }
            remove { RemoveSubscriber("OnRightMouseHold", value.Target, ref _onRightMouseHold, value); }
        }

        // Edge scrolling event
        private Action<Vector2> _onEdgeScroll;
        /// <summary>
        /// Fired when mouse is at screen edge for edge scrolling
        /// </summary>
        public event Action<Vector2> OnEdgeScroll
        {
            add { AddSubscriber("OnEdgeScroll", value.Target, ref _onEdgeScroll, value); }
            remove { RemoveSubscriber("OnEdgeScroll", value.Target, ref _onEdgeScroll, value); }
        }

        // Boundary events
        private Action<Vector2> _onBoundaryHit;
        /// <summary>
        /// Fired when view reaches a boundary. Provides boundary direction.
        /// </summary>
        public event Action<Vector2> OnBoundaryHit
        {
            add { AddSubscriber("OnBoundaryHit", value.Target, ref _onBoundaryHit, value); }
            remove { RemoveSubscriber("OnBoundaryHit", value.Target, ref _onBoundaryHit, value); }
        }

        private Action<Vector2, float> _onBoundaryApproaching;
        /// <summary>
        /// Fired when view is near a boundary. Provides direction and distance.
        /// </summary>
        public event Action<Vector2, float> OnBoundaryApproaching
        {
            add { AddSubscriber("OnBoundaryApproaching", value.Target, ref _onBoundaryApproaching, value); }
            remove { RemoveSubscriber("OnBoundaryApproaching", value.Target, ref _onBoundaryApproaching, value); }
        }

        // Dictionary to track subscribers for leak detection
        private readonly Dictionary<string, HashSet<object>> eventSubscribers =
            new Dictionary<string, HashSet<object>>();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity's Awake method. Handles singleton instance management and validates
        /// required components. Any duplicate instances will be destroyed.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (debugLog) Debug.Log($"{CLASS_NAME}: Instance created on {gameObject.name}");

                InitializeService();
            }
            else
            {
                Debug.LogWarning($"{CLASS_NAME}: Duplicate instance found on {gameObject.name}. " +
                    $"Keeping instance on {Instance.gameObject.name}, destroying this one.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts the input service and begins processing input events.
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
        /// Updates the input service and processes input events.
        /// </summary>
        private void Update()
        {
            if (!IsInitialized || !IsStarted || IsDisposed || !inputEnabled) return;

            // Process continuous input states
            ProcessScrollInput();
            ProcessZoomInput();
            ProcessMouseWheelZoom();
            ProcessEdgeScrolling();
            ProcessMouseHoldStates();

            // Validate subscribers periodically
            if (Time.time >= nextSubscriberValidationTime)
            {
                ValidateSubscribers();
                nextSubscriberValidationTime = Time.time + SUBSCRIBER_VALIDATION_INTERVAL;
            }
        }

        /// <summary>
        /// Cleans up the input service and disposes of resources.
        /// </summary>
        private void OnDestroy()
        {
            Dispose(true);
        }

        #endregion // Unity Lifecycle

        #region Private Methods

        private void InitializeService()
        {
            try
            {
                // Cache mouse reference
                cachedMouse = Mouse.current;
                if (cachedMouse == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: No mouse device found. Mouse input will be disabled.");
                }

                // Validate input actions are assigned
                if (mapScrollAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Map Scroll Action is not assigned.");
                if (mapZoomAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Map Zoom Action is not assigned.");
                if (mouseWheelZoomAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Mouse Wheel Zoom Action is not assigned.");
                if (resetZoomAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Reset Zoom Action is not assigned.");
                if (leftMouseAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Left Mouse Action is not assigned.");
                if (rightMouseAction == null)
                    Debug.LogWarning($"{CLASS_NAME}: Right Mouse Action is not assigned.");

                // Enable all input actions
                mapScrollAction?.Enable();
                mapZoomAction?.Enable();
                mouseWheelZoomAction?.Enable();
                resetZoomAction?.Enable();
                leftMouseAction?.Enable();
                rightMouseAction?.Enable();

                // Subscribe to input events
                SubscribeToInputEvents();

                IsInitialized = true;
                if (debugLog) Debug.Log($"{CLASS_NAME}: Service initialized successfully.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "InitializeService", e);
                IsInitialized = false;
            }
        }

        private void StartService()
        {
            try
            {
                if (IsDisposed)
                {
                    Debug.LogError($"{CLASS_NAME}: Cannot start a disposed service.");
                    throw new InvalidOperationException("Cannot start a disposed service.");
                }

                IsStarted = true;
                nextSubscriberValidationTime = Time.time + SUBSCRIBER_VALIDATION_INTERVAL;
                if (debugLog) Debug.Log($"{CLASS_NAME}: Service started successfully.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "StartService", e);
                IsStarted = false;
            }
        }

        #endregion // Private Methods

        #region Input Event Handlers

        /// <summary>
        /// Subscribes to all relevant input events.
        /// </summary>
        private void SubscribeToInputEvents()
        {
            try
            {
                // Mouse button events
                if (leftMouseAction != null)
                {
                    leftMouseAction.performed += OnLeftMouseButtonPerformed;
                    leftMouseAction.started += OnLeftMouseButtonStarted;
                    leftMouseAction.canceled += OnLeftMouseButtonCanceled;
                }

                if (rightMouseAction != null)
                {
                    rightMouseAction.performed += OnRightMouseButtonPerformed;
                    rightMouseAction.started += OnRightMouseButtonStarted;
                    rightMouseAction.canceled += OnRightMouseButtonCanceled;
                }

                if (resetZoomAction != null)
                {
                    resetZoomAction.performed += OnResetZoomPerformed;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SubscribeToInputEvents", e);
            }
        }

        /// <summary>
        /// Handles left mouse button press start
        /// </summary>
        private void OnLeftMouseButtonStarted(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                leftButtonHoldStartTime = Time.time;
                isLeftButtonHeld = true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnLeftMouseButtonStarted", e);
            }
        }

        /// <summary>
        /// Handles left mouse button release
        /// </summary>
        private void OnLeftMouseButtonCanceled(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                isLeftButtonHeld = false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnLeftMouseButtonCanceled", e);
            }
        }

        /// <summary>
        /// Handles left mouse button input events.
        /// </summary>
        private void OnLeftMouseButtonPerformed(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                if (cachedMouse == null)
                {
                    cachedMouse = Mouse.current;
                    if (cachedMouse == null) return;
                }

                Vector2 mousePosition = cachedMouse.position.ReadValue();
                LastLeftClickPosition = mousePosition;

                // Check for double-click
                float currentTime = Time.time;
                if (currentTime - lastLeftClickTime < DOUBLE_CLICK_TIME)
                {
                    _onLeftMouseDoubleClick?.Invoke(mousePosition);
                    lastLeftClickTime = 0f; // Reset to prevent triple-click detection
                }
                else
                {
                    _onLeftMouseClick?.Invoke(mousePosition);
                    lastLeftClickTime = currentTime;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnLeftMouseButtonPerformed", e);
            }
        }

        /// <summary>
        /// Handles right mouse button press start
        /// </summary>
        private void OnRightMouseButtonStarted(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                rightButtonHoldStartTime = Time.time;
                isRightButtonHeld = true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnRightMouseButtonStarted", e);
            }
        }

        /// <summary>
        /// Handles right mouse button release
        /// </summary>
        private void OnRightMouseButtonCanceled(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                isRightButtonHeld = false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnRightMouseButtonCanceled", e);
            }
        }

        /// <summary>
        /// Handles right mouse button input events.
        /// </summary>
        private void OnRightMouseButtonPerformed(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                if (cachedMouse == null)
                {
                    cachedMouse = Mouse.current;
                    if (cachedMouse == null) return;
                }

                Vector2 mousePosition = cachedMouse.position.ReadValue();
                LastRightClickPosition = mousePosition;

                // Check for double-click
                float currentTime = Time.time;
                if (currentTime - lastRightClickTime < DOUBLE_CLICK_TIME)
                {
                    _onRightMouseDoubleClick?.Invoke(mousePosition);
                    lastRightClickTime = 0f; // Reset to prevent triple-click detection
                }
                else
                {
                    _onRightMouseClick?.Invoke(mousePosition);
                    lastRightClickTime = currentTime;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnRightMouseButtonPerformed", e);
            }
        }

        /// <summary>
        /// Handles reset zoom input events.
        /// </summary>
        private void OnResetZoomPerformed(InputAction.CallbackContext context)
        {
            if (!inputEnabled) return;

            try
            {
                _onResetZoom?.Invoke();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnResetZoomPerformed", e);
            }
        }

        #endregion

        #region Input Processing

        /// <summary>
        /// Processes the current map scroll input state.
        /// </summary>
        private void ProcessScrollInput()
        {
            if (mapScrollAction == null) return;

            try
            {
                Vector2 scrollValue = mapScrollAction.ReadValue<Vector2>();

                // Apply boundary constraints if enabled
                if (scrollBounds != null && scrollBounds.ClampInput)
                {
                    scrollValue = ApplyBoundaryConstraints(scrollValue);
                }

                CurrentScrollVector = scrollValue;

                // Fire event if there's any significant input
                if (scrollValue.sqrMagnitude > 0.0001f)
                {
                    _onMapScroll?.Invoke(scrollValue);
                }

                // Update boundary status
                UpdateBoundaryStatus();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ProcessScrollInput", e);
            }
        }

        /// <summary>
        /// Processes the current map zoom input state.
        /// </summary>
        private void ProcessZoomInput()
        {
            if (mapZoomAction == null) return;

            try
            {
                float zoomValue = mapZoomAction.ReadValue<float>();
                CurrentZoomDelta = zoomValue;

                // Fire event continuously while button is held (if value is significant)
                if (Mathf.Abs(zoomValue) > ZOOM_DEAD_ZONE)
                {
                    _onMapZoom?.Invoke(zoomValue);
                }

                lastZoomValue = zoomValue;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ProcessZoomInput", e);
            }
        }

        /// <summary>
        /// Processes mouse wheel zoom input.
        /// </summary>
        private void ProcessMouseWheelZoom()
        {
            if (mouseWheelZoomAction == null) return;

            try
            {
                // Read scroll delta (Vector2 or float, we use Y axis for vertical scroll)
                Vector2 scrollDelta = mouseWheelZoomAction.ReadValue<Vector2>();
                float wheelDelta = scrollDelta.y;

                // Apply mouse wheel zoom speed multiplier
                if (Mathf.Abs(wheelDelta) > ZOOM_DEAD_ZONE)
                {
                    // Invert so scroll up = zoom in (negative orthographic size change)
                    // Apply MouseWheelZoomSpeed for separate control from Q/E zoom
                    float scaledDelta = -wheelDelta * MouseWheelZoomSpeed;
                    _onMapZoom?.Invoke(scaledDelta);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ProcessMouseWheelZoom", e);
            }
        }

        /// <summary>
        /// Processes edge scrolling when mouse is at screen edges
        /// </summary>
        private void ProcessEdgeScrolling()
        {
            if (cachedMouse == null || !EdgeScrollEnabled) return;

            try
            {
                Vector2 mousePos = cachedMouse.position.ReadValue();
                Vector2 edgeScroll = Vector2.zero;

                // Calculate distances from edges
                float leftDist = mousePos.x;
                float rightDist = Screen.width - mousePos.x;
                float bottomDist = mousePos.y;
                float topDist = Screen.height - mousePos.y;

                // Check horizontal edges
                if (leftDist <= EdgeScrollThreshold)
                {
                    if (UseGraduatedEdgeSpeed)
                    {
                        // Closer to edge = faster scroll
                        float normalizedDist = 1f - (leftDist / EdgeScrollThreshold);
                        edgeScroll.x = -normalizedDist * EdgeScrollSpeed;
                    }
                    else
                    {
                        edgeScroll.x = -EdgeScrollSpeed;
                    }
                }
                else if (rightDist <= EdgeScrollThreshold)
                {
                    if (UseGraduatedEdgeSpeed)
                    {
                        float normalizedDist = 1f - (rightDist / EdgeScrollThreshold);
                        edgeScroll.x = normalizedDist * EdgeScrollSpeed;
                    }
                    else
                    {
                        edgeScroll.x = EdgeScrollSpeed;
                    }
                }

                // Check vertical edges
                if (bottomDist <= EdgeScrollThreshold)
                {
                    if (UseGraduatedEdgeSpeed)
                    {
                        float normalizedDist = 1f - (bottomDist / EdgeScrollThreshold);
                        edgeScroll.y = -normalizedDist * EdgeScrollSpeed;
                    }
                    else
                    {
                        edgeScroll.y = -EdgeScrollSpeed;
                    }
                }
                else if (topDist <= EdgeScrollThreshold)
                {
                    if (UseGraduatedEdgeSpeed)
                    {
                        float normalizedDist = 1f - (topDist / EdgeScrollThreshold);
                        edgeScroll.y = normalizedDist * EdgeScrollSpeed;
                    }
                    else
                    {
                        edgeScroll.y = EdgeScrollSpeed;
                    }
                }

                // Apply boundary constraints if enabled
                if (scrollBounds != null && scrollBounds.ClampInput)
                {
                    edgeScroll = ApplyBoundaryConstraints(edgeScroll);
                }

                if (edgeScroll.sqrMagnitude > 0.0001f)
                {
                    _onEdgeScroll?.Invoke(edgeScroll);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ProcessEdgeScrolling", e);
            }
        }

        /// <summary>
        /// Applies boundary constraints to scroll input based on current view position
        /// </summary>
        private Vector2 ApplyBoundaryConstraints(Vector2 input)
        {
            if (scrollBounds == null) return input;

            Vector2 constrainedInput = input;
            Vector2 predictedPosition = CurrentViewPosition + input;

            // Check if we're at or would exceed boundaries
            if (predictedPosition.x <= scrollBounds.Min.x && input.x < 0)
                constrainedInput.x = Mathf.Max(0, scrollBounds.Min.x - CurrentViewPosition.x);
            else if (predictedPosition.x >= scrollBounds.Max.x && input.x > 0)
                constrainedInput.x = Mathf.Min(0, scrollBounds.Max.x - CurrentViewPosition.x);

            if (predictedPosition.y <= scrollBounds.Min.y && input.y < 0)
                constrainedInput.y = Mathf.Max(0, scrollBounds.Min.y - CurrentViewPosition.y);
            else if (predictedPosition.y >= scrollBounds.Max.y && input.y > 0)
                constrainedInput.y = Mathf.Min(0, scrollBounds.Max.y - CurrentViewPosition.y);

            // Apply soft stops if enabled
            if (scrollBounds.EnableSoftStops && scrollBounds.SoftStopDistance > 0)
            {
                float proximity = scrollBounds.GetBoundaryProximity(CurrentViewPosition);
                if (proximity < scrollBounds.SoftStopDistance)
                {
                    float softStopMultiplier = proximity / scrollBounds.SoftStopDistance;
                    constrainedInput *= softStopMultiplier;
                }
            }

            return constrainedInput;
        }

        /// <summary>
        /// Updates boundary status and fires events as needed
        /// </summary>
        private void UpdateBoundaryStatus()
        {
            if (scrollBounds == null) return;

            Vector2 prevBoundaryDir = BoundaryDirection;
            BoundaryDirection = scrollBounds.GetBoundaryDirection(CurrentViewPosition);
            BoundaryProximity = scrollBounds.GetBoundaryProximity(CurrentViewPosition);

            bool wasAtBoundary = IsAtBoundary;
            IsAtBoundary = BoundaryDirection.sqrMagnitude > 0.0001f;

            // Fire boundary hit event when first hitting boundary
            if (IsAtBoundary && !wasAtBoundary)
            {
                _onBoundaryHit?.Invoke(BoundaryDirection);
            }

            // Fire approaching event when within soft stop distance
            if (scrollBounds.EnableSoftStops && BoundaryProximity < scrollBounds.SoftStopDistance)
            {
                _onBoundaryApproaching?.Invoke(BoundaryDirection, BoundaryProximity);
            }
        }

        /// <summary>
        /// Processes mouse button hold states
        /// </summary>
        private void ProcessMouseHoldStates()
        {
            if (cachedMouse == null) return;

            try
            {
                Vector2 mousePos = cachedMouse.position.ReadValue();

                if (isLeftButtonHeld)
                {
                    float holdDuration = Time.time - leftButtonHoldStartTime;
                    _onLeftMouseHold?.Invoke(mousePos, holdDuration);
                }

                if (isRightButtonHeld)
                {
                    float holdDuration = Time.time - rightButtonHoldStartTime;
                    _onRightMouseHold?.Invoke(mousePos, holdDuration);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ProcessMouseHoldStates", e);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if any input processing is currently active.
        /// </summary>
        /// <returns>True if any input (scrolling, zooming, etc.) is being processed.</returns>
        public bool IsProcessingInput()
        {
            return IsScrolling || IsZooming || isLeftButtonHeld || isRightButtonHeld;
        }

        /// <summary>
        /// Temporarily enables or disables all input processing.
        /// Useful for modal dialogs or cutscenes.
        /// </summary>
        /// <param name="enabled">True to enable input, false to disable</param>
        public void SetInputEnabled(bool enabled)
        {
            try
            {
                if (!IsInitialized)
                {
                    Debug.LogWarning($"{CLASS_NAME}: Attempted to set input state before initialization.");
                    return;
                }

                inputEnabled = enabled;

                if (enabled)
                {
                    mapScrollAction?.Enable();
                    mapZoomAction?.Enable();
                    mouseWheelZoomAction?.Enable();
                    resetZoomAction?.Enable();
                    leftMouseAction?.Enable();
                    rightMouseAction?.Enable();
                    if (debugLog) Debug.Log($"{CLASS_NAME}: Input enabled.");
                }
                else
                {
                    mapScrollAction?.Disable();
                    mapZoomAction?.Disable();
                    mouseWheelZoomAction?.Disable();
                    resetZoomAction?.Disable();
                    leftMouseAction?.Disable();
                    rightMouseAction?.Disable();

                    // Reset input state variables but preserve event subscribers
                    ResetInputStateVariables();
                    if (debugLog) Debug.Log($"{CLASS_NAME}: Input disabled.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetInputEnabled", e);
            }
        }

        /// <summary>
        /// Resets input state variables without clearing event subscribers.
        /// Used when temporarily disabling input (e.g., for dialogs).
        /// </summary>
        private void ResetInputStateVariables()
        {
            try
            {
                CurrentScrollVector = Vector2.zero;
                CurrentZoomDelta = 0f;
                lastZoomValue = 0f;

                isLeftButtonHeld = false;
                isRightButtonHeld = false;
                lastLeftClickTime = 0f;
                lastRightClickTime = 0f;

                LastLeftClickPosition = Vector2.zero;
                LastRightClickPosition = Vector2.zero;

                if (debugLog) Debug.Log($"{CLASS_NAME}: Input state variables reset (subscribers preserved).");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResetInputStateVariables", e);
            }
        }

        /// <summary>
        /// Clears all current input states and resets to default values.
        /// Also clears all event subscribers. Only use when fully disposing the service.
        /// </summary>
        public void ResetInputState()
        {
            try
            {
                // Reset variables first
                ResetInputStateVariables();

                // Clear all event delegates
                _onMapScroll = null;
                _onMapZoom = null;
                _onLeftMouseClick = null;
                _onRightMouseClick = null;
                _onResetZoom = null;
                _onLeftMouseDoubleClick = null;
                _onRightMouseDoubleClick = null;
                _onLeftMouseHold = null;
                _onRightMouseHold = null;
                _onEdgeScroll = null;
                _onBoundaryHit = null;
                _onBoundaryApproaching = null;

                eventSubscribers.Clear();

                if (debugLog) Debug.Log($"{CLASS_NAME}: Input state fully reset (including subscribers).");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResetInputState", e);
            }
        }

        /// <summary>
        /// Gets the current mouse position
        /// </summary>
        public Vector2 GetCurrentMousePosition()
        {
            if (cachedMouse == null)
            {
                cachedMouse = Mouse.current;
                if (cachedMouse == null) return Vector2.zero;
            }

            return cachedMouse.position.ReadValue();
        }

        /// <summary>
        /// Gets the hold duration for left mouse button if currently held
        /// </summary>
        public float GetLeftButtonHoldDuration()
        {
            return isLeftButtonHeld ? Time.time - leftButtonHoldStartTime : 0f;
        }

        /// <summary>
        /// Gets the hold duration for right mouse button if currently held
        /// </summary>
        public float GetRightButtonHoldDuration()
        {
            return isRightButtonHeld ? Time.time - rightButtonHoldStartTime : 0f;
        }

        #endregion // Event Handling

        #region Event Subscriber Management

        /// <summary>
        /// Validates all event subscribers and removes any dead references.
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
                        Debug.LogWarning($"{CLASS_NAME}: Dead subscriber detected on {kvp.Key}");
                    }
                }

                foreach (var dead in deadSubscribers)
                {
                    kvp.Value.Remove(dead);
                }
            }
        }

        /// <summary>
        /// Adds a subscriber to the specified event.
        /// </summary>
        private void AddSubscriber(string eventName, object subscriber, ref Action backingField, Action handler)
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
            backingField += handler;
        }

        /// <summary>
        /// Adds a subscriber to the specified event.
        /// </summary>
        private void AddSubscriber<T>(string eventName, object subscriber, ref Action<T> backingField, Action<T> handler)
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
            backingField += handler;
        }

        /// <summary>
        /// Adds a subscriber to the specified event with two parameters.
        /// </summary>
        private void AddSubscriber<T1, T2>(string eventName, object subscriber, ref Action<T1, T2> backingField, Action<T1, T2> handler)
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
            backingField += handler;
        }

        /// <summary>
        /// Removes a subscriber from the specified event.
        /// </summary>
        private void RemoveSubscriber(string eventName, object subscriber, ref Action backingField, Action handler)
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

            backingField -= handler;
        }

        /// <summary>
        /// Removes a subscriber from the specified event.
        /// </summary>
        private void RemoveSubscriber<T>(string eventName, object subscriber, ref Action<T> backingField, Action<T> handler)
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

            backingField -= handler;
        }

        /// <summary>
        /// Removes a subscriber from the specified event with two parameters.
        /// </summary>
        private void RemoveSubscriber<T1, T2>(string eventName, object subscriber, ref Action<T1, T2> backingField, Action<T1, T2> handler)
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

            backingField -= handler;
        }

        #endregion // Event Subscriber Management

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the service and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the service and releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Unsubscribe from input events first
                        if (leftMouseAction != null)
                        {
                            leftMouseAction.performed -= OnLeftMouseButtonPerformed;
                            leftMouseAction.started -= OnLeftMouseButtonStarted;
                            leftMouseAction.canceled -= OnLeftMouseButtonCanceled;
                        }

                        if (rightMouseAction != null)
                        {
                            rightMouseAction.performed -= OnRightMouseButtonPerformed;
                            rightMouseAction.started -= OnRightMouseButtonStarted;
                            rightMouseAction.canceled -= OnRightMouseButtonCanceled;
                        }

                        if (resetZoomAction != null)
                        {
                            resetZoomAction.performed -= OnResetZoomPerformed;
                        }

                        // Disable all input actions
                        mapScrollAction?.Disable();
                        mapZoomAction?.Disable();
                        mouseWheelZoomAction?.Disable();
                        resetZoomAction?.Disable();
                        leftMouseAction?.Disable();
                        rightMouseAction?.Disable();

                        // Clear all event delegates
                        _onMapScroll = null;
                        _onMapZoom = null;
                        _onLeftMouseClick = null;
                        _onRightMouseClick = null;
                        _onResetZoom = null;
                        _onLeftMouseDoubleClick = null;
                        _onRightMouseDoubleClick = null;
                        _onLeftMouseHold = null;
                        _onRightMouseHold = null;
                        _onEdgeScroll = null;
                        _onBoundaryHit = null;
                        _onBoundaryApproaching = null;

                        // Clear all event subscriptions
                        eventSubscribers.Clear();

                        if (debugLog) Debug.Log($"{CLASS_NAME}: Service disposed successfully.");
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, "Dispose", e);
                    }
                }

                IsDisposed = true;
                if (Instance == this)
                {
                    Instance = null;
                }
            }
        }

        #endregion
    }
}
