using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Persistence;
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

                // One path family since 2026-07-27 — the manifest resolves against its own content folder,
                // so campaign and standalone scenarios load identically and neither knows where it lives.
                string mapFilePath = manifest.GetMapFilePath();

                if (string.IsNullOrWhiteSpace(mapFilePath))
                {
                    Debug.LogError($"{CLASS_NAME}: Manifest '{manifest.ScenarioId}' has no map file path " +
                                   $"(MapFilename='{manifest.MapFilename}', ContentRoot='{manifest.ContentRoot}')");
                    throw new ArgumentException("Scenario manifest has no map file path", nameof(manifest));
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

                // Serialization policy is centralized — see JsonPolicy for why enums must persist by NAME.
                // Do not reintroduce a local options object here.
                var jsonOptions = JsonPolicy.Content;

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

                // Version-compatibility gate. A structurally-valid map can still be the WRONG format
                // version: IsValid() only checks SaveVersion > 0, so without this an old map loads with
                // the post-rework HexTile fields (isPort/isDeploymentZone/isBeachhead/hexControlLevel/...)
                // silently defaulted. Reject the mismatch here so stale maps are regenerated, not loaded
                // half-populated. (CurrentMapDataVersion was bumped 1→2 for the HexTile rework.)
                if (mapData.Header != null && !mapData.Header.IsCompatibleVersion())
                {
                    string versionMsg =
                        $"Map '{mapData.Header.MapName}' is format version {mapData.Header.SaveVersion}, but this " +
                        $"build requires version {JsonMapHeader.GetCurrentSaveVersion()}. Regenerate the map from the Scenario Editor.";
                    Debug.LogError($"{CLASS_NAME}: {versionMsg}");
                    AppService.CaptureUiMessage(versionMsg);
                    throw new InvalidDataException(versionMsg);
                }

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Map data validation successful");

                AppService.CaptureUiMessage($"Map data loaded: {mapData.Header.MapName}");

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: Creating HexMap instance...");

                /* Create HexMap instance at the size THE FILE DECLARES (G1, 2026-08-12). This used to pass
                 * `mapData.Header.MapConfiguration` into a constructor that mapped Small => 32x21, which is
                 * why any map that was not one of two blessed sizes loaded silently truncated. Throws rather
                 * than guessing when the header cannot answer — see ResolveMapDimensions. */
                UnityEngine.Vector2Int dims = mapData.Header.ResolveMapDimensions();

                /* Cross-check against the manifest, which carries its own copy for pre-parse consumers
                 * (menu display, validation). They should never disagree; if they do, this scenario folder
                 * has a `.map` and a manifest that were not built together — the mis-pairing failure that a
                 * self-describing `.map` exists to make detectable. The HEADER wins: it is the file whose
                 * geometry is actually being loaded. */
                if (manifest != null)
                {
                    var manifestDims = manifest.GetMapDimensions();
                    if (manifestDims.x >= 10 && manifestDims.y >= 10 && manifestDims != dims)
                    {
                        string mismatch =
                            $"Map '{mapData.Header.MapName}' header says {dims.x}x{dims.y} but manifest " +
                            $"'{manifest.ScenarioId}' says {manifestDims.x}x{manifestDims.y}. Using the header. " +
                            "These files were not exported together — re-export the scenario.";
                        Debug.LogWarning($"{CLASS_NAME}: {mismatch}");
                        AppService.CaptureUiMessage(mismatch);
                    }
                }

                HexMap hexMap = new HexMap(mapData.Header.MapName, dims.x, dims.y);

                // Write to log.
                if (log) Debug.Log($"{CLASS_NAME}: HexMap created - Expected size: {hexMap.MapSize}");

                // Populate the map with hex tiles
                if (log) Debug.Log($"{CLASS_NAME}: Populating map with hex tiles...");
                int successCount = 0;
                int failCount = 0;
                int nullCount = 0;
                int negativeValueCount = 0;

                foreach (HexTile hex in mapData.Hexes)
                {
                    if (hex == null)
                    {
                        nullCount++;
                        if (nullCount <= 5) // Only log first 5
                            if (log) Debug.LogWarning($"{CLASS_NAME}: Encountered null hex tile in map data");
                        continue;
                    }

                    // Negative victoryValue is authoring garbage, not geometry — warn, don't refuse
                    // (ruled 2026-08-17, todo_prestige Stage 1). VictoryLedger.Compute's v <= 0 skip
                    // treats it as 0 for scoring; the value is loaded as-is so the editor can see it.
                    if (hex.VictoryValue < 0f)
                    {
                        negativeValueCount++;
                        if (negativeValueCount <= 5) // Only log first 5
                            Debug.LogWarning($"{CLASS_NAME}: Hex {hex.Position} has negative victoryValue " +
                                             $"({hex.VictoryValue}) — scoring treats it as 0. Fix in the Scenario Editor.");
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

                /* ⚠ TRUNCATION IS A HARD FAILURE (G6, 2026-08-12). With G1 in place the header's dimensions
                 * ARE the map's dimensions, so a hex outside them can only mean a corrupt or hand-edited
                 * file — there is no legitimate case left. This condition used to produce five log lines and
                 * a playable, WRONG map, which is the single worst property this area had: the map loaded,
                 * played, and silently lost every hex past the truncation. MapLoader's catch turns this into
                 * a refused load with a visible reason rather than a crash. */
                if (failCount > 0)
                {
                    throw new InvalidDataException(
                        $"Map '{hexMap.MapName}': {failCount} of {mapData.Hexes.Length} hexes fell outside " +
                        $"{hexMap.MapSize.x}x{hexMap.MapSize.y}. The file's geometry and the loaded map " +
                        "disagree — regenerate it from the Scenario Editor.");
                }

                AppService.CaptureUiMessage($"Populated map with {hexMap.HexCount} hexes");

                // C6b (2026-08-17): mission objectives are MANIFEST data stamped onto the map here —
                // the authored isObjective value in the file is DEAD and IGNORED. Scenario load is the
                // ONLY stamping site: the SnapshotMapper restore path never re-stamps, because an
                // in-battle save carries its own stamped map and must load with the scenario
                // uninstalled (§7.3).
                ApplyMissionObjectiveStamp(hexMap, manifest);

                // Build neighbor relationships
                if (log) Debug.Log($"{CLASS_NAME}: Building neighbor relationships...");
                hexMap.BuildNeighborRelationships();
                if (log) Debug.Log($"{CLASS_NAME}: Neighbor relationships built successfully");

                // Symmetrize border data: ensure both hexes sharing an edge agree on
                // rivers, bridges, pontoon bridges, and damaged bridges. Map files may
                // store border flags on only one side of a shared edge; this pass
                // propagates each flag to the neighboring hex's opposite edge.
                if (log) Debug.Log($"{CLASS_NAME}: Symmetrizing border data...");
                int bordersFixed = 0;

                foreach (HexTile hex in hexMap)
                {
                    if (hex == null) continue;

                    for (int d = 0; d < 6; d++)
                    {
                        HexDirection dir = (HexDirection)d;
                        HexDirection opposite = HexMapUtil.GetOppositeDirection(dir);
                        HexTile neighbor = hex.GetNeighbor(dir);

                        if (neighbor == null) continue;

                        // Rivers
                        SymmetrizeBorder(hex.RiverBorders, neighbor.RiverBorders, dir, opposite, ref bordersFixed);

                        // Bridges
                        SymmetrizeBorder(hex.BridgeBorders, neighbor.BridgeBorders, dir, opposite, ref bordersFixed);

                        // Pontoon bridges
                        SymmetrizeBorder(hex.PontoonBridgeBorders, neighbor.PontoonBridgeBorders, dir, opposite, ref bordersFixed);

                        // Damaged bridges
                        SymmetrizeBorder(hex.DamagedBridgeBorders, neighbor.DamagedBridgeBorders, dir, opposite, ref bordersFixed);
                    }
                }

                if (log) Debug.Log($"{CLASS_NAME}: Border symmetrization complete - {bordersFixed} one-sided borders fixed");
                if (bordersFixed > 0)
                {
                    AppService.CaptureUiMessage($"Fixed {bordersFixed} asymmetric border entries");
                }

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

        #region Private Methods

        /// <summary>
        /// Ensures both sides of a shared hex edge have matching border data.
        /// If one side has a border set and the other does not, the missing side is filled in.
        /// </summary>
        /// <param name="aBorders">Border data for hex A</param>
        /// <param name="bBorders">Border data for hex B (neighbor)</param>
        /// <param name="aDir">Direction from A toward B</param>
        /// <param name="bDir">Direction from B toward A (opposite)</param>
        /// <param name="fixCount">Running count of fixes applied</param>
        /// <summary>
        /// C6b clear-then-stamp: zeroes EVERY authored isObjective on the loaded map (the file's value
        /// is dead — stale flags in legacy maps and editor lag are harmless by construction), then
        /// stamps the manifest's missionObjectives onto the hexes. Internal + static so EditorTests
        /// can drive it against a fixture map without file I/O.
        /// ⚠ An objective on a MISSING hex is a REFUSED LOAD (G6 doctrine: manifest and .map were not
        /// exported together — silently skipping would make the C6 victory gate quietly easier, the
        /// exact silent-wrong shape G6 exists to kill). An objective on a NON-STRONGHOLD hex warns
        /// loudly and stamps anyway — stronghold placement is an authoring convention (Bob's call):
        /// an open-ground objective flips by mere transit and the gate flickers.
        /// </summary>
        internal static void ApplyMissionObjectiveStamp(HexMap hexMap, ScenarioManifest manifest)
        {
            foreach (HexTile hex in hexMap)
            {
                if (hex != null) hex.IsObjective = false;
            }

            if (manifest?.MissionObjectives == null) return;

            foreach (MissionObjective obj in manifest.MissionObjectives)
            {
                if (obj == null) continue;

                string name = string.IsNullOrEmpty(obj.Label) ? $"({obj.X},{obj.Y})" : $"'{obj.Label}' ({obj.X},{obj.Y})";
                HexTile tile = hexMap.GetHexAt(new Position2D(obj.X, obj.Y));

                if (tile == null)
                {
                    throw new InvalidDataException(
                        $"Mission objective {name} does not exist on map '{hexMap.MapName}' " +
                        $"({hexMap.MapSize.x}x{hexMap.MapSize.y}) — the manifest and .map were not " +
                        "exported together. Fix the manifest or re-export the map.");
                }

                tile.IsObjective = true;

                if (!tile.IsStronghold)
                {
                    Debug.LogWarning($"{CLASS_NAME}: Mission objective {name} is not on a stronghold hex " +
                                     "(city/fort/airbase/port) — it will flip by mere transit and the victory " +
                                     "gate will flicker. Move it or fortify the hex.");
                }
            }
        }

        private static void SymmetrizeBorder(
            JSONFeatureBorders aBorders,
            JSONFeatureBorders bBorders,
            HexDirection aDir,
            HexDirection bDir,
            ref int fixCount)
        {
            if (aBorders == null || bBorders == null) return;

            bool aHas = aBorders.GetBorder(aDir);
            bool bHas = bBorders.GetBorder(bDir);

            if (aHas && !bHas)
            {
                bBorders.SetBorder(bDir, true);
                fixCount++;
            }
            else if (!aHas && bHas)
            {
                aBorders.SetBorder(aDir, true);
                fixCount++;
            }
        }

        #endregion // Private Methods
    }
}
