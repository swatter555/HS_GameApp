using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Runtime hex map container providing efficient hex access and neighbor management.
    /// Supports save/load operations and implements enumeration for tactical queries.
    /// </summary>
    public class HexMap : IEnumerable<HexTile>, IDisposable
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexMap);

        #endregion // Constants

        #region Properties

        /// <summary>
        /// Display name of the map.
        /// </summary>
        public string MapName { get; private set; }

        /// <summary>
        /// Map dimensions in hex coordinates.
        /// </summary>
        public Vector2Int MapSize { get; private set; }

        /// <summary>
        /// Map configuration determining bounds and layout.
        /// </summary>
        public MapConfig Configuration { get; private set; }

        /// <summary>
        /// Total number of hexes in the map.
        /// </summary>
        public int HexCount => hexDictionary?.Count ?? 0;

        /// <summary>
        /// Indicates if the map has been properly initialized.
        /// </summary>
        [JsonIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates if the map has been disposed.
        /// </summary>
        [JsonIgnore]
        public bool IsDisposed { get; private set; }

        #endregion // Properties

        #region Private Fields

        private Dictionary<Position2D, HexTile> hexDictionary;
        private readonly Coordinate2DEqualityComparer coordinateComparer;
        private readonly bool enableDebugLogging;

        #endregion // Private Fields

        #region Constructors

        /// <summary>
        /// Creates a new hex map with the specified configuration.
        /// </summary>
        /// <param name="mapName">Display name of the map</param>
        /// <param name="configuration">Map size configuration</param>
        /// <param name="enableLogging">Enable debug logging for this map</param>
        public HexMap(string mapName, MapConfig configuration, bool enableLogging = false)
        {
            try
            {
                MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
                Configuration = configuration;
                enableDebugLogging = enableLogging;
                coordinateComparer = new Coordinate2DEqualityComparer();

                Initialize();

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Created map '{MapName}' with configuration {Configuration}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(HexMap), ex);
                throw;
            }
        }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        [JsonConstructor]
        public HexMap()
        {
            MapName = string.Empty;
            Configuration = MapConfig.None;
            enableDebugLogging = false;
            coordinateComparer = new Coordinate2DEqualityComparer();
            Initialize();
        }

        #endregion // Constructors

        #region Initialization

        /// <summary>
        /// Initializes the hex map with proper dimensions and data structures.
        /// </summary>
        private void Initialize()
        {
            try
            {
                MapSize = GetMapDimensions(Configuration);
                hexDictionary = new Dictionary<Position2D, HexTile>(coordinateComparer);
                IsInitialized = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Initialized map with size {MapSize.x}x{MapSize.y}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), ex);
                IsInitialized = false;
                throw;
            }
        }

        /// <summary>
        /// Gets the map dimensions based on configuration.
        /// </summary>
        /// <param name="config">Map configuration</param>
        /// <returns>Map dimensions as Vector2Int</returns>
        private static Vector2Int GetMapDimensions(MapConfig config)
        {
            return config switch
            {
                MapConfig.Small => new Vector2Int(HexMapConstants.SmallHexWidth, HexMapConstants.SmallHexHeight),
                MapConfig.Large => new Vector2Int(HexMapConstants.LargeHexWidth, HexMapConstants.LargeHexHeight),
                _ => Vector2Int.zero
            };
        }

        #endregion // Initialization

        #region Public Methods

        /// <summary>
        /// Retrieves a hex at the specified position with bounds validation.
        /// </summary>
        /// <param name="position">Coordinate position of the hex</param>
        /// <returns>MapHex at the position, or null if not found or out of bounds</returns>
        public HexTile GetHexAt(Position2D position)
        {
            try
            {
                ValidateState();

                if (!IsPositionInBounds(position))
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Position {position} is out of bounds for map size {MapSize}");
                    }
                    return null;
                }

                return hexDictionary.TryGetValue(position, out HexTile hex) ? hex : null;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexAt), ex);
                return null;
            }
        }

        /// <summary>
        /// Adds or updates a hex at the specified position.
        /// </summary>
        /// <param name="hex">Hex to add or update</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetHexAt(HexTile hex)
        {
            try
            {
                ValidateState();

                if (hex == null)
                {
                    throw new ArgumentNullException(nameof(hex));
                }

                if (!IsPositionInBounds(hex.Position))
                {
                    AppService.CaptureUiMessage($"Cannot place hex at {hex.Position}: position out of bounds");
                    return false;
                }

                hexDictionary[hex.Position] = hex;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Set hex at position {hex.Position}");
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetHexAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Removes a hex at the specified position.
        /// </summary>
        /// <param name="position">Position of hex to remove</param>
        /// <returns>True if hex was removed, false if not found</returns>
        public bool RemoveHexAt(Position2D position)
        {
            try
            {
                ValidateState();
                return hexDictionary.Remove(position);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveHexAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if a position is within the map bounds.
        /// </summary>
        /// <param name="position">Position to validate</param>
        /// <returns>True if position is within bounds, false otherwise</returns>
        public bool IsPositionInBounds(Position2D position)
        {
            try
            {
                return position.X >= 0 && position.X < MapSize.x &&
                       position.Y >= 0 && position.Y < MapSize.y;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsPositionInBounds), ex);
                return false;
            }
        }

        /// <summary>
        /// Builds neighbor relationships for all hexes in the map.
        /// Should be called after loading all hexes.
        /// </summary>
        public void BuildNeighborRelationships()
        {
            try
            {
                ValidateState();

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Building neighbor relationships for {HexCount} hexes");
                }

                int neighborsBuilt = 0;
                foreach (var hex in hexDictionary.Values)
                {
                    for (int direction = 0; direction < 6; direction++)
                    {
                        HexDirection hexDirection = (HexDirection)direction;
                        Position2D neighborPos = HexMapUtil.GetNeighborPosition(hex.Position, hexDirection);

                        if (hexDictionary.TryGetValue(neighborPos, out HexTile neighbor))
                        {
                            hex.SetNeighbor(hexDirection, neighbor);
                            neighborsBuilt++;
                        }
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Built {neighborsBuilt} neighbor relationships");
                }

                AppService.CaptureUiMessage($"Built neighbor relationships for {HexCount} hexes");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildNeighborRelationships), ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all hexes from the map.
        /// </summary>
        public void Clear()
        {
            try
            {
                ValidateState();

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Clearing {HexCount} hexes");
                }

                // Dispose of all hexes
                foreach (var hex in hexDictionary.Values)
                {
                    hex?.Dispose();
                }

                hexDictionary.Clear();
                AppService.CaptureUiMessage($"Cleared all hexes from map '{MapName}'");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Clear), ex);
                throw;
            }
        }

        #endregion // Public Methods

        #region Validation

        /// <summary>
        /// Validates the integrity of the hex map data.
        /// </summary>
        /// <returns>True if map data is valid, false otherwise</returns>
        public bool ValidateIntegrity()
        {
            try
            {
                ValidateState();

                bool isValid = true;
                int validationErrors = 0;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Starting integrity validation for {HexCount} hexes");
                }

                // Validate basic map state
                if (hexDictionary == null)
                {
                    AppService.CaptureUiMessage("Map integrity failed: Hex dictionary is null");
                    return false;
                }

                if (MapSize.x <= 0 || MapSize.y <= 0)
                {
                    AppService.CaptureUiMessage($"Map integrity failed: Invalid map dimensions {MapSize.x}x{MapSize.y}");
                    return false;
                }

                // Validate each hex for internal consistency
                foreach (var hex in hexDictionary.Values)
                {
                    if (hex == null)
                    {
                        AppService.CaptureUiMessage("Map integrity failed: Found null hex in dictionary");
                        validationErrors++;
                        isValid = false;
                        continue;
                    }

                    // Validate hex internal consistency
                    if (!hex.ValidateHex())
                    {
                        AppService.CaptureUiMessage($"Map integrity failed: Hex at {hex.Position} failed validation");
                        validationErrors++;
                        isValid = false;
                    }

                    // Validate hex position matches dictionary key
                    if (hexDictionary.TryGetValue(hex.Position, out HexTile storedHex))
                    {
                        if (!ReferenceEquals(hex, storedHex))
                        {
                            AppService.CaptureUiMessage($"Map integrity failed: Position mismatch for hex at {hex.Position}");
                            validationErrors++;
                            isValid = false;
                        }
                    }
                    else
                    {
                        AppService.CaptureUiMessage($"Map integrity failed: Hex at {hex.Position} not found in dictionary by position");
                        validationErrors++;
                        isValid = false;
                    }
                }

                // Check for duplicate positions (should not happen with proper dictionary usage)
                var allPositions = new HashSet<Position2D>();
                foreach (var hex in hexDictionary.Values)
                {
                    if (hex != null && !allPositions.Add(hex.Position))
                    {
                        AppService.CaptureUiMessage($"Map integrity failed: Duplicate position found at {hex.Position}");
                        validationErrors++;
                        isValid = false;
                    }
                }

                if (enableDebugLogging)
                {
                    if (isValid)
                    {
                        Debug.Log($"{CLASS_NAME}: Integrity validation passed - all {HexCount} hexes valid");
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Integrity validation failed with {validationErrors} errors");
                    }
                }

                if (isValid)
                {
                    AppService.CaptureUiMessage($"Map integrity validation passed for '{MapName}'");
                }
                else
                {
                    AppService.CaptureUiMessage($"Map integrity validation failed with {validationErrors} errors");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateIntegrity), ex);
                return false;
            }
        }

        /// <summary>
        /// Validates that all hexes are within proper coordinate bounds.
        /// </summary>
        /// <returns>True if all coordinates are valid, false otherwise</returns>
        public bool ValidateDimensions()
        {
            try
            {
                ValidateState();

                bool isValid = true;
                int outOfBoundsCount = 0;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Validating dimensions for {HexCount} hexes against bounds {MapSize.x}x{MapSize.y}");
                }

                // Check each hex position against map bounds
                foreach (var hex in hexDictionary.Values)
                {
                    if (hex == null)
                    {
                        continue; // Skip null hexes (will be caught by integrity validation)
                    }

                    // Validate position is within map bounds
                    if (!IsPositionInBounds(hex.Position))
                    {
                        AppService.CaptureUiMessage($"Dimension validation failed: Hex at {hex.Position} is outside map bounds ({MapSize.x}x{MapSize.y})");
                        outOfBoundsCount++;
                        isValid = false;

                        if (enableDebugLogging)
                        {
                            Debug.LogWarning($"{CLASS_NAME}: Hex at {hex.Position} is out of bounds");
                        }
                    }

                    // Validate position components are non-negative
                    if (hex.Position.X < 0 || hex.Position.Y < 0)
                    {
                        AppService.CaptureUiMessage($"Dimension validation failed: Hex at {hex.Position} has negative coordinates");
                        outOfBoundsCount++;
                        isValid = false;
                    }

                    // Use Position2D.Clamp to get corrected position and compare
                    Position2D clampedPosition = Position2D.Clamp(
                        hex.Position,
                        Position2D.Zero,
                        new Position2D(MapSize.x - 1, MapSize.y - 1)
                    );

                    if (!hex.Position.Equals(clampedPosition))
                    {
                        AppService.CaptureUiMessage($"Dimension validation failed: Hex at {hex.Position} would be clamped to {clampedPosition}");
                        outOfBoundsCount++;
                        isValid = false;
                    }
                }

                // Check for coverage gaps (optional - identifies areas without hexes)
                int expectedHexCount = MapSize.x * MapSize.y;
                if (HexCount != expectedHexCount)
                {
                    AppService.CaptureUiMessage($"Dimension validation warning: Expected {expectedHexCount} hexes for {MapSize.x}x{MapSize.y} map, found {HexCount}");

                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Hex count mismatch - expected {expectedHexCount}, found {HexCount}");
                    }
                    // Note: This is a warning, not an error, as sparse maps may be valid
                }

                if (enableDebugLogging)
                {
                    if (isValid)
                    {
                        Debug.Log($"{CLASS_NAME}: Dimension validation passed - all hexes within bounds");
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Dimension validation failed - {outOfBoundsCount} hexes out of bounds");
                    }
                }

                if (isValid)
                {
                    AppService.CaptureUiMessage($"Dimension validation passed for map '{MapName}'");
                }
                else
                {
                    AppService.CaptureUiMessage($"Dimension validation failed - {outOfBoundsCount} hexes out of bounds");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateDimensions), ex);
                return false;
            }
        }

        /// <summary>
        /// Validates that all neighbor relationships are properly established.
        /// </summary>
        /// <returns>True if connectivity is valid, false otherwise</returns>
        public bool ValidateConnectivity()
        {
            try
            {
                ValidateState();

                bool isValid = true;
                int connectivityErrors = 0;
                int orphanedHexes = 0;
                int brokenRelationships = 0;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Validating neighbor connectivity for {HexCount} hexes");
                }

                foreach (var hex in hexDictionary.Values)
                {
                    if (hex == null)
                    {
                        continue; // Skip null hexes
                    }

                    var neighbors = hex.GetAllNeighbors();
                    bool hasAnyNeighbor = false;

                    // Check each direction for neighbor relationships
                    for (int direction = 0; direction < 6; direction++)
                    {
                        HexDirection hexDirection = (HexDirection)direction;

                        // Calculate expected neighbor position
                        Position2D expectedNeighborPos = HexMapUtil.GetNeighborPosition(hex.Position, hexDirection);

                        // Get actual neighbor from hex
                        HexTile actualNeighbor = hex.GetNeighbor(hexDirection);

                        // Get neighbor that should be in the map at that position
                        HexTile expectedNeighbor = GetHexAt(expectedNeighborPos);

                        // Case 1: Hex has a neighbor reference, but position is wrong
                        if (actualNeighbor != null && !actualNeighbor.Position.Equals(expectedNeighborPos))
                        {
                            AppService.CaptureUiMessage($"Connectivity error: Hex at {hex.Position} has neighbor in direction {hexDirection} at wrong position {actualNeighbor.Position}, expected {expectedNeighborPos}");
                            connectivityErrors++;
                            brokenRelationships++;
                            isValid = false;
                        }

                        // Case 2: Expected neighbor exists but hex doesn't reference it
                        if (expectedNeighbor != null && actualNeighbor == null)
                        {
                            AppService.CaptureUiMessage($"Connectivity error: Hex at {hex.Position} missing neighbor reference in direction {hexDirection} to {expectedNeighborPos}");
                            connectivityErrors++;
                            brokenRelationships++;
                            isValid = false;
                        }

                        // Case 3: Hex references neighbor that doesn't exist in map
                        if (actualNeighbor != null && expectedNeighbor == null)
                        {
                            AppService.CaptureUiMessage($"Connectivity error: Hex at {hex.Position} references non-existent neighbor in direction {hexDirection} at {actualNeighbor.Position}");
                            connectivityErrors++;
                            brokenRelationships++;
                            isValid = false;
                        }

                        // Case 4: References don't match (hex A points to hex B, but hex B is not at expected position)
                        if (actualNeighbor != null && expectedNeighbor != null && !ReferenceEquals(actualNeighbor, expectedNeighbor))
                        {
                            AppService.CaptureUiMessage($"Connectivity error: Hex at {hex.Position} neighbor reference mismatch in direction {hexDirection}");
                            connectivityErrors++;
                            brokenRelationships++;
                            isValid = false;
                        }

                        // Track if hex has any valid neighbors
                        if (actualNeighbor != null && expectedNeighbor != null && ReferenceEquals(actualNeighbor, expectedNeighbor))
                        {
                            hasAnyNeighbor = true;
                        }

                        // Validate bidirectional relationships
                        if (actualNeighbor != null && expectedNeighbor != null && ReferenceEquals(actualNeighbor, expectedNeighbor))
                        {
                            // Check if the neighbor points back to this hex
                            HexDirection oppositeDirection = GetOppositeDirection(hexDirection);
                            HexTile backReference = actualNeighbor.GetNeighbor(oppositeDirection);

                            if (!ReferenceEquals(backReference, hex))
                            {
                                AppService.CaptureUiMessage($"Connectivity error: Non-bidirectional relationship between {hex.Position} and {actualNeighbor.Position}");
                                connectivityErrors++;
                                brokenRelationships++;
                                isValid = false;
                            }
                        }
                    }

                    // Check for orphaned hexes (no valid neighbors when they should have some)
                    if (!hasAnyNeighbor)
                    {
                        // Check if this hex should have neighbors based on its position
                        bool shouldHaveNeighbors = false;
                        for (int direction = 0; direction < 6; direction++)
                        {
                            Position2D neighborPos = HexMapUtil.GetNeighborPosition(hex.Position, (HexDirection)direction);
                            if (IsPositionInBounds(neighborPos) && hexDictionary.ContainsKey(neighborPos))
                            {
                                shouldHaveNeighbors = true;
                                break;
                            }
                        }

                        if (shouldHaveNeighbors)
                        {
                            AppService.CaptureUiMessage($"Connectivity warning: Hex at {hex.Position} appears orphaned despite having potential neighbors");
                            orphanedHexes++;
                            // This is a warning, not necessarily an error for edge hexes
                        }
                    }
                }

                if (enableDebugLogging)
                {
                    if (isValid)
                    {
                        Debug.Log($"{CLASS_NAME}: Connectivity validation passed - all neighbor relationships valid");
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Connectivity validation failed - {connectivityErrors} total errors, {brokenRelationships} broken relationships, {orphanedHexes} orphaned hexes");
                    }
                }

                if (isValid)
                {
                    AppService.CaptureUiMessage($"Connectivity validation passed for map '{MapName}'");
                }
                else
                {
                    AppService.CaptureUiMessage($"Connectivity validation failed - {connectivityErrors} errors found");
                }

                if (orphanedHexes > 0)
                {
                    AppService.CaptureUiMessage($"Connectivity validation found {orphanedHexes} potentially orphaned hexes");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateConnectivity), ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the opposite direction for bidirectional relationship validation.
        /// </summary>
        /// <param name="direction">Original direction</param>
        /// <returns>Opposite direction</returns>
        private static HexDirection GetOppositeDirection(HexDirection direction)
        {
            return direction switch
            {
                HexDirection.NW => HexDirection.SE,
                HexDirection.NE => HexDirection.SW,
                HexDirection.E => HexDirection.W,
                HexDirection.SE => HexDirection.NW,
                HexDirection.SW => HexDirection.NE,
                HexDirection.W => HexDirection.E,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid hex direction")
            };
        }

        #endregion // Validation

        #region Save/Load (Stubs)

        /// <summary>
        /// Saves the hex map to the specified file path.
        /// </summary>
        /// <param name="filePath">Path to save the map file</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                ValidateState();

                // TODO: Implement save functionality
                // TODO: Convert to JsonMapData format
                // TODO: Use snapshot pattern for serialization
                // TODO: Calculate and store checksum

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: SaveToFile - stub implementation for path: {filePath}");
                }

                AppService.CaptureUiMessage($"Save functionality not yet implemented for map '{MapName}'");
                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveToFile), ex);
                return false;
            }
        }

        /// <summary>
        /// Loads a hex map from the specified file path.
        /// </summary>
        /// <param name="filePath">Path to the map file</param>
        /// <returns>Loaded HexMap instance, or null if loading failed</returns>
        public static HexMap LoadFromFile(string filePath)
        {
            try
            {
                // TODO: Implement load functionality
                // TODO: Read and deserialize JsonMapData
                // TODO: Convert HexTile[] to MapHex instances
                // TODO: Validate checksum integrity
                // TODO: Build neighbor relationships

                Debug.Log($"{CLASS_NAME}: LoadFromFile - stub implementation for path: {filePath}");
                AppService.CaptureUiMessage("Load functionality not yet implemented");
                return null;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadFromFile), ex);
                return null;
            }
        }

        #endregion // Save/Load

        #region IEnumerable Implementation

        /// <summary>
        /// Returns an enumerator that iterates through the hexes in the map.
        /// </summary>
        /// <returns>Enumerator for MapHex instances</returns>
        public IEnumerator<HexTile> GetEnumerator()
        {
            try
            {
                ValidateState();
                return hexDictionary.Values.GetEnumerator();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetEnumerator), ex);
                return Enumerable.Empty<HexTile>().GetEnumerator();
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator that iterates through the hexes.
        /// </summary>
        /// <returns>Non-generic enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // IEnumerable Implementation

        #region Private Methods

        /// <summary>
        /// Validates the current state of the hex map.
        /// </summary>
        private void ValidateState()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized");
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(CLASS_NAME);
            }
        }

        #endregion // Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the hex map and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the hex map's resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if finalizing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Dispose all hexes
                        if (hexDictionary != null)
                        {
                            foreach (var hex in hexDictionary.Values)
                            {
                                hex?.Dispose();
                            }
                            hexDictionary.Clear();
                            hexDictionary = null;
                        }

                        if (enableDebugLogging)
                        {
                            Debug.Log($"{CLASS_NAME}: Map '{MapName}' disposed");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(Dispose), ex);
                    }
                }

                IsDisposed = true;
            }
        }

        #endregion // IDisposable Implementation
    }

    /// <summary>
    /// Custom equality comparer for Position2D that uses exact comparison
    /// instead of epsilon-based equality for dictionary key performance.
    /// </summary>
    public class Coordinate2DEqualityComparer : IEqualityComparer<Position2D>
    {
        public bool Equals(Position2D x, Position2D y)
        {
            return x.X.Equals(y.X) && x.Y.Equals(y.Y);
        }

        public int GetHashCode(Position2D obj)
        {
            return HashCode.Combine(obj.X, obj.Y);
        }
    }
}