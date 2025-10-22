using System;
using UnityEngine;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// Manages camera movement and zoom for the battle map.
    /// </summary>
    public class CameraService : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(CameraService);

        #region Singleton

        public static CameraService Instance { get; private set; }

        #endregion // Singleton

        #region Serialized Fields

        [Header("Camera Reference")]
        [SerializeField]
        [Tooltip("The camera to control")]
        private Camera controlledCamera;

        [Header("Scroll Settings")]
        [SerializeField]
        [Range(1f, 50f)]
        [Tooltip("Speed of camera scrolling")]
        private float scrollSpeed = 10f;

        [Header("Zoom Settings")]
        [SerializeField]
        [Range(0.1f, 20f)]
        [Tooltip("Speed of camera zooming (per frame)")]
        private float zoomSpeed = 5f;

        [SerializeField]
        [Range(1f, 50f)]
        [Tooltip("Minimum orthographic size (max zoom in)")]
        private float minOrthographicSize = 5f;

        [SerializeField]
        [Range(10f, 100f)]
        [Tooltip("Maximum orthographic size (max zoom out)")]
        private float maxOrthographicSize = 50f;

        [SerializeField]
        [Range(5f, 50f)]
        [Tooltip("Default orthographic size for reset")]
        private float defaultOrthographicSize = 20f;

        #endregion // Serialized Fields

        #region Properties

        [Header("Service Status")]
        /// <summary>
        /// Indicates whether the service has been successfully initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Current camera position (read-only)
        /// </summary>
        public Vector3 CameraPosition => controlledCamera != null ? controlledCamera.transform.position : Vector3.zero;

        /// <summary>
        /// Current orthographic size (read-only)
        /// </summary>
        public float CurrentZoom => controlledCamera != null ? controlledCamera.orthographicSize : defaultOrthographicSize;

        #endregion // Properties

        #region Unity Lifecycle

        /// <summary>
        /// Unity's Awake method. Handles singleton instance management.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"{CLASS_NAME}: Instance created on {gameObject.name}");

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
        /// Unity's Start method. Subscribes to input events.
        /// </summary>
        private void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"{CLASS_NAME}.Start: Service failed to initialize properly.");
                return;
            }

            SubscribeToInputEvents();
        }

        /// <summary>
        /// Unity's OnDestroy method. Cleans up subscriptions.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromInputEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Initializes the camera manager service.
        /// </summary>
        private void InitializeService()
        {
            try
            {
                // Validate camera reference
                if (controlledCamera == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: No camera assigned. Attempting to find main camera.");
                    controlledCamera = Camera.main;

                    if (controlledCamera == null)
                    {
                        Debug.LogError($"{CLASS_NAME}: Could not find a camera to control.");
                        IsInitialized = false;
                        return;
                    }
                }

                // Validate camera is orthographic
                if (!controlledCamera.orthographic)
                {
                    Debug.LogWarning($"{CLASS_NAME}: Camera is not orthographic. Converting to orthographic mode.");
                    controlledCamera.orthographic = true;
                }

                // Set initial orthographic size if needed
                if (controlledCamera.orthographicSize != defaultOrthographicSize)
                {
                    controlledCamera.orthographicSize = defaultOrthographicSize;
                }

                IsInitialized = true;
                Debug.Log($"{CLASS_NAME}: Service initialized successfully.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "InitializeService", e);
                IsInitialized = false;
            }
        }

        #endregion // Initialization

        #region Event Subscription

        /// <summary>
        /// Subscribes to input events from InputService_BattleMap.
        /// </summary>
        private void SubscribeToInputEvents()
        {
            try
            {
                if (InputService_BattleMap.Instance == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: InputService_BattleMap instance not found. Cannot subscribe to events.");
                    return;
                }

                // Subscribe to scroll events
                InputService_BattleMap.Instance.OnMapScroll += HandleScroll;
                InputService_BattleMap.Instance.OnEdgeScroll += HandleEdgeScroll;

                // Subscribe to zoom events
                InputService_BattleMap.Instance.OnMapZoom += HandleZoom;
                InputService_BattleMap.Instance.OnResetZoom += HandleResetZoom;

                // Report initial camera position
                UpdateCameraPositionInInputService();

                Debug.Log($"{CLASS_NAME}: Subscribed to input events.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SubscribeToInputEvents", e);
            }
        }

        /// <summary>
        /// Unsubscribes from input events.
        /// </summary>
        private void UnsubscribeFromInputEvents()
        {
            try
            {
                if (InputService_BattleMap.Instance == null) return;

                // Unsubscribe from scroll events
                InputService_BattleMap.Instance.OnMapScroll -= HandleScroll;
                InputService_BattleMap.Instance.OnEdgeScroll -= HandleEdgeScroll;

                // Unsubscribe from zoom events
                InputService_BattleMap.Instance.OnMapZoom -= HandleZoom;
                InputService_BattleMap.Instance.OnResetZoom -= HandleResetZoom;

                Debug.Log($"{CLASS_NAME}: Unsubscribed from input events.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UnsubscribeFromInputEvents", e);
            }
        }

        #endregion // Event Subscription

        #region Event Handlers

        /// <summary>
        /// Handles map scroll input (WASD keys).
        /// </summary>
        private void HandleScroll(Vector2 scrollVector)
        {
            try
            {
                if (controlledCamera == null) return;

                // Move camera based on scroll input
                Vector3 movement = new Vector3(scrollVector.x, scrollVector.y, 0f) * scrollSpeed * Time.deltaTime;
                controlledCamera.transform.position += movement;

                // Report new position to InputService for boundary checking
                UpdateCameraPositionInInputService();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HandleScroll", e);
            }
        }

        /// <summary>
        /// Handles edge scroll input (mouse at screen edges).
        /// Edge scroll vectors are already scaled by InputService, so we only apply Time.deltaTime.
        /// </summary>
        private void HandleEdgeScroll(Vector2 scrollVector)
        {
            try
            {
                if (controlledCamera == null) return;

                // Edge scroll is already scaled by EdgeScrollSpeed in InputService
                // We only need to apply Time.deltaTime for frame-rate independence
                Vector3 movement = new Vector3(scrollVector.x, scrollVector.y, 0f) * Time.deltaTime;
                controlledCamera.transform.position += movement;

                // Report new position to InputService for boundary checking
                UpdateCameraPositionInInputService();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HandleEdgeScroll", e);
            }
        }

        /// <summary>
        /// Handles zoom input (Q/E keys).
        /// </summary>
        private void HandleZoom(float zoomDelta)
        {
            try
            {
                if (controlledCamera == null) return;

                // Adjust orthographic size (negative delta = zoom in, positive = zoom out)
                // No Time.deltaTime needed - fires every frame already at consistent rate
                float newSize = controlledCamera.orthographicSize + (zoomDelta * zoomSpeed * 0.01f);
                controlledCamera.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HandleZoom", e);
            }
        }

        /// <summary>
        /// Handles reset zoom input (Middle mouse button).
        /// </summary>
        private void HandleResetZoom()
        {
            try
            {
                if (controlledCamera == null) return;

                controlledCamera.orthographicSize = defaultOrthographicSize;
                Debug.Log($"{CLASS_NAME}: Zoom reset to default ({defaultOrthographicSize}).");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HandleResetZoom", e);
            }
        }

        /// <summary>
        /// Updates the camera position in InputService for boundary checking.
        /// </summary>
        private void UpdateCameraPositionInInputService()
        {
            try
            {
                if (InputService_BattleMap.Instance != null && controlledCamera != null)
                {
                    Vector2 position = new Vector2(controlledCamera.transform.position.x, controlledCamera.transform.position.y);
                    InputService_BattleMap.Instance.UpdateViewPosition(position);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateCameraPositionInInputService", e);
            }
        }

        #endregion // Event Handlers
    }
}
