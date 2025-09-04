using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.Testing
{
    /// <summary>
    /// Test script for validating map file loading functionality.
    /// Attach to a GameObject and specify the map file name to test loading.
    /// </summary>
    public class MapLoadingTestScript : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(MapLoadingTestScript);

        #endregion // Constants

        #region Inspector Fields

        [Header("Map Loading Test Configuration")]
        [Tooltip("Name of the map file to load (with or without .map extension)")]
        public string mapFileName = "test_map.map";

        [Tooltip("Enable detailed debug logging during the test")]
        public bool enableDebugLogging = true;

        [Header("Test Results")]
        [SerializeField] private string testStatus = "Not started";
        [SerializeField] private string mapName = "";
        [SerializeField] private string mapConfiguration = "";
        [SerializeField] private int hexCount = 0;
        [SerializeField] private bool validationPassed = false;

        #endregion // Inspector Fields

        #region Private Fields

        private HexMap loadedHexMap;
        private bool hasRunTest = false;
        private ScenarioLoadingService loadingService;

        #endregion // Private Fields

        #region Unity Lifecycle

        /// <summary>
        /// Initialize the loading service on start.
        /// </summary>
        private void Start()
        {
            try
            {
                Debug.Log($"{CLASS_NAME}: Starting map loading test");

                // Initialize the loading service
                loadingService = ScenarioLoadingService.Instance;

                if (!loadingService.Initialize())
                {
                    testStatus = "Failed to initialize ScenarioLoadingService";
                    AppService.CaptureUiMessage("Map loading test failed: Service initialization failed");
                    return;
                }

                testStatus = "Ready to load map file";
                AppService.CaptureUiMessage($"Map loading test ready - will load '{mapFileName}' on first update");

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Test initialized, will load '{mapFileName}' on first update");
                }
            }
            catch (Exception ex)
            {
                testStatus = $"Start failed: {ex.Message}";
                AppService.HandleException(CLASS_NAME, nameof(Start), ex);
            }
        }

        /// <summary>
        /// Perform the map loading test on the first update call.
        /// </summary>
        private void Update()
        {
            if (!hasRunTest && loadingService != null && loadingService.IsInitialized)
            {
                hasRunTest = true;
                _ = LoadMapFileTest();
            }
        }

        #endregion // Unity Lifecycle

        #region Test Methods

        /// <summary>
        /// Async test method that loads the specified map file and validates the results.
        /// </summary>
        private async System.Threading.Tasks.Task LoadMapFileTest()
        {
            try
            {
                testStatus = "Loading map file...";

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Starting map file load test for '{mapFileName}'");
                }

                // Perform the map loading
                loadedHexMap = await loadingService.LoadMapFileAsync(mapFileName);

                if (loadedHexMap == null)
                {
                    testStatus = "Map loading failed - returned null";
                    AppService.CaptureUiMessage($"Map loading test FAILED: LoadMapFileAsync returned null for '{mapFileName}'");

                    if (enableDebugLogging)
                    {
                        Debug.LogError($"{CLASS_NAME}: Map loading failed - service returned null");
                    }
                    return;
                }

                // Extract map information for display
                mapName = loadedHexMap.MapName;
                mapConfiguration = loadedHexMap.Configuration.ToString();
                hexCount = loadedHexMap.HexCount;

                // Run additional validation
                bool integrityValid = loadedHexMap.ValidateIntegrity();
                bool dimensionsValid = loadedHexMap.ValidateDimensions();
                bool connectivityValid = loadedHexMap.ValidateConnectivity();

                validationPassed = integrityValid && dimensionsValid && connectivityValid;

                if (validationPassed)
                {
                    testStatus = "✅ Map loading SUCCESS";
                    AppService.CaptureUiMessage($"Map loading test PASSED: '{mapName}' loaded successfully ({hexCount} hexes)");

                    if (enableDebugLogging)
                    {
                        Debug.Log($"{CLASS_NAME}: Map loading test PASSED");
                        Debug.Log($"{CLASS_NAME}: - Map Name: {mapName}");
                        Debug.Log($"{CLASS_NAME}: - Configuration: {mapConfiguration}");
                        Debug.Log($"{CLASS_NAME}: - Hex Count: {hexCount}");
                        Debug.Log($"{CLASS_NAME}: - Validation: Integrity={integrityValid}, Dimensions={dimensionsValid}, Connectivity={connectivityValid}");
                    }
                }
                else
                {
                    testStatus = "❌ Map loaded but validation failed";
                    AppService.CaptureUiMessage($"Map loading test PARTIAL FAILURE: '{mapName}' loaded but validation failed");

                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Map loaded but validation failed");
                        Debug.LogWarning($"{CLASS_NAME}: - Integrity: {integrityValid}, Dimensions: {dimensionsValid}, Connectivity: {connectivityValid}");
                    }
                }

                // Log detailed map statistics
                LogMapStatistics();
            }
            catch (Exception ex)
            {
                testStatus = $"Exception during test: {ex.Message}";
                AppService.HandleException(CLASS_NAME, nameof(LoadMapFileTest), ex);
                AppService.CaptureUiMessage($"Map loading test EXCEPTION: {ex.Message}");

                if (enableDebugLogging)
                {
                    Debug.LogError($"{CLASS_NAME}: Exception during map loading test: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Logs detailed statistics about the loaded map.
        /// </summary>
        private void LogMapStatistics()
        {
            try
            {
                if (loadedHexMap == null) return;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: === Map Statistics ===");
                    Debug.Log($"{CLASS_NAME}: Map Size: {loadedHexMap.MapSize.x} x {loadedHexMap.MapSize.y}");
                    Debug.Log($"{CLASS_NAME}: Total Hexes: {loadedHexMap.HexCount}");
                    Debug.Log($"{CLASS_NAME}: Is Initialized: {loadedHexMap.IsInitialized}");
                    Debug.Log($"{CLASS_NAME}: Is Disposed: {loadedHexMap.IsDisposed}");
                }

                // Count terrain types
                var terrainCounts = new System.Collections.Generic.Dictionary<TerrainType, int>();
                int objectiveHexes = 0;
                int fortHexes = 0;
                int airbaseHexes = 0;

                foreach (var hex in loadedHexMap)
                {
                    if (hex != null)
                    {
                        // Count terrain types
                        if (terrainCounts.ContainsKey(hex.Terrain))
                            terrainCounts[hex.Terrain]++;
                        else
                            terrainCounts[hex.Terrain] = 1;

                        // Count special features
                        if (hex.IsObjective) objectiveHexes++;
                        if (hex.IsFort) fortHexes++;
                        if (hex.IsAirbase) airbaseHexes++;
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Special Features - Objectives: {objectiveHexes}, Forts: {fortHexes}, Airbases: {airbaseHexes}");

                    foreach (var kvp in terrainCounts)
                    {
                        Debug.Log($"{CLASS_NAME}: Terrain {kvp.Key}: {kvp.Value} hexes");
                    }
                }

                AppService.CaptureUiMessage($"Map statistics logged - {terrainCounts.Count} terrain types, {objectiveHexes} objectives");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LogMapStatistics), ex);
            }
        }

        #endregion // Test Methods

        #region Public Methods

        /// <summary>
        /// Manually trigger the map loading test (for testing from inspector or other scripts).
        /// </summary>
        [ContextMenu("Run Map Loading Test")]
        public void RunMapLoadingTest()
        {
            if (!hasRunTest)
            {
                hasRunTest = true;
                _ = LoadMapFileTest();
            }
            else
            {
                AppService.CaptureUiMessage("Map loading test has already been run");
            }
        }

        /// <summary>
        /// Reset the test to allow running it again.
        /// </summary>
        [ContextMenu("Reset Test")]
        public void ResetTest()
        {
            try
            {
                hasRunTest = false;

                // Dispose of current map if it exists
                if (loadedHexMap != null)
                {
                    loadedHexMap.Dispose();
                    loadedHexMap = null;
                }

                // Reset display fields
                testStatus = "Reset - ready to test again";
                mapName = "";
                mapConfiguration = "";
                hexCount = 0;
                validationPassed = false;

                AppService.CaptureUiMessage("Map loading test reset");

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Test reset complete");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResetTest), ex);
            }
        }

        /// <summary>
        /// Gets the loaded HexMap for external inspection.
        /// </summary>
        /// <returns>The loaded HexMap, or null if not loaded or failed</returns>
        public HexMap GetLoadedMap()
        {
            return loadedHexMap;
        }

        #endregion // Public Methods

        #region Cleanup

        /// <summary>
        /// Clean up resources when the script is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                if (loadedHexMap != null)
                {
                    loadedHexMap.Dispose();
                    loadedHexMap = null;
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Script destroyed, resources cleaned up");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(OnDestroy), ex);
            }
        }

        #endregion // Cleanup
    }
}