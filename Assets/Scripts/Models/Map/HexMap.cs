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
                return position.x >= 0 && position.x < MapSize.x &&
                       position.y >= 0 && position.y < MapSize.y;
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

        #region Validation (Stubs)

        /// <summary>
        /// Validates the integrity of the hex map data.
        /// </summary>
        /// <returns>True if map data is valid, false otherwise</returns>
        public bool ValidateIntegrity()
        {
            try
            {
                ValidateState();

                // TODO: Implement checksum validation
                // TODO: Implement hex consistency checks
                // TODO: Implement neighbor relationship validation

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: ValidateIntegrity - stub implementation");
                }

                return true;
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

                // TODO: Implement bounds checking for all hexes
                // TODO: Use Position2D.Clamp() for validation

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: ValidateDimensions - stub implementation");
                }

                return true;
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

                // TODO: Implement neighbor relationship validation
                // TODO: Check for orphaned hexes
                // TODO: Validate bidirectional relationships

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: ValidateConnectivity - stub implementation");
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateConnectivity), ex);
                return false;
            }
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
            return x.x.Equals(y.x) && x.y.Equals(y.y);
        }

        public int GetHashCode(Position2D obj)
        {
            return HashCode.Combine(obj.x, obj.y);
        }
    }
}