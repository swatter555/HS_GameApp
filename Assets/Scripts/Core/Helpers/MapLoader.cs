using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace HammerAndSickle.Core.Helpers
{
    /// <summary>
    /// Loads hex map data from .map files and populates the GameDataManager.
    /// </summary>
    public class MapLoader : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(MapLoader);

        #region Public Methods

        /// <summary>
        /// Loads a hex map from the specified scenario manifest.
        /// </summary>
        public static bool LoadMapFile(ScenarioManifest manifest, bool log = false)
        {
            try
            {
                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Starting map load process");

                // PrepareBattle the manifest
                if (manifest == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Manifest is null");
                    throw new ArgumentNullException(nameof(manifest), "ScenarioManifest is null");
                }

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Manifest validated - ScenarioId: {manifest.ScenarioId}");

                // Get the proper path to load the map data
                string mapFilePath;

                // Determine whether this is the campaign version or the scenario version.
                if (manifest.IsCampaignScenario)
                {
                    mapFilePath = manifest.GetMapFilePath_GDP();

                    // Write to log.
                    if (log) Debug.Log($"{CLASS_NAME}: Campaign scenario detected");

                    if (string.IsNullOrWhiteSpace(mapFilePath))
                    {
                        Debug.LogError($"{CLASS_NAME}: Campaign scenario has no map file path");
                        throw new ArgumentException("Campaign scenario manifest has no map file path", nameof(manifest));
                    }
                }
                else
                {
                    mapFilePath = manifest.GetMapFilePath();

                    // Write to log.
                    if (log) Debug.Log($"{CLASS_NAME}: Standard scenario detected");

                    if (string.IsNullOrWhiteSpace(mapFilePath))
                    {
                        Debug.LogError($"{CLASS_NAME}: Scenario has no map file path");
                        throw new ArgumentException("Scenario manifest has no map file path", nameof(manifest));
                    }
                }

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Map file path: {mapFilePath}");

                // Verify the file exists
                if (!File.Exists(mapFilePath))
                {
                    Debug.LogError($"{CLASS_NAME}: Map file not found at path: {mapFilePath}");
                    throw new FileNotFoundException($"Map file not found: {mapFilePath}");
                }

                long fileSize = new FileInfo(mapFilePath).Length;

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Map file found, size: {fileSize:N0} bytes");

                AppService.CaptureUiMessage($"Loading map from: {Path.GetFileName(mapFilePath)}");

                // Read the JSON file synchronously (Unity-safe)
                if (log) Debug.Log($"{CLASS_NAME}: Reading file content...");

                string jsonContent = File.ReadAllText(mapFilePath);

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: File read successfully, content length: {jsonContent.Length:N0} characters");

                // Deserialize using System.Text.Json
                if (log) Debug.Log($"{CLASS_NAME}: Deserializing JSON data...");

                // Configure JSON options for proper deserialization
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false,
                    IncludeFields = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                };

                JsonMapData mapData;

                try
                {
                    mapData = JsonSerializer.Deserialize<JsonMapData>(jsonContent, jsonOptions);
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    Debug.LogError($"{CLASS_NAME}: JSON Deserialization Error Details:");
                    Debug.LogError($"{CLASS_NAME}: Path: {jsonEx.Path}");
                    Debug.LogError($"{CLASS_NAME}: LineNumber: {jsonEx.LineNumber}");
                    Debug.LogError($"{CLASS_NAME}: BytePositionInLine: {jsonEx.BytePositionInLine}");
                    Debug.LogError($"{CLASS_NAME}: Message: {jsonEx.Message}");
                    if (jsonEx.InnerException != null)
                    {
                        Debug.LogError($"{CLASS_NAME}: Inner Exception: {jsonEx.InnerException.GetType().Name}");
                        Debug.LogError($"{CLASS_NAME}: Inner Message: {jsonEx.InnerException.Message}");
                    }
                    throw;
                }

                if (mapData == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Deserialization returned null");
                    throw new InvalidDataException("Failed to deserialize map data - result was null");
                }

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Deserialization successful");

                // Debug: Check header
                if (mapData.Header == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Map data header is null");
                }
                else
                {
                    // Write to log.
                    if (log)
                    {
                        Debug.Log($"{CLASS_NAME}: Header - MapName: '{mapData.Header.MapName}', Config: {mapData.Header.MapConfiguration}, Version: {mapData.Header.SaveVersion}");
                        Debug.Log($"{CLASS_NAME}: Header - Checksum: '{mapData.Header.Checksum}', CreatedAt: {mapData.Header.CreatedAt}");
                    }
                }

                // Debug: Check hexes
                if (mapData.Hexes == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Map hexes array is null");
                }
                else
                {
                    // Write to log.
                    if (log) Debug.Log($"{CLASS_NAME}: Hexes array loaded with {mapData.Hexes.Length} entries");

                    // Debug: Check border data in first few hexes
                    if (log)
                    {
                        try
                        {
                            int hexesToCheck = Math.Min(10, mapData.Hexes.Length);
                            int hexesWithRivers = 0;
                            int hexesWithBridges = 0;
                            int hexesWithDamagedBridges = 0;
                            int hexesWithPontoonBridges = 0;

                            for (int i = 0; i < hexesToCheck; i++)
                            {
                                var hex = mapData.Hexes[i];
                                if (hex == null) continue;

                                if (hex.RiverBorders != null && hex.RiverBorders.HasAnyBorders())
                                {
                                    hexesWithRivers++;
                                    Debug.Log($"{CLASS_NAME}: Hex {i} at ({hex.Position.X},{hex.Position.Y}) has rivers: {hex.RiverBorders.GetBorderString()}");
                                }
                                if (hex.BridgeBorders != null && hex.BridgeBorders.HasAnyBorders())
                                {
                                    hexesWithBridges++;
                                    Debug.Log($"{CLASS_NAME}: Hex {i} at ({hex.Position.X},{hex.Position.Y}) has bridges: {hex.BridgeBorders.GetBorderString()}");
                                }
                                if (hex.DamagedBridgeBorders != null && hex.DamagedBridgeBorders.HasAnyBorders())
                                {
                                    hexesWithDamagedBridges++;
                                    Debug.Log($"{CLASS_NAME}: Hex {i} at ({hex.Position.X},{hex.Position.Y}) has damaged bridges: {hex.DamagedBridgeBorders.GetBorderString()}");
                                }
                                if (hex.PontoonBridgeBorders != null && hex.PontoonBridgeBorders.HasAnyBorders())
                                {
                                    hexesWithPontoonBridges++;
                                    Debug.Log($"{CLASS_NAME}: Hex {i} at ({hex.Position.X},{hex.Position.Y}) has pontoon bridges: {hex.PontoonBridgeBorders.GetBorderString()}");
                                }
                            }

                            Debug.Log($"{CLASS_NAME}: Border data summary (first {hexesToCheck} hexes): Rivers={hexesWithRivers}, Bridges={hexesWithBridges}, Damaged={hexesWithDamagedBridges}, Pontoon={hexesWithPontoonBridges}");
                        }
                        catch (Exception debugEx)
                        {
                            Debug.LogWarning($"{CLASS_NAME}: Error during border debug logging: {debugEx.Message}");
                        }
                    }
                }

                // PrepareBattle the loaded data with detailed diagnostics
                if (log) Debug.Log($"{CLASS_NAME}: Validating map data...");

                if (!mapData.IsValid())
                {
                    Debug.LogError($"{CLASS_NAME}: Map data validation failed - running detailed diagnostics");

                    // Log the dianostics
                    if (log)
                    {
                        // Detailed validation diagnostics
                        if (mapData.Header == null)
                        {
                            Debug.LogError($"{CLASS_NAME}: Validation failed: Header is null");
                        }
                        else if (!mapData.Header.IsValid())
                        {
                            Debug.LogError($"{CLASS_NAME}: Validation failed: Header validation failed");

                            // Check individual header fields
                            if (string.IsNullOrWhiteSpace(mapData.Header.MapName))
                                Debug.LogError($"{CLASS_NAME}: Header issue: MapName is null or empty");

                            if (!Enum.IsDefined(typeof(MapConfig), mapData.Header.MapConfiguration))
                                Debug.LogError($"{CLASS_NAME}: Header issue: Invalid MapConfiguration value {mapData.Header.MapConfiguration}");

                            if (mapData.Header.SaveVersion <= 0)
                                Debug.LogError($"{CLASS_NAME}: Header issue: Invalid SaveVersion {mapData.Header.SaveVersion}");

                            if (string.IsNullOrWhiteSpace(mapData.Header.Checksum))
                                Debug.LogError($"{CLASS_NAME}: Header issue: Checksum is null or empty");

                            if (mapData.Header.CreatedAt == default)
                                Debug.LogError($"{CLASS_NAME}: Header issue: CreatedAt is default value");
                        }
                    }

                    if (mapData.Hexes == null)
                    {
                        Debug.LogError($"{CLASS_NAME}: Validation failed: Hexes array is null");
                    }
                    else
                    {
                        // Write to log.
                        if (log) Debug.Log($"{CLASS_NAME}: Checking individual hex tiles for validation issues...");

                        for (int i = 0; i < mapData.Hexes.Length && i < 10; i++) // Check first 10
                        {
                            if (mapData.Hexes[i] == null)
                            {
                                // Write to log.
                                if (log) Debug.LogError($"{CLASS_NAME}: Hex at index {i} is null");
                            }
                            else if (!mapData.Hexes[i].ValidateHex())
                            {
                                // Write to log.
                                if (log) Debug.LogError($"{CLASS_NAME}: Hex at index {i} failed validation - Position: {mapData.Hexes[i].Position}");
                            }
                        }
                    }

                    throw new InvalidDataException("Map data validation failed - see logs for details");
                }

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Map data validation successful");

                AppService.CaptureUiMessage($"Map data loaded: {mapData.Header.MapName}");

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Creating HexMap instance...");

                // Create HexMap instance
                HexMap hexMap = new HexMap(mapData.Header.MapName, mapData.Header.MapConfiguration);

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: HexMap created - Expected size: {hexMap.MapSize}");

                // Populate the map with hex tiles
                if (log) Debug.Log($"{CLASS_NAME}: Populating map with hex tiles...");
                int successCount = 0;
                int failCount = 0;
                int nullCount = 0;

                foreach (HexTile hex in mapData.Hexes)
                {
                    if (hex == null)
                    {
                        nullCount++;
                        if (nullCount <= 5) // Only log first 5
                            if (log) Debug.LogWarning($"{CLASS_NAME}: Encountered null hex tile in map data");
                        continue;
                    }

                    if (!hexMap.SetHexAt(hex))
                    {
                        failCount++;
                        if (failCount <= 5) // Only log first 5 failures
                            if (log) Debug.LogWarning($"{CLASS_NAME}: Failed to add hex at position {hex.Position}");
                    }
                    else
                    {
                        successCount++;
                    }
                }

                if (log) Debug.Log($"{CLASS_NAME}: Hex population complete - Success: {successCount}, Failed: {failCount}, Null: {nullCount}");
                AppService.CaptureUiMessage($"Populated map with {hexMap.HexCount} hexes");

                // Build neighbor relationships
                if (log) Debug.Log($"{CLASS_NAME}: Building neighbor relationships...");
                hexMap.BuildNeighborRelationships();
                if (log) Debug.Log($"{CLASS_NAME}: Neighbor relationships built successfully");

                // PrepareBattle the constructed map
                if (log) Debug.Log($"{CLASS_NAME}: Validating constructed map integrity...");
                if (!hexMap.ValidateIntegrity())
                {
                    if (log) Debug.LogError($"{CLASS_NAME}: Map integrity validation failed after construction");
                    throw new InvalidDataException("Map integrity validation failed after construction");
                }

                if (log) Debug.Log($"{CLASS_NAME}: Map integrity validation successful");

                // Store in GameDataManager
                if (log) Debug.Log($"{CLASS_NAME}: Storing map in GameDataManager...");
                GameDataManager.CurrentHexMap = hexMap;
                GameDataManager.CurrentMapSize = hexMap.MapSize;
                GameDataManager.CurrentMapTheme = manifest.MapTheme;

                if (log) Debug.Log($"{CLASS_NAME}: Map loaded successfully!");
                if (log) Debug.Log($"{CLASS_NAME}: Final stats - Name: {hexMap.MapName}, Hexes: {hexMap.HexCount}, Size: {hexMap.MapSize}, Theme: {manifest.MapTheme}");

                AppService.CaptureUiMessage($"Map loaded successfully: {hexMap.MapName} ({hexMap.HexCount} hexes, {manifest.MapTheme} theme)");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"{CLASS_NAME}: Exception occurred during map loading");
                Debug.LogError($"{CLASS_NAME}: Exception type: {e.GetType().Name}");
                Debug.LogError($"{CLASS_NAME}: Exception message: {e.Message}");
                if (e.InnerException != null)
                {
                    Debug.LogError($"{CLASS_NAME}: Inner exception: {e.InnerException.Message}");
                }
                Debug.LogError($"{CLASS_NAME}: Stack trace: {e.StackTrace}");

                AppService.HandleException(CLASS_NAME, nameof(LoadMapFile), e);
                AppService.CaptureUiMessage($"Failed to load map: {e.Message}");

                return false;
            }
        }

        #endregion // Public Methods
    }
}
